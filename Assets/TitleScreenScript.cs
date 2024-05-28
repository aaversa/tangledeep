using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine.UI;
using TMPro;
using System;
using LapinerTools.Steam.Data;
using LapinerTools.uMyGUI;
using Steamworks;

using System.IO;
using System.Runtime;
using Random = UnityEngine.Random;

public enum CreationStages { SELECTSLOT, SELECTMODE, NAMEINPUT, JOBSELECT, PERKSELECT, GAME_MODS, TITLESCREEN, COMMUNITY,
    CAMPAIGNSELECT, COUNT }

public enum TextFadeStates { WAIT_TO_FADE_IN, FADING_IN, HOLDING, FADING_OUT, COMPLETE, COUNT }

[System.Serializable]
public partial class TitleScreenScript : MonoBehaviour
{
    [Header("Logo and Splash Stuff")]
    public TextMeshProUGUI buildText;
    public TextMeshProUGUI creditsText;
    public Transform logoHolder; // Container that holds Tangledeep and DLC logos
    public GameObject legendOfSharaLogo; // The DLC logo object
    public GameObject dragonsLogo;
    public Transform legendOfSharaTransform; // DLC logo transform
    public Image legendOfSharaImage; // DLC logo image

    public Image dragonsImage;

    public Transform openingDialogBox; // Main menu dialog box
    public Animatable sharaPrefab; // Anim component of shara sprite on cliff
    public Transform sharaPrefabTransform; // Transform of shara sprite
    public Image sharaPrefabImage; // Image of shara sprite
    Color sharaColor = Color.white;
    Color dragonsColor = Color.white;
    public Sprite sharaBackgroundLayer03Mid; // BG layer to use if we have DLC installed
    public Image scrollingLayer03; // image component of 3rd layer in title crawl

    bool isFinishing;

    enum ETitleScreenStates
    {
        unloaded = 0,
        loading,
        ready_to_scroll,
    }

    public bool sharaModeUnlocked;

    private ETitleScreenStates titleScreenState;

    private ETitleScreenStates TitleScreenState
    {
        get
        {
            return titleScreenState;
        }
        set
        {
            //if (Debug.isDebugBuild) Debug.Log("Setting titleScreenState to " + value);
            titleScreenState = value;
        }
    }

    public PlayerModManager modManager;
    public enum EWhyAreWeInTheSelectSlotWindow
    {
        because_load_game = 0,
        because_delete_game,
        because_new_game,
        max,
    }

    [HideInInspector]
    public static EWhyAreWeInTheSelectSlotWindow reasonWeAreInSelectSlotWindow;
    int framesBeforeClearDebug = 3;
    [Header("Scrolling Info")]
    [Tooltip("All the title screen art should child to this to make sure it is in the proper position and order")]
    public GameObject titleScreenScrollAnchor;

    public float buildNumber;
    public static TitleScreenScript titleScreenSingleton;
    public UIManagerScript uims;
    public float movementInputOptionsTime;
    private bool bufferingInput;
    private float inputBufferCount = 0.0f;
    float joystickDeadZone = 0.25f;
    private bool submitConfirm = false;

    GameObject LogoElements; //used on PS4 and XBOXONE

    static CreationStages charCreationStage;
    public static CreationStages CreateStage
    {
        get
        {
            return charCreationStage;
        }
        set
        {
            //if (Debug.isDebugBuild) Debug.Log("Setting char creation stage to " + value);
            charCreationStage = value;
            if (charCreationStage == CreationStages.SELECTSLOT)
            {
                cursorIsOnChangePages = false;
                // Set our cursor position to the first save slot when we enter this mode.                
            }
        }
    }
    public MusicManagerScript mms;
    public float[] inputAxes;

    public int framesSinceNeutral;
    public float timeDirectionPressed;

    private GameObject[] titleBGLayers;
    private SimpleTimedMove[] layerMovers;

    public bool scrollingTitleBG = false;
    public float bgScrollTime;
    public float textWaitToFadeTime;
    public float textFadeTime;
    public float textHoldTime;
    public float textFadeOutTime;
    float timeTextEvent;
    TextFadeStates creditsFadeTextState;
    public float logoStartFadeInTime;
    float timeScrollStarted;
    bool logoFading;
    float timeLogoStarted;
    CanvasGroup logoCG;

    public bool blackFading;
    public float blackFadeInTime;
    Image blackFadeImage;

    public Player player;
    public bool loadingGame;

    public ControlMapper cMapper;
    static bool allowInput;

    public static bool webChallengesLoaded;

    [Header("Localization")]
    [Tooltip("This probably isn't the best place to set this value, but hey.")]
    public EGameLanguage GameLanguage;

    public TextMeshProUGUI worldSeedPlaceholder;
    public TextMeshProUGUI nameInputPlaceholder;

    static float MAIN_MENU_DEFAULT_YPOS = -80f;

    public TextMeshProUGUI nameInputText;
    public TextMeshProUGUI worldSeedText;
    public TextMeshProUGUI confirmNameButton;
    public TextMeshProUGUI randomNameButton;

    /// <summary>
    /// This is used for the "Switch-style" save game slots, which we are now switching to
    /// </summary>
    public GameObject prefabSaveDataBlock;

    [HideInInspector]    
    public static GameObject PrefabSaveDataBlock;

    private int idxActiveSaveSlotInMenu;

    // Use this for initialization

    public static bool bReadyForMainMenuDialog = false;
    static List<string> debugAllGameObjectsInScene;

    static bool waitingForSaveSlotsToLoad;

    public static bool WaitingForSaveSlotsToLoad
    {
        get
        {
            return waitingForSaveSlotsToLoad;
        }
        set
        {
            waitingForSaveSlotsToLoad = value;
        }
    }

    [Header("Main Canvas")]

    public Canvas titleScreenCanvas;

    public static void AllowInput(bool state)
    {
        allowInput = state;
    }        

    public static void OnChallengesLoaded()
    {
        webChallengesLoaded = true;
    }    

    public static void ChangeControlType(KeyboardControlMaps kcm)
    {
		if (PlatformVariables.GAMEPAD_ONLY) return;

        if (Debug.isDebugBuild) Debug.Log("CONTROL MAPPER: Title Screen loading map " + kcm);
        titleScreenSingleton.player.controllers.maps.ClearMaps(ControllerType.Keyboard, false);
        titleScreenSingleton.player.controllers.maps.RemoveMap(ControllerType.Keyboard, 0, 0);
        switch (kcm)
        {
            case KeyboardControlMaps.DEFAULT:
                LayoutHelper.SwitchLayout(0, ControllerType.Keyboard, 0, "Default", "Default");

                PlayerOptions.keyboardMap = KeyboardControlMaps.DEFAULT;
                PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.DEFAULT;
                break;
            case KeyboardControlMaps.WASD:
                LayoutHelper.SwitchLayout(0, ControllerType.Keyboard, 0, "Default", "WASD");
                PlayerOptions.keyboardMap = KeyboardControlMaps.WASD;
                PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.WASD;
                break;
        }
        titleScreenSingleton.player.controllers.maps.SetMapsEnabled(true, "Default");
        titleScreenSingleton.player.controllers.maps.SetMapsEnabled(true, "MenuControls");

        titleScreenSingleton.cMapper.keyboardMapDefaultLayout = (int)kcm;

        PlayerOptions.WriteOptionsToFile();
        ReInput.userDataStore.Save();
        TDPlayerPrefs.Save();
    }

    public static void ReturnToMenu()
    {
        if (WaitingForSaveSlotsToLoad)
        {
            if (Debug.isDebugBuild) Debug.Log("But I can't do that yet.");
            WaitingForSaveSlotsToLoad = false;
            return;
        }
        UIManagerScript.CloseDialogBox();
        foreach (var sd in UIManagerScript.saveDataDisplayComponents)
        {
            if (sd != null && sd.gameObject != null)
            {
                sd.gameObject.SetActive(false);
            }
        }
        //reset this values so we go through all of character create a second time
        CharCreation.NameEntryScreenState = ENameEntryScreenState.max;
        GameMasterScript.gmsSingleton.SetTempGameData("first_reset_feats", 0);
        GameStartData.FlushData();
        GameStartData.ClearPlayerFeats();
        titleScreenSingleton.InitializeTitleScreen(false);
    }

    public void Debug_SkipStart_FinishTitleScroll()
    {
        FinishTitleScroll();
    }

    void FinishTitleScroll()
    {
        StartCoroutine(FinishScrollCoroutine());
    }
    IEnumerator FinishScrollCoroutine()
    {
        if (isFinishing)
        {
            yield break;
        }

        isFinishing = true;
        yield return new WaitWhile(() => !bReadyForMainMenuDialog);
        blackFading = false;
        blackFadeImage.color = new Color(0f, 0f, 0f, 0f);
        logoCG.alpha = 1f;
        scrollingTitleBG = false;
        logoFading = false;
        creditsText.gameObject.SetActive(false);

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
			bool showSharaConditional = true;
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE
            showSharaConditional = DLCManager.ShouldShowLegendOfSharaTitleScreen();
#endif
            if (showSharaConditional || legendOfSharaImage.color.a > 0f)
            {
                legendOfSharaImage.color = Color.white;
            }                
        }
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            if ((!DLCManager.ShouldShowLegendOfSharaTitleScreen() || dragonsImage.color.a > 0f) ||
                TDPlayerPrefs.GetInt(GlobalProgressKeys.LAST_PLAYED_CAMPAIGN) != (int)StoryCampaigns.SHARA)
            {
                legendOfSharaImage.color = UIManagerScript.transparentColor;
                dragonsImage.color = Color.white;
            }
        }


        InitializeTitleScreen(true);
    }



    /// <summary>
    /// What to do with the player's response to being asked about NG+?
    /// </summary>
    /// <param name="bc"></param>
    private void OnDialogConfirm_ShouldStartNGPlus(ButtonCombo bc)
    {
        switch (bc.actionRef)
        {
            case "no":
                UIManagerScript.PlayCursorSound("Confirm");
                UIManagerScript.singletonUIMS.StartCharacterCreation_Mirai();
                break;
            case "yes":
                UIManagerScript.PlayCursorSound("OPSelect");
                StartNGPlusSwitch();
                break;
            case "oldsave":
                UIManagerScript.PlayCursorSound("OPSelect");
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.LOADGAME);
                UIManagerScript.HideDialogMenuCursor();
                var uims = UIManagerScript.singletonUIMS;
                uims.StartCoroutine(uims.FadeOutThenLoadGame());
                break;
            case "cancel":
                UIManagerScript.PlayCursorSound("Cancel");
                ReturnToMenu();
                break;
        }
    }

    /// <summary>
    /// Starts a NewGame+
    /// </summary>
    private void StartNGPlusSwitch()
    {
        if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 0)
        {
            GameStartData.NewGamePlus = 1;
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAMEPLUS);
        }
        else
        {
            GameStartData.NewGamePlus = 2;
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAMEPLUS);
        }
        
        //but the important part is that we remember the last dialog response was NEWGAMEPLUS because
        //we check the dialog box during all data loading to see if we're NG+ or not.        
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.FadeOutThenLoadGame());
    }

    /// <summary>
    /// Maintains the position of the selection cursor during updates
    /// </summary>
    void UpdateCursorDuringSlotSelection()
    {
        //only during slot selection, obvs
        if (CreateStage != CreationStages.SELECTSLOT)
        {
            return;
        }

        var sdComponents = UIManagerScript.saveDataDisplayComponents;

        //make sure we have all the objects we need, since they are generated in coroutines.
        if (!UIManagerScript.saveDataComponentsCreated || sdComponents.Length <= idxActiveSaveSlotInMenu)
        {
            Debug.Log("Data not ready");
            return;
        }

        var sdc = sdComponents[idxActiveSaveSlotInMenu];
        if (sdc == null || sdc.imgPortrait == null)
        {
            Debug.Log("Components not ready");
            return;
        }

        UIManagerScript.AlignCursorPos(sdc.imgPortrait.gameObject, -5.0f, 0, false);
    }


    /// <summary>
    /// Handles (Switch-based) input for this last part of character creation.
    /// Only three buttons are allowed at first: change, confirm, randomize.
    /// </summary>
    /// <returns></returns>
    public bool HandleSelectNameAndConfirmInput(Directions dInput)
    {
        switch (CharCreation.NameEntryScreenState)
        {
            case ENameEntryScreenState.deciding_on_name:
                CharCreation.HandleInputNameEntry_DecidingOnName();
                break;
            case ENameEntryScreenState.name_confirmed_and_ready_to_go:
                CharCreation.HandleInputNameEntry_ConfirmedAndReady(dInput);
                break;
            case ENameEntryScreenState.max:
            case ENameEntryScreenState.game_loading_stop_updating:
                return false;
        }

        return true;
    }

    public static void HandleTitleScreenDialogInput(DialogButtonResponse dbr)
    {
        if (WaitingForSaveSlotsToLoad) return;

        UIManagerScript singletonUIMS = UIManagerScript.singletonUIMS;
        if (dbr == DialogButtonResponse.SETUP_BOTHHANDS)
        {
            UIManagerScript.PlayCursorSound("Select");
            TitleScreenScript.ChangeControlType(KeyboardControlMaps.DEFAULT);
            TitleScreenScript.ReturnToMenu();
        }
        if (dbr == DialogButtonResponse.CONTINUE)
        {
            TitleScreenScript.ReturnToMenu();
        }
        if (dbr == DialogButtonResponse.SETUP_WASD)
        {
            UIManagerScript.PlayCursorSound("Select");
            TitleScreenScript.ChangeControlType(KeyboardControlMaps.WASD);
            TitleScreenScript.ReturnToMenu();
        }

        if (dbr == DialogButtonResponse.NEWGAME)
        {
            UIManagerScript.PlayCursorSound("Select");
            singletonUIMS.StartCoroutine(singletonUIMS.SelectSaveSlot(DialogButtonResponse.NEWGAME));
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAME);
            return;
        }
        if (dbr == DialogButtonResponse.MANAGEDATA)
        {
            UIManagerScript.PlayCursorSound("Select");
            singletonUIMS.StartCoroutine(singletonUIMS.SelectSaveSlot(DialogButtonResponse.MANAGEDATA));
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.MANAGEDATA);
            return;
        }
        if (dbr == DialogButtonResponse.LOADGAME)
        {
            UIManagerScript.PlayCursorSound("Select");
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.LOADGAME);

            singletonUIMS.StartCoroutine(singletonUIMS.SelectSaveSlot(DialogButtonResponse.LOADGAME));
            return;
        }
        if (dbr == DialogButtonResponse.COMMUNITY)
        {
            Vector2 size = new Vector2(Screen.width * 0.7f, 5f);
            Vector2 pos = new Vector2(0f, -50f);
            TitleScreenScript.CreateCommunityJoinDialog(size, pos);
            CreateStage = CreationStages.COMMUNITY;
        }

        if (dbr == DialogButtonResponse.COMMUNITY_REJECT)
        {
            Vector2 size = new Vector2(400f, 5f);
            Vector2 pos = new Vector2(0f, -50f);
            TitleScreenScript.CreateMainMenuDialog(size, pos);
        }

        if (dbr == DialogButtonResponse.COMMUNITY_OPEN_DISCORD)
        {
            Application.OpenURL("https://discord.gg/4q5kjUm");
            Vector2 size = new Vector2(400f, 5f);
            Vector2 pos = new Vector2(0f, -50f);
            TitleScreenScript.CreateMainMenuDialog(size, pos);
        }
if (PlatformVariables.LEADERBOARDS_ENABLED)
{
        if (dbr == DialogButtonResponse.WEEKLY_LEADERBOARD)
        {
            LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreDownloadSource = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
            ((LapinerTools.Steam.UI.SteamLeaderboardsPopup)uMyGUI_PopupManager.Instance.ShowPopup("steam_leaderboard")).LeaderboardUI.DownloadScoresAroundUser(ChallengesAndLeaderboards.GetWeeklyChallengeLeaderboard(true), 20);
        }
        else if (dbr == DialogButtonResponse.WEEKLY_LEADERBOARD_FRIENDS)
        {
            LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreDownloadSource = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends;
            ((LapinerTools.Steam.UI.SteamLeaderboardsPopup)uMyGUI_PopupManager.Instance.ShowPopup("steam_leaderboard")).LeaderboardUI.DownloadScores(ChallengesAndLeaderboards.GetWeeklyChallengeLeaderboard(true));
        }
        else if (dbr == DialogButtonResponse.DAILY_LEADERBOARD)
        {
            LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreDownloadSource = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
            ((LapinerTools.Steam.UI.SteamLeaderboardsPopup)uMyGUI_PopupManager.Instance.ShowPopup("steam_leaderboard")).LeaderboardUI.DownloadScoresAroundUser(ChallengesAndLeaderboards.GetDailyChallengeLeaderboard(true), 20);
        }
        else if (dbr == DialogButtonResponse.DAILY_LEADERBOARD_FRIENDS)
        {
            LapinerTools.Steam.SteamLeaderboardsMain.Instance.ScoreDownloadSource = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends;
            ((LapinerTools.Steam.UI.SteamLeaderboardsPopup)uMyGUI_PopupManager.Instance.ShowPopup("steam_leaderboard")).LeaderboardUI.DownloadScores(ChallengesAndLeaderboards.GetDailyChallengeLeaderboard(true));
        }
}
        if (dbr == DialogButtonResponse.EXIT)
        {
            Application.Quit();
        }
    }

    bool confirmInputThisFrame = false;

    public static void OnChangedUIFocus(UIManagerScript.UIObject obj)
    {
        if (!GameMasterScript.gmsSingleton.titleScreenGMS) return;
        if (CreateStage != CreationStages.SELECTSLOT) return;

        cursorIsOnChangePages = true;
    }

    public void OnSelectSlotPagesChanged()
    {
        //UIManagerScript.PlayCursorSound("UITock");
        for (int i = 0; i < UIManagerScript.saveDataDisplayComponents.Length; i++)
        {
            if (UIManagerScript.saveDataDisplayComponents[i].gameObject.activeSelf)
            {
                cursorIsOnChangePages = false;
                OnHoverPrefabSaveBlockSlot(i);                
                return;
            }
        }        
    }

    public void NavigateFromChangePagesToTop()
    {
        UIManagerScript.PlayCursorSound("Move");
        //Debug.Log("NavigateFromChangePagesToTop");
        OnSelectSlotPagesChanged();
    }

    public void NavigateFromChangePagesToBottom()
    {
        UIManagerScript.PlayCursorSound("Move");
        //Debug.Log("NavigateFromChangePagesToBottom");
        for (int i = UIManagerScript.saveDataDisplayComponents.Length-1; i >= 0; i--)
        {
            if (UIManagerScript.saveDataDisplayComponents[i].gameObject.activeSelf)
            {
                cursorIsOnChangePages = false;
                OnHoverPrefabSaveBlockSlot(i);
                return;
            }
        }
    }

    public static Canvas GetCanvas() 
    {
        return titleScreenSingleton.titleScreenCanvas;
    }
}
