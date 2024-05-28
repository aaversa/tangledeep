using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DungeonGenerationScripts {

    public static List<MapTileData> newSeedTiles;  

    public static void SeedJobTrial(Map trialMap, int playerLevel, int trialLevel)
    {
        ActorTable spawnTable = new ActorTable();

        int numDifferentMonsters = 5;
        int numMonstersInMap = 12;
        int numFungus = 0;
        int numFountains = 0;

        switch (trialLevel)
        {
            case 0:
                numDifferentMonsters = 5;
                numMonstersInMap = 12;
                numFountains = 2;

                // Spawn quest pillars
                List<Actor> pillarsCreated = new List<Actor>();
                for (int i = 0; i < 4; i++)
                {
                    MapTileData emptyTile = null;
                    bool tileValid = false;
                    Vector2 centerPos = new Vector2(trialMap.columns / 2f, trialMap.rows / 2f);
                    int attempts = 0;
                    while (!tileValid)
                    {
                        attempts++;
                        if (attempts > 1000)
                        {
                            Debug.Log("Hit max attempts.");
                            break;
                        }
                        tileValid = true;
                        emptyTile = trialMap.GetRandomEmptyTileForMapGen();
                        foreach(Actor act in pillarsCreated)
                        {
                            if (MapMasterScript.GetGridDistance(act.GetPos(), emptyTile.pos) < 10)
                            {
                                tileValid = false;
                                continue;
                            }
                        }
                        
                        if (MapMasterScript.GetGridDistance(centerPos, emptyTile.pos) < 5)
                        {
                            tileValid = false;
                        }
                    }

                    pillarsCreated.Add(trialMap.CreateDestructibleInTile(emptyTile, "obj_jobtrial_crystal"));
                }

                break;
            case 1:
                numDifferentMonsters = 6;
                numFungus = 15;
                numMonstersInMap = 14;
                numFountains = 1;
                break;
            case 2:
                numDifferentMonsters = 7;
                numFungus = 20;
                numMonstersInMap = 15;
                numFountains = 1;
                break;
        }

        for (int i = 0; i < numFountains; i++)
        {
            trialMap.SpawnFountainInMap();
        }

        HashSet<string> invalidMonRefs = new HashSet<string>()
        {
            "mon_fungalcolumn",
            "mon_xp_mimic",
            "mon_xp_chameleon"
        };

        for (int i = 0; i < numDifferentMonsters; i++)
        {
            string mRefToAdd = GameMasterScript.masterSpawnableMonsterList[UnityEngine.Random.Range(0, GameMasterScript.masterSpawnableMonsterList.Count)].refName;
            MonsterTemplateData mTemplate = GameMasterScript.masterMonsterList[mRefToAdd];

            bool validForThisTrial =
                (trialLevel == 0 && mTemplate.baseLevel <= 7) ||
                (trialLevel == 1 && mTemplate.baseLevel >= 3 && mTemplate.baseLevel <= 10) ||
                (trialLevel == 2 && mTemplate.baseLevel >= 5);

            while (spawnTable.HasActor(mRefToAdd) || !validForThisTrial || invalidMonRefs.Contains(mRefToAdd))
            {
                mRefToAdd = GameMasterScript.masterSpawnableMonsterList[UnityEngine.Random.Range(0, GameMasterScript.masterSpawnableMonsterList.Count)].refName;
                mTemplate = GameMasterScript.masterMonsterList[mRefToAdd];
                validForThisTrial =
                                (trialLevel == 0 && mTemplate.baseLevel <= 7) ||
                                (trialLevel == 1 && mTemplate.baseLevel >= 3 && mTemplate.baseLevel <= 10) ||
                                (trialLevel == 2 && mTemplate.baseLevel >= 5);
            }
            spawnTable.AddToTable(mRefToAdd, UnityEngine.Random.Range(75, 125));
        }

        Vector2 center = new Vector2(trialMap.columns / 2f, trialMap.rows / 2f);

        for (int i = 0; i < numMonstersInMap; i++)
        {
            MapTileData emptyTile = trialMap.GetRandomEmptyTileForMapGen();
            int attempts = 0;
            while (MapMasterScript.GetGridDistance(emptyTile.pos,center) <= 6)
            {
                emptyTile = trialMap.GetRandomEmptyTileForMapGen();
                attempts++;
                if (attempts >1500)
                {
                    break;
                }
            }
            string mRef = spawnTable.GetRandomActorRef();
            int scaleLevel = BalanceData.monsterLevelByPlayerLevel[playerLevel];
            Monster spawnedMon = MonsterManagerScript.CreateMonster(mRef, false, false, false, 0f, false);
            if (scaleLevel > 1)
            {
                scaleLevel--;
            }
            spawnedMon.ScaleToSpecificLevel(scaleLevel, false);
            //spawnedMon.myStats.BoostStatByPercent(StatTypes.HEALTH, trialLevel * 0.05f);
            //spawnedMon.myStats.BoostCoreStatsByPercent(trialLevel * 0.05f);

            trialMap.PlaceActor(spawnedMon, emptyTile);
        }

        int[] numChampionsWithMods = new int[4]; // up to 4 mod champs

        switch (trialLevel)
        {
            case 0:
                numChampionsWithMods[1] = 1;
                break;
            case 1:
                numChampionsWithMods[1] = 1;
                numChampionsWithMods[0] = 2;
                break;
            case 2:
                numChampionsWithMods[2] = 1;
                numChampionsWithMods[1] = 1;
                numChampionsWithMods[0] = 1;
                break;
        }

        int monsterIndex = 0;
        for (int i = 0; i < numChampionsWithMods.Length; i++)
        {
            for (int y = 0; y < numChampionsWithMods[i]; y++)
            {
                Monster m = trialMap.monstersInMap[monsterIndex];                
                for (int x = 0; x <= i; x++)
                {
                    m.MakeChampion();
                }
                m.myInventory.GetInventory().Clear();
                monsterIndex++;
            }
        }

        if (GameStartData.CheckGameModifier(GameModifiers.MONSTERS_MIN_1POWER))
        {
            foreach(Monster m in trialMap.monstersInMap)
            {
                if (!m.isChampion && m.actorfaction != Faction.PLAYER)
                {
                    m.MakeChampion(false);
                }
            }
        }

        for (int i = 0; i < numFungus; i++)
        {
            MapTileData emptyTile = trialMap.GetRandomEmptyTileForMapGen();
            Monster newMon = MonsterManagerScript.CreateMonster("mon_fungalcolumn", false, false, false, 0f, false);
            trialMap.PlaceActor(newMon, emptyTile);
        }
    }

    static ActorTable CreateCustomSpawnTable(float cv)
    {
        ActorTable spawnTable = new ActorTable();        
        foreach(MonsterTemplateData m in GameMasterScript.masterSpawnableMonsterList)
        {
            if (m.challengeValue <= cv)
            {
                if (m.challengeValue >= cv-0.2f)
                {
                    spawnTable.AddToTable(m.refName, 5);
                }
                if (m.challengeValue >= cv - 0.1f)
                {
                    spawnTable.AddToTable(m.refName, 5);
                }
                if (m.challengeValue >= cv)
                {
                    spawnTable.AddToTable(m.refName, 5);
                }

            }
        }

        return spawnTable;
    }

	public static void PostDirtbeakFiller(Map processMap)
    {   
        int numExtraCratesAndBarrels = UnityEngine.Random.Range(15, 18);

        ActorTable breakables = processMap.GetBreakablesForThisFloor();

        for (int i = 0; i < numExtraCratesAndBarrels; i++)
        {
            MapTileData randomEmpty = processMap.GetRandomEmptyTileForMapGen();
            processMap.CreateDestructibleInTile(randomEmpty, breakables.GetRandomActorRef());
        }
    }

    public static void BuildSwampRooms(Map processMap)
    {
        foreach(Corridor c in processMap.mapCorridors)
        {
            foreach(MapTileData mtd in c.internalTiles)
            {
                mtd.AddTag(LocationTags.WATER);
            }
        }

        BuildSwampRoomsSecretAreas(processMap);

        BuildSwampWaterAndMud(processMap);
    }

    public static void BuildSwampWaterAndMud(Map processMap)
    {
        HashSet<MapTileData> seedTilesForWater = new HashSet<MapTileData>();
        List<MapTileData> allTilesConvertedToWater = new List<MapTileData>(250);
        int randomX;
        int randomY;

        // Pick some tiles to be seeds for water throughout the level. They can't be too close together.

        float startTime = Time.realtimeSinceStartup;

        int maxSeedTiles = processMap.dungeonLevelData.GetMetaData("seedtiles");
        int minDistanceBetweenSeeds = processMap.dungeonLevelData.GetMetaData("minseeddistance");
        float chanceSkipWater = processMap.dungeonLevelData.GetMetaData("chanceskipwater") / 100f;
        float mudChance = processMap.dungeonLevelData.GetMetaData("mudchance") / 100f;
        float chanceEdgeMuck = processMap.dungeonLevelData.GetMetaData("edgemuckchance") / 100f;
        int minPropogateCycles = processMap.dungeonLevelData.GetMetaData("minpropogatecycles");
        int maxPropogateCycles = processMap.dungeonLevelData.GetMetaData("maxpropogatecycles");

        bool[,] alreadyProcessedTiles = new bool[processMap.columns, processMap.rows];

        int safetyAttempts = 0;
        int maxAttempts = 10000;

        while (seedTilesForWater.Count <= maxSeedTiles)
        {
            safetyAttempts++;
            if (safetyAttempts >= maxAttempts) break;
            randomX = UnityEngine.Random.Range(4, processMap.columns - 4);
            randomY = UnityEngine.Random.Range(4, processMap.columns - 4);
            MapTileData mtd = processMap.mapArray[randomX, randomY];
            if (mtd.tileType != TileTypes.GROUND) continue;
            if (seedTilesForWater.Contains(mtd)) continue;

            bool valid = true;
            foreach(MapTileData existingMTD in seedTilesForWater)
            {
                float dist = MapMasterScript.GetGridDistance(mtd.pos, existingMTD.pos);
                if (dist <= minDistanceBetweenSeeds)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid) continue;
            
            seedTilesForWater.Add(mtd);
        }

        if (safetyAttempts >= maxAttempts)
        {
            if (Debug.isDebugBuild) Debug.Log("Failed to create all water seeds. " + seedTilesForWater.Count + " created vs max of " + maxSeedTiles + " " + processMap.floor);
        }

        // Now that we have seed tiles, convert each of these to water and build water around them a bunch of times.
        newSeedTiles = new List<MapTileData>(150);

        foreach(MapTileData seedWaterTile in seedTilesForWater)
        {
            PropogateWaterAroundCenterTile(seedWaterTile, processMap, minPropogateCycles, maxPropogateCycles, chanceSkipWater, alreadyProcessedTiles, allTilesConvertedToWater);
        }

        foreach(MapTileData water in allTilesConvertedToWater)
        {
            CustomAlgorithms.GetTilesAroundPoint(water.pos, 1, processMap);
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                if (CustomAlgorithms.tileBuffer[i].iPos.x == 0 || CustomAlgorithms.tileBuffer[i].iPos.y == 0 ||
                    CustomAlgorithms.tileBuffer[i].iPos.x == processMap.columns - 1 || CustomAlgorithms.tileBuffer[i].iPos.y == processMap.rows - 1) continue;

                if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.WATER) || CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.MUD)) continue;
                if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
                if (UnityEngine.Random.Range(0, 1f) > mudChance) continue;
                CustomAlgorithms.tileBuffer[i].AddTag(LocationTags.MUD);
            }
        }

        bool okToConvertWallsToMud = processMap.dungeonLevelData.GetMetaData("wallstomuck") == 1;

        // Now go along map edges and pepper with a bit of water or mud.
        MapTileData checkMTD;
        for (int x = 1; x < processMap.columns - 1; x++)
        {
            for (int y = 1; y < 4; y++)
            {
                checkMTD = processMap.mapArray[x, y];
                TryConvertNearEdgeTileToMuck(checkMTD, processMap, chanceEdgeMuck, okToConvertWallsToMud);
            }
            for (int y = processMap.rows-4; y < processMap.rows-1; y++)
            {
                checkMTD = processMap.mapArray[x, y];
                TryConvertNearEdgeTileToMuck(checkMTD, processMap, chanceEdgeMuck, okToConvertWallsToMud);
            }
        }
        for (int y = 1; y < processMap.rows - 1; y++)
        {
            for (int x = 1; x < 4; x++)
            {
                checkMTD = processMap.mapArray[x, y];
                TryConvertNearEdgeTileToMuck(checkMTD, processMap, chanceEdgeMuck, okToConvertWallsToMud);
            }
            for (int x = processMap.columns - 4; x < processMap.columns - 1; x++)
            {
                checkMTD = processMap.mapArray[x, y];
                TryConvertNearEdgeTileToMuck(checkMTD, processMap, chanceEdgeMuck, okToConvertWallsToMud);
            }
        }

        //Debug.Log("Finish: " + (Time.realtimeSinceStartup - startTime));
    }

    static void TryConvertNearEdgeTileToMuck(MapTileData checkMTD, Map processMap, float chanceEdgeMuck, bool okToConvertWallsToMud)
    {
        if (checkMTD.tileType != TileTypes.GROUND && !okToConvertWallsToMud) return;
        if (checkMTD.CheckTag(LocationTags.WATER) || checkMTD.CheckTag(LocationTags.MUD)) return;
        if (UnityEngine.Random.Range(0, 1f) <= chanceEdgeMuck)
        {
            if (UnityEngine.Random.Range(0, 3) == 0)
            {
                checkMTD.AddTag(LocationTags.MUD);
            }
            else
            {
                checkMTD.AddTag(LocationTags.WATER);
            }
        }
    }

    static void PropogateWaterAroundCenterTile(
        MapTileData seedWaterTile,
        Map processMap,
        int minPropogateCycles, 
        int maxPropogateCycles,
        float chanceSkipWater,
        bool[,] alreadyProcessedTiles,
        List<MapTileData> allTilesConvertedToWater)
    {

        List<Actor> toRemove = new List<Actor>();

        seedWaterTile.AddTag(LocationTags.WATER);
        newSeedTiles.Clear();
        int maxConversionCycles = UnityEngine.Random.Range(minPropogateCycles, maxPropogateCycles + 1);
        newSeedTiles.Add(seedWaterTile);
        for (int cycle = 0; cycle < maxConversionCycles; cycle++)
        {
            int initialCount = newSeedTiles.Count;
            for (int t = 0; t < initialCount; t++)
            {
                MapTileData tile = newSeedTiles[t];
                if (alreadyProcessedTiles[tile.iPos.x, tile.iPos.y]) continue;

                alreadyProcessedTiles[tile.iPos.x, tile.iPos.y] = true;

                // Find all tiles nearby that aren't water, and (probably) convert them to water.
                CustomAlgorithms.GetTilesAroundPoint(tile.pos, 1, processMap);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].iPos.x == 0 || CustomAlgorithms.tileBuffer[i].iPos.y == 0 ||
                        CustomAlgorithms.tileBuffer[i].iPos.x == processMap.columns - 1 || CustomAlgorithms.tileBuffer[i].iPos.y == processMap.rows - 1) continue;

                    if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.WATER)) continue;
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
                    if (UnityEngine.Random.Range(0, 1f) <= chanceSkipWater) continue;
                    CustomAlgorithms.tileBuffer[i].AddTag(LocationTags.WATER);
                    newSeedTiles.Add(CustomAlgorithms.tileBuffer[i]);
                    allTilesConvertedToWater.Add(CustomAlgorithms.tileBuffer[i]);
                }
            }
        }
    }

    public static void BuildSwampRoomsSecretAreas(Map processMap)
    {
        // Create some hidden rooms that are not adjacent to any existing rooms.

        int numHiddenRooms = 4;
        int minHiddenRoomSize = 5;
        int maxHiddenRoomSize = 7;

        MapTileData checkOverlapTile = null;

        for (int i = 0; i < numHiddenRooms; i++)
        {
            int attempts = 0;

            while (attempts < 5000)
            {
                attempts++;
                int originX = UnityEngine.Random.Range(1, processMap.columns - maxHiddenRoomSize);
                int originY = UnityEngine.Random.Range(1, processMap.rows - maxHiddenRoomSize);
                int roomSizeX = UnityEngine.Random.Range(minHiddenRoomSize, maxHiddenRoomSize + 1);
                int roomSizeY = UnityEngine.Random.Range(minHiddenRoomSize, maxHiddenRoomSize + 1);

                bool valid = true;

                // Check overlap slightly beyond room bounds, so that we can do cool stuff to room bounds as needed
                for (int x = originX - 1; x < originX + roomSizeX + 2; x++)
                {
                    for (int y = originY - 1; y < originY + roomSizeY + 2; y++)
                    {
                        if (x <= 0 || y <= 0 || x >= processMap.columns - 1 || y >= processMap.rows-1)
                        {
                            valid = false;
                            break;
                        }
                        checkOverlapTile = processMap.mapArray[x, y];
                        if (checkOverlapTile.tileType == TileTypes.GROUND)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) break;
                }

                if (!valid) continue;

                // OK, we have a vacant area. Build internal tiles.

                Room r = new Room();
                r.InitializeLists();
                processMap.AddAreaToDictionary(r);
                r.areaType = AreaTypes.ROOM;
                r.origin = new Vector2(originX, originY);
                r.size = new Vector2(roomSizeX, roomSizeY);

                for (int x = originX; x < originX + roomSizeX + 1; x++)
                {
                    for (int y = originY; y < originY + roomSizeY + 1; y++)
                    {
                        checkOverlapTile = processMap.mapArray[x, y];
                        if (x == originX || x == originX + roomSizeX || y == originY || y == originY + roomSizeY)
                        {
                            // This is edge tile.                            
                            r.edgeTiles.Add(checkOverlapTile);
                            checkOverlapTile.ChangeTileType(TileTypes.WALL);
                        }
                        else
                        {
                            // This is internal tile
                            r.internalTiles.Add(checkOverlapTile);
                            checkOverlapTile.ChangeTileType(TileTypes.GROUND);
                        }
                        processMap.areaIDArray[x, y] = r.areaID;

                        // Mark the tiles in the room (temporarily) as secret room tiles
                        // We can connect to them later
                        checkOverlapTile.AddMapTag(MapGenerationTags.SECRETSTUFF);
                    }
                }

                MapTileData randomTileForMud = r.internalTiles[UnityEngine.Random.Range(0, r.internalTiles.Count)];
                randomTileForMud.AddTag(LocationTags.MUD); // we'll need at least one tile to connect teleporter!

                processMap.mapRooms.Add(r);
                break; // Move on to next room iteration
            }

            if (attempts >= 5000)
            {
#if UNITY_EDITOR
                //Debug.Log("Failed to make room on floor " + processMap.floor);
#endif
            }
        }        
    }

    public static void SeedMapWithMonsterEggs(Map processMap)
    {
        // Destructibles that, when attacked, release a FRIENDLY monster
        int numEggs = processMap.dungeonLevelData.GetMetaData("monstereggs");

        string monsterEggReference = "obj_friendlymonster_egg";

        Vector2 centerPos = new Vector2(processMap.columns / 2, processMap.rows / 2);
        Vector2 spawnPosAsVector = Vector2.one;

        List<Destructible> existingEggs = new List<Destructible>();

        int minDistanceBetweenEggs = 3;

        for (int i = 0; i < numEggs; i++)
        {
            int spawnX = UnityEngine.Random.Range(2, processMap.columns - 2);
            int spawnY = UnityEngine.Random.Range(2, processMap.rows - 2);
            MapTileData checkTile = processMap.mapArray[spawnX, spawnY];
            spawnPosAsVector = new Vector2(spawnX, spawnY);

            bool tileValid = false;

            while (!tileValid)
            {
                spawnX = UnityEngine.Random.Range(2, processMap.columns - 2);
                spawnY = UnityEngine.Random.Range(2, processMap.rows - 2);
                checkTile = processMap.mapArray[spawnX, spawnY];
                spawnPosAsVector = new Vector2(spawnX, spawnY);

                tileValid = true;

                if (checkTile.tileType != TileTypes.GROUND || !checkTile.IsEmpty()
                    || MapMasterScript.GetGridDistance(centerPos, spawnPosAsVector) < 4)
                {
                    tileValid = false;
                }

                if (tileValid)
                {
                    foreach(Destructible egg in existingEggs)
                    {
                        if (MapMasterScript.GetGridDistance(egg.GetPos(), checkTile.pos) < minDistanceBetweenEggs)
                        {
                            tileValid = false;
                            break;
                        }
                    }
                }
            }

            Destructible dt = processMap.CreateDestructibleInTile(checkTile, monsterEggReference);
            existingEggs.Add(dt);
        }
    }

    public static void SeedMapWithHiddenMonstersAndExtras(Map processMap)
    {
        // Goal: Create some WARPS from secret area mud tiles to regular tiles. They must be connected.

        List<Room> secretRooms = new List<Room>();
        List<Room> nonSecretRooms = new List<Room>();

        foreach(Room r in processMap.mapRooms)
        {
            if (r.internalTiles.Count > 0 && r.internalTiles[0].CheckMapTag(MapGenerationTags.SECRETSTUFF))
            {
                secretRooms.Add(r);
            }
            else
            {
                nonSecretRooms.Add(r);
            }
        }

        foreach(Room r in secretRooms)
        {
            if (nonSecretRooms.Count == 0)
            {
                // We're done here, no more rooms to connect to.
                break;
            }

            // Find a mud tile in this secret area, and build a warp on it.
            MapTileData secretMudTile = null;

            secretMudTile = FindMudTileInRoom(r);

            if (secretMudTile == null || !secretMudTile.CheckTag(LocationTags.MUD))
            {
#if UNITY_EDITOR
                Debug.Log("Failed to find mud tile in secret room");
#endif
                continue; // Something failed here.            
            }

            // Now find another mud tile in a NORMAL room.

            Room randomNormalRoom = null;
            MapTileData nonSecretMudTile = null;

            while (nonSecretRooms.Count > 0)
            {
                randomNormalRoom = nonSecretRooms[UnityEngine.Random.Range(0, nonSecretRooms.Count)];
                while (randomNormalRoom.internalTiles.Count == 0)
                {
                    nonSecretRooms.Remove(randomNormalRoom); // we don't want to use rooms with 0 internal tiles
                    if (nonSecretRooms.Count == 0)
                    {
                        break;
                    }
                    randomNormalRoom = nonSecretRooms[UnityEngine.Random.Range(0, nonSecretRooms.Count)];
                }

                nonSecretMudTile = FindMudTileInRoom(randomNormalRoom);

                if (nonSecretMudTile == null || !nonSecretMudTile.CheckTag(LocationTags.MUD))
                {
                    nonSecretRooms.Remove(randomNormalRoom);
                    continue;
                }

                // We found a room with a mud tile, great!
                break;
            }

            if (nonSecretRooms.Count == 0)
            {
                break;
            }

            // Teleporter 1 goes FROM the secret area TO the normal room
            Destructible teleporter1 = processMap.CreateDestructibleInTile(secretMudTile, "exp1_mudteleport");

            // Destination tile of teleporter 2 --> 1
            MapTileData nearTeleport1 = processMap.GetRandomEmptyTile(teleporter1.GetPos(), 1, true, anyNonCollidable: true, preferLOS: false, avoidTilesWithPowerups: false, excludeCenterTile: true);

            // Teleporter 2 goes FROM the normal room TO the secret area
            Destructible teleporter2 = processMap.CreateDestructibleInTile(nonSecretMudTile, "exp1_mudteleport");

            // Destination tile of teleporter 1 --> 2
            MapTileData nearTeleport2 = processMap.GetRandomEmptyTile(teleporter2.GetPos(), 1, true, anyNonCollidable: true, preferLOS: false, avoidTilesWithPowerups: false, excludeCenterTile: true);

            nonSecretRooms.Remove(randomNormalRoom);
            
            teleporter1.SetActorData("teleport_dest_x", nearTeleport2.iPos.x);
            teleporter1.SetActorData("teleport_dest_y", nearTeleport2.iPos.y);
            teleporter2.SetActorData("teleport_dest_x", nearTeleport1.iPos.x);
            teleporter2.SetActorData("teleport_dest_y", nearTeleport1.iPos.y);

            // Lastly, in the secret room, add a buried or visible treasure, plus a Big Boy champion

            int attempts = 0;
            MapTileData tileForTreasure = null;
            attempts = 0;
            while (true) // sweatdrop
            {
                attempts++;
                if (attempts > 500) break;
                tileForTreasure = r.internalTiles[UnityEngine.Random.Range(0, r.internalTiles.Count)];
                if (tileForTreasure.tileType != TileTypes.GROUND) continue;
                if (tileForTreasure.CheckActorRef("hiddenmonstertrap")) continue;                
                break;
            }


            if (attempts >= 500)
            {
                Debug.Log("No tile for treasure in secret room.");
                continue;
            }

            // codereview shep: The meat of this next block only happens on a random roll, so why not wrap all this in
            // an if() based on that roll? Saves you the 500 iteration loop during map gen.
            
            attempts = 0;
            MapTileData tileForMon = null;
            while (true) // sweatdrop
            {
                attempts++;
                if (attempts > 500) break;
                tileForMon = r.internalTiles[UnityEngine.Random.Range(0, r.internalTiles.Count)];
                if (tileForMon.tileType != TileTypes.GROUND) continue;
                if (tileForMon.CheckActorRef("hiddenmonstertrap")) continue;
                if (tileForMon.IsCollidable(GameMasterScript.genericMonster)) continue;
                if (tileForMon.Equals(tileForTreasure)) continue;
                break;
            }

            if (tileForMon != null && UnityEngine.Random.Range(0,3) != 1) // 66% chance of champion spawn
            {
                Monster champForRoom = MonsterManagerScript.CreateMonster(processMap.dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
                processMap.OnEnemyMonsterSpawned(processMap, champForRoom, false);
                champForRoom.MakeChampion();
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    champForRoom.MakeChampion(); // 50% chance of 2 mod
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        champForRoom.MakeChampion(); // 25% (total) chance of 3 mod
                    }
                }
            }

            Destructible dt = processMap.CreateDestructibleInTile(tileForTreasure, "obj_ornatechest");

            /* if (tileForTreasure.CheckTag(LocationTags.MUD))
            {
                Destructible dt = processMap.CreateDestructibleInTile(tileForTreasure, "hiddenmonstertrap");
                dt.SetActorDataString("containedactortype", "chest");
            }
            else
            {
                
            } */
            
        }

        SeedMapWithHiddenMonsters(processMap);
    }

    static MapTileData FindMudTileInRoom(Room r)
    {
        MapTileData mudTile = null;
        int attempts = 0;
        if (r.internalTiles.Count == 0) return null;
        while (true) // sweatdrops
        {
            mudTile = r.internalTiles[UnityEngine.Random.Range(0, r.internalTiles.Count)];
            if (mudTile.CheckTag(LocationTags.MUD))
            {
                break;
            }
            attempts++;
            if (attempts >= 500)
            {
                break;
            }
        }
        return mudTile;
    }

    public static void SeedMapWithHiddenMonsters(Map processMap)
    {
        int numHiddenMonsters = processMap.dungeonLevelData.GetMetaData("numhiddenmonsters");
        int numHiddenChests = processMap.dungeonLevelData.GetMetaData("numhiddenchests");
        List<MapTileData> existingSpawns = new List<MapTileData>(numHiddenMonsters);
        int minDistanceOfSpawns = processMap.dungeonLevelData.GetMetaData("minmonsterdistance");
        bool monstersPlaced = false;
        bool chestsPlaced = false;

        while (!monstersPlaced || !chestsPlaced)
        {
            // First, place monsters. Then, place chests.

            int maxToPlace = 0;
            string containedActorType = "";
            if (!monstersPlaced)
            {
                maxToPlace = numHiddenMonsters;
                containedActorType = "monster";
            }
            else
            {
                maxToPlace = numHiddenChests;
                containedActorType = "chest";
            }
            existingSpawns.Clear();

            for (int i = 0; i < maxToPlace; i++)
            {
                MapTileData mtd = null;
                bool foundTile = false;
                while (!foundTile)
                {
                    int x = UnityEngine.Random.Range(1, processMap.columns - 1);
                    int y = UnityEngine.Random.Range(1, processMap.rows - 1);
                    mtd = processMap.mapArray[x, y];
                    if (mtd.tileType != TileTypes.GROUND) continue;
                    if (!mtd.CheckTag(LocationTags.MUD)) continue;
                    bool tileValid = true;
                    foreach (MapTileData existing in existingSpawns)
                    {
                        float dist = MapMasterScript.GetGridDistance(mtd.pos, existing.pos);
                        if (dist < minDistanceOfSpawns)
                        {
                            tileValid = false;
                            break;
                        }
                    }

                    if (tileValid)
                    {
                        // Good to create our spawner!
                        Destructible dt = processMap.CreateDestructibleInTile(mtd, "hiddenmonstertrap");
                        dt.SetActorDataString("containedactortype", containedActorType);
                        existingSpawns.Add(mtd);
                        foundTile = true;
                    }
                }
            }

            if (!monstersPlaced)
            {
                monstersPlaced = true;
                continue;
            }
            if (!chestsPlaced) chestsPlaced = true;
        }

    }

    public static void MarkMonstersAsChampions(Map processMap)
    {
        foreach(Monster m in processMap.monstersInMap)
        {
            if (!m.isBoss && !m.isChampion)
            {
                m.isChampion = true;
                if (m.championMods == null)
                {
                    m.championMods = new List<ChampionMod>();
                }
            }
        }
    }

    public static void SpawnGoldfrogPerFloor(Map processMap)
    {        
        Monster goldfrog = MonsterManagerScript.CreateMonster("mon_goldfrog", true, true, false, processMap.challengeRating, false);
        goldfrog.myStats.SetLevel(BalanceData.GetMonsterLevelByCV(processMap.challengeRating, false));
        goldfrog.myStats.SetStat(StatTypes.HEALTH, BalanceData.expectedMonsterHealth[goldfrog.myStats.GetLevel()], StatDataTypes.ALL, true);
        processMap.PlaceActorAtRandom(goldfrog);
    }

    public static void EnforceMinimumNumberOfChampions(Map processMap)
    {
        int numChampions = 0;
        foreach(Monster m in processMap.monstersInMap)
        {
            if (m.isChampion) numChampions++;
        }
        if (numChampions < processMap.dungeonLevelData.maxChampions)
        {
            processMap.monstersInMap.Shuffle();
            int processIndex = 0;
            int numMonstersInMap = processMap.monstersInMap.Count;
            for (int i = numChampions; i <= processMap.dungeonLevelData.maxChampions; i++)
            {
                for (int x = processIndex; x < numMonstersInMap; x++)
                {
                    Monster m = processMap.monstersInMap[x];
                    if (m.isChampion) continue;
                    m.MakeChampion();
                    for (int numMods = 1; numMods < processMap.dungeonLevelData.maxChampionMods; numMods++)
                    {
                        if (UnityEngine.Random.Range(0, 3) == 0) break;
                        m.MakeChampion();
                    }
                    numChampions++;
                    processIndex = x;
                    break;
                }                
            }
        }
    }

    public static void AddExtraChestsWithGear(Map processMap)
    {
        ActorTable possibleChests = new ActorTable();
        possibleChests.AddToTable("obj_smallwoodencrate", 50);
        possibleChests.AddToTable("obj_mediumwoodencrate", 70);
        possibleChests.AddToTable("obj_largewoodencrate", 100);
        possibleChests.AddToTable("obj_smallwoodenchest", 100);
        possibleChests.AddToTable("obj_largewoodenchest", 70);
        possibleChests.AddToTable("obj_ornatechest", 50);
        // How many chests? Depends on size of map

        int numChests = processMap.columns / 12;

#if UNITY_EDITOR
        //Debug.Log("Adding " + numChests + " chests to floor " + processMap.floor);
#endif

        bool addedScroll = false;

        for (int i = 0; i < numChests; i++)
        {
            MapTileData emptyTile = null;
            bool tileFound = false;
            int attempts = 0;
            while (!tileFound)
            {
                attempts++;
                if (attempts > 100)
                {
                    break;
                }
                emptyTile = processMap.GetRandomEmptyTileForMapGen();
                if (emptyTile.GetStairsInTile() == null)
                {
                    tileFound = true;
                }
            }

            string chestRef = possibleChests.GetRandomActorRef();

            Destructible template = Destructible.FindTemplate(chestRef);
            Destructible act = DTPooling.GetDestructible();
            act.CopyFromTemplate(template);
            processMap.PlaceActor(act, emptyTile);

            float runningChance = template.lootChance;
            if (runningChance < 1.0f) runningChance = 1f;

            int nItems = template.maxItems;
            if (nItems == 0) nItems = 1;

            for (int x = 0; x < template.maxItems; x++)
            {
                if (UnityEngine.Random.Range(0,1f) > runningChance)
                {
                    continue;
                }

                float baseValue = processMap.GetChallengeRating();
                baseValue += template.bonusLootValue;

                float bonusMagicValue = template.bonusMagicChance + UnityEngine.Random.Range(0.2f, 0.5f);
                float magicValue = (GameMasterScript.gmsSingleton.globalMagicItemChance + bonusMagicValue + MysteryDungeonManager.EXTRA_MAGIC_ITEM_CHANCE) * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.MAGIC_ITEM_CHANCE);
                magicValue += 1.2f; // magic value multiplier is supposed to be above 1, come on now.

                Item itm = LootGeneratorScript.GenerateLootFromTable(baseValue, magicValue, "equipment", minimumCV: baseValue-0.3f);

#if UNITY_EDITOR
                //Debug.Log("Created: " + itm.displayName + " with cv " + itm.challengeValue + " on floor " + processMap.floor + " for " + act.actorUniqueID + " lootvalue? " + baseValue + " magicvalue? " + magicValue);
#endif

                runningChance *= 0.5f;

                act.myInventory.AddItemRemoveFromPrevCollection(itm, false);
            }            
            
            if (!addedScroll && MysteryDungeonManager.InOrCreatingMysteryDungeon())
            {
                if (processMap.floor % 10 == 0)
                {
                    Item scroll = LootGeneratorScript.CreateItemFromTemplateRef("scroll_jobchange", 1f, 0f, false, true);
                    act.myInventory.AddItemRemoveFromPrevCollection(scroll, false);
                    addedScroll = true;
                }
            }
        }
    }

    public static void ConvertWallsToHolesForMountainGrass(Map processMap)
    {
        float chanceConvertSingleWallToTree = 0.6f;
        float chanceConvertAlmostSingleWallToTree = 0.3f;

        HashSet<MapTileData> tilesForTrees = new HashSet<MapTileData>();

        int numWallsSurrounding = 0;

        // First, check for one-off wall tiles and consider replacing with trees or bushes (wall -> ground + destructible)
        for (int x = 2; x < processMap.columns - 2; x++)
        {
            for (int y = 2; y < processMap.rows -2; y++)
            {
                MapTileData mtd = processMap.mapArray[x, y];
                numWallsSurrounding = 0;
                if (mtd.tileType == TileTypes.WALL)
                {
                    // Count all tiles around this, see if there is any wall nearby.
                    CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, processMap);
                    bool anyWallSurrounding = false;
                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        if (tilesForTrees.Contains(CustomAlgorithms.tileBuffer[i])) // don't put a tree right near another tree
                        {
                            numWallsSurrounding += 9;
                            break;
                        }
                        if (CustomAlgorithms.tileBuffer[i] == mtd) continue;
                        if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                        {
                            numWallsSurrounding++;
                            if (numWallsSurrounding > 1) break;
                        }
                    }
                    if (numWallsSurrounding == 0 && UnityEngine.Random.Range(0,1f) <= chanceConvertSingleWallToTree ||
                        numWallsSurrounding == 1 && UnityEngine.Random.Range(0, 1f) <= chanceConvertAlmostSingleWallToTree)
                    {
                        tilesForTrees.Add(mtd);
                        mtd.ChangeTileType(TileTypes.GROUND);
                    }
                }
            }
        }

        processMap.ConvertAllWallTilesToHoles(MapTileData.MOUNTAINGRASS_TILE_INDICES[0]);

        //Debug.Log(processMap.floor + " has tiles for trees count: " + tilesForTrees.Count);

        foreach(MapTileData mtd in tilesForTrees)
        {
            mtd.ChangeTileType(TileTypes.WALL);
            mtd.AddTag(LocationTags.TREE);
            mtd.GetWallReplacementIndex();
        }
    }

    public static void AddWaterPoolsAndIce(Map processMap)
    {
        HashSet<MapTileData> tilesValidForIce = new HashSet<MapTileData>();

        HashSet<MapTileData> seedTilesForWater = new HashSet<MapTileData>();

        HashSet<MapTileData> tilesConvertedToWater = new HashSet<MapTileData>();

        float seedWaterChance = 0.04f;

        for (int x = 2; x < processMap.columns - 2; x++)
        {
            for (int y = 2; y < processMap.rows -2; y++)
            {
                MapTileData checkTile = processMap.mapArray[x, y];
                if (checkTile.tileType != TileTypes.GROUND) continue;
                if (UnityEngine.Random.Range(0,1f) <= seedWaterChance)
                {
                    seedTilesForWater.Add(checkTile);
                }
            }
        }

        HashSet<MapTileData> localTileList = new HashSet<MapTileData>();

        float convertNeighborTileToWaterChance = 0.4f;

        foreach(MapTileData mtd in seedTilesForWater)
        {
            mtd.AddTag(LocationTags.WATER);
            tilesConvertedToWater.Clear();
            localTileList.Clear();
            localTileList.Add(mtd);
            for (int x = 0; x < 2; x++) // Two cycles of converting surrounding tiles to water
            {
                foreach(MapTileData localTile in localTileList)
                {
                    CustomAlgorithms.GetTilesAroundPoint(localTile.pos, 1, processMap);
                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        if (UnityEngine.Random.Range(0, 1f) > convertNeighborTileToWaterChance) continue;
                        if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.WATER)) continue;
                        CustomAlgorithms.tileBuffer[i].AddTag(LocationTags.WATER);
                        if (CustomAlgorithms.tileBuffer[i].tileType != TileTypes.GROUND)
                        {
                            CustomAlgorithms.tileBuffer[i].ChangeTileType(TileTypes.GROUND);
                        }
                        tilesConvertedToWater.Add(mtd);
                    }
                }
                if (x == 1)
                {
                    // we're done, we already did one iteration
                    break;
                }
                localTileList.Clear();
                foreach(MapTileData copyMTD in tilesConvertedToWater)
                {
                    localTileList.Add(copyMTD);
                }
                tilesConvertedToWater.Clear();
            }
        }

        for (int x = 1; x < processMap.columns - 1; x++)
        {
            for (int y = 1; y < processMap.rows -1; y++)
            {
                MapTileData checkTile = processMap.mapArray[x, y];
                if (!TileValidForIce(checkTile, tilesValidForIce, false)) continue;

                CustomAlgorithms.GetNonCollidableTilesAroundPoint(checkTile.pos, 1, GameMasterScript.genericMonster, processMap);
                for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
                {
                    if (!TileValidForIce(CustomAlgorithms.nonCollidableTileBuffer[i], tilesValidForIce, true)) continue;
                    tilesValidForIce.Add(CustomAlgorithms.nonCollidableTileBuffer[i]);
                }
            }
        }

        float chanceOfIce = 0.115f;

        foreach(MapTileData mtd in tilesValidForIce)
        {
            if (UnityEngine.Random.Range(0, 1f) > chanceOfIce) continue;
            // Good to create ice!
            processMap.CreateDestructibleInTile(mtd, "obj_dungeoniceblock");
        }
    }

    public static bool TileValidForIce(MapTileData checkTile, HashSet<MapTileData> tilesValidForIce, bool allowNonWater)
    {
        if (checkTile.tileType != TileTypes.GROUND) return false;
        if (checkTile.IsCollidableActorInTile(GameMasterScript.genericMonster)) return false;
        if (!checkTile.CheckTag(LocationTags.WATER) && !allowNonWater) return false; // on our FIRST pass, we use water tiles as seeds. but ice can surround them.
        if (tilesValidForIce.Contains(checkTile)) return false;

        return true;
    }

    /// <summary>
    /// Attempts to build a variety of small 2x2 or 3x3 *unconnected* rooms to fill in the map and give the player something to jump to. Put a little treasure on each.
    /// </summary>
    /// <param name="processMap"></param>
    public static void InsertSmallDugoutAreas(Map processMap)
    {
        float timeAtStart = Time.realtimeSinceStartup;

        int numGroundTiles = processMap.GetNumGroundTiles();
        // 2x2 room takes up 4 tiles, 3x3 takes up 9 tiles
        // we can't touch anything else with these rooms, so
        // we might expect to convert *some* of these tiles.

        int tilesToTryAndConvert = numGroundTiles / 3;
        int tilesConverted = 0;
        int attempts = 0;
        int maxAttempts = 6000;

        bool[,] invalidTiles = new bool[processMap.columns, processMap.rows];

        // debug only
        int failuresAtStart = 0;
        int failuresAtOverlapping = 0;
        int failuresAtRoomCreate = 0;

        while (tilesConverted < tilesToTryAndConvert && attempts < maxAttempts)
        {
            attempts++;

            // Pick size and origin at random

            int sizeX = UnityEngine.Random.Range(4, 6);
            int sizeY = UnityEngine.Random.Range(4, 6);

            int maxX = processMap.columns - sizeX - 1;
            int maxY = processMap.rows - sizeY - 1;

            int originX = UnityEngine.Random.Range(1, maxX);
            int originY = UnityEngine.Random.Range(1, maxY);

            int endX = originX + sizeX;
            int endY = originY + sizeY;

            // Make sure none of the tiles would collide with anything

            bool valid = true;

            for (int x = originX; x <= endX; x++)
            {
                for (int y = originY; y <= endY; y++)
                {
                    if (invalidTiles[x,y])
                    {
                        failuresAtStart++;
                        valid = false;
                        break;
                    }
                    if (processMap.mapArray[x,y].tileType == TileTypes.GROUND)
                    {
                        invalidTiles[x, y] = true;
                        failuresAtStart++;
                        valid = false;
                        break;
                    }
                }
                if (!valid)
                {
                    break;
                }
            }

            if (!valid) // we're overlapping with something. Try Again!
            {
                continue;
            }

            Map.RoomCreationResult rcr = processMap.CreateRoom(originX, originY, sizeX, sizeY, processMap.mapAreaID, false, false, false);
            if (!rcr.success)
            {
                failuresAtRoomCreate++;
                continue;
            }

            // Well, we're good now!
            //Debug.Log("Building mini room at " + originX + "," + originY + " " + sizeX + "," + sizeY + " ! Cute!");

            rcr.roomCreated.InitializeLists();
            processMap.AddAreaToDictionary(rcr.roomCreated);

            for (int x = originX; x <= endX; x++)
            {
                for (int y = originY; y<= endY; y++)
                {
                    TileTypes tTypeToUse = TileTypes.GROUND;
                    if (x == originX || x == endX || y == originY || y == endY)
                    {
                        tTypeToUse = TileTypes.WALL;
                    }
                    if (processMap.GetAreaID(new Vector2(x,y)) == -777)
                    {
                        processMap.mapArray[x, y].ChangeTileType(tTypeToUse);
                        processMap.SetTileAreaID(processMap.mapArray[x, y], rcr.roomCreated);
                        if (tTypeToUse == TileTypes.GROUND)
                        {
                            rcr.roomCreated.internalTiles.Add(processMap.mapArray[x, y]);
                        }
                        else
                        {
                            rcr.roomCreated.edgeTiles.Add(processMap.mapArray[x, y]);
                        }                        
                    }                    
                }
            }

            MapTileData randomInternalTile = rcr.roomCreated.internalTiles.GetRandomElement();

            randomInternalTile.ChangeTileType(TileTypes.WALL);
            randomInternalTile.AddTag(LocationTags.TREE);
            randomInternalTile.AddMapTag(MapGenerationTags.DONT_CONVERT_TO_HOLE);
            randomInternalTile.GetWallReplacementIndex();

            processMap.mapRooms.Add(rcr.roomCreated);
            tilesConverted += (sizeX * sizeY);

            // do treasure?
        }

        //Debug.Log("THIS TOOK " + (Time.realtimeSinceStartup - timeAtStart) + " with " + attempts + " and " + tilesConverted + " vs " + tilesToTryAndConvert + " start failures? " + failuresAtStart + " overlap? " + failuresAtOverlapping + " RCR? " + failuresAtRoomCreate);

    }

    public static void VerifyStairsAreAccessible(Map processMap)
    {
        foreach(Stairs st in processMap.mapStairs)
        {
            bool valid = false;
            CustomAlgorithms.GetTilesAroundPoint(st.GetPos(), 2, processMap);
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                MapTileData mtd = CustomAlgorithms.tileBuffer[i];
                if (mtd.tileType == TileTypes.GROUND)
                {
                    valid = true;
                    break;
                }
                if (valid)
                {
                    break;
                }
            }

            if (!valid)
            {
                // just open up all tiles around it, I guess, unless they're edge tiles
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    MapTileData mtd = CustomAlgorithms.tileBuffer[i];
                    if (mtd.pos.x > 0 && mtd.pos.y > 0 && mtd.pos.x < processMap.columns-1 && mtd.pos.y < processMap.rows - 1)
                    {
                        mtd.ChangeTileType(TileTypes.GROUND);
                    }
                }
            }

            if (processMap.floor < MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR && processMap.floor > MapMasterScript.BANDIT_DRAGON_DUNGEONEND_FLOOR)
            {
                bool accessible = processMap.FloodFillToSeeIfGivenTileIsConnectedToBiggestCavern(processMap.GetTile(st.GetPos()));
            }
            
        }
    }

    public static void AddBlessedPoolsToWater(Map processMap)
    {
        for (int x = 1; x < processMap.columns-1; x++)
        {
            for (int y= 1; y < processMap.rows-1; y++)
            {
                if (processMap.mapArray[x,y].CheckTag(LocationTags.WATER))
                {
                    processMap.CreateDestructibleInTile(processMap.mapArray[x, y], "bpst");
                }
            }
        }
    }
        
        
}
