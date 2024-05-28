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

#if UNITY_SWITCH
	using Rewired.Platforms.Switch;
#endif

public enum StatDataTypes { TRUEMAX, MAX, CUR, ALL, COUNT };
public enum TurnTypes { MOVE, PASS, ATTACK, ABILITY, ITEM, REST, CONTINUE, COUNT };
public enum ActedStates { ACTED, NOTACTED, SKIP }
public enum GameModes { NORMAL, ADVENTURE, HARDCORE, COUNT }
public enum KeyboardControlMaps { DEFAULT, WASD, NOTSET, COUNT }
public enum Seasons { NONE, HALLOWEEN, LUNAR_NEW_YEAR, COUNT }
public enum JoystickControlStyles { STANDARD, STEP_MOVE, MAX };

public enum LoadStates { NORMAL, MAP_MISMATCH, PLAYER_VICTORY, BACK_TO_TITLE, RESTART_SAME_CHARACTER, PLAYER_VICTORY_NGPLUS, PLAYER_VICTORY_NGPLUSPLUS, COUNT }
public enum GameLoadingState { NOT_LOADED, LOADING, LOADED, BUSTED, ATTEMPTING_REBUILD_FIX, HOPELESS, MAX }

[System.Serializable]
public partial class GameMasterScript : MonoBehaviour
{
    static int GlobalLoadThreadIndex;
    public static int STEAM_APP_ID = 628770;
    public static bool applicationQuittingOrChangingScenes;

    public static GameObject goPooledObjectHolder;

    [Header("Debug")]
    public int gameRandomSeed = 0;
    public bool bSkipTitleJustStart;
    public int iSkipTitleLoadSlot;
    public bool bLoadFromXML = true;
    [Tooltip("Fakes Nintendo Switch mode for testing UI changes in Editor")]
    public bool bPretendSwitchVersion;

    static bool loadFromXMLEnabled; // Set this to the singleton value on the title screen and leave it that way
    public static bool pretendSwitchEnabled;

    public bool equipmentDebug;

    public static bool switchedInputMethodThisFrame;

    [Header("Seasonal")]
    public bool seasonHalloweenEnabled;
    public bool lunarNewYearEnabled;


    public static GameLoadingState gameLoadingState { get; set; }

    static HotbarBindable[] bufferedHotbarActions;

    public static bool initialGameAwakeComplete;
    public static bool allResourcesLoaded;
    bool resourcePoolsCreated;
    public static bool allAbilitiesLoaded;
    public static bool allJobsLoaded;

    public bool turnExecuting;
    public SteamStatsAndAchievements statsAndAchievements;

    GameObject masterMouseBlocker;
    Vector3 lastMousePosition;

    public GameModes gameMode;
    public static GameObject canvasObject;
    public static bool endingItemWorld;
    public static int framesToLoadGame;
    public static int checkMoveIndex = 0;
    private bool firstUpdate;
    public bool titleScreenGMS;
    public bool turnWasStopped; // Due to stun, root, etc?    
    public static CharacterJobs startJob;
    public CasinoScript theCasino;

    public static bool mapsLoadedSuccessfully; // Set by MapMasterScript during the load coroutine.

    public static bool levelChangeInProgress;
    public bool creditsActive;

    public static bool debug_freezeMonsters;
    public static bool debug_neverDie;
    public static int debug_freezeAllButThisID;

    private int iBadDataDuringLoadComboChain;

    public ControlMapper cMapper;

    public Player player;

    public float itemWorldOrbDropChance;
    public float specificModOrbDropChance;
    public float classModOrbDropChance;
    public float seedsDropChance;

    public static bool[] seasonsActive;
    public static float timeAtGameStartOrLoad;

    int framesSinceCleanedMouseWalk;
    static List<Stairs> loadGame_stairsToTryRelinking;
    static bool loadGame_itemWorldPortalOpen;
    static bool loadGame_inProgress;
    public static bool loadGame_creatingResourcePools;

    public static TurnData bufferedTurnData; // We may need to wait for player confirmation before continuing a turn.

    public int maxEquippedPassives;

    public static bool playerStatsChangedThisTurn;

    public static Monster genericMonster;

    public static bool actualGameStarted;
    public static bool gameLoadSequenceCompleted;
    public static bool playerIsResting = false;
    public static bool playerIsScavenger = false;
    public bool firstTurnRefresh = false;
    public static bool playerDied = false;
    public static bool returningToTownAfterKO = false;
    public int abilityRepetition = 0;

    public bool realTimeMode = false;

    // Game constants
    public float levelTransitionTime;
    public float playerMoveSpeed;
    public float visionFadeTime;
    public int globalOutOfCombatLimit;

    // Hero objects
    public static GameObject heroPC;
    public static HeroPC heroPCActor;
    public static bool createdHeroPCActor;
    private Movable heroMovable;

    public static int allActorIDs;
    public static Dictionary<int, Actor> dictAllActors;
    public static List<NPC> allLoadedNPCs;
    public static List<Actor> deadActorsToSaveAndLoad;
    public static int allAbilityIDs;

    private bool gamePaused;

    private bool heroObjectCreated;

    public static float turnSpeed = 100.0f;
    //public float joystickDeadZone;
    public float movementInputDelayTime;
    public const float movementMouseDelayTime = 0.18f;
    public float movementInputOptionsTime;
    public float movementFirstDelayTime;
    public float globalLootDropChance;
    public float globalMagicItemChance;
    public float globalPowerupDropChance;
    public float globalSacrificeBonusChance;

    public float attackAnimationTime;
    public static float baseAttackAnimationTime;
    public float statusRotationTime;

    public static int turnNumber;

    public bool tdHasFocus;

    private bool animationPlaying;
    bool animationFromCutscene;
    private HashSet<Actor> animatingActorsPreventingUpdate;
    private List<ImpactCoroutineWatcher> coroutinesPreventingUpdate;


    public static CameraController cameraScript;

    // Map master
    public static MapMasterScript mms;

    // Combat master
    public static CombatManagerScript combatManager;

    // Music manager
    public static MusicManagerScript musicManager;
    public static UIManagerScript uims;

    static AbilityScript realAbilToTry;
    private static AbilityScript abilityToTry
    {
        get
        {
            return realAbilToTry;
        }
        set
        {
            realAbilToTry = value;
            /* if (value != null)
            {
                Debug.Log("Switched to " + value.myID + " of course");
            } */
        }
    }
    public static AbilityScript GetAbilityToTry()
    {
        return abilityToTry;
    }
    static Consumable itemToUse;
    public static AbilityScript unmodifiedAbility;

    public static GameMasterScript gmsSingleton;

    public TextAsset[] itemXML;
    public TextAsset[] spawnTableXML;
    public TextAsset[] monsterXML;
    public TextAsset[] abilityXML;
    string[] abilityXMLText;
    public TextAsset[] stringXML;
    public TextAsset[] roomXML;
    public TextAsset[] statusXML;
    public TextAsset[] magicmodXML;
    public TextAsset[] jobXML;
    public TextAsset[] mapObjectXML;
    public TextAsset[] dungeonXML;
    public TextAsset[] championDataXML;
    public TextAsset[] npcXML;
    public TextAsset[] dialogXML;
    public TextAsset[] lootTableXML;

    public static Dictionary<string, Item> masterItemList;
    public static List<Equipment> listDuringLoadOfEqInGearSets;
    public static List<Item> itemsAutoAddToShops;
    public static List<Item> temp_itemsAddedToDictDuringLoad;
    public static List<Actor> temp_actorsAddedToDictDuringLoad;
    public static List<GearSet> temp_gearSetsAddedToDictDuringLoad;
    public static List<Recipe> temp_recipesAddedToDictDuringLoad;
    public static bool temp_errorDuringItemLoad;

    public static List<GearSet> masterGearSetList;
    public static List<Item> masterFoodList;
    public static List<Item> masterTreeFoodList;
    public static List<Consumable> masterConsumableList;
    public static Dictionary<string, MonsterTemplateData> masterMonsterList;
    public static List<MonsterTemplateData> monstersInPedia;
    public static List<MonsterTemplateData> sharaModeOnlyMonsters;
    public static List<MonsterFamily> masterFamilyList;
    public static List<string> monsterFamilyList;
    public static List<MonsterTemplateData> masterSpawnableMonsterList;
    public static Dictionary<string, AbilityScript> masterAbilityList;
    public static Dictionary<string, AbilityScript> masterSharaPowerList;
    public static Dictionary<string, AbilityScript> masterUniqueSharaPowerList;
    public static Dictionary<string, EffectScript> masterEffectList;
    public static Dictionary<string, RoomTemplate> masterDungeonRoomlist;
    public static List<RoomTemplate>[] masterDungeonRoomsByLayout;
    public static Dictionary<string, StatusEffect> masterStatusList;
    public static Dictionary<string, MagicMod> masterMagicModList;
    public static Dictionary<int, MagicMod> dictMagicModIDs;
    public static Dictionary<MagicModFlags, List<MagicMod>> dictMagicModsByFlag;
    public static List<MagicMod> listModsSortedByChallengeRating;
    public static List<CharacterJobData> masterJobList;
    public static List<CreationFeat> masterFeatList;
    public static Dictionary<string, Destructible> masterMapObjectDict;
    public static Dictionary<string, Destructible> masterSpawnableMapObjectList;
    public static Dictionary<int, DungeonLevel> masterDungeonLevelList;
    public static List<DungeonLevel> allDungeonLevelsAsList;
    public static Dictionary<float, List<DungeonLevel>> itemWorldMapDict;
    public static List<DungeonLevel> itemWorldMapList;
    public static Dictionary<string, Conversation> masterConversationList;
    public static List<Conversation> masterJournalEntryList;
    public static List<MonsterQuip> masterMonsterQuipList;
    public static List<Conversation> masterTutorialList;
    public static Dictionary<string, ChampionData> masterChampionDataDict;
    public static Dictionary<string, ChampionMod> masterChampionModList;
    public static List<ChampionMod> masterShadowKingChampModList;
    public static List<ChampionMod> masterMemoryKingChampModList;
    public static Dictionary<string, NPC> masterNPCList;
    public static List<NPC> masterCampfireNPCList;
    public static Dictionary<string, ActorTable> masterShopTableList;
    public static Dictionary<string, ActorTable> masterSpawnTableList;
    public static Dictionary<string, ShopScript> masterShopList;
    public static Dictionary<string, ActorTable> masterLootTables;
    public static Dictionary<string, ActorTable> dictAllActorTables;
    public static ActorTable masterBreakableSpawnTable;
    public static ActorTable tableOfLootTables;

    public static AbilityScript petAttackAbilityDummy;
    public static AbilityScript rangedWeaponAbilityDummy;
    public static EffectScript tossProjectileDummy;
    public static AbilityScript regenFlaskAbility;
    public static AbilityScript escapeTorchAbility;
    public static Weapon kickDummy;
    public static Fighter theDungeonActor;
    public static SummonActorEffect spellshiftMaterializeTemplate;
    public static AddStatusEffect vitalPointPainTemplate;
    public static AddStatusEffect vitalPointBleedTemplate;
    public static AddStatusEffect vitalPointExplodeTemplate;
    public static AddStatusEffect vitalPointParalyzeTemplate;
    public static SummonActorEffect brigandBomberTemplate;
    public static List<EffectScript> spellshaperEvocationEffects;
    public static CharacterJobData monsterJob;

    public static string[] characterJobNames;
    public static string[] elementNames;

    public static bool[] simpleBool;
    public static int[] boolToInt;
    private int[] randomSign;

    public static Item itemBeingUsed;

    // Used for targeting
    public static Vector2 bufferedLandingTile;
    public static List<TargetData> bufferTargetData;
    public static AbilityScript originatingAbility;
    public static List<EffectScript> localTurnEffectsFromPlayer;
    public static int processBufferTargetDataIndex = 0;
    public static bool playerAttackedThisTurn = false;
    public static bool changePlayerTimerThisTurn = false;
    public static bool playerMovingAnimation = false;

    public static Queue<Actor> deadQueue;
    public static string[] monsterAttributeNames;
    public static bool jobChangeFromNPC;
    public static List<string> itemWorldMonsterDeathLines;

    public string strEndColor = "</color>";
    public string startColor = "<color=";

    // Game help and tutorial stuff

    public static TutorialManagerScript tutorialManager;

    // End game help and tutorial

    // Pooling / gc management
    public StringBuilder reusableStringBuilder;
    static List<Monster> deadMonstersToRemove;
    public static List<Actor> dtTileActors;
    public static List<MapTileData> pool_checkMTDs;
    static List<Monster> pool_monsterList;
    static List<Actor> pool_removeList;
    static List<Actor> pool_targetList;
    public static List<MapTileData> pool_MTD;
    public static List<Actor> actorListCopy;
    static List<Actor> dtActors;
    static List<Fighter> actorsThatDoStuff;
    static List<Actor> currentMapActors;
    static List<Vector2> dtSpreadPositions;
    static List<Actor> allTargetable;
    static List<Actor> affectedActors;
    static List<CombatResult> cResults;

    static HashSet<string> pooledObjectsWithSpriteEffects;
    static HashSet<string> pooledObjectsWithAnimatables;
    static HashSet<string> pooledObjectsWithMovables;
    public static HashSet<string[]> resourcesToLoadAfterMainSceneThatWeUsedToPreload;
    private string[] kStrHotbarButtonIdx;
    // Pre-loading for resources
    public static Dictionary<string, GameObject> coreResources;

    Dictionary<string, string> resourcesQueuedForLoading;

    public static Material spriteMaterialLit;
    public static Material spriteMaterialUnlit;
    public static Material spriteMaterial_DestructiblesLit;
    public static Material spriteMaterial_DestructiblesUnLit;
    public static Material spriteMaterialHologram;
    public static Material spriteMaterialGreyscale;
    public static Material spriteMaterialFloorSlime;

    Dictionary<string, Stack<GameObject>> dictObjectPools;
    public static List<Item> possibleItems;

    // Pre-computed
    public static int[] maxJPAllJobs;

    // Various important game data here
    // Such as information about the town
    // Special merchant / wandering merchant
    public float wanderingMerchantChance;
    public NPC wanderingMerchantInTown;
    public int durationOfWanderingMerchant;

    public bool adventureModeActive;
    public float[] trueTimeOfRecentTurns;

    public int globalUniqueItemID; // used for reading items from XML.

    GameObject newLoadingCharacter;
    GameObject loadingFrog;
    public static GameObject loadingWaiter;
    public TextMeshProUGUI newLoadingText;
    
    public static List<string> allColorTags;

    public static void SetRNGSeed(int value)
    {
        gmsSingleton.gameRandomSeed = value;
        UnityEngine.Random.InitState(value);
        MyExtensions.InitRNG(value);

        //if (Debug.isDebugBuild) Debug.Log("<color=green>Set RNG seed to " + value + "</color>!");
    }

    public static List<Vector2> GetAllBufferedTargetTiles()
    {
        List<Vector2> tiles = new List<Vector2>();
        foreach (TargetData td in bufferTargetData)
        {
            foreach (Vector2 tile in td.targetTiles)
            {
                if (!tiles.Contains(tile))
                {
                    tiles.Add(tile);
                }
            }
        }
        return tiles;
    }

    public static GameObject GetHeroObject()
    {
        return heroPC;
    }

    public static HeroPC GetHeroActor()
    {
        return heroPCActor;
    }

    void GetStringsFromAbilityXML()
    {
        if (abilityXMLText == null)
        {
            abilityXMLText = new string[abilityXML.Length];
            for (int i = 0; i < abilityXMLText.Length; i++)
            {
                abilityXMLText[i] = abilityXML[i].text;
            }
        }
        else
        {
            Debug.Log("Ability XML Text already created.");
        }
    }
    
    void CacheStaticMaterial(ref Material staticMat, string matName)
    {
        if (staticMat == null)
        {
            staticMat = Instantiate(Resources.Load<Material>(matName));

            //This value ensures that the renderQueue value is used to determine rendering without being
            //overruled by z-positioning.
            staticMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
    }

    // Use this for initialization

    //This adds an corotuine to a watch list, which will be removed when it is null.
    //Until then, the game will not update. 
    public static ImpactCoroutineWatcher StartWatchedCoroutine(IEnumerator routine)
    {
        ImpactCoroutineWatcher newWatcher = new ImpactCoroutineWatcher();
        //keep an eye on this, and when it is done remove it from the list 
        gmsSingleton.coroutinesPreventingUpdate.Add(newWatcher);

        gmsSingleton.StartCoroutine(newWatcher.StartCoroutine(routine));


        return newWatcher;
    }

    public static void StopWatchedCoroutine(string strCoroutineName)
    {
        gmsSingleton.coroutinesPreventingUpdate.ForEach(cr =>
        {
            if (cr.GetCoroutineName().Contains(strCoroutineName))
            {
                cr.StopCoroutine();
            }
        });
    }

    public static bool ShouldRunSwitchCode()
    {
#if UNITY_SWITCH
    return true;
#elif UNITY_EDITOR
        return pretendSwitchEnabled;
#endif
        return false;
    }

    // Update is called once per frame
    void Update()
    {
if (!PlatformVariables.GAMEPAD_ONLY)
{
        if (SteamScript.steamOverlayActive) return;

        if (!titleScreenGMS && gameLoadSequenceCompleted && cMapper.isOpen)
        {
            Cursor.visible = true;
        }
        else
        {
            if (masterMouseBlocker.activeSelf)
            {
                if (Input.mousePosition != lastMousePosition || TDTouchControls.GetMouseButton(0) || TDTouchControls.GetMouseButton(1))
                {
                    SetMouseControlState(true);
                }
            }
            else
            {
                if (!masterMouseBlocker.activeSelf && initialGameAwakeComplete && allResourcesLoaded)
                {
                    if (!titleScreenGMS)
                    {
                        //Make sure player isn't null -- that's a thing that can happen
                        //if the Player goes back to the title screen

                        if (PlayerOptions.disableMouseOnKeyJoystick 
                                && TDInputHandler.lastActiveControllerType != ControllerType.Mouse 
                                && Cursor.visible && gameLoadSequenceCompleted)
                        {
                            if (!GameMasterScript.IsAnimationPlaying() && !cameraScript.customAnimationPlaying)
                            {
                                SetMouseControlState(false);
                                switchedInputMethodThisFrame = true;
                            }
                        }

                    }
                    else
                    {
                        if (ReInput.controllers.GetLastActiveControllerType() == ControllerType.Mouse)
                        {
                            masterMouseBlocker.SetActive(true);
                            Cursor.visible = true;
                            if (GameMasterScript.gameLoadSequenceCompleted)
                            {
                                UIManagerScript.dynamicCanvasRaycaster.enabled = true;
                            }
                        }
                        else
                        {

                        }
                    }
                }
            }
        }
}

        lastMousePosition = Input.mousePosition;

        if (titleScreenGMS)
        {
            //Shep: Skip title, just load

            //Mode select object input
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) &&
                UIManagerScript.singletonUIMS.campaignSelectManager.gameObject.activeInHierarchy &&
                UIManagerScript.singletonUIMS.campaignSelectManager.UpdateInput())
            {
                return;
            }


            //maybe we want to do something else before this i don't know
            return;
        }

        if (!initialGameAwakeComplete || !allResourcesLoaded)
        {
            return;
        }

        if (!resourcePoolsCreated && !loadGame_creatingResourcePools && dictObjectPools == null)
        {
            StartCoroutine(CreateResourcePools());
            return;
        }
        else
        {
            if (!resourcePoolsCreated)
            {
                // We're waiting for resource pooling.
                return;
            }
        }

        /* if ((cameraScript.customAnimationPlaying) && (Time.time - timeAtGameStart > 10f) && (MapMasterScript.activeMap.floor == MapMasterScript.TOWN_MAP_FLOOR))
        {
            cameraScript.customAnimationPlaying = false;
        } */

        if (loadGame_inProgress) return;

        if (framesToLoadGame > 0)
        {
            framesToLoadGame--;
            if (framesToLoadGame == 0)
            {
                try
                {
                    if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
                    {
                        loadGame_inProgress = true;
                        StartCoroutine(TryLoadGame());
                        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
                        {
                            UIManagerScript.SetGlobalResponse(DialogButtonResponse.NOTHING);
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Error occurred in game load attempt.");
                    Debug.Log(e);

                    if (GameStartData.CurrentLoadState == LoadStates.MAP_MISMATCH)
                    {
                        firstUpdate = false;
                        GameStartData.CurrentLoadState = LoadStates.NORMAL;
                        GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
                        UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
                        SceneManager.LoadScene("Gameplay");
                        return;
                    }
                    else
                    {
                        BattleTextManager.DeInitialize();
                        SceneManager.LoadScene("Main");
                        UIManagerScript.SetGlobalResponse(DialogButtonResponse.NOTHING);

                    }
                }
            }
            else
            {
                return;
            }
        }

        //Debug.Log(firstUpdate + " " + initialGameAwakeComplete + " " + allResourcesLoaded + " " + resourcePoolsCreated + " " + UIManagerScript.globalDialogButtonResponse);

        if (!firstUpdate && initialGameAwakeComplete && allResourcesLoaded && resourcePoolsCreated)
        {
            if (!SharedBank.finishedReadingStash && !SharedBank.startedReadingStash)
            {                
                SharedBank.startedReadingStash = true;
                if (Debug.isDebugBuild) Debug.Log("Started read from shared bank save coroutine.");
                StartCoroutine(SharedBank.ReadFromSave(false));
                return;
            }

            if (!SharedBank.finishedReadingStash) return;

            if (GameStartData.newGame && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
            {
                if (heroPCActor == null)
                {
                    UIManagerScript.SetPlayerHudAlpha(0.0f);
                    heroPCActor = CreateHeroPC();
                    heroPCActor.HeroStart(true);
                    //Debug.Log("Started hero PC");
                    if (heroPCActor.myStats.CheckHasStatusName("status_scavenger"))
                    {
                        playerIsScavenger = true;
                    }
                }
            }
            else if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.NEWGAMEPLUS)
            {
                if (Debug.isDebugBuild) Debug.Log("Attempting NEW GAME PLUS start.");
                try 
                { 
                    LoadOnlyHeroData();
                    //LoadOnlyHeroDataAndReturnHeroPetToCorral();                     
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to start NG+ because of " + e);
                    BattleTextManager.DeInitialize();
                    SceneManager.LoadScene("Main");
                }
            }
            else if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.REBUILDMAPS)
            {
                Debug.Log("<color=green>****************Load only hero data to REBUILD MAPS.*****************</color>");
                try
                {
                    Debug.Log("<color=green>****************Load only hero data to REBUILD MAPS.*****************</color>");
                    LoadOnlyHeroData(loadPreMysteryData: true);
                }
                catch (Exception e)
                {
                    Debug.Log("COULD NOT LOAD HERO DATA: " + e);
                    BattleTextManager.DeInitialize();
                    SceneManager.LoadScene("Main");
                }
            }
            // Below applies for New Game+            
           

            mms.MapStart(true);
            firstUpdate = true;

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
            {	
                musicManager.PushSpecificMusicTrackOnStack("towntheme1");
            }
        }

        //Watch for actors holding up the show
        if (animatingActorsPreventingUpdate.Count > 0)
        {
            animatingActorsPreventingUpdate.RemoveWhere(a => a == null || (a.myMovable.IsMoving() == false && a.myMovable.QueuedMovementCount() == 0));
        }

        //Clear out old dead coroutines that are holding us up
        if (coroutinesPreventingUpdate.Count > 0)
        {
            coroutinesPreventingUpdate.RemoveAll(c => c == null || c.IsFinished());
        }

        if (actualGameStarted)
        {
            TDInputHandler.UpdateInput();
        }
    }


    /// <summary>
    /// Use this to hide the cursor on the Switch, or other gamepad-only platforms, at all times.
    /// </summary>
    void LateUpdate()
    {
        if (!PlatformVariables.GAMEPAD_ONLY) return;
        Cursor.visible = false;
    }


    private HeroPC CreateHeroPC()
    {
        heroPCActor = new HeroPC();
        //Debug.Log("Hero PC actor created.");
        heroPCActor.lastMovedDirection = Directions.SOUTH;
        heroPCActor.actorRefName = "actor_player";
        heroPCActor.actorfaction = Faction.PLAYER;
        heroPCActor.playerCollidable = true;
        heroPCActor.monsterCollidable = true;
        heroPCActor.myAbilities = new AbilityComponent();
        heroPCActor.myAbilities.owner = heroPCActor;
        heroPCActor.CreateNewInventory();
        heroPCActor.myEquipment = new EquipmentBlock();
        heroPCActor.myEquipment.owner = heroPCActor;
        heroPCActor.myStats = new StatBlock();
        heroPCActor.myStats.SetOwner(heroPCActor);
        heroPCActor.displayName = GameStartData.playerName;
        if (GameStartData.miscGameStartTags.Contains("malemode"))
        {
            heroPCActor.SetActorData("malemode", 1);
        }
        heroObjectCreated = true;
        heroPCActor.summonedActors = new List<Actor>();
        heroPCActor.summonedActorIDs = new List<int>();
        mms.heroPCActor = heroPCActor;

        //SHEP: Prevent random unlucky dying in the editor
#if UNITY_EDITOR
        debug_neverDie = true;
#endif

        createdHeroPCActor = true;
        return heroPCActor;
    }

    IEnumerator WaitForPlayerMove(float seconds)
    {
        checkMoveIndex++;
        int x = checkMoveIndex;
        yield return new WaitForSeconds(seconds);
        if (x == checkMoveIndex)
        {
            playerMovingAnimation = false;
        }
    }

    IEnumerator FinalLoadWaiter()
    {
        yield return null;
        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            musicManager.FillStackWithTracksToLoad();
        }
        for (int i = 0; i < 30; i++)
        {
            yield return null;
        }
        MapCreationFinished();
    }

    public static void CheckForTextOverlay(bool markAsReadButDontDisplay = false)
    {

        bool showCustomIntro = false;

#if UNITY_EDITOR
        showCustomIntro = false;
#endif

        if ((MapMasterScript.activeMap.dungeonLevelData.hasOverlayText && GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.SHARA &&
            !RandomJobMode.IsCurrentGameInRandomJobMode()) || showCustomIntro)
        {
            OverlayTextData otd = new OverlayTextData();
            otd.refName = MapMasterScript.activeMap.dungeonLevelData.overlayRefName;
            otd.headerText = MapMasterScript.activeMap.dungeonLevelData.overlayDisplayName;

            if (otd.refName == "intro_custom" || showCustomIntro)
            {
                string searchString = "intro_" + GameMasterScript.heroPCActor.myJob.jobEnum.ToString().ToLowerInvariant();
                string findText = StringManager.GetString(searchString);
                otd.descText = findText;
                otd.introText = true;

                //it looks like this is the spot where we check to see if we're starting a new character in riverstone? 

                if ((PlayerOptions.tutorialTips && !tutorialManager.WatchedTutorial("tutorial_analog_movement")) || showCustomIntro)
                {
                    if ((PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == ControllerType.Joystick) || showCustomIntro)
                    {
                        Conversation walktut = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_analog_movement");
                        StartWatchedCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(walktut, DialogType.TUTORIAL, null, 7.5f));
                    }
                }
            }
            else
            {
                otd.descText = MapMasterScript.activeMap.dungeonLevelData.overlayText;
            }

            UIManagerScript.WriteOverlayText(otd, markAsReadButDontDisplay);
        }
    }

    public bool IsGamePaused()
    {
        return gamePaused;
    }

    public bool HeroCreated()
    {
        return heroObjectCreated;
    }

    public bool CheckTileForFriendlyConversation(MapTileData mtd, bool rightClick)
    {
        if (mtd == null)
        {
            return false;
        }
        foreach (Actor act in mtd.GetAllActors())
        {
            if (act.actorfaction == Faction.PLAYER && act.GetActorType() == ActorTypes.MONSTER)
            {
                if ((act.summoner == heroPCActor || act == heroPCActor.GetMonsterPet()) && !rightClick)
                {
                    PetPartyUIScript.StartPetBehaviorConversationFromRef(act);
                    return true;
                }
                else
                {
                    // Charmed monster?
                    if (act != heroPCActor.GetMonsterPet() && act.actorRefName != "mon_runiccrystal" && act.summoner != heroPCActor)
                    {
                        StringManager.SetTag(0, act.displayName);
                        StringManager.SetTag(1, StringManager.GetString("dialog_confirm_hit_friendly_intro_txt_part2"));
                        gmsSingleton.SetTempGameData("hitfriendly", act.actorUniqueID);
                        UIManagerScript.StartConversationByRef("confirm_hit_friendly", DialogType.STANDARD, null);
                        return true;
                    }
                }
            }
            else if (act.actorfaction == Faction.ENEMY && act.GetActorType() == ActorTypes.MONSTER && !rightClick)
            {
                // Examine mode clicked on an enemy that is not presently hostile or in combat with us
                Monster m = act as Monster;
                if (!m.CheckTarget(heroPCActor))
                {
                    StringManager.SetTag(0, act.displayName);
                    StringManager.SetTag(1, "\n");
                    gmsSingleton.SetTempGameData("hitfriendly", act.actorUniqueID);
                    UIManagerScript.StartConversationByRef("confirm_hit_friendly", DialogType.STANDARD, null);
                    return true;
                }
            }
        }
        return false;
}

    public void SetAbilityToTry(AbilityScript abil)
    {
        abilityToTry = abil;
    }

    //todo: find out where this function is being called from the in the editor
    // AA answer: click on portrait
    public void ToggleMenuSelect()
    {
        if (IsNextTurnPausedByAnimations() || UIManagerScript.dialogBoxOpen)
        {
            return;
        }

        uims.ExitTargeting();

        if (UIManagerScript.AnyInteractableWindowOpen())
        {
            UIManagerScript.TryCloseFullScreenUI();
        }
        else
        {
            if (UIManagerScript.uiPortraitExclamation.activeSelf)
            {
                UIManagerScript.OpenFullScreenUI(UITabs.SKILLS);
            }
            else
            {
                UIManagerScript.OpenFullScreenUI(UITabs.CHARACTER);
            }

        }

        return;
    }

    public void ToggleMinimap(MinimapStates size, bool alwaysSwitchOnOrOff = false)
    {
        MinimapUIScript.SetMinimapToSpecificState(size, alwaysSwitchOnOrOff);
    }

    public List<Monster> GetMonstersWithinSight()
    {
        pool_monsterList.Clear();
        //pool_MTD = MapMasterScript.GetTilesAroundPoint(heroPCActor.GetPos(), (int)heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE));

        CustomAlgorithms.GetTilesAroundPoint(heroPCActor.GetPos(), (int)(heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) / 2f), MapMasterScript.activeMap);
        MapTileData mtd = null;
        List<Actor> targ = null;
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            mtd = CustomAlgorithms.tileBuffer[i];
            if (mtd.tileType == TileTypes.GROUND && heroPCActor.visibleTilesArray[(int)mtd.pos.x, (int)mtd.pos.y]) //(mtd.pos != heroPCActor.GetPos()))
            {
                // if (MapMasterScript.CheckTileToTileLOS(heroPCActor.GetPos(), mtd.pos, heroPCActor))
                //if (heroPCActor.visibleTilesArray[(int)mtd.pos.x, (int)mtd.pos.y])
                {
                    targ = mtd.GetAllTargetable();
                    if (targ.Count > 0)
                    {
                        foreach (Actor act in targ)
                        {
                            if (act.actorfaction != Faction.PLAYER && act.GetActorType() == ActorTypes.MONSTER)
                            {
                                Monster mn = act as Monster;
                                pool_monsterList.Add(mn);
                            }
                        }
                    }
                }
            }
        }

        return pool_monsterList;
    }
    
    public static void TryBeginItemWorld(Item itm, Item orbUsed, float magicChance, bool debugTest = false)
    {

        if (itm.itemType == ItemTypes.CONSUMABLE)
        {
            Debug.Log("Cannot start item world with consumable.");
            return;
        }

        if (orbUsed.GetQuantity() > 1)
        {
            orbUsed = heroPCActor.myInventory.GetItemAndSplitIfNeeded(orbUsed, 1);
        }
        else
        {
            heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(orbUsed, -1);
        }


        ItemDreamFunctions.ResetDreamData();

        ProgressTracker.RemoveProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO);

        if (!debugTest)
        {
	        UIManagerScript.PlayCursorSound("EnterItemWorld");
        }        

        ItemDreamFunctions.InitializeItemDreamHeroData();
        Map[] itemWorld = mms.SpawnItemWorld(itm, orbUsed, magicChance, null, null, debugTest: true);
        for (int i = 0; i < itemWorld.Length; i++)
        {
            heroPCActor.RemoveActorData("iw_map_" + itemWorld[i].mapAreaID);
        }
        if (debugTest)
        {
            return;
        }

        ItemDreamFunctions.TryCreateDreamWeaponRumorBoss();

        heroPCActor.myStats.RemoveStatusByRef("status_itemworld");
        UIManagerScript.RefreshStatuses();

        Equipment eq = itm as Equipment;

        heroPCActor.myEquipment.UnequipByReference(eq);
        if (eq.itemType == ItemTypes.WEAPON)
        {
            UIManagerScript.RemoveWeaponFromActives(eq as Weapon);
        }
        heroPCActor.myInventory.RemoveItem(eq);



        TravelManager.TravelMaps(itemWorld[0], null, false);

        if (ItemDreamFunctions.IsItemDreamNightmare())
        {
            ItemDreamFunctions.RelocateKingToPlayer();
        }
    }

    // Should the Map Master handle this?
    private void SpawnMapObject(string name, Vector2 location, ActorTypes aType)
    {
        Actor newObject = new Actor();
        bool terrainSprite = false;
        int spriteIndex = 0;

        if (name == "obj_rivertile" || name.Contains("phasmashieldtile") || name == "obj_mudtile" || name == "obj_lavatile" || name == "obj_voidtile" || name == "obj_electile")
        {
            MapTileData mtd = MapMasterScript.GetTile(location);

            newObject.prefab = "TerrainTile";
            if (name == "obj_electile")
            {
                newObject.prefab = "ElectricTile";
            }
            else if (name == "obj_mudtile")
            {
                newObject.prefab = "MudTile";
            }
            else if (name.Contains("phasmashieldtile"))
            {
                newObject.prefab = "LaserTile";
            }
            terrainSprite = true;
            spriteIndex = mtd.indexOfTerrainSpriteInAtlas;
        }
        else
        {
            newObject.prefab = name;
        }

        GameObject go = (GameObject)Instantiate(Resources.Load(newObject.prefab));
        if (terrainSprite)
        {
            Animatable localAnim = go.GetComponent<Animatable>();
            for (int i = 0; i < localAnim.myAnimations[0].mySprites.Count; i++)
            {
                zirconAnim.AnimationFrameData afd = localAnim.myAnimations[0].mySprites[i];
                if (name == "obj_mudtile")
                {
                    afd.mySprite = MapMasterScript.terrainAtlas[spriteIndex];
                }
                else
                {
                    afd.mySprite = MapMasterScript.terrainAtlas[spriteIndex + (i * 20)];
                }
            }

        }

        newObject.SetActorType(aType);
        newObject.SetObject(go);

        Movable move = go.GetComponent<Movable>();
        Vector2 mapSize = mms.GetMapSize();

        //move.collidable = move.defaultCollidable;

        // Figure out better spawn logic.
        //Vector2 spawnLocation = new Vector2((int)(UnityEngine.Random.Range(1, mapSize.x - 1)), (int)(UnityEngine.Random.Range(1, mapSize.y - 1)));        
        Vector2 spawnLocation = location;
        /* while (mms.CheckCollision(spawnLocation))
        {
            spawnLocation = new Vector2((int)(UnityEngine.Random.Range(1, mapSize.x - 1)), (int)(UnityEngine.Random.Range(1, mapSize.y - 1)));
        } */

        Animatable anm = go.GetComponent<Animatable>();
        if (anm != null)
        {
            anm.SetAnim(anm.defaultIdleAnimationName);
        }

        Vector3 pos = go.transform.position;
        pos.x = spawnLocation.x;
        pos.y = spawnLocation.y;
        go.transform.position = pos;
        move.SetPosition(pos);
        newObject.SetCurPos(pos);
        mms.AddActorToLocation(pos, newObject);
        mms.AddActorToMap(newObject);

        if (aType == ActorTypes.POWERUP)
        {
            newObject.monsterCollidable = false;
            newObject.playerCollidable = false;
        }
    }

    public static void SpawnItemAtPosition(Item spawnItem, Vector3 location)
    {
        location.x = Mathf.Round(location.x);
        location.y = Mathf.Round(location.y);
        spawnItem.SetSpawnPosXY((int)location.x, (int)location.y);
        bool success = mms.AddActorToLocation(location, spawnItem);
        if (success)
        {
            success = mms.AddActorToMap(spawnItem);
            if (success)
            {
                mms.SpawnItem(spawnItem);
                spawnItem.collection = null;
                spawnItem.dungeonFloor = MapMasterScript.activeMap.floor;
            }
        }
    }

    public GameObject GetHeroPC()
    {
        return heroPC;
    }

    public void AwardJP(float amount)
    {
        amount = Mathf.Round(amount);

        amount = heroPCActor.ProcessJPGain(amount);

        heroPCActor.AddJP(amount);

        playerStatsChangedThisTurn = true;
        StringManager.SetTag(0, ((int)amount).ToString());
        GameLogScript.LogWriteStringRef("log_gain_jp");
    }

    public void AwardXPFlat(float amount, bool silent)
    {
        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            return;
        }

        StatBlock heroSB = heroPCActor.myStats;
        if (!silent)
        {
            StringManager.SetTag(0, ((int)amount).ToString());
            GameLogScript.LogWriteStringRef("log_gain_experience");
        }
        bool levelUp = heroSB.ChangeExperience((int)amount);
        if (levelUp)
        {
            heroSB.LevelUp();
        }
        //UIManagerScript.RefreshPlayerStats();
        playerStatsChangedThisTurn = true;
    }

    public void TryNextTurn(TurnData tData, bool newTurn, int iThreadIndex = 0)
    {
        /* if (Time.time - timeSinceLastActionInput <= playerMoveSpeed+0.01f)
        {
            Debug.Log("Too fast...");
            return;
        } */

        // Use this to determine if player is acting really fast for purposes of gameover
        for (int i = trueTimeOfRecentTurns.Length - 1; i > 0; i--)
        {
            trueTimeOfRecentTurns[i] = trueTimeOfRecentTurns[i - 1];
        }
        trueTimeOfRecentTurns[0] = Time.time;
#if UNITY_EDITOR
        GameNextTurn(tData, newTurn, iThreadIndex);
#else
    try { GameNextTurn(tData, newTurn, iThreadIndex); }
    catch (Exception e)
    {
            ResetTurnDataDueToError();
           if (Debug.isDebugBuild) 
            {
        if (tData != null)
        {
            Debug.Log(tData.GetTurnType() + " " + tData.centerPosition + " " + tData.newPosition);
            if (tData.tAbilityToTry != null)
            {
                Debug.Log("TABIL: " + tData.tAbilityToTry.refName);
            }
        }
        else
        {
            Debug.Log("NULL TDATA?");
        }
                GameLogScript.GameLogWrite("An error occurred: " + e, null);
                GameLogScript.GameLogWrite(e.StackTrace, null);
            }

    }
#endif
    }

    bool CheckIfPlayerParalyzedOrObstructed(TurnData tData)
    {
        MapTileData newLoc = MapMasterScript.GetTile(tData.newPosition);
        Actor targ = newLoc.GetTargetable();

        if (heroPCActor.myStats.CheckParalyzeChance() == 1.0f)
        {
            if (targ != null && targ.actorfaction != Faction.PLAYER && targ.GetActorType() == ActorTypes.MONSTER)
            {
                GameLogScript.DelayedParalyzeMessage(StringManager.GetString("player_paralyzed"), heroPCActor);
                return true;
            }
        }

        if (targ == null && newLoc.playerCollidable)
        {
            if (newLoc.GetInteractableNPC() == null)
            {
                return true;
            }

        }
        if (targ != null && targ.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = targ as Destructible;
            if (dt.playerCollidable && !dt.targetable)
            {
                // TODO: Play bump sound.
                return true;
            }
        }

        return false;
    }

    List<Actor> GetTargetsFromTargetData(TargetData processTD)
    {
        pool_targetList.Clear();
        if (!processTD.whichAbility.CheckAbilityTag(AbilityTags.GROUNDTARGET)) // This hits actors and not tiles.
        {
            foreach (Actor act in processTD.targetActors)
            {
                if ((!pool_targetList.Contains(act)) || (!processTD.whichAbility.CheckAbilityTag(AbilityTags.UNIQUETARGET)))
                {
                    pool_targetList.Add(act);
                }
            }
        }
        else // Ground targeted ability
        {
            // First, find all targets.
            foreach (Vector2 pos in processTD.targetTiles)
            {
                if (!MapMasterScript.InBounds(pos)) continue;
                List<Actor> subTargets = MapMasterScript.GetTile(pos).GetAllTargetablePlusDestructibles();

                if (processTD.whichAbility.refName == "skill_revivemonster" || processTD.whichAbility.refName == "skill_revivemonster_herge")
                {
                    Actor powerup = MapMasterScript.GetTile(pos).GetActorRef("powerup_stamina");
                    if (powerup == null)
                    {
                        powerup = MapMasterScript.GetTile(pos).GetActorRef("powerup_energy");
                    }
                    if (powerup == null)
                    {
                        powerup = MapMasterScript.GetTile(pos).GetActorRef("powerup_health");
                    }
                    if (powerup == null)
                    {
                        powerup = MapMasterScript.GetTile(pos).GetActorRef("monsterspirit");
                    }
                    if (powerup != null)
                    {
                        pool_targetList.Add(powerup);
                        Destructible dt = powerup as Destructible;
                        SetTempStringData("revivemonster", dt.monsterAttached);
                    }
                }

                if (subTargets != null)
                {
                    foreach (Actor tAct in subTargets)
                    {
                        if (!processTD.whichAbility.CheckAbilityTag(AbilityTags.HEROAFFECTED) && tAct == heroPCActor)
                        {
                            continue;
                        }
                        if ((!processTD.whichAbility.CheckAbilityTag(AbilityTags.MONSTERAFFECTED)) && (tAct.GetActorType() == ActorTypes.MONSTER))
                        {
                            continue;
                        }
                        if ((!processTD.whichAbility.CheckAbilityTag(AbilityTags.DESTRUCTIBLEAFFECTED)) && (tAct.GetActorType() == ActorTypes.DESTRUCTIBLE))
                        {
                            continue;
                        }
                        if ((!pool_targetList.Contains(tAct)) || (!processTD.whichAbility.CheckAbilityTag(AbilityTags.UNIQUETARGET)))
                        {
                            pool_targetList.Add(tAct);
                        }
                    }
                }
            }
        }
        // End if/else case for actor vs. ground target    
        return pool_targetList;
    }

    public float RunTileEffect(Destructible dt, TurnData td, int iThreadIndex, out bool continueTurn)
    {
        continueTurn = true;
        GameLogScript.BeginTextBuffer();
        allTargetable = MapMasterScript.GetTile(dt.GetPos()).GetAllTargetable();

        float waitTime = 0.0f; // Ignore wait time for now.
        affectedActors.Clear();
        cResults.Clear();

        if (!dt.CheckIfCanUseStatus()) 
        {
            //Debug.Log(dt.actorRefName + " cannot use status");
            return 0f;
        }

        //Debug.Log(GameMasterScript.turnNumber + " " +  dt.actorRefName + " " + dt.statusRef + " " + dt.dtStatusEffect.listEffectScripts.Count);
        {
            // Can do something to actors who end turn in this tile.         
            dt.dtStatusEffect.maxDuration = 1;
            dt.dtStatusEffect.curDuration = 1;

            EffectScript eff;
            for (int w = 0; w < dt.dtStatusEffect.listEffectScripts.Count; w++)
            {
                eff = dt.dtStatusEffect.listEffectScripts[w];
                if (dt.actorfaction == Faction.DUNGEON || eff.originatingActor == null)
                {
                    eff.originatingActor = theDungeonActor;
                }
                else if (eff.originatingActor.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    // Destructibles should never have themselves as OA, it must be connected to a fighter, such as the dummy Dungeon actor
                    eff.destructibleOwnerOfEffect = eff.originatingActor; // Set the destructible "owner" here instead
                    eff.originatingActor = theDungeonActor;
                }
                eff.affectedActors.Clear();
                eff.targetActors.Clear();
                eff.centerPosition = dt.GetPos();
                eff.positions.Clear();
                eff.positions.Add(dt.GetPos());
                eff.destructibleOwnerOfEffect = dt;
                Fighter posTarget = null;
                for (int i = 0; i < allTargetable.Count; i++)
                {
                    posTarget = allTargetable[i] as Fighter;
                    Monster mon = null;
                    if (posTarget.GetActorType() == ActorTypes.MONSTER)
                    {
                        mon = posTarget as Monster;
                        bool lava = false;
                        if (dt.dtStatusEffect.refName == "status_lavaburns" || dt.dtStatusEffect.refName == "status_floorspikes")
                        {
                            lava = true;
                        }
                        if (lava && mon.CheckAttribute(MonsterAttributes.LOVESLAVA) > 0 || mon.CheckAttribute(MonsterAttributes.FLYING) > 0)
                        {
                            // Does nothing to fire-immune monsters. Should it actually buff them?
                            continue;
                        }
                        if (dt.dtStatusEffect.refName == "status_mudtile")
                        {
                            if (mon.CheckAttribute(MonsterAttributes.LOVESMUD) > 0 || mon.CheckAttribute(MonsterAttributes.FLYING) > 0)
                            {
                                continue;
                            }
                            if (mon.isChampion && UnityEngine.Random.Range(0, 1f) <= 0.5f) continue;
                        }
                    }

                    if ((posTarget.GetNumCombatTargets() > 0 && dt.dtStatusEffect.combatOnly) || !dt.dtStatusEffect.combatOnly)
                    {
                        if (posTarget.GetActorType() == ActorTypes.MONSTER)
                        {
                            if (mon.myBehaviorState != BehaviorState.FIGHT && dt.dtStatusEffect.combatOnly)
                            {
                                continue;
                            }


                        }
                        else if (posTarget.GetActorType() == ActorTypes.HERO)
                        {
                            if (dt.dtStatusEffect.refName == "status_mudtile")
                            {
                                if (heroPCActor.myStats.CheckHasStatusName("status_mmresistmud"))
                                {
                                    //Debug.Log("Resist mud.");
                                    continue;
                                }
                            }
                        }
                        if (dt.dtStatusEffect.isPositive)
                        {
                            if (posTarget.actorfaction == eff.originatingActor.actorfaction)
                            {
                                eff.targetActors.Add(posTarget);
                            }
                        }
                        else
                        {
                            //Debug.Log(eff.effectRefName + " " + dt.GetPos() + " " + posTarget.actorfaction + " " + eff.originatingActor.actorfaction + " " + posTarget.actorRefName + " " + eff.originatingActor.actorRefName + " " + eff.originatingActor.GetPos());
                            if (posTarget.actorfaction != eff.originatingActor.actorfaction)
                            {
                                eff.targetActors.Add(posTarget);
                            }
                            if (posTarget.actorfaction == Faction.ENEMY && eff.effectRefName != null && eff.effectRefName == "eff_blazegrounddmg") // Hardcoded
                            {
                                if (posTarget.CheckSummon(dt)) // Run this effect on the flame's summoner.
                                {
                                    eff.targetActors.Add(posTarget);
                                }
                            }
                        }
                    }

                }

                try 
                {
                    //Debug.Log(dt.actorRefName + " tries running effect " + eff.effectRefName);
                    waitTime += eff.DoEffect(); 
                }
                catch (Exception e)
                {
                    Debug.Log("Error running tile effect " + eff.effectRefName + " by " + dt.actorRefName + " " + e);
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                    GameLogScript.GameLogWrite(UIManagerScript.redHexColor + "A tile effect error occurred. You can keep playing, but please send the devs your output_log file!</color>", heroPCActor);
#endif
                }

                foreach (CombatResult reso in eff.results)
                {
                    cResults.Add(reso);
                }
                foreach (Actor affAct in eff.affectedActors)
                {
                    affectedActors.Add(affAct);
                    // Hardcoded exception
                    if (affAct.GetActorType() == ActorTypes.MONSTER && affAct.actorfaction != Faction.PLAYER && dt.dtStatusEffect.refName != "status_smokecloud")
                    {
                        Monster monmon = affAct as Monster;
                        monmon.AddAggro(eff.originatingActor, 15f);
                        monmon.lastActorAttackedBy = eff.originatingActor as Fighter;
                    }
                }
                for (int x = 0; x < cResults.Count; x++)
                {
                    CombatResultsScript.CheckCombatResult(cResults[x], affectedActors[x], MapMasterScript.activeMap);
                }

                if (affectedActors.Count == 0)
                {
                    GameLogScript.EndTextBufferAndWrite();
                    // Commenting out the return here, as it cancels other tile effects.
                    //return -1f;
                }
            }
        }

        if (waitTime > 0.0f && td != null) // if td is null, we are not running inside the turn thread.
        {
            SetAnimationPlaying(true);
            combatManager.ProcessQueuedEffects();

            if (td.GetTurnType() == TurnTypes.ABILITY)
            {
                //Debug.Log("DT Waiting to check results, " + heroPCActor.lastTurnActed + " " + turnNumber + " Acted? " + td.GetPlayerActedThisTurn() + " " + processBufferTargetDataIndex);
            }
            continueTurn = false;


            //Debug.Log(dt.actorUniqueID + " is about to create a new thread in RunTileEffect FROM thread " + iThreadIndex);

            //Debug.Log("Doing something 3 to exit turn routine.");
            StartCoroutine(WaitCheckResultsThenContinueTurn(cResults, affectedActors, waitTime, td, dt, iThreadIndex));
        }
        else
        {
            //Debug.Log("No process time.");
            for (int x = 0; x < cResults.Count; x++)
            {
                CombatResultsScript.CheckCombatResult(cResults[x], affectedActors[x], MapMasterScript.activeMap);
            }
        }

        GameLogScript.EndTextBufferAndWrite();

        if (dt.dieAfterRunEffect) 
        {
            dt.RemoveImmediately();
            MapMasterScript.GetTile(dt.GetPos()).RemoveActor(dt);
            AddToDeadQueue(dt);
        }

        return waitTime;
    }

    public static void DisplayLevelUpDialog()
    {
        if (gmsSingleton.coroutinesPreventingUpdate.Count > 0)
        {
            return;
        }

        Conversation lvlup = GameMasterScript.FindConversation("levelupstats");
        StartWatchedCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(lvlup, DialogType.LEVELUP, null, 0.1f));
    }

    public void UseTent()
    {
        UIManagerScript.FadeOut(3f);
        MusicManagerScript.RequestPlayNonLoopingMusicFromScratchWithCrossfade("resttheme");
		GameMasterScript.SetAnimationPlaying(true);
        heroPCActor.myStats.HealToFull();
        heroPCActor.HealAllSummonsToFull();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.singletonUIMS.WaitThenFadeIn(3.1f, 3f, true);
        StartCoroutine(WaitThenTickGameTime(1, true, 3.01f));
        if (MapMasterScript.activeMap == mms.townMap)
        {
            MusicManagerScript.singleton.WaitThenPlay("towntheme1", 7f);
        }
        else
        {
            MusicManagerScript.singleton.WaitThenPlay("grovetheme", 7f);
        }
    }
    
    public static bool InMainScene()
    {
        // This "Switch_Main" scene will be deprecated.
        return SceneManager.GetActiveScene().name == "Main";
    }
    public static void LoadMainScene()
    {        
#if UNITY_SWITCH
        Switch_SaveDataHandler.FlushBytesLoadedAsync();
#endif

        TDSceneManager.LoadScene("Main");        
    }
    public void UpdateHeroObject(string forcePrefab = "")
    {

        if (heroPC != null)
        {
            //Debug.Log("destroying old sprite");
            heroPCActor.myAnimatable = null;
            heroPCActor.myMovable = null;
            TryReturnChildrenToStack(heroPC);
            Destroy(heroPCActor.myAnimatable);
            Destroy(heroPC);
            heroPC = null;
        }

        if (heroPCActor == null)
        {
            Debug.Log("Hero PC actor is null somehow.");
        }

        if (heroPCActor.myJob == null)
        {
            Debug.Log("HeroPC my job is null...?");
        }


        string prefabToUse = heroPCActor.myJob.prefab;

        if (!string.IsNullOrEmpty(heroPCActor.selectedPrefab))
        {
            prefabToUse = heroPCActor.selectedPrefab;
        }

        if (!string.IsNullOrEmpty(forcePrefab))
        {
            prefabToUse = forcePrefab;
        }

        if (prefabToUse == "SwordDancer" && GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR]) prefabToUse = "LNY_SwordDancer";

        //Debug.Log("<color=green>Update hero object: " + prefabToUse + "</color>");

        heroPC = (GameObject)Instantiate(Resources.Load("Jobs/" + prefabToUse));
        GameObject wrathBar = (GameObject)Instantiate(Resources.Load("PlayerWrathBar"));
        PlayerModManager.TryReplaceJobSprites(heroPCActor.myJob.jobEnum, heroPC);
        heroPCActor.wrathBarScript = wrathBar.GetComponent<WrathBarScript>();
        heroPCActor.wrathBarScript.gameObject.transform.SetParent(heroPC.transform);
        heroPCActor.wrathBarScript.gameObject.transform.localPosition = new Vector3(0f, -0.84f, 1f);
        heroPCActor.wrathBarScript.gameObject.transform.localScale = Vector3.one;
        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            heroPCActor.wrathBarScript.gameObject.SetActive(false);
        }


        //Debug.Log("Setting portrait sprite " + heroPCActor.myJob.portraitSpriteRef);

        if (heroPCActor.myJob.PortraitSprite != null)
        {
            UIManagerScript.hudPlayerPortrait.sprite = heroPCActor.GetPortrait();
            UIManagerScript.csPortrait.sprite = heroPCActor.GetPortrait();
        }
        else
        {
            UIManagerScript.hudPlayerPortrait.sprite = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND).PortraitSprite;
            UIManagerScript.csPortrait.sprite = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND).PortraitSprite;
        }

        GameObject healthBar = GameObject.Instantiate(GameMasterScript.GetResourceByRef("PlayerIngameHealthBar"));
        healthBar.transform.SetParent(heroPC.transform);
        healthBar.transform.localScale = Vector3.one;
        heroPCActor.healthBarScript = healthBar.GetComponent<HealthBarScript>();
        heroPCActor.healthBarScript.UpdateBar(heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH));
        heroPCActor.healthBarScript.parentSR = heroPCActor.mySpriteRenderer;

        if (!PlayerOptions.playerHealthBar)
        {
            heroPCActor.healthBarScript.gameObject.SetActive(false);
        }

        cameraScript.heroTransform = heroPC.transform;
        heroPCActor.SetObject(heroPC);
        heroMovable = heroPCActor.myMovable;
        heroMovable.SetPosition(heroPCActor.GetPos());
        heroMovable.SyncGridPosition();
        heroPCActor.myAnimatable = heroPC.GetComponent<Animatable>();
        heroPCActor.myAnimatable.SetAnim("Idle");

        heroPCActor.diagonalOverlay = (GameObject)Instantiate(Resources.Load("DiagonalGrip"));
        heroPCActor.diagonalOverlay.transform.SetParent(heroPC.transform);
        heroPCActor.diagonalOverlay.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        heroPCActor.diagonalOverlay.SetActive(false);

        var goAnalogGrip = (GameObject)Instantiate(Resources.Load("AnalogArrowOverlay"));
        goAnalogGrip.SetActive(true);
        goAnalogGrip.transform.SetParent(heroPC.transform);
        heroPCActor.analogTargetingOverlayRenderer = goAnalogGrip.GetComponent<SpriteRenderer>();
        heroPCActor.analogTargetingOverlayRenderer.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        heroPCActor.analogTargetingOverlayRenderer.enabled = false;


        var goExamineGlass = (GameObject)Instantiate(Resources.Load("ExamineModeIconOverlay"));
        goExamineGlass.SetActive(true);
        goExamineGlass.transform.SetParent(heroPC.transform);
        heroPCActor.examineModeIconRenderer = goExamineGlass.GetComponent<SpriteRenderer>();
        heroPCActor.examineModeIconRenderer.transform.localPosition = new Vector3(0f, 0.0f, 0f);
        heroPCActor.examineModeIconRenderer.enabled = false;

        if (!actualGameStarted)
        {
            heroPCActor.myMovable.SetInSightAndSnapEnable(false);
        }
        else
        {
            heroPCActor.myMovable.SetInSightAndSnapEnable(true);
        }
        BattleTextManager.AddObjectToDict(heroPCActor.GetObject());
    }

    public static string strAsyncLoadOutput;
    public static double lastAutoSaveTime;
    public static SaveDataDisplayBlockInfo saveDataBlockAsyncLoadOutput;

    public IEnumerator RebuildMapsAfterResourcesLoad()
    {
        yield return null;

        UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);

        while (!allResourcesLoaded && !resourcePoolsCreated && !gameLoadSequenceCompleted)
        {
            yield return null;
            Debug.Log(allResourcesLoaded + " " + resourcePoolsCreated + " " + gameLoadSequenceCompleted);
        }
        GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
        UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);

        TDSceneManager.LoadScene("Gameplay");
    }

    public void UpdateHeroSpriteMaterial()
    {

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            GameMasterScript.heroPCActor.mySpriteRenderer.material = spriteMaterialUnlit;
        }
        else

        {

            if (MapMasterScript.activeMap.IsTownMap())
            {
                GameMasterScript.heroPCActor.mySpriteRenderer.material = spriteMaterialUnlit;
            }
            else
            {
                GameMasterScript.heroPCActor.mySpriteRenderer.material = spriteMaterialLit;
            }
        }
    }
               
    public static bool HelpPlayerInAdventureMode()
    {
        if (gmsSingleton.gameMode == GameModes.ADVENTURE && heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f)
        {
            return true;
        }
        return false;
    }
    
    public IEnumerator WaitThenStopAnimation(float time)
    {
        yield return new WaitForSeconds(time);

        if (!animationFromCutscene)
        {
            SetAnimationPlaying(false);
        }
    }

    /// <summary>
    /// str8 copy a component's data from one object to another. Let's see if this works :D
    /// </summary>
    /// <param name="original">the source -- likely from a prefab</param>
    /// <param name="destination">the destination!</param>
    /// <returns></returns>
    public static Component CopyComponent(Component original, GameObject destination)
    {
        System.Type type = original.GetType();
        Component copy = destination.GetComponent(type);
        if (copy == null)
        {
            copy = destination.AddComponent(type);
        }

        // Copied fields can be restricted with BindingFlags
        System.Reflection.FieldInfo[] fields = type.GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy;
    }
    /// <summary>
    /// Fades the game out politely, then restarts a new character based on our current character's settings.
    /// </summary>
    /// <returns></returns>
    public static IEnumerator RestartGameSameCharacter()
    {
        UIManagerScript.PlayCursorSound("OPSelect");
        MusicManagerScript.singleton.Fadeout(0.8f);
        UIManagerScript.FadeOut(1.0f);
        yield return new WaitForSeconds(1.1f);

        GameStartData.newGame = true;
        GameStartData.playerJob = GameMasterScript.heroPCActor.myJob.jobEnum.ToString();
        GameStartData.playerName = GameMasterScript.heroPCActor.displayName;
        foreach (string feat in GameMasterScript.heroPCActor.heroFeats)
        {
            GameStartData.AddFeat(feat);
        }
        GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
        BossHealthBarScript.DisableBoss();
        TDSceneManager.LoadScene("Gameplay");
    }
    /// <summary>
    /// Fades out the game and returns to the title screen.
    /// </summary>
    /// <returns></returns>
    public static IEnumerator BackToTitleWithFade()
    {
        LoadingWaiterManager.Display(0.2f);
        UIManagerScript.PlayCursorSound("OPSelect");
        UIManagerScript.FadeOut(1.0f);
        MusicManagerScript.singleton.Fadeout(0.8f);
        yield return new WaitForSeconds(1.1f);

        GameMasterScript.ResetAllVariablesToGameLoad();
#if UNITY_SWITCH
        Switch_SaveDataHandler.FlushBytesLoadedAsync();
#endif
        //SceneManager.LoadScene("Empty");
        GameMasterScript.LoadMainScene();
    }




    static string GetGameClearText(bool clearedSavageMode)
    {
        if (allColorTags == null)
        {
            allColorTags = new List<string>()
        {
            UIManagerScript.redHexColor,
            UIManagerScript.orangeHexColor,
            UIManagerScript.blueHexColor,
            UIManagerScript.greenHexColor,
            UIManagerScript.greenHexColor,
            UIManagerScript.lightPurpleHexColor,
            UIManagerScript.brightOrangeHexColor,
            UIManagerScript.cyanHexColor,
            "<#fffb00>"
        };
        }
        string baseString = StringManager.GetString("misc_victory");
        if (!clearedSavageMode)
        {
            return "<color=green>**" + baseString + "**</color>";
        }
        else
        {
            baseString = "***" + baseString + "***";
            string strConstruct = "";
            string lastPicked = "";
            for (int i = 0; i < baseString.Length; i++)
            {
                string tagToUse = allColorTags.GetRandomElement();
                while (tagToUse == lastPicked)
                {
                    tagToUse = allColorTags.GetRandomElement();
                }
                strConstruct += tagToUse + baseString[i] + "</color>";
            }
            return strConstruct;
        }
    }
            
}