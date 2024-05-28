using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript : MonoBehaviour {

    public float timeAtLastPauseBetweenMonsterActions;

    /// <summary>
    /// Does basic turn setup related to player character, returns TRUE if we shouldn't be taking a turn.
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    /// <returns></returns>
	public bool DoNewTurnProcessing(bool newTurn, TurnData tData)
    {
        if (newTurn)
        {
            if (turnExecuting)
            {
                return true;
            }

            if (TurnManager.CheckForLimitBreak(tData))
            {
                TurnManager.TriggerLimitBreak(tData);
                return true;
            }

            if (TurnManager.CheckForPromptHitDangerousDestructible(tData))
            {
                return true;
            }

            if (TurnManager.CheckForNPCConversations(tData))
            {
                return true;
            }

            SetTempGameData("dt_confirm_destroy", -1);

            GameLogScript.BeginTextBuffer();

            heroPCActor.RemoveActorData("mh_dagger_crit_thisturn");

            heroPCActor.VerifySpritePositionIsAccurate();

            processBufferTargetDataIndex = 0;

            GameMasterScript.heroPCActor.SetActionThisTurn(tData.GetTurnType());

            if (tData.GetTurnType() == TurnTypes.ABILITY && bufferTargetData.Count == 0)
            {
                StringManager.SetTag(0, tData.tAbilityToTry.abilityName);
                GameLogScript.LogWriteStringRef("log_abil_needstarget");
                return true;
            }


            localTurnEffectsFromPlayer = new List<EffectScript>();

            if (tData.GetTurnType() == TurnTypes.MOVE)
            {
                bool isObstructed = CheckIfPlayerParalyzedOrObstructed(tData);
                if (isObstructed)
                {
                    return true;
                }
            }

            if (tData.GetTurnType() == TurnTypes.ABILITY)
            {
                if (TurnManager.CheckForToggleAndFreeAbility(tData, localTurnEffectsFromPlayer))
                {
                    // We used something that exited the turn.
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Various init turn functions including initializing all actors, ticking hero cooldowns etc.
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    public void AdditionalNewTurnSetupOrTextProcessing(bool newTurn, TurnData tData)
    {
        // ***********************************
        // PRIMARY NEXT TURN LOGIC / NEW TURN
        // ***********************************
        if (newTurn)
        {
            CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
            turnExecuting = true;
            turnNumber++;

            GameMasterScript.heroPCActor.CheckForNotorious();

            playerAttackedThisTurn = false;
            tData.extraTurn = false;
            abilityRepetition = 0;

            TurnManager.CheckForAbilityUseFirstTimeSetup(tData, GameMasterScript.heroPCActor);

            firstTurnRefresh = false;
            changePlayerTimerThisTurn = false;

            heroPCActor.ResetHeroTurnData();

            actorListCopy.Clear();

            foreach (Actor checkAct in mms.GetAllActors())
            {
                if (checkAct.ignoreMeInTurnProcessing) continue;
                actorListCopy.Add(checkAct);
            }

            TurnManager.InitializeActorsAtStartOfTurn();

            // Player has acted at this point, run their statuses.
            heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.TURNSTART);

            heroPCActor.myAbilities.TickAllCooldowns();

            if (tData.GetTurnType() == TurnTypes.PASS)
            {
                TileInteractions.CheckForAndOpenConversationInInteractedTile(MapMasterScript.activeMap.GetTile(GameMasterScript.heroPCActor.GetPos()), npcInSameTileAsPlayer: true);
            }

        }
        else
        {
            combatManager.ProcessQueuedText();
        }
    }

    public void CheckForStunPassAndItemUse(bool newTurn, TurnData tData, int iThreadIndex)
    {
        // Moved  cooldown reset here to section above
        heroPCActor.acted = true;

        if (heroPCActor.influenceTurnData.stunChance > 0)
        {
            if (UnityEngine.Random.Range(0, 1f) <= heroPCActor.influenceTurnData.stunChance)
            {
                heroPCActor.lastTurnActed = turnNumber;
                BattleTextManager.NewText(StringManager.GetString("misc_stunned"), heroPC, Color.yellow, 0.0f);
                GameLogScript.LogWriteStringRef("log_error_stunned");
                tData.SetTurnType(TurnTypes.PASS);
            }
        }

        // This code was moved from further in the loop.
        // Player Used an Item To Begin Turn
        if (newTurn && tData.GetTurnType() != TurnTypes.PASS && itemBeingUsed != null)
        {
            gmsSingleton.SetTempGameData("consumeitem_once_used", itemBeingUsed.actorUniqueID);
        }

        int itemBeingUsedID = gmsSingleton.ReadTempGameData("consumeitem_once_used");
        Actor retrieveItem;
        if (itemBeingUsedID >= 0 && dictAllActors.TryGetValue(itemBeingUsedID, out retrieveItem))
        {
            if (retrieveItem.GetActorType() == ActorTypes.ITEM)
            {
                ConsumeItemInTurn(dictAllActors[itemBeingUsedID] as Item, tData, decreaseQuantity: true);
                gmsSingleton.SetTempGameData("consumeitem_once_used", -1);
            }
        }

        if (tData.GetTurnType() == TurnTypes.PASS)
        {
            tData.SetPlayerActedThisTurn(true, iThreadIndex);
            heroPCActor.lastTurnActed = turnNumber;
            heroPCActor.DoPassTurnStuff();
        }
    }

    /// <summary>
    /// Rebuild actorsThatDoStuff and dtActors based on what things can act this turn
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    public void RefreshActorsThatDoStuffList(bool newTurn, TurnData tData)
    {
        //Debug.Log("Refreshing DTActor list.");

        currentMapActors = mms.GetAllActors();
        // Get all actors that can actually move.
        actorsThatDoStuff.Clear();
        dtActors.Clear();

        bool exitingDream = GameMasterScript.gmsSingleton.ReadTempGameData("exitingdream") == 1;

        for (int i = 0; i < currentMapActors.Count; i++)
        {
            Actor act = currentMapActors[i];
            if (exitingDream) continue; // while exiting dream, stop the entire turn.
            // #todo - This object check is pricey, and is the object ever NOT set? We probably don't need to check terrain tiles
            // as they should always have actors.
            bool checkForObjectStatus = true;
            ActorTypes atp = act.GetActorType();
            if (atp == ActorTypes.STAIRS)
            {
                continue;
            }
            if (atp == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.isTerrainTile || dt.mapObjType == SpecialMapObject.BLOCKER) 
                {
                    checkForObjectStatus = false;
                }
            }
            if (checkForObjectStatus && !act.objectSet)
            {
                bool actorNull = act.GetObject() == null;

                // If this happened, the wheels have come off.
                if (actorNull && !act.isInDeadQueue)
                {
                    Debug.Log(act.actorRefName + " floor " + act.dungeonFloor + " destroyed? " + act.destroyed + " id " + act.actorUniqueID + " has a null object, an error has occurred. We are making it dead.");
                    AddToDeadQueue(act);
                }
                else if (!actorNull)
                {
                    // If the actor is in the map, the actor object should be enabled
                    // Not sure why it wouldn't be. We manage visibility with SpriteRenderer
                    act.GetObject().SetActive(true);
                }

            }
            else
            {
                if (act.ignoreMeInTurnProcessing)
                {
                    continue;
                }
                if (atp == ActorTypes.MONSTER && !tData.extraTurn) // Only monsters act, besides the player.
                {
                    if (act.lastTurnActed == turnNumber && !act.acted)
                    {
                        Debug.Log("WARNING: " + act.actorRefName + " already acted this turn?!");
                        continue;
                    }
                    Fighter ft = act as Fighter;
                    actorsThatDoStuff.Add(ft);
                }
                else if (atp == ActorTypes.DESTRUCTIBLE)
                {
                    dtActors.Add(act);
                }
            }
        }
    }

    /// <summary>
    /// Checks for and runs "STARTTURNINTILE" statuses. Returns TRUE if we should exit here.
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    public bool CheckForTileStartTurnStatuses(bool newTurn, TurnData tData, int iThreadIndex)
    {
        // Start turn on tile check        
        Destructible ldt;
        Actor nAct;
        for (int i = 0; i < dtActors.Count; i++)
        {
            nAct = dtActors[i];
            ldt = nAct as Destructible;
            float waitTime = 0.0f;

            nAct.lastTurnActed = turnNumber;

            if (ldt.startCheckThisTurn) continue;

            ldt.startCheckThisTurn = true;

            //Debug.Log("check " + ldt.actorRefName);

            if (ldt.CheckIfCanUseStatus())
            {

                //Debug.Log("can use");

                bool runStartEffect = true;
                if (tData.extraTurn && ldt.runEffectOnLastTurn) 
                {
                    //Debug.Log("CANNOT USE extra turn");
                    continue;
                }

                if (ldt.runEffectOnLastTurn && ldt.turnsToDisappear > 0)
                {
                    runStartEffect = false;
                    //Debug.Log("CANNOT use this turn because " + ldt.turnsToDisappear);
                }
                if (ldt.dtStatusEffect.CheckRunTriggerOn(StatusTrigger.STARTTURNINTILE) && runStartEffect)
                {
                    bool valid = ldt.CheckForValidTargetsOnMe();
                    if (!valid)
                    {
                        for (int w = 0; w < ldt.dtStatusEffect.listEffectScripts.Count; w++)
                        {
                            if (ldt.dtStatusEffect.listEffectScripts[w].tActorType == TargetActorType.ADJACENT)
                            {
                                valid = true;
                                break;
                            }
                        }
                    }


                    if (valid && ldt.CheckStepTriggerCondition())
                    {
                        bool continueTurn = true;
                        waitTime = RunTileEffect(ldt, tData, iThreadIndex, out continueTurn);
#if UNITY_EDITOR
                        //Debug.Log(ldt.actorUniqueID + " runs START effect " + iThreadIndex + " continue turn? " + continueTurn);
#endif
                        if (ldt.destroyOnStep && waitTime != -1f)
                        {
                            ldt.turnsToDisappear = 0;
                            AddToDeadQueue(ldt);
                        }
                        if (!continueTurn)
                        {
                            // Coroutine has been started to continue the turn, so just exit here OK?
                            return true;
                        }
                    }

                    if (ldt.runEffectNoMatterWhatIsOnMe && ldt.turnSummoned != turnNumber)
                    {
                        bool continueTurn = true;
                        waitTime = RunTileEffect(ldt, tData, iThreadIndex, out continueTurn);
                    }
                }
            }

            // Never wait for START turn effects...?
            /* if (waitTime > 0.0f)
            {
                return;
            } */
        }
        // End start turn on tile check.
        return false;
    }

    /// <summary>
    /// Iterates through all monsters, ticks their time, statuses, allows them to act if possible. TRUE means exit the function + turn loop here.
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    /// <param name="iThreadIndex"></param>
    public bool ProcessAllMonsterActions(bool newTurn, TurnData tData, int iThreadIndex)
    {
        timeAtLastPauseBetweenMonsterActions = Time.realtimeSinceStartup;
        for (int i = 0; i < actorsThatDoStuff.Count; i++)
        {
            Fighter act = actorsThatDoStuff[i];
            bool debug = false;

            if (act.dungeonFloor != MapMasterScript.activeMap.floor) continue;

            Movable move = act.myMovable;

            if (move != null)
            {
                if (move.GetInSight() == false && (!act.acted || act.skipTurn)) // And or OR?
                {
                    int turns = move.GetTurnsSinceLastSeen();
                    turns++;
                    move.SetTurnsSinceLastSeen(turns);
                }
            }

            if (!act.acted || act.skipTurn)
            {
                act.TickCombatStats();

                Monster mon = act as Monster;
                if (debug) Debug.Log(mon.myStats.IsAlive() + " Destroyed? " + mon.destroyed + " Acted? " + mon.acted + " Turn start? " + mon.startedTurn + " Frozen? " + debug_freezeMonsters);
                if (mon.myStats.IsAlive() && !mon.destroyed && !mon.acted)
                {
                    mon.previousPosition = mon.GetPos();
                    if (!mon.startedTurn && !mon.acted && (!debug_freezeMonsters || mon.actorUniqueID == debug_freezeAllButThisID)) //Shep: Prevent monster CT gain during freeze state
                    {
                        mon.ChangeCT(mon.cachedBattleData.chargeGain);
                    }
                    if (debug) Debug.Log("Timer? " + mon.actionTimer);

                    if (mon.actionTimer < GameMasterScript.turnSpeed)
                    {
                        mon.acted = false;
                        mon.timesActedThisTurn++;
                        mon.skipTurn = true;
                        mon.startedTurn = false;
                    }
                    else
                    {
                        // Time to act!
                        mon.ChangeCT(-100f);
                        if (mon.maxTurnsToDisappear > 0)
                        {
                            mon.turnsToDisappear--;
                            if (mon.turnsToDisappear <= 0)
                            {
                                DestroyActor(mon);
                            }
                        }

                        //We might be destroyed for whatever reason (including KO from despawning) so
                        //don't act if we are ded
                        if (!mon.destroyed)
                        {
                            if (mon.actionTimer < 100f)
                            {
                                mon.acted = true;
                                mon.timesActedThisTurn++;
                            }
                            else
                            {
                                mon.acted = true;
                                mon.timesActedThisTurn++;
                                if (mon.timesActedThisTurn > 1)
                                {
                                    if (Debug.isDebugBuild) Debug.Log("ERROR: MON ACTED MORE THAN ONCE! " + mon.actorRefName + " " + turnNumber + " " + heroPCActor.lastTurnActed + " " + tData.GetPlayerActedThisTurn() + " " + tData.GetTurnType());
                                }
                            }
                            mon.TickAllCombatants();
                            mon.myAbilities.TickAllCooldowns();
                            mon.CalculateMaxRange();
                            //Debug.Log("Monster goes turn " + turnNumber);
                            MonsterTurnData mTurn = mon.myMonsterTurnData.Pass();

                            mon.PrepareMyTurn();

#if UNITY_EDITOR
                            mon.VerifySpritePositionIsAccurate();
                            GameLogScript.BeginTextBuffer();

                            //Clear old info from previous turn
                            if (DebugConsole.IsOpen)
                            {
                                mon.ResetDebugAIInfo();
                            }

                            mTurn = mon.TakeAction();
                            mon.PostTurnActionProcessingPart1(mTurn);
                            mon.PostTurnActionProcessingPart2(mTurn);

#else
                            try
                            {
                                mon.VerifySpritePositionIsAccurate();
                                GameLogScript.BeginTextBuffer();

                                mTurn = mon.TakeAction();
                                mon.PostTurnActionProcessingPart1(mTurn);
                                mon.PostTurnActionProcessingPart2(mTurn);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("MONSTER TURN ERROR: " + e);
                                string txt = "";
                                txt = "Error with " + mon.actorRefName + " " + mon.actorUniqueID + " at " + mon.GetPos() + " Destroyed: " + mon.destroyed + " Alive: " + mon.myStats.IsAlive() + " State: " + mon.myBehaviorState + " Floor: " + mon.dungeonFloor;
                                if (mon.myTarget != null)
                                {
                                    txt += " TARGET: " + mon.myTarget.actorRefName + " TPos: " + mon.myTarget.GetPos() + " TTile: " + mon.myTargetTile + " Floor? " + mon.dungeonFloor;
                                }
                                Debug.Log(txt);
                                GameLogScript.GameLogWrite(StringManager.GetString("error_occurred"), heroPCActor);
                            }
#endif



                            if (move.GetInSight() == true && (mTurn.waitTime > 0.0f || (Time.realtimeSinceStartup - timeAtLastPauseBetweenMonsterActions >= 0.013f)))
                            {
                                SetAnimationPlaying(true);
                                if (mTurn.turnType == TurnTypes.ABILITY)
                                {
                                    // There could be ability animations to trigger.
                                    combatManager.ProcessQueuedEffects();
                                }
                                if (tData.GetTurnType() == TurnTypes.ABILITY && newTurn)
                                {

                                    //Debug.Log("Waiting to check results, newturn " + newTurn + " " + heroPCActor.lastTurnActed + " " + turnNumber + " Acted? " + tData.GetPlayerActedThisTurn() + " " + processBufferTargetDataIndex);

                                }
                                StartCoroutine(WaitCheckResultsThenContinueTurn(mTurn.results, mTurn.affectedActors, mTurn.waitTime, tData, mon, iThreadIndex));

                                //Debug.Log("After " + mon.actorRefName + " " + mon.actorUniqueID + " acts, we're exiting out of " + iThreadIndex);

                                timeAtLastPauseBetweenMonsterActions = Time.realtimeSinceStartup;
                                return true;
                            }
                            else
                            {
                                if (mTurn.turnType != TurnTypes.PASS && mTurn.turnType != TurnTypes.MOVE)
                                {
                                    for (int x = 0; x < mTurn.results.Count; x++)
                                    {
                                        CombatResultsScript.CheckCombatResult(mTurn.results[x], mTurn.affectedActors[x], MapMasterScript.activeMap);
                                    }
                                    if (mTurn.results.Count == 0)
                                    {

                                    }
                                }

                            }
                        }
                    }
                }


            }
        }

        return false;
    }

    /// <summary>
    /// Executes tile end-of-turn statuses, disappearing, spread. TRUE return means we exit here.
    /// </summary>
    /// <param name="newTurn"></param>
    /// <param name="tData"></param>
    /// <param name="iThreadIndex"></param>
    /// <returns></returns>
    public bool CheckForTileEndTurnStatusesAndSpread(bool newTurn, TurnData tData, int iThreadIndex)
    {

        Actor lAct;
        for (int i = 0; i < dtActors.Count; i++)
        {
            //Debug.LogError(dtActors[i].actorRefName + " " + dtActors[i].actorUniqueID + " " + dtActors[i].maxTurnsToDisappear + " " + dtActors[i].turnsToDisappear);
            lAct = dtActors[i];

            if (lAct.dungeonFloor != MapMasterScript.activeMap.floor)
            {

                continue;
            }

            // Destructible / Tile  / Hazard status effects
            bool skip = false;

            if (lAct.bRemovedAndTakeNoActions)
            {
                //Debug.LogError("Removed and take no actions.");
                continue;
            }

            if (lAct.summoner != null && lAct.actOnlyWithSummoner && (!lAct.summoner.acted || lAct.summoner.skipTurn))
            {
                skip = true;
            }

            Destructible dt = lAct as Destructible;
            if (dt.mapObjType == SpecialMapObject.BLOCKER)
            {
                continue;
            }

            bool rotateButDontMove = false;

            if (lAct.summoner != null && !skip)
            {
                if (dt.anchor == dt.summoner && dt.movementType == Spread.FORWARD)
                {
                    if (dt.summoner.movedLastTurn)
                    {
                        // There should be a more elegant way of doing this. It doesn't look great though.
                        //rotateButDontMove = true;
                    }
                }
            }

            if (!lAct.acted && !skip)
            {
                if (!dt.isTerrainTile && dt.mapObjType != SpecialMapObject.ISLANDSWATER) gmsSingleton.SetTempGameData("latest_destructibleturn_beforeaction", lAct.actorUniqueID);
                lAct.acted = true;
                lAct.timesActedThisTurn++;
                if (lAct.timesActedThisTurn > 1)
                {
                    Debug.LogWarning("ERROR: DESTR ACTED MORE THAN ONCE! " + lAct.actorRefName);
                }

                //Debug.Log("Process " + lAct.actorRefName + " " + lAct.actorUniqueID + " on turn " + GameMasterScript.turnNumber);

                if (!dt.movedThisTurn)
                {
                    switch (dt.movementType)
                    {
                        case Spread.NOSPREAD:
                            dt.movedThisTurn = false;
                            break;
                        case Spread.RANDOM:
                            List<MapTileData> possibleTiles = new List<MapTileData>();
                            CustomAlgorithms.GetTilesAroundPoint(dt.GetPos(), 1, MapMasterScript.activeMap);
                            for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
                            {
                                if (CustomAlgorithms.tileBuffer[x].tileType == TileTypes.GROUND)
                                {
                                    possibleTiles.Add(CustomAlgorithms.tileBuffer[x]);
                                }
                            }
                            possibleTiles.Shuffle();
                            if (possibleTiles.Count > 0)
                            {
                                Vector2 newPos = possibleTiles[0].pos;
                                MapMasterScript.singletonMMS.MoveAndProcessActorNoPush(dt.GetPos(), newPos, dt);
                                if (dt.myMovable != null)
                                {
                                    dt.myMovable.AnimateSetPosition(newPos, playerMoveSpeed, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
                                }
                                dt.movedThisTurn = true;
                            }
                            break;
                        case Spread.FORWARD:
                            Vector2 objectStartPosition = dt.GetPos();
                            if (dt.actorRefName == "obj_blessedhammer" || dt.actorRefName == "obj_spinblade" || dt.actorRefName == "obj_tutblade")
                            {
                                // Boomerang effect!
                                if ((dt.turnsToDisappear == (dt.maxTurnsToDisappear / 2)) || (dt.actorfaction == Faction.DUNGEON && turnNumber % 3 == 0))
                                {
                                    if (dt.lastMovedDirection == Directions.NEUTRAL)
                                    {
                                        dt.UpdateLastMovedDirection(Directions.NORTH);
                                    }
                                    dt.UpdateLastMovedDirection(MapMasterScript.oppositeDirections[(int)dt.lastMovedDirection]);
                                    if (dt.showDirection)
                                    {
                                        dt.ShowDirection(false);
                                        dt.ShowDirection(true);
                                    }
                                }
                                if (dt.turnsToDisappear == dt.maxTurnsToDisappear && dt.actorRefName != "obj_tutblade")
                                {
                                    dt.movedThisTurn = true;
                                }
                                if (dt.rotateToMoveDirection)
                                {
                                    dt.myMovable.UpdateObjectRotation(0.01f, dt.lastMovedDirection);
                                }
                            }

                            if (!dt.movedThisTurn && !rotateButDontMove)
                            {
                                dt.movedThisTurn = true;
                                Directions localDir = dt.lastMovedDirection;
                                if (localDir != Directions.NEUTRAL)
                                {
                                    Vector2 newPos = dt.GetPos() + MapMasterScript.xDirections[(int)dt.lastMovedDirection];
                                    if (MapMasterScript.InBounds(newPos))
                                    {
                                        MapTileData mtd = MapMasterScript.GetTile(newPos);
                                        if (mtd.tileType == TileTypes.GROUND || dt.passThroughAnything)
                                        {
                                            // Right now we're assuming it just moves through anything.
                                            //Debug.Log("DT movement. " + dt.actorRefName + " moving from " + dt.GetPos() + " to " + newPos);
                                            MapMasterScript.singletonMMS.MoveAndProcessActorNoPush(dt.GetPos(), newPos, dt);
                                            if (dt.myMovable != null)
                                            {
                                                dt.myMovable.AnimateSetPosition(newPos, playerMoveSpeed * 0.75f, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
                                            }
                                        }
                                    }

                                    if (objectStartPosition == dt.GetPos() && dt.destroyOnWallHit)
                                    {
                                        AddToDeadQueue(dt);
                                    }
                                }
                            }
                            if (dt.actorRefName == "obj_iceshard"
                                || dt.actorRefName == "obj_playericeshard"
                                || dt.actorRefName == "obj_playericeshard_2")
                            {
                                // Boomerang effect!
                                // Triggers AFTER moving, BEFORE turnsAlive ticks
                                if (dt.turnsAlive % 2 != 0 && dt.turnsAlive != 0)
                                {
                                    dt.UpdateLastMovedDirection(MapMasterScript.RotateDirection90(dt.lastMovedDirection));
                                    if (dt.rotateToMoveDirection)
                                    {
                                        dt.myMovable.UpdateObjectRotation(playerMoveSpeed, dt.lastMovedDirection);
                                    }
                                }
                                if (dt.turnsToDisappear == dt.maxTurnsToDisappear)
                                {
                                    dt.movedThisTurn = true;
                                }
                            }

                            break;
                    }
                }

                dt.DoDestructibleTurnMovement(tData);

                float waitTime = 0.0f;

                if (dt.maxTurnsToDisappear > 0 && !tData.extraTurn)
                {
                    dt.turnsToDisappear--;
                    //Debug.Log("Tick " + dt.actorUniqueID);
                    dt.turnsAlive++;
                    if (lAct.turnsToDisappear <= 0)
                    {
                        if (lAct.actorRefName == "obj_rivertile")
                        {
                            MapMasterScript.GetTile(lAct.GetPos()).RemoveTag(LocationTags.WATER);
                        }
                        else if (lAct.actorRefName == "obj_electile")
                        {
                            MapMasterScript.GetTile(lAct.GetPos()).RemoveTag(LocationTags.ELECTRIC);
                        }
                        else if (lAct.actorRefName == "obj_lavatile" || lAct.actorRefName == "obj_lavashieldtile")
                        {
                            MapMasterScript.GetTile(lAct.GetPos()).RemoveTag(LocationTags.LAVA);
                        }
                        else if (lAct.actorRefName.Contains("phasmashieldtile"))
                        {
                            MapMasterScript.GetTile(lAct.GetPos()).RemoveTag(LocationTags.LASER);
                        }
                        AddToDeadQueue(dt);
                        //Debug.Log("No more turns!" + dt.actorRefName);
                        //DestroyActor(dt);
                    }
                }


                bool continueThread = true;

                if (dt.CheckIfCanUseStatus())
                {
                    bool runEffect = true;
                    // HARDCODED - When not to run effect?
                    if (dt.runEffectOnLastTurn && lAct.turnsToDisappear > 0)
                    {
                        runEffect = false;
                    }
                    if (dt.dtStatusEffect.CheckRunTriggerOn(StatusTrigger.ENDTURNINTILE) && runEffect)
                    {
                        if (dt.dtStatusEffect.CheckAbilityTag(AbilityTags.REQHEROTRIGGER) && dt.GetPos() != heroPCActor.GetPos())
                        {
                            continue;
                        }
                        else if (dt.dtStatusEffect.CheckAbilityTag(AbilityTags.REQHEROTRIGGER) && dt.GetPos() == heroPCActor.GetPos())
                        {
                            if (tData.GetTurnType() != TurnTypes.MOVE)
                            {
                                continue;
                            }
                        }

                        bool valid = dt.CheckForValidTargetsOnMe();

                        if (!valid)
                        {
                            for (int w = 0; w < dt.dtStatusEffect.listEffectScripts.Count; w++)
                            {
                                if (dt.dtStatusEffect.listEffectScripts[w].tActorType == TargetActorType.ADJACENT)
                                {
                                    valid = true;
                                    break;
                                }
                            }
                        }

                        if ((valid || dt.dtStatusEffect.CheckAbilityTag(AbilityTags.PLAYANIMONEMPTY)) && dt.CheckStepTriggerCondition())
                        {
                            waitTime = RunTileEffect(dt, tData, iThreadIndex, out continueThread); // WAIT IS THIS BAD?

                            //Debug.Log(dt.actorUniqueID + " is running END TURN effect " + iThreadIndex);

                            if (dt.destroyOnStep && waitTime != -1f)
                            {
                                dt.turnsToDisappear = 0;
                                AddToDeadQueue(dt);
                            }
                        }


                        if (valid && !MapMasterScript.activeMap.GetTile(dt.GetPos()).CheckTag(LocationTags.MUD) &&
                            !MapMasterScript.activeMap.GetTile(dt.GetPos()).CheckTag(LocationTags.LAVA) &&
                            !MapMasterScript.activeMap.GetTile(dt.GetPos()).CheckTag(LocationTags.ELECTRIC) &&
                            !MapMasterScript.activeMap.GetTile(dt.GetPos()).CheckTag(LocationTags.LASER) &&
                            !MapMasterScript.activeMap.GetTile(dt.GetPos()).CheckTag(LocationTags.ISLANDSWATER))
                        {
                            if (dt.GetPos() == heroPCActor.GetPos() && UnityEngine.Random.Range(0, 1f) <= CHANCE_HAZARD_SWEEP && heroPCActor.myStats.CheckHasStatusName("hazardsweep"))
                            {
                                if (dt.actorfaction != Faction.PLAYER)
                                {
                                    dt.turnsToDisappear = 0;
                                    AddToDeadQueue(dt);
                                }
                            }
                        }
                    }
                }

                gmsSingleton.SetTempGameData("latest_destructibleturn_afteraction", lAct.actorUniqueID);

                if (waitTime > 0.0f || !continueThread)
                {
                    // Why was there a return here?

                    return true;
                }
            }
        }

        return false;
    }

    // TRUE if using turn due to toggle.
    public bool ProcessAbilityToggle(TurnData tData, out AbilityScript freeAbility)
    {
        //Debug.Log("We are attempting to toggle on OR off " + tData.tAbilityToTry.refName);
        if (!tData.tAbilityToTry.toggled)
        {
            //Debug.Log("It is not toggled, so let us toggle it.");
            tData.tAbilityToTry.Toggle(true);
            if (unmodifiedAbility != null)
            {
                //Debug.Log("Unmodified ability exists so we'll toggle that too.");
                unmodifiedAbility.Toggle(true);
            }
            if (tData.tAbilityToTry.exclusionGroup > 0)
            {
                foreach (AbilityScript checkAbil in heroPCActor.myAbilities.GetAbilityList())
                {
                    if (checkAbil.exclusionGroup == tData.tAbilityToTry.exclusionGroup && checkAbil.toggled
                        && checkAbil.refName != tData.tAbilityToTry.refName)
                    {
                        checkAbil.Toggle(false);
                        foreach (EffectScript eff in checkAbil.listEffectScripts)
                        {
                            eff.ReverseEffect();
                        }

                        GameMasterScript.heroPCActor.ResetAbilityCooldownWithModifiers(checkAbil);
                        uims.RefreshAbilityCooldowns();
                    }
                }
            }
            freeAbility = tData.tAbilityToTry;
        }
        else
        {
            //Debug.Log("The ability " + tData.tAbilityToTry.refName + " WAS toggled. But now it is not.");
            tData.tAbilityToTry.Toggle(false);
            if (unmodifiedAbility != null)
            {
                unmodifiedAbility.Toggle(false);
                //Debug.Log("Also turning off the unmodified ability.");
            }
            StringManager.SetTag(0, tData.tAbilityToTry.abilityName);
            GameLogScript.LogWriteStringRef("log_toggle_ability_off");

            foreach (EffectScript eff in tData.tAbilityToTry.listEffectScripts)
            {
                eff.ReverseEffect();
            }
            heroPCActor.ResetAbilityCooldownWithModifiers(tData.tAbilityToTry);
            if (unmodifiedAbility != null)
            {
                heroPCActor.ResetAbilityCooldownWithModifiers(unmodifiedAbility);
            }
            uims.RefreshAbilityCooldowns();
            UIManagerScript.RefreshStatuses();
            freeAbility = null;
            return true;
        }
        return false;
    }

    public void ProcessFreeAbility(TurnData tData, AbilityScript freeAbility)
    {
        float waitTime = 0f;
        StringManager.SetTag(0, heroPCActor.displayName);
        StringManager.SetTag(1, freeAbility.abilityName);
        GameLogScript.LogWriteStringRef("log_ability_used");
        GameMasterScript.gmsSingleton.SetTempStringData("last_abilityref_used", tData.tAbilityToTry.refName);
        CombatManagerScript.SetLastUsedAbility(freeAbility);

        GameMasterScript.heroPCActor.DoAbilityAnimation(tData);
        //heroPCActor.myAnimatable.SetAnimDirectional("Attack", heroPCActor.lastMovedDirection, heroPCActor.lastCardinalDirection);
        heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, -1 * freeAbility.staminaCost, StatDataTypes.CUR, true);
        heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, -1 * freeAbility.energyCost, StatDataTypes.CUR, true);
        heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, -1 * freeAbility.healthCost, StatDataTypes.CUR, true);

        GameMasterScript.heroPCActor.TryHealThroughThaneSong(tData.tAbilityToTry.staminaCost + tData.tAbilityToTry.energyCost);

        if (freeAbility.healthCost != 0f)
        {
            GameMasterScript.heroPCActor.CheckForLimitBreakOnDamageTaken(freeAbility.healthCost);
        }

        if (freeAbility.combatLogText != null && freeAbility.combatLogText != "")
        {
            string displayText = freeAbility.combatLogText.Replace("$user", heroPCActor.displayName);
            GameLogScript.GameLogWrite(freeAbility.combatLogText, heroPCActor);
        }
        if (freeAbility.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
        {
            GameObject go = CombatManagerScript.GetEffect(freeAbility.sfxOverride);
            CombatManagerScript.TryPlayAbilitySFX(go, heroPCActor.GetPos(), freeAbility);
        }

        if (freeAbility.refName == "skill_regenflask")
        {
            int baseExtraDur = 5;
            if (heroPCActor.ReadActorData("flask_apple_infuse") == 1)
            {
                baseExtraDur++;
            }
            if (heroPCActor.myStats.CheckHasStatusName("status_flask_megaflavor"))
            {
                heroPCActor.myStats.RemoveStatusByRef("status_flask_megaflavor");
                heroPCActor.myStats.AddStatusByRef("status_megaboost", heroPCActor, 5);
            }
            switch (heroPCActor.ReadActorData("infuse1"))
            {
                case FLASK_HEAL_STAMINAENERGY:
                    heroPCActor.myStats.AddStatusByRef("flask_staminaenergyheal", heroPCActor, baseExtraDur);
                    break;
                case FLASK_BUFF_ATTACKDEF:
                    heroPCActor.myStats.AddStatusByRef("flask_combatboost", heroPCActor, baseExtraDur);
                    break;
            }
            switch (heroPCActor.ReadActorData("infuse2"))
            {
                case FLASK_INSTANT_HEAL:
                    EffectScript instantHeal = GetEffectByRef("flaskinstantheal");
                    GameLogScript.BeginTextBuffer();
                    instantHeal.targetActors.Add(heroPCActor);
                    instantHeal.originatingActor = heroPCActor;
                    instantHeal.selfActor = heroPCActor;
                    instantHeal.centerPosition = heroPCActor.GetPos();
                    instantHeal.positions.Add(heroPCActor.GetPos());
                    instantHeal.parentAbility = regenFlaskAbility;
                    instantHeal.DoEffect();
                    GameLogScript.EndTextBufferAndWrite();
                    UIManagerScript.RefreshPlayerStats();
                    break;
            }
            switch (heroPCActor.ReadActorData("infuse3"))
            {
                case FLASK_HASTE:
                    heroPCActor.myStats.AddStatusByRef("flask_haste", heroPCActor, baseExtraDur);
                    break;
                case FLASK_BUFF_DODGE:
                    heroPCActor.myStats.AddStatusByRef("flask_dodge", heroPCActor, baseExtraDur);
                    break;
            }
        }

        foreach (EffectScript eff in freeAbility.listEffectScripts)
        {
            eff.originatingActor = heroPCActor;
            eff.targetActors.Clear();
            eff.targetActors.Add(heroPCActor);
            eff.positions.Clear();
            eff.positions.Add(heroPCActor.GetPos());
            eff.centerPosition = heroPCActor.GetPos();
            eff.parentAbility = tData.tAbilityToTry;
            //Debug.Log("Do effect " + freeAbility.refName + " " + eff.effectName + " " + eff.effectRefName);
            waitTime += eff.DoEffect();
        }
        combatManager.ProcessQueuedEffects();
        UIManagerScript.RefreshStatuses();
        uims.RefreshAbilityCooldowns();
        processBufferTargetDataIndex = 0;
        bufferTargetData.Clear(); // new to prevent double effect triggers
    }

    void ConsumeItemInTurn(Item itemBeingUsed, TurnData tData, bool decreaseQuantity)
    {
        Consumable consume = itemBeingUsed as Consumable; // Is it ALWAYS a consumable being used?
        if (consume == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Tried to use null item or non-consumable.");
            return;
        }
        if (consume.seasoningAttached != "")
        {
            Item tmp = Item.GetItemTemplateFromRef(consume.seasoningAttached);
            if (tmp != null && consume.seasoningAttached != consume.actorRefName)
            {
                Consumable template = tmp as Consumable;
                EffectScript temp = GetEffectByRef(template.seasoningAttached);
                if (temp != null)
                {
                    localTurnEffectsFromPlayer.Add(temp);
                }
                else
                {
                    Debug.Log("Couldn't find seasoning effect " + consume.seasoningAttached);
                }
            }
            else
            {
                Debug.Log("Couldn't find item ref " + consume.seasoningAttached);
            }

        }

        if (!consume.CheckTag(ItemFilters.MULTI_USE))
        {
            //Debug.Log("Preparing to use " + consume.actorRefName + " " + consume.actorUniqueID + " which now has qty " + consume.GetQuantity() + " " + consume.collection.owner.actorRefName);
            consume.curUsesRemaining--;
            bool destroyItem = false;
            if (!heroPCActor.TryChangeQuantity(consume, -1))
            {
                destroyItem = true;
            }
            if (consume.GetQuantity() <= 0)
            {
                destroyItem = true;
            }
            if (destroyItem)
            {
                heroPCActor.myInventory.RemoveItem(consume);
                UIManagerScript.RemoveItemFromHotbar(consume);
                //Debug.Log("Removing " + consume.actorRefName + " from our inventory.");
                //Debug.Log(heroPCActor.myInventory.HasItem(consume) + " " + heroPCActor.myInventory.HasItemByRef(consume.actorRefName));
            }
            //Debug.Log("Player used item " + consume.actorRefName + " " + consume.actorUniqueID + " which now has qty " + consume.GetQuantity());
            GameMasterScript.heroPCActor.myStats.CheckRunAllStatuses(StatusTrigger.ITEMUSED);
        }

        if (GameStartData.CheckGameModifier(GameModifiers.CONSUMABLE_COOLDOWN) && !consume.isFood)
        {
            heroPCActor.myStats.AddStatusByRefAndLog("status_consumableburnout", heroPCActor, 3);
        }

        if (heroPCActor.jobTrial != null)
        {
            heroPCActor.jobTrial.IncreaseConsumableUsesAndPrint(1);
        }

        if (consume.Quantity <= 0)
        {
            heroPCActor.myInventory.RemoveItem(consume);
            UIManagerScript.RemoveItemFromHotbar(consume);
        }
        tData.usedItem = itemBeingUsed;
        itemBeingUsed = null;
    }



    void DoAbilityDuringTurn(TurnData tData)
    {
        bool playerusedItem = tData.usedItem != null;

        heroPCActor.lastTurnActed = turnNumber;
        heroPCActor.SetActorData("lastenergyspent", tData.tAbilityToTry.energyCost);
        heroPCActor.SetActorData("laststaminaspent", tData.tAbilityToTry.staminaCost);
        GameMasterScript.gmsSingleton.SetTempStringData("last_abilityref_used", tData.tAbilityToTry.refName);
        CombatManagerScript.SetLastUsedAbility(tData.tAbilityToTry); // swapped this with previous line, is it ok?

        GameMasterScript.gmsSingleton.SetTempGameData("useditem", playerusedItem ? 1 : 0);

        heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.USEABILITY);
        StringManager.SetTag(0, heroPCActor.displayName);
        StringManager.SetTag(1, "<#fffb00>" + tData.tAbilityToTry.abilityName + "</color>");

        if (!tData.tAbilityToTry.IsDamageAbility())
        {
            GameLogScript.LogWriteStringRef("log_ability_used");
        }

        if (tData.usedItem == null || tData.usedItem.CheckTag((int)ItemFilters.DEALDAMAGE)
            || tData.usedItem.CheckTag((int)ItemFilters.SUMMON))
        {
            heroPCActor.myStats.RemoveStatusByRef("status_sanctuary");
        }
        if (tData.usedItem != null)
        {
            heroPCActor.myStats.RemoveStatusByRef("spiritwalk");
            heroPCActor.myStats.RemoveStatusByRef("invisible");
        }

        // Below block is to determine which direction to play attack animation in
        Actor randomTarget = null;
        Vector2 usePos = Vector2.zero;
        Directions animDir = GameMasterScript.heroPCActor.lastMovedDirection;

        if (bufferTargetData.Count > 0)
        {
            if (bufferTargetData[0].targetActors.Count > 0)
            {
                randomTarget = bufferTargetData[0].targetActors[UnityEngine.Random.Range(0, bufferTargetData[0].targetActors.Count)];
                usePos = randomTarget.GetPos();
            }
            else
            {
                if (bufferTargetData[0].targetTiles.Count > 0)
                {
                    usePos = bufferTargetData[0].targetTiles[UnityEngine.Random.Range(0, bufferTargetData[0].targetTiles.Count)];
                }
            }
        }
        if (usePos == Vector2.zero)
        {
            animDir = UIManagerScript.singletonUIMS.GetLineDir();
        }
        else
        {
            float angle = CombatManagerScript.GetAngleBetweenPoints(heroPCActor.GetPos(), usePos);
            animDir = MapMasterScript.GetDirectionFromAngle(angle);
        }

        if (!string.IsNullOrEmpty(tData.tAbilityToTry.instantDirectionalAnimationRef))
        {
            CombatManagerScript.GenerateDirectionalEffectAnimation(GameMasterScript.heroPCActor.GetPos(), animDir, tData.tAbilityToTry.instantDirectionalAnimationRef, true);
        }

        if (usePos != GameMasterScript.heroPCActor.GetPos())
        {
            heroPCActor.UpdateLastMovedDirection(animDir);
        }

        //Debug.Log("Now we're using an ability or an item! " + playerusedItem);

        if (!tData.tAbilityToTry.CheckAbilityTag(AbilityTags.NO_ATTACK_ANIM) && !playerusedItem)
        {
            heroPCActor.DoAbilityAnimation(tData, animDir, animDir);
            //heroPCActor.myAnimatable.SetAnimDirectional("Attack", animDir, animDir); // was last cardinal?
        }
        else if (playerusedItem)
        {
            zirconAnim useItem = heroPCActor.myAnimatable.FindAnim("UseItem");
            if (useItem != null)
            {
                heroPCActor.myAnimatable.SetAnim("UseItem"); // was last cardinal?
                TDVisualEffects.PopupSprite(tData.usedItem.spriteRef, heroPC.transform, true, tData.usedItem.GetSpriteForUI());
            }

        }
        heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, -1 * tData.tAbilityToTry.staminaCost, StatDataTypes.CUR, true);
        heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, -1 * tData.tAbilityToTry.energyCost, StatDataTypes.CUR, true);

        GameMasterScript.heroPCActor.TryHealThroughThaneSong(tData.tAbilityToTry.staminaCost + tData.tAbilityToTry.energyCost);

        heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, -1 * tData.tAbilityToTry.healthCost, StatDataTypes.CUR, true);        

        if (tData.tAbilityToTry.healthCost != 0f)
        {
            StringManager.SetTag(0, ((int)tData.tAbilityToTry.healthCost).ToString());
            StringManager.SetTag(1, tData.tAbilityToTry.abilityName);
            GameLogScript.LogWriteStringRef("log_ability_hp_loss");
            BattleTextManager.NewDamageText((int)tData.tAbilityToTry.healthCost, false, Color.white, heroPCActor.GetObject(), 0.15f, 1.0f);
            GameMasterScript.heroPCActor.CheckForLimitBreakOnDamageTaken(tData.tAbilityToTry.healthCost);
        }

        if (tData.tAbilityToTry.spiritsRequired > 0)
        {
            for (int i = 0; i < tData.tAbilityToTry.spiritsRequired; i++)
            {
                heroPCActor.myStats.RemoveStatusByRef("spiritcollected");
            }
        }

        if (tData.tAbilityToTry.energyReserve > 0)
        {
            // We want to track the original, unmodified version of this skill for the purpose of energy/stamina reserve check
            //Debug.Log("Ability to try is " + tData.tAbilityToTry.myID);
            AbilityScript unmod = heroPCActor.cachedBattleData.GetOriginalVersionOfRemappedAbility(tData.tAbilityToTry.refName, heroPCActor);
            //Debug.Log("We are using ability: " + unmod.refName + " " + unmod.myID);
            heroPCActor.myAbilities.ActivateEnergyReservingAbility(unmod, true);
            heroPCActor.cachedBattleData.SetDirty();
        }
        if (tData.tAbilityToTry.staminaReserve > 0)
        {
            // We want to track the original, unmodified version of this skill for the purpose of energy/stamina reserve check
            AbilityScript unmod = heroPCActor.cachedBattleData.GetOriginalVersionOfRemappedAbility(tData.tAbilityToTry.refName, heroPCActor);
            heroPCActor.myAbilities.ActivateStaminaReservingAbility(unmod, true);
            heroPCActor.cachedBattleData.SetDirty();
        }

        int hairbandCount = heroPCActor.myStats.CheckHasStatusName("mmhairband") ? 1 : 0;
        if (heroPCActor.myStats.CheckHasStatusName("stamina_energy_swaprestore")) hairbandCount++;

        if (hairbandCount > 0)
        {
            if (tData.tAbilityToTry.staminaCost > 0 && tData.tAbilityToTry.energyCost == 0)
            {
                float restore = tData.tAbilityToTry.staminaCost * 0.2f * hairbandCount;
                heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, restore, StatDataTypes.CUR, true);
            }
            if (tData.tAbilityToTry.energyCost > 0 && tData.tAbilityToTry.staminaCost == 0)
            {
                float restore = tData.tAbilityToTry.energyCost * 0.2f * hairbandCount;
                heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, restore, StatDataTypes.CUR, true);
            }
            else if (tData.tAbilityToTry.energyCost > 0 && tData.tAbilityToTry.staminaCost > 0)
            {
                float restore = tData.tAbilityToTry.energyCost * 0.2f * hairbandCount;
                heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, restore, StatDataTypes.CUR, true);
                restore = tData.tAbilityToTry.staminaCost * 0.2f * hairbandCount;
                heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, restore, StatDataTypes.CUR, true);
            }
        }

        if (heroPCActor.myStats.CheckHasStatusName("staffmastery2")
            && heroPCActor.myEquipment.GetWeaponType() == WeaponTypes.STAFF
            && tData.tAbilityToTry.energyCost > 0)
        {
            heroPCActor.myStats.AddStatusByRef("spiritpower10mult", heroPCActor, 8);
        }

        // Switch thanesongs for free.
        int localChargeTime = tData.tAbilityToTry.chargeTime;
        if (heroPCActor.ReadActorData("buffer_thanesong_level") >= 1 && gmsSingleton.ReadTempGameData("playerswitchedsong") == 1)
        {
            localChargeTime = 200;
        }
        if (localChargeTime == 200)
        {
            SetTempGameData("heroct", (int)heroPCActor.actionTimer);
        }
        heroPCActor.ChangeCT(localChargeTime);
        if (tData.tAbilityToTry.combatLogText != null && tData.tAbilityToTry.combatLogText != "")
        {
            string displayText = tData.tAbilityToTry.combatLogText.Replace("$user", heroPCActor.displayName);
            GameLogScript.GameLogWrite(tData.tAbilityToTry.combatLogText, heroPCActor);
        }
        if (tData.tAbilityToTry.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
        {
            GameObject go = CombatManagerScript.GetEffect(tData.tAbilityToTry.sfxOverride);
            CombatManagerScript.TryPlayAbilitySFX(go, heroPCActor.GetPos(), tData.tAbilityToTry);
        }

        if (tData.tAbilityToTry.energyCost > 0)
        {
            // Shadow cast
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_PROC_SHADOWCAST && heroPCActor.myStats.CheckHasStatusName("shadowcast"))
            {
                SummonActorEffect sae = GetEffectByRef("eff_shadowcast") as SummonActorEffect;
                sae.positions.Clear();
                sae.centerPosition = heroPCActor.GetPos();
                sae.originatingActor = heroPCActor;
                sae.selfActor = heroPCActor;
                sae.DoEffect();
            }
        }

        // Lose stamina for using abilities in water
        /* if ((MapMasterScript.GetTile(heroPCActor.GetPos()).CheckTag(LocationTags.WATER)) && (!heroPCActor.myStats.CheckHasStatusName("oceangem")))
        {
            float decStam = (heroPCActor.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX) * -0.03f) - 4f;
            heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, decStam, StatDataTypes.CUR, true);
            UIManagerScript.FlashStaminaBar(0.5f);
        } */
    }
}
