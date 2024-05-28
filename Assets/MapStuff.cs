using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Text;
using System.Linq;

public class RoomAndMapIDDatapack
{
    public int mapID;
    public Map map;
    public Room room;

}

public class MapPoint
{
    public int x;
    public int y;

    public MapPoint(int nx, int ny)
    {
        x = nx;
        y = ny;
    }
}

public class RoomTemplate
{
    public int numRows;
    public int numColumns;
    public int visualRows; // for image overlays
    public int visualColumns; // for image overlays
    public int calcArea;
    public string[] rowData;
    public string refName;
    public bool initialized;
    public bool specialArea;
    //public List<DungeonFloorTypes> possibleLayouts;
    public bool[] possibleLayouts;
    public bool specialClamp;
    public float clampMinX;
    public float clampMinY;
    public float clampMaxX;
    public float clampMaxY;
    public float specificXPos;
    public float specificYPos;
    public bool hasModPlayerIDDef; // used for player modding
    public Dictionary<char, CharDefinitionForRoom> dictCharDef;

    public RoomTemplate()
    {
        initialized = false;
        numColumns = 0;
        numRows = 0;
        visualColumns = 0;
        visualRows = 0;
        specialArea = false;
        possibleLayouts = new bool[(int)DungeonFloorTypes.COUNT];
        specificXPos = 0f;
        specificYPos = 0f;
        dictCharDef = new Dictionary<char, CharDefinitionForRoom>();
    }

    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();

        int curRow = 0;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string txt;
            switch (reader.Name)
            {
                case "RefName":
                    refName = reader.ReadElementContentAsString();
                    if (GameMasterScript.masterDungeonRoomlist.ContainsKey(refName))
                    {
                        Debug.Log("WARNING! Dungeon room " + refName + " already exists in dict.");
                    }
                    else
                    {
                        GameMasterScript.masterDungeonRoomlist.Add(refName, this);
                    }
                    break;
                case "PossibleLayoutType":
                    DungeonFloorTypes dft = (DungeonFloorTypes)Enum.Parse(typeof(DungeonFloorTypes), reader.ReadElementContentAsString());
                    possibleLayouts[(int)dft] = true;
                    GameMasterScript.masterDungeonRoomsByLayout[(int)dft].Add(this);
                    break;
                case "SpecialArea":
                    specialArea = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "SpecialClamp":
                    specialClamp = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ClampMinX":
                    txt = reader.ReadElementContentAsString();
                    clampMinX = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ClampMaxX":
                    txt = reader.ReadElementContentAsString();
                    clampMaxX = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ClampMinY":
                    txt = reader.ReadElementContentAsString();
                    clampMinY = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ClampMaxY":
                    txt = reader.ReadElementContentAsString();
                    clampMaxY = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Rows":
                    numRows = reader.ReadElementContentAsInt();
                    rowData = new string[numRows];
                    break;
                case "Columns":
                    numColumns = reader.ReadElementContentAsInt();
                    break;
                case "VisualRows":
                    visualRows = reader.ReadElementContentAsInt();
                    break;
                case "VisualColumns":
                    visualColumns = reader.ReadElementContentAsInt();
                    break;
                case "SpecificXPos":
                    txt = reader.ReadElementContentAsString();
                    specificXPos = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "SpecificYPos":
                    txt = reader.ReadElementContentAsString();
                    specificYPos = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Row":
                    string readRowData = reader.ReadElementContentAsString();
                    rowData[curRow] = readRowData;
                    curRow++;
                    break;
                case "StandardCharDefinitions":
                    reader.ReadElementContentAsInt();

                    CharDefinitionForRoom standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'u';
                    standardDef.eCharType = RoomCharTypes.STAIRS;
                    standardDef.stairDir = StairDirections.BACK;
                    dictCharDef.Add('u', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'd';
                    standardDef.eCharType = RoomCharTypes.STAIRS;
                    standardDef.stairDir = StairDirections.FORWARDS;
                    dictCharDef.Add('d', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'T';
                    standardDef.eCharType = RoomCharTypes.TREE;
                    dictCharDef.Add('T', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'p';
                    standardDef.eCharType = RoomCharTypes.PLAYERSTART;
                    dictCharDef.Add('p', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 't';
                    standardDef.eCharType = RoomCharTypes.TREASURESPARKLE;
                    dictCharDef.Add('t', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'b';
                    standardDef.eCharType = RoomCharTypes.DESTRUCTIBLE;
                    standardDef.actorRef.Add("obj_barrel");
                    standardDef.actorRef.Add("obj_crate");
                    standardDef.actorRef.Add("obj_cratestack");
                    dictCharDef.Add('b', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'f';
                    standardDef.eCharType = RoomCharTypes.DESTRUCTIBLE;
                    standardDef.actorRef.Add("obj_regenfountain");
                    dictCharDef.Add('f', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'w';
                    standardDef.eCharType = RoomCharTypes.TERRAIN;
                    standardDef.locTag = LocationTags.WATER;
                    standardDef.changeTileType = true;
                    standardDef.eTileType = TileTypes.GROUND;
                    dictCharDef.Add('w', standardDef);

                    standardDef = new CharDefinitionForRoom();
                    standardDef.symbol = 'L';
                    standardDef.eCharType = RoomCharTypes.TERRAIN;
                    standardDef.locTag = LocationTags.LAVA;
                    standardDef.changeTileType = true;
                    standardDef.eTileType = TileTypes.GROUND;
                    dictCharDef.Add('L', standardDef);

                    break;
                case "CharDef":
                    CharDefinitionForRoom cdfr = RoomBuilder.ReadCharDefinition(reader);
                    try
                    {
                        if (!dictCharDef.ContainsKey(cdfr.symbol))
                        {
                            dictCharDef.Add(cdfr.symbol, cdfr);
                        }
                        else
                        {
                            dictCharDef[cdfr.symbol] = cdfr;
                        }
                        if (cdfr.eCharType == RoomCharTypes.STAIRS && cdfr.pointToModLevelID >= 0)
                        {
                            // This RoomTemplate is now marked as having player mod data that references OTHER player mod data
                            // We have to link this stuff up properly during LoadAllMapGenerationData.
                            hasModPlayerIDDef = true;
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.Log(cdfr.symbol + " already defined in " + refName + ": " + dictCharDef[cdfr.symbol]);
                        Debug.Log(e);
                    }
                    break;
                default:
                    reader.Read();
                    break;
            } // End of switch
        } // End of RT read

        reader.ReadEndElement();

        if (numRows > 0 && numColumns > 0 && !initialized)
        {
            initialized = true;
            calcArea = numColumns * numRows;
        }
        else
        {
            Debug.LogError("Error! Room template " + refName + " has invalid size. Rows/columns: " + numRows + "/" + numColumns);
            return false;
        }

        return true;

    }

    public void InitializeRT()
    {
        rowData = new string[numRows];
    }
}

public class Area
{
    public bool initialized;
    public bool isCorridor;
    public Vector2 origin;
    public Vector2 size;
    public int areaID { get; set; }
    public bool reachable;
    public List<MapTileData> internalTiles;
    public List<MapTileData> edgeTiles { get; set; }
    //public HashSet<MapTileData> internalTiles;
    //public HashSet<MapTileData> edgeTiles;
    public List<Area> connections;
    public List<int> connectionIDs;
    public int connectionTries;
    public int deadends;
    public Vector2 center;
    public bool hasStairs;
    public List<Actor> props;
    public AreaTypes areaType;
    bool propsInitialized;

    public Area()
    {
        /* origin = Vector2.zero;
        size = Vector2.zero;         */
        areaID = -1000; // Default.
        // list was here.
    }

    public virtual void InitializeLists()
    {
        if (initialized) return;
        initialized = true;
        int maxInternalSize = (int)((size.x - 1) * (size.y - 1));
        int maxEdgeSize = (int)((size.x * 2) + (size.y * 2));
        internalTiles = new List<MapTileData>(maxInternalSize);
        edgeTiles = new List<MapTileData>(maxEdgeSize);
        connections = new List<Area>(10);
        props = new List<Actor>(5);
    }

    public List<Actor> GetProps()
    {
        if (propsInitialized)
        {
            return props;
        }
        else
        {
            props = new List<Actor>();
            propsInitialized = true;
            return props;
        }
    }
}

public class Room : Area
{
    public RoomTemplate template;
    public Room()
    {
        //edgeTiles = new List<MapTileData>();
        //internalTiles = new List<MapTileData>();
        //connections = new List<Area>(5);
        deadends = 0;
        connectionTries = 0;
        areaType = AreaTypes.ROOM;
        initialized = false;
        // Constructor
    }

    public string GetTemplateName()
    {
        if (template == null) return "";

        return template.refName;
    }

    public MapTileData GetEmptyTile()
    {
        if (internalTiles.Count == 0) return null;
        List<MapTileData> possibleMTD = new List<MapTileData>();
        List<MapTileData> idealMTD = new List<MapTileData>();
        foreach (MapTileData mtd in internalTiles)
        {
            if (mtd.tileType != TileTypes.GROUND)
            {
                continue;
            }
            if (mtd.IsCollidableActorInTile(GameMasterScript.heroPCActor))
            {
                continue;
            }
            if (mtd.AreItemsInTile())
            {
                possibleMTD.Add(mtd);
                continue;
            }
            idealMTD.Add(mtd);
        }
        if (idealMTD.Count > 0)
        {
            return idealMTD[UnityEngine.Random.Range(0, idealMTD.Count)];
        }
        else
        {
            return possibleMTD[UnityEngine.Random.Range(0, possibleMTD.Count)];
        }
    }
    public override void InitializeLists()
    {
        if (initialized) return;
        initialized = true;
        edgeTiles = new List<MapTileData>();
        internalTiles = new List<MapTileData>();
        connections = new List<Area>(5);
    }

    public static RoomTemplate GetRoomTemplate(string refName)
    {
        RoomTemplate outRT;

        if (GameMasterScript.masterDungeonRoomlist.TryGetValue(refName, out outRT))
        {
            return outRT;
        }
        else
        {
            Debug.Log("Couldn't find RT " + refName);
            return null;
        }
    }
}

public class Corridor : Area
{
    public int length { get; set; }
    public Directions direction;

    public Corridor()
    {
        isCorridor = true;
        //internalTiles = new List<MapTileData>();
        //edgeTiles = new List<MapTileData>();
        //connections = new List<Area>();
        deadends = 0;
        areaType = AreaTypes.CORRIDOR;
        initialized = false;
    }

    public override void InitializeLists()
    {
        if (initialized) return;
        initialized = true;
        internalTiles = new List<MapTileData>();
        edgeTiles = new List<MapTileData>();
        connections = new List<Area>();
    }
}

public class MapGenerationData
{
    public int columns;
    public int rows;
    public float chanceConvertWallToGround;
    public float[] chanceToUseRoomTemplate;
    public float chanceToSpawnChampionMonster;
    public int maxChampMods;
    public int maxChamps;

    public Sprite[] doorTiles;
    public DungeonStuff[] randomDecor;
    public DungeonStuff[] decor3x3;
    public DungeonProp[] randomProps;
    public float anyDecorChance;
    public float grassDecorChance;
    public float anyPropChance;
    public int maxPropsPerRoom;
    public int minRoomSize;
    public int maxRoomSize;
    public int maxRooms;
    public int minRooms;
    public int maxRoomAttempts;
    public int maxCorridors;
    public int maxCorridorAttempts;
    public int maxCorridorLength;
    public float corridorOrigRandom;
    public int minDeadendLength;
    public int maxDeadendLength;
    public int maxDeadends;
    public int minMonstersPerRoom;
    public int maxMonstersPerRoom;
}

public partial class Map
{
    public MapMasterScript mms;
    static List<MapTileData> pool_tileList = new List<MapTileData>();
    static List<Actor> actorsToMove = new List<Actor>();
    static List<MapTileData> pool_tileList2 = new List<MapTileData>();
    static List<MapTileData> pool_connectingTiles = new List<MapTileData>();
    static List<RoomTemplate> possibleRT = new List<RoomTemplate>();
    static Room roomToReturn;
    public MapGenerationData mgd;
    public MapTileData[,] mapArray;
    public int[,] areaIDArray;
    public List<Actor> actorsInMap;
    public List<Room> mapRooms;
    public List<Corridor> mapCorridors;
    //public List<DoorData> mapDoors;
    public List<Actor> mapDecor;
    public int mapAreaID;
    public int areaIDCounter;
    public List<Stairs> mapStairs;
    public int floor;
    public int effectiveFloor;
    public int numFloors;
    public float challengeRating;
    public float unusedArea;
    public bool[,] exploredTiles;
    public bool spawnerAlive;
    public Dictionary<int, Area> areaDictionary;
    public bool mapIsHidden; // Used only for map generation.
    public bool clearedMap;
    public bool alwaysSpin;
    public bool costumeParty;
    public bool bigMode;
    public int levelDataLink;
    public List<Monster> monstersInMap;

    public DungeonLevel dungeonLevelData;

    public Area heroStartArea;
    public Vector2 heroStartTile;

    public int initialSpawnedMonsters;
    public int unfriendlyMonsterCount;
    public int championCount;

    public int columns;
    public int rows;
    public List<MonsterTemplateData> possibleMonstersToSpawn;

    // Related to map gen.
    private bool priorityRoomsBuilt = false;
    private int numPriorityRoomsBuilt = 0;
    //private List<Vector2> possibleTiles;
    private List<MapTileData> waterTiles;
    List<MapTileData> genericTileList;

    public string musicCurrentlyPlaying;

    public bool usedByMapGen;

    public float bonusRewards;
    public List<Actor> fountainsInMap;

    static RoomCreationResult FailedRoomCreation;

    MapPoint[] localPointArray;
    int localPointArrayMaxX;
    int localPointArrayMaxY;
    //Dictionary<MapPoint, List<RoomTemplate>> localUnenforcedRoomDict;
    //Dictionary<MapPoint, List<RoomTemplate>> localEnforcedRoomDict;

    List<RoomTemplate>[] localUnenforcedRoomDict;
    List<RoomTemplate>[] localEnforcedRoomDict;

    HashSet<MapTileData> internalTilesHash;
    HashSet<MapTileData> edgeTilesHash;

    public Dictionary<string, int> dictMapDataForGeneration;

    // Maintain link of switch indexes to the gates they destroy.
    public Dictionary<int, List<Destructible>> linkSwitchesToGates;

    // For pooling
    static bool monsterSeedStuffInitialized;
    static List<Monster> monstersThisMap;
    static List<string> priorityItemRefs;
    static ActorTable localSpawnTable;
    static List<string> monsterRefsToScale;
    static Dictionary<int, string> mapMonsterTypeEnumToFamilyRef;

    static PriorityQueue<MapTileData> openList;
    static List<MapTileData> tilePath;

    public bool initialized;

    public List<Area> possibleAreas;

    public static void ResetAllVariablesToGameLoad()
    {
        pool_tileList = new List<MapTileData>();
        actorsToMove = new List<Actor>();
        pool_tileList2 = new List<MapTileData>();
        pool_connectingTiles = new List<MapTileData>();
        possibleRT = new List<RoomTemplate>();
        roomToReturn = null;
        monstersThisMap = new List<Monster>();        
    }

    /// <summary>
    /// Required so that we can inherit from this class.
    /// </summary>
    public Map()
    {
        #if UNITY_EDITOR
        //Debug.LogWarning("Please construct Map class objects using the Map(int mFloor) call.");
        #endif
    }

    static void InitializeMonsterSeedStuff()
    {
        if (monsterSeedStuffInitialized) return;

        monstersThisMap = new List<Monster>();
        priorityItemRefs = new List<string>();
        localSpawnTable = new ActorTable();
        monsterRefsToScale = new List<string>();

        mapMonsterTypeEnumToFamilyRef = new Dictionary<int, string>();
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_BANDITS, "bandits");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_JELLIES, "jelly");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_BEASTS, "beasts");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_FROGS, "frogs");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_INSECTS, "insects");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_ROBOTS, "robots");
        mapMonsterTypeEnumToFamilyRef.Add((int)ItemWorldProperties.TYPE_FAMILY_HYBRIDS, "hybrids");

        monsterSeedStuffInitialized = true;
    }

    /// <summary>
    /// Moved from the constructor so that it can be called by the kiddos
    /// </summary>
    /// <param name="mFloor"></param>
    protected virtual void Initialize(int mFloor)
    {
        if (initialized) return;

        dictMapDataForGeneration = new Dictionary<string, int>();
        internalTilesHash = new HashSet<MapTileData>();
        edgeTilesHash = new HashSet<MapTileData>();
        FailedRoomCreation = new RoomCreationResult(null, false);
        genericTileList = new List<MapTileData>();
        mapIsHidden = false;
        areaDictionary = new Dictionary<int, Area>();
        spawnerAlive = true;
        mms = GameObject.Find("MapMaster").GetComponent<MapMasterScript>();
        actorsInMap = new List<Actor>();
        monstersInMap = new List<Monster>();
        mapRooms = new List<Room>();
        mapCorridors = new List<Corridor>();
        mapStairs = new List<Stairs>();
        mapDecor = new List<Actor>();
        waterTiles = new List<MapTileData>();
        floor = mFloor;
        areaIDCounter = 10 + (floor * 160);
        fountainsInMap = new List<Actor>();
        if (floor < 100)
        {
            effectiveFloor = floor;
        }
        usedByMapGen = false;
        musicCurrentlyPlaying = "";
        unfriendlyMonsterCount = 0;
        bonusRewards = 0f;
        possibleAreas = new List<Area>();
        initialized = true;
    }
    
    public Map(int mFloor)
    {
        Initialize(mFloor);
    }

    public bool IsBossFloor()
    {
        return dungeonLevelData.bossArea;
    }

    public void LoadAndPlayMusic()
    {            
        if (dungeonLevelData == null) // 4112019 - should never happen, yadda yadda
        {
            return;
        }
        if (!string.IsNullOrEmpty(musicCurrentlyPlaying) && (dungeonLevelData.altMusicCues.Count == 0 || GameEventsAndTriggers.ShouldCutscenesBeSkipped()))
        {
			MusicManagerScript.RequestPlayMusic(musicCurrentlyPlaying, true, true);

        }
        else
        {
            // No existing saved music.
            HeroPC heroPCActor = GameMasterScript.heroPCActor;

            if (heroPCActor.ReadActorData("viewfloor" + floor.ToString()) == 999)
            {
                dungeonLevelData.musicCue = "postboss";
                musicCurrentlyPlaying = "postboss";
            }

            if (!string.IsNullOrEmpty(dungeonLevelData.musicCue))
            {
                string cueToPlay = dungeonLevelData.musicCue;
                bool usingAlternateCue = false;
                foreach (MusicCueData mcd in dungeonLevelData.altMusicCues)
                {
                    //Debug.LogError("Check " + mcd.altCueName + " " + mcd.altCueSeason + " " + mcd.altCueFlag);
                    if (mcd.altCueSeason != Seasons.COUNT)
                    {
                        if (!GameMasterScript.seasonsActive[(int)mcd.altCueSeason])
                        {
                            //Debug.LogError("Wrong season");
                            continue;
                        }
                        //Debug.LogError("Matched " + mcd.altCueName);
                        cueToPlay = mcd.altCueName;
                        usingAlternateCue = true;
                    }

                    //Debug.Log(mcd.altCueName);
                    int playerFlagValue = heroPCActor.ReadActorData(mcd.altCueFlag);
                    if (mcd.altCueFlagMeta)
                    {
                        playerFlagValue = MetaProgressScript.ReadMetaProgress(mcd.altCueFlag);
                    }
                    //Debug.Log("PFV is " + playerFlagValue + " " + mcd.altCueFlagValue);
                    if (playerFlagValue >= mcd.altCueFlagValue)
                    {
                        cueToPlay = mcd.altCueName;
                        usingAlternateCue = true;
                    }
                    else 
                    {
                        usingAlternateCue = false;
                    }
                }
                musicCurrentlyPlaying = cueToPlay;

                bool playFromPreviousPosition = true;

                if (usingAlternateCue) playFromPreviousPosition = false;

                MusicManagerScript.RequestPlayMusic(cueToPlay, true, playFromPreviousPosition);
            }
            else
            {			
                if (PlatformVariables.USE_INTROLOOP)
                {
                    string cueLoaded = GameMasterScript.musicManager.LoadDungeonMusicAtRandom(this, IsItemWorld());
                    musicCurrentlyPlaying = cueLoaded;
                    GameMasterScript.musicManager.Play_WithIntroloop(true, true);
                }
                else
                {
	                string cueLoaded = GameMasterScript.musicManager.SelectDungeonMusicAtRandom(this, IsItemWorld());
	                musicCurrentlyPlaying = cueLoaded;
    	            MusicManagerScript.LoadAndPlayTrack_NoIntroLoop(musicCurrentlyPlaying, true, true);
                }

            }
        }

        
    }

    public void RedirectStairs(int currentFloor, Map redirectToThisMap)
    {
        foreach (Stairs st in mapStairs)
        {
            if (st.NewLocation == null) continue;
            if (st.NewLocation.floor == currentFloor || st.pointsToFloor == currentFloor)
            {
                st.NewLocation = redirectToThisMap;
                st.pointsToFloor = redirectToThisMap.floor;
                st.newLocationID = redirectToThisMap.mapAreaID;
            }
        }
    }

    public bool IsMainPath()
    {
        if (floor == MapMasterScript.TUTORIAL_FLOOR_2 || floor == MapMasterScript.TUTORIAL_FLOOR)
        {
            return false;
        }
        if (floor < 100)
        {
            return true;
        }
        if (dungeonLevelData.altPath > 0)
        {
            return true;
        }
        return false;
    }

    public void Init()
    {
        if (!initialized)
        {
            Initialize(floor);
        }

        if (dungeonLevelData == null) Debug.Log(floor + " no DLD?");

        if (dungeonLevelData == null)
        {
            return;
        }

        CreateTileArrays(dungeonLevelData.size);
        
        challengeRating = dungeonLevelData.challengeValue;
    }

    public bool IsItemWorld()
    {
        if (floor >= MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_START && floor <= MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_END)
        {
            return true;
        }

        return floor >= MapMasterScript.ITEMWORLD_START_FLOOR &&
               floor < MapMasterScript.ITEMWORLD_END_FLOOR;
    }

    public Actor FindActorByID(int id)
    {
        foreach (Actor act in actorsInMap)
        {
            //Debug.Log("<color=yellow>SEARCH FOR:</color> " + id + " vs. " + act.displayName + " " + act.actorRefName + " " + act.actorUniqueID);
            if (act.actorUniqueID == id)
            {
                return act;
            }
        }
        //if (Debug.isDebugBuild) Debug.Log("ID " + id + " not found in " + floor + " " + mapAreaID);
        return null;
    }

    public void PrintMonstersRemainingToLogIfNecessary()
    {
        if ((IsClearableSideArea() || IsJobTrialFloor()) && unfriendlyMonsterCount <= 5)
        {
            if (unfriendlyMonsterCount < 0)
            {
                RecountMonsters();
            }
            StringManager.SetTag(0, unfriendlyMonsterCount.ToString());
            GameLogScript.LogWriteStringRef("log_monsters_remaining");
        }
    }

    public bool IsClearableSideArea()
    {
        if (dungeonLevelData.noSpawner && dungeonLevelData.sideArea && !IsTownMap() && !IsItemWorld() && !dungeonLevelData.safeArea &&
            floor != MapMasterScript.FINAL_HUB_FLOOR && !dungeonLevelData.noRewardPopup)
        {

            // Special case: For "memory" dream levels, like side areas re-used in dreams, make sure these don't technically count as side areas.
            if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsItemWorld())
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public Actor FindActor(string refName)
    {
        foreach (Actor act in actorsInMap)
        {
            if (act.actorRefName == refName)
            {
                return act;
            }
        }
#if UNITY_EDITOR
        Debug.Log("COULD NOT FIND actor ref in map " + floor + ": " + refName);
#endif
        return null;
    }

    public float GetChallengeRating()
    {
        float maxChallengeRating = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_CHALLENGE_RATING : GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION;

        if (challengeRating >= maxChallengeRating)
        {
            challengeRating = maxChallengeRating;
        }

        return challengeRating;
    }

    public Map GetNearbyPathMap()
    {
        Map baseFloor = this;
        Map processFloor = this;

        bool debug = false;

        if (dungeonLevelData == null)
        {
            return null;
        }

        if (dungeonLevelData.deepSideAreaFloor)
        {
            baseFloor = null;
            int attempts = 0;
            while (baseFloor == null)
            {
                attempts++;
                if (attempts > 100) break;
                foreach (Stairs st in processFloor.mapStairs)
                {
                    if (!st.stairsUp) continue;
                    if (st.NewLocation != null)
                    {
                        if (st.NewLocation.IsMainPath())
                        {
                            if (debug) Debug.Log(st.NewLocation.GetName() + " " + st.NewLocation.floor + " connects to " + processFloor + " so we'll use that.");
                            baseFloor = processFloor;
                            break;
                        }
                        processFloor = st.NewLocation;
                        if (debug) Debug.Log("Stairs up lead to " + processFloor.GetName() + " " + processFloor.floor + " so let's keep searching.");
                        break;
                    }
                }
            }
            if (attempts >= 100)
            {
                if (Debug.isDebugBuild) Debug.Log("Circular reference trying to find nearby path map to " + floor + " " + GetName() + " as it is a deep side area floor.");
                return processFloor;
            }
        }

        if (baseFloor == null) return processFloor;

        foreach (Stairs st in baseFloor.mapStairs)
        {
            if (st.NewLocation != null && st.NewLocation.IsMainPath())
            {
                if (debug) Debug.Log(st.GetPos() + " on floor " + baseFloor + " lead to " + st.NewLocation.floor + " " + st.NewLocation.GetName() + " so that's it!");
                return st.NewLocation;
            }
            if (st.pointsToFloor > 0)
            {
                Map m = MapMasterScript.theDungeon.FindFloor(st.pointsToFloor);
                if (m == null)
                {
                    continue;
                }
                if (m.IsMainPath())
                {
                    if (debug) Debug.Log(st.GetPos() + " on floor " + baseFloor + " pointstofloor to " + m.floor + " " + m.GetName() + " so that's it!");
                    return m;
                }
            }
        }

        return processFloor;
    }

    public string GetNearbyPathFloor(bool allowHidden = false)
    {
        if (mapIsHidden && !allowHidden) return "";

        if (floor == MapMasterScript.RIVERSTONE_WATERWAY_START)
        {
            return "";
        }

        Map checkMap = null;
        
        try
        {
            checkMap = GetNearbyPathMap();
        }
        catch(Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Uh oh, getting nearby path map for " + GetName() + "," + floor + " failed!");
        }
        
        if (checkMap != null)
        {
            return checkMap.GetName();
        }

        return "";
    }

    public string GetName()
    {
        if (dungeonLevelData == null)
        {
            return "";
        }
        if (!string.IsNullOrEmpty(dungeonLevelData.customName) && !IsItemWorld() && !IsMainPath())
        {
            return dungeonLevelData.customName;
        }
        else
        {
            if (!IsItemWorld())
            {
                string dispFloor = "";
                if (dungeonLevelData.altPath == 0)
                {
                    dispFloor = "(" + (floor + 1) + "F)";
                    //return "Tangledeep " + (floor + 1) + "F";
                    return dungeonLevelData.customName + " " + dispFloor;
                }
                else
                {
                    dispFloor = "(" + (effectiveFloor + 1) + "F)";
                    //return "Tangledeep " + (effectiveFloor + 1) + "F";
                    return dungeonLevelData.customName + " " + dispFloor;
                }

            }
            else
            {
                int index = 0;
                if (MapMasterScript.itemWorldMaps != null)
                {
                    if (mapAreaID == GameMasterScript.heroPCActor.ReadActorData("iw_np_floor"))
                    {
                        return StringManager.GetString("name_nightmareprince_lair");
                    }

                    for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
                    {
                        if (MapMasterScript.itemWorldMaps[i] == this)
                        {
                            index = i;
                            break;
                        }
                    }
                    index++;

                    if (index >= 0)
                    {
                    StringManager.SetTag(0, index.ToString());
                    }
                    else
                    {
                        StringManager.SetTag(0, "???");
                    }                    

                    if (ItemDreamFunctions.IsItemDreamNightmare())
                    {
                        return StringManager.GetString("misc_item_nightmare_specificfloor");
                    }
                    else
                    {
                        return StringManager.GetString("misc_item_dream_specificfloor");
                    }


                }
                else
                {
                    return StringManager.GetString("misc_item_dream");
                }
            }

        }
    }

    public void ExploreAllTiles()
    {
        int xMax = exploredTiles.GetLength(0);
        int yMax = exploredTiles.GetLength(1);
        for (int x = 0; x < xMax; x++)
        {
            for (int y = 0; y < yMax; y++)
            {
                exploredTiles[x, y] = true;
            }
        }
    }

    public List<MapTileData> GetCardinalTilesAroundPoint(int x, int y)
    {
        pool_tileList.Clear();

        if (x - 1 >= 1)
        {
            pool_tileList.Add(mapArray[x - 1, y]);
        }

        if (x + 1 <= columns - 1)
        {
            pool_tileList.Add(mapArray[x + 1, y]);
        }

        if (y + 1 <= rows - 1)
        {
            pool_tileList.Add(mapArray[x, y + 1]);
        }

        if (y - 1 >= 1)
        {
            pool_tileList.Add(mapArray[x, y - 1]);
        }

        return pool_tileList;
    }

    public List<MapTileData> GetCardinalTilesAroundPoint2(int x, int y)
    {
        pool_tileList2.Clear();

        if (x - 1 >= 1)
        {
            pool_tileList2.Add(mapArray[x - 1, y]);
        }

        if (x + 1 <= columns - 1)
        {
            pool_tileList2.Add(mapArray[x + 1, y]);
        }

        if (y + 1 <= rows - 1)
        {
            pool_tileList2.Add(mapArray[x, y + 1]);
        }

        if (y - 1 >= 1)
        {
            pool_tileList2.Add(mapArray[x, y - 1]);
        }


        return pool_tileList2;
    }

    public List<MapTileData> GetListOfTilesAroundPoint(Vector2 centerTile, int radius)
    {
        pool_tileList.Clear();

        int startX = (int)centerTile.x - radius;

        if (startX <= 0)
        {
            startX = 1;
        }

        int endX = (int)centerTile.x + radius;

        if (endX >= columns)
        {
            endX = columns - 2;
        }

        int startY = (int)centerTile.y - radius;

        if (startY <= 0)
        {
            startY = 1;
        }

        int endY = (int)centerTile.y + radius;

        if (endY >= rows)
        {
            endY = rows - 2;
        }

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                MapTileData mtd = mapArray[x, y];
                if (mtd.tileType != TileTypes.NOTHING)
                {
                    //Debug.Log("Adding tile " + x + "," + y);
                    pool_tileList.Add(mtd);
                }
            }
        }

        return pool_tileList;
    }

    /// <summary>
    /// Also removes actor from its own tile based on GetPos, and recounts monsters.
    /// </summary>
    /// <param name="act"></param>
    public void RemoveActorFromMap(Actor act)
    {
        Vector2 pos = act.GetPos();
        RemoveActorFromLocation(pos, act);

#if UNITY_EDITOR
        if (floor == 150 && act.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = act as Monster;
            if (mn.tamedMonsterStuff != null)
            {
                Debug.Log("Removing " + mn.PrintCorralDebug() + " from grove.");
            }
            else
            {
                Debug.Log("Removing " + act.actorRefName + " " + act.actorUniqueID + " " + act.displayName + " from grove.");
            }
            
        }
#endif


        int count = actorsInMap.Count;

        actorsInMap.Remove(act);

        if (actorsInMap.Count == count)
        {
            // Count didn't change, meaning actor wasn't in map, so don't do anything else.
            return;
        }

        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mAct = act as Monster;
            // New in 2/26/18 - "Dungeon" faction stuff like shrooms do NOT affect monster count.
            if (mAct.actorfaction != Faction.PLAYER && mAct.bufferedFaction != Faction.PLAYER && mAct.actorfaction != Faction.DUNGEON)
            {
                unfriendlyMonsterCount--;
                //Debug.Log("Current monster count floor " + floor + ": " + monsterCount + " removed " + act.actorRefName);
            }
            monstersInMap.Remove(act as Monster);
        }
        if (act.GetActorType() == ActorTypes.STAIRS)
        {
            Stairs st = act as Stairs;
            //Debug.Log("Removing stairs from map " + floor);
            mapStairs.Remove(st);
        }
        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = act as Destructible;
            if (dt.mapObjType == SpecialMapObject.FOUNTAIN)
            {
                fountainsInMap.Remove(dt);
            }
        }
        if (act.GetActorType() == ActorTypes.NPC)
        {
            // Do nothing?
        }
    }

    public bool AddActorToMap(Actor act, bool forceAddActor = false)
    {

        /* if (Debug.isDebugBuild && floor == 150 && act.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = act as Monster;
            string tcm = "";
            if (mn.tamedMonsterStuff != null)
            {
                tcm = " in corral? " + mn.isInCorral + " sharedbankid " + mn.tamedMonsterStuff.sharedBankID;
            }
           Debug.Log(Time.time + " Adding " + act.actorRefName + " " + act.actorUniqueID + " " + act.prefab + " " + act.displayName + " to grove. TCM: " + tcm);
        }  */

        if (!forceAddActor) // Don't leave this here forever. Remove eventually.
        {
            if (!MapMasterScript.dungeonCreativeActive)
            {
                foreach (Actor existingAct in actorsInMap)
                {
                    if (existingAct == act || (existingAct.actorUniqueID == act.actorUniqueID && existingAct.GetActorType() == act.GetActorType()))
                    {
                        if (Debug.isDebugBuild) Debug.Log("WARNING! Actor " + existingAct.actorRefName + "," + existingAct.actorUniqueID + " already in this map " + floor + " " + act.actorUniqueID + " " + existingAct.actorUniqueID);
                        return false;
                    }
                }
            }
        }
        actorsInMap.Add(act);
        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mAct = act as Monster;
            if (act.actorfaction == Faction.ENEMY && mAct.bufferedFaction == Faction.ENEMY) // Exclude dungeon monsters
            {
                unfriendlyMonsterCount++;
                //Debug.Log("Current monster count floor " + floor + ": " + monsterCount + " added " + act.actorRefName);
            }
            monstersInMap.Add(act as Monster);
        }
        if (act.GetActorType() == ActorTypes.HERO || act.GetActorType() == ActorTypes.MONSTER)
        {
            Fighter ft = act as Fighter;
            ft.visibleTilesArray = new bool[columns, rows];
        }
        if (act.GetActorType() == ActorTypes.STAIRS)
        {
            Stairs st = act as Stairs;
            if (!mapStairs.Contains(st))
            {
                mapStairs.Add(st);
            }
        }
        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = act as Destructible;
            if (dt.mapObjType == SpecialMapObject.FOUNTAIN)
            {
                fountainsInMap.Add(dt);
            }
        }
        act.SetActorMap(this);
        act.dungeonFloor = floor;
        //Debug.Log("Adding actor to map " + mapAreaID + " " + act.actorRefName + " " + floor);
        return true;
    }


    public bool AreAreasOverlapping(Area a1, Area a2, int extra)
    {
        if ((a2.origin.x > (a1.origin.x + a1.size.x + extra)) || (a2.origin.y > (a1.origin.y + a1.size.y + extra)) || (a1.origin.x > (a2.size.x + a2.origin.x + extra)) || (a1.origin.y > (a2.size.y + a2.origin.y + extra)))
        {
            return false;
        }

        return true;
    }

    protected void FillMapWithNothing(TileTypes ttype)
    {
        MapTileData mtd = new MapTileData();
        mtd.floor = floor;

        Room fillArea = null;

        fillArea = new Room();
        fillArea.InitializeLists();
        fillArea.origin = new Vector2(1, 1);
        mapRooms.Add(fillArea);
        fillArea.areaID = MapMasterScript.FILL_AREA_ID; // THIS IS NEW.
        AddAreaToDictionaryWithID(fillArea, MapMasterScript.FILL_AREA_ID);
        areaIDCounter++;

        for (int y = 0; y < rows; y++)
        {
            // Generate map tiles
            for (int x = 0; x < columns; x++)
            {
                mtd = new MapTileData();
                mtd.floor = floor;
                mtd.iPos = new Point(x, y);

                if (dungeonLevelData.layoutType == DungeonFloorTypes.RUINS)
                {
                    if (x <= (rows / 2))
                    {
                        mtd.tileVisualSet = TileSet.STONE;
                    }
                    else
                    {
                        mtd.tileVisualSet = TileSet.SLATE; // was Earth.
                    }
                }
                else
                {
                    mtd.tileVisualSet = dungeonLevelData.tileVisualSet;
                }

                if (y == 0 || y == rows - 1 || x == 0 || x == columns - 1)
                {
                    // Outer bounds - add walls
                    mtd.ChangeTileType(TileTypes.NOTHING, mgd);
                    //mtd.AddTag(LocationTags.MAPEDGE);                    
                }
                else
                {
                    // Not outer bounds, fill with whatever
                    if (ttype != TileTypes.GROUND)
                    {
                        mtd.ChangeTileType(TileTypes.NOTHING, mgd);
                    }
                    else
                    {
                        mtd.ChangeTileType(TileTypes.GROUND, mgd);
                    }
                    mtd.areaType = AreaTypes.NONE;
                }
                mtd.pos.x = x;
                mtd.pos.y = y;
                mapArray[x, y] = mtd;

                SetTileAreaID(mtd, fillArea);
            }
        }
    }

    private void BuildAndPlaceSecretRooms()
    {
        int numSecretRooms = dungeonLevelData.maxSecretAreas;

        List<RoomTemplate> possibleSecretRooms = new List<RoomTemplate>();
        foreach (RoomTemplate rt in GameMasterScript.masterDungeonRoomlist.Values)
        {
            if ((rt.possibleLayouts[(int)dungeonLevelData.layoutType]) && (rt.specialArea))
            {
                possibleSecretRooms.Add(rt);
            }
        }

        if (possibleSecretRooms.Count == 0)
        {
            Debug.Log("No possible secret rooms for " + floor + " type " + dungeonLevelData.layoutType);
            return;
        }

        MapTileData checkRTile = null;
        MapTileData buildTile = null;
        MapTileData connectTile = null;

        int minX = 0;
        int minY = 0;
        int maxX = 0;
        int maxY = 0;
        Vector2 checkPos = Vector2.zero;
        Room connectRoom = null;
        Room cRoom = null;
        MapTileData mtd = null;
        Room dRoom = null;
        List<Room> usedRooms = new List<Room>();
        List<RoomTemplate> usedTemplates = new List<RoomTemplate>();

        for (int n = 0; n < numSecretRooms; n++)
        {
            buildTile = null;
            RoomTemplate tryRT = possibleSecretRooms[UnityEngine.Random.Range(0, possibleSecretRooms.Count)];

            int tries = 0;
            while (usedTemplates.Contains(tryRT))
            {
                tryRT = possibleSecretRooms[UnityEngine.Random.Range(0, possibleSecretRooms.Count)];
                tries++;
                if (tries > 100) { break; }
            }

            Room roomToAdd = CreateRoomFromTemplate(0, 0, tryRT);
            roomToAdd.InitializeLists();
            //foreach (Room dRoom in mapRooms)
            for (int i = 0; i < mapRooms.Count; i++)
            {
                dRoom = mapRooms[i];
                if (usedRooms.Contains(dRoom)) continue;
                if (dRoom.areaID <= 0) continue; // This is fill area or null area.

                connectRoom = dRoom;
                dRoom.edgeTiles.Shuffle();
                bool anyGround = false;
                bool alreadyOffset = false;
                //foreach (MapTileData mtd in dRoom.edgeTiles)

                if (dRoom.edgeTiles.Count == 0) continue;

                for (int d = 0; d < dRoom.edgeTiles.Count; d++)
                {
                    mtd = dRoom.edgeTiles[d];
                    //surrounding.Clear();
                    //surrounding = GetTilesAroundPoint(mtd.pos,1);
                    CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);

                    for (int t = 0; t < CustomAlgorithms.numTilesInBuffer; t++)
                    {
                        if (CustomAlgorithms.tileBuffer[t].tileType == TileTypes.GROUND)
                        {
                            anyGround = true;
                            break;
                        }
                    }
                    if (!anyGround) continue;

                    if (mtd.pos.x == dRoom.origin.x)
                    {
                        // Along left side.

                        // Say the room is at 10,15. New room is 6. MinX would be 10-6-1 = 5. Max X would be 10-1.

                        minX = (int)mtd.pos.x - (int)roomToAdd.size.x; // was -1
                        maxX = (int)mtd.pos.x - 1; // was -1
                        alreadyOffset = true;
                    }
                    else if (mtd.pos.x < (dRoom.origin.x + dRoom.size.x - 1))
                    {
                        minX = (int)mtd.pos.x;
                        maxX = (int)mtd.pos.x + (int)roomToAdd.size.x;
                    }
                    else
                    {
                        minX = (int)mtd.pos.x + 1; // + 1;
                        maxX = (int)mtd.pos.x + (int)roomToAdd.size.x; // + 1;
                        alreadyOffset = true;
                    }

                    if (alreadyOffset)
                    {
                        minY = (int)mtd.pos.y - 1;
                        maxY = (int)mtd.pos.y + (int)roomToAdd.size.y - 2;
                    }
                    else
                    {
                        if (mtd.pos.y == dRoom.origin.y)
                        {
                            // South wall
                            minY = (int)mtd.pos.y - (int)roomToAdd.size.y; // - 1; // For example, 15 - 6 - 1 = 8
                            maxY = (int)mtd.pos.y - 1;// - 1; // Check up to 14
                        }
                        else if (mtd.pos.y < (dRoom.origin.y + dRoom.size.y - 1))
                        {
                            minY = (int)mtd.pos.y;
                            maxY = (int)mtd.pos.y + (int)roomToAdd.size.y;
                        }
                        else
                        {
                            minY = (int)mtd.pos.y + 1; // + 1
                            maxY = (int)mtd.pos.y + (int)roomToAdd.size.y; // + 1
                        }
                    }

                    bool valid = true;

                    for (int x = minX; x < maxX; x++)
                    {
                        if (!valid) break;
                        for (int y = minY; y < maxY; y++)
                        {
                            checkPos.x = x;
                            checkPos.y = y;
                            if (!InBounds(checkPos))
                            {
                                valid = false;
                                break;
                            }
                            checkRTile = mapArray[x, y];
                            //if ((checkRTile.roomID != -1) && (checkRTile.roomID != dRoom.areaID))
                            if (CheckMTDArea(checkRTile) > 0)
                            {
                                valid = false;
                                break;
                            }
                            if (checkRTile.CheckTag(LocationTags.CORRIDOR))
                            {
                                valid = false;
                                break;
                            }
                        }
                    }
                    if (valid)
                    {
                        buildTile = mapArray[minX, minY];
                        connectTile = mtd;
                        //Debug.Log("Verified " + mtd + " has at least 1 adjacent ground tile.");
                        cRoom = dRoom;
                        usedRooms.Add(dRoom);
                        break;
                    }
                }
                if (buildTile != null)
                {
                    break;
                }
            }
            if (buildTile != null)
            {
                //buildTile.AddTag(LocationTags.SECRETENTRANCE);
                //connectTile.AddTag(LocationTags.SECRETENTRANCE);
                roomToAdd.origin.x = buildTile.pos.x;
                roomToAdd.origin.y = buildTile.pos.y;
                //Debug.Log("Building secret room " + roomToAdd.template.refName + " on floor " + floor + " build tile " + buildTile.pos + " connect tile " + connectTile.pos + " " + roomToAdd.areaID + " size " + roomToAdd.size + " Connect room origin " + connectRoom.origin + " size " + connectRoom.size);
                BuildRoomTiles(roomToAdd);

                if (roomToAdd.edgeTiles.Count == 0)
                {
                    Debug.Log("No edge tiles in room to add on floor " + floor);

                    if (roomToAdd.template != null)
                    {
                        Debug.Log(roomToAdd.template.refName);
                    }
                }

                for (int m = 0; m < roomToAdd.internalTiles.Count; m++)
                {

                    //SetTileAreaID(roomToAdd.internalTiles[m], roomToAdd);

                }
                AddAreaToDictionary(roomToAdd);

                // New code to make sure secret areas are discoverable
                foreach (MapTileData secretMTD in roomToAdd.internalTiles)
                {
                    SetTileAreaID(secretMTD, roomToAdd);
                }
                foreach (MapTileData secretMTD in roomToAdd.edgeTiles)
                {
                    SetTileAreaID(secretMTD, roomToAdd);
                }

                //Debug.Log("Secret area floor " + floor + " areaID is " + roomToAdd.areaID);
                MapTileData bestTile1 = null;
                MapTileData bestTile2 = null;

                float shortest = 9999f;
                float dist = 0f;
                for (int m = 0; m < roomToAdd.edgeTiles.Count; m++)
                {
                    mtd = roomToAdd.edgeTiles[m];

                    //SetTileAreaID(mtd, roomToAdd);

                    for (int i = 0; i < connectRoom.edgeTiles.Count; i++)
                    {
                        connectTile = connectRoom.edgeTiles[i];
                        if (connectTile.CheckTag(LocationTags.CORNER)) continue;
                        //dist = Vector2.Distance(connectTile.pos, mtd.pos);
                        dist = (connectTile.pos - mtd.pos).sqrMagnitude;
                        if ((dist < shortest) && (!mtd.CheckTag(LocationTags.CORNER)))
                        {
                            shortest = dist;
                            bestTile1 = connectTile;
                            bestTile2 = mtd;
                        }
                    }
                }

                try { bestTile1.ChangeTileType(TileTypes.GROUND, mgd); bestTile2.ChangeTileType(TileTypes.GROUND, mgd); }
                catch (Exception e)
                {
                    Debug.Log("While placing secret rooms, besttile 1 or 2 is null on floor " + floor + " " + dungeonLevelData.layoutType);
                    Debug.Log(e);
                    return;
                }


                CreateDestructibleInTile(bestTile1, "obj_pathrock");
                CreateDestructibleInTile(bestTile2, "obj_pathrock");
                bestTile1.AddTag(LocationTags.SECRETENTRANCE);
                bestTile2.AddTag(LocationTags.SECRETENTRANCE);
                bestTile1.RemoveTag(LocationTags.SECRET);
                bestTile2.RemoveTag(LocationTags.SECRET);

                foreach (MapTileData mtdCheck in roomToAdd.internalTiles)
                {
                    if (mtdCheck.CheckTag(LocationTags.WATER))
                    {
                        //mtdCheck.ChangeTileType(TileTypes.GROUND, mgd);
                        CreateDestructibleInTile(mtdCheck, "obj_rivertile");
                    }
                    if (mtdCheck.CheckTag(LocationTags.MUD))
                    {
                        //mtdCheck.ChangeTileType(TileTypes.GROUND, mgd);
                        CreateDestructibleInTile(mtdCheck, "obj_mudtile");
                    }
                    if (mtdCheck.CheckTag(LocationTags.LAVA))
                    {
                        //mtdCheck.ChangeTileType(TileTypes.GROUND, mgd);
                        CreateDestructibleInTile(mtdCheck, "obj_lavatile");
                    }
                    if (mtdCheck.CheckTag(LocationTags.ELECTRIC))
                    {
                        CreateDestructibleInTile(mtdCheck, "obj_electile");
                    }
                    if (mtdCheck.CheckTag(LocationTags.LASER))
                    {
                        CreateDestructibleInTile(mtdCheck, "obj_phasmashieldtile");
                    }
                }

                //Debug.Log("Connections at " + bestTile1.pos + " " + bestTile2.pos);

                roomToAdd.center.x = roomToAdd.origin.x + (roomToAdd.size.x / 2f);
                roomToAdd.center.y = roomToAdd.origin.x + (roomToAdd.size.y / 2f);

                //AddAreaToDictionary(roomToAdd);
                mapRooms.Add(roomToAdd);
                usedTemplates.Add(roomToAdd.template);
            }
        }
    }

    public void AddAreaToDictionary(Area ar)
    {
        ar.areaID = areaIDCounter;
        if (!areaDictionary.ContainsKey(ar.areaID))
        {
            areaDictionary.Add(ar.areaID, ar);
            areaIDCounter++;
        }
        else
        {
            //Debug.Log("Area dictionary floor " + floor + " already has an area with ID " + ar.areaID);
        }
    }

    public void AddAreaToDictionaryWithID(Area ar, int ID)
    {
        ar.areaID = ID;
        if (!areaDictionary.ContainsKey(ID))
        {
            areaDictionary.Add(ID, ar);
            //Debug.Log("Added area to dictionary, specifically " + ar.areaID + " on floor " + floor);
        }
        else
        {
            //Debug.Log("Area dictionary floor " + floor + " already has a specific area with ID " + ar.areaID);
        }
    }

    public void RelocateMonstersFromStairsDuringGameplay()
    {
        List<Actor> toRelocate = new List<Actor>();
        foreach (Stairs st in mapStairs)
        {
            MapTileData stairsTile = GetTile(st.GetPos());
            toRelocate.Clear();
            foreach (Actor act in stairsTile.GetAllActors())
            {
                if (act.GetActorType() == ActorTypes.MONSTER && act.actorfaction != Faction.PLAYER)
                {
                    // Welp, move it
                    toRelocate.Add(act);
                }
            }
            foreach(Actor act in toRelocate)
            {
                MapTileData newTile = GetRandomEmptyTile(st.GetPos(), 2, true, anyNonCollidable: true, preferLOS: true, avoidTilesWithPowerups: false, excludeCenterTile: true);
                Vector2 oldPos = act.GetPos();
                MoveActor(act.GetPos(), newTile.pos, act);
                if (MapMasterScript.activeMap == this && act.myMovable != null)
                {
                    act.myMovable.AnimateSetPosition(newTile.pos, 0.01f, false, 0f, 0f, MovementTypes.LERP);
                }
                // Double check for duplicates
                RemoveActorFromLocation(oldPos, act);
            }
        }
    }

    public void RelocateMonstersFromStairs(bool processOnlyChampions = false)
    {
        if (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)
        {
            // Go through each monster in the map, selecting ONLY champions
            foreach (Monster m in monstersInMap)
            {
                if (!m.isChampion) continue;
                if (m.actorfaction != Faction.ENEMY) continue;
                foreach (Stairs checkStairs in mapStairs)
                {
                    // Now search for every stairs UP, because UP stairs are where we would first arrive
                    // Remember: in the code, "UP" stairs are "Back to previous floor"
                    if (!checkStairs.stairsUp) continue;
                    int attempts = 0;
                    MapTileData oldMonsterPosition = GetTile(m.GetPos());
                    MapTileData newMonsterPosition = GetTile(m.GetPos()); 
                    bool relocated = false;

                    // If this champion monster is within 5 tiles of the stairs, try to relocate it.
                    while (MapMasterScript.GetGridDistance(oldMonsterPosition.pos, checkStairs.GetPos()) <= 5)
                    {
                        attempts++;
                        newMonsterPosition = GetRandomEmptyTileForMapGen();
                        relocated = true;
                        if (attempts > 5)
                        {
                            break;
                        }
                    }
                    if (relocated)
                    {
                        // If we found a valid tile, remove the monster from their old position
                        // And add them to their new position
#if UNITY_EDITOR
                        //Debug.Log("Relocated champion from " + oldMonsterPosition.pos + " to " + newMonsterPosition.pos + " because they were too close to " + checkStairs.GetPos());
#endif
                        RemoveActorFromLocation(oldMonsterPosition.pos, m);
                        AddActorToLocation(newMonsterPosition.pos, m);
                        m.SetPos(newMonsterPosition.pos);
                        m.SetCurPos(newMonsterPosition.pos);
                        break; // no need to keep iterating through stairs for this monster. we moved it already.
                    }
                }
            }
        }

        if (processOnlyChampions) return;

        // Now we need to move *any* monsters that are ON the stairs.
        foreach (Stairs st in mapStairs)
        {
            MapTileData stairMTD = GetTile(st.GetPos());
            List<Actor> actorsToMove = new List<Actor>();
            Actor[] actorsInStairTile = new Actor[stairMTD.GetAllActors().Count];
            stairMTD.GetAllActors().CopyTo(actorsInStairTile);

            // Iterate through each actor in the stair's tile
            for (int i = 0; i < actorsInStairTile.Length; i++)
            {
                Actor act = actorsInStairTile[i];

                // If it's a monster...
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster monmon = act as Monster;

                    // Make a list of all tiles within 2 squares of the stair tile
                    pool_tileList = GetListOfTilesAroundPoint(stairMTD.pos, 2);
                    if (pool_tileList.Count > 0)
                    {
                        // Shuffle them up and iterate through these tiles until we find a clear, non-dangerous, non-collidable tile
                        pool_tileList.Shuffle();
                        foreach (MapTileData mtd in pool_tileList)
                        {
                            if (mtd.tileType == TileTypes.GROUND && !mtd.IsDangerous(monmon) && !mtd.IsUnbreakableCollidable(monmon))
                            {
#if UNITY_EDITOR
                                Debug.Log("Moved " + act.GetPos() + " to " + mtd.pos + " on floor " + floor);
#endif
                                // Once we find one, move this monster there.

                                actorsToMove.Add(act);
                                AddActorToLocation(mtd.pos, act);
                                act.SetPos(mtd.pos);
                                act.SetSpawnPos(mtd.pos);
                                break;
                            }
                        }
                    }
                }
            }
            foreach (Actor act in actorsToMove)
            {
                RemoveActorFromLocation(stairMTD.pos, act);
            }
        }
    }

    private void ShuffaluffRoomEdgeTiles()
    {
        for (int roomIdx = 0; roomIdx < mapRooms.Count; roomIdx++)
        {
            Room r = mapRooms[roomIdx];
            if (r.edgeTiles != null)
            {
                r.edgeTiles.Shuffle();
            }
        }
    }

    private bool BuildAndPlaceRoomsStandardNoHalls()
    {
        int roomSizeX = 0;
        int roomSizeY = 0;
        int originX = 0;
        int originY = 0;

        Room roomResult = null;

        int failSafeX = 0;

        if (dungeonLevelData.priorityTemplates.Count == 0)
        {
            priorityRoomsBuilt = true;
        }

        List<Area> reachable = new List<Area>(mgd.maxRooms);
        Room connectRoom = null;
        MapTileData connectMtd = null;
        MapTileData chkMtd = null;

        ShuffaluffRoomEdgeTiles();

        for (int n = 0; n < mgd.maxRooms; n++)
        {
            failSafeX++;
            if (failSafeX > mgd.maxRooms)
            {
                Debug.Log("Failsafe 0 break");
                break;
            }
            // First, generate rooms.
            int attempts = 0;
            bool roomSuccess = false;

            int failSafe = 0;

            int localMinX;
            int localMinY;
            int localMaxX;
            int localMaxY;

            connectRoom = null;
            connectMtd = null;
            chkMtd = null;
            Vector2 checkpos = Vector2.zero;
            Directions buildOut = Directions.NORTH;

            //Shuffle up here to save one million shuffles later
            ShuffaluffRoomEdgeTiles();

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    mapArray[x, y].checkedForRoomGen = false;
                }
            }

            while ((attempts < mgd.maxRoomAttempts) && (!roomSuccess))
            {
                failSafe++;
                if (failSafe > MapMasterScript.MAX_ROOM_ATTEMPTS)
                {
                    //Debug.Log("Failsafe 1 break placing rooms, floor " + floor + " " + mapRooms.Count + " " + mgd.minRooms + " " + mgd.chanceToUseRoomTemplate[(int)dungeonLevelData.layoutType]);
                    return false;
                }
                if (mapRooms.Count < mgd.minRooms)
                {
                    // Must build the minimum number of rooms.
                    attempts = 0;
                }
                roomSizeX = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);
                roomSizeY = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);

                if ((UnityEngine.Random.Range(0, 1f) <= mgd.chanceToUseRoomTemplate[(int)dungeonLevelData.layoutType]) && (dungeonLevelData.layoutType != DungeonFloorTypes.FUTURENOHALLS))
                {
                    roomSizeY = roomSizeX;
                }

                if (dungeonLevelData.layoutType == DungeonFloorTypes.HIVES)
                {
                    roomSizeY = roomSizeX;
                }

                if (roomSizeX > 9) roomSizeX = 9;
                if (roomSizeY > 9) roomSizeY = 9;

                localMinX = 1;
                localMinY = 1;
                localMaxX = columns - roomSizeX - 1;
                localMaxY = rows - roomSizeY - 1;

                bool success = false;
                if (n == 0)
                {
                    originX = UnityEngine.Random.Range(localMinX, localMaxX + 1); // +1 was not here before. Will it break?!
                    originY = UnityEngine.Random.Range(localMinY, localMaxY + 1);
                    success = true;
                }
                else
                {
                    chkMtd = null;
                    connectMtd = null;
                    // Connect to existing room
                    connectRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                    int buildDirIndex = -1;
                    int localattempts = 0;
                    while (connectRoom.edgeTiles.Count == 0)
                    {
                        localattempts++;
                        if (localattempts >= 500)
                        {
                            Debug.Log("No rooms with edge tiles?!");
                            return false;
                        }
                        connectRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                    }
                    int iRandoPoint = UnityEngine.Random.Range(0, connectRoom.edgeTiles.Count);
                    int iCount = 0;
                    for (int r = iRandoPoint; iCount < connectRoom.edgeTiles.Count; r = (r >= connectRoom.edgeTiles.Count - 1 ? 0 : r + 1))
                    {
                        iCount++;
                        connectMtd = connectRoom.edgeTiles[r];
                        if (connectMtd.CheckTag(LocationTags.CORNER))
                        {
                            continue;
                        }
                        if (connectMtd.checkedForRoomGen)
                        {
                            continue;
                        }
                        {
                            buildDirIndex = -1;
                            for (int x = 0; x < MapMasterScript.directions.Length; x++)
                            {
                                if (connectMtd.dirCheckedForRoomGen[x])
                                {
                                    continue;
                                }

                                checkpos.x = connectMtd.pos.x + MapMasterScript.directions[x].x;
                                checkpos.y = connectMtd.pos.y + MapMasterScript.directions[x].y;
                                if ((checkpos.x < 1) || (checkpos.y < 1) || (checkpos.x > columns - mgd.minRoomSize) || (checkpos.y > rows - mgd.minRoomSize) || (checkpos.x >= columns - 1) || (checkpos.y >= rows - 1))
                                {
                                    connectMtd.dirCheckedForRoomGen[x] = true;
                                    continue;
                                }

                                chkMtd = mapArray[(int)checkpos.x, (int)checkpos.y];

                                bool checkValid = false;
                                if (dungeonLevelData.layoutType != DungeonFloorTypes.MAZEROOMS)
                                {
                                    if (chkMtd.tileType == TileTypes.NOTHING)
                                    {
                                        checkValid = true;
                                    }
                                }
                                else
                                {
                                    if (chkMtd.CheckMapTag(MapGenerationTags.MAZEFILL))
                                    {
                                        checkValid = true;
                                    }
                                }

                                if (checkValid)
                                {
                                    // Build out in this direction.
                                    buildDirIndex = x;
                                    break;
                                }
                                else
                                {
                                    connectMtd.dirCheckedForRoomGen[x] = true;
                                }
                            }
                            if (buildDirIndex == -1)
                            {
                                connectMtd.checkedForRoomGen = true;
                                continue;
                            }
                            // Found an edge tile with nothing immediately "out"
                            buildOut = MapMasterScript.cardinalDirections[buildDirIndex];
                            switch (buildOut)
                            {
                                case Directions.NORTH:
                                    originX = (int)connectMtd.pos.x;
                                    originY = (int)(connectMtd.pos.y);
                                    break;
                                case Directions.EAST:
                                    originX = (int)connectMtd.pos.x;
                                    originY = (int)(connectMtd.pos.y);
                                    break;
                                case Directions.WEST:
                                    originX = (int)connectMtd.pos.x - roomSizeX;
                                    originY = (int)(connectMtd.pos.y);
                                    break;
                                case Directions.SOUTH:
                                    originX = (int)connectMtd.pos.x;
                                    originY = (int)(connectMtd.pos.y - roomSizeY);
                                    break;
                            }

                            if ((originX < localMinX) || (originX > localMaxX) || (originY < localMinY) || (originY > localMaxY))
                            {
                                continue;
                            }
                            success = true;
                            break;
                        }

                    }
                }

                if (!success)
                {
                    continue;
                }

                // Have our connection MTD and origin point, so try building the room.

                RoomCreationResult rcr = FailedRoomCreation;

                if (dungeonLevelData.layoutType != DungeonFloorTypes.FUTURENOHALLS)
                {
                    rcr = CreateRoom(originX, originY, roomSizeX, roomSizeY, mapAreaID, true);
                }
                else
                {
                    rcr = CreateRoom(originX, originY, roomSizeX, roomSizeY, mapAreaID, false);
                }



                if (rcr.success)
                {
                    roomResult = rcr.roomCreated;
                    bool overlapping = false;

                    for (int g = 0; g < mapRooms.Count; g++)
                    {
                        if (mapRooms[g] == connectRoom)
                        {
                            continue;
                        }
                        overlapping = AreAreasOverlapping(mapRooms[g], roomResult, 0);
                        if (overlapping)
                        {
                            if (dungeonLevelData.layoutType == DungeonFloorTypes.FUTURENOHALLS)
                            {
                                //BuildRoomTiles(roomResult);
                                bool valid = true;
                                // Now make sure that the overlapped tiles are actually open.
                                for (int x = (int)roomResult.origin.x; x <= (roomResult.origin.x + roomResult.size.x); x++)
                                {
                                    if (!valid) break;
                                    for (int y = (int)roomResult.origin.y; y <= (roomResult.origin.y + roomResult.size.y); y++)
                                    {
                                        if (mapArray[x, y].CheckMapTag(MapGenerationTags.NOCONNECTION))
                                        {
                                            valid = false;
                                            break;
                                        }
                                    }
                                }
                                if (!valid)
                                {
                                    overlapping = true;
                                    for (int i = 0; i < roomResult.edgeTiles.Count; i++)
                                    {
                                        roomResult.edgeTiles[i].ResetToDefault();
                                    }
                                    for (int i = 0; i < roomResult.internalTiles.Count; i++)
                                    {
                                        roomResult.internalTiles[i].ResetToDefault();
                                    }
                                }
                            }
                            break;
                        }
                    }

                    if (overlapping == false)
                    {
                        // Room success!
                        AddAreaToDictionary(roomResult);
                        mapRooms.Add(roomResult);
                        BuildRoomTiles(roomResult);
                        roomSuccess = true;
                        if (n == 0)
                        {
                            //Debug.Log("Built " + roomResult.areaID + " " + roomResult.origin + " " + roomResult.size);
                        }
                        if (n > 0)
                        {
                            roomResult.reachable = true;
                            connectRoom.reachable = true;
                            roomResult.connections.Add(connectRoom);
                            connectRoom.connections.Add(roomResult);
                            if (!reachable.Contains(roomResult))
                            {
                                reachable.Add(roomResult);
                            }
                            if (!reachable.Contains(connectRoom))
                            {
                                reachable.Add(connectRoom);
                            }
                            chkMtd.ChangeTileType(TileTypes.GROUND, mgd);
                            chkMtd.AddTag(LocationTags.CORRIDORENTRANCE);
                            connectMtd.ChangeTileType(TileTypes.GROUND, mgd);
                            connectMtd.AddTag(LocationTags.CORRIDORENTRANCE);
                            //Debug.Log("Floor " + floor + " Built rm " + roomResult.areaID + " " + roomResult.origin + " " + roomResult.size + " " + buildOut + " relative " + connectRoom.areaID + " via points " + chkMtd.pos + " " + connectMtd.pos);
                            if (roomResult.template != null)
                            {
                                //Debug.Log("Previously built room is " + roomResult.template.refName);
                            }
                        }

                        if (!priorityRoomsBuilt && roomResult.template != null)
                        {
                            if (dungeonLevelData.priorityTemplates.Contains(roomResult.template))
                            {
                                numPriorityRoomsBuilt++;
                                if (numPriorityRoomsBuilt >= dungeonLevelData.priorityMinimum)
                                {
                                    priorityRoomsBuilt = true;
                                }
                            }
                        }

                        unusedArea -= (roomResult.size.x * roomResult.size.y);
                    }
                }

                attempts++;
            }
            // Finish generating rooms;
        }
        List<Room> remover = new List<Room>();
        for (int i = 0; i < mapRooms.Count; i++)
        {
            if (mapRooms[i].internalTiles.Count == 0)
            {
                remover.Add(mapRooms[i]);
            }
            foreach (Room area in mapRooms)
            {
                area.connections.Remove(mapRooms[i]);
            }
            if ((mapRooms[i].template != null) && (dungeonLevelData.layoutType == DungeonFloorTypes.FUTURENOHALLS))
            {
                Debug.Log(mapRooms[i].template.refName + " " + floor);
            }
        }
        foreach (Room rem in remover)
        {
            mapRooms.Remove(rem);
            //Debug.Log("Removing ID " + rem.areaID);
        }
        return true;
    }

    private bool BuildAndPlaceRoomsFutureNoHalls()
    {
        int roomSizeX = 0;
        int roomSizeY = 0;
        int originX = 0;
        int originY = 0;

        Room roomResult = null;

        int failSafeX = 0;

        if (dungeonLevelData.priorityTemplates.Count == 0)
        {
            priorityRoomsBuilt = true;
        }

        //List<MapTileData> poolEdgeTiles = new List<MapTileData>(60);
        List<Area> reachable = new List<Area>(mgd.maxRooms);
        List<MapTileData> surrounding = new List<MapTileData>();
        Room connectRoom = null;
        MapTileData connectMtd = null;
        MapTileData chkMtd = null;

        for (int n = 0; n < mgd.maxRooms; n++)
        {
            failSafeX++;
            if (failSafeX > mgd.maxRooms)
            {
                Debug.Log("Failsafe 0 break");
                break;
            }
            // First, generate rooms.
            int attempts = 0;
            bool roomSuccess = false;

            int failSafe = 0;

            int localMinX;
            int localMinY;
            int localMaxX;
            int localMaxY;

            Vector2 checkpos = Vector2.zero;
            Directions buildOut = Directions.NORTH;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    mapArray[x, y].checkedForRoomGen = false;
                }
            }

            //shuffle all the edge tiles in all the rooms, no need to shuffle every loop
            ShuffaluffRoomEdgeTiles();

            while ((attempts < mgd.maxRoomAttempts) && (!roomSuccess))
            {
                failSafe++;
                if (failSafe > MapMasterScript.MAX_ROOM_ATTEMPTS)
                {
                    //Debug.Log("Failsafe 1 break placing rooms, floor " + floor + " " + mapRooms.Count + " " + mgd.minRooms + " " + mgd.chanceToUseRoomTemplate[(int)dungeonLevelData.layoutType]);
                    return false;
                }
                if (mapRooms.Count < mgd.minRooms)
                {
                    // Must build the minimum number of rooms.
                    attempts = 0;
                }
                roomSizeX = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);
                roomSizeY = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);

                if ((UnityEngine.Random.Range(0, 1f) <= mgd.chanceToUseRoomTemplate[(int)dungeonLevelData.layoutType]) && (dungeonLevelData.layoutType != DungeonFloorTypes.FUTURENOHALLS))
                {
                    roomSizeY = roomSizeX;
                }

                if (dungeonLevelData.layoutType == DungeonFloorTypes.HIVES)
                {
                    roomSizeY = roomSizeX;
                }

                if (roomSizeX > 9) roomSizeX = 9;
                if (roomSizeY > 9) roomSizeY = 9;

                localMinX = 1;
                localMinY = 1;
                localMaxX = columns - roomSizeX - 1;
                localMaxY = rows - roomSizeY - 1;

                bool success = false;
                if (n == 0)
                {
                    originX = UnityEngine.Random.Range(localMinX, localMaxX + 1); // +1 was not here before. Will it break?!
                    originY = UnityEngine.Random.Range(localMinY, localMaxY + 1);
                    success = true;
                }
                else
                {
                    chkMtd = null;
                    connectMtd = null;
                    // Connect to existing room
                    connectRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                    int buildDirIndex = -1;

                    //start at a random location in the list and loop around
                    int iRandoPoint = UnityEngine.Random.Range(0, connectRoom.edgeTiles.Count);
                    int iCount = 0;
                    for (int r = iRandoPoint; iCount < connectRoom.edgeTiles.Count; r = (r >= connectRoom.edgeTiles.Count - 1 ? 0 : r + 1))
                    {
                        iCount++;
                        connectMtd = connectRoom.edgeTiles[r];
                        if ((connectMtd.CheckTag(LocationTags.CORNER)) || (connectMtd.CheckMapTag(MapGenerationTags.NOCONNECTION)))
                        {
                            continue;
                        }
                        if (connectMtd.checkedForRoomGen)
                        {
                            continue;
                        }
                        {
                            buildDirIndex = -1;
                            for (int x = 0; x < MapMasterScript.directions.Length; x++)
                            {
                                if (connectMtd.dirCheckedForRoomGen[x])
                                {
                                    continue;
                                }

                                checkpos.x = connectMtd.pos.x + MapMasterScript.directions[x].x;
                                checkpos.y = connectMtd.pos.y + MapMasterScript.directions[x].y;
                                if ((checkpos.x < 1) || (checkpos.y < 1) || (checkpos.x > columns - mgd.minRoomSize) || (checkpos.y > rows - mgd.minRoomSize) || (checkpos.x >= columns - 1) || (checkpos.y >= rows - 1))
                                {
                                    connectMtd.dirCheckedForRoomGen[x] = true;
                                    continue;
                                }

                                chkMtd = mapArray[(int)checkpos.x, (int)checkpos.y];

                                if (chkMtd.tileType == TileTypes.NOTHING)
                                {
                                    // Build out in this direction.
                                    buildDirIndex = x;
                                    break;
                                }
                                else
                                {
                                    connectMtd.dirCheckedForRoomGen[x] = true;
                                }
                            }
                            if (buildDirIndex == -1)
                            {
                                connectMtd.checkedForRoomGen = true;
                                continue;
                            }
                            // Found an edge tile with nothing immediately "out"
                            buildOut = MapMasterScript.cardinalDirections[buildDirIndex];
                            switch (buildOut)
                            {
                                case Directions.NORTH:
                                    originX = (int)connectMtd.pos.x;
                                    originY = (int)(connectMtd.pos.y + 1);
                                    break;
                                case Directions.EAST:
                                    originX = (int)connectMtd.pos.x + 1;
                                    originY = (int)(connectMtd.pos.y);
                                    break;
                                case Directions.WEST:
                                    originX = (int)connectMtd.pos.x - roomSizeX;
                                    originY = (int)(connectMtd.pos.y);
                                    break;
                                case Directions.SOUTH:
                                    originX = (int)connectMtd.pos.x;
                                    originY = (int)(connectMtd.pos.y - roomSizeY);
                                    break;
                            }

                            if ((originX < localMinX) || (originX > localMaxX) || (originY < localMinY) || (originY > localMaxY))
                            {
                                //connectMtd.dirCheckedForRoomGen[buildDirIndex] = true;
                                continue;
                            }
                            success = true;
                            break;
                        }
                        /*
                        if (success)
                        {
                            break;
                        }
                        */
                    }
                }

                if (!success)
                {
                    continue;
                }

                // Have our connection MTD and origin point, so try building the room.                
                RoomCreationResult rcr = CreateRoom(originX, originY, roomSizeX, roomSizeY, mapAreaID, false);


                if (rcr.success)
                {
                    roomResult = rcr.roomCreated;
                    bool overlapping = false;

                    for (int i = 0; i < mapRooms.Count; i++)
                    {
                        if (mapRooms[i] == connectRoom)
                        {
                            continue;
                        }
                        overlapping = AreAreasOverlapping(mapRooms[i], roomResult, -1); // WAS 0, but we're not just checking the outer layer walls.
                        if (overlapping)
                        {
                            // Cannot collide with any existing rooms.
                            break;
                        }
                    }

                    if (overlapping == false)
                    {
                        // Room CAN be built, but we must check the edge connections.

                        if (n > 0)
                        {
                            //Debug.Log("Check " + connectMtd.pos + " with room origin built at " + roomResult.origin);
                            BuildRoomTiles(roomResult);
                            bool hasEdgeConnection = false;
                            Vector2 checkPos = Vector2.zero;

                            //surrounding = GetTilesAroundPoint(connectMtd.pos, 1);
                            CustomAlgorithms.GetTilesAroundPoint(connectMtd.pos, 1, this);
                            if (chkMtd.CheckMapTag(MapGenerationTags.NOCONNECTION))
                            {
                                ResetRoomTiles(roomResult);
                                attempts++;
                                continue;
                            }
                            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                            {
                                if ((roomResult.edgeTiles.Contains(CustomAlgorithms.tileBuffer[i])) && (!CustomAlgorithms.tileBuffer[i].CheckMapTag(MapGenerationTags.NOCONNECTION)))
                                {
                                    hasEdgeConnection = true;
                                    break;
                                }
                            }

                            if (!hasEdgeConnection)
                            {
                                ResetRoomTiles(roomResult);
                                attempts++;
                                continue;
                            }
                        }
                        else
                        {
                            BuildRoomTiles(roomResult);
                        }

                        AddAreaToDictionary(roomResult);
                        mapRooms.Add(roomResult);
                        BuildRoomTiles(roomResult);
                        roomSuccess = true;
                        if (n == 0)
                        {
                            //Debug.Log("Built " + roomResult.areaID + " " + roomResult.origin + " " + roomResult.size);
                        }
                        if (n > 0)
                        {
                            roomResult.reachable = true;
                            connectRoom.reachable = true;
                            roomResult.connections.Add(connectRoom);
                            connectRoom.connections.Add(roomResult);
                            if (!reachable.Contains(roomResult))
                            {
                                reachable.Add(roomResult);
                            }
                            if (!reachable.Contains(connectRoom))
                            {
                                reachable.Add(connectRoom);
                            }
                            chkMtd.ChangeTileType(TileTypes.GROUND, mgd);
                            chkMtd.AddTag(LocationTags.CORRIDORENTRANCE);

                            //List<MapTileData> surr = GetTilesAroundPoint(chkMtd.pos, 1);
                            CustomAlgorithms.GetTilesAroundPoint(chkMtd.pos, 1, this);
                            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                            {
                                CustomAlgorithms.tileBuffer[i].ChangeTileType(TileTypes.GROUND, mgd);
                                CustomAlgorithms.tileBuffer[i].RemoveMapTag(MapGenerationTags.EDGETILE);
                            }
                            CustomAlgorithms.GetTilesAroundPoint(connectMtd.pos, 1, this);
                            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                            {
                                CustomAlgorithms.tileBuffer[i].ChangeTileType(TileTypes.GROUND, mgd);
                                CustomAlgorithms.tileBuffer[i].RemoveMapTag(MapGenerationTags.EDGETILE);
                            }

                            connectMtd.ChangeTileType(TileTypes.GROUND, mgd);
                            connectMtd.AddTag(LocationTags.CORRIDORENTRANCE);
                            //Debug.Log("Floor " + floor + " Built rm " + roomResult.areaID + " " + roomResult.origin + " " + roomResult.size + " " + buildOut + " relative " + connectRoom.areaID + " via points " + chkMtd.pos + " " + connectMtd.pos);
                            if (roomResult.template != null)
                            {
                                //Debug.Log("Previously built room is " + roomResult.template.refName);
                            }
                        }

                        if ((!priorityRoomsBuilt) && (roomResult.template != null))
                        {
                            if (dungeonLevelData.priorityTemplates.Contains(roomResult.template))
                            {
                                numPriorityRoomsBuilt++;
                                if (numPriorityRoomsBuilt >= dungeonLevelData.priorityMinimum)
                                {
                                    priorityRoomsBuilt = true;
                                }
                            }
                        }

                        unusedArea -= (roomResult.size.x * roomResult.size.y);
                    }
                }

                attempts++;
            }
            // Finish generating rooms;
        }
        List<Room> remover = new List<Room>();
        for (int i = 0; i < mapRooms.Count; i++)
        {
            if (mapRooms[i].internalTiles.Count == 0)
            {
                remover.Add(mapRooms[i]);
            }
            for (int x = 0; x < mapRooms.Count; x++)
            {
                mapRooms[x].connections.Remove(mapRooms[i]);
            }
            /* if ((areaC.template != null) && (dungeonLevelData.layoutType == DungeonFloorTypes.FUTURENOHALLS))
            {
                Debug.Log(areaC.template.refName + " " + floor);
            } */
        }
        foreach (Room rem in remover)
        {
            mapRooms.Remove(rem);
            //Debug.Log("Removing ID " + rem.areaID);
        }
        return true;
    }

    void ResetRoomTiles(Area ar)
    {
        for (int i = 0; i < ar.internalTiles.Count; i++)
        {
            ar.internalTiles[i].ResetToDefault();
        }
        for (int i = 0; i < ar.edgeTiles.Count; i++)
        {
            ar.edgeTiles[i].ResetToDefault();
        }
    }

    private bool BuildAndPlaceRoomsRuins()
    {
        int roomSizeX = 0;
        int roomSizeY = 0;
        int originX = 0;
        int originY = 0;

        int extra = 1;


        Room roomResult = null;

        int failSafeX = 0;

        if (dungeonLevelData.priorityTemplates.Count == 0)
        {
            priorityRoomsBuilt = true;
        }

        /*possibleTiles = new List<Vector2>(columns * rows);
        Vector2 adder = Vector2.zero;
        for (int x = 1; x < columns - 2; x++)
        {
            for (int y = 1; y < rows - 2; y++)
            {
                adder.x = x;
                adder.y = y;
                possibleTiles.Add(new Vector2(x, y));
            }
        }*/

        for (int n = 0; n < mgd.maxRooms; n++)
        {
            //Debug.Log(n + " rooms built " + floor + " " + possibleTiles.Count);
            failSafeX++;
            if (failSafeX > mgd.maxRooms)
            {
                Debug.Log("Failsafe 0 break");
                break;
            }
            // First, generate rooms.
            int attempts = 0;
            bool roomSuccess = false;

            int failSafe = 0;

            int localMinX;
            int localMinY;
            int localMaxX;
            int localMaxY;

            Vector2 check = Vector2.zero;

            while ((attempts < mgd.maxRoomAttempts) && (!roomSuccess))
            {
                failSafe++;
                if (failSafe > MapMasterScript.MAX_ROOM_ATTEMPTS)
                {
                    //Debug.Log("Failsafe 1 break placing rooms. " + floor);
                    return false;
                }
                if (mapRooms.Count < mgd.minRooms)
                {
                    // Must build the minimum number of rooms.
                    attempts = 0;
                }
                roomSizeX = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);
                roomSizeY = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);
                localMinX = 1;
                localMinY = 1;
                localMaxX = columns - roomSizeX - 1; // was -1 before
                localMaxY = rows - roomSizeY - 1; // was -1 before
                originX = UnityEngine.Random.Range(localMinX, localMaxX);
                originY = UnityEngine.Random.Range(localMinY, localMaxY);


                // Say size is 5. Let's say minimum is 1, maximum is now 30-5-1 = 24
                // OriginX should be somewhere between 1 and 24
                // 24,25,26,27,28

                RoomCreationResult rcr = CreateRoom(originX, originY, roomSizeX, roomSizeY, mapAreaID, false);
                if (rcr.success)
                {
                    roomResult = rcr.roomCreated;
                    bool overlapping = false;

                    int numOverlaps = 0;
                    Room overlapCandidate = null;

                    for (int i = 0; i < mapRooms.Count; i++)
                    {
                        overlapping = AreAreasOverlapping(mapRooms[i], roomResult, extra);
                        if (overlapping)
                        {
                            numOverlaps++;
                            overlapCandidate = mapRooms[i];
                            //mapRooms[i].connections.Add(roomResult);
                            //roomResult.connections.Add(mapRooms[i]);
                            //break;
                        }
                        if (numOverlaps > 1)
                        {
                            overlapping = false;
                            break;
                        }
                    }

                    if (overlapping || n == 0)
                    {
                        if (overlapCandidate == null)
                        {
                            overlapCandidate = mapRooms[0];
                        }
                        overlapCandidate.connections.Add(roomResult);
                        roomResult.connections.Add(overlapCandidate);

                        // Room success!
                        AddAreaToDictionary(roomResult);
                        mapRooms.Add(roomResult);
                        BuildRoomTiles(roomResult);

                        roomSuccess = true;

                        if ((!priorityRoomsBuilt) && (roomResult.template != null))
                        {
                            if (dungeonLevelData.priorityTemplates.Contains(roomResult.template))
                            {
                                numPriorityRoomsBuilt++;
                                if (numPriorityRoomsBuilt >= dungeonLevelData.priorityMinimum)
                                {
                                    priorityRoomsBuilt = true;
                                }
                            }
                        }

                        unusedArea -= (roomResult.size.x * roomResult.size.y);
                    }
                }
                attempts++;
            }
            // Finish generating rooms;
        }

        // Do this later.
        /* List<Room> remover = new List<Room>();
        foreach (Room areaC in mapRooms)
        {
            if (areaC.internalTiles.Count == 0)
            {
                remover.Add(areaC);
            }
            else
            {
                if (areaC.template != null)
                {
                }
            }
            foreach (Room area in mapRooms)
            {
                area.connections.Remove(areaC);
            }
            foreach (Corridor cr in mapCorridors)
            {
                cr.connections.Remove(areaC);
            }
        }
        foreach (Room rem in remover)
        {
            mapRooms.Remove(rem);
        } */

        return true;

    }

    public bool BuildAndPlaceRoomsStandard()
    {
        int roomSizeX = 0;
        int roomSizeY = 0;
        int originX = 0;
        int originY = 0;

        int extra = 0;

        Room roomResult = null;

        int failSafeX = 0;

        if (dungeonLevelData.priorityTemplates.Count == 0)
        {
            priorityRoomsBuilt = true;
        }

        // Just pick stuff at random ok??????

        Vector2 adder = Vector2.zero;

        bool[,] invalidTiles = new bool[columns, rows];

        int failSafe = 0;
        int highestFailsafe = 0;

        int metaRoomMinSize = dungeonLevelData.GetMetaData("minroomsize");
        int metaRoomMaxSize = dungeonLevelData.GetMetaData("maxroomsize");
        if (metaRoomMaxSize > 0)
        {
            mgd.maxRoomSize = metaRoomMaxSize;
        }
        if (metaRoomMinSize > 0)
        {
            mgd.minRoomSize = metaRoomMinSize;
        }

        bool forcePriorityRoomsFirst = dungeonLevelData.GetMetaData("forcepriorityrooms") == 1;
        bool breakMaxRoomSize = dungeonLevelData.GetMetaData("breakmaxroomsize") == 1;
        bool allowDuplicatePriorityTemplateRooms = !(dungeonLevelData.GetMetaData("forceuniquepriorityrooms") == 1);
        int overlapOffsetValue = dungeonLevelData.GetMetaData("forceoverlapvalue");

        for (int n = 0; n < mgd.maxRooms; n++)
        {
            failSafeX++;
            if (failSafeX > mgd.maxRooms)
            {
                Debug.Log("Failsafe 0 break");
                break;
            }
            // First, generate rooms.
            int attempts = 0;
            bool roomSuccess = false;

            if (highestFailsafe < failSafe)
            {
                highestFailsafe = failSafe;
            }
            failSafe = 0;

            int localMaxX;
            int localMaxY;

            MapTileData checkTile = null;
            Vector2 check = Vector2.zero;

            while (attempts < mgd.maxRoomAttempts && !roomSuccess)
            {
                // Figure out room dimensions.

                failSafe++;
                if (failSafe > MapMasterScript.MAX_ROOM_ATTEMPTS)
                {
#if UNITY_EDITOR
                    //Debug.Log("Tried to make rooms over 1000 times for floor " + floor + " cur rooms " + mapRooms.Count + " attempts " + attempts + " min rooms " + mgd.minRooms);
#endif
                    return false;
                }
                if (mapRooms.Count < mgd.minRooms)
                {
                    attempts = 0;
                }
                roomSizeX = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);
                roomSizeY = UnityEngine.Random.Range(mgd.minRoomSize, mgd.maxRoomSize + 1);

                if (roomSizeX > 9 && !breakMaxRoomSize) roomSizeX = 9;
                if (roomSizeY > 9 && !breakMaxRoomSize) roomSizeY = 9;


                localMaxX = columns - roomSizeX - 1;
                localMaxY = rows - roomSizeY - 1;

                int failSafe2 = 0;

                bool valid = false;

                while (!valid && failSafe2 < 50)
                {

                    failSafe2++;

                    // Brute force tile selection.
                    originX = UnityEngine.Random.Range(1, columns - 2 - roomSizeX);
                    originY = UnityEngine.Random.Range(1, rows - 2 - roomSizeY);

                    int nAttempts = 0;
                    while (invalidTiles[originX, originY])
                    {
                        nAttempts++;
                        if (nAttempts >= 8000)
                        {
                            break;
                        }
                        originX = UnityEngine.Random.Range(1, columns - 2 - roomSizeX);
                        originY = UnityEngine.Random.Range(1, rows - 2 - roomSizeY);
                    }

                    if (nAttempts >= 8000)
                    {
                        failSafe2 = 50;
                    }

                    /* originX = (int)check.x;
                    originY = (int)check.y; */

                    if (originX >= localMaxX || originY >= localMaxY || originX > columns - 2 - mgd.minRoomSize || originY > rows - 2 - mgd.minRoomSize)
                    {
                        continue;
                    }
                    checkTile = mapArray[originX, originY];

                    bool validTile = true;

                    switch (dungeonLevelData.layoutType)
                    {
                        case DungeonFloorTypes.MAZEROOMS:
                            if (!checkTile.CheckMapTag(MapGenerationTags.MAZEFILL))
                            {
                                validTile = false;
                            }
                            break;
                        case DungeonFloorTypes.SPIRIT_DUNGEON:
                            if (checkTile.CheckMapTag(MapGenerationTags.FILLED))
                            {
                                validTile = false;
                            }
                            break;
                        default:
                            if (checkTile.tileType != TileTypes.NOTHING)
                            {
                                validTile = false;
                            }
                            break;
                    }

                    if (!validTile)
                    {
                        invalidTiles[originX, originY] = true;
                        continue;
                    }
                    valid = true;
                }

                // Tried 50 times to find a tile that fit the room. Nothing did, so we're moving on.
                if (failSafe2 >= 50)
                {
                    Debug.Log("No valids at all");
                    continue;
                }

                bool endHitsSomething = false;
                check = Vector2.zero;
                checkTile = mapArray[originX + roomSizeX, originY + roomSizeY];

                bool endCollision = false;

                switch (dungeonLevelData.layoutType)
                {
                    case DungeonFloorTypes.MAZEROOMS:
                        if (!checkTile.CheckMapTag(MapGenerationTags.MAZEFILL))
                        {
                            endCollision = true;
                        }
                        break;
                    case DungeonFloorTypes.SPIRIT_DUNGEON:
                        if (checkTile.CheckMapTag(MapGenerationTags.FILLED))
                        {
                            endCollision = true;
                        }
                        break;
                    default:
                        if (checkTile.tileType != TileTypes.NOTHING)
                        {
                            endCollision = true;
                        }
                        break;
                }

                if (endCollision)
                {
                    endHitsSomething = true;
                }

                failSafe2 = 0;
                while (endHitsSomething && failSafe2 <= 100)
                {
                    failSafe2++;
                    // Shrink the room a bit
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        roomSizeX--;
                    }
                    else
                    {
                        roomSizeY--;
                    }

                    // Room is now too small or out of bounds.
                    if (roomSizeX < mgd.minRoomSize || roomSizeY < mgd.minRoomSize || originX + roomSizeX <= 1 || originY + roomSizeY <= 1 || originX + roomSizeX > columns - 2 || originY + roomSizeY > rows - 2)
                    {
                        break;
                    }

                    checkTile = mapArray[originX + roomSizeX, originY + roomSizeY];

                    endCollision = true;

                    switch (dungeonLevelData.layoutType)
                    {
                        case DungeonFloorTypes.MAZEROOMS:
                            if (checkTile.CheckMapTag(MapGenerationTags.MAZEFILL))
                            {
                                endCollision = false;
                            }
                            break;
                        case DungeonFloorTypes.SPIRIT_DUNGEON:
                            if (!checkTile.CheckMapTag(MapGenerationTags.FILLED))
                            {
                                endCollision = false;
                            }
                            break;
                        default:
                            if (checkTile.tileType == TileTypes.NOTHING)
                            {
                                endCollision = false;
                            }
                            break;
                    }

                    if (!endCollision)
                    {
                        endHitsSomething = false;
                    }
                }
                if (failSafe2 >= 100)
                {
                    Debug.Log("Not a lot of good options");
                    continue;
                }
                RoomCreationResult rcr = CreateRoom(originX, originY, roomSizeX, roomSizeY, mapAreaID, false, 
                    forcePriorityRoomsFirst, true, allowDuplicatePriorityTemplateRooms, breakMaxRoomSize);
                if (rcr.success)
                {
                    roomResult = rcr.roomCreated;
                    bool overlapping = false;

                    int overlapExtra = overlapOffsetValue;

                    if (dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS)
                    {
                        overlapExtra = 1;
                        if (roomResult.origin.x < 2 || roomResult.origin.y < 2)
                        {
                            overlapping = true;
                        }
                    }

                    if (roomResult.origin.x + roomResult.size.x >= columns - 1 - overlapExtra || roomResult.origin.y + roomResult.size.y >= rows - 1 - overlapExtra)
                    {
                        overlapping = true;
                    }


                    if (!overlapping)
                    {
                        for (int i = 0; i < mapRooms.Count; i++)
                        {
                            if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVE)
                            {
                                extra = -1;
                            }
                            else if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVEROOMS || dungeonLevelData.layoutType == DungeonFloorTypes.MAZEROOMS)
                            {
                                extra = 1;
                            }
                            else if (dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS)
                            {
                                extra = 2;
                            }
                            else
                            {
                                extra = 0;
                            }

                            extra += overlapExtra;

                            overlapping = AreAreasOverlapping(mapRooms[i], roomResult, extra);

                            if (overlapping)
                            {
                                break;
                            }
                        }

                    }

                    if (overlapping == false)
                    {
                        // Room success!
                        AddAreaToDictionary(roomResult);
                        mapRooms.Add(roomResult);
                        BuildRoomTiles(roomResult);

                        for (int i = 0; i < roomResult.internalTiles.Count; i++)
                        {
                            invalidTiles[(int)roomResult.internalTiles[i].pos.x, (int)roomResult.internalTiles[i].pos.y] = true;
                        }
                        for (int i = 0; i < roomResult.edgeTiles.Count; i++)
                        {
                            invalidTiles[(int)roomResult.edgeTiles[i].pos.x, (int)roomResult.edgeTiles[i].pos.y] = true;
                        }

                        if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVEROOMS || dungeonLevelData.layoutType == DungeonFloorTypes.MAZEROOMS)
                        {
                            roomResult.edgeTiles.Shuffle();
                            int entranceCount = 0;
                            int groundCount = 0;
                            for (int v = 0; v < roomResult.edgeTiles.Count; v++)
                            {
                                if (roomResult.edgeTiles[v].tileType == TileTypes.GROUND)
                                {
                                    groundCount++;
                                }
                                if (roomResult.edgeTiles[v].CheckMapTag(MapGenerationTags.ENTRANCEPOSSIBLE))
                                {
                                    if (entranceCount > 1)
                                    {
                                        if (UnityEngine.Random.Range(0, 1f) <= 0.4f)
                                        {
                                            break;
                                        }
                                    }
                                    MapTileData mtd = roomResult.edgeTiles[v];
                                    mtd.ChangeTileType(TileTypes.GROUND, mgd);
                                    roomResult.edgeTiles.Remove(mtd);
                                    roomResult.internalTiles.Add(mtd);
                                    mtd.RemoveMapTag(MapGenerationTags.EDGETILE);
                                    mtd.AddTag(LocationTags.CORRIDORENTRANCE);
                                    entranceCount++;
                                }
                            }
                            if (groundCount == 0 && roomResult.edgeTiles.Count > 0)
                            {
                                roomResult.edgeTiles[UnityEngine.Random.Range(0, roomResult.edgeTiles.Count)].ChangeTileType(TileTypes.GROUND, mgd);
                            }
                        }
                        if (dungeonLevelData.layoutType == DungeonFloorTypes.MAZEROOMS)
                        {
                            foreach (MapTileData mtd in roomResult.edgeTiles)
                            {
                                mtd.RemoveMapTag(MapGenerationTags.MAZEFILL);
                            }
                            foreach (MapTileData mtd in roomResult.internalTiles)
                            {
                                mtd.RemoveMapTag(MapGenerationTags.MAZEFILL);
                            }
                        }


                        roomSuccess = true;

                        if (!priorityRoomsBuilt && roomResult.template != null)
                        {
                            if (dungeonLevelData.priorityTemplates.Contains(roomResult.template))
                            {
                                numPriorityRoomsBuilt++;
                                if (numPriorityRoomsBuilt >= dungeonLevelData.priorityMinimum)
                                {
                                    priorityRoomsBuilt = true;
                                }
                                if (!allowDuplicatePriorityTemplateRooms)
                                {
                                    dungeonLevelData.priorityTemplates.Remove(roomResult.template); // don't worry, we restore this later!
                                }                                
                            }
                            
                        }
                    }
                    if (!roomSuccess)
                    {
                        if (floor == 2)
                        {
                            //Debug.Log("Room at " + originX + "," + originY + " size " + roomSizeX + "," + roomSizeY + " Overlapped with one of " + mapRooms.Count);
                        }
                    }
                }
                attempts++;
            }
            // Finish generating rooms;
        }

            //Debug.LogError(mapRooms.Count + " " + priorityRoomsBuilt + " " + numPriorityRoomsBuilt + " " + floor + " DONE");
        //Debug.Log("Attempts " + floor + ": " + failSafe + " highest " + highestFailsafe);

        return true;
    }

    public bool BuildAndPlaceLakeRooms()
    {
        int roomSizeX = 0;
        int roomSizeY = 0;
        int originX = 0;
        int originY = 0;

        int extra = 0;

        Room roomResult = null;

        int failSafeX = 0;

        if (dungeonLevelData.priorityTemplates.Count == 0)
        {
            priorityRoomsBuilt = true;
        }

        //possibleTiles = new List<Vector2>(columns * rows);
        Vector2 adder = Vector2.zero;

        int localMinX = (int)(columns * 0.07f);
        int localMaxX = (int)(columns * 0.8f);
        int localMinY = (int)(rows * 0.07f);
        int localMaxY = (int)(rows * 0.8f);

        /* for (int x = localMinX; x < localMaxX + 1; x++)
        {
            for (int y = localMinY; y < localMaxY + 1; y++)
            {
                adder.x = x;
                adder.y = y;
                possibleTiles.Add(new Vector2(x, y));
            }
        } */
        bool[,] invalidTiles = new bool[columns, rows];

        List<RoomTemplate> lakeRoomTemplates = GameMasterScript.masterDungeonRoomsByLayout[(int)DungeonFloorTypes.LAKE];

        int highestFailsafe = 0;
        int failSafe = 0;

        //Debug.Log("Prepare to build lake rooms.");
        
        for (int n = 0; n < mgd.maxRooms; n++)
        {            
            failSafeX++;
            if (failSafeX > mgd.maxRooms)
            {
                Debug.Log("Failsafe 0 break");
                break;
            }
            int attempts = 0;
            bool roomSuccess = false;

            if (failSafe > highestFailsafe)
            {
                highestFailsafe = failSafe;
            }

            failSafe = 0;

            MapTileData checkTile = null;
            Vector2 check = Vector2.zero;

            //Debug.Log("n is " + n + " out of " + mgd.maxRooms + " rooms, failesafe is " + failSafeX);

            while (attempts < mgd.maxRoomAttempts && !roomSuccess)
            {
                failSafe++;
                if (failSafe > MapMasterScript.MAX_ROOM_ATTEMPTS)
                {
#if UNITY_EDITOR
                    //Debug.Log("Tried to make rooms over " + MapMasterScript.MAX_ROOM_ATTEMPTS + " times for floor " + floor + " cur rooms " + mapRooms.Count + " attempts " + attempts + " min rooms " + mgd.minRooms + " " + dungeonLevelData.layoutType);
#endif
                    return false;
                }
                if (mapRooms.Count < mgd.minRooms)
                {
                    attempts = 0;
                }

                RoomTemplate selectedTemplate = lakeRoomTemplates[UnityEngine.Random.Range(0, lakeRoomTemplates.Count)];

                if (!priorityRoomsBuilt)
                {
                    selectedTemplate = dungeonLevelData.priorityTemplates[UnityEngine.Random.Range(0, dungeonLevelData.priorityTemplates.Count)];
                }

                roomSizeX = selectedTemplate.numColumns;
                roomSizeY = selectedTemplate.numRows;

                int failSafe2 = 0;

                bool valid = false;

                while (!valid && failSafe2 < 50)
                {

                    failSafe2++;

                    originX = UnityEngine.Random.Range(1, columns - 2 - roomSizeX);
                    originY = UnityEngine.Random.Range(1, rows - 2 - roomSizeY);
                    int subTries = 0;
                    while (invalidTiles[originX, originY])
                    {
                        subTries++;
                        if (subTries > 2000) // really shouldn't take this long to find a valid tile
                        {
                            break;
                        }
                        originX = UnityEngine.Random.Range(1, columns - 2 - roomSizeX);
                        originY = UnityEngine.Random.Range(1, rows - 2 - roomSizeY);
                    }
                    if (subTries >= 2000)
                    {
                        failSafe2 = 50;
                        //Debug.Log("Failure at point 3.");
                        break;
                    }

                    checkTile = mapArray[originX, originY];

                    bool validTile = true;

                    // Allow ANY tile type as origin, right?

                    if (originX + roomSizeX >= columns - 1)
                    {
                        validTile = false;
                    }

                    if (originY + roomSizeY >= rows - 1)
                    {
                        validTile = false;
                    }

                    if (!validTile)
                    {
                        //possibleTiles.Remove(check);
                        invalidTiles[originX, originY] = true;
                        continue;
                    }
                    valid = true;
                }

                // Tried 50 times to find a tile that fit the room. Nothing did, so we're moving on.
                if (failSafe2 >= 50)
                {
                    //Debug.Log("No valids at all");
                    continue;
                }

                check = Vector2.zero;
                // TODO: Why is this happening?
                try { checkTile = mapArray[originX + roomSizeX, originY + roomSizeY]; }
                catch (Exception e)
                {
                    Debug.Log(originX + " " + roomSizeX + " " + originY + " " + roomSizeY + " " + columns + " " + rows);
                    Debug.Log(e);
                    continue;
                }

                // TODO: Collision conditional?

                failSafe2 = 0;

                roomResult = CreateRoomFromTemplate(originX, originY, selectedTemplate);
                if (roomResult != null)
                {
                    bool overlapping = false;

                    int overlapExtra = 0;

                    if (roomResult.origin.x + roomResult.size.x >= columns - 1 - overlapExtra || roomResult.origin.y + roomResult.size.y >= rows - 1 - overlapExtra)
                    {
                        overlapping = true;
                    }

                    if (!overlapping)
                    {
                        for (int i = 0; i < mapRooms.Count; i++)
                        {
                            extra = -2;
                            overlapping = AreAreasOverlapping(mapRooms[i], roomResult, extra);
                            if (overlapping)
                            {
                                break;
                            }
                        }

                    }

                    if (overlapping == false)
                    {
                        // Room success!
                        AddAreaToDictionary(roomResult);
                        mapRooms.Add(roomResult);
                        BuildRoomTiles(roomResult);


                        for (int i = 0; i < roomResult.internalTiles.Count; i++)
                        {
                            invalidTiles[(int)roomResult.internalTiles[i].pos.x, (int)roomResult.internalTiles[i].pos.y] = true;
                        }
                        for (int i = 0; i < roomResult.edgeTiles.Count; i++)
                        {
                            invalidTiles[(int)roomResult.edgeTiles[i].pos.x, (int)roomResult.edgeTiles[i].pos.y] = true;
                        }

                        roomSuccess = true;

                        if (!priorityRoomsBuilt && roomResult.template != null)
                        {
                            if (dungeonLevelData.priorityTemplates.Contains(roomResult.template))
                            {
                                numPriorityRoomsBuilt++;
                                if (numPriorityRoomsBuilt >= dungeonLevelData.priorityMinimum)
                                {
                                    priorityRoomsBuilt = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Failure conditional
                }
                attempts++;
            }
            // Finish generating rooms;
        }

        //Debug.Log("Floor " + floor + " attempts " + failSafe + " max " + highestFailsafe);

        return true;
    }

    public bool BuildAndConnectCorridorsStandard()
    {
        // Begin connecting rooms with corridors
        List<Area> reachableRooms = new List<Area>(20);
        List<Area> unreachableRooms = new List<Area>(20);
        List<MapTileData> possibleTiles = new List<MapTileData>();

        bool ruins = false;

        if (dungeonLevelData.layoutType == DungeonFloorTypes.RUINS)
        {
            ruins = true;
        }

        int deadEndsCreated = 0;

        if (mapRooms.Count > 1)
        {
            // Are there at least two rooms? Let's connect them with corridors!

            bool builtEssentialCorridors = false;
            int requirement = 100;

            int xFailSafe = 0;
            int localMax = mgd.maxCorridors;
            if (localMax == 0) localMax = 1;

            Area room1 = null;
            Area room2 = null;
            Area workRoom = null;

            foreach (Room rm in mapRooms)
            {
                if (rm.edgeTiles.Count == 0) //|| (rm.areaID <= 0))
                {
                    continue;
                }
                if (rm.reachable)
                {
                    reachableRooms.Add(rm);
                }
                else if (rm.areaID > 0)
                {
                    unreachableRooms.Add(rm);
                }
            }
            foreach (Corridor cr in mapCorridors)
            {
                if (cr.edgeTiles.Count == 0) // corridors can have areaID of less than 0
                {
                    continue;
                }
                if (cr.reachable)
                {
                    reachableRooms.Add(cr);
                }
                else if (cr.areaID > 0)
                {
                    unreachableRooms.Add(cr);
                }
            }

            //Debug.Log("Floor " + floor + " start: " + reachableRooms.Count + " " + unreachableRooms.Count + " " + mapRooms.Count + " Localmax: " + localMax);

            //int unreachableRoomCount = unreachableRooms.Count;

            for (int n = 0; n < localMax; n++)
            {
                //float timeAtLoopStart = Time.realtimeSinceStartup;
                xFailSafe++;
                bool allReachable = true;

                if (builtEssentialCorridors == false)
                {
                    if (unreachableRooms.Count > 0)
                    {
                        allReachable = false;
                        n = 0;
                    }
                }

                if (allReachable == true || dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS)
                {
                    builtEssentialCorridors = true;
                    //Debug.Log("All corridors built " + floor + " " + reachableRooms.Count + " " + unreachableRooms.Count);
                    requirement = mgd.maxCorridorLength;
                }
                else
                {
                    n--;
                    // Don't count essential corridors toward total.
                }

                if (xFailSafe == 20 && !builtEssentialCorridors)
                {
#if UNITY_EDITOR
                    //Debug.Log("Corridor creation failed " + builtEssentialCorridors + " " + floor + " " + reachableRooms.Count + " UR: " + unreachableRooms.Count);
#endif
                    return false;
                }

                int corAttempts = 0;
                bool success = false;

                int rFailSafe = 0;

                while (corAttempts <= mgd.maxCorridorAttempts && !success)
                {
                    //Debug.Log("Attempt " + corAttempts + " " + reachableRooms.Count + " " + unreachableRooms.Count);
                    room1 = mapRooms[0];
                    room2 = mapRooms[1];

                    //counter2++;

                    rFailSafe++;
                    if (rFailSafe == 200)
                    {
                        //Debug.Log("Failsafe 2 break on floor " + floor + " reachable: " + reachableRooms.Count + " unreach: " + unreachableRooms.Count);
                        return false;
                    }

                    workRoom = null;

                    #region Cruft
                    // Commented this out below 3/17 because it appears to be redundant?
                    /* for (int b = 0; b < mapRooms.Count; b++)
                    {
                        counter3++;
                        if (mapRooms[b].edgeTiles.Count > 0) //We should know this already.
                        {
                            workRoom = mapRooms[b];
                            break;
                        }
                    } 

                    if (workRoom == null)
                    {
                        Debug.Log("No rooms have ANY edge tiles?! " + floor);
                    } */
                    // Find all reachable rooms

                    // 3/17 commented below to try maintaining the list of reachable/unreachable better.

                    /* reachableRooms.Clear();
                    unreachableRooms.Clear();

                    for (int i = 0; i < mapRooms.Count; i++)
                    {
                        counter4++;
                        if ((mapRooms[i].edgeTiles.Count == 0) || (mapRooms[i].areaID <= 0)) // DOn't connect to fill.
                        {
                            continue;
                        }
                        if (mapRooms[i].reachable == true)
                        {
                            reachableRooms.Add(mapRooms[i]);
                        }
                        else
                        {
                            unreachableRooms.Add(mapRooms[i]);
                        }
                    } 

                    // Also add reachable CORRIDORS
                    foreach (Corridor cr in mapCorridors)
                    {
                        counter5++;
                        if ((cr.reachable) && (cr.edgeTiles.Count > 0))
                        {
                            reachableRooms.Add(cr);
                        }
                    } 

                    if (unreachableRooms.Count == 1)
                    {
                        //Debug.Log("Remaining: " + unreachableRooms[0].internalTiles.Count + " " + unreachableRooms[0].edgeTiles.Count + " " + unreachableRooms[0].connections.Count);
                    } */
                    #endregion

                    if (reachableRooms.Count == 0)
                    {
                        // No reachable rooms, so just pick any room at random.
                        workRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                        int failsafe5 = 0;
                        while (workRoom.edgeTiles.Count == 0 && failsafe5 < 500)
                        {
                            workRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                            failsafe5++;
                        }
                        if (failsafe5 == 500)
                        {
                            Debug.Log("BIG MAP GEN PROBLEM");
                        }
                    }
                    else if (unreachableRooms.Count > 0)
                    {
                        // There will be at least two reachable rooms. Pick the one that is closest to an unreachable room.
                        Area c1 = mapRooms[0];
                        Area c2 = mapRooms[1];
                        float sd = 60000f;
                        foreach (Area cr in reachableRooms)
                        {
                            //counter6++;
                            if (cr.edgeTiles.Count == 0) continue; // We should know this already
                            Vector2 center1 = cr.center;
                            foreach (Area ur in unreachableRooms)
                            {
                                //counter4++;
                                if (ur.edgeTiles.Count == 0) continue;
                                Vector2 center2 = ur.center;
                                float dist = Mathf.Max(Math.Abs(center2.x - center1.x), Math.Abs(center2.y - center1.y));

                                if (cr.connectionTries > 0)
                                {
                                    dist = dist + (((9 * cr.connectionTries) * dist) / 100);
                                }
                                if (dist < sd)
                                {
                                    c1 = cr;
                                    c2 = ur;
                                    sd = Mathf.Max(Math.Abs(center2.x - center1.x), Math.Abs(center2.y - center1.y));
                                }
                            }
                        }
                        room1 = c1;
                        room2 = c2;
                        workRoom = c1;
                    }
                    else
                    {
                        // All rooms are reachable. Pick the one with the fewest connections.
                        int lowestConnections = 999;
                        foreach (Area cr in reachableRooms)
                        {
                            if (cr.connections.Count <= lowestConnections && cr.edgeTiles.Count > 0)
                            {
                                workRoom = cr;
                                lowestConnections = cr.connections.Count;
                            }
                        }
                    }

                    // We have a starting room - workRoom                    
                    workRoom.connectionTries++;
                    Area connectRoom = mapRooms[1];

                    // Pick the list to use.
                    List<Area> possibleRooms = new List<Area>();

                    if (unreachableRooms.Count > 0)
                    {
                        possibleRooms = unreachableRooms;
                    }
                    else
                    {
                        possibleRooms = reachableRooms;
                    }

                    // Now, are there any unreachable rooms? If so, find the closet.

                    if (possibleRooms.Count > 0)
                    {
                        float shortest = 1000f;
                        Vector2 center1 = workRoom.center;
                        foreach (Area ur in possibleRooms)
                        {
                            //counter8++;
                            if (ur.edgeTiles.Count == 0) continue;
                            Vector2 center2 = ur.center;
                            float dist = Mathf.Max(Math.Abs(center2.x - center1.x), Math.Abs(center2.y - center1.y));
                            if ((dist < shortest) && (ur != workRoom))
                            {
                                connectRoom = ur;
                                shortest = Mathf.Max(Math.Abs(center2.x - center1.x), Math.Abs(center2.y - center1.y));
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No possible rooms.");
                        return false;
                    }

                    room1 = workRoom;
                    room2 = connectRoom;

                    if (room1 == room2)
                    {
                        Debug.Log("Problem, both rooms are the same.");
                        return false;
                    }

                    // DEBUG
                    /* if ((workRoom.edgeTiles.Count == 0) || (connectRoom.edgeTiles.Count == 0))
                        Debug.Log(mapRooms[0].areaID + " " + room1.areaID + " " + mapRooms[1].areaID + " " + room2.areaID); */
                    // END DEBUG

                    MapTileData bestTile1 = null;
                    MapTileData bestTile2 = null;

                    // Compare each edge tile of the room to each edge tile in the other room, not counting corners.
                    // Find the tiles with the shortest distance

                    Vector2 room1tilepos = new Vector2(0, 0);
                    Vector2 room2tilepos = new Vector2(0, 0);

                    float shortestDistance = 99999f;
                    // Find the room1 tile closest to room2.

                    if (ruins)
                    {
                        pool_connectingTiles = room1.internalTiles;
                    }
                    else
                    {
                        pool_connectingTiles = room1.edgeTiles;
                    }

                    foreach (MapTileData mt in pool_connectingTiles)
                    {
                        float dist = Mathf.Max(Math.Abs(mt.pos.x - room2.center.x), Math.Abs(mt.pos.y - room2.center.y));

                        if (mt.triedForCorridorCreation)
                        {
                            dist *= 1.1f;
                        }

                        if (mt.CheckTag(LocationTags.CORNER))
                        {
                            continue;
                        }

                        if (mt.tileType == TileTypes.WALL && ruins)
                        {
                            continue;
                        }

                        for (int r = 0; r < MapMasterScript.directions.Length; r++)
                        {
                            Vector2 checkPos = mt.pos + MapMasterScript.directions[r];
                            if ((checkPos.x <= 0) || (checkPos.y <= 0) || (checkPos.x >= columns - 1) || (checkPos.y >= rows - 1))
                            {
                                continue;
                            }
                            if ((mapArray[(int)checkPos.x, (int)checkPos.y].CheckTag(LocationTags.CORRIDOR)) || (mapArray[(int)checkPos.x, (int)checkPos.y].CheckTag(LocationTags.CORRIDORENTRANCE)) || (mapArray[(int)checkPos.x, (int)checkPos.y].CheckMapTag(MapGenerationTags.ESSENTIALCORRIDOR)))
                            {
                                // Prioritize it less by spiking up the distance. Was 2.2f, let's try less.
                                dist = dist * 1.5f;
                            }
                        }

                        if (dist < shortestDistance)
                        {
                            shortestDistance = dist;
                            bestTile1 = mt;
                        }
                    }

                    if (bestTile1 == null)
                    {
                        if (workRoom.edgeTiles.Count > 0)
                        {
                            bestTile1 = workRoom.edgeTiles[UnityEngine.Random.Range(0, workRoom.edgeTiles.Count)];
                        }
                        else
                        {
                            Debug.Log(reachableRooms.Count + " " + unreachableRooms.Count + " " + shortestDistance + " BT1 is null in creation " + workRoom.edgeTiles.Count + " " + connectRoom.edgeTiles.Count + " " + n + " " + corAttempts + " " + mapCorridors.Count + " " + workRoom.areaID + " " + connectRoom.areaID + " floor " + floor);
                            corAttempts++;
                            continue;
                        }

                    }

                    if (ruins)
                    {
                        pool_connectingTiles = room2.internalTiles;
                    }
                    else
                    {
                        pool_connectingTiles = room2.edgeTiles;
                    }

                    // Find the room2 tile closest to bestTile1
                    shortestDistance = 99999f;
                    foreach (MapTileData mt in pool_connectingTiles)
                    {
                        float dist = Mathf.Max(Math.Abs(mt.pos.x - bestTile1.pos.x), Math.Abs(mt.pos.y - bestTile1.pos.y));
                        if (mt.triedForCorridorCreation)
                        {
                            dist *= 1.1f;
                        }

                        if (mt.CheckTag(LocationTags.CORNER))
                        {
                            continue;
                        }
                        if (ruins)
                        {
                            if (mt.tileType == TileTypes.WALL) continue;
                        }


                        for (int r = 0; r < MapMasterScript.directions.Length; r++)
                        {
                            Vector2 checkPos = mt.pos + MapMasterScript.directions[r];
                            if ((checkPos.x <= 0) || (checkPos.y <= 0) || (checkPos.x >= columns - 1) || (checkPos.y >= rows - 1))
                            {
                                continue;
                            }
                            if ((mapArray[(int)checkPos.x, (int)checkPos.y].CheckTag(LocationTags.CORNER) || (mapArray[(int)checkPos.x, (int)checkPos.y].CheckTag(LocationTags.CORRIDOR)) || (mapArray[(int)checkPos.x, (int)checkPos.y].CheckTag(LocationTags.CORRIDORENTRANCE)) || (mapArray[(int)checkPos.x, (int)checkPos.y].CheckMapTag(MapGenerationTags.ESSENTIALCORRIDOR))))
                            {
                                // Prioritize it less by spiking up the distance. Was 2.2f, let's try less.
                                dist = dist * 1.35f;
                            }
                        }

                        if (dist < shortestDistance)
                        {
                            shortestDistance = dist;
                            bestTile2 = mt;
                        }

                    }

                    if (bestTile1 == null || bestTile2 == null)
                    {
                        continue;
                    }


                    // We have origination points. Turn those tiles into entrance tiles...

                    room1tilepos = bestTile1.pos;
                    room2tilepos = bestTile2.pos;
                    bestTile1.triedForCorridorCreation = true;
                    bestTile2.triedForCorridorCreation = true;

                    // Corridor Pathfinding

                    HashSet<MapTileData> openList = new HashSet<MapTileData>();
                    // Add adjacent tiles to list

                    MapTileData startTile = bestTile1;

                    //openList.Enqueue(startTile);
                    openList.Add(startTile);
                    startTile.pfState = MapTileData.openState;

                    MapTileData evalTile = startTile;
                    MapTileData finalTile = bestTile2;

                    FlushTilePathfindingData(evalTile.pos, finalTile.pos, true);
                    
                    if (MapMasterScript.GetGridDistance(startTile.pos, finalTile.pos) <= 1)
                    {
                        // The tiles are right next to one another.
                        room1.connections.Add(room2);
                        room2.connections.Add(room1);
                        room1.reachable = true;
                        room2.reachable = true;

                        // This block new 3/17/18 to track reachable/unreachable without constant iteration
                        if (!reachableRooms.Contains(room1)) reachableRooms.Add(room1);
                        if (!reachableRooms.Contains(room2)) reachableRooms.Add(room2);
                        unreachableRooms.Remove(room1);
                        unreachableRooms.Remove(room2);
                        //unreachableRoomCount -= 2;

                        success = true;

                        if (startTile.tileType != TileTypes.GROUND)
                        {
                            startTile.ChangeTileType(TileTypes.GROUND, mgd);
                            startTile.AddMapTag(MapGenerationTags.FILLED);
                            startTile.AddTag(LocationTags.CORRIDORENTRANCE);
                        }
                        if (finalTile.tileType != TileTypes.GROUND)
                        {
                            finalTile.ChangeTileType(TileTypes.GROUND, mgd);
                            finalTile.AddMapTag(MapGenerationTags.FILLED);
                            finalTile.AddTag(LocationTags.CORRIDORENTRANCE);
                        }
                        //mapAreaID++; Don't think this should be here?
                        continue;
                    }

                    int tries = 0;

                    while (openList.Count > 0 && tries < 250)
                    {
                        tries++;
                        if (tries >= 250)
                        {
                            //Debug.Log(floor + " pathfinding failure for corridors.");
                            break;
                        }
                        // Pathfind
                        
                        float lowestFscore = 9999f;

                        foreach (MapTileData tile in openList)
                        {
                            if (tile.f < lowestFscore)
                            {
                                evalTile = tile;
                                lowestFscore = tile.f;
                            }
                        }

                        openList.Remove(evalTile);
                        evalTile.pfState = MapTileData.closedState; // new

                        if (Equals(evalTile.pos, finalTile.pos))
                        {
                            // Found our path!
                            finalTile = evalTile;
                            break;
                        }

                        evalTile.pfState = MapTileData.closedState;

                        for (int i = 0; i < MapMasterScript.directions.Length; i++)
                        {
                            Vector2 newPos = evalTile.pos + MapMasterScript.directions[i];
                            if ((newPos.x >= 1) && (newPos.y >= 1) && (newPos.x < columns - 1) && (newPos.y < rows - 1))
                            {
                                MapTileData tile = mapArray[(int)newPos.x, (int)newPos.y];

                                if (Equals(tile.pos, finalTile.pos))
                                {
                                    tile.parent = evalTile;
                                    tile.f = -1;
                                    openList.Add(tile);
                                    tile.pfState = MapTileData.openState;
                                    break;
                                }

                                if (tile.pfState == MapTileData.closedState)
                                {
                                    continue;
                                }
                                if (tile.areaType == AreaTypes.ROOM && dungeonLevelData.layoutType != DungeonFloorTypes.RUINS) // With ruins, we can bore into other rooms.
                                {
                                    // This is an existing room feature, don't use it.
                                    tile.pfState = MapTileData.closedState;
                                    continue;
                                }
                                
                                float gScore = evalTile.g + 1;
                                if (tile.areaType == AreaTypes.CORRIDOR || tile.CheckTag(LocationTags.CORNER))
                                {
                                    // Building into existing corridor is less preferable.
                                    gScore *= 1.5f;
                                    if (builtEssentialCorridors)
                                    {
                                        gScore *= 1.5f; // Much less preferable;
                                    }
                                }

                                if (evalTile != startTile)
                                {
                                    // Try not to build in 'steps'
                                    if (tile.pos.x != evalTile.parent.pos.x && tile.pos.y != evalTile.parent.pos.y)
                                    {
                                        gScore *= 1.75f;
                                    }
                                }

                                if (tile.pfState == MapTileData.openState)
                                {
                                    // Already in open list?
                                    float localF = gScore + tile.GetHScore(finalTile);
                                    if (localF < tile.f)
                                    {
                                        // better path
                                        tile.g = gScore;
                                        tile.f = localF;
                                        tile.parent = evalTile;
                                        evalTile.child = tile; // new
                                    }
                                }
                                else
                                {
                                    // Not in open list   
                                    tile.parent = evalTile;
                                    tile.g = gScore; // # of steps to get to this tile
                                    tile.f = tile.g + tile.GetHScore(finalTile);
                                    openList.Add(tile);
                                    tile.pfState = MapTileData.openState; // new
                                    evalTile.child = tile; // new
                                }
                            }
                        }




                    } // End while loop

                    //Debug.Log(tries);


                    #region Pathfinding Finished OR Failed.
                    //if (openList.Count() > 0)
                    if (openList.Count > 0)
                    {
                        // Found a path
                        bool finished = false;
                        if (finalTile.parent == null)
                        {
                            //Debug.Log("Final tile no parent");
                        }
                        MapTileData pTile = finalTile.parent;
                        Corridor newC = new Corridor();
                        newC.InitializeLists();
                        newC.isCorridor = true;

                        int tFailSafe = 0;
                        while (!finished)
                        {
                            tFailSafe++;

                            if (tFailSafe == 100)
                            {
                                Debug.Log("Failsafe 3 break. Going from " + startTile.pos.ToString() + " " + startTile.tileType.ToString() + " to " + finalTile.pos.ToString() + " " + finalTile.tileType.ToString() + " Essential corridors: " + builtEssentialCorridors);
                                corAttempts++;
                                return false;
                            }

                            // Process current
                            if (pTile != null)
                            {
                                newC.internalTiles.Add(pTile);
                                pTile = pTile.parent;
                            }
                            else
                            {
                                //Debug.Log("problem: ptile is null? connecting tiles " + startTile.pos.ToString() + " and " + finalTile.pos.ToString());
                                corAttempts++;
                                break;
                            }

                            if (pTile == startTile)
                            {
                                finished = true;

                                if (builtEssentialCorridors == false)
                                {
                                    //Debug.Log("Raising requirement...");
                                    requirement += 4;
                                }

                                if (newC.internalTiles.Count > requirement)
                                {
                                    openList.Clear();
                                    //openList = new PriorityQueue<MapTileData>();

                                    //closedList.Clear();
                                    success = false;
                                    corAttempts++;
                                }
                                else
                                {
                                    requirement = mgd.maxCorridorLength;
                                    success = true;
                                    //Debug.Log("Connected " + room1.areaID + " to " + room2.areaID);
                                    BuildCorridorTiles(newC);

                                    newC.reachable = true;

                                    reachableRooms.Add(newC); // new 3/17/18 to track reachable more accurately

                                    newC.connections.Add(room1);
                                    newC.connections.Add(room2);
                                    AddAreaToDictionary(newC);
                                    mapCorridors.Add(newC);

                                    bestTile1.ChangeTileType(TileTypes.GROUND, mgd);
                                    bestTile1.AddTag(LocationTags.CORRIDORENTRANCE);
                                    bestTile1.AddMapTag(MapGenerationTags.FILLED);
                                    newC.edgeTiles.Remove(bestTile1);
                                    newC.edgeTiles.Remove(bestTile2);

                                    #region Unused Door Stuff
                                    // Don't put doors immediately next to other doors.
                                    //Shep: Note, doors aren't being added right now, comment back in when doors make glorious return
                                    /*
                                    bool doorNearby = false;
                                    for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                                    {
                                        Vector2 tryPos = bestTile1.pos + MapMasterScript.xDirections[i];
                                        if (mapArray[(int)tryPos.x, (int)tryPos.y].GetDoor() != null)
                                        {
                                            doorNearby = true;
                                        }
                                    }


                                    if ((UnityEngine.Random.Range(0, 2) == 1)) // Only add a door 50% of the time.
                                    {
                                        if (!doorNearby)
                                        {
                                            CreateAndAddDoor(bestTile1.pos);
                                        }

                                    }
                                    else
                                    {
                                        //bestTile1.ChangeTileType(TileTypes.GROUND, mgd);
                                    }
                                    */
                                    #endregion
                                    bestTile2.ChangeTileType(TileTypes.GROUND, mgd);
                                    bestTile2.AddTag(LocationTags.CORRIDORENTRANCE);
                                    bestTile2.AddMapTag(MapGenerationTags.FILLED);
                                    #region More Unused Door Stuff
                                    //Shep: Doors aren't being added right now, comment back in when doors make their glorious return
                                    /*
                                    if (UnityEngine.Random.Range(0, 2) == 1) // Only add a door 50% of the time.
                                    {
                                        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                                        {
                                            Vector2 tryPos = bestTile2.pos + MapMasterScript.xDirections[i];
                                            if (mapArray[(int)tryPos.x, (int)tryPos.y].GetDoor() != null)
                                            {
                                                doorNearby = true;
                                            }
                                        }
                                        if (!doorNearby)
                                        {
                                            CreateAndAddDoor(bestTile2.pos);
                                        }
                                    }
                                    else
                                    {
                                        //bestTile2.ChangeTileType(TileTypes.GROUND, mgd);
                                    }
                                    */
                                    #endregion
                                    room1.connections.Add(room2);
                                    room2.connections.Add(room1);
                                    room1.connections.Add(newC);
                                    room2.connections.Add(newC);

                                    room1.reachable = true;
                                    room2.reachable = true;

                                    // Track reachable more accurately 3/17/18
                                    if (!reachableRooms.Contains(room1)) reachableRooms.Add(room1);
                                    if (!reachableRooms.Contains(room2)) reachableRooms.Add(room2);
                                    unreachableRooms.Remove(room1);
                                    unreachableRooms.Remove(room2);
                                    //unreachableRoomCount -= 2;
                                }
                            }
                        } // End while
                        success = false;
                        corAttempts++;

                    }
                    else
                    {
                        //Debug.Log("Could not find path.");
                        success = false;
                    }
                    #endregion

                    //Debug.Log("Time elapsed: " + (Time.realtimeSinceStartup - timeAtPathfindingStart));

                    corAttempts++;
                }

                // End of corridor creation

            }
            // End of corridor creation

            // Begin deadend creation
            int maxDeadEndAttempts = 20;
            mapRooms.Shuffle();
            int indexOfLastRoomUsedForDeadend = 0;
            for (int n = 0; n < mgd.maxDeadends; n++)
            {
                int attempts = 0;
                bool success = false;
                int failSafe = 0;
                while (attempts < maxDeadEndAttempts && !success)
                {
                    failSafe++;
                    if (failSafe == 101)
                    {
                        //Debug.Log("Failsafe 4 break.");
                        break;
                    }

                    bool validRoomFound = false;
                    bool loopedBackToStart = true;
                    while (!validRoomFound)
                    {
                        if (indexOfLastRoomUsedForDeadend >= mapRooms.Count)
                        {
                            if (loopedBackToStart)
                            {
                                break;
                            }
                            indexOfLastRoomUsedForDeadend = 0;
                            loopedBackToStart = true;
                        }

                        // Check the room, see if we can use any of the tiles.
                        workRoom = mapRooms[indexOfLastRoomUsedForDeadend];
                        possibleTiles.Clear();

                        MapTileData checkEdgeTile = null;
                        for (int i = 0; i < workRoom.edgeTiles.Count; i++)
                        {
                            checkEdgeTile = workRoom.edgeTiles[i];
                            if (checkEdgeTile.tileType != TileTypes.WALL)
                            {
                                continue;
                            }
                            // Are there existing corridor tiles around?

                            bool anyBadTiles = false;
                            for (int r = 0; r < MapMasterScript.directions.Length; r++)
                            {
                                Vector2 newTile = checkEdgeTile.pos + MapMasterScript.directions[r];
                                if ((newTile.x <= 0) || (newTile.y <= 0) || (newTile.x >= columns - 1) || (newTile.y >= rows - 1))
                                {
                                    anyBadTiles = true;
                                    break;
                                }
                                if ((mapArray[(int)newTile.x, (int)newTile.y].CheckTag(LocationTags.CORRIDORENTRANCE)) || (mapArray[(int)newTile.x, (int)newTile.y].CheckTag(LocationTags.CORNER)))
                                {
                                    anyBadTiles = true;
                                    break;
                                }
                            }
                            if (anyBadTiles == false)
                            {
                                possibleTiles.Add(checkEdgeTile);
                            }

                        }

                        if (possibleTiles.Any())
                        {
                            validRoomFound = true;
                        }

                        indexOfLastRoomUsedForDeadend++;

                    }

                    if (!validRoomFound)
                    {
                        //Debug.Log("No more deadends possible on " + floor + " " + dungeonLevelData.layoutType);
                        attempts = 999;
                        break;
                    }

                    // Pick an edge tile that isn't a corner, existing entrance, or next to one.




                    MapTileData workTile = null; // workRoom.edgeTiles[0];

                    // Pick a tile?
                    if (possibleTiles.Count == 0) // None available.
                    {
                        attempts++;
                        if (floor == 216)
                        {
                            Debug.Log("No tiles available for deadend on this floor " + floor);
                        }
                        continue;
                    }
                    else
                    {
                        workTile = possibleTiles[UnityEngine.Random.Range(0, possibleTiles.Count)];
                    }

                    int startCheckDir = UnityEngine.Random.Range(0, 4);
                    int bestDir = -1;
                    int bestLength = 0;
                    for (int i = startCheckDir; i < 4; i++)
                    {
                        if (i > 3) i = 0; // Loop back to start of array
                        Vector2 newPos = workTile.pos + MapMasterScript.directions[i];

                        if (!InBounds(newPos)) continue;
                        if (CheckMTDArea(mapArray[(int)newPos.x, (int)newPos.y]) > 0) continue;

                        // We can build out by at least 1.
                        int localLength = 1;
                        if (localLength > bestLength)
                        {
                            bestLength = localLength;
                            bestDir = i;
                        }

                        while (localLength <= mgd.maxDeadendLength)
                        {
                            localLength++;
                            newPos += MapMasterScript.directions[i];
                            if (!InBounds(newPos)) break;
                            if (CheckMTDArea(mapArray[(int)newPos.x, (int)newPos.y]) > 0) break;
                            if (localLength > bestLength)
                            {
                                bestLength = localLength;
                                bestDir = i;
                            }

                        }
                    }

                    if (bestDir == -1 || bestLength < mgd.minDeadendLength)
                    {
                        // There were no good directions from that tile
                        attempts++;
                        continue;
                    }

                    possibleTiles.Clear();
                    for (int i = 0; i < bestLength; i++)
                    {
                        Vector2 goodPos = workTile.pos;

                        for (int x = 0; x <= i; x++)
                        {
                            goodPos += MapMasterScript.directions[bestDir];
                        }
                        MapTileData mtd = GetTile(goodPos);
                        possibleTiles.Add(mtd);
                    }

                    Corridor newC = new Corridor();
                    newC.InitializeLists();
                    AddAreaToDictionary(newC);

                    //Debug.Log("C: Area id increased to " + mapAreaID);
                    success = true;

                    workTile.ChangeTileType(TileTypes.GROUND, mgd);
                    workTile.AddMapTag(MapGenerationTags.FILLED);
                    workTile.AddTag(LocationTags.CORRIDORENTRANCE);
                    deadEndsCreated++;
                    //Shep: Doors aren't being added right now, comment back in when doors make their glorious return
                    /*
                    if (UnityEngine.Random.Range(0, 3) == 0)
                    {
                        bool doorNearby = false;
                        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                        {
                            Vector2 tryPos = workTile.pos + MapMasterScript.xDirections[i];
                            if (mapArray[(int)tryPos.x, (int)tryPos.y].GetDoor() != null)
                            {
                                doorNearby = true;
                            }
                        }
                        if (!doorNearby)
                        {
                            CreateAndAddDoor(workTile.pos);
                        }
                    }
                    */

                    for (int i = 0; i < possibleTiles.Count; i++)
                    {
                        possibleTiles[i].areaType = AreaTypes.CORRIDOR;

                        //SetTileAreaID(tile, newC);

                        possibleTiles[i].ChangeTileType(TileTypes.GROUND, mgd);
                        possibleTiles[i].AddMapTag(MapGenerationTags.FILLED);
                        newC.internalTiles.Add(possibleTiles[i]);
                    }

                    workRoom.deadends++;

                } // End deadend attempt loop


            } // End deadend creation loop

        } // End of gen if there's more than 1 room.

        /* Debug.Log("FLOOR " + floor + "\n 1: " + counter1 + "\n" + "2: " + counter2 + "\n" + "3: " + counter3 + "\n" + "4: " + counter4 + "\n" + "5: " + counter5 + "\n" + "6: " + counter6 + "\n" +
           "7: " + counter7 + "\n" + "8: " + counter8 + "\n" + "9: " + counter9 + "\n" + "10: " + counter10 + "\n" + "11: " + counter11 + "\n");   */
        return true;
    }

    public void SetTileAreaID(MapTileData mtd, Area ar)
    {
        areaIDArray[(int)mtd.pos.x, (int)mtd.pos.y] = ar.areaID;
    }

    private void BuildAndPlaceRoomsCave()
    {

    }

    public void FlushTilePathfindingData(Vector2 start, Vector2 finish, bool mapCreation)
    {
        int startX = 0;
        if (mapCreation) startX = 0;

        MapTileData.closedState += 2;
        MapTileData.openState += 2;

        for (int x = startX; x < columns - startX; x++)
        {
            for (int y = startX; y < rows - startX; y++)
            {
                if (mapArray[x, y].tileType == TileTypes.WALL && !mapCreation)
                {
                    continue;
                }
                mapArray[x, y].parent = null;
                mapArray[x, y].child = null;
                mapArray[x, y].f = 0f;
                mapArray[x, y].g = 0f;
                mapArray[x, y].h = -1f;

                // Deprecate
                mapArray[x, y].closed = false;
                mapArray[x, y].open = false;
            }
        }
    }

    public void OldFlushTilePathfindingData(Vector2 start, Vector2 finish)
    {
        Vector2 check = Vector2.zero;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                check.x = x;
                check.y = y;
                mapArray[x, y].parent = null;
                mapArray[x, y].child = null;
                mapArray[x, y].f = 0f;
                mapArray[x, y].g = 0f;
                //mapArray[x, y].h = Vector2.Distance(check, finish);
                mapArray[x, y].h = (check - finish).sqrMagnitude;

                /* mapArray[x, y].closed = false;
                    mapArray[x, y].open = false; */
            }
        }
    }

    public int CheckMTDArea(MapTileData mtd)
    {
        return areaIDArray[(int)mtd.pos.x, (int)mtd.pos.y];
    }

    private bool TileValidForLava(MapTileData mtd)
    {
        // Should we allow lava overlaps?
        if ((mtd.CheckTag(LocationTags.CORRIDOR)) || (mtd.CheckTag(LocationTags.LAVA)) || (mtd.CheckTag(LocationTags.CORRIDORENTRANCE)) || (mtd.CheckTag(LocationTags.WATER)) || (mtd.CheckTag(LocationTags.MUD)))
        {
            return false;
        }
        if (mtd.CheckTag(LocationTags.SECRET)) return false;

        // Don't build lava in the middle of nowhere or fill area.
        /*if (CheckMTDArea(mtd) < 0)
        {
            
            return false;
        }  */

        return true;
    }

    private List<MapTileData> FindTilesForLavaPool(int squareSize, bool limitToCenter)
    {
        List<MapTileData> lavaReturnList = new List<MapTileData>();
        MapTileData check;

        int minX = 1;
        int maxX = columns - 1;
        int minY = 1;
        int maxY = rows - 1;

        if (limitToCenter)
        {
            minX = (int)(columns * 0.34f);
            maxX = (int)(columns * 0.66f);
            minY = (int)(rows * 0.34f);
            maxY = (int)(rows * 0.66f);
        }

        for (int x = minX; x < maxX; x++)
        {

            if (UnityEngine.Random.Range(0, 1f) >= 0.3f)
            {
                int addAmount = UnityEngine.Random.Range(0, maxX - x);
                x += addAmount;
            }

            for (int y = minY; y < maxY; y++)
            {

                if (UnityEngine.Random.Range(0, 1f) > 0.25f)
                {
                    continue;
                }

                if (UnityEngine.Random.Range(0, 1f) >= 0.3f)
                {
                    int addAmount = UnityEngine.Random.Range(0, maxY - y);
                    y += addAmount;
                }

                check = mapArray[x, y];
                if (!TileValidForLava(check))
                {
                    continue;
                }
                if (squareSize == 1)
                {
                    lavaReturnList.Add(check);
                    return lavaReturnList;
                }
                for (int x2 = 1; x2 <= squareSize; x2++)
                {
                    if (x + x2 >= maxX)
                    {
                        break;
                    }
                    for (int y2 = 1; y2 <= squareSize; y2++)
                    {
                        if (y + y2 >= maxY)
                        {
                            break;
                        }
                        check = mapArray[x + x2, y + y2];
                        if (TileValidForLava(check))
                        {
                            lavaReturnList.Add(check);
                        }
                        else
                        {
                            lavaReturnList.Clear();
                            x2 = squareSize;
                            break;
                        }
                    }
                }
                if (lavaReturnList.Count > 0)
                {
                    return lavaReturnList;
                }
            }
        }
        return lavaReturnList;
    }

    public void BuildMudPatches(int number)
    {
        for (int b = 0; b < number; b++)
        {
            MapTileData mtd = waterTiles[UnityEngine.Random.Range(0, waterTiles.Count)];
            // Better way of finding water...
            MapTileData checkMtd = null;
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                Vector2 pos = mtd.pos + MapMasterScript.xDirections[i];
                checkMtd = mapArray[(int)pos.x, (int)pos.y];
                if (checkMtd == mtd)
                {
                    continue;
                }
                if (checkMtd.CheckTag(LocationTags.WATER) || checkMtd.CheckTag(LocationTags.LAVA))
                {
                    continue;
                }
                if (checkMtd.tileType != TileTypes.GROUND || checkMtd.CheckTag(LocationTags.MUD))
                {
                    continue;
                }
                checkMtd.AddTag(LocationTags.MUD);
                MapTileData checkMtd2 = null;
                for (int g = 0; g < MapMasterScript.xDirections.Length; g++)
                {
                    if (UnityEngine.Random.Range(0, 1f) < 0.25f) continue;
                    Vector2 pos2 = checkMtd.pos + MapMasterScript.xDirections[g];
                    checkMtd2 = mapArray[(int)pos2.x, (int)pos2.y];
                    if ((checkMtd2 == mtd) || (checkMtd2 == checkMtd))
                    {
                        continue;
                    }
                    if (checkMtd2.CheckTag(LocationTags.WATER) || checkMtd2.CheckTag(LocationTags.LAVA))
                    {
                        continue;
                    }
                    if (checkMtd2.tileType != TileTypes.GROUND || checkMtd2.CheckTag(LocationTags.MUD))
                    {
                        continue;
                    }
                    checkMtd2.AddTag(LocationTags.MUD);
                }
            }
        }
    }

    public void BuildRiver()
    {
        if (openList == null)
        {
            openList = new PriorityQueue<MapTileData>();
        }
        else
        {
            openList.data.Clear();
        }

        if (tilePath == null)
        {
            tilePath = new List<MapTileData>();
        }
        else
        {
            tilePath.Clear();
        }

        bool[] adjacentValid = new bool[8];

        MapTileData startTile = null;

        bool mode = false;        

        if (dungeonLevelData.layoutType != DungeonFloorTypes.KEEP)
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                startTile = mapArray[1, UnityEngine.Random.Range(1, rows - 1)];
            }
            else
            {
                startTile = mapArray[UnityEngine.Random.Range(1, columns - 1), 1];
                mode = true;
            }
        }
        else
        {
            // River should run perpendicular to the hallways of the dungeon
            if (dictMapDataForGeneration["horizontal"] == 0)
            {
                startTile = mapArray[1, UnityEngine.Random.Range(4, rows - 4)];
            }
            else
            {
                startTile = mapArray[UnityEngine.Random.Range(4, columns - 4), 1];
                mode = true;
            }
        }


        startTile.f = 0;

        MapTileData evalTile = startTile;

        MapTileData finalTile = null;

        if (dungeonLevelData.layoutType != DungeonFloorTypes.KEEP)
        {
            if (!mode)
            {
                finalTile = mapArray[columns - 2, UnityEngine.Random.Range(1, rows - 1)];
            }
            else
            {
                finalTile = mapArray[UnityEngine.Random.Range(1, columns - 1), rows - 2];
            }
        }
        else
        {
            if (!mode)
            {
                finalTile = mapArray[columns - 2, UnityEngine.Random.Range(4, rows - 4)];
            }
            else
            {
                finalTile = mapArray[UnityEngine.Random.Range(4, columns - 4), rows - 2];
            }
        }


        FlushTilePathfindingData(startTile.pos, finalTile.pos, true);
        openList.Enqueue(startTile);

        startTile.pfState = MapTileData.openState;

        Vector2 checkAdjPos;
        MapTileData adjTile;

        while (openList.Count() > 0)
        {
            // Pathfinding            

            //Take the very best one, certified by the fact 
            //that we're using a priority queue.
            evalTile = openList.Dequeue();

            if (evalTile.pos.x == finalTile.pos.x && evalTile.pos.y == finalTile.pos.y)
            {
                // Found our path!
                finalTile = evalTile;
                break;
            }

            //rip            
            evalTile.pfState = MapTileData.closedState;

            for (int i = 0; i < MapMasterScript.directions.Length; i++)
            {
                checkAdjPos = evalTile.pos + MapMasterScript.directions[i];
                if (checkAdjPos.x > 0 && checkAdjPos.y > 0 && checkAdjPos.x < columns - 1 && checkAdjPos.y < rows - 1)
                {
                    adjTile = mapArray[(int)(evalTile.pos.x + MapMasterScript.directions[i].x), (int)(evalTile.pos.y + MapMasterScript.directions[i].y)];

                    if (Vector2.Equals(adjTile.pos, finalTile.pos))
                    {
                        adjTile.parent = evalTile;
                        evalTile.child = adjTile;
                        adjTile.f = -1;
                        adjTile.pfState = MapTileData.openState;
                        openList.Enqueue(adjTile);
                        break;
                    }

                    if (adjTile.pfState == MapTileData.closedState)
                    {
                        continue;
                    }

                    if (adjTile.CheckTag(LocationTags.CORRIDORENTRANCE))
                    {
                        adjTile.pfState = MapTileData.closedState;
                        continue;
                    }

                    // River can cut through stuff... Why not?
                    /*if ((dungeonLevelData.layoutType != DungeonFloorTypes.AUTOCAVE) && (adjTile.containingArea != null) && (adjTile.containingArea.internalTiles.Contains(adjTile)))
                    {
                        adjTile.closed = true;
                        continue;
                    }*/

                    float gScore = evalTile.g + 1;

                    // Modify gScore here

                    bool containsTile = false;

                    if (adjTile.pfState == MapTileData.openState)
                    {
                        float localF = gScore + adjTile.GetHScore(finalTile);
                        if (localF < adjTile.f)
                        {
                            // better path
                            adjTile.g = gScore;
                            adjTile.f = adjTile.g + adjTile.GetHScore(finalTile);
                            adjTile.parent = evalTile;
                            evalTile.child = adjTile;
                        }
                        containsTile = true;
                    }

                    if (!containsTile)
                    {
                        gScore += UnityEngine.Random.Range(1f, 12f);
                        // Not in open list   
                        adjTile.parent = evalTile;
                        evalTile.child = adjTile;
                        adjTile.g = gScore; // # of steps to get to this tile
                                            //tile.h = Vector2.Distance(tile.pos, finalTile.pos) * 5f;
                                            //tile.h = MapMasterScript.GetGridDistance(tile.pos, finalTile.pos); //Modifying this because diagonals are OK.
                        adjTile.f = adjTile.g + adjTile.GetHScore(finalTile);
                        adjTile.pfState = MapTileData.openState;
                        openList.Enqueue(adjTile);
                    }


                }
            }

        }
        // End of pathfinding WHILE loop

        if (openList.Count() > 0)
        {
            // Found a path
            bool finished = false;
            MapTileData pTile = finalTile.parent;
            tilePath.Clear();
            tilePath.Add(finalTile);
            tilePath.Add(pTile);
            while (!finished)
            {
                if (pTile.parent == startTile)
                {
                    // Use pTile as the next move.
                    finished = true;
                    tilePath.Add(startTile);
                }
                //Debug.Log("Ptile is " + pTile.pos + " when traveling " + GetPos() + " " + finalTile.pos);

                pTile = pTile.parent;
                tilePath.Add(pTile);
            }
        }
        else
        {
            Debug.Log("No river path. " + floor);
            // Did NOT find a path.
        }

        int tCount = tilePath.Count;
        for (int x = 0; x < tCount; x++)
        {
            if (tilePath[x].tileType != TileTypes.GROUND)
            {
                tilePath[x].AddMapTag(MapGenerationTags.MAPGEN_RIVERDIGOUT);
            }
            tilePath[x].ChangeTileType(TileTypes.GROUND, mgd); // Was wall.
            tilePath[x].AddTag(LocationTags.WATER);
            
            waterTiles.Add(tilePath[x]);
        }
    }


    private void BuildTerrainPools(int numTerrainPools, LocationTags tag, bool centerOnly)
    {
        List<MapTileData> masterLavaPoolList = new List<MapTileData>();
        List<MapTileData> validTilesForLava = new List<MapTileData>();

        for (int i = 0; i < numTerrainPools; i++)
        {
            int size = 3;
            if (UnityEngine.Random.Range(0, 1f) <= 0.4f)
            {
                size = 2;
            }
            validTilesForLava = FindTilesForLavaPool(size, centerOnly); // Generalize this
            if (validTilesForLava.Count > 0)
            {
                foreach (MapTileData mtd in validTilesForLava)
                {
                    masterLavaPoolList.Add(mtd);
                    mtd.ChangeTileType(TileTypes.GROUND, mgd); // If it wasn't ground before, it is now.
                    mtd.AddTag(tag);
                    //Debug.Log(mtd.pos + " is now lava on floor " + floor);
                }
            }
        }

        List<MapTileData> addedLava = new List<MapTileData>();

        Vector2 pos = Vector2.zero;
        MapTileData checkMtd = null;
        foreach (MapTileData mtd in masterLavaPoolList)
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.3f) continue;
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f) continue;
                pos = mtd.pos + MapMasterScript.xDirections[i];
                if (InBounds(pos))
                {
                    checkMtd = mapArray[(int)pos.x, (int)pos.y];
                    if (!checkMtd.CheckTag(LocationTags.LAVA) && !checkMtd.CheckTag(LocationTags.WATER))
                    {
                        checkMtd.ChangeTileType(TileTypes.GROUND, mgd); // If it wasn't ground before, it is now.
                        checkMtd.AddTag(tag);
                        addedLava.Add(checkMtd);
                    }
                }
            }
        }

        foreach (MapTileData mtd in addedLava)
        {
            masterLavaPoolList.Add(mtd);
        }
    }

    public void BuildLavaPools(int numLavaPools, bool centerOnly)
    {
        List<MapTileData> masterLavaPoolList = new List<MapTileData>();
        List<MapTileData> validTilesForLava = new List<MapTileData>();

        for (int i = 0; i < numLavaPools; i++)
        {
            int size = 3;
            if (UnityEngine.Random.Range(0, 1f) <= 0.4f)
            {
                size--;
            }

            validTilesForLava = FindTilesForLavaPool(size, centerOnly);
            if (validTilesForLava.Count > 0)
            {
                MapTileData lavaMTD = null;
                for (int x = 0; x < validTilesForLava.Count; x++)
                {
                    lavaMTD = validTilesForLava[x];
                    masterLavaPoolList.Add(lavaMTD);
                    lavaMTD.ChangeTileType(TileTypes.GROUND, mgd); // If it wasn't ground before, it is now.
                    lavaMTD.AddTag(LocationTags.LAVA);
                }
            }
        }

        List<MapTileData> addedLava = new List<MapTileData>();

        Vector2 pos = Vector2.zero;
        MapTileData checkMtd = null;
        for (int x = 0; x < masterLavaPoolList.Count; x++)
        {
            MapTileData mtd = masterLavaPoolList[x];
            if (UnityEngine.Random.Range(0, 1f) <= 0.3f) continue;
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f) continue;
                pos = mtd.pos + MapMasterScript.xDirections[i];
                if (InBounds(pos))
                {
                    checkMtd = mapArray[(int)pos.x, (int)pos.y];
                    if ((!checkMtd.CheckTag(LocationTags.LAVA)) && (!checkMtd.CheckTag(LocationTags.WATER)))
                    {
                        checkMtd.ChangeTileType(TileTypes.GROUND, mgd); // If it wasn't ground before, it is now.
                        checkMtd.AddTag(LocationTags.LAVA);
                        addedLava.Add(checkMtd);
                    }
                }
            }
        }

        for (int i = 0; i < addedLava.Count; i++)
        {
            masterLavaPoolList.Add(addedLava[i]);
        }

        //Debug.Log("Created " + masterLavaPoolList.Count + " lava tiles on floor " + floor + " with max pools " + numLavaPools);
    }

    public bool InBounds(Vector2 pos)
    {
        if (pos.x <= 0 || pos.x >= columns - 1 || pos.y <= 0 || pos.y >= rows - 1)
        {
            return false;
        }
        return true;
    }

    private bool RoomInBounds(Vector2 pos)
    {
        if ((pos.x < 0) || (pos.x > columns - 1) || (pos.y < 0) || (pos.y > rows - 1))
        {
            return false;
        }
        return true;
    }

    private void FillUnusedCaveTiles()
    {
        for (int i = 0; i < dungeonLevelData.extraRivers; i++)
        {
            BuildRiver();
            BuildMudPatches(4);
        }

        Area fillArea = areaDictionary[-777];
        fillArea.InitializeLists();

        List<MapTileData> tilesAroundPoint = new List<MapTileData>();

        for (int x = 1; x < columns - 2; x++)
        {
            for (int y = 1; y < rows - 2; y++)
            {
                MapTileData mtd = mapArray[x, y];
                /* if (mtd.CheckTag(LocationTags.WATER))
                {
                    continue;
                } */
                if ((CheckMTDArea(mtd) < 0) && ((mtd.tileType == TileTypes.NOTHING) || (mtd.tileType == TileTypes.GROUND)))
                {
                    SetTileAreaID(mtd, fillArea);
                    TryFillGroundTile(mtd);
                }
            }
        }
    }

    public bool IsMapEdge(MapTileData mtd)
    {
        if ((mtd.pos.x <= 0) || (mtd.pos.y <= 0) || (mtd.pos.x >= columns - 1) || (mtd.pos.y >= rows - 1))
        {
            return true;
        }
        return false;
    }

    private void TryFillGroundTile(MapTileData mtd)
    {
        Area fillArea = areaDictionary[-777];
        fillArea.InitializeLists();
        bool convertedToWall = false;
        if (UnityEngine.Random.Range(0, 1f) <= dungeonLevelData.caveFillConvertChance) // Adjust this for interesting shapes.
        {
            //pool_tileList = GetTilesAroundPoint(mtd.pos, 1);

            CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);

            bool safe = true;

            for (int t = 0; t < CustomAlgorithms.numTilesInBuffer; t++)
            {
                if (CustomAlgorithms.tileBuffer[t] == mtd) continue;

                if ((CheckMTDArea(CustomAlgorithms.tileBuffer[t]) < 0) || (CheckMTDArea(CustomAlgorithms.tileBuffer[t]) == fillArea.areaID)) // Was == -1?
                {
                    if (((CustomAlgorithms.tileBuffer[t].tileType == TileTypes.NOTHING) || (CustomAlgorithms.tileBuffer[t].tileType == TileTypes.GROUND)) && (!IsMapEdge(CustomAlgorithms.tileBuffer[t])))
                    {
                        // continue
                    }
                    else
                    {
                        safe = false;
                        break;
                    }
                }
                else
                {
                    safe = false;
                    break;
                }
            }

            if (safe)
            {
                fillArea.edgeTiles.Add(mtd);
                mtd.ChangeTileType(TileTypes.WALL, mgd);
                mtd.RemoveMapTag(MapGenerationTags.EDGETILE);
                //Debug.Log("Converted " + mtd.pos + " " + floor);
                convertedToWall = true;
            }
            else
            {
                fillArea.internalTiles.Add(mtd);
                mtd.ChangeTileType(TileTypes.GROUND, mgd);
                mtd.RemoveMapTag(MapGenerationTags.EDGETILE);
                convertedToWall = false;
            }
        }
        if (!convertedToWall)
        {
            fillArea.internalTiles.Add(mtd);
            mtd.ChangeTileType(TileTypes.GROUND, mgd);
            mtd.RemoveMapTag(MapGenerationTags.EDGETILE);
        }
    }

    private void BuildCaveEdgeJaggies()
    {
        float baseChance = 0.5f; // Hardcoded
        float addChance = 0.65f;
        List<MapTileData> tilesToConvert = new List<MapTileData>();
        for (int x = 0; x < columns - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {

                if ((x == 0) || (y == 0) || (x == columns - 2) || (y == rows - 2))
                {
                    Vector2 add = Vector2.zero;

                    if ((x == 0) && (y > 0)) // Left edge
                    {
                        add = MapMasterScript.xDirections[(int)Directions.EAST];
                    }
                    if ((x == columns - 2) && (y > 0)) // Right edge
                    {
                        add = MapMasterScript.xDirections[(int)Directions.WEST];
                    }
                    if ((x > 0) && (y == 0)) // Bottom
                    {
                        add = MapMasterScript.xDirections[(int)Directions.NORTH];
                    }
                    if ((x > 0) && (y == rows - 2)) // Top
                    {
                        add = MapMasterScript.xDirections[(int)Directions.SOUTH];
                    }
                    if ((add != Vector2.zero) && (UnityEngine.Random.Range(0, 1f) <= baseChance))
                    {
                        bool jaggy = true;
                        int numJaggies = 0;
                        while ((jaggy) && (numJaggies < 3))
                        {
                            MapTileData check = mapArray[(int)(x + (add.x * (numJaggies + 1))), (int)(y + (add.y * (numJaggies + 1)))];
                            if (CheckMTDArea(check) > 0)
                            {
                                jaggy = false;
                                continue;
                            }
                            if (check.tileType == TileTypes.WALL)
                            {
                                tilesToConvert.Clear();
                                break;
                            }
                            tilesToConvert.Add(check);
                            numJaggies++;
                            if (UnityEngine.Random.Range(0, 1f) <= addChance)
                            {
                                jaggy = true;
                            }
                            else
                            {
                                jaggy = false;
                            }
                        }

                        // Final check
                        MapTileData finalCheck = mapArray[(int)(x + (add.x * (numJaggies + 1))), (int)(y + (add.y * (numJaggies + 1)))];
                        if (finalCheck.tileType == TileTypes.GROUND)
                        {
                            if (tilesToConvert.Count > 0)
                            {
                                for (int i = 0; i < tilesToConvert.Count; i++)
                                {
                                    tilesToConvert[i].ChangeTileType(TileTypes.WALL, mgd);
                                    tilesToConvert[i].AddTag(LocationTags.DUGOUT);
                                    //pool_tileList = GetTilesAroundPoint(tilesToConvert[i].pos, 1);
                                    CustomAlgorithms.GetTilesAroundPoint(tilesToConvert[i].pos, 1, this);
                                    for (int z = 0; z < CustomAlgorithms.numTilesInBuffer; z++)
                                    //foreach(MapTileData mtd2 in pool_tileList)
                                    {
                                        if (CustomAlgorithms.tileBuffer[z].tileType == TileTypes.GROUND)
                                        {
                                            CustomAlgorithms.tileBuffer[z].AddTag(LocationTags.DUGOUT);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }
        //Debug.Log("Done building cave jaggies");
    }

    public bool BuildMaze()
    {
        bool[,] isGround = new bool[columns, rows]; // FALSE = wall, TRUE = ground

        int startX = UnityEngine.Random.Range(1, columns - 2);
        int startY = UnityEngine.Random.Range(1, rows - 2);
        isGround[startX, startY] = true;

        List<Point> openTiles = new List<Point>();
        // Try a hashset here?


        Point[,] allPoints = new Point[columns, rows];
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                allPoints[x, y] = new Point(x, y);
            }
        }

        openTiles.Add(allPoints[startX, startY]);

        List<MapTileData> subNeighbors = new List<MapTileData>();

        int tries = 0;

        List<MapTileData> cardinalNeighbors = new List<MapTileData>();
        MapTileData checkMTD;
        Point baseTile;
        MapTileData mtd2;

        while (openTiles.Count > 0)
        {
            tries++;
            if (tries > 300000)
            {
                Debug.Log("Breaking! Floor: " + dungeonLevelData.layoutType + " " + floor);
                break;
            }
            baseTile = openTiles[UnityEngine.Random.Range(0, openTiles.Count)];

            //Look at the cardinal tiles around our base point, starting in a random direction
            int iCardinalCount = 0;
            int iStart = UnityEngine.Random.Range(0, 4) * 2;
            bool anyValidTiles = false;

            //from that tile, look all of ITs neighbors
            for (int i = iStart; iCardinalCount < 4; i = (i + 2) % 8)
            {
                iCardinalCount++;
                checkMTD = GetTileInDirection(baseTile, (Directions)i);
                if (checkMTD == null)
                {
                    continue;
                }
                int numGroundNearby = 0;
                for (Directions subD = Directions.NORTH; subD < Directions.RANDOMALL; subD += 2)
                {
                    mtd2 = GetTileInDirection(checkMTD.pos, subD);
                    if (mtd2 == null)
                    {
                        continue;
                    }
                    if (isGround[(int)mtd2.pos.x, (int)mtd2.pos.y])
                    {
                        numGroundNearby++;
                    }
                }

                if (numGroundNearby <= 1)
                {
                    isGround[(int)checkMTD.pos.x, (int)checkMTD.pos.y] = true;
                    openTiles.Add(allPoints[(int)checkMTD.pos.x, (int)checkMTD.pos.y]);
                    anyValidTiles = true;
                    break;
                }
                else
                {
                    // Remove point?
                    if (numGroundNearby >= 2)
                    {
                        //Changed remove to be a little quicker
                        Point p = new Point(checkMTD.pos);
                        int idxKillMe = openTiles.FindIndex(sp => (sp.x == p.x && sp.y == p.y));
                        if (idxKillMe >= 0)
                        {
                            int idxLast = openTiles.Count - 1;
                            openTiles[idxKillMe] = openTiles[idxLast];
                            openTiles.RemoveAt(idxLast);
                        }
                    }
                }
            }
            if (!anyValidTiles)
            {
                //Changed remove to be a little quicker
                int idxKillMe = openTiles.FindIndex(sp => (sp.x == baseTile.x && sp.y == baseTile.y));
                if (idxKillMe >= 0)
                {
                    int idxLast = openTiles.Count - 1;
                    openTiles[idxKillMe] = openTiles[idxLast];
                    openTiles.RemoveAt(idxLast);
                }
            }
        }

        // Now fill in weird spots.


        for (int x = 1; x < columns - 2; x++)
        {
            for (int y = 1; y < rows - 2; y++)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // Vertical bias
                    if (((isGround[x - 1, y]) && (!isGround[x + 1, y])) || (!isGround[x - 1, y]) && (isGround[x + 1, y]))
                    {
                        if (!isGround[x, y + 1] && !isGround[x, y - 1])
                        {
                            isGround[x, y] = false;
                            //x = 0;
                            //y = 0;
                        }
                    }
                }
                else
                {
                    // Horizontal bias
                    if ((isGround[x, y + 1] && !isGround[x, y - 1]) || (!isGround[x, y + 1] && isGround[x, y - 1]))
                    {
                        if ((!isGround[x + 1, y]) && (!isGround[x - 1, y]))
                        {
                            isGround[x, y] = false;
                            //x = 0;
                            //y = 0;
                        }
                    }
                }
            }
        }



        int startCheckX = 0;
        int startCheckY = 0;

        tries = 0;

        FloodPoint[,] pointArray = new FloodPoint[columns, rows];
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                pointArray[x, y] = new FloodPoint(x, y, 0);

            }
        }

        // Find a simple ground tile.
        MapTileData checkMtd = mapArray[startCheckX, startCheckY];
        // MUST find a ground tile.
        while ((!isGround[startCheckX, startCheckY]) && tries < 30000) // Checkmtd = -1, should this be <= 0? 
        {
            tries++;
            startCheckX = UnityEngine.Random.Range(1, columns - 1);
            startCheckY = UnityEngine.Random.Range(1, rows - 1);
            checkMtd = mapArray[startCheckX, startCheckY];
        }
        if (tries >= 30000)
        {
            Debug.Log("Cavern fill error on floor. " + floor);
        }
        pointArray[startCheckX, startCheckY].color = 1; // 1 is the first cavern.
        Dictionary<int, int> cavernSizes = new Dictionary<int, int>();
        int cavernColorIndex = 1;
        FloodPoint startPoint = pointArray[startCheckX, startCheckY];

        int cavernSize = FillCavern(pointArray, isGround, startPoint);
        cavernSizes.Add(cavernColorIndex, cavernSize);
        cavernColorIndex++;

        //Debug.Log("Floor " + floor + " found a cavern, color " + (cavernColorIndex - 1) + " size " + cavernSize);

        // Are there any other caverns?

        bool filledAllCaverns = false;

        while (!filledAllCaverns)
        {
            bool foundCavern = false;
            for (int x = 0; x <= columns - 1; x++)
            {
                for (int y = 0; y <= rows - 1; y++)
                {
                    if ((isGround[x, y]) && (pointArray[x, y].color == 0))
                    {
                        // This is a ground space that has not been checked.
                        startPoint = pointArray[x, y];
                        startPoint.color = cavernColorIndex;
                        foundCavern = true;
                        break;
                    }
                    if (foundCavern) break;
                }
                if (foundCavern) break;
            }
            if (!foundCavern)
            {
                filledAllCaverns = true;
            }
            else
            {
                int size = FillCavern(pointArray, isGround, startPoint);
                //Debug.Log("Floor " + floor + " found a cavern, color " + cavernColorIndex + " size " + size);

                cavernSizes.Add(cavernColorIndex, size);
                cavernColorIndex++;
            }
        }

        int biggestCavernColorIndex = -1;
        int biggestNumber = 0;

        foreach (int colorIndex in cavernSizes.Keys)
        {
            if (cavernSizes[colorIndex] > biggestNumber)
            {
                biggestCavernColorIndex = colorIndex;
                biggestNumber = cavernSizes[colorIndex];
            }
        }

        //Debug.Log("Biggest cavern is " + biggestCavernColorIndex + " " + biggestNumber + " floor " + floor);

        Area fillArea = areaDictionary[-777];
        fillArea.InitializeLists();

        //string row = "";

        for (int x = 1; x < columns - 2; x++)
        {
            for (int y = 1; y < rows - 2; y++)
            {
                if (pointArray[x, y].color != biggestCavernColorIndex)
                {
                    isGround[x, y] = false;
                }
                if (isGround[x, y])
                {
                    mapArray[x, y].ChangeTileType(TileTypes.GROUND, mgd);
                    SetTileAreaID(mapArray[x, y], fillArea);
                    fillArea.internalTiles.Add(mapArray[x, y]);
                    mapArray[x, y].RemoveMapTag(MapGenerationTags.EDGETILE);
                    //row += ".";
                }
                else
                {
                    mapArray[x, y].ChangeTileType(TileTypes.WALL, mgd);
                    //row += "x";
                }
            }
            //row += "\n";
        }

        //Debug.Log(row);
        return true;

    }

    public bool BuildAutomataCave()
    {
        // Initialize the map.
        // General variables
        float chanceToStartAlive = dungeonLevelData.cellChanceToStartAlive; // 0.45f; 
        int maxNeighbors = dungeonLevelData.cellMaxNeighbors; // 4 // Birth limit.
        int minNeighbors = dungeonLevelData.cellMinNeighbors; // 3 This is death limit.
        int simulationSteps = dungeonLevelData.cellSimulationSteps; // 1

        bool[,] newMap = new bool[columns, rows];
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= chanceToStartAlive)
                {
                    newMap[x, y] = true; // TRUE is GROUND.
                }
            }
        }

        for (int i = 0; i < simulationSteps; i++)
        {
            newMap = DoCellAutomataStep(newMap, minNeighbors, maxNeighbors);
        }

        // Remove diagonals.

        FloodPoint[,] pointArray = new FloodPoint[columns, rows];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                pointArray[x, y] = new FloodPoint(x, y, 0);
            }
        }        

        if (!dungeonLevelData.allowDiagonals)
        {
            if (dungeonLevelData.layoutType != DungeonFloorTypes.VOLCANO)
            {
                newMap = RemoveMapDiagonals(newMap, pointArray);
            }
        }

        // Now flood fill check.
        int startCheckX = 0;
        int startCheckY = 0;

        int tries = 0;

        // Find a simple ground tile.
        try
        {
            while (!newMap[startCheckX, startCheckY] && tries < 1000)
            {
                tries++;
                startCheckX = UnityEngine.Random.Range(0, columns);
                startCheckY = UnityEngine.Random.Range(0, rows);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Failure on floor " + floor + " type " + dungeonLevelData.layoutType + " in cell automata design.");
            Debug.Log(columns + " " + rows + " " + startCheckX + " " + startCheckY + " " + newMap.Length + " " + newMap.LongLength);
            Debug.Log(e);
        }


        pointArray[startCheckX, startCheckY].color = 1; // 1 is the first cavern.
        Dictionary<int, int> cavernSizes = new Dictionary<int, int>();
        int cavernColorIndex = 1;
        FloodPoint startPoint = pointArray[startCheckX, startCheckY];

        int cavernSize = FillCavern(pointArray, newMap, startPoint);
        cavernSizes.Add(cavernColorIndex, cavernSize);
        cavernColorIndex++;

        // Are there any other caverns?

        bool filledAllCaverns = false;

        while (!filledAllCaverns)
        {
            bool foundCavern = false;
            for (int x = 1; x < columns - 1; x++)
            {
                for (int y = 1; y < rows - 1; y++)
                {
                    if ((newMap[x, y]) && (pointArray[x, y].color == 0))
                    {
                        // This is a ground space that has not been checked.
                        startPoint = pointArray[x, y];
                        startPoint.color = cavernColorIndex;
                        foundCavern = true;
                        break;
                    }
                    if (foundCavern) break;
                }
                if (foundCavern) break;
            }
            if (!foundCavern)
            {
                filledAllCaverns = true;
            }
            else
            {
                int size = FillCavern(pointArray, newMap, startPoint);
                cavernSizes.Add(cavernColorIndex, size);
                cavernColorIndex++;
            }
        }

        int biggestCavernColorIndex = -1;
        int biggestNumber = 0;

        foreach (int colorIndex in cavernSizes.Keys)
        {
            if (cavernSizes[colorIndex] > biggestNumber)
            {
                biggestCavernColorIndex = colorIndex;
                biggestNumber = cavernSizes[colorIndex];
            }
        }


        int numGroundTiles = 0;

        MapTileData mtd;

        // Let's debug.
        for (int x = 0; x <= columns - 1; x++)
        {
            for (int y = 0; y <= rows - 1; y++)
            {
                mtd = mapArray[x, y];
                //mtd.caveColorOrigDebug = pointArray[x, y].color;
            }
        }
        // End debug.


        for (int x = 0; x <= columns - 1; x++)
        {
            for (int y = 0; y <= rows - 1; y++)
            {
                if (!newMap[x, y]) continue; // Walls are unrelated to this.

                if (pointArray[x, y].color != biggestCavernColorIndex)
                {
                    newMap[x, y] = false;
                    // For debugging.
                    mtd = mapArray[x, y];
                    mtd.caveColor = 99;
                }
                else
                {
                    numGroundTiles++;
                }
            }
        }

        float minGroundTiles = (columns * rows) * dungeonLevelData.cellMinGroundPercent; // 0.45 default
        float maxGroundTiles = (columns * rows) * dungeonLevelData.cellMaxGroundPercent; // 0.69 default

        float percent = numGroundTiles / (float)(columns * rows);

        //Debug.Log("Floor " + floor + " Percent ground: " + percent + " ");

        if ((numGroundTiles < (int)minGroundTiles) || (numGroundTiles > (int)maxGroundTiles))
        {
            //Debug.Log("Floor " + floor + " did not meet ground tile thresh with " + numGroundTiles + " vs min/max " + minGroundTiles + " " + maxGroundTiles);
            return false;
        }


        Area fillArea = areaDictionary[-777];
        fillArea.InitializeLists();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                mtd = mapArray[x, y];

                if ((x == 0) || (y == 0) || (x == columns - 1) || (y == rows - 1))
                {
                    mtd.ChangeTileType(TileTypes.WALL, mgd);
                    continue;
                }

                if (newMap[x, y])
                {
                    mtd.ChangeTileType(TileTypes.GROUND, mgd);
                    SetTileAreaID(mtd, fillArea);
                    //mtd.containingArea = fillArea;
                    fillArea.internalTiles.Add(mtd);
                    mtd.RemoveMapTag(MapGenerationTags.EDGETILE);
                    mtd.caveColor = pointArray[x, y].color;
                }
                else
                {
                    mtd.ChangeTileType(TileTypes.WALL, mgd);
                    fillArea.edgeTiles.Add(mtd);
                    mtd.AddMapTag(MapGenerationTags.EDGETILE);
                }
            }
        }

        return true;
    }

    private bool[,] RemoveMapDiagonals(bool[,] newMap, FloodPoint[,] pointArray)
    {
        FloodPoint checkNorth = new FloodPoint(0, 0, 0);
        FloodPoint checkSouth = new FloodPoint(0, 0, 0);
        FloodPoint checkWest = new FloodPoint(0, 0, 0);
        FloodPoint checkEast = new FloodPoint(0, 0, 0);
        FloodPoint checkTile = new FloodPoint(0, 0, 0);

        FloodPoint tileNorthEast;
        FloodPoint tileNorthWest;
        FloodPoint tileSouthEast;
        FloodPoint tileSouthWest;


        // Make sure this tile has no "diagonal only" movement.
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                checkTile.x = x;
                checkTile.y = y;
                if (newMap[x, y]) // Ground
                {
                    checkNorth.x = checkTile.x;
                    checkNorth.y = checkTile.y + 1;

                    checkSouth.x = checkTile.x;
                    checkSouth.y = checkTile.y - 1;

                    checkWest.x = checkTile.x - 1;
                    checkWest.y = checkTile.y;

                    checkEast.x = checkTile.x + 1;
                    checkEast.y = checkTile.y;

                    tileNorthEast = pointArray[x + 1, y + 1];
                    tileNorthWest = pointArray[x - 1, y + 1];
                    tileSouthEast = pointArray[x + 1, y - 1];
                    tileSouthWest = pointArray[x - 1, y - 1];

                    // OXO
                    // XOX
                    // OXO

                    if ((newMap[tileNorthWest.x, tileNorthWest.y]) && (!newMap[checkWest.x, checkWest.y]) && (!newMap[checkNorth.x, checkNorth.y]))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            newMap[checkWest.x, checkWest.y] = true;
                        }
                        else
                        {
                            newMap[checkNorth.x, checkNorth.y] = true;
                        }
                    }
                    // NE clear, E/N not clear
                    if ((newMap[tileNorthEast.x, tileNorthEast.y]) && (!newMap[checkEast.x, checkEast.y]) && (!newMap[checkNorth.x, checkNorth.y]))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            newMap[checkEast.x, checkEast.y] = true;
                        }
                        else
                        {
                            newMap[checkNorth.x, checkNorth.y] = true;
                        }
                    }

                    // SE clear, E/S not clear
                    if ((newMap[tileSouthEast.x, tileSouthEast.y]) && (!newMap[checkEast.x, checkEast.y]) && (!newMap[checkSouth.x, checkSouth.y]))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            newMap[checkEast.x, checkEast.y] = true;
                        }
                        else
                        {
                            newMap[checkSouth.x, checkSouth.y] = true;
                        }
                    }

                    // SW clear, W/S not clear
                    if ((newMap[tileSouthWest.x, tileSouthWest.y]) && (!newMap[checkWest.x, checkWest.y]) && (!newMap[checkSouth.x, checkSouth.y]))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            newMap[checkWest.x, checkWest.y] = true;
                        }
                        else
                        {
                            newMap[checkSouth.x, checkSouth.y] = true;
                        }
                    }

                }
            }
        }

        return newMap;
    }

    private int FillCavern(FloodPoint[,] pointArray, bool[,] mapLayout, FloodPoint startPoint)
    {
        // Map Layout array: TRUE means the tile is ground, and thus should be filled
        int cavernSize = 0;
        Queue<FloodPoint> pointsToFill = new Queue<FloodPoint>();
        pointsToFill.Enqueue(startPoint);
        FloodPoint processPoint;
        FloodPoint nextPoint;

        while (pointsToFill.Count > 0)
        {
            cavernSize++;
            processPoint = pointsToFill.Dequeue();
            if (!mapLayout[processPoint.x, processPoint.y]) continue; // Not sure why this would ever happen. Is it necessary?
            if (processPoint.x + 1 < columns - 1)
            {
                nextPoint = pointArray[processPoint.x + 1, processPoint.y];
                if ((nextPoint.color == 0) && (mapLayout[nextPoint.x, nextPoint.y]))
                {
                    // Not yet checked, and not a wall tile.
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.x - 1 > 0)
            {
                nextPoint = pointArray[processPoint.x - 1, processPoint.y];
                if ((nextPoint.color == 0) && (mapLayout[nextPoint.x, nextPoint.y]))
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.y + 1 < rows - 1)
            {
                nextPoint = pointArray[processPoint.x, processPoint.y + 1];
                if ((nextPoint.color == 0) && (mapLayout[nextPoint.x, nextPoint.y]))
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.y - 1 > 0)
            {
                nextPoint = pointArray[processPoint.x, processPoint.y - 1];
                if ((nextPoint.color == 0) && (mapLayout[nextPoint.x, nextPoint.y]))
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
        }
        return cavernSize;
    }

    private int FillCavernWithDiagonals(FloodPoint[,] pointArray, bool[,] mapLayout, FloodPoint startPoint)
    {
        // Map Layout array: TRUE means the tile is ground, and thus should be filled
        int cavernSize = 0;
        Queue<FloodPoint> pointsToFill = new Queue<FloodPoint>();
        pointsToFill.Enqueue(startPoint);
        FloodPoint processPoint;
        FloodPoint nextPoint;

        while (pointsToFill.Count > 0)
        {
            cavernSize++;
            processPoint = pointsToFill.Dequeue();
            if (!mapLayout[processPoint.x, processPoint.y]) continue; // Ignore walls.

            if (processPoint.x + 1 < columns - 1 && processPoint.y + 1 < rows - 1) // Northeast
            {
                nextPoint = pointArray[processPoint.x + 1, processPoint.y + 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }

            if (processPoint.x - 1 > 0 && processPoint.y + 1 < rows - 1) // Northwest
            {
                nextPoint = pointArray[processPoint.x - 1, processPoint.y + 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }

            if (processPoint.x - 1 > 0 && processPoint.y - 1 > 0) // Southwest
            {
                nextPoint = pointArray[processPoint.x - 1, processPoint.y - 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }

            if (processPoint.x + 1 < columns - 1 && processPoint.y - 1 > 0) // Southeast
            {
                nextPoint = pointArray[processPoint.x + 1, processPoint.y - 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }

            if (processPoint.x + 1 < columns - 1) // East
            {
                nextPoint = pointArray[processPoint.x + 1, processPoint.y];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.x - 1 > 0) // West
            {
                nextPoint = pointArray[processPoint.x - 1, processPoint.y];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.y + 1 < rows - 1) // North
            {
                nextPoint = pointArray[processPoint.x, processPoint.y + 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
            if (processPoint.y - 1 > 0) // South
            {
                nextPoint = pointArray[processPoint.x, processPoint.y - 1];
                if (nextPoint.color == 0 && mapLayout[nextPoint.x, nextPoint.y])
                {
                    nextPoint.color = startPoint.color;
                    pointsToFill.Enqueue(nextPoint);
                }
            }
        }
        return cavernSize;
    }

    public class FloodPoint
    {
        public int x;
        public int y;
        public int color;

        public FloodPoint(int startX, int startY, int startColor)
        {
            x = startX;
            y = startY;
            color = startColor;
        }
    }

    public Area GetArea(MapTileData mtd)
    {
        Area retrieve = null;
        int areaID = CheckMTDArea(mtd);
        try { retrieve = areaDictionary[areaID]; }
        catch (Exception e)
        {
            //Debug.Log("Area not found in dictionary for " + mtd.pos + ", area: " + CheckMTDArea(mtd) + " floor type " + dungeonLevelData.layoutType + " Columns: " + columns + " Rows: " + rows + " on floor " + floor + " " + mtd.CheckTag(LocationTags.SECRET));
            Debug.Log(e);
        }
        return retrieve;
    }

    public Area GetAreaByID(int ID)
    {
        Area retrieve = null;
        try { retrieve = areaDictionary[ID]; }
        catch (Exception e)
        {
            //Debug.Log("Area not found in dictionary for " + ID);
            Debug.Log(e);
        }
        return retrieve;
    }

    public bool[,] CreateMapTileArrayFromMap()
    {
        bool[,] mapTileArray = new bool[columns, rows];
        // TRUE is ground
        // FALSE is anything else

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {                
                if (mapArray[x, y].tileType == TileTypes.GROUND)
                {
                    mapTileArray[x, y] = true;
                }
                else
                {
                    mapTileArray[x, y] = false;
                }
            }
        }

        return mapTileArray;
    }

    public FloodPoint[,] CreateFloodPointArray()
    {
        FloodPoint[,] floodPointArray = new FloodPoint[columns, rows];
        // TRUE is ground
        // FALSE is anything else

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                floodPointArray[x,y] = new FloodPoint(x,y,0);
            }
        }

        return floodPointArray;
    }

    public bool FloodFillToRemoveCutoffs(bool alterMap = false, bool returnIfExceededMaxConversions = true)
    {
        FloodPoint[,] pointArray = CreateFloodPointArray();
        bool[,] mapTileArray = CreateMapTileArrayFromMap();

        // Now flood fill check.
        int startCheckX = 0;
        int startCheckY = 0;
        int tries = 0;

        // Find a simple ground tile.
        MapTileData checkMtd = mapArray[startCheckX, startCheckY];
        while (!mapTileArray[startCheckX, startCheckY] && tries < 10000)
        {
            tries++;
            startCheckX = UnityEngine.Random.Range(0, columns);
            startCheckY = UnityEngine.Random.Range(0, rows);
            checkMtd = mapArray[startCheckX, startCheckY];
        }
        if (tries >= 10000)
        {
            Debug.Log("Cavern fill error.");
            return false;
        }

        return ActualFloodFill(checkMtd, mapTileArray, pointArray, alterMap, returnIfExceededMaxConversions);
    }

    public bool FloodFillToSeeIfGivenTileIsConnectedToBiggestCavern(MapTileData startTile)
    {
        FloodPoint[,] pointArray = CreateFloodPointArray();
        bool[,] mapTileArray = CreateMapTileArrayFromMap();
        return ActualFloodFill(startTile, mapTileArray, pointArray, false, true, true);
    }

    bool ActualFloodFill(MapTileData startTile, bool[,] mapTileArray, FloodPoint[,] pointArray, bool alterMap = false, bool returnIfExceededMaxConversions = true, bool scanToSeeIfTileIsInBiggestCavern = false)
    {        

        int startCheckX = (int)startTile.pos.x;
        int startCheckY = (int)startTile.pos.y;

        if (alterMap)
        {
            mapTileArray = RemoveMapDiagonals(mapTileArray, pointArray);
        }


        pointArray[startCheckX, startCheckY].color = 1; // 1 is the first cavern.
        Dictionary<int, int> cavernSizes = new Dictionary<int, int>();
        int cavernColorIndex = 1;
        FloodPoint startPoint = pointArray[startCheckX, startCheckY];

        int cavernSize = FillCavernWithDiagonals(pointArray, mapTileArray, startPoint);
        cavernSizes.Add(cavernColorIndex, cavernSize);
        cavernColorIndex++;

        bool filledAllCaverns = false;

        while (!filledAllCaverns)
        {
            bool foundCavern = false;
            for (int x = 0; x <= columns - 1; x++)
            {
                for (int y = 0; y <= rows - 1; y++)
                {
                    if (mapTileArray[x, y] && pointArray[x, y].color == 0)
                    {
                        // This is a ground space that has not been checked.
                        startPoint = pointArray[x, y];
                        startPoint.color = cavernColorIndex;
                        foundCavern = true;
                        break;
                    }
                    if (foundCavern) break;
                }
                if (foundCavern) break;
            }
            if (!foundCavern)
            {
                filledAllCaverns = true;
            }
            else
            {
                int size = FillCavernWithDiagonals(pointArray, mapTileArray, startPoint);
                cavernSizes.Add(cavernColorIndex, size);
                cavernColorIndex++;
            }
        }

        //Debug.Log(cavernSizes.Values.Count + " " + dungeonLevelData.layoutType + " " + floor);

        int biggestCavernColorIndex = -1;
        int biggestNumber = 0;

        foreach (int colorIndex in cavernSizes.Keys)
        {
            if (cavernSizes[colorIndex] > biggestNumber)
            {
                biggestCavernColorIndex = colorIndex;
                biggestNumber = cavernSizes[colorIndex];
            }
        }

        if (scanToSeeIfTileIsInBiggestCavern)
        {
            if (pointArray[startCheckX, startCheckY].color != biggestCavernColorIndex)
            {
                if (Debug.isDebugBuild) Debug.Log("Uh oh! " + startCheckX + "," + startCheckY + " is not in the biggest cavern in map " + floor + " " + GetName() +", maybe it is cut off?");
                return false;
            }
            return true;
        }

        //Debug.Log("Biggest cavern is " + biggestCavernColorIndex + " " + biggestNumber + " floor " + floor);

        MapTileData mtd;
        int conversions = 0;
        int maxConversions = 20;

        if (dungeonLevelData.layoutType == DungeonFloorTypes.BSPROOMS)
        {
            maxConversions = 200;
        }

        for (int x = 0; x <= columns - 1; x++)
        {
            for (int y = 0; y <= rows - 1; y++)
            {
                if (!mapTileArray[x, y]) continue; // Walls are unrelated to this.

                // For debugging.
                mtd = mapArray[x, y];
                mtd.caveColor = pointArray[x, y].color;

                if (mtd.caveColor != biggestCavernColorIndex)
                {
                    if (!mtd.CheckTag(LocationTags.LAVA))
                    {
                        conversions++;
                    }
                    mtd.ChangeTileType(TileTypes.WALL, mgd);
                }

                if (conversions >= maxConversions && returnIfExceededMaxConversions)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool[,] DoCellAutomataStep(bool[,] oldMap, int minNeighbors, int maxNeighbors)
    {
        int neighbors = 0;
        bool[,] newMap = new bool[columns, rows];
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                neighbors = CountAliveNeighbors(oldMap, x, y);
                if (oldMap[x, y]) // Currently alive
                {
                    if (neighbors < minNeighbors) // If alive: Must have at least 3 neighbors, or die.
                    {
                        newMap[x, y] = false;
                    }
                    else
                    {
                        newMap[x, y] = true;
                    }
                }
                else
                {
                    if (neighbors > maxNeighbors) // If dead: If you have at least maxNeighbors, you come to life.
                    {
                        newMap[x, y] = true;
                    }
                    else
                    {
                        newMap[x, y] = false;
                    }
                }
            }
        }

        return newMap;
    }

    private int CountAliveNeighbors(bool[,] checkMap, int x, int y)
    {
        int neighbors = 0;
        for (int a = x - 1; a <= x + 1; a++)
        {
            if (a < 0 || a >= columns)
            {
                //neighbors++; Don't count map edges as live neighbors.
                continue;
            }
            for (int b = y - 1; b <= y + 1; b++)
            {
                if (b < 0 || b >= rows)
                {
                    //neighbors++; Don't count map edges as live neighbors.
                    continue;
                }
                if (a == 0 && b == 0) continue;
                if (checkMap[a, b]) neighbors++;
            }
        }

        return neighbors;
    }

    public void DigRandomVolcanoTunnels()
    {
        int numTunnels = 4;
        MapTileData mtd;

        for (int x = 0; x < numTunnels; x++)
        {
            int startX = 0;
            int startY = 0;
            int endX = 0;
            int endY = 0;

            //endX = UnityEngine.Random.Range(columns - 6, columns - 2);

            Directions dirToUse = Directions.NEUTRAL;

            // 0 = Left, Bottom
            // 1 = Right, Bottom
            // 2 = Left, Top
            // 3 = Right, Top

            if (x == 0 || x == 3)
            {
                // Start on left side
                startX = UnityEngine.Random.Range(2, 4);
            }
            else
            {
                // Start on right
                startX = UnityEngine.Random.Range(columns - 3, columns - 2);
            }

            if (x == 0 || x == 1)
            {
                // Start at the bottom
                startY = UnityEngine.Random.Range(2, 4);
            }
            else
            {
                // Start at top
                startY = UnityEngine.Random.Range(rows - 3, rows - 2);
            }

            // Start Top/Left? Can end in Top/Right, or Bottom/Left

            if (startX < 10 && startY > 10)
            {
                //if (UnityEngine.Random.Range(0,2) == 0)
                {
                    // Top right
                    endX = UnityEngine.Random.Range(columns - 3, columns - 2);
                    endY = UnityEngine.Random.Range(rows - 3, rows - 2);
                    dirToUse = Directions.EAST;
                }
                /* else
                {
                    // Bottom Left
                    dirToUse = Directions.SOUTH;
                    endX = UnityEngine.Random.Range(2,5); 
                    endY = UnityEngine.Random.Range(2,5); 
                } */
            }

            // Bottom right? Can go top/right, or bottom/left
            if (startX > 10 && startY < 10)
            {
                /* if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // Top right
                    dirToUse = Directions.NORTH;
                    endX = UnityEngine.Random.Range(columns - 4, columns - 1);
                    endY = UnityEngine.Random.Range(rows - 4, rows - 1);
                }
                else */
                {
                    // Bottom Left
                    dirToUse = Directions.EAST;
                    endX = UnityEngine.Random.Range(2, 4);
                    endY = UnityEngine.Random.Range(2, 4);
                }
            }

            // Start Top/Right? can end Bottom/Right, or Top/Left

            if (startX > 10 && startY > 10)
            {
                /* if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // Top left
                    dirToUse = Directions.WEST;
                    endX = UnityEngine.Random.Range(2,5);
                    endY = UnityEngine.Random.Range(rows - 4, rows - 1);
                }
                else */
                {
                    // Bottom Right
                    dirToUse = Directions.SOUTH;
                    endX = UnityEngine.Random.Range(columns - 3, columns - 2);
                    endY = UnityEngine.Random.Range(2, 4);
                }
            }

            // Bottom left? Can go bottom/right, or top/left
            if (startX < 10 && startY < 10)
            {
                //if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // Top left
                    dirToUse = Directions.NORTH;
                    endX = UnityEngine.Random.Range(2, 4);
                    endY = UnityEngine.Random.Range(rows - 3, rows - 2);
                }
                /* else
                {
                    // Bottom Right
                    dirToUse = Directions.EAST;
                    endX = UnityEngine.Random.Range(columns - 4, columns - 1);
                    endY = UnityEngine.Random.Range(2, 5);
                } */
            }

            CustomAlgorithms.GetPointsOnLineNoGarbage(new Vector2(startX, startY), new Vector2(endX, endY));

            for (int p = 0; p < CustomAlgorithms.numPointsInLineArray; p++)
            {

                switch (dirToUse)
                {
                    case Directions.NORTH:
                    case Directions.SOUTH:
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            CustomAlgorithms.pointsOnLine[p].x += UnityEngine.Random.Range(-1, 2);
                        }
                        break;
                    case Directions.EAST:
                    case Directions.WEST:
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            CustomAlgorithms.pointsOnLine[p].y += UnityEngine.Random.Range(-1, 2);
                        }
                        break;
                }


                mtd = GetTile(CustomAlgorithms.pointsOnLine[p]);

                if (CustomAlgorithms.pointsOnLine[p].x >= (columns * 0.35f) && CustomAlgorithms.pointsOnLine[p].x <= (columns * 0.65f)
                    && CustomAlgorithms.pointsOnLine[p].y >= (rows * 0.35f) && CustomAlgorithms.pointsOnLine[p].y <= (rows * 0.65f))
                {
                    // Don't dig through the middle please
                    continue;
                }

                mtd.ChangeTileType(TileTypes.GROUND, mgd);
            }
        }
    }

    public virtual bool BuildRandomMap(ItemWorldMetaData itemWorldProperties)
    {
        // We are now setting map ID here, but only incrementing and adding to dict when we have known good map.
        mapAreaID = MapMasterScript.singletonMMS.mapAreaIDAssigner;
        if (MapMasterScript.singletonMMS.mapAreaIDAssigner == 0)
        {
            Debug.Log("Assigned a map with ID of 0 - this is not correct.");
        }

        areaDictionary.Clear();
        // Now generate rooms

        CreatePossibleRoomTemplateDictionaries();

        localPointArray = MapMasterScript.pointsForRoomDicts[floor];
        localPointArrayMaxX = MapMasterScript.pointsForRoomDictsMaxSizes[floor].x;
        localPointArrayMaxY = MapMasterScript.pointsForRoomDictsMaxSizes[floor].y;
        localEnforcedRoomDict = MapMasterScript.masterDictOfEnforcedMapRoomDicts[floor];
        localUnenforcedRoomDict = MapMasterScript.masterDictOfUnenforcedMapRoomDicts[floor];

        //Debug.Log("Start core map generation " + floor);

        bool roomSuccess = true;

        switch (dungeonLevelData.layoutType)
        {
            case DungeonFloorTypes.STANDARD:
            case DungeonFloorTypes.HIVES:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsStandard();
                if (!roomSuccess)
                {
#if UNITY_EDITOR
                    //Debug.Log("Failed to build+place rooms " + floor + " " + mgd.minRoomSize + " " + mgd.maxRoomSize + " " + mapRooms.Count + " " + numPriorityRoomsBuilt + " " + priorityRoomsBuilt);
#endif
                    return false;
                }
                roomSuccess = BuildAndConnectCorridorsStandard();
                if (!roomSuccess)
                {
#if UNITY_EDITOR
                    //Debug.Log("Failed to connect corridors " + floor + " corridors in map: " + mapCorridors.Count);
#endif
                    return false;
                }

                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }
                break;
            case DungeonFloorTypes.ISLANDS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsStandard();
                if (!roomSuccess) return false;
                foreach (Room rm in mapRooms)
                {
                    rm.reachable = true;
                }
                roomSuccess = BuildAndConnectCorridorsStandard();
                if (!roomSuccess) return false;
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        if ((x == 0) || (x == columns - 1) || (y == 0) || (y == rows - 1))
                        {
                            mapArray[x, y].ChangeTileType(TileTypes.WALL, mgd);
                        }
                    }
                }
                break;
            case DungeonFloorTypes.SPECIAL:
                FillMapWithNothing(TileTypes.WALL);
                Room special = CreateRoomFromTemplate(1, 1, dungeonLevelData.specialRoomTemplate);
                BuildRoomTiles(special);
                AddAreaToDictionary(special);
                //areaDictionary.Add(special.areaID, special);
                mapRooms.Add(special);
                break;
            case DungeonFloorTypes.RUINS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsRuins();
                if (!roomSuccess)
                {
                    //Debug.Log("Failed creating ruins rooms.");
                    return false;
                }
                roomSuccess = BuildAndConnectCorridorsStandard();
                if (!roomSuccess)
                {
                    //Debug.Log("Failed creating ruins corridors.");
                    return false;
                }
                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }

                List<Room> roomRemover = new List<Room>();
                List<Corridor> corridorRemover = new List<Corridor>();
                foreach (Room areaC in mapRooms)
                {
                    if (areaC.areaID < 0)
                    {
                        continue;
                    }
                    if (areaC.edgeTiles.Count == 0)
                    {
                        Debug.Log("Floor " + floor + " area has no edge tiles, " + areaC.internalTiles.Count + " internals");
                    }
                    if (areaC.internalTiles.Count == 0)
                    {
                        roomRemover.Add(areaC);
                        //Debug.Log(floor + " had room with no internal tiles after corridor gen " + areaC.origin + " " + areaC.size);
                        foreach (Corridor cor in mapCorridors)
                        {
                            cor.connections.Remove(areaC);
                        }
                        foreach (Area area in mapRooms)
                        {
                            area.connections.Remove(areaC);
                        }
                    }
                }
                foreach (Corridor areaC in mapCorridors)
                {
                    if (areaC.areaID < 0)
                    {
                        continue;
                    }
                    if (areaC.internalTiles.Count == 0)
                    {
                        corridorRemover.Add(areaC);
                        //Debug.Log(floor + " had corridor with no internal tiles after corridor gen " + areaC.origin + " " + areaC.size);
                        foreach (Corridor cor in mapCorridors)
                        {
                            cor.connections.Remove(areaC);
                        }
                        foreach (Area area in mapRooms)
                        {
                            area.connections.Remove(areaC);
                        }
                    }
                }
                foreach (Room rem in roomRemover)
                {
                    mapRooms.Remove(rem);
                    areaDictionary.Remove(rem.areaID);
                }
                foreach (Corridor cor in corridorRemover)
                {
                    mapCorridors.Remove(cor);
                    areaDictionary.Remove(cor.areaID);
                }

                break;
            case DungeonFloorTypes.STANDARDNOHALLS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsStandardNoHalls();
                if (!roomSuccess) return false;

                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }
                break;
            case DungeonFloorTypes.FUTURENOHALLS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsFutureNoHalls();
                if (!roomSuccess) return false;
                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }
                break;
            case DungeonFloorTypes.CAVEROOMS:
                FillMapWithNothing(TileTypes.WALL);
                List<RoomTemplate> backupPriorityTemplates = new List<RoomTemplate>();
                foreach (RoomTemplate rt in dungeonLevelData.priorityTemplates)
                {
                    backupPriorityTemplates.Add(rt);
                }
                roomSuccess = BuildAndPlaceRoomsStandard();
                dungeonLevelData.priorityTemplates.Clear();
                foreach (RoomTemplate rt in backupPriorityTemplates)
                {
                    dungeonLevelData.priorityTemplates.Add(rt);
                }

                if (!roomSuccess) return false;
                Area fillArea = areaDictionary[-777];
                fillArea.InitializeLists();
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        MapTileData mtd = mapArray[x, y];
                        if ((x == 0) || (y == 0) || (x == columns - 1) || (y == rows - 1))
                        {
                            mtd.ChangeTileType(TileTypes.WALL, mgd);
                            continue;
                        }
                        if (mtd.tileType == TileTypes.NOTHING)
                        {
                            TryFillGroundTile(mtd);
                        }
                    }
                }

                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }

                List<Room> rroomRemover = new List<Room>();
                foreach (Room areaC in mapRooms)
                {
                    if (areaC.areaID < 0)
                    {
                        continue;
                    }
                    if (areaC.internalTiles.Count == 0)
                    {
                        rroomRemover.Add(areaC);
                        //Debug.Log(floor + " had room with no internal tiles after corridor gen " + areaC.origin + " " + areaC.size);
                        foreach (Corridor cor in mapCorridors)
                        {
                            cor.connections.Remove(areaC);
                        }
                        foreach (Area area in mapRooms)
                        {
                            area.connections.Remove(areaC);
                        }
                    }
                }
                foreach (Room rem in rroomRemover)
                {
                    mapRooms.Remove(rem);
                    areaDictionary.Remove(rem.areaID);
                }

                break;
            case DungeonFloorTypes.CAVE:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAndPlaceRoomsStandard();
                if (!roomSuccess) return false;
                BuildCaveEdgeJaggies();
                FillUnusedCaveTiles();
                break;
            case DungeonFloorTypes.MAZE:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildMaze();
                if (!roomSuccess) return false;

                for (int x = 1; x < columns - 1; x++)
                {
                    for (int y = 1; y < rows - 1; y++)
                    {
                        if ((mapArray[x, y].tileType == TileTypes.WALL) && (UnityEngine.Random.Range(0, 1f) < dungeonLevelData.chanceToOpenWalls))
                        {
                            mapArray[x, y].ChangeTileType(TileTypes.GROUND, mgd);
                            mapArray[x, y].AddTag(LocationTags.DUGOUT);
                        }
                    }
                }

                break;
            case DungeonFloorTypes.MAZEROOMS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildMaze();
                if (!roomSuccess) return false;
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        mapArray[x, y].AddMapTag(MapGenerationTags.MAZEFILL);
                    }
                }
                roomSuccess = BuildAndPlaceRoomsStandard();
                if (!roomSuccess) return false;
                roomSuccess = BuildAndConnectCorridorsStandard();
                if (!roomSuccess)
                {
                    return false;
                }

                if (dungeonLevelData.extraRivers > 0)
                {
                    for (int i = 0; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                    }
                }

                break;
            case DungeonFloorTypes.VOLCANO:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAutomataCave();
                if (!roomSuccess) return false;

                DungeonGenerationAlgorithms.DoVolcanoInitialBuild(this);

                break;
            case DungeonFloorTypes.LAKE:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAutomataCave();
                if (!roomSuccess) return false;
                roomSuccess = BuildAndPlaceLakeRooms();
                if (!roomSuccess) return false;
                break;
            case DungeonFloorTypes.CIRCLES:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = DungeonGenerationAlgorithms.BuildCircleDungeon(this);
                if (!roomSuccess) return false;

                if (dungeonLevelData.extraRivers > 0)
                {
                    BuildRiver();
                    BuildMudPatches(6);
                    for (int i = 1; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                        BuildMudPatches(3);
                    }
                }

                break;
            case DungeonFloorTypes.BSPROOMS:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = DungeonGenerationAlgorithms.BuildBSPLayout(this);
                if (!roomSuccess) return false;
                if (dungeonLevelData.extraRivers > 0)
                {
                    BuildRiver();
                    BuildMudPatches(6);
                    for (int i = 1; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                        BuildMudPatches(3);
                    }
                }
                break;
            case DungeonFloorTypes.BANDITDUNGEON_HUB:
                FillMapWithNothing(TileTypes.WALL);
                // I messed up a little bit when I configured the M_BDH class
                Map_BanditDungeonHub mbdh = this as Map_BanditDungeonHub;
                roomSuccess = mbdh.BuildBanditDungeonHub();
                if (!roomSuccess) return false;
                break;
            case DungeonFloorTypes.KEEP:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = DLC_DungeonGenerationAlgorithms.BuildKeepMap(this);
                if (!roomSuccess) return false;
                /* string debugger = "";
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        if (mapArray[x,y].tileType == TileTypes.GROUND)
                        {
                            debugger += ".";
                        }
                        else
                        {
                            debugger += "X";
                        }
                    }
                    debugger += "\n";
                }
                Debug.Log(debugger); */
                break;
            case DungeonFloorTypes.AUTOCAVE:
                FillMapWithNothing(TileTypes.WALL);
                roomSuccess = BuildAutomataCave();
                if (!roomSuccess) return false;

                if (dungeonLevelData.extraRivers > 0)
                {
                    BuildRiver();
                    BuildMudPatches(6);
                    for (int i = 1; i < dungeonLevelData.extraRivers; i++)
                    {
                        BuildRiver();
                        BuildMudPatches(3);
                    }
                }

                break;
        }

        TryConvertCenterOfMapToWater();

        //Debug.Log("Finish core generation " + floor);

        CreateLavaPools();

        AssignTileAreaIDsToRoomsAndCorridors();

        // Now, at random, pick out some tiles and convert them to ground tiles for variation.

        ConvertWallTilesToGroundAtRandom();

        // Pick a random room to add the stairs down.
        
        int minMonsters = dungeonLevelData.minMonsters;
        int maxMonsters = dungeonLevelData.maxMonsters;

        if (IsItemWorld())
        {
            int numExtras = UnityEngine.Random.Range(1, 4);
            minMonsters += numExtras;
            maxMonsters += numExtras;
        }

        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_DENSE])
        {
            minMonsters += (int)(minMonsters * 0.4);
            maxMonsters += (int)(minMonsters * 0.4);
        }

        if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
        {
            minMonsters = (int)(minMonsters * 1.2f);
            maxMonsters = (int)(minMonsters * 1.2f);
            if (GameStartData.NewGamePlus == 2)
            {
                minMonsters += 2;
                maxMonsters += 2;
            }
        }

        TryCreateBreathePillars();

        // Water pools? Let's try it.
        TryCreateRandomWaterPools();

        if (!string.IsNullOrEmpty(dungeonLevelData.script_preBeautify))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DungeonGenerationScripts), dungeonLevelData.script_preBeautify);
            if (runscript != null)
            {
                object[] paramList = new object[1];
                paramList[0] = this;
                runscript.Invoke(null, paramList);
            }
        }

        AddStumpsIfNecessary();

        if (dungeonLevelData.layoutType == DungeonFloorTypes.BSPROOMS)
        {
            //float groundPercentage = GetPercentageOfMapAsGround();

            // Need to do this for BSPROOMS before stairs, in case of ground 'pockets' 3x3 that are technically
            // valid for stairs, but not accessible elsewhere in the level.
            FloodFillToRemoveCutoffs(false, false);

            // #todo - Why are the wheels falling off with BSP? Oh well just brute force it for now.

            float groundPercentage = GetPercentageOfMapAsGround();

            int minGroundFromMap = dungeonLevelData.GetMetaData("mingroundpercent");
            float localPercent = 0.15f;
            if (minGroundFromMap != 0)
            {
                localPercent = minGroundFromMap / 100f;
            }

            if (groundPercentage <= localPercent) // Something must have failed in map gen. Time to bail
            {
#if UNITY_EDITOR
                //Debug.Log("Wheels fell off in BSP generation: " + groundPercentage);
#endif
                return false;
            }
        }

        if (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)
        {
            if (dungeonLevelData.GetMetaData("dont_create_startarea") != 1)
            {
                CreateStairsInMap();
                AssignHeroStartAreaAndTile();
            }

            if (dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS)
            {
                CreateItemDreamToolsIfNeeded();
            }

            foreach (int id in areaDictionary.Keys)
            {
                Area ar = areaDictionary[id];
                if (id != ar.areaID)
                {
                    Debug.Log("Generated mismatch on floor " + floor + " corridor? " + ar.isCorridor + " expected: " + id + " vs " + ar.areaID);
                }
            }

            if (dungeonLevelData.tileVisualSet == TileSet.EARTH && dungeonLevelData.specialRoomTemplate == null)
            {
                CreateRandomIsolatedWallTiles();
            }
        } // End non-special generation.


        SetMapBoundVisuals();

        //Make sure we have ground in the right places so we can spawn bads and build
        //secret rooms
        PrepareGroundTiles();

        CorrectForDiagonalMovement();

        if (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL || (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.gmsSingleton.ReadTempGameData("dream_map_from_template") == 1))
        {
            if (dungeonLevelData.layoutType != DungeonFloorTypes.KEEP && dungeonLevelData.GetMetaData("no_floodfill_check") == 0)
            {
                bool mapStateIsGood = FloodFillToRemoveCutoffs(false);
                if (!mapStateIsGood)
                {
#if UNITY_EDITOR
                    //Debug.Log("Too many cutoffs for " + dungeonLevelData.layoutType + " " + floor + " - returning...");
#endif
                    return false;
                }
            }
            if (dungeonLevelData.GetMetaData("no_initial_monster_seed") != 1)
            {
                SeedMapWithMonsters(minMonsters, maxMonsters, itemWorldProperties);
            }            
        }
        // Don't do floodfill check for circles due to weird layouts ;)
        if (dungeonLevelData.layoutType == DungeonFloorTypes.CIRCLES)
        {
            SeedMapWithMonsters(minMonsters, maxMonsters, itemWorldProperties);
        }

        if (dungeonLevelData.maxSecretAreas > 0)
        {
            BuildAndPlaceSecretRooms();
        }

        //Debug.Log("Done correcting diagonal-only movement.");

        AddGrassLayer1ToMap();
        AddElectricTilesToMap();
        AddWallChunkDecor();

        RemoveLavaAndWaterFromHoles();

        AddGrassLayer2ToMapAndBeautifyAll();
        AddDecorAndFlowersAndTileDirections();

        //After adding tags for danger zones and other destructibles, add those now.
        AddEnvironmentDestructiblesToMap(); // Lava, water, mud

        AddObjectsFromRoomTemplates(itemWorldProperties); // This should be deprecated eventually

        if (dungeonLevelData.noSpawner)
        {
            spawnerAlive = false;
        }

        if (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)
        {
            // Spawn one monster spawner.

            if (IsMainPath() && !SharaModeStuff.IsSharaModeActive())
            {
                AddJournalEntriesToMap();
            }

            if (maxMonsters > 0 && !dungeonLevelData.noSpawner && floor >= 2)
            {
                if (!AddPandoraBoxToMap())
                {
                    return false; // Something screwed up with map gen.
                }
            }
            else
            {
                spawnerAlive = false;
            }

            AddBreakablesFountainsAndSparklesToMap();
            AddFungusToMap();

#region DEBUG STUFF
            // DEBUG ONLY
            if (unfriendlyMonsterCount > 0)
            {
                int area = 0;
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        MapTileData mtd = mapArray[x, y];
                        if (mtd.tileType == TileTypes.GROUND)
                        {
                            area++;
                        }
                    }
                }

                float density = (float)(unfriendlyMonsterCount / (float)area);
                //Debug.Log("Floor " + floor + " layout " + dungeonLevelData.layoutType + " Area " + area + " Density " + density);
            }
#endregion

            // Add crystals to item world

            if (IsItemWorld())
            {
                AddCrystalsToItemWorld();
            }
        }

        if (floor == MapMasterScript.PRE_BOSS3_WALKTOBOSS_FLOOR)
        {
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (x == 0 || y == 0 || x == 1 || y == 1 || x == columns - 1 || x == columns - 2 || y == rows - 1 || y == rows - 2)
                    {
                        mapArray[x, y].ChangeTileType(TileTypes.GROUND, mgd);
                        mapArray[x, y].SetTileVisualType(VisualTileType.GROUND);
                    }
                }
            }
        }

        if (dungeonLevelData.layoutType == DungeonFloorTypes.BSPROOMS)
        {
            DungeonGenerationAlgorithms.CleanUpBSPMap(this);
        }

        RefreshTerrainVisualIndices();

        DungeonGenerationAlgorithms.RunPostMapBuildScripts(this);

        return true;
    }

    protected void PrepareGroundTiles()
    {
        MapTileData checkTile;
        for (int x = 0; x < columns - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                checkTile = mapArray[x, y];
                if (checkTile.CheckTag(LocationTags.WATER) ||
                    checkTile.CheckTag(LocationTags.ISLANDSWATER) ||
                    checkTile.CheckTag(LocationTags.LAVA) ||
                    checkTile.CheckTag(LocationTags.MUD) ||
                    checkTile.CheckTag(LocationTags.ELECTRIC))
                {
                    checkTile.ChangeTileType(TileTypes.GROUND, mgd);

                }
            }
        }
    }

    public MapTileData GetRandomEmptyTileForMapGen()
    {
        int x = 0;
        int y = 0;
        bool valid = false;
        MapTileData mtd = null;
        int attempts = 0;
        while (!valid)
        {
            attempts++;
            if (attempts > 5000)
            {
#if UNITY_EDITOR
                Debug.Log("No possible empty tiles?! " + floor);
#endif
                return null;
            }
            x = UnityEngine.Random.Range(1, columns - 2);
            y = UnityEngine.Random.Range(1, rows - 2);
            mtd = mapArray[x, y];
            if (mtd.tileType == TileTypes.GROUND && !mtd.CheckTag(LocationTags.DUGOUT))
            {
                if (!mtd.CheckMapTag(MapGenerationTags.EDGETILE) && !mtd.CheckTag(LocationTags.WATER) && !mtd.CheckTag(LocationTags.LAVA) && !mtd.CheckTag(LocationTags.ELECTRIC))
                {
                    if (mtd.GetAllActors().Count == 0 && !mtd.playerCollidable)
                    {
                        return mtd;
                    }
                }
            }
        }
        return mtd;
    }

    public Actor SpawnFountainInMap()
    {
        bool validTile = false;
        MapTileData check = null;
        int tries = 0;
        while ((!validTile) && (tries < 2500))
        {
            tries++;
            int x = UnityEngine.Random.Range(1, columns - 1);
            int y = UnityEngine.Random.Range(1, rows - 1);
            check = mapArray[x, y];
            if (CheckValidForDestructible(check))
            {
                if (!check.CheckActorRef("obj_regenfountain"))
                {
                    return CreateDestructibleInTile(check, "obj_regenfountain");
                }
                break;
            }
        }
        if (tries >= 2500)
        {
            Debug.Log("Fountain fail.");
        }
        return null;
    }

    public void PlaceItemAtRandom(Item itm)
    {
        bool validTile = false;
        MapTileData check = null;
        int tries = 0;

        while ((!validTile) && (tries < 2500))
        {
            tries++;
            int x = UnityEngine.Random.Range(1, columns - 1);
            int y = UnityEngine.Random.Range(1, rows - 1);
            check = mapArray[x, y];
            if ((check.tileType == TileTypes.GROUND) || ((UnityEngine.Random.Range(0, 1f) <= 0.25f) && (check.CheckTag(LocationTags.ISLANDSWATER))))
            {
                PlaceActor(itm, check);
                break;
            }
        }
        if (tries >= 2500)
        {
            Debug.Log("Food fail.");
        }
    }

    public void SpawnFoodInMap()
    {
        ActorTable foodTable = LootGeneratorScript.GetLootTable("food_and_meals");
        string foodRef = foodTable.GetRandomActorRefNonWeighted();

        bool allowRosePetals = MetaProgressScript.RosePetalsAllowed();
        while (!allowRosePetals && foodRef == "spice_rosepetals")
        {
            foodRef = foodTable.GetRandomActorRefNonWeighted();
        }

        Item food = LootGeneratorScript.CreateItemFromTemplateRef(foodRef, challengeRating, 0f, false);
        PlaceItemAtRandom(food);
        if (IsItemWorld())
        {
            food.SetActorData("fromdreammob", 1);
        }
    }


    public Stairs SpawnStairs(bool stairsUp, int pointsToFloor = -1, MapTileData forceTile = null)
    {
        mapRooms.Shuffle();
        int mapRoomIndex = 0;
        bool validForStairs = false;
        int mtdIndex = 0;
        MapTileData mtdForStairs = null;
        Room stairsRoom = null;

        stairsRoom = mapRooms[mapRoomIndex];

        while (stairsRoom.internalTiles.Count == 0)
        {
            mapRoomIndex++;
            if (mapRoomIndex >= mapRooms.Count)
            {
                //Debug.Log("Error 1: Every room is invalid for stairs?! Floor: " + floor + " Stairs Up? " + stairsUp);
                //Debug.Log(mapRooms.Count);
                return null;
                break;
            }
            stairsRoom = mapRooms[mapRoomIndex];
        }

        stairsRoom.internalTiles.Shuffle();
        try { mtdForStairs = stairsRoom.internalTiles[0]; }
        catch (Exception e)
        {
            Debug.LogError(e + "\nWARNING: Couldn't find stairs for " + floor + " " + dungeonLevelData.layoutType + " " + dungeonLevelData.customName);
        }
        // Don't put stairs on collidable tiles.

        validForStairs = CheckValidForStairs(mtdForStairs);
        mtdIndex = 1;

        while (!validForStairs)
        {
            if (mtdIndex >= stairsRoom.internalTiles.Count)
            {
                mapRoomIndex++;
                if (mapRoomIndex >= mapRooms.Count)
                {
                    Debug.Log("Error 2: Every room is invalid for stairs?! " + floor + " Up? " + stairsUp);
                    break;
                }
                //Debug.Log("Switched rooms.");
                stairsRoom = mapRooms[mapRoomIndex];                
                while (stairsRoom.internalTiles.Count == 0)
                {
                    mapRoomIndex++;
                    if (mapRoomIndex >= mapRooms.Count)
                    {
                        Debug.Log("Every room is invalid for stairs?! " + floor + " Up? " + stairsUp);
                        break;
                    }
                    stairsRoom = mapRooms[mapRoomIndex];
                }
                stairsRoom.internalTiles.Shuffle();
                mtdIndex = 0;
            }

            // We have a room selected.
            mtdForStairs = stairsRoom.internalTiles[mtdIndex];

            validForStairs = CheckValidForStairs(mtdForStairs);
            if (validForStairs)
            {
                break;
            }
            mtdIndex++;
        }

        //Merge note: it's possible to call this from the new function in the utility section of
        //Map_DragonRobot.cs
        Stairs nStairs = new Stairs();
        nStairs.dungeonFloor = floor;
        nStairs.SetUniqueIDAndAddToDict();

        if (nStairs.autoMove)
        {
            nStairs.prefab = "TransparentStairs";
        }
        else
        {
            if (stairsUp)
            {
                nStairs.displayName = "Stairway Down";
                nStairs.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsDown";
            }
            else
            {
                nStairs.displayName = "Stairway Up";
                nStairs.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsUp";
            }
        }

        if (forceTile != null)
        {
            mtdForStairs = forceTile;
        }

        nStairs.stairsUp = stairsUp;
        nStairs.SetSpawnPosXY((int)mtdForStairs.pos.x, (int)mtdForStairs.pos.y);
        nStairs.SetCurPos(mtdForStairs.pos);
        mtdForStairs.AddActor(nStairs);
        AddActorToMap(nStairs);
        mtdForStairs.RemoveTag(LocationTags.MUD);
        mtdForStairs.RemoveTag(LocationTags.LAVA);
        mtdForStairs.RemoveTag(LocationTags.ELECTRIC);
        mtdForStairs.ChangeTileType(TileTypes.GROUND, mgd);

        if (pointsToFloor != -1)
        {
            nStairs.pointsToFloor = pointsToFloor;
            nStairs.NewLocation = MapMasterScript.theDungeon.FindFloor(pointsToFloor);
            nStairs.newLocationID = nStairs.NewLocation.mapAreaID;
        }

        return nStairs;

    }

    public bool CheckValidForDestructible(MapTileData mtd)
    {
        if (mtd.tileType != TileTypes.GROUND)
        {
            return false;
        }
        if (mtd.CheckTag(LocationTags.DUGOUT))
        {
            return false;
        }
        if ((mtd.CheckTag(LocationTags.WATER)) || (mtd.CheckTag(LocationTags.LAVA)) || (mtd.CheckTag(LocationTags.ELECTRIC)) || (mtd.CheckTag(LocationTags.MUD)))
        {
            return false;
        }
        if (mtd.CheckTag(LocationTags.SOLIDTERRAIN))
        {
            return false;
        }
        if (mtd.IsCollidable(GameMasterScript.heroPCActor))
        {
            return false;
        }
        if (mtd.pos == heroStartTile)
        {
            return false;
        }
        if (mtd.GetStairsInTile() != null)
        {
            return false;
        }
        if (!mtd.IsEmpty()) return false;
        if (floor == 0)
        {
            if (Vector2.Distance(heroStartTile, mtd.pos) <= 3f)
            {
                return false;
            }
            if ((mtd.pos.y > 26f) || (mtd.pos.x <= 3f) || (mtd.pos.y <= 3f) || (mtd.pos.x > 26f))
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckValidForStairs(MapTileData mtd)
    {
        if (mtd.tileType != TileTypes.GROUND || mtd.CheckTag(LocationTags.SOLIDTERRAIN))
        {
            return false;
        }
        if (mtd.CheckTag(LocationTags.DUGOUT))
        {
            return false;
        }
        if (mtd.CheckTag(LocationTags.SECRET) || mtd.CheckTag(LocationTags.SECRETENTRANCE))
        {
            return false;
        }
        if ((mtd.CheckTag(LocationTags.WATER)) || (mtd.CheckTag(LocationTags.ELECTRIC)) || (mtd.CheckTag(LocationTags.LAVA)) || (mtd.CheckTag(LocationTags.MUD)))
        {
            return false;
        }
        if (mtd.IsCollidable(GameMasterScript.heroPCActor))
        {
            return false;
        }
        if (mtd.collidableActors)
        {
            return false;
        }
        if (!mtd.IsEmpty()) return false;

        if (mtd.GetStairsInTile() != null)
        {
            return false;
        }
        if (floor == 0)
        {
            if (Vector2.Distance(heroStartTile, mtd.pos) <= 3f)
            {
                return false;
            }
        }

        CustomAlgorithms.GetTilesAroundPoint(mtd.pos, 1, this);
        if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVE)
        {
            for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
            {
                if (CustomAlgorithms.tileBuffer[x].tileType != TileTypes.GROUND)
                {
                    return false;
                }
            }

            if ((mtd.pos.x < 4) || (mtd.pos.x >= columns - 4) || (mtd.pos.y < 4) || (mtd.pos.y >= rows - 4))
            {
                return false;
            }
        }
        for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
        {
            if (CustomAlgorithms.tileBuffer[x].GetStairsInTile() != null)
            {
                return false;
            }
        }

        // Sanity check. ANY ground surrounding?
        if (GetNumGroundTilesAroundPoint(mtd.pos) <= 2)
        {
            return false;
        }

        return true;
    }

    public int GetNumGroundTilesAroundPoint(Vector2 pos)
    {
        MapTileData mtd = mapArray[(int)pos.x, (int)pos.y];
        CustomAlgorithms.GetTilesAroundPoint(pos, 1, this);
        int groundCount = 0;
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND)
            {
                groundCount++;
            }
        }
        return groundCount;
    }

    public Monster CreateMonsterInTile(MapTileData tile)
    {
        if (dungeonLevelData.spawnTable == null)
        {
            Debug.Log("No spawn table on floor " + floor);
        }
        string monsterToSpawn = dungeonLevelData.spawnTable.GetRandomActorRef();
        Monster newMon = MonsterManagerScript.CreateMonster(monsterToSpawn, true, true, false, 0f, bonusRewards, false);
        newMon.dungeonFloor = floor;
        newMon.startAreaID = CheckMTDArea(tile);
        newMon.SetSpawnPosXY((int)tile.pos.x, (int)tile.pos.y);
        AddActorToLocation(tile.pos, newMon);
        AddActorToMap(newMon);
        return newMon;
    }

    // For some reason, the monster count gets desynced. It is much slower to iterate through every actor but we have to do it to be sure.
    public void RecountMonsters(bool ignoreKnockedOutMonsters = false)
    {
        unfriendlyMonsterCount = 0;
        Monster mn;
        for (int i = 0; i < actorsInMap.Count; i++)
        {
            if (actorsInMap[i].GetActorType() == ActorTypes.MONSTER)
            {
                mn = actorsInMap[i] as Monster;
                if (mn.myStats.IsAlive() && !mn.isInDeadQueue && (mn.actorfaction != Faction.PLAYER || mn.bufferedFaction != Faction.PLAYER))
                {
                    if (mn.actorRefName != "mon_fungalcolumn")
                    {
                        unfriendlyMonsterCount++;
                    }
                }
                if (mn.surpressTraits && mn.myStats.CheckHasStatusName("sleepvisual") && !ignoreKnockedOutMonsters)
                {
                    // Knocked out monsters also count?
                    unfriendlyMonsterCount++;
                }
            }
        }
        //Debug.Log(monsterCount + " monsters remaining in " + floor);
    }

    public NPC CreateNPCInTile(MapTileData checkTile, string npcRef)
    {
        NPC makeNPC = new NPC();
        makeNPC.CopyFromTemplateRef(npcRef);
        makeNPC.dungeonFloor = floor;
        PlaceActor(makeNPC, checkTile);
        return makeNPC;
    }

    public Destructible CreateDestructibleInTile(MapTileData checkTile, string dt, bool mysteryDungeon = false, bool mysteryDungeonWithExtraBreakables = false, bool thisIsDefinitelyTerrain = false)
    {
        Destructible template = Destructible.FindTemplate(dt);

        if (!thisIsDefinitelyTerrain && template == null)
        {
            Debug.Log("WARNING Could not find dt " + dt);
            return null;
        }
        //Destructible act = new Destructible();
        Destructible act = DTPooling.GetDestructible();
        act.CopyFromTemplate(template);
        act.dungeonFloor = floor;
        act.SetUniqueIDAndAddToDict();
        act.SetSpawnPosXY((int)checkTile.pos.x, (int)checkTile.pos.y);
        act.SetCurPos(checkTile.pos);
        AddActorToLocation(checkTile.pos, act);
        AddActorToMap(act);
        float runningLootChance = act.lootChance;

        if (mysteryDungeon)
        {
            runningLootChance += MysteryDungeonManager.EXTRA_LOOT_DROP_CHANCE;
        }

        runningLootChance *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.LOOT_RATE);

        if (checkTile.tileType != TileTypes.GROUND)
        {
            checkTile.ChangeTileType(TileTypes.GROUND, mgd);
            Area ar = GetAreaByID(areaIDArray[(int)checkTile.pos.x, (int)checkTile.pos.y]);
            if (!ar.internalTiles.Contains(checkTile))
            {
                ar.internalTiles.Add(checkTile);
            }
            if (ar.edgeTiles.Contains(checkTile))
            {
                ar.edgeTiles.Remove(checkTile);
            }
        }

        bool lootPossible = act.maxItems > 0;
        if (!thisIsDefinitelyTerrain && lootPossible)
        {
            float scaledLootValue = 1f + ((challengeRating - 1.0f) / 2f) + act.bonusLootValue;

            if (mysteryDungeon)
            {
                scaledLootValue += MysteryDungeonManager.EXTRA_BREAKABLE_LOOT_VALUE;
            }

            if (mysteryDungeonWithExtraBreakables && UnityEngine.Random.Range(0,1f) <= MysteryDungeon.CHANCE_EXTRA_ITEMS_BREAKABLE)
            {
                Item itm = LootGeneratorScript.GenerateLoot(scaledLootValue + 0.3f, 0.5f, floor);
                act.myInventory.AddItem(itm, false);
            }

            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                scaledLootValue += 0.05f;
            }
            scaledLootValue = (float)Math.Round(scaledLootValue, 1);

            if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
            {
                scaledLootValue += (0.35f * GameStartData.NewGamePlus);
                if (GameStartData.NewGamePlus == 2)
                {
                    scaledLootValue += 0.15f;
                }
            }
            if (scaledLootValue > challengeRating + 0.1f && floor < 5)
            {
                scaledLootValue = challengeRating + 0.1f;
            }

            float extraMagicValue = mysteryDungeon ? MysteryDungeonManager.EXTRA_MAGIC_ITEM_CHANCE : 0f;

            if (GameStartData.NewGamePlus >= 2)
            {
                extraMagicValue += 0.2f;
            }

            for (int m = 0; m < act.maxItems; m++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= runningLootChance)
                {
                    if (UnityEngine.Random.Range(0, 1f) < act.bonusLegendaryChance)
                    {
                        Item myLeg = LootGeneratorScript.GenerateLootFromTable(scaledLootValue, GameMasterScript.gmsSingleton.globalMagicItemChance + act.bonusMagicChance + extraMagicValue, "equipment");
                        myLeg.SetUniqueIDAndAddToDict();
                        act.myInventory.AddItem(myLeg, false);
                    }
                    else
                    {
                        Item itm = LootGeneratorScript.GenerateLoot(scaledLootValue, 1f + act.bonusMagicChance + extraMagicValue + act.bonusLootValue * 2f, floor);
                        act.myInventory.AddItem(itm, false);
                        if (itm.IsEquipment())
                        {
                            Equipment eq = itm as Equipment;
                            if (UnityEngine.Random.Range(0, 1f) < act.bonusMagicChance && scaledLootValue >= 1.3f)
                            {
                                EquipmentBlock.MakeMagical(eq, scaledLootValue, false);
                            }
                        }
                    }
                }
                runningLootChance = runningLootChance / (m + 1);
            }

            int curCount = act.myInventory.GetActualInventoryCount();

            for (int i = curCount; i < act.minItems; i++)
            {
                Item itm = LootGeneratorScript.GenerateLoot(scaledLootValue, 1f + extraMagicValue + act.bonusLootValue * 2f, floor);
                act.myInventory.AddItem(itm, false);
                if (itm.IsEquipment())
                {
                    Equipment eq = itm as Equipment;
                    if (UnityEngine.Random.Range(0, 1f) < act.bonusMagicChance)
                    {
                        EquipmentBlock.MakeMagical(eq, scaledLootValue, false);
                    }
                }
            }

            int magicItemCount = 0;
            int gearCount = 0;
            foreach (Item itm in act.myInventory.GetInventory())
            {
                if (itm.IsEquipment())
                {
                    Equipment eq = itm as Equipment;
                    if (eq.rarity > (int)Rarity.COMMON)
                    {
                        magicItemCount++;
                    }
                    gearCount++;
                }
            }

            for (int i = gearCount; i < magicItemCount; i++)
            {
                Item test = LootGeneratorScript.GenerateLootFromTable(scaledLootValue, GameMasterScript.gmsSingleton.globalMagicItemChance + extraMagicValue + act.bonusMagicChance, "equipment");
                test.SetUniqueIDAndAddToDict();
                act.myInventory.AddItemRemoveFromPrevCollection(test, false);
            }

            if (magicItemCount < act.minMagicItems)
            {
                foreach (Item itm in act.myInventory.GetInventory())
                {
                    if (itm.IsEquipment())
                    {
                        Equipment eq = itm as Equipment;
                        if (eq.rarity == Rarity.COMMON)
                        {
                            EquipmentBlock.MakeMagical(eq, scaledLootValue, false);
                            magicItemCount++;
                            if (magicItemCount >= act.minMagicItems)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (!thisIsDefinitelyTerrain)
        {
        checkTile.ChangeTileType(TileTypes.GROUND, mgd);
        }
        return act;

    }

    public void CreateTreasureInTile(MapTileData tile, bool mysteryDungeon = false, bool mysteryDungeonWithExtraTreasure = false)
    {
        Destructible template = Destructible.FindTemplate("obj_treasuresparkle");
        //Destructible act = new Destructible();
        Destructible act = DTPooling.GetDestructible();
        act.CopyFromTemplate(template);
        act.dungeonFloor = floor;
        act.SetUniqueIDAndAddToDict();
        act.SetSpawnPosXY((int)tile.pos.x, (int)tile.pos.y);
        act.SetCurPos(tile.pos);
        act.monsterCollidable = false; // Shouldn't be necessary
        act.playerCollidable = false; // Shouldn't be necessary
        AddActorToLocation(tile.pos, act);
        AddActorToMap(act);
        float runningLootChance = act.lootChance;

        float scaledLootValue = 1f + ((challengeRating - 1.0f) / 2f) + act.bonusLootValue;
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            scaledLootValue += 0.05f;
        }
        scaledLootValue = (float)Math.Round(scaledLootValue, 1);

        if (scaledLootValue > challengeRating + 0.1f && floor < 5)
        {
            scaledLootValue = challengeRating + 0.1f;
        }

        if (mysteryDungeon)
        {
            scaledLootValue += MysteryDungeonManager.EXTRA_BREAKABLE_LOOT_VALUE;
        }

        float extraLootChance = mysteryDungeon ? MysteryDungeonManager.EXTRA_LOOT_DROP_CHANCE : 0f;
        runningLootChance += extraLootChance;
        float extraMagicChance = mysteryDungeon ? MysteryDungeonManager.EXTRA_MAGIC_ITEM_CHANCE : 0f;

        if (GameStartData.NewGamePlus == 2)
        {
            act.maxItems++;
            runningLootChance += 0.2f;
            scaledLootValue += 0.2f;
        }

        for (int m = 0; m < act.maxItems; m++)
        {
            if (UnityEngine.Random.Range(0, 1f) <= runningLootChance)
            {
                float localMagicChance = 0.9f;
                Item itm = LootGeneratorScript.GenerateLoot(scaledLootValue, localMagicChance + extraMagicChance, floor);
                act.myInventory.AddItem(itm, false);
            }
            runningLootChance = runningLootChance / (m + 1);
        }
    }

    // Deprecated
    public List<MonsterTemplateData> GetPossibleMonstersForMap()
    {
        if (possibleMonstersToSpawn == null)
        {
            possibleMonstersToSpawn = new List<MonsterTemplateData>();
        }
        else
        {
            return possibleMonstersToSpawn;
        }
        // Challenge of monster can be UP TO map challenge level, or 30% lower
        // TODO: More variety? Special monster types per floor?
        foreach (MonsterTemplateData monTemp in GameMasterScript.masterSpawnableMonsterList)
        {
            if ((monTemp.challengeValue <= challengeRating) && (monTemp.challengeValue >= challengeRating * 0.8f)) // These are rules for spawning monsters, make this better.
            {
                possibleMonstersToSpawn.Add(monTemp);
            }
        }
        return possibleMonstersToSpawn;
    }

    public bool ValidateTileForMonster(MapTileData mtd, int laxFactor)
    {
        if (mtd.tileType != TileTypes.GROUND) return false;

        // Allow monsters to spawn in water.
        if (mtd.CheckTag(LocationTags.MUD) || mtd.CheckTag(LocationTags.ELECTRIC) || mtd.CheckTag(LocationTags.LAVA))
        {
            return false;
        }

        if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVE)
        {
            if (mtd.pos.x <= 5 || mtd.pos.x >= columns - 4 || mtd.pos.y <= 5 || mtd.pos.y >= rows - 4)
            {
                return false;
            }
        }

        if (mtd.GetStairsInTile() != null) return false;
        if (mtd.GetAllTargetablePlusDestructibles().Count > 0) return false;



        foreach (Monster m in monstersInMap)
        {
            if (MapMasterScript.GetGridDistance(m.GetPos(), mtd.pos) <= 3 - laxFactor)
            {
                return false;
            }
        }

        foreach (Stairs st in mapStairs)
        {
            if (MapMasterScript.GetGridDistance(st.GetPos(), mtd.pos) <= 3 - laxFactor)
            {
                return false;
            }
        }

        return true;
    }

    public Monster SpawnRandomMonster(bool mustBeChampion, bool spawnWandering, string specificRef = "", bool championPossible = true, MapTileData specificTile = null)
    {
        if (GameMasterScript.gameLoadSequenceCompleted && RandomJobMode.IsCurrentGameInRandomJobMode() 
            && UnityEngine.Random.Range(0,1f) <= 0.25f && specificRef == "")
        {
            specificRef = RandomJobMode.GetRandoMonsterRefForMap(dungeonLevelData.challengeValue);
        }

        Vector2 heroPos = new Vector2(columns / 2f, rows / 2f);
        float maxRange = 8f;

        if (GameMasterScript.heroPCActor != null)
        {
            heroPos = GameMasterScript.heroPCActor.GetPos();
            maxRange = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE);
        }
        possibleAreas.Clear();

        if (dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS || spawnWandering)
        {
            maxRange = 1;
        }

        MapTileData mtdForMonster = null;// = bestArea.internalTiles[0];
        string monsterToSpawn = dungeonLevelData.spawnTable.GetRandomActorRef();

        if (specificRef != "")
        {
            monsterToSpawn = specificRef;
        }

        int mTries = 0;
        while (mustBeChampion && !MonsterManagerScript.CheckIfRefCanBeChampion(monsterToSpawn))
        {
            mTries++;
            monsterToSpawn = dungeonLevelData.spawnTable.GetRandomActorRef();
            if (mTries > 25000)
            {
                Debug.Log("WARNING: More than 5000 tries to find monster than can be champion.");
                return null;
            }
        }

        bool spawnWithLoot = ShouldSpawnWithLootOnThisFloor();

        Monster newMon = MonsterManagerScript.CreateMonster(monsterToSpawn, spawnWithLoot, true, false, 0f, false);
        MapTileData checkTile;

        int tries = 0;

        if (specificTile == null)
        {
            /* if (dungeonLevelData.layoutType == DungeonFloorTypes.MAZE || dungeonLevelData.layoutType == DungeonFloorTypes.CAVE 
                || dungeonLevelData.layoutType == DungeonFloorTypes.VOLCANO || dungeonLevelData.layoutType == DungeonFloorTypes.AUTOCAVE 
                || dungeonLevelData.layoutType == DungeonFloorTypes.MAZEROOMS || dungeonLevelData.layoutType == DungeonFloorTypes.CAVEROOMS 
                || dungeonLevelData.layoutType == DungeonFloorTypes.LAKE || dungeonLevelData.layoutType == DungeonFloorTypes.CIRCLES
                || dungeonLevelData.layoutType == DungeonFloorTypes.BSPROOMS) */
            // From now on, let's ONLY use this method as it's much more efficient - particularly on Switch
            {
                bool validTile = false;
                int laxFactor = 0;
                while (!validTile)
                {
                    tries++;
                    if (tries > 5000)
                    {
                        Debug.Log("Could not find good area for monster " + dungeonLevelData.floor + " " + floor + " " + dungeonLevelData.layoutType);
                        return null;
                    }
                    checkTile = mapArray[UnityEngine.Random.Range(1, columns - 1), UnityEngine.Random.Range(1, rows - 1)];
                    if (tries >= 750)
                    {
                        laxFactor++;
                    }
                    else if (tries >= 1500)
                    {
                        laxFactor++;
                    }
                    else if (tries >= 2250)
                    {
                        laxFactor++;
                    }
                    else if (tries >= 3000)
                    {
                        laxFactor++;
                    }
                    else if (tries >= 3750)
                    {
                        laxFactor++;
                    }
                    else if (tries >= 4500)
                    {
                        laxFactor++;
                    }
                    if (ValidateTileForMonster(checkTile, laxFactor))
                    {
                        if (GameMasterScript.gameLoadSequenceCompleted)
                        {
                            if (MapMasterScript.GetGridDistance(checkTile.pos, GameMasterScript.heroPCActor.GetPos()) >= GameMasterScript.MIN_WANDERING_MONSTER_SPAWN_DIST - laxFactor)
                            {
                                validTile = true;
                                mtdForMonster = checkTile;
                            }
                        }
                        else
                        {
                            // Map gen stuff.
                            validTile = true;
                            mtdForMonster = checkTile;
                        }
                    }
                }
            }
        }
        else
        {
            mtdForMonster = specificTile;
        }


        newMon.startAreaID = CheckMTDArea(mtdForMonster);
        newMon.dungeonFloor = floor;
        newMon.SetSpawnPosXY((int)mtdForMonster.pos.x, (int)mtdForMonster.pos.y);
        newMon.SetCurPos(mtdForMonster.pos);
        AddActorToLocation(mtdForMonster.pos, newMon);
        AddActorToMap(newMon);

        if (newMon.challengeValue < dungeonLevelData.challengeValue && newMon.autoSpawn && dungeonLevelData.scaleMonstersToMinimumLevel == 0)
        {
            float diff = dungeonLevelData.challengeValue - newMon.challengeValue;
            if (!spawnerAlive)
            {
                diff += 0.1f;
            }
            newMon.ScaleWithDifficulty(dungeonLevelData.challengeValue);
        }

        if (championCount < mgd.maxChamps && championPossible)
        {
            if (UnityEngine.Random.Range(0, 1f) <= mgd.chanceToSpawnChampionMonster && !newMon.myTemplate.cannotBeChampion) // 10% chance to spawn
            {
                newMon.MakeChampion();
                championCount++;
                for (int numMods = 1; numMods < mgd.maxChampMods; numMods++)
                {
                    if (UnityEngine.Random.Range(0, 1f) <= 0.5f) // 50% chance for add'l mods. So 5% chance for 2 mod, 2.5% chance for 1 mod etc.
                    {
                        newMon.MakeChampion();
                    }
                }
            }
        }

        if (!newMon.isChampion && GameStartData.CheckGameModifier(GameModifiers.MONSTERS_MIN_1POWER) && newMon.actorfaction != Faction.PLAYER)
        {
            newMon.MakeChampion(false);
        }


        if (GameMasterScript.actualGameStarted && GameMasterScript.heroPCActor.dungeonFloor == newMon.dungeonFloor)
        {
            mms.SpawnMonster(newMon, spawnWandering);
        }

        OnEnemyMonsterSpawned(this, newMon, spawnWandering);

        return newMon;
    }

    public bool CheckAdjacentTileType(Vector2 pos, TileTypes type, bool countCenterTile)
    {
        bool anyAdjacentTileType = false;
        if (mapArray[(int)pos.x, (int)pos.y].tileType == type && countCenterTile)
        {
            return true;
        }
        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
        {
            Vector2 newPos = pos + MapMasterScript.xDirections[i];
            if (newPos.x >= 0 && newPos.y >= 0 && newPos.x < columns && newPos.y < rows)
            {
                MapTileData mtd = mapArray[(int)newPos.x, (int)newPos.y];
                if (mtd.tileType == type)
                {
                    return true;
                }
            }
        }
        return anyAdjacentTileType;
    }

    //Shep: Doors aren't being added right now, but this is a Good Function that can be used if they do return
    /*
    private void CreateAndAddDoor(Vector2 pos)
    {
        // For now, no doors. We'll make blocking objects later when we have the art.
        return;
        MapTileData mtd = mapArray[(int)pos.x, (int)pos.y];
        if (mtd.GetDoor() != null)
        {
            return;
        }

        DoorData dd = new DoorData();
        dd.SetPos(pos);
        dd.displayName = "Wooden Door";
        dd.prefab = "BasicDoor";
        mapDoors.Add(dd);
        AddActorToLocation(pos, dd);
        AddActorToMap(dd);
    }
    */

    private void BuildCorridorTiles(Corridor myCorridor)
    {
        // Determine the center of the corridor - the internal tile that is most in the center of the corridor.

        myCorridor.InitializeLists();

        Vector2 startPos = myCorridor.internalTiles[0].pos;
        Vector2 endPos = myCorridor.internalTiles[myCorridor.internalTiles.Count - 1].pos;
        Vector2 averagePos = new Vector2((startPos.x + endPos.x) / 2, (startPos.y + endPos.y) / 2);

        float distFromAveragePos = 5000f;

        foreach (MapTileData tile in myCorridor.internalTiles)
        {
            tile.ChangeTileType(TileTypes.GROUND, mgd);
            tile.AddTag(LocationTags.CORRIDOR);
            tile.AddMapTag(MapGenerationTags.FILLED);

            if (CheckMTDArea(tile) > 0 && CheckMTDArea(tile) != myCorridor.areaID)
            {
                // Do we need a check for this later to make sure there are internal tiles remaining?

                GetArea(tile).internalTiles.Remove(tile);
                GetArea(tile).edgeTiles.Remove(tile);
                //tile.GetRoom().internalTiles.Remove(tile);
                //tile.GetRoom().edgeTiles.Remove(tile);
            }


            //SetTileAreaID(tile, myCorridor);

            tile.areaType = AreaTypes.CORRIDOR;

            //float checkDist = Vector2.Distance(tile.pos, averagePos);
            float checkDist = (tile.pos - averagePos).sqrMagnitude;

            if (checkDist < distFromAveragePos)
            {
                distFromAveragePos = checkDist;
                myCorridor.center = tile.pos;
            }

            // Get surrounding tiles and convert "NOTHINGS" to walls / add to edgeTiles

            if ((dungeonLevelData.layoutType != DungeonFloorTypes.RUINS) && (dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS))
            {
                for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
                {
                    Vector2 newPos = tile.pos + MapMasterScript.xDirections[i];
                    if (!Vector2.Equals(newPos, tile.pos))
                    {
                        MapTileData testTile = mapArray[(int)newPos.x, (int)newPos.y];
                        if ((testTile.tileType == TileTypes.NOTHING) && (newPos.x > 0) && (newPos.x < columns - 1) && (newPos.y > 0) && (newPos.y < rows - 1))
                        {
                            testTile.ChangeTileType(TileTypes.WALL, mgd);

                            //SetTileAreaID(testTile, myCorridor);

                            testTile.areaType = AreaTypes.CORRIDOR;
                            myCorridor.edgeTiles.Add(testTile);
                        }
                    }
                }
            }
        }
    }

    public Room CreateRoomFromTemplate(int originX, int originY, RoomTemplate rt)
    {
        int roomSizeX = rt.numColumns;
        int roomSizeY = rt.numRows;
        Room roomToReturn = new Room();
        roomToReturn.origin.x = originX;
        roomToReturn.origin.y = originY;
        roomToReturn.size.x = roomSizeX;
        roomToReturn.size.y = roomSizeY;
        roomToReturn.center.x = originX + (roomSizeX / 2);
        roomToReturn.center.y = originY + (roomSizeY / 2);
        roomToReturn.reachable = false;
        roomToReturn.template = rt;
        return roomToReturn;
    }

    public class RoomCreationResult
    {
        public Room roomCreated;
        public bool success;

        public RoomCreationResult(Room rm, bool succeeded)
        {
            roomCreated = rm;
            success = succeeded;
        }
    }

    public RoomCreationResult CreateRoom(int originX, int originY, int roomSizeX, int roomSizeY, int roomID, bool enforceRoomSize, bool forcePriorityRooms = true, bool allowTemplates = true, bool allowDuplicatePriorityRooms = true, bool breakMaxRoomSize = false)
    {
        // Maybe create something from a template instead? Use template dimensions
        RoomTemplate templateToUse = null;

        float chance = mgd.chanceToUseRoomTemplate[(int)dungeonLevelData.layoutType];

        if (dungeonLevelData.noCustomRoomTemplates || !allowTemplates)
        {
            chance = 0;
        }
        if (forcePriorityRooms)
        {
            chance = 1f;
        }

        if (chance > 0 && UnityEngine.Random.Range(0, 0.99f) < chance) // Good chance to use a template.
        {
            //possibleRT.Clear(); Old method

            if (!priorityRoomsBuilt)
            {
                possibleRT = dungeonLevelData.priorityTemplates;
            }
            else
            {
                if (localPointArray[(roomSizeY * localPointArrayMaxX) + roomSizeX] == null)
                {
                    // Map does not support this size
                    return FailedRoomCreation;
                }
                if (enforceRoomSize)
                {
                    possibleRT = localEnforcedRoomDict[(roomSizeY * localPointArrayMaxX) + roomSizeX];

                }
                else
                {
                    possibleRT = localUnenforcedRoomDict[(roomSizeY * localPointArrayMaxX) + roomSizeX];

                }
            }

            if (possibleRT.Count > 0)
            {
                int attempts = 0;
                while (attempts < 100)
                {
                    RoomTemplate rt = possibleRT[UnityEngine.Random.Range(0, possibleRT.Count)];
                    if (rt.numColumns + originX < (columns - 1) && rt.numRows + originY < (rows - 1))
                    {
                        templateToUse = rt;
                        roomSizeX = templateToUse.numColumns;
                        roomSizeY = templateToUse.numRows;
                        break;
                    }
                    attempts++;
                    if (floor == 2 && UnityEngine.Random.Range(0, 1f) <= 0.25f)
                    {
                        Debug.Log(rt.refName + " failed due to size overflow " + (rt.numColumns + originX) + "," + (rt.numRows + originY) + " vs " + columns + ", " + rows);
                    }
                }
            }

            if (templateToUse == null)
            {
                if (chance == 1f)
                {
                    return FailedRoomCreation;
                }
            }
        }

        if (!breakMaxRoomSize && templateToUse == null)
        {
            if (dungeonLevelData.layoutType == DungeonFloorTypes.STANDARD || dungeonLevelData.layoutType == DungeonFloorTypes.STANDARDNOHALLS || dungeonLevelData.layoutType == DungeonFloorTypes.FUTURENOHALLS)
            {
                if (roomSizeX > 9)
                {
                    roomSizeX = 9;
                }
                if (roomSizeY > 9)
                {
                    roomSizeY = 9;
                }
            }
        }

        roomToReturn = new Room();
        roomToReturn.origin.x = originX;
        roomToReturn.origin.y = originY;
        roomToReturn.size.x = roomSizeX;
        roomToReturn.size.y = roomSizeY;
        roomToReturn.center.x = originX + (roomSizeX / 2);
        roomToReturn.center.y = originY + (roomSizeY / 2);
        roomToReturn.reachable = false;
        roomToReturn.template = templateToUse;

        // Debug.Log("Outlining room " + roomID + " at " + originX + "," + originY + " size " + roomSizeX + "," + roomSizeY);
        return new RoomCreationResult(roomToReturn, true);
    }

    public void PlaceActor(Actor act, MapTileData mtd)
    {
        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mon = act as Monster;
            mon.startAreaID = CheckMTDArea(mtd);
        }
        if (act.actorUniqueID == 0) // Why do some actors not have IDs?
        {
            act.SetUniqueIDAndAddToDict();
        }

        act.SetCurPos(mtd.pos);
        act.SetSpawnPosXY((int)mtd.pos.x, (int)mtd.pos.y);
        AddActorToLocation(mtd.pos, act);
        AddActorToMap(act);
    }

    public bool IsJobTrialFloor()
    {
        if (floor == MapMasterScript.JOB_TRIAL_FLOOR || floor == MapMasterScript.JOB_TRIAL_FLOOR2)
        {
            return true;
        }
        return false;
    }

    public bool IsTownMap()
    {
        if ((floor == MapMasterScript.TOWN_MAP_FLOOR) || (floor == MapMasterScript.TOWN2_MAP_FLOOR))
        {
            return true;
        }
        return false;
    }

    public void BuildRoomTiles(Room myArea)
    {
        bool mapIsItemDreamFromTemplate = (GameMasterScript.actualGameStarted && GameMasterScript.gmsSingleton.ReadTempGameData("dream_map_from_template") == 1);

        int minX = (int)myArea.origin.x;
        int minY = (int)myArea.origin.y;
        int maxX = (int)myArea.size.x + (int)myArea.origin.x; // try -1
        int maxY = (int)myArea.size.y + (int)myArea.origin.y; // try -1

        myArea.InitializeLists();

        internalTilesHash.Clear();
        edgeTilesHash.Clear();

        genericTileList.Clear();

        if (myArea.template != null) // And we're actually using the template...
        {
            RoomTemplate templateToUse = myArea.template;

            bool specialArea = templateToUse.specialArea;
            for (int i = 0; i < templateToUse.numRows; i++) // This doesn't matter because it still reads the same data.
            {
                char[] rowTiles = templateToUse.rowData[i].ToCharArray();

                // Scan through row data.
                for (int e = 0; e < templateToUse.numColumns; e++)
                {
                    char c = '.';

                    try { c = rowTiles[e]; }
                    catch (Exception error)
                    {
                        Debug.Log("Error on " + floor + " " + templateToUse.refName + " " + error);
                    }

                    MapTileData thisTile = null;
                    try { thisTile = mapArray[minX + e, minY + (templateToUse.numRows - 1 - i)]; } // Just added ttu.numrows-1... To see if we can flip the room templates on Y
                    catch (IndexOutOfRangeException h)
                    {
                        Debug.Log(minX + " " + e + " " + minY + " " + i + " " + mapArray.LongLength + " " + mapArray.Length);
                        Debug.Log("Problem with template " + templateToUse.refName + " " + myArea.size + " " + myArea.origin + " " + columns + " " + rows + " " + floor + " " + dungeonLevelData.layoutType);
                        Debug.Log(h);
                    }
                    thisTile.areaType = AreaTypes.ROOM;

                    if (specialArea)
                    {
                        thisTile.AddTag(LocationTags.SECRET);
                        thisTile.RemoveTag(LocationTags.LAVA);
                        thisTile.RemoveTag(LocationTags.WATER);
                        thisTile.RemoveTag(LocationTags.MUD);
                        thisTile.RemoveTag(LocationTags.ELECTRIC);
                    }

                    // Two rooms overlapping, what do we do?
                    if (CheckMTDArea(thisTile) > 0 && !specialArea) // Overlapping but NOT fill area. Fill area is 0.
                    {
                        Area ar = GetArea(thisTile);
                        thisTile.ChangeTileType(TileTypes.GROUND, mgd);

                        if (ar == myArea)
                        {
                            edgeTilesHash.Remove(thisTile);
                            internalTilesHash.Remove(thisTile);
                        }
                        else
                        {
                            ar.edgeTiles.Remove(thisTile);
                            ar.internalTiles.Remove(thisTile);
                        }

                        thisTile.AddMapTag(MapGenerationTags.OVERLAP);
                        thisTile.RemoveMapTag(MapGenerationTags.EDGETILE);
                    }

                    mapArray[minX + e, minY + (templateToUse.numRows - 1 - i)] = thisTile;

                    bool readAnySpecial = false;

                    {
                        string templateName = templateToUse.refName;

                        Monster newMon;
                        bool rewards;
                        if (myArea.template.dictCharDef.ContainsKey(c))
                        {
                            CharDefinitionForRoom charDef = myArea.template.dictCharDef[c];
                            Actor createdActor = null;
                            
                            if (mapIsItemDreamFromTemplate)
                            {
                                switch(charDef.eCharType)
                                {
                                    // Skip all this special stuff when making item dreams based on this template.
                                    case RoomCharTypes.NPC:
                                    case RoomCharTypes.DESTRUCTIBLE:
                                    case RoomCharTypes.STAIRS:
                                    //case RoomCharTypes.PLAYERSTART:
                                    case RoomCharTypes.MONSTER:
                                    case RoomCharTypes.RANDOMMONSTER:
                                        thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                        internalTilesHash.Add(thisTile);
                                        edgeTilesHash.Remove(thisTile);
                                        continue;
                                }
                            }

                            switch (charDef.eCharType)
                            {
                                case RoomCharTypes.TERRAIN:
                                    thisTile.AddTag(charDef.locTag);
                                    readAnySpecial = true; // This is used to denote that some special entity was read, and the tile type should be converted to ground / internal
                                    break;
                                case RoomCharTypes.NPC:                                    
                                    createdActor = CreateNPCInTile(thisTile, charDef.GetRandomActorRef());
                                    readAnySpecial = true;
                                    break;
                                case RoomCharTypes.DESTRUCTIBLE:
                                    createdActor = CreateDestructibleInTile(thisTile, charDef.GetRandomActorRef());
                                    if (!charDef.actorEnabled)
                                    {
                                        createdActor.DisableActor();
                                    }
                                    readAnySpecial = true;
                                    break;
                                case RoomCharTypes.TREASURESPARKLE:
                                    CreateTreasureInTile(thisTile);
                                    readAnySpecial = true;
                                    break;
                                case RoomCharTypes.ITEM:
                                case RoomCharTypes.ITEM_TYPE:
                                case RoomCharTypes.FOOD:
                                    string itemRef;
                                    if (charDef.eCharType == RoomCharTypes.FOOD)
                                    {
                                        ActorTable foodTable = LootGeneratorScript.GetLootTable("food_and_meals");
                                        itemRef = foodTable.GetRandomActorRefNonWeighted();
                                        bool bAllowRosePetals = MetaProgressScript.RosePetalsAllowed();
                                        while (!bAllowRosePetals && itemRef == "spice_rosepetals")
                                        {
                                            itemRef = foodTable.GetRandomActorRefNonWeighted();
                                        }
                                    }
                                    else if (charDef.eCharType == RoomCharTypes.ITEM_TYPE)
                                    {
                                        ActorTable useTable = LootGeneratorScript.GetLootTable(charDef.GetRandomActorRef());
                                        itemRef = useTable.GetRandomActorRefNonWeighted();
                                    }
                                    else
                                    {
                                        itemRef = charDef.GetRandomActorRef();
                                    }

                                    float itemCV = challengeRating;
                                    float itemBonusMagicValue = 0.0f;
                                    if (charDef.extraLoot.Count > 0)
                                    {
                                        itemCV = charDef.extraLoot[0].challengeValue;
                                        itemBonusMagicValue = charDef.extraLoot[0].bonusMagicChance;
                                    }
                                    createdActor = LootGeneratorScript.CreateItemFromTemplateRef(itemRef, itemCV, itemBonusMagicValue, true);
                                    PlaceActor(createdActor, thisTile);
                                    readAnySpecial = true;

                                    break;
                                case RoomCharTypes.MONSTER:
                                case RoomCharTypes.RANDOMMONSTER:

                                    //intercept if we don't want to spawn this monster during map generation

                                    rewards = true;
                                    if (charDef.actorFaction == Faction.PLAYER)
                                    {
                                        rewards = false;
                                    }

                                    string monRef = "";
                                    if (charDef.eCharType == RoomCharTypes.RANDOMMONSTER)
                                    {
                                        ActorTable tableToSpawnFrom = dungeonLevelData.spawnTable;
                                        if (charDef.possibleActorTables.Count > 0)
                                        {
                                            tableToSpawnFrom = GameMasterScript.GetSpawnTable(charDef.possibleActorTables[UnityEngine.Random.Range(0, charDef.possibleActorTables.Count)]);
                                        }
                                        monRef = tableToSpawnFrom.GetRandomActorRef();
                                    }
                                    else
                                    {
                                        monRef = charDef.GetRandomActorRef();
                                    }

                                    newMon = MonsterManagerScript.CreateMonster(monRef, rewards, rewards, false, 0f, false);

                                    PlaceActor(newMon, thisTile);
                                    for (int x = 0; x < charDef.champMods; x++)
                                    {
                                        newMon.MakeChampion();
                                    }
                                    if (charDef.actorFaction == Faction.PLAYER)
                                    {
                                        newMon.actorfaction = Faction.PLAYER;
                                        newMon.bufferedFaction = Faction.PLAYER;
                                    }
                                    createdActor = newMon;
                                    readAnySpecial = true;
                                    if (charDef.sleepUntilSeePlayer)
                                    {
                                        newMon.sleepUntilSeehero = true;
                                    }
                                    if (!charDef.actorEnabled)
                                    {
                                        newMon.DisableActor();
                                    }

                                    if (!newMon.isChampion && GameStartData.CheckGameModifier(GameModifiers.MONSTERS_MIN_1POWER) && newMon.actorfaction != Faction.PLAYER)
                                    {
                                        newMon.MakeChampion(false);
                                    }

                                    break;
                                case RoomCharTypes.TREE:
                                    thisTile.ChangeTileType(TileTypes.WALL, mgd);
                                    thisTile.AddTag(LocationTags.TREE);
                                    thisTile.RemoveTag(LocationTags.SOLIDTERRAIN);
                                    thisTile.wallReplacementIndex = -1;
                                    thisTile.SelectWallReplacementIndex();
                                    break;
                                case RoomCharTypes.PLAYERSTART:
                                    heroStartTile = thisTile.pos;
                                    heroStartArea = myArea;
                                    readAnySpecial = true;
                                    break;
                                case RoomCharTypes.STAIRS:
                                    Stairs nStairs = new Stairs();
                                    nStairs.dungeonFloor = floor;

                                    if (charDef.stairDir == StairDirections.FORWARDS)
                                    {
                                        nStairs.displayName = "Stairway Up";
                                        nStairs.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsUp";
                                        nStairs.stairsUp = false;
                                    }
                                    else
                                    {
                                        nStairs.displayName = "Stairway Down";
                                        nStairs.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsDown";
                                        nStairs.stairsUp = true;
                                    }

                                    if (!string.IsNullOrEmpty(charDef.prefab))
                                    {
                                        nStairs.prefab = charDef.prefab;
                                    }

                                    if (charDef.pointToFloor != -1)
                                    {
                                        nStairs.pointsToFloor = charDef.pointToFloor;
                                    }
                                    PlaceActor(nStairs, thisTile);
                                    createdActor = nStairs;
                                    readAnySpecial = true;

                                    //Debug.Log("Spawned SPECIAL stairs in floor " + floor + " on " + thisTile.pos + " pointing to floor " + nStairs.pointsToFloor + " Stair up? " + nStairs.stairsUp);

                                    //mapStairs.Add(nStairs); // New as of 11/4/17

                                    if (charDef.transparentStairs)
                                    {
                                        nStairs.prefab = "TransparentStairs";
                                        nStairs.autoMove = true;
                                    }

                                    if (!charDef.actorEnabled)
                                    {
                                        createdActor.DisableActor();
                                    }

                                    break;
                            }

                            bool actorCreated = createdActor != null;

                            if (actorCreated)
                            {
                                foreach(string key in charDef.startActorData.Keys)
                                {
                                    createdActor.SetActorData(key, charDef.startActorData[key]);
                                }
                            }

                            if (charDef.extraLoot.Count > 0 && actorCreated)
                            {
                                foreach (LootPackage lp in charDef.extraLoot)
                                {
                                    Item newLoot = LootGeneratorScript.GenerateLoot(lp.challengeValue, lp.bonusMagicChance);
                                    createdActor.myInventory.AddItemRemoveFromPrevCollection(newLoot, false);
                                }
                            }
                            else if (charDef.extraLoot.Count > 0 && !actorCreated)
                            {
                                Debug.Log("Bonus loot needed for " + charDef.symbol + " floor " + floor + " but no actor was created.");
                            }

                            if (charDef.actorDirection != Directions.NEUTRAL && actorCreated)
                            {
                                createdActor.lastMovedDirection = charDef.actorDirection;
                            }
                            else if (charDef.actorDirection != Directions.NEUTRAL && !actorCreated)
                            {
                                Debug.Log("Direction set for new actor " + charDef.symbol + " " + floor + " but no actor created.");
                            }

                            if (charDef.changeTileType)
                            {
                                thisTile.ChangeTileType(charDef.eTileType, mgd);
                            }
                        }

                        // This is used for remaining special cases.
                        bool nReadAnySpecial = TryBuildSpecialTemplateTile(myArea, templateName, c, thisTile, mapIsItemDreamFromTemplate);
                        if (nReadAnySpecial)
                        {
                            readAnySpecial = nReadAnySpecial;
                        }
                    } // End special template function

                    // Regular room template stuff.
                    switch (c)
                    {
                        case 'X': // Wall, non-edge tile.
                            thisTile.ChangeTileType(TileTypes.WALL, mgd);
                            thisTile.AddMapTag(MapGenerationTags.NOCONNECTION);
                            internalTilesHash.Add(thisTile);
                            edgeTilesHash.Remove(thisTile);

                            /*
                            if (!myArea.internalTiles.Contains(thisTile))
                            {
                                myArea.internalTiles.Add(thisTile);
                            }
                            if (myArea.edgeTiles.Contains(thisTile))
                            {
                                myArea.edgeTiles.Remove(thisTile);
                            }
                            */
                            break;
                        case 'C': // Wall, corner tile.
                            thisTile.AddTag(LocationTags.CORNER);
                            thisTile.ChangeTileType(TileTypes.WALL, mgd); // It is a wall
                            internalTilesHash.Remove(thisTile);
                            edgeTilesHash.Add(thisTile);

                            /*
                            if (!myArea.internalTiles.Contains(thisTile))
                            {
                                myArea.internalTiles.Remove(thisTile);
                            }
                            if (!myArea.edgeTiles.Contains(thisTile))
                            {
                                myArea.edgeTiles.Add(thisTile);
                            }
                            */
                            break;
                        case '.': // Empty
                            readAnySpecial = true;
                            break;
                        case 'E': // Edge wall
                            thisTile.ChangeTileType(TileTypes.WALL, mgd);
                            thisTile.AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);

                            internalTilesHash.Remove(thisTile);
                            edgeTilesHash.Add(thisTile);
                            /*
                            if (myArea.internalTiles.Contains(thisTile))
                            {
                                myArea.internalTiles.Remove(thisTile);
                            }
                            if (!myArea.edgeTiles.Contains(thisTile))
                            {
                                myArea.edgeTiles.Add(thisTile);
                            }
                            */

                            break;
                        case 'Y': // Edge non-wall
                            readAnySpecial = true;
                            break;
                    }

                    if (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)
                    {
                        if (TryBuildNonSpecialRoomTile(myArea, c, thisTile, mapIsItemDreamFromTemplate))
                        {
                            readAnySpecial = true;
                        }
                    }

                    // Special tiles are converted to ground by default.
                    if (readAnySpecial)
                    {
                        thisTile.ChangeTileType(TileTypes.GROUND, mgd);

                        internalTilesHash.Add(thisTile);
                        edgeTilesHash.Remove(thisTile);
                    }

                } // End of FOR loop iterating through template characters in columns (x)
            } // End of FOR loop iterating through rows (y)

            foreach (MapTileData maptile in internalTilesHash) 
            {
                maptile.RemoveMapTag(MapGenerationTags.EDGETILE);
                maptile.AddMapTag(MapGenerationTags.FILLED);
            }
            foreach (MapTileData maptile in edgeTilesHash) 
            {
                maptile.AddMapTag(MapGenerationTags.EDGETILE);
                maptile.AddMapTag(MapGenerationTags.FILLED);
            }

            if (myArea.template.refName == "buriedloot")
            {
                genericTileList.Shuffle();
                Item keyTemplate = Item.GetItemTemplateFromRef("item_goldkey");
                Consumable goldKey = new Consumable();
                goldKey.CopyFromItem(keyTemplate);
                PlaceActor(goldKey, genericTileList[0]);
                //Debug.Log("<color=green>Gold key for Buried Loot placed at " + genericTileList[0].pos + "</color>");

                for (int x = 1; x < genericTileList.Count; x++)
                {
                    DungeonLevel dl = MapMasterScript.theDungeon.FindFloor(112).dungeonLevelData;
                    string mnToSpawn = dl.spawnTable.GetRandomActorRef();
                    Monster hideMon = MonsterManagerScript.CreateMonster(mnToSpawn, true, true, false, 0f, false);
                    PlaceActor(hideMon, genericTileList[x]);
                    hideMon.sleepUntilSeehero = true;

                    if (!hideMon.isChampion && GameStartData.CheckGameModifier(GameModifiers.MONSTERS_MIN_1POWER) && hideMon.actorfaction != Faction.PLAYER)
                    {
                        hideMon.MakeChampion(false);
                    }
                }

            }
        }
        //Debug.Log("Finished creating template " + templateToUse.refName + " Edge Tiles: " + myArea.edgeTiles.Count + " Internal: " + myArea.internalTiles.Count + " Loc: " + myArea.origin.ToString());
        // bracket was here
        else
        {
            Vector2 v2 = Vector2.zero;

            switch (dungeonLevelData.layoutType)
            {
                case DungeonFloorTypes.STANDARD:
                case DungeonFloorTypes.STANDARDNOHALLS:
                case DungeonFloorTypes.FUTURENOHALLS:
                case DungeonFloorTypes.RUINS:
                case DungeonFloorTypes.CAVEROOMS:
                case DungeonFloorTypes.HIVES:
                case DungeonFloorTypes.MAZEROOMS:
                case DungeonFloorTypes.SPIRIT_DUNGEON:
                    for (int x = minX; x <= maxX; x++) // was <=
                    {
                        for (int y = minY; y <= maxY; y++) // was <=
                        {
                            v2.x = x;
                            v2.y = y;
                            if (!RoomInBounds(v2))
                            {
                                Debug.Log("Xrange " + minX + "," + maxX + " Yrange: " + minY + "," + maxY + " Coords: " + x + "," + y + " is out of bounds for " + floor + " " + dungeonLevelData.layoutType + " " + columns + "," + rows + " RSize " + myArea.size + " " + myArea.origin);
                                continue;
                            }
                            MapTileData thisTile = mapArray[x, y];
                            // Two rooms overlapping, what do we do?


                            if (CheckMTDArea(thisTile) > 0)
                            {
                                Area ar = GetArea(thisTile);
                                ar.edgeTiles.Remove(thisTile);
                                ar.internalTiles.Remove(thisTile);
                                thisTile.AddMapTag(MapGenerationTags.OVERLAP);
                                thisTile.RemoveMapTag(MapGenerationTags.EDGETILE);

                                internalTilesHash.Add(thisTile);
                                //myArea.internalTiles.Add(thisTile);
                                thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                thisTile.areaType = AreaTypes.ROOM;
                                mapArray[x, y] = thisTile;
                                continue;
                            }
                            if ((y == minY) || (y == maxY) || (x == minX) || (x == maxX))
                            {
                                if (dungeonLevelData.layoutType == DungeonFloorTypes.MAZEROOMS)
                                {
                                    thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                    edgeTilesHash.Add(thisTile);
                                    //myArea.edgeTiles.Add(thisTile);
                                    thisTile.AddMapTag(MapGenerationTags.EDGETILE);
                                    continue;
                                }

                                // Outer bounds - add walls
                                thisTile.ChangeTileType(TileTypes.WALL, mgd);
                                edgeTilesHash.Add(thisTile);
                                //myArea.edgeTiles.Add(thisTile);
                                if (((y == minY) && (x == minX)) || ((y == minY) && (x == maxX)) || ((y == maxY) && (x == minX)) || ((y == maxY) && (x == maxX)))
                                {
                                    thisTile.AddTag(LocationTags.CORNER);
                                }
                                else
                                {
                                    thisTile.ChangeTileType(TileTypes.WALL, mgd);
                                    thisTile.AddMapTag(MapGenerationTags.ENTRANCEPOSSIBLE);
                                }
                                thisTile.AddMapTag(MapGenerationTags.EDGETILE);

                            }
                            else
                            {
                                // Not outer bounds, fill with whatever
                                thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                internalTilesHash.Add(thisTile);
                                //myArea.internalTiles.Add(thisTile);
                                thisTile.RemoveMapTag(MapGenerationTags.EDGETILE);
                            }

                            //SetTileAreaID(thisTile, myArea);

                            thisTile.areaType = AreaTypes.ROOM;
                            mapArray[x, y] = thisTile;
                        }
                    }
                    break;
                case DungeonFloorTypes.CAVE:
                case DungeonFloorTypes.ISLANDS:
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            MapTileData thisTile = mapArray[x, y];
                            if ((y == minY) || (y == maxY) || (x == minX) || (x == maxX))
                            {
                                // Outer bounds - mark as edge tiles, but otherwise they are ground.

                                thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                edgeTilesHash.Add(thisTile);
                                //myArea.edgeTiles.Add(thisTile);
                                if (((y == minY) && (x == minX)) || ((y == minY) && (x == maxX)) || ((y == maxY) && (x == minX)) || ((y == maxY) && (x == maxX)))
                                {
                                    thisTile.AddTag(LocationTags.CORNER);
                                }
                                thisTile.AddMapTag(MapGenerationTags.EDGETILE);
                            }
                            else
                            {
                                // Not outer bounds, fill with whatever
                                thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                                internalTilesHash.Add(thisTile);
                                //myArea.internalTiles.Add(thisTile);
                                thisTile.RemoveMapTag(MapGenerationTags.EDGETILE);
                            }

                            //SetTileAreaID(thisTile, myArea);

                            thisTile.areaType = AreaTypes.ROOM;
                            mapArray[x, y] = thisTile;
                        }
                    }
                    break;
            }


        }

        myArea.edgeTiles = edgeTilesHash.ToList();
        myArea.internalTiles = internalTilesHash.ToList();


        List<MapTileData> toSwitch = new List<MapTileData>();
        int switched = 0;
        for (int i = 0; i < myArea.edgeTiles.Count; i++)
        {
            if ((myArea.edgeTiles[i].CheckMapTag(MapGenerationTags.OVERLAP)) && ((switched < 2) || (UnityEngine.Random.Range(0, 1f) <= dungeonLevelData.chanceToOpenWalls)))
            {
                if ((myArea.template != null) && (myArea.template.specialArea)) continue;
                if ((myArea.edgeTiles[i].pos.x >= 1) && (myArea.edgeTiles[i].pos.x <= columns - 2) && (myArea.edgeTiles[i].pos.y >= 1) && (myArea.edgeTiles[i].pos.y <= rows - 2))
                {
                    myArea.edgeTiles[i].ChangeTileType(TileTypes.GROUND, mgd);
                    toSwitch.Add(myArea.edgeTiles[i]);
                    myArea.edgeTiles[i].RemoveMapTag(MapGenerationTags.EDGETILE);
                    //switched = true; experimental - commented out to deal with blocked off areas.
                    switched++;
                }
            }
        }
        foreach (MapTileData mtd in toSwitch)
        {
            myArea.edgeTiles.Remove(mtd);
            myArea.internalTiles.Add(mtd);
        }
    }

    public bool CheckCollision(Vector2 location, Actor checkActor)
    {
        if (location.x <= 0 || location.y <= 0 || location.x >= columns - 1 || location.y >= rows - 1)
        {
            return true;
        }

        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        return mtd.IsCollidable(checkActor);
    }


    public bool CheckUnbreakableCollision(Vector2 location, Actor checkActor)
    {
        if (!InBounds(location))
        {
            return true;
        }
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        return mtd.IsUnbreakableCollidable(checkActor);
    }


    public Actor GetTargetableAtLocation(Vector3 location)
    {
        if ((location.x >= 0) && (location.y >= 0) && (location.x < columns) && (location.y < rows))
        {
            MapTileData mtd = mapArray[(int)location.x, (int)location.y];
            return mtd.GetTargetable();
        }
        return null;

    }

    public List<Actor> GetAllTargetableAtLocation(Vector3 location)
    {
        if ((location.x >= 0) && (location.y >= 0) && (location.x < columns) && (location.y < rows))
        {
            MapTileData mtd = mapArray[(int)location.x, (int)location.y];
            return mtd.GetAllTargetable();
        }
        return new List<Actor>();
    }
    //Shep: Function does nothing and isn't called anymore
    /*
    public DoorData CheckDoor(Vector3 location)
    {
        return null;
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        return mtd.GetDoor();
    }
    */

    /// <summary>
    /// Returns a map tile, if valid
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>The tile, or null if the coordinates are out of bound</returns>
    public MapTileData GetTile(int x, int y)
    {
        if (x < 0 || 
            y < 0 ||
            x >= columns ||
            y >= rows)
        {
            return null;
        }

        return mapArray[x,y];
    }
    
    public MapTileData GetTile(Vector2 location)
    {
        if ((location.x < 0) || (location.y < 0) || (location.x >= columns) || (location.y >= rows))
        {
            //Debug.Log("No tile at " + location + " map floor " + floor);
            return null;
        }
        return mapArray[(int)location.x, (int)location.y];
    }

    public MapTileData GetTileInDirection(Vector2 vStart, Directions d)
    {
        vStart += MapMasterScript.xDirections[(int)d];
        return GetTile(vStart);
    }

    public MapTileData GetTileInDirection(Point pStart, Directions d)
    {
        Vector2 vStart = new Vector2(pStart.x, pStart.y);
        vStart += MapMasterScript.xDirections[(int)d];
        return GetTile(vStart);
    }

    public bool MoveActor(Vector3 oldLoc, Vector3 newLoc, Actor act)
    {
        if (!InBounds(oldLoc) || !InBounds(newLoc))
        {
            if (Debug.isDebugBuild) Debug.Log("ERROR trying to move actor " + act.actorRefName + " on floor " + floor + " with old -> new pos being " + oldLoc + " " + newLoc);
            if (Debug.isDebugBuild) Debug.Log("Map columns, rows: " + columns + "," + rows);
            return false;
        }
        MapTileData oldTile = mapArray[(int)oldLoc.x, (int)oldLoc.y];
        MapTileData newTile = mapArray[(int)newLoc.x, (int)newLoc.y];
        oldTile.RemoveActor(act);
        newTile.AddActor(act);       

        //Debug.Log(act.actorRefName + " moved from " + oldTile.pos + " to " + newTile.pos);

        act.SetPos(newLoc);

        GameObject obj = act.GetObject();

        if (act.GetActorType() == ActorTypes.HERO || act.GetActorType() == ActorTypes.MONSTER)
        {
            float angle = CombatManagerScript.GetAngleBetweenPoints(oldLoc, newLoc);
            act.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(angle));
            Fighter ft = act as Fighter;

            ft.myStats.UpdateStatusDirections();

            if (newTile.CheckTag(LocationTags.WATER))
            {
                bool groundEffects = true;

                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.CheckAttribute(MonsterAttributes.FLYING) > 0)
                    {
                        groundEffects = false;
                    }
                }
                if (groundEffects)
                {
                    if (ft.myStats.RemoveStatusByRef("status_lavaburns") || ft.myStats.RemoveStatusByRef("status_fireburns") || ft.myStats.RemoveStatusByRef("status_fireburns2") || ft.myStats.RemoveStatusByRef("status_fireburns3"))
                    {
                        StringManager.SetTag(0, act.displayName);
                        GameLogScript.GameLogWrite(StringManager.GetString("douse_burns"), act);
                    }

                }
            }
        }

        act.areaID = CheckMTDArea(newTile);
        act.SetActorArea(GetArea(newTile));
        if (CheckMTDArea(newTile) != act.areaID)
        {
            act.lastAreaVisited = act.areaID;
        }

        // 6/1/18: Why do we need to do the LOS check here? It happens at the end of each turn guaranteed anyway.
        /* if (obj != null)
        {
            MapMasterScript.CheckMapActorLOS(act);
        } */

        // Adjust sorting layer based on position.            
        act.UpdateSpriteOrder();

        if (act.GetActorType() == ActorTypes.HERO)
        {
            if (CheckMTDArea(newTile) > 0)
            { // An area that is not fill.
                if (!GameMasterScript.heroPCActor.exploredAreas[CheckMTDArea(newTile)])
                {
                    GameMasterScript.heroPCActor.exploredAreas[CheckMTDArea(newTile)] = true;

                    if (CheckMTDArea(newTile) > 0)
                    {
                        if (newTile.CheckTag(LocationTags.SECRET) || newTile.CheckTag(LocationTags.SECRETENTRANCE))
                        {
                            GameLogScript.GameLogWrite(StringManager.GetString("secret_area_found"), GameMasterScript.heroPCActor);
                            BattleTextManager.NewText(StringManager.GetString("secret_area_bt"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.4f);
                        }
                    }

                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_explorer"))
                    {
                        if (UnityEngine.Random.Range(0, 1f) <= 0.25f)
                        {
                            GameMasterScript.gmsSingleton.AwardJP(UnityEngine.Random.Range(2, 8));
                        }
                    }
                }
            }
            else
            {
                //Debug.Log("Player is in room with no ID.");
            }
        }

        try { TileInteractions.CheckAndRunTileOnMove(newTile, act); }
        catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log("Trying to move from " + oldLoc + " to " + newLoc);
            if (newTile == null)
            {
                Debug.Log("New tile is null.");
            }
            if (oldTile == null)
            {
                Debug.Log("Old tile is null.");
            }
            if (act == null)
            {
                Debug.Log("Move a null actor?");
            }
            return false;
        }

        return true;
    }

    public bool InAbsoluteBounds(Vector2 loc)
    {
        int x = (int)loc.x;
        int y = (int)loc.y;
        if ((x < 0) || (y < 0) || (x >= columns) || (y >= rows)) return false;
        return true;
    }

    public bool RemoveActorFromLocation(Vector3 location, Actor act)
    {
        if (!InAbsoluteBounds(location) && location != Vector3.zero)
        {
            //Debug.Log("Cannot remove " + act.actorRefName + " " + act.actorUniqueID + " from location " + location + " as it is OOB on floor " + act.dungeonFloor + " " + columns + "," + rows);
            return false;
        }

        MapTileData mtd = mapArray[(int)location.x, (int)location.y];

        if (!mtd.RemoveActor(act))
        {
            return false;
        }

        //Debug.Log("Removed " + act.actorRefName + " at " + act.GetPos() + " from " + location);
        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            if (CheckMTDArea(mtd) > -1)
            {
                Area getAR = GetArea(mtd);
                if (getAR.props != null) getAR.props.Remove(act);
            }
        }

        return true;
    }

    public void PlaceActorAtRandom(Actor mn, bool mustHaveOpenSpaceAround = false)
    {
        bool foundTile = false;
        MapTileData mtd = null;
        if (mustHaveOpenSpaceAround)
        {
            int attempts = 0;
            while (!foundTile)
            {
                attempts++;
                if (attempts > 5000)
                {
                    if (Debug.isDebugBuild) Debug.Log("Too hard to find tile with open space around on " + GetName() + " for random actor placement.");
                    break;
                }
                // Pick a random tile.
                int x = UnityEngine.Random.Range(1, columns - 1);
                int y = UnityEngine.Random.Range(1, rows - 1);
                mtd = mapArray[x, y];
                if (mtd.tileType != TileTypes.GROUND || mtd.IsCollidable(mn))
                {
                    // Must be ground and not occupied.
                    continue;
                }
                
                // If it's clear, get all tiles around it. Then ensure THOSE tiles are ground.
                CustomAlgorithms.GetTilesAroundPoint(new Vector2(x, y), 1, this);
                bool anyValid = true;
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    MapTileData checkM = CustomAlgorithms.tileBuffer[i];
                    if (checkM.tileType != TileTypes.GROUND)
                    {
                        anyValid = false;
                        break;
                    }
                }
                if (!anyValid)
                {
                    // If we failed the above check, keep searchin'...
                    continue;
                }

                // OK, we found something!
                foundTile = true;
                break;                
            }
            if (foundTile)
            {
                PlaceActor(mn, mtd);
                return;
            }
        }

        foreach (Room rm in mapRooms)
        {
            rm.internalTiles.Shuffle();
            foreach (MapTileData m in rm.internalTiles)
            {
                if (!m.IsCollidable(mn) && m.GetStairsInTile() == null)
                {
                    mtd = m;
                    foundTile = true;
                    break;
                }
            }
            if (foundTile)
            {
                break;
            }
        }
        if (mtd != null)
        {
            PlaceActor(mn, mtd);
        }
        else
        {
            Debug.Log("Couldn't find tile for " + mn.actorRefName);
        }

    }

    public int GetAreaID(Vector2 loc)
    {
        MapTileData mtd = mapArray[(int)loc.x, (int)loc.y];
        return CheckMTDArea(mtd);
    }

    /* public void CheckPowerups(Fighter act, Vector3 location)
    {
        // go is what is interacting with the powerup.
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        List<Actor> pups = mtd.GetPowerupsInTile();
        if (pups.Count > 0)
        {
            for (int i = 0; i < pups.Count; i++)
            {
                GameObject pgo = pups[i].GetObject();
                PowerupScript pup = pgo.GetComponent<PowerupScript>();
                Movable move = pgo.GetComponent<Movable>();
                pup.UsePowerUp(act);
                //RemoveActorFromLocation(move.GetGridPosition(), pups[i]);
                RemoveActorFromMap(pups[i]);
            }
        }
    } */

    public List<Item> GetItemsInTile(Vector3 location)
    {
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        return mtd.GetItemsInTile();
    }

    public Stairs GetStairsInTile(Vector3 location)
    {
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];
        return mtd.GetStairsInTile();
    }

    public bool AddActorToLocation(Vector3 location, Actor act)
    {
        if (location.x >= columns || location.y >= rows || location.x < 0 || location.y < 0)
        {
            // Outside of map bounds.
            Debug.Log(act.actorRefName + " " + act.actorUniqueID + " " + act.dungeonFloor + " Outside of map bounds at " + location);
            return false;
        }

        // int index = GetTileIndex(location);
        MapTileData mtd = mapArray[(int)location.x, (int)location.y];

            if (act.GetActorType() == ActorTypes.MONSTER)
        {
            // 12/25/17. For some reason, monsters are getting duplicated sometimes. We need to figure out why.
            // But for now let's make absolutely sure the previous location is being cleared...
            if (act.previousPosition != Vector2.zero && InBounds(act.previousPosition))
            {
                RemoveActorFromLocation(act.previousPosition, act);
#if UNITY_EDITOR
                //Debug.Log(act.actorRefName + " " + act.actorUniqueID + " was out of position. Heading to " + location + " but was in " + act.previousPosition);
#endif
            }
            if (act.GetPos().x != location.x || act.GetPos().y != location.y)
            {
                RemoveActorFromLocation(act.GetPos(), act);
#if UNITY_EDITOR
                //Debug.Log(act.actorRefName + " " + act.actorUniqueID + " was out of position. Was heading to " + location + " but was in " + act.GetPos());
#endif
            }
        }            
        else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible d = act as Destructible;
            if (!d.isTerrainTile && act.actorRefName == "obj_voidtile" && mtd.CheckActorRef("obj_voidtile"))
            {
                return false;
            }
        }

        if (mtd.HasActor(act))
        {
#if UNITY_EDITOR
            //Debug.Log("Warning: " + mtd.pos + " already has actor " + act.actorRefName + ", returning.");
#endif
            return false;
        }

        mtd.AddActor(act);

        if (CheckMTDArea(mtd) != act.areaID)
        {
            act.lastAreaVisited = CheckMTDArea(mtd);
        }
        act.areaID = CheckMTDArea(mtd);

        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            if (CheckMTDArea(mtd) > -1)
            {
                Area ar = GetArea(mtd);
                if (ar == null)
                {
                    //Debug.Log("Null area for actor " + act.actorRefName);
                }
                else
                {
                    GetArea(mtd).GetProps().Add(act);
                }
            }
        }

        /* if (act.GetPos() != mtd.pos)
        {
            Debug.Log("Mismatch of actor " + act.actorRefName + " position. Is in " + act.GetPos() + " but is being placed in " + mtd.pos);                
        } */

        act.UpdateSpriteOrder();
        return true;
        

    }

    public void BeautifyTile(MapTileData checkTile)
    {
        int i = (int)checkTile.pos.x;
        int e = (int)checkTile.pos.y;
        if (checkTile.tileType == TileTypes.NOTHING || checkTile.tileType == TileTypes.WALL)
        {
            MapTileData tileNorth = mapArray[i, e + 1]; // Tile that is NORTH of the checked tile
            MapTileData tileSouth = mapArray[i, e - 1];
            MapTileData tileEast = mapArray[i + 1, e];
            MapTileData tileWest = mapArray[i - 1, e];

            checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);

            if ((tileSouth.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
            {
                // *X*
                // XoX
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
            }

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
            {
                // *.*
                // .o.
                // *.*

                checkTile.SetTileVisualType(VisualTileType.WALL_NONE);
            }

            //continue; // Was commented out for old method of GROUND instead of WALL variations.

            if (checkTile.visualTileType != VisualTileType.NOTSET)
            {
                //continue;
            }

            // This is a wall tile.

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND))
            {
                // *.*
                // XoX
                // *.*

                checkTile.SetTileVisualType(VisualTileType.WALL_E_W);
            }

            if ((tileSouth.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
            {
                // *X*
                // .o.
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_S);
            }

            if ((tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
            {
                // *.*
                // Xo.
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_S_W);
            }

            if ((tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
            {
                // *X*
                // .oX
                // *.*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_E);
            }

            if ((tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
            {
                // *X*
                // Xo.
                // *.*
                checkTile.SetTileVisualType(VisualTileType.WALL_N_W);
            }

            if ((tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
            {
                // *.*
                // .oX
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_E_S);
            }

            //  Non-Corners

            if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND))
            {
                // *X*
                // XoX
                // *.*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_E_W);
            }

            if ((tileNorth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
            {
                // *.*
                // XoX
                // *X*
                checkTile.SetTileVisualType(VisualTileType.WALL_E_S_W);
            }

            if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
            {
                // *X*
                // .oX
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S);
            }

            if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
            {
                // *X*
                // Xo.
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_S_W);
            }

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND))
            {
                // *.*
                // Xo.
                // *.*
                // Easternmost bit of wall

                checkTile.SetTileVisualType(VisualTileType.WALL_W);
            }

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
            {
                // *.*
                // .oX
                // *.*
                // Westernmost bit of wall
                checkTile.SetTileVisualType(VisualTileType.WALL_E);
            }

            if ((tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
            {
                // *.*
                // .o.
                // *X*
                // Northernmost bit of wall

                checkTile.SetTileVisualType(VisualTileType.WALL_S);
            }

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
            {
                // *X*
                // .o.
                // *.*
                // Southernmost bit of wall

                checkTile.SetTileVisualType(VisualTileType.WALL_N);
            }



            if ((tileSouth.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
            {
                // *X*
                // XoX
                // *X*

                checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
            }

            if ((tileSouth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
            {
                // *.*
                // .o.
                // *.*

                checkTile.SetTileVisualType(VisualTileType.WALL_NONE);
            }
        }
    }

    public MapTileData FindClearGroundTile()
    {
        int x = UnityEngine.Random.Range(2, columns - 2);
        int y = UnityEngine.Random.Range(2, rows - 2);
        MapTileData mtd = mapArray[x, y];
        bool valid = false;
        //List<MapTileData> surrounding = new List<MapTileData>();

        if (mtd.tileType == TileTypes.GROUND)
        {
            if ((!mtd.IsCollidable(GameMasterScript.heroPCActor)) && (!mtd.IsDangerous(GameMasterScript.heroPCActor)))
            {
                valid = true;
                //surrounding = GetNonCollidableTilesAroundPoint(mtd.pos, 1, GameMasterScript.heroPCActor);
                CustomAlgorithms.GetNonCollidableTilesAroundPoint(mtd.pos, 1, GameMasterScript.heroPCActor, this);
                if (CustomAlgorithms.numNonCollidableTilesInBuffer <= 3) valid = false;
            }
        }

        while (!valid)
        {
            x = UnityEngine.Random.Range(2, columns - 2);
            y = UnityEngine.Random.Range(2, rows - 2);
            mtd = mapArray[x, y];

            int attempts = 0;

            if (mtd.tileType == TileTypes.GROUND)
            {
                if (!mtd.IsCollidable(GameMasterScript.heroPCActor) && !mtd.IsDangerous(GameMasterScript.heroPCActor))
                {
                    valid = true;
                    CustomAlgorithms.GetNonCollidableTilesAroundPoint(mtd.pos, 1, GameMasterScript.heroPCActor, this);
                    if (CustomAlgorithms.numNonCollidableTilesInBuffer <= 3) valid = false;
                    attempts++;

                    if (attempts > 500 && !mtd.IsCollidable(GameMasterScript.heroPCActor))
                    {
                        break;
                    }
                    //surrounding = GetNonCollidableTilesAroundPoint(mtd.pos, 1, GameMasterScript.heroPCActor);
                    //if (surrounding.Count <= 3) valid = false;
                }
            }
        }

        return mtd;
    }

    // The new optional override field is used when we're temporarily messing with tags for purposes of summoned terrain
    // Otherwise, even if we're actually just adding and quickly removing a tag like Laser...
    // ... The tile can get confused and change its index based on an existing type, like Mud
    // And the sprites get screwed up.
    public void BeautifyTerrain(MapTileData checkTile, LocationTags terrainType, LocationTags terrainType2, LocationTags overrideTerrainCheckType = LocationTags.COUNT)
    {
        int i = (int)checkTile.pos.x;
        int e = (int)checkTile.pos.y;        

        if (i <= 0 || e <= 0 || i >= columns - 1 || e >= rows - 1)
        {
            //Debug.Log("Beautify out of range.");
            return;
        }

        if (checkTile.CheckTag(terrainType))
        {
            MapTileData tileNorth = mapArray[i, e + 1]; // Tile that is NORTH of the checked tile
            MapTileData tileSouth = mapArray[i, e - 1];
            MapTileData tileEast = mapArray[i + 1, e];
            MapTileData tileWest = mapArray[i - 1, e];

            checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_NONE, overrideTerrainCheckType);

            if ((tileSouth.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_E_S_W, overrideTerrainCheckType);
            }

            if ((!tileSouth.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_NONE, overrideTerrainCheckType);
            }

            if ((!tileSouth.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_E_W, overrideTerrainCheckType);
            }

            if ((tileSouth.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_S, overrideTerrainCheckType);
            }

            if ((tileWest.CheckTag(terrainType2)) && (tileSouth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_S_W, overrideTerrainCheckType);
            }

            if ((!tileWest.CheckTag(terrainType2)) && (!tileSouth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_E, overrideTerrainCheckType);
            }

            if ((tileWest.CheckTag(terrainType2)) && (!tileSouth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_W, overrideTerrainCheckType);
            }

            if ((!tileWest.CheckTag(terrainType2)) && (tileSouth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_E_S, overrideTerrainCheckType);
            }

            //  Non-Corners

            if ((tileNorth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)) && (!tileSouth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_E_W, overrideTerrainCheckType);
            }

            if ((!tileNorth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)) && (tileSouth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_E_S_W, overrideTerrainCheckType);
            }

            if ((tileNorth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)) && (tileSouth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_E_S, overrideTerrainCheckType);
            }

            if ((tileNorth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)) && (tileSouth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_S_W, overrideTerrainCheckType);
            }

            if ((!tileSouth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_W, overrideTerrainCheckType);
            }

            if ((!tileSouth.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_E, overrideTerrainCheckType);
            }

            if ((tileSouth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (!tileNorth.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_S, overrideTerrainCheckType);
            }

            if ((!tileSouth.CheckTag(terrainType2)) && (!tileEast.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)) && (!tileWest.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N, overrideTerrainCheckType);
            }

            if ((tileSouth.CheckTag(terrainType2)) && (tileWest.CheckTag(terrainType2)) && (tileEast.CheckTag(terrainType2)) && (tileNorth.CheckTag(terrainType2)))
            {
                checkTile.SetTerrainTileType(terrainType, VisualTileType.WALL_N_E_S_W, overrideTerrainCheckType);
            }
        }
    }

    public MapTileData GetRandomNonCollidableTile(Vector2 center, int radius, bool expandSearch, bool preferClose)
    {
        return GetRandomEmptyTile(center, radius, preferClose, anyNonCollidable: true);
    }

    /// <summary>
    /// Searches around Center and returns a tile that does not collide with the hero. This includes searching through
    /// the actors in the tile. 
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius">The intial search radius. If there are no empty tiles in this radius, the radius will
    /// expand until a tile is found or the radius exceeds the size of the map.</param>
    /// <param name="preferClose">Set this flag to return a tile close to the center.</param>
    /// <param name="anyNonCollidable"></param>
    /// <param name="preferLOS"></param>
    /// <param name="avoidTilesWithPowerups"></param>
    /// <param name="excludeCenterTile"></param>
    /// <returns>A non-collidable tile in the map given the parameters. If absolutely no empty tiles were found,
    /// this function returns the center tile.</returns>
    public MapTileData GetRandomEmptyTile(Vector2 center, int radius, bool preferClose, bool anyNonCollidable = false,
        bool preferLOS = false, bool avoidTilesWithPowerups = false, bool excludeCenterTile = false)
    {
        //pool_tileList = GetNonCollidableTilesAroundPoint(center, radius, GameMasterScript.heroPCActor);
        CustomAlgorithms.GetNonCollidableTilesAroundPoint(center, radius, GameMasterScript.heroPCActor, this);

        bool possibleTile = true;
        bool foundTile = false;

        MapTileData best = null;

        MapTileData possibleButNotLOS = null;

        float shortest = 999f;

        int iterations = 0;

        while (!foundTile)
        {
            iterations++;

            //We have to return something, this function is critical for map loading and if we don't, we can hang.
            if (iterations > 500)
            {
                Debug.Log("Couldn't find any possible empty tile on floor " + floor + " " + radius);
                return GetTile(center);
            }

            if (CustomAlgorithms.numNonCollidableTilesInBuffer == 0)
            {
                possibleTile = false;
            }

            //Allow this function to fail if it examines the whole map and comes back with no empty tile.
            while (!possibleTile && 
                   (radius < columns || radius < rows ))
            {
                radius++;
                CustomAlgorithms.GetNonCollidableTilesAroundPoint(center, radius, GameMasterScript.heroPCActor, this);
                if (CustomAlgorithms.numNonCollidableTilesInBuffer > 0)
                {
                    possibleTile = true;
                    break;
                }
            }

            //We have to return something, this function is critical for map loading and if we don't, we can hang.
            if (!possibleTile)
            {
                Debug.LogError("Not one empty tile was found in map " + GetName() + ". ");
                return GetTile(center);
            }

            if (preferClose)
            {
                for (int x = 0; x < CustomAlgorithms.numNonCollidableTilesInBuffer; x++)
                {
                    if (!anyNonCollidable && !CustomAlgorithms.nonCollidableTileBuffer[x].IsEmpty()) continue;
                    if (avoidTilesWithPowerups && CustomAlgorithms.nonCollidableTileBuffer[x].CheckForSpecialMapObjectType(SpecialMapObject.POWERUP)) continue;

                    if (excludeCenterTile && CustomAlgorithms.nonCollidableTileBuffer[x].pos == center) continue;

                    float dist = MapMasterScript.GetGridDistance(CustomAlgorithms.nonCollidableTileBuffer[x].pos, center);
                    if (dist < shortest)
                    {
                        //if (!MapMasterScript.CheckTileToTileLOS(CustomAlgorithms.nonCollidableTileBuffer[x].pos, center, GameMasterScript.heroPCActor, this))
                        if (!CustomAlgorithms.CheckBresenhamsLOS(CustomAlgorithms.nonCollidableTileBuffer[x].pos, center, this))
                        {
                            dist *= 1.5f;
                            possibleButNotLOS = CustomAlgorithms.nonCollidableTileBuffer[x];
                            if (preferLOS)
                            {
                                dist *= 1.5f;
                            }
                        }
                        if (dist < shortest)
                        {
                            shortest = dist;
                            best = CustomAlgorithms.nonCollidableTileBuffer[x];
                        }
                    }
                }
                if (best != null)
                {
                    return best;
                }
            }

            if (!preferClose)
            {
                pool_tileList.Clear();
                for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
                {
                    pool_tileList.Add(CustomAlgorithms.nonCollidableTileBuffer[i]);
                }
                pool_tileList.Shuffle();
                for (int i = 0; i < pool_tileList.Count; i++)
                {
                    if (avoidTilesWithPowerups && pool_tileList[i].CheckForSpecialMapObjectType(SpecialMapObject.POWERUP)) continue;

                    if (pool_tileList[i].IsEmpty() || anyNonCollidable)
                    {
                        foundTile = true;
                        return pool_tileList[i];
                    }
                }
            }

            radius++;
            CustomAlgorithms.GetNonCollidableTilesAroundPoint(center, radius, GameMasterScript.heroPCActor, this);
        }

        return null;
    }

    public Stairs GetStairsUp()
    {
        foreach (Stairs st in mapStairs)
        {
            if (st.stairsUp)
            {
                return st;
            }
        }
        return null;
    }

    public Stairs GetStairsDown()
    {
        foreach (Stairs st in mapStairs)
        {
            if (!st.stairsUp)
            {
                return st;
            }
        }
        return null;
    }

    public void SeedMapWithMonsters(int minMonsters, int maxMonsters, ItemWorldMetaData itemWorldProperties)
    {
        InitializeMonsterSeedStuff();
        
        Vector2 monsterRange = MonsterSeedSpawnSetup(minMonsters, maxMonsters, itemWorldProperties);
        minMonsters = (int)monsterRange.x;
        maxMonsters = (int)monsterRange.y;

        int ngPlusValue = 0;
        if (GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
        {
            ngPlusValue = GameStartData.NewGamePlus;
        }

        int localMinMonsters = minMonsters + (ngPlusValue * 3);
        int localMaxMonsters = minMonsters + (ngPlusValue * 3);

        bool monstersSpawnWithLoot = ShouldSpawnWithLootOnThisFloor();

        if (dungeonLevelData.evenMonsterDistribution)
        {
            SpawnMonstersWithEvenDistribution(localMinMonsters, localMaxMonsters, monstersSpawnWithLoot, itemWorldProperties);
        }
        else
        {
            SpawnMonstersWithRoomBasedDistribution(localMinMonsters, localMaxMonsters, monstersSpawnWithLoot, itemWorldProperties);
        }



        if (initialSpawnedMonsters < minMonsters)
        {
            int diff = minMonsters - initialSpawnedMonsters;
            initialSpawnedMonsters = minMonsters;
            int failsInARow = 0;
            for (int i = 0; i < diff; i++)
            {
                Monster check = SpawnRandomMonster(false, false);
                if (check == null)
                {
                    i--;
                    failsInARow++;
                }
                else
                {
                    failsInARow = 0;
                }
                if (failsInARow >= 20)
                {
                    Debug.Log("Tried at least 20 times to place monster.");
                    i = diff;
                }
            }
        }
        if (initialSpawnedMonsters > maxMonsters)
        {
            int count = 0;
            //Debug.Log("While4");
            while (initialSpawnedMonsters > maxMonsters)
            {
                count++;
                Monster mn = monstersThisMap[UnityEngine.Random.Range(0, monstersThisMap.Count)];
                RemoveActorFromLocation(mn.GetPos(), mn);
                RemoveActorFromMap(mn);
                monstersThisMap.Remove(mn);
                initialSpawnedMonsters--;
            }
            //Debug.Log("Removed " + count + " monsters from floor " + floor);
        }
    }

    protected void SetMapBoundVisuals()
    {
        MapTileData checkTile;
        for (int i = 0; i < columns; i++)
        {
            for (int e = 0; e < rows; e++)
            {
                checkTile = mapArray[i, e];
                if (i == 0 || i == columns - 1)
                {
                    // Leftmost or rightmost edge

                    checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
                }
                else
                {
                    if (e == 0 || e == rows - 1)
                    {
                        // Bottom or top edge
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
                    }
                }
                if (checkTile.tileType == TileTypes.NOTHING)
                {
                    if (dungeonLevelData.layoutType != DungeonFloorTypes.CAVEROOMS && dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS)
                    {
                        checkTile.ChangeTileType(TileTypes.WALL, mgd);
                    }
                    else if (InBounds(checkTile.pos))
                    {
                        checkTile.ChangeTileType(TileTypes.GROUND, mgd);
                    }
                }
            }
        }
    }

    void CorrectForDiagonalMovement()
    {
        List<DungeonFloorTypes> layoutTypesThatShouldNotBeCorrected = new List<DungeonFloorTypes>()
        {
            DungeonFloorTypes.SPECIAL,
            DungeonFloorTypes.AUTOCAVE,
            DungeonFloorTypes.LAKE,
            DungeonFloorTypes.MAZEROOMS,
            DungeonFloorTypes.MAZE,
            DungeonFloorTypes.VOLCANO,
            DungeonFloorTypes.BSPROOMS,
            DungeonFloorTypes.KEEP
        };

        if (dungeonLevelData.GetMetaData("no_diagonal_correction") == 1)
        {
            return;
        }

        if (layoutTypesThatShouldNotBeCorrected.Contains(dungeonLevelData.layoutType))
        {
            return;
        }

        MapTileData checkTile;
        MapTileData tileSouth;
        MapTileData tileNorth;
        MapTileData tileEast;
        MapTileData tileWest;
        // Diagonal checker.

        MapTileData tileNorthEast;
        MapTileData tileNorthWest;
        MapTileData tileSouthEast;
        MapTileData tileSouthWest;
        
        for (int x = 1; x < columns - 1; x++) // was 0, <=
        {
            for (int y = 1; y < rows - 1; y++)
            {
                checkTile = mapArray[x, y];
                if (checkTile.CheckTag(LocationTags.WATER))
                {
                    continue;
                }
                if (checkTile.CheckTag(LocationTags.SECRET))
                {
                    continue;
                }
                if (checkTile.tileType == TileTypes.GROUND)
                {
                    if (UnityEngine.Random.Range(0, 1f) > dungeonLevelData.correctDiagonalChance) continue;

                    // Make sure this tile has no "diagonal only" movement.
                    tileNorth = null;
                    tileSouth = null;
                    tileEast = null;

                    tileNorth = mapArray[x, y + 1];
                    tileSouth = mapArray[x, y - 1];
                    tileEast = mapArray[x + 1, y];

                    /* try { tileNorth = mapArray[x, y + 1]; } // Tile that is NORTH of the checked tile
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.Log("Out of range North " + CheckMTDArea(checkTile) + " " + checkTile.pos + " " + dungeonLevelData.layoutType + " " + floor);
                        Debug.Log(e);
                    }
                    try { tileSouth = mapArray[x, y - 1]; }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.Log("Out of range South " + CheckMTDArea(checkTile) + " " + checkTile.pos + " " + dungeonLevelData.layoutType + " " + floor);
                        Debug.Log(e);
                    }
                    try { tileEast = mapArray[x + 1, y]; }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.Log("Out of range East " + CheckMTDArea(checkTile) + " " + checkTile.pos + " " + dungeonLevelData.layoutType + " " + floor);
                        Debug.Log(e);
                    } */
                    tileWest = mapArray[x - 1, y];
                    tileNorthEast = mapArray[x + 1, y + 1];
                    tileNorthWest = mapArray[x - 1, y + 1];
                    tileSouthEast = mapArray[x + 1, y - 1];
                    tileSouthWest = mapArray[x - 1, y - 1];

                    // OXO
                    // XOX
                    // OXO

                    if ((tileNorthWest.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.WALL) && (tileNorth.tileType == TileTypes.WALL))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!tileWest.CheckTag(LocationTags.SECRET))
                            {
                                tileWest.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                        else
                        {
                            if (!tileNorth.CheckTag(LocationTags.SECRET))
                            {
                                tileNorth.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }

                    }
                    if ((tileNorthEast.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.WALL) && (tileNorth.tileType == TileTypes.WALL))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!tileEast.CheckTag(LocationTags.SECRET))
                            {
                                tileEast.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                        else
                        {
                            if (!tileNorth.CheckTag(LocationTags.SECRET))
                            {
                                tileNorth.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                    }
                    if ((tileSouthEast.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.WALL) && (tileSouth.tileType == TileTypes.WALL))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!tileEast.CheckTag(LocationTags.SECRET))
                            {
                                tileEast.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                        else
                        {
                            if (!tileSouth.CheckTag(LocationTags.SECRET))
                            {
                                tileSouth.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                    }
                    if ((tileSouthWest.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.WALL) && (tileSouth.tileType == TileTypes.WALL))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!tileWest.CheckTag(LocationTags.SECRET))
                            {
                                tileWest.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                        else
                        {
                            if (!tileSouth.CheckTag(LocationTags.SECRET))
                            {
                                tileSouth.ChangeTileType(TileTypes.GROUND, mgd);
                                x = 1;
                                y = 1;
                            }
                        }
                    }
                }
            }
        }
        
    }

    protected void AddWallChunkDecor()
    {
        MapTileData checkForGrass;

        bool[,] invalidForChunk = new bool[columns, rows];
        bool[,] usedForChunk = new bool[columns, rows];
        MapTileData checkForChunk = null;
        bool cancel = false;

        bool validFor3x3Chunk = false;
        bool validFor2x2Chunk = false;

        if (dungeonLevelData.tileVisualSet == TileSet.EARTH)
        {
            validFor3x3Chunk = true;
            validFor2x2Chunk = true;
        }
        else if (dungeonLevelData.tileVisualSet == TileSet.STONE)
        {
            validFor2x2Chunk = true;
        }
        else if (dungeonLevelData.tileVisualSet == TileSet.SLATE)
        {
            validFor2x2Chunk = true;
        }

        if ((validFor3x3Chunk) && (dungeonLevelData.layoutType != DungeonFloorTypes.RUINS) && (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL))
        {
            for (int x = 0; x < columns - 5; x++)
            {
                for (int y = 0; y < rows - 5; y++)
                {
                    if (invalidForChunk[x, y]) continue;
                    if (usedForChunk[x, y]) continue;
                    checkForGrass = mapArray[x, y];
                    if (checkForGrass.tileType != TileTypes.WALL)
                    {
                        invalidForChunk[x, y] = true;
                        continue;
                    }

                    cancel = false;

                    for (int x2 = x; x2 < x + 5; x2++)
                    {
                        for (int y2 = y; y2 < y + 5; y2++)
                        {
                            checkForChunk = mapArray[x2, y2];
                            if ((checkForChunk.tileType != TileTypes.WALL) || (usedForChunk[x2, y2]))
                            {
                                invalidForChunk[x2, y2] = true;
                                cancel = true;
                                break;
                            }
                        }
                        if (cancel) break;
                    }

                    if (cancel) continue;

                    //Debug.Log("Floor " + floor + " Spawning decor chunk at " + x + "," + y + " checking up to " + (x+4) +"," + (y+4));

                    // Either the original tile is valid or not!
                    checkForGrass = mapArray[x + 2, y + 2];
                    checkForGrass.AddMapTag(MapGenerationTags.WALLDECOR3X3START);
                    for (int x2 = x; x2 < x + 5; x2++)
                    {
                        for (int y2 = y; y2 < y + 5; y2++)
                        {
                            usedForChunk[x2, y2] = true;
                        }
                    }
                }
            }
        }

        cancel = false;
        if ((validFor2x2Chunk) && (dungeonLevelData.layoutType != DungeonFloorTypes.RUINS) && (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL))
        {
            for (int x = 0; x < columns - 3; x++)
            {
                for (int y = 0; y < rows - 3; y++)
                {
                    if (invalidForChunk[x, y]) continue;
                    if (usedForChunk[x, y]) continue;
                    checkForGrass = mapArray[x, y];
                    if (checkForGrass.tileType != TileTypes.WALL)
                    {
                        invalidForChunk[x, y] = true;
                        continue;
                    }

                    cancel = false;

                    for (int x2 = x; x2 < x + 3; x2++)
                    {
                        for (int y2 = y; y2 < y + 3; y2++)
                        {
                            checkForChunk = mapArray[x2, y2];
                            if ((checkForChunk.tileType != TileTypes.WALL) || (usedForChunk[x2, y2]))
                            {
                                invalidForChunk[x2, y2] = true;
                                cancel = true;
                                break;
                            }
                        }
                        if (cancel) break;
                    }

                    if (cancel) continue;

                    //Debug.Log("Floor " + floor + " Spawning decor chunk at " + x + "," + y + " checking up to " + (x+4) +"," + (y+4));

                    // Either the original tile is valid or not!
                    checkForGrass = mapArray[x + 1, y + 1];
                    checkForGrass.AddMapTag(MapGenerationTags.WALLDECOR2X2START);
                    for (int x2 = x; x2 < x + 3; x2++)
                    {
                        for (int y2 = y; y2 < y + 3; y2++)
                        {
                            usedForChunk[x2, y2] = true;
                        }
                    }
                }
            }
        }
    }

    protected virtual void AddDecorAndFlowersAndTileDirections()
    {
        MapTileData checkTile;
        MapTileData tileSouth;
        MapTileData tileNorth;
        MapTileData tileEast;
        MapTileData tileWest;
        for (int i = 0; i <= columns - 1; i++) // i is the column
        {
            for (int e = 0; e <= rows - 1; e++) // e is the row
            {
                checkTile = mapArray[i, e];

                // Check if there's any decor needed. This can be improved.

                // This is both WALL decor and GROUND decor depending on tile type.
                if (UnityEngine.Random.Range(0, 1f) <= mgd.anyDecorChance && 
                    dungeonLevelData.tileVisualSet != TileSet.VOID && dungeonLevelData.tileVisualSet != TileSet.FUTURE && 
                    dungeonLevelData.tileVisualSet != TileSet.VOLCANO && dungeonLevelData.tileVisualSet != TileSet.REINFORCED 
                    && dungeonLevelData.tileVisualSet != TileSet.NIGHTMARISH && dungeonLevelData.tileVisualSet != TileSet.TREETOPS && 
                    dungeonLevelData.tileVisualSet != TileSet.MOUNTAINGRASS)
                {
                    if (!checkTile.CheckTag(LocationTags.GRASS))
                    {
                        if (checkTile.tileType == TileTypes.GROUND)
                        {

                        }
                        else
                        {
                            if (dungeonLevelData.tileVisualSet != TileSet.BLUESTONELIGHT && dungeonLevelData.tileVisualSet != TileSet.BLUESTONEDARK && dungeonLevelData.tileVisualSet != TileSet.SAND &&
                                dungeonLevelData.tileVisualSet != TileSet.RUINED)
                            {
                                checkTile.SetDecor();
                            }
                        }

                    }
                }

                // Flowers
                if ((UnityEngine.Random.Range(0, 1f) <= mgd.grassDecorChance && checkTile.CheckTag(LocationTags.GRASS)) 
                    && dungeonLevelData.tileVisualSet != TileSet.FUTURE && dungeonLevelData.tileVisualSet != TileSet.REINFORCED)
                {
                    // For some reason, flower decor is causing rendering errors on Switch...?
#if !UNITY_SWITCH
                        checkTile.SetDecor();
#endif
                }

                // This block is for the ground method.
                if (i == 0 && e >= 0)
                {
                    // West wall

                    if ((i == 0) && (e == 0))
                    {
                        // Southwest corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E);
                    }
                    else if ((i == 0) && (e == rows - 1))
                    {
                        // Northwest corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_E_S);
                    }
                    else
                    {
                        tileWest = mapArray[i + 1, e];
                        if (tileWest.tileType == TileTypes.WALL)
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S);
                        }
                        else
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_N_S);
                        }
                    }

                    continue;
                }
                if (i >= 0 && e == 0)
                {
                    // South wall

                    if (i == 0 && e == 0)
                    {
                        // Southwest corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E);
                    }
                    else if ((i == columns - 1) && (e == 0))
                    {
                        // Southeast corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_W);
                    }
                    else
                    {
                        tileNorth = mapArray[i, e + 1];
                        if (tileNorth.tileType == TileTypes.WALL)
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_N_E_W);
                        }
                        else
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_E_W);
                        }
                    }
                    continue;
                }
                if ((i >= 0) && (e == rows - 1))
                {

                    // North wall
                    if ((i == 0) && (e == rows - 1))
                    {
                        // Northwest corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_E_S);
                    }
                    else if ((i == columns - 1) && (e == rows - 1))
                    {
                        // Northeast corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_S_W);
                    }
                    else
                    {
                        tileSouth = mapArray[i, e - 1];
                        if (tileSouth.tileType == TileTypes.WALL)
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_E_S_W);
                        }
                        else
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_E_W);
                        }
                    }

                    continue;
                }
                if ((i == columns - 1) && (e >= 0))
                {
                    // East wall

                    if ((i == columns - 1) && (e == rows - 1))
                    {
                        // Northeast corner of map
                        checkTile.SetTileVisualType(VisualTileType.WALL_S_W);
                    }
                    else if ((i == columns - 1) && (e == 0))
                    {
                        // Southeast corner
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_W);
                    }
                    else
                    {
                        tileWest = mapArray[i - 1, e];
                        if (tileWest.tileType == TileTypes.WALL)
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_N_S_W);
                        }
                        else
                        {
                            checkTile.SetTileVisualType(VisualTileType.WALL_N_S);
                        }
                    }

                    continue;
                }


                if ((checkTile.tileType == TileTypes.GROUND) && (checkTile.visualTileType == VisualTileType.NOTSET))
                {
                    // This short check assumes all ground is the same, and we adjust walls to match borders instead. The bypassed code is for GROUND variations.

                    checkTile.SetTileVisualType(VisualTileType.GROUND);
                    continue;

                }
                // Beautify wall tiles

                if ((checkTile.tileType == TileTypes.NOTHING) || (checkTile.tileType == TileTypes.WALL))
                {
                    tileNorth = mapArray[i, e + 1]; // Tile that is NORTH of the checked tile
                    tileSouth = mapArray[i, e - 1];
                    tileEast = mapArray[i + 1, e];
                    tileWest = mapArray[i - 1, e];
                    checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);

                    if ((tileSouth.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // XoX
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
                    }

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // .o.
                        // *.*

                        checkTile.SetTileVisualType(VisualTileType.WALL_NONE);
                    }

                    if (checkTile.visualTileType != VisualTileType.NOTSET)
                    {
                        //continue;
                    }

                    // This is a wall tile.

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND))
                    {
                        // *.*
                        // XoX
                        // *.*

                        checkTile.SetTileVisualType(VisualTileType.WALL_E_W);
                    }

                    if ((tileSouth.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
                    {
                        // *X*
                        // .o.
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_S);
                    }

                    if ((tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // Xo.
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_S_W);
                    }

                    if ((tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // .oX
                        // *.*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E);
                    }

                    if ((tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // Xo.
                        // *.*
                        checkTile.SetTileVisualType(VisualTileType.WALL_N_W);
                    }

                    if ((tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // .oX
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_E_S);
                    }

                    //  Non-Corners

                    if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType == TileTypes.GROUND))
                    {
                        // *X*
                        // XoX
                        // *.*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E_W);
                    }

                    if ((tileNorth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
                    {
                        // *.*
                        // XoX
                        // *X*
                        checkTile.SetTileVisualType(VisualTileType.WALL_E_S_W);
                    }

                    if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // .oX
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S);
                    }

                    if ((tileNorth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileSouth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // Xo.
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_S_W);
                    }

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND))
                    {
                        // *.*
                        // Xo.
                        // *.*
                        // Easternmost bit of wall
                        checkTile.SetTileVisualType(VisualTileType.WALL_W);
                    }

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // .oX
                        // *.*
                        // Westernmost bit of wall
                        checkTile.SetTileVisualType(VisualTileType.WALL_E);
                    }

                    if ((tileSouth.tileType != TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // .o.
                        // *X*
                        // Northernmost bit of wall
                        checkTile.SetTileVisualType(VisualTileType.WALL_S);
                    }

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND))
                    {
                        // *X*
                        // .o.
                        // *.*
                        // Southernmost bit of wall
                        checkTile.SetTileVisualType(VisualTileType.WALL_N);
                    }

                    if ((tileSouth.tileType != TileTypes.GROUND) && (tileWest.tileType != TileTypes.GROUND) && (tileEast.tileType != TileTypes.GROUND) && (tileNorth.tileType != TileTypes.GROUND))
                    {
                        // *X*
                        // XoX
                        // *X*

                        checkTile.SetTileVisualType(VisualTileType.WALL_N_E_S_W);
                    }

                    if ((tileSouth.tileType == TileTypes.GROUND) && (tileWest.tileType == TileTypes.GROUND) && (tileEast.tileType == TileTypes.GROUND) && (tileNorth.tileType == TileTypes.GROUND))
                    {
                        // *.*
                        // .o.
                        // *.*

                        checkTile.SetTileVisualType(VisualTileType.WALL_NONE);
                    }
                }

            }
        } // End decor gen.
    }

    protected void AddEnvironmentDestructiblesToMap()
    {
        MapTileData checkTile;
        for (int x = 0; x < columns - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                checkTile = mapArray[x, y];
                if (checkTile.CheckTag(LocationTags.WATER) || checkTile.CheckTag(LocationTags.ISLANDSWATER))
                {
                    if (!checkTile.CheckTag(LocationTags.ISLANDSWATER))
                    {
                        PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_rivertile");
                    }
                    else
                    {
                        PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_voidtile");

                    }

                    if (((dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS) && (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL)) && (dungeonLevelData.allowPlanks))
                    {
                        if ((UnityEngine.Random.Range(0, 1f) <= 0.1f) && (checkTile.tileType == TileTypes.GROUND)) // Plank spawn chance
                        {
                            PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_plank");
                        }
                    }
                }
                if (checkTile.CheckTag(LocationTags.LAVA))
                {
                    PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_lavatile");
                }
                if (checkTile.CheckTag(LocationTags.MUD))
                {
                    PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_mudtile");
                }
                if (checkTile.CheckTag(LocationTags.LASER))
                {
                    PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_phasmashieldtile");
                }
                if (checkTile.CheckTag(LocationTags.ELECTRIC))
                {
                    if (checkTile.CheckActorRef("obj_voidtile"))
                    {
                        Debug.LogError("Tried to add a conduit to a tile with deadly void in it, location " + checkTile.pos);
                    }
                    PlaceDestructibleDuringAddTerrainToMap(checkTile, "obj_electile");
                }
            }
        }
    }

    void PlaceDestructibleDuringAddTerrainToMap(MapTileData checkTile, string strTemplate)
    {
        //Destructible act = new Destructible();
        Destructible act = DTPooling.GetDestructible();
        Destructible template = Destructible.FindTemplate(strTemplate);
        act.CopyFromTemplate(template);
        act.SetSpawnPosXY((int)checkTile.pos.x, (int)checkTile.pos.y);
        act.SetUniqueIDAndAddToDict(); // This is new.
        AddActorToLocation(checkTile.pos, act);
        AddActorToMap(act);
    }

    void AddObjectsFromRoomTemplates(ItemWorldMetaData itemWorldProperties)
    {
        MapTileData checkTile;
        for (int x = 0; x < columns; x++) // Was 1 to columns -2. Messed with handcrafted mapgen?
        {
            for (int y = 0; y < rows; y++)
            {
                checkTile = mapArray[x, y];
                if ((checkTile.CheckMapTag(MapGenerationTags.MAPGEN_BARREL)) && (!checkTile.CheckMapTag(MapGenerationTags.MAPGEN_TREASURE1)))
                {
                    // Todo - better way of picking random destructibles
                    int roll = UnityEngine.Random.Range(0, 3);

                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        if (roll == 0)
                        {
                            CreateDestructibleInTile(checkTile, "obj_barrel");
                        }
                        else if (roll == 1)
                        {
                            CreateDestructibleInTile(checkTile, "obj_crate");
                        }
                        else
                        {
                            CreateDestructibleInTile(checkTile, "obj_cratestack");
                        }
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_shinymetalorb");
                    }

                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_TREASURE1))
                {
                    CreateTreasureInTile(checkTile);
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST1))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_smallwoodencrate");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_shinymetalorb");
                    }
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST2))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_mediumwoodencrate");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_shinymetalorb");
                    }
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST3))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_largewoodencrate");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_shinymetalorb");
                    }
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST4))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_smallwoodenchest");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_smallmetalchest");
                    }

                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST5))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_largewoodenchest");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_largemetalchest");
                    }
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHEST6))
                {
                    if (dungeonLevelData.tileVisualSet != TileSet.FUTURE)
                    {
                        CreateDestructibleInTile(checkTile, "obj_ornatechest");
                    }
                    else
                    {
                        CreateDestructibleInTile(checkTile, "obj_shinymetalchest");
                    }
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_MONSTER))
                {
                    Monster mon = CreateMonsterInTile(checkTile);
                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHAMP1))
                {
                    Monster mon = CreateMonsterInTile(checkTile);
                    championCount++;
                    if (!itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS]) mon.MakeChampion();

                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHAMP2))
                {
                    Monster mon = CreateMonsterInTile(checkTile);
                    championCount++;
                    if (!itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS])
                    {
                        mon.MakeChampion();
                        mon.MakeChampion();
                    }

                }
                if (checkTile.CheckMapTag(MapGenerationTags.MAPGEN_CHAMP3))
                {
                    Monster mon = CreateMonsterInTile(checkTile);
                    championCount++;
                    if (!itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS])
                    {
                        mon.MakeChampion();
                        mon.MakeChampion();
                        mon.MakeChampion();
                    }
                }
            }
        }
    }

    void AddJournalEntriesToMap()
    {
        if (GameMasterScript.masterJournalEntryList.Count > 0)
        {
            List<Conversation> possibleConvos = new List<Conversation>();
            foreach (Conversation conv in GameMasterScript.masterJournalEntryList)
            {
                if (conv.challengeValue <= challengeRating)
                {
                    bool validEntries = true;
                    foreach (int reqEntry in conv.reqEntries)
                    {
                        if (!MetaProgressScript.journalEntriesRead.Contains(reqEntry))
                        {
                            validEntries = false;
                            break;
                        }
                    }
                    if (!validEntries) continue;
                    if (!MetaProgressScript.journalEntriesRead.Contains(conv.journalEntry))
                    {
                        possibleConvos.Add(conv);
                    }
                }
            }
            possibleConvos.Shuffle();
            if (possibleConvos.Count > 0)
            {
                Conversation select = possibleConvos[0];
                MapTileData mtd = GetRandomEmptyTileForMapGen();
                if (mtd != null)
                {
                    Destructible page = CreateDestructibleInTile(mtd, "obj_journalpage");
                    page.extraActorReference = select.refName;
                    GameMasterScript.masterJournalEntryList.Remove(select);
                    //Debug.Log("Spawned page in map " + floor + " at " + mtd.pos);
                }
            }
        }
    }

    bool AddPandoraBoxToMap()
    {
        int numBoxes = 1;
        if (GameStartData.CheckGameModifier(GameModifiers.MULTI_PANDORA))
        {
            numBoxes = 3;
        }
        else if (GameStartData.CheckGameModifier(GameModifiers.NO_PANDORA))
        {
            numBoxes = 0;
        }

        if (dungeonLevelData.GetMetaData("no_pandora") == 1)
        {
            numBoxes = 0;
        }

            if (GameMasterScript.heroPCActor == null)
            {
                Debug.Log("Hero is null for pandora box adder!");
                return false;
            }

        for (int i = 0; i < numBoxes; i++)
        {
            Destructible spawnerTemplate = Destructible.FindTemplate("obj_monsterspawner");
            Destructible spawner = DTPooling.GetDestructible();
            spawner.CopyFromTemplate(spawnerTemplate);
            spawner.SetUniqueIDAndAddToDict();
            spawner.dungeonFloor = floor;

            Room randomRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];

            int nTries = 0;
            //Debug.Log("While6");
            while ((randomRoom.internalTiles.Count == 0) && (nTries < 500))
            {
                nTries++;
                randomRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
            }

            if (nTries >= 500)
            {
                Debug.Log("No valid rooms for monster spawner!");
                return false;
            }

            MapTileData spawnerMTD = randomRoom.internalTiles[UnityEngine.Random.Range(0, randomRoom.internalTiles.Count)];

            if (spawnerMTD == null) 
            {
                Debug.Log("No valid tiles for monster spawner!");
                return false;
            }

            nTries = 0;

            bool tileValidForPandoraBox = false;

            nTries = 0;
            while (!tileValidForPandoraBox)
            {
                if (nTries > 1000)
                {
                    Debug.Log("Pandora placement " + floor + " failed.");
                    return false;
                }
                randomRoom = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                if (randomRoom.internalTiles.Count == 0) { continue; }
                spawnerMTD = randomRoom.internalTiles[UnityEngine.Random.Range(0, randomRoom.internalTiles.Count)];
                tileValidForPandoraBox = true;
                if (spawnerMTD.IsCollidable(GameMasterScript.heroPCActor))
                {
                    tileValidForPandoraBox = false;
                }
                else if (spawnerMTD.pos == heroStartTile)
                {
                    tileValidForPandoraBox = false;
                }
                else if (spawnerMTD.CheckTag(LocationTags.SECRET))
                {
                    tileValidForPandoraBox = false;
                }
                else if (spawnerMTD.CheckTag(LocationTags.WATER))
                {
                    tileValidForPandoraBox = false;
                }
                else if (spawnerMTD.IsDangerous(GameMasterScript.heroPCActor))
                {
                    tileValidForPandoraBox = false;
                }
                else
                {
                    foreach (Stairs st in mapStairs)
                    {
                        if (MapMasterScript.GetGridDistance(spawnerMTD.pos, st.GetPos()) <= 3)
                        {
                            tileValidForPandoraBox = false;
                            break;
                        }
                    }
                }

            }

            if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.PANDORA_BASE_LEGENDARY_CHANCE)
            {
                Item leg = LootGeneratorScript.GenerateLootFromTable(challengeRating, 0f, "legendary");
                if (leg != null)
                {
                    spawner.myInventory.AddItemRemoveFromPrevCollection(leg, false);
                }
            }

            if (SharaModeStuff.IsSharaModeActive())
            {
                Item sharaGear = LootGeneratorScript.GenerateLootFromTable(challengeRating + 0.1f, 3f, "equipment");
                Equipment eq = sharaGear as Equipment;
                int guaranteedMods = 2;
                if (challengeRating >= 1.4f) guaranteedMods++;
                if (challengeRating >= 1.7f) guaranteedMods++;
                for (int x = eq.GetNonAutomodCount(); x <= guaranteedMods; x++)
                {
                    EquipmentBlock.MakeMagical(eq, challengeRating, false);
                }
                spawner.myInventory.AddItem(sharaGear, false);
                //Debug.Log("Generated shara gear: " + eq.displayName);
            }

            spawner.SetSpawnPosXY((int)spawnerMTD.pos.x, (int)spawnerMTD.pos.y);
            AddActorToLocation(spawnerMTD.pos, spawner);
            AddActorToMap(spawner);
        }

        return true;
    }

    public ActorTable GetBreakablesForThisFloor()
    {
        ActorTable breakables = new ActorTable();

        ActorTable baseTable = GameMasterScript.masterBreakableSpawnTable;
        foreach (Actor act in baseTable.actors)
        {
            Destructible dt = act as Destructible;
            if (dt.spawnInVisualSet[(int)dungeonLevelData.tileVisualSet])
            {
                breakables.AddToTable(dt.actorRefName, baseTable.table[dt.actorRefName]);
                breakables.actors.Add(dt);
                //Debug.Log("Possible breakable this floor " + floor + " " + dt.actorRefName + " " + breakablesForThisFloor.table[dt.actorRefName]);
            }
        }

        if (breakables.actors.Count == 0)
        {
            Debug.Log("No possible breakables floor " + floor + " set " + dungeonLevelData.tileVisualSet);
        }

        return breakables;
    }

    protected void AddBreakablesFountainsAndSparklesToMap()
    {
        MapTileData checkTile;

        ActorTable breakablesForThisFloor = GetBreakablesForThisFloor();

        bool mysteryDungeonWithExtraBreakables = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
            && GameMasterScript.gameLoadSequenceCompleted 
            && GameMasterScript.gmsSingleton.ReadTempGameData("creatingmysterydungeon") == 1 
            && MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.EXTRA_ITEMS_BREAKABLES);

        bool mysteryDungeon = MysteryDungeonManager.InOrCreatingMysteryDungeon();

        float localBreakableChance = MapMasterScript.CHANCE_RANDOM_BREAKABLE;

        if (mysteryDungeonWithExtraBreakables) localBreakableChance += MysteryDungeon.CHANCE_ADDITIONAL_BREAKABLES_ON_MAP;

        checkTile = null;
        for (int x = 1; x < columns - 2; x++)
        {
            for (int y = 1; y < rows - 2; y++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= localBreakableChance) // Barrel, random destructible chance.
                {
                    checkTile = mapArray[x, y];
                    if (checkTile.CheckTag(LocationTags.SECRET)) continue;
                    if ((checkTile.CheckTag(LocationTags.CORRIDOR)) || (checkTile.CheckTag(LocationTags.LAVA)) || (checkTile.CheckTag(LocationTags.ELECTRIC)) || (checkTile.CheckTag(LocationTags.WATER)) || (checkTile.CheckTag(LocationTags.MUD)) || (checkTile.CheckTag(LocationTags.CORRIDORENTRANCE)) || (checkTile.tileType != TileTypes.GROUND))
                    {
                        continue;
                    }
                    if (checkTile.tileType != TileTypes.GROUND) continue;
                    if (checkTile.CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER)) continue;
                    if (checkTile.pos == heroStartTile) continue;
                    if (checkTile.CheckTrueTag(LocationTags.ISLANDSWATER)) continue; // Special case to avoid overlap with planks
                    if (checkTile.CheckTrueTag(LocationTags.WATER)) continue; // Special case to avoid overlap with planks
                    if (checkTile.IsCollidable(GameMasterScript.heroPCActor))
                    {
                        continue;
                    }
                    if (checkTile.GetStairsInTile() != null)
                    {
                        continue;
                    }

                    // Todo - better way of picking random destructibles

                    if (breakablesForThisFloor.actors.Count > 0)
                    {
                        string template = breakablesForThisFloor.GetRandomActorRef();
                        CreateDestructibleInTile(checkTile, template, mysteryDungeon, mysteryDungeonWithExtraBreakables);
                    }
                }
            }
        }

        int numFountains = 2 + (floor / 3);

        if (floor >= 100)
        {
            if (dungeonLevelData.size <= 32)
            {
                numFountains = 1;
                //numFountains = UnityEngine.Random.Range(1, 3);
            }
            else if (dungeonLevelData.size > 32 && dungeonLevelData.size <= 40)
            {
                numFountains = 2;
                //numFountains = UnityEngine.Random.Range(2, 4);
            }
            else
            {
                numFountains = 3;
                //numFountains = UnityEngine.Random.Range(3, 5);
            }
        }

        if (numFountains > 5)
        {
            numFountains = 5;
        }

        if (IsMysteryDungeonMap())
        {
            numFountains += 2;
        }

        if (SharaModeStuff.IsSharaModeActive())
        { 
            if (IsMainPath())
            {
                if (floor < 10)
                {
                    numFountains -= 1;
                }
                else if (floor == MapMasterScript.FINAL_SIDEAREA_1 || floor == MapMasterScript.FINAL_SIDEAREA_2 || floor == MapMasterScript.FINAL_SIDEAREA_3 || floor == MapMasterScript.FINAL_SIDEAREA_4)
                {
                    numFountains += 1;
                }                
            }            
        }

        if (GameMasterScript.gameLoadSequenceCompleted && IsItemWorld())
        {
            if (GameMasterScript.heroPCActor.myStats.GetLevel() > dungeonLevelData.expectedPlayerLevel)
            {
                numFountains--;
            }
            if (GameMasterScript.heroPCActor.myStats.GetLevel() > dungeonLevelData.expectedPlayerLevel + 1)
            {
                numFountains--;
            }
            if (GameMasterScript.heroPCActor.myStats.GetLevel() > dungeonLevelData.expectedPlayerLevel + 2)
            {
                numFountains--;
            }
            //Debug.Log("IW fountains: " + numFloors + " " + floor + " " + dungeonLevelData.expectedPlayerLevel);
        }

        int numFountainsFromData = dungeonLevelData.GetMetaData("numfountains");
        if (numFountainsFromData != 0)
        {
            numFountains = numFountainsFromData;
        }

        if (numFountains > 0)
        {
            for (int i = 0; i < numFountains; i++)
            {
                SpawnFountainInMap();
            }
        }

        foreach (Room rm in mapRooms)
        {
            // Check for props - random destructibles. At most one per room for now.

            if (rm.internalTiles.Count == 0)
            {
                continue;
            }

            if (rm.template != null && rm.template.specialArea) continue;

            for (int n = 0; n < mgd.maxPropsPerRoom; n++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= mgd.anyPropChance)
                {
                    checkTile = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];

                    for (int x = 0; x < mgd.randomProps.Length; x++)
                    {
                        if ((!checkTile.CheckTag(LocationTags.HASDECOR)) && (checkTile.GetAllActors().Count == 0) && (checkTile.tileType == TileTypes.GROUND) && (checkTile.pos != heroStartTile))
                        {
                            if ((checkTile.IsCollidable(GameMasterScript.genericMonster)) || (checkTile.CheckTag(LocationTags.WATER)))
                            {
                                continue;
                            }
                            // Pick a random destructible

                            string template = breakablesForThisFloor.GetRandomActorRef();

                            Destructible act = CreateDestructibleInTile(checkTile, template, mysteryDungeon, mysteryDungeonWithExtraBreakables);
                            break;

                        }
                    }
                }
            }
        }


        int sparkles = UnityEngine.Random.Range(1, 3);
        if (floor > 1)
        {
            sparkles += ((floor - 2) / 2);
        }

        if (floor > 100 && !IsTownMap())
        {
            sparkles = UnityEngine.Random.Range(2, 5);
        }

        if (mapRooms.Count > 4)
        {
            if (sparkles > mapRooms.Count - 2)
            {
                sparkles = mapRooms.Count - 2;
            }
        }

        if (sparkles > 5)
        {
            sparkles = 5;
        }
        else if (sparkles < 0) sparkles = 0;

        sparkles += dungeonLevelData.bonusSparkles;

        List<int> usedIDs = new List<int>();

        if (dungeonLevelData.layoutType != DungeonFloorTypes.VOLCANO)
        {
            for (int x = 0; x <= sparkles; x++)
            {
                Room rm = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                int tries = 0;
                while ((tries < 500) && (((rm.internalTiles.Count == 0) || (usedIDs.Contains(rm.areaID))) || ((rm.template != null) && (rm.template.specialArea))))
                {
                    rm = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                    tries++;
                }
                if (tries >= 500)
                {
                    //Debug.Log("No valid treasure rooms on sparkle " + x + " for floor " + floor);
                    continue;
                }

                List<MapTileData> possible = new List<MapTileData>();
                foreach (MapTileData mtd in rm.internalTiles)
                {
                    possible.Add(mtd);
                }
                //possible.Shuffle();
                MapTileData tile = possible[UnityEngine.Random.Range(0, possible.Count)];

                tries = 0;
                while ((tries < 500) && ((tile.GetStairsInTile() != null) || (tile.pos == heroStartTile) || (tile.tileType != TileTypes.GROUND) || (tile.GetAllActors().Count > 0)))
                {
                    tries++;
                    possible.Remove(tile);
                    if (possible.Count == 0)
                    {
                        tile = null;
                        break;
                    }
                    //tile = possible[0];
                    tile = possible[UnityEngine.Random.Range(0, possible.Count)];
                }
                if (tile == null || tries == 500)
                {
                    //Debug.Log("No more possible tiles");
                    continue;
                }

                CreateTreasureInTile(tile);

                if ((dungeonLevelData.layoutType != DungeonFloorTypes.CAVE) && (dungeonLevelData.layoutType != DungeonFloorTypes.MAZEROOMS) && (dungeonLevelData.layoutType != DungeonFloorTypes.LAKE) && (dungeonLevelData.layoutType != DungeonFloorTypes.AUTOCAVE) && (dungeonLevelData.layoutType != DungeonFloorTypes.MAZE) && (dungeonLevelData.layoutType != DungeonFloorTypes.VOLCANO) && (dungeonLevelData.layoutType != DungeonFloorTypes.CIRCLES))
                {
                    usedIDs.Add(rm.areaID);
                }

                //Debug.Log("Spawned sparkle on floor " + floor + " " + tile.pos);

            }
        }
        else
        {
            // Volcanos are special...
            sparkles += 2;
            for (int i = 0; i < sparkles; i++)
            {
                int minX = (int)(columns * 0.34f);
                int maxX = (int)(columns * 0.66f);
                int minY = (int)(rows * 0.34f);
                int maxY = (int)(rows * 0.66f);
                MapTileData mtd = mapArray[UnityEngine.Random.Range(minX, maxX + 1), UnityEngine.Random.Range(minY, maxY + 1)];
                while (mtd.tileType == TileTypes.WALL)
                {
                    mtd = mapArray[UnityEngine.Random.Range(minX, maxX + 1), UnityEngine.Random.Range(minY, maxY + 1)];
                }
                CreateTreasureInTile(mtd, mysteryDungeon, mysteryDungeonWithExtraBreakables);
            }

        }
    }

    void AddFungusToMap()
    {
        // All map gen completed. Now add fungus!
        MapTileData fungalTile = null;
        if ((dungeonLevelData.hasFungus) || ((IsItemWorld()) && (challengeRating >= 1.3f) && (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_ITEMWORLD_FUNGUS)))
        {
            MonsterTemplateData fungalTemplate = MonsterManagerScript.GetTemplateByRef("mon_fungalcolumn");
            for (int x = 1; x < columns - 2; x++)
            {
                for (int y = 1; y < rows - 2; y++)
                {
                    if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.singletonMMS.fungusSpawnChance)
                    {
                        fungalTile = mapArray[x, y];
                        if (fungalTile.tileType != TileTypes.GROUND) continue;
                        if ((fungalTile.CheckTag(LocationTags.WATER)) || (fungalTile.CheckTag(LocationTags.ISLANDSWATER)) || (fungalTile.CheckTag(LocationTags.LAVA)) || (fungalTile.CheckTag(LocationTags.ELECTRIC)) || (fungalTile.CheckTag(LocationTags.MUD))) continue;
                        if (fungalTile.GetAllActors().Count > 0) continue;
                        Monster newMon = MonsterManagerScript.CreateMonster("mon_fungalcolumn", false, false, false, 0f, bonusRewards, false);
                        PlaceActor(newMon, fungalTile);
                    }
                }
            }
        }
    }

    public void ClearAllCrystals()
    {
        List<Actor> remove = new List<Actor>();
        foreach (Actor act in actorsInMap)
        {
            if (act.actorRefName == "mon_itemworldcrystal")
            {
                remove.Add(act);
            }
        }
        foreach (Actor act in remove)
        {
            RemoveActorFromLocation(act.GetPos(), act);
            RemoveActorFromMap(act);
        }
    }

    public Room FindRoom(string searchRef)
    {
        foreach (Room rm in mapRooms)
        {
            if (rm.template != null)
            {
                if (rm.template.refName.Contains(searchRef))
                {
                    return rm;
                }
            }
        }

        Debug.Log("WARNING: Room with string " + searchRef + " NOT found on " + floor + " " + dungeonLevelData.layoutType);
        return null;
    }

    public void AddCrystalsToRoom(Room rm, int numCrystals = 2)
    {
        rm.internalTiles.Shuffle();
        int crystalsPlaced = 0;

        for (int i = 0; i < rm.internalTiles.Count; i++)
        {
            if (crystalsPlaced == numCrystals) break;

            if (!rm.internalTiles[i].IsEmpty()) continue;
            if (rm.internalTiles[i].tileType != TileTypes.GROUND) continue;

            Monster newMon = MonsterManagerScript.CreateMonster("mon_itemworldcrystal", false, true, false, 0f, 0f, false);
            newMon.SetActorData("monster_auras_only", 1);
            if (!MapMasterScript.hostileDreamAuras.Contains(newMon.ReadActorData("itemworldaura")))
            {
                int aura = MapMasterScript.hostileDreamAuras[UnityEngine.Random.Range(0, MapMasterScript.hostileDreamAuras.Count)];
                newMon.SetActorData("itemworldaura", aura);
            }
            PlaceActor(newMon, rm.internalTiles[i]);
            crystalsPlaced++;
        }
    }

    public void ClearStairsExceptRef(Stairs st)
    {
        Stairs stRemove = null;
        foreach (Stairs existingStairs in mapStairs)
        {
            if (existingStairs == st) continue;
            stRemove = existingStairs;
            break;
        }
        RemoveActorFromMap(stRemove);
        mapStairs.Remove(stRemove);
    }

    public void AddCrystalsToItemWorld()
    {
        int numCrystals = UnityEngine.Random.Range(2, 4);
        if ((challengeRating >= 1.2f) && (challengeRating <= 1.35f))
        {
            numCrystals++;
        }
        if ((challengeRating > 1.35f) && (challengeRating <= 1.5f))
        {
            numCrystals += 2;
        }
        if (challengeRating > 1.5f)
        {
            numCrystals += 3;
        }
        mapRooms.Shuffle();
        List<Actor> crystals = new List<Actor>();


        for (int i = 0; i < numCrystals; i++)
        {
            MapTileData randomMTD = null;
            int x = 0;
            int y = 0;
            bool tileValid = false;
            int tries = 0;
            while (!tileValid)
            {
                tries++;
                if (tries > 5000)
                {
                    break;
                }
                x = UnityEngine.Random.Range(1, columns - 1);
                y = UnityEngine.Random.Range(1, rows - 1);
                randomMTD = mapArray[x, y];

                if ((randomMTD.tileType == TileTypes.GROUND) && (!randomMTD.CheckTag(LocationTags.DUGOUT)))
                {
                    if (randomMTD.GetAllTargetablePlusDestructibles().Count == 0)
                    {
                        bool crystalValid = true;
                        foreach (Actor mn in crystals)
                        {
                            if ((mn.actorRefName == "mon_itemworldcrystal") && (MapMasterScript.GetGridDistance(mn.GetPos(), randomMTD.pos) <= 3))
                            {
                                crystalValid = false;
                                break;
                            }
                        }
                        if (crystalValid)
                        {
                            tileValid = true;
                            break;
                        }
                    }
                }
            }

            if (tileValid)
            {
                //Debug.Log("Spawned crystal at " + randomMTD.pos);
                numCrystals--;
                Monster newMon = MonsterManagerScript.CreateMonster("mon_itemworldcrystal", false, true, false, 0f, 0f, false);
                PlaceActor(newMon, randomMTD);
                crystals.Add(newMon);
            }
        }
    }

    protected void AddGrassLayer2ToMapAndBeautifyAll(bool onlyBeautify = false)
    {
        List<TileSet> tileSetsThatIgnoreGrass2 = new List<TileSet>()
        {
            TileSet.FUTURE,
            TileSet.SAND,
            TileSet.SNOW,
            TileSet.REINFORCED,
            TileSet.LUSHGREEN,
            TileSet.BLUESTONEDARK,
            TileSet.MOSS,
            TileSet.BLUESTONELIGHT
        };

        bool hasGrass2 = !tileSetsThatIgnoreGrass2.Contains(dungeonLevelData.tileVisualSet);

        MapTileData checkForGrass;
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                if (!onlyBeautify)
                {
                    checkForGrass = mapArray[x, y];
                    if (!checkForGrass.CheckTag(LocationTags.GRASS) && checkForGrass.tileType == TileTypes.GROUND && !checkForGrass.CheckMapTag(MapGenerationTags.HOLE)) // Possibly valid grass border tile.
                    {
                        if (hasGrass2)
                        {
                            if (mapArray[x + 1, y].CheckTag(LocationTags.GRASS)) // Grass to east
                            {
                                checkForGrass.AddTag(LocationTags.GRASS2);
                            }
                            else if (mapArray[x - 1, y].CheckTag(LocationTags.GRASS)) // Grass to west
                            {
                                checkForGrass.AddTag(LocationTags.GRASS2);
                            }
                            else if (mapArray[x, y + 1].CheckTag(LocationTags.GRASS)) // Grass to north
                            {
                                checkForGrass.AddTag(LocationTags.GRASS2);
                            }
                            else if (mapArray[x, y - 1].CheckTag(LocationTags.GRASS)) // Grass to south
                            {
                                checkForGrass.AddTag(LocationTags.GRASS2);
                            }
                        }
                    }
                }
                BeautifyTerrain(mapArray[x, y], LocationTags.GRASS, LocationTags.GRASS);
                BeautifyTerrain(mapArray[x, y], LocationTags.WATER, LocationTags.WATER);
                BeautifyTerrain(mapArray[x, y], LocationTags.LAVA, LocationTags.LAVA);
                BeautifyTerrain(mapArray[x, y], LocationTags.MUD, LocationTags.MUD);
                BeautifyTerrain(mapArray[x, y], LocationTags.ELECTRIC, LocationTags.ELECTRIC);
            }
        }

        RemoveOneOffGrassTiles();

        if (hasGrass2)
        {
            for (int x = 1; x < columns - 1; x++)
            {
                for (int y = 1; y < rows - 1; y++)
                {
                    BeautifyTerrain(mapArray[x, y], LocationTags.GRASS2, LocationTags.GRASS);
                }
            }
        }
    }

    protected void RemoveOneOffGrassTiles()
    {
        float oneOffGrassRemoveChance = 0f;

        if (dungeonLevelData.tileVisualSet == TileSet.LUSHGREEN || dungeonLevelData.tileVisualSet == TileSet.MOSS
            || dungeonLevelData.tileVisualSet == TileSet.SNOW)
        {
            oneOffGrassRemoveChance = 0.9f;
        }
        else if (dungeonLevelData.tileVisualSet == TileSet.REINFORCED || dungeonLevelData.tileVisualSet == TileSet.FUTURE || dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            oneOffGrassRemoveChance = 0.75f;
        }
        else if (dungeonLevelData.tileVisualSet == TileSet.STONE)
        {
            oneOffGrassRemoveChance = 0.6f;
        }

        MapTileData checkForGrass;

        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                checkForGrass = mapArray[x, y];
                if (!checkForGrass.CheckTag(LocationTags.GRASS)) continue;
                if (floor == 0)
                {
                    //Debug.Log((VisualTileType)checkForGrass.visualGrassTileType);
                }
                if (checkForGrass.visualGrassTileType == VisualTileType.WALL_NONE && UnityEngine.Random.Range(0, 1f) <= oneOffGrassRemoveChance)
                {
                    checkForGrass.RemoveTag(LocationTags.GRASS);
                }
            }
        }
    }

    protected void AddElectricTilesToMap()
    {
        // Place elec in tiles.
        MapTileData checkForElec;
        Vector2 checkForElecV2 = Vector2.zero;
        float elecChance = 0.0f;
        switch (dungeonLevelData.tileVisualSet)
        {
            case TileSet.FUTURE:
                elecChance = 0.08f;
                break;
        }
        int groundNearbyCount = 0;
        int elecNearbyCount = 0;
        float percentNearbyFilled = 0f;
        if (dungeonLevelData.specialRoomTemplate != null)
        {
            elecChance = 0f;
        }
        if (!dungeonLevelData.HasOverlayImage() && elecChance > 0f)
        {
            for (int x = 1; x < columns - 1; x++)
            {
                for (int y = 1; y < rows - 1; y++)
                {
                    elecChance = 0.08f;
                    checkForElec = mapArray[x, y];
                    if (checkForElec.tileType == TileTypes.WALL) continue;
                    if ((checkForElec.CheckTag(LocationTags.LAVA)) || (checkForElec.CheckTag(LocationTags.MUD)) || (checkForElec.CheckTag(LocationTags.WATER)) || (checkForElec.CheckTag(LocationTags.ISLANDSWATER)))
                    {
                        continue;
                    }

                    CustomAlgorithms.GetTilesAroundPoint(checkForElecV2, 1, this);

                    //foreach (MapTileData m in GetTilesAroundPoint(checkForElecV2, 1))
                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND) groundNearbyCount++;
                        if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.ELECTRIC))
                        {
                            elecNearbyCount++;
                        }
                    }

                    if (groundNearbyCount > 0)
                    {
                        percentNearbyFilled = elecNearbyCount / groundNearbyCount;
                        elecChance += percentNearbyFilled;
                    }

                    if (UnityEngine.Random.Range(0, 1f) <= elecChance) // Chance to spawn grass.
                    {
                        if (checkForElec.IsEmpty())
                        {
                            checkForElec.AddTag(LocationTags.ELECTRIC);
                        }
                    }
                }
            }
        }
    }

    protected void AddGrassLayer1ToMap(float forceChance = 0f)
    {
        // Place grass in tiles.
        MapTileData checkForGrass;
        Vector2 checkForGrassV2 = Vector2.zero;
        float grassChance = 0.0f;
        switch (dungeonLevelData.tileVisualSet)
        {
            case TileSet.EARTH:
            case TileSet.MOSS:
                grassChance = 0.52f;
                break;
            case TileSet.SLATE:
                grassChance = 0.3f;
                break;
            case TileSet.LUSHGREEN:
                grassChance = 0.4f;
                break;
            case TileSet.REINFORCED:
                grassChance = 0.5f;
                break;
            case TileSet.SAND:
                grassChance = 0f; // was 0.22f
                break;
            case TileSet.STONE:
            case TileSet.BLUESTONELIGHT:
            case TileSet.BLUESTONEDARK:
                grassChance = 0.25f;
                break;
            case TileSet.FUTURE:
                grassChance = 0.2f;
                break;
            case TileSet.SNOW:
                grassChance = 0.22f;
                break;
            case TileSet.TREETOPS:
                grassChance = 0.25f;
                break;
            case TileSet.MOUNTAINGRASS:
                grassChance = 0.42f;
                break;
        }

        if (forceChance > 0)
        {
            grassChance = forceChance;
        }

        int groundNearbyCount = 0;
        int grassNearbyCount = 0;
        float percentNearbyFilled = 0f;

        if (grassChance > 0f && !dungeonLevelData.HasOverlayImage())
        {
            for (int x = 1; x < columns - 1; x++)
            {
                for (int y = 1; y < rows - 1; y++)
                {
                    checkForGrass = mapArray[x, y];
                    if (checkForGrass.tileType == TileTypes.WALL) continue;
                    if (checkForGrass.CheckTag(LocationTags.LAVA) || checkForGrass.CheckTag(LocationTags.MUD) || 
                        checkForGrass.CheckTag(LocationTags.WATER))
                    {
                        continue;
                    }
                    if (checkForGrass.CheckMapTag(MapGenerationTags.HOLE)) continue; // no grass on holes pls

                    checkForGrassV2 = checkForGrass.pos; // This wasn't here before how did this ever work?

                    CustomAlgorithms.GetTilesAroundPoint(checkForGrassV2, 1, this);

                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND) groundNearbyCount++;
                        if (CustomAlgorithms.tileBuffer[i].CheckTag(LocationTags.GRASS))
                        {
                            grassNearbyCount++;
                        }
                    }

                    if (groundNearbyCount > 0)
                    {
                        percentNearbyFilled = grassNearbyCount / groundNearbyCount;
                        grassChance += percentNearbyFilled;
                    }

                    if (UnityEngine.Random.Range(0, 1f) <= grassChance) // Chance to spawn grass.
                    {
                        checkForGrass.AddTag(LocationTags.GRASS);
                    }
                }
            }
        }

        if (dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            DungeonGenerationAlgorithms.CleanUpGrassAndMudInMountainGrassMap(this);
        }
    }

    public bool GetMapVisibility()
    {
        return mapIsHidden;
    }

    /// <summary>
    /// If forciblyRemove is set to "true", the connecting stairs will be removed FOREVER. Be careful!
    /// </summary>
    /// <param name="vis"></param>
    /// <param name="forciblyRemove"></param>
    public void SetMapVisibility(bool vis, bool forciblyRemove = false)
    {
        //Debug.Log("Set map " + floor + " visibility to " + vis);
        mapIsHidden = !vis;

        foreach (Stairs st in mapStairs)
        {
            if (vis)
            {
                st.EnableActor();
            }
            else
            {
                st.DisableActor();
            }
            Map connectMap = st.NewLocation;
            if (connectMap == null)
            {
                connectMap = MapMasterScript.theDungeon.FindFloor(st.pointsToFloor);
            }
            if (connectMap != null)
            {
                foreach (Stairs cStairs in connectMap.mapStairs)
                {
                    if (cStairs.NewLocation == null)
                    {
                        cStairs.SetDestination(cStairs.pointsToFloor);
                        if (cStairs.NewLocation == null)
                        {
                            continue;
                        }
                    }
                    //Debug.Log("On floor " + connectMap.floor + ", stairs point to " + cStairs.newLocation.floor + " " + cStairs.pointsToFloor);
                    if (cStairs.NewLocation.floor == floor)
                    {
                        if (vis)
                        {
                            cStairs.EnableActor();
                        }
                        else
                        {
                            cStairs.DisableActor();
                            //Debug.Log("Stairs " + cStairs.GetPos() + " on floor " + connectMap.floor + " are now DISABLED.");
                        }
                    }
                }
                if (forciblyRemove)
                {
                    connectMap.RemoveStairsPointingToFloor(floor);
                }
            }
        }
    }

    public bool IsRealmOfGodsAndNotUnlocked()
    {
        if (floor < MapMasterScript.REALM_OF_GODS_START || floor > MapMasterScript.REALM_OF_GODS_END) return false;
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return true;
        if (ProgressTracker.CheckProgress(TDProgress.REALMGODS_UNLOCKED, ProgressLocations.META) < 1) return true;
        if (ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.HERO) < 2) return true;

        return false;

    }

    public bool ValidForQuest(int lowest, int highest)
    {
        if (floor == MapMasterScript.BRANCH_PASSAGE_POSTBOSS1) return false;
        if (floor == MapMasterScript.BRANCH_PASSAGE_POSTBOSS2) return false;
        if (floor == MapMasterScript.PREBOSS1_MAP_FLOOR) return false;
        if (floor == MapMasterScript.PREBOSS2_MAP_FLOOR) return false;
        if (floor == MapMasterScript.ROMANCE_SIDEAREA) return false;
        if (floor == MapMasterScript.SHARA_START_CAMPFIRE_FLOOR) return false;
        if (floor == MapMasterScript.SHARA_START_FOREST_FLOOR) return false;
        if (floor >= MapMasterScript.RIVERSTONE_WATERWAY_START &&
            floor <= MapMasterScript.RIVERSTONE_WATERWAY_END) // riverstone waterway
        {
            if (!SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.RIVERSTONE_WATERWAY))
            {
                return false;
            }
            Map checkWaterway = MapMasterScript.theDungeon.FindFloor(floor);
            if (checkWaterway == null) return false;
            if (checkWaterway.mapIsHidden) return false;
            if (!MapMasterScript.singletonMMS.townMap.HasActor("npc_jumpintoriver"))
            {
                return false;
            }
        }

        if (dungeonLevelData.effectiveFloor != 0)
        {
            if (dungeonLevelData.effectiveFloor < lowest || dungeonLevelData.effectiveFloor > highest)
            {
                return false;
            }
        }

        return true;
    }

    bool TryBuildSpecialTemplateTile(Area myArea, string templateName, char c, MapTileData thisTile, bool mapIsItemDreamFromTemplate)
    {        
        bool readAnySpecial = false;
        Monster newMon;
        NPC makeNPC;
        switch (templateName)
        {
            case "snowhouse_2":
                readAnySpecial = false;
                switch (c)
                {
                    case 'z':
                        if (mapIsItemDreamFromTemplate) return true;

                        readAnySpecial = true;
                        Item shard = LootGeneratorScript.CreateItemFromTemplateRef("item_lucidorb_shard", 1.5f, 0f, false);
                        MagicMod mmRandom = null;
                        bool modvalid = false;
                        while (!modvalid)
                        {
                            mmRandom = GameMasterScript.listModsSortedByChallengeRating[UnityEngine.Random.Range(0, GameMasterScript.masterMagicModList.Count)];
                            if (mmRandom.challengeValue >= 3f || mmRandom.noNameChange || mmRandom.modFlags[(int)MagicModFlags.NIGHTMARE] || mmRandom.modFlags[(int)MagicModFlags.CASINO] || mmRandom.bDontAnnounceAddedAbilities)
                            {
                                continue;
                            }
                            modvalid = true;
                        }
                        shard.SetOrbMagicModRef(mmRandom.refName);                        
                        shard.RebuildDisplayName();
                        PlaceActor(shard, thisTile);
                        break;
                }
                break;
            case "town1expansion":
                readAnySpecial = false;
                switch (c)
                {
                    case 'u': // Stairs back to camp
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsDown = new Stairs();
                        stairsDown.dungeonFloor = floor;
                        stairsDown.displayName = "Path to Riverstone Camp";
                        stairsDown.prefab = "TransparentStairs";
                        stairsDown.stairsUp = false;
                        PlaceActor(stairsDown, thisTile);
                        stairsDown.EnableActor();
                        stairsDown.autoMove = true;
                        readAnySpecial = true;
                        break;
                    case 'p': // player start
                        heroStartTile = thisTile.pos;
                        heroStartArea = myArea;
                        readAnySpecial = true;
                        break;
                    case 'J': // corral
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_monsterguy");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'f': // farmer
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_farmer");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'M':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_itemworld");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'm':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_tinkerer");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case '1':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = MetaProgressScript.treesPlanted[0];
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case '2':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = MetaProgressScript.treesPlanted[1];
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case '3':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = MetaProgressScript.treesPlanted[2];
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case '4':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = MetaProgressScript.treesPlanted[3];
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case '5':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = MetaProgressScript.treesPlanted[4];
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                }
                break;
            case "town1hub":
                readAnySpecial = false;
                switch (c)
                {
                    case 'd': // Stairs down to Tangledeep
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsDown = new Stairs();
                        stairsDown.dungeonFloor = floor;
                        stairsDown.displayName = "Path to Tangledeep";
                        stairsDown.prefab = "TransparentStairs";
                        stairsDown.stairsUp = false;
                        PlaceActor(stairsDown, thisTile);
                        stairsDown.EnableActor();
                        stairsDown.autoMove = true;
                        readAnySpecial = true;
                        break;
                    case 'u': // Stairs to grove
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsUp = new Stairs();
                        stairsUp.dungeonFloor = floor;
                        stairsUp.displayName = "Path to Riverstone Grove";
                        stairsUp.prefab = "TransparentStairs";
                        stairsUp.stairsUp = true;
                        PlaceActor(stairsUp, thisTile);
                        stairsUp.EnableActor();
                        stairsUp.autoMove = true;
                        readAnySpecial = true;
                        break;

                    case 'p': // player start
                        heroStartTile = thisTile.pos;
                        heroStartArea = myArea;
                        readAnySpecial = true;
                        break;
                    case 'g':
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_townsign");
                        makeNPC.dungeonFloor = floor;
                        //makeNPC.SetUniqueIDAndAddToDict();
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'w':
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_weaponmaster");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'j': // Julia
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_julia");
                        makeNPC.dungeonFloor = floor;
                        //makeNPC.SetUniqueIDAndAddToDict();
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'q': // Quest giver
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_questgiver");
                        makeNPC.dungeonFloor = floor;
                        makeNPC.RefreshShopInventory(0);
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'F': // Campifire
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_cookingfire");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'K': // Kitchen
                        CreateDestructibleInTile(thisTile, "obj_townkitchen");
                        readAnySpecial = true;
                        break;
                    case 'W': // Wardrobe
                        CreateNPCInTile(thisTile, "npc_wardrobe");
                        readAnySpecial = true;
                        break;
                    case 'b': // Banker
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_banker");
                        makeNPC.dungeonFloor = floor;
                        makeNPC.RefreshShopInventory(0);
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                    case 't': // Turtle
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_healer");
                        makeNPC.dungeonFloor = floor;
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;

                    case 'n': // Nando
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_nando");
                        makeNPC.dungeonFloor = floor;
                        makeNPC.RefreshShopInventory(0);
                        //makeNPC.SetUniqueIDAndAddToDict();
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;

                    case 'k': // Katje
                        if (mapIsItemDreamFromTemplate) return true;
                        makeNPC = new NPC();
                        makeNPC.CopyFromTemplateRef("npc_katje");
                        makeNPC.dungeonFloor = floor;
                        makeNPC.RefreshShopInventory(0);
                        //makeNPC.SetUniqueIDAndAddToDict();
                        PlaceActor(makeNPC, thisTile);
                        readAnySpecial = true;
                        break;
                }
                break;
            case "spiritsbossfight":
                readAnySpecial = false;
                newMon = null;
                switch (c)
                {
                    case 'u': // Stairs up
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsUp = new Stairs();
                        stairsUp.dungeonFloor = floor;
                        stairsUp.displayName = "Stairway Down";
                        stairsUp.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsDown";
                        stairsUp.stairsUp = true;
                        PlaceActor(stairsUp, thisTile);
                        stairsUp.DisableActor();
                        readAnySpecial = true;
                        break;
                    case 'd': // Stairs down
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsDown = new Stairs();
                        stairsDown.dungeonFloor = floor;
                        stairsDown.displayName = "Stairway Up";
                        stairsDown.prefab = MapMasterScript.visualTileSetNames[(int)dungeonLevelData.tileVisualSet] + "StairsUp";
                        stairsDown.stairsUp = false;
                        PlaceActor(stairsDown, thisTile);
                        stairsDown.DisableActor();
                        readAnySpecial = true;
                        break;
                    case 'w': // Water
                        thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                        thisTile.AddTag(LocationTags.WATER);
                        if (myArea.edgeTiles.Contains(thisTile))
                        {
                            myArea.edgeTiles.Remove(thisTile);
                        }
                        if (!myArea.internalTiles.Contains(thisTile))
                        {
                            myArea.internalTiles.Add(thisTile);
                        }
                        break;
                    case 'A':
                        if (mapIsItemDreamFromTemplate) return true;
                        CreateDestructibleInTile(thisTile, "obj_breathepillar");
                        readAnySpecial = true;
                        break;
                    case 'S':
                        Actor existingShadow = FindActor("mon_shadowelementalboss");
                        if (existingShadow == null) // First spirit, drop loot.
                        {
                            newMon = MonsterManagerScript.CreateMonster("mon_shadowelementalboss", true, true, false, 0f, false);
                        }
                        else
                        {
                            // Don't drop loot
                            newMon = MonsterManagerScript.CreateMonster("mon_shadowelementalboss", false, true, false, 0f, false);
                        }

                        PlaceActor(newMon, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'R':
                        newMon = MonsterManagerScript.CreateMonster("mon_youngwaterelemental", true, true, false, 0f, false);
                        newMon.MakeChampion();
                        PlaceActor(newMon, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'E':
                        newMon = MonsterManagerScript.CreateMonster("mon_youngfireelemental", true, true, false, 0f, false);
                        PlaceActor(newMon, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'T':
                        newMon = MonsterManagerScript.CreateMonster("mon_younglightningelemental", true, true, false, 0f, false);
                        PlaceActor(newMon, thisTile);
                        readAnySpecial = true;
                        break;
                    case 'p': // Player location
                        heroStartTile = thisTile.pos;
                        heroStartArea = myArea;
                        readAnySpecial = true;
                        break;
                    case 'L': // Lava
                        {
                            thisTile.ChangeTileType(TileTypes.GROUND, mgd);
                            thisTile.AddTag(LocationTags.LAVA);
                            if (myArea.edgeTiles.Contains(thisTile))
                            {
                                myArea.edgeTiles.Remove(thisTile);
                            }
                            if (!myArea.internalTiles.Contains(thisTile))
                            {
                                myArea.internalTiles.Add(thisTile);
                            }
                            break;
                        }
                }
                break;
            case "campfire":                
                readAnySpecial = false;
                switch (c)
                {
                    case 'F': // campfire
                        CreateDestructibleInTile(thisTile, "obj_campfirebg");
                        CreateNPCInTile(thisTile, "npc_restfire");
                        readAnySpecial = true;
                        break;
                }
                break;
            case "buriedloot":
                readAnySpecial = false;
                switch (c)
                {
                    case 'T':
                        if (mapIsItemDreamFromTemplate) return true;
                        Destructible bigChest = CreateDestructibleInTile(thisTile, "obj_giantchest");

                        int gearCount = 0;
                        foreach (Item itm in bigChest.myInventory.GetInventory())
                        {
                            if (itm.IsEquipment())
                            {
                                gearCount++;
                            }
                        }

                        if (gearCount < 2)
                        {
                            for (int x = gearCount; x < 3; x++)
                            {
                                Item test = LootGeneratorScript.GenerateLootFromTable(1.4f, 0.8f, "equipment");
                                test.SetUniqueIDAndAddToDict();
                                bigChest.myInventory.AddItemRemoveFromPrevCollection(test, false);
                            }
                        }

                        foreach (Item itm in bigChest.myInventory.GetInventory())
                        {
                            if (itm.IsEquipment())
                            {
                                Equipment eq = itm as Equipment;
                                EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                                if (UnityEngine.Random.Range(0, 1f) <= 0.45f)
                                {
                                    EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                                }
                            }
                        }
                        readAnySpecial = true;
                        break;
                    case '1': // Crate / bookshelf
                        genericTileList.Add(thisTile);
                        readAnySpecial = true;
                        break;
                }
                break;
            case "postgolembranch":
                readAnySpecial = false;
                Stairs branchStairs;
                switch (c)
                {
                    case 'u': // Stairs up
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsUp = new Stairs();
                        stairsUp.dungeonFloor = floor;
                        stairsUp.displayName = "Stairway Down";
                        stairsUp.prefab = "TransparentStairs";
                        stairsUp.stairsUp = true;
                        stairsUp.autoMove = true;
                        PlaceActor(stairsUp, thisTile);
                        stairsUp.pointsToFloor = 5; // back to golem
                        stairsUp.DisableActor();
                        readAnySpecial = true;
                        break;
                    case '1': // Stairs down 1
                        if (mapIsItemDreamFromTemplate) return true;
                        branchStairs = new Stairs();
                        branchStairs.dungeonFloor = floor;
                        branchStairs.displayName = "Stairway Up";
                        branchStairs.prefab = "TransparentStairs";
                        branchStairs.stairsUp = false;
                        branchStairs.autoMove = true;
                        branchStairs.pointsToFloor = 6; // main path
                        PlaceActor(branchStairs, thisTile);
                        branchStairs.DisableActor();
                        readAnySpecial = true;
                        break;
                    case '2': // Stairs down 2
                        if (mapIsItemDreamFromTemplate) return true;
                        branchStairs = new Stairs();
                        branchStairs.dungeonFloor = floor;
                        branchStairs.displayName = "Stairway Up";
                        branchStairs.prefab = "TransparentStairs";
                        branchStairs.stairsUp = false;
                        branchStairs.autoMove = true;
                        branchStairs.pointsToFloor = 135; // branched path
                        PlaceActor(branchStairs, thisTile);
                        branchStairs.DisableActor();
                        readAnySpecial = true;
                        break;
                    case 't':
                        if (mapIsItemDreamFromTemplate) return true;
                        CreateTreasureInTile(thisTile);
                        readAnySpecial = true;
                        break;
                    case '3':
                        if (mapIsItemDreamFromTemplate) return true;
                        CreateDestructibleInTile(thisTile, "trigger_fungalcave");
                        readAnySpecial = true;
                        break;
                    case '4':
                        if (mapIsItemDreamFromTemplate) return true;
                        CreateDestructibleInTile(thisTile, "trigger_oldamberstation");
                        readAnySpecial = true;
                        break;
                    case 'p': // Player location
                        heroStartTile = thisTile.pos;
                        heroStartArea = myArea;
                        readAnySpecial = true;
                        break;
                }
                break;
            case "postspiritsbranch":
                readAnySpecial = false;
                branchStairs = null;
                switch (c)
                {
                    case 'u': // Stairs up
                        if (mapIsItemDreamFromTemplate) return true;
                        Stairs stairsUp = new Stairs();
                        stairsUp.dungeonFloor = floor;
                        stairsUp.displayName = "Stairway Down";
                        stairsUp.prefab = "TransparentStairs";
                        stairsUp.stairsUp = true;
                        PlaceActor(stairsUp, thisTile);
                        stairsUp.pointsToFloor = MapMasterScript.BOSS2_MAP_FLOOR; // back to spirits
                        stairsUp.DisableActor();
                        stairsUp.autoMove = true;
                        readAnySpecial = true;
                        break;
                    case '1': // Stairs down 1
                        if (mapIsItemDreamFromTemplate) return true;
                        branchStairs = new Stairs();
                        branchStairs.dungeonFloor = floor;
                        branchStairs.displayName = "Stairway Up";
                        branchStairs.prefab = "TransparentStairs";
                        branchStairs.stairsUp = false;
                        branchStairs.pointsToFloor = 11; // main path
                        PlaceActor(branchStairs, thisTile);
                        branchStairs.DisableActor();
                        branchStairs.autoMove = true;
                        readAnySpecial = true;
                        break;
                    case '2': // Stairs down 2
                        if (mapIsItemDreamFromTemplate) return true;
                        branchStairs = new Stairs();
                        branchStairs.dungeonFloor = floor;
                        branchStairs.displayName = "Stairway Up";
                        branchStairs.prefab = "TransparentStairs";
                        branchStairs.stairsUp = false;
                        branchStairs.pointsToFloor = 151; // branched path
                        PlaceActor(branchStairs, thisTile);
                        branchStairs.DisableActor();
                        branchStairs.autoMove = true;
                        readAnySpecial = true;
                        break;
                    case 't':
                        if (mapIsItemDreamFromTemplate) return true;
                        CreateTreasureInTile(thisTile);
                        readAnySpecial = true;
                        break;
                    case 'p': // Player location
                        heroStartTile = thisTile.pos;
                        heroStartArea = myArea;
                        readAnySpecial = true;
                        break;
                }
                break;
        }
        return readAnySpecial;
    }

    // TODO: Move these to StandardCharDefinitions
    bool TryBuildNonSpecialRoomTile(Area myArea, char c, MapTileData thisTile, bool mapIsItemDreamTemplate)
    {
        NPC makeNPC;
        bool readAnySpecial = false;
        switch (c)
        {
            case 'a': // Armando test
                makeNPC = new NPC();
                makeNPC.CopyFromTemplateRef("npc_armorguy");
                makeNPC.dungeonFloor = floor;
                makeNPC.RefreshShopInventory(0);
                PlaceActor(makeNPC, thisTile);
                readAnySpecial = true;
                break;
            case 'y': //  
                makeNPC = new NPC();
                makeNPC.CopyFromTemplateRef("npc_bombguy");
                makeNPC.dungeonFloor = floor;
                makeNPC.RefreshShopInventory(0);
                PlaceActor(makeNPC, thisTile);
                readAnySpecial = true;
                break;
            case 'r': // 
                makeNPC = new NPC();
                makeNPC.CopyFromTemplateRef("npc_jewelryguy");
                makeNPC.dungeonFloor = floor;
                makeNPC.RefreshShopInventory(0);
                PlaceActor(makeNPC, thisTile);
                readAnySpecial = true;
                break;
            case 'R': // 
                makeNPC = new NPC();
                makeNPC.CopyFromTemplateRef("npc_rangedguy");
                makeNPC.dungeonFloor = floor;
                makeNPC.RefreshShopInventory(0);
                PlaceActor(makeNPC, thisTile);
                readAnySpecial = true;
                break;
            case 'n': // Level 1 champion
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHAMP1);
                readAnySpecial = true;
                break;
            case '1': // Level 1 champion
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHAMP1);
                readAnySpecial = true;
                break;
            case '2': // Level 2 champion
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHAMP2);
                readAnySpecial = true;
                break;
            case '3': // Level 3 champion
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHAMP3);
                readAnySpecial = true;
                break;
            case 'm': // Basic monster
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_MONSTER);
                readAnySpecial = true;
                break;
            case 's': // Chest 1
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST1);
                readAnySpecial = true;
                break;
            case 'S': // Chest 2
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST2);
                readAnySpecial = true;
                break;
            case 'k': // Chest 3
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST3);
                readAnySpecial = true;
                break;
            case 'K': // Chest 4
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST4);
                readAnySpecial = true;
                break;
            case 'j': // Chest 5
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST5);
                readAnySpecial = true;
                break;
            case 'J': // Chest 6
                thisTile.AddMapTag(MapGenerationTags.MAPGEN_CHEST6);
                readAnySpecial = true;
                break;
            case 'L': // Lava
                thisTile.AddTag(LocationTags.LAVA);
                readAnySpecial = true;
                break;
            case 'w': // Water
                thisTile.AddTag(LocationTags.WATER);
                readAnySpecial = true;
                break;
            case 'o': // Fountain
                CreateDestructibleInTile(thisTile, "obj_regenfountain");
                readAnySpecial = true;
                break;
            case 'M': // Monster statute.
                makeNPC = new NPC();
                makeNPC.CopyFromTemplateRef("npc_statue");
                makeNPC.dungeonFloor = floor;
                makeNPC.RefreshShopInventory(0);
                PlaceActor(makeNPC, thisTile);
                readAnySpecial = true;
                break;
        }
        return readAnySpecial;
    }

    public void TryCreateBreathePillars()
    {
        if ((dungeonLevelData.poisonAir) && (dungeonLevelData.layoutType != DungeonFloorTypes.SPECIAL) && (dungeonLevelData.layoutType != DungeonFloorTypes.ISLANDS))
        {
            foreach (Room rm in mapRooms)
            {
                MapTileData clearTile = null;
                rm.internalTiles.Shuffle();
                for (int i = 0; i < rm.internalTiles.Count; i++)
                {
                    if (!rm.internalTiles[i].IsUnbreakableCollidable(GameMasterScript.heroPCActor))
                    {
                        if (rm.internalTiles[i].GetStairsInTile() == null)
                        {
                            clearTile = rm.internalTiles[i];
                        }
                        break;
                    }
                }
                if (clearTile != null)
                {
                    CreateDestructibleInTile(clearTile, "obj_breathepillar");
                }
                else
                {
                    Debug.Log("No room for the breather object on floor " + floor);
                }
            }
        }
    }

    public void TryCreateRandomWaterPools()
    {
        if (dungeonLevelData.randomWaterPools > 0)
        {
            int min = (int)(dungeonLevelData.randomWaterPools * 0.75f);
            int max = (int)(dungeonLevelData.randomWaterPools * 1.25f);
            int numPools = UnityEngine.Random.Range(min, max);
            Area fillArea = areaDictionary[-777];
            fillArea.InitializeLists();
            fillArea.internalTiles.Shuffle();
            bool startedPool = false;
            for (int i = 0; i < numPools; i++)
            {
                int size = UnityEngine.Random.Range(3, 7);
                //Debug.Log("Pool " + i + " count of fillarea " + fillArea.internalTiles.Count);
                bool[,] water = new bool[size, size];
                startedPool = false;
                for (int c = 0; c < fillArea.internalTiles.Count; c++)
                {
                    if (startedPool) break;
                    MapTileData wMTD = fillArea.internalTiles[c];
                    Vector2 check = new Vector2(wMTD.pos.x + size, wMTD.pos.y + size);
                    startedPool = false;
                    if (InBounds(check))
                    {
                        startedPool = true;
                        MapTileData wMTD2 = null;
                        for (int x = 0; x < size; x++)
                        {
                            for (int y = 0; y < size; y++)
                            {
                                wMTD2 = mapArray[(int)wMTD.pos.x + x, (int)wMTD.pos.y + y];
                                if (CheckMTDArea(wMTD2) == fillArea.areaID && !wMTD2.CheckTag(LocationTags.WATER))
                                {
                                    water[x, y] = true;
                                }
                                else
                                {
                                    startedPool = false;
                                    break;
                                }
                            }
                            if (!startedPool) break;
                        }
                    }

                    if (startedPool)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            for (int y = 0; y < size; y++)
                            {
                                if (((x == 0) || (y == 0) || (x == size - 1) || (y == size - 1)) && (UnityEngine.Random.Range(0, 1f) <= 0.4f))
                                {
                                    water[x, y] = false;
                                }
                            }
                        }
                        for (int x = 0; x < size; x++)
                        {
                            for (int y = 0; y < size; y++)
                            {
                                if (water[x, y])
                                {
                                    mapArray[(int)wMTD.pos.x + x, (int)wMTD.pos.y + y].AddTag(LocationTags.WATER);
                                }
                            }
                        }
                        //Debug.Log("Pool started at " + wMTD.pos);
                    } // End "If started pool" loop
                    if (c == fillArea.internalTiles.Count - 1)
                    {
                        size--;
                        c = 0;
                        if (size < 3)
                        {
                            c = 9999;
                        }
                    }
                } // End iteration through Fill Area internal tiles loop
            }
        }
    }

    public void TryConvertCenterOfMapToWater()
    {
        if (dungeonLevelData.convertCenterMapToWater)
        {
            MapTileData cMTD = null;

            float minWaterArea = 0.2f;
            float maxWaterArea = 0.8f;

            if (dungeonLevelData.doLake)
            {
                minWaterArea = 0.22f;
                maxWaterArea = 0.78f;
            }

            for (int x = (int)(columns * minWaterArea); x < (int)(columns * maxWaterArea); x++)
            {
                for (int y = (int)(rows * minWaterArea); y < (int)(rows * maxWaterArea); y++)
                {
                    cMTD = mapArray[x, y];

                    if (cMTD.tileType == TileTypes.WALL)
                    {
                        cMTD.ChangeTileType(TileTypes.GROUND, mgd);
                        cMTD.AddTag(LocationTags.WATER);
                    }
                    else
                    {
                        if (dungeonLevelData.doLake && UnityEngine.Random.Range(0, 1f) <= 0.3f)
                        {
                            cMTD.AddTag(LocationTags.WATER);
                            //pool_tileList = GetNonCollidableTilesAroundPoint(cMTD.pos, 1, GameMasterScript.genericMonster);                                    
                            CustomAlgorithms.GetNonCollidableTilesAroundPoint(cMTD.pos, 1, GameMasterScript.genericMonster, this);
                            //foreach(MapTileData waterConvert in pool_tileList)
                            for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
                            {
                                if (UnityEngine.Random.Range(0, 1f) <= 0.25f)
                                {
                                    CustomAlgorithms.nonCollidableTileBuffer[i].AddTag(LocationTags.WATER);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Ensures tile is accessible in some way
    public void StairSanityCheck(Vector2 pos)
    {
        int numGround = GetNumGroundTilesAroundPoint(pos);
        if (numGround <= 2)
        {
            CustomAlgorithms.GetTilesAroundPoint(pos, 1, this);
            for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
            {
                CustomAlgorithms.tileBuffer[x].ChangeTileType(TileTypes.GROUND, mgd);
            }
            //Debug.Log("Stairs " + pos + " in floor " + floor + " id " + mapAreaID + " are NOT accessible!");
        }
    }
    public Stairs PlaceStairsInRoomTemplate(string searchRef)
    {
        Stairs st = new Stairs();
        MapTileData tileForStairs = null;
        foreach (Room rm in mapRooms)
        {
            bool foundTile = false;

            if (rm.GetTemplateName().Contains(searchRef)) continue;

            rm.internalTiles.Shuffle();

            foreach (MapTileData mtd in rm.internalTiles)
            {
                if (mtd.CheckTag(LocationTags.WATER) || mtd.CheckTag(LocationTags.ELECTRIC) || mtd.CheckTag(LocationTags.LAVA) 
                    || mtd.CheckTag(LocationTags.MUD))
                {
                    continue;
                }
                if (mtd.IsEmpty())
                {
                    foundTile = true;
                    tileForStairs = mtd;
                    break;
                }
            }
            if (foundTile)
            {
                break;
            }
        }

        PlaceActor(st, tileForStairs);
        tileForStairs.ChangeTileType(TileTypes.GROUND, mgd);
        return st;
    }

    public void CheckForStackedActors(Vector2 pos, Actor primary, bool animate)
    {
        if (!InBounds(pos))
        {
            return;
        }
        if (primary == null)
        {
            return;
        }
        if (!primary.monsterCollidable) return;
        actorsToMove.Clear();
        foreach (Actor act in GetTile(pos).GetAllActors())
        {
            if (act.GetActorType() == ActorTypes.MONSTER && act.actorUniqueID != primary.actorUniqueID)
            {
                actorsToMove.Add(act);
            }
        }
        foreach (Actor act in actorsToMove)
        {
            if (act.actorUniqueID == primary.actorUniqueID) continue;
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) continue;
            MapTileData mtd = GetRandomEmptyTile(act.GetPos(), 1, true);
            //Debug.Log("Stacked actor " + act.actorRefName + " " + act.actorUniqueID + " at " + pos + " moved to " + mtd.pos);
            RemoveActorFromLocation(act.GetPos(), act);
            RemoveActorFromLocation(act.previousPosition, act);
            AddActorToLocation(mtd.pos, act);
            act.SetCurPos(pos);
            if (animate)
            {
                if (act.myMovable != null) // let's just be extra safe here
                {
                    act.myMovable.AnimateSetPosition(mtd.pos, 0.15f, false, 0f, 0f, MovementTypes.LERP);
                }
            }
            else
            {
                if (act.myMovable != null) // let's just be extra safe here
                {
                    act.myMovable.SetPosition(mtd.pos);
                }
            }
        }
    }

    public void CheckForNearbyGreedyMonsterAggro(Monster looter, Vector2 pos)
    {
        CustomAlgorithms.GetTilesAroundPoint(pos, 8, this);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            MapTileData mtd = CustomAlgorithms.tileBuffer[i];
            if (mtd.tileType != TileTypes.GROUND) continue;
            foreach (Actor act in mtd.GetAllActors())
            {
                if (looter == act) continue;
                if (act.GetActorType() != ActorTypes.MONSTER) continue;
                if (act.actorfaction == Faction.PLAYER) continue;
                Monster mn = act as Monster;
                if (UnityEngine.Random.Range(1, 101) <= mn.CheckAttribute(MonsterAttributes.GREEDY))
                {
                    if ((mn.myBehaviorState != BehaviorState.NEUTRAL) && (mn.myBehaviorState != BehaviorState.CURIOUS)) continue;
                    //if (MapMasterScript.CheckTileToTileLOS(mn.GetPos(), pos, mn, this))
                    if (CustomAlgorithms.CheckBresenhamsLOS(mn.GetPos(), pos, this))
                    {
                        StringManager.SetTag(0, mn.displayName);
                        StringManager.SetTag(1, looter.displayName);
                        if (UnityEngine.Random.Range(0, 1f) <= 0.5f)
                        {
                            GameLogScript.LogWriteStringRef("log_monster_angry_looter");
                        }
                        CombatManagerScript.SpawnChildSprite("AggroEffect", mn, Directions.NORTHEAST, false);
                        mn.AddAggro(looter, 50f);
                    }
                }
            }
        }
    }

    public void RefreshTerrainVisualIndices()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (mapArray[x, y].CheckTag(LocationTags.MUD) || mapArray[x, y].CheckTag(LocationTags.ELECTRIC) || mapArray[x, y].CheckTag(LocationTags.WATER) || mapArray[x, y].CheckTag(LocationTags.LAVA))
                {
                    mapArray[x, y].SetTerrainTileType(mapArray[x, y].visualTerrainTileType);
                }
            }
        }
    }

    public void RefreshTileVisualIndices()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                //Debug.Log("Setting " + x + "," + y + " which is " + (VisualTileType)mapArray[x, y].visualTileType);
                mapArray[x, y].tileVisualSet = dungeonLevelData.tileVisualSet;
                mapArray[x, y].SelectWallReplacementIndex();
                mapArray[x, y].SetTileVisualType(mapArray[x, y].visualTileType);
            }
        }
    }

    public void BeautifyAllTerrain()
    {
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                if (mapArray[x, y].CheckTag(LocationTags.WATER))
                {
                    BeautifyTerrain(mapArray[x, y], LocationTags.WATER, LocationTags.WATER);
                }
                else if (mapArray[x, y].CheckTag(LocationTags.ELECTRIC))
                {
                    BeautifyTerrain(mapArray[x, y], LocationTags.ELECTRIC, LocationTags.ELECTRIC);
                }
                else if (mapArray[x, y].CheckTag(LocationTags.MUD))
                {
                    BeautifyTerrain(mapArray[x, y], LocationTags.MUD, LocationTags.MUD);
                }

            }
        }
    }

    public void RemoveAllActorsOfType(ActorTypes removeType)
    {
        List<Actor> toRemove = new List<Actor>();
        foreach (Actor act in actorsInMap)
        {
            if (act.GetActorType() == removeType)
            {
                toRemove.Add(act);
            }
        }

        foreach (Actor act in toRemove)
        {
            RemoveActorFromMap(act);
            actorsInMap.Remove(act);
            if (this == MapMasterScript.activeMap && act.objectSet)
            {
                act.myMovable.FadeOutThenDie();
            }
        }
    }

    protected void CreatePossibleRoomTemplateDictionaries()
    {
        MapMasterScript.TryInitializeRoomDicts();
        if (MapMasterScript.masterDictOfEnforcedMapRoomDicts.ContainsKey(floor))
        {
            if (MapMasterScript.masterDictOfEnforcedMapRoomDicts[floor] != null)
            {
                if (!IsItemWorld() && !IsMysteryDungeonMap())
                {
                    return;
                }
                else
                {
                    // Reset item dream stuff.
                    MapMasterScript.masterDictOfEnforcedMapRoomDicts.Remove(floor);
                    MapMasterScript.masterDictOfUnenforcedMapRoomDicts.Remove(floor);
                    MapMasterScript.pointsForRoomDicts.Remove(floor);
                    MapMasterScript.pointsForRoomDictsMaxSizes.Remove(floor);
                }

            }
        }


        //MapMasterScript.masterDictOfEnforcedMapRoomDicts.Add(floor, new Dictionary<MapPoint, List<RoomTemplate>>());
        //MapMasterScript.masterDictOfUnenforcedMapRoomDicts.Add(floor, new Dictionary<MapPoint, List<RoomTemplate>>());

        List<RoomTemplate> startTemplates = new List<RoomTemplate>();
        int minX = 99;
        int maxX = 0;
        int minY = 99;
        int maxY = 0;
        foreach (RoomTemplate rt in GameMasterScript.masterDungeonRoomsByLayout[(int)dungeonLevelData.layoutType])
        {
            if (rt.specialArea) continue;
            startTemplates.Add(rt);
            if (rt.numColumns < minX) minX = rt.numColumns;
            if (rt.numColumns > maxX) maxX = rt.numColumns;
            if (rt.numRows < minY) minY = rt.numRows;
            if (rt.numRows > maxY) maxY = rt.numRows;
        }

        MapMasterScript.pointsForRoomDicts.Add(floor, new MapPoint[(maxX + 1) * (maxY + 1)]);
        MapMasterScript.pointsForRoomDictsMaxSizes.Add(floor, new MapPoint(maxX, maxY));
        localPointArray = MapMasterScript.pointsForRoomDicts[floor];

        MapMasterScript.masterDictOfEnforcedMapRoomDicts.Add(floor, new List<RoomTemplate>[(maxX + 1) * (maxY + 1)]);
        MapMasterScript.masterDictOfUnenforcedMapRoomDicts.Add(floor, new List<RoomTemplate>[(maxX + 1) * (maxY + 1)]);

        //Debug.Log(startTemplates.Count + " possible room templates for floor " + floor + " min is " + minX + " " + minY + " by " + maxX + " " + maxY);
        if (startTemplates.Count == 0)
        {
            return;
        }
        bool finished = false;

        localEnforcedRoomDict = MapMasterScript.masterDictOfEnforcedMapRoomDicts[floor];
        localUnenforcedRoomDict = MapMasterScript.masterDictOfUnenforcedMapRoomDicts[floor];

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                localPointArray[y * maxX + x] = new MapPoint(x, y);
                MapPoint mp = localPointArray[y * maxX + x];

                localEnforcedRoomDict[y * maxX + x] = new List<RoomTemplate>();
                localUnenforcedRoomDict[y * maxX + x] = new List<RoomTemplate>();

                foreach (RoomTemplate rt in startTemplates)
                {
                    if (rt.numColumns == x && rt.numRows == y)
                    {
                        localEnforcedRoomDict[y * maxX + x].Add(rt);
                    }
                    if (rt.numColumns <= x && rt.numRows <= y)
                    {
                        localUnenforcedRoomDict[y * maxX + x].Add(rt);
                    }
                }
            }

        }

    }

    public List<Actor> FindAllActors(string refName, ActorTypes aType = ActorTypes.COUNT)
    {
        List<Actor> returnList = new List<Actor>();
        foreach(Actor act in actorsInMap)
        {
            if (aType != ActorTypes.COUNT && act.GetActorType() != aType) continue;
            if (act.actorRefName == refName)
            {
                returnList.Add(act);
            }
        }
        return returnList;
    }

    public void AddStumpsIfNecessary()
    {
        if (dungeonLevelData.addStumps == 0f)
        {
            return;
        }

        // Add 'em at random throughout valid tiles on the map
        List<string> possibleStumpRefs = new List<string>()
        {
            "obj_stump1",
            "obj_stump2"
        };

        MapTileData checkTile = null;
        for (int x = 1; x < columns - 1; x++)
        {
            for (int y = 1; y < rows - 1; y++)
            {
                checkTile = mapArray[x, y];
                if (checkTile.tileType != TileTypes.GROUND) continue;
                if (checkTile.AnyTerrainHazard()) continue;
                if (checkTile.CheckTag(LocationTags.DUGOUT) || checkTile.CheckTag(LocationTags.CORRIDORENTRANCE)) continue;

                if (UnityEngine.Random.Range(0,1f) <= dungeonLevelData.addStumps)
                {
                    CreateDestructibleInTile(checkTile, possibleStumpRefs[UnityEngine.Random.Range(0, possibleStumpRefs.Count)]);
                }
            }
        }
    }

    public void CreateLavaPools()
    {
        if (dungeonLevelData.maxLavaPools > 0)
        {
            if (dungeonLevelData.layoutType == DungeonFloorTypes.VOLCANO)
            {
                BuildLavaPools(dungeonLevelData.maxLavaPools, true);
                BuildLavaPools((int)((float)(dungeonLevelData.maxLavaPools * 0.72f)), false);
            }
            else
            {
                BuildLavaPools(dungeonLevelData.maxLavaPools, false);
            }
        }
    }

    public void AssignTileAreaIDsToRoomsAndCorridors()
    {
        // Sets tile IDs for each MapTileData
        // In theory, we should never need to set tile IDs other than this.
        for (int i = 0; i < mapRooms.Count; i++)
        {
            Room rm = mapRooms[i];
            for (int e = 0; e < rm.edgeTiles.Count; e++)
            {
                SetTileAreaID(rm.edgeTiles[e], rm);
            }
            for (int t = 0; t < rm.internalTiles.Count; t++)
            {
                SetTileAreaID(rm.internalTiles[t], rm);
            }
        }
        for (int i = 0; i < mapCorridors.Count; i++)
        {
            Corridor cr = mapCorridors[i];
            for (int e = 0; e < cr.edgeTiles.Count; e++)
            {
                SetTileAreaID(cr.edgeTiles[e], cr);
            }
            for (int t = 0; t < cr.internalTiles.Count; t++)
            {
                SetTileAreaID(cr.internalTiles[t], cr);
            }
        }
    }

    public void ConvertWallTilesToGroundAtRandom()
    {
        MapTileData checkTile = null;

        switch (dungeonLevelData.layoutType)
        {
            case DungeonFloorTypes.LAKE:

                // Randomly convert some spaces to nice little trees
                if (dungeonLevelData.caveFillConvertChance > 0.0f)
                {
                    for (int x = 2; x < columns - 2; x++)
                    {
                        for (int y = 2; y < rows - 2; y++)
                        {
                            if (UnityEngine.Random.Range(0, 1f) <= dungeonLevelData.caveFillConvertChance)
                            {
                                //pool_tileList = GetTilesAroundPoint(new Vector2(x, y), 1);
                                CustomAlgorithms.GetTilesAroundPoint(new Vector2(x, y), 1, this);
                                int numWall = 0;
                                //foreach (MapTileData checkForWall in pool_tileList)
                                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                                {
                                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL)
                                    {
                                        numWall++;
                                    }
                                    if (numWall > 1) break;
                                }

                                if (numWall <= 1)
                                {
                                    mapArray[x, y].ChangeTileType(TileTypes.WALL, mgd);
                                    mapArray[x, y].RemoveTag(LocationTags.WATER);
                                }
                            }
                        }
                    }

                }
                break;
            case DungeonFloorTypes.STANDARD:
            case DungeonFloorTypes.STANDARDNOHALLS:
            case DungeonFloorTypes.FUTURENOHALLS:
            case DungeonFloorTypes.HIVES:
            case DungeonFloorTypes.RUINS:
                if (mgd.chanceConvertWallToGround > 0.0f)
                {
                    for (int x = 1; x < columns - 1; x++)
                    {
                        for (int y = 1; y < rows - 1; y++)
                        {
                            checkTile = mapArray[x, y];
                            if ((checkTile.tileType == TileTypes.WALL) && (UnityEngine.Random.Range(0, 1f) <= mgd.chanceConvertWallToGround) && (!checkTile.CheckTag(LocationTags.SECRET)))
                            {
                                if ((dungeonLevelData.layoutType == DungeonFloorTypes.RUINS) && (CheckMTDArea(checkTile) < 0)) continue;
                                checkTile.ChangeTileType(TileTypes.GROUND, mgd);
                                checkTile.AddTag(LocationTags.DUGOUT);
                            }
                        }
                    }
                }
                break;
            case DungeonFloorTypes.ISLANDS:
                MapTileData mtd = null;
                for (int x = 1; x < columns - 1; x++)
                {
                    for (int y = 1; y < rows - 1; y++)
                    {
                        mtd = mapArray[x, y];
                        if (CheckMTDArea(mtd) == -777)
                        {
                            mtd.ChangeTileType(TileTypes.GROUND, mgd);
                            if ((UnityEngine.Random.Range(0, 1f) >= 0.024f) && (x > 0) && (x < columns - 1) && (y > 0) && (y < rows - 1))
                            {
                                mtd.AddTag(LocationTags.WATER); // Islands are water, right? Ok                            
                                mtd.AddTag(LocationTags.ISLANDSWATER); // Islands are water, right? Ok                                        
                            }
                            if ((UnityEngine.Random.Range(0, 1f) <= 0.04f) && (x > 0) && (x < columns - 1) && (y > 0) && (y < rows - 1))
                            {
                                mtd.ChangeTileType(TileTypes.WALL, mgd);
                                mtd.RemoveTag(LocationTags.WATER);
                                mtd.RemoveTag(LocationTags.ISLANDSWATER);
                                continue;
                            }

                        }
                    }
                }

                break;
        }
    }

    void CreateStairsInMap()
    {
        Stairs mainDown = null;
        if (floor < 20) // HARDCODED DUNGEON MAX FLOORS, the final floor obviously needs no stairs down. Number of floors
        {
            mainDown = SpawnStairs(false);
        }

        if (floor >= 0 && floor != MapMasterScript.BRANCH_PASSAGE_POSTBOSS1 && floor != MapMasterScript.BRANCH_PASSAGE_POSTBOSS2 &&
            floor != 24) // Don't spawn stairs in branched path selectors nor Shara's special area
        {
            Stairs st = SpawnStairs(true);
        }
    }

    void AssignHeroStartAreaAndTile()
    {
        bool startTileFound = false;

        int ssTries = 0;
        //Debug.Log("While1");
        while (!startTileFound && ssTries < 1000)
        {
            ssTries++;
            heroStartArea = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
            //Debug.Log("While2");
            while (heroStartArea.internalTiles.Count == 0 && ssTries < 1000)
            {
                heroStartArea = mapRooms[UnityEngine.Random.Range(0, mapRooms.Count)];
                ssTries++;
            }

            if (ssTries >= 1000) break;

            heroStartArea.internalTiles.Shuffle();
            for (int t = 0; t < heroStartArea.internalTiles.Count; t++)
            {
                MapTileData testTile = heroStartArea.internalTiles[t];
                if ((!testTile.IsCollidable(GameMasterScript.heroPCActor)) && (!testTile.CheckTag(LocationTags.LAVA)) && (!testTile.CheckTag(LocationTags.ELECTRIC)))
                {
                    if (dungeonLevelData.layoutType == DungeonFloorTypes.CAVE)
                    {
                        if ((testTile.pos.x < 4) || (testTile.pos.x >= columns - 4) || (testTile.pos.y < 4) || (testTile.pos.y >= rows - 4))
                        {
                            continue;
                        }
                    }
                    heroStartTile = testTile.pos;
                    startTileFound = true;
                    break;
                }
            }

            if (ssTries >= 1000)
            {
                Debug.Log("Failure to create map due to room issue.");
            }
        }
    }

    void CreateItemDreamToolsIfNeeded()
    {
        foreach (Room rm in mapRooms)
        {
            int randCount = UnityEngine.Random.Range(0, 4);
            MapTileData convert = null;
            if ((rm.internalTiles.Count == 0) || (rm.areaID == -777))
            {
                continue;
            }

            for (int i = 0; i < randCount; i++)
            {
                convert = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
                convert.ChangeTileType(TileTypes.WALL, mgd);
            }

            int tries = 0;
            convert = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            while (convert.tileType != TileTypes.GROUND && tries < 50)
            {
                tries++;
                convert = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            }
            if (tries < 50)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.25f)
                {
                    Consumable planks = Consumable.GetItemTemplateFromRef("item_planks") as Consumable;
                    Consumable nPlanks = new Consumable();
                    nPlanks.CopyFromItem(planks);
                    nPlanks.ChangeQuantity(UnityEngine.Random.Range(1, 4));
                    nPlanks.dreamItem = true;
                    PlaceActor(nPlanks, convert);
                    convert.ChangeTileType(TileTypes.GROUND, mgd);
                }
            }
            tries = 0;
            convert = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            while ((convert.tileType != TileTypes.GROUND || !convert.IsEmpty()) && tries < 150)
            {
                tries++;
                convert = rm.internalTiles[UnityEngine.Random.Range(0, rm.internalTiles.Count)];
            }
            if (tries < 150)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.2f)
                {
                    Consumable planks = Consumable.GetItemTemplateFromRef("item_itemworldwallbreaker") as Consumable;
                    Consumable nPlanks = new Consumable();
                    nPlanks.CopyFromItem(planks);
                    nPlanks.ChangeQuantity(UnityEngine.Random.Range(1, 4));
                    nPlanks.dreamItem = true;
                    PlaceActor(nPlanks, convert);
                    convert.ChangeTileType(TileTypes.GROUND, mgd);
                }
            }
        }
    }

    void CreateRandomIsolatedWallTiles()
    {
        // Put some random one-offs
        pool_connectingTiles.Clear();
        MapTileData m;
        for (int x = 3; x < columns - 3; x++)
        {
            for (int y = 3; y < rows - 3; y++)
            {
                m = mapArray[x, y];
                if (m.tileType == TileTypes.GROUND && !m.AnyTerrainHazard())
                {
                    //pool_connectingTiles = GetTilesAroundPoint(m.pos, 1);
                    CustomAlgorithms.GetTilesAroundPoint(m.pos, 1, this);
                    bool anyWall = false;
                    for (int c = 0; c < CustomAlgorithms.numTilesInBuffer; c++)
                    {
                        if (GetTile(CustomAlgorithms.tileBuffer[c].pos).tileType == TileTypes.WALL)
                        {
                            anyWall = true;
                            break;
                        }
                    }
                    if (!anyWall)
                    {
                        if (UnityEngine.Random.Range(0, 1f) <= 0.065f) // Chance earth oneoff wall.
                        {
                            m.ChangeTileType(TileTypes.WALL, mgd);
                            m.AddTag(LocationTags.SOLIDTERRAIN);
                            m.SelectWallReplacementIndex();
                        }
                    }
                }
            }
        }
    }

    public void OnEnemyMonsterSpawned(Map m, Monster newMon, bool spawnWandering)
    {
        if (!string.IsNullOrEmpty(dungeonLevelData.script_onMonsterSpawn))
        {
            Action<Map, Monster, bool> myFunc;

            if (MonsterSpawnFunctions.dictDelegates.TryGetValue(dungeonLevelData.script_onMonsterSpawn, out myFunc))
            {
                myFunc(this, newMon, spawnWandering);
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(MonsterSpawnFunctions), dungeonLevelData.script_onMonsterSpawn);
                object[] paramList = new object[3];
                paramList[0] = this; // the map
                paramList[1] = newMon; // monster actor that was spawned
                paramList[2] = spawnWandering; // wandering monster or not
                runscript.Invoke(null, paramList);
            }
        }

        if (newMon.actorfaction != Faction.PLAYER && m.dungeonLevelData.scaleMonstersToMinimumLevel > newMon.myStats.GetLevel())
        {
            newMon.ScaleToSpecificLevel(m.dungeonLevelData.scaleMonstersToMinimumLevel, false);
        }

        if (newMon.actorfaction != Faction.PLAYER && m.ScaleUpToPlayerLevel())
        {
            if (newMon.myStats.GetLevel() < GameMasterScript.heroPCActor.myStats.GetLevel())
            {
                newMon.ScaleToSpecificLevel(GameMasterScript.heroPCActor.myStats.GetLevel(), false, scaleToPlayerLevel:true);
            }            
        }                
    }

    /// <summary>
    /// If necessary, executes all monster level scaling when map is loaded.
    /// </summary>
    public void CheckForLevelScalingToPlayerLevelOnLoad()
    {
        if (!ScaleUpToPlayerLevel()) return;

        foreach(Monster m in monstersInMap)
        {
            if (m.actorfaction != Faction.ENEMY)
            {
                continue;
            }

            if (m.myStats.GetLevel() < GameMasterScript.heroPCActor.myStats.GetLevel())
            {
                m.ScaleToSpecificLevel(GameMasterScript.heroPCActor.myStats.GetLevel(), false, scaleToPlayerLevel:true);
            }
        }
    }

    /// <summary>
    /// Returns TRUE if monsters should be scaled UP to the player's current level - they will never be scaled DOWN.
    /// </summary>
    /// <returns></returns>
    public bool ScaleUpToPlayerLevel()
    {
        if (MapMasterScript.activeMap == this && dungeonLevelData.GetMetaData("scaleuptoplayerlevel") == 1 && GameMasterScript.gameLoadSequenceCompleted)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Converts bounds of a ROOM (area) to ground, optionally converting edges to walls
    /// </summary>
    /// <param name="rm"></param>
    /// <param name="startX"></param>
    /// <param name="endX"></param>
    /// <param name="startY"></param>
    /// <param name="endY"></param>
    public void OpenRoomWithBounds(Room rm, int startX, int endX, int startY, int endY, bool convertEdgesToWalls)
    {
        rm.InitializeLists();

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (!InBounds(new Vector2(x,y)))
                {
                    continue;
                }
                if (convertEdgesToWalls && (x == startX || y == startY || x == endX || y == endY))
                {
                    mapArray[x, y].ChangeTileType(TileTypes.WALL);
                    rm.edgeTiles.Add(mapArray[x, y]);
                }
                else
                {
                    mapArray[x, y].ChangeTileType(TileTypes.GROUND);
                    rm.internalTiles.Add(mapArray[x, y]);
                }
            }
        }
    }

    // #todo: Port river to use this
    public static List<MapTileData> TunnelFromTileToTile(Map createMap, MapTileData startTile, MapTileData endTile, int maxGScoreRandomization, bool tunnelThroughRooms, bool tunnelThroughGround, int areaIDOfFill = -1)
    {
        DungeonLevel dungeonLevelData = createMap.dungeonLevelData;

        if (openList == null)
        {
            openList = new PriorityQueue<MapTileData>();
        }
        else
        {
            openList.data.Clear();
        }
        
        if (tilePath == null)
        {
            tilePath = new List<MapTileData>();
        }
        else
        {
            tilePath.Clear();
        }
        
        bool[] adjacentValid = new bool[8];

        startTile.f = 0;

        MapTileData evalTile = startTile;

        MapTileData finalTile = endTile;

        createMap.FlushTilePathfindingData(startTile.pos, finalTile.pos, true);
        openList.Enqueue(startTile);

        startTile.pfState = MapTileData.openState;

        Vector2 checkAdjPos;
        MapTileData adjTile;

        while (openList.Count() > 0)
        {
            // Pathfinding            
            //Take the very best one, certified by the fact 
            //that we're using a priority queue.
            evalTile = openList.Dequeue();

            if (evalTile.pos.x == finalTile.pos.x && evalTile.pos.y == finalTile.pos.y)
            {
                // Found our path!
                finalTile = evalTile;
                break;
            }

            //rip            
            evalTile.pfState = MapTileData.closedState;

            for (int i = 0; i < MapMasterScript.directions.Length; i++)
            {
                checkAdjPos = evalTile.pos + MapMasterScript.directions[i];
                if (checkAdjPos.x > 0 && checkAdjPos.y > 0 && checkAdjPos.x < createMap.columns - 1 && checkAdjPos.y < createMap.rows - 1)
                {
                    adjTile = createMap.mapArray[(int)(evalTile.pos.x + MapMasterScript.directions[i].x), (int)(evalTile.pos.y + MapMasterScript.directions[i].y)];

                    if (Vector2.Equals(adjTile.pos, finalTile.pos))
                    {
                        adjTile.parent = evalTile;
                        evalTile.child = adjTile;
                        adjTile.f = -1;
                        adjTile.pfState = MapTileData.openState;
                        openList.Enqueue(adjTile);
                        break;
                    }

                    if (adjTile.pfState == MapTileData.closedState)
                    {
                        continue;
                    }

                    if (!tunnelThroughGround && adjTile.tileType == TileTypes.GROUND)
                    {
                        adjTile.pfState = MapTileData.closedState;
                        continue;
                    }

                    if (!tunnelThroughRooms && createMap.GetAreaID(adjTile.pos) != areaIDOfFill)
                    {
                        adjTile.pfState = MapTileData.closedState;
                        continue;
                    }

                    if (adjTile.CheckTag(LocationTags.CORRIDORENTRANCE))
                    {
                        adjTile.pfState = MapTileData.closedState;
                        continue;
                    }

                    float gScore = evalTile.g + 1;

                    // Modify gScore here

                    bool containsTile = false;

                    if (adjTile.pfState == MapTileData.openState)
                    {
                        float localF = gScore + adjTile.GetHScore(finalTile);
                        if (localF < adjTile.f)
                        {
                            // better path
                            adjTile.g = gScore;
                            adjTile.f = adjTile.g + adjTile.GetHScore(finalTile);
                            adjTile.parent = evalTile;
                            evalTile.child = adjTile;
                        }
                        containsTile = true;
                    }

                    if (!containsTile)
                    {
                        if (maxGScoreRandomization > 0)
                        {
                            gScore += UnityEngine.Random.Range(1, maxGScoreRandomization);
                        }
                        // Not in open list   
                        adjTile.parent = evalTile;
                        evalTile.child = adjTile;
                        adjTile.g = gScore; // # of steps to get to this tile
                                            //tile.h = Vector2.Distance(tile.pos, finalTile.pos) * 5f;
                                            //tile.h = MapMasterScript.GetGridDistance(tile.pos, finalTile.pos); //Modifying this because diagonals are OK.
                        adjTile.f = adjTile.g + adjTile.GetHScore(finalTile);
                        adjTile.pfState = MapTileData.openState;
                        openList.Enqueue(adjTile);
                    }


                }
            }

        }
        // End of pathfinding WHILE loop

        if (openList.Count() > 0)
        {
            // Found a path
            bool finished = false;
            MapTileData pTile = finalTile.parent;
            if (pTile == null)
            {
                return new List<MapTileData>();
            }
            tilePath.Clear();
            tilePath.Add(finalTile);
            tilePath.Add(pTile);
            while (!finished)
            {
                if (pTile.parent == null)
                {
                    return new List<MapTileData>();
                }
                if (pTile.parent == startTile)
                {
                    // Use pTile as the next move.
                    finished = true;
                    tilePath.Add(startTile);
                }
                pTile = pTile.parent;
                tilePath.Add(pTile);
            }
        }
        else
        {
            //Debug.Log("No possible path tunneling from " + startTile.pos + " to " + endTile.pos + " on " + createMap.floor);
            return new List<MapTileData>();
            // Did NOT find a path.
        }

        return tilePath;
    }

    public virtual void InitializeSwitchGateLists()
    {
        if (linkSwitchesToGates == null)
        {
            linkSwitchesToGates = new Dictionary<int, List<Destructible>>();
        }                
    }

    public bool HasHoles()
    {
        if (dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            return true;
        }
        return false;
    }

    public void RemoveLavaAndWaterFromHoles()
    {
        if (dungeonLevelData.GetMetaData("hasholes") != 1)
        {
            return;
        }

        for (int x = 1; x < columns; x++)
        {
            for (int y = 1; y < rows; y++)
            {
                if (mapArray[x,y].CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER))
                {
                    mapArray[x, y].RemoveTag(LocationTags.WATER);
                    mapArray[x, y].RemoveTag(LocationTags.LAVA);
                }
            }
        }
    }

    /// <summary>
    /// Moves actor aRef (MUST BE IN MAP) to the desired tile, handles sprite movement if needed.
    /// </summary>
    /// <param name="aRef"></param>
    /// <param name="destTile"></param>
    public void RepositionActor(Actor aRef, MapTileData destTile)
    {
        RemoveActorFromLocation(aRef.GetPos(), aRef);
        aRef.SetPos(destTile.pos);
        aRef.SetSpawnPos(destTile.pos);
        AddActorToLocation(aRef.GetPos(), aRef);
        if (aRef.GetObject() != null && aRef.myMovable != null)
        {
            aRef.myMovable.AnimateSetPosition(destTile.pos, 0.01f, false, 0f, 0f, MovementTypes.LERP);
        }
    }

    /// <summary>
    /// Removes any Stairs that point to the given 'floor', completely from this map
    /// </summary>
    /// <param name="floor"></param>
    public void RemoveStairsPointingToFloor(int floor)
    {
        if (TileInteractions.pool_removeActors == null)
        {
            TileInteractions.pool_removeActors = new List<Actor>();
        }
        TileInteractions.pool_removeActors.Clear();
        foreach(Stairs st in mapStairs)
        {
            if (st.pointsToFloor == floor || (st.NewLocation != null && st.NewLocation.floor == floor))
            {
                TileInteractions.pool_removeActors.Add(st);
            }
        }
        foreach(Actor act in TileInteractions.pool_removeActors)
        {
            RemoveActorFromMap(act);
            mapStairs.Remove(act as Stairs);
        }
    }

    /// <summary>
    /// Sets monster count range for monster spawning, adjusts local spawn table as needed. Returns vector2 where x = min, y = max monsters.
    /// </summary>
    /// <param name="minMonsters"></param>
    /// <param name="maxMonsters"></param>
    /// <param name="itemWorldProperties"></param>
    Vector2 MonsterSeedSpawnSetup(int minMonsters, int maxMonsters, ItemWorldMetaData itemWorldProperties)
    {
        localSpawnTable.Clear();

        //Debug.Log("Initialize seed " + floor + " " + challengeRating);
        //Debug.Log(dungeonLevelData.spawnTable.table.Keys.Count);

        // Now add monsters, going through one room at a time.
        monstersThisMap.Clear();
        priorityItemRefs.Clear();

        if (itemWorldProperties == null)
        {
            itemWorldProperties = new ItemWorldMetaData();
        }

        if (dungeonLevelData.itemWorld)
        {
            priorityItemRefs = GameMasterScript.heroPCActor.GetMissingSetPieces();
        }

        minMonsters = (int)(minMonsters * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.MONSTER_DENSITY));
        maxMonsters = (int)(minMonsters * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.MONSTER_DENSITY));

        if (dungeonLevelData.spawnTable == null || dungeonLevelData.spawnTable.table == null)
        {
            Debug.Log("Warning! Floor " + floor + " spawn table, or contained table, is null");
        }

        foreach (string spawnRef in dungeonLevelData.spawnTable.table.Keys)
        {
            localSpawnTable.AddToTable(spawnRef, dungeonLevelData.spawnTable.table[spawnRef]);
            //Debug.Log("Pulling " + spawnRef + " from " + floor + " spawn table " + dungeonLevelData.spawnTable.refName);
        }

#if UNITY_EDITOR
        //Debug.Log(localSpawnTable.GetNumActors() + " possible actors for " + floor + " which has spawn table " + dungeonLevelData.spawnTable.refName);
#endif

        int totalCount = localSpawnTable.GetTotalCount();
        int countToAddForSpecialSpawns = totalCount / 4;

        monsterRefsToScale.Clear();

        initialSpawnedMonsters = 0;

        for (int i = 0; i < (int)ItemWorldProperties.COUNT; i++)
        {
            if (mapMonsterTypeEnumToFamilyRef.ContainsKey(i))
            {
                if (itemWorldProperties.properties[i])
                {
                    foreach (MonsterTemplateData mtd in GameMasterScript.masterSpawnableMonsterList)
                    {
                        if (mtd.baseLevel <= dungeonLevelData.expectedPlayerLevel + 5 && mtd.monFamily == mapMonsterTypeEnumToFamilyRef[i])
                        {
                            //Debug.Log("Adding " + mtd.refName + " to table with quantity " + countToAddForSpecialSpawns);
                            if (!localSpawnTable.table.Keys.Contains(mtd.refName))
                            {
                                monsterRefsToScale.Add(mtd.refName);
                            }
                            localSpawnTable.AddToTable(mtd.refName, countToAddForSpecialSpawns);
                            //Debug.Log("Special IW: Adding " + mtd.refName + " to local spawn table " + floor);

                        }
                    }
                }
            }
        }

        return new Vector2(minMonsters, maxMonsters);
    }

    /// <summary>
    /// Spawns monsters throughout the map during map gen, regardless of room structure. They will be an even distance from each other (roughly)
    /// </summary>
    /// <param name="localMinMonsters"></param>
    /// <param name="localMaxMonsters"></param>
    /// <param name="monstersSpawnWithLoot"></param>
    /// <param name="itemWorldProperties"></param>
    void SpawnMonstersWithEvenDistribution(int localMinMonsters, int localMaxMonsters, bool monstersSpawnWithLoot, ItemWorldMetaData itemWorldProperties)
    {
        int bounds = dungeonLevelData.GetMetaData("boundsformonsterspawn");
        if (bounds < 1) bounds = 1;

        int count = UnityEngine.Random.Range(localMinMonsters, localMaxMonsters + 1);
        for (int i = 0; i < count; i++)
        {
            int x = UnityEngine.Random.Range(bounds, columns - bounds);
            int y = UnityEngine.Random.Range(bounds, rows - bounds);
            MapTileData mtd = mapArray[x, y];
            bool valid = false;
            int tries = 0;

            int minMonsterSpawnDistance = 4;
            if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.gmsSingleton.ReadTempGameData("dream_map_from_template") == 1)
            {
                minMonsterSpawnDistance = 2;
            }

            while (!valid && tries < 1500)
            {
                tries++;
                x = UnityEngine.Random.Range(bounds, columns - bounds);
                y = UnityEngine.Random.Range(bounds, rows - bounds);
                mtd = mapArray[x, y];
                if (mtd.tileType == TileTypes.GROUND && mtd.GetAllActors().Count == 0 && !mtd.AnyTerrainHazard())
                {
                    bool validPositions = true;
                    for (int g = 0; g < monstersThisMap.Count; g++)
                    {
                        if (MapMasterScript.GetGridDistance(mtd.pos, monstersThisMap[g].GetPos()) <= minMonsterSpawnDistance)
                        {
                            validPositions = false;
                            break;
                        }
                    }
                    if (validPositions)
                    {
                        Monster newMon = SpawnMonsterDuringSeedInTile(mtd, monstersSpawnWithLoot, itemWorldProperties);

                        initialSpawnedMonsters++;
                        monstersThisMap.Add(newMon);
                        valid = true;
                    }
                }
            }
            if (tries >= 1500)
            {
#if UNITY_EDITOR
                Debug.Log("Failed to spawn monster " + i + " on floor " + floor + " with layout " + dungeonLevelData.layoutType + " " + dungeonLevelData.size);
#endif
            }
        }
    }

    /// <summary>
    /// Creates a monster from this map's spawn table in the desired MTD, but should only be used during map gen. NOT for wandering monsters.
    /// </summary>
    /// <param name="mtd"></param>
    /// <param name="monstersSpawnWithLoot"></param>
    /// <param name="itemWorldProperties"></param>
    /// <returns></returns>
    Monster SpawnMonsterDuringSeedInTile(MapTileData mtd, bool monstersSpawnWithLoot, ItemWorldMetaData itemWorldProperties)
    {
        string monToSpawn = localSpawnTable.GetRandomActorRef();
        MonsterTemplateData montemplate = MonsterManagerScript.GetTemplateByRef(monToSpawn);

        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_RANGED])
        {
            bool isMonsterRanged = montemplate.IsRanged();

            while (!isMonsterRanged)
            {
                monToSpawn = localSpawnTable.GetRandomActorRef();
                montemplate = MonsterManagerScript.GetTemplateByRef(monToSpawn);
                isMonsterRanged = montemplate.IsRanged();
            }
        }

        Monster newMon = MonsterManagerScript.CreateMonster(monToSpawn, monstersSpawnWithLoot, true, false, 0f, itemWorldProperties.rewards, false, itemWorldProperties);
        newMon.dungeonFloor = floor;

        if (floor >= MapMasterScript.REALM_OF_GODS_START && floor <= MapMasterScript.REALM_OF_GODS_END)
        {
            newMon.SetActorData("realmgod", 1);
        }

        if (monsterRefsToScale.Contains(newMon.actorRefName))
        {
            int avgLevel = GameMasterScript.masterMonsterList[dungeonLevelData.spawnTable.GetRandomActorRef()].baseLevel;
            newMon.ScaleToSpecificLevel(avgLevel, false); // DON'T do expected here
        }
        // below scaling should not apply here, we handle it separately
        else if (newMon.challengeValue < dungeonLevelData.challengeValue && newMon.autoSpawn && dungeonLevelData.scaleMonstersToMinimumLevel == 0)
        {
            float diff = dungeonLevelData.challengeValue - newMon.challengeValue;
            newMon.ScaleWithDifficulty(dungeonLevelData.challengeValue);
        }

        int localMaxChamps = mgd.maxChamps;
        int localMaxChampMods = mgd.maxChampMods;
        float localChampChance = mgd.chanceToSpawnChampionMonster;
        int universalMaxChampMods = 4;
        if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1 && (floor < MapMasterScript.REALM_OF_GODS_START || floor > MapMasterScript.REALM_OF_GODS_END))
        {
            universalMaxChampMods = 5;
        }

        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_LEGENDARY])
        {
            localMaxChamps++;
            localMaxChampMods++;
            if (localMaxChampMods > universalMaxChampMods)
            {
                localMaxChampMods = universalMaxChampMods;
            }
            localChampChance *= 1.2f;
        }

        if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
        {
            localMaxChamps += 2;
        }

        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_MORECHAMPIONS])
        {
            localMaxChamps += UnityEngine.Random.Range(2, 3);
            localChampChance *= 1.1f;
        }

        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS])
        {
            localMaxChamps = 0;
        }

        if (UnityEngine.Random.Range(0, 1f) <= localChampChance && championCount < localMaxChamps && !newMon.myTemplate.cannotBeChampion)
        {
            newMon.MakeChampion();
            championCount++;
            for (int numMods = 1; numMods < localMaxChampMods; numMods++)
            {
                /* if (championCount >= mgd.maxChamps)
                {
                    break;
                } */
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f) // 50% chance for add'l mods. So 5% chance for 2 mod, 2.5% chance for 1 mod etc.
                {
                    newMon.MakeChampion();
                }
            }
        }

        newMon.startAreaID = CheckMTDArea(mtd);
        newMon.SetSpawnPosXY((int)mtd.pos.x, (int)mtd.pos.y);
        AddActorToLocation(mtd.pos, newMon);
        AddActorToMap(newMon);

        newMon.AlterFromItemWorldProperties(itemWorldProperties, priorityItemRefs);

        return newMon;
    }

    /// <summary>
    /// Spawns monsters throughout the map during map gen, placing monsters by room rather than arbitrary distance
    /// </summary>
    /// <param name="localMinMonsters"></param>
    /// <param name="localMaxMonsters"></param>
    /// <param name="monstersSpawnWithLoot"></param>
    /// <param name="itemWorldProperties"></param>
    void SpawnMonstersWithRoomBasedDistribution(int minMonsters, int maxMonsters, bool monstersSpawnWithLoot, ItemWorldMetaData itemWorldProperties)
    {
        List<MapTileData> possibleTiles = new List<MapTileData>();
        mapRooms.Shuffle();


        foreach (Room room in mapRooms)
        {
            if (initialSpawnedMonsters == maxMonsters)
            {
                break;
            }
            if (room.hasStairs ||
                (room == heroStartArea && floor == 0 && dungeonLevelData.layoutType != DungeonFloorTypes.CAVE &&
                dungeonLevelData.layoutType != DungeonFloorTypes.MAZEROOMS && dungeonLevelData.layoutType != DungeonFloorTypes.MAZE &&
                dungeonLevelData.layoutType != DungeonFloorTypes.AUTOCAVE && dungeonLevelData.layoutType != DungeonFloorTypes.VOLCANO &&
                dungeonLevelData.layoutType != DungeonFloorTypes.LAKE && dungeonLevelData.layoutType != DungeonFloorTypes.CIRCLES))
            // Should BSPRooms go here?
            {
                continue;
            }
            if (room.internalTiles.Count == 0)
            {
                continue;
            }
            // Spawn a few monsters per room.
            int count = UnityEngine.Random.Range(mgd.minMonstersPerRoom, mgd.maxMonstersPerRoom + 1);

            possibleTiles.Clear();

            for (int g = 0; g < room.internalTiles.Count; g++)
            {
                possibleTiles.Add(room.internalTiles[g]);
            }
            //possibleTiles.Shuffle();

            float roomSize = room.size.x * room.size.y;
            if (roomSize <= 36f && count > 1)
            {
                count = 1;
            }

            if (minMonsters == 0)
            {
                count = 0;
            }

            for (int i = 0; i < count; i++)
            {
                if (dungeonLevelData.spawnTable == null)
                {
                    Debug.Log("No spawn table for floor " + floor);
                }
                if (localSpawnTable.GetNumActors() == 0)
                {
                    Debug.Log("NO ACTORS in " + floor + " spawn table?!");
                }
                string monsterToSpawn = localSpawnTable.GetRandomActorRef();
                // Basic monster spawning.
                Monster newMon = MonsterManagerScript.CreateMonster(monsterToSpawn, monstersSpawnWithLoot, true, false, 0f, bonusRewards, false);

                if (floor >= MapMasterScript.REALM_OF_GODS_START && floor <= MapMasterScript.REALM_OF_GODS_END)
                {
                    newMon.SetActorData("realmgod", 1);
                }

                // Pick a random tile
                bool empty = false;
                MapTileData mtdForMonster = room.internalTiles[0];

                while (possibleTiles.Count > 0 && !empty)
                {
                    //mtdForMonster = possibleTiles[0];
                    mtdForMonster = possibleTiles[UnityEngine.Random.Range(0, possibleTiles.Count)];
                    if (mtdForMonster.IsDangerous(newMon))
                    {
                        possibleTiles.Remove(mtdForMonster);
                        continue;
                    }
                    if (Vector2.Distance(mtdForMonster.pos, heroStartTile) < 9f || mtdForMonster.GetStairsInTile() != null)
                    {
                        possibleTiles.Remove(mtdForMonster);
                        continue;
                    }
                    if (mtdForMonster.GetAllActors().Count == 0 && mtdForMonster.tileType == TileTypes.GROUND && !mtdForMonster.CheckTag(LocationTags.DUGOUT))
                    {
                        empty = true;
                    }
                    possibleTiles.Remove(mtdForMonster);
                }
                if (possibleTiles.Count == 0)
                {
                    // No more free tiles.
                    break;
                }

                possibleTiles.Remove(mtdForMonster);

                //string monsterToSpawn = possibleMonsters[UnityEngine.Random.Range(0, possibleMonsters.Count)].refName;
                newMon.dungeonFloor = floor;

                if (monsterRefsToScale.Contains(newMon.actorRefName))
                {
                    //Debug.Log("Level scaling " + newMon.actorRefName + " from " + newMon.myStats.GetLevel() + " to " + dungeonLevelData.expectedPlayerLevel);
                    newMon.ScaleToSpecificLevel(GameMasterScript.masterMonsterList[dungeonLevelData.spawnTable.GetRandomActorRef()].baseLevel, false);
                }
                else if (newMon.challengeValue < dungeonLevelData.challengeValue && newMon.autoSpawn && dungeonLevelData.scaleMonstersToMinimumLevel == 0)
                {
                    float diff = dungeonLevelData.challengeValue - newMon.challengeValue;
                    newMon.ScaleWithDifficulty(dungeonLevelData.challengeValue);
                }

                int localMaxChamps = mgd.maxChamps;
                int localMaxChampMods = mgd.maxChampMods;
                float localChampChance = mgd.chanceToSpawnChampionMonster;
                int universalMaxChampMods = 4;
                if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1 && (floor < MapMasterScript.REALM_OF_GODS_START || floor > MapMasterScript.REALM_OF_GODS_END))
                {
                    universalMaxChampMods = 5;
                }


                if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_LEGENDARY])
                {
                    localMaxChamps++;
                    localMaxChampMods++;
                    if (localMaxChampMods > universalMaxChampMods)
                    {
                        localMaxChampMods = universalMaxChampMods;
                    }
                    localChampChance *= 1.2f;
                }

                if (GameStartData.NewGamePlus > 0 && GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") != 1)
                {
                    localMaxChamps += 2;
                }

                if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_MORECHAMPIONS])
                {
                    localMaxChamps += UnityEngine.Random.Range(2, 3);
                    localChampChance *= 1.1f;
                }

                if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS])
                {
                    localMaxChamps = 0;
                }

                if (UnityEngine.Random.Range(0, 1f) <= localChampChance && championCount < localMaxChamps && !newMon.myTemplate.cannotBeChampion)
                {
                    newMon.MakeChampion();
                    championCount++;
                    for (int numMods = 1; numMods < localMaxChampMods; numMods++)
                    {
                        /* if (championCount >= mgd.maxChamps)
                        {
                            break;
                        } */
                        if (UnityEngine.Random.Range(0, 1f) <= 0.5f) // 50% chance for add'l mods. So 5% chance for 2 mod, 2.5% chance for 1 mod etc.
                        {
                            newMon.MakeChampion();
                        }
                    }
                }

                newMon.startAreaID = CheckMTDArea(mtdForMonster);
                newMon.SetSpawnPosXY((int)mtdForMonster.pos.x, (int)mtdForMonster.pos.y);
                AddActorToLocation(mtdForMonster.pos, newMon);
                AddActorToMap(newMon);
                initialSpawnedMonsters++;
                monstersThisMap.Add(newMon);

                newMon.AlterFromItemWorldProperties(itemWorldProperties, priorityItemRefs);
            }
        }
    }

    /// <summary>
    /// Called by Unity via MapMasterScript every frame
    /// </summary>
    public virtual void TickFrame()
    {
    }

    
    /// <summary>
    /// When the map finally loads, we may need to call special things on the objects in it
    /// based on unique map data. This isn't only sprite renderers, but it is called from
    /// that function in map master script.
    /// </summary>
    public virtual void UpdateSpriteRenderersOnLoad()
    {
        
    }

    /// <summary>
    /// If we have fancy data that this map needs to run, save it out here. Be sure to
    /// save the floor number in your own implementation.
    /// </summary>
    /// <param name="writer"></param>
    public virtual void WriteSpecialMapDataToSave(XmlWriter writer)
    {
        
    }


    /// <summary>
    /// If we have fancy data that this map needs to run, read it in here.
    /// Don't do anything with it yet, save that for post load.
    /// </summary>
    /// <param name="reader"></param>
    public virtual void ReadSpecialMapDataFromSave(XmlReader reader)
    {
        //How the data is loaded!
        //
        //It should look like this
        //
        //<specialmapdata>
        //    <floor>[floornum]</floor>
        //    [whatever other key/values you need to write]
        //</specialmapdata>
        //
        //the floor MUST be the first key you write to XML in your map.
        //the loading code expects this. 
        //the loading code also will open the specialmapdata element, and then
        //call readendelement when your loading function returns.
        //
        //	  while (reader.NodeType != XmlNodeType.EndElement)
        //    {
        //        string strValue = reader.Name;
        //        switch (strValue)
        //        {
        //            [kickin' rad cases/ results]
        //        }
        //        reader.Read();
        //
        //    }
        
    }

    public bool IsDragonDungeonMap()
    {
        if (floor >= MapMasterScript.FROG_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.FROG_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        if (floor >= MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        if (floor >= MapMasterScript.ROBOT_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.ROBOT_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        if (floor >= MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.SPIRIT_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        if (floor >= MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.BANDIT_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        if (floor >= MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR && floor <= MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Scan through monsters and see if any non-temporary, non-summoned monsters in a Dragon map are set to 0 xp mod - which is wrong.
    /// </summary>
    public void CheckForBadLevelScaling()
    {
        foreach(Monster m in monstersInMap)
        {
            if (m.actorfaction != Faction.ENEMY)
            {
                continue;
            }
            if (!m.myStats.IsAlive())
            {
                continue;
            }
            if (m.xpMod != 0f)
            {
                continue;
            }
            if (m.maxTurnsToDisappear > 0)
            {
                continue;
            }
            if (m.summoner != null)
            {
                continue;
            }

            if (m.levelScaled)
            {
                m.xpMod = 1f;
            }            
        }
    }

    public bool HasActor(string aRef)
    {
        foreach(Actor act in actorsInMap)
        {
            if (act.actorRefName == aRef) return true;
        }

        return false;
    }

    public Map GetNearestConnectionThatIsNotSideArea()
    {
        Map connection = null;
        Map backup = null;
        foreach (Stairs st in mapStairs)
        {
            if (!st.NewLocation.dungeonLevelData.sideArea)
            {
                connection = st.NewLocation;
                break;
            }

            backup = st.NewLocation;
        }

        Map mapConnection = null;
        mapConnection = connection ?? backup;

        return mapConnection;
    }

    public bool FindIdenticalActor(Actor a, out Actor foundActor)
    {
        foundActor = null;

        foreach(Actor act in actorsInMap)
        {
            if (a.QuickCompareTo(act))
            {
                foundActor = act;
                return true;
            }
        }

        return false;
    }

    public bool IsActorObjectInMap(Actor a)
    {
        foreach(Actor act in actorsInMap)
        {
            if (act == a) return true;
        }

        return false;
    }
}
