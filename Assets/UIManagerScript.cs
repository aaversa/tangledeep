using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Audio;
using System.Xml;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;
using System.Text;
using TMPro;
using System.Reflection;
using System.Runtime;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
	using UnityEngine.Analytics;
	using LapinerTools.Steam.Data;
	using LapinerTools.uMyGUI;
#endif

using Rewired;
public enum InputMethod { MOUSE, KEYBOARD, COUNT }
//public enum LineOrientations { NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST, COUNT }
public enum DialogType { INTRO, TUTORIAL, /* EXIT WAS HERE, */ CHANGECLOTHES, GAMEOVER, TITLESCREEN, GAMEINTRO, CCMODESELECT, CCJOBSELECT, CCFEATSELECT,
    STANDARD, WAYPOINT, LEVELUP, SELECTSLOT, CONFIRM, MENUSELECT, KNOCKEDOUT, KEYSTORY, CCGAMEMODSELECT, EXIT, COUNT }
public enum HotbarBindableActions { ABILITY, CONSUMABLE, NOTHING, COUNT }
public enum SND { SILENT, PLAY }
public enum InventorySortTypes { ALPHA, RARITY, VALUE, ITEMTYPE, RANK, CONSUMABLETYPE, COUNT }
public enum AbilitySortTypes { ALPHA, JOB, PASSIVEALPHA, PASSIVEEQUIPPED, COUNT }
public enum ShopState { BUY, SELL }
public enum DraggableTypes { EQUIPMENT, COUNT }
public enum UITabs { CHARACTER, EQUIPMENT, INVENTORY, SKILLS, RUMORS, OPTIONS, SHOP, COOKING, CRAFTING, NONE, COUNT }
public enum UIWindows { CHARACTER, EQUIPMENT, INVENTORY, SKILLS, RUMORS, OPTIONS, SHOP, DIALOG, COUNT }
public enum MinimapStates { CLOSED, SMALL, LARGE, TRANSLUCENT, MAX }
public enum EFadeStates { NOT_FADING, FADING_IN, FADING_OUT, COUNT }

public class TargetData
{
    public List<Vector2> targetTiles;
    public Vector2 clickedPosition;
    public List<Actor> targetActors;
    public AbilityScript whichAbility;

    public TargetData()
    {
        targetTiles = new List<Vector2>();
        targetActors = new List<Actor>();
        whichAbility = null;
    }

    public string GetAllInfo()
    {
        string info = "";
        info += " CP: " + clickedPosition;
        foreach (Vector2 v2 in targetTiles)
        {
            info += " V2 " + v2;
        }
        foreach (Actor act in targetActors)
        {
            if (act == null) continue;
            info += " T " + act.actorRefName;
        }
        return info;
    }
}

public class OverlayTextData
{
    public string headerText;
    public string refName;
    public string descText;
    public bool introText;
    public bool showOnlyOnce;

    public OverlayTextData()
    {
        headerText = "";
        refName = "";
        descText = "";
        showOnlyOnce = true;
    }
}

public class InventoryUIState
{
    public int indexOfSelectedItem;
    public int listIndexOffset;
    public bool[] filterStates;
    public InventorySortTypes sortType;
    //public UIManagerScript.UIObject objectFocus;

    public InventoryUIState(bool equipment)
    {
        filterStates = new bool[20];
        if (equipment)
        {
            for (int i = 0; i < filterStates.Length; i++)
            {
                filterStates[i] = true;
            }
            filterStates[(int)GearFilters.WEAPON] = false;
            filterStates[(int)GearFilters.ARMOR] = false;
            filterStates[(int)GearFilters.OFFHAND] = false;
            filterStates[(int)GearFilters.ACCESSORY] = false;
        }
        else
        {

            filterStates[(int)ItemFilters.VIEWALL] = true;
            filterStates[(int)ItemFilters.FAVORITES] = false;
        }

    }

    //Set only the index to be true
    public void ClearAllBut(int idx)
    {
        for (int t = 0; t < filterStates.Count(); t++)
        {
            filterStates[t] = (t == idx);
        }
    }
}


[System.Serializable]
public partial class UIManagerScript : MonoBehaviour
{
    public Image gradientBGImageForUltraWide;

    public static bool saveDataComponentsCreated;

    public Switch_PrettyLoadingArtComponent prettyLoadingComponentInScene;

    public static Switch_PrettyLoadingArtComponent prettyLoadingArtComponent;
    /// <summary>
    /// At TurnEndCleanup, if this is true, we'll do RefreshStatuses. We don't need to RefreshStatuses multiple times during the turn.
    /// </summary>
    public static bool statusesDirty = true;

    // We need to turn the dynamic canvas' raycaster OFF when cursor is disabled
    public static GraphicRaycaster dynamicCanvasRaycaster;

    [Header("For Bottom HUD Area")]
    public GameObject uiBGImageBar; // Should be populated for both PC and Switch
    public Switch_PlayerBottomBar Switch_BGImageBar; // Only Switch version has this component, attached to BGImageBar_Switch prefab

    [Header("Other Stuff")]
    public static InventoryUIState lastInventoryState;
    static InventoryUIState lastEquipmentState;
    static bool waitingToClearDialogOptions;

    public static string bufferInfoBarText;

    public float dialogWaitTime;
    public float menuFadeTime;

    public CreditRollScript creditsRoll;
    public EndingCutsceneManager endingCutscene;

    [Tooltip("DLC only, allows us to select Shara mode.")]
    public UI_CampaignSelect campaignSelectManager;

    public static List<ItemFilters> allPossibleInventoryFilters;

    // Tracks whether we are either: Fading Out, Fading In, or Not Fading.
    // This should help avoid conflicts between multiple fades separated only by coroutines.
    static EFadeStates blackFadeState;

    // This array contains things like the Skill Sheet, character sheet, cooking menu... any sort of interactable
    // user interface, NOT including Dialog windows which are their own thing.
    // Items are index by (int)UITabs
    public static GameObject[] menuScreenObjects;

    public static bool bFullScreenUIOpen;

    // This array tracks the state of whether certain windows/menus are closed are open
    // This replaces GetWindowState(UITabs.SKILLS), GetWindowState(UITabs.OPTIONS), etc. which is a more clumsy way of doing it
    public static bool[] gameWindowStates;

    public static bool skippingOverlayText;

    static Transform nwPosHelper;
    static Transform nePosHelper;
    static Transform swPosHelper;
    static Transform sePosHelper;


    public RectTransform myCanvas;
    public static RectTransform endingCanvas;
    public StringBuilder typewriterSB;
    public StringBuilder typewriterSBopen;
    public StringBuilder typewriterSBclose;

    public float typewriterTextSpeed;
    public int charactersPerTypewriterTick;
    public static char[] currentDialogTextArray;
    public static bool typingText;
    public static bool finishTypewriterTextImmediately;
    public static string closeTags;
    public static string hiddenStartSizeTag;
    static Dictionary<string, int> buffStringIntDict;
    static Dictionary<string, int> debuffStringIntDict;
    public static int idOfText;

    static int indexOfActiveHotbar;

    public static bool titleScreenNameInputDone;
    public static bool movingSliderViaKeyboard;

    private static UITabs uiTabSelected;
    public static UITabs lastOpenedFullScreenUI;

    public static string debugText;
    public static Text debugUIElement;

    public static Color colorOrange = new Color(1.0f, 0.4f, 0f);
    public static Color colorMuddy = new Color(0.6862f, 0.5058f, 0.2941f);
    public static Color transparentColor = new Color(1.0f, 1.0f, 1.0f, 0f);
    public static Sprite transparentSprite;

    private static bool fadingToGame;
    public static bool FadingToGame
    {
        get
        {
            return fadingToGame;
        }
        set
        {
            //if (Debug.isDebugBuild) Debug.Log("Setting UIMS fadingToGame to " + value);
            fadingToGame = value;
        }
    }

    // Button assets
    static Sprite pressedGreyButton250x50;
    static Sprite greyButton250x50;

    public const int MAX_HOTBARS = 2;

    public const int UI_GROUP_ACTIVES = 0;
    public const int UI_GROUP_PASSIVES = 1;
    //public const int UI_GROUP_RESERVEPASSIVES = 2;
    public const int UI_GROUP_JOBSKILLS = 3;

    public const int NUM_INGREDIENTS = 18;

    public const int PLAYERNAME_MAX_CHARACTERS = 36;

    static int typewriterCharactersSinceLastSound;
    public int typewriterCharsToPlaySound;

    public static bool infoBarAtBottom;

    public static float jpGainedSinceJobScreenToggled = 100f;

    public const float DIALOG_ANIM_TIME = 0.14f;
    public const float DIALOG_FADEIN_TIME = 0.35f;
    public const float DIALOG_FADEOUT_TIME = 0.15f;
    static int dialogCounter;

    // Delayed message timing

    public static QuestScript bufferQuest;
    static bool[] saveSlotOpen;
    private static SaveDataDisplayBlockInfo[] saveDataInfoBlocks;
    public static SaveDataDisplayBlock[] saveDataDisplayComponents;

    public bool isMouseOverUI;
    public bool isMouseOverGameWorld;

    public static bool requestUpdateScrollbar;
    int requestUpdateScrollbarFrames = 0;

    public static bool requireDoubleConfirm = false;

    public static DialogButtonResponse globalDialogButtonResponse;

    public static int framesAfterDialogOpen;

    public static UIManagerScript singletonUIMS;

    private int hoverUpdateFrames;
    private Vector2 lastHoverPosition;
    private int hoverTurnNumber;    //#question: Is this still used? It's never assigned anywhere

    static float timeAtDialogStart;
    static bool animatingDialog;

    public static bool examineMode;

    //static GameObject regenFlask;
    static TextMeshProUGUI regenFlaskText;

    public float deltaTime;
    //private Texture2D overlayTexture;

    public int renderColumns;
    public int renderRows;
    public static int sRenderColumns;
    public static int sRenderRows;
    private static GameObject mainCamera;
    private GameObject heroPC;
    public GameObject blackFade;

    private GameObject whiteFade;
    public static GameMasterScript staticGMSReference;

    [Header("Fade Markers for Dialog")]
    public GameObject goDialogBehindFade;
    public GameObject goDialogInFrontOfFade;
    static Actor lastHeroTarget;

    static OverlayTextData queuedOverlayTextData;

    bool dialogInteractableDelayed;

    // IMPROVED DIALOG SYSTEM

    public static ButtonCombo selectedButton;
    public static TextBranch currentTextBranch;
    public static Conversation currentConversation;
    public static Queue<ConversationData> conversationQueue;

    // END DIALOG

    // FOR TITLE SCREEN / CHAR CREATION

    public GameObject characterCreationBG;
    CharCreation charCreationManager;
    public GameObject buildText;
    public CanvasGroup switchPromoTextCG;
    public GameObject nameInput;


    // END TITLE SCREEN CHAR CREATION

    private static GameObject playerHUD;

    static int activeWeaponSlot;

    public string editString = "Command?";

    public static bool dbRevealMode; // When true, map reveals all unknown tiles. Used for debugging.

    public static GameObject genericInfoBar;
    static TextMeshProUGUI genericInfoBarText;
    private static GameObject hudPortraitHolder;
    public static Image hudPlayerPortrait;
    public static RectTransform genericInfoBarTransform;
    public static bool forceShowInfoBar;
    public static bool forceInfoBarTop;
    //private static GameObject petInfo;

    // Casino stuff
    private static GameObject slotsGame;
    private static UIObject slotsPlayGame;
    private static UIObject slotsExit;

    static List<GameObject> immutableDialogObjects;

    private static Image slotsImage1;
    private static Image slotsImage2;
    private static Image slotsImage3;
    private static TextMeshProUGUI slotsBet;
    public static TextMeshProUGUI slotsHeader;

    static GameObject blackjackGame;
    static UIObject blackjackPlayGame;
    static UIObject blackjackExit;
    static UIObject blackjackHit;
    static UIObject blackjackStay;

    public static TextMeshProUGUI blackjackPlayerHand;
    public static TextMeshProUGUI blackjackDealerHand;
    public static TextMeshProUGUI blackjackResults;
    public static TextMeshProUGUI blackjackHeader;

    // Player info
    private static TextMeshProUGUI uiPlayerName;
    private static TextMeshProUGUI uiPlayerHealth;
    private static TextMeshProUGUI uiPlayerStamina;
    private static TextMeshProUGUI uiPlayerEnergy;
    private static TextMeshProUGUI uiPlayerXP;
    private static TextMeshProUGUI uiPlayerLevel;
    private static TextMeshProUGUI uiPlayerCT;
    public static GameObject lowHealthDangerFlash;
    private static Image uiPlayerHealthBar;
    private static Image uiPlayerHealthBarFill;
    private static Image uiPlayerStaminaBarFill;
    private static Image uiPlayerEnergyBarFill;
    private static Image uiPlayerXPBarFill;
    private static Image uiPlayerCTBarFill;
    
    //Shara shield info
    private static Image uiPlayerShieldBarFill;
    private static TextMeshProUGUI uiPlayerShieldBarText;

    
    //private static Text uiPlayerWeaponInfo;
    private static TextMeshProUGUI uiPlayerSkillInfo;
    private static GameObject uiPlayerSkillInfoContainer;
    //private static Text uiPlayerXP;
    //private static Text uiPlayerCT;
    private static GameObject uiPlayerLearnSkill;
    public static GameObject uiPortraitExclamation;
    //private static GameObject jobScreenUIButton;

    public static GameObject[] eqArray;

    public static TextMeshProUGUI uiDungeonName;
    public static Image uiDungeonIcon;
    public static Sprite dungeonIconChestOpen;
    public static Sprite dungeonIconChestClosed;


    public static GameObject uiHotbarCursor;
    //public static GameObject uiOptionsMenuCursor;
    public GameObject uiDialogMenuCursor;
    public AudioStuff uiDialogMenuCursorAudioComponent;
    public CursorBounce uiDialogMenuCursorBounce;
    public Image uiDialogMenuCursorImage;
    public float fCursorOpacity = 1.0f;

    public static List<UIObject> allOptionsSliders;
    public static GameObject eqBlinkingCursor;
    public static bool swappingItems;
    public static Equipment itemToSwap;
    public static Item draggingItem;
    public static AbilityScript draggingSkill;
    public int draggingItemButtonIndex;
    public int draggingSkillButtonIndex;
    public static EquipmentSlots slotToSwap;
    public static UIObject hoverObject;

    //Dragging is mouse only, and continually gets stomped if 
    //the mouse isn't being used, so we also need heldGenericObject
    public static ISelectableUIObject draggingGenericObject;
    static ISelectableUIObject heldGenericObject;

    //Can be set by each individual UI screen to have a place to return to
    //when we're not sure where to go after taking an action
    private static UIObject defaultUIFocus;

    static TextMeshProUGUI introOverlayDescText;
    static GameObject introOverlayBG;
    static CanvasGroupFader introOverlayBGFader;
    static CanvasGroupFader introOverlayTextFader;

    static TextMeshProUGUI screenOverlayHeaderText;
    static TextMeshProUGUI screenOverlayDescText;
    static CanvasGroupFader screenOverlayTextCGFader;
    static GameObject screenOverlayBG;
    static GameObject screenOverlayStuff;
    static CanvasGroupFader screenOverlayBGCGFader;

    Resolution[] possibleDisplayResolutions;
    List<Resolution> availableDisplayResolutions;


    public bool uiHotbarNavigating = false;
    int uiHotbarSelected = 0;
    private bool abilityTargeting = false;
    private Directions lineDir = Directions.NORTH;
    private int localTargetOffsetX;
    private int localTargetOffsetY;
    public AbilityScript abilityInTargeting;
    private Vector2 abilityOrigin;
    public Actor abilityUser;

    public TextAsset mouseCursorImageAsset;
    public Sprite mainMouseCursor;
    public Sprite targetingMouseCursor;

    private static GameObject groundTargetingMesh;
    private static GameObject cursorTargetingMesh;
    public bool gameStarted = false;

    private Vector2 lastCursorPosition;
    public Vector2 lastPhysicalMousePosition;
    private Vector2 virtualCursorPosition;

    public static GameObject[] abilityIcons;
    public static GameObject[] weaponHolders; // Invisible outer shell for alignment
    public static Image[] weaponItemIcons; // The item sprite
    public static GameObject[] weaponBoxes; // The UI wrapper
    public static Image[] buffIcons;
    public static Image[] debuffIcons;
    public static Image[] buffCountIcons;
    public static Image[] debuffCountIcons;

    public static Sprite[] allUIGraphics;
    public static Dictionary<string, Sprite> dictUIGraphics;
    public static Sprite[] quickslotNumbers;
    public static HotbarBindable[] hotbarAbilities;
    public static Sprite[] allItemGraphics;
    public static Dictionary<string, Sprite> dictItemGraphics;
    public static Sprite[] eqFilterButtons;
    public static Sprite[] invFilterButtons;
    public static Sprite[] playingCardSprites;
    public static Weapon[] hotbarWeapons;
    private static StatusEffect[] playerBuffs;
    private static StatusEffect[] playerDebuffs;

    public static string[] portraitNames;
    public static Sprite[] allPortraits;

    private GameMasterScript gms;

    public static string[] weaponBoxPaths;

    // *** MENU SYSTEM ***
    //public static bool GetWindowState(UITabs.OPTIONS);
    private static GameObject uiOptionsMenu;
    private static List<UIObject> uiOptionsObjects;
    public static List<GameObject> uiObjectsWithHighlights;
    public static bool highlightingOptionsObject;
    public static GameObject highlightedOptionsObject;
    public static OptionsSlider optionsSliderSelected;

    public static TextMeshProUGUI worldSeedText;

    public static UIObject optionsResolution;
    public static UIObject optionsZoomScale;
    public static UIObject optionsMusicVolume;
    public static UIObject optionsSFXVolume;
    public static UIObject optionsFootstepsVolume;
    public static UIObject optionsAudioOffWhenMinimized;
    public static UIObject optionsFramecap;
    public static UIObject optionsTextSpeed;
    public static UIObject optionsBattleTextSpeed;
    public static UIObject optionsBattleTextScale;
    public static UIObject optionsButtonDeadZone;
    public static TextMeshProUGUI optionsButtonDeadZoneText;
    public static TextMeshProUGUI optionsCursorRepeatDelayText;
    public static TextMeshProUGUI optionsFrameCapText;
    public static TextMeshProUGUI optionsResolutionText;
    public static TextMeshProUGUI optionsTextSpeedText;
    public static TextMeshProUGUI optionsBattleTextSpeedText;
    public static TextMeshProUGUI optionsBattleTextScaleText;
    public static UIObject optionsCursorRepeatDelay;
    public static UIObject optionsFullscreen;
    public static UIObject optionsCameraScanlines;
    public static UIObject optionsShowTutorialPopups;
    public static UIObject optionsShowJPXPGain;
    public static UIObject optionsPlayerHealthBar;
    public static UIObject optionsMonsterHealthBars;
    public static UIObject optionsScreenFlashes;
    public static UIObject optionsSmallCombatLogText;
    public static UIObject optionsUIPulses;
    public static UIObject optionsShowRumorOverlay;
    public static UIObject optionsAutoEatFood;
    public static UIObject optionsAutoAbandonRumors;
    public static UIObject optionsAutoUsePlanks;
    public static UIObject optionsAutoEquipBestOffhand;
    public static UIObject optionsAutoEquipWeapons;
    public static UIObject optionsVerboseCombatLog;
    public static UIObject optionsPickupDisplay;
    public static UIObject optionsExtraTurnDisplay;
    public static UIObject optionsGridOverlay;
    public static UIObject optionsInputManager;
    public static UIObject optionsViewHelp;
    public static UIObject optionsSaveAndQuit;
    public static UIObject optionsSaveAndBackToTitle;
    public static UIObject optionsKeybindWASD;
    public static UIObject optionsKeybind2Hands;
    public static UIObject optionsShowControllerPrompts;
    public static UIObject optionsUseStepMoveJoystickStyle;
    public static UIObject optionsDisableMouseOnKeyJoystick;
    public static UIObject optionsDisableMouseMovement;
    //private static UIObject optionsBackToGame;

    public static GameObject optionsMusicVolumeContainer;
    public static GameObject optionsSFXVolumeContainer;
    public static GameObject optionsFootstepsVolumeContainer;
    public static GameObject optionsCursorRepeatDelayContainer;
    public static GameObject optionsButtonDeadZoneContainer;
    public static GameObject optionsZoomScaleContainer;
    public static GameObject optionsFrameCapContainer;
    public static GameObject optionsTextSpeedContainer;
    public static GameObject optionsBattleTextSpeedContainer;
    public static GameObject optionsBattleTextScaleContainer;
    static GameObject optionsResolutionContainer;

    public static bool dialogBoxOpen;
    public static bool waitingToOpenDialog;
    public static Conversation waitingForDialog;
    public static bool prevDialogBoxOpen;
    public static bool nameInputOpen;
    public static bool ignoreNextButtonConfirm;
    public static DialogType dialogBoxType;
    public static string lastDialogActionRefSelected;
    public static DialogButtonResponse lastDialogDBRSelected;

    // *** JOB SHEET

    public static List<UIObject> allUIObjects;


    // *** CHARACTER SHEET, INV, EQUIPMENT...

    //public static bool GetWindowState(UITabs.RUMORS);
    public static GameObject questSheet;
    //public static Text questSheetText;

    //public static bool GetWindowState(UITabs.CHARACTER);
    public static GameObject characterSheet;

    public static GameObject gameplayJobSelectionScreen;

    public static Image csPortrait;
    public static TextMeshProUGUI csName;
    public static TextMeshProUGUI csLevel;
    public static TextMeshProUGUI csExperience;
    public static TextMeshProUGUI csExplore;
    public static TextMeshProUGUI csMonster;
    public static TextMeshProUGUI csFavorites;
    public static TextMeshProUGUI csResources;
    public static TextMeshProUGUI csStrength;
    public static TextMeshProUGUI csSwiftness;
    public static TextMeshProUGUI csSpirit;
    public static TextMeshProUGUI csDiscipline;
    public static TextMeshProUGUI csGuile;
    public static TextMeshProUGUI csWeaponPower;
    public static TextMeshProUGUI csSpiritPower;
    public static TextMeshProUGUI csCritChance;

    public static GameObject csStrengthLabel;
    public static GameObject csSwiftnessLabel;
    public static GameObject csSpiritLabel;
    public static GameObject csDisciplineLabel;
    public static GameObject csGuileLabel;
    public static GameObject csWeaponPowerLabel;
    public static GameObject csSpiritPowerLabel;
    public static GameObject csCritChanceLabel;

    public static UIObject csStrengthUIObject;
    public static UIObject csSwiftnessUIObject;
    public static UIObject csSpiritUIObject;
    public static UIObject csDisciplineUIObject;
    public static UIObject csGuileUIObject;
    public static UIObject csWeaponPowerUIObject;
    public static UIObject csSpiritPowerUIObject;
    public static UIObject csCritChanceUIObject;

    public static TextMeshProUGUI csCritDamage;
    public static TextMeshProUGUI csChargeTime;
    public static TextMeshProUGUI csPhysicalResist;
    public static TextMeshProUGUI csFireResist;
    public static TextMeshProUGUI csWaterResist;
    public static TextMeshProUGUI csPoisonResist;
    public static TextMeshProUGUI csLightningResist;
    public static TextMeshProUGUI csShadowResist;

    public static TextMeshProUGUI csPhysicalDamage;
    public static TextMeshProUGUI csFireDamage;
    public static TextMeshProUGUI csWaterDamage;
    public static TextMeshProUGUI csPoisonDamage;
    public static TextMeshProUGUI csLightningDamage;
    public static TextMeshProUGUI csShadowDamage;

    public static TextMeshProUGUI csParryChance;
    public static TextMeshProUGUI csBlockChance;
    public static TextMeshProUGUI csDodgeChance;
    public static TextMeshProUGUI csPowerupDrop;
    public static TextMeshProUGUI csDamageMod;
    public static TextMeshProUGUI csDefenseMod;
    public static TextMeshProUGUI csMoney;
    public static TextMeshProUGUI csMoneyLabel;
    public static TextMeshProUGUI csStatusEffects;
    public static TextMeshProUGUI csFeatsText;

    public static GameObject csChargeTimeLabel;
    public static GameObject csPowerupDropLabel;
    public static GameObject csDamageModlabel;
    public static GameObject csDefenseModlabel;
    public static GameObject csParryChanceLabel;

    public static UIObject csChargeTimeUIObject;
    public static UIObject csPowerupDropUIObject;
    public static UIObject csDamageModUIObject;
    public static UIObject csDefenseModUIObject;
    public static UIObject csParryChanceUIObject;


    public static UIObject[] csObjectButtons;

    public static GameObject charSheetStatImage;
    public static TextMeshProUGUI charSheetStatInfo; // Hover text.
    public static GameObject currentCharsheetTooltipObject;

    // public static bool GetWindowState(UITabs.EQUIPMENT);
    //public static bool GetWindowState(UITabs.INVENTORY);
    public static bool casinoGameOpen;

    public static UIObject jobExit;
    public static UIObject skillExit;



    public static GameObject shopScrollbar;
    public static GameObject skillActiveScrollbar;
    public static GameObject skillSupportScrollbar;
    //public static GameObject skillReserveScrollbar;
    public static GameObject eqScrollbar;
    public static GameObject itemWorldScrollbar;
    public static GameObject corralFoodScrollbar;
    public static GameObject invScrollbar;


    //public static UIObject journalExit;


    // EQ Submenu stuff

    public static Equipment eqItemSelected;
    public static Item invItemSelected;
    public static UIObject lastUIFocus;
    public static GameObject eqSubmenu;
    public static bool eqSubmenuOpen;
    public static UIObject[] eqSubmenuObjects;
    public static UIObject eqSubmenuEquip;
    public static UIObject eqSubmenuPairWithMainhand;
    public static UIObject eqSubmenuUnequip;
    public static UIObject eqSubmenuEquipAcc1;
    public static UIObject eqSubmenuEquipAcc2;
    public static UIObject eqSubmenuEquipOffhand;
    public static UIObject eqSubmenuHotkey1;
    public static UIObject eqSubmenuHotkey2;
    public static UIObject eqSubmenuHotkey3;
    public static UIObject eqSubmenuHotkey4;
    public static UIObject eqSubmenuDrop;
    public static UIObject eqSubmenuFavorite;

    public static GameObject invSubmenu;
    public static bool invSubmenuOpen;
    public static UIObject[] invSubmenuObjects;
    public static UIObject invSubmenuUse;
    public static UIObject invSubmenuDrop;
    public static UIObject invSubmenuFavorite;

    // Item info blocks.


    public static GameObject eqTooltipContainer;
    public static GameObject eqItemInfoHolder;
    public static RectTransform eqItemInfoTextRect;
    public static RectTransform eqTooltipContainerRect;
    public static RectTransform invItemInfoTextRect;
    public static GameObject invTooltipContainer;
    public static GameObject invItemInfoHolder;
    public static UIObject mainUINav;
    public static TextMeshProUGUI uiNavHeaderText;
    public static UIObject[] uiNavButtons;
    public static UIObject uiNavCharacter;
    public static UIObject uiNavEquipment;
    public static UIObject uiNavInventory;
    public static UIObject uiNavSkills;
    public static UIObject uiNavRumors;
    public static UIObject uiNavOptions;
    public static Sprite[] uiNavSelected;
    public static Sprite[] uiNavDeselected;
    public static TextMeshProUGUI eqItemInfoText;
    public static TextMeshProUGUI eqItemInfoName;
    public static Image eqItemInfoImage;
    public static TextMeshProUGUI eqComparisonAreaText;

    public static TextMeshProUGUI eqCompItemInfoText;
    public static TextMeshProUGUI eqCompItemInfoName;
    public static Image eqCompItemInfoImage;
    public static TextMeshProUGUI eqCompHeader;
    public static TextMeshProUGUI invItemInfoText;
    public static TextMeshProUGUI invItemInfoName;
    public static Image invItemInfoImage;

    public static GameObject inventorySheet;
    public static GameObject equipmentSheet;
    public static UIObject[] eqWeapons;
    public static List<UIObject> eqFilterObjects;
    public static List<UIObject> invFilterObjects;
    public static List<UIObject> eqObjectsWithBorders;
    public static List<UIObject> skillObjectsWithBorders;
    public static List<UIObject> invObjectsWithBorders;
    public static Image[] eqWeaponSprites;
    public static UIObject[] eqPlayerEquipment;

    public static string greenHexColor;
    public static string orangeHexColor;
    public static string brightOrangeHexColor;
    public static string blueHexColor;
    public static string cyanHexColor;
    public static string redHexColor;
    public static string purpleHexColor;
    public static string lightPurpleHexColor;
    public static string customLegendaryColor;
    public static string goldHexColor;
    public static Color purpleRGBColor;
    public static string silverHexColor;
    public static string favoriteStar;
    public static string vendorTrashMark = "-";

    public static List<GameObject> statModifierIcons;

    // Cooking UI

    //public static bool GetWindowState(UITabs.COOKING);
    public static int[] ingredientQuantities;
    public static GameObject cookingUI;
    public static Item[] cookingIngredientItems;
    public static Item cookingSeasoningItem;
    public static Item cookingResultItem;
    public static Item[] lastCookedItems;

    public static Item[] cookingPlayerIngredientList;
    public static Item[] cookingPlayerSeasoningList;

    public static UIObject cookingExit;
    public static UIObject cookingReset;
    public static UIObject cookingRepeat;
    public static GameObject[] ingredientsHolders;
    public static TextMeshProUGUI[] ingredientsQuantityText;
    public static UIObject[] ingredients;
    public static GameObject[] seasoningHolders;
    public static TextMeshProUGUI[] seasoningQuantityText;
    public static UIObject[] seasoning;
    public static GameObject panIngredient1Holder;
    public static GameObject panIngredient2Holder;
    public static GameObject panIngredient3Holder;
    public static GameObject panIngredient1;
    public static GameObject panIngredient2;
    public static GameObject panIngredient3;
    public static GameObject panSeasoningHolder;
    public static GameObject panSeasoning;
    public static GameObject cookingResultImageHolder;
    public static GameObject cookingResultImage;
    public static UIObject cookButton;
    public static TextMeshProUGUI cookingResultText;

    // End cooking UI

    /* public static Text eqPlayerStats1;
    public static Text eqPlayerStats2;
    public static Text eqPlayerStats3;
    public static Text eqPlayerStats4; */

    /* public static GameObject eqPlayerHealth;
    public static GameObject eqPlayerStamina;
    public static GameObject eqPlayerEnergy;
    public static GameObject eqPlayerStrength;
    public static GameObject eqPlayerSwiftness;
    public static GameObject eqPlayerSpirit;
    public static GameObject eqPlayerDiscipline;
    public static GameObject eqPlayerGuile;

    public static GameObject eqPlayerMeleePower;
    //public static GameObject eqPlayerRangedPower;
    public static GameObject eqPlayerSpiritPower;
    public static GameObject eqPlayerCrit;

    public static GameObject eqPlayerParry;
    public static GameObject eqPlayerBlock;
    public static GameObject eqPlayerDodge;
    public static GameObject eqPlayerPhysicalRes;
    public static GameObject eqPlayerFireRes;
    public static GameObject eqPlayerWaterRes;
    public static GameObject eqPlayerPoisonRes;
    public static GameObject eqPlayerShadowRes;
    public static GameObject eqPlayerLightningRes;

    public static GameObject eqPlayerCTGain;
    public static GameObject eqPlayerMoney; */

    public static GameObject eqEquippedWeapon;

    //public static UIObject eqItemInfo;
    //public static GameObject invItemInfo;

    public static GameObject eqWeaponHighlight;

    public static int listArrayIndexOffset;
    public static int selectedUIObjectGroup;

    public static List<Item> playerItemList;
    public static List<Item> shopItemList;
    public static InventorySortTypes invSortItemType;
    public static ItemTypes invFilterItemType;
    public static bool[] itemFilterTypes;
    public static TextMeshProUGUI invPlayerMoney;
    public static TextMeshProUGUI eqPlayerMoney;

    public static UIObject invFilterViewAll;
    public static UIObject invFilterSelfBuff;
    public static UIObject invFilterSummon;
    public static UIObject invFilterValuables;
    public static UIObject invFilterUtility;
    public static UIObject invFilterFavorites;
    public static UIObject invFilterRecovery;
    public static UIObject invFilterOffense;
    // NEW EQ STUFF
    public static UIObject invItemListButton1;
    public static UIObject invItemListButton2;
    public static UIObject invItemListButton3;
    public static UIObject invItemListButton4;
    public static UIObject invItemListButton5;
    public static UIObject invItemListButton6;
    public static UIObject invItemListButton7;
    public static UIObject invItemListButton8;
    public static UIObject invItemListButton9;
    public static UIObject invItemListButton10;
    public static UIObject invItemListButton11;
    public static UIObject invItemListButton12;
    public static UIObject invItemListButton13;
    public static UIObject invItemListButton14;
    public static UIObject invItemListButton15;
    public static UIObject invItemListButton16;
    public static UIObject invItemListButton17;
    public static UIObject invItemListButton18;
    public static UIObject invItemListButton19;
    public static UIObject invItemListButton20;
    public static UIObject invItemListButton21;
    public static UIObject invItemListButton22;
    public static UIObject invItemListButton23;
    public static UIObject invItemListButton24;
    public static UIObject invItemListButton25;
    public static UIObject invItemListButton26;
    public static UIObject invItemListButton27;



    public static UIObject invItemSortAZ;
    public static UIObject invItemSortValue;
    public static UIObject invItemSortRank;

    public static bool[] sortForward;

    public static UIObject[] hudHotbarAbilities;
    public static UIObject hudHotbarFlask;
    public static UIObject hudHotbarPortal;
    public static UIObject hudHotbarSnackBag;
    public static UIObject hudHotbarSkill1;
    public static UIObject hudHotbarSkill2;
    public static UIObject hudHotbarSkill3;
    public static UIObject hudHotbarSkill4;
    public static UIObject hudHotbarSkill5;
    public static UIObject hudHotbarSkill6;
    public static UIObject hudHotbarSkill7;
    public static UIObject hudHotbarSkill8;

    public static UIObject hudHotbarSkill9;
    public static UIObject hudHotbarSkill10;
    public static UIObject hudHotbarSkill11;
    public static UIObject hudHotbarSkill12;
    public static UIObject hudHotbarSkill13;
    public static UIObject hudHotbarSkill14;
    public static UIObject hudHotbarSkill15;
    public static UIObject hudHotbarSkill16;

    public static int indexOfActiveSkillSelected;
    public static UIObject[] skillHotbar;
    public static UIObject skillHotbar1;
    public static UIObject skillHotbar2;
    public static UIObject skillHotbar3;
    public static UIObject skillHotbar4;
    public static UIObject skillHotbar5;
    public static UIObject skillHotbar6;
    public static UIObject skillHotbar7;
    public static UIObject skillHotbar8;

    public static UIObject skillHotbar9;
    public static UIObject skillHotbar10;
    public static UIObject skillHotbar11;
    public static UIObject skillHotbar12;
    public static UIObject skillHotbar13;
    public static UIObject skillHotbar14;
    public static UIObject skillHotbar15;
    public static UIObject skillHotbar16;

    public static UIObject[] invHotbar;
    public static UIObject invHotbar1;
    public static UIObject invHotbar2;
    public static UIObject invHotbar3;
    public static UIObject invHotbar4;
    public static UIObject invHotbar5;
    public static UIObject invHotbar6;
    public static UIObject invHotbar7;
    public static UIObject invHotbar8;

    public static UIObject invHotbar9;
    public static UIObject invHotbar10;
    public static UIObject invHotbar11;
    public static UIObject invHotbar12;
    public static UIObject invHotbar13;
    public static UIObject invHotbar14;
    public static UIObject invHotbar15;
    public static UIObject invHotbar16;

    //public static UIObject invUseItem;

    public static TextMeshProUGUI eqGearBonusText;

    //public static Image eqEquippedWeaponBar;
    public static UIObject eqWeapon1;
    public static UIObject eqWeapon2;
    public static UIObject eqWeapon3;
    public static UIObject eqWeapon4;
    public static UIObject eqOffhand;
    public static UIObject eqArmor;
    public static UIObject eqAccessory1;
    public static UIObject eqAccessory2;
    public static UIObject eqItemListDummyButton;
    public static UIObject invItemListDummyButton;
    public static UIObject eqItemListButton1;
    public static UIObject eqItemListButton2;
    public static UIObject eqItemListButton3;
    public static UIObject eqItemListButton4;
    public static UIObject eqItemListButton5;
    public static UIObject eqItemListButton6;
    public static UIObject eqItemListButton7;
    public static UIObject eqItemListButton8;
    public static UIObject eqItemListButton9;
    public static UIObject eqItemListButton10;
    public static UIObject eqItemListButton11;
    public static UIObject eqItemListButton12;
    public static UIObject eqItemListButton13;
    public static UIObject eqItemListButton14;
    public static UIObject eqItemListButton15;
    public static UIObject eqItemListButton16;
    public static UIObject eqItemListButton17;
    public static UIObject eqItemListButton18;
    public static UIObject eqItemListButton19;
    public static UIObject eqItemListButton20;
    public static UIObject eqItemListButton21;
    public static UIObject eqItemListButton22;

    public static UIObject eqSortAZ;
    public static UIObject eqSortItemType;
    public static UIObject eqSortRare;
    public static UIObject eqSortRank;

    public static UIObject eqFilterViewAll;
    public static UIObject eqFilterWeapons;
    public static UIObject eqFilterOffhand;
    public static UIObject eqFilterArmor;
    public static UIObject eqFilterAccessory;

    public static UIObject eqFilterMagical;
    public static UIObject eqFilterCommon;
    public static UIObject eqFilterLegendary;
    public static UIObject eqFilterGearSet;

    public static TextMeshProUGUI eqFilterCommonText;
    public static TextMeshProUGUI eqFilterMagicalText;
    public static TextMeshProUGUI eqFilterLegendaryText;
    public static TextMeshProUGUI eqFilterGearSetText;

    public static GameObject eqDragger;
    public static GameObject invDragger;
    public static GameObject skillDragger;
    public static GameObject cookingDragger;
    public int framesSinceDragUp;
    public static bool isDraggingItem;
    public static bool isDraggingSkill;

    public static UIObject[] eqItemButtons;
    public static UIObject[] invItemButtons;

    // END NEW EQ STUFF

    public static Item selectedItem;
    static Item mouseHoverItem;
    public static EquipmentSlots eqSlotSelected;
    public static int eqSlotIndexSelected;
    public static int invSlotIndexSelected;

    public static int[] cursorPosition;
    public static UIObject uiObjectFocus;

    // *** SKILL SHEET

    public static bool swappingSkill;
    public static bool swappingHotbarAction;
    public static bool activeSkillSelectedInList;
    public static AbilityScript skillSelectedInList;
    public static AbilityScript skillToReplace;
    public static int skillIndexToReplace;
    public static int hotbarIndexToReplace;
    public static AbilityScript skillReplaceWith;
    //public static bool GetWindowState(UITabs.SKILLS);
    public static GameObject gamblerHand;
    public static List<GameObject> gamblerHandObjects;
    public static List<PlayingCard> cardsInUIArea;

    // Object pooling

    static HashSet<Vector2> returnTileList;

    // *** JOB SHEET
    public static bool jobSheetOpen;
    public static GameObject jobSheet;

    public static Scrollbar jobBonusScrollbar;
    public static Scrollbar eqBonusScrollbar;

    public static Text jobLore;
    public static TextMeshProUGUI jobInnate;
    public static Text jobAbilInfo;
    public static Text jobName;
    public static Text jobSkillHeader;
    public static UIObject[] jobAbilButtons;
    public static UIObject jobAbilListButton1;
    public static UIObject jobAbilListButton2;
    public static UIObject jobAbilListButton3;
    public static UIObject jobAbilListButton4;
    public static UIObject jobAbilListButton5;
    public static UIObject jobAbilListButton6;
    public static UIObject jobAbilListButton7;
    public static UIObject jobAbilListButton8;
    public static UIObject jobAbilListButton9;
    public static UIObject jobAbilListButton10;
    public static UIObject jobAbilListButton11;
    public static UIObject jobAbilListButton12;
    public static UIObject jobAbilListButton13;
    public static UIObject jobAbilListButton14;

    public static List<JobAbility> playerJobAbilities;
    public static List<AbilityScript> playerActiveAbilities;
    public static List<AbilityScript> playerSupportAbilities;
    //public static List<AbilityScript> playerReservePassives;
    public static AbilityScript selectedAbilitySkillSheet;

    public static JobAbility selectedAbility;
    //public static AbilityScript selectedKnownAbility;
    //public static List<AbilityScript> knownAbilities;

    // Fill this with things like job abilities or equipment slots, stuff where the # of buttons is constant but the content is dynamic
    public static List<GameObject> uiInteractButtons;

    // *** DIALOG SYSTEM
    // #dialogrefactor Make sure most of these are cleaned out

    private static List<GameObject> dialogObjects;
    public static List<UIObject> dialogUIObjects;

    private GameObject dialogValueSliderParent;
    private TextMeshProUGUI dialogValueSliderText;
    private static string defaultSliderText;
    private Slider dialogValueSlider;

    static GameObject genericTextInput;
    static Image dialogBoxImage;
    static GameObject dialogBoxImageLayoutParent;
    public static TMP_InputField genericTextInputField;
    static TextMeshProUGUI genericTextInputPlaceholder;
    public static TextMeshProUGUI genericTextInputText;

    public static List<GameObject> nonDialogHUDElements;
    public static bool playerHUDEnabled = true;

    // END DIALOG SYSTEM ***       

    // Loading bar

    public static GameObject loadingGroup;
    public static Image loadingBar;
    public static TextMeshProUGUI loadingBarText;

    private static KeyCode[] IntToKeyCode;

    public static CursorUpdateData waitToUpdateCursorData;

    //When true, the cursor is not drawn. Mainly used for situations when
    //the cursor would be confusing, such as when the OptionsUI is being navigated
    //by the mouse.
    public static bool bForceHideCursor;

    [Header("Searchbars")]
    public TMP_InputField shopSearchbar;
    public TMP_InputField itemWorldSearchbar;

    public static bool RequireDoubleConfirm()
    {
        return requireDoubleConfirm;
    }

    public static void ResetAllVariablesToGameLoad(DialogButtonResponse dbr = DialogButtonResponse.NOTHING)
    {
        framesAfterDialogOpen = 0;
        examineMode = false;
        activeWeaponSlot = 0;
        GameLogScript.journalLogStringBuffer.Clear();
        groundTargetingMesh = null;
        cursorTargetingMesh = null;
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            ClearHotbar(i);
        }
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            hotbarWeapons[i] = null;
        }
        for (int i = 0; i < playerBuffs.Length; i++)
        {
            playerBuffs[i] = null;
        }
        for (int i = 0; i < playerDebuffs.Length; i++)
        {
            playerDebuffs[i] = null;
        }

        dialogBoxOpen = false;
        jobSheetOpen = false;
        //optionsCursorPosition = 0;
        //dialogCursorPosition = 0;
        SetListOffset(0);
        //listArrayIndexPosition = 0;
        requireDoubleConfirm = false;
        bFullScreenUIOpen = false;
        UIManagerScript.SetGlobalResponse(dbr);

        //This wasn't being cleared on character reset
        dialogBoxType = DialogType.STANDARD;

        bufferQuest = null;
        singletonUIMS = null;
        mainCamera = null;
        staticGMSReference = null;
        lastHeroTarget = null;
        itemToSwap = null;
        draggingItem = null;
        draggingSkill = null;
        eqItemSelected = null;
        invItemSelected = null;
        cookingSeasoningItem = null;
        cookingSeasoningItem = null;
        cookingResultItem = null;
        lastCookedItems = null;
        playerItemList = new List<Item>();
        shopItemList = new List<Item>();
        selectedItem = null;
        mouseHoverItem = null;
        skillSelectedInList = null;
        skillToReplace = null;
        skillReplaceWith = null;
        cardsInUIArea = null;
        playerJobAbilities = new List<JobAbility>();
        playerActiveAbilities = new List<AbilityScript>();
        playerSupportAbilities = new List<AbilityScript>();
        selectedAbilitySkillSheet = null;
        selectedAbility = null;
    }

    public static void SetWindowState(UITabs tab, bool state)
    {
        if (gameWindowStates[(int)tab] != state)
        {
            //Debug.Log("Window " + tab + " is currently " + gameWindowStates[(int)tab] + " and will be set to " + state);
        }
        gameWindowStates[(int)tab] = state;
    }

    public static bool GetWindowState(UITabs tab)
    {
        return gameWindowStates[(int)tab];
    }

    public void Awake()
    {
        singletonUIMS = this;

        if (prettyLoadingArtComponent == null)
        {
            prettyLoadingArtComponent = prettyLoadingComponentInScene;
        }


        bool firstInitialization = false;

        if (saveDataDisplayComponents == null)
        {
            saveDataDisplayComponents = new SaveDataDisplayBlock[GameMasterScript.kNumSaveSlots];
            saveDataComponentsCreated = true;
            firstInitialization = true;
        }
        
        if (saveDataInfoBlocks == null)
        {
            saveDataInfoBlocks = new SaveDataDisplayBlockInfo[GameMasterScript.kNumSaveSlots];
            firstInitialization = true;
        }
        
        saveSlotOpen = new bool[(int)GameMasterScript.kNumSaveSlots];
        for (int i = 0; i < saveSlotOpen.Length; i++)
        {
            if (firstInitialization)
            {
                saveDataInfoBlocks[i] = new SaveDataDisplayBlockInfo();
                saveDataInfoBlocks[i].id = i;
            }
            else
            {
                saveDataInfoBlocks[i].Clear();
            }

            saveSlotOpen[i] = false;
        }
        gameWindowStates = new bool[(int)UITabs.COUNT];
        menuScreenObjects = new GameObject[(int)UITabs.COUNT]; // number of ui tabs

        possibleDisplayResolutions = Screen.resolutions;
        availableDisplayResolutions = new List<Resolution>();
        conversationQueue = new Queue<ConversationData>();

            //Debug.Log("Attempting to link dialog cursor.");            
        TryLoadDialogCursor();
        DoLoadPrettyLoadingScreen();

    }

    public void Start()
    {

        firstCCInitialize = true;
        // We might need portraits by now?
        UIManagerScript.singletonUIMS.TryLoadingAllPortraits();

        validResponsesInCurrentTextBranch = new List<ButtonCombo>();

        nextPageResponseButton = new ButtonCombo();
        nextPageResponseButton.actionRef = "changepages";
        nextPageResponseButton.dialogEventScript = "ChangeDialogPages";
        nextPageResponseButton.dialogEventScriptValue = "next";
        nextPageResponseButton.dbr = DialogButtonResponse.CONTINUE;
        
        previousPageResponseButton = new ButtonCombo();
        previousPageResponseButton.actionRef = "changepages";
        previousPageResponseButton.dialogEventScript = "ChangeDialogPages";
        previousPageResponseButton.dialogEventScriptValue = "previous";
        previousPageResponseButton.dbr = DialogButtonResponse.CONTINUE;
        // Next and Previous page button text was being set here to English only
        // But we need to pull the localized strings. However the StringManager hasn't fully loaded yet.
        // So... this is being moved to MainGameStart.

        allPossibleInventoryFilters = new List<ItemFilters>();
        allPossibleInventoryFilters.Add(ItemFilters.RECOVERY);
        allPossibleInventoryFilters.Add(ItemFilters.OFFENSE);
        allPossibleInventoryFilters.Add(ItemFilters.SUMMON);
        allPossibleInventoryFilters.Add(ItemFilters.SELFBUFF);
        allPossibleInventoryFilters.Add(ItemFilters.VALUABLES);
        allPossibleInventoryFilters.Add(ItemFilters.UTILITY);

        lastInventoryState = new InventoryUIState(false);
        lastEquipmentState = new InventoryUIState(true);
        buffStringIntDict = new Dictionary<string, int>();
        debuffStringIntDict = new Dictionary<string, int>();
        sortForward = new bool[(int)InventorySortTypes.COUNT];
        infoBarAtBottom = false;
        greenHexColor = "<#40b843>";
        orangeHexColor = "<#ffc80a>";
        brightOrangeHexColor = "<#ffd20a>";
        blueHexColor = "<#25a3dd>";
        redHexColor = "<#f20a0a>";
        cyanHexColor = "<#0cffe6>";
        purpleHexColor = "<#7440ad>";
        lightPurpleHexColor = "<#bf42f4>";
        customLegendaryColor = "<#fc4b71>";
        goldHexColor = "<#f2b02e>";
        purpleRGBColor = new Color(0.6f, 0.2f, 1f);
        silverHexColor = "<#79a2ab>";

        favoriteStar = brightOrangeHexColor + "*</color>";

        singletonUIMS = this;
        InitializeDialogBoxComponent();

        Vector2 hotspot = Vector2.zero;
        blackFade = GameObject.Find("BlackFade");
        whiteFade = GameObject.Find("WhiteFade");
        //Cursor.SetCursor(mainMouseCursor.texture, hotspot, mode);
        waitToUpdateCursorData = new CursorUpdateData();
        returnTileList = new HashSet<Vector2>();
        itemFilterTypes = new bool[20];



        lastOpenedFullScreenUI = UITabs.EQUIPMENT;
        ShopUIScript shop = GetComponentInChildren<ShopUIScript>();
        if (shop != null)
        {
            shop.Initialize();
        }


    }            

    public void ViewHelp(int x)
    {
        GameMasterScript.tutorialManager.ViewHelp();
    }
   
    public int GetIndexOfSelectedButton()
    {
        //Debug.Log(dialogBoxOpen);
        //if (uiObjectFocus != null) Debug.Log(uiObjectFocus.gameObj.name);
        //Check dialog boxes as they are more important than the UI they're standing on.

        if (dialogBoxOpen || casinoGameOpen)
        {
            for (int i = 0; i < dialogUIObjects.Count; i++)
            {
                if (uiObjectFocus == dialogUIObjects[i])
                {
                    return i;
                }
            }
            if (uiObjectFocus == null && dialogUIObjects.Count > 0)
            {
                ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
                return 0;
            }
        }

        //New UI method may not use this function at all.
        if (currentFullScreenUI != null)
        {
            return 0;
        }

        //old stuff after here
        //todo: remove checks for Equipment, Inventory, and Skills from below.

        int maxUIObjectButtons = 0;
        int maxItemsInList = 0;
        UIObject topOfList = null;

        //Debug.Log("Searching for index of selectedbutton, offset is " + listArrayIndexOffset);

        if (GetWindowState(UITabs.COOKING))
        {
            if (uiObjectFocus == cookingExit)
            {
                return -1;
            }
            for (int i = 0; i < ingredients.Length; i++)
            {
                if (uiObjectFocus == ingredients[i])
                {
                    return i;
                }
            }
            for (int i = 0; i < seasoning.Length; i++)
            {
                if (uiObjectFocus == seasoning[i])
                {
                    return 100 + i;
                }
            }
            if (uiObjectFocus == cookButton)
            {
                return 200;
            }
        }
        else if (ShopUIScript.CheckShopInterfaceState())
        {
            maxUIObjectButtons = ShopUIScript.shopItemButtonList.Length;
            maxItemsInList = ShopUIScript.playerItemList.Count;// - 1; // Why -1
            if (ShopUIScript.playerItemList.Count == 1) // This is janky. Why is it needed
            {
                maxItemsInList = 1;
            }
            topOfList = ShopUIScript.shopItemButton1;
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            maxUIObjectButtons = ItemWorldUIScript.itemListButtons.Length;
            maxItemsInList = ItemWorldUIScript.playerItemList.Count;// - 1; // Why -1
            if (ItemWorldUIScript.playerItemList.Count == 1) // This is janky. Why is it needed
            {
                maxItemsInList = 1;
            }
            topOfList = ItemWorldUIScript.itemListButtons[0];
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            maxUIObjectButtons = MonsterCorralScript.corralFoodButtons.Length;
            maxItemsInList = MonsterCorralScript.playerItemList.Count;// - 1; // Why -1
            if (MonsterCorralScript.playerItemList.Count == 1) // This is janky. Why is it needed
            {
                maxItemsInList = 1;
            }
            topOfList = MonsterCorralScript.corralFoodButtons[0];
        }
        else if (GetWindowState(UITabs.SKILLS))
        {
        }

        if (casinoGameOpen)
        {
            for (int i = 0; i < dialogUIObjects.Count; i++)
            {
                if (uiObjectFocus == dialogUIObjects[i])
                {
                    return i;
                }
            }
            if ((uiObjectFocus == null) && (dialogUIObjects.Count > 0))
            {
                ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
            }
        }
        else if (jobSheetOpen)
        {
            if (maxItemsInList <= 0)
            {
                ChangeUIFocusAndAlignCursor(jobExit);
                return -1;
            }
            for (int i = 0; i < jobAbilButtons.Length; i++)
            {
                if (uiObjectFocus == jobAbilButtons[i])
                {
                    /*if (i >= maxItemsInList)
                    {
                        i = maxItemsInList - 1;
                    }
                    if (maxItemsInList == 0)
                    {
                        // TODO: What to align to...?
                    } */
                    return i;
                }
            }
            if (uiObjectFocus == null)
            {
                ChangeUIFocusAndAlignCursor(jobAbilButtons[0]);
            }
        }
        else if (ShopUIScript.CheckShopInterfaceState())
        {
            if (maxItemsInList <= 0)
            {
                ChangeUIFocusAndAlignCursor(ShopUIScript.shopExit);
                return -1;
            }
            for (int i = 0; i < ShopUIScript.shopItemButtonList.Length; i++)
            {
                if (uiObjectFocus == ShopUIScript.shopItemButtonList[i])
                {
                    if (i + listArrayIndexOffset >= maxItemsInList)
                    {
                        // Say 11 + 16 is the same as 27. Then we want to do maxItemsInList(27) minus offset (16) minus 1
                        i = maxItemsInList - listArrayIndexOffset - 1; // -1 should avoid jank
                        ChangeUIFocusAndAlignCursor(ShopUIScript.shopItemButtonList[i]);
                    }
                    //Debug.Log("Returning " + i);
                    return i;
                }
            }
            if (uiObjectFocus == null)
            {
                ChangeUIFocusAndAlignCursor(ShopUIScript.shopItemButtonList[0]);
            }
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            if (maxItemsInList <= 0)
            {
                ChangeUIFocusAndAlignCursor(ItemWorldUIScript.itemWorldExit);
                return -1;
            }
            for (int i = 0; i < ItemWorldUIScript.itemListButtons.Length; i++)
            {
                if (uiObjectFocus == ItemWorldUIScript.itemListButtons[i])
                {
                    if (i + listArrayIndexOffset >= maxItemsInList)
                    {
                        // Say 11 + 16 is the same as 27. Then we want to do maxItemsInList(27) minus offset (16) minus 1
                        i = maxItemsInList - listArrayIndexOffset - 1; // -1 should avoid jank
                        ChangeUIFocusAndAlignCursor(ItemWorldUIScript.itemListButtons[i]);
                    }
                    return i;
                }
            }
            if (uiObjectFocus == null)
            {
                ChangeUIFocusAndAlignCursor(ItemWorldUIScript.itemListButtons[0]);
            }
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            if (maxItemsInList <= 0)
            {
                ChangeUIFocusAndAlignCursor(MonsterCorralScript.corralFoodExit);
                return -1;
            }
            for (int i = 0; i < MonsterCorralScript.corralFoodButtons.Length; i++)
            {
                if (uiObjectFocus == MonsterCorralScript.corralFoodButtons[i])
                {
                    if (i + listArrayIndexOffset >= maxItemsInList)
                    {
                        // Say 11 + 16 is the same as 27. Then we want to do maxItemsInList(27) minus offset (16) minus 1
                        i = maxItemsInList - listArrayIndexOffset - 1; // -1 should avoid jank
                        ChangeUIFocusAndAlignCursor(MonsterCorralScript.corralFoodButtons[i]);
                    }
                    return i;
                }
            }
            if (uiObjectFocus == null)
            {
                ChangeUIFocusAndAlignCursor(MonsterCorralScript.corralFoodButtons[0]);
            }
        }
        else if (dialogBoxOpen)
        {
            for (int i = 0; i < dialogUIObjects.Count; i++)
            {
                if (uiObjectFocus == dialogUIObjects[i])
                {
                    return i;
                }
            }
            if ((uiObjectFocus == null) && (dialogUIObjects.Count > 0))
            {
                ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
            }
        }
        else if (GetWindowState(UITabs.OPTIONS))
        {
            for (int i = 0; i < uiOptionsObjects.Count; i++)
            {
                if (uiObjectFocus == uiOptionsObjects[i])
                {
                    return i;
                }
            }
            if ((uiObjectFocus == null) && (uiOptionsObjects.Count > 0))
            {
                ChangeUIFocusAndAlignCursor(uiOptionsObjects[0]);
            }
        }

        //Debug.Log("No selected item found.");
        return -1;

    }

    public static void SetLastHeroTarget(Actor act)
    {
        if (act == null)
        {
            lastHeroTarget = null;
            return;
        }
        if (act.GetActorType() == ActorTypes.HERO)
        {
            lastHeroTarget = null;
            return;
        }
        singletonUIMS.lastPhysicalMousePosition = Input.mousePosition;
        lastHeroTarget = act;
    }

    public static void TryRemoveLastHeroTarget(Actor act)
    {
        if (act == lastHeroTarget)
        {
            lastHeroTarget = null;
        }
    }    
        
    //todo: make this happen in the new UI somehow
    public void PairItemWithMainhand(int id)
    {
        if (eqSubmenuOpen)
        {
            Equipment eq = eqItemSelected; // This is the offhand
            Weapon w = hotbarWeapons[activeWeaponSlot];
            if (w != null)
            {
                w.PairWithItem(eq, false, true);
                PlayCursorSound("EquipItem");
                StringManager.SetTag(0, eq.displayName);
                StringManager.SetTag(1, w.displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_pairoffhand"), GameMasterScript.heroPCActor);
            }
        }
    }

    public IEnumerator Flash_EquipItem(Image baseImage, float fTime, float fScale = 1.0f)
    {
        if (baseImage == null)
        {
            yield break;
        }

        baseImage.transform.parent.localScale = Vector3.one;

        GameObject go = Instantiate(baseImage.gameObject, baseImage.gameObject.transform);

        go.tag = "equipflash";

        Destroy(go, fTime);

        Image flashyClone = go.GetComponent<Image>();
        flashyClone.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        flashyClone.rectTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        flashyClone.rectTransform.localPosition = new Vector3(0, 0, 0);

        //move to front of draw chain
        flashyClone.rectTransform.SetAsFirstSibling();

        float fLocalScale = 1.2f * fScale;


        LeanTween.scale(go, new Vector3(fLocalScale, fLocalScale, fLocalScale), fTime).setEaseOutElastic().setOvershoot(3.0f * fScale);
        LeanTween.color(flashyClone.rectTransform, new Color(0.1f, 0.1f, 0.1f, 0.1f), fTime).setEaseInOutBack();

    }

    public bool CheckIfPassTurnFromEquipping()
    {
        if (!GameMasterScript.actualGameStarted)
        {
            return false;
        }

        bool bShouldSkipTurn = false;
        if (!MapMasterScript.activeMap.dungeonLevelData.safeArea && !MapMasterScript.activeMap.clearedMap && MapMasterScript.activeMap.GetChallengeRating() >= 1.4f)
        {
            ForceCloseFullScreenUI();
            TurnData nData = new TurnData();
            nData.SetTurnType(TurnTypes.PASS);
            nData.centerPosition = GameMasterScript.heroPCActor.GetPos();
            GameMasterScript.gmsSingleton.TryNextTurn(nData, true);
            if (PlayerOptions.tutorialTips)
            {
                if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_dungeonequip"))
                {
                    Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_dungeonequip");
                    UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                }
            }

            GameLogScript.LogWriteStringRef("log_skipturn_equip");

            bShouldSkipTurn = true;
        }

        GameMasterScript.heroPCActor.myEquipment.OnHeroEquipmentChanged();

        return bShouldSkipTurn;
    }    

    public void HideAllTooltips(int id)
    {
        HideEQTooltipContainer();
        HideInvTooltipContainer();
    }

    public void ClearItemInfo(int id, bool forceClear = false)
    {
        if (GameMasterScript.switchedInputMethodThisFrame && !forceClear)
        {
            GameMasterScript.switchedInputMethodThisFrame = false;
            return;
        }
        if (ShopUIScript.CheckShopInterfaceState())
        {
            ShopUIScript.ClearComparison();
        }

    } 

    public static void SwitchSelectedUIObjectGroup(int group)
    {
        if ((GetWindowState(UITabs.SKILLS)) && (selectedUIObjectGroup != group))
        {
            HideEQBlinkingCursor();
            swappingHotbarAction = false;
            if (listArrayIndexOffset > 0)
            {
                SetListOffset(0);
                UpdateSkillSheet();
            }
        }
        //Debug.Log("Switched group to " + group);
        selectedUIObjectGroup = group;
    }

    public void ShowItemInfoShop(int id, List<Item> listShopInventory)
    {
        if (id < 0)
        {
            ClearItemInfo(id, true);
            return;
        }

        int iAdjustedIdx = id + listArrayIndexOffset;

        if (iAdjustedIdx >= listShopInventory.Count)
        {
            return;
        }

        DisplayItemInfo(listShopInventory[iAdjustedIdx], ShopUIScript.shopInterface, false);
    }

    public void ShowItemInfoInternal(int id, List<Item> listToUse)
    {
        // Do we even need everything below?

        selectedItem = null;

        if (ShopUIScript.CheckShopInterfaceState())
        {
            listToUse = ShopUIScript.playerItemList;
            if (id < 0)
            {
                ClearItemInfo(id);
                return;
            }

            //Debug.Log("Check at index " + id);

            ShowItemInfoShop(id, listToUse);

            //selectedItem = listToUse[id];
        }


    }

    public static void UpdatePetInfo(bool debug = false)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        PetPartyUIScript.RefreshContentsOfPlayerParty(UICommandArgument.COUNT, debug);
        return;
    }

    


    
    // Use this for initialization
    public void UIStart(bool newGame)
    {

        //UpdateAbilityIcons();
        singletonUIMS = this;

        heroPC = GameMasterScript.GetHeroObject();
        gameStarted = true;

        UpdateDungeonText();

        GameLogScript.Initialize();

        //Sprite nSprite = LoadSpriteFromAtlas(allItemGraphics, "assorteditems_140");
        Sprite nSprite = null;

        if (newGame)
        {

            UpdateWeaponHotbarGraphics();


            Image impComp = weaponItemIcons[0];
            impComp.enabled = true;
            impComp.sprite = nSprite;

            SwitchActiveWeaponSlot(0, false);

        }

        jobSheetOpen = false;
        CloseOptionsMenu();

        ShopUIScript.CloseShopInterface();
        CloseCookingInterface();
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            ClearHotbar(i);
        }

        // Listeners for UI events
    }

    public void ToggleFullscreen()
    {
        UIManagerScript.PlayCursorSound("Tick");
        if (LogoSceneScript.AllowFullScreenSelection())
        {
            PlayerOptions.fullscreen = optionsFullscreen.gameObj.GetComponent<Toggle>().isOn;
        }
        else
        {
            PlayerOptions.fullscreen = true;
        }
        
        SetResolutionFromOptions();
    }

    public static void FlashStaminaBar(float time)
    {
        uiPlayerStaminaBarFill.GetComponent<ImageEffects>().ColorToRedThenBackToNormal(time);
    }

    public static void FlashHealthBar(float time)
    {
        uiPlayerHealthBarFill.GetComponent<ImageEffects>().ColorToRedThenBackToNormal(time);
    }

    public static void ToggleHealthBlink(bool blinking, float time)
    {
        if (blinking)
        {
            uiPlayerHealthBar.GetComponent<ImageEffects>().BlinkRed(time);
            lowHealthDangerFlash.SetActive(true);
        }
        else
        {
            uiPlayerHealthBar.GetComponent<ImageEffects>().ResetToNormal();
            if (lowHealthDangerFlash.activeSelf)
            {
                lowHealthDangerFlash.SetActive(false);
            }
        }

    }

    public static void ShowLearnSkillIndicator()
    {
        uiPortraitExclamation.SetActive(true);
        return;
        //uiPlayerLearnSkill.SetActive(true);
    }

    public static void HideLearnSkillIndicator()
    {
        uiPortraitExclamation.SetActive(false);

    }
    

    // TOGGLES an ability ON or OFF.
    public void SelectSkillForSwap(int index)
    {
        // Todo
        index += listArrayIndexOffset;

        if (index >= playerSupportAbilities.Count)
        {
            return;
        }
        if (playerSupportAbilities.Count == 0)
        {
            return;
        }

        skillToReplace = playerSupportAbilities[index];
        SwitchSelectedUIObjectGroup(UI_GROUP_PASSIVES);

        int abilCount = GameMasterScript.heroPCActor.myAbilities.GetPassiveSlotsUsed();

        if (skillToReplace.UsePassiveSlot) // This takes a slot.
        {
            if (skillToReplace.passiveEquipped)
            {
                GameMasterScript.heroPCActor.myAbilities.UnequipPassiveAbility(skillToReplace);
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Cancel");
            }
            else
            {
                if (abilCount == GameMasterScript.gmsSingleton.maxEquippedPassives)
                {
                    PlayCursorSound("Error");
                    return;
                }
                GameMasterScript.heroPCActor.myAbilities.EquipPassiveAbility(skillToReplace);
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem");
            }
        }
        else
        {
            // An innate or something that doesn't take a slot.
            if (skillToReplace.passiveEquipped)
            {
                GameMasterScript.heroPCActor.myAbilities.UnequipPassiveAbility(skillToReplace);
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Cancel");
            }
            else
            {
                GameMasterScript.heroPCActor.myAbilities.EquipPassiveAbility(skillToReplace);
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem");
            }
        }

        UpdateSkillSheet();

    }

    public void UpdateAbilityInfo(int index)
    {
        if (!GetWindowState(UITabs.SKILLS)) return;
        if (index >= playerJobAbilities.Count) return;

        // TODO - Write code depending on what list the ability is in...?
        SwitchSelectedUIObjectGroup(UI_GROUP_JOBSKILLS);

        selectedAbility = playerJobAbilities[index];
        ShowSkillToolTipContainer();
    }

    public void DropActiveSkill(int index)
    {
        if (draggingSkill == null && draggingItem == null)
        {
            ExitDragMode();
            return;
        }

        // Code for clearing skill slot.
        if (index < 0)
        {
            // Drop on hotbar.
            bool swapped = false;
            int nIndex = index + 16;
            if (draggingSkillButtonIndex < 0)
            {
                int dIndex = draggingSkillButtonIndex + 16;

                bool swappedItem = false;
                //Debug.Log("D " + dIndex + " N " + nIndex);
                if (hotbarAbilities[nIndex].actionType == HotbarBindableActions.CONSUMABLE)
                {
                    Consumable c = hotbarAbilities[nIndex].consume;
                    AddItemToSlot(c, dIndex, false);
                    swappedItem = true;
                }

                if (hotbarAbilities[dIndex].actionType == HotbarBindableActions.ABILITY)
                {
                    AbilityScript swap = hotbarAbilities[nIndex].ability;
                    if (swap != null)
                    {
                        AddAbilityToSlot(swap, dIndex, false);
                        swapped = true;
                    }
                }
                else
                {
                    if (!swappedItem)
                    {
                        ClearHotbar(dIndex);
                    }
                }
            }

            ClearHotbar(nIndex);

            if (draggingSkill != null)
            {
                AbilityScript abil = draggingSkill;
                AddAbilityToSlot(abil, nIndex, !swapped);
            }
            else // Must have dropped an item
            {
                Consumable itm = draggingItem as Consumable;
                AddItemToSlot(itm, nIndex, !swapped);
            }


            RefreshAbilityCooldowns();
            UpdateSkillSheet();
            ExitDragMode();
            return;
        }
        else
        {
            // Dropped on skill list.
            int draggingIndex = 0;
            if (draggingSkillButtonIndex < 0)
            {
                draggingIndex = draggingSkillButtonIndex + 16;
            }
            ClearHotbar(draggingIndex);
            RefreshAbilityCooldowns();
            UpdateSkillSheet();
            ExitDragMode();

            return;
        }
    }
    
    //returns the held item, if there isn't one, returns the dragged item
    //which could also be null
    public static ISelectableUIObject GetHeldGenericObject()
    {
        if (heldGenericObject != null)
        {
            return heldGenericObject;
        }

        return draggingGenericObject;
    }

    public static void SetHeldGenericObject(ISelectableUIObject obj)
    {
        heldGenericObject = obj;
    }

    public static void ClearAllHeldGenericObjects()
    {
        heldGenericObject = null;
        draggingGenericObject = null;
    }

    public static UIObject GetDefaultUIFocus()
    {
        return defaultUIFocus;
    }

    public static void SetDefaultUIFocus(UIObject newFocus)
    {
        defaultUIFocus = newFocus;
    }

    public static void ClearDefaultUIFocus()
    {
        defaultUIFocus = null;
    }

    //Allows us to drag any ol' thing
    public static void BeginDragGenericObject(ISelectableUIObject obj, GameObject originGO)
    {
        draggingGenericObject = obj;

        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.BeginDragGenericObject(obj, originGO);
        }

        PlayCursorSound("StartDrag");
    }

    //Allows us to drag any ol' thing
    public static void EndDragGenericObject()
    {
        draggingGenericObject = null;
        heldGenericObject = null;

        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.EndDragGenericObject();
        }

    }

    public void DragActiveSkill(int index)
    {
        if (!GetWindowState(UITabs.SKILLS)) return;
        if ((isDraggingSkill) || (isDraggingItem)) return;

        if (index < 0) // Dragging FROM the hotbar.
        {
            if (hotbarAbilities[index + 16].actionType == HotbarBindableActions.CONSUMABLE)
            {
                draggingItem = hotbarAbilities[index + 16].consume;
                isDraggingItem = true;
                //skillDragger.SetActive(true);
                draggingSkillButtonIndex = index;
                //skillDragger.GetComponent<Image>().sprite = GetItemSprite(draggingItem.spriteRef);
                return;

            }
            else if (hotbarAbilities[index + 16].actionType == HotbarBindableActions.ABILITY)
            {
                draggingSkill = hotbarAbilities[index + 16].ability;
            }
            else
            {
                return;
            }
        }
        else
        {
            // Dragging FROM skill list.
            int sIndex = GetIndexOfSelectedButton();
            if ((sIndex + listArrayIndexOffset) >= playerActiveAbilities.Count)
            {
                Debug.Log("Try drag error.");
                ExitDragMode();
                return;
            }
            try { draggingSkill = playerActiveAbilities[sIndex + listArrayIndexOffset]; }
            catch (Exception e)
            {
                Debug.Log(sIndex + " " + listArrayIndexOffset + " " + playerActiveAbilities.Count);
                Debug.Log(e);
                ExitDragMode();
                return;
            }
        }

        //skillDragger.SetActive(true);
        isDraggingSkill = true;
        draggingSkillButtonIndex = index;
        //skillDragger.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allUIGraphics, draggingSkill.iconSprite);

    }

    public void DragInvItem(int index)
    {
        if (!GetWindowState(UITabs.INVENTORY)) return;
        if (isDraggingItem) return;

        if (index < 0)
        {
            if (hotbarAbilities[index + 16].actionType == HotbarBindableActions.CONSUMABLE)
            {
                draggingItem = hotbarAbilities[index + 16].consume;
            }
            else
            {
                return;
            }
        }
        else
        {
            int sIndex = GetIndexOfSelectedButton();
            if ((sIndex + listArrayIndexOffset) >= playerItemList.Count)
            {
                Debug.Log("Try drag error.");
                ExitDragMode();
                return;
            }
            try { draggingItem = playerItemList[sIndex + listArrayIndexOffset]; }
            catch (Exception e)
            {
                Debug.Log(sIndex + " " + listArrayIndexOffset + " " + playerItemList.Count);
                Debug.Log(e);
                ExitDragMode();
                return;
            }
        }

        invDragger.SetActive(true);
        isDraggingItem = true;
        draggingItemButtonIndex = index;
        invDragger.GetComponent<Image>().sprite = draggingItem.GetSpriteForUI();

    }

    public void ExitDragMode()
    {
        EndDragGenericObject();

        draggingItem = null;
        draggingSkill = null;
        draggingGenericObject = null;

        isDraggingItem = false;
        isDraggingSkill = false;

        cookingDragger.SetActive(false);
    }
    
    public static void CycleUITabs(Directions dir)
    {
        if (!mainUINav.gameObj.activeSelf)
        {
            return;
        }

        int curTab = (int)singletonUIMS.GetCurrentUITab();
        
        if (dir == Directions.EAST)
        {
            curTab++;
        }
        else
        {
            curTab--;
        }
        if (curTab < 0)
        {
            curTab = uiNavButtons.Length - 1;
        }
        if (curTab >= uiNavButtons.Length)
        {
            curTab = 0;
        }
        //Debug.Log("Is now " + (UITabs)curTab);
        singletonUIMS.SwitchUITabs(curTab);
    }

    public static void HideInvTooltipContainer()
    {
        invTooltipContainer.SetActive(false);
        if (genericItemTooltip != null)
        {
            genericItemTooltip.Hide();
        }
    }

    public static void HideEQTooltipContainer()
    {
        eqTooltipContainer.SetActive(false);
        if (genericItemTooltip != null)
        {
            genericItemTooltip.Hide();
        }
    }

    public static void SaveEquipmentUIState()
    {
        lastEquipmentState = new InventoryUIState(true);
        for (int i = 0; i < itemFilterTypes.Length; i++)
        {
            lastEquipmentState.filterStates[i] = itemFilterTypes[i];
        }
        lastEquipmentState.listIndexOffset = listArrayIndexOffset;
        lastEquipmentState.indexOfSelectedItem = singletonUIMS.GetIndexOfSelectedButton();
    }

    public static void SaveInventoryUIState()
    {
        lastInventoryState = new InventoryUIState(false);
        for (int i = 0; i < itemFilterTypes.Length; i++)
        {
            lastInventoryState.filterStates[i] = itemFilterTypes[i];
        }
        lastInventoryState.listIndexOffset = listArrayIndexOffset;
        lastInventoryState.indexOfSelectedItem = singletonUIMS.GetIndexOfSelectedButton();
    }

    public static void TogglePlayerHUD()
    {
        playerHUDEnabled = !playerHUDEnabled;
        if (!playerHUDEnabled)
        {
            singletonUIMS.isMouseOverUI = false;
        }
        foreach (GameObject go in nonDialogHUDElements)
        {
            go.SetActive(playerHUDEnabled);
            if (playerHUDEnabled)
            {
                if (go == genericInfoBar)
                {
                    if (genericInfoBar.GetComponentInChildren<TextMeshProUGUI>().text == "")
                    {
                        singletonUIMS.HideGenericInfoBar();
                    }
                }
                if (go == lowHealthDangerFlash 
                    && GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > GameMasterScript.PLAYER_HEALTH_PERCENT_CRITICAL)
                {
                    go.SetActive(false);
                }
            }
        }
    }

    public static void SetPlayerHudAlpha(float amt)
    {
        amt = Mathf.Clamp(amt, 0f, 1.0f);

        if (nonDialogHUDElements == null)
        {
            return;
        }

        foreach (GameObject go in nonDialogHUDElements)
        {
            if (go.GetComponent<CanvasGroup>() != null)
            {
                go.GetComponent<CanvasGroup>().alpha = amt;
            }
            if (amt == 0.0f)
            {
                go.SetActive(false);
            }
            else
            {
                go.SetActive(true);
                if (go == lowHealthDangerFlash && GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > GameMasterScript.PLAYER_HEALTH_PERCENT_CRITICAL)
                {
                    go.SetActive(false);
                }
            }
        }
    }   
    

    
    private static void SelectSaveSlot_OnComplete(TextBranch saveBranch, Conversation saveSelect)
    {
        CloseDialogBox();
        TitleScreenScript.WaitingForSaveSlotsToLoad = false;
    }

    public static void Debug_SkipStart_FadeOutThenLoadGame()
    {
        singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());
    }

    public IEnumerator FadeOutThenLoadGame()
    {
        // new as of 7/6, let's see if this works.
        UIManagerScript.CloseDialogBox();

        FadingToGame = true;
        blackFade.SetActive(true);
        blackFade.GetComponent<ImageEffects>().FadeOut(0.9f);
        MusicManagerScript.singleton.Fadeout(1.00f);
        yield return new WaitForSeconds(0.95f);
        LoadingWaiterManager.Display();
        while (!GameMasterScript.allAbilitiesLoaded || !GameMasterScript.allJobsLoaded)
        {
            yield return null;
        }


        DLCManager.OnSwitchToGameplaySceneFromMainMenu();

        if (RandomJobMode.creatingCharacterNotInRJMode)
        {
            //if (Debug.isDebugBuild) Debug.Log("<color=red>We are NOT in random job mode.</color>");
            RandomJobMode.SetGameToRandomJobMode(false);
        }
        else
        {
            if (GameStartData.slotInRandomJobMode[GameStartData.saveGameSlot])
            {
                RandomJobMode.SetGameToRandomJobMode(true);
            }
        }

        TDSceneManager.LoadSceneAsync("Gameplay"); 
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
        GC.Collect();
    }

    public static void FlashWhite(float time)
    {
        singletonUIMS.whiteFade.SetActive(true);
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().ResetToWhite();
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().FadeIn(time);
    }

    public IEnumerator WaitThenWhiteFadeIn(float time)
    {
        yield return new WaitForSeconds(time);
        whiteFade.SetActive(true);
        whiteFade.GetComponent<ImageEffects>().FadeIn(time / 2f);
    }

    public static void FadeWhiteOutAndIn(float time)
    {
        //Debug.Log("Fading white out, then in");
        singletonUIMS.whiteFade.SetActive(true);
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().SetColorToWhite();
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().FadeOut(time / 2f);
        singletonUIMS.StartCoroutine(singletonUIMS.WaitThenWhiteFadeIn(0.1f + (time / 2f)));
    }

    public static void FlashRed(float time)
    {
        singletonUIMS.whiteFade.SetActive(true);
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().ResetToColor(Color.red);
        singletonUIMS.whiteFade.GetComponent<ImageEffects>().FadeIn(time);
    }

    public static void FadeIn(float time)
    {
        singletonUIMS.blackFade.SetActive(true);
        singletonUIMS.blackFade.GetComponent<ImageEffects>().FadeIn(time);
        LoadingWaiterManager.Hide();
    }

    public static void FadeGradientIn(float time)
    {
        ToggleBGPlayerBar(true);        
        #if UNITY_EDITOR            
            Debug.Log("Fade gradient in " + time);
        #endif

        if (time < 0.04f)
        {
            //if (Debug.isDebugBuild) Debug.Log("Turn gradient off at once");
            singletonUIMS.gradientBGImageForUltraWide.GetComponent<ImageEffects>().TurnOffAtOnce();
            ToggleBackgroundGradientImage(false);                        
            return;
        }
        ToggleBackgroundGradientImage(true);        
        singletonUIMS.gradientBGImageForUltraWide.GetComponent<ImageEffects>().FadeIn(time);
    }

    public static void FadeGradientOut(float time)
    {
        #if UNITY_EDITOR
            //Debug.Log("Fade gradient out " + time);
        #endif
        ToggleBackgroundGradientImage(true);        
        ToggleBGPlayerBar(false);
        singletonUIMS.gradientBGImageForUltraWide.GetComponent<ImageEffects>().FadeOut(time);
    }

    public static void ClearConversation()
    {
        currentConversation = null;
        currentTextBranch = null;
    }

    public static float GetAlphaOfBlackOverlay()
    {
        if (singletonUIMS.blackFade == null)
        {
            singletonUIMS.blackFade = GameObject.Find("BlackFade");
        }
        if (singletonUIMS.blackFade != null)
        {
            return singletonUIMS.blackFade.GetComponent<ImageEffects>().GetAlpha();
        }
        else return 1f;
    }

    public static void SetToBlack()
    {
        if (singletonUIMS == null) return;
        if (singletonUIMS.blackFade == null)
        {
            singletonUIMS.blackFade = GameObject.Find("BlackFade");
        }
        if (singletonUIMS.blackFade != null)
        {
            singletonUIMS.blackFade.GetComponent<ImageEffects>().SetAlpha(1.0f);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Couldn't find black fade?");
        }

    }

    public static void SetToClear()
    {
        singletonUIMS.blackFade.GetComponent<ImageEffects>().SetAlpha(0.0f);
    }

    public static void FadeOut(float time)
    {
        SetFadeState(EFadeStates.FADING_OUT);
        singletonUIMS.blackFade.SetActive(true);
        singletonUIMS.blackFade.GetComponent<ImageEffects>().FadeOut(time);
    }

    public static void SetGlobalResponse(DialogButtonResponse dbr)
    {
        globalDialogButtonResponse = dbr;
    }

    public static void PlaySound(string refCue)
    {
        if (singletonUIMS.uiDialogMenuCursor.activeSelf)
        {
            PlayCursorSound(refCue);
        }
    }

    public static void TryHotkey(int slot)
    {
        KeyCode kc = IntToKeyCode[slot];
        if (GetWindowState(UITabs.EQUIPMENT) || GetWindowState(UITabs.INVENTORY))
        {
            if (GetWindowState(UITabs.EQUIPMENT) && eqSubmenuOpen) return;
            Item itemToHotkey = selectedItem;
            if (itemToHotkey == null)
            {
                itemToHotkey = mouseHoverItem;
            }
            if (itemToHotkey != null)
            {
                if (itemToHotkey.itemType == ItemTypes.WEAPON && slot < 4)
                {
                    TryAddWeaponToActiveSlot(slot);

                }
                else if (itemToHotkey.itemType == ItemTypes.CONSUMABLE)
                {
                    // Try map to hotbar.
                    Consumable cx = (Consumable)itemToHotkey as Consumable;
                    AddItemToSlot(cx, slot, true);
                }
            }
        }
        if (GetWindowState(UITabs.SKILLS))
        {
            if (selectedUIObjectGroup != 0) // Not in active area.
            {
                return;
            }
            slot = slot + (indexOfActiveHotbar * 8);

            AbilityScript tryToHotkey = playerActiveAbilities[singletonUIMS.GetIndexOfSelectedButton() + listArrayIndexOffset]; // was index
            AddAbilityToSlot(tryToHotkey, slot, true);
            UpdateSkillSheet();
        }
    }

    

    public static void AlignEquippedWeaponTextOnSheet(UIObject weapon)
    {
        // This is for equipment sheet
        eqEquippedWeapon.transform.SetParent(weapon.gameObj.transform);
        eqEquippedWeapon.transform.localPosition = new Vector3(0f, -10f, 0f);
    }
    
    int CalculateTextWidth(Text text)
    {
        int len = 0;
        Font font = text.font;
        CharacterInfo ci = new CharacterInfo();
        char[] arr = text.text.ToCharArray();

        foreach (var a in arr)
        {
            font.GetCharacterInfo(a, out ci, text.fontSize);
            len += ci.advance;
        }

        return len;
    }

    float CalculateLeftOffset(Text text)
    {
        return CalculateTextWidth(text) / 2f;
    }



    public static void CheckConversationQueue()
    {
        if (conversationQueue.Count > 0)
        {
            myDialogBoxComponent.StopFadeImmediatelyIfFadingOut();
            ConversationData cd = conversationQueue.Dequeue();
            StartConversation(cd.conv, cd.dType, cd.whichNPC, true);
        }
        else
        {
            // This is new place to fade out.
            myDialogBoxComponent.FadeOut();
        }
    }

    // *** BEGIN OPTIONS MENU CODE ***

    public void FadeInAndOut(float time)
    {
        blackFade.SetActive(true);
        blackFade.GetComponent<ImageEffects>().FadeInAndOut(time);
    }
    
    public void ResumeGameFromOptions(int x)
    {
        PlayCursorSound("Cancel");
        //GetWindowState(UITabs.OPTIONS) = false;
        //uiOptionsMenu.SetActive(false);
        CleanupAfterUIClose(UITabs.OPTIONS); // new on 11/18
        if (!playerHUDEnabled)
        {
            TogglePlayerHUD();
        }
    }
    
    public static string RandomizeColors(String txt)
    {
        string retString = "";

        // Define these as hex
        string[] colors = new string[11];
        colors[0] = "aqua";
        colors[1] = "white";
        colors[2] = "magenta";
        colors[3] = "green";
        colors[4] = "lime";
        colors[5] = "lightblue";
        colors[6] = "orange";
        colors[7] = "purple";
        colors[8] = "red";
        colors[9] = "teal";
        colors[10] = "yellow";

        char[] characters = txt.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            retString += "<color=" + colors[UnityEngine.Random.Range(0, colors.Length)] + ">" + characters[i] + "</color>";
        }

        return retString;
    }

    // On next Update(), typewriter text will auto finish
    // This could be called by things other than cursor confirm!
    public static void FinishTypewriterTextImmediately()
    {
        finishTypewriterTextImmediately = true;
    }
            
    // *** END OPTIONS MENU CODE ***

    public static Vector2 GetRenderVector()
    {
        return new Vector2(sRenderRows, sRenderColumns);
    }
    
    private static bool IsStatusInBuffs(StatusEffect se)
    {
        for (int i = 0; i < playerBuffs.Length; i++)
        {
            if (playerBuffs[i] == se)
            {
                return true;
            }
        }
        return false;
    }
    private static bool IsStatusInDebuffs(StatusEffect se)
    {
        for (int i = 0; i < playerDebuffs.Length; i++)
        {
            if (playerDebuffs[i] == se)
            {
                return true;
            }
        }
        return false;
    }

    public static void RefreshStatuses(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            statusesDirty = true;
            return;
        }

        statusesDirty = false;
        // First, get rid of expired statuses.
        // Need to run this for buffs AND debuffs
        for (int i = 0; i < playerBuffs.Length; i++)
        {
            if (playerBuffs[i] != null)
            {
                buffIcons[i].sprite = transparentSprite; // WAS enable on/off
                playerBuffs[i] = null;
                buffIcons[i].raycastTarget = false;
                buffCountIcons[i].sprite = transparentSprite;
            }
        }
        for (int i = 0; i < playerDebuffs.Length; i++)
        {
            if (playerDebuffs[i] != null)
            {
                debuffIcons[i].sprite = transparentSprite;
                debuffIcons[i].raycastTarget = false;
                playerDebuffs[i] = null;
                debuffCountIcons[i].sprite = transparentSprite;
            }
        }

        // Now run through statuses. If they're not expired, add them one by one.
        StatBlock myStats = GameMasterScript.heroPCActor.myStats;

        buffStringIntDict.Clear();
        debuffStringIntDict.Clear();

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.showIcon && (se.curDuration > 0 || se.CheckDurTriggerOn(StatusTrigger.PERMANENT)))
            {
                if (se.isPositive)
                {
                    //if (!IsStatusInBuffs(se))
                        if (buffStringIntDict.ContainsKey(se.refName))
                        {
                            buffStringIntDict[se.refName]++;
                            continue;
                        }
                        else
                        {
                            buffStringIntDict.Add(se.refName, 1);
                        }

                        // Better loader/atlas for UI.
                        int emptyIndex = -1;
                        for (int i = 0; i < playerBuffs.Length; i++)
                        {
                            if ((playerBuffs[i] == null) && (emptyIndex == -1))
                            {
                                emptyIndex = i;
                                break;
                            }
                        }
                        if (emptyIndex == -1) continue;
                        string path = se.statusIconRef;
                        Sprite nSprite = LoadSpriteFromDict(dictUIGraphics, path);
                        buffIcons[emptyIndex].sprite = nSprite;
                        buffIcons[emptyIndex].raycastTarget = true;
                        playerBuffs[emptyIndex] = se;
                        //impComp.enabled = true;
                }
                else
                    //if (!IsStatusInDebuffs(se))
                    {
                        if (debuffStringIntDict.ContainsKey(se.refName))
                        {
                            debuffStringIntDict[se.refName]++;
                            continue;
                        }
                        else
                        {
                            debuffStringIntDict.Add(se.refName, 1);
                        }

                        // Better loader/atlas for UI.
                        int emptyIndex = -1;
                        for (int i = 0; i < playerDebuffs.Length; i++)
                        {
                            if ((playerDebuffs[i] == null) && (emptyIndex == -1))
                            {
                                emptyIndex = i;
                                break;
                            }
                        }
                        if (emptyIndex == -1) continue;
                        string path = se.statusIconRef;
                        Sprite nSprite = LoadSpriteFromDict(dictUIGraphics, path);
                        //Image impComp = debuffIcons[emptyIndex].GetComponent<Image>();
                        debuffIcons[emptyIndex].sprite = nSprite;
                        debuffIcons[emptyIndex].raycastTarget = true;
                        playerDebuffs[emptyIndex] = se;
                        //impComp.enabled = true;
                }
            }
        }

        string refName;
        for (int i = 0; i < playerBuffs.Length; i++)
        {
            if (playerBuffs[i] != null)
            {
                refName = playerBuffs[i].refName;
                if (buffStringIntDict[refName] > 1)
                {
                    buffCountIcons[i].sprite = LoadSpriteFromAtlas(quickslotNumbers, "QuickslotNumbers_" + (buffStringIntDict[refName] - 1));
                }
            }
            if (playerDebuffs[i] != null)
            {
                refName = playerDebuffs[i].refName;
                if (debuffStringIntDict[refName] > 1)
                {
                    debuffCountIcons[i].sprite = LoadSpriteFromAtlas(quickslotNumbers, "QuickslotNumbers_" + (debuffStringIntDict[refName] - 1));
                }
            }
        }
        RefreshStatusCooldowns();
    }
    
    static List<Sprite> reusableSpriteList;
    static bool spriteListInitialized;
    public static Sprite[] GetPortraitForDialog(string strPortraitName)
    {
        if (!spriteListInitialized)
        {
            reusableSpriteList = new List<Sprite>();
            spriteListInitialized = true;
        }
        reusableSpriteList.Clear();

        for (int iPIdx = 0; iPIdx < allPortraits.Length; iPIdx++)
        {            
            if (portraitNames[iPIdx] == null) continue;

            if (portraitNames[iPIdx].Contains(strPortraitName))
            {
                reusableSpriteList.Add(allPortraits[iPIdx]);
            }
        }

        return reusableSpriteList.ToArray();
    }

    public static Sprite LoadSpriteFromDict(Dictionary<string, Sprite> sprites, string compareName)
    {
        Sprite outSpr = null;
        
        if (sprites.TryGetValue(compareName, out outSpr))
        {
            return outSpr;
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Cannot find " + compareName);
        }
        return outSpr;
    }

    public static Sprite LoadSpriteFromAtlas(Sprite[] sprites, string compareName)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i].name == compareName)
            {
                return sprites[i];
            }

        }
        return null;
    }

    public static Sprite GetItemSprite(string compareName)
    {
        return LoadSpriteFromDict(dictItemGraphics, compareName);
    }



    

    public void UseTownPortal(int dummy)
    {
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;

        GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.escapeTorchAbility);
        CloseHotbarNavigating();
    }

    public void UseRegenFlask(int dummy)
    {
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;

        GameMasterScript.gmsSingleton.UseAbilityRegenFlask();
        //GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.regenFlaskAbility);
        CloseHotbarNavigating();
    }



    public bool CheckTargeting()
    {
        return abilityTargeting;
    }

    public static void SwitchRevealMode()
    {
        dbRevealMode = !dbRevealMode;
    }

    public static void UpdateDungeonText()
    {
        if (uiDungeonName == null)
        {
            return;
        }


        //uiDungeonName.text = name; //+ " (" + displayLevel + ")";
        if (!MapMasterScript.mapLoaded)
        {
            return;
        }

        int displayLevel = MapMasterScript.activeMap.effectiveFloor + 1;

        float cv = MapMasterScript.activeMap.challengeRating;

        if (MapMasterScript.activeMap.ScaleUpToPlayerLevel())
        {
            displayLevel = GameMasterScript.heroPCActor.myStats.GetLevel() + 1;
            cv = BalanceData.LEVEL_TO_CV[displayLevel];
        }

        float playerStrengthMultiplier = 0.06f;
        if (SharaModeStuff.IsSharaModeActive())
        {
            playerStrengthMultiplier = 0.085f;
        }

        float playerCV = 1.0f + (playerStrengthMultiplier * (GameMasterScript.heroPCActor.myStats.GetLevel() - 1));

        float diff = playerCV - cv;
        string dColor = "<color=white>";

        // Player 1.2 vs level 1.0: diff is 0.2

        if (MapMasterScript.itemWorldOpen && MapMasterScript.itemWorldItem != null)
        {
            Equipment itemWorldEQ = MapMasterScript.itemWorldItem as Equipment;
            ItemWorldMetaData properties = itemWorldEQ.GetItemWorldProperties();
            float rewardBonus = properties.rewards;
            rewardBonus *= 0.35f;
            diff -= rewardBonus;
        }

        //Debug.Log("Level CV: " + cv + " PCV: " + playerCV + " Diff: " + diff + " Floor: " + MapMasterScript.activeMap.floor + " Active Rating: " + MapMasterScript.activeMap.GetChallengeRating());

        // HIGHER diff means EASIER
        string diffText = "";
        if (diff >= 0.2f)
        {
            dColor = silverHexColor;
            diffText += " (" + StringManager.GetString("difficulty_1") + ")";
        }
        else if (diff >= 0.1f && diff < 0.2f)
        {
            dColor = greenHexColor;
            diffText += " (" + StringManager.GetString("difficulty_2") + ")";
        }
        else if (diff >= 0.05f && diff < 0.1f)
        {
            dColor = cyanHexColor;
            diffText += " (" + StringManager.GetString("difficulty_3") + ")";
        }
        else if (diff <= -0.05f && diff > -0.1f)
        {
            dColor = "<color=yellow>";
        }
        else if (diff <= -0.1f && diff > -0.15f)
        {
            dColor = orangeHexColor;
            diffText += " (" + StringManager.GetString("difficulty_7") + ")";
        }
        else if (diff <= -0.15f && diff > -0.2f)
        {
            dColor = redHexColor;
            diffText += " (" + StringManager.GetString("difficulty_8") + ")";
        }
        else if (diff <= -0.2f)
        {
            dColor = redHexColor;
            diffText += " (" + StringManager.GetString("difficulty_9") + ")";
        }

        if (diffText == "")
        {
            diffText += " (" + StringManager.GetString("difficulty_6") + ")";
        }

        if (MapMasterScript.activeMap.dungeonLevelData.safeArea)
        {
            diffText = "";
        }



        if (MapMasterScript.activeMap.dungeonLevelData.sideArea && MapMasterScript.activeMap.dungeonLevelData.showRewardSymbol)
        {
            uiDungeonIcon.gameObject.SetActive(true);
            if (MapMasterScript.activeMap.clearedMap || MapMasterScript.activeMap.unfriendlyMonsterCount <= 0)
            {
                uiDungeonIcon.sprite = dungeonIconChestOpen;
            }
            else
            {
                uiDungeonIcon.sprite = dungeonIconChestClosed;
            }
            uiDungeonName.text = dColor + MapMasterScript.activeMap.GetName() + diffText + "</color>";
        }
        else
        {
            uiDungeonIcon.gameObject.SetActive(false);
            uiDungeonName.text = dColor + MapMasterScript.activeMap.GetName() + diffText + "</color>";
        }
        return;
    }

    public static void RefreshPlayerCT(bool extraTurn)
    {
        string calcCTDisplay = "";

        if (extraTurn)
        {
            calcCTDisplay = StringManager.GetString("misc_extraturn").ToUpperInvariant() + "!";
        }
        else
        {
            int amt = (int)(GameMasterScript.heroPCActor.GetActionTimerDisplay());
            if (amt < 0)
            {
                amt = 0;
            }
            calcCTDisplay = StringManager.GetString("misc_extraturn") + ": " + amt.ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }

        uiPlayerCT.text = calcCTDisplay;

        float pMax = GameMasterScript.heroPCActor.GetActionTimerDisplay() / 100f;
        if (pMax < 0) pMax = 0;
        uiPlayerCTBarFill.fillAmount = pMax;
    }

    public static void UpdateFlaskCharges()
    {
        regenFlaskText.text = GameMasterScript.heroPCActor.regenFlaskUses.ToString(); // + " Uses";
    }

    public static void RefreshPlayerStats()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (GameMasterScript.heroPCActor.myJob == null)
        {
            return;
        }


        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
        {
            singletonUIMS.Switch_BGImageBar.UpdateJPAndGold();
        }

        regenFlaskText.text = GameMasterScript.heroPCActor.regenFlaskUses.ToString(); // + " Uses";

        StatBlock myStats = GameMasterScript.GetHeroActor().myStats;
        uiPlayerHealth.text = (int)myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) + "/" + (int)myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
        uiPlayerStamina.text = (int)myStats.GetStat(StatTypes.STAMINA, StatDataTypes.CUR) + "/" + (int)myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX);
        uiPlayerEnergy.text = (int)myStats.GetStat(StatTypes.ENERGY, StatDataTypes.CUR) + "/" + (int)myStats.GetStat(StatTypes.ENERGY, StatDataTypes.MAX);

        float pMax = myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) / myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
        uiPlayerHealthBarFill.fillAmount = pMax;
        pMax = myStats.GetStat(StatTypes.STAMINA, StatDataTypes.CUR) / myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX);
        uiPlayerStaminaBarFill.fillAmount = pMax;
        pMax = myStats.GetStat(StatTypes.ENERGY, StatDataTypes.CUR) / myStats.GetStat(StatTypes.ENERGY, StatDataTypes.MAX);
        uiPlayerEnergyBarFill.fillAmount = pMax;

        PlayerHUDStatsComponent.RefreshReservedEnergy(GameMasterScript.heroPCActor.cachedBattleData.energyReservedByAbilities, myStats.GetStat(StatTypes.ENERGY, StatDataTypes.MAX));
        PlayerHUDStatsComponent.RefreshReservedStamina(GameMasterScript.heroPCActor.cachedBattleData.staminaReservedByAbilities, myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX));

        RefreshEnergyShield();

        int calcXPDisplay = myStats.GetXPToNextLevel();
        uiPlayerXP.text = "XP: " + myStats.GetXP() + " / " + calcXPDisplay;

        uiPlayerLevel.text = " " + myStats.GetLevel();

        pMax = ((float)myStats.GetXP() - myStats.GetXPToCurrentLevel()) / ((float)myStats.GetXPToNextLevel() - myStats.GetXPToCurrentLevel());

        // Xp to next = 250
        // Current xp = 210
        // previous level = 150

        uiPlayerXPBarFill.fillAmount = pMax;



        HeroPC heroPCactor = GameMasterScript.GetHeroActor();

        string playerText = heroPCactor.displayName;

        uiPlayerName.text = playerText;
    }

    // No update function for now.

    public static void CycleWeapons(int amount)
    {
        //Debug.Log("Cycle " + amount);
        int origSlot = activeWeaponSlot;
        bool validWeapon = false;

        int tries = 0;
        while ((!validWeapon) && (tries < 4))
        {
            tries++;
            activeWeaponSlot += amount;
            if (activeWeaponSlot < 0)
            {
                activeWeaponSlot = 3;
            }
            if (activeWeaponSlot >= 4)
            {
                activeWeaponSlot = 0;
            }
            if (activeWeaponSlot != origSlot)
            {
                validWeapon = true;
            }
        }

        if (validWeapon)
        {
            SwitchActiveWeaponSlot(activeWeaponSlot, false, origSlot);
        }
        else
        {
            activeWeaponSlot = origSlot;
        }

    }

    public static void RefreshStatusCooldowns()
    {
        // This is a good place to also remove hotbar actions we cannot do, e.g. Shara mode
        if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            if (hudHotbarFlask.gameObj.activeSelf)
            {
                hudHotbarFlask.gameObj.SetActive(false);
                hudHotbarFlask.gameObj.transform.parent.gameObject.SetActive(false);
                hudHotbarFlask.enabled = false;
            }
        }

        for (int i = 0; i < buffIcons.Length; i++)
        {
            GameObject go = buffIcons[i].transform.GetChild(0).gameObject;
            Image img = go.GetComponent<Image>();
            if (playerBuffs[i] != null)
            {
                StatusEffect se = playerBuffs[i];
                if (se.curDuration > 0 && se.CheckDurTriggerOn(StatusTrigger.TURNEND))
                {
                    //img.color = new Color(img.color.r, img.color.g, img.color.b, 0.5f);
                    img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    img.fillAmount = (float)se.curDuration / (float)se.maxDuration;
                }
                else
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                }
            }
            else
            {
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
            }
        }
        for (int i = 0; i < debuffIcons.Length; i++)
        {
            GameObject go = debuffIcons[i].transform.GetChild(0).gameObject;
            Image img = go.GetComponent<Image>();
            if (playerDebuffs[i] != null)
            {
                StatusEffect se = playerDebuffs[i];
                if (se.curDuration > 0 && se.CheckDurTriggerOn(StatusTrigger.TURNEND))
                {
                    //img.color = new Color(img.color.r, img.color.g, img.color.b, 0.5f);
                    img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    img.fillAmount = (float)se.curDuration / (float)se.maxDuration;
                }
                else
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                }
            }
            else
            {
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
            }
        }
    }

    public IEnumerator FadeInWaiter(float time1, float time2, bool alsoStopAnimation = true)
    {
        yield return new WaitForSeconds(time1);

        // Safety check. Are we fully faded?
        if (blackFade.GetComponent<ImageEffects>().IsFading())
        {
            // We've desynced somehow. The fade OUT is still happening. So, we wait.
            Debug.Log("Fadeout still happening. Waiting another 250ms.");
            WaitThenFadeIn(0.25f, time2, alsoStopAnimation);
        }
        else
        {
            FadeIn(time2);            

            //Debug.Log("Now fading in over " + time2);
            if (alsoStopAnimation)
            {
                GameMasterScript.SetAnimationPlaying(false);
            }

            if (MapMasterScript.activeMap.floor == MapMasterScript.CAMPFIRE_FLOOR) // Campfire rest floor
            {
                RefreshPlayerStats();
                Actor toRemove = null;
                foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
                {
                    if (act.actorRefName == "npc_restfire")
                    {
                        if (act.HasActorData("fireused"))
                        {
                            toRemove = act;
                        }

                    }
                }
                if (toRemove != null)
                {
                    MapTileData mtd = MapMasterScript.activeMap.GetTile(toRemove.GetPos());
                    MapMasterScript.activeMap.RemoveActorFromLocation(toRemove.GetPos(), toRemove);
                    MapMasterScript.activeMap.RemoveActorFromMap(toRemove);
                    if (toRemove.objectSet)
                    {
                        GameMasterScript.ReturnActorObjectToStack(toRemove, toRemove.GetObject());
                    }
                    mtd.ChangeTileType(TileTypes.GROUND, MapMasterScript.activeMap.mgd);
                    mtd.UpdateCollidableState();
                }
            }
        }


    }

    public void WaitThenFadeIn(float time1, float time2, bool alsoStopAnimation = true)
    {
        //Debug.Log("<color=yellow>Waiting " + time1 + " to fade in over " + time2 + "</color>");
        StartCoroutine(FadeInWaiter(time1, time2, alsoStopAnimation));
    }

    public static void SetCursorColor(Color newColor)
    {
        if (singletonUIMS.uiDialogMenuCursorImage != null)
        {
            singletonUIMS.uiDialogMenuCursorImage.color = newColor;
        }
    }

    /// <summary>
    /// Make the player shield pulse a little, and go faster as you get hit.
    /// </summary>
    void UpdatePlayerShieldUI()
    {
        var mat = uiPlayerShieldBarFill.material;
        var hero = GameMasterScript.heroPCActor;

        if (hero == null) return;
        
        float shieldCurrent = hero.ReadActorData("flowshield_dmgleft");
        // always scale shieldMax to max health for now
        //float shieldMax = hero.ReadActorData("flowshield_dmgmax");
        float shieldMax = hero.myStats.GetMaxStat(StatTypes.HEALTH);

        if (shieldCurrent <= 0) return;

        //this is the 1 to 1 ratio
        float shieldRatio = shieldCurrent / shieldMax;

        //make the bar a little less static
        float modifiedFillRatio = shieldRatio + (0.015f * Mathf.Sin(((Time.realtimeSinceStartup / 4.0f) % 1.0f) * 6.28f));
        
        //clamp the fill so it's never < 10%
        modifiedFillRatio = Mathf.Clamp(modifiedFillRatio,0.1f,1f);
        uiPlayerShieldBarFill.fillAmount = modifiedFillRatio;
        
        //but adjust it so that it isn't ever 0. The higher the ratio, the slower the shield pulse.
        if (shieldRatio < 0.8f)
        {
            shieldRatio = Mathf.Lerp(3.0f, 0.1f, shieldRatio / 0.8f);
        }
        else
        {
            shieldRatio = 0.1f;
        }
        float offset = (Time.realtimeSinceStartup * shieldRatio)  % 1.0f;
        uiPlayerShieldBarFill.material.SetTextureOffset("_MainTex", new Vector2(offset, 0) );
    }

    void Update()
    {
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            GameLogScript.singleton.UpdateLog();
        }
        
        UpdatePreventOptionMenuToggleTimer();

        if (animatingDialog)
        {
            uiDialogMenuCursorImage.color = transparentColor;
            float percentComplete = (Time.time - timeAtDialogStart) / DIALOG_ANIM_TIME;

            if (percentComplete >= 1.0f)
            {
                animatingDialog = false;
#if UNITY_EDITOR
#endif
                percentComplete = 1.0f;
                uiDialogMenuCursorImage.color = new Color(uiDialogMenuCursorImage.color.r, uiDialogMenuCursorImage.color.g, uiDialogMenuCursorImage.color.b, fCursorOpacity); // Color.white;
            }

            Vector3 scale = new Vector3(DialogBoxScript.TARGET_DIALOGBOX_SCALE, percentComplete * DialogBoxScript.TARGET_DIALOGBOX_SCALE, DialogBoxScript.TARGET_DIALOGBOX_SCALE);
            myDialogBoxComponent.transform.localScale = scale;
        }

        if (requestUpdateScrollbar)
        {
            requestUpdateScrollbarFrames++;
            if (requestUpdateScrollbarFrames >= 8)
            {
                GameLogScript.singleton.uiGameLog.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
                requestUpdateScrollbar = false;
                requestUpdateScrollbarFrames = 0;
            }
        }

        if (waitToUpdateCursorData != null)
        {
            if (waitToUpdateCursorData.waiting)
            {
                waitToUpdateCursorData.framesWaited++;
                if (waitToUpdateCursorData.framesWaited == 2)
                {
                    UpdateCursorPosFromData();
                }
            }
        }

        if (isDraggingItem || isDraggingSkill || draggingGenericObject != null)
        {
            if (GetWindowState(UITabs.COOKING))
            {
                cookingDragger.transform.position = Input.mousePosition;
            }

            if (TDTouchControls.GetMouseButtonDown(1))
            {
                ExitDragMode();
            }
            if (TDTouchControls.GetMouseButtonUp(0))
            {
                framesSinceDragUp = 1;
            }
            if (!TDTouchControls.GetMouseButton(0))
            {
                framesSinceDragUp++;
                if (framesSinceDragUp >= 6)
                {
                    ExitDragMode();
                }
            }
        }

        // Hover tooltips
        if (GetWindowState(UITabs.CHARACTER))
        {
            if ((charSheetStatImage.activeSelf) && (currentCharsheetTooltipObject != null))
            {
                Vector3 v3Pos = Vector3.zero;
                {
                    v3Pos = currentCharsheetTooltipObject.transform.position;
                }

                float height = charSheetStatImage.GetComponent<RectTransform>().rect.height;

                v3Pos.x -= (currentCharsheetTooltipObject.GetComponent<RectTransform>().rect.width);
                v3Pos.x -= 100f;
                v3Pos.y -= 20f;

                if (v3Pos.y < height)
                {
                    v3Pos.y = height;
                }

                charSheetStatImage.transform.position = v3Pos;
            }
        }

        if (framesAfterDialogOpen == 5 && dialogBoxOpen)
        {
            if (TitleScreenScript.CreateStage != CreationStages.SELECTSLOT) UpdateDialogCursorPos();
            framesAfterDialogOpen++;
        }
        else if (dialogBoxOpen)
        {
            framesAfterDialogOpen++;
        }
        if (dialogBoxOpen || GetWindowState(UITabs.EQUIPMENT) || GetWindowState(UITabs.CHARACTER) || jobSheetOpen)
        {

            return;
        }
        if (!GameMasterScript.actualGameStarted)
        {
            return;
        }

        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        UpdateHoverBarState();
        UpdatePlayerShieldUI();
    }

    public void ShowGenericInfoBar(bool forceTop)
    {
        if (WasFullScreenMenuRecentlyOpened || AnyInteractableWindowOpenExceptDialog()) return;
        if (BossHealthBarScript.fillingHealthBarAnimation) return;

        if (GameMasterScript.cameraScript.customAnimationPlaying) return;

        forceInfoBarTop = forceTop;
        if (!genericInfoBar.activeSelf) genericInfoBar.SetActive(true);
        if (BossHealthBarScript.healthBarShouldBeActive)
        {
            BossHealthBarScript.ToggleBossHealthBar(false);
        }
    }

    public void ShowGenericInfoBar()
    {
        ShowGenericInfoBar(false);
    }
    public string GetInfoText()
    {
        return genericInfoBarText.text;
    }
    public void SetInfoText(string text)
    {
        //Debug.Log("Set info text to " + text);
        genericInfoBarText.text = text;
    }
    public void HideGenericInfoBar(bool hideInTargetingMode = true)
    {
        if (hideInTargetingMode && abilityTargeting) return;
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (genericInfoBar != null) genericInfoBar.SetActive(false);
        forceShowInfoBar = false;
        SetInfoText("");
        if (BossHealthBarScript.healthBarShouldBeActive)
        {
            BossHealthBarScript.ToggleBossHealthBar(true);
        }
    }

    public void ReadFromSave(XmlReader reader)
    {
        //Debug.Log(reader.Name + " " + reader.NodeType);
        reader.ReadStartElement();
        activeWeaponSlot = reader.ReadElementContentAsInt();
        reader.ReadStartElement(); // activeweapons
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            int weapID = reader.ReadElementContentAsInt();
            if (weapID == 0)
            {
                // Do nothing.
            }
            else
            {
                bool foundWeapon = false;
                foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
                {
                    if (itm.actorUniqueID == weapID)
                    {
                        Weapon wp = itm as Weapon;
                        AddWeaponToActiveSlot(wp, i);
                        foundWeapon = true;
                        break;
                    }
                }
                if (!foundWeapon)
                {
                    for (int e = 0; e < GameMasterScript.heroPCActor.myEquipment.equipment.Length; e++)
                    {
                        Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[e];
                        if (eq != null)
                        {
                            if (eq.actorUniqueID == weapID)
                            {
                                Weapon wp = eq as Weapon;
                                AddWeaponToActiveSlot(wp, i);
                                //Debug.Log("Adding " + wp.displayName + " to active slot " + i);
                                foundWeapon = true;
                                break;
                            }
                        }
                    }
                }

            }
        }
        reader.ReadEndElement(); // end weapon hotbar

        if (reader.Name.ToLowerInvariant() == "invsort")
        {
            int type = reader.ReadElementContentAsInt();
            Switch_UIInventoryScreen.lastSortType = (InventorySortTypes)type;
        }

        reader.ReadStartElement(); // start abil hotbar
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            string strValue = reader.Name.ToLowerInvariant();
            switch (strValue)
            {
                case "ability":
                    string abilRef = reader.ReadElementContentAsString();
                    foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
                    {
                        if (abil.refName == abilRef)
                        {
                            AddAbilityToSlot(abil, i, true);
                            break;
                        }
                    }
                    break;
                case "consumable":
                    int itemID = reader.ReadElementContentAsInt();
                    foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
                    {
                        if (itm.actorUniqueID == itemID)
                        {
                            Consumable consume = itm as Consumable;
                            AddItemToSlot(consume, i, true);
                            break;
                        }
                    }
                    break;
                case "nothing":
                    reader.ReadElementContentAsString();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement(); // end abil hotbar

        if (reader.Name == "jpsincelastsheetopen")
        {
            jpGainedSinceJobScreenToggled = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
        }

        reader.ReadEndElement(); // end ui

        if (activeWeaponSlot < 0 || activeWeaponSlot >= hotbarWeapons.Length)
        {
            activeWeaponSlot = 0;
        }

        SwitchActiveWeaponSlot(activeWeaponSlot, true);
    }

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("ui");
        writer.WriteElementString("activeweaponslot", activeWeaponSlot.ToString());
        writer.WriteStartElement("activeweapons");
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (hotbarWeapons[i] != null)
            {
                writer.WriteElementString("slot", hotbarWeapons[i].actorUniqueID.ToString());
            }
            else
            {
                writer.WriteElementString("slot", (0).ToString());
            }
        }
        writer.WriteEndElement();

        writer.WriteElementString("invsort", ((int)Switch_UIInventoryScreen.lastSortType).ToString());

        writer.WriteStartElement("hotbar");

        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i] != null)
            {
                if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
                {
                    writer.WriteElementString("ability", hotbarAbilities[i].ability.refName);
                }
                if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
                {
                    writer.WriteElementString("consumable", hotbarAbilities[i].consume.actorUniqueID.ToString());
                }
                if (hotbarAbilities[i].actionType == HotbarBindableActions.NOTHING)
                {
                    writer.WriteElementString("nothing", "nobind");
                }
            }
        }
        
        writer.WriteEndElement(); // end hotbar write

        writer.WriteElementString("jpsincelastsheetopen", jpGainedSinceJobScreenToggled.ToString());

        writer.WriteEndElement();
    }

    public void EQItemButtonClicked(int itemSlotInList)
    {
        uiObjectFocus.mySubmitFunction.Invoke(itemSlotInList); // Is this going to duplicate functionality?
    }

    public static bool charCreationInputFieldActivated = false;
    public static void ResetNameInputCursor()
    {
        singletonUIMS.DisableCursor();
        HideDialogMenuCursor();

        charCreationInputFieldActivated = true;
        CharCreation.nameInputTextBox.ActivateInputField();        
        uiObjectFocus = null;
        titleScreenNameInputDone = false;
    }
    
    public UITabs GetCurrentUITab()
    {
        if (GetWindowState(UITabs.EQUIPMENT)) return UITabs.EQUIPMENT;
        if (GetWindowState(UITabs.INVENTORY)) return UITabs.INVENTORY;
        if (GetWindowState(UITabs.RUMORS)) return UITabs.RUMORS;
        if (GetWindowState(UITabs.SKILLS)) return UITabs.SKILLS;
        if (jobSheetOpen) return UITabs.SKILLS;
        if (GetWindowState(UITabs.OPTIONS)) return UITabs.OPTIONS;
        if (GetWindowState(UITabs.CHARACTER)) return UITabs.CHARACTER;
        return UITabs.NONE;
    }

    // #todo - Deprecate this? (Andrew)
    public void SetUIFocus(UITabs tab)
    {
        Debug.Log("UI focus changing to " + tab);

        switch (tab)
        {
            case UITabs.EQUIPMENT:
                SetWindowState(UITabs.EQUIPMENT, true);
                break;
            case UITabs.INVENTORY:
                SetWindowState(UITabs.INVENTORY, true);
                break;
            case UITabs.CHARACTER:
                SetWindowState(UITabs.CHARACTER, true);
                break;
            case UITabs.SKILLS:
                SetWindowState(UITabs.SKILLS, true);
                break;
            case UITabs.OPTIONS:
                SetWindowState(UITabs.OPTIONS, true);
                break;
            case UITabs.RUMORS:
                SetWindowState(UITabs.RUMORS, true);
                break;
        }

        uiTabSelected = tab;
    }

    public void SwitchUITabs(int newTab)
    {
        OpenFullScreenUI((UITabs)newTab);        
    }

    public static void UpdateUINavHighlights()
    {
        for (int i = 0; i < uiNavButtons.Length; i++)
        {
            uiNavButtons[i].gameObj.GetComponent<Image>().color = transparentColor;
            uiNavButtons[i].subObjectImage.GetComponent<Image>().sprite = uiNavDeselected[i];
            uiNavButtons[i].subObjectImage.GetComponent<Image>().color = Color.white;
        }

        uiNavButtons[(int)singletonUIMS.GetCurrentUITab()].subObjectImage.GetComponent<Image>().sprite = uiNavSelected[(int)singletonUIMS.GetCurrentUITab()];

        switch ((int)singletonUIMS.GetCurrentUITab())
        {
            case (int)UITabs.EQUIPMENT:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_1");
                break;
            case (int)UITabs.INVENTORY:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_2");
                break;
            case (int)UITabs.CHARACTER:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_5");
                break;
            case (int)UITabs.SKILLS:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_3");
                break;
            case (int)UITabs.RUMORS:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_6");
                break;
            case (int)UITabs.OPTIONS:
                uiNavHeaderText.text = StringManager.GetString("dialog_menuselect_intro_btn_0");
                break;
        }
    }

    public void ShowUINavigation()
    {
        if (MinimapUIScript.GetOverlay())
        {
            MinimapUIScript.StopOverlay();
        }
        mainUINav.gameObj.SetActive(true);
        ShowUICharacterStatBlock();
    }

    public void HideUINavigation(bool bHideImmediately = false)
    {
        mainUINav.gameObj.SetActive(false);
        HideUICharacterStatBlock(bHideImmediately);
    }

    public void TryUnequipSelectedFromButton(int dummy)
    {
        TryUnequipSelected();
    }

    public static void ShowSkillToolTipContainer()
    {
        //skillTooltipContainer.SetActive(true);
    }
    public static void HideSkillToolTipContainer()
    {
        //skillTooltipContainer.SetActive(false);
    }

    public void SubmenuChangeMouseFocus(int index)
    {
        if (eqSubmenuOpen)
        {
            switch (index)
            {
                case 0:
                    ChangeUIFocusAndAlignCursor(eqSubmenuEquip);
                    break;
                case 1:
                    ChangeUIFocusAndAlignCursor(eqSubmenuUnequip);
                    break;
                case 2:
                    ChangeUIFocusAndAlignCursor(eqSubmenuEquipOffhand);
                    break;
                case 3:
                    ChangeUIFocusAndAlignCursor(eqSubmenuEquipAcc1);
                    break;
                case 4:
                    ChangeUIFocusAndAlignCursor(eqSubmenuEquipAcc2);
                    break;
                case 5:
                    ChangeUIFocusAndAlignCursor(eqSubmenuHotkey1);
                    break;
                case 6:
                    ChangeUIFocusAndAlignCursor(eqSubmenuHotkey2);
                    break;
                case 7:
                    ChangeUIFocusAndAlignCursor(eqSubmenuHotkey3);
                    break;
                case 8:
                    ChangeUIFocusAndAlignCursor(eqSubmenuHotkey4);
                    break;
                case 9:
                    ChangeUIFocusAndAlignCursor(eqSubmenuDrop);
                    break;
                case 10:
                    ChangeUIFocusAndAlignCursor(eqSubmenuFavorite);
                    break;
                case 11:
                    ChangeUIFocusAndAlignCursor(eqSubmenuPairWithMainhand);
                    break;
            }
        }
        else if (invSubmenuOpen)
        {
            switch (index)
            {
                case 0:
                    ChangeUIFocusAndAlignCursor(invSubmenuUse);
                    break;
                case 9:
                    ChangeUIFocusAndAlignCursor(invSubmenuDrop);
                    break;
                case 10:
                    ChangeUIFocusAndAlignCursor(invSubmenuFavorite);
                    break;
            }
        }
    }

    public static bool IsAnyTooltipOpen()
    {
        return eqSubmenuOpen || invSubmenuOpen;
    }

    public static void AddUIObject(UIObject obj)
    {
        allUIObjects.Add(obj);
    }

    public static void ClearUIObjects()
    {
        allUIObjects.Clear();
    }

    public IEnumerator WaitThenDisableObject(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        go.SetActive(false);
    }

    public IEnumerator WaitThenFadeOverlay(float time)
    {
        yield return new WaitForSeconds(time);
        if (!skippingOverlayText)
        {
            screenOverlayTextCGFader.FadeOut(1.0f);
            screenOverlayBGCGFader.FadeOut(1.0f);
            StartCoroutine(WaitThenDisableObject(screenOverlayStuff, 1.1f));
        }
    }

    public IEnumerator WaitThenFadeIntroOverlay(float time)
    {
        yield return new WaitForSeconds(time);
        introOverlayTextFader.FadeOut(1.0f);
        introOverlayBGFader.FadeOut(1.0f);
        StartCoroutine(WaitThenDisableObject(introOverlayDescText.transform.parent.gameObject, 1.1f));
    }

    public IEnumerator WaitThenTryStoredOverlayTextData(float time)
    {
        yield return new WaitForSeconds(time);
        if ((!AnyInteractableWindowOpen()) && (queuedOverlayTextData != null))
        {
            WriteOverlayText(queuedOverlayTextData);
        }
    }

    public static bool HasOverlayText()
    {
        if (screenOverlayStuff == null) return false;
        return screenOverlayStuff.activeSelf;
    }

    public static void SkipOverlayText()
    {
        if (skippingOverlayText) return;
        skippingOverlayText = true;
        screenOverlayTextCGFader.FadeOut(1.0f);
        screenOverlayBGCGFader.FadeOut(1.0f);
        singletonUIMS.StartCoroutine(singletonUIMS.WaitThenDisableObject(screenOverlayStuff, 1.1f));
    }

    public void CloseSkillTooltip(int x)
    {
        HideSkillToolTipContainer();
    }

    public static void WriteOverlayText(OverlayTextData otd, bool markAsReadButDontDisplay = false)
    {       
        if (string.IsNullOrEmpty(otd.refName)) return;

        if (GameMasterScript.heroPCActor.ReadActorData(otd.refName) != -1)
        {
            return;
        }

        if (!dialogBoxOpen)
        {
            if (otd.showOnlyOnce)
            {
                GameMasterScript.heroPCActor.SetActorData(otd.refName, 1);
            }

            if (markAsReadButDontDisplay)
            {
                return;
            }

            if (otd.introText)
            {
                introOverlayDescText.transform.parent.gameObject.SetActive(true);
                introOverlayDescText.text = otd.descText;
                introOverlayBGFader.FadeIn(0.35f);
                introOverlayTextFader.FadeIn(0.9f);
                singletonUIMS.StartCoroutine(singletonUIMS.WaitThenFadeIntroOverlay(6.2f));
                introOverlayDescText.transform.position = introOverlayDescText.transform.position; // force parse?
            }
            else
            {
                skippingOverlayText = false;
                screenOverlayStuff.SetActive(true);
                screenOverlayDescText.text = otd.descText;
                screenOverlayHeaderText.text = otd.headerText;
                screenOverlayTextCGFader.FadeIn(1.1f);
                screenOverlayBGCGFader.FadeIn(0.35f);
                singletonUIMS.StartCoroutine(singletonUIMS.WaitThenFadeOverlay(6.2f));
                screenOverlayDescText.transform.position = screenOverlayDescText.transform.position; // force parse?
            }

            GameLogScript.GameLogWrite(otd.descText, GameMasterScript.heroPCActor);

            queuedOverlayTextData = null;

        }
        else
        {
            queuedOverlayTextData = otd;
        }
    }

    public void AdjustItemWorldGoldTribute()
    {
        if ((!ItemWorldUIScript.isItemSelected) || (ItemWorldUIScript.itemSelected == null)) return;
        PlayCursorSound("Tick");
        float actualValue = ItemWorldUIScript.TryAdjustGoldAmount(ItemWorldUIScript.itemGoldSlider.value);
        ItemWorldUIScript.itemGoldSlider.value = actualValue;

    }

    public static void CheckForInfusionDialog()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("infuse1") == 99)
        {
            UIManagerScript.StartConversationByRef("flask1_upgrade", DialogType.LEVELUP, null);
        }
        else if (GameMasterScript.heroPCActor.ReadActorData("infuse2") == 99)
        {
            UIManagerScript.StartConversationByRef("flask2_upgrade", DialogType.LEVELUP, null);
        }
        else if (GameMasterScript.heroPCActor.ReadActorData("infuse3") == 99)
        {
            UIManagerScript.StartConversationByRef("flask3_upgrade", DialogType.LEVELUP, null);
        }
    }

    //Plays an error sound, wiggles the target and puts a !?! over their head
    public static void DisplayPlayerError(Actor target)
    {
        UIManagerScript.PlayCursorSound("Error");
        target.myMovable.Jitter(0.1f);
        CombatManagerScript.SpawnChildSprite("AggroEffect", target, Directions.NORTHEAST, false);
    }

    public static void PlaceDialogBoxInFrontOfFade(bool state)
    {
        if (state)
        {
            myDialogBoxComponent.transform.SetSiblingIndex(singletonUIMS.whiteFade.transform.GetSiblingIndex() + 1);
        }
        else
        {
            myDialogBoxComponent.transform.SetSiblingIndex(singletonUIMS.blackFade.transform.GetSiblingIndex() - 1);
        }
    }

    IEnumerator WaitThenToggleUITab(float time, UITabs tab, EMessageForFullScreenUI fullScreenMessage = EMessageForFullScreenUI.none)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.SetAnimationPlaying(false);
        ToggleUITab(tab);
    }

    public static UITabs lastUITabOpened = UITabs.CHARACTER;

    void ToggleUITab(UITabs tab, bool forceClose = false)
    {
        lastUITabOpened = tab;

        bool doAlternateGamepadStyleOptionsCheck = false;

        if (currentFullScreenUI != null && currentFullScreenUI.GetUIType() == tab)
        {
            currentFullScreenUI.ToggleActive(forceClose);
            bFullScreenUIOpen = currentFullScreenUI.gameObject.activeSelf;
        }
        else
        {
            switch (tab)
            {
                //case UITabs.CHARACTER:
                //    ToggleCharacterSheet();
                //    break;
                case UITabs.OPTIONS:
                    if (!PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
                    {
                        ToggleOptionsMenu(forceClose);
                    }             
                    else
                    {
                        doAlternateGamepadStyleOptionsCheck = true;
                    }
                    break;
                case UITabs.RUMORS:
                    ToggleQuestSheet(forceClose);
                    break;
                default:
                    if (dictFullScreenUI.ContainsKey(tab))
                    {
                        currentFullScreenUI = dictFullScreenUI[tab];
                        currentFullScreenUI.ToggleActive(forceClose);
                        bFullScreenUIOpen = currentFullScreenUI.gameObject.activeSelf;
                    }
                    break;
            }
        }

        if (doAlternateGamepadStyleOptionsCheck)
        {
            if (dictFullScreenUI.ContainsKey(tab))
            {
                currentFullScreenUI = dictFullScreenUI[tab];
                currentFullScreenUI.ToggleActive(forceClose);
                bFullScreenUIOpen = currentFullScreenUI.gameObject.activeSelf;
            }
        }

        //if we turned off the dialog, make sure to clear the value     
        if (bFullScreenUIOpen == false)
        {
            currentFullScreenUI = null;
            HideDialogMenuCursor();
        }
    }

    public static IEnumerator FadeOutThenInForUI(float fOutTime, float fWaitTime, float fInTime, bool closingUI = false)
    {
        //If we are mid some animation already, don't do dis.
        if (GameMasterScript.IsNextTurnPausedByAnimations())
        {
            yield break;
        }

        //GameMasterScript.SetAnimationPlaying(true);

        //Debug.Log("Fade out then in. " + fOutTime + " " + fWaitTime + " " + fInTime + " " + closingUI);

        //fade out for X time
        FadeOut(fOutTime);

        if (!closingUI)
        {
            FadeGradientOut(fInTime);
        }
        else
        {
            FadeGradientIn(fInTime);            
        }

        //X time later, fade back in
        yield return new WaitForSeconds(fOutTime + fWaitTime);

        //return!
        FadeIn(fInTime);
        

        //and then
        yield return new WaitForSeconds(fInTime);
        GameMasterScript.SetAnimationPlaying(false);
    }



    public static float GetCanvasScale()
    {
        return Screen.width / 1920f; // This should probably Just Work because 1920 is the reference resolution.

        //return singletonUIMS.myCanvas.localScale.x;
    }

    // This does not, itself, start or end a fade. The *fade* object is what calls this function,
    // reporting on its current state. This state can then be checked by other functions to ensure we 
    // don't have overlapping fades.
    public static void SetFadeState(EFadeStates state)
    {
        blackFadeState = state;
    }

    public static EFadeStates GetFadeState()
    {
        return blackFadeState;
    }

    public static IEnumerator WaitThenPlayCursorSound(float time, string cue)
    {
        yield return new WaitForSeconds(time);
        PlayCursorSound(cue);
    }

    public static bool IsCurrentConversationKeyStory()
    {
        if (dialogBoxType == DialogType.KEYSTORY)
        {
            return true;
        }
        if (currentTextBranch != null && currentTextBranch.enableKeyStoryState)
        {
            return true;
        }
        return false;
    }

    public static void RefreshEnergyShield()
    {
        //player shielding
        var hero = GameMasterScript.GetHeroActor();
        float pMax = 0f;
        if (hero.ReadActorData("shieldinfo_dirty") == 1)
        {
            hero.SetActorData("shieldinfo_dirty", 0);
            float shieldCurrent = hero.ReadActorData("flowshield_dmgleft");
            float shieldMax = hero.ReadActorData("flowshield_dmgmax");

            if (shieldCurrent > 0)
            {
                pMax = shieldCurrent / shieldMax;
                uiPlayerShieldBarFill.fillAmount = pMax;

                //draw shield on the left, health on the right.
                uiPlayerShieldBarText.text = ((int)shieldCurrent).ToString();
                uiPlayerShieldBarText.alignment = TextAlignmentOptions.Left;

                uiPlayerHealth.outlineColor = Color.red;
                uiPlayerHealth.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                uiPlayerShieldBarFill.fillAmount = 0f;
                uiPlayerShieldBarText.text = null;
                uiPlayerHealth.alignment = TextAlignmentOptions.Left;
                uiPlayerHealth.outlineColor = Color.clear;

            }
        }
    }



    public static bool TryLoadDialogCursor()
    {
        //are we too sleepy to even care
        if (singletonUIMS == null) return false;

        //did we already load it up?
        if (singletonUIMS.uiDialogMenuCursor != null) return true;

        //ok go load it up
        singletonUIMS.uiDialogMenuCursor = GameObject.Find("Dialog Cursor");

        //wait what
        if (singletonUIMS.uiDialogMenuCursor == null) return false;

        singletonUIMS.uiDialogMenuCursorImage = singletonUIMS.uiDialogMenuCursor.GetComponent<Image>();
        singletonUIMS.uiDialogMenuCursorAudioComponent = singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>();
        singletonUIMS.uiDialogMenuCursorBounce = singletonUIMS.uiDialogMenuCursor.GetComponent<CursorBounce>();
        singletonUIMS.uiDialogMenuCursorImage.enabled = true;

        return true;
    }
		
    void SetAvailableDisplayResolutions()
    {
        for (int i = 0; i < possibleDisplayResolutions.Length; i++)
        {
            float idealRatio = 16f / 9f;
            float checkRatio = (float)possibleDisplayResolutions[i].width / (float)possibleDisplayResolutions[i].height;
            if (checkRatio < 1.6f) continue;
            //if (Mathf.Abs(checkRatio - idealRatio) > 0.02f) continue;

            bool skip = false;

            foreach (Resolution r in availableDisplayResolutions)
            {
                if (r.width == possibleDisplayResolutions[i].width && r.height == possibleDisplayResolutions[i].height)
                {
                    skip = true;
                    break;
                }

            }

            if (skip) continue;

            availableDisplayResolutions.Add(possibleDisplayResolutions[i]);
        }

        availableDisplayResolutions.Sort((a, b) => (a.width.CompareTo(b.width)));
    }	
	
    /// <summary>
    /// Returns all the children of the dbox image to the stack.
    /// </summary>
    public static void ClearChildrenOnDialogBox()
    {
        //goodbye kids
        myDialogBoxComponent.ClearChildGameObjects();
    }

    /// <summary>
    /// Instantiates an object and attaches it to the dialogboximage
    /// </summary>
    /// <param name="strPrefabName"></param>
    /// <param name="vOffset"></param>
    public static void SetChildObjectOnDialogBox(string strPrefabName, Vector2 vOffset)
    {
        //attach like so
        var newGO = GameMasterScript.TDInstantiate(strPrefabName);
        var rt = newGO.transform as RectTransform;
        rt.SetParent(dialogBoxImage.rectTransform);

        //and scoot it where it should be scooted to
        rt.localPosition = vOffset;
        rt.localScale = Vector3.one;

        //keep track of this
        myDialogBoxComponent.AddChildGameObject(newGO);
    }	

    public static bool CurrentConversationIsSelectSlot()
    {
        if (currentConversation != null && (currentConversation.refName == "loadgame" || currentConversation.refName == "newgame" || currentConversation.refName == "managedata"))
        {
            if (TitleScreenScript.CreateStage == CreationStages.SELECTSLOT)
            {
                return true;
            }
            
        }

        return false;
    }

    public static void ToggleBackgroundGradientImage(bool state)
    {
        #if UNITY_SWITCH
            return;
        #endif
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;
        //Debug.Log("Request toggle bg gradient image: " + state);
        singletonUIMS.gradientBGImageForUltraWide.gameObject.SetActive(state);
    }

    public static void ToggleBGPlayerBar(bool state)
    {
        #if UNITY_SWITCH
            return;
        #endif
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) return;
        //Debug.Log("Request toggle bg player bar: " + state);
        singletonUIMS.uiBGImageBar.SetActive(state);
    }
}
