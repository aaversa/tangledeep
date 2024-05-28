using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

public class DungeonGenerationAlgorithms : MonoBehaviour {

    public static bool BuildLakeRooms(Map createMap)
    {
        return false;
    }

    public static bool BuildCircleDungeon(Map createMap)
    {
        List<List<MapTileData>> cornerstones = new List<List<MapTileData>>();

        int radius = (createMap.columns / 2) - 2;

        for (int i = 0; i < createMap.dungeonLevelData.numCircles; i++)
        {
            List<MapTileData> cornerstonesCreated = DoCirclePath(createMap, radius, UnityEngine.Random.Range(createMap.dungeonLevelData.minCircleDigs, createMap.dungeonLevelData.maxCircleDigs+ 1));
            cornerstones.Add(cornerstonesCreated);
            radius -= createMap.dungeonLevelData.circleRadiusInterval;
            radius += UnityEngine.Random.Range(0, 2);
            if (i == 0)
            {
                radius -= 1;
            }
            if (radius < 2) break;
        }


        // We want to link 1-2, 2-3, 3-4, etc.

        int dirIndex = 0;
        MapTileData startTile = null;
        MapTileData endTile = null;

        for (int i = 0; i < cornerstones.Count-1; i++) // Where 3 is num circles.
        {

            dirIndex = UnityEngine.Random.Range(0, cornerstones[i].Count);
            if ((dirIndex > cornerstones[i].Count) || (dirIndex > cornerstones[i+1].Count)) dirIndex = 0;

            startTile = cornerstones[i][dirIndex];
            endTile = cornerstones[i+1][dirIndex];

            CustomAlgorithms.GetPointsOnLineNoGarbage(startTile.pos, endTile.pos);

            for (int x = 0; x < CustomAlgorithms.numPointsInLineArray; x++)
            {
                Vector2 v2 = CustomAlgorithms.pointsOnLine[x];
                MapTileData mtd = createMap.GetTile(v2);
                if (mtd.tileType != TileTypes.GROUND)
                {
                    mtd.ChangeTileType(TileTypes.GROUND, createMap.mgd);
                    createMap.mapRooms[0].internalTiles.Add(mtd);
                }
            }
        }    
        
        
        if (createMap.dungeonLevelData.addCentralRoom)
        {
            List<RoomTemplate> listToUse = GameMasterScript.masterDungeonRoomsByLayout[(int)DungeonFloorTypes.CIRCLES];

            RoomTemplate centralRoomRT = listToUse[UnityEngine.Random.Range(0, listToUse.Count)];

            int sizeX = centralRoomRT.numColumns;
            int sizeY = centralRoomRT.numRows;
            int originX = (createMap.columns / 2) - (sizeX / 2);
            int originY = (createMap.rows / 2) - (sizeY / 2);

            Room rm = createMap.CreateRoomFromTemplate(originX,originY,centralRoomRT);
            createMap.BuildRoomTiles(rm);
            createMap.AddAreaToDictionary(rm);
            createMap.mapRooms.Add(rm);

            foreach (MapTileData mtd in rm.internalTiles)
            {
                createMap.SetTileAreaID(mtd, rm);
            }
            foreach (MapTileData mtd in rm.edgeTiles)
            {
                createMap.SetTileAreaID(mtd, rm);
            }

            // Now tunnel out from here to somewhere else.

            int digDir = UnityEngine.Random.Range(0, 4);
            Directions dirToUse = Directions.NORTH;
            switch (digDir)
            {
                case 0:
                    dirToUse = Directions.NORTH;
                    break;
                case 1:
                    dirToUse = Directions.EAST;
                    break;
                case 2:
                    dirToUse = Directions.SOUTH;
                    break;
                case 3:
                    dirToUse = Directions.WEST;
                    break;
            }

            bool finished = false;
            MapTileData workTile = createMap.GetTile(new Vector2(createMap.columns / 2, createMap.rows / 2));
            while (!finished)
            {
                Vector2 workPos = workTile.pos + MapMasterScript.xDirections[(int)dirToUse];
                workTile = createMap.GetTile(workPos);
                if (workTile == null)
                {
                    break;
                }

                if ((workTile.tileType != TileTypes.GROUND) && ((createMap.GetAreaID(workTile.pos) <= 0) || (rm.edgeTiles.Contains(workTile))))
                {
                    workTile.ChangeTileType(TileTypes.GROUND, createMap.mgd);
                    if (!createMap.mapRooms[0].internalTiles.Contains(workTile))
                    {
                        createMap.mapRooms[0].internalTiles.Add(workTile);
                    }
                    //Debug.Log("Dug out " + workTile.pos);
                }
                else if ((workTile.tileType == TileTypes.GROUND) && (createMap.GetAreaID(workTile.pos) == createMap.mapRooms[0].areaID))
                {
                    finished = true;
                    break;
                }
            }
        }

        return true;
    }

    static List<MapTileData> DoCirclePath(Map createMap, int radius, int circleDigPasses)
    {
        List<MapTileData> cornerstones = new List<MapTileData>();
        // Create the first point.

        int centerOfMapX = createMap.columns / 2;
        int centerOfMapY = createMap.rows / 2;

        float curveMult = 0.75f;

        int westPointX = centerOfMapX - radius;
        int westPointY = centerOfMapY;

        if (westPointX < 1) westPointX = 1;

        MapTileData westPoint = createMap.mapArray[westPointX, westPointY];

        int northWestPointX = (int)(centerOfMapX - (radius * curveMult));
        if (northWestPointX < 1) westPointX = 1;

        int northWestPointY = (int)(centerOfMapY + (radius * curveMult));

        MapTileData northWestPoint = createMap.mapArray[northWestPointX, northWestPointY];

        int northPointX = centerOfMapX;
        int northPointY = centerOfMapY + radius;
        if (northPointY > createMap.rows - 2) northPointY = createMap.rows - 2;

        MapTileData northPoint = createMap.mapArray[northPointX, northPointY];

        int northEastPointX = (int)(centerOfMapX + (radius * curveMult));
        int northEastPointY = (int)(centerOfMapY + (radius * curveMult));
        MapTileData northEastPoint = createMap.mapArray[northEastPointX, northEastPointY];

        int eastPointX = centerOfMapX + radius;
        if (eastPointX > createMap.columns - 2) eastPointX = createMap.columns - 2;
        int eastPointY = centerOfMapY;
        MapTileData eastPoint = createMap.mapArray[eastPointX, eastPointY];

        int southEastPointX = (int)(centerOfMapX + (radius * curveMult));
        int southEastPointY = (int)(centerOfMapY - (radius * curveMult));
        MapTileData southEastPoint = createMap.mapArray[southEastPointX, southEastPointY];

        int southPointX = centerOfMapX;
        int southPointY = centerOfMapY - radius;
        MapTileData southPoint = createMap.mapArray[southPointX, southPointY];

        int southWestPointX = (int)(centerOfMapX - (radius * curveMult));
        int southWestPointY = (int)(centerOfMapY - (radius * curveMult));
        MapTileData southWestPoint = createMap.mapArray[southWestPointX, southWestPointY];

        // Start at the leftmost point
        westPoint.ChangeTileType(TileTypes.GROUND, createMap.mgd);

        MapTileData workTile = null;

        MapTileData[] circlePoints = new MapTileData[8];
        circlePoints[0] = westPoint;
        circlePoints[1] = northWestPoint;
        circlePoints[2] = northPoint;
        circlePoints[3] = northEastPoint;
        circlePoints[4] = eastPoint;
        circlePoints[5] = southEastPoint;
        circlePoints[6] = southPoint;
        circlePoints[7] = southWestPoint;

        Directions[] digDirections = new Directions[8];
        digDirections[0] = Directions.NORTHEAST;
        digDirections[1] = Directions.NORTHEAST;
        digDirections[2] = Directions.SOUTHEAST;
        digDirections[3] = Directions.SOUTHEAST;
        digDirections[4] = Directions.SOUTHWEST;
        digDirections[5] = Directions.SOUTHWEST;
        digDirections[6] = Directions.NORTHWEST;
        digDirections[7] = Directions.NORTHWEST;

        for (int n = 0; n < circleDigPasses; n++)
        {
            for (int i = 0; i < circlePoints.Length; i++)
            {
                MapTileData startTile = circlePoints[i];
                MapTileData endTile = null;
                Directions workDir = digDirections[i];
                if (i < circlePoints.Length - 1)
                {
                    endTile = circlePoints[i + 1];
                }
                else
                {
                    endTile = circlePoints[0];
                }

                Directions[] dirPriorities = new Directions[4];

                switch (workDir)
                {
                    case Directions.NORTHEAST:
                        dirPriorities[0] = Directions.NORTH;
                        dirPriorities[1] = Directions.WEST;
                        dirPriorities[2] = Directions.NORTH;
                        dirPriorities[3] = Directions.SOUTH;
                        break;
                    case Directions.SOUTHEAST:
                        dirPriorities[0] = Directions.NORTH;
                        dirPriorities[1] = Directions.EAST;
                        dirPriorities[2] = Directions.EAST;
                        dirPriorities[3] = Directions.SOUTH;
                        break;
                    case Directions.SOUTHWEST:
                        dirPriorities[0] = Directions.EAST;
                        dirPriorities[1] = Directions.SOUTH;
                        dirPriorities[2] = Directions.SOUTH;
                        dirPriorities[3] = Directions.WEST;
                        break;
                    case Directions.NORTHWEST:
                        dirPriorities[0] = Directions.WEST;
                        dirPriorities[1] = Directions.SOUTH;
                        dirPriorities[2] = Directions.WEST;
                        dirPriorities[3] = Directions.SOUTH;
                        break;
                }
                // Dig out along a simple line from one point to the next.
                CustomAlgorithms.GetPointsOnLineNoGarbage(startTile.pos, endTile.pos);
                for (int x = 0; x < CustomAlgorithms.numPointsInLineArray; x++)
                {
                    workTile = createMap.mapArray[(int)CustomAlgorithms.pointsOnLine[x].x, (int)CustomAlgorithms.pointsOnLine[x].y];
                    if (workTile.tileType != TileTypes.GROUND)
                    {
                        workTile.ChangeTileType(TileTypes.GROUND, createMap.mgd);
                        createMap.mapRooms[0].internalTiles.Add(workTile);
                    }
                    // We MUST convert at least one neighboring tiles to ground to thiccen the circle
                    int randomDigs = 2;

                    while (randomDigs > 0)
                    {
                        randomDigs--;
                        int dirRoll = UnityEngine.Random.Range(0, 10);
                        if (dirRoll <= 4)
                        {
                            workDir = dirPriorities[0];
                        }
                        else if ((dirRoll >= 5) && (dirRoll <= 6))
                        {
                            workDir = dirPriorities[1];
                        }
                        else if ((dirRoll >= 7) && (dirRoll <= 8))
                        {
                            workDir = dirPriorities[2];
                        }
                        else
                        {
                            workDir = dirPriorities[3];
                        }

                        Vector2 digPos = workTile.pos + MapMasterScript.xDirections[(int)workDir];
                        if (!createMap.InBounds(digPos))
                        {
                            break;
                        }
                        workTile = createMap.mapArray[(int)digPos.x, (int)digPos.y];

                        if (workTile.tileType != TileTypes.GROUND)
                        {
                            workTile.ChangeTileType(TileTypes.GROUND, createMap.mgd);
                            createMap.mapRooms[0].internalTiles.Add(workTile);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < circlePoints.Length; i++)
        {
            if (circlePoints[i].tileType == TileTypes.GROUND)
            {
                cornerstones.Add(circlePoints[i]);
            }
        }
        return cornerstones;
    }    

	public static bool BuildLake(Map createMap)
    {
        int minX = (int)(createMap.columns * 0.15f);
        int minY = (int)(createMap.rows * 0.15f);
        int maxX = (int)(createMap.columns * 0.85f);
        int maxY = (int)(createMap.rows * 0.85f);

        int minStartX = (int)(createMap.columns * 0.4f);
        int minStartY = (int)(createMap.rows * 0.4f);
        int maxStartX = (int)(createMap.columns * 0.6f);
        int maxStartY = (int)(createMap.rows * 0.6f);

        MapTileData workTile;

        for (int x = 1; x < createMap.columns-1; x++)
        {
            for (int y = 1; y < createMap.rows-1; y++)
            {
                workTile = createMap.mapArray[x, y];
                workTile.ChangeTileType(TileTypes.GROUND, createMap.mgd);
            }
        }

        int numTilesInMap = (createMap.columns-1) * (createMap.rows - 1);
        int maxWaterTiles = (int)(numTilesInMap * 0.4f);
        int curWaterTiles = 0;

        workTile = createMap.mapArray[UnityEngine.Random.Range(minStartX, maxStartX), UnityEngine.Random.Range(minY, maxY)];
        List<MapTileData> workingTiles = new List<MapTileData>();
        workingTiles.Add(workTile);
        List<MapTileData> adjacentTiles = new List<MapTileData>();

        int attempts = 0;

        while (curWaterTiles < maxWaterTiles)
        {
            attempts++;
            if (attempts > 1500)
            {
                Debug.Log("Max lake iterations reached, breaking.");
                break;
            }
            if (workingTiles.Count == 0) break;

            // Pick a tile from the working list.
            // Convert it to water.
            // Pick surrounding tiles. Non-water gets added to list. Repeat.

            workTile = workingTiles[UnityEngine.Random.Range(0, workingTiles.Count)];
            if (!workTile.CheckTag(LocationTags.WATER))
            {
                workTile.AddTag(LocationTags.WATER);
                workingTiles.Remove(workTile);
                curWaterTiles++;
            }
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                Vector2 checkNewPos = workTile.pos + MapMasterScript.xDirections[i];
                if (!createMap.InBounds(checkNewPos)) continue;

                if ((checkNewPos.x >= minX) && (checkNewPos.x <= maxX) && (checkNewPos.y >= minY) && (checkNewPos.y <= maxY))
                {
                    if (createMap.GetTile(checkNewPos).CheckTag(LocationTags.WATER)) continue;

                    workingTiles.Add(createMap.GetTile(checkNewPos));
                }                
            }            
        }

        string row = "";

        // Below is debug only.
        for (int x = 0; x < createMap.rows; x++)
        {
            row = "";
            for (int y = 0; y < createMap.columns; y++)
            {
                if (createMap.mapArray[y,x].tileType == TileTypes.GROUND)
                {
                    if (createMap.mapArray[y,x].CheckTag(LocationTags.WATER))
                    {
                        row += "w";
                    }
                    else
                    {
                        row += ".";
                    }
                    
                }
                else
                {
                    row += "x";
                }
            }
            Debug.Log(row);
        }

        return true;
    }

    int GetWaterTilesNearby(MapTileData mtd, Map createMap)
    {
        MapTileData checkTile;
        int waterCount = 0;
        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
        {
            Vector2 newTilePos = mtd.pos + MapMasterScript.xDirections[i];
            if (createMap.InBounds(newTilePos))
            {
                checkTile = createMap.GetTile(newTilePos);
                if (checkTile.CheckTag(LocationTags.WATER))
                {
                    waterCount++;
                }
            }
        }
        return waterCount;
    }

    public static void RunPostMapBuildScripts(Map processMap)
    {
        foreach(string script in processMap.dungeonLevelData.scripts_postBuild)
        {
            if (string.IsNullOrEmpty(script)) continue;
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DungeonGenerationScripts), script);
            if (runscript != null)
            {
                object[] paramList = new object[1];
                paramList[0] = processMap;
                runscript.Invoke(null, paramList);
            }
        }
    }

    public class BSPLeaf
    {
        public const int MIN_LEAF_SIZE = 7;

        public BSPLeaf child1;
        public BSPLeaf child2;
        public Room roomChild;
        public int xPos;
        public int yPos;
        public int width;
        public int height;

        public BSPLeaf(int x, int y, int w, int h)
        {
            xPos = x;
            yPos = y;
            width = w;
            height = h;
        }

        public bool CanBeSplit(int maxSize, bool isBaseLeaf)
        {
            if (child1 != null || child2 != null)
            {
                return false;
            }
            /* if ((width > maxSize || height > maxSize) && !isBaseLeaf)
            {
                return false;
            } */
            return true;
        }

        public void AddChildrenToList(List<BSPLeaf> leaves)
        {
            if (child1 != null)
            {
                leaves.Add(child1);
            }
            if (child2 != null)
            {
                leaves.Add(child2);
            }
        }

        public void CreateRoomsRecursive()
        {
            bool bottomOfTree = true;
            if (child1 != null)
            {
                child1.CreateRoomsRecursive();
                bottomOfTree = false;
            }
            if (child2 != null)
            {
                child2.CreateRoomsRecursive();
                bottomOfTree = false;
            }
            if (!bottomOfTree) return;

            int roomSizeX = UnityEngine.Random.Range(5, width - 1);
            int roomSizeY = UnityEngine.Random.Range(5, height - 1);
            int roomPosX = UnityEngine.Random.Range(1, width - roomSizeX);
            int roomPosY = UnityEngine.Random.Range(1, height - roomSizeY);

            Debug.Log(roomSizeX + "," + roomSizeY + " compared to w/h of " + width + "," + height);

            Room rm = new Room();
            rm.size = new Vector2(roomSizeX, roomSizeY);
            rm.center = new Vector2(roomPosX, roomPosY);            
        }

        public bool Split()
        {
            bool splitHorizontal = UnityEngine.Random.Range(0, 2) == 0;

            if (width > height && width / height >= 1.4)
            {
                splitHorizontal = false;
            }
            else if (height > width && height / width >= 1.4)
            {
                splitHorizontal = true;
            }

            // Determine max height or width.
            int maxDimension = (splitHorizontal ? height : width);
            if (maxDimension <= MIN_LEAF_SIZE)
            {
                return false;
            }

            maxDimension /= 2;
            int dimension = UnityEngine.Random.Range(MIN_LEAF_SIZE, maxDimension + 1);

            // Create children based on split direction
            if (splitHorizontal)
            {
                child1 = new BSPLeaf(xPos, yPos, width, dimension);
                child2 = new BSPLeaf(xPos, yPos + dimension, width, height - dimension);
            }
            else
            {
                child1 = new BSPLeaf(xPos, yPos, dimension, height);
                child2 = new BSPLeaf(xPos + dimension, yPos, width - dimension, height);
            }

            return true;
        }
    }
    
    public static bool BuildBSPLayout(Map createMap)
    {
        int MAX_LEAF_SIZE = (createMap.columns / 2) + 1;

        List<BSPLeaf> leaves = new List<BSPLeaf>();

        // Create the root of all leaves

        BSPLeaf root = new BSPLeaf(0, 0, createMap.columns, createMap.rows);
        leaves.Add(root);

        bool split = true;
        while (split)
        {
            split = false;
            List<BSPLeaf> leavesToAdd = new List<BSPLeaf>();
            foreach(BSPLeaf leafo in leaves)
            {
                if (!leafo.CanBeSplit(MAX_LEAF_SIZE, leafo == root)) continue;
                leafo.Split();
                leafo.AddChildrenToList(leavesToAdd);
                split = true;
            }
            foreach(BSPLeaf addLeaf in leavesToAdd)
            {
                leaves.Add(addLeaf);
            }
            if (leavesToAdd.Count == 0)
            {
                break;
            }
        }

        foreach(BSPLeaf leaf in leaves)
        {
            if (leaf.width <= 3 || leaf.height <= 3)
            {
                // Do nothing. This leaf is too small to convert.
                continue;
            }

            Room rm = new Room();
            rm.InitializeLists();
            rm.size = new Vector2(leaf.width, leaf.height);
            createMap.mapRooms.Add(rm);
            createMap.AddAreaToDictionary(rm);

            bool roomBecomesTotallyFull = UnityEngine.Random.Range(0, 1f) <= 0.1f;

            for (int x = leaf.xPos; x < leaf.xPos + leaf.width; x++) 
            {
                for (int y = leaf.yPos; y < leaf.yPos + leaf.height; y++) 
                {
                    if (roomBecomesTotallyFull)
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.WALL, createMap.mgd);
                        rm.edgeTiles.Add(createMap.mapArray[x, y]);
                        continue;
                    }

                    // Edges.
                    bool openWall = UnityEngine.Random.Range(0, 1f) <= createMap.dungeonLevelData.chanceToOpenWalls;

                    if (x == 0 || y == 0 || x == createMap.columns - 1 || y == createMap.rows - 1)
                    {
                        openWall = false;
                    }

                    if (!openWall && (x == leaf.xPos || y == leaf.yPos || x == leaf.xPos + leaf.width - 1 || y == leaf.yPos + leaf.height - 1))
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.WALL, createMap.mgd);
                        rm.edgeTiles.Add(createMap.mapArray[x, y]);
                    }
                    else
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.GROUND, createMap.mgd);
                        rm.internalTiles.Add(createMap.mapArray[x, y]);
                    }
                }
            }
        }

        // OK, now everything has been split. Sup next.
        return true;

    }

    public static void CleanUpBSPMap(Map m)
    {

        for (int x = 1; x < m.columns - 1; x++)
        {
            for (int y = 1; y < m.rows - 1; y++)
            {
                // Look for ground tiles surrounded by 100% wall.
                if (m.mapArray[x, y].tileType != TileTypes.GROUND) continue;

                // Iterate through a 3x3 square, ignoring the center tile
                bool anyNearbyGround = false;
                for (int a = x-1; a <= x+1; a++)
                {
                    for (int b = y-1; b <= y+1; b++)
                    {
                        if (a == x && b == y) continue;
                        if (m.mapArray[a, b].tileType == TileTypes.GROUND)
                        {
                            //Debug.Log("Tile " + x + "," + y + " is OK because " + a + "," + b + " is ground.");
                            anyNearbyGround = true;
                            break;
                        }
                    }
                    if (anyNearbyGround) break;
                }

                if (!anyNearbyGround)
                {
                    //Debug.Log("Tile " + x + "," + y + " is NOT OK!!!");
                    m.mapArray[x, y].ChangeTileType(TileTypes.WALL, m.mgd);
                    m.RemoveAllActorsOfType(ActorTypes.DESTRUCTIBLE);
                }
            }
        }

        m.FloodFillToRemoveCutoffs(false, false);
    }

    public static void VerifyStairTransparencies()
    {
        // Checks tiles around staircases to make sure they aren't falsely flagged for generating transparencies 
        // For example if a Tree was spawned but then removed, that might be causing tiles above it to have TransLayers enabled
        // Even though that tree is no longer there.

        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.myMovable != null && st.actorEnabled)
            {
                // This sets a flag in the stair's Movable which is read by its TransLayer child
                // If true, then the TransLayer activates its own self in SpriteTransLayer.cs Update()
                try { st.myMovable.CheckTransparencyBelow(); } // 312019 - this function can fail
                catch(Exception)
                {
                    Debug.Log("Stair transparency failed.");
                };
            }            
        }
    }

    public static void DoVolcanoInitialBuild(Map processMap)
    {
        int minX = (int)(processMap.columns * 0.39f);
        int maxX = (int)(processMap.columns * 0.61f);
        int minY = (int)(processMap.rows * 0.39f);
        int maxY = (int)(processMap.rows * 0.61f);

        MapTileData vMTD;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                vMTD = processMap.mapArray[x, y];
                vMTD.ChangeTileType(TileTypes.GROUND, processMap.mgd);
            }
        }

        int totalMiddleArea = ((maxX - minX) * (maxY - minY));
        int numStartWallAreas = (int)(totalMiddleArea / 25);

        List<Vector2> middleTiles = new List<Vector2>();

        for (int x = 0; x < numStartWallAreas; x++)
        {
            Vector2 v2 = new Vector2(UnityEngine.Random.Range(minX, maxX + 1), UnityEngine.Random.Range(minY, maxY + 1));
            bool valid = true;
            foreach (Vector2 existing in middleTiles)
            {
                if (!Vector2.Equals(existing, v2))
                {
                    if (MapMasterScript.GetGridDistance(existing, v2) <= 3f)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            if (valid)
            {
                middleTiles.Add(v2);
            }
        }

        foreach (Vector2 v2 in middleTiles)
        {
            // Middle tile becomes wall.
            processMap.GetTile(v2).ChangeTileType(TileTypes.WALL, processMap.mgd);
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.4f) continue;
                Vector2 changer = v2 + MapMasterScript.xDirections[i];
                processMap.GetTile(changer).ChangeTileType(TileTypes.WALL, processMap.mgd);
            }
        }

        int numRandomRocks = (processMap.columns * processMap.rows) / 95;

        int extraRocks = (int)(numRandomRocks * processMap.dungeonLevelData.extraCrackedRockMultiplier);
        numRandomRocks += extraRocks;

        for (int i = 0; i < numRandomRocks; i++)
        {
            int x = UnityEngine.Random.Range(2, processMap.columns - 2);
            int y = UnityEngine.Random.Range(2, processMap.rows - 2);
            MapTileData mtd = processMap.mapArray[x, y];
            mtd.ChangeTileType(TileTypes.GROUND, processMap.mgd);
            if (mtd.CheckActorRef("obj_lavarock"))
            {
                continue;
            }
            Destructible dt = processMap.CreateDestructibleInTile(mtd, "obj_lavarock");
            if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_CRACKEDROCK_GEM)
            {
                Item gem = LootGeneratorScript.GenerateLootFromTable(processMap.dungeonLevelData.challengeValue, 0f, "gems");
                dt.myInventory.AddItem(gem, true);
            }
        }

        for (int i = 0; i < numRandomRocks / 2; i++)
        {
            int x = UnityEngine.Random.Range(2, processMap.columns - 2);
            int y = UnityEngine.Random.Range(2, processMap.rows - 2);
            MapTileData mtd = processMap.mapArray[x, y];
            if (mtd.tileType == TileTypes.GROUND)
            {
                if (mtd.CheckActorRef("obj_lavarock"))
                {
                    continue;
                }
                Destructible dt = processMap.CreateDestructibleInTile(mtd, "obj_lavarock");
                if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_CRACKEDROCK_GEM)
                {
                    Item gem = LootGeneratorScript.GenerateLootFromTable(processMap.dungeonLevelData.challengeValue, 0f, "gems");
                    dt.myInventory.AddItem(gem, true);
                }
            }
        }

        processMap.DigRandomVolcanoTunnels();
        processMap.DigRandomVolcanoTunnels();

        if (processMap.dungeonLevelData.lavaRivers > 0)
        {
            for (int i = 0; i < processMap.dungeonLevelData.lavaRivers; i++)
            {
                processMap.BuildRiver();
            }
            for (int x = 1; x < processMap.columns - 1; x++)
            {
                for (int y = 1; y < processMap.rows - 1; y++)
                {
                    if (processMap.mapArray[x, y].CheckTag(LocationTags.WATER))
                    {
                        processMap.mapArray[x, y].RemoveTag(LocationTags.WATER);
                        processMap.mapArray[x, y].AddTag(LocationTags.LAVA);
                    }
                }
            }
        }
    }

    /// <summary>
    /// This just CONNECTS existing stairs, it doesn't make stairs!
    /// </summary>
    /// <param name="startFloorID"></param>
    /// <param name="numFloors"></param>
    /// <param name="packageRequired"></param>
    public static void ConnectSeriesOfMapsPostGeneration(int startFloorID, int numFloors, EDLCPackages packageRequired)
    {
        if (packageRequired != EDLCPackages.COUNT && !DLCManager.CheckDLCInstalled(packageRequired)) return;
        // Connect dragon dungeon maps to each other.

        List<Map> allMaps = new List<Map>();

        for (int i = 0; i < numFloors; i++)
        {
            Map findMap = MapMasterScript.theDungeon.FindFloor(startFloorID + i);
            if (findMap != null)
            {
                allMaps.Add(findMap);
            }
            else
            {
                Debug.Log("Couldn't find map floor " + (startFloorID + i));
            }
        }        

        for (int i = 0; i < allMaps.Count; i++)
        {
            foreach (Stairs st in allMaps[i].mapStairs)
            {
                if (st.stairsUp) // "Up" means PREVIOUS floor. 
                {
                    if (i == 0) continue; // #todo Hook this up later to whatever area we decide to connect it to. TBD

                    //Debug.Log("Stairs up on floor " + allMaps[i].floor + " should point to " + allMaps[i - 1].floor + " " + allMaps[i - 1].GetName() + " " + st.actorUniqueID);
                    st.NewLocation = allMaps[i - 1];
                    st.newLocationID = allMaps[i - 1].mapAreaID;
                    st.pointsToFloor = allMaps[i - 1].floor;
                }
                else // "Down" means NEXT floor. There should be no Next stairs on the final floor.
                {
                    if (i == allMaps.Count - 1)
                    {
                        // Shouldn't be any stairs down here. Skip.
                        continue;
                    }
                    //Debug.Log("Stairs down on floor " + allMaps[i].floor + " should point to " + allMaps[i + 1].floor + " " + allMaps[i + 1].GetName() + " " + st.actorUniqueID);
                    st.NewLocation = allMaps[i + 1];
                    st.newLocationID = allMaps[i + 1].mapAreaID;
                    st.pointsToFloor = allMaps[i + 1].floor;
                }
            }

        }
    }

    public static void CleanUpGrassAndMudInMountainGrassMap(Map m)
    {
        float cleanupChance = 0.75f;
        int minGrassTilesToCleanUp = 3;
        int minGrassTilesToGrow = 3;
        float growChance = 0.4f;

        for (int x = 1; x < m.columns-1; x++)
        {
            for (int y = 1; y < m.rows-1; y++)
            {
                MapTileData mtd = m.mapArray[x, y];

                if (mtd.tileType == TileTypes.WALL || mtd.CheckMapTag(MapGenerationTags.HOLE)) continue;

                if (mtd.CheckTag(LocationTags.MUD))
                {
                    mtd.RemoveTag(LocationTags.MUD);
                }

                int grassAroundTile = 0;
                CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, m);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.GRASS))
                    {
                        grassAroundTile++;
                    }
                }

                bool hasGrass = mtd.CheckTag(LocationTags.GRASS);

                if (hasGrass && grassAroundTile <= minGrassTilesToCleanUp && UnityEngine.Random.Range(0,1f) <= cleanupChance)
                {
                    mtd.RemoveTag(LocationTags.GRASS);
                }
                else if (!hasGrass && grassAroundTile >= minGrassTilesToGrow && UnityEngine.Random.Range(0,1f) <= growChance)
                {
                    mtd.AddTag(LocationTags.GRASS);
                }
            }
        }
    }
}
