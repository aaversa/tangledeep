using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;
using Random = UnityEngine.Random;

public class Map_SlimeDungeon : Map 
{
    //const int TURN_INTERVAL_FOR_LIEUTENANTS = 18;
    //const int MAX_LIEUTENANTS = 3;

    List<Actor> killMeAfter;
    Dictionary<Destructible, SlimeStatus> conversionDictionary;

    /// <summary>
    /// Used only for the final boss conflict. These gates are unlocked by boss to unleash adds.
    /// </summary>
    public List<int> bossGateIDs;

	public enum SlimeStatus
	{
		Unslimed = 0,
		Friendly,
		Enemy_1, 
		Enemy_2,
		Enemy_3,
		Count
	}

	/// <summary>
	/// If a tile was slimed or unslimed during a turn, mark it here.
	/// </summary>
	private Dictionary<int, SlimeStatus> tilesChangedThisTurn;

	/// <summary>
	/// If a tile has ever been slimed, it is tracked here.
	/// </summary>
	private Dictionary<int, SlimeStatus> tileSlimeState;

	/// <summary>
	/// Tracked each tower, sorted by slime faction
	/// </summary>
	private Dictionary<SlimeStatus, List<Destructible>> slimeTowersOnMap;

	/// <summary>
	/// After loading from disk, we need to make sure to assign the towers to the dictionary. Not necessary when
	/// first creating the map though. 
	/// </summary>
	private bool bTowersAllTracked;
	
	//private const int kTurnsBetweenSlimeLaunches = 8;
	private const int kSlimeLaunchDistanceMin = 2;
	private const int kSlimeLaunchDistanceMax = 4;

	private Dictionary<SlimeStatus, string> dictSlimeArchetypesByFlavor;
	
	public Map_SlimeDungeon() : base()
	{
		
	}
	
	public Map_SlimeDungeon(int mFloor)
	{
		Initialize(mFloor);
	}

	/// <inheritdoc />
	protected override void Initialize(int mFloor)
	{
		base.Initialize(mFloor);
		tilesChangedThisTurn = new Dictionary<int, SlimeStatus>();
		tileSlimeState = new Dictionary<int, SlimeStatus>();
		slimeTowersOnMap = new Dictionary<SlimeStatus, List<Destructible>>();
		for (SlimeStatus t = 0; t < SlimeStatus.Count; t++)
		{
			slimeTowersOnMap[t] = new List<Destructible>();
		}
		
		dictSlimeArchetypesByFlavor = new Dictionary<SlimeStatus, string>();
		dictSlimeArchetypesByFlavor[SlimeStatus.Unslimed] = "mon_xp_friendly_slime";
		dictSlimeArchetypesByFlavor[SlimeStatus.Friendly] = "mon_xp_friendly_slime";
		dictSlimeArchetypesByFlavor[SlimeStatus.Enemy_1] = "mon_xp_slimedungeon_enemy_slime_1";
		dictSlimeArchetypesByFlavor[SlimeStatus.Enemy_2] = "mon_xp_slimedungeon_enemy_slime_2";
		dictSlimeArchetypesByFlavor[SlimeStatus.Enemy_3] = "mon_xp_slimedungeon_enemy_slime_3";
        killMeAfter = new List<Actor>();
        conversionDictionary = new Dictionary<Destructible, SlimeStatus>();

        bossGateIDs = new List<int>();
	}

	/// <inheritdoc />
	public override bool BuildRandomMap(ItemWorldMetaData itemWorldProperties)
	{
        mapAreaID = MapMasterScript.singletonMMS.mapAreaIDAssigner;
		floor = dungeonLevelData.floor;

		//make it solid
		FillMapWithNothing(TileTypes.WALL);

		//carve out the center
		int mapCenterCoordinate = rows / 2 + 1;

        int slimeMapIndex = floor - MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR + 1;
        var roomTemplate = Room.GetRoomTemplate("slimedungeon_map_0" + slimeMapIndex);
		
		var numRoomRows = roomTemplate.numRows;
		var roomTopLeft = new Vector2(1, mapCenterCoordinate - numRoomRows / 2);

        int yOffset = dungeonLevelData.GetMetaData("yoffset");
        int xOffset = dungeonLevelData.GetMetaData("xoffset");

        roomTopLeft.y += yOffset;
        roomTopLeft.x += xOffset;

        //Debug.LogError(roomTopLeft + " " + yOffset + " " + xOffset + " " + floor + " " + slimeMapIndex + " " + roomTemplate.refName);

        int slimeKeyIndex = 0;

        List<Monster> monstersInHiddenRooms = new List<Monster>();

		for (int rowIdx = 0; rowIdx < numRoomRows; rowIdx++)
		{
			var strRowInfo = roomTemplate.rowData[rowIdx];
			var strLemf = strRowInfo.Length;
			for (int colIdx = 0; colIdx < strLemf; colIdx++)
			{
                int calcXPos = (int)roomTopLeft.x + colIdx;
                int calcYPos = (int)roomTopLeft.y + numRoomRows - rowIdx;
                var checkTile = GetTile(calcXPos, calcYPos);
                //Debug.Log(calcXPos +"," + calcYPos + " " + rowIdx + " " + colIdx + " " + numRoomRows + " " + strLemf + " " + strRowInfo);
                char tileInfo = strRowInfo[colIdx];
				switch (tileInfo)
				{
					case 'X':
						//this tile stays a wall
						break;
                    case 'C':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        CreateDestructibleInTile(checkTile, "obj_largewoodenchest");
                        break;
                    case '5':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        CreateDestructibleInTile(checkTile, "obj_slimemetalgate");                        
                        break;
                    case 'm':
                    case 'n':
                    case 'y':
                    case 'u':
                    case 'p':
                    case 'o':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        Destructible dt = CreateDestructibleInTile(checkTile, "obj_slimemetalgate");
                        dt.SetActorData("slimekey", slimeKeyIndex);
                        bossGateIDs.Add(dt.actorUniqueID);
                        slimeKeyIndex++;
                        break;
                        
                    case '.':
						checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
						break;
					case '1':
						checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        PlaceSlimeTower(checkTile, SlimeStatus.Friendly);
						break;
					case '2':
						checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        PlaceSlimeTower(checkTile, SlimeStatus.Enemy_1);
						break;
					case '9':
						checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        PlaceSlimeTower(checkTile, SlimeStatus.Unslimed);
						break;
                    case 'P':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        heroStartTile = checkTile.pos;
                        break;
                    case 'B':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        Monster m = MonsterManagerScript.CreateMonster("mon_jellydragon", true, true, false, 0f, false);
                        OnEnemyMonsterSpawned(this, m, false);
                        PlaceActor(m, checkTile);
                        break;
                    case 'q':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        Monster reinforce = MonsterManagerScript.CreateMonster("mon_xp_slimedungeon_enemy_slime_1", false, false, false, 0f, false);
                        reinforce.ChangeMyFaction(Faction.ENEMY);
                        PlaceActor(reinforce, checkTile);
                        monstersInHiddenRooms.Add(reinforce);
                        OnEnemyMonsterSpawned(this, reinforce, false);
                        reinforce.AddAttribute(MonsterAttributes.STARTASLEEP, 100);
                        break;
                    case 'r':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        Monster lieut = MonsterManagerScript.CreateMonster(dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
                        if (!lieut.isChampion)
                        {
                            lieut.MakeChampion(true);
                        }
                        lieut.ChangeMyFaction(Faction.ENEMY);
                        lieut.AddAttribute(MonsterAttributes.STARTASLEEP, 100);
                        OnEnemyMonsterSpawned(this, lieut, false);
                        PlaceActor(lieut, checkTile);
                        monstersInHiddenRooms.Add(lieut);
                        break;
                    case 'S':
                        checkTile.ChangeTileType(TileTypes.GROUND);
                        mapRooms[0].internalTiles.Add(checkTile);
                        int ptFloor = 0;
                        switch(floor)
                        {
                            case MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR:
                                ptFloor = MapMasterScript.JELLY_GROTTO;
                                break;
                            default:
                                ptFloor = floor - 1; // go to previous
                                break;
                        }
                        Stairs st = SpawnStairsAtLocation(true, checkTile, ptFloor);
						break;
				}
			}
		}

        // for the boss map, each monster should be tied to a gate id. When that gate awakes, we should tell the linked monsters to wake TF up
        Dictionary<Destructible, List<int>> monsterIDsPerGate = new Dictionary<Destructible, List<int>>();
        foreach(Monster m in monstersInHiddenRooms)
        {
            Destructible dtShortest = null;
            int shortestDistance = 999;
            foreach(int id in bossGateIDs)
            {
                Destructible dt = GameMasterScript.dictAllActors[id] as Destructible;
                int dist = MapMasterScript.GetGridDistance(m.GetPos(), dt.GetPos());
                if (dist < shortestDistance)
                {
                    dtShortest = dt;
                    shortestDistance = dist;
                }
            }
            if (!monsterIDsPerGate.ContainsKey(dtShortest))
            {
                monsterIDsPerGate.Add(dtShortest, new List<int>());
            }
            monsterIDsPerGate[dtShortest].Add(m.actorUniqueID);
        }

        StringBuilder sb = new StringBuilder();

        foreach(var kvp in monsterIDsPerGate)
        {
            sb.Length = 0;
            foreach(int id in kvp.Value)
            {
                if (sb.Length != 0) sb.Append(',');
                sb.Append(id);
            }
            kvp.Key.SetActorDataString("linkedmonsters", sb.ToString());
        }
		
		// === Post build clean up section =============================================================================
		
		//various map cleanups
		SetMapBoundVisuals();
		
		//Make sure we have ground in the right places so we can spawn bads and build
		//secret rooms
		PrepareGroundTiles();
		
		//seed with monsters
		//PlaceEncountersInSections();
		
		//secret areas?
		
		//decor and layered stuff
		AddGrassLayer1ToMap();
		//AddWallChunkDecor(); No wall chunks in Future tileset for this map
		AddGrassLayer2ToMapAndBeautifyAll(); 
		
		//This will make our holes become reality. 																																																																																//our dreams of holes WILL BECOME WHOLE
		AddDecorAndFlowersAndTileDirections();

        //This converts tiles we tagged as lava, mud, elec etc to the actual tile
        AddEnvironmentDestructiblesToMap();

        int numFountains = dungeonLevelData.GetMetaData("numfountains");
        for (int i = 0; i < numFountains; i++)
        {
            SpawnFountainInMap();
        }

        //done?
        bTowersAllTracked = true;
        return true;

	}

	/// <summary>
	/// Make sure all slime tower positions are recorded.
	/// </summary>
	void AddTowersToMasterTowerList()
	{
		foreach (var maybeTower in actorsInMap)
		{
			if (maybeTower.actorRefName == "exp2_slime_tower")
			{
				var status = GetSlimeStatusFromActorData(maybeTower);
				var towerList = slimeTowersOnMap[status];
				var dtTower = maybeTower as Destructible;
				if (!towerList.Contains(dtTower))
				{
					towerList.Add(dtTower);
				}
			}
		}
	}
	
	/// <summary>
	/// Called by Unity via MapMasterScript every frame
	/// </summary>
	public override void TickFrame()
	{
		base.TickFrame();

		if (!bTowersAllTracked)
		{
			AddTowersToMasterTowerList();
			bTowersAllTracked = true;
		}
		
		//mess with shaders on all slimes
		foreach (var kvp in tileSlimeState)
		{
			//if not slimy, ignore
			if (kvp.Value == SlimeStatus.Unslimed)
			{
				continue;
			}
			
			MapTileData mtd = IntToMTD(kvp.Key);
			var mudActor = mtd.GetActorRef("obj_mudtile");
			if (mudActor == null)
			{
				continue;
			}
			
			var slimeMat = mudActor.mySpriteRenderer.material;
			slimeMat.SetFloat("_PulseTime", Time.realtimeSinceStartup);
		}
		
		
		
	}

	public override void WriteSpecialMapDataToSave(XmlWriter writer)
	{
		writer.WriteStartElement("specialmapdata");
		writer.WriteElementString("floor", floor.ToString());
		//save all the tiles that have been slimed
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<int, SlimeStatus> kvp in tileSlimeState)
		{
			sb.Append(kvp.Key.ToString());
			sb.Append(",");
			sb.Append(((int) kvp.Value).ToString());
			sb.Append(",");
		}
		writer.WriteElementString("trackedmaptiles", sb.ToString());

        if (bossGateIDs.Count > 0)
        {
            sb.Length = 0;
            foreach(int id in bossGateIDs)
            {
                if (sb.Length != 0)
                {
                    sb.Append(',');
                }
                sb.Append(id);
            }
            writer.WriteElementString("gateids", sb.ToString());
        }

		writer.WriteEndElement(); //"specialmapdata"
	}

	public override void ReadSpecialMapDataFromSave(XmlReader reader)
	{
		while (reader.NodeType != XmlNodeType.EndElement)
		{
			string strValue = reader.Name;
			switch (strValue)
			{
				//see the function above for how this info is stored. csv of tile int and
				//slime value.
				case "trackedmaptiles":
					
					var splitsies = reader.ReadElementContentAsString().Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);

					for (int t = 0; t < splitsies.Length; t+=2)
					{
						tileSlimeState[Int32.Parse(splitsies[t])] = 
							(SlimeStatus)Int32.Parse(splitsies[t + 1]);
					}
					break;
                case "gateids":
                    splitsies = reader.ReadElementContentAsString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int t = 0; t < splitsies.Length; t++)
                    {
                        int id = 0;
                        if (Int32.TryParse(splitsies[t], out id))
                        {
                            bossGateIDs.Add(id);
                        }
                    }
                    break;
				default:
					reader.Read();
					break;
			}
		}

	}	

	/// <summary>
	/// Called after the player has taken an action.
	/// </summary>
	public void OnEndOfTurn()
	{        
        var hero = GameMasterScript.heroPCActor;

        if (hero.ReadActorData("slimevic" + MapMasterScript.activeMap.floor) == 1) return; // already marked as won

        var heroPos = hero.GetPos();
		var checkTile = GetTile(heroPos);
		
		//If there are no friendly towers left, it's gg. But also if there are no enemy towers left!
		if (slimeTowersOnMap[SlimeStatus.Friendly].Count <= 0)
		{
            //player loses, do thing here:
            GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.FailedCurrentMap());
			return;
		}

		if (slimeTowersOnMap[SlimeStatus.Enemy_1].Count <= 0 &&
		    slimeTowersOnMap[SlimeStatus.Enemy_2].Count <= 0 &&
		    slimeTowersOnMap[SlimeStatus.Enemy_3].Count <= 0)
		{
            //player wins, do thing here:            
            GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.WonCurrentMap());
            return;
		}

        int turnsToSpawnLieutenant = GameMasterScript.heroPCActor.ReadActorData("enemylieutenant_turns");
        int numLieutenants = GameMasterScript.heroPCActor.ReadActorData("num_lieutenants");

        if (numLieutenants <= 0)
        {
            numLieutenants = 0;
            turnsToSpawnLieutenant--;
            // more lieutenants if there are fewer monsters on map
        }

        if (numLieutenants <= 1)
        {
            turnsToSpawnLieutenant--;
        }

        turnsToSpawnLieutenant--;

        if (turnsToSpawnLieutenant <= 0 && ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) < 3)
        {
            int turnInterval = dungeonLevelData.GetMetaData("lieutenantinterval");
            int maxLieutenants = dungeonLevelData.GetMetaData("maxlieutenants");
            turnsToSpawnLieutenant = turnInterval;
            if (numLieutenants < maxLieutenants)
            {                
                var enemyTower = GetTowerLocations(SlimeStatus.Enemy_1);

                Vector2 spawnPos = Vector2.zero;

                if (enemyTower.Length > 0)
                {
                     spawnPos = enemyTower[0];
                }
                else
                {
                    spawnPos = GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 5, false, true, false).pos;
                }                

                MapTileData placement = MapMasterScript.GetRandomEmptyTile(spawnPos, 1, true, false, false, false);

                Monster mLieutenant = SpawnRandomMonster(true, true, "", true, placement);
                if (!mLieutenant.isChampion)
                {
                    mLieutenant.MakeChampion(true);
                }
                numLieutenants++;
                GameMasterScript.heroPCActor.SetActorData("num_lieutenants", numLieutenants);
                mLieutenant.AddAggro(GameMasterScript.heroPCActor, 200f);
                mLieutenant.displayName = StringManager.GetString("xp2_slime_lieutenant_name");
                mLieutenant.ChangeMyFaction(Faction.ENEMY);
                CombatManagerScript.GenerateSpecificEffectAnimation(mLieutenant.GetPos(), "LightningStrikeEffectBig", null, true);
                //Debug.Log("THere are now " + numLieutenants + " lieutenants");
            }
            
        }

        GameMasterScript.heroPCActor.SetActorData("enemylieutenant_turns", turnsToSpawnLieutenant);


        //unslime the tile the player is standing on
        SetSlimeStateForEndOfTurn(checkTile, SlimeStatus.Unslimed);
		
		//iterate through all currently slimed tiles and look for a spread.
		foreach (var kvp in tileSlimeState)
		{
			var mySlimeState = kvp.Value;
			if (mySlimeState != SlimeStatus.Unslimed)
			{
				//this tile has a slime, check its neighbors for emptyness and perhaps spread.
				var spreadCenter = IntToMTD(kvp.Key);

				for (Directions d = Directions.NORTH; d < Directions.RANDOMALL; d += 2)
				{
					//if there is no tile, or it's not ground, don't worry.
					var possibleBro = GetTileInDirection(spreadCenter.pos, d);
					if (possibleBro == null || possibleBro.tileType != TileTypes.GROUND)
					{
						continue;
					}

                    if (possibleBro.specialMapObjectsInTile[(int)SpecialMapObject.METALGATE])
                    {
                        continue;
                    }
					
					//if the tile is already slimed, don't worry
					if (GetSlimeState(possibleBro) != SlimeStatus.Unslimed)
					{
						continue;
					}
					
					//maybe slime it up
					if (Random.value < 0.04f)
					{
						SetSlimeStateForEndOfTurn(possibleBro, mySlimeState);
					}
				}
			}
		}
		
		//look to see if any of our combat slimes have touched enemy territory
		CombatSlimesEndOfTurn();
		
		//make sure we update the destructibles in anything that may have changed this turn.
		foreach (var kvp in tilesChangedThisTurn)
		{
			var changeTile = IntToMTD(kvp.Key);
			var newSlimeState = kvp.Value;
			var dt = changeTile.GetActorRef("obj_mudtile") as Destructible;

			if (newSlimeState == SlimeStatus.Unslimed)
			{
				//remove the destructible
				if (dt != null)
				{
					dt.mySpriteRenderer.sprite = null;
					dt.RemoveSelfFromMap();
				}
			}
			else
			{
				//place a new destructible if there isn't one
				
				if (dt == null)
				{
					dt = CreateDestructibleInTile(changeTile, "obj_mudtile");
					
					SetStartingInformationOnNewSlimeTile(dt, changeTile);
				}

				//update the slime pulse origin
				var towerLocs = GetTowerLocations(newSlimeState);
				
				//dt.mySpriteRenderer.material.SetVector("_PulseOrigin1", towerLocs[0] );
				//dt.mySpriteRenderer.material.SetVector("_PulseOrigin2",towerLocs[1] );
				
				dt.mySpriteRenderer.material.SetVectorArray("_OriginArray",towerLocs );
			}
		}

		//clean up any water, sparks, or actual mud that may be in the level,
		//BeautifyAllTerrain();
		
		//since BeautifyAllTerrain uses .mud to determine shape, it may be incorrect. Two mud tiles can be adjacent,
		//but they may have different slime flavors. So we need to do special math based on that.
		
		//First, copy the changedThisTurn cache over to the real final state,
		foreach (var kvp in tilesChangedThisTurn)
		{
			tileSlimeState[kvp.Key] = kvp.Value;
		}

		//then use that final state to determine what destructibles should look like,
		BeautifySlimeTerrain();

		//finally adjust the destructibles to look correct.
		foreach (var kvp in tilesChangedThisTurn)
		{
			AdjustSpriteInAllTilesAroundThisOne(IntToMTD(kvp.Key));
		}
		
		tilesChangedThisTurn.Clear();
		
		//now that the ground slime has changed this turn, check towers for control changes or new spawns.
		UpdateAllSlimeTowers();

	}

	private void SetStartingInformationOnNewSlimeTile(Destructible dt, MapTileData changeTile)
	{
		// We do not want these mud tiles to actually root the player.
		dt.dtStatusEffect = null;
						
		changeTile.AddTag(LocationTags.MUD);
        if (dt.GetObject() == null || !dt.GetObject().activeSelf)
        {
            MapMasterScript.singletonMMS.SpawnDestructible(dt);
        }
		
		Material mat = Material.Instantiate(GameMasterScript.spriteMaterialFloorSlime);

		mat.SetVector("_TileLocation",
			new Vector4(changeTile.pos.x, changeTile.pos.y, 0, 0)); //.pos.magnitude);
		// magic numbers based on the width/height of river_lava_mud.png, which we don't have access to at this time.
		mat.SetFloat("_TileSizeX", 32.0f / 640.0f); //dt.mySpriteRenderer.sprite.texture.width));
		mat.SetFloat("_TileSizeY", 32.0f / 1760.0f); //dt.mySpriteRenderer.sprite.texture.height);
		mat.SetFloat("_PulseMod", 4.0f);

		dt.mySpriteRenderer.material = mat;
	}

	/// <summary>
	/// Get the location of the very first tower of a given slime state
	/// </summary>
	/// <param name="towerSlimeStatus"></param>
	/// <returns></returns>
	private Vector4[] GetTowerLocations(SlimeStatus towerSlimeStatus)
	{
		var retList = new Vector4[8];

		if (slimeTowersOnMap[towerSlimeStatus].Count == 0)
		{
			retList[0] = Vector4.zero;
		}
		else
		{
			for(int t = 0; t < slimeTowersOnMap[towerSlimeStatus].Count; t++)
			{
				var tower = slimeTowersOnMap[towerSlimeStatus][t];
				retList[t] = new Vector4(tower.GetPos().x + 0.5f, tower.GetPos().y + 0.5f, 0,0);
			}
		}

		return retList;
	}

	/// <summary>
	/// Looks through all the tiles of towers that don't belong to us, and finds the closest one.
	/// </summary>
	/// <param name="originTile"></param>
	/// <param name="myFaction"></param>
	/// <returns></returns>
	public MapTileData GetClosestTowerOfOpposingFaction(MapTileData originTile, SlimeStatus myFaction)
	{
		float bestDistance = 99f * 99f;
		var myPos = originTile.pos;
		var goalTile = GetTile(myPos);
		
		foreach (var kvp in slimeTowersOnMap)
		{
			//don't check our own towers!
			if (kvp.Key == myFaction)
			{
				continue;
			}

			//if this tower is the closest, keep track of it.
			var towerList = kvp.Value;
			foreach (var tower in towerList)
			{
				var destPos = tower.GetPos();
				var delta = (myPos - destPos).SqrMagnitude();
				if (delta < bestDistance)
				{
					bestDistance = delta;
					goalTile = GetTile(destPos);
				}
			}
		}

		//this is either where we are, or where we want to go.
		return goalTile;
	}

	/// <summary>
	/// Call after BeautifyAllTerrain, this does special hand-massaged adjustments for the special slime tiles.
	/// </summary>
	void BeautifySlimeTerrain()
	{
		for (int x = 1; x < columns - 1; x++)
		{
			for (int y = 1; y < rows - 1; y++)
			{
				int idx = MTDToInt(mapArray[x,y]);

                SlimeStatus ss = SlimeStatus.Unslimed;

                if (tileSlimeState.TryGetValue(idx, out ss))
                {
                    if (ss != SlimeStatus.Unslimed)
                    {
                        SetSlimeVisualTileTypeBasedOnNeighbors(mapArray[x,y]);
                    }
                }

				/* if (tileSlimeState.ContainsKey(idx) &&
				    tileSlimeState[idx] != SlimeStatus.Unslimed)
				{
					SetSlimeVisualTileTypeBasedOnNeighbors(mtd);
				} */
				
			}
		}

	}

	void SetSlimeVisualTileTypeBasedOnNeighbors(MapTileData slimeTile)
	{
		//what kind of slime am I?
		int myIdx = MTDToInt(slimeTile);

        SlimeStatus mySlime = SlimeStatus.Unslimed;

		//if I am not slimy, gtfo
		if (!tileSlimeState.TryGetValue(myIdx, out mySlime))
		{
			return;
		}
						
		//set our texture based on who is near us.
		var newVisualIdx = 0;
		int bitValue = 0;
		for (int buddyDir = 0; buddyDir < MapMasterScript.directions.Length; buddyDir++)
		{
            //if our neighbors are the same slime flavor as us, the index goes up
            Vector2 vTilePos = slimeTile.pos + MapMasterScript.directions[buddyDir];
            var buddyTile = mapArray[(int)vTilePos.x, (int)vTilePos.y];
			if (buddyTile != null )
			{
				int buddyIdx = MTDToInt(buddyTile);

                SlimeStatus buddyState = SlimeStatus.Unslimed;

				if (!tileSlimeState.TryGetValue(buddyIdx, out buddyState))
				{
					continue;
				}
				
				//if they're slimy like I'm slimy, know this.
				if (buddyState == mySlime)
				{
					bitValue += 1 << buddyDir;
				}
			}
		}

		//what we look like now is determined by our neighbors.
		slimeTile.SetTerrainTileType(visualTileTypesByBitAddition[bitValue]);
	}

	public void PlaceSlimeTower(MapTileData mtd, SlimeStatus towerSlimeStatus)
	{
		//place it down
		//todo: check for existing tower
		var tower = CreateDestructibleInTile(mtd, "exp2_slime_tower");
		
		tower.SetActorData("slimestatus", (int)towerSlimeStatus);

		//slime the tile it is under
		SetSlimeStateForEndOfTurn(mtd, towerSlimeStatus);
		
		//keep track of this tower
		slimeTowersOnMap[towerSlimeStatus].Add(tower);

		var go = tower.GetObject();
		if (go)
		{
			var controller = go.GetComponent<SlimeTowerController>();
			controller.SetTower(tower);
		}

	}

	/// <summary>
	/// Change the sprite on the destructible and its neighbors to match the shape of the slime.
	/// </summary>
	/// <param name="centerMTD"></param>
	void AdjustSpriteInAllTilesAroundThisOne(MapTileData centerMTD)
	{
		int xPos = (int)centerMTD.pos.x;
		int yPos = (int)centerMTD.pos.y;
		
		//look at all the tiles nearby, and repaint the destructibles sprite
		for(int x = -1; x <= 1; x++)
		for (int y = -1; y <= 1; y++)
		{
            var broTile = mapArray[xPos + x, yPos + y];
			//var broTile = GetTile(xPos + x, yPos + y);
			if (broTile == null)
			{
				continue;
			}

			var mudActor = broTile.GetActorRef("obj_mudtile");
			if (mudActor == null)
			{
				continue;
			}
                    
			GameObject mudObj = mudActor.GetObject();            
            Animatable localAnim = mudActor.myAnimatable;

			int changedTileIdx = MTDToInt(broTile);

			for (int i = 0; i < localAnim.myAnimations[0].mySprites.Count; i++)
			{
				zirconAnim.AnimationFrameData afd = localAnim.myAnimations[0].mySprites[i];
				afd.mySprite = MapMasterScript.terrainAtlas[broTile.indexOfTerrainSpriteInAtlas];
			}

			if( tileSlimeState.ContainsKey(changedTileIdx))
			{
                SpriteRenderer sr = mudActor.mySpriteRenderer;
				AssignColorToMudTileShader(sr, tileSlimeState[changedTileIdx]);
			}
		}
	}

	void AssignColorToMudTileShader(SpriteRenderer mudRenderer, SlimeStatus newState)
	{
		switch (newState)
		{
			case SlimeStatus.Unslimed:
				break;
			case SlimeStatus.Friendly:
                mudRenderer.material.color = Color.yellow;
                break;
			case SlimeStatus.Enemy_1:
                mudRenderer.material.color = new Color(135f / 255f, 23f / 255f, 146f / 255f);                
                break;
			case SlimeStatus.Enemy_2:
				break;
			case SlimeStatus.Enemy_3:
				break;
			case SlimeStatus.Count:
				break;
		}
	}
	

	int MTDToInt(MapTileData mtd)
	{
		return (int)(mtd.pos.x + mtd.pos.y * rows);
	}

	MapTileData IntToMTD(int i)
	{
		return GetTile((i % rows), (i / rows));
	}
	
	/// <summary>
	/// Slime or unslime a tile. The destructible inside will be modified at the end of the turn.
	/// </summary>
	/// <param name="mtd"></param>
	/// <param name="newSlimeState"></param>
	public void SetSlimeStateForEndOfTurn(MapTileData mtd, SlimeStatus newSlimeState)
	{
		int id = MTDToInt(mtd);
		
		//If we didn't change anything, don't mark it as changed.
		if (tileSlimeState.ContainsKey(id) && tileSlimeState[id] == newSlimeState)
		{
			return;
		}

        if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.METALGATE])
        {
            return;
        }

		tilesChangedThisTurn[id] = newSlimeState;
	}

	public SlimeStatus GetSlimeState(MapTileData mtd)
	{
		int id = MTDToInt(mtd);

        SlimeStatus ss = SlimeStatus.Unslimed;

        if (tileSlimeState.TryGetValue(id, out ss))
        {
            return ss;
        }

		return SlimeStatus.Unslimed;

	}

	
	/// <summary>
	/// Look at each combat slime in the map, if it is on a tile that is slimey, but not its own slimey, it should explode.
	/// </summary>
	void CombatSlimesEndOfTurn()
	{
        killMeAfter.Clear();

        string victory = "slimevic" + floor;

        if (GameMasterScript.heroPCActor.ReadActorData(victory) == 1)
        {
            return;
        }

		foreach (var a in actorsInMap)
		{
			if (a.HasActorData("slimeareaondeath"))
			{
				int ssIndex = a.ReadActorData("slimestatus");
				if (ssIndex > 0)
				{
					var mySlimeStatus = (SlimeStatus)ssIndex;
					if (mySlimeStatus != SlimeStatus.Unslimed)
					{
						//look at the ground beneath it
						var myTile = GetTile(a.GetPos());
						var groundState = GetSlimeState( myTile);
						
						//if we are touching a different slime, explode.
						if (groundState != SlimeStatus.Unslimed &&
						    groundState != mySlimeStatus)
						{
							killMeAfter.Add(a);
							SlimeAreaAroundTile(myTile, 1, mySlimeStatus);
							CombatManagerScript.GenerateSpecificEffectAnimation(myTile.pos, "GreenSmokePoof", null, true);
						}
						
					}
				}
			}
		}

		foreach (var a in killMeAfter)
		{
			a.RemoveSelfFromMap();
		}
	}

	/// <summary>
	/// Slime an area around a given tile.
	/// </summary>
	/// <param name="centerTile">The tile to start the splash at</param>
	/// <param name="radius">Distance from center to slime. 1 == 3x3 area, 2 == 5x5 area, etc.</param>
	/// <param name="convertToThisSlime">The flavor to convert to. Can also be unslimed!</param>
	/// <param name="requiresLOS">If true, tiles won't change slime if they can't be seen from the center.</param>
	/// <param name="changeUnderTowersOK">Normally this function won't change the slime style under a tower</param>
	public void SlimeAreaAroundTile(MapTileData centerTile, int radius, SlimeStatus convertToThisSlime, 
		bool requiresLOS = false, bool changeUnderTowersOK = false, bool destroyEnemySlimes = false)
	{
		var centerLoc = centerTile.pos;
		for(int x = (int)centerLoc.x - radius; x <= centerLoc.x + radius; x++)
			for (int y = (int) centerLoc.y - radius; y <= centerLoc.y + radius; y++)
			{
				var mtd = GetTile(x, y);
				if (mtd == null || mtd.BlocksLineOfEffect())
				{
					continue;
				}
				
				//if the tile contains a tower, do not convert the tower
				if (!changeUnderTowersOK && GetTowerInTile(mtd) != null)
				{
					continue;
				}
				
				//if the tile requires LOS, check for that.
				if( requiresLOS && 
				    !MapMasterScript.CheckTileToTileLOS(centerLoc, mtd.pos, null, this))
				{
					continue;
				}

                if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.METALGATE])
                {
                    continue;
                }

                //success
                SetSlimeStateForEndOfTurn(mtd, convertToThisSlime);

				if (destroyEnemySlimes)
				{
					//remove slimes in here who aren't my flavor.
					var enemyFightar = GetSlimeFighterFromTile(mtd);
					if (enemyFightar != null)
					{
						var otherSlimeStatus = GetSlimeStatusFromActorData(enemyFightar);
						if (otherSlimeStatus != convertToThisSlime)
						{
							enemyFightar.TakeDamage(enemyFightar.myStats.GetMaxStat(StatTypes.HEALTH) * 1.5f,
								DamageTypes.PHYSICAL);
						}
					}
				}
			}
	}

	/// <summary>
	/// Returns a slime tower in a given tile, if there is one.
	/// </summary>
	/// <param name="mtd"></param>
	/// <returns></returns>
	public Destructible GetTowerInTile(MapTileData mtd)
	{
		return mtd.GetActorRef("exp2_slime_tower") as Destructible;
	}

	/// <summary>
	/// Check towers for color flipping if surrounded, and also check to see if they are going to spawn anything new.
	/// </summary>
	private void UpdateAllSlimeTowers()
	{
        //do conversions first, check every tower's base to see what's up
        conversionDictionary.Clear();
		
		foreach (var kvp in slimeTowersOnMap)
		{
			foreach (var tower in kvp.Value)
			{
				var controller = tower.GetObject().GetComponent<SlimeTowerController>();
				SlimeStatus changeStatus = SlimeStatus.Count;
				if (controller.ShouldConvert(ref changeStatus))
				{
					conversionDictionary[tower] = changeStatus;
				}
			}
		}
		
		//for every key-value pair in the change dictionary, make the move
		bool bHaveStartedCutsceneThisFrame = false;

        int turnsBetweenFriendlyLaunches = dungeonLevelData.GetMetaData("friendlyslimeturns");
        int turnsBetweenEnemyLaunches = dungeonLevelData.GetMetaData("enemyslimeturns");

        foreach (var kvp in conversionDictionary)
		{
			var tower = kvp.Key;
			var newSlimeStatus = kvp.Value;
			var oldSlimeStatus = GetSlimeStatusFromActorData(tower);
			slimeTowersOnMap[oldSlimeStatus].Remove(tower);
			slimeTowersOnMap[newSlimeStatus].Add(tower);
			tower.SetActorData("slimestatus", (int)newSlimeStatus);
            if (newSlimeStatus == SlimeStatus.Friendly)
            {
                tower.SetActorData("slime_spawn_turns_remaining", turnsBetweenFriendlyLaunches);
            }
            else
            {
                tower.SetActorData("slime_spawn_turns_remaining", turnsBetweenEnemyLaunches);
            }
			

			//song and dance
			if (!bHaveStartedCutsceneThisFrame)
			{
				bHaveStartedCutsceneThisFrame = true;
				GameMasterScript.StartWatchedCoroutine(DLCCutscenes.SlimeTowerConversion(tower, newSlimeStatus));
			}
			else
			{
				SlimeAreaAroundTile(GetTile(tower.GetPos()), 2, newSlimeStatus, true, true);
				var controller = tower.GetObject().GetComponent<SlimeTowerController>();
				controller.DoConversion(newSlimeStatus);
			}

		}
		
		//did we actually convert anything? If so, update every material with new tower locations.
		if (conversionDictionary.Count > 0)
		{
			ResetShaderDataOnAllTiles();
		}


		//Spawn from the towers that are taken by a color
		foreach (var kvp in slimeTowersOnMap)
		{
			if (kvp.Key == SlimeStatus.Unslimed || kvp.Key == SlimeStatus.Count)
			{
				continue;
			}
			
			foreach (var tower in kvp.Value)
			{
				int turnsRemaining = tower.ReadActorData("slime_spawn_turns_remaining");
				if (turnsRemaining <= 0)
				{
					turnsRemaining = turnsBetweenFriendlyLaunches;
					SpawnSlimeFromTower(tower);
                    if (kvp.Key != SlimeStatus.Friendly)
                    {
                        turnsRemaining = turnsBetweenEnemyLaunches;
                    }
				}
				else
				{
					turnsRemaining--;
				}
				
				tower.SetActorData("slime_spawn_turns_remaining", turnsRemaining);
			}
		}
	}

	/// <summary>
	/// Not the priceyest function in the world but don't call it every frame. Use this
	/// when a tower converts, or at map load, to make sure all slime looks correct.
	/// </summary>
	private void ResetShaderDataOnAllTiles()
	{
		foreach (var slimedTile in tileSlimeState.Keys)
		{
			MapTileData checkTile = IntToMTD(slimedTile);
			var dt = checkTile.GetActorRef("obj_mudtile");
			if (dt != null)
			{
				var towerLocs = GetTowerLocations(tileSlimeState[slimedTile]);
				dt.mySpriteRenderer.material.SetVectorArray("_OriginArray",towerLocs );
				AssignColorToMudTileShader(dt.mySpriteRenderer, tileSlimeState[slimedTile]);
			}

		}
	}

	/// <summary>
	/// Pop a slime out from this tower
	/// </summary>
	/// <param name="dtTower"></param>
	private void SpawnSlimeFromTower(Destructible dtTower)
	{
		//What type of slime?
		var archetypeName = dictSlimeArchetypesByFlavor[GetSlimeStatusFromActorData(dtTower)];
		
		//Create it
		Monster newSlime = MonsterManagerScript.CreateMonster(archetypeName, false,
			false, true, 0f, false);
		
		//Figure out where to put it
		var spawnTile = GetRandomEmptyTile(dtTower.GetPos(),
			Random.Range(kSlimeLaunchDistanceMin, kSlimeLaunchDistanceMax + 1), false,
			true);
		
		PlaceActor(newSlime, spawnTile);
		
		//Now that we've created it, and put it somewhere, spawn it. ¯\_(ツ)_/¯
		MapMasterScript.singletonMMS.SpawnMonster(newSlime, true);
		
		//And also, make sure we know we just spawned it.
		OnEnemyMonsterSpawned(this, newSlime, false);
	}

	/// <summary>
	/// Reads the actor data and returns a slime status. Covers the -1 condition.
	/// </summary>
	/// <param name="readMe"></param>
	/// <returns></returns>
	public static SlimeStatus GetSlimeStatusFromActorData(Actor readMe)
	{
		int ssIndex = readMe.ReadActorData("slimestatus");
		if (ssIndex < 0)
		{
			//this tower has no slimellegiance? 
			return SlimeStatus.Unslimed;
		}

		return (SlimeStatus)ssIndex;
	}

	/// <summary>
	/// Called by MonsterBehaviorScript, this is what we would like our combat slimes to do every turn.
	/// Slimes will hunt down towers of enemy factions and move towards them.
	/// </summary>
	/// <param name="actor"></param>
	/// <returns></returns>
	public MonsterTurnData SlimeDragonSlimeTakeActionScript(Monster actor)
	{
		var myPos = actor.GetPos();
        
		//how slimy am I?
		int ssIndex = actor.ReadActorData("slimestatus");
		if (ssIndex < 0)
		{
			//i'm not slimy at all?
			return actor.myMonsterTurnData.Pass();
		}

		var mySlimeStatus = (SlimeStatus)ssIndex;
		MapTileData desiredTile;
		
		//Action choice #1: If I am near an enemy slime, attack it.
		for (Directions dir = Directions.NORTH; dir < Directions.RANDOMALL; dir++)
		{
			var checkMTD = GetTileInDirection(myPos, dir);
			if (checkMTD == null || checkMTD.IsEmpty())
			{
				continue;
			}

			//We know that this function will return only fighters with active slime statuses. 
			//Not count or unslimed.
			var enemyFightar = GetSlimeFighterFromTile(checkMTD);
			if (enemyFightar != null)
			{
				var otherSlimeStatus = GetSlimeStatusFromActorData(enemyFightar);
				if (otherSlimeStatus != mySlimeStatus)
				{
					//crush this foe!
					//we're gonna use a fake attack, because we don't want to have an actual
					//attack that takes realtime and puts stuff in the log.

					var angleToFoe = CombatManagerScript.GetAngleBetweenPoints(myPos, enemyFightar.GetPos());
					var foeDirection = MapMasterScript.GetDirectionFromAngle(angleToFoe);

					//looks like an attack!
					actor.myMovable.Jab(foeDirection);

					//enemy loses HP
					enemyFightar.TakeDamage(enemyFightar.myStats.GetMaxStat(StatTypes.HEALTH) *
					                        Random.Range(0.18f, 0.22f) + 20f, DamageTypes.PHYSICAL);

					//we're cool and strong
					return actor.myMonsterTurnData.Pass();
				}
			}
		}
		

		//Action choice #2: if we are within five tiles of a tower that needs our help, go help it. If not, then
		Destructible healmeTower = GetClosestTowerWithinRangeThatNeedsHelpFrom(GetTile(myPos), mySlimeStatus, 5);
		if (healmeTower == null)
		{
			//Action choice #3: Go find an enemy tower and convert it.
			desiredTile = GetClosestTowerOfOpposingFaction(GetTile(myPos), mySlimeStatus);
		}
		else
		{
			desiredTile = GetTile(healmeTower.GetPos());
		}
		
		//if there isn't one, continue.
		if (desiredTile == null)
		{
			return actor.myMonsterTurnData.Continue();
		}
		
		//Am I destined to move to my own tile? 
		if (desiredTile.pos == myPos)
		{
			//bounce in the air, and end my turn.
			actor.myMovable.Jab(Directions.NORTH);
			return actor.myMonsterTurnData.Pass();
		}
		

		//which way is that?
		var angleFromStartToEnd = CombatManagerScript.GetAngleBetweenPoints(myPos, desiredTile.pos); 
		var towerGeneralDirection = MapMasterScript.GetDirectionFromAngle(angleFromStartToEnd);

		//am I RIGHT next to it?
		if ((desiredTile.pos - myPos).SqrMagnitude() <= Vector2.one.SqrMagnitude())
		{
			//do something to help convert the tower.
			actor.myMovable.Jab(towerGeneralDirection);

			//play this attack anim, which is a bunch of crossed swords flashing on the flag
			actor.myAnimatable.SetAnim("Attack");

			var tower = GetTowerInTile(desiredTile);
			if (tower != null)
			{
				var towerController = tower.GetObject().GetComponent<SlimeTowerController>();
				towerController.TakeDamage(mySlimeStatus);
			}
            
			//don't actually take a turn.
			return actor.myMonsterTurnData.Pass();
		}
        
        
		//Find the best tile to step into to approach the goal
		var nextTile = actor.BuildPath(myPos, desiredTile.pos, true);

		//Is there no next tile? I don't know why.
		if (nextTile == null)
		{
			//rip.
			nextTile = GetTileInDirection(myPos, towerGeneralDirection);
		}

		//Commit the move
		actor.MoveSelf(nextTile.pos, true);
		return actor.myMonsterTurnData.Move();

	}

	/// <summary>
	/// Checks a tile for a Fighter that has a slime allegiance. Unslimed actors do not count.
	/// </summary>
	/// <param name="checkMTD"></param>
	/// <returns></returns>
	private Fighter GetSlimeFighterFromTile(MapTileData checkMTD)
	{
		foreach (var checkActor in checkMTD.GetAllActors())
		{
			if (checkActor is Fighter )
			{
				var otherSlimeStatus = GetSlimeStatusFromActorData(checkActor);
				if (otherSlimeStatus != SlimeStatus.Count &&
				    otherSlimeStatus != SlimeStatus.Unslimed )
				{
					return checkActor as Fighter;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Looks around for a tower that needs healing.
	/// </summary>
	/// <param name="originPoint"></param>
	/// <param name="healingStatus"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	private Destructible GetClosestTowerWithinRangeThatNeedsHelpFrom(MapTileData originPoint, SlimeStatus healingStatus,
		int range)
	{
		Destructible retTower = null;
		var bestDistance = range;
		foreach (var tower in slimeTowersOnMap[healingStatus])
		{
			var dist = MapMasterScript.GetGridDistance(tower.GetPos(), originPoint.pos);
			if (dist <= bestDistance)
			{
				var go = tower.GetObject();
				if (go != null)
				{
					var controller = go.GetComponent<SlimeTowerController>();
					if (controller.DoINeedRepairFrom(healingStatus))
					{
						retTower = tower;
						bestDistance = dist;
					}
				}
			}
		}
		return retTower;
	}

	/// <summary>
	/// When a monster is added, if it is a slime we need to put some effects on it.
	/// </summary>
	/// <param name="monSpawned"></param>
	public void OnMonsterSpawnedInSlimeDungeon(Monster monSpawned)
	{
		//if this monster is a slime, we want it to lose its next turn when the player attacks it.
		// :badcode:
		//
		if (monSpawned.actorRefName.Contains("_slime_"))
		{
			var fighterMons = monSpawned as Fighter;
			fighterMons.myStats.AddStatusByRef("playeratk_loseturn", fighterMons, 9999);
		}
	}

	/// <inheritdoc />
	public override void UpdateSpriteRenderersOnLoad()
	{
		base.UpdateSpriteRenderersOnLoad();

		//record the position of all towers
		AddTowersToMasterTowerList();
		
		//determine what destructibles should look like,
		BeautifySlimeTerrain();

		//finally adjust the destructibles to look correct.
		foreach (var kvp in tileSlimeState)
		{
			var mtd = IntToMTD(kvp.Key);
			var mudActor = mtd.GetActorRef("obj_mudtile") as Destructible;
            if (mtd.HasActorByRef("obj_mudtile"))
			{
				SetStartingInformationOnNewSlimeTile(mudActor, mtd);
				AdjustSpriteInAllTilesAroundThisOne(mtd);
			}
		}
		
		//apply the shader values to every slime tile
		ResetShaderDataOnAllTiles();
	}
}
