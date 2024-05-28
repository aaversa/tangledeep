using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif


/*
 *      Code here used for:
 * 
 *      Loading from AssetBundles
 *      Writing/Reading binary streamlined ability data
 * 
 */

public partial class GameMasterScript : MonoBehaviour
{
    //Buffer for loading TextAssets from assetbundles
    private TextAsset loadedAssetFromBundle;
    private static TDAssetBundleLoader bundleLoader;

    public static Dictionary<string, AssetBundle> loadedAssetBundles
    {
        get
        {
            if (bundleLoader == null)
            {
                
                bundleLoader = GameObject.Find("LoadedAssetBundles").GetComponent<TDAssetBundleLoader>();
                if (bundleLoader.loadedAssetBundles == null)
                {
                    bundleLoader.loadedAssetBundles = new Dictionary<string, AssetBundle>();
                    
                }
            }
            else
            {
                
            }
            
            return bundleLoader.loadedAssetBundles;
        }
    }

    bool assetBundleFunctionEntered = false;

    IEnumerator LoadAllAssetBundles()
    {
        if (assetBundleFunctionEntered)
        {
            Debug.Log("Entered LoadAllAssetBundles twice, serious error!");
        }
        assetBundleFunctionEntered = true;
        // For now, why are asset bundles not working?
#if UNITY_ANDROID || UNITY_IPHONE
        yield break;
#endif

        float fStartTime = Time.realtimeSinceStartup;
#if UNITY_STANDALONE_OSX
        string strPath = Application.dataPath + "/Resources/Data/StreamingAssets";
#elif UNITY_STANDALONE_LINUX
        string strPath = Application.streamingAssetsPath;
#elif UNITY_SWITCH
        //nn.fs.Rom.MountRom();
        string strPath = Application.streamingAssetsPath.Replace("/rom:/", "rom:/");

        //string test = GetStreamingPath();
#elif UNITY_PS4
        string strPath = Application.streamingAssetsPath;
#elif UNITY_XBOXONE
        string strPath = Application.streamingAssetsPath;
#elif UNITY_ANDROID
        string strPath = Application.streamingAssetsPath;
#else
        string strPath = Application.streamingAssetsPath;
#endif

        float timeAtLastPause = Time.realtimeSinceStartup;

        string titleBundlePath = Path.Combine(strPath, "title");
        string titleBundleName = GetBundleNameFromBundlePath(titleBundlePath);

        //if (Debug.isDebugBuild) Debug.Log("Try loading a specific asset bundle " + titleBundlePath);

        //first we must load the title screen bundle
		// This was commented out in switch version
        yield return LoadSpecificAssetBundle(titleBundlePath, titleBundleName);

        string[] strFiles = Directory.GetFiles(strPath);

        foreach (string strBundlePath in strFiles)
        {
            //Only get the bundles
            if (strBundlePath.Contains(".meta") ||
                strBundlePath.Contains("manifest") ||
				strBundlePath.Contains("nowloading") ||
				strBundlePath.Contains("_cn"))
            {
                continue;
            }

            // as of 1/4/2019 we only need the audio bundle on PC. ... Right?
            if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES && !strBundlePath.Contains("audio"))            
            {
                continue;
            }

            string strBundleName = GetBundleNameFromBundlePath(strBundlePath);

            if (loadedAssetBundles.ContainsKey(strBundleName)) continue;

            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBundlePath);

            if (Time.realtimeSinceStartup - timeAtLastPause >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }

            //yield return LoadSpecificAssetBundle(strBundlePath, strBundleName);
        }

        yield return TDAssetBundleLoader.BuildSpriteCollection();
#if UNITY_EDITOR
        Debug.Log("LoadAllAssetBundles end, total time: " + (Time.realtimeSinceStartup - fStartTime));
#endif
    }

    string GetStreamingPath()
    {
#if UNITY_SWITCH
        nn.fs.DirectoryHandle dirHandle = new nn.fs.DirectoryHandle();
        nn.Result result = nn.fs.Directory.Open(ref dirHandle, Application.streamingAssetsPath, nn.fs.OpenDirectoryMode.File);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            Debug.LogError("Directory not found: " + Application.streamingAssetsPath);
            return "";
        }
        long entryCount = 0;
        nn.fs.Directory.GetEntryCount(ref entryCount, dirHandle);
        nn.fs.DirectoryEntry[] dirEntries = new nn.fs.DirectoryEntry[entryCount];
        long actualEntries = 0;
        nn.fs.Directory.Read(ref actualEntries, dirEntries, dirHandle, entryCount);

        for (int i = 0; i < actualEntries; ++i)
        {
            //Debug.Log("Name is: " + dirEntries[i].name);
        }

        return "Done.";
#else
        return "Done.";
#endif

    }

    IEnumerator LoadSpecificAssetBundle(string strBundlePath, string strBundleName)
    {
        //if (Debug.isDebugBuild) Debug.Log("Trying to load " + strBundleName + " from path " + strBundlePath);
        if (loadedAssetBundles.ContainsKey(strBundleName))
        {
           //if (Debug.isDebugBuild) Debug.Log("Bundle " + strBundleName + " already loaded, not loading from " + strBundlePath);
            yield break;
        }

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {

            //#todo - help
            //yield return saveDataHandler.load_binary_file_async(strBundlePath);
            //AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(saveDataHandler.asyncLoadedByteArray);
            //yield return new WaitWhile(() => createRequest.isDone == false);
            //loadedAssetBundles[strBundleName] = createRequest.assetBundle;
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBundlePath);
        }
        else
        {
            AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes(strBundlePath));
            yield return createRequest;
            loadedAssetBundles[strBundleName] = createRequest.assetBundle;
        }
        
    }

    string GetBundleNameFromBundlePath(string strBundlePath)
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        //uh
        if( strBundleName == "Switch")
        {
            return strBundleName;
        }
#elif UNITY_STANDALONE_OSX
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
#elif UNITY_STANDALONE_LINUX
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
#elif UNITY_PS4 && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
#elif UNITY_XBOXONE && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('\\') + 1);
#elif UNITY_ANDROID && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
#else
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('\\') + 1);
#endif

        return strBundleName;
    }

}