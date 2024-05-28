using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
//using UnityEditorInternal;

public enum HeroVisibilityToMonster { NOTCHECKED, NOTVISIBLE, VISIBLE, COUNT }
public enum TileDangerStates { NOTCHECKED, DANGEROUS, SAFE, COUNT }

public partial class Monster : Fighter
{
    public const float HEALING_ABIL_HP_THRESHOLD = 0.85f; // % of max HP for possible targets of player pet healing abilities

    float aggro_CheckDistanceFromHero;
    // Quite a bit of behavior code has been cleaned up and put into separate functions
    // TakeAction() will now ask individual behavior functions if they can produce an action.
    // If they return a value of CONTINUE, then those behaviors are not relevant and we should continue
    // down the priority list of possible behaviors/actions

    private delegate MonsterTurnData MonsterBehaviorScriptDelegate(Monster actorMon);
    List<Directions> tryDir;
    List<CombatResult> abilResults;
    List<Actor> abilAffectedActors;
    bool confusedThisTurn;

    public MonsterTurnData TakeAction()
    {
        bool debug = false;
        if (dungeonFloor != MapMasterScript.activeMap.floor)  // this could happen if we exited an item dream suddenly.
        {
            if (debug) Debug.Log(actorRefName + " " + displayName + " " + actorUniqueID + " is passing turn because not on same floor.");
            return myMonsterTurnData.Pass();
        }
        if (GetActorMap() == null) // 4112019 - WOW this should really never happen, but IF IT DOES...
        {
            SetActorMap(MapMasterScript.activeMap);
            if (GetActorMap() == null)
            {
                return myMonsterTurnData.Pass();
            }
        }
        influenceTurnData.Reset();

        confusedThisTurn = false;

        heroVisibilityThisTurn = HeroVisibilityToMonster.NOTCHECKED;

        //SHEP: allow for monster inaction
        if (GameMasterScript.debug_freezeMonsters && GameMasterScript.debug_freezeAllButThisID != actorUniqueID)
        {
            return myMonsterTurnData.Pass();
        }

        myStats.CheckRunAndTickAllStatuses(StatusTrigger.TURNSTART);
        if (!myStats.IsAlive())
        {
            GameMasterScript.AddToDeadQueue(this);
            return myMonsterTurnData.Pass();
        }

        if (surpressTraits)
        {
            if (actorRefName != "mon_runiccrystal")
            {
                RemoveAllAttributes();
            }
            else
            {
                EnableWrathBarIfNeeded();
            }
            AddAttribute(MonsterAttributes.CANTATTACK, 100);
        }

        UpdateMyMoveBoundaries();

        if (isInCorral)
        {
            Vector2 newPos = FindRandomPos(true);
            moveRange = 1;
            MoveSelf(newPos, true);
            return myMonsterTurnData.Move();
        }

        MonsterTurnData checkForPass = CheckForConditionsThatPassTurn();
        if (checkForPass.turnType == TurnTypes.PASS)
        {
            return checkForPass;
        }

        if (UnityEngine.Random.Range(0, 1f) < influenceTurnData.confuseChance)
        {
            confusedThisTurn = true;
            if (UnityEngine.Random.Range(0,2) == 0)
            {
                MonsterTurnData checkForConfuse = CheckForConfusion((int)cachedBattleData.maxAttackRange);
                if (checkForConfuse.turnType != TurnTypes.CONTINUE)
                {
                    return checkForConfuse;
                }
            }
            else
            {
                // wander?
                Vector2 newPos = FindRandomPos(true);
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("confused_wander_log", this);
                return AttemptMovement(newPos);
            }
        }

        CheckForCrabGrab();
        CheckForBloodlustOrFrenzy();

        bool paralyzed = CheckForParalysis();

        MonsterTurnData checkForFear = CheckForFearBehavior();
        if (checkForFear.turnType == TurnTypes.MOVE || checkForFear.turnType == TurnTypes.PASS)
        {
            return checkForFear;
        }

        int stayNearPlayerValue = ReadActorData("stay_nextto_anchor");
        bool petMustStayNextToPlayer = false;
        if (stayNearPlayerValue > 0)
        {
            stayNearPlayerValue--;
            petMustStayNextToPlayer = true;
            SetActorData("stay_nextto_anchor", stayNearPlayerValue);
        }

        CheckForCharmEffects();

        if (actorfaction == Faction.PLAYER)
        {
            RemoveAttribute(MonsterAttributes.GREEDY);
        }

        MonsterTurnData checkForSecret = CheckForSecretPassTurn(MapMasterScript.GetTile(GetPos()));
        if (checkForSecret.turnType == TurnTypes.PASS)
        {
            return checkForSecret;
        }

        MonsterTurnData checkForStun = CheckForStun();
        if (checkForStun.turnType == TurnTypes.PASS)
        {
            return checkForStun;
        }

        MonsterTurnData checkStoredTurn = CheckForAndExecuteStoredTurn();
        if (checkStoredTurn.turnType != TurnTypes.CONTINUE)
        {
            return checkStoredTurn;
        }

        CheckForChampionEnrage();

        if (!surpressTraits && ReadActorData("pet_no_abilities") != 1)
        {
            RefreshConsiderAbilities();
        }
        else
        {
            considerAbilities.Clear();
        }

        // Causes weird behavior when summoning plant turrets in town but its probably fine.
        if (dungeonFloor == MapMasterScript.TOWN2_MAP_FLOOR || dungeonFloor == MapMasterScript.TOWN_MAP_FLOOR)
        {
            if (turnsToDisappear > 0 && maxTurnsToDisappear > 0)
            {
                moveRange = 1;
            }
        }

        UpdateMaxMoveAndAttackRanges();

        MonsterTurnData checkForcedMove = CheckForForcedMove();
        if (checkForcedMove.turnType != TurnTypes.CONTINUE)
        {
            return checkForcedMove;
        }

        // Is this a good place for this? Maybe? :thinking:
        if (!string.IsNullOrEmpty(scriptTakeAction))
        {
            Func<Monster, MonsterTurnData> checkFunc;
            if (MonsterBehaviorScript.dictDelegates.TryGetValue(scriptTakeAction, out checkFunc))
            {
                MonsterTurnData mtd = checkFunc(this);
                if (mtd.turnType != TurnTypes.CONTINUE)
                {
                    return mtd;
                }
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(MonsterBehaviorScript), scriptTakeAction);
                try
                {
                    object[] paramList = new object[1];
                    paramList[0] = this;
                    MonsterTurnData mtd = runscript.Invoke(null, paramList) as MonsterTurnData;

                    if (mtd.turnType != TurnTypes.CONTINUE)
                    {
                        return mtd;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Error with " + scriptTakeAction + ": " + e);
                }                
            }
        }

        UpdatePlayerPetTargets();

        PickTargetFromTargetList(petMustStayNextToPlayer);

        // confusion was here

        TryCallingForHelp();

        CheckIfDestinationTileReached();

        MonsterTurnData checkForCombo = CheckForCombineWithOtherMonster();
        if (checkForCombo.turnType != TurnTypes.CONTINUE)
        {
            return checkForCombo;
        }

        MonsterTurnData checkForHeal = CheckForAndUseSupportAbility();
        if (checkForHeal.turnType != TurnTypes.CONTINUE)
        {
            return checkForHeal;
        }

        MonsterTurnData tryPullAbility = CheckForAndUsePullAbility();
        if (tryPullAbility.turnType != TurnTypes.CONTINUE)
        {
            return tryPullAbility;
        }

        MonsterTurnData trySeekItem = CheckForSeekItemBehavior();
        if (trySeekItem.turnType != TurnTypes.CONTINUE)
        {
            return trySeekItem;
        }

        MonsterTurnData checkForGreed = CheckForGreedyFightBehavior();
        if (checkForGreed.turnType != TurnTypes.CONTINUE)
        {
            return checkForGreed;
        }

        MonsterTurnData checkZerk = CheckForBerserkerBehavior();
        if (checkZerk.turnType != TurnTypes.CONTINUE)
        {
            return checkZerk;
        }

        RefreshAndPruneCombatTargets();

        CheckForTimidState();

        MonsterTurnData checkRun = CheckForRunningAwayBehavior();
        if (checkRun.turnType != TurnTypes.CONTINUE)
        {
            return checkRun;
        }

        //If we are being forced to return to the player, do that ASAP
        if (myBehaviorState == BehaviorState.PETFORCEDRETURN && anchor != null)
        {
            return ImmediateJumpToAnchorOrPosition();
        }

        MonsterTurnData checkNeutral = CheckForAndTryNeutralBehavior();
        if (checkNeutral.turnType != TurnTypes.CONTINUE)
        {
            return checkNeutral;
        }

        MonsterTurnData checkForStalkBehavior = CheckForAndTryStalkingBehavior();
        if (checkForStalkBehavior.turnType != TurnTypes.CONTINUE)
        {
            return checkForStalkBehavior;
        }

        // Check item / tile seek state (not aggressive)
        MonsterTurnData checkSeek = CheckForAndTrySeekBehavior();
        if (checkSeek.turnType != TurnTypes.CONTINUE)
        {
            return checkSeek;
        }

        CheckMyCombatTargetIsValid();

        if (myBehaviorState == BehaviorState.FIGHT)
        {
            bool skipFighting = false;
            if (actorfaction == Faction.PLAYER && ReadActorData("pet_no_attack") == 1)
            {
                skipFighting = true;

                if (myTarget != null)
                {
                    MonsterMoveData mmd = GetMoveTile(GetPos(), myTarget.GetPos());
                    if (mmd != null)
                    {
                        return AttemptMovement(mmd.destinationTile.pos);
                    }
                    
                }

            }
            if (!skipFighting)
            {
                MonsterTurnData tryFight = TryFightBehavior(paralyzed, petMustStayNextToPlayer);
                if (tryFight.turnType != TurnTypes.CONTINUE)
                {
                    return tryFight;
                }
            }
        } 

        return myMonsterTurnData.Pass();
    }

    // This function assumes a cleaned combatTargets list
    public Actor GetNearestVisibleTarget(bool petMustStayNearPlayer)
    {
        Actor act = null;
        float highestAggro = 0f;

        pool_aggroData.Clear();

        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];

            //Debug.Log(ad.combatant.dungeonFloor + " " + ad.combatant.actorRefName + " " + ad.combatant.actorfaction);

            if (ad == null || ad.combatant == null)
            {
                pool_aggroData.Add(ad);
            }

            if (ad.combatant.dungeonFloor != dungeonFloor) continue;
            if (ad.combatant.actorfaction == actorfaction || ad.combatant.actorfaction == Faction.DUNGEON) continue; // Experimental - DO NOT aggro/attack people with the same level as aggro as you! Try something else.
            if (!ad.combatant.myStats.IsAlive()) continue;

            // Don't even think about attacking stuff that might be out of our reach, if we're in "stay" mode.
            if (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), ad.combatant.GetPos()) >= GetMaxAttackRange()+1)
            {
                continue;
            }

            bool canSeeHero = false;

            if (ad.combatant == GameMasterScript.heroPCActor)
            {
                canSeeHero = GameMasterScript.heroPCActor.CanSeeActor(this);
            }

            if (!canSeeHero && !CustomAlgorithms.CheckBresenhamsLOS(GetPos(), ad.combatant.GetPos(), MapMasterScript.activeMap))
            {
                continue;
            }

            float baseAggro = ad.aggroAmount;
            float dist = MapMasterScript.GetGridDistance(ad.combatant.GetPos(), GetPos());
            if (dist > cachedBattleData.maxAttackRange)
            {
                float diff = dist - cachedBattleData.maxAttackRange;
                baseAggro -= ((baseAggro * diff * 6f) / 100f);
            }
            if (baseAggro > highestAggro)
            {
                act = ad.combatant;
                highestAggro = baseAggro;
            }
        }

        foreach (AggroData ad in pool_aggroData) // Scrub bad stuff
        {
            combatTargets.Remove(ad);
        }

        return act;
    }

    public Actor GetTargetByAggro()
    {
        Actor act = null;
        float highestAggro = 0f;

        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];
            if (ad.aggroAmount > highestAggro)
            {
                act = ad.combatant;
            }
        }

        return act;
    }

    public Actor EvaluateTargetsByRangeAndAggro(bool petMustStayNextToPlayer = false)
    {
        // Are the actors even alive?
        aggroToRemove.Clear();

        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];

            if (ad == null)
            {
                aggroToRemove.Add(ad);
                if (Debug.isDebugBuild) Debug.Log(actorRefName + " had null aggro data.");
                continue;
            }
            if (ad.combatant == null)
            {
                aggroToRemove.Add(ad);
                if (Debug.isDebugBuild) Debug.Log(actorRefName + " had aggro data with null combatant. " + ad.combatantUniqueID);
                continue;
            }

            // Why are monsters EVER adding a destructible as aggro?
            if (!ad.combatant.myStats.IsAlive() || ad.combatant.destroyed || ad.combatant.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                //Debug.Log("Remove " + ad.combatant.actorUniqueID);
                aggroToRemove.Add(ad);
            }
            else if (CheckAlly(ad.combatant))
            {
                if (GetAllyAggro(ad.combatant) > GetTargetAggro(ad.combatant))
                {
                    // We're not mad at it anymore.
                    if (Debug.isDebugBuild) Debug.Log("Remove ally aggro of " + ad.combatant.actorUniqueID);
                    aggroToRemove.Add(ad);
                }
            }

            //Updated to adjust for distance from player and not from self,
            //we were losing aggro on monsters 8 tiles away from us, but still in the player's business.
            if (actorfaction == Faction.PLAYER &&
                ad.combatant.actorfaction != Faction.PLAYER &&
                MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), ad.combatant.GetPos()) >= 8)
            {
#if UNITY_EDITOR
                if (ad.combatant == myTarget)
                {
                    Debug_AddMessage("Target Too Far");
                }
#endif
                aggroToRemove.Add(ad);
            }

            if (ad.combatant.GetActorType() == ActorTypes.HERO)
            {
                if (ad.combatant.myStats.CheckHasStatusName("spiritwalk"))
                {
#if UNITY_EDITOR
                    if (ad.combatant == myTarget)
                    {
                        Debug_AddMessage("Spiritwalking Hero?");
                    }
#endif
                    aggroToRemove.Add(ad);
                }
            }

            // Check for invalid targets
            if (actorfaction == Faction.PLAYER)
            {

                if (ad.combatant.actorRefName == "mon_nightmareking" && ItemDreamFunctions.IsNightmareKingInvincible())
                {
#if UNITY_EDITOR
                    if (ad.combatant == myTarget)
                    {
                        Debug_AddMessage("NK Too Stronk!");
                    }
#endif
                    aggroToRemove.Add(ad);
                }
                else if (ad.combatant.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster tMon = ad.combatant as Monster;
                    if (tMon.foodLovingMonster)
                    {
#if UNITY_EDITOR
                        if (ad.combatant == myTarget)
                        {
                            Debug_AddMessage("Target wanted food");
                        }
#endif
                        aggroToRemove.Add(ad);
                    }
                }
            }
        }

        if (aggroToRemove.Count > 0)
        {
            for (int i = 0; i < aggroToRemove.Count; i++)
            {
                combatTargets.Remove(aggroToRemove[i]);
            }

        }

        aggroToRemove.Clear();

        for (int i = 0; i < combatAllies.Count; i++)
        {
            AggroData ad = combatAllies[i];

            if (ad == null)
            {
                aggroToRemove.Add(ad);
                Debug.Log(actorRefName + " had null ally data.");
                continue;
            }
            if (ad.combatant == null)
            {
                aggroToRemove.Add(ad);
                Debug.Log(actorRefName + " had ally data with null combatant. " + ad.combatantUniqueID);
                continue;
            }

            if (!ad.combatant.myStats.IsAlive() || ad.combatant.destroyed)
            {
                aggroToRemove.Add(ad);
            }
            else if (CheckTarget(combatAllies[i].combatant))
            {
                if (GetAllyAggro(combatAllies[i].combatant) < GetTargetAggro(combatAllies[i].combatant))
                {
                    // We're not friends with it anymore.
                    aggroToRemove.Add(ad);
                }
            }
        }

        if (aggroToRemove.Count > 0)
        {
            for (int i = 0; i < aggroToRemove.Count; i++)
            {
                combatAllies.Remove(aggroToRemove[i]);
            }
        }

        pool_actorGeneric.Clear();

        foreach (AggroData ad in combatTargets)
        {
            if (ad.combatant.summonedActors != null)
            {
                for (int i = 0; i < ad.combatant.summonedActors.Count; i++)
                {
                    if (ad.combatant.summonedActors[i].GetActorType() != ActorTypes.MONSTER) continue;
                    if (CheckTarget(ad.combatant.summonedActors[i])) continue;
                    if (ad.combatant.destroyed || !ad.combatant.myStats.IsAlive()) continue;
                    pool_actorGeneric.Add(ad.combatant.summonedActors[i]);
                }
            }
        }

        if (pool_actorGeneric.Count > 0)
        {
            foreach (Actor poolAct in pool_actorGeneric)
            {
                AddAggro(poolAct, 10f);
            }
        }

        Actor act = null;
        float highestAggro = 0f;
        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];

            if (ad.combatant.dungeonFloor != dungeonFloor) continue;

            if (ad.combatant.actorfaction == actorfaction || ad.combatant.actorfaction == Faction.DUNGEON) continue; // Experimental - DO NOT aggro/attack people with the same level as aggro as you! Try something else.

            if (petMustStayNextToPlayer)
            {
                // Don't even think about attacking stuff that isn't next to player and within our reach.
                // For example, enemy is 2 tiles away, and our range is 1. We can feasibly hit that. Or if we have a ranged attack,
                // 3 away from the player and weapon range of 2 means we can still hit. Otherwise, SKIP this target.
                if (MapMasterScript.GetGridDistance(ad.combatant.GetPos(), GameMasterScript.heroPCActor.GetPos()) > GetMaxAttackRange()+1)
                {
                    continue;
                }
            }

            float baseAggro = ad.aggroAmount;
            float dist = MapMasterScript.GetGridDistance(ad.combatant.GetPos(), GetPos());
            if (dist > cachedBattleData.maxAttackRange)
            {
                float diff = dist - cachedBattleData.maxAttackRange;
                baseAggro *= 0.5f;
                //baseAggro -= ((baseAggro * diff * 6f) / 100f);
            }
            else
            {
                // in ng++, monsters will seek to destroy very low health targets first
                if (ad.combatant.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.2f && GameStartData.NewGamePlus >= 2 && actorfaction == Faction.ENEMY)
                {
                    baseAggro *= 1.5f;
                }
            }

            if (GameMasterScript.heroPCActor.GetMonsterPetID() == ad.combatant.actorUniqueID)
            {
                if (ad.combatant.myStats.CheckHasStatusName("petugly_infuriate"))
                {
                    baseAggro *= 2f;
                }
            }

            if (baseAggro < 1f)
            {
                baseAggro = 1f;
            }

            if (baseAggro >= highestAggro)
            {
#if UNITY_EDITOR
                if (ad.combatant != myTarget && myTarget != null)
                {
                    Debug_AddMessage("New Target has more aggro");
                }
#endif
                act = ad.combatant;
                highestAggro = baseAggro;
            }
        }

        return act;
    }

    public Actor GetTargetAtRandom()
    {
        return combatTargets[UnityEngine.Random.Range(0, GetNumCombatTargets())].combatant;
    }

    List<Actor> withinRange;

    public Actor GetTargetWithinRangeAtRandom()
    {
        float myRange = cachedBattleData.maxAttackRange;
        if (withinRange == null) withinRange = new List<Actor>();
        withinRange.Clear();

        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];
            float dist = MapMasterScript.GetGridDistance(ad.combatant.GetPos(), GetPos());
            if (dist <= myRange)
            {
                withinRange.Add(ad.combatant);
            }
        }

        if (withinRange.Count > 0)
        {
            return withinRange[UnityEngine.Random.Range(0, withinRange.Count)];
        }
        else
        {
            return EvaluateTargetsByRangeAndAggro();
        }

    }

    // Checks for rooting.
    MonsterTurnData AttemptMovement(Vector2 newPos, bool forceMovement = false)
    {
        if (UnityEngine.Random.Range(0, 1.0f) < influenceTurnData.rootChance && !forceMovement)
        {
            StringManager.SetTag(0, displayName);
            GameLogScript.LogWriteStringRef("log_monster_cantmove", this, TextDensity.VERBOSE);
            return myMonsterTurnData.Pass();
        }


        MoveSelf(newPos, true, forceMovement);
        return myMonsterTurnData.Move();
    }

    // Usage of ability
    List<Vector2> newTiles;
    MonsterTurnData ExecuteMovementAbility(MonsterMoveData mmd)
    {
        if (newTiles == null) newTiles = new List<Vector2>();
        newTiles.Clear();
        newTiles.Add(mmd.destinationTile.pos);
        TurnData td = new TurnData();
        td.tAbilityToTry = mmd.abilityUsed;
        td.targetPosition = newTiles;
        td.centerPosition = GetPos();
        float waitTime = UseAbility(td);
        MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
        monTurn.affectedActors = td.affectedActors;
        monTurn.results = td.results;
        if (waitTime > 0.0f)
        {
            CombatManagerScript.cmsInstance.ProcessQueuedEffects();
            return monTurn;
        }
        else
        {
            CombatManagerScript.cmsInstance.ClearQueuedEffects();
            return monTurn;
        }
    }

    // Step move function.
    public void MoveSelf(Vector2 newPos, bool moveViaStep, bool forceMovePosition = false)
    {
        if (myBehaviorState == BehaviorState.NEUTRAL && pushedThisTurn)
        {
            // Don't move if we were pushed by player, i.e. for being anchored? This is new.
            // but don't leave this flag true forever or else you will be a sadcrab. This is newer.
            pushedThisTurn = false;
            return;
        }

        if (moveRange == 0 && moveViaStep)
        {
            return;
        }
        if (myMovable == null)
        {
            Debug.Log("WARNING: " + actorRefName + " " + GetPos() + " has no movable component.");
            return;
        }

        // Nightmare queen: automatically close the gap at longer ranges.
        bool isNightmareKing = actorRefName == "mon_nightmareking";

        if ((ItemDreamFunctions.IsNightmareKingInvincible() && isNightmareKing) || ReadActorData("mystery_king_chaser") == 1)
        {
            bool doRedirect = false;
            if (MapMasterScript.GetGridDistance(GetPos(), GameMasterScript.heroPCActor.GetPos()) >= 9)
            {
                doRedirect = true;
            }
            else if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)GetPos().x, (int)GetPos().y] && !MapMasterScript.activeMap.dungeonLevelData.revealAll)
            {
                // Can't see hero... if this is true for a bit, warp.
                int turns = ReadActorData("hero_oos_turns");
                turns++;
                SetActorData("hero_oos_turns", turns);
                if (turns >= 4)
                {
                    doRedirect = true;
                    SetActorData("hero_oos_turns", 0);
                }
            }

            if (doRedirect)
            {
                int redirectRange = isNightmareKing ? 1 : 3;

                MapTileData redirect = MapMasterScript.GetRandomNonCollidableTile(GameMasterScript.heroPCActor.GetPos(), redirectRange, true, isNightmareKing);
                newPos = redirect.pos;
                if (isNightmareKing)
                {
                    GameLogScript.LogWriteStringRef("log_nightmareking_taunt1");
                }           
                else // Meaning, it IS a Mystery King chaser
                {
                    // Reset action timer to make it a bit more fair
                    actionTimer = 0;
                }
            }
        }

        if (dungeonFloor == MapMasterScript.TUTORIAL_FLOOR && GameMasterScript.heroPCActor.visibleTilesArray[(int)newPos.x, (int)newPos.y] && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_monmove") && PlayerOptions.tutorialTips)
        {
            Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_monmove");
            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
        }

        // Verify position isn't out of range, but Nightmare Queens have forced relocation code... so....
        if (MapMasterScript.GetGridDistance(GetPos(), newPos) > cachedBattleData.maxMoveRange && actorRefName != "mon_nightmareking" && !forceMovePosition && ReadActorData("mystery_king_chaser") != 1)
        {

            /* 
            Debug.Log(actorRefName + " " + displayName + " " + actorUniqueID + " at " + GetPos() + " trying to move to " + newPos + " out of max move range! Alive? " + myStats.IsAlive() + " Target tile? " + myTargetTile + " State? " + myBehaviorState);
            if (myTarget != null)
            {
                Debug.Log("Target: " + myTarget.actorRefName + " " + myTarget.GetPos());
            } */

            newPos = GetPos();
        }
        MapTileData mtd = MapMasterScript.GetTile(newPos);
        if (IsTileDangerous(mtd) && myBehaviorState == BehaviorState.STALKING)
        {
            //Debug.Log(actorRefName + " try move to " + newPos + " which is dangerous!");
        }

        Destructible breakable = mtd.GetBreakableCollidable(this);
        if (breakable != null && breakable.monsterCollidable)
        {
            if (myBehaviorState == BehaviorState.FIGHT || breakable.actorfaction == Faction.PLAYER)
            {
                StringManager.SetTag(0, displayName);
                StringManager.SetTag(1, breakable.displayName);
                GameLogScript.LogWriteStringRef("break_object_log", this);
                TileInteractions.BreakDestructible(this, breakable);
                return;
            }
            else
            {
                Vector2 destination = Vector2.zero;
                if (myTarget != null)
                {
                    // Find nearest tile in direction of target
                    destination = myTarget.GetPos();
                }
                else if (myTargetTile != Vector2.zero)
                {
                    // Find nearest to target tile
                    destination = myTargetTile;
                }

                if (destination != Vector2.zero)
                {
                    adjacent = MapMasterScript.GetNonCollidableTilesAroundPoint(newPos, 1, this);
                    Vector2 best = Vector2.zero;
                    float shortest = 999f;
                    for (int i = 0; i < adjacent.Count; i++)
                    {
                        float ndist = MapMasterScript.GetGridDistance(adjacent[i].pos, destination);
                        if (adjacent[i].HasBreakableCollidable(this)) continue;
                        if (ndist < shortest)
                        {
                            shortest = ndist;
                            best = adjacent[i].pos;
                        }
                    }

                    if (best != Vector2.zero)
                    {
                        newPos = best;
                    }
                    else
                    {
                        destination = Vector2.zero;
                    }
                }

                if (destination == Vector2.zero)
                {
                    newPos = FindRandomPos(true);
                    if (newPos == Vector2.zero)
                    {
                        Debug.Log("Couldn't find anyting at all.");
                        return;
                    }
                }
            }
        }

        Vector2 origPosition = GetPos();

        if (origPosition == newPos)
        {
            return;
        }

        if (MapMasterScript.GetTile(newPos).IsCollidable(this) && ReadActorData("abouttocombine") != 1)
        {
            //Debug.Log("Warning: " + actorRefName + " " + actorUniqueID + " wants to move to a tile with a collidable object. WHY?");
            newPos = FindRandomPos(true);
            if (newPos == Vector2.zero)
            {
                Debug.Log("Couldn't find anyting at all.");
                return;
            }
        }

        int dist = MapMasterScript.GetGridDistance(origPosition, newPos);
        // Move the actor, only animate it if player can see.
        // #Todo - Why am I doing it this way?

        if ((GameMasterScript.heroPCActor.visibleTilesArray[(int)origPosition.x, (int)origPosition.y] && GameMasterScript.heroPCActor.visibleTilesArray[(int)newPos.x, (int)newPos.y]) 
            || Vector2.Distance(GameMasterScript.heroPCActor.GetPos(), GetPos()) <= 12f 
            || MapMasterScript.activeMap.IsTownMap() 
            || MapMasterScript.activeMap.dungeonLevelData.revealAll)
        {
            //Debug.Log(actorRefName + " " + actorUniqueID + " at " + GetPos() + " will move to " + newPos + " on its turn. " + MapMasterScript.GetTile(newPos).monCollidable + " " + MapMasterScript.GetTile(newPos).IsUnbreakableCollidable(this));
            myMovable.AnimateSetPosition(new Vector3(newPos.x, newPos.y, 0f), GameMasterScript.gmsSingleton.playerMoveSpeed - 0.02f, true, 0.0f, 0.0f, MovementTypes.LERP);
        }
        else
        {
            myMovable.SetPosition(new Vector3(newPos.x, newPos.y, 0f));
        }
        
        if (MapMasterScript.GetTile(newPos).CheckTag(LocationTags.LAVA) && CheckAttribute(MonsterAttributes.LOVESLAVA) == 0 && CheckAttribute(MonsterAttributes.FLYING) == 0)
        {
            string extraData = "";
            if (myTarget != null)
            {
                extraData = " My target is: " + myTarget.actorRefName + " " + myTarget.GetPos();
            }
            if (myTargetTile != Vector2.zero)
            {
                extraData += " My target tile is: " + myTargetTile;
            }
            
            //Debug.Log(actorRefName + " at " + GetPos() + " is moving into lava " + myBehaviorState + extraData);
        } 

        SetCurPos(newPos);
        // Move on the map

        //GameMasterScript.mms.MoveActor(origPosition, newPos, this);
        GameMasterScript.mms.MoveAndProcessActor(origPosition, newPos, this);

        MapTileData getTile = MapMasterScript.GetTile(origPosition);

        for (int i = 0; i < tilePath.Count; i++)
        {
            if (tilePath[i] == getTile)
            {
                tilePath.Remove(getTile);
                break;
            }
        }
        //FightDebugLog("Has moved to " + GetPos());
        MapTileData newTile = MapMasterScript.GetTile(newPos);

        GameMasterScript.heroPCActor.CheckForSpearMastery3(this);
    }

    public Vector2 DrunkFindRandomPos()
    {
        MapTileData mtd = null;
        Vector2 newPos = GetPos();
        int moveIndex = (int)wanderDirection;
        if (UnityEngine.Random.Range(0, 1f) <= myTemplate.drunkWalkChance)
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                moveIndex--;
            }
            else
            {
                moveIndex++;
            }
            if (moveIndex < 0) moveIndex = 7;
            if (moveIndex > 7) moveIndex = 0;
        }
        newPos += MapMasterScript.xDirections[moveIndex];
        if (moveRange > 1)
        {
            for (int i = 1; i < moveRange; i++)
            {
                newPos += MapMasterScript.xDirections[moveIndex];
            }
        }
        mtd = MapMasterScript.GetTile(newPos);
        if (mtd.IsCollidable(this) || IsTileDangerous(mtd) || !InMyLocalBounds(mtd.pos))
        {
            wanderDirection = (Directions)UnityEngine.Random.Range(0, 8);
            return FindRandomPos(false);
        }
        else
        {
            return newPos;
        }
    }

    public Vector2 FindRandomPos(bool ignoreAreaLimit)
    {
        Vector2 pos = GetPos();
        // Build a list of all tiles we can possibly reach within ONE tile...
        adjacent.Clear();

        MapTileData mtd = null;

        adjacent = MapMasterScript.GetNonCollidableTilesAroundPoint(GetPos(), moveRange, this);
        clearTiles.Clear();

        for (int i = 0; i < adjacent.Count; i++)
        {
            mtd = adjacent[i];
            if (mtd.IsUnbreakableCollidable(this) || IsTileDangerous(mtd) || !InMyLocalBounds(mtd.pos))
            {
                clearTiles.Add(mtd);
            }
        }

        for (int i = 0; i < clearTiles.Count; i++)
        {
            adjacent.Remove(clearTiles[i]);
        }

        if (adjacent.Count == 0 && moveRange > 0)
        {
            //Debug.Log(actorRefName + " " + actorUniqueID + " cannot move ANYWHERE on floor " + dungeonFloor + "? Position: " + GetPos() + " Moverange: " + moveRange);
            return GetPos();
        }

        if (moveRange == 0 || adjacent.Count == 0)
        {
            return GetPos();
        }

        Vector2 selected = adjacent[UnityEngine.Random.Range(0, adjacent.Count)].pos;

        return selected;
    }

    public List<MonsterPowerData> GetUsableAggressiveAbilities()
    {
        usables.Clear();
        //List<MonsterPowerData> usables = new List<MonsterPowerData>();

        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript abil = FetchLocalAbility(considerAbilities[i].abilityRef);
            if (abil.targetForMonster == AbilityTarget.ENEMY)
            {
                // SPECIAL EFFECTS GO HERE - Hardcoded
                if (myTarget != null && myTarget.GetActorType() != ActorTypes.DESTRUCTIBLE && abil.refName == "skill_stealfood")
                {
                    Fighter ft = myTarget as Fighter;
                    if (!ft.myInventory.HasAnyFood()) continue;
                }
                usables.Add(considerAbilities[i]);
            }
        }

        return usables;
    }

    private bool CheckHeroWithinAggroRange(bool engageCombat)
    {
        if (aggroRange > 0)
        {
            Fighter heroPC = GameMasterScript.GetHeroActor();
            bool CanSeeHero = false;
            float baseSightRange = myStats.GetCurStat(StatTypes.VISIONRANGE);
            float baseAggroRange = aggroRange;

            float aggroChance = 1.0f;

            aggroChance *= heroPC.cachedBattleData.stealthValue;

            // Reduce aggro chance in regular gameplay if player is already engaged
            if (GameStartData.NewGamePlus == 0 && !isBoss)
            {
                foreach (AggroData ad in GameMasterScript.heroPCActor.combatTargets)
                {
                    if (ad.combatant.myStats.IsAlive() && ad.combatant.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = ad.combatant as Monster;
                        if (mn.isBoss) continue;
                        if (mn.isChampion && isChampion)
                        {
                            aggroChance *= 0.15f; // Champions have a much lower chance to aggro if you'er already fighting one
                        }
                        else if (mn.isChampion)
                        {
                            aggroChance *= 0.4f; // Reduce chance of dogpiles anyway
                        }
                        if (GameMasterScript.gmsSingleton.adventureModeActive)
                        {
                            aggroChance *= 0.9f;
                        }
                        if (GameMasterScript.heroPCActor.myStats.GetLevel() <= 6)
                        {
                            aggroChance *= 0.7f; // Reduce chance of dogpiles at very low levels
                        }

                    }
                }
            }

            if (heroPC.myStats.CheckHasStatusName("verse_suppression_2"))
            {
                aggroChance *= 0.15f;
            }
            if (heroPC.myStats.CheckHasStatusName("wildchildbonus2"))
            {
                aggroChance *= 0.5f;
            }
            if (heroPC.myStats.CheckHasStatusName("status_intimidating") && heroPC.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > 0.5f)
            {
                aggroChance *= 0.6f;
            }
            if (monFamily != "bandits" && monFamily != "robots" && heroPC.myStats.CheckHasStatusName("frogsuit"))
            {
                aggroChance *= 0.2f;
            }
            if (heroPC.myEquipment.GetArmorType() == ArmorTypes.LIGHT)
            {
                aggroChance *= 0.85f;
            }
            else if (heroPC.myEquipment.GetArmorType() == ArmorTypes.HEAVY)
            {
                baseSightRange += 1f;
                baseAggroRange += 1f;
                aggroChance += 0.1f;
            }

            if (heroPC.myStats.CheckHasStatusName("ramirelbonus1"))
            {
                baseSightRange -= 1f;
                baseAggroRange -= 1f;
            }

            if (heroPC.myStats.CheckHasStatusName("monsterattract"))
            {
                baseSightRange += 2f;
                baseAggroRange += 2f;
                aggroChance += 0.2f;
            }

            MapTileData mtd = MapMasterScript.GetTile(heroPC.GetPos());
            if (mtd.CheckActorRef("obj_smokecloud"))
            {
                aggroChance *= 0.5f;
            }

            int distanceToPlayer = MapMasterScript.GetGridDistance(heroPC.GetPos(), GetPos());

            if (distanceToPlayer <= baseSightRange)
            {
                // Ramirel mask - monsters can't ever see player from 6+ tiles, unless they're champs.
                if (heroPC.myStats.CheckHasStatusName("ramirelstealth") && distanceToPlayer >= 6 && !isChampion && !isBoss)
                {
                    return false;
                }
                // Hero is POTENTIALLY within sight. Use hero LOS check for now TODO MAYBE CHANGE LATER

                if (heroPC.CanSeeActor(this))
                {
                    CanSeeHero = true;
                }
                else
                {
                    //CanSeeHero = MapMasterScript.CheckTileToTileLOS(GetPos(), heroPC.GetPos(), this, MapMasterScript.activeMap);
                    CanSeeHero = false;
                }
            }

            if (UnityEngine.Random.Range(0, 1f) > aggroChance || distanceToPlayer > baseAggroRange)
            {
                //Debug_AddMessage("Can't see: Range " + distanceToPlayer + " aggroRange " + baseAggroRange);
                CanSeeHero = false;                
            }

            if (CanSeeHero)
            {
                if (CheckAttribute(MonsterAttributes.PREDATOR) > 0)
                {
                    // Predators will wait to strike until prey is low on health.
                    float maxHealthToAggro = ((100 - CheckAttribute(MonsterAttributes.PREDATOR)) * heroPC.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX)) / 100;
                    float curHP = heroPC.myStats.GetCurStat(StatTypes.HEALTH);
                    if (curHP > maxHealthToAggro && UnityEngine.Random.Range(0, 1f) >= CHANCE_PREDATOR_AGGRO)
                    {
                        return false;
                    }

                    if (DisplayMonAttributeMessage() && engageCombat)
                    {
                        StringManager.SetTag(0, displayName);
                        StringManager.SetTag(1, heroPC.displayName);
                        GameLogScript.LogWriteStringRef("smells_blood_log", this);
                    }
                }
                if (CheckAttribute(MonsterAttributes.GANGSUP) > 0)
                {
                    // GANGSUP will wait for a certain # of actors before striking
                    int numCombatantsNeeded = CheckAttribute(MonsterAttributes.GANGSUP) / 20;
                    if (numCombatantsNeeded > heroPC.GetNumCombatTargets() && UnityEngine.Random.Range(0, 1f) > CHANCE_GANGUP_AGGRO)
                    {
                        return false;
                    }

                    if (DisplayMonAttributeMessage() && engageCombat)
                    {
                        StringManager.SetTag(0, displayName);
                        StringManager.SetTag(1, heroPC.displayName);
                        GameLogScript.LogWriteStringRef("gangup_log", this);
                    }
                }

                float roll = UnityEngine.Random.Range(0, 1f);

                if (roll <= 0.25f && engageCombat && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_soothingaura"))
                {
                    myStats.AddStatusByRefAndLog("status_asleep", this, 6);
                }
                else if (roll <= 0.2f && monFamily == "bandits" && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("miraisharabonus1"))
                {
                    myStats.AddStatusByRefAndLog("status_confused50", this, 5);
                }
                else if (engageCombat)
                {
#if UNITY_EDITOR
                    Debug_AddMessage("->FIGHT: Aggro Hero");
#endif
                    SetState(BehaviorState.FIGHT);
                    if (!CheckTarget(GameMasterScript.GetHeroActor()))
                    {
                        AddTarget(GameMasterScript.GetHeroActor());
                        //Debug.Log("Adding hero actor to " + actorRefName + " " + actorUniqueID + " target list.");
                        if (GameMasterScript.heroPCActor.summonedActors != null)
                        {
                            foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
                            {
                                if (act.GetActorType() == ActorTypes.MONSTER)
                                {
                                    AddAggro(act, 20f);
                                }
                            }
                        }
                    }
                    return true; // We engaged the hero, and we can see the hero
                }
                else
                {
                    return true; // We did not engage, but we still can see the hero
                }

            }
        }
        else
        {
            return false; // We have no aggro range at all.
        }
        return false;
    }

    private TurnData EvaluateAbilityTiles(AbilityScript abilToUse, Directions forceDirection, Actor localTarget)
    {
        TurnData dataPack = new TurnData();
        dataPack.SetTurnType(TurnTypes.ABILITY);
        dataPack.centerPosition = GetPos();
        dataPack.tAbilityToTry = abilToUse;

        pool_targets.Clear();
        affectedTiles.Clear();

        if (abilToUse.targetForMonster == AbilityTarget.SUMMONGROUND) // Eventually need to support more than one tile I guess.
        {
            SummonActorEffect eff = abilToUse.listEffectScripts[0] as SummonActorEffect;

            // Just find a random open square.
            if (eff.summonOnCollidable)
            {
                allMTD = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GetPos(), abilToUse.range);
                // Use pooling cleartiles?
            }
            else
            {
                allMTD = MapMasterScript.GetNonCollidableTilesAroundPoint(GetPos(), abilToUse.range, this);
            }
            allMTD.Shuffle();
            if (allMTD.Count > 0)
            {
                MapTileData centerMTD = allMTD[UnityEngine.Random.Range(0, allMTD.Count)];

                if (abilToUse.CheckAbilityTag(AbilityTags.CURSORTARGET))
                {
                    tilesWithinRange = UIManagerScript.singletonUIMS.CreateShapeTileList(abilToUse.targetShape, abilToUse, centerMTD.pos, abilToUse.direction, abilToUse.targetRange, false);
                    foreach (Vector2 v2 in tilesWithinRange)
                    {
                        dataPack.targetPosition.Add(v2);
                    }
                }
                else
                {
                    if (eff.summonActorPerTile)
                    {
                        for (int i = 0; i < allMTD.Count; i++)
                        {
                            dataPack.targetPosition.Add(allMTD[i].pos);
                        }
                    }
                    else
                    {
                        dataPack.targetPosition.Add(allMTD[UnityEngine.Random.Range(0, allMTD.Count)].pos);
                    }
                }
                dataPack.canHeroSeeThisTurn = true;
                return dataPack;
            }
            else
            {
                Debug.Log("Nothing usable for ground summon...");
                dataPack.target = pool_targets;
                dataPack.targetPosition = affectedTiles;
                return dataPack;
            }
        }

        if (localTarget == null || localTarget.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            if (abilToUse.targetForMonster == AbilityTarget.ENEMY)
            {
                Debug.Log("Warning: Monster had an enemy-targeted ability, " + abilToUse.refName + " but has no enemy to use it on?");
                dataPack.SetTurnType(TurnTypes.PASS);
                return dataPack;
            }
            Debug.Log("No target for monster ability: " + abilToUse.refName);
        }

        Fighter fightTarget = localTarget as Fighter;
        bool targetHittable = true;

        // Performance hack.
        if (abilToUse.boundsShape == TargetShapes.RECT && abilToUse.targetShape == TargetShapes.POINT && abilToUse.CheckAbilityTag(AbilityTags.CURSORTARGET))
        {
            if (CustomAlgorithms.CheckBresenhamsLOS(GetPos(), fightTarget.GetPos(), MapMasterScript.activeMap))
            {
                dataPack.canHeroSeeThisTurn = true;
                pool_targets.Add(fightTarget);
                dataPack.target.Add(fightTarget);
                dataPack.targetPosition.Add(fightTarget.GetPos()); // Is TargetPosition the right var

                if (abilToUse.CheckAbilityTag(AbilityTags.FORCEPOSITIONTOTARGETSONLY))
                {
                    dataPack.targetPosition.Clear();
                    foreach (Actor target in pool_targets)
                    {
                        dataPack.targetPosition.Add(target.GetPos());
                    }
                }

                return dataPack;
            }
            else
            {
                //Debug.Log("Can't see from " + GetPos() + " to " + fightTarget.GetPos());
            }
        }

        tryDir.Clear();

        Directions getDir = Directions.NORTH;

        if (fightTarget != null)
        {
            getDir = CombatManagerScript.GetDirection(this, fightTarget);
        }

        if (abilToUse.boundsShape == TargetShapes.FLEXCONE)
        {
            tryDir.Add(UIManagerScript.SnapLine(getDir, false));
            tryDir.Add(UIManagerScript.SnapLine(getDir, true));
        }
        else if (abilToUse.boundsShape == TargetShapes.FLEXCROSS)
        {
            tryDir.Add(UIManagerScript.SnapLine(getDir, false));
            tryDir.Add(UIManagerScript.SnapLine(getDir, true));
        }
        else
        {
            tryDir.Add(getDir);
        }

        if (forceDirection != Directions.NEUTRAL)
        {
            tryDir.Clear();
            tryDir.Add(forceDirection);
        }

        int tries = 0;
        while (targetHittable && tries < 200)
        {
            tries++;
            pool_targets.Clear();
            affectedTiles.Clear();

            if (tryDir.Count == 0)
            {
                targetHittable = false;
                break;
            }

            getDir = tryDir[0];

            tryDir.Remove(tryDir[0]);

            //int range = abilToUse.targetRange;
            int range = abilToUse.range;
            TargetShapes shape = abilToUse.boundsShape;

            // This is the list of BOUNDS tiles. Not necessarily affected.
            UIManagerScript.singletonUIMS.abilityUser = this;

            if (shape == TargetShapes.DIRECTLINE || shape == TargetShapes.DIRECTLINE_THICK)
            {
                GameMasterScript.gmsSingleton.SetTempGameData("directline_actor_target", localTarget.actorUniqueID);
            }
            List<Vector2> bounds = UIManagerScript.singletonUIMS.CreateShapeTileList(shape, abilToUse, GetPos(), getDir, range, false);
            //Debug.Log(bounds.Count);
            List<Vector2> possible = bounds;

            if (abilToUse.CheckAbilityTag(AbilityTags.CURSORTARGET))
            {
                Vector2 tPos = Vector2.zero;
                if (bounds.Contains(localTarget.GetPos()))
                {
                    tPos = localTarget.GetPos();
                }
                else
                {
                    float shortest = 999f;
                    float dist = 0f;
                    for (int i = 0; i < bounds.Count; i++)
                    {
                        dist = Vector2.Distance(bounds[i], localTarget.GetPos());
                        if (dist < shortest)
                        {
                            shortest = dist;
                            tPos = bounds[i];
                        }
                    }
                }
                possible = UIManagerScript.singletonUIMS.CreateShapeTileList(abilToUse.targetShape, abilToUse, tPos, getDir, abilToUse.targetRange, false);
            }

            bool isIceDaggers = abilToUse.refName.Contains("icedaggers");

            foreach (Vector2 v2 in possible)
            {
                if (isIceDaggers)
                {
                    if (MapMasterScript.InBounds(v2))
                    {
                        affectedTiles.Add(v2);
                        continue;
                    }

                }
                if (!MapMasterScript.InBounds(v2))
                {
                    continue;
                }

                // Monsters don't need the more lenient vector method, we can use bresenhams for them.
                if (abilToUse.CheckAbilityTag(AbilityTags.LINEOFSIGHTREQ) && !CustomAlgorithms.CheckBresenhamsLOS(GetPos(), v2, MapMasterScript.activeMap))
                {
                    continue;
                }
                MapTileData checkMTD = MapMasterScript.GetTile(v2);
                if (checkMTD.tileType == TileTypes.GROUND)
                {
                    affectedTiles.Add(checkMTD.pos);
                    foreach (Actor act in checkMTD.GetAllTargetable())
                    {
                        pool_targets.Add(act);
                    }
                }
            }

            if (abilToUse.landingTile != LandingTileTypes.NONE)
            {
                switch (abilToUse.landingTile)
                {
                    case LandingTileTypes.ENDOFLINE:
                        if (abilToUse.boundsShape == TargetShapes.FLEXLINE || abilToUse.boundsShape == TargetShapes.HLINE || abilToUse.boundsShape == TargetShapes.VLINE)
                        {
                            Vector2 endOfLine = Vector2.zero;
                            if (abilToUse.CheckAbilityTag(AbilityTags.CENTERED))
                            {
                                endOfLine = GetPos() + (MapMasterScript.xDirections[(int)getDir] * (range + 1));
                            }
                            else
                            {
                                endOfLine = GetPos() + (MapMasterScript.xDirections[(int)getDir] * ((range * 2) + 2));
                            }
                            Debug.Log("End of line for " + getDir + " is " + endOfLine + " relative to " + GetPos());
                            MapTileData mtd = MapMasterScript.GetTile(endOfLine);
                            if ((mtd.IsCollidable(this)) || (!MapMasterScript.CheckTileToTileLOS(GetPos(), endOfLine, this, MapMasterScript.activeMap)))
                            {
                                targetHittable = false;
                                continue;
                            }
                            else
                            {
                                GameMasterScript.bufferedLandingTile = endOfLine;
                            }
                        }
                        break;
                    case LandingTileTypes.FURTHEST:
                        if (abilToUse.boundsShape == TargetShapes.FLEXLINE || abilToUse.boundsShape == TargetShapes.HLINE || abilToUse.boundsShape == TargetShapes.VLINE)
                        {
                            float furthest = 0f;
                            Vector2 far = Vector2.zero;
                            for (int i = 0; i < possible.Count; i++)
                            {
                                if (!MapMasterScript.InBounds(possible[i]))
                                {
                                    continue;
                                }
                                float dist = Vector2.Distance(possible[i], GetPos());
                                if (dist > furthest)
                                {
                                    furthest = dist;
                                    far = possible[i];
                                }
                            }
                            MapTileData mtd = MapMasterScript.GetTile(far);

                            bool ignoreCollidables = false;

                            if (abilToUse.refName != "skill_jadebeetlecharge")
                            {
                                ignoreCollidables = true;
                            }

                            if ((mtd.IsCollidable(this) && !ignoreCollidables) || !MapMasterScript.CheckTileToTileLOS(GetPos(), far, this, MapMasterScript.activeMap))
                            {
                                targetHittable = false;
                                continue;
                            }
                            else
                            {
                                GameMasterScript.bufferedLandingTile = far;
                            }
                        }
                        break;
                }
            }

            if (!pool_targets.Contains(fightTarget))
            {
                targetHittable = false;
                continue;
            }
            else
            {
                targetHittable = true;
                dataPack.direction = getDir;
                break;
            }
        }

        if (tries >= 200)
        {
            Debug.Log("Broke monster ability targeting WHILE loop");
        }
        dataPack.canHeroSeeThisTurn = targetHittable;
        dataPack.target = pool_targets;

        if ((targetHittable) && (abilToUse.CheckAbilityTag(AbilityTags.FORCEPOSITIONTOTARGETSONLY)))
        {
            dataPack.targetPosition.Clear();
            foreach (Actor target in pool_targets)
            {
                dataPack.targetPosition.Add(target.GetPos());
            }
        }
        else
        {
            dataPack.targetPosition = affectedTiles;
        }

        if (abilToUse.refName == "skill_laserfield")
        {
            for (int i = 0; i < dataPack.targetPosition.Count; i++)
            {
                Vector2 v2 = dataPack.targetPosition[i];
                v2.y += 1;
                dataPack.targetPosition[i] = v2;
            }
        }
        if (abilToUse.refName == "skill_laserfield2")
        {
            for (int i = 0; i < dataPack.targetPosition.Count; i++)
            {
                Vector2 v2 = dataPack.targetPosition[i];
                v2.x -= 2f;
                if (v2.x <= 1f)
                {
                    v2.x = 1f;
                }
                dataPack.targetPosition[i] = v2;
            }
        }

        return dataPack;
    }

    bool CheckLOSToHeroPets()
    {
        if (GameMasterScript.heroPCActor.summonedActors == null)
        {
            return false;
        }
        foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Fighter ft = act as Fighter;
            if (!ft.myStats.IsAlive()) continue;
            //if (MapMasterScript.CheckTileToTileLOS(ft.GetPos(), GetPos(), this, MapMasterScript.activeMap))
            if (CustomAlgorithms.CheckBresenhamsLOS(ft.GetPos(), GetPos(), MapMasterScript.activeMap))
            {
                //Debug.Log("Can see the " + ft.actorRefName);
                return true;
            }
            else
            {
                //Debug.Log(act.actorRefName + " is not in LOS");
            }
        }
        return false;
    }

    private void RefreshConsiderAbilities()
    {
        considerAbilities.Clear();

        if (MapMasterScript.activeMap.IsMysteryDungeonMap() && ReadActorData("mystery_king_chaser") == 1)
        {
            return;
        }

        float healthPercent = myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);

        if (monsterPowers == null)
        {
            return;
        }

        if (UnityEngine.Random.Range(0.01f, 1f) <= influenceTurnData.silenceChance)
        {
            if (isBoss && UnityEngine.Random.Range(0, 1f) <= 0.75f)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("resist_seal_log", this);
            }
            else
            {
                return;
            }
        }

        for (int i = 0; i < monsterPowers.Count; i++)
        {
            MonsterPowerData mpd = monsterPowers[i];

            if (mpd == null)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " has a null mpd.");
                continue;
            }
            else if (mpd.abilityRef == null)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " has a null mpd ref.");
                continue;
            }

            //Debug.Log(actorRefName + " " + actorUniqueID + " " + displayName + " " + mpd.abilityRef.refName);

            if (dungeonFloor == MapMasterScript.FINAL_BOSS_FLOOR && actorRefName == "mon_finalbossai")
            {
                int barrierCores = MapMasterScript.CountMobsInZone("mon_barriercore");
                if (mpd.abilityRef.refName == "skill_barrier2")
                {
                    if (barrierCores < 2)
                    {
                        continue;
                    }
                }
                else if (mpd.abilityRef.refName == "skill_turtle2")
                {
                    if (barrierCores == 2 || barrierCores == 0)
                    {
                        continue;
                    }
                }
            }

            if (mpd.abilityRef.passiveAbility) continue;

            if (mpd.abilityRef.CheckAbilityTag(AbilityTags.CANNOT_INHERIT) && actorfaction == Faction.PLAYER) continue;

            AbilityScript localAbil = null;
            for (int z = 0; z < myAbilities.abilities.Count; z++)
            {
                if (mpd.abilityRef.refName == myAbilities.abilities[z].refName)
                {
                    localAbil = myAbilities.abilities[z];
                }
            }

            if (mpd.healthThreshold < healthPercent && !(actorfaction == Faction.PLAYER && mpd.abilityRef.targetForMonster == AbilityTarget.ALLY))
            {
                continue;
            }
            if (UnityEngine.Random.Range(0, 1f) > mpd.chanceToUse)
            {
                continue;
            }
            if (myBehaviorState != mpd.useState && mpd.useState != BehaviorState.ANY)
            {
                continue;
            }
            if (actorfaction == Faction.PLAYER && myBehaviorState != BehaviorState.FIGHT && mpd.abilityRef.targetForMonster == AbilityTarget.SELF)
            {
                // don't use self buffs outside of combat
                continue;
            }

            bool debug = false;
            /* if (mpd.abilityRef.refName.Contains("barrier"))
            {
                Debug.LogError("Checking barrier");
                debug = true;
            } */

            if (!localAbil.CanActorUseAbility(this, mpd.ignoreCosts, debug))
            {
                continue;
            }

            // reqActorData is a key, which must exist in our dictionary at given value
            if (!string.IsNullOrEmpty(mpd.reqActorData))
            {
                int curValue = ReadActorData(mpd.reqActorData);
                if (curValue < mpd.reqActorDataValue)
                {
                    continue;
                }
            }

            if (localAbil.targetForMonster == AbilityTarget.ENEMY)
            {
                if (myTarget == null)
                {
                    continue;
                }
            }

            bool hasTarget = myTarget != null;

            if (hasTarget || mpd.enforceRangesForHeroTargeting)
            {
                Actor checkActor = GameMasterScript.heroPCActor;
                if (hasTarget)
                {
                    checkActor = myTarget;
                }
                int dist = MapMasterScript.GetGridDistance(GetPos(), checkActor.GetPos());
                if (dist < mpd.minRange || dist > mpd.maxRange)
                {
                    continue;
                }
                // in NG++, charge abilities wont be used if the player is likely to get an extra turn
                if (GameStartData.NewGamePlus == 2 && localAbil.chargeTurns >= 1 && actorfaction != Faction.PLAYER && myTarget.IsFighter() && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
                {
                    Fighter ftTarget = myTarget as Fighter;
                    if (ftTarget.actionTimer + ftTarget.cachedBattleData.chargeGain >= 200)
                    {
                        continue;
                    }
                }
            }

            if ((actorfaction == Faction.PLAYER && UnityEngine.Random.Range(0, 1f) <= 0.33f) || !myStats.CheckHasStatusName("status_bloodfrenzy"))
            {
                if (mpd.abilityRef.refName == "skill_fishrush")
                {
                    continue;
                }
            }

            considerAbilities.Add(mpd);
        }
    }

    //Causes us to jump towards our anchor without any consideration for anything else.
    MonsterTurnData ImmediateJumpToAnchorOrPosition(Vector2 vOverrideAnchor = default(Vector2))
    {
        //Find a tile near our anchor and move to it via jump
        MapTileData nearMTD = null;
        bool bJumpOverride = false;

        //We are either jumping directly to a tile, or jumping to our anchor
        if (vOverrideAnchor != default(Vector2))
        {
            bJumpOverride = true;
            nearMTD = MapMasterScript.GetTile(vOverrideAnchor);
        }
        else
        {
            nearMTD = MapMasterScript.GetRandomEmptyTile(anchor.GetPos(), 1, true, true, true);
        }

        //play a poof
        CombatManagerScript.GenerateSpecificEffectAnimation(nearMTD.pos, "SmokePoof", null, true);

        //if we are a playerpet, spawn a chat log entry if we're just jumping because we were called or
        //too far away
        if (actorfaction == Faction.PLAYER && !bJumpOverride)
        {
            StringManager.SetTag(0, displayName);
            GameLogScript.LogWriteStringRef("log_pet_automove", null, TextDensity.VERBOSE);
        }

        //If we're doing this because we're being forced to, clear that out.
        if (myBehaviorState == BehaviorState.PETFORCEDRETURN)
        {
#if UNITY_EDITOR
            Debug_AddMessage("->NEUTRAL: Jumped");
#endif
            SetState(BehaviorState.NEUTRAL);
        }

        //Attempt to jump to the player
        return AttemptMovement(nearMTD.pos, true);
    }


    public MonsterTurnData AttackIfValid(Fighter fightTarget, bool paralyzed)
    {
        float distance = MapMasterScript.GetGridDistance(GetPos(), fightTarget.GetPos());


        bool inLOS = false;

        if (fightTarget.GetActorType() == ActorTypes.HERO && GameMasterScript.heroPCActor.visibleTilesArray[(int)GetPos().x, (int)GetPos().y])
        {
            //inLOS = GameMasterScript.heroPCActor.CheckIfTileIsTrulyVisible(GetPos());
            // No need for vector LOS check for monsters. Edge cases where they can't hit are probably fine.
            inLOS = CustomAlgorithms.CheckBresenhamsLOS(GetPos(), fightTarget.GetPos(), MapMasterScript.activeMap);
        }
        else if (fightTarget.GetActorType() != ActorTypes.HERO)
        {
            inLOS = MapMasterScript.CheckTileToTileLOS(GetPos(), fightTarget.GetPos(), this, GetActorMap());
        }

        // If the hero cannot see us, don't run EITHER conditional above. 

        if (distance <= cachedBattleData.weaponAttackRange && CheckAttribute(MonsterAttributes.CANTATTACK) == 0 
            && !paralyzed && inLOS)
        {
            // Not using an ability, just attacking normally.
            MonsterTurnData mtd = new MonsterTurnData(GameMasterScript.baseAttackAnimationTime, TurnTypes.ATTACK);
            mtd.affectedActors.Add(fightTarget);
            CombatResultPayload crp = CombatManagerScript.Attack(this, fightTarget);
            mtd.results.Add(crp.result);
            mtd.waitTime += crp.waitTime;
            if (mtd.results[0] == CombatResult.MONSTERDIED)
            {
                GameMasterScript.AddToDeadQueue(fightTarget);
                ClearMyTarget();
                RemoveTarget(fightTarget);
            }
            return mtd;
        }
        return null;
    }

    // Returns total wait time.
    private float UseAbility(TurnData td)
    {
        bool heroCanSeeMonster = GameMasterScript.heroPCActor.CanSeeActor(this);
        if (heroCanSeeMonster && UnityEngine.Random.Range(0, 1f) <= CombatManagerScript.CHANCE_BOWMASTERY3 && actorfaction == Faction.ENEMY)
        {
            if (GameMasterScript.heroPCActor.myEquipment.GetWeaponType() == WeaponTypes.BOW)
            {
                if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("bowmastery3"))
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "SkirmisherEffect", null, false);

                    CombatManagerScript.GetSwingEffect(GameMasterScript.heroPCActor, this, GameMasterScript.heroPCActor.myEquipment.GetWeapon(), true);
                    StringManager.SetTag(0, displayName);
                    StringManager.SetTag(1, td.tAbilityToTry.abilityName);
                    GameLogScript.LogWriteStringRef("log_bowmastery3_interrupt");

                    td.tAbilityToTry.ResetCooldown();

                    return 0f;
                }
            }
        }

        if (td.tAbilityToTry.refName == "skill_bladewave")
        {
            SetActorData("targetofbladesid", myTarget.actorUniqueID);
        }

        //if (Debug.isDebugBuild) Debug.Log(td.tAbilityToTry.refName + " used on turn " + GameMasterScript.turnNumber + " " + td.tAbilityToTry.GetCurCooldownTurns() + " " + td.tAbilityToTry.maxCooldownTurns);

        CombatManagerScript.accumulatedCombatWaitTime = 0.0f;
        
        if (!mySpriteRenderer.enabled)
        {
            heroCanSeeMonster = false;
        }
        if (heroCanSeeMonster && !td.tAbilityToTry.CheckAbilityTag(AbilityTags.NOLOGTEXT))
        {
            // #todo - be more strict here with "Uses" display? Make tags for displaying or not
            if (!td.tAbilityToTry.IsDamageAbility() && !td.tAbilityToTry.IsSummonAbility())
            {
                StringManager.SetTag(0, UIManagerScript.redHexColor + displayName + "</color>"); // was orange here
                StringManager.SetTag(1, td.tAbilityToTry.abilityName);
                GameLogScript.LogWriteStringRef("log_ability_used");
            }
            //GameLogScript.GameLogWrite(UIManagerScript.orangeHexColor + this.displayName + "</color> uses <color=yellow>" + td.tAbilityToTry.abilityName + "</color>!",this);
        }

        if (heroCanSeeMonster)
        {
            if (!string.IsNullOrEmpty(td.tAbilityToTry.teachPlayerAbility))
            {
                GameMasterScript.heroPCActor.TryLearnMonsterSkill(td.tAbilityToTry, td.tAbilityToTry.teachPlayerAbility);
            }
        }

        if (td.tAbilityToTry.CheckAbilityTag(AbilityTags.OVERRIDECHILDSFX))
        {
            GameObject go = CombatManagerScript.GetEffect(td.tAbilityToTry.sfxOverride);
            CombatManagerScript.TryPlayAbilitySFX(go, GetPos(), td.tAbilityToTry);
        }

        float waitTime = 0.0f;
        myStats.ChangeStat(StatTypes.STAMINA, -1 * td.tAbilityToTry.staminaCost, StatDataTypes.CUR, true);
        myStats.ChangeStat(StatTypes.ENERGY, -1 * td.tAbilityToTry.energyCost, StatDataTypes.CUR, true);

        abilResults.Clear();
        abilAffectedActors.Clear();

        CombatManagerScript.SetLastUsedAbility(td.tAbilityToTry);

        // Figure out angle stuff here.

        Vector2 usePos = Vector2.zero;
        Directions animDir = GameMasterScript.heroPCActor.lastMovedDirection;

        if (td.target.Count > 0)
        {
            Actor randomTarget = td.target[UnityEngine.Random.Range(0, td.target.Count)];
            usePos = randomTarget.GetPos();
        }
        else
        {
            if (td.targetPosition.Count > 0)
            {
                usePos = td.targetPosition[UnityEngine.Random.Range(0, td.targetPosition.Count)];
            }
        }

        if (usePos == Vector2.zero)
        {
            if (myTarget != null)
            {
                animDir = CombatManagerScript.GetDirection(this, myTarget);
            }
            else
            {
                animDir = lastMovedDirection;
            }

        }
        else
        {
            float angle = CombatManagerScript.GetAngleBetweenPoints(GetPos(), usePos);
            animDir = MapMasterScript.GetDirectionFromAngle(angle);
        }

        if (!string.IsNullOrEmpty(td.tAbilityToTry.instantDirectionalAnimationRef))
        {
            CombatManagerScript.GenerateDirectionalEffectAnimation(GetPos(), animDir, td.tAbilityToTry.instantDirectionalAnimationRef, true);
        }

        // End angle stuff.

        if (myBehaviorState != BehaviorState.FIGHT)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("monincombat", 0);
        }
        else
        {
            GameMasterScript.gmsSingleton.SetTempGameData("monincombat", 1);
        }

        for (int i = 0; i < td.tAbilityToTry.listEffectScripts.Count; i++)
        {
            EffectScript eff = td.tAbilityToTry.listEffectScripts[i];

            bool runEffect = true;

            if (td.tAbilityToTry.hasConditions)
            {
                foreach (EffectConditional ec in td.tAbilityToTry.conditions)
                {
                    if (ec.index == i)
                    {
                        runEffect = false;
                        switch (ec.ec)
                        {
                            case EffectConditionalEnums.STATUSREMOVED:
                                foreach (Actor act in EffectScript.actorsAffectedByAbility)
                                {
                                    if (act.IsFighter())
                                    {
                                        Fighter ft = act as Fighter;
                                        if (ft.myStats.statusesRemovedSinceLastTurn.Count > 0)
                                        {
                                            runEffect = true;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            if (!runEffect) continue;
            eff.originatingActor = this;
            eff.targetActors = td.target;
            eff.positions = td.targetPosition;

            if (eff.effectType == EffectType.MOVEACTOR)
            {
                MoveActorEffect mae = eff as MoveActorEffect;
                if (mae.tActorType == TargetActorType.SINGLE)
                {
                    // This is a move effect that affects the enemy. 
                    if (mae.pullActor)
                    {
                        // Pull the actor toward me.
                        Vector2 nearbyTile = FindRandomPos(true);
                        eff.positions.Clear();
                        eff.positions.Add(nearbyTile);
                    }
                    else
                    {
                        // Push actor. What are we doing?
                    }
                }
            }

            eff.centerPosition = td.centerPosition;
            eff.parentAbility = td.tAbilityToTry;

            waitTime += eff.DoEffect(i);

            foreach (CombatResult reso in eff.results)
            {
                abilResults.Add(reso);
            }
            foreach (Actor affAct in eff.affectedActors)
            {
                abilAffectedActors.Add(affAct);
            }
        }
        // All effects have been processed. Now, process aggro
        // This needs to account for buffing friends
        foreach (Actor affected in abilAffectedActors)
        {
            if (affected.GetActorType() == ActorTypes.MONSTER && affected != this) // Don't aggro yourself.
            {
                Monster monmon = (Monster)affected as Monster;
                if (td.tAbilityToTry.targetForMonster != AbilityTarget.SELF && td.tAbilityToTry.targetForMonster != AbilityTarget.ALLY 
                    && td.tAbilityToTry.targetForMonster != AbilityTarget.PET_ALLY && td.tAbilityToTry.targetForMonster != AbilityTarget.ALLY_ONLY)
                {
                    monmon.AddAggro(this, 15f);
                    monmon.lastActorAttackedBy = this;
                }
            }
        }

        td.affectedActors = abilAffectedActors;
        td.results = abilResults;
        td.canHeroSeeThisTurn = heroCanSeeMonster;

        if (td.tAbilityToTry.passTurns > 0)
        {
            waitTurnsRemaining = td.tAbilityToTry.passTurns;
        }

        if (!heroCanSeeMonster)
        {
            waitTime = 0.0f;
        }
        else
        {
            if (td.tAbilityToTry != null)
            {
                if (td.tAbilityToTry.combatLogText != null && td.tAbilityToTry.combatLogText != "")
                {
                    string displayText = td.tAbilityToTry.combatLogText.Replace("$user", displayName);
                    GameLogScript.GameLogWrite(td.tAbilityToTry.combatLogText, this);
                }

            }
        }
        td.tAbilityToTry.ResetCooldown();
        //if (Debug.isDebugBuild) Debug.Log("Resetting cooldown for " + td.tAbilityToTry.refName + " " + actorRefName + " " + GetPos() + " " + td.tAbilityToTry.GetCurCooldownTurns());
        return waitTime;
    }

    public void SetState(BehaviorState newState)
    {
        //If we are being asked to return to our owner, do not leave this state outside of a request to be neutral
        //If we are in forcedmove, obey nothing else except a request to be neutral
        if ((myBehaviorState == BehaviorState.FORCEMOVE || myBehaviorState == BehaviorState.PETFORCEDRETURN) &&
            newState != BehaviorState.NEUTRAL)
        {
            return;
        }

        //this thing
        if (surpressTraits) return;

        //If we have pacifist attributes, and we're told to fight, we may not depending on the die roll.
        if ((CheckAttribute(MonsterAttributes.PACIFIST) > 0) && (UnityEngine.Random.Range(0, 101) <= CheckAttribute(MonsterAttributes.PACIFIST)) && (newState == BehaviorState.FIGHT))
        {
            ClearMyTarget();
            ClearCombatTargets();
#if UNITY_EDITOR
            Debug_AddMessage("->NEUTRAL: Pacifist");
#endif
            SetState(BehaviorState.NEUTRAL);
            return;
        }

        //if we are being told to fight, and we are not fighting, play a ! over our heads.
        if ((newState == BehaviorState.FIGHT) && (myBehaviorState != BehaviorState.FIGHT))
        {
            CombatManagerScript.SpawnChildSprite("AggroEffect", this, Directions.NORTHEAST, false);
        }

        //toggle the Fear status on us based on whether Run is being turned on or off. 
        if ((newState == BehaviorState.RUN) && (myBehaviorState != BehaviorState.RUN))
        {
            myStats.AddStatus(GameMasterScript.FindStatusTemplateByName("status_feared"), this);
        }
        if ((newState != BehaviorState.RUN) && (myBehaviorState == BehaviorState.RUN))
        {
            myStats.RemoveStatusByRef("status_feared");
        }
        if (newState != BehaviorState.FIGHT && newState != BehaviorState.RUN)
        {
            // If we're no longer fighting clear our aggro data.
            PruneCombatTargets(removeAll:true);
        }

        //now we are this.
        myBehaviorState = newState;

    }

    public AggroData GetTargetAggroData(Actor target)
    {
        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            if (combatTargets[i].combatant == target)
            {
                return combatTargets[i];
            }

        }
        return null;
    }

    public void AddAggro(Actor target, float amount)
    {
        if (target == this)
        {
            return;
        }
        /* if (target.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Debug.Log("WARNING: Do NOT add destructible aggro to " + actorRefName + " " + actorUniqueID);
            return;
        } */
        if (!target.IsFighter())
        {
            return;
        }

        if (target.GetActorType() == ActorTypes.MONSTER && GetActorType() == ActorTypes.MONSTER && GameMasterScript.heroPCActor.GetMonsterPetID() == actorUniqueID && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
        {
            // We must be in the grove, as player's pet
            if (actorfaction == Faction.ENEMY) // and we must be in duel mode
            {
                Monster mn = target as Monster;
                if (mn.isInCorral) // don't hit corral pets!
                {
                    return;
                }
            }
        }

        if (actorRefName == "mon_hoverbot" && target.actorRefName == "mon_fungalcolumn") return;
        if (actorRefName == "mon_fungalcolumn" && target.actorRefName == "mon_hoverbot") return;

        if (actorfaction == Faction.PLAYER)
        {
            if (target.actorRefName == "mon_nightmareking" && ItemDreamFunctions.IsNightmareKingInvincible())
            {
                return;
            }
            else if (target.GetActorType() == ActorTypes.MONSTER)
            {
                Monster tMon = target as Monster;
                if (tMon.foodLovingMonster)
                {
                    return;
                }
            }

        }

        if (target.IsFighter())
        {
            Fighter ft = target as Fighter;
            if (ft.myStats.CheckHasStatusName("status_taunting"))
            {
                amount *= 1.33f;
            }
            amount *= ft.aggroMultiplier;
        }

        if (!CheckTarget(target))
        {
            AddTarget(target);
            if (target == GameMasterScript.heroPCActor)
            {
                if ((GameMasterScript.heroPCActor.myStats.CheckHasStatusName("draik")) && (ReadActorData("draik") < 1))
                {
                    // DRAIK challenge buff.
                    SetActorData("draik", 1);
                    challengeValue += 0.1f;
                    lootChance += 0.15f;
                    myStats.SetLevel(myStats.GetLevel() + 1);
                    myStats.BoostStatByPercent(StatTypes.HEALTH, 0.2f);
                    allMitigationAddPercent -= 0.1f;
                    allDamageMultiplier += 0.15f;
                }
            }
        }
        GetTargetAggroData(target).turnsSinceCombatAction = 0;
        GetTargetAggroData(target).aggroAmount += amount;
    }

    /*
     * If the pet wants to attack a target,
	but it can't get there because something is blocking it
	and the blocker is Mirai
	and we are adjacent to her
	
	Collect all tiles that are
		adjacent to mirai
		not adjacent to me
		not my position
		not solid
		
	Evaluate that collection and find the closest tile to the target
	Jump there and end my turn
     */

    HashSet<MapTileData> validTiles;

    private MapTileData PetJumpOverHero(Vector2 start, Vector2 finish)
    {
        //if I am not a pet, return
        if (actorfaction != Faction.PLAYER)
        {
            return null;
        }

        //if I don't have a combat target, return.
        if (myTarget == null)
        {
            return null;
        }

        //if I am not adjacent to Mirai, return
        Vector2 vHeroPos = GameMasterScript.heroPCActor.GetPos();
        if (MapMasterScript.GetGridDistance(start, vHeroPos) > 1)
        {
            return null;
        }

        //if our goal IS the player's space, return
        if (vHeroPos == finish)
        {
            return null;
        }

        //If we can see our target, don't jump
        if (MapMasterScript.CheckTileToTileLOS(GetPos(), myTarget.GetPos(), this, MapMasterScript.activeMap))
        {
            return null;
        }

        //ok! Let's gather all the tiles adjacent to Mirai that are
        // * not solid
        // * not adjacent to me
        // * in LOS of the target
        if (validTiles == null) validTiles = new HashSet<MapTileData>();
        validTiles.Clear();        
        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
        {
            //look at our bro
            Vector2 newPos = vHeroPos + MapMasterScript.xDirections[i];

            //if the tile is adjacent to ME, don't even.
            if (MapMasterScript.GetGridDistance(GetPos(), newPos) <= 1)
            {
                continue;
            }

            //if it is valid on the map
            if ((newPos.x > 0) && (newPos.y > 0) && (newPos.x < MapMasterScript.activeMap.columns - 1) && (newPos.y < MapMasterScript.activeMap.rows - 1))
            {
                MapTileData newtile = MapMasterScript.GetTile(newPos);

                //if the tile is solid or otherwise unjumpable, don't
                if (newtile.BlocksVision())
                {
                    continue;
                }

                //this could work!
                validTiles.Add(newtile);
            }
        }

        //of the valid locations, we want one that is closest to the target
        //and also has LOS.
        float fBestDistance = 999f;
        MapTileData bestTile = null;
        foreach (var mtd in validTiles)
        {
            //can we see the target?
            if (!MapMasterScript.CheckTileToTileLOS(mtd.pos, myTarget.GetPos(), this, MapMasterScript.activeMap))
            {
                continue;
            }

            //If this is the closest so far, keep it.
            float fDist = Vector2.SqrMagnitude(mtd.pos - myTarget.GetPos());
            if (fDist < fBestDistance)
            {
                fBestDistance = fDist;
                bestTile = mtd;
            }
        }

        //if we have found a tile that matches all the above conditions
        //and is the very best one.
        return bestTile;
    }

    private MonsterMoveData GetMoveTile(Vector2 start, Vector2 finish)
    {
        if (cachedBattleData.maxMoveRange == 0) // This WAS just "moverange" before
        {
            return null;
        }

        pathIsLine = false;
        // First, build the path.

        // Experimental new optimization 5/16/18
        // If we are NOT in combat or seek modes, don't ever build a path. But our pets can build always path, it's ok.
        if (myBehaviorState != BehaviorState.FIGHT && myBehaviorState != BehaviorState.CURIOUS && myBehaviorState != BehaviorState.STALKING && actorfaction != Faction.PLAYER &&
            myBehaviorState != BehaviorState.SEEKINGITEM && myBehaviorState != BehaviorState.PETFORCEDRETURN)
        {
            MapTileData randomnearby = MapMasterScript.activeMap.GetRandomNonCollidableTile(GetPos(), 1, false, false);
            if (randomnearby != null)
            {
                return new MonsterMoveData(randomnearby);
            }
            else // We couldn't possibly move anywhere anyway.
            {
                return null;
            }
        }

        if (cachedBattleData.maxMoveRange > 1 && MapMasterScript.activeMap.dungeonLevelData.GetMetaData("hasholes") == 1)
        {
            MonsterMoveData jumpPath = BuildJumpPath(start, finish);
            if (jumpPath != null)
            {
                return jumpPath;
            }
        }

        MapTileData pfPath = BuildPath(start, finish);
        if (pfPath == null)
        {
            //Debug.Log("No pf path from " + start + " to " + finish);
            if (myBehaviorState == BehaviorState.FORCEMOVE)
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: No Path");
#endif
                SetState(BehaviorState.NEUTRAL);
            }
            // Let's just use a 'dumb' move toward target. It's better than nothing. Failsafe.
            if (myTarget != null && myTargetTile == Vector2.zero)
            {
                myTargetTile = myTarget.GetPos();
            }
            MapTileData best = null;
            float shortestToTarget = 999f;
            Vector2 pos = Vector2.zero;
            MapTileData check = null;
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                pos = GetPos() + MapMasterScript.xDirections[i];
                if (!InMyLocalBounds(pos)) continue;
                check = MapMasterScript.activeMap.mapArray[(int)pos.x, (int)pos.y];

                if (check.tileType != TileTypes.GROUND) continue;

                if (check.IsUnbreakableCollidable(this)) continue;
                if ((IsTileDangerous(check)) && (!check.CheckTag(LocationTags.ISLANDSWATER))) continue;

                float cDist = MapMasterScript.GetGridDistance(pos, myTargetTile);
                if (cDist < shortestToTarget)
                {
                    best = check;
                    shortestToTarget = cDist;
                }
            }
            if (best == null)
            {
#if UNITY_EDITOR
                //Debug.Log("There is no possible path or position for " + this.actorRefName + " at " + GetPos() + " trying to get to " + finish);
                Debug_AddMessage("<color=red>NOPATH!</color>");
#endif
                best = MapMasterScript.GetTile(start);
            }
            return new MonsterMoveData(best);
        }

        int dist = MapMasterScript.GetGridDistance(pfPath.pos, start);
        if (dist > 1)
        {
            //Debug.Log("Warning! Pathfinding weirdness. " + moveRange);
        }

        // Can we move MORE than 1?
        if (cachedBattleData.maxMoveRange == 1)
        {
            // No
            //FightDebugLog("Can't move more than 1");
            return new MonsterMoveData(pfPath);
        }

        // Is there anything beyond the first pathfinding step?
        if (pfPath.child == null && !pathIsLine)
        {
            // No
            //FightDebugLog("Nothing beyond first PF step.");
            return new MonsterMoveData(pfPath);
        }

        // Are we in combat? If not, maybe save your resources. Make some attributes to decide this further, i.e. how much monster cares about a goal.
        if (myBehaviorState != BehaviorState.FIGHT && UnityEngine.Random.Range(0, 101) <= CheckAttribute(MonsterAttributes.ALWAYSUSEMOVEABILITIES))
        {
            if (pathIsLine)
            {
                MapTileData mtd = null;
                for (int i = 0; i < tilePath.Count; i++)
                {
                    mtd = tilePath[i];
                    if (MapMasterScript.GetGridDistance(start, mtd.pos) <= cachedBattleData.maxMoveRange)
                    {
                        return new MonsterMoveData(mtd);
                    }
                }
            }
            return new MonsterMoveData(pfPath);
        }

        // Can move more than 1 using an ability.
        int bestRange = 0;
        AbilityScript bestAbil = GetBestUsableMovementAbility(out bestRange);

        if (bestAbil == null)
        {
            return new MonsterMoveData(pfPath);
        }

        MonsterMoveData localmmd = new MonsterMoveData(null);

        if (pathIsLine)
        {
            localmmd.destinationTile = pfPath;
            localmmd.abilityUsed = bestAbil;
            return localmmd;
        }

        // Don't do pathfinding tile 'chain' stuff below if it's a bres line, because the bres line already receives the best tile.

        MapTileData moveTile = pfPath;

        // Start with the final tile. Let's say the tiles are 10,9  11,9    12,10. Total count of 3. Start with index 2, and decrease from there.

        int steps = 0;
        int index = tilePath.Count - 1;
        MapTileData checkTile = null;

        while (steps < bestRange && index >= 0)
        {
            checkTile = tilePath[index];
            if (!checkTile.IsCollidableEvenWithBreakable(this))
            {
                moveTile = checkTile;
            }
            else
            {
                break;
            }
            index--;
            steps++;
        }
        localmmd.destinationTile = moveTile;
        localmmd.abilityUsed = bestAbil;
        
        if (localmmd.destinationTile == pfPath || MapMasterScript.GetGridDistance(localmmd.destinationTile.pos, GetPos()) <= 1)
        {
            return new MonsterMoveData(localmmd.destinationTile);
        }


        return localmmd;
    }

    public MapTileData BuildPath(Vector2 start, Vector2 finish, bool bAllowFinishTileToBeSolid = false)
    {
        openList.Clear();

        int columns = MapMasterScript.GetColumns();
        int rows = MapMasterScript.GetRows();

        MapTileData startTile = MapMasterScript.GetTile(start);
        startTile.f = 0;

        MapTileData evalTile = startTile;
        PFNode evalNode = Pathfinding.TileToNode(startTile, this);
        MapTileData finalTile = MapMasterScript.GetTile(finish);
        if (finalTile == null)
        {
            return null;
        }

        // Are we just one tile away? And not dangerous?
        float gDist = MapMasterScript.GetGridDistance(start, finish);

        if (gDist <= moveRange)  // Possibly problematic. Was "1" before.
        {
            if (gDist <= moveRange && !finalTile.IsUnbreakableCollidable(this) && MapMasterScript.CheckTileToTileLOS(start, finish, this, MapMasterScript.activeMap))
            {
                return finalTile;
            }
            if (finalTile.IsCollidableActorInTile(this))
            {
                return null;
            }
            return finalTile;
        }

        // Check existing path. Do this better.
        if (tilePath.Count > 0) // Rework using existing paths later.
        {
            // We have an existing path.
            if (tilePath[0] == finalTile)
            {
                // The destination is the same.
                bool pathChanged = false;

                int currentTileIndex = 0;
                for (int i = 1; i < tilePath.Count; i++)
                {
                    MapTileData mtd = tilePath[i];
                    if (mtd.IsCollidable(this))
                    {
                        pathChanged = true;
                        break;
                    }
                    if (mtd == startTile)
                    {
                        currentTileIndex = i;
                        break;
                    }
                }

                if (!pathChanged)
                {
                    MapTileData returnTile = null;
                    // Final tile is 0,0. Start tile is 2,2. So if current is 1,2, we want to move further back in the index.
                    if (currentTileIndex == 0)
                    {
                        returnTile = tilePath[0];
                    }
                    else
                    {
                        returnTile = tilePath[currentTileIndex - 1];
                    }

                    if (MapMasterScript.GetGridDistance(returnTile.pos, GetPos()) <= moveRange)
                    {
                        //Debug.Log("Using existing path. " + GetPos() + " to " + returnTile.pos);
                        return returnTile;
                    }
                    else
                    {
                        //Debug.Log("Existing path is jacked. Can't use it. " + GetPos() + returnTile.pos + " " + currentTileIndex);
                    }

                }
                else
                {
                    //Debug.Log("Path has changed - there is now a collision.");
                }
            }
        }

        // Are the tiles in direct line of sight?

        // 8============================================================================D
        #region (╯°□°)╯︵ ┻━┻
        /*

        bool LOS = MapMasterScript.CheckTileToTileLOS(GetPos(),finish,this, MapMasterScript.activeMap);

        // Using LOS, abilities that move the monster will ALWAYS be used if possible to achieve max distance.

        if ((LOS) && (1==2)) // Temporarily force everything to pathfind, no LOS.
        {
            pathIsLine = true;
            tilePath.Clear();
            int tpIndex = 0;
            //IEnumerable<Vector2> fPoints = CustomAlgorithms.GetPointsOnLine(start, finish);

            CustomAlgorithms.GetPointsOnLineNoGarbage(start, finish);

            Vector2 bestReachable = new Vector2(0, 0);
            int dist = 0;
            //using(var point = fPoints.GetEnumerator())
            {
                //while ((point.MoveNext()) && (searching))
                for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
                {
                    MapTileData checkTile = MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]);
                    int cDist = MapMasterScript.GetGridDistance(CustomAlgorithms.pointsOnLine[i], GetPos());                    
                    //FightDebugLog("Start point is " + point.Current + " cdist " + cDist + " mr " + cachedBattleData.maxMoveRange);
                    if ((cDist > dist) && (GetPos() != CustomAlgorithms.pointsOnLine[i]) && (cDist <= cachedBattleData.maxMoveRange))
                    {
                        bool collideCheck;
                        if (cDist <= 1)
                        {
                            collideCheck = checkTile.IsUnbreakableCollidable(this);
                        }
                        else
                        {
                            collideCheck = checkTile.IsCollidable(this);
                        }
                        if (!collideCheck)
                        {
                            //FightDebugLog("Best reachable from " + start + " to " + finish + " is " + checkTile.pos);
                            bestReachable = CustomAlgorithms.pointsOnLine[i];
                            tilePath.Add(checkTile);
                            if (tpIndex > 0)
                            {
                                tilePath[tpIndex].child = tilePath[tpIndex - 1];
                                tilePath[tpIndex - 1].parent = checkTile;
                            }
                            tpIndex++;
                            dist = cDist;
                        }
                    }
                }
            }

            if (MapMasterScript.GetGridDistance(bestReachable,GetPos()) <= cachedBattleData.maxMoveRange)
            {
                //FightDebugLog("Returning the best reachable which is " + bestReachable);
                return MapMasterScript.GetTile(bestReachable);
            }
            else
            {
                //Debug.Log("Bres failed.");
            }
        }
        */
        #endregion

        float lowestFscore = 99999f;

        MapMasterScript.FlushTilePathfindingData(start, finish, false);

        openList.Add(startTile);
        startTile.open = true;
        MapTileData aSquare = null;
        float localF = 0.0f;
        float gScore = 0.0f;
        MapTileData newTile = null;

        int reps = 0;

        Vector2 newPos = Vector2.zero;

        bool playerPet = actorfaction == Faction.PLAYER && summoner == GameMasterScript.heroPCActor;

        // Experimental new optimization 5/16/18
        // Let's limit our REPS count based on how far the target is
        // For example if we are 2 tiles away from the target, and we're at 1000+ reps, chances are there is no good path
        // And even if there is, it's probably not worth it for the monster anyway, so might as well just wait.

        int MAX_REPS = (int)(gDist * 5); 
        if (PlatformVariables.OPTIMIZE_MONSTER_BEHAVIOR)
        {
            MAX_REPS = (int)(gDist * 4); // More performant
        }

        if (MapMasterScript.activeMap.HasHoles())
        {
            MAX_REPS *= 3;
        }

        if (MAX_REPS > 99)
        {
            MAX_REPS = 99;
        }

        int MAX_OPEN_LIST_SIZE = (int)(gDist * 4) + 1;

        while (openList.Count > 0 && reps < MAX_REPS)
        {
            reps++;
            for (int i = 0; i < adjacentArray.Length; i++)
            {
                adjacentArray[i] = null;
                validTileAdjacent[i] = false;
            }

            lowestFscore = 9999f;
            foreach (MapTileData tile in openList)
            {
                if (tile.f < lowestFscore)
                {
                    lowestFscore = tile.f;
                    evalTile = tile;
                }
            }

            if (evalTile.pos.x == finalTile.pos.x && evalTile.pos.y == finalTile.pos.y)
            {
                // Found our path!
                finalTile = evalTile;
                break;
            }

            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                newPos = evalTile.pos + MapMasterScript.xDirections[i];
                if (InMyLocalBounds(newPos))
                {
                    newTile = MapMasterScript.GetTile(newPos);
                    if (newTile.tileType == TileTypes.GROUND && 
                        !newTile.CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER) ||
                        bAllowFinishTileToBeSolid && newTile.pos == finish)
                    {
                        adjacentArray[i] = newTile;
                        validTileAdjacent[i] = true;
                    }
                }
            }

            openList.Remove(evalTile);
            evalTile.open = false;
            evalTile.closed = true;

            //Allowing pets to walk through players
            bool bIamPet = actorfaction == Faction.PLAYER;
            Vector2 vPlayerLoc = GameMasterScript.GetHeroActor().GetPos();

            for (int i = 0; i < adjacentArray.Length; i++)
            {
                if (!validTileAdjacent[i]) continue;
                aSquare = adjacentArray[i];

                if (aSquare.pos.x == finalTile.pos.x && aSquare.pos.y == finalTile.pos.y)
                {
                    aSquare.parent = evalTile;
                    evalTile.child = aSquare;
                    aSquare.f = -1;
                    InsertTileToOpenList(aSquare);
                    aSquare.open = true;
                    break;
                }

                if (aSquare.closed)
                {
                    continue;
                }

                //don't check this tile for unbreakable collision if it contains the hero
                //and we are a pet.
                if (!bIamPet || aSquare.pos != vPlayerLoc)
                {
                    if ((!bAllowFinishTileToBeSolid || aSquare.pos != finish) && 
                        MapMasterScript.CheckUnbreakableCollision(aSquare.pos, this)) // && (!aSquare.CheckTag(LocationTags.ISLANDSWATER)))
                    {
                        aSquare.closed = true;
                        continue;
                    }
                }

                gScore = evalTile.g + 1;

                if (evalTile.CheckTag(LocationTags.WATER) && !evalTile.CheckTag(LocationTags.ISLANDSWATER))
                {
                    gScore += 1f;
                }

                if (!playerPet && IsTileDangerous(evalTile) && !evalTile.CheckTag(LocationTags.ISLANDSWATER))
                {
                    gScore *= 5f; 
                    // Don't walk into lava
                    if (evalTile.CheckTag(LocationTags.LAVA) && CheckAttribute(MonsterAttributes.LOVESLAVA) == 0)
                    {
                        gScore *= 2;
                        // Especially if we aren't fighting
                        if (myBehaviorState != BehaviorState.FIGHT)
                        {
                            gScore *= 10f;
                        }
                    }
                }

                if (aSquare.open)
                {
                    localF = gScore + aSquare.GetHScore(finalTile);
                    if (localF < aSquare.f)
                    {
                        // better path
                        aSquare.g = gScore;
                        aSquare.f = gScore + aSquare.GetHScore(finalTile);
                        aSquare.parent = evalTile;
                        evalTile.child = aSquare;
                        // Must re-order openList.
                    }
                }
                else
                {
                    // Not in open list   
                    aSquare.parent = evalTile;
                    evalTile.child = aSquare;
                    aSquare.g = gScore; // # of steps to get to this tile
                    aSquare.f = gScore + aSquare.GetHScore(finalTile);
                    InsertTileToOpenList(aSquare);
                    aSquare.open = true;
                }
            }
        }

        if (reps >= MAX_REPS)
        {
            //Debug.Log("Broke pathfinding loop. " + openList.Count + " " + gDist + " " + MAX_REPS);
            //Debug.Log("Broke monster pathfinding loop. " + actorRefName + " " + GetPos() + " " + areaID + " " + myBehaviorState + " trying to reach " + finish + " " + openList.Count + " on floor " + dungeonFloor + " " + MapMasterScript.activeMap.columns + "," + MapMasterScript.activeMap.rows + " " + MapMasterScript.GetAreaID(GetPos()));
            return null;
        }

        // End of pathfinding WHILE loop

        if (openList.Any())
        {
            // Found a path

            bool finished = false;
            MapTileData pTile = finalTile.parent;
            tilePath.Clear();

            if (myBehaviorState != BehaviorState.FIGHT)
            {
                tilePath.Add(finalTile);
            }

            tilePath.Add(pTile); // The first thing in tile path list is the second to last tile
            int tries = 0;
            while (!finished && tries < 250)
            {
                tries++;
                // Need to keep pathfinding back to the start tile.


                if (pTile == null || pTile.parent == null)
                {
                    return null; // Debug this more.
                }

                if (Vector2.Equals(pTile.parent.pos, startTile.pos))
                {
                    // Use pTile as the next move.
                    finished = true;
                    return pTile;
                }
                pTile = pTile.parent;
                tilePath.Add(pTile);
            }
            if (tries >= 250)
            {
                Debug.Log("Broke monster pathfinding while loop");
            }
        }

        return null;
    }

    static int SortTilesByFScore(MapTileData m1, MapTileData m2)
    {
        if (m1.f < m2.f)
        {
            return -1;
        }
        if (m2.f < m1.f)
        {
            return 1;
        }
        return 0;
    }

    static int SortNodesByFScore(PFNode m1, PFNode m2)
    {
        if (m1.f < m2.f)
        {
            return -1;
        }
        if (m2.f < m1.f)
        {
            return 1;
        }
        return 0;
    }

    void InsertTileToOpenList(MapTileData aSquare)
    {
        /*for (int e = 0; e < openList.Count; e++)
        {
            if (aSquare.f < openList[e].f)
            {
                //openList.AddBefore(e, aSquare);
                return;
            }
        }*/

        openList.Add(aSquare);
        //openList.Enqueue(aSquare, aSquare.f);
    }

    public void PostTurnActionProcessingPart1(MonsterTurnData mTurn)
    {

#if UNITY_EDITOR
        //Make monster AI state visible on screen
        if (DebugConsole.IsOpen)
        {
            UpdateDebugAIInfo();
        }
#endif
        if (actorfaction == Faction.PLAYER && bufferedFaction == Faction.PLAYER)
        {
            RepositionWrathBarIfNeeded();
        }

        //UnityEngine.Profiling.Profiler.BeginSample("end text buffer and writealite");
        GameLogScript.EndTextBufferAndWrite();
        //UnityEngine.Profiling.Profiler.EndSample();

        if (MapMasterScript.GetItemWorldAura(GetPos()) == (int)ItemWorldAuras.MONSTER_CLEARSTATUS)
        {
            bool anyRemoved = myStats.RemoveTemporaryNegativeStatusEffects();
            if (anyRemoved)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.GameLogWrite("<color=yellow>" + StringManager.GetString("log_monster_clearstatus") + "</color>", this);
            }
        }

    }

    public void PostTurnActionProcessingPart2(MonsterTurnData mTurn)
    {
        if (mTurn.turnType == TurnTypes.MOVE)
        {
            // Should this be somewhere else? Should effects take place before or after move?
            myStats.CheckRunAndTickAllStatuses(StatusTrigger.ONMOVE);
            movedLastTurn = true;
        }
        else
        {
            movedLastTurn = false;
        }
        myStats.CheckRunAndTickAllStatuses(StatusTrigger.TURNEND);

        if (MapMasterScript.activeMap.IsItemWorld())
        {
            int itemWorldAura = MapMasterScript.GetItemWorldAura(GetPos());
            if (actorfaction != Faction.PLAYER && actorRefName != "mon_itemworldcrystal")
            {
                if (itemWorldAura == (int)ItemWorldAuras.MONSTERREGEN5)
                {
                    int amount = (int)(0.05f * myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX));
                    myStats.ChangeStat(StatTypes.HEALTH, amount, StatDataTypes.CUR, true);
                    GameMasterScript.heroPCActor.CheckForLimitBreakOnDamageTaken(amount);
                    BattleTextManager.NewDamageText(amount, true, Color.green, GetObject(), 0f, 1.0f);
                }
            }
        }

        if (TurnsSinceLastCombatAction > GameMasterScript.gmsSingleton.globalOutOfCombatLimit)
        {            
            myStats.CheckRunAndTickAllStatuses(StatusTrigger.OUTOFCOMBATTURN);
        }

        myStats.RemoveQueuedStatuses();
    }

    public MonsterTurnData PrepareChargeTurn(TurnData checkData)
    {
        // Display danger squares.
        createWarningSquaresSubEffect.summonActorRef = "obj_dangersquare";

        createWarningSquaresSubEffect.originatingActor = this;
        createWarningSquaresSubEffect.centerPosition = checkData.centerPosition;
        createWarningSquaresSubEffect.positions.Clear();
        createWarningSquaresSubEffect.targetActors.Clear();
        foreach (Vector2 v2 in checkData.targetPosition)
        {
            createWarningSquaresSubEffect.positions.Add(v2);
        }
        foreach (Actor act in checkData.target)
        {
            createWarningSquaresSubEffect.targetActors.Add(act);
        }

        createWarningSquares.CopyAllButEffects(checkData.tAbilityToTry);
        createWarningSquaresSubEffect.summonDuration = checkData.tAbilityToTry.chargeTurns + 1;
        createWarningSquaresSubEffect.dieWithSummoner = true;
        createWarningSquaresSubEffect.actOnlyWithSummoner = true; // New code to prevent desync.

        // Locked abilities, like Jade Beetle charge, should child the tiles to the target, whatever that is.
        if (checkData.tAbilityToTry.CheckAbilityTag(AbilityTags.LOCKSQUARETOTARGET))
        {
            RemoveActorData("locktarget");
            if (checkData.target.Count > 0)
            {
                lockedTargetForDangerTiles = checkData.target[0];
                if (lockedTargetForDangerTiles.IsFighter())
                {
                    Fighter ft = lockedTargetForDangerTiles as Fighter;

                    /* StatusEffect added = ft.myStats.AddStatusByRef("status_targetwarning", this, checkData.tAbilityToTry.chargeTurns + 1);
                    added.addedByActorID = actorUniqueID; */
                    SetActorData("locktarget", ft.actorUniqueID);

                    //myStats.AddStatusByRef("status_createtargetwarning", this, 10);

                    TargetingLineScript.CreateTargetingLine(GetObject(), ft.GetObject());
                }
            }
        }
        else
        {
            lockedTargetForDangerTiles = null;
            RemoveActorData("locktarget");
        }

        if (checkData.tAbilityToTry.refName != "skill_summonmedirays") // #todo - Data drive this, "don't create warning tiles"
        {
            createWarningSquaresSubEffect.DoEffect();
        }

        storingTurnData = true;
        waitTurnsRemaining = checkData.tAbilityToTry.chargeTurns;
        storeTurnData = checkData;

        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(GetPos(), "ChargingSkillParticles", null);
        particles.transform.SetParent(GetObject().transform);
        particles.transform.localScale = Vector3.one;
        myMovable.AttachParticleSystem("charging_skill", particles);

        //if (checkData.tAbilityToTry.abilityFlags[(int)AbilityFlags.SELFDESTRUCT] == "skill_selfdestruct1" || checkData.tAbilityToTry.abilityFlags[(int)(int)AbilityFlags.SELFDESTRUCT] == "skill_fungaldestruct")
        if (checkData.tAbilityToTry.abilityFlags[(int)AbilityFlags.SELFDESTRUCT])
        {
            StatusEffect exploder = new StatusEffect();
            StatusEffect temp = GameMasterScript.FindStatusTemplateByName("status_preexplode");
            exploder.CopyStatusFromTemplate(temp);
            myStats.AddStatus(exploder, this);
            StringManager.SetTag(0, displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("log_monsterexplode"), this);
        }
        else
        {
            if (!String.IsNullOrEmpty(checkData.tAbilityToTry.chargeText))
            {
                GameLogScript.GameLogWrite(checkData.tAbilityToTry.chargeText, this);
            }
            else
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_monsterchargeability"), this, TextDensity.VERBOSE);
            }
        }

        return myMonsterTurnData.ChargeTurn();
    }

    public void SetMyTarget(Actor act)
    {       
        if (act != null && act.IsFighter())
        {
            myTarget = act;
        }
        else
        {
            myTarget = null;
        }
    }

    MonsterTurnData CheckForConditionsThatPassTurn()
    {
        if (CheckAttribute(MonsterAttributes.CANTACT) > 0)
        {
            return myMonsterTurnData.Pass();
        }

        if (UnityEngine.Random.Range(0, 101) < CheckAttribute(MonsterAttributes.LAZY) && myBehaviorState == BehaviorState.NEUTRAL)
        {
            return myMonsterTurnData.Pass();
        }

        if (CheckAttribute(MonsterAttributes.STARTASLEEP) > 0)
        {
            if (myBehaviorState == BehaviorState.NEUTRAL && myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) == 1.0f)
            {
                if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)GetPos().x, (int)GetPos().y] || MapMasterScript.activeMap.floor == MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR)
                {
                    return myMonsterTurnData.Pass();
                }
            }
        }

        if (sleepUntilSeehero)
        {            
            if (GameMasterScript.heroPCActor.CanSeeActor(this))
            {
                sleepUntilSeehero = false;
                if (actorRefName == "mon_dirtbeak_library" && GameMasterScript.heroPCActor.ReadActorData("dirtbeak_library") < 1)
                {
                    Cutscenes.DirtbeakLibraryIntro(this);
                    return myMonsterTurnData.Pass();
                }
                else if (actorRefName != "mon_dirtbeak_library")
                {
                    // Sleeping companions.
                    return myMonsterTurnData.Pass();
                }
            }
            else
            {
                return myMonsterTurnData.Pass();
            }
        }

        // To deal with edge cases where a ton of monsters spawn near stairs and hamper progress, there is now a short turn buffer
        // During this time, monsters will not act (but will accumulate CT) unless they are attacked
        // This should allow players to spend a turn or two eating or getting into a favorable position before the Fun begins q
        if (MapMasterScript.activeMap.effectiveFloor < 15 && GameStartData.NewGamePlus == 0 && !MapMasterScript.activeMap.IsBossFloor())
        {
            int turnDiff = GameMasterScript.turnNumber - GameMasterScript.heroPCActor.ReadActorData("turn_entered_floor");
            if (myBehaviorState != BehaviorState.FIGHT && turnDiff < GameMasterScript.MONSTER_WAIT_TIME_NEWFLOOR && myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) >= 1.0f)
            {
                return myMonsterTurnData.Pass();
            }
        }

        // On SWITCH ONLY, if we are NOT a nightmare queen, and not engaged in any interesting behavior, and undamaged
        // And we are VERY far from the player... don't do anything. Save some cycles on huge maps with tons of mobs.
        if (PlatformVariables.OPTIMIZE_MONSTER_BEHAVIOR)
        {
            if (myBehaviorState == BehaviorState.NEUTRAL && myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) >= 0.999f)
            {
                if (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), GetPos()) >= 25f && !(ItemDreamFunctions.NightmareKingAlive() && actorRefName != "mon_nightmareking"))
                {
                    return myMonsterTurnData.Pass();
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    void CheckForCrabGrab()
    {
        if (myStats.CheckHasStatusName("status_crabgrab"))
        {
            moveRange = 0;
            cachedBattleData.maxMoveRange = 0;
            if (anchor == null)
            {
                myStats.RemoveStatusByRef("status_crabgrab");
                moveRange = 1;
                cachedBattleData.maxMoveRange = 1;
            }
            else if (MapMasterScript.GetGridDistance(GetPos(), anchor.GetPos()) > 2)
            {
                myStats.RemoveStatusByRef("status_crabgrab");
                moveRange = Math.Max(1, moveRange);
                cachedBattleData.maxMoveRange = Math.Max(1, cachedBattleData.maxMoveRange);

                //All of this shit crumbles if the crab is a pet
                //we don't want to remove statuses from ourselves if our anchor is the player
                //but we're too far away -_-
                if (actorfaction != Faction.PLAYER)
                {
                    Fighter ft = anchor as Fighter;
                    ft.myStats.RemoveStatusByRef("status_paralyzed");
                    ft.myStats.RemoveStatusByRef("crabbleed");
                    anchor = null;
                }
            }
        }
    }

    void CheckForBloodlustOrFrenzy()
    {
        if (myStats.CheckHasStatusName("status_bloodlust") && !myStats.CheckHasStatusName("status_bloodfrenzy"))
        {
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.IsFighter())
                {
                    Fighter ft = act as Fighter;
                    if (MapMasterScript.GetGridDistance(ft.GetPos(), GetPos()) <= 6f)
                    {
                        if (ft.myStats.IsBleeding())
                        {
                            // Add the status of Blood Frenzy
                            StringManager.SetTag(0, displayName);
                            GameLogScript.GameLogWrite(StringManager.GetString("frenzy_log"), this);
                            BattleTextManager.NewText(StringManager.GetString("frenzy_bt"), GetObject(), Color.red, 0f);

                            myStats.AddStatusByRef("status_bloodfrenzy", this, 9);

                            turnsToLoseInterest = 999;
                            AddAttribute(MonsterAttributes.BERSERKER, 25);
                            if (myBehaviorState != BehaviorState.FIGHT)
                            {
                                myActorOfInterest = ft;
                                myActorOfInterestUniqueID = ft.actorUniqueID;
                                myTargetTile = ft.GetPos();
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    void CheckForCharmEffects()
    {
        if (UnityEngine.Random.Range(0, 1f) < influenceTurnData.charmChance && !isChampion && !isBoss)
        {
            if (bufferedFaction == actorfaction)
            {
                bufferedFaction = actorfaction;
                //actorfaction = Faction.PLAYER; // Force to player. Ideally, charm could be used by enemies on YOUR creatures...
                foreach (StatusEffect se in myStats.GetAllStatuses())
                {
                    if (se.listEffectScripts.Count > 0)
                    {
                        if (se.listEffectScripts[0].effectType == EffectType.INFLUENCETURN)
                        {
                            // #TODO - Make this language independent!
                            if (se.listEffectScripts[0].effectRefName == "charmed")
                            {
                                ChangeMyFaction(se.listEffectScripts[0].originatingActor.actorfaction);
                                //Debug.Log("Test " + se.listEffectScripts[0].originatingActor.actorfaction);
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            ChangeMyFaction(bufferedFaction);
        }
    }

    MonsterTurnData CheckForFearBehavior()
    {
        float localFearChance = influenceTurnData.fearChance;
        bool ignoreFear = false;

        if (influenceTurnData.fearChance > 0.0f)
        {

            if (UnityEngine.Random.Range(0, 1f) > influenceTurnData.fearChance)
            {
                ignoreFear = true;
            }

            if (isChampion && !ignoreFear)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.6f)
                {
                    ignoreFear = true;
                }
            }
            if (isBoss && !ignoreFear)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.75f)
                {
                    ignoreFear = true;
                }
            }

            RemoveActorData("fear_cantrun");

            if (!ignoreFear)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("log_monster_feared");
                Vector2 newPos = FindRandomPos(true);
                SetActorData("feared", 1);
                if (Vector2.Equals(newPos, new Vector2(0, 0)))
                {
                    SetActorData("fear_cantrun", 1);
                    GameLogScript.LogWriteStringRef("log_monster_cantrun");
                    return myMonsterTurnData.Pass();
                }
                else
                {

                    return AttemptMovement(newPos);
                }
            }
            else
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("log_monster_resist_fear");
                ignoreFear = true;
            }
        }

        if (!ignoreFear)
        {
            RemoveActorData("feared");
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForSecretPassTurn(MapTileData currentTile)
    {
        if (currentTile == null)
        {
            // This should never happen but if it does...
            myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
            GameMasterScript.AddToDeadQueue(this, true);
            return myMonsterTurnData.Pass();
        }
        // Secret monsters do nothing until the area is discovered or they are line of sight.
        if (currentTile.CheckTag(LocationTags.SECRET))
        {
            if (MapMasterScript.activeMap.CheckMTDArea(currentTile) == MapMasterScript.GetFillArea().areaID)
            {
                // Do nothing
            }
            else
            {
                if (!GameMasterScript.heroPCActor.exploredAreas[MapMasterScript.activeMap.CheckMTDArea(currentTile)] && 
                    myBehaviorState != BehaviorState.FIGHT && GetNumCombatTargets() == 0)
                {
                    //if (!MapMasterScript.CheckTileToTileLOS(GetPos(), GameMasterScript.heroPCActor.GetPos(), this, MapMasterScript.activeMap))
                    if (!GameMasterScript.heroPCActor.CanSeeActor(this))
                    {
                        return myMonsterTurnData.Pass();
                    }
                }
            }
        }
        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForStun()
    {
        if (influenceTurnData.anyChange)
        {
            // Something impacts our turn.
            float localStun = influenceTurnData.stunChance;
            if (isBoss)
            {
                localStun *= 0.25f;
            }
            if (UnityEngine.Random.Range(0, 1.0f) < localStun)
            {
                // Stunned
                influenceTurnData.stunnedThisTurn = true;
                return myMonsterTurnData.Pass();
            }

            if (isBoss && localStun > 0f)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("log_monster_resists_stun", GameMasterScript.heroPCActor);
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndExecuteStoredTurn()
    {
        // First, error checking.
        if (storingTurnData)
        {
            if (storeTurnData == null) 
            {
                Debug.Log("WARNING: " + actorRefName + " " + actorUniqueID + " at " + GetPos() + " was storing turn data, but the data has been lost.");
                waitTurnsRemaining = 0;
                storingTurnData = false;
                myMovable.RemoveParticleSystem("charging_skill");
            }
            else
            {
                if (storeTurnData.GetTurnType() == TurnTypes.ABILITY)
                {
                    if (storeTurnData.tAbilityToTry == null)
                    {
                        Debug.Log("WARNING: " + actorRefName + " " + actorUniqueID + " at " + GetPos() + " was storing ability turn data, but the ability data has been lost.");
                        waitTurnsRemaining = 0;
                        storingTurnData = false;
                        myMovable.RemoveParticleSystem("charging_skill");
                    }
                }
            }

        }

        // We are being forced to "wait" from some effect or another, so tick down our wait counter and do nothing.
        if (waitTurnsRemaining > 0 && !storingTurnData)
        {
            waitTurnsRemaining--;
            return myMonsterTurnData.Pass();
        }
        else
        {
            if (waitTurnsRemaining > 0)
            {
                waitTurnsRemaining--;
            }

            // We were storing a turn, and it's time to execute that turn.
            if (storingTurnData && waitTurnsRemaining <= 0)
            {
                storingTurnData = false;
                myMovable.RemoveParticleSystem("charging_skill");
                // Execute the stored turn.
                if (summonedActors != null)
                {
                    foreach (Actor act in summonedActors)
                    {
                        if (act.actorRefName == "obj_dangersquare" || act.actorRefName == "obj_friendlydangersquare")
                        {
                            GameMasterScript.AddToDeadQueue(act);
                        }
                    }
                }

                switch (storeTurnData.GetTurnType())
                {
                    case TurnTypes.ABILITY:

                        Actor targetOfLockedAbility = null;
                        TargetingLineScript findLine;

                        // Is locked actor still alive? If so, remove the bullseye sprite FX
                        if (storeTurnData.tAbilityToTry.CheckAbilityTag(AbilityTags.LOCKSQUARETOTARGET))
                        {
                            int lockedActorID = ReadActorData("locktarget");
                            targetOfLockedAbility = GameMasterScript.gmsSingleton.TryLinkActorFromDict(lockedActorID);
                            if (targetOfLockedAbility != null)
                            {
                                // It MUST be a fighter, because the thing that sets "locktarget" only works on fighters.
                            }
                            findLine = GetObject().GetComponentInChildren<TargetingLineScript>();
                            if (findLine != null)
                            {
                                findLine.gameObject.SetActive(false);
                                GameMasterScript.ReturnToStack(findLine.gameObject, "TargetingLine");
                            }
                        }

                        if (GetPos() != storeTurnData.centerPosition)
                        {
                            break;
                        }
                        if (!storeTurnData.tAbilityToTry.CanActorUseAbility(this, CanIgnoreCosts(storeTurnData.tAbilityToTry)))
                        {
                            break;
                        }
                        // Double check target is still hittable? Or do we trust the data is good?
                        TurnData checkData = null;

                        if (storeTurnData.tAbilityToTry.CheckAbilityTag(AbilityTags.MONSTERFIXEDPOS))
                        {
                            checkData = storeTurnData;
                        }
                        else
                        {
                            if (myTarget == null)
                            {
                                SetMyTarget(EvaluateTargetsByRangeAndAggro());
                            }
                            checkData = EvaluateAbilityTiles(storeTurnData.tAbilityToTry, storeTurnData.direction, myTarget);
                        }

                        if (checkData.target != null)
                        {
                            List<Actor> remover = new List<Actor>();
                            for (int i = 0; i < checkData.target.Count; i++)
                            {
                                if (!checkData.targetPosition.Contains(checkData.target[i].GetPos()))
                                {
                                    remover.Add(checkData.target[i]);
                                }
                            }
                            foreach (Actor act in remover)
                            {
                                checkData.target.Remove(act);
                            }
                        }

                        float waitTime = UseAbility(checkData); // WAS storeturn data

                        MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                        monTurn.affectedActors = checkData.affectedActors; // WAS storeTurnData - Jul 17
                        monTurn.results = checkData.results; // WAS storeTurnData - Jul 17
                        if (waitTime > 0.0f)
                        {
                            // Process the effects visually
                            CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                            return monTurn;
                        }
                        else
                        {
                            // No wait time
                            CombatManagerScript.cmsInstance.ClearQueuedEffects();
                            return monTurn;
                        }
                }
            }
            else if (storingTurnData && waitTurnsRemaining > 0)
            {
                // Still charging
                return myMonsterTurnData.Pass();
            }
        }

        return myMonsterTurnData.Continue();
    }

    // At low healths, champs get MEANER and STRONGER
    void CheckForChampionEnrage()
    {
        if (isChampion)
        {
            if (myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.33f) // Champion ENRAGE!
            {
                if (ReadActorData("bigmode") != 1)
                {
                    myAnimatable.SetAllSpriteScale(1.35f);
                }
                else
                {
                    myAnimatable.SetAllSpriteScale(1.65f);
                }

                if (!isEnraged)
                {
                    StringManager.SetTag(0, displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("enraged_log"), this);
                    isEnraged = true;
                    allDamageMultiplier += 0.1f;
                    //allMitigationAddPercent -= 0.15f; // was additive?
                    SetBattleDataDirty();
                }
            }
        }
    }

    // Some effects may cause a monster to forcibly move to a specific tile
    // In this case, this overrides almost all other behaviors
    MonsterTurnData CheckForForcedMove()
    {
        if (myBehaviorState == BehaviorState.FORCEMOVE)
        {
            if (MapMasterScript.GetGridDistance(GetPos(), myTargetTile) <= 1)
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Forcemove Over");
#endif
                SetState(BehaviorState.NEUTRAL);
            }
            else
            {
                MonsterMoveData mmd = GetMoveTile(GetPos(), myTargetTile);
                if (mmd == null)
                {
                    // Can't move...
#if UNITY_EDITOR
                    Debug_AddMessage("->NEUTRAL: Can't Move");
#endif
                    SetState(BehaviorState.NEUTRAL);
                    return myMonsterTurnData.Pass();
                }
                else
                {
                    if (mmd.abilityUsed == null)
                    {
                        return AttemptMovement(mmd.destinationTile.pos);
                    }
                    else
                    {
                        return ExecuteMovementAbility(mmd);
                    }
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    void UpdatePlayerPetTargets()
    {
        if (actorfaction == Faction.PLAYER && myBehaviorState != BehaviorState.FIGHT)
        {
            // This is an ally of the player, so assist the player if player is in combat.
            // Just copy player's aggro data exactly for now.
            // And uh, don't attack itself.
            for (int i = 0; i < GameMasterScript.heroPCActor.GetNumCombatTargets(); i++)
            {
                AggroData ad = GameMasterScript.heroPCActor.combatTargets[i];
                if (!CheckTarget(ad.combatant) && ad.combatant != this)
                {
                    if (summoner == null && MapMasterScript.activeMap.IsItemWorld() && ad.combatant.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = ad.combatant as Monster;
                        if (mn.isItemBoss)
                        {
#if UNITY_EDITOR
                            Debug_AddMessage("->RUN: Item Boss");
#endif
                            SetState(BehaviorState.RUN);
                            continue;
                        }
                    }

                    if (MapMasterScript.GetGridDistance(GetPos(), ad.combatant.GetPos()) < 8)
                    {
                        AddTarget(ad.combatant);
                        AddAggro(ad.combatant, ad.aggroAmount);
                    }
                }
            }
        }
    }

    // Monster picks it's primary target based on all combatants it has in its list.
    void PickTargetFromTargetList(bool petMustStayNextToPlayer)
    {
        if (GetNumCombatTargets() > 0)
        {
            SetMyTarget(EvaluateTargetsByRangeAndAggro(petMustStayNextToPlayer));
            if (myTarget != null)
            {
                Fighter ft = myTarget as Fighter;
                if (ft.myStats.CheckHasStatusName("status_mmlucky"))
                {
                    actorFlags[(int)ActorFlags.STARTCOMBATLUCKY] = true;
                }
#if UNITY_EDITOR
                Debug_AddMessage("->FIGHT: Got Target");
#endif

                SetState(BehaviorState.FIGHT);
                if (myTarget != null)
                {
                    myTargetTile = myTarget.GetPos();
                }
                //Debug.Log("Now has target: " + myTarget.actorRefName); 
            }
            else
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Lost Target");
#endif
                // Wait, don't do this if we're in forced return to player mode.
                if (myBehaviorState != BehaviorState.PETFORCEDRETURN)
                {
                    SetState(BehaviorState.NEUTRAL); // Is this correct?
                }                
            }
        }
    }

    MonsterTurnData CheckForConfusion(int weaponRange)
    {
        
        // Confused!
        //if (MapMasterScript.GetGridDistance(GetPos(), myTarget.GetPos()) <= weaponRange)
            
        clearTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GetPos(), 1);
        List<Actor> possibleTargets = MapMasterScript.GetAllTargetableInTiles(clearTiles);
        //possibleTargets.Remove(this);
        Fighter targ = null;
        if (possibleTargets.Count > 0)
        {
            targ = possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)] as Fighter;
            //UIManagerScript.PlayCursorSound("Confusion");

            StringManager.SetTag(0, displayName);
            GameLogScript.LogWriteStringRef("confused_attack_log", this);
            MonsterTurnData mtd = new MonsterTurnData(GameMasterScript.baseAttackAnimationTime, TurnTypes.ATTACK);
            mtd.affectedActors.Add(targ);
            CombatResultPayload crp = CombatManagerScript.Attack(this, targ);
            mtd.results.Add(crp.result);
            mtd.waitTime += crp.waitTime;
            return mtd;
        }
            
        
        return myMonsterTurnData.Continue();
    }

    void TryCallingForHelp()
    {        
        if (CheckAttribute(MonsterAttributes.CALLFORHELP) > 0 && myBehaviorState == BehaviorState.FIGHT && myTarget != null)
        {
            // Monsters that CAN call for help will do so below a certain % health value
            if (myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= (CheckAttribute(MonsterAttributes.CALLFORHELP) / 100f))
            {
                List<Monster> possibleActors = new List<Monster>();
                for (int i = 0; i < MapMasterScript.activeMap.actorsInMap.Count; i++)
                {
                    Actor act = MapMasterScript.activeMap.actorsInMap[i];
                    if ((act.GetActorType() != ActorTypes.MONSTER) || (act == this) || (act.destroyed) || (act.actorfaction != actorfaction))
                    {
                        continue;
                    }
                    Monster mn = act as Monster;
                    if ((!mn.myStats.IsAlive()) || (mn.monFamily != monFamily))
                    {
                        continue;
                    }

                    // Monster IS in the same family and is alive.
                    if (mn.myBehaviorState == BehaviorState.RUN)
                    {
                        continue;
                    }
                    if (mn.CheckTarget(myTarget))
                    {
                        continue;
                    }

                    if (Vector2.Distance(GetPos(), mn.GetPos()) <= helpRange)
                    {
                        possibleActors.Add(mn);
                    }
                }
                // If there are monsters within our help range, call out to them and those monsters immediately become hostile
                if (possibleActors.Count > 0)
                {
                    for (int x = 0; x < possibleActors.Count; x++)
                    {
#if UNITY_EDITOR
                        possibleActors[x].Debug_AddMessage("->FIGHT: Helping!");
#endif
                        possibleActors[x].SetState(BehaviorState.FIGHT);
                        possibleActors[x].AddAggro(myTarget, 50f);
                    }
                    StringManager.SetTag(0, displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("mon_call_for_help_log"), this);
                    BattleTextManager.NewText(StringManager.GetString("help_bt"), GetObject(), Color.yellow, 0.0f);
                }
                else
                {

                }
            }
        }
    }

    void CheckIfDestinationTileReached()
    {
        // Has the monster already reached its target tile?
        if (myTargetTile != Vector2.zero)
        {
            // We were curious, and we arrived at the destination
            if (myBehaviorState == BehaviorState.CURIOUS && myTargetTile == GetPos())
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Curiosity Sated");
#endif
                SetState(BehaviorState.NEUTRAL);
                myTargetTile = Vector2.zero;
            }
            else if ((myTargetTile == GetPos()) && (myBehaviorState != BehaviorState.SEEKINGITEM))
            {
                // Not curious, but we're still satisfied
                myTargetTile = Vector2.zero;
            }
        }
    }

    MonsterTurnData CheckForCombineWithOtherMonster()
    {
        // Mob combining regardless of behavior state. It just happens. Eventually, make monsters WANT to move nearby to combine.
        if (IsValidCombinableMob())
        {
            float healthThreshold = myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * (CheckAttribute(MonsterAttributes.COMBINABLE)) / 100f;
            if (myStats.GetCurStat(StatTypes.HEALTH) < healthThreshold)
            {
                List<MapTileData> nearbyTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GetPos(), 1);
                Monster combiner = null;

                for (int i = 0; i < nearbyTiles.Count; i++)
                {
                    MapTileData mtd = nearbyTiles[i];
                    for (int x = 0; x < mtd.GetAllActors().Count; x++)
                    {
                        Actor act = mtd.GetAllActors()[x];
                        if (act.GetActorType() == ActorTypes.MONSTER && act.actorfaction == actorfaction)
                        {
                            Monster mon = (Monster)act as Monster;
                            if (mon.actorRefName == actorRefName && mon.actorUniqueID != actorUniqueID && mon.myStats.IsAlive() && mon.IsValidCombinableMob())
                            {
                                // Let's combine!
                                combiner = mon;
                                break;
                            }
                        }
                    }
                    if (combiner != null)
                    {
                        break;
                    }
                }

                // Did we find something to combine?
                if (combiner != null)
                {
                    if (UnityEngine.Random.Range(0, 1.0f) < influenceTurnData.rootChance)
                    {
                        StringManager.SetTag(0, displayName);
                        GameLogScript.LogWriteStringRef("log_monster_cantmove", this);

                        return myMonsterTurnData.Pass();
                    }
                    SetActorData("abouttocombine", 1);

                    combiner.myStats.ChangeStat(StatTypes.HEALTH, myStats.GetCurStat(StatTypes.HEALTH), StatDataTypes.ALL, true);
                    combiner.myStats.ChangeStat(StatTypes.ENERGY, myStats.GetCurStat(StatTypes.ENERGY), StatDataTypes.ALL, true);
                    combiner.myStats.ChangeStat(StatTypes.STAMINA, myStats.GetCurStat(StatTypes.STAMINA), StatDataTypes.ALL, true);
                    combiner.myStats.BoostStatByPercent(StatTypes.STRENGTH, 1.4f);
                    combiner.myStats.BoostStatByPercent(StatTypes.SWIFTNESS, 1.4f);
                    combiner.myStats.BoostStatByPercent(StatTypes.GUILE, 1.4f);
                    combiner.myStats.BoostStatByPercent(StatTypes.SPIRIT, 1.4f);
                    combiner.myStats.BoostStatByPercent(StatTypes.DISCIPLINE, 1.4f);
                    combiner.myStats.ChangeStat(StatTypes.CHARGETIME, 10f, StatDataTypes.ALL, true);
                    combiner.myStats.SetLevel(combiner.myStats.GetLevel() + 1);
                    List<Item> itemsToMove = new List<Item>();
                    foreach (Item itm in myInventory.GetInventory())
                    {
                        itemsToMove.Add(itm);
                    }
                    foreach (Item itm in itemsToMove)
                    {
                        combiner.myInventory.AddItemRemoveFromPrevCollection(itm, true);
                    }
                    myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
                    GameMasterScript.AddToDeadQueue(this);
                    MoveSelf(combiner.GetPos(), false);

                    StringManager.SetTag(0, displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("combined_log"), this);
                    BattleTextManager.NewText(StringManager.GetString("combined_bt"), combiner.GetObject(), Color.yellow, 1.5f);

                    whoKilledMe = null;
                    myMonsterTurnData.Clear();
                    myMonsterTurnData.waitTime = 0.2f;

                    myMonsterTurnData.turnType = TurnTypes.PASS;
                    return myMonsterTurnData;
                }
            }
        }
        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndUsePullAbility()
    {
        // Custom puller code
        Fighter pullTarget = null;
        AbilityScript pullAbility = null;
        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript abil = FetchLocalAbility(considerAbilities[i].abilityRef);
            if (abil.refName == "skill_pullmon" || abil.refName == "skill_fungalpull")
            {
                pullAbility = abil;
                for (int a = 0; a < MapMasterScript.activeMap.actorsInMap.Count; a++)
                {
                    if (MapMasterScript.activeMap.actorsInMap[a].GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = MapMasterScript.activeMap.actorsInMap[a] as Monster;
                        if (((mn.actorfaction == actorfaction) && (abil.refName == "skill_pullmon")) || ((mn.actorfaction == Faction.DUNGEON) && (abil.refName == "skill_fungalpull")))
                        {
                            float dist = MapMasterScript.GetGridDistance(GetPos(), mn.GetPos());
                            if ((dist > 2f) && (dist <= abil.range))
                            {
                                if (MapMasterScript.CheckTileToTileLOS(GetPos(), mn.GetPos(), this, MapMasterScript.activeMap))
                                {
                                    pullTarget = mn;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (pullTarget != null) break;
            }
        }

        // Found a pull effect
        if (pullAbility != null && pullTarget != null)
        {
            // We're close enough so let's heal up.

            TurnData td = new TurnData();
            td.tAbilityToTry = pullAbility;
            td.centerPosition = GetPos();
            td.target.Add(pullTarget);

            float waitTime = UseAbility(td);
            MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
            //monTurn.affectedActors = affectedActors;
            monTurn.affectedActors = td.affectedActors;
            monTurn.results = td.results;
            if (waitTime > 0.0f)
            {
                // Process the effects visually
                CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                return monTurn;
            }
            else
            {
                // No wait time
                CombatManagerScript.cmsInstance.ClearQueuedEffects();
                return monTurn;
            }
        }

        return myMonsterTurnData.Continue();
    }

    List<Fighter> healTargets;

    MonsterTurnData CheckForAndUseSupportAbility()
    {
        // Healing is a priority regardless of state, right?
        AbilityScript considerHealing = null;
        Fighter healTarget = null;

        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript abil = FetchLocalAbility(considerAbilities[i].abilityRef);
            if (abil.targetForMonster == AbilityTarget.SELF || abil.targetForMonster == AbilityTarget.ALLY || abil.targetForMonster == AbilityTarget.ALLY_ONLY || abil.targetForMonster == AbilityTarget.PET_ALLY)
            {
                if (healTargets == null) healTargets = new List<Fighter>();
                healTargets.Clear();     

                if (abil.targetForMonster == AbilityTarget.PET_ALLY)
                {
                    
                    if (ReadActorData("any_summoned_creatures") <= 0)
                    {
                        break;
                    }
                    foreach (Actor act in summonedActors)
                    {
                        if (act.GetActorType() != ActorTypes.MONSTER) continue;
                        Monster m = act as Monster;
                        if (!m.myStats.IsAlive()) continue;
                        healTargets.Add(m);
                    }                    
                }
                else
                {
                    if (abil.targetForMonster != AbilityTarget.ALLY_ONLY)
                    {
                        healTargets.Add(this);
                    }
                    Fighter tFight = myTarget as Fighter;
                    if (myTarget != null)
                    {
                        if (tFight.combatTargets != null)
                        {
                            foreach (AggroData ad in tFight.combatTargets)
                            {
                                if (ad == null || ad.combatant == null) continue;
                                if (ad.combatant == this && abil.targetForMonster != AbilityTarget.SELF) continue;
                                if (ad.combatant != this && abil.targetForMonster == AbilityTarget.SELF) continue;
                                // New conditional: HP healing abilities will NOT be used on player if they are flagged as HEALHP, due to balance concerns.
                                if (actorfaction == Faction.PLAYER && abil.abilityFlags[(int)AbilityFlags.HEALHP] && ad.combatant == GameMasterScript.heroPCActor) continue;
                                if (ad.combatant.actorfaction == actorfaction && ad.combatant.dungeonFloor == dungeonFloor && !ad.combatant.destroyed)
                                {
                                    healTargets.Add(ad.combatant);
                                }
                            }
                        }
                    }
                }

                // This is some sort of beneficial ability.
                foreach (EffectScript eff in abil.listEffectScripts)
                {
                    if (eff.effectType == EffectType.CHANGESTAT)
                    {
                        ChangeStatEffect cse = eff as ChangeStatEffect;
                        if (cse.stat == StatTypes.HEALTH)
                        {
                            // It's a usable healing ability.
                            // For now, use only on self. But need to evaluate allies here too.

                            float lowest = myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
                            healTarget = this;

                            if (abil.targetForMonster == AbilityTarget.PET_ALLY)
                            {
                                healTarget = healTargets.GetRandomElement();
                            }

                            for (int a = 0; a < healTargets.Count; a++)
                            {
                                Fighter ft = healTargets[a];
                                float percentHP = ft.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
                                if (percentHP < lowest)
                                {
                                    lowest = percentHP;
                                    healTarget = ft;
                                }
                            }

                            if ((actorfaction != Faction.PLAYER && lowest <= (CheckAttribute(MonsterAttributes.HEALER) / 100f)) 
                                || actorfaction == Faction.PLAYER && lowest <= 0.8f)
                            {
                                considerHealing = abil;
                                break;
                            }
                        }
                    }
                    if ((eff.effectType == EffectType.SPECIAL || eff.effectType == EffectType.ADDSTATUS || eff.effectType == EffectType.MOVEACTOR) && !abil.abilityFlags[(int)AbilityFlags.HEALHP])
                    {
                        if (healTargets.Count > 0)
                        {
                            considerHealing = abil;

                            if (healTargets.Contains(GameMasterScript.heroPCActor))
                            {
                                healTarget = GameMasterScript.heroPCActor;
                            }
                            else
                            {
                                healTarget = healTargets[UnityEngine.Random.Range(0, healTargets.Count)];                                
                            }

                            //Debug.Log("Target is: " + healTarget.actorRefName + " " + healTarget.displayName + " " + abil.targetForMonster);

                            break;
                        }
                    }
                }
                
            }
            if (considerHealing != null)
            {
                break;
            }
        }

        // Found a healing spell to use, now to use it.
        if (considerHealing != null)
        {
            float distToHealTarget = MapMasterScript.GetGridDistance(GetPos(), healTarget.GetPos());
            // Are we too far from our healing target? Continue our turn. Don't bother trying to pathfind.
            // #todo: Need to account for AOE stuff here. use evaluate tiles etc.
            if (distToHealTarget > considerHealing.range) 
            {
                return myMonsterTurnData.Continue(); // Don't try to move toward heal target, just continue with AI behavior path.

                /* MonsterMoveData mmd = GetMoveTile(GetPos(), healTarget.GetPos());
                if (mmd == null)
                {
                    // Can't move...
                    return myMonsterTurnData.Pass();
                }
                else
                {
                    // Standard move.
                    return AttemptMovement(mmd.destinationTile.pos);
                } */
            }

            // We're close enough so let's heal up.

            // TODO: Account for AOE heals...

            TurnData buffTurn = EvaluateAbilityTiles(considerHealing, Directions.NEUTRAL, healTarget);

            if (buffTurn.tAbilityToTry.chargeTurns > 0)
            {
                return PrepareChargeTurn(buffTurn);
            }

            float waitTime = UseAbility(buffTurn);
            MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
            monTurn.affectedActors = buffTurn.affectedActors;
            monTurn.results = buffTurn.results;
            if (waitTime > 0.0f)
            {
                // Process the effects visually
                CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                return monTurn;
            }
            else
            {
                // No wait time
                CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                CombatManagerScript.cmsInstance.ClearQueuedEffects();
                return monTurn;
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForSeekItemBehavior()
    {
        if (myBehaviorState == BehaviorState.SEEKINGITEM)
        {
            if (myTargetTile == Vector2.zero)
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Found Item");
#endif
                SetState(BehaviorState.NEUTRAL);
            }
            else
            {
                MapTileData tt = MapMasterScript.GetTile(myTargetTile);
                if (!tt.AreItemsInTile())
                {
                    myTargetTile = Vector2.zero;
#if UNITY_EDITOR
                    Debug_AddMessage("->NEUTRAL: Found Item");
#endif
                    SetState(BehaviorState.NEUTRAL);

                }
                else
                {
                    MapTileData curTile = MapMasterScript.GetTile(GetPos());
                    if (tt == curTile)
                    {
                        TileInteractions.LootAllItemsInTile(this, curTile);
#if UNITY_EDITOR
                        Debug_AddMessage("->NEUTRAL: Found Item");
#endif
                        SetState(BehaviorState.NEUTRAL);
                        myTargetTile = Vector2.zero;

                        MapMasterScript.activeMap.CheckForNearbyGreedyMonsterAggro(this, GetPos());

                        return myMonsterTurnData.Pass();

                    }
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForGreedyFightBehavior()
    {
        // In the middle of combat, greedy monsters may try to pick up an item.
        if (myBehaviorState == BehaviorState.FIGHT && CheckAttribute(MonsterAttributes.GREEDY) > 0)
        {
            MapTileData curTile = MapMasterScript.GetTile(GetPos());
            if (curTile.anyItemsInTile && UnityEngine.Random.Range(0, 100) <= CheckAttribute(MonsterAttributes.GREEDY))
            {
                TileInteractions.LootAllItemsInTile(this, curTile);
                MapMasterScript.activeMap.CheckForNearbyGreedyMonsterAggro(this, GetPos());
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("monster_fascinated", this);
                return myMonsterTurnData.Pass();
            }
        }
        return myMonsterTurnData.Continue();
    }

    void UpdateMaxMoveAndAttackRanges()
    {
        // Check max movement range now.
        int maxMoveRange = moveRange;
        int maxAttackRange = myEquipment.GetWeapon().range;

        foreach (MonsterPowerData mpd in considerAbilities)
        {
            AbilityScript abil = FetchLocalAbility(mpd.abilityRef);
            if (abil.CanActorUseAbility(this, CanIgnoreCosts(abil)))
            {
                // Do any powers have a longer attack range?
                if (abil.targetForMonster == AbilityTarget.ENEMY)
                {
                    // We can possibly use it
                    // Use base range for now, but eventually need to calculate the max possible range via ground AND cursor target, plus line of sight.
                    if (abil.range > maxAttackRange)
                    {
                        maxAttackRange = abil.range;
                    }
                }

                // Do any powers have a longer MOVE range?
                if (abil.targetForMonster == AbilityTarget.GROUND)
                {
                    // Is this a moveSelf ability?
                    foreach (EffectScript eff in abil.listEffectScripts)
                    {
                        if (eff.effectType == EffectType.MOVEACTOR)
                        {
                            MoveActorEffect mae = eff as MoveActorEffect;
                            if (mae.tActorType == TargetActorType.SELF || mae.tActorType == TargetActorType.ORIGINATING)
                            {
                                if (abil.range > maxMoveRange)
                                {
                                    maxMoveRange = abil.range;
                                }
                            }
                        }
                    }
                }

            }
        }
        cachedBattleData.maxAttackRange = maxAttackRange;
        cachedBattleData.maxMoveRange = maxMoveRange;

        if ((actorRefName == "mon_nightmareking" && ItemDreamFunctions.IsNightmareKingInvincible()) ||
            ReadActorData("mystery_king_chaser") == 1)
        {
            cachedBattleData.maxAttackRange = 1f;
            cachedBattleData.weaponAttackRange = 1;
        }
    }

    MonsterTurnData CheckForBerserkerBehavior()
    {
        if (myBehaviorState == BehaviorState.FIGHT && CheckAttribute(MonsterAttributes.BERSERKER) > 0 && actorfaction != Faction.PLAYER)
        {
            float berserkValue = (myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * CheckAttribute(MonsterAttributes.BERSERKER)) / 100f;
            if (myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) < berserkValue)
            {
                // Below a certain % of health, GO NUTS
                if (!berserking)
                {
                    StringManager.SetTag(0, displayName);
                    GameLogScript.LogWriteStringRef("rage_log", this);
                    BattleTextManager.NewText(StringManager.GetString("rage_bt"), GetObject(), Color.red, 0f);
                    berserking = true;
                }

                List<MapTileData> nearbyTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GetPos(), 1);
                List<Actor> possibleTargets = new List<Actor>();

                for (int i = 0; i < nearbyTiles.Count; i++)
                {
                    MapTileData mtd = nearbyTiles[i];
                    for (int x = 0; x < mtd.GetAllActors().Count; x++)
                    {
                        Actor act = mtd.GetAllActors()[x];
                        if ((act.GetActorType() == ActorTypes.MONSTER) || (act.GetActorType() == ActorTypes.HERO))
                        {
                            possibleTargets.Add(act);
                        }
                    }
                }

                possibleTargets.Remove(this);
                // Add nearby targets
                foreach (Actor act in possibleTargets)
                {
                    if (!CheckTarget(act))
                    {
                        AddTarget(act);
                    }
                }

                // Pick one at random that is within striking range. Probably do an aggro thing
                SetMyTarget(GetTargetWithinRangeAtRandom());
            }
        }
        return myMonsterTurnData.Continue();
    }

    void PruneCombatTargets(bool removeAll = false)
    {
        if (removeAll)
        {
            combatTargets.Clear();
            return;
        }

        // Evaluate targets and interest in those targets
        aggroToRemove.Clear();

        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            AggroData ad = combatTargets[i];
            if (ad.turnsSinceCombatAction >= turnsToLoseInterest)
            {
                // Haven't fought them for awhile, so get rid of them.
                aggroToRemove.Add(ad);
                ad.combatant.RemoveTarget(this);
            }
        }

        for (int i = 0; i < aggroToRemove.Count; i++)
        {
            combatTargets.Remove(aggroToRemove[i]);
        }
    }

    void RefreshAndPruneCombatTargets()
    {
        if (myBehaviorState == BehaviorState.FIGHT && !berserking)
        {
            PruneCombatTargets();

            if (GetNumCombatTargets() == 0)
            {
                ClearMyTarget();
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: No Target");
#endif
                SetState(BehaviorState.NEUTRAL);
            }
            else
            {
                SetMyTarget(EvaluateTargetsByRangeAndAggro());
                //Debug.Log("Found a target: " + myTarget.actorRefName + " " + myBehaviorState);
            }

        }
    }

    void CheckForTimidState()
    {
        // Are we timid? Worth running away?
        if (CheckAttribute(MonsterAttributes.TIMID) > 0)
        {
            if (myBehaviorState == BehaviorState.FIGHT)
            {
                // At low health, chance of running away.

                float timidValue = (myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * CheckAttribute(MonsterAttributes.TIMID)) / 100f;
                if (myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) <= timidValue)
                {
                    if (UnityEngine.Random.Range(0, 50) <= CheckAttribute(MonsterAttributes.TIMID)) // Consider tweaking this
                    {
                        if (!runningAway)
                        {
                            StringManager.SetTag(0, displayName);
                            GameLogScript.LogWriteStringRef("fleeing_log", this);
                            BattleTextManager.NewText(StringManager.GetString("fleeing_bt"), GetObject(), Color.yellow, 0f);
                            runningAway = true;
                        }
#if UNITY_EDITOR
                        Debug_AddMessage("->RUN: Timid!");
#endif
                        SetState(BehaviorState.RUN);
                    }
                }
            }
        } // End timid check
    }

    MonsterTurnData CheckForRunningAwayBehavior()
    {
        // Running away from MyTarget
        if (myBehaviorState == BehaviorState.RUN)
        {
            float distanceToRun = 10f; // Try to get 10 squares away before stopping.
            Vector2 runFromPos = new Vector2(0, 0);
            Actor targ;
            if (myTarget == null)
            {
                if (lastActorAttackedBy != null)
                {
                    runFromPos = lastActorAttackedBy.GetPos();
                    targ = lastActorAttackedBy;
                }
                else
                {
                    runFromPos = GameMasterScript.heroPCActor.GetPos();
                    targ = GameMasterScript.heroPCActor;
                }

            }
            else
            {
                runFromPos = myTarget.GetPos();
                targ = myTarget;
            }
            float distanceFromTarget = Vector2.Distance(GetPos(), runFromPos);

            if (distanceFromTarget >= distanceToRun)
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Got Away");
#endif
                SetState(BehaviorState.NEUTRAL);
                // Successfully ran away.
            }
            else
            {
                // Gotta try to run.
                MapTileData bestRunTile = null;
                float longestDistance = distanceFromTarget;

                for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                {
                    Vector2 newPos = GetPos() + MapMasterScript.xDirections[i];
                    if (newPos.x > 0 && newPos.y > 0 && newPos.x < MapMasterScript.GetColumns() - 1 && newPos.y < MapMasterScript.GetRows() - 1)
                    {
                        // Within render bounds
                        MapTileData mtd = MapMasterScript.GetTile(newPos);
                        if (!mtd.IsCollidable(this) && !IsTileDangerous(mtd))
                        {
                            float tDistance = Vector2.Distance(mtd.pos, runFromPos);
                            if (tDistance > longestDistance)
                            {
                                bestRunTile = mtd;
                                longestDistance = tDistance;
                            }
                        }
                    }
                }

                if (bestRunTile == null && distanceFromTarget <= 3f && UnityEngine.Random.Range(0, 0.99f) > CheckAttribute(MonsterAttributes.PACIFIST)) // 6f is hardcoded distance.
                {
                    // Can't get any further away, so try to fight.
                    StringManager.SetTag(0, displayName);
                    GameLogScript.LogWriteStringRef("panicked_log", this);
                    BattleTextManager.NewText(StringManager.GetString("panicked_bt"), GetObject(), Color.yellow, 0f);
#if UNITY_EDITOR
                    Debug_AddMessage("->FIGHT: Panic!");
#endif
                    SetState(BehaviorState.FIGHT);
                    runningAway = false;
                }
                else if (bestRunTile != null)
                {
                    // TODO - Use abilities to run.
                    return AttemptMovement(bestRunTile.pos);
                }
                else if (bestRunTile == null && distanceFromTarget > 3f)
                {
                    //GameLogScript.GameLogWrite(displayName + " hopes " + targ + " will go away.");
                    return myMonsterTurnData.Pass();
                }
            }
        } // End run behavior

        if (myBehaviorState == BehaviorState.NEUTRAL)
        {
            if (actorfaction != Faction.PLAYER && myStats.GetLevel() < GameMasterScript.heroPCActor.myStats.GetLevel() && GetXPModToPlayer() == 0.0f &&
                !GetActorMap().IsJobTrialFloor())
            {
                if (MapMasterScript.GetGridDistance(GetPos(), GameMasterScript.heroPCActor.GetPos()) <= 3)
                {
                    // Don't get near the hero lol
                    if (cachedBattleData.maxMoveRange > 0)
                    {
#if UNITY_EDITOR
                        Debug_AddMessage("->RUN: Hero Level");
#endif
                        SetState(BehaviorState.RUN);
                    }
                }
            }

        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData MoveToAnchorIfNecessary()
    {
        // Top priority - move toward anchor if needed.
        if (anchor != null && anchorRange > 0)
        {
            float anchorDistance = MapMasterScript.GetGridDistance(anchor.GetPos(), GetPos());
            if (anchorDistance > anchorRange)
            {
                if (anchorDistance > anchorRange + 1)
                {
                    //tick up our out of range counter
                    int turnsOutOfAnchorRange = Math.Max(0, ReadActorData("turnsoutofanchorrange") + 1);

                    //if we've been away too long, we seek to return home.
                    if (turnsOutOfAnchorRange >= GameMasterScript.PET_MAXTURNS_OUT_OF_ANCHORRANGE && moveRange > 0)
                    {
                        turnsOutOfAnchorRange = 0;
                        SetActorData("turnsoutofanchorrange", turnsOutOfAnchorRange);
                        return ImmediateJumpToAnchorOrPosition();
                    }

                    //otherwise, just keep track of this number
                    SetActorData("turnsoutofanchorrange", turnsOutOfAnchorRange);
                }

                MonsterMoveData mmd = GetMoveTile(GetPos(), anchor.GetPos());
                if (mmd == null)
                {
                    // Can't move...
                    return myMonsterTurnData.Pass();
                }
                else
                {
                    if (mmd.abilityUsed == null)
                    {
                        return AttemptMovement(mmd.destinationTile.pos);
                    }
                    else
                    {
                        return ExecuteMovementAbility(mmd);
                    }
                }
            }
            else
            {

                SetActorData("turnsoutofanchorrange", 0);

                Vector2 newPos = FindRandomPos(false); // Basic wandering.

                List<MapTileData> pTiles = MapMasterScript.GetNonCollidableTilesAroundPoint(GetPos(), 1, this);
                List<Vector2> realPossible = new List<Vector2>(pTiles.Count);
                Vector2 move = Vector2.zero;
                for (int x = 0; x < pTiles.Count; x++)
                {
                    if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)pTiles[x].pos.x, (int)pTiles[x].pos.y])
                    {
                        continue;
                    }
                    if (MapMasterScript.GetGridDistance(anchor.GetPos(), pTiles[x].pos) > anchorRange)
                    {
                        continue;
                    }
                    if (IsTileDangerous(pTiles[x]))
                    {
                        continue;
                    }
                    realPossible.Add(pTiles[x].pos);
                }

                if (realPossible.Count == 0)
                {
                    //Debug.Log(GetPos() + " passed 2");
                    return myMonsterTurnData.Pass();
                }
                else
                {
                    return AttemptMovement(realPossible[UnityEngine.Random.Range(0, realPossible.Count)]);
                }

            }
        }

        return myMonsterTurnData.Continue();
    }

    void CheckIfActorOfInterestIsAlive()
    {
        if (myBehaviorState == BehaviorState.CURIOUS && myActorOfInterest != null)
        {
            if (myActorOfInterest.GetActorType() == ActorTypes.MONSTER || myActorOfInterest.GetActorType() == ActorTypes.HERO)
            {
                // Is my actor of interest still alive and in combat?
                Fighter ft = myActorOfInterest as Fighter;
                if (!ft.IsFighter() || !ft.myStats.IsAlive() || ft.TurnsSinceLastCombatAction > 3)
                {
                    myActorOfInterest = null;
                    myTargetTile = Vector2.zero;
#if UNITY_EDITOR
                    Debug_AddMessage("->NEUTRAL: Not Curious");
#endif
                    SetState(BehaviorState.NEUTRAL);
                }
            }
        }
    }

    void CheckForNoiseAttraction()
    {
        if (myBehaviorState == BehaviorState.NEUTRAL && CheckAttribute(MonsterAttributes.LOVESBATTLES) > 0)
        {
            // We didn't aggro yet, but maybe we like battle...
            // Run through all actors and check distance vs. our calculated "hearing distance" to see if we heard them.
            float maxHearingDistance = CheckAttribute(MonsterAttributes.LOVESBATTLES) / 8f;
            float shortestDistanceTobattle = 99f;
            Actor actorOfInterest = null;

            // Check hero first to see if we can hear them.
            float calcDistance = 0f;
            if (GameMasterScript.heroPCActor.TurnsSinceLastCombatAction <= 2)
            {
                calcDistance = Vector2.Distance(GetPos(), GameMasterScript.heroPCActor.GetPos());
                if (calcDistance < shortestDistanceTobattle && calcDistance <= maxHearingDistance)
                {
                    actorOfInterest = GameMasterScript.heroPCActor;
                    shortestDistanceTobattle = calcDistance;
                }
            }

            // Then check other monsters. No need to iterate through every actor, just monsters.
            foreach(Monster mn in MapMasterScript.activeMap.monstersInMap)
            {
                if (mn.actorfaction == Faction.PLAYER && mn.actorUniqueID != actorUniqueID)
                {
                    if (mn.TurnsSinceLastCombatAction <= 2)
                    {
                        calcDistance = Vector2.Distance(GetPos(), mn.GetPos());
                        if (calcDistance < shortestDistanceTobattle && calcDistance <= maxHearingDistance)
                        {
                            actorOfInterest = mn;
                            shortestDistanceTobattle = calcDistance;
                        }
                    }
                }
            }

            if (actorOfInterest != null)
            {
                if (DisplayMonAttributeMessage())
                {
                    StringManager.SetTag(0, displayName);
                    GameLogScript.LogWriteStringRef("hears_battle_log", this);
                    BattleTextManager.NewText("!!!!", GetObject(), Color.yellow, 0f);
                }
#if UNITY_EDITOR
                Debug_AddMessage("->CURIOUS: Heard battle");
#endif
                //Debug.Log(actorRefName + " " + actorUniqueID + " " + GetPos() + " heard battle from " + actorOfInterest.actorRefName + " " + actorOfInterest.actorUniqueID);
                SetState(BehaviorState.CURIOUS);
                myActorOfInterest = actorOfInterest;
                myTargetTile = actorOfInterest.GetPos();
                tilePath.Clear();
            }
        }
    }

    void CheckForPlayerToStalk(bool CanSeeHero)
    {
        if (myBehaviorState == BehaviorState.NEUTRAL && CheckAttribute(MonsterAttributes.STALKER) > 0 && CanSeeHero)
        {
            // If STALKER can see the hero, start tracking the hero and waiting for the time to strike.
            if (UnityEngine.Random.Range(0, 100) <= CheckAttribute(MonsterAttributes.STALKER))
            {
                if (DisplayMonAttributeMessage())
                {
                    StringManager.SetTag(0, displayName);
                    GameLogScript.LogWriteStringRef("stalking_log", this);
                }
#if UNITY_EDITOR
                Debug_AddMessage("->STALKING: Stalking Hero");
#endif
                SetState(BehaviorState.STALKING);
                SetMyTarget(GameMasterScript.GetHeroActor());
            }
        }
    }

    void UpdateTargetTileForRonin(bool noWander)
    {
        if (CheckAttribute(MonsterAttributes.RONIN) > 0 && UnityEngine.Random.Range(0, 101) <= CheckAttribute(MonsterAttributes.RONIN) && !noWander)
        {
            // Ronin wander with no rooms available. Pick a direction and walk in that direction.
            List<Directions> dirToCheck = new List<Directions>(8);
            dirToCheck.Add(Directions.NORTH);
            dirToCheck.Add(Directions.NORTHEAST);
            dirToCheck.Add(Directions.EAST);
            dirToCheck.Add(Directions.SOUTHEAST);
            dirToCheck.Add(Directions.SOUTH);
            dirToCheck.Add(Directions.SOUTHWEST);
            dirToCheck.Add(Directions.WEST);
            dirToCheck.Add(Directions.NORTHWEST);

            Vector2 checkPos = GetPos();
            MapTileData chk = null;
            Vector2 usablePos = Vector2.zero;
            Directions selected;
            while (dirToCheck.Count > 0)
            {
                selected = dirToCheck[UnityEngine.Random.Range(0,dirToCheck.Count)];
                checkPos = GetPos() + MapMasterScript.xDirections[(int)selected];
                chk = MapMasterScript.GetTile(checkPos);
                if (chk == null)
                {
                    dirToCheck.Remove(selected);
                    continue;
                }
                else
                {
                    int steps = 0;
                    while (!chk.IsCollidable(this) && !IsTileDangerous(chk) && steps < 6 && InMyLocalBounds(chk.pos))
                    {
                        usablePos = checkPos;
                        checkPos += MapMasterScript.xDirections[(int)selected];
                        chk = MapMasterScript.GetTile(checkPos);
                        steps++;
                        if (chk == null)
                        {
                            break;
                        }

                    }

                    if (steps >= 3)
                    {
                        myTargetTile = usablePos;
                        tilePath.Clear();
                        break;
                    }
                    dirToCheck.Remove(selected);
                }
            }
        }
    }

    // Returns whether or not we can see the hero
    bool TryToAggroHero()
    {
        HeroPC hero = GameMasterScript.GetHeroActor() as HeroPC;

        bool CanSeeHero = false;

        if (actorfaction == Faction.ENEMY)
        {
            float distanceFromHero = Vector2.Distance(hero.GetPos(), GetPos());
            if (distanceFromHero <= myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.CUR))
            {
                if (hero.CanSeeActor(this))
                {
                    CanSeeHero = true;
                    if (foodLovingMonster && UnityEngine.Random.Range(0, 1f) <= 0.4f)
                    {
                        BattleTextManager.NewText(StringManager.GetString("mon_hungry_beg"), GetObject(), Color.green, 0.5f);
                    }
                }
                else
                {
                    // Change on October 7th: Hero can't see me, so I should never be able to see hero.
                    CanSeeHero = false;
                }
            }
            if (!CanSeeHero)
            {
                CanSeeHero = CheckLOSToHeroPets();
            }

            bool heroWithinAggroRange = CheckHeroWithinAggroRange(true);
            if (!heroWithinAggroRange)
            {
                heroVisibilityThisTurn = HeroVisibilityToMonster.NOTVISIBLE;
            }
            else
            {
                heroVisibilityThisTurn = HeroVisibilityToMonster.VISIBLE;
            }

            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("spiritwalk"))
            {
                CanSeeHero = false;
            }

            if (!CanSeeHero)
            {
                // Well, nothing to do here.
            }
        }

        return CanSeeHero;
    }

    MonsterTurnData CheckForAndTryWanderBehavior(bool CanSeeHero, bool forceWander = false)
    {
        if (forceWander || (myBehaviorState == BehaviorState.NEUTRAL && (myMoveBehavior == MoveBehavior.WANDER || !CanSeeHero) && cachedBattleData.maxMoveRange > 0)) 
        {
            if (CheckAttribute(MonsterAttributes.GREEDY) > 0)
            {
                // No target. Is there an item around that we can loot? 

                int maxRange = (int)myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.CUR);
                if (GetActorMap().IsItemWorld())
                {
                    maxRange = 9; // Why was this 3 before...?
                }

                if (maxRange > 9) maxRange = 9;
                possibleTiles.Clear();

                // Remove tiles that can't hold anything.                    

                CustomAlgorithms.GetTilesAroundPoint(GetPos(), maxRange, MapMasterScript.activeMap);

                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    MapTileData mtd = CustomAlgorithms.tileBuffer[i];
                    if (mtd.tileType != TileTypes.WALL)
                    {
                        if (IsTileDangerous(mtd)) continue;
                        if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.BLOCKER]) continue;
                        if (mtd.anyItemsInTile)
                        {
                            //if (MapMasterScript.CheckTileToTileLOS(GetPos(), mtd.pos, this, MapMasterScript.activeMap))
                            if (CustomAlgorithms.CheckBresenhamsLOS(GetPos(), mtd.pos, MapMasterScript.activeMap))
                            {
                                possibleTiles.Add(mtd);
                            }
                        }
                    }

                }

                if (possibleTiles.Count > 0)
                {
                    List<Item> nearbyItems = new List<Item>();
                    MapTileData bestOption = possibleTiles[0];
                    float shortestDistance = 100f;
                    bool foundItem = false;

                    for (int i = 0; i < possibleTiles.Count; i++)
                    {
                        MapTileData mtd = possibleTiles[i];
                        if (mtd.anyItemsInTile)
                        {
                            foundItem = true;
                            float cDistance = Vector2.Distance(GetPos(), mtd.pos);
                            if (cDistance < shortestDistance)
                            {
                                bestOption = mtd;
                                shortestDistance = cDistance;
                            }
                        }

                    }

                    if (foundItem)
                    {
                        myTargetTile = bestOption.pos;
                        tilePath.Clear();
#if UNITY_EDITOR
                        Debug_AddMessage("->SEEKINGITEM: Found Item");
#endif

                        SetState(BehaviorState.SEEKINGITEM);
                        bool specialItem = false;
                        if (wantsItem != null)
                        {
                            foreach (Item itm in bestOption.GetItemsInTile())
                            {
                                if (itm.actorRefName == wantsItem.actorRefName)
                                {
                                    CombatManagerScript.SpawnChildSprite("AggroEffect", this, Directions.NORTHEAST, false);
                                    StringManager.SetTag(0, displayName);
                                    StringManager.SetTag(1, itm.displayName);

                                    GameLogScript.GameLogWrite(StringManager.GetString("monster_want_item_log"), this);
                                    BattleTextManager.NewText(StringManager.GetString("monster_want_item_bt"), GetObject(), Color.yellow, 0.0f);
                                    specialItem = true;
                                    break;
                                }
                            }
                        }
                        if (DisplayMonAttributeMessage() && !specialItem)
                        {
                            StringManager.SetTag(0, displayName);
                            GameLogScript.LogWriteStringRef("monster_fascinated", this);
                            BattleTextManager.NewText(StringManager.GetString("mon_want_popup"), GetObject(), Color.yellow, 0.0f);
                        }
                    }
                    else
                    {
                        myTargetTile = Vector2.zero;
                    }
                }
            }
            // End greedy check

            // No tile, so let's wander.

            bool noWander = false;

            if (myBehaviorState == BehaviorState.NEUTRAL && GetNumCombatTargets() == 0 && myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) == 1f)
            {
                if (!MapMasterScript.activeMap.exploredTiles[(int)GetPos().x, (int)GetPos().y])
                {
                    noWander = true;
                }
            }

            if (myTargetTile == Vector2.zero)
            {
                UpdateTargetTileForRonin(noWander);

                if (myTargetTile == Vector2.zero)
                {
                    // Wander direction: Only used for drunk right now.
                    if (UnityEngine.Random.Range(0, 1f) <= 0.15f)
                    {
                        wanderDirection = (Directions)UnityEngine.Random.Range(0, 8);
                    }
                    Vector2 newPos = Vector2.zero;
                    if (myTemplate.drunkWalkChance == 0f)
                    {
                        newPos = FindRandomPos(false); // Basic wandering.
                    }
                    else
                    {
                        newPos = DrunkFindRandomPos(); // Basic wandering.
                    }

                    if (Vector2.Equals(newPos, new Vector2(0, 0)))
                    {
                        return myMonsterTurnData.Pass();
                    }
                    else
                    {
                        if (PlatformVariables.OPTIMIZE_MONSTER_BEHAVIOR)
                        {
                            // Another optimization - why act if we're really far away from the hero?
                            if (aggro_CheckDistanceFromHero >= 10f && UnityEngine.Random.Range(0, 1f) <= 0.5f)
                            {
                                return myMonsterTurnData.Pass();
                            }
                        }
                        return AttemptMovement(newPos);
                    }
                }
            }

        }
        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndTryStalkingBehavior()
    {
        if (myBehaviorState == BehaviorState.STALKING)
        {
            float distanceFromHero = Vector2.Distance(GetPos(), GameMasterScript.GetHeroActor().GetPos());
            Fighter ft = myTarget as Fighter;
            if (ft == null || !ft.myStats.IsAlive())
            {
                //Debug.Log(displayName + " hunting " + myTarget.displayName + " is dead");
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: Target Died");
#endif

                SetState(BehaviorState.NEUTRAL);
                ClearMyTarget();
            }
            else
            {
                bool heroVis = false;
                if (heroVisibilityThisTurn == HeroVisibilityToMonster.NOTCHECKED)
                {
                    heroVis = CheckHeroWithinAggroRange(true); 
                }
                else if (heroVisibilityThisTurn == HeroVisibilityToMonster.NOTVISIBLE)
                {
                    return myMonsterTurnData.Continue();
                }

                if (!heroVis)
                {
                    return myMonsterTurnData.Continue();
                }

                // Do we need to move toward target?
                if (distanceFromHero > stalkerRange)
                {
                    // Beyond this distance, try to get closer to target.
                    MonsterMoveData mmd = GetMoveTile(GetPos(), myTarget.GetPos());
                    if (mmd == null)
                    {
                        // Can't move...
                        return myMonsterTurnData.Pass();
                    }
                    else
                    {
                        // Move tile isn't null? Move closer.
                        if (mmd.abilityUsed == null)
                        {
                            // Standard move.
                            return AttemptMovement(mmd.destinationTile.pos);
                        }
                        else
                        {
                            // Move via ability
                            return ExecuteMovementAbility(mmd);
                        }

                    }
                }
                else if (distanceFromHero == stalkerRange)
                {
                    // We're the right distance from our target... So do nothing, IF we're patient enough.
                    if (UnityEngine.Random.Range(0, 100) <= CheckAttribute(MonsterAttributes.STALKER))
                    {
                        return myMonsterTurnData.Pass();
                    }
                    else
                    {
                        Vector2 newPos = FindRandomPos(false);
                        if (newPos != emptyVector)
                        {
                            return AttemptMovement(newPos);
                        }
                    }

                }
                else if (distanceFromHero < stalkerRange)
                {
                    // We're too close, back up a bit.
                    MapTileData bestRunTile = null;
                    float longestDistance = distanceFromHero;

                    for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                    {
                        Vector2 newPos = GetPos() + MapMasterScript.xDirections[i];
                        if ((newPos.x > 0) && (newPos.y > 0) && (newPos.x < MapMasterScript.GetColumns() - 1) && (newPos.y < MapMasterScript.GetRows() - 1))
                        {
                            // Within render bounds
                            MapTileData mtd = MapMasterScript.GetTile(newPos);
                            if ((!mtd.IsUnbreakableCollidable(this)) && (!IsTileDangerous(mtd)))
                            {
                                float tDistance = Vector2.Distance(mtd.pos, GameMasterScript.GetHeroActor().GetPos());
                                if (tDistance > longestDistance)
                                {
                                    bestRunTile = mtd;
                                    longestDistance = tDistance;
                                }
                            }
                        }
                    }

                    if (bestRunTile != null)
                    {
                        // Found a tile to move to, so will move there.
                        return AttemptMovement(bestRunTile.pos);
                    }
                    else
                    {
                        // No good tile, do nothing.
                        return myMonsterTurnData.Pass();
                    }

                }
            }
        } // End stalking state

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndTrySeekBehavior()
    {
        if (myTargetTile != Vector2.zero && (myBehaviorState == BehaviorState.SEEKINGITEM || myBehaviorState == BehaviorState.CURIOUS || myBehaviorState == BehaviorState.NEUTRAL))
        {
            // We have a destination, for some reason. Maybe to investigate noise or maybe to get an item.
            MonsterMoveData mmd = GetMoveTile(GetPos(), myTargetTile);
            if (mmd != null)
            {
                if (mmd.abilityUsed == null)
                {
                    return AttemptMovement(mmd.destinationTile.pos);
                }
                else
                {
                    return ExecuteMovementAbility(mmd);
                }
            }
            else
            {
                // MMD is null

                MapTileData tt = MapMasterScript.GetTile(myTargetTile);
                if (myBehaviorState == BehaviorState.SEEKINGITEM && tt.GetAllTargetable().Count > 0)
                {
                    // Greedy and might want to hit the thing on the tile.
                    if (MapMasterScript.GetGridDistance(GetPos(), myTargetTile) <= cachedBattleData.maxAttackRange && UnityEngine.Random.Range(0, 100) <= CheckAttribute(MonsterAttributes.GREEDY))
                    {
                        // Get angry that something is on the item tile, and consider attacking it.
                        if (tt.GetAllActors().Count > 0)
                        {
                            Fighter localTarget = null;
                            foreach (Actor act in tt.GetAllActors())
                            {
                                if ((act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO) && act != this)
                                {
                                    localTarget = act as Fighter;
                                    if (!localTarget.myStats.IsAlive())
                                    {
                                        localTarget = null;
                                    }
                                }
                            }
                            if (localTarget != null)
                            {
                                MonsterTurnData mtd = new MonsterTurnData(GameMasterScript.baseAttackAnimationTime, TurnTypes.ATTACK);
                                mtd.affectedActors.Add(localTarget);
                                StringManager.SetTag(0, displayName);
                                StringManager.SetTag(1, localTarget.displayName);
                                GameLogScript.LogWriteStringRef("angry_cause_blocked_log", this);
                                CombatResultPayload crp = CombatManagerScript.Attack(this, localTarget);
                                mtd.results.Add(crp.result);
                                mtd.waitTime += crp.waitTime;
                                return mtd;
                            }

                        }
                    }
                }

                // Can't move, so just wait.
                return myMonsterTurnData.Pass();
            }

        } // Seeking item or curious

        return myMonsterTurnData.Continue();
    }

    void CheckMyCombatTargetIsValid()
    {
        if (myTarget != null && myTarget.IsFighter())
        {
            Fighter ft = myTarget as Fighter;
            if (!ft.myStats.IsAlive() || ft.dungeonFloor != dungeonFloor)
            {
                ClearMyTarget();
            }
        }

        // Null target for some reason. Try finding a new one.
        if (myBehaviorState == BehaviorState.FIGHT && myTarget == null)
        {
            SetMyTarget(EvaluateTargetsByRangeAndAggro());
            if (myTarget == null)
            {
#if UNITY_EDITOR
                Debug_AddMessage("->NEUTRAL: No Target");
#endif

                SetState(BehaviorState.NEUTRAL);
            }
            else
            {
                //Debug.Log(actorRefName + " has found a new target: " + myTarget.actorRefName);
            }
        }
    }

    MonsterTurnData CheckForAndUseBuffAbility()
    {
        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript abil = FetchLocalAbility(considerAbilities[i].abilityRef);
            if (abil.CanActorUseAbility(this, CanIgnoreCosts(abil)))
            {
                if (abil.targetForMonster == AbilityTarget.SELF)
                {
                    // We can use this ability, and it's self targeted, so it's probably beneficial.

                    //foreach (EffectScript eff in abil.listEffectScripts)
                    for (int e = 0; e < abil.listEffectScripts.Count; e++)
                    {
                        EffectScript eff = abil.listEffectScripts[e];
                        if (eff.effectType == EffectType.SPECIAL)
                        {
                            TurnData td = new TurnData();
                            td.target.Add(this);
                            td.tAbilityToTry = abil;
                            td.centerPosition = GetPos();
                            td.targetPosition.Add(GetPos());
                            float waitTime = UseAbility(td);
                            MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                            monTurn.affectedActors.Add(this);
                            monTurn.results.Add(CombatResult.NOTHING);
                            if (waitTime > 0.0f)
                            {
                                CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                                return monTurn;
                            }
                            else
                            {
                                CombatManagerScript.cmsInstance.ClearQueuedEffects();
                                return monTurn;
                            }
                        }
                        if (eff.effectType == EffectType.ADDSTATUS)
                        {
                            AddStatusEffect ase = eff as AddStatusEffect;
                            string statusRef = ase.statusRef;
                            // Do we already have it?
                            if (!myStats.CheckHasStatusName(statusRef))
                            {
                                // No? OK, let's use it.
                                TurnData td = new TurnData();
                                td.target.Add(this);
                                td.tAbilityToTry = abil;
                                td.centerPosition = GetPos();
                                td.targetPosition.Add(GetPos());
                                float waitTime = UseAbility(td);
                                MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                                monTurn.affectedActors.Add(this);
                                monTurn.results.Add(CombatResult.NOTHING);
                                if (waitTime > 0.0f)
                                {
                                    CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                                    return monTurn;
                                }
                                else
                                {
                                    CombatManagerScript.cmsInstance.ClearQueuedEffects();
                                    return monTurn;
                                }
                            }
                        }
                    }
                }
            }
        }
        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndTrySniperBehavior(float attackRange, float distance, Vector2 targetPos)
    {
        // If monster is a SNIPER, try to put some distance between enemy and self.
        if (CheckAttribute(MonsterAttributes.SNIPER) > 0)
        {
            float preferredRangeF = (attackRange / 100f) * CheckAttribute(MonsterAttributes.SNIPER);
            preferredRangeF -= 1f;
            //Debug.Log(displayName + " " + attackRange + " " + preferredRangeF + " " + (preferredRangeF - distance));

            bool dontSnipe = false;

            if (turnsSinceLastDamaged <= 4)
            {
                preferredRangeF = 0f;
                dontSnipe = true;
            }
            Fighter ftTarget = myTarget as Fighter;
            if (ftTarget.myEquipment.IsWeaponRanged(ftTarget.myEquipment.GetWeapon()))
            {
                preferredRangeF = 0f;
                dontSnipe = true;
            }

            if (preferredRangeF > distance && !dontSnipe)
            {
                // Move away one tile at a time. But maybe pathfinding to cover would be better.

                List<MapTileData> nearbyTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(GetPos(), 1); // Check nearby tiles within movement range.
                List<MapTileData> possibleTiles = new List<MapTileData>();

                float realDistance = Vector2.Distance(GetPos(), targetPos);
                MapTileData bestTileToMove = null;
                float longestDistance = realDistance;

                // Remove tiles that can't hold anything.                    

                for (int i = 0; i < nearbyTiles.Count; i++)
                {
                    MapTileData mtd = nearbyTiles[i];
                    if ((!mtd.IsUnbreakableCollidable(this)) && (!IsTileDangerous(mtd)))
                    {
                        // Is the tile further from the player?
                        float checkDistance = Vector2.Distance(mtd.pos, targetPos);
                        if (checkDistance > longestDistance)
                        {
                            longestDistance = checkDistance;
                            bestTileToMove = mtd;
                        }
                    }
                }

                if (bestTileToMove != null)
                {
                    //Debug.Log("Repositioning self");
                    if (UnityEngine.Random.Range(0, 1.0f) < influenceTurnData.rootChance)
                    {
                        StringManager.SetTag(0, displayName);
                        GameLogScript.LogWriteStringRef("log_monster_cantmove", this);
                        return myMonsterTurnData.Pass();
                    }
                    MoveSelf(bestTileToMove.pos, true);
                    return myMonsterTurnData.Move();
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndUseSummonAbility()
    {
        // Summon Abilities? Use them!
        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript localAbil = FetchLocalAbility(considerAbilities[i].abilityRef);
            if (localAbil.targetForMonster == AbilityTarget.SUMMONGROUND || localAbil.targetForMonster == AbilityTarget.SUMMONHAZARD)
            {
                TurnData checkData = EvaluateAbilityTiles(localAbil, Directions.NEUTRAL, myTarget);
                float waitTime = UseAbility(checkData);

                MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                if (waitTime > 0.0f)
                {
                    // Process the effects visually
                    CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                }
                return monTurn;
            }
        }
        return myMonsterTurnData.Continue();
    }

    bool CheckIfHeroIsVisibleOrFindNewTarget(bool petMustStayNearPlayer)
    {
        bool canSeeHero = false;
        if (myTarget == GameMasterScript.GetHeroActor())
        {
            if (GameMasterScript.heroPCActor.CanSeeActor(this))
            {
                canSeeHero = true;
            }
            else
            {
                if (myTarget.GetActorType() != ActorTypes.HERO)
                {
                    canSeeHero = CustomAlgorithms.CheckBresenhamsLOS(GetPos(), myTarget.GetPos(), MapMasterScript.activeMap);
                }
                else
                {
                    canSeeHero = false;
                }

            }
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("spiritwalk"))
            {
                canSeeHero = false;
            }
        }
        else if (myTarget != null) // targeting something other than hero
        {
            if (myTarget.GetActorType() != ActorTypes.HERO)
            {
                canSeeHero = CustomAlgorithms.CheckBresenhamsLOS(GetPos(), myTarget.GetPos(), MapMasterScript.activeMap);
            }
            else
            {

            }
        }

        // Can't see our main target. Let's get another one that we can see
        if (!canSeeHero)
        {
            Actor newTarget = GetNearestVisibleTarget(petMustStayNearPlayer);
            if (newTarget != null)
            {
                SetMyTarget(newTarget);
            }
            else
            {
                //Debug.Log(actorRefName + " can't see anything to hit.");
            }
        }

        return canSeeHero;
    }

    MonsterTurnData CheckForAndTrySupporterBehavior(float distance)
    {
        // Supporter logic
        if (CheckAttribute(MonsterAttributes.SUPPORTER) > 0 && myTarget != null)
        {
            Fighter fightTarg = myTarget as Fighter;
            if (fightTarg.GetNumCombatTargets() > 1)
            {
                if (distance < CheckAttribute(MonsterAttributes.SUPPORTER))
                {
                    // Too close to target; move away.
                    List<MapTileData> nearbyTiles = MapMasterScript.GetNonCollidableTilesAroundPoint(GetPos(), cachedBattleData.maxMoveRange, this);
                    MapTileData best = null;
                    float longest = -1f;
                    foreach (MapTileData mtd in nearbyTiles)
                    {
                        float checkDistance = MapMasterScript.GetGridDistance(mtd.pos, myTarget.GetPos());
                        if (checkDistance > longest)
                        {
                            longest = checkDistance;
                            best = mtd;
                        }
                    }
                    if (best == null)
                    {
                        best = MapMasterScript.GetTile(GetPos());
                    }

                    MonsterMoveData supportMMD = GetMoveTile(GetPos(), best.pos);
                    if (supportMMD != null)
                    {
                        if (supportMMD.abilityUsed != null)
                        {
                            // Use the movement ability
                            TurnData td = new TurnData();
                            td.tAbilityToTry = supportMMD.abilityUsed;
                            List<Vector2> targetTiles = new List<Vector2>();
                            targetTiles.Add(supportMMD.destinationTile.pos);
                            td.targetPosition = targetTiles;
                            td.centerPosition = GetPos();
                            float waitTime = UseAbility(td);
                            MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                            monTurn.affectedActors = td.affectedActors;
                            monTurn.results = td.results;
                            if (waitTime > 0.0f)
                            {
                                // Process the effects visually
                                CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                                return monTurn;
                            }
                            else
                            {
                                // No wait time
                                CombatManagerScript.cmsInstance.ClearQueuedEffects();
                                return monTurn;
                            }

                        }
                        else
                        {
                            return AttemptMovement(supportMMD.destinationTile.pos);
                        }
                    }
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData CheckForAndExecuteAttackAction(float distance, float attackRange, bool canSeeHero, bool paralyzed)
    {
        // Target is within range, let's attack.
        if (distance <= attackRange && canSeeHero && CheckAttribute(MonsterAttributes.CANTATTACK) < 100 && myTarget != null && myTarget.IsFighter())
        {
            Fighter fightTarget = myTarget as Fighter;
            // Don't just attack. Use an ability
            List<MonsterPowerData> usableAggAbilities = GetUsableAggressiveAbilities();
            MonsterPowerData mpdTried = null;
            if (usableAggAbilities.Count > 0)
            {
                // Pick an ability. 
                AbilityScript abilToUse = null;

                float maxRange = cachedBattleData.weaponAttackRange;

                abilToUse = GetBestUsableOffenseAbility(usableAggAbilities, maxRange, out mpdTried);

                if (distance > maxRange)
                {
                    maxRange = 0;
                    // No damaging abilities or attacks are within range, but we must have an ability that can move the player.
                    foreach (MonsterPowerData mpd in usableAggAbilities)
                    {                        
                        AbilityScript abil = mpd.abilityRef;
                        // Use a damaging one if within range, otherwise, try to reposition the player.
                        foreach (EffectScript eff in abil.listEffectScripts)
                        {
                            if (eff.effectType == EffectType.MOVEACTOR)
                            {
                                MoveActorEffect mae = eff as MoveActorEffect;
                                if (mae.tActorType == TargetActorType.SINGLE && mae.pullActor) // Moves the target (such as the player) toward the monster.
                                {
                                    if (abil.range > maxRange)
                                    {
                                        // Need better way of calculating range properly via ground targets etc.
                                        abilToUse = abil;
                                        mpdTried = mpd;
                                        maxRange = abil.range;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (abilToUse != null && mpdTried != null)
                {
                    TurnData checkData = EvaluateAbilityTiles(abilToUse, Directions.NEUTRAL, myTarget);

                    // in NG++, make sure that the target cannot just walk out of this.
                    // #todo a way to do this more easily :D
                    /* if (checkData.canHeroSeeThisTurn && GameStartData.newGamePlus >= 2)
                    {
                        
                    } */

                    if (!checkData.canHeroSeeThisTurn)
                    {
                        // We couldn't use this ability this turn, so don't try it again while target is in same place
                        mpdTried.MarkAsFailed(GetPos(), myTarget);                        
                    }
                    else if (checkData.canHeroSeeThisTurn && VerifyTargetCanSeeMeIfTargetIsHero(checkData))
                    {
                        mpdTried.MarkAsSucceeded(myTarget);
                        if (checkData.tAbilityToTry.chargeTurns > 0)
                        {
                            return PrepareChargeTurn(checkData);
                        }

                        float waitTime = UseAbility(checkData);

                        MonsterTurnData monTurn = new MonsterTurnData(waitTime, TurnTypes.ABILITY);
                        monTurn.affectedActors = checkData.affectedActors;
                        monTurn.results = checkData.results;
                        if (waitTime > 0.0f)
                        {
                            // Process the effects visually
                            CombatManagerScript.cmsInstance.ProcessQueuedEffects();
                            return monTurn;
                        }
                        else
                        {
                            // No wait time
                            CombatManagerScript.cmsInstance.ClearQueuedEffects();
                            return monTurn;
                        }
                    }                    

                }   // End use ability code                 

            } // End has ability code

            MonsterTurnData mtd = AttackIfValid(fightTarget, paralyzed);
            if (mtd != null) return mtd;
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData TryMoveToTargetOrHitAlternate(Vector2 targetPos, bool paralyzed, bool petMustStayNearPlayer)
    {
        // We CAN see that target, but we're not in attack distance.
        CustomAlgorithms.GetPointsOnLineNoGarbage(GetPos(), targetPos);
        bool heroAlliesOnPath = false;
        MapTileData checkMTD;
        Actor mTarget = null;
        for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
        {
            if ((CustomAlgorithms.pointsOnLine[i] == GetPos()) || (CustomAlgorithms.pointsOnLine[i] == targetPos)) continue;
            checkMTD = MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]);
            if (checkMTD.tileType != TileTypes.GROUND) continue;
            if (checkMTD.GetBreakableCollidable(this) != null) continue;
            mTarget = checkMTD.GetTargetableForMonster();

            // This is used for targeting pets, not the hero.
            if (mTarget != null && mTarget.actorfaction != actorfaction && mTarget.actorfaction != Faction.DUNGEON 
                && MapMasterScript.GetGridDistance(GetPos(), mTarget.GetPos()) <= cachedBattleData.weaponAttackRange)
            {
                if (mTarget.GetActorType() != ActorTypes.MONSTER) continue;
                if (MapMasterScript.CheckTileToTileLOS(GetPos(), mTarget.GetPos(), this, MapMasterScript.activeMap))
                {
                    heroAlliesOnPath = true;
                    //Debug.Log(actorRefName + " is blocked by hero ally " + mTarget.actorRefName + " and I can hit it.");
                    break;
                }
            }
        }

        if (heroAlliesOnPath && !paralyzed && CheckAttribute(MonsterAttributes.CANTATTACK) == 0)
        {
            // Not using an ability, just attacking normally.                       
            Fighter mFighter = mTarget as Fighter;
            MonsterTurnData mtd = AttackIfValid(mFighter, paralyzed);
            if (mtd != null) return mtd;
        }

        // We can see our target, but we're out of range.
        Actor nextBestTarget = GetNearestVisibleTarget(petMustStayNearPlayer);
        if (nextBestTarget != null)
        {
            //Debug.Log("Switch targets to " + nextBestTarget.actorRefName);
            SetMyTarget(nextBestTarget);
            MonsterTurnData mtd = AttackIfValid(myTarget as Fighter, paralyzed);
            if (mtd != null) return mtd;

        }

        //If we are in fight mode, perhaps we are a pet who can jump over the player?

        MapTileData maybeJump = PetJumpOverHero(GetPos(), targetPos);
        if (maybeJump != null)
        {
#if UNITY_EDITOR
            Debug_AddMessage("Jumped over hero!");
#endif
            return ImmediateJumpToAnchorOrPosition(maybeJump.pos);
        }

        bool considerMoving = true;
        if (petMustStayNearPlayer && MapMasterScript.GetGridDistance(targetPos, GetPos()) >= 2)
        {
            considerMoving = false;
        }

        if (considerMoving)
        {
            MonsterMoveData mmd = GetMoveTile(GetPos(), targetPos);
            if (mmd != null)
            {
                if (mmd.abilityUsed == null)
                {
                    //Debug.Log("Move towards my target. Was " + GetPos() + " Move to " + mmd.destinationTile.pos + " Dist difference " + MapMasterScript.GetGridDistance(GetPos(),targetPos) + " " + MapMasterScript.GetGridDistance(mmd.destinationTile.pos,targetPos));
                    return AttemptMovement(mmd.destinationTile.pos);
                }
                else
                {
                    return ExecuteMovementAbility(mmd);
                }
            }
        }

        return myMonsterTurnData.Continue();
    }

    MonsterTurnData TryFightBehavior(bool paralyzed, bool petMustStayNearPlayer)
    {
        // Do we have a target? If not, get one.
        // Can we buff ourselves? Make better logic for this - when to buff.
        MonsterTurnData checkBuffs = CheckForAndUseBuffAbility();
        if (checkBuffs.turnType != TurnTypes.CONTINUE)
        {
            return checkBuffs;
        }

        // Is the target within range? Attack it!

        float distance = 999f;
        Vector2 targetPos = GetPos();
        if (myTarget != null)
        {
            if (!myTarget.objectSet)
            {
                targetPos = myTarget.GetPos();
                if (Debug.isDebugBuild) Debug.Log("WARNING! " + actorRefName + " " + actorUniqueID + " " + GetPos() + " trying to hit target " + myTarget.displayName + " at " + GetPos() + ", but it has no object!");
            }
            else
            {
                targetPos = myTarget.GetPos();
                distance = MapMasterScript.GetGridDistance(GetPos(), targetPos); // Distance between me and target.
            }
        }

        // Improve range algorithm.
        float attackRange = cachedBattleData.maxAttackRange;

        MonsterTurnData checkSniper = CheckForAndTrySniperBehavior(attackRange, distance, targetPos);
        if (checkSniper.turnType != TurnTypes.CONTINUE)
        {
            return checkSniper;
        }

        MonsterTurnData checkSummon = CheckForAndUseSummonAbility();
        if (checkSummon.turnType != TurnTypes.CONTINUE)
        {
            return checkSummon;
        }

        bool canSeeHero = CheckIfHeroIsVisibleOrFindNewTarget(petMustStayNearPlayer);

        MonsterTurnData checkSupporter = CheckForAndTrySupporterBehavior(distance);
        if (checkSupporter.turnType != TurnTypes.CONTINUE)
        {
            return checkSupporter;
        }

        MonsterTurnData checkAttack = CheckForAndExecuteAttackAction(distance, attackRange, canSeeHero, paralyzed);
        if (checkAttack.turnType != TurnTypes.CONTINUE)
        {
            return checkAttack;
        }

        // Not within range? Pathfind, then move.          

        // We can't SEE the target.
        if (distance > myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.CUR))
        {
            Vector2 newPos = FindRandomPos(true); // It's ok to move outside limit during combat
            return AttemptMovement(newPos);
        }

        MonsterTurnData checkAlternate = TryMoveToTargetOrHitAlternate(targetPos, paralyzed, petMustStayNearPlayer);
        if (checkAlternate.turnType != TurnTypes.CONTINUE)
        {
            return checkAlternate;
        }

        return myMonsterTurnData.Continue();
    }

    bool CheckForParalysis()
    {
        bool paralyzed = false;
        if (UnityEngine.Random.Range(0, 1.0f) < influenceTurnData.paralyzeChance)
        {
            if (isBoss && UnityEngine.Random.Range(0, 1f) <= 0.75f)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("resist_paralyze_log", this);
            }
            else
            {
                paralyzed = true;
            }
        }

        return paralyzed;
    }

    MonsterTurnData CheckForAndTryNeutralBehavior()
    {
        // Neutral state - what happens?
        if (myBehaviorState == BehaviorState.NEUTRAL || myBehaviorState == BehaviorState.CURIOUS)
        {
            UpdateMyAnchor();

            MonsterTurnData checkMoveToAnchor = MoveToAnchorIfNecessary();
            if (checkMoveToAnchor.turnType != TurnTypes.CONTINUE)
            {
                return checkMoveToAnchor;
            }

            CheckIfActorOfInterestIsAlive();

            // Move, I guess!
            Vector2 pos = GetPos();

            // AGGRO Behavior. Try aggroing the hero, as well as hero pets.
            bool CanSeeHero = TryToAggroHero();

            CheckForNoiseAttraction();

            CheckForPlayerToStalk(CanSeeHero);

            // WANDER Behavior
            MonsterTurnData checkForWander = CheckForAndTryWanderBehavior(CanSeeHero);
            if (checkForWander.turnType != TurnTypes.CONTINUE)
            {
                return checkForWander;
            }
        }

        return myMonsterTurnData.Continue();
    }

    // Checks if tile is dangerous *to me*
    // Returns cached value, or if there is no cached value, calculates + caches it
    public bool IsTileDangerous(MapTileData mtd)
    {
        bool debug = false;
        if (debug)
        {
            Debug.Log("Mtd null? " + (mtd == null));
            Debug.Log("Dangerous tiles null? " + (dangerousTilesToMe == null));
            Debug.Log("iPos null? " + (mtd.iPos == null));
        }
        TileDangerStates getState = dangerousTilesToMe[mtd.iPos.x, mtd.iPos.y];
        if (getState != TileDangerStates.NOTCHECKED)
        {
            if (mtd.CheckTag(LocationTags.ISLANDSWATER)) // Special case fpr this tile type: has to be rolled every time.
            {
                // We want to allow wandering through Deadly Void *sometimes*...
                if (UnityEngine.Random.Range(0, 1f) <= 0.4f)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (getState == TileDangerStates.SAFE) return false;
            if (getState == TileDangerStates.DANGEROUS) return true;
        }

        bool isDangerous = mtd.IsDangerous(this);
        if (isDangerous)
        {
            dangerousTilesToMe[mtd.iPos.x, mtd.iPos.y] = TileDangerStates.DANGEROUS;
            return true; 
        }
        else
        {
            dangerousTilesToMe[mtd.iPos.x, mtd.iPos.y] = TileDangerStates.SAFE;
            return false;
        }
    }

    public void PickTargetFromPossibleTargets()
    {
        Actor best = null;
        float highest = 0f;
        foreach(AggroData ad in combatTargets)
        {
            if (!ad.combatant.IsFighter()) continue;
            Fighter ft = ad.combatant as Fighter;
            if (!ft.myStats.IsAlive()) continue;
            if (ad.aggroAmount > highest)
            {
                best = ad.combatant;
                highest = ad.aggroAmount;
            }
        }
        SetMyTarget(best);
        if (myTarget != null)
        {
            myTargetTile = myTarget.GetPos();
            SetState(BehaviorState.FIGHT);
        }
    }

    public void ClearMyTarget()
    {
        myTarget = null;
    }

    public bool IsValidCombinableMob()
    {
        if (CheckAttribute(MonsterAttributes.COMBINABLE) > 0  && !isChampion && !foodLovingMonster && actorUniqueID != GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            // non-summoned pets should not be combinable
            if (actorfaction == Faction.PLAYER && turnsToDisappear <= 0 && GameMasterScript.heroPCActor.CheckSummon(this))
            {
                return false;
            }
            foreach(QuestScript qs in GameMasterScript.heroPCActor.myQuests)
            {
                if (qs.targetMonsterID == actorUniqueID) return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// At this point we MUST have a target (myTarget).
    /// </summary>
    /// <param name="tData"></param>
    /// <returns></returns>
    public bool VerifyTargetCanSeeMeIfTargetIsHero(TurnData tData)
    {
        if (myTarget.GetActorType() == ActorTypes.HERO)
        {
            if (!GameMasterScript.heroPCActor.CanSeeActor(this))
            {                
                return false;
            }
        }
        return true;
    }

    public void PrepareMyTurn()
    {
        //UnityEngine.Profiling.Profiler.BeginSample("Maybe its this tick/calc block?");
        TickAllCombatants();

        // Cooldowns are handled in ProcessAllMonsterActions
        //myAbilities.TickAllCooldowns();

        CalculateMaxRange();
        //UnityEngine.Profiling.Profiler.EndSample();

        //UnityEngine.Profiling.Profiler.BeginSample("Verifying Sprite Position!");
        VerifySpritePositionIsAccurate();
        //UnityEngine.Profiling.Profiler.EndSample();

        GameLogScript.BeginTextBuffer();

        //Clear old info from previous turn
#if UNITY_EDITOR
        if (DebugConsole.IsOpen)
        {
            ResetDebugAIInfo();
        }
#endif
    }
    /// <summary>
    /// Removes the 'TargetingLine' game object, if we have one.
    /// </summary>
    public void TryRemoveTargetingLine()
    {
        TargetingLineScript findLine = GetObject().GetComponentInChildren<TargetingLineScript>();
        if (findLine != null)
        {
            findLine.gameObject.SetActive(false);
            GameMasterScript.ReturnToStack(findLine.gameObject, "TargetingLine");
        }
    }
    public MonsterMoveData BuildJumpPath(Vector2 start, Vector2 finish)
    {
        // build line from me to my target tile
        CustomAlgorithms.GetPointsOnLineNoGarbage(start, finish, false, MapMasterScript.activeMap);

        int bestRange = 0;
        AbilityScript bestAbility = GetBestUsableMovementAbility(out bestRange);

        MonsterMoveData mmd = null;
        MapTileData furthestTileWeCanReach = null;

        for (int x = 0; x < CustomAlgorithms.numPointsInLineArray; x++)
        {
            if (CustomAlgorithms.pointsOnLine[x] == start) continue;

            if (x > bestRange) break;// this is beyond the furthest tile that we can possibly move.

            MapTileData checkTile = MapMasterScript.activeMap.GetTile(CustomAlgorithms.pointsOnLine[x]);

            if (checkTile.tileType == TileTypes.WALL)
            {
                break;
            }
            if (checkTile.IsCollidableEvenWithBreakable(this))
            {
                continue;
            }
            furthestTileWeCanReach = checkTile;
        }

        if (furthestTileWeCanReach == null)
        {
            return null;
        }

        mmd = new MonsterMoveData(furthestTileWeCanReach);
        mmd.abilityUsed = bestAbility;
        return mmd;
    }

    public AbilityScript GetBestUsableMovementAbility(out int bestRange)
    {
        bestRange = 0;
        bool rooted = false;
        AbilityScript bestAbil = null;

        if (UnityEngine.Random.Range(0, 1.0f) < influenceTurnData.rootChance)
        {
            rooted = true;
            influenceTurnData.rootedThisTurn = true;
        }

        for (int i = 0; i < considerAbilities.Count; i++)
        {
            AbilityScript abil = FetchLocalAbility(considerAbilities[i].abilityRef);

            bool ignoreCosts = false;
            if (abil.CanActorUseAbility(this, CanIgnoreCosts(considerAbilities[i].abilityRef)) && !rooted)
            {
                for (int g = 0; g < abil.listEffectScripts.Count; g++)
                {
                    EffectScript eff = abil.listEffectScripts[g];
                    if (eff.effectType == EffectType.MOVEACTOR)
                    {
                        MoveActorEffect mae = eff as MoveActorEffect;
                        if (mae.tActorType == TargetActorType.SELF || mae.tActorType == TargetActorType.ORIGINATING)
                        {
                            if (abil.range > bestRange)
                            {
                                bestAbil = abil;
                                bestRange = abil.range;
                            }
                        }
                    }
                }
            }
        }

        return bestAbil;
    }

    public AbilityScript GetBestUsableOffenseAbility(List<MonsterPowerData> usableAggAbilities, float maxRange, out MonsterPowerData mpdTried)
    {
        AbilityScript abilToUse = null;
        mpdTried = null;
        foreach (MonsterPowerData mpd in usableAggAbilities)
        {
            if (!mpd.CheckIfValid(GetPos(), myTarget)) continue; // MPD was tried in this same spot / relative positioning and didn't work. Skip.
            AbilityScript abil = mpd.abilityRef;
            // Use a damaging one if within range, otherwise, try to reposition the player.
            foreach (EffectScript eff in abil.listEffectScripts)
            {
                if (eff.effectType == EffectType.DAMAGE || eff.effectType == EffectType.ADDSTATUS || eff.effectType == EffectType.CHANGESTAT || abil.targetForMonster == AbilityTarget.ENEMY)
                {
                    // Need better way of calculating range properly via ground targets etc.
                    if (abil.range >= maxRange || mpd.alwaysUseIfInRange)
                    {
                        abilToUse = abil;
                        maxRange = abil.range;
                        mpdTried = mpd;
                        break;
                    }
                }
            }
        }
        return abilToUse;
    }

    /// <summary>
    /// Returns FALSE if we are an echo (friendly to player), or we are an illusion of the main monster
    /// </summary>
    /// <returns></returns>
    public bool IsTrueEnemyNotEchoOrIllusion()
    {
        if (actorfaction != Faction.ENEMY) return false;

        if (surpressTraits) return false;

        if (ReadActorData("illusion") != 1) return true;

        return false;
    }

    public bool CanIgnoreCosts(AbilityScript abil)
    {
        MonsterPowerData mpd;
        if (dictMonsterPowersStrToMPD.TryGetValue(abil.refName, out mpd))
        {
            return mpd.ignoreCosts;
        }

        return false;
    }
}
