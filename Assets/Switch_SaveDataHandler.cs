using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime;
#if UNITY_SWITCH
    using nn.account;
    using nn.fs;
#endif
using UnityEngine.UI;
using System.Runtime.InteropServices;

#if !UNITY_SWITCH

public class Switch_SaveDataHandler : MonoBehaviour
{ }


#endif

#if UNITY_SWITCH

// This code is a for a saving sample that saves/loads a single string.

// Important: This code requires that in the Unity Editor you set PlayerSettings > Publishing Settings > Startup user account to 'Required'.
// This code does not check whether or not there is enough free space on disk to create save date. Instead, it relies on the 'Startup user account'
// setting to verify there is enough free space on device. If you would like to instead manage how your game creates save data space on device,
// see the NintendoSDK plugin and the NintendoSDK documentation.

public class Switch_SaveDataHandler : MonoBehaviour
{
#if UNITY_SWITCH
     [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
     private static extern void TestPenif(string words);
     [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
     private static extern void ReadFileChunk(FileHandle handle, long offset, byte[] buffer, long size, long buffer_offset);
#endif

    private static Uid userId; // user ID for the user account on the Nintendo Switch
    private const string mountName = "saveData";
    private string saveDataPath = mountName + ":/";
    private FileHandle fileHandle = new nn.fs.FileHandle();


    //A collection of buckets used to load assetbundles and other files quickly.
    private static List<byte[]> listByteBuckets;

    //The bucket from the above list we are currently using to load files
    private static byte[] currentLoadingBucket;
    private const int k24Mb = 1024 * 1024 * 24;
    private const int k2Mb = 1024 * 1024 * 2;

    enum EBucketSizes
    {
        _2mb = 0,
        _4mb = 1,
        _8mb = 2,
        _16mb = 3,
        _32mb = 4,
        max
    }


    // Save journaling memory is used for each time files are created, deleted, or written.
    // The journaling memory is freed after nn::fs::CommitSaveData is called.
    // For any single time you save data, check the file size against your journaling size.
    // Check against the total save data size only when you want to be sure all files don't exceed the limit.
    // The variable journalSaveDataSize is only a value that is checked against in this code. The actual journal size is set in the
    // Unity editor in PlayerSettings > Publishing Settings > User account save data
      
        // this was 8192000 before 12/16/19
    private const int journalSaveDataSize = 16777248 - (32 * 1024); // - 2 blocks

                                                                // 16 KB. This value should be 32KB less than the journal size
                                                                // entered in PlayerSettings > Publishing Settings

    private const int loadBufferSize = journalSaveDataSize;     // why would this be any smaller than the journal? 
    private static bool bInitialized;

    [HideInInspector]
    public  static string strCurrentFileInAsyncLoad;

    private static Switch_SaveDataHandler _instance;

    //A byte buffer used to read stuff from disk
    const int kMaxLoadBlockSize = 65536 * 2;    
    private byte[] byteBucket;
    private Dictionary<EBucketSizes, long> dictBucketSizes;


    //Used to keep track of performance metrics for us
    private static Dictionary<string, float> dictMarkedTime;

    public void Awake()
    {
        FirstAwakeOrInitialize();
    }

    bool initialized;

    public void FirstAwakeOrInitialize()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (bInitialized) return;
        _instance = this;
        DontDestroyOnLoad(gameObject);
        initialize();
    }

    public void initialize()
    {
        if (Debug.isDebugBuild) Debug.Log("Calling Save Data Handler Initialize");
        nn.Result result;
        if (bInitialized)
        {
            return;
            result = nn.fs.SaveData.Mount(mountName, userId);
            if (result.IsSuccess() == false)
            {
                Debug.Log("Critical Error: File System could not be mounted a second time. " +  result.GetDescription());
                result.abortUnlessSuccess();
            }

            return;
        }

        nn.account.Account.Initialize();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();

        if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }

        result = nn.account.Account.GetUserId(ref userId, userHandle);
        result.abortUnlessSuccess();
        result = nn.fs.SaveData.Mount(mountName, userId);
        result.abortUnlessSuccess();

        //print out error (debug only) and abort if the filesystem couldn't be mounted 
        if (result.IsSuccess() == false)
        {
           Debug.Log("Critical Error: File System could not be mounted.");
           result.abortUnlessSuccess();
        }

        //time markers
        dictMarkedTime = new Dictionary<string, float>();

        //various sizes of bucket
        dictBucketSizes = new Dictionary<EBucketSizes, long>();
        listByteBuckets = new List<byte[]>();
        for (EBucketSizes eb = EBucketSizes._2mb; eb < EBucketSizes.max; eb++)
        {
            dictBucketSizes[eb] = k2Mb * (long) Math.Pow(2,(int) eb);
            //listByteBuckets.Add(new byte[dictBucketSizes[eb]]);
            listByteBuckets.Add(new byte[0]);
        }

        bInitialized = true;

        if (Debug.isDebugBuild) Debug.Log("Switch save data handler initialized.");
    }

    void OnDestroy()
    {
        nn.fs.FileSystem.Unmount(mountName);
    }

    //
    EBucketSizes SetBucketForFileLength(long iLemf)
    {
        for (EBucketSizes eb = EBucketSizes._2mb; eb < EBucketSizes.max; eb++)
        {
            if (iLemf < dictBucketSizes[eb])
            {
                return eb;
            }
        }

        //too big!

        Debug.Log("Dis is too big");

        return EBucketSizes.max;
    }

    public void SaveSwitchFile(string dataToSave, string filename)
    {
        //#TGS: Do not save anything
        if (!Switch_DebugMenu.tgs_AllowSaveData)
        {
            Debug.Log("TGS: *Not* saving " + filename);
            return;
        }
        

        //if (Debug.isDebugBuild) Debug.Log("Saving Data file '" + filename + "' data length is " + dataToSave.Length + " character(s)");

        string filePath = saveDataPath + filename;

        byte[] dataByteArray;
        using (MemoryStream stream = new MemoryStream(dataToSave.Length * sizeof(char))) //  journalSaveDataSize)) // the stream size must be less than or equal to the save journal size
        {
            BinaryWriter binaryWriter = new BinaryWriter(stream);
            binaryWriter.Write(dataToSave);
            stream.Close();
            dataByteArray = stream.GetBuffer();
        }

        //if (Debug.isDebugBuild) Debug.Log("File successfully saved.");

#if UNITY_SWITCH && !UNITY_EDITOR
        // This next line prevents the user from quitting the game while saving. 
        // This is required for Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif

        // If you only ever save the entire file, it may be simpler just to delete the file and create a new one every time you save.
        // Most of the functions return an nn.Result which can be used for debugging purposes.

        /* nn.Result result = nn.fs.File.Delete(filePath);
        result.abortUnlessSuccess();

        if (Debug.isDebugBuild) Debug.Log("Previous file deleted."); */

        long iSaveSizeBytes = dataByteArray.LongLength;

        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);        
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            //if (Debug.isDebugBuild) Debug.Log("File doesn't exist. Creating it.");            
            result = nn.fs.File.Create(filePath, iSaveSizeBytes); //this makes a file the size of your save journal. You may want to make a file smaller than this.
            result.abortUnlessSuccess();
            //if (Debug.isDebugBuild) Debug.Log("File created.");
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("File does exist, not creating it.");
        }        

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        //if (Debug.isDebugBuild) Debug.Log("File opened.");

        nn.fs.File.SetSize(fileHandle, iSaveSizeBytes);

        result = nn.fs.File.Write(fileHandle, 0, dataByteArray, iSaveSizeBytes, nn.fs.WriteOption.Flush); // Writes and flushes the write at the same time
        result.abortUnlessSuccess();

        //if (Debug.isDebugBuild) Debug.Log("File written.");

        nn.fs.File.Close(fileHandle);

        //if (Debug.isDebugBuild) Debug.Log("File closed.");

        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();
        

        //if (Debug.isDebugBuild) Debug.Log("Everything has been committed.");

#if UNITY_SWITCH && !UNITY_EDITOR
        // End preventing the user from quitting the game while saving.
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif

    }

    //Shep
    public void SaveBinarySwitchFile(byte[] dataToSave, string filename)
    {
        //#TGS: Do not save anything
        if (!Switch_DebugMenu.tgs_AllowSaveData)
        {
            //if (Debug.isDebugBuild) Debug.Log("TGS: *Not* saving " + filename);
            return;
        }
        

        string filePath = saveDataPath + filename;

        //if (Debug.isDebugBuild) Debug.Log("save_binary start, filePath==" + filePath);

#if UNITY_SWITCH && !UNITY_EDITOR
        // This next line prevents the user from quitting the game while saving. 
        // This is required for Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif

        long iSaveSizeBytes = dataToSave.LongLength;

        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            //if (Debug.isDebugBuild) Debug.Log("File doesn't exist. Creating it.");
            result = nn.fs.File.Create(filePath, iSaveSizeBytes); //this makes a file the size of your save journal. You may want to make a file smaller than this.
            result.abortUnlessSuccess();
            //if (Debug.isDebugBuild) Debug.Log("File created.");
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("File does exist, not creating it.");
        }

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        //if (Debug.isDebugBuild) Debug.Log("File opened.");

        nn.fs.File.SetSize(fileHandle, iSaveSizeBytes);

        result = nn.fs.File.Write(fileHandle, 0, dataToSave, iSaveSizeBytes, nn.fs.WriteOption.Flush); // Writes and flushes the write at the same time
        result.abortUnlessSuccess();

        //if (Debug.isDebugBuild) Debug.Log("File written");

            nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName); //you must commit the changes.
        result.abortUnlessSuccess();

#if UNITY_SWITCH && !UNITY_EDITOR
        // End preventing the user from quitting the game while saving.
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif

        //Debug.Log("save_binary commit success, filePath==" + filePath);
    }

    public bool DeleteSwitchDataFile(string filename)
    {
        //#TGS: Do not delete anything
        if (!Switch_DebugMenu.tgs_AllowSaveData)
        {
            Debug.Log("TGS: *Not* deleting " + filename);
            return true;
        }

        string filePath = saveDataPath + filename;

#if UNITY_SWITCH && !UNITY_EDITOR
        // This next line prevents the user from quitting the game while saving. 
        // This is required for Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif
        //this should be ok even if the file doesn't exist
        nn.Result res = nn.fs.File.Delete(filePath);        
        res = nn.fs.FileSystem.Commit(mountName); //you must commit the changes.
        res.abortUnlessSuccess();

#if UNITY_SWITCH && !UNITY_EDITOR
        // End preventing the user from quitting the game while saving.
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif

        return res.IsSuccess();
    }

    //Shep
    public bool CheckIfSwitchFileExists(string filename)
    {
        string filePath = saveDataPath + filename;

        //Debug.Log("Checking file exists " + filePath);

        nn.Result res = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
        if( !res.IsSuccess())
        {
            return false;
        }
        nn.fs.File.Close(fileHandle);        

        return true;
    }
        
    public bool LoadSwitchDataFile(ref string outputData, string filename)
    {
        if (Debug.isDebugBuild) Debug.Log("Trying to load '" + filename + "' and saveDataPath is " + saveDataPath);
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, saveDataPath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            if (Debug.isDebugBuild) Debug.Log("Not successful loading save data " + filename + " " + result.GetDescription());
            return false;
        }

        result = nn.fs.File.Open(ref fileHandle, saveDataPath + filename, nn.fs.OpenFileMode.Read);
        if (result.IsSuccess() == false)
        {
            if (Debug.isDebugBuild) Debug.Log("Not successful loading save data " + filename + " " + result.GetDescription());

            return false;   // Could not open file. This can be used to detect if this is the first time a user has launched your game. 
                            // (However, be sure you are not getting this error due to your file being locked by another process, etc.)
        }
        long iFileSize = 0;
        nn.fs.File.GetSize(ref iFileSize, fileHandle);

        //if (Debug.isDebugBuild) Debug.Log("File opened successfully.");

        byte[] loadedData = new byte[iFileSize];
        result = nn.fs.File.Read(fileHandle, 0, loadedData, iFileSize);
        result.abortUnlessSuccess();

        //if (Debug.isDebugBuild) Debug.Log("File read successfully.");

        nn.fs.File.Close(fileHandle);

        using (MemoryStream stream = new MemoryStream(loadedData))
        {
            if( loadedData.Length == 0 )
            {
                //if (Debug.isDebugBuild) Debug.Log("Load: loaded data '" + filename + "' loaded nothing.");
            }
            else
            {
                BinaryReader reader = new BinaryReader(stream);

                outputData = reader.ReadString();
                //if (Debug.isDebugBuild) Debug.Log("Load: loaded data '" + filename + "' size is " + loadedData.Length + " characters.") ; // Data is " + outputData);
            }
        }
        return true;
    }

    //Shep
    public bool LoadBinarySwitchSave(ref BinaryReader reader, string filename)
    {
        //Debug.Log("Tryna load binary file " + filename );
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, saveDataPath);

        if(result.IsSuccess() == false)
        {
            if (Debug.isDebugBuild) Debug.Log("Failed to open binary file " + filename + ", NN says " + result.ToString());
            return false;
        }

        result = nn.fs.File.Open(ref fileHandle, saveDataPath + filename, nn.fs.OpenFileMode.Read);
        if (result.IsSuccess() == false)
        {
            if (Debug.isDebugBuild) Debug.Log("Failed to open binary file " + filename + ", NN says " + result.ToString());
            return false;   // Could not open file. This can be used to detect if this is the first time a user has launched your game. 
                            // (However, be sure you are not getting this error due to your file being locked by another process, etc.)
        }
        long iFileSize = 0;
        nn.fs.File.GetSize(ref iFileSize, fileHandle);

        byte[] loadedData = new byte[iFileSize];
        nn.fs.File.Read(fileHandle, 0, loadedData, iFileSize);
        nn.fs.File.Close(fileHandle);

        MemoryStream stream = new MemoryStream(loadedData);
        reader = new BinaryReader(stream);
        //Debug.Log("Successful load of " + filename + " is reader null?" + (reader == null));

        return true;
    }

    public byte[] LoadBinarySwitchFile(string filename)
    {
        //Debug.Log("Tryna load binary file " + filename);
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.fs.FileSystem.GetEntryType(ref entryType, saveDataPath);
        nn.Result result = nn.fs.File.Open(ref fileHandle, saveDataPath + filename, nn.fs.OpenFileMode.Read);
        if (result.IsSuccess() == false)
        {
            Debug.Log("Failed to open binary file " + filename + ", NN says " + result.ToString());
            return null;   // Could not open file. This can be used to detect if this is the first time a user has launched your game. 
                            // (However, be sure you are not getting this error due to your file being locked by another process, etc.)
        }
        long iFileSize = 0;
        nn.fs.File.GetSize(ref iFileSize, fileHandle);

        byte[] loadedData = new byte[iFileSize];
        
        nn.fs.File.Read(fileHandle, 0, loadedData, iFileSize);
        nn.fs.File.Close(fileHandle);

        //Debug.Log("Successful load of " + filename + " as a byte array.");

        return loadedData;
    }

    public IEnumerator LoadSwitchSavedDataFileAsync(string filename, bool bChangeGarbageCollectionTiming = true)
    {
        if (bChangeGarbageCollectionTiming)
        {
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        }

        string strPath = filename;
        if (!filename.Contains(":"))
        {
            strPath = saveDataPath + filename;
        }

        //if (Debug.isDebugBuild) Debug.Log("Loading binary file async.");


        if (CheckIfSwitchFileExists(filename))
        {
            yield return LoadSwitchBinaryFileAsync(strPath, filename);
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("File " + filename + " does not exist.");
        }
        

        //if (Debug.isDebugBuild) Debug.Log("Done loading binary file.");

        if (bChangeGarbageCollectionTiming)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

    }

    private void SetFileNameCurrentlyLoadingAsync(string newName)
    {
        if (!string.IsNullOrEmpty(strCurrentFileInAsyncLoad))
        {
            Debug.LogError("Oh no! I'm loading " + strCurrentFileInAsyncLoad + " but somehow also loading " + newName);
            return;
        }

        strCurrentFileInAsyncLoad = newName;
    }

    public IEnumerator LoadSwitchBinaryFileAsync(string filename, string strByteKey = null)
    {
        //if (Debug.isDebugBuild) Debug.Log("Begin load binary file async of " + filename);
        int i = 0;

        //Wait for any other loads to finish before going
        while (!String.IsNullOrEmpty(strCurrentFileInAsyncLoad))
        {
            //if (i % 100 == 0 && Debug.isDebugBuild) Debug.Log("Waiting to load file " + filename + " because we're still loading " + strCurrentFileInAsyncLoad);
            yield return null;
            i++;
        }

        //if (Debug.isDebugBuild) Debug.Log("Request load " + filename);

        float fTimer = Time.realtimeSinceStartup;
        SetFileNameCurrentlyLoadingAsync(filename);

        FileHandle asyncFileHandle = new nn.fs.FileHandle();
        
        //if (Debug.isDebugBuild) Debug.Log("Async load binary file " + filename);
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.fs.FileSystem.GetEntryType(ref entryType, filename);

        //if (Debug.isDebugBuild) Debug.Log("Got entry type! " + entryType.ToString());

        nn.Result result = nn.fs.File.Open(ref asyncFileHandle, filename, nn.fs.OpenFileMode.Read);

        //if (Debug.isDebugBuild) Debug.Log("Got async handle!");

        if (result.IsSuccess() == false)
        {
            Debug.Log("Failed to open async binary file " + filename + ", NN says " + result.ToString());
            strCurrentFileInAsyncLoad = null;
            yield break;
        }

        //if (Debug.isDebugBuild) Debug.Log("File was opened and no one got hurt!!!");
        

        long iFileSize = 0;
        nn.fs.File.GetSize(ref iFileSize, asyncFileHandle);

        if (iFileSize <= 0)
        {
            Debug.Log("Why is file size less or equal to 0? " + iFileSize);
            yield break;
        }

        //if (Debug.isDebugBuild) Debug.Log("File size is " + iFileSize + ", max block is " + kMaxLoadBlockSize);

        int iOffset = 0;
        int iAsyncLoadBlock = (int)Mathf.Min(kMaxLoadBlockSize, iFileSize);

        //if (Debug.isDebugBuild) Debug.Log("async load block is " + iAsyncLoadBlock);

        //find out how big we need our bucket to be.
        EBucketSizes bucketSizeForFile = SetBucketForFileLength(iFileSize);

        //Debug.Log("Bucket size needed is " + bucketSizeForFile);

        //set the correct one -- if we haven't yet made this bucket, make it now. 
        if (listByteBuckets[(int) bucketSizeForFile].Length == 0)
        {
            listByteBuckets[(int) bucketSizeForFile] = new byte[dictBucketSizes[bucketSizeForFile]];
        }
       
        currentLoadingBucket = listByteBuckets[(int)bucketSizeForFile];

        //clear it out
        Array.Clear(currentLoadingBucket,0,(int)dictBucketSizes[bucketSizeForFile]);

        //if (Debug.isDebugBuild) Debug.Log("File " + filename + " is " + iFileSize + " big, so I'm using bucket " + bucketSizeForFile + " which is " + dictBucketSizes[bucketSizeForFile] + " bytes long.");

        while (iOffset < iFileSize && currentLoadingBucket != null && currentLoadingBucket.Length > 0)
        {
            //if we are writing to this chunk, but somehow shouldn't be, then cry
            //if (strCurrentFileInAsyncLoad != filename)
            //{
            //    Debug.LogError("Somehow got into the while loop of async loading while someone else was there too.");
            //}

            //grab the smallest amount so we don't go over the edge.
            int iReadAmount = (int)Mathf.Min(iFileSize - iOffset, iAsyncLoadBlock);

            //read in one chunk of data
#if UNITY_SWITCH
            //Debug.Log("Prepare to read one file chunk at a time. Current offset: " + iOffset + " Read amount: " + iReadAmount);
            if (strCurrentFileInAsyncLoad != filename)
            {
                //Debug.Log("Somehow got into the while loop of async loading while someone else was there too."); 
            }
            ReadFileChunk(asyncFileHandle, iOffset, currentLoadingBucket, iReadAmount, iOffset);
#endif
            iOffset += iReadAmount;

            //yield if we're killing the frame rate
            if (Time.realtimeSinceStartup - fTimer > 0.024f ) //0.016f)
            {                
                yield return null;
                fTimer = Time.realtimeSinceStartup;
            }
        }

        //if (Debug.isDebugBuild) Debug.Log("Done while loop.");

        nn.fs.File.Close(asyncFileHandle);

        //do NOT clear the currentFile flag here -- instead wait for it to be picked up
        //by the caller who asked us to load the file
        //strCurrentFileInAsyncLoad = null;
        
    }

    //This will return the bytes we asked for, AND allow us to load a new file down the road
    //because we are clearing the strCurrentFile value
    public static void GetBytesLoadedAsync(string filename, ref byte[] loadedBytes )
    {
        loadedBytes = currentLoadingBucket;
        strCurrentFileInAsyncLoad = null;

    }

    /// <summary>
    /// Clear this out to prevent loading issues during scene changes.
    /// </summary>
    public static void FlushBytesLoadedAsync()
    {
        strCurrentFileInAsyncLoad = null;
        currentLoadingBucket = null;

        for (int t=0; t < (int)EBucketSizes.max; t++ )
        {
            listByteBuckets[t] = new byte[0];
        }

        //if (Debug.isDebugBuild) Debug.Log("All bytes flushed.");
    }

    public static void Initialize()
    {
       _instance.initialize();
    }

    public static Switch_SaveDataHandler GetInstance()
    {
        return _instance;
    }

    public static void MarkTime(string strMarker)
    {
        float fRealTime = Time.realtimeSinceStartup;
        if (Debug.isDebugBuild)
        { 
            if (dictMarkedTime.ContainsKey(strMarker))
            {
                if (Debug.isDebugBuild) Debug.Log("Time Marked: " + strMarker + " at " + fRealTime + ". Since last mark: " + (fRealTime - dictMarkedTime[strMarker]));
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Time Marked: " + strMarker + " at " + fRealTime + ".");
            }
        }

        dictMarkedTime[strMarker] = fRealTime;
    }
}

#endif