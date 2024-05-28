using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// New utility functions for Map generation
public partial class Map
{
	//A second method of determining what wall shapes go where, based on the tile's 4 cardinal neighbors.
	//Each neighbor has a value: N, E, S, W == 1, 2, 4, 8.
	//Ergo, each tile has a 0-15 total of neighbors that match the terrain. These then map to a particular 
	//sprite. The VisualTileType enum matches the sprite order on the sheet, which does NOT match this formula,
	//so this array is a conversion from one to the other.
	//
	//The win here is that it's easy to calculate the sprite to use for shaped terrain like walls and holes.
	//It doesn't require 16 IF statements, just a bit of math. Check out the uses of this array for examples.
	public static VisualTileType[] visualTileTypesByBitAddition =
	{
		VisualTileType.WALL_NONE, // 0
		VisualTileType.WALL_N,  // 1
		VisualTileType.WALL_E,  // 2
		VisualTileType.WALL_N_E, // 3
		VisualTileType.WALL_S,  // 4
		VisualTileType.WALL_N_S, // 5
		VisualTileType.WALL_E_S, // 6
		VisualTileType.WALL_N_E_S, // 7

		VisualTileType.WALL_W, // 8
		VisualTileType.WALL_N_W, // 9
		VisualTileType.WALL_E_W, // 10
		VisualTileType.WALL_N_E_W, // 11
		VisualTileType.WALL_S_W, // 12
		VisualTileType.WALL_N_S_W, // 13
		VisualTileType.WALL_E_S_W, // 14
		VisualTileType.WALL_N_E_S_W // 15
	};

	/// <summary>
	/// Changes a block of tiles at once.
	/// </summary>
	/// <param name="left">Leftmost tile value, lowest X</param>
	/// <param name="bottom">Bottom tile value, lowest Y</param>
	/// <param name="width">width in tiles</param>
	/// <param name="height">height in tiles</param>
	/// <param name="newType">desired tile conversion</param>
	/// <returns></returns>
	public void ConvertAreaToTileType(int left, int bottom, int width, int height, TileTypes newType)
	{
		for (int x = left; x < left + width; x++)
		{
			for (int y = bottom; y < bottom + height; y++)
			{
				var mtd = GetTile(x, y);
				if (mtd != null)
				{
					mtd.ChangeTileType(newType);
				}
				
			}
		}
	}

	
	public void ConvertAreaToTileType(Vector2 corner, int width, int height, TileTypes newType)
	{
		ConvertAreaToTileType((int) corner.x, (int) corner.y, width, height, newType);
	}

	/// <summary>
	/// Adds a destructible of a given type to every tile in the area
	/// </summary>
	/// <param name="left">Leftmost tile value, lowest X</param>
	/// <param name="bottom">Bottom tile value, lowest Y</param>
	/// <param name="width">width in tiles</param>
	/// <param name="height">height in tiles</param>
	/// <param name="prefabRef">the prefab to spawn in every location</param>
	/// <param name="groundOnly">if true, only TileTypes.GROUND tiles will receive the destructible</param>
	/// <param name="alsoChangeTile">If not COUNT, each tile in the area will be changed to this type</param>
	public void CreateDestructiblesInArea(int left, int bottom, int width, int height, string prefabRef,
		bool groundOnly = true, TileTypes alsoChangeTile = TileTypes.COUNT)
	{
		for (int x = left; x < left + width; x++)
		{
			for (int y = bottom; y < bottom + height; y++)
			{
				var mtd = GetTile(x, y);
				if (mtd == null) continue;

				//if we want to change the tile, do so
				if (alsoChangeTile != TileTypes.COUNT)
				{
					mtd.ChangeTileType(alsoChangeTile);
				}

				//comedy if groundOnly == true and
				//alsoChangeTile is neither COUNT nor GROUND
				if (groundOnly &&
				    mtd.tileType != TileTypes.GROUND)
				{
					continue;
				}

				//place the destructible in the tile.
				var oldTT = mtd.tileType;
				
				//this forces the tile to ground, boo
				CreateDestructibleInTile(mtd, prefabRef);

				//so put it back
				mtd.ChangeTileType(oldTT);
			}
		}
	}

	/// <summary>
	/// Sets up the maparray and various other pieces of data that are based on the number of tiles in the map.
	/// </summary>
	/// <param name="size">The width and height of the dungeon</param>
	protected void CreateTileArrays(int size)
	{
		columns = size;
		rows = size;

		unusedArea = columns * rows;

		mapArray = new MapTileData[columns, rows];
		areaIDArray = new int[columns, rows];
		exploredTiles = new bool[columns, rows];
		
		//Areas start with no IDs
		for (int x = 0; x < columns; x++)
		{
			for (int y = 0; y < rows; y++)
			{
				areaIDArray[x, y] = -1;
			}
		}
	}

	/// <summary>
	/// Used to spawn a set of stairs at a set location, as opposed to randomly in the map somewhere.
	/// </summary>
	/// <param name="stairsUp">True if these stairs ascend Tangledeep</param>
	/// <param name="mtdForStairs">The tile we'd like to spawn on</param>
	/// <param name="pointsToFloor">Forces the stairs to travel to a set floor</param>
	/// <returns></returns>
	public Stairs SpawnStairsAtLocation(bool stairsUp, MapTileData mtdForStairs, int pointsToFloor = -1)
	{       
		Stairs nStairs = new Stairs();
		nStairs.dungeonFloor = floor;
		nStairs.SetUniqueIDAndAddToDict();

		//Make the stairs presentable
		nStairs.stairsUp = stairsUp;

        // What we show to players is reversed, down is up and up is down :|
		nStairs.displayName = StringManager.GetString( stairsUp ? "stairs_down" : "stairs_up");
		nStairs.prefab = MapMasterScript.visualTileSetNames[(int) dungeonLevelData.tileVisualSet] +
		                 (stairsUp ? "StairsDown" : "StairsUp");

		//Place these stairs in the world
		nStairs.SetSpawnPosXY((int)mtdForStairs.pos.x, (int)mtdForStairs.pos.y);
		nStairs.SetCurPos(mtdForStairs.pos);
		mtdForStairs.AddActor(nStairs);
		AddActorToMap(nStairs);
		
		//Don't leave stairs in a pile of bad
		mtdForStairs.RemoveTag(LocationTags.MUD);
		mtdForStairs.RemoveTag(LocationTags.LAVA);
		mtdForStairs.RemoveTag(LocationTags.ELECTRIC);
		mtdForStairs.ChangeTileType(TileTypes.GROUND, mgd);

        int desiredFloor = -1;

		//If we have asked for this, point at the desired floor
		if (pointsToFloor != -1)
		{
            desiredFloor = pointsToFloor;
		}

        if (stairsUp && dungeonLevelData.stairsUpToLevel != 0)
        {
            desiredFloor = dungeonLevelData.stairsUpToLevel;
        }
        else if (!stairsUp && dungeonLevelData.stairsDownToLevel != 0)
        {
            desiredFloor = dungeonLevelData.stairsDownToLevel;
        }

        if (desiredFloor != -1)
        {
            nStairs.pointsToFloor = desiredFloor;
            
            // This map may not be generated yet, so we can't link to it yet.
            //nStairs.newLocation = MapMasterScript.theDungeon.FindFloor(desiredFloor);
            //nStairs.newLocationID = nStairs.newLocation.mapAreaID;
        }

        //Debug.Log(nStairs.prefab + " Spawning stairs up? " + stairsUp + " at location " + mtdForStairs.pos + " PTF? " + pointsToFloor + " Desired floor? " + desiredFloor);

        return nStairs;
	}

	/// <summary>
	/// Uses the maps dungeonLevelData to set or clear a sprite based background
	/// </summary>
	public void UpdateParallaxBackground()
	{
		//If the dungeonleveldata has a parallax background request, display it here
		if (string.IsNullOrEmpty(dungeonLevelData.parallaxSpriteRef))
		{
			DungeonParallaxManager.ClearBackground();
			return;
		}
		
		//pretty
		DungeonParallaxManager.SetBackground( dungeonLevelData.parallaxSpriteRef, 
											  dungeonLevelData.parallaxShiftPerTile.x,
											  dungeonLevelData.parallaxShiftPerTile.y,
											  dungeonLevelData.parallaxTileCount);
		
		

	}

	/// <summary>
	/// Instantiate a teleporter of a given actor type at a location, and set the destination.
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="dest"></param>
	/// <param name="teleportDestructibleRef"></param>
	/// <returns>The created teleporter destructible</returns>
	public Destructible CreateTeleporterAtLocationWithDest(Vector2 origin, Vector2 dest, string teleportDestructibleRef)
	{
		var telepipe = CreateDestructibleInTile(GetTile(origin), teleportDestructibleRef);
		telepipe.SetActorData("teleport_dest_x",(int)dest.x);
		telepipe.SetActorData("teleport_dest_y",(int)dest.y);

		return telepipe;
	}

    /// <summary>
    /// Takes all walls that are NOT trees/solid terrain and converts them into see-through holes for cool parallax stuff later.
    /// </summary>
    public void ConvertAllWallTilesToHoles(int tileIndex)
    {
        HashSet<MapTileData> convertToHoles = new HashSet<MapTileData>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                MapTileData mtd = mapArray[x, y];
                if (mtd.tileType == TileTypes.GROUND) continue;
                if (mtd.CheckMapTag(MapGenerationTags.DONT_CONVERT_TO_HOLE)) continue;

                mtd.RemoveTag(LocationTags.TREE);
                mtd.RemoveTag(LocationTags.SOLIDTERRAIN);
                //change it, we'll change this back before map gen is done
                mtd.ChangeTileType(TileTypes.HOLE);
                mtd.AddMapTag(MapGenerationTags.HOLE);
                convertToHoles.Add(mtd);
            }
        }

        ConvertListOfHoles(convertToHoles, tileIndex);

        // Now make the map EDGES into "nothing" so they look correct.
        /* for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (x == 0 || x == columns-1 || y == 0 || y == rows - 1)
                {
                    MapTileData mtd = mapArray[x, y];

                    if (x > 0 && x < columns-1)
                    {
                        if (y == rows - 1) // top horizontal border
                        {
                            mtd.indexOfSpriteInAtlas = tileIndex + 14;
                        }
                        else if (y == 0) // bottom horizontal border
                        {
                            mtd.indexOfSpriteInAtlas = tileIndex + 7;
                        }
                    }                    
                    else if (x == 0 && y > 0 && y < rows-1) // left vertical border
                    {
                        
                    }                    
                }                                
            }
        } */
    }

    /// <summary>
    /// Takes a given set of WALL tiles and converts them to see-through HOLES for COOL VISUAL EFFECTS
    /// </summary>
    /// <param name="convertToHoles"></param>
    /// <param name="targetIndex"></param>
    public void ConvertListOfHoles(HashSet<MapTileData> convertToHoles, int targetIndex, bool cornerCorrection = false)
    {
        //if this tile got changed back into a ground tile, do not do this.
        convertToHoles.RemoveWhere(tile => tile.tileType != TileTypes.HOLE);

        //take every tile we wanted to make into a hole, and do so
        foreach (var mtd in convertToHoles)
        {
            //place a blocker there. We *must* do this first, because CreateDestructible secretly sets
            //the terrain type to ground.
            CreateDestructibleInTile(mtd, "stb");

            //change our flag back to hole.
            mtd.tileType = TileTypes.HOLE;

            //remove any decor/flowers that may have been dropped here.
            mtd.RemoveTag(LocationTags.HASDECOR);
            mtd.indexOfDecorSpriteInAtlas = -1;
            mtd.RemoveTag(LocationTags.GRASS);
            mtd.RemoveTag(LocationTags.GRASS2);
            mtd.indexOfGrassSpriteInAtlas = -1;

            //set our texture based on who is near us.
            var newIdx = 0;
            int bitValue = 0;
            for (int buddyIdx = 0; buddyIdx < MapMasterScript.directions.Length; buddyIdx++)
            {
                //if our neighbors are the same, the index goes up
                var buddyTile = GetTile(mtd.pos + MapMasterScript.directions[buddyIdx]);
                if (buddyTile == null ||
                    buddyTile.tileType == TileTypes.HOLE)
                {
                    bitValue += 1 << buddyIdx;
                }
            }

            newIdx = (int)visualTileTypesByBitAddition[bitValue];
            mtd.visualTileType = visualTileTypesByBitAddition[bitValue];

            //yet another consideration. If this tile has only holes in the NESW neighbors,
            //we should also check the NE, SE, NW, and SW corners for solid terrain
            //if they have not-holes, we will be an ugly corner unless we correct ourselves.
            //This will NOT correct corners that are have walls in the N, E, S or W areas.
            //
            //There can still be problems, so make sure that inside corners only exist with
            // * one of NE, SE, SW or NW occupied
            // * no occupation in the N, E, S, or W blocks.
            //
            //Picky and bad, I know, but otherwise we refactor everything and face ourselves to bloodshed.

            //N, E, S, W all holes
            if (bitValue == 15 && cornerCorrection)
            {
                //north is 0, so start at NE
                for (int buddyIdx = 1; buddyIdx < MapMasterScript.xDirections.Length; buddyIdx += 2)
                {
                    //if our neighbors are the same, the index goes up
                    var buddyTile = GetTile(mtd.pos + MapMasterScript.xDirections[buddyIdx]);
                    if (buddyTile != null &&
                        buddyTile.tileType != TileTypes.HOLE)
                    {
                        //16 is the NE corner edge
                        newIdx = 16 + (buddyIdx / 2);

                        //one shot
                        break;
                    }
                }
            }

            //move it down to our New Future Shit holes in the ground
            //the visual index is on one of 25 columns
            //(targetIndex/32nd) row, starting at 0, with each row being 25 wide. 44 x 25            
            newIdx += targetIndex;

            //set the visual index
            mtd.indexOfSpriteInAtlas = newIdx;            
        }

        //change all the holes back to walls
        foreach (var convertToHole in convertToHoles)
        {
            //do NOT call ChangeTileType here, because that breaks
            //the visual settings we made earlier.
            convertToHole.tileType = TileTypes.GROUND;
        }
    }

}

