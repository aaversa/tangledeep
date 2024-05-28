using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class Map_SpiritDungeon : Map {

    List<MapTileData> possibleTiles;
    List<MapTileData> poolTiles;
    List<MapTileData> tilesConvertedToWater;
    List<MapTileData> poolsOfWater;

    static List<string> elementalHazards;

    public Map_SpiritDungeon() : base()
    {
        possibleTiles = new List<MapTileData>();
        tilesConvertedToWater = new List<MapTileData>();
        poolTiles = new List<MapTileData>();
        poolsOfWater = new List<MapTileData>();
    }

    public Map_SpiritDungeon(int mFloor)
    {
        Initialize(mFloor);
    }

    protected override void Initialize(int mFloor)
    {
        base.Initialize(mFloor);

        possibleTiles = new List<MapTileData>();
        tilesConvertedToWater = new List<MapTileData>();
        poolTiles = new List<MapTileData>();
        poolsOfWater = new List<MapTileData>();

        if (elementalHazards == null)
        {
            elementalHazards = new List<string>()
            {
                "obj_spirit_firetrap",
                "obj_spirit_acidtrap",
                "obj_spirit_shadowtrap",
                "obj_spirit_icetrap"
            };
        }
    }

    /// <inheritdoc />
    public override bool BuildRandomMap(ItemWorldMetaData itemWorldProperties)
    {
        mapAreaID = MapMasterScript.singletonMMS.mapAreaIDAssigner;
        

        floor = dungeonLevelData.floor;

        //start with a blank canvas of only ground
        FillMapWithNothing(TileTypes.GROUND);

        mgd.maxRoomAttempts = 5000;

        BuildAndPlaceRoomsStandard();

        foreach(Room rm in mapRooms)
        {
            foreach(MapTileData mtd in rm.internalTiles)
            {
                mtd.AddMapTag(MapGenerationTags.FILLED);
            }
            foreach (MapTileData mtd in rm.edgeTiles)
            {
                mtd.AddMapTag(MapGenerationTags.FILLED);
            }
        }

        BuildAndConnectCorridorsStandard();        

        FillInAndSurroundCorridors();

        AddTagsToTilesBorderingRoomsAndCorridors();

        AssignTileAreaIDsToRoomsAndCorridors();

        AddRandomPoolsOfWaterInRooms();
        AddRandomPoolsOfWaterAndColumnsToOutsideArea();

        if (dungeonLevelData.GetMetaData("connectwater") != 0)
        {
            ConnectPoolsOfWater();
        }        

        BreakExternalWallsOfEveryRoom();

        AddBreakablesFountainsAndSparklesToMap();

        AddTreasureToExteriorArea();

        CreateRandomElementalHazards();

        SeedMapWithMonsters(dungeonLevelData.minMonsters, dungeonLevelData.maxMonsters, new ItemWorldMetaData());

        int directionUsedAsInt = 0;
        MapTileData findTile = GetRandomTileInCorner(-1, out directionUsedAsInt);

        heroStartTile = new Vector2(findTile.pos.x, findTile.pos.y);

        SpawnStairsAtLocation(true, GetTile(heroStartTile));

        int exclude = directionUsedAsInt;
        MapTileData nextStairsTile = GetRandomTileInCorner(exclude, out directionUsedAsInt);

        SpawnStairsAtLocation(false, nextStairsTile);

        // === Post build clean up section =============================================================================

        SetMapBoundVisuals();

        PrepareGroundTiles();

        if (!string.IsNullOrEmpty(dungeonLevelData.script_preBeautify))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DungeonGenerationScripts), dungeonLevelData.script_preBeautify);
            if (runscript != null)
            {
                object[] paramList = new object[1];
                paramList[0] = this;
                runscript.Invoke(null, paramList);
            }
        }

        //decor and layered stuff
        AddGrassLayer1ToMap();
        AddGrassLayer2ToMapAndBeautifyAll();

        AddDecorAndFlowersAndTileDirections();
        AddGrassLayer1ToMap();
        RemoveOneOffGrassTiles();
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                BeautifyTerrain(mapArray[x, y], LocationTags.GRASS, LocationTags.GRASS);
            }
        }

        //This converts tiles we tagged as lava, mud, elec etc to the actual tile
        AddEnvironmentDestructiblesToMap();

        //done?
        return true;

    }

    void CreateSpiritRooms()
    {
        int numRooms = 6;

    }

    void FillInAndSurroundCorridors()
    {
        foreach (Corridor cr in mapCorridors)
        {
            foreach (MapTileData mtd in cr.internalTiles)
            {
                if (!mtd.CheckTag(LocationTags.WATER))
                {
                    CreateDestructibleInTile(mtd, "bpst");
                    mtd.AddTag(LocationTags.WATER);
                }

                CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
                    if (CustomAlgorithms.tileBuffer[i].CheckMapTag(MapGenerationTags.FILLED)) continue;
                    if (CheckMTDArea(CustomAlgorithms.tileBuffer[i]) != MapMasterScript.FILL_AREA_ID) continue;
                    if (CustomAlgorithms.tileBuffer[i].CheckMapTag(MapGenerationTags.ESSENTIALCORRIDOR)) continue;
                    if (CustomAlgorithms.tileBuffer[i].CheckMapTag(MapGenerationTags.EDGETILE)) continue;
                    if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.CORRIDOR)) continue;

                    CustomAlgorithms.tileBuffer[i].ChangeTileType(TileTypes.WALL);
                }
            }
        }
    }

    void AddRandomPoolsOfWaterAndColumnsToOutsideArea()
    {        
        FillPoolTileListWithEmptyExteriorTiles();
        
        // Find valid tiles for pools
        int numTilesToUseForWaterCenters = dungeonLevelData.randomWaterPools;

        FillPossibleTileListWithValidTiles(numTilesToUseForWaterCenters, checkingForColumns: false);

        tilesConvertedToWater.Clear();

        foreach(MapTileData mtd in possibleTiles)
        {
            tilesConvertedToWater.Add(mtd);
            CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                {
                    continue;
                }
                tilesConvertedToWater.Add(CustomAlgorithms.tileBuffer[i]);
            }
            poolsOfWater.Add(mtd);
        }
        foreach(MapTileData mtd in tilesConvertedToWater)
        {
            if (mtd.CheckTag(LocationTags.WATER)) continue;
            CreateDestructibleInTile(mtd, "bpst");
            mtd.AddTag(LocationTags.WATER);
        }

        int randomColumns = dungeonLevelData.GetMetaData("randomcolumns");

        tilesConvertedToWater.Clear(); // well, we're converting these to wall actually
        FillPossibleTileListWithValidTiles(randomColumns, checkingForColumns: true);

        List<int> tileNumbersToUse = new List<int>();

        foreach (MapTileData mtd in possibleTiles)
        {
            tilesConvertedToWater.Add(mtd);
            CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);

            tileNumbersToUse.Clear();
            int tileCount = tileNumbersToUse.Count;
            
            while (tileCount <= 3)
            {
                int randomNumber = UnityEngine.Random.Range(0, CustomAlgorithms.numTilesInBuffer);
                while (tileNumbersToUse.Contains(randomNumber))
                {
                    randomNumber = UnityEngine.Random.Range(0, CustomAlgorithms.numTilesInBuffer);
                }
                tileNumbersToUse.Add(randomNumber);
                tileCount++;
            }

            for (int i = 0; i < tileCount; i++)
            {
                if (CustomAlgorithms.tileBuffer[tileNumbersToUse[i]].CheckTag(LocationTags.WATER))
                {
                    continue;
                }
                tilesConvertedToWater.Add(CustomAlgorithms.tileBuffer[tileNumbersToUse[i]]);
            }
        }

        foreach(MapTileData mtd in tilesConvertedToWater)
        {
            mtd.ChangeTileType(TileTypes.WALL);
        }

    }

    void AddRandomPoolsOfWaterInRooms()
    {
        possibleTiles.Clear();
        tilesConvertedToWater.Clear();

        foreach(Room rm in mapRooms)
        {
            if (rm.areaID == MapMasterScript.FILL_AREA_ID)
            {
                continue;
            }
            MapTileData centerTile = null;
            possibleTiles.Clear();
            foreach (MapTileData mtd in rm.internalTiles)
            {
                CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                    {
                        continue;
                    }
                    possibleTiles.Add(mtd);
                }
            }
            centerTile = possibleTiles.GetRandomElement();
            tilesConvertedToWater.Add(centerTile);
            CustomAlgorithms.GetTilesAroundPoint(centerTile.pos, 1, this);
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                {
                    continue;
                }
                tilesConvertedToWater.Add(CustomAlgorithms.tileBuffer[i]);
            }
        }

        foreach(MapTileData mtd in tilesConvertedToWater)
        {
            if (mtd.CheckTag(LocationTags.WATER)) continue;
            CreateDestructibleInTile(mtd, "bpst");
            mtd.AddTag(LocationTags.WATER);
        }
    }

    void BreakExternalWallsOfEveryRoom()
    {
        possibleTiles.Clear();

        foreach(Room rm in mapRooms)
        {
            if (rm.areaID == MapMasterScript.FILL_AREA_ID) continue;

            rm.edgeTiles.Shuffle();

            MapTileData mtdToBreak = null;

            foreach(MapTileData mtd in rm.edgeTiles)
            {
                bool valid = false;
                CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
                    if (GetAreaID(CustomAlgorithms.tileBuffer[i].pos) == MapMasterScript.FILL_AREA_ID)
                    {
                        valid = true;
                        break;
                    }
                }
                if (valid)
                {
                    mtd.ChangeTileType(TileTypes.GROUND);
                    mtd.AddMapTag(MapGenerationTags.FILLED);
                    //mtd.AddTag(LocationTags.LAVA);
                    break;
                }
            }
        }
    }

    void AddTagsToTilesBorderingRoomsAndCorridors()
    {
        for (int x = 1; x < columns-1; x++)
        {
            for (int y = 1; y < rows-1; y++)
            {
                if (mapArray[x, y].tileType == TileTypes.WALL) continue;
                if (mapArray[x, y].CheckMapTag(MapGenerationTags.FILLED)) continue;

                bool valid = true;

                CustomAlgorithms.GetTilesAroundPoint(mapArray[x, y].pos, 1, this);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                    {
                        valid = false;
                        break;
                    }
                    if (CustomAlgorithms.tileBuffer[i].CheckMapTag(MapGenerationTags.FILLED))
                    {
                        valid = false;
                        break;
                    }                    
                }

                if (!valid)
                {
                    mapArray[x, y].AddMapTag(MapGenerationTags.NOCONNECTION); // do we need to create a different tag for this
                }
            }
        }
    }

    void ConnectPoolsOfWater()
    {
        int numConnections = 7;

        if (numConnections >= poolsOfWater.Count)
        {
            numConnections = poolsOfWater.Count - 1;
        }
        tilesConvertedToWater.Clear();

        for (int i = 0; i < numConnections; i++)
        {
            MapTileData start = poolsOfWater.GetRandomElement();

            while (tilesConvertedToWater.Contains(start))
            {
                start = poolsOfWater.GetRandomElement();
            }

            int lowDistance = 999;
            MapTileData lowest = null;

            foreach(MapTileData mtd in poolsOfWater)
            {
                if (mtd == start) continue;
                int dist = MapMasterScript.GetGridDistance(start.pos, mtd.pos);
                if (dist < lowDistance)
                {
                    lowDistance = dist;
                    lowest = mtd;
                }
            }

            possibleTiles = TunnelFromTileToTile(this, start, lowest, 7, false, true, areaIDOfFill:-777);
            foreach(MapTileData mtd in possibleTiles)
            {
                if (mtd.CheckTag(LocationTags.WATER)) continue;
                mtd.AddTag(LocationTags.WATER);
                CreateDestructibleInTile(mtd, "bpst");
            }

            tilesConvertedToWater.Add(start);
        }
    }

    /// <summary>
    /// Finds "tilestToGet" tiles from the "possibleTiles" list. These tiles must be spaced a certain distance apart.
    /// </summary>
    /// <param name="tilesToGet"></param>
    void FillPossibleTileListWithValidTiles(int tilesToGet, bool checkingForColumns)
    {
        possibleTiles.Clear();
        for (int i = 0; i < tilesToGet; i++)
        {
            bool foundTile = false;
            MapTileData useTile = null;

            int attempts = 0;

            while (!foundTile)
            {
                attempts++;
                if (attempts > 1000)
                {
                    //Debug.LogError("FAIL");
                    break;
                }
                MapTileData comparisonTile = null;

                int subAttempts = 0;

                comparisonTile = poolTiles.GetRandomElement();
                while (possibleTiles.Contains(comparisonTile) || comparisonTile.CheckMapTag(MapGenerationTags.NOCONNECTION) 
                    || (checkingForColumns && (comparisonTile.tileType == TileTypes.WALL || comparisonTile.CheckMapTag(MapGenerationTags.FILLED))))
                {
                    subAttempts++;
                    if (subAttempts > 500)
                    {
                        break;
                    }
                    comparisonTile = poolTiles.GetRandomElement();
                }

                foundTile = true;

                // first, find a tile
                foreach (MapTileData mtd in possibleTiles)
                {
                    if (MapMasterScript.GetGridDistance(mtd.pos, comparisonTile.pos) < 4)
                    {
                        foundTile = false;
                        break;
                    }

                }

                if (foundTile)
                {
                    useTile = comparisonTile;
                }
            }

            possibleTiles.Add(useTile);
        }
    }

    void FillPoolTileListWithEmptyExteriorTiles()
    {
        poolTiles.Clear();
        for (int x = 2; x < columns - 2; x++)
        {
            for (int y = 2; y < rows - 2; y++)
            {
                if (mapArray[x, y].CheckMapTag(MapGenerationTags.FILLED))
                {
                    continue;
                }
                if (mapArray[x,y].tileType == TileTypes.WALL)
                {
                    continue;
                }
                poolTiles.Add(mapArray[x, y]);
            }
        }
    }

    void AddTreasureToExteriorArea()
    {
        FillPoolTileListWithEmptyExteriorTiles();

        int numTreasures = dungeonLevelData.GetMetaData("numtreasures");

        for (int i = 0; i < numTreasures; i++)
        {
            MapTileData checkTile = null;
            while (checkTile == null)
            {
                checkTile = poolTiles.GetRandomElement();

                if (checkTile.IsEmpty())
                {
                    break;
                }

                if (UnityEngine.Random.Range(0,3) != 0)
                {
                    CreateDestructibleInTile(checkTile, "obj_largewoodenchest");
                }
                else
                {
                    CreateTreasureInTile(checkTile);
                }
            }
        }


    }

    MapTileData GetRandomTileInCorner(int exclude, out int directionUsedAsInt)
    {
        MapTileData findTile = null;
        int startPosX = 0;
        int startPosY = 0;
        directionUsedAsInt = 0;
        while (findTile == null)
        {
            directionUsedAsInt = UnityEngine.Random.Range(0, 4);
            while (directionUsedAsInt == exclude)
            {
                directionUsedAsInt = UnityEngine.Random.Range(0, 4);
            }
            switch (directionUsedAsInt)
            {
                case 0: // top left      
                    startPosX = UnityEngine.Random.Range(1, 5);
                    startPosY = UnityEngine.Random.Range(rows - 5, rows - 1);
                    break;
                case 1: // top right
                    startPosX = UnityEngine.Random.Range(columns - 5, columns - 1);
                    startPosY = UnityEngine.Random.Range(rows - 5, rows - 1);
                    break;
                case 2: // bottom left
                    startPosX = UnityEngine.Random.Range(1, 5);
                    startPosY = UnityEngine.Random.Range(1, 5);
                    break;
                case 3: // bottom right
                    startPosX = UnityEngine.Random.Range(columns - 5, columns - 1);
                    startPosY = UnityEngine.Random.Range(1, 5);
                    break;
            }

            if (mapArray[startPosX, startPosY].tileType == TileTypes.GROUND && !mapArray[startPosX, startPosY].CheckTag(LocationTags.WATER) && mapArray[startPosX, startPosY].IsEmpty())
            {
                findTile = mapArray[startPosX, startPosY];
            }
        }

        return findTile;
    }

    void CreateRandomElementalHazards()
    {
        int numHazards = dungeonLevelData.GetMetaData("randomelementalhazards");

        poolTiles.Clear();
        possibleTiles.Clear();
        for (int x = 2; x < columns-2; x++)
        {
            for (int y = 2; y < rows -2; y++)
            {
                if (mapArray[x, y].tileType != TileTypes.GROUND) continue;
                if (mapArray[x, y].CheckTag(LocationTags.WATER)) continue;
                if (mapArray[x, y].CheckMapTag(MapGenerationTags.HAZARD)) continue;
                if (mapArray[x, y].CheckMapTag(MapGenerationTags.NOCONNECTION)) continue;
                possibleTiles.Add(mapArray[x, y]);
            }
        }

        for (int i = 0; i < numHazards; i++)
        {
            string dtRef = elementalHazards.GetRandomElement();

            MapTileData rnd = possibleTiles.GetRandomElement();
            while (poolTiles.Contains(rnd))
            {
                rnd = possibleTiles.GetRandomElement();
            }

            CustomAlgorithms.GetTilesAroundPoint(rnd.pos, 1, this);

            for (int x= 0; x < CustomAlgorithms.numTilesInBuffer; x++)
            {
                if (CustomAlgorithms.tileBuffer[x].tileType != TileTypes.GROUND) continue;
                if (CustomAlgorithms.tileBuffer[x].CheckTag(LocationTags.WATER)) continue;
                if (CustomAlgorithms.tileBuffer[x].CheckMapTag(MapGenerationTags.HAZARD)) continue;
                Destructible dt2 = CreateDestructibleInTile(CustomAlgorithms.tileBuffer[x], dtRef);
                dt2.actorfaction = Faction.DUNGEON;
                CustomAlgorithms.tileBuffer[x].AddMapTag(MapGenerationTags.HAZARD);
            }

            /* Destructible dt = CreateDestructibleInTile(rnd, dtRef);
            dt.actorfaction = Faction.DUNGEON;
            rnd.AddMapTag(MapGenerationTags.HAZARD); */
        }
    }
}
