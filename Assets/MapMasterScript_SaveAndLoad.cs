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

    public bool TryRelinkStairs(Stairs st)
    {
        bool relinkSuccessful = false;
        if (st.GetActorMap() != null)
        {
#if UNITY_EDITOR
            if (st.GetActorMap().floor != 222 && st.GetActorMap().floor != 227)
            {
                Debug.Log("Trying to relink stairs in map " + st.GetActorMap().floor + " which point to floor " + st.pointsToFloor);
            }
#endif

            if (st.GetActorMap().IsItemWorld())
            {
                if (!st.stairsUp)
                {
                    // Returning to previous level of item world.
                    for (int i = 0; i < itemWorldMaps.Length; i++)
                    {
                        if (itemWorldMaps[i] == st.GetActorMap())
                        {
                            Debug.Log("Stairs DOWN are in item world map floor " + itemWorldMaps[i].floor + " map id " + itemWorldMaps[i].mapAreaID + " index " + i);
                            if (i > 0)
                            {
                                st.NewLocation = itemWorldMaps[i - 1];
                                st.newLocationID = st.NewLocation.mapAreaID;
                                Debug.Log(st.actorRefName + " on floor " + st.dungeonFloor + " had null newlocation in Item World, but we relinked it.");
                                relinkSuccessful = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Going to NEXT level of item world.
                    for (int i = 0; i < itemWorldMaps.Length; i++)
                    {
                        if (itemWorldMaps[i] == st.GetActorMap())
                        {
                            Debug.Log("Stairs UP are in item world map floor " + itemWorldMaps[i].floor + " map id " + itemWorldMaps[i].mapAreaID + " index " + i);
                            if (i < itemWorldMaps.Length - 1)
                            {
                                st.NewLocation = itemWorldMaps[i + 1];
                                st.newLocationID = st.NewLocation.mapAreaID;
                                Debug.Log(st.actorRefName + " on floor " + st.dungeonFloor + " had null newlocation in Item World, but we relinked it.");
                                relinkSuccessful = true;
                                break;
                            }
                        }
                    }
                }

            }
        }
        if (!relinkSuccessful)
        {
            if (st.dungeonFloor != 222 && st.dungeonFloor != 227 && st.dungeonFloor != CAMPFIRE_FLOOR)
            {
#if UNITY_EDITOR
                Debug.Log("Stairs in " + st.dungeonFloor + " " + st.actorUniqueID + " has no location; not spawning");
#endif
            }

            return false;
        }
        return true;
    }

    public static void TryAssignMap(Actor act, int mapID, bool debug = false)
    {
        Map assignMap;
        if (dictAllMaps.TryGetValue(mapID, out assignMap))
        {
            if (debug) Debug.Log("Assigning " + mapID + " to " + act.actorRefName + " " + act.GetActorType() + " on floor " + act.dungeonFloor);
            act.SetActorMap(assignMap);
            return;
        }
        else
        {
            if (mapID == 1)
            {
                Debug.Log("Map ID is 1 for " + act.actorRefName);
                // Why would mapID ever be 1...? Trying to figure that out.
                if (singletonMMS.townMap != null)
                {
                    Debug.Log("Map ID is 1? Setting to town instead.");
                    assignMap = singletonMMS.townMap;
                    act.SetActorMap(assignMap);
                }
                return;
            }
            if (act.dungeonFloor == 150)
            {
                //Debug.Log("Couldn't connect " + act.actorRefName + " to town map 2, but we'll connect manually.");
                if (MapMasterScript.singletonMMS != null && MapMasterScript.singletonMMS.townMap2 != null)
                {
                    act.SetActorMap(MapMasterScript.singletonMMS.townMap2);
                }
                return;
            }
            foreach (Map m in dictAllMaps.Values)
            {
                if (m.floor == act.dungeonFloor)
                {
                    if (Debug.isDebugBuild) Debug.Log("Could not connect " + act.actorRefName + " ID " + act.actorUniqueID + " type " + act.GetActorType() + " floor " + act.dungeonFloor + " to map ID " + mapID + ". Linking to " + m.mapAreaID + " instead...");
                    act.SetActorMap(m);
                    break;
                }
            }

            if (act.actorRefName != "npc_banker" && act.GetActorMap() == null) // This would occur if that map floor is not in the dictionary at all, such as an orphaned Item World.
            {
                Debug.Log("Could not connect " + act.actorRefName + " floor " + act.dungeonFloor + " to map ID " + mapID + ". There are " + dictAllMaps.Count + " maps in the dictionary.");
            }
        }

        if (debug) Debug.Log("Failed to connect " + act.actorRefName + " to floor " + act.dungeonFloor);
    }

    public IEnumerator WriteToSave()
    {

        bool townMapInDict = false;
        bool townMap2InDict = false;
        foreach (Map map in dictAllMaps.Values)
        {
            if (map == townMap)
            {
                townMapInDict = true;
            }
            if (map == townMap2)
            {
                townMap2InDict = true;
            }
        }
        if (!townMapInDict)
        {
            AddMapToDictAndIncrementMapCounter(townMap);
            Debug.Log("Town was not in map dictionary. Why?");
        }
        if (!townMap2InDict)
        {
            AddMapToDictAndIncrementMapCounter(townMap2);
            Debug.Log("Town2 was not in map dictionary. Why?");
        }

#if UNITY_SWITCH
        var sdh = Switch_SaveDataHandler.GetInstance();
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        MemoryStream saveStream = new MemoryStream();

#elif UNITY_PS4
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        MemoryStream saveStream = new MemoryStream();
#elif UNITY_XBOXONE
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        MemoryStream saveStream = new MemoryStream();
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedMapCopy.dat";
        File.Delete(path);
        Stream saveStream = File.Open(path, FileMode.Create);
#endif
        BinaryWriter writer = new BinaryWriter(saveStream);


        int numCountedIWMaps = 0;
        foreach (Map writeMap in dictAllMaps.Values)
        {
            if (writeMap.IsItemWorld()) numCountedIWMaps++;
        }

        if (itemWorldOpen && numCountedIWMaps == 0)
        {
            if (Debug.isDebugBuild) Debug.Log("MISMATCH: No item worlds in dict, yet item world is considered open.");
            itemWorldOpen = false;
            GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
        }

        int counter = theDungeon.maps.Count;

        if (!itemWorldOpen && numCountedIWMaps > 0)
        {
            if (Debug.isDebugBuild) Debug.Log("Item world is not open, but there are IW maps in dict. Not writing them.");
            counter -= numCountedIWMaps;
        }

        // First two bytes: Number of floors in the dungeon.
        writer.Write(counter);

        // ITEM WORLD STUFF

        // Verify item world maps ARE in dict.


        if (itemWorldOpen)
        {
            if (itemWorldItem == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Item world listed as open, but no item...?");
                itemWorldOpen = false;
                GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
                writer.Write(false);
            }
            else
            {
                writer.Write(true);

                if (GameMasterScript.GAME_BUILD_VERSION >= 113)
                {
                    writer.Write((int)itemWorldItemID);
                    //Debug.Log("Writing item world item ID. " + itemWorldItemID + " from " + itemWorldItem.actorRefName + " as int32 " + GameStartData.loadGameVer);
                }
                else
                {
                    writer.Write((short)itemWorldItemID);
                    //Debug.Log("Writing item world item ID. " + itemWorldItemID + " from " + itemWorldItem.actorRefName + " as int16" + GameStartData.loadGameVer);
                }

                writer.Write((short)itemWorldMaps.Length);
                for (int i = 0; i < itemWorldMaps.Length; i++)
                {
                    writer.Write((short)itemWorldMaps[i].mapAreaID);
#if UNITY_EDITOR
                    //Debug.Log("Writing item world map ID: " + itemWorldMaps[i].mapAreaID + " " + itemWorldMaps[i].floor);
#endif
                }
                Debug.Log("Item world is open. Writing " + itemWorldMaps.Length + " maps");
            }
        }
        else
        {
            // Item world is NOT open.
            writer.Write(false);
        }


        // Next two: Number of maps in dict.
        //Debug.Log("Total maps: " + dictAllMaps.Values.Count);
        writer.Write(dictAllMaps.Values.Count);

        // Now... Write all maps in the dictionary.
#if UNITY_EDITOR
        //Debug.Log("Saving " + dictAllMaps.Values.Count + " maps");
        /* foreach(var kvp in dictAllMaps)
        {
            if (kvp.Key < 100) continue;
            Debug.Log("Saving map ID " + kvp.Key + " " + kvp.Value.GetName() + " " + kvp.Value.floor);
        } */
#endif

        int numLocTags = (int)LocationTags.COUNT;
        byte[] tagArray = new byte[numLocTags];
        short[] fourShorts = new short[4];
        byte[] eightByteArray = new byte[8];
        bool[] twoBools = new bool[2];
        byte[] twoByteArray = new byte[2];
        byte[] threeByteArray = new byte[3];
        byte[] fourByteArray = new byte[4];
        byte[] sevenByteArray = new byte[7];
        BinaryFormatter bf = new BinaryFormatter();

        float timeSaveStart = Time.realtimeSinceStartup;

        int index = 0;
        foreach (Map writeMap in dictAllMaps.Values)
        {
            if (writeMap.IsItemWorld() && !itemWorldOpen)
            {
                if (GameMasterScript.heroPCActor.dungeonFloor != writeMap.floor)
                {
                    continue;
                }
            }

            if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeSaveStart = Time.realtimeSinceStartup;
            }

            bool debug = false;

            //Debug.Log("Writing map " + writeMap.GetName() + " " + writeMap.floor);

            // Write about the map itself.
            Map writingMap = writeMap;

            // First two bytes: map Area ID
            writer.Write((short)writingMap.mapAreaID);

            if (!writingMap.IsItemWorld())
            {
                writer.Write((short)0);
            }
            else
            {
                writer.Write((short)writingMap.levelDataLink);
            }

            // Next: Map floor
            writer.Write((short)writingMap.floor);

            // Effective floor?
            writer.Write((short)writingMap.effectiveFloor);

            if (writingMap.heroStartTile == Vector2.zero)
            {
                writer.Write(false);
                writer.Write((short)0);
                writer.Write((short)0);
            }
            else
            {
                writer.Write(true);
                writer.Write((short)writingMap.heroStartTile.x);
                writer.Write((short)writingMap.heroStartTile.y);
            }

            // Next EIGHT bytes: challenge rating
            writer.Write((float)writingMap.challengeRating);

            // Next single byte: Is spawner alive?
            writer.Write(writingMap.spawnerAlive);

            // Is the map hidden?
            writer.Write(writingMap.mapIsHidden);

            if (debug) Debug.Log("Writing map " + writingMap.mapAreaID + " fl " + writingMap.floor + " LDL " + writingMap.levelDataLink + " index: " + index + " " + writingMap.columns + "," + writingMap.rows + " CV " + writingMap.challengeRating);
            index++;

            if (writingMap.mgd == null)
            {
                Debug.Log("Data corruption: " + writingMap.floor + " has no MGD?");
            }

            // Next 2 bytes: number of initial spawned monsters
            writer.Write((short)writingMap.initialSpawnedMonsters);

            // Next 2 bytes: max champ mods
            writer.Write((short)writingMap.mgd.maxChampMods);

            // Next 2 bytes: max champions
            writer.Write((short)writingMap.mgd.maxChamps);

            // Next 8 bytes: chance to spawn champion
            writer.Write(writingMap.mgd.chanceToSpawnChampionMonster);

            writer.Write((short)writingMap.areaDictionary.Keys.Count);

            int counter2 = 0;
            foreach (int areaID in writingMap.areaDictionary.Keys)
            {
                Area ar = writingMap.areaDictionary[areaID];

                writer.Write(ar.isCorridor);
                counter2++;
                writer.Write((int)ar.areaID);
                writer.Write((Byte)ar.origin.x); // was short
                writer.Write((Byte)ar.origin.y); // was short

            }

            writer.Write((Byte)writingMap.columns);
            writer.Write((Byte)writingMap.rows);

            for (int x = 0; x < writingMap.columns; x++)
            {
                for (int y = 0; y < writingMap.rows; y++)
                {
                    MapTileData mtd = writingMap.mapArray[x, y];
                    // First byte: Explored?

                    twoBools[0] = writingMap.exploredTiles[x, y];

                    //writer.Write(writingMap.exploredTiles[x, y]);
                    // First byte: Wall tile, true or false?
                    switch (mtd.tileType)
                    {
                        case TileTypes.GROUND:
                            //writer.Write((bool)false);
                            twoBools[1] = false;
                            break;
                        case TileTypes.WALL:
                            //writer.Write((bool)true);
                            twoBools[1] = true;
                            break;
                        case TileTypes.NOTHING:
                            Debug.Log("WTF! " + x + "," + y + " " + writingMap.floor);
                            break;
                    }

                    Buffer.BlockCopy(twoBools, 0, eightByteArray, 0, count: 2);

                    eightByteArray[2] = (byte)mtd.tileVisualSet;
                    //writer.Write((byte)mtd.tileVisualSet); // was short

                    // Break down an int into four bytes...
                    fourByteArray = BitConverter.GetBytes((int)writingMap.areaIDArray[x, y]);
                    eightByteArray[3] = fourByteArray[0];
                    eightByteArray[4] = fourByteArray[1];
                    eightByteArray[5] = fourByteArray[2];
                    eightByteArray[6] = fourByteArray[3];

                    //writer.Write((int)writingMap.areaIDArray[x, y]);

                    // NEW SPACE-SAVING METHOD
                    byte numTags = 0; // was short
                    //charsToWrite.Clear();

                    //if (mtd.tags != null)
                    if (mtd.anyLocationTags)
                    {
                        for (LocationTags i = 0; i < LocationTags.COUNT; i++)
                        {
                            if (mtd.CheckTrueTag(i))
                            {
                                numTags++;
                                //charsToWrite.Add((byte)i);
                            }
                        }
                    }

                    //writer.Write((byte)numTags);
                    eightByteArray[7] = (byte)numTags;

                    writer.Write(eightByteArray);

                    // Using a List<> is expensive, so we can just iterate over the Tags array one more time
                    // And write whatever we have. We already have the count written above.
                    /* foreach (byte c in charsToWrite)
                    {
                        writer.Write(c);
                    } */

                    if (mtd.anyLocationTags)
                    {
                        tagArray = new byte[numTags];
                        int writeIndex = 0;
                        for (int i = 0; i < numLocTags; i++) // numLocTags is LocationTags.COUNT
                        {
                            if (mtd.CheckTrueTag((LocationTags)i))
                            {
                                tagArray[writeIndex] = (byte)i;
                                writeIndex++;
                                //writer.Write((byte)i);
                            }
                        }
                        writer.Write(tagArray);
                    }
                    // END NEW SPACE-SAVING METHOD

                    writer.Write((byte)mtd.visualTileType);

                    // No need to save the sprite, since we just use tile index...

                    fourShorts[0] = (short)mtd.indexOfSpriteInAtlas;
                    fourShorts[1] = (short)mtd.indexOfGrassSpriteInAtlas;
                    fourShorts[2] = (short)mtd.indexOfTerrainSpriteInAtlas;

                    /* writer.Write((short)mtd.indexOfSpriteInAtlas);
                    writer.Write((short)mtd.indexOfGrassSpriteInAtlas);
                    writer.Write((short)mtd.indexOfTerrainSpriteInAtlas);  */
                    if (!mtd.CheckTag(LocationTags.HASDECOR))
                    {
                        //writer.Write((short)-1);
                        fourShorts[3] = (short)-1;
                    }
                    else
                    {
                        //writer.Write((short)mtd.indexOfDecorSpriteInAtlas);
                        fourShorts[3] = (short)mtd.indexOfDecorSpriteInAtlas;
                    }

                    Buffer.BlockCopy(fourShorts, 0, eightByteArray, 0, count: 8);
                    writer.Write(eightByteArray);
                }
            }
        }

#if UNITY_SWITCH
        saveStream.Position = 0;
        sdh.SaveBinarySwitchFile(saveStream.ToArray(), path);
#endif

#if UNITY_PS4
        saveStream.Position = 0;        
        PS4SaveManager.instance.SaveData(PS4SaveManager.ROOT_DIR, path, saveStream.ToArray());
#endif

#if UNITY_XBOXONE
        saveStream.Position = 0;
        XboxSaveManager.instance.saveByte(path, saveStream.ToArray());
        XboxSaveManager.instance.Save();
#endif
        writer.Close();
        saveStream.Close();

        yield break;
    }

    // PostLoadMap
    public IEnumerator PostMapLoadOperations(Map newMap)
    {
        // Sanity check cleared maps to avoid multi-clears

        foreach (Map processMap in dictAllMaps.Values)
        {
            if (processMap.unfriendlyMonsterCount <= 0 && processMap.dungeonLevelData.noSpawner && processMap.dungeonLevelData.sideArea && !processMap.IsTownMap() && !processMap.IsItemWorld() && !processMap.dungeonLevelData.safeArea)
            {
                if (GameMasterScript.heroPCActor.CheckIfMapCleared(processMap)) continue;

                if (!processMap.clearedMap)
                {
                    bool stairsDown = false;
                    foreach (Stairs st in processMap.mapStairs)
                    {
                        if (!st.stairsUp)
                        {
                            stairsDown = true;
                            break;
                        }
                    }
                    if (stairsDown) // Final part of a side area
                    {
                        processMap.clearedMap = true;
                        GameMasterScript.heroPCActor.ClearMap(processMap);
                    }
                }
            }
        }

        if (itemWorldOpen)
        {
            itemWorldMaps = new Map[itemWorldIDs.Length];
            Array.Sort(itemWorldIDs);
            for (int i = 0; i < itemWorldMaps.Length; i++)
            {
                //Debug.Log("Try link item world map index " + i + " ID " + itemWorldIDs[i]);
                try { itemWorldMaps[i] = dictAllMaps[itemWorldIDs[i]]; }
                //catch (Exception e)
                catch
                {
                    if (Debug.isDebugBuild) Debug.Log("Error linking item world ID " + itemWorldIDs[i]);
                }
            }
            try
            {
                itemWorldItem = GameMasterScript.dictAllActors[itemWorldItemID] as Item;
                if (Debug.isDebugBuild) Debug.Log("<color=green>Loaded item world item: " + itemWorldItem.actorRefName + " " + itemWorldItem.actorUniqueID + "</color>");
            }
            //catch(Exception e)
            catch
            {
                if (Debug.isDebugBuild) Debug.Log("<color=red>Error linking item world item " + itemWorldItemID + "</color>");
            }


        }

        yield return null;
        // New code to update collide state.
        foreach (Map m in dictAllMaps.Values)
        {
            for (int x = 0; x < m.columns; x++)
            {
                for (int y = 0; y < m.rows; y++)
                {
                    m.mapArray[x, y].UpdateCollidableState();
                }
            }
        }

        activeMap = newMap;
        mapLoaded = true;


        newMap.BeautifyAllTerrain();
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;


        GameMasterScript.cameraScript.SetBGColor(activeMap.dungeonLevelData.bgColor);
        traversed = new bool[activeMap.rows, activeMap.columns];
        tChecked = new bool[activeMap.rows, activeMap.columns];

        if (activeMap.dungeonLevelData.imageOverlay != null && activeMap.dungeonLevelData.imageOverlay != "")
        {
            mapTileMesh.gameObject.SetActive(false);

            string mainOverlayRef = activeMap.dungeonLevelData.imageOverlay;

            // Check seasonal stuff.
            foreach (SeasonalImageDataPack sid in activeMap.dungeonLevelData.seasonalImages)
            {
                //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Evaluate seasonal image datapack " + sid.replaceNormalOverlay + " " + sid.whichSeason + " active? " + GameMasterScript.seasonsActive[(int)sid.whichSeason]);
                if (sid.replaceNormalOverlay && GameMasterScript.seasonsActive[(int)sid.whichSeason])
                {
                    //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG yes replacing the main ref");
                    mainOverlayRef = sid.seasonalImage;
                    break;
                }
            }

            Sprite spr = null;
            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
            {
                spr = TDAssetBundleLoader.GetSpriteFromMemory(mainOverlayRef);
            }
            else
            {
                spr = Resources.Load<Sprite>("Art/Tilesets/" + mainOverlayRef);
            }


            if (spr == null)
            {
                //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Could not find overlay for level " + activeMap.floor + ": " + activeMap.dungeonLevelData.imageOverlay);
            }
            else
            {
                imageOverlay = (GameObject)Instantiate(Resources.Load("ImageOverlay"));
                imageOverlay.GetComponent<SpriteRenderer>().sprite = spr;
                Vector3 pos = imageOverlay.transform.position;

                if (activeMap.dungeonLevelData.specialRoomTemplate.specificXPos != 0f)
                {
                    pos.x = (activeMap.dungeonLevelData.specialRoomTemplate.specificXPos);
                    pos.y = (activeMap.dungeonLevelData.specialRoomTemplate.specificYPos);
                }
                else
                {
                    pos.x = (activeMap.dungeonLevelData.specialRoomTemplate.numColumns / 2f) + 0.5f;
                    pos.y = (activeMap.dungeonLevelData.specialRoomTemplate.numRows / 2f) + 0.5f;
                }

                if (activeMap.dungeonLevelData.imageOverlayOffset != Vector2.zero)
                {
                    pos.x += activeMap.dungeonLevelData.imageOverlayOffset.x;
                    pos.y += activeMap.dungeonLevelData.imageOverlayOffset.y;
                }

                imageOverlay.transform.position = pos;

                //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG loaded overlay ref " + spr.name);
            }
            float adder = 0.001f;
            int count = 0;

            List<string> allOverlaysTemp = new List<string>();
            foreach (SeasonalImageDataPack sid in activeMap.dungeonLevelData.seasonalImages)
            {
                //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG evaluate " + sid.whichSeason + " " + sid.replaceNormalOverlay + " " + GameMasterScript.seasonsActive[(int)sid.whichSeason]);
                if (!sid.replaceNormalOverlay && GameMasterScript.seasonsActive[(int)sid.whichSeason])
                {
                    //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG adding to all overlay temp list.");
                    allOverlaysTemp.Add(sid.seasonalImage);
                }
            }
            foreach (string str in activeMap.dungeonLevelData.extraOverlays)
            {
                allOverlaysTemp.Add(str);
            }

            foreach (string extra in allOverlaysTemp)
            {
                GameObject overlay = (GameObject)Instantiate(Resources.Load("Art/Tilesets/" + extra));                
                if (overlay == null)
                {
                    //Debug.Log("SEASONDEBUG Could not find extra overlay for level " + activeMap.floor + " " + extra);
                }
                else
                {
                    //if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Spawning overlay called " + overlay.name + " from " + extra);
                    extraOverlays.Add(overlay);
                    Vector3 pos = overlay.transform.position;
                    pos.z += adder * count;
                    overlay.transform.position = pos;
                    count++;
                    // It's good to go.
                }
            }
        }

        yield return null;
        //If the dungeonLevelData has a parallax background, display it here.
        activeMap.UpdateParallaxBackground();

        //SpawnAllDoors(activeMap);
        SpawnAllDecor(activeMap);
        SpawnAllProps(activeMap);
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;
        SpawnAllItems(activeMap);
        SpawnAllMonsters(activeMap);
        SpawnAllNPCs(activeMap);
        SpawnAllStairs(activeMap);
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;


        foreach (Actor act in activeMap.actorsInMap)
        {
            try { act.myMovable.CheckTransparencyBelow(); }
            //catch (Exception e)
            catch
            {
#if UNITY_EDITOR
                //Debug.Log("Spawn transparency check failed on map load, " + act.GetPos() + " " + act.dungeonFloor + " " + act.actorRefName + " " + act.actorUniqueID);
#endif
            }
        }

        mapGridMesh.size_x = activeMap.columns;
        mapGridMesh.size_z = activeMap.rows;
        mapGridMesh.BuildMesh();
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.medium);
        yield return null;
        mapTileMesh.size_x = activeMap.columns;
        mapTileMesh.size_z = activeMap.rows;
        mapTileMesh.BuildMesh();
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.medium);
        yield return null;
        fogOfWarTMG.size_x = activeMap.columns;
        fogOfWarTMG.size_z = activeMap.rows;
        fogOfWarTMG.BuildMesh();
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.medium);
        yield return null;

        MinimapUIScript.BaseMapChanged(activeMap.columns, activeMap.rows);

        try { UpdateFOWOverlay(true); }
        //catch(Exception e)
        catch
        {
            Debug.Log("Error updating FOW overlay. " + activeMap.floor);
        }
        MinimapUIScript.RefreshMiniMap();
        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;

        currentFloor = activeMap.floor;
        if (activeMap.dungeonLevelData.revealAll)
        {
            fogOfWarQuad.SetActive(false);
        }
        else
        {
            fogOfWarQuad.SetActive(true);
        }


        for (int lvl = 0; lvl < theDungeon.maps.Count; lvl++)
        {
            Map workMap = theDungeon.maps[lvl];
            for (int x = 0; x < workMap.columns; x++)
            {
                for (int y = 0; y < workMap.rows; y++)
                {
                    MapTileData mtd = workMap.mapArray[x, y];
                    if (workMap.CheckMTDArea(mtd) == -1)
                    {
                        //Debug.Log("No room for " + mtd.pos + " floor " + mtd.floor);
                        continue;
                    }
                    Area container = null;
                    //Debug.Log("Number of areas on floor " + workMap.floor + " " + workMap.areaDictionary.Values.Count);
                    int areaID = workMap.CheckMTDArea(mtd);
                    try { container = workMap.areaDictionary[areaID]; }
                    catch (KeyNotFoundException e)
                    {
                        Debug.Log(e.ToString() + ": Key not found. Map level " + lvl + " x/y " + x + "," + y + " areaID " + areaID + " floor " + workMap.floor);
                    }
                    if (container != null)
                    {
                        //mtd.containingArea = container;
                        if (mtd.CheckMapTag(MapGenerationTags.EDGETILE))
                        {
                            container.edgeTiles.Add(mtd); // Edge tiles are now ignored as of 9/27 after map gen.
                        }
                        else
                        {
                            container.internalTiles.Add(mtd);
                        }
                    }

                }
            }
        }

        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;


        int highestID = 0;
        foreach (Map m in dictAllMaps.Values)
        {
            if (m.mapAreaID > highestID)
            {
                highestID = m.mapAreaID + 1;
            }
        }
        singletonMMS.mapAreaIDAssigner = highestID + 1;

        GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.small);
        yield return null;
    }


    // Read from save
    public IEnumerator LoadFromSave() // false if catastrophic failure.
    {
        // Don't do this function
        //RebuildUnvisitedMaps();

#if UNITY_SWITCH
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        BinaryReader reader = null;
        var sdh = Switch_SaveDataHandler.GetInstance();
        sdh.LoadBinarySwitchSave(ref reader, path);

        if( reader == null )
        {
            Debug.LogWarning("No file at " + path);
        }
#elif UNITY_PS4
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        byte[] savedData = null;
        BinaryReader reader = null;
        //load data       
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, path))
        {
            Debug.LogError("No file at " + path);
        }
        else
        {            
            PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, path, out savedData);
            //convert byte data to BinaryReader
            var memStream = new MemoryStream(savedData);
            reader = new BinaryReader(memStream);
        }
#elif UNITY_XBOXONE
        string path = "savedMap" + GameStartData.saveGameSlot + ".dat";
        byte[] savedData = null;
        BinaryReader reader = null;
        //load data        
        if (!XboxSaveManager.instance.HasKey(path))
        {
            Debug.LogError("No file at " + path);
        }
        else
        {
            savedData = XboxSaveManager.instance.GetBytes(path);
            //convert byte data to BinaryReader
            var memStream = new MemoryStream(savedData);
            reader = new BinaryReader(memStream);
        }
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";
        Stream saveStream = File.Open(path, FileMode.Open);
        BinaryReader reader = new BinaryReader(saveStream);
#endif

        int dungeonFloors = reader.ReadInt32();
        theDungeon = new Dungeon(dungeonFloors, 1.0f, .1f); // Using 1.0f and .1f as global defaults for now.

        theDungeon.dungeonName = "Loading Dungeon..."; // This is placeholder, naturally

        // ITEM WORLD STUFF

        List<Map> unlinkedItemWorldMaps = new List<Map>();

        byte[] eightByteArray = new byte[8];
        byte[] fourByteArray = new byte[4];
        byte[] nByteArray;
        byte[] twoByteArray = new byte[2];

        itemWorldOpen = reader.ReadBoolean();
        if (itemWorldOpen)
        {
            int id = 0;

            if (GameStartData.loadGameVer >= 113)
            {
                id = reader.ReadInt32();
            }
            else
            {
                id = reader.ReadInt16();
            }

            if (itemWorldItemID == 0)
            {
                itemWorldItemID = id;
            }
            if (Debug.isDebugBuild) Debug.Log("Item world item Id is " + itemWorldItemID);
            int numMapsInWorld = 0;
            try { numMapsInWorld = reader.ReadInt16(); }
            catch (Exception e)
            {
                Debug.Log("Exception during load: " + e);
                int lgNumber = reader.ReadInt32();
                Debug.Log("Try reading bigger number? " + lgNumber);
            }

            itemWorldIDs = new int[numMapsInWorld];
            for (int i = 0; i < numMapsInWorld; i++)
            {
                itemWorldIDs[i] = reader.ReadInt16();
                //Debug.Log("Loading item world map ID " + itemWorldIDs[i]);
            }
            Array.Sort(itemWorldIDs);
            //Debug.Log("Item world is open, with " + numMapsInWorld + ", item ID: " + itemWorldItemID + ". Maps have been sorted.");
        }


        int numTotalMaps = reader.ReadInt32();
        dictAllMaps.Clear();

        int itemWorldIDIndex = 0;

        if (Debug.isDebugBuild)  Debug.Log("LOADING " + numTotalMaps + " maps");
        
        if (numTotalMaps == 0)
        {
            // Kick it back to title screen.
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
            Debug.Log("<color=yellow>Reset the game saves, try to salvage it.</color>");
            reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            saveStream.Close();
#endif
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
            GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
            UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);

            // Failure occurred.
            GameMasterScript.mapsLoadedSuccessfully = false;
            SceneManager.LoadScene("Gameplay");
            yield break;
        }


        for (int mapCounter = 0; mapCounter < numTotalMaps; mapCounter++)
        {

            int mapAreaID = 0;

            try
            {
                mapAreaID = reader.ReadInt16();
            }
            catch (Exception e)
            {
                Debug.Log("Error when loading map " + mapCounter + ": " + e);
                break;
            }


            //if (debug) Debug.Log(mapAreaID);

            int levelDataLink = reader.ReadInt16();
            //if (debug) Debug.Log(mapAreaID + " load level data link, " + levelDataLink + " counter " + mapCounter);

            //if (debug) Debug.Log(levelDataLink);

            int mapFloor = reader.ReadInt16();

            // if (debug) Debug.Log(mapFloor);

            int effectiveFloor = reader.ReadInt16();

            // if (debug) Debug.Log(effectiveFloor);
            //Debug.Log("Reading map ID " + mapAreaID + " floor " + mapFloor + " effective " + effectiveFloor + " index: " + mapCounter);

            bool specialStartTile = reader.ReadBoolean();

            //if (debug) Debug.Log(specialStartTile);

            int startX = reader.ReadInt16();

            // if (debug) Debug.Log(startX);

            int startY = reader.ReadInt16();

            // if (debug) Debug.Log(startY);

            /* int columns = reader.ReadInt16();

            if (debug) Debug.Log(columns);

            int rows = reader.ReadInt16();

            if (debug) Debug.Log(rows); */

            float challengeRating = reader.ReadSingle();

            //   if (debug) Debug.Log(challengeRating);

            bool spawnerAlive = reader.ReadBoolean();

            //  if (debug) Debug.Log(spawnerAlive);

            bool mapIsHidden = reader.ReadBoolean();

            //  if (debug) Debug.Log(mapIsHidden);

            int initialSpawnedMonsters = reader.ReadInt16();

            // if (debug) Debug.Log(initialSpawnedMonsters);

            //Map readingMap = new Map(mapFloor);
            //readingMap.floor = mapFloor;

            Map readingMap = null;

            DungeonLevel dungeonLevelToCheck;
            if (GameMasterScript.masterDungeonLevelList.TryGetValue(mapFloor, out dungeonLevelToCheck))
            {
                readingMap = SetCorrectMapTypeFromDungeonLevelData(dungeonLevelToCheck, mapFloor);
            }
            else
            {
                readingMap = new Map();
                readingMap.floor = mapFloor;
            }

            readingMap.mapAreaID = mapAreaID;
            dictAllMaps[mapAreaID] = readingMap;

#if UNITY_EDITOR
            //Debug.Log("Added " + readingMap.floor + " id " + readingMap.mapAreaID + " to dictionary");
#endif

            if (mapFloor == TOWN_MAP_FLOOR)
            {
                singletonMMS.townMap = readingMap; // Does this fix it
            }
            if (mapFloor == TOWN2_MAP_FLOOR)
            {
                singletonMMS.townMap2 = readingMap;
                //Debug.Log("Connected town map 2.");
            }
            if (specialStartTile)
            {
                readingMap.heroStartTile = Vector2.zero;
                readingMap.heroStartTile.x = startX;
                readingMap.heroStartTile.y = startY;
            }
            theDungeon.maps.Add(readingMap);

            readingMap.levelDataLink = levelDataLink;

            if (!readingMap.IsItemWorld())
            {
                readingMap.dungeonLevelData = theDungeon.GetLevelData(readingMap.floor);
            }
            else
            {
                //readingMap.dungeonLevelData = theDungeon.GetLevelData(readingMap.levelDataLink);
                if (levelDataLink == 0)
                {
                    Debug.Log("WARNING! Floor " + readingMap.floor + " has LDL of 0, it should have something else.");
                }

                DungeonLevel dlToLink = DungeonLevel.GetSpecificLevelData(readingMap.levelDataLink);
                if (dlToLink != null)
                {
                    readingMap.dungeonLevelData = dlToLink;
                    //Debug.Log("Linked level data for " + readingMap.floor + " " + readingMap.mapAreaID + " " + readingMap.levelDataLink);
                    if (readingMap.IsItemWorld())
                    {
                        if (!itemWorldOpen)
                        {
                            Debug.Log("This is reading as IW map, but IW was not written as open.");
                            unlinkedItemWorldMaps.Add(readingMap);
                        }
                        else
                        {
                            itemWorldIDs[itemWorldIDIndex] = readingMap.mapAreaID;
                            itemWorldIDIndex++;
                        }
                    }
                }
                /* for (int i = 0; i < itemWorldIDs.Length; i++)
                {
                    Debug.Log("IW ID " + i + " is " + itemWorldIDs[i]);
                } */


            }

            if (readingMap.dungeonLevelData == null)
            {
                Debug.Log("Could not link dungeon level data for " + mapAreaID + " " + readingMap.floor);
            }

            readingMap.Init();
            readingMap.effectiveFloor = effectiveFloor;

            //if (debug) Debug.Log("Map counter " + mapCounter + " " + mapAreaID + " LDL " + levelDataLink + " Floor " + mapFloor + " col/row " + readingMap.columns + "," + readingMap.rows + " CV " + challengeRating);

            //Debug.Log(readingMap.floor + " " + readingMap.columns + " " + readingMap.rows);

            MapGenerationData mgd = new MapGenerationData();
            readingMap.initialSpawnedMonsters = initialSpawnedMonsters;
            readingMap.numFloors = dungeonFloors;
            readingMap.spawnerAlive = spawnerAlive;
            readingMap.mapIsHidden = mapIsHidden;
            int maxChampMods = reader.ReadInt16();

            //if (debug) Debug.Log(maxChampMods);

            int maxChamps = reader.ReadInt16();

            // if (debug) Debug.Log(maxChamps);

            float chanceToSpawnChamp = reader.ReadSingle();

            //  if (debug) Debug.Log("CTSC " + chanceToSpawnChamp);

            mgd.maxChampMods = maxChampMods;
            mgd.maxChamps = maxChamps;
            mgd.chanceToSpawnChampionMonster = chanceToSpawnChamp;
            readingMap.mgd = mgd;

            if (readingMap.mapRooms == null)
            {
                Debug.Log(readingMap.floor + " has no maprooms? " + readingMap.dungeonLevelData.layoutType + " " + readingMap.initialized);
                readingMap.mapRooms = new List<Room>();
            }

            int numAreas = reader.ReadInt16();
            //Debug.Log("Reading " + numAreas + " areas on map floor " + readingMap.floor + " " + mapFloor + " effective: " + readingMap.effectiveFloor + " Cols: " + readingMap.columns + " Rows: " + readingMap.rows);
            for (int ar = 0; ar < numAreas; ar++)
            {
                bool isCorridor = reader.ReadBoolean();
                //if (debug) Debug.Log(ar + " " + isCorridor);
                Area holder;
                if (isCorridor)
                {
                    Corridor cr = new Corridor();
                    cr.InitializeLists();
                    holder = cr;
                    readingMap.mapCorridors.Add(cr);
                }
                else
                {
                    Room rm = new Room();
                    rm.InitializeLists();
                    holder = rm;
                    readingMap.mapRooms.Add(rm);
                }

                holder.areaID = reader.ReadInt32();
                holder.origin.x = reader.ReadByte(); // was int16
                holder.origin.y = reader.ReadByte(); // was int16

                //Debug.Log("Area " + holder.areaID + " loading on floor " + readingMap.floor + " out of " + numAreas + " areas");

                /* int numConnections = reader.ReadInt16();
                try { holder.connectionIDs = new List<int>(numConnections); }
                catch(Exception e)
                {
                    Debug.Log("Error with connection holder... " + numConnections);
                }
                for (int c = 0; c < numConnections; c++)
                {
                    holder.connectionIDs.Add(reader.ReadInt16());
                } */
                readingMap.AddAreaToDictionaryWithID(holder, holder.areaID);
            }

            MapTileData mtd = null;

            if (mapAreaID == 0)
            {
                if (Debug.isDebugBuild) Debug.Log("Map has ID of 0 - warning. " + mapFloor + " " + effectiveFloor + " Col/Rows " + columns + "," + rows + " CR: " + challengeRating + " Num areas: " + numAreas);
            }


            int numColumns = reader.ReadByte(); // was int16
            int numRows = reader.ReadByte(); // was int16

            if (numColumns != readingMap.columns || numRows != readingMap.rows)
            {
                Debug.Log("WARNING! Mismatch of columns " + numColumns + " vs template " + readingMap.columns + " or rows " + numRows + " vs template " + readingMap.rows + " in " + readingMap.floor);
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
                Debug.Log("<color=yellow>Reset the game saves, try to salvage it.</color>");
                reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
                saveStream.Close();
#endif
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
                GameMasterScript.ResetAllVariablesToGameLoadExceptStartData();
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);

                // Failure occurred.
                GameMasterScript.mapsLoadedSuccessfully = false;
                SceneManager.LoadScene("Gameplay");
                yield break;
            }

            float timeAtStart = Time.realtimeSinceStartup;

            readingMap.mapArray = new MapTileData[numColumns, numRows];
            readingMap.areaIDArray = new int[numColumns, numRows];
            for (int x = 0; x < numColumns; x++)
            {
                for (int y = 0; y < numRows; y++)
                {
                    readingMap.areaIDArray[x, y] = -1; // Area NOT SET
                }
            }
            readingMap.exploredTiles = new bool[numColumns, numRows];


            for (int x = 0; x < readingMap.columns; x++) // WAS "Columns"
            {
                for (int y = 0; y < readingMap.rows; y++) // WAS rows
                {
                    mtd = new MapTileData();
                    mtd.pos = new Vector2(x, y);
                    mtd.iPos = new Point(x, y);

                    eightByteArray = reader.ReadBytes(8);

                    //readingMap.exploredTiles[x, y] = reader.ReadBoolean(); // 1st read: Explored
                    readingMap.exploredTiles[x, y] = Convert.ToBoolean(eightByteArray[0]);

                    readingMap.mapArray[x, y] = mtd;
                    //bool isWall = reader.ReadBoolean(); // 2nd read: Wall or not
                    bool isWall = Convert.ToBoolean(eightByteArray[1]);

                    if (isWall)
                    {
                        mtd.ChangeTileType(TileTypes.WALL, mgd);
                    }
                    else
                    {
                        mtd.ChangeTileType(TileTypes.GROUND, mgd);
                    }
                    //int number = reader.ReadByte();
                    int number = eightByteArray[2];
                    mtd.tileVisualSet = (TileSet)number;  // was int16

                    fourByteArray[0] = eightByteArray[3];
                    fourByteArray[1] = eightByteArray[4];
                    fourByteArray[2] = eightByteArray[5];
                    fourByteArray[3] = eightByteArray[6];

                    //readingMap.areaIDArray[x, y] = reader.ReadInt32();
                    readingMap.areaIDArray[x, y] = (int)BitConverter.ToInt32(fourByteArray, 0);

                    //int numTags = reader.ReadByte(); // was int16
                    int numTags = eightByteArray[7];

                    nByteArray = reader.ReadBytes(numTags);

                    if (numTags > 0)
                    {
                        for (int i = 0; i < numTags; i++)
                        {
                            //byte readByte = reader.ReadByte();
                            int tag = Convert.ToInt16(nByteArray[i]);
                            mtd.AddTag((LocationTags)tag);
                        }
                    }

                    nByteArray = reader.ReadBytes(9);

                    //byte b = reader.ReadByte();
                    //mtd.visualTileType = (VisualTileType)b; // was int16
                    mtd.visualTileType = (VisualTileType)nByteArray[0];

                    /* mtd.indexOfSpriteInAtlas = reader.ReadInt16();
                    mtd.indexOfGrassSpriteInAtlas = reader.ReadInt16();
                    mtd.indexOfTerrainSpriteInAtlas = reader.ReadInt16();
                    int decorIndex = reader.ReadInt16();  */

                    twoByteArray[0] = nByteArray[1];
                    twoByteArray[1] = nByteArray[2];
                    mtd.indexOfSpriteInAtlas = BitConverter.ToInt16(twoByteArray, 0);

                    twoByteArray[0] = nByteArray[3];
                    twoByteArray[1] = nByteArray[4];
                    mtd.indexOfGrassSpriteInAtlas = BitConverter.ToInt16(twoByteArray, 0);

                    twoByteArray[0] = nByteArray[5];
                    twoByteArray[1] = nByteArray[6];
                    mtd.indexOfTerrainSpriteInAtlas = BitConverter.ToInt16(twoByteArray, 0);

                    twoByteArray[0] = nByteArray[7];
                    twoByteArray[1] = nByteArray[8];
                    int decorIndex = BitConverter.ToInt16(twoByteArray, 0);

                    if (decorIndex != -1)
                    {
                        mtd.indexOfDecorSpriteInAtlas = decorIndex;
                    }

                    // Do we need to read another 4?
                }
            }

            if (readingMap.dungeonLevelData.tileVisualSet != TileSet.SPECIAL && !readingMap.dungeonLevelData.HasOverlayImage())
            {

            }

            readingMap.BeautifyAllTerrain();

            //Debug.Log("Read map: " + readingMap.floor);

            if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                GameMasterScript.IncrementLoadingBar(0.015f);
                timeAtStart = Time.realtimeSinceStartup;
            }
        }

        if (unlinkedItemWorldMaps.Count > 0)
        {
            Debug.Log("At least one unlinked item world.");
            itemWorldIDs = new int[unlinkedItemWorldMaps.Count];
            itemWorldMaps = new Map[unlinkedItemWorldMaps.Count];
            for (int i = 0; i < unlinkedItemWorldMaps.Count; i++)
            {
                itemWorldMaps[i] = unlinkedItemWorldMaps[i];
                itemWorldIDs[i] = unlinkedItemWorldMaps[i].mapAreaID;
            }
        }


        reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        saveStream.Close();
#endif

        if (Debug.isDebugBuild) Debug.Log("Completed loading all maps.");

        GameMasterScript.mapsLoadedSuccessfully = true;
    }

    /// <summary>
    /// Runs exactly ONCE on loading an existing save. Creates all terrain destructibles.
    /// </summary>
    public static void CreateTerrainTilesFromLoadedData()
    {
        MapTileData mtd;
        foreach (Map m in MapMasterScript.dictAllMaps.Values)
        {
            if (m.floor >= JELLY_DRAGON_DUNGEONSTART_FLOOR && m.floor <= JELLY_DRAGON_DUNGEONEND_FLOOR)
            {
                //return;
            }
            for (int x = 1; x < m.columns - 1; x++)
            {
                for (int y = 1; y < m.rows - 1; y++)
                {
                    mtd = m.mapArray[x, y];
                    if (mtd.CheckTag(LocationTags.WATER))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_rivertile");
                    }
                    else if (mtd.CheckTag(LocationTags.ISLANDSWATER))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_voidtile");
                    }
                    if (mtd.CheckTag(LocationTags.LAVA))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_lavatile");
                    }
                    if (mtd.CheckTag(LocationTags.ELECTRIC))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_electile");
                    }
                    if (mtd.CheckTag(LocationTags.MUD))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_mudtile");
                    }
                    if (mtd.CheckTag(LocationTags.LASER))
                    {
                        m.CreateDestructibleInTile(mtd, "obj_phasmashieldtile");
                    }
                }
            }
        }
    }

}

