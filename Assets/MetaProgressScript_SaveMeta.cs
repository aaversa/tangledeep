using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System.Text;

public partial class MetaProgressScript
{

    public static void SaveMinimalMetaProgress()
    {
        SaveMetaProgress(true);
    }

    public static IEnumerator SaveMetaProgress(bool minimal)
    {
        //if (Debug.isDebugBuild) Debug.Log("Actually writing meta progress to disk. Minimal? " + minimal);

        float timeSaveStart = Time.realtimeSinceStartup; 

#if UNITY_SWITCH
        string kStrFileName = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        var sdh = Switch_SaveDataHandler.GetInstance();
        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter metaWriter = XmlWriter.Create(strData, xmlSettings);
#elif UNITY_PS4
        string kByteFileName = "metaprogress" + GameStartData.saveGameSlot + ".xml";
        
        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter metaWriter = XmlWriter.Create(strData, xmlSettings);
#elif UNITY_XBOXONE
        string kByteFileName = "metaprogress" + GameStartData.saveGameSlot + ".xml";

        StringBuilder strData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding();
        XmlWriter metaWriter = XmlWriter.Create(strData, xmlSettings);
#else
        string metaPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogressCopy.xml";
        File.Delete(metaPath);
        //Debug.Log("Saving metagame progress to " + metaPath);

        XmlWriter metaWriter = XmlWriter.Create(metaPath);
#endif
        metaWriter.WriteStartDocument();
        metaWriter.WriteStartElement("DOCUMENT");

        string playerModBuilder = PlayerModManager.GetPlayerModStringForSerialization();
        if (!string.IsNullOrEmpty(playerModBuilder))
        {
            metaWriter.WriteElementString("playermodsactive", playerModBuilder);
        }

        string expansionsInstalled = DLCManager.GetExpansionsStringForSerialization();
        if (expansionsInstalled != "")
        {
            metaWriter.WriteElementString("expansions", expansionsInstalled);
        }

        metaWriter.WriteElementString("gameversion", GameMasterScript.GAME_BUILD_VERSION.ToString());
        metaWriter.WriteElementString("numcharacters", totalCharacters.ToString());
        metaWriter.WriteElementString("lowestfloor", lowestFloorReached.ToString());
        //playTimeInSeconds += Time.fixedTime - GameMasterScript.timeSinceStartOrSave;
        metaWriter.WriteElementString("playtime", GetPlayTime().ToString());
        metaWriter.WriteElementString("totaldayspassed", totalDaysPassed.ToString());

        string constructMainMenuText = ConstructMetaProgressSaveStringFromData(RandomJobMode.IsCurrentGameInRandomJobMode(), totalCharacters, lowestFloorReached, GetPlayTime(), totalDaysPassed);
        
        WriteRandomJobModeStuff(metaWriter);

        metaWriter.WriteElementString("buffereddata", constructMainMenuText);

        metaWriter.WriteElementString("wft", watchedFirstTutorial.ToString().ToLowerInvariant());
        for (int i = 0; i < recipesKnown.Count; i++)
        {
            metaWriter.WriteElementString("recipeknown", recipesKnown[i].ToString());
        }
        metaWriter.WriteStartElement("jobsplayed");

        for (int i = 0; i < (int)CharacterJobs.COUNT - 2; i++)
        {
            metaWriter.WriteElementString(((CharacterJobs)i).ToString(), jobsStarted[i].ToString());
        }

        metaWriter.WriteEndElement();

        string monsterBuilder = "";
        foreach (string mKey in monstersDefeated.Keys)
        {
            monsterBuilder += mKey + "," + monstersDefeated[mKey] + "|";
        }
        metaWriter.WriteElementString("mdefeated", monsterBuilder);

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        if (minimal)
        {
            foreach (int i in journalEntriesRead)
            {
                metaWriter.WriteElementString("journalentry", i.ToString());
            }
            TutorialManagerScript.WriteToSave(metaWriter);
            DefeatData.WriteAllDataToSave(metaWriter);
            metaWriter.WriteEndElement();
            metaWriter.WriteEndDocument();
            metaWriter.Close();
            yield break;
        }

        TutorialManagerScript.WriteToSave(metaWriter);

        FoodCartScript.WriteToSave(metaWriter);
        
        string sBuilder = "";
        for (int i = 0; i < relicRefsThatShouldNotBeDeleted.Count; i++)
        {
            if (i > 0)
            {
                sBuilder += "|";
            }
            sBuilder += relicRefsThatShouldNotBeDeleted[i];
        }
        if (relicRefsThatShouldNotBeDeleted.Count > 0)
        {
            metaWriter.WriteElementString("saverelics", sBuilder);
        }

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        //LegendaryMaker.WriteAllCustomItemsToSave(metaWriter);
        MonsterMaker.WriteAllCustomMonstersToSave(metaWriter);
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) DungeonMaker.WriteAllCustomLevelsToSave(metaWriter);
        
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i] != null)
            {
                // Writes NPC to file, with magic tree component
                metaWriter.WriteStartElement("grovetree");
                treesPlanted[i].WriteToSave(metaWriter);
                metaWriter.WriteEndElement();
            }
        }

        if (GameStartData.GetGameMode() == GameModes.HARDCORE || RandomJobMode.IsCurrentGameInRandomJobMode() ||
            GameStartData.challengeTypeBySlot[GameStartData.saveGameSlot] != ChallengeTypes.NONE)
        {
            foreach(TamedCorralMonster tcm in localTamedMonstersForThisSlot)
            {
                if (Debug.isDebugBuild) Debug.Log("Writing MetaProgress tamed pet " + tcm.refName + " " + tcm.monsterObject.actorUniqueID);
                tcm.monsterObject.WriteToSave(metaWriter);
            }
        }

        if (releasedMonsters.Count > 0)
        {
            foreach (ReleasedMonster rm in releasedMonsters)
            {
                rm.WriteToSave(metaWriter);
            }
        }

        if (monsterPetsAvailable.Count > 0)
        {
            foreach (Monster m in monsterPetsAvailable)
            {
                m.WriteToSave(metaWriter);
            }
        }

        DefeatData.WriteAllDataToSave(metaWriter);

        Map townMap = MapMasterScript.singletonMMS.townMap;
        Map townMap2 = MapMasterScript.singletonMMS.townMap2;

        /* NPC banker = null;
        foreach (Actor act in townMap.actorsInMap)
        {
            if (act.actorRefName == "npc_banker")
            {
                banker = act as NPC;
                break;
            }
        }
        if (banker == null)
        {
            Debug.Log("Could not find the banker in the town map!");
        } */

        foreach (int i in journalEntriesRead)
        {
            metaWriter.WriteElementString("journalentry", i.ToString());
        }

        if (Time.realtimeSinceStartup - timeSaveStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeSaveStart = Time.realtimeSinceStartup;
        }

        //banker.WriteToSave(metaWriter);

        if (dictMetaProgress.Keys.Count > 0)
        {
            metaWriter.WriteStartElement("dictmetaprogress");
            foreach (string str in dictMetaProgress.Keys)
            {
                metaWriter.WriteElementString(str, dictMetaProgress[str].ToString());
            }
            metaWriter.WriteEndElement();
        }
        metaWriter.WriteEndElement();
        metaWriter.WriteEndDocument();
        metaWriter.Close();

#if UNITY_SWITCH
        sdh.SaveSwitchFile(strData.ToString(), kStrFileName);
#endif

#if UNITY_PS4
        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(strData.ToString());        
        PS4SaveManager.instance.SaveData(PS4SaveManager.ROOT_DIR, kByteFileName, myByte);
#endif

#if UNITY_XBOXONE
        XboxSaveManager.instance.SetString(kByteFileName, strData.ToString());
        XboxSaveManager.instance.Save();
#endif

        //if (Debug.isDebugBuild) Debug.Log("Successfully saved meta progress " + metaPath);
    }


}
