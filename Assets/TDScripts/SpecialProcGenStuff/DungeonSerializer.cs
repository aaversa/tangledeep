using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.IO;

public partial class DungeonMaker
{
    public static void WriteAllCustomLevelsToSave(XmlWriter writer)
    {
        foreach(DungeonLevel dl in customDungeonLevelDataInSaveFile.Values)
        {
            dl.WriteCustomDungeonLevelDataToSave(writer);
        }
    }
}

public partial class DungeonLevel
{
    /// <summary>
    /// We must be on a "dldata" node when running this function! Will add to the master list.
    /// </summary>
    /// <param name="reader"></param>
    public void ReadCustomDungeonLevelDataFromSave(XmlReader reader)
    {
        string unparsed;
        string[] parsed;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name)
            {
                case "coreinfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    floor = Int32.Parse(parsed[0]);
                    size = Int32.Parse(parsed[1]);                    
                    layoutType = (DungeonFloorTypes)Enum.Parse(typeof(DungeonFloorTypes), parsed[2]);
                    tileVisualSet = (TileSet)Enum.Parse(typeof(TileSet), parsed[3]);
                    customName = parsed[4];
                    revealAll = Boolean.Parse(parsed[5]);
                    safeArea = Boolean.Parse(parsed[6]);
                    sideArea = Boolean.Parse(parsed[7]);
                    noSpawner = Boolean.Parse(parsed[8]);
                    altPath = Int32.Parse(parsed[9]);
                    break;
                case "spawntable":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    ActorTable customSpawnTable = new ActorTable();
                    customSpawnTable.refName = parsed[0]; // 0 index is always the name
                    GameMasterScript.masterSpawnTableList.Add(customSpawnTable.refName, customSpawnTable);
                    // Actor keys are stored as "monsterName",5
                    for (int i = 1; i < parsed.Length; i++)
                    {
                        string[] kvp = parsed[i].Split(',');
                        customSpawnTable.AddToTable(kvp[0], Int32.Parse(kvp[1]));
                    }
                    break;
                case "extendedinfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    minMonsters = Int32.Parse(parsed[0]);
                    maxMonsters = Int32.Parse(parsed[1]);
                    maxChampions = Int32.Parse(parsed[2]);
                    maxChampionMods = Int32.Parse(parsed[3]);
                    spawnRateModifier = CustomAlgorithms.TryParseFloat(parsed[4]);
                    spawnTable = GameMasterScript.masterSpawnTableList[parsed[5]];
                    fastTravelPossible = Boolean.Parse(parsed[6]);
                    scaleMonstersToMinimumLevel = Int32.Parse(parsed[7]);
                    bossArea = Boolean.Parse(parsed[8]);
                    challengeValue = CustomAlgorithms.TryParseFloat(parsed[9]);
                    break;
                case "extrainfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    unbreakableWalls = Boolean.Parse(parsed[0]);
                    effectiveFloor = Int32.Parse(parsed[1]);
                    if (!string.IsNullOrEmpty(parsed[2]))
                    {
                        specialRoomTemplate = GameMasterScript.masterDungeonRoomlist[parsed[2]];
                    }                    
                    stairsDownToLevel = Int32.Parse(parsed[3]);
                    stairsUpToLevel = Int32.Parse(parsed[4]);
                    poisonAir = Boolean.Parse(parsed[5]);
                    showRewardSymbol = Boolean.Parse(parsed[6]);
                    noRewardPopup = Boolean.Parse(parsed[7]);
                    break;
                case "parallax":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    parallaxSpriteRef = parsed[0];
                    parallaxTileCount = Int32.Parse(parsed[1]);
                    parallaxShiftPerTile = Vector2.zero;
                    parallaxShiftPerTile.x = CustomAlgorithms.TryParseFloat(parsed[2]);
                    parallaxShiftPerTile.y = CustomAlgorithms.TryParseFloat(parsed[3]);
                    break;
                case "reward":
                    clearRewards.Add(reader.ReadElementContentAsString());
                    break;
                case "music":
                    musicCue = reader.ReadElementContentAsString();
                    break;
                case "scriptonenter":
                    script_onEnterMap = reader.ReadElementContentAsString();
                    break;
                case "scriptonmonsterspawn":
                    script_onMonsterSpawn = reader.ReadElementContentAsString();
                    MonsterSpawnFunctions.CacheScript(script_onMonsterSpawn);
                    break;
                case "scriptonmonsterdeath":
                    script_onMonsterDeath = reader.ReadElementContentAsString();
                    break;
                case "scriptonturnend":
                    script_onTurnEnd = reader.ReadElementContentAsString();
                    TDGenericFunctions.CacheScript(script_onTurnEnd);
                    break;
                case "dictmeta":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.None)
                        {
                            reader.Read();
                            continue;
                        }
                        string dictKey = reader.Name;
                        int value = reader.ReadElementContentAsInt();
                        if (!dictMetaData.ContainsKey(dictKey))
                        {
                            dictMetaData.Add(dictKey, value);
                        }
                        
                    }
                    reader.ReadEndElement();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }


        reader.ReadEndElement(); // finish reading the level node

        if (!GameMasterScript.masterDungeonLevelList.ContainsKey(floor))
        {
        GameMasterScript.masterDungeonLevelList.Add(floor, this);
        }
#if UNITY_EDITOR
        //Debug.Log("Success! Read serialized dungeon level " + floor);
#endif
    }

    public void WriteCustomDungeonLevelDataToSave(XmlWriter writer)
    {
        writer.WriteStartElement("dldata");

        string coreInfo = "";
        // floor(int), size(int), layoutType(enum), tileVisual(enum), customName, revealAll(bool), safeArea(bool), 
        // sideArea(bool), noSpawner(bool), altPath (int)
        coreInfo = floor + "|" + size + "|" + layoutType + "|" + tileVisualSet + "|" + customName + "|" + revealAll + "|" + safeArea + "|" + sideArea + "|" + noSpawner + "|" + altPath;
        writer.WriteElementString("coreinfo", coreInfo);

        // Write spawn table data
        string spawnTableData = spawnTable.refName;
        foreach (string actorRef in spawnTable.table.Keys)
        {
            spawnTableData += "|";
            spawnTableData += actorRef + "," + spawnTable.table[actorRef];
        }
        writer.WriteElementString("spawntable", spawnTableData);

        string extendedInfo = "";
        // minMonsters(int), max(int), maxChamps(int), maxChampMods(int), spawnRate(float), spawnTable, fastTravel(bool), 
        // scalemon (int), bossArea (bool), challengevalue (float)
        extendedInfo = minMonsters + "|" + maxMonsters + "|" + maxChampions + "|" + maxChampionMods + "|" + spawnRateModifier + "|" + spawnTable.refName + "|" + fastTravelPossible + "|" + scaleMonstersToMinimumLevel + "|" + bossArea + "|" + challengeValue;
        writer.WriteElementString("extendedinfo", extendedInfo);

        string extraInfo = "";
        // unbreakwalls(bool), effectiveFloor(int), specialRoomTemplate, stairsDown, stairsUp, poisonAir(bool), showRewardSymbol(bool), noRewardPopup(bool)
        extraInfo = unbreakableWalls + "|" + effectiveFloor + "|" + specialRoomTemplate + "|" + stairsDownToLevel + "|" + stairsUpToLevel + "|" + poisonAir + "|" + showRewardSymbol + "|" + noRewardPopup;
        writer.WriteElementString("extrainfo", extraInfo);

        if (!string.IsNullOrEmpty(parallaxSpriteRef))
        {
            writer.WriteElementString("parallax", parallaxSpriteRef + "|" + parallaxTileCount + "|" + parallaxShiftPerTile.x + "|" + parallaxShiftPerTile.y);
        }

        if (!string.IsNullOrEmpty(script_onEnterMap))
        {
            writer.WriteElementString("scriptonenter", script_onEnterMap);
        }
        if (!string.IsNullOrEmpty(script_onMonsterDeath))
        {
            writer.WriteElementString("scriptonmonsterdeath", script_onMonsterDeath);
        }
        if (!string.IsNullOrEmpty(script_onMonsterSpawn))
        {
            writer.WriteElementString("scriptonmonsterspawn", script_onMonsterSpawn);
        }
        if (!string.IsNullOrEmpty(script_onTurnEnd))
        {
            writer.WriteElementString("scriptonturnend", script_onTurnEnd);
        }
       
        foreach(string reward in clearRewards)
        {
            writer.WriteElementString("reward", reward);
        }

        if (!string.IsNullOrEmpty(musicCue))
        {
            writer.WriteElementString("music", musicCue);
        }

        bool anyDictMetaData = false;
        foreach(string key in dictMetaData.Keys)
        {
            if (!anyDictMetaData)
            {
                anyDictMetaData = true;
                writer.WriteStartElement("dictmeta");
            }
            writer.WriteElementString(key, dictMetaData[key].ToString());
        }

        if (anyDictMetaData) writer.WriteEndElement();

        writer.WriteEndElement();
    }
}
