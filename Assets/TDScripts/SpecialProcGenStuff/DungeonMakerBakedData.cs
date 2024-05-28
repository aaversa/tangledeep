using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EDungeonSizes { TINY, SMALL, MEDIUM, LARGE, HUGE, COUNT };
public enum EDungeonFeatures { STUMPS, FUNGUS, LAVAPOOLS, RIVERS, COUNT }

// We can either keep these or not
public enum EMysteryDungeonPlayerResources { GEAR, ITEMS, FLASK, STATS, SKILLS, MONEY, RELICS, PET, COUNT }

public partial class DungeonMaker
{
    public static Dictionary<EDungeonSizes, int> possibleMapSizeValues;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> roomCountsByLayout;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> maxCorridorsByLayout;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> maxCorridorLengthByLayout;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> deadendsByLayout;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> secretAreasByLayout;
    public static Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>> baseMonsterDensityByLayout;
    public static Dictionary<EDungeonFeatures, Dictionary<TileSet, float>> chanceOfDungeonFeaturesByTileset;
    public static ActorTable dungeonSizeProbability;
    public static ActorTable layoutTypeProbability;

    public static Dictionary<string, MysteryDungeon> masterMysteryDungeonDict;

    public static HashSet<string> disallowedItemsInMysteryDungeons = new HashSet<string>()
    {
        "spice_rosepetals",
        "item_tent",
        "item_dreamdrum",
        "orb_itemworld",
        "seeds_tree1",
        "seeds_tree2",
        "seeds_tree3",
        "seeds_tree4",
        "spice_staranise",
        "spice_nutmeg",
        "spice_garlic",
        "spice_cinnamon",
        "spice_cilantro",
        "food_monsterchow",
        "item_monstermallet" // maybe? allow this for pets
    };

    static void SetDungeonTemplateData()
    {
        CreateBakedMysteryDungeonData();

        dungeonSizeProbability = new ActorTable();
        dungeonSizeProbability.AddToTable(EDungeonSizes.TINY.ToString(), 50);
        dungeonSizeProbability.AddToTable(EDungeonSizes.SMALL.ToString(), 80);
        dungeonSizeProbability.AddToTable(EDungeonSizes.MEDIUM.ToString(), 100);
        dungeonSizeProbability.AddToTable(EDungeonSizes.LARGE.ToString(), 80);
        dungeonSizeProbability.AddToTable(EDungeonSizes.HUGE.ToString(), 50);

        layoutTypeProbability = new ActorTable();
        layoutTypeProbability.AddToTable(ECustomDungeonLayouts.AUTOCAVE.ToString(), 100);
        layoutTypeProbability.AddToTable(ECustomDungeonLayouts.BSPROOMS.ToString(), 50);
        layoutTypeProbability.AddToTable(ECustomDungeonLayouts.STANDARD.ToString(), 150);
        layoutTypeProbability.AddToTable(ECustomDungeonLayouts.STANDARDNOHALLS.ToString(), 100);

        chanceOfDungeonFeaturesByTileset = new Dictionary<EDungeonFeatures, Dictionary<TileSet, float>>()
        {
            { EDungeonFeatures.STUMPS, new Dictionary<TileSet, float>()
            {
                { TileSet.EARTH, 0.035f },
                { TileSet.TREETOPS, 0.1f },
                { TileSet.MOUNTAINGRASS, 0.04f },
                { TileSet.MOSS, 0.1f },
                { TileSet.LUSHGREEN, 0.075f },
                { TileSet.SLATE, 0.075f },
                { TileSet.SNOW, 0.05f },
            }
            },
            { EDungeonFeatures.FUNGUS, new Dictionary<TileSet, float>()
            {
                { TileSet.EARTH, 0.01f },
                { TileSet.MOSS, 0.15f },
                { TileSet.LUSHGREEN, 0.05f },
                { TileSet.SLATE, 0.05f },
            }
            }
        };

        possibleMapSizeValues = new Dictionary<EDungeonSizes, int>()
        {
            { EDungeonSizes.TINY, 26 },
            { EDungeonSizes.SMALL, 34 },
            { EDungeonSizes.MEDIUM, 42 },
            { EDungeonSizes.LARGE, 50 },
            { EDungeonSizes.HUGE, 50 }
        };

        // Point: x to y is num monster range
        baseMonsterDensityByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(5,7) },
                    { EDungeonSizes.SMALL, new Point(7,9) },
                    { EDungeonSizes.MEDIUM, new Point(9,11) },
                    { EDungeonSizes.LARGE, new Point(11,14) },
                    { EDungeonSizes.HUGE, new Point(13,16) }
                }
            },
            { ECustomDungeonLayouts.BSPROOMS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(5,7) },
                    { EDungeonSizes.SMALL, new Point(7,9) },
                    { EDungeonSizes.MEDIUM, new Point(8,10) },
                    { EDungeonSizes.LARGE, new Point(10,12) },
                    { EDungeonSizes.HUGE, new Point(12,14) }
                }
            },
            { ECustomDungeonLayouts.STANDARDNOHALLS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(5,7) },
                    { EDungeonSizes.SMALL, new Point(7,9) },
                    { EDungeonSizes.MEDIUM, new Point(9,11) },
                    { EDungeonSizes.LARGE, new Point(11,14) },
                    { EDungeonSizes.HUGE, new Point(13,16) }
                }
            },
            { ECustomDungeonLayouts.AUTOCAVE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,5) },
                    { EDungeonSizes.SMALL, new Point(6,7) },
                    { EDungeonSizes.MEDIUM, new Point(8,9) },
                    { EDungeonSizes.LARGE, new Point(11,13) },
                    { EDungeonSizes.HUGE, new Point(13,16) }
                }
            },
            { ECustomDungeonLayouts.VOLCANO, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,5) },
                    { EDungeonSizes.SMALL, new Point(6,7) },
                    { EDungeonSizes.MEDIUM, new Point(8,9) },
                    { EDungeonSizes.LARGE, new Point(11,13) },
                    { EDungeonSizes.HUGE, new Point(13,16) }
                }
            },
            { ECustomDungeonLayouts.CAVE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,5) },
                    { EDungeonSizes.SMALL, new Point(6,7) },
                    { EDungeonSizes.MEDIUM, new Point(7,8) },
                    { EDungeonSizes.LARGE, new Point(11,13) },
                    { EDungeonSizes.HUGE, new Point(12,15) }
                }
            },
            { ECustomDungeonLayouts.LAKE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,5) },
                    { EDungeonSizes.SMALL, new Point(6,7) },
                    { EDungeonSizes.MEDIUM, new Point(7,8) },
                    { EDungeonSizes.LARGE, new Point(11,13) },
                    { EDungeonSizes.HUGE, new Point(12,15) }
                }
            },
            { ECustomDungeonLayouts.MAZE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,5) },
                    { EDungeonSizes.SMALL, new Point(6,7) },
                    { EDungeonSizes.MEDIUM, new Point(7,8) },
                    { EDungeonSizes.LARGE, new Point(11,13) },
                    { EDungeonSizes.HUGE, new Point(12,15) }
                }
            },
            { ECustomDungeonLayouts.MAZEROOMS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(5,6) },
                    { EDungeonSizes.SMALL, new Point(7,8) },
                    { EDungeonSizes.MEDIUM, new Point(8,9) },
                    { EDungeonSizes.LARGE, new Point(12,14) },
                    { EDungeonSizes.HUGE, new Point(14,16) }
                }
            },
            { ECustomDungeonLayouts.CAVEROOMS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(5,6) },
                    { EDungeonSizes.SMALL, new Point(7,8) },
                    { EDungeonSizes.MEDIUM, new Point(8,9) },
                    { EDungeonSizes.LARGE, new Point(12,14) },
                    { EDungeonSizes.HUGE, new Point(14,16) }
                }
            }
        };

        // Point: x to y is secret area range
        secretAreasByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(0,1) },
                    { EDungeonSizes.SMALL, new Point(0,2) },
                    { EDungeonSizes.MEDIUM, new Point(1,2) },
                    { EDungeonSizes.LARGE, new Point(1,3) },
                    { EDungeonSizes.HUGE, new Point(2,4) }
                }
            },
            { ECustomDungeonLayouts.STANDARDNOHALLS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(0,1) },
                    { EDungeonSizes.SMALL, new Point(0,3) },
                    { EDungeonSizes.MEDIUM, new Point(1,3) },
                    { EDungeonSizes.LARGE, new Point(2,4) },
                    { EDungeonSizes.HUGE, new Point(2,5) }
                }
            }
        };

        // Point: x to y is minroom range
        roomCountsByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(8,13) },
                    { EDungeonSizes.SMALL, new Point(8,18) },
                    { EDungeonSizes.MEDIUM, new Point(9,20) },
                    { EDungeonSizes.LARGE, new Point(9,24) },
                    { EDungeonSizes.HUGE, new Point(10,28) }
                }
            },
            { ECustomDungeonLayouts.STANDARDNOHALLS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(10,15) },
                    { EDungeonSizes.SMALL, new Point(10,24) },
                    { EDungeonSizes.MEDIUM, new Point(12,28) },
                    { EDungeonSizes.LARGE, new Point(12,32) },
                    { EDungeonSizes.HUGE, new Point(13,38) }
                }
            },
            { ECustomDungeonLayouts.CAVE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(11,13) },
                    { EDungeonSizes.SMALL, new Point(14,18) },
                    { EDungeonSizes.MEDIUM, new Point(17,20) },
                    { EDungeonSizes.LARGE, new Point(21,25) },
                    { EDungeonSizes.HUGE, new Point(26,31) }
                }
            },
            { ECustomDungeonLayouts.LAKE, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(8,13) },
                    { EDungeonSizes.SMALL, new Point(8,18) },
                    { EDungeonSizes.MEDIUM, new Point(9,20) },
                    { EDungeonSizes.LARGE, new Point(9,24) },
                    { EDungeonSizes.HUGE, new Point(10,28) }
                }
            },
            { ECustomDungeonLayouts.CAVEROOMS, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(4,8) },
                    { EDungeonSizes.SMALL, new Point(5,10) },
                    { EDungeonSizes.MEDIUM, new Point(6,12) },
                    { EDungeonSizes.LARGE, new Point(7,13) },
                    { EDungeonSizes.HUGE, new Point(7,15) }
                }
            }
        };

        // Point: x to y is deadend range
        deadendsByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(0,10) },
                    { EDungeonSizes.SMALL, new Point(1,13) },
                    { EDungeonSizes.MEDIUM, new Point(3,18) },
                    { EDungeonSizes.LARGE, new Point(5,24) },
                    { EDungeonSizes.HUGE, new Point(7,30) }
                }
            }
        };

        // Point: x to y is maxCorridor range
        maxCorridorsByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(1,6) },
                    { EDungeonSizes.SMALL, new Point(1,9) },
                    { EDungeonSizes.MEDIUM, new Point(1,12) },
                    { EDungeonSizes.LARGE, new Point(2,12) },
                    { EDungeonSizes.HUGE, new Point(3,15) },
                }
            }
        };

        // Point: x to y is maxCorridor range
        maxCorridorLengthByLayout = new Dictionary<ECustomDungeonLayouts, Dictionary<EDungeonSizes, Point>>()
        {
            { ECustomDungeonLayouts.STANDARD, new Dictionary<EDungeonSizes, Point>()
                {
                    { EDungeonSizes.TINY, new Point(7,10) },
                    { EDungeonSizes.SMALL, new Point(3,10) },
                    { EDungeonSizes.MEDIUM, new Point(3,12) },
                    { EDungeonSizes.LARGE, new Point(4,14) },
                    { EDungeonSizes.HUGE, new Point(5,16) },
                }
            }
        };
    }

    static void CreateBakedMysteryDungeonData()
    {
        masterMysteryDungeonDict = new Dictionary<string, MysteryDungeon>();
        
        // Tropical Trove - find lots of consumables

        MysteryDungeon md = new MysteryDungeon("dungeon1");
        md.minScaledRelicValueMultiplier = 0.5f;
        md.maxScaledRelicValueMultiplier = 0.8f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon1_name");
        md.floors = 15;
        md.dungeonLayouts.AddToTable("AUTOCAVE", 125);
        md.dungeonLayouts.AddToTable("CAVE", 70);
        md.dungeonLayouts.AddToTable("BSPROOMS", 70);
        md.dungeonLayouts.AddToTable("LAKE", 85);
        md.dungeonLayouts.AddToTable("MAZEROOMS", 50);
        md.dungeonLayouts.AddToTable("MAZE", 50);

        md.dungeonTileSets.AddToTable("SAND", 225);
        md.dungeonTileSets.AddToTable("TREETOPS", 100);
        md.dungeonTileSets.AddToTable("MOSS", 50);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;

        md.monsterFamilies.AddToTable("insects", 125);
        md.monsterFamilies.AddToTable("snakes", 75);
        md.monsterFamilies.AddToTable("beasts", 150);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 50);
        md.monsterFamilies.AddToTable("frogs", 25);
        md.monsterFamilies.AddToTable("bandits", 125);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.localChanceSpawnShopkeeperOnFloor = 0.33f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.5f;

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.2f },
            { 5, 1.25f },
            { 6, 1.3f },
            { 7, 1.35f },
            { 8, 1.4f },
            { 9, 1.45f },
            { 10, 1.5f },
            { 11, 1.55f },
            { 12, 1.6f },
            { 13, 1.65f },
            { 14, 1.7f }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 3 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 3 },
            { 8, 3 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
        };

        md.AddGimmick(MysteryGimmicks.NO_JP_GAIN);
        md.AddGimmick(MysteryGimmicks.JP_CAMPFIRES);
        md.AddGimmick(MysteryGimmicks.EXTRA_CONSUMABLES);
        md.AddGimmick(MysteryGimmicks.HIGHER_MERCHANT_VALUE);

        masterMysteryDungeonDict.Add(md.refName, md);

        // Temple of Elements - hard as hell

        md = new MysteryDungeon("dungeon2");
        md.minScaledRelicValueMultiplier = 0.7f;
        md.maxScaledRelicValueMultiplier = 1.0f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon2_name");
        md.floors = 30;
        md.dungeonLayouts.AddToTable("STANDARD", 100);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 75);
        md.dungeonLayouts.AddToTable("BSPROOMS", 75);
        md.dungeonLayouts.AddToTable("AUTOCAVE", 125);
        md.dungeonLayouts.AddToTable("CAVE", 70);
        md.dungeonLayouts.AddToTable("MAZE", 70);
        md.dungeonLayouts.AddToTable("MAZEROOMS", 70);

        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS] = true;

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.FLASK] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;

        md.dungeonTileSets.AddToTable("SLATE", 100);
        md.dungeonTileSets.AddToTable("EARTH", 100);
        md.dungeonTileSets.AddToTable("SNOW", 100);
        md.dungeonTileSets.AddToTable("SAND", 100);
        md.dungeonTileSets.AddToTable("NIGHTMARISH", 100);
        md.dungeonTileSets.AddToTable("VOLCANO", 100);
        md.dungeonTileSets.AddToTable("MOSS", 100);

        md.monsterFamilies.AddToTable("insects", 50);
        md.monsterFamilies.AddToTable("snakes", 50);
        md.monsterFamilies.AddToTable("beasts", 100);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 150);
        md.monsterFamilies.AddToTable("bandits", 50);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.floorsToCV = new Dictionary<int, float>();

        for (int i = 0; i < md.floors; i++)
        {
            md.floorsToCV[i] = 1.0f + (i * 0.037f);
        }

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 3 },
            { 14, 4 },
            { 15, 4 },
            { 16, 4 },
            { 17, 4 },
            { 18, 5 },
            { 19, 5 },
            { 20, 5 },
            { 21, 5 },
            { 22, 5 },
            { 23, 5 },
            { 24, 5 },
            { 25, 5 },
            { 26, 5 },
            { 27, 5 },
            { 28, 5 },
            { 29, 5 },
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 2 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 3 },
            { 14, 4 },
            { 15, 4 },
            { 16, 4 },
            { 17, 4 },
            { 18, 4 },
            { 19, 5 },
            { 20, 5 },
            { 21, 5 },
            { 22, 5 },
            { 23, 5 },
            { 24, 5 },
            { 25, 5 },
            { 26, 5 },
            { 27, 5 },
            { 28, 5 },
            { 29, 5 },
        };

        md.localChanceSpawnShopkeeperOnFloor = 0.25f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.4f;

        // GOLDFROG GROTTO

        md = new MysteryDungeon("dungeon3");
        md.minScaledRelicValueMultiplier = 0.45f;
        md.maxScaledRelicValueMultiplier = 0.75f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon3_name");
        md.floors = 15;
        md.dungeonLayouts.AddToTable("AUTOCAVE", 125);
        md.dungeonLayouts.AddToTable("CAVE", 75);
        md.dungeonLayouts.AddToTable("MAZEROOMS", 75);
        md.dungeonLayouts.AddToTable("VOLCANO", 30);
        md.dungeonLayouts.AddToTable("LAKE", 55);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;

        md.scriptsOnPostBuild.Add("SpawnGoldfrogPerFloor");
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");
        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";

        md.localChanceSpawnShopkeeperOnFloor = 1.0f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.33f;

        md.AddGimmick(MysteryGimmicks.NO_ITEM_DROPS);
        md.AddGimmick(MysteryGimmicks.HIGHER_MERCHANT_VALUE);

        md.dungeonTileSets.AddToTable("TREETOPS", 100);
        md.dungeonTileSets.AddToTable("LUSHGREEN", 100);
        md.dungeonTileSets.AddToTable("MOSS", 100);

        md.monsterFamilies.AddToTable("insects", 100);
        md.monsterFamilies.AddToTable("snakes", 75);
        md.monsterFamilies.AddToTable("beasts", 75);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 100);
        md.monsterFamilies.AddToTable("frogs", 100);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.2f },
            { 5, 1.25f },
            { 6, 1.3f },
            { 7, 1.35f },
            { 8, 1.4f },
            { 9, 1.45f },
            { 10, 1.5f },
            { 11, 1.5f },
            { 12, 1.55f },
            { 13, 1.6f },
            { 14, 1.65f },
            { 15, 1.7f },
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 4 },
            { 13, 4 },
            { 14, 4 },
            { 15, 5 }            
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
            { 15, 4 }
        };

        // GUARDIAN ANCIENT RUINS

        md = new MysteryDungeon("dungeon4");
        md.minScaledRelicValueMultiplier = 0.5f;
        md.maxScaledRelicValueMultiplier = 0.9f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon4_name");
        md.floors = 15;
        md.dungeonLayouts.AddToTable("STANDARD", 200);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 70);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 70);
        md.dungeonLayouts.AddToTable("BSPROOMS", 60);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;

        md.dungeonTileSets.AddToTable("RUINED", 100);
        md.dungeonTileSets.AddToTable("FUTURE", 100);

        md.monsterFamilies.AddToTable("spirits", 60);
        md.monsterFamilies.AddToTable("bandits", 150);
        md.monsterFamilies.AddToTable("robots", 200);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.2f },
            { 5, 1.25f },
            { 6, 1.3f },
            { 7, 1.35f },
            { 8, 1.4f },
            { 9, 1.4f },
            { 10, 1.45f },
            { 11, 1.5f },
            { 12, 1.55f },
            { 13, 1.6f },
            { 14, 1.65f },
            { 15, 1.75f },
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 4 },
            { 13, 4 },
            { 14, 4 },
            { 15, 5 }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
            { 15, 4 }
        };

        md.localChanceSpawnShopkeeperOnFloor = 0.33f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.6f;

        // HALL OF CHAMPIONS

        md = new MysteryDungeon("dungeon5");
        md.minScaledRelicValueMultiplier = 0.6f;
        md.maxScaledRelicValueMultiplier = 0.95f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon5_name");
        md.floors = 12;
        md.dungeonLayouts.AddToTable("MAZE", 100);
        md.dungeonLayouts.AddToTable("MAZEROOMS", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 100);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);

        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.FLASK] = false;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.MONEY] = false;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.PET] = false;

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        //md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR] = true;

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("EnforceMinimumNumberOfChampions");
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.localChanceSpawnShopkeeperOnFloor = 0.5f;
        md.monstersRespawn = false;

        md.AddGimmick(MysteryGimmicks.MANY_CHAMPIONS);
        //md.AddGimmick(MysteryGimmicks.EXTRA_CONSUMABLES);

        md.dungeonTileSets.AddToTable("BLUESTONEDARK", 100);
        md.dungeonTileSets.AddToTable("BLUESTONELIGHT", 100);
        md.dungeonTileSets.AddToTable("COBBLE", 100);
        md.dungeonTileSets.AddToTable("REINFORCED", 100);
        md.dungeonTileSets.AddToTable("STONE", 100);

        md.monsterFamilies.AddToTable("insects", 50);
        md.monsterFamilies.AddToTable("snakes", 30);
        md.monsterFamilies.AddToTable("beasts", 100);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 100);
        md.monsterFamilies.AddToTable("frogs", 40);
        md.monsterFamilies.AddToTable("bandits", 100);
        md.monsterFamilies.AddToTable("robots", 100);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.1f },
            { 1, 1.2f },
            { 2, 1.3f },
            { 3, 1.4f },
            { 4, 1.5f },
            { 5, 1.6f },
            { 6, 1.7f },
            { 7, 1.8f },
            { 8, 1.9f },
            { 9, 2.0f },
            { 10, 2.1f },
            { 11, 2.2f }
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 2 },
            { 1, 2 },
            { 2, 2 },
            { 3, 3 },
            { 4, 3 },
            { 5, 3 },
            { 6, 4 },
            { 7, 4 },
            { 8, 4 },
            { 9, 5 },
            { 10, 5 },
            { 11, 5 }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 5 },
            { 1, 5 },
            { 2, 5 },
            { 3, 6 },
            { 4, 6 },
            { 5, 6 },
            { 6, 7 },
            { 7, 7 },
            { 8, 7 },
            { 9, 8 },
            { 10, 8 },
            { 11, 8 }
        };

        // TOWER OF ORDEALS

        md = new MysteryDungeon("dungeon6");
        md.minScaledRelicValueMultiplier = 0.8f;
        md.maxScaledRelicValueMultiplier = 1.2f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon6_name");
        md.floors = 50;
        //md.dungeonLayouts.AddToTable("MAZE", 75);
        md.dungeonLayouts.AddToTable("MAZEROOMS", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 75);
        md.dungeonLayouts.AddToTable("STANDARD", 200);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);
        md.dungeonLayouts.AddToTable("CAVE", 100);
        md.dungeonLayouts.AddToTable("AUTOCAVE", 100);
        md.dungeonLayouts.AddToTable("LAKE", 75);
        md.dungeonLayouts.AddToTable("VOLCANO", 75);
        md.dungeonLayouts.AddToTable("BSPROOMS", 75);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.localChanceSpawnShopkeeperOnFloor = 0.25f;
        md.monstersRespawn = true;

        md.dungeonTileSets.AddToTable("BLUESTONEDARK", 100);
        md.dungeonTileSets.AddToTable("BLUESTONELIGHT", 100);
        md.dungeonTileSets.AddToTable("COBBLE", 100);
        md.dungeonTileSets.AddToTable("REINFORCED", 100);
        md.dungeonTileSets.AddToTable("STONE", 100);

        md.dungeonTileSets.AddToTable("NIGHTMARISH", 100);
        md.dungeonTileSets.AddToTable("VOLCANO", 100);
        md.dungeonTileSets.AddToTable("SLATE", 100);
        md.dungeonTileSets.AddToTable("EARTH", 100);
        md.dungeonTileSets.AddToTable("TREETOPS", 100);
        md.dungeonTileSets.AddToTable("LUSHGREEN", 100);
        md.dungeonTileSets.AddToTable("MOSS", 100);
        md.dungeonTileSets.AddToTable("VOID", 100);

        md.monsterFamilies.AddToTable("insects", 50);
        md.monsterFamilies.AddToTable("snakes", 30);
        md.monsterFamilies.AddToTable("beasts", 120);
        md.monsterFamilies.AddToTable("hybrids", 65);
        md.monsterFamilies.AddToTable("spirits", 100);
        md.monsterFamilies.AddToTable("frogs", 50);
        md.monsterFamilies.AddToTable("bandits", 120);
        md.monsterFamilies.AddToTable("robots", 90);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.floorsToCV = new Dictionary<int, float>();
        md.championModsByFloor = new Dictionary<int, int>();
        md.championsByFloor = new Dictionary<int, int>();

        for (int i = 0; i < 50; i++)
        {
            //float targetCV = 1.0f + ((i - 1) * 0.026f);

            float targetCV = 1f;

            if (i < 10)
            {
                targetCV = 1.0f + ((i - 1) * 0.034f);
            }
            else if (i >= 10 && i < 20)
            {
                targetCV = 1.0f + (9 * 0.034f); // 1.306
                targetCV += ((i-9) * 0.03f); // Up to 0.3
            }
            else if (i >= 20 && i < 30)
            {
                targetCV = 1.0f + (9 * 0.034f); // 1.306
                targetCV += (10 * 0.03f); // 0.3
                targetCV += ((i - 19) * 0.024f); // up to 0.24
            }
            else
            {
                targetCV = 1.0f + (9 * 0.034f); // 1.306
                targetCV += (10 * 0.03f); // 0.3
                targetCV += (10 * 0.024f); // 0.224, for a total of 1.83
                targetCV += ((i - 29) * 0.022f); // up to 0.24
            }

            if (targetCV >= GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION)
            {
                targetCV = GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION;
            }
            md.floorsToCV.Add(i, targetCV);

            int targetChampions = 1 + (i / 10);
            if (i >= 10) targetChampions++;
            if (i >= 30) targetChampions++;

            int targetChampionMods = 1;
            if (i >= 9) targetChampionMods++;
            if (i >= 21) targetChampionMods++;
            if (i >= 34) targetChampionMods++;

            md.championsByFloor.Add(i, targetChampions);
            md.championModsByFloor.Add(i, targetChampionMods);
        }

        // CHASER DUNGEON

        md = new MysteryDungeon("dungeon7");
        md.displayName = StringManager.GetString("exp_wanderer_dungeon7_name");
        md.floors = 15;
        md.minScaledRelicValueMultiplier = 0.55f;
        md.maxScaledRelicValueMultiplier = 1f;
        md.dungeonLayouts.AddToTable("MAZEROOMS", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 75);
        md.dungeonLayouts.AddToTable("STANDARD", 200);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);
        md.dungeonLayouts.AddToTable("CAVE", 100);
        md.dungeonLayouts.AddToTable("AUTOCAVE", 100);
        md.dungeonLayouts.AddToTable("BSPROOMS", 75);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.AddGimmick(MysteryGimmicks.MONSTER_CHASER);

        md.localChanceSpawnShopkeeperOnFloor = 0.45f;
        md.monstersRespawn = true;

        md.dungeonTileSets.AddToTable("NIGHTMARISH", 100);
        md.dungeonTileSets.AddToTable("VOLCANO", 100);
        md.dungeonTileSets.AddToTable("EARTH", 100);
        md.dungeonTileSets.AddToTable("SNOW", 100);

        md.monsterFamilies.AddToTable("insects", 50);
        md.monsterFamilies.AddToTable("beasts", 120);
        md.monsterFamilies.AddToTable("hybrids", 65);
        md.monsterFamilies.AddToTable("spirits", 80);
        md.monsterFamilies.AddToTable("bandits", 160);
        md.monsterFamilies.AddToTable("robots", 160);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.floorsToCV = new Dictionary<int, float>();
        md.championModsByFloor = new Dictionary<int, int>();
        md.championsByFloor = new Dictionary<int, int>();

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.25f },
            { 5, 1.3f },
            { 6, 1.35f },
            { 7, 1.4f },
            { 8, 1.4f },
            { 9, 1.45f },
            { 10, 1.5f },
            { 11, 1.55f },
            { 12, 1.6f },
            { 13, 1.65f },
            { 14, 1.7f },
            { 15, 1.75f },
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 4 },
            { 13, 4 },
            { 14, 4 },
            { 15, 5 }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
            { 15, 4 }
        };

        // FACILITY ESCAPE FACTORY FREE FOR ALL 

        md = new MysteryDungeon("dungeon8");
        md.displayName = StringManager.GetString("exp_wanderer_dungeon8_name");
        md.floors = 15;
        md.minScaledRelicValueMultiplier = 0.6f;
        md.maxScaledRelicValueMultiplier = 0.95f;
        md.dungeonLayouts.AddToTable("MAZEROOMS", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 100);
        md.dungeonLayouts.AddToTable("STANDARD", 100);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);
        md.dungeonLayouts.AddToTable("BSPROOMS", 100);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        //md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.PET] = true;
        //md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.PET] = true;

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.AddGimmick(MysteryGimmicks.MONSTER_INFIGHTING_AND_LEVELUP);

        md.localChanceSpawnShopkeeperOnFloor = 0.5f;

        md.dungeonTileSets.AddToTable("FUTURE", 100);
        md.dungeonTileSets.AddToTable("RUINED", 50);
        md.dungeonTileSets.AddToTable("BLUESTONELIGHT", 100);
        md.dungeonTileSets.AddToTable("BLUESTONEDARK", 100);

        md.monsterFamilies.AddToTable("spirits", 50);
        md.monsterFamilies.AddToTable("bandits", 125);
        md.monsterFamilies.AddToTable("robots", 125);

        masterMysteryDungeonDict.Add(md.refName, md);

        md.monstersRespawn = true;

        md.floorsToCV = new Dictionary<int, float>();
        md.championModsByFloor = new Dictionary<int, int>();
        md.championsByFloor = new Dictionary<int, int>();

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.25f },
            { 5, 1.3f },
            { 6, 1.35f },
            { 7, 1.4f },
            { 8, 1.4f },
            { 9, 1.45f },
            { 10, 1.5f },
            { 11, 1.55f },
            { 12, 1.6f },
            { 13, 1.65f },
            { 14, 1.7f },
            { 15, 1.75f },
        };

        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 4 },
            { 13, 4 },
            { 14, 4 },
            { 15, 5 }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 2 },
            { 1, 2 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 4 },
            { 14, 4 },
            { 15, 4 }
        };

        // MOUNTAIN ASCENT

        md = new MysteryDungeon("dungeon9");
        md.minScaledRelicValueMultiplier = 0.6f;
        md.maxScaledRelicValueMultiplier = 0.9f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon9_name");
        int numFloors = 10;
        md.floors = numFloors;
        md.dungeonLayouts.AddToTable("AUTOCAVE", 125);
        md.dungeonLayouts.AddToTable("CAVE", 70);
        //md.dungeonLayouts.AddToTable("LAKE", 85);

        md.dungeonTileSets.AddToTable("MOUNTAINGRASS", 100);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;

        md.monsterFamilies.AddToTable("insects", 125);
        md.monsterFamilies.AddToTable("snakes", 75);
        md.monsterFamilies.AddToTable("beasts", 150);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 50);
        md.monsterFamilies.AddToTable("frogs", 25);
        md.monsterFamilies.AddToTable("bandits", 125);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("ConvertWallsToHolesForMountainGrass");
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");
        md.dictMetaDataPerLevel.Add("nocamerabounds", 1);
        md.dictMetaDataPerLevel.Add("hasholes", 1);
        DungeonLevel dl = new DungeonLevel();        

        md.parallaxSpriteRef = "clouds";
        md.parallaxShiftPerTile = new Vector2(-0.25f, -0.25f);
        md.parallaxTileCount = 50;
        md.revealAll = true;
        md.unbreakableWalls = true;

        md.localChanceSpawnShopkeeperOnFloor = 0.33f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.5f;

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.25f },
            { 5, 1.3f },
            { 6, 1.35f },
            { 7, 1.4f },
            { 8, 1.45f },
            { 9, 1.5f }, 
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 3 },
            { 7, 3 },
            { 8, 3 },
            { 9, 4 },
        };
        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 2 },
            { 3, 2 },
            { 4, 2 },
            { 5, 3 },
            { 6, 3 },
            { 7, 3 },
            { 8, 3 },
            { 9, 4 },
        };

        /* for (int i = 0; i < numFloors; i++)
        {
            float targetCV = 1.0f + ((i - 1) * 0.038f);
            if (targetCV >= GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION)
            {
                targetCV = GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION;
            }
            md.floorsToCV.Add(i, targetCV);

            int targetChampions = 1 + (i / 8);
            if (i >= 8) targetChampions++;
            if (i >= 16) targetChampions++;

            int targetChampionMods = 1;
            if (i >= 6) targetChampionMods++;
            if (i >= 12) targetChampionMods++;
            if (i >= 18) targetChampionMods++;

            md.championsByFloor.Add(i, targetChampions);
            md.championModsByFloor.Add(i, targetChampionMods);
        } */

        masterMysteryDungeonDict.Add(md.refName, md);

        // MOUNTAIN REVENGE

        md = new MysteryDungeon("dungeon10");
        md.minScaledRelicValueMultiplier = 0.6f;
        md.maxScaledRelicValueMultiplier = 1.05f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon10_name");
        numFloors = 20;
        md.floors = numFloors;
        md.dungeonLayouts.AddToTable("AUTOCAVE", 125);
        md.dungeonLayouts.AddToTable("CAVE", 70);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);
        md.dungeonLayouts.AddToTable("STANDARD", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 60);


        md.dungeonTileSets.AddToTable("MOUNTAINGRASS", 100);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;

        md.monsterFamilies.AddToTable("insects", 105);
        md.monsterFamilies.AddToTable("snakes", 60);
        md.monsterFamilies.AddToTable("beasts", 125);
        md.monsterFamilies.AddToTable("hybrids", 100);
        md.monsterFamilies.AddToTable("spirits", 50);
        md.monsterFamilies.AddToTable("frogs", 25);
        md.monsterFamilies.AddToTable("robots", 50);
        md.monsterFamilies.AddToTable("bandits", 125);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        md.scriptsOnPostBuild.Add("ConvertWallsToHolesForMountainGrass");
        md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");
        md.dictMetaDataPerLevel.Add("nocamerabounds", 1);
        md.dictMetaDataPerLevel.Add("hasholes", 1);        

        md.parallaxSpriteRef = "clouds";
        md.parallaxShiftPerTile = new Vector2(-0.25f, -0.25f);
        md.parallaxTileCount = 50;
        md.revealAll = true;
        md.unbreakableWalls = true;

        md.localChanceSpawnShopkeeperOnFloor = 0.33f;
        md.monstersRespawn = true;
        md.respawnRateModifier = 0.75f;

        md.floorsToCV = new Dictionary<int, float>()
        {
            { 0, 1.0f },
            { 1, 1.1f },
            { 2, 1.15f },
            { 3, 1.2f },
            { 4, 1.2f },
            { 5, 1.25f },
            { 6, 1.3f },
            { 7, 1.35f },
            { 8, 1.4f },
            { 9, 1.4f },
            { 10, 1.45f },
            { 11, 1.5f },
            { 12, 1.55f },
            { 13, 1.6f },
            { 14, 1.66f },
            { 15, 1.7f },
            { 16, 1.75f },
            { 17, 1.8f },
            { 18, 1.85f },
            { 19, 1.9f } 
        };
        
        md.championModsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 1 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 2 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 3 },
            { 13, 3 },
            { 14, 3 },
            { 15, 4 },
            { 16, 4 },
            { 17, 4 },
            { 18, 4 },
            { 19, 4 }
        };

        md.championsByFloor = new Dictionary<int, int>()
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 },
            { 6, 2 },
            { 7, 2 },
            { 8, 3 },
            { 9, 3 },
            { 10, 3 },
            { 11, 3 },
            { 12, 4 },
            { 13, 4 },
            { 14, 4 },
            { 15, 4 },
            { 16, 4 },
            { 17, 5 },
            { 18, 5 },
            { 19, 5 }
        };

        masterMysteryDungeonDict.Add(md.refName, md);

        // CAVE OF ETERNITY

        md = new MysteryDungeon("dungeon11");
        md.minScaledRelicValueMultiplier = 0.8f;
        md.maxScaledRelicValueMultiplier = 1.2f;
        md.displayName = StringManager.GetString("exp_wanderer_dungeon11_name");
        md.floors = 99;

        md.dungeonLayouts.AddToTable("MAZEROOMS", 100);
        md.dungeonLayouts.AddToTable("CAVEROOMS", 75);
        md.dungeonLayouts.AddToTable("STANDARD", 200);
        md.dungeonLayouts.AddToTable("SPIRIT_DUNGEON", 200);
        md.dungeonLayouts.AddToTable("STANDARDNOHALLS", 100);
        md.dungeonLayouts.AddToTable("CAVE", 100);
        md.dungeonLayouts.AddToTable("AUTOCAVE", 100);
        md.dungeonLayouts.AddToTable("LAKE", 75);
        md.dungeonLayouts.AddToTable("VOLCANO", 75);
        md.dungeonLayouts.AddToTable("BSPROOMS", 75);

        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY] = true;

        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.FLASK] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.MONEY] = true;        
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS] = true;
        md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.PET] = true;
        //md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.ITEMS] = true;

        md.AddGimmick(MysteryGimmicks.NO_SCALING_LIMIT);

        md.scriptOnEnterMap = "RefreshShopkeepersOnlyOnce";
        //md.scriptsOnPostBuild.Add("AddExtraChestsWithGear");

        md.localChanceSpawnShopkeeperOnFloor = 0.15f;
        md.monstersRespawn = true;

        md.dungeonTileSets.AddToTable("BLUESTONEDARK", 100);
        md.dungeonTileSets.AddToTable("BLUESTONELIGHT", 100);
        md.dungeonTileSets.AddToTable("COBBLE", 100);
        md.dungeonTileSets.AddToTable("REINFORCED", 100);
        md.dungeonTileSets.AddToTable("STONE", 100);

        md.dungeonTileSets.AddToTable("NIGHTMARISH", 100);
        md.dungeonTileSets.AddToTable("VOLCANO", 100);
        md.dungeonTileSets.AddToTable("SLATE", 100);
        md.dungeonTileSets.AddToTable("EARTH", 100);
        md.dungeonTileSets.AddToTable("TREETOPS", 100);
        md.dungeonTileSets.AddToTable("LUSHGREEN", 100);
        md.dungeonTileSets.AddToTable("MOSS", 100);
        md.dungeonTileSets.AddToTable("VOID", 100);

        md.monsterFamilies.AddToTable("insects", 50);
        md.monsterFamilies.AddToTable("snakes", 30);
        md.monsterFamilies.AddToTable("beasts", 120);
        md.monsterFamilies.AddToTable("hybrids", 65);
        md.monsterFamilies.AddToTable("spirits", 100);
        md.monsterFamilies.AddToTable("frogs", 50);
        md.monsterFamilies.AddToTable("bandits", 120);
        md.monsterFamilies.AddToTable("robots", 90);

        md.floorsToCV = new Dictionary<int, float>();
        md.championModsByFloor = new Dictionary<int, int>();
        md.championsByFloor = new Dictionary<int, int>();

        for (int i = 0; i < 99; i++)
        {
            float targetCV = 1.0f + (0.025f * i);
            md.floorsToCV.Add(i, targetCV);

            int targetChampions = 1 + (i / 10);
            if (i >= 6) targetChampions++;
            if (i >= 12) targetChampions++;
            if (i >= 18) targetChampions++;
            if (i >= 24) targetChampions++;
            if (i >= 30) targetChampions++;

            int targetChampionMods = 1;
            if (i >= 5) targetChampionMods++;
            if (i >= 10) targetChampionMods++;
            if (i >= 15) targetChampionMods++;
            if (i >= 20) targetChampionMods++;

            md.championsByFloor.Add(i, targetChampions);
            md.championModsByFloor.Add(i, targetChampionMods);
        }

        masterMysteryDungeonDict.Add(md.refName, md);

    }

    public static MysteryDungeon GetMysteryDungeonByRef(string refName)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return null;
        if (!initialized)
        {
            Initialize();
        }
        MysteryDungeon md = null;
        if (masterMysteryDungeonDict.TryGetValue(refName, out md))
        {
            return md;
        }
        Debug.LogError("Warning: Could not find mystery dungeon ref " + refName + "!!!");
        return md;
    }
}
