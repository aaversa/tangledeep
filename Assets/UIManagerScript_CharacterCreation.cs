using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.IO;

public partial class UIManagerScript
{

    IEnumerator WaitThenTryStartChallenge()
    {
        yield return new WaitForSeconds(0.1f);
        while (CharCreation.randomNameList == null || CharCreation.randomNameList.Count == 0)
        {
            yield return new WaitForSeconds(0.1f);
        }
        StartChallengeGame();
    }

    void StartChallengeGame()
    {
        GameStartData.playerName = CharCreation.randomNameList[UnityEngine.Random.Range(0, CharCreation.randomNameList.Count)];
        ToggleDialogBox(DialogType.EXIT, false, false);
        GameStartData.newGame = true;
        GameStartData.ChangeGameMode(GameModes.HARDCORE);
        GameStartData.loadGameVer = GameMasterScript.GAME_BUILD_VERSION;
        UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAME);
        //singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("OPSelect");
        StartCoroutine(FadeOutThenLoadGame());
    }

    /// <summary>
    /// If we have the DLC and unlocked Shara Mode, this will send us to the campaign selector. If not,
    /// it simply begins character creation for Mirai as normal.
    /// </summary>
    private void StartCharacterCreation_OpenCampaignSelectIfNeeded()
    {
        if (Debug.isDebugBuild) Debug.Log("Consider campaign selection.");

        if (CharCreation.IsSharaCampaignAvailable())
        {
            StartCharacterCreation_ChooseCampaign();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("No campaign selection needed.");
            StartCharacterCreation_Mirai();
        }
    }
    
    public void StartCharacterCreation_Mirai()
    {
		
        MetaProgressScript.FlushAllData(GameStartData.saveGameSlot);

        if (GameStartData.currentChallengeData != null)
        {
            TitleScreenScript.AllowInput(false);
            if (CharCreation.randomNameList == null)
            {
                StartCoroutine(WaitThenTryStartChallenge());
            }
            else
            {
                StartChallengeGame();
            }
            return;
        }

        // * HARDCODED - Read this text from a file! Load all conversations from files! *
#if UNITY_SWITCH
        string metaPath ="metaprogress" + GameStartData.saveGameSlot + ".xml";
        if (Switch_SaveDataHandler.GetInstance().CheckIfSwitchFileExists(metaPath))
#elif UNITY_PS4
        string metaPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";       
        if (PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, metaPath))
#elif UNITY_XBOXONE
        string metaPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        if (XboxSaveManager.instance.HasKey(metaPath))
#else
        string metaPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";

        //Set this to zero, then check the file if there is one.
        CharCreation.totalCharacters = 0;
        if (File.Exists(metaPath))
#endif
        {
            StartCharacterCreation_DifficultyModeSelect();
            //MetaProgressScript.LoadCoreData(metaPath, GameStartData.saveGameSlot);
            // After first character, skip the story part
            //myDialogBoxComponent.GetDialogText().gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
            //CloseDialogBox();
            //StartCharacterCreation_DifficultyModeSelect();

            return;
        }

        // INTRO CINEMATIC STUFF!

        Conversation charCreation = new Conversation();
        charCreation.refName = "introstory1";
        charCreation.forceTypewriter = true;
        charCreation.ingameDialogue = true;

        TextBranch ccPage1 = new TextBranch();
        ccPage1.text = StringManager.GetString("introtext1");
        ccPage1.text = CustomAlgorithms.ParseRichText(ccPage1.text, true);

        ButtonCombo continuePage1 = new ButtonCombo();
        continuePage1.buttonText = StringManager.GetString("ui_btn_continue");
        continuePage1.dbr = DialogButtonResponse.CONTINUE;
        ccPage1.branchRefName = "gameintropart1";

        ccPage1.responses.Add(continuePage1);

        TextBranch ccPage2 = new TextBranch();
        ccPage2.text = StringManager.GetString("introtext2");
        ccPage2.text = CustomAlgorithms.ParseRichText(ccPage2.text, true);
        ccPage2.branchRefName = "gameintropart2";
        ButtonCombo continuePage2 = new ButtonCombo();
        continuePage2.buttonText = StringManager.GetString("ui_btn_continue");
        continuePage2.dbr = DialogButtonResponse.CONTINUE;

        ccPage2.responses.Add(continuePage2);

        TextBranch ccPage3 = new TextBranch();
        ccPage3.text = StringManager.GetString("introtext3");
        ccPage3.text = CustomAlgorithms.ParseRichText(ccPage3.text, true);
        ccPage3.branchRefName = "gameintropart3";
        ButtonCombo continuePage3 = new ButtonCombo();
        continuePage3.buttonText = StringManager.GetString("ui_btn_continue");
        continuePage3.dbr = DialogButtonResponse.CONTINUE;

        ccPage3.responses.Add(continuePage3);

        TextBranch ccPage4 = new TextBranch();
        ccPage4.text = StringManager.GetString("introtext4");
        ccPage4.text = CustomAlgorithms.ParseRichText(ccPage4.text, true);
        ccPage4.branchRefName = "gameintropart4";
        ButtonCombo continuePage4 = new ButtonCombo();
        continuePage4.buttonText = StringManager.GetString("ui_btn_begin");
        continuePage4.dbr = DialogButtonResponse.CREATIONSTEP2;

        ccPage4.responses.Add(continuePage4);

        continuePage1.actionRef = "gameintropart2";
        continuePage2.actionRef = "gameintropart3";
        continuePage3.actionRef = "gameintropart4";
        continuePage4.actionRef = "playernameinput";

        charCreation.allBranches.Add(ccPage1);
        charCreation.allBranches.Add(ccPage2);
        charCreation.allBranches.Add(ccPage3);
        charCreation.allBranches.Add(ccPage4);

        // END HARDCODED / TEMP STUFF
        characterCreationBG.SetActive(true);
        buildText.SetActive(false);
        switchPromoTextCG.gameObject.SetActive(false);

        //todo (did!): #dialogrefactor Call PrepareForStartCharacterCreation() that does whatever this does
        SetDialogBoxScriptType(EDialogBoxScriptTypes.@default);
        myDialogBoxComponent.PrepareForStartCharacterCreation();
        myDialogBoxComponent.GetDialogText().alignment = TMPro.TextAlignmentOptions.Top;

        currentConversation = charCreation;
        currentTextBranch = ccPage1;
        SwitchConversationBranch(ccPage1);

        UpdateDialogBox();
    }

    public void StartCharacterCreation_NameInput()
    {
        // ** ALSO HARDCODED!!! NOT GREAT! **

        HideDialogMenuCursor();

        if (SharaModeStuff.IsSharaModeActive())
        {
            //this seems important
            CharCreation.singleton.SetWorldSeed();

            GameStartData.newGame = true;            
            //here we go!
            UIManagerScript.PlayCursorSound("OPSelect");
            GameStartData.loadGameVer = GameMasterScript.GAME_BUILD_VERSION;
            singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());
            GameStartData.playerName = StringManager.GetString("npc_npc_shara_preboss3_name");
            return;
        }

        TitleScreenScript.AllowInput(true);
        TitleScreenScript.CreateStage = CreationStages.NAMEINPUT;
        ToggleDialogBox(DialogType.EXIT, false, false);
        GameStartData.playerName = "";
        CharCreation.nameInputParentCanvasGroup.gameObject.SetActive(true);
        nameInputOpen = true;

        allUIObjects.Add(CharCreation.titleScreenConfirmButton);

        //if no name, put in a random one
        if (string.IsNullOrEmpty(CharCreation.nameInputTextBox.text))
        {
            if (CharCreation.totalCharacters <= 1)
            {
                CharCreation.nameInputTextBox.text = StringManager.GetString("default_hero_name_0");
            }
            else
            {
                CharCreation.nameInputTextBox.text = CharCreation.GetRandomHeroineName();
            }

            CharCreation.nameInputTextBox.MoveToEndOfLine(false, false);
        }


        titleScreenNameInputDone = false;
        ChangeUIFocusAndAlignCursor(CharCreation.titleScreenConfirmButton);

        CharCreation.PrepareNameEntryPage();        
    }

    /// <summary>
    /// Kicks off Heroic, Hardcore, Adventure selection. If we are here, we have already confirmed overwriting existing data (if any is in the current slot)!
    /// </summary>
    public void StartCharacterCreation_DifficultyModeSelect()
    {
        TitleScreenScript.CreateStage = CreationStages.SELECTMODE;

        myDialogBoxComponent.GetTextGameObject().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);

        ignoreNextButtonConfirm = false;

        // if we're starting in Shara mode, keep the wistful title music
        if (!GameStartData.slotInSharaMode[GameStartData.saveGameSlot])
        {
            string trackName = PlatformVariables.USE_INTROLOOP ? "charcreation" : "trainingtheme";
            MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade(trackName);
        }
        
        TitleScreenScript.CreateStage = CreationStages.SELECTMODE;
        Conversation selectMode = new Conversation();
        TextBranch selectModeTB = new TextBranch();
        StringManager.GetString("ui_gamestart_selectmode");
        selectModeTB.text = StringManager.GetString("ui_gamestart_selectmode");

        //Shara has Heroic and Adventure mode, with different text.
        ButtonCombo normalMode = new ButtonCombo();
        normalMode.actionRef = "mode_heroic";

        ButtonCombo adventureMode = new ButtonCombo();
        adventureMode.actionRef = "mode_adventure";

        ButtonCombo hardcoreMode = new ButtonCombo();
        hardcoreMode.actionRef = "mode_hardcore";

        ButtonCombo randomJobMode = new ButtonCombo();
        randomJobMode.actionRef = "mode_randomjob";

        if (GameStartData.gameInSharaMode)
        {
            normalMode.buttonText = StringManager.GetString("modedescription_heroic_shara");
            adventureMode.buttonText = StringManager.GetString("modedescription_adventure_shara");

            //she doesn't have a town to lose!
            hardcoreMode = null;
            randomJobMode = null;
        }
        else
        {
            normalMode.buttonText = StringManager.GetString("modedescription_heroic");
            adventureMode.buttonText = StringManager.GetString("modedescription_adventure");
            hardcoreMode.buttonText = StringManager.GetString("modedescription_hardcore");
            randomJobMode.buttonText = StringManager.GetString("modedescription_randomjob");
        }
        
        selectModeTB.responses.Add(normalMode);
        selectModeTB.responses.Add(adventureMode);
        
        //might be null from Shara Mode
        if (hardcoreMode != null)
        {
            selectModeTB.responses.Add(hardcoreMode);
        }
        if (RandomJobMode.AllowEntryIntoRandomJobMode())
        {
            selectModeTB.responses.Add(randomJobMode);
        }

        Vector2 size = new Vector2(1200f, 120f);

        ToggleDialogBox(DialogType.CCMODESELECT, true, true, size, Vector2.zero);
        currentConversation = selectMode;
        SwitchConversationBranch(selectModeTB);
        UpdateDialogBox();

        myDialogBoxComponent.GetTextGameObject().GetComponent<LayoutElement>().minHeight = 60f; // was 120

		// For Switch, since it can be handheld, maybe we need to increase font size
#if UNITY_SWITCH
        myDialogBoxComponent.GetDialogText().fontSize = 50;        
#endif

        myDialogBoxComponent.GetDialogText().alignment = TextAlignmentOptions.Top;
        //shh! secretly clear this box
        CharCreation.nameInputTextBox.text = "";
    }

    public void StartCharacterCreation3FromButton(int dummy)
    {
        StartCharacterCreation_SelectJob();
    }

    public void StartCharacterCreation4FromButton(int dummy)
    {
        CharCreation.singleton.StartCharacterCreation_FeatSelect();
    }

    IEnumerator CheckIfAbilitiesAreLoadedBeforeGoingToCharCreation()
    {
        LoadingWaiterManager.Display(0.2f);

        while (!GameMasterScript.allAbilitiesLoaded)
        {
            TitleScreenScript.AllowInput(false);
            yield return null;
        }
        TitleScreenScript.AllowInput(true);
        LoadingWaiterManager.Hide(0.2f);
        TitleScreenScript.CreateStage = CreationStages.JOBSELECT;
        charCreationManager.BeginCharCreation_JobSelection();
    }

    public void StartCharacterCreation_SelectJob()
    {
        StartCoroutine(CheckIfAbilitiesAreLoadedBeforeGoingToCharCreation());
        return;
    }

    /// <summary>
    /// Closes the campaign select box without picking one or the other
    /// </summary>
    public void CloseCampaignSelectWithoutChoosing()
    {
        campaignSelectManager.enabled = false;
        campaignSelectManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// Allows the player to select between Shara Mode and regular good old Tangledeep
    /// </summary>
    public void StartCharacterCreation_ChooseCampaign()
    {
        TitleScreenScript.CreateStage = CreationStages.CAMPAIGNSELECT;

        GameStartData.gameInSharaMode = false;
        GameStartData.jobAsEnum = CharacterJobs.COUNT;
        GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;

        //Hide the dialog box
        CloseDialogBox();
        
        //activate campaign select
        campaignSelectManager.gameObject.SetActive(true);
        campaignSelectManager.enabled = true;
    }

    /// <summary>
    /// If we are starting a new game and select the Mirai campaign, we should offer these as normal.
    /// </summary>
    public void StartCharacterCreation_ChooseChallengesForMirai()
    {
        //This displays a Challenge dialog. If it doesn't already exist, we create it here.
        //Either way, it ends up on screen.
        
        StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());
        if (currentConversation.FindBranch("challenge") != null)
        {
            //We've already created it, so show it.
            SwitchConversationBranch(currentConversation.FindBranch("challenge"));
            UpdateDialogBox();
            return;
        }
        
        //Build it up here because it doesn't exist.
        TextBranch challengeTB = new TextBranch();
        challengeTB.branchRefName = "challenge";
        challengeTB.text = StringManager.GetString("ui_prompt_challenges");
        ButtonCombo bc = new ButtonCombo();
        bc.actionRef = "newgame";
        bc.dbr = DialogButtonResponse.EXIT;
        bc.buttonText = greenHexColor + StringManager.GetString("ui_btn_regularnewgame") + "</color>";
        challengeTB.responses.Add(bc);

        bc = new ButtonCombo();
        bc.actionRef = "weekly";
        bc.dbr = DialogButtonResponse.EXIT;
        bc.buttonText = UIManagerScript.orangeHexColor + StringManager.GetString("ui_btn_weeklychallenge") + "</color>";
        challengeTB.responses.Add(bc);

        bc = new ButtonCombo();
        bc.actionRef = "daily"; // this is wrong
        bc.dbr = DialogButtonResponse.EXIT;
        bc.buttonText = "<color=yellow>" + StringManager.GetString("ui_btn_dailychallenge") + "</color>";
        challengeTB.responses.Add(bc);

        currentConversation.allBranches.Add(challengeTB);

        //we may have gotten here via the player saying "continue" and then picking an empty slot.
        //so let's make sure we are in newgame mode.
        // AA says: But what if we're in "introstory"...?

        currentConversation.refName = "newgame";
        ToggleDialogBox(DialogType.SELECTSLOT,true,false);
        SwitchConversationBranch(challengeTB);
        UpdateDialogBox();
        ChangeUIFocusAndAlignCursor(dialogUIObjects[0]); // Make sure we default focus to "New Game"
    }

#if !UNITY_SWITCH
    void StartSolsticeNameInput()
    {
        /*
        TitleScreenScript.AllowInput(false);
        TitleScreenScript.createStage = CreationStages.NAMEINPUT;
        ToggleDialogBox(DialogType.EXIT, false, false);
        GameStartData.playerName = "";
        CharCreation.nameInputTextBox = nameInput.GetComponent<TMP_InputField>();
        CharCreation.nameInputTextBox.text = CharCreation.GetDefaultHeroineName();
        StartCharCreation4();        */
    }
#endif
}
