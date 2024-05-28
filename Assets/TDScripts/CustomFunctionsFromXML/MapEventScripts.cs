using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

public class MapEventScripts
{
    public static void RefreshShopkeepersOnlyOnce(Map processMap)
    {
        foreach(Actor act in processMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.NPC) continue;
            NPC n = act as NPC;
            if (string.IsNullOrEmpty(n.shopRef)) continue;
            if (n.ReadActorData("stockedonce") == 1) continue;
            n.RefreshShopInventory(processMap.floor);
        }
    }

    public static void SlimeDungeonIntro(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO) < 2)
        {            
            GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.SlimeDungeonIntroCutscene());
        }
    }

    public static void SlimeDungeonFinalAreaIntro(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO) < 3)
        {
            GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.SlimeDungeonFinalAreaIntroCutscene());
        }
    }

    // Should appear once, and only once, when entering the Spirit Dungeon
    public static void SpiritDungeonIntroCutscene(Map processMap)
    {
        bool anyStairsUp = false;

        foreach (Stairs st in processMap.mapStairs)
        {
            if (st.stairsUp)
            {
                anyStairsUp = true;
                if (!MapMasterScript.itemWorldOpen)
                {
                    st.SetDestination(MapMasterScript.theDungeon.FindFloor(MapMasterScript.TOWN2_MAP_FLOOR));
                }
                else
                {
                    if (st.NewLocation != null && !st.NewLocation.IsItemWorld())
                    {
                        st.SetDestination(MapMasterScript.theDungeon.FindFloor(GameMasterScript.gmsSingleton.ReadTempGameData("prevfloor")));
                    }                    
                }

                if (Debug.isDebugBuild) Debug.Log("Reroute! " + st.NewLocation.GetName());
            }
        } 
        
        if (!anyStairsUp)
        {
            int floor = 0;
            if (!MapMasterScript.itemWorldOpen)
            {
                floor = MapMasterScript.TOWN2_MAP_FLOOR;
            }
            else
            {
                floor = GameMasterScript.gmsSingleton.ReadTempGameData("prevfloor");
            }

            Stairs st = processMap.SpawnStairsAtLocation(true, processMap.GetTile(GameMasterScript.heroPCActor.GetPos()), floor);
            st.SetDestination(floor);
            MapMasterScript.singletonMMS.SpawnStairs(st);
        }

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.HERO) < 2)
        {
            UIManagerScript.StartConversationByRef("spirit_dungeon_intro", DialogType.STANDARD, null);
        }
    }

    // Should appear once, and only once, when entering the Frog Dungeon
    public static void FrogDragonDungeonInitialDialog(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META) < 4)
        {            
            UIManagerScript.StartConversationByRef("frog_dungeon_entry", DialogType.STANDARD, null);
            ProgressTracker.SetProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META, 4);
        }
    }

    // Used for Tyrant Dragon horde/wave maps
    public static void DisableStairsIfHordeIncomplete(Map processMap)
    {
        bool complete = GameMasterScript.heroPCActor.ReadActorData("hordeclear_" + processMap.floor) == 1;
        if (!complete)
        {
            foreach(Stairs st in processMap.mapStairs)
            {
                if (!st.stairsUp)
                {
                    st.DisableActor();
                }
            }
            // ng+, sw scaling for vinelings.
            if (GameStartData.NewGamePlus > 0)
            {
                foreach (Monster m in processMap.monstersInMap)
                {
                    if (m.actorRefName == "mon_xp_defensecrystal")
                    {
                        if (m.myStats.GetMaxStat(StatTypes.HEALTH) <= 6005f)
                        {
                            m.myStats.SetStat(StatTypes.HEALTH, 6000f + (GameStartData.NewGamePlus * 1500), StatDataTypes.ALL, true, true, true);
                            m.allMitigationAddPercent -= (0.25f * GameStartData.NewGamePlus);
                        }
                    }
                }
            }
        }
        else
        {
            bool haveStairsToNextArea = false;
            Stairs existing = null;
            foreach(Stairs st in processMap.mapStairs)
            {
                existing = st;
                if (st.prefab == "MightyVine") haveStairsToNextArea = true;
            }
            if (!haveStairsToNextArea)
            {
                MapTileData randomTile = processMap.GetRandomEmptyTile(existing.GetPos(), 3, false, true, true, true, true);
                Stairs newMightyVine = new Stairs();
                newMightyVine.prefab = "MightyVine";
                newMightyVine.SetDestination(processMap.floor + 1);
                processMap.PlaceActor(newMightyVine, randomTile);
                if (MapMasterScript.activeMap == processMap)
                {
                    MapMasterScript.singletonMMS.SpawnStairs(newMightyVine);
                }
            }
        }
        foreach(Stairs st in processMap.mapStairs)
        {
            if (st.prefab == "MightyVine")
            {
                st.EnableActor();
            }
        }
    }

    // Used for any maps that have switches & gates, mostly Bandit Dragon stuff now
    public static void SetupSwitchToGateLinks(Map processMap)
    {
        if (processMap.floor == MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR)
        {
            foreach(Stairs st in processMap.mapStairs)
            {
                if (st.stairsUp)
                {
                    st.SetDestination(MapMasterScript.BOSS1_MAP_FLOOR);
                }
            }
        }

        processMap.InitializeSwitchGateLists();

        foreach(Actor act in processMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;

            int gateIndex = act.ReadActorData("gateindex");
            // If this is a gate, find the appropriate list (or make it) and add ourselves to it

            if (gateIndex >= 0)
            {
                if (!processMap.linkSwitchesToGates.ContainsKey(gateIndex))
                {
                    processMap.linkSwitchesToGates.Add(gateIndex, new List<Destructible>());
                }
                processMap.linkSwitchesToGates[gateIndex].Add(act as Destructible);
                continue;
            }
        }
    }

    public static void BanditDragonBossFightIntro(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_BANDIT, ProgressLocations.HERO) <= 0)
        {
            Cutscenes.singleton.StartCoroutine(BanditDragonStuff.BanditDragonIntro());
        }
        SetupSwitchToGateLinks(processMap);
    }

    public static void FrogDragonBossFight(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_FROG, ProgressLocations.HERO) <= 0)
        {
            Cutscenes.singleton.StartCoroutine(FrogDragonStuff.FrogDragonIntro());
        }
    }

    public static void TyrantDragonBossFight(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_BEAST, ProgressLocations.HERO) <= 0)
        {
            Cutscenes.singleton.StartCoroutine(TyrantDragonStuff.TyrantDragonIntro());
        }
    }

    public static void SpiritDragonBossFight(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO) <= 0)
        {
            Cutscenes.singleton.StartCoroutine(SpiritDragonStuff.SpiritDragonIntro());
        }
    }

    public static void RobotDragonBossFight(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_ROBOT, ProgressLocations.HERO) <= 0)
        {
            Cutscenes.singleton.StartCoroutine(RobotDragonStuff.RobotDragonIntro());
        }
    }

    public static void SpawnInitialEggParts(Map processMap)
    {
        if (ProgressTracker.CheckProgress(TDProgress.ROBOT_DUNGEON_INITIAL_ENTRY, ProgressLocations.HERO) == 1) return;

        ProgressTracker.SetProgress(TDProgress.ROBOT_DUNGEON_INITIAL_ENTRY, ProgressLocations.HERO, 1);

        List<string> itemsToSpawn = new List<string>()
        {
            "item_part_metalslime_red_01", "item_part_metalslime_red_02", "item_part_metalslime_red_03",
            "item_part_metalslime_blue_01", "item_part_metalslime_blue_02", "item_part_metalslime_blue_03",
            "item_part_metalslime_yellow_01", "item_part_metalslime_yellow_02", "item_part_metalslime_yellow_03",
        };

        foreach(string partRef in itemsToSpawn)
        {
            MapTileData getNearbyTile = processMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, false, true, false, true);
            Item slimePart = LootGeneratorScript.CreateItemFromTemplateRef(partRef, 1f, 0f, false);
            processMap.PlaceActor(slimePart, getNearbyTile);
            MapMasterScript.singletonMMS.SpawnItem(slimePart);
        }
        
    }
}
