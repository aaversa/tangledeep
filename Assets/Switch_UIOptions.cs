using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Switch_UIOptions : ImpactUI_Base
{
    [Header("Options Positioning")]
    [SerializeField]    private GameObject goAnchorObject;
    [SerializeField]    private int pxBetweenObjectsVertical;
    [SerializeField]    private int numRowsOnScreen;
    [SerializeField]    private float optionSlideTime = 0.05f;
    
    [Header("Prefabs")]
    [SerializeField]    private GameObject prefab_LabelObject;
    [SerializeField]    private GameObject prefab_OptionObject;
    [SerializeField]    private GameObject prefab_CommandObject;

    [Header("Other Objects")]
    [SerializeField]    private TextMeshProUGUI txt_helpInfo;
    [SerializeField]    private Scrollbar myScrollbar;


    private List<Switch_UIOptions_OptionObject> listOptionObjects;
    private int iSelectedIdx;

    //the range of option objects we can draw on screen
    private int iFirstVisibleIdx;
    private int iLastVisibleIdx;

    //used when scrolling around
    private int iPxBetweenObjectsOffset;

    //make this true when we are moving the option list around
    private bool bOptionsCurrentlySliding;

    //this value indicates moves we have to make after the current move,
    //for situations where we are scrolling the option list but also stepping on a label.
    //In that case we want to keep moving.
    private int iCachedMoveIndex;

    //if we are jumping way the hell across the list to a different option
    //such as when wrapping at top/bottom of screen
    //set this to true to skip all the fancy shmancy fading
    private bool bMegaWarpEngage;

    public override void Awake()
    {
        base.Awake();
        myTabType = UITabs.OPTIONS;
    }

    public override void Start ()
    {
        FontManager.LocalizeMe(txt_helpInfo, TDFonts.WHITE);
		base.Start();
    }

    /// <summary>
    /// Create the big list of options!
    /// </summary>
    /// <returns>False if already initialized</returns>
    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        listOptionObjects = new List<Switch_UIOptions_OptionObject>();

        /*
            Audio
	            Music
	            Sfx
	            Footsteps

            Display
	            Camera Distance
	            Battle Text Speed
	            Damage Text Size
	            Camera Scanlines
	            Grid Overlay
	            Player Health Bar
	            Monster Health Bars
	            Screen Flashes
                Minimap Style

            Gameplay
	            Analog Movement Style
	            Tutorial Tips
	            JP/XP Gain Popups
	            Extra Turn Popups
	            Show Item Info on Pickup
            XX	Auto Use Planks				<-- set to perma-on
	            Auto Equip Weapons
	            Auto Equip Best Offhand

            Interface
	            Cursor Repeat Delay
	            Log Text Size
	            Log Information
	            Text Speed


        ui_options_textspeed	Text Speed
Cursor Repeat Delay	Battle Text
ui_options_cursorrepeatdelay	Cursor Repeat Delay
misc_milliseconds_abbreviation	ms
misc_speed_0	Slow
misc_speed_1	Medium
misc_speed_2	Fast
misc_speed_3	Very Fast
misc_speed_instant	Instant

        ui_options_visual_scanlines	Camera Scanlines
ui_options_visual_gridoverlay	Grid Overlay

ui_options_gameplay_tutorial	Tutorial Popups
ui_options_gameplay_jpxp	JP/XP Gain Popups
ui_options_gameplay_extraturn	Extra Turn Popups
ui_options_gameplay_pickups	Show Item Info on Pickup
ui_options_gameplay_bestweapons	Auto Equip Weapons
ui_options_gameplay_bestoffhand	Auto Equip Best Offhand

        ui_options_gameplay_verbose	Verbose Combat Log
ui_options_gameplay_viewhelp	View Help
ui_options_gameplay_savequit	Save and Quit
ui_options_gameplay_saveback	Save & Back to Title
ui_options_battletextspeed	Battle Text Speed


         * 
         * 
         * 
         */

        //gameplay section
        CreateOption_LabelOnly("switch_ui_options_label_audio");
        CreateOption_PercentRange("switch_ui_options_music_volume", "switch_ui_options_info_music",
            i => { PlayerOptions.hundredBasedMusicVolume = i; return MusicManagerScript.singleton.SetMusicVolumeFromHundredBasedPlayerOptions(); }, PlayerOptions.hundredBasedMusicVolume);
        CreateOption_PercentRange("switch_ui_options_sfx_volume", "switch_ui_options_info_sfx",
            i => { PlayerOptions.hundredBasedSFXVolume = i; return MusicManagerScript.singleton.SetSFXVolumeFromHundredBasedPlayerOptions(true); }, PlayerOptions.hundredBasedSFXVolume, false);
        CreateOption_PercentRange("switch_ui_options_footsteps_volume", "switch_ui_options_info_footsteps",
            i => { PlayerOptions.hundredBasedFootstepsVolume = i; return MusicManagerScript.singleton.SetFootstepsVolumeFromHundredBasedPlayerOptions(true); }, PlayerOptions.hundredBasedFootstepsVolume, false);

        CreateOption_LabelOnly("switch_ui_options_label_display");

        //zoom scale is weird. Starts at 1 for most zoomed out, 2 is normal. No zero value.
        CreateOption_ValueList("switch_ui_options_camera_distance", "switch_ui_options_info_camera_distance",
            i => { PlayerOptions.zoomScale = i + 1;
                GameMasterScript.cameraScript.UpdateFOVFromOptionsValue();
                return null;
            }, Math.Max(PlayerOptions.zoomScale - 1, 0),
            "switch_ui_options_far", "switch_ui_options_normal");
        //Battle text speed, enums
        CreateOption_ValueList("ui_options_battletextspeed", "switch_ui_options_info_battletextspeed",
            i => { PlayerOptions.battleTextSpeed = i; UIManagerScript.singletonUIMS.UpdateTextSpeed(); return null; }, 1,
            "misc_speed_0", "switch_ui_options_normal", "misc_speed_2", "misc_speed_3");
        //Battle text scale, 50 - 100
        int value = 0;
        if (PlayerOptions.battleTextScale == 50) value = 0; if (PlayerOptions.battleTextScale == 100) value = 1; if (PlayerOptions.battleTextScale == 200) value = 2;
        CreateOption_ValueList("switch_ui_options_dmg_text_size", "switch_ui_options_info_dmg_text_size",
            i => { PlayerOptions.battleTextScale = (int)Mathf.Pow(2, i) * 50; return null; }, value,
            "switch_ui_options_small", "switch_ui_options_normal", "switch_ui_options_large");
        //Scanlines toggle
        CreateOption_Boolean("ui_options_visual_scanlines", "switch_ui_options_info_scanlines",
            i => { PlayerOptions.scanlines = i > 0; GameMasterScript.cameraScript.UpdateScanlinesFromOptionsValue(); return null; }, PlayerOptions.scanlines);
        //Grid overlay toggle
        CreateOption_Boolean("ui_options_visual_gridoverlay", "switch_ui_options_info_gridoverlay",
            i => { PlayerOptions.gridOverlay = i > 0; UIManagerScript.UpdateGridOverlayFromOptionsValue(); return null; }, PlayerOptions.gridOverlay);
        //Player Health bar toggle
        CreateOption_Boolean("ui_options_visual_playerhealth", "switch_ui_options_info_playerhealth",
            i => {PlayerOptions.playerHealthBar = i > 0; GameMasterScript.gmsSingleton.TogglePlayerHealthBar(); return null; }, PlayerOptions.playerHealthBar);
        //Monster Health bar toggle
        CreateOption_Boolean("ui_options_visual_monsterhealth", "switch_ui_options_info_monsterhealth",
            i => { PlayerOptions.monsterHealthBars = i > 0; GameMasterScript.gmsSingleton.ToggleMonsterHealthBars(); return null; }, PlayerOptions.monsterHealthBars);
        //Screenflashes toggle
        CreateOption_Boolean("ui_options_visual_screenflashes", "switch_ui_options_info_screenflashes",
            i => { return PlayerOptions.screenFlashes = i > 0; }, PlayerOptions.screenFlashes);

        //Minimap style toggle - new in 312019, popular request
        CreateOption_ValueList("ui_options_visuals_minimaptype", "switch_ui_options_info_minimaptype",
            i => { PlayerOptions.mapStyle = i; PlayerOptions.UpdateMinimapStyle(); return null; }, PlayerOptions.mapStyle,
            "option_minimap_cycle", "switch_ui_options_small", "switch_ui_options_large", "option_minimap_overlay");

        //Rumor overlay display
        CreateOption_Boolean("ui_options_rumor_display", "switch_ui_options_info_rumor_display",
            i => { return PlayerOptions.showRumorOverlay = i > 0; }, PlayerOptions.showRumorOverlay);


        CreateOption_LabelOnly("switch_ui_options_label_gameplay");

        //Create a quickie function here that returns a help string ID based on the state of joystick control style.
        //We call this in two places below.
        Func<int, string> funcGetAnalogHelpString = s => PlayerOptions.joystickControlStyle == JoystickControlStyles.STEP_MOVE
            ? "switch_ui_options_info_analog_step_move"
            : "switch_ui_options_info_analog_direct";

        //Confirm analog movement
        CreateOption_Boolean("switch_ui_options_confirm_analog_movement", funcGetAnalogHelpString(0),
            i =>
            {
                PlayerOptions.joystickControlStyle = (JoystickControlStyles)(i % (int)JoystickControlStyles.MAX);
                return funcGetAnalogHelpString(i);
            }, PlayerOptions.joystickControlStyle == JoystickControlStyles.STEP_MOVE);

        //Tutorial tips on/off
        CreateOption_Boolean("ui_options_gameplay_tutorial", "switch_ui_options_info_tutorial",
            i => { return PlayerOptions.tutorialTips = i > 0; }, PlayerOptions.tutorialTips);
        //Show JP/XP gains in world
        CreateOption_Boolean("ui_options_gameplay_jpxp", "switch_ui_options_info_jpxp",
            i => { return PlayerOptions.battleJPXPGain = i > 0; }, PlayerOptions.battleJPXPGain);
        //Show the "Extra Turn!" popup
        CreateOption_Boolean("ui_options_gameplay_extraturn", "switch_ui_options_info_extraturn",
            i => { return PlayerOptions.extraTurnPopup = i > 0; }, PlayerOptions.extraTurnPopup);
        //Show quick item name on pickup in world
        CreateOption_Boolean("ui_options_gameplay_pickups", "switch_ui_options_info_pickups",
            i => { return PlayerOptions.pickupDisplay = i > 0; }, PlayerOptions.pickupDisplay);
        //Auto equip best possible offhand when selecting a one-handed main hand weapon
        CreateOption_Boolean("ui_options_gameplay_bestweapons", "switch_ui_options_info_bestweapons",
            i => { return PlayerOptions.autoEquipWeapons = i > 0; }, PlayerOptions.autoEquipWeapons);
        //Automatically equip picked up weapons if your hands are empty unless you are a Budoka
        CreateOption_Boolean("ui_options_gameplay_bestoffhand", "switch_ui_options_info_bestoffhand",
            i => { return PlayerOptions.autoEquipBestOffhand = i > 0; }, PlayerOptions.autoEquipBestOffhand);

        // Eat cheap food automatically between battles
        CreateOption_Boolean("ui_options_autoeat_food", "switch_ui_options_info_autoeat_food_desc",
            i => { return PlayerOptions.autoEatFood = i > 0; }, PlayerOptions.autoEatFood);

        // Auto abandon rumors
        CreateOption_Boolean("ui_options_auto_abandon_rumors", "switch_ui_options_info_auto_abandon_rumors_desc",
            i => { return PlayerOptions.autoAbandonTrivialRumors = i > 0; }, PlayerOptions.autoAbandonTrivialRumors);

        CreateOption_LabelOnly("switch_ui_options_label_interface");


        //Cursor repeat delay, no longer a slider
        //50ms steps from 200 (slowest) to 50 (fastest)
        /*
        CreateOption_ValueList("ui_options_cursorrepeatdelay", "Can a dog?",
            i =>
            {
                PlayerOptions.cursorRepeatDelay = 200 - i * 50;
                return null;
            }, (200 - PlayerOptions.cursorRepeatDelay) / 50, "misc_speed_0", "misc_speed_1", "misc_speed_2", "misc_speed_3");
        */
        //Size of the text in the log
        CreateOption_ValueList("switch_ui_options_log_text_size", "switch_ui_options_info_log_text_size",
            i => { PlayerOptions.smallLogText = i == 0; GameLogScript.UpdateLogTextSize(); return null;}, PlayerOptions.smallLogText ? 0 : 1,
            "switch_ui_options_compact", "switch_ui_options_normal");

        //Create a quickie function here that returns a help string ID based on the state of verbose log text
        Func<int, string> funcGetVerboseLogHelpString = s => PlayerOptions.verboseCombatLog
            ? "switch_ui_options_info_log_verbose"
            : "switch_ui_options_info_log_normal";

        //How much info will we show in the log?
        CreateOption_ValueList("switch_ui_options_log_text_information", funcGetVerboseLogHelpString(0),
            i => { PlayerOptions.verboseCombatLog = i == 1; return funcGetVerboseLogHelpString(i); }, PlayerOptions.verboseCombatLog ? 1 : 0,
            "switch_ui_options_normal", "switch_ui_options_verbose");

        //How fast the text draws in dialog boxes.
        CreateOption_ValueList("ui_options_textspeed", "switch_ui_options_info_textspeed", 
            i => { PlayerOptions.textSpeed = i; return null; }, PlayerOptions.textSpeed,
            "misc_speed_0", "misc_speed_1", "misc_speed_2", "misc_speed_3", "misc_speed_instant");

        CreateOption_LabelOnly("switch_ui_options_label_data");

        CreateOption_Command("switch_ui_options_world_seed", "switch_ui_options_info_world_seed",
            i => { NullCommand(); return null; }, () => GameMasterScript.gmsSingleton.gameRandomSeed.ToString());

        CreateOption_Command("switch_ui_options_save_and_exit", "",
            i => { SaveAndQuitViaOptionsMenu(); return null; }, null);


        //line 'em all up
        PlaceOptionsInPositions();

        //move past the first label to the first available option
        iSelectedIdx = 0;
        AdjustSelectedIndex(1);

        bHasBeenInitialized = true;
        return true;
    }

    /// <summary>
    /// Instantiates an option in the boolean style and adds it to the list.
    /// </summary>
    /// <param name="stringID_OptionName"></param>
    /// <param name="stringID_HelpText"></param>
    /// <param name="funcOnChange"></param>
    /// <param name="bStartingValue"></param>
    /// <param name="playDefaultSound">If true, this option will tick/tock when changed</param>
    void CreateOption_Boolean(string stringID_OptionName, string stringID_HelpText, Func<int, object> funcOnChange, bool bStartingValue, bool playDefaultSound = true)
    {
        var oo = InstantiateOptionObject();
        oo.InitializeAsOptionInformation(stringID_OptionName, stringID_HelpText, funcOnChange, this, playDefaultSound);
        oo.SetStyleBoolean(bStartingValue);
    }

    /// <summary>
    /// Instantiates an option in the percent range style and adds it to the list.
    /// </summary>
    /// <param name="stringID_OptionName"></param>
    /// <param name="stringID_HelpText"></param>
    /// <param name="funcOnChange"></param>
    /// <param name="iStartingValue">0-100 in multiples of 10</param>
    /// <param name="playDefaultSound">If true, this option will tick/tock when changed</param>
    void CreateOption_PercentRange(string stringID_OptionName, string stringID_HelpText, Func<int, object> funcOnChange, int iStartingValue, bool playDefaultSound = true)
    {
        var oo = InstantiateOptionObject();
        oo.InitializeAsOptionInformation(stringID_OptionName, stringID_HelpText, funcOnChange, this, playDefaultSound);
        oo.SetStylePercentRange(iStartingValue);
    }

    /// <summary>
    /// Instantiates an option in the percent range style and adds it to the list.
    /// </summary>
    /// <param name="stringID_OptionName"></param>
    /// <param name="stringID_HelpText"></param>
    /// <param name="funcOnChange"></param>
    /// <param name="iStartingValue">index in the list to set as current value</param>
    /// <param name="stringID_Values">List of string IDs to use as values such as "near", "far" or "big", "realbig", "reaaallll big"</param>
    void CreateOption_ValueList(string stringID_OptionName, string stringID_HelpText, Func<int, object> funcOnChange, int iStartingValue, params string[] stringID_Values)
    {
        var oo = InstantiateOptionObject();
        oo.InitializeAsOptionInformation(stringID_OptionName, stringID_HelpText, funcOnChange, this);
        oo.SetStyleValueList(iStartingValue, stringID_Values);
    }

    /// <summary>
    /// Create a new OptionObject and add it to the list. It is empty.
    /// If you are adding a new option to the list, call one of the Create functions instead.
    /// </summary>
    /// <returns>An empty OptionObject</returns>
    Switch_UIOptions_OptionObject InstantiateOptionObject()
    {
        var go = Instantiate(prefab_OptionObject);
        var oo = go.GetComponent<Switch_UIOptions_OptionObject>();
        listOptionObjects.Add(oo);
        oo.IndexInParent = listOptionObjects.Count - 1;
        go.transform.localScale = Vector3.one;
        return oo;
    }

    /// <summary>
    /// Create an OptionObject, set it to label-only status, and assign the string ID.
    /// </summary>
    /// <param name="stringID_LabelText"></param>
    void CreateOption_LabelOnly(string stringID_LabelText)
    {
        //This is not copy pasta! The labelobject and optionobject prefabs are
        //the same component, but the labelobject has a different size text box.
        var go = Instantiate(prefab_LabelObject);

        var oo = go.GetComponent<Switch_UIOptions_OptionObject>();
        listOptionObjects.Add(oo);
        oo.IndexInParent = listOptionObjects.Count - 1;
        oo.InitializeAsLabelOnly(stringID_LabelText);
    }

    /// <summary>
    /// Create an OptionObject, assign to be a command with a function called when selected and confirm is pressed.
    /// </summary>
    /// <param name="stringID_LabelText"></param>
    /// <param name="stringID_Helptext"></param>
    /// <param name="funcOnExecute"></param>
    /// <param name="funcGetOptionalLabelText">Use this if you'd like to add some text to the right column, even though it won't be mutable.</param>
    void CreateOption_Command(string stringID_LabelText, string stringID_Helptext, Func<int, object> funcOnExecute, Func<string> funcGetOptionalLabelText)
    {
        var go = Instantiate(prefab_CommandObject);
        var oo = go.GetComponent<Switch_UIOptions_OptionObject>();
        listOptionObjects.Add(oo);
        oo.IndexInParent = listOptionObjects.Count - 1;
        oo.InitializeAsCommandObject(stringID_LabelText, stringID_Helptext, funcOnExecute, funcGetOptionalLabelText);
    }


    /// <summary>
    /// Align the options according to their index offset by the first visible option index.
    /// </summary>
    /// <param name="bFadeInstantly">Don't use the fade time and just fade immediately. Used when opening the UI</param>
    void PlaceOptionsInPositions(bool bFadeInstantly = false)
    {
        //Create that vertical list!
        for (int idx = 0; idx < listOptionObjects.Count; idx++)
        {
            var oo = listOptionObjects[idx];
            oo.transform.SetParent(goAnchorObject.transform);
            oo.transform.localScale = Vector3.one;

            //note the offset here to cover sliding around
            oo.transform.localPosition = new Vector3(0, iPxBetweenObjectsOffset + pxBetweenObjectsVertical * (idx - iFirstVisibleIdx) * -1, 0);

            //update the draw/nodraw states of the options
            if (idx >= iFirstVisibleIdx && idx <= iLastVisibleIdx)
            {
                listOptionObjects[idx].FadeIn(bFadeInstantly ? 0.01f : optionSlideTime);
            }
            else
            {
                listOptionObjects[idx].FadeOut(bFadeInstantly ? 0.01f : optionSlideTime);
            }
        }
    }

    /// <summary>
    /// Called by a child option object when it is changed via touch or click.
    /// Set our current object to be this one, and update accordingly.
    /// </summary>
    /// <param name="obj"></param>
    public void OnOptionObjectSelectedViaTouch(Switch_UIOptions_OptionObject obj, Image imageClicked)
    {
        iSelectedIdx = obj.IndexInParent;

        //flash and bounce a bit
        Vector3 vNewScale = new Vector3(1.2f, 1.2f, 1.2f);
        imageClicked.rectTransform.localScale = vNewScale;
        LeanTween.scale(imageClicked.rectTransform, Vector3.one, 0.15f).setEaseInOutBack();

        imageClicked.color = Color.yellow;
        LeanTween.color(imageClicked.rectTransform, Color.white, 0.15f).setEaseInOutBack();

        //update our content list
        UpdateContent();
    }

    bool CheckIndexAndScrollBoardIfNecessary()
    {
        int iOldFirst = iFirstVisibleIdx;

        //if we are pointing at the bottom or top, cycle the visible objects
        //so that we scroll smoothly
        if (iSelectedIdx <= iFirstVisibleIdx + 1)
        {
            //reduce first visible just a wee bit, scrolling just before we hit the top of the screen
            iFirstVisibleIdx = Math.Max(iSelectedIdx - 2, 0);
            iLastVisibleIdx = Math.Min(iFirstVisibleIdx + numRowsOnScreen - 1, listOptionObjects.Count - 1);
        }

        else if (iSelectedIdx >= iLastVisibleIdx)
        {
            //advance first visible enough to keep up
            iFirstVisibleIdx = Math.Max(iSelectedIdx - numRowsOnScreen + 1, 0);
            iLastVisibleIdx = Math.Min(iFirstVisibleIdx + numRowsOnScreen - 1, listOptionObjects.Count - 1);
        }

        return iOldFirst != iFirstVisibleIdx;
    }


    public override void UpdateContent(bool adjustSorting = true)
    {
        base.UpdateContent();

        //we will need this later.
        int iOldFirst = iFirstVisibleIdx;

        //Did we move?
        if (CheckIndexAndScrollBoardIfNecessary())
        {
            if (bMegaWarpEngage)
            {
                PlaceOptionsInPositions(true);
                bMegaWarpEngage = false;
            }
            else
            {
                //adjust by some small amount
                int iDelta = iOldFirst - iFirstVisibleIdx;
                iPxBetweenObjectsOffset = pxBetweenObjectsVertical * iDelta;

                //maybe we changed the cursor delay?
                optionSlideTime = 0.05f; //PlayerOptions.cursorRepeatDelay / 1000f; <-- let's get back to this. Eventually.
                
                //then make a coroutine that updates this amount.
                StartCoroutine(SlideOptions(optionSlideTime, iPxBetweenObjectsOffset));
            }
        }

        //move options where they should be
        PlaceOptionsInPositions();

        //grab the current object and use its value for the help window
        var oo = listOptionObjects[iSelectedIdx];

        //if this object is a label, we'll only be looking at it if we're scrolling the list
        //so just play it cool for now.
        if (!oo.IsLabel())
        {
            txt_helpInfo.text = oo.GetLocalizedHelpText();

            //align the cursor, the object will take care of positioning
            oo.AttachCursorToMe();
        }
        else
        {
            //hide while we point at labels
            UIManagerScript.HideDialogMenuCursor();
        }


    }

    IEnumerator SlideOptions(float fSlideTime, int iStartValue)
    {
        bOptionsCurrentlySliding = true;
        float fTime = 0f;
        while (fTime < fSlideTime)
        {
            iPxBetweenObjectsOffset = (int)Mathf.Lerp(iStartValue, 0f, fTime / fSlideTime);
            fTime += Time.deltaTime;
            yield return null;
        }

        //turn off the machine, but make sure we have them in place.
        iPxBetweenObjectsOffset = 0;
        PlaceOptionsInPositions();
        bOptionsCurrentlySliding = false;

        //if we have to do this again, then do it again.
        if (iCachedMoveIndex != 0)
        {
            //clear the cache
            int i = iCachedMoveIndex;
            iCachedMoveIndex = 0;

            //make the move -- it might change the cache
            AdjustSelectedIndex(i);
        }
    }

    public override void Update()
    {
        //If we are sliding options around, keep positioning them
        if (bOptionsCurrentlySliding)
        {
            PlaceOptionsInPositions();
        }
    }

    public override void TurnOn()
    {
        base.TurnOn();

        //make sure we don't cling to this from last time if we closed during a move.
        bOptionsCurrentlySliding = false;

        //rewind to the top
        ResetAllOptionPositions();

        //don't use the shadow object current focus here
        UIManagerScript.uiObjectFocus = null;

    }

    /// <summary>
    /// Reset the option board position and fade values to their default.
    /// This doesn't change any options, rather just moves the scroll to the top.
    /// </summary>
    void ResetAllOptionPositions()
    {
        //point at the first option beneath the label
        iSelectedIdx = 1;

        //make all the options hidden for this first frame so they don't poppalop in all over the place
        //and put them right where they belong
        iPxBetweenObjectsOffset = 0;
        iFirstVisibleIdx = 0;
        iLastVisibleIdx = Math.Min(iFirstVisibleIdx + numRowsOnScreen - 1, listOptionObjects.Count - 1);
        UpdateContent();
        //PlaceOptionsInPositions(true);

        //everything moves into place now
        StartCoroutine(SlideOptions(0f, 0));
    }

    public override bool TryTurnOff()
    {
        //We don't have any special states we can't leave immediately
        return true;
    }

    public override void TurnOff()
    {
        base.TurnOff();
        //PlayerOptions.WriteOptionsToFile();
    }

    public override void OnDialogClose()
    {
        //Do nothing here
    }

    public override UIManagerScript.UIObject GetDefaultUiObjectForFocus()
    {
        return null;
    }

    /// <summary>
    /// Moves the dialog cursor up and down, also changes values on left/right pushes.
    /// Checking for button presses or swipes happens via UI Callbacks
    /// </summary>
    /// <param name="dInput">Direction recieved from the controller</param>
    /// <returns>True if input was handled and no further handling needs to happen.</returns>
    public override bool HandleInput(Directions dInput)
    {
        if (base.HandleInput(dInput))
        {
            return true;
        }

        var oo = listOptionObjects[iSelectedIdx];

        //if sliding options, don't take any input in
        if (bOptionsCurrentlySliding)
        {
            return true;
        }

        var rewiredPlayer = GameMasterScript.gmsSingleton.player;

        //Confirm should execute commands, but not 
        if (rewiredPlayer.GetButtonDown("Confirm"))
        {
            if (oo.IsCommand())
            {
                oo.OnConfirm();
            }
            return true;
        }

        //directions
        //if no direction is pressed, pass the input on to
        //the normal handler
        // up/down to change which option is selected
        // left/right to change selected option value
        switch (dInput)
        {
            case Directions.NORTH:
                AdjustSelectedIndex(-1);
                break;
            case Directions.SOUTH:
                AdjustSelectedIndex(1);
                break;
            case Directions.EAST:
                oo.OnClickChangeValue(1);
                UpdateContent();
                break;
            case Directions.WEST:
                oo.OnClickChangeValue(-1);
                UpdateContent();
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Moves the index up and down based on delta. Will skip over objects that 
    /// are only labels, and make sure to clamp the value accordingly.
    /// If the index moves past given visible boundaries, the options will scroll.
    /// </summary>
    /// <param name="iDelta"></param>
    void AdjustSelectedIndex(int iDelta)
    {
        // 312019 - This function can fail. Something with List_1_setcapacity.

        int iOldIdx = iSelectedIdx;

        if (!bHasBeenInitialized)
        {
            return;
        }

        //if our next step places us on to a label, we want to skip the label, unlessss we are scrolling the list
        //with this move, in which case we want to move twice consecutively instead of simply jumping the label.
        //
        //The numbers behind the scenes will stay the same, but the calls to the motion coroutines will have 
        //a flag set indicating that more moves need to be made after the first. 

        int iPotentialIndex = iSelectedIdx + iDelta;

        //If we are moving UP, and 
        //  the index will be 0 and
        //  the object is a label
        //  don't move at all.
        if (iPotentialIndex <= 0 &&
            listOptionObjects[0].IsLabel())
        {
            //sure why not
            iPotentialIndex = listOptionObjects.Count - 1;
            bMegaWarpEngage = true;
            //play a bump/tock/fail sound
            //UIManagerScript.PlayCursorSound("Tick");
            //return;
        }

        //This assumes there's no label at the bottom. Please don't make the last entry a label
        if (iPotentialIndex >= listOptionObjects.Count )
        {
            //oooor instead jump to the first option that isn't a label
            //iPotentialIndex = TotallyCleanFunctionThatFindsFirstIndexThatIsntALabel();
            iPotentialIndex = 1;
            bMegaWarpEngage = true;

            //play a bump/tock/fail sound
            //UIManagerScript.PlayCursorSound("Tick");
            //return;
        }

        //If our move would take us out of bounds, don't do it.
        if (iPotentialIndex < 0 || iPotentialIndex >= listOptionObjects.Count)
        {
            //play a bump/tock/fail sound
            UIManagerScript.PlayCursorSound("Tick");
            return;
        }

        //we are moving
        iSelectedIdx = iPotentialIndex;

        //if none of the conditions above are met, and our next object is a label, 
        //our next step depends on if we are scrolling the board or not.
        if (listOptionObjects[iPotentialIndex].IsLabel())
        {
            //if we are gonna scroll the board...
            if (iSelectedIdx <= iFirstVisibleIdx + 1 ||
                iSelectedIdx >= iLastVisibleIdx)
            {
                //don't push past the label, but instead tell the system
                //that we need to move again after the move coroutine is done.
                iCachedMoveIndex += iDelta;

                //play a good/move sound
                UIManagerScript.PlayCursorSound("Move");

                //update our scrolling menu
                UpdateContent();
                return;
            }

            //otherwise, take one more step over the label.
            iSelectedIdx += iDelta;

            //the game will then scroll for us. 
        }

        //How would it get out of bounds? If the initial value before the loop was out of bounds.
        iSelectedIdx = Mathf.Clamp(iSelectedIdx, 0, listOptionObjects.Count - 1);

        //if we changed our index, play a sound and update content.
        if (iSelectedIdx != iOldIdx)
        {
            //beep boop
            UIManagerScript.PlayCursorSound("Move");

            UpdateContent();
            return;
        }

        //play a bump/tock/fail sound
        UIManagerScript.PlayCursorSound("Tick");

        //and do not update content because we did not move.
    }

    //used on android to change to specific index 420test
    public void SelectIndexWithSpecificID(int iPotentialIndex)
    {
        int iOldIdx = iSelectedIdx;

        if (!bHasBeenInitialized)
        {
            return;
        }

        //If our move would take us out of bounds, don't do it.
        if (iPotentialIndex < 0 || iPotentialIndex >= listOptionObjects.Count)
        {
            //play a bump/tock/fail sound
            UIManagerScript.PlayCursorSound("Tick");
            return;
        }

        //we are moving
        iSelectedIdx = iPotentialIndex;

        //if we changed our index, play a sound and update content.
        if (iSelectedIdx != iOldIdx)
        {
            //beep boop
            UIManagerScript.PlayCursorSound("Move");

            UpdateContent();
            return;
        }

        //play a bump/tock/fail sound
        UIManagerScript.PlayCursorSound("Tick");

        //and do not update content because we did not move.
    }

    #region Editor UI Callbacks

    public void OnSwipe(Vector2 vSwipeDir)
    {
        //if we are currently moving, ignore the swipe
        if (!bOptionsCurrentlySliding)
        {
            if (vSwipeDir.y > 1)
            {
                AdjustSelectedIndex(1);
            }
            else if (vSwipeDir.y < -1)
            {
                AdjustSelectedIndex(-1);
            }
        }
    }
    
    #endregion

    #region Unused Interface Calls

    public override Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj)
    {
        return null;
    }


    public override void StartAssignObjectToHotbarMode(ISelectableUIObject content)
    {
    }

    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
    }

    public override void TryShowSelectableObject(ISelectableUIObject obj)
    {
    }

    #endregion

    #region Callbacks to adjust values

    public void ToggleAutoEquipWeapons()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoEquipWeapons = !PlayerOptions.autoEquipWeapons;
    }

    public void ToggleGridOverlay()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.gridOverlay = !PlayerOptions.gridOverlay;
        UIManagerScript.UpdateGridOverlayFromOptionsValue();
    }

    public void TogglePopups()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.tutorialTips = !PlayerOptions.tutorialTips;
    }

    public void ToggleLogVerbosity()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.verboseCombatLog = !PlayerOptions.verboseCombatLog;
    }

    public void ToggleBattleJPXPGain()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.battleJPXPGain = !PlayerOptions.battleJPXPGain;
    }

    public void ToggleAutoPickup()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoPickupItems = !PlayerOptions.autoPickupItems;
    }

    public void TogglePlayerHealthBar()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.playerHealthBar = !PlayerOptions.playerHealthBar;
        GameMasterScript.gmsSingleton.TogglePlayerHealthBar();
    }

    public void ToggleControllerPrompts()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.showControllerPrompts = !PlayerOptions.showControllerPrompts;
    }

    public void ToggleScreenFlashes()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.screenFlashes = !PlayerOptions.screenFlashes;
    }

    public void ToggleSmallerLogText()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.smallLogText = !PlayerOptions.smallLogText;
        GameLogScript.UpdateLogTextSize();
    }

    public void ToggleAutoPlanksInItemWorld()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoPlanksInItemWorld = !PlayerOptions.autoPlanksInItemWorld;
    }

    public void ToggleAutoEquipBestOffhand()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoEquipBestOffhand = !PlayerOptions.autoEquipBestOffhand;
    }

    public void ToggleMonsterHealthBars()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.monsterHealthBars = !PlayerOptions.monsterHealthBars;
        GameMasterScript.gmsSingleton.ToggleMonsterHealthBars();
    }

    public void TogglePickupDisplay()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.pickupDisplay = !PlayerOptions.pickupDisplay;
    }

    public void ToggleExtraTurnPopup()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.extraTurnPopup = !PlayerOptions.extraTurnPopup;
    }

    public void ToggleScanlinesFromMouse()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.scanlines = !PlayerOptions.scanlines;
        if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            GameMasterScript.cameraScript.UpdateScanlinesFromOptionsValue();
        }
    }

    /// <summary>
    /// Fade the screen out while running away and quitting
    /// </summary>
    public void SaveAndQuitViaOptionsMenu()
    {
        UIManagerScript.PlayCursorSound("OPSelect");
        UIManagerScript.FadeOut(1.25f);
        GameMasterScript.StartWatchedCoroutine(WaitThenSaveAndQuitToTitle(1.26f));
    }

    /// <summary>
    /// Do nothing when activated. This is weird code but makes for cleaner reading of the option implementation.
    /// </summary>
    public void NullCommand()
    {

    }

    /// <summary>
    /// This call has some weird but intentional yields, designed to let the now-loading icon animate a little in
    /// order to match Nintendo's lotcheck requirements.
    /// </summary>
    /// <param name="fWaitTime"></param>
    /// <returns></returns>
    IEnumerator WaitThenSaveAndQuitToTitle(float fWaitTime)
    {
        var fadeTime = fWaitTime * 0.8f;
        MusicManagerScript.singleton.Fadeout(fadeTime);
        
        //wait the entire fade out time,the music fades out just a little quicker
        yield return new WaitForSeconds(fWaitTime);
        

#if UNITY_SWITCH        
        //this call happens anyway when the scene is destroyed,
        //but by doing it here we might avoid the heap spike that is plaguing us while switching scenes.
        MapMasterScript.ReleaseAllTextures();
        GC.Collect();

        Switch_SaveIconDisplay.Show();
        
        //Wait two frames for the 0-delay Display to fire up and show up on screen.
        yield return null;
        yield return null;
        
        //Don't yield between this call and the next to prevent save errors
        Switch_SaveDataHandler.FlushBytesLoadedAsync();
#endif

        //save the game        
        LoadingWaiterManager.Display();
        yield return GameMasterScript.gmsSingleton.ISaveTheGame(autoSave: false);
        //let the now-saving animate a touch
        yield return new WaitForSeconds(1.0f);
        //Switch_SaveIconDisplay.Hide();

        GameMasterScript.ResetAllVariablesToGameLoad();

        //finish up.
        GameMasterScript.LoadMainScene();
    }

    #endregion

}
