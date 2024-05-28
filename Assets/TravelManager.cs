using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
	using UnityEngine.Analytics;
#endif
using System;

public class TravelManager : MonoBehaviour
{

    public static TravelManager singleton;
    static List<int> keepersToRemove;

    public void Start()
    {
        singleton = this;
        keepersToRemove = new List<int>();
    }

    public static void TravelToDungeonViaPortal(Stairs st)
    {
        TravelMaps(st.NewLocation, st, false);

        Map removeMap = null;
        MapMasterScript mms = GameMasterScript.mms;

        if (st.isPortal)
        {
            if (st.NewLocation.IsItemWorld())
            {
                mms.townMap2.RemoveActorFromMap(st);
                removeMap = mms.townMap2;
            }
            else
            {
                mms.townMap.RemoveActorFromMap(st);
                removeMap = mms.townMap;
            }

        }

        // Is the below block complete?
        GameMasterScript.heroPCActor.influenceTurnData.Reset();
        GameMasterScript.ClearTurnVariables();

    }

    public static void ExitItemDream(bool withItem)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("exitingdream", 1);
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        GameMasterScript.ClearTurnVariables();
        TDInputHandler.EnableInput();
        TravelMaps(MapMasterScript.singletonMMS.townMap, null, false);

        ItemDreamFunctions.EndItemWorldNoRewards(withItem);
    }

    public static void BackToTownAfterKO()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        MapMasterScript mms = GameMasterScript.mms;

        heroPCActor.ClearBattleDataAndStatuses();

        heroPCActor.myStats.HealToFull();

        Map destinationMap = null;

        //Shara mode does different things
        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            destinationMap =  MapMasterScript.theDungeon.FindFloor(MapMasterScript.SHARA_START_CAMPFIRE_FLOOR);
        }
        //Mirai
        else
        {
            //regen flasks
            if (heroPCActor.regenFlaskUses < 3)
            {
                heroPCActor.SetRegenFlaskUses(3);
            }
            
            //send pets back to corral
            MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();

            //spawn bow if bow was never fired
            if (heroPCActor.ReadActorData("fireranged") != 1)
            {
                heroPCActor.SetActorData("fireranged", 1);
                Weapon w = LootGeneratorScript.CreateItemFromTemplateRef("weapon_shortbow", 1.0f, 0f, false) as Weapon;
                w.SetPos(new Vector2(14f, 11f));
                mms.townMap.PlaceActor(w, mms.townMap.GetTile(new Vector2(18f, 11f)));
            }
            
            //end item world
            if (MapMasterScript.itemWorldOpen) // Close it.
            {
                ItemDreamFunctions.EndItemWorldNoRewards();
            }
            
            //travel to town map
            destinationMap = MapMasterScript.singletonMMS.townMap;
        }

        BackToTown_Part2(destinationMap);        
    }

    public static void BackToTown_Part2(Map destinationMap, bool actuallyTravel = true)
    {
        GameMasterScript.ClearTurnVariables();
        TDInputHandler.EnableInput();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshPlayerCT(false);
        GameMasterScript.actualGameStarted = true;
        GameMasterScript.heroPCActor.destroyed = false;
        GameMasterScript.heroPCActor.deathProcessed = false;
        GameMasterScript.cameraScript.SetToGrayscale(false);
        UIManagerScript.HideLearnSkillIndicator();
        UIManagerScript.ToggleHealthBlink(false, 0f);
        GameMasterScript.playerDied = false;
        GameMasterScript.returningToTownAfterKO = true;

        if (actuallyTravel)
        {
            TravelMaps(destinationMap, null, false);
            UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
        }

        GameMasterScript.RemoveActorFromDeadQueue(GameMasterScript.heroPCActor);
        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.heroPCActor.VerifyAbilities();
        GameMasterScript.heroPCActor.myAbilities.VerifyEquippedPassivesAreActivated();

        if (actuallyTravel)
        {
            // #todo - Coroutine the two lines below so they dont flicker the current box
            UIManagerScript.OverrideDialogWidth(1060f, 200f);
            UIManagerScript.ResetDialogBoxComponents();
        }
    }

    public static void TravelMaps(Map newLocation, Stairs st, bool escapePortalUsed, MapTileData forceLocation = null)
    {
        GameMasterScript.SetLevelChangeState(true);
        GameMasterScript.SetAnimationPlaying(true);
        singleton.StartCoroutine(singleton.FadeThenSwitchMaps(GameMasterScript.gmsSingleton.levelTransitionTime, newLocation, st, escapePortalUsed, false, forceLocation));
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenStopAnimation(0.75f));
    }

    public static void TravelFromTownToFloor(int floor)
    {
        Stairs st = null;
        foreach (Stairs s in MapMasterScript.singletonMMS.townMap.mapStairs)
        {
            if (s.NewLocation == null) continue;
            if (s.NewLocation.floor == 0)
            {
                st = s;
            }
        }

        if (st == null)
        {
            Debug.Log("Couldn't find stairs in town?");
        }

        Map newLocation = MapMasterScript.theDungeon.FindFloor(floor);

        GameMasterScript.ClearTurnVariables();
        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);

        /* heroPCActor.influenceTurnData.Reset();
        turnExecuting = false;
        SetAnimationPlaying(false);        
        UIManagerScript.RefreshStatuses();
        turnTimer = 0.0f;
        playerAttackedThisTurn = false;
        changePlayerTimerThisTurn = false;
        bufferTargetData.Clear();
        processBufferTargetDataIndex = 0; */
        singleton.StartCoroutine(singleton.FadeThenSwitchMaps(GameMasterScript.gmsSingleton.levelTransitionTime, newLocation, st, false, true));
    }

    public static bool TryTravelStairs()
    {
        Stairs st = GameMasterScript.mms.GetStairsInTile(GameMasterScript.heroPCActor.GetPos());

        // don't use stairs right after a job change
        if (Time.time - CharCreation.timeAtLastJobChange < 0.2f)
        {
            return false;
        }

        if (st != null)
        {
            // DEBUG
            if (!MapMasterScript.mapLoaded)
            {
                Debug.Log("No active map set.");
            }
            else if (st.NewLocation == null)
            {
                Debug.Log("Stairs have no location");
            }
            else if (st.NewLocation.dungeonLevelData == null)
            {
                Debug.Log("New location has no dungeon level data");
            }


            if (MapMasterScript.activeMap.floor == MapMasterScript.CAMPFIRE_FLOOR)
            {
                // If we did NOT use the campfire and we're trying to leave the zone, prompt the player first.
                if (GameMasterScript.gmsSingleton.ReadTempGameData("confirm_campfirestairs") != 1 && 
                    MapMasterScript.activeMap.FindActor("npc_restfire") != null)
                {
                    UIManagerScript.StartConversationByRef("dialog_confirm_campfire_stairs", DialogType.STANDARD, null);
                    return false;
                }

                GameMasterScript.gmsSingleton.SetTempGameData("confirm_campfirestairs", 0);
            }

            st.usedByPlayer = true;

            // SPECIAL CASE - Ending stairs.
            if (st.pointsToFloor == 999 || st.ReadActorData("finalstairs") == 1)
            {
                Cutscenes.BeginEndgameCutscene();
                return true;
            }

            TravelMaps(st.NewLocation, st, false);

            return true;
        }
        return false;
    }

    public static bool CheckForAutomoveStairs(Stairs checkStairs)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        MapMasterScript mms = MapMasterScript.singletonMMS;

        if (checkStairs != null && !MapMasterScript.activeMap.IsTownMap())
        {
            if (checkStairs.ReadActorData("finalstairs") == 1 && !checkStairs.autoMove)
            {
                StringManager.SetTag(0, "?????");
            }
            else
            {
                StringManager.SetTag(0, checkStairs.NewLocation.GetName());
            }
            GameLogScript.GameLogWrite("<color=yellow>" + StringManager.GetString("log_standonstairs") + "</color>", heroPCActor);

        }
        
        //check for sharaportation if she is in camp and has been defeated.
        if (checkStairs != null && 
            SharaModeStuff.IsSharaModeActive() &&
            heroPCActor.ReadActorData("shara_defeated") == 1 &&
            heroPCActor.GetActorMap() != null &&
            heroPCActor.GetActorMap().floor == MapMasterScript.SHARA_START_CAMPFIRE_FLOOR)
        {
            if (SharaMode_LeaveBaseCamp())
            {
                return true;
            }
        }
        

        if (checkStairs != null && checkStairs.autoMove )
        {
            if (heroPCActor.GetActorMap() == mms.townMap && !checkStairs.isPortal && checkStairs.NewLocation != MapMasterScript.singletonMMS.townMap2)
            {
                // FAST TRAVEL

                if (BeginFastTravelDialog())
                {
                    return true;
                }
                else if (GameMasterScript.heroPCActor.ReadActorData("entereddungeon") != 1 && GameMasterScript.heroPCActor.ReadActorData("enteredtutorial") != 1 && GameStartData.NewGamePlus == 0)
                {
                    UIManagerScript.StartConversationByRef("skiptutorial_prompt", DialogType.KEYSTORY, null);
                    GameMasterScript.ClearTurnVariables();
                    GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
                    return true;
                }
            }

            TravelMaps(checkStairs.NewLocation, checkStairs, false);

            if (checkStairs.isPortal)
            {
                MapMasterScript.activeMap.RemoveActorFromMap(checkStairs);
            }

            // Is the below block complete?
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            GameMasterScript.ClearTurnVariables();
            /* heroPCActor.influenceTurnData.Reset();
            turnExecuting = false;
            SetAnimationPlaying(false);
            ProcessDeadQueue(MapMasterScript.activeMap);
            UIManagerScript.RefreshStatuses();
            turnTimer = 0.0f;
            playerAttackedThisTurn = false;
            changePlayerTimerThisTurn = false;
            processBufferTargetDataIndex = 0; */
            return true;
        }

        return false;
    }

    public IEnumerator WaitThenFadeThenSwitchMaps(float waitTime, float time, Map newLocation, Stairs st, bool escapePortalUsed, bool forcedSkip, MapTileData forceLocation = null, bool fadeUpAfterLoad = true)
    {
        yield return new WaitForSeconds(waitTime);
        StartCoroutine(FadeThenSwitchMaps(time, newLocation, st, escapePortalUsed, forcedSkip, forceLocation, fadeUpAfterLoad));
    }

    public IEnumerator FadeThenSwitchMaps(float time, Map newLocation, Stairs st, bool escapePortalUsed, bool forcedSkip, MapTileData forceLocation = null, bool fadeUpAfterLoad = true)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        MapMasterScript mms = MapMasterScript.singletonMMS;

        bool travelingToTownAfterMysteryDungeon = CheckForAndProcessMysteryDungeonCompletion(heroPCActor, escapePortalUsed);

        Vector2 newLoc = Vector2.zero;
        if (st != null)
        {
            heroPCActor.lastStairsTraveled = st;
            if (st.pointsToSpecificTile)
            {
                newLoc.x = st.pointsToTileX;
                newLoc.y = st.pointsToTileY;
            }
        }
        if (escapePortalUsed)
        {
            // Player spawn position after using portal.
            if (newLocation == MapMasterScript.singletonMMS.townMap)
            {
                newLoc.x = 8;
                newLoc.y = 10;
            }
            else
            {
                newLoc.x = 7;
                newLoc.y = 13;
            }
            GameMasterScript.heroPCActor.myInventory.RemoveAllDreamItems();

        }
        int prevX = (int)heroPCActor.GetPos().x;
        int prevy = (int)heroPCActor.GetPos().y;
        Map prevMap = heroPCActor.GetActorMap();
        TDInputHandler.DisableInput();

        LoadingWaiterManager.Display(0.25f);

        // If we're NOT going to a mystery dungeon from town or a campfire, do a fade
        if (!(!MapMasterScript.activeMap.IsMysteryDungeonMap() && MapMasterScript.activeMap.floor != MapMasterScript.CAMPFIRE_FLOOR && newLocation.IsMysteryDungeonMap()))
        {
            UIManagerScript.FadeOut(time * 0.5f);
            yield return new WaitForSeconds(time * 0.52f);
            LoadingWaiterManager.Display();
            yield return null;

            // DO NOT BEGIN TRANSITION until we are totally faded out.
            float MAX_POSSIBLE_DELAY = 6f;
            float timeAtWaitStart = Time.realtimeSinceStartup;
            while (UIManagerScript.GetFadeState() != EFadeStates.NOT_FADING)
            {
                yield return null;
                if (Time.realtimeSinceStartup - timeAtWaitStart >= MAX_POSSIBLE_DELAY)
                {
                    Debug.Log("Can't wait any longer for fade! " + timeAtWaitStart);
                    break;
                }
            }
        }

        if (forceLocation != null)
        {
            newLoc = forceLocation.pos;
        }

        if (GameMasterScript.gmsSingleton.ReadTempGameData("nomapfade") == 1)
        {
            fadeUpAfterLoad = false;
            GameMasterScript.gmsSingleton.SetTempGameData("nomapfade", 1);
        }


        bool prevMapIsJobTrialFloor = prevMap != null && (prevMap.floor == MapMasterScript.JOB_TRIAL_FLOOR || prevMap.floor == MapMasterScript.JOB_TRIAL_FLOOR + 1);

#if UNITY_PS4
        //on PS4 we fade in later on
        bool savedGame = mms.SwitchMaps(newLocation, newLoc, forcedSkip, false, time);
#else
        bool savedGame = mms.SwitchMaps(newLocation, newLoc, forcedSkip, fadeUpAfterLoad, time);
#endif

        yield return EnsureNextTrackIsLoadedBeforeSwitching();

        TDInputHandler.EnableInput();

        if (GameMasterScript.endingItemWorld && MapMasterScript.itemWorldMaps == null)
        {
            GameMasterScript.endingItemWorld = false;
        }

        bool portalCreated = false;

        if (GameMasterScript.gmsSingleton.ReadTempGameData("waypointtravel") == 1) escapePortalUsed = false;

        GameMasterScript.gmsSingleton.SetTempGameData("escapeportalused", 1);

        portalCreated = TryCreatePortalDependingOnPreviousArea(escapePortalUsed, prevMapIsJobTrialFloor, prevMap, newLocation, prevX, prevy);

        // Clear waypoint travel flag no matter what.
        GameMasterScript.gmsSingleton.SetTempGameData("waypointtravel", 0);

        // Do we have the item dream item in our inventory? Or is it equipped? If so, let's be sure to close the item dream.
        // This is a super duper sanity check to catch some weird edge cases...
        if (MapMasterScript.itemWorldOpen && newLocation.floor == MapMasterScript.TOWN_MAP_FLOOR)
        {
            ItemDreamFunctions.SanityCheckThatItemDreamShouldBeOpen();
        }

        if (GameMasterScript.endingItemWorld && escapePortalUsed)
        {
            ItemDreamFunctions.EndItemWorld();
        }

        if (!savedGame)
        {
            try
            {                
                GameMasterScript.gmsSingleton.SaveTheGame(autoSave:true);
            }
            catch (Exception e)
            {
                Debug.Log("Autosave failed! " + e.ToString());
            }
        }

#if UNITY_PS4
        //hide loading icon and fade in
        StartCoroutine(IWaitForSaveAndThenHideLoadingIconAndFadeIn(fadeUpAfterLoad, time));
#else
        StartCoroutine(IWaitForSaveAndThenHideLoadingIcon());
#endif
    }

    IEnumerator IWaitForSaveAndThenHideLoadingIcon()
    {
        while (GameMasterScript.gmsSingleton.CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            yield return null;
        }
        LoadingWaiterManager.Hide();
    }

    //used on PS4
    IEnumerator IWaitForSaveAndThenHideLoadingIconAndFadeIn(bool fadeInAfterMapSave, float fadeTime)
    {
        while (GameMasterScript.gmsSingleton.CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            yield return null;
        }

        if (fadeInAfterMapSave)
        {
            UIManagerScript.FadeIn(fadeTime * 0.5f);
            GuideMode.OnFullScreenUIClosed();
        }
        LoadingWaiterManager.Hide();
    }

    public static void BackToTown(bool backToTownAfterKO)
    {
        GameMasterScript.heroPCActor.ClearCombatTargets();
        GameMasterScript.heroPCActor.ClearCombatAllies();
        TDInputHandler.EnableInput();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshPlayerCT(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        GameMasterScript.actualGameStarted = true;
        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.heroPCActor.destroyed = false;
        GameMasterScript.heroPCActor.deathProcessed = false;
        GameMasterScript.playerDied = false;
        GameMasterScript.returningToTownAfterKO = backToTownAfterKO;
        MapTileData caveLoc = null;
        foreach (Stairs st in MapMasterScript.singletonMMS.townMap.mapStairs)
        {
            if (st.pointsToFloor == 0 || st.NewLocation.floor == 0)
            {
                //caveLoc = MapMasterScript.singletonMMS.townMap.mapArray[(int)st.GetPos().x, (int)st.GetPos().y - 2];
                caveLoc = MapMasterScript.singletonMMS.townMap.mapArray[8, 10];
                break;
            }
        }
        TravelManager.TravelMaps(MapMasterScript.singletonMMS.townMap, null, true, caveLoc);

        GameMasterScript.RemoveActorFromDeadQueue(GameMasterScript.heroPCActor);

    }

    public static bool CanFastTravelFromCurrentMap()
    {
        // Don't allow waypoint dialog in tutorial maps, ever.
        if (MapMasterScript.activeMap.floor == MapMasterScript.TUTORIAL_FLOOR 
            || MapMasterScript.activeMap.floor == MapMasterScript.TUTORIAL_FLOOR_2)
        {
            return false;
        }

        if (MapMasterScript.activeMap.IsTownMap())
        {
            // Don't allow it in town if we haven't explored around much either. If we haven't even gone to cedar 1 yet, no fast travel.
            Map cedar1 = MapMasterScript.theDungeon.FindFloor(0);
            if (!GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(cedar1.mapAreaID))
            {
                return false;
            }
        }

        return true;
    }

    static List<int> possibleFastTravelFloors;

    public static bool BeginFastTravelDialog()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if (possibleFastTravelFloors == null) possibleFastTravelFloors = new List<int>();
        possibleFastTravelFloors.Clear();
        
        Conversation waypoint = GameMasterScript.FindConversation("riverstone_waypoint");
        if (!CanFastTravelFromCurrentMap())
        {
            return false;
        }

        if (waypoint == null)
        {
            Debug.Log("ERROR: No waypoint conversation");
        }

        TextBranch waypointTB = waypoint.FindBranch("main");

        waypointTB.responses.Clear();

        List<int> waypointFloors = FastTravelStuff.GetWaypointIDList();

        foreach (int dungeonFloor in waypointFloors)
        {
            Map checkMap = MapMasterScript.theDungeon.FindFloor(dungeonFloor);
            if (checkMap == null)
            {
                continue;
            }
            int mapID = checkMap.mapAreaID;
            if (checkMap.IsItemWorld()) continue;
            if (dungeonFloor == 0 || GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(mapID))
            {
                if ((checkMap.floor == MapMasterScript.REALM_OF_GODS_START || dungeonFloor == MapMasterScript.REALM_OF_GODS_START) && !SanityCheckForRealmOfGods()) continue;

                possibleFastTravelFloors.Add(dungeonFloor);
                if (checkMap.mapIsHidden)
                {
                    checkMap.SetMapVisibility(true);
                }
            }
            else if (dungeonFloor == 16 && heroPCActor.lowestFloorExplored >= 16)
            {
                possibleFastTravelFloors.Add(dungeonFloor);
            }
        }
        foreach (int mapExploredID in GameMasterScript.heroPCActor.mapsExploredByMapID)
        {
            Map checkMap;
            if (MapMasterScript.dictAllMaps.TryGetValue(mapExploredID, out checkMap))
            {
                if (checkMap.dungeonLevelData.fastTravelPossible && !checkMap.IsItemWorld())
                {
                    if (checkMap.floor == MapMasterScript.REALM_OF_GODS_START && !SanityCheckForRealmOfGods()) continue;

                    if (checkMap.floor == MapMasterScript.TOWN_MAP_FLOOR || checkMap.floor == MapMasterScript.TOWN2_MAP_FLOOR || checkMap.floor == MapMasterScript.CAMPFIRE_FLOOR)
                    {
                        continue;
                    }
                    if (possibleFastTravelFloors.Contains(checkMap.floor))
                    {
                        continue;
                    }
                    possibleFastTravelFloors.Add(checkMap.floor);
                }
            }
        }

        keepersToRemove.Clear(); 

        foreach(int dungeonFloor in possibleFastTravelFloors)
        {
            Map tMap = MapMasterScript.theDungeon.FindFloor(dungeonFloor);
            ButtonCombo travelOption = new ButtonCombo();
            travelOption.buttonText = "<color=yellow>" + MapMasterScript.theDungeon.FindFloor(dungeonFloor).GetName() + "</color>";

            if (!tMap.IsMainPath())
            {
                string nearby = tMap.GetNearbyPathFloor();
                if (!string.IsNullOrEmpty(nearby))
                {
                    travelOption.buttonText += " - " + nearby;
                }
            }

            travelOption.dbr = DialogButtonResponse.CONTINUE;
            travelOption.actionRef = dungeonFloor.ToString();
            waypointTB.responses.Add(travelOption);

            keepersToRemove.Clear();
            foreach (int iShop in GameMasterScript.heroPCActor.shopkeepersThatRefresh)
            {
                NPC findN = GameMasterScript.gmsSingleton.TryLinkActorFromDict(iShop) as NPC;
                if (findN != null)
                {
                    if (findN.dungeonFloor == tMap.floor && findN.shopRef != "" && findN.newStuff &&
                        findN.actorRefName != "npc_casinoshop") // last special case is a bad hack due to how I set up the map
                    {
                        travelOption.buttonText += " (" + UIManagerScript.greenHexColor + StringManager.GetString("misc_fasttravel_shoprestock") + "</color>)";
                        break;
                    }
                }
                else
                {
                    keepersToRemove.Add(iShop);
                }
            }
            foreach (int iKeeper in keepersToRemove)
            {
                GameMasterScript.heroPCActor.shopkeepersThatRefresh.Remove(iKeeper);
            }
        }

        ButtonCombo exitOption = new ButtonCombo();
        exitOption.buttonText = StringManager.GetString("misc_button_exit_normalcase");
        exitOption.dbr = DialogButtonResponse.EXIT;
        exitOption.actionRef = "exit";
        waypointTB.responses.Add(exitOption);

        if (possibleFastTravelFloors.Count >= 2)
        {
            UIManagerScript.StartConversation(waypoint, DialogType.WAYPOINT, null);
            GameMasterScript.ClearTurnVariables();
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            return true;
        }
        
        if (!SharaModeStuff.IsSharaModeActive() &&
            GameMasterScript.heroPCActor.ReadActorData("entereddungeon") != 1 && 
            GameMasterScript.heroPCActor.ReadActorData("enteredtutorial") != 1 && 
            GameStartData.NewGamePlus == 0)
        {
            // Skip tutorial?
            UIManagerScript.StartConversationByRef("skiptutorial_prompt", DialogType.KEYSTORY, null);
            GameMasterScript.ClearTurnVariables();
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            return true;
        }

        return false;
    }


    /// <summary>
    /// Called when Shara tries to use the exit from her camp. It either takes her to the beginning of her adventure,
    /// or allows her to pick where she was when defeated last.
    /// </summary>
    /// <returns>True if a fast travel dialog was spawned</returns>
    public static bool SharaMode_LeaveBaseCamp()
    {
        //check to see if the just_defeated flag is on shara,
        //if so, look to see what her highest floor is,
        //then allow some teleports based on that.
        var hero = GameMasterScript.GetHeroActor();

        if (hero.ReadActorData("shara_defeated") != 1)
        {
            return false;
        }
        
        int lowestFloor = hero.lowestFloorExplored;
        
        //what floors make good milestones for returning to the fight? 
        //
        // Before the den
        // Branching Meadow
        // 

        return BeginFastTravelDialog();
    }

    static void CreateDreamPortalInRiverstoneGrove(Stairs portal, Map prevMap, Map newLocation, int prevX, int prevy)
    {
        // Item world return portal!
        Actor rem = MapMasterScript.singletonMMS.townMap2.FindActor("itemworldtownportal");
        if (rem != null)
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(rem.GetPos(), rem);
            MapMasterScript.activeMap.RemoveActorFromMap(rem);
            if (rem.objectSet && rem.GetObject().activeSelf)
            {
                GameMasterScript.ReturnActorObjectToStack(rem, rem.GetObject());
            }
        }

        if (prevMap.IsItemWorld())
        {
            // return to specific tile
            portal.pointsToSpecificTile = true;
            portal.pointsToTileX = prevX;
            portal.pointsToTileY = prevy;
        }

        portal.actorRefName = "itemworldtownportal";
        portal.prefab = "AltPortal";
        portal.displayName = StringManager.GetString("portal_tangledeep");
        portal.NewLocation = prevMap;
        portal.autoMove = true;
        portal.newLocationID = prevMap.mapAreaID;
        portal.SetActorType(ActorTypes.STAIRS);

        portal.SetUniqueIDAndAddToDict();

        bool spawnInActiveMap = true;

        Map mToUse = MapMasterScript.activeMap;

        if (MapMasterScript.activeMap == MapMasterScript.singletonMMS.townMap)
        {
            portal.SetSpawnPosXY(11, 12);
            portal.SetPos(new Vector2(11f, 12f));
        }
        else
        {
            portal.SetSpawnPosXY(5, 13);
            portal.SetPos(new Vector2(5f, 13f));
            
            if (!MapMasterScript.activeMap.IsTownMap())
            {
                mToUse = MapMasterScript.singletonMMS.townMap2;
                spawnInActiveMap = false;
            }
        }

        mToUse.AddActorToMap(portal);
        mToUse.AddActorToLocation(portal.GetSpawnPos(), portal);

        if (spawnInActiveMap)
        {
            MapMasterScript.singletonMMS.SpawnStairs(portal);
            portal.myAnimatable.SetAnim("Default");
        }
    }

    static bool SanityCheckForRealmOfGods()
    {
        if (ProgressTracker.CheckProgress(TDProgress.WANDERER_JOURNEY, ProgressLocations.META) < 4)
        {
            return false;
        }
        if (ProgressTracker.CheckProgress(TDProgress.REALMGODS_UNLOCKED, ProgressLocations.META) == 1)
        {
            return true;
        }
        if (ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.HERO) < 2)
        {
            return false;
        }

        return false;
    }

    static bool CheckForAndProcessMysteryDungeonCompletion(HeroPC heroPCActor, bool escapePortalUsed)
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)
            && ProgressTracker.CheckProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO) == 1
            && heroPCActor.myMysteryDungeonData != null
            && heroPCActor.myMysteryDungeonData.dungeonVictory
            && escapePortalUsed)
        {            
            MysteryDungeonManager.CompleteActiveMysteryDungeon();
            return true;
        }

        return false;
    }

    static IEnumerator EnsureNextTrackIsLoadedBeforeSwitching()
    {
        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            //wait here until the music we want to play is loaded and playing
            float fHangAvoider = Time.realtimeSinceStartup;
            if (!string.IsNullOrEmpty(MapMasterScript.activeMap.musicCurrentlyPlaying))
            {
                //is the track we're looking for actually playing?
                var track = MapMasterScript.activeMap.musicCurrentlyPlaying;
                if (!MusicManagerScript.IsTrackPlaying(track))
                {
                    yield return new WaitWhile(() =>
                        !MusicManagerScript.IsTrackPlaying(track) &&
                        Time.realtimeSinceStartup - fHangAvoider < 5.0f);
                }

            }
        }
    }

    static bool TryCreatePortalDependingOnPreviousArea(bool escapePortalUsed, bool prevMapIsJobTrialFloor, Map prevMap, Map newLocation, int prevX, int prevy)
    {
        bool portalCreated = false;
        // Only create a portal if we've used an escape portal, we're NOT coming from a just-ended item world, a job trial, or a Mystery Dungeon
        if (escapePortalUsed && !GameMasterScript.endingItemWorld && !prevMapIsJobTrialFloor
            && ProgressTracker.CheckProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO) != 1)
        {
            Actor rem;
            Stairs portal;
            portal = new Stairs();
            portal.isPortal = true;
            portalCreated = true;

            if (prevMap.IsItemWorld())
            {
                CreateDreamPortalInRiverstoneGrove(portal, prevMap, newLocation, prevX, prevy);
            }
            else
            {
                //Debug.Log("NOT coming from an item world.");
                List<Actor> remover = new List<Actor>();
                Actor act = MapMasterScript.activeMap.FindActor("townportal");
                if (act != null)
                {
                    remover.Add(act);
                }

                foreach (Stairs st2 in MapMasterScript.activeMap.mapStairs)
                {
                    if (!st2.NewLocation.IsItemWorld() && st2.isPortal)
                    {
                        remover.Add(st2);
                    }
                }

                if (remover.Count > 0)
                {
                    foreach (Actor rAct in remover)
                    {
                        MapMasterScript.activeMap.RemoveActorFromLocation(rAct.GetPos(), rAct);
                        MapMasterScript.activeMap.RemoveActorFromMap(rAct);
                        MapMasterScript.activeMap.mapStairs.Remove(rAct as Stairs);
                        if (rAct.objectSet && rAct.GetObject().activeSelf)
                        {
                            GameMasterScript.ReturnActorObjectToStack(rAct, rAct.GetObject());
                        }
                    }

                }

                //Debug.Log("Creating new portal");
                portal.actorRefName = "townportal";
                portal.prefab = "Portal";
                portal.displayName = StringManager.GetString("portal_tangledeep");
                portal.pointsToSpecificTile = true;
                portal.pointsToTileX = prevX;
                portal.pointsToTileY = prevy;
                portal.NewLocation = prevMap;
                portal.autoMove = true;
                portal.newLocationID = prevMap.mapAreaID;
                portal.SetActorType(ActorTypes.STAIRS);
                portal.SetSpawnPosXY(8, 12);
                portal.SetPos(new Vector2(8f, 12f));
                portal.SetUniqueIDAndAddToDict();
                MapMasterScript.activeMap.AddActorToMap(portal);
                MapMasterScript.activeMap.AddActorToLocation(new Vector2(8f, 12f), portal);
                MapMasterScript.singletonMMS.SpawnStairs(portal);
                portal.myAnimatable.SetAnim("Default");
            }

        }

        return portalCreated;

    }
}
