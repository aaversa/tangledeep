using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Various charts for things like monster stats, weapon power, expected health values
// Level : CV, CV : level... etc

public class ShieldBlockData
{
    public float blockChance;
    public float blockDamageReduction;
    public float physicalResist;
    public int physicalFlatOffset;    

    public ShieldBlockData(float bChance, float bAmount, int pOffset, float pResist)
    {
        blockChance = bChance;
        blockDamageReduction = bAmount;
        physicalFlatOffset = pOffset;
        physicalResist = pResist;
    }
}

public class BalanceData
{
    static bool initialized;
    public static float[,] playerMonsterRewardTable;

    /* public static Dictionary<string, int> boundsRangeForEnemyAbilitiesInNewGamePP = new Dictionary<string, int>()
    {
        { "skill_clawrake", 3 },
        { "skill_firebreath1", 5 },
        { "skill_smalllightningcircle", 2 },
        { "skill_energyburst", 3 },
        { "skill_laserclaw", 5 },
        { "skill_watercross", 3 },
    } */

    public static float[] LEVEL_TO_CV = new float[]
    {
        1.0f, // 0
        1.0f, // 1
        1.05f, // 2
        1.1f, // 3
        1.2f, // 4
        1.25f, // 5
        1.3f, // 6
        1.35f, // 7
        1.4f, // 8
        1.5f, // 9
        1.6f, // 10
        1.65f, // 1
        1.7f, // 12
        1.8f, // 13
        1.85f, // 14
        1.9f, // 15
        1.9f, // 16
        2.0f, // 17
        2.05f, // 18
        2.1f, // 19
        2.15f, // 20
        2.2f, // 21
        2.25f, // 22
        2.3f, // 23
        2.35f, // 24
        2.4f, // 25
        2.45f, // 26
        2.5f, // 27
        2.55f, // 28
        2.6f, // 29
        2.65f, // 30
        2.7f, // 31
        2.75f, // 32
        2.8f, // 33
        2.85f, // 34
        2.9f, // 35
        2.95f, // 36
        3.0f, // 37
    };

    public static Dictionary<int, string> monsterShieldsByMonsterLevel = new Dictionary<int, string>()
    {
        { 1, "exp_monshield1" },
        { 2, "exp_monshield1" },
        { 3, "exp_monshield1" },
        { 4, "exp_monshield2" },
        { 5, "exp_monshield2" },
        { 6, "exp_monshield2" },
        { 7, "exp_monshield3" },
        { 8, "exp_monshield3" },
        { 9, "exp_monshield3" },
        { 10, "exp_monshield4" },
        { 11, "exp_monshield4" },
        { 12, "exp_monshield4" },
        { 13, "exp_monshield5" },
        { 14, "exp_monshield5" },
        { 15, "exp_monshield5" },
        { 16, "exp_monshield5" },
        { 17, "exp_monshield5" },
        { 18, "exp_monshield5" },
        { 19, "exp_monshield5" },
        { 20, "exp_monshield5" }
    };

    public static Dictionary<int, string> quiverBaseModByRank = new Dictionary<int, string>()
    {
        { 1, "mm_quiver1" },
        { 2, "mm_quiver1" },
        { 3, "mm_quiver1" },
        { 4, "mm_quiver2" },
        { 5, "mm_quiver2" },
        { 6, "mm_quiver2" },
        { 7, "mm_quiver3" },
        { 8, "mm_quiver3" },
        { 9, "mm_quiver3" },
        { 10, "mm_quiver3" },
        { 11, "mm_quiver4" },
        { 12, "mm_quiver4" },
        { 13, "mm_quiver4" },
    };

    public static Dictionary<int, string> relicQuiverBaseModByRank = new Dictionary<int, string>()
    {
        { 1, "mm_quiver1" },
        { 2, "mm_quiver1" },
        { 3, "mm_quiver1" },
        { 4, "mm_quiver1" },
        { 5, "mm_quiver2" },
        { 6, "mm_quiver2" },
        { 7, "mm_quiver2" },
        { 8, "mm_quiver2" },
        { 9, "mm_quiver3" },
        { 10, "mm_quiver3" },
        { 11, "mm_quiver3" },
        { 12, "mm_quiver4" },
        { 13, "mm_quiver4" },
    };

    public static Dictionary<int, string> magicBookBaseModByrank = new Dictionary<int, string>()
    {
        { 1, "mm_magicbook1" },
        { 2, "mm_magicbook1" },
        { 3, "mm_magicbook2" },
        { 4, "mm_magicbook2" },
        { 5, "mm_magicbook3" },
        { 6, "mm_magicbook3" },
        { 7, "mm_magicbook4" },
        { 8, "mm_magicbook4" },
        { 9, "mm_magicbook4" },
        { 10, "mm_magicbook5" },
        { 11, "mm_magicbook5" },
        { 12, "mm_magicbook5" },
        { 13, "mm_magicbook5" },
    };

    public static Dictionary<int, string> relicMagicBookBaseModByrank = new Dictionary<int, string>()
    {
        { 1, "mm_magicbook1" },
        { 2, "mm_magicbook1" },
        { 3, "mm_magicbook1" },
        { 4, "mm_magicbook2" },
        { 5, "mm_magicbook2" },
        { 6, "mm_magicbook2" },
        { 7, "mm_magicbook3" },
        { 8, "mm_magicbook3" },
        { 9, "mm_magicbook3" },
        { 10, "mm_magicbook4" },
        { 11, "mm_magicbook4" },
        { 12, "mm_magicbook5" },
        { 13, "mm_magicbook5" },
    };

    public static Dictionary<int, ShieldBlockData> shieldBlockDataByRank = new Dictionary<int, ShieldBlockData>()
    {
        { 1, new ShieldBlockData(0.15f, 0.65f, 2, 1f) },
        { 2, new ShieldBlockData(0.16f, 0.65f, 3, 1f) },
        { 3, new ShieldBlockData(0.18f, 0.65f, 4, 1f) },
        { 4, new ShieldBlockData(0.2f, 0.65f, 5, 1f) },
        { 5, new ShieldBlockData(0.21f, 0.65f, 6, 1f) },
        { 6, new ShieldBlockData(0.23f, 0.65f, 7, 1f) },
        { 7, new ShieldBlockData(0.24f, 0.65f, 9, 1f) },
        { 8, new ShieldBlockData(0.26f, 0.65f, 10, 0.98f) },
        { 9, new ShieldBlockData(0.28f, 0.65f, 11, 0.96f) },
        { 10, new ShieldBlockData(0.3f, 0.65f, 13, 0.95f) },
        { 11, new ShieldBlockData(0.32f, 0.65f, 14, 0.93f) },
        { 12, new ShieldBlockData(0.33f, 0.6f, 15, 0.91f) },
        { 13, new ShieldBlockData(0.35f, 0.6f, 16, 0.89f) }
    };

    public static Dictionary<int, ShieldBlockData> relicShieldBlockDataByRank = new Dictionary<int, ShieldBlockData>()
    {
        { 1, new ShieldBlockData(0.15f, 0.7f, 1, 1f) },
        { 2, new ShieldBlockData(0.16f, 0.7f, 2, 1f) },
        { 3, new ShieldBlockData(0.18f, 0.7f, 3, 1f) },
        { 4, new ShieldBlockData(0.2f, 0.7f, 3, 1f) },
        { 5, new ShieldBlockData(0.21f, 0.65f, 3, 1f) },
        { 6, new ShieldBlockData(0.22f, 0.65f, 4, 1f) },
        { 7, new ShieldBlockData(0.23f, 0.65f, 5, 1f) },
        { 8, new ShieldBlockData(0.24f, 0.65f, 6, 0.98f) },
        { 9, new ShieldBlockData(0.25f, 0.65f, 7, 0.96f) },
        { 10, new ShieldBlockData(0.27f, 0.65f, 8, 0.95f) },
        { 11, new ShieldBlockData(0.29f, 0.65f, 10, 0.93f) },
        { 12, new ShieldBlockData(0.31f, 0.6f, 12, 0.91f) },
        { 13, new ShieldBlockData(0.33f, 0.6f, 14, 0.89f) }
    };

    public static Dictionary<ArmorTypes, Dictionary<int, int>> relicArmorDodgeAmountsByRank = new Dictionary<ArmorTypes, Dictionary<int, int>>()
    {
        { ArmorTypes.LIGHT, new Dictionary<int, int>() {
            { 1, 5 },
            { 2, 6 },
            { 3, 8 },
            { 4, 9 },
            { 5, 10 },
            { 6, 11 },
            { 7, 13 },
            { 8, 14 },
            { 9, 15 },
            { 10, 16 },
            { 11, 17 },
            { 12, 19 },
            { 13, 21 }
        }
        },

        { ArmorTypes.MEDIUM, new Dictionary<int, int>() {
            { 1, 1 },
            { 2, 2 },
            { 3, 3 },
            { 4, 3 },
            { 5, 4 },
            { 6, 4 },
            { 7, 4 },
            { 8, 5 },
            { 9, 5 },
            { 10, 6 },
            { 11, 6 },
            { 12, 7 },
            { 13, 7 }
        }
        },
    };

    public static Dictionary<ArmorTypes, Dictionary<int, int>> armorDodgeAmountsByRank = new Dictionary<ArmorTypes, Dictionary<int, int>>()
    {
        { ArmorTypes.LIGHT, new Dictionary<int, int>() {
            { 1, 5 },
            { 2, 6 },
            { 3, 8 },
            { 4, 10 },
            { 5, 11 },
            { 6, 12 },
            { 7, 14 },
            { 8, 15 },
            { 9, 17 },
            { 10, 18 },
            { 11, 20 },
            { 12, 22 },
            { 13, 24 }
        }
        },

        { ArmorTypes.MEDIUM, new Dictionary<int, int>() {
            { 1, 2 },
            { 2, 2 },
            { 3, 3 },
            { 4, 3 },
            { 5, 4 },
            { 6, 4 },
            { 7, 5 },
            { 8, 5 },
            { 9, 6 },
            { 10, 7 },
            { 11, 8 },
            { 12, 9 },
            { 13, 9 }
        }
        },
    };

    public static Dictionary<ArmorTypes, Dictionary<int, int>> relicArmorResistPhysicalOffsetsByRank = new Dictionary<ArmorTypes, Dictionary<int, int>>()
    {
        { ArmorTypes.MEDIUM, new Dictionary<int, int>() {
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 3 },
            { 5, 4 },
            { 6, 5 },
            { 7, 6 },
            { 8, 7 },
            { 9, 8 },
            { 10, 8 },
            { 11, 9 },
            { 12, 9 },
            { 13, 10 }
        }
        },

        { ArmorTypes.HEAVY, new Dictionary<int, int>() {
            { 1, 2 },
            { 2, 3 },
            { 3, 4 },
            { 4, 6 },
            { 5, 7 },
            { 6, 8 },
            { 7, 10 },
            { 8, 11 },
            { 9, 12 },
            { 10, 14 },
            { 11, 16 },
            { 12, 17 },
            { 13, 18 }
        }
        },
    };

    public static Dictionary<ArmorTypes, Dictionary<int, int>> armorResistPhysicalOffsetsByRank = new Dictionary<ArmorTypes, Dictionary<int, int>>()
    {
        { ArmorTypes.MEDIUM, new Dictionary<int, int>() {
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 3 },
            { 5, 4 },
            { 6, 5 },
            { 7, 6 },
            { 8, 7 },
            { 9, 8 },
            { 10, 9 },
            { 11, 10 },
            { 12, 11 },
            { 13, 13 }
        }
        },

        { ArmorTypes.HEAVY, new Dictionary<int, int>() {
            { 1, 2 },
            { 2, 3 },
            { 3, 4 },
            { 4, 6 },
            { 5, 7 },
            { 6, 8 },
            { 7, 10 },
            { 8, 11 },
            { 9, 12 },
            { 10, 14 },
            { 11, 16 },
            { 12, 18 },
            { 13, 20 }
        }
        },
    };

    public static Dictionary<ArmorTypes, Dictionary<int, float>> armorPhysicalResistByRank = new Dictionary<ArmorTypes, Dictionary<int, float>>()
    {
        { ArmorTypes.MEDIUM, new Dictionary<int, float>() {
            { 1, 0.95f },
            { 2, 0.94f },
            { 3, 0.92f },
            { 4, 0.9f },
            { 5, 0.89f },
            { 6, 0.88f },
            { 7, 0.86f },
            { 8, 0.85f },
            { 9, 0.83f },
            { 10, 0.82f },
            { 11, 0.8f },
            { 12, 0.78f },
            { 13, 0.75f }
        }
        },

        { ArmorTypes.HEAVY, new Dictionary<int, float>() {
            { 1, 0.9f },
            { 2, 0.87f },
            { 3, 0.85f },
            { 4, 0.83f },
            { 5, 0.81f },
            { 6, 0.79f },
            { 7, 0.77f },
            { 8, 0.75f },
            { 9, 0.73f },
            { 10, 0.72f },
            { 11, 0.69f },
            { 12, 0.66f },
            { 13, 0.63f }
        }
        },
    };

    public static Dictionary<ArmorTypes, Dictionary<int, float>> relicArmorPhysicalResistByRank = new Dictionary<ArmorTypes, Dictionary<int, float>>()
    {
        { ArmorTypes.MEDIUM, new Dictionary<int, float>() {
            { 1, 0.95f },
            { 2, 0.94f },
            { 3, 0.92f },
            { 4, 0.9f },
            { 5, 0.89f },
            { 6, 0.88f },
            { 7, 0.86f },
            { 8, 0.85f },
            { 9, 0.84f },
            { 10, 0.83f },
            { 11, 0.82f },
            { 12, 0.81f },
            { 13, 0.8f }
        }
        },

        { ArmorTypes.HEAVY, new Dictionary<int, float>() {
            { 1, 0.9f },
            { 2, 0.87f },
            { 3, 0.85f },
            { 4, 0.83f },
            { 5, 0.81f },
            { 6, 0.8f },
            { 7, 0.79f },
            { 8, 0.78f },
            { 9, 0.77f },
            { 10, 0.75f },
            { 11, 0.73f },
            { 12, 0.72f },
            { 13, 0.7f }
        }
        },
    };

    public static Dictionary<WeaponTypes, Dictionary<int, float>> weaponPowersByRank = new Dictionary<WeaponTypes, Dictionary<int, float>>()
    {
        { WeaponTypes.AXE, new Dictionary<int,float>()
        {
            {  1, 16f },
            {  2, 17.3f },
            {  3, 18.7f },
            {  4, 20.1f },
            {  5, 21.6f },
            {  6, 23.2f },
            {  7, 24.9f },
            {  8, 26.7f },
            {  9, 28.5f },
            {  10, 30.2f },
            {  11, 32f },
            {  12, 33.9f },
            { 13, 36f }
        }
        },

        { WeaponTypes.SPEAR, new Dictionary<int,float>()
        {
            {  1, 16f },
            {  2, 17.3f },
            {  3, 18.7f },
            {  4, 20.1f },
            {  5, 21.6f },
            {  6, 23.2f },
            {  7, 24.9f },
            {  8, 26.7f },
            {  9, 28.5f },
            {  10, 30.2f },
            {  11, 32f },
            {  12, 33.9f },
            { 13, 36f }
        }
        },

        { WeaponTypes.STAFF, new Dictionary<int,float>()
        {
            {  1, 15.5f },
            {  2, 17f },
            {  3, 18.5f },
            {  4, 19.7f },
            {  5, 21f },
            {  6, 22.7f },
            {  7, 24.4f },
            {  8, 26.2f },
            {  9, 28f },
            {  10, 29.5f },
            { 11, 31f },
            { 12, 32.4f },
            { 13, 33.9f }
        }
        },

        { WeaponTypes.MACE, new Dictionary<int,float>()
        {
            {  1, 17.5f },
            {  2, 18.9f },
            {  3, 20.3f },
            {  4, 21.8f },
            {  5, 23.4f },
            {  6, 25.1f },
            {  7, 26.9f },
            {  8, 28.8f },
            {  9, 30.7f },
            {  10, 32.7f },
            {  11, 34.8f },
            {  12, 36.9f },
            {  13, 39f }
        }
        },

        { WeaponTypes.SWORD, new Dictionary<int,float>()
        {
            {  1, 17f },
            {  2, 18.4f },
            {  3, 19.8f },
            {  4, 21.3f },
            {  5, 22.8f },
            {  6, 24.5f },
            {  7, 26.2f },
            {  8, 28.1f },
            {  9, 30f },
            {  10, 32f },
            {  11, 34f },
            {  12, 35.5f },
            {  13, 37f }
        }
        },

        { WeaponTypes.DAGGER, new Dictionary<int,float>()
        {
            {  1, 14.5f },
            {  2, 15.8f },
            {  3, 17.2f },
            {  4, 18.5f },
            {  5, 19.8f },
            {  6, 21.3f },
            {  7, 22.9f },
            {  8, 24.6f },
            {  9, 26.3f },
            {  10, 27.6f },
            {  11, 29f },
            {  12, 30.5f },
            {  13, 32f }
        }
        },

        { WeaponTypes.CLAW, new Dictionary<int,float>()
        {
            {  1, 14.5f },
            {  2, 15.8f },
            {  3, 17.2f },
            {  4, 18.5f },
            {  5, 19.8f },
            {  6, 21.3f },
            {  7, 22.9f },
            {  8, 24.6f },
            {  9, 26.3f },
            {  10, 27.6f },
            {  11, 29f },
            {  12, 30.5f },
            {  13, 32f }
        }
        },

        { WeaponTypes.BOW, new Dictionary<int,float>()
        {
            {  1, 14f },
            {  2, 15.1f },
            {  3, 16.2f },
            {  4, 17.3f },
            {  5, 18.5f },
            {  6, 19.8f },
            {  7, 21.1f },
            {  8, 22.5f },
            {  9, 23.9f },
            {  10, 25.4f },
            {  11, 26.9f },
            {  12, 28.4f },
            {  13, 30f }
        }
        },

        { WeaponTypes.NATURAL, new Dictionary<int,float>()
        {
            {  1, 10f },
            {  2, 10f },
            {  3, 10f },
            {  4, 10f },
            {  5, 10f },
            {  6, 10f },
            {  7, 10f },
            {  8, 10f },
            {  9, 10f },
            {  10, 10f },
            {  11, 10f },
            {  12, 10f },
            { 13, 10f }
        }
        },

        { WeaponTypes.WHIP, new Dictionary<int,float>()
        {
            {  1, 14f },
            {  2, 15.1f },
            {  3, 16.2f },
            {  4, 17.3f },
            {  5, 18.5f },
            {  6, 19.8f },
            {  7, 21.1f },
            {  8, 22.5f },
            {  9, 23.9f },
            {  10, 25.4f },
            {  11, 26.9f },
            {  12, 28.4f },
            {  13, 30f }
        }
        }

    };

    public static Dictionary<int, float> DICT_LEVEL_TO_CV = new Dictionary<int, float>()
    {
        { 1, 1.0f },
        { 2, 1.05f },
        { 3, 1.1f },
        { 4, 1.2f },
        { 5, 1.25f },
        { 6, 1.35f },
        { 7, 1.4f },
        { 8, 1.45f },
        { 9, 1.55f },
        { 10, 1.6f },
        { 11, 1.7f },
        { 12, 1.8f },
        { 13, 1.85f },
        { 14, 1.9f },
        { 15, 1.9f },
        { 16, 1.95f },
        { 17, 2.0f },
        { 18, 2.05f },
        { 19, 2.1f },
        { 20, 2.15f },
    };

    public static float[] playerHealthCurve = {
        170f,
        210f,
        260f,
        330f,
        420f,
        510f,
        600f,
        695f,
        830f,
        980f,
        1145f,
        1325f,
        1520f,
        1730f,
        1950f, // level 15
        2100f,
        2300f,
        2500f,
        2750f,
        3000f
        };

    public static float[] playerToughnessHealthCurve = {
        200f,
        240f,
        300f,
        390f,
        480f,
        570f,
        690f,
        830f,
        990f,
        1160f,
        1355f,
        1565f,
        1790f,
        2030f,
        2250f, // level 15
        2400f,
        2650f,
        2875f,
        3150f,
        3450f
        };

    public static float GetExpectedMonsterStatByLevel(int level, StatTypes stat)
    {
        if (level >= expectedStatValues.Length)
        {
            level = expectedStatValues.Length - 1;
        }
        switch(stat)
        {
            case StatTypes.CHARGETIME:
                return expectedStatValues[(level * 3) + 1];
            case StatTypes.ACCURACY:
                return expectedStatValues[(level * 3) + 2];
            case StatTypes.RANDOM_CORE:
            case StatTypes.RANDOM_NONRESOURCE:
            case StatTypes.STRENGTH:
            case StatTypes.SWIFTNESS:
            case StatTypes.GUILE:
            case StatTypes.DISCIPLINE:
            case StatTypes.SPIRIT:
            default:
                return expectedStatValues[(level * 3)];
        }
    }

    // CORESTATS, CHARGETIME, ACCURACY
    public static float[] expectedStatValues =
    {
        0f, 0f, 0f, // Level 0, doesn't matter
        18f, 85f, 80f,
        20f, 90f, 82f,
        23f, 90f, 90f,
        25f, 95f, 95f,
        28f, 95f, 95f, // Level 5
        30f, 95f, 100f,
        33f, 95f, 100f,
        35f, 99f, 100f, // Level 8
        38f, 99f, 100f,
        40f, 99f, 100f, // Level 10
        43f, 99f, 100f,
        45f, 99f, 100f,
        48f, 99f, 100f,
        50f, 99f, 100f,
        53f, 99f, 100f, // Level 15
        55f, 99f, 100f,
        58f, 99f, 100f,
        60f, 99f, 100f,
        63f, 99f, 100f,
        65f, 99f, 100f, // Level 20
        68f, 99f, 100f,
        71f, 99f, 100f,
        75f, 99f, 100f,
        79f, 99f, 100f,
        83f, 99f, 100f, // Level 25
        87f, 99f, 100f, // Level 26
        92f, 99f, 100f, // Level 27
        96f, 99f, 100f, // Level 28
        101f, 99f, 100f, // Level 29
        106f, 99f, 100f, // Level 30
        112f, 99f, 100f, // Level 31
        118f, 99f, 100f, // Level 32
        124f, 99f, 100f, // Level 33
        130f, 99f, 100f, // Level 34
        136f, 99f, 100f, // Level 35
        142f, 99f, 100f, // Level 36
        150f, 99f, 100f, // Level 37

    };

    public static float[] expectedMonsterHealth =
    {
        95f, //0
        95f, // 1
        130f,
        170f,
        210f,
        280f, //5
        340f,
        420f,
        510f,
        600f,
        710f, // 10
        850f,
        1000f,
        1200f,
        1450f,
        1700f, // 15
        2000f,
        2300f, // 17
        2600f,
        2900f,
        3200f, // 20
        3500f, // 21
        3900f, // 22
        4300f, // 23
        4800f, // 24
        5300f, // 25
        5800f, // 26
        6400f, // 27
        7000f, // 28
        7600f, // 29
        8300f, // 30
        9000f, // 31
        10000f, // 32
        11000f, // 33
        12000f, // 34
        13000f, // 35
        14000f, // 36
        15000f, // 37
    };

    public const float MAX_CORRAL_PET_HEALTH = 4000f;
    public const float MAX_CORRAL_PET_HEALTH_NG1 = 6000f;
    public const float MAX_CORRAL_PET_HEALTH_NG2 = 8000f;

    public static int[] monsterLevelByPlayerLevel =
    {
        0, // 0
        1, // Player level 1
        2, // Player level 2
        3, // Player level 3
        3, // Player level 4
        4, // Player level 5
        5, // Player level 6
        6, // Player level 7
        7, // Player level 8
        8, // Player level 9
        9, // Player level 10
        9, // Player level 11
        10, // Player level 12
        11, // Player level 13
        12, // Player level 14
        12, // Player level 15
        13, // ??? Player level 16
        14, // Plvl 17
        15, // Plvl 18
        16, // Plvl 19
        17, // Plvl 20
    };

    public static float[] expectedMonsterWeaponPower =
    {
        3f, // 0
        4f, // 1
        5.3f, // 2
        7.2f, // 3
        8.5f,
        11.4f, // 5
        17.4f,
        32f, // 7
        36f,
        60f, // 9
        64f,
        82f, // 11
        93f,
        105f, // 13
        115f,
        123f, // 15
        136f,
        150f, // 17
        165f,
        173, // 19
        185, // 20
        195, // 21
        205, // 22
        216, // 23
        227, // 24
        240, // 25
        253, // 26
        266, // 27
        270, // 28
        284, // 29
        298, // 30
        312, // 31
        326, // 32
        344, // 33
        362, // 34
        380, // 35
        400, // 36
        420, // 37
    };

    public static int GetMaxChampionsByCV(float cv)
    {
        if (cv <= 1.1f) return 1;
        if (cv > 1.1f && cv < 1.4f) return 2;
        if (cv >= 1.4f && cv < 1.8f) return 3;
        if (cv >= 1.8f && cv < 2.0f) return 4;
        return 5;
    }

    public static ActorTable GetSpawnTableByCV(float cv)
    {
        float multCV = cv * 100f;
        multCV = (Mathf.Round(multCV / 5.0f) * 5);
        string tableString = "cv" + ((int)multCV); // should yield: cv100, 105, 110, etc.
        ActorTable spawnTable;
        if (!GameMasterScript.masterSpawnTableList.TryGetValue(tableString, out spawnTable))
        {
            spawnTable = GameMasterScript.masterSpawnTableList["cv100"];
            Debug.Log("Couldn't find spawn table for cv: " + cv);
        }
        return spawnTable;
    }

    public static int GetExpectedPlayerLevelByCV(float cv)
    {
        if (cv <= 1.0f) return 1;
        if (cv > 1.0f && cv < 1.1f) return 2;
        if (cv >= 1.1f && cv < 1.2f) return 3;
        if (cv >= 1.2f && cv < 1.25f) return 4;
        if (cv >= 1.25f && cv < 1.3f) return 5;
        if (cv >= 1.3f && cv < 1.4f) return 6;
        if (cv >= 1.4f && cv < 1.45f) return 7;
        if (cv >= 1.45f && cv < 1.55f) return 8;
        if (cv >= 1.55f && cv < 1.65f) return 9;
        if (cv >= 1.65f && cv < 1.7f) return 10;
        if (cv >= 1.7f && cv < 1.75f) return 11;
        if (cv >= 1.75f && cv < 1.8f) return 12;
        if (cv >= 1.8f && cv < 1.85f) return 13;
        if (cv >= 1.85f && cv < 1.9f) return 14;
        if (cv >= 1.9f && cv < 1.95f) return 15;
        if (cv >= 1.95f && cv < 2.0f) return 16;
        if (cv >= 2.0f && cv < 2.05f) return 17;
        if (cv >= 2.05f && cv < 2.1f) return 18;
        if (cv >= 2.1f && cv < 2.15f) return 19;
        if (cv >= 2.15f) return 20;

        return 1;
    }

    /// <summary>
    /// Returns the highest possible CV allowed for an item based on various factors of player's game state / DLC
    /// </summary>
    /// <returns></returns>
    public static float GetMaxChallengeValueForItems()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return 2.2f;
        }
        return 1.9f;
    }

    public static int ConvertChallengeValueToRank(float baseCV)
    {

        if ((baseCV >= 1.0f && baseCV < 1.1f) || baseCV > 100f)
        {
            return 1;
        }
        else if (baseCV >= 1.1f && baseCV < 1.2f)
        {
            return 2;
        }
        else if (baseCV >= 1.2f && baseCV < 1.3f)
        {
            return 3;
        }
        else if (baseCV >= 1.3f && baseCV < 1.4f)
        {
            return 4;
        }
        else if (baseCV >= 1.4f && baseCV < 1.5f)
        {
            return 5;
        }
        else if ((baseCV >= 1.5f) && (baseCV < 1.6f))
        {
            return 6;
        }
        else if ((baseCV >= 1.6f) && (baseCV < 1.7f))
        {
            return 7;
        }
        else if ((baseCV >= 1.7f) && (baseCV < 1.8f))
        {
            return 8;
        }
        else if ((baseCV >= 1.8f) && (baseCV < 1.9f))
        {
            return 9;
        }
        else if ((baseCV >= 1.9f) && (baseCV < 2f))
        {
            return 10;
        }
        else if ((baseCV >= 2f) && (baseCV < 2.1f))
        {
            return 11;
        }
        else if ((baseCV >= 2.1f) && (baseCV < 2.2f))
        {
            return 12;
        }
        else
        {
            return 13;
        }
    }

    public static int GetMonsterLevelByCV(float cv, bool uncapped)
    {
        if (cv <= 1.0f) return 1;
        if (cv > 1.0f && cv <= 1.05f) return 2;
        if (cv > 1.05f && cv <= 1.1f) return 3;
        if (cv > 1.1f && cv <= 1.2f) return 4;
        if (cv > 1.2f && cv <= 1.25f) return 5;
        if (cv > 1.25f && cv <= 1.35f) return 6;
        if (cv > 1.35f && cv <= 1.4f) return 7;
        if (cv > 1.4f && cv <= 1.45f) return 8;
        if (cv > 1.45f && cv <= 1.55f) return 9;
        if (cv > 1.55f && cv <= 1.6f) return 10;
        if (cv > 1.6f && cv <= 1.7f) return 11;
        if (cv > 1.7f && cv <= 1.8f) return 12;
        if (cv > 1.8f && cv <= 1.85f) return 13;
        if (cv > 1.85f && cv <= 1.9f) return 14;
        if (cv > 1.9f && cv <= 1.95f) return 15;
        if (cv > 1.95 && cv <= 2.0f) return 16;
        if (cv > 2.0f && cv <= 2.05f) return 17;
        if (cv > 2.05f && cv <= 2.1f) return 18;
        if (cv > 2.1f && cv <= 2.15f) return 19;
        else if (!uncapped)
        {
            return 20;
        }
        else
        {
            if (cv > 2.15f && cv <= 2.2f) return 20;
            if (cv > 2.2f && cv <= 2.25f) return 21;
            if (cv > 2.25f && cv <= 2.3f) return 22;
            if (cv > 2.3f && cv <= 2.35f) return 23;
            if (cv > 2.35f && cv <= 2.4f) return 24;
            if (cv > 2.40f && cv <= 2.45f) return 25;
            if (cv > 2.45f && cv <= 2.5f) return 26;
            if (cv > 2.50f && cv <= 2.55f) return 27;
            if (cv > 2.55f && cv <= 2.6f) return 28;
            if (cv > 2.6f && cv <= 2.65f) return 29;
            if (cv > 2.65f && cv <= 2.7f) return 30;
            if (cv > 2.7f && cv <= 2.75f) return 31;
            if (cv > 2.75f && cv <= 2.8f) return 32;
            if (cv > 2.8f && cv <= 2.85f) return 33;
            if (cv > 2.85f && cv <= 2.9f) return 34;
            if (cv > 2.9f && cv <= 2.95f) return 35;
            if (cv > 2.95f && cv <= 3f) return 36;
            return 37;
        }
    }

    public static void Initialize()
    {
        if (initialized) return;       

        playerMonsterRewardTable = new float[26, 26];
        // X = Current player level, Y = difference.
        playerMonsterRewardTable[1, 0] = 1.0f;

        playerMonsterRewardTable[2, 0] = 1.0f; // Player 2, monster 2
        playerMonsterRewardTable[2, 1] = 0.66f; // Player 2, monster 1

        playerMonsterRewardTable[3, 0] = 1.0f; // Player 3, monster 3
        playerMonsterRewardTable[3, 1] = 0.7f;
        playerMonsterRewardTable[3, 2] = 0.4f;

        playerMonsterRewardTable[4, 0] = 1.0f; // Player 4, monster 4
        playerMonsterRewardTable[4, 1] = 0.85f;
        playerMonsterRewardTable[4, 2] = 0.4f;
        playerMonsterRewardTable[4, 3] = 0.15f; // Player 4, monster 1

        playerMonsterRewardTable[5, 0] = 1.0f;
        playerMonsterRewardTable[5, 1] = 0.9f;
        playerMonsterRewardTable[5, 2] = 0.65f; // Monster 3
        playerMonsterRewardTable[5, 3] = 0.2f; // Player 5, monster 2
        playerMonsterRewardTable[5, 4] = 0.0f;

        playerMonsterRewardTable[6, 0] = 1.0f;
        playerMonsterRewardTable[6, 1] = 0.9f;
        playerMonsterRewardTable[6, 2] = 0.7f; // Monster 4
        playerMonsterRewardTable[6, 3] = 0.35f; // Monster 3
        playerMonsterRewardTable[6, 4] = 0.05f; // Player 6, monster 2
        playerMonsterRewardTable[6, 5] = 0.0f;

        playerMonsterRewardTable[7, 0] = 1.0f;
        playerMonsterRewardTable[7, 1] = 0.95f;
        playerMonsterRewardTable[7, 2] = 0.8f;
        playerMonsterRewardTable[7, 3] = 0.6f; // Monster 4
        playerMonsterRewardTable[7, 4] = 0.18f; // Monster 3
        playerMonsterRewardTable[7, 5] = 0.0f;
        playerMonsterRewardTable[7, 6] = 0.0f;

        playerMonsterRewardTable[8, 0] = 1.0f;
        playerMonsterRewardTable[8, 1] = 1.0f;
        playerMonsterRewardTable[8, 2] = 0.9f;
        playerMonsterRewardTable[8, 3] = 0.65f;
        playerMonsterRewardTable[8, 4] = 0.35f; // Monster 4
        playerMonsterRewardTable[8, 5] = 0.0f;
        playerMonsterRewardTable[8, 6] = 0.0f;
        playerMonsterRewardTable[8, 7] = 0.0f;

        playerMonsterRewardTable[9, 0] = 1.0f;
        playerMonsterRewardTable[9, 1] = 1.0f;
        playerMonsterRewardTable[9, 2] = 0.95f;
        playerMonsterRewardTable[9, 3] = 0.8f;
        playerMonsterRewardTable[9, 4] = 0.5f;
        playerMonsterRewardTable[9, 5] = 0.25f; // Monster 4
        playerMonsterRewardTable[9, 6] = 0.0f;
        playerMonsterRewardTable[9, 7] = 0.0f;
        playerMonsterRewardTable[9, 8] = 0.0f;

        playerMonsterRewardTable[10, 0] = 1.0f;
        playerMonsterRewardTable[10, 1] = 1.0f;
        playerMonsterRewardTable[10, 2] = 0.95f;
        playerMonsterRewardTable[10, 3] = 0.85f;
        playerMonsterRewardTable[10, 4] = 0.7f;
        playerMonsterRewardTable[10, 5] = 0.35f; // Monster 5
        playerMonsterRewardTable[10, 6] = 0.1f; // Monster 4
        playerMonsterRewardTable[10, 7] = 0.0f;
        playerMonsterRewardTable[10, 8] = 0.0f;
        playerMonsterRewardTable[10, 9] = 0.0f;

        playerMonsterRewardTable[11, 0] = 1.0f;
        playerMonsterRewardTable[11, 1] = 1.0f;
        playerMonsterRewardTable[11, 2] = 0.95f;
        playerMonsterRewardTable[11, 3] = 0.9f;
        playerMonsterRewardTable[11, 4] = 0.75f;
        playerMonsterRewardTable[11, 5] = 0.6f; // Monster 6
        playerMonsterRewardTable[11, 6] = 0.15f; // Monster 5
        playerMonsterRewardTable[11, 7] = 0.0f;
        playerMonsterRewardTable[11, 8] = 0.0f;
        playerMonsterRewardTable[11, 9] = 0.0f;
        playerMonsterRewardTable[11, 10] = 0.0f;

        playerMonsterRewardTable[12, 0] = 1.0f;
        playerMonsterRewardTable[12, 1] = 1.0f;
        playerMonsterRewardTable[12, 2] = 1.0f;
        playerMonsterRewardTable[12, 3] = 0.9f;
        playerMonsterRewardTable[12, 4] = 0.8f;
        playerMonsterRewardTable[12, 5] = 0.6f;
        playerMonsterRewardTable[12, 6] = 0.3f; // Monster 6
        playerMonsterRewardTable[12, 7] = 0.0f;
        playerMonsterRewardTable[12, 8] = 0.0f;
        playerMonsterRewardTable[12, 9] = 0.0f;
        playerMonsterRewardTable[12, 10] = 0.0f;
        playerMonsterRewardTable[12, 11] = 0.0f;

        playerMonsterRewardTable[13, 0] = 1.0f;
        playerMonsterRewardTable[13, 1] = 1.0f;
        playerMonsterRewardTable[13, 2] = 1.0f;
        playerMonsterRewardTable[13, 3] = 0.9f;
        playerMonsterRewardTable[13, 4] = 0.8f;
        playerMonsterRewardTable[13, 5] = 0.65f;
        playerMonsterRewardTable[13, 6] = 0.45f;
        playerMonsterRewardTable[13, 7] = 0.15f; // Monster 6
        playerMonsterRewardTable[13, 8] = 0.0f;
        playerMonsterRewardTable[13, 9] = 0.0f;
        playerMonsterRewardTable[13, 10] = 0.0f;
        playerMonsterRewardTable[13, 11] = 0.0f;
        playerMonsterRewardTable[13, 12] = 0.0f;

        playerMonsterRewardTable[14, 0] = 1.0f;
        playerMonsterRewardTable[14, 1] = 1.0f;
        playerMonsterRewardTable[14, 2] = 1.0f;
        playerMonsterRewardTable[14, 3] = 0.9f;
        playerMonsterRewardTable[14, 4] = 0.8f;
        playerMonsterRewardTable[14, 5] = 0.6f;
        playerMonsterRewardTable[14, 6] = 0.4f;
        playerMonsterRewardTable[14, 7] = 0.25f; // Monster 7
        playerMonsterRewardTable[14, 8] = 0.0f;
        playerMonsterRewardTable[14, 9] = 0.0f;
        playerMonsterRewardTable[14, 10] = 0.0f;
        playerMonsterRewardTable[14, 11] = 0.0f;
        playerMonsterRewardTable[14, 12] = 0.0f;
        playerMonsterRewardTable[14, 13] = 0.0f;

        playerMonsterRewardTable[15, 0] = 1.0f;
        playerMonsterRewardTable[15, 1] = 1.0f;
        playerMonsterRewardTable[15, 2] = 1.0f;
        playerMonsterRewardTable[15, 3] = 0.9f;
        playerMonsterRewardTable[15, 4] = 0.8f;
        playerMonsterRewardTable[15, 5] = 0.6f;
        playerMonsterRewardTable[15, 6] = 0.4f;
        playerMonsterRewardTable[15, 7] = 0.2f;
        playerMonsterRewardTable[15, 8] = 0.1f;
        playerMonsterRewardTable[15, 9] = 0.0f;
        playerMonsterRewardTable[15, 10] = 0.0f;
        playerMonsterRewardTable[15, 11] = 0.0f;
        playerMonsterRewardTable[15, 12] = 0.0f;
        playerMonsterRewardTable[15, 13] = 0.0f;
        playerMonsterRewardTable[15, 14] = 0.0f;

        playerMonsterRewardTable[16, 0] = 1.0f;
        playerMonsterRewardTable[16, 1] = 1.0f;
        playerMonsterRewardTable[16, 2] = 1.0f;
        playerMonsterRewardTable[16, 3] = 0.9f;
        playerMonsterRewardTable[16, 4] = 0.8f;
        playerMonsterRewardTable[16, 5] = 0.6f;
        playerMonsterRewardTable[16, 6] = 0.4f;
        playerMonsterRewardTable[16, 7] = 0.2f;
        playerMonsterRewardTable[16, 8] = 0.1f;
        playerMonsterRewardTable[16, 9] = 0.0f;
        playerMonsterRewardTable[16, 10] = 0.0f;
        playerMonsterRewardTable[16, 11] = 0.0f;
        playerMonsterRewardTable[16, 12] = 0.0f;
        playerMonsterRewardTable[16, 13] = 0.0f;
        playerMonsterRewardTable[16, 14] = 0.0f;
        playerMonsterRewardTable[16, 15] = 0.0f;

        playerMonsterRewardTable[17, 0] = 1.0f;
        playerMonsterRewardTable[17, 1] = 1.0f;
        playerMonsterRewardTable[17, 2] = 1.0f;
        playerMonsterRewardTable[17, 3] = 0.9f;
        playerMonsterRewardTable[17, 4] = 0.8f;
        playerMonsterRewardTable[17, 5] = 0.6f;
        playerMonsterRewardTable[17, 6] = 0.4f;
        playerMonsterRewardTable[17, 7] = 0.2f;
        playerMonsterRewardTable[17, 8] = 0.1f;
        playerMonsterRewardTable[17, 9] = 0.0f;
        playerMonsterRewardTable[17, 10] = 0.0f;
        playerMonsterRewardTable[17, 11] = 0.0f;
        playerMonsterRewardTable[17, 12] = 0.0f;
        playerMonsterRewardTable[17, 13] = 0.0f;
        playerMonsterRewardTable[17, 14] = 0.0f;
        playerMonsterRewardTable[17, 15] = 0.0f;
        playerMonsterRewardTable[17, 16] = 0.0f;

        playerMonsterRewardTable[18, 0] = 1.0f;
        playerMonsterRewardTable[18, 1] = 1.0f;
        playerMonsterRewardTable[18, 2] = 1.0f;
        playerMonsterRewardTable[18, 3] = 0.9f;
        playerMonsterRewardTable[18, 4] = 0.8f;
        playerMonsterRewardTable[18, 5] = 0.6f;
        playerMonsterRewardTable[18, 6] = 0.4f;
        playerMonsterRewardTable[18, 7] = 0.2f;
        playerMonsterRewardTable[18, 8] = 0.1f;
        playerMonsterRewardTable[18, 9] = 0.0f;
        playerMonsterRewardTable[18, 10] = 0.0f;
        playerMonsterRewardTable[18, 11] = 0.0f;
        playerMonsterRewardTable[18, 12] = 0.0f;
        playerMonsterRewardTable[18, 13] = 0.0f;
        playerMonsterRewardTable[18, 14] = 0.0f;
        playerMonsterRewardTable[18, 15] = 0.0f;
        playerMonsterRewardTable[18, 16] = 0.0f;
        playerMonsterRewardTable[18, 17] = 0.0f;

        playerMonsterRewardTable[19, 0] = 1.0f;
        playerMonsterRewardTable[19, 1] = 1.0f;
        playerMonsterRewardTable[19, 2] = 1.0f;
        playerMonsterRewardTable[19, 3] = 0.9f;
        playerMonsterRewardTable[19, 4] = 0.8f;
        playerMonsterRewardTable[19, 5] = 0.6f;
        playerMonsterRewardTable[19, 6] = 0.4f;
        playerMonsterRewardTable[19, 7] = 0.2f;
        playerMonsterRewardTable[19, 8] = 0.1f;
        playerMonsterRewardTable[19, 9] = 0.0f;
        playerMonsterRewardTable[19, 10] = 0.0f;
        playerMonsterRewardTable[19, 11] = 0.0f;
        playerMonsterRewardTable[19, 12] = 0.0f;
        playerMonsterRewardTable[19, 13] = 0.0f;
        playerMonsterRewardTable[19, 14] = 0.0f;
        playerMonsterRewardTable[19, 15] = 0.0f;
        playerMonsterRewardTable[19, 16] = 0.0f;
        playerMonsterRewardTable[19, 17] = 0.0f;
        playerMonsterRewardTable[19, 18] = 0.0f;

        playerMonsterRewardTable[20, 0] = 1.0f;
        playerMonsterRewardTable[20, 1] = 1.0f;
        playerMonsterRewardTable[20, 2] = 1.0f;
        playerMonsterRewardTable[20, 3] = 0.9f;
        playerMonsterRewardTable[20, 4] = 0.8f;
        playerMonsterRewardTable[20, 5] = 0.6f;
        playerMonsterRewardTable[20, 6] = 0.4f;
        playerMonsterRewardTable[20, 7] = 0.2f;
        playerMonsterRewardTable[20, 8] = 0.1f;
        playerMonsterRewardTable[20, 9] = 0.0f;
        playerMonsterRewardTable[20, 10] = 0.0f;
        playerMonsterRewardTable[20, 11] = 0.0f;
        playerMonsterRewardTable[20, 12] = 0.0f;
        playerMonsterRewardTable[20, 13] = 0.0f;
        playerMonsterRewardTable[20, 14] = 0.0f;
        playerMonsterRewardTable[20, 15] = 0.0f;
        playerMonsterRewardTable[20, 16] = 0.0f;
        playerMonsterRewardTable[20, 17] = 0.0f;
        playerMonsterRewardTable[20, 18] = 0.0f;
        playerMonsterRewardTable[20, 19] = 0.0f;

        playerMonsterRewardTable[21, 0] = 1.0f;
        playerMonsterRewardTable[21, 1] = 1.0f;
        playerMonsterRewardTable[21, 2] = 1.0f;
        playerMonsterRewardTable[21, 3] = 1.0f;
        playerMonsterRewardTable[21, 4] = 0.9f;
        playerMonsterRewardTable[21, 5] = 0.6f;
        playerMonsterRewardTable[21, 6] = 0.4f;
        playerMonsterRewardTable[21, 7] = 0.2f;
        playerMonsterRewardTable[21, 8] = 0.1f;
        playerMonsterRewardTable[21, 9] = 0.0f;
        playerMonsterRewardTable[21, 10] = 0.0f;
        playerMonsterRewardTable[21, 11] = 0.0f;
        playerMonsterRewardTable[21, 12] = 0.0f;
        playerMonsterRewardTable[21, 13] = 0.0f;
        playerMonsterRewardTable[21, 14] = 0.0f;
        playerMonsterRewardTable[21, 15] = 0.0f;
        playerMonsterRewardTable[21, 16] = 0.0f;
        playerMonsterRewardTable[21, 17] = 0.0f;
        playerMonsterRewardTable[21, 18] = 0.0f;
        playerMonsterRewardTable[21, 19] = 0.0f;
        playerMonsterRewardTable[21, 20] = 0.0f;

        playerMonsterRewardTable[22, 0] = 1.0f;
        playerMonsterRewardTable[22, 1] = 1.0f;
        playerMonsterRewardTable[22, 2] = 1.0f;
        playerMonsterRewardTable[22, 3] = 1.0f;
        playerMonsterRewardTable[22, 4] = 0.9f;
        playerMonsterRewardTable[22, 5] = 0.6f;
        playerMonsterRewardTable[22, 6] = 0.4f;
        playerMonsterRewardTable[22, 7] = 0.2f;
        playerMonsterRewardTable[22, 8] = 0.1f;
        playerMonsterRewardTable[22, 9] = 0.1f;
        playerMonsterRewardTable[22, 10] = 0.0f;
        playerMonsterRewardTable[22, 11] = 0.0f;
        playerMonsterRewardTable[22, 12] = 0.0f;
        playerMonsterRewardTable[22, 13] = 0.0f;
        playerMonsterRewardTable[22, 14] = 0.0f;
        playerMonsterRewardTable[22, 15] = 0.0f;
        playerMonsterRewardTable[22, 16] = 0.0f;
        playerMonsterRewardTable[22, 17] = 0.0f;
        playerMonsterRewardTable[22, 18] = 0.0f;
        playerMonsterRewardTable[22, 19] = 0.0f;
        playerMonsterRewardTable[22, 20] = 0.0f;
        playerMonsterRewardTable[22, 21] = 0.0f;

        playerMonsterRewardTable[23, 0] = 1.0f;
        playerMonsterRewardTable[23, 1] = 1.0f;
        playerMonsterRewardTable[23, 2] = 1.0f;
        playerMonsterRewardTable[23, 3] = 1.0f;
        playerMonsterRewardTable[23, 4] = 0.9f;
        playerMonsterRewardTable[23, 5] = 0.6f;
        playerMonsterRewardTable[23, 6] = 0.4f;
        playerMonsterRewardTable[23, 7] = 0.2f;
        playerMonsterRewardTable[23, 8] = 0.1f;
        playerMonsterRewardTable[23, 9] = 0.1f;
        playerMonsterRewardTable[23, 10] = 0.0f;
        playerMonsterRewardTable[23, 11] = 0.0f;
        playerMonsterRewardTable[23, 12] = 0.0f;
        playerMonsterRewardTable[23, 13] = 0.0f;
        playerMonsterRewardTable[23, 14] = 0.0f;
        playerMonsterRewardTable[23, 15] = 0.0f;
        playerMonsterRewardTable[23, 16] = 0.0f;
        playerMonsterRewardTable[23, 17] = 0.0f;
        playerMonsterRewardTable[23, 18] = 0.0f;
        playerMonsterRewardTable[23, 19] = 0.0f;
        playerMonsterRewardTable[23, 20] = 0.0f;
        playerMonsterRewardTable[23, 21] = 0.0f;
        playerMonsterRewardTable[23, 22] = 0.0f;

        playerMonsterRewardTable[24, 0] = 1.0f;
        playerMonsterRewardTable[24, 1] = 1.0f;
        playerMonsterRewardTable[24, 2] = 1.0f;
        playerMonsterRewardTable[24, 3] = 1.0f;
        playerMonsterRewardTable[24, 4] = 0.9f;
        playerMonsterRewardTable[24, 5] = 0.6f;
        playerMonsterRewardTable[24, 6] = 0.4f;
        playerMonsterRewardTable[24, 7] = 0.2f;
        playerMonsterRewardTable[24, 8] = 0.1f;
        playerMonsterRewardTable[24, 9] = 0.1f;
        playerMonsterRewardTable[24, 10] = 0.0f;
        playerMonsterRewardTable[24, 11] = 0.0f;
        playerMonsterRewardTable[24, 12] = 0.0f;
        playerMonsterRewardTable[24, 13] = 0.0f;
        playerMonsterRewardTable[24, 14] = 0.0f;
        playerMonsterRewardTable[24, 15] = 0.0f;
        playerMonsterRewardTable[24, 16] = 0.0f;
        playerMonsterRewardTable[24, 17] = 0.0f;
        playerMonsterRewardTable[24, 18] = 0.0f;
        playerMonsterRewardTable[24, 19] = 0.0f;
        playerMonsterRewardTable[24, 20] = 0.0f;
        playerMonsterRewardTable[24, 21] = 0.0f;
        playerMonsterRewardTable[24, 22] = 0.0f;
        playerMonsterRewardTable[24, 23] = 0.0f;

        playerMonsterRewardTable[25, 0] = 1.0f;
        playerMonsterRewardTable[25, 1] = 1.0f;
        playerMonsterRewardTable[25, 2] = 1.0f;
        playerMonsterRewardTable[25, 3] = 1.0f;
        playerMonsterRewardTable[25, 4] = 0.9f;
        playerMonsterRewardTable[25, 5] = 0.6f;
        playerMonsterRewardTable[25, 6] = 0.4f;
        playerMonsterRewardTable[25, 7] = 0.2f;
        playerMonsterRewardTable[25, 8] = 0.1f;
        playerMonsterRewardTable[25, 9] = 0.1f;
        playerMonsterRewardTable[25, 10] = 0.0f;
        playerMonsterRewardTable[25, 11] = 0.0f;
        playerMonsterRewardTable[25, 12] = 0.0f;
        playerMonsterRewardTable[25, 13] = 0.0f;
        playerMonsterRewardTable[25, 14] = 0.0f;
        playerMonsterRewardTable[25, 15] = 0.0f;
        playerMonsterRewardTable[25, 16] = 0.0f;
        playerMonsterRewardTable[25, 17] = 0.0f;
        playerMonsterRewardTable[25, 18] = 0.0f;
        playerMonsterRewardTable[25, 19] = 0.0f;
        playerMonsterRewardTable[25, 20] = 0.0f;
        playerMonsterRewardTable[25, 21] = 0.0f;
        playerMonsterRewardTable[25, 22] = 0.0f;
        playerMonsterRewardTable[25, 23] = 0.0f;
        playerMonsterRewardTable[25, 24] = 0.0f;
    }

    public static void OnNewGameOrLoad_DoBalanceAdjustments()
    {
        // only relevant for NG++ right now!
        if (GameStartData.NewGamePlus < 2) return;

        // buff certain abilities

        
    }
}
