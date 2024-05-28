using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    private void TurnEndCleanup(TurnData tData, int iThreadIndex = 0)
    {

        //Debug.Log("Execute turn end cleanup " + turnNumber + " on index " + iThreadIndex);


        if (!MapMasterScript.mapLoaded)
        {
            Debug.Log("WARNING! Turn end cleanup: There is no active map.");
        }

        SetTempGameData("buffer_thanesong_level", 0);
        SetTempGameData("playerswitchedsong", 0);

        GameLogScript.PrintEndOfTurnLogMessages();

        ProcessAllEndOfTurnFunctions();
        uims.RefreshAbilityCooldowns();
        heroPCActor.TurnEndCleanup();

        if (UIManagerScript.statusesDirty)
        {
            UIManagerScript.RefreshStatuses(true);
        }

        HoverInfoScript.TurnEndCleanup();

        TileInteractions.CheckForItemDreamAuraNotification();
        TileInteractions.CheckForPowerupsInHeroTile(heroPCActor.GetPos()); // This should cover powerups that spawned ON the player during the turn
        if (heroPCActor.myStats.CheckHasStatusName("emblem_husynemblem_tier2_runic") && heroPCActor.CheckSummonRefs("mon_runiccrystal"))
        {
            TileInteractions.CheckForPowerupsInHeroTile(heroPCActor.GetSummonByRef("mon_runiccrystal").GetPos());
        }

        SharaModeStuff.CheckForDominateHealthLog();

        TileInteractions.CleanupGhostsAroundHero();

        tutorialManager.CheckForAnyTutorialsThisTurn();

        if (heroPCActor.GetPos() == heroPCActor.previousPosition)
        {
            heroPCActor.turnsInSamePosition++;
            heroPCActor.movedLastTurn = false;
        }
        else
        {
            heroPCActor.movedLastTurn = true;
            heroPCActor.turnsInSamePosition = 0;
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_DANCER_EXTEND_SUMMON && heroPCActor.myStats.CheckHasStatusName("status_dancermove"))
            {
                foreach (Actor act in heroPCActor.summonedActors)
                {
                    if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                    if (act.actorRefName == "obj_flameserpent" || act.actorRefName == "obj_playericeshard")
                    {
                        act.turnsToDisappear++;
                    }
                }
            }
        }

        combatManager.ProcessQueuedEffects();

        UIManagerScript.UpdatePetInfo();
        turnExecuting = false;
        if (!(animationPlaying && animationFromCutscene))
        {
            SetAnimationPlaying(false);
        }

        SetItemBeingUsed(null);

        for (int i = 0; i < MapMasterScript.activeMap.monstersInMap.Count; i++)
        {
            Monster mn = MapMasterScript.activeMap.monstersInMap[i];
            if (!mn.myStats.IsAlive()) continue;
            mn.VerifySpritePositionIsAccurate();
        }

        #region Poison Air - Unused
        /* if (MapMasterScript.activeMap.dungeonLevelData.poisonAir)
        {
            if ((MapMasterScript.activeMap.dungeonLevelData.layoutType == DungeonFloorTypes.SPECIAL) || (heroPCActor.area == MapMasterScript.GetFillArea()))
            {
                bool breather = false;
                if (MapMasterScript.activeMap.dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS)
                {
                    pool_MTD = MapMasterScript.activeMap.GetListOfTilesAroundPoint(heroPCActor.GetPos(), 1);
                    breather = false;
                    for (int i = 0; i < pool_MTD.Count; i++)
                    {
                        if (pool_MTD[i].HasBreathingPillar())
                        {
                            breather = true;
                            break;
                        }
                    }
                }
                if (!breather)
                {
                    int amount = (int)((float)(heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.015f));
                    UIManagerScript.FlashHealthBar(0.5f);
                    heroPCActor.TakeDamage(amount, DamageTypes.POISON);
                    BattleTextManager.NewDamageText(amount, false, Color.yellow, heroPC, 0.0f, 1.0f);
                    heroPCActor.stepsInDifficultTerrain++;

                    if ((MapMasterScript.activeMap.dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS) && (!tutorialManager.WatchedTutorial("tutorial_poisonair")))
                    {
                        Conversation newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_poisonair");
                        UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                    }
                }
            }
        } */
        #endregion

        tutorialManager.CheckForEndOfTurnTutorials();

        heroPCActor.SetActorData("nomore_forcedmove_thisturn", 0);

        if (!heroPCActor.myStats.IsAlive())
        {
            //Debug.Log("Hero has " + heroPCActor.myStats.GetCurStat(StatTypes.HEALTH) + " HP, they have died.");
            if (MapMasterScript.activeMap.IsMysteryDungeonMap())
            {
                MysteryDungeonManager.MysteryDungeonGameOver();
            }
            else
            {
                GameOver();
            }

            return;
        }

        int mID = gmsSingleton.ReadTempGameData("petrescue_thisturn");
        if (mID > 0)
        {
            gmsSingleton.SetTempGameData("petrescue_thisturn", 0);
            Actor checkMon = gmsSingleton.TryLinkActorFromDict(mID);
            if (checkMon != null)
            {
                RemoveActorFromDeadQueue(checkMon);
            }
        }

        ProcessDeadQueue(MapMasterScript.activeMap);

        // *** SPECIAL HARDCODED STUFF

        GameplayScripts.TryDetectWeaknessEffects();
        GameplayScripts.TryRunicCrystalBoostEffects();
        GameplayScripts.TryFloramancerPlantGrowthAura();
        GameplayScripts.TryFloramancerPlantSynergy();

        if (JobTrialScript.IsJobTrialActive())
        {
            GameEventsAndTriggers.CheckForJobTrialVictory();
        }

        int itemWorldAura = MapMasterScript.GetItemWorldAura(heroPCActor.GetPos());

        heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.TURNEND);

        if (ReadTempGameData("deadqueue_process") == 1)
        {
            ProcessDeadQueue(MapMasterScript.activeMap);
        }

        GameEventsAndTriggers.CheckForBossClears();

        gmsSingleton.SetTempStringData("last_abilityref_used", "");
        combatManager.ProcessQueuedEffects(); // new code to process starhelm stuff.

        if (heroPCActor.TurnsSinceLastCombatAction > globalOutOfCombatLimit)
        {
            heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.OUTOFCOMBATTURN);
        }
        else if (heroPCActor.TurnsSinceLastCombatAction < 0)
        {
            heroPCActor.TurnsSinceLastCombatAction = 0;
        }
        if (!heroPCActor.myStats.IsAlive())
        {
            AddToDeadQueue(heroPCActor);
            ProcessDeadQueue(MapMasterScript.activeMap);
        }
        else
        {
            deadMonstersToRemove.Clear();
            foreach (Monster mn in MapMasterScript.activeMap.monstersInMap)
            {
                if (!mn.myStats.IsAlive())
                {
                    if (mn.deathProcessed)
                    {
                        deadMonstersToRemove.Add(mn);
                    }
                    else
                    {
                        AddToDeadQueue(mn);
                    }
                }
            }
            if (deadMonstersToRemove.Count > 0)
            {
                foreach (Monster mn in deadMonstersToRemove)
                {
                    MapMasterScript.activeMap.RemoveActorFromMap(mn);
                }
            }
        }

        // If we switched to ranged weapon during the turn, switch back.
        heroPCActor.TrySwitchToPreviousUsedWeapon();

        UIManagerScript.RefreshHotbarItems();

        CombatResultsScript.CheckForSideAreaClear(MapMasterScript.activeMap);

        if (ReadTempGameData("finishmysterydungeonturn") != turnNumber)
        {
            if ((MapMasterScript.activeMap.dungeonLevelData.spawnTable != null
                && MapMasterScript.activeMap.IsMainPath()
                && !MapMasterScript.activeMap.dungeonLevelData.bossArea &&
                !MapMasterScript.activeMap.dungeonLevelData.safeArea)
                || (MapMasterScript.activeMap.IsMysteryDungeonMap() && !MapMasterScript.activeMap.dungeonLevelData.noSpawner)
                || MapMasterScript.activeMap.dungeonLevelData.GetMetaData("respawningmonsters") == 1) // Mystery Dungeons can have wandering monsters
            {
                mms.WanderingMonsterCheck();
            }
        }

        if (gmsSingleton.ReadTempGameData("fov_recomputed") != 1)
        {
            mms.UpdateMapObjectData();
        }
        else
        {
            gmsSingleton.SetTempGameData("fov_recomputed", 0); // We already rebuilt LOS elsewhere.
            if (gmsSingleton.ReadTempGameData("hero_swapped") == 1)
            {
                gmsSingleton.SetTempGameData("hero_swapped", 0);
                mms.UpdateMapObjectData();
            }
            mms.CheckMapActorsLOS();
        }

        AutoEatFoodLogic.CheckForAndTryAutoEat();

        TDInputHandler.turnTimer = 0.0f;
        changePlayerTimerThisTurn = false;
        heroPCActor.ChangeCT(heroPCActor.cachedBattleData.chargeGain);
        //Debug.Log("Hero PC actor now at " + heroPCActor.actionTimer);

        //If the hero is going to get another turn after this one, say so.
        if (heroPCActor.actionTimer >= 200f && ReadTempGameData("finishmysterydungeonturn") != turnNumber && !heroPCActor.GetActorMap().IsTownMap())
        {
            if (PlayerOptions.extraTurnPopup && (heroPCActor.TurnsSinceLastCombatAction < 10 || heroPCActor.turnsSinceLastDamaged < 10))
            {
                GameLogScript.LogWriteStringRef("log_extraturn");
                BattleTextManager.NewText(StringManager.GetString("misc_extraturn").ToUpperInvariant() + "!", heroPCActor.GetObject(), Color.yellow, 0.5f);
            }
        }

        processBufferTargetDataIndex = 0;
        //Debug.Log(turnNumber);

        // Hardcoded for waiting / exhaustion after using something.

        GameplayScripts.CheckForSpellshiftSelfSealing(tData);

        tutorialManager.CheckForEndOfTurnTutorials();


        if (playerStatsChangedThisTurn)
        {
            UIManagerScript.RefreshPlayerStats();
            playerStatsChangedThisTurn = false;
        }
        if (GameMasterScript.heroPCActor.actionTimer >= 200)
        {
            UIManagerScript.RefreshPlayerCT(true);
        }
        else
        {
            if (GameMasterScript.heroPCActor.actionTimer >= 100)
            {
                UIManagerScript.RefreshPlayerCT(false);
            }
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            heroPCActor.CheckForSerenityBuff();
        }

        float pChance = 0.05f;
#if UNITY_EDITOR
        pChance = 1f;
#endif

        GameEventsAndTriggers.CheckForDelayedLevelUpAndFlaskInfusionPrompts();
        if (MapMasterScript.activeMap.IsItemWorld() && UnityEngine.Random.Range(0, 1f) <= pChance)
        {
            SpiritDragonStuff.EvaluateSpiritDragonQuestLineInItemDream();
        }


        heroPCActor.TryRefreshStatuses();
        
    }

}