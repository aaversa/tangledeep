using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml;

public enum EMonsterPowerType { DAMAGE, HEALBUFF, PULL, PUSH, MOVESELF, SUMMONHAZARD, SUMMONPET, DEBUFF, PASSIVE, MISC, COUNT };
public enum EMonsterPowerTags { FIRE, WATER, SHADOW, POISON, LIGHTNING, HEALING, ATTACKPASSIVE, DEFENDPASSIVE, SUMMONPASSIVE,
    RANGEDDAMAGE, ANYSUMMON, ANYMOVEMENT, ANYELEMENT, COUNT };
public enum EPrefabExtraTags { MULTICOLOR, RANGEDWEAPON, CASTER, FIRE, WATER, SHADOW, POISON, LIGHTNING, GRASS_OR_TREE,
    NATURALGROWTH, HIGHLEVEL, FLYING, MELEE_ONLY, COUNT };

public enum EMonsterNameElements { SLIMESUFFIX, BANDITPREFIX, INSECTSUFFIX, ROBOTSUFFIX, SPIRITSUFFIX, HYBRIDSUFFIX,
    FROGSUFFIX, BEASTPREFIX, ROBOTPREFIX, COLOREDPREFIX, SLIMEPREFIX, WEAKFIREPREFIX, STRONGFIREPREFIX, WEAKWATERPREFIX,
    STRONGWATERPREFIX, WEAKPOISONPREFIX, STRONGPOISONPREFIX, WEAKSHADOWPREFIX, STRONGSHADOWPREFIX, WEAKLIGHTNINGPREFIX,
    STRONGLIGHTNINGPREFIX, SUPPORTERWORD, SUMMONERWORD, RANGEDWORD, STATUSUSERWORD, MELEEWORD, BEASTSUFFIX, SNAKESUFFIX,
    GRASSTREEWORD, FROGPREFIX, HYBRIDPREFIX, INSECTPREFIX, FROGCONCATPREFIX, FROGCONCATSUFFIX, BANDITSUFFIX,
    BEASTCONCATPREFIX, BEASTCONCATSUFFIX, SLIMECONCATPREFIX, SLIMECONCATSUFFIX, SPIRITPREFIX, ROBOTCONCATPREFIX, ROBOTCONCATSUFFIX,
    NATURALGROWTHPREFIX, NATURALGROWTHSUFFIX, SPIRITCONCATPREFIX, SPIRITCONCATSUFFIX, DISRUPTORWORD, COUNT }

// We want to pack even more data AROUND the MonsterPowerData class
// This data is only used by MonsterMaker, so there's no need to put it in MPD
public class MonsterPowerDataTemplate
{
    // Based on existing hand-balanced data, store the min/max possible monster level where this power shows up
    public int minMonsterLevel = 99;
    public int maxMonsterLevel = 0;

    public MonsterPowerData mpd;
    public string powerRef;
    public EMonsterPowerType powerType;
    public bool chargeAbility;
    public bool[] extraTags;
    public string limitToFamily;

    public bool IsCompatibleWithElement(EPrefabExtraTags tag)
    {
        if (tag == EPrefabExtraTags.FIRE)
        {
            if (extraTags[(int)EMonsterPowerTags.WATER] || extraTags[(int)EMonsterPowerTags.SHADOW] || extraTags[(int)EMonsterPowerTags.LIGHTNING]
                || extraTags[(int)EMonsterPowerTags.POISON])
            {
                return false;
            }
        }

        if (tag == EPrefabExtraTags.WATER)
        {
            if (extraTags[(int)EMonsterPowerTags.FIRE] || extraTags[(int)EMonsterPowerTags.SHADOW] || extraTags[(int)EMonsterPowerTags.LIGHTNING]
                || extraTags[(int)EMonsterPowerTags.POISON])
            {
                return false;
            }
        }

        if (tag == EPrefabExtraTags.SHADOW)
        {
            if (extraTags[(int)EMonsterPowerTags.WATER] || extraTags[(int)EMonsterPowerTags.FIRE] || extraTags[(int)EMonsterPowerTags.LIGHTNING]
                || extraTags[(int)EMonsterPowerTags.POISON])
            {
                return false;
            }
        }

        if (tag == EPrefabExtraTags.POISON)
        {
            if (extraTags[(int)EMonsterPowerTags.WATER] || extraTags[(int)EMonsterPowerTags.SHADOW] || extraTags[(int)EMonsterPowerTags.LIGHTNING]
                || extraTags[(int)EMonsterPowerTags.FIRE])
            {
                return false;
            }
        }

        if (tag == EPrefabExtraTags.LIGHTNING)
        {
            if (extraTags[(int)EMonsterPowerTags.WATER] || extraTags[(int)EMonsterPowerTags.SHADOW] || extraTags[(int)EMonsterPowerTags.FIRE]
                || extraTags[(int)EMonsterPowerTags.POISON])
            {
                return false;
            }
        }
        return true;
    }

    public void AddTag(EMonsterPowerTags tag)
    {
        extraTags[(int)tag] = true;
    }

    public bool CheckTag(EMonsterPowerTags tag)
    {
        return extraTags[(int)tag];
    }

    public MonsterPowerDataTemplate(string aRef, EMonsterPowerType aType, int minRange = 1, int maxRange = 99, int minLevel = 1, int maxLevel = 99)
    {
        limitToFamily = "";
        extraTags = new bool[(int)EMonsterPowerTags.COUNT];
        powerRef = aRef;
        powerType = aType;
        minMonsterLevel = minLevel;
        maxMonsterLevel = maxLevel;
        mpd = new MonsterPowerData();
        mpd.abilityRef = GameMasterScript.masterAbilityList[powerRef];
        mpd.minRange = minRange;
        mpd.maxRange = maxRange;
    }

    public MonsterPowerDataTemplate(string aRef, EMonsterPowerType aType, bool isChargeAbility, int minRange = 1, int maxRange = 99, int minLevel = 1, int maxLevel = 99)
    {
        limitToFamily = "";
        extraTags = new bool[(int)EMonsterPowerTags.COUNT];
        powerRef = aRef;
        powerType = aType;
        minMonsterLevel = minLevel;
        maxMonsterLevel = maxLevel;
        chargeAbility = isChargeAbility;
        mpd = new MonsterPowerData();
        mpd.abilityRef = GameMasterScript.masterAbilityList[powerRef];
        mpd.minRange = minRange;
        mpd.maxRange = maxRange;
    }
}

/// <summary>
/// This defines special properties for a generated monster template like ranged attacker, elemental user, etc.
/// </summary>
public class MonsterArchetypes
{
    public string refName;
    public bool forceMeleeWeapon;
    public bool forceRangedWeapon;
    public DamageTypes forceWeaponElement;
    public float[] statMods;
    public float weaponPowerMod = 1f;
    public float defenseMod = 1f;
    public string armorID;
    public float[] elementalResists;
    public int[] monAttributes;

    public int[] minPowersRequiredByType;
    public int[] minPowersRequiredByTag;
    public List<string> powerPool;
    public int minPowersRequiredFromPool;

    public EPrefabExtraTags elementalPrefabTag;

    public EMonsterNameElements weakNameElement;
    public EMonsterNameElements strongNameElement;
    public bool namingElementIsPrefix;
    

    public MonsterArchetypes(string rName)
    {
        elementalPrefabTag = EPrefabExtraTags.COUNT;
        refName = rName;
        powerPool = new List<string>();
        statMods = new float[(int)StatTypes.COUNT];
        for (int i = 0; i < statMods.Length; i++)
        {
            statMods[i] = 1f;
        }

        elementalResists = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < elementalResists.Length; i++)
        {
            elementalResists[i] = 1f;
        }

        monAttributes = new int[(int)MonsterAttributes.COUNT];

        minPowersRequiredByType = new int[(int)EMonsterPowerType.COUNT];
        minPowersRequiredByTag = new int[(int)EMonsterPowerTags.COUNT];
    }
}

/// <summary>
/// Data pack that stores % chance of attributes occurring per monster family
/// </summary>
public class FamilyAttributeTemplate
{
    public string familyRef;
    public Dictionary<MonsterAttributes, float> dictChanceOfAttributes;
    public Dictionary<MonsterAttributes, int> dictAverageAttribute;
}

public partial class MonsterMaker
{

    static int generatedMonsterCounter = 0;
    static int generatedMonsterWeaponCounter = 0;

    static List<string> monsterNamesUsedInSaveFile;

    static List<string>[] monsterReferencesByLevel;

    public static List<MonsterTemplateData> uniqueMonstersSpawnedInSaveFile = new List<MonsterTemplateData>();
    public static Dictionary<string, Weapon> uniqueWeaponsSpawnedInSaveFile = new Dictionary<string, Weapon>();
    public static Dictionary<string, List<MonsterTemplateData>> existingMonstersByFamily = new Dictionary<string, List<MonsterTemplateData>>();
    public static Dictionary<string, List<string>> prefabsByFamily = new Dictionary<string, List<string>>();
    public static Dictionary<string, MonsterPowerDataTemplate> monsterPowerMasterList = new Dictionary<string, MonsterPowerDataTemplate>();
    public static Dictionary<EMonsterPowerType, List<MonsterPowerDataTemplate>> monsterPowersByType = new Dictionary<EMonsterPowerType, List<MonsterPowerDataTemplate>>();
    public static Dictionary<EMonsterPowerTags, List<MonsterPowerDataTemplate>> monsterPowersByTag = new Dictionary<EMonsterPowerTags, List<MonsterPowerDataTemplate>>();
    public static Dictionary<string, List<EPrefabExtraTags>> dictPrefabTags;
    public static Dictionary<string, MonsterArchetypes> dictMonsterArchetypes;
    public static Dictionary<string, int> dictAverageMonsterWeightsByFamily;
    public static Dictionary<EMonsterNameElements, List<string>> dictMonsterNameElements;
    public static List<MonsterPowerDataTemplate> possiblePowers;
    public static ActorTable monsterFamilyFrequency;
    public static ActorTable monsterArchetypes;
    public static ActorTable multiPowerChances;

    public static bool initialized;

    public const float MONSTER_DUALWIELD_CHANCE = 0.1f;
    public const float MONSTER_SHIELD_CHANCE = 0.15f;
    public const float MONSTER_RANGE_CHANCE = 0.13f;

    public const float CHANCE_REROLL_ELEMPOWER = 0.75f; // If we're NOT an elemental creature, chance that we will NOT use a given elem power
    public const float CHANCE_REROLL_OUTOFLEVELRANGE_POWER = 1.0f; // Chance for creatures to REROLL 'required' powers outside their level range
    public const float CHANCE_MULTIPLE_MOVE_POWERS = 0.5f; // Chance to have more than one movement ability

    public static Dictionary<string, FamilyAttributeTemplate> dictAttributeChancesByFamily;

    public HashSet<string> germanMonsterPrefixesThatShouldHyphenate;

    public static void FlushSaveFileData()
    {
        if (!initialized) // || !DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return;
        }

        monsterNamesUsedInSaveFile.Clear();
        uniqueMonstersSpawnedInSaveFile.Clear();
        uniqueWeaponsSpawnedInSaveFile.Clear();

        generatedMonsterCounter = 0;
        generatedMonsterWeaponCounter = 0;
    }

    public static void Initialize()
    {
        if (initialized) return; // || !DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) 

        initialized = true;

        AddExistingMonstersSortedByFamilyToDictionary();
        CreateMonsterPowerData();
        CreateMonsterArchetypes();
        CreateFamilyAttributeData();
        ReadMonsterNameElements();

        uniqueMonstersSpawnedInSaveFile = new List<MonsterTemplateData>();
        uniqueWeaponsSpawnedInSaveFile = new Dictionary<string, Weapon>();
        monsterNamesUsedInSaveFile = new List<string>();
        possiblePowers = new List<MonsterPowerDataTemplate>();

        // This is a slightly jank way of determining % chance to get 1-4 powers but... it's not terrible?
        multiPowerChances = new ActorTable();
        multiPowerChances.AddToTable("1", 100);
        multiPowerChances.AddToTable("2", 100);
        multiPowerChances.AddToTable("3", 40);
        multiPowerChances.AddToTable("4", 15);

        monsterFamilyFrequency = new ActorTable();
        monsterFamilyFrequency.AddToTable("jelly", 75);
        monsterFamilyFrequency.AddToTable("bandits", 100);
        monsterFamilyFrequency.AddToTable("beasts", 110);
        monsterFamilyFrequency.AddToTable("hybrids", 110);
        monsterFamilyFrequency.AddToTable("robots", 75);
        monsterFamilyFrequency.AddToTable("snakes", 50);
        monsterFamilyFrequency.AddToTable("insects", 50);
        monsterFamilyFrequency.AddToTable("spirits", 100);
        monsterFamilyFrequency.AddToTable("frogs", 100);
    }

    public static string GetMonsterOfCV(float cv)
    {
        if (monsterReferencesByLevel == null)
        {
            PlaceAllGeneratedMonstersIntoCVArray();
        }

        int level = BalanceData.GetMonsterLevelByCV(cv, false);

        if (monsterReferencesByLevel[level] == null || monsterReferencesByLevel[level].Count == 0)
        {
            level--;
            if (monsterReferencesByLevel[level] == null || monsterReferencesByLevel[level].Count == 0)
            {
                level += 2;
            }
            if (monsterReferencesByLevel[level] == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Uhh NOTHING available for " + cv + "...?");
                return uniqueMonstersSpawnedInSaveFile.GetRandomElement().refName;
            }
        }

        return monsterReferencesByLevel[level].GetRandomElement();
    }

    static void PlaceAllGeneratedMonstersIntoCVArray()
    {
        monsterReferencesByLevel = new List<string>[25];

        foreach(MonsterTemplateData m in uniqueMonstersSpawnedInSaveFile)
        {
            if (monsterReferencesByLevel[m.baseLevel] == null)
            {
                monsterReferencesByLevel[m.baseLevel] = new List<string>();
            }
            monsterReferencesByLevel[m.baseLevel].Add(m.refName);
            //Debug.Log("Added monster by level " + m.baseLevel);
        }

        //if (Debug.isDebugBuild) Debug.Log("Placed all generated monsters into CV array.");
    }

    /// <summary>
    /// Sets the current monster & weapons counter (ensuring unique IDs) based on loaded monsters from meta progress
    /// </summary>
    public static void SetCustomMonsterCounterFromLoadedMonsters()
    {
        generatedMonsterCounter = uniqueMonstersSpawnedInSaveFile.Count + 1;
        generatedMonsterWeaponCounter = uniqueWeaponsSpawnedInSaveFile.Count + 1;
    }

    static void AddExistingMonstersSortedByFamilyToDictionary()
    {
        existingMonstersByFamily = new Dictionary<string, List<MonsterTemplateData>>();
        prefabsByFamily = new Dictionary<string, List<string>>();
        dictPrefabTags = new Dictionary<string, List<EPrefabExtraTags>>();
        dictAverageMonsterWeightsByFamily = new Dictionary<string, int>();

        foreach (MonsterTemplateData mtd in GameMasterScript.masterMonsterList.Values)
        {
            if (mtd.isBoss) continue; // Exclude bosses from this entire system. Too many weird and special cases.

            if (!dictAverageMonsterWeightsByFamily.ContainsKey(mtd.monFamily))
            {
                dictAverageMonsterWeightsByFamily.Add(mtd.monFamily, mtd.weight);
            }
            else
            {
                int avgWeight = (dictAverageMonsterWeightsByFamily[mtd.monFamily] + mtd.weight)/ 2;
                dictAverageMonsterWeightsByFamily[mtd.monFamily] = avgWeight;
            }

            if (!string.IsNullOrEmpty(mtd.monFamily) && !existingMonstersByFamily.ContainsKey(mtd.monFamily))
            {
                existingMonstersByFamily.Add(mtd.monFamily, new List<MonsterTemplateData>());
            }

            if (!existingMonstersByFamily[mtd.monFamily].Contains(mtd))
            {
                existingMonstersByFamily[mtd.monFamily].Add(mtd);
            }

            if (!prefabsByFamily.ContainsKey(mtd.monFamily))
            {
                prefabsByFamily.Add(mtd.monFamily, new List<string>());
            }

            if (!prefabsByFamily[mtd.monFamily].Contains(mtd.prefab))
            {
                prefabsByFamily[mtd.monFamily].Add(mtd.prefab);
            }

            if (!dictPrefabTags.ContainsKey(mtd.prefab))
            {
                dictPrefabTags.Add(mtd.prefab, new List<EPrefabExtraTags>());
            }
        }

        // Add any unused prefabs manually

        dictPrefabTags.Add("MonsterDarkChemist_Alt", new List<EPrefabExtraTags>());
        prefabsByFamily["bandits"].Add("MonsterDarkChemist_Alt");


        if (!dictPrefabTags.ContainsKey("MonsterGhostSamurai_Alt"))
        {
            dictPrefabTags.Add("MonsterGhostSamurai_Alt", new List<EPrefabExtraTags>());
        }
        if (!prefabsByFamily["spirits"].Contains("MonsterGhostSamurai_Alt"))
        {
            prefabsByFamily["spirits"].Add("MonsterGhostSamurai_Alt");
        }
        dictPrefabTags["MonsterGhostSamurai_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);

        dictPrefabTags["GuardianSeeker_Alt"].Add(EPrefabExtraTags.FLYING);

        dictPrefabTags.Add("MonsterJadeBeetle_Alt", new List<EPrefabExtraTags>());
        prefabsByFamily["insects"].Add("MonsterJadeBeetle_Alt");
        dictPrefabTags["MonsterJadeBeetle_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);


        if (!dictPrefabTags.ContainsKey("MonsterSlimeRat_Alt"))
        {
            dictPrefabTags.Add("MonsterSlimeRat_Alt", new List<EPrefabExtraTags>());
        }
        if (!prefabsByFamily.ContainsKey("MonsterSlimeRat_Alt"))
        {
            prefabsByFamily["beasts"].Add("MonsterSlimeRat_Alt");
        }
        
        dictPrefabTags["MonsterSlimeRat_Alt"].Add(EPrefabExtraTags.MELEE_ONLY);

        if (!dictPrefabTags.ContainsKey("Hornet"))
        {
            dictPrefabTags.Add("Hornet", new List<EPrefabExtraTags>());
        }
        if (!prefabsByFamily["insects"].Contains("Hornet"))
        {
            prefabsByFamily["insects"].Add("Hornet");
        }                

        dictPrefabTags["MonsterSpiritMoose"].Add(EPrefabExtraTags.HIGHLEVEL);
        dictPrefabTags["MonsterSpiritMoose"].Add(EPrefabExtraTags.MELEE_ONLY);

        dictPrefabTags.Add("MonsterRockViperAlt", new List<EPrefabExtraTags>());
        prefabsByFamily["snakes"].Add("MonsterRockViperAlt");

        if (!dictPrefabTags.ContainsKey("TreeFrog"))
        {
            dictPrefabTags.Add("TreeFrog", new List<EPrefabExtraTags>());
        }
        if (prefabsByFamily["frogs"].Contains("TreeFrog"))
        {
            prefabsByFamily["frogs"].Add("TreeFrog");
        }

        dictPrefabTags.Add("MonsterAcidElemental_Alt", new List<EPrefabExtraTags>());
        prefabsByFamily["hybrids"].Add("MonsterAcidElemental_Alt");
        dictPrefabTags["MonsterAcidElemental_Alt"].Add(EPrefabExtraTags.HIGHLEVEL);

        dictPrefabTags.Add("SnappingTurtle_Alt", new List<EPrefabExtraTags>());
        prefabsByFamily["beasts"].Add("SnappingTurtle_Alt");
        dictPrefabTags["SnappingTurtle_Alt"].Add(EPrefabExtraTags.MULTICOLOR);

        dictPrefabTags.Add("MonsterSpiritMoose_Alt", new List<EPrefabExtraTags>());
        prefabsByFamily["hybrids"].Add("MonsterSpiritMoose_Alt");
        dictPrefabTags["MonsterSpiritMoose_Alt"].Add(EPrefabExtraTags.HIGHLEVEL);
        dictPrefabTags["MonsterSpiritMoose_Alt"].Add(EPrefabExtraTags.FIRE);


        dictPrefabTags.Add("MonsterFungalFrogAlt", new List<EPrefabExtraTags>());
        prefabsByFamily["frogs"].Add("MonsterFungalFrogAlt");

        if (!prefabsByFamily["snakes"].Contains("MonsterFloatingSnake"))
        {
            prefabsByFamily["snakes"].Add("MonsterFloatingSnake");
        }
        if (!dictPrefabTags.ContainsKey("MonsterFloatingSnake"))
        {
            dictPrefabTags.Add("MonsterFloatingSnake", new List<EPrefabExtraTags>());
        }
        dictPrefabTags["MonsterFloatingSnake"].Add(EPrefabExtraTags.FLYING);
        
        dictPrefabTags["MonsterButterfly"].Add(EPrefabExtraTags.FLYING);

        dictPrefabTags.Add("MonsterPumpkinVine", new List<EPrefabExtraTags>());
        dictPrefabTags["MonsterPumpkinVine"].Add(EPrefabExtraTags.GRASS_OR_TREE);
        prefabsByFamily["hybrids"].Add("MonsterPumpkinVine");


        dictPrefabTags.Add("MonsterPumpkinSlime", new List<EPrefabExtraTags>());
        prefabsByFamily["jelly"].Add("MonsterPumpkinSlime");

        dictPrefabTags["MonsterBatSlime"].Add(EPrefabExtraTags.FLYING);

        dictPrefabTags.Add("Panthrox", new List<EPrefabExtraTags>());
        dictPrefabTags["Panthrox"].Add(EPrefabExtraTags.HIGHLEVEL);
        prefabsByFamily["beasts"].Add("Panthrox");

        dictPrefabTags.Add("MadderScientist", new List<EPrefabExtraTags>());
        dictPrefabTags["MadderScientist"].Add(EPrefabExtraTags.HIGHLEVEL);
        prefabsByFamily["bandits"].Add("MadderScientist");

        dictPrefabTags["MadderScientist_Alt"].Add(EPrefabExtraTags.HIGHLEVEL);        

        dictPrefabTags.Add("MonsterNewGolem", new List<EPrefabExtraTags>());
        dictPrefabTags["MonsterNewGolem"].Add(EPrefabExtraTags.HIGHLEVEL);
        prefabsByFamily["robots"].Add("MonsterNewGolem");

        dictPrefabTags["GuardianDisc2"].Add(EPrefabExtraTags.FLYING);

        if (!dictPrefabTags.ContainsKey("MiniSentryBot"))
        {
            dictPrefabTags.Add("MiniSentryBot", new List<EPrefabExtraTags>());
        }
        dictPrefabTags["MiniSentryBot"].Add(EPrefabExtraTags.FLYING);
        if (!prefabsByFamily["robots"].Contains("MiniSentryBot"))
        {
            prefabsByFamily["robots"].Add("MiniSentryBot");
        }

        // Remove special prefabs like mr. gold frog
        prefabsByFamily["beasts"].Remove("MonsterMiniCaveLion");
        prefabsByFamily["frogs"].Remove("MonsterGoldfrog");
        prefabsByFamily["frogs"].Remove("MonsterDarkfrog");
        prefabsByFamily["hybrids"].Remove("MagicVine");
        prefabsByFamily["hybrids"].Remove("MonsterBrigand");
        prefabsByFamily["hybrids"].Remove("RunicCrystal");
        prefabsByFamily["robots"].Remove("MonsterNewGolem");
        //prefabsByFamily["hybrids"].Remove("MonsterTargetDummy"); // This one is kinda funny.
        prefabsByFamily["spirits"].Remove("ShadowHunter");
        prefabsByFamily["robots"].Remove("PrototypeHusynStasis");

        // Reclassify this because it doesn't look like a 'bot
        prefabsByFamily["robots"].Remove("BarrierCrystal");
        prefabsByFamily["hybrids"].Add("BarrierCrystal");

        /* prefabsByFamily["hybrids"].Remove("BigUrchin");
        prefabsByFamily["hybrids"].Remove("ToxicUrchin");
        prefabsByFamily["hybrids"].Remove("FungalColumn");
        prefabsByFamily["hybrids"].Remove("SpittingPlant"); */
        // BigUrchin, ToxicUrchin, FungalColumn, SpittingPlant... reclassify these prefabs?

        AddTagsToMonsterPrefabs();

        #region Debug - Count prefabs/tags
        /* int[] totalCounts = new int[9];
        string prefabList = "";
        foreach(string key in prefabsByFamily.Keys)
        {
            prefabList = "";
            Debug.Log("Prefabs in " + key + ": " + prefabsByFamily[key].Count);
            int fire = 0;
            int water = 0;
            int poison = 0;
            int lightning = 0;
            int shadow = 0;
            int elemental = 0;
            int ranged = 0;
            int melee = 0;
            int flying = 0;
            foreach(string prefab in prefabsByFamily[key])
            {
                prefabList += prefab + " ";
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.RANGEDWEAPON))
                {
                    ranged++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.MELEE_ONLY))
                {
                    melee++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.FLYING))
                {
                    flying++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.FIRE))
                {
                    fire++;
                    elemental++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.WATER))
                {
                    water++;
                    elemental++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.POISON))
                {
                    poison++;
                    elemental++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.LIGHTNING))
                {
                    lightning++;
                    elemental++;
                }
                if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.SHADOW))
                {
                    shadow++;
                    elemental++;
                }
            }
            Debug.Log(key + " has: " + elemental + " any elemental, " + fire + " fire, " + water + " water, " + lightning + " lightning," + shadow + " shadow, and " + poison + " poison. " + melee + " melee, " + ranged + " ranged, and " + flying + " flyers.");
            Debug.Log(prefabList);
        } */
        #endregion
    }


    public static MonsterTemplateData CreateNewMonsterByCV(float challengeValue, string forceFamily, MysteryDungeon theDungeon)
    {
        int level = 0;

        foreach(int levelKey in BalanceData.DICT_LEVEL_TO_CV.Keys)
        {
            if (challengeValue >= BalanceData.DICT_LEVEL_TO_CV[levelKey])
            {
                level = levelKey;
            }
            else
            {
                break;
            }
        }

        if (level == 0) level = 1;
        if (level >= 20) level = 20;

        MonsterTemplateData mtd = CreateNewMonster(level, theDungeon, forceFamily);
        return mtd;
    }

    public static void CreateNewMonsterByPlayerLevel(MysteryDungeon theDungeon, string forceFamily = "")
    {
        int desiredMonsterLevel = BalanceData.monsterLevelByPlayerLevel[GameMasterScript.heroPCActor.myStats.GetLevel()];
        CreateNewMonster(desiredMonsterLevel, theDungeon, forceFamily);
    }

    public static MonsterTemplateData CreateNewMonster(int monsterLevel, MysteryDungeon theDungeon, string forceFamily = "")
    {
        MonsterTemplateData mtd = new MonsterTemplateData();

        // First, pick a monster family.

        string monFamily = monsterFamilyFrequency.GetRandomActorRef();        
        if (!string.IsNullOrEmpty(forceFamily))
        {
            monFamily = forceFamily;
        }

        mtd.monFamily = monFamily;
        mtd.showInPedia = true;
        mtd.autoSpawn = true;
        
        // Does this have a special archetype?
        string archetype = monsterArchetypes.GetRandomActorRef();
        MonsterArchetypes mArchetypeUsed = mArchetypeUsed = dictMonsterArchetypes[archetype];

        // Don't allow melee-only or ranged-only sprites to use melee/ranged prefabs, please.
        // Also don't allow monsters w/ same prefab to be used... let the dungeon check that
        mtd.prefab = SetMonsterPrefab(monFamily, mArchetypeUsed, monsterLevel);
        int attempts = 0;
        bool resetArchetype = false;
        while ((mArchetypeUsed.forceMeleeWeapon && dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.RANGEDWEAPON)) || (mArchetypeUsed.forceRangedWeapon && dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.MELEE_ONLY)) || !theDungeon.CheckIfMonsterPrefabValid(mtd))
        {
            attempts++;
            mtd.prefab = SetMonsterPrefab(monFamily, mArchetypeUsed, monsterLevel);            
            if (attempts > 50 && !resetArchetype) // Maybe our archetype / family combo is bad? Try something else.
            {                
                mArchetypeUsed = dictMonsterArchetypes[monsterArchetypes.GetRandomActorRef()];
                attempts = 0;
                resetArchetype = true;
            }
            if (attempts >= 100)
            {
                // OK, maybe our *family* is bad and there are no more prefabs left in it.
                mtd.monFamily = theDungeon.monsterFamilies.GetRandomActorRef();
                mArchetypeUsed = dictMonsterArchetypes[monsterArchetypes.GetRandomActorRef()];
                //Debug.Log("Reroll family, archetype");
            }
            if (attempts >= 200)
            {
                Debug.Log("Big time problem selecting prefab, family, archetype given " + mtd.monFamily + " " + mArchetypeUsed.refName + " " + mtd.prefab);
                break;
            }
        }

        //Debug.Log("Selected family, prefab, archetype: " + mtd.monFamily + " " + mtd.prefab + " " + mArchetypeUsed.refName);

        SetMonsterLevelAndCoreStats(monsterLevel, mtd, mArchetypeUsed);
        SetMonsterWeaponsAndOffhand(monsterLevel, mtd, mArchetypeUsed);
        SetMonsterAttributes(monsterLevel, mtd, mArchetypeUsed);

        mtd.turnsToLoseInterest = UnityEngine.Random.Range(10, 13) * monsterLevel;
        mtd.aggroRange = 2 + (monsterLevel / 2);
        mtd.weight = dictAverageMonsterWeightsByFamily[monFamily];
        mtd.weight = (int)UnityEngine.Random.Range(mtd.weight * 0.7f, mtd.weight * 1.3f);
        if (mtd.aggroRange > 8)
        {
            mtd.aggroRange = 8;
        }

        if (theDungeon.HasGimmick(MysteryGimmicks.LOW_MONSTER_AGGRO))
        {
            mtd.aggroRange = 1;
            if (mtd.monAttributes[(int)MonsterAttributes.CALLFORHELP] >= 50)
            {
                mtd.monAttributes[(int)MonsterAttributes.CALLFORHELP] = 50;
            }
            mtd.visionRange = 6;
            mtd.turnsToLoseInterest = 20;
        }

        mtd.showInPedia = true;

        mtd.monsterName = CreateMonsterName(mtd, mArchetypeUsed);

        while (monsterNamesUsedInSaveFile.Contains(mtd.monsterName))
        {
            mtd.monsterName = CreateMonsterName(mtd, mArchetypeUsed);
            //Debug.Log("Fail 1");
        }

        mtd.refName = "mon_genmon_" + mtd.monFamily + generatedMonsterCounter;
        while (GameMasterScript.masterMonsterList.ContainsKey(mtd.refName) || SharedCorral.HasCustomMonsterTemplateOfRefName(mtd.refName))
        {
            generatedMonsterCounter++;
            mtd.refName = "mon_genmon_" + mtd.monFamily + generatedMonsterCounter;
            //Debug.Log("Fail 2");
        }
        mtd.moveRange = 1;
        mtd.xpMod = 1f;
        mtd.lootChance = 1f;
        mtd.faction = Faction.ENEMY;
        mtd.challengeValue = BalanceData.DICT_LEVEL_TO_CV[mtd.baseLevel];

        SetMonsterPowers(mtd, mArchetypeUsed);

        if (theDungeon.HasGimmick(MysteryGimmicks.MONSTER_INFIGHTING_AND_LEVELUP))
        {
            AddSuperInfightingPropertiesToMTD(mtd);
        }

        generatedMonsterCounter++;

#if UNITY_EDITOR
        //Debug.Log(mtd.refName + " " + mtd.monFamily + " " + archetype + " " + mtd.prefab + " " + mtd.monsterName + " " + mtd.challengeValue + " " + mtd.baseLevel);
#endif

        GameMasterScript.masterMonsterList.Add(mtd.refName, mtd);
        uniqueMonstersSpawnedInSaveFile.Add(mtd);
        monsterNamesUsedInSaveFile.Add(mtd.monsterName);
        GameMasterScript.monstersInPedia.Add(mtd);

        theDungeon.prefabsUsedByMonstersInDungeon.Add(mtd.prefab);

        return mtd;
    }

    /// <summary>
    /// Creates a weapon which may be melee (1H) or bow. Sets element, range, animations.
    /// </summary>
    /// <param name="monsterLevel"></param>
    /// <param name="mtd"></param>
    /// <param name="archetype"></param>
    /// <param name="rangedAllowed"></param>
    /// <returns></returns>
    static Weapon CreateMonsterWeapon(int monsterLevel, MonsterTemplateData mtd, MonsterArchetypes archetype, bool rangedAllowed = true)
    {
        Weapon monWeapon = new Weapon();

        // Figure out weapon element.
        monWeapon.damType = DamageTypes.PHYSICAL;
        if (archetype.forceWeaponElement != DamageTypes.PHYSICAL && archetype.forceWeaponElement != DamageTypes.COUNT)
        {
            monWeapon.damType = archetype.forceWeaponElement;
        }

        // Now create a weapon. Is it ranged?

        bool weaponIsRanged = UnityEngine.Random.Range(0, 1f) <= MONSTER_RANGE_CHANCE;

        if (rangedAllowed)
        {
            if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.RANGEDWEAPON) && !weaponIsRanged)
            {
                weaponIsRanged = UnityEngine.Random.Range(0, 2) == 0;
            }

            if (archetype.forceMeleeWeapon) weaponIsRanged = false;
            if (archetype.forceRangedWeapon) weaponIsRanged = true;
        }
        else
        {
            weaponIsRanged = false;
        }

        if (weaponIsRanged && dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.MELEE_ONLY))
        {
            Debug.Log("Created ranged weapon for " + mtd.prefab + " which should be melee only.");
        }

        monWeapon.actorRefName = "genmon_weapon_" + generatedMonsterWeaponCounter;
        while (GameMasterScript.masterItemList.ContainsKey(monWeapon.actorRefName))
        {
            generatedMonsterWeaponCounter++;
            monWeapon.actorRefName = "genmon_weapon_" + generatedMonsterWeaponCounter;
        }
        generatedMonsterWeaponCounter++;
        monWeapon.isRanged = weaponIsRanged;

        // Figure out flavor damage type.
        if (weaponIsRanged)
        {
            int range = UnityEngine.Random.Range(2, 5);
            if (range == 2) range = UnityEngine.Random.Range(2, 5); // range=2 should be less common.
            monWeapon.range = range;
            monWeapon.eqFlags[(int)EquipmentFlags.MELEEPENALTY] = true;

            if (UnityEngine.Random.Range(0,2) == 0)
            {
                monWeapon.flavorDamType = FlavorDamageTypes.BLUNT;
            }
            else
            {
                monWeapon.flavorDamType = FlavorDamageTypes.PIERCE;
            }

            monWeapon.weaponType = WeaponTypes.BOW;
        }
        else
        {
            monWeapon.range = 1;
            int flavorType = UnityEngine.Random.Range(0, 4);
            switch(flavorType)
            {
                case 0:
                    if (mtd.monFamily != "robots")
                    {
                        monWeapon.weaponType = WeaponTypes.SWORD;
                        monWeapon.flavorDamType = FlavorDamageTypes.BITE;
                    }
                    else
                    {
                        monWeapon.weaponType = WeaponTypes.MACE;
                        monWeapon.flavorDamType = FlavorDamageTypes.BLUNT;
                    }
                    break;
                case 1:
                    monWeapon.weaponType = WeaponTypes.MACE;
                    monWeapon.flavorDamType = FlavorDamageTypes.BLUNT;
                    break;
                case 2:
                    if (UnityEngine.Random.Range(0,2) == 0)
                    {
                        monWeapon.weaponType = WeaponTypes.SWORD;                        
                    }
                    else
                    {
                        monWeapon.weaponType = WeaponTypes.CLAW;
                    }
                    monWeapon.flavorDamType = FlavorDamageTypes.SLASH;

                    break;
                case 3:
                    monWeapon.weaponType = WeaponTypes.DAGGER;
                    monWeapon.flavorDamType = FlavorDamageTypes.PIERCE;
                    break;
            }
        }

        // Figure out weapon power. Easy enough!
        monWeapon.power = BalanceData.expectedMonsterWeaponPower[monsterLevel] * archetype.weaponPowerMod;

        // This sets our projectile, swing/impact effects as needed.
        monWeapon.SetSwingAndImpactAnimations();

        monWeapon.spriteRef = "none"; // doesn't matter, won't ever be dropped
        monWeapon.challengeValue = 999f; // indicates it won't ever be dropped :D 

        uniqueWeaponsSpawnedInSaveFile.Add(monWeapon.actorRefName, monWeapon);
        GameMasterScript.masterItemList.Add(monWeapon.actorRefName, monWeapon);

        return monWeapon;
    }

    /// <summary>
    /// Builds the entire monster display name based on monster family and archetype
    /// </summary>
    /// <param name="mtd"></param>
    /// <param name="mArchetypeUsed"></param>
    /// <returns></returns>
    public static string CreateMonsterName(MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        string baseName = ""; // independent of archetype
        string modifierName = ""; // may be influenced by archetype
        string finalName = "";

        string concatLetter = "";
        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            concatLetter = "-";
        }

        bool baseNameIsPrefix = false;

        string prefixForGerman = "";
        string suffixForGerman = "";
        string concatNameForGerman = "";

        switch(mtd.monFamily)
        {
            case "jelly":
                baseName = dictMonsterNameElements[EMonsterNameElements.SLIMESUFFIX].GetRandomElement();
                suffixForGerman = baseName;
                baseNameIsPrefix = false;
                modifierName = dictMonsterNameElements[EMonsterNameElements.SLIMEPREFIX].GetRandomElement();
                prefixForGerman = modifierName;
                if (UnityEngine.Random.Range(0,2) == 0 && StringManager.gameLanguage != EGameLanguage.es_spain)
                {
                    modifierName = "";
                    baseName = dictMonsterNameElements[EMonsterNameElements.SLIMECONCATPREFIX].GetRandomElement() + concatLetter + dictMonsterNameElements[EMonsterNameElements.SLIMECONCATSUFFIX].GetRandomElement();
                    concatNameForGerman = baseName;
                }
                break;
            case "hybrids":
                baseName = dictMonsterNameElements[EMonsterNameElements.HYBRIDSUFFIX].GetRandomElement();
                if (UnityEngine.Random.Range(0,2) == 0 || StringManager.gameLanguage == EGameLanguage.de_germany)
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.HYBRIDPREFIX].GetRandomElement();
                }
                else
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.BEASTPREFIX].GetRandomElement();
                }
                prefixForGerman = modifierName;
                suffixForGerman = baseName;
                baseNameIsPrefix = false;
                break;
            case "frogs":
                baseName = dictMonsterNameElements[EMonsterNameElements.FROGSUFFIX].GetRandomElement();
                suffixForGerman = baseName;
                if (UnityEngine.Random.Range(0,3) == 0 && StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.BEASTPREFIX].GetRandomElement();
                }
                else
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.FROGPREFIX].GetRandomElement();
                }
                prefixForGerman = modifierName;

                if (UnityEngine.Random.Range(0, 2) == 0 && StringManager.gameLanguage != EGameLanguage.es_spain)
                {
                    modifierName = "";
                    baseName = dictMonsterNameElements[EMonsterNameElements.FROGCONCATPREFIX].GetRandomElement() + concatLetter + dictMonsterNameElements[EMonsterNameElements.FROGCONCATSUFFIX].GetRandomElement();
                    concatNameForGerman = baseName;
                }

                baseNameIsPrefix = false;
                break;
            case "robots":
                int roll = UnityEngine.Random.Range(0, 3);
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    roll = UnityEngine.Random.Range(0, 2);
                }
                if (roll == 0)
                {
                    baseName = dictMonsterNameElements[EMonsterNameElements.ROBOTPREFIX].GetRandomElement();
                    modifierName = dictMonsterNameElements[EMonsterNameElements.ROBOTSUFFIX].GetRandomElement();
                    baseNameIsPrefix = true;
                    prefixForGerman = baseName;
                    suffixForGerman = modifierName;
                }
                else if (roll == 1 && StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    baseName = dictMonsterNameElements[EMonsterNameElements.ROBOTSUFFIX].GetRandomElement();
                    modifierName = dictMonsterNameElements[EMonsterNameElements.ROBOTPREFIX].GetRandomElement();
                    baseNameIsPrefix = false;
                    prefixForGerman = modifierName;
                    suffixForGerman = baseName;
                }
                else
                {
                    baseName = dictMonsterNameElements[EMonsterNameElements.ROBOTCONCATPREFIX].GetRandomElement() + concatLetter + dictMonsterNameElements[EMonsterNameElements.ROBOTCONCATSUFFIX].GetRandomElement();
                    concatNameForGerman = baseName;
                }

                break;
            case "beasts":
                baseName = dictMonsterNameElements[EMonsterNameElements.BEASTSUFFIX].GetRandomElement();
                modifierName = dictMonsterNameElements[EMonsterNameElements.BEASTPREFIX].GetRandomElement();
                prefixForGerman = modifierName;
                suffixForGerman = baseName;
                baseNameIsPrefix = false;
                if (UnityEngine.Random.Range(0, 2) == 0 && StringManager.gameLanguage != EGameLanguage.es_spain)
                {
                    modifierName = "";
                    baseName = dictMonsterNameElements[EMonsterNameElements.BEASTCONCATPREFIX].GetRandomElement() + concatLetter + dictMonsterNameElements[EMonsterNameElements.BEASTCONCATSUFFIX].GetRandomElement();
                    concatNameForGerman = baseName;
                }
                break;
            case "snakes":
                baseName = dictMonsterNameElements[EMonsterNameElements.SNAKESUFFIX].GetRandomElement();

                if (StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.BEASTPREFIX].GetRandomElement();
                }
                else
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.COLOREDPREFIX].GetRandomElement();
                }
                
                prefixForGerman = modifierName;
                suffixForGerman = baseName;
                baseNameIsPrefix = false;
                break;
            case "insects":
                baseName = dictMonsterNameElements[EMonsterNameElements.INSECTSUFFIX].GetRandomElement();
                suffixForGerman = baseName;
                if (UnityEngine.Random.Range(0,2) == 0 && StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.BEASTPREFIX].GetRandomElement();
                }
                else
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.INSECTPREFIX].GetRandomElement();
                }
                prefixForGerman = modifierName;
                baseNameIsPrefix = false;
                break;
            case "bandits":
                baseName = dictMonsterNameElements[EMonsterNameElements.BANDITPREFIX].GetRandomElement();
                modifierName = dictMonsterNameElements[EMonsterNameElements.BANDITSUFFIX].GetRandomElement();
                prefixForGerman = baseName;
                suffixForGerman = modifierName;
                baseNameIsPrefix = true;
                break;
            case "spirits":
                baseName = dictMonsterNameElements[EMonsterNameElements.SPIRITSUFFIX].GetRandomElement();
                modifierName = dictMonsterNameElements[EMonsterNameElements.SPIRITPREFIX].GetRandomElement();
                prefixForGerman = modifierName;
                suffixForGerman = baseName;
                baseNameIsPrefix = false;
                if (UnityEngine.Random.Range(0,2) == 0 && StringManager.gameLanguage != EGameLanguage.es_spain)
                {
                    baseName = dictMonsterNameElements[EMonsterNameElements.SPIRITCONCATPREFIX].GetRandomElement() + concatLetter + dictMonsterNameElements[EMonsterNameElements.SPIRITCONCATSUFFIX].GetRandomElement();
                    modifierName = "";
                    concatNameForGerman = baseName;
                }
                break;
        }

        bool forceModifierAsPrefix = false;

        // What if it's a real planty thing tho.
        if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.NATURALGROWTH))
        {
            baseName = dictMonsterNameElements[EMonsterNameElements.NATURALGROWTHSUFFIX].GetRandomElement();
            modifierName = dictMonsterNameElements[EMonsterNameElements.NATURALGROWTHPREFIX].GetRandomElement();
            suffixForGerman = baseName;
            prefixForGerman = modifierName;
            baseNameIsPrefix = false;
        }

        // Archetypes can give us special names
        if (mArchetypeUsed.refName != "none")
        {
            if (mtd.baseLevel < 9)
            {
                modifierName = dictMonsterNameElements[mArchetypeUsed.weakNameElement].GetRandomElement();
            }
            else
            {
                modifierName = dictMonsterNameElements[mArchetypeUsed.strongNameElement].GetRandomElement();
            }

            forceModifierAsPrefix = mArchetypeUsed.namingElementIsPrefix;
            prefixForGerman = modifierName;
        }
        else
        {
            // No archetype? Maybe our prefab type infers something?
            // If we already have a modifier though, only 50/50 to base this on prefab.
            if (UnityEngine.Random.Range(0,2) == 0 || string.IsNullOrEmpty(modifierName))
            {
                if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.MULTICOLOR) && !baseNameIsPrefix)
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.COLOREDPREFIX].GetRandomElement();
                    prefixForGerman = modifierName;
                }
                else if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.RANGEDWEAPON))
                {
                    modifierName = dictMonsterNameElements[EMonsterNameElements.RANGEDWORD].GetRandomElement();
                    prefixForGerman = modifierName;
                }
            }

        }

        string addCharacter = " ";
        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            // Strip out the spaces in this case

            if (!string.IsNullOrEmpty(concatNameForGerman))
            {
                return concatNameForGerman;
            }

            string finalNameForGerman = "";

            if (prefixForGerman[prefixForGerman.Length-1] != '-') // prefix does NOT end with a hyphen
            {
                finalNameForGerman = prefixForGerman + " " + suffixForGerman;
            }
            else // prefix DOES end with hyphen, no space needed
            {
                finalNameForGerman = prefixForGerman + " " + suffixForGerman;
                finalNameForGerman = finalNameForGerman.Replace(" ", String.Empty);
            }                       

            finalNameForGerman = CustomAlgorithms.RemoveTrailingCharacter(finalNameForGerman, '-');            

            return finalNameForGerman;
        }

        if (!string.IsNullOrEmpty(modifierName))
        {
            if (forceModifierAsPrefix)
            {
                return modifierName + addCharacter + baseName;
            }
            else if (baseNameIsPrefix)
            {
                return baseName + addCharacter + modifierName;
            }
            else if (!baseNameIsPrefix)
            {
                return modifierName + addCharacter + baseName;
            }
        }



        return baseName;
    }

    /// <summary>
    /// Sets the monsters prefab, avoiding things like Fire monster w/ Water prefab, based on Archetype
    /// </summary>
    /// <param name="monFamily"></param>
    /// <param name="mArchetypeUsed"></param>
    /// <returns></returns>
    static string SetMonsterPrefab(string monFamily, MonsterArchetypes mArchetypeUsed, int monsterLevel)
    {
        string prefabSelected = prefabsByFamily[monFamily].GetRandomElement();

        bool prefabValid = false;
        int attempts = 0;
        while (!prefabValid)
        {
            attempts++;
            if (attempts > 100)
            {
                //Debug.LogError("Nothing at all valid??? " + monFamily + " " + mArchetypeUsed.refName + " " + prefabSelected);
                prefabValid = true;
                break;
            }
            // Detect and avoid conflicts
            if (mArchetypeUsed.elementalPrefabTag != EPrefabExtraTags.COUNT)
            {
                // If our selected prefab is an elemental (fire, water, etc) but does not match our forced prefab tag, reroll it.
                int localAttempts = 0;
                while (IsPrefabElemental(prefabSelected) && !dictPrefabTags[prefabSelected].Contains(mArchetypeUsed.elementalPrefabTag))
                {
                    localAttempts++;
                    if (localAttempts > 100)
                    {
                        Debug.LogError("Shit The Bed 2: " + monFamily + " " + mArchetypeUsed.refName + " " + prefabSelected);
                        break;
                    }
                    prefabSelected = prefabsByFamily[monFamily].GetRandomElement();
                }
            }

            prefabValid = true;

            // Don't use badass prefabs on weak mooks
            if (monsterLevel < 10 && dictPrefabTags[prefabSelected].Contains(EPrefabExtraTags.HIGHLEVEL))
            {
                prefabValid = false;
            }
        }

        return prefabSelected;
    }

    /// <summary>
    /// Returns TRUE if the prefab is tagged with a specific element like Fire or Water
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    static bool IsPrefabElemental(string prefab)
    {
        if (dictPrefabTags[prefab].Count == 0) return false;
        if (dictPrefabTags[prefab].Contains(EPrefabExtraTags.FIRE) || dictPrefabTags[prefab].Contains(EPrefabExtraTags.WATER) ||
            dictPrefabTags[prefab].Contains(EPrefabExtraTags.SHADOW) || dictPrefabTags[prefab].Contains(EPrefabExtraTags.LIGHTNING)
            || dictPrefabTags[prefab].Contains(EPrefabExtraTags.POISON))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets level, HP, core stats including charge time + accuracy based on monster level + archetype
    /// </summary>
    /// <param name="mtd"></param>
    /// <param name="mArchetypeUsed"></param>
    static void SetMonsterLevelAndCoreStats(int monsterLevel, MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        // Then assign level and write stats based on level.
        mtd.baseLevel = monsterLevel;
        // expectedStatValues has 3 values accessible by adding 0,1,2 to the lvl*3
        // Corestats, Chargetime, accuracy.
        mtd.strength = BalanceData.expectedStatValues[mtd.baseLevel * 3] * mArchetypeUsed.statMods[(int)StatTypes.STRENGTH];
        mtd.swiftness = BalanceData.expectedStatValues[mtd.baseLevel * 3] * mArchetypeUsed.statMods[(int)StatTypes.SWIFTNESS];
        mtd.guile = BalanceData.expectedStatValues[mtd.baseLevel * 3] * mArchetypeUsed.statMods[(int)StatTypes.GUILE];
        mtd.visionRange = 12;
        mtd.discipline = BalanceData.expectedStatValues[mtd.baseLevel * 3] * mArchetypeUsed.statMods[(int)StatTypes.DISCIPLINE];
        mtd.spirit = BalanceData.expectedStatValues[mtd.baseLevel * 3] * mArchetypeUsed.statMods[(int)StatTypes.SPIRIT];
        mtd.chargetime = BalanceData.expectedStatValues[mtd.baseLevel * 3 + 1];
        mtd.accuracy = BalanceData.expectedStatValues[mtd.baseLevel * 3 + 1];
        mtd.hp = BalanceData.expectedMonsterHealth[mtd.baseLevel];
    }

    /// <summary>
    /// Creates at least one weapon for monster. May be a 2h ranged, or dual wield, or 1h + shield
    /// </summary>
    /// <param name="monsterLevel"></param>
    /// <param name="mtd"></param>
    /// <param name="mArchetypeUsed"></param>
    static void SetMonsterWeaponsAndOffhand(int monsterLevel, MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        bool allowRanged = true;
        if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.MELEE_ONLY))
        {
            allowRanged = false;
        }

        Weapon monWeapon = CreateMonsterWeapon(monsterLevel, mtd, mArchetypeUsed, allowRanged);

        mtd.weaponID = monWeapon.actorRefName;

        if (!monWeapon.isRanged)
        {
            if (monsterLevel < 4) // low level MD monsters need just a *bit* more damage
            {
                monWeapon.power += (monsterLevel * 0.75f);
            }

            if (UnityEngine.Random.Range(0, 1f) <= MONSTER_DUALWIELD_CHANCE)
            {
                Weapon offWeapon = CreateMonsterWeapon(monsterLevel, mtd, mArchetypeUsed, false);
                mtd.offhandWeaponID = offWeapon.actorRefName;
            }
            else if (UnityEngine.Random.Range(0, 1f) <= MONSTER_SHIELD_CHANCE)
            {
                int lookupLevel = monsterLevel;
                if (lookupLevel >= BalanceData.monsterShieldsByMonsterLevel.Count)
                {
                    lookupLevel = BalanceData.monsterShieldsByMonsterLevel.Count - 1;
                }
                mtd.offhandArmorID = BalanceData.monsterShieldsByMonsterLevel[lookupLevel];
            }
        }
        else
        {
            if (monsterLevel < 4) // low level MD monsters need just a *bit* more damage
            {
                monWeapon.power += (monsterLevel * 0.5f);
            }
        }


    }

    /// <summary>
    /// Sets all attributes in the template based on family, random chance, and archetype.
    /// </summary>
    /// <param name="monsterLevel"></param>
    /// <param name="mtd"></param>
    /// <param name="mArchetypeUsed"></param>
    static void SetMonsterAttributes(int monsterLevel, MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        // Some properties are basically only BENEFICIAL to the player, and should not appear on low level monsters.
        if (monsterLevel < 5)
        {
            CheckAndSetAttribute(mtd, MonsterAttributes.TIMID, mArchetypeUsed);
            CheckAndSetAttribute(mtd, MonsterAttributes.GANGSUP, mArchetypeUsed);
        }
        if (monsterLevel < 7)
        {
            CheckAndSetAttribute(mtd, MonsterAttributes.PREDATOR, mArchetypeUsed);
        }
        if (monsterLevel < 9)
        {
            CheckAndSetAttribute(mtd, MonsterAttributes.STALKER, mArchetypeUsed);
        }
        CheckAndSetAttribute(mtd, MonsterAttributes.RONIN, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.BERSERKER, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.GREEDY, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.CALLFORHELP, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.LOVESMUD, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.FLYING, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.LOVESLAVA, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.LOVESBATTLES, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.COMBINABLE, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.SUPPORTER, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.HEALER, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.SNIPER, mArchetypeUsed);
        CheckAndSetAttribute(mtd, MonsterAttributes.LAZY, mArchetypeUsed);
    }

    static void CheckAndSetAttribute(MonsterTemplateData mtd, MonsterAttributes attr, MonsterArchetypes mArchetypeUsed)
    {
        float chance = 0f;
        if (dictAttributeChancesByFamily[mtd.monFamily].dictChanceOfAttributes.TryGetValue(attr, out chance))
        {
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                mtd.monAttributes[(int)attr] = GetAttributeValueByFamily(mtd.monFamily, attr);
            }
        }        

        // Archetype overrides everything else.
        if (mArchetypeUsed.monAttributes[(int)attr] > 0)
        {
            mtd.monAttributes[(int)attr] = mArchetypeUsed.monAttributes[(int)attr];
        }

        if (attr == MonsterAttributes.FLYING || attr == MonsterAttributes.LOVESMUD || attr == MonsterAttributes.LOVESLAVA)
        {
            if (dictPrefabTags[mtd.prefab].Contains(EPrefabExtraTags.FLYING))
            {
                mtd.monAttributes[(int)MonsterAttributes.FLYING] = 100;
                mtd.monAttributes[(int)MonsterAttributes.LOVESMUD] = 100;
                mtd.monAttributes[(int)MonsterAttributes.LOVESLAVA] = 100;
            }
        }

        if (attr == MonsterAttributes.STALKER)
        {            
            Weapon w = GameMasterScript.masterItemList[mtd.weaponID] as Weapon;
            if (w.range > 1)
            {
                mtd.stalkerRange = w.range;
            }
            else
            {
                mtd.monAttributes[(int)MonsterAttributes.STALKER] = 0;
            }
            
        }
    }

    static int GetAttributeValueByFamily(string family, MonsterAttributes attr)
    {
        int targetValue = dictAttributeChancesByFamily[family].dictAverageAttribute[attr];
        targetValue = (int)UnityEngine.Random.Range(targetValue * 0.75f, targetValue * 1.25f);
        if (targetValue < 0) targetValue = 0;
        if (targetValue > 100) targetValue = 100;
        return targetValue;
    }

    static void SetMonsterPowers(MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        string powerSelector = multiPowerChances.GetRandomActorRef();
        // Returns 1p, 2p, 3p, etc
        int numPowers = Int32.Parse(powerSelector);
        if (mtd.baseLevel < 4 && numPowers > 2) // Really weak monsters shouldn't have many powers.
        {
            numPowers = UnityEngine.Random.Range(1, 3);
        }

        List<MonsterPowerDataTemplate> powersForMonster = new List<MonsterPowerDataTemplate>();

        possiblePowers.Clear();
        foreach(MonsterPowerDataTemplate mpd in monsterPowerMasterList.Values)
        {
            if (mtd.baseLevel < mpd.minMonsterLevel || mtd.baseLevel > mpd.maxMonsterLevel) continue;

            // Some powers are Jelly-only, Robot-only, etc.
            if (!string.IsNullOrEmpty(mpd.limitToFamily) && mpd.limitToFamily != mtd.monFamily) continue;

            if (!mpd.IsCompatibleWithElement(mArchetypeUsed.elementalPrefabTag))
            {
                continue;
            }
         
            possiblePowers.Add(mpd);
        }

#if UNITY_EDITOR
        //Debug.Log(possiblePowers.Count + " possible powers for monster " + mtd.baseLevel + " " + mtd.monFamily + " " + mArchetypeUsed.refName);
#endif

        int powersUsed = 0;
        for (int i = 0; i < mArchetypeUsed.minPowersRequiredByTag.Length; i++)
        {
            EMonsterPowerTags eTag = (EMonsterPowerTags)i;
            for (int x = 0; x < mArchetypeUsed.minPowersRequiredByTag[i]; x++)
            {
                // Should we take from all powers or make sure these are in possiblePowers? :thinking:
                MonsterPowerDataTemplate powerSelected = monsterPowersByTag[eTag].GetRandomElement();
                int localAttempts = 0;

                while (!CheckPowerCompatibility(powerSelected, powersForMonster, mtd, mArchetypeUsed))
                //while (powersForMonster.Contains(powerSelected) || (!string.IsNullOrEmpty(powerSelected.limitToFamily) && powerSelected.limitToFamily != mtd.monFamily))
                {
                    localAttempts++;
                    if (localAttempts > 100)
                    {
                        if (Debug.isDebugBuild) Debug.Log("<color=red>Error 1: " + mtd.refName + " " + mArchetypeUsed.refName + " " + mtd.baseLevel + " " + possiblePowers.Count + " " + powersForMonster.Count + "</color>");
                        break;
                    }
                    powerSelected = monsterPowersByTag[eTag].GetRandomElement();
                }
                powersForMonster.Add(powerSelected);
                powersUsed++;
                //Debug.Log("Need power tag " + eTag + " for " + mArchetypeUsed.refName + " and picked " + powerSelected.powerRef);

                /* if (powerSelected.powerType != EMonsterPowerType.PASSIVE || !firstFreePassive)
                {
                    powersUsed--;
                    firstFreePassive = true;
                } */
            }
        }

        //firstFreePassive = false;

        for (int i = 0; i < mArchetypeUsed.minPowersRequiredByType.Length; i++)
        {
            EMonsterPowerType eType = (EMonsterPowerType)i;
            for (int x = 0; x < mArchetypeUsed.minPowersRequiredByType[i]; x++)
            {
                // Should we take from all powers or make sure these are in possiblePowers? :thinking:
                MonsterPowerDataTemplate powerSelected = monsterPowersByType[eType].GetRandomElement();
                int localAttempts = 0;
                while (!CheckPowerCompatibility(powerSelected, powersForMonster, mtd, mArchetypeUsed))
                //while (powersForMonster.Contains(powerSelected) || (!string.IsNullOrEmpty(powerSelected.limitToFamily) && powerSelected.limitToFamily != mtd.monFamily))
                {
                    localAttempts++;
                    if (localAttempts > 100)
                    {
                        Debug.Log("<color=red>Error 2: " + mtd.refName + " " + mArchetypeUsed.refName + " " + mtd.baseLevel + " " + possiblePowers.Count + " " + powersForMonster.Count + "</color>");
                        break;
                    }
                    
                    powerSelected = monsterPowersByType[eType].GetRandomElement();
                }

                powersForMonster.Add(powerSelected);
                powersUsed++;

                /* if (powerSelected.powerType != EMonsterPowerType.PASSIVE || !firstFreePassive)
                {
                    powersUsed--;
                    firstFreePassive = true;
                } */
            }
        }

        List<EMonsterPowerType> typesUsed = new List<EMonsterPowerType>();
        bool[] tagsUsed = new bool[(int)EMonsterPowerTags.COUNT];
        MonsterPowerDataTemplate power = null;

        int attempts = 0;

        for (int i = powersUsed; i < numPowers; i++)
        {
            bool powerValid = false;
            while (!powerValid)
            {
                attempts++;
                if (attempts > 100)
                {
                    Debug.Log("<color=red>Error 3: " + mtd.refName + " " + mArchetypeUsed.refName + " " + mtd.baseLevel + " " + possiblePowers.Count + " " + powersForMonster.Count + "</color>");
                }
                powerValid = true;
                power = possiblePowers.GetRandomElement();
                int localAttempts = 0;
                while (powersForMonster.Contains(power))
                {
                    localAttempts++;
                    if (localAttempts > 100)
                    {
                        Debug.Log("<color=red>Error 4: " + mtd.refName + " " + mArchetypeUsed.refName + " " + mtd.baseLevel + " " + possiblePowers.Count + " " + powersForMonster.Count + "</color>");
                        break;
                    }
                    power = possiblePowers.GetRandomElement();
                }

                if (tagsUsed[(int)EMonsterPowerTags.ANYMOVEMENT] && power.CheckTag(EMonsterPowerTags.ANYMOVEMENT) && UnityEngine.Random.Range(0, 1f) > CHANCE_MULTIPLE_MOVE_POWERS)
                {
                    powerValid = false;
                    continue;
                }

                // We're not an elemental creature, but this is an elem power. Is that OK?
                if (mArchetypeUsed.elementalPrefabTag == EPrefabExtraTags.COUNT && power.CheckTag(EMonsterPowerTags.ANYELEMENT))
                {
                    if (UnityEngine.Random.Range(0, 1f) <= CHANCE_REROLL_ELEMPOWER)
                    {
                        powerValid = false;
                        continue;
                    }
                }
            }


            // Keep track of what tags we have already used
            for (int x = 0; x < power.extraTags.Length; x++)
            {
                if (power.extraTags[x])
                {
                    tagsUsed[x] = true;
                }
            }
                        
            powersForMonster.Add(power);
            
            // 50% chance that your FIRST Passive power doesn't count toward power cap, as long as we have a reasonable power count
            if (power.powerType == EMonsterPowerType.PASSIVE && UnityEngine.Random.Range(0,2) == 0 && !typesUsed.Contains(EMonsterPowerType.PASSIVE) && numPowers < 4)
            {
                i--;
            }

            typesUsed.Add(power.powerType);
        }


        foreach(MonsterPowerDataTemplate mpdt in powersForMonster)
        {
            mtd.monsterPowers.Add(mpdt.mpd);
        }


        //Debug.Log(powerBuilder);


        if (powersForMonster.Count == 1 && mtd.baseLevel >= 3) // monsters with few powers scale up
        {
            mtd.hp *= 1.1f;
            mtd.strength *= 1.1f;
            mtd.swiftness *= 1.1f;
        }
    }

    /// <summary>
    /// Returns TRUE if powerSelected is compatible with the current template, archetype, level, and used power list
    /// </summary>
    /// <param name="powerSelected"></param>
    /// <param name="powersForMonster"></param>
    /// <param name="mtd"></param>
    /// <returns></returns>
    public static bool CheckPowerCompatibility(MonsterPowerDataTemplate powerSelected, List<MonsterPowerDataTemplate> powersForMonster, MonsterTemplateData mtd, MonsterArchetypes mArchetypeUsed)
    {
        // Don't allow duplicate powers
        if (powersForMonster.Contains(powerSelected)) return false;

        // If the given power is limited to a specific monster family and mtd is NOT that family, disallow
        if (!string.IsNullOrEmpty(powerSelected.limitToFamily) && powerSelected.limitToFamily != mtd.monFamily)
        {
            return false;
        }

        // This archetype is not elemental, but power IS elemental. Slim chance to allow, otherwise don't.
        if (mArchetypeUsed.elementalPrefabTag == EPrefabExtraTags.COUNT && powerSelected.CheckTag(EMonsterPowerTags.ANYELEMENT) && UnityEngine.Random.Range(0,1f) <= CHANCE_REROLL_ELEMPOWER)
        {
            return false;
        }

        if ((powerSelected.minMonsterLevel > mtd.baseLevel || powerSelected.maxMonsterLevel < mtd.baseLevel) && UnityEngine.Random.Range(0,1f) <= CHANCE_REROLL_OUTOFLEVELRANGE_POWER)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Write all of our custom-generated monster templates AND their weapons to XML
    /// </summary>
    /// <param name="writer"></param>
    public static void WriteAllCustomMonstersToSave(XmlWriter writer)
    {
        foreach(MonsterTemplateData mtd in uniqueMonstersSpawnedInSaveFile)
        {
            MonsterTemplateSerializer.WriteCustomMonsterToSave(mtd, writer);
        }
    }

    static void AddSuperInfightingPropertiesToMTD(MonsterTemplateData mtd)
    {
        mtd.monAttributes[(int)MonsterAttributes.BERSERKER] = 85;
        mtd.monAttributes[(int)MonsterAttributes.TIMID] = 0;
        MonsterPowerData removePower = null;
        bool hasDmgMonsters = false;
        foreach (MonsterPowerData mpd in mtd.monsterPowers)
        {
            if (mpd.abilityRef.refName == "skill_expmon_empowerkill")
            {
                removePower = mpd;
            }
            else if (mpd.abilityRef.refName == "skill_expmon_dmgmonsters")
            {
                hasDmgMonsters = true;
            }
        }

        if (removePower != null)
        {
            mtd.monsterPowers.Remove(removePower);
        }

        if (!hasDmgMonsters)
        {
            MonsterPowerData dmgPassive = new MonsterPowerData();
            dmgPassive.abilityRef = GameMasterScript.masterAbilityList["skill_expmon_dmgmonsters"];
            mtd.monsterPowers.Add(dmgPassive);
        }

        MonsterPowerData specialDmgMonsters = new MonsterPowerData();
        specialDmgMonsters.abilityRef = GameMasterScript.masterAbilityList["skill_expmon_empowerkill_special"];
        mtd.monsterPowers.Add(specialDmgMonsters);
    }



}
