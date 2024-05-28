using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public partial class UIManagerScript
{



    public static void UpdateGridOverlayFromOptionsValue()
    {
        /* if (gridOverlayGO == null)
        {
            gridOverlayGO = GameObject.Find("GridPrefabInstance");
        } */

        if (!PlayerOptions.gridOverlay)
        {
            MapMasterScript.singletonMMS.mapGridMesh.gameObject.SetActive(false);
            //gridOverlayGO.SetActive(false);
        }
        else
        {
            MapMasterScript.singletonMMS.mapGridMesh.gameObject.SetActive(true);
            //gridOverlayGO.SetActive(true);
        }
    }

    public void RefocusOptionsSlider(OptionsSlider whichSlider)
    {
        DeselectOptionsSlider(0);
        highlightingOptionsObject = false;
        SelectOptionsSlider((int)whichSlider);
    }

    public void SelectOptionsSlider(int value)
    {
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            if ((int)optionsSliderSelected != value)
            {
                PlayCursorSound("Select");
            }
        }
        else
        {
            return;
        }

        //Debug.Log("Selected options slider " + value + ". Highlight state: " + highlightingOptionsObject);

        if (highlightingOptionsObject && value > -1)
        {
            if (value == (int)OptionsSlider.RESOLUTION)
            {
                Resolution selection = singletonUIMS.availableDisplayResolutions[(int)optionsResolution.gameObj.GetComponent<Slider>().value];
                //Debug.Log("SET player resolution via SelectOptionsSlider: " + selection.width + "," + selection.height);
                PlayerOptions.resolutionX = selection.width;
                PlayerOptions.resolutionY = selection.height;

                SetResolutionFromOptions();
            }

            DeselectOptionsSlider(0);
            highlightingOptionsObject = false;
            return;
        }

        foreach (GameObject go in uiObjectsWithHighlights)
        {
            go.GetComponent<Image>().color = transparentColor;
        }
        highlightingOptionsObject = false;
        movingSliderViaKeyboard = true;
        switch (value)
        {
            case -1:
                // Nothing.     
                //Debug.Log("Do nothing.");
                movingSliderViaKeyboard = false;
                return;
            case (int)OptionsSlider.MUSICVOLUME:
                highlightedOptionsObject = optionsMusicVolumeContainer;
                break;
            case (int)OptionsSlider.SFXVOLUME:
                highlightedOptionsObject = optionsSFXVolumeContainer;
                break;
            case (int)OptionsSlider.CURSORREPEATDEALY:
                highlightedOptionsObject = optionsCursorRepeatDelayContainer;
                break;
            case (int)OptionsSlider.FRAMECAP:
                highlightedOptionsObject = optionsFrameCapContainer;
                break;
            case (int)OptionsSlider.TEXTSPEED:
                highlightedOptionsObject = optionsTextSpeedContainer;
                break;
            case (int)OptionsSlider.BATTLETEXTSPEED:
                highlightedOptionsObject = optionsBattleTextSpeedContainer;
                break;
            case (int)OptionsSlider.BATTLETEXTSCALE:
                highlightedOptionsObject = optionsBattleTextScaleContainer;
                break;
            case (int)OptionsSlider.FOOTSTEPSVOLUME:
                highlightedOptionsObject = optionsFootstepsVolumeContainer;
                break;
            case (int)OptionsSlider.BUTTONDEADZONE:
                highlightedOptionsObject = optionsButtonDeadZoneContainer;
                break;
            case (int)OptionsSlider.ZOOMSCALE:
                highlightedOptionsObject = optionsZoomScaleContainer;
                break;
            case (int)OptionsSlider.RESOLUTION:
                highlightedOptionsObject = optionsResolutionContainer;
                break;
        }
        optionsSliderSelected = (OptionsSlider)value;
        highlightedOptionsObject.GetComponent<Image>().color = Color.white;
        highlightingOptionsObject = true;
        UpdateOptionsSlidersDirectionalActions();
        //Debug.Log("At the bottom, " + highlightedOptionsObject.gameObject.name);
    }

    public void DeselectOptionsSlider(int dummy)
    {
        highlightingOptionsObject = false;
        if (highlightedOptionsObject == null) return;
        highlightedOptionsObject.GetComponent<Image>().color = transparentColor;
        UpdateOptionsSlidersDirectionalActions();
    }

    public static void UpdateOptionsSlidersDirectionalActions()
    {
        for (int i = 0; i < allOptionsSliders.Count; i++)
        {
            allOptionsSliders[i].directionalValues[(int)Directions.NORTH] = (int)Directions.NORTH;
            allOptionsSliders[i].directionalValues[(int)Directions.EAST] = (int)Directions.EAST;
            allOptionsSliders[i].directionalValues[(int)Directions.WEST] = (int)Directions.WEST;
            allOptionsSliders[i].directionalValues[(int)Directions.SOUTH] = (int)Directions.SOUTH;
            allOptionsSliders[i].directionalActions[(int)Directions.EAST] = allOptionsSliders[i].MoveCursorToNeighbor;
            allOptionsSliders[i].directionalActions[(int)Directions.WEST] = allOptionsSliders[i].MoveCursorToNeighbor;
            allOptionsSliders[i].directionalActions[(int)Directions.NORTH] = allOptionsSliders[i].MoveCursorToNeighbor;
            allOptionsSliders[i].directionalActions[(int)Directions.SOUTH] = allOptionsSliders[i].MoveCursorToNeighbor;

        }

        if (highlightingOptionsObject)
        {
            switch (optionsSliderSelected)
            {
                case OptionsSlider.RESOLUTION:
                    optionsResolution.directionalActions[(int)Directions.EAST] = optionsResolution.ChangeSliderValue;
                    optionsResolution.directionalActions[(int)Directions.WEST] = optionsResolution.ChangeSliderValue;
                    optionsResolution.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsResolution.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsResolution.directionalValues[(int)Directions.EAST] = 1;
                    optionsResolution.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.FRAMECAP:
                    optionsFramecap.directionalActions[(int)Directions.EAST] = optionsFramecap.ChangeSliderValue;
                    optionsFramecap.directionalActions[(int)Directions.WEST] = optionsFramecap.ChangeSliderValue;
                    optionsFramecap.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsFramecap.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsFramecap.directionalValues[(int)Directions.EAST] = 1;
                    optionsFramecap.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.TEXTSPEED:
                    optionsTextSpeed.directionalActions[(int)Directions.EAST] = optionsTextSpeed.ChangeSliderValue;
                    optionsTextSpeed.directionalActions[(int)Directions.WEST] = optionsTextSpeed.ChangeSliderValue;
                    optionsTextSpeed.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsTextSpeed.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsTextSpeed.directionalValues[(int)Directions.EAST] = 1;
                    optionsTextSpeed.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.BATTLETEXTSPEED:
                    optionsBattleTextSpeed.directionalActions[(int)Directions.EAST] = optionsTextSpeed.ChangeSliderValue;
                    optionsBattleTextSpeed.directionalActions[(int)Directions.WEST] = optionsTextSpeed.ChangeSliderValue;
                    optionsBattleTextSpeed.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsBattleTextSpeed.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsBattleTextSpeed.directionalValues[(int)Directions.EAST] = 1;
                    optionsBattleTextSpeed.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.BATTLETEXTSCALE:
                    optionsBattleTextScale.directionalActions[(int)Directions.EAST] = optionsBattleTextScale.ChangeSliderValue;
                    optionsBattleTextScale.directionalActions[(int)Directions.WEST] = optionsBattleTextScale.ChangeSliderValue;
                    optionsBattleTextScale.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsBattleTextScale.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsBattleTextScale.directionalValues[(int)Directions.EAST] = 1;
                    optionsBattleTextScale.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.ZOOMSCALE:
                    optionsZoomScale.directionalActions[(int)Directions.EAST] = optionsZoomScale.ChangeSliderValue;
                    optionsZoomScale.directionalActions[(int)Directions.WEST] = optionsZoomScale.ChangeSliderValue;
                    optionsZoomScale.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsZoomScale.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsZoomScale.directionalValues[(int)Directions.EAST] = 1;
                    optionsZoomScale.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.BUTTONDEADZONE:
                    optionsButtonDeadZone.directionalActions[(int)Directions.EAST] = optionsButtonDeadZone.ChangeSliderValue;
                    optionsButtonDeadZone.directionalActions[(int)Directions.WEST] = optionsButtonDeadZone.ChangeSliderValue;
                    optionsButtonDeadZone.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsButtonDeadZone.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsButtonDeadZone.directionalValues[(int)Directions.EAST] = 1;
                    optionsButtonDeadZone.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.CURSORREPEATDEALY:
                    optionsCursorRepeatDelay.directionalActions[(int)Directions.EAST] = optionsCursorRepeatDelay.ChangeSliderValue;
                    optionsCursorRepeatDelay.directionalActions[(int)Directions.WEST] = optionsCursorRepeatDelay.ChangeSliderValue;
                    optionsCursorRepeatDelay.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsCursorRepeatDelay.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsCursorRepeatDelay.directionalValues[(int)Directions.EAST] = 1;
                    optionsCursorRepeatDelay.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.MUSICVOLUME:
                    optionsMusicVolume.directionalActions[(int)Directions.EAST] = optionsMusicVolume.ChangeSliderValue;
                    optionsMusicVolume.directionalActions[(int)Directions.WEST] = optionsMusicVolume.ChangeSliderValue;
                    optionsMusicVolume.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsMusicVolume.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsMusicVolume.directionalValues[(int)Directions.EAST] = 1;
                    optionsMusicVolume.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.SFXVOLUME:
                    optionsSFXVolume.directionalActions[(int)Directions.EAST] = optionsSFXVolume.ChangeSliderValue;
                    optionsSFXVolume.directionalActions[(int)Directions.WEST] = optionsSFXVolume.ChangeSliderValue;
                    optionsSFXVolume.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsSFXVolume.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsSFXVolume.directionalValues[(int)Directions.EAST] = 1;
                    optionsSFXVolume.directionalValues[(int)Directions.WEST] = -1;
                    break;
                case OptionsSlider.FOOTSTEPSVOLUME:
                    optionsFootstepsVolume.directionalActions[(int)Directions.EAST] = optionsFootstepsVolume.ChangeSliderValue;
                    optionsFootstepsVolume.directionalActions[(int)Directions.WEST] = optionsFootstepsVolume.ChangeSliderValue;
                    optionsFootstepsVolume.directionalActions[(int)Directions.NORTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsFootstepsVolume.directionalActions[(int)Directions.SOUTH] = singletonUIMS.DeselectOptionsSlider;
                    optionsFootstepsVolume.directionalValues[(int)Directions.EAST] = 1;
                    optionsFootstepsVolume.directionalValues[(int)Directions.WEST] = -1;
                    break;
            }
        }

    }

    // This is called only by the mouse up event. 
    public void ChangeResWithoutExecuting()
    {
        if (!LogoSceneScript.AllowResolutionSelection())
        {
            return;
        }
        if (optionsResolution == null || optionsResolution.gameObj == null)
        {
            return;
        }
        if (singletonUIMS == null || singletonUIMS.availableDisplayResolutions == null)
        {
            return;
        }
        if (singletonUIMS.availableDisplayResolutions.Count == 0)
        {
            return;
        }
        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            int index = 0;
            for (int i = 0; i < singletonUIMS.availableDisplayResolutions.Count; i++)
            {
                if (PlayerOptions.resolutionX == singletonUIMS.availableDisplayResolutions[i].width
                    && PlayerOptions.resolutionY == singletonUIMS.availableDisplayResolutions[i].height)
                {
                    index = i;
                    break;
                }
            }
            optionsResolution.gameObj.GetComponent<Slider>().value = index;
            return;
        }

        //UIManagerScript.singletonUIMS.RefocusOptionsSlider(OptionsSlider.RESOLUTION);

        int value = (int)optionsResolution.gameObj.GetComponent<Slider>().value;
        if (value >= singletonUIMS.availableDisplayResolutions.Count)
        {
            value = 0;
        }
        if (!GameMasterScript.actualGameStarted) return;
        Resolution selection = singletonUIMS.availableDisplayResolutions[(int)optionsResolution.gameObj.GetComponent<Slider>().value];
        //Debug.Log("Set player resolution (no actual change) via ChangeResWithoutExecuting: " + selection.width + "," + selection.height);
        PlayerOptions.resolutionX = selection.width;
        PlayerOptions.resolutionY = selection.height;
        UpdateResolutionText();
    }

    public void SetResolutionFromOptions()
    {
        for (int i = 0; i < availableDisplayResolutions.Count; i++)
        {
            if (PlayerOptions.resolutionX == availableDisplayResolutions[i].width && PlayerOptions.resolutionY == availableDisplayResolutions[i].height)
            {

                FullScreenMode fsm = FullScreenMode.Windowed;
                if (LogoSceneScript.globalSolsticeDebug && (PlayerOptions.fullscreen || Screen.fullScreen))
                {
                    fsm = FullScreenMode.ExclusiveFullScreen;
                }

                //Debug.Log("Setting resolution from options: " + PlayerOptions.resolutionX + "," + PlayerOptions.resolutionY + " " + PlayerOptions.fullscreen);
                if (GameMasterScript.gameLoadSequenceCompleted)
                {
                    if (!LogoSceneScript.globalSolsticeDebug)
                    {
                        fsm = PlayerOptions.fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
                    }
                    Screen.SetResolution(PlayerOptions.resolutionX, PlayerOptions.resolutionY, fsm);
                    GameMasterScript.cameraScript.UpdateScanlinesFromOptionsValue();
                }
                else
                {
                    if (!LogoSceneScript.globalSolsticeDebug)
                    {
                        fsm = Screen.fullScreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
                    }
                    Screen.SetResolution(PlayerOptions.resolutionX, PlayerOptions.resolutionY, fsm);
                    GameMasterScript.cameraScript.UpdateScanlinesFromOptionsValue();
                }

                FontManager.OnResolutionChanged(); // Always run this when resolution has changed, which right now is only here.
                GameplayResolutionChangeHandler.OnResolutionChanged();
                CameraController.UpdateTileRanges();
                break;
            }
        }
    }

    public void GetAllDisplayResolutions()
    {
        availableDisplayResolutions.Clear();
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            float idealRatio = 16f / 9f;
            float checkRatio = (float)Screen.resolutions[i].width / (float)Screen.resolutions[i].height;
            if (Mathf.Abs(checkRatio - idealRatio) > 0.02f) continue;

            bool skip = false;

            foreach (Resolution r in availableDisplayResolutions)
            {
                if ((r.width == Screen.resolutions[i].width) && (r.height == Screen.resolutions[i].height))
                {
                    skip = true;
                    break;
                }
            }

            if (skip) continue;
            availableDisplayResolutions.Add(Screen.resolutions[i]);
        }

        availableDisplayResolutions.Sort((a, b) => (a.width.CompareTo(b.width)));
    }

    public void ChangeResolutionFromDropdownAndSetOptions()
    {
        if (!GameMasterScript.actualGameStarted) return;
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        int selected = Math.Max(0, (int)optionsResolution.gameObj.GetComponent<Slider>().value);
        bool fullScreen = optionsFullscreen.gameObj.GetComponent<Toggle>().isOn;
        if (!LogoSceneScript.AllowFullScreenSelection())
        {
            fullScreen = true;
        }

        //Debug.Log("SET player resolution via ChangeResFromDropdownAndSetOptions");

        if (selected >= availableDisplayResolutions.Count)
        {
            if (availableDisplayResolutions.Count != 0)
            {
                selected = availableDisplayResolutions.Count - 1;
            }
            else
            {
                GetAllDisplayResolutions();
                if (availableDisplayResolutions.Count != 0)
                {
                    selected = availableDisplayResolutions.Count - 1;
                }
                else
                {
                    return;
                }
            }
        }

        PlayerOptions.resolutionX = availableDisplayResolutions[selected].width;
        PlayerOptions.resolutionY = availableDisplayResolutions[selected].height;

        if (!movingSliderViaKeyboard)
        {
            //Screen.SetResolution(availableDisplayResolutions[selected].width, availableDisplayResolutions[selected].height, PlayerOptions.fullscreen);
            SetResolutionFromOptions();
        }

        UpdateResolutionText();
    }

    public static void SetSoundSliderValuesFromPrefs()
    {

    }

    public static void ReadOptionsMenuVariables()
    {
        // Field of view

        GameObject go = optionsZoomScale.gameObj;
        Slider sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.zoomScale;

        // Music volume
        go = optionsMusicVolume.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.musicVolume;

        // SFX volume
        go = optionsSFXVolume.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.SFXVolume;

        // Steps volume
        go = optionsFootstepsVolume.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.footstepsVolume;

        go = optionsFramecap.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.framecap;

        go = optionsTextSpeed.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.textSpeed;

        go = optionsBattleTextSpeed.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.battleTextSpeed;

        go = optionsBattleTextScale.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.battleTextScale;

        go = optionsButtonDeadZone.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.buttonDeadZone;

        go = optionsCursorRepeatDelay.gameObj;
        sli = go.GetComponent<Slider>();
        sli.value = PlayerOptions.cursorRepeatDelay;

        // Lock camera

        /* go = optionsLockCamera.gameObj;
        Toggle tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.lockCamera; */

        Toggle tgl;

        go = optionsCameraScanlines.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.scanlines;

        go = optionsAudioOffWhenMinimized.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.audioOffWhenMinimized;

        go = optionsFullscreen.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.fullscreen;

        go = optionsUseStepMoveJoystickStyle.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.joystickControlStyle == JoystickControlStyles.STEP_MOVE;

	    go = optionsDisableMouseOnKeyJoystick.gameObj;
	    tgl = go.GetComponent<Toggle>();
	    tgl.isOn = PlayerOptions.disableMouseOnKeyJoystick;		

        go = optionsShowTutorialPopups.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.tutorialTips;

        go = optionsShowControllerPrompts.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.showControllerPrompts;

        go = optionsShowJPXPGain.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.battleJPXPGain;

        /* go = optionsAutoPickupItems.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoPickupItems; */

        go = optionsAutoUsePlanks.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoPlanksInItemWorld;

        go = optionsAutoEquipBestOffhand.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoEquipBestOffhand;

        go = optionsAutoEquipWeapons.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoEquipWeapons;

        go = optionsVerboseCombatLog.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.verboseCombatLog;

        go = optionsPlayerHealthBar.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.playerHealthBar;

        go = optionsMonsterHealthBars.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.monsterHealthBars;

        go = optionsScreenFlashes.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.screenFlashes;

        go = optionsSmallCombatLogText.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.smallLogText;

        go = optionsUIPulses.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.showUIPulses;

        go = optionsShowRumorOverlay.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.showRumorOverlay;

        go = optionsAutoEatFood.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoEatFood;

        go = optionsAutoAbandonRumors.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.autoAbandonTrivialRumors;

        go = optionsExtraTurnDisplay.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.extraTurnPopup;

        go = optionsPickupDisplay.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.pickupDisplay;

        go = optionsGridOverlay.gameObj;
        tgl = go.GetComponent<Toggle>();
        tgl.isOn = PlayerOptions.gridOverlay;

        if ((singletonUIMS == null) || (singletonUIMS.availableDisplayResolutions == null))
        {
            return;
        }
        if (singletonUIMS.availableDisplayResolutions.Count == 0)
        {
            return;
        }
        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            int index = 0;
            for (int i = 0; i < singletonUIMS.availableDisplayResolutions.Count; i++)
            {
                if ((PlayerOptions.resolutionX == singletonUIMS.availableDisplayResolutions[i].width) && (PlayerOptions.resolutionY == singletonUIMS.availableDisplayResolutions[i].height))
                {
                    index = i;
                    break;
                }
            }
            optionsResolution.gameObj.GetComponent<Slider>().value = index;
            return;
        }
    }

    public static float GetFOVSlider()
    {
        //GameObject go = GameObject.Find("FOV Slider");

        GameObject go = null;
        if (GameMasterScript.InMainScene())
        {
            go = GameObject.Find("FOV Slider");
        }
        else
        {
            go = optionsZoomScale.gameObj;
        }


        Slider sli = go.GetComponent<Slider>();
        //PlayerPrefs.SetInt("ZoomScale",(int)sli.value);
        PlayerOptions.zoomScale = (int)sli.value;
        return sli.value;
    }

    public static float GetMusicVolume()
    {
        //GameObject go = GameObject.Find("Music Volume");

        GameObject go = null;
        if (GameMasterScript.InMainScene())
        {
            go = GameObject.Find("Music Volume");
            Debug.Log("Seeking music vol in main");
        }
        else
        {
            go = optionsMusicVolume.gameObj;
            //Debug.Log("Seeking music vol in gameplay");
        }

        if (go == null) return 1f;
        Slider sli = go.GetComponent<Slider>();
        //Debug.Log("NOT returning 1f, sli value is " + sli.value + " but int is " + PlayerPrefs.GetInt("MusicVolume"));
        //PlayerPrefs.SetInt("MusicVolume",(int)sli.value);
        PlayerOptions.MusicVolume = (int)sli.value;
        
        return sli.value;
    }

    public static float GetFootstepsVolume()
    {
        GameObject go = null;
        if (GameMasterScript.InMainScene())
        {
            go = GameObject.Find("Footsteps Volume");
        }
        else
        {
            go = optionsFootstepsVolume.gameObj;
        }
        if (go == null) return 1f;
        Slider sli = go.GetComponent<Slider>();
        PlayerOptions.FootstepsVolume = (int)sli.value;
        return sli.value;
    }

    public static float GetSFXVolume()
    {
        GameObject go = null;
        if (GameMasterScript.InMainScene())
        {
            go = GameObject.Find("SFX Volume");
        }
        else
        {
            go = optionsSFXVolume.gameObj;
        }

        if (go == null) return 1f;
        Slider sli = go.GetComponent<Slider>();
        
        PlayerOptions.SSFXVolume = (int)sli.value;
        return sli.value;
    }

    public static void UpdateButtonDeadZoneText()
    {
        optionsButtonDeadZoneText.text = StringManager.GetString("ui_options_deadzone") + ": " + PlayerOptions.buttonDeadZone + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
    }

    public static void UpdateResolutionText()
    {
        if (singletonUIMS.availableDisplayResolutions == null) return;
        if ((PlayerOptions.resolutionX == 0) || (PlayerOptions.resolutionY == 0))
        {
            for (int i = 0; i < singletonUIMS.availableDisplayResolutions.Count; i++)
            {
                if ((singletonUIMS.availableDisplayResolutions[i].width == Screen.width) && (singletonUIMS.availableDisplayResolutions[i].height == Screen.height))
                {
                    PlayerOptions.resolutionX = Screen.width;
                    PlayerOptions.resolutionY = Screen.height;
                    break;
                }
            }

        }
        optionsResolutionText.text = StringManager.GetString("ui_options_resolution") + ": " + PlayerOptions.resolutionX + "x" + PlayerOptions.resolutionY;

    }

    public static void UpdateFrameCapText()
    {
        int value = PlayerOptions.framecap;
        string txt = StringManager.GetString("ui_options_framecap") + ": ";
        switch (value)
        {
            case 0:
                txt += "30";
                break;
            case 1:
                txt += "60";
                break;
            case 2:
                txt += "120";
                break;
            case 3:
                txt += "144";
                break;
        }
        optionsFrameCapText.text = txt;
    }

    public static void UpdateBattleTextScaleText()
    {
        int value = PlayerOptions.battleTextScale;
        string txt = StringManager.GetString("ui_options_battletextscale") + ": " + value + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        optionsBattleTextScaleText.text = txt;

    }

    public static void UpdateTextSpeedText()
    {
        int value = PlayerOptions.textSpeed;
        string txt = StringManager.GetString("ui_options_textspeed") + ": ";
        switch (value)
        {
            case (int)TextSpeed.SLOW:
                txt += StringManager.GetString("misc_speed_0").ToUpperInvariant();
                break;
            case (int)TextSpeed.MEDIUM:
                txt += StringManager.GetString("misc_speed_1").ToUpperInvariant();
                break;
            case (int)TextSpeed.FAST:
                txt += StringManager.GetString("misc_speed_2").ToUpperInvariant();
                break;
            case (int)TextSpeed.VERYFAST:
                txt += StringManager.GetString("misc_speed_3").ToUpperInvariant();
                break;
            case (int)TextSpeed.INSTANT:
                txt += StringManager.GetString("misc_speed_instant").ToUpperInvariant();
                break;
        }

        optionsTextSpeedText.text = txt;

        value = PlayerOptions.battleTextSpeed;

        txt = StringManager.GetString("ui_options_battletextspeed") + ": ";

        switch (value)
        {
            case (int)TextSpeed.SLOW:
                txt += StringManager.GetString("misc_speed_0").ToUpperInvariant();
                break;
            case (int)TextSpeed.MEDIUM:
                txt += StringManager.GetString("misc_speed_1").ToUpperInvariant();
                break;
            case (int)TextSpeed.FAST:
                txt += StringManager.GetString("misc_speed_2").ToUpperInvariant();
                break;
            case (int)TextSpeed.VERYFAST:
                txt += StringManager.GetString("misc_speed_3").ToUpperInvariant();
                break;
        }

        optionsBattleTextSpeedText.text = txt;
    }

    public static void UpdateCursorRepeatDelayText()
    {
        //int value = PlayerPrefs.GetInt("CursorRepeatDelay");
        int value = PlayerOptions.cursorRepeatDelay;
        optionsCursorRepeatDelayText.text = StringManager.GetString("ui_options_cursorrepeatdelay") + ": " + value + StringManager.GetString("misc_milliseconds_abbreviation");
    }

    public void UpdateBattleTextScale()
    {
        if (GetWindowState(UITabs.OPTIONS))
        {
            UpdateBattleTextScaleText();
        }
    }

    public void UpdateTextSpeed()
    {
        switch (PlayerOptions.textSpeed)
        {
            case (int)TextSpeed.SLOW:
                charactersPerTypewriterTick = 1;
                typewriterTextSpeed = 0.0075f;
                break;
            case (int)TextSpeed.MEDIUM:
                charactersPerTypewriterTick = 1;
                typewriterTextSpeed = 0.005f;
                break;
            case (int)TextSpeed.FAST:
                charactersPerTypewriterTick = 2;
                typewriterTextSpeed = 0.005f;
                break;
            case (int)TextSpeed.VERYFAST:
                charactersPerTypewriterTick = 3;
                typewriterTextSpeed = 0.004f;
                break;
        }

        switch (PlayerOptions.battleTextSpeed)
        {
            case (int)TextSpeed.SLOW:
                BattleTextManager.SetAnimTime(1.6f);
                break;
            case (int)TextSpeed.MEDIUM:
                BattleTextManager.SetAnimTime(1.2f);
                break;
            case (int)TextSpeed.FAST:
                BattleTextManager.SetAnimTime(0.78f);
                typewriterTextSpeed = 0.005f;
                break;
            case (int)TextSpeed.VERYFAST:
                BattleTextManager.SetAnimTime(0.45f);
                break;
        }

        if (GetWindowState(UITabs.OPTIONS))
        {
            UpdateTextSpeedText();
        }
    }

}
