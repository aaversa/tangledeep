using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public enum ECustomDungeonLayouts
{
    AUTOCAVE,
    AUTOCAVE_MAZE, // Variation on AUTOCAVE algorithm
    HIVES,
    STANDARD,
    STANDARDNOHALLS,
    FUTURENOHALLS,
    MAZE,
    MAZEROOMS,
    KEEP,
    CAVEROOMS,
    CAVE,
    LAKE,
    BSPROOMS,
    VOLCANO,
    CIRCLES,
    SPIRIT_DUNGEON,
}

// Creates new one-way-only dungeons, sort of like specialized Item Dreams
public partial class DungeonMaker
{
    public const float CHANCE_MAZE_EXTRAOPEN = 0.2f;
    public const float CHANCE_OF_RIVERS = 0.25f;
    public const float CHANCE_OF_RIVERS_ROOMS = 0.14f;
    public const float CHANCE_BIGGER_ROOMS = 0.4f;
    public const float CHANCE_CHUNKYAUTOCAVE = 0.2f;
    public const float CHANCE_EXTRACAVEBLOCKS = 0.2f;
    public const float CHANCE_RUINEDSTANDARD = 0.18f;
    public const float CHANCE_AUTOCAVE_LAVA = 0.18f;
    public const float CHANCE_CAVEROOMS_LAVA = 0.35f;
    public const float CHANCE_STANDARD_LAVA = 0.12f;
    public const float CHANCE_AUTOCAVE_LAKE = 0.18f;
    public const float CHANCE_BSP_LAVA = 0.2f;
    public const float CHANCE_BSP_RUINED = 0.25f;
    public const float MINIMUM_GROUND_STANDARDNOHALLS = 0.2f;
    public const float MINIMUM_GROUND_BSPROOMS = 0.23f;
    public const int CHANGE_TILESET_PER_FLOORS = 3;
    public static bool initialized;

    public static Dictionary<int, DungeonLevel> customDungeonLevelDataInSaveFile;

    static List<MonsterTemplateData> monstersCreatedInDungeon; // temporary variable used during dungeon creation

    static List<MonsterTemplateData> allRegularMonstersSortedByLevel;

    static int currentFloorIndex;

    /// <summary>
    /// Creates a custom DungeonLevel AND corresponding map, then adds the map to the master dict / dungeon list.
    /// </summary>
    /// <param name="md"></param>
    /// <param name="levelIndex"></param>
    /// <param name="typeSelected"></param>
    /// <returns></returns>
    public static Map BuildRandomMapOfType(MysteryDungeon md, int levelIndex, string typeSelected)
    {
        //Debug.Log("Try create map on level index " + levelIndex + " of type " + typeSelected);

        typeSelected = "SPIRIT_DUNGEON";

        float timeAtStart = Time.realtimeSinceStartup;

        Map createdMap = null;

        //string mapType = layoutTypeProbability.GetRandomActorRef();
        // #todo - functionalize this better
        switch(typeSelected)
        {
            case "AUTOCAVE":
                createdMap = GenerateAutoCaveMap(md, levelIndex);
                break;
            case "LAKE":
                createdMap = GenerateLakeMap(md, levelIndex);
                break;
            case "VOLCANO":
                createdMap = GenerateVolcanoMap(md, levelIndex);
                break;
            case "BSPROOMS":
                createdMap = GenerateBSPMap(md, levelIndex);
                break;
            case "CAVE":
                createdMap = GenerateCaveMap(md, levelIndex);
                break;
            case "STANDARD":
                createdMap = GenerateStandardMap(md, levelIndex);
                break;
            case "STANDARDNOHALLS":
                createdMap = GenerateStandardNoHallsMap(md, levelIndex);
                break;
            case "MAZE":
                createdMap = GenerateMazeMap(md, levelIndex);
                break;
            case "MAZEROOMS":
                createdMap = GenerateMazeRoomsMap(md, levelIndex);
                break;
            case "CAVEROOMS":
                createdMap = GenerateCaveRoomsMap(md, levelIndex);
                break;
            case "SPIRIT_DUNGEON":
                createdMap = GenerateSpiritDungeonMap(md, levelIndex);
                break;
        }

        createdMap.RelocateMonstersFromStairs();

#if UNITY_EDITOR
        //Debug.Log("Created map floor " + createdMap.dungeonLevelData.floor + " of type " + createdMap.dungeonLevelData.layoutType + " " + createdMap.dungeonLevelData.tileVisualSet + " with monster count " + createdMap.monstersInMap.Count + " in " + (Time.realtimeSinceStartup - timeAtStart));
#endif
        return createdMap;
    }

    public static Map GenerateSpiritDungeonMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            while (size == EDungeonSizes.TINY || size == EDungeonSizes.SMALL)
            {
                size = GetDungeonSize();
            }

            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.BSPROOMS][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.BSPROOMS][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            
            newDL.dictMetaData.Add("randomcolumns", UnityEngine.Random.Range(8, 20));

            switch (size)
            {
                case EDungeonSizes.MEDIUM:
                    newDL.dictMetaData.Add("minroomsize", 6);
                    newDL.dictMetaData.Add("maxroomsize", 8);
                    newDL.dictMetaData.Add("forceoverlapvalue", 0);
                    newDL.minRooms = 10;
                    newDL.maxRooms = 13;
                    newDL.maxCorridorLength = 15;
                    newDL.maxCorridors = 2;                    
                    break;
                case EDungeonSizes.LARGE:
                case EDungeonSizes.HUGE:
                    newDL.dictMetaData.Add("minroomsize", 6);
                    newDL.dictMetaData.Add("maxroomsize", 12);
                    newDL.dictMetaData.Add("forceoverlapvalue", 1);
                    newDL.minRooms = 9;
                    newDL.maxRooms = 15;
                    newDL.maxCorridorLength = 15;
                    newDL.maxCorridors = 2;
                    break;
            }

            newDL.dictMetaData.Add("dont_create_startarea", 1);

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

#if UNITY_EDITOR
            //Debug.Log("BSP size: " + size + " Rivers: " + newDL.extraRivers + " Lava: " + newDL.maxLavaPools + " Conv: " + newDL.chanceToOpenWalls);
#endif

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                float gp = createdMap.GetPercentageOfMapAsGround();
                /* if (gp < MINIMUM_GROUND_BSPROOMS)
                {
                    continue;
                } */
                success = true;
                break;
            }
        }

        //float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        //Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); 

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateBSPMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();
            
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.BSPROOMS][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.BSPROOMS][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.BSPROOMS;

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BSP_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            newDL.chanceToOpenWalls = UnityEngine.Random.Range(0.035f, 0.07f);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BSP_RUINED)
            {
                newDL.chanceToOpenWalls += 0.07f;
            }

            newDL.dictMetaData.Add("dont_create_startarea", 1);
            
            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }

            // Stumps chance?

#if UNITY_EDITOR
            //Debug.Log("BSP size: " + size + " Rivers: " + newDL.extraRivers + " Lava: " + newDL.maxLavaPools + " Conv: " + newDL.chanceToOpenWalls);
#endif

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                float gp = createdMap.GetPercentageOfMapAsGround();
                if (gp < MINIMUM_GROUND_BSPROOMS)
                {
                    continue;
                }
                success = true;
                break;
            }
        }

        //float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        //Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); 

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateVolcanoMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        float cellChanceToStartAlive = 0f;
        int cellMaxNeighbors = UnityEngine.Random.Range(1, 4);
        int cellMinNeighbors = UnityEngine.Random.Range(5, 7);
        int cellSimulationSteps = UnityEngine.Random.Range(1, 3); // is this ok?
        float cellMinGroundPercent = 0.23f;
        float cellMaxGroundPrecent = 0.6f;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.VOLCANO][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.VOLCANO][size].y + 1);

            cellChanceToStartAlive = UnityEngine.Random.Range(0.24f, 0.36f);
            cellMaxNeighbors = UnityEngine.Random.Range(1, 4);
            cellMinNeighbors = UnityEngine.Random.Range(5, 7);
            cellSimulationSteps = UnityEngine.Random.Range(1, 3); // is this ok?
            cellMinGroundPercent = 0.23f;
            cellMaxGroundPrecent = 0.6f;

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.VOLCANO;

            newDL.maxLavaPools = (int)(GetMaxLavaPools(size) * 1.5f);

            newDL.extraCrackedRockMultiplier = UnityEngine.Random.Range(0.5f, 1.5f);

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            newDL.cellMaxNeighbors = cellMaxNeighbors;
            newDL.cellMinNeighbors = cellMinNeighbors;
            newDL.cellSimulationSteps = cellSimulationSteps;
            newDL.cellMinGroundPercent = cellMinGroundPercent;
            newDL.cellMaxGroundPercent = cellMaxGroundPrecent;
            newDL.cellChanceToStartAlive = cellChanceToStartAlive;
            newDL.dictMetaData.Add("dont_create_startarea", 1);
            newDL.tileVisualSet = TileSet.VOLCANO;

            CheckForDungeonFeatures(newDL);

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }

        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateAutoCaveMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        float cellChanceToStartAlive = 0f;
        int cellMaxNeighbors = UnityEngine.Random.Range(3, 7);
        int cellMinNeighbors = UnityEngine.Random.Range(3, 7);
        int cellSimulationSteps = UnityEngine.Random.Range(1, 4); // is this ok?
        float cellMinGroundPercent = 0.25f;
        float cellMaxGroundPrecent = 0.72f;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.AUTOCAVE][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.AUTOCAVE][size].y + 1);

            cellChanceToStartAlive = UnityEngine.Random.Range(0.3f,0.65f);
            cellMaxNeighbors = UnityEngine.Random.Range(3,7);
            cellMinNeighbors = UnityEngine.Random.Range(3, 7);
            cellSimulationSteps = UnityEngine.Random.Range(1, 4); // is this ok?
            cellMinGroundPercent = 0.25f;
            cellMaxGroundPrecent = 0.69f;

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.AUTOCAVE;

            if (UnityEngine.Random.Range(0,1f) <= CHANCE_AUTOCAVE_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }
            else if (UnityEngine.Random.Range(0,1f) <= CHANCE_AUTOCAVE_LAKE)
            {
                newDL.convertCenterMapToWater = true;
                newDL.doLake = true;
            }

            newDL.caveFillConvertChance = UnityEngine.Random.Range(0f, 0.05f); 

            if (UnityEngine.Random.Range(0,1f) <= CHANCE_CHUNKYAUTOCAVE)
            {
                newDL.caveFillConvertChance += 0.035f;
            }

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            newDL.cellMaxNeighbors = cellMaxNeighbors;
            newDL.cellMinNeighbors = cellMinNeighbors;
            newDL.cellSimulationSteps = cellSimulationSteps;
            newDL.cellMinGroundPercent = cellMinGroundPercent;
            newDL.cellMaxGroundPercent = cellMaxGroundPrecent;
            newDL.cellChanceToStartAlive = cellChanceToStartAlive;
            newDL.dictMetaData.Add("dont_create_startarea", 1);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }
            
            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }


        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateCaveRoomsMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x + (roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y - roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x) / 2;
            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x, roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.CAVEROOMS][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.CAVEROOMS][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.CAVEROOMS;
            newDL.caveFillConvertChance = UnityEngine.Random.Range(0.065f, 0.1f);

            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms + 2;

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange + 1)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_CHUNKYAUTOCAVE)
            {
                newDL.caveFillConvertChance += 0.05f;
            }

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            newDL.priorityMinimum = (int)size;
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR13"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR14"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR15"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR16"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR17"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["CR18"]);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_CAVEROOMS_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }
            else
            {
                newDL.randomWaterPools = UnityEngine.Random.Range(5, 9) + (int)size;
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }


            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }

        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateMazeRoomsMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x + (roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y - roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x) / 2;
            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x, roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int secretAreas = UnityEngine.Random.Range(secretAreasByLayout[ECustomDungeonLayouts.STANDARD][size].x, secretAreasByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.MAZEROOMS][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.MAZEROOMS][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.MAZEROOMS;
            newDL.chanceToOpenWalls = UnityEngine.Random.Range(0.01f, 0.03f);
            newDL.allowDiagonals = true;

            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms + 2;

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange + 1)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_MAZE_EXTRAOPEN)
            {
                newDL.chanceToOpenWalls += 0.04f;
            }

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_AUTOCAVE_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }

        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateMazeMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.MAZE][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.MAZE][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.MAZE;
            newDL.chanceToOpenWalls = UnityEngine.Random.Range(0.01f, 0.03f);
            newDL.allowDiagonals = true;

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_MAZE_EXTRAOPEN)
            {
                newDL.chanceToOpenWalls += 0.04f;
            }

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_AUTOCAVE_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }

        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateLakeMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        int attempts = 0;

        float cellChanceToStartAlive = 0f;
        int cellMaxNeighbors = UnityEngine.Random.Range(3, 7);
        int cellMinNeighbors = UnityEngine.Random.Range(3, 7);
        int cellSimulationSteps = UnityEngine.Random.Range(1, 4); // is this ok?
        float cellMinGroundPercent = 0.25f;
        float cellMaxGroundPrecent = 0.72f;

        EDungeonSizes size = EDungeonSizes.COUNT;

        while (!success)
        {
            attempts++;
            size = GetDungeonSize();

            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.LAKE][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.LAKE][size].y + 1);
            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.LAKE][size].x + (roomCountsByLayout[ECustomDungeonLayouts.LAKE][size].y - roomCountsByLayout[ECustomDungeonLayouts.LAKE][size].x) / 2;
            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.LAKE][size].x, roomCountsByLayout[ECustomDungeonLayouts.LAKE][size].y + 1);

            cellChanceToStartAlive = UnityEngine.Random.Range(0.4f, 0.7f);
            cellMaxNeighbors = UnityEngine.Random.Range(4, 7);
            cellMinNeighbors = UnityEngine.Random.Range(4, 7);
            cellSimulationSteps = UnityEngine.Random.Range(1, 4); // is this ok?
            cellMinGroundPercent = 0.2f;
            cellMaxGroundPrecent = 0.7f;

            DungeonLevel newDL = new DungeonLevel();
            newDL.layoutType = DungeonFloorTypes.LAKE;            
            newDL.caveFillConvertChance = UnityEngine.Random.Range(0.5f, 0.08f);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_CHUNKYAUTOCAVE)
            {
                newDL.caveFillConvertChance += 0.04f;
            }

            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            newDL.cellMaxNeighbors = cellMaxNeighbors;
            newDL.cellMinNeighbors = cellMinNeighbors;
            newDL.cellSimulationSteps = cellSimulationSteps;
            newDL.cellMinGroundPercent = cellMinGroundPercent;
            newDL.cellMaxGroundPercent = cellMaxGroundPrecent;
            newDL.cellChanceToStartAlive = cellChanceToStartAlive;
            newDL.dictMetaData.Add("dont_create_startarea", 1);

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            /* if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange + 1)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            } */

            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms;

            newDL.priorityMinimum = 2;
            if (size == EDungeonSizes.LARGE || size == EDungeonSizes.HUGE)
            {
                newDL.priorityMinimum++;
            }
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["lake1"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["lake12"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["lake14"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["lake15"]);
            newDL.priorityTemplates.Add(GameMasterScript.masterDungeonRoomlist["lake18"]);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 4);
            }

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null && newDL.tileVisualSet != TileSet.MOUNTAINGRASS) // mountain grass messes with ground:wall ratio
            {
                float gp = createdMap.GetPercentageOfMapAsGround();
                if (gp < newDL.cellMinGroundPercent || gp > newDL.cellMaxGroundPercent)
                {
                    continue;
                }
                success = true;
                break;
            }
        }


        /* float groundPercentage = createdMap.GetPercentageOfMapAsGround();
        Debug.Log("Size: " + size + " CellMinNeigh " + cellMinNeighbors + " CellMax " + cellMaxNeighbors + " Cell chance " + cellChanceToStartAlive + " Simulations " + cellSimulationSteps + " MinGround " + cellMinGroundPercent + " MaxGround " + cellMaxGroundPrecent);
        Debug.Log("Success in " + attempts + " attempts! Created map " + createdMap.floor + " Ground %: " + groundPercentage + " time: " + (Time.realtimeSinceStartup - timeAtStart)); */

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateStandardNoHallsMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        while (!success)
        {
            EDungeonSizes size = GetDungeonSize();

            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].x + (roomCountsByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].y - roomCountsByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].x) / 2;
            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].x, roomCountsByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].y + 1);
            int secretAreas = UnityEngine.Random.Range(secretAreasByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].x, secretAreasByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].y + 1);
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.STANDARDNOHALLS][size].y + 1);

            // #todo : modify num monsters, champions, champ mods based on difficulty.
            // Maybe this would go somewhere else.

            DungeonLevel newDL = new DungeonLevel();
            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms;
            newDL.layoutType = DungeonFloorTypes.STANDARDNOHALLS;
            newDL.chanceToOpenWalls = UnityEngine.Random.Range(0f, 0.05f);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_RUINEDSTANDARD)
            {
                newDL.chanceToOpenWalls += 0.07f;
            }            

            newDL.dictMetaData.Add("dont_create_startarea", 1);
            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS_ROOMS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 3);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_STANDARD_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange+1)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            }

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                float gp = createdMap.GetPercentageOfMapAsGround();
                if (gp <= MINIMUM_GROUND_STANDARDNOHALLS)
                {
                    continue;
                }
                success = true;
                break;
            }            
        }

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateCaveMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        while (!success)
        {
            EDungeonSizes size = GetDungeonSize();

            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.CAVE][size].x + (roomCountsByLayout[ECustomDungeonLayouts.CAVE][size].y - roomCountsByLayout[ECustomDungeonLayouts.CAVE][size].x) / 2;

            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.CAVE][size].x, roomCountsByLayout[ECustomDungeonLayouts.CAVE][size].y + 1);
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.CAVE][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.CAVE][size].y + 1);

            DungeonLevel newDL = new DungeonLevel();
            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms;
            newDL.layoutType = DungeonFloorTypes.CAVE;
            newDL.caveFillConvertChance = UnityEngine.Random.Range(0.035f, 0.065f);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_EXTRACAVEBLOCKS)
            {
                newDL.caveFillConvertChance += 0.05f;
            }

            newDL.dictMetaData.Add("dont_create_startarea", 1);
            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_OF_RIVERS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 3);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_AUTOCAVE_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            }

            newDL.priorityMinimum = 3 + ((int)size);
            newDL.priorityTemplates = new List<RoomTemplate>()
            {
                GameMasterScript.masterDungeonRoomlist["cave_chunks4"],
                GameMasterScript.masterDungeonRoomlist["cave_chunks5"],
                GameMasterScript.masterDungeonRoomlist["cave_chunks6"],
                GameMasterScript.masterDungeonRoomlist["cave_chunks7"],
                GameMasterScript.masterDungeonRoomlist["cave_bigchunks"],
                GameMasterScript.masterDungeonRoomlist["cave_bigchunks2"],
                GameMasterScript.masterDungeonRoomlist["cave_bigchunks3"],
                GameMasterScript.masterDungeonRoomlist["cave_bigchunks4"],
                GameMasterScript.masterDungeonRoomlist["XPBigChunks1"],
                GameMasterScript.masterDungeonRoomlist["XPBigChunks2"],
                GameMasterScript.masterDungeonRoomlist["XPBigChunks3"],
                GameMasterScript.masterDungeonRoomlist["XPBigChunks4"],
                GameMasterScript.masterDungeonRoomlist["XPBigChunks5"],

            };

            //Debug.Log("Size: " + size + " Rooms: " + numRooms + " Corridors: " + maxCorridors + " Max Length: " + maxCorridorSize + " Deadends: " + deadends);

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }

        FinishMapCreation(createdMap);
        return createdMap;
    }

    public static Map GenerateStandardMap(MysteryDungeon md, int levelIndex)
    {
        bool success = false;
        Map createdMap = null;

        while (!success)
        {
            EDungeonSizes size = GetDungeonSize();

            int middleOfRoomSizeRange = roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x + (roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y - roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x) / 2;

            int numRooms = UnityEngine.Random.Range(roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].x, roomCountsByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int maxCorridors = UnityEngine.Random.Range(maxCorridorsByLayout[ECustomDungeonLayouts.STANDARD][size].x, maxCorridorsByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int maxCorridorSize = UnityEngine.Random.Range(maxCorridorLengthByLayout[ECustomDungeonLayouts.STANDARD][size].x, maxCorridorLengthByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int deadends = UnityEngine.Random.Range(deadendsByLayout[ECustomDungeonLayouts.STANDARD][size].x, deadendsByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int secretAreas = UnityEngine.Random.Range(secretAreasByLayout[ECustomDungeonLayouts.STANDARD][size].x, secretAreasByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);
            int numMonsters = UnityEngine.Random.Range(baseMonsterDensityByLayout[ECustomDungeonLayouts.STANDARD][size].x, baseMonsterDensityByLayout[ECustomDungeonLayouts.STANDARD][size].y + 1);

            // #todo : modify num monsters, champions, champ mods based on difficulty.
            // Maybe this would go somewhere else.

            DungeonLevel newDL = new DungeonLevel();
            newDL.minRooms = numRooms;
            newDL.maxRooms = numRooms;
            newDL.layoutType = DungeonFloorTypes.STANDARD;
            newDL.chanceToOpenWalls = UnityEngine.Random.Range(0f, 0.05f);

            if (UnityEngine.Random.Range(0,1f) <= CHANCE_RUINEDSTANDARD)
            {
                newDL.chanceToOpenWalls += 0.05f;
            }            

            newDL.dictMetaData.Add("dont_create_startarea", 1);
            InitializeDungeonLevelData(newDL, size, md, levelIndex, numMonsters);

            CheckForDungeonFeatures(newDL);

            if (UnityEngine.Random.Range(0,1f) <= CHANCE_OF_RIVERS_ROOMS)
            {
                newDL.extraRivers = UnityEngine.Random.Range(1, 3);
            }

            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_STANDARD_LAVA)
            {
                newDL.maxLavaPools = GetMaxLavaPools(size);
            }

            // If the room range is 10 to 16, and we have room size <= 13, consider using Embiggened rooms
            if (UnityEngine.Random.Range(0, 1f) <= CHANCE_BIGGER_ROOMS && numRooms <= middleOfRoomSizeRange)
            {
                newDL.dictMetaData.Add("minroomsize", 7);
                newDL.dictMetaData.Add("maxroomsize", 12);
            }

            //Debug.Log("Size: " + size + " Rooms: " + numRooms + " Corridors: " + maxCorridors + " Max Length: " + maxCorridorSize + " Deadends: " + deadends);

            createdMap = MapMasterScript.singletonMMS.CreateNewMap(false, newDL.floor, 1, 1.0f, newDL, null, specialMapGenData: true);
            if (createdMap != null)
            {
                success = true;
                break;
            }
        }
        
        FinishMapCreation(createdMap);
        return createdMap;
    }

    static void InitializeDungeonLevelData(DungeonLevel newDL, EDungeonSizes size, MysteryDungeon md, int levelIndex, int numMonsters)
    {
        newDL.dictMetaData.Add("no_pandora", 1);

        newDL.noSpawner = !md.monstersRespawn;

        if (levelIndex <= 2)
        {
            newDL.musicCue = "wanderer";
        }

        newDL.sideArea = true;
        newDL.noRewardPopup = true;
        newDL.excludeFromRumors = true;
        newDL.dontConnectToAnything = true;
        newDL.size = possibleMapSizeValues[size];
        newDL.evenMonsterDistribution = true;
        newDL.floor = currentFloorIndex;
        //newDL.dictMetaData.Add("no_initial_monster_seed", 1);
        newDL.spawnTable = md.spawnTables[levelIndex];
        newDL.customName = md.displayName + " " + (levelIndex+1) + "F";
        newDL.maxChampions = md.championsByFloor[levelIndex];
        newDL.maxChampionMods = md.championModsByFloor[levelIndex];
        newDL.challengeValue = md.floorsToCV[levelIndex];
        // Multiply monster count based on difficulty                

        float rateModByCv = md.floorsToCV[levelIndex] - 1f;
        if (rateModByCv < 1f) rateModByCv = 1f;

        newDL.minMonsters = (int)(numMonsters * rateModByCv);
        newDL.maxMonsters = newDL.minMonsters + 1;

        if (md.HasGimmick(MysteryGimmicks.MANY_CHAMPIONS))
        {
            newDL.minMonsters -= 2;
            newDL.maxMonsters -= 2;
        }

        newDL.spawnRateModifier = md.respawnRateModifier;

        foreach(string script in md.scriptsOnPostBuild)
        {
            newDL.scripts_postBuild.Add(script);
        }
        
        newDL.script_onEnterMap = md.scriptOnEnterMap;

        // At tileset switch points (every X floors), change the tile set
        if (levelIndex == 0 || levelIndex % CHANGE_TILESET_PER_FLOORS == 0)
        {
            newDL.tileVisualSet = (TileSet)Enum.Parse(typeof(TileSet), md.dungeonTileSets.GetRandomActorRef());
        }
        else // Otherwise, use previous floor's tileset
        {
            newDL.tileVisualSet = md.mapsInDungeon[levelIndex - 1].dungeonLevelData.tileVisualSet; 
        }               

        if (!string.IsNullOrEmpty(md.parallaxSpriteRef))
        {
            newDL.parallaxSpriteRef = md.parallaxSpriteRef;
            newDL.parallaxShiftPerTile = md.parallaxShiftPerTile;
            newDL.parallaxTileCount = md.parallaxTileCount;
        }
        newDL.revealAll = md.revealAll;
        newDL.unbreakableWalls = md.unbreakableWalls;

        int midFloor = md.floors / 2;

        // final levels get harder music
        
        if (md.floors >= 30)
        {
            if (levelIndex >= md.floors - 5)
            {
                newDL.musicCue = "hardmysterydungeon";
            }
            if (levelIndex >= midFloor - 2 && levelIndex <= midFloor + 2)
            {
                newDL.musicCue = "mediummysterydungeon";
            }
        }
        else
        {
            if (levelIndex >= md.floors - 3)
            {
                newDL.musicCue = "hardmysterydungeon";
            }
            if (levelIndex >= midFloor - 1 && levelIndex <= midFloor + 1)
            {
                newDL.musicCue = "mediummysterydungeon";
            }
        }  
    }

    static void CheckForDungeonFeatures(DungeonLevel dl)
    {
        float chance;
        if (chanceOfDungeonFeaturesByTileset[EDungeonFeatures.STUMPS].TryGetValue(dl.tileVisualSet, out chance))
        {
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                dl.addStumps = UnityEngine.Random.Range(0.01f, 0.03f);
            }
        }
        if (chanceOfDungeonFeaturesByTileset[EDungeonFeatures.FUNGUS].TryGetValue(dl.tileVisualSet, out chance))
        {
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                dl.hasFungus = true;
            }
        }
    }

    static EDungeonSizes GetDungeonSize()
    {
        string sizeAsStr = dungeonSizeProbability.GetRandomActorRef();
        EDungeonSizes size = (EDungeonSizes)Enum.Parse(typeof(EDungeonSizes), sizeAsStr);
        return size;
    }

    static int GetMaxLavaPools(EDungeonSizes size)
    {
        int minValue = 2 + ((int)size) / 2;
        int maxValue = minValue + ((int)size) / 2;
        return UnityEngine.Random.Range(minValue, maxValue + 1);
    }

    /// <summary>
    /// Adds created map to master dict/dungeon list, increases counter/index of floor
    /// </summary>
    /// <param name="createdMap"></param>
    static void FinishMapCreation(Map createdMap)
    {
        MapMasterScript.theDungeon.maps.Add(createdMap);
        customDungeonLevelDataInSaveFile.Add(createdMap.floor, createdMap.dungeonLevelData);
        currentFloorIndex++;
    }

    /// <summary>
    ///  Modified version of this function messes with room sizes.
    /// </summary>
    /// <param name="mgd"></param>
    /// <param name="createdMap"></param>
    /// <param name="localFloor"></param>
    /// <returns></returns>
    public static MapGenerationData RerollMapGenerationParameters(MapGenerationData mgd, Map createdMap, int localFloor)
    {
        mgd.minRoomSize = 5;
        mgd = MapMasterScript.singletonMMS.RerollMapGenerationParameters(mgd, createdMap, localFloor);

        // Only bother with this for map types that use rooms at all.
        if (createdMap.dungeonLevelData.layoutType == DungeonFloorTypes.STANDARD)
        {
            if (!createdMap.dungeonLevelData.dictMetaData.ContainsKey("triedbigrooms"))
            {
                createdMap.dungeonLevelData.dictMetaData.TryGetValue("minroomsize", out mgd.minRoomSize);
                createdMap.dungeonLevelData.dictMetaData.TryGetValue("maxroomsize", out mgd.maxRoomSize);
                createdMap.dungeonLevelData.dictMetaData.Add("triedbigrooms", 1);
            }
            else
            {
                // Tried big rooms once, didn't work. Move on.
                createdMap.dungeonLevelData.dictMetaData.Remove("minroomsize");
                createdMap.dungeonLevelData.dictMetaData.Remove("maxroomsize");
            }
        }
            

        return mgd;
    }

    public static void FlushSaveFileData()
    {
        if (!initialized) return;
        customDungeonLevelDataInSaveFile.Clear();
        currentFloorIndex = MapMasterScript.CUSTOMDUNGEON_START_FLOOR;
    }

    public static void Initialize()
    {
        if (initialized) return;
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;

        customDungeonLevelDataInSaveFile = new Dictionary<int, DungeonLevel>();

        SetDungeonTemplateData();

        currentFloorIndex = MapMasterScript.CUSTOMDUNGEON_START_FLOOR;
        initialized = true;
        monstersCreatedInDungeon = new List<MonsterTemplateData>();
    }


    /// <summary>
    /// Sets the current level floor counter (ensuring unique floor #s) based on loaded DLDs from meta progress
    /// </summary>
    public static void SetCustomDungeonLevelCounterFromLoadedLevels()
    {
        if (!initialized) return;
        currentFloorIndex = MapMasterScript.CUSTOMDUNGEON_START_FLOOR + customDungeonLevelDataInSaveFile.Count + 1;
    }

    public static ActorTable CreateSpawnTableForDungeonLevel(MysteryDungeon md, int level)
    {
        List<MonsterTemplateData> monstersForThisLevel = new List<MonsterTemplateData>();
        int targetMonsterLevel = BalanceData.GetMonsterLevelByCV(md.floorsToCV[level], md.HasGimmick(MysteryGimmicks.NO_SCALING_LIMIT));

        ActorTable theTable = new ActorTable();        

        foreach (MonsterTemplateData mtd in md.monstersInDungeon)
        {
            //Debug.Log("Compare " + mtd.refName + " " + mtd.baseLevel + " to tlvl " + targetMonsterLevel);
            if (mtd.baseLevel == targetMonsterLevel)
            {
                theTable.AddToTable(mtd.refName, 100);
            }
            else if (mtd.baseLevel < targetMonsterLevel)
            {
                // We might include some of these, depending on 'distance' from target monster level
                int distance = targetMonsterLevel - mtd.baseLevel;

                // For example, at level 3, a level 2 monster has an XPmod of 0.7f
                // We could add 70 of these to table - a bit less common.
                // At level 5, a level 2 monster is 0.2f - just 20 of these.
                float xpModToPlayer = BalanceData.playerMonsterRewardTable[targetMonsterLevel, distance];
                if (xpModToPlayer >= 0.1f)
                {
                    theTable.AddToTable(mtd.refName, (int)(xpModToPlayer * 100));
                }
            }
            else if (mtd.baseLevel == targetMonsterLevel+1)
            {
                // If it's just a little too high level, we can introduce these at a lower rate.
                theTable.AddToTable(mtd.refName, 20);
            }
            else if (mtd.baseLevel > targetMonsterLevel+1)
            {
                // Too high level, just stop here.
                break;
            }
        }
        
        return theTable;
    }

    public static IEnumerator CreateMonsterTemplatesForEntireDungeon(MysteryDungeon md)
    {
        // We must have a minimium of 3-4 unique monsters
        int baseNumUniqueMonsters = UnityEngine.Random.Range(3, 5);

        MonsterTemplateData mtd = null;

        float timeAtLastPause = Time.realtimeSinceStartup;

        for (int i = 0; i < baseNumUniqueMonsters; i++)
        {
            mtd = MonsterMaker.CreateNewMonsterByCV(1.0f, md.monsterFamilies.GetRandomActorRef(), md);

            if ((Time.realtimeSinceStartup - timeAtLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }

            monstersCreatedInDungeon.Add(mtd);
        }

        float loadingBarIncrement = 0.5f / md.floors;

        float cvDifferenceToSpawnNewMonster = 0.1f;
        // For each increase in CV, create a new monster - scaling down depending on overall level
        // For each floor, 50% to create a new monster anyway
        // Plus another 25% roll
        float previousCVForNewMonsterSpawn = 0.1f;

        // We are balanced around expectation of 20 floors. If there are more, we need to spread things out.
        bool doSecondarySpawnCheck = true;
        bool doTertiarySpawnCheck = true;
        if (md.floors > 20)
        {
            doSecondarySpawnCheck = false;
        }
        if (md.floors > 50)
        {
            doTertiarySpawnCheck = false;
            previousCVForNewMonsterSpawn = 0.15f;
        }

        int minUniqueMonsters = md.floors + 1;

        if (minUniqueMonsters > 60)
        {
            minUniqueMonsters = 60;
        }

        for (int i = 1; i < md.floors; i++)
        {
            float levelCV = md.floorsToCV[i];
            if (i >= md.floors/2)
            {
                cvDifferenceToSpawnNewMonster = 0.05f;
            }
            if (levelCV - previousCVForNewMonsterSpawn >= cvDifferenceToSpawnNewMonster)
            {
                previousCVForNewMonsterSpawn = levelCV;
                mtd = MonsterMaker.CreateNewMonsterByCV(levelCV, md.monsterFamilies.GetRandomActorRef(), md);
                monstersCreatedInDungeon.Add(mtd);
            }
            if (UnityEngine.Random.Range(0, 3) == 0 && doSecondarySpawnCheck) // Generate new monster for this floor
            {
                mtd = MonsterMaker.CreateNewMonsterByCV(levelCV, md.monsterFamilies.GetRandomActorRef(), md);
                monstersCreatedInDungeon.Add(mtd);
            }

            if ((Time.realtimeSinceStartup - timeAtLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }

            UIManagerScript.FillLoadingBar(UIManagerScript.GetLoadingBarFillValue() + loadingBarIncrement);
        }

        // Spawn more monsters if we're under the limit
        if (md.monstersInDungeon.Count < minUniqueMonsters)
        {
            int monstersToCreate = minUniqueMonsters - md.monstersInDungeon.Count;
            float previousCV = 0f;
            for (int i = 0; i <= monstersToCreate; i++)
            {
                float cvAtRandom = UnityEngine.Random.Range(md.floorsToCV[1], md.floorsToCV[md.floors - 1]);
                cvAtRandom = Mathf.Round(cvAtRandom * 100f) / 100f;
                while (CustomAlgorithms.CompareFloats(previousCV, cvAtRandom))
                {
                    cvAtRandom = UnityEngine.Random.Range(md.floorsToCV[1], md.floorsToCV[md.floors - 1]);
                    cvAtRandom = Mathf.Round(cvAtRandom * 100f) / 100f;
                }
                previousCV = cvAtRandom;
                mtd = MonsterMaker.CreateNewMonsterByCV(cvAtRandom, md.monsterFamilies.GetRandomActorRef(), md);
                monstersCreatedInDungeon.Add(mtd);

                if ((Time.realtimeSinceStartup - timeAtLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    timeAtLastPause = Time.realtimeSinceStartup;
                }
            }

            // spawn monsters here
        }

        yield return null;

        md.monstersInDungeon = monstersCreatedInDungeon;
        if (Debug.isDebugBuild) Debug.Log("Total unique monsters in this dungeon: " + monstersCreatedInDungeon.Count);
    }

    public static IEnumerator CreateRegularNonCustomSpawnTablesForDungeon(MysteryDungeon md)
    {
        monstersCreatedInDungeon.Clear();

        if (allRegularMonstersSortedByLevel == null)
        {
            allRegularMonstersSortedByLevel = new List<MonsterTemplateData>();
            foreach (MonsterTemplateData mtd in GameMasterScript.masterMonsterList.Values)
            {
                if (mtd.isBoss) continue;
                if (mtd.xpMod == 0f) continue;
                if (mtd.faction != Faction.ENEMY) continue;
                if (!mtd.autoSpawn) continue;
                allRegularMonstersSortedByLevel.Add(mtd);
            }

            allRegularMonstersSortedByLevel.Sort((a, b) => (a.baseLevel.CompareTo(b.baseLevel)));            
        }


        foreach(MonsterTemplateData mtd in allRegularMonstersSortedByLevel)
        {
            monstersCreatedInDungeon.Add(mtd);
        }

        md.monstersInDungeon = monstersCreatedInDungeon.OrderBy(m => m.challengeValue).ToList();

        float timeAtLastPause = Time.realtimeSinceStartup;

        // Then create spawn tables per floor, plus merchants
        for (int i = 0; i < md.floors; i++)
        {
            ActorTable table = CreateSpawnTableForDungeonLevel(md, i);
            table.refName = md.refName + "_spawntable_" + i;
            md.spawnTables.Add(i, table);
            //Debug.Log("Created: " + table.refName + " " + i + " " + table.actors.Count);
            GameMasterScript.masterSpawnTableList.Add(table.refName, table);

            if ((Time.realtimeSinceStartup - timeAtLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }
        }
    }

    public static IEnumerator CreateMonstersAndSpawnTablesPerFloor(MysteryDungeon md)
    {
        // First create all monsters for this dungeon.
        monstersCreatedInDungeon.Clear();
        yield return DungeonMaker.CreateMonsterTemplatesForEntireDungeon(md);

        float timeAtLastPause = Time.realtimeSinceStartup;

        md.monstersInDungeon = md.monstersInDungeon.OrderBy(m => m.challengeValue).ToList();

        // Then create spawn tables per floor, plus merchants
        for (int i = 0; i < md.floors; i++)
        {
            ActorTable table = CreateSpawnTableForDungeonLevel(md, i);
            table.refName = md.refName + "_spawntable_" + i;
            md.spawnTables.Add(i, table);
            GameMasterScript.masterSpawnTableList.Add(table.refName, table);

            if ((Time.realtimeSinceStartup - timeAtLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastPause = Time.realtimeSinceStartup;
            }
        }
    }

    public static IEnumerator CreateAndPopulateMysteryDungeon(MysteryDungeon theDungeon)
    {
        float timeAtStart = Time.realtimeSinceStartup;

        theDungeon.prefabsUsedByMonstersInDungeon.Clear();

        // Step 1: Create all unique monsters for the dungeon, and create spawn tables for all levels of the dungeon
        if (!theDungeon.HasGimmick(MysteryGimmicks.NORMAL_MONSTERS))
        {
            yield return CreateMonstersAndSpawnTablesPerFloor(theDungeon);
        }
        else
        {
            yield return CreateRegularNonCustomSpawnTablesForDungeon(theDungeon);
        }
        
        //Debug.Log("Finished creating spawn tables per floor.");

        theDungeon.mapsInDungeon = new Map[theDungeon.floors];

        float timeSinceLastPause = Time.realtimeSinceStartup;

        float loadingBarIncrement = 0.5f / theDungeon.floors;

        // Step 2: Create the actual dungeon levels themselves.
        // ... Should we actually do this here, or just do them one at a time on level change? 
        for (int i = 0; i < theDungeon.floors; i++)
        {
            string levelType = theDungeon.dungeonLayouts.GetRandomActorRef();
            Map createdMap = BuildRandomMapOfType(theDungeon, i, levelType);
            createdMap.challengeRating = createdMap.dungeonLevelData.challengeValue;
            if ((Time.realtimeSinceStartup - timeSinceLastPause) >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeSinceLastPause = Time.realtimeSinceStartup;
            }
            UIManagerScript.FillLoadingBar(UIManagerScript.GetLoadingBarFillValue() + loadingBarIncrement);
            theDungeon.mapsInDungeon[i] = createdMap;

            // Consider spawning a merchant!
            if (UnityEngine.Random.Range(0, 1f) < theDungeon.localChanceSpawnShopkeeperOnFloor)
            {
                string shopOwnerRef = "npc_randomshop" + UnityEngine.Random.Range(1, 7); // at the moment, all merchants are Nandos
                NPC n = NPC.CreateNPC(shopOwnerRef);
                createdMap.PlaceActorAtRandom(n);
                n.SetActorData("mysterymerchant", 1);
            }

        }

        yield return null;

        // Now create stairs per floor. Stairs only go FORWARD, never back. No backtracking!
        for (int i = 0; i < theDungeon.floors; i++)
        {
            if (i < theDungeon.floors-1)
            {
                theDungeon.mapsInDungeon[i].SpawnStairs(false, theDungeon.mapsInDungeon[i + 1].floor);
            }            
        }

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS])
        {
            SpawnWeaponAndArmorStatuesInDungeon(theDungeon);
        }

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            SpawnMiniDreamcastersInDungeon(theDungeon);
        }
        

        SpawnBossInMap(theDungeon.mapsInDungeon[theDungeon.floors - 1], theDungeon);

        if (Debug.isDebugBuild) Debug.Log("Time to create dungeon: " + (Time.realtimeSinceStartup - timeAtStart));
    }
    
    static void SpawnMiniDreamcastersInDungeon(MysteryDungeon theDungeon)
    {
        int spawnEveryNFloors = 4;
        if (theDungeon.floors <= 12)
        {
            spawnEveryNFloors = 3;
        }
        if (theDungeon.floors > 20)
        {
            spawnEveryNFloors = 5;
        }

        // Every N floors, spawn a mini dreamcaster somewhere!
        for (int i = 1; i < theDungeon.floors; i++)
        {
            if (i % spawnEveryNFloors == 0) 
            {
                Map floorToSpawn = theDungeon.mapsInDungeon[i];
                MapTileData emptyMTD = FindEmptyMTDInMysteryDungeonFloor(floorToSpawn);

                if (emptyMTD != null)
                {
                    floorToSpawn.CreateNPCInTile(emptyMTD, "npc_minidreamcaster");
#if UNITY_EDITOR
                    //Debug.Log("Spawned minidreamcaster at " + emptyMTD.pos + " on " + floorToSpawn.floor);
#endif
                }
            }
        }
    }

    static void SpawnWeaponAndArmorStatuesInDungeon(MysteryDungeon theDungeon)
    {
        // Depending on # of floors, create some Armor and Weapon Master statues for player to find
        int weaponMasterStatues = 1;
        int armorMasterStatues = 1;
        if (theDungeon.floors >= 15)
        {
            weaponMasterStatues++;
        }
        if (theDungeon.floors >= 20)
        {
            armorMasterStatues++;
        }
        if (theDungeon.floors >= 25)
        {
            weaponMasterStatues++;
        }

        List<Map> mapsUsedForStatues = new List<Map>();

        bool allStatuesCreated = false;

        while (!allStatuesCreated)
        {
            if (weaponMasterStatues == 0 && armorMasterStatues == 0)
            {
                allStatuesCreated = true;
                break;
            }

            Map floorToSpawn = null;
            bool floorValid = false;
            int attempts = 0;
            while (!floorValid)
            {
                attempts++;
                if (attempts > 500) break;
                floorToSpawn = theDungeon.mapsInDungeon[UnityEngine.Random.Range(0, theDungeon.mapsInDungeon.Length)];
                if (mapsUsedForStatues.Contains(floorToSpawn)) continue;
                if (floorToSpawn.GetChallengeRating() <= 1.2f) continue;

                floorValid = true;

                foreach (Map m in mapsUsedForStatues)
                {
                    if (Mathf.Abs(floorToSpawn.floor - m.floor) <= 1)
                    {
                        floorValid = false;
                        break;
                    }
                }

                if (!floorValid) continue;
                // Success, found a good map!

                string npcTemplate = "";

                if (weaponMasterStatues > 0)
                {
                    weaponMasterStatues--;
                    npcTemplate = "npc_weaponmaster_statue";
                }
                else if (armorMasterStatues > 0)
                {
                    armorMasterStatues--;
                    npcTemplate = "npc_armormaster_statue";
                }

                MapTileData emptyMTD = FindEmptyMTDInMysteryDungeonFloor(floorToSpawn);

                if (emptyMTD != null)
                {
                    floorToSpawn.CreateNPCInTile(emptyMTD, npcTemplate);
#if UNITY_EDITOR
                    //Debug.Log("Spawned " + npcTemplate + " at " + emptyMTD.pos + " on " + floorToSpawn.floor);
#endif
                    //mapsUsedForStatues.Add(floorToSpawn); Uncomment to disallow weapon+armor statues on same floor.
                }                
                break;
            } // End of sub loop that picked map for the statue

        }
    }

    static MapTileData FindEmptyMTDInMysteryDungeonFloor(Map floorToSpawn)
    {
        MapTileData emptyMTD = null;
        // We don't want walls n/e/s/w of the statue
        int attempts = 0;
        while (true)
        {
            attempts++;
            if (attempts > 250) break;
            emptyMTD = floorToSpawn.GetRandomEmptyTileForMapGen();
            bool valid = true;
            for (int x = 0; x < MapMasterScript.directions.Length; x++)
            {
                Vector2 checkPos = emptyMTD.pos + MapMasterScript.directions[x];
                if (!floorToSpawn.InBounds(checkPos))
                {
                    valid = false;
                    break;
                }
                MapTileData mtd = floorToSpawn.GetTile(checkPos);
                if (mtd.tileType != TileTypes.GROUND)
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
            {
                break;
            }
        }

        return emptyMTD;
    }

    static void SpawnBossInMap(Map m, MysteryDungeon md)
    {
        string mTemplate = m.dungeonLevelData.spawnTable.GetRandomActorRef();
        Monster bossMon = MonsterManagerScript.CreateMonster(mTemplate, true, true, false, 0f, false);
        for (int i = 0; i < m.dungeonLevelData.maxChampionMods; i++)
        {
            bossMon.MakeChampion(true);
        }
        bossMon.myStats.BoostCoreStatsByPercent(0.15f);
        bossMon.myStats.AddStatusByRef("status_shadowking", bossMon, 99);
        bossMon.displayName = StringManager.GetString("exp_mysterydungeon_bossname");
        bossMon.SetActorData("mysteryking", 1);

        float targetCVForRelic = m.dungeonLevelData.challengeValue;
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            targetCVForRelic = GameMasterScript.heroPCActor.myMysteryDungeonData.statsPriorToEntry.GetLevel();
        }

        int numRelics = 1;
        if (md.floors >= 40)
        {
            numRelics++;
        }

        for (int i = 0; i < numRelics; i++)
        {
            Item relic = LegendaryMaker.CreateNewLegendaryItem(targetCVForRelic);
            if (Debug.isDebugBuild) Debug.Log("Relic created for MK: " + relic.actorRefName);
            bossMon.myInventory.AddItem(relic, false);
            relic.SetActorData("grc", 1);
        }

        MapTileData randomTile = m.GetRandomEmptyTileForMapGen();
        m.PlaceActor(bossMon, randomTile);

        GameMasterScript.heroPCActor.SetActorData("mystery_king_id", bossMon.actorUniqueID);

        if (md.HasGimmick(MysteryGimmicks.MONSTER_CHASER))
        {
            bossMon.SetActorData("mystery_king_chaser", 1);
            bossMon.myStats.SetStat(StatTypes.CHARGETIME, 50f, StatDataTypes.ALL, true);
            bossMon.myStats.SetStat(StatTypes.ACCURACY, 50f, StatDataTypes.ALL, true);
            //bossMon.myTemplate.scriptTakeAction = "MysteryKingChaserActions";
            bossMon.scriptTakeAction = "MysteryKingChaserActions";
            // Prevent our permanent positive statuses from firing.
            foreach (StatusEffect se in bossMon.myStats.GetAllStatuses())
            {
                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT)) continue;
                if (!se.isPositive) continue;
                se.active = false;
            }
        }
    }
}

public partial class MapMasterScript
{
    //600 - 750 custom dungeons, nothing inbetween can be used
    public const int CUSTOMDUNGEON_START_FLOOR = 600;
    public const int CUSTOMDUNGEON_END_FLOOR = 750;
}

public partial class Map
{
    /// <summary>
    /// Returns TRUE if this map is part of a special/custom DLC1 dungeon
    /// </summary>
    /// <returns></returns>
    public bool IsMysteryDungeonMap()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        if (floor >= MapMasterScript.CUSTOMDUNGEON_START_FLOOR && floor <= MapMasterScript.CUSTOMDUNGEON_END_FLOOR)
        {
            return true;
        }
        return false;
    }

    public float GetPercentageOfMapAsGround()
    {
        // We don't want to count the edges of the map as part of this calculation.
        // So for a 20x20 map, we're really only counting the contents: 18x18
        int numTiles = (dungeonLevelData.size - 2) * (dungeonLevelData.size - 2);
        int numGroundTiles = GetNumGroundTiles();

        float groundPercentage = (float)numGroundTiles / (float)numTiles;
        return groundPercentage;
    }

    public int GetNumGroundTiles()
    {
        int numGroundTiles = 0;
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                if (mapArray[x, y].tileType == TileTypes.GROUND) numGroundTiles++;
            }
        }

        return numGroundTiles;
    }
}