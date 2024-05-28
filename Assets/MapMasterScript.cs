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


public enum TileTypes { GROUND, WALL, MAPEDGE, NOTHING, HOLE, COUNT };
public enum AreaTypes { ROOM, CORRIDOR, NONE };
public enum Directions
{
    NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST, RANDOMALL, RANDOMCARDINAL, NEUTRAL, ATTACKERDIR, MONSTERTARGETDIR, OPPOSETARGET, INHERIT,
    LASTMOVEDDIR, TRUENEUTRAL, COMBATATTACKERDIR, OPPOSE_MONSTERTARGETDIRECTION, COUNT
};
public enum DungeonFloorTypes
{
    STANDARD, CAVE, STANDARDNOHALLS, RUINS, SPECIAL, AUTOCAVE, CAVEROOMS, HIVES, ISLANDS,
    FUTURENOHALLS, VOLCANO, MAZE, MAZEROOMS, LAKE, CIRCLES, BSPROOMS, KEEP, DRAGON_ROBOT, DRAGON_ROBOT_BRIDGE, BANDITDUNGEON_HUB, SPIRIT_DUNGEON,
    SLIME_DUNGEON, SLIME_DUNGEON2, COUNT
}
public enum SimpleDir { RIGHT, DOWN, LEFT, UP }
public enum ItemDreamFloorTypes { GOLDFROG_FLOOR, BIGMODE, COSTUMEPARTY, SPINMODE, FOUNTAINS, EXTRAFOOD, BERSERK, BRAWL, COUNT }

public class MovementResults
{
    public bool movedAnotherActor;
    public bool moveSuccessful;
    public Vector2 newLocation;
    public MovementResults()
    {

    }
}

public class ActorSpawnData
{
    public ActorTypes aType;
    public string refName;
    public float spawnChance;
    public int minDistanceFromStairsUp;
    public int maxDistanceFromStairsUp;
    public string metaFlag; // Required meta progress flag
    public int flagMinValue; // we can require that the flag be in a specific value range
    public int flagMaxValue;

    public ActorSpawnData()
    {
        spawnChance = 1.0f;
        metaFlag = "";
    }
}

public class SeasonalImageDataPack
{
    public Seasons whichSeason;
    public bool replaceNormalOverlay;
    public string seasonalImage;
}

/* public class MapConditionalChanges
{
    public string reqFlagName;
    public int reqFlagValue;
    public bool changeRevealAll;
    public string changeOverlay;
    public MapConditionalChanges()
    {
        reqFlagName = "";
        changeOverlay = "";
        changeRevealAll = false;
    }
} */

public class MusicCueData
{
    public string altCueName;

    public Seasons altCueSeason;
    public string altCueFlag;
    public int altCueFlagValue;
    public bool altCueFlagMeta;

    public MusicCueData()
    {
        altCueName = "";
        altCueFlag = "";
        altCueFlagValue = -99;
        altCueSeason = Seasons.COUNT;
    }
}

[System.Serializable]
public partial class MapMasterScript : MonoBehaviour
{
    //public static XXHashRNG MapRandom;

    // For object pooling
    static List<Actor> pool_actorList = new List<Actor>();
    static List<MapTileData> pool_tileList = new List<MapTileData>();
    static List<MapTileData> pool_tileList2 = new List<MapTileData>();
    static List<MapTileData> baseList = new List<MapTileData>();
    static List<MapTileData> returnList = new List<MapTileData>();
    //static List<Actor> allAnchors = new List<Actor>();
    static bool[,] tChecked;
    static bool[,] traversed;

    public static bool mapLoaded;

    public static List<Map> specialMaps;

    //public static Dictionary<int, Dictionary<MapPoint, List<RoomTemplate>>> masterDictOfEnforcedMapRoomDicts;
    //public static Dictionary<int, Dictionary<MapPoint, List<RoomTemplate>>> masterDictOfUnenforcedMapRoomDicts;

    public static Dictionary<int, List<RoomTemplate>[]> masterDictOfEnforcedMapRoomDicts;
    public static Dictionary<int, List<RoomTemplate>[]> masterDictOfUnenforcedMapRoomDicts;

    public static Dictionary<int, MapPoint[]> pointsForRoomDicts;
    public static Dictionary<int, MapPoint> pointsForRoomDictsMaxSizes;

    public static List<int> hostileDreamAuras;

    static Color[] fowColorArray;
    static Color halfTransparent = new Color(0, 0, 0, 0.5f);
    static Color fullTransparent = new Color(0, 0, 0, 0f);

    public float fungusSpawnChance;

    public float campfireFloorChance;

    public static bool dungeonCreativeActive = false;

    public const int MAX_ROOM_ATTEMPTS = 5000;
    
    public const int ITEM_WORLD_AURA_SIZE = 3;
    public const float CHANCE_CAMPFIRE_NPC = 0.4f;
    public const float CHANCE_MEMORY_FLOOR = 0.18f;
    public const float CHANCE_DRAGON_FLOOR_PER_UNLOCKED_DUNGEON = 0.0275f;
    public const float CHANCE_ITEMWORLD_BRAWLFLOOR = 0.03f;
    public const float CHANCE_ITEMWORLD_NIGHTMARE_PRINCE_SPAWN = 0.21f;
    public const float CHANCE_COOLFROG = 0.1f;
    public const float CHANCE_DARKFROG_LEGENDARY = 0.2f;
    public const float CHANCE_NIGHTMAREWORLD_DARKFROG_SPAWN = 0.35f;
    public const float CHANCE_CRACKEDROCK_GEM = 0.1f;
    public const int NUM_BEASTDUNGEON_MAPS = 5;

    // ************************************************************************************
    // Const floor IDs
    // Please keep these in numerical order, it's easier to find places to stuff new levels
    // Also I'm sure there's some missing.
#region Const Floor IDs
    public const int FILL_AREA_ID = -777;
    
    public const int BOSS1_MAP_FLOOR = 5;

    public const int PRE_FINALHUB_FLOOR = 18;
    public const int FINAL_BOSS_FLOOR = 19;
    public const int TUTORIAL_FLOOR_2 = 20;
    public const int SHARA_START_FOREST_FLOOR = 24;

    public const int TOWN_MAP_FLOOR = 100;
    
    public const int MAP_FROG_BOG = 102;
    public const int ELEMENTAL_LAIR = 108;
    public const int ELEMENTAL_LAIR3_FLOOR = 109;
    public const int FLOODED_TEMPLE_2F = 112;
    public const int JELLY_GROTTO = 116;
    public const int BRANCH_PASSAGE_POSTBOSS1 = 134;
    public const int BURIED_LOOT_FLOOR = 139;
    public const int CAMPFIRE_FLOOR = 144;
    public const int BRANCH_PASSAGE_POSTBOSS2 = 146;
    public const int TOWN2_MAP_FLOOR = 150;
    public const int TUTORIAL_FLOOR = 200;
    public const int PREBOSS1_MAP_FLOOR = 204;
    public const int PREBOSS2_MAP_FLOOR = 205;
    public const int BOSS2_MAP_FLOOR = 206;
    public const int BEASTLAKE_SIDEAREA = 207;
    public const int FINAL_SIDEAREA_1 = 208;
    public const int FINAL_SIDEAREA_2 = 209; // regenerator
    public const int FINAL_SIDEAREA_3 = 210;
    public const int FINAL_SIDEAREA_4 = 211;
    public const int FINAL_HUB_FLOOR = 212;
    public const int ROMANCE_SIDEAREA = 213;
    public const int NIGHTMARE_WORLD_FLOOR1 = 214;
    public const int BOSS3_MAP_FLOOR = 215;
    public const int NIGHTMARE_PRINCE_FLOOR = 216;
    public const int NIGHTMARE_WORLD_FLOOR2 = 217;
    public const int NIGHTMARE_WORLD_FLOOR3 = 218;
    public const int FINAL_BOSS_FLOOR2 = 222;
    public const int RESEARCH_ALCOVE_FROZEN = 223;
    public const int RESEARCH_ALCOVE_THAWED = 224;
    public const int CASINO_BASEMENT = 226;
    public const int CASINO = 110;
    public const int SPECIALFLOOR_DIMENSIONAL_RIFT = 227;
    public const int PRE_BOSS3_MEETSHARA_MAP_FLOOR = 228;
    public const int PRE_BOSS3_WALKTOBOSS_FLOOR = 229;
    public const int BANDIT_LIBRARY_FLOOR = 119;
    public const int BOTTLES_AND_BREWS = 107;

    public const int JOB_TRIAL_FLOOR = 230;
    public const int JOB_TRIAL_FLOOR2 = 231;
    public const int FINAL_BOSS_FLOOR2_OLD = 299;

    public const int SHARA_FINALBOSS_FLOOR = 314;

    //350-354 all taken for Riverstone Waterway
    public const int RIVERSTONE_WATERWAY_START = 350;
    public const int RIVERSTONE_WATERWAY_END = 354;

    public const int REALM_OF_GODS_START = 356;
    public const int REALM_OF_GODS_END = 360;

    public const int SHARA_START_CAMPFIRE_FLOOR = 355;
    public const int FROG_DRAGON_DUNGEONSTART_FLOOR = 380;
    public const int FROG_DRAGON_DUNGEONEND_FLOOR = 383;
    public const int ROBOT_DRAGON_DUNGEONSTART_FLOOR = 370;
    public const int ROBOT_DRAGON_DUNGEONEND_FLOOR = 374;
    public const int BANDIT_DRAGON_DUNGEONSTART_FLOOR = 375;
    public const int BANDIT_DRAGON_DUNGEONEND_FLOOR = 378;
    public const int BEAST_DRAGON_DUNGEONSTART_FLOOR = 385;
    public const int BEAST_DRAGON_DUNGEONEND_FLOOR = 389;
    public const int SPIRIT_DRAGON_DUNGEONSTART_FLOOR = 390;
    public const int SPIRIT_DRAGON_DUNGEONEND_FLOOR = 393;
    public const int JELLY_DRAGON_DUNGEONSTART_FLOOR = 394;
    public const int JELLY_DRAGON_DUNGEONEND_FLOOR = 397;

    public const int DRAGON_ITEMWORLD_MAP_TEMPLATES_START = 361;
    public const int DRAGON_ITEMWORLD_MAP_TEMPLATES_END = 368;

    public const int GUARDIAN_RUINS_ENTRY_FLOOR = 151;

    //400 - 500 item world, nothing inbetween can be used
    public const int ITEMWORLD_START_FLOOR = 400;
    public const int ITEMWORLD_END_FLOOR = 500;
#endregion
    // ************************************************************************************

    // Stuff for tile resources
    public static string[] earthWallSingleReplacements;
    public static string[] snowWallSingleReplacements;
    public static string[] desertWallSingleReplacements;
    public static string[] stoneWallSingleReplacements;
    public static string[] nightmareWallSingleReplacements;
    public static string[] slateWallSingleReplacements;
    public static string[] mountainGrassWallSingleReplacements;
    public static string[] lushGreenWallSingleReplacements;
    public static string[] cobbleWallSingleReplacements;
    public static string[] volcanoWallSingleReplacements;
    public static string[] treeTopsWallSingleReplacements;
    public static string[] futureWallSingleReplacements;
    public static string[] ruinedWallSingleReplacements;
    public static string[] justTrees;

    public static Sprite[] terrainAtlas; // Rivers, mud etc.
    public static Sprite[] chunk2x2DecorAtlas; // Boulders etc.
    // End stuff for tile resources

    // ***

    public static MapMasterScript singletonMMS;
    public static Dictionary<int, Map> dictAllMaps = new Dictionary<int, Map>();

    public static Map[] itemWorldMaps;
    public static int[] itemWorldIDs;
    public static Item itemWorldItem;
    public static float itemWorldMagicChance;
    public static bool itemWorldOpen;
    public static int itemWorldItemID;
    public static Item orbUsedToOpenItemWorld;

    public Map townMap;
    public Map townMap2;

    public CameraController cameraScript;

    public TileMeshGenerator mapTileMesh;
    public GameObject secondaryTileMap;
    public TileMeshGenerator mapGridMesh;
    //public FogOfWarScript unexploredFog;

    public GameObject fogOfWarQuad;
    public TileMeshGenerator fogOfWarTMG;
    public Texture2D fogOfWarTexture;

    private static bool generatingMap;
    public static GameObject heroPC;
    public HeroPC heroPCActor;
    private UIManagerScript uims;
    private GameMasterScript gms;
    public GameObject[,] mapTileObjectArray;
    public static Map activeMap;
    public static Dungeon theDungeon;
    public static int currentFloor = 0;
    public List<GameObject> activeNonTileGameObjects;

    public int mapAreaIDAssigner = 250;

    public GameObject imageOverlay;
    public List<GameObject> extraOverlays;

    private int numberOfMaps = 0;

    // Related to map generation
    public static int renderColumns;
    public static int renderRows;
    public int columns;
    public int rows;
    public float chanceConvertWallToGround;
    public float[] chanceToUseRoomTemplate;
    public float chanceToSpawnChampionMonster;
    public float chance1xWallReplace;
    public int maxChampMods;
    public int maxChamps;
    /* public Sprite[] NWOpenCornerGroundTiles;
    public Sprite[] topGroundTiles;
    public Sprite[] NEOpenCornerGroundTiles;
    public Sprite[] leftGroundTiles;
    public Sprite[] groundTiles;
    public Sprite[] StandaloneGroundTiles;
    public Sprite[] rightGroundTiles;
    public Sprite[] SWOpenCornerGroundTiles;
    public Sprite[] bottomGroundTiles;
    public Sprite[] SEOpenCornerGroundTiles;
    public Sprite[] NWClosedCornerGroundTiles;
    public Sprite[] HorizontalRunGroundTiles;
    public Sprite[] NEClosedCornerGroundTiles;
    public Sprite[] VerticalRunGroundTiles;
    public Sprite[] StandaloneWallTiles;
    public Sprite[] SWClosedCornerGroundTiles;
    public Sprite[] SEClosedCornerGroundTiles;
    public Sprite[] deadEndNorthGroundTiles;
    public Sprite[] deadEndWestGroundTiles;
    public Sprite[] deadEndEastGroundTiles;
    public Sprite[] deadEndSouthGroundTiles;
    public Sprite[] TJNorthGroundTiles;
    public Sprite[] TJWestGroundTiles;
    public Sprite[] TJSouthGroundTiles;
    public Sprite[] TJEastGroundTiles;
    public Sprite[] wallTiles;
    public Sprite[] open4CornersGroundTiles;
    public Sprite[] wallSouthCornerNW;
    public Sprite[] wallEastCornerNW;
    public Sprite[] wallWestCornerNE;
    public Sprite[] wallWestCornerSE;
    public Sprite[] wallNorthCornerSW;
    public Sprite[] wallNorthCornerSE;
    public Sprite[] wallEastCornerSW;
    public Sprite[] wallSouthCornerNE; */
    /*public Sprite[] bottomWallTiles;
    public Sprite[] topWallTiles;
    public Sprite[] leftWallTiles;
    public Sprite[] rightWallTiles;
    public Sprite[] NWCornerWallTiles;
    public Sprite[] NECornerWallTiles;
    public Sprite[] SECornerWallTiles;
    public Sprite[] SWCornerWallTiles;
    public Sprite[] NWInnerCornerWallTiles;
    public Sprite[] NEInnerCornerWallTiles;
    public Sprite[] SEInnerCornerWallTiles;
    public Sprite[] SWInnerCornerWallTiles;
    public Sprite[] westHorizontalNorthWallEndcaps;
    public Sprite[] eastHorizontalNorthWallEndcaps;
    public Sprite[] northVerticalWestWallEndcaps;
    public Sprite[] northVerticalEastWallEndcaps;
    public Sprite[] southVerticalWestWallEndcaps;
    public Sprite[] southVerticalEastWallEndcaps;
    public Sprite[] westHorizontalSouthWallEndcaps;
    public Sprite[] eastHorizontalSouthWallEndcaps;
    public Sprite[] verticalWallTiles;
    public Sprite[] horizontalWallTiles;
    public Sprite[] tOpenNorthDarkTiles;
    public Sprite[] tOpenNorthLightTiles;
    public Sprite[] tOpenWestDarkTiles;
    public Sprite[] tOpenWestLightTiles;
    public Sprite[] tOpenEastDarkTiles;
    public Sprite[] tOpenEastLightTiles;
    public Sprite[] tOpenSouthDarkTiles;
    public Sprite[] tOpenSouthLightTiles; */
    public Sprite[] doorTiles;
    public float anyDecorChance;
    public float grassDecorChance;
    public DungeonStuff[] randomDecor;
    public DungeonStuff[] decor3x3;
    public float anyPropChance;
    public int maxPropsPerRoom;
    public DungeonProp[] randomProps;
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

    public static Vector2[] directions = new Vector2[4];
    public static Vector2[] xDirections = new Vector2[8];
    public static Directions[] cardinalDirections = new Directions[6];
    public static Directions[] allDirections = new Directions[12];
    public static float[] directionAngles = new float[12];
    public static float[] oppositeDirectionAngles = new float[12];
    public static Directions[] oppositeDirections = new Directions[12];
    public static string[] visualTileSetNames;

    // For pooling

    static float minXLOS;
    static float minYLOS;
    static float maxXLOS;
    static float maxYLOS;
    static int iMinXLOS;
    static int iMaxXLOS;
    static int iMinYLOS;
    static int iMaxYLOS;
    static Vector2 checkPosLOS;
    static Point iCheckPosLOS;

    public const float CHANCE_IW_GEARSET_BONUS = 0.012f;
    public const float CHANCE_IW_GILDED_BONUS = 0.2f;
    public const float CHANCE_GOLDFROG_FLOOR = .0075f;
    public const float CHANCE_ITEMWORLD_NEW_MOD = .2f;
    public const float CHANCE_ITEMWORLD_FUNGUS = 0.25f;
    public const float CHANCE_ITEMWORLD_COSTUMEPARTY = 0.035f;
    public const float CHANCE_BERSERK_FLOOR = .03f;
    public const float CHANCE_FOUNTAIN_FLOOR = 0.05f;
    public const float CHANCE_FOOD_FLOOR = 0.015f;
    public const float CHANCE_ITEMWORLD_CHARM = 0.08f;
    public const float CHANCE_ITEMWORLD_SPIN = 0.005f;
    public const float CHANCE_ITEMWORLD_LOTSOFMONSTERS = 0.02f;
    public const float CHANCE_ITEMWORLD_BIGMODE = 0.025f;
    public const float CHANCE_RANDOM_BREAKABLE = 0.015f;

    public static Map mapUnderConstruction;
    public static void ResetAllVariablesToGameLoad()
    {
        pool_tileList = new List<MapTileData>();
        pool_actorList = new List<Actor>();
        pool_tileList = new List<MapTileData>();
        pool_tileList2 = new List<MapTileData>();
        fowColorArray = null;
        generatingMap = false;
        activeMap = null;
        mapLoaded = false;
        theDungeon = null;
        currentFloor = 0;
        dictAllMaps.Clear();
        baseList = new List<MapTileData>();
        returnList = new List<MapTileData>();
        specialMaps = new List<Map>();
        singletonMMS = null;
        itemWorldMaps = null;
        itemWorldItem = null;
        orbUsedToOpenItemWorld = null;
        heroPC = null;
        activeMap = null;
        theDungeon = null;
    }

    public static void AddMapToDictAndIncrementMapCounter(Map addMap)
    {
        foreach (Map tMap in dictAllMaps.Values)
        {
            if (tMap.floor == addMap.floor)
            {
                //shep: replacing instead of ignoring so we can create multiple instances of a test map
                //in one run. Hopefully nothing in the game relies on the old behavior.
                addMap.mapAreaID = tMap.mapAreaID;
                dictAllMaps[addMap.mapAreaID] = addMap;

                //don't increment the assigner, because we replaced instead of created.
                return;

                /*
                Debug.Log("WARNING: Attempt to add duplicate floor " + tMap.floor + " already in dict, skipping.");
                return;
                */
            }
        }

        singletonMMS.mapAreaIDAssigner++;
        AddMapToDict(addMap);
    }

    public static void AddMapToDict(Map addMap)
    {
        //Debug.Log("Adding Floor " + addMap.floor + " Map ID " + addMap.mapAreaID + " to dict.");
        try { dictAllMaps.Add(addMap.mapAreaID, addMap); }
        catch (Exception e)
        {
            Debug.Log("Trying to add floor " + addMap.floor + " map ID " + addMap.mapAreaID + " but failed due to " + e.ToString());
        }
    }

    public static bool GeneratingMap()
    {
        return generatingMap;
    }

    public MapMasterScript()
    {
        extraOverlays = new List<GameObject>();
    }

    public static MapTileData FindNearbyEmptyTileForItem(Vector2 center, bool canSpawnOnNonCollidableDestructibles = false, bool dontUseCenterTile = false, bool dontSpawnNorth = false)
    {
        pool_tileList.Clear();
        pool_tileList2.Clear(); // Checked
        MapTileData mtd = GetTile(center);

        //if ((mtd.AreItemsOrDestructiblesInTile()) || (mtd.IsCollidable(GameMasterScript.heroPCActor)))
        if (dontUseCenterTile || !mtd.IsTileEmptyForItem(canSpawnOnNonCollidableDestructibles))
        {
            pool_tileList = GetNonCollidableTilesAroundPoint(center, 5, GameMasterScript.heroPCActor);
            MapTileData best = null;
            float shortest = 99f;
            foreach (MapTileData checkMTD in pool_tileList)
            {
                if (dontSpawnNorth && checkMTD.pos.y > center.y)
                {
                    continue;
                }
                if (checkMTD.IsTileEmptyForItem(canSpawnOnNonCollidableDestructibles))
                {
                    //Debug.Log(checkMTD.pos + " is potentially valid.");
                    float dist = GetGridDistance(checkMTD.pos, mtd.pos);
                    if (dist < shortest)
                    {
                        if (!CustomAlgorithms.CheckBresenhamsLOS(center, checkMTD.pos, activeMap)) //, GameMasterScript.heroPCActor, activeMap))
                        {
                            dist *= 1.5f;
                        }
                        if (checkMTD.CheckTag(LocationTags.LAVA))
                        {
                            dist *= 1.3f;
                        }
                        if (dist < shortest)
                        {
                            shortest = dist;
                            best = checkMTD;
                        }
                    }
                }
            }
            if (best != null)
            {
                //Debug.Log("Nearest empty tile is " + best.pos + " dist " + shortest);
                return best;
            }
            else
            {
                Debug.Log("Couldn't find anything.");
                return mtd;
            }
        }
        else
        {
            return mtd;
        }
    }

    public static void InitTileSetNames()
    {
        visualTileSetNames = new string[(int)TileSet.COUNT];
        visualTileSetNames[(int)TileSet.COBBLE] = "Cobble";
        visualTileSetNames[(int)TileSet.STONE] = "Stone";
        visualTileSetNames[(int)TileSet.SLATE] = "Slate";
        visualTileSetNames[(int)TileSet.LUSHGREEN] = "LushGreen";
        visualTileSetNames[(int)TileSet.SNOW] = "Snow";
        visualTileSetNames[(int)TileSet.EARTH] = "Earth";
        visualTileSetNames[(int)TileSet.MOSS] = "Moss";
        visualTileSetNames[(int)TileSet.BLUESTONELIGHT] = "LightStone";
        visualTileSetNames[(int)TileSet.BLUESTONEDARK] = "DarkStone";
        visualTileSetNames[(int)TileSet.FUTURE] = "Future";
        visualTileSetNames[(int)TileSet.RUINED] = "Ruined";
        visualTileSetNames[(int)TileSet.VOLCANO] = "Volcano";
        visualTileSetNames[(int)TileSet.SAND] = "Sand";
        visualTileSetNames[(int)TileSet.VOID] = "Void";
        visualTileSetNames[(int)TileSet.SPECIAL] = "Earth";
        visualTileSetNames[(int)TileSet.REINFORCED] = "Reinforced";
        visualTileSetNames[(int)TileSet.NIGHTMARISH] = "Nightmarish";
        visualTileSetNames[(int)TileSet.TREETOPS] = "Treetops";
        visualTileSetNames[(int)TileSet.MOUNTAINGRASS] = "MountainGrass";
    }

    public void MainMapStart()
    {
        hostileDreamAuras = new List<int>();
        hostileDreamAuras.Add((int)ItemWorldAuras.PLAYERSEALED);
        hostileDreamAuras.Add((int)ItemWorldAuras.MONSTERREGEN5);
        hostileDreamAuras.Add((int)ItemWorldAuras.TOUGHMONSTER);
        hostileDreamAuras.Add((int)ItemWorldAuras.MONSTER_CLEARSTATUS);

        cobbleWallSingleReplacements = new string[2];
        cobbleWallSingleReplacements[0] = "BronzePillar1";
        cobbleWallSingleReplacements[1] = "BronzePillar2";

        stoneWallSingleReplacements = new string[6];
        stoneWallSingleReplacements[0] = "StonePillar1";
        stoneWallSingleReplacements[1] = "StonePillar2";
        stoneWallSingleReplacements[2] = "StonePillar3";
        stoneWallSingleReplacements[3] = "StonePillar4";
        stoneWallSingleReplacements[4] = "StonePillar5";
        stoneWallSingleReplacements[5] = "StonePillar6";

        nightmareWallSingleReplacements = new string[4];
        nightmareWallSingleReplacements[0] = "NightmarePillar1";
        nightmareWallSingleReplacements[1] = "NightmarePillar2";
        nightmareWallSingleReplacements[2] = "NightmarePillar3";
        nightmareWallSingleReplacements[3] = "NightmarePillar4";

        desertWallSingleReplacements = new string[2];
        desertWallSingleReplacements[0] = "PalmTree1";
        desertWallSingleReplacements[1] = "PalmTree2";

        snowWallSingleReplacements = new string[3];
        snowWallSingleReplacements[0] = "SnowTree1";
        snowWallSingleReplacements[1] = "SnowTree2";
        snowWallSingleReplacements[2] = "SnowTree3";

        earthWallSingleReplacements = new string[4];
        earthWallSingleReplacements[0] = "EarthPillar1";
        earthWallSingleReplacements[1] = "EarthPillar2";
        earthWallSingleReplacements[2] = "EarthPillar3";
        earthWallSingleReplacements[3] = "EarthPillar4";
        /* earthWallSingleReplacements[4] = "EarthBush1";
        earthWallSingleReplacements[5] = "EarthBush2"; */

        volcanoWallSingleReplacements = new string[7];
        volcanoWallSingleReplacements[0] = "VolcanoBush1";
        volcanoWallSingleReplacements[1] = "VolcanoBush2";
        volcanoWallSingleReplacements[2] = "VolcanoBush3";
        volcanoWallSingleReplacements[3] = "VolcanoBush4";
        volcanoWallSingleReplacements[4] = "VolcanoBush5";
        volcanoWallSingleReplacements[5] = "MagmaPillar1";
        volcanoWallSingleReplacements[6] = "MagmaPillar2";

        treeTopsWallSingleReplacements = new string[]
        {
            "Bush1",
            "Bush2",
            "Bush3",
            "Bush4",
            "Bush5",
            "Bush6",
            "Bush7",
            "Bush8",
            "Bush9",
            "Bush10",
        };

        mountainGrassWallSingleReplacements = new string[21];
        for (int i = 0; i < 10; i++)
        {
            mountainGrassWallSingleReplacements[i] = treeTopsWallSingleReplacements[i];
        }
        mountainGrassWallSingleReplacements[10] = "Tree1";
        mountainGrassWallSingleReplacements[11] = "Tree2";
        mountainGrassWallSingleReplacements[12] = "Tree3";
        mountainGrassWallSingleReplacements[13] = "Tree8";
        mountainGrassWallSingleReplacements[14] = "Tree5";
        mountainGrassWallSingleReplacements[15] = "Tree7";
        mountainGrassWallSingleReplacements[16] = "CherryTree";
        mountainGrassWallSingleReplacements[17] = "RareTreeAdult";
        mountainGrassWallSingleReplacements[18] = "RareTreeSapling";
        mountainGrassWallSingleReplacements[19] = "UncommonTreeAdult";
        mountainGrassWallSingleReplacements[20] = "UncommonTreeAdult2";


        lushGreenWallSingleReplacements = new string[20];
        lushGreenWallSingleReplacements[0] = "Tree1";
        lushGreenWallSingleReplacements[1] = "Tree2";
        lushGreenWallSingleReplacements[2] = "Tree3";
        lushGreenWallSingleReplacements[3] = "Tree1";
        lushGreenWallSingleReplacements[4] = "Tree2";
        lushGreenWallSingleReplacements[5] = "Tree3";
        lushGreenWallSingleReplacements[6] = "Bush1";
        lushGreenWallSingleReplacements[7] = "Bush2";
        lushGreenWallSingleReplacements[8] = "Bush3";
        lushGreenWallSingleReplacements[9] = "Bush4";
        lushGreenWallSingleReplacements[10] = "Bush5";
        lushGreenWallSingleReplacements[11] = "Bush6";
        lushGreenWallSingleReplacements[12] = "Bush7";
        lushGreenWallSingleReplacements[13] = "Bush8";
        lushGreenWallSingleReplacements[14] = "Bush9";
        lushGreenWallSingleReplacements[15] = "Bush10";
        lushGreenWallSingleReplacements[16] = "Tree5";
        lushGreenWallSingleReplacements[17] = "Tree5";
        lushGreenWallSingleReplacements[18] = "Tree7";
        lushGreenWallSingleReplacements[19] = "Tree8";

        slateWallSingleReplacements = new string[25];
        slateWallSingleReplacements[0] = "Tree1";
        slateWallSingleReplacements[1] = "Tree2";
        slateWallSingleReplacements[2] = "Tree3";
        slateWallSingleReplacements[3] = "Tree1";
        slateWallSingleReplacements[4] = "Tree2";
        slateWallSingleReplacements[5] = "Tree3";
        slateWallSingleReplacements[6] = "Bush1";
        slateWallSingleReplacements[7] = "Bush2";
        slateWallSingleReplacements[8] = "Bush3";
        slateWallSingleReplacements[9] = "Bush4";
        slateWallSingleReplacements[10] = "Bush5";
        slateWallSingleReplacements[11] = "Bush6";
        slateWallSingleReplacements[12] = "Bush7";
        slateWallSingleReplacements[13] = "Bush8";
        slateWallSingleReplacements[14] = "Bush9";
        slateWallSingleReplacements[15] = "Bush10";
        slateWallSingleReplacements[16] = "Tree5";
        slateWallSingleReplacements[17] = "Tree5";
        slateWallSingleReplacements[18] = "SlateTree1";
        slateWallSingleReplacements[19] = "SlateTree1";
        slateWallSingleReplacements[20] = "SlateTree1";
        slateWallSingleReplacements[21] = "Tree7";
        slateWallSingleReplacements[22] = "Tree8";
        slateWallSingleReplacements[23] = "Tree7";
        slateWallSingleReplacements[24] = "Tree8";

        futureWallSingleReplacements = new string[7];
        futureWallSingleReplacements[0] = "FutureBush1";
        futureWallSingleReplacements[1] = "FutureBush2";
        futureWallSingleReplacements[2] = "FutureBush3";
        futureWallSingleReplacements[3] = "FutureBush4";
        futureWallSingleReplacements[4] = "FutureBush5";
        futureWallSingleReplacements[5] = "FutureBush6";
        futureWallSingleReplacements[6] = "FutureBush7";

        ruinedWallSingleReplacements = new string[3];
        ruinedWallSingleReplacements[0] = "RuinedTree1";
        ruinedWallSingleReplacements[1] = "RuinedTree2";
        ruinedWallSingleReplacements[2] = "RuinedTree4";


        justTrees = new string[6];
        justTrees[0] = "Tree1";
        justTrees[1] = "Tree2";
        justTrees[2] = "Tree3";
        justTrees[3] = "Tree5";
        justTrees[4] = "Tree7";
        justTrees[5] = "Tree8";


            // This shouldn't be necessary.
        InitializeDictAllMapsIfNecessary();

        terrainAtlas = Resources.LoadAll<Sprite>("Art/Tilesets/river_lava_mud");
        chunk2x2DecorAtlas = Resources.LoadAll<Sprite>("Art/Tilesets/2x2-template");

        singletonMMS = this;
        mapTileMesh = GameObject.Find("TileMap").GetComponent<TileMeshGenerator>();
        mapGridMesh = GameObject.Find("GridMap").GetComponent<TileMeshGenerator>();

        fogOfWarTexture = null;
        fogOfWarQuad = GameObject.Find("RenderedFog");
        fogOfWarTMG = fogOfWarQuad.GetComponent<TileMeshGenerator>();

        GameObject go = GameObject.Find("UIManager");
        uims = go.GetComponent<UIManagerScript>();
        cameraScript = GameObject.Find("Main Camera").GetComponent<CameraController>();

        go = GameObject.Find("GameMaster");
        gms = go.GetComponent<GameMasterScript>();
    }

    public static int GetColumns()
    {
        return activeMap.columns;
    }

    public static int GetRows()
    {
        return activeMap.rows;
    }

    public static Area GetFillArea()
    {
        return activeMap.areaDictionary[-777];
    }

    public class DungeonInfo
    {
        public float baseChallenge;
        public int floors;
        public float challengeScale;

        public DungeonInfo(int fl, float baseC, float cs)
        {
            floors = fl;
            baseChallenge = baseC;
            challengeScale = cs;
        }
    }

    public static bool CheckAdjacentTileType(Vector2 coords, TileTypes type, bool countCenterTile)
    {
        return activeMap.CheckAdjacentTileType(coords, type, countCenterTile);
    }
   
    public static Directions RotateDirection90(Directions start)
    {
        switch (start)
        {
            case Directions.NORTH:
                return Directions.EAST;
            case Directions.NORTHEAST:
                return Directions.SOUTHEAST;
            case Directions.EAST:
                return Directions.SOUTH;
            case Directions.SOUTHEAST:
                return Directions.SOUTHWEST;
            case Directions.SOUTH:
                return Directions.WEST;
            case Directions.SOUTHWEST:
                return Directions.NORTHWEST;
            case Directions.WEST:
                return Directions.NORTH;
            case Directions.NORTHWEST:
                return Directions.NORTHEAST;
        }

        return Directions.NORTH;
    }
    
    void DestroyObjects()
    {
        for (int i = 0; i < activeNonTileGameObjects.Count; i++)
        {
            if (activeNonTileGameObjects[i] != null)
            {
                if (activeNonTileGameObjects[i].activeSelf)
                {
                    GameMasterScript.TryReturnChildrenToStack(activeNonTileGameObjects[i]);
                }
                //Debug.Log("Try to destroy " + activeNonTileGameObjects[i].name);
                string possibleRefName = activeNonTileGameObjects[i].name.Replace("(Clone)", String.Empty);
                GameMasterScript.ReturnToStack(activeNonTileGameObjects[i], possibleRefName);
            }
        }
        activeNonTileGameObjects.Clear();
    }

    public void SpawnMapOverlays()
    {
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Preparing to spawn map overlays.");

        Vector3 gPos = Vector3.zero;
        gPos.x = (activeMap.columns / 2f) + 0.5f;
        gPos.y = (activeMap.rows / 2f) + 0.5f;
        //UIManagerScript.SetGridOverlayPosition(gPos);
        if (activeMap.dungeonLevelData.imageOverlay != null && activeMap.dungeonLevelData.imageOverlay != "")
        {

            string mainOverlayRef = activeMap.dungeonLevelData.imageOverlay;

            // Check seasonal stuff.
            foreach (SeasonalImageDataPack sid in activeMap.dungeonLevelData.seasonalImages)
            {
                if (sid.replaceNormalOverlay && GameMasterScript.seasonsActive[(int)sid.whichSeason])
                {
                    mainOverlayRef = sid.seasonalImage;
                    if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Replace regular map with seasonal overlay " + mainOverlayRef);
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
                Debug.Log("SEASONDEBUG Could not find overlay for level " + activeMap.floor + ": " + activeMap.dungeonLevelData.imageOverlay);
            }
            else
            {
                imageOverlay = (GameObject)Instantiate(Resources.Load("ImageOverlay"));
                imageOverlay.GetComponent<SpriteRenderer>().sprite = spr;

                if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Seasonal overlay sprite loaded and set up: " + spr.name);

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
                mapTileMesh.gameObject.SetActive(false); // Hide the background behind maps with a special overlay
            }
        }
        else
        {
            mapTileMesh.gameObject.SetActive(true);
            mapGridMesh.gameObject.SetActive(PlayerOptions.gridOverlay);
        }

        List<string> allOverlaysTemp = new List<string>();
        foreach (SeasonalImageDataPack sid in activeMap.dungeonLevelData.seasonalImages)
        {
            if (!sid.replaceNormalOverlay && GameMasterScript.seasonsActive[(int)sid.whichSeason])
            {
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
                Debug.Log("SEASONDEBUG Could not find extra overlay for level " + activeMap.floor + " " + extra);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Seasonal overlay extra sprite to be loaded: " + extra);
                extraOverlays.Add(overlay);
                // It's good to go.
            }
        }

        //If the dungeonLevelData has a parallax background, display it here.
        activeMap.UpdateParallaxBackground();
    }

    public void CheckRevealMap()
    {
        if (activeMap.dungeonLevelData.revealAll)
        {
            fogOfWarQuad.SetActive(false);
        }
        else
        {
            fogOfWarQuad.SetActive(true);
        }
    }

    public Map GetMapFromDict(int id)
    {
        Map outMap;
        if (dictAllMaps.TryGetValue(id, out outMap))
        {
            return outMap;
        }
        else
        {
            Debug.Log("Could not find " + id + " in map dict.");
            return null;
        }
    }


    public static List<Actor> GetMonstersAroundTile(Vector2 tile)
    {
        pool_actorList.Clear();
        Vector2 checkPos = tile;
        MapTileData mtd;
        Actor mon;
        for (int i = 0; i < xDirections.Length; i++)
        {
            checkPos = tile + xDirections[i];
            if (InBounds(checkPos))
            {
                mtd = GetTile(checkPos);
                mon = mtd.GetMonster();
                if (mon != null)
                {
                    pool_actorList.Add(mon);
                }
            }
        }
        return pool_actorList;
    }

    public static List<Actor> GetFactionMonstersAroundTile(Vector2 tile, Faction f)
    {
        pool_actorList.Clear();
        Vector2 checkPos = tile;
        MapTileData mtd;
        Actor mon;
        for (int i = 0; i < xDirections.Length; i++)
        {
            checkPos = tile + xDirections[i];
            if (InBounds(checkPos))
            {
                mtd = GetTile(checkPos);
                mon = mtd.GetMonster();
                if ((mon != null) && (mon.actorfaction == f))
                {
                    pool_actorList.Add(mon);
                }
            }
        }
        return pool_actorList;
    }

    /// <summary>
    /// Returns a collection of breakable objects that have not already been broken adjacent to a given tile.
    /// </summary>
    /// <param name="vTile">The desired tile's location on the map</param>
    /// <returns>A list containing every neighboring destructible</returns>
    public static List<Actor> GetUsableBreakableCollidablesForHeroAroundTile(Vector2 vTile)
    {
        pool_actorList.Clear();
        MapTileData mtd;
        for (int i = 0; i < xDirections.Length; i++)
        {
            mtd = GetTile(vTile + xDirections[i]);
            if (mtd == null) continue;
            var a = mtd.GetBreakableCollidable(GameMasterScript.heroPCActor);
            if( a != null && !a.isDestroyed )
            {
                pool_actorList.Add(a);
            }

        }
        return pool_actorList;
    }
    public static int CountMobsInZone(string refName)
    {
        int count = 0;
        for (int i = 0; i < activeMap.monstersInMap.Count; i++)
        {
            if (activeMap.monstersInMap[i].actorRefName == refName)
            {
                count++;
            }
        }
        return count;
    }

    public void SwitchFloors(int floorMod, bool forced)
    {
        //Map switchToMap = theDungeon.levels[currentFloor + floorMod];
        Map switchToMap = theDungeon.FindFloor(GameMasterScript.heroPCActor.dungeonFloor + floorMod);
        //Debug.Log("Switching to map floor " + (GameMasterScript.heroPCActor.dungeonFloor + floorMod) + " ref " + switchToMap.floor);
        SwitchMaps(switchToMap, Vector2.zero, forced);
    }

    public bool TrySwitchFloor(string strFloorData, int iFloorOffset)
    {
        Map switchToMap = theDungeon.FindFloorViaData(strFloorData, iFloorOffset);
        if (switchToMap == null)
        {
            return false;
        }
        SwitchMaps(switchToMap, Vector2.zero, true);
        return true;
    }

    public static MapTileData GetRandomEmptyTile(Vector2 center, int radius, bool expandSearch, bool preferClose, bool anyNonCollidable = false, bool preferLOS = false, bool avoidTilesWithPowerups = false)
    {
        return activeMap.GetRandomEmptyTile(center, radius, preferClose, anyNonCollidable: anyNonCollidable, preferLOS: preferLOS, avoidTilesWithPowerups: avoidTilesWithPowerups);
    }

    public static MapTileData GetRandomNonCollidableTile(Vector2 center, int radius, bool expandSearch, bool preferClose)
    {
        return activeMap.GetRandomNonCollidableTile(center, radius, expandSearch, preferClose);
    }
    
    public static void RemoveActor(string refName)
    {
        Actor actObj = null;
        foreach (Actor act in activeMap.actorsInMap)
        {
            if (act.actorRefName == refName)
            {
                actObj = act;
                break;
            }
        }
        if (actObj != null)
        {
            activeMap.RemoveActorFromLocation(actObj.GetPos(), actObj);
            activeMap.RemoveActorFromMap(actObj);
            if (actObj.objectSet)
            {
                GameMasterScript.TryReturnChildrenToStack(actObj.GetObject());
                GameMasterScript.ReturnActorObjectToStack(actObj, actObj.GetObject());
            }
        }
    }

    public static int GetItemWorldAura(Vector2 pos, bool doubleCheckForItemWorld = true)
    {
        if (doubleCheckForItemWorld && !activeMap.IsItemWorld()) return -1;
        MapTileData mtd = GetTile(pos);
        Monster mn;
        for (int i = 0; i < activeMap.monstersInMap.Count; i++)
        {
            mn = activeMap.monstersInMap[i];
            if (mn.actorRefName == "mon_itemworldcrystal" && mn.myStats.IsAlive())
            {
                if (MapMasterScript.GetGridDistance(pos, mn.GetPos()) <= ITEM_WORLD_AURA_SIZE)
                {
                    return mn.ReadActorData("itemworldaura");
                }
            }
        }
        return -1;
    }


    void Start()
    {
        specialMaps = new List<Map>();
    }

    public static void EnableMap(Map m)
    {
        foreach (Stairs s in m.mapStairs)
        {
            s.EnableActor();
        }

        foreach (Map m2 in dictAllMaps.Values)
        {
            foreach (Stairs s in m2.mapStairs)
            {
                if (s.NewLocation == m)
                {
                    s.EnableActor();
                }
            }
        }

        //if (Debug.isDebugBuild) Debug.Log("Map enabled: " + m.GetName() + " " + m.floor);
    }

    void Awake()
    {
        /* int rngValue = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(rngValue);
        MyExtensions.InitRNG(rngValue); */
        InitializeDictAllMapsIfNecessary();

        itemWorldOpen = false;
        mapAreaIDAssigner = 250;
        directions[0] = new Vector2(0, 1); // North
        directions[1] = new Vector2(1, 0); // East
        directions[2] = new Vector2(0, -1); // South
        directions[3] = new Vector2(-1, 0); // West

        cardinalDirections[0] = Directions.NORTH;
        cardinalDirections[1] = Directions.EAST;
        cardinalDirections[2] = Directions.SOUTH;
        cardinalDirections[3] = Directions.WEST;

        allDirections[0] = Directions.NORTH;
        allDirections[1] = Directions.NORTHEAST;
        allDirections[2] = Directions.EAST;
        allDirections[3] = Directions.SOUTHEAST;
        allDirections[4] = Directions.SOUTH;
        allDirections[5] = Directions.SOUTHWEST;
        allDirections[6] = Directions.WEST;
        allDirections[7] = Directions.SOUTHWEST;

        oppositeDirections[(int)Directions.NORTH] = Directions.SOUTH; // FACING north
        oppositeDirections[(int)Directions.EAST] = Directions.WEST; // FACING east
        oppositeDirections[(int)Directions.SOUTH] = Directions.NORTH;
        oppositeDirections[(int)Directions.WEST] = Directions.EAST;
        oppositeDirections[(int)Directions.NORTHEAST] = Directions.SOUTHWEST;
        oppositeDirections[(int)Directions.SOUTHEAST] = Directions.NORTHWEST;
        oppositeDirections[(int)Directions.SOUTHWEST] = Directions.NORTHEAST;
        oppositeDirections[(int)Directions.NORTHWEST] = Directions.SOUTHEAST;

        directionAngles[(int)Directions.NORTH] = 0; // FACING north
        directionAngles[(int)Directions.EAST] = -90f; // FACING east
        directionAngles[(int)Directions.SOUTH] = 180f;
        directionAngles[(int)Directions.WEST] = 90f;
        directionAngles[(int)Directions.NORTHEAST] = -45f;
        directionAngles[(int)Directions.SOUTHEAST] = -135f;
        directionAngles[(int)Directions.SOUTHWEST] = 135f;
        directionAngles[(int)Directions.NORTHWEST] = 45f;

        oppositeDirectionAngles[(int)Directions.NORTH] = 180; // FACING north
        oppositeDirectionAngles[(int)Directions.EAST] = 90f; // FACING east
        oppositeDirectionAngles[(int)Directions.SOUTH] = 0f;
        oppositeDirectionAngles[(int)Directions.WEST] = -90f;
        oppositeDirectionAngles[(int)Directions.NORTHEAST] = 135f;
        oppositeDirectionAngles[(int)Directions.SOUTHEAST] = 45f;
        oppositeDirectionAngles[(int)Directions.SOUTHWEST] = -45f;
        oppositeDirectionAngles[(int)Directions.NORTHWEST] = -135f;

        xDirections[(int)Directions.NORTH] = new Vector2(0, 1); // North
        xDirections[(int)Directions.NORTHEAST] = new Vector2(1, 1); // Northeast
        xDirections[(int)Directions.EAST] = new Vector2(1, 0); // East
        xDirections[(int)Directions.SOUTHEAST] = new Vector2(1, -1); // Southeast
        xDirections[(int)Directions.SOUTH] = new Vector2(0, -1); // South
        xDirections[(int)Directions.SOUTHWEST] = new Vector2(-1, -1); // Southwest
        xDirections[(int)Directions.WEST] = new Vector2(-1, 0); // West
        xDirections[(int)Directions.NORTHWEST] = new Vector2(-1, 1); // Northwest
    }

    public static void RebuildMapMesh()
    {
        singletonMMS.mapTileMesh.BuildMesh();
        singletonMMS.mapGridMesh.BuildMesh();
    }

    public static float GetAngleFromDirection(Directions dir)
    {
        switch (dir)
        {
            case Directions.NORTH:
                return 0f;
            case Directions.NORTHEAST:
                return 45f;
            case Directions.EAST:
                return 90f;
            case Directions.SOUTHEAST:
                return 135f;
            case Directions.SOUTH:
                return 180f;
            case Directions.SOUTHWEST:
                return -135f;
            case Directions.WEST:
                return -90f;
            case Directions.NORTHWEST:
                return -45f;
        }
        return 0.0f;
    }

    public static Directions GetDirectionFromAngle(float angle)
    {
        if ((angle > -22f) && (angle <= 22f))
        {
            return Directions.NORTH;
        }
        if ((angle > 22f) && (angle <= 67f))
        {
            return Directions.NORTHEAST;
        }
        if ((angle > 67f) && (angle <= 113f))
        {
            return Directions.EAST;
        }
        if ((angle > 113f) && (angle <= 158f))
        {
            return Directions.SOUTHEAST;
        }

        if ((angle > 158f) && (angle <= 180f))
        {
            return Directions.SOUTH;
        }
        if ((angle >= -180f) && (angle < -158f))
        {
            return Directions.SOUTH;
        }
        if ((angle >= -158f) && (angle <= -113f))
        {
            return Directions.SOUTHWEST;
        }
        if ((angle > -113f) && (angle <= -67f))
        {
            return Directions.WEST;
        }
        if ((angle > -67f) && (angle <= -22f))
        {
            return Directions.NORTHWEST;
        }
        return Directions.NEUTRAL;
    }

    public void WanderingMonsterCheck()
    {
        int max = activeMap.initialSpawnedMonsters;

        bool heroIsDangerMagnet = GameMasterScript.heroPCActor.ReadActorData("dangermagnet") == 1;

        int attempts = 1;
        if (heroIsDangerMagnet) attempts = 2;

        for (int i = 0; i < attempts; i++)
        {
            if (activeMap.unfriendlyMonsterCount < max)
            {
                float localSpawnRate = activeMap.dungeonLevelData.spawnRateModifier;
                if (GameStartData.NewGamePlus >= 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
                {
                    localSpawnRate *= 1.5f;
                }

                //Debug.Log("Compare unfriendly count " + activeMap.unfriendlyMonsterCount + " to max of " + max + " and rate mod of " + activeMap.dungeonLevelData.spawnRateModifier);
                if (UnityEngine.Random.Range(0, 1f) > localSpawnRate)
                {
                    continue;
                }
                float percentRemaining = (float)activeMap.unfriendlyMonsterCount / (float)max;
                //Debug.Log("Current creatures out of total creatures is " + percentRemaining);

                float spawnRateAdjustmentFromMods = PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.MONSTER_SPAWN_RATE);

                float roll = UnityEngine.Random.Range(-11f, 1.0f);

                // Yes this is weird. The way we check for wandering monsters is weird.
                if (spawnRateAdjustmentFromMods > 1f)
                {
                    percentRemaining = percentRemaining / spawnRateAdjustmentFromMods;
                    roll += (1f - spawnRateAdjustmentFromMods);
                }
                else if (spawnRateAdjustmentFromMods < 1f)
                {
                    if (UnityEngine.Random.Range(0,1f) > spawnRateAdjustmentFromMods)
                    {
                        continue;
                    }
                }

                if (GameStartData.NewGamePlus >= 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
                {
                    roll += 1;
                    percentRemaining *= 1.2f;
                }

                if (roll >= percentRemaining)
                {
                    // In Shara mode, wandering monster logic might be altered instead of spawning a monster.
                    if (SharaModeStuff.SharaWanderingMonsterCheck(max))
                    {
                        return;
                    }

                    // Spawn a random monster.
                    try
                    {
                        activeMap.SpawnRandomMonster(false, true);
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error with wandering monster spawn on floor " + activeMap.floor);
                        Debug.Log(e);
                    }
                }
            }
        }
    }

    public static TileTypes GetTileType(int x, int y)
    {
        // Debug.Log(x + "," + y);
        MapTileData mtd = activeMap.mapArray[x, y];
        return mtd.tileType;
    }

    public bool GetExplored(int x, int y)
    {
        return activeMap.exploredTiles[x, y];
    }

    public bool AddActorToMap(Actor act)
    {
        return activeMap.AddActorToMap(act);
    }

    public void RemoveActorFromMap(Actor act)
    {
        activeMap.RemoveActorFromMap(act);
    }

    public Destructible GetAltarNearbyPlayer()
    {
        float shortestDistance = 9999f;
        float dist = 0f;
        Destructible nearestAltar = null;
        for (int i = 0; i < activeMap.actorsInMap.Count; i++)
        {
            Actor act = activeMap.actorsInMap[i];
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType == SpecialMapObject.ALTAR)
                {
                    dist = GetGridDistance(GameMasterScript.heroPCActor.GetPos(), act.GetPos());
                }
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    nearestAltar = dt;
                }
            }
        }
        return nearestAltar;
    }

    public List<Actor> GetAllActors()
    {
        return activeMap.actorsInMap;
    }

    public List<Item> GetAllItemsAtLocation(Vector2 coords)
    {
        return activeMap.mapArray[(int)coords.x, (int)coords.y].GetItemsInTile();
    }

    public Vector2 GetMapSize()
    {
        Vector2 size = new Vector2(columns, rows);
        return size;
    }

    public void UpdateMapObjectData(bool updateFromBufferedHeroPOS = false)
    {
        CheckAllTileVisibility(updateFromBufferedHeroPOS);
        CheckMapActorsLOS(updateFromBufferedHeroPOS);
    }

    public static void FlushTilePathfindingData(Vector2 start, Vector2 finish, bool mapCreation)
    {
        activeMap.FlushTilePathfindingData(start, finish, mapCreation);
    }

    private float CalculateDistance(Vector3 a, Vector3 b)
    {
        float diffX = Mathf.Abs(a.x - b.x);
        float diffY = Mathf.Abs(a.y - b.y);
        float distance = diffX + diffY;
        return distance;
    }

    public static List<MapTileData> GetNonCollidableTilesAroundPoint(Vector2 centerTile, int radius, Actor checkActor)
    {
        baseList = activeMap.GetListOfTilesAroundPoint(centerTile, radius);
        returnList.Clear();
        foreach (MapTileData mtd in baseList)
        {
            if (InBounds(mtd.pos) && mtd.tileType != TileTypes.WALL && mtd.tileType != TileTypes.NOTHING)
            {
                if (!mtd.IsCollidable(checkActor))
                {
                    returnList.Add(mtd);
                }
            }

        }
        return returnList;
    }

    /* public static List<MapTileData> GetTilesAroundPoint(Vector2 centerTile, int radius)
    {
        return activeMap.GetTilesAroundPoint(centerTile, radius);
    } */

    public static bool CheckTileToTileLOS(Vector2 pos1, Vector2 pos2, Actor originatingActor, Map checkMap, bool treatForcefieldsAsBlocking = false)
    {
        bool debug = false;

        //Don't check for impossible draws
        if (!InBounds(pos1) || !InBounds(pos2))
        {
            return false;
        }

        float fGridDistBetweenPoints = GetGridDistance(pos1, pos2);

        //If we are a fighter and have a vision range, check that range
        var ft = originatingActor as Fighter;
        if (ft != null && fGridDistBetweenPoints > ft.myStats.GetCurStat(StatTypes.VISIONRANGE))
        {
            return false;
        }

        bool startSeesFinish = true;
        bool finishSeesStart = true;

        // New LENIENT method of checking tile-to-tile LOS.
        // Get normalized vector between the two points, from start to finish.
        // Start from center to center

        // Make sure our baseline comparison is centered
        pos1.x += 0.5f;
        pos1.y += 0.5f;
        pos2.x += 0.5f;
        pos2.y += 0.5f;

        Vector2 sum = pos2 - pos1;
        sum.Normalize();
        Vector2 crossProduct = Vector3.Cross(sum, new Vector3(0, 0, 1f));
        Vector2 posToCheck = pos2;

        if (debug) Debug.Log("Sum: " + sum + " Cross product: " + crossProduct + " Pos to check: " + posToCheck);

        if (!CustomAlgorithms.CheckLOSWithVectors(pos1, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
        {
            // Fire at adjacent point that is a half tile away by getting the cross product.
            posToCheck = new Vector2(pos2.x + (crossProduct.x * 0.5f), pos2.y + (crossProduct.y * 0.5f));
            if (InBounds(posToCheck) && !CustomAlgorithms.CheckLOSWithVectors(pos1, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
            {
                posToCheck = new Vector2(pos2.x - (crossProduct.x * 0.5f), pos2.y - (crossProduct.y * 0.5f));
                if (InBounds(posToCheck) && !CustomAlgorithms.CheckLOSWithVectors(pos1, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
                {
                    startSeesFinish = false;
                }
            }
        }

        if (startSeesFinish)
        {
            return true;
        }

        // Maybe end can see start?
        crossProduct *= -1;


        /* sum = pos1 - pos2;
        sum.Normalize();
        crossProduct = Vector3.Cross(sum, new Vector3(0, 0, 1f)); */

        posToCheck = pos1;

        if (InBounds(posToCheck) && !CustomAlgorithms.CheckLOSWithVectors(pos2, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
        {
            // Fire at adjacent point that is a half tile away by getting the cross product.
            posToCheck = new Vector2(pos1.x + (crossProduct.x * 0.5f), pos1.y + (crossProduct.y * 0.5f));
            if (InBounds(posToCheck) && !CustomAlgorithms.CheckLOSWithVectors(pos2, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
            {
                posToCheck = new Vector2(pos1.x + (crossProduct.x * 0.5f), pos1.y + (crossProduct.y * 0.5f));
                if (InBounds(posToCheck) && !CustomAlgorithms.CheckLOSWithVectors(pos2, posToCheck, checkMap, debug, treatForcefieldsAsBlocking))
                {
                    finishSeesStart = false;
                }
            }
        }

        if (finishSeesStart)
        {
            return true;
        }

        // Start can't see finish, finish can't see start, even with lenient lines firing off. Oh well!
        return false;

    }

    public static void DoWaitThenMoveActor(Actor act, float time, Vector2 pos)
    {
        singletonMMS.StartCoroutine(singletonMMS.WaitThenMoveActor(act, time, pos));
    }

    public IEnumerator WaitThenMoveActor(Actor act, float time, Vector2 pos)
    {
        yield return new WaitForSeconds(time);
        MoveActorAndChangeCamera(act, pos);
    }

    public static void MoveActorAndChangeCamera(Actor act, Vector2 pos)
    {
        act.myMovable.SetPosition(pos);
        act.SetPos(pos);
        MapMasterScript.singletonMMS.MoveAndProcessActor(act.GetPos(), pos, act);

        if (act.GetActorType() == ActorTypes.HERO)
        {
            CameraController.UpdateCameraPosition(pos, true);
            CameraController.UpdateLightPosition();
            MapMasterScript.singletonMMS.UpdateMapObjectData();
            // Update overlay, if any
            if (MinimapUIScript.GetOverlay())
            {
                MinimapUIScript.GenerateOverlay();
            }
        }
    }

    public static int GetGridDistance(Vector2 start, Vector2 finish, bool newMethod = false)
    {
        //return (int)Mathf.Floor(Vector2.Distance(start, finish));

        return CustomAlgorithms.GetGridDistance(start, finish);

        //Debug.Log("Distance of " + start.ToString() + " to " + finish.ToString() + " is " + dist);
    }
    

    // This checks if the HERO can see this actor.
    public static void CheckMapActorLOS(Actor act)
    {
        if (act == null || act == GameMasterScript.heroPCActor )
        {
            return;
        }



        // in RevealAll maps, hero can see  everything.
        if (activeMap.dungeonLevelData.revealAll && act.actorEnabled && act.myMovable != null)
        {
                    act.myMovable.SetInSightAndSnapEnable(true);

            return;
        }

        if (act.myMovable.GetShouldBeVisible())
        {

            act.myMovable.SetInSightAndSnapEnable(true);
        }
        if (!act.actorEnabled)
        {
            act.myMovable.SetInSightAndSnapEnable(false);
            return;
        }

        //if we are enabled, and a hero pet, we can be seen, and that's that.
        if (act.summoner == GameMasterScript.heroPCActor) // This should be faster than the CheckSummonRefs call
        {
            act.myMovable.SetInSightAndSnapEnable(true);
            return;
        }


        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            Fighter ft = act as Fighter;
            if (ft.CheckFlag(ActorFlags.TRACKED))
            {
                ft.myMovable.SetInSightAndSnapEnable(true);
                return;
            }
        }

        if (act.GetActorType() == ActorTypes.MONSTER &&
		   (act.GetPos().x > activeMap.columns - 1 || act.GetPos().y > activeMap.rows - 1))
        {
                // let's move it in bounds?
                MapTileData available = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 3, true, false, true, false);
                MapMasterScript.activeMap.AddActorToLocation(available.pos, act);
                act.SetPos(available.pos);
                act.SetCurPos(available.pos);
                act.myMovable.SetPosition(available.pos);
			return;
            }


        bool isTileVisible = false;
        try { isTileVisible = GameMasterScript.heroPCActor.visibleTilesArray[(int)act.GetPos().x, (int)act.GetPos().y]; }
        catch (Exception e)
        {
            Debug.Log(e + " " + act.actorRefName + " at " + act.GetPos() + " vs " + GameMasterScript.heroPCActor.visibleTilesArray.Length);
        }

        MapTileData mtd = activeMap.mapArray[(int)act.GetPos().x, (int)act.GetPos().y];



        Vector2 vActorPosition = act.GetPos();
        // Tile is not visible and not explored.
        if (!activeMap.exploredTiles[(int)vActorPosition.x, (int)vActorPosition.y])
        {
            if (!act.destroyed && act.GetObject().activeSelf)
            {
                act.myMovable.SetInSightAndFade(false);
            }
            return;
        }

        // OK, we've explored the tile.

        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE || 
            act.GetActorType() == ActorTypes.STAIRS)
        {
            act.myMovable.SetInSightAndSnapEnable(true);
            if (act.mySpriteRenderer.color.a == 0)
            {
                act.mySpriteRenderer.color = Color.white;
            }
            return; // Destructibles are basically static so just leave it alone.
        }


        if (!isTileVisible)
        {
            if (act.myMovable.inSight)
            {
                act.myMovable.SetInSightAndFade(false);
            }
        }
        else
        {
            act.myMovable.SetInSightAndFade(true);
            act.myMovable.SetTurnsSinceLastSeen(0);
        }


    }

    public void UpdateSpriteRenderersOnLoad()
    {
        activeMap.UpdateSpriteRenderersOnLoad();
        
        Color c = Color.white;
        c.a = 1.0f;
        //Debug.Log("Updating sprite renderers for map " + activeMap.GetName() + " " + activeMap.floor);
        foreach (Actor act in activeMap.actorsInMap)
        {
            if (act == GameMasterScript.heroPCActor) continue;

            //Debug.Log("Checking actor " + act.displayName + " at " + act.GetPos() + " " + act.currentPosition);
            //Movable mv = act.GetObject().GetComponent<Movable>();

            if (!InMaxBounds(act.GetPos()))
            {
                Debug.Log("Warning: " + act.actorRefName + " at " + act.GetPos() + " is out of bounds on floor " + activeMap.floor);
                continue;
            }

            if ((heroPCActor.visibleTilesArray[(int)act.GetPos().x, (int)act.GetPos().y]) || ((act.myMovable.remember) && (act.myMovable.rememberTurns == 0)))
            {
                if (act.myMovable == null || act.mySpriteRenderer == null)
                {
                    Debug.Log(act.actorRefName + " movable or sprite renderer is destroyed?");
                    continue;
                }
                act.myMovable.EnableRenderer(true);
                act.mySpriteRenderer.color = c;
            }
            else
            {
                act.myMovable.EnableRenderer(false);
            }
        }
    }

    public void CheckMapActorsLOS(bool useBufferedHeroPos = false)
    {
        // Can the player see each actor?
        Vector2 playerPos = GameMasterScript.heroPCActor.GetPos();
        if (useBufferedHeroPos)
        {
            playerPos.x = GameMasterScript.gmsSingleton.ReadTempFloatData("bufferx");
            playerPos.y = GameMasterScript.gmsSingleton.ReadTempFloatData("buffery");
        }

        int aCount = activeMap.actorsInMap.Count;
		int attempts = 0;
        for (int i = 0; i < aCount; i++)
        {
            Actor actToCheck = activeMap.actorsInMap[i];
            if (actToCheck.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = actToCheck as Destructible;
                if (dt.mapObjType == SpecialMapObject.BLOCKER)
                {
                    continue;
                }
            }
            Movable m = actToCheck.myMovable;
            if (m != null && (m.terrainTile || m.transparentStairs))
            {
                continue;
            }
            CheckMapActorLOS(activeMap.actorsInMap[i]);

            // Below: New code to see if we can eliminate problem with monsters being visible when they should not be.
			Monster mn = actToCheck as Monster;
            if (mn != null && mn.actorfaction != Faction.PLAYER)
            {

                if (activeMap.dungeonLevelData.revealAll && mn.actorEnabled)
                {
                    continue;
                }

                int distance = GetGridDistance(playerPos, mn.GetPos());

                if (distance > GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) && mn.actorfaction != Faction.PLAYER)
                {
                    mn.myMovable.SetInSightAndSnapEnable(false);
                    continue;
                }

                    // First conditional was not there before.

                    // Don't do the more expensive check method if the actor is further away. Save cpu.
                    if (distance <= 7 && CheckTileToTileLOS(heroPCActor.GetPos(), mn.GetPos(), heroPCActor, activeMap))
                    {
                        GameMasterScript.heroPCActor.visibleTilesArray[(int)mn.GetPos().x, (int)mn.GetPos().y] = true;
                    }

                    if (!GameMasterScript.heroPCActor.visibleTilesArray[(int)mn.GetPos().x, (int)mn.GetPos().y]) // && (!CheckTileToTileLOS(playerPos, mn.GetPos(), GameMasterScript.heroPCActor, MapMasterScript.activeMap)))
                    {
                        if (!GameMasterScript.heroPCActor.CheckTempRevealTile(mn.GetPos()))
                        {
                            mn.myMovable.SetInSightAndFade(false);
                        }
                    }
                    else
                    {
                        mn.myMovable.SetInSightAndFade(true);
                    }
                }
            
        }
    }

    public void ShowAllActors()
    {
        Vector2 playerPos = GameMasterScript.heroPCActor.GetPos();

        foreach (Actor act in activeMap.actorsInMap)
        {
            Movable move = act.GetObject().GetComponent<Movable>();

            bool draw = Input.GetKey(KeyCode.L);

            if (draw)
            {
                Debug.DrawLine(playerPos, act.GetPos(), Color.white, 1.0f);

            }
            act.myMovable.SetInSightAndSnapEnable(true);
        }
    }

    private bool TileWithinVisionRadius(MapTileData tile)
    {
        // Is the tile within the player's vision range? If it's already explored, then it always is

        Vector2 tilePos = tile.pos;

        if (activeMap.exploredTiles[(int)tile.pos.x, (int)tile.pos.y])
        {
            
            return true;
        }

        bool visible = true;
        Vector2 playerPos = GameMasterScript.heroPCActor.GetPos();
        int visionRange = (int)GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.CUR);

        int minX = (int)playerPos.x - (visionRange / 2);
        int maxX = (int)playerPos.x + (visionRange / 2);
        int minY = (int)playerPos.y - (visionRange / 2);
        int maxY = (int)playerPos.y + (visionRange / 2);

        if ((tilePos.x < minX) || (tilePos.x > maxX) || (tilePos.y < minY) || (tilePos.y > maxY))
        {
            visible = false;
        }

        
        return visible;

    }

    private static bool WithinRenderBounds(Vector2 pos)
    {
        bool withinBounds = true;
        Vector3 playerPos = GameMasterScript.heroPCActor.GetPos();

        int minX = (int)playerPos.x - renderColumns / 2;
        int minY = (int)playerPos.y - renderRows / 2;
        int maxX = (int)playerPos.x + renderColumns / 2;
        int maxY = (int)playerPos.y + renderRows / 2;

        if ((pos.x < minX + 1) || (pos.x > maxX - 1) || (pos.y < minY + 1) || (pos.y > maxY - 1)) // outside bounds 
        {
            withinBounds = false;
        }
        return withinBounds;

    }

    static void ClearArray(bool[,] cArray)
    {
        for (int x = 0; x < activeMap.columns; x++)
        {
            for (int y = 0; y < activeMap.rows; y++) // was cArray.GetLength(1)
            {
                cArray[x, y] = false;
            }
        }

    }

    public static void CheckFighterLOSToTiles(Fighter act, bool useSpecialBufferedPosition = false)
    {
        if (act == null)
        {
            Debug.Log("Can't check null actor's LOS");
            return;
        }
        /* if (act.myStats == null)
        {
            Debug.Log(act.actorRefName + " has no stats?");
            return;
        } */

        act.ClearVisibleTiles();
        int vRange = (int)act.myStats.GetCurStat(StatTypes.VISIONRANGE) / 2;
        Vector2 cPos = act.GetPos();

        if (!MapMasterScript.InBounds(cPos))
        {
            Debug.Log("WARNING: " + cPos + " on " + MapMasterScript.activeMap.floor + " is out of bounds, yet " + act.actorRefName + " " + act.actorUniqueID + " is supposed to be there?");
        }

        // Maybe we want to check LOS from a position other than the hero's current LOS, for various reasons
        // Such as the hero is being pulled by something but hasn't reached destination square yet.
        if (useSpecialBufferedPosition)
        {
            cPos.x = GameMasterScript.gmsSingleton.ReadTempFloatData("bufferx");
            cPos.y = GameMasterScript.gmsSingleton.ReadTempFloatData("buffery");
        }

        minXLOS = cPos.x - vRange;
        minYLOS = cPos.y - vRange;
        maxXLOS = cPos.x + vRange;
        maxYLOS = cPos.y + vRange;

        // These are absolute bounds.
        minXLOS = Mathf.Clamp(minXLOS, 0, activeMap.columns - 1);
        minYLOS = Mathf.Clamp(minYLOS, 0, activeMap.rows - 1);
        maxXLOS = Mathf.Clamp(maxXLOS, 0, activeMap.columns - 1);
        maxYLOS = Mathf.Clamp(maxYLOS, 0, activeMap.rows - 1);

        iMinXLOS = (int)minXLOS;
        iMinYLOS = (int)minYLOS;
        iMaxXLOS = (int)maxXLOS;
        iMaxYLOS = (int)maxYLOS;

        // Sanity check. This should never happen, but...
        if (GameMasterScript.heroPCActor.visibleTilesArray.GetLength(0) <= iMaxXLOS || GameMasterScript.heroPCActor.visibleTilesArray.GetLength(1) <= iMaxYLOS)
        {

            //Debug.Log("WARNING: Player's visibility array was out-of-whack, recreating it.");
            GameMasterScript.heroPCActor.visibleTilesArray = new bool[activeMap.columns, activeMap.rows];

        }

        // Check edges ONLY first

        checkPosLOS = Vector2.zero;
        iCheckPosLOS = new Point(0, 0);

        // Write a spiral check here.

        // fpoints declare was here

        ClearArray(tChecked);
        ClearArray(traversed);
        SimpleDir traverse = SimpleDir.RIGHT;
        int checkX = iMinXLOS;
        int checkY = iMinYLOS;

        // New method of check.

        checkPosLOS.x = checkX;
        checkPosLOS.y = checkY;
        iCheckPosLOS.x = checkX;
        iCheckPosLOS.y = checkY;

        int maxTilesTraversed = ((iMaxXLOS - iMinXLOS) + 1) * ((iMaxYLOS - iMinYLOS) + 1);
        //bool tileIsValid;

        Vector2 cv2 = Vector2.zero;

        while (iMinXLOS <= iMaxXLOS && iMinYLOS <= iMaxYLOS)
        {
            for (int x = iMinXLOS; x <= iMaxXLOS; x++)
            {
                cv2.x = x;
                cv2.y = iMinYLOS;
                if (CustomAlgorithms.CheckBresenhamsLOS(cPos, cv2, activeMap))
                {
                    act.visibleTilesArray[x, iMinYLOS] = true;
                }

                cv2.x = x;
                cv2.y = iMaxYLOS;

                if (!act.visibleTilesArray[x, iMaxYLOS] && CustomAlgorithms.CheckBresenhamsLOS(cPos, cv2, activeMap))
                {
                    act.visibleTilesArray[x, iMaxYLOS] = true;
                }
            }

            for (int y = iMinYLOS + 1; y < iMaxYLOS; y++)
            {

                cv2.x = iMinXLOS;
                cv2.y = y;

                if (CustomAlgorithms.CheckBresenhamsLOS(cPos, cv2, activeMap))
                {
                    act.visibleTilesArray[iMinXLOS, y] = true;
                }

                cv2.x = iMaxXLOS;
                cv2.y = y;

                if (!act.visibleTilesArray[iMaxXLOS, y] && CustomAlgorithms.CheckBresenhamsLOS(cPos, cv2, activeMap))
                {
                    act.visibleTilesArray[iMaxXLOS, y] = true;
                }
            }

            iMinXLOS++;
            iMinYLOS++;
            iMaxXLOS--;
            iMaxYLOS--;
        }

        if ((int)cPos.x >= 0 && (int)cPos.y >= 0 && (int)cPos.x <= iMaxXLOS && (int)cPos.y <= iMaxYLOS)
        {
            act.visibleTilesArray[(int)cPos.x, (int)cPos.y] = true; // can always see own tile.        
        }

        int xMax = act.visibleTilesArray.GetLength(0);
        int yMax = act.visibleTilesArray.GetLength(1);
        foreach (Vector2 v2 in GameMasterScript.heroPCActor.tempRevealTiles)
        {
            if (!MapMasterScript.InBounds(v2))
            {

                continue;
            }
            if (v2.x < xMax && v2.y < yMax)
            {
                act.visibleTilesArray[(int)v2.x, (int)v2.y] = true;
            }
        }
        foreach (Vector2 v2 in GameMasterScript.heroPCActor.tempPetRevealTiles)
        {
            if (!MapMasterScript.InBounds(v2))
            {

                continue;
            }
            if (v2.x < xMax && v2.y < yMax)
            {
                act.visibleTilesArray[(int)v2.x, (int)v2.y] = true;
            }
        }

        return;
    }

    public void CheckAllTileVisibility(bool updateFromBufferdHeroPOS = false)
    {
        CheckFighterLOSToTiles(GameMasterScript.heroPCActor, updateFromBufferdHeroPOS);

        // inherit pet vision for exploration purposes
        GameMasterScript.heroPCActor.InheritVisionFromPets();

        Vector2 playerPos = GameMasterScript.heroPCActor.GetPos();
        if (updateFromBufferdHeroPOS)
        {
            playerPos.x = GameMasterScript.gmsSingleton.ReadTempFloatData("bufferx");
            playerPos.y = GameMasterScript.gmsSingleton.ReadTempFloatData("buffery");
        }

        float minX = playerPos.x - (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) / 3);
        float minY = playerPos.y - (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) / 3);
        float maxX = playerPos.x + (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) / 3);
        float maxY = playerPos.y + (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.VISIONRANGE) / 3);

        minX = Mathf.Clamp(minX, 0, activeMap.columns - 1);
        minY = Mathf.Clamp(minY, 0, activeMap.rows - 1);
        maxX = Mathf.Clamp(maxX, 0, activeMap.columns - 1);
        maxY = Mathf.Clamp(maxY, 0, activeMap.rows - 1);

        int iMinX = (int)minX;
        int iMinY = (int)minY;
        int iMaxX = (int)maxX;
        int iMaxY = (int)maxY;

        Vector2 temp = new Vector2(0, 0);

        //MapTileData tMTD;

        for (int x = iMinX; x <= iMaxX; x++)
        {
            for (int y = iMinY; y <= iMaxY; y++)
            {
                temp.x = x;
                temp.y = y;
                //tMTD = GetTile(temp);
                if (GameMasterScript.heroPCActor.visibleTilesArray[x, y])
                {
                    activeMap.exploredTiles[x, y] = true;
                }
            }
        }

        //fogOfWar.UpdateColors();

        if (!activeMap.dungeonLevelData.revealAll)
        {
            try { UpdateFOWOverlay(false); }     //#questionable_try_block
            catch
            {
                Debug.Log("Error updating FOW overlay in turn. " + activeMap.floor);
            }
        }

        try { MinimapUIScript.UpdateMinimapColors(); }
        catch (Exception e)
        {
            Debug.Log("Error updating minimap colors: " + e);
        }

    }

    public MapTileData GetMapTileData(int x, int y)
    {
        return activeMap.mapArray[x, y];
    }


    public int RandomDir()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    public void MoveAndProcessActorNoPush(Vector2 oldPos, Vector3 newPos, Actor act)
    {
        if (act == null)
        {
            Debug.Log("Trying to move and no push a null actor.");
            return;
        }
        act.SetCurPos(newPos);
        MoveActor(oldPos, newPos, act);
    }

    IEnumerator WaitThenPlayAnimation(Vector2 pos, string refName)
    {
        yield return new WaitForSeconds(GameMasterScript.gmsSingleton.playerMoveSpeed);
        GameObject water = GameMasterScript.TDInstantiate(refName);
        CombatManagerScript.TryPlayEffectSFX(water, pos, null);
        water.transform.position = pos;
    }

    // This moves anchors of an actor
    public void ProcessActorAnchorMove(Vector2 oldPos, Vector2 newPos, Actor ownerOfAnchor)
    {
        if (ownerOfAnchor.GetActorType() == ActorTypes.MONSTER || ownerOfAnchor.GetActorType() == ActorTypes.HERO)
        {
            Fighter ft = ownerOfAnchor as Fighter;

            List<Actor> allAnchors = new List<Actor>();

            if (ft.summonedActors != null)
            {
                foreach (Actor sAct in ft.summonedActors)
                {
                    allAnchors.Add(sAct);
                }
            }
            if (ft.anchoredActorData != null)
            {
                foreach (AnchoredActorData aad in ft.anchoredActorData)
                {
                    if (aad.actorRef != null)
                    {
                        allAnchors.Add(aad.actorRef);
                    }
                }
            }

            if (allAnchors.Count > 0)
            {
                for (int x = 0; x < allAnchors.Count; x++)
                {
                    // The actor that moved has an anchored actor list: allAnchors. Let's find ANY ANCHORS with (moved actor) as parent.
                    Actor sAct = allAnchors[x];
                    if (sAct.anchor == ownerOfAnchor && sAct.anchorRange == 0 && !sAct.destroyed && !sAct.movedByAnchorThisTurn)
                    {
                        sAct.movedByAnchorThisTurn = true;
                        //Debug.Log("Move " + sAct.actorRefName + " at " + sAct.GetPos());
                        MovementResults mr = PushActorBasedOnAnchorMovement(sAct, ownerOfAnchor, oldPos, newPos);

                        if (!mr.moveSuccessful)
                        {
                            MapTileData alternativeMTD = MapMasterScript.GetTile(ownerOfAnchor.previousPosition);
                            if (alternativeMTD.IsCollidable(sAct))
                            {
                                Debug.Log("Move anchor " + sAct.actorUniqueID + " " + sAct.actorRefName + " failed.");
                                continue;
                            }
                            MapMasterScript.activeMap.RemoveActorFromLocation(sAct.GetPos(), sAct);
                            MapMasterScript.activeMap.RemoveActorFromLocation(sAct.previousPosition, sAct);
                            MapMasterScript.activeMap.AddActorToLocation(alternativeMTD.pos, sAct);
                            if (sAct.myMovable != null)
                            {
                                sAct.myMovable.AnimateSetPosition(alternativeMTD.pos, GameMasterScript.gmsSingleton.playerMoveSpeed * 0.75f, false, 0f, 0f, MovementTypes.LERP);
                            }
                            sAct.SetCurPos(alternativeMTD.pos);
                            //Debug.Log(sAct.actorRefName + " redirected to " + alternativeMTD.pos + " prev pos of its owner.");

                        }
                    }
                } // End of FOR loop
            }
        }
    }

    public bool MoveAndProcessActor(Vector2 oldPos, Vector3 newPos, Actor act, bool ignoreStackedActors = false)
    {
        act.SetCurPos(newPos);

        if (!MoveActor(oldPos, newPos, act)) // Did the move fail?
        {
            if (Debug.isDebugBuild) Debug.Log("Move of " + act.actorRefName + " from " + oldPos + " to " + newPos + " failed?");
            return false;
        }
        bool movedAnotherActor = false;
        //UpdateMapObjectData();  EXPERIMENTAL - wait for turn end cleanup to do this

        if (act.GetActorType() == ActorTypes.HERO)
        {
            if (!GetTile(oldPos).CheckTag(LocationTags.WATER) && GetTile(newPos).CheckTag(LocationTags.WATER))
            {
                if (!GetTile(newPos).HasPlanks())
                    StartCoroutine(WaitThenPlayAnimation(newPos, "EnterWaterSplash"));
            }
            else if (!GetTile(oldPos).CheckTag(LocationTags.ISLANDSWATER) && GetTile(newPos).CheckTag(LocationTags.ISLANDSWATER))
            {
                if (!GetTile(newPos).HasPlanks())
                    StartCoroutine(WaitThenPlayAnimation(newPos, "EnterWaterSplash"));
            }
            else if (GetTile(oldPos).CheckTag(LocationTags.WATER) && GetTile(newPos).CheckTag(LocationTags.WATER))
            {
                if (!GetTile(newPos).HasPlanks())
                    StartCoroutine(WaitThenPlayAnimation(newPos, "WalkWaterSplash"));
            }
            else if (GetTile(oldPos).CheckTag(LocationTags.ISLANDSWATER) && GetTile(newPos).CheckTag(LocationTags.ISLANDSWATER))
            {
                if (!GetTile(newPos).HasPlanks())
                    StartCoroutine(WaitThenPlayAnimation(newPos, "WalkWaterSplash"));
            }
            else if (!GetTile(oldPos).CheckTag(LocationTags.LAVA) && GetTile(newPos).CheckTag(LocationTags.LAVA))
            {
                StartCoroutine(WaitThenPlayAnimation(newPos, "EnterLavaSplash"));
            }
            else if (GetTile(oldPos).CheckTag(LocationTags.LAVA) && GetTile(newPos).CheckTag(LocationTags.LAVA))
            {
                StartCoroutine(WaitThenPlayAnimation(newPos, "WalkLavaSplash"));
            }
            else if (!GetTile(oldPos).CheckAnyMud() && GetTile(newPos).CheckAnyMud())
            {
                StartCoroutine(WaitThenPlayAnimation(newPos, "EnterMudSplash"));
            }
            else if (GetTile(oldPos).CheckAnyMud() && GetTile(newPos).CheckAnyMud())
            {
                StartCoroutine(WaitThenPlayAnimation(newPos, "WalkMudSplash"));
            }
        }

        ProcessActorAnchorMove(oldPos, newPos, act);

        // 11/23/2017 - Why was I allowing actors to push other actors...? Why would this happen?
        // Actors should check, on THEIR move, whether a tile is valid. Period.

        /* List<Actor> possibleCollidableActors = GetTile(newPos).GetAllActors();
        if (possibleCollidableActors.Count > 1)
        {
            for (int i = 0; i < possibleCollidableActors.Count; i++)
            {
                Actor localAct = possibleCollidableActors[i];
                if (act == localAct) continue;

                if (((localAct.GetActorType() == ActorTypes.HERO) && (act.playerCollidable)) || ((localAct.GetActorType() == ActorTypes.MONSTER) && (act.monsterCollidable)))
                {
                    // This is something the moved actor can collide with.
                    List<MapTileData> openPositions = MapMasterScript.GetNonCollidableTilesAroundPoint(newPos, 1, localAct);
                    if (openPositions.Count == 0)
                    {
                        Debug.Log(localAct.displayName + " pushed into a corner.");
                    }
                    else
                    {
                        Vector2 movedPos = openPositions[0].pos;
                        MoveAndProcessActor(newPos, movedPos, localAct);
                        if (localAct.myMovable != null)
                        {
                            localAct.myMovable.AnimateSetPosition(movedPos, GameMasterScript.gmsSingleton.playerMoveSpeed, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
                        }
                        movedAnotherActor = true;
                    }

                }
            }
        } */

        if (!ignoreStackedActors)
        {
            if (act != null)
            {
            activeMap.CheckForStackedActors(newPos, act, true); // Make sure actors aren't stacking...
            }
        }


        return movedAnotherActor;
    }

    public MovementResults PushActorBasedOnAnchorMovement(Actor sAct, Actor anchor, Vector2 oldPos, Vector2 newPos)
    {
        MovementResults mr = new MovementResults();
        mr.moveSuccessful = false;

        List<Actor> collidable = new List<Actor>();

        sAct.pushedThisTurn = true;
        Vector2 anchorPos = sAct.GetPos();
        int xDiff = (int)(newPos.x - oldPos.x);
        int yDiff = (int)(newPos.y - oldPos.y);

        Vector2 newPos2 = new Vector2(anchorPos.x + xDiff, anchorPos.y + yDiff);
        if (!InBounds(newPos2)) return mr;

        Directions dirOfMove = CombatManagerScript.GetDirection(oldPos, newPos2);
        Vector2 moveAdd = MapMasterScript.xDirections[(int)dirOfMove];

        if (sAct.GetActorType() == ActorTypes.MONSTER)
        {
            if (sAct.dungeonFloor != activeMap.floor) return mr;
            if (GetTile(newPos2).IsCollidable(sAct))
            {
                // The anchor being pulled is a monster, and the destination tile is collidable.
                //pool_vectorList = CustomAlgorithms.GetPointsOnLine(sAct.GetPos(), act.GetPos()).ToList();

                CustomAlgorithms.GetPointsOnLineNoGarbage(sAct.GetPos(), anchor.GetPos());
                Vector2 bestTile = sAct.GetPos();
                float shortest = 99f;
                MapTileData mtd = null;

                for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
                {
                    if (CustomAlgorithms.pointsOnLine[i] == sAct.GetPos())
                    {
                        continue;
                    }

                    mtd = GetTile(CustomAlgorithms.pointsOnLine[i]);
                    if (!mtd.IsCollidable(sAct))
                    {
                        float dist = Vector2.Distance(sAct.GetPos(), mtd.pos);
                        if (dist < shortest)
                        {
                            bestTile = mtd.pos;
                            shortest = dist;
                        }

                    }
                }

                newPos2 = bestTile;

                if (bestTile == sAct.GetPos())
                {
                    // Really nothing better? Let's try something else.
                    pool_tileList = GetNonCollidableTilesAroundPoint(anchor.GetPos(), 1, sAct);
                    shortest = 99f;
                    if (pool_tileList.Count > 0)
                    {
                        foreach (MapTileData pMTD in pool_tileList)
                        {
                            float dist = Vector2.Distance(sAct.GetPos(), pMTD.pos);
                            if (dist < shortest)
                            {
                                shortest = dist;
                                newPos2 = pMTD.pos;
                            }
                        }
                    }
                }
            }
        }
        else if (sAct.monsterCollidable) // Below is for moving destructibles that are monster-collidable only.
        {
            collidable = GetTile(newPos2).GetAllActors();
            if (collidable.Count > 0)
            {
                for (int c = 0; c < collidable.Count; c++)
                {
                    Actor col = collidable[c];

                    if ((col == anchor) || (col.actorRefName == sAct.actorRefName)) continue;
                    if (col.GetActorType() == ActorTypes.MONSTER)
                    {
                        // The anchored collidable needs to push the monster   
                        Vector2 monNewPos = col.GetPos() + moveAdd;
                        if (!MapMasterScript.InBounds(monNewPos))
                        {
                            continue;
                        }
                        MapTileData mtd = GetTile(monNewPos);
                        if (!mtd.IsCollidable(col))
                        {
                            MoveAndProcessActor(col.GetPos(), monNewPos, col);
                            if (col.myMovable != null)
                            {
                                col.myMovable.AnimateSetPosition(monNewPos, GameMasterScript.gmsSingleton.playerMoveSpeed * 0.75f, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
                            }
                            mr.movedAnotherActor = true;
                        }
                        else
                        {
                            List<MapTileData> openPositions = MapMasterScript.GetNonCollidableTilesAroundPoint(col.GetPos(), 1, col);
                            if (openPositions.Count == 0)
                            {
                                Debug.Log(col.displayName + " pushed into a corner.");
                            }
                            else
                            {
                                monNewPos = openPositions[0].pos;
                                MoveAndProcessActor(col.GetPos(), monNewPos, col);
                                if (col.myMovable != null)
                                {
                                    col.myMovable.AnimateSetPosition(monNewPos, GameMasterScript.gmsSingleton.playerMoveSpeed * 0.75f, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
                                }
                                mr.movedAnotherActor = true;

                            }
                        }
                    }
                }
            }
        }

        if (newPos2.x != newPos.x || newPos2.y != newPos.y) // Prevent stack overflow from actors swapping forever
        {
            MoveAndProcessActor(anchorPos, newPos2, sAct);
            if (sAct.myMovable != null)
            {
                sAct.myMovable.AnimateSetPosition(newPos2, GameMasterScript.gmsSingleton.playerMoveSpeed * 0.75f, false, 0.0f, 0.0f, MovementTypes.SMOOTH);
            }
            sAct.pushedThisTurn = true;
        }

        sAct.SetCurPos(newPos2);

        mr.moveSuccessful = true;
        mr.newLocation = newPos2;
        return mr;
    }

    public static bool CheckCollision(Vector2 location, Actor checkActor)
    {
        return activeMap.CheckCollision(location, checkActor);
    }

    public static bool CheckUnbreakableCollision(Vector2 location, Actor checkActor)
    {
        return activeMap.CheckUnbreakableCollision(location, checkActor);
    }

    public static Actor GetTargetableAtLocation(Vector3 location)
    {
        return activeMap.GetTargetableAtLocation(location);
    }

    public static List<Actor> GetAllTargetableInV2Tiles(List<Vector2> tileList)
    {
        List<MapTileData> mtdList = new List<MapTileData>();

        for (int i = 0; i < tileList.Count; i++)
        {
            mtdList.Add(GetTile(tileList[i]));
        }

        List<Actor> returnActors = new List<Actor>();

        for (int i = 0; i < mtdList.Count; i++)
        {
            MapTileData mtd = mtdList[i];
            foreach (Actor act in mtd.GetAllTargetable())
            {
                if (!returnActors.Contains(act))
                {
                    returnActors.Add(act);
                }
            }
        }

        return returnActors;
    }


    public static List<Actor> GetAllTargetablePlusDestructibles(List<MapTileData> tileList)
    {
        List<Actor> returnActors = new List<Actor>();

        for (int i = 0; i < tileList.Count; i++)
        {
            MapTileData mtd = tileList[i];
            foreach (Actor act in mtd.GetAllTargetablePlusDestructibles())
            {
                if (!returnActors.Contains(act))
                {
                    returnActors.Add(act);
                }
            }
        }

        return returnActors;
    }

    public static List<Actor> GetAllTargetableInTiles(List<MapTileData> tileList)
    {
        List<Actor> returnActors = new List<Actor>();

        for (int i = 0; i < tileList.Count; i++)
        {
            MapTileData mtd = tileList[i];
            foreach (Actor act in mtd.GetAllTargetable())
            {
                if (!returnActors.Contains(act))
                {
                    returnActors.Add(act);
                }
            }
        }

        return returnActors;
    }


    public static bool InBounds(Vector2 check)
    {
        if (!mapLoaded) return false;
        if (check.x <= 0 || check.x >= activeMap.columns - 1 || check.y <= 0 || check.y >= activeMap.rows - 1)
        {
            return false;
        }
        return true;
    }

    public static bool InMaxBounds(Vector2 check)
    {
        if ((check.x < 0) || (check.x >= activeMap.columns) || (check.y < 0) || (check.y >= activeMap.rows))
        {
            return false;
        }
        return true;
    }

    public static List<Actor> GetAllTargetableAtLocation(Vector3 location)
    {
        return activeMap.GetAllTargetableAtLocation(location);
    }
    //Shep: Function never called
    /*
    public DoorData CheckDoor(Vector3 location)
    {
        return activeMap.CheckDoor(location);
    }
    */

    public static MapTileData GetTile(Vector2 location)
    {
        return activeMap.GetTile(location);
    }

    public bool MoveActor(Vector3 oldLoc, Vector3 newLoc, Actor act)
    {
        return activeMap.MoveActor(oldLoc, newLoc, act);
    }

    public void RemoveActorFromTile(Vector3 location, Actor act)
    {
        activeMap.RemoveActorFromLocation(location, act);
    }

    public static int GetAreaID(Vector2 loc)
    {
        return activeMap.GetAreaID(loc);
    }

    public bool AddActorToLocation(Vector3 location, Actor act)
    {
        return activeMap.AddActorToLocation(location, act);
    }

    public List<Item> GetItemsInTile(Vector3 location)
    {
        return activeMap.GetItemsInTile(location);
    }

    public Stairs GetStairsInTile(Vector3 location)
    {
        return activeMap.GetStairsInTile(location);
    }


    /* public void CheckPowerups(Fighter go, Vector3 location)
    {
        activeMap.CheckPowerups(go, location);
    } */

    public static void ReleaseAllTextures()
    {
#if UNITY_SWITCH
        singletonMMS.mapTileMesh.DestroyTexturesAndCleanup();
        singletonMMS.mapGridMesh.DestroyTexturesAndCleanup();
        singletonMMS.fogOfWarTMG.DestroyTexturesAndCleanup();
        MinimapUIScript.DestroyTexturesAndCleanup();
#endif
    }
    public void OnDestroy()
    {
        if (fogOfWarTexture != null)
        {
            Destroy(fogOfWarTexture);
        }
        fowColorArray = null;
    }
    public void UpdateFOWOverlay(bool newMap)
    {
        if (newMap)
        {
            // Create an all-new texture for the new map.
            if (fogOfWarTexture != null)
            {
                Destroy(fogOfWarTexture);
            }
#if !UNITY_SWITCH
            fogOfWarTexture = new Texture2D(GetColumns(), GetRows(), TextureFormat.RGBA32, false);
            fowColorArray = new Color[GetColumns() * GetRows()];
#else
            fogOfWarTexture = new Texture2D(GetColumns(), GetRows(), TextureFormat.RGBAHalf, false);
            fowColorArray = fogOfWarTexture.GetPixels(); //  new Color[GetColumns() * GetRows()];			
#endif
		}

        if (fogOfWarTexture == null) return;

        for (int i = 0; i < fowColorArray.Length; i++)
        {
            fowColorArray[i].r = 0;
            fowColorArray[i].g = 0;
            fowColorArray[i].b = 0;
            fowColorArray[i].a = 1;
        }

        int rows = GetRows();
        int columns = GetColumns();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                bool explored = activeMap.exploredTiles[x, y];
                bool visible = GameMasterScript.heroPCActor.visibleTilesArray[x, y];

                int index = y * columns + x;

                if (!explored)
                {
#if !UNITY_SWITCH
                    fowColorArray[index].r = 0;
                    fowColorArray[index].g = 0;
                    fowColorArray[index].b = 0;
                    fowColorArray[index].a = 1;
#endif
                }
                else if (explored && !visible)
                {
                    fowColorArray[index] = halfTransparent;
                }
                else if (visible)
                {
                    fowColorArray[index] = fullTransparent;
                }
            }
        }

        fogOfWarTexture.SetPixels(fowColorArray);
        fogOfWarTexture.filterMode = FilterMode.Bilinear;
        fogOfWarTexture.wrapMode = TextureWrapMode.Clamp;
        fogOfWarTexture.Apply();
        fogOfWarTMG.ApplyTexture(fogOfWarTexture);
    }
    

    public static void CheckForMapStartConversationsAndEvents(Map previousMap)
    {
        GameMasterScript.heroPCActor.ValidateInvincibleFrog();
        GameMasterScript.heroPCActor.CheckForAndSetJobMasteryFlag();
        GameMasterScript.theDungeonActor.displayName = activeMap.GetName();

        GameMasterScript.heroPCActor.OnMapsChanged(activeMap);

        DLCManager.OnMapChangeOrLoad();

        if (activeMap.IsJobTrialFloor())
        {
            JobTrialScript.VerifyJobTrialIsSetup();
        }

        if (activeMap.dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            DecorativeScreenEffectManager.EnableFallingStuffInOutsideArea();
        }
        else
        {
            DecorativeScreenEffectManager.DisableFallingStuffInOutsideArea();
        }

        TryRunScriptOnMapEntry();   

        if (activeMap.floor == CAMPFIRE_FLOOR)
        {
            GameEventsAndTriggers.CleanupCampfireOnEntry();
        }

        if (activeMap.floor == 102) // Frog Bog 
        {
            if (activeMap.FindActor("npc_pettrainer") == null && singletonMMS.townMap2.FindActor("npc_pettrainer") == null && !SharaModeStuff.IsSharaModeActive())
            {
                GameEventsAndTriggers.AddPetTrainerToFrogBog();
            }
        }

        if (activeMap.floor != JOB_TRIAL_FLOOR && activeMap.floor != JOB_TRIAL_FLOOR2 && JobTrialScript.IsJobTrialActive())
        {
            JobTrialScript.FailedJobTrial();
        }

        if (activeMap.floor == JOB_TRIAL_FLOOR && JobTrialScript.IsJobTrialActive())
        {
            if (GameMasterScript.heroPCActor.jobTrial.trialTierLevel == 2)
            {
                // must have cleared first floor AND have the relic
                JobTrialScript.CheckForTrialTier3Clear();
            }
        }

        if (activeMap.floor == SPECIALFLOOR_DIMENSIONAL_RIFT)
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.FoundDimRift();
        }

        if (activeMap.floor == PRE_BOSS3_WALKTOBOSS_FLOOR && !activeMap.IsItemWorld())
        {
            Cutscenes.Preboss3WalkWithShara();
            return;
        }
        if (activeMap.floor == BOSS3_MAP_FLOOR && !activeMap.IsItemWorld())
        {
            if (SharaModeStuff.IsSharaModeActive() && ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) < 2)
            {
                SharaModeStuff.HideStairsInBoss3Map();
            }

            if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) < 1)
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
                {
                    DLCCutscenes.SharaBoss3Intro();
                }
                else
                {
                    Cutscenes.BeginBoss3DialogueWithShara();
                }
                return;
            }
        }

        GameEventsAndTriggers.CheckForFinalBossFloorEvents();

        if (GameMasterScript.gmsSingleton.ReadTempGameData("nk_boss_arena") == 1)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("nk_boss_arena", 0);
            Vector2 playerPos = new Vector2(GameMasterScript.gmsSingleton.ReadTempGameData("nk_playersquare_x"), GameMasterScript.gmsSingleton.ReadTempGameData("nk_playersquare_y"));
            Vector2 bossPos = new Vector2(GameMasterScript.gmsSingleton.ReadTempGameData("nk_bosssquare_x"), GameMasterScript.gmsSingleton.ReadTempGameData("nk_bosssquare_y"));
            MapTileData playerSquare = activeMap.GetTile(playerPos);
            MapTileData bossSquare = activeMap.GetTile(bossPos);

            ItemDreamFunctions.MovePlayerAndNKToArena(playerSquare, bossSquare);
        }

        if (activeMap.floor == MapMasterScript.BRANCH_PASSAGE_POSTBOSS1 && PlayerAdvice.DreamcasterAvailableButUnused() && GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.SHARA)
        {
            if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_firstbranch_dreamcaster") && !GameEventsAndTriggers.ShouldCutscenesBeSkipped())
            {
                Conversation tutRef = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_firstbranch_dreamcaster");
                UIManagerScript.StartConversation(tutRef, DialogType.STANDARD, null);
            }
        }


        if (activeMap.floor == BOSS2_MAP_FLOOR && !activeMap.IsItemWorld())
        {
            GameEventsAndTriggers.CheckForBoss2Clear();
        }

        TutorialManagerScript.CheckForPetCommandTutorialOnMapChange();


        if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_itemworld") && PlayerOptions.tutorialTips && activeMap.IsItemWorld())
        {
            Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_itemworld");
            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
        }

        GameEventsAndTriggers.CheckForItemDreamEventsOnMapChange();

        bool anyTutorial = false;

        if (GameMasterScript.gmsSingleton.ReadTempGameData("enteringmysterydungeon") == 1) // must be start of the dungeon
        {
            UIManagerScript.StartConversationByRef("mysterydungeon_intro", DialogType.STANDARD, null);
            if (ProgressTracker.CheckProgress(TDProgress.WANDERER_JOURNEY, ProgressLocations.META) == 1)
            {
                ProgressTracker.SetProgress(TDProgress.WANDERER_JOURNEY, ProgressLocations.META, 2); // signifies we have begun our first journey
            }
            MysteryDungeonManager.SpawnStartingItemsNearPlayer();
            GameMasterScript.gmsSingleton.SetTempGameData("enteringmysterydungeon", 0);
            anyTutorial = true;
        }

        if (!GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(activeMap.mapAreaID))
        {
            anyTutorial = CheckForNewMapExploredEvents(anyTutorial);
        }

        if (activeMap.floor == GUARDIAN_RUINS_ENTRY_FLOOR && !activeMap.IsItemWorld())
        {
            RobotDragonStuff.CheckForRobotDungeonUnlockInGuardianRuins();
        }

        if (activeMap.floor == JELLY_GROTTO && !activeMap.IsItemWorld())
        {
            SlimeDragonStuff.CheckForSlimeDungeonUnlockInJellyGrotto();
        }

        if (activeMap.floor == BOSS1_MAP_FLOOR && !activeMap.IsItemWorld()) // First boss - bandits
        {
            if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) < 1)
            {
                Cutscenes.TryBoss1IntroCutscene();
            }
            if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) < 3) // not defeated yet
            {
                // ensure stairs don't appear.
                foreach(Stairs st in activeMap.mapStairs)
                {
                    if (!st.stairsUp)
                    {
                        st.DisableActor();
                        if (st.GetObject() != null) st.myMovable.ForceFadeOut();
                    }
                }
            }
            if (SharaModeStuff.CheckForBoss1Clear())
            {
                anyTutorial = true;
            }
        }
        else if (activeMap.floor == BOSS2_MAP_FLOOR && ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) < 2 && !activeMap.IsItemWorld())
        {
            if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) < 1)
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
                {
                    DLCCutscenes.SharaBoss2_FightIntro();
                    return;
                }
                else
                {
                    Cutscenes.Boss2_FightIntro();
                }
            }
            else
            {
                GameMasterScript.SetAnimationPlaying(true);
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(GameMasterScript.FindConversation("second_boss_postintro"), DialogType.KEYSTORY, null, 1.25f));
            }

        }
        else if (activeMap.floor == TOWN_MAP_FLOOR && !activeMap.IsItemWorld())
        {
            GameEventsAndTriggers.CheckForTown1MapEvents();
        }

        else if (activeMap.floor == TOWN2_MAP_FLOOR && !activeMap.IsItemWorld())
        {
            GameEventsAndTriggers.CheckForTown2MapEvents();
        }
        else if (activeMap.floor == MapMasterScript.BEASTLAKE_SIDEAREA && GameMasterScript.heroPCActor.ReadActorData("beastlakequest") == 1) // Beastlake park
        {
            Monster bandit = null;
            foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.monFamily == "bandits")
                    {
                        bandit = mn;
                        GameMasterScript.gmsSingleton.SetTempGameData("beastlakebandit", bandit.actorUniqueID);
                        break;
                    }
                }
            }
            if (bandit != null)
            {
                UIManagerScript.StartConversationByRef("dialog_mvrancher_questcomplete", DialogType.STANDARD, activeMap.FindActor("npc_monstervalleyrancher") as NPC);
            }
        }

        if (activeMap.IsItemWorld())
        {
            ItemDreamFunctions.CheckForAndWriteInfoAboutDreamFloor();
        }

        // Is boss floor?
        foreach (Monster mn in activeMap.monstersInMap)
        {
            if (mn.myTemplate.showBossHealthBar && MapMasterScript.CheckTileToTileLOS(mn.GetPos(), GameMasterScript.heroPCActor.GetPos(), GameMasterScript.heroPCActor, activeMap))
            {
                if (!GameMasterScript.IsAnimationPlayingFromCutscene())
                {
                    BossHealthBarScript.EnableBoss(mn);
                }
            }
        }

        if (MapMasterScript.activeMap.floor == MapMasterScript.BRANCH_PASSAGE_POSTBOSS1 && RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            if (ProgressTracker.CheckProgress(TDProgress.RANDOMJOBMODE_FIRST_BRANCH, ProgressLocations.META) != 1)
            {
                UIManagerScript.StartConversationByRef("randomjobmode_nobranches", DialogType.STANDARD, null);
                ProgressTracker.SetProgress(TDProgress.RANDOMJOBMODE_FIRST_BRANCH, ProgressLocations.META, 1);
            }
        }

        if (!SharaModeStuff.IsSharaModeActive() && !anyTutorial 
            && !UIManagerScript.AnyInteractableWindowOpen()
            && !GameMasterScript.IsGameInCutsceneOrDialog())
        {
            if (SharaModeStuff.CheckConditionsForSorceressUnlock()            
                && !SharedBank.CheckIfJobIsUnlocked(CharacterJobs.MIRAISHARA)
                && !RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                //Debug.Log("Shara story cleared, but didn't unlock sorceress. Let's unlock.");
                SharedBank.UnlockJob(CharacterJobs.MIRAISHARA, true);
            }
            else
            {
                //Debug.Log("Can't unlock for some reason. " + ProgressTracker.CheckProgress(TDProgress.SORCERESS_UNLOCKED, ProgressLocations.META));
            }
        }

        SharaModeStuff.CheckForSharaModeUnlockBasedOnProgress();
    }

    public static Vector2 FindPositionForHero(MapChangeInfo mci, Map prevMap, bool forced)
    {
        Vector2 specificTile = mci.spawnPosition;
        //if (Debug.isDebugBuild) Debug.Log("Find hero position starting from " + specificTile + " " + mci.spawnPosition);
        // Figure out hero placement.
        bool heroPlaced = false;

        if (GameMasterScript.gmsSingleton.ReadTempGameData("losingmysterydungeon") == 1)
        {
            Vector2 freePos = new Vector2(11f, 14f); // spawn player below portal
            GameMasterScript.gmsSingleton.SetTempGameData("losingmysterydungeon", 0);
            return freePos; 
        }

        // Item world floor 1.
        if (activeMap.IsMysteryDungeonMap()) // No start tiles at all here
        {
            specificTile = activeMap.FindClearGroundTile().pos;
        }
        else if (activeMap.floor == ITEMWORLD_START_FLOOR && specificTile != GameMasterScript.heroPCActor.portalLocationFromItemWorld && specificTile != Vector2.zero)
        {
            specificTile = activeMap.FindClearGroundTile().pos;
            Stairs stDown = null;
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (!st.stairsUp)
                {
                    stDown = st;
                    break;
                }
            }

            float checkDistance = 8f;
            int attempts = 0;
            while (GetGridDistance(specificTile, stDown.GetPos()) <= checkDistance)
            {
                specificTile = activeMap.FindClearGroundTile().pos;
                attempts++;
                if (attempts >= 500)
                {
                    attempts = 0;
                    checkDistance--;
#if UNITY_EDITOR
                    Debug.Log("Relaxing start search factor...");
#endif
                }
            }
        }
        else if (activeMap.floor == 0 && prevMap.floor == TUTORIAL_FLOOR)
        {
            Stairs toTown = null;
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (st.stairsUp)
                {
                    toTown = st;
                    break;
                }
            }
            specificTile = toTown.GetPos();
        }
        // Returning to main town from Riverstone Grove
        else if (activeMap.floor == TOWN_MAP_FLOOR && prevMap.floor == TOWN2_MAP_FLOOR)
        {
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (st.stairsUp)
                {
                    specificTile = st.GetPos();
                    specificTile.y += 1;
                    break;
                }
            }
        }

        Vector2 movePlayerToLocation = Vector2.zero;

        // Special waypoint travel case, this is definitely a hack.
        // We want the map to FIND the best tile, not be fed possibly bad connective data from town.
        if (GameMasterScript.gmsSingleton.ReadTempGameData("waypointtravel") == 1)
        {
            //if (Debug.isDebugBuild) Debug.Log("Waypoint travel.");
            specificTile = Vector2.zero;
        }

        if (specificTile != Vector2.zero)
        {
            movePlayerToLocation = specificTile;
            //if (Debug.isDebugBuild) Debug.Log("Placing hero at specific tile: " + movePlayerToLocation);
            heroPlaced = true;
        }

        if (!heroPlaced )
        {
            if ((activeMap.dungeonLevelData.layoutType == DungeonFloorTypes.SPECIAL && !GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(activeMap.mapAreaID)) ||
                (activeMap.floor >= JELLY_DRAGON_DUNGEONSTART_FLOOR && activeMap.floor <= JELLY_DRAGON_DUNGEONEND_FLOOR))
            {
                movePlayerToLocation = activeMap.heroStartTile;
                //Debug.Log("Placing hero at special start tile: " + movePlayerToLocation);
                heroPlaced = true;
            }
        }

        if (!forced && !heroPlaced)
        {
            bool foundStairs = false;
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (st == GameMasterScript.heroPCActor.lastStairsTraveled)
                {
                    movePlayerToLocation = st.GetPos();
                    st.usedByPlayer = true;
                    foundStairs = true;
                    heroPlaced = true;
                    break;
                }
            }
            if (!foundStairs)
            {
                foreach (Stairs st in activeMap.mapStairs)
                {
                    if (st.NewLocation == prevMap)
                    {
                        movePlayerToLocation = st.GetPos();
                        st.usedByPlayer = true;
                        heroPlaced = true;
                        //if (Debug.isDebugBuild) Debug.Log("Placing hero at any stairs from prev map: " + movePlayerToLocation);
                        break;
                    }
                }

            }
        }
        else if (forced && !heroPlaced)
        {
            foreach (Stairs st in activeMap.mapStairs)
            {
                if (st.stairsUp)
                {
                    //Debug.Log("defaulting to stairsup " + st.GetPos());
                    movePlayerToLocation = st.GetPos();
                    //if (Debug.isDebugBuild) Debug.Log("Placing hero at any valid stairs: " + movePlayerToLocation);
                    heroPlaced = true;
                    break;
                }
            }
        }

        if (!heroPlaced)
        {
            if (activeMap.dungeonLevelData.layoutType == DungeonFloorTypes.SPECIAL)
            {
                movePlayerToLocation = activeMap.heroStartTile;
                //if (Debug.isDebugBuild) Debug.Log("Placing hero at special start tile B: " + movePlayerToLocation);
                heroPlaced = true;
            }
            else
            {
                Stairs stBackup = null;
                foreach (Stairs st in activeMap.mapStairs)
                {
                    if (st.NewLocation == prevMap)
                    {
                        //Debug.Log("Stairs up link.");
                        movePlayerToLocation = st.GetPos();
                        //if (Debug.isDebugBuild) Debug.Log("Placing hero at specific stairs B: " + movePlayerToLocation);
                        heroPlaced = true;
                        break;
                    }
                    if (st.stairsUp)
                    {
                        stBackup = st;
                    }
                }
                if (!heroPlaced)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Last resort to ANY stairs up.");

                    if (stBackup == null)
                    {
                        specificTile = activeMap.FindClearGroundTile().pos;
                        movePlayerToLocation = specificTile;
                        //Debug.Log("Placing hero at ANY stairs up B: " + movePlayerToLocation);
                        heroPlaced = true;
                    }
                    else
                    {
                        movePlayerToLocation = stBackup.GetPos();
                        //Debug.Log("Placing hero at backup stairs B: " + movePlayerToLocation);
                        heroPlaced = true;
                    }
                }
                if (!heroPlaced)
                {
                    // Pick any random tile.
                    MapTileData mtd = activeMap.GetRandomEmptyTile(new Vector2(5, 5), 1, false);
                    specificTile = mtd.pos;
                    movePlayerToLocation = mtd.pos;
                    heroPlaced = true;
                    if (Debug.isDebugBuild) Debug.Log("WARNING! Could not find ANY stairs for the player to spawn in " + activeMap.floor);
                }
            }
        }

        if (activeMap.floor == TOWN_MAP_FLOOR)
        {
            if (movePlayerToLocation.y == 15f && movePlayerToLocation.x >= 20f)
            {
                movePlayerToLocation.y = 14f;
            }
        }

        if (activeMap.floor == TOWN2_MAP_FLOOR)
        {
            movePlayerToLocation.y -= 1f;
        }

        //if (Debug.isDebugBuild) Debug.Log("Final player location is: " + movePlayerToLocation);
        return movePlayerToLocation;
    }

    public static bool CheckIfSubmerged(Actor act)
    {
        if (GetTile(act.GetPos()).CheckTag(LocationTags.WATER))
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (mn.CheckAttribute(MonsterAttributes.FLYING) == 0)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    public static void VerifyBuriedLootHasKey()
    {
        foreach (Actor act in activeMap.actorsInMap)
        {
            if (act.actorRefName == "item_goldkey")
            {
                if (act.GetPos().x > 1 && act.GetPos().y > 1)
                {
                    return;
                }

            }
        }
        // No key? weird. Give it to a monster.
        Item goldKey = LootGeneratorScript.CreateItemFromTemplateRef("item_goldkey", 1.0f, 0f, false);
        Monster m = activeMap.monstersInMap[0];
        m.myInventory.AddItemRemoveFromPrevCollection(goldKey, false);
        goldKey.SetActorData("alwaysdrop", 1);
        //Debug.Log("Added gold key to monster " + m.actorRefName + " because it wasn't in the map.");
    }

    public static Destructible SpawnCoins(MapTileData origTile, MapTileData tileForMoney, int amount)
    {
        Destructible coins = MapMasterScript.activeMap.CreateDestructibleInTile(origTile, "obj_coins");
        coins.moneyHeld = amount;
        MapMasterScript.singletonMMS.SpawnDestructible(coins);
        coins.myMovable.SetPosition(origTile.pos);
        coins.destroyOnStep = true;

        // Toss instead.
        //coins.myMovable.AnimateSetPosition(tileForMoney.pos, 0.3f, false, 360f, 3f, MovementTypes.SLERP);

        CombatManagerScript.FireProjectile(origTile.pos, tileForMoney.pos, coins.GetObject(), 0.25f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);

        activeMap.RemoveActorFromLocation(origTile.pos, coins);
        activeMap.AddActorToLocation(tileForMoney.pos, coins);
        coins.SetCurPos(tileForMoney.pos);
        coins.SetSpawnPos(tileForMoney.pos);
        //Debug.Log("New money is in " + tileForMoney.pos + " CHECK this mtd " + tileForMoney.CheckActorRef("obj_coins") + " orig? " + origTile.CheckActorRef("obj_coins") + " " + coins.GetPos() + " " + coins.myMovable.position);
        return coins;
    }

    // Checks if the gameobject generates sprite-blocking height and sets tile data appropriately
    void CheckForExtraHeightTiles(GameObject go, MapTileData mtd, float x, float y)
    {
        Movable mv = go.GetComponent<Movable>();
        if (mv != null)
        {
            mtd.extraHeightTiles = mv.extraHeightTiles;
            mtd.diagonalBlock = mv.diagonalBlock;
            mtd.diagonalLBlock = mv.diagonalLBlock;
            go.transform.position = new Vector2(x, y);
            go.GetComponent<SpriteRenderer>().sortingOrder = (120 - (int)y) * 10;
            activeNonTileGameObjects.Add(go);
        }
    }

    public static void UnlockFastTravelDialog(Map theMapToUnlock)
    {
        StringManager.SetTag(4, StringManager.GetPortalBindingString());
        GameMasterScript.gmsSingleton.SetTempStringData("fasttravel_destination", theMapToUnlock.GetName());

        Conversation newConvo = SharaModeStuff.IsSharaModeActive() ?
            GameMasterScript.FindConversation("dialog_waypoint_unlocked_shara") :
            GameMasterScript.FindConversation("dialog_waypoint_unlocked");

        if (GameMasterScript.IsGameInCutsceneOrDialog())
        {
            //GameLogScript.GameLogWrite(newConvo.allBranches[0].text, GameMasterScript.heroPCActor);
            return;
        }

        if (UIManagerScript.dialogBoxOpen) return;

        UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
    }

    public static void DoSpeedrunningChangesIfNecessary()
    {
        if (!GameEventsAndTriggers.ShouldCutscenesBeSkipped()) return;

        // Boss 1 map - music begins playing immediately
        Map boss1 = theDungeon.FindFloor(BOSS1_MAP_FLOOR);
        boss1.musicCurrentlyPlaying = "bosstheme1";
        // Also remove the trigger tiles for "widen doorway"
        ClearActorsByRefFromMap(boss1, new List<string>() { "dirtbeak_bottleneck_explainer" });

        // Clear the boss2 callout trigger from the pre-boss2 area
        Map preBoss2 = theDungeon.FindFloor(PREBOSS2_MAP_FLOOR);
        ClearActorsByRefFromMap(preBoss2, new List<string>() { "trigger_preboss2" });

        Map boss2 = theDungeon.FindFloor(BOSS2_MAP_FLOOR);
        // Remove Dirtbeak from boss fight 2 room
        ClearActorsByRefFromMap(boss2, new List<string>() { "mon_banditwarlord", "obj_bossdevice" });

        // Set the map state correctly
        Cutscenes.DoPreBossFight2Stuff(boss2, false);
        boss2.musicCurrentlyPlaying = "bosstheme1";

        Map meadow = theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS1);
        ClearActorsByRefFromMap(meadow, new List<string>() { "trigger_oldamberstation", "trigger_fungalcave" });

        Map vista = theDungeon.FindFloor(BRANCH_PASSAGE_POSTBOSS2);
        ClearActorsByRefFromMap(vista, new List<string>() { "npc_shara1", "obj_sharacallout_vista", "obj_sharacallout2_vista" });

        Map centralProc = theDungeon.FindFloor(FINAL_HUB_FLOOR);
        ClearActorsByRefFromMap(centralProc, new List<string>() { "trigger_finalhubdialog", "trigger_finalhubdialog2" });

        GameMasterScript.heroPCActor.SetActorData("triggerfinalhub", 1);
        GameMasterScript.heroPCActor.SetActorData("triggerfinalhubentry", 1);
    }

    public static void ClearActorsByRefFromMap(Map m, List<string> refs)
    {
        List<Actor> triggerRemove = new List<Actor>();
        foreach (Actor act in m.actorsInMap)
        {
            if (refs.Contains(act.actorRefName))
            {
                triggerRemove.Add(act);
            }
        }
        foreach (Actor act in triggerRemove)
        {
            m.RemoveActorFromLocation(act.GetPos(), act);
            m.RemoveActorFromMap(act);
        }
    }

    /// <summary>
    /// Returns TRUE if anyTutorial has been started.
    /// </summary>
    /// <returns></returns>
    public static bool CheckForNewMapExploredEvents(bool anyTutorial)
    {
        if (activeMap.floor == BURIED_LOOT_FLOOR)
        {
            VerifyBuriedLootHasKey();
        }

        // Make sure there are no stacked stairs for any reason!
        foreach(Stairs st1 in activeMap.mapStairs)
        {
            // Compare every stairs to every other stairs to see if they're in the same tile.
            foreach(Stairs st2 in activeMap.mapStairs)
            {
                if (st1.actorUniqueID == st2.actorUniqueID)
                {
                    continue;
                }
                if (st1.GetPos() != st2.GetPos())
                {
                    continue;
                }
                // These stairs are occupying the same tile. We should move st2.
                MapTileData mtdForStairs = activeMap.GetRandomEmptyTileForMapGen();
                int attempts = 0;
                while (mtdForStairs == null)
                {
                    attempts++;
                    mtdForStairs = activeMap.GetRandomEmptyTileForMapGen();
                    if (attempts == 10)
                    {
                        break;
                    }
                }
                if (mtdForStairs == null)
                {
                    Debug.Log("Couldn't repositions stacked stairs.");
                    continue;
                }
                activeMap.RepositionActor(st2, mtdForStairs);
            }
        }

        GameMasterScript.heroPCActor.mapsExploredByMapID.Add(activeMap.mapAreaID);
        if (activeMap.dungeonLevelData != null && !GameMasterScript.heroPCActor.mapFloorsExplored.Contains(activeMap.dungeonLevelData.floor))
        {
            GameMasterScript.heroPCActor.mapFloorsExplored.Add(activeMap.dungeonLevelData.floor);
        }

        if (PlayerOptions.tutorialTips && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_sideareas") && activeMap.dungeonLevelData.sideArea)
        {
            Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_sideareas");
            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
            anyTutorial = true;
        }

        // Discovered a new fast travel destination? Print it to the log.
        if (activeMap.dungeonLevelData.fastTravelPossible && !MapMasterScript.activeMap.IsItemWorld())
        {
            UnlockFastTravelDialog(activeMap);
        }

        return anyTutorial;
    }
    
    /// <summary>
    /// Also removes tongue latched Mimics and such.
    /// </summary>
    static void RemoveCrabGrabsAndBadAnchors()
    {
        // Get rid of crabgrabs.
        List<StatusEffect> SEtoRemove = new List<StatusEffect>();
        foreach (StatusEffect se in GameMasterScript.heroPCActor.myStats.GetAllStatuses())
        {
            if (se.refName == "status_crabgrab" || se.refName == "crabbleed" || se.refName == "status_paralyzed")
            {
                SEtoRemove.Add(se);
                if (se.listEffectScripts.Count > 0)
                {
                    Actor orig = se.listEffectScripts[0].originatingActor;
                    if (orig != null)
                    {
                        GameMasterScript.heroPCActor.RemoveAnchor(orig);
                        orig.anchor = orig;
                        orig.anchorRange = 99;
                        orig.summoner = null;
                    }
                }
            }
        }
        foreach (StatusEffect se in SEtoRemove)
        {
            GameMasterScript.heroPCActor.myStats.RemoveStatus(se, true);
        }
        if (SEtoRemove.Count > 0)
        {
            foreach (Monster mn in activeMap.monstersInMap)
            {
                // Get rid of crabgrabs on the grabber as well
                if (mn.actorRefName == "mon_redcrab")
                {
                    mn.myStats.RemoveStatusByRef("status_crabgrab");
                }
            }
        }

        SEtoRemove.Clear();

        // Disconnect any mimics from us
        GameMasterScript.heroPCActor.DisconnectMimicsIfNecessary();
        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (!act.IsFighter()) continue;
            Fighter ft = act as Fighter;
            if (!ft.myStats.IsAlive()) continue;
            ft.DisconnectMimicsIfNecessary();
        }
    }

    /// <summary>
    /// Changes the number of fountains in a map based on how many charges we have in our flask (or Shara shield equivalent)
    /// </summary>
    /// <param name="destinationMap"></param>
    static void AdjustFountainsForMapOnFirstEntry(Map destinationMap)
    {
        // New map, shall we mess with Fountain count? :D
        int curCharges = GameMasterScript.heroPCActor.regenFlaskUses;
        int fountainsToAddOrRemove = 0;

        if (SharaModeStuff.IsSharaModeActive())
        {
            // Reduce fountains if our shield is Huge, give a bit more if our shield is low.
            float flowShieldDmgLeft = GameMasterScript.heroPCActor.ReadActorData("flowshield_dmgleft");
            float maxHealth = GameMasterScript.heroPCActor.myStats.GetMaxStat(StatTypes.HEALTH);

            if (flowShieldDmgLeft >= maxHealth * 0.5f && flowShieldDmgLeft < maxHealth)
            {
                fountainsToAddOrRemove--;
            }
            else if (flowShieldDmgLeft >= maxHealth && flowShieldDmgLeft < maxHealth * 1.5f)
            {
                fountainsToAddOrRemove -= 2;
            }
            else if (flowShieldDmgLeft >= maxHealth * 2f)
            {
                fountainsToAddOrRemove -= 3;
            }
            else if (flowShieldDmgLeft < maxHealth * 0.25f)
            {
                fountainsToAddOrRemove += 1;
            }
        }
        else
        {
            if (curCharges == 0)
            {
                fountainsToAddOrRemove += 3;
            }
            else if (curCharges > 0 && curCharges < 5)
            {
                fountainsToAddOrRemove += 2;
            }
            else if (curCharges > 20)// && curCharges < 25)
            {
                fountainsToAddOrRemove -= 1;
            }
            else
            {
                //fountainsToAddOrRemove -= 2;
            }
        }

        if (fountainsToAddOrRemove != 0) destinationMap.fountainsInMap.Shuffle();
        if (fountainsToAddOrRemove < 0)
        {
            for (int i = fountainsToAddOrRemove; i < 0; i++)
            {
                if (destinationMap.fountainsInMap.Count == 0) break;
                Destructible fountain = destinationMap.fountainsInMap[0] as Destructible;
                destinationMap.fountainsInMap.Remove(fountain);
                destinationMap.RemoveActorFromMap(fountain);
            }
        }
        else if (fountainsToAddOrRemove > 0)
        {
            if (GameStartData.NewGamePlus == 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
            {
                fountainsToAddOrRemove = (int)(fountainsToAddOrRemove * 1.2f);
            }
            for (int i = 0; i < fountainsToAddOrRemove; i++)
            {
                destinationMap.SpawnFountainInMap();
            }
        }
    }

    void Update()
	{
        if (activeMap != null)
        {
            activeMap.TickFrame();
        }
	}
    
    public static void RelocateMonsterToPlayerWithinTileRange(Fighter monToRelocate, int distanceFromPlayer, int minimumRange = 1)
    {
        MapTileData findNearbyTile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), distanceFromPlayer, true, false);

        int attempts = 0;
        while (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), findNearbyTile.pos) < minimumRange)
        {
            attempts++;
            if (attempts > 100) break;
            MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), distanceFromPlayer, true, false);
        }

        if (monToRelocate.GetActorMap() != GameMasterScript.heroPCActor.GetActorMap())
        {
            monToRelocate.GetActorMap().RemoveActorFromMap(monToRelocate);
            GameMasterScript.heroPCActor.GetActorMap().AddActorToMap(monToRelocate);
            MapMasterScript.activeMap.PlaceActor(monToRelocate, findNearbyTile);
            monToRelocate.SetPos(findNearbyTile.pos);
        }
        else
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(monToRelocate.GetPos(), monToRelocate);
            MapMasterScript.activeMap.AddActorToLocation(findNearbyTile.pos, monToRelocate);
            monToRelocate.SetPos(findNearbyTile.pos);
            if (monToRelocate.myMovable != null)
            {
                monToRelocate.myMovable.SetPosition(findNearbyTile.pos);
            }
        }
    }
    public static bool IsActiveMapMysteryDungeon()
    {
        if (activeMap == null) return false;
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        return activeMap.IsMysteryDungeonMap();
    }
    /// Creates a NEW MAP of either basic type (Map) or special type (Dragon_Robot, etc) based on layout tpye in dlData
    /// </summary>
    /// <param name="dlData"></param>
    /// <param name="mapFloor"></param>
    public static Map SetCorrectMapTypeFromDungeonLevelData(DungeonLevel dlData, int mapFloor)
    {
        Map readingMap = null;
        
        switch (dlData.layoutType)
        {
            case DungeonFloorTypes.DRAGON_ROBOT:
                readingMap = new Map_DragonRobot(mapFloor);
                break;
            case DungeonFloorTypes.DRAGON_ROBOT_BRIDGE:
                readingMap = new Map_DragonRobot_BridgeArea(mapFloor);
                break;
            case DungeonFloorTypes.BANDITDUNGEON_HUB:
                readingMap = new Map_BanditDungeonHub(mapFloor);
                break;
            case DungeonFloorTypes.SPIRIT_DUNGEON:
                readingMap = new Map_SpiritDungeon(mapFloor);
                break;
            case DungeonFloorTypes.SLIME_DUNGEON:
                readingMap = new Map_SlimeDungeon(mapFloor);
                break;
            default:
			    readingMap = new Map(mapFloor);
                break;
        }

        readingMap.floor = mapFloor;
        return readingMap;
    }

    public static void TryRunScriptOnMapEntry()
    {
        if (!string.IsNullOrEmpty(activeMap.dungeonLevelData.script_onEnterMap))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(MapEventScripts), activeMap.dungeonLevelData.script_onEnterMap);
            if (runscript != null)
            {
                object[] paramList = new object[1];
                paramList[0] = activeMap;
                runscript.Invoke(null, paramList);
            }
        }
    }

    public static void OnMapRemoved(Map m)
    {
        int count = m.actorsInMap.Count;
        for (int i = 0; i < count; i++)
        {
            Actor act = m.actorsInMap[i];
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            DTPooling.ReturnToPool(act as Destructible);
        }
    }
    public void InitializeDictAllMapsIfNecessary()
    {
        if (dictAllMaps != null)
        {
            dictAllMaps.Clear();
        }
        else
        {
            dictAllMaps = new Dictionary<int, Map>(200); // size initializer to prevent list expansion
        }
    }	
}
