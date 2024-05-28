using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class OptionsUIScript : MonoBehaviour {

    public static OptionsUIScript singleton;

	// Use this for initialization
	void Start () {
        singleton = this;
	}
	
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

    public void ToggleUIPulses()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.showUIPulses = !PlayerOptions.showUIPulses;
        GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
    }

    public void ToggleAutoEatFood()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoEatFood = !PlayerOptions.autoEatFood;
    }

    public void ToggleDisableMouseMovement()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.disableMouseMovement = !PlayerOptions.disableMouseMovement;
    }

    public void ToggleAutoAbandonRumors()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.autoAbandonTrivialRumors = !PlayerOptions.autoAbandonTrivialRumors;
    }

    public void ToggleShowRumorOverlay()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.showRumorOverlay = !PlayerOptions.showRumorOverlay;
        RumorTextOverlay.OnRumorOverlayToggleChanged();
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
    /// Refocuses on a given slider, and then provides that slider's value.
    /// </summary>
    /// <param name="checkObject">The UIObject to check for validity</param>
    /// <param name="sliderEnum">The enum of the slider we want to focus</param>
    /// <param name="changedValue">The value we want to adjust based on the slider. This will be unchanged if the checkObject is not valid!</param>
    /// <returns>False if the slider is inactive</returns>
    bool RefocusAndGetValueFromSlider(UIManagerScript.UIObject checkObject, OptionsSlider sliderEnum, ref int changedValue )
    {   
        //check for validity
        if (checkObject == null ||
            checkObject.gameObj == null ||
            !checkObject.gameObj.activeSelf )
        {
            return false;
        }

        //focus on the slider
        UIManagerScript.singletonUIMS.RefocusOptionsSlider(sliderEnum);

        //return the new updated value
        changedValue = (int)checkObject.gameObj.GetComponent<Slider>().value;

        return true;
    }

    public void SetFrameCapbyMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsFramecap, OptionsSlider.FRAMECAP,
            ref PlayerOptions.framecap))
        {
            GameMasterScript.UpdateFrameCapFromOptionsValue();
            UIManagerScript.UpdateFrameCapText();
        }
    }

    public void SetTextSpeedByMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsTextSpeed, OptionsSlider.TEXTSPEED,
            ref PlayerOptions.textSpeed))
        {
            UIManagerScript.singletonUIMS.UpdateTextSpeed();
        }
    }

    public void SetBattleTextSpeedByMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsBattleTextSpeed, OptionsSlider.BATTLETEXTSPEED,
            ref PlayerOptions.battleTextSpeed))
        {
            UIManagerScript.singletonUIMS.UpdateTextSpeed();
        }
    }

    public void SetBattleTextScaleByMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsBattleTextScale, OptionsSlider.BATTLETEXTSCALE,
            ref PlayerOptions.battleTextScale))
        {
            UIManagerScript.singletonUIMS.UpdateBattleTextScale();
        }
    }

    public void SetCursorRepeatDelayByMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsCursorRepeatDelay, OptionsSlider.CURSORREPEATDEALY,
            ref PlayerOptions.cursorRepeatDelay))
        {
            GameMasterScript.UpdateCursorRepeatDelayFromOptionsValue();
            UIManagerScript.UpdateCursorRepeatDelayText();
        }
    }

    public void SetButtonDeadZoneByMouse()
    {
        if (RefocusAndGetValueFromSlider(UIManagerScript.optionsButtonDeadZone, OptionsSlider.BUTTONDEADZONE,
            ref PlayerOptions.buttonDeadZone))
        {
            GameMasterScript.UpdateButtonDeadZoneFromOptionsValues();
            UIManagerScript.UpdateCursorRepeatDelayText();
        }
    }

    public void SetFOVByMouse()
    {
        UIManagerScript.singletonUIMS.RefocusOptionsSlider(OptionsSlider.ZOOMSCALE);
        PlayerOptions.zoomScale = (int)UIManagerScript.GetFOVSlider();
        if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            GameMasterScript.cameraScript.UpdateFOVFromOptionsValue();
        }        
    }

    public void ChangeFullScreenByMouse()
    {
        if (!GameMasterScript.actualGameStarted) return;
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.fullscreen = !PlayerOptions.fullscreen;
        UIManagerScript.singletonUIMS.SetResolutionFromOptions();
        UIManagerScript.UpdateResolutionText();
    }

    public void ToggleAudioOffWhenMinimized()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.audioOffWhenMinimized = !PlayerOptions.audioOffWhenMinimized;
    }

    public void SetFootstepsVolumeByMouse()
    {
        UIManagerScript.singletonUIMS.RefocusOptionsSlider(OptionsSlider.FOOTSTEPSVOLUME);
        MusicManagerScript.singleton.SetFootstepsVolumeFromPlayerOptions();
        SetLabelOnOptionSliderTextWithPercentValue(UIManagerScript.optionsFootstepsVolume.gameObj.transform.parent.gameObject, "ui_options_footsteps_volume");
    }

    public void SetMusicVolumeViaMouse()
    {
        UIManagerScript.singletonUIMS.RefocusOptionsSlider(OptionsSlider.MUSICVOLUME);
        MusicManagerScript.singleton.SetMusicVolumeToMatchOptionsSlider();
        SetLabelOnOptionSliderTextWithPercentValue(UIManagerScript.optionsMusicVolume.gameObj.transform.parent.gameObject, "ui_options_music_volume");
    }

    public void SetSFXVolumeViaMouse()
    {
        UIManagerScript.singletonUIMS.RefocusOptionsSlider(OptionsSlider.SFXVOLUME);
        MusicManagerScript.singleton.SetSFXVolumeFromPlayerOptions();
        SetLabelOnOptionSliderTextWithPercentValue(UIManagerScript.optionsSFXVolume.gameObj.transform.parent.gameObject, "ui_options_sfx_volume");
    }

    void SetLabelOnOptionSliderTextWithPercentValue(GameObject obj, string strLocalizedReference)
    {
        TextMeshProUGUI txtLabel = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (txtLabel == null)
        {
            return;
        }
        Slider sli = obj.GetComponentInChildren<Slider>();
        StringManager.SetTag(0, (int)(sli.normalizedValue * 100.0f) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
        txtLabel.text = StringManager.GetString(strLocalizedReference);
    }

    public void ToggleDisableMouse()
    {
        UIManagerScript.PlayCursorSound("Tick");
        PlayerOptions.disableMouseOnKeyJoystick = !PlayerOptions.disableMouseOnKeyJoystick;
        if (!PlayerOptions.disableMouseOnKeyJoystick)
        {
            Cursor.visible = true;
        }
    }

    public void ToggleJoystickStyle( )
    {
        if (PlayerOptions.joystickControlStyle == JoystickControlStyles.STANDARD)
        {
            PlayerOptions.joystickControlStyle = JoystickControlStyles.STEP_MOVE;
        }
        else
        {
            PlayerOptions.joystickControlStyle = JoystickControlStyles.STANDARD;

            try
            {
                GameMasterScript.heroPCActor.diagonalOverlay.SetActive(false);
            }
            catch(Exception e)
            {
                Debug.Log("Issue occurred trying to disable diagonal overlay: " + e);
            }                        
        }
    }
}
