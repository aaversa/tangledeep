using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.XPath;
using Rewired.Data.Mapping;
using Steamworks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Map_DragonRobot : Map
{
	protected HashSet<MapTileData> convertToHoles;
	
	/// <summary>
	/// The number of sections in each column and row, always the same because the map is square
	/// </summary>
	protected int sectionsPerColumnAndRow;

	/// <summary>
	/// the number of tiles in a sections height or width. -2 for number of GROUND tiles.
	/// </summary>
	protected const int sizeOfSection = 8;


	/// <summary>
	/// The section that contains the opening stairway. Don't put any monsters here.
	/// </summary>
	protected int startingSection;
	
	/// <summary>
	/// Sections that are being used on the main path
	/// </summary>
	protected HashSet<int> setMainPathSections;

	/// <summary>
	/// Side areas that have been carved up.
	/// </summary>
	protected HashSet<int> sidePathSections;

	protected enum LockColors
	{
		red = 0,
		yellow,
		blue,
		max
	}

	/// <summary>
	/// Sections of the map that are placed behind colored doors
	/// </summary>
	protected HashSet<int>[] lockedSections;

	protected static string[] forcefieldRefsByLockColor;

#region Door and Lock Code at Generation Time

	/// <summary>
	/// Used for planning the lock colors as sections extend from the main path.
	/// </summary>
	class SectionNode
	{
		public int sectionIdx;
		public LockColors sectionColor;
		public List<SectionNode> neighbors;

		public SectionNode()
		{
			neighbors = new List<SectionNode>();
			sectionColor = LockColors.max;
		}
	}
	
	private Dictionary<int, SectionNode> allCarvedSectionNodes;
	
	/// <summary>
	/// A record of all places where two sections meet and have a door between them.
	/// </summary>
	private Dictionary<Vector2, LockColors> allDoorConnectionsAndLocks;

	/// <summary>
	/// Creates a new node for a given section if one does not exist.
	/// </summary>
	/// <param name="sectionIdx"></param>
	void CreateNodeForSection(int sectionIdx)
	{
		if (allCarvedSectionNodes.ContainsKey(sectionIdx)) return;
		
		var newNode = new SectionNode();
		newNode.sectionIdx = sectionIdx;
		allCarvedSectionNodes[sectionIdx] = newNode;
	}

	/// <summary>
	/// Sets the lock color for a given section node. Will create the node if it doesn't already exist.
	/// </summary>
	/// <param name="sectionIdx"></param>
	/// <param name="lockColor"></param>
	void SetLockColorForSectionNode(int sectionIdx, LockColors lockColor)
	{
		if (!allCarvedSectionNodes.ContainsKey(sectionIdx))
		{
			CreateNodeForSection(sectionIdx);
		}

		var node = allCarvedSectionNodes[sectionIdx];

		if (lockColor != node.sectionColor &&
		    lockColor == LockColors.max)
		{
			Debugger.Break();
		}
		
		node.sectionColor = lockColor;
		
	}

	/// <summary>
	/// Tell two sections that they are adjacent.
	/// </summary>
	/// <param name="section1"></param>
	/// <param name="section2"></param>
	void SetNodesAsNeighbors(int section1, int section2)
	{
		if (!allCarvedSectionNodes.ContainsKey(section1))
		{
			CreateNodeForSection(section1);
		}
		
		if (!allCarvedSectionNodes.ContainsKey(section2))
		{
			CreateNodeForSection(section2);
		}
		
		var n1 = allCarvedSectionNodes[section1];
		var n2 = allCarvedSectionNodes[section2];
		
		n1.neighbors.Add(n2);
		n2.neighbors.Add(n1);
	}


	/// <summary>
	/// March through every node we've carved out and check to see if we need to add
	/// locks between sections.
	/// </summary>
	void CreateAllRequiredLocksBetweenSections()
	{
		foreach (var kvp in allCarvedSectionNodes)
		{
			var checkNode = kvp.Value;
			
			//look at all my neighbors
			foreach (var bro in checkNode.neighbors)
			{
				//if we are not the same lock color, a door should arrive between us.
				if (bro.sectionColor != checkNode.sectionColor)
				{
					var comboKey = new Vector2( Math.Min(bro.sectionIdx, checkNode.sectionIdx), 
												Math.Max(bro.sectionIdx, checkNode.sectionIdx));
					
					//if we have already locked this connection, don't lock it again.
					if( allDoorConnectionsAndLocks.ContainsKey(comboKey)) continue;
					
					//pick a color that isn't clear/empty
					LockColors doorColor =
						bro.sectionColor == LockColors.max ? checkNode.sectionColor : bro.sectionColor;
					
					//create the door
					Create2x2OpeningBetweenAdjacentSections(checkNode.sectionIdx, bro.sectionIdx, doorColor);
					
					//store this for future reference
					allDoorConnectionsAndLocks.Add(comboKey, doorColor);
				}
			}
		}
	}
	
	
#endregion

	public Map_DragonRobot() : base()
	{
		
	}
	
	public Map_DragonRobot(int mFloor)
	{
		Initialize(mFloor);
	}

	/// <inheritdoc />
	protected override void Initialize(int mFloor)
	{
		base.Initialize(mFloor);
		forcefieldRefsByLockColor = new string[(int) LockColors.max];
		
		forcefieldRefsByLockColor[(int) LockColors.red] = "obj_forcefield_red";
		forcefieldRefsByLockColor[(int) LockColors.blue] = "obj_forcefield_blue";
		forcefieldRefsByLockColor[(int) LockColors.yellow] = "obj_forcefield_yellow";
		
		allCarvedSectionNodes = new Dictionary<int, SectionNode>();
		allDoorConnectionsAndLocks = new Dictionary<Vector2, LockColors>();
		

	}

	/// <inheritdoc />
	public override bool BuildRandomMap(ItemWorldMetaData itemWorldProperties)
	{
        mapAreaID = MapMasterScript.singletonMMS.mapAreaIDAssigner;

        convertToHoles = new HashSet<MapTileData>();
		floor = dungeonLevelData.floor;
		
		//define a map size based on data from elsewhere, but right now I don't have any.
		CreateTileArrays(dungeonLevelData.size);
		
		//start with a full canvas that we carves spaces out of
		FillMapWithNothing(TileTypes.WALL);

		//start with empty sections
		BuildBaseFloorLayout();

		//stairs down to previous floor, intro location
		SpawnStairsAtLocation(true, GetTile(heroStartTile));

		//test stuff around entrance
		/*
		var teleporter = CreateDestructibleInTile(GetTile((int)heroStartTile.x +1, (int)heroStartTile.y), "exp1_teleport_tile");
		
		CreateDestructibleInTile(GetTile((int)heroStartTile.x +2, (int)heroStartTile.y+1), "obj_forcefield_red");
		CreateDestructibleInTile(GetTile((int)heroStartTile.x +2, (int)heroStartTile.y), "obj_forcefield_blue");
		CreateDestructibleInTile(GetTile((int)heroStartTile.x +2, (int)heroStartTile.y-1), "obj_forcefield_yellow");
		
		// pick random valid tile for TEST TELEPORTER destination
        while (true)
        {
            int teleportX = UnityEngine.Random.Range(1, columns - 1);
            int teleportY = UnityEngine.Random.Range(1, rows - 1);
            if (mapArray[teleportX,teleportY].tileType == TileTypes.GROUND)
            {
                teleporter.SetActorData("teleport_dest_x", teleportX);
                teleporter.SetActorData("teleport_dest_y", teleportY);
                break;
            }
        }


		*/
		
		// === Post build clean up section =============================================================================
		
		//various map cleanups
		SetMapBoundVisuals();
		
		//Make sure we have ground in the right places so we can spawn bads and build
		//secret rooms
		PrepareGroundTiles();
		
		//seed with monsters
		PlaceEncountersInSections();
		
		//secret areas?
		
		//decor and layered stuff
		AddGrassLayer1ToMap();
		//AddWallChunkDecor(); No wall chunks in Future tileset for this map
		AddGrassLayer2ToMapAndBeautifyAll(); 
		
		//This will make our holes become reality. 																																																																																//our dreams of holes WILL BECOME WHOLE
		AddDecorAndFlowersAndTileDirections();

        //Because of how Holes work, our grass may have been screwed up, so clear, re-seed it and re-beautify just grass.
        for (int x = 1; x < columns-1; x++)
        {
            for (int y = 1; y < rows-1; y++)
            {
                mapArray[x, y].RemoveTag(LocationTags.GRASS);
                mapArray[x, y].RemoveTag(LocationTags.GRASS2);
            }
        }
        AddGrassLayer1ToMap();
        RemoveOneOffGrassTiles();
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                BeautifyTerrain(mapArray[x, y], LocationTags.GRASS, LocationTags.GRASS);
                if (mapArray[x,y].CheckTag(LocationTags.GRASS) && mapArray[x,y].indexOfGrassSpriteInAtlas < 0)
                {
                    Debug.Log(x + "," + y + " on " + floor + " has grass and is " + mapArray[x, y].visualGrassTileType + " but has index -1. Wat?");
                }
            }
        }

        //This converts tiles we tagged as lava, mud, elec etc to the actual tile
        AddEnvironmentDestructiblesToMap();

        //done?
        return true;

	}


	/// <summary>
	/// determine which tile is in the bottom left of this section
	/// </summary>
	/// <param name="sectionIdx"></param>
	/// <returns>The tile location that represents the bottom left of the section</returns>
	Vector2 GetLocationForCornerOfSection(int sectionIdx)
	{
		var sectionX = (sectionIdx % sectionsPerColumnAndRow) * sizeOfSection + 2;
		var sectionY = (sectionIdx / sectionsPerColumnAndRow) * sizeOfSection + 2;
		
		return new Vector2(sectionX, sectionY);
	}

	/// <summary>
	/// Takes two sections IDs, and clears out all the walls between them.
	/// The sections cannot be identical, and they must be on the same
	/// row or column
	/// </summary>
	/// <param name="idx1"></param>
	/// <param name="idx2"></param>
	void ClearWallsBetweenSections(int idx1, int idx2)
	{
		if (idx1 > idx2)
		{
			var temp = idx2;
			idx2 = idx1;
			idx1 = temp;
		}

		var lowPos = GetLocationForCornerOfSection(idx1);
		var highPos = GetLocationForCornerOfSection(idx2);

		//if they are on the same column
		if (lowPos.x == highPos.x)
		{
			ConvertAreaToTileType( (int)lowPos.x + 1, (int)lowPos.y +1, 6, 6 + (int)(highPos.y - lowPos.y), TileTypes.GROUND);
		}
		//or same row
		else if (lowPos.y == highPos.y)
		{
			ConvertAreaToTileType( (int)lowPos.x + 1, (int)lowPos.y +1, 6+ (int)(highPos.x - lowPos.x), 6, TileTypes.GROUND);
		}
		//neither!? 
		else
		{
			//uh
			Debug.LogError("uh");
		}

	}

	/// <summary>
	/// Clears out a 2x2 piece of wall between two adjacent sections. May also allow for
	/// something to be placed in that space, such as a secret breakable wall object, or
	/// a forcefield.
	/// </summary>
	/// <param name="idx1"></param>
	/// <param name="idx2"></param>
	/// <param name="optionalColorOfLockedForcefields"></param>
	void Create2x2OpeningBetweenAdjacentSections(int idx1, int idx2, 
		LockColors optionalColorOfLockedForcefields = LockColors.max)
	{
		if (idx1 > idx2)
		{
			var temp = idx2;
			idx2 = idx1;
			idx1 = temp;
		}

		var lowPos = GetLocationForCornerOfSection(idx1);
		var highPos = GetLocationForCornerOfSection(idx2);
		
		//the corner spot will be the least of the 2x2 tiles we need to clear.
		Vector2 leastCorner = Vector2.zero;
		//if they are on the same column
		if (lowPos.x == highPos.x)
		{
			leastCorner.y = highPos.y - 1;
			leastCorner.x = highPos.x + 3;
		}
		//or same row
		else if (lowPos.y == highPos.y)
		{
			leastCorner.x = highPos.x - 1;
			leastCorner.y = highPos.y + 3;
		}
		//neither!? 
		else
		{
			//uh
			Debug.LogError("uh");
		}

		//break walls and/or make things
		if (optionalColorOfLockedForcefields == LockColors.max)
		{
			ConvertAreaToTileType((int)leastCorner.x, (int)leastCorner.y, 2,2, TileTypes.GROUND);
		}
		else
		{
			CreateDestructiblesInArea((int) leastCorner.x, (int) leastCorner.y, 2, 2, 
				forcefieldRefsByLockColor[(int)optionalColorOfLockedForcefields],
				false, TileTypes.GROUND);
		}
		
	}
	
	/// <summary>
	/// Creates a series of 6x6 rooms with walls between them
	/// </summary>
	protected virtual void BuildBaseFloorLayout()
	{
		setMainPathSections = new HashSet<int>();
		sidePathSections = new HashSet<int>();
		lockedSections = new HashSet<int>[(int)LockColors.max];
		for (int t = 0; t < (int)LockColors.max; t++)
		{
			lockedSections[t] = new HashSet<int>();
		}
		
		//floor H and W are always the same
		//rooms are sizeOfSection by sizeOfSection, with sizeOfSection-2 floor tiles in the center
		//and a ring of wall tiles on the outside.
		//floors always have a two tile border for map edges
		//Subtract 4 from the size to get the number of playable tiles, divide that by sizeOfSection
		//to get the number of sections
		sectionsPerColumnAndRow = (dungeonLevelData.size - 4) / sizeOfSection;

		/*
		 *  x and y are the bottom left corner
		 *  there needs to be floor in a 6x6 by grid
		 *  one tile NE of that
		 *	var corner = GetLocationForCornerOfSection(idx);
		 *	ConvertAreaToTileType((int)corner.x+1, (int)corner.y+1, sizeOfSection -2,  sizeOfSection -2, TileTypes.GROUND);
		 */

		//The deeper into dragon town we go, the more likely we'll encounter locked doors.
		var chanceToLockDoor = dungeonLevelData.GetMetaData("chancetolockdoor") / 100.0f;
		
		//pick a start point somewhere in the leftmost column
		startingSection = UnityEngine.Random.Range(0, sectionsPerColumnAndRow) * sectionsPerColumnAndRow;
		int currentIdx = startingSection;
		bool weDoinThis = true;
		bool horizontalMovement = UnityEngine.Random.value < 0.5f;
		
		//our start tile is based on the starting index
		var startCorner = GetLocationForCornerOfSection(startingSection);
		heroStartTile = startCorner + Vector2.one + Vector2.one;

		//loop start -- create the main path
		while (weDoinThis)
		{
			//track the sections we are converting.
			setMainPathSections.Add(currentIdx);
			
			//pick a direction -- horizontal or vertical
			var currentColumn = currentIdx % sectionsPerColumnAndRow;
			var currentRow = currentIdx / sectionsPerColumnAndRow;
			
			//if we are in the final column (sectionsPerColumnAndRow -1), direction must be vertical.
			if (currentColumn == sectionsPerColumnAndRow -1)
			{
				horizontalMovement = false;
			}

			//if horizontal
			if (horizontalMovement)
			{
				//direction is east for 2-4 sections
				//but not past the final column (sectionsPerColumnAndRow -1)
				var delta = UnityEngine.Random.Range(2, 5);
				while (currentColumn < sectionsPerColumnAndRow - 1 &&
				       delta + currentColumn > sectionsPerColumnAndRow - 1)
				{
					delta = UnityEngine.Random.Range(1, 5);
				}

				//add these sections
				for (int idx = currentColumn; idx <= currentColumn + delta; idx++)
				{
					setMainPathSections.Add(idx + currentRow * sectionsPerColumnAndRow);
				}

				//track this new location
				currentColumn += delta;
	
			}
			//if vertical
			else
			{
				var targetRow = currentRow;
				//direction is N if current section is in bottom half
				if (currentRow < sectionsPerColumnAndRow / 2)
				{
					targetRow += UnityEngine.Random.Range(2, 5);
				}
				//otherwise S 
				else
				{
					targetRow -= UnityEngine.Random.Range(2, 5);
				}

				//clamp it up
				targetRow = Mathf.Clamp(targetRow, 0, 7);

				//assign each of these to the main path
				for (int idx = Math.Min(currentRow, targetRow); idx <= Math.Max(currentRow, targetRow); idx++)
				{
					setMainPathSections.Add(currentColumn + idx * sectionsPerColumnAndRow);
				}
				
				currentRow = targetRow;
			}
			
			//build line to goal section
			var goalIdx = currentColumn + currentRow * sectionsPerColumnAndRow;
			ClearWallsBetweenSections(currentIdx,goalIdx);
			
			//if goal section is in final column and
			//if we just moved vertically, we're done
			if (currentColumn == sectionsPerColumnAndRow - 1 &&
			    horizontalMovement == false)
			{
				weDoinThis = false;
			}

			//continue from our new location with a different direction
			currentIdx = goalIdx;
			horizontalMovement = !horizontalMovement;

		}
		
		//exit tile -- last section in the main path, stairs UP to next floor
		var endCorner = GetLocationForCornerOfSection(currentIdx);
		var exitStairLoc = endCorner + Vector2.one + Vector2.one;
		SpawnStairsAtLocation(false, GetTile(exitStairLoc));

		//Build some sections off to the side.
		int expansionCount = 0;
		int maxExpansions = 200;
		var mainPathArray = setMainPathSections.ToArray();
		while (expansionCount < maxExpansions)
		{
			expansionCount++;

			//pick a spot adjacent to the main path
			var choiceSection = mainPathArray[UnityEngine.Random.Range(0, mainPathArray.Length)];
			var adjacentSection = GetRandomAdjacentSection(choiceSection);
			
			//if the target spot is on the main path, try again
			if( setMainPathSections.Contains(adjacentSection)) continue;
			
			var numTimesToExtend = UnityEngine.Random.Range(1, 7);
			var extensionsMade = 0;
			
			//Here's where the chain starts. Opened on the main path, extended out into the dungeon,
			//and we keep track of which color lock we have, if any.
			LockColors sectionLockColor = LockColors.max;
			
			while (extensionsMade < numTimesToExtend)
			{
				//start with these two here
				CreateNodeForSection(choiceSection);
				CreateNodeForSection(adjacentSection);
				
				//we know what our color is, we don't yet know what the neighbor is or will be
				SetLockColorForSectionNode(choiceSection, sectionLockColor);
				
				bool carvingIntoNewArea = false;

				//if we have never used the adjacent section, mark it as used and 
				//carve it up
				if (!sidePathSections.Contains(adjacentSection))
				{
					sidePathSections.Add(adjacentSection);
					carvingIntoNewArea = true;
					
					//carve carve
					var corner = GetLocationForCornerOfSection(adjacentSection);
					ConvertAreaToTileType((int)corner.x+1, (int)corner.y+1, sizeOfSection -2,  sizeOfSection -2, TileTypes.GROUND);
					
					//if we are locked, this section is locked too, but we don't 
					//need to make a new lock, since we carving out rooms in an already-locked chain.
					if (sectionLockColor != LockColors.max)
					{
						lockedSections[(int) sectionLockColor].Add(adjacentSection);
						SetLockColorForSectionNode(adjacentSection, sectionLockColor);
					}
				}
				
				//	if we JUST left the main path, or if we're going into an existing space,
				//  assure use of a 2x2 which we can lock.
				//  But if it is a new space, and we're off the main path,
				//  then maybe create an opening that we can't lock.
				if (carvingIntoNewArea &&
				    extensionsMade > 0 &&
				    UnityEngine.Random.value < 0.33f)
				{
					//we are NOT setting nodes as neighbors here,
					//because we don't want a door between these two sections.
					ClearWallsBetweenSections(choiceSection, adjacentSection);
				}				
				else 
				{
					//If we are digging into a new area, and we have yet to place a lock down,
					//maybe do so.
					if (carvingIntoNewArea && sectionLockColor == LockColors.max)
					{
						//lock chance 
						if (UnityEngine.Random.value < chanceToLockDoor)
						{
							//pick a color, that is the new color.
							sectionLockColor = (LockColors)UnityEngine.Random.Range(0, (int)LockColors.max);
							lockedSections[(int) sectionLockColor].Add(adjacentSection);
							SetLockColorForSectionNode(adjacentSection, sectionLockColor);
							
						}
					}

					//place the opening -- but don't place the door, we'll do that later.
					SetNodesAsNeighbors(choiceSection, adjacentSection);
					Create2x2OpeningBetweenAdjacentSections(choiceSection, adjacentSection);
				}

				//we have extended correctly
				extensionsMade++;
				expansionCount++;
				
				//however! If we have built a door into an already existing area, start a new chain
				if (!carvingIntoNewArea)
				{
					extensionsMade = numTimesToExtend + 1;
				}

				//grab the next area
				var nextAdjacentSection = GetRandomAdjacentSection(adjacentSection);
			
				//If this next section is the one we just left, or on the main path, try again
				//until we're out of tries
				while (	extensionsMade < numTimesToExtend &&
						(setMainPathSections.Contains(nextAdjacentSection) ||
						nextAdjacentSection == choiceSection))
				{
					nextAdjacentSection = GetRandomAdjacentSection(adjacentSection);
					extensionsMade++;
				}
				
				//otherwise, prepare to move on
				choiceSection = adjacentSection;
				adjacentSection = nextAdjacentSection;

				//adjust our color to match the place we've moved in to.
				sectionLockColor = GetLockColorForSection(choiceSection);
			}
		}
		
		//place all the doors in all the areas we need to.
		CreateAllRequiredLocksBetweenSections();
		

		//Convert some walls to holes. Imperfect, because some areas may get rolles twice.
		var sidePathAsArray = sidePathSections.ToArray();
		float percentWallsToHoles = dungeonLevelData.GetMetaData("chanceforwallsasholes") / 100.0f;
		var numWallsToConvertToHoles = (int) (sidePathAsArray.Length * percentWallsToHoles);
		for (int t = 0; t < numWallsToConvertToHoles; t++)
		{
			var targetSideArea = sidePathAsArray[UnityEngine.Random.Range(0, sidePathAsArray.Length)];
			CreateHolesAroundRoomSection(targetSideArea);
		}

	}

	/// <summary>
	/// Determine which color gates a given section of the map.
	/// </summary>
	/// <param name="sectionID"></param>
	/// <returns></returns>
	private LockColors GetLockColorForSection(int sectionID)
	{
		for (LockColors t = 0; t < LockColors.max; t++)
		{
			if (lockedSections[(int)t].Contains(sectionID))
			{
				return t;
			}
		}

		return LockColors.max;
	}
	
	/// <summary>
	/// Converts the walls in a room AND
	/// tiles one space past them into holes.
	/// </summary>
	/// <param name="idxSection"></param>
	protected void CreateHolesAroundRoomSection(int idxSection)
	{
		//convert the walls (which are still marked as NOTHING)
		//around this section into holes.
		var corner = GetLocationForCornerOfSection(idxSection);
		corner.x--;
		corner.y--;
		ConvertAreaToHoles((int)corner.x, (int)corner.y, sizeOfSection + 2, sizeOfSection + 2, TileTypes.NOTHING);
	}

	/// <summary>
	/// Come back with a section adjacent to this one in one of four directions.
	/// </summary>
	/// <param name="idxCenter"></param>
	/// <returns></returns>
	protected int GetRandomAdjacentSection(int idxCenter)
	{
		int retSection = -1;
		while (retSection == -1)
		{
			var roll = UnityEngine.Random.Range(0, 4);
			var column = idxCenter % sectionsPerColumnAndRow;
			var row = idxCenter / sectionsPerColumnAndRow;
			
			switch (roll)
			{
				case 0:
					row++;
					break;
				case 1:
					column++;
					break;
				case 2:
					row--;
					break;
				case 3:
					column--;
					break;
			}

			if (row >= 0 &&
			    column >= 0 &&
			    row < sectionsPerColumnAndRow &&
			    column < sectionsPerColumnAndRow)
			{
				retSection = column + row * sectionsPerColumnAndRow;
			}
		}
	
		return retSection;
	}

	/// <summary>
	/// Goes through each carved out and main path section and looks into placing monsters, fountains,
	/// treasure, and other excitement.
	/// </summary>
	protected virtual void PlaceEncountersInSections()
	{
		//these are maximum values, there's a chance they won't all be placed.
		var numFountainsInLockedArea = 5;
		var numLootBoxesToPlace = 10;
		
		//Maybe fountains should live mostly in the side areas?
		var numFountainsInMainPath = 1;
		
		//We probably don't want more than one of these per map
		var numHusynPrototypesToPlace = 1;

		var stumpRefs = new string[]
		{
			"obj_ruined_future_obstacle_01",
			"obj_ruined_future_obstacle_02",
			"obj_ruined_future_obstacle_03"
		};
		
		
		//work through the main path. The column indicates difficulty level,
		//as harder monsters are further to the right.
		//don't put anything in the start section.
		foreach (var sectionIdx in setMainPathSections)
		{
			if (sectionIdx == startingSection) continue;

			//this bottom left corner will be the first free floor tile. GetLocation returns a wall tile.
			var bottomLeftCorner = GetLocationForCornerOfSection(sectionIdx);
			bottomLeftCorner += Vector2.one;

			//0-1.0f difficulty ratio
			var difficultyLevel = (float) (sectionIdx % sectionsPerColumnAndRow) / sectionsPerColumnAndRow;
			
			//monsters
			//Chance of beasts between x and y percent, based on difficulty
			var minChance = 0.5f;
			var monsterChance = minChance + 0.3f * difficultyLevel;
			if (UnityEngine.Random.value < monsterChance)
			{
				//Spawn 1, and sometimes 2 or 3. Don't add champions to the main path.
				SpawnRandomMonstersInSection(sectionIdx, Math.Max(1, UnityEngine.Random.Range(-2, 4)));
				
				//If we placed monsters here, there's no need for anything else.
				continue;

			}
			
			//fountain: the main path should have one, but more fountains should be hidden in the 
			//rooms off the beaten path.
			if (difficultyLevel > 0.5f && numFountainsInMainPath > 0)
			{
				CreateDestructibleInTile(GetTile(GetRandomFloorLocationInSection(sectionIdx)), "obj_regenfountain");
				numFountainsInMainPath--;
				continue;
			}
			
			//loots from a junked robot?
			var minLootChance = 0.05f;
			var lootChance = minLootChance + 0.3f * difficultyLevel;
			if (UnityEngine.Random.value < lootChance)
			{
				//junked robot
				CreateDestructibleInTile(GetTile(GetRandomFloorLocationInSection(sectionIdx)),
					"obj_ruined_robot_scrap_heap");
				
			}

			//junk? four corners or random placement? 
			var junkChance = 0.5f;
			if (UnityEngine.Random.value < junkChance)
			{
				//four corners
				if (UnityEngine.Random.value < 0.3f)
				{
					CreateDestructibleInTile(GetTile(bottomLeftCorner),
						stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
					CreateDestructibleInTile(GetTile((int)bottomLeftCorner.x + 5, (int)bottomLeftCorner.y),
						stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
					CreateDestructibleInTile(GetTile((int)bottomLeftCorner.x + 5, (int)bottomLeftCorner.y + 5),
						stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
					CreateDestructibleInTile(GetTile((int)bottomLeftCorner.x, (int)bottomLeftCorner.y + 5),
						stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
					
				}
				//just laying around
				else
				{
					var junkCount = UnityEngine.Random.Range(1, 5);
					var tries = 20;
					while (tries > 0 && junkCount > 0)
					{
						tries--;
						var junkLoc = GetRandomFloorLocationInSection(sectionIdx);
						var mtd = GetTile(junkLoc);
						if (CheckValidForDestructible(mtd))
						{
							CreateDestructibleInTile(GetTile(junkLoc),
								stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
							junkCount--;
						}
					}
					
				}
			}

		}
		
		//visit the side areas!
		//areas that are marked as locked should contain more interesting
		//or valuable encounters.
		foreach (var kvp in allCarvedSectionNodes)
		{
			var sectionIdx = kvp.Value.sectionIdx;
			if (setMainPathSections.Contains(sectionIdx)) continue;

			var bottomLeftCorner = GetLocationForCornerOfSection(sectionIdx);

			//0-1.0f difficulty ratio
			var difficultyLevel = (float) (sectionIdx % sectionsPerColumnAndRow) / sectionsPerColumnAndRow;

			//if it is NOT a locked area, maybe a monster, maybe some junk. Nothing serious.
			if (kvp.Value.sectionColor == LockColors.max)
			{
				//monsters
				//Chance of beasts between x and y percent, based on difficulty
				var minChance = 0.3f;
				var monsterChance = minChance + 0.3f * difficultyLevel;
				if (UnityEngine.Random.value < monsterChance)
				{
					//Spawn 1, and sometimes 2.
					//No champions in unlocked areas.
					SpawnRandomMonstersInSection(sectionIdx, Math.Max(1, UnityEngine.Random.Range(-2, 3)));
				}

				//loots from a junked robot?
				var minLootChance = 0.05f;
				var lootChance = minLootChance + 0.3f * difficultyLevel;
				if (UnityEngine.Random.value < lootChance)
				{
					//junked robot
					CreateDestructibleInTile(GetTile(GetRandomFloorLocationInSection(sectionIdx)),
						"obj_ruined_robot_scrap_heap");
				}
				

				//obstacle chance
				var junkChance = 0.6f;
				if (UnityEngine.Random.value < junkChance)
				{
					var junkCount = UnityEngine.Random.Range(1, 5);
					var tries = 20;
					while (tries > 0 && junkCount > 0)
					{
						tries--;
						var junkLoc = GetRandomFloorLocationInSection(sectionIdx);
						var mtd = GetTile(junkLoc);
						if (CheckValidForDestructible(mtd))
						{
							CreateDestructibleInTile(GetTile(junkLoc),
								stumpRefs[UnityEngine.Random.Range(0, stumpRefs.Length)]);
							junkCount--;
						}
					}
				}
				
				//no fountains
			}

			// locked areas below =======
			else
			{
				//if this is a difficult area, and locked, maybe we can spawn one of these bad boys
				if (difficultyLevel >= 0.8f &&
					numHusynPrototypesToPlace > 0)
				{
					numHusynPrototypesToPlace--;
					
					//spawn a husyn prototype!?
					var husynLoc = bottomLeftCorner + new Vector2(3, 3);
					Monster stasisTube = MonsterManagerScript.CreateMonster("exp1_mon_prototype_husyn_stasis", true,
						true, false, 0f, false);
	
					OnEnemyMonsterSpawned(this, stasisTube, false);
					PlaceActor(stasisTube, GetTile(husynLoc));
	
					//do nothing else with this room
					continue;
				}
				
				//otherwise: lootable junk, and maybe good treasure
				if( numLootBoxesToPlace > 0 )
				{
					numLootBoxesToPlace--;
					CreateDestructibleInTile(GetTile(GetRandomFloorLocationInSection(sectionIdx)),
					UnityEngine.Random.value <= 0.1f ? 
						"obj_shinymetalchest" :
						"obj_ruined_robot_scrap_heap");
				}

				//beests
				var minChance = 0.3f;
				var monsterChance = minChance + 0.3f * difficultyLevel;
				if (UnityEngine.Random.value < monsterChance)
				{
					//Spawn 1, and sometimes 2 or 3.
					//Rare champions in locked areas. 10% max chance farthest east, 
					//and less on the west side. 
					SpawnRandomMonstersInSection(sectionIdx, 
						Math.Max(1, UnityEngine.Random.Range(-2, 4)), 
						0.1f * difficultyLevel
						);
				}
				
				//water?
				if (numFountainsInLockedArea > 0  && UnityEngine.Random.value < 0.2f)
				{
					numFountainsInLockedArea--;
					CreateDestructibleInTile(GetTile(GetRandomFloorLocationInSection(sectionIdx)), "obj_regenfountain");
				}
				
			}
		}
		
	}

	/// <summary>
	/// Put some random beastoz in the room.
	/// </summary>
	/// <param name="sectionIdx">Which section to spawn into</param>
	/// <param name="numBeasties">Number of random monsters to spawn</param>
	/// <param name="championChance">0f-1f odds of championness</param>
	protected void SpawnRandomMonstersInSection(int sectionIdx, int numBeasties, float championChance = 0f)
	{
		for (int t = 0; t < numBeasties; t++)
		{
			var spawnLoc = GetRandomFloorLocationInSection(sectionIdx);
				
			Monster beastie = MonsterManagerScript.CreateMonster(dungeonLevelData.spawnTable.GetRandomActorRef(), true, 
				true, false, 0f, false);
		
			OnEnemyMonsterSpawned(this, beastie, false);
			PlaceActor(beastie, GetTile(spawnLoc));

			// Some monsters are just better.
			if (UnityEngine.Random.value < championChance)
			{
				beastie.MakeChampion();
			}
		}
	}

	/// <summary>
	/// Pulls up a random floor tile in the section.
	/// </summary>
	/// <param name="sectionIdx"></param>
	/// <returns></returns>
	protected Vector2 GetRandomFloorLocationInSection(int sectionIdx)
	{
		var randomLoc = GetLocationForCornerOfSection(sectionIdx);
		randomLoc += Vector2.one;
		randomLoc.x += UnityEngine.Random.Range(0, 6);
		randomLoc.y += UnityEngine.Random.Range(0, 6);

		return randomLoc;
	}
	
	/// <summary>
	/// Connects hole in the ground tiles just like we do for walls.
	/// </summary>
	protected override void AddDecorAndFlowersAndTileDirections()
	{
		//This will shape all our walls, and give them correct indices into 
		//the texture map.
		base.AddDecorAndFlowersAndTileDirections();

        ConvertListOfHoles(convertToHoles, 1100, cornerCorrection:true);		
	}

	/// <summary>
	/// Add each tile in the area to our list of things to convert to holes.
	/// </summary>
	/// <param name="left">Leftmost tile value, lowest X</param>
	/// <param name="bottom">Bottom tile value, lowest Y</param>
	/// <param name="width">width in tiles</param>
	/// <param name="height">height in tiles</param>
	/// <param name="thisTypeOnly">If not COUNT, this parameter will force us to only change tiles of this type.</param>
	protected void ConvertAreaToHoles(int left, int bottom, int width, int height, TileTypes thisTypeOnly = TileTypes.COUNT)
	{
		for (int x = left; x < left + width; x++)
		{
			for (int y = bottom; y < bottom + height; y++)
			{
				var mtd = GetTile(x, y);
				if (mtd != null)
				{
					if (thisTypeOnly == TileTypes.COUNT ||
					    thisTypeOnly == mtd.tileType)
					{
						//change it, we'll change this back before map gen is done
						mtd.ChangeTileType(TileTypes.HOLE);
                        mtd.AddMapTag(MapGenerationTags.HOLE);
						//but we know the truth
						convertToHoles.Add(mtd);
					}
				}
			}
		}
	}

	/// <summary>
	/// Create a 3x3 piece of land, with a 2-deep border of holes around it.
	/// A 1-deep border will unfortunately cause ugly floor artifacts.
	/// </summary>
	/// <param name="center">Center of island</param>
	protected void CreateFloatingIslandInHole(Vector2 center, bool carveHoles = true)
	{
		//make hole
		if (carveHoles)
		{
			ConvertAreaToHoles((int) center.x - 3, (int) center.y - 3, 7, 7);
		}
		
		//make island
		ConvertAreaToTileType((int)center.x-1, (int)center.y -1, 3,3, TileTypes.GROUND);
		
		//add glowy dealie to center
		CreateDestructibleInTile(GetTile(center), "exp1_glowing_cyber_pillar");

		//yay
	}
}

