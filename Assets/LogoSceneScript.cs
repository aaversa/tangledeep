using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;


public class LogoSceneScript : MonoBehaviour {

    public bool forceSolsticeMode;
    public static bool globalIsSolsticeBuild;
    public static bool globalSolsticeDebug;

    public static bool debugMusic;

    public bool localDebugMusic;

    public Switch_SaveDataHandler switchDataHandler;

    LogoSceneFader blackFadeImage;
    bool keyPressed = false;
    bool alreadyFading = false;

    bool initialized;

	// Use this for initialization
	void Awake ()
    {        
        if (initialized) return;

        Initialize();
	}

    void Initialize()
    {
        debugMusic = localDebugMusic;

        PlatformVariables.OnInitialLoad();

        string solsticeVariable = Environment.GetEnvironmentVariable("SOLSTICE_LAUNCH_MODE");
#if !UNITY_EDITOR
        forceSolsticeMode = false;
#else
        //solsticeVariable = "DEBUG";
        Debug.Log("Solstice variable is " + solsticeVariable + " and force is " + forceSolsticeMode);
#endif


        if (solsticeVariable == "RELEASE" || forceSolsticeMode)
        {
            globalIsSolsticeBuild = true;
        }
        else 
        {
            globalIsSolsticeBuild = false;
            if (solsticeVariable == "DEBUG")
            {
                globalSolsticeDebug = true;
            }
        }

        GraphicsAndFramerateManager.SetApplicationFPS(144);

        GameObject go = GameObject.Find("BlackFade");
        if (blackFadeImage == null) blackFadeImage = go.GetComponent<LogoSceneFader>();

        TDPlayerPrefs.Initialize();
        TDPlayerPrefs.Load();

        LoadingWaiterManager.Display();

        SetToBlack();

#if UNITY_SWITCH
        switchDataHandler.FirstAwakeOrInitialize();
#endif

        if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            StartCoroutine(WaiterToFade());
        }
        else
        {
            StartCoroutine(LoadNowLoadingObjectsViaAssetBundles());
        }

        Debug.ClearDeveloperConsole();
        Debug.developerConsoleVisible = false;

        initialized = true;

        if (globalIsSolsticeBuild)
        {
            DLCManager.MarkAllDLCsOwnedForSession();
            QualitySettings.vSyncCount = 1;
            SetInternalResolution();
        }                
    }

    private void Start()
    {
        if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            SetAllLoadingHeroPrefabs_NoBundles();
        }
        
    }

    void SetInternalResolution()
    {
        string strHeight = Environment.GetEnvironmentVariable("SOLSTICE_DISPLAY_RESOLUTION_HEIGHT");
        string strWidth = Environment.GetEnvironmentVariable("SOLSTICE_DISPLAY_RESOLUTION_WIDTH");

        int height = 1080;
        int width = 1920;
        bool resSuccess = false;

        if (int.TryParse(strHeight, out height))
        {
            if (int.TryParse(strWidth, out width))
            {
                Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                if (Debug.isDebugBuild) Debug.Log("Success!" + width + "x" + height);
                resSuccess = true;
            }
        }

        if (!resSuccess)
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen);
        }
        
    }
	
    void Update()
    {
        if (!initialized)
        {
            Initialize();
        }
        if (StringManager.initializedCompletely && Input.anyKeyDown && !keyPressed)
        {

            //on PS4/XBOXONE we are in Logos scene checking saves, if there is any issue, skipping to next scene could cause even more issues
#if UNITY_PS4 || UNITY_XBOXONE
            return;
#endif


            keyPressed = true;
            if (!alreadyFading)
            {
                FadeInBlackImage(0.5f);
                StartCoroutine(SwitchScenes());
            }            
        }
    }

    IEnumerator WaiterToFade()
    {
        //Debug.Log("Preparing to load strings");

        //detect language from hardware when LOAD_EVERYTHING_FROM_ASSET_BUNDLES is false
#if UNITY_PS4
        EGameLanguage lang = StringManager.GetLanguageForPS4(UnityEngine.PS4.Utility.systemLanguage.ToString());
        yield return StringManager.LoadAllStrings(lang);

        //SAVE
        //wait until we check, if save is corrupted (we are checking it at PS4SaveManager Start)
        yield return new WaitUntil(() => PS4SaveManager.instance.checkedIfSaveIsCorrupted);

#elif UNITY_XBOXONE && !UNITY_EDITOR
        EGameLanguage lang = StringManager.GetLanguageForXBOXONE(System.Globalization.CultureInfo.CurrentCulture.ToString());
        yield return StringManager.LoadAllStrings(lang);

        //USER
        FindObjectOfType<XboxUsersManager>().StartSequence();
        yield return new WaitUntil(() => XboxUsersManager.instance.userID != 0);
        Debug.Log("user loaded");

        //SAVE
        FindObjectOfType<XboxSaveManager>().StartSequence();
        yield return new WaitUntil(() => XboxSaveManager.fullyLoaded);
        Debug.Log("save loaded");
#elif UNITY_XBOXONE && UNITY_EDITOR
        EGameLanguage lang = EGameLanguage.en_us;
        //lang = EGameLanguage.zh_cn;
        yield return StringManager.LoadAllStrings(lang);
#elif UNITY_ANDROID
        EGameLanguage lang = StringManager.GetLanguageForAndroid(Application.systemLanguage.ToString());
        yield return StringManager.LoadAllStrings(lang);
#else
        yield return StringManager.LoadAllStrings();
#endif



        LoadingWaiterManager.Hide(0.1f);
        FadeOutBlackImage(0.5f);
        //LoadingWaiterManager.Hide(0.25f);

        float counter = 0f;
        while (counter < 3.2f && !keyPressed)
        {
            yield return null;
            if (!StringManager.initializedCompletely) continue;
            counter += Time.deltaTime;
        }       

        if (!keyPressed)
        {
            alreadyFading = true;
            FadeInBlackImage(0.5f);
            StartCoroutine(SwitchScenes());
        }
    }

    IEnumerator SwitchScenes()
    {
        float timeCounter = 0f;
        while (timeCounter < 0.25f)
        {
            yield return null;
            timeCounter += Time.deltaTime;
        }

        timeCounter = 0f;        
        LoadingWaiterManager.Display(0.25f);

        while (timeCounter < 0.26f)
        {
            yield return null;
            timeCounter += Time.deltaTime;
        }

        timeCounter = 0f;
        while (timeCounter < 0.25f)
        {
            yield return null;
            timeCounter += Time.deltaTime;
        }

        //if (Debug.isDebugBuild) Debug.Log("Request load main at " + Time.realtimeSinceStartup);
        SceneManager.LoadScene("Main");
    }

    public static bool AllowResolutionSelection()
    {
        if (!LogoSceneScript.globalIsSolsticeBuild) return true;

        if (LogoSceneScript.globalSolsticeDebug) return true;

        return false;
    }

    public static bool AllowFullScreenSelection()
    {
        if (!LogoSceneScript.globalIsSolsticeBuild) return true;

        if (LogoSceneScript.globalSolsticeDebug) return true;

        return false;
    }

    static void SetAllLoadingHeroPrefabs_NoBundles()
    {
        GameObject[] allHeroPrefabs = Resources.LoadAll<GameObject>("Jobs/LoadingPrefabs");
        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < allHeroPrefabs.Length; i++)
        {
            prefabs.Add(allHeroPrefabs[i]);
        }
        //if (Debug.isDebugBuild) Debug.Log("<color=green>Setting all loading hero prefabs.</color>");
        LoadingWaiterManager.SetPrefabsListForRunningHeroines(prefabs);
    }

    private IEnumerator LoadNowLoadingObjectsViaAssetBundles()
    {
        //start loading next scene first?

        float fStartTime = Time.realtimeSinceStartup;

        GameObject go = GameObject.Find("BlackFade");
        //show up
        if (blackFadeImage == null) blackFadeImage = go.GetComponent<LogoSceneFader>();
        SetToBlack();

        bool debug = false;

#if UNITY_EDITOR
        debug = false;
#endif

#if UNITY_SWITCH
        if (Debug.isDebugBuild)
        {
            debug = true;
        }
#endif


        //load up the strings
        yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath,
                "localization"));

        yield return null;

        //if (debug) Debug.Log("Strings loaded.");

            //pull up the required art
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath,
                "nowloading_assets"));

        yield return null;

        //now the prefabs
        yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath,
                "nowloading_object"));

        yield return null;

        //if (debug) Debug.Log("NowLoading bundles loaded.");

        //assemble the prefabs correctly
        float fSlowassTime = Time.realtimeSinceStartup;
            AssetBundle abTitleTimes = TDAssetBundleLoader.GetBundleIfExists("nowloading_object");
            var request = abTitleTimes.LoadAllAssetsAsync();
            yield return new WaitWhile(() => !request.isDone);

        //if (debug) Debug.Log("All assets loaded async.");

            //yeah while we're here, do this in the loading screen to spare us the stutter later.
            yield return TDAssetBundleLoader.CacheAssetsAndSpritesFromBundleCollection("nowloading_object", request.allAssets);

            //if (debug) Debug.Log("Finished caching assets and sprites at title screen.");

            List<GameObject> prefabsForLoader = new List<GameObject>();

            foreach (object o in request.allAssets)
            {
                var possiblePrefabInAssetBundle = o as GameObject;
                if (possiblePrefabInAssetBundle == null)
                {
                    continue;
                }
                //Debug.Log("Checking asset in loader: " + possiblePrefabInAssetBundle.name);
                if (possiblePrefabInAssetBundle.name.Contains("_LoadingScreen"))
                {
                    prefabsForLoader.Add(possiblePrefabInAssetBundle);
                    possiblePrefabInAssetBundle.GetComponent<Animatable>().GetSpritesFromMemory();
                }
                /* else if (yuigogo.name == "LoadingWaiter")
                {
                    GameObject waiter = Instantiate(yuigogo) as GameObject;
                    waiter.transform.SetParent(transform.parent);
                } */
            }

        //if (debug) Debug.Log("Went through request iteration");

            //Give Unity a frame to catch up with these objects and allow them to Start()
            yield return null;

            //chill until go time
            LoadingWaiterManager.SetPrefabsListForRunningHeroines(prefabsForLoader);

        //Shep: Detect language from hardware, then load it up
#if UNITY_SWITCH
        EGameLanguage lang = StringManager.GetLanguageFromNNLanguageCode(nn.oe.Language.GetDesired());
#elif UNITY_PS4
        EGameLanguage lang = StringManager.GetLanguageForPS4(UnityEngine.PS4.Utility.systemLanguage.ToString());

        //SAVE
        //wait until we check, if save is corrupted (we are checking it at PS4SaveManager Start)
        yield return new WaitUntil(() => PS4SaveManager.instance.checkedIfSaveIsCorrupted);
#elif UNITY_XBOXONE && !UNITY_EDITOR
        EGameLanguage lang = StringManager.GetLanguageForXBOXONE(System.Globalization.CultureInfo.CurrentCulture.ToString());

        //USER
        FindObjectOfType<XboxUsersManager>().StartSequence();
        yield return new WaitUntil(() => XboxUsersManager.instance.userID != 0);
        Debug.Log("user loaded");

        //SAVE
        FindObjectOfType<XboxSaveManager>().StartSequence();
        yield return new WaitUntil(() => XboxSaveManager.fullyLoaded);
        Debug.Log("save loaded");
#elif UNITY_XBOXONE && UNITY_EDITOR
        EGameLanguage lang = EGameLanguage.zh_cn;
#elif UNITY_ANDROID
        EGameLanguage lang = StringManager.GetLanguageForAndroid(Application.systemLanguage.ToString());
#else
        EGameLanguage lang = StringManager.TryGetGameLanguageFromPlayerPrefs();
#endif

        /* string langFromPrefs = PlayerPrefs.GetString("lang");
        if (!string.IsNullOrEmpty(langFromPrefs))
        {
            try
            {
                lang = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), langFromPrefs);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to parse game language from playerprefs due to " + e);
            }
        }
        StringManager.SetGameLanguage(lang); */


        // Debug for testing languages (override system settings0
        //Language debug languagedebug debuglang debuglanguage debug lang
        //lang = EGameLanguage.jp_japan;
        //lang = EGameLanguage.zh_cn;
        //lang = EGameLanguage.de_germany;
        //lang = EGameLanguage.es_spain;

        if (debug) Debug.Log("Preparing to load strings in language: " + lang);

        yield return StringManager.LoadAllStrings(lang);

        

        //if (debug) Debug.Log("Loaded all strings");

        FadeInBlackImage(0.5f);
        LoadingWaiterManager.Display(0.2f);

        //not really async
#if UNITY_SWITCH || UNITY_XBOXONE
        //if (debug) Debug.Log("Load scene async MAIN");
        AsyncOperation op = SceneManager.LoadSceneAsync("Main");
#endif
        //if this didn't take at least two seconds, keep waiting.
        yield return new WaitWhile(() => Time.realtimeSinceStartup - fStartTime < 2.0f);

        //wait for fade to finish, and give objects a chance to Start()
        yield return new WaitForSeconds(0.5f);

#if !UNITY_SWITCH && !UNITY_XBOXONE
            AsyncOperation op = TDSceneManager.LoadSceneAsync("Main");
#endif
            yield return new WaitWhile(() => !op.isDone);

        
    }

    void FadeOutBlackImage(float time)
    {
#if UNITY_PS4 //on PS4 we don't Fade Out/In, since it will just show logo a second time
        return;
#endif
        //Debug.Log("Fade OUT black image over " + time);
        blackFadeImage.FadeOut(time);
    }

    void FadeInBlackImage(float time)
    {
#if UNITY_PS4 //on PS4 we don't Fade Out/In, since it will just show logo a second time
        return;
#endif
        //Debug.Log("Fade IN black image over " + time);
        blackFadeImage.FadeIn(time);
    }

    void SetToBlack()
    {
        blackFadeImage.SetToBlack();
    }


}
