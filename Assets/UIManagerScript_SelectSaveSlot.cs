using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Text;
using System.IO;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public partial class UIManagerScript
{
    static bool firstCCInitialize = true;

    public IEnumerator SelectSaveSlot(DialogButtonResponse dbr)
    {
#if UNITY_PS4
        //on PS4 it takes few seconds to load saves so we will use loading popup
        //LoadingWaiterManager.Display();
        LoadingWaiterManager._instance.TurnOnNoAnimation();
        //disable input while it loads
        TitleScreenScript.AllowInput(false);
#endif

        yield return SharedBank.ReadFromSave(readingAtTitleScreen: true);

        if (SharedBank.fatalReadError)
        {
            SharedBank.fatalReadError = false;
            SharedBank.DeleteSharedBankDueToCriticalFailure();
        }

        if (firstCCInitialize && charCreationManager != null)
        {
            charCreationManager.Initialize(firstCCInitialize);
            charCreationManager.GetComponent<CanvasGroup>().alpha = 1.0f;
            firstCCInitialize = false;
        }

        // make sure next/prev page buttons have text!
        if (string.IsNullOrEmpty(nextPageResponseButton.buttonText))
        {
            nextPageResponseButton.buttonText = StringManager.GetString("misc_nextpage");
            previousPageResponseButton.buttonText = StringManager.GetString("misc_previouspage");
        }
        TitleScreenScript.WaitingForSaveSlotsToLoad = true;

        PlayerModManager.singleton.DisableWorkshopArea();

        
        Conversation saveSelect = new Conversation();

        if (dbr == DialogButtonResponse.NEWGAME)
        {
            saveSelect.refName = "newgame";
            TitleScreenScript.reasonWeAreInSelectSlotWindow = TitleScreenScript.EWhyAreWeInTheSelectSlotWindow.because_new_game;
        }
        else if (dbr == DialogButtonResponse.MANAGEDATA)
        {
            saveSelect.refName = "managedata";
            TitleScreenScript.reasonWeAreInSelectSlotWindow = TitleScreenScript.EWhyAreWeInTheSelectSlotWindow.because_delete_game;
        }
        else
        {
            saveSelect.refName = "loadgame";
            TitleScreenScript.reasonWeAreInSelectSlotWindow = TitleScreenScript.EWhyAreWeInTheSelectSlotWindow.because_load_game;
        }

        TextBranch saveBranch = new TextBranch();
        saveBranch.text = StringManager.GetString("prompt_saveslot_newcharacter");

       if (dbr == DialogButtonResponse.MANAGEDATA)
        {
            saveBranch.text = StringManager.GetString("prompt_saveslot_clear");
        }
        else if (dbr == DialogButtonResponse.CONTINUE || dbr == DialogButtonResponse.LOADGAME)
        {
            saveBranch.text = StringManager.GetString("prompt_saveslot_continue");
        }

        for (int i = 0; i < saveSlotOpen.Length; i++)
        {
            saveSlotOpen[i] = false;
        }

        for (int i = 0; i < GameStartData.slotContainsMetaData.Length; i++)
        {
            GameStartData.slotContainsMetaData[i] = true;
            GameStartData.slotInRandomJobMode[i] = false;
        }

        // Base the special save slot buttons on our canvas/render height
        var goCanvas = GameObject.Find("DialogBox");
        float fCanvasHeight = goCanvas.GetComponent<RectTransform>().rect.height;
        float fHeightBlock = fCanvasHeight / 10.0f;

        for (int i = 0; i < GameMasterScript.kNumSaveSlots; i++)
        {
            GameMasterScript.saveDataBlockAsyncLoadOutput.Clear();

            saveDataInfoBlocks[i].Clear();

            bool anyMetaThisSlot = false;

#if UNITY_SWITCH
            string path = "savedGame" + i + ".xml";
            string metaPath = "metaprogress" + i + ".xml";
#elif UNITY_PS4
            string path = "savedGame" + i + ".xml";
            string metaPath = "metaprogress" + i + ".xml";
#elif UNITY_XBOXONE
            string path = "savedGame" + i + ".xml";
            string metaPath = "metaprogress" + i + ".xml";
#else
            string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + i + ".xml";
            string metaPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + i + ".xml";
#endif
            string buttonText = "";

            StringManager.SetTag(0, (i + 1).ToString());

            #region Load Meta Progress from File

#if UNITY_SWITCH
            if (!Switch_SaveDataHandler.GetInstance().CheckIfSwitchFileExists(metaPath))
#elif UNITY_PS4           
            if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, metaPath))
#elif UNITY_XBOXONE
            if (!XboxSaveManager.instance.HasKey(metaPath))
#else
            if (!File.Exists(metaPath))
#endif
            {
                //Debug.Log("index " + i + " " + metaPath + " doesnt exist so.");
                GameMasterScript.saveDataBlockAsyncLoadOutput.SetDataDisplayType(SaveDataDisplayBlock.ESaveDataDisplayType.empty_af);
                buttonText = StringManager.GetString("misc_saveslot_info") + " " + StringManager.GetString("misc_nodataatall") + "\n";
                GameStartData.slotContainsMetaData[i] = false;
            }
            else
            {
                anyMetaThisSlot = true;
                if (!MetaProgressScript.bufferedMetaDataDirty[i] && !string.IsNullOrEmpty(MetaProgressScript.bufferedMetaDataInAllSlots[i]))
                {
                    // Load buffered data if we can.
                    GameMasterScript.strAsyncLoadOutput = MetaProgressScript.bufferedMetaDataInAllSlots[i];
                    MetaProgressScript.ParseMetaBufferedAsyncLoadOutput(GameMasterScript.strAsyncLoadOutput);
                }
                else
                {
                    yield return MetaProgressScript.LoadCoreData(metaPath, i);
                    MetaProgressScript.bufferedMetaDataDirty[i] = false;
                    MetaProgressScript.bufferedMetaDataInAllSlots[i] = GameMasterScript.strAsyncLoadOutput;
                }

                buttonText += "\n" + GameMasterScript.strAsyncLoadOutput + "\n";
            }

            #endregion

            #region Load hero progress from file

            //Debug.Log("Check hero progress in " + path);

#if UNITY_SWITCH
            if (!Switch_SaveDataHandler.GetInstance().CheckIfSwitchFileExists(path))
#elif UNITY_PS4            
            if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, path))
#elif UNITY_XBOXONE            
            if (!XboxSaveManager.instance.HasKey(path))
#else
            if (!File.Exists(path))
#endif
            {
                if (anyMetaThisSlot)
                {
                    GameMasterScript.saveDataBlockAsyncLoadOutput.SetDataDisplayType(SaveDataDisplayBlock.ESaveDataDisplayType.no_character_but_world_exists);
                }                

                buttonText = StringManager.GetString("misc_saveslot_info") + " " + StringManager.GetString("misc_nocharacterdata");
                saveSlotOpen[i] = true;
            }
            else
            {
                GameStartData.saveGameSlot = i; // why is this line here

                string heroData = "";

                //Debug.Log("Dirty data? " + MetaProgressScript.bufferedHeroDataDirty[i] + " in slot " + i + ", data itself? " + MetaProgressScript.bufferedHeroDataInAllSlots[i]);

                if (!MetaProgressScript.bufferedHeroDataDirty[i] && !string.IsNullOrEmpty(MetaProgressScript.bufferedHeroDataInAllSlots[i]))
                {
                    heroData = MetaProgressScript.bufferedHeroDataInAllSlots[i];
                    GameMasterScript.strAsyncLoadOutput = heroData;
                    GameMasterScript.ParseHeroBufferedAsyncLoadOutput(heroData);
                }
                else
                {
                    //Debug.Log("NOT using buffered hero data.");
                    yield return GameMasterScript.LoadCoreData(path, true);
                    heroData = GameMasterScript.strAsyncLoadOutput;
                    MetaProgressScript.bufferedHeroDataInAllSlots[i] = heroData;
                    MetaProgressScript.bufferedHeroDataDirty[i] = false;
                }                              

                StringManager.SetTag(0, (i + 1).ToString());
                if (string.IsNullOrEmpty(heroData)) // This would occur if file was corrupt.
                {
                    if (Debug.isDebugBuild) Debug.Log("Data in slot " + i + " is corrupt?");
                    // If we are here, data is corrupt
                    buttonText = StringManager.GetString("misc_saveslot_info") + " " + StringManager.GetString("misc_nocharacterdata");
                    saveSlotOpen[i] = true;
                }
                else
                {
                    //Debug.Log("WE DO HAVE HERO DATA FOR SLOT " + i + " SO PLEASE SHOW IT.");
                    buttonText = StringManager.GetString("misc_saveslot_info") + " " + heroData;
                }
            }

            #endregion

            CreateSlotSelectButtonAndAddToBranch(buttonText, i, saveBranch, goCanvas, fHeightBlock, dbr);
        }

        saveBranch.branchRefName = "selectslot";

        myDialogBoxComponent.PrepareSelectSlot();

        currentConversation = saveSelect;
        SwitchConversationBranch(saveBranch);
        saveSelect.allBranches.Add(saveBranch);

        //if (Debug.isDebugBuild) Debug.Log("Preparing to update dialog box on select save slot command " + Time.realtimeSinceStartup);

        TitleScreenScript.CreateStage = CreationStages.SELECTSLOT;

        UpdateDialogBox();

        TitleScreenScript.cursorIsOnChangePages = false;

        TitleScreenScript.titleScreenSingleton.OnHoverPrefabSaveBlockSlot(0);

        TitleScreenScript.WaitingForSaveSlotsToLoad = false;

#if UNITY_PS4
        //hide loading text
        LoadingWaiterManager.Hide(0.2f);
        //allow input
        TitleScreenScript.AllowInput(true);
#endif
    }

    void CreateSlotSelectButtonAndAddToBranch(string buttonText, int index, TextBranch saveBranch, GameObject slotSelectAnchor, float fHeightBlock, DialogButtonResponse dbr)
    {
        //Debug.Log("Creating slot: " + index + " select button " + buttonText);
        //instantiate a new display block if we haven't already.
        if (saveDataDisplayComponents[index] == null)
        {
            CreateSaveDataDisplayComponent(index, slotSelectAnchor, fHeightBlock);
        }

        //Debug.Log("OUTPUT WE'RE WRITING: " + GameMasterScript.saveDataBlockAsyncLoadOutput.strHeroName + " " + GameMasterScript.saveDataBlockAsyncLoadOutput.strJobName);

        //assign stuff here, now that the information from both the hero and the meta have been loaded.
        var sddc = saveDataDisplayComponents[index];
        saveDataInfoBlocks[index] = GameMasterScript.saveDataBlockAsyncLoadOutput;

        sddc.saveInfo = saveDataInfoBlocks[index];
        sddc.SetDeleteMode(dbr == DialogButtonResponse.MANAGEDATA);
        sddc.bInfoIsDirty = true;

        // We're no longer using ButtonCombos here, but rather, the fancy slots~
        ButtonCombo slotSelect = new ButtonCombo();
        //slotSelect.buttonText = buttonText + "\n";

        //Debug.Log("Button " + index + " text would be " + buttonText);

        slotSelect.actionRef = "slot" + (index + 1);
        slotSelect.AttachSaveObjectForThisConversation(sddc);

        saveBranch.responses.Add(slotSelect); 
    }

    void CreateSaveDataDisplayComponent(int index, GameObject slotSelectAnchor, float fHeightBlock)
    {
        GameObject go = Instantiate(TitleScreenScript.PrefabSaveDataBlock);

        //assign the transform using the gui calls and not normal calls
        saveDataDisplayComponents[index] = go.GetComponent<SaveDataDisplayBlock>();
        saveDataDisplayComponents[index].slotIndex = index;

        Button[] bts = go.GetComponentsInChildren<Button>();
        for (int i = 0; i < bts.Length; i++)
        {
            Button bt = bts[i];
            bt.onClick.RemoveAllListeners();
            var local_index = index;
            bt.onClick.AddListener(() => TitleScreenScript.titleScreenSingleton.OnSelectSlotConfirmPressed(local_index)); ;
        }
        Button bt2 = go.GetComponent<Button>();
        bt2.onClick.RemoveAllListeners();
        var local_index2 = index;
        bt2.onClick.AddListener(() => TitleScreenScript.titleScreenSingleton.OnSelectSlotConfirmPressed(local_index2)); ;

        EventTrigger et = go.GetComponent<EventTrigger>();

        EventTrigger.Entry onHover = new EventTrigger.Entry();
        onHover.eventID = EventTriggerType.PointerEnter;
        onHover.callback.AddListener((eventData) => { TitleScreenScript.titleScreenSingleton.OnHoverPrefabSaveBlockSlot(local_index2); });
        et.triggers.Add(onHover);

        var rt = saveDataDisplayComponents[index].transform as RectTransform;
        rt.SetParent(slotSelectAnchor.transform, false);

        //position it down 1 height block + 2 for each object prior
        float fHeight = fHeightBlock * (1 + index * 2);

        //negative value because Unity
        rt.anchoredPosition = new Vector2(0, fHeight * -1);

        go.SetActive(false);
    }
}
