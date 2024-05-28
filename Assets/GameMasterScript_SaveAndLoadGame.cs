using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.IO;
using Rewired;
using UnityEngine.SceneManagement;
using System.Text;

public enum SaveGameState { NOT_SAVING, SAVE_IN_PROGRESS, SAVE_SUCCEEDED, SAVE_FAILED, COUNT }

public partial class GameMasterScript : MonoBehaviour {

    SaveGameState currentSaveGameState;
    public SaveGameState CurrentSaveGameState
    {
        get
        {
            return currentSaveGameState;
        }
        set
        {
            //if (Debug.isDebugBuild) Debug.Log("Setting current save game state to " + value + ", was " + currentSaveGameState);
            currentSaveGameState = value;
        }
    }

    private IEnumerator TrySaveMetaProgress()
    {
        TDPlayerPrefs.Save();
        PlayerOptions.WriteOptionsToFile();
        yield return MetaProgressScript.SaveMetaProgress(false);

        //Debug.Log("Saved meta progress.");
    }

    private void TrySaveMinimalMetaProgress()
    {
        TDPlayerPrefs.Save();


#if !UNITY_SWITCH
        PlayerOptions.WriteOptionsToFile();
#endif

        MetaProgressScript.SaveMinimalMetaProgress();

        if (Debug.isDebugBuild) Debug.Log("Saved meta progress.");
    }

    public IEnumerator TrySaveGame(bool autoSave = false)
    {

#if UNITY_SWITCH && !UNITY_EDITOR
        // Don't allow overly fast saves


// This next line prevents the user from quitting the game while saving. 
// This is required for Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif


        float timeSaveStart = Time.realtimeSinceStartup;

        // Here we go.       

        ReInput.userDataStore.Save();

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        if (Debug.isDebugBuild) Debug.Log("About to try saving shared progress.");
        yield return SharedBank.TrySaveSharedProgress();

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        yield return TrySaveMetaProgress();

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

#if UNITY_SWITCH
        StringBuilder sbData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter writer = XmlWriter.Create(sbData, xmlSettings);
#elif UNITY_PS4
        StringBuilder sbData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter writer = XmlWriter.Create(sbData, xmlSettings);
#elif UNITY_XBOXONE
        StringBuilder sbData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter writer = XmlWriter.Create(sbData, xmlSettings);
#else

        try
        {
            JournalScript.TryWriteLogToDisk();
        }
        catch (Exception e)
        {
            Debug.Log("Failed to write combat log to disk: " + e);
        }

        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGameCopy.xml";
        File.Delete(path);
        //Debug.Log("Saving to " + path);

        XmlWriter writer = XmlWriter.Create(path);
#endif
        yield return mms.WriteToSave();

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        writer.WriteStartDocument();
        writer.WriteStartElement("DOCUMENT");

        writer.WriteElementString("GameVersion", GAME_BUILD_VERSION.ToString());

        // Content goes below

        // Save the hero
        heroPCActor.WriteToSave(writer);

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        // Save the dungeon contents
        List<Actor> deadActorsToRemove = new List<Actor>();

        // Write out special map data if we have any
        foreach (var kvp in MapMasterScript.dictAllMaps)
        {
            kvp.Value.WriteSpecialMapDataToSave(writer);
        }

        // Save special actors; dead guys that have live summons, for example
        for (int i = 0; i < deadActorsToSaveAndLoad.Count; i++)
        {
            deadActorsToSaveAndLoad[i].MarkAsDestroyed();
            //Debug.Log("Writing " + deadActorsToSaveAndLoad[i].actorRefName + " is saving to disk");
            deadActorsToSaveAndLoad[i].WriteToSave(writer);
        }

        /* foreach(Actor act in deadActorsToRemove)
        {
            deadActorsToSaveAndLoad.Remove(act);
        } */

        for (int i = 0; i < MapMasterScript.theDungeon.maps.Count; i++)
        {
            //Debug.Log("Saving actors on map floor " + MapMasterScript.theDungeon.maps[i].floor);
            Destructible dt;
            foreach (Actor act in MapMasterScript.theDungeon.maps[i].actorsInMap)
            {
                if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    dt = act as Destructible;
                    bool isSummon = false;
                    if ((dt.maxTurnsToDisappear > 0 && dt.turnsToDisappear > 0) || dt.summoner != null)
                    {
                        isSummon = true;
                    }
                    if (!isSummon && (dt.mapObjType == SpecialMapObject.WATER || dt.mapObjType == SpecialMapObject.LAVA || dt.mapObjType == SpecialMapObject.ISLANDSWATER
                        || dt.mapObjType == SpecialMapObject.MUD || dt.mapObjType == SpecialMapObject.ELECTRIC))
                    {
                        continue;
                    }
                }
                if (act != heroPCActor)
                {
                    // If we are NOT in hardcore/ronin/challenge mode, 
                    // We don't actually want to save local copies of pets in the corral
                    // Because the 'latest version' will be pulled from shared progress on load anyway
                    if (MapMasterScript.theDungeon.maps[i].floor == MapMasterScript.TOWN2_MAP_FLOOR)
                    {
                        if (act.IsCorralPetAndInCorral())
                        {
                            continue;
                        }
                    }
                    act.WriteToSave(writer);
                }
            }

            if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeSaveStart = Time.realtimeSinceStartup;
            }
        }

        // Save reference to the NPC
        writer.WriteElementString("turn", turnNumber.ToString());

        writer.WriteStartElement("wanderingmerchant");
        if (wanderingMerchantInTown == null)
        {
            writer.WriteElementString("exists", "false");
        }
        else
        {
            writer.WriteElementString("exists", "true");
            writer.WriteElementString("id", wanderingMerchantInTown.actorUniqueID.ToString());
            writer.WriteElementString("turns", durationOfWanderingMerchant.ToString());
        }
        writer.WriteEndElement();

        // Save hotbars etc.
        uims.WriteToSave(writer);
        // Content ends

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        MusicManagerScript.WriteMusicDataToSave(writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Close();

#if UNITY_PS4
        //convert string build to byte
        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(sbData.ToString());
        
        PS4SaveManager.instance.SaveData(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml", myByte);
#endif

#if UNITY_XBOXONE
        XboxSaveManager.instance.SetString("savedGame" + GameStartData.saveGameSlot + ".xml", sbData.ToString());
        XboxSaveManager.instance.Save();
#endif

#if UNITY_SWITCH
        var saveDataHandler = Switch_SaveDataHandler.GetInstance();
        //Note -- no longer saving to copy here, but rather the save slot directly.
        saveDataHandler.SaveSwitchFile(sbData.ToString(), "savedGame" + GameStartData.saveGameSlot + ".xml");
#endif

#if UNITY_SWITCH && !UNITY_EDITOR
// This next line prevents the user from quitting the game while saving. 
// This is required for Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
        Switch_SaveIconDisplay.Hide();
#endif      

        CurrentSaveGameState = SaveGameState.SAVE_SUCCEEDED;

#if UNITY_PS4
        //after we save - make back up
        //Debug.LogError("Backup");
        yield return PS4SaveManager.instance.BackupCoroutine(PS4SaveManager.ROOT_DIR);
#endif

        OnSaveCompleted();
    }

    float timeAtLastAutoSave;
    public void SaveTheGame(bool autoSave)
    {
        if (Debug.isDebugBuild) Debug.Log("Request save the game at " + Time.time + ", last time was " + timeAtLastAutoSave + ". Autosaving now? " + autoSave);
#if UNITY_SWITCH
        // To comply with Switch guidelines, we will only *autosave* if it has been at least 5 minutes since the last
        if (autoSave)
        {
            if (Time.time - timeAtLastAutoSave < 300)
            {
                if (Debug.isDebugBuild) Debug.Log("Too soon since last save. Not autosaving.");
                return;
            }
            timeAtLastAutoSave = Time.time;
        }
#endif
        Switch_SaveIconDisplay.Show();
        currentSaveGameState = SaveGameState.SAVE_IN_PROGRESS;
        StartCoroutine(TrySaveGame(autoSave));
    }

    public IEnumerator ISaveTheGame(bool autoSave)
    {
        if (Debug.isDebugBuild) Debug.Log("Request IEnumerator save the game at " + Time.time + ", last time was " + timeAtLastAutoSave + ". Autosaving now? " + autoSave);
#if UNITY_SWITCH
        // To comply with Switch guidelines, we will only *autosave* if it has been at least 5 minutes since the last
        if (autoSave)
        {
            if (Time.time - timeAtLastAutoSave < 300)
            {
                //if (Debug.isDebugBuild) Debug.Log("Too soon since last save. Not autosaving.");
                yield break;
            }
            timeAtLastAutoSave = Time.time;
        }
#endif
        Switch_SaveIconDisplay.Show();
        currentSaveGameState = SaveGameState.SAVE_IN_PROGRESS;
        yield return TrySaveGame(autoSave);
    }

    public void OnSaveCompleted()
    {

        if (CurrentSaveGameState == SaveGameState.SAVE_SUCCEEDED)
        {
            //if (Debug.isDebugBuild) Debug.Log("Successfully saved game! Time: " + Time.time);

#if !UNITY_SWITCH && !UNITY_PS4  && !UNITY_XBOXONE 
            string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGameCopy.xml";
            string path2 = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";

            try 
            {
                File.Copy(path, path2, true);
            }
            catch(Exception e)
            {
                Debug.Log("Could not make savedGame backup because: " + e);
            }            

            if (GameStartData.challengeType != ChallengeTypes.NONE)
            {
                TDSecurity.UpdateFileHash(path2);
            }

            path = CustomAlgorithms.GetPersistentDataPath() + "/metaprogressCopy.xml";
            path2 = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";
            
            try 
            {
                File.Copy(path, path2, true);
            }
            catch(Exception e)
            {
                Debug.Log("Could not make metaprogress backup because: " + e);
            }            
            
            path = CustomAlgorithms.GetPersistentDataPath() + "/savedMapCopy.dat";
            path2 = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";

            //Debug.Log("Pre Map status lock: " + CustomAlgorithms.IsFileLocked(path2));
            //Debug.Log("Pre Map copy status lock: " + CustomAlgorithms.IsFileLocked(path));

            try 
            {
                File.Copy(path, path2, true);
            }
            catch(Exception e)
            {
                Debug.Log("Could not make savedmapcopy backup because: " + e);
            }           

            //Debug.Log("Map status lock: " + CustomAlgorithms.IsFileLocked(path2));
            //Debug.Log("Map copy status lock: " + CustomAlgorithms.IsFileLocked(path));
#endif
        }

        Switch_SaveIconDisplay.Hide();
        currentSaveGameState = SaveGameState.NOT_SAVING;
    }

    public static IEnumerator LoadCoreData(string path, bool dontChangeGameState = false)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

#if UNITY_SWITCH
        var sdh = Switch_SaveDataHandler.GetInstance();
        yield return sdh.LoadSwitchSavedDataFileAsync(path, false);
        if (Debug.isDebugBuild) Debug.Log("The save file was loaded, hurray: " + path);

        byte[] loadedBytes = null;
        Switch_SaveDataHandler.GetBytesLoadedAsync(path, ref loadedBytes);
        var memStream = new MemoryStream(loadedBytes);
        var binaryReader = new BinaryReader(memStream);
        var sReader = new StringReader(binaryReader.ReadString());
        XmlReader reader = XmlReader.Create(sReader, settings);
#elif UNITY_PS4
        byte[] loadedBytes = null;        
        PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, path, out loadedBytes);
        string strLoadedData = System.Text.Encoding.UTF8.GetString(loadedBytes);
       
        XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#elif UNITY_XBOXONE                       
        string strLoadedData = XboxSaveManager.instance.GetString(path);

        XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#else
        FileStream stream = new FileStream(path, FileMode.Open);
        XmlReader reader = XmlReader.Create(stream, settings);
#endif

        try { reader.Read(); } // XML
        catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log("SERIOUS ERROR: Meta progress in slot " + GameStartData.saveGameSlot + " is corrupted.");
            reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            stream.Close();
#endif
            File.Delete(path);

            yield break;
        }
        reader.Read(); // DOCUMENT
        reader.Read(); // GAMEVERSION
        string builder = "";
        string playerName = "";
        int lowestFloor = 0;
        int daysPassed = 0;
        int level = 0;
        bool randomJobMode = false;
        string currentJob = "";
        int ngp = 0;
        bool beatTheGame = false;
        int iterations = 0;
        string strMapName = "";

        Sprite portraitSprite = null;

        string strPortraitSprite = "BrigandPortrait";    // Defaultarino
        GameModes gMode = GameModes.NORMAL;
        bool finishReadingImmediately = false;
        bool readBufferedData = false;
        while (reader.NodeType != XmlNodeType.EndElement || (reader.NodeType == XmlNodeType.EndElement && reader.Name != "hero"))
        {
            string txt;
            switch (reader.Name) // No longer ToLowerInvariant as it becomes VERY costly.
            {
                case "buffereddata":
                    string text = reader.ReadElementContentAsString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        strAsyncLoadOutput = text;
                        ParseHeroBufferedAsyncLoadOutput(text);
                        ngp = saveDataBlockAsyncLoadOutput.iNewGamePlusRank;
                        GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] = ngp;
                        GameStartData.beatGameStates[GameStartData.saveGameSlot] = saveDataBlockAsyncLoadOutput.bGameClearSave;                        

                        finishReadingImmediately = true;
                        readBufferedData = true;
                        break;
                    }
                    break;
                case "hero":
                    reader.ReadStartElement();
                    break;
                case "expansions":
                    GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot].Clear();
                    DLCManager.ParseSavedPlayerExpansionsIntoList(GameStartData.dlcEnabledPerSlot[GameStartData.saveGameSlot], reader.ReadElementContentAsString());
                    break;
                case "playermodsactive":
                    GameStartData.modsEnabledPerSlot[GameStartData.saveGameSlot].Clear();
                    PlayerModManager.ParseSavedPlayerModsIntoList(GameStartData.modsEnabledPerSlot[GameStartData.saveGameSlot], reader.ReadElementContentAsString());
                    break;
                case "newgameplus":
                    ngp = reader.ReadElementContentAsInt();
                    GameStartData.saveSlotNGP[(int)GameStartData.saveGameSlot] = ngp;
                    //Debug.Log(path + " ngP? " + ngp);
                    break;
                case "name":
                case "displayname":
                    playerName = reader.ReadElementContentAsString();
                    //Debug.Log(playerName);
                    break;
                case "randomjob":
                    randomJobMode = true;
                    reader.ReadElementContentAsString();
                    break;
                case "lowestfloorexplored":
                    lowestFloor = reader.ReadElementContentAsInt();
                    break;
                case "dayspassed":
                    daysPassed = reader.ReadElementContentAsInt();
                    break;
                case "playtime":
                    txt = reader.ReadElementContentAsString();
                    GameStartData.playTimeInSeconds = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "lv":
                case "lvl":
                case "level":
                    level = reader.ReadElementContentAsInt();
                    GameStartData.saveSlotLevels[(int)GameStartData.saveGameSlot] = level;
                    finishReadingImmediately = true;
                    break;
                case "bg":
                    beatTheGame = reader.ReadElementContentAsBoolean();
                    GameStartData.beatGameStates[(int)GameStartData.saveGameSlot] = beatTheGame;
                    //Debug.Log(path + " beat game? " + beatTheGame);
                    break;
                case "currentjob":
                    currentJob = reader.ReadElementContentAsString();
                    string dispJob = currentJob.ToUpperInvariant();
                    CharacterJobs jobEnum = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), dispJob);
                    currentJob = GameMasterScript.characterJobNames[(int)jobEnum];

                    portraitSprite = CharacterJobData.GetJobDataByEnum((int)jobEnum).PortraitSprite;

                    strPortraitSprite = CharacterJobData.GetJobDataByEnum((int)jobEnum).portraitSpriteRef;

                    //Track Shara-mode status
                    GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = jobEnum == CharacterJobs.SHARA;
                    if (jobEnum == CharacterJobs.SHARA && (TDPlayerPrefs.GetInt(GlobalProgressKeys.SHARA_CAMPAIGN_UNLOCKED) == 0 ||
                        SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE)))
                    {
                        TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_CAMPAIGN_UNLOCKED, 1);
                        SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE);
                    }
                    break;
                case "advm": // Change game mode because we're reading it from save.
                    bool advMode = reader.ReadElementContentAsBoolean();
                    if (advMode)
                    {
                        if (gmsSingleton != null && !dontChangeGameState)
                        {
                            GameStartData.ChangeGameMode(GameModes.ADVENTURE);
                            gmsSingleton.gameMode = GameModes.ADVENTURE;
                            gmsSingleton.adventureModeActive = true;
                        }
                        gMode = GameModes.ADVENTURE;
                        if (heroPCActor != null) heroPCActor.SetActorData("advm", 1);
                    }
                    break;
                case "challenge":
                    GameStartData.challengeTypeBySlot[(int)GameStartData.saveGameSlot] = (ChallengeTypes)Enum.Parse(typeof(ChallengeTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "selectedprefab":
                    reader.ReadElementContentAsString();
                    break;
                case "seed":
                    reader.ReadElementContentAsInt();
                    break;
                case "mode":
                    if (gmsSingleton != null && !dontChangeGameState)
                    {
                        gmsSingleton.gameMode = (GameModes)Enum.Parse(typeof(GameModes), reader.ReadElementContentAsString().ToUpperInvariant());
                        GameStartData.ChangeGameMode(gmsSingleton.gameMode);
                        if (gmsSingleton.adventureModeActive)
                        {
                            gmsSingleton.gameMode = GameModes.ADVENTURE;
                            gMode = GameModes.ADVENTURE;
                        }
                    }
                    else
                    {
                        gMode = (GameModes)Enum.Parse(typeof(GameModes), reader.ReadElementContentAsString().ToUpperInvariant());
                    }

                    GameStartData.gameModesSelectedBySlot[GameStartData.saveGameSlot] = gMode;

                    break;
                case "mapname":
                    strMapName = reader.ReadElementContentAsString();
                    break;
                case "portraitsprite":
                    strPortraitSprite = reader.ReadElementContentAsString();
                    break;
                case "cr":
                    string coreParse = reader.ReadElementContentAsString();
                    if (string.IsNullOrEmpty(strMapName)) TryGetMapNameFromCoreData(coreParse);
                    break;
                case "sts":
                case "stats":
                default:
                    reader.Read();
                    break;
            }
            if (finishReadingImmediately)
            {
                break;
            }
        }

        //Debug.Log("Time to read end element."); // Another breakpoint here.

        if (reader.NodeType == XmlNodeType.EndElement)
        {
            reader.ReadEndElement();
        }

        reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        stream.Close();
#endif

        if (!readBufferedData)
        {
            saveDataBlockAsyncLoadOutput.Clear();
            saveDataBlockAsyncLoadOutput.strHeroName = playerName;
            saveDataBlockAsyncLoadOutput.strJobName = randomJobMode ? StringManager.GetString("job_wanderer") : currentJob;
            saveDataBlockAsyncLoadOutput.iHeroLevel = level;
            if (string.IsNullOrEmpty(strMapName))
            {
                strMapName = StringManager.GetString("loc_unknown");
            }
            saveDataBlockAsyncLoadOutput.strLocation = strMapName;
            saveDataBlockAsyncLoadOutput.strTimePlayed = MetaProgressScript.GetDisplayPlayTime(true, GameStartData.playTimeInSeconds);

            if (portraitSprite == null) saveDataBlockAsyncLoadOutput.portrait = UIManagerScript.GetPortraitForDialog(strPortraitSprite)[0];
            else saveDataBlockAsyncLoadOutput.portrait = portraitSprite;

            saveDataBlockAsyncLoadOutput.strGameModeInfo = GetGameModeString(gMode);

            string txtBuilder = ConstructHeroSaveStringFromData(playerName, randomJobMode, currentJob, level, beatTheGame, lowestFloor, daysPassed, MetaProgressScript.GetDisplayPlayTime(false, 0f), gMode, ngp, strMapName, strPortraitSprite);

            strAsyncLoadOutput = txtBuilder;
        }

        yield break;
    }

    public void LoadOnlyHeroData(bool doSafely = false, bool loadPreMysteryData = false)
    {
        UIManagerScript.SetToBlack();
        PlayingCard.Reset();
        if (dictAllActors == null)
        {
            dictAllActors = new Dictionary<int, Actor>();
            allLoadedNPCs = new List<NPC>();
        }
        ClearActorDict();
        GameObject go = GameObject.Find("BattleTextManager");
        if (go == null)
        {
            Debug.Log("<color=red>BTM not found on load.</color>");
            return;
        }
        go.GetComponent<BattleTextManager>().InitializeBTM();
#if UNITY_SWITCH
        var saveDataHandler = Switch_SaveDataHandler.GetInstance();
        string strLoadedData = "";
        if (!saveDataHandler.LoadSwitchDataFile(ref strLoadedData, "savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            // No game to load.
            if (Debug.isDebugBuild) Debug.Log("No game to load in slot " + GameStartData.saveGameSlot);
            return;
        }
        

#elif UNITY_PS4
        byte[] byteLoadedData = null;
        StringReader sReaderLoadedData;       
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            // No game to load.
            if (Debug.isDebugBuild) Debug.Log("No game to load in slot " + GameStartData.saveGameSlot);
            return;
        }
        else
        {           
            PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml", out byteLoadedData);
            //convert byte to StringReader
            string strLoadedData = System.Text.Encoding.UTF8.GetString(byteLoadedData);
            
            sReaderLoadedData = new StringReader(strLoadedData);
        }
#elif UNITY_XBOXONE        
        StringReader sReaderLoadedData;
        if (!XboxSaveManager.instance.HasKey("savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            // No game to load.
            if (Debug.isDebugBuild) Debug.Log("No game to load in slot " + GameStartData.saveGameSlot);
            return;
        }
        else
        {
            string strLoadedData = XboxSaveManager.instance.GetString("savedGame" + GameStartData.saveGameSlot + ".xml");
            sReaderLoadedData = new StringReader(strLoadedData);
        }
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
        if (!File.Exists(path))
        {
            // No game to load.
            Debug.Log("<color=red>No game to load.</color>");
            return;
        }
        if (File.Exists(path))
        {
            //Debug.Log("Begin load only hero data from path " + path);
            FileStream stream = new FileStream(path, FileMode.Open);
#endif

        XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
        // using (XmlReader reader = XmlReader.Create(new StringReader(dungeonXML.text), settings))

#if UNITY_SWITCH
            XmlReader reader = XmlReader.Create(new StringReader(strLoadedData), settings);
#elif UNITY_PS4
            XmlReader reader = XmlReader.Create(sReaderLoadedData, settings);
#elif UNITY_XBOXONE
        XmlReader reader = XmlReader.Create(sReaderLoadedData, settings);
#else
            XmlReader reader = XmlReader.Create(stream, settings);
#endif

        reader.Read();

            //Debug.Log(reader.Name);

            reader.Read();
            //Debug.Log(reader.Name);
            reader.Read();
            //Debug.Log(reader.Name);

            if (reader.Name != "GameVersion")
            {
                Debug.Log("Can't load old save data structure.");
                reader.Close();
                LoadMainScene();
                return;
            }

            // Now ONLY read hero.            

            CreateHeroPC();
            Debug.Log("<color=yellow>Hero has been created from loaded data.</color>");
            heroPCActor.HeroStart(false);

            BattleTextManager.InitializeTextObjectSources();

            heroPCActor.ReadFromSave(reader, loadPreMysteryData);
            gmsSingleton.UpdateHeroObject();
            UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, false);
            if (heroPCActor.myStats.CheckHasStatusName("status_scavenger"))
            {
                playerIsScavenger = true;
            }

            heroPCActor.myQuests.Clear();
            heroPCActor.SetBattleDataDirty();

            int petID = heroPCActor.GetMonsterPetID();

            Debug.Log("Pet ID appears to be " + petID);

            if (petID > 0)
            {
                Monster pet = null;
                Debug.Log("Player has a pet of some kind, id: " + petID + " Must load it and return to meta files.");
                bool foundPet = false;
                while (!foundPet)
                {
                    if (reader.Name == "ui")
                    {
                        Debug.LogError("<color=red>Could not find pet...</color>");
                        heroPCActor.ResetPetData();
                        break;
                    }
                    if (reader.Name != "mn" || reader.NodeType == XmlNodeType.EndElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        Monster mon = new Monster();
                        mon.ReadFromSave(reader, false, false); // Temp read that doesn't add to any dicts.
                        if (mon.actorUniqueID == petID)
                        {
                            Debug.Log("<color=green>Found player pet! " + mon.PrintCorralDebug() + " Returning it to meta list; adding to corral.</color>");
                            if (mon.tamedMonsterStuff == null)
                            {
                                Debug.Log("But it has no TCM?");
                                continue;
                            }
                            mon.SetActorData("ngpluspet", 1);
                            MetaProgressScript.AddPetToLocalSlotCorralList(mon.tamedMonsterStuff);
                            int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
                            gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);
                            heroPCActor.ResetPetData();

                            heroPCActor.summonedActorIDs.Remove(mon.actorUniqueID);
                            heroPCActor.summonedActors.Remove(mon);
                            foundPet = true;
                            break;
                        }
                    }
                }
            }

            while (reader.Name != "ui")
            {
                reader.Read();
            }

            uims.ReadFromSave(reader);

            bufferedHotbarActions = new HotbarBindable[(int)UIManagerScript.hotbarAbilities.Length];
            for (int i = 0; i < UIManagerScript.hotbarAbilities.Length; i++)
            {
                bufferedHotbarActions[i] = new HotbarBindable();
                bufferedHotbarActions[i].actionType = UIManagerScript.hotbarAbilities[i].actionType;
                bufferedHotbarActions[i].ability = UIManagerScript.hotbarAbilities[i].ability;
                bufferedHotbarActions[i].consume = UIManagerScript.hotbarAbilities[i].consume;
            }

            reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            stream.Close();
#endif
            Debug.Log("Finished loading ONLY hero data for NG+ or map rebuild.");
            if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.REBUILDMAPS)
            {
                Debug.Log("Rebuilding maps...");
                heroPCActor.summonedActors.Clear();
            }

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        }
#endif
    }

    public void LoadOnlyHeroDataAndReturnHeroPetToCorral()
    {
        
    }

    public IEnumerator TryLoadGame()
    {
        int loadThreadIndex = GlobalLoadThreadIndex;
        GlobalLoadThreadIndex++;
        //Set this to a value more than five minutes ago to assure the next autosave works.
        lastAutoSaveTime = Time.realtimeSinceStartup - 301.0;

        SetLoadingBarToLoadGameOnlyMode();

        //Debug.Log("Loading game.");
        UIManagerScript.SetToBlack();
        bufferedHotbarActions = null;
        PlayingCard.Reset(); // Initialize the gambler deck.
        if (dictAllActors == null)
        {
            dictAllActors = new Dictionary<int, Actor>();
            allLoadedNPCs = new List<NPC>();
        }
        // Don't clear here, this messes up meta loading.

        GameObject go = GameObject.Find("BattleTextManager");
        if (go == null)
        {
            Debug.Log("BTM not found on load.");
            yield break;
        }
        go.GetComponent<BattleTextManager>().InitializeBTM();

#if UNITY_SWITCH
        var saveDataHandler = Switch_SaveDataHandler.GetInstance();
        string strLoadData = "";
        if( !saveDataHandler.LoadSwitchDataFile(ref strLoadData, "savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            Debug.Log("NO GAME TO LOAD!");
            // No game to load.
            SetLoadStatusDueToErrorInGameLoad();

            yield break;
        }

        mms.LoadFromSave(); // will this break??

        MetaProgressScript.LinkAllActors();

#elif UNITY_PS4
        byte[] byteLoadedData = null;
        string strLoadedData = "";
        StringReader sReaderLoadedData;        
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            Debug.Log("NO GAME TO LOAD!");
            // No game to load.
            SetLoadStatusDueToErrorInGameLoad();

            yield break;
        }        

        mms.LoadFromSave(); // will this break??

        MetaProgressScript.LinkAllActors();
#elif UNITY_XBOXONE        
        string strLoadedData = "";
        StringReader sReaderLoadedData;
        if (!XboxSaveManager.instance.HasKey("savedGame" + GameStartData.saveGameSlot + ".xml"))
        {
            Debug.Log("NO GAME TO LOAD!");
            // No game to load.
            SetLoadStatusDueToErrorInGameLoad();

            yield break;
        }

        mms.LoadFromSave(); // will this break??

        MetaProgressScript.LinkAllActors();
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
        if (!File.Exists(path))
        {
            // No game to load.
            Debug.Log("No game to load.");
            yield break;
        }

        string mapPath = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";

        if (File.Exists(path))
        {

            FileStream stream = new FileStream(path, FileMode.Open);
#endif
        XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            // using (XmlReader reader = XmlReader.Create(new StringReader(dungeonXML.text), settings))

#if UNITY_SWITCH
            XmlReader reader = XmlReader.Create(new StringReader(strLoadData), settings);

#elif UNITY_PS4           
            PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml", out byteLoadedData);
            //convert byte to StringReader
            strLoadedData = System.Text.Encoding.UTF8.GetString(byteLoadedData);            

            sReaderLoadedData = new StringReader(strLoadedData);
            
            XmlReader reader = XmlReader.Create(sReaderLoadedData, settings);
#elif UNITY_XBOXONE                                   
            strLoadedData = XboxSaveManager.instance.GetString("savedGame" + GameStartData.saveGameSlot + ".xml");           

            sReaderLoadedData = new StringReader(strLoadedData);
            
            XmlReader reader = XmlReader.Create(sReaderLoadedData, settings);
#else
            XmlReader reader = XmlReader.Create(stream, settings);
#endif
            reader.Read();
            reader.Read();
            reader.Read();

            if (reader.Name != "GameVersion")
            {
                SetLoadStatusDueToErrorInGameLoad();
                Debug.Log("Can't load old save data structure.");
                reader.Close();
                SceneManager.LoadScene("Main");
                yield break;
            }

            int loadGameVer = reader.ReadElementContentAsInt();
            GameStartData.loadGameVer = loadGameVer;

            if (Debug.isDebugBuild) Debug.Log("Attempt reading from shared bank in load game routine.");
            yield return SharedBank.ReadFromSave(false);

            Debug.Log("<color=green>Loaded game ver is " + loadGameVer + "</color>");
            if (loadGameVer != GAME_BUILD_VERSION)
            {
                Debug.Log("Loading file from older game version, watch for potential issues. " + loadGameVer);
                if (loadGameVer < 107)
                {
                Debug.Log("Load game ver mismatch of " + loadGameVer + " vs " + GAME_BUILD_VERSION);
                    // Kick it back to title screen.
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
                    reader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
                    stream.Close();
#endif
                    StartCoroutine(RebuildMapsAfterResourcesLoad());
                    yield break;
                }
                /* if (loadGameVer < 151)
                {
                    PlayerOptions.disableMouseOnKeyJoystick = false;
                } */
            }

            // NEW LOCATION OF MAP BLOCK to verify map structure

            // mapsLoadedSuccessfully is now a class variable
            // The LoadFromSave coroutine will mark it "true" if all goes well.

            //bool mapsLoadedSuccessfully = mms.LoadFromSave();

#if UNITY_EDITOR
            float timeAtMapLoadStart = Time.realtimeSinceStartup;
#endif

            yield return mms.LoadFromSave();

#if UNITY_EDITOR
            //Debug.Log("Time to load all map data: " + (Time.realtimeSinceStartup - timeAtMapLoadStart));
#endif

            if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.REBUILDMAPS && loadThreadIndex == GlobalLoadThreadIndex - 1)
            {
                yield break;
            }

            MetaProgressScript.LinkAllActors();

            CreateHeroPC();
            heroPCActor.HeroStart(false);

            BattleTextManager.InitializeTextObjectSources();

            try
            {
                heroPCActor.ReadFromSave(reader);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log("COULD NOT LOAD HERO PC.");
                SetLoadStatusDueToErrorInGameLoad();
                yield break;
            }
            gmsSingleton.UpdateHeroObject();

            heroPCActor.ValidateAndFixStats(true); // New 11/12/17 to address weird unknown stat bonuses.

            UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, false);
            if (heroPCActor.myStats.CheckHasStatusName("status_scavenger"))
            {
                playerIsScavenger = true;
            }

            GameStartData.gameInSharaMode = heroPCActor.myJob.jobEnum == CharacterJobs.SHARA;
            GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = GameStartData.gameInSharaMode;
            if (GameStartData.gameInSharaMode)
            {
                DLCManager.SetLastPlayedCampaign(StoryCampaigns.SHARA);
            }
            else
            {
                DLCManager.SetLastPlayedCampaign(StoryCampaigns.MIRAI);
            }


            //Debug.Log("Hero loading on floor: " + heroPCActor.GetActorMap().floor + " vs " + heroPCActor.dungeonFloor);

            if (heroPCActor.GetActorMap() == null) // Catastrophic save error in progress, we're just trying to salvage it now.
            {
                heroPCActor.SetActorMap(MapMasterScript.singletonMMS.townMap);
                heroPCActor.SetPos(new Vector2(9f, 3f));
                heroPCActor.SetCurPos(new Vector2(9f, 3f));
                Debug.Log("Couldn't connect hero to any map, so adding back to town... THIS IS BAD.");
            }

            if (heroPCActor.GetActorMap() != null)
            {
                heroPCActor.GetActorMap().AddActorToMap(heroPCActor);
                heroPCActor.GetActorMap().AddActorToLocation(heroPCActor.GetPos(), heroPCActor);
                heroPCActor.myMovable.SetPosition(heroPCActor.GetPos());
                heroPCActor.myMovable.SetInSightAndSnapEnable(true);

                loadGame_stairsToTryRelinking = new List<Stairs>();
            }

            if (!mapsLoadedSuccessfully)
            {
                if (Debug.isDebugBuild) Debug.Log("Maps were not loaded successfully. Rebuilding them.");
                gameLoadSequenceCompleted = false;
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.REBUILDMAPS);
                yield break;
            }

            bool errorOccurred = false;
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE
            //"stream" isn't used by the next function, and is only passed into the function after next
            //to have it closed.
            try { StartCoroutine(ContinueGameFromSave(reader, null)); }
#else
            try
            {
                StartCoroutine(ContinueGameFromSave(reader, stream));
            }
#endif
            catch (Exception e)
            {
                Debug.LogError("Error occurred while loading data: " + e);
                throw e;
            }
            IncrementLoadingBar(ELoadingBarIncrementValues.whoa);

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        }
        else
        {
            GameLogScript.GameLogWrite(StringManager.GetString("no_save_game_data"), heroPCActor);
        }
#endif
    }

    static List<Monster> monstersInCorralAtLoadTime;

    public IEnumerator ContinueGameFromSave(XmlReader reader, FileStream stream)
    {
        if (monstersInCorralAtLoadTime == null) monstersInCorralAtLoadTime = new List<Monster>();
        monstersInCorralAtLoadTime.Clear();

        bool loadDebug = false;

#if UNITY_EDITOR
        loadDebug = false;
#endif

        if (loadDebug) Debug.Log("Load Step 1. Node type, name? " + reader.Name + " : " + reader.NodeType);

        //Debug.Log("Begin coroutine for reading chunks of game data.");
        bool doneActors = false;

        GameMasterScript.IncrementLoadingBar(ELoadingBarIncrementValues.small);
        float timeAtLastYield = Time.realtimeSinceStartup;

        //GameMasterScript.IncrementLoadingBar(0.05f);

        // this will track how many actors did not correctly add to a given map
        // if this number is really high then we might need to remake that map
        int[] actorAddFailuresPerMap = new int[999];

        if (SharedBank.ShouldUseSharedBankForCurrentGame())
        {
            MetaProgressScript.AddAllTamedSharedMonstersToDictionary();
        }

#if UNITY_EDITOR
        if (loadDebug) Debug.Log("Load Step2. Done actors? " + doneActors + " name/nodetype? " + reader.Name + " : " + reader.NodeType);
#endif

        while (reader.NodeType != XmlNodeType.EndElement && !doneActors)
        {
            string strValue = reader.Name.ToLowerInvariant();
            int floor = 0;
            if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
            {
                yield return null;
                timeAtLastYield = Time.realtimeSinceStartup;
            }
            try
            {
#if UNITY_EDITOR
                if (loadDebug) Debug.Log(strValue + " " + reader.NodeType);
#endif
                //Debug.Log(strValue + " " + reader.NodeType);
                switch (strValue)
                {
                    case "stairs":
                        Stairs stair = new Stairs();
                        stair.ReadFromSave(reader);


                        if (stair.autoMove && !stair.isPortal && stair.prefab != "MightyVine" && stair.prefab != "RedPortal") // todo: generalize this
                        {
                            stair.prefab = "TransparentStairs";
                        }
                        else if (stair.isPortal)
                        {
                            stair.displayName = StringManager.GetString("portal_tangledeep"); // "Portal to Tangledeep";
                            stair.prefab = "Portal";
                        }
                        else
                        {
                            if (stair.GetActorMap() != null)
                            {
                                if (!string.IsNullOrEmpty(stair.prefab))
                                {
                                    if (stair.stairsUp)
                                    {
                                        stair.displayName = "Stairway Down";
                                    }
                                    else
                                    {
                                        stair.displayName = "Stairway Up";
                                    }
                                }
                                else
                                {
                                    if (stair.stairsUp)
                                    {
                                        stair.displayName = StringManager.GetString("stairs_down");
                                        stair.prefab = MapMasterScript.visualTileSetNames[(int)stair.GetActorMap().dungeonLevelData.tileVisualSet] + "StairsDown";
                                    }
                                    else
                                    {
                                        stair.displayName = StringManager.GetString("stairs_up");
                                        stair.prefab = MapMasterScript.visualTileSetNames[(int)stair.GetActorMap().dungeonLevelData.tileVisualSet] + "StairsUp";
                                    }
                                }
                            }
                            else
                            {
                                if (stair.stairsUp)
                                {
                                    stair.displayName = StringManager.GetString("stairs_down");
                                    stair.prefab = "EarthStairsDown";
                                }
                                else
                                {
                                    stair.displayName = StringManager.GetString("stairs_up");
                                    stair.prefab = "EarthStairsUp";
                                }
                            }
                        }

                        floor = stair.dungeonFloor;

                        Map outMap;
                        if (MapMasterScript.dictAllMaps.TryGetValue(stair.newLocationID, out outMap))
                        {
                            stair.NewLocation = outMap;
                        }
                        else
                        {
                            loadGame_stairsToTryRelinking.Add(stair);
                        }

                        // TODO - Save/Load stair location for town stuff.

                        if (stair.GetActorMap() != null)
                        {
                            if (!stair.VerifyLoadPositionIsValidThenAddToMap())
                            {
                                // failed to add actor, notate this
                                actorAddFailuresPerMap[stair.GetActorMap().floor]++;
                            }
                        }

                        break;
                    case "npc":
                        NPC loadNPC = new NPC();
                        loadNPC.ReadFromSave(reader);
                        if (loadNPC.ReadActorData("loadfail") == 1)
                        {
                            continue;
                        }
                        floor = loadNPC.dungeonFloor;

                        // TREES are loaded from Meta Progress script and should not be added normally.
                        if (floor == 150)
                        {
                            for (int i = 0; i < MetaProgressScript.treesPlanted.Length; i++)
                            {
                                if (MetaProgressScript.treesPlanted[i] != null)
                                {
                                    if (MetaProgressScript.treesPlanted[i].actorUniqueID == loadNPC.actorUniqueID)
                                    {
                                        loadNPC = MetaProgressScript.treesPlanted[i];
                                        loadNPC.SetActorMap(MapMasterScript.singletonMMS.townMap2);
                                        loadNPC.actorMapID = loadNPC.GetActorMap().mapAreaID;
                                        //Debug.Log("Copied tree in load, ID " + MetaProgressScript.treesPlanted[i].actorUniqueID);
                                        break;
                                    }
                                }
                            }
                        }
                        if (loadNPC.GetActorMap() == null)
                        {
                            Debug.Log(loadNPC.actorRefName + " " + loadNPC.dungeonFloor + " could not connect to map.");
                        }
                        else
                        {
                            if (!loadNPC.VerifyLoadPositionIsValidThenAddToMap())
                            {
                                // failed to add actor, notate this
                                actorAddFailuresPerMap[loadNPC.GetActorMap().floor]++;
                            }
                        }
                        break;
                    case "mn":
                    case "monster":
                        Monster mon = new Monster();
                        bool success = mon.ReadFromSave(reader, true, true, assignAndIncrementSharedCorralID: false);

                        if (mon.isInCorral && mon.ReadActorData("loadtime_repair") != 1)
                        {
                            if (Debug.isDebugBuild) Debug.Log("Disregard any local corral creatures. Only load from shared bank or meta progress.");
                            if (mon.GetActorMap() != null)
                            {
                                if (Debug.isDebugBuild) Debug.Log("Removing dead/unsuitable " + mon.actorRefName + " " + mon.actorUniqueID + " " + mon.GetPos() + " " + mon.dungeonFloor);
                                mon.GetActorMap().RemoveActorFromMap(mon);
                                mon.GetActorMap().RemoveActorFromLocation(mon.GetPos(), mon);
                                
                            }
                            continue;
                        }
                        else if (mon.isInCorral)
                        {
                            mon.RemoveActorData("loadtime_repair");
                        }

                        if (!mon.actorEnabled)
                        {
                            mon.DisableActor();
                        }

                        bool debug = false;

                        if (mon.actorUniqueID == 16170) debug = true;

                        if (success)
                        {
                            Fighter ft = mon as Fighter;
                            if (!ft.myStats.IsAlive())
                            {
                                if (debug) Debug.Log("Monster is dead " + ft.myStats.GetCurStat(StatTypes.HEALTH) + " " + ft.actorRefName + " " + ft.myStats.GetMaxStat(StatTypes.HEALTH));
                                // Don't load dead monster.
                                if (ft.destroyed)
                                {
                                    if (ft.summonedActorIDs != null && ft.summonedActorIDs.Count > 0 && masterMonsterList.ContainsKey(ft.actorRefName))
                                    {
                                        //Debug.Log("<color=yellow>Loading the destroyed monster " + ft.actorUniqueID + " " + ft.summonedActorIDs.Count + " </color>");
                                        deadActorsToSaveAndLoad.Add(ft);
                                    }
                                }

                                if (ft.GetActorMap() != null)
                                {
                                    if (debug) Debug.Log("Removing dead " + ft.actorRefName + " " + ft.actorUniqueID + " " + ft.GetPos());
                                    ft.GetActorMap().RemoveActorFromMap(ft);
                                    ft.GetActorMap().RemoveActorFromLocation(ft.GetPos(), ft);
                                }
                            }
                            else
                            {
                                floor = mon.dungeonFloor;
                                if (mon.GetActorMap() != null)
                                {
                                    if (!mon.VerifyLoadPositionIsValidThenAddToMap())
                                    {
                                        // failed to add actor, notate this
                                        actorAddFailuresPerMap[mon.GetActorMap().floor]++;
                                        if (debug) Debug.Log("Failed to add actor");
                                    }
                                    else
                                    {
                                        if (mon.isInCorral)
                                        {
                                            monstersInCorralAtLoadTime.Add(mon);
                                        }
                                    }
                                }
                                else
                                {
                                    if (debug) Debug.Log("But monster map doesn't exist");
                                }
                            }
                        }



                        break;
                    case "dt":
                    case "destructible":
                        //Destructible dt = new Destructible();
                        Destructible dt = DTPooling.GetDestructible();
                        dt.ReadFromSave(reader);
                        floor = dt.dungeonFloor;
                        if (dt.GetActorMap() != null)
                        {
                            if (!dt.VerifyLoadPositionIsValidThenAddToMap())
                            {
                                // failed to add actor, notate this
                                actorAddFailuresPerMap[dt.GetActorMap().floor]++;
                            }
                        }
                        break;
                    case "item":
                        Item itm = new Item();
                        itm = itm.ReadFromSave(reader);
                        if (itm != null)
                        {
                            floor = itm.dungeonFloor;
                            if (itm.GetActorMap() != null)
                            {
                                if (!itm.VerifyLoadPositionIsValidThenAddToMap())
                                {
                                    // failed to add actor, notate this
                                    actorAddFailuresPerMap[itm.GetActorMap().floor]++;
                                }
                            }
                        }
                        break;
                    case "ui": // Done loading actors.
                               //Debug.Log("Done loading actors via ui.");
                        doneActors = true;
                        break;
                    case "turn":
                        turnNumber = reader.ReadElementContentAsInt();
                        //if (Debug.isDebugBuild) Debug.Log("Turn number read as: " + turnNumber);
                        break;
                    case "wanderingmerchant":
                        reader.ReadStartElement();
                        bool isWanderingMerchant = reader.ReadElementContentAsBoolean();
                        if (isWanderingMerchant)
                        {
                            int mID = reader.ReadElementContentAsInt();
                            int turns = reader.ReadElementContentAsInt();
                            durationOfWanderingMerchant = turns;
                            Actor merch;
                            if (dictAllActors.TryGetValue(mID, out merch))
                            {
                                wanderingMerchantInTown = merch as NPC;
                            }
                            else
                            {
                                if (Debug.isDebugBuild) Debug.Log("Couldn't find wandering merchant with ID " + mID);
                            }
                        }
                        reader.ReadEndElement();
                        break;
                    //See Map.ReadSpecialMapDataFromSave to understand how the data is written
                    //and why it is loaded in this fashion.
                    case "specialmapdata":
                        reader.ReadStartElement();
                        int mapFloorNum = reader.ReadElementContentAsInt();
                        Map targetMap = MapMasterScript.theDungeon.FindFloor(mapFloorNum);
                        if (targetMap == null)
                        {
                            if (Debug.isDebugBuild)
                            {
                                Debug.Log("But target map " + mapFloorNum + " does not exist.");
                                Debug.Log(reader.NodeType + " " + reader.Name);
                            }
                            while (!(reader.Name == "specialmapdata" && reader.NodeType == XmlNodeType.EndElement))
                            {
                                reader.Read();
                                if (Debug.isDebugBuild) Debug.Log("Cycle through " + reader.NodeType + " " + reader.Name);
                            }
                        }
                        else
                        {
                            targetMap.ReadSpecialMapDataFromSave(reader);
                        }
                        //if (Debug.isDebugBuild) Debug.Log("End read special map data at " + reader.NodeType + " " + reader.Name);
                        reader.ReadEndElement();
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log("ERROR EXCEPTION " + e);
                Debug.Log(strValue + " " + reader.Name + " " + reader.NodeType);
                SetLoadStatusDueToErrorInGameLoad();

            }
            if (gameLoadingState == GameLoadingState.BUSTED ||
                gameLoadingState == GameLoadingState.HOPELESS)
            {
                yield break;
            }
        }
        DLCManager.CheckForMapLoadFailures(actorAddFailuresPerMap);

#if UNITY_EDITOR
        if (loadDebug) Debug.Log("Load Step3");
#endif

        uims.UIStart(false);
        yield return null;
        GameMasterScript.IncrementLoadingBar(ELoadingBarIncrementValues.small);
        yield return null;
        tutorialManager.TutorialStart();
        //Debug.Log("Prior to start uims " + reader.Name.ToLowerInvariant() + " " + reader.NodeType);

        uims.ReadFromSave(reader);

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.Name == "mmusic")
            {
                MusicManagerScript.ReadMusicDataFromSave(reader);
                break;
            }
        }

#if UNITY_EDITOR
        if (loadDebug) Debug.Log("Load Step4");
#endif

        if (!PlatformVariables.USE_INTROLOOP)
        {
            if (heroPCActor != null && heroPCActor.GetActorMap() != null)
            {
                musicManager.PushSpecificMusicTrackOnStack(heroPCActor.GetActorMap().musicCurrentlyPlaying);
            }
        }

#if UNITY_SWITCH
        //#switch_memory
        //Debug.Log("Memory use during load: " + GC.GetTotalMemory(false) + " ContinueFromSave before FinishGameLoadAfterReadingSaveData" );

        GameMasterScript.IncrementLoadingBar(ELoadingBarIncrementValues.small);
        yield return null;
#endif

#if UNITY_EDITOR
        if (loadDebug) Debug.Log("Load Step5");
#endif

        yield return FinishGameLoadAfterReadingSaveData(reader, stream);

#if UNITY_EDITOR
        if (loadDebug) Debug.Log("Load Step6");
#endif
    }

    public static void ParseHeroBufferedAsyncLoadOutput(string text)
    {
        string[] split1 = text.Split('|');

        bool rjMode = false;

        //Debug.Log("Parse " + text);

        for (int i = 0; i < split1.Length; i++)
        {
            string[] split2 = split1[i].Split('!');

            string key = split2[0];

            if (split2.Length != 2)
            {
                if (Debug.isDebugBuild) Debug.LogError("Uh oh, split2 failed? Key " + key + " Length is not 2?");
                continue;
            }

            switch(key)
            {
                case "gmode":
                    saveDataBlockAsyncLoadOutput.strGameModeInfo = GetGameModeString(split2[1]);

                    GameModes gMode = (GameModes)Enum.Parse(typeof(GameModes), split2[1]);

                    GameStartData.gameModesSelectedBySlot[GameStartData.saveGameSlot] = gMode;
                    break;
                case "ngp":
                    if (!int.TryParse(split2[1], out saveDataBlockAsyncLoadOutput.iNewGamePlusRank))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse NGP rank");
                    }                    
                    break;
                case "name":
                    saveDataBlockAsyncLoadOutput.strHeroName = split2[1];
                    break;
                case "rjmode":                    
                    if (!bool.TryParse(split2[1], out rjMode))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse RJ Mode");
                    }
                    if (rjMode) saveDataBlockAsyncLoadOutput.strJobName = StringManager.GetString("job_wanderer");
                    break;
                case "job":
                    if (!rjMode) saveDataBlockAsyncLoadOutput.strJobName = split2[1];
                    break;
                case "lfloor":
                    break;
                case "lvl":
                    if (!int.TryParse(split2[1], out saveDataBlockAsyncLoadOutput.iHeroLevel))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse hero level.");
                    }
                    break;
                case "beat":
                    if (!bool.TryParse(split2[1], out saveDataBlockAsyncLoadOutput.bGameClearSave))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse game clear save");
                    }
                    break;
                case "dp":
                    
                    break;
                case "pt":
                    saveDataBlockAsyncLoadOutput.strTimePlayed = split2[1];
                    break;
                case "chal":
                    ChallengeTypes cType = (ChallengeTypes)Enum.Parse(typeof(ChallengeTypes), split2[1]);
                    saveDataBlockAsyncLoadOutput.challengeMode = cType;
                    break;
                case "map":
                    saveDataBlockAsyncLoadOutput.strLocation = split2[1];
                    break;
                case "port":
                    saveDataBlockAsyncLoadOutput.portrait = UIManagerScript.GetPortraitForDialog(split2[1])[0];
                    break;
            }
        }
        
    }

    public static string GetGameModeString(GameModes gm)
    {
        string mode = "";
        switch (gm)
        {
            case GameModes.ADVENTURE:
                return UIManagerScript.greenHexColor + StringManager.GetString("misc_adventure_mode") + "</color>";
            case GameModes.HARDCORE:
                return UIManagerScript.redHexColor + StringManager.GetString("misc_hardcore_mode") + "</color>";
            case GameModes.NORMAL:
                return UIManagerScript.orangeHexColor + StringManager.GetString("misc_heroic_mode") + "</color>";
        }

        return mode;
    }

    public static string GetGameModeString(string gmString)
    {
        GameModes gm = GameModes.NORMAL;

        gm = (GameModes)Enum.Parse(typeof(GameModes), gmString);
        
        return GetGameModeString(gm);
    }

    static string TryGetMapNameFromCoreData(string coreData)
    {
        string[] split = coreData.Split('|');
        if (split.Length < 2) return "";

        int floor = 0;

        int.TryParse(split[1], out floor);

        return "";
    }

}
