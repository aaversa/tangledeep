using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine.Events;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using Steamworks;
    using UnityEngine.Analytics;
    using LapinerTools.Steam.Data;
    using LapinerTools.uMyGUI;
    using System.Security.Cryptography;
#endif

using UnityEngine.UI;
using System.Text;
using TMPro;
using System.Threading;
using Rewired.ComponentControls.Data;
using System.Reflection;
using System.Runtime;

public partial class GameMasterScript
{


    bool TryPreloadResourceFromAssetBundleInstant(string key, string path)
    {
        if (coreResources.ContainsKey(key))
        {
            //that was easy!
            return true;
        }

        string strBundleName = "";
        string strAssetName = "";
        if (!path.Contains('/'))
        {
            //unsorted bundle
            strBundleName = "unsorted";
            strAssetName = path;
        }
        else
        {
            string[] splitPath = path.Split('/');
            strBundleName = splitPath[0].ToLowerInvariant();
            strAssetName = splitPath[splitPath.Length - 1];
        }

        if (!loadedAssetBundles.ContainsKey(strBundleName))
        {
            return false;
        }

        float fLoadTime = Time.realtimeSinceStartup;
        AssetBundle bun = loadedAssetBundles[strBundleName];
        GameObject go = bun.LoadAsset<GameObject>(strAssetName);
        if (go != null)
        {
            coreResources.Add(key, go);
        }
        else
        {
            //Debug.Log("Did not find " + key + " at " + path);
            return false;
        }

#if UNITY_EDITOR
        float fDelta = fLoadTime - Time.realtimeSinceStartup;
        if (fDelta > 0.008f)
        {
            Debug.Log("More than 8 ms spent 'instantly' loading asset '" + strAssetName + "': " + fDelta);
        }
#endif

        return true;
    }

    public bool TryPreloadResourceInstant(string key, string path)
    {
        // Success is true if:
        // * CoreResources already has this object
        // * Or we load it successfully
        // Success is FALSE if
        // * We cannot load the resource from the bundles
        bool success = TryPreloadResourceFromAssetBundleInstant(key, path);
        if (success)
        {
            return true;
        }

        bool bShouldLoad = true;
        if (String.IsNullOrEmpty(key))
        {
            Debug.Log("Cannot preload null key.");
            bShouldLoad = false;
        }

        if (coreResources.ContainsKey(key))
        {
            bShouldLoad = false;
        }

        if (bShouldLoad)
        {
            GameObject go = Resources.Load<GameObject>(path);
            if (go != null)
            {
                coreResources.Add(key, go);
            }
            else
            {

            }
        }
        return success;
    }

    IEnumerator TryPreloadResourceFromAssetBundle(string key, string path)
    {
        if (string.IsNullOrEmpty(key))
        {
            yield break;
        }

        if (coreResources.ContainsKey(key))
        {
            //that was easy!
            yield break;
        }

        string strBundleName = "";
        string strAssetName = "";
        if (!path.Contains('/'))
        {
            //unsorted bundle
            strBundleName = "unsorted";
            strAssetName = path;
        }
        else
        {
            string[] splitPath = path.Split('/');
            strBundleName = splitPath[0].ToLowerInvariant();
            strAssetName = splitPath[splitPath.Length - 1];
        }

        if (!loadedAssetBundles.ContainsKey(strBundleName))
        {
            yield break;
        }

        AssetBundle bun = loadedAssetBundles[strBundleName];
        AssetBundleRequest rr = bun.LoadAssetAsync(strAssetName);
        yield return new WaitWhile(() => !rr.isDone);

        //Didn't we already check this? Yes, BUT... 
        //some other coroutine may have added this resource while we were yielding to a load. Threading!
        if (coreResources.ContainsKey(key))
        {
            yield break;
        }

        GameObject go = rr.asset as GameObject;
        if (go != null)
        {
            coreResources.Add(key, go);
        }
    }

	// This is only if... we're not loading everything from bundles.
    public static bool TryPreloadResourceNoBundles(string key, string path)
    {
        if (!coreResources.ContainsKey(key))
        {
            GameObject loadedResource = Resources.Load<GameObject>(path);

            coreResources.Add(key, loadedResource);
            return false;
        }
        else
        {
            //Debug.Log("Already have resource " + key + " at " + path);
            return true;
        }

    }


    IEnumerator TryPreloadResource(string key, string path)
    {

        //check asset bundles first
        yield return TryPreloadResourceFromAssetBundle(key, path);
        //if we have it now, yay!
        if (coreResources.ContainsKey(key))
        {

            yield break;
        }
        Debug.LogError("Tried to load " + key + " from " + path + " in TryPreloadResource coroutine. The assetbundle loader ran, but I couldn't find it, so RIP.");

        bool bShouldLoad = true;

        if (String.IsNullOrEmpty(key))
        {
            Debug.Log("Cannot preload null key.");
            bShouldLoad = false;
        }

        if (coreResources.ContainsKey(key))
        {
            bShouldLoad = false;
        }

        if (bShouldLoad)
        {
            ResourceRequest loadedResource = Resources.LoadAsync(path);
            yield return loadedResource;

            //The resource could have been loaded and added while we were yielding
            if (!coreResources.ContainsKey(key))
            {
                coreResources.Add(key, loadedResource.asset as GameObject);
            }
        }
        else
        {
            yield break; // was null, but if we dont have to wait for anything then just keep going.
        }

    }


    public static object Debug_TestAssetBundleLoading(params string[] args)
    {
        Debug.Log("Calling TestAssetBundleLoading");
        gmsSingleton.StartCoroutine(gmsSingleton.LoadAbilitiesViaAssetBundle());

        return "Called successfully, check Unity debug log for details.";
    }

    private IEnumerator LoadAbilitiesViaAssetBundle()
    {
        Debug.Log("Attempting to load bundle at " + Time.time);
        //Grab the bundle of text files
        AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes("AssetBundles/Windows/abilities_data"));
        yield return createRequest;

        //Pull the text files out
        Debug.Log("Loading assets in bundle at " + Time.time);
        AssetBundle bun = createRequest.assetBundle;
        AssetBundleRequest requestForTextAssets = bun.LoadAllAssetsAsync<TextAsset>();
        yield return requestForTextAssets;



        //Read them in?
        for (int t = 0; t < requestForTextAssets.allAssets.Length; t++)
        {
            TextAsset txt = requestForTextAssets.allAssets[t] as TextAsset;
            if (txt == null)
            {
                continue;
            }

            Debug.Log("Asset bundled loaded text asset " + txt.name);
        }
    }
}