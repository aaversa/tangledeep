using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map_BanditDungeonHub : Map
{
    List<Room> objectiveRooms;
    List<Room> allTreasureRooms;
    List<MapTileData> hubRoomCorners;
    Room hubRoom;

    int startX;
    int endX;
    int startY;
    int endY;

    List<Destructible> finalUnlockGates;    

    const float percentTreasureConnectionsConvertedToSpikes = 0.18f;

    public override bool BuildRandomMap(ItemWorldMetaData itemWorldProperties)
    {
        return base.BuildRandomMap(itemWorldProperties);
    }

    protected override void Initialize(int mFloor)
    {
        base.Initialize(mFloor);
        linkSwitchesToGates = new Dictionary<int, List<Destructible>>();
        finalUnlockGates = new List<Destructible>();
    }

    public Map_BanditDungeonHub(int mFloor)
    {
        Initialize(mFloor);
    }

    public bool BuildBanditDungeonHub()
    {
        float startTime = Time.realtimeSinceStartup;

        BuildCenterHubRoom();

        BuildObjectiveRooms();

        DigFromHubRoomToObjectiveRooms();

        CreateTreasureRooms();

        // For treasure rooms, we never want to dig from room tiles. Mark these. We only want connections from corridors.
        foreach (Room rm in mapRooms)
        {
            foreach (MapTileData mtd in rm.internalTiles)
            {
                mtd.AddMapTag(MapGenerationTags.NOCONNECTION);
            }
            foreach (MapTileData mtd in rm.edgeTiles)
            {
                mtd.AddMapTag(MapGenerationTags.NOCONNECTION);
            }
        }

        TunnelToTreasureRooms();

        TunnelFromObjectiveRoomsToEachother();

        // Any opened tiles along the edges of our treasure rooms should be gated off
        int treasureUnlockCount = 0;
        for (int i = 0; i < allTreasureRooms.Count; i++)
        {
            Room rm = allTreasureRooms[i];
            foreach (MapTileData edgeTile in rm.edgeTiles)
            {
                if (edgeTile.tileType != TileTypes.GROUND) continue;
                Destructible gate = CreateDestructibleInTile(edgeTile, "obj_metalgate");
                gate.SetActorData("gateindex", treasureUnlockCount);
            }

            //Debug.Log("Created gates in room " + i + " with unlock count " + treasureUnlockCount);

            treasureUnlockCount++;

            if (i == allTreasureRooms.Count-1)
            {
                // don't create a switch in THIS room pls
                continue;
            }

            // Create a switch that unlocks the NEXT treasure room's gates.

            MapTileData tileForSwitch = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];

            while (tileForSwitch.tileType != TileTypes.GROUND || !tileForSwitch.IsEmpty())
            {
                tileForSwitch = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            }

            Destructible floorSwitch = CreateDestructibleInTile(tileForSwitch, "obj_floorswitch");
            floorSwitch.SetActorData("floorswitch_index", treasureUnlockCount);

            //Debug.Log("Room " + i + " has a switch pointing to gates " + treasureUnlockCount);
        }

        AddLootToTreasureRooms();

        CreateGatekeepersInCorners();

        return true;

    }

    public void TunnelFromObjectiveRoomsToEachother()
    {
        // original order of objective rooms:
        // bottom left, top left, bottom right, top right

        // Tunnel from each room to the next, in order. Convert tiles along the way to ground.

        List<Room> orderedObjectiveRooms = new List<Room>()
        {
            objectiveRooms[0],
            objectiveRooms[1],
            objectiveRooms[3],
            objectiveRooms[2]
        };

        Room startRoom = null;
        Room finishRoom = null;

        for (int r = 0; r < orderedObjectiveRooms.Count; r++)
        {
            startRoom = orderedObjectiveRooms[r];
            if (r < orderedObjectiveRooms.Count-1)
            {
                finishRoom = orderedObjectiveRooms[r + 1];
            }
            else
            {
                finishRoom = orderedObjectiveRooms[0]; // final room links back to first room
            }
            Vector2 start = startRoom.origin + (startRoom.size / 2f);
            Vector2 finish = finishRoom.origin + (finishRoom.size / 2f);
            //Debug.Log("Tunnel from " + start + " to " + finish);
            CustomAlgorithms.GetPointsOnLineNoGarbage(start, finish);            
            for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
            {
                MapTileData tileToDig = mapArray[(int)CustomAlgorithms.pointsOnLine[i].x, (int)CustomAlgorithms.pointsOnLine[i].y];
                tileToDig.ChangeTileType(TileTypes.GROUND);
            }
        }
                
    }

    public List<MapTileData> BuildCenterHubRoom()
    {
        // Build hub room in the center

        hubRoomCorners = new List<MapTileData>();

        hubRoom = new Room();
        hubRoom.InitializeLists();
        int roomSize = UnityEngine.Random.Range(11, 13);
        startX = (columns / 2) - (roomSize / 2);
        startY = (rows / 2) - (roomSize / 2);
        endX = startX + roomSize;
        endY = startY + roomSize;
        hubRoom.origin = new Vector2(startX, startY);
        hubRoom.size = new Vector2(roomSize, roomSize);

        // corners will be added in order:
        // bottom left, top left, bottom right, top right

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if ((x == startX && y == startY) || (x == endX && y == startY) ||
                    (x == startX && y == endY) || (x == endX && y == endY))
                {
                    hubRoomCorners.Add(mapArray[x, y]);
                }

                if (x == startX || x == endX || y == startY || y == endY)
                {
                    mapArray[x, y].ChangeTileType(TileTypes.WALL);
                    hubRoom.edgeTiles.Add(mapArray[x, y]);
                }
                else
                {
                    mapArray[x, y].ChangeTileType(TileTypes.GROUND);
                    hubRoom.internalTiles.Add(mapArray[x, y]);
                }
            }
        }

        AddAreaToDictionary(hubRoom);
        mapRooms.Add(hubRoom);

        Room subRoom = new Room();
        roomSize = 4;
        startX = (columns / 2) - (roomSize / 2);
        startY = (rows / 2) - (roomSize / 2);
        endX = startX + roomSize;
        endY = startY + roomSize;

        OpenRoomWithBounds(subRoom, startX, endX, startY, endY, true);
        AddAreaToDictionary(subRoom);
        mapRooms.Add(subRoom);

        foreach (MapTileData mtd in subRoom.internalTiles)
        {
            hubRoom.internalTiles.Remove(mtd);
        }
        foreach (MapTileData mtd in subRoom.edgeTiles)
        {
            hubRoom.internalTiles.Remove(mtd);
        }

        // Pick a random edge tile of subRoom and make it into our final unlock gate
        MapTileData unlockGateTile = subRoom.edgeTiles[UnityEngine.Random.Range(0, subRoom.edgeTiles.Count)];
        unlockGateTile.ChangeTileType(TileTypes.GROUND);
        Destructible mainUnlockGate = CreateDestructibleInTile(unlockGateTile, "obj_metalgate");
        mainUnlockGate.SetActorData("gateindex", 999);

        Stairs toBoss = new Stairs();

        MapTileData randomClearMTD = subRoom.internalTiles.GetRandomElement();
        SpawnStairsAtLocation(false, randomClearMTD, MapMasterScript.BANDIT_DRAGON_DUNGEONEND_FLOOR);
        
        // Determine hero start tile in the hub room, create stairs
        heroStartArea = hubRoom;
        MapTileData playerStart = hubRoom.internalTiles[UnityEngine.Random.Range(0, hubRoom.internalTiles.Count)];
        heroStartTile = playerStart.pos;

        Stairs startStairsPrevious = new Stairs();
        startStairsPrevious.prefab = "ReinforcedStairsDown";
        startStairsPrevious.stairsUp = true;
        PlaceActor(startStairsPrevious, playerStart);

        return hubRoomCorners;
    }

    public List<Room> BuildObjectiveRooms()
    {
        int roomSize;

        objectiveRooms = new List<Room>();

        // Create 4 objective rooms in the corners of the map
        for (int i = 0; i < 4; i++)
        {
            Room objectiveRoom = new Room();
            objectiveRoom.InitializeLists();
            roomSize = UnityEngine.Random.Range(8, 10);
            switch (i)
            {
                case 0: // lower left
                    //startX = UnityEngine.Random.Range(1, 3);
                    startY = UnityEngine.Random.Range(1, 3);
                    startX = 1;
                    break;
                case 1: // upper left
                    //startX = UnityEngine.Random.Range(1, 3);
                    startX = 1;
                    startY = UnityEngine.Random.Range(rows - roomSize - 3, rows - roomSize);
                    break;
                case 2: // lower right
                    startX = UnityEngine.Random.Range(columns - roomSize - 3, columns - roomSize);
                    //startY = UnityEngine.Random.Range(1, 3);
                    startY = 1;
                    break;
                case 3: // upper right
                default:
                    startX = UnityEngine.Random.Range(columns - roomSize - 3, columns - roomSize);
                    startY = UnityEngine.Random.Range(rows - roomSize - 3, rows - roomSize);
                    break;
            }

            endX = startX + roomSize;
            endY = startY + roomSize;

            objectiveRoom.origin = new Vector2(startX, startY);
            objectiveRoom.size = new Vector2(roomSize, roomSize);

            OpenRoomWithBounds(objectiveRoom, startX, endX, startY, endY, true);
            AddAreaToDictionary(objectiveRoom);
            mapRooms.Add(objectiveRoom);

            objectiveRooms.Add(objectiveRoom);
        }

        return objectiveRooms;
    }

    public void DigFromHubRoomToObjectiveRooms()
    {
        // Now dig from corners to map edges
        // corners are in the corner list as: bottom left, top left, bottom right, top right
        // objectiveRooms are sorted in the same order

        for (int i = 0; i < 4; i++)
        {
            MapTileData startTile = hubRoomCorners[i];

            // #todo: better heuristic for picking destination tile
            MapTileData endTile = objectiveRooms[i].edgeTiles[UnityEngine.Random.Range(0, objectiveRooms[i].edgeTiles.Count)];

            // pathfind to get from start to end
            List<MapTileData> corridorTiles = Map.TunnelFromTileToTile(this, startTile, endTile, 8, true, false);

            // create a corridor and fill it with the dugout tiles

            Corridor cr = new Corridor();
            cr.InitializeLists();

            foreach (MapTileData mtd in corridorTiles)
            {
                // Must already be part of the room, so no need to widen it or add to the corridor list
                if (mtd.tileType == TileTypes.GROUND) continue;

                mtd.ChangeTileType(TileTypes.GROUND);

                // widen the corridor by converting *most* nearby tiles to ground
                CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this, true);
                for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
                {
                    if (UnityEngine.Random.Range(0, 1f) <= 0.2f && CustomAlgorithms.tileBuffer[x].tileType != TileTypes.GROUND &&
                        InBounds(CustomAlgorithms.tileBuffer[x].pos))
                    {
                        CustomAlgorithms.tileBuffer[x].ChangeTileType(TileTypes.GROUND);
                        cr.internalTiles.Add(CustomAlgorithms.tileBuffer[x]);
                        CustomAlgorithms.tileBuffer[x].AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);
                        CustomAlgorithms.tileBuffer[x].AddMapTag(MapGenerationTags.ESSENTIALCORRIDOR);
                    }
                }
                cr.internalTiles.Add(mtd);
                mtd.AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);
                mtd.AddMapTag(MapGenerationTags.ESSENTIALCORRIDOR);
            }

            // carve around the start tiles too
            CustomAlgorithms.GetTilesAroundPoint(startTile.pos, 1, this, true);
            for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.4f && CustomAlgorithms.tileBuffer[x].tileType != TileTypes.GROUND)
                {
                    CustomAlgorithms.tileBuffer[x].ChangeTileType(TileTypes.GROUND);
                    cr.internalTiles.Add(CustomAlgorithms.tileBuffer[x]);
                    CustomAlgorithms.tileBuffer[x].AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);
                    CustomAlgorithms.tileBuffer[x].AddMapTag(MapGenerationTags.ESSENTIALCORRIDOR);
                }
            }

            AddAreaToDictionary(cr);
            mapCorridors.Add(cr);
        }
    }

    public void CreateTreasureRooms()
    {
        allTreasureRooms = new List<Room>();

        // Create small treasure rooms
        int numTreasureRooms = 9;
        int maxAttempts = 500;
        int attempts = 0;
        for (int i = 0; i < numTreasureRooms; i++)
        {
            while (attempts < maxAttempts)
            {
                attempts++;
                Room treasureRoom = new Room();
                int sizeX = UnityEngine.Random.Range(6, 8);
                int sizeY = UnityEngine.Random.Range(6, 8);

                if (UnityEngine.Random.Range(0, 2) == 0) // Start on the sides.
                {
                    if (UnityEngine.Random.Range(0, 2) == 0) // start on left side
                    {
                        //startX = UnityEngine.Random.Range(1, 3);
                        startX = 1;
                        startY = UnityEngine.Random.Range(10, rows - sizeY - 10); // anywhere from top to bottom
                    }
                    else
                    {
                        startX = UnityEngine.Random.Range(columns - sizeX - 3, columns - sizeX - 1);
                        startY = UnityEngine.Random.Range(10, rows - sizeY - 10); // anywhere from top to bottom
                    }
                }
                else // Start on the top/bottom
                {
                    if (UnityEngine.Random.Range(0, 2) == 0) // start at top
                    {
                        startX = UnityEngine.Random.Range(10, columns - sizeX - 10); // anywhere from left to right
                        startY = UnityEngine.Random.Range(rows - sizeY - 3, rows - sizeY - 1);
                    }
                    else
                    {
                        startX = UnityEngine.Random.Range(10, columns - sizeX - 10); // anywhere from left to right
                        //startY = UnityEngine.Random.Range(1, 3);
                        startY = 1;
                    }
                }

                endX = startX + sizeX;
                endY = startY + sizeY;
                treasureRoom.size = new Vector2(sizeX, sizeY);
                treasureRoom.origin = new Vector2(startX, startY);
                bool valid = true;
                foreach (Room rm in mapRooms)
                {
                    if (AreAreasOverlapping(treasureRoom, rm, 2))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid) continue;

                // Check for corridor overlapz
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        if (mapArray[x, y].tileType == TileTypes.GROUND)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        break;
                    }
                }

                if (!valid) continue;

                OpenRoomWithBounds(treasureRoom, startX, endX, startY, endY, true);
                AddAreaToDictionary(treasureRoom);
                mapRooms.Add(treasureRoom);

                allTreasureRooms.Add(treasureRoom);
                break;
            }
        }
    }

    public void TunnelToTreasureRooms()
    {
        // Now how do we get to the treasure rooms?
        // 1. Pick a treasure room edge tile   
        // 2. Poll 100 random map tiles that are either room edge (wall), or corridor ground
        // 3. Pathfind tile to tile :)))
        foreach (Room treasureRoom in allTreasureRooms)
        {
            MapTileData startTile = treasureRoom.edgeTiles[UnityEngine.Random.Range(0, treasureRoom.edgeTiles.Count)];
            MapTileData endTile = null;
            float shortest = 999f;
            int tilesToPoll = 100;
            for (int i = 0; i < tilesToPoll; i++)
            {
                int randomX = UnityEngine.Random.Range(4, columns - 3);
                int randomY = UnityEngine.Random.Range(4, rows - 3);
                MapTileData checkMTD = mapArray[randomX, randomY];

                if (checkMTD.CheckMapTag(MapGenerationTags.NOCONNECTION)) continue;
                if (!checkMTD.CheckMapTag(MapGenerationTags.ENTRANCEPOSSIBLE)) continue;
                float dist = MapMasterScript.GetGridDistance(checkMTD.pos, startTile.pos);
                if (dist < shortest)
                {
                    shortest = dist;
                    endTile = checkMTD;
                }
            }

            // Now we pathfind and dig Yet Another corridor
            List<MapTileData> corridorTiles = Map.TunnelFromTileToTile(this, startTile, endTile, 10, true, true);
            Corridor treasureConnector = new Corridor();
            treasureConnector.InitializeLists();
            mapCorridors.Add(treasureConnector);
            AddAreaToDictionary(treasureConnector);

            foreach (MapTileData mtd in corridorTiles)
            {                
                mtd.ChangeTileType(TileTypes.GROUND);
                treasureConnector.internalTiles.Add(mtd);                
                mtd.AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);
                if (mtd.CheckMapTag(MapGenerationTags.ESSENTIALCORRIDOR)) continue;
                if (UnityEngine.Random.Range(0, 1f) <= percentTreasureConnectionsConvertedToSpikes)
                {
                    CreateDestructibleInTile(mtd, "obj_floorspikes_visible");
                }
            }

        }
    }

    public void CreateGatekeepersInCorners()
    {
        int gatekeeperIndex = 100;
        foreach(Room objRoom in objectiveRooms)
        {
            // Pick a tile and surround it with gates. Inside it will be a switch to open other gates!
            int tileX = UnityEngine.Random.Range((int)objRoom.origin.x + 2, (int)objRoom.origin.x + (int)objRoom.size.x - 2);
            int tileY = UnityEngine.Random.Range((int)objRoom.origin.y + 2, (int)objRoom.origin.y + (int)objRoom.size.y - 2);
            MapTileData centerTileForGateSwitch = mapArray[tileX, tileY];

            int attempts = 0;

            while (centerTileForGateSwitch.tileType != TileTypes.GROUND || !centerTileForGateSwitch.IsEmpty())
            {
                attempts++;
                tileX = UnityEngine.Random.Range((int)objRoom.origin.x + 2, (int)objRoom.origin.x + (int)objRoom.size.x - 2);
                tileY = UnityEngine.Random.Range((int)objRoom.origin.y + 2, (int)objRoom.origin.y + (int)objRoom.size.y - 2);
                centerTileForGateSwitch = mapArray[tileX, tileY];
                if (attempts >= 500)
                {
                    Debug.Log("Can't find a tile for centertilegateswitch " + gatekeeperIndex + " ??");
                    break;
                }
            }

            Destructible floorSwitch = CreateDestructibleInTile(centerTileForGateSwitch, "obj_floorswitch");
            floorSwitch.SetActorData("floorswitch_index", 0); // opens up the first treasure room
            floorSwitch.SetActorData("bandithub_storyswitch", 1);

            // surround tile with blocking gates that open when you beat gatekeeper

            for (int x = tileX - 1; x <= tileX+1; x++)
            {
                for (int y = tileY - 1; y <= tileY+1; y++)
                {
                    if (x == tileX && y == tileY) continue; // ignore center tile
                    Destructible bossGate = CreateDestructibleInTile(mapArray[x,y], "obj_metalgate");
                    bossGate.SetActorData("gateindex", gatekeeperIndex);
                }
            }

            MapTileData spawnTile = objRoom.internalTiles[UnityEngine.Random.Range(0, objRoom.internalTiles.Count)];

            attempts = 0;
            while (spawnTile.tileType != TileTypes.GROUND || !spawnTile.IsEmpty())
            {
                spawnTile = objRoom.internalTiles[UnityEngine.Random.Range(0, objRoom.internalTiles.Count)];
                attempts++;
                if (attempts >= 500)
                {
                    Debug.Log("Can't find a tile for gatekeeper " + gatekeeperIndex + " ??");
                    break;
                }
            }

            // Then create the Big Boy

            Monster gatekeeper = MonsterManagerScript.CreateMonster(dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
            gatekeeper.MakeChampion();
            gatekeeper.MakeChampion();
            gatekeeper.MakeChampion();
            gatekeeper.displayName = StringManager.GetString("exp_specialmon_gatekeeper") + " " + gatekeeper.myTemplate.monsterName;
            gatekeeper.allDamageMultiplier += 0.1f;
            gatekeeper.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.25f);

            PlaceActor(gatekeeper, spawnTile);
            gatekeeper.SetSpawnPos(spawnTile.pos);
            gatekeeper.AddAttribute(MonsterAttributes.STARTASLEEP, 100);

            gatekeeper.SetActorData("gatekeeper", gatekeeperIndex);
            gatekeeper.scriptOnDefeat = "UnlockGatesOfGatekeeperIndex";

            OnEnemyMonsterSpawned(this, gatekeeper, false);

            gatekeeperIndex++;
        }
    }

    void AddLootToTreasureRooms()
    {
        int numSparklesPerRoom = 2;
        int numChestsPerRoom = 2;
        int numFountainsPerRoom = 1;
        List<string> possibleChests = new List<string>()
        {
            "obj_largewoodenchest",
            "obj_ornatechest"
        };

        foreach (Room rm in allTreasureRooms)
        {
            numSparklesPerRoom = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < numSparklesPerRoom; i++)
            {
                MapTileData emptyTile = FindEmptyTileInRoom(rm);
                CreateDestructibleInTile(emptyTile, "obj_treasuresparkle");
            }
            for (int i = 0; i < numChestsPerRoom; i++)
            {
                MapTileData emptyTile = FindEmptyTileInRoom(rm);
                CreateDestructibleInTile(emptyTile, possibleChests[UnityEngine.Random.Range(0, possibleChests.Count)]);
            }
            for (int i = 0; i < numFountainsPerRoom; i++)
            {
                MapTileData emptyTile = FindEmptyTileInRoom(rm);
                CreateDestructibleInTile(emptyTile, "obj_regenfountain");
            }
        }
    }

    MapTileData FindEmptyTileInRoom(Room rm)
    {
        MapTileData randomTile = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
        int attempts = 0;
        while (randomTile.tileType != TileTypes.GROUND || !randomTile.IsEmpty())
        {
            attempts++;
            randomTile = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            if (attempts > 1000)
            {
                Debug.Log("WARNING! No empty tile found in floor " + floor + "!!!!");
                break;
            }
        }

        return randomTile;
    }

    public override void InitializeSwitchGateLists()
    {
        base.InitializeSwitchGateLists();
        if (finalUnlockGates == null)
        {
            finalUnlockGates = new List<Destructible>();
        }        
    }
}
