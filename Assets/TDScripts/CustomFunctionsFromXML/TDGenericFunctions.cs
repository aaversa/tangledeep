using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class TDGenericFunctions
{
    public static Dictionary<string, Action<string[]>> dictDelegates;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        dictDelegates = new Dictionary<string, Action<string[]>>();

        initialized = true;
    }

    public static void CacheScript(string scriptName)
    {
        if (!initialized) Initialize();        

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo myMethod = typeof(TDGenericFunctions).GetMethod(scriptName, new Type[] { typeof(string[]) });

        Action<string[]> converted = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), myMethod);

        dictDelegates.Add(scriptName, converted);

    }

    public static List<Actor> genericPooledList = new List<Actor>(25);
    public static List<string> possibleRandomChestRefs = new List<string>()
    {
            "obj_largewoodencrate",
            "obj_smallwoodenchest",
            "obj_largewoodenchest",
            "obj_ornatechest"
    };

    public static void CheckBoss2ClearOnTurnEnd(string[] args)
    {
        if (GameMasterScript.turnNumber % 3 != 0) return;

        GameEventsAndTriggers.CheckForBoss2Clear();
    }

    /// <summary>
    /// Runs in beastlake to see if we can unlock beast dungeon
    /// </summary>
    /// <param name="args"></param>
    public static void EvaluateBeastDragonQuestLine(string[] args)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return;

        // have not unlocked the dragon stuff yet
        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) < 1)
        {
            return;
        }

        // already revealed beast dungeon
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_BEAST_DUNGEON, ProgressLocations.HERO) >= 1)
        {
            return;
        }

        if (UIManagerScript.dialogBoxOpen)
        {
            return;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        if (MapMasterScript.activeMap.IsItemWorld())
        {
            return;
        }

        // Only spawn the quest once we clear the map of enemies
        TyrantDragonStuff.DoBeastDragonUnlockIntro();
        ProgressTracker.SetProgress(TDProgress.DRAGON_BEAST_DUNGEON, ProgressLocations.BOTH, 1);
    }

    /// <summary>
    /// Runs in boss2 room to see if we can unlock bandit dragon.
    /// </summary>
    /// <param name="args"></param>
    public static void EvaluateBanditDragonQuestLine(string[] args)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return;

        // have not unlocked the dragon stuff yet
        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) < 1)
        {
            return;
        }

        // already revealed bandit dungeon
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_BANDIT_DUNGEON, ProgressLocations.HERO) >= 1)
        {
            return;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        // Only spawn the quest once we clear the map of enemies
        if (!MapMasterScript.activeMap.IsItemWorld() && (MapMasterScript.activeMap.unfriendlyMonsterCount == 0 || MapMasterScript.activeMap.clearedMap))
        {
            BanditDragonStuff.DoBanditDungeonUnlockIntro();
            ProgressTracker.SetProgress(TDProgress.DRAGON_BANDIT_DUNGEON, ProgressLocations.BOTH, 1);
        }
    }

    /// <summary>
    /// Args is just the id of the final boss, who will peace out for a bit.
    /// </summary>
    /// <param name="args"></param>
    public static void SharaBoss4Phase2Begin(string[] args)
    {
        if (GameMasterScript.playerDied) return;

        GameMasterScript.SetAnimationPlaying(true, true);
        int idOfBoss = Int32.Parse(args[0]);
        Actor bossMon = GameMasterScript.gmsSingleton.TryLinkActorFromDict(idOfBoss);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(bossMon.GetPos(), 1.3f, false);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameMasterScript.gmsSingleton.StartCoroutine(SharaBoss4Phase2Begin_Part2(bossMon));
    }

    /// <summary>
    /// Camera has panned over to the boss
    /// </summary>
    /// <param name="bossMon"></param>
    /// <returns></returns>
    static IEnumerator SharaBoss4Phase2Begin_Part2(Actor bossMon)
    {
        float explodeLength = 2f;
        int numExplosions = 14;
        float durPerExplosion = explodeLength / numExplosions;

        bossMon.myAnimatable.SetAnim("TakeDamage");

        // Robot takes some explosion damage
        for (int i = 0; i < numExplosions; i++)
        {
            Vector2 explodePos = bossMon.GetPos();
            explodePos.x += UnityEngine.Random.Range(-0.45f, 0.45f);
            explodePos.y += UnityEngine.Random.Range(-0.2f, 0.45f);
            CombatManagerScript.GenerateSpecificEffectAnimation(explodePos, "SmallExplosionEffect", null, true);
            yield return new WaitForSeconds(durPerExplosion);
            if (i % 3 == 0)
            {
                bossMon.myAnimatable.SetAnim("TakeDamage");
            }
        }

        yield return new WaitForSeconds(1f);

        GameMasterScript.gmsSingleton.SetTempGameData("sharafinalbossid", bossMon.actorUniqueID);

        GameMasterScript.heroPCActor.SetActorData("finalbossphase2_turns", SharaModeStuff.FINALBOSS_PHASE_2_TURNS);

        StringManager.SetTag(0, SharaModeStuff.FINALBOSS_PHASE_2_TURNS.ToString());
        UIManagerScript.StartConversationByRef("dialog_sharafinalboss_phase2begin", DialogType.KEYSTORY, null);
    }

    // Runs in Frog Bog to unlock the Frog Dungeon, if possible.
    public static void EvaluateFrogDragonQuestLine(string[] args)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return;

        // "1" means the quest has been introduced by Langdon, but we haven't done anything else yet.
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META) < 1)
        {
            return;
        }

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.HERO) == 1)
        {
            return;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        // Only spawn the quest once we clear the map of Frogz.
        if (!MapMasterScript.activeMap.IsItemWorld() && (MapMasterScript.activeMap.unfriendlyMonsterCount == 0 || MapMasterScript.activeMap.clearedMap))
        {
            FrogDragonStuff.DoFrogBogUnlockIntro();
            ProgressTracker.SetProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.HERO, 1);
        }
    }

    public static void BeastDungeonIntro(string[] args)
    {
        return; 
        // dont need this story stuff for now.
        int introTurns = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("introturns");
        int turnsLeft = GameMasterScript.heroPCActor.ReadActorData("beastdungeonintroturns");
        if (turnsLeft == 0)
        {
            return;
        }
        if (turnsLeft < 0)
        {
            turnsLeft = introTurns;
        }
        turnsLeft--;
        if (turnsLeft == 0)
        {
            UIManagerScript.StartConversationByRef("beastdungeon_intro", DialogType.STANDARD, null);
        }
        GameMasterScript.heroPCActor.SetActorData("beastdungeonintroturns", turnsLeft);
    }

    /// <summary>
    /// For shara's final boss phase 2
    /// </summary>
    /// <param name="args"></param>
    public static void ProcessRobotWaves(string[] args)
    {
        int robotWaveTurnsRemaining = GameMasterScript.heroPCActor.ReadActorData("finalbossphase2_turns");
        
        if (robotWaveTurnsRemaining < 0)
        {
            // haven't started yet 
            return;
        }
        if (robotWaveTurnsRemaining == 0)
        {
            // bring the Core Guardian back
            GameMasterScript.gmsSingleton.StartCoroutine(GameEventsAndTriggers.SharaBoss4Phase2Ends());
            GameMasterScript.heroPCActor.RemoveActorData("finalbossphase2_turns");
            return;
        }

        // Consider spawning the next wave.

        robotWaveTurnsRemaining--;
        GameMasterScript.heroPCActor.SetActorData("finalbossphase2_turns", robotWaveTurnsRemaining);

        if (robotWaveTurnsRemaining % 10 != 0 && robotWaveTurnsRemaining != 49)
        {
            return;
        }

        int numMonstersPerWave = 5;

        List<Actor> monstersSpawnedThisWave = new List<Actor>();

        for (int i = 0; i < numMonstersPerWave; i++)
        {
            string monsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();
            Monster newMon = MonsterManagerScript.CreateMonster(monsterRef, false, true, false, 0f, false);

            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, newMon, true);
            
            //newMon.allMitigationAddPercent = 1.2f; // make them a little more vulnerable
            newMon.allDamageMultiplier = 0.8f; // make them deal a little less damage
            //newMon.myStats.SetStat(StatTypes.HEALTH, newMon.myStats.GetMaxStat(StatTypes.HEALTH) * 0.8f, StatDataTypes.ALL, true); // reduce max hp
            newMon.myStats.SetStat(StatTypes.CHARGETIME, 80f, StatDataTypes.ALL, true); // dont move them quite as often
            newMon.aggroRange = 15;
            newMon.AddAggro(GameMasterScript.heroPCActor, 200f);
            newMon.actorfaction = Faction.ENEMY;
            newMon.bufferedFaction = Faction.ENEMY;
            MapTileData spawnTile = null;

            int attempts = 0;
            while (spawnTile == null)
            {
                bool valid = true;
                attempts++;
                int x = UnityEngine.Random.Range(1, MapMasterScript.activeMap.columns - 1);
                int y = UnityEngine.Random.Range(1, MapMasterScript.activeMap.rows - 1);
                Vector2 checkPos = new Vector2(x, y);
                MapTileData mtd = MapMasterScript.GetTile(checkPos);
                if (mtd.IsCollidable(newMon) || mtd.tileType != TileTypes.GROUND)
                {
                    continue;
                }
                if (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), checkPos) >= 6)
                {
                    foreach(Actor act in monstersSpawnedThisWave)
                    {
                        if (MapMasterScript.GetGridDistance(act.GetPos(), checkPos) <= 4 && attempts < 1000)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                else
                {
                    valid = false;
                }

                if (valid)
                {
                    spawnTile = MapMasterScript.GetTile(checkPos); 
                }
            }

            MapMasterScript.activeMap.PlaceActor(newMon, spawnTile);
            MapMasterScript.singletonMMS.SpawnMonster(newMon, true);
            CombatManagerScript.GenerateSpecificEffectAnimation(newMon.GetPos(), "TeleportDown", null, true);
        }
    }


    public static void ProcessMonsterHorde(string[] args)
    {                
        if (MapMasterScript.activeMap.floor < MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR || MapMasterScript.activeMap.floor > MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR)
        {
            return;
        }

        string hordeCheck = "horde_" + MapMasterScript.activeMap.floor;

        bool hordeLevelBeaten = GameMasterScript.heroPCActor.ReadActorData("hordeclear_" + MapMasterScript.activeMap.floor) == 1;

        bool noHordeWavesRemaining = GameMasterScript.heroPCActor.ReadActorData(hordeCheck + "_waves") == 0;

        bool dragonBossFloor = MapMasterScript.activeMap.floor == MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR;
        bool dragonBossAlive = false;

        // Did all the crystals die? If so, boot the player out - level failed.
        bool anyCrystalsAlive = false;
        if (dragonBossFloor)
        {
            // for beast dragon boss, there are no crystals, so they are always alive
            anyCrystalsAlive = true;
            foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorfaction == Faction.ENEMY && m.myStats.IsAlive() && m.actorRefName == "mon_beastdragon")
                {
                    dragonBossAlive = true;
                    break;
                }
            }
        }
        else
        {
            foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorfaction != Faction.PLAYER) continue;
                if (m.actorRefName == "mon_xp_defensecrystal")
                {
                    if (m.myStats.IsAlive())
                    {
                        anyCrystalsAlive = true;
                        break;
                    }
                }
            }
        }

        if (!anyCrystalsAlive && !dragonBossFloor && !hordeLevelBeaten)
        {
            GameMasterScript.gmsSingleton.StartCoroutine(DLCCutscenes.FailedMonsterHordeLevel());
            return;
        }

        if (dragonBossFloor && !dragonBossAlive)
        {
            //GameMasterScript.heroPCActor.SetActorData("hordeclear_" + MapMasterScript.activeMap.floor, 1);
            return;
        }

        if (noHordeWavesRemaining && !hordeLevelBeaten && MapMasterScript.activeMap.floor != MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR)
        {            
            // Check if we beat the level.
            bool anyEnemies = false;
            foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorfaction == Faction.ENEMY)
                {
                    anyEnemies = true;
                    break;
                }
            }
            if (!anyEnemies)
            {
                GameMasterScript.gmsSingleton.StartCoroutine(DLCCutscenes.BeatMonsterHordeLevel());
            }
            return;
        }

        if (!dragonBossFloor && hordeLevelBeaten)
        {
            return;
        }

        int hordeInterval = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("hordeinterval");


        int turnsToHorde = GameMasterScript.heroPCActor.ReadActorData(hordeCheck);
        if (turnsToHorde < 0)
        {
            // countdown to prepare
            turnsToHorde = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("turnstostart");
        }
        turnsToHorde--;

        if (turnsToHorde <= 0)
        {
            turnsToHorde = hordeInterval;
            // Spawn monsters here.

            int monSealChance = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("monsealchance");

            int maxWaves = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("hordewaves");
            int hordeWavesRemaining = GameMasterScript.heroPCActor.ReadActorData(hordeCheck + "_waves");

            bool firstpopup = false;

            if (hordeWavesRemaining < 0)
            {
                hordeWavesRemaining = maxWaves;

                // first tutorial popup
                if (ProgressTracker.CheckProgress(TDProgress.DRAGON_BEAST_HORDES, ProgressLocations.HERO) <= 0)
                {
                    ProgressTracker.SetProgress(TDProgress.DRAGON_BEAST_HORDES, ProgressLocations.HERO, 1);
                    UIManagerScript.StartConversationByRef("beastdungeon_firstwavestart", DialogType.STANDARD, null);
                    firstpopup = true;
                    GameMasterScript.heroPCActor.SetActorData(hordeCheck, turnsToHorde);
                    GameMasterScript.heroPCActor.SetActorData(hordeCheck + "_waves", hordeWavesRemaining);
                    return;
                }
            }

            hordeWavesRemaining--;

            int waveNumber = (maxWaves - hordeWavesRemaining);

            GameMasterScript.heroPCActor.SetActorData(hordeCheck + "_waves", hordeWavesRemaining);

            StringManager.SetTag(0, waveNumber.ToString());
            GameLogScript.LogWriteStringRef("exp_log_monsterhorde_attack");
            if (!firstpopup)
            {
                BattleTextManager.NewText(StringManager.GetString("exp_popup_wave"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1f);
            }            

            int championsPerWave = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("championwave" + waveNumber);

            for (int i = 0; i < 4; i++)
            {
                string monsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();
                Monster spawned = MonsterManagerScript.CreateMonster(monsterRef, false, false, false, 0f, false);

                Vector2 spawnPos = Vector2.zero;
                MapTileData checkTile = null;
                int x = 0;
                int y = 0;

                bool foundTile = false;

                // figure out spawn position
                while (!foundTile)
                {
                    foundTile = true;                    
                    switch (i)
                    {
                        case 0: // top
                            x = UnityEngine.Random.Range(6, MapMasterScript.activeMap.columns - 6);
                            y = MapMasterScript.activeMap.rows - 2;
                            break;
                        case 1: // right
                            x = MapMasterScript.activeMap.columns - 2;
                            y = UnityEngine.Random.Range(6, MapMasterScript.activeMap.rows - 6);
                            break;
                        case 2: // bottom
                            x = UnityEngine.Random.Range(6, MapMasterScript.activeMap.columns - 6);
                            y = UnityEngine.Random.Range(1,3);
                            break;
                        case 3: // left
                            x = UnityEngine.Random.Range(1,3);
                            y = UnityEngine.Random.Range(6, MapMasterScript.activeMap.rows - 6);
                            break;
                    }
                    checkTile = MapMasterScript.activeMap.mapArray[x, y];
                    if (!checkTile.IsEmpty() || checkTile.tileType != TileTypes.GROUND)
                    {
                        foundTile = false;
                    }
                }

                spawned.xpMod = 0.25f;

                spawned.SetSpawnPos(checkTile.pos);
                MapMasterScript.activeMap.PlaceActor(spawned, checkTile);
                MapMasterScript.singletonMMS.SpawnMonster(spawned);

                MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, spawned, false);
                
                if (MapMasterScript.activeMap.floor == MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR)
                {
                    spawned.AddAggro(GameMasterScript.heroPCActor, 50f);
                    spawned.myBehaviorState = BehaviorState.FIGHT;
                }

                if (championsPerWave > 0)
                {
                    float runningChance = 1f;
                    int numChampMods = 0;
                    while (UnityEngine.Random.Range(0,1f) <= runningChance && numChampMods < MapMasterScript.activeMap.dungeonLevelData.maxChampionMods)
                    {
                        spawned.MakeChampion();
                        runningChance *= 0.5f;
                    }                                        
                }
                championsPerWave--;

                // Many normal monsters are Sealed so we don't clog the game up with tons of abilities
                if (!spawned.isChampion && UnityEngine.Random.Range(0, 101) <= monSealChance)
                {
                    spawned.myStats.AddStatusByRef("status_silentsealed", GameMasterScript.heroPCActor, 999);
                }
            }
         
            
        }

        GameMasterScript.heroPCActor.SetActorData(hordeCheck, turnsToHorde);
    }

    /// <summary>
    /// This is called at the end of every turn, and will update the progress of our ground slimes
    /// </summary>
    /// <param name="args"></param>
    public static void UpdateSlimeDungeon(string[] args)
    {
        var level = MapMasterScript.activeMap as Map_SlimeDungeon;
        if (level != null)
        {
            level.OnEndOfTurn();
        }        
    }

    /// <summary>
    /// Tweak this behavior, as the slime dragon boss fight has multiple stages.
    /// </summary>
    /// <param name="args"></param>
    public static void UpdateSlimeDungeonBossArea(string[] args)
    {
        if (MapMasterScript.activeMap.floor != MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR) return;

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) < 2)
        {
            int maxCountdownToBoss = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("turnstoboss");

            int curTurns = GameMasterScript.heroPCActor.ReadActorData("jellydragon_countdown");
            if (curTurns == -1)
            {
                curTurns = maxCountdownToBoss;
            }

            curTurns--;

            if (curTurns % 5 == 0)
            {
                StringManager.SetTag(0, curTurns.ToString());
                GameLogScript.LogWriteStringRef("log_jellydragon_arrivalturns");
                BattleTextManager.NewText(StringManager.GetString("popup_jellydragon_arriveturns"), GameMasterScript.heroPCActor.GetObject(), Color.red, 2f);
            }

            GameMasterScript.heroPCActor.SetActorData("jellydragon_countdown", curTurns);
            
            if (curTurns == 0)
            {
                if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) < 4)
                {
                    ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO, 2);
                }                
                GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.SlimeDragonArrives());
                return;
            }
        }


        var level = MapMasterScript.activeMap as Map_SlimeDungeon;
        if (level != null)
        {
            level.OnEndOfTurn();
        }
    }

    public static void CheckAndExecuteMonsterTrapsNextToPlayer(string[] args)
    {
        genericPooledList.Clear();

        CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 1, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            MapTileData mtd = CustomAlgorithms.tileBuffer[i];

            MapTileData spawnTile = mtd;
            bool findNewTile = false;
            foreach (Actor act in spawnTile.GetAllActors())
            {
                if (act.IsFighter() || (act.GetActorType() == ActorTypes.DESTRUCTIBLE && act.playerCollidable))
                {
                    findNewTile = true;
                    break;
                }
            }

            Destructible trap = mtd.GetActorRef("hiddenmonstertrap") as Destructible;
            if (trap != null)
            {
                GameMasterScript.AddToDeadQueue(trap, true);

                string actorType = trap.ReadActorDataString("containedactortype");
                Actor actorSpawned = null;

                if (findNewTile)
                {
                    spawnTile = MapMasterScript.activeMap.GetRandomEmptyTile(mtd.pos, 1, true, true, true, true, true);
                }

                if (actorType == "monster")
                {
                    string monsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();
                    Monster monSpawned = MonsterManagerScript.CreateMonster(monsterRef, true, true, false, 0f, false);
                    actorSpawned = monSpawned;
                    if (UnityEngine.Random.Range(0, 1f) <= 0.15f)
                    {
                        if (MapMasterScript.activeMap.championCount < MapMasterScript.activeMap.dungeonLevelData.maxChampions)
                        {
                            monSpawned.MakeChampion();
                        }
                    }
                    MapMasterScript.activeMap.PlaceActor(monSpawned, spawnTile);
                    MapMasterScript.singletonMMS.SpawnMonster(monSpawned, true);
                    CombatManagerScript.GenerateSpecificEffectAnimation(mtd.pos, "SoundEmanation", null, true);

                    MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, monSpawned, false);                
                }
                else
                {
                    // Chest was spawned instead!
                    Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(spawnTile, possibleRandomChestRefs[UnityEngine.Random.Range(0, possibleRandomChestRefs.Count)]);
                    actorSpawned = dt;
                    MapMasterScript.singletonMMS.SpawnDestructible(dt);
                }

                CombatManagerScript.GenerateSpecificEffectAnimation(mtd.pos, "MudExplosion", null, true);

                GameObject monObj = actorSpawned.GetObject();
                GameObject mudObj = mtd.GetActorRef("obj_mudtile").GetObject();
                if (!findNewTile)
                {
                    CombatManagerScript.FireProjectile(mtd.pos, mtd.pos, monObj, 0.5f, false, mudObj, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);                    
                }
                else
                {
                    TDAnimationScripts.JumpActorToTargetPoint(actorSpawned, spawnTile.pos, 0.5f, 360f, true);
                }

                CombatManagerScript.WaitThenGenerateSpecificEffect(spawnTile.pos, "EnterMudSplash", null, 0.5f, true); ;
            }
        }
    }

    public static void RefreshSkillCooldownOnUniqueUse(string[] args)
    {
        // args[0] is the skill ref
        // args[1] is the actor ID that used it

        // Refresh the cooldown of this skill IF it was used THIS turn, but not on the PREVIOUS turn.

        string skillRef = args[0];

        // This does not link up to the hero for some reason.
        /* int actorID = Int32.Parse(args[1]);
        Fighter actorUser = GameMasterScript.gmsSingleton.TryLinkActorFromDict(actorID) as Fighter;
        if (actorUser == null)
        {
            Debug.Log("ERROR: Actor id " + actorID + " not found.");
            return;
        } */

        Fighter actorUser = GameMasterScript.heroPCActor;

        AbilityScript getAbility = actorUser.myAbilities.GetAbilityByRef(skillRef);

        string lookup = "lastfreeuse_" + skillRef;

        int freeUseState = actorUser.ReadActorData(lookup);

        if (freeUseState != 1)
        {
            // must be free!
            actorUser.SetActorData(lookup, 1);
        }
        else
        {
            actorUser.SetActorData(lookup, 0);
            getAbility.SetCurCooldownTurns(0);
        }

        /* 

        int turnOfLatestUsage = actorUser.ReadActorData("lastfreeuse_" + skillRef);
        int timeDifference = GameMasterScript.turnNumber - turnOfLatestUsage;

        // Say we use ability on turn 55, this function is enqueued
        // Check when we *last* used ability
        // If we last used it >= abilityCooldown turns, then refresh cooldown and write to dict.
        // If we used it < abilityCooldown turns, do nothing

        Debug.Log(turnOfLatestUsage + " " + timeDifference + " " + getAbility.maxCooldownTurns);

        if (timeDifference >= getAbility.maxCooldownTurns)
        {
            getAbility.SetCurCooldownTurns(0);
            actorUser.SetActorData("lastfreeuse_" + skillRef, GameMasterScript.turnNumber);
        } */
    }
}
