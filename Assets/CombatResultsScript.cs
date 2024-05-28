using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
#endif

public class CombatResultsScript : MonoBehaviour
{
    public static void CheckCombatResult(CombatResult result, Actor target, Map processMap)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        MapMasterScript mms = GameMasterScript.mms;
        MusicManagerScript musicManager = GameMasterScript.musicManager;
        UIManagerScript uims = GameMasterScript.uims;

        if (result == CombatResult.PLAYERDIED)
        {
            //Debug.Log("Player should be dead");
            if (GameMasterScript.debug_neverDie)
            {
                return;
            }

            // Verify player is actually dead.
            if (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.HEALTH) >= 1f)
            {
                //Debug.Log("Player isn't actually dead.");
                GameMasterScript.RemoveActorFromDeadQueue(GameMasterScript.heroPCActor);
                GameMasterScript.heroPCActor.destroyed = false;
                GameMasterScript.playerDied = false;
                //GameLogScript.GameLogWrite(UIManagerScript.redHexColor + "Possible error occurred - please send your output log!", GameMasterScript.heroPCActor);
                return;
            }

            if (MapMasterScript.activeMap.IsMysteryDungeonMap())
            {
                MysteryDungeonManager.MysteryDungeonGameOver();
            }
            else
            {
                GameMasterScript.GameOver();
            }
            return;
        }

        if (GameMasterScript.playerDied)
        {
            // Don't continue if we game over'd
            return;
        }

        if (target == null)
        {
            Debug.Log("Combat result - null actor?");
            return;
        }

        if (processMap == null)
        {
            Debug.Log(target.actorRefName + " no process map..?");
            return;
        }

        if (result == CombatResult.MONSTERDIED)
        {                        
            if (target.GetActorType() != ActorTypes.MONSTER)
            {
                Debug.Log(target.actorRefName + " isn't a monster");
                return;
            }
            Monster mon = target as Monster;
            if (mon.myStats.IsAlive() && !mon.destroyed)
            {
                //Debug.Log(mon.actorRefName + " is alive, and not destroyed... don't process death.");
                return;
            }
            if (mon.actorRefName == "mon_targetdummy" || mon.actorRefName == "mon_harmlessfungaltoad" 
                || (mon.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID() && mon.actorfaction == Faction.ENEMY))
            {
                if (!(mon.surpressTraits && mon.actorRefName == "mon_harmlessfungaltoad"))
                {
                    // No kill pls
                    return;
                }
            }

            // This is done in the "completely destroy" function
            //processMap.RemoveActorFromMap(target);

            UIManagerScript.TryRemoveLastHeroTarget(mon);

            if (mon.deathProcessed)
            {
                return;
            }

            CheckForLivingVineSpecialCases(mon);

            if (mon.actorfaction == Faction.PLAYER)
            {
                GameMasterScript.heroPCActor.RemoveSummon(mon);
                if (heroPCActor.CanSeeActor(mon))
                {
                    CombatResultsScript.DoMonsterDeathFX(mon);
                }
            }

            mon.deathProcessed = true;

            CheckForNightmarePrinceDeath(mon);

            if (mon.myTemplate.showBossHealthBar)
            {
                BossHealthBarScript.DisableBoss();
            }

            mon.myStats.CheckRunAllStatuses(StatusTrigger.DESTROYED);

            if (mon.summonedActors != null)
            {
                if (mon.summonedActors.Count > 0 && GameMasterScript.masterMonsterList.ContainsKey(mon.actorRefName))
                {
                    mon.MarkAsDestroyed();
                    GameMasterScript.deadActorsToSaveAndLoad.Add(mon);
                }
            }

            if (processMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && (mon.actorRefName == "mon_harmlessfungaltoad" || mon.actorRefName == "mon_fungaltoad")
                && MetaProgressScript.ReadMetaProgress("corralquest") != 3)
            {
                UIManagerScript.StartConversationByRef("monstercorral_failquest", DialogType.STANDARD, null);
            }

            if (mon.actorUniqueID == heroPCActor.ReadActorData("knockedoutmonster"))
            {
                heroPCActor.RemoveActorData("knockedoutmonster");
            }

            bool displayDeathMessage = true;
            bool destroyCreature = true;

            destroyCreature = CheckForPlayerCorralDeathEvents(mon, out displayDeathMessage);

            CheckForGoldfrogDeath(mon);

            CheckForAvengerChampionNearby(mon, processMap);

            CheckForItemDreamEvents(mon, processMap);

            MysteryDungeonManager.CheckForMysteryDungeonEvents(mon, processMap);

            CheckForPlayerBenefitsOnMonsterDeath(mon);

            CheckForQuestSuccessOrFailure(mon);



            //Debug.Log(mon.actorRefName + " " + mon.actorUniqueID + " " + mon.destroyed + " " + mon.deathProcessed);

            mon.CheckForAndRemoveHauntTargetOnDeath();

            // ** ORIGINAL location of DESTROYED status trigger

            if (mon.summoner != null)
            {
                if (mon.summoner == heroPCActor)
                {
                    UIManagerScript.UpdatePetInfo();
                }
                mon.summoner.RemoveSummon(mon);
            }

            mon.RemoveAllSummonedActorsOnDeath();

            bool monsterIsDominatedByShara = SharaModeStuff.CheckIfMonsterIsDominated(mon);


            if (destroyCreature)
            {
                mon.MarkAsDestroyed();
                //Debug.Log(mon.actorRefName + " has been destroyed.");
            }            
            else
            {
                //Debug.Log("Not destroying " + mon.actorRefName);
            }
            mon.deathProcessed = true;
            if (mon.isChampion)
            {
                processMap.championCount--;
            }
            if (mon.actorRefName == "mon_finalbossai") // Beat the final boss!!! First phase anyway.
            {
                MetaProgressScript.TryAddMonsterFought(mon.actorRefName);

        if (PlatformVariables.SEND_UNITY_ANALYTICS)
        {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                    Analytics.CustomEvent("boss_defeated", new Dictionary<string, object>()
                {
                    { "boss", "finalboss_1" },
                    { "plvl", GameMasterScript.heroPCActor.myStats.GetLevel() },
                    { "hbslotsused", GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed() },
                    { "job", GameMasterScript.heroPCActor.myJob.jobEnum.ToString() }
                });
#endif
                }

                GameMasterScript.heroPCActor.SetActorData("finalboss1", 2);

                Cutscenes.FinalBossPhase2_Part1(mon.GetPos());

                //Stop any further monster processing
                return;
            }

            CheckForMonsterDeathEventTriggers(mon, processMap);

            GameEventsAndTriggers.CheckForPainterQuestCompletion();

            if (mon.whoKilledMe != heroPCActor && mon.whoKilledMe != null && displayDeathMessage)
            {
                StringManager.SetTag(0, target.displayName);

                GameLogScript.LogWriteStringRef("log_monsterdeath_unknown");
            }

            // New code - get rid of the healthbar
            if (mon.healthBarScript != null)
            {
                GameMasterScript.ReturnToStack(mon.healthBarScript.gameObject, mon.healthBarScript.name.Replace("(Clone)", string.Empty));
            }

            CheckForItemCrystalExplosion(mon);

            bool lootGenerated = false;

            bool isFinalBoss = mon.actorRefName == "mon_finalboss2" || mon.actorRefName == "mon_shara_finalboss";

            if (mon.whoKilledMe != null)
            {              
                if ((mon.actorfaction != Faction.PLAYER && !isFinalBoss) || monsterIsDominatedByShara)
                {
                    LootGeneratorScript.TryGenerateLoot(target, target.GetPos());
                    lootGenerated = true;
                }
            }

            if (mon.isItemBoss && !lootGenerated)
            {
                LootGeneratorScript.TryGenerateLoot(target, target.GetPos());
            }

            bool destroyObjectAndSprite = true;

            if (mon.GetActorType() == ActorTypes.MONSTER)
            {
                Monster defeatedMon = mon as Monster;

                List<string> poolStrings = null;
                bool anyScripts = false;

                if (!string.IsNullOrEmpty(defeatedMon.scriptOnDefeat))
                {
                    poolStrings = new List<string>();
                    anyScripts = true;
                    poolStrings.Add(defeatedMon.scriptOnDefeat);
                }

                if (!string.IsNullOrEmpty(MapMasterScript.activeMap.dungeonLevelData.script_onMonsterDeath))
                {
                    if (poolStrings == null) poolStrings = new List<string>();
                    anyScripts = true;
                    poolStrings.Add(MapMasterScript.activeMap.dungeonLevelData.script_onMonsterDeath);
                }

                if (anyScripts)
                {
                    foreach (string funcName in poolStrings)
                    {                        
                        MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(MonsterDeathEventScripts), funcName);
                        object[] paramList = new object[1];
                        paramList[0] = defeatedMon;
                        bool shouldDestroy = (bool)runscript.Invoke(null, paramList);
                        // however, if *any* script said DONT destroy the object, then we absolutely shouldnt.
                        if (destroyObjectAndSprite && !shouldDestroy)
                        {
                            destroyObjectAndSprite = false;
                        }
                    }
                }
            }

            // End FF6 style death fadeout
            float xpMod = mon.GetXPModToPlayer();

            CheckForCoinDestructibleSpawn(mon, xpMod, processMap);

            if (destroyObjectAndSprite && mon.whoKilledMe != null)
            {
                if (!isFinalBoss)
                {
                    DoMonsterDeathFX(mon);
                }
            }

            if (mon.whoKilledMe != null)
            {
                // Hero or hero summon gets credit
                if ((mon.whoKilledMe == heroPCActor || heroPCActor.CheckSummon(mon.whoKilledMe)) || 
                    monsterIsDominatedByShara)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("deadmonid",mon.actorUniqueID);
                    MetaProgressScript.TryAddMonsterFought(mon.actorRefName);
                    if (xpMod > 0f)
                    {
                        heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.KILLENEMY_NOT_WORTHLESS);
                    }
                    if (xpMod >= 0.25f)
                    {
                        if (mon.lastAttackTypeReceived == AttackType.ABILITY)
                        {
                            heroPCActor.SetActorDataString("lastkill", "abil");
                        }
                        else
                        {
                            heroPCActor.SetActorDataString("lastkill", "atk");                            
                        }
                        heroPCActor.myStats.CheckRunAndTickAllStatuses(StatusTrigger.KILLENEMY_NOT_TRIVIAL);

                    }
                }

                // What if an enemy monster kills something?!
                if (mon.whoKilledMe.GetActorType() == ActorTypes.MONSTER && mon.ReadActorData("illusion") != 1)
                {
                    Monster killer = mon.whoKilledMe as Monster;
                    float monXPMod = mon.InternalGetXPMod(killer.myStats.GetLevel());
                    GameMasterScript.gmsSingleton.SetTempGameData("deadmonid",mon.actorUniqueID);
                    if (monXPMod >= 0.25f)
                    {
                        killer.myStats.CheckRunAndTickAllStatuses(StatusTrigger.KILLENEMY_NOT_TRIVIAL);
                    }                    
                }

            }

            //Debug.Log("Preparing to award player. " + mon.destroyed + " " + mon.deathProcessed);

            if (!isFinalBoss)
            {
                CheckForPlayerRewards(mon, xpMod, monsterIsDominatedByShara);
                CompletelyDestroyMonsterAndObject(processMap, mon, destroyObjectAndSprite);
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("finalboss_id", mon.actorUniqueID);
                GameMasterScript.SetAnimationPlaying(true);
            }

            if (mon.actorfaction == Faction.ENEMY)
            {
                MapMasterScript.activeMap.PrintMonstersRemainingToLogIfNecessary();                
                
                if (SharaModeStuff.CheckForBoss1Clear())
                {
                    return;
                }

                if (SharaModeStuff.CheckForBoss2Clear())
                {
                    return;
                }
            }


        }
    }

    public static void CompletelyDestroyMonsterAndObject(Map processMap, Monster mon, bool destroyObjectAndSprite = true)
    {
        if (mon == null)
        {
            Debug.Log("Cannot remove null monster from map.");
            return;
        }

        if (mon.ReadActorData("death_processed") == 1)
        {
            if (Debug.isDebugBuild) Debug.Log("Not destroying dead actor " + mon.actorRefName + "," + mon.actorUniqueID + " because death was handled elsewhere.");
            mon.SetActorData("death_processed", 0);
            return;
        }

        //Debug.Log("Destroying " + mon.actorRefName + " " + mon.actorUniqueID + " who is at " + mon.GetPos());

        if (processMap.RemoveActorFromLocation(mon.previousPosition, mon) && mon.GetPos() != mon.previousPosition)
        {
            //Debug.Log("Actor " + mon.actorRefName + " " + mon.actorUniqueID + " died at " + mon.GetPos() + " but was also in " + mon.previousPosition + "???");
        }

        processMap.RemoveActorFromMap(mon);
        processMap.RemoveActorFromLocation(mon.positionAtStartOfTurn, mon);

        GameObject dgo = mon.GetObject();
        if (destroyObjectAndSprite && dgo != null)
        {
            mon.myMovable.FadeOutThenDie();
            GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(mon, dgo, 0.75f));
        }
    }

    public static void CheckForGoldfrogDeath(Monster mon)
    {
        if ((mon.actorRefName == "mon_goldfrog" || mon.actorRefName == "mon_darkfrog") && mon.whoKilledMe != null && mon.whoKilledMe.actorfaction == Faction.PLAYER)
        {
            StringManager.SetTag(0, mon.displayName);
            GameLogScript.LogWriteStringRef("log_event_goldfrog_died");
            float goldfrogMoney = UnityEngine.Random.Range(100, 150) * mon.myStats.GetLevel();

            if (MapMasterScript.activeMap.IsMysteryDungeonMap())
            {
                goldfrogMoney = UnityEngine.Random.Range(100, 150) * (MapMasterScript.activeMap.floor - MapMasterScript.CUSTOMDUNGEON_START_FLOOR);
            }

            if (mon.actorRefName == "mon_darkfrog")
            {
                goldfrogMoney *= 1.3f;
                LootGeneratorScript.SpawnRandomPowerup(mon, true);
                LootGeneratorScript.SpawnRandomPowerup(mon, true);
                LootGeneratorScript.SpawnRandomPowerup(mon, true);
                LootGeneratorScript.SpawnRandomPowerup(mon, true);
                LootGeneratorScript.SpawnRandomPowerup(mon, true);
            }

            int numStacks = UnityEngine.Random.Range(7, 10);
            float moneyPerStack = goldfrogMoney / numStacks;
            moneyPerStack = UnityEngine.Random.Range(0.8f * moneyPerStack, 1.2f * moneyPerStack);
            MapTileData origTile = MapMasterScript.GetTile(mon.GetPos());
            List<MapTileData> possible = null;
            int radius = 2;
            int attempts = 0;
            while (true)
            {
                attempts++;
                possible = MapMasterScript.activeMap.GetListOfTilesAroundPoint(mon.GetPos(), radius);
                List<MapTileData> pool_MTD = new List<MapTileData>();
                foreach (MapTileData mtd in possible)
                {
                    if ((mtd.tileType == TileTypes.WALL) || (mtd.playerCollidable) || (mtd.AreItemsOrDestructiblesInTile()))
                    {
                        pool_MTD.Add(mtd);
                    }
                }
                foreach (MapTileData mtd in pool_MTD)
                {
                    possible.Remove(mtd);
                }

                if (possible.Count < numStacks)
                {
                    radius++;
                }
                else
                {
                    break;
                }

                if (attempts > 5)
                {
                    numStacks--;
                    attempts = 0;
                    if (numStacks < 1)
                    {
                        break;
                    }
                }
            }

            possible.Shuffle();

            for (int i = 0; i < numStacks; i++)
            {
                MapTileData tileForMoney = possible[i];

                MapMasterScript.SpawnCoins(origTile, tileForMoney, (int)moneyPerStack);
            }
        }
    }

    public static void CheckForAvengerChampionNearby(Monster mon, Map processMap)
    {
        foreach (Monster mn in processMap.monstersInMap)
        {
            if (mn.isChampion && mn.actorfaction == mon.actorfaction && mn != mon)
            {
                if (mn.myStats.CheckHasStatusName("monmod_avenger") && 
                    MapMasterScript.CheckTileToTileLOS(mon.GetPos(), mn.GetPos(), mon, MapMasterScript.activeMap))
                {
                    // mystery king chaser should not benefit from this until it is unlocked
                    if (mn.ReadActorData("mystery_king_chaser") == 1) continue;

                    CombatManagerScript.SpawnChildSprite("AggroEffect", mn, Directions.NORTHEAST, false);
                    StringManager.SetTag(0, mn.displayName);
                    StringManager.SetTag(1, mon.displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("log_monster_avenger"), mn);
                    BattleTextManager.NewText(StringManager.GetString("popup_atk_up"), mn.GetObject(), Color.red, 1f);
                    mn.allDamageMultiplier += 0.12f;
                    CombatManagerScript.SpawnChildSprite("FervirBuffSilent", mn, Directions.TRUENEUTRAL, false);
                }
            }
        }
    }

    public static void CheckForItemDreamEvents(Monster mon, Map processMap)
    {
        if (mon.actorRefName == "mon_nightmarecrystal")
        {
            ItemDreamFunctions.PlayerDestroyedNightmareCrystal();
        }
        if (processMap.IsItemWorld() && mon.actorfaction == Faction.ENEMY)
        {
            StringManager.SetTag(0, mon.displayName);
            string build = StringManager.GetString("log_mon_exclamation") + " <#fffb00>'";
            build += GameMasterScript.itemWorldMonsterDeathLines[UnityEngine.Random.Range(0, GameMasterScript.itemWorldMonsterDeathLines.Count)] + "'</color>";
            GameLogScript.GameLogWrite(build, mon);

            if (mon.actorUniqueID == ItemDreamFunctions.FindNightmareKingID())
            {
                // Killed nightmare king.
                ItemDreamFunctions.PlayerKilledNightmareKing(mon);    
            }
            else if (mon.isItemBoss && !mon.myStats.CheckHasStatusName("status_shadowking"))
            {
                GameMasterScript.heroPCActor.SetActorData("killed_memory_king", 1);

                int npFloor = GameMasterScript.heroPCActor.ReadActorData("iw_np_floor");

                if (npFloor > 0)
                {
                    // Destroy nightmare prince once memory king is killed.                    
                    Map nkMap = null;

                    if (MapMasterScript.dictAllMaps.ContainsKey(npFloor))
                    {
                        nkMap = MapMasterScript.dictAllMaps[npFloor];
                    }
                    else
                    {
                        Debug.Log("WARNING: Couldn't find nightmare prince to destroy, floor/ID " + npFloor + " so searching manually.");
                        nkMap = MapMasterScript.itemWorldMaps[MapMasterScript.itemWorldMaps.Length - 1];
                    }

                    Monster nk = null;
                    foreach (Monster checkMon in nkMap.monstersInMap)
                    {
                        if (checkMon.isItemBoss && checkMon.myStats.CheckHasStatusName("status_shadowking"))
                        {
                            nk = checkMon;
                            break;
                        }
                    }
                    if (nk != null)
                    {
                        nk.myInventory.ClearInventory();
                        nkMap.RemoveActorFromLocation(nk.GetPos(), nk);
                        nkMap.RemoveActorFromMap(nk);
                    }


                }

                ItemDreamFunctions.UpgradeItemDreamItem(false);
            }
        }
    }

    public static void CheckForPlayerRewards(Monster mon, float xpMod, bool monsterIsDominatedByShara = false)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if (mon.whoKilledMe != null && (mon.whoKilledMe.actorfaction == Faction.PLAYER || monsterIsDominatedByShara))
        {
            heroPCActor.RemoveTarget(mon);
            if (mon.whoKilledMe != heroPCActor && mon.whoKilledMe.GetActorType() == ActorTypes.MONSTER)
            {
                Fighter ft = mon.whoKilledMe as Fighter;
                ft.RemoveTarget(mon);
            }

            heroPCActor.TryDrawWildCard(mon);

            Fighter fight = mon as Fighter;
            CombatResultsScript.AwardXPJPGoldFromMonsterDefeat(fight);

            if (mon.whoKilledMe == heroPCActor && xpMod > 0f && heroPCActor.myStats.CheckHasStatusName("glorious_battler_passive"))
            {
                heroPCActor.IncreaseSongLevel();
                heroPCActor.actionTimer += GameMasterScript.GLORIOUS_BATTLER_CT_GAIN;
            }

            heroPCActor.monstersKilled++;
            if (mon.isChampion)
            {
                if (xpMod > 0)
                {
                    heroPCActor.KilledChampionWithWeapon();                    
                }                
                heroPCActor.championsKilled++;
                GameMasterScript.gmsSingleton.statsAndAchievements.IncrementChampionsDefeated();
                if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f)
                {
                    SharedBank.UnlockFeat("skill_rager");
                }
                if (heroPCActor.championsKilled >= 10)
                {
                    SharedBank.UnlockFeat("skill_champfinder");
                }
            }
        }
    }

    public static void CheckForCoinDestructibleSpawn(Monster mon, float xpMod, Map processMap)
    {
        if (mon.moneyHeld > 0 && (UnityEngine.Random.Range(0, 1f) <= xpMod || UnityEngine.Random.Range(0, 4) == 0))
        {
            Destructible coins = processMap.CreateDestructibleInTile(MapMasterScript.GetTile(mon.GetPos()), "obj_coins");
            coins.AddMoney(mon.moneyHeld);
            MapMasterScript.singletonMMS.SpawnDestructible(coins);
        }
    }

    public static void DoMonsterDeathFX(Monster mon, bool playSFX = true)
    {
        if (Vector2.Distance(mon.GetPos(), GameMasterScript.heroPCActor.GetPos()) <= GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE))
        {
            GameObject deathRedFade = GameMasterScript.TDInstantiate("MonsterDeath");
            if (playSFX)
            {
                CombatManagerScript.TryPlayEffectSFX(deathRedFade, mon.GetPos(), null);
            }            

            if (mon.objectSet && mon.GetObject().activeSelf)
            {
                deathRedFade.transform.position = mon.GetObject().transform.position;
            }
            else
            {
                deathRedFade.transform.position = mon.GetPos();
            }

            if (mon.mySpriteRenderer != null)
            {
                Sprite origSprite = mon.mySpriteRenderer.sprite;                
                Animatable anm = deathRedFade.GetComponent<Animatable>();
                anm.myAnimations[0].SetSpriteOnly(0, origSprite); // as mSprite
                deathRedFade.GetComponent<SpriteRenderer>().sprite = origSprite; // was mSprite
                deathRedFade.GetComponent<SpriteRenderer>().color = Color.red;
                deathRedFade.GetComponent<Movable>().forceColor = Color.red;
                anm.SetAnim("Default");
            }
        }
    }

    public static void CheckForItemCrystalExplosion(Monster mon)
    {
        int itemWorldAura = MapMasterScript.GetItemWorldAura(mon.GetPos());
        if (itemWorldAura == (int)ItemWorldAuras.EXPLODEONDEATH)
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(mon.GetPos(), "BigExplosionEffect", null);
            List<MapTileData> pool_MTD = MapMasterScript.activeMap.GetListOfTilesAroundPoint(mon.GetPos(), MapMasterScript.ITEM_WORLD_AURA_SIZE);
            List<Actor> affected = MapMasterScript.GetAllTargetableInTiles(pool_MTD);
            Actor act;
            for (int i = 0; i < affected.Count; i++)
            {
                act = affected[i];
                if ((act.GetActorType() != ActorTypes.MONSTER) && (act.GetActorType() != ActorTypes.HERO))
                {
                    continue;
                }
                Fighter ft = act as Fighter;
                if (ft != mon && ft.myStats.IsAlive())
                {
                    // Do explosion effect.
                    float dmg = mon.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.15f;
                    StringManager.SetTag(0, CombatManagerScript.fireDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.fireDescriptors.Length)]);
                    StringManager.SetTag(1, ft.displayName);
                    StringManager.SetTag(2, ((int)dmg).ToString());
                    GameLogScript.GameLogWrite(StringManager.GetString("log_explosion_dmg"), ft);
                    float processDamage = CombatManagerScript.ProcessDamage(mon, ft, AttackType.ATTACK, new DamageCarrier(dmg, DamageTypes.FIRE), null);
                    BattleTextManager.NewDamageText((int)dmg, false, Color.red, ft.GetObject(), 0f, 1f);
                    ft.TakeDamage(processDamage, DamageTypes.FIRE);
                }
            }
        }
    }

    public static void CheckForSideAreaClear(Map processMap)
    {
        if (processMap.dungeonLevelData.floor == MapMasterScript.SPECIALFLOOR_DIMENSIONAL_RIFT)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("dimrift") < 1 && processMap.unfriendlyMonsterCount < 3)
            {                
                processMap.RecountMonsters();
                Debug.Log(processMap.unfriendlyMonsterCount + " left in dim rift after recount");
                if (processMap.unfriendlyMonsterCount <= 0)
                {
                    Cutscenes.StartDimRiftCutscene();
                }                
            }
        }

        if (processMap.floor == MapMasterScript.JOB_TRIAL_FLOOR2 && JobTrialScript.IsJobTrialActive())
        {
            JobTrialScript.SpawnTrialRelicIfNeeded();
        }

        if (processMap.floor == MapMasterScript.JOB_TRIAL_FLOOR && JobTrialScript.IsJobTrialActive())
        {
            switch(GameMasterScript.heroPCActor.jobTrial.trialTierLevel)
            {
                case 1: // Kill all the monsters
                case 2: 
                    if (processMap.unfriendlyMonsterCount > 0 && processMap.unfriendlyMonsterCount < 4)
                    {
                        processMap.RecountMonsters();
                    }
                    if (processMap.unfriendlyMonsterCount <= 0 || (processMap.unfriendlyMonsterCount == 1 && GameMasterScript.heroPCActor.HasKnockedOutMonster()))
                    {
                        if (GameMasterScript.heroPCActor.jobTrial.trialTierLevel == 1)
                        {
                            JobTrialScript.BeatJobTrial();
                        }
                        else
                        {
                            JobTrialScript.CheckForTrialTier3Clear();
                        }
                        
                    }
                    break;
            }
        }

        if (processMap.IsClearableSideArea() && processMap.unfriendlyMonsterCount < 3) //&& processMap.dungeonLevelData.clearRewards.Count > 0)
        {
            if (processMap.unfriendlyMonsterCount > 0)
            {
                processMap.RecountMonsters();
            }            
            else if (processMap.floor == MapMasterScript.ELEMENTAL_LAIR3_FLOOR)
            {
                //used to be == 0, possibility that the monster count is being reduced to -1
                //but also, make sure if the quest is complete that we never trigger this again
                if (processMap.unfriendlyMonsterCount <= 0 && ProgressTracker.CheckProgress(TDProgress.ARMOR_MASTER_QUEST, ProgressLocations.HERO) < 2 
                    && ProgressTracker.CheckProgress(TDProgress.ARMOR_MASTER_QUEST, ProgressLocations.HERO) != 3)
                {
                    ProgressTracker.SetProgress(TDProgress.ARMOR_MASTER_QUEST, ProgressLocations.HERO, 2);
                }
            }


            if (processMap.floor == MapMasterScript.CASINO_BASEMENT)
            {
                if (GameMasterScript.heroPCActor.ReadActorData("casino_bandit_quest") < 2)
                {
                    GameMasterScript.heroPCActor.SetActorData("casino_bandit_quest", 2);
                }
            }

            

            bool clearPossible = true;

            if (processMap.floor == MapMasterScript.ROMANCE_SIDEAREA)
            {
                if (processMap.unfriendlyMonsterCount <= 0 && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) < 2)
                {
                    UIManagerScript.StartConversationByRef("friendshipforest_bossmon", DialogType.KEYSTORY, null);
                    clearPossible = false;
                }
            }

            // #todo - data drive this
            if (processMap.floor == 221) // Craggan miner rescue quest
            {
                if (processMap.unfriendlyMonsterCount <= 0 && GameMasterScript.heroPCActor.ReadActorData("craggan_mine_rescue") < 2)
                {
                    GameMasterScript.heroPCActor.SetActorData("craggan_mine_rescue", 2);
                }
            }


            if (GameMasterScript.heroPCActor.CheckIfMapCleared(processMap))
            {
                if (Debug.isDebugBuild) Debug.Log("Map is already cleared.");
                return;
            }
            if (processMap.dungeonLevelData.spawnTable == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Map has no spawn table.");
                //Debug.Log("No spawn table");
                return;
            }

            if (processMap.unfriendlyMonsterCount > 0)
            {
                processMap.RecountMonsters();
            }
           
            if (Debug.isDebugBuild) Debug.Log("SWITCH CHECK 2: " + processMap.clearedMap + " " + clearPossible + " " + processMap.unfriendlyMonsterCount);

            if (processMap.unfriendlyMonsterCount <= 0 && !processMap.clearedMap 
                && clearPossible && !processMap.dungeonLevelData.noRewardPopup)
            {
                bool stairsDown = false;
                foreach (Stairs st in processMap.mapStairs)
                {
                    if (!st.stairsUp)
                    {
                        stairsDown = true;
                        break;
                    }
                }
                if (!stairsDown)
                {
                    float jpAward = (int)(Mathf.Pow(processMap.GetChallengeRating(), 3.75f) * processMap.GetChallengeRating() * 23f + UnityEngine.Random.Range(6, 9f));
                    
                    StringManager.SetTag(4, StringManager.GetPortalBindingString());
                    StringManager.SetTag(0, ((int)jpAward).ToString());

                    if (PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
                    {
                        GameMasterScript.gmsSingleton.SetTempStringData("help_sideareaclear", StringManager.GetString("dialog_side_area_clear_main_txt_controller", true));
                    }
                    else
                    {
                        GameMasterScript.gmsSingleton.SetTempStringData("help_sideareaclear", StringManager.GetString("dialog_side_area_clear_main_txt", true));
                    }


                    UIManagerScript.StartConversationByRef("side_area_clear", DialogType.TUTORIAL, null);
                    processMap.clearedMap = true;
                    GameMasterScript.heroPCActor.ClearMap(processMap);

                    if (GameMasterScript.heroPCActor.ReadActorData("floorid_highestfloor") != processMap.floor || processMap.dungeonLevelData.GetMetaData("passday") == 1)
                    {
                        GameMasterScript.gmsSingleton.TickGameTime(1, false);
                    }
                    
                    GameMasterScript.gmsSingleton.AwardJP(jpAward);
                    UIManagerScript.UpdateDungeonText();
                    GameMasterScript.heroPCActor.SetActorData("sideclear" + processMap.floor, 1);

                    foreach (string str in MapMasterScript.activeMap.dungeonLevelData.clearRewards)
                    {
                        MapTileData mtdEmpty = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true);
                        Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(mtdEmpty, str);
                        MapMasterScript.singletonMMS.SpawnDestructible(dt);
                    }

                    UIManagerScript.PlayCursorSound("CookingSuccess");
                }
                else {
                    if (Debug.isDebugBuild) Debug.Log("Stairs condition is wrong.");
                }
            }
        }
    }

    public static void CheckForNightmarePrinceDeath(Monster mon)
    {
        if (mon.myStats.CheckHasStatusName("status_shadowking") && mon.isItemBoss && !ItemDreamFunctions.IsItemDreamNightmare())
        {
            GameMasterScript.heroPCActor.SetActorData("killed_nightmareprince", 1);
            Monster memoryKing = null;
            Item itemWorldItem = MapMasterScript.itemWorldItem;

            if (itemWorldItem == null)
            {
                MapMasterScript.itemWorldItem = LootGeneratorScript.GenerateLootFromTable(mon.challengeValue, 0f, "legendary");
                itemWorldItem = MapMasterScript.itemWorldItem;
                Debug.Log("Spawned " + MapMasterScript.itemWorldItem.actorRefName);
                MapMasterScript.itemWorldOpen = true;
            }

            mon.myInventory.AddItemRemoveFromPrevCollection(itemWorldItem, false);
            
            Consumable orbUsed = MapMasterScript.orbUsedToOpenItemWorld as Consumable;
            string checkMod = "";
            if (orbUsed != null)
            {
                checkMod = orbUsed.GetOrbMagicModRef();
                mon.myInventory.AddItemRemoveFromPrevCollection(orbUsed, true);
            }

            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                Map iwMap = MapMasterScript.itemWorldMaps[i];
                foreach (Monster dreamMonster in iwMap.monstersInMap)
                {
                    if (dreamMonster.isItemBoss)
                    {
                        memoryKing = dreamMonster;
                        break;
                    }
                }
                if (memoryKing != null && memoryKing != mon)
                {
                    iwMap.RemoveActorFromMap(memoryKing);
                    if (itemWorldItem != null)
                    {
                        memoryKing.myInventory.RemoveItem(itemWorldItem);
                    }                    
                    break;
                }
            }
        }
    }

    public static void CheckForPlayerBenefitsOnMonsterDeath(Monster mon)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        if (mon.actorfaction == Faction.ENEMY)
        {
            if (MapMasterScript.activeMap.IsItemWorld() && mon.actorRefName == "mon_goldfrog")
            {
                heroPCActor.AddActorData("dream_gilfrogs", 1);
            }
        }
        else if (mon.actorfaction == Faction.PLAYER)
        {
            if (mon.summoner == heroPCActor)
            {
                if (heroPCActor.myStats.CheckHasStatusName("status_compost"))
                {
                    if (mon.actorRefName.Contains("livingvine"))
                    {
                        float healAmount = (heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.15f);
                        heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, healAmount, StatDataTypes.CUR, true);
                        StringManager.SetTag(0, ((int)healAmount).ToString());
                        BattleTextManager.NewDamageText((int)healAmount, true, Color.green, heroPCActor.GetObject(), 0f, 1f);
                        GameLogScript.LogWriteStringRef("log_flora_compost");
                    }
                }       
                if (heroPCActor.myStats.CheckHasStatusName("exp_status_necessarysacrifice"))
                {
                    if (mon.myStats.CheckHasStatusName("exp_status_dominated"))
                    {
                        float energy = heroPCActor.myStats.GetMaxStat(StatTypes.ENERGY) * 0.1f;
                        float stamina = heroPCActor.myStats.GetMaxStat(StatTypes.STAMINA) * 0.1f;
                        heroPCActor.myStats.ChangeStat(StatTypes.ENERGY, energy, StatDataTypes.CUR, true);
                        heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, energy, StatDataTypes.CUR, true);
                        ChangeCoreStatPackage ccsp = GameLogDataPackages.GetChangeCoreStatPackage();
                        ccsp.effectSource = StringManager.GetString("exp_skill_necessarysacrifice_name");
                        ccsp.abilityUser = mon.displayName;
                        ccsp.gameActor = heroPCActor;
                        ccsp.statChanges[(int)StatTypes.ENERGY] = energy;
                        ccsp.statChanges[(int)StatTypes.STAMINA] = stamina;
                        GameLogScript.CombatEventWrite(ccsp);
                    }
                }         
            }
        }
    }

    // "Mon" has just died. Was this a quest requirement? (success)
    // Or did we need the monster alive for some reason? (failure)
    public static void CheckForQuestSuccessOrFailure(Monster mon)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        if (heroPCActor.myQuests.Count > 0)
        {
            QuestScript qToRemove = null;
            QuestScript[] qToCheck = new QuestScript[heroPCActor.myQuests.Count];
            heroPCActor.myQuests.CopyTo(qToCheck);

            for (int i = 0; i < qToCheck.Length; i++)
            {
                QuestScript qs = qToCheck[i];
                if (qs == null || qs.complete) continue;
                if (qs.qType == QuestType.KILLCHAMPION)
                {
                    if (qs.targetMonster == mon)
                    {
                        QuestScript.CompleteQuest(qs);
                        //qToRemove = qs;
                        break;
                    }
                }
                else if (qs.qType == QuestType.DREAMWEAPON_BOSS)
                {
                    if (qs.targetMonster == mon || qs.targetActor == mon)
                    {
                        QuestScript.CompleteQuest(qs);
                        break;
                    }
                }
                else if (qs.qType == QuestType.BOSSGANG)
                {
                    if (qs.targetMonster == mon)
                    {
                        QuestScript.CompleteQuest(qs);
                        //qToRemove = qs;
                        break;
                    }
                }
                else if (qs.qType == QuestType.APPEASEMONSTER || qs.qType == QuestType.TAMEMONSTER)
                {
                    if (qs.targetMonster == mon && !qs.complete)
                    {
                        QuestScript.HeroFailedQuest(qs);
                        break;
                    }
                }
            }
            if (qToRemove != null)
            {
                //heroPCActor.myQuests.Remove(qToRemove);
            }
        }
        if (heroPCActor.myQuests.Count > 0)
        {
            QuestScript qToRemove = null;
            foreach (QuestScript qs in heroPCActor.myQuests)
            {
                if (qs.qType == QuestType.KILLMONSTERELEMENTAL)
                {                    
                    if (qs.targetRef == mon.actorRefName && mon.killedByDamage == qs.damType)
                    {
                        qs.numTargetsRemaining--;
                        if (qs.numTargetsRemaining <= 0)
                        {
                            QuestScript.CompleteQuest(qs);
                            qToRemove = qs;
                            break;
                        }
                    }
                }
            }
            if (qToRemove != null)
            {
                heroPCActor.myQuests.Remove(qToRemove);
            }
        }
    }

    public static void CheckForMonsterDeathEventTriggers(Monster mon, Map processMap)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        UIManagerScript uims = UIManagerScript.singletonUIMS;
        MusicManagerScript musicManager = GameMasterScript.musicManager;

        if (mon.actorRefName == "mon_scientist_device" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            GameEventsAndTriggers.SharaDestroysScientistDevice(mon);
        }
        else if (mon.actorRefName == "mon_scientist_summoner" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            GameEventsAndTriggers.SharaDefeatsBoss2Scientist(mon);
        }
        else if (mon.actorRefName == "mon_dimriftboss" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            GameEventsAndTriggers.BeatDimensionalRift();
        }
        else if (mon.actorRefName == "mon_sideareaboss1" && mon.IsTrueEnemyNotEchoOrIllusion() && MapMasterScript.activeMap.floor != MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            GameEventsAndTriggers.ClearFinalSideArea1();
        }
        else if (mon.actorRefName == "mon_xp_heavygolem" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            GameEventsAndTriggers.CheckForSharaBoss3Victory(mon);
        }

        if (mon.ReadActorData("friendship_boss") == 1 && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            GameEventsAndTriggers.ClearFriendshipForest();
        }
        if (mon.actorRefName == "mon_sideareaboss2" && mon.IsTrueEnemyNotEchoOrIllusion() && MapMasterScript.activeMap.floor != MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            GameEventsAndTriggers.ClearFinalSideArea2();
        }        
        else if (mon.actorRefName == "mon_shara_finalboss" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            if (SharaModeStuff.IsSharaModeActive())
            {
                GameMasterScript.gmsSingleton.SetTempFloatData("sharabossposx", mon.GetPos().x);
                GameMasterScript.gmsSingleton.SetTempFloatData("sharabossposy", mon.GetPos().y);
                GameEventsAndTriggers.SharaBoss4Victory();
            }
        }
        else if (mon.actorRefName == "mon_sideareaboss4" && mon.IsTrueEnemyNotEchoOrIllusion() && MapMasterScript.activeMap.floor != MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            GameEventsAndTriggers.ClearFinalSideArea4();
        }
        else if (mon.actorRefName == "mon_ancientsteamgolem" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            foreach (Stairs st in processMap.mapStairs)
            {
                st.EnableActor();
                st.myMovable.SetInSightAndSnapEnable(true);
            }

            List<Monster> removeActor = new List<Monster>();
            foreach(Monster m in processMap.monstersInMap)
            {
                if (m != mon && m.actorfaction == Faction.ENEMY)
                {
                    m.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.ALL, true);
                    m.whoKilledMe = GameMasterScript.heroPCActor;
                    GameMasterScript.AddToDeadQueue(m);
                }
            }

            if (PlatformVariables.SEND_UNITY_ANALYTICS)
            {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                Analytics.CustomEvent("boss_defeated", new Dictionary<string, object>()
                {
                    { "boss", "quadriped" },
                    { "plvl", GameMasterScript.heroPCActor.myStats.GetLevel() },
                    { "hbslotsused", GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed() },
                        { "job", GameMasterScript.heroPCActor.myJob.jobEnum.ToString() }
                });
#endif
            }

            GameMasterScript.gmsSingleton.ProcessDeadQueue(processMap);
            
            ProgressTracker.SetProgress(TDProgress.BOSS3, ProgressLocations.HERO, 2);
                        
            BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

			MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory", true);
			

            GameMasterScript.heroPCActor.SetActorData("viewfloor" + processMap.floor.ToString(), 999);

            GameMasterScript.gmsSingleton.statsAndAchievements.Boss3Defeated();

            
        }

        if (mon.actorRefName == "mon_dirtbeak_library" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            Conversation victory = GameMasterScript.FindConversation("dirtbeak_library_defeat");
            UIManagerScript.StartConversation(victory, DialogType.KEYSTORY, null);

            MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory", true);

        }

        if (mon.actorRefName == "mon_banditwarlord" && mon.IsTrueEnemyNotEchoOrIllusion() && ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) != 3)
        {
            GameEventsAndTriggers.RemoveTrapsFromBoss1Map();
            foreach (Stairs st in processMap.mapStairs)
            {
                st.EnableActor();
                st.myMovable.SetInSightAndSnapEnable(true);
            }

if (PlatformVariables.SEND_UNITY_ANALYTICS)
{
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                Analytics.CustomEvent("boss_defeated", new Dictionary<string, object>()
            {
                { "boss", "dirtbeak" },
                { "plvl", GameMasterScript.heroPCActor.myStats.GetLevel() },
                { "hbslotsused", GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed() },
                    { "job", GameMasterScript.heroPCActor.myJob.jobEnum.ToString() }
            });
#endif
            }

            ProgressTracker.SetProgress(TDProgress.BOSS1, ProgressLocations.HERO, 3);

            // Room to breathe after beating the boss...            
            BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

            if (!GameEventsAndTriggers.ShouldCutscenesBeSkipped())
            {
                GameMasterScript.SetAnimationPlaying(true, true);
                Conversation victory = GameMasterScript.FindConversation("first_boss_defeat");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(victory, DialogType.KEYSTORY, null, 1.5f));
                MapMasterScript.ClearActorsByRefFromMap(MapMasterScript.activeMap, new List<string>() { "dirtbeak_bottleneck_explainer" });                
            }

            SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.RIVERSTONE_WATERWAY);

            GameMasterScript.gmsSingleton.statsAndAchievements.Boss1Defeated();

            // Clear out the triggers related to widening the doorway            

            MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory", true);

            GameMasterScript.heroPCActor.SetActorData("viewfloor" + processMap.floor.ToString(), 999);

            // how to treat ancientcube for speedrun mode?
            if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) < 1)
            {
                if (!GameEventsAndTriggers.ShouldCutscenesBeSkipped())
                {
                    MapTileData mtd = MapMasterScript.GetRandomEmptyTile(mon.GetPos(), 1, true, true);
                    Destructible dt = processMap.CreateDestructibleInTile(mtd, "obj_techcube");
                    MapMasterScript.singletonMMS.SpawnDestructible(dt);
                    dt.myMovable.SetInSightAndSnapEnable(true);
                }
                else
                {
                    ProgressTracker.SetProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META, 2);
                }
                TDPlayerPrefs.SetInt(GlobalProgressKeys.BEAT_FIRST_BOSS, 1);
            }
        }


        if (processMap.dungeonLevelData.specialRoomTemplate != null && processMap.dungeonLevelData.specialRoomTemplate.refName == "boss2")
        {
            GameEventsAndTriggers.CheckForBoss2Clear();
        }

        //Defeat final boss phase 2 and win the game
        //if (mon.actorRefName == "mon_finalboss" || mon.actorRefName == "mon_finalboss2")
        if (mon.actorRefName == "mon_finalboss2" && mon.IsTrueEnemyNotEchoOrIllusion())
        {
            foreach (Stairs st in processMap.mapStairs)
            {
                st.EnableActor();
                st.myMovable.SetInSightAndSnapEnable(true);
            }

            GameMasterScript.heroPCActor.SetActorData("finalboss2", 2); // indicates boss was beaten

            //Set the required stats and chievos for this monumental accomplishment
            GameMasterScript.gmsSingleton.statsAndAchievements.Boss4Defeated();
            if (GameStartData.NewGamePlus > 0)
            {
                GameMasterScript.gmsSingleton.statsAndAchievements.Boss4Defeated_NG();
            }

            //report home to unity
if (PlatformVariables.SEND_UNITY_ANALYTICS)
{
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                Analytics.CustomEvent("boss_defeated", new Dictionary<string, object>()
            {
                { "boss", "finalboss_2" },
                { "plvl", GameMasterScript.heroPCActor.myStats.GetLevel() },
                { "hbslotsused", GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed() },
                    { "job", GameMasterScript.heroPCActor.myJob.jobEnum.ToString() }
            });
#endif
            }

            //Start the cutscene
            Cutscenes.PostFinalBoss_Part1();

            //Do these things
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act == mon)
                {
                    continue;
                }

                //Add all enemy monsters and powerups and monsterspirits to the DeadQueue, also set monster health to 0.
                if (act.actorfaction != Faction.PLAYER || act.actorRefName.Contains("powerup") || act.actorRefName == "monsterspirit")
                {
                    GameMasterScript.AddToDeadQueue(act);

                    var mn = act as Monster;
                    if (mn != null)
                    {
                        mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.ALL, true);
                    }
                }
            }
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
        }
    }

    public static void AwardXPJPGoldFromMonsterDefeat(Fighter monster)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        Monster mon = monster as Monster;
        float cv = mon.challengeValue;
        float xpMod = mon.xpMod;

        GameMasterScript.gmsSingleton.reusableStringBuilder.Length = 0;

        if (xpMod == 0) return;
        StatBlock heroSB = heroPCActor.myStats;
        // Do the xp calculation here, we'll keep it simple for now but improve on this later.

        // Level 10 monster with xp mod 1.1, cv 1.6
        // 12.65 * 3.68 = 46.5        

        float xpToGive = ((xpMod + mon.myStats.GetLevel()) * 1.25f) * (cv * 2.3f) + UnityEngine.Random.Range(0f, 2f);

        if (mon.isChampion && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            xpToGive *= 1f + (0.15f * mon.championMods.Count);            
        }

        bool extraRewardsFromMonsterStatus = monster.myStats.CheckHasStatusName("extrarewards");
        if (extraRewardsFromMonsterStatus)
        {
            BattleTextManager.NewText(StringManager.GetString("exp_misc_bonus"), heroPCActor.GetObject(), Color.yellow, 0.5f);
        }
        bool extraJPXPBonusFromGear = false;

        if (heroPCActor.myStats.CheckHasStatusName("status_extrajpxp"))
        {
            extraJPXPBonusFromGear = true;
        }
        if (extraJPXPBonusFromGear) xpToGive *= 1.2f;
        if (extraRewardsFromMonsterStatus) xpToGive *= 1.35f;

        int levelDiff = heroPCActor.myStats.GetLevel() - mon.myStats.GetLevel();

        float levelDiffMod = 1.0f;

        if (levelDiff < 0)
        {
            levelDiffMod += (-0.05f * levelDiff);
        }
        else
        {
            levelDiffMod = BalanceData.playerMonsterRewardTable[heroPCActor.myStats.GetLevel(), levelDiff];
        }

        if (heroPCActor.myStats.CheckHasStatusName("status_blessxp"))
        {
            xpToGive *= 1.15f;
            xpToGive += 1;
        }

        xpToGive *= levelDiffMod;

        if (MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            if (levelDiffMod >= 1f)
            {
                // Harder monsters in Mystery Dungeons should give *even more* XP
                xpToGive *= levelDiffMod;
            }
            xpToGive *= MysteryDungeonManager.BONUS_MD_XP_MULTIPLIER;
        }


        // Flat penalty/bonus based on monster level.
        //xpToGive += (mon.myStats.GetLevel() - heroSB.GetLevel()) * 2f;

        if (MapMasterScript.GetItemWorldAura(monster.GetPos()) == (int)ItemWorldAuras.BONUSXP)
        {
            xpToGive *= 1.5f;
        }

        xpToGive *= (1.0f + MapMasterScript.activeMap.bonusRewards);

        if (xpToGive < 0)
        {
            xpToGive = 0;
        }


        if (MapMasterScript.activeMap.IsJobTrialFloor())
        {
            xpToGive = 0;
        }

        // Base money calc?
        float moneyToGive = UnityEngine.Random.Range(16f, 21f);
        moneyToGive += mon.myStats.GetLevel() * 1.5f;
        moneyToGive *= levelDiffMod;

        // Flat penalty/bonus based on monster level.
        moneyToGive += (mon.myStats.GetLevel() - heroSB.GetLevel()) * 2f;

        if (heroPCActor.myStats.CheckHasStatusName("status_entrepreneur")) moneyToGive *= 1.25f;
        if (extraRewardsFromMonsterStatus) moneyToGive *= 1.35f;

        moneyToGive += (moneyToGive * heroPCActor.advStats[(int)AdventureStats.GOLDFIND]);

        // Pandora's box "bonus" heh heh heh
        moneyToGive += (moneyToGive * heroPCActor.numPandoraBoxesOpened * GameMasterScript.PANDORA_BONUS_MONEY);

        if (!MapMasterScript.activeMap.spawnerAlive && !MapMasterScript.activeMap.dungeonLevelData.noSpawner)
        {
            moneyToGive += (moneyToGive * 0.04f);
        }

        if (monster.CheckFlag(ActorFlags.STARTCOMBATLUCKY))
        {
            int luckyQuant = heroPCActor.myStats.CheckStatusQuantity("status_mmlucky");
            if (luckyQuant > 0)
            {
                if (UnityEngine.Random.Range(0, 1f) <= (luckyQuant * 0.15f))
                {
                    moneyToGive += (int)UnityEngine.Random.Range(2, 6);
                }
            }
        }

        moneyToGive *= xpMod;

        //moneyToGive += (heroPCActor.advStats[(int)AdventureStats.GOLDFIND] * moneyToGive);

        moneyToGive *= (1.0f + MapMasterScript.activeMap.bonusRewards);

        if (GameMasterScript.gmsSingleton.adventureModeActive && moneyToGive < 8f)
        {
            moneyToGive = UnityEngine.Random.Range(7f, 11f);
        }

        if (MapMasterScript.GetItemWorldAura(monster.GetPos()) == (int)ItemWorldAuras.BONUSGOLD)
        {
            moneyToGive *= 1.5f;
        }

        int money = (int)moneyToGive;
        if (money < 0) money = 0;

        if (GameStartData.CheckGameModifier(GameModifiers.NO_GOLD_DROPS))
        {
            money = 0;
        }

        if (MapMasterScript.activeMap.IsJobTrialFloor())
        {
            money = 0;
        }

        float jpToGive = UnityEngine.Random.Range(12f, 16f);
        if (heroPCActor.myStats.CheckHasStatusName("status_fastlearner"))
        {
            jpToGive += UnityEngine.Random.Range(3, 6f);
        }

        jpToGive *= levelDiffMod;

        if (extraJPXPBonusFromGear) jpToGive *= 1.2f;
        if (extraRewardsFromMonsterStatus) jpToGive *= 1.35f;

        jpToGive = Mathf.Clamp(jpToGive, 1f, 25f);

        jpToGive *= xpMod;

        float conditionalMult = 1f;
        if (xpMod < 1f)
        {
            conditionalMult = xpMod;
        }

        if (mon.isBoss && mon.myTemplate.showBossHealthBar)
        {
            jpToGive += (150f * conditionalMult);
        }
        else if (mon.isChampion)
        {
            jpToGive += (10f * conditionalMult);
        }
        if (mon.ReadActorData("elementalking") >= 0)
        {
            jpToGive += (50f * conditionalMult);
        }

        // Flat penalty/bonus based on monster level.

        if (MapMasterScript.GetItemWorldAura(monster.GetPos()) == (int)ItemWorldAuras.BONUSJP)
        {
            jpToGive *= 1.5f;
        }

        jpToGive *= (1.0f + MapMasterScript.activeMap.bonusRewards);

        if (jpToGive < 0)
        {
            jpToGive = 0;
        }

        jpToGive = Mathf.Round(jpToGive);

        jpToGive = heroPCActor.ProcessJPGain(jpToGive);

        int convertToMoney = 0;
        if (jpToGive > 0)
        {
            if (heroPCActor.myStats.CheckHasStatusName("status_jptogold"))
            {
                float localJPToGive = jpToGive;
                convertToMoney = (int)localJPToGive / 2;
                money += convertToMoney;
                jpToGive = 0;
            }
        }

        if (MapMasterScript.activeMap.IsJobTrialFloor())
        {
            jpToGive = 0;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            xpToGive = 0;
        }

        heroPCActor.AddJP((int)jpToGive);
        money = heroPCActor.ChangeMoney(money);

        string deadString = null;
        if (SharaModeStuff.IsSharaModeActive())
        {
            StringManager.SetTag(0, monster.displayName);
            StringManager.SetTag(1, ((int)jpToGive).ToString());
            StringManager.SetTag(2, ((int)money).ToString());

            deadString = StringManager.GetString("log_monster_died_shara");

        }
        else
        {
            StringManager.SetTag(0, monster.displayName);
            StringManager.SetTag(1, ((int)xpToGive).ToString());
            StringManager.SetTag(2, ((int)jpToGive).ToString());
            StringManager.SetTag(3, ((int)money).ToString());

            deadString = StringManager.GetString("log_monster_died");
        }

        GameLogScript.EnqueueEndOfTurnLogMessage(deadString);

        money = heroPCActor.ChangeMoney(money);
        GameMasterScript.gmsSingleton.reusableStringBuilder.Append(((int)xpToGive).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.XP) + " " + ((int)jpToGive).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP));

        bool canGainExperience = true;

        if (SharaModeStuff.IsSharaModeActive())
        {
            canGainExperience = false;
            GameMasterScript.gmsSingleton.reusableStringBuilder.Length = 0;
            GameMasterScript.gmsSingleton.reusableStringBuilder.Append(((int)jpToGive).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP));
        }

        if (PlayerOptions.battleJPXPGain)
        {
            BattleTextManager.NewText(GameMasterScript.gmsSingleton.reusableStringBuilder.ToString(), heroPCActor.GetObject(), Color.yellow, 0.33f);
        }

        if (canGainExperience)
        {
            bool levelUp = heroSB.ChangeExperience(((int)xpToGive));
            if (levelUp)
            {
                // Level up code for the hero here, raise stats etc.
                heroSB.LevelUp();
            }
        }

        //UIManagerScript.RefreshPlayerStats();
        GameMasterScript.playerStatsChangedThisTurn = true;

        if (extraRewardsFromMonsterStatus)
        {
            GameMasterScript.heroPCActor.myStats.RemoveAllStatusByRef("extrarewards");
        }
    }

    /// <summary>
    /// Returns TRUE if we should destroy creature.
    /// </summary>
    /// <param name="mon"></param>
    /// <returns></returns>
    static bool CheckForPlayerCorralDeathEvents(Monster mon, out bool displayDeathMessage)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        bool destroyCreature = true;

        displayDeathMessage = true;

        if (mon.actorUniqueID == heroPCActor.GetMonsterPetID())
        {
            bool insurance = heroPCActor.ReadActorData("petinsurance") == 1;

            // As of 5/4, pets always return.
            //if (insurance || GameStartData.CheckGameModifier(GameModifiers.PETS_DONTDIE))
            {
                heroPCActor.RemoveActorData("petinsurance");
                mon.destroyed = false;
                if (insurance)
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(mon.GetPos(), "TeleportUp", null, true);
                }
                else
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(mon.GetPos(), "SmokePoof", null, true);
                }

                mon.myStats.HealToFull();
                mon.myStats.RemoveTemporaryNegativeStatusEffects();
                GameMasterScript.RemoveActorFromDeadQueue(mon);
                mon.destroyed = false;
                mon.myStats.ForciblyRemoveStatus("status_preexplode"); // This is a "permanent" status but it's just visual, and it doesn't come off normally
                MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();
                displayDeathMessage = false;
                StringManager.SetTag(0, mon.displayName);
                destroyCreature = false;

                if (insurance)
                {
                    GameLogScript.LogWriteStringRef("log_petdied_insured");
                    BattleTextManager.NewText(StringManager.GetExcitedString("misc_rescue"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.1f);
                }
                else
                {
                    // Pet ran away
                    if (!GameStartData.CheckGameModifier(GameModifiers.PETS_DONTDIE))
                    {
                        mon.tamedMonsterStuff.ChangeHappiness(-99);
                        mon.SetActorData("day_at_unisured_death", MetaProgressScript.totalDaysPassed);
                    }
                    GameLogScript.LogWriteStringRef("log_petdied_safemode");
                    BattleTextManager.NewText(StringManager.GetExcitedString("misc_petran"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.1f);
                }
                GameMasterScript.gmsSingleton.SetTempGameData("petrescue_thisturn", mon.actorUniqueID);
            }

            // Make sure we are totally getting rid of it...
            heroPCActor.ResetPetData();
            heroPCActor.RemoveAlly(mon);
            heroPCActor.RemoveSummon(mon);
            heroPCActor.RemoveAnchor(mon);

            //if (!insurance) Debug.Log(mon.actorRefName + " " + mon.actorUniqueID + " died for real.");                            
        }

        return destroyCreature;
    }

    static void CheckForLivingVineSpecialCases(Monster mon)
    {
        if (mon.actorfaction == Faction.PLAYER && GameMasterScript.heroPCActor.summonedActors.Contains(mon) && mon.actorRefName.Contains("livingvine"))
        {
            List<AbilityScript> summonsToReset = new List<AbilityScript>();
            AbilityScript summon = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_summonlivingvine");
            if (summon != null) summonsToReset.Add(summon);
            summon = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_summonlivingvine_2");
            if (summon != null) summonsToReset.Add(summon);
            foreach (AbilityScript abil in summonsToReset)
            {
                abil.ResetCooldown();
            }
            UIManagerScript.singletonUIMS.RefreshAbilityCooldowns();
        }
    }
}
