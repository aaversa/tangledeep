using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public partial class TitleScreenScript
{
    void InitializeTitleScreen(bool firstStart)
    {
        LoadingWaiterManager.Hide(0.25f);
        PlayerModManager.Initialize();
        uims.TitleScreenStart(firstStart);
        if (blackFadeImage.color.a == 1f)
        {
            blackFading = true;
            timeScrollStarted = Time.time;
        }

        Vector2 pos;
        Vector2 size;

        pos = SetMainMenuDialogBoxSizeAndPosition(out size);

        TextMeshProUGUI txtDialogMessage = UIManagerScript.myDialogBoxComponent.GetDialogText();

        GameMasterScript.applicationQuittingOrChangingScenes = false;

        int numControllersConnected = player.controllers.Joysticks.Count;
        bool keyboardSelectionDialogPresented = false;


        if (!PlatformVariables.GAMEPAD_ONLY && TDPlayerPrefs.GetInt(GlobalProgressKeys.KEYBOARD_MAP_SELECTED) < 12) // Should be 12
        {
            if (numControllersConnected == 0)
            {
                TDPlayerPrefs.SetInt(GlobalProgressKeys.KEYBOARD_MAP_SELECTED, 12); // Should be 12
                txtDialogMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 200f);
                UIManagerScript.ToggleDialogBox(DialogType.TITLESCREEN, true, true, size, pos);
                UIManagerScript.CreateDialogOption(StringManager.GetString("key_layout_wasd"), DialogButtonResponse.SETUP_WASD);
                UIManagerScript.CreateDialogOption(StringManager.GetString("key_layout_full"), DialogButtonResponse.SETUP_BOTHHANDS);
                txtDialogMessage.fontSize = 36;
                txtDialogMessage.enabled = true;
                string strToWrite = StringManager.GetString("dialog_firsttime_keysetup");
                UIManagerScript.DialogBoxWrite(strToWrite);
                txtDialogMessage.text = strToWrite;
                txtDialogMessage.color = Color.white;
                keyboardSelectionDialogPresented = true;
            }
            else
            {
                TitleScreenScript.ChangeControlType(KeyboardControlMaps.DEFAULT);
                if (Debug.isDebugBuild) Debug.Log("Set default control type because we had a controller attached.");
            }
        }


        if (!keyboardSelectionDialogPresented)
        {
            if (GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUS || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUSPLUS)
            {
                CreateTitleScreenVictoryDialog(txtDialogMessage, pos, size);
            }
            else
            {
                CreateMainMenuDialog(size, pos);
            }
        }

        UIManagerScript.UpdateDialogCursorPos();
        CreateStage = CreationStages.TITLESCREEN;
        CharCreation.NameEntryScreenState = ENameEntryScreenState.max;

        StringManager.AssignWeaponPropertyStrings();

#if UNITY_EDITOR
        //Debug.Log("Title screen initialized");
#endif
    }

    void CreateTitleScreenVictoryDialog(TextMeshProUGUI txtDialogMessage, Vector2 pos, Vector2 size)
    {
        string victoryString = "help_player_victorious";

        switch (GameStartData.CurrentLoadState)
        {
            case LoadStates.PLAYER_VICTORY:
                break;
            case LoadStates.PLAYER_VICTORY_NGPLUS:
                victoryString = "help_player_victorious_ngplus";
                break;
            case LoadStates.PLAYER_VICTORY_NGPLUSPLUS:
                victoryString = "help_player_victorious_ngplusplus";
                break;
        }

        GameStartData.CurrentLoadState = LoadStates.NORMAL;
        UIManagerScript.ToggleDialogBox(DialogType.TITLESCREEN, true, true, new Vector2(900f, 5f), pos);
        UIManagerScript.CreateDialogOption(StringManager.GetString("ui_option_ok"), DialogButtonResponse.CONTINUE);
        txtDialogMessage.fontSize = 36;
        txtDialogMessage.color = Color.white; // make sure we don't have transparent text for any reason!
        txtDialogMessage.enabled = true;
        UIManagerScript.DialogBoxWrite(StringManager.GetString(victoryString));
        UIManagerScript.ChangeUIFocus(UIManagerScript.dialogUIObjects[0]);
        txtDialogMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 200f);
        UIManagerScript.myDialogBoxComponent.transform.localPosition = new Vector3(0f, 0f, 0f); // center this box
    }
}