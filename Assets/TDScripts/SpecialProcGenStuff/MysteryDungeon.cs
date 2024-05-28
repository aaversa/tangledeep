using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MysteryGimmicks { NO_JP_GAIN, NO_XP_GAIN, NO_ITEM_DROPS, EXTRA_CONSUMABLES, EXTRA_ITEMS_BREAKABLES, MANY_CHAMPIONS, JP_CAMPFIRES, MONSTER_CHASER,
    LOW_MONSTER_AGGRO, MONSTER_INFIGHTING_AND_LEVELUP, HIGHER_MERCHANT_VALUE, NORMAL_MONSTERS, NO_SCALING_LIMIT, COUNT
}

/// <summary>
/// Contains all metadata for a dungeon that is procedurally generated. Num floors, possible types, size weighting, start/end conditions etc.
/// </summary>
public class MysteryDungeon
{
    public string refName;
    public string displayName;
    public int floors;
    public float monsterDensityMultiplier;
    public float randomDestructibleMultiplier;
    public float lootDropMultiplier;
    public float xpModMultiplier;
    public float monsterAttackAndDefenseScaling;
    public ActorTable dungeonLayouts;
    public ActorTable dungeonTileSets;
    public ActorTable monsterFamilies;
    public bool[] resourcesAtStart;
    public bool[] resourcesKeepAtEnd;
    public Dictionary<int, float> floorsToCV;
    public Dictionary<int, ActorTable> spawnTables;
    public List<MonsterTemplateData> monstersInDungeon;
    public Dictionary<int, int> championsByFloor;
    public Dictionary<int, int> championModsByFloor;
    public List<string> scriptsOnPostBuild;
    public string scriptOnEnterMap;
    public bool[] gimmicks;
    public bool monstersRespawn;
    public Map[] mapsInDungeon;
    public bool dieInRealLife;
    public HashSet<string> prefabsUsedByMonstersInDungeon;

    public float localChanceSpawnShopkeeperOnFloor;
    public float respawnRateModifier;

    public float minScaledRelicValueMultiplier;
    public float maxScaledRelicValueMultiplier;

    public const float CHANCE_ADDITIONAL_BREAKABLES_ON_MAP = 0.008f;
    public const float CHANCE_EXTRA_ITEMS_BREAKABLE = 0.2f;
    public const float CHANCE_EXTRA_ANY_CONSUMABLE = 0.035f;
    public const float CHANCE_OFFENSE_ITEM = 0.09f;
    public const float CHANCE_EXTRA_FOOD = 0.045f;

    public string parallaxSpriteRef;
    public Vector2 parallaxShiftPerTile;
    public int parallaxTileCount;
    public bool revealAll;
    public bool unbreakableWalls;
    public Dictionary<string, int> dictMetaDataPerLevel;

    public MysteryDungeon(string rName)
    {
        refName = rName;
        dungeonLayouts = new ActorTable();
        monsterFamilies = new ActorTable();
        dungeonTileSets = new ActorTable();
        scriptsOnPostBuild = new List<string>();
        prefabsUsedByMonstersInDungeon = new HashSet<string>();
        monstersInDungeon = new List<MonsterTemplateData>();
        mapsInDungeon = new Map[5];
        monsterDensityMultiplier = 1f;
        randomDestructibleMultiplier = 1f;
        lootDropMultiplier = 1f;
        xpModMultiplier = 1f;
        monsterAttackAndDefenseScaling = 1f;
        respawnRateModifier = 1.0f;
        localChanceSpawnShopkeeperOnFloor = 0f;
        resourcesAtStart = new bool[(int)EMysteryDungeonPlayerResources.COUNT];
        resourcesKeepAtEnd = new bool[(int)EMysteryDungeonPlayerResources.COUNT];
        spawnTables = new Dictionary<int, ActorTable>(); // procedurally generated later
        dictMetaDataPerLevel = new Dictionary<string, int>();
        gimmicks = new bool[(int)MysteryGimmicks.COUNT];

        for (int i = 0; i < resourcesAtStart.Length; i++)
        {
            resourcesAtStart[i] = false;
            resourcesKeepAtEnd[i] = false;
        }
    }

    public bool CheckIfMonsterPrefabValid(MonsterTemplateData mtd)
    {
        if (prefabsUsedByMonstersInDungeon.Contains(mtd.prefab))
        {
            return false;
        }

        return true;
    }

    public bool HasGimmick(MysteryGimmicks gim)
    {
        return gimmicks[(int)gim];
    }

    public void AddGimmick(MysteryGimmicks gim)
    {
        gimmicks[(int)gim] = true;
    }
}
