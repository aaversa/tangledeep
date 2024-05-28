using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class MapMasterScript
{

    static MapChangeInfo currentMapChangeInfo;

    /// <summary>
    /// Return TRUE if we save the game here.
    /// </summary>
    /// <param name="originDestinationMap"></param>
    /// <param name="originSpawnPosition"></param>
    /// <param name="forced"></param>
    /// <param name="fadeInAfterMapSave"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    public bool SwitchMaps(Map originDestinationMap, Vector2 originSpawnPosition, bool forced,
        bool fadeInAfterMapSave = false, float fadeTime = 0.5f)
    {

        bool debug = false;
#if UNITY_EDITOR
            
#endif

        if (currentMapChangeInfo == null) currentMapChangeInfo = new MapChangeInfo();
        currentMapChangeInfo.destinationMap = originDestinationMap;
        currentMapChangeInfo.spawnPosition = originSpawnPosition;
        currentMapChangeInfo.anyCampfire = false;

#if UNITY_SWITCH
        Resources.UnloadUnusedAssets();
        GC.Collect();
#endif
        GameMasterScript.heroPCActor.SetActorData("num_lieutenants", 0);

        // AA: New way to combat some memory leaks that are happening due to dynamic texture creation
        // This is really minor but memory usage is increasing by 1-3mb per level load. This SHOULD fix it 
        // and does not appear to have any adverse effects...? We just don't need to run it constantly.
        int levelSwitchesSinceUnload = GameMasterScript.gmsSingleton.ReadTempGameData("levels_since_unload");
        levelSwitchesSinceUnload++;
        if (levelSwitchesSinceUnload >= 5)
        {
            Resources.UnloadUnusedAssets();
        }

        if (debug && Debug.isDebugBuild) Debug.Log("SwitchMaps top of function.");

        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("pet_duel"))
        {
            PetPartyUIScript.EndPetDuel(GameMasterScript.heroPCActor.GetMonsterPet());
        }

        GameMasterScript.heroPCActor.SetActorData("turn_entered_floor", GameMasterScript.turnNumber);
        GameMasterScript.heroPCActor.CleanStuckVisualFX();
        GameMasterScript.heroPCActor.ValidateAndFixStats(true); // New 11/20 to make sure hero's stats don't get fuzzled up
        GameMasterScript.heroPCActor.SetActorData("steps_startfloor", GameMasterScript.heroPCActor.stepsTaken);

        BossHealthBarScript.DisableBoss();
        GameMasterScript.heroPCActor.SetActorData("sideareawarp", 0);
        if (currentMapChangeInfo.destinationMap == null)
        {
            Debug.Log("WARNING: Null destination map? Why?");
            return false;
        }
        if (UIManagerScript.GetWindowState(UITabs.SHOP))
        {
            ShopUIScript.CloseShopInterface();
            UIManagerScript.CloseDialogBox();
        }


        if (currentMapChangeInfo.destinationMap.floor == 0 && GameMasterScript.heroPCActor.ReadActorData("tutorial_finished") < 1
            && GameMasterScript.heroPCActor.ReadActorData("enteredtutorial") < 1 && !SharaModeStuff.IsSharaModeActive())
        {
            GameMasterScript.heroPCActor.SetActorData("tutorial_finished", 1);
            currentMapChangeInfo.destinationMap = theDungeon.FindFloor(MapMasterScript.TUTORIAL_FLOOR);
        }

        if (currentMapChangeInfo.destinationMap.floor == MapMasterScript.RIVERSTONE_WATERWAY_START)
        {
            GameMasterScript.heroPCActor.SetActorData("tutorial_finished", 1);
        }
        if (currentMapChangeInfo.destinationMap.floor == TUTORIAL_FLOOR || currentMapChangeInfo.destinationMap.floor == TUTORIAL_FLOOR_2)
        {
            GameMasterScript.heroPCActor.SetActorData("enteredtutorial", 1);
        }

        RemoveCrabGrabsAndBadAnchors();

        //Vector2 testSpawn = CheckForCampfireFloorOrCreateNewCampfire(destinationMap, out anyCampfire);

        CheckForCampfireFloorOrCreateNewCampfire();

        //if (currentMapChangeInfo.anyCampfire) originSpawnPosition = currentMapChangeInfo.spawnPosition;

        if (debug && Debug.isDebugBuild) Debug.Log("Campfire checks done.");

        currentMapChangeInfo.destinationMap.LoadAndPlayMusic();
        
        if (debug && Debug.isDebugBuild) Debug.Log("SwitchMaps: LoadAndPlayMusic was called, now I am playing " + currentMapChangeInfo.destinationMap.musicCurrentlyPlaying);

        GameMasterScript.heroPCActor.visibleTilesArray = new bool[currentMapChangeInfo.destinationMap.columns, currentMapChangeInfo.destinationMap.rows];

        //Place the hero in a special start point
        if (GameMasterScript.returningToTownAfterKO)
        {
            if (SharaModeStuff.IsSharaModeActive())
            {
                currentMapChangeInfo.spawnPosition = new Vector2(5f, 5f);
            }
            else
            {
                currentMapChangeInfo.spawnPosition = new Vector2(10f, 4f);
            }

            GameMasterScript.returningToTownAfterKO = false;
        }

        foreach (Actor act in activeMap.actorsInMap)
        {
            List<Actor> toRemove = new List<Actor>();

            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                if (act.summoner != null)
                {
                    if (act.summoner.actorfaction == Faction.PLAYER)
                    {
                        act.summoner.RemoveSummon(act); // Redundant?
                        toRemove.Add(act);
                        GameMasterScript.AddToDeadQueue(act);

                    }
                }
            }
        }

        if (!GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(currentMapChangeInfo.destinationMap.mapAreaID) 
            && currentMapChangeInfo.destinationMap.IsMainPath() && !currentMapChangeInfo.destinationMap.IsBossFloor())
        {
            AdjustFountainsForMapOnFirstEntry(currentMapChangeInfo.destinationMap);
        }

        if (debug && Debug.isDebugBuild) Debug.Log("Actors, fountains, summons cleaned.");

        GameMasterScript.heroPCActor.ClearCombatTargets(); // New.

        GameMasterScript.gmsSingleton.ProcessDeadQueue(activeMap);

        DestroyObjects();
        RemoveActorFromMap(heroPCActor);

        MysteryDungeonManager.exitingMysteryDungeon = false;

        //Debug.Log("Currently on " + currentFloor + " and adding " + floorMod);

        Map prevMap = activeMap;

        if (activeMap.floor == CAMPFIRE_FLOOR) // Campfire floor, so REAL prev map is something else.
        {
            foreach (Actor act in heroPCActor.summonedActors)
            {
                activeMap.RemoveActorFromMap(act);
            }
            Map getMap = GetMapFromDict(GameMasterScript.heroPCActor.ReadActorData("precampfirefloor"));
            if (getMap != null)
            {
                prevMap = getMap;
            }
            else
            {
                Debug.Log("Couldn't link up previous player map from dictionary, " + GameMasterScript.heroPCActor.ReadActorData("precampfirefloor"));
            }
        }


        activeMap = currentMapChangeInfo.destinationMap;

        if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            SharaModeStuff.DisableStairsToRiverstoneInCurrentMap();
        }

        mapLoaded = true;

        if (activeMap.floor == 0 || activeMap.floor == TUTORIAL_FLOOR)
        {
            GameMasterScript.heroPCActor.SetActorData("entereddungeon", 1);
        }

        GameMasterScript.cameraScript.SetBGColor(activeMap.dungeonLevelData.bgColor);

        //Debug.Log("Switching to map floor " + activeMap.floor + " layout " + activeMap.dungeonLevelData.layoutType + " from " + prevMap.floor);

        GameMasterScript.heroPCActor.SetActorMap(activeMap);
        GameMasterScript.heroPCActor.actorMapID = activeMap.mapAreaID;

        traversed = new bool[activeMap.rows, activeMap.columns];
        tChecked = new bool[activeMap.rows, activeMap.columns];
        // TODO - BETTER WAY OF SELECTING AREAS.

        currentFloor = activeMap.floor;
        heroPCActor.dungeonFloor = currentFloor;

        if (debug && Debug.isDebugBuild) Debug.Log("Part 10 or something");

        Actor healAct;
        Fighter ft;
        for (int a = 0; a < activeMap.actorsInMap.Count; a++)
        {
            healAct = activeMap.actorsInMap[a];
            if (healAct.GetActorType() == ActorTypes.MONSTER)
            {
                ft = healAct as Fighter;
                if (!ft.myStats.IsAlive())
                {
                    GameMasterScript.AddToDeadQueue(ft);
                }
                else if (healAct.actorfaction != Faction.PLAYER || healAct.actorRefName == "mon_targetdummy")
                {
                    ft.myStats.HealToFull();
                    ft.myStats.RemoveTemporaryNegativeStatusEffects();
                }

            }
        }

        heroPCActor.actionTimer = 100f;
        UIManagerScript.RefreshPlayerCT(false);

        MapEntryRegenerateImageExtraAndMapOverlays();

        UIManagerScript.UpdateDungeonText();

        // Find player movement was here

        //if (Debug.isDebugBuild) Debug.Log("Check 2. " + currentMapChangeInfo.spawnPosition);

        Vector2 movePlayerToLocation = FindPositionForHero(currentMapChangeInfo, prevMap, forced);

        //if (Debug.isDebugBuild) Debug.Log("Updated position is " + movePlayerToLocation);

        currentMapChangeInfo.spawnPosition = movePlayerToLocation;

        if (prevMap != null)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("prevfloor", prevMap.floor);
        }

        if (debug && Debug.isDebugBuild) Debug.Log("Part 11 or something");

        // Stair sanity check.
        // Returning to the dungeon from town counts as a stairway, which is fine.
        // If you have shoveled your way into a space with <=2 free adjacent spots,
        // StairSanityCheck will panic and clear every tile around you.
        // Let's avoid that if we can by not running a sanity check if you are returning via portal.
        if (prevMap != null &&
            prevMap.floor != TOWN_MAP_FLOOR &&
            prevMap.floor != TOWN2_MAP_FLOOR &&
            activeMap.dungeonLevelData.specialRoomTemplate == null &&
            activeMap.dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)
        {
            activeMap.StairSanityCheck(currentMapChangeInfo.spawnPosition);
        }

        activeMap.CheckForStackedActors(currentMapChangeInfo.spawnPosition, GameMasterScript.heroPCActor, false);

        MapEntryMoveHeroAndCamera(currentMapChangeInfo.spawnPosition);

        if (debug && Debug.isDebugBuild) Debug.Log("Part 12 or something");

        MapEntryUpdateQuests();
        SeasonalFunctions.CheckForSeasonalAdjustmentsInNewMap();
        OnMapEntryBarks();

        RumorTextOverlay.OnNewFloorEntry();

        MapEntryFixSummonedActors(prevMap);

        BattleTextManager.InitializeTextObjectSources();

        float localBigModeChance = CHANCE_ITEMWORLD_BIGMODE;
        float localCostumePartyChance = CHANCE_ITEMWORLD_COSTUMEPARTY;
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("luciddreamer"))
        {
            localBigModeChance *= 1.2f;
            localCostumePartyChance *= 1.5f;
        }

        ItemDreamFunctions.CheckForCostumeParty(localCostumePartyChance);

        if (activeMap.IsItemWorld() && ItemDreamFunctions.IsItemDreamNightmare())
        {
            ItemDreamFunctions.RelocateKingToPlayer();
        }
        if (activeMap.IsMysteryDungeonMap())
        {
            MysteryDungeonManager.RelocateChaserToPlayerIfNeeded();
        }

    if (debug && Debug.isDebugBuild) Debug.Log("Part 13 or something");

        if (RandomJobMode.IsCurrentGameInRandomJobMode() && !activeMap.IsItemWorld()) RandomJobMode.RemoveStairsInMapThatPointToBadConnection(activeMap);

        MapEntrySpawnPrefabs();
        MetaProgressScript.VerifyTreeState();

        activeMap.CheckForLevelScalingToPlayerLevelOnLoad();

        DungeonGenerationAlgorithms.VerifyStairTransparencies();

        bool floorDebug = false;

#if UNITY_EDITOR
    floorDebug = false;
#endif

        // This block has been moved so we don't get floating "New Item Displays" not attached to any NPC
        if ((GameMasterScript.heroPCActor.lowestFloorExplored < activeMap.effectiveFloor && !activeMap.dungeonLevelData.deepSideAreaFloor)
            || floorDebug)
        {
            if ((activeMap.effectiveFloor != TUTORIAL_FLOOR && activeMap.effectiveFloor != TUTORIAL_FLOOR_2) || floorDebug)
            {
                GameMasterScript.heroPCActor.SetLowestFloorExplored(activeMap.effectiveFloor);
            }
        }

        GameMasterScript.SetLevelChangeState(false);

        GameMasterScript.gmsSingleton.UpdateHeroSpriteMaterial();

        UpdateSpriteRenderersOnLoad(); // Why are sprite renderers being disabled by default?        

        if (debug && Debug.isDebugBuild) Debug.Log("Part 14 or something");


        ItemDreamFunctions.CheckForBigMode(localBigModeChance);

        foreach (Actor act in activeMap.actorsInMap)
        {
            try { act.myMovable.CheckTransparencyBelow(); }
            catch   //#questionable_try_block
            {
                //Debug.Log("Spawn transparency check failed on map load, " + act.GetPos() + " " + act.dungeonFloor + " " + act.actorRefName);
            }
        }

        CheckRevealMap();

        bool wasMinimapOpen = MapEntryAdjustGridsMeshesAndOverlays();

        UpdateMapObjectData(); // Was above meshes?

        UpdateSpriteRenderersOnLoad();
        if (wasMinimapOpen)
        {
            if (Debug.isDebugBuild) Debug.Log("Minimap WAS open during map change.");
            MinimapUIScript.SetMinimapToSpecificState(MinimapUIScript.prevMinimapState);
        }
        else
        {
            MinimapUIScript.StopOverlay();
        }

        if (debug && Debug.isDebugBuild) Debug.Log("Part 15 or something");

        BattleTextManager.AddObjectToDict(heroPCActor.GetObject());
        cameraScript.SnapPosition(heroPCActor.GetPos());

        // Is below line necessary anymore?
        //cameraScript.SetCameraPosition(heroPCActor.GetPos()); 

        ItemDreamFunctions.CheckForSpinMode(CHANCE_ITEMWORLD_SPIN);

        MapEntryItemBossCheck();
        MapEntryPainterQuest();
        MapEntryMoveJunkOffStaircase();
        MapEntryDungeonDigest();

        GameMasterScript.CheckForTextOverlay();

        // Pre and post boss dialog.

        GameMasterScript.gmsSingleton.SetTempGameData("exitingdream", 0);

        GameMasterScript.heroPCActor.VerifyAbilities();

        if (PlayerOptions.autoAbandonTrivialRumors) GameMasterScript.heroPCActor.EvaluateTrivialRumors();

        GameMasterScript.heroPCActor.myStats.CheckConsumeAllStatuses(StatusTrigger.SWITCHMAPS);

        CheckForMapStartConversationsAndEvents(prevMap);

        if (activeMap.IsBossFloor())
        {
            GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("status_escapedungeon");
        }
        
        if (debug && Debug.isDebugBuild) Debug.Log("Part 16 or something");

        CombatResultsScript.CheckForSideAreaClear(MapMasterScript.activeMap);
        activeMap.PrintMonstersRemainingToLogIfNecessary();

        if (!VerifyEntryStairsAreAccessible())
        {
            if (activeMap.IsItemWorld())
            {
                ForciblyTunnelDisconnectedStairs(activeMap);
                MapMasterScript.RebuildMapMesh();
                if (MinimapUIScript.GetOverlay())
                {
                    MinimapUIScript.StopOverlay();
                    MinimapUIScript.GenerateOverlay();
                }
            }
            else
            {
                RebuildMapWithInaccessibleStairsAndReturnToPrevious(prevMap);
                return false;
            }                        
        }

        if (debug && Debug.isDebugBuild) Debug.Log("Part 17 or something");

        bool saved = false;

        bool escapePortalUsed = GameMasterScript.gmsSingleton.ReadTempGameData("escapeportalused") == 1;
        if (GameMasterScript.gmsSingleton.ReadTempGameData("waypointtravel") == 1)
        {
            escapePortalUsed = false;
        }

        bool prevMapIsJobTrialFloor = prevMap != null && (prevMap.floor == MapMasterScript.JOB_TRIAL_FLOOR || prevMap.floor == MapMasterScript.JOB_TRIAL_FLOOR + 1);

        try
        {

            if (escapePortalUsed && !GameMasterScript.endingItemWorld && !prevMapIsJobTrialFloor
                && ProgressTracker.CheckProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO) != 1)
            {
                // Item world may be open, so save AFTER this function.
                
            }
            else
            {
                GameMasterScript.gmsSingleton.SaveTheGame(autoSave: true);
                saved = true;
            }
                        
        }
        catch (Exception e)
        {
            Debug.Log("Autosave failed! " + e.ToString());
        }

        foreach (Actor act in activeMap.actorsInMap)
        {
            act.UpdateSpriteOrder();
        }

        if (debug && Debug.isDebugBuild) Debug.Log("Part 18 or something");

        GameMasterScript.heroPCActor.myMovable.IncreaseSortingOrder(); // so player draws over stairs on load
        
        if (fadeInAfterMapSave)
        {
            GC.Collect(); // Might as well collect since the screen is black right now.            
            StartCoroutine(IWaitUntilSavedToFadeIn(fadeTime));
        }

        return saved;
    } // END SWITCH MAPS

    IEnumerator IWaitUntilSavedToFadeIn(float fadeTime)
    {
        
        while (GameMasterScript.gmsSingleton.CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            yield return null;
        }
        UIManagerScript.FadeIn(fadeTime * 0.5f);
        GuideMode.OnFullScreenUIClosed();
    }



    /// <summary>
    /// Writes to currentMapChangeInfo
    /// </summary>
    /// <param name="destinationMap"></param>
    /// <param name="anyCampfireFloor"></param>
    /// <returns></returns>
    void CheckForCampfireFloorOrCreateNewCampfire()
    {
        currentMapChangeInfo.anyCampfire = false;
        bool okToCreateCampfire = false;


        //If we aren't on a campfire floor, the next one might be one!
        if (MapMasterScript.activeMap.floor != CAMPFIRE_FLOOR)
        {
            //If we're heading up the main path or in an itemworld and not headed to floor 0, we can try for camp.
            //Also, if we're in a mystery dungeon!
            if ((currentMapChangeInfo.destinationMap.IsMainPath() || currentMapChangeInfo.destinationMap.IsItemWorld() 
                || currentMapChangeInfo.destinationMap.IsMysteryDungeonMap()) &&
                currentMapChangeInfo.destinationMap.floor != 0)
            {

                bool husynForceEncounter = false;

                //Neither of those flags can be true in Shara Mode
                if (!SharaModeStuff.IsSharaModeActive())
                {
                    //if the next map is in itemworld and not some other flag, it's ok to be a campfire.
                    if (currentMapChangeInfo.destinationMap.IsItemWorld() &&
                        GameMasterScript.heroPCActor.ReadActorData("iw_map_" + currentMapChangeInfo.destinationMap.mapAreaID) != 1)

                    {
                        okToCreateCampfire = true;
                    }

                    // if we're in a mystery dungeon its always ok
                    if (currentMapChangeInfo.destinationMap.IsMysteryDungeonMap())

                    {
                        okToCreateCampfire = true;
                    }

                    //If we haven't unlocked the HUSYN and are about to hit the 17th floor, set this flag.
                    husynForceEncounter = currentMapChangeInfo.destinationMap.floor == 17 &&
                                          !SharedBank.CheckIfJobIsUnlocked(CharacterJobs.HUSYN);

                }

                //If we have not explored the floor above, OR
                //we're in an itemworld and it is OK to make one, OR
                //we have to have one because husyn,
                if (!GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(currentMapChangeInfo.destinationMap.mapAreaID) ||
                    okToCreateCampfire ||
                    husynForceEncounter)
                {
                    var campfireMap = TryCreateCampfireMap(husynForceEncounter);
                    if (campfireMap != null)
                    {
                        currentMapChangeInfo.destinationMap = campfireMap;
                    }
                }
            }
            else if (currentMapChangeInfo.destinationMap.IsMainPath() && SharaModeStuff.IsSharaModeActive() 
                && !GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(currentMapChangeInfo.destinationMap.mapAreaID))
            {
                currentMapChangeInfo.anyCampfire = SharaModeStuff.TickCampfireCounterAndCheckForSpawn(currentMapChangeInfo);                
                
            }
        }
        // Currently in campfire floor.
        else
        {
            if (currentMapChangeInfo.destinationMap.heroStartTile == Vector2.zero)
            {
                foreach (Stairs st in currentMapChangeInfo.destinationMap.mapStairs)
                {
                    if (st.newLocationID == GameMasterScript.heroPCActor.ReadActorData("precampfirefloor"))
                    {
                        currentMapChangeInfo.spawnPosition = st.GetPos();
                        currentMapChangeInfo.anyCampfire = true;
                        break;
                    }
                }
            }
        }
    }

    void MapEntryRegenerateImageExtraAndMapOverlays()
    {
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Top of map entry regeneration function.");

        // New image overlay
        if (imageOverlay != null)
        {
            Destroy(imageOverlay);
            imageOverlay = null;
        }

        foreach (GameObject go in extraOverlays)
        {
            Destroy(go);
        }
        extraOverlays.Clear();


        SpawnMapOverlays();
    }

    void MapEntryMoveHeroAndCamera(Vector3 movePlayerToLocation)
    {        
        GameMasterScript.heroPC.GetComponent<Movable>().SetPosition(movePlayerToLocation);
        GameMasterScript.heroPCActor.SetCurPos(movePlayerToLocation);
        bool success = AddActorToLocation(movePlayerToLocation, heroPCActor);
        if (success) { AddActorToMap(heroPCActor); }

        if (!activeMap.dungeonLevelData.bossArea)
        {
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (st.NewLocation == null) continue;
                if (GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(st.NewLocation.mapAreaID) || !st.NewLocation.mapIsHidden)
                {
                    st.EnableActor();
                }
            }
        }

        CameraController.UpdateCameraPosition(heroPCActor.GetPos(), true);
    }

    public void MapEntryUpdateQuests()
    {
        if (GameMasterScript.heroPCActor.myQuests.Count > 0)
        {
            QuestScript completed = null;
            foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
            {
                if (qs.qType == QuestType.FINDAREA)
                {
                    if (qs.targetMap == activeMap)
                    {
                        completed = qs;
                        break;
                    }
                }
            }
            if (completed != null)
            {
                QuestScript.CompleteQuest(completed);
                //GameMasterScript.heroPCActor.myQuests.Remove(completed);
            }
            GameMasterScript.heroPCActor.myQuests.Remove(completed);
        }

        foreach(Actor act in activeMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster m = act as Monster;
            if (!m.foodLovingMonster) continue;
            if (GameMasterScript.heroPCActor.HasSpecificMonsterInFoodLovingQuests(m)) continue;
            m.RemoveItemLovingAttributeFromSelf();
        }
    }

    void MapEntryFixSummonedActors(Map prevMap)
    {
        bool success = false;
        if (GameMasterScript.heroPCActor.summonedActors != null && GameMasterScript.heroPCActor.summonedActors.Count > 0)
        {
            List<Actor> remove = new List<Actor>();
            foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Fighter pet = act as Fighter;
                    // New 9/28/17 - if player has "pets" that aren't in player's faction, they should be removed from pet list HERE.
                    if (pet.actorfaction != Faction.PLAYER)
                    {
                        remove.Add(pet);
                    }
                    else
                    {
                        if (pet.myStats.IsAlive())
                        {
                            prevMap.RemoveActorFromMap(act);
                            act.dungeonFloor = currentFloor;
                            Vector2 newPos = heroPCActor.GetPos();
                            MapTileData mtd = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true, true);
                            act.SetPos(mtd.pos);
                            success = AddActorToLocation(mtd.pos, act);
                            if (success)
                            {
                                AddActorToMap(act);
                            }
                        }
                        else
                        {
                            remove.Add(pet);
                            Debug.Log("Don't bring " + pet.actorRefName + " " + pet.actorUniqueID + " with hero on map switch.");
                        }
                    }


                }
            }
            foreach (Actor act in remove)
            {
                GameMasterScript.heroPCActor.RemoveSummon(act);
            }
        }
    }

    void MapEntrySpawnPrefabs()
    {
        SpawnAllDecor(activeMap);
        SpawnAllProps(activeMap);
        SpawnAllItems(activeMap);
        SpawnAllMonsters(activeMap);
        SpawnAllNPCs(activeMap);
        SpawnAllStairs(activeMap);
    }

    /// <summary>
    /// Returns whether minimap was open
    /// </summary>
    /// <returns></returns>
    bool MapEntryAdjustGridsMeshesAndOverlays()
    {
        mapGridMesh.size_x = activeMap.columns;
        mapGridMesh.size_z = activeMap.rows;
        mapGridMesh.BuildMesh();

        mapTileMesh.size_x = activeMap.columns;
        mapTileMesh.size_z = activeMap.rows;
        mapTileMesh.BuildMesh();
        fogOfWarTMG.size_x = activeMap.columns;
        fogOfWarTMG.size_z = activeMap.rows;
        fogOfWarTMG.BuildMesh();

        bool wasMinimapOpen = MinimapUIScript.GetOverlay();

        MinimapUIScript.StopOverlay();

        MinimapUIScript.BaseMapChanged(activeMap.columns, activeMap.rows);
        MinimapUIScript.RefreshMiniMap();

        try { UpdateFOWOverlay(true); }
        catch   //#questionable_try_block
        {
            Debug.Log("Error updating FOW overlay. " + activeMap.floor);
        }

        return wasMinimapOpen;
    }

    private static bool VerifyEntryStairsAreAccessible()
    {
        if (activeMap.IsBossFloor()) return true;
        if (activeMap.dungeonLevelData.fastTravelPossible) return true;
        if (activeMap.IsTownMap()) return true;
        if (activeMap.dungeonLevelData.layoutType == DungeonFloorTypes.SPECIAL) return true;
        if (activeMap.dungeonLevelData.GetMetaData("frogdungeon") == 1) return true;

        int closestDistance = 99;
        Stairs closestStairs = null;
        foreach(Stairs st in activeMap.mapStairs)
        {
            int dist = MapMasterScript.GetGridDistance(st.GetPos(), GameMasterScript.heroPCActor.GetPos());
            if (dist < closestDistance)
            {
                closestStairs = st;
                closestDistance = dist;
            }
        }

        if (closestStairs == null) return true;

        if (activeMap.floor == 221) return true;

        bool accessible = activeMap.FloodFillToSeeIfGivenTileIsConnectedToBiggestCavern(activeMap.GetTile(closestStairs.GetPos()));

        return accessible;
    }

    void MapEntryItemBossCheck()
    {
        if (activeMap.IsItemWorld())
        {
            if (activeMap == itemWorldMaps[itemWorldMaps.Length - 1] && !GameMasterScript.endingItemWorld)
            {
                bool foundItemBoss = false;
                foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = act as Monster;
                        if (mn.isItemBoss)
                        {
                            foundItemBoss = true;
                            break;
                        }
                    }
                }

                if (!foundItemBoss && !ItemDreamFunctions.HasPlayerKilledMemoryKing() && !ItemDreamFunctions.HasPlayerKilledNightmarePrince() && !ItemDreamFunctions.IsItemDreamNightmare())
                {
                    Monster newMemoryKing = activeMap.SpawnRandomMonster(true, true);
                    newMemoryKing.displayName = "<color=yellow>" + StringManager.GetString("mon_memory_king_disp") + " " + newMemoryKing.myTemplate.monsterName + "</color>";

                    Item upgrade = MapMasterScript.itemWorldItem;
                    if (upgrade == null)
                    {
                        GameLogScript.GameLogWrite(UIManagerScript.redHexColor + "An error has occurred; the Memory King is dead / not found, and the Item World item is null... Please send your output_log and savegame files.", GameMasterScript.heroPCActor);
                        GameLogScript.GameLogWrite(MapMasterScript.itemWorldItemID + " is the ID.", GameMasterScript.heroPCActor);
                        GameMasterScript.endingItemWorld = true;
                    }
                    else
                    {
                        newMemoryKing.myInventory.AddItemRemoveFromPrevCollection(upgrade, false);
                        newMemoryKing.isItemBoss = true;
                        newMemoryKing.isBoss = true;
                    }
                }
            }
        }
    }

    void MapEntryPainterQuest()
    {
        // QUEST - Painter
        if (GameMasterScript.heroPCActor.ReadActorData("painterquestfloor") == activeMap.floor && GameMasterScript.heroPCActor.ReadActorData("painterquest") == 1)
        {
            GameMasterScript.heroPCActor.SetActorData("painterquest", 2);
            Actor toRemove = null;
            foreach (Actor act in MapMasterScript.singletonMMS.townMap.actorsInMap)
            {
                if (act.actorRefName == "npc_painter")
                {
                    toRemove = act;
                    break;
                }
            }
            if (toRemove != null)
            {
                singletonMMS.townMap.RemoveActorFromLocation(toRemove.GetPos(), toRemove);
                singletonMMS.townMap.RemoveActorFromMap(toRemove);
            }
            NPC painter = NPC.CreateNPC("npc_painter");
            MapTileData bestTile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true, false);
            int tries = 0;
            while (bestTile.GetStairsInTile() != null)
            {
                tries++;
                if (tries > 500)
                {
                    Debug.Log("Couldnt find a spot for Talrose on " + activeMap.floor);
                }
                bestTile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true, false);
            }

            {
                activeMap.PlaceActor(painter, bestTile);
                painter.SetPos(bestTile.pos);
                UIManagerScript.StartConversation(painter.GetConversation(), DialogType.STANDARD, painter);
                SpawnNPC(painter);
                painter.myMovable.SetInSightAndSnapEnable(true);
            }
        }
    }

    static void OnMapEntryBarks()
    {
        foreach (Actor act in activeMap.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.NPC && act.actorRefName != "npc_banker" && act.actorRefName != "npc_foodcart")
            {
                NPC merchCheck = act as NPC;
                if ((merchCheck.playerLastLowestLevelVisited != GameMasterScript.heroPCActor.lowestFloorExplored) && (((merchCheck.shopRef != null) && (merchCheck.shopRef != "")) || (merchCheck.givesQuests)))
                {
                    if (UnityEngine.Random.Range(0, 1f) <= 0.75f)
                    {
                        merchCheck.RefreshShopInventory(GameMasterScript.heroPCActor.lowestFloorExplored);

                    }
                }
                merchCheck.playerLastLowestLevelVisited = GameMasterScript.heroPCActor.lowestFloorExplored;

                if (merchCheck.newStuff && merchCheck.actorRefName != "npc_casinoshop")
                {
                    if (merchCheck.actorRefName == "npc_questgiver")
                    {
                        GameLogScript.GameLogWrite(UIManagerScript.cyanHexColor + act.displayName + "</color>: " + StringManager.GetString("bark_new_rumors"), GameMasterScript.heroPCActor);
                    }
                    else
                    {
                        GameLogScript.GameLogWrite(UIManagerScript.cyanHexColor + act.displayName + "</color>: " + StringManager.GetString("bark_new_goods"), GameMasterScript.heroPCActor);
                    }
                }
            }
        }
    }

    void MapEntryMoveJunkOffStaircase()
    {
        // Move junk off the staircase, such as monsters that are spawn camping
        MapTileData heroMTD = activeMap.GetTile(GameMasterScript.heroPCActor.GetPos());
        List<Actor> actorsToMove = new List<Actor>();
        foreach (Actor act in heroMTD.GetAllActors())
        {
            if (act.GetActorType() == ActorTypes.HERO) continue;
            if (act.playerCollidable)
            {
                actorsToMove.Add(act);
            }
        }
        foreach (Actor actorToMove in actorsToMove)
        {
            Vector2 oldPos = actorToMove.GetPos();
            MapTileData nearbyTile = GetRandomNonCollidableTile(heroMTD.pos, 1, true, true);
            MoveAndProcessActor(actorToMove.GetPos(), nearbyTile.pos, actorToMove, true);
            actorToMove.myMovable.AnimateSetPosition(nearbyTile.pos, 0.05f, false, 0f, 0f, MovementTypes.LERP);
            MapMasterScript.activeMap.RemoveActorFromLocation(oldPos, actorToMove);
        }
    }

    static void MapEntryDungeonDigest()
    {
        // Legendary - Dungeon Digest code
        if (!activeMap.IsTownMap())
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("mmdungeondigest"))
            {

                GameLogScript.LogWriteStringRef("log_digest_intro");
                StringManager.SetTag(0, activeMap.unfriendlyMonsterCount.ToString());
                GameLogScript.LogWriteStringRef("log_digest_monsters");

                Item legendary = null;
                Monster legMon = null;

                foreach (Monster mn in activeMap.monstersInMap)
                {
                    if (legendary == null)
                    {
                        foreach (Item itm in mn.myInventory.GetInventory())
                        {
                            if (itm.legendary)
                            {
                                legendary = itm;
                                legMon = mn;
                                break;
                            }
                        }
                    }

                    if (mn.isChampion)
                    {
                        string cDisp = "";
                        int count = 0;
                        foreach (ChampionMod cm in mn.championMods)
                        {
                            cDisp += cm.displayName;
                            if (count == mn.championMods.Count - 1)
                            {
                                cDisp += " ";
                            }
                            else
                            {
                                cDisp += ", ";
                            }
                        }
                        StringManager.SetTag(0, mn.displayName);
                        StringManager.SetTag(1, cDisp);
                        StringManager.SetTag(2, mn.GetPos().ToString());
                        GameLogScript.LogWriteStringRef("log_digest_champion");
                    }
                }

                if (legendary != null)
                {
                    StringManager.SetTag(0, legendary.displayName);
                    StringManager.SetTag(1, legMon.displayName);
                    StringManager.SetTag(2, legMon.GetPos().ToString());
                    GameLogScript.LogWriteStringRef("log_digest_legitem");
                }
            }
        }
    }

    List<Stairs> GetAllStairsInEntireDungeonConnectedToFloor(int floor)
    {
        List<Stairs> allStairs = new List<Stairs>();
        for (int i = 0; i < theDungeon.maps.Count; i++)
        {
            Map m = theDungeon.maps[i];
            foreach (Stairs st in m.mapStairs)
            {
                if (st.NewLocation == null) continue;
                if (st.NewLocation.floor == floor || st.pointsToFloor == floor)
                {
                    allStairs.Add(st);
                    if (Debug.isDebugBuild) Debug.Log("Stairs located on " + m.floor + " need to be reconnected.");
                }
            }
        }

        return allStairs;
    }

    void ReconnectAllStairsInListToMap(List<Stairs> allStairs, Map m)
    {
        foreach(Stairs st in allStairs)
        {
            st.NewLocation = m;
        }
    }

    List<Map> GetAllStairDestinationsInMap(Map workMap)
    {
        List<Map> destinations = new List<Map>();
        foreach(Stairs st in workMap.mapStairs)
        {
            if (st.NewLocation == null)
            {
                if (st.pointsToFloor == -1) continue;
                Map target = theDungeon.FindFloor(st.pointsToFloor);
                if (target == null) continue;
                destinations.Add(target);
            }
            else
            {
                destinations.Add(st.NewLocation);
            }            
        }

        return destinations;
    }

    void ReconnectAllStairsInMapToTheFollowingList(Map workMap, List<Map> destinations)
    {
        for (int i = 0; i < destinations.Count; i++)
        {
            int destFloor = destinations[i].floor;
            if (i >= workMap.mapStairs.Count)
            {
                bool upStairs = destFloor > workMap.floor;
                // make a new stairs
                workMap.SpawnStairs(upStairs, destinations[i].floor);
            }
            else
            {
                workMap.mapStairs[i].NewLocation = destinations[i];
                workMap.mapStairs[i].pointsToFloor = destinations[i].floor;
            }
        }
    }

    void RebuildMapWithInaccessibleStairsAndReturnToPrevious(Map prevMap)
    {
        int floorBeingReplaced = MapMasterScript.activeMap.floor;

        List<Stairs> allStairsConnectedToPreviousFloor = GetAllStairsInEntireDungeonConnectedToFloor(floorBeingReplaced);
        List<Map> stairDestinationsInBadMap = GetAllStairDestinationsInMap(activeMap);
        Map recreatedMap = CreateMap(MapMasterScript.activeMap.floor, true);
        ReconnectAllStairsInListToMap(allStairsConnectedToPreviousFloor, recreatedMap);
        ReconnectAllStairsInMapToTheFollowingList(recreatedMap, stairDestinationsInBadMap);
        TravelManager.TravelMaps(prevMap, null, false);
    }

    /// <summary>
    /// Returns FALSE if failed.
    /// </summary>
    /// <param name="targetMap"></param>
    /// <returns></returns>
    bool ForciblyTunnelDisconnectedStairs(Map targetMap)
    {
        Stairs st1 = null;
        Stairs st2 = null;

        foreach(Stairs st in targetMap.mapStairs)
        {
            if (st1 == null) st1 = st;
            else st2 = st;            
        }

        if (st1 == null || st2 == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Could not find two staircases in " + targetMap.floor + " " + targetMap.GetName());
            return false;
        }

        CustomAlgorithms.GetPointsOnLineNoGarbage(st1.GetPos(), st2.GetPos());

        for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
        {
            MapTileData mtd = targetMap.GetTile(CustomAlgorithms.pointsOnLine[i]);
            if (mtd.tileType == TileTypes.GROUND) continue;
            mtd.ChangeTileType(TileTypes.GROUND);
            mtd.UpdateCollidableState();
            mtd.UpdateVisionBlockingState();
        }

        return true;
    }
}

public class MapChangeInfo
{
    public Map destinationMap;
    public bool anyCampfire;
    public Vector2 spawnPosition;
}

