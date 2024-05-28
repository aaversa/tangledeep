using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript : MonoBehaviour
{
    private void GameNextTurn(TurnData tData, bool newTurn, int iThreadIndex = 0)
    {

        if (playerDied)
        {
            return;
        }
        if (tData == null)
        {
            Debug.Log("Null turn data.");
            return;
        }

        //Debug.Log("New turn? " + newTurn + " n " + turnNumber + " idx " + iThreadIndex + " " + tData.GetTurnType());

        if (newTurn && tData.GetTurnType() != TurnTypes.REST)
        {
            turnWasStopped = false;
            playerIsResting = false;
        }

        // Don't ever run a turn during a cutscene idiot
        if (IsAnimationPlaying() && animationFromCutscene)
        {
            return;
        }

        // THINGS THAT SHOULDN'T ACTUALLY TAKE A TURN...


        //Debug.Log("Entering turn " + turnNumber + " loop thread " + iThreadIndex);


        // this function will exit, causing the function to exit, if we're not supposed to run a turn
        if (DoNewTurnProcessing(newTurn, tData))
        {
            return;
        }

        AdditionalNewTurnSetupOrTextProcessing(newTurn, tData);

        // Did player try to move?

        if (!tData.GetPlayerActedThisTurn())
        {
            CheckForStunPassAndItemUse(newTurn, tData, iThreadIndex);

            Vector3 positionAtStartOfTurn = heroPCActor.GetPos();

            bool movedAnotherActor = false;

            #region Player Movement Turn Type

            // *****************************************
            // BEGIN PLAYER MOVE CODE - TURN TYPE: MOVE
            // *****************************************

            if (tData.GetTurnType() == TurnTypes.MOVE)
            {
                tData.SetPlayerActedThisTurn(true, iThreadIndex);
                /* if (tData.GetTurnType() == TurnTypes.MOVE && UnityEngine.Random.Range(0, 1f) <= 0.4f
                    && heroPCActor.myStats.CheckHasStatusName("status_tipsy"))
                {
                    Vector2 rnd = heroPCActor.GetPos() + MapMasterScript.xDirections[UnityEngine.Random.Range(0, MapMasterScript.xDirections.Length)];

                    if (tData.newPosition != rnd)
                    {
                        GameLogScript.LogWriteStringRef("log_tipsy");
                    }

                    tData.newPosition = rnd;
                } */

                bool checkCollide = MapMasterScript.CheckCollision(tData.newPosition, heroPCActor);

                float angle = CombatManagerScript.GetAngleBetweenPoints(heroPCActor.GetPos(), tData.newPosition);
                heroPCActor.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(angle));
                heroPCActor.myStats.UpdateStatusDirections();

                MapTileData checkTile = MapMasterScript.GetTile(tData.newPosition);

                bool rooted = false;
                if (tData.GetTurnType() == TurnTypes.MOVE && heroPCActor.influenceTurnData.rootChance > 0)
                {
                    if (UnityEngine.Random.Range(0, 1f) < heroPCActor.influenceTurnData.rootChance)
                    {
                        heroPCActor.lastTurnActed = turnNumber;
                        BattleTextManager.NewText(StringManager.GetString("misc_rooted"), heroPC, Color.yellow, 0.0f);
                        GameLogScript.LogWriteStringRef("log_error_rooted");
                        tData.SetTurnType(TurnTypes.PASS);
                        rooted = true;
                        turnWasStopped = true;
                    }
                }

                #region Player Movement - Collision
                if (checkCollide)   //If we tried to move but instead collided with something, do all this
                {
                    heroPCActor.lastTurnActed = turnNumber;
                    TDInputHandler.ClearMousePathfinding();
                    // Moving into collidable square - something is in the way.

                    if (TileInteractions.CheckForAndOpenConversationInInteractedTile(checkTile))
                    {
                        heroPCActor.ChangeCT(-100f);
                        TurnEndCleanup(tData, iThreadIndex);
                        return;
                    }

                    // Is it an attackable monster?

                    pool_removeList.Clear();
                    pool_removeList = MapMasterScript.GetTile(tData.newPosition).GetAllTargetablePlusDestructibles();

                    Monster attackableMonster = null;
                    Monster swappablePet = null;
                    Destructible breakableDestructible = null;
                    Actor mon = null;
                    bool anyInteractableActor = false;

                    for (int i = 0; i < pool_removeList.Count; i++)
                    {
                        if (pool_removeList[i].destroyed) continue;
                        if (pool_removeList[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
                        {
                            if (pool_removeList[i].playerCollidable)
                            {
                                breakableDestructible = pool_removeList[i] as Destructible;
                                anyInteractableActor = true;
                                mon = breakableDestructible;
                            }
                        }
                        else if (pool_removeList[i].GetActorType() == ActorTypes.MONSTER)
                        {
                            Monster checkMon = pool_removeList[i] as Monster;
                            anyInteractableActor = true;
                            if (pool_removeList[i].actorfaction == Faction.PLAYER)
                            {
                                if (checkMon.CheckAttribute(MonsterAttributes.PLAYERCANTSWAP) == 0)
                                {
                                    swappablePet = pool_removeList[i] as Monster;
                                    mon = swappablePet;
                                }
                            }
                            else
                            {
                                attackableMonster = pool_removeList[i] as Monster;
                                mon = attackableMonster;
                            }
                        }
                    }

                    if (anyInteractableActor)
                    {
                        // Is it allied?
                        if (breakableDestructible != null || attackableMonster != null)
                        {
                            tData.SetTurnType(TurnTypes.ATTACK);
                            if (attackableMonster != null)
                            {
                                tData.SetSingleTargetActor(attackableMonster);
                            }
                            else
                            {
                                tData.SetSingleTargetActor(breakableDestructible);
                            }

                        }
                        else if (swappablePet != null && !rooted)
                        {
                            if (!mon.pushedThisTurn)
                            {
                                // Swap places.
                                mms.MoveAndProcessActorNoPush(mon.GetPos(), heroPCActor.GetPos(), mon);
                                movedAnotherActor = true;
                                if (mon.myMovable != null)
                                {
                                    mon.myMovable.AnimateSetPosition(heroPCActor.GetPos(), playerMoveSpeed, false, 0.0f, 0.0f, MovementTypes.SMOOTH); // Was SMOOTH
                                }
                                mon.SetCurPos(heroPCActor.GetPos());
                                checkCollide = false;
                            }
                            else
                            {
                                mon.pushedThisTurn = true;
                                checkCollide = false;
                            }
                        }
                        else
                        {
                            checkCollide = true;
                        }
                    }
                    else
                    {
                        // Do nothing - just return.                        
                        turnExecuting = false;
                        TurnEndCleanup(tData, iThreadIndex);
                        return;
                    }
                }
                else
                {
                    heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ONMOVE);
                    MapMasterScript.activeMap.CheckForStackedActors(heroPCActor.GetPos(), heroPCActor, true);
                }
                #endregion

                #region Player Move with No Collision or Root
                if (!checkCollide && !rooted && tData.GetTurnType() != TurnTypes.PASS)
                {
                    // Commence normal movement
                    // Move hero
                    heroPCActor.lastTurnActed = turnNumber;
                    heroMovable.AnimateSetPosition(tData.newPosition, playerMoveSpeed, true, 0.0f, 1.0f, MovementTypes.SMOOTH); // Was SMOOTH
                    heroPCActor.stepsTaken++;

                    if (!MapMasterScript.activeMap.IsTownMap())
                    {
                        heroPCActor.CheckUpdateQuestSteps();
                    }

                    playerMovingAnimation = true;
                    StartCoroutine(WaitForPlayerMove(playerMoveSpeed));
                    if (heroPCActor.GetPos().y == tData.newPosition.y)
                    {
                        CameraController.horizontalOnlyMovement = true;
                    }
                    else
                    {
                        CameraController.horizontalOnlyMovement = false;
                    }
                    heroPCActor.SetCurPos(tData.newPosition);
                    CameraController.UpdateCameraPosition(tData.newPosition, true);

                    bool previouslyMovedAnActor = movedAnotherActor;
                    movedAnotherActor = mms.MoveAndProcessActor(positionAtStartOfTurn, tData.newPosition, heroPCActor);
                    if (previouslyMovedAnActor) movedAnotherActor = true;

                    MapTileData mtd = MapMasterScript.GetTile(heroPCActor.GetPos()); // Newly moved position

                    //Shep cleaned this allow for checks in more than once place
                    TileInteractions.HandleEffectsForHeroMovingIntoTile(mtd, false);
                    Stairs checkStairs = mtd.GetStairsInTile();

                    ProcessDeadQueue(MapMasterScript.activeMap);

                    bool autoMoved = TravelManager.CheckForAutomoveStairs(checkStairs);

                    if (autoMoved)
                    {
                        return;
                    }
                }
                #endregion
            }

            // *****************************************
            // END PLAYER MOVE CODE / TURN TYPE: MOVE
            // *****************************************

            if (movedAnotherActor)
            {
                StartCoroutine(WaitThenContinueTurn(tData, 0.22f, heroPCActor));
                return;
            }
            #endregion

            #region Player Attack Turn Type
            // *****************************************
            // BEGIN PLAYER ATTACK CODE - TURN TYPE: ATTACK
            // *****************************************
            if (tData.GetTurnType() == TurnTypes.ATTACK && !playerAttackedThisTurn)
            {
                TDInputHandler.directionalInput = false;
                // ATTACKING
                tData.SetPlayerActedThisTurn(true, iThreadIndex);
                playerAttackedThisTurn = true;

                bool paralyzed = false;

                float parChance = heroPCActor.myStats.CheckParalyzeChance();

                if (parChance > 0)
                {
                    if (UnityEngine.Random.Range(0, 1f) <= parChance)
                    {
                        BattleTextManager.NewText(StringManager.GetString("misc_disarmed"), heroPC, Color.yellow, 0.0f);
                        GameLogScript.LogWriteStringRef("log_error_disarmed");
                        tData.SetTurnType(TurnTypes.PASS);
                        paralyzed = true;
                        turnWasStopped = true;
                    }
                }

                if (!paralyzed)
                {
                    // Lose stamina for attacking in water
                    /* if ((MapMasterScript.GetTile(heroPCActor.GetPos()).CheckTag(LocationTags.WATER)) && (!heroPCActor.myStats.CheckHasStatusName("oceangem")))
                    {
                        float decStam = (heroPCActor.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX) * -0.03f) - 4f;
                        heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, decStam, StatDataTypes.CUR, true);
                        UIManagerScript.FlashStaminaBar(0.5f);
                    } */

                    Actor mon = tData.GetSingleTargetActor();

                    if (mon == null)
                    {
                        Debug.Log("WARNING: Cannot attack null actor.");
                        tData.SetPlayerActedThisTurn(true, iThreadIndex);
                        heroPCActor.lastTurnActed = turnNumber;
                        heroPCActor.DoPassTurnStuff();
                        StartCoroutine(WaitCheckResultThenContinueTurn(CombatResult.NOTHING, tData, 0.01f + 0.01f, heroPCActor));
                        return;
                    }
                    else if (mon.GetActorType() == ActorTypes.DESTRUCTIBLE)
                    {
                        Destructible dt = mon as Destructible;

                        if (dt == null)
                        {
                            Debug.Log("WARNING: " + mon.actorRefName + " " + mon.GetActorType() + " " + mon.actorUniqueID + " is marked as destructible, but isn't.");
                            return;
                        }
                        bool ableToDestroy = false;

                        if (!String.IsNullOrEmpty(dt.reqDestroyItem))
                        {
                            Item temp = Item.GetItemTemplateFromRef(dt.reqDestroyItem);
                            if (temp != null)
                            {
                                int qty = heroPCActor.myInventory.GetItemQuantity(dt.reqDestroyItem);
                                if (qty == 0)
                                {
                                    if (!string.IsNullOrEmpty(temp.displayName))
                                    {
                                        StringManager.SetTag(0, temp.displayName);
                                        GameLogScript.LogWriteStringRef("log_error_require_key");
                                    }
                                }
                                else
                                {
                                    StringManager.SetTag(0, temp.displayName);
                                    GameLogScript.LogWriteStringRef("log_use_key");
                                    heroPCActor.myInventory.ChangeItemQuantityByRef(dt.reqDestroyItem, -1);
                                    ableToDestroy = true;
                                }
                            }

                        }
                        else
                        {
                            ableToDestroy = true;
                        }

                        if (ableToDestroy)
                        {
                            if (!dt.isDestroyed)
                            {
                                TileInteractions.BreakDestructible(heroPCActor, dt);
                            }
                            SetAnimationPlaying(true);
                            StartCoroutine(WaitThenContinueTurn(tData, baseAttackAnimationTime, heroPCActor));
                            return; // Should we return, or continue turn instead?
                        }
                        else
                        {
                            //Debug.Log("Cannot destroy " + dt.actorRefName);
                            SetAnimationPlaying(true);
                            StartCoroutine(WaitThenContinueTurn(tData, baseAttackAnimationTime, heroPCActor));
                            return;
                        }

                    }
                    else
                    {
                        Monster targMon = mon as Monster;

                        UIManagerScript.SetLastHeroTarget(targMon);

                        CombatResultPayload crp = CombatManagerScript.Attack(heroPCActor, targMon);
                        CombatResult result = crp.result;
                        tData.SetSingleTargetActor(mon);
                        tData.SetSingleTargetPosition(tData.newPosition);
                        SetAnimationPlaying(true);
                        float attackWaitTime = baseAttackAnimationTime;
                        /* if (CombatManagerScript.bufferedCombatData.numAttacks > 1)
                        {
                            attackWaitTime *= CombatManagerScript.bufferedCombatData.numAttacks;
                        } */
                        if (CombatManagerScript.accumulatedCombatWaitTime == 0)
                        {
                            attackWaitTime = baseAttackAnimationTime;
                        }
                        else
                        {
                            attackWaitTime = CombatManagerScript.accumulatedCombatWaitTime;
                        }
                        StartCoroutine(WaitCheckResultThenContinueTurn(result, tData, attackWaitTime + 0.01f, heroPCActor));
                        StartCoroutine(combatManager.WaitThenProcessQueuedEffects(baseAttackAnimationTime)); // Attack first then other effects
                        heroPCActor.lastTurnActed = turnNumber;
                        return;
                    }

                }
            }
            // *****************************************
            // END PLAYER ATTACK CODE - TURN TYPE: ATTACK
            // *****************************************
            #endregion

            #region UNUSED PROBABLY
            // I don't think this block ever runs...?
            if (tData.GetTurnType() == TurnTypes.ITEM && !newTurn)
            {
                tData.SetPlayerActedThisTurn(true, iThreadIndex);
                heroPCActor.myInventory.RemoveItem(tData.usedItem);
            }
            #endregion

            // 1142019 - Item use code WAS HERE

            #region Player Use Ability Turn Type
            // *****************************************
            // BEGIN PLAYER ABILITY CODE - TURN TYPE: ABILITY
            // *****************************************
            //Debug.Log(tData.GetTurnType() + " NT? " + newTurn + " PA? " + playerActedThisTurn + " no" + turnNumber + " " + heroPCActor.lastTurnActed);
            if (tData.GetTurnType() == TurnTypes.ABILITY)
            {
                TDInputHandler.directionalInput = false;
                // Has the player EVER used an ability
                if (TDPlayerPrefs.GetInt(GlobalProgressKeys.ABIL_USED_EVER) == 0)
                {
                    TDPlayerPrefs.SetInt(GlobalProgressKeys.ABIL_USED_EVER, 1);
                }

                string tdInfo = "";
                if (bufferTargetData != null && bufferTargetData.Count > 0)
                {
                    tdInfo += " " + bufferTargetData[0].GetAllInfo();
                }
#if !UNITY_SWITCH
                //Debug.Log("Player is using ability: " + tData.tAbilityToTry.refName + " on turn " + turnNumber + " Last turn acted? " + heroPCActor.lastTurnActed + " Acted this turn? " + tData.GetPlayerActedThisTurn() + " TD count? " + bufferTargetData.Count + " Data index? " + processBufferTargetDataIndex + " " + tdInfo + " NEW TURN? " + newTurn + " Actor who initiated turn: " + tData.actorThatInitiatedTurn.actorRefName + " CT: " + heroPCActor.actionTimer + " " + tData.extraTurn);
#endif
            }

            #region First time player ability is used in new turn
            if (tData.GetTurnType() == TurnTypes.ABILITY && processBufferTargetDataIndex < bufferTargetData.Count)
            {

                // --------------------------
                // BEGIN ABILITY NEWTURN LOGIC
                // --------------------------
                if (newTurn && (heroPCActor.lastTurnActed != turnNumber || turnNumber == 0))
                {
                    DoAbilityDuringTurn(tData);
                }
                // --------------------------
                // END ABILITY NEWTURN LOGIC
                // --------------------------
                #endregion

                // Go through all the buffer data.
                #region Process all buffer data and run effects
                for (int i = processBufferTargetDataIndex; i < bufferTargetData.Count; i++)
                {
                    TargetData processTD = bufferTargetData[processBufferTargetDataIndex];
                    //Debug.Log("Trying ability " + processTD.whichAbility.abilityName + " " + processTD.whichAbility.myID + " at index " + processBufferTargetDataIndex + " abil ID " + processTD.whichAbility.myID + " Actor count? " + processTD.targetActors.Count + " BTD count: " + bufferTargetData.Count);
                    // Build target lists.

                    List<Actor> targets = GetTargetsFromTargetData(processTD);

                    // Just pick one at random. TODO - Do this for monsters too.

                    if (processTD.whichAbility.targetForMonster == AbilityTarget.ENEMY)
                    {
                        targets.Remove(heroPCActor);
                    }

                    if (processTD.whichAbility.CheckAbilityTag(AbilityTags.RANDOMTARGET))
                    {
                        if (targets.Count > 0)
                        {
                            Actor targ = targets[UnityEngine.Random.Range(0, targets.Count)];
                            targets.Clear();
                            targets.Add(targ);
                        }
                    }

                    if (targets.Count > 0)
                    {
                        UIManagerScript.SetLastHeroTarget(targets[UnityEngine.Random.Range(0, targets.Count)]);
                    }

                    // Built target lists.

                    float waitTime = 0.0f;

                    List<CombatResult> results = new List<CombatResult>();
                    List<Actor> affectedActors = new List<Actor>();

                    if (processBufferTargetDataIndex > 0 && processTD.whichAbility.clearEffectsForSubAbilities)
                    {
                        // This is part 2, 3, etc. of an ability, such as Godspeed Strike
                        // We might need to refresh the list of effects to check (localTurnEffectsFromPlayer)                
                        localTurnEffectsFromPlayer.Clear();
                        foreach (EffectScript eff in processTD.whichAbility.listEffectScripts)
                        {
                            localTurnEffectsFromPlayer.Add(eff);
                        }
                    }

                    for (int t = 0; t < localTurnEffectsFromPlayer.Count; t++)
                    {
                        bool runEffect = true;

                        EffectScript eff = localTurnEffectsFromPlayer[t];

                        //Debug.Log(eff.effectRefName + " local effect index: " + t + " " + eff.effectName + " vs buffer index " + eff.processBufferIndex + " How many possible effects? " + localTurnEffectsFromPlayer.Count);

                        if (eff.processBufferIndex != processBufferTargetDataIndex && processTD.whichAbility.repetitions == 0)
                        {
                            //Debug.Log("Don't run this.");
                            runEffect = false;
                        }

                        //Debug.Log(processTD.whichAbility.hasConditions + " " + processTD.whichAbility.hasConditions + " " + runEffect);

                        if (processTD.whichAbility.hasConditions && runEffect)
                        {
                            //Debug.Log("Has conditions! " + t);
                            foreach (EffectConditional ec in processTD.whichAbility.conditions)
                            {
                                if (ec.index == t)
                                {
                                    runEffect = false;
                                    //                                    Debug.Log("Check conditional " + ec.ec + " " + ec.index + " " + processTD.whichAbility.refName);
                                    switch (ec.ec)
                                    {
                                        case EffectConditionalEnums.STATUSREMOVED:
                                            foreach (Actor act in EffectScript.actorsAffectedByAbility)
                                            {
                                                //Debug.Log(act.actorRefName + " was affected");
                                                if (act.IsFighter())
                                                {
                                                    Fighter ft = act as Fighter;
                                                    //Debug.Log(ft.myStats.CountStatusesRemovedSinceLastTurn());
                                                    if (ft.myStats.CountStatusesRemovedSinceLastTurn() > 0)
                                                    {
                                                        runEffect = true;
                                                    }
                                                }
                                            }
                                            break;
                                        case EffectConditionalEnums.ORIGDAMAGETAKEN:
                                            if (heroPCActor.GetFlagData(ActorFlags.TRACKDAMAGE) >= (heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.1f))
                                            {
                                                runEffect = true;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        if (!runEffect)
                        {
                            //Debug.Log("SKIP effect. " + eff.effectRefName);
                            continue;
                        }

                        if (processTD.whichAbility.refName == "skill_gravitysurge")
                        {
                            // Reposition on crystal.
                            eff.originatingActor = heroPCActor.GetSummonByRef("mon_runiccrystal");
                            if (eff.originatingActor == null)
                            {
                                eff.originatingActor = heroPCActor;
                            }
                            eff.centerPosition = eff.originatingActor.GetPos();

                        }
                        else
                        {
                            eff.originatingActor = heroPCActor;
                            eff.centerPosition = processTD.clickedPosition;
                        }

                        eff.targetActors = targets;
                        eff.positions = processTD.targetTiles;
                        eff.parentAbility = processTD.whichAbility;

                        //Debug.Log("Execute " + eff.effectRefName + " " + eff.effectName + " " + processTD.whichAbility.refName + " num: " + localTurnEffectsFromPlayer.Count + " index " + t + ", num actors " + targets.Count);

                        waitTime += eff.DoEffect(t);
                        foreach (CombatResult reso in eff.results)
                        {
                            results.Add(reso);
                        }
                        foreach (Actor affAct in eff.affectedActors)
                        {
                            affectedActors.Add(affAct);
                        }
                    }

                    // Discern hostile vs. non-hostile effects...
                    foreach (Actor affected in affectedActors)
                    {
                        if (affected.GetActorType() == ActorTypes.MONSTER)
                        {
                            if (processTD.whichAbility.targetForMonster == AbilityTarget.ENEMY)
                            {
                                Monster monmon = (Monster)affected as Monster;
                                monmon.AddAggro(heroPCActor, 15f);
                                monmon.lastActorAttackedBy = heroPCActor;
                            }
                        }
                    }

                    processBufferTargetDataIndex++;

                    //tData.SetPlayerActedThisTurn(true, iThreadIndex); // This was not here before, why not...?

                    if (waitTime > 0.0f)
                    {
                        combatManager.ProcessQueuedEffects();
                        combatManager.ProcessQueuedText();
                        if (tData.GetTurnType() == TurnTypes.ABILITY)
                        {
#if !UNITY_SWITCH
                            //Debug.Log("PLAYER: Waiting to check results, newturn " + newTurn + " " + heroPCActor.lastTurnActed + " " + turnNumber + " Acted? " + tData.GetPlayerActedThisTurn() + " " + processBufferTargetDataIndex);
#endif
                        }
                        StartCoroutine(WaitCheckResultsThenContinueTurn(results, affectedActors, waitTime, tData, heroPCActor, iThreadIndex));
                        return;
                    }
                    else
                    {
                        combatManager.ProcessQueuedText();
                        //Debug.Log("No process time.");
                        for (int x = 0; x < results.Count; x++)
                        {
                            CombatResultsScript.CheckCombatResult(results[x], affectedActors[x], MapMasterScript.activeMap);
                        }
                    }
                }
                #endregion

            }
            // *****************************************
            // END PLAYER ABILITY CODE - TURN TYPE: ABILITY
            // *****************************************
            #endregion

            if (heroPCActor.lastTurnActed != turnNumber && tData.GetTurnType() != TurnTypes.PASS)
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: Hero has not taken any action on turn " + turnNumber + " " + tData.GetTurnType() + " " + heroPCActor.lastTurnActed + " " + newTurn + " Last actor to process turn: " + tData.actorThatInitiatedTurn.actorRefName);
#endif
                GameLogScript.GameLogWrite(StringManager.GetString("error_occurred"), heroPCActor);
                TurnEndCleanup(tData, iThreadIndex);
                return;
            }

            #region Player Used Ability - All Effects Executed, Cleanup Ability
            if (tData.GetTurnType() == TurnTypes.ABILITY)
            {
                //Debug.Log("All abilities have been processed");
                if (originatingAbility.CheckAbilityTag(AbilityTags.RANDOMOFFSETSIGNS))
                {
                    originatingAbility.targetOffsetX *= randomSign[UnityEngine.Random.Range(0, 2)];
                    originatingAbility.targetOffsetY *= randomSign[UnityEngine.Random.Range(0, 2)];
                }
                if (originatingAbility.CheckAbilityTag(AbilityTags.SEQUENTIALCLOCKWISEOFFSET))
                {
                    // Say x = -1, y = -1.
                    Vector2 curVector = new Vector2(originatingAbility.targetOffsetX, originatingAbility.targetOffsetY);
                    for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                    {
                        if (Vector2.Equals(MapMasterScript.xDirections[i], curVector))
                        {
                            int newIndex = i - 1;
                            if (newIndex == 0)
                            {
                                newIndex = MapMasterScript.xDirections.Length - 1;
                            }
                            originatingAbility.targetOffsetX = (int)MapMasterScript.xDirections[newIndex].x;
                            originatingAbility.targetOffsetY = (int)MapMasterScript.xDirections[newIndex].y;
                        }
                    }
                }
                if (originatingAbility.CheckAbilityTag(AbilityTags.SEQROTATECLOCKWISE))
                {
                    int cur = (int)originatingAbility.lineDir;
                    cur++;
                    if (cur >= 8) // 8 normal directions
                    {
                        cur = 0;
                    }
                    originatingAbility.lineDir = (Directions)cur;
                }

                // AA: This block below is causing abilities to have their cooldowns reset 3 times... maybe not a problem, but it's weird.

                if (!originatingAbility.toggled)
                {
                    //originatingAbility.ResetCooldown();
                    GameMasterScript.heroPCActor.ResetAbilityCooldownWithModifiers(originatingAbility);
                }
                /* if ((unmodifiedAbility != null) && (!unmodifiedAbility.toggled))
                {
                    unmodifiedAbility.ResetCooldown();
                    Debug.Log("Resetting unmodified abil " + unmodifiedAbility.refName);
                } */
                if (unmodifiedAbility != null) // Spellshift ability, so make sure to modify the original reference.
                {
                    //Debug.Log("Resetting unmodified abil " + unmodifiedAbility.refName + " on turn " + turnNumber);
                    //unmodifiedAbility.ResetCooldown();
                    GameMasterScript.heroPCActor.ResetAbilityCooldownWithModifiers(unmodifiedAbility);
                    if (unmodifiedAbility.spellshift && (heroPCActor.myStats.CheckHasStatusName("status_spellshiftmaterialize") || heroPCActor.myStats.CheckHasStatusName("status_spellshiftmaterialize_2")))
                    {
                        unmodifiedAbility.SetCurCooldownTurns(unmodifiedAbility.GetCurCooldownTurns() * 2);
                        originatingAbility.SetCurCooldownTurns(originatingAbility.GetCurCooldownTurns() * 2);
                    }
                    if (heroPCActor.myStats.CheckHasStatusName("emblem_brigand_tier1_smokecloud"))
                    {
                        unmodifiedAbility.SetCurCooldownTurns(unmodifiedAbility.GetCurCooldownTurns() / 2);
                    }
                }

                if (originatingAbility.spellshift)
                {
                    if (heroPCActor.myStats.CheckHasStatusName("status_spellshapemaster"))
                    {
                        originatingAbility.ChangeCurrentCooldown(-1);
                        unmodifiedAbility.ChangeCurrentCooldown(-1);
                    }
                }

                //If the ability made us move, then we should actually run code that runs during a 
                //turntype.MOVE, since we did indeed move
                if (originatingAbility.abilityFlags[(int)AbilityFlags.MOVESELF])
                {
                    heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.ONMOVE);
                    MapTileData mtdLocation = MapMasterScript.GetTile(heroPCActor.GetPos());
                    TileInteractions.CheckAndRunTileOnMove(mtdLocation, heroPCActor);
                    TileInteractions.HandleEffectsForHeroMovingIntoTile(mtdLocation, false);
                }

                //uims.RefreshAbilityCooldowns(); do this end of turn
                tData.SetPlayerActedThisTurn(true, iThreadIndex);
                //Debug.Log("Player ABILITY this turn " + turnNumber);
                processBufferTargetDataIndex = 0;
            }
            #endregion
        }

        if (!changePlayerTimerThisTurn)
        {
            changePlayerTimerThisTurn = true;
            heroPCActor.ChangeCT(-100f);
            // Need code to let the player go more frequently.
            if (heroPCActor.actionTimer >= 100f)
            {
                /*
                 * Moved this call to the start of the turn when the player gains CT
                 * it was jank that the pop-up was showing up after you took your extra turn. 
                 * 
                 * Specifically because the game says "Monsters won't act!" but they do! They just didn't last turn.

                */
                heroPCActor.ChangeCT(-100f);
                if (heroPCActor.actionTimer >= 100f)
                {
                    heroPCActor.actionTimer = 0f;
                }
                tData.extraTurn = true;

                if (ReadTempGameData("hero_overflowct") == 1)
                {
                    GameLogScript.LogWriteStringRef("log_turn_movedfast");
                    SetTempGameData("hero_overflowct", 0);
                    foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
                    {
                        if (!heroPCActor.visibleTilesArray[(int)m.GetPos().x, (int)m.GetPos().y]) continue;
                        if (m.actorfaction == Faction.PLAYER) continue;
                        if (MapMasterScript.GetGridDistance(heroPCActor.GetPos(), m.GetPos()) <= 6)
                        {
                            BattleTextManager.NewText(StringManager.GetString("popup_missed_turn"), m.GetObject(), Color.red, 0.5f);
                        }
                    }
                }

                if (ReadTempGameData("heroct") > 0)
                {
                    heroPCActor.actionTimer = (float)ReadTempGameData("heroct") - 100f;
                    SetTempGameData("heroct", 0);
                }

            }
        }

        if (!firstTurnRefresh)
        {
            firstTurnRefresh = true;
        }

        RefreshActorsThatDoStuffList(newTurn, tData);

        // Exit if a tile is running an effect and we're supposed to come back to this turn later.
        if (CheckForTileStartTurnStatuses(newTurn, tData, iThreadIndex))
        {
            return;
        }

        // Exit if a monster is acting and we're supposed to come back to this turn later.
        if (ProcessAllMonsterActions(newTurn, tData, iThreadIndex))
        {
            return;
        }

        // Exit if a tile is running an effect and we're supposed to come back to this turn later.
        if (CheckForTileEndTurnStatusesAndSpread(newTurn, tData, iThreadIndex))
        {
            return;
        }

        TurnEndCleanup(tData, iThreadIndex);
    }

    void ClearBufferTargetData()
    {
        bufferTargetData.Clear();
    }

    public void AddBufferTargetData(TargetData td, bool allowMultiples)
    {
        if (!bufferTargetData.Contains(td) || allowMultiples)
        {
            //Debug.Log("Adding target data. " + td.whichAbility.refName + " " + td.whichAbility.myID);
            bufferTargetData.Add(td);
        }
    }

    public static void PassTurnViaPlayerInput()
    {
        TurnData td = new TurnData();
        td.actorThatInitiatedTurn = heroPCActor;
        td.SetTurnType(TurnTypes.PASS);
        gmsSingleton.TryNextTurn(td, true);
        TDInputHandler.timeSinceLastActionInput = Time.time;

        heroPCActor.myMovable.Jab(Directions.NORTH);
    }
}
