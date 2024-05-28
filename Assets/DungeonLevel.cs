using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.IO;

public partial class DungeonLevel
{
    public int floor;
    public int size; // num columns and rows
    public int maxSecretAreas;
    public float challengeValue;
    public bool bossArea;
    public bool noSpawner;
    public bool hasFungus;
    public bool noCustomRoomTemplates;
    public float spawnRateModifier;
    public ActorTable spawnTable;
    public DungeonFloorTypes layoutType;
    public List<RoomTemplate> priorityTemplates;
    public List<SeasonalImageDataPack> seasonalImages;
    public RoomTemplate specialRoomTemplate;
    public int priorityMinimum;
    public int extraRivers;
    public int lavaRivers;
    public float chanceToOpenWalls;
    public float caveFillConvertChance;
    public string imageOverlay;
    public string customName;
    public bool revealAll;
    public List<string> extraOverlays;
    public int maxLavaPools;
    public int minSpawnFloor;
    public int maxSpawnFloor;
    public string musicCue;
    public int minMonsters;
    public int maxMonsters;
    public TileSet tileVisualSet;
    public int effectiveFloor;
    public int stairsDownToLevel;
    public int stairsUpToLevel;
    public int stairsDownToModLevelID; // used in player modding
    public int stairsUpToModLevelID; // used in player modding

    public CameraBGColors bgColor;

    public int maxChampions;
    public int maxChampionMods;
    public bool unbreakableWalls;
    public bool poisonAir;
    public bool safeArea;
    public bool sideArea;
    public bool fastTravelPossible;
    public bool convertCenterMapToWater;
    public bool doLake;
    public bool allowPlanks;
    public bool allowDiagonals;
    public bool evenMonsterDistribution;
    public bool excludeFromRumors;
    public bool deepSideAreaFloor;
    public int bonusSparkles;
    public bool itemWorld;
    public int altPath; // Is this a branched (mirrored) path to the main dungeon branch?
    public List<ActorSpawnData> spawnActors;
    public int expectedPlayerLevel;
    public bool hasOverlayText;
    public string overlayRefName;
    public string overlayDisplayName;
    public string overlayText;
    public int randomWaterPools;
    public Vector2 imageOverlayOffset;
    public List<string> clearRewards;

    public List<MusicCueData> altMusicCues;

    public bool showRewardSymbol;
    public bool noRewardPopup;

    // Volcano
    public float extraCrackedRockMultiplier;

    // Related to circle dungeon
    public int minCircleDigs;
    public int maxCircleDigs;
    public int numCircles;
    public int circleRadiusInterval;
    public bool addCentralRoom;

    // Used for auto cave
    public float cellChanceToStartAlive;
    public int cellMaxNeighbors;
    public int cellMinNeighbors;
    public int cellSimulationSteps;
    public float cellMinGroundPercent;
    public float cellMaxGroundPercent;

    // Used for maps with rooms
    public int minRooms;
    public int maxRooms;
    public int maxCorridors;
    public int maxCorridorLength;
    public int maxDeadends;

    // All
    public float correctDiagonalChance;
    public float addStumps;

    public bool dontConnectToAnything;


    public List<string> scripts_postBuild;
    public string script_onEnterMap;
    public string script_preBeautify;
    public string script_onMonsterSpawn;
    public string script_onMonsterDeath;
    public string script_onTurnEnd;

    // Below used for player modding. Players should be able to assign their own "ModLevelID" for dungeon levels.
    // ModLevelID can be whatever they want. We'll remap this value to an auto-assigned floor level when the game starts.
    public int modLevelID;

    
    //parallax scrolling for some dungeon backgrounds
    public string parallaxSpriteRef;
    
    /// <summary>
    /// For each unit away from 0,0 the Camera is, the background will shift this much.
    /// </summary>
    public Vector2 parallaxShiftPerTile;

    /// <summary>
    /// How many times to tile the sprite on the parallax sprite renderer.
    /// </summary>
    public int parallaxTileCount;

    // Use this dictionary for all other random metadata we might use in custom scripts and map gen
    // So we don't make another 100 ints, bools, and floats that are unused for most DungeonLevels...
    public Dictionary<string, int> dictMetaData;

    // If set to a value above 0, then any ENEMY monsters spawned in this level will be raised to AT LEAST this XP level
    // If monster is already this level or higher, nothing happens.
    public int scaleMonstersToMinimumLevel;

    public DungeonLevel()
    {
        dictMetaData = new Dictionary<string, int>();
        altMusicCues = new List<MusicCueData>();
        bgColor = CameraBGColors.STANDARD;
        spawnRateModifier = 1.0f;
        priorityTemplates = new List<RoomTemplate>();
        extraOverlays = new List<string>();
        stairsDownToLevel = 0;
        stairsUpToLevel = 0;
        stairsDownToModLevelID = -1;
        stairsUpToModLevelID = -1;
        altPath = 0;
        spawnActors = new List<ActorSpawnData>();
        imageOverlayOffset = new Vector2();
        clearRewards = new List<string>();
        seasonalImages = new List<SeasonalImageDataPack>();
        correctDiagonalChance = 1.0f;
        dontConnectToAnything = false;
        showRewardSymbol = true;
        scripts_postBuild = new List<string>();
        script_onMonsterSpawn = "";
        script_onMonsterDeath = "";
        script_onEnterMap = "";
        modLevelID = 0;
    }

    public bool HasSpecialClamp()
    {
        if (specialRoomTemplate == null) return false;
        if (specialRoomTemplate.specialClamp) return true;
        return false;
    }

    public int GetMetaData(string metaRef)
    {
        int value = 0;
        if (dictMetaData.TryGetValue(metaRef, out value))
        {
            return value;
        }
        //Debug.LogError("DL floor " + floor + " does not have metadata " + metaRef);
        return value;
    }

    public bool HasOverlayImage()
    {
        if (specialRoomTemplate == null) return false;
        if (imageOverlay == "" || imageOverlay == null) return false;
        return true;
    }

    public static DungeonLevel GetSpecificLevelData(int floor)
    {
        DungeonLevel outFloor;

        if (GameMasterScript.masterDungeonLevelList.TryGetValue(floor, out outFloor))
        {
            return outFloor;
        }
        else
        {
            Debug.Log("Couldn't find floor " + floor);
            return null;
        }
    }

    public static List<DungeonLevel> ReadAllLevelsFromText(string textToRead, XmlReaderSettings settings)
    {
        List<DungeonLevel> levelsRead = new List<DungeonLevel>();
        using (XmlReader reader = XmlReader.Create(new StringReader(textToRead), settings))
        {
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.Name == "Floor")
                {
                    DungeonLevel dLevel = new DungeonLevel();
                    try
                    {
                        dLevel.ReadFromXml(reader);
                        levelsRead.Add(dLevel);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to load dungeon level. " + e);
                    }
                }
                else
                {
                    reader.Read();
                }
            }
            reader.ReadEndElement();
        }
        return levelsRead;
    }

    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement) // was while read...
        {
            string txt;
            #region Dungeon Level Switch Statement            
            switch (reader.Name)
            {
                case "Level":
                    floor = reader.ReadElementContentAsInt();

                    // Handled in GMS now.
                    /* if (GameMasterScript.masterDungeonLevelList.ContainsKey(floor))
                    {
                        Debug.LogError("WARNING! Dungeon floor " + floor + " already in dict, not adding again.");
                    }
                    else
                    {
                        GameMasterScript.masterDungeonLevelList.Add(floor, this);
                        //Debug.Log("Add " + floor + " to dict");
                    } */

                    break;
                case "ModLevelID":
                    modLevelID = reader.ReadElementContentAsInt();
                    break;
                case "MinSpawnFloor":
                    minSpawnFloor = reader.ReadElementContentAsInt();
                    break;
                case "MaxSpawnFloor":
                    maxSpawnFloor = reader.ReadElementContentAsInt();
                    break;
                case "Size":
                    size = reader.ReadElementContentAsInt();
                    break;
                case "MinMonsters":
                    minMonsters = reader.ReadElementContentAsInt();
                    break;
                case "MaxMonsters":
                    maxMonsters = reader.ReadElementContentAsInt();
                    break;
                case "MetaData":
                    // Put this data in our dict which can be used for weird custom map creation scripts
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split('|');
                    if (dictMetaData.ContainsKey(parsed[0]))
                    {
                        Debug.LogError("WARNING! " + floor + " already contains key " + parsed[0] + "!");
                    }
                    else
                    {
                        dictMetaData.Add(parsed[0], Int32.Parse(parsed[1]));
                    }
                    break;
                case "MaxLavaPools":
                    maxLavaPools = reader.ReadElementContentAsInt();
                    break;
                case "AltPath":
                    altPath = reader.ReadElementContentAsInt();
                    break;
                case "MinRooms":
                    minRooms = reader.ReadElementContentAsInt();
                    break;
                case "MaxRooms":
                    maxRooms = reader.ReadElementContentAsInt();
                    break;
                case "NumCircles":
                    numCircles = reader.ReadElementContentAsInt();
                    break;
                case "MinCircleDigs":
                    minCircleDigs = reader.ReadElementContentAsInt();
                    break;
                case "MaxCircleDigs":
                    maxCircleDigs = reader.ReadElementContentAsInt();
                    break;
                case "AddCentralRoom":
                    addCentralRoom = true;
                    reader.Read();
                    break;
                case "ExtraCrackedRockPercent":
                    string pRock = reader.ReadElementContentAsString();
                    extraCrackedRockMultiplier = CustomAlgorithms.TryParseFloat(pRock);
                    break;
                case "Script_PostBuild":
                    scripts_postBuild.Add(reader.ReadElementContentAsString());
                    break;
                case "Script_OnEnterMap":
                    script_onEnterMap = reader.ReadElementContentAsString();
                    break;
                case "Script_PreBeautify":
                    script_preBeautify = reader.ReadElementContentAsString();
                    break;
                case "Script_OnMonsterSpawn":
                    script_onMonsterSpawn = reader.ReadElementContentAsString();
                    MonsterSpawnFunctions.CacheScript(script_onMonsterSpawn);
                    break;
                case "Script_OnMonsterDeath":
                    script_onMonsterDeath = reader.ReadElementContentAsString();
                    break;
                case "Script_OnTurnEnd":
                    script_onTurnEnd = reader.ReadElementContentAsString();
                    TDGenericFunctions.CacheScript(script_onTurnEnd);
                    break;
                case "CircleRadiusInterval":
                    circleRadiusInterval = reader.ReadElementContentAsInt();
                    break;
                case "CorrectDiagonalChance":
                    string sFloat = reader.ReadElementContentAsString();
                    float cdc = CustomAlgorithms.TryParseFloat(sFloat);
                    correctDiagonalChance = cdc;
                    break;
                case "AddStumps":
                    string stumpsFloat = reader.ReadElementContentAsString();
                    addStumps = CustomAlgorithms.TryParseFloat(stumpsFloat);
                    break;
                case "MaxCorridors":
                    maxCorridors = reader.ReadElementContentAsInt();
                    break;
                case "BonusSparkles":
                    bonusSparkles = reader.ReadElementContentAsInt();
                    break;
                case "ExpectedPlayerLevel":
                    expectedPlayerLevel = reader.ReadElementContentAsInt();
                    break;
                case "ImageOverlayOffsetX":
                    txt = reader.ReadElementContentAsString();
                    imageOverlayOffset.x = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "DontConnectToAnything":                    
                    dontConnectToAnything = true;
                    reader.Read();
                    break;
                case "ShowRewardSymbol":
                    showRewardSymbol = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "NoRewardPopup":
                    noRewardPopup = true;
                    reader.Read();
                    break;
                case "SpawnRateModifier":
                    txt = reader.ReadElementContentAsString();
                    spawnRateModifier = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ImageOverlayOffsetY":
                    txt = reader.ReadElementContentAsString();
                    imageOverlayOffset.y = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ClearReward":
                    txt = reader.ReadElementContentAsString();
                    clearRewards.Add(txt);
                    break;
                case "CameraBGColor":
                    bgColor = (CameraBGColors)Enum.Parse(typeof(CameraBGColors), reader.ReadElementContentAsString());
                    break;
                case "SeasonalOverlay":
                    reader.ReadStartElement();
                    SeasonalImageDataPack seasonImageData = new SeasonalImageDataPack();
                    seasonalImages.Add(seasonImageData);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "Season":
                                seasonImageData.whichSeason = (Seasons)Enum.Parse(typeof(Seasons), reader.ReadElementContentAsString());
                                break;
                            case "ReplaceNormalOverlay":
                                seasonImageData.replaceNormalOverlay = true;
                                reader.Read();
                                break;
                            case "SeasonalImage":
                                seasonImageData.seasonalImage = reader.ReadElementContentAsString();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "TextOverlay":
                    reader.ReadStartElement();
                    hasOverlayText = true;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "RefName":
                                overlayRefName = reader.ReadElementContentAsString();
                                break;
                            case "Name":
                                overlayDisplayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                break;
                            case "Text":
                                string refNameToGetString = reader.ReadElementContentAsString();
                                overlayText = StringManager.GetLocalizedStringOrFallbackToEnglish(refNameToGetString);
                                overlayText = CustomAlgorithms.ParseRichText(overlayText, false);
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "MaxChampions":
                    maxChampions = reader.ReadElementContentAsInt();
                    break;
                case "MaxChampionMods":
                    maxChampionMods = reader.ReadElementContentAsInt();
                    break;
                case "MaxCorridorLength":
                    maxCorridorLength = reader.ReadElementContentAsInt();
                    break;
                case "MaxDeadends":
                    maxDeadends = reader.ReadElementContentAsInt();
                    break;
                case "StairsDownToLevel":
                    stairsDownToLevel = reader.ReadElementContentAsInt();
                    break;
                case "StairsUpToLevel":
                    stairsUpToLevel = reader.ReadElementContentAsInt();
                    break;
                case "StairsDownToModLevelID":
                    stairsDownToModLevelID = reader.ReadElementContentAsInt();
                    break;
                case "StairsUpToModLevelID":
                    stairsUpToModLevelID = reader.ReadElementContentAsInt();
                    break;
                case "MaxSecretAreas":
                    maxSecretAreas = reader.ReadElementContentAsInt();
                    break;
                case "RevealAll":                    
                    revealAll = true;
                    reader.Read();
                    break;
                case "EffectiveFloor":
                    effectiveFloor = reader.ReadElementContentAsInt();
                    break;
                case "UnbreakableWalls":
                    unbreakableWalls = true;
                    reader.Read();
                    break;
                case "SafeArea":
                    safeArea = true;
                    reader.Read();
                    break;
                case "FastTravel":
                    fastTravelPossible = true;
                    reader.Read();
                    break;
                case "PoisonAir":
                    poisonAir = true;
                    reader.Read();
                    break;
                case "SideArea":
                    sideArea = true;
                    reader.Read();
                    break;
                case "ConvertCenterMapToWater":
                    convertCenterMapToWater = true;
                    reader.Read();
                    break;
                case "DoLake":
                    doLake = true;
                    reader.Read();
                    break;
                case "AllowPlanks":
                    allowPlanks = true;
                    reader.Read();
                    break;
                case "ItemWorld":
                    itemWorld = true;
                    reader.Read();
                    break;
                case "AllowDiagonals":
                    allowDiagonals = true;
                    reader.Read();
                    break;
                case "RandomWaterPools":
                    randomWaterPools = reader.ReadElementContentAsInt();
                    break;
                case "EvenMonsterDistribution":
                    evenMonsterDistribution = true;
                    reader.Read();
                    break;
                case "ExcludeFromRumors":
                    excludeFromRumors = true;
                    reader.Read();
                    break;
                case "DeepSideAreaFloor":
                    deepSideAreaFloor = true;
                    reader.Read();
                    break;
                case "NoSpawner":
                    noSpawner = true;
                    reader.Read();
                    break;
                case "HasFungus":
                    hasFungus = true;
                    reader.Read();
                    break;
                case "NoCustomRoomTemplates":
                    noCustomRoomTemplates = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];                    
                    break;
                case "PriorityRoomTemplate":
                    string roomRef = reader.ReadElementContentAsString();
                    priorityTemplates.Add(Room.GetRoomTemplate(roomRef));
                    break;
                case "MusicCue":
                    string readmusicCue = reader.ReadElementContentAsString();
                    musicCue = readmusicCue;
                    break;
                case "AlternateMusicCue":
                    reader.ReadStartElement();
                    MusicCueData mcd = new MusicCueData();
                    altMusicCues.Add(mcd);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "ReqPlayerFlag":
                                mcd.altCueFlag = reader.ReadElementContentAsString();
                                break;
                            case "ReqPlayerFlagValue":
                                mcd.altCueFlagValue = reader.ReadElementContentAsInt();
                                break;
                            case "ReqPlayerFlagMeta":
                                mcd.altCueFlagMeta = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "CueName":
                                mcd.altCueName = reader.ReadElementContentAsString();
                                break;
                            case "Season":
                                string test = reader.ReadElementContentAsString();
                                Enum.TryParse<Seasons>(test, out mcd.altCueSeason);
                                //Debug.LogErrorFormat("Tried parsing " + test + " to " + mcd.altCueSeason);
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "SpecialRoomTemplate":
                    string specialTemp = reader.ReadElementContentAsString();
                    specialRoomTemplate = Room.GetRoomTemplate(specialTemp);
                    break;
                case "ImageOverlay":
                    imageOverlay = reader.ReadElementContentAsString();
                    break;
                case "ExtraOverlay":
                    extraOverlays.Add(reader.ReadElementContentAsString());
                    break;
                case "CustomName":
                    customName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "PriorityMinimum":
                    priorityMinimum = reader.ReadElementContentAsInt();
                    break;
                case "LavaRivers":
                    lavaRivers = reader.ReadElementContentAsInt();
                    break;
                case "Rivers":
                case "ExtraRivers":
                    extraRivers = reader.ReadElementContentAsInt();
                    break;
                case "ChallengeValue":
                    txt = reader.ReadElementContentAsString();
                    challengeValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "CaveFillConverter":
                    txt = reader.ReadElementContentAsString();
                    caveFillConvertChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ChanceToOpenWalls":
                    txt = reader.ReadElementContentAsString();
                    chanceToOpenWalls = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "DungeonFloorType":
                case "LayoutType":
                    layoutType = (DungeonFloorTypes)Enum.Parse(typeof(DungeonFloorTypes), reader.ReadElementContentAsString());
                    break;
                case "Tileset":
                    tileVisualSet = (TileSet)Enum.Parse(typeof(TileSet), reader.ReadElementContentAsString());
                    break;
                case "SpawnTable":
                    string tableName = reader.ReadElementContentAsString();
                    ActorTable myTable = GameMasterScript.GetSpawnTable(tableName);
                    spawnTable = myTable;
                    if (myTable == null)
                    {
                        Debug.Log("WARNING! " + floor + " spawn table is null? " + tableName);
                    }
                    break;
                case "BossArea":
                    bossArea = true;
                    reader.Read();
                    break;
                case "CellChanceToStartAlive":
                    txt = reader.ReadElementContentAsString();
                    cellChanceToStartAlive = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "CellMinGroundPercent":
                    txt = reader.ReadElementContentAsString();
                    cellMinGroundPercent = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "CellMaxGroundPercent":
                    txt = reader.ReadElementContentAsString();
                    cellMaxGroundPercent = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "CellMinNeighbors":
                    cellMinNeighbors = reader.ReadElementContentAsInt();
                    break;
                case "CellMaxNeighbors":
                    cellMaxNeighbors = reader.ReadElementContentAsInt();
                    break;
                case "CellSimulationSteps":
                    cellSimulationSteps = reader.ReadElementContentAsInt();
                    break;
                case "SpawnActor":
                    reader.ReadStartElement();
                    ActorSpawnData nsd = new ActorSpawnData();
                    spawnActors.Add(nsd);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        if ((reader.NodeType == XmlNodeType.Whitespace) || (reader.NodeType == XmlNodeType.None))
                        {
                            reader.Read();
                            continue;
                        }
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "actortype":
                                nsd.aType = (ActorTypes)Enum.Parse(typeof(ActorTypes), reader.ReadElementContentAsString());
                                break;
                            case "refname":
                                nsd.refName = reader.ReadElementContentAsString();
                                break;
                            case "reqmetaflag":
                                nsd.metaFlag = reader.ReadElementContentAsString();
                                break;
                            case "flagminvalue":
                                nsd.flagMinValue = reader.ReadElementContentAsInt();
                                break;
                            case "flagmaxvalue":
                                nsd.flagMaxValue = reader.ReadElementContentAsInt();
                                break;
                            case "spawnchance":
                                txt = reader.ReadElementContentAsString();
                                nsd.spawnChance = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "mindistancefromstairsup":
                                nsd.minDistanceFromStairsUp = reader.ReadElementContentAsInt();
                                break;
                            case "maxdistancefromstairsup":
                                nsd.maxDistanceFromStairsUp = reader.ReadElementContentAsInt();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                // "dragonbackground,-0.25,-0.25,40"
                case "ParallaxBackground":
                    var pdata = reader.ReadElementContentAsString();
                    var splitsies = pdata.Split(',');
                    parallaxSpriteRef = splitsies[0];
                    parallaxShiftPerTile = new Vector2( CustomAlgorithms.TryParseFloat(splitsies[1]), CustomAlgorithms.TryParseFloat(splitsies[2]));
                    parallaxTileCount = int.Parse(splitsies[3]);
                    break;
                case "ScaleMonstersToMinimumLevel":
                    scaleMonstersToMinimumLevel = reader.ReadElementContentAsInt();
                    break;
                default:
                    reader.Read();
                    break;
            }
        } // end while
        #endregion

        reader.ReadEndElement();

        //Debug.Log("Read level name: " + customName + " Size: " + size + " Floor: " + floor + " side/safe/boss? " + sideArea + " " + safeArea + " " + bossArea);

        return true;
    }

}