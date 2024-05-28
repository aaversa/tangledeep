using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.IO;
using System.Linq;

public partial class MetaProgressScript
{
    public static NPC theBanker;

    public static IEnumerator LoadMetaProgress(bool minimal = false)
    {
        loadingMetaProgress = true;
        dictMetaProgress.Clear();
        monstersDefeated.Clear();
        defeatHistory.Clear();

        localTamedMonstersForThisSlot.Clear();

        theBanker = null;

        RandomJobMode.SetGameToRandomJobMode(false);

        LegendaryMaker.Initialize();
        MonsterMaker.Initialize();
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            DungeonMaker.Initialize();
        }

        float timeAtStart = Time.realtimeSinceStartup;

        //if (Debug.isDebugBuild) Debug.Log("Loading meta progress " + GameStartData.saveGameSlot);

#if UNITY_SWITCH
        string strPath = "metaprogress" + GameStartData.saveGameSlot.ToString() + ".xml";
        string strLoadedData = "";
        var sdh = Switch_SaveDataHandler.GetInstance();
        sdh.LoadSwitchDataFile(ref strLoadedData, strPath);
        if (!string.IsNullOrEmpty(strLoadedData))
        {
#elif UNITY_PS4
        string strPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        string strLoadedData = "";
        byte[] byteLoadedData = null;
        
        if (PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, strPath))
        {
#elif UNITY_XBOXONE
        string strPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        string strLoadedData = "";

        if (XboxSaveManager.instance.HasKey(strPath))
        {
#else
        string metaPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";

        if (Debug.isDebugBuild) Debug.Log("<color=green>Begin loading meta progress. " + metaPath + "</color>");


        if (File.Exists(metaPath))
        {
#endif
            // Load metagame progress
            XmlReaderSettings metaSettings = new XmlReaderSettings();
            metaSettings.IgnoreComments = true;
            metaSettings.IgnoreProcessingInstructions = true;
            metaSettings.IgnoreWhitespace = true;
#if UNITY_SWITCH
            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_PS4           
            PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, strPath, out byteLoadedData);
            //convert byte to StringReader
            strLoadedData = System.Text.Encoding.UTF8.GetString(byteLoadedData);           

            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_XBOXONE                                   
            strLoadedData = XboxSaveManager.instance.GetString(strPath);

            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#else
            FileStream metaStream = new FileStream(metaPath, FileMode.Open);
            XmlReader metaReader = XmlReader.Create(metaStream, metaSettings);
#endif

            //metaReader.Read();
            bool prepareToReadTree = false; // For old save compatibility
            string txt;
            MagicTree bufferedTreeLoader = null; // For old save compatibility

            while (metaReader.NodeType != XmlNodeType.EndElement)
            {
                string readValue = metaReader.Name.ToLowerInvariant();

                //Debug.Log(readValue + " " + metaReader.NodeType);

                switch (readValue)
                {
                    case "buffereddata":
                        metaReader.ReadElementContentAsString();
                        break;
                    case "gameversion":
                        loadedGameVersion = metaReader.ReadElementContentAsInt();
                        GameStartData.loadGameVer = loadedGameVersion;
                        Debug.Log("<color=green>Loaded game version is " + GameStartData.loadGameVer + "</color>");
                        /* if (loadedGameVersion != GameMasterScript.GAME_BUILD_VERSION)
                        {
                            Debug.Log("WARNING! Save file version does not match game version");
                        } */
                        break;
                    case "playermodsactive":
                        PlayerModManager.ParseSavedPlayerModsIntoList(playerModsSavedLastInMeta, metaReader.ReadElementContentAsString());
                        break;
                    case "expansions":
                        DLCManager.ParseSavedPlayerExpansionsIntoList(GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot], metaReader.ReadElementContentAsString());
                        break;
                    case "tree": // Restoring legacy data
                        bufferedTreeLoader = new MagicTree(null);
                        bufferedTreeLoader.ReadFromSave(metaReader);
                        Debug.Log("Loaded old tree data.");
                        prepareToReadTree = true;
                        break;
                    case "rlsmon":
                        ReleasedMonster rm = new ReleasedMonster();
                        releasedMonsters.Add(rm);
                        rm.ReadFromSave(metaReader);
                        break;
                    case "tutswatched":
                        TutorialManagerScript.ReadFromSave(metaReader);
                        break;
                    case "randomjob":
                    case "rjabils":
                    case "rjinnates":
                    case "rjxfloors":
                        ReadRandomJobModeStuff(metaReader, readValue);
                        break;
                    case "foodcart":
                        FoodCartScript.ReadFromSave(metaReader);
                        break;
                    case "citm":
                    case "customitem": // for procgen legendaries
                        Item itemReadFromFile = null;
                        bool success = true;

                        try { itemReadFromFile = CustomItemSerializer.ReadCustomItemFromSave(metaReader, true, GameStartData.saveGameSlot); }
                        catch (Exception e)
                        {
                            if (Debug.isDebugBuild) Debug.LogError("Could not read relic from meta file because: " + e);
                            success = false;
                        }

                        //if (Debug.isDebugBuild) Debug.Log("Read custom item: " + itemReadFromFile.actorRefName);

                        // Starting at game version 150, we're adding this to the master file...
                        // But citm will no longer be saved here!
                        if (success && !SharedBank.allRelicTemplates.ContainsKey(itemReadFromFile.actorRefName))
                        {
                            SharedBank.allRelicTemplates.Add(itemReadFromFile.actorRefName, itemReadFromFile);
                        }

                        //Debug.Log("Read item: " + itemReadFromFile.actorRefName);

                        break;
                    case "custommonster": // for procgen monsters
                        TryReadCustomMonsterTemplateFromSave(metaReader);
                        break;
                    case "dldata":
                        DungeonLevel dl = new DungeonLevel();
                        success = true;
                        try { dl.ReadCustomDungeonLevelDataFromSave(metaReader); }
                        catch (Exception e)
                        {
                            if (Debug.isDebugBuild) Debug.Log("Could not read dungeon level from meta file because: " + e);
                            success = false;
                        }
                        if (success && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                        {
                            if (!DungeonMaker.customDungeonLevelDataInSaveFile.ContainsKey(dl.floor))
                            {
                                DungeonMaker.customDungeonLevelDataInSaveFile.Add(dl.floor, dl);
                            }
                        }
                        break;
                    case "mdefeated":
                        string unparsed = metaReader.ReadElementContentAsString();
                        string[] parsed = unparsed.Split('|');
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            if (string.IsNullOrEmpty(parsed[i])) continue;
                            string[] subparse = parsed[i].Split(',');

                            int amount;
                            if (Int32.TryParse(subparse[1], out amount))
                            {
                                CustomAlgorithms.AddIntToDictionary(monstersDefeated, subparse[0], amount);
                            }

                            //monstersDefeated.Add(subparse[0], Int32.Parse(subparse[1]));
                        }

                        break;
                    case "saverelics":
                        relicRefsThatShouldNotBeDeleted.Clear();
                        string data = metaReader.ReadElementContentAsString();
                        string[] split = data.Split('|');
                        for (int i = 0; i < split.Length; i++)
                        {
                            relicRefsThatShouldNotBeDeleted.Add(split[i]);
                        }
                        break;
                    case "npc":
                        if (prepareToReadTree)
                        {
                            int readTreeSlot = bufferedTreeLoader.slot;
                            treesPlanted[readTreeSlot].myInventory.ClearInventory();
                            treesPlanted[readTreeSlot].ReadFromSave(metaReader, false);
                            treesPlanted[readTreeSlot].treeComponent = bufferedTreeLoader;
                            bufferedTreeLoader.npcObject = treesPlanted[readTreeSlot];
                            if (bufferedTreeLoader.npcObject == null)
                            {
                                Debug.Log("WARNING! No valid tree object in slot " + readTreeSlot);
                            }
                            prepareToReadTree = false;
                            //Debug.Log("Read buffered tree npc " + treesPlanted[readTreeSlot].actorUniqueID + " " + readTreeSlot);
                            break;
                        }

                        theBanker = new NPC();
                        theBanker.ReadFromSave(metaReader, false);
                        //Debug.Log("Loaded " + theBanker.actorRefName + " " + theBanker.actorUniqueID + " from meta data.");
                        break;
                    case "mn":
                    case "monster":
                        TryReadMonsterFromMetaProgress(metaReader);

                       break;
                    case "numcharacters":
                        int chars = metaReader.ReadElementContentAsInt();
                        SetTotalCharacters(chars);

                        break;
                    case "lowestfloor":
                        lowestFloorReached = metaReader.ReadElementContentAsInt();
                        break;
                    case "wft":
                        watchedFirstTutorial = metaReader.ReadElementContentAsBoolean();
                        break;
                    case "ju":
                        //ReadThroughEndOfNode(metaReader, "ju");
                        //Debug.Log("Read through end of JU node. " + metaReader.Name + " " + metaReader.NodeType);
                        //metaReader.ReadEndElement();
                        
                        ReadJobsUnlocked(metaReader, GameStartData.saveGameSlot);
                        break;
                    case "cfu":
                        //ReadThroughEndOfNode(metaReader, "cfu");
                        //Debug.Log("Read through end of CFU node. " + metaReader.Name + " " + metaReader.NodeType);
                        //metaReader.ReadEndElement();
                        //metaReader.ReadElementContentAsString();

                        ReadCreationFeats(metaReader);
                        break;
                    case "journalentry":
                        int entry = metaReader.ReadElementContentAsInt();
                        if (!journalEntriesRead.Contains(entry))
                        {
                            journalEntriesRead.Add(entry);
                        }
                        break;
                    case "defeat":
                        DefeatData dd = new DefeatData();
                        dd.ReadFromXml(metaReader); // handles the entire read process
                        AddDefeatData(dd);
                        break;
                    case "playtime":
                        txt = metaReader.ReadElementContentAsString();
                        playTimeAtGameLoad = CustomAlgorithms.TryParseFloat(txt);
                        break;
                    case "recipeknown":
                        txt = metaReader.ReadElementContentAsString();
                        LearnRecipe(txt);
                        break;
                    case "totaldayspassed":
                        totalDaysPassed = metaReader.ReadElementContentAsInt();
                        break;
                    case "jobsplayed":
                        metaReader.ReadStartElement();
                        while (metaReader.NodeType != XmlNodeType.EndElement)
                        {
                            CharacterJobs cj = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), metaReader.Name);
                            jobsStarted[(int)cj] = metaReader.ReadElementContentAsInt();
                        }
                        metaReader.ReadEndElement();
                        break;
                    case "grovetree":
                        if (metaReader.IsEmptyElement)
                        {
                            metaReader.Read();
                        }
                        else
                        {
                            metaReader.ReadStartElement();

                            NPC n = new NPC();
                            success = true;
                            try { n.ReadFromSave(metaReader); }
                            catch (Exception e)
                            {
                                Debug.Log("Could not read from meta tree " + e);
                                success = false;
                            }

                            if (success && n.treeComponent != null)
                            {
                                treesPlanted[n.treeComponent.slot] = n;
                            }
                            else
                            {
                                Debug.Log("Null tree read from metaprogress.");
                            }

                            metaReader.ReadEndElement();
                        }
                        break;
                    case "dictmetaprogress":
                        metaReader.ReadStartElement();

                        while (metaReader.NodeType != XmlNodeType.EndElement)
                        {
                            if (metaReader.NodeType == XmlNodeType.Whitespace || metaReader.NodeType == XmlNodeType.None)
                            {
                                metaReader.Read();
                            }
                            else
                            {
                                string keyName = metaReader.Name;
                                int value = metaReader.ReadElementContentAsInt();
                                if (dictMetaProgress.ContainsKey(keyName))
                                {
                                    dictMetaProgress[keyName] = value;
                                    //Debug.Log(keyName + " already existed in meta progress");
                                }
                                else
                                {
                                    dictMetaProgress.Add(keyName, value);
                                    //Debug.Log("Loaded key " + keyName + ", value " + value);
                                }

                            }
                        }

                        metaReader.ReadEndElement();
                        break;
                    default:
                        //Debug.Log("Didn't find anything, reading reading reading.");
                        metaReader.Read();
                        break;
                }

                if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    timeAtStart = Time.realtimeSinceStartup;
                    GameMasterScript.IncrementLoadingBar(0.005f);
                }
            }

            metaReader.Close();
#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE
            metaStream.Close();
#endif
            //if (Debug.isDebugBuild) Debug.Log("<color=green>FINISHED LOADING meta progress!</color>");

            // Fix for bad save data.
            if (!dictMetaProgress.ContainsKey("ancientcube"))
            {
                if (lowestFloorReached >= 6)
                {
                    dictMetaProgress.Add("ancientcube", 2);
                }
            }
            if (!dictMetaProgress.ContainsKey("secondbossdefeated"))
            {
                if (lowestFloorReached >= 11)
                {
                    dictMetaProgress.Add("secondbossdefeated", 1);
                }
            }
        }
        else
        {
            //Debug.Log("There is no meta file.");
        }
        loadingMetaProgress = false;

        MonsterMaker.SetCustomMonsterCounterFromLoadedMonsters();
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            DungeonMaker.SetCustomDungeonLevelCounterFromLoadedLevels();
        }

        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) >= 1)
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.DRAGON_DEFEAT, 1);
        }

        CreateResponsesFromDefeatData();
    }

    public static void ReadJobsUnlocked(XmlReader metaReader, int saveSlot)
    {
        if (metaJobsUnlocked == null) metaJobsUnlocked = new bool[(int)CharacterJobs.COUNT];

        metaReader.ReadStartElement();
        string unparsed = metaReader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');
        for (int i = 0; i < parsed.Length; i++)
        {
            if (i >= metaJobsUnlocked.Length) continue;
            metaJobsUnlocked[i] = Boolean.Parse(parsed[i]);
        }
        metaReader.ReadEndElement();
    }

    /// <summary>
    /// Ensures that we have the correct number of trees, no null trees, no trees in a weird state etc.
    /// </summary>
    public static void VerifyTreeState()
    {
        // gather up alllll the trees in the map into a list
        List<NPC> allTheTrees = new List<NPC>();
        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.actorRefName.Contains("town_tree"))
            {
                allTheTrees.Add(act as NPC);
            }
        }

        // Also, grab the trees from treesPlanted. Which SHOULD be the same.
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (!allTheTrees.Contains(treesPlanted[i]))
            {
                //if (Debug.isDebugBuild) Debug.Log("Uh oh! Meta tree wasn't one of the actual trees in the map! " + treesPlanted[i].actorUniqueID);
                allTheTrees.Add(treesPlanted[i]);
            }
        }

        allTheTrees = allTheTrees.OrderBy(n => n.actorRefName).ToList();

        if (allTheTrees.Count > 5)
        {
            if (Debug.isDebugBuild) Debug.Log("There are " + allTheTrees.Count + " total trees in this map.");
        }


        // Each tree slot, 1-5, should have one tree. Does it? Let's find out.
        List<NPC>[] treesPerSlot = new List<NPC>[5];
        for (int i = 0; i < treesPerSlot.Length; i++)
        {
            treesPerSlot[i] = new List<NPC>();
        }
        foreach (NPC n in allTheTrees)
        {
            char lastChar = n.actorRefName[n.actorRefName.Length - 1];
            int getSlot = Int32.Parse(lastChar.ToString());
            getSlot--; // offset because the refname is +1 from the actual slot index 
            // Now we have the slot numbered 0 to 4
            treesPerSlot[getSlot].Add(n);
        }

        // At this point, we should have 1 tree per list in the array.
        // If more than 1 tree exists in any slot, we'll figure out the 'best' tree to use (alive, rarest) and move the rest to an unassigned list
        List<NPC> unassignedTrees = new List<NPC>();

        for (int i = 0; i < treesPerSlot.Length; i++)
        {
            int highestRarity = 0;
            NPC treeToKeep = treesPerSlot[i].GetRandomElement();
            if (treesPerSlot[i].Count > 1)
            {
                if (Debug.isDebugBuild) Debug.Log("There are " + treesPerSlot[i].Count + " trees in slot " + i + ", so figure out the best one!");
                foreach (NPC n in treesPerSlot[i])
                {
                    if (n.treeComponent.alive && (int)n.treeComponent.treeRarity > highestRarity)
                    {
                        treeToKeep = n;
                        highestRarity = (int)n.treeComponent.treeRarity;
                    }
                }

                if (Debug.isDebugBuild) Debug.Log("The best tree is " + treeToKeep.actorRefName + " " + treeToKeep.actorUniqueID + " " + treeToKeep.treeComponent.treeRarity);
                // Now that we have the best, move the others into the unassigned category and out of this list.
                foreach (NPC n in treesPerSlot[i])
                {
                    if (n.actorUniqueID != treeToKeep.actorUniqueID)
                    {
                        unassignedTrees.Add(n);
                    }
                }

                treesPerSlot[i].Clear();
                treesPerSlot[i].Add(treeToKeep);
            }
        }

        // Now, each slot has only one of the "best" possible tree.        
        if (unassignedTrees.Count > 0)
        {
            // And we have a bunch of unassigned trees. Are there any empty slots? Put the best one(s) there
            for (int i = 0; i < 5; i++)
            {
                if (treesPerSlot[i].Count == 0)
                {
                    int highestRarity = 0;
                    NPC treeToKeep = unassignedTrees.GetRandomElement();
                    // Get the best tree for this slot
                    foreach (NPC n in unassignedTrees)
                    {
                        if (n.treeComponent.alive && (int)n.treeComponent.treeRarity > highestRarity)
                        {
                            treeToKeep = n;
                            highestRarity = (int)n.treeComponent.treeRarity;
                        }
                    }
                    treesPerSlot[i].Add(treeToKeep);
                    treeToKeep.actorRefName = "town_tree" + (i + 1); // have to change the ref name as the slot is changing.
                    unassignedTrees.Remove(treeToKeep);
                    Debug.Log("Assigned " + treeToKeep.actorUniqueID + " to slot " + i + " because there was nothing in it.");
                }
            }
        }

        // Now every slot SHOULD have a tree, but what if that's not true?
        for (int i = 0; i < 5; i++)
        {
            if (treesPerSlot[i].Count == 0)
            {
                NPC newTree = MagicTree.CreateTree(i);
                newTree.SetUniqueIDAndAddToDict();
                treesPerSlot[i].Add(newTree);
                Debug.Log("Had to create a new tree from scratch in slot " + i + " with ID " + newTree.actorUniqueID);
            }
        }

        // Get rid of extraneous trees on the map
        foreach (NPC n in unassignedTrees)
        {
            if (Debug.isDebugBuild) Debug.Log("Killing unassigned tree " + n.actorUniqueID);
            MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(n);
            if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && n.myMovable != null)
            {
                n.myMovable.FadeOutThenDie();
            }
        }

        // Do a final check to make our correct tree data matches meta data, overwriting meta data if necessary
        // Also make sure everything is spawned, and in the right position.
        List<Vector2> treePositions = new List<Vector2>()
        {
            new Vector2(26f, 13f),
            new Vector2(31f, 14f),
            new Vector2(31f, 10f),
            new Vector2(35f, 11f),
            new Vector2(39f, 12f)
        };

        for (int i = 0; i < 5; i++)
        {
            bool forceReplant = false;
            if (treesPlanted[i] != treesPerSlot[i][0])
            {
                if (Debug.isDebugBuild) Debug.Log("Mismatch between assigned tree " + treesPerSlot[i][0].actorUniqueID + " in slot " + i + " and meta data " + treesPlanted[i].actorUniqueID);
                treesPlanted[i] = treesPerSlot[i][0];
                forceReplant = true;
            }

            if (!forceReplant)
            {
                if (!MapMasterScript.singletonMMS.townMap2.actorsInMap.Contains(treesPlanted[i]))
                {
                    forceReplant = true;
                }
                else if (!MapMasterScript.singletonMMS.townMap2.GetTile(treesPlanted[i].GetPos()).HasActor(treesPlanted[i]))
                {
                    forceReplant = true;
                }
            }

            // If our position is wrong, reset it completely!
            if (treesPlanted[i].GetPos() != treePositions[i] || forceReplant)
            {
                //if (Debug.isDebugBuild) Debug.Log(treesPlanted[i].actorUniqueID + " position is wrong. Adjusting it from " + treesPlanted[i].GetPos() + " to " + treePositions[i]);
                MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(treesPlanted[i]);
                MapMasterScript.singletonMMS.townMap2.PlaceActor(treesPlanted[i], MapMasterScript.singletonMMS.townMap2.GetTile(treePositions[i]));

                if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
                {
                    if (treesPlanted[i].GetObject() == null || !treesPlanted[i].GetObject().activeSelf) // Not spawned? Spawn it
                    {
                        MapMasterScript.singletonMMS.SpawnNPC(treesPlanted[i]);
                    }
                    else
                    {
                        // otherwise, move the movable so the sprite is in the right position
                        treesPlanted[i].myMovable.AnimateSetPosition(treePositions[i], 0.01f, false, 0f, 0f, MovementTypes.LERP);
                    }
                }
            }
            else // position is right but
            {
                if (MapMasterScript.activeMap.floor == MapMasterScript.singletonMMS.townMap2.floor)
                {
                    if (treesPlanted[i].GetObject() == null || !treesPlanted[i].GetObject().activeSelf) // Not spawned? Spawn it
                    {
                        MapMasterScript.singletonMMS.SpawnNPC(treesPlanted[i]);
                    }
                }
            }

            /* if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                Debug.Log(treesPlanted[i].GetPos() + " " + treesPlanted[i].actorRefName + " " + treesPlanted[i].treeComponent.age + " " + treesPlanted[i].GetObject().name);
                Debug.Log("In map ? " + MapMasterScript.singletonMMS.townMap2.actorsInMap.Contains(treesPlanted[i]));
                Debug.Log(MapMasterScript.GetTile(treesPlanted[i].GetPos()).HasActor(treesPlanted[i]));
            } */

        }

        return;

    }



    public static void LoadMetaDictProgress()
    {
#if UNITY_SWITCH
        string strPath = "metaprogress" + GameStartData.saveGameSlot.ToString() + ".xml";
        string strLoadedData = "";
        var sdh = Switch_SaveDataHandler.GetInstance();
        sdh.LoadSwitchDataFile(ref strLoadedData, strPath);
        if( !string.IsNullOrEmpty(strLoadedData))
        { 
#elif UNITY_PS4
        string strPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        string strLoadedData = "";
        byte[] byteLoadedData = null;
       
        if (PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, strPath))
        {
#elif UNITY_XBOXONE
        string strPath = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        string strLoadedData = "";

        if (XboxSaveManager.instance.HasKey(strPath))
        {
#else
        string metaPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";

        if (Debug.isDebugBuild) Debug.Log("<color=yellow>Begin loading meta progress. " + metaPath + "</color>");

        if (File.Exists(metaPath))
        {
#endif
            // Load metagame progress
            XmlReaderSettings metaSettings = new XmlReaderSettings();
            metaSettings.IgnoreComments = true;
            metaSettings.IgnoreProcessingInstructions = true;
            metaSettings.IgnoreWhitespace = true;
#if UNITY_SWITCH
            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_PS4            
            PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, strPath, out byteLoadedData);
            strLoadedData = System.Text.Encoding.UTF8.GetString(byteLoadedData);          

            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_XBOXONE                       
            strLoadedData = XboxSaveManager.instance.GetString(strPath);

            XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#else
            FileStream metaStream = new FileStream(metaPath, FileMode.Open);
            XmlReader metaReader = XmlReader.Create(metaStream, metaSettings);
#endif
            try
            {
                metaReader.Read();
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log("SERIOUS ERROR: Meta progress in slot " + GameStartData.saveGameSlot + " is corrupted.");
                metaReader.Close();

#if UNITY_SWITCH
                sdh.DeleteSwitchDataFile(strPath);
#elif UNITY_PS4              
                PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, strPath);
#elif UNITY_XBOXONE
                XboxSaveManager.instance.DeleteKey(strPath);
                XboxSaveManager.instance.Save();
#else
                metaStream.Close();
                File.Delete(metaPath);
#endif
                // #todo - Notify the player of file corruption
                return;
            }

            bool finished = false;
            int iterations = 0;
            dictMetaProgress = new Dictionary<string, int>();
            while (!finished)
            {
                iterations++;
                if (iterations > 500000)
                {
                    Debug.Log(metaReader.Name + " " + metaReader.NodeType);
                    if (iterations > 500010)
                    {
                        Debug.Log("Broke meta dict progress loop");
                        break;
                    }
                }
                string readValue = metaReader.Name.ToLowerInvariant();

                if (metaReader.NodeType == XmlNodeType.EndElement && readValue == "document")
                {
                    break;
                }

                switch (readValue)
                {
                    case "dictmetaprogress":
                        metaReader.ReadStartElement();

                        while (metaReader.NodeType != XmlNodeType.EndElement)
                        {
                            while ((metaReader.NodeType == XmlNodeType.Whitespace) || (metaReader.NodeType == XmlNodeType.None))
                            {
                                metaReader.Read();
                            }


                            if (metaReader.NodeType == XmlNodeType.EndElement)
                            {
                                break;
                            }

                            {
                                string keyName = metaReader.Name;
                                int value = metaReader.ReadElementContentAsInt();
                                if (dictMetaProgress.ContainsKey(keyName))
                                {
                                    dictMetaProgress[keyName] = value;
                                    Debug.Log(keyName + " already existed in meta progress");
                                }
                                else
                                {
                                    dictMetaProgress.Add(keyName, value);
                                    //Debug.Log("Read meta progress key " + keyName + " val " + value);
                                }

                            }
                        }

                        metaReader.ReadEndElement();
                        finished = true;
                        break;
                    default:
                        metaReader.Read();
                        break;
                }
            }

            metaReader.Close();
#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE
            metaStream.Close();
#endif
            //Debug.Log("Finished loading meta progress.");
        }
    }

    static void TryReadMonsterFromMetaProgress(XmlReader metaReader)
    {
        List<TamedCorralMonster> corralMonsterListToUse = null;
        if (GameStartData.GetGameMode() != GameModes.HARDCORE 
            && GameStartData.gameModesSelectedBySlot[GameStartData.saveGameSlot] != GameModes.HARDCORE
            && GameStartData.challengeTypeBySlot[GameStartData.saveGameSlot] == ChallengeTypes.NONE
            && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            if (Debug.isDebugBuild) Debug.Log("Loading a monster, and we are NOT in hardcore, ronin mode, and not in a challenge");
            corralMonsterListToUse = SharedCorral.tamedMonstersSharedWithAllSlots;
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Loading a monster, and WE ARE IN in hardcore, ronin mode, OR in a challenge");
            corralMonsterListToUse = localTamedMonstersForThisSlot;
        }
        Monster m = new Monster();
        bool successRead = false;

        try
        {
            m.ReadFromSave(metaReader, false);
            successRead = true;
        }
        catch(Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Could not read monster. " + e);
        }
            
            
        if (m.tamedMonsterStuff != null && successRead)
        {
            bool addToCorral = true;
            foreach (TamedCorralMonster tcm in corralMonsterListToUse)
            {
                Actor act = tcm.monsterObject;
                if (act.actorUniqueID == m.actorUniqueID && act.actorRefName == m.actorRefName && act.displayName == m.displayName)
                {
                    if (Debug.isDebugBuild) Debug.Log(act.actorUniqueID + " " + act.actorRefName + " already in corral, not adding them again during load phase.");
                    addToCorral = false;
                    break;
                }
                else

                {
                    // Update this monster ID and THEN add it to corral
                    // But if it's a local creature, don't do this
                    if (SharedBank.ShouldUseSharedBankForCurrentGame())
                    {
                        GameMasterScript.AssignActorID(act);
                    }                    
                    
                    // Do we need to check anything else?
                    addToCorral = true;
                }
            }
            if (addToCorral) // eventually we will need to merge everything in.
            {
                GameMasterScript.AssignActorID(m, SharedBank.corralIDAssigner);
                SharedBank.corralIDAssigner++;
                corralMonsterListToUse.Add(m.tamedMonsterStuff);
                GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(corralMonsterListToUse.Count);
                if (Debug.isDebugBuild) Debug.Log("Read monster " + m.PrintCorralDebug() + " into corral from META PROGRESS. Current count is " + corralMonsterListToUse.Count);
            }
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Failed to read tamed monster component from MetaProgress creature.");
        }

    }
    
    static void TryReadCustomMonsterTemplateFromSave(XmlReader metaReader)
    {
        MonsterTemplateData mtd = null;
        bool success = true;
        try { mtd = MonsterTemplateSerializer.ReadCustomMonsterFromSave(metaReader); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.LogError("Could not read MD monster from meta file because: " + e);
            success = false;
        }

        //if (Debug.isDebugBuild) Debug.Log("Read custom monster: " + mtd.refName);

        if (success )
        {
            if (!MonsterMaker.uniqueMonstersSpawnedInSaveFile.Contains(mtd)) // Not local, but is it in our shared progress?
            {
                if (SharedCorral.HasCustomMonsterTemplateOfNameAndPrefab(mtd))
                {
                    // Do nothing
                    Debug.Log("Shared bank already has this template, so do nothing.");
                }
                else
                {
                    if (SharedCorral.HasCustomMonsterTemplateOfSameNameButDifferentContents(mtd))
                    {
                        // Rename the monster prefab based on the current save slot and see if this will work.
                        mtd.refName += "_" + GameStartData.saveGameSlot;
                    }

                    if (SharedCorral.CheckForMonsterTemplateRefInListOfAllPets(mtd.refName))
                    {
                        SharedCorral.customMonsterTemplatesUsedByPets.Add(mtd);
                        //Debug.Log("Adding " + mtd.refName + " to shared list instead!");
                    }
                    else
                    {
                        MonsterMaker.uniqueMonstersSpawnedInSaveFile.Add(mtd);
                        //Debug.Log("Read monster " + mtd.refName + " " + mtd.prefab + " " + mtd.monsterName);
                    }                    
                }
            }            
        }
    }
}
