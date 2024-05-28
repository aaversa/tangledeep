using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionsLocalizationHelper : MonoBehaviour {

    [Header("Headers")]
    public TextMeshProUGUI audioHeader;
    public TextMeshProUGUI controlHeader;
    public TextMeshProUGUI visualHeader;
    public TextMeshProUGUI gameplayHeader;



    [Header("Control Options")]

    public TextMeshProUGUI keyBindings;
    public TextMeshProUGUI keyLayoutWASD;
    public TextMeshProUGUI keyLayout2Hands;

    public TextMeshProUGUI viewHelp; 
    public TextMeshProUGUI saveAndQuit;
    public TextMeshProUGUI saveAndBackToTitle;


    // Use this for initialization
    void Start () {
        audioHeader.text = StringManager.GetString("ui_options_audio");
        controlHeader.text = StringManager.GetString("ui_options_control");
        visualHeader.text = StringManager.GetString("ui_options_visual");
        gameplayHeader.text = StringManager.GetString("ui_options_gameplay");

        FontManager.LocalizeMe(audioHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(controlHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(visualHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(gameplayHeader, TDFonts.WHITE);

        keyBindings.text = StringManager.GetString("ui_options_control_keybindings");
        keyLayoutWASD.text = StringManager.GetString("ui_options_control_keylayoutwasd");
        keyLayout2Hands.text = StringManager.GetString("ui_options_control_twohands");

        FontManager.LocalizeMe(keyBindings, TDFonts.BLACK);
        FontManager.LocalizeMe(keyLayoutWASD, TDFonts.BLACK);
        FontManager.LocalizeMe(keyLayout2Hands, TDFonts.BLACK);

        viewHelp.text = StringManager.GetString("ui_options_gameplay_viewhelp");
        saveAndQuit.text = StringManager.GetString("ui_options_gameplay_savequit");
        saveAndBackToTitle.text = StringManager.GetString("ui_options_gameplay_saveback");

        FontManager.LocalizeMe(viewHelp, TDFonts.BLACK);
        FontManager.LocalizeMe(saveAndQuit, TDFonts.BLACK);
        FontManager.LocalizeMe(saveAndBackToTitle, TDFonts.BLACK);
    }

    public static void SetupFonts()
    {
        FontManager.LocalizeMe(UIManagerScript.optionsMusicVolumeContainer.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        FontManager.LocalizeMe(UIManagerScript.optionsSFXVolumeContainer.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        FontManager.LocalizeMe(UIManagerScript.optionsFootstepsVolumeContainer.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        FontManager.LocalizeMe(UIManagerScript.optionsZoomScaleContainer.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        UIManagerScript.optionsZoomScaleContainer.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("ui_options_visual_zoomscale");

        TextMeshProUGUI fullscreenMesh = UIManagerScript.optionsFullscreen.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        fullscreenMesh.text = StringManager.GetString("ui_options_visual_fullscreen");
        FontManager.LocalizeMe(fullscreenMesh, TDFonts.WHITE);

        TextMeshProUGUI audioOff = UIManagerScript.optionsAudioOffWhenMinimized.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        audioOff.text = StringManager.GetString("ui_options_audio_audiominimize");
        FontManager.LocalizeMe(audioOff, TDFonts.WHITE);

        TextMeshProUGUI buttonPrompts = UIManagerScript.optionsShowControllerPrompts.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        buttonPrompts.text = StringManager.GetString("ui_options_control_controllerprompts");
        FontManager.LocalizeMe(buttonPrompts, TDFonts.WHITE);

        TextMeshProUGUI confirmStep = UIManagerScript.optionsUseStepMoveJoystickStyle.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        confirmStep.text = StringManager.GetString("ui_options_control_confirmstepmove");
        FontManager.LocalizeMe(confirmStep, TDFonts.WHITE);

		if (!PlatformVariables.GAMEPAD_ONLY)
		{
	        TextMeshProUGUI disableMouse = UIManagerScript.optionsDisableMouseOnKeyJoystick.gameObj.GetComponentInChildren<TextMeshProUGUI>();
	        disableMouse.text = StringManager.GetString("ui_options_control_disablemouse");
	        FontManager.LocalizeMe(disableMouse, TDFonts.WHITE);

            TextMeshProUGUI disableMouseMovement = UIManagerScript.optionsDisableMouseMovement.gameObj.GetComponentInChildren<TextMeshProUGUI>();
            disableMouseMovement.text = StringManager.GetString("ui_options_control_disablemousemovement");
            FontManager.LocalizeMe(confirmStep, TDFonts.WHITE);
        }

        TextMeshProUGUI autoUsePlanks = UIManagerScript.optionsAutoUsePlanks.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        autoUsePlanks.text = StringManager.GetString("ui_options_gameplay_planks");
        FontManager.LocalizeMe(autoUsePlanks, TDFonts.WHITE);

        TextMeshProUGUI bestOffhand = UIManagerScript.optionsAutoEquipBestOffhand.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        bestOffhand.text = StringManager.GetString("ui_options_gameplay_bestoffhand");
        FontManager.LocalizeMe(bestOffhand, TDFonts.WHITE);

        TextMeshProUGUI bestWeapon = UIManagerScript.optionsAutoEquipWeapons.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        bestWeapon.text = StringManager.GetString("ui_options_gameplay_bestweapons");
        FontManager.LocalizeMe(bestWeapon, TDFonts.WHITE);

        TextMeshProUGUI scanlines = UIManagerScript.optionsCameraScanlines.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        scanlines.text = StringManager.GetString("ui_options_visual_scanlines");
        FontManager.LocalizeMe(scanlines, TDFonts.WHITE);

        TextMeshProUGUI gridOverlay = UIManagerScript.optionsGridOverlay.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        gridOverlay.text = StringManager.GetString("ui_options_visual_gridoverlay");
        FontManager.LocalizeMe(gridOverlay, TDFonts.WHITE);

        TextMeshProUGUI playerHealth = UIManagerScript.optionsPlayerHealthBar.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        playerHealth.text = StringManager.GetString("ui_options_visual_playerhealth");
        FontManager.LocalizeMe(playerHealth, TDFonts.WHITE);

        TextMeshProUGUI monsterHealth = UIManagerScript.optionsMonsterHealthBars.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        monsterHealth.text = StringManager.GetString("ui_options_visual_monsterhealth");
        FontManager.LocalizeMe(monsterHealth, TDFonts.WHITE);

        TextMeshProUGUI visualFlashes = UIManagerScript.optionsScreenFlashes.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        visualFlashes.text = StringManager.GetString("ui_options_visual_screenflashes");
        FontManager.LocalizeMe(visualFlashes, TDFonts.WHITE);

        TextMeshProUGUI smallerLog = UIManagerScript.optionsSmallCombatLogText.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        smallerLog.text = StringManager.GetString("ui_options_visual_logtext");
        FontManager.LocalizeMe(smallerLog, TDFonts.WHITE);

        TextMeshProUGUI uiPulse = UIManagerScript.optionsUIPulses.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        uiPulse.text = StringManager.GetString("ui_options_visual_uipulses");
        FontManager.LocalizeMe(uiPulse, TDFonts.WHITE);

        TextMeshProUGUI showRumorOverlay = UIManagerScript.optionsShowRumorOverlay.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        showRumorOverlay.text = StringManager.GetString("ui_options_rumor_display");
        FontManager.LocalizeMe(showRumorOverlay, TDFonts.WHITE);

        TextMeshProUGUI autoEatFood = UIManagerScript.optionsAutoEatFood.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        autoEatFood.text = StringManager.GetString("ui_options_autoeat_food");
        FontManager.LocalizeMe(autoEatFood, TDFonts.WHITE);

        TextMeshProUGUI autoAbandonRumors = UIManagerScript.optionsAutoAbandonRumors.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        autoAbandonRumors.text = StringManager.GetString("ui_options_auto_abandon_rumors");
        FontManager.LocalizeMe(autoAbandonRumors, TDFonts.WHITE);

        TextMeshProUGUI verboseLog = UIManagerScript.optionsVerboseCombatLog.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        verboseLog.text = StringManager.GetString("ui_options_gameplay_verbose");
        FontManager.LocalizeMe(verboseLog, TDFonts.WHITE);

        TextMeshProUGUI tutorial = UIManagerScript.optionsShowTutorialPopups.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        tutorial.text = StringManager.GetString("ui_options_gameplay_tutorial");
        FontManager.LocalizeMe(tutorial, TDFonts.WHITE);

        TextMeshProUGUI jpxp = UIManagerScript.optionsShowJPXPGain.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        jpxp.text = StringManager.GetString("ui_options_gameplay_jpxp");
        FontManager.LocalizeMe(jpxp, TDFonts.WHITE);

        TextMeshProUGUI pickupDisplay = UIManagerScript.optionsPickupDisplay.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        pickupDisplay.text = StringManager.GetString("ui_options_gameplay_pickups");
        FontManager.LocalizeMe(pickupDisplay, TDFonts.WHITE);

        TextMeshProUGUI extraTurn = UIManagerScript.optionsExtraTurnDisplay.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        extraTurn.text = StringManager.GetString("ui_options_gameplay_extraturn");
        FontManager.LocalizeMe(extraTurn, TDFonts.WHITE);
    }
	
}
