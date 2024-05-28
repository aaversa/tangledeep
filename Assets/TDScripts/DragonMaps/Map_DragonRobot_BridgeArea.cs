using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class used to build the bridge-based dragonrobot level right before the boss. 
/// </summary>
public class Map_DragonRobot_BridgeArea : Map_DragonRobot
{
	//The location of the initial teleporter for the puzzle, 
	//and other teleporter destinations will be adjacent to this.
	private Vector2 startOfTeleportPuzzleLoc;
	
	//The following areas are places where you may decide to spawn monsters.
	//They each cover a different section of the map. I tried to label each one with
	//position and advice for what might go there.
#region Rectangles for Monster Spawning
	
	/// <summary>
	/// Starting area where the stairs are, probably don't want much combat here.
	/// </summary>
	private Rect bottomLeftCubby;

	/// <summary>
	/// The first bridge that connects the two bottom sections. Combat here for sure.
	/// </summary>
	private Rect bottomBridgeArea;

	/// <summary>
	/// Tall rectangle in the bottom right of the map. Probably want some solid combat here,
	/// as well as junk obstacles to move around.
	/// </summary>
	private Rect bottomRightCubby;

	/// <summary>
	/// Second bridge, further along the map. Great for fighting 
	/// </summary>
	private Rect middleHorizontalBridgeArea;

	/// <summary>
	/// The section full of jumpable islands between the two bottom bridges.
	/// Lots of holes here, so make sure to use many attempts when spawning monsters.
	/// Advise ranged attack monsters, as melee monsters would be sitting ducks.
	/// </summary>
	private Rect bottomFloatingIslandArea;
	
	/// <summary>
	/// Center area with walls and the teleporter. Most fierce combat can go here
	/// </summary>
	private Rect middleCombatAreaRect;

	/// <summary>
	/// Final bridge that leads to the exit. Maybe a low-combat cooldown? 
	/// </summary>
	private Rect topHorizontalBridgeArea;

	/// <summary>
	/// Final cubby in top right, where the exit stairs can go. Big combat or
	/// breathing room? Who can say!?
	/// </summary>
	private Rect exitAreaCubby;

	/// <summary>
	/// Challenge area where the three Husyn prototypes live. Probably want to leave it chill,
	/// but it is up to you.
	/// </summary>
	private Rect topLeftChallengeRect;
	
	/// <summary>
	/// Where the final teleporter in the puzzle takes you.
	/// Not a rectangle.
	/// </summary>
	private Vector2 startOfChallengeAreaLoc;

    /// <summary>
    /// Our table of various breakables and blocking objects spawned at random
    /// </summary>
    private ActorTable possibleMapObjects;
    #endregion

    public Map_DragonRobot_BridgeArea() : base()
	{
		
	}
	
	public Map_DragonRobot_BridgeArea(int mFloor)
	{
		Initialize(mFloor);
	}

	/// <summary>
	/// Override the parent to make a mostly hole-filled mess of bridges and jumpable spaces.
	/// </summary>
	protected override void BuildBaseFloorLayout()
	{
		/*
		 * Start in the bottom left, and make your way over
		 * a series of simple bridges that lead to the the goal. Along the way, there are floating
		 * platforms that can be jumped to.
		 */

		var mapWidthAndHeight = dungeonLevelData.size;
		var playableSpaceAnchor = new Vector2(2,2);
		//we are in a giant mess of holes
		ConvertAreaToHoles(2,2, mapWidthAndHeight - 4, mapWidthAndHeight - 4);
		
		//starting zone, little cubby to get rolling in
		ConvertAreaToTileType(playableSpaceAnchor, 8, 12, TileTypes.WALL);
		var floorAreaAnchor = playableSpaceAnchor + Vector2.one;
		ConvertAreaToTileType(floorAreaAnchor, 6, 10, TileTypes.GROUND);
		bottomLeftCubby = new Rect(floorAreaAnchor.x, floorAreaAnchor.y, 6, 10);

		//right side area
		var bottomRightAnchor = new Vector2(mapWidthAndHeight - 10, 2);
		ConvertAreaToTileType(bottomRightAnchor, 8, 35, TileTypes.WALL);
		var bottomRightfloorAreaAnchor = bottomRightAnchor + Vector2.one;
		ConvertAreaToTileType(bottomRightfloorAreaAnchor, 6, 33, TileTypes.GROUND);
		bottomRightCubby = new Rect(bottomRightfloorAreaAnchor.x, bottomRightfloorAreaAnchor.y, 6, 33);

		//bridge from left of map to right along the bottom. Do this after creating the two bottom rooms
		//so that the bridge can turn the walls to floors.
		var bottomBridgeAnchor = playableSpaceAnchor;
		bottomBridgeAnchor.x += 7;
		bottomBridgeAnchor.y += 4;
		var bottomBridgeLength = mapWidthAndHeight - 16;
		ConvertAreaToTileType(bottomBridgeAnchor, bottomBridgeLength, 5, TileTypes.GROUND);
		bottomBridgeArea = new Rect(bottomBridgeAnchor.x, bottomBridgeAnchor.y, bottomBridgeLength, 5);
		
		//create hole/pillar pattern along the way
		//======.P.======.P.======.P.======.P.======
		//40 tiles long,
		//add 5 on each side and that's 50, which is very close to 52, which should be the size of the bridge
		var holeAndPillarAnchor = bottomBridgeAnchor;
		holeAndPillarAnchor.x += 5;
		holeAndPillarAnchor.y += 2;

		for (int t = 0; t < 5; t++)
		{
			//create holes
			CreateHoleAndPillarLine(holeAndPillarAnchor, t < 4);
			//move along
			holeAndPillarAnchor.x += 9;
		}
		
		//second bridge, bottom of which will be 18 tiles above the current bridge and half as long
		//adding 24 because the bridge is 5 tall.
		var middleHorizontalBridgeAnchor = bottomBridgeAnchor;
		middleHorizontalBridgeAnchor.y += 23;
		middleHorizontalBridgeAnchor.x += 20;
		var midBridgeLength = bottomBridgeLength - 20;
		ConvertAreaToTileType(middleHorizontalBridgeAnchor, midBridgeLength, 5, TileTypes.GROUND);
		middleHorizontalBridgeArea = new Rect(middleHorizontalBridgeAnchor.x, middleHorizontalBridgeAnchor.y, 
			midBridgeLength, 5);

		//more of this
		holeAndPillarAnchor = middleHorizontalBridgeAnchor;
		holeAndPillarAnchor.x += 4;
		holeAndPillarAnchor.y += 2;
		for (int t = 0; t < 3; t++)
		{
			//create holes
			CreateHoleAndPillarLine(holeAndPillarAnchor, t < 2);
			//move along
			holeAndPillarAnchor.x += 9;
		}

		//vertical bridge in middle, leads to top horizontal bridge, but also has combat area in center of it.
		var verticalBridgeAnchor = middleHorizontalBridgeAnchor;
		ConvertAreaToTileType(verticalBridgeAnchor, 5, 32, TileTypes.GROUND);
		
		//middle combat area in that bridge
		var middleCombatAreaAnchor = verticalBridgeAnchor;
		middleCombatAreaAnchor.x -= 5;
		middleCombatAreaAnchor.y += 16;
		ConvertAreaToTileType(middleCombatAreaAnchor, 15, 12, TileTypes.GROUND);
		middleCombatAreaRect = new Rect(middleCombatAreaAnchor.x, middleCombatAreaAnchor.y, 15, 12);
		
		//carve out walls, teleporter to puzzle, color gates, water, etc. 
		CreateMiddleCombatArea(middleCombatAreaRect);
		
		
		//exit area in the top right
		var exitAreaAnchor = new Vector2( mapWidthAndHeight -9, mapWidthAndHeight - 13 );
		ConvertAreaToTileType(exitAreaAnchor, 8, 12, TileTypes.WALL);
		exitAreaAnchor += Vector2.one;
		ConvertAreaToTileType(exitAreaAnchor, 6, 10, TileTypes.GROUND);
		exitAreaCubby = new Rect(exitAreaAnchor.x, exitAreaAnchor.y, 6, 10);

		//draw a bridge over it
		var topBridgeAnchor = verticalBridgeAnchor;
		topBridgeAnchor.y += 30;
		ConvertAreaToTileType(topBridgeAnchor, midBridgeLength, 5, TileTypes.GROUND);
		topHorizontalBridgeArea = new Rect(topBridgeAnchor.x, topBridgeAnchor.y, midBridgeLength, 5);
		
		//EVEN MORE OF THIS
		holeAndPillarAnchor = topBridgeAnchor;
		holeAndPillarAnchor.x += 4;
		holeAndPillarAnchor.y += 2;
		for (int t = 0; t < 3; t++)
		{
			//create holes
			CreateHoleAndPillarLine(holeAndPillarAnchor, t < 2);
			//move along
			holeAndPillarAnchor.x += 9;
		}
		
		//some jumpable islands in the southeast
		CreateBottomRightIslandSection( new Vector2(mapWidthAndHeight / 2, bottomBridgeAnchor.y + 7));
		
		//create the 3 Prototype Husyn Challenge in the top left
		//
		// -2, -2, -15 == -2 for the wall border, -2 for holes around sides, and then -15 to reach the bottom
		// of a 15 tall area.
		topLeftChallengeRect = new Rect(4, mapWidthAndHeight - 2- 2 - 15, 12, 15);
		CreateTopLeftChallengeArea(topLeftChallengeRect);
		
		//puzzle time, since the challenge island is ready and the destination set.
		BuildTeleporterIslandPuzzle();
		
		//starting stairway
		heroStartTile.x = playableSpaceAnchor.x + 2;
		heroStartTile.y = playableSpaceAnchor.y + 2;
        
        Vector2 searchPos = new Vector2(63f, 63f);
        MapTileData finishStairsLocation = GetRandomEmptyTile(searchPos, 1, true, true, false);

        Stairs toNextLevel = SpawnStairsAtLocation(false, finishStairsLocation, -1);
    }

	/// <summary>
	/// Create a top left challenge area that is reached by the teleporter puzzle. Include three Prototype Husyn,
	/// some treasure, walls, and an escape vector.
	/// </summary>
	/// <param name="topLeftChallengeRect"></param>
	private void CreateTopLeftChallengeArea(Rect topLeftChallengeRect)
	{
		//	we arrive at the bottom center of the area, and are invited to move north.
		//	when that happens, we see three prototype husyn chambers and some treasure. Good luck!
		ConvertAreaToTileType(topLeftChallengeRect.min, (int)topLeftChallengeRect.width, (int)topLeftChallengeRect.height, TileTypes.GROUND);

		startOfChallengeAreaLoc = new Vector2(topLeftChallengeRect.x + 5, topLeftChallengeRect.y + 2);
		
		//start placing walls in the corners and on the sides
		// stored as array pairs:
		//  * anchor location
		//  * width/height of change-to-wall area
		var wallLocArray = new Vector2[]
		{
			//bottom rows
			new Vector2(1, 2),
			new Vector2(3, 1),

			new Vector2(8, 2),
			new Vector2(3, 1),

			//bottom middle row
			new Vector2(3, 4),
			new Vector2(6, 1),

			//two middle columns
			new Vector2(4, 6),
			new Vector2(1, 2),

			new Vector2(7, 6),
			new Vector2(1, 2),

			//middle T section left
			new Vector2(1, 9),
			new Vector2(4, 1),

			new Vector2(2, 7),
			new Vector2(1, 3),

			//middle T section right
			new Vector2(7, 9),
			new Vector2(4, 1),
			
			new Vector2(9, 7),
			new Vector2(1, 3),

			//top corner left
			new Vector2(1, 13),
			new Vector2(2, 1),

			new Vector2(1, 12),
			new Vector2(1, 2),

			//top corner right
			new Vector2(9, 13),
			new Vector2(2, 1),

			new Vector2(10, 12),
			new Vector2(1, 2),

		};

		//use each vector pair to make wall sections
		for (int idx = 0; idx < wallLocArray.Length; idx += 2)
		{
			//the anchor of each wall section is relative to the combat area
			var anchor = wallLocArray[idx] + topLeftChallengeRect.min;
			var range = wallLocArray[idx + 1];
			ConvertAreaToTileType(anchor, (int)range.x, (int)range.y, TileTypes.WALL);
		}
		
		//create husyn up top
		
		//left
		Monster stasisTube = MonsterManagerScript.CreateMonster("exp1_mon_prototype_husyn_stasis", true, 
			true, false, 0f, false);
			
		OnEnemyMonsterSpawned(this, stasisTube, false);
		PlaceActor(stasisTube, GetTile(topLeftChallengeRect.min + new Vector2(3, 11)));

		//center
		stasisTube = MonsterManagerScript.CreateMonster("exp1_mon_prototype_husyn_stasis", true, 
			true, false, 0f, false);
			
		OnEnemyMonsterSpawned(this, stasisTube, false);
		PlaceActor(stasisTube, GetTile(topLeftChallengeRect.min + new Vector2(5, 12)));
		
		//this one has a larger sense range
		stasisTube.SetActorData("sense_range", 3);

		//right
		stasisTube = MonsterManagerScript.CreateMonster("exp1_mon_prototype_husyn_stasis", true, 
			true, false, 0f, false);
			
		OnEnemyMonsterSpawned(this, stasisTube, false);
		PlaceActor(stasisTube, GetTile(topLeftChallengeRect.min + new Vector2(7, 11)));

		//rewards -- can we add nice loots to these?
		CreateDestructibleInTile(GetTile(topLeftChallengeRect.min + new Vector2(4, 14)), "obj_superchest");
		CreateDestructibleInTile(GetTile(topLeftChallengeRect.min + new Vector2(7, 14)), "obj_superchest");
		
		//escapipe
		CreateTeleporterAtLocationWithDest(topLeftChallengeRect.min, startOfTeleportPuzzleLoc + Vector2.down,
			"exp1_teleport_tile");

	}

	/// <summary>
	/// Carve out the walls, color forcefields, and the teleporter to the puzzle start. See reference image
	/// in the Image Reference folder
	/// </summary>
	/// <param name="middleCombatAreaRect">Bounds of the floor area we can paint in.</param>
	private void CreateMiddleCombatArea(Rect middleCombatAreaRect)
	{
		//start placing walls in the corners and on the sides
		// stored as array pairs:
		//  * anchor location
		//  * width/height of change-to-wall area
		var wallLocArray = new Vector2[]
		{
			new Vector2(1, 1),
			new Vector2(3, 1),

			new Vector2(1, 1),
			new Vector2(1, 3),

			new Vector2(1, 9),
			new Vector2(1, 1),

			new Vector2(3, 5),
			new Vector2(1, 3),

			new Vector2(6, 11),
			new Vector2(3, 1),

			new Vector2(5, 8),
			new Vector2(5, 1),

			new Vector2(5, 7),
			new Vector2(1, 1),

			new Vector2(5, 5),
			new Vector2(1, 1),

			new Vector2(9, 7),
			new Vector2(1, 1),

			new Vector2(9, 5),
			new Vector2(1, 1),

			new Vector2(5, 4),
			new Vector2(2, 1),

			new Vector2(8, 4),
			new Vector2(2, 1),

			new Vector2(6, 1),
			new Vector2(1, 1),

			new Vector2(8, 1),
			new Vector2(1, 1),

			new Vector2(11, 1),
			new Vector2(3, 1),

			new Vector2(13, 1),
			new Vector2(1, 3),

			new Vector2(11, 5),
			new Vector2(1, 3),

			new Vector2(13, 9),
			new Vector2(1, 1),

		};

		//use each vector pair to make wall sections
		for (int idx = 0; idx < wallLocArray.Length; idx += 2)
		{
			//the anchor of each wall section is relative to the combat area
			var anchor = wallLocArray[idx] + middleCombatAreaRect.min;
			var range = wallLocArray[idx + 1];
			ConvertAreaToTileType(anchor, (int)range.x, (int)range.y, TileTypes.WALL);
		}

		
		//place forcefields
		CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(7, 4)), forcefieldRefsByLockColor[(int) LockColors.red]);
		CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(5, 6)), forcefieldRefsByLockColor[(int) LockColors.blue]);
		CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(9, 6)), forcefieldRefsByLockColor[(int) LockColors.yellow]);
		
		//teleporter in the middle, store that location
		//we don't know where the destination is just yet.
		startOfTeleportPuzzleDestructible = CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(7,6)), "exp1_teleport_tile");
		startOfTeleportPuzzleLoc = middleCombatAreaRect.min + new Vector2(7, 6);
		
		//water to the sides
		CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(6,7)), "obj_regenfountain");
		CreateDestructibleInTile(GetTile(middleCombatAreaRect.min + new Vector2(8,7)), "obj_regenfountain");

	}

	/// <summary>
	/// Not a puzzle area, just some places to jump to between bridge sections
	/// </summary>
	void CreateBottomRightIslandSection( Vector2 bottomRightAnchor)
	{
		//start placing islands around that can be reached by hook,
		//vine jump or shadow jump. Or whatever.
		var islandMarker = bottomRightAnchor + Vector2.one;
		
		//zig zaggy column
		CreateFloatingIslandInHole(islandMarker);
		islandMarker += new Vector2(5, 3);
		CreateFloatingIslandInHole(islandMarker);
		islandMarker += new Vector2(-5, 3);
		CreateFloatingIslandInHole(islandMarker);
		
		//another, but mirrored
		islandMarker = bottomRightAnchor + Vector2.one;
		islandMarker.x += 15;
		CreateFloatingIslandInHole(islandMarker);
		islandMarker += new Vector2(-5, 3);
		CreateFloatingIslandInHole(islandMarker);
		islandMarker += new Vector2(5, 3);
		CreateFloatingIslandInHole(islandMarker);
		
		//a centerpiece -- don't carve holes, since these two will 
		//be directly adjacent.
		islandMarker = bottomRightAnchor + new Vector2(7, 9);
		CreateFloatingIslandInHole(islandMarker, false);
		islandMarker.x += 3;
		CreateFloatingIslandInHole(islandMarker, false);

		//two more, closer to the top bridge
		islandMarker.x += 6;
		islandMarker.y += 3;
		CreateFloatingIslandInHole(islandMarker);
		islandMarker.x -= 15;
		CreateFloatingIslandInHole(islandMarker);

		//mark this location for later monster spawning
		bottomFloatingIslandArea = new Rect(bottomRightAnchor.x, bottomRightAnchor.y, 17, 15);
	}
	

	/// <summary>
	/// Create a decorative map construct along a horizontal bridge
	/// </summary>
	/// <param name="startVec"></param>
	/// <param name="includePillar"></param>
	void CreateHoleAndPillarLine(Vector2 startVec, bool includePillar)
	{
		//create holes
		ConvertAreaToHoles((int)startVec.x, (int)startVec.y, 6, 1);
			
		//add pillar
		if (includePillar)
		{
			var pillarLoc = startVec;
			pillarLoc.x += 7;
			CreateDestructibleInTile(GetTile(pillarLoc), "exp1_glowing_cyber_pillar");
		}
	}
	
	/// <inheritdoc />
	protected override void PlaceEncountersInSections()
	{
        //No need to call the base function, just use this to place encounters 
        //loot and fountains in the various areas above. See the region called
        //"Rectangles for Monster Spawning"

        SeedRectWithActors(bottomLeftCubby, "bottomLeftCubby", 0.05f, 0.02f, 0.04f, 0.2f,3);
        SeedRectWithActors(bottomBridgeArea, "bottomBridgeArea", 0.03f, 0.01f, 0.035f, 0.2f,4);
        SeedRectWithActors(bottomRightCubby, "bottomRightCubby", 0.03f, 0f, 0.03f, 0.2f,4);
        SeedRectWithActors(middleHorizontalBridgeArea, "middleHorizontalBridgeArea", 0.03f, 0.01f, 0.03f, 0.2f,4);
        SeedRectWithActors(bottomFloatingIslandArea, "bottomFloatingIslandArea", 0.08f, 0.09f, 0f, 0.2f,2);
        SeedRectWithActors(middleCombatAreaRect, "middleCombatAreaRect", 0.05f, 0, 0.04f, 0.2f,2);
        SeedRectWithActors(topHorizontalBridgeArea, "topHorizontalBridgeArea", 0f, 0f, 0.05f, 0.2f,4);
        SeedRectWithActors(exitAreaCubby, "exitAreaCubby", 0, 0, 0.07f, 0f, 1);
        SeedRectWithActors(topLeftChallengeRect, "topLeftChallengeRect", 0f, 0.01f, 0.06f, 0.2f,4);

    }

	public struct TeleporterPuzzleIslandData
	{
		//where the hero is placed when she is teleported here
		public Vector2 spawnLocation;

		//string ID used for linking
		public string id;

		//Where do the teleporters take us?
		public string leftDestination;
		public string rightDestination;

		public Destructible leftTele;
		public Destructible rightTele;
	}

	private Dictionary<string, TeleporterPuzzleIslandData> puzzleIslandDict;
	private Dictionary<string, Vector2> puzzleSpecialDestinationDict;
	private Destructible startOfTeleportPuzzleDestructible;


	void BuildTeleporterIslandPuzzle()
	{
		puzzleIslandDict = new Dictionary<string, TeleporterPuzzleIslandData>();
		puzzleSpecialDestinationDict = new Dictionary<string, Vector2>();
		
		//Place some of these on the right of the map, range
		//x 40 y 39 to max x 55 y 52
		
		//create a collection of destinations
		//0 - 9 == A -> J in the image. 
		var destLocations = new Vector2[]
		{
			new Vector2(6, 22),
			new Vector2(40, 39),
			new Vector2(7, 39),
			new Vector2(14, 25),
			new Vector2(45, 45),
			new Vector2(10, 33),
			new Vector2(50, 40),
			new Vector2(21, 32),
			new Vector2(21, 39),
			new Vector2(54, 51),
		};
		
		//special destinations
		puzzleSpecialDestinationDict["goal"] = startOfChallengeAreaLoc;
		
		//these four areas are places you get sent for making too many wrong decisions in the puzzle.
		//if they are too far from the origin point, the puzzle becomes super frustrating. 
		puzzleSpecialDestinationDict["mainpath_1"] = new Vector2( 0, 4) + startOfChallengeAreaLoc;
		puzzleSpecialDestinationDict["mainpath_2"] = new Vector2( 0, -4) + startOfChallengeAreaLoc;
		puzzleSpecialDestinationDict["mainpath_3"] = new Vector2( -3, 0) + startOfChallengeAreaLoc;
		puzzleSpecialDestinationDict["mainpath_4"] = new Vector2( 3, 0) + startOfChallengeAreaLoc;

		//here are 10 islands that all have teleporters that take you somewhere.
		CreateTeleporterIsland(destLocations[0], "a", "b", "d");
		CreateTeleporterIsland(destLocations[1], "b", "e", "c");
		CreateTeleporterIsland(destLocations[2], "c", "goal", "f");
		CreateTeleporterIsland(destLocations[3], "d", "g", "h");
		CreateTeleporterIsland(destLocations[4], "e", "h", "a");
		CreateTeleporterIsland(destLocations[5], "f", "i", "j");
		CreateTeleporterIsland(destLocations[6], "g", "a", "mainpath_1");
		CreateTeleporterIsland(destLocations[7], "h", "g", "mainpath_2");
		CreateTeleporterIsland(destLocations[8], "i", "e", "mainpath_3");
		CreateTeleporterIsland(destLocations[9], "j", "b", "mainpath_4");
		
		LinkTeleporterIslandPuzzle();

	}
	
	/// <summary>
	/// Create an island in the holespace, add two teleporters to it. Once all islands are created,
	/// link them together using the linker function
	/// </summary>
	/// <param name="location"></param>
	/// <param name="islandID"></param>
	/// <param name="teleporterLeftDestination">Use the ID of another island, 'goal' for the final destination,
	/// 'start' for the origin teleporter, or 'mainpath_#' for one of the return to mainpath calls.</param>
	/// <param name="teleporterRightDestination"></param>
	void CreateTeleporterIsland(Vector2 location, string islandID, string teleporterLeftDestination,
		string teleporterRightDestination)
	{
		//make the place we're heading to
		CreateFloatingIslandInHole(location);

		//store the data
		var newData = new TeleporterPuzzleIslandData();
		newData.id = islandID;
		newData.leftDestination = teleporterLeftDestination;
		newData.rightDestination = teleporterRightDestination;

		//spawn location is just beneath the origin pillar
		newData.spawnLocation = location;
		newData.spawnLocation.y -= 1;
		
		//teleporters are above and to the sides of the origin pillar
		newData.leftTele = CreateDestructibleInTile(GetTile((int)location.x -1, (int)location.y+1), "exp1_teleport_tile");
		newData.rightTele = CreateDestructibleInTile(GetTile((int)location.x +1, (int)location.y+1), "exp1_teleport_tile");
		
		//keep track of it
		puzzleIslandDict.Add(islandID, newData);
	}

	/// <summary>
	/// Connect all the islands in the puzzle, including sending players back to the main path if they mess up.
	/// </summary>
	void LinkTeleporterIslandPuzzle()
	{
		//our initial teleporter must go to island A
		var islandALocation = puzzleIslandDict["a"].spawnLocation;
		startOfTeleportPuzzleDestructible.SetActorData("teleport_dest_x",(int)islandALocation.x);
		startOfTeleportPuzzleDestructible.SetActorData("teleport_dest_y",(int)islandALocation.y);
		
		foreach (var kvp in puzzleIslandDict)
		{
			var currentData = kvp.Value;
			ConnectTeleporter(currentData.leftTele, currentData.leftDestination);
			ConnectTeleporter(currentData.rightTele, currentData.rightDestination);
		}
	}

	/// <summary>
	/// Points a teleporter at a destination based on puzzle data
	/// </summary>
	/// <param name="tp">Our teleporter</param>
	/// <param name="goalID">An ID that matches an existing island or special goal</param>
	void ConnectTeleporter(Destructible tp, string goalID)
	{
		Vector2 dest = Vector2.zero;
		
		//check the goalID, if it is not just a letter, it is a SPECIAL LOCATION 
		if (puzzleSpecialDestinationDict.ContainsKey(goalID))
		{
			dest = puzzleSpecialDestinationDict[goalID];
		}
		else
		{
			var islandTarget = puzzleIslandDict[goalID];
			dest = islandTarget.spawnLocation;
		}
		
		tp.SetActorData("teleport_dest_x", (int)dest.x);
		tp.SetActorData("teleport_dest_y", (int)dest.y);
	}

    /// <summary>
    /// Fills the given rectangle with monsters, fountains, and objects (breakable loot). Density is a 0-1 percentage, i.e. 0.5f = 50% chance to spawn monster/fountain per tile
    /// </summary>
    /// <param name="r">Rectangle to populate</param>
    /// <param name="id">String ID for our own reference purposes</param>
    /// <param name="monsterDensity">Density of monsters as a % per tile</param>
    /// <param name="fountainDensity">Density of fountains as a % per tile</param>
    /// <param name="objectDensity">Density of destructibles as a % per tile</param>
    /// <param name="championChance">% chance to generate a champion</param>
    void SeedRectWithActors(Rect r, string id, float monsterDensity, float fountainDensity, float objectDensity, float championChance, int minMonsterDistance)
    {
        List<MapTileData> usableTiles = new List<MapTileData>();
        for (int x = (int)r.min.x; x < (int)r.max.x; x++)
        {
            for (int y = (int)r.min.y; y < (int)r.max.y; y++)
            {
                if (x < 1 || x > columns - 2 || y < 1 || y > rows - 2) continue;
                if (mapArray[x,y].tileType == TileTypes.GROUND && mapArray[x,y].GetAllActors().Count == 0)
                {
                    usableTiles.Add(mapArray[x, y]);
                }
            }
        }

        int numTiles = usableTiles.Count;
        int targetMonsterCount = (int)(monsterDensity * numTiles);
       
        if (possibleMapObjects == null)
        {
            possibleMapObjects = new ActorTable();
            possibleMapObjects.AddToTable("obj_ruined_future_obstacle_01", 110);
            possibleMapObjects.AddToTable("obj_ruined_future_obstacle_02", 110);
            possibleMapObjects.AddToTable("obj_ruined_future_obstacle_03", 110);
            possibleMapObjects.AddToTable("obj_ruined_robot_scrap_heap", 150);
            possibleMapObjects.AddToTable("obj_shinymetalorb", 70);
            possibleMapObjects.AddToTable("obj_smallmetalchest", 50);
            possibleMapObjects.AddToTable("obj_smallmetalchest", 25);
            possibleMapObjects.AddToTable("obj_shinymetalchest", 10);
        }

        ActorTable monsterSpawnTableToUse = dungeonLevelData.spawnTable;
        if (id == "bottomFloatingIslandArea")
        {
            monsterSpawnTableToUse = GameMasterScript.GetSpawnTable("robotdungeon_ranged");
        }


        HashSet<MapTileData> usedTiles = new HashSet<MapTileData>();

        for (int i = 0; i < targetMonsterCount; i++)
        {
            bool validTile = false;
            MapTileData spawn = null;
            int attempts = 0;
            while (!validTile)
            {                
                attempts++;
                if (attempts >= 100)
                {
                    Debug.Log("Took too long to place monster " + i + " in " + id);
                    break;
                }
                validTile = true;
                spawn = usableTiles[UnityEngine.Random.Range(0, usableTiles.Count)];
                if (usedTiles.Contains(spawn)) continue;
                foreach (MapTileData mtd in usedTiles)
                {
                    if (MapMasterScript.GetGridDistance(spawn.pos, mtd.pos) < minMonsterDistance)
                    {
                        validTile = false;
                        break;
                    }
                }
            }

            if (!validTile) continue;

            Monster mSpawn = MonsterManagerScript.CreateMonster(monsterSpawnTableToUse.GetRandomActorRef(), true, true, false, 0f, false);
            OnEnemyMonsterSpawned(this, mSpawn, false);
            
            PlaceActor(mSpawn, spawn);
            mSpawn.SetSpawnPos(spawn.pos);
            usableTiles.Remove(spawn);
            usedTiles.Add(spawn);
        }

        numTiles = usableTiles.Count;
        int targetObjectCount = (int)(objectDensity * numTiles);

        usedTiles.Clear();

        // For our various destructibles, make sure they are NOT adjacent to one another so we don't have potential
        // unwalkable paths
        for (int i = 0; i < targetObjectCount; i++)
        {
            MapTileData spawn = null;
            bool validTile = false;
            int attempts = 0;
            while (!validTile)
            {
                attempts++;
                if (attempts >= 50) break;
                validTile = true;
                spawn = usableTiles[UnityEngine.Random.Range(0, usableTiles.Count)];
                foreach(MapTileData mtd in usedTiles)
                {
                    if (MapMasterScript.GetGridDistance(spawn.pos, mtd.pos) == 1)
                    {
                        validTile = false;
                        break;
                    }
                }
            }
            
            Destructible dest = CreateDestructibleInTile(spawn, possibleMapObjects.GetRandomActorRef());
            usableTiles.Remove(spawn);
            usedTiles.Add(spawn);
        }

        numTiles = usableTiles.Count;
        int targetFountainCount = (int)(fountainDensity * numTiles);

        for (int i = 0; i < targetFountainCount; i++)
        {
            MapTileData spawn = usableTiles[UnityEngine.Random.Range(0, usableTiles.Count)];
            Destructible dest = CreateDestructibleInTile(spawn, "obj_regenfountain");
            usableTiles.Remove(spawn);
        }

        //Debug.Log("For " + id + ", " + targetMonsterCount + " mons " + targetFountainCount + " fountains " + targetObjectCount + " objects " + minMonsterDistance + " min monster dist");
    }
}
