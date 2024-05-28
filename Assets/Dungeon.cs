using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Dungeon
{
    public List<Map> maps;
    //public Map[] levels;
    //public DungeonLevel[] levelData;
    public List<DungeonLevel> levelData;
    public string dungeonName;
    public float challengeScale;
    public float startChallenge;

    /* string[] prefixes = { "Haunt", "Mystery", "Crypt", "Temple", "Ruins", "Underworld", "Mine", "Hells", "Realm", "Labyrinth", "Suffering", "Rest", "Delve", "Catacombs", "Sins", "Dread", "Deep", "Depths", "Quandary", "Riddle", "Enigma", "Caverns", "Subterranea", "Halls", "Calamity", "Citadel", "Stronghold", "Cache", "Capital", "Pinnacle", "Fortress", "Dusk", "Twilight", "Design", "Aberration", "Cataclysm", "Destruction", "Portal", "Spiral", "Pantheon", "Hole", "Tunnels", "Fracture", "Cistern", "Lair", "Colossus" };

    string[] adjectives = { "Shifting", "Shattered", "Ancient", "Clouded", "Untold", "Runic", "Mired", "Puzzling", "Screaming", "Skittering", "Crawling", "Unknown", "Blackened", "Perilous", "Fallen", "Unknown", "Dreaded", "Dire", "Monstrous", "Undaunting", "Abominable", "Wicked", "Distressed", "Burdened", "Fortified", "Gleaming", "Abundant", "Decrepit", "Buried", "Unchained", "Timeless", "Eternal", "Enduring", "Subtle", "Exquisite", "Profound", "Ethereal", "Abyssal", "Abysmal", "Devious", "Cunning", "Creeping", "Bubonic", "Wild", "Lethal", "Grim", "Laughing", "Impenetrable", "Hollow" };

    string[] suffixes = { "Pain", "Mysteries", "Time", "Horror", "Wonders", "Demons", "Beasts", "Greed", "Torment", "Legend", "Judgment", "Legends", "Horrors", "Entombed", "Peril", "Storms", "Knowledge", "Valor", "Battle", "Misdeeds", "Grief", "Suffering", "War", "Spoils", "Secrets", "Punishment", "Riches", "Fiends", "Doubt", "Obscurity", "Shadows", "Contrivance", "Madness", "Hysteria", "Delirium", "Awe", "Confusion", "Vexation", "Defeat", "Demons", "Bane", "Fate", "Agony", "Distress" }; */

    public Monster FindMonsterByRef(float baseCV, string mRef, bool allowChampions)
    {
        // #todo - use baseCV to optimize this somehow? Pretty rough check here.
        for (int i = 0; i < maps.Count; i++)
        {
            foreach (Monster m in maps[i].monstersInMap)
            {
                if (m.actorRefName == mRef)
                {
                    if ((m.isChampion || m.isBoss) && !allowChampions) continue;
                    return m;
                }
            }
        }
        Debug.Log("Monster ref " + mRef + " not found...");
        return null;
    }

    public Map FindFloor(int floorNumber)
    {
        for (int x = 0; x < maps.Count; x++)
        {
            if (maps[x] == null)
            {
                Debug.Log("Null map in array position " + x);
            }
            else if (maps[x].floor == floorNumber)
            {
                return maps[x];
            }

            //Debug.Log(maps[x].floor + " is in dictionary.");
        }

        return null;
    }

    public DungeonLevel GetLevelData(int floor)
    {
        for (int x = 0; x < levelData.Count; x++)
        {
            if ((levelData[x].floor) == floor) // was .floor - 1 in the first part
            {
                return levelData[x];
            }
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            // maybe its a custom level?
            DungeonLevel mdCheck;
            if (DungeonMaker.customDungeonLevelDataInSaveFile.TryGetValue(floor, out mdCheck))
            {
                return mdCheck;
            }
        }

        return null;

    }

    public Map FindFloorViaData(string strData, int iFloorOffset = 1)
    {
        int iBestFloorNum = -1;
        int iFloorsInMatch = 0;
        int iBestStringMatch = 0;
        //for (int t = 0; t < levelData.Count; t++)
        foreach (DungeonLevel dl in GameMasterScript.masterDungeonLevelList.Values)
        {
            //DungeonLevel dl = levelData[t];
            if (dl == null)
            {
                continue;
            }
            //0: no match
            //1: strData appears in the name
            //2: a direct match between strData and one of the words 
            //3: strData IS the map name
            //
            // "amber" 
            //  1: Magma Chamber 1F    
            //  2: Old Amber Station 1F    
            //  
            // 2 is a better match
            //
            //  3: Old Amber Station 2F | "Old Amber Station 2F"
            int iThisMatch = 0;
            if (dl.customName != null)
            {
                iThisMatch = Debug_GetStringMatchValue(dl.customName, strData);
            }

            if (dl.overlayDisplayName != null)
            {
                iThisMatch = Debug_GetStringMatchValue(dl.overlayDisplayName, strData);

            }

            //If this area matches what we're looking for...
            if (iThisMatch != 0 && iThisMatch >= iBestStringMatch)
            {
                //if we just ranked up to a better match
                if (iBestStringMatch != iThisMatch)
                {
                    //Exmple: we had floor 3 for "magma chamber" but that was not as good a match as "old amber station" so start the count over
                    iFloorsInMatch = 0;
                }
                iBestStringMatch = iThisMatch;

                //if we have not yet exceeded the floor offset, this is a candidate
                if (iFloorsInMatch < iFloorOffset)
                {
                    //this is the floor we're looking for
                    iBestFloorNum = dl.floor;
                }

                //increase this, even if it is our first time
                iFloorsInMatch++;


            }

        }

        if (iBestFloorNum > -1)
        {
            return FindFloor(iBestFloorNum);
        }

        return null;
    }

    int Debug_GetStringMatchValue(string strSource, string strDest)
    {
        int iMatchVal = 0;
        strSource = strSource.ToLowerInvariant();
        strDest = strDest.ToLowerInvariant();
        if (strDest == strSource)
        {
            return 3;
        }

        if (strSource.Contains(strDest))
        {
            iMatchVal = 1;
        }

        string[] splitSource = strSource.Split(' ');
        for (int t = 0; t < splitSource.Length; t++)
        {
            if (splitSource[t] == strDest)
            {
                return 2;
            }
        }
        return iMatchVal;
    }

    public void RemoveMapByFloor(int floorID)
    {
        int numMapsRemoved = maps.RemoveAll(m => m.floor == floorID);

        //Debug.Log("Removed " + numMapsRemoved + " matching floor ID " + floorID);

    }

    public DungeonLevel CopyDungeonLevelFromTemplate(DungeonLevel dl)
    {
        DungeonLevel copyOfLevel = new DungeonLevel();
        copyOfLevel.floor = dl.floor;
        copyOfLevel.size = dl.size;
        copyOfLevel.maxSecretAreas = dl.maxSecretAreas;
        copyOfLevel.challengeValue = dl.challengeValue;
        copyOfLevel.bossArea = dl.bossArea;
        copyOfLevel.noSpawner = dl.noSpawner;
        copyOfLevel.hasFungus = dl.hasFungus;
        copyOfLevel.spawnRateModifier = dl.spawnRateModifier;
        copyOfLevel.spawnTable = dl.spawnTable;
        copyOfLevel.layoutType = dl.layoutType;
        foreach (RoomTemplate rt in dl.priorityTemplates)
        {
            copyOfLevel.priorityTemplates.Add(rt);
        }
        foreach (SeasonalImageDataPack sdp in dl.seasonalImages)
        {
            copyOfLevel.seasonalImages.Add(sdp);
        }
        copyOfLevel.specialRoomTemplate = dl.specialRoomTemplate;
        copyOfLevel.priorityMinimum = dl.priorityMinimum;
        copyOfLevel.extraRivers = dl.extraRivers;
        copyOfLevel.lavaRivers = dl.lavaRivers;
        copyOfLevel.chanceToOpenWalls = dl.chanceToOpenWalls;
        copyOfLevel.caveFillConvertChance = dl.caveFillConvertChance;
        copyOfLevel.imageOverlay = dl.imageOverlay;
        copyOfLevel.customName = dl.customName;
        copyOfLevel.revealAll = dl.revealAll;
        foreach (string overlay in dl.extraOverlays)
        {
            copyOfLevel.extraOverlays.Add(overlay);
        }
        copyOfLevel.maxLavaPools = dl.maxLavaPools;
        copyOfLevel.minSpawnFloor = dl.minSpawnFloor;
        copyOfLevel.maxSpawnFloor = dl.maxSpawnFloor;
        copyOfLevel.musicCue = dl.musicCue;
        copyOfLevel.minMonsters = dl.minMonsters;
        copyOfLevel.maxMonsters = dl.maxMonsters;
        copyOfLevel.tileVisualSet = dl.tileVisualSet;
        copyOfLevel.effectiveFloor = dl.effectiveFloor;
        copyOfLevel.stairsDownToLevel = dl.stairsDownToLevel;
        copyOfLevel.stairsUpToLevel = dl.stairsUpToLevel;
        copyOfLevel.bgColor = dl.bgColor;
        copyOfLevel.maxChampionMods = dl.maxChampionMods;
        copyOfLevel.maxChampions = dl.maxChampions;
        copyOfLevel.unbreakableWalls = dl.unbreakableWalls;
        copyOfLevel.poisonAir = dl.poisonAir;
        copyOfLevel.safeArea = dl.safeArea;
        copyOfLevel.sideArea = dl.sideArea;
        copyOfLevel.fastTravelPossible = dl.fastTravelPossible;
        copyOfLevel.convertCenterMapToWater = dl.convertCenterMapToWater;
        copyOfLevel.doLake = dl.doLake;
        copyOfLevel.allowPlanks = dl.allowPlanks;
        copyOfLevel.allowDiagonals = dl.allowDiagonals;
        copyOfLevel.evenMonsterDistribution = dl.evenMonsterDistribution;
        copyOfLevel.excludeFromRumors = dl.excludeFromRumors;
        copyOfLevel.deepSideAreaFloor = dl.deepSideAreaFloor;
        copyOfLevel.bonusSparkles = dl.bonusSparkles;
        copyOfLevel.itemWorld = dl.itemWorld;
        copyOfLevel.altPath = dl.altPath;
        foreach (ActorSpawnData asd in dl.spawnActors)
        {
            copyOfLevel.spawnActors.Add(asd);
        }
        copyOfLevel.expectedPlayerLevel = dl.expectedPlayerLevel;
        copyOfLevel.hasOverlayText = dl.hasOverlayText;
        copyOfLevel.overlayRefName = dl.overlayRefName;
        copyOfLevel.overlayDisplayName = dl.overlayDisplayName;
        copyOfLevel.overlayText = dl.overlayText;
        copyOfLevel.randomWaterPools = dl.randomWaterPools;
        copyOfLevel.imageOverlayOffset = dl.imageOverlayOffset;
        foreach (string clearRewards in dl.clearRewards)
        {
            copyOfLevel.clearRewards.Add(clearRewards);
        }
        foreach (MusicCueData mcd in dl.altMusicCues)
        {
            copyOfLevel.altMusicCues.Add(mcd);
        }
        copyOfLevel.extraCrackedRockMultiplier = dl.extraCrackedRockMultiplier;
        copyOfLevel.minCircleDigs = dl.minCircleDigs;
        copyOfLevel.maxCircleDigs = dl.maxCircleDigs;
        copyOfLevel.numCircles = dl.numCircles;
        copyOfLevel.circleRadiusInterval = dl.circleRadiusInterval;
        copyOfLevel.addCentralRoom = dl.addCentralRoom;
        copyOfLevel.cellChanceToStartAlive = dl.cellChanceToStartAlive;
        copyOfLevel.cellMaxNeighbors = dl.cellMaxNeighbors;
        copyOfLevel.cellMinNeighbors = dl.cellMinNeighbors;
        copyOfLevel.cellSimulationSteps = dl.cellSimulationSteps;
        copyOfLevel.cellMinGroundPercent = dl.cellMinGroundPercent;
        copyOfLevel.cellMaxGroundPercent = dl.cellMaxGroundPercent;
        copyOfLevel.minRooms = dl.minRooms;
        copyOfLevel.maxRooms = dl.maxRooms;
        copyOfLevel.maxCorridorLength = dl.maxCorridorLength;
        copyOfLevel.maxCorridors = dl.maxCorridors;
        copyOfLevel.maxDeadends = dl.maxDeadends;
        copyOfLevel.correctDiagonalChance = dl.correctDiagonalChance;
        copyOfLevel.stairsDownToModLevelID = dl.stairsDownToModLevelID;
        copyOfLevel.stairsUpToModLevelID = dl.stairsUpToModLevelID;

        copyOfLevel.parallaxShiftPerTile = dl.parallaxShiftPerTile;
        copyOfLevel.parallaxSpriteRef = dl.parallaxSpriteRef;
        copyOfLevel.parallaxTileCount = dl.parallaxTileCount;
        copyOfLevel.noRewardPopup = dl.noRewardPopup;
        copyOfLevel.scaleMonstersToMinimumLevel = dl.scaleMonstersToMinimumLevel;

        copyOfLevel.script_onMonsterSpawn = dl.script_onMonsterSpawn;
        copyOfLevel.script_onMonsterDeath = dl.script_onMonsterDeath;
        copyOfLevel.script_onTurnEnd = dl.script_onTurnEnd;
        foreach (string script in dl.scripts_postBuild)
        {
            copyOfLevel.scripts_postBuild.Add(script);
        }

        copyOfLevel.script_onEnterMap = dl.script_onEnterMap;
        copyOfLevel.script_preBeautify = dl.script_preBeautify;

        foreach (string key in dl.dictMetaData.Keys)
        {
            copyOfLevel.dictMetaData.Add(key, dl.dictMetaData[key]);
        }

        return copyOfLevel;
    }

    public Dungeon(int numLevels, float startDiff, float diffScaling)
    {
        maps = new List<Map>();
        levelData = new List<DungeonLevel>();
        //levels = new Map[numLevels];
        //levelData = new DungeonLevel[numLevels];

        /* for (int i = 0; i < levelData.Length; i++)
        {
            DungeonLevel floorTemplate = null;
            foreach(DungeonLevel dl in GameMasterScript.masterDungeonLevelList)
            {
                if (dl.floor == i+1)
                {
                    floorTemplate = dl;
                    break;
                }
            }
            if (floorTemplate == null)
            {
                Debug.Log("No template for floor " + i);
            }
            levelData[i] = floorTemplate;
        } */

        int newGamePlusLevel = GameStartData.saveSlotNGP[GameStartData.saveGameSlot];

        foreach (DungeonLevel dl in GameMasterScript.masterDungeonLevelList.Values)
        {
            DungeonLevel dupeDL = CopyDungeonLevelFromTemplate(dl);
            if (dl.floor < MapMasterScript.CUSTOMDUNGEON_START_FLOOR)
            {
                dupeDL.challengeValue += newGamePlusLevel * 0.3f;
            }
            levelData.Add(dupeDL);
        }

        /*int adj = 0;
        string adjS = "";
        if (UnityEngine.Random.Range(0,3) < 2)
        {
            adj = UnityEngine.Random.Range(0, adjectives.Length);
            adjS = adjectives[adj] + " ";
        }

        dungeonName = "The " + adjS + prefixes[UnityEngine.Random.Range(0, prefixes.Length)];

        if (UnityEngine.Random.Range(0,3) < 2)
        {
            adjS = "";
            if (UnityEngine.Random.Range(0,2) == 1)
            {
                int adj2 = UnityEngine.Random.Range(0, adjectives.Length);
                while (adj2 == adj)
                {
                    adj2 = UnityEngine.Random.Range(0, adjectives.Length);
                }
                adjS = adjectives[adj2] + " ";
            }
            string suffix = suffixes[Random.Range(0, suffixes.Length)];
            dungeonName = dungeonName + " of " + adjS + suffix;
        } */

        startChallenge = startDiff;
        challengeScale = diffScaling;
    }
}

[System.Serializable]
public class DungeonStuff
{
    public float probability;
    public Sprite mySprite;
    public TileTypes tileType;
    public int indexOfSpriteInAtlas;

    public DungeonStuff()
    {

    }
}

[System.Serializable]
public class DungeonProp
{
    public float probability;
    public TileTypes tileType;
    public string prefab;
    public string displayName;

    public DungeonProp()
    {

    }
}