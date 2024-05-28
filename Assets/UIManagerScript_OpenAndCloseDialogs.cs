using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using TMPro;
using System.Reflection;
using System.Linq;

public partial class UIManagerScript
{
    public IEnumerator WaitThenStartConversation(Conversation convo, DialogType dType, NPC whichNPC, float time, string startingBranch = "", string[] mergeTags = null)
    {
        //Debug.Log("Wait " + time + " to start conversation " + convo.refName);
        if (dialogBoxOpen)
        {
            prevDialogBoxOpen = true;
        }
        else
        {
            prevDialogBoxOpen = false;
        }

        waitingToOpenDialog = true;
        waitingForDialog = convo;

        // Don't set dialogBoxOpen unless it REALLY is open.
        //dialogBoxOpen = true;

        yield return new WaitForSeconds(time);
        waitingToOpenDialog = false;

        if (dType == DialogType.LEVELUP && GameMasterScript.IsAnimationPlayingFromCutscene())
        {
            // Hero's levelupBoostWaiting should be ticked up, but we'll wait.
        }
        else
        {
            StartConversation(convo, dType, whichNPC, false, startingBranch, mergeTags);
        }


    }

    public static void StartConversationByRef(string refName, DialogType dType, NPC whichNPC, bool conversationStartedViaDequeue = false, string strStartingBranch = "", bool flushOtherConversations = false)
    {
        Conversation c = GameMasterScript.FindConversation(refName);
        if (c != null)
        {
            if (flushOtherConversations)
            {
                conversationQueue.Clear();
            }
            StartConversation(c, dType, whichNPC, conversationStartedViaDequeue, strStartingBranch);
        }
    }

    public static void StartConversation(Conversation convo, DialogType dType, NPC whichNPC, bool conversationStartedViaDequeue = false, string strStartingBranch = "", string[] mergeTags = null)
    {
        if (convo == null)
        {
            Debug.Log("Doesn't exist ");
            return;
        }

        if (currentConversation != null && (dialogBoxType == DialogType.GAMEOVER || dialogBoxType == DialogType.KNOCKEDOUT) || GameMasterScript.playerDied)
        {
            if (convo.refName != "gameover_ko" && convo.refName != "gameover_forreal" && convo.refName != "gameover_mysterydungeon")
            {
                // Don't let any other windows pop up
                if (Debug.isDebugBuild) Debug.Log("This is not a gameover conversation, so exit.");
                return;
            }
        }

        if (IsConversationInvalid(convo))
        {
            return;
        }

        //todo: #dialogrefactor This may not be needed any longer after switching the dialog box system over.
        //assume this is off, the branches will tell us otherwise
        //dialog_ShouldDisplayFaces = false;
        //singletonUIMS.PrepareDialogFaces(); 
        
        myDialogBoxComponent.strMusicWhenConvoStarted = GameMasterScript.musicManager.GetCurrentTrackName();

        if (convo.refName == "levelupstats")
        {
            if (GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
            {
                CloseDialogBox();
                return;
            }
            StringManager.SetTag(0, GameMasterScript.heroPCActor.myStats.GetLevel().ToString());
        }

        if (convo.refName == "openitemworld")
        {
            int orbQty = GameMasterScript.heroPCActor.myInventory.GetItemQuantity("orb_itemworld");
            if (orbQty == 0)
            {
                GameLogScript.LogWriteStringRef("log_iw_need_orb");
                UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                return;
            }
        }

        // 332019: No conversation should ever have no branches, but maybe it happened.
        if (convo.allBranches.Count == 0)
        {
            Debug.Log("No branches");
            return;
        }
        //Debug.Log("Start " + convo.refName + " " + dType);
        TextBranch start = convo.allBranches[0];

        CheckForConversationStartScript(convo);

        if (start.altBranches.Count > 0)
        {
            foreach (AlternateBranch ab in start.altBranches)
            {
                //Shep 12 Dec : All these were ever doing was exiting, so I added a shortcut here.
                if (ab.altBranchRef.ToLowerInvariant() != "exit")
                {
                    continue;
                }

                int value = GameMasterScript.heroPCActor.ReadActorData(ab.branchReqFlag);
                if (ab.branchReqFlagMeta)
                {
                    value = MetaProgressScript.ReadMetaProgress(ab.branchReqFlag);
                }
                if (value >= ab.branchReqFlagValue)
                {
                    bool valid = true;
                    if (!String.IsNullOrEmpty(ab.reqItemInInventory))
                    {
                        if (GameMasterScript.heroPCActor.myInventory.GetItemByRef(ab.reqItemInInventory) == null)
                        {
                            valid = false;
                        }
                        else
                        {
                        }
                    }
                    if (valid)
                    {
                        if (ab.altBranchRef == "exit") return;
                    }

                }
            }
        }


        // Close dialogs WAS here, but this was killing open conversations. Bad. We want to queue them.

        if (conversationQueue == null)
        {
            conversationQueue = new Queue<ConversationData>();
        }

        bool curDialogState = dialogBoxOpen;

        if (currentConversation != null && currentConversation.refName == "loadgame")
        {
            curDialogState = false;
            dialogBoxOpen = false;
            waitingToOpenDialog = false; // new 12/28. "loadgame" should never be considered here
        }

        if (waitingToOpenDialog)
        {
            curDialogState = prevDialogBoxOpen;
            //Debug.Log("Waiting to open dialog. Cur dialog state is " + curDialogState + " and we're waiting on " + waitingForDialog.refName);
            if (waitingForDialog != convo)
            {
#if UNITY_EDITOR
                Debug.Log("Existing dialog, " + currentConversation.refName + " so enqueueing " + convo.refName);
#endif
                conversationQueue.Enqueue(new ConversationData(convo, dType, whichNPC, mergeTags));
                return;
            }
        }

        // waiting to open = false WAS here...

        if (curDialogState)
        {
#if UNITY_EDITOR
            Debug.Log("Existing dialog, " + currentConversation.refName + " so enqueueing " + convo.refName);
#endif
            conversationQueue.Enqueue(new ConversationData(convo, dType, whichNPC, mergeTags));
            return;
        }

        if (convo.stopAnimationAndUnlockInput)
        {
            GameMasterScript.SetAnimationPlaying(false);
            GameMasterScript.gmsSingleton.turnExecuting = false;
        }

        if (mergeTags != null)
        {
            for (int i = 0; i < mergeTags.Length; i++)
            {
                StringManager.SetTag(i, mergeTags[i]);
            }
        }

        waitingToOpenDialog = false;

        if (convo == null)
        {
            Debug.Log("Can't start null conversation");
            return;
        }

        //Shep 26 Nov 2017 - we don't want the Drop Item dialog to close up the fullscreen UI
        if (!conversationStartedViaDequeue && dType != DialogType.COUNT)
        {
            if (convo.refName != "adjust_quantity")
            {
                singletonUIMS.CloseAllDialogs();
            }
        }

        if (convo.allBranches.Count == 0)
        {
            Debug.Log("Conversation has no text branches. " + convo.refName);
            return;
        }
        currentConversation = convo;
        currentConversation.whichNPC = whichNPC;

        if (whichNPC != null && whichNPC.actorRefName == "npc_monsterguy")
        {
            MonsterCorralScript.CloseAllInterfaces();
        }

        //Debug.Log("Start convo for real: " + currentConversation.refName);

        if (convo.refName == "cooking")
        {
            OpenCookingInterface();
            return;
        }

        if (convo.refName == "monstercorral")
        {
            // Rose petal check.
            bool foundRosePetals = false;
            foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
            {
                if (itm.itemType != ItemTypes.CONSUMABLE) continue;
                Consumable c = itm as Consumable;
                if (c.isFood && c.seasoningAttached == "spice_rosepetals")
                {
                    GameMasterScript.heroPCActor.SetActorData("romanticmeal", 1);
                    foundRosePetals = true;
                    break;
                }
            }
            if (!foundRosePetals)
            {
                GameMasterScript.heroPCActor.SetActorData("romanticmeal", 0);
            }

        }

        if (whichNPC != null)
        {
            if (whichNPC.newStuff)
            {
                //whichNPC.SetNewStuff(false);
                whichNPC.RemoveOverlays();
            }
        }

        //Shep: Ability to start from any branch
        TextBranch startingBranch = convo.allBranches[0];
        if (!string.IsNullOrEmpty(convo.strOverrideStartingBranch))
        {
            strStartingBranch = convo.strOverrideStartingBranch;
        }

        if (!string.IsNullOrEmpty(strStartingBranch))
        {
            startingBranch = convo.allBranches.FirstOrDefault(b => b.branchRefName.ToLowerInvariant() == strStartingBranch.ToLowerInvariant());
            if (startingBranch == null)
            {
                Debug.LogError("Tried to start conversaion " + convo.refName + " from a bad branch '" + strStartingBranch + "'");
                return;
            }
        }

        SwitchConversationBranch(startingBranch);

        if (string.IsNullOrEmpty(currentTextBranch.text))
        {
            Debug.Log("Null or no text in " + currentTextBranch.branchRefName + " " + convo.refName + " br " + currentTextBranch.branchRefName + " txt: " + currentTextBranch.text);
            return;
        }

        Vector2 size = convo.windowSize;
        if (size == Vector2.zero)
        {
            size.x = 1280f;
            size.y = 300f;
            // Previously, UNITY_SWITCH was set to 720. Does this matter anymore?
        }

        

        singletonUIMS.StartCoroutine(DialogBoxScript.PlayOpenDialogSoundIfNotDelayed());

        if (currentConversation.keyStory)
        {
            dType = DialogType.KEYSTORY;
        }

        myDialogBoxComponent.transform.localScale = new Vector3(DialogBoxScript.TARGET_DIALOGBOX_SCALE, 0f, DialogBoxScript.TARGET_DIALOGBOX_SCALE);

        ToggleDialogBox(dType, true, true, size, Vector2.zero);

#if UNITY_EDITOR
        UpdateDialogBox();
#else
        try { UpdateDialogBox(); }
        catch (Exception e)
        {
            Debug.Log("Failed to update/begin dialog box " + e);
            Debug.Log(convo.refName + " " + dType);
        }
#endif
        //Debug.Log("Convo force update");

        // We may have messed with the transparency when changing position, so always reset this just in case.
        myDialogBoxComponent.SetImageColor(new Color(1f, 1f, 1f, (180f / 255f)));

        if (convo.overrideSize)
        {
            myDialogBoxComponent.OverrideConversationSize(convo.windowSize);
        }
        else
        {
            myDialogBoxComponent.SetHorizontalFitType(ContentSizeFitter.FitMode.PreferredSize);
        }

        if (convo.overridePos)
        {
            myDialogBoxComponent.OverrideConversationPos(convo.windowPos);
        }
        else
        {
            myDialogBoxComponent.ResetToDefaultPosition();
        }

#if !UNITY_SWITCH
        Canvas.ForceUpdateCanvases(); // IS THIS REALLY NECESSARY? Wasting CPU cycles but the dialog code is also bad and stuff ends up outside anyway.
        myDialogBoxComponent.CalculateLayoutInputs();
#endif

        // Below two shouldn't be necessary, but things are messin' up
        singletonUIMS.EnableCursor();
        ShowDialogMenuCursor();

        if (!String.IsNullOrEmpty(convo.textInputField))
        {
            if (!PlatformVariables.SHOW_TEXT_INPUT_BOXES)
            {
                if (convo.refName == "corral_namemonster" || convo.refName == "corral_namenewmonster")
                {
                    genericTextInputField.text = MonsterManagerScript.GetRandomPetName();      
                }
            }


            ShowTextInputField(StringManager.GetString(convo.textInputField));
        }
        // If we are naming a brand new monster, pick one from the rando list
        if (convo.refName == "corral_namenewmonster")
        {
            genericTextInputPlaceholder.text = "";
            genericTextInputField.text = MonsterManagerScript.GetRandomPetName();
            if (Debug.isDebugBuild) Debug.Log("Set text to random 2");
        }

        // If there's an image requirement, read it somehow
        // #todo - data drive this...
        if (convo.refName == "corral_namemonster" || convo.refName == "corral_namenewmonster")
        {
            Actor findAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("monsterbeingnamedforcorral"));
            if (findAct != null)
            {
                Monster m = findAct as Monster;
                if (m.myAnimatable != null)
                {
                    ShowDialogBoxImage(m.myAnimatable.myAnimations[0].mySprites[0].mySprite, 2f);
                }
                else
                {
                    if (Debug.isDebugBuild) Debug.Log(m.actorRefName + " not in map, can't show it.");
                }
            }
        }
        else if (convo.refName == "legendary_found" || convo.refName == "relic_found")
        {
            Sprite itemSprite = null;
            try
            {
                int ID = GameMasterScript.gmsSingleton.DequeueTempGameData("legfoundid");
                GameMasterScript.gmsSingleton.SetTempGameData("itemidinbox", ID);
                Actor act = GameMasterScript.gmsSingleton.TryLinkActorFromDict(ID);
                Item itmFound = act as Item;
                itemSprite = itmFound.GetSpriteForUI();
            }
            catch (Exception e)
            {
                Debug.Log("Failed to find item for some reason " + GameMasterScript.gmsSingleton.DequeueTempGameData("legfoundid") + " " + e);
                itemSprite = LoadSpriteFromDict(dictItemGraphics, GameMasterScript.gmsSingleton.DequeueTempStringData("legfoundsprite"));
            }

            ShowDialogBoxImage(itemSprite, 2f);
        }

        //store the currently playing track, incase we need to restore it later        
        myDialogBoxComponent.strMusicWhenConvoStarted = GameMasterScript.musicManager.GetCurrentTrackName();
    }

    public static void Dialog_ClearQueuedConversations()
    {
        waitingToOpenDialog = false;
        conversationQueue = new Queue<ConversationData>();
    }

    static void ActivateDialogBoxAndEnableFitters()
    {
        myDialogBoxComponent.MyLayoutGroup.enabled = true;

        // Don't mess with the dialog box size if we're overriding it for any reason.
        if (currentConversation != null && !currentConversation.overrideSize)
        {
            //ContentSizeFitter csf = myDialogBoxComponent.gameObject.GetComponent<ContentSizeFitter>();

            myDialogBoxComponent.MyCSF.enabled = false;
            myDialogBoxComponent.MyCSF.enabled = true;
        }
        myDialogBoxComponent.gameObject.SetActive(true);
    }

    public static IEnumerator DialogBoxStart()
    {
        HideTextInputField();
        HideDialogBoxImage();
        ClearChildrenOnDialogBox();

        //set an image if the current textbranch has asked for one.
        if (!string.IsNullOrEmpty(currentConversation.strSpriteToDisplay))
        {
            
            var s = TDAssetBundleLoader.GetSpriteFromMemory(currentConversation.strSpriteToDisplay);
            //if tutorial image, change it depending on platform
#if UNITY_XBOXONE
            if (s.name == "tutorial_movement_main")
            {
                s = TDAssetBundleLoader.GetSpriteFromMemory("tutorial_movement_main_XB1");
            }
#endif
#if UNITY_PS4
            if (s.name == "tutorial_movement_main")
            {
                s = TDAssetBundleLoader.GetSpriteFromMemory("tutorial_movement_main_PS4");
            }
#endif
#if UNITY_ANDROID
            if (s.name == "tutorial_movement_main")
            {
                s = TDAssetBundleLoader.GetSpriteFromMemory("tutorial_movement_main_ANDROID");
            }
#endif
            ShowDialogBoxImage(s, 1f, true);
        }

        //if we need some child attached, do that too.
        if (!string.IsNullOrEmpty(currentConversation.strPrefabToDisplayInFrontOfDialog))
        {
            SetChildObjectOnDialogBox(currentConversation.strPrefabToDisplayInFrontOfDialog,
                currentConversation.vOffsetForPrefabToDisplay);
        }
        ActivateDialogBoxAndEnableFitters();
        yield return null;
        myDialogBoxComponent.StopFadeImmediatelyIfFadingOut();

        //todo: #dialogrefactor Call a text disable function from dbs.
        while (myDialogBoxComponent.IsDelayed())
        {
            yield return null;
        }

        //todo: #dialogrefacotr then turn it back on!
        //singletonUIMS.dialogBoxTextText.enabled = true;
        dialogBoxOpen = true;

        if (dialogUIObjects.Count > 0)
        {
            if (!(TitleScreenScript.CreateStage == CreationStages.SELECTSLOT && GameMasterScript.gmsSingleton.titleScreenGMS))
            {
                ChangeUIFocus(dialogUIObjects[0]);
                SwitchSelectedUIObjectGroup(uiObjectFocus.uiGroup);
            }
        }

        if (dialogUIObjects.Count < 1) // Was <= 1.
        {
            //Debug.Log("Not enough objects.");
            Canvas.ForceUpdateCanvases();
            myDialogBoxComponent.CalculateLayoutInputs();
            yield break;
        }

        SetListOffset(0);
        ShowDialogMenuCursor(); // ???

        allUIObjects.Clear();
        for (int i = 0; i < dialogUIObjects.Count; i++)
        {
            allUIObjects.Add(dialogUIObjects[i]);
        }

        if (dialogUIObjects.Count > 1)
        {
            for (int i = 0; i < dialogUIObjects.Count; i++)
            {
                if (i == 0)
                {
                    dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[dialogUIObjects.Count - 1];
                    dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[i + 1];
                }
                else if (i == dialogUIObjects.Count - 1)
                {
                    dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[i - 1];
                    dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[0];
                }
                else
                {
                    dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[i - 1];
                    dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[i + 1];
                }
            }
        }

        //our cursor may point to other side of the object
        float fXCursorOffset = -5f;

        myDialogBoxComponent.AdjustSettingsForResponsesAfterDialogBoxStart(false, ref fXCursorOffset);

        //align cursor once we know where everything is.
        if (TitleScreenScript.CreateStage != CreationStages.SELECTSLOT)
        {
            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursorPos(dialogUIObjects[0].gameObj, fXCursorOffset, -4f));
        }       

        yield return null;
        
        myDialogBoxComponent.CalculateLayoutInputs();
        dialogBoxOpen = true;

       //if (Debug.isDebugBuild) Debug.Log("Box started completely. " + currentConversation.refName + " " + dialogBoxOpen + " " + Time.realtimeSinceStartup);
    }

    public static void DialogBoxWriteNoUpdate(string text)
    {
        singletonUIMS.BeginTypewriterText(text, myDialogBoxComponent.GetDialogText()); // singletonUIMS.dialogBoxTextText);
    }

    public static void ClearDialogBoxText(bool reallyClear = false)
    {
        //todo #dialogrefactor myDialogBoxComponent.ClearDialogText();
        //singletonUIMS.dialogBoxTextText.text = "";
        if (reallyClear)
        {
            myDialogBoxComponent.GetDialogText().text = "";
        }

    }

    // Deprecate this eventually in favor of text branch system.
    public static void DialogBoxWrite(string nText)
    {
        ActivateDialogBoxAndEnableFitters();
        HideTextInputField();
        HideDialogBoxImage();
        ClearDialogBoxText();

        string text = String.Copy(nText);
        singletonUIMS.BeginTypewriterText(text, myDialogBoxComponent.GetDialogText()); // singletonUIMS.dialogBoxTextText);

        for (int i = 0; i < dialogObjects.Count; i++)
        {
            UIObject dialogUIThing = new UIObject();
            dialogUIThing.gameObj = dialogObjects[i];
            dialogUIThing.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
            dialogUIThing.onSubmitValue = i;
            dialogUIObjects.Add(dialogUIThing);
        }

        if (dialogObjects.Count > 1)
        {
            ChangeUIFocus(dialogUIObjects[0]);
            SwitchSelectedUIObjectGroup(uiObjectFocus.uiGroup);
        }

        if (dialogUIObjects.Count <= 1)
        {
            //Debug.Log("Only one dialog object, so we're ending dialog box write");

#if UNITY_SWITCH
            LayoutRebuilder.ForceRebuildLayoutImmediate(myDialogBoxComponent.GetComponent<RectTransform>());
#else
            Canvas.ForceUpdateCanvases();
            myDialogBoxComponent.CalculateLayoutInputs();
            Canvas.ForceUpdateCanvases();
#endif
            return;
        }

        allUIObjects.Clear();
        for (int i = 0; i < dialogUIObjects.Count; i++)
        {
            allUIObjects.Add(dialogUIObjects[i]);
        }

        for (int i = 0; i < dialogUIObjects.Count; i++)
        {
            if (i == 0)
            {
                dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[dialogUIObjects.Count - 1];
                dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[i + 1];
            }
            else if (i == dialogUIObjects.Count - 1)
            {
                dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[i - 1];
                dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[0];
            }
            else
            {
                dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[i - 1];
                dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[i + 1];
            }
        }

        singletonUIMS.EnableCursor();
        //Canvas.ForceUpdateCanvases();
        myDialogBoxComponent.MyLayoutGroup.enabled = false;
        myDialogBoxComponent.MyLayoutGroup.enabled = true;
        //myDialogBoxComponent.CalculateLayoutInputs();
        //Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(myDialogBoxComponent.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Waits a bit and makes sure that the active dialog box is at full y-scale post-animation. If this was called by a previous dialog box 'thread' then it does nothing.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="counter"></param>
    /// <returns></returns>
    IEnumerator WaitThenEnsureDialogBoxIsCorrectYScale(float time, int thread)
    {
        yield return new WaitForSeconds(time);

        float fXCursorOffset = -5f;        

        if (thread == dialogCounter)
        {
            // Animation should be finished, make sure yScale is correct.
            myDialogBoxComponent.transform.localScale = new Vector3(DialogBoxScript.TARGET_DIALOGBOX_SCALE, DialogBoxScript.TARGET_DIALOGBOX_SCALE, DialogBoxScript.TARGET_DIALOGBOX_SCALE);
        }
    }

    // The system for creating dialog buttons is convoluted and should be improved.
    public static void UpdateDialogBox()
    {
        CheckForConversationStartScript(currentConversation);

        dialogCounter++;
        singletonUIMS.StartCoroutine(singletonUIMS.WaitThenEnsureDialogBoxIsCorrectYScale(DIALOG_ANIM_TIME, dialogCounter));

        // Reset our response index for dialogs with multiple pages of responses
        startIndexOfCurrentResponseList = 0;

        //before we do anything, check to see if we need to start or maintain faces.
        //Conversations turn faces off when they end. So here, we check to see if we need to turn it on,
        //but don't change the state if we are moving to another branch in the same voncersation
        bool bConversationHasFaces = currentTextBranch != null && !string.IsNullOrEmpty(currentTextBranch.strFaceSprite);
        if (bConversationHasFaces)
        {
            SetDialogBoxScriptType(EDialogBoxScriptTypes.conversation_with_faces);
            if (animatingDialog)
            //if (currentDialogBoxScriptType != EDialogBoxScriptTypes.conversation_with_faces)
            {
                myDialogBoxComponent.transform.localScale = new Vector3(DialogBoxScript.TARGET_DIALOGBOX_SCALE, 0f, DialogBoxScript.TARGET_DIALOGBOX_SCALE);
            }
            myDialogBoxComponent.gameObject.SetActive(true);
        }

        TextMeshProUGUI currentTMPro = myDialogBoxComponent.GetDialogText();

        // New code has been added *on close* that sets text to transparent to avoid visual glitches
        // This just restores it to normal color upon opening the box.
        currentTMPro.color = Color.white;

        if (dialogBoxOpen)
        {
            myDialogBoxComponent.StopFadeImmediatelyIfFadingOut();
            ClearAllDialogOptions();

            OnDialogBoxUpdate_SpecialConversationCases();

            //Are we displaying faces? 
            //todo: #dialogrefactor this code is only necessary for the face based dialog
            if (bConversationHasFaces)
            {
                var dbsFace = myDialogBoxComponent as DialogBoxScript_Faces;
                dbsFace.SetFaces(currentTextBranch.strFaceSprite, currentTextBranch.optionalAnimTiming);
            }

            //Note that if we WERE displaying faces in the last branch if this conversation,
            //we will continue to do so. The face display flag is turned off at the start of conversations.

            //Check for faces before assigning typewriter text, because that changes the output object.
            if (currentTextBranch.text == "")
            {
                currentTMPro.gameObject.SetActive(false);
            }
            else
            {
                currentTMPro.gameObject.SetActive(true);
                var parsedText = singletonUIMS.BeginTypewriterText(currentTextBranch.text, currentTMPro);
                if (parsedText.Contains("<sprite"))
                {
                    currentTMPro.lineSpacing = StringManager.GetLineSpacingForTMPSpriteBasedOnLanguage();
                }
                else
                {
                    currentTMPro.lineSpacing = 0;
                }
            }

            //Are we changing music?
            if (!string.IsNullOrEmpty(currentTextBranch.strAudioCommands))
            {
                myDialogBoxComponent.HandleAudioCommands(currentTextBranch.strAudioCommands);
            }

            TryRunScriptOnCurrentBranchOpen();

            //Does this branch set a script to run at the conversation's end?
            if (!string.IsNullOrEmpty(currentTextBranch.strSetScriptOnConvoEnd))
            {
                myDialogBoxComponent.strCutsceneScriptOnConvoEnd = currentTextBranch.strSetScriptOnConvoEnd;
            }

            //sometimes we want to hide text and display an icon
            bool bDisplayOnlyMoreOrCloseIcon = false;
            if (currentTextBranch.responses.Count == 1)
            {
                switch (currentTextBranch.responses[0].buttonText)
                {
                    case "(more)":
                        myDialogBoxComponent.EnableNextIcon();
                        bDisplayOnlyMoreOrCloseIcon = true;
                        break;
                    case "(exit)":
                    case "(close)":
                        myDialogBoxComponent.EnableCloseIcon();
                        bDisplayOnlyMoreOrCloseIcon = true;
                        break;
                }
            }

            if (bDisplayOnlyMoreOrCloseIcon)
            {
                //turn on the icon
                GameObject advanceIconToDisplay = myDialogBoxComponent.GetAdvanceIcon();

                advanceIconToDisplay.transform.localScale = new Vector3(DialogBoxScript.DIALOGBOX_ICON_SCALE, DialogBoxScript.DIALOGBOX_ICON_SCALE, DialogBoxScript.DIALOGBOX_ICON_SCALE);

                //The DialogButtonScript on the next/advance objects has no typical text or image component
                DialogButtonScript btnScript = advanceIconToDisplay.GetComponent<DialogButtonScript>();

                //Create a UI shadow object, attach it to the icon, and make sure that is selected.
                UIObject dialogUIThing = new UIObject();
                dialogUIThing.gameObj = advanceIconToDisplay;
                dialogUIThing.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
                dialogUIThing.onSubmitValue = (int)DialogButtonResponse.CONTINUE;
                dialogUIThing.bHideCursorWhileFocused = true;

                //advanceIconToDisplay.GetComponent<Button>().interactable = true;
                advanceIconToDisplay.GetComponent<LayoutElement>().minHeight = 40;

                //Set the button to behave as normal, but hide the text
                dialogUIThing.button = currentTextBranch.responses[0];
                dialogUIObjects.Add(dialogUIThing);

                //if we are Delayed, take all the new stuff we just created and add it to the 
                //delay list in the box
                if (myDialogBoxComponent.IsDelayed())
                {
                    myDialogBoxComponent.AddTheseObjectsToDelay(dialogUIObjects);
                }

                //remove any padding we added, because we only have one icon to display
                myDialogBoxComponent.MyLayoutGroup.padding.bottom = 30;

                //start the dialog
                singletonUIMS.StartCoroutine(DialogBoxStart());


                //don't do the normal button display
                return;
            }

            // Moved this into a separate function for cleanliness. This function will evaluate and instantiate all responses
            // based on the current branch as needed
            CreateResponseButtonsForCurrentBranch();

            //if we are Delayed, take all the new stuff we just created and add it to the 
            //delay list in the box
            if (myDialogBoxComponent.IsDelayed())
            {
                myDialogBoxComponent.AddTheseObjectsToDelay(dialogUIObjects);
            }

            // Depending on the type of dialog box and the current language, we may need
            // to change size, position or other attributes of the box or buttons.
            myDialogBoxComponent.DoSizeAdjustmentsByCulture();

            singletonUIMS.StartCoroutine(DialogBoxStart());
        }
        else
        {
            //Debug.Log("Cannot update dialog box, as it is closed");
        }

    }

    public static void ToggleDialogBox(DialogType tType, bool on, bool overrideDefaultDialogSize)
    {
        ToggleDialogBox(tType, on, overrideDefaultDialogSize, Vector2.zero, Vector2.zero);
    }

    public static void ToggleDialogBox(DialogType tType, bool on, bool overrideDefaultDialogSize, Vector2 size, Vector2 pos)
    {
        singletonUIMS.CloseExamineMode();
        MinimapUIScript.StopOverlay();
        framesAfterDialogOpen = 0;
        typingText = false;
        dialogBoxOpen = on;
        ShowDialogMenuCursor();
        ClearAllDialogOptions();
        dialogBoxType = tType;

        if (tType == DialogType.EXIT)
        {
            singletonUIMS.DisableCursor();
            CloseDialogBox();
            if (ShopUIScript.CheckShopInterfaceState())
            {
                // Well, reopen the shop
                ShopUIScript.ReopenShop();
            }
        }

        if (dialogBoxOpen)
        {
            TDInputHandler.OnDialogOrFullScreenUIOpened();

            DisableDialogSlider();
            ActivateDialogBoxAndEnableFitters();

            // New 6/25/18 to hopefully prevent the 'flash' that occurs when the box is first opened
            // Since the y-scale smoothly lerps up to 1f in Update() we want to make sure it always starts at 0
            myDialogBoxComponent.gameObject.transform.localScale = new Vector3(1f, 0f, 1f);

            // Do we need this clear? It's causing problems with stacks of dialogue
            //ClearDialogBoxText();

            timeAtDialogStart = Time.fixedTime;
            animatingDialog = true;

            if (size == Vector2.zero)
            {
                size = new Vector2(800f, 200f);
            }
            SetDialogSize(size.x, size.y);
            SetDialogPos(pos.x, pos.y);

            // Standard dialog box fade time
            float localFadeTime = DIALOG_FADEIN_TIME;
            if (currentConversation != null) localFadeTime = currentConversation.fadeTime;
            myDialogBoxComponent.FadeIn(localFadeTime);

            float baseWaitTime = singletonUIMS.dialogWaitTime;

            if (currentConversation != null)
            {
                SpriteFontManager.SetSpriteFontForDialogBox(myDialogBoxComponent, currentConversation.spriteFontUsed);

                if (!currentConversation.centered && currentConversation.ingameDialogue)
                {
                    myDialogBoxComponent.GetDialogText().alignment = TMPro.TextAlignmentOptions.TopLeft;
                }
                else
                {
                    myDialogBoxComponent.GetDialogText().alignment = TMPro.TextAlignmentOptions.Top; // was center
                }

                if (currentConversation.extraWaitTime != 0f)
                {
                    baseWaitTime = currentConversation.extraWaitTime;
                }
            }
            else
            {
                myDialogBoxComponent.GetDialogText().alignment = TMPro.TextAlignmentOptions.Center; // was center
            }

            if (dialogUIObjects.Count > 0 && TitleScreenScript.CreateStage != CreationStages.SELECTSLOT)
            {
                singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursorPos(dialogUIObjects[0].gameObj, -5f, -4f));
            }
            singletonUIMS.EnableCursor();



            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAllowDialogConfirmation(baseWaitTime));
        }
        else
        {
            singletonUIMS.DisableCursor();
        }
    }

    public static void ClearAllDialogOptions(bool forceWaitToFadeOptions = false, bool prepForBoxToCloseCompletely = false)
    {
        myDialogBoxComponent.DisableCloseNextIcons();

        if (casinoGameOpen) return;

        if (dialogBoxOpen)
        {
            waitingToClearDialogOptions = false;
        }
        if (singletonUIMS.uiDialogMenuCursor != null)
        {
            if (playerHUD != null)
            {
                singletonUIMS.uiDialogMenuCursor.transform.SetParent(playerHUD.transform);
            }
            else
            {
                singletonUIMS.uiDialogMenuCursor.transform.SetParent(GameObject.Find("Canvas").transform);
            }
            if ((!singletonUIMS.uiHotbarNavigating) && (!AnyInteractableWindowOpen()))
            {
                singletonUIMS.DisableCursor();
            }
        }

        if (forceWaitToFadeOptions ||
                (!dialogBoxOpen &&
                myDialogBoxComponent.gameObject.activeSelf &&
                !myDialogBoxComponent.FadedOutCompletely()))
        {
            if (DialogBoxScript.bWaitingToClearOptions) return; // We're already fading right?
            //if (Debug.isDebugBuild) Debug.Log("Wait " + DIALOG_FADEOUT_TIME + " to clear dialog options.");
            DialogBoxScript.bWaitingToClearOptions = true;
            singletonUIMS.StartCoroutine(singletonUIMS.WaitToClearDialogOptions(DIALOG_FADEOUT_TIME + 0.01f));
            return;
        }

        DialogBoxScript.bWaitingToClearOptions = false;

        //Debug.Log("Dialog open state: " + dialogBoxOpen + " active? " + singletonUIMS.dialogBox.activeSelf + " faded out completely? " + singletonUIMS.dialogBox.GetComponent<DialogBoxScript>().FadedOutCompletely());

        foreach (GameObject go in dialogObjects)
        {
            if (GameMasterScript.actualGameStarted)
            {
                if (immutableDialogObjects.Contains(go)) continue;
            }
            //GameMasterScript.ReturnToStack(go, myDialogBoxComponent.GetButtonRef(), new string[] { "DialogButtonThreeColumn" });
            go.SetActive(false); // don't return to stack, try just disabling?
        }

        myDialogBoxComponent.ClearDelayState();
        dialogObjects.Clear();
        dialogUIObjects.Clear();

        //Debug.Log("SizeDelta now " + myDialogBoxComponent.GetComponent<RectTransform>().sizeDelta);
    }

    static void TryRunScriptOnCurrentBranchOpen()
    {
        //Are we running a script? This script should not actually adjust the dialog box 
        //but rather do things that accent the story, such as screen shake, flash lights, etc.
        //If you want to do something fancy on the end of the conversation, that happens via
        //strCutsceneScriptOnConvoEnd
        if (!string.IsNullOrEmpty(currentTextBranch.strScriptOnBranchOpen))
        {
            MethodInfo cutsceneMethod = CustomAlgorithms.TryGetMethod(typeof(Cutscenes), currentTextBranch.strScriptOnBranchOpen);

            //Maybe this is hiding in a DLC somewhere?
            if (cutsceneMethod == null)
            {
                cutsceneMethod = CustomAlgorithms.TryGetMethod(typeof(DLCCutscenes), currentTextBranch.strScriptOnBranchOpen);
            }
            if (cutsceneMethod == null)
            {
                Debug.LogError("Very Bad Error: trying to call cutscene method '" + currentTextBranch.strScriptOnBranchOpen + "' on branch open, but it does not exist.");
            }
            else
            {
                cutsceneMethod.Invoke(null, null);
            }
        }
    }
}

