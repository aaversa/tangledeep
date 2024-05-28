using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum LocationTags { CORRIDOR, CORRIDORENTRANCE, CORNER, DUGOUT, HASDECOR, WATER, LAVA, MUD, SECRETENTRANCE, SECRET, SUMMONEDMUD, GRASS, 
     SOLIDTERRAIN, GRASS2, ISLANDSWATER, TREE, ELECTRIC, LASER, HOLE, COUNT }

//REMOVED: MapEdge, EdgeTile
public enum MapGenerationTags { EDGETILE, MAPGEN_TREASURE1, MAPGEN_CHAMP1, ESSENTIALCORRIDOR, MAPGEN_CHAMP2, OVERLAP, MAPGEN_CHAMP3, MAPGEN_CHEST1, MAPGEN_CHEST2, MAPGEN_CHEST3, MAPGEN_CHEST4, MAPGEN_CHEST5, MAPGEN_CHEST6, MAPGEN_BARREL,
    MAPGEN_MONSTER, WALLDECOR3X3START, WALLDECOR2X2START, MAZEFILL, NOCONNECTION, ENTRANCEPOSSIBLE, MAPGEN_RIVERDIGOUT, SECRETSTUFF, HOLE, DONT_CONVERT_TO_HOLE, FILLED, HAZARD, COUNT
}
//public enum VisualTileType { NORTH, NORTHEAST, NORTHEAST_OPEN, EAST, SOUTHEAST, SOUTHEAST_OPEN, SOUTH, SOUTHWEST, SOUTHWEST_OPEN, WEST, NORTHWEST, NORTHWEST_OPEN, STANDALONE, FILL, WEST_HORIZONTAL_NORTH_ENDCAP, EAST_HORIZONTAL_NORTH_ENDCAP, NORTH_VERTICAL_EAST_ENDCAP, NORTH_VERTICAL_WEST_ENDCAP, SOUTH_VERTICAL_WEST_ENDCAP, SOUTH_VERTICAL_EAST_ENDCAP, WEST_HORIZONTAL_SOUTH_ENDCAP, EAST_HORIZONTAL_SOUTH_ENDCAP, HORIZONTALWALL, VERTICALWALL, TBONE_NORTH, TBONE_SOUTH, TBONE_WEST, TBONE_EAST, DEADEND_NORTH, DEADEND_EAST, DEADEND_SOUTH, DEADEND_WEST, OPEN4CORNERS, NOTHING, COUNT }
public enum VisualTileType { WALL_E, WALL_S, WALL_N_E_S_W, WALL_W, WALL_E_S, WALL_E_W, WALL_S_W, WALL_E_S_W, WALL_N_S, WALL_N, WALL_NONE, WALL_N_W, WALL_N_E_S, WALL_N_S_W, WALL_N_E_W, WALL_N_E, GROUND, NOTSET, COUNT }
public enum TileSet { EARTH, STONE, COBBLE, VOID, FUTURE, SLATE, SPECIAL, RUINED, VOLCANO, REINFORCED, LUSHGREEN, MOSS,
    BLUESTONEDARK, BLUESTONELIGHT, SNOW, NIGHTMARISH, SAND, TREETOPS, MOUNTAINGRASS, COUNT }
public enum IRLCollision { PLAYER, MONSTER, ANY, COUNT }

public class Point
{
    public int x;
    public int y;

    public Point(int nx, int ny)
    {
        x = nx;
        y = ny;
    }

    public Point(Vector2 v)
    {
        x = (int)v.x;
        y = (int)v.y;
    }

    public Point()
    {

    }

    public bool Compare(Point p)
    {
        if ((x == p.x) && (y == p.y)) return true;
        return false;
    }
}

[System.Serializable]
public class MapTileData : IComparable<MapTileData>
{
    private List<Actor> actorsInTile;
    public AreaTypes areaType;
    //public Sprite tileSprite;               never used
    public int indexOfSpriteInAtlas;
    public int indexOfDecorSpriteInAtlas;
    public int indexOfGrassSpriteInAtlas;
    public int indexOfTerrainSpriteInAtlas; // Water, Mud, Lava... Chunk decor/
    public int overlayIndexOfTerrainSprite = -1;
    public Vector2 pos;
    public Point iPos;
    //public bool withinVisibleRadius;       
    //public bool visible;                    
    public bool actorBlocksVision;
    public bool inherentBlocksVision;

    bool anyActorsInTile; // simple bool so we don't have to check if list is null/empty every time.

    bool planksInTile;

    public static readonly int[] EARTH_TILE_INDICES = { 0, 25, 50 };
    public static readonly int[] STONE_TILE_INDICES = { 75, 100, 125, 500 };
    public static readonly int[] COBBLE_TILE_INDICES = { 150, 175, 525 };
    public static readonly int[] VOID_TILE_INDICES = { 200, 225 };
    public static readonly int[] FUTURE_TILE_INDICES = { 250, 275, 875 };
    public static readonly int[] SLATE_TILE_INDICES = { 300, 325, 350 };
    public static readonly int[] RUINED_TILE_INDICES = { 375, 400 };
    public static readonly int[] VOLCANO_TILE_INDICES = { 425, 450 };
    public static readonly int[] REINFORCED_TILE_INDICES = { 475, 850 };
    public static readonly int[] LUSHGREEN_TILE_INDICES = { 550, 575, 600 };
    public static readonly int[] MOSS_TILE_INDICES = { 625, 650, 675 };
    public static readonly int[] BLUESTONEDARK_TILE_INDICES = { 700, 725, 750 };
    public static readonly int[] BLUESTONELIGHT_TILE_INDICES = { 775, 800, 825 };
    public static readonly int[] SNOW_TILE_INDICES = { 900, 925 };
    public static readonly int[] NIGHTMARISH_TILE_INDICES = { 975, 1000 };
    public static readonly int[] SAND_TILE_INDICES = { 950,1025 };
    public static readonly int[] TREETOP_TILE_INDICES = { 1050,1075 };
    public static readonly int[] MOUNTAINGRASS_TILE_INDICES = { 1125 };
    public const int NUMBER_SPRITES_PER_ROW = 25;

    public TileTypes tileType;
    public int tags;
    public int mapGenTags;
    short[] objectFlagQuantities;


    public bool anyLocationTags
    {
        get { return tags > 0; }
    }
    bool anyMapGenTags;
    bool anyObjectFlags;

    public VisualTileType visualTileType;
    public VisualTileType visualGrassTileType;
    public VisualTileType visualTerrainTileType;
    public bool triedForCorridorCreation;

    public TileSet tileVisualSet;
    public int wallReplacementIndex;

    public int caveColor; // Debug only REMOVE in final.
    //public int caveColorOrigDebug; // Debug only REMOVE in final.    never used

    // Used for pathfinding.
    public MapTileData parent;
    public MapTileData child;
    public float f, g, h;

    // Deprecate these
    public bool open;
    public bool closed;

    // New faster method for clearing for pathfinding
    public static int openState = 1;
    public static int closedState = 2;
    public int pfState = 0;

    public List<Item> returnItems;
    public List<Actor> returnActors;

    public bool collidableActors;

    public int numberOfItemsInTile;

    public bool checkedForRoomGen = false;
    public bool[] dirCheckedForRoomGen;

    public bool monCollidable;
    public bool playerCollidable;
    public int floor;

    public int extraHeightTiles;
    public bool diagonalBlock;
    public bool diagonalLBlock;

    public bool[] specialMapObjectsInTile;

    int localTileID;
    static int globalTileIDCounter;
    public bool anyItemsInTile;
    public bool CheckForSpecialMapObjectType(SpecialMapObject smo)
    {
        if (!anyActorsInTile) return false;

        return specialMapObjectsInTile[(int)smo];


        if (actorsInTile == null || actorsInTile.Count == 0)
        {
            return false;
        }
        foreach(Actor act in actorsInTile)
        {
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType == smo) return true;
            }
        }
        return false;
    }

    public bool IsEmpty()
    {
        return !anyActorsInTile;
    }

    public bool HasActorByRef(string aRef)
    {
        if (!anyActorsInTile) return false;

        foreach (Actor checkAct in actorsInTile)
        {
            if (checkAct.actorRefName == aRef)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasActor(Actor act)
    {
        if (!anyActorsInTile) return false;
        
        foreach(Actor checkAct in actorsInTile)
        {
            if (checkAct == act)
            {
                return true;
            }
        }
        return false;
    }
    public bool HasBreakableCollidable(Actor act)
    {
        if (!anyActorsInTile) return false;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = actorsInTile[i] as Destructible;
                if (dt.destroyed || dt.isDestroyed) continue;
                if (dt.targetable && act.actorfaction == Faction.PLAYER
                    || dt.monsterDestroyable && act.actorfaction == Faction.ENEMY)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool HasPlanks()
    {
        return planksInTile;
    }

    public bool AnyTerrainHazard()
    {
        if (CheckTag(LocationTags.LAVA) || CheckTag(LocationTags.LASER) || CheckTag(LocationTags.ISLANDSWATER)
            || CheckTag(LocationTags.ELECTRIC) || CheckTag(LocationTags.WATER) || CheckTag(LocationTags.MUD))
        {
            return true;
        }
        return false;
    }

    public void SetTileVisualType(VisualTileType vtt)
    {
        // Max variations right now: 4
        // 18 per row

        visualTileType = vtt; // NEW AS OF 12/26, THIS WASN'T BEING SET BEFORE????

        

        if ((CheckTag(LocationTags.SOLIDTERRAIN)) || (CheckTag(LocationTags.TREE)))
        {
            indexOfSpriteInAtlas = 16;
            wallReplacementIndex = GetWallReplacementIndex();
            /* if (CheckTag(LocationTags.TREE))
            {
                wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.justTrees.Length);
            } */
        }
        else
        {
            indexOfSpriteInAtlas = (int) vtt;

            // Standalone. Maybe add a tree or something?
            if (vtt == VisualTileType.WALL_NONE)
            {
                if ((this.tileType == TileTypes.WALL && UnityEngine.Random.Range(0, 1f) <= MapMasterScript.singletonMMS.chance1xWallReplace) ||
                    floor == MapMasterScript.JOB_TRIAL_FLOOR)
                {
                    AddTag(LocationTags.SOLIDTERRAIN);
                    indexOfSpriteInAtlas = 16;
                    wallReplacementIndex = GetWallReplacementIndex();
                }
            }
            //lots of ground tiles get a random variant, but apparently some only get them 33% of the time.
            else if (vtt == VisualTileType.GROUND)
            {
                if (tileVisualSet == TileSet.COBBLE || tileVisualSet == TileSet.SAND || tileVisualSet == TileSet.NIGHTMARISH 
                    || tileVisualSet == TileSet.SNOW || tileVisualSet == TileSet.LUSHGREEN || tileVisualSet == TileSet.REINFORCED 
                    || tileVisualSet == TileSet.FUTURE || tileVisualSet == TileSet.SLATE || tileVisualSet == TileSet.VOLCANO 
                    || tileVisualSet == TileSet.RUINED || tileVisualSet == TileSet.BLUESTONELIGHT || tileVisualSet == TileSet.BLUESTONEDARK 
                    || tileVisualSet == TileSet.TREETOPS || tileVisualSet == TileSet.MOUNTAINGRASS)
                {
                    indexOfSpriteInAtlas = UnityEngine.Random.Range(17, NUMBER_SPRITES_PER_ROW);
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 1f) <= 0.33f)
                    {
                        indexOfSpriteInAtlas = UnityEngine.Random.Range(17, NUMBER_SPRITES_PER_ROW);
                    }
                }
            }
        }


        // Use variation tileset if available.

        //indexOfSpriteInAtlas += UnityEngine.Random.Range(0, numVariations) * NUMBER_SPRITES_PER_ROW;

        // for future reference:
        // https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=netframework-4.7.2
        
        if (tileVisualSet == TileSet.EARTH)
        {
            indexOfSpriteInAtlas += EARTH_TILE_INDICES[UnityEngine.Random.Range(0,EARTH_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.STONE)
        {
            indexOfSpriteInAtlas += STONE_TILE_INDICES[UnityEngine.Random.Range(0, STONE_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.COBBLE)
        {
            indexOfSpriteInAtlas += COBBLE_TILE_INDICES[UnityEngine.Random.Range(0, COBBLE_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.VOID)
        {
            indexOfSpriteInAtlas += VOID_TILE_INDICES[UnityEngine.Random.Range(0, VOID_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.FUTURE)
        {
            indexOfSpriteInAtlas += FUTURE_TILE_INDICES[UnityEngine.Random.Range(0, FUTURE_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.SLATE)
        {
            indexOfSpriteInAtlas += SLATE_TILE_INDICES[UnityEngine.Random.Range(0, SLATE_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.LUSHGREEN)
        {
            indexOfSpriteInAtlas += LUSHGREEN_TILE_INDICES[UnityEngine.Random.Range(0, LUSHGREEN_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.SNOW)
        {
            indexOfSpriteInAtlas += SNOW_TILE_INDICES[UnityEngine.Random.Range(0, SNOW_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.NIGHTMARISH)
        {
            indexOfSpriteInAtlas += NIGHTMARISH_TILE_INDICES[UnityEngine.Random.Range(0, NIGHTMARISH_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.RUINED)
        {
            indexOfSpriteInAtlas += RUINED_TILE_INDICES[UnityEngine.Random.Range(0, RUINED_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.VOLCANO)
        {
            indexOfSpriteInAtlas += VOLCANO_TILE_INDICES[UnityEngine.Random.Range(0, VOLCANO_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.REINFORCED)
        {
            indexOfSpriteInAtlas += REINFORCED_TILE_INDICES[UnityEngine.Random.Range(0, REINFORCED_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.SAND)
        {
            indexOfSpriteInAtlas += SAND_TILE_INDICES[UnityEngine.Random.Range(0, SAND_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.TREETOPS)
        {
            indexOfSpriteInAtlas += TREETOP_TILE_INDICES[UnityEngine.Random.Range(0, TREETOP_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            indexOfSpriteInAtlas += MOUNTAINGRASS_TILE_INDICES[UnityEngine.Random.Range(0, MOUNTAINGRASS_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.MOSS)
        {
            indexOfSpriteInAtlas += MOSS_TILE_INDICES[UnityEngine.Random.Range(0, MOSS_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.BLUESTONEDARK)
        {
            indexOfSpriteInAtlas += BLUESTONEDARK_TILE_INDICES[UnityEngine.Random.Range(0, BLUESTONEDARK_TILE_INDICES.Length)];
        }
        else if (tileVisualSet == TileSet.BLUESTONELIGHT)
        {
            indexOfSpriteInAtlas += BLUESTONELIGHT_TILE_INDICES[UnityEngine.Random.Range(0, BLUESTONELIGHT_TILE_INDICES.Length)];
        }
    }

    public bool CheckHasExtraHeight(int tiles)
    {
        if (tiles <= extraHeightTiles) // This was reversed, which I think was busted?
        {
            return true;
        }
        return false;
    }

    public bool CheckDiagonalBlock()
    {
        return diagonalBlock;
    }

    public bool CheckDiagonalLBlock()
    {
        return diagonalLBlock;
    }

    public void SelectWallReplacementIndex()
    {
        if (wallReplacementIndex != -1) return; // already have a replacement index, so what's the problem bud

        if (tileVisualSet == TileSet.EARTH || tileVisualSet == TileSet.SLATE || tileVisualSet == TileSet.LUSHGREEN || 
            tileVisualSet == TileSet.MOSS || tileVisualSet == TileSet.SNOW || tileVisualSet == TileSet.TREETOPS || tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            if (CheckTag(LocationTags.SOLIDTERRAIN))
            {
                switch (tileVisualSet)
                {
                    case TileSet.EARTH:
                    case TileSet.MOSS:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.earthWallSingleReplacements.Length);
                        break;
                    case TileSet.SLATE:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.slateWallSingleReplacements.Length);
                        break;
                    case TileSet.TREETOPS:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.treeTopsWallSingleReplacements.Length);
                        break;
                    case TileSet.MOUNTAINGRASS:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.mountainGrassWallSingleReplacements.Length);
                        break;
                    case TileSet.LUSHGREEN:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.lushGreenWallSingleReplacements.Length);
                        break;
                    case TileSet.SNOW:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.snowWallSingleReplacements.Length);
                        break;
                    case TileSet.SAND:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.desertWallSingleReplacements.Length);
                        break;
                    case TileSet.STONE:
                    case TileSet.BLUESTONEDARK:
                    case TileSet.BLUESTONELIGHT:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.stoneWallSingleReplacements.Length);
                        break;
                    case TileSet.COBBLE:
                        wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.cobbleWallSingleReplacements.Length);
                        break;
                }
            }

            if (CheckTag(LocationTags.TREE))
            {
                wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.justTrees.Length);
            }            
        }    
    }

    public void SetTerrainTileType(LocationTags terrainType, VisualTileType tileType, LocationTags overrideTagForBeautification = LocationTags.COUNT)
    {
        switch(terrainType)
        {
            case LocationTags.GRASS:
            case LocationTags.GRASS2:
                SetGrassTileType(tileType);
                break;
            case LocationTags.WATER:
            case LocationTags.LAVA:
            case LocationTags.ELECTRIC:
            case LocationTags.MUD:
            case LocationTags.SUMMONEDMUD:
            case LocationTags.LASER:
                SetTerrainTileType(tileType, overrideTagForBeautification);
                break;
        }
    }

    public void SetGrassTileType(VisualTileType tileType)
    {
        // Variations right now: 2
        // 20 per row
        visualGrassTileType = tileType;

        switch (tileType)
        {
            case VisualTileType.WALL_E:
                indexOfGrassSpriteInAtlas = 0;
                break;
            case VisualTileType.WALL_S:
                indexOfGrassSpriteInAtlas = 1;
                break;
            case VisualTileType.WALL_N_E_S_W:
                indexOfGrassSpriteInAtlas = 2;
                break;
            case VisualTileType.WALL_W:
                indexOfGrassSpriteInAtlas = 3;
                break;
            case VisualTileType.WALL_E_S:
                indexOfGrassSpriteInAtlas = 4;
                break;
            case VisualTileType.WALL_E_W:
                indexOfGrassSpriteInAtlas = 5;
                break;
            case VisualTileType.WALL_S_W:
                indexOfGrassSpriteInAtlas = 6;
                break;
            case VisualTileType.WALL_E_S_W:
                indexOfGrassSpriteInAtlas = 7;
                break;
            case VisualTileType.WALL_N_S:
                indexOfGrassSpriteInAtlas = 8;
                break;
            case VisualTileType.WALL_N:
                indexOfGrassSpriteInAtlas = 9;
                break;
            case VisualTileType.WALL_NONE:
                indexOfGrassSpriteInAtlas = 10;
                break;
            case VisualTileType.WALL_N_W:
                indexOfGrassSpriteInAtlas = 11;
                break;
            case VisualTileType.WALL_N_E_S:
                indexOfGrassSpriteInAtlas = 12;
                break;
            case VisualTileType.WALL_N_S_W:
                indexOfGrassSpriteInAtlas = 13;
                break;
            case VisualTileType.WALL_N_E_W:
                indexOfGrassSpriteInAtlas = 14;
                break;
            case VisualTileType.WALL_N_E:
                indexOfGrassSpriteInAtlas = 15;
                break;
            case VisualTileType.GROUND:
                indexOfGrassSpriteInAtlas = 16;
                if (UnityEngine.Random.Range(0, 1f) <= 0.33f)
                {
                    indexOfGrassSpriteInAtlas = UnityEngine.Random.Range(17, 19);
                }
                break;
        }

        if (CheckTag(LocationTags.GRASS2) && 
            (tileVisualSet == TileSet.EARTH || tileVisualSet == TileSet.STONE || tileVisualSet == TileSet.SLATE 
            || tileVisualSet == TileSet.TREETOPS || tileVisualSet == TileSet.MOUNTAINGRASS))
        {
            indexOfGrassSpriteInAtlas += 20;
        }

        if (tileVisualSet == TileSet.STONE || tileVisualSet == TileSet.BLUESTONEDARK)
        {
            // Need to fix stone index.
            //indexOfGrassSpriteInAtlas += 20;
        }
        else if (tileVisualSet == TileSet.SAND)
        {
            indexOfGrassSpriteInAtlas += 140;
        }
        else if (tileVisualSet == TileSet.BLUESTONELIGHT)
        {
            indexOfGrassSpriteInAtlas += 160;
        }
        else if (tileVisualSet == TileSet.TREETOPS)
        {
            indexOfGrassSpriteInAtlas += 220;
        }
        else if (tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            indexOfGrassSpriteInAtlas += 260;            
        }
        else if (tileVisualSet == TileSet.SNOW)
        {
            indexOfGrassSpriteInAtlas += 80;
        }
        else if (tileVisualSet == TileSet.FUTURE)
        {
            indexOfGrassSpriteInAtlas += 60;
        }
        else if (tileVisualSet == TileSet.LUSHGREEN)
        {
            indexOfGrassSpriteInAtlas += 80; // Dirt
        }
        else if (tileVisualSet == TileSet.REINFORCED)
        {
            indexOfGrassSpriteInAtlas += 120;
        }
        else if (tileVisualSet == TileSet.MOSS)
        {
            indexOfGrassSpriteInAtlas += 140;
        }
    }

    public void SetTerrainTileType(VisualTileType vtt, LocationTags overrideTag = LocationTags.COUNT)
    {
        visualTerrainTileType = vtt;
        var idx = (int)vtt;
        
        //ground tiles get special randomizer
        if (vtt == VisualTileType.GROUND &&
            UnityEngine.Random.Range(0, 1f) <= 0.33f)
        {
            idx = UnityEngine.Random.Range(17, 20);
        }
        
        if (overrideTag != LocationTags.COUNT)
        {
            overlayIndexOfTerrainSprite = idx;
        }
        else
        {
            indexOfTerrainSpriteInAtlas = idx;
        }
        
       

        if (overrideTag != LocationTags.COUNT)
        {
            if (overrideTag == LocationTags.LAVA)
            {
                overlayIndexOfTerrainSprite += 240;
            }
            else if (overrideTag == LocationTags.MUD || overrideTag == LocationTags.SUMMONEDMUD)
            {
                overlayIndexOfTerrainSprite += 480;
            }
            else if (overrideTag == LocationTags.ELECTRIC)
            {
                overlayIndexOfTerrainSprite += 500;
            }
            else if (overrideTag == LocationTags.LASER) 
            {
                overlayIndexOfTerrainSprite += 940;
            }
            return;
        }

        if (CheckTag(LocationTags.LAVA))
        {
            indexOfTerrainSpriteInAtlas += 240;
        }
        else if ((CheckTag(LocationTags.MUD)) || (CheckTag(LocationTags.SUMMONEDMUD) && overlayIndexOfTerrainSprite < 0))
        {
            indexOfTerrainSpriteInAtlas += 480;
        }
        else if (CheckTag(LocationTags.ELECTRIC))
        {
            indexOfTerrainSpriteInAtlas += 500;
        }
        else if (CheckTag(LocationTags.LASER))
        {
            indexOfTerrainSpriteInAtlas += 940;
        }
    }

    // DEPRECATED
    /* public void ChangeTileSprite(Sprite spr) {
    	return; // Skip this for now
    	tileSprite = spr;
    	char delimiterChar = '_';

    	string[] words = spr.name.Split(delimiterChar);

    	indexOfSpriteInAtlas = System.Int32.Parse(words[1]);

        // Auto decor generator.

        if (Random.Range(0,1f) <= 0.5f)
        {
            indexOfSpriteInAtlas += 40;
        }
    } */

    public MapTileData()
    {
        specialMapObjectsInTile = new bool[(int)SpecialMapObject.COUNT];
        
        pos = new Vector2();
        areaType = AreaTypes.NONE;
        triedForCorridorCreation = false;
        visualTileType = VisualTileType.NOTSET;
        visualGrassTileType = VisualTileType.NOTSET;
        //inherentBlocksVision = true; // By default, tiles block vision since they are walls/"NOTHING"
        dirCheckedForRoomGen = new bool[4];
        dirCheckedForRoomGen[0] = false;
        dirCheckedForRoomGen[1] = false;
        dirCheckedForRoomGen[2] = false;
        dirCheckedForRoomGen[3] = false;

        wallReplacementIndex = -1;
        overlayIndexOfTerrainSprite = -1;

        //tags = new bool[(int)LocationTags.COUNT];
        //mapGenTags = new bool[(int)MapGenerationTags.COUNT];
        //objectFlagQuantities = new short[(int)ObjectFlags.COUNT];
        extraHeightTiles = 0;
        localTileID = globalTileIDCounter;
        globalTileIDCounter++;
    }

    public void RemoveTag(LocationTags remTag)
    {
        tags &= ~(1 << (int) remTag);
        
        //if (!anyLocationTags) return;
        //tags[(int)remTag] = false;
    }

    public void AddMapTag(MapGenerationTags newTag)
    {
        mapGenTags |= 1 << (int)newTag;
        
        /*
        if (!anyMapGenTags)
        {
            mapGenTags = new bool[(int)MapGenerationTags.COUNT];
            anyMapGenTags = true;
        }
        mapGenTags[(int)newTag] = true;
        */
    }

    public void RemoveMapTag(MapGenerationTags newTag)
    {
        mapGenTags &=  ~(1 << (int)newTag);

        /*
        if (!anyMapGenTags)
        {
            return;
        }
        mapGenTags[(int)newTag] = false;
        */
    }

    public bool CheckMapTag(MapGenerationTags newTag)
    {
        return (mapGenTags & (1 << (int)newTag)) > 0;
        
        /*
        if (!anyMapGenTags) return false;
        return mapGenTags[(int)newTag];
        */
        
    }

    public void AddTag(LocationTags newTag)
    {
        //do not set the SOLIDTERRAIN flag if we are a ground tile.
        if (newTag == LocationTags.SOLIDTERRAIN)
        {               
            if (tileType == TileTypes.GROUND)
            {
                return;
            }
        }

        bool bNewTileIsWater = newTag == LocationTags.WATER || newTag == LocationTags.ISLANDSWATER;
        bool bThisTileIsWater = CheckTag(LocationTags.ISLANDSWATER) || CheckTag(LocationTags.WATER);

        bool bThisTileisMud = CheckTag(LocationTags.MUD);
        bool bThisTileIsElectric = CheckTag(LocationTags.ELECTRIC);

        //Mud cannot be changed to water.
        if (CheckTag(LocationTags.MUD) && bNewTileIsWater)
        {
            return;
        }

        //Don't add lava or electric to a water tile
        if (bThisTileIsWater &&
            (newTag == LocationTags.ELECTRIC || newTag == LocationTags.LAVA))
        {
            return;
        }

        if (bNewTileIsWater && newTag == LocationTags.MUD)
        {
            return;
        }

        if (newTag == LocationTags.ELECTRIC && bThisTileisMud)
        {
            return;
        }
        if (newTag == LocationTags.MUD && bThisTileIsElectric)
        {
            return;
        }

        //Don't demote islandwater back into regular water
        if (newTag == LocationTags.WATER && (CheckTag(LocationTags.ISLANDSWATER) ))
        {
            return;
        }

        //But always promote a tile from water to islandwater
        if (newTag == LocationTags.ISLANDSWATER)
        {
            RemoveTag(LocationTags.WATER);
        }

        //Adding water cleans out a few things!
        if (bNewTileIsWater)
        {
            RemoveTag(LocationTags.ELECTRIC);
            RemoveTag(LocationTags.LAVA);
        }

        //Mud and Lava are exclusive, and can't share a tile.
        if (newTag == LocationTags.LAVA)
        {
            RemoveTag(LocationTags.MUD);
        }

        if (newTag == LocationTags.MUD)
        {
            RemoveTag(LocationTags.LAVA);
        }

        //Now the terrain is set, the die is cast!
        tags |= 1 << (int) newTag;
        //tags[(int)newTag] = true;

    }

    public NPC GetInteractableNPC()
    {
        if (!anyActorsInTile) return null;

        foreach(Actor act in actorsInTile)
        {
            if (act.GetActorType() == ActorTypes.NPC)
            {
                NPC n = act as NPC;
                if (n.interactable)
                {
                    return n;
                }
            }
        }
        return null;
    }

    public bool CheckTag(LocationTags tag)
    {
        if (!anyLocationTags) return false;
        if (tag == LocationTags.WATER &&
            CheckTrueTag(LocationTags.WATER))
        {
            return !CheckActorRef("obj_plank");
            }

        return (tags & (1 << (int)tag)) > 0;
    }

    public bool CheckTrueTag(LocationTags tag)
    {
        if (!anyLocationTags) return false;

        return (tags & (1 << (int) tag)) > 0;
    }

    public Actor FindActorByRef(string aRef, out bool anyActor)
    {
        anyActor = false;
        if (!anyActorsInTile) return null;

        foreach(Actor act in actorsInTile)
        {
            if (act.actorRefName == aRef)
            {
                anyActor = true;
                return act;
            }
        }
        return null;
    }

    public void RemoveAllActorsOfRef(string aRef)
    {
        while (RemoveActorByRef(aRef))
        {
            
        }
    }

    public bool IsTileEmptyForItem(bool canSpawnOnNonCollidableDestructibles)
    {
        if (IsCollidable(GameMasterScript.heroPCActor)) return false;
        if (AreItemsInTile()) return false;

        if (canSpawnOnNonCollidableDestructibles)
        {
            return true;
        }
        else
        {
            if (AreItemsOrDestructiblesInTile())
            {
                return false;
            }
        }

        return true;

    }
    public bool RemoveActorByRef(string aRef)
    {
        bool anyActor = false;
        Actor act = FindActorByRef(aRef, out anyActor);
        if (!anyActor || act == null)
        {
            return false;
        }

        return RemoveActor(act);
    }

    public bool CheckAnyMud()
    {
        if (CheckTrueTag(LocationTags.MUD))
        {
            return true;
        }
        if (CheckTrueTag(LocationTags.SUMMONEDMUD))
        {
            if (CheckActorRef("obj_mudtile"))
            {
                return true;
            }
            else
            {
                RemoveTag(LocationTags.SUMMONEDMUD);
            }
        }
        return false;
    }

    public bool IsDangerous(Fighter ft) {
        Monster mn = null;

        bool isMonster = ft.GetActorType() == ActorTypes.MONSTER;
        
        if (CheckTag(LocationTags.ISLANDSWATER) && isMonster)
        {
            // We want to allow wandering through Deadly Void *sometimes*...
            if (UnityEngine.Random.Range(0,1f) <= 0.4f)
            {
                return true;
            }
        }

    	if (isMonster) {
    		mn = ft as Monster;
    		if (mn.CheckAttribute(MonsterAttributes.FLYING) > 0)
            {
    			if (CheckTag(LocationTags.LAVA) || CheckTag(LocationTags.MUD) || CheckTag(LocationTags.ELECTRIC)) {
    				return false;
    			}
    		}
    	}
    	if (CheckTag(LocationTags.LAVA)) {
    		if (isMonster)
            {
    			if (mn.CheckAttribute(MonsterAttributes.LOVESLAVA) > 0)
                {
    				return false;
    			}
    		}
    		return true;
    	}
		if (CheckTag(LocationTags.MUD)) {
    		if (isMonster)
            {
    			if (mn.CheckAttribute(MonsterAttributes.LOVESMUD) > 0) {
    				return false;
    			}
    		}
    		return true;
    	}

        if (CheckTag(LocationTags.ELECTRIC))
        {
            if (isMonster)
            {
                if (mn.CheckAttribute(MonsterAttributes.LOVESELEC) > 0)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
    
    public void SetDecor()
    {
        if (tileType == TileTypes.HOLE) return;
        
        if (tileType == TileTypes.WALL && tileVisualSet == TileSet.STONE) return;

        if (CheckTag(LocationTags.GRASS))
        {
            // Flowers
            indexOfDecorSpriteInAtlas = UnityEngine.Random.Range(0, 10);
        }
        else
        {
            // This corresponds to decor-1
            if (tileType == TileTypes.GROUND)
            {
                indexOfDecorSpriteInAtlas = UnityEngine.Random.Range(0, 55);
            }
            else
            {
                indexOfDecorSpriteInAtlas = UnityEngine.Random.Range(60, 70);
            }
        }

        AddTag(LocationTags.HASDECOR);
    }

    /* public DungeonStuff GetDecor()
    {
        return decor;
    } */

    public void ResetToDefault()
    {
        ChangeTileType(TileTypes.NOTHING, null);
        for (int i = 0; i < (int)LocationTags.COUNT; i++)
        {
            RemoveTag((LocationTags)i);
        }
    }

    /// <summary>
    /// Changes a tile terrain and sets required values that go along with that.
    /// </summary>
    /// <param name="type">Desired terrain type</param>
    /// <param name="mgd">Not used in function, left here to prevent massive project-wide refactor</param>
    public void ChangeTileType(TileTypes type, MapGenerationData mgd = null)
    {
        if (CheckTag(LocationTags.CORRIDORENTRANCE) && type == TileTypes.WALL)
        {
            // Don't ever convert corridor entrances to walls. This is hacky though. TODO.
            return;
        }

        if (tileType == type)
        {
            return;
        }

        tileType = type;
        switch (type)
        {
            // HOLE behaves the same as GROUND, it's just a marker that we use later
            // to make hole textures happen, then flip it back to ground.
            case TileTypes.HOLE:
            case TileTypes.GROUND:
                inherentBlocksVision = false;
				UpdateCollidableState();
                UpdateVisionBlockingState();
                SetTileVisualType(VisualTileType.GROUND);
                RemoveMapTag(MapGenerationTags.NOCONNECTION);

                RemoveTag(LocationTags.SOLIDTERRAIN);
                RemoveTag(LocationTags.TREE);
                break;
            case TileTypes.NOTHING:
            case TileTypes.MAPEDGE:
                inherentBlocksVision = true;
                //tileSprite = mgd.wallTiles[Random.Range(0, mgd.wallTiles.Length)];
                break;
            case TileTypes.WALL:
                if (actorsInTile != null && actorsInTile.Count > 0)
                {
                    tileType = TileTypes.GROUND;
                    /* foreach(Actor act in actorsInTile)
                    {
                        Debug.Log("Hey don't convert " + pos.x + " " + pos.y + " due to " + act.actorRefName + " " + act.GetActorType());
                    } */
                    
                    inherentBlocksVision = false;
                    return;
                }
                inherentBlocksVision = true;
            	UpdateCollidableState();
                break;
        }
    }

    public Actor GetTargetableForMonster()
    {
        // Returns the first possible thing to target, as long as it's a monster.
        if (!anyActorsInTile) return null;

        Actor bestOption = null;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.HERO) return act;
            if ((act.GetActorType() == ActorTypes.MONSTER) && (act.targetable))
            {
                return act;
            }
            if ((act.GetActorType() == ActorTypes.DESTRUCTIBLE) && (act.targetable))
            {
                Destructible dt = act as Destructible;
                if (!dt.isDestroyed)
                {
                    bestOption = dt;
                }
            }
        }
        if (bestOption != null) return bestOption;
        return null;
    }

    public Actor GetMonster()
    {
        if (!anyActorsInTile) return null;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if ((act.GetActorType() == ActorTypes.MONSTER) && (act.targetable))
            {
                return act;
            }
        }
        return null;
    }

    public Actor GetTargetable()
    {
        // Returns the first possible thing to target, as long as it's a monster.
        if (!anyActorsInTile) return null;

        Actor bestOption = null;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.MONSTER && act.targetable)
            {
                return act;
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE && act.targetable)
            {
                Destructible dt = act as Destructible;
                if (!dt.isDestroyed)
                {
                    bestOption = dt;
                }                
            }
        }
        if (bestOption != null) return bestOption;
        return null;
    }

    public bool HasPlayerTargetableDestructibleButNoMonsters()
    {
        if (!anyActorsInTile) return false;

        returnActors.Clear();

        bool hasDT = false;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (!act.targetable)
            {
                continue;
            }
            if (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO)
            {
                Fighter ft = act as Fighter;
                if (ft.myStats.IsAlive())
                {
                    return false;
                }
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (!dt.isDestroyed && dt.targetable && !dt.destroyed)
                {
                    hasDT = true;
                }
            }
        }

        return hasDT;
    }

    // Allows us access to actors that aren't targetable but are destructibles.
    public List<Actor> GetAllTargetablePlusDestructibles()
    {
        if (!anyActorsInTile)
        {
            return Actor.emptyActorList;
        }

        returnActors.Clear();

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.targetable &&
                (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO))
            {
                var ft = act as Fighter;
                if (ft.myStats.IsAlive())
                {
                    returnActors.Add(act); // Don't add dead things.
                }
            }
            else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (!dt.isDestroyed && !dt.destroyed)
                {
                    returnActors.Add(dt);
                }
            }
        }

        return returnActors;
    }

    public Destructible GetRegenFountain()
    {
        if (!anyActorsInTile)
        {
            return null;
        }

        if (actorsInTile.Count == 1) // JUST the player is there.
        {
            return null;
        }

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE)
            {
                continue;
            }
            Destructible dt = act as Destructible;
            if (dt.mapObjType == SpecialMapObject.FOUNTAIN)
            {
                return dt;
            }
        }

        return null;
    }

    public Destructible GetSpecialDestructible(SpecialMapObject smo)
    {
        if (!anyActorsInTile)
        {
            return null;
        }
        if (actorsInTile.Count == 1) // Means JUST the player is there
        {
            return null;
        }        

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE)
            {
                continue;
            }
            Destructible dt = act as Destructible;
            if (dt.mapObjType == smo)
            {
                return dt;
            }
        }
        return null;
    }

    public List<Actor> GetAllTargetable()
    {
        if (!anyActorsInTile) return Actor.emptyActorList;

		returnActors.Clear();

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (!act.targetable || act.bRemovedAndTakeNoActions)
            {
                continue;
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.destroyed || dt.isDestroyed) continue;
            }
            if (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO)
            {
                Fighter ft = act as Fighter;
                if (ft.myStats.IsAlive())
                {
                    returnActors.Add(act); // Don't add dead things.
                }

            }
        }

        return returnActors;
    }

    /* public DoorData GetDoor()
    {
		if (actorsInTile == null) {
        	return null;
        }

        if (actorsInTile.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.DOOR)
            {
                return act as DoorData;
            }
        }

        return null;
    } */

    public void AddActor(Actor actorToAdd)
    {
        Destructible dt = null;

        if (actorsInTile == null) {
        	actorsInTile = new List<Actor>(5);
        	returnActors = new List<Actor>(5);
        }

        if (actorToAdd.GetActorType() == ActorTypes.ITEM) {
        	returnItems = new List<Item>(5);
            numberOfItemsInTile++;
            anyItemsInTile = true;
        }
        else if (actorToAdd.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            dt = actorToAdd as Destructible;
            for (int x = 0; x < dt.objectFlags.Length; x++)
            {
                if (dt.objectFlags[x])
                {
                    if (!anyObjectFlags)
                    {
                        anyObjectFlags = true;
                        objectFlagQuantities = new short[(int)ObjectFlags.COUNT];
                    }
                    //Debug.Log("Adding flag " + (ObjectFlags)x + " to " + pos);
                    objectFlagQuantities[x]++;
                }
            }
            if (actorToAdd.actorRefName == "obj_plank")
            {
                planksInTile = true;
            }
        }
        else
        {
            if (actorToAdd.IsFighter())
            {
                Fighter ft = actorToAdd as Fighter;
                if (!ft.myStats.IsAlive())
                {
                    //Debug.Log("Don't add dead fight " + ft.actorUniqueID + " " + ft.actorRefName + " to " + pos);
                    return;
                }
            }
        }

        switch (actorToAdd.GetActorType())
        {
            case ActorTypes.MONSTER:
            case ActorTypes.HERO:
                actorsInTile.Insert(0, actorToAdd);
                break;
            case ActorTypes.DESTRUCTIBLE:
                actorsInTile.Add(actorToAdd);
                dt = actorToAdd as Destructible;
                if (!dt.destroyed)
                {
                    specialMapObjectsInTile[(int)dt.mapObjType] = true;
                }
                break;
            case ActorTypes.STAIRS:
            case ActorTypes.ITEM:
                actorsInTile.Add(actorToAdd);                
                break;
            default:
                actorsInTile.Add(actorToAdd);
                break;            
        }

        anyActorsInTile = true;

        if (actorToAdd.blocksVision && !actorToAdd.destroyed)
        {
            if (actorToAdd.GetActorType() == ActorTypes.DESTRUCTIBLE && dt.isDestroyed)
            {

            }
            else
            {
                actorBlocksVision = true;
            }            
        }

        if (actorToAdd.monsterCollidable)
        {
            if (actorToAdd.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                dt = actorToAdd as Destructible;
                if (!dt.monsterDestroyable)
                {
                    monCollidable = true;
                }
            }
            else
            {
                monCollidable = true;
            }
        }
        if (actorToAdd.playerCollidable) playerCollidable = true;
    }

    /* public void SetRoomID(int ID)
    {
        roomID = ID;
    } */

    //Check to see if the tile is solid, which would block things like lasers.
    //We are allowed to go through other actors and tiles that block LOS such as clouds of darkness or gas.
    public bool BlocksLineOfEffect()
    {
        return tileType == TileTypes.NOTHING ||
               tileType == TileTypes.MAPEDGE ||
               tileType == TileTypes.WALL && !CheckTag(LocationTags.WATER) ||
               CheckTag(LocationTags.SOLIDTERRAIN);
    }

    public bool BlocksVision(bool treatForcefieldsAsBlocking = false)
    {
        if (inherentBlocksVision)
        {
            return inherentBlocksVision;
        }

        if (treatForcefieldsAsBlocking)
        {
            if (specialMapObjectsInTile[(int)SpecialMapObject.FORCEFIELD])
            {
                return true;
            }
        }


        return actorBlocksVision;
        /*
        if (actorsInTile != null) {
			for (int i = 0; i < actorsInTile.Count; i++)
	        {
	            Actor act = actorsInTile[i];
	            if (act.blocksVision)
	            {
	                return true;
	            }

        	}
        }
        
        return false;
        */
    }

    // Returns TRUE if the tile is inherently collidable (wall, map edge)
    // OR there is any kind of blocking destructible, EVEN if the actor can destroy it
    // This is important so monsters don't jump on tiles with breakable objects
    public bool IsCollidableEvenWithBreakable(Actor checkActor)
    {
        if (tileType == TileTypes.NOTHING || tileType == TileTypes.MAPEDGE || tileType == TileTypes.WALL || CheckTag(LocationTags.SOLIDTERRAIN))
        {
            return true;
        }
        if (actorsInTile == null || actorsInTile.Count == 0) return false;

        foreach(Actor act in actorsInTile)
        {
            if (checkActor.GetActorType() == ActorTypes.MONSTER && act.monsterCollidable)
            {
                return true;
            }
            else if (checkActor.GetActorType() == ActorTypes.HERO && act.playerCollidable)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsCollidable(Actor checkActor)
    {
        if (tileType == TileTypes.NOTHING || tileType == TileTypes.MAPEDGE || tileType == TileTypes.WALL || CheckTag(LocationTags.SOLIDTERRAIN))
        {
            return true;
        }

        return IsCollidableActorInTile(checkActor);
    }

    public float GetHScore(MapTileData target) // try int instead of float?
    {
        if (h != -1f) return h;

        //h = MapMasterScript.GetGridDistance(pos, target.pos);

        //h = Mathf.Max(Math.Abs(pos.x - target.pos.x), Math.Abs(pos.y - target.pos.y));
        h = Mathf.Max(Math.Abs(pos.x - target.pos.x), Math.Abs(pos.y - target.pos.y));

        if (MapMasterScript.dungeonCreativeActive) h *= 3f;
          
        return h;
    }

    public bool IsUnbreakableCollidable(Actor checkActor)
    {
        if (specialMapObjectsInTile[(int)SpecialMapObject.BLOCKER]) return true;

        if (checkActor.GetActorType() == ActorTypes.MONSTER)
        {
            if (checkActor.actorfaction != Faction.PLAYER)
            {
                return monCollidable;
            }
            else
            {
                return playerCollidable;
            }
            
        }

        return playerCollidable;        
    }

    public Destructible GetBreakableCollidableIfNoMonsterTargets(Actor checkAct)
    {
        if (!anyActorsInTile) return null;
        Destructible dt = null;
        for (int i =0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible checkDT = act as Destructible;
                if (checkAct.GetActorType() == ActorTypes.MONSTER)
                {
                    if (checkDT.monsterDestroyable && checkDT.actorfaction != checkAct.actorfaction)
                    {
                        dt = checkDT;
                    }
                }
                else if (checkAct.GetActorType() == ActorTypes.HERO)
                {
                    if (checkDT.actorfaction != Faction.PLAYER && checkDT.targetable 
                        && !checkDT.isDestroyed && !checkDT.destroyed)
                    {
                        dt = checkDT;
                    }
                }

            }
            else if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (mn.actorfaction != checkAct.actorfaction && !mn.isInDeadQueue && !mn.destroyed &&
                    mn.myStats.IsAlive())
                {
                    return null;
                }
            }
        }

        return dt;
    }

    public Destructible GetBreakableCollidable(Actor checkAct)
    {
        if (!anyActorsInTile) return null;
        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (checkAct.GetActorType() == ActorTypes.MONSTER)
                {
                    if (dt.monsterDestroyable && dt.actorfaction != checkAct.actorfaction)
                    {
                        return dt;
                    }
                }
                else if (checkAct.GetActorType() == ActorTypes.HERO)
                {
                    if (dt.actorfaction != Faction.PLAYER && dt.targetable && !dt.isDestroyed && !dt.destroyed)
                    {
                        return dt;
                    }
                }

            }
        }
        return null;
    }

    public Actor GetActorRef(string refName)
    {
        if (!anyActorsInTile) return null;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.actorRefName == refName)
            {
                return act;
            }
        }
        return null;
    }

    public bool CheckActorRef(string refName)
    {
        if (!anyActorsInTile) return false;
        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.actorRefName == refName)
            {
                return true;
            }
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (mn.actorRefName == refName)
                {
                    return true;
                }
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.actorRefName == refName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool HasBreathingPillar()
    {
        if (tileType == TileTypes.WALL || !anyActorsInTile)
        {
            return false;
        }
        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = actorsInTile[i] as Destructible;
                if (dt.mapObjType == SpecialMapObject.BREATHEPILLAR)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsUnbreakableCollidableActorInTile(Actor checkActor)
    {
        if (!anyActorsInTile) return false;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if ((act.GetActorType() == ActorTypes.MONSTER) || (act.GetActorType() == ActorTypes.HERO))
            {
                return true;
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (!dt.monsterDestroyable)
                {
                    if ((checkActor.GetActorType() == ActorTypes.MONSTER) && (dt.monsterCollidable) && (dt.actorfaction != checkActor.actorfaction)) // Try this to prevent Floracondas
                    {
                        return true;
                    }         
                }
            }
        }        
        return false;
    }

    public bool IsCollidableActorInTile(Actor checkActor)
    {
        if (checkActor == null || checkActor.GetActorType() == ActorTypes.MONSTER)
        {
            return monCollidable;
        }

        return playerCollidable;        
    }

    public bool AreItemsInTile()
    {
        if (numberOfItemsInTile > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool AreAliveDestructiblesOfSpecificFactionInTile(Faction destructiblesOfFaction)
    {
        if (!anyActorsInTile) return false;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = actorsInTile[i] as Destructible;
                if (dt.destroyed || dt.isDestroyed) continue;
                if (dt.actorfaction != destructiblesOfFaction) continue;
                return true;
            }
        }    

        return false;    
    }

    public bool AreItemsOrDestructiblesInTile()
    {
        if (!anyActorsInTile) return false;

        if (numberOfItemsInTile > 0)
        {
            return true;
        }
        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = actorsInTile[i] as Destructible;
                if (dt.destroyed || dt.isDestroyed) continue;
                return true;
            }
        }
        return false;
    }

    public List<Item> GetItemsInTile()
    {
        if (!anyActorsInTile)
        {
            return Item.emptyItemList;
        }

    	if (returnItems == null) {
    		return Item.emptyItemList;
    	}

        returnItems.Clear();

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.ITEM)
            {
                Item itm = act as Item;
                returnItems.Add(itm);
            }
        }

        return returnItems;
    }

    public List<Actor> GetPowerupsInTile()
    {
        if (!anyActorsInTile)
        {
            return Actor.emptyActorList;
        }	

        returnActors.Clear();

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType == SpecialMapObject.POWERUP)
                {
                    returnActors.Add(act);
                }                
            }
        }

        return returnActors;
    } 

    public Stairs GetStairsInTile()
    {
        if (!anyActorsInTile) return null;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            Actor act = actorsInTile[i];
            if ((act.GetActorType() == ActorTypes.STAIRS) && (act.actorEnabled))
            {
                return (Stairs)act as Stairs;
            }
        }

        return null;
    }

    public void UpdateCollidableState()
    {
        monCollidable = false;
        playerCollidable = false;
        if (tileType == TileTypes.WALL)
        {
            monCollidable = true;
            playerCollidable = true;
            return;
        }

        bool anyPlayerCollidable = false;
        bool anyMonsterCollidable = false;

        if (!anyActorsInTile || actorsInTile.Count == 0) return;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].playerCollidable)
            {
                playerCollidable = true;
                anyPlayerCollidable = true;
            }
            else
            {
                playerCollidable = false;
            }

            if (actorsInTile[i].monsterCollidable)
            {
                if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = actorsInTile[i] as Destructible;
                    if (!dt.monsterDestroyable)
                    {
                        monCollidable = true;
                        anyMonsterCollidable = true;
                    }
                    else
                    {
                        monCollidable = false;
                    }
                }
                else
                {
                    monCollidable = true;
                    anyMonsterCollidable = true;
                }
            }
        }
        monCollidable = anyMonsterCollidable;
        playerCollidable = anyPlayerCollidable;
    }

    public void UpdateVisionBlockingState()
    {
        actorBlocksVision = false;
        if (tileType == TileTypes.WALL)
        {
            return;
        }

        bool anyBlock = false;

        if (!anyActorsInTile) return;

        for (int i = 0; i < actorsInTile.Count; i++)
        {
            if (actorsInTile[i].blocksVision && !actorsInTile[i].destroyed)
            {
                
                if (actorsInTile[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = actorsInTile[i] as Destructible;
                    if (dt.isDestroyed) continue;
                }
                anyBlock = true;
                break;
            }
        }
        actorBlocksVision = anyBlock; 
    }

    public int GetObjectFlagAmount(ObjectFlags flag)
    {
        if (!anyObjectFlags)
        {
            return 0;
        }
        return objectFlagQuantities[(int)flag];
    }

    public bool RemoveActor(Actor act)
    {
        if (!anyActorsInTile) return false;

        if (!actorsInTile.Contains(act))
        {
            //Debug.Log("That actor " + act.displayName + " isn't in this tile at " + pos.x + "," + pos.y + " is actually in " + act.GetPos());
            return false;
        }

        /* if (GameMasterScript.gameLoadSequenceCompleted)
        {
            Debug.Log(act.actorRefName + " removed from " + pos + " turn " + GameMasterScript.turnNumber + " cpos? " + act.GetPos());
        } */

        if (act.GetActorType() == ActorTypes.ITEM)
        {            
            numberOfItemsInTile--;
            if (numberOfItemsInTile <= 0)
            {
                anyItemsInTile = false;
            }
        }
        else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = act as Destructible;
            for (int x = 0; x < dt.objectFlags.Length; x++)
            {
                if (dt.objectFlags[x])
                {
                    if (!anyObjectFlags)
                    {
                        anyObjectFlags = true;
                        objectFlagQuantities = new short[(int)ObjectFlags.COUNT];
                    }
                    objectFlagQuantities[x]--;
                }
            }
            if (dt.mapObjType != SpecialMapObject.NOTHING)
            {
                bool otherDTHasSameFlag = false;
                foreach(Actor otherAct in actorsInTile)
                {
                    if (otherAct.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                    if (otherAct.actorUniqueID == dt.actorUniqueID) continue;
                    Destructible otherDT = otherAct as Destructible;
                    if (otherDT.mapObjType == dt.mapObjType)
                    {
                        otherDTHasSameFlag = true;
                        break;
                    }
                }
                if (!otherDTHasSameFlag)
                {
                    specialMapObjectsInTile[(int)dt.mapObjType] = false;
                }
            }
        }

        actorsInTile.Remove(act);

        //Debug.Log("Removed " + act.actorRefName + " " + act.actorUniqueID + " from " + pos);

        if (actorsInTile.Count == 0) anyActorsInTile = false;

        UpdateCollidableState();
        UpdateVisionBlockingState();        
        return true;        
    }


    public List<Actor> GetAllActors()
    {
        if (!anyActorsInTile)
        {
            return Actor.emptyActorList;
        }
    	
        return actorsInTile;
    }

    public void SetTagByChar(char c)
    {
        switch (c)
        {
            case '1':
                AddTag(LocationTags.CORRIDOR);
                break;
            case '2':
                AddTag(LocationTags.CORRIDORENTRANCE);
                break;
            /* case '3':
                AddTag(LocationTags.EDGETILE);
                break; */
            /* case '4':
                AddTag(LocationTags.MAPEDGE);
                break; */
            case '5':
                AddTag(LocationTags.DUGOUT);
                break;
            case '6':
                AddTag(LocationTags.HASDECOR);
                break;
            case '7':
                AddTag(LocationTags.WATER);
                break;
            case '8':
                AddTag(LocationTags.LAVA);
                break;
            case '9':
                AddTag(LocationTags.MUD);
                break;
            case 'a':
                AddTag(LocationTags.SECRETENTRANCE);
                break;
            case 'b':
                AddTag(LocationTags.SECRET);
                break;
            case 'c':
                AddTag(LocationTags.SUMMONEDMUD);
                break;
            case 'd':
                AddTag(LocationTags.GRASS);
                break;
            case 'e':
                AddTag(LocationTags.SOLIDTERRAIN);
                break;
            case 'f':
                AddTag(LocationTags.GRASS2);
                break;
            case 'g':
                AddTag(LocationTags.ISLANDSWATER);
                break;
            case 'h':
                AddTag(LocationTags.TREE);
                break;
            case 'i':
                AddTag(LocationTags.ELECTRIC);
                break;
            case 'j':
                AddTag(LocationTags.LASER);
                break;
        }
    }

    public char GetTerrainChar(LocationTags t)
    {
        switch(t)
        {
            case LocationTags.CORRIDOR:
                return '1';
            case LocationTags.CORRIDORENTRANCE:
                return '2';
            /* case LocationTags.EDGETILE:
                return '3'; */
            /* case LocationTags.MAPEDGE:
                return '4'; */
            case LocationTags.DUGOUT:
                return '5';
            case LocationTags.HASDECOR:
                return '6';
            case LocationTags.WATER:
                return '7';
            case LocationTags.LAVA:
                return '8';
            case LocationTags.MUD:
                return '9';
            case LocationTags.SECRETENTRANCE:
                return 'a';
            case LocationTags.SECRET:
                return 'b';
            case LocationTags.SUMMONEDMUD:
                return 'c';
            case LocationTags.GRASS:
                return 'd';
            case LocationTags.SOLIDTERRAIN:
                return 'e';
            case LocationTags.GRASS2:
                return 'f';
            case LocationTags.ISLANDSWATER:
                return 'g';
            case LocationTags.TREE:
                return 'h';
            case LocationTags.ELECTRIC:
                return 'i';
            case LocationTags.LASER:
                return 'j';
        }

        return '0';
    }

    public int CompareTo(MapTileData other)
    {
        return f.CompareTo(other.f);
    }

    public string GetSolidTileReplacement()
    {
        string resource = "";

        if (wallReplacementIndex < 0)
        {
            wallReplacementIndex = GetWallReplacementIndex();
        }

        if (tileVisualSet == TileSet.EARTH || tileVisualSet == TileSet.MOSS)
        {
            resource = MapMasterScript.earthWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.SLATE || tileVisualSet == TileSet.REINFORCED)
        {
            resource = MapMasterScript.slateWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.TREETOPS)
        {
            resource = MapMasterScript.mountainGrassWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            resource = MapMasterScript.mountainGrassWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.LUSHGREEN)
        {
            resource = MapMasterScript.lushGreenWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.STONE || tileVisualSet == TileSet.BLUESTONEDARK || tileVisualSet == TileSet.BLUESTONELIGHT)
        {
            resource = MapMasterScript.stoneWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.COBBLE)
        {
            resource = MapMasterScript.cobbleWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.NIGHTMARISH)
        {
            resource = MapMasterScript.nightmareWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.SNOW)
        {
            resource = MapMasterScript.snowWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.SAND)
        {
            resource = MapMasterScript.desertWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.VOLCANO)
        {
            resource = MapMasterScript.volcanoWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.FUTURE)
        {
            resource = MapMasterScript.futureWallSingleReplacements[wallReplacementIndex];
        }
        else if (tileVisualSet == TileSet.RUINED)
        {
            if (wallReplacementIndex < MapMasterScript.ruinedWallSingleReplacements.Length)
            {
                resource = MapMasterScript.ruinedWallSingleReplacements[wallReplacementIndex];
            }
            else
            {
                resource = MapMasterScript.justTrees[wallReplacementIndex];
            }

        }

        return resource;
    }

    public int GetWallReplacementIndex()
    {
        if (!CheckTag(LocationTags.SOLIDTERRAIN) && !CheckTag(LocationTags.TREE)) return 0;

        int newIndex = 0;
        if (tileVisualSet == TileSet.MOUNTAINGRASS)
        {
            return UnityEngine.Random.Range(0, MapMasterScript.mountainGrassWallSingleReplacements.Length);
            
        }
        if (CheckTag(LocationTags.TREE))
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.justTrees.Length);
            return newIndex;
        }
        if (tileVisualSet == TileSet.SLATE || tileVisualSet == TileSet.REINFORCED)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.slateWallSingleReplacements.Length);
        }
        else if (tileVisualSet == TileSet.LUSHGREEN)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.lushGreenWallSingleReplacements.Length);
        }
        else if ((tileVisualSet == TileSet.EARTH) || (tileVisualSet == TileSet.MOSS))
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.earthWallSingleReplacements.Length);
        }
        else if (tileVisualSet == TileSet.SNOW)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.snowWallSingleReplacements.Length);
        }
        else if ((tileVisualSet == TileSet.STONE) || (tileVisualSet == TileSet.BLUESTONEDARK) || (tileVisualSet == TileSet.BLUESTONELIGHT))
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.stoneWallSingleReplacements.Length);
        }
        else if (tileVisualSet == TileSet.COBBLE)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.cobbleWallSingleReplacements.Length);
        }
        else if (tileVisualSet == TileSet.NIGHTMARISH)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.nightmareWallSingleReplacements.Length);
        }
        else if (tileVisualSet == TileSet.FUTURE)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.futureWallSingleReplacements.Length);
        }
        /* else if (tileVisualSet == TileSet.RUINED)
        {
            if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.singletonMMS.chance1xWallReplace)
            {
                AddTag(LocationTags.SOLIDTERRAIN);
                wallReplacementIndex = UnityEngine.Random.Range(0, MapMasterScript.ruinedWallSingleReplacements.Length);
                indexOfSpriteInAtlas = 16;
            }
        } */

        else if (tileVisualSet == TileSet.VOLCANO)
        {
            newIndex = UnityEngine.Random.Range(0, MapMasterScript.volcanoWallSingleReplacements.Length);
        }

        return newIndex;
    }

    public bool HasImpassableDestructible(Actor act)
    {
        if (!anyActorsInTile) return false;

        foreach(Actor checkAct in actorsInTile)
        {
            if (checkAct.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            Destructible dt = checkAct as Destructible;
            if (act.actorfaction == Faction.PLAYER)
            {
                if (dt.playerCollidable && !dt.targetable && !dt.destroyed) return true;
            }
            if (act.actorfaction == Faction.ENEMY)
            {
                if (dt.monsterCollidable && !dt.monsterDestroyable && !dt.destroyed && !dt.isDestroyed) return true;
            }
        }
        return false;
    }

    public bool IsTerrain()
    {
        return CheckTag(LocationTags.WATER) || CheckTag(LocationTags.MUD) || CheckTag(LocationTags.LAVA);
    }
}
