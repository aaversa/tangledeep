using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Reflection;
using System;

public partial class GameMasterScript
{
    void Awake()
    {
#if UNITY_SWITCH
        //UnityEngine.Switch.Performance.SetCpuBoostMode(UnityEngine.Switch.Performance.CpuBoostMode.FastLoad);
#endif
        TryReadCustomSeasonFromPlayerPrefs();

        //if (Debug.isDebugBuild) Debug.Log("Awakening GMS at " + Time.realtimeSinceStartup);

        BalanceData.Initialize();

        if(queueTempGameData == null) queueTempGameData = new Dictionary<string, Queue<int>>();
        queueTempGameData.Clear();

        queueTempStringData = new Dictionary<string, Queue<string>>();

        possibleItems = new List<Item>();

        dictEndOfTurnFunctions = new Dictionary<Action<string[]>, string[]>();
        dictTempGameData = new Dictionary<string, int>();
        dictTempIntData = new Dictionary<int, int>();
        trueTimeOfRecentTurns = new float[3];

        if (seasonsActive == null)
        {
            seasonsActive = new bool[(int)Seasons.COUNT];
#if UNITY_SWITCH
            if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Reset seasonsActive array");
#endif
        }

        bufferedHotbarActions = null;
        tdHasFocus = true;
        masterMouseBlocker = GameObject.Find("MasterMouseBlocker");
        masterMouseBlocker.SetActive(false);

        gmsSingleton = this;

#if UNITY_SWITCH
    Input_InitializeSwitchControllers();
#endif
        if (coreResources == null)
        {

            coreResources = new Dictionary<string, GameObject>(800);

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
            {
                resourcesToLoadAfterMainSceneThatWeUsedToPreload = new HashSet<string[]>();
            }			
            else 
            {
                resourcesQueuedForLoading = new Dictionary<string, string>();
            }
        }

        CustomAlgorithms.Init();
        if (titleScreenGMS)
        {
            StartCoroutine(DoTitleScreenLoader());
            loadFromXMLEnabled = bLoadFromXML;
            pretendSwitchEnabled = bPretendSwitchVersion;
            UIManagerScript.bForceHideCursor = false;
            return;
        }
        else
        {
            startJob = GameStartData.jobAsEnum;
            bLoadFromXML = loadFromXMLEnabled;
        }

        if (GameStartData.worldSeed != 0)
        {
            SetRNGSeed(GameStartData.worldSeed);
        }

        PlayerModManager.OnEnterGameplayScene();

        seasonsActive[(int)Seasons.HALLOWEEN] = seasonHalloweenEnabled;
        seasonsActive[(int)Seasons.LUNAR_NEW_YEAR] = lunarNewYearEnabled;

#if UNITY_SWITCH
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Halloween active? " + seasonHalloweenEnabled + " LNY? " + lunarNewYearEnabled);
#endif

        gmsSingleton = this;

        CustomAlgorithms.Init();
        MetaProgressScript.Initialize();
        CookingScript.Initialize();
        allLoadedNPCs = new List<NPC>();
        CreateOrClearStaticContainers();



if (PlatformVariables.FIXED_FRAMERATE)
{
    Application.targetFrameRate = 60;
}


        animationPlaying = false;
        animatingActorsPreventingUpdate = new HashSet<Actor>();
        coroutinesPreventingUpdate = new List<ImpactCoroutineWatcher>();


        /*s_ItemFilePath = Application.dataPath + "/Resources/Items/XML/";
        s_MonsterFilePath = Application.dataPath + "/Resources/Monsters/XML/";
        s_AbilityFilePath = Application.dataPath + "/Resources/Abilities/XML/";
        s_DungeonRoomFilePath = Application.dataPath + "/Resources/DungeonGenerator/XML/"; */

        // Data stuff

        // Maybe put this somewhere else.

        AssignAttributeAndElementStrings();

        bufferTargetData = new List<TargetData>();

        // Game begins
        heroPCActor = null;
        createdHeroPCActor = false;
        heroMovable = null;
        turnNumber = 0;
        gamePaused = false;
    }

    void Start()
    {
        pool_checkMTDs = new List<MapTileData>();
        if (loadingWaiter == null) loadingWaiter = GameObject.Find("LoadingWaiter");
        if (newLoadingCharacter == null) newLoadingCharacter = GameObject.Find("NewLoadingCharacter");       
        if (loadingFrog == null) loadingFrog = GameObject.Find("LoadingFrog");
        if (loadingFrog != null) loadingFrog.SetActive(false);
        if (newLoadingCharacter != null) newLoadingCharacter.GetComponent<Animatable>().SetAnimDirectional("Walk", Directions.EAST, Directions.EAST);


        if (titleScreenGMS)
        {
            LoadingWaiterManager.Hide();
            return;
        }

        GraphicsAndFramerateManager.OnStartLoad();

        UIManagerScript.loadingGroup = GameObject.Find("LoadingGroup");
        UIManagerScript.loadingBar = GameObject.Find("LoadingBar").GetComponent<Image>();

        string tipString = TutorialManagerScript.GetNextGameTip();

        UIManagerScript.WriteLoadingText(StringManager.GetString(tipString));
        UIManagerScript.TurnOnPrettyLoading(1.0f);

        UIManagerScript.FillLoadingBar(0f);

        // Random tip gets loaded here.

        LoadingWaiterManager.Display();

        StartCoroutine(FirstTimeLoadRoutine());
    }

    void TryReadCustomSeasonFromPlayerPrefs()
    {
        Seasons customSeasonVal = Seasons.COUNT;

#if UNITY_SWITCH
        customSeasonVal = SharedBank.customSeasonValue;
#else
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.CUSTOM_SEASON) == (int)Seasons.LUNAR_NEW_YEAR)
        {
            customSeasonVal = Seasons.LUNAR_NEW_YEAR;
        }
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.CUSTOM_SEASON) == (int)Seasons.HALLOWEEN)
        {
            customSeasonVal = Seasons.HALLOWEEN;
        }
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.CUSTOM_SEASON) == (int)Seasons.COUNT)
        {
            customSeasonVal = Seasons.NONE;
        }              
#endif

#if UNITY_SWITCH
        //if (Debug.isDebugBuild) Debug.Log("Checking CUSTOM_SEASON value. " + TDPlayerPrefs.GetInt(GlobalProgressKeys.CUSTOM_SEASON));
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG checking value at load is " + customSeasonVal);
#endif

        if (customSeasonVal == Seasons.LUNAR_NEW_YEAR)
        {
            lunarNewYearEnabled = true;
            if (seasonsActive != null)
            {
                seasonsActive[(int)Seasons.LUNAR_NEW_YEAR] = true;
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG seasonsActive is null though!");
                seasonsActive = new bool[(int)Seasons.COUNT];
                seasonsActive[(int)Seasons.LUNAR_NEW_YEAR] = true;
            }
            if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Season enabled on load: Lunar New Year");
        }
        if (customSeasonVal == Seasons.HALLOWEEN)
        {
            seasonHalloweenEnabled = true;
            if (seasonsActive != null) seasonsActive[(int)Seasons.HALLOWEEN] = true;
            else
            {
                if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG seasonsActive is null though!");
                seasonsActive = new bool[(int)Seasons.COUNT];
                seasonsActive[(int)Seasons.HALLOWEEN] = true;
            }
            if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Season enabled on load: Halloween");
        }
        if (customSeasonVal == Seasons.NONE)
        { 
            if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG int doesn't match so disabling everything.");
            seasonHalloweenEnabled = false;
            lunarNewYearEnabled = false;
            if (seasonsActive != null)
            {
                seasonsActive[(int)Seasons.HALLOWEEN] = false;
                seasonsActive[(int)Seasons.LUNAR_NEW_YEAR] = false;
            }
        }
    }
}