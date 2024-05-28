using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Rewired;

public partial class GameMasterScript : MonoBehaviour
{
	const float CONSOLE_MOVEMENT_OPTIONS_TIME = 0.38f;
    public static void UpdateButtonDeadZoneFromOptionsValues()
    {
        // Nothing happens.
    }

    public static void UpdateFrameCapFromOptionsValue()
    {
		if (PlatformVariables.FIXED_FRAMERATE) return;

        int value = PlayerOptions.framecap;
        switch (value)
        {
            case 0:
                GraphicsAndFramerateManager.SetApplicationFPS(30);
                break;
            case 1:
                GraphicsAndFramerateManager.SetApplicationFPS(60);
                break;
            case 2:
                GraphicsAndFramerateManager.SetApplicationFPS(120);
                break;
            case 3:
                GraphicsAndFramerateManager.SetApplicationFPS(144);
                break;
        }
    }

    public static void UpdateCursorRepeatDelayFromOptionsValue()
    {

        int value = PlayerOptions.cursorRepeatDelay;
        float trueValue = value / 1000f;

		if (PlatformVariables.GAMEPAD_ONLY) 
		{
			gmsSingleton.movementInputOptionsTime = CONSOLE_MOVEMENT_OPTIONS_TIME;		
		}
		else 
		{
			gmsSingleton.movementInputOptionsTime = trueValue;	
		}
		

    }


    public void SwitchControlModeByInt(int mode)
    {
        if (Debug.isDebugBuild) Debug.Log("Switching control mode method " + (KeyboardControlMaps)mode);
        SetTempGameData("trykeybindmode", mode);

        UIManagerScript.lastDialogDBRSelected = DialogButtonResponse.CONTINUE;
        UIManagerScript.lastDialogActionRefSelected = "switchcontrolmode";
        string keybindString = "";
        switch (mode)
        {
            case (int)KeyboardControlMaps.DEFAULT:
                keybindString = StringManager.GetString("key_layout_full");
                break;
            case (int)KeyboardControlMaps.WASD:
                keybindString = StringManager.GetString("key_layout_wasd");
                break;
            case (int)KeyboardControlMaps.NOTSET:
                keybindString = StringManager.GetString("key_layout_full");
                PlayerOptions.keyboardMap = KeyboardControlMaps.DEFAULT;
                PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.DEFAULT;
                break;
        }
        StringManager.SetTag(0, keybindString);

        UIManagerScript.ForceCloseFullScreenUIWithNoFade();        
        UIManagerScript.StartConversationByRef("confirm_reset_keybindings", DialogType.CONFIRM, null);
        //UIManagerScript.ToggleConfirmationDialog(StringManager.GetString("confirm_reset_keybinds"), true, "resetbindings");
        //SwitchControlMode((KeyboardControlMaps)mode);
    }

    public static void SwitchControlMode(KeyboardControlMaps kcm)
    {
        if (kcm == KeyboardControlMaps.NOTSET)
        {
            kcm = KeyboardControlMaps.DEFAULT;
            PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.DEFAULT;
        }

		if (PlatformVariables.GAMEPAD_ONLY) return;

        gmsSingleton.player.controllers.maps.ClearMaps(ControllerType.Keyboard, false);
        gmsSingleton.player.controllers.maps.RemoveMap(ControllerType.Keyboard, 0, 0);

        switch (kcm)
        {
            case KeyboardControlMaps.DEFAULT:
                LayoutHelper.SwitchLayout(0, ControllerType.Keyboard, 0, "Default", "Default");
                PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.DEFAULT;
                PlayerOptions.keyboardMap = KeyboardControlMaps.DEFAULT;
                break;
            case KeyboardControlMaps.WASD:
                LayoutHelper.SwitchLayout(0, ControllerType.Keyboard, 0, "Default", "WASD");
                PlayerOptions.defaultKeyboardMap = KeyboardControlMaps.WASD;
                PlayerOptions.keyboardMap = KeyboardControlMaps.WASD;

                break;
        }
        gmsSingleton.player.controllers.maps.SetMapsEnabled(true, "Default");
        gmsSingleton.player.controllers.maps.SetMapsEnabled(true, "MenuControls");

        gmsSingleton.cMapper.keyboardMapDefaultLayout = (int)kcm;
        PlayerOptions.WriteOptionsToFile();
        ReInput.userDataStore.Save();
        TDPlayerPrefs.Save();
    }

    void OnRestoreDefaults()
    {
        IList<Player> players = ReInput.players.Players;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            player.controllers.maps.LoadDefaultMaps(ControllerType.Joystick);
            player.controllers.maps.LoadDefaultMaps(ControllerType.Mouse);
        }
        GameMasterScript.SwitchControlMode(PlayerOptions.defaultKeyboardMap);
        StartCoroutine(WaitThenCloseControlMapper());
    }

    public void TogglePlayerHealthBar()
    {
        //PlayerOptions.playerHealthBar = !PlayerOptions.playerHealthBar;
        //Debug.Log("Turning player health bar: " + PlayerOptions.playerHealthBar);
        heroPCActor.healthBarScript.gameObject.SetActive(PlayerOptions.playerHealthBar);
        heroPCActor.EnableWrathBarIfNeeded();
    }

    public void TogglePlayerHealthBarFromKB()
    {
        PlayerOptions.playerHealthBar = !PlayerOptions.playerHealthBar;
        //Debug.Log("KB switch player health bar state");
        TogglePlayerHealthBar();
    }

    public void TogglePetHUDFromKB()
    {
        if (PetPartyUIScript.tabIsExpanded)
        {
            PetPartyUIScript.singleton.CollapsePetPartyUI();
        }
        else
        {
            PetPartyUIScript.singleton.ExpandPetPartyUI();
        }
    }

    public void ToggleMonsterHealthBars()
    {
        //PlayerOptions.monsterHealthBars = !PlayerOptions.monsterHealthBars;
        foreach (Monster mn in MapMasterScript.activeMap.monstersInMap)
        {
            mn.healthBarScript.gameObject.SetActive(PlayerOptions.monsterHealthBars);
            if (!mn.myMovable.inSight)
            {
                mn.healthBarScript.SetAlpha(0f);
            }
            mn.healthBarScript.UpdateBar(mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH));
        }
    }

    public void ToggleMonsterHealthBarsFromKB()
    {
        PlayerOptions.monsterHealthBars = !PlayerOptions.monsterHealthBars;
        //Debug.Log("KB switch MONSTER health bar state");
        ToggleMonsterHealthBars();
    }
}
