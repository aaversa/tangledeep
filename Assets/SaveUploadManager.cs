/* 
#if !UNITY_SWITCH
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nn;
using System;
using System.Text;

public class SaveUploadManager : MonoBehaviour
{

    const uint PLUGIN_MEMSIZE = 1024 * 1024;
    const uint NEX_MEMSIZE = (((1024 * 2) + 400) * 1024);
    const uint RESERVE_MEMSIZE = (400 * 1024);
    static uint async_id = 0;

    public uint DispatchTimeOut = 3;

    private static IntPtr ngsFacade = IntPtr.Zero;
    static uint gameServerId = 12345;
    static string accessKey = "xxxxxxxx";
    static int timeOut = 30000;
    // The nsaId of the user to log in, must be obtained in advance using nn.account.NetworkServiceAccount.GetId().
    static nn.account.NetworkServiceAccountId nsaId;
    // The nsaIdToken for the user to log in, must be obtained in advance using nn.account.NetworkServiceAccount.LoadIdTokenCache().
    static byte[] idTokenCache;

    static int[] byteSizeOfChunks;

    static void OnConnectionLost()
    {

    }

    public const float MIN_DELAY_BETWEEN_UPLOADS = 60f;

    public static float timeOfLastFileUpload;

    public static SaveUploadManager singleton;

    private static bool s_NexAsync = false;

    public void Initialize()
    {
        NexAssets.Common.LostConnectCallback callback = new NexAssets.Common.LostConnectCallback(OnConnectionLost);
        NexAssets.Common.SetConnectionLostCallback(callback);
        singleton = this;
        return;
    }    

    void Update()
    {
        NexPlugin.Common.UpdateAsyncResult();
        NexPlugin.Common.Dispatch(DispatchTimeOut);
    }

    void LateUpdate()
    {
        NexPlugin.Common.Dispatch(DispatchTimeOut);
    }

    [SerializeField]
    private NexAssets.DataStore_Upload m_Upload;
    [SerializeField]
    private NexAssets.DataStorePreparePostParam postParam;

    void OnValidate()
    {
        postParam.Validate();
    }

    private static NexPlugin.DataStore.PostCB m_PostDataCallBack = null;         //!<  Callback function to call when the data sending process is complete.
    public static NexPlugin.DataStore.PostCB PostDataCB                          ///  Callback function when the data sending process is complete (for reference).
    {
        set { m_PostDataCallBack = value; }
    }

    private static NexPlugin.AsyncResultCB m_UpdateDataCallBack = null;     //!<  Callback function to call when the data upload process is complete.
    public static NexPlugin.AsyncResultCB UpdateDataCB                      ///  Callback function when the data upload process is complete (for reference).
    {
        set { m_UpdateDataCallBack = value; }
    }


    // ---------------------------------------------------------
    /// @brief  Uploads data.
    /// @details  Uploads data if <tt><var>dataID</var></tt> is invalid.
    ///  Updates data if <tt><var>dataID</var></tt> is valid.
    ///
    /// @param[in] data  Data.
    /// @param[in] dataID  Data ID.
    // ---------------------------------------------------------
    public static void UploadData(byte[] dataArray, ulong dataID)
    {
        if (dataID == NexPlugin.DataStore.INVALID_DATAID)
        {
            //  Configure the parameters.
            NexPlugin.DataStorePreparePostParam param = singleton.postParam.GetDataStorePreparePostParam();
            param.SetTags(new List<string> { "GameData" });

            //  Set the send data to dataArray as a byte array.
            param.SetSize((uint)dataArray.Length);
            //  Set a callback function.
            NexPlugin.DataStore.PostCB postCallback;
            if (m_PostDataCallBack != null)
            {
                postCallback = m_PostDataCallBack;
            }
            else
            {
                postCallback = singleton.PostCallback;
            }

            //  Call PostDataAsync with SendMessage.
            singleton.m_Upload.PostObjectAsync(param, dataArray, postCallback);
        }
        else
        {
            //  If the data has a valid data ID, update the data.
            //  Configure the parameters.
            NexPlugin.DataStorePrepareUpdateParam param = new NexPlugin.DataStorePrepareUpdateParam();
            param.SetDataId(dataID);
            param.SetUpdatePassword(NexPlugin.DataStore.INVALID_PASSWORD);

            //  Set the send data to dataArray as a byte array.
            param.SetSize((uint)dataArray.Length);

            //  Set a callback function.
            NexPlugin.AsyncResultCB updateCallback;
            if (m_UpdateDataCallBack != null)
            {
                updateCallback = m_UpdateDataCallBack;
            }
            else
            {
                updateCallback = singleton.UpdateCallback;
            }

            //  Call UpdateDataAsync with SendMessage.
            singleton.m_Upload.UpdateObjectAsync(param, dataArray, updateCallback);
        }
    }

    void PostCallback(NexPlugin.AsyncResult asyncResult, ulong dataId)
    {
        if (asyncResult.IsSuccess() == false)
        {
            Debug.Log(asyncResult.netErrCode);
            Debug.Log("netErrCode " + asyncResult.netErrCode + " returnCode " + asyncResult.returnCode);
        }
        else
        {
            Debug.Log("Successfully uploaded data!");
        }
    }

    void UpdateCallback(NexPlugin.AsyncResult asyncResult)
    {
        if (asyncResult.IsSuccess() == false)
        {
            Debug.Log(asyncResult.netErrCode);
            Debug.Log("netErrCode " + asyncResult.netErrCode + " returnCode " + asyncResult.returnCode);
        }
    }

    public static byte[] CreateByteArrayFromFilesInSlot(int saveIndex)
    {
        // This array contains the size (in bytes) of the metaprogress, savedgame, and savedmap files.
        byteSizeOfChunks = new int[3];

        string strPath = "metaprogress" + saveIndex + ".xml";
        string metaProgressData = "";

        if (Debug.isDebugBuild) Debug.Log("Attempting to load meta progress from path: " + strPath);

#if UNITY_SWITCH
        var sdh = Switch_SaveDataHandler.GetInstance();
        sdh.load(ref metaProgressData, strPath);

        byte[] metaProgressBytes = Encoding.UTF8.GetBytes(metaProgressData);

        Debug.Log("Meta length is " + metaProgressData.Length);

        strPath = "savedGame" + saveIndex + ".xml";
        string saveGameData = "";
        sdh.load(ref saveGameData, strPath);

        if (Debug.isDebugBuild) Debug.Log("Attempting to load save progress from path: " + strPath);

        byte[] savedGameBytes = Encoding.UTF8.GetBytes(saveGameData);

        string mapPath = "savedMap" + saveIndex + ".dat";

        if (Debug.isDebugBuild) Debug.Log("Success! Game length is " + saveGameData.Length + ", now attempting to load map data from path: " + mapPath);

        byte[] mapDataBytes = sdh.load_binary_file(mapPath);

        byte[] masterSaveByteArray = new byte[metaProgressBytes.Length + savedGameBytes.Length + mapDataBytes.Length];

        byteSizeOfChunks[0] = metaProgressBytes.Length;
        byteSizeOfChunks[1] = savedGameBytes.Length;
        byteSizeOfChunks[2] = mapDataBytes.Length;

        Debug.Log("Master array length is " + masterSaveByteArray.Length);

        Buffer.BlockCopy(metaProgressBytes, 0, masterSaveByteArray, 0, metaProgressBytes.Length);
        Buffer.BlockCopy(savedGameBytes, 0, masterSaveByteArray, metaProgressBytes.Length, savedGameBytes.Length);
        Buffer.BlockCopy(mapDataBytes, 0, masterSaveByteArray, metaProgressBytes.Length+savedGameBytes.Length, mapDataBytes.Length);

        return masterSaveByteArray;
#else
        return null;
#endif
    }

    public static void SaveByteArrayToIndex(byte[] saveData, int saveIndex)
    {
#if UNITY_SWITCH
        Debug.Log("Loading master array of size " + saveData.Length);

        byte[] metaProgressBytes = new byte[byteSizeOfChunks[0]];
        byte[] savedGameBytes = new byte[byteSizeOfChunks[1]];
        byte[] mapDataBytes = new byte[byteSizeOfChunks[2]];

        Buffer.BlockCopy(saveData, 0, metaProgressBytes, 0, metaProgressBytes.Length);
        Buffer.BlockCopy(saveData, metaProgressBytes.Length, savedGameBytes, 0, savedGameBytes.Length);
        Buffer.BlockCopy(saveData, metaProgressBytes.Length+savedGameBytes.Length, mapDataBytes, 0, mapDataBytes.Length);

        var sdh = Switch_SaveDataHandler.GetInstance();

        string metaString = Encoding.UTF8.GetString(metaProgressBytes);
        Debug.Log("Saving meta... Bytes length is " + metaProgressBytes.Length + " string length is " + metaString.Length);
        sdh.save(metaString, "metaprogress" + saveIndex + ".xml");

        string savedGameString = Encoding.UTF8.GetString(savedGameBytes);
        Debug.Log("Done! Saving game... Bytes length is " + savedGameBytes.Length + " string length is " + savedGameString.Length);
        sdh.save(savedGameString, "savedGame" + saveIndex + ".xml");

        Debug.Log("Done! Saving game... Bytes length is " + mapDataBytes.Length);

        string path = "savedMap" + saveIndex + ".dat";

        sdh.save_binary(mapDataBytes, path);

        Debug.Log("Save complete!");
#endif
    }

    public static void TryUploadSaveData(int saveIndex)
    {
        Debug.Log("Secret input method engaged! " + saveIndex);
        if (Time.time - timeOfLastFileUpload < MIN_DELAY_BETWEEN_UPLOADS)
        {
            return;
        }

        s_NexAsync = true;

        // Set the callback function to call when the send process is complete.
        PostDataCB = (NexPlugin.AsyncResult asyncResult, ulong dataId) =>
        {
            singleton.PostOrUpdateCallBack(asyncResult, " Failed the post of save data. ");
        };

        // Set the callback function to call when the update process is complete.
        UpdateDataCB = (NexPlugin.AsyncResult asyncResult) =>
        {
            singleton.PostOrUpdateCallBack(asyncResult, " Failed the update of save data. ");
        };

        timeOfLastFileUpload = Time.time;
        byte[] array = CreateByteArrayFromFilesInSlot(saveIndex);
        UploadData(array, 0);
    }

    // --------------------------------------------------------
    /// @brief  Callback function to call when the process for sending or updating save data is complete.
    ///
    /// @param[in] ret  Success or failure.
    /// @param[in] returnCode  Network error code.
    /// @param[in] returnCode  Return value.
    /// @param[in] message  Message.
    // --------------------------------------------------------
    private void PostOrUpdateCallBack(NexPlugin.AsyncResult asyncResult, string message)
    {
        Debug.Log("GameManagerScriptTV::PostOrUpdateCallBack End");
        Debug.Log(string.Format("ret             :{0}", asyncResult.IsSuccess()));
        Debug.Log(string.Format("NetworkErrorCode:{0}", asyncResult.netErrCode));
        Debug.Log(string.Format("ReturnCode      :0x{0:x}", asyncResult.returnCode));

        if (asyncResult.IsSuccess() != true)
        {
            // Display an error message when a network error occurs.
            Debug.LogError(asyncResult.netErrCode);
            
            Debug.Log(String.Format("result:{0}", asyncResult.ToString()));
#if UNITY_SWITCH
            if (!asyncResult.nnResult.IsSuccess())
            {
                nn.err.Error.Show(asyncResult.nnResult);
            }
#endif
            
            UninitNexAssetAll();
            return;
        }

        if (asyncResult.IsSuccess() == true)
        {
            Debug.Log("GameManagerScriptTV::PostOrUpdateCallBack:: asyncResult.IsSuccess() == true");
            s_NexAsync = false;
        }
        else
        {
            // Display an error message if a non-network error occurs.
            Debug.LogError(message);

            UninitNexAssetAll();
        }
    }

    // ---------------------------------------------------------
    ///
    /// @brief  Finalizes <tt>NexAssets</tt>.
    ///
    // ---------------------------------------------------------
    private void UninitNexAssetAll()
    {
        // Get the NexAssets.Common object.
        NexAssets.Common core = GameObject.Find("Lobby").GetComponent<NexAssets.Common>();
        if (core != null)
        {
            // Call UninitNexAssetAll with SendMessage.
            Debug.Log("UninitNexAssetAll Start");
            core.LogoutAsync(LogOutCallback);
        }
    }

    void LogOutCallback(NexPlugin.AsyncResult asyncResult)
    {
        NexAssets.Common core = GameObject.Find("Lobby").GetComponent<NexAssets.Common>();
        core.FinalizeNex();
        NexMemoryDump();
    }

    // ---------------------------------------------------------
    ///
    /// @brief  Performs a memory dump of <tt>NexAssets</tt> and <tt>NexPlugin</tt>.
    ///
    // ---------------------------------------------------------
    protected static void NexMemoryDump()
    {
        NexPlugin.Common.DumpMemory();
    }

}
#endif
*/