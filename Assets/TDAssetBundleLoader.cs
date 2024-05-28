using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = System.Object;

public class TDAssetBundleLoader : MonoBehaviour
{
    private TextAsset loadedAssetFromBundle;
    public Dictionary<string, AssetBundle> loadedAssetBundles;

    private static Dictionary<string, int> bullshitByteDictionary;

    private static TDAssetBundleLoader _instance;

    private static Dictionary<string, Sprite> spritesFromBundle;
    private static Dictionary<string, AudioClip> musicFromBundle;
    private static Dictionary<string, ScriptableObject_MusicData> smdFromBundle;

    private static Dictionary<string, Color32[][]> choppedTilesCollection;

    private static bool bLoadInProgress;
    private static bool bSpriteBundleReady;

    //many memory chewed up here
    private static Dictionary<string, Dictionary<string, UnityEngine.Object>> cachedUnpackedAssetBundles;

    static readonly List<string> bundleNamesToSkipForSpriteCaching = new List<string>()
    {
        "weaponboxes",
        "targetingline",
        "sfx",
        "localization",
        "localization_dlc1",
        "localization_dlc2",
        "fonts",
        "data_abilities",
        "data_championdata",
        "data_dialogs",
        "data_dungeon",
        "data_items",
        "data_jobs",
        "data_loottable",
        "data_magicmod",
        "data_mapobject",
        "data_monster",
        "data_npc",
        "data_room",
        "data_shop",
        "data_spawntable",
        "data_status"
    };


    public static void DebugPurgeTheXenos()
    {
        _instance._DebugPurgeTheXenos();
    }

    public void _DebugPurgeTheXenos()
    {
        foreach (var kvp in loadedAssetBundles)
        {
            kvp.Value.Unload(true);
        }
        loadedAssetBundles.Clear();

        foreach (var kvp_master in cachedUnpackedAssetBundles)
        {
            foreach (var kvp_child in kvp_master.Value)
            {
                Destroy(kvp_child.Value);
            }

            kvp_master.Value.Clear();
        }

        cachedUnpackedAssetBundles.Clear();

        foreach (var kvp in musicFromBundle)
        {
            Destroy(kvp.Value);
        }
        musicFromBundle.Clear();
        smdFromBundle.Clear();
        choppedTilesCollection.Clear();
    }

    // Use this for initialization
    void Start ()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        loadedAssetBundles = new Dictionary<string, AssetBundle>();
        bullshitByteDictionary = new Dictionary<string, int>();
        cachedUnpackedAssetBundles = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();
        cachedUnpackedAssetBundles["art"] = new Dictionary<string, UnityEngine.Object>();
        musicFromBundle = new Dictionary<string, AudioClip>();
        smdFromBundle = new Dictionary<string, ScriptableObject_MusicData>();
        choppedTilesCollection = new Dictionary<string, Color32[][]>();
        spritesFromBundle = new Dictionary<string, Sprite>();
        
    }

    /// <summary>
    /// Use when we need to load something via coroutine but have no updating object available.
    /// </summary>
    /// <param name="cc"></param>
    public static void RunThisCoroutineForMeOK(IEnumerator cc)
    {
        _instance.StartCoroutine(cc);
    }

    /// <summary>
    /// Clears the cache of unpacked assets, which will be Very Big and cause the GC to come visit us without question.
    /// </summary>
    public static void ClearCachedUnpackedAssetsAndInviteTheGCToDinner()
    {
        cachedUnpackedAssetBundles.Clear();
    }

    /// <summary>
    /// Looks to see if we've already LoadAssetAsync'd a bundle and stored the objects within.
    /// </summary>
    /// <param name="strBundle"></param>
    /// <param name="strAsset"></param>
    /// <returns></returns>
    public static GameObject GetCachedGOIfWeHaveIt(string strBundle, string strAsset)
    {
        if (!cachedUnpackedAssetBundles.ContainsKey(strBundle))
        {
            return null;
        }

        var dict = cachedUnpackedAssetBundles[strBundle];
        if (!dict.ContainsKey(strAsset))
        {
            return null;
        }

        return dict[strAsset] as GameObject;
    }

    public static AssetBundle GetBundleIfExists(string strBundleName)
    {
        if (_instance == null)
        {
#if UNITY_EDITOR
            Debug.Log("Null bundle instance when trying to load " + strBundleName);
#endif
            return null;
        }

        AssetBundle ab;
        _instance.loadedAssetBundles.TryGetValue(strBundleName, out ab);
        return ab;
    }

    public static IEnumerator LoadSpecificAssetBundle(string strBundlePath)
    {
        //if (Debug.isDebugBuild) Debug.Log("Request load bundle: " + strBundlePath);

#if UNITY_SWITCH && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        //uh
        if( strBundleName == "Switch")
        {
            yield break;
        }
#elif UNITY_STANDALONE_OSX
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        Debug.Log("Loading OSX asset bundle with name of " + strBundleName);
#elif UNITY_STANDALONE_LINUX
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        Debug.Log("Loading Linux asset bundle with name of " + strBundleName);
#elif UNITY_PS4 && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        Debug.Log("Loading PS4 asset bundle with name of " + strBundleName);
#elif UNITY_XBOXONE && !UNITY_EDITOR
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('\\') + 1);
        Debug.Log("Loading XBOXONE asset bundle with name of " + strBundleName);
#elif UNITY_ANDROID && !UNITY_EDITOR        
        Debug.Log(strBundlePath);
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('/') + 1);
        Debug.Log(strBundleName);
        Debug.Log("Loading ANDROID asset bundle with name of " + strBundleName);
#else
        strBundlePath = strBundlePath.ToLowerInvariant();
        string strBundleName = strBundlePath.Substring(strBundlePath.LastIndexOf('\\') + 1);
#endif
        //if (Debug.isDebugBuild) Debug.Log("Loading asset bundle with name of " + strBundleName);

        //check if already loaded -- but we'll check again after bLoadInProgress because the
        //thing we're loading right now could be this asset bundle!

        if (_instance.loadedAssetBundles.ContainsKey(strBundleName))
        {
            yield break;
        }
        //wait if there's a load aloadin'
        //yield return new WaitWhile(() => bLoadInProgress);

        //and look again
        if (_instance.loadedAssetBundles.ContainsKey(strBundleName))
        {
#if UNITY_EDITOR
            Debug.Log("We already loaded " + strBundleName);
#endif
            yield break;
        }

        //ok go
        bLoadInProgress = true;

#if UNITY_SWITCH

        //Debug.Log("Load Specific Asset Bundle Path: " + strBundlePath);
        //Debug.Log("Load Specific Asset Bundle Name: " + strBundleName);

        var saveDataHandler = Switch_SaveDataHandler.GetInstance();

        if (saveDataHandler == null) Debug.Log("Null save data handler? ");

        yield return saveDataHandler.LoadSwitchBinaryFileAsync(strBundlePath);

        #region Testing code i commented out.
        /* ===============================================================
        int iSize = saveDataHandler.asyncLoadedByteArray.Length;
        if (bullshitByteDictionary.ContainsKey(strBundlePath))
        {
            Debug.Log("fail: " + strBundlePath);
        }
        else
        {
            foreach (var kvp in bullshitByteDictionary)
            {
                if (kvp.Value == iSize)
                {
                    Debug.Log("AB " + kvp.Key + " is exactly as many bytes as " + strBundlePath + "...");
                }
            }

            bullshitByteDictionary[strBundlePath] = iSize;
        }
        ====================================================================  */
        #endregion

        AssetBundleCreateRequest createRequest = null;
        byte[] loadedBytes = null;

        //Debug.Log("Prepare to get bytes loaded for " + strBundleName);

        Switch_SaveDataHandler.GetBytesLoadedAsync(strBundlePath, ref loadedBytes);

        //Debug.Log("Got those bytes!");

        //did it not work? 
        if (loadedBytes == null)
        {
            //Debug.Log("Loaded bytes are null for " + strBundleName);
            bLoadInProgress = false;
            yield break;
        }
        
        createRequest = AssetBundle.LoadFromMemoryAsync(loadedBytes);

        if (Debug.isDebugBuild && createRequest == null) Debug.Log("Can't load from memory async.");

        float fWaitingTime = 0f;
        float fStartTime = Time.realtimeSinceStartup;
        
        while (createRequest.isDone == false)
        {
            if (Time.realtimeSinceStartup - fStartTime >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                fStartTime = Time.realtimeSinceStartup;
            }
            yield return null;
        }

        _instance.loadedAssetBundles[strBundleName] = createRequest.assetBundle;
#else
        //Debug.Log("Creating request for " + strBundleName);
        AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(File.ReadAllBytes(strBundlePath));
        yield return createRequest;
        //Debug.Log("Request finished for " + strBundleName);
        _instance.loadedAssetBundles[strBundleName] = createRequest.assetBundle;

#endif

        bLoadInProgress = false;
    }

    public static void AddAssetBundle(string strBundleName, AssetBundle bun)
    {
        foreach (var kvp in _instance.loadedAssetBundles)
        {
            if (kvp.Key == strBundleName || kvp.Value == bun)
            {
                Debug.LogError("Why do you hate america? You are loading bundle " + strBundleName +
                               " but it already lives in your dictionary as " + kvp.Key + ".");

            }
        }
        _instance.loadedAssetBundles[strBundleName] = bun;
    }

    /// <summary>
    /// Once we've loaded every asset bundle, go through and put all the sprites we can grab into
    /// a hashset. This will let us reference them from memory instead of from disk which is very
    /// important when you are mid-game and the loader wants to STALL for 4 seconds before trying to 
    /// load up foom.png or whatever.
    /// </summary>
    public static IEnumerator BuildSpriteCollection()
    {
        //if (Debug.isDebugBuild) Debug.Log("<color=green>BUILDING THE SPRITE COLLECTION</color>");

        //plz don't do this more than once
        if (bSpriteBundleReady)
        {
            //if (Debug.isDebugBuild) Debug.Log("but we already did it...");
            yield break;
        }

        //stop the music loader so we don't collide on loadassetsasync
        MusicManagerScript.bPauseLoadAllMusicCoroutine = true;

        //this is the worst, but I don't right now know how to check to see if some other bundle is being loaded.
        //it's a guessing game, imprecise, and bad. 

        string strLongChainOfFails = "";
        var bundleList = new List<AssetBundle>();
        foreach (var kvp in _instance.loadedAssetBundles)
        {
            //Debug.Log("Consider caching sprites in " + kvp.Key);
            if (kvp.Value == null)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogError("Loaded a null asset bundle for " + kvp.Key);
                }
                else
                {
                    Debug.Log("Loaded a null asset bundle for " + kvp.Key);
                }

                
                continue;
            }
            //try to avoid pulling up the fonts as sprites
            if (kvp.Value.name.Contains("nowloading_assets") ||
                kvp.Value.name.Contains("nowloading_object") ||     //full of job sprites, and cached earlier.
                kvp.Value.name.Contains("localization") ||
                kvp.Value.name.Contains("music") ||
                kvp.Value.name.Contains("sfx") ||
                kvp.Value.name.Contains("fonts") ||
                kvp.Value.name.Contains("data_") ||
                kvp.Value.name.Contains("titlescreen_object") ||
                kvp.Value.name.Contains("Standalone"))
            {
                continue;
            }

            bundleList.Add(kvp.Value);
        }

        float fBunStartTime = Time.realtimeSinceStartup;
        float timeAtLastPause = fBunStartTime;

        foreach (var bun in bundleList)
        {
            //Debug.Log("Consider again caching sprites in " + bun.name);

            float cacheTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
            //UnityEngine.Profiling.Profiler.BeginSample("Opening sprite asset " + bun.name);
            //UnityEngine.Profiling.Profiler.EndSample();
#endif

            var request = bun.LoadAllAssetsAsync();
            //Debug.Log("Finished loading all assets async in " + bun.name);
            yield return new WaitWhile(() => !request.isDone);

            if (!bundleNamesToSkipForSpriteCaching.Contains(bun.name))
            {
                yield return CacheAssetsAndSpritesFromBundleCollection(bun.name, request.allAssets);
            }

            if (Time.realtimeSinceStartup - timeAtLastPause >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }
            //if (Debug.isDebugBuild) Debug.Log("Cache time: " + (Time.realtimeSinceStartup - cacheTime) + " for " + bun.name);
        }

        //allow the music loader to resume.
        MusicManagerScript.bPauseLoadAllMusicCoroutine = false;


        //now we have EVERY SPRITE omg
        if (strLongChainOfFails != "")
        {
#if UNITY_EDITOR
            Debug.LogError("Sprite load failure, these keys were duplicated: " + strLongChainOfFails);
#endif
        }

        /* if (Debug.isDebugBuild)
        {
            Debug.LogError("Done cache assets! It took " + (Time.realtimeSinceStartup - fBunStartTime) + " seconds.");
        } */

#if UNITY_EDITOR
        //Debug.Log("<color=green>All asset caching is complete. Sprite bundles are ready!</color>");
#endif


        bSpriteBundleReady = true;
    }

    static List<string> cachedAssetsAndSprites;

    public static IEnumerator CacheAssetsAndSpritesFromBundleCollection(string strBundleName, UnityEngine.Object[] allAssets)
    {
        if (cachedAssetsAndSprites == null) cachedAssetsAndSprites = new List<string>();

        if (cachedAssetsAndSprites.Contains(strBundleName))
        {
            //if (Debug.isDebugBuild) Debug.Log("Already cached " + strBundleName);
            yield break;
        }

        cachedAssetsAndSprites.Add(strBundleName);

        //if (Debug.isDebugBuild) Debug.Log("Cache sprites from bundle " + strBundleName);
        if (spritesFromBundle == null)
        {
            spritesFromBundle = new Dictionary<string, Sprite>();
        }

        //cache all these, because later when we load static assets,
        //Wouldn't It Be Nice (tm) if we didn't have to call LoadAllAssetsAsync over and over again.
        // ...
        //especially for ART
        Dictionary<string, UnityEngine.Object> cacheDict = null;
        if (strBundleName == "art" || strBundleName.Contains("art_"))
        {
            cacheDict = cachedUnpackedAssetBundles["art"];
        }
        else
        {
            cachedUnpackedAssetBundles[strBundleName] = new Dictionary<string, UnityEngine.Object>();
            cacheDict = cachedUnpackedAssetBundles[strBundleName];
        }

        float fDelayTime = Time.realtimeSinceStartup;

        foreach (var o in allAssets)
        {
            if (fDelayTime - Time.realtimeSinceStartup > (GameMasterScript.MIN_FPS_DURING_LOAD * 2f))
            {
                yield return null;
                fDelayTime = Time.realtimeSinceStartup;
            }

            var s = o as Sprite;
            if (s != null)
            {
                spritesFromBundle[s.name] = s;
                //Debug.Log("Cached sprite " + s.name);
            }
            else if (o is GameObject)
            {
                //cache this even if it is not a sprite.
                // but do we need to? we're not using it for anything. 2/2/2020
                cacheDict[o.name] = o;
            }
            else
            {
                //Debug.Log("Object in bundle is not a sprite, so we can't cache it.");
            }
        }

        //Debug.Log("DONE caching sprites from bundle " + strBundleName);
    }

    /// <summary>
    /// Returns an audio clip that we've already opened via LoadAssetAsync
    /// </summary>
    /// <param name="clipname">the name we used to cache the clip</param>
    /// <param name="smd">Cached music data that we'll return if we have it.</param>
    /// <returns>The clip, if we have it, and null otherwise</returns>
    public static AudioClip GetCachedAudioClip(string clipname, ref ScriptableObject_MusicData smd)
    {
        if (musicFromBundle.ContainsKey(clipname))
        {
            smd = smdFromBundle[clipname];
            return musicFromBundle[clipname];
        }

        return null;
    }

    /// <summary>
    /// Stores a clip we've loaded from an assetbundle so we don't need to LoadAssetAsync again. 
    /// </summary>
    /// <param name="clipname"></param>
    /// <param name="clip"></param>
    /// <param name="smd"></param>
    public static void CacheAudioClip(string clipname, AudioClip clip, ScriptableObject_MusicData smd)
    {
        musicFromBundle[clipname] = clip;
        smdFromBundle[clipname] = smd;
        if (clip != null)
        {
            if (!MusicManagerScript.allMusicTrackLengths.ContainsKey(clipname))
            {
                MusicManagerScript.allMusicTrackLengths.Add(clipname, clip.samples);
            }
        }
    }


    /// <summary>
    /// Keeps track of a collection of chopped tiles, usually for terrain or minimap
    /// </summary>
    /// <param name="tilename"></param>
    /// <param name="tiles"></param>
    public static void CacheChoppedTilesCollection(string tilename, Color32[][] tiles)
    {
        choppedTilesCollection[tilename] = tiles;
    }


    /// <summary>
    /// Grabs a pre-choppalopped collection of tiles, if we have it.
    /// </summary>
    /// <param name="tilename"></param>
    /// <returns></returns>
    public static Color32[][] GetCachedChoppedTilesCollection(string tilename)
    {
        if (choppedTilesCollection.ContainsKey(tilename))
        {
            return choppedTilesCollection[tilename];
        }

        return null;
    }

    /// <summary>
    /// Once all bundles are loaded, and the dictionary of sprites is made,
    /// we are free to hand out sprites to those who request them.
    /// </summary>
    /// <param name="spriteName"></param>
    /// <returns>sprite 4u</returns>
    public static Sprite GetSpriteFromMemory(string spriteName)
    {
        if (!bSpriteBundleReady)
        {
            //This is allowed to happen, even though it's not the best. But we can't throw a warning every time because
            //there are realities to how assetbundles get loaded.
            if (Debug.isDebugBuild)
            {
                //Debug.LogError("Sprite Request! '" + spriteName + "' was requested before the bundles were finished. Oh no.");
            }
            return null;
        }


        Sprite returnSpr = null;
        if (!spritesFromBundle.TryGetValue(spriteName, out returnSpr))
        {
#if UNITY_EDITOR
            Debug.LogError("Sprite " + spriteName + " is not in memory.");
#endif

#if UNITY_SWITCH
        if (Debug.isDebugBuild) Debug.LogError("Sprite " + spriteName + " is not in memory.");
#endif
            returnSpr = Resources.Load<Sprite>(spriteName);
            if (returnSpr == null)
            {
                Debug.LogError("Could not find sprite " + spriteName);
            }
            spritesFromBundle.Add(spriteName, returnSpr);
        }

        return returnSpr;
    }

    /// <summary>
    /// Grabs all the textassets from a given assetbundle and places them into the array you provide.
    /// The list is a reference so we can get around the coroutine limitation of not allowing
    /// return values or ref/out parameters.
    /// </summary>
    /// <param name="strBundleName">Name of the assetbundle -- not the path!</param>
    /// <param name="txtList">Shining pile of results.</param>
    /// <returns>it is a coroutine</returns>
    public static IEnumerator LoadTextAssetsIntoArray(string strBundleName, List<TextAsset> txtList)
    {
        float fTime = Time.realtimeSinceStartup;

        //grab the bundle -- costs no time if already loaded.
        yield return LoadSpecificAssetBundle(strBundleName);
        var bun = GetBundleIfExists(strBundleName);

        //grab the assets
        var request = bun.LoadAllAssetsAsync<TextAsset>();
        yield return new WaitWhile(() => !request.isDone);

        //poof
        foreach (var a in request.allAssets)
        {
            txtList.Add(a as TextAsset);
            //Debug.Log(a.name + " from " + strBundleName);
        }

        //Debug.Log("Loading " + strBundleName + " from asset bundle took " + (Time.realtimeSinceStartup - fTime) + "s and loaded " + txtList.Count + " values.");

    }
}
