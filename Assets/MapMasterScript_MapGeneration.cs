using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public partial class MapMasterScript
{
    IEnumerator MapLoadingWaiter(int prevFloor, DungeonInfo di)
    {
        //yield return new WaitForSeconds(0.01f);
        yield return null;
        //float fillAmount = ((float)(prevFloor+1) / (float)di.floors);
        float fillAmount = (1f / numberOfMaps);
        int displayFloor = prevFloor + 2;
        //string txt = "Building the dungeon...";
        if (displayFloor % 8 == 0)
        {
            GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        }
        UIManagerScript.MoveLoadingBar(fillAmount);
        if (prevFloor == -1)
        {
            CreateNewDungeon(di.floors, di.baseChallenge, di.challengeScale, true, (prevFloor + 1)); // Create for the first time.
        }
        else
        {
            CreateNewDungeon(di.floors, di.baseChallenge, di.challengeScale, false, (prevFloor + 1));
        }

    }

    public Dungeon CreateNewDungeon(int floors, float baseChallenge, float challengeScale, bool firstIteration, int curFloor)
    {
        dungeonCreativeActive = true;

        bool finished = false;
        if (firstIteration)
        {
            if (GameMasterScript.gmsSingleton.gameRandomSeed == 0)
            {
                GameMasterScript.SetRNGSeed(UnityEngine.Random.Range(1, 1000000));
            }
            else
            {

            }
            numberOfMaps = GameMasterScript.masterDungeonLevelList.Values.Count;
            theDungeon = new Dungeon(floors, baseChallenge, challengeScale);

            UIManagerScript.TurnOnPrettyLoading(1.0f);

            //UIManagerScript.WriteLoadingText(StringManager.GetString("creating_map"));

            string tipString = TutorialManagerScript.GetNextGameTip();
            //Debug.Log("Load tip " + tipString);

            UIManagerScript.WriteLoadingText(StringManager.GetString(tipString));
        }

        if (curFloor < floors)
        {
            Map nMap = CreateNewMap(false, curFloor, floors, baseChallenge + (challengeScale * curFloor), null, null);
            theDungeon.maps.Add(nMap);
            StartCoroutine(MapLoadingWaiter(curFloor, new DungeonInfo(floors, baseChallenge, challengeScale)));
            return theDungeon;
        }
        else
        {
            finished = true;
            Map linkMap = null;

            // Connect stairs in main dungeon

            for (int i = 0; i < floors; i++) // Is floors the right thing to use here? Not theDungeon.maps.Count?
            {
                linkMap = theDungeon.FindFloor(i);

                if (i == 5 || i == 6) // Custom for bandits boss fight here, as well as the following map.
                {
                    continue;
                }

                if (i == 10)
                {
                    List<Actor> remove = new List<Actor>();
                    foreach (Stairs st in linkMap.mapStairs)
                    {
                        remove.Add(st);
                    }
                    foreach (Actor act in remove)
                    {
                        linkMap.RemoveActorFromMap(act);
                    }
                    continue;
                }

                if ((i == 9) || (i == 11)) // Custom before and after spirit fight
                {
                    continue;
                }

                foreach (Stairs st in linkMap.mapStairs)
                {
                    if ((st.stairsUp == false) && (st.pointsToFloor == -1)) // Stairs down?
                    {
                        st.NewLocation = theDungeon.FindFloor(i + 1);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else
                    {
                        if ((i > 0) && (st.pointsToFloor == -1))
                        {
                            st.NewLocation = theDungeon.FindFloor(i - 1);
                            if (st.NewLocation == null)
                            {
                                Debug.Log("Failure to link stairs on floor " + linkMap.floor);
                            }
                            st.newLocationID = st.NewLocation.mapAreaID;
                        }
                    }
                }
            }

        }

        // Now link the stairs.

        if (finished)
        {
            MapStart(false);
        }

        return theDungeon;
    }

    public MapGenerationData RerollMapGenerationParameters(MapGenerationData mgd, Map createdMap, int localFloor)
    {
        switch (createdMap.dungeonLevelData.layoutType)
        {
            case DungeonFloorTypes.RUINS:
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.STANDARD:
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.HIVES:
                mgd.minRoomSize = 5;
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.STANDARDNOHALLS:
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.FUTURENOHALLS:
                mgd.minRoomSize = 4;
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.CAVE:
                mgd.minRoomSize = 9;
                mgd.maxRoomSize = 12;
                break;
            case DungeonFloorTypes.AUTOCAVE:
                // We don't use rooms for this.
                break;
            case DungeonFloorTypes.VOLCANO:
                // We don't use rooms for this.
                break;
            case DungeonFloorTypes.MAZE:
                // We don't use rooms for this.
                break;
            case DungeonFloorTypes.LAKE:
                mgd.maxRoomSize = 18;
                break;
            case DungeonFloorTypes.MAZEROOMS:
                // We don't use rooms for this.
                mgd.minRoomSize = 2;
                mgd.maxRoomSize = 4;
                break;
            case DungeonFloorTypes.CAVEROOMS:
                mgd.minRoomSize = 5;
                mgd.maxRoomSize = 9;
                break;
            case DungeonFloorTypes.ISLANDS:
                mgd.minRoomSize = 2;
                mgd.maxRoomSize = 4;
                break;
        }

        mgd.maxRoomAttempts = maxRoomAttempts;
        mgd.corridorOrigRandom = corridorOrigRandom;

        mgd.maxCorridorAttempts = 20; // This should justbe preset.
        mgd.minDeadendLength = 2;
        mgd.maxDeadendLength = 9;

        mgd.chanceConvertWallToGround = createdMap.dungeonLevelData.chanceToOpenWalls;

        mgd.chanceToUseRoomTemplate = chanceToUseRoomTemplate;
        mgd.chanceToSpawnChampionMonster = chanceToSpawnChampionMonster;

        return mgd;
    }

    public Map CreateNewMap(bool setNewActivemap, int floor, int maxFloors, float challengeLevel, DungeonLevel forceDL, ItemWorldMetaData itemWorldProperties, bool specialMapGenData = false)
    {
        mapUnderConstruction = null;
        float startTime = Time.realtimeSinceStartup;

        if (itemWorldProperties == null)
        {
            itemWorldProperties = new ItemWorldMetaData();
        }

        // Allowing for alternate Map subclasses with their own creation calls. 
        // Right now, we'll use enums to determine what to create, but what 
        // might be smoother in the long run is keeping the class name in the
        // level data, and instantiating it automagically via activator.
        Map createdMap = null;

        //if we are asking for a specfic type, create one of those
        if (forceDL == null)
        {
            createdMap = new Map(floor);
        }
        else
        {
            createdMap = SetCorrectMapTypeFromDungeonLevelData(forceDL, floor);
        }

        if (forceDL != null)
        {
            createdMap.dungeonLevelData = forceDL;
        }
        else
        {
            createdMap.dungeonLevelData = theDungeon.GetLevelData(floor);
        }

        createdMap.Init();
        createdMap.numFloors = maxFloors;
        numberOfMaps++;
        MapGenerationData mgd = new MapGenerationData();
        createdMap.mgd = mgd;

        //mgd.columns = columns;
        //mgd.rows = rows;

        mgd.maxChamps = createdMap.dungeonLevelData.maxChampions;
        mgd.maxChampMods = createdMap.dungeonLevelData.maxChampionMods;

        if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
        {
            mgd.maxChampMods++;
        }

        if (floor >= MapMasterScript.REALM_OF_GODS_START && floor <= MapMasterScript.REALM_OF_GODS_END)
        {
            mgd.maxChampMods = 4; // ROG creatures cant have too many
        }

        mgd.randomProps = randomProps;
        mgd.anyDecorChance = anyDecorChance;
        mgd.grassDecorChance = grassDecorChance;
        mgd.anyPropChance = anyPropChance;
        mgd.maxPropsPerRoom = maxPropsPerRoom;
        mgd.doorTiles = doorTiles;

        // Hardcode stuff below, eventually do it better and make templates.

        mgd.minRoomSize = 4;

        mgd.minMonstersPerRoom = minMonstersPerRoom;
        mgd.maxMonstersPerRoom = maxMonstersPerRoom;

        if (floor > 2 && floor < 100)
        {
            minMonstersPerRoom = 1;
            maxMonstersPerRoom = 1 + (int)(floor / 3f);
            if (maxMonstersPerRoom > 2)
            {
                maxMonstersPerRoom = 2;
            }
        }

        int localFloor = floor;
        if (floor >= 100)
        {
            localFloor = 0;
        }

        if (!specialMapGenData)
        {
            mgd = RerollMapGenerationParameters(mgd, createdMap, localFloor);
        }
        else
        {
            mgd = DungeonMaker.RerollMapGenerationParameters(mgd, createdMap, localFloor);
        }

        mgd.minRooms = createdMap.dungeonLevelData.minRooms;
        mgd.maxRooms = createdMap.dungeonLevelData.maxRooms;
        mgd.maxCorridors = createdMap.dungeonLevelData.maxCorridors;
        mgd.maxCorridorLength = createdMap.dungeonLevelData.maxCorridorLength;
        mgd.maxDeadends = createdMap.dungeonLevelData.maxDeadends;

        // Be much less lenient with special map failures. If we are having this much trouble, reroll the master params.
        int maxFailures = specialMapGenData ? 5 : 50;

        // Generates the map data in array, builds rooms, corridors, connects them with doors.        
        bool success = createdMap.BuildRandomMap(itemWorldProperties);
        int fail = 0;
        while (!success)
        {
            if (!specialMapGenData)
            {
                mgd = RerollMapGenerationParameters(mgd, createdMap, localFloor);
            }
            else
            {
                mgd = DungeonMaker.RerollMapGenerationParameters(mgd, createdMap, localFloor);
            }

            createdMap = new Map(floor); // Create the first floor.
            if (forceDL != null)
            {
                createdMap.dungeonLevelData = forceDL;
            }
            else
            {
                createdMap.dungeonLevelData = theDungeon.GetLevelData(floor);
            }

            createdMap.Init();
            createdMap.numFloors = maxFloors;
            numberOfMaps++;
            createdMap.mgd = mgd;
            success = createdMap.BuildRandomMap(itemWorldProperties);
            fail++;
            if (fail > maxFailures)
            {

                return null;
            }
        }


        //Debug.Log("Created map " + createdMap.floor + " " + createdMap.GetName());
        AddMapToDictAndIncrementMapCounter(createdMap);
        int tempCount = 0;
        foreach (Room room in createdMap.mapRooms)
        {
            if (room.template != null)
            {
                tempCount++;
            }
        }
        if (setNewActivemap)
        {
            activeMap = createdMap;
            mapLoaded = true;
            GameMasterScript.cameraScript.SetBGColor(activeMap.dungeonLevelData.bgColor);
            traversed = new bool[activeMap.rows, activeMap.columns];
            tChecked = new bool[activeMap.rows, activeMap.columns];
        }
        if (createdMap.dungeonLevelData.effectiveFloor != 0)
        {
            createdMap.effectiveFloor = createdMap.dungeonLevelData.effectiveFloor;
        }

        float totalTime = Time.realtimeSinceStartup - startTime;
        /* if (totalTime > 0.032f)
        {
            Debug.Log("Sub 30 FPS creating floor " + createdMap.floor + " " + createdMap.dungeonLevelData.layoutType + ": " + totalTime + "s");
        } */
        //Debug.Log("Time to build level " + createdMap.floor + " " + createdMap.dungeonLevelData.layoutType + ": " + totalTime + "s");

        return createdMap;
    }

    IEnumerator WaitThenContinueDungeonGeneration(int index)
    {
        yield return null;
        index++;
        float fillAmount = (1f / numberOfMaps) * 0.5f;
        UIManagerScript.MoveLoadingBar(fillAmount);
        ContinueDungeonGeneration(index);
    }

    public void ContinueDungeonGeneration(int index)
    {
        // Create special maps that aren't part of the normal dungeon.
        if (index >= GameMasterScript.allDungeonLevelsAsList.Count) // WAS masterDungeonLevelList.Values.Count...
        {
            FinishDungeonGeneration();
            return;
        }

        DungeonLevel dlToCreate = GameMasterScript.allDungeonLevelsAsList[index];

        if (index % 5 == 0)
        {
            GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        }
        if (dlToCreate.floor < 100)
        {
            StartCoroutine(WaitThenContinueDungeonGeneration(index));
            return;
        }

        // Town is a special case.
        if (dlToCreate.specialRoomTemplate != null && dlToCreate.specialRoomTemplate.refName == "town1hub")
        {
            townMap = CreateNewMap(false, TOWN_MAP_FLOOR, 0, 0.0f, dlToCreate, null);
            townMap.effectiveFloor = 0;
            theDungeon.maps.Add(townMap);
            StartCoroutine(WaitThenContinueDungeonGeneration(index));
            return;
        }
        if (dlToCreate.specialRoomTemplate != null && dlToCreate.specialRoomTemplate.refName == "town1expansion")
        {
            townMap2 = CreateNewMap(false, TOWN2_MAP_FLOOR, 0, 0.0f, dlToCreate, null);
            townMap2.effectiveFloor = 0;
            theDungeon.maps.Add(townMap2);
            StartCoroutine(WaitThenContinueDungeonGeneration(index));
            return;
        }

        if (!dlToCreate.itemWorld)
        {
            if (((dlToCreate.floor >= MapMasterScript.FROG_DRAGON_DUNGEONSTART_FLOOR && dlToCreate.floor <= 399) ||
                (dlToCreate.floor >= 356 && dlToCreate.floor <= 360)) &&
                ((GameStartData.gameInSharaMode || GameStartData.slotInSharaMode[GameStartData.saveGameSlot])))
            {
                StartCoroutine(WaitThenContinueDungeonGeneration(index));
                return;
            }
            Map newMap = CreateNewMap(false, dlToCreate.floor, 0, dlToCreate.challengeValue, dlToCreate, null);
            if (newMap == null)
            {
                Debug.LogError(dlToCreate.floor + " ITS NULL???");
            }
            theDungeon.maps.Add(newMap);
            specialMaps.Add(newMap);
            StartCoroutine(WaitThenContinueDungeonGeneration(index));
            return;
        }

        StartCoroutine(WaitThenContinueDungeonGeneration(index));
        return;

        // End special map creation
    }

    public void FinishDungeonGeneration()
    {
        dungeonCreativeActive = false;
        float start = GameMasterScript.gmsSingleton.ReadTempFloatData("mapgenstart");
        //if (Debug.isDebugBuild) Debug.Log("Creation time: " + (Time.realtimeSinceStartup - start));

        // Link stairs for these special maps.       
        // Connect all the stairs.
        Map campfire = theDungeon.FindFloor(CAMPFIRE_FLOOR);
        foreach (Stairs st in campfire.mapStairs)
        {
            //Debug.Log("Connecting campfire stairs");
            st.NewLocation = townMap;
            st.newLocationID = townMap.mapAreaID;
        }

        Map dirtbeakFight = theDungeon.FindFloor(5); // Dirtbeak fight
        Map floor6 = theDungeon.FindFloor(6); // The regular dungeon 7F
        Map spiritsFight = theDungeon.FindFloor(BOSS2_MAP_FLOOR); // spirit battle
        Map floor11 = theDungeon.FindFloor(11); // The regular dungeon 12F

        Map branch1AltPathEnd = theDungeon.FindFloor(138);
        Map branch2AltPathEnd = theDungeon.FindFloor(154);

        // Link Stairs, Connect Stairs, Alt Path Stairs
        Stairs newST = branch1AltPathEnd.SpawnStairs(false);
        newST.SetDestination(spiritsFight);

        // End of branch 2.
        newST = branch2AltPathEnd.SpawnStairs(false);
        newST.SetDestination(theDungeon.FindFloor(PRE_BOSS3_MEETSHARA_MAP_FLOOR));

        foreach (Stairs st in dirtbeakFight.mapStairs)
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS1)); // Branched path selection
            }
            else
            {
                st.SetDestination(theDungeon.FindFloor(PREBOSS1_MAP_FLOOR)); // Pre-boss1 corridor. Was 4
            }
        }

        Map preFinalHubFloor = theDungeon.FindFloor(PRE_FINALHUB_FLOOR);
        foreach (Stairs st in preFinalHubFloor.mapStairs)
        {
            if (!st.stairsUp &&
                (st.NewLocation.floor == FINAL_BOSS_FLOOR))
            {
                st.SetDestination(theDungeon.FindFloor(FINAL_HUB_FLOOR));
            }
        }

        foreach (Stairs st in floor6.mapStairs)
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.NewLocation = theDungeon.FindFloor(7); // Just go the normal way right?
                st.newLocationID = st.NewLocation.mapAreaID;
            }
            else
            {
                st.NewLocation = theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS1);
                st.newLocationID = st.NewLocation.mapAreaID;
            }
        }

        foreach (Stairs st in floor11.mapStairs)
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.NewLocation = theDungeon.FindFloor(12); // Branched path selection
                st.newLocationID = st.NewLocation.mapAreaID;
            }
            else
            {
                st.NewLocation = theDungeon.FindFloor(146); // The passageway
                st.newLocationID = st.NewLocation.mapAreaID;
            }
        }

        // Pre-Boss1 stair stuff
        foreach (Stairs st in theDungeon.FindFloor(4).mapStairs)
        {
            if (st.stairsUp == false) // Stairs down?
            {
                bool pointsToBoss = false;
                if ((st.NewLocation != null) && (st.NewLocation.floor == 5))
                {
                    pointsToBoss = true;
                }
                if ((st.pointsToFloor == 5) || (pointsToBoss))
                {
                    st.SetDestination(theDungeon.FindFloor(PREBOSS1_MAP_FLOOR)); // Set ingame floor 5 to go to the branch.
                    //Debug.Log("Redirect map 5 stairs to preboss1");
                    break;
                }
            }
        }
        foreach (Stairs st in theDungeon.FindFloor(PREBOSS1_MAP_FLOOR).mapStairs)
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(5)); // Boss fight
            }
            else
            {
                st.SetDestination(theDungeon.FindFloor(4)); // Dungeon floor
            }
        }

        // Pre-boss2
        foreach (Stairs st in theDungeon.FindFloor(9).mapStairs) // Old amber
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(PREBOSS2_MAP_FLOOR)); // Set ingame floor 5 to go to the branch.
            }
            else
            {
                st.SetDestination(theDungeon.FindFloor(8));
            }
        }
        foreach (Stairs st in theDungeon.FindFloor(138).mapStairs) // Fungal caverns
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(PREBOSS2_MAP_FLOOR)); // Set ingame floor 5 to go to the branch.
                break;
            }
        }
        foreach (Stairs st in theDungeon.FindFloor(PREBOSS2_MAP_FLOOR).mapStairs) // Preboss area
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(BOSS2_MAP_FLOOR)); // Boss fight
            }
            else
            {
                if (st.pointsToFloor == 9)
                {
                    st.SetDestination(theDungeon.FindFloor(9)); // Dungeon floor
                }
                else
                {
                    st.SetDestination(theDungeon.FindFloor(138)); // Dungeon floor
                }

            }
        }
        foreach (Stairs st in theDungeon.FindFloor(BOSS2_MAP_FLOOR).mapStairs) // Preboss area
        {
            if (st.stairsUp == false) // Stairs down?
            {
                st.SetDestination(theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS2));
            }
            else
            {
                st.SetDestination(theDungeon.FindFloor(PREBOSS2_MAP_FLOOR));
            }
        }

        // Boss1 stairs

        // Do special level connections.
        foreach (Map sMap in specialMaps)
        {
            // Mirrored / alt branches to the main dungeon.
            if (sMap.dungeonLevelData.altPath > 0)
            {

                sMap.effectiveFloor = sMap.dungeonLevelData.altPath;
                foreach (Stairs st in sMap.mapStairs)
                {
                    if (st.stairsUp)
                    {
                        //Debug.Log("Stairs up in map " + sMap.floor + " set to level " + sMap.dungeonLevelData.stairsUpToLevel);
                        st.NewLocation = theDungeon.FindFloor(sMap.dungeonLevelData.stairsUpToLevel);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else
                    {
                        //Debug.Log("Stairs down in map " + sMap.floor + " set to level " + sMap.dungeonLevelData.stairsDownToLevel);
                        st.NewLocation = theDungeon.FindFloor(sMap.dungeonLevelData.stairsDownToLevel);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                }
                continue;
            }

            if (sMap.floor == BRANCH_PASSAGE_POSTBOSS1) // Branched path selection #1
            {
                foreach (Stairs st in sMap.mapStairs)
                {
                    if (st.pointsToFloor == 6) // Main dungeon
                    {
                        st.NewLocation = theDungeon.FindFloor(6);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else if (st.pointsToFloor == 135) // To branched path
                    {
                        st.NewLocation = theDungeon.FindFloor(135);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else if (st.pointsToFloor == 5) // Back to bandits
                    {
                        st.NewLocation = theDungeon.FindFloor(5);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                }
                continue;
            }

            if (sMap.floor == BRANCH_PASSAGE_POSTBOSS2) // Branched path selection #2
            {
                foreach (Stairs st in sMap.mapStairs)
                {
                    if (st.pointsToFloor == 11) // Main dungeon
                    {
                        st.NewLocation = theDungeon.FindFloor(11);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else if (st.pointsToFloor == 151) // To branched path
                    {
                        st.NewLocation = theDungeon.FindFloor(151);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else if (st.pointsToFloor == BOSS2_MAP_FLOOR) // Back to spirits
                    {
                        st.NewLocation = theDungeon.FindFloor(BOSS2_MAP_FLOOR);
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                }
                continue;
            }
        }
        // End special connections.

        // Connect side area spawns

        foreach (Map sMap in specialMaps)
        {
            if (sMap.floor == FINAL_BOSS_FLOOR2 || sMap.floor == FINAL_BOSS_FLOOR2_OLD)
            {
                continue;
            }

            if (sMap.dungeonLevelData.dontConnectToAnything) continue;

            Map getMap = null;

            if (sMap.floor == BRANCH_PASSAGE_POSTBOSS1 || sMap.floor == BRANCH_PASSAGE_POSTBOSS2) // Branched path selectors, don't connect to this normally.
            {
                continue;
            }

            if (sMap.floor == PREBOSS1_MAP_FLOOR || sMap.floor == PREBOSS2_MAP_FLOOR || sMap.floor == BOSS2_MAP_FLOOR)
            {
                continue;
            }

            if (sMap.dungeonLevelData == null)
            {
                Debug.Log("Error: " + sMap.floor + " no DLD?");
            }

            if (sMap.floor == TUTORIAL_FLOOR) continue;


            if (sMap.dungeonLevelData.stairsUpToLevel == 0)
            {
                if (!FindConnectionMapForSideArea(sMap, true, out getMap))
                {
                    continue;
                }
            }
            else
            {
                // We're linking UP to another level. So, we should put stairs down TO this level.
                getMap = theDungeon.FindFloor(sMap.dungeonLevelData.stairsUpToLevel);
                if (getMap == null)
                {
                    Debug.Log("Couldn't find parent (upper level) of map " + sMap.dungeonLevelData.customName + " " + sMap.floor);
                    continue;
                }
                else
                {
                    //Debug.Log("Parent (upper level) of map " + sMap.dungeonLevelData.customName + " is " + getMap.dungeonLevelData.customName);
                }

            }

            // Getmap is the floor that will link to THIS special area.

            if (getMap.floor != BRANCH_PASSAGE_POSTBOSS1 && getMap.floor != BRANCH_PASSAGE_POSTBOSS2)
            {
                // Don't spawn stairs in branch connector. 

                bool foundStairsDown = false;

                foreach (Stairs st in getMap.mapStairs)
                {
                    if ((st.NewLocation != null && st.NewLocation.floor == sMap.floor) || st.pointsToFloor == sMap.floor)
                    {
                        foundStairsDown = true;
                        break;
                    }
                }

                if (!foundStairsDown)
                {
                    // Spawn stairs in the CONNECTING floor, not the process floor.
                    Stairs newStairsDown = getMap.SpawnStairs(false);
                    newStairsDown.NewLocation = sMap;
                    newStairsDown.newLocationID = sMap.mapAreaID;
                }
            }

            // We're on B2F (sMap). We just created stairs down ON B1F (getMap).
            // Now we need to create stairs UP from this level (sMap) to getMap.


            Stairs specialAreaReturnStairs;

            if (sMap.mapStairs.Count == 0)
            {
                Debug.Log("WARNING: " + sMap.floor + " has no stairs to return to " + getMap.floor);
            }
            specialAreaReturnStairs = sMap.mapStairs[0];

            foreach (Stairs st in sMap.mapStairs)
            {
                //Debug.Log("Stairs in map " + sMap.floor + " points to floor: " + st.pointsToFloor);
                if (st.pointsToFloor == -1) // Unused stairs.
                {
                    //Debug.Log("USE the stairs at " + st.currentPosition + " pointing to " + st.pointsToFloor);
                    specialAreaReturnStairs = st;
                    break;
                }
            }
            //test
            if (specialAreaReturnStairs.pointsToFloor >= 0 && getMap.floor != specialAreaReturnStairs.pointsToFloor)
            {
                //Debug.Log("Trying to set stairs with PTF " + specialAreaReturnStairs.pointsToFloor + " to " + getMap.floor + ", skipping.");
                continue;
            }

            if (sMap.dungeonLevelData.altPath == 0)
            {
                sMap.effectiveFloor = getMap.effectiveFloor + 1;
                //Debug.Log("3check " + sMap.floor + " effective is " + (getMap.effectiveFloor + 1));
            }

            specialAreaReturnStairs.NewLocation = getMap;
            specialAreaReturnStairs.newLocationID = getMap.mapAreaID;

            //Debug.Log("Check! Special area " + sMap.dungeonLevelData.customName + " " + sMap.floor + " has stairs UP located at " + specialAreaReturnStairs.GetPos() + " now pointing to floor " + specialAreaReturnStairs.newLocation.floor);
        }


        // Special quest stuff for generated maps.

        foreach (Map m in theDungeon.maps)
        {
            foreach (Stairs st in m.mapStairs) // Why is this extra  check necessary? On some side maps, we're getting solid terrain over stairs.
            {
                MapTileData mtd = m.GetTile(st.GetPos());
                mtd.ChangeTileType(TileTypes.GROUND, m.mgd);
                mtd.RemoveTag(LocationTags.WATER);
                mtd.RemoveTag(LocationTags.LAVA);
                mtd.RemoveTag(LocationTags.ELECTRIC);
                mtd.RemoveTag(LocationTags.SOLIDTERRAIN);
                mtd.RemoveTag(LocationTags.MUD);
            }

            // Create monsters, destructibles, or NPCs that are spawned at random outside map generation code
            // This could include quest NPCs for example
            foreach (ActorSpawnData nsd in m.dungeonLevelData.spawnActors)
            {
                if (UnityEngine.Random.Range(0, 1f) <= nsd.spawnChance)
                {
                    // NPCs might spawn only if the player has (or has not) met certain flag criteria
                    // For example, making an NPC appear on only ONE playthrough until a quest is complete
                    // And then never again
                    if (!string.IsNullOrEmpty(nsd.metaFlag))
                    {
                        int currentFlagValue = MetaProgressScript.ReadMetaProgress(nsd.metaFlag);
                        if (currentFlagValue < nsd.flagMinValue || currentFlagValue > nsd.flagMaxValue)
                        {
                            continue;
                        }
                    }
                    Actor spawnedActor = null;
                    if (nsd.aType == ActorTypes.NPC)
                    {
                        spawnedActor = NPC.CreateNPC(nsd.refName);
                    }
                    else if (nsd.aType == ActorTypes.MONSTER)
                    {
                        spawnedActor = MonsterManagerScript.CreateMonster(nsd.refName, true, true, false, 0f, 0f, false);
                    }
                    else if (nsd.aType == ActorTypes.DESTRUCTIBLE)
                    {
                        spawnedActor = Destructible.FindTemplate(nsd.refName);
                    }
                    if (spawnedActor == null)
                    {
                        Debug.Log("Could not spawn " + nsd.refName + " in map " + m.floor + ": template not found.");
                        continue;
                    }

                    Stairs startStairs = null;
                    foreach (Stairs st in m.mapStairs)
                    {
                        if (st.stairsUp)
                        {
                            startStairs = st;
                            break;
                        }
                    }
                    if (startStairs == null)
                    {
                        Debug.Log("Couldn't find any stairs up to place NPC " + nsd.refName + " on floor " + m.floor);
                        continue;
                    }
                    bool foundNPCTile = false;
                    MapTileData npcTile = null;

                    if (nsd.maxDistanceFromStairsUp <= 4)
                    {
                        List<MapTileData> possibleSpawnTiles = m.GetListOfTilesAroundPoint(startStairs.GetPos(), nsd.maxDistanceFromStairsUp);
                        possibleSpawnTiles.Shuffle();
                        foreach (MapTileData mtd in possibleSpawnTiles)
                        {
                            if ((mtd.IsEmpty()) && (mtd.tileType == TileTypes.GROUND))
                            {
                                float dist = GetGridDistance(mtd.pos, startStairs.GetPos());
                                if (dist >= nsd.minDistanceFromStairsUp && dist <= nsd.maxDistanceFromStairsUp)
                                {
                                    foundNPCTile = true;
                                    npcTile = mtd;
                                    break;
                                }
                            }
                        }
                    }

                    if (!foundNPCTile)
                    {
                        // #todo This does not take into account spawn distance... why?
                        int x = UnityEngine.Random.Range(1, m.columns - 1);
                        int y = UnityEngine.Random.Range(1, m.rows - 1);
                        MapTileData checkMTD = null;
                        int tries = 0;
                        int triesAtCurrentDistance = 0;

                        int localMin = nsd.minDistanceFromStairsUp;
                        int localMax = nsd.maxDistanceFromStairsUp;

                        while (!foundNPCTile)
                        {
                            tries++;

                            if (triesAtCurrentDistance >= 10)
                            {
                                localMax++;
                                localMin--;
                                triesAtCurrentDistance = 0;
                            }

                            if (tries > 2500)
                            {
                                Debug.Log("Broke find loop for " + spawnedActor.actorRefName + " floor " + m.floor);
                                break;
                            }
                            x = UnityEngine.Random.Range(1, m.columns - 1);
                            y = UnityEngine.Random.Range(1, m.rows - 1);
                            checkMTD = m.mapArray[x, y];
                            if (checkMTD.IsEmpty() && checkMTD.tileType == TileTypes.GROUND)
                            {
                                int dist = MapMasterScript.GetGridDistance(checkMTD.pos, startStairs.GetPos());
                                if (dist < localMin || dist > localMax)
                                {
                                    triesAtCurrentDistance++;
                                    continue;

                                }
                                npcTile = checkMTD;
                                foundNPCTile = true;
                                break;
                            }

                        }
                    }


                    if (foundNPCTile)
                    {
                        if ((nsd.aType == ActorTypes.NPC) || (nsd.aType == ActorTypes.MONSTER))
                        {
                            spawnedActor.SetPos(npcTile.pos);
                            m.PlaceActor(spawnedActor, npcTile);
                        }
                        else
                        {
                            m.CreateDestructibleInTile(npcTile, nsd.refName);
                        }

                    }
                    else
                    {
                        Debug.Log("Could not find spawn point for " + spawnedActor.actorRefName + " map " + m.floor);
                    }
                }
            }
        }

        // Hide maps, Hide Stairs, map hide, map stairs, stairs hide
        Map startFloor = theDungeon.FindFloor(0);

        foreach (Stairs st in startFloor.mapStairs)
        {
            if (st.stairsUp == true) // Stairs up?
            {
                st.NewLocation = townMap;
                st.newLocationID = townMap.mapAreaID;
            }
        }
        foreach (Stairs st in townMap.mapStairs)
        {
            if (!st.stairsUp) // Stairs to tangledeep
            {
                st.NewLocation = startFloor;
                st.newLocationID = startFloor.mapAreaID;
            }
            else
            {
                st.NewLocation = townMap2;
                st.newLocationID = townMap2.mapAreaID;
            }
        }

        foreach (Stairs st in townMap2.mapStairs)
        {
            if (!st.stairsUp) // Stairs to camp
            {
                st.NewLocation = townMap;
                st.newLocationID = townMap.mapAreaID;
            }
        }

        // Finish linking stairs.

        List<MapTileData> surrounding = new List<MapTileData>();
        foreach (Map m in dictAllMaps.Values)
        {
            m.RelocateMonstersFromStairs();
            foreach (Stairs s in m.mapStairs)
            {
                if (s.NewLocation == null)
                {
                    // Try to link these to a specific floor if possible.

                    if (s.pointsToFloor >= 0)
                    {
                        s.NewLocation = theDungeon.FindFloor(s.pointsToFloor);
                    }
                    else
                    {
                        //Debug.Log(m.dungeonLevelData.deepSideAreaFloor + " " + s.stairsUp + " " + m.dungeonLevelData.stairsDownToLevel + " " + m.dungeonLevelData.stairsDownToModLevelID);
                        if (!s.stairsUp && m.dungeonLevelData.stairsDownToLevel >= 0)
                        {
                            s.NewLocation = theDungeon.FindFloor(m.dungeonLevelData.stairsDownToLevel);
                            s.pointsToFloor = m.dungeonLevelData.stairsDownToLevel;
                        }
                        else if (s.stairsUp && m.dungeonLevelData.stairsUpToLevel >= 0)
                        {
                            s.NewLocation = theDungeon.FindFloor(m.dungeonLevelData.stairsUpToLevel);
                            s.pointsToFloor = m.dungeonLevelData.stairsUpToLevel;
                        }
                        else
                        {
                            Debug.Log("Stairs in map " + m.GetName() + " " + m.floor + " " + s.actorUniqueID + " have no new location, and no pointer floor? " + s.pointsToFloor + " " + s.GetPos());
                            continue;
                        }
                    }

                    if (s.NewLocation == null)
                    {
                        Debug.Log("Stairs in map " + m.GetName() + " " + m.floor + " have no new location? " + s.pointsToFloor + " " + s.GetPos() + " link to floor...");
                    }
                }
                //Debug.Log(m.floor + " " + s.stairsUp + " good " + s.newLocation.floor + " " + s.GetPos());
                if (s.NewLocation.mapIsHidden || m.mapIsHidden)
                {
                    s.DisableActor();
                }
            }
        }

        // Misc cleanup.
        Map friendshipForest = theDungeon.FindFloor(ROMANCE_SIDEAREA);
        friendshipForest.SetMapVisibility(false);

        DLC_DungeonGenerationAlgorithms.MakeLevelChangesOnPostCreation();

        float fillAmount = 0.9f;
        //UIManagerScript.FillLoadingBar(fillAmount);

        // Done generating the map
        generatingMap = false;

        // INITIAL MAP STARTING MAP
        activeMap = theDungeon.FindFloor(TOWN_MAP_FLOOR); // WAS floor 0 at first.
        bool sharaMode = false;
        // Unless it's Shara mode...
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            sharaMode = true;
            if (MetaProgressScript.ReadMetaProgress("shara_start") != 1)
            {
                activeMap = theDungeon.FindFloor(SHARA_START_FOREST_FLOOR);
                GameMasterScript.gmsSingleton.UpdateHeroObject("ChildShara");
            }
            else
            {
                // start at Shara home base
                activeMap = theDungeon.FindFloor(SHARA_START_CAMPFIRE_FLOOR);
            }
        }

        mapLoaded = true;
        GameMasterScript.cameraScript.SetBGColor(activeMap.dungeonLevelData.bgColor);

        DoSpeedrunningChangesIfNecessary();
        CheckForAndFixDeprecatedAreasAndConnections();
        DLCManager.MakeDLCStairConnectionsIfNeeded();

        traversed = new bool[activeMap.rows, activeMap.columns];
        tChecked = new bool[activeMap.rows, activeMap.columns];
        SpawnMapOverlays();
        CheckRevealMap();

        mapGridMesh.size_x = activeMap.columns;
        mapGridMesh.size_z = activeMap.rows;

        mapTileMesh.size_x = activeMap.columns;
        mapTileMesh.size_z = activeMap.rows;
        //SpawnAllDoors(activeMap);
        // Spawn stair GameObjects        
        SpawnAllDecor(activeMap);
        SpawnAllProps(activeMap);
        SpawnAllItems(activeMap);

        // Spawn some monsters to fight. UNCOMMENT BELOW
        SpawnAllMonsters(activeMap);
        SpawnAllNPCs(activeMap);
        SpawnAllStairs(activeMap);

        // Add hero to starting tile

        Vector2 newPos = new Vector2(9f, 13f); // GAME START POSITION - HARDCODED START SPAWN, HERO START, HERO SPAWN, PLAYER START, PLAYER SPAWN
        // But if it's shara tho?
        if (sharaMode)
        {
            newPos = activeMap.heroStartTile; // Set in the specialroom definition
        }

        heroPCActor.SetCurPos(newPos);
        cameraScript.SnapPosition(newPos);

        AddActorToLocation(newPos, GameMasterScript.heroPCActor);
        AddActorToMap(GameMasterScript.heroPCActor);
        if (sharaMode)
        {
            try { UpdateFOWOverlay(true); }  //#questionable_try_block
            catch
            {
                Debug.Log("Error updating FOW overlay. " + activeMap.floor);
            }
        }
        UpdateMapObjectData();
        mapGridMesh.BuildMesh();
        mapTileMesh.BuildMesh();
        fogOfWarTMG.size_x = activeMap.columns;
        fogOfWarTMG.size_z = activeMap.rows;
        fogOfWarTMG.BuildMesh();
        MinimapUIScript.BaseMapChanged(activeMap.columns, activeMap.rows);
        MinimapUIScript.RefreshMiniMap();
        if (!sharaMode)
        {
        try { UpdateFOWOverlay(true); }  //#questionable_try_block
        catch
        {
            Debug.Log("Error updating FOW overlay. " + activeMap.floor);
            }
        }


        UpdateSpriteRenderersOnLoad(); // Why are sprite renderers being disabled by default?
        gms.MapCreationFinished();
    }

    public static void CheckForAndFixDeprecatedAreasAndConnections()
    {
        bool sharaMode = false;
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            sharaMode = true;
        }

        // Now go through and connect any references to the OLD golem fight, which is floor "15", to the new floor.
        // Basically, we are deprecating level 15.

        Map boss3Floor = null;
        Map preBoss3MeetSharaFloor = null;
        foreach (Map m in dictAllMaps.Values)
        {
            if (m.floor == PRE_BOSS3_MEETSHARA_MAP_FLOOR)
            {
                preBoss3MeetSharaFloor = m;
                // Let's also make sure these stairs go back to previous floors, as intended.
                for (int i = 0; i < preBoss3MeetSharaFloor.mapStairs.Count; i++)
                {
                    Stairs st = preBoss3MeetSharaFloor.mapStairs[i];
                    if (sharaMode && st.pointsToFloor == MapMasterScript.BOSS3_MAP_FLOOR)
                    {
                        continue;
                    }
                    if (i == 0)
                    {
                        st.NewLocation = theDungeon.FindFloor(14);
                        st.pointsToFloor = 14;
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                    else
                    {
                        st.NewLocation = theDungeon.FindFloor(154);
                        st.pointsToFloor = 154;
                        st.newLocationID = st.NewLocation.mapAreaID;
                    }
                }
            }
            if (m.floor == BOSS3_MAP_FLOOR)
            {
                boss3Floor = m;
            }
        }
        if (boss3Floor == null)
        {
            boss3Floor = theDungeon.FindFloor(BOSS3_MAP_FLOOR);
            if (boss3Floor == null)
            {
                Debug.Log("<color=red>No boss 3 floor exists...?</color>");
            }
            return;
        }
        if (preBoss3MeetSharaFloor == null)
        {
            preBoss3MeetSharaFloor = theDungeon.FindFloor(PRE_BOSS3_MEETSHARA_MAP_FLOOR);
            if (preBoss3MeetSharaFloor == null)
            {
                Debug.Log("<color=red>No pre-boss 3 floor exists...?</color>");
            }
        }

        foreach (Map m in dictAllMaps.Values)
        {
            foreach (Stairs st in m.mapStairs)
            {
                if (st.pointsToFloor == 15 || (st.NewLocation != null && st.NewLocation.floor == 15))
                {
                    Map mapToChangeTo = preBoss3MeetSharaFloor;
                    if (GameMasterScript.createdHeroPCActor && GameMasterScript.heroPCActor.ReadActorData("boss3fight") >= 1)
                    {
                        mapToChangeTo = boss3Floor;
                    }
                    st.NewLocation = mapToChangeTo;
                    st.newLocationID = st.NewLocation.mapAreaID;
                    st.pointsToFloor = mapToChangeTo.floor;
                }
            }
        }

        // In the boss2 room, make sure stairs lead to the appropriate zones
        Map boss2fight = MapMasterScript.theDungeon.FindFloor(BOSS2_MAP_FLOOR);
        foreach (Stairs st in boss2fight.mapStairs)
        {
            if (st.stairsUp)
            {
                st.pointsToFloor = PREBOSS2_MAP_FLOOR;
                st.NewLocation = MapMasterScript.theDungeon.FindFloor(PREBOSS2_MAP_FLOOR);
            }
            else
            {
                st.pointsToFloor = BRANCH_PASSAGE_POSTBOSS2;
                st.NewLocation = MapMasterScript.theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS2);
            }
        }

    }

    public static void TryInitializeRoomDicts()
    {
        if (masterDictOfEnforcedMapRoomDicts == null)
        {
            masterDictOfEnforcedMapRoomDicts = new Dictionary<int, List<RoomTemplate>[]>();
            masterDictOfUnenforcedMapRoomDicts = new Dictionary<int, List<RoomTemplate>[]>();
            pointsForRoomDicts = new Dictionary<int, MapPoint[]>();
            pointsForRoomDictsMaxSizes = new Dictionary<int, MapPoint>();
        }
    }

    public void MapStart(bool initMap)
    {
        BattleTextManager.InitializeTextObjectSources();

        if (initMap)
        {
            GameMasterScript.gmsSingleton.SetTempFloatData("mapgenstart", Time.realtimeSinceStartup);
            //masterDictOfEnforcedMapRoomDicts = new Dictionary<int, Dictionary<MapPoint, List<RoomTemplate>>>();
            //masterDictOfUnenforcedMapRoomDicts = new Dictionary<int, Dictionary<MapPoint, List<RoomTemplate>>>();

            masterDictOfEnforcedMapRoomDicts = new Dictionary<int, List<RoomTemplate>[]>();
            masterDictOfUnenforcedMapRoomDicts = new Dictionary<int, List<RoomTemplate>[]>();

            pointsForRoomDicts = new Dictionary<int, MapPoint[]>();
            pointsForRoomDictsMaxSizes = new Dictionary<int, MapPoint>();

            generatingMap = true;
            // Handled in GMS now.
            //UIManagerScript.ToggleLoadingBar();

            // For initial map gen.
            activeNonTileGameObjects = new List<GameObject>();

            heroPCActor = GameMasterScript.heroPCActor;
            heroPC = GameMasterScript.heroPC;
            renderColumns = uims.renderColumns;
            renderRows = uims.renderRows;
        }

        // HARDCODED DUNGEON FLOOR INFO - Should tweak this!!!
        if (initMap)
        {
            DungeonInfo di = new DungeonInfo(25, 1.0f, 0.1f); // HARDCODED Max floors. Number of floors Dungeon floors
            /* string txt = "Building dungeon floor 1...";
            UIManagerScript.WriteLoadingText(txt); */
            StartCoroutine(MapLoadingWaiter(-1, di));
            //CreateNewDungeon(5, 1.0f, 0.15f, true, 0); // Number of floors, difficulty scaling. This should be published in the editor tho.
            return;
        }

        // The basic dungeon floors have been built.

        // Create separate scenes
        townMap = null;
        townMap2 = null;


        ContinueDungeonGeneration(0);
        return;
    }

    // Has the game been updated with more maps since last start? Let's check
    public static void BuildUnusedMaps()
    {
        List<Map> newlyAddedMaps = new List<Map>();
        foreach (DungeonLevel dl in GameMasterScript.masterDungeonLevelList.Values)
        {
            if (dl.itemWorld) continue;
            if (!dl.sideArea) continue;
            bool foundMap = false;
            foreach (Map m in dictAllMaps.Values)
            {
                if (m.IsItemWorld()) continue;
                if (m.IsMainPath()) continue;
                if (dl.floor == m.floor)
                {
                    foundMap = true;
                    break;
                }
            }
            if (foundMap) continue;
            // OK, DL has not been built, let's build it now and add it to dict.
            Map nMap = null;
            try { nMap = singletonMMS.CreateNewMap(false, dl.floor, 1, dl.challengeValue, dl, new ItemWorldMetaData()); }
            catch (Exception e)
            {
                Debug.Log("Failed to make map floor " + dl.floor + " due to " + e);
            }
            if (nMap != null)
            {
                theDungeon.maps.Add(nMap);
                newlyAddedMaps.Add(nMap);
                //Debug.Log("Created map " + nMap.floor + " (" + dl.customName + ") and added to dict!");
            }
        }

        // Connect these new maps to the rest of the dungeon, and each other, as needed.
        foreach (Map m in newlyAddedMaps)
        {
            if (m.dungeonLevelData.dontConnectToAnything) continue;
            if (m.dungeonLevelData.deepSideAreaFloor)
            {
                // This map has defined connections in the DungeonLevelData
                foreach (Stairs st in m.mapStairs)
                {
                    if (st.stairsUp && st.pointsToFloor <= 0)
                    {
                        st.NewLocation = theDungeon.FindFloor(m.dungeonLevelData.stairsUpToLevel);
                        st.pointsToFloor = m.dungeonLevelData.stairsUpToLevel;
                    }
                    else if (st.pointsToFloor <= 0)
                    {
                        st.NewLocation = theDungeon.FindFloor(m.dungeonLevelData.stairsDownToLevel);
                        st.pointsToFloor = m.dungeonLevelData.stairsDownToLevel;
                    }
                }
            }
            else
            {
                // Connect this area to the rest of the dungeon.
                int minSpawnFloor = 0;
                int maxSpawnFloor = 20;
                int spawnFloor = 0;
                if (m.dungeonLevelData.minSpawnFloor < 99 && m.dungeonLevelData.maxSpawnFloor < 99 && m.dungeonLevelData.minSpawnFloor != m.dungeonLevelData.maxSpawnFloor)
                {
                    minSpawnFloor = m.dungeonLevelData.minSpawnFloor;
                    maxSpawnFloor = m.dungeonLevelData.maxSpawnFloor;
                }
                spawnFloor = UnityEngine.Random.Range(minSpawnFloor, maxSpawnFloor + 1);
                Map connectionMap = theDungeon.FindFloor(spawnFloor);
                int attempts = 0;
                while (connectionMap.IsBossFloor())
                {
                    attempts++;
                    if (attempts > 100)
                    {
                        Debug.Log("Could NOT find a non-boss floor between " + minSpawnFloor + " and " + maxSpawnFloor + " for map " + m.dungeonLevelData.floor + "?");
                        break;
                    }
                    spawnFloor = UnityEngine.Random.Range(minSpawnFloor, maxSpawnFloor + 1);
                    connectionMap = theDungeon.FindFloor(spawnFloor);
                }

                foreach (Stairs st in m.mapStairs)
                {
                    if (!st.stairsUp && st.pointsToFloor <= 0)
                    {
                        st.NewLocation = theDungeon.FindFloor(m.dungeonLevelData.stairsDownToLevel);
                        st.pointsToFloor = m.dungeonLevelData.stairsDownToLevel;
                    }
                    else
                    {
                        st.NewLocation = connectionMap;
                        st.pointsToFloor = connectionMap.floor;
                    }
                }

                // Now spawn stairs in the connection map

                Stairs connectionSt = connectionMap.SpawnStairs(false, m.floor);
                Debug.Log("Spawned connection stairs to " + m.floor + " by way of " + connectionMap.floor + " at " + connectionSt.GetPos());
            }
        }
    }

    public static void RebuildUnvisitedMaps()
    {
        List<int> exploredMapIDs = new List<int>();
#if UNITY_SWITCH
        string path = "savedGame" + GameStartData.saveGameSlot + ".xml";
        var sdh = Switch_SaveDataHandler.GetInstance();
        if (!sdh.CheckIfSwitchFileExists(path))
#elif UNITY_PS4
        string path = "savedGame" + GameStartData.saveGameSlot + ".xml";
        byte[] loadedData = null;       
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, path))
#elif UNITY_XBOXONE
        string path = "savedGame" + GameStartData.saveGameSlot + ".xml";
        if (!XboxSaveManager.instance.HasKey(path))
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
        if (!File.Exists(path))
#endif
        {
            Debug.Log("No game to load.");
            return;
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;
#if UNITY_SWITCH
        string strLoadedData = "";
        sdh.LoadSwitchDataFile(ref strLoadedData, path);
        XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#elif UNITY_PS4
        string strLoadedData = "";       
        PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, path, out loadedData);
        //convert byte to String
        strLoadedData = System.Text.Encoding.UTF8.GetString(loadedData);
               
        XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#elif UNITY_XBOXONE
        string strLoadedData = "";

        strLoadedData = XboxSaveManager.instance.GetString(path);

        XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#else
        FileStream stream = new FileStream(path, FileMode.Open);
        XmlReader reader = XmlReader.Create(stream, settings);
#endif
        while (reader.Read())
        {
            if (reader.Name == "exploredmaps")
            {
                reader.ReadStartElement();

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if ((reader.NodeType == XmlNodeType.None) || (reader.NodeType == XmlNodeType.Whitespace))
                    {
                        continue;
                    }

                    int mapID = reader.ReadElementContentAsInt();
                    exploredMapIDs.Add(mapID);
                }

                reader.ReadEndElement();
                break;
            }
        }

        reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        stream.Close();
#endif
    }

    public static Map CreateMap(int floor, bool skipToFloorImmediately = false)
    {
        //Debug.Log("<color=green>Requesting creation of new floor ID: " + floor + "</color>");
        int floorIDOfMapFromXML = floor;

        var dlData = GameMasterScript.masterDungeonLevelList[floorIDOfMapFromXML];

        var newMap = singletonMMS.CreateNewMap(skipToFloorImmediately, floorIDOfMapFromXML, 1, 1, dlData, null);

        theDungeon.RemoveMapByFloor(floorIDOfMapFromXML);
        theDungeon.maps.Add(newMap);

#if UNITY_EDITOR
        //Debug.Log("Created new map of floor " + floor + " with ID " + newMap.mapAreaID);
#endif
        // I don't think we have to do this, because CreateNewMap already adds the map to dict!

        if (dictAllMaps.ContainsKey(newMap.mapAreaID)) // was floorIDOfMapFromXML
        {
#if UNITY_EDITOR
            Debug.Log("New map " + newMap.GetName() + " " + newMap.floor + " is overwriting " + dictAllMaps[newMap.mapAreaID].floor + " " + dictAllMaps[newMap.mapAreaID].GetName());
#endif
            dictAllMaps[newMap.mapAreaID] = newMap;
            
        }
        else
        {
            dictAllMaps.Add(newMap.mapAreaID, newMap); // was floorIDOfMapFromXML
#if UNITY_EDITOR
            Debug.Log("New map " + newMap.GetName() + " " + newMap.floor + " is safely in the dict.");
#endif
        }

#if UNITY_EDITOR
        string retString = "<color=green>Created map with floor ID " + floorIDOfMapFromXML + " called " + newMap.GetName() + " which is actually floor " + floor + "!</color>";
        GameLogScript.GameLogWrite(retString, null);
        //Debug.Log(retString);
        GameLogScript.GameLogWrite(retString, null);
#endif

        if (skipToFloorImmediately)
        {
            singletonMMS.SwitchMaps(newMap, Vector2.zero, true);
        }
        return newMap;
    }

    public static bool FindConnectionMapForSideArea(Map sMap, bool allowHideMap, out Map getMap)
    {
        // Start of a special map zone, or a one-off
        int lowestStairs = 99;
        Map bestOption = null;
        
        for (int x = 0; x < theDungeon.maps.Count; x++)
        {
            int evaluateFloor = 0;

            if (theDungeon.maps[x].floor == BRANCH_PASSAGE_POSTBOSS1 || theDungeon.maps[x].floor == BRANCH_PASSAGE_POSTBOSS2) continue; // Don't touch branch connectors

            if (theDungeon.maps[x].IsMainPath())
            {
                evaluateFloor = theDungeon.maps[x].effectiveFloor;
            }
            if (evaluateFloor < sMap.dungeonLevelData.minSpawnFloor || evaluateFloor > sMap.dungeonLevelData.maxSpawnFloor)
            {
                continue;
            }
            if (theDungeon.maps[x].dungeonLevelData.specialRoomTemplate != null) continue;

            if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) < 1 &&
                theDungeon.maps[x].floor >= RIVERSTONE_WATERWAY_START && theDungeon.maps[x].floor <= RIVERSTONE_WATERWAY_END)
            {
                // This is a riverstone waterway map, but we don't have access to the waterway yet. Don't use it.
                continue;
            }

            if (RandomJobMode.IsCurrentGameInRandomJobMode() && !RandomJobMode.IsFloorEnabled(theDungeon.maps[x].floor))
            {
                continue;
            }
            
            if (theDungeon.maps[x].IsMainPath())
            {
                int rollStairs = theDungeon.maps[x].mapStairs.Count + UnityEngine.Random.Range(-1, 2);
                if (rollStairs < lowestStairs)
                {
                    bestOption = theDungeon.maps[x];
                    lowestStairs = theDungeon.maps[x].mapStairs.Count;
                }
            }
        }

        if (bestOption == null)
        {
            getMap = null;
            return false;
        }

        getMap = bestOption;
        if (sMap.effectiveFloor == 0)
        {
            sMap.effectiveFloor = getMap.effectiveFloor + 1;
        }
        //Debug.Log(sMap.floor + " 2check is now " + sMap.effectiveFloor);
        if (sMap.effectiveFloor > 1 && allowHideMap)
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.45f)
            {
                //Debug.Log(sMap.GetName() + " is hidden.");
                sMap.mapIsHidden = true;
            }
        }

        return true;
    }
}