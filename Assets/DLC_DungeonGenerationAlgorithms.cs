using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DLC_DungeonGenerationAlgorithms
{
    // Called when initial dungeon creation is finished
    public static void MakeLevelChangesOnPostCreation()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return;
        }

        if (GameStartData.jobAsEnum == CharacterJobs.SHARA)
        {
            RedoBossFightsForSharaMode();
            RemoveBranchEventsForSharaMode();
            RemoveReferencesToRiverstoneForShara();
            SharaModeStuff.RemoveAndHideMapsOrNPCsForShara();
            if (Debug.isDebugBuild) Debug.Log("<color=green>Shara post-gen done.</color>");
        }
        
    }

    static void RemoveReferencesToRiverstoneForShara()
    {
        List<Map> mapsToEdit = new List<Map>();
        mapsToEdit.Add(MapMasterScript.theDungeon.FindFloor(0));
        mapsToEdit.Add(MapMasterScript.theDungeon.FindFloor(350));
        List<Actor> actorsToRemove = new List<Actor>();
        foreach (Map m in mapsToEdit)
        {
            actorsToRemove.Clear();
            foreach(Stairs st in m.mapStairs)
            {
                if (st.NewLocation == MapMasterScript.singletonMMS.townMap || st.pointsToFloor == MapMasterScript.TOWN_MAP_FLOOR)
                {
                    actorsToRemove.Add(st);
                }
            }
            foreach(Actor act in actorsToRemove)
            {
                m.RemoveActorFromMap(act);
            }
        }
    }

    static void RemoveBranchEventsForSharaMode()
    {
        Map startDreamMap = MapMasterScript.theDungeon.FindFloor(MapMasterScript.SHARA_START_FOREST_FLOOR);
        List<Actor> toRemove = new List<Actor>();
        foreach(Stairs st in startDreamMap.mapStairs)
        {
            toRemove.Add(st);
        }
        foreach(Actor act in toRemove)
        {
            startDreamMap.RemoveActorFromMap(act);
        }

        Map branch1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BRANCH_PASSAGE_POSTBOSS1);

        toRemove.Clear();
        foreach (Actor act in branch1.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE && act.actorRefName.Contains("trigger")) // triggers
            {
                toRemove.Add(act);
            }
        }
        foreach (Actor act in toRemove)
        {
            branch1.RemoveActorFromMap(act);
        }

        Map branch2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BRANCH_PASSAGE_POSTBOSS2);
        Actor nShara = branch2.FindActor("npc_shara1") as NPC;
        branch2.RemoveActorFromMap(nShara);
        toRemove.Clear();
        foreach(Actor act in branch2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) // triggers
            {
                toRemove.Add(act);
            }
        }
        foreach(Actor act in toRemove)
        {
            branch2.RemoveActorFromMap(act);
        }       
        
    }

    static void SharaMode_ReconfigureBoss1()
    {
        // Boss 1: Replace Dirtbeak with a couple champion bandits and remove Dirtbeak's callout trigger tiles
        Map boss1map = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS1_MAP_FLOOR);
        Actor dirtbeak = boss1map.FindActor("mon_banditwarlord");
        boss1map.RemoveActorFromMap(dirtbeak);
        boss1map.RemoveActorFromLocation(dirtbeak.GetPos(), dirtbeak);

        List<Actor> triggerTiles = new List<Actor>();
        foreach (Actor act in boss1map.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            if (act.actorRefName == "dirtbeak_bottleneck_explainer" || act.actorRefName == "obj_dirtbeakthronetrigger")
            {
                triggerTiles.Add(act);
            }
        }
        foreach (Actor act in triggerTiles)
        {
            boss1map.RemoveActorFromMap(act);
        }

        Monster bandit1 = MonsterManagerScript.CreateMonster("mon_saboteur", true, true, false, 0f, false);
        Monster bandit2 = MonsterManagerScript.CreateMonster("mon_bandithunter", true, true, false, 0f, false);
        List<Monster> newBosses = new List<Monster>() { bandit1, bandit2 };
        foreach (Monster boss in newBosses)
        {
            boss.isBoss = true;
            boss.MakeChampion();
            boss.MakeChampion();
            boss.myStats.BoostCoreStatsByPercent(0.2f);
            boss.xpMod *= 1.25f;
        }

        boss1map.PlaceActor(bandit1, boss1map.GetTile(new Vector2(6f, 13f)));
        boss1map.PlaceActor(bandit2, boss1map.GetTile(new Vector2(10f, 13f)));

        Monster standardPlunderer = MonsterManagerScript.CreateMonster("mon_plundererboss", true, true, false, 0f, false);
        boss1map.PlaceActor(standardPlunderer, boss1map.GetTile(new Vector2(8f, 9f)));
    }

    static void SharaMode_ReconfigurePreBoss2()
    {
        // Pre boss2 - Switch mob factions, remove hostile beasts, swap the trigger tiles out.
        Map preboss2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PREBOSS2_MAP_FLOOR);
        List<Actor> toRemove = new List<Actor>();

        List<Actor> triggersToAdd = new List<Actor>();       

        foreach (Actor act in preboss2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                if (act.actorRefName == "mon_mottledsandjaw")
                {
                    toRemove.Add(act);
                }
                else if (act.actorRefName == "mon_alchemist" || act.actorRefName == "mon_bandithunter")
                {
                    act.actorfaction = Faction.PLAYER;
                    Monster mn = act as Monster;
                    mn.bufferedFaction = Faction.PLAYER;
                }
            }
            else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                if (act.actorRefName == "trigger_preboss2")
                {
                    toRemove.Add(act);
                    //Destructible newSharaTrigger = new Destructible();
                    Destructible newSharaTrigger = DTPooling.GetDestructible();
                    newSharaTrigger.CopyFromTemplate(GameMasterScript.masterMapObjectDict["trigger_shara_preboss2"]);
                    newSharaTrigger.SetSpawnPos(act.GetPos());
                    newSharaTrigger.SetPos(act.GetPos());
                    triggersToAdd.Add(newSharaTrigger);
                }
            }
        }
        foreach (Actor act in toRemove)
        {
            preboss2.RemoveActorFromMap(act);
        }
        foreach (Actor act in triggersToAdd)
        {
            preboss2.PlaceActor(act, preboss2.GetTile(act.GetPos()));
        }
    }

    static void SharaMode_ReconfigureBoss2()
    {
        List<Actor> actorsToRemove = new List<Actor>();
        Map boss2map = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS2_MAP_FLOOR);
        Vector2 tileForBoss = Vector2.zero;
        List<Vector2> banditTiles = new List<Vector2>();
        Vector2 bossDevicePos = Vector2.zero;
        foreach(Actor act in boss2map.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                if (act.actorRefName == "shadowdummy")
                {
                    actorsToRemove.Add(act);
                }
                if (act.actorRefName == "obj_bossdevice")
                {
                    actorsToRemove.Add(act);
                    bossDevicePos = act.GetPos();
                }
            }
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                if (act.actorRefName == "mon_plundererboss")
                {
                    continue;
                }
                if (act.actorRefName == "mon_fakeplunderer")
                {
                    continue;
                }
                actorsToRemove.Add(act);
                
                if (act.actorRefName == "mon_banditwarlord")
                {
                    tileForBoss = act.GetPos();
                }
            }
        }
        foreach(Actor act in actorsToRemove)
        {
            boss2map.RemoveActorFromMap(act);
        }

        // Now add the new scientist boss.
        Monster crazedSummoner = MonsterManagerScript.CreateMonster("mon_scientist_summoner", true, true, false, 0f, false);
        boss2map.PlaceActor(crazedSummoner, boss2map.GetTile(tileForBoss));

        // And the device, which is actually a monster this time that can be destroyed
        Monster bossDeviceMonster = MonsterManagerScript.CreateMonster("mon_scientist_device", false, false, false, 0f, false);
        boss2map.PlaceActor(bossDeviceMonster, boss2map.GetTile(bossDevicePos));

    }

    static void SharaMode_ReconfigureBoss3()
    {
        Map boss3map = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR);

        // First, get rid of the old Guardian and our Shara NPCs
        boss3map.RemoveActorFromMap(boss3map.FindActor("npc_shara_preboss3"));
        boss3map.RemoveActorFromMap(boss3map.FindActor("mon_ancientsteamgolem"));

        // Now create two new Heavy Golems
        List<Monster> golems = new List<Monster>();
        for (int i = 0; i < 2; i++)
        {
            Monster golem = MonsterManagerScript.CreateMonster("mon_xp_heavygolem", true, true, false, 0f, false);
            golems.Add(golem);
        }

        MapTileData golem1pos = boss3map.GetTile(new Vector2(6f, 16f));
        MapTileData golem2pos = boss3map.GetTile(new Vector2(9f, 16f));

        boss3map.PlaceActor(golems[0], golem1pos);
        boss3map.PlaceActor(golems[1], golem2pos);
    }

    static void SharaMode_ReconfigureFinalAreas()
    {
        Map preHub = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PRE_FINALHUB_FLOOR);
        Map centralProcessing = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_HUB_FLOOR);
        Map finalArea1 = MapMasterScript.theDungeon.FindFloor(208);
        Map finalArea2 = MapMasterScript.theDungeon.FindFloor(209);
        Map finalArea3 = MapMasterScript.theDungeon.FindFloor(210);
        Map finalArea4 = MapMasterScript.theDungeon.FindFloor(211);
        Map sharaFinalBoss = MapMasterScript.theDungeon.FindFloor(MapMasterScript.SHARA_FINALBOSS_FLOOR);

        // Shara bypasses Central Processing and passes through the final areas linearly.
        preHub.RedirectStairs(centralProcessing.floor, finalArea1);

        // Final Area 1 stairs link back to preHub
        finalArea1.RedirectStairs(centralProcessing.floor, preHub);

        // And forward to area2
        Actor sideBoss1 = finalArea1.FindActor("mon_sideareaboss1");
        Stairs linkStairs = new Stairs();
        linkStairs.prefab = "FutureStairsUp";
        linkStairs.pointsToFloor = finalArea2.floor;
        linkStairs.NewLocation = finalArea2;
        linkStairs.newLocationID = finalArea2.mapAreaID;
        MapTileData stairsTile = finalArea1.GetRandomEmptyTile(sideBoss1.GetPos(), 2, true, anyNonCollidable: false, preferLOS: true);
        finalArea1.PlaceActor(linkStairs, stairsTile);

        // Side area 2 goes back to area 1, and forward to area3
        finalArea2.RedirectStairs(centralProcessing.floor, finalArea1);
        Actor regenObject = finalArea2.FindActor("obj_regenquestobject");
        linkStairs = new Stairs();
        linkStairs.prefab = "FutureStairsUp";
        linkStairs.pointsToFloor = finalArea3.floor;
        linkStairs.NewLocation = finalArea3;
        linkStairs.newLocationID = finalArea3.mapAreaID;
        stairsTile = finalArea2.GetRandomEmptyTile(regenObject.GetPos(), 2, true, anyNonCollidable: false, preferLOS: true);
        finalArea2.PlaceActor(linkStairs, stairsTile);

        // Side area 3 goes back to area 2, and forward to area4
        finalArea3.RedirectStairs(centralProcessing.floor, finalArea2);
        Actor sideBoss3 = finalArea3.FindActor("mon_sideareaboss4");
        linkStairs = new Stairs();
        linkStairs.prefab = "FutureStairsUp";
        linkStairs.pointsToFloor = finalArea4.floor;
        linkStairs.NewLocation = finalArea4;
        linkStairs.newLocationID = finalArea4.mapAreaID;
        stairsTile = finalArea3.GetRandomEmptyTile(sideBoss3.GetPos(), 2, true, anyNonCollidable: false, preferLOS: true);
        finalArea3.PlaceActor(linkStairs, stairsTile);

        // Side area 4 goes back to area 3, and forward to area4
        finalArea4.RedirectStairs(centralProcessing.floor, finalArea3);
        Actor sideBoss4 = finalArea4.FindActor("mon_sideareaboss2");
        linkStairs = new Stairs();
        linkStairs.prefab = "FutureStairsUp";
        linkStairs.pointsToFloor = sharaFinalBoss.floor;
        linkStairs.NewLocation = sharaFinalBoss;
        linkStairs.newLocationID = sharaFinalBoss.mapAreaID;
        stairsTile = finalArea4.GetRandomEmptyTile(sideBoss4.GetPos(), 2, true, anyNonCollidable: false, preferLOS: true);
        finalArea4.PlaceActor(linkStairs, stairsTile);

    }

    static void RedoBossFightsForSharaMode()
    {
        SharaMode_ReconfigureBoss1();
        SharaMode_ReconfigurePreBoss2();
        SharaMode_ReconfigureBoss2();
        SharaMode_ReconfigureBoss3();
        SharaMode_ReconfigureFinalAreas();
    }

    public static bool BuildKeepMap(Map createMap)
    {
        float startTime = Time.realtimeSinceStartup;

        // Decide on horizontal or vertical orientation. 
        // Horizontal has columns of walls interspersed with columns of open space
        // Vertical is rows of walls with rows of open space

        bool horizontalOrientation = UnityEngine.Random.Range(0, 2) == 0;

        if (createMap.dungeonLevelData.GetMetaData("horizontal") == 1)
        {
            horizontalOrientation = true;
        }
        else if (createMap.dungeonLevelData.GetMetaData("vertical") == 1)
        {
            horizontalOrientation = false;
        }

        if (!horizontalOrientation)
        {
            createMap.dictMapDataForGeneration.Add("horizontal", 0);
        }
        else
        {
            createMap.dictMapDataForGeneration.Add("horizontal", 1);
        }

        float lavaChanceForOpenings = 1.0f;

        // Start with either a wall OR open space

        bool creatingWall = UnityEngine.Random.Range(0, 2) == 0;

        int minChunkSizeWall = 1;
        int maxChunkSizeWall = 4;
        int minChunkSizeGround = 2;
        int maxChunkSizeGround = 5;
        int minChunk = 0;
        int maxChunk = 0;

        if (creatingWall)
        {
            minChunk = minChunkSizeWall;
            maxChunk = maxChunkSizeWall;
        }
        else
        {
            minChunk = minChunkSizeGround;
            maxChunk = maxChunkSizeGround;
        }

        // And figure out how big the chunk is...
        int chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
        // Preferring smaller sizes
        if (chunkSize == maxChunk - 1) chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);

        int prevChunkSize = chunkSize;

        List<int> locationsOfWallOpenings = new List<int>();

        int startX = 1;
        int endX = createMap.columns - 1;
        int startY = 1;
        int endY = createMap.rows - 1;

        int clampValue = 5;
        if (createMap.columns >= 32)
        {
            clampValue++;
        }
        if (createMap.columns >= 36)
        {
            clampValue++;
        }
        if (createMap.columns >= 40)
        {
            clampValue++;
        }

        if (horizontalOrientation)
        {
            startX = 5;
            endX = createMap.rows - 5;
        }
        else
        {
            startY = 5;
            endY = createMap.columns - 5;
        }

        int maxBoundsOfOpening = endX - 1; // Openings within the map
        if (horizontalOrientation)
        {
            maxBoundsOfOpening = endY - 1;
        }

        // In horizontal orientation, we need to know in advance which rows are wall and which are ground,
        // Because we are iterating by row. For example on a 3x3 grid the iteration looks like
        // 2 5 8  <- Row 2
        // 1 4 7  <- Row 1
        // 0 3 6  <- Row 0
        // We need to know on Step 0, 1, 2 whether rows 0, 1, 2 are walls or not
        // And remember this for when we're on steps 3, 4, 5

        HashSet<int> yCoordsOfGroundRows = new HashSet<int>();
        bool[,] mapOfHorizontalOpenings = new bool[createMap.columns, createMap.rows];
        Dictionary<int, int> yCoordsToChunkIndex = new Dictionary<int, int>();

        Dictionary<MapTileData, int> dictMTDToSwitchGateIndex = new Dictionary<MapTileData, int>();

        HashSet<MapTileData> tilesConvertedToLava = new HashSet<MapTileData>();

        int chunkIndex = 0;

        Dictionary<int, Destructible> dictFloorSwitchesByGateIndex = new Dictionary<int, Destructible>();

        locationsOfWallOpenings = new List<int>();

        if (horizontalOrientation)
        {
            for (int i = 1; i < createMap.rows - 1; i++)
            {
                if (!creatingWall)
                {
                    yCoordsOfGroundRows.Add(i);                    
                }
                else
                {
                    // This must be a wall.
                }
                
                yCoordsToChunkIndex.Add(i, chunkIndex);

                if (creatingWall) // Find the opening dimension for this wall chunk
                {
                    if (locationsOfWallOpenings.Count == 0)
                    {
                        locationsOfWallOpenings.Clear();
                        int numOpenings = UnityEngine.Random.Range(1, 4);
                        for (int t = 0; t < numOpenings; t++)
                        {
                            locationsOfWallOpenings.Add(UnityEngine.Random.Range(startX + 1, maxBoundsOfOpening));
                        }
                    }
                    for (int t = 0; t < locationsOfWallOpenings.Count; t++)
                    {
                        mapOfHorizontalOpenings[locationsOfWallOpenings[t], i] = true;
                    }

                }

                chunkSize--;
                if (chunkSize == 0) // Done with this chunk.
                {
                    if (!creatingWall)
                    {
                        // If we just finished creating a ground chunk, let's pick a place to put our switcheroo
                        // It must be somewhere in the chunk we just did.
                        int randomX = UnityEngine.Random.Range(startX + 2, endX - 2);
                        int randomY = UnityEngine.Random.Range(i - prevChunkSize+1, i+1);
                        //Debug.Log("XSpawn range for HORIZONTAL orientation is " + (startX+2) + " to " + (endX-3));
                        //Debug.Log("YSpawn range for HORIZONTAL orientation is " + (i-prevChunkSize+1) + " to " + (i));
                        MapTileData tileForChunk = createMap.mapArray[randomX, randomY];
                        Destructible floorSwitch = createMap.CreateDestructibleInTile(tileForChunk, "obj_floorswitch");
                        floorSwitch.SetActorData("floorswitch_index", chunkIndex+1);
                    }

                    locationsOfWallOpenings.Clear();
                    creatingWall = !creatingWall;

                    if (creatingWall)
                    {
                        minChunk = minChunkSizeWall;
                        maxChunk = maxChunkSizeWall;
                    }
                    else
                    {
                        minChunk = minChunkSizeGround;
                        maxChunk = maxChunkSizeGround;
                    }

                    chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    if (chunkSize == maxChunk - 1) chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    if (prevChunkSize == chunkSize)
                    {
                        chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    }
                    prevChunkSize = chunkSize;
                    chunkIndex++;
                }
            }
        }

        chunkIndex = 0;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                // Iterating one row at a time
                if (!horizontalOrientation)
                {
                    if (creatingWall)
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.WALL, createMap.mgd);
                    }
                    else
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.GROUND, createMap.mgd);                        
                    }
                    dictMTDToSwitchGateIndex.Add(createMap.mapArray[x, y], chunkIndex);
                }
                else
                {
                    // Constructing horizontal orientation.
                    if (yCoordsOfGroundRows.Contains(y))
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.GROUND, createMap.mgd);
                    }
                    else if (mapOfHorizontalOpenings[x, y])
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.GROUND, createMap.mgd);
                        createMap.mapArray[x, y].AddTag(LocationTags.DUGOUT);
                        if (UnityEngine.Random.Range(0, 1f) <= lavaChanceForOpenings && !tilesConvertedToLava.Contains(createMap.mapArray[x, y]))
                        {
                            tilesConvertedToLava.Add(createMap.mapArray[x, y]);
                        }
                    }
                    else // If it's not in the Ground set, this MUST be wall.
                    {
                        createMap.mapArray[x, y].ChangeTileType(TileTypes.WALL, createMap.mgd);
                    }

                    dictMTDToSwitchGateIndex.Add(createMap.mapArray[x, y], yCoordsToChunkIndex[y]);
                }
            }

            // If we are creating a wall chunk, we must ensure an opening exists so you can pass through
            if (creatingWall && !horizontalOrientation)
            {
                if (locationsOfWallOpenings.Count == 0)
                {
                    int numOpenings = UnityEngine.Random.Range(1, 4);
                    for (int i = 0; i < numOpenings; i++)
                    {
                        locationsOfWallOpenings.Add(UnityEngine.Random.Range(startY + 1, maxBoundsOfOpening));
                    }
                }
                for (int i = 0; i < locationsOfWallOpenings.Count; i++)
                {
                    createMap.mapArray[x, locationsOfWallOpenings[i]].ChangeTileType(TileTypes.GROUND, createMap.mgd);
                    createMap.mapArray[x, locationsOfWallOpenings[i]].AddTag(LocationTags.DUGOUT);
                    if (UnityEngine.Random.Range(0, 1f) <= lavaChanceForOpenings && !tilesConvertedToLava.Contains(createMap.mapArray[x, locationsOfWallOpenings[i]]))
                    {
                        tilesConvertedToLava.Add(createMap.mapArray[x, locationsOfWallOpenings[i]]);
                    }
                }
            }

            if (!horizontalOrientation) // We don't do this in horizontal orientation because it was pre-determined.
            {
                chunkSize--;
                if (chunkSize == 0) // Done with this chunk.
                {
                    if (!creatingWall)
                    {
                        // Pick random tile in the chunk we just finished and use it for a floor switch
                        // At some point we want to make sure this is actually valid for chunk.
                        //Debug.Log("VERTICAL orientation. X is " + x + " prev chunk size is " + prevChunkSize);
                        int randomX = UnityEngine.Random.Range(x - prevChunkSize + 1, x+1);
                        int randomY = UnityEngine.Random.Range(startY + 2, endY - 2);
                        //Debug.Log("XSpawn range for VERTICAL orientation is " + (x - prevChunkSize+1) + " to " + (x));
                        //Debug.Log("YSpawn range for VERTICAL orientation is " + (startY+2) + " to " + (endY-3));
                        MapTileData tileForChunk = createMap.mapArray[randomX, randomY];
                        Destructible floorSwitch = createMap.CreateDestructibleInTile(tileForChunk, "obj_floorswitch");
                        floorSwitch.SetActorData("floorswitch_index", chunkIndex+1);
                    }

                    creatingWall = !creatingWall;

                    if (creatingWall)
                    {
                        minChunk = minChunkSizeWall;
                        maxChunk = maxChunkSizeWall;
                    }
                    else
                    {
                        minChunk = minChunkSizeGround;
                        maxChunk = maxChunkSizeGround;
                    }

                    chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    if (chunkSize == maxChunk - 1) chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    locationsOfWallOpenings.Clear();
                    if (prevChunkSize == chunkSize)
                    {
                        chunkSize = UnityEngine.Random.Range(minChunk, maxChunk);
                    }
                    prevChunkSize = chunkSize;
                    chunkIndex++;
                }
            }

        }

        createMap.mapRooms[0].internalTiles.Clear();

        for (int x = 1; x < createMap.columns - 1; x++)
        {
            for (int y = 1; y < createMap.rows - 1; y++)
            {
                if (createMap.mapArray[x, y].tileType == TileTypes.GROUND)
                {
                    createMap.mapRooms[0].internalTiles.Add(createMap.mapArray[x, y]);
                }
            }
        }

        if (createMap.dungeonLevelData.extraRivers > 0)
        {
            createMap.BuildRiver();
            //createMap.BuildMudPatches(6);
            for (int i = 1; i < createMap.dungeonLevelData.extraRivers; i++)
            {
                createMap.BuildRiver();
                //createMap.BuildMudPatches(3);
            }
        }

        bool[,] tileAlreadyConverted = new bool[createMap.columns, createMap.rows];

        foreach (MapTileData mtd in tilesConvertedToLava)
        {
            // Rivers should always be clear!
            if (tileAlreadyConverted[mtd.iPos.x, mtd.iPos.y] || mtd.CheckTag(LocationTags.WATER) || mtd.CheckTag(LocationTags.MUD))
            {
                tileAlreadyConverted[mtd.iPos.x, mtd.iPos.y] = true;
                mtd.RemoveTag(LocationTags.LAVA);
                mtd.RemoveActorByRef("obj_floorspikes_visible");
                continue;
            }

            tileAlreadyConverted[mtd.iPos.x, mtd.iPos.y] = true;
            bool isLava = false;
            if (UnityEngine.Random.Range(0, 2) == 1)
            {
                isLava = true;
                mtd.AddTag(LocationTags.LAVA);
            }
            else
            {
                if (!mtd.CheckActorRef("obj_floorspikes_visible"))
                {
                    createMap.CreateDestructibleInTile(mtd, "obj_floorspikes_visible");
                }

            }

            float chanceToSpreadDanger = 0.34f;

            CustomAlgorithms.GetNonCollidableTilesAroundPoint(mtd.pos, 1, GameMasterScript.genericMonster, createMap);
            for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
            {
                if (tileAlreadyConverted[CustomAlgorithms.nonCollidableTileBuffer[i].iPos.x, CustomAlgorithms.nonCollidableTileBuffer[i].iPos.y])
                {
                    continue;
                }

                if (UnityEngine.Random.Range(0, 1f) <= chanceToSpreadDanger
                     && !CustomAlgorithms.nonCollidableTileBuffer[i].CheckTag(LocationTags.WATER)
                     && !CustomAlgorithms.nonCollidableTileBuffer[i].CheckTag(LocationTags.MUD))
                {
                    tileAlreadyConverted[mtd.iPos.x, mtd.iPos.y] = true;

                    if (isLava)
                    {
                        CustomAlgorithms.nonCollidableTileBuffer[i].AddTag(LocationTags.LAVA);
                    }
                    else
                    {
                        if (mtd.CheckActorRef("obj_floorspikes_visible")) continue;
                        createMap.CreateDestructibleInTile(mtd, "obj_floorspikes_visible");
                    }
                }
            }
        }

        HashSet<MapTileData> convertedRiverTiles = new HashSet<MapTileData>();
        for (int x = 1; x < createMap.columns - 1; x++)
        {
            for (int y = 1; y < createMap.rows - 1; y++)
            {
                if (createMap.mapArray[x, y].CheckMapTag(MapGenerationTags.MAPGEN_RIVERDIGOUT))
                {
                    convertedRiverTiles.Add(createMap.mapArray[x, y]);
                }
            }
        }
        

        // Convert river tile crossover points to metal gates that we cannot pass through
        // without pressing the corresponding floor switch
        foreach (MapTileData mtd in convertedRiverTiles)
        {
            if (horizontalOrientation)
            {
                // Check UP and DOWN, do not build gate if we already have one there.
                MapTileData checkTile = createMap.mapArray[mtd.iPos.x, mtd.iPos.y + 1];
                if (checkTile.CheckActorRef("obj_metalgate"))
                {
                    continue;
                }
                checkTile = createMap.mapArray[mtd.iPos.x, mtd.iPos.y - 1];
                if (checkTile.CheckActorRef("obj_metalgate"))
                {
                    continue;
                }
            }
            else
            {
                // Check LEFT and RIGHT, do not build.. etc
                MapTileData checkTile = createMap.mapArray[mtd.iPos.x - 1, mtd.iPos.y];
                if (checkTile.CheckActorRef("obj_metalgate"))
                {
                    continue;
                }
                checkTile = createMap.mapArray[mtd.iPos.x + 1, mtd.iPos.y];
                if (checkTile.CheckActorRef("obj_metalgate"))
                {
                    continue;
                }
            }

            int switchGateIndex;
            if (dictMTDToSwitchGateIndex.TryGetValue(mtd, out switchGateIndex))
            {
                Destructible gate = createMap.CreateDestructibleInTile(mtd, "obj_metalgate");
                gate.SetActorData("gateindex", switchGateIndex);
            }
        }

        Stairs stairsPrevious = new Stairs();
        stairsPrevious.stairsUp = true;
        stairsPrevious.prefab = "StoneStairsDown";

        Stairs stairsNext = new Stairs();
        stairsNext.prefab = "StoneStairsUp";
        
        bool foundStairsTile = false;
        MapTileData checkTileForStairsPrevious = null;
        int attempts = 0;
        while (!foundStairsTile)
        {
            int x = 0;
            int y = 0;
            if (horizontalOrientation)
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(startY, startY+4);
            }
            else
            {
                x = UnityEngine.Random.Range(startX, startX + 4);
                y = UnityEngine.Random.Range(startY, endY);
            }            
            
            checkTileForStairsPrevious = createMap.mapArray[x, y];
            if (createMap.CheckValidForStairs(checkTileForStairsPrevious))
            {
                break;
            }
            foundStairsTile = true;
            attempts++;
            if (attempts > 1000)
            {
                break;
            }
        }

        MapTileData checkTileForStairsNext = null;
        foundStairsTile = false;
        attempts = 0;
        while (!foundStairsTile)
        {
            int x = 0;
            int y = 0;
            if (horizontalOrientation)
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(endY-4, endY);
            }
            else
            {
                x = UnityEngine.Random.Range(endX-4, endX);
                y = UnityEngine.Random.Range(startY, endY);
            }

            checkTileForStairsNext = createMap.mapArray[x, y];
            if (createMap.CheckValidForStairs(checkTileForStairsNext))
            {
                break;
            }
            foundStairsTile = true;
            attempts++;
            if (attempts > 1000)
            {
                break;
            }
        }

        createMap.PlaceActor(stairsNext, checkTileForStairsNext);
        createMap.PlaceActor(stairsPrevious, checkTileForStairsPrevious);

        createMap.heroStartTile = checkTileForStairsPrevious.pos;
       
        //Debug.Log("Time to create keep map: " + (Time.realtimeSinceStartup - startTime));

        return true;
    }


}
