using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeasonalFunctions : MonoBehaviour
{

    public const string LUNAR_RABBIT_NPC = "npc_lny_moonrabbit";

    static Vector2 rabbitSpawnLoc = new Vector2(9f, 10f);
    
    public static void CheckForSeasonalAdjustmentsInNewMap()
    {
        if (GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR] && MapMasterScript.activeMap.dungeonLevelData != null && !MapMasterScript.activeMap.IsBossFloor() && !MapMasterScript.activeMap.IsJobTrialFloor() &&
            !MysteryDungeonManager.InOrCreatingMysteryDungeon() && !MapMasterScript.activeMap.IsTownMap() && !MapMasterScript.activeMap.dungeonLevelData.safeArea &&
            MapMasterScript.activeMap.dungeonLevelData.spawnTable != null)
        {
            TryAddLunarNewYearMonstersToThisMap();
        }

        if (MapMasterScript.activeMap.floor != MapMasterScript.TOWN_MAP_FLOOR) return;        

        bool rabbitExists = false;
        NPC rabbit = null;

        foreach(Actor a in MapMasterScript.activeMap.actorsInMap)
        {
            if (a.GetActorType() != ActorTypes.NPC) continue;
            if (a.actorRefName != LUNAR_RABBIT_NPC) continue;
            rabbitExists = true;
            rabbit = a as NPC;
            break;
        }

        if (!GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR]) 
        {
            if (rabbitExists)
            {
                MapMasterScript.activeMap.RemoveActorFromMap(rabbit);
                if (rabbit.GetObject() != null)
                {
                    rabbit.myMovable.FadeOutThenDie();
                }                
            }
            return;
        } 

        if (rabbitExists) return;

        // Spawn rabbit

        rabbit = NPC.CreateNPC(LUNAR_RABBIT_NPC);

        MapTileData mtd = MapMasterScript.GetTile(rabbitSpawnLoc);

        MapMasterScript.activeMap.PlaceActor(rabbit, mtd);
        MapMasterScript.singletonMMS.SpawnNPC(rabbit);        
        
    }

    private static void TryAddLunarNewYearMonstersToThisMap()
    {
        string checkStr = "lny_" + MapMasterScript.activeMap.floor;
        if (GameMasterScript.heroPCActor.ReadActorData(checkStr) == 1) 
        {
            return;
        }
        
        //if (Debug.isDebugBuild) Debug.Log("Trying to add lunar new year monsters to this map!");

        ActorTable lunarMonsters = GameMasterScript.GetSpawnTable("lunarnewyear_table");

        ActorTable mapTable = MapMasterScript.activeMap.dungeonLevelData.spawnTable;

        foreach(var kvp in lunarMonsters.table)
        {
            if (mapTable.HasActor(kvp.Key))
            {                
                for (int i = 0; i < 2; i++)
                {
                    Monster newMon = MonsterManagerScript.CreateMonster(kvp.Key, true, true, false, 0f, false);
                    //if (Debug.isDebugBuild) Debug.Log("Created monster " + newMon.actorRefName + " for LNY.");
                    MapMasterScript.activeMap.PlaceActorAtRandom(newMon, true);
                    if (i == 0 && UnityEngine.Random.Range(0,3) != 0) break;
                }
            }
        }

        GameMasterScript.heroPCActor.SetActorData(checkStr, 1);
    }

    static bool seasonalAdjustmentsToSpawnTablesDone = false;

    public static void CheckForSeasonalAdjustmentsToSpawnTablesOnGameLoad()
    {
        if (seasonalAdjustmentsToSpawnTablesDone) return;

        if (!GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR])
        {
            return;
        }

        ActorTable lunarMonsters = GameMasterScript.GetSpawnTable("lunarnewyear_table");

        foreach(var kvp in GameMasterScript.masterSpawnTableList)
        {
            if (kvp.Key == "lunarnewyear_table") continue;

            ActorTable localSpawnTable = kvp.Value;

            foreach(var lunarTableKVP in lunarMonsters.table)
            {
            
                if (localSpawnTable.HasActor(lunarTableKVP.Key))
                {
                    //Debug.Log(kvp.Key + " table has " + lunarTableKVP.Key + " monster at value " + localSpawnTable.table[lunarTableKVP.Key]);
                    localSpawnTable.table[lunarTableKVP.Key] *= 2;
                    //Debug.Log(kvp.Key + " new value is " + localSpawnTable.table[lunarTableKVP.Key]);
                }
            }

        }
    }

}
