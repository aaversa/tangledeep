using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;
using UnityEngine.SceneManagement;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
using UnityEngine.Analytics;
#endif
using UnityEngine.UI;

public partial class UIManagerScript
{
    static int ccLayers;

    /// <summary>
    /// Returns true if RUMORS tab is open and cursor confirm was caught & processed.
    /// </summary>
    static int cursorConfirmLayers
    {
        get
        {
            return ccLayers;
        }
        set
        {
            ccLayers = value;
        }
    }
    /// <returns></returns>
    static bool TryHandleRumorTabCursorConfirmInput()
    {
        if (GetWindowState(UITabs.RUMORS))
        {
            if (uiObjectFocus != null && uiObjectFocus.mySubmitFunction != null)
            {
                try { uiObjectFocus.mySubmitFunction.Invoke(0); }    //#questionable_try_block
                catch (Exception e)
                {
                    Debug.LogWarning("Error interacting with quest interface item. " + e.ToString());
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if SHOP is open and cursor confirm was caught & processed.
    /// </summary>
    /// <returns></returns>
    static bool TryHandleShopCursorConfirmInput()
    {
        if (ShopUIScript.CheckShopInterfaceState() && !UIManagerScript.dialogBoxOpen && GameMasterScript.gmsSingleton.ReadTempGameData("dropitem") < 0)
        {
            if (uiObjectFocus != null)
            {
                //Debug.Log(uiObjectFocus.gameObj.name);
                try { uiObjectFocus.mySubmitFunction.Invoke(0); }
                catch (Exception e)
                {
                    Debug.Log("Error interacting with shop item. " + playerItemList.Count + " items in list, " + singletonUIMS.GetIndexOfSelectedButton() + " btn selected, " + listArrayIndexOffset);
                    Debug.Log(e);
                }

            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if CASINO game is being played and cursor confirm was caught & processed.
    /// </summary>
    /// <returns></returns>
    static bool TryHandleCasinoGameCursorConfirmInput()
    {
        // TODO - Differentiate between casino game options.
        if (casinoGameOpen)
        {
            int index = singletonUIMS.GetIndexOfSelectedButton();
            if (!dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].gameObj.activeSelf) // was index pos
            {
                Debug.Log("Can't confirm an inactive button.");
                return true;
            }
            selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index pos

            if (selectedButton.dbr == DialogButtonResponse.EXIT)
            {
                CloseSlotsGame();
                CloseBlackjackGame();
                return true;
            }

            staticGMSReference.PlayCasinoGame(CasinoScript.playerBet);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if cursor index exceeds number of total objects or otherwise can't find linked button
    /// </summary>
    /// <returns></returns>
    static bool IsDialogCursorConfirmOutOfOptionRange()
    {
        if (singletonUIMS.GetIndexOfSelectedButton() >= dialogUIObjects.Count)
        {
            Debug.Log("Button error.");
            if (ShopUIScript.CheckShopInterfaceState())
            {
                ShopUIScript.ReopenShop();
            }
            return true;
        }

        int bIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (bIndex < dialogUIObjects.Count)
        {
            selectedButton = dialogUIObjects[bIndex].button; // was index
        }
        else
        {
            Debug.Log("Tried confirm out of list range " + bIndex + " max " + dialogUIObjects.Count);
            if (dialogUIObjects.Count > 0)
            {
                ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs dialog script attached to button, if any. Returns true ONLY IF: a dialog event script executed AND the script told us not to continue.
    /// </summary>
    /// <returns></returns>
    static bool CheckForAndRunDialogEventScript()
    {
        if (selectedButton != null && !string.IsNullOrEmpty(selectedButton.dialogEventScript))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DialogEventsScript), selectedButton.dialogEventScript);

            if (runscript == null)
            {
                Debug.Log("VERY BAD - " + selectedButton.dialogEventScript + " no good!");
            }
            else
            {
                object[] paramList = new object[1];
                if (!string.IsNullOrEmpty(selectedButton.dialogEventScriptValue))
                {
                    paramList[0] = selectedButton.dialogEventScriptValue;
                }
                else
                {
                    paramList[0] = "nothing";
                }
                try
                {
                    bool continueDialog = (bool)(runscript.Invoke(null, paramList));
                    if (!continueDialog)
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Error with " + selectedButton.dialogEventScript + ": " + e);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if cursor input was caught and processed.
    /// </summary>
    /// <returns></returns>
    static bool CheckForFeatOrGameModSelectCursorConfirm()
    {
        selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index

        //Debug.Log(selectedButton.dbr + " " + selectedButton.actionRef);

        if (selectedButton.dbr == DialogButtonResponse.GAMEMODIFIERSELECT)
        {
            UIManagerScript.PlayCursorSound("Equip Item");
            CharCreation.singleton.StartCharacterCreation_SelectGameMods();
            return true;
        }
        else if (selectedButton.dbr == DialogButtonResponse.CREATIONSTEP2) // Back to feats.
        {
            PlayCursorSound("Skill Learnt");
            CharCreation.singleton.StartCharacterCreation_FeatSelect();
            return true;
        }
        else if (selectedButton.dbr == DialogButtonResponse.NEWGAME)
        {
            //post feats, go to name now
            ToggleDialogBox(DialogType.EXIT, false, false);
            singletonUIMS.StartCharacterCreation_NameInput();
            
            return true;

        }
        else if (selectedButton.dbr == DialogButtonResponse.TOGGLE)
        {
            if (selectedButton.actionRef == "randomfeats")
            {
                GameStartData.ClearFeats();
                string feat1 = GameStartData.allFeats[UnityEngine.Random.Range(0, GameStartData.allFeats.Count)];
                string feat2 = GameStartData.allFeats[UnityEngine.Random.Range(0, GameStartData.allFeats.Count)];
                while (feat1 == feat2)
                {
                    feat2 = GameStartData.allFeats[UnityEngine.Random.Range(0, GameStartData.allFeats.Count)];
                }
                GameStartData.AddFeat(feat1);
                GameStartData.AddFeat(feat2);
                GameStartData.newGame = true;
                PlayCursorSound("OPSelect");
                ToggleDialogBox(DialogType.EXIT, false, false);

                singletonUIMS.StartCharacterCreation_NameInput();
                
                return true;
            }

            selectedButton.toggled = !selectedButton.toggled;

            //Debug.Log("Button state is now: " + selectedButton.toggled + " fts selected? " + GameStartData.GetPlayerFeatCount() + " dbt: " + dialogBoxType);

            if (selectedButton.toggled)
            {
                if (GameStartData.GetPlayerFeatCount() == 2 && dialogBoxType != DialogType.CCGAMEMODSELECT)
                {
                    selectedButton.toggled = false;
                }
                else
                {
                            PlayCursorSound("AltSelect");
                    if (dialogBoxType == DialogType.CCGAMEMODSELECT)
                    {
                        GameStartData.AddGameModifier(selectedButton.actionRef);
                        selectedButton.buttonText = "* " + selectedButton.buttonText;
                    }
                    else
                    {
                        GameStartData.AddFeat(selectedButton.actionRef);
                        selectedButton.buttonText = UIManagerScript.greenHexColor + selectedButton.buttonText + "</color>";
                    }

                }
            }
            else
            {
                selectedButton.buttonText = selectedButton.buttonText.Replace("* ", "");
                GameStartData.RemoveFeat(selectedButton.actionRef);

                //Debug.Log("Feat count is now: " + GameStartData.playerFeats.Count);

                if (dialogBoxType == DialogType.CCGAMEMODSELECT)
                {
                    GameStartData.RemoveGameModifier(selectedButton.actionRef);
                }
                else
                {
                    selectedButton.buttonText = CustomAlgorithms.StripColors(selectedButton.buttonText);
                    GameStartData.RemoveFeat(selectedButton.actionRef);
                }
                        PlayCursorSound("AltSelect");
            }

            dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].gameObj.GetComponentInChildren<DialogButtonScript>().bodyText.text = selectedButton.buttonText; // was index
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if cursor input was caught and processed.
    /// </summary>
    /// <returns></returns>
    static bool CheckForWaypointCursorConfirm()
    {
        int wIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (wIndex >= dialogObjects.Count)
        {
            return true;
        }
        DialogButtonResponse bdbr = dialogObjects[wIndex].GetComponent<DialogButtonScript>().myResponse; // was index
        selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index

        if (selectedButton != null)
        {
            string waypointRef = selectedButton.actionRef;

            int travelToFloor = 0;

            Int32.TryParse(waypointRef, out travelToFloor);
            if (bdbr == DialogButtonResponse.CONTINUE)
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
                GameMasterScript.gmsSingleton.SetTempGameData("waypointtravel", 1);
                TravelManager.TravelFromTownToFloor(travelToFloor);
                UIManagerScript.PlayCursorSound("Mirage");
                return true;
            }

        }

        if (bdbr == DialogButtonResponse.EXIT)
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if cursor input was caught and processed.
    /// </summary>
    /// <returns></returns>
    static bool CheckForLevelupCursorConfirm()
    {
        int butIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (butIndex >= dialogUIObjects.Count)
        {
            return true;
        }
        selectedButton = dialogUIObjects[butIndex].button; // was index

        PlayCursorSound("Heavy Learn"); // was equip item

        if (currentConversation.refName.Contains("flask"))
        {
            switch (selectedButton.actionRef)
            {
                case "infuse_staminaenergy":
                    GameMasterScript.heroPCActor.SetActorData("infuse1", GameMasterScript.FLASK_HEAL_STAMINAENERGY);
                    break;
                case "infuse_attackdefense":
                    GameMasterScript.heroPCActor.SetActorData("infuse1", GameMasterScript.FLASK_BUFF_ATTACKDEF);
                    break;
                case "infuse_instantheal":
                    GameMasterScript.heroPCActor.SetActorData("infuse2", GameMasterScript.FLASK_INSTANT_HEAL);
                    break;
                case "infuse_morehp":
                    GameMasterScript.heroPCActor.SetActorData("infuse2", GameMasterScript.FLASK_HEAL_MORE);
                    break;
                case "infuse_haste":
                    GameMasterScript.heroPCActor.SetActorData("infuse3", GameMasterScript.FLASK_HASTE);
                    break;
                case "infuse_dodge":
                    GameMasterScript.heroPCActor.SetActorData("infuse3", GameMasterScript.FLASK_BUFF_DODGE);
                    break;
            }
            ToggleDialogBox(DialogType.EXIT, false, false);
            return true;
        }

        if (selectedButton.actionRef == "STRENGTH")
        {
            GameMasterScript.heroPCActor.BoostStatFromLevelup(StatTypes.STRENGTH);
            return true;
        }
        else if (selectedButton.actionRef == "SWIFTNESS")
        {
            GameMasterScript.heroPCActor.BoostStatFromLevelup(StatTypes.SWIFTNESS);
            return true;
        }
        else if (selectedButton.actionRef == "GUILE")
        {
            GameMasterScript.heroPCActor.BoostStatFromLevelup(StatTypes.GUILE);
            return true;
        }
        else if (selectedButton.actionRef == "DISCIPLINE")
        {
            GameMasterScript.heroPCActor.BoostStatFromLevelup(StatTypes.DISCIPLINE);

            return true;
        }
        else if (selectedButton.actionRef == "SPIRIT")
        {
            GameMasterScript.heroPCActor.BoostStatFromLevelup(StatTypes.SPIRIT);
            
            return true;
        }

        return false;
    }

    static void CheckForModeSelectCursorConfirm()
    {
        int ccIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (ccIndex >= dialogUIObjects.Count)
        {
            
            return;
        }
        selectedButton = dialogUIObjects[ccIndex].button;

        GameMasterScript.gmsSingleton.SetTempGameData("cc_randomjob", 0);
        RandomJobMode.preparingEntryForRandomJobMode = false;
        RandomJobMode.creatingCharacterNotInRJMode = true;

        bool randomJobMode = false;
        if (selectedButton.actionRef == "mode_adventure")
        {
            GameStartData.ChangeGameMode(GameModes.ADVENTURE);
        }
        if (selectedButton.actionRef == "mode_heroic")
        {
            GameStartData.ChangeGameMode(GameModes.NORMAL);
        }
        if (selectedButton.actionRef == "mode_hardcore")
        {
            GameStartData.ChangeGameMode(GameModes.HARDCORE);
        }
        if (selectedButton.actionRef == "mode_randomjob")
        {
            if (GameStartData.slotContainsMetaData[GameStartData.saveGameSlot] && !GameStartData.slotInRandomJobMode[GameStartData.saveGameSlot])
            {
                UIManagerScript.PlayCursorSound("Error");
                return;
            }
            GameStartData.ChangeGameMode(GameModes.NORMAL);
            GameMasterScript.gmsSingleton.SetTempGameData("cc_randomjob", 1);
            RandomJobMode.InitializeGameStartDataForRandomJobMode();
            RandomJobMode.preparingEntryForRandomJobMode = true;
            randomJobMode = true;
            RandomJobMode.creatingCharacterNotInRJMode = false;
            if (Debug.isDebugBuild) Debug.Log("<color=green>We have selected random job mode.</color>");
        }

        PlayCursorSound("Equip Item"); // was Skill Learnt

        if (SharaModeStuff.IsSharaModeActive() || randomJobMode)
        {
            CharCreation.singleton.StartCharacterCreation_FeatSelect();
        }
        else
        {
            if (PlatformVariables.ALLOW_WEB_CHALLENGES)
            {
                singletonUIMS.StartCharacterCreation_ChooseChallengesForMirai();
            }
            else
            {
                singletonUIMS.StartCharacterCreation_SelectJob();
            }
            
            
        }
        cursorConfirmLayers--;
    }

    static bool CheckForGameIntroCursorConfirm()
    {
        selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index
        if (selectedButton.dbr == DialogButtonResponse.CREATIONSTEP2)
        {
            PlayCursorSound("OPSelect");
            singletonUIMS.StartCharacterCreation_DifficultyModeSelect();
            
            return true;
        }
        else if (selectedButton.dbr == DialogButtonResponse.EXIT)
        {
            Debug.Log("Trying to quit");
            GameMasterScript.applicationQuittingOrChangingScenes = true;
            Application.Quit();
            
            return true;
        }
        else if (selectedButton.dbr == DialogButtonResponse.CONTINUE)
        {
            bool found = false;
            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (selectedButton.actionRef == currentConversation.allBranches[i].branchRefName)
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    found = true;
                    
                    break;
                }
            }
            if (!found)
            {
                Debug.Log("Cannot find text branch " + selectedButton.actionRef);
                
                return true;
            }
            UpdateDialogBox();
        }

        return false;
    }

    static bool CheckForChangeClothesCursorConfirm()
    {
        int jIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (jIndex >= dialogObjects.Count)
        {
            
            return true;
        }
        DialogButtonResponse dbr = dialogObjects[jIndex].GetComponent<DialogButtonScript>().myResponse; // was index
        if (dbr == DialogButtonResponse.EXIT)
        {
            // Do nothing
        }
        else
        {

            int jobEnumToSwitch = (int)dbr;
            jobEnumToSwitch -= 10; // really bad hacky way of doing this sorry jim
            CharacterJobData cjd = CharacterJobData.GetJobDataByEnum(jobEnumToSwitch);
            GameMasterScript.heroPCActor.selectedPrefab = cjd.prefab;

            if (cjd.prefab == "SwordDancer" && GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR]) GameMasterScript.heroPCActor.selectedPrefab = "LNY_SwordDancer";

            GameMasterScript.heroPCActor.UpdatePrefab();
        }
        ToggleDialogBox(DialogType.EXIT, false, false);
        return false;
    }

    static void CheckForExitTutorialGameOverCursorConfirm()
    {
        int index = singletonUIMS.GetIndexOfSelectedButton();
        if (index >= dialogObjects.Count)
        {
            if (index == 0)
            {
                // Probably means exit.
                ToggleDialogBox(DialogType.EXIT, false, false);
            }
            
            return;
        }
        DialogButtonResponse dbr = dialogObjects[index].GetComponent<DialogButtonScript>().myResponse; // was index
        selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index
        if (dbr == DialogButtonResponse.EXIT)
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton != null && selectedButton.actionRef == "backtotown")
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            if (currentConversation.refName != "gameover_ko" && !GameMasterScript.playerDied)
            {
                if (MapMasterScript.activeMap.dungeonLevelData.fastTravelPossible)
                {
                    // Only mark as waypoint travel IF we can fast travel back
                    // Otherwise we want to leave a portal
                    GameMasterScript.gmsSingleton.SetTempGameData("waypointtravel", 1);
                }

                TravelManager.BackToTown(false);
                UIManagerScript.PlayCursorSound("Mirage");
            }
            else
            {
                TravelManager.BackToTown(true);
            }
            
            return;
        }
        if (selectedButton != null)
        {
            if (dbr == DialogButtonResponse.WARPTOSTAIRS || selectedButton.actionRef == "warptostairs")
            {
                GameMasterScript.heroPCActor.SetActorData("sideareawarp", 0);
                ToggleDialogBox(DialogType.EXIT, false, false);

                //Look at our stairs up. If they point to the main floor, great.
                //If not, check that floor for stairs, and do so recursively until 
                //we find an exit or eat shit
                Stairs stHolder = null;
                Map destinationMap = null;
                Map searchMap = MapMasterScript.activeMap;
                while (destinationMap == null)
                {
                    //look for the stairs up
                    foreach (Stairs st in searchMap.mapStairs)
                    {
                        if (st.stairsUp)
                        {
                            stHolder = st;
                            break;
                        }
                    }

                    //if there are no stairs up, or there are but lead nowhere?
                    //RIP.
                    if (stHolder == null ||
                        stHolder.NewLocation == null)
                    {
                        
                        return;
                    }

                    //if these stairs go to the main path, sweet.
                    if (stHolder.NewLocation.IsMainPath())
                    {
                        // We don't want to set the destination map to the main path.
                        // We want to set it to the map that LEADS to the main path.

                        //destinationMap = stHolder.newLocation;
                        destinationMap = searchMap; // Which would be searchMap.
                    }
                    //otherwise, look at the map above us by following these stairs.
                    else
                    {
                        searchMap = stHolder.NewLocation;
                    }

                }

                //if the destination map is not the active map, travel to it.
                if (destinationMap != MapMasterScript.activeMap)
                {
                    TravelManager.TravelMaps(destinationMap, stHolder, false);
                    MapMasterScript.DoWaitThenMoveActor(GameMasterScript.heroPCActor, GameMasterScript.gmsSingleton.levelTransitionTime + 0.001f, stHolder.GetPos());
                }
                else
                {
                    MapMasterScript.MoveActorAndChangeCamera(GameMasterScript.heroPCActor, stHolder.GetPos());
                }

                
                return;
            }
        }

        if (selectedButton != null)
        {
            if (selectedButton.actionRef == "disabletips")
            {
                PlayerOptions.tutorialTips = false;
                ToggleDialogBox(DialogType.EXIT, false, false);
            }
            else if (selectedButton.actionRef == "BACKTOTITLE")
            {
                selectedButton.dbr = DialogButtonResponse.BACKTOTITLE;
                dbr = DialogButtonResponse.BACKTOTITLE;
            }
            else if (selectedButton.actionRef == "RESTARTGAME")
            {
                selectedButton.dbr = DialogButtonResponse.RESTARTGAME;
                dbr = DialogButtonResponse.RESTARTGAME;
            }
        }
        if (dbr == DialogButtonResponse.BACKTOTITLE)
        {
            GameMasterScript.applicationQuittingOrChangingScenes = true;
            GameMasterScript.ResetAllVariablesToGameLoad();
            GameStartData.CurrentLoadState = LoadStates.BACK_TO_TITLE;
            SceneManager.LoadScene("Main");
        }
        else if (dbr == DialogButtonResponse.RESTARTGAME)
        {
            GameStartData.newGame = true;
            GameStartData.playerJob = GameMasterScript.heroPCActor.myJob.jobEnum.ToString();
            GameStartData.playerName = GameMasterScript.heroPCActor.displayName;
            foreach (string feat in GameMasterScript.heroPCActor.heroFeats)
            {
                GameStartData.AddFeat(feat);
            }
            GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
            BossHealthBarScript.DisableBoss();
            GameStartData.CurrentLoadState = LoadStates.RESTART_SAME_CHARACTER;
            SceneManager.LoadScene("Gameplay");
        }
        if (dbr == DialogButtonResponse.LOADGAME)
        {
            staticGMSReference.TryLoadGame();            
        }
    }

    static void CheckForStandardDialogCursorConfirm()
    {
        int dIndex = singletonUIMS.GetIndexOfSelectedButton();

        if (dIndex >= dialogUIObjects.Count)
        {
            
            return;
        }
        if (dIndex < 0)
        {
            
            return;
        }
        selectedButton = dialogUIObjects[dIndex].button; // was index

        if (GameMasterScript.actualGameStarted)
        {
            PlayCursorSound("Select");
        }

        if (currentConversation.refName == "dreamcaster_modify")
        {
            if (currentTextBranch.branchRefName == "main_removemod")
            {
                if (selectedButton.dbr == DialogButtonResponse.CONTINUE)
                {
                    DialogEventsScript.RemoveModifierFromItemViaDreamcaster(selectedButton.actionRef, false);
                    
                    return;
                }
            }
            if (currentTextBranch.branchRefName == "main_extractmod")
            {
                if (selectedButton.dbr == DialogButtonResponse.CONTINUE)
                {
                    DialogEventsScript.RemoveModifierFromItemViaDreamcaster(selectedButton.actionRef, true);
                    
                    return;
                }
            }

        }
        else if (currentConversation.refName == "weaponmaster_chat")
        {
            if (selectedButton.actionRef != "exit")
            {
                if (currentConversation.FindBranch(selectedButton.actionRef) == null)
                {
                    // Must be an ability ref.
                    string skillRef = "skill_" + selectedButton.actionRef;
                    string masteryGeneric = selectedButton.actionRef.Substring(0, selectedButton.actionRef.Length - 1);
                    //Debug.Log(masteryGeneric);
                    //Debug.Log("Search for skill " + skillRef);
                    int jpCost = 0;
                    int lvl = 0;
                    if (Int32.TryParse(skillRef.Substring(skillRef.Length - 1), out lvl))
                    {
                        if (lvl == 1)
                        {
                            jpCost = 400;
                        }
                        else if (lvl == 2)
                        {
                            jpCost = 700;
                        }
                        else if (lvl == 3)
                        {
                            jpCost = 800;
                        }
                        else if (lvl == 4)
                        {
                            jpCost = 1000;
                        }
                    }
                    if (GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum] <= jpCost - 0.009f)
                    {
                        GameLogScript.GameLogWrite(redHexColor + StringManager.GetString("cant_learn_no_jp") + "</color>", GameMasterScript.heroPCActor);
                        PlayCursorSound("Error");
                        
                        return;
                    }
                    //Debug.Log("Lvl is " + lvl);
                    GameMasterScript.heroPCActor.SetActorData(masteryGeneric, lvl);

                    GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum] -= jpCost;

                    AbilityScript abilToLearn = new AbilityScript();
                    AbilityScript template = AbilityScript.GetAbilityByName(skillRef);
                    AbilityScript.CopyFromTemplate(abilToLearn, template);

                    GameLogScript.GameLogWrite(StringManager.GetString("ui_log_learnweaponmastery1"), GameMasterScript.heroPCActor);

                    GameMasterScript.heroPCActor.LearnAbility(abilToLearn, false, true, false);

                    PlayCursorSound("Heavy Learn");

                    if (lvl == 4)
                    {
                        GameMasterScript.heroPCActor.SetPlayerLearnedUltimateWeaponTechnique(true);

                        if (skillRef == "skill_whipmastery4")
                        {
                            GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_UltimateWhip_Learned();
                        }

                    }

                    GameMasterScript.gmsSingleton.statsAndAchievements.SetHighestWeaponMasteryTier(lvl);

                    UIManagerScript.HideLearnSkillIndicator();
                    if (PlayerOptions.screenFlashes)
                    {
                        UIManagerScript.FlashWhite(0.3f);
                    }

                    RefreshPlayerStats();
                    ToggleDialogBox(DialogType.EXIT, false, false);
                    
                    return;
                }

            }
        }
        else if (currentConversation.refName == "restfire")
        {
            if (selectedButton.actionRef == "startcooking")
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
                Conversation cooking = GameMasterScript.FindConversation("cooking");
                StartConversation(cooking, DialogType.STANDARD, currentConversation.whichNPC);
                
                return;
            }
        }

        if (selectedButton.actionRef == "backtotown")
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            bool backToTownState = false;
            if (GameMasterScript.playerDied || currentConversation.refName == "gameover_ko")
            {
                backToTownState = true;
            }
            TravelManager.BackToTown(backToTownState);
            
            return;
        }

        if (currentConversation.refName == "grovetree")
        {
            //Debug.Log("Is grove tree convo.");
            if (selectedButton.actionRef == "exit")
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
                
                return;
            }
            else if (selectedButton.actionRef == "choptree")
            {
                int slot = currentConversation.whichNPC.GetTreeSlot();
                if (!MetaProgressScript.treesPlanted[slot].treeComponent.alive)
                {
                    Debug.Log("ERROR: This tree is not alive but we're trying to cut it.");
                    
                    return;
                }
                MetaProgressScript.ChopTree(slot);
                GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Attack", CombatManagerScript.GetDirection(GameMasterScript.heroPCActor, MetaProgressScript.treesPlanted[slot]), GameMasterScript.heroPCActor.lastCardinalDirection);
                ToggleDialogBox(DialogType.EXIT, false, false);
                
                return;
            }
        }

        if (currentConversation.refName == "cooking")
        {
            if (selectedButton.actionRef == "exit")
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
                
                return;
            }
            string recipeName = selectedButton.actionRef;
            bool success = GameMasterScript.heroPCActor.TryCookRecipe(recipeName);
            if (success)
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
            }
            
            return;
        }

        if (currentTextBranch.branchRefName == "bankmoney")
        {
            if (selectedButton.actionRef == "confirmbank")
            {
                if (ShopUIScript.shopState == ShopState.BUY)
                {
                    // DEPOSIT
                    int amount = (int)singletonUIMS.dialogValueSlider.value;
                    if (amount == 0)
                    {
                        PlayCursorSound("Cancel");
                        ToggleDialogBox(DialogType.EXIT, false, false);
                        
                        return;
                    }
                    int depAmount = amount - 200; // 200 fee
                    GameMasterScript.heroPCActor.ChangeMoney(-1 * amount);
                    SharedBank.goldBanked += depAmount;
                    //currentConversation.whichNPC.money += depAmount;
                    PlayCursorSound("Buy Item");
                    StringManager.SetTag(0, depAmount.ToString());
                    GameLogScript.LogWriteStringRef("log_bank_deposit_money");
                    ToggleDialogBox(DialogType.EXIT, false, false);
                    
                    return;
                }
                else
                {
                    // WITHDRAW
                    int amount = (int)singletonUIMS.dialogValueSlider.value;
                    GameMasterScript.heroPCActor.ChangeMoney(amount, doNotAlterFromGameMods:true);
                    SharedBank.goldBanked -= amount;
                    //currentConversation.whichNPC.money -= amount;
                    PlayCursorSound("Buy Item");
                    StringManager.SetTag(0, amount.ToString());
                    GameLogScript.LogWriteStringRef("log_bank_withdraw_money");
                    ToggleDialogBox(DialogType.EXIT, false, false);
                    
                    return;
                }
            }
        }

        if (selectedButton.actionRef == "depositmoney")
        {
            if (GameMasterScript.heroPCActor.GetMoney() <= 200)
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
                GameLogScript.LogWriteStringRef("log_error_banker_fee");
                
                return;
            }
            ShopUIScript.shopState = ShopState.BUY;

            ClearAllDialogOptions();
            TextBranch dtb = new TextBranch();
            dtb.branchRefName = "depositmoney";

            ButtonCombo cancel = new ButtonCombo();
            cancel.buttonText = StringManager.GetString("dialog_adjust_quantity_main_btn_1");
            cancel.dbr = DialogButtonResponse.EXIT;
            cancel.actionRef = "exit";
            dtb.responses.Add(cancel);

            ButtonCombo confirm = new ButtonCombo();
            confirm.buttonText = StringManager.GetString("dialog_adjust_quantity_main_btn_0");
            confirm.dbr = DialogButtonResponse.CONTINUE;
            confirm.actionRef = "confirmbank";
            dtb.responses.Add(confirm);



            dtb.text = "";
            dtb.branchRefName = "bankmoney";
            Vector2 size = new Vector2(800f, 120f);
            Vector2 pos = new Vector2(0, 120f);
            ToggleDialogBox(DialogType.STANDARD, true, true, size, pos);
            SwitchConversationBranch(dtb);
            UpdateDialogBox();

            GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "gold");
            StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
            string prompt = StringManager.GetString("deposit_slider_query");
            EnableDialogSlider(prompt, 201, GameMasterScript.heroPCActor.GetMoney(), false);
            
            return;
        }
        else if (selectedButton.actionRef == "withdrawmoney")
        {
            ShopUIScript.shopState = ShopState.SELL;

            ClearAllDialogOptions();
            TextBranch dtb = new TextBranch();
            dtb.branchRefName = "depositmoney";
            ButtonCombo confirm = new ButtonCombo();
            confirm.buttonText = StringManager.GetString("dialog_adjust_quantity_main_btn_0");
            confirm.dbr = DialogButtonResponse.CONTINUE;
            confirm.actionRef = "confirmbank";
            dtb.responses.Add(confirm);
            dtb.text = "";
            dtb.branchRefName = "bankmoney";
            Vector2 size = new Vector2(800f, 120f);
            Vector2 pos = new Vector2(0, 120f);
            ToggleDialogBox(DialogType.STANDARD, true, true, size, pos);
            SwitchConversationBranch(dtb);
            UpdateDialogBox();

            GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "gold");

            //int maxGold = currentConversation.whichNPC.money;

            int maxGold = SharedBank.goldBanked;

            if (SharedBank.goldBanked + GameMasterScript.heroPCActor.GetMoney() >= GameMasterScript.MAX_GOLD)
            {
                maxGold = GameMasterScript.MAX_GOLD - GameMasterScript.heroPCActor.GetMoney();
            }

            EnableDialogSlider(StringManager.GetString("ui_banker_withdraw_ask"), 0, maxGold, false);
            
            return;
        }
        else if (selectedButton.actionRef == "casino_100")
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            staticGMSReference.StartCasino(CasinoScript.curGameType, 100);
            
            return;
        }
        else if (selectedButton.actionRef == "casino_1000")
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            staticGMSReference.StartCasino(CasinoScript.curGameType, 1000);
            
            return;
        }
        else if (selectedButton.actionRef == "casino_10000")
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            staticGMSReference.StartCasino(CasinoScript.curGameType, 10000);
            
            return;
        }
        else if (selectedButton.actionRef == "shop_sellcommon")
        {
            lastDialogDBRSelected = DialogButtonResponse.CONTINUE;
            lastDialogActionRefSelected = "shop_sellcommon";

            string sellAll = StringManager.GetString("confirm_sell_all_common");

            ToggleConfirmationDialog(sellAll + "\n", false, null);
            
            return;
        }
        else if (selectedButton.actionRef == "shop_sellmagical")
        {
            lastDialogDBRSelected = DialogButtonResponse.CONTINUE;
            lastDialogActionRefSelected = "shop_sellmagical";

            string sellAll = StringManager.GetString("confirm_sell_all_magical");

            ToggleConfirmationDialog(sellAll + "\n", false, null);
            
            return;
        }
        else if (selectedButton.actionRef == "buymallet")
        {
            if (GameMasterScript.heroPCActor.GetMoney() < 200)
            {
                PlayCursorSound("Error");
                
                return;
            }

            Item itm = Item.GetItemTemplateFromRef("item_monstermallet");
            Consumable nItem = new Consumable();
            nItem.CopyFromItem(itm);
            nItem.SetUniqueIDAndAddToDict();
            GameMasterScript.heroPCActor.myInventory.AddItem(nItem, true);
            GameMasterScript.heroPCActor.ChangeMoney(-200);
            GameLogScript.LogWriteStringRef("log_bought_monster_mallet");
            PlayCursorSound("Buy Item");
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        else if (selectedButton.actionRef == "viewmonsters")
        {

            if (MetaProgressScript.localTamedMonstersForThisSlot.Count == 0)
            {
                DialogBoxWriteNoUpdate(StringManager.GetString("dialog_corral_error_nomonsters"));
                ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
                singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursorPos(dialogUIObjects[0].gameObj, -5f, -4f));
                
                return;
            }
            else
            {
                CloseDialogBox();
                MonsterCorralScript.OpenCorralInterface();
            }

            return;
        }

        if (selectedButton.dbr == DialogButtonResponse.RELEASEMONSTER)
        {
            lastDialogActionRefSelected = selectedButton.actionRef;
            lastDialogDBRSelected = selectedButton.dbr;

            int monIndex = -1;

            if (Int32.TryParse(selectedButton.actionRef.Substring(1), out monIndex))
            {
                if (monIndex < MetaProgressScript.localTamedMonstersForThisSlot.Count)
                {
                    StringManager.SetTag(0, MetaProgressScript.localTamedMonstersForThisSlot[monIndex].monsterObject.displayName);
                    string confirmation = StringManager.GetString("confirm_corral_releasemonster");
                    ToggleConfirmationDialog(confirmation, false, null);
                }
                else
                {
                    Debug.Log(monIndex + " exceeds max monsters in corral");
                    
                    return;
                }

            }
            else
            {
                Debug.Log("Couldn't find monster from index " + selectedButton.actionRef);
            }

            
            return;
        }

        if (selectedButton.dbr == DialogButtonResponse.CONTINUE)
        {
            if (currentConversation.refName == "viewalltutorials")
            {
                PlayCursorSound("OPSelect");
                CloseDialogBox();
                currentConversation = null;
                string searchTut = selectedButton.actionRef;
                if (searchTut == "tutorial_jp")
                {
                    if (PlayerOptions.showControllerPrompts || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick || PlatformVariables.GAMEPAD_ONLY)
                    {
                        searchTut = "tutorial_jpcontroller";
                    }
                    else
                    {
                        searchTut = "tutorial_jpkeyboard";
                    }
                }
                StartConversation(GameMasterScript.FindConversation(selectedButton.actionRef), DialogType.TUTORIAL, null);
                
                return;
            }

            bool found = false;
            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (selectedButton.actionRef == currentConversation.allBranches[i].branchRefName)
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                //Debug.Log("Cannot find text branch " + selectedButton.actionRef);
                
                return;
            }
            UpdateDialogBox();
        }
        if (selectedButton.dbr == DialogButtonResponse.EXIT)
        {
            //Debug.Log("Exiting...");
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.SHOPBUY)
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            ShopUIScript.shopState = ShopState.BUY;
            ShopUIScript.OpenShopInterface(currentConversation.whichNPC);
            GameMasterScript.heroPCActor.visitedMerchant = true;
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.SHOPSELL)
        {
            ToggleDialogBox(DialogType.EXIT, false, false);
            ShopUIScript.shopState = ShopState.SELL;
            ShopUIScript.OpenShopInterface(currentConversation.whichNPC);
            GameMasterScript.heroPCActor.visitedMerchant = true;
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.CASINOSLOTS)
        {
            CasinoScript.curGameType = CasinoGameType.SLOTS;
            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (currentConversation.allBranches[i].branchRefName == "placebets")
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    
                    break;
                }
            }
            UpdateDialogBox();
        
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.CASINOBLACKJACK)
        {
            CasinoScript.curGameType = CasinoGameType.BLACKJACK;
            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (currentConversation.allBranches[i].branchRefName == "placebets")
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    
                    break;
                }
            }
            UpdateDialogBox();
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.CASINOCEELO)
        {
            CasinoScript.curGameType = CasinoGameType.CEELO;
            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (currentConversation.allBranches[i].branchRefName == "placebets")
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    
                    break;
                }
            }
            UpdateDialogBox();
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.BLESSATTACK)
        {
            int cost = GameMasterScript.GetBlessingCost();
            if (GameMasterScript.heroPCActor.GetMoney() < cost)
            {
                PlayCursorSound("Error");
                
                return;
            }
            GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);
            GameLogScript.LogWriteStringRef("log_percy_effect");
            GameLogScript.LogWriteStringRef("log_percy_buff");
            StatusEffect sTemplate = GameMasterScript.FindStatusTemplateByName("status_blessattack");
            StatusEffect nStatus = new StatusEffect();
            nStatus.CopyStatusFromTemplate(sTemplate);
            nStatus.curDuration = 99;
            nStatus.maxDuration = 99;
            GameMasterScript.heroPCActor.myStats.AddStatus(nStatus, GameMasterScript.heroPCActor);
            PlayCursorSound("ShamanHeal");
            if (PlayerOptions.screenFlashes)
            {
                UIManagerScript.FlashWhite(0.3f);
            }

            UIManagerScript.RefreshStatuses();
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.BLESSXP)
        {
            int cost = GameMasterScript.GetBlessingCost();
            if (GameMasterScript.heroPCActor.GetMoney() < cost)
            {
                PlayCursorSound("Error");
                
                return;
            }
            GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);
            GameLogScript.GameLogWrite(StringManager.GetString("percy_mutters"), GameMasterScript.heroPCActor);
            GameLogScript.GameLogWrite(StringManager.GetString("blessed_xp"), GameMasterScript.heroPCActor);
            StatusEffect sTemplate = GameMasterScript.FindStatusTemplateByName("status_blessxp");
            StatusEffect nStatus = new StatusEffect();
            nStatus.CopyStatusFromTemplate(sTemplate);
            nStatus.curDuration = 99;
            nStatus.maxDuration = 99;
            GameMasterScript.heroPCActor.myStats.AddStatus(nStatus, GameMasterScript.heroPCActor);
            PlayCursorSound("ShamanHeal");
            if (PlayerOptions.screenFlashes)
            {
                UIManagerScript.FlashWhite(0.3f);
            }
            UIManagerScript.RefreshStatuses();
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.BLESSJP)
        {
            int cost = GameMasterScript.GetBlessingCost();
            if (GameMasterScript.heroPCActor.GetMoney() < cost)
            {
                PlayCursorSound("Error");
                
                return;
            }
            GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);
            GameLogScript.GameLogWrite(StringManager.GetString("percy_mutters"), GameMasterScript.heroPCActor);
            GameLogScript.GameLogWrite(StringManager.GetString("blessed_jp"), GameMasterScript.heroPCActor);
            StatusEffect sTemplate = GameMasterScript.FindStatusTemplateByName("status_blessjp");
            StatusEffect nStatus = new StatusEffect();
            nStatus.CopyStatusFromTemplate(sTemplate);
            nStatus.curDuration = 99;
            nStatus.maxDuration = 99;
            GameMasterScript.heroPCActor.myStats.AddStatus(nStatus, GameMasterScript.heroPCActor);
            PlayCursorSound("ShamanHeal");
            if (PlayerOptions.screenFlashes)
            {
                UIManagerScript.FlashWhite(0.3f);
            }

            UIManagerScript.RefreshStatuses();
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.BLESSDEFENSE)
        {
            int cost = GameMasterScript.GetBlessingCost();
            if (GameMasterScript.heroPCActor.GetMoney() < cost)
            {
                PlayCursorSound("Error");
                
                return;
            }
            GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);
            GameLogScript.GameLogWrite(StringManager.GetString("percy_mutters"), GameMasterScript.heroPCActor);
            GameLogScript.GameLogWrite(StringManager.GetString("blessed_protection"), GameMasterScript.heroPCActor);
            StatusEffect sTemplate = GameMasterScript.FindStatusTemplateByName("status_blessdefense");
            StatusEffect nStatus = new StatusEffect();
            nStatus.CopyStatusFromTemplate(sTemplate);
            nStatus.curDuration = 99;
            nStatus.maxDuration = 99;
            GameMasterScript.heroPCActor.myStats.AddStatus(nStatus, GameMasterScript.heroPCActor);
            UIManagerScript.RefreshStatuses();
            singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("ShamanHeal");
            if (PlayerOptions.screenFlashes)
            {
                UIManagerScript.FlashWhite(0.3f);
            }
            ToggleDialogBox(DialogType.EXIT, false, false);
            
            return;
        }
        if (selectedButton.dbr == DialogButtonResponse.NEWQUEST)
        {
            if (currentConversation.whichNPC == null)
            {
                currentConversation.whichNPC = MapMasterScript.activeMap.FindActor("npc_questgiver") as NPC;
                if (currentConversation.whichNPC == null) // Ok she's not in the map?
                {
                    ToggleDialogBox(DialogType.EXIT, false, false);
                    
                    return;
                }
            }
            currentConversation.whichNPC.SetNewStuff(false);
            if (currentConversation.whichNPC.questsRemaining <= 0)
            {
                for (int i = 0; i < currentConversation.allBranches.Count; i++)
                {
                    if (currentConversation.allBranches[i].branchRefName == "nomorequests")
                    {
                        SwitchConversationBranch(currentConversation.allBranches[i]);
                        
                        break;
                    }
                }
                UpdateDialogBox();
                
                return;
            }
            if (GameMasterScript.heroPCActor.myQuests.Count == 3)
            {
                for (int i = 0; i < currentConversation.allBranches.Count; i++)
                {
                    if (currentConversation.allBranches[i].branchRefName == "fullquests")
                    {
                        SwitchConversationBranch(currentConversation.allBranches[i]);
                        
                        break;
                    }
                }
                UpdateDialogBox();
                
                return;
            }

            //int questCost = QuestScript.GetQuestCost();
            int questCost = 0;
            if (GameMasterScript.heroPCActor.GetMoney() < questCost)
            {
                PlayCursorSound("Error");
                
                return;
            }
            QuestScript qs = null;
            try
            {
                qs = QuestScript.CreateNewQuest();
                qs.dayReceived = MetaProgressScript.totalDaysPassed;
            }
            catch (Exception)
            {
            }
            if (qs == null)
            {
                Debug.Log("No possible quest.");
                SwitchConversationBranch(currentConversation.FindBranch("nomorequests"));
                UpdateDialogBox();
                
                return;
            }
            try { qs.GenerateQuestText(); }
            catch(Exception e)
            {
                Debug.Log("Couldnt generate quest text for " + qs.qType + " " + qs.displayName + " due to: " + e);
                Debug.Log(qs.damType + " " + qs.questText + " " + qs.rewardsText);
            }
            bufferQuest = qs;

            for (int i = 0; i < currentConversation.allBranches.Count; i++)
            {
                if (selectedButton.actionRef == currentConversation.allBranches[i].branchRefName)
                {
                    SwitchConversationBranch(currentConversation.allBranches[i]);
                    break;
                }
            }

            string text = StringManager.GetString("dialog_questgiver_town_newquest_txt") + "\n";
            text += qs.GetAllQuestText(50);
            currentTextBranch.text = text;
            UpdateDialogBox();

            currentConversation.whichNPC.questsRemaining--;
            
            return;
        }
    }

    public static void DialogCursorConfirm()
    {
        //if (Debug.isDebugBuild) Debug.Log("Confirming dialog cursor: " + MonsterCorralScript.corralInterfaceOpen + " " + UIManagerScript.dialogBoxOpen + " " + singletonUIMS.uiDialogMenuCursor.GetComponent<Image>().enabled + " " + UIManagerScript.singletonUIMS.uiDialogMenuCursor.activeSelf + " " + AnyInteractableWindowOpen());

        if (TDTouchControls.GetMouseButtonUp(1)) return;
        if (singletonUIMS.dialogInteractableDelayed) return;
        if (FadingToGame) return;

        if ((CharCreation.creationActive && GameMasterScript.actualGameStarted) || ItemWorldUIScript.itemWorldInterfaceOpen || CorralBreedScript.corralBreedInterfaceOpen
                || MonsterCorralScript.corralGroomingInterfaceOpen || MonsterCorralScript.corralInterfaceOpen ||
                MonsterCorralScript.corralFoodInterfaceOpen || MonsterCorralScript.monsterStatsInterfaceOpen)
        {
            if (uiObjectFocus != null)
            {
                try { uiObjectFocus.mySubmitFunction.Invoke(0); }
                catch (Exception e)
                {
                    Debug.Log("Error interacting with interface item. " + e);
                }

            }
            cursorConfirmLayers--;
            return;
        }

        // For these checks below, we exit out if another window/modal is absorbing our input

        if (TryHandleRumorTabCursorConfirmInput())
        {
            return;
        }

        if (TryHandleShopCursorConfirmInput())
        {
            return;
        }

        if (TryHandleCasinoGameCursorConfirmInput())
        {
            return;
        }

        if (IsDialogCursorConfirmOutOfOptionRange())
        {
            return;
        }

        if (CheckForAndRunDialogEventScript())
        {
            return;
        }

        /* if (currentConversation != null && currentTextBranch != null)
        {
            selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index
            Debug.Log("Convo: " + currentConversation.refName + " tb: " + currentTextBranch.branchRefName + " dbr " + selectedButton.dbr + " ref " + selectedButton.actionRef);
        } */

        // If we have reached this point, there is no special window active, and we have pressed some kind of response button

        switch (dialogBoxType)
        {
            case DialogType.CCJOBSELECT:
                selectedButton = dialogUIObjects[singletonUIMS.GetIndexOfSelectedButton()].button; // was index
                GameStartData.playerJob = selectedButton.dbr.ToString().ToUpperInvariant();
                CharCreation.singleton.StartCharacterCreation_FeatSelect();
                cursorConfirmLayers--;
                return;
            case DialogType.CCFEATSELECT:
            case DialogType.CCGAMEMODSELECT:

                if (CheckForFeatOrGameModSelectCursorConfirm())
                {
                    return;
                }
                break;
            case DialogType.WAYPOINT:

                if (CheckForWaypointCursorConfirm())
                {
                    return;
                }
                break;
            case DialogType.LEVELUP:

                if (CheckForLevelupCursorConfirm())
                {
                    return;
                }

                break;
            case DialogType.SELECTSLOT:

                CheckSelectSaveSlotCursorConfirm();

                break;
            case DialogType.CONFIRM:
                int cIndex = singletonUIMS.GetIndexOfSelectedButton();
                if (cIndex >= dialogUIObjects.Count)
                {
                    return;
                }
                selectedButton = dialogUIObjects[cIndex].button; // was index

                if (selectedButton.actionRef.ToLowerInvariant() == "yes") selectedButton.dbr = DialogButtonResponse.YES;
                if (selectedButton.actionRef.ToLowerInvariant() == "no") selectedButton.dbr = DialogButtonResponse.NO;


                if (!PlatformVariables.GAMEPAD_ONLY && lastDialogActionRefSelected == "switchcontrolmode")
                {
                    switch (selectedButton.dbr)
                    {
                        case DialogButtonResponse.YES:
                            PlayCursorSound("OPSelect");
                            GameMasterScript.SwitchControlMode((KeyboardControlMaps)GameMasterScript.gmsSingleton.ReadTempGameData("trykeybindmode"));
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            return;
                        case DialogButtonResponse.NO:
                            PlayCursorSound("Cancel");
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            return;
                    }
                }


                if (lastDialogActionRefSelected == "shop_sellcommon")
                {
                    switch (selectedButton.dbr)
                    {
                        case DialogButtonResponse.YES:
                            PlayCursorSound("Buy Item");
                            GameMasterScript.SellAllItems(Rarity.COMMON);
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                        case DialogButtonResponse.NO:
                            PlayCursorSound("Cancel");
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                    }
                }
                if (lastDialogActionRefSelected == "shop_sellmagical")
                {
                    switch (selectedButton.dbr)
                    {
                        case DialogButtonResponse.YES:
                            PlayCursorSound("Buy Item");
                            GameMasterScript.SellAllItems(Rarity.ANCIENT);
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                        case DialogButtonResponse.NO:
                            PlayCursorSound("Cancel");
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                    }
                }

                if (lastDialogDBRSelected == DialogButtonResponse.RELEASEMONSTER)
                {
                    switch (selectedButton.dbr)
                    {
                        case DialogButtonResponse.YES:
                            PlayCursorSound("OPSelect");
                            string mRef = lastDialogActionRefSelected.Substring(1);
                            int indexToRelease = -1;

                            if (Int32.TryParse(mRef, out indexToRelease))
                            {
                                TamedCorralMonster tcmReleased = MetaProgressScript.ReleaseMonsterFromCorral(indexToRelease);
                            }
                            else
                            {
                                Debug.Log("Couldn't find monster to release: index " + lastDialogActionRefSelected + " " + mRef);
                            }


                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                        case DialogButtonResponse.NO:
                            PlayCursorSound("Cancel");
                            ToggleDialogBox(DialogType.EXIT, false, false);
                            cursorConfirmLayers--;
                            return;
                    }
                }


                break;
            // Deprecated
            case DialogType.KNOCKEDOUT:
                int addIndex = singletonUIMS.GetIndexOfSelectedButton();
                if (addIndex >= dialogUIObjects.Count)
                {
                    cursorConfirmLayers--;
                    return;
                }
                selectedButton = dialogUIObjects[addIndex].button; // was index

                // Only one option here so...
                GameMasterScript.SetAnimationPlaying(true);
                TravelManager.BackToTownAfterKO();
                UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                cursorConfirmLayers--;
                return;

            //We have chosen one of the three difficulty modes.
            case DialogType.CCMODESELECT:

                CheckForModeSelectCursorConfirm();

                return;
            case DialogType.STANDARD:
            case DialogType.KEYSTORY:
            case DialogType.COUNT:

                CheckForStandardDialogCursorConfirm();

                break;
            case DialogType.GAMEINTRO:

                if (CheckForGameIntroCursorConfirm())
                {
                    return;
                }

                break;
            case DialogType.TITLESCREEN:
                DialogButtonResponse dbr = dialogObjects[singletonUIMS.GetIndexOfSelectedButton()].GetComponent<DialogButtonScript>().myResponse; // was index

                TitleScreenScript.HandleTitleScreenDialogInput(dbr);
                break;
            case DialogType.CHANGECLOTHES:

                if (CheckForChangeClothesCursorConfirm())
                {
                    return;
                }

                break;
            case DialogType.INTRO:
                dbr = dialogObjects[singletonUIMS.GetIndexOfSelectedButton()].GetComponent<DialogButtonScript>().myResponse; // was index
                if (dbr == DialogButtonResponse.LOADGAME)
                {
                    staticGMSReference.TryLoadGame();
                    cursorConfirmLayers--;
                    return;
                }
                if (dbr == DialogButtonResponse.BRIGAND)
                {
                    GameMasterScript.startJob = CharacterJobs.BRIGAND;
                }
                if (dbr == DialogButtonResponse.FLORAMANCER)
                {
                    GameMasterScript.startJob = CharacterJobs.FLORAMANCER;
                }
                if (dbr == DialogButtonResponse.SWORDDANCER)
                {
                    GameMasterScript.startJob = CharacterJobs.SWORDDANCER;
                }
                if (dbr == DialogButtonResponse.SPELLSHAPER)
                {
                    GameMasterScript.startJob = CharacterJobs.SPELLSHAPER;
                }
                if (dbr == DialogButtonResponse.PALADIN)
                {
                    GameMasterScript.startJob = CharacterJobs.PALADIN;
                }
                if (dbr == DialogButtonResponse.BUDOKA)
                {
                    GameMasterScript.startJob = CharacterJobs.BUDOKA;
                }
                if (dbr == DialogButtonResponse.HUNTER)
                {
                    GameMasterScript.startJob = CharacterJobs.HUNTER;
                }
                if (dbr == DialogButtonResponse.GAMBLER)
                {
                    GameMasterScript.startJob = CharacterJobs.GAMBLER;
                }
                if (dbr == DialogButtonResponse.HUSYN)
                {
                    GameMasterScript.startJob = CharacterJobs.HUSYN;
                }
                if (dbr == DialogButtonResponse.SOULKEEPER)
                {
                    GameMasterScript.startJob = CharacterJobs.SOULKEEPER;
                }
                if (dbr == DialogButtonResponse.EDGETHANE)
                {
                    GameMasterScript.startJob = CharacterJobs.EDGETHANE;
                }
                if (dbr == DialogButtonResponse.WILDCHILD)
                {
                    GameMasterScript.startJob = CharacterJobs.WILDCHILD;
                }
                PlayCursorSound("OPSelect");
                ToggleDialogBox(DialogType.EXIT, false, false);
                break;
            case DialogType.EXIT:
            case DialogType.TUTORIAL:
            case DialogType.GAMEOVER:

                CheckForExitTutorialGameOverCursorConfirm();

                break;
        }
    }

    static void TrySetSaveGameSlotFromButtonPress()
    {
        for (int i = 0; i < GameMasterScript.kNumSaveSlots; i++)
        {
            if (selectedButton.actionRef == "slot" + (i + 1))
            {
                GameStartData.saveGameSlot = i;
                break;
            }
        }
    }

    /// <summary>
    /// Returns TRUE if the current prompt is reminding player of invalid mods, and player has selected YES or NO
    /// </summary>
    /// <returns></returns>
    static bool CheckForInvalidModPromptCursorConfirm()
    {
        if (currentTextBranch.branchRefName == "confirm_mods_invalid")
        {
            switch (selectedButton.actionRef)
            {
                case "yes":
                    PlayCursorSound("OPSelect");
                    singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());
                    break;
                case "no":
                    TitleScreenScript.ReturnToMenu();
                    break;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if the current dialog is about managing save data, and player has pushed confirm. Can select Clear Character or Clear All, or exit.
    /// </summary>
    /// <returns></returns>
    static bool CheckForManageDataCursorConfirm()
    {
        if (currentTextBranch.branchRefName == "managedatachoice")
        {
            // Player is backing out of Manage Data dialog
            if (selectedButton.actionRef == "exit")
            {
                PlayCursorSound("Cancel");
                TitleScreenScript.ReturnToMenu();
                return true;
            }

            // Set up a branch that will make sure the player is SURE they want to delete data.

            TextBranch tbRemove = null;
            foreach (TextBranch tb in currentConversation.allBranches)
            {
                if (tb.branchRefName == "confirmcleardata")
                {
                    tbRemove = tb;
                }
            }
            if (tbRemove != null)
            {
                currentConversation.allBranches.Remove(tbRemove);
            }

            // Create the yes / no confirmation dialog.
            TextBranch confirm = new TextBranch();

            ButtonCombo yes = new ButtonCombo();
            yes.actionRef = selectedButton.actionRef;

            if (selectedButton.actionRef == "clearcharacterdata")
            {
                confirm.text = StringManager.GetString("ui_confirm_clear_cdata");
                StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());
                yes.buttonText = StringManager.GetString("desc_simple_upperyes") + StringManager.GetString("ui_confirmresponse_clear_cdata");
            }
            else
            {
                confirm.text = StringManager.GetString("ui_confirm_clear_alldata");
                StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());
                yes.buttonText = StringManager.GetString("desc_simple_upperyes") + StringManager.GetString("ui_confirmresponse_clear_alldata");
            }
            confirm.branchRefName = "confirmcleardata";
            ButtonCombo no = new ButtonCombo();
            no.buttonText = StringManager.GetString("button_donot_clear");
            no.actionRef = "exit";

            confirm.responses.Add(no);
            confirm.responses.Add(yes);
            currentConversation.allBranches.Add(confirm);
            SwitchConversationBranch(confirm);
            UpdateDialogBox();
            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursor(0.01f, dialogUIObjects[0]));

            // We have now switched to a confirmation dialogue asking if the player is SURE they want to clear character/all
            // data from the selected save slot.
            return true;
        }

        return false;
    }

    /// <summary>
    /// Begins campaign select OR starts New Game+
    /// </summary>
    static void SaveSlotCursorConfirmYesToStartGame(bool savage)
    {
        //if (Debug.isDebugBuild) Debug.Log("Save slot confirm yes savage? " + savage);

        if (currentTextBranch.branchRefName != "ngplus")
        {
            singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("OPSelect"); // Redundant?
            singletonUIMS.StartCharacterCreation_OpenCampaignSelectIfNeeded();
        }
        else
        {
            // Load game, but as NG Plus            
            if (savage)
            {
                GameStartData.NewGamePlus = 2;
            }
            else
            {
                GameStartData.NewGamePlus = 1;
            }

            if (Debug.isDebugBuild) Debug.Log("Starting game as new game plus. " + GameStartData.NewGamePlus);

            SetGlobalResponse(DialogButtonResponse.NEWGAMEPLUS);
            singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());
        }
    }

    /// <summary>
    /// Clears all character data in the selected save slot, then returns to menu. Player has already confirmed this is OK.
    /// </summary>
    static void ClearCharacterDataInSelectedSaveSlot()
    {
#if UNITY_SWITCH
                        {
            Switch_SaveDataHandler sdh = Switch_SaveDataHandler.GetInstance();
                            sdh.DeleteSwitchDataFile("savedGame" + GameStartData.saveGameSlot + ".xml");
                            sdh.DeleteSwitchDataFile("savedMap" + GameStartData.saveGameSlot + ".dat");
                        }
#elif UNITY_PS4
        {
            
            PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml");
            PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedMap" + GameStartData.saveGameSlot + ".dat");
        }
#elif UNITY_XBOXONE
        {
            XboxSaveManager.instance.DeleteKey("savedGame" + GameStartData.saveGameSlot + ".xml");
            XboxSaveManager.instance.DeleteKey("savedMap" + GameStartData.saveGameSlot + ".dat");
            XboxSaveManager.instance.Save();
        }
#else
        {
            string clearPath = "";
            clearPath = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
            File.Delete(clearPath);
            clearPath = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";
            File.Delete(clearPath);
        }
#endif
        SharedBank.OnFileDeleted(GameStartData.saveGameSlot);

        GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;
        GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot].Clear();

        MetaProgressScript.FlushAllData(GameStartData.saveGameSlot);
        MetaProgressScript.bufferedHeroDataDirty[GameStartData.saveGameSlot] = true;
        PlayCursorSound("OPSelect"); // Redundant?
        TitleScreenScript.ReturnToMenu();
    }

    /// <summary>
    /// Clears ALL DATA in the selected save slot, then returns to menu. Player has already confirmed this is OK.
    /// </summary>
    static void ClearAllDataInSelectedSaveSlot()
    {
#if UNITY_SWITCH
                        
        Switch_SaveDataHandler sdh = Switch_SaveDataHandler.GetInstance();
        sdh.DeleteSwitchDataFile("savedGame" + GameStartData.saveGameSlot + ".xml");
        sdh.DeleteSwitchDataFile("savedMap" + GameStartData.saveGameSlot + ".dat");
        sdh.DeleteSwitchDataFile("metaprogress" + GameStartData.saveGameSlot + ".xml");
                        
#elif UNITY_PS4       
        PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml");
        PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedMap" + GameStartData.saveGameSlot + ".dat");
        PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "metaprogress" + GameStartData.saveGameSlot + ".xml");
#elif UNITY_XBOXONE        
        XboxSaveManager.instance.DeleteKey("savedGame" + GameStartData.saveGameSlot + ".xml");
        XboxSaveManager.instance.DeleteKey("savedMap" + GameStartData.saveGameSlot + ".dat");
        XboxSaveManager.instance.DeleteKey("metaprogress" + GameStartData.saveGameSlot + ".xml");
        XboxSaveManager.instance.Save();
#else
        {
            string clearPath = "";
            clearPath = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
            File.Delete(clearPath);
            clearPath = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";
            File.Delete(clearPath);
            clearPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";
            File.Delete(clearPath);

            GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;
            GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot].Clear();
        }
#endif

        SharedBank.OnFileDeleted(GameStartData.saveGameSlot);

        MetaProgressScript.FlushAllData(GameStartData.saveGameSlot);
        MetaProgressScript.bufferedHeroDataDirty[GameStartData.saveGameSlot] = true;
        MetaProgressScript.bufferedMetaDataDirty[GameStartData.saveGameSlot] = true;
        PlayCursorSound("OPSelect"); // Redundant?
        TitleScreenScript.ReturnToMenu();
    }

    /// <summary>
    /// Returns to previous menu from new game selection; campaign select, or save slot select.
    /// </summary>
    static void SaveSlotCursorConfirmNoAndGoBack()
    {
        if (currentTextBranch.branchRefName == "ngplus")
        {
            singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("OPSelect"); // Redundant?
            singletonUIMS.StartCharacterCreation_OpenCampaignSelectIfNeeded();
            return;
        }
        else
        {
            singletonUIMS.StartCoroutine(singletonUIMS.SelectSaveSlot(DialogButtonResponse.NEWGAME));
            PlayCursorSound("Cancel");
        }
    }

    /// <summary>
    /// Checks if we are in the manage data save slot selection. If so, initiates prompt to ask player HOW they want to clear data (character or all)
    /// </summary>
    static bool CheckForManageDataConfirmPopupCursorConfirm()
    {
        if (currentConversation.refName == "managedata")
        {
            // Now select the choice...
            if (!GameStartData.slotContainsMetaData[GameStartData.saveGameSlot])
            {
                PlayCursorSound("Error");
                return true;
            }
            PlayCursorSound("OPSelect");

            TextBranch remove = null;

            foreach (TextBranch tb in currentConversation.allBranches)
            {
                if (tb.branchRefName == "managedatachoice")
                {
                    remove = tb;
                    break;
                }
            }

            if (remove != null)
            {
                currentConversation.allBranches.Remove(remove);
            }

            TextBranch managedata = new TextBranch();
            managedata.text = StringManager.GetString("ui_cleardata_types");
            managedata.branchRefName = "managedatachoice";

            StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());
            // #todo - Localization
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
            currentConversation.allBranches.Add(managedata);

            currentConversation.extraWaitTime = 0.5f;

            SwitchConversationBranch(currentConversation.FindBranch("managedatachoice"));
            UpdateDialogBox();
            HideDialogMenuCursor();
            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursorToSelectedObject(0.05f));
            return true;
        }

        return false;
    }

    /// <summary>
    ///  Returns TRUE if we have selected an EMPTY save slot, sending the player into the campaign (etc) select process.
    /// </summary>
    /// <returns></returns>
    static bool CheckForBeginNewGameSequenceInEmptySlotCursorConfirm()
    {
#if UNITY_SWITCH
        if (!Switch_SaveDataHandler.GetInstance().CheckIfSwitchFileExists("savedGame" + GameStartData.saveGameSlot + ".xml"))
#elif UNITY_PS4       
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml"))
#elif UNITY_XBOXONE
        if (!XboxSaveManager.instance.HasKey("savedGame" + GameStartData.saveGameSlot + ".xml"))
#else
        string loadPath = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
        if (!File.Exists(loadPath))
#endif
        {
            // Trying to continue a slot with no save? Just do new game.

            PlayCursorSound("OPSelect"); // Redundant?
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAME);
            singletonUIMS.StartCharacterCreation_OpenCampaignSelectIfNeeded();

            //singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("Error"); // Don't always do this
            return true;
        }

        return false;
    }


    /// <summary>
    /// Returns TRUE when loading a save file, but expansions are missing: player is presented with a warning prompt
    /// </summary>
    /// <returns></returns>
    static bool CheckForPromptInvalidDLCConfirmationCursorConfirm()
    {
#if UNITY_SWITCH // Perhaps consoles ALWAYS have DLC? On Steam, maybe DLC could be uninstalled.
        return true;
#endif

        string notFound = DLCManager.GetDLCNotFound(GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot]);
        if (notFound != "")
        {
            StringManager.SetTag(0, notFound);
            // Uh oh, mod mismatch, better let the player know before they continue.
            if (currentConversation.FindBranch("dlc_invalid") == null)
            {
                TextBranch invalidDLC = new TextBranch();
                invalidDLC.branchRefName = "dlc_invalid";
                StringManager.SetTag(1, StringManager.GetString("ui_misc_dlcmissing_warning"));
                invalidDLC.text = StringManager.GetString("dialog_dlc_disabled_loadwarning_text");

                ButtonCombo no = new ButtonCombo();
                no.actionRef = "no";
                no.dbr = DialogButtonResponse.BACKTOTITLE;
                no.buttonText = StringManager.GetString("button_backtomenu");
                invalidDLC.responses.Add(no);

                currentConversation.allBranches.Add(invalidDLC);
            }
            SwitchConversationBranch(currentConversation.FindBranch("dlc_invalid"));
            UpdateDialogBox();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE when loading a save file, but mods are missing: player is presented with a warning/confirmation prompt
    /// </summary>
    /// <returns></returns>
    static bool CheckForPromptInvalidModConfirmationCursorConfirm()
    {
        string notFound = PlayerModManager.GetModsNotFound(GameStartData.modsEnabledPerSlot[GameStartData.saveGameSlot]);
        if (!string.IsNullOrEmpty(notFound))
        {
            StringManager.SetTag(0, notFound);
            // Uh oh, mod mismatch, better let the player know before they continue.
            if (currentConversation.FindBranch("confirm_mods_invalid") == null)
            {
                TextBranch invalidMods = new TextBranch();
                invalidMods.branchRefName = "confirm_mods_invalid";
                StringManager.SetTag(1, StringManager.GetString("ui_misc_modmissing_warning"));
                invalidMods.text = StringManager.GetString("dialog_playermods_disabled_loadwarning_text");
                ButtonCombo yes = new ButtonCombo();
                yes.actionRef = "yes";
                yes.dbr = DialogButtonResponse.LOADGAME;
                yes.buttonText = StringManager.GetString("button_continue_game_no_mods");
                ButtonCombo no = new ButtonCombo();
                no.actionRef = "no";
                no.dbr = DialogButtonResponse.BACKTOTITLE;
                no.buttonText = StringManager.GetString("button_backtomenu");
                invalidMods.responses.Add(no);
                invalidMods.responses.Add(yes);
                currentConversation.allBranches.Add(invalidMods);
            }
            SwitchConversationBranch(currentConversation.FindBranch("confirm_mods_invalid"));
            UpdateDialogBox();
            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenAlignCursorToSelectedObject(0.05f));
            return true;
        }

        return false;
    }

    static bool CheckForStartNewGameInExistingSlotCursorConfirm_SetupChallenges()
    {
        //GameStartData.Initialize();
        StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString());
        if (currentConversation.FindBranch("challenge") != null)
        {
            SwitchConversationBranch(currentConversation.FindBranch("challenge"));
            UpdateDialogBox();
            return true;
        }

        return false;
    }
    static bool CheckForStartNewGameInExistingSlotCursorConfirm()
    {
        //if (Debug.isDebugBuild) Debug.Log("Boop 3 Check for start new game");

        //Debug.Log("Check for start new game in existing slot");
        if (CheckForPromptInvalidDLCConfirmationCursorConfirm())
        {
            return true;
        }	

        if (!saveSlotOpen[GameStartData.saveGameSlot])
        {

            if (Debug.isDebugBuild) Debug.Log("Selected slot " + GameStartData.saveGameSlot + " beat state? " + GameStartData.beatGameStates[(int)GameStartData.saveGameSlot] + " NGP level? " + GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot]);

            if (!GameStartData.beatGameStates[(int)GameStartData.saveGameSlot])
            {
                StringManager.SetTag(0, (GameStartData.saveGameSlot + 1).ToString()); // not sure why we have to do this

                currentConversation.RemoveBranchByRef("confirm");

                TextBranch overwriteCharData = new TextBranch();
                if (GameStartData.currentChallengeData == null)
                {
                    StringManager.SetTag(4, StringManager.GetString("ui_metaprogress_noerase"));
                }
                else
                {
                    StringManager.SetTag(4, StringManager.GetString("ui_metaprogress_erase"));
                }

                overwriteCharData.text = StringManager.GetString("warning_existingdata");
                overwriteCharData.branchRefName = "confirm";
                ButtonCombo yes = new ButtonCombo();
                yes.buttonText = StringManager.GetString("prompt_confirm_overwrite");
                yes.actionRef = "yes_overwrite";
                ButtonCombo no = new ButtonCombo();
                no.buttonText = StringManager.GetString("prompt_selectanotherslot");
                no.actionRef = "no";

                overwriteCharData.responses.Add(yes);
                overwriteCharData.responses.Add(no);
                currentConversation.allBranches.Add(overwriteCharData);

                SwitchConversationBranch(currentConversation.FindBranch("confirm"));
            }
            else // we have beaten the game
            {
                TextBranch ngPlusBranch = new TextBranch();

                string confirmText = "";
                string startInNextDifficultyText = StringManager.GetString("ui_prompt_delete_startnewgameplus");

                if (Debug.isDebugBuild) Debug.Log("Checking " + GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] + " for " + GameStartData.saveGameSlot);

                switch (GameStartData.saveSlotNGP[GameStartData.saveGameSlot])
                {
                    case 0: // regular game defeated
                        confirmText = StringManager.GetString("newgameplus_option_new");                        
                        break;
                    case 1: // ng+  defeated
                        confirmText = StringManager.GetString("newgameplus_option_existing");
                        startInNextDifficultyText = StringManager.GetString("ui_prompt_delete_savage");
                        confirmText = StringManager.GetString("savagemode_option_new"); 
                        break;
                    case 2: // ng++ (savage) defeated
                        startInNextDifficultyText = StringManager.GetString("ui_prompt_delete_savage");
                        confirmText = StringManager.GetString("savagemode_option_existing");
                        break;
                }


                ngPlusBranch.text = confirmText;
                ngPlusBranch.branchRefName = "ngplus";

                ButtonCombo noNG = new ButtonCombo();
                noNG.buttonText = StringManager.GetString("ui_prompt_delete_startnewgame");
                noNG.actionRef = "no";

                ButtonCombo ngPlus = null;

                if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] >= 1) // && Debug.isDebugBuild)
                {
                    ngPlus = new ButtonCombo();
                    ngPlus.buttonText = StringManager.GetString("ui_prompt_delete_startnewgameplus");
                    ngPlus.actionRef = "yes";
                }

                ButtonCombo yesNG = new ButtonCombo();
                yesNG.buttonText = startInNextDifficultyText;
                yesNG.actionRef = "yes";

                if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] >= 1) // && Debug.isDebugBuild)
                {
                    yesNG.actionRef = "savage";
                }



                ngPlusBranch.responses.Add(yesNG);

                if (ngPlus != null)
                {
                    if (GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] >= 2) // && Debug.isDebugBuild)
                    {
                        // don't allow NG+ here
                    }
                    else
                    {
                        ngPlusBranch.responses.Add(ngPlus);
                    }                    
                }

                ngPlusBranch.responses.Add(noNG);
                currentConversation.RemoveBranchByRef("ngplus");
                currentConversation.allBranches.Add(ngPlusBranch);
                SwitchConversationBranch(currentConversation.FindBranch("ngplus"));
            }

            UpdateDialogBox();
            ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
            Debug.Log("Done?");
            return true;
        }

        return false;
    }

    /// <summary>
    ///  This function runs when we have selected NEW GAME in a save slot. The slot might be empty or not. This is ALSO CALLED when we are in CHALLENGE selection.
    /// </summary>
    /// <returns></returns>
    static bool CheckForSelectNewGameCursorConfirm()
    {
        if (currentConversation.refName == "newgame")
        {
            PlayCursorSound("Select"); // was OPSelect

            if (TitleScreenScript.webChallengesLoaded && currentTextBranch.branchRefName != "challenge")
            {
                if (selectedButton.dbr == DialogButtonResponse.EXIT && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) // We must have simply selected a save slot.
                {
                    // Returns TRUE if we're starting a new game in a save slot WITH DATA. We could get a prompt to overwrite, OR a prompt for NG+.
                    if (CheckForStartNewGameInExistingSlotCursorConfirm())
                    {
                        return true;
                    }

                    //from here, we open the campaign select. If we choose Mirai, we'll jump to
                    //challenge mode select.

                    singletonUIMS.StartCharacterCreation_OpenCampaignSelectIfNeeded();
                    return true;
                }
            }
            else if (currentTextBranch.branchRefName == "challenge")
            {
                if (selectedButton.actionRef == "weekly")
                {
                    GameStartData.SetCurrentChallengeData(ChallengesAndLeaderboards.weeklyChallenge);
                    //singletonUIMS.StartCharacterCreation_Mirai();
                    //return true;
                }
                else if (selectedButton.actionRef == "daily")
                {
                    GameStartData.SetCurrentChallengeData(ChallengesAndLeaderboards.dailyChallenge);
                    //singletonUIMS.StartCharacterCreation_Mirai();
                    //return true;
                }

                // Didn't pick a challenge, so let's do difficulty mode select.
                //singletonUIMS.StartCharacterCreation_DifficultyModeSelect(false);
                //return true;
            }

            // Returns TRUE if we're starting a new game in a save slot WITH DATA. We could get a prompt to overwrite, OR a prompt for NG+.
            if (CheckForStartNewGameInExistingSlotCursorConfirm())
            {
                return true;
            }

            if (selectedButton.actionRef == "weekly" || selectedButton.actionRef == "daily")
            {
                singletonUIMS.StartCharacterCreation_NameInput();
                return true;
            }

            if (currentTextBranch.branchRefName == "challenge")
            {
                TitleScreenScript.CreateStage = CreationStages.JOBSELECT;
                CharCreation.singleton.BeginCharCreation_JobSelection();
                return true;
            }        

            // If we're here, the slot is empty and we're clear to proceed forward.

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                singletonUIMS.StartCharacterCreation_ChooseCampaign();
            }
            else
            {
                singletonUIMS.StartCharacterCreation_DifficultyModeSelect();
            }

            
            return true;
        }

        return false;
    }

    /// <summary>
    /// This entire function runs AFTER we have selected a save slot. We could be managing data, starting a new game or continuing an existing one.
    /// </summary>
    static void CheckSelectSaveSlotCursorConfirm()
    {
        int sIndex = singletonUIMS.GetIndexOfSelectedButton();
        selectedButton = dialogUIObjects[sIndex].button; // was index

        //Debug.Log(sIndex + " Convo: " + currentConversation.refName + " Branch: " + currentTextBranch.branchRefName + " " + selectedButton.actionRef);

        TrySetSaveGameSlotFromButtonPress();

        if (CheckForInvalidModPromptCursorConfirm())
        {
            return;
        }
        
        if (CheckForManageDataCursorConfirm())
        {
            return;
        }

        //Debug.Log(selectedButton.actionRef + " " + currentTextBranch.branchRefName + " " + currentConversation.refName);

        switch (selectedButton.actionRef)
        {
            case "yes_overwrite":
                if (Debug.isDebugBuild)  Debug.Log("Overwrite existing.");
                return;
            case "yes":
                // Begins campaign select OR actually starts New Game+ in gameplay scene
                SaveSlotCursorConfirmYesToStartGame(savage:false);
                return;
            case "savage": // starts NG++ mode
                SaveSlotCursorConfirmYesToStartGame(savage:true);
                break;
            case "clearcharacterdata":
                ClearCharacterDataInSelectedSaveSlot();
                return;
            case "clearalldata":
                ClearAllDataInSelectedSaveSlot();
                return;
            case "no":
                SaveSlotCursorConfirmNoAndGoBack();
                return;
            case "exit":
                PlayCursorSound("Cancel");
                TitleScreenScript.ReturnToMenu();
                return;
            case "oldsave":
                //if (Debug.isDebugBuild) Debug.Log("Oldsave option.");
                //singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());

                UIManagerScript.PlayCursorSound("OPSelect");
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.LOADGAME);
                UIManagerScript.HideDialogMenuCursor();
                var uims = UIManagerScript.singletonUIMS;
                uims.StartCoroutine(uims.FadeOutThenLoadGame());                
                return;
        }

        if (currentConversation == null) return;

        // Checks if we have selected NEW GAME. Lots of things can follow from the function below: confirm overwrite char data? Challenges? Campaign? etc.
        if (CheckForSelectNewGameCursorConfirm())
        {
            return;
        }

        // Checks if we are in the *manage data save slot* selection. If so, initiates prompt to ask player HOW they want to clear data (character or all)
        if (CheckForManageDataConfirmPopupCursorConfirm())
        {
            return;
        }

        // Returns TRUE if we have selected an EMPTY save slot, sending the player into the campaign (etc) select process.
        if (CheckForBeginNewGameSequenceInEmptySlotCursorConfirm())
        {
            return;
        }

        // Returns TRUE when loading a save file, but mods are missing: player is presented with a warning/confirmation prompt
        if (CheckForPromptInvalidModConfirmationCursorConfirm())
        {
            return;
        }

        // Returns TRUE when loading a save file, but DLC is missing - player will be punted back to main menu after a warning
        if (CheckForPromptInvalidDLCConfirmationCursorConfirm())
        {
            return;
        }
        PlayCursorSound("OPSelect");
        // At this point, there are no issues with starting the game. So, we're starting it!


        singletonUIMS.StartCoroutine(singletonUIMS.FadeOutThenLoadGame());
        return;
    }    

    public void DialogCursorConfirm(int option)
    {
        try { DialogCursorConfirm(); }
        catch (Exception e)
        {
            Debug.Log("Cursor confirm failed");
            Debug.Log(e);
            //Debug.Log("Tried and failed to confirm dialog on slot " + option + " ui status: DBox? " + dialogBoxOpen + " EQ? " + GetWindowState(UITabs.EQUIPMENT) + " Inv? " + GetWindowState(UITabs.INVENTORY) + " Shop? " + shopInterfaceOpen + " IW? " + ItemWorldUIScript.itemWorldInterfaceOpen);
        }
    }

}
