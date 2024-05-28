using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TitleScreenScript
{
    public static void CreateMainMenuDialog(Vector2 size, Vector2 pos, bool useDefaultSizes = false)
    {
        if (useDefaultSizes)
        {
            size = new Vector2(400f, 5f);
            pos = new Vector2(0f, MAIN_MENU_DEFAULT_YPOS);
        }
        UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
        UIManagerScript.ClearDialogBoxText();
        UIManagerScript.ClearAllDialogOptions();
        UIManagerScript.ClearConversation();
        UIManagerScript.ToggleDialogBox(DialogType.TITLESCREEN, true, true, size, pos);

        float fXCursorOffset = -5f;
        UIManagerScript.myDialogBoxComponent.AdjustSettingsForResponsesAfterDialogBoxStart(true, ref fXCursorOffset); // needed anymore?

        UIManagerScript.ClearDialogBoxText(true);
        UIManagerScript.CreateBigDialogOption(StringManager.GetString("ui_mm_new_game"), DialogButtonResponse.NEWGAME);
        UIManagerScript.CreateBigDialogOption(StringManager.GetString("ui_mm_continue"), DialogButtonResponse.LOADGAME);
        UIManagerScript.CreateBigDialogOption(StringManager.GetString("ui_mm_manage_data"), DialogButtonResponse.MANAGEDATA);

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            UIManagerScript.CreateBigDialogOption(StringManager.GetString("ui_mm_opendiscord"), DialogButtonResponse.COMMUNITY);
        }

        UIManagerScript.CreateBigDialogOption(StringManager.GetString("ui_mm_back"), DialogButtonResponse.EXIT);
		int asianFontSize = 36;
#else
		int asianFontSize = 26;
#endif
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                UIManagerScript.myDialogBoxComponent.GetDialogText().fontSize = asianFontSize;
                break;
            default:
                UIManagerScript.myDialogBoxComponent.GetDialogText().fontSize = 60;
                break;
        }
        UIManagerScript.DialogBoxWrite("");
        UIManagerScript.UpdateDialogCursorPos();

        titleScreenSingleton.modManager.TryEnableWorkshopArea();

        /* UIManagerScript.dialogBoxCloseIcon.SetActive(true);
        UIManagerScript.dialogBoxCloseIcon.GetComponent<Animatable>().SetAnim("Default");
        UIManagerScript.dialogBoxCloseIcon.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); */

    }

    public static void CreateCommunityJoinDialog(Vector2 size, Vector2 pos)
    {
        UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        UIManagerScript.ClearDialogBoxText();
        UIManagerScript.ClearAllDialogOptions();
        UIManagerScript.ClearConversation();
        UIManagerScript.ToggleDialogBox(DialogType.TITLESCREEN, true, true, size, pos);

        // Switched away from BIG options due to font size issues in JP

        UIManagerScript.CreateDialogOption(StringManager.GetString("ui_mm_opendiscord"), DialogButtonResponse.COMMUNITY_OPEN_DISCORD);

        if (webChallengesLoaded && SteamManager.Initialized)
        {
            UIManagerScript.CreateDialogOption("<color=yellow>" + StringManager.GetString("ui_daily_leaderboard") + "</color>", DialogButtonResponse.DAILY_LEADERBOARD);
            UIManagerScript.CreateDialogOption("<color=yellow>" + StringManager.GetString("ui_daily_leaderboard") + "</color> (" + StringManager.GetString("misc_friends") + ")", DialogButtonResponse.DAILY_LEADERBOARD_FRIENDS);
            UIManagerScript.CreateDialogOption(UIManagerScript.greenHexColor + StringManager.GetString("ui_weekly_leaderboard") + "</color>", DialogButtonResponse.WEEKLY_LEADERBOARD);
            UIManagerScript.CreateDialogOption(UIManagerScript.greenHexColor + StringManager.GetString("ui_weekly_leaderboard") + " </color>(" + StringManager.GetString("misc_friends") + ")", DialogButtonResponse.WEEKLY_LEADERBOARD_FRIENDS);
        }

        UIManagerScript.CreateDialogOption(StringManager.GetString("ui_mm_back"), DialogButtonResponse.COMMUNITY_REJECT);
        UIManagerScript.myDialogBoxComponent.GetDialogText().fontSize = 52; // Was 60, which was a bit too big
        UIManagerScript.myDialogBoxComponent.GetDialogText().enabled = true;
        StringManager.SetTag(0, "");
        if (webChallengesLoaded && SteamManager.Initialized)
        {
            StringManager.SetTag(0, StringManager.GetString("community_extra_info"));
        }
        UIManagerScript.DialogBoxWrite(StringManager.GetString("community_discord_hype"));
        UIManagerScript.myDialogBoxComponent.GetDialogText().color = Color.white;
        UIManagerScript.UpdateDialogCursorPos();
    }

    /// <summary>
    /// Ask the player if they want to head boldly into NG+, or instead flush 20 hours of gameplay down the shitter.
    /// </summary>
    private void PromptNGPlusDialogSwitch()
    {
        //if (Debug.isDebugBuild) Debug.Log("NGPLUS dialog switch.");

        //Hide the save/load boxes, just like if we were deleting things
        foreach (var displaySlot in UIManagerScript.saveDataDisplayComponents)
        {
            displaySlot.gameObject.SetActive(false);
        }

        //Don't update the slot select boxes, because they are gone!
        CreateStage = CreationStages.COUNT;


        string confirmText = "";

        //If this file has never been NG+, say one thing.
        //If it already was NG+ and we cleared it again, maybe we can talk about NG++? NG+++?

            bool startSavageConfirm = false;

        if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 0)
        {
            confirmText = StringManager.GetString("newgameplus_option_new");
        }
        else
        {
            if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 1)
            {
                if (!GameStartData.beatGameStates[(int)GameStartData.saveGameSlot])
                {
                    // We are in NG+ mode and haven't beaten it.
                    confirmText = StringManager.GetString("newgameplus_option_existing");
                }
                else
                {
                    confirmText = StringManager.GetString("savagemode_option_new");
                    startSavageConfirm = true;
                }
            }
            else 
            {
                if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 2)
                {
                    startSavageConfirm = true;
                    confirmText = StringManager.GetString("savagemode_option_existing");
                    /* if (GameStartData.beatGameStates[(int)GameStartData.saveGameSlot])
                    {
                        // We are in Savage World and have beaten it
                        confirmText = StringManager.GetString("savagemode_option_existing");
                    }
                    else
                    {
                        
                    } */
                }                
            }
        }

        //Build up a dynamic conversation explaining the choices.
        TextBranch ngPlusBranch = new TextBranch();

        ngPlusBranch.text = confirmText;
        ngPlusBranch.branchRefName = "ngplus";

        ButtonCombo noNG = new ButtonCombo();
        noNG.buttonText = StringManager.GetString("ui_prompt_delete_startnewgame");
        noNG.actionRef = "no";

        ButtonCombo yesNG = new ButtonCombo();



        yesNG.buttonText = StringManager.GetString("ui_prompt_delete_startnewgameplus");

        if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 1)
        {
            if (GameStartData.beatGameStates[(int)GameStartData.saveGameSlot]) // Beat NG+, so we can move on to Savage
            {
                yesNG.buttonText = StringManager.GetString("ui_prompt_delete_savage");
            }
            else // Have not beat NG+, but we can restart our save
            {
                yesNG.buttonText = StringManager.GetString("ui_prompt_delete_startnewgameplus");
            }
        }
        else if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] == 2) // We're already in Savage
        {
            yesNG.buttonText = StringManager.GetString("ui_prompt_delete_savage");
        }

        

        yesNG.actionRef = startSavageConfirm ? "savage" : "yes";

        ButtonCombo loadOldSave = new ButtonCombo();
        loadOldSave.buttonText = StringManager.GetString("ui_prompt_old_save_startnewgameplus");
        loadOldSave.actionRef = "oldsave";

        ButtonCombo iAmScaredAndOrAfraid = new ButtonCombo();
        iAmScaredAndOrAfraid.buttonText = StringManager.GetString("button_backtomenu");
        iAmScaredAndOrAfraid.actionRef = "exit"; // was cancel


        ngPlusBranch.responses.Add(loadOldSave);
        ngPlusBranch.responses.Add(yesNG);        
        ngPlusBranch.responses.Add(noNG);
        ngPlusBranch.responses.Add(iAmScaredAndOrAfraid);

        Conversation convo = new Conversation();
        convo.refName = "switchngplusselection";
        convo.allBranches.Add(ngPlusBranch);
        convo.onDialogSelectionMade = OnDialogConfirm_ShouldStartNGPlus;        

        //Turn on the dialog box again
        UIManagerScript.myDialogBoxComponent.PrepareSelectSlot(false);

        //Set this to be our conversation
        UIManagerScript.currentConversation = convo;

        //Jump to this, the only branch
        UIManagerScript.SwitchConversationBranch(ngPlusBranch);

        //Update!
        UIManagerScript.UpdateDialogBox();

    }

    /// <summary>
    /// Once the player has selected a slot in the Delete mode, we ask them to confirm.
    /// </summary>
    void SpawnDialogForDataDeletion()
    {
        GameStartData.saveGameSlot = idxActiveSaveSlotInMenu;

        //embiggen this once more
        UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);

        //Create a textbranch built on deleting a save file.
        TextBranch managedata = new TextBranch();
        managedata.text = StringManager.GetString("ui_cleardata_types");
        managedata.branchRefName = "managedatachoice";

        StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());

        ButtonCombo ccd = new ButtonCombo();
        ccd.buttonText = StringManager.GetString("ui_confirmresponse_clear_cdata");
        ccd.actionRef = "clearcharacterdata";
        ButtonCombo cad = new ButtonCombo();
        cad.buttonText = StringManager.GetString("ui_confirmresponse_clear_alldata");
        cad.actionRef = "clearalldata";
        ButtonCombo btt = new ButtonCombo();
        btt.buttonText = StringManager.GetString("button_backtomenu");
        btt.dbr = DialogButtonResponse.EXIT;
        btt.actionRef = "exit";
        managedata.responses.Add(ccd);
        managedata.responses.Add(cad);
        managedata.responses.Add(btt);

        //if the conversation is open, reset and use this branch
        var conv = UIManagerScript.currentConversation;
        if (conv == null)
        {
            Debug.Log("Making new convo");
            conv = new Conversation();
            conv.refName = "managedata";
            conv.allBranches.Add(managedata);
            UIManagerScript.myDialogBoxComponent.PrepareSelectSlot(false);
            UIManagerScript.currentConversation = conv;            
        }
        else
        {
            //clear out the old, add in the new
            conv.allBranches.RemoveAll(tb => tb.branchRefName == "managedatachoice");
            conv.allBranches.Add(managedata);
        }

        UIManagerScript.SwitchConversationBranch(managedata);
        UIManagerScript.UpdateDialogBox();
    }
    Vector2 SetMainMenuDialogBoxSizeAndPosition(out Vector2 size)
    {
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                MAIN_MENU_DEFAULT_YPOS = -85f;
                break;
        }

        float yPos = MAIN_MENU_DEFAULT_YPOS;

        if (DLCManager.ShouldShowLegendOfSharaTitleScreen()
            || DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            yPos -= 90f;
        } 
        

        yPos -= 30f;

        size = new Vector2(400f, 5f);
        Vector2 pos = new Vector2(0f, yPos);

        return pos;
    }
}