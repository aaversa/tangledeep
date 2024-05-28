using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml;

public partial class MetaProgressScript
{
    /// <summary>
    /// Write to this on save, then use this on load so we don't have to parse the whole XML to figure out what to display @ slot select time.
    /// </summary>
    public string bufferedData;

    /// <summary>
    /// Once we retrieve core meta data for a slot, it can live here forever until we return to the title screen or manage data in some regard;
    /// </summary>
    public static string[] bufferedMetaDataInAllSlots;

    public static string[] bufferedHeroDataInAllSlots;

    public static bool[] bufferedMetaDataDirty;

    public static bool[] bufferedHeroDataDirty;

    /// <summary>
    /// Copy this into SharedData.
    /// </summary>
    static List<string> metaCreationFeatsUnlocked;

    /// <summary>
    /// Copy this into SharedData.
    /// </summary>
    static bool[] metaJobsUnlocked;

    public static IEnumerator LoadCoreData(string path, int saveSlot)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;
        string builder = "";
        int numCharacters = 0;
        int lowestFloorEver = 0;
        int tDays = 0;
        float localPlayTime = 0;
        string txt;
        bool randomJobMode = false;

        // here is a bunch of code that removes the stall from loading up XML coredata
        string strLoadedData = "";
#if UNITY_SWITCH
        var saveDataHandler = Switch_SaveDataHandler.GetInstance();

        yield return saveDataHandler.LoadSwitchSavedDataFileAsync(path, false);

        byte[] loadedBytes = null;
        Switch_SaveDataHandler.GetBytesLoadedAsync(path, ref loadedBytes);
        var memStream = new MemoryStream(loadedBytes);
        var binaryReader = new BinaryReader(memStream);
        var sReader = new StringReader(binaryReader.ReadString());

        //Switch_SaveDataHandler.GetInstance().load(ref strLoadedData, path);
        XmlReader metaReader = XmlReader.Create(sReader, settings);
#elif UNITY_PS4
        byte[] loadedBytes = null;        
        if(!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, path))
        {
            Debug.LogError("LoadCoreData doesn't exist");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }        
        PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, path, out loadedBytes);
        strLoadedData = System.Text.Encoding.UTF8.GetString(loadedBytes);       

        
        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), settings);
#elif UNITY_XBOXONE        
        if (!XboxSaveManager.instance.HasKey(path))
        {
            Debug.LogError("LoadCoreData doesn't exist");
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }
        strLoadedData = XboxSaveManager.instance.GetString(path);

        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), settings);
#endif

#if !UNITY_PS4 && !UNITY_XBOXONE 
#if !UNITY_SWITCH
        if (!File.Exists(path))
#else
        if (!Switch_SaveDataHandler.GetInstance().CheckIfSwitchFileExists(path))
#endif
        {
            GameMasterScript.strAsyncLoadOutput = "";
            yield break;
        }
#endif

        bool finishReadingImmediately = false;
        bool readBufferedData = false;

#if !UNITY_PS4 && !UNITY_XBOXONE
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
#endif
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            using (XmlReader metaReader = XmlReader.Create(stream, settings))
#else
            using (metaReader)
#endif
            {
                try
                {
                    metaReader.Read();


                    while (metaReader.Name != "jobsplayed")
                    {
                        string readValue = metaReader.Name.ToLowerInvariant();
                        switch (readValue)
                        {
                            case "buffereddata":
                                string text = metaReader.ReadElementContentAsString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    GameMasterScript.strAsyncLoadOutput = text;
                                    ParseMetaBufferedAsyncLoadOutput(text);
                                    finishReadingImmediately = true;
                                    readBufferedData = true;
                                    break;
                                }
                                break;
                            case "numcharacters":
                                numCharacters = metaReader.ReadElementContentAsInt();
                                CharCreation.totalCharacters = numCharacters;
                                break;
                            case "expansions":
                                DLCManager.ParseSavedPlayerExpansionsIntoList(GameStartData.dlcEnabledPerSlot[saveSlot], metaReader.ReadElementContentAsString());
                                break;
                            case "lowestfloor":
                                lowestFloorEver = metaReader.ReadElementContentAsInt();
                                break;
                            case "playtime":
                                txt = metaReader.ReadElementContentAsString();
                                localPlayTime = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "randomjob":
                            case "rjabils":
                            case "rjinnates":
                            case "rjxfloors":
                                txt = metaReader.ReadElementContentAsString();
                                randomJobMode = true;
                                GameStartData.slotInRandomJobMode[saveSlot] = true;
                                //ReadRandomJobModeStuff(metaReader, readValue);
                                break;
                            case "totaldayspassed":
                                tDays = metaReader.ReadElementContentAsInt();
                                break;
                            case "ju":
                                ReadJobsUnlocked(metaReader, saveSlot);
                                //ReadJobsUnlocked(metaReader, saveSlot);
                                break;
                            case "cfu":
                                ReadCreationFeats(metaReader);
                                //ReadCreationFeats(metaReader);
                                break;
                            default:
                                metaReader.Read();
                                break;
                        }
                        if (finishReadingImmediately) break;
                    }

#if !UNITY_SWITCH// && !UNITY_PS4 && !UNITY_XBOXONE
                    metaReader.Close();
#endif

                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    metaReader.Close();
#if !UNITY_PS4 && !UNITY_XBOXONE
                stream.Close();
#endif
                    // Something is screwy  with this meta data. Make a copy and then delete it.
                    File.Copy(path, path + "_corruptCopy.xml");
                    File.Delete(path);
                    Debug.Log("Meta data corrupted and will be reset.");
                    yield break;
                    //throw;
                }
            }
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
            stream.Close();
#endif
#if !UNITY_PS4 && !UNITY_XBOXONE
    }
#endif

        if (!readBufferedData)
        {
            builder = ConstructMetaProgressSaveStringFromData(randomJobMode, numCharacters, lowestFloorEver, localPlayTime, tDays);

            //store the playtime here, everything else was in the core data for the hero
            GameMasterScript.saveDataBlockAsyncLoadOutput.strTimePlayed = GetDisplayPlayTime(true, localPlayTime);
            GameMasterScript.saveDataBlockAsyncLoadOutput.daysPassed = tDays;
            GameMasterScript.saveDataBlockAsyncLoadOutput.lowestFloor = lowestFloorEver;
            GameMasterScript.saveDataBlockAsyncLoadOutput.numCharacters = numCharacters;
            //GameMasterScript.saveDataBlockAsyncLoadOutput.strCampaignData = StringManager.GetString("misc_days_passed") + ": " + tDays + ", " + StringManager.GetString("total_characters") + ": " + numCharacters;
            GameMasterScript.strAsyncLoadOutput = builder;
        }
    }

    static void ReadThroughEndOfNode(XmlReader metaReader, string nodeName)
    {
        bool endCase = metaReader.NodeType == XmlNodeType.EndElement && metaReader.Name == nodeName;
        int attempts = 0;
        while (!endCase)
        {
            metaReader.Read();
            attempts++;
            if (attempts > 5000)
            {
                Debug.LogError("Uh oh. Trying to find the end of node " + nodeName);
                break;
            }
            endCase = metaReader.NodeType == XmlNodeType.EndElement && metaReader.Name == nodeName;
        }
    }

    public static string ConstructMetaProgressSaveStringFromData(bool randomJobMode, int numCharacters, int lowestFloorEver, float localPlayTime, int tDays)
    {
        string builder = "";

        builder += "chars!" + numCharacters;
        builder += "|lfloor!" + lowestFloorEver;
        builder += "|pt!" + GetDisplayPlayTime(true, localPlayTime);
        builder += "|dp!" + totalDaysPassed;
        builder += "|rjmode!" + randomJobMode;

        return builder;

        /* if (randomJobMode)
        {
            builder = StringManager.GetString("randomjob_mode") + " ";
        }

        builder = builder + StringManager.GetString("total_characters") + ": <color=yellow>" + numCharacters + "</color>, " + StringManager.GetString("highest_floor_ever") + ": <color=yellow>"
            + lowestFloorEver + "</color>, " + StringManager.GetString("total_playtime") + ": <color=yellow>" + GetDisplayPlayTime(true, localPlayTime) + "</color>";

        return builder; */
    }

    public static void ParseMetaBufferedAsyncLoadOutput(string text)
    {
        
        string[] split1 = text.Split('|');

        //Debug.Log("parse meta: " + text);

        bool rjMode = false;

        for (int i = 0; i < split1.Length; i++)
        {
            string[] split2 = split1[i].Split('!');

            string key = split2[0];

            if (split2.Length != 2)
            {
                if (Debug.isDebugBuild) Debug.LogError("Uh oh, split2 failed? Key " + key + " Length is not 2?");
                continue;
            }

            switch (key)
            {
                case "chars":
                    if (!int.TryParse(split2[1], out GameMasterScript.saveDataBlockAsyncLoadOutput.numCharacters))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse meta num characters");
                    }
                    break;
                case "rjmode":
                    if (!bool.TryParse(split2[1], out rjMode))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse RJ Mode");
                    }
                    if (rjMode)
                    {
                        GameMasterScript.saveDataBlockAsyncLoadOutput.strJobName = StringManager.GetString("job_wanderer");
                    }
                    break;
                case "lfloor":
                    if (!int.TryParse(split2[1], out GameMasterScript.saveDataBlockAsyncLoadOutput.lowestFloor))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse lowest floor");
                    }
                    break;
                case "dp":
                    if (!int.TryParse(split2[1], out GameMasterScript.saveDataBlockAsyncLoadOutput.daysPassed))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Couldn't parse days passed.");
                    }
                    break;
                case "pt":
                    GameMasterScript.saveDataBlockAsyncLoadOutput.strTimePlayed = split2[1];
                    break;
            }
        }        
    }
}