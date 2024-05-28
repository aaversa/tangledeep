using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class MonsterSpawnFunctions {

    public static Dictionary<string, Action<Map, Monster, bool>> dictDelegates;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        dictDelegates = new Dictionary<string, Action<Map, Monster, bool>>();

        initialized = true;
    }

    public static void CacheScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo myMethod = typeof(MonsterSpawnFunctions).GetMethod(scriptName, new Type[] { typeof(Map), typeof(Monster), typeof(bool) });

        Action<Map, Monster, bool> converted = (Action<Map, Monster, bool>)Delegate.CreateDelegate(typeof(Action<Map, Monster, bool>), myMethod);

        dictDelegates.Add(scriptName, converted);

    }

    // These functions will fire after a monster has been completely spawned, created, and added to map
    // It will fire during map gen, OR when a wandering monster has spawned
    // If you want to act on the monster's physical sprite/object, it will already exist IF we are in active gameplay
    public static void OnSpawnFunctionTemplate(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        if (GameMasterScript.gameLoadSequenceCompleted && mapForMonster == MapMasterScript.activeMap)
        {
            // If this conditional is true, the monster was spawned during gameplay and in the active map
            // So we KNOW its prefab exists and has been generated
        }

        // monSpawned is the newly created monster

        // wanderingMonster is TRUE if it was spawned outside of map gen.
    }

    public static void AddJumpToMonstersWithoutIt(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        // Add a special "Leap" ability only to monsters with no other way to move or pull

        foreach (MonsterPowerData mpd in monSpawned.monsterPowers)
        {
            foreach(EffectScript eff in mpd.abilityRef.listEffectScripts)
            {
                if (eff.effectType == EffectType.MOVEACTOR)
                {
                    MoveActorEffect mae = eff as MoveActorEffect;
                    if (mpd.abilityRef.abilityFlags[(int)AbilityFlags.MOVESELF])
                    {
                        return;
                    }
                }
            }
        }

        monSpawned.LearnNewPower("skill_simplehop", 1f, 1f, 2, 99);
    }


    public static void FocusMonstersOnJungleCrystals(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (MapMasterScript.activeMap == null || MapMasterScript.activeMap.floor != mapForMonster.floor) return;

        monSpawned.scriptTakeAction = "FocusOnJungleCrystalsOnly";

        if (monSpawned.actorfaction == Faction.PLAYER) return;

        foreach(Monster m in mapForMonster.monstersInMap)
        {
            if (m.actorfaction != Faction.PLAYER) continue;
            if (m.actorRefName == "mon_xp_defensecrystal")
            {
                monSpawned.AddAggro(m, 99999f);
            }
        }
        monSpawned.aggroRange = 99;
        monSpawned.turnsToLoseInterest = 9999;
        monSpawned.PickTargetFromPossibleTargets();
        monSpawned.AddAttribute(MonsterAttributes.LOVESLAVA, 100);
    }


    public static void GiveMonsterMudImmunity(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        if (monSpawned.turnsToDisappear > 0) return; // Don't apply to summons.

        if (monSpawned.CheckAttribute(MonsterAttributes.LOVESMUD) < 100)
        {
            monSpawned.AddAttribute(MonsterAttributes.LOVESMUD, 100);
        }        
    }


    /// <summary>
    /// Called in the DragonRobot dungeon, this function has a chance of assigning slime egg parts to monsters that
    /// spawn in the level. The slime egg parts assemble to make slime eggs, which hatch into slimes that open doors.
    /// </summary>
    /// <param name="mapForMonster"></param>
    /// <param name="monSpawned"></param>
    /// <param name="wanderingMonster"></param>
    public static void AssignSlimeEggPartsToMonster(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        if (monSpawned.turnsToDisappear > 0 && mapForMonster.floor != MapMasterScript.ROBOT_DRAGON_DUNGEONEND_FLOOR)
        {
            return; // Don't apply to summons, unless it's the robot dragon boss fight
        }

        var partChance = mapForMonster.dungeonLevelData.GetMetaData("chanceforeggpart");
        if (UnityEngine.Random.value * 100.0f > partChance) return;
        
        //drop at least 1, but sometimes 2 or 3.
        var numItems = Math.Max(1, UnityEngine.Random.Range(-2, 4));

        for (int t = 0; t < numItems; t++)
        {
            AddRandomSlimeEggPartsToInventory(monSpawned.myInventory);
        }

    }

    /// <summary>
    /// Actually adds slime egg parts, removed from the Assign function above so we could call this on any inventory
    /// and not just a monster.
    /// </summary>
    /// <param name="inv"></param>
    public static void AddRandomSlimeEggPartsToInventory(InventoryScript inv)
    {
        //item_part_metalslime_[color]_[number]
        int itemnumber = UnityEngine.Random.Range(1, 4);
        string colorname = "";
        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                colorname = "red";
                break;
            case 1:
                colorname = "blue";
                break;
            case 2:
                colorname = "yellow";
                break;
        }

        //Add this item to the monster inventory to drop when killed.
        var itemref = "item_part_metalslime_" + colorname + "_0" + itemnumber;
        var newItem = LootGeneratorScript.CreateItemFromTemplateRef(itemref, 1.0f, 1.0f, false);
        inv.AddItem(newItem, true);
        newItem.SetActorData("alwaysdrop", 1);
    }

    /// <summary>
    /// When a monster is spawned in a slime dungeon, it may need special effects. The code that gets run is in
    /// the Map_SlimeDungeon class
    /// </summary>
    /// <param name="mapForMonster"></param>
    /// <param name="monSpawned"></param>
    /// <param name="wanderingMonster"></param>
    public static void OnMonsterSpawnedInSlimeDungeon(Map mapForMonster, Monster monSpawned, bool wanderingMonster)
    {
        var slimeMap = mapForMonster as Map_SlimeDungeon;
        if (slimeMap != null)
        {
            slimeMap.OnMonsterSpawnedInSlimeDungeon(monSpawned);
        }
    }

}
