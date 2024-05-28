using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.IO;
using System.Text;

public partial class SharedBank
{
    public static bool startedReadingStash;
    public static bool finishedReadingStash;

    public static List<int> saveSlotsClearedAndAwaitingRelicFlush;

    public static bool fatalReadError;

    public static IEnumerator TrySaveSharedProgress()
    {
        yield return WriteToSave();
        //if (Debug.isDebugBuild) Debug.Log("Finished serializing SharedBank! Items in bank? " + allItemsInBank.Count + " creatures in shared corral? " + SharedCorral.tamedMonstersSharedWithAllSlots.Count);
    }

    static bool readFromDiskAtLeastOnceOnTitleScreen = false;
    static bool readInventoryFromDiskAtLeastOnce = false;

    public static void OnSharedBankFileCreated()
    {
        readFromDiskAtLeastOnceOnTitleScreen = true;
        readInventoryFromDiskAtLeastOnce = true;
        finishedReadingStash = true;
    }

    public static IEnumerator ReadFromSave(bool readingAtTitleScreen)
    {
        if (Debug.isDebugBuild) Debug.Log("Request read from shared bank. At title screen? " + readingAtTitleScreen + " NGP should be? " + GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot]);
        GameStartData.NewGamePlus = GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot];

        if (readInventoryFromDiskAtLeastOnce && saveSlotsClearedAndAwaitingRelicFlush != null)
        {
            if (Debug.isDebugBuild) Debug.Log("We already read inventory from SharedBank once, but iterate over it to see if there were any deleted files that impact the relic templates. Slots deleted: " + saveSlotsClearedAndAwaitingRelicFlush.Count);
            foreach (var kvp in allRelicTemplates)
            {
                if (saveSlotsClearedAndAwaitingRelicFlush.Contains(kvp.Value.saveSlotIndexForCustomItemTemplate))
                {
                    MarkRelicTemplateForDeletion(kvp.Key);
                    Debug.Log("Marking " + kvp.Value.actorRefName + " for deletion on save file load.");
                }
            }
            saveSlotsClearedAndAwaitingRelicFlush.Clear();
        }

        if (readFromDiskAtLeastOnceOnTitleScreen && readingAtTitleScreen)
        {
            yield break;
        }
        if (readInventoryFromDiskAtLeastOnce && !readingAtTitleScreen)
        {
            yield break;
        }


        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        // Do some sort of "time passed" check so we don't lock up the whole process

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        string globalSaveSlotDataPath = CustomAlgorithms.GetPersistentDataPath() + "/shareddata.xml";

        if (!File.Exists(globalSaveSlotDataPath))
        {
            finishedReadingStash = true;
            if (Debug.isDebugBuild) Debug.Log("No shared bank. Continuing.");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }
#else
        string globalSaveSlotDataPath = "shareddata.xml";
#endif

#if UNITY_SWITCH
        var saveDataHandler = Switch_SaveDataHandler.GetInstance();

        if (!saveDataHandler.CheckIfSwitchFileExists(globalSaveSlotDataPath))
        {
            finishedReadingStash = true;
            if (Debug.isDebugBuild) Debug.Log("No shared Switch bank. Continuing.");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }

        yield return saveDataHandler.LoadSwitchSavedDataFileAsync(globalSaveSlotDataPath, false);

        byte[] loadedBytes = null;
        Switch_SaveDataHandler.GetBytesLoadedAsync(globalSaveSlotDataPath, ref loadedBytes);

        if (loadedBytes == null && Debug.isDebugBuild) Debug.Log("Null loaded bytes for shared bank?");

        //if (Debug.isDebugBuild) Debug.Log("Sharedbank loaded bytes length is " + loadedBytes.Length);

        var memStream = new MemoryStream(loadedBytes);
        var binaryReader = new BinaryReader(memStream);
        var sReader = new StringReader(binaryReader.ReadString());
        
        XmlReader metaReader = XmlReader.Create(sReader, settings);
#endif

#if UNITY_PS4
        globalSaveSlotDataPath = "shareddata.xml";
        byte[] loadedBytes = null;       
        if(!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, globalSaveSlotDataPath))
        {
            finishedReadingStash = true;
            if (Debug.isDebugBuild) Debug.Log("No shared bank. Continuing.");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }       
        PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, globalSaveSlotDataPath, out loadedBytes);
        string strLoadedData = System.Text.Encoding.UTF8.GetString(loadedBytes);       

        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), settings);
#endif

#if UNITY_XBOXONE
        globalSaveSlotDataPath = "shareddata.xml";
        if (!XboxSaveManager.instance.HasKey(globalSaveSlotDataPath))
        {
            finishedReadingStash = true;
            if (Debug.isDebugBuild) Debug.Log("No shared bank. Continuing.");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }
        string strLoadedData = XboxSaveManager.instance.GetString(globalSaveSlotDataPath);

        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), settings);
#endif

        if (!readingAtTitleScreen)
        {
            startedReadingStash = true;
            SharedCorral.tamedMonstersSharedWithAllSlots.Clear();
        }

#if !UNITY_PS4 && !UNITY_XBOXONE
        //if (Debug.isDebugBuild) Debug.Log("Prepare to check filestream of sharedbank.");
        using (FileStream stream = new FileStream(globalSaveSlotDataPath, FileMode.Open, FileAccess.Read))
        {
        #endif
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            using (XmlReader metaReader = XmlReader.Create(stream, settings))
#else
            using (metaReader)
#endif
            {
                // 1/19/23 check for total corruption of file.
                try
                {
                    metaReader.Read(); // reads the xml tag
                }
                catch(Exception e) 
                {
                    Debug.Log(e);
                    fatalReadError = true;
                    OnFinishedReadingStash(readingAtTitleScreen);
                    yield break;
                }
                
                metaReader.Read(); // reads document tag

                bool finishImmediately = false;

                int attempts = 0;
                while (metaReader.NodeType != XmlNodeType.EndElement && metaReader.NodeType != XmlNodeType.None)
                {
                    if (finishImmediately) break;
                    attempts++;
                    if (attempts > 50000) break;
                    //Debug.Log(metaReader.Name + " " + metaReader.NodeType);
                    switch (metaReader.Name)
                    {
                        case "sharamode":
                            metaReader.ReadElementContentAsString();
                            break;
                        case "citm":
                            bool keepReading = TryReadRelicFromSave(metaReader, readingAtTitleScreen);
                            if (!keepReading) finishImmediately = true;
                            break;
                        case "custommonster":
                            keepReading = TryReadMonsterTemplateFromSave(metaReader, readingAtTitleScreen);
                            if (!keepReading) finishImmediately = true;
                            break;
                        case "item":
                            keepReading = TryReadItemFromSave(metaReader, readingAtTitleScreen);
                            if (!keepReading) finishImmediately = true;
                            break;
                        case "globalsharedpetidcounter":
                            int newValue = metaReader.ReadElementContentAsInt();
                            if (SharedCorral.globalSharedPetIDCounter <= 0)
                            {
                                SharedCorral.globalSharedPetIDCounter = newValue;
                            }                            
                            break;
                        case "mn":
                            keepReading = TryReadMonsterFromSave(metaReader, readingAtTitleScreen);
                            if (!keepReading) finishImmediately = true;
                            break;
                        case "jobsunlocked":
                            if (!readingAtTitleScreen)
                            {
                                metaReader.ReadElementContentAsString();
                                break;
                            }
                            TryReadAndParseJobsUnlocked(metaReader);
                            break;
                        case "featsunlocked":
                            if (!readingAtTitleScreen)
                            {
                                metaReader.ReadElementContentAsString();
                                break;
                            }
                            TryReadAndParseFeatsUnlocked(metaReader);
                            break;
                        case "progressflags":
                            if (!readingAtTitleScreen)
                            {
                                metaReader.ReadElementContentAsString();
                                break;
                            }
                            TryReadAndParseProgressFlags(metaReader);
                            break;
                        case "season":
                            TryReadAndParseCustomSeasonValue(metaReader);
                            break;                        
                        case "maxbankeditems":
                            bankerMaxItems = metaReader.ReadElementContentAsInt();
                            break;
                        case "bankgold":
                            int gold = metaReader.ReadElementContentAsInt();
                            goldBanked = gold;
                            break;
                        default:
                            metaReader.Read();
                            break;
                    }
                }

                //Debug.Log("Done reading from shared bank. Title screen? " + readingAtTitleScreen + " Reader info? " + metaReader.NodeType + " " + metaReader.Name);
            }
#if !UNITY_PS4 && !UNITY_XBOXONE
        }
#endif

        OnFinishedReadingStash(readingAtTitleScreen);

        if (Debug.isDebugBuild) Debug.Log("Finished reading shared data. From title screen? " + readingAtTitleScreen + ", num monsters: " + SharedCorral.tamedMonstersSharedWithAllSlots.Count + " relic count: " + allRelicTemplates.Count);

        if (!readingAtTitleScreen)
        {

        }
    }

    static void OnFinishedReadingStash(bool readingAtTitleScreen)
    {
        if (readingAtTitleScreen) readFromDiskAtLeastOnceOnTitleScreen = true;
        else
        {
            readInventoryFromDiskAtLeastOnce = true;
            finishedReadingStash = true;
        }

        if (saveSlotsClearedAndAwaitingRelicFlush != null && !readingAtTitleScreen)
        {
            saveSlotsClearedAndAwaitingRelicFlush.Clear();
        }

    }

    static void TryReadAndParseFeatsUnlocked(XmlReader metaReader)
    {
        string flagString = metaReader.ReadElementContentAsString();
        string[] chopped = flagString.Split('|');
        for (int i = 0; i < chopped.Length; i++)
        {
            creationFeatsUnlocked.Add(chopped[i]);
        }
    }

    static void TryReadAndParseJobsUnlocked(XmlReader metaReader)
    {
        string flagString = metaReader.ReadElementContentAsString();
        string[] chopped = flagString.Split('|');
        for (int i = 0; i < chopped.Length; i++)
        {
            string jobString = chopped[i];

            CharacterJobs cj = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), jobString);

            jobsUnlocked[(int)cj] = true;
        }
    }

    static void TryReadAndParseProgressFlags(XmlReader metaReader)
    {
        string flagString = metaReader.ReadElementContentAsString();
        string[] chopped = flagString.Split('|');
        for (int i = 0; i < chopped.Length; i++)
        {
            string flag = chopped[i];

            SharedSlotProgressFlags cj = (SharedSlotProgressFlags)Enum.Parse(typeof(SharedSlotProgressFlags), flag);

            sharedProgressFlags[(int)cj] = true;            
        }
    }

    static void TryReadAndParseCustomSeasonValue(XmlReader metaReader)
    {
        string seasonString = metaReader.ReadElementContentAsString();
        if (int.TryParse(seasonString, out int seasonValue))
        {
            if (seasonValue < (int)Seasons.COUNT)
            {
                customSeasonValue = (Seasons)seasonValue;
#if UNITY_SWITCH
                if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Read custom season value: " + customSeasonValue);
#endif
            }
        }
    }

    static IEnumerator WriteToSave()
    {
        float timeSaveStart = Time.realtimeSinceStartup;
#if UNITY_SWITCH
        string kStrFileName = "shareddata.xml";
        var sdh = Switch_SaveDataHandler.GetInstance();
        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter sharedWriter = XmlWriter.Create(strData, xmlSettings);
#elif UNITY_PS4
        string kStrFileName = "shareddata.xml";
        
        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter sharedWriter = XmlWriter.Create(strData, xmlSettings);
#elif UNITY_XBOXONE
        string kStrFileName = "shareddata.xml";

        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter sharedWriter = XmlWriter.Create(strData, xmlSettings);
#else
        string globalSaveSlotDataPath = CustomAlgorithms.GetPersistentDataPath() + "/shareddata.xml";
        File.Delete(globalSaveSlotDataPath);

        if (Debug.isDebugBuild) Debug.Log("Saving SHARED progress to " + globalSaveSlotDataPath);

        XmlWriter sharedWriter = XmlWriter.Create(globalSaveSlotDataPath);
#endif
        sharedWriter.WriteStartDocument();
        sharedWriter.WriteStartElement("DOCUMENT");

        string jobsUnlockedString = CreateJobsUnlockedString();
        sharedWriter.WriteElementString("jobsunlocked", jobsUnlockedString);

        string featsUnlockedString = CreateFeatsUnlockedString();
        if (!string.IsNullOrEmpty(featsUnlockedString))
        {
            sharedWriter.WriteElementString("featsunlocked", featsUnlockedString);
        }

        if (customSeasonValue != Seasons.NONE)
        {
            sharedWriter.WriteElementString("season", ((int)customSeasonValue).ToString());
        }

        string flagString = CreateFlagProgressString();
        if (!string.IsNullOrEmpty(flagString)) sharedWriter.WriteElementString("progressflags", flagString);

        sharedWriter.WriteElementString("bankgold", goldBanked.ToString());

        sharedWriter.WriteElementString("maxbankeditems", bankerMaxItems.ToString());

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        sharedWriter.WriteElementString("globalsharedpetidcounter", SharedCorral.globalSharedPetIDCounter.ToString());

        SharedCorral.WriteAllCustomMonsterTemplatesToSave(sharedWriter);

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        SharedCorral.WriteAllTamedCorralMonstersToSave(sharedWriter);

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        WriteAllRelicTemplatesToSave(sharedWriter);

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        int check = 5;

        foreach (Item itm in allItemsInBank)
        {
            check--;
            if (check == 0)
            {
                if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    timeSaveStart = Time.realtimeSinceStartup;
                }
                check = 5;
            }
            itm.WriteToSave(sharedWriter);
        }

        sharedWriter.WriteEndElement();
        sharedWriter.WriteEndDocument();
        sharedWriter.Close();

#if UNITY_SWITCH
        sdh.SaveSwitchFile(strData.ToString(), kStrFileName);
#endif

#if UNITY_PS4
        //convert string build to byte
        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(strData.ToString());        
        PS4SaveManager.instance.SaveData(PS4SaveManager.ROOT_DIR, kStrFileName, myByte);
#endif

#if UNITY_XBOXONE
        XboxSaveManager.instance.SetString(kStrFileName, strData.ToString());
        XboxSaveManager.instance.Save();
#endif

        OnSharedBankFileCreated();
    }

    static void WriteInventoryToSave(XmlWriter writer)
    {
        foreach (Item itm in allItemsInBank)
        {
            itm.WriteToSave(writer);
        }
    }

    static string CreateJobsUnlockedString()
    {
        string retString = "";
        for (int i = 0; i < jobsUnlocked.Length; i++)
        {
            if (!jobsUnlocked[i]) continue;
            retString += (CharacterJobs)i; // BRIGAND, MIRAISHARA, etc
            retString += "|";
        }

        retString = retString.Remove(retString.Length - 1);

        return retString;
    }

    static string CreateFeatsUnlockedString()
    {
        string retString = "";

        for (int i = 0; i < creationFeatsUnlocked.Count; i++)
        {
            retString += creationFeatsUnlocked[i];
            retString += "|";
        }

        if (string.IsNullOrEmpty(retString)) return "";

        retString = retString.Remove(retString.Length - 1);

        return retString;
    }

    static string CreateFlagProgressString()
    {
        string retString = "";

        for (int i = 0; i < sharedProgressFlags.Length; i++)
        {
            if (!sharedProgressFlags[i]) continue;
            retString += (SharedSlotProgressFlags)i;
            retString += "|";
        }

        if (string.IsNullOrEmpty(retString)) return retString;

        retString = retString.Remove(retString.Length - 1);

        return retString;
    }

    static void WriteAllRelicTemplatesToSave(XmlWriter writer)
    {
        if (allRelicTemplates.Values.Count == 0) return;

        foreach (Item itm in allRelicTemplates.Values)
        {
            if (itm.ReadActorData("loserelic") == 1 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
            {
                Debug.Log(itm.actorRefName + " is marked for deletion, so don't serialize it.");
                continue;
            }
            itm.WriteEntireTemplateToSaveAsCustomItem(writer);
        }
    }

    static bool TryReadRelicFromSave(XmlReader metaReader, bool readingAtTitleScreen)
    {
        if (saveSlotsClearedAndAwaitingRelicFlush == null) saveSlotsClearedAndAwaitingRelicFlush = new List<int>();

        if (readingAtTitleScreen)
        {
            return false;
        }
        else
        {
            Item itemReadFromFile = null;
            bool success = true;

            try { itemReadFromFile = CustomItemSerializer.ReadCustomItemFromSave(metaReader); }
            catch (Exception e)
            {
                if (Debug.isDebugBuild) Debug.LogError("Could not read relic from meta file because: " + e);
                success = false;
            }

            //if (Debug.isDebugBuild && success) Debug.Log("Successfully read custom item: " + itemReadFromFile.actorRefName + " " + itemReadFromFile.displayName);

            if (success && !allRelicTemplates.ContainsKey(itemReadFromFile.actorRefName))
            {
                allRelicTemplates.Add(itemReadFromFile.actorRefName, itemReadFromFile);
            }

            // This relic belongs to a specific save file
            // Was it deleted since we last checked?
            if (success && itemReadFromFile.saveSlotIndexForCustomItemTemplate != 99)
            {
                if (saveSlotsClearedAndAwaitingRelicFlush.Contains(itemReadFromFile.saveSlotIndexForCustomItemTemplate))
                {
                    MarkRelicTemplateForDeletion(itemReadFromFile.actorRefName);
                    Debug.Log("Marking " + itemReadFromFile.actorRefName + " for deletion on relic read be cause it belongs to another save file, maybe.");
                }
            }

            //Debug.Log("Reader position is now: " + metaReader.NodeType + " " + metaReader.Name);
        }
        return true;
    }

    static bool TryReadItemFromSave(XmlReader metaReader, bool readingAtTitleScreen)
    {
        if (readingAtTitleScreen)
        {
            return false;
        }
        else
        {
            Item create = new Item();
            create = create.ReadFromSave(metaReader, false, true);
            allItemsInBank.Add(create);
            //if (Debug.isDebugBuild) Debug.Log("Finished reading shared bank item: " + create.actorRefName + " " + create.displayName + " " + create.actorUniqueID);
        }

        return true;
    }

    public static int corralIDAssigner = 900000;
    public const int CORRAL_ID_ASSIGNER_BASE = 900000;

    static bool TryReadMonsterFromSave(XmlReader metaReader, bool readingAtTitleScreen)
    {
        if (readingAtTitleScreen) return false;
        Monster m = new Monster();
        bool successRead = false;

        try
        {
            m.ReadFromSave(metaReader, false);
            successRead = true;
        }
        catch(Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Could not read monster due to: " + e);
        }
            
        if (m.tamedMonsterStuff != null && successRead)
        {
            foreach(TamedCorralMonster tcm in SharedCorral.tamedMonstersSharedWithAllSlots)
            {
                if (tcm.sharedBankID == m.tamedMonsterStuff.sharedBankID)
                {
                    if (Debug.isDebugBuild) Debug.Log("Found a tamed monster in the shared bank with the same shared bank ID as the one we just read. This is a problem.");
                    return true;
                }                
            }

            if (Debug.isDebugBuild) Debug.Log("Assigning " + corralIDAssigner + " to monster " + m.tamedMonsterStuff.sharedBankID + "," + m.displayName);
            GameMasterScript.AssignActorID(m, corralIDAssigner);
            corralIDAssigner++;                                    
            SharedCorral.tamedMonstersSharedWithAllSlots.Add(m.tamedMonsterStuff);
            if (Debug.isDebugBuild) 
            {
                Debug.Log("We have read shared monster " + m.PrintCorralDebug() + " from shared progress. There are now " + SharedCorral.tamedMonstersSharedWithAllSlots.Count + " pets.");
            }

            GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(SharedCorral.tamedMonstersSharedWithAllSlots.Count);
            return true;

            /*
            bool addToCorral = false;
            bool alreadyInCorral = false;
            foreach (TamedCorralMonster tcm in SharedCorral.tamedMonstersSharedWithAllSlots)
            {

                addToCorral = false;
                continue;

                //if (Debug.isDebugBuild) Debug.Log("Let's comapre read monster " + m.actorRefName + "," + m.displayName +"," + m.actorUniqueID + " to " + tcm.monsterObject.actorRefName + "," + tcm.monsterObject.displayName + "," + tcm.monsterObject.actorUniqueID);
                Actor act = tcm.monsterObject;
                if (act.actorUniqueID == m.actorUniqueID 
                    && act.actorRefName == m.actorRefName 
                    && act.displayName == m.displayName)
                {
                    if (Debug.isDebugBuild) Debug.Log(act.actorUniqueID + " " + act.actorRefName + " already in corral, not adding them again during load phase.");
                    addToCorral = false;
                    alreadyInCorral = true;
                    break;
                }
            }
            
            if (addToCorral && !alreadyInCorral)
            {
                int oldID = m.actorUniqueID;                
                // Update this monster ID and THEN add it to corral
                GameMasterScript.AssignActorID(m, corralIDAssigner);
                corralIDAssigner++;
                // Do we need to check anything else?
                addToCorral = true;

                if (Debug.isDebugBuild) Debug.Log("Updating unique ID for " + m.actorRefName + ", was " + oldID + " is now " + m.actorUniqueID);
            }

            if (addToCorral) // eventually we will need to merge everything in.
            {
                if (Debug.isDebugBuild) Debug.Log("Finished reading monster " + m.actorRefName + " " + m.actorUniqueID + " from shared progress, prefab is " + m.prefab);
                SharedCorral.tamedMonstersSharedWithAllSlots.Add(m.tamedMonsterStuff);
                GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(SharedCorral.tamedMonstersSharedWithAllSlots.Count);
            } */
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Failed to read tamed monster component from MetaProgress creature.");
        } 

        return true;
    }

    static bool TryReadMonsterTemplateFromSave(XmlReader metaReader, bool readingAtTitleScreen)
    {
        if (readingAtTitleScreen) return false;

        MonsterTemplateData mtd = null;
        bool success = true;
        try { mtd = MonsterTemplateSerializer.ReadCustomMonsterFromSave(metaReader); }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.LogError("Could not read MD monster from meta file because: " + e);
            success = false;
        }

        if (success)
        {
            if (Debug.isDebugBuild) Debug.Log("Read special template " + mtd.refName + " from shared file");
            SharedCorral.customMonsterTemplatesUsedByPets.Add(mtd);

            GameMasterScript.masterMonsterList.Add(mtd.refName, mtd);            
        }
        return false;
    }

    public static void OnFileDeleted(int slot)
    {
        if (saveSlotsClearedAndAwaitingRelicFlush == null) saveSlotsClearedAndAwaitingRelicFlush = new List<int>();

        if (Debug.isDebugBuild) Debug.Log("SharedBank notes that a file in slot " + slot + " was deleted.");

        if (saveSlotsClearedAndAwaitingRelicFlush.Contains(slot)) return;

        saveSlotsClearedAndAwaitingRelicFlush.Add(slot);
    }

    public static void DeleteSharedBankDueToCriticalFailure()
    {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        string globalSaveSlotDataPath = CustomAlgorithms.GetPersistentDataPath() + "/shareddata.xml";
        if (!File.Exists(globalSaveSlotDataPath)) return;

        File.Copy(globalSaveSlotDataPath, globalSaveSlotDataPath + "corrupt", true);
        File.Delete(globalSaveSlotDataPath);
        Debug.Log("Deleted shared bank due to critical error.");
#else
    string globalSaveSlotDataPath = "shareddata.xml";
#endif



    }
}
