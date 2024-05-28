using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using Random = System.Random;

public class MonsterBehaviorScript {

    public static Dictionary<string, Func<Monster, MonsterTurnData>> dictDelegates;
    public static bool initialized;
    public static List<AggroData> adToRemove;

    public static MonsterTurnData GetAggroFromPlayer(Monster actor)
    {
        actor.AddAggro(GameMasterScript.heroPCActor, 25f);
        actor.myBehaviorState = BehaviorState.FIGHT;

        // If we end up not doing anything special, return default.
        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData ExampleMonsterBehavior(Monster actor)
    {
        MonsterTurnData mtd = null; // default value.

        // Behavior goes here, possibly creating a new MonsterTurnData if we decide to act.
        // mtd = etc etc etc.

        // If we end up not doing anything special, return default.
        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData SlimeDragonBehaviorScript(Monster actor)
    {
        if (MapMasterScript.activeMap.floor != MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR)
        {
            return actor.myMonsterTurnData.Continue();
        }

        if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        Map_SlimeDungeon bossmap = MapMasterScript.activeMap as Map_SlimeDungeon;        
        int numGates = bossmap.bossGateIDs.Count;

        float healthBreakpointDivisor = 1f / numGates;
        // for example, 6 gates means a gate should open with each 0.1666f of max health lost

        int numBreakpointsReached = actor.ReadActorData("gatebreakpoints");
        if (numBreakpointsReached < 0) numBreakpointsReached = 0;

        if (numBreakpointsReached <  numGates)
        {
            float healthMinPercentageForNextBreakpoint = 1f - ((numBreakpointsReached + 1) * healthBreakpointDivisor);
            if (healthMinPercentageForNextBreakpoint < 0.08f)
            {
                healthMinPercentageForNextBreakpoint = 0.08f;
            }

            // open a gate!
            if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= healthMinPercentageForNextBreakpoint)
            {
                int gateID = bossmap.bossGateIDs[numBreakpointsReached];
                Actor theGate;
                if (GameMasterScript.dictAllActors.TryGetValue(gateID, out theGate) && theGate.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = theGate as Destructible;
                    TileInteractions.BreakDestructible(actor, dt, false);
                    UIManagerScript.PlayCursorSound("StoneMovement");
                    CustomAlgorithms.RevealTilesAroundPoint(dt.GetPos(), 1, true);
                    GameLogScript.LogWriteStringRef("slimeboss_gate_opens");
                    GameMasterScript.cameraScript.AddScreenshake(0.5f);

                    string linkedActors = dt.ReadActorDataString("linkedmonsters");
                    var splitsies = linkedActors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < splitsies.Length; i++)
                    {
                        int idParse; 
                        if (Int32.TryParse(splitsies[i], out idParse))
                        {
                            Actor m;
                            if (GameMasterScript.dictAllActors.TryGetValue(idParse, out m) && m.GetActorType() == ActorTypes.MONSTER)
                            {
                                Monster linkedmon = m as Monster;
                                linkedmon.myBehaviorState = BehaviorState.FIGHT;
                                linkedmon.RemoveAttribute(MonsterAttributes.STARTASLEEP);
                            }
                        }
                    }
                }
                numBreakpointsReached++;
                actor.SetActorData("gatebreakpoints", numBreakpointsReached);
            }            
        }        

        // If we end up not doing anything special, return default.
        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData TryReviveOtherGolem(Monster actor)
    {
        MonsterTurnData mtd = null; // default value.

        if (!actor.myStats.IsAlive() || actor.isInDeadQueue)
        {
            return actor.myMonsterTurnData.Continue();
        }

        if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        // Search map for another one of us.
        bool otherGolemIsAlive = false;

        int reviveCountdownValue = actor.ReadActorData("golemrevive");
        if (reviveCountdownValue > 0) // we know the other golem is dead
        {
            reviveCountdownValue--; // so tick down the timer
        }
        else if (reviveCountdownValue == -1) 
        {
            foreach (Monster mon in MapMasterScript.activeMap.monstersInMap)
            {
                if (mon.actorfaction != Faction.PLAYER && mon.actorUniqueID != actor.actorUniqueID 
                    && mon.actorRefName == actor.actorRefName && mon.myStats.IsAlive())
                {
                    otherGolemIsAlive = true;
                    break;
                }
            }            
        }

        if (otherGolemIsAlive) return actor.myMonsterTurnData.Continue();

        int turnsToRevive = 2;

        if (reviveCountdownValue == -1)
        {
            reviveCountdownValue = turnsToRevive; // initialize this value
            actor.SetActorData("golemrevive", reviveCountdownValue);
            return actor.myMonsterTurnData.Continue();
        }
        else if (reviveCountdownValue == 0)
        {
            // Revive the other golem here!
            reviveCountdownValue = -1;
            actor.SetActorData("golemrevive", reviveCountdownValue);

            Monster newGolem = MonsterManagerScript.CreateMonster("mon_xp_heavygolem", false, false, false, 0f, false);

            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, newGolem, false);

            newGolem.xpMod = 0f;
            newGolem.myStats.SetStat(StatTypes.HEALTH, newGolem.myStats.GetMaxStat(StatTypes.HEALTH) * 0.5f, StatDataTypes.CUR, true);

            MapTileData nearbyTile = MapMasterScript.activeMap.GetRandomEmptyTile(actor.GetPos(), 1, true, true, true, false, true);
            MapMasterScript.activeMap.PlaceActor(newGolem, nearbyTile);
            MapMasterScript.singletonMMS.SpawnMonster(newGolem, true);
            CombatManagerScript.GenerateSpecificEffectAnimation(newGolem.GetPos(), "MetalPoofSystem", null, true);

            StringManager.SetTag(0, actor.displayName);
            GameLogScript.LogWriteStringRef("log_robot_summon");

            mtd = new MonsterTurnData(0.4f, TurnTypes.PASS);

            return mtd;
        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData MysteryKingChaserActions(Monster actor)
    {
        // If the king is damaged below 85% of max health, remove its "chaser" special status, which will unlock all abilities.
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.85f && actor.ReadActorData("mystery_king_chaser") == 1)
        {
            actor.RemoveActorData("mystery_king_chaser");
            // Allow our permanent positive statuses to fire.
            foreach (StatusEffect se in actor.myStats.GetAllStatuses())
            {
                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT)) continue;
                if (!se.isPositive) continue;
                se.active = true;
            }
            actor.myStats.SetStat(StatTypes.CHARGETIME, 99f, StatDataTypes.ALL, true);
            actor.myStats.SetStat(StatTypes.ACCURACY, 99f, StatDataTypes.ALL, true);
            UIManagerScript.StartConversationByRef("mysteryking_chaser_powerup", DialogType.STANDARD, null);
        }
        else
        {
            // Prevent our permanent positive statuses from firing.
            foreach(StatusEffect se in actor.myStats.GetAllStatuses())
            {
                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT)) continue;
                if (!se.isPositive) continue;
                se.active = false;
            }
        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData TyrantDragonBehavior(Monster actor)
    {
        if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        // Enrage at 50% health
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f &&
            actor.ReadActorData("enrage50") != 1 && actor.myStats.IsAlive())
        {
            GameMasterScript.AddEndOfTurnFunction(TyrantDragonStuff.EnrageScript, new string[] { actor.actorUniqueID.ToString() });

            return actor.myMonsterTurnData.Pass();
        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData AggroAnyEnemyMonster(Monster actor)
    {
        if (actor.myTarget == null)
        {
            Monster mBest = null;
            bool foundAnything = false;
            int shortest = 99;
            foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorfaction != Faction.PLAYER && m.myStats.IsAlive())
                {
                    int dist = MapMasterScript.GetGridDistance(actor.GetPos(), m.GetPos());
                    if (dist < shortest)
                    {
                        mBest = m;
                        foundAnything = true;
                        shortest = dist;
                    }
                }
            }
            if (foundAnything)
            {
                actor.AddAggro(mBest, 250f);
                actor.SetMyTarget(mBest);
                actor.SetState(BehaviorState.FIGHT);
            }

        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData FocusOnJungleCrystalsOnly(Monster actor)
    {
        // prune anything that isn't a jungle crystal from target list. Is this enough?

        //List<AggroData> adToRemove = new List<AggroData>();

        if (!initialized) Initialize();

        adToRemove.Clear();

        foreach(AggroData ad in actor.combatTargets)
        {
            /* if (ad.combatant.actorRefName != "mon_xp_defensecrystal")
            {
                adToRemove.Add(ad);
            } */
            if (ad.combatant.actorRefName != "mon_xp_defensecrystal")
            {
                ad.aggroAmount += 50f + (GameStartData.NewGamePlus * 2f);
            }
        }
        /* foreach(AggroData ad in adToRemove)
        {
            actor.combatTargets.Remove(ad);
        } */
        return actor.myMonsterTurnData.Continue();
    }

    static void FrogDragonSummonFrogs(Monster dragon, int numFrogs)
    {
        if (dragon.actorfaction != Faction.ENEMY) return;

        string[] frogsToSummon = new string[] { "mon_xp_bladefrog", "mon_xp_bouldertoad", "mon_xp_cheftoad" };
        for (int i = 0; i < numFrogs; i++)
        {
            Monster makeFrog = MonsterManagerScript.CreateMonster(frogsToSummon[i], false, true, false, 0f, false);
            MapTileData randomPos = MapMasterScript.GetRandomEmptyTile(dragon.GetPos(), 3, true, false, true, true);
            makeFrog.AddAttribute(MonsterAttributes.LOVESMUD, 100);
            MapMasterScript.activeMap.PlaceActor(makeFrog, randomPos);
            CombatManagerScript.GenerateSpecificEffectAnimation(randomPos.pos, "MudExplosion", null, true);
            MapMasterScript.singletonMMS.SpawnMonster(makeFrog, true);
            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, makeFrog, true);
        }
        StringManager.SetTag(0, dragon.displayName);
        GameLogScript.LogWriteStringRef("log_frogs_join_battle");
    }

    public static MonsterTurnData BanditDragonBehavior(Monster actor)
    {
        if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        int numPossibleHops = 0;
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.8f)
        {
            numPossibleHops += 1;
        }
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.6)
        {
            numPossibleHops += 1;
        }
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.4)
        {
            numPossibleHops += 1;
        }

        int numUsedHops = actor.ReadActorData("hopsused");
        if (numUsedHops < 0) numUsedHops = 0;

        if (numPossibleHops > numUsedHops)
        {
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                Destructible dt = act as Destructible;
                if (dt.mapObjType != SpecialMapObject.FLOORSWITCH)
                {
                    continue;
                }
                if (dt.destroyed || dt.isDestroyed)
                {
                    continue;
                }
                if (dt.GetPos() == actor.GetPos()) continue;

                MapTileData mtd = MapMasterScript.GetTile(dt.GetPos());
                bool valid = true;

                foreach(Actor subAct in mtd.GetAllActors())
                {
                    if (subAct.GetActorType() == ActorTypes.MONSTER || subAct.GetActorType() == ActorTypes.HERO)
                    {
                        valid = false;
                        break;
                    }
                    if (subAct.GetActorType() == ActorTypes.DESTRUCTIBLE)
                    {
                        Destructible checkDT = subAct as Destructible;
                        if (checkDT.mapObjType == SpecialMapObject.FLOORSWITCH)
                        {
                            continue;
                        }
                        if (checkDT.monsterCollidable)
                        {
                            valid = false;
                            break;
                        }
                    }
                    
                }
                if (!valid) continue;
                numUsedHops++;
                actor.SetActorData("hopsused", numUsedHops);
                Vector2 oldPos = actor.GetPos();
                TDAnimationScripts.JumpActorToTargetPoint(actor, dt.GetPos(), 0.5f, 360f, true);
                MapMasterScript.singletonMMS.MoveAndProcessActor(oldPos, dt.GetPos(), actor);

                MonsterTurnData newMTD = new MonsterTurnData(0.6f, TurnTypes.MOVE);

                GameMasterScript.gmsSingleton.StartCoroutine(TileInteractions.WaitThenBreakDestructible(0.5f, actor, dt, false));
                foreach(EffectScript eff in dt.dtStatusEffect.listEffectScripts)
                {
                    eff.originatingActor = actor;
                    eff.selfActor = actor;
                    eff.destructibleOwnerOfEffect = dt;
                    eff.DoEffect();
                }

                return newMTD;
            }
        }

        // At 50%, lock the player's items for a little while!
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f &&
            actor.ReadActorData("lockplayeritems") != 1 && actor.myStats.IsAlive())
        {
            actor.SetActorData("lockplayeritems", 1);

            GameMasterScript.gmsSingleton.StartCoroutine(BanditDragonStuff.BanditDragonThreatenFood(actor));

            return actor.myMonsterTurnData.Pass();
        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData FrogDragonBehavior(Monster actor)
    {
        if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        // Summon extra frogs at 66% and 33% health, only once!
        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.66f && 
            actor.ReadActorData("frogsummon66") != 1 && actor.myStats.IsAlive())
        {
            // Summon froggos #1
            actor.SetActorData("frogsummon66", 1);
            FrogDragonSummonFrogs(actor, 2);
            return actor.myMonsterTurnData.Pass();
        }

        if (actor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.33f &&
            actor.ReadActorData("frogsummon33") != 1 && actor.myStats.IsAlive())
        {
            // Summon froggos #2
            FrogDragonSummonFrogs(actor, 3);
            actor.SetActorData("frogsummon33", 1);
            return actor.myMonsterTurnData.Pass();
        }

        // Nothin special to do here.
        return actor.myMonsterTurnData.Continue();
    }


    public static MonsterTurnData RequireCampfireForFoodAbilities(Monster actor)
    {
        // Chef Toads (mon_xp_cheftoad) must have "obj_frog_campfire" for two of their abilities:
        // skill_expmon_foodheal

        bool hasCampfire = actor.CheckSummonRefs("obj_frog_campfire");

        if (hasCampfire && actor.actorfaction != Faction.PLAYER) return actor.myMonsterTurnData.Continue();

        // No campfire, so make sure our food abilities aren't usable.

        foreach (MonsterPowerData mpd in actor.monsterPowers)
        {
            if (mpd.abilityRef.refName == "skill_expmon_foodheal" || mpd.abilityRef.refName == "skill_expmon_foodbuffattack")
            {
                actor.considerAbilities.Remove(mpd);
            }
        }

        return actor.myMonsterTurnData.Continue();
    }

    public static MonsterTurnData FroggoScript(Monster actor)
    {
        //Debug.Log("Running FroggoScript!");
        return actor.myMonsterTurnData.Continue();
    }

    /// <summary>
    /// Seek and destroy logic for the red doorslimes
    /// </summary>
    /// <param name="actor"></param>
    public static MonsterTurnData KeySlimeTakeActionScript_Red(Monster actor)
    {
        return MoveToAndActUponSpecificActorRef(actor, "obj_forcefield_red", 5,
            RemoveForcefieldByDestroyingBothOfUs, "mon_want_popup");
    }
    
    /// <summary>
    /// Seek and destroy logic for the yellow doorslimes
    /// </summary>
    /// <param name="actor"></param>
    public static MonsterTurnData KeySlimeTakeActionScript_Yellow(Monster actor)
    {
        return MoveToAndActUponSpecificActorRef(actor, "obj_forcefield_yellow", 5,
            RemoveForcefieldByDestroyingBothOfUs, "mon_want_popup");
    }
    
    /// <summary>
    /// Seek and destroy logic for the cyan doorslimes
    /// </summary>
    /// <param name="actor"></param>
    public static MonsterTurnData KeySlimeTakeActionScript_Cyan(Monster actor)
    {
        return MoveToAndActUponSpecificActorRef(actor, "obj_forcefield_blue", 5,
            RemoveForcefieldByDestroyingBothOfUs, "mon_want_popup");
    }

    /// <summary>
    /// Looks around for a particular type of actor within a given range. If it is adjacent, it will call a special
    /// function. If it is close, it will move toward it. If neither of those are true, it takes a turn as normal.
    /// </summary>
    /// <param name="seekingMonster"></param>
    /// <param name="targetActorRef">The type of Destructible we're looking to approach</param>
    /// <param name="sightRange">The distance in tiles we are allowed to check</param>
    /// <param name="onAdjacent">The function to call if we are adjacent to the target</param>
    /// <param name="monsterBarkOnFindTarget">Battle text to spawn on first identifying the target</param>
    /// <returns></returns>
    public static MonsterTurnData MoveToAndActUponSpecificActorRef(Monster seekingMonster, string targetActorRef, 
        int sightRange, Func<Monster, Actor, MonsterTurnData> onAdjacent, string monsterBarkOnFindTarget = null)
    {
        //grab nearby tiles        
        var myPos = seekingMonster.GetPos();
        CustomAlgorithms.GetTilesAroundPoint(myPos, sightRange, MapMasterScript.activeMap);

        //keep track of very best one
        MapTileData bestTile = null;
        Actor bestActor = null;
        var bestDist = float.MaxValue;

        //look for the target
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            MapTileData mtd = CustomAlgorithms.tileBuffer[i];

            //Don't look in walls (Maybe look in walls?)
            if (mtd.tileType != TileTypes.GROUND) continue;

            //Only check tiles closer than the best one we found so far
            if (Vector2.SqrMagnitude(myPos - mtd.pos) > bestDist) continue;
            
            //look at every actor in this tile and ask for the target
            var a = mtd.GetActorRef(targetActorRef);
            {
                //if a target actor lives there, and that tile is closer than our previous best
                //and we can see it,
                if( a != null &&
                    CustomAlgorithms.CheckBresenhamsLOS(myPos, mtd.pos, MapMasterScript.activeMap))
                {
                    //it is best now
                    bestTile = mtd;
                    bestDist = Vector2.SqrMagnitude(myPos - mtd.pos);
                    bestActor = a;
                }
            }
        }

        //if we have no target, be normal.
        if (bestActor == null)
        {
            return seekingMonster.myMonsterTurnData.Continue();
        }        
        
        //is our target adjacent to us?
        if (CustomAlgorithms.GetGridDistance(myPos, bestTile.pos) <= 1)
        {
            //do whatever we were sent to do
            return onAdjacent(seekingMonster, bestActor);
        }
        
        //otherwise, move towards our goal
        if (seekingMonster.myTarget != bestActor)
        {
            //well it does now
            seekingMonster.myTarget = bestActor;
            if (!string.IsNullOrEmpty(monsterBarkOnFindTarget))
            {
                BattleTextManager.NewText(StringManager.GetString(monsterBarkOnFindTarget), seekingMonster.GetObject(), 
                    Color.yellow, 0.0f);
            }
        }

        //step step
        var nextTile = seekingMonster.BuildPath(myPos, bestActor.GetPos());
        seekingMonster.MoveSelf(nextTile.pos, true);
        return seekingMonster.myMonsterTurnData.Move();

    }


    /// <summary>
    /// Remove the door, remove ourselves, and play some visual effects.
    /// </summary>
    /// <param name="slime"></param>
    /// <param name="door"></param>
    /// <returns></returns>
    public static MonsterTurnData RemoveForcefieldByDestroyingBothOfUs(Monster slime, Actor door)
    {
        //blow up the slime
        slime.TakeDamage(slime.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 10.0f, DamageTypes.COUNT);
        slime.whoKilledMe = slime;
        GameObject attackSpriteObj = GameMasterScript.TDInstantiate("CriticalEffect");
        if (attackSpriteObj != null)
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(slime.GetPos(), "BigExplosionEffect", null, true);
            CombatManagerScript.TryPlayEffectSFX(attackSpriteObj, slime.GetPos(), null);
            Animatable anim = attackSpriteObj.GetComponent<Animatable>();
            anim.SetAnim("Default");
        }
        
        //remove the door
        RecursivelyRemoveAllAdjacentActorsOfThisRef(door, 0f, "BigExplosionEffect");
        
        //dead queue processed at end of turn?
        return slime.myMonsterTurnData.Pass();
    }

    static void RecursivelyRemoveAllAdjacentActorsOfThisRef(Actor focus, float delayTime, string optionalEffectRef = "")
    {
        //remove this actor
        focus.SetActorData("marked_for_removal",1);
        GameMasterScript.WaitThenDestroyActorWithVFX(focus, delayTime, optionalEffectRef);

        delayTime += 0.1f;
        var focusLoc = focus.GetPos();
        var ix = (int) focusLoc.x;
        var iy = (int) focusLoc.y;

        //check all adjacent tiles
        for(int x = -1; x <= 1; x++)
            for(int y = -1; y <= 1; y++)
            {
                var mtd = MapMasterScript.activeMap.GetTile( ix + x, iy +  y);
                if( mtd == null ) continue;
    
                //if someone like us lives nearby, blow them up too.
                foreach (var a in mtd.GetAllActors())
                {
                    if (a.actorRefName == focus.actorRefName &&
                        a.ReadActorData("marked_for_removal") != 1)
                    {
                        RecursivelyRemoveAllAdjacentActorsOfThisRef(a, delayTime, optionalEffectRef );                    
                    }
                }
            }
    }


    /// <summary>
    /// Called for slimes in the slime dungeon territory control maps. The code is in that class.
    /// </summary>
    /// <param name="actor"></param>
    /// <returns></returns>
    public static MonsterTurnData SlimeDragonSlimeTakeActionScript(Monster actor)
    {
        //if (actor.actorfaction != Faction.ENEMY) return actor.myMonsterTurnData.Continue();

        var level = MapMasterScript.activeMap as Map_SlimeDungeon;
        if (level == null)
        {
            //these monsters shouldn't exist on levels that aren't slimedungeon levels.
            return actor.myMonsterTurnData.Pass();
        }

        return level.SlimeDragonSlimeTakeActionScript(actor);
    }

    public static void CacheScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        // Find the existing, known method of name "scriptName"
        // Which belongs to class MonsterBehaviorScript
        // Which takes the argument Monster
        MethodInfo sdsTakeAction = typeof(MonsterBehaviorScript).GetMethod(scriptName, new Type[] { typeof(Monster) });

        // Now create a delegate (Func) based on this
        // <T1, TResult> where our argument type is Monster, return type is MonsterTurnData.
        Func<Monster, MonsterTurnData> converted = (Func<Monster, MonsterTurnData>)Delegate.CreateDelegate(typeof(Func<Monster, MonsterTurnData>), sdsTakeAction);

        // Cache it.
        dictDelegates.Add(scriptName, converted);

    }

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        dictDelegates = new Dictionary<string, Func<Monster, MonsterTurnData>>();
        adToRemove = new List<AggroData>();

        initialized = true;

    }           

    static Func<T, object, object> MagicMethod<T>(MethodInfo method) where T : class
    {
        // First fetch the generic form
        MethodInfo genericHelper = typeof(MonsterBehaviorScript).GetMethod("MagicMethodHelper", BindingFlags.Static | BindingFlags.NonPublic);

        // Now supply the type arguments
        MethodInfo constructedHelper = genericHelper.MakeGenericMethod
            (typeof(T), method.GetParameters()[0].ParameterType, method.ReturnType);

        // Now call it. The null argument is because it’s a static method.
        object ret = constructedHelper.Invoke(null, new object[] { method });

        // Cast the result to the right kind of delegate and return it
        return (Func<T, object, object>)ret;
    }

    static Func<TTarget, object, object> MagicMethodHelper<TTarget, TParam, TReturn>(MethodInfo method)
        where TTarget : class
    {
        // Convert the slow MethodInfo into a fast, strongly typed, open delegate
        Func<TTarget, TParam, TReturn> func = (Func<TTarget, TParam, TReturn>)Delegate.CreateDelegate
            (typeof(Func<TTarget, TParam, TReturn>), method);

        // Now create a more weakly typed delegate which will call the strongly typed one
        Func<TTarget, object, object> ret = (TTarget target, object param) => func(target, (TParam)param);
        return ret;
    }
}
