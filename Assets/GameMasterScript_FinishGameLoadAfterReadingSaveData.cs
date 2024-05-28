using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.IO;
using Rewired;
using UnityEngine.SceneManagement;
using System.Text;
public partial class GameMasterScript
{ 

    public IEnumerator FinishGameLoadAfterReadingSaveData(XmlReader reader, FileStream stream)
    {
        reader.Close();

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        stream.Close();
#endif
        //Debug.Log("Loaded all data");
        //if (Debug.isDebugBuild) Debug.Log("<color=green>We have entered FinishGameLoadAfterReadingSaveData: " + UIManagerScript.GetLoadingBarFillAmount() + "</color>");
        ProgressTracker.SetPlayerPrefsFromLoadedProgress();

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            musicManager.FillStackWithTracksToLoad();
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }
        }

        ProcessSpecialLaserLavaAndMudTiles();

        // Post-load stuff. Link up actors. Floor by floor.

        LinkHeroLastOffhandEquipped();
        LinkAllActorReferencesAndAreas();

        TryCopySharedCorralIntoLocalCorral();

        IncrementLoadingBar(ELoadingBarIncrementValues.medium);
        yield return null;

        heroPCActor.LinkItemDreamDataMaps();
        heroPCActor.TryRelinkMonsterPet(onGameLoad:true);
        heroPCActor.ValidateQuestsOnLoad();

        SetTreesPlantedMaps();

        MapMasterScript.CreateTerrainTilesFromLoadedData();

        yield return null;

        GameMasterScript.IncrementLoadingBar(ELoadingBarIncrementValues.lots);
        heroPCActor.LoadAndValidateHeroSummons();

        // Before we actually spawn actors, make sure we remove ones that shouldn't be in Shara mode. If we're in shara mode, that is.
        if (SharaModeStuff.IsSharaModeActive())
        {
            SharaModeStuff.RemoveAndHideMapsOrNPCsForShara();
        }

        yield return mms.PostMapLoadOperations(heroPCActor.GetActorMap()); // Initialize the current dungeon to hero's floor.

        //if (Debug.isDebugBuild) Debug.Log("Post map load operations complete.");

        yield return null;

        RelinkAllStairs();
        
        MetaProgressScript.AddAllTamedMonstersToDictionary();

        SetTempGameData("tamed_finished", 0);

        yield return null;

        GameMasterScript.heroPCActor.LinkAndValidateAllAnchoredActors(true);

        LinkHeroLastUsedWeapons();

        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.NEWGAMEPLUS || UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.REBUILDMAPS)
        {
            MetaProgressScript.LinkAllActors();
        }

        int petMonsterID = heroPCActor.GetMonsterPetID();

        if (petMonsterID > 0)
        {
            //if (Debug.isDebugBuild) Debug.Log("Player pet monster ID is " + petMonsterID);
            Monster monPet = TryLinkActorFromDict(petMonsterID) as Monster; // 232019 explicit monster type check
            if (monPet == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Player had a pet, id " + petMonsterID + " but could not find it.");
                heroPCActor.ResetPetData();
            }
            else
            {
                //if (Debug.isDebugBuild) Debug.Log("Linked the hero pet successfully!");
            }
        }

        ValidateItemWorldStuff();

        yield return null;

        heroPCActor.OnLoadCleanup();

        UIManagerScript.UpdateWeaponHotbarGraphics();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshStatusCooldowns();
        UIManagerScript.RefreshStatuses(true);
        uims.RefreshAbilityCooldowns();
        //uims.UpdateAbilityIcons();
        UIManagerScript.CloseDialogBox(); // Problems?
        cameraScript.UpdateCameraSmoothingFromOptionsValue();
        cameraScript.UpdateFOVFromOptionsValue();
        cameraScript.UpdateLockToPlayerFromOptionsValue();

        GraphicsAndFramerateManager.OnEndLoad();
        MinimapUIScript.StopOverlay();

        UpdateFrameCapFromOptionsValue();
        UpdateCursorRepeatDelayFromOptionsValue();
        actualGameStarted = true;
        TDInputHandler.Initialize();
        timeAtGameStartOrLoad = Time.fixedTime;
        UIManagerScript.SetPlayerHudAlpha(1.0f);
        //cameraScript.SnapPosition(heroPCActor.GetPos());
        yield return null;

        mms.UpdateMapObjectData();
        mms.CheckRevealMap();
        mms.UpdateSpriteRenderersOnLoad();
        UIManagerScript.UpdateDungeonText();
        heroPC.GetComponent<Movable>().SetPosition(heroPCActor.GetPos());

        bool ignoreCue = false;

        ValidateHeroPetAttributesOrResetIt();

        MapMasterScript.activeMap.LoadAndPlayMusic();

        heroPCActor.idleAnimation = heroPCActor.myAnimatable.FindAnim("Idle");
        heroPCActor.myAnimatable.SetAnim("Idle");

        // Make sure hero overlays are spawned on game load.
        foreach (StatusEffect se in heroPCActor.myStats.GetAllStatuses())
        {
            if (!string.IsNullOrEmpty(se.ingameSprite))
            {
                se.AddSpawnedOverlayRef(heroPCActor, se.direction);
            }
        }

        ValidateLegendaryItemReferencesToRemove();

        ValidateOrbUsedToOpenItemWorld();

        MetaProgressScript.CopyMetaUnlocksIntoSharedProgress();
        ValidateTheBanker();
        
        MetaProgressScript.AssignIDsToTreeItemsAndTrees();
        MetaProgressScript.AssignIDsToMonsterCorralItems();

        NotificationUIScript.Clear();

        GameLogScript.LogWriteStringRef("log_game_loaded");

        MapMasterScript.CheckForAndFixDeprecatedAreasAndConnections();

        float currentSmoothTime = cameraScript.smoothTime;
        cameraScript.smoothTime = 0.0001f;
        CameraController.WaitToResetSmoothTime(currentSmoothTime);

        CheckForDeadHeroPet();

        SteamStatsAndAchievements.VerifyPlayerAchievementsOnLoad();
        heroPCActor.VerifyAbilities();
        TDExpansion.VerifyExpansionStuffOnLoad();
        heroPCActor.VerifyStatsAbilitiesStatuses();

        firstUpdate = true;
        MonsterCorralScript.Initialize();
        UIManagerScript.UpdateActiveWeaponInfo();
        BattleTextManager.AddObjectToDict(heroPCActor.GetObject());
        CameraController.UpdateCameraPosition(heroPCActor.GetPos(), true);

        //cameraScript.SnapPosition(heroPCActor.GetPos()); // Avoid camera jitter?        
        UIManagerScript.UpdateGridOverlayFromOptionsValue();
        UIManagerScript.DisableLoadingBar();
        UIManagerScript.UpdatePetInfo();
        UIManagerScript.ReadOptionsMenuVariables();
        UIManagerScript.RefreshPlayerStats();
        musicManager.SetSFXVolumeFromPlayerOptions();
        musicManager.SetMusicVolumeToMatchOptionsSlider();

        heroPCActor.UpdateSpriteOrder();

        try { heroPCActor.myMovable.CheckTransparencyBelow(); }  //#questionable_try_block
        catch (Exception e)
        {
            Debug.Log("Hero load transparency check failed, " + heroPCActor.GetPos() + " " + heroPCActor.dungeonFloor + " " + e.ToString());
        }

        if (heroPCActor.actionTimer < 200)
        {
            UIManagerScript.RefreshPlayerCT(false);
        }
        else
        {
            UIManagerScript.RefreshPlayerCT(true);
        }

        MetaProgressScript.ValidateTamedMonstersInCorralOnGameLoad();

        heroPCActor.ValidateNoDuplicatePetsOnLoad();

        yield return null;
        //uims.SetToClear();
        // Fade out cute lil running animation

        LoadingWaiterManager.Hide();

#if UNITY_SWITCH
        //UnityEngine.Switch.Performance.SetCpuBoostMode(UnityEngine.Switch.Performance.CpuBoostMode.Normal);
#endif

        UIManagerScript.singletonUIMS.WaitThenFadeIn(0.25f, 1.33f, false);

        // If player is low on health, add screen blink
        if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= GameMasterScript.PLAYER_HEALTH_PERCENT_CRITICAL)
        {
            UIManagerScript.ToggleHealthBlink(true, 0.7f); // Health blind red cycle time
        }

        uims.UpdateTextSpeed();
        gameLoadSequenceCompleted = true;
        Switch_RadialMenu.Initialize();
        EquipmentBlock.SetItemDreamPropertyStrings();
        PetPartyUIScript.singleton.ExpandPetPartyUI();
        UIManagerScript.UpdatePetInfo();
        UpdateHeroSpriteMaterial();

        //CameraController.UpdateCameraPosition(new Vector2(heroPCActor.GetPos().x, heroPCActor.GetPos().y - 1f), true);
        heroPCActor.EnableWrathBarIfNeeded();

        UIManagerScript.singletonUIMS.CloseAllDialogs(); // new 10/26 to avoid some issues

        CreateAndValidateTargetingLines();

        RepositionHeroSummonMonsterWrathBars();

        IncrementLoadingBar(ELoadingBarIncrementValues.medium);
        yield return null;
        yield return null;
        MapMasterScript.activeMap.CheckForStackedActors(heroPCActor.GetPos(), heroPCActor, true);

        BossHealthBarScript.DisableBoss();

        MapMasterScript.CheckForMapStartConversationsAndEvents(null);

        heroPCActor.SetActorData("shieldinfo_dirty", 1);
        if (SharaModeStuff.IsSharaModeActive())
        {
            GameMasterScript.heroPCActor.SetActorData("allow_campfire_cooking", 1);
        }

        heroPCActor.ValidateAdventureStats();

        // GAME LOADED, SAVE GAME LOADED, SAVE LOADED

        MapMasterScript.singletonMMS.MapEntryUpdateQuests();
        SeasonalFunctions.CheckForSeasonalAdjustmentsInNewMap();

        DLCManager.OnMapChangeOrLoad();

        heroPCActor.ScanForAndLearnInnates();

        MetaProgressScript.FlushUnusedCustomDataIfNecessary();

        GameStartData.speedRunModeActive = PlayerOptions.speedrunMode;

        GameMasterScript.heroPCActor.SetActorData("shieldinfo_dirty", 1);

        MetaProgressScript.VerifyTreeState();

        heroPCActor.CheckForJPTutorialsOrNotifications(true);

        if (!MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            ProgressTracker.RemoveProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO);
        }
        else
        {
            heroPCActor.SetActorData("allow_campfire_cooking", 1);
        }

        PlayerModManager.PrintAnyModLogData();

        GameStartData.CurrentLoadState = LoadStates.NORMAL;

        gmsSingleton.SetTempGameData("exitingdream", 0);

        gmsSingleton.SetTempGameData("creatingmysterydungeon", 0);

        CustomAlgorithms.BackupSaveFilesInSlot(GameStartData.saveGameSlot);
        BalanceData.OnNewGameOrLoad_DoBalanceAdjustments();
        GameMasterScript.heroPCActor.VerifyMiscellaneousFlagsAndData();

        UIManagerScript.RefreshStatusCooldowns();

        MapMasterScript.BuildUnusedMaps();

        IncrementLoadingBar(ELoadingBarIncrementValues.small);
        yield return null;
        if (GameStartData.NewGamePlus > 0)
        {
            globalPowerupDropChance *= (0.5f * GameStartData.NewGamePlus);
        }

        GameModifiersScript.OnGameLoaded();

        UIManagerScript.TurnOffPrettyLoading(1.5f, 0.25f);

        // Double-sanity check that an Item Dream exists before we mark it as open.
        if (MapMasterScript.itemWorldOpen && MapMasterScript.theDungeon.FindFloor(400) != null)
        {
            heroPCActor.SetActorData("item_dream_open", 1);
        }
        else
        {
            MapMasterScript.itemWorldOpen = false;
        }

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (PlatformVariables.VERIFY_FILE_HASH_FOR_CHALLENGES)
{
        if (GameStartData.challengeType != ChallengeTypes.NONE)
        {
            bool fileMatch = TDSecurity.CheckIfHashesMatch(CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml");
            if (!fileMatch && GameStartData.loadGameVer >= 108)
            {
                ChallengesAndLeaderboards.ResetPlayerChallengeToNone(true);
            }
        }
}
#endif

    if (PlatformVariables.ALLOW_WEB_CHALLENGES)
    {
        ChallengesAndLeaderboards.CheckIfChallengeExpired();
    }

        heroPCActor.CheckForMissingEmblemAbilities();
        MetaProgressScript.VerifyMonstersInCorralShouldBeThere();

        VerifyWildChildUnlock();

        heroPCActor.CheckForAndSetJobMasteryFlag();

        CombatResultsScript.CheckForSideAreaClear(MapMasterScript.activeMap);
        MapMasterScript.activeMap.PrintMonstersRemainingToLogIfNecessary();
        UIManagerScript.RefreshPlayerStats();
        //if (Debug.isDebugBuild) Debug.Log("<color=green>Game load successful!</color>");

        ValidateHeroRunicCrystal();
        RumorTextOverlay.OnNewFloorEntry();

#if UNITY_SWITCH
        UIManagerScript.SetToClear();
        UIManagerScript.TurnOffPrettyLoading(2.0f, 3.0f);
#endif
        loadGame_inProgress = false;

        PlayerModManager.CheckForModMismatchOnLoadAndWarnPlayer();

        CheckForSharedBankerTutorial();

        if (Debug.isDebugBuild)
        {
            string debugger = "";
            if (GameMasterScript.endingItemWorld) debugger = "Ending item world, ";
            if (MapMasterScript.itemWorldOpen) debugger += " Item world open, ";

            if (MapMasterScript.itemWorldItem == null) debugger += " Null item, ";
            else debugger += " Item ref+id is " + MapMasterScript.itemWorldItem.actorRefName + "," + MapMasterScript.itemWorldItem.actorUniqueID;

            debugger += " IWItem ID in mms: " + MapMasterScript.itemWorldItemID;

            Debug.Log("Item world debugger: " + debugger);
        }

        if (!endingItemWorld && !heroPCActor.GetActorMap().IsItemWorld() && MapMasterScript.itemWorldOpen)
        {
            ItemDreamFunctions.SanityCheckThatItemDreamShouldBeOpen();
        }

        if (endingItemWorld && !heroPCActor.GetActorMap().IsItemWorld())
        {
            if (Debug.isDebugBuild) Debug.Log("Sanity closed the item dream on load.");
            ItemDreamFunctions.EndItemWorld();
        }

#if UNITY_EDITOR
        Debug.Log(MetaProgressScript.localTamedMonstersForThisSlot.Count + " monsters in the corral, total.");
#endif

    }

    void ProcessSpecialLaserLavaAndMudTiles()
    {
        // Special case for final boss' lava shield.
        List<Vector2> specialLavaTiles = new List<Vector2>();
        List<Vector2> specialLaserTiles = new List<Vector2>();
        List<Vector2> summonedMudTiles = new List<Vector2>();

        List<Map> mapsToCheck = new List<Map>()
        {
            heroPCActor.GetActorMap(),
            MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2)
        };

        foreach (Map checkMap in mapsToCheck)
        {
            if (checkMap == null)
            {
                continue; // I mean... it shouldn't happen, because the hero's map should never be null, but still?
            }
            specialLaserTiles.Clear();
            specialLavaTiles.Clear();
            summonedMudTiles.Clear();
            foreach (Actor act in checkMap.actorsInMap)
            {
                if (act.actorRefName == "obj_lavashieldtile")
                {
                    specialLavaTiles.Add(act.GetPos());
                }
                if (act.actorRefName.Contains("phasmashieldtile"))
                {
                    specialLaserTiles.Add(act.GetPos());
                }
                if (act.actorRefName == "obj_mudtile" && checkMap.GetTile(act.GetPos()).CheckTag(LocationTags.SUMMONEDMUD))
                {
                    summonedMudTiles.Add(act.GetPos());
                }
            }

            foreach (Vector2 vPos in specialLavaTiles)
            {
                checkMap.GetTile(vPos).AddTag(LocationTags.LAVA);
            }
            foreach (Vector2 vPos in specialLavaTiles)
            {
                checkMap.BeautifyTerrain(checkMap.GetTile(vPos), LocationTags.LAVA, LocationTags.LAVA);
            }
            foreach (Vector2 vPos in specialLavaTiles)
            {
                checkMap.GetTile(vPos).RemoveTag(LocationTags.LAVA);
            }

            // New stuff to fix tile weirdness with summoned mud
            foreach (Vector2 vPos in summonedMudTiles)
            {
                checkMap.GetTile(vPos).AddTag(LocationTags.MUD);
            }
            List<int> previousTerrainIndices = new List<int>();
            foreach (Vector2 vPos in summonedMudTiles)
            {
                previousTerrainIndices.Add(checkMap.GetTile(vPos).indexOfTerrainSpriteInAtlas);
                checkMap.BeautifyTerrain(checkMap.GetTile(vPos), LocationTags.MUD, LocationTags.MUD, LocationTags.MUD);
            }

            for (int i = 0; i < summonedMudTiles.Count; i++)
            {
                Vector2 vPos = summonedMudTiles[i];
                //checkMap.GetTile(vPos).indexOfTerrainSpriteInAtlas = previousTerrainIndices[i];
                checkMap.GetTile(vPos).RemoveTag(LocationTags.MUD);
            }


            foreach (Vector2 vPos in specialLaserTiles)
            {
                checkMap.GetTile(vPos).AddTag(LocationTags.LASER);
            }
            previousTerrainIndices.Clear();
            foreach (Vector2 vPos in specialLaserTiles)
            {
                previousTerrainIndices.Add(checkMap.GetTile(vPos).indexOfTerrainSpriteInAtlas);
                checkMap.BeautifyTerrain(checkMap.GetTile(vPos), LocationTags.LASER, LocationTags.LASER, LocationTags.LASER);
            }

            for (int i = 0; i < specialLaserTiles.Count; i++)
            {
                Vector2 vPos = specialLaserTiles[i];
                //checkMap.GetTile(vPos).indexOfTerrainSpriteInAtlas = previousTerrainIndices[i];
                checkMap.GetTile(vPos).RemoveTag(LocationTags.LASER);
            }

        }
    }

    void LinkHeroLastOffhandEquipped()
    {
        if (heroPCActor.lastOffhandEquippedID != 0)
        {
            Actor offItem;
            if (dictAllActors.TryGetValue(heroPCActor.lastOffhandEquippedID, out offItem) && offItem is Equipment)
            {
                heroPCActor.lastOffhandEquipped = offItem as Equipment;
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Couldn't link player's last equipped offhand: " + heroPCActor.lastOffhandEquippedID);
            }
        }
    }

    void LinkHeroLastUsedWeapons()
    {
        Weapon wActor = TryLinkActorFromDict(heroPCActor.idOfLastUsedMeleeWeapon) as Weapon;
        if (wActor != null)
        {
            heroPCActor.lastUsedMeleeWeapon = wActor;
        }
        wActor = TryLinkActorFromDict(heroPCActor.idOfLastUsedWeapon) as Weapon;
        if (wActor != null)
        {
            heroPCActor.lastUsedWeapon = wActor;
        }
    }

    void LinkAllActorReferencesAndAreas()
    {
        List<int> invalidIDs = new List<int>(); // 142019 - We need to make sure any invalid summons or anchors are removed from ID lists. Otherwise it can cause some weird and bad corruption.
        List<AggroData> adRemove = new List<AggroData>();

        for (int i = 0; i < MapMasterScript.theDungeon.maps.Count; i++)
        {
            foreach (Actor act in MapMasterScript.theDungeon.maps[i].actorsInMap)
            {
                if (act.areaID != -1)
                {
                    Area outValue;
                    if (MapMasterScript.theDungeon.maps[i].areaDictionary.TryGetValue(act.areaID, out outValue))
                    {
                        act.SetActorArea(outValue);
                    }
                    else
                    {
                        //Debug.Log("Area " + act.areaID + " not found in dict on floor " + MapMasterScript.theDungeon.maps[i].floor + " " + act.GetActorType() + " " + act.actorRefName + " " + act.displayName);
                    }
                }
                if (act.GetActorType() == ActorTypes.STAIRS && act.GetActorArea() != null)
                {
                    act.GetActorArea().hasStairs = true;
                }
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.anchorID != -1)
                    {
                        Actor anchor;
                        if (dictAllActors.TryGetValue(mn.anchorID, out anchor))
                        {
                            mn.anchor = anchor;
                        }
                        else
                        {
                            if (mn.actorfaction == Faction.PLAYER)
                            {
                                mn.anchorID = 1;
                                mn.anchor = heroPCActor;
                            }
                            else
                            {
                                Debug.Log("Error in load linking anchor of " + mn.actorRefName + " " + mn.isChampion + " " + mn.displayName + " , AID " + mn.anchorID);
                            }
                        }
                        mn.UpdateMyAnchor(); // This will verify that the anchor is totally valid.
                        if (mn.actorRefName.Contains("runiccrystal"))
                        {
                            mn.anchorRange = 0;
                        }
                    }
                    if (mn.storingTurnData)
                    {
                        if (mn.storeTurnData == null) // 232019 - This COULD be possible? So don't assume it's not null
                        {
                            mn.storingTurnData = false;
                        }
                        else
                        {
                            if (mn.storeTurnData.target != null && mn.storeTurnData.targetIDs.Count > 0)
                            {
                                foreach (int id in mn.storeTurnData.targetIDs)
                                {
                                    Actor orig = null;
                                    if (dictAllActors.TryGetValue(id, out orig))
                                    {
                                        mn.storeTurnData.target.Add(orig);
                                    }
                                    else
                                    {
                                        Debug.Log("Could not load originating actor for " + mn.actorUniqueID + " with actor ID " + id);
                                    }
                                }
                            }
                        }
                    }
                }
                if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = act as Destructible;
                    if (dt.anchorID != -1)
                    {
                        Actor anchor;
                        if (dictAllActors.TryGetValue(dt.anchorID, out anchor))
                        {
                            dt.anchor = anchor;
                        }
                        else
                        {
                            Debug.Log("Error in load linking anchor of " + dt.actorRefName + " " + dt.displayName + " , AID " + dt.anchorID);
                        }
                        dt.UpdateMyAnchor();
                    }
                    if (dt.dtStatusEffect != null)
                    {
                        foreach (EffectScript eff in dt.dtStatusEffect.listEffectScripts)
                        {
                            eff.parentAbility = dt.dtStatusEffect;
                            Actor orig = null;
                            if (eff.originatingActorUniqueID == -100 || act.actorfaction == Faction.DUNGEON || (dt.dtStatusEffect.refName == "status_lavaburns" && eff.originatingActorUniqueID <= 0))
                            {
                                eff.originatingActor = theDungeonActor;
                            }
                            else
                            {
                                if (dictAllActors.TryGetValue(eff.originatingActorUniqueID, out orig))
                                {
                                    eff.originatingActor = orig;
                                    //Debug.Log(dt.actorUniqueID + " " + eff.effectName + " " + eff.effectRefName + " " + orig.actorUniqueID + " " + orig.actorRefName);
                                }
                                else
                                {
                                    //Debug.Log("Could not load originating actor for status " + dt.dtStatusEffect.abilityName + " ID " + eff.originatingActorUniqueID);
                                }

                            }
                        }
                    }
                }
                if (act.GetActorType() == ActorTypes.HERO || act.GetActorType() == ActorTypes.MONSTER)
                {
                    // Status effects orig actor.
                    Fighter ft = act as Fighter;

                    if (ft.summonerID > 0)
                    {
                        Actor summoner = null;
                        if (dictAllActors.TryGetValue(ft.summonerID, out summoner) && summoner is Fighter) // 232019 - make sure our summoner is a fighter
                        {
                            ft.summoner = summoner as Fighter;
                        }
                        else
                        {
#if UNITY_EDITOR
                            //Debug.Log("Could not load summoner actor for " + ft.displayName + " " + ft.summonerID);
#endif
                        }
                    }

                    bool debugSummons = false;
                    if (Debug.isDebugBuild && ft.GetActorType() == ActorTypes.HERO)
                    {
                        debugSummons = true;
                    } 

                    if (ft.summonedActorIDs != null && ft.summonedActorIDs.Count > 0)
                    {
                        //if (debugSummons) Debug.Log("Count of hero summons is " + ft.summonedActorIDs.Count);
                        invalidIDs.Clear();
                        foreach (int id in ft.summonedActorIDs)
                        {
                            Actor summoned = null;
                            if (dictAllActors.TryGetValue(id, out summoned))
                            {
                                bool okToAddSummon = true;
                                if (summoned.GetActorType() == ActorTypes.MONSTER)
                                {
                                    Monster mn = summoned as Monster;
                                    if (mn != null)
                                    {
                                        if (!mn.myStats.IsAlive())
                                        {
                                            okToAddSummon = false;
                                        }
                                        if (ft.GetActorType() == ActorTypes.HERO && mn.actorfaction != Faction.PLAYER)
                                        {
                                            okToAddSummon = false;
                                            Debug.Log(summoned.actorRefName + " " + summoned.actorUniqueID + " is not player faction, removing from summon list on load.");
                                        }
                                    }
                                    else // 232019 - if our 'summoned' isn't actually a monster, don't proceed
                                    {
                                        okToAddSummon = false;
                                    }
                                }
                                if (okToAddSummon && summoned.GetActorType() != ActorTypes.ITEM)
                                {
                                    ft.AddSummon(summoned);
                                    summoned.summoner = ft;
                                }
                                    //Debug.Log("Loaded summoned actor for " + ft.displayName + " " + id + " " + summoned.displayName);
                                if (!okToAddSummon)
                                {
                                    if (debugSummons) Debug.Log("Not OK to add summon of ID " + id);
                                    invalidIDs.Add(id);
                                }

                                //Debug.Log(eff.originatingActor.displayName);
                            }
                            else
                            {
                                if (debugSummons) Debug.Log("Could not load summoned actor for " + ft.displayName + " " + id);
                                invalidIDs.Add(id);
                            }
                        }
                        foreach (int id in invalidIDs)
                        {
                            ft.summonedActorIDs.Remove(id);
                        }
                    }

                    ft.LinkAndValidateAllAnchoredActors(true);

                    // This will be deprecated eventually.
                    if (ft.anchoredActorsIDs != null && ft.anchoredActorsIDs.Count > 0)
                    {
                        invalidIDs.Clear();
                        foreach (int id in ft.anchoredActorsIDs)
                        {
                            Actor anchored = null;
                            if (dictAllActors.TryGetValue(id, out anchored))
                            {
                                if (anchored.GetActorType() != ActorTypes.ITEM && anchored.actorfaction != Faction.DUNGEON)
                                {
                                    ft.AddAnchor(anchored);
                                    anchored.anchor = ft;
                                }
                            }
                            else
                            {
                                Debug.Log("Could not load originating anchored actor for " + ft.displayName + " " + id);
                                invalidIDs.Add(id);
                            }
                        }
                        foreach (int id in invalidIDs)
                        {
                            ft.anchoredActorsIDs.Remove(id);
                        }
                    }
                    // End deprecation

                    foreach (StatusEffect se in ft.myStats.GetAllStatuses())
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
                                if (eff.originatingActorUniqueID < 1 || (ft.GetActorType() == ActorTypes.HERO && eff.originatingActorUniqueID <= 1))
                                {
                                    eff.originatingActor = ft;
                                }
                                else
                                {
                                    if (dictAllActors.TryGetValue(eff.originatingActorUniqueID, out orig))
                                    {
                                        eff.originatingActor = orig;
                                    }
                                    else
                                    {
                                        //Debug.Log("Could not load originating actor for status " + se.refName + " OA ID " + eff.originatingActorUniqueID + " " + ft.actorRefName + " " + ft.actorUniqueID);
                                        eff.originatingActor = ft; // We're just guessing here.
                                    }
                                }
                            }

                            eff.selfActor = ft;
                        }
                    }

                    // Combatants                    
                    adRemove.Clear();
                    foreach (AggroData ad in ft.combatTargets)
                    {
                        Actor cmb = null;

                        if (dictAllActors.TryGetValue(ad.combatantUniqueID, out cmb) && cmb is Fighter) // 232019 - make sure cmb is a fighter
                        {
                            ad.combatant = cmb as Fighter;
                            if (ft.actorfaction == Faction.ENEMY && ad.combatant == null)
                            {
                                ad.combatant = GameMasterScript.heroPCActor;
                            }
                        }
                        else
                        {
                            adRemove.Add(ad);
                        }
                    }
                    foreach (AggroData ad in adRemove)
                    {
                        ft.combatTargets.Remove(ad);
                    }
                    adRemove.Clear();
                    // Allies
                    foreach (AggroData ad in ft.combatAllies)
                    {
                        Actor cmb = null;
                        if (dictAllActors.TryGetValue(ad.combatantUniqueID, out cmb) && cmb is Fighter) // 232019 - make sure cmb is a fighter
                        {
                            ad.combatant = cmb as Fighter;
                        }
                        else
                        {
                            adRemove.Add(ad);
                        }
                    }
                    foreach (AggroData ad in adRemove)
                    {
                        ft.combatAllies.Remove(ad);
                    }

                    // Last attacked, combat data etc
                    Actor find = null;
                    if (dictAllActors.TryGetValue(ft.lastActorAttackedByUniqueID, out find) && find is Fighter) // 232019 - make sure find is a fighter
                    {
                        ft.lastActorAttackedBy = find as Fighter;
                    }
                    if (dictAllActors.TryGetValue(ft.lastActorAttackedUniqueID, out find) && find is Fighter) // 232019 - make sure find is a fighter
                    {
                        ft.lastActorAttacked = find as Fighter;
                    }

                    if (ft.GetActorType() == ActorTypes.MONSTER)
                    {
                        // Special monster initialization.
                        Monster mon = ft as Monster;
                        if (dictAllActors.TryGetValue(mon.myTargetUniqueID, out find))
                        {
                            mon.SetMyTarget(find);
                        }
                        if (dictAllActors.TryGetValue(mon.myActorOfInterestUniqueID, out find))
                        {
                            mon.myActorOfInterest = find;
                        }
                    }

                    ft.SetBattleDataDirty();

                }
            }
        }

    }

    void SetTreesPlantedMaps()
    {
        for (int i = 0; i < MetaProgressScript.treesPlanted.Length; i++)
        {
            Map tMap;
            if (MapMasterScript.dictAllMaps.TryGetValue(MetaProgressScript.treesPlanted[i].actorMapID, out tMap))
            {
                MetaProgressScript.treesPlanted[i].SetActorMap(tMap);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Could not link up map ID " + MetaProgressScript.treesPlanted[i].actorMapID + " for magic tree");
            }
        }
    }

    void ValidateItemWorldStuff()
    {
        // Is item world portal ACTUALLY open?
        foreach (Stairs st in mms.townMap2.mapStairs)
        {
            if (st.isPortal)
            {
                loadGame_itemWorldPortalOpen = true;
                break;
            }
        }

        if (!loadGame_itemWorldPortalOpen && MapMasterScript.itemWorldOpen
            && !heroPCActor.GetActorMap().IsItemWorld() && heroPCActor.GetActorMap().floor != MapMasterScript.CAMPFIRE_FLOOR &&
            (heroPCActor.GetActorMap().floor < MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR ||
            heroPCActor.GetActorMap().floor > MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR))
        {
            if (Debug.isDebugBuild) Debug.Log("Item World on load is not ACTUALLY open");
            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                if (MapMasterScript.itemWorldMaps[i] == null) continue;
                MapMasterScript.itemWorldMaps[i].clearedMap = true;
                MapMasterScript.theDungeon.maps.Remove(MapMasterScript.itemWorldMaps[i]);
                MapMasterScript.dictAllMaps.Remove(MapMasterScript.itemWorldMaps[i].mapAreaID);
                MapMasterScript.OnMapRemoved(MapMasterScript.itemWorldMaps[i]);
	            ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 0);
            }
            MapMasterScript.itemWorldOpen = false;
            ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 0);
            GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
            MapMasterScript.itemWorldItem = null;
        }
    }

    void ValidateHeroPetAttributesOrResetIt()
    {
        if (heroPCActor.GetMonsterPetID() > 0)
        {
            Monster pet = TryLinkActorFromDict(heroPCActor.GetMonsterPetID()) as Monster; // 232019 explicit type check
            if (pet != null)
            {
                pet.ApplyHeroPetAttributes();
            }
            else
            {
                Debug.Log("Error linking player pet ID: " + heroPCActor.GetMonsterPetID());
                heroPCActor.ResetPetData();
            }
        }
    }

    void ValidateLegendaryItemReferencesToRemove()
    {
        List<Actor> legendaryRefsToRemove = new List<Actor>();

        ActorTable legendaries = LootGeneratorScript.GetLootTable("legendary");

        foreach (Actor legEntry in legendaries.actors)
        {
            if (heroPCActor.FoundLegItem(legEntry.actorRefName))
            {
                legendaryRefsToRemove.Add(legEntry);
            }
        }

        foreach (Actor act in legendaryRefsToRemove)
        {
            legendaries.RemoveFromTable(act.actorRefName);
        }
    }

    void ValidateOrbUsedToOpenItemWorld()
    {
        int itemWorldOrb = heroPCActor.ReadActorData("orbusedtoopenitemworld");
        if (itemWorldOrb >= 0 && MapMasterScript.itemWorldOpen)
        {
            Item orb = TryLinkActorFromDict(itemWorldOrb) as Item;
            if (orb != null)
            {
                MapMasterScript.orbUsedToOpenItemWorld = orb;
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Couldn't find player's orb used to open IW.");
            }
        }
    }

    void ValidateTheBanker()
    {
        bool shouldHaveBanker = SharedBank.ShouldUseSharedBankForCurrentGame();

        // Now ensure the bankers items don't conflict with anything, so assign fresh IDs...
        NPC banker = mms.townMap.FindActor("npc_banker") as NPC;

        if (shouldHaveBanker && banker == null)
        {
            if (Debug.isDebugBuild) Debug.Log("We should have banker, but banker is null, so rebuild maps.");
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
            GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);

            // Failure occurred.
            GameMasterScript.mapsLoadedSuccessfully = false;
            SceneManager.LoadScene("Gameplay");
            return;
        }

        if (banker != null)
        {
            if (!shouldHaveBanker)
            {
                RemoveBankerFromMap(banker);
            }
            else
            {
                List<Item> items = SharedBank.allItemsInBank;

                items.RemoveAll(a => a == null);

                foreach (Item itm in items)
                {
                    itm.collection = banker.myInventory;
                }
            }
        }        
        CopySharedBankIntoLocalBanker(banker);

        /*
        foreach (Item itm in banker.myInventory.GetInventory())
        {
            //itm.SetUniqueIDAndAddToDict(); // This line is now done in the CopySharedBank function
            if (itm.legendary && !heroPCActor.FoundLegItem(itm.actorRefName))
            {
                heroPCActor.SetActorData("legfound_" + itm.actorRefName, 1);
            }
        } */

        if (banker != null)
        {
            dictAllActors.Remove(banker.actorUniqueID);
            dictAllActors.Add(banker.actorUniqueID, banker);
            if (banker != TryLinkActorFromDict(banker.actorUniqueID))
            {
                Debug.Log("<color=red>BANKERS ARE NOT THE SAME!!!</color>");
            }
        }

    }

    void CheckForDeadHeroPet()
    {
        Monster mPet = heroPCActor.GetMonsterPet();
        if (mPet != null)
        {
            if (mPet.destroyed)
            {
                // Pet dead for whatever reason? Unlink it from actor.
                heroPCActor.ResetPetData();
                heroPCActor.RemoveAnchor(mPet);
                heroPCActor.RemoveSummon(mPet);
            }
            else
            {
                if (mPet.moveRange == 0)
                {
                    mPet.moveRange = 1;
                }
            }

        }
    }    

    void CreateAndValidateTargetingLines()
    {
        foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            int targetID = m.ReadActorData("locktarget");
            // Is there an ability actually being used?
            if (!m.storingTurnData) continue;
            if (targetID != -1)
            {
                Actor tryLink = TryLinkActorFromDict(targetID);
                if (tryLink != null)
                {
                    TargetingLineScript.CreateTargetingLine(m.GetObject(), tryLink.GetObject());
                }
            }
        }
    }

    void RepositionHeroSummonMonsterWrathBars()
    {
        foreach (Actor act in heroPCActor.summonedActors)
        {
            if (act.IsFighter())
            {
                Fighter ft = act as Fighter;
                ft.RepositionWrathBarIfNeeded();
            }
        }
    }

    void VerifyWildChildUnlock()
    {
        // Verify wild child is unlocked if it SHOULD be unlocked.
        if (ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) >= 1 || ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) >= 4)
        {
            if (!SharedBank.CheckIfJobIsUnlocked(CharacterJobs.WILDCHILD))
            {
                SharedBank.UnlockJob(CharacterJobs.WILDCHILD);
            }
        }
    }

    void ValidateHeroRunicCrystal()
    {
        Monster crystal = heroPCActor.GetSummonByRef("mon_runiccrystal") as Monster;
        if (crystal != null)
        {
            int runicCount = crystal.myStats.CheckStatusQuantity("runic_charge");
            if (crystal.wrathBarScript != null)
            {
                crystal.wrathBarScript.UpdateWrathCount(runicCount);
            }
            else
            {
                crystal.EnableWrathBarIfNeeded();
                crystal.wrathBarScript.UpdateWrathCount(runicCount);
            }

        }
    }

    void RelinkAllStairs()
    {
        foreach (Stairs st in loadGame_stairsToTryRelinking)
        {
            bool success = mms.TryRelinkStairs(st);
            if (!success && st.dungeonFloor != MapMasterScript.CAMPFIRE_FLOOR)
            {
                if (Debug.isDebugBuild) Debug.Log("Error connecting stairs on floor " + st.dungeonFloor + ". Stairs Up? " + st.stairsUp + " AID " + st.actorUniqueID + " display name " + " portal? " + st.isPortal + " " + st.displayName + " pos " + st.GetPos() + " on floor " + st.dungeonFloor + " could not find destination " + st.newLocationID);
            }
        }
    }

    static void RemoveBankerFromMap(NPC banker)
    {
        if (banker == null) return;
        Map townMap = MapMasterScript.theDungeon.FindFloor(100);
        //Actor banker = townMap.FindActor("npc_banker");
        townMap.RemoveActorFromLocation(banker.GetPos(), banker);
        townMap.RemoveActorFromMap(banker);

        if (banker.myMovable == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Banker exists but is not spawned?");
            return;
        }

        banker.myMovable.FadeOutThenDie();

        if (Debug.isDebugBuild) Debug.Log("Removing banker permanently.");
    }

    static void TryCopySharedCorralIntoLocalCorral()
    {
        if (!SharedBank.ShouldUseSharedBankForCurrentGame())
        {
            if (Debug.isDebugBuild) Debug.Log("We cannot copy shared corral into local corral given this game type.");
            return;
        }

        List<TamedCorralMonster> monstersToConcatenate = new List<TamedCorralMonster>();

        foreach(TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == null) continue;
            if (tcm.monsterObject.ReadActorData("ngpluspet") == 1) 
            {
                monstersToConcatenate.Add(tcm);
            }
            if (tcm.monsterObject.actorUniqueID < SharedBank.CORRAL_ID_ASSIGNER_BASE)
            {
                if (Debug.isDebugBuild) Debug.Log("Uh oh! " + tcm.monsterObject.PrintCorralDebug() + " has a non-valid actor unique ID for corral monster.");
            }
        }

        MetaProgressScript.localTamedMonstersForThisSlot = SharedCorral.tamedMonstersSharedWithAllSlots;
        if (Debug.isDebugBuild) Debug.Log("Copied all pets from shared bank into local slot. There are: " + SharedCorral.tamedMonstersSharedWithAllSlots.Count + " pets total.");

        foreach(TamedCorralMonster tcm in monstersToConcatenate)
        {
            if (Debug.isDebugBuild) Debug.Log("Port over NG+ pet: " + tcm.monsterObject.PrintCorralDebug());
            MetaProgressScript.AddPetToLocalSlotCorralList(tcm);
            tcm.monsterObject.RemoveActorData("ngpluspet");
        }
       
    }
}
