using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    public void MapCreationFinished()
    {
        //Debug.Log("Map creation finished!");

        // Spawn a shortsword near the player.

        TryCopySharedCorralIntoLocalCorral();

        if (SharedBank.ShouldUseSharedBankForCurrentGame())
        {
            MetaProgressScript.AddAllTamedSharedMonstersToDictionary();
        }

        if (RandomJobMode.IsCurrentGameInRandomJobMode()) RandomJobMode.OnDungeonGenerationFinished();

        //At this point, if we haven't already, start loading all the game music.

        SpawnStartingItemsForNewCharacter();

        mms.UpdateMapObjectData();

        MetaProgressScript.LinkAllActors();

        //Debug.Log("Linked all actors.");

        // Banker relink WAS here.

        MetaProgressScript.AssignIDsToTreeItemsAndTrees();

        MetaProgressScript.UpdateTamedCorralMonsterIDsOnNewGame();

        //Debug.Log(UIManagerScript.greenHexColor + "Done relinking corral monsters.</color>");

        heroPC.GetComponent<Movable>().SetPosition(heroPCActor.GetPos());
        uims.UIStart(true);

        //Debug.Log("UI started");

        SetHeroStartingWeaponForNewCharacter();

        tutorialManager.TutorialStart();
        cameraScript.CameraStart();
        actualGameStarted = true;
        TDInputHandler.Initialize();
        timeAtGameStartOrLoad = Time.fixedTime;
        cameraScript.SetCameraPosition(GameMasterScript.heroPCActor.GetPos());


        MapMasterScript.activeMap.LoadAndPlayMusic();
        SeasonalFunctions.CheckForSeasonalAdjustmentsInNewMap();

        UIManagerScript.SetPlayerHudAlpha(1.0f);
        heroPCActor.idleAnimation = heroPCActor.myAnimatable.FindAnim("Idle");
        heroPCActor.myAnimatable.SetAnim("Idle");
        if (PlayerOptions.tutorialTips)
        {
            //tutorialManager.ShowHotbarHighlight();
            // Game initial hint
        }

        UIManagerScript.ReadOptionsMenuVariables();
        musicManager.SetSFXVolumeFromPlayerOptions();
        musicManager.SetMusicVolumeToMatchOptionsSlider();


        UIManagerScript.ToggleLoadingBar();

        UIManagerScript.UpdateActiveWeaponInfo();
        GameLogScript.HideCombatLog();

        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
        {
            AwardJP(250);
            if (GameStartData.GetGameMode() != GameModes.COUNT) // Inherit game mode from our game start settings
            {
                gameMode = GameStartData.GetGameMode();
            }
        }

        UIManagerScript.RefreshPlayerStats();

        // START THE GAME, GAME START, START GAME, GAME STARTED
		GameLogScript.OnGameSceneStarted();
        MinimapUIScript.StopOverlay();
        GraphicsAndFramerateManager.OnEndLoad();

        GameStartData.speedRunModeActive = PlayerOptions.speedrunMode;

        adventureModeActive = false;
        if (gameMode == GameModes.HARDCORE)
        {
            MetaProgressScript.totalDaysPassed = 0;
        }
        else if (gameMode == GameModes.ADVENTURE)
        {
            adventureModeActive = true;
            heroPCActor.SetActorData("advm", 1);
        }

        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            DLCManager.SetLastPlayedCampaign(StoryCampaigns.SHARA);
            SharaModeStuff.UpdateStatJPCostsInJobData();
        }
        else
        {
            DLCManager.SetLastPlayedCampaign(StoryCampaigns.MIRAI);
        }

        NotificationUIScript.Clear();

        statsAndAchievements.SetLocalJobChanges(0);
        statsAndAchievements.SetChampionsDefeatedLocal(0);

        /* if (GameStartData.theMode == GameModes.ADVENTURE)
        {
            adventureModeActive = true;
        } */

        UIManagerScript.SetToClear();

        //Debug.Log("Pre cleanup");

        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
        {
            MetaProgressScript.jobsStarted[(int)heroPCActor.myJob.jobEnum]++;
            MetaProgressScript.ChangeTotalCharacters(1);
            statsAndAchievements.SetTotalCharacters(MetaProgressScript.totalCharacters);
            foreach (AbilityScript abil in heroPCActor.myAbilities.GetAbilityList())
            {
                if (!abil.passiveAbility && !UIManagerScript.IsAbilityInHotbar(abil))
                {
                    UIManagerScript.AddAbilityToOpenSlot(abil);
                }
            }
        }
        else
        {
            // NEW GAME PLUS CLEANUP.
            heroPCActor.beatTheGame = false;
            GameStartData.beatGameStates[GameStartData.saveGameSlot] = false;
            globalPowerupDropChance *= 0.5f;
            heroPCActor.relicsDroppedOnTheGroundOrSold.Clear();
            heroPCActor.myAbilities.ToggleOffAllAbilities();
            heroPCActor.myStats.RemoveAllTemporaryEffects(); // Is it safe to do this area?
            heroPCActor.myStats.HealToFull();
            heroPCActor.ClearActorDict();
            heroPCActor.ResetMapsAndAreasExplored();
            heroPCActor.lowestFloorExplored = 0;
            GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot]++; // needed for dungeon initializer

            GameStartData.ChangeGameMode(heroPCActor.localGameMode);
            if (heroPCActor.localGameMode == GameModes.ADVENTURE)
            {
                adventureModeActive = true;
                heroPCActor.SetActorData("advm", 1);
            }
            gameMode = heroPCActor.localGameMode;

            // Reset tree ages in NG+ since the campaign is reset
            for (int i = 0; i < MetaProgressScript.treesPlanted.Length; i++)
            {
                NPC tree = MetaProgressScript.treesPlanted[i];
                if (tree != null)
                {
                    if (tree.treeComponent != null)
                    {
                        tree.treeComponent.dayPlanted = 0;
                    }
                }
            }

            foreach (StatusEffect se in heroPCActor.myStats.GetAllStatuses())
            {
                foreach (EffectScript eff in se.listEffectScripts)
                {
                    eff.parentAbility = se;
                    Actor orig = null;

                    if (eff.originatingActorUniqueID == -100)
                    {
                        eff.originatingActor = theDungeonActor;
                    }
                    else
                    {
                        if (eff.originatingActorUniqueID <= 1)
                        {
                            eff.originatingActor = heroPCActor;
                        }
                        else
                        {
                            if (dictAllActors.TryGetValue(eff.originatingActorUniqueID, out orig))
                            {
                                eff.originatingActor = orig;
                            }
                            else
                            {
                                //Debug.Log("Could not load originating actor for status " + se.refName + " OA ID " + eff.originatingActorUniqueID + " " + heroPCActor.actorRefName + " " + heroPCActor.actorUniqueID);
                                eff.originatingActor = heroPCActor; // We're just guessing here.
                            }
                        }
                    }

                    eff.selfActor = heroPCActor;
                }
            }

            // Make sure every equipped passive is in fact adding its status to player

            Equipment[] equippedGear = new Equipment[(int)EquipmentSlots.COUNT];

            for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
            {
                equippedGear[i] = heroPCActor.myEquipment.GetEquipmentInSlot((EquipmentSlots)i);
                heroPCActor.myEquipment.Unequip((EquipmentSlots)i, false, SND.SILENT, false, true);
            }
            for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
            {
                if (equippedGear[i] != null)
                {
                    heroPCActor.myEquipment.Equip(equippedGear[i], SND.SILENT, (EquipmentSlots)i, false, false, false);
                }
            }
            heroPCActor.myStats.HealToFull();
            heroPCActor.VerifyStatsAbilitiesStatuses();

            //Debug.Log("NG+ cleanup done.");
        }

        // Link loaded monsters to corral stuff.

        MetaProgressScript.LinkLoadedMonstersToTamedCorralMonsters();

        heroPCActor.mapsExploredByMapID.Add(MapMasterScript.activeMap.mapAreaID);
        BattleTextManager.AddObjectToDict(heroPCActor.GetObject());
        MonsterCorralScript.Initialize();
        heroPCActor.myMovable.SetInSightAndSnapEnable(true);
        heroPCActor.UpdateSpriteOrder();
        UIManagerScript.UpdateGridOverlayFromOptionsValue();

        UIManagerScript.DisableLoadingBar();
        // Fade out cute lil running animation
        LoadingWaiterManager.Hide();

        PlayerModManager.PrintAnyModLogData();

        UIManagerScript.FadeIn(1.33f);


        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS
            && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS
            && heroPCActor.myJob.jobEnum != CharacterJobs.SHARA)
        {
            if (Debug.isDebugBuild) Debug.Log("Prepare for camera movement!");
            cameraScript.SetCustomCameraAnimation(new Vector2(heroPCActor.GetPos().x + 8f, heroPCActor.GetPos().y + 4f), heroPCActor.GetPos(), 4f);
        }
        else
        {
            cameraScript.SnapPosition(heroPCActor.GetPos()); // New code to avoid camera move?            
            animationPlaying = false;
            TDInputHandler.EnableInput();
            TDInputHandler.framesSinceNeutral = 0;
            TDInputHandler.bufferingInput = false;
            turnExecuting = false;
            actualGameStarted = true;
            TDInputHandler.Initialize();
            UIManagerScript.singletonUIMS.HideGenericInfoBar();
            UIManagerScript.singletonUIMS.CloseAllDialogs();
            //Debug.Log("Additional NG+ work");
        }

        //Debug.Log("Almost ready to start game...");

        //cameraScript.SnapPosition(heroPCActor.GetPos()); // New code to avoid camera move?
        uims.UpdateTextSpeed();
        gameLoadSequenceCompleted = true;

        EquipmentBlock.SetItemDreamPropertyStrings();
        Switch_RadialMenu.Initialize();
        GameModifiersScript.OnNewGameStarted();
        BalanceData.OnNewGameOrLoad_DoBalanceAdjustments();
        GameMasterScript.heroPCActor.VerifyMiscellaneousFlagsAndData();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshPlayerCT(false);
        PetPartyUIScript.singleton.ExpandPetPartyUI();
        UIManagerScript.UpdatePetInfo();
        UpdateHeroSpriteMaterial();
        heroPCActor.EnableWrathBarIfNeeded();
        BossHealthBarScript.DisableBoss(); // No reason boss healthbar should ever be on here at game load.
        if (UIManagerScript.GetIndexOfActiveHotbar() != 0)
        {
            UIManagerScript.ToggleSecondaryHotbar();
        }

        DLCManager.OnMapChangeOrLoad();

        MetaProgressScript.SpawnFoodFromTreeInventories();

        CheckForAndCreateIntroTextOverlay();

        CheckForNewGamePlusFlagClearsAndNGPRoninRelicSeeding();

        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.NEWGAMEPLUS)
        {
            GameLogScript.LogWriteStringRef("newgameplus_start");
            if (GameStartData.NewGamePlus >= 2 && ProgressTracker.CheckProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.HERO) != 1)
            {
                ProgressTracker.SetProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.HERO, 1);
                heroPCActor.RemoveActorData("ultimate_weapontechlearned_resetonce");
                heroPCActor.RemoveActorData("herbalist_resetonce");
            }
            heroPCActor.beatTheGame = false;
            if (RandomJobMode.IsCurrentGameInRandomJobMode()) RandomJobMode.SeedRelicsInBosses();
        }

        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.REBUILDMAPS)
        {
            GameLogScript.LogWriteStringRef("log_maps_rebuilt");
            GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.Clear();
        }

        UIManagerScript.SetGlobalResponse(DialogButtonResponse.NOTHING);

        CopySharedBankIntoLocalBanker();

        GameMasterScript.heroPCActor.TryLinkAllPairedItems();

        SetHotbarBindingsFromBufferIfAny();

        MetaProgressScript.AssignIDsToMonsterCorralItems();

        UIManagerScript.RefreshStatuses();

        CheckForNewGamePlusDialogsOrFlagsOnGameStart();

        if (GameStartData.NewGamePlus > 0 && RandomJobMode.IsCurrentGameInRandomJobMode()
            && ProgressTracker.CheckProgress(TDProgress.RANDOMJOBMODE_STARTED_ONCE, ProgressLocations.META) != 1)
        {
            UIManagerScript.StartConversationByRef("randomjobmode_intro", DialogType.STANDARD, null);
            ProgressTracker.SetProgress(TDProgress.RANDOMJOBMODE_STARTED_ONCE, ProgressLocations.META, 1);
        }

#if UNITY_EDITOR
        //Debug.Log("Game should be started now.");
#endif

        CheckForClearLocalBankerDueToChallengeMode();

#if UNITY_SWITCH
        //UnityEngine.Switch.Performance.SetCpuBoostMode(UnityEngine.Switch.Performance.CpuBoostMode.Normal);
#endif

        UIManagerScript.TurnOffPrettyLoading(1.5f, 0.25f);
        GameStartData.CurrentLoadState = LoadStates.NORMAL;

        UIManagerScript.singletonUIMS.HideGenericInfoBar();

        CheckForSharedBankerTutorial();
                
    }

    static void CheckForSharedBankerTutorial()
    {
        if (MetaProgressScript.loadedGameVersion < 150 && MetaProgressScript.loadedGameVersion > 0 && !SharaModeStuff.IsSharaModeActive())
        {
            doSharedBankerTutorial = true;
        }

        if (doSharedBankerTutorial)
        {
            //Debug.Log("Do shared banker tutorial!");
            if (!IsAnimationPlayingFromCutscene())
            {
                UIManagerScript.StartConversation(GameMasterScript.FindConversation("sharedstash_update"), DialogType.STANDARD, null);
            }
            else
            {
                GameLogScript.LogWriteStringRef("patch_150_notes");
            }
            doSharedBankerTutorial = false;
        }
    }

    static bool doSharedBankerTutorial = false;

    void ClearLocalBankerOnly(NPC actualBanker)
    {
        if (Debug.isDebugBuild) Debug.Log("Clearing LOCAL BANKER ONLY.");
        actualBanker.myInventory.SetInventoryList(new List<Item>());
        actualBanker.money = 0;
    }

    void CopySharedBankIntoLocalBanker(NPC banker = null)
    {
        if (mms == null || mms.townMap == null || heroPCActor == null) return;

        if (banker == null)
        {
            // First, FIND the banker.
            foreach (Actor act in mms.townMap.actorsInMap)
            {
                if (act.actorRefName == "npc_banker")
                {
                    banker = act as NPC;
                    break;
                }
            }
        }

        if (!SharedBank.ShouldUseSharedBankForCurrentGame())
        {
                        
            if (Debug.isDebugBuild) Debug.Log("Game mode is hardcore, RJ mode, so not copying our master list into local banker. " + SharedBank.allItemsInBank.Count + " " + SharedBank.allRelicTemplates.Count);
            if (MetaProgressScript.theBanker != null && MetaProgressScript.theBanker.ReadActorData("transferhero") != 1)
            {
                if (Debug.isDebugBuild) Debug.Log("Copy metabanker's items into character inventory, size " + MetaProgressScript.theBanker.myInventory.GetInventory().Count);
                heroPCActor.ChangeMoney(MetaProgressScript.theBanker.money, true);
                List<Item> toTransfer = new List<Item>();
                foreach (Item itm in MetaProgressScript.theBanker.myInventory.GetInventory())
                {
                    toTransfer.Add(itm);
                }
                foreach(Item itm in toTransfer)
                {
                    heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itm, true);
                }

                MetaProgressScript.theBanker.SetActorData("transferhero", 1);
            }            

            if (banker != null) RemoveBankerFromMap(banker);

            return;
        }



        if (banker == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No banker so not copying. " + SharedBank.allItemsInBank.Count + " " + SharedBank.allRelicTemplates.Count);
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("We ARE COPYING INTO localbank, FROM Shared Bank into local bank, which has this many items: " + SharedBank.allItemsInBank.Count + " " + SharedBank.allRelicTemplates.Count + " On diff " + GameMasterScript.gmsSingleton.gameMode);

        // Next, copy the item list from the SharedBank into this local version, so it's the same List<>
        banker.myInventory.SetInventoryList(SharedBank.allItemsInBank);
        banker.money = SharedBank.goldBanked;

        foreach (Item itm in banker.myInventory.GetInventory())
        {
            if (itm == null) continue;
            itm.SetUniqueIDAndAddToDict();
            itm.collection = banker.myInventory;
        }

        banker.myInventory.RemoveNullItems();

        // Now check if this save slot's META progress has a banker with data
        // This might happen if we are loading from a pre-version 150 patch.

        if (MetaProgressScript.theBanker == null) return;

        if (Debug.isDebugBuild) Debug.Log("<color=green>The SHARED BANKER should have: " + SharedBank.allItemsInBank.Count + " items in bank.</color>");
        if (Debug.isDebugBuild) Debug.Log("Loading from meta banker who has " + MetaProgressScript.theBanker.money + " gold, and " + MetaProgressScript.theBanker.myInventory.GetActualInventoryCount() + " items.");

        SharedBank.goldBanked += MetaProgressScript.theBanker.money;

        int maxItems = MetaProgressScript.ReadMetaProgress("banker_maxitems");
        if (maxItems > 30)
        {
            maxItems -= 30;
            SharedBank.bankerMaxItems += maxItems;
            MetaProgressScript.SetMetaProgress("banker_maxitems", 0);
        }

        MetaProgressScript.theBanker.money = 0;

        foreach(Item itm in MetaProgressScript.theBanker.myInventory.GetInventory())
        {
            itm.SetUniqueIDAndAddToDict();
            banker.myInventory.AddItem(itm, true);
        }

        MetaProgressScript.theBanker.myInventory.ClearInventory();

        
    }

    void CheckForClearLocalBankerDueToChallengeMode()
    {
        NPC actualBanker = MapMasterScript.singletonMMS.townMap.FindActor("npc_banker") as NPC;

        if (GameStartData.challengeType == ChallengeTypes.DAILY && actualBanker != null)
        {
            StringManager.SetTag(0, StringManager.GetString("ui_btn_dailychallenge"));
            UIManagerScript.StartConversationByRef("start_new_challenge", DialogType.STANDARD, null);
            ClearLocalBankerOnly(actualBanker);
        }
        else if (GameStartData.challengeType == ChallengeTypes.WEEKLY && actualBanker != null)
        {
            StringManager.SetTag(0, StringManager.GetString("ui_btn_weeklychallenge"));
            UIManagerScript.StartConversationByRef("start_new_challenge", DialogType.STANDARD, null);
            ClearLocalBankerOnly(actualBanker);
        }

        if (TDCalendarEvents.IsAprilFoolsDay())
        {
            TDCalendarEvents.ProcessAprilFoolsTextForMenOnly();
        }
    }

    void CheckForNewGamePlusDialogsOrFlagsOnGameStart()
    {
        if (GameStartData.NewGamePlus > 0)
        {
            if (Debug.isDebugBuild) Debug.Log("Starting new game plus " + GameStartData.NewGamePlus + " " + ProgressTracker.CheckProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.META));

            if (heroPCActor.numPandoraBoxesOpened > 0)
            {
                StringManager.SetTag(0, heroPCActor.numPandoraBoxesOpened.ToString());
                UIManagerScript.StartConversationByRef("ngplus_pandora_choice", DialogType.KEYSTORY, null);
            }
            if (GameStartData.NewGamePlus == 2 && ProgressTracker.CheckProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.META) != 1)
            {
                UIManagerScript.StartConversationByRef("savageworld_intro", DialogType.STANDARD, null);
                ProgressTracker.SetProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.META, 1);
            }
        }
    }

    void SetHotbarBindingsFromBufferIfAny()
    {
        if (bufferedHotbarActions != null)
        {
            for (int i = 0; i < bufferedHotbarActions.Length; i++)
            {
                switch (bufferedHotbarActions[i].actionType)
                {
                    case HotbarBindableActions.ABILITY:
                        UIManagerScript.AddAbilityToSlot(bufferedHotbarActions[i].ability, i, true);
                        break;
                    case HotbarBindableActions.CONSUMABLE:
                        UIManagerScript.AddItemToSlot(bufferedHotbarActions[i].consume, i, true);
                        break;
                }
            }
        }
    }

    void SetHeroStartingWeaponForNewCharacter()
    {
        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
        {
            if (heroPCActor.myJob.startingWeapon != null && heroPCActor.myJob.startingWeapon != "")
            {
                Weapon wTemplate = Item.GetItemTemplateFromRef(heroPCActor.myJob.startingWeapon) as Weapon;
                Weapon newWeapon = new Weapon();
                newWeapon.CopyFromItem(wTemplate);
                newWeapon.collection = heroPCActor.myInventory;
                newWeapon.SetUniqueIDAndAddToDict();
                heroPCActor.myEquipment.Equip(newWeapon, SND.SILENT, 0, true);
                UIManagerScript.AddWeaponToActiveSlot(newWeapon, 0);
                UIManagerScript.UpdateActiveWeaponInfo();
            }
        }
    }

    void SpawnStartingItemsForNewCharacter()
    {
        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
        {
            List<Item> itemsToSpawn = new List<Item>();
            foreach (string itemRef in heroPCActor.myJob.startingItems)
            {
                itemsToSpawn.Add(LootGeneratorScript.CreateItemFromTemplateRef(itemRef, 1.0f, 1.0f, false));
            }

            // Would there ever be a case where there is no possible position? There shouldn't be...
            List<MapTileData> possibleSpawnTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(heroPCActor.GetPos(), 2);
            List<MapTileData> finalTiles = new List<MapTileData>();

            for (int i = 0; i < possibleSpawnTiles.Count; i++)
            {
                MapTileData mtd = possibleSpawnTiles[i];
                if (!mtd.IsUnbreakableCollidable(heroPCActor) && mtd.pos != heroPCActor.GetPos() && mtd.GetAllActors().Count == 0)
                {
                    finalTiles.Add(mtd);
                }
            }

            if (finalTiles.Count > 0)
            {
                foreach (Item itm in itemsToSpawn)
                {
                    SpawnItemAtPosition(itm, finalTiles[UnityEngine.Random.Range(0, finalTiles.Count)].pos);
                }
            }
        }
    }

    void CheckForNewGamePlusFlagClearsAndNGPRoninRelicSeeding()
    {
        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.NEWGAMEPLUS)
        {
            GameLogScript.LogWriteStringRef("newgameplus_start");
            if (GameStartData.NewGamePlus >= 2 && ProgressTracker.CheckProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.HERO) != 1)
            {
                ProgressTracker.SetProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.HERO, 1);
                heroPCActor.RemoveActorData("ultimate_weapontechlearned_resetonce");
                heroPCActor.RemoveActorData("herbalist_resetonce");
            }
            heroPCActor.beatTheGame = false;
            if (RandomJobMode.IsCurrentGameInRandomJobMode()) RandomJobMode.SeedRelicsInBosses();
        }
    }

    void CheckForAndCreateIntroTextOverlay()
    {
        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS &&
            UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.REBUILDMAPS)
        {
            if (GameStartData.challengeType == ChallengeTypes.NONE)
            {
                CheckForTextOverlay();
            }
            else
            {
                CheckForTextOverlay(true);
            }
        }
        else
        {
            CheckForTextOverlay(true);
        }
    }

}