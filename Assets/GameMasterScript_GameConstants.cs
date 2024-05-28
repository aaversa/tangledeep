using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript : MonoBehaviour {

    public const int MAX_PASSIVE_SKILLS_EQUIPPABLE = 4;
    public const int MAX_JOBCHANGE_COST = 99999;
    public const int MINIMUM_NON_STARTING_JOB_JP_COST = 250;
    public const int MONSTER_WAIT_TIME_NEWFLOOR = 3; // # of turns before monsters act when players first arrive, assuming monsters are not in combat and uninjured
    public const int PET_STAY_NEAR_PLAYER_TURNS = 4; // How many turns to stay immediately next to player after "come to me at once" is used
    public const int MONSTER_PET_ANGRY_THRESHOLD_DAYS = 3;
    public const int MAX_GOLD = 9999999;
    public const int PET_MAXTURNS_OUT_OF_ANCHORRANGE = 6;
    public const int FLASK_REDUCED_GAIN_THRESHOLD = 25;
    public const int GLORIOUS_BATTLER_CT_GAIN = 25;
    public const int CHAMPS_KILLED_REQ_FOR_ULTIMATE = 25;
    public const float OIL_SLICK_POISON_DAMAGE_MULTIPLIER = 1.3f;
    public const float MIN_WANDERING_MONSTER_SPAWN_DIST = 8f;
    public const float DLC1_CHANCE_RELIC_MD_MERCHANT = 0.03f;
    public const float DLC1_CHANCE_RELIC_SUB_FOR_LEGENDARY = 0.5f;
    public const float CHEAT_DEATH_CHANCE = 0.5f;
    public const float BRIGAND_DETECT_WEAKNESS_CHANCE = 0.045f;
    public const float PET_INHERIT_XP = 1f;
    public const float MELEE_POWERUP_WHACK_CHANCE = 0.2f;
    public const float SURVIVE_1HP_CHANCE = 0.08f;
    public const float CHANCE_HERB_FORAGE = 0.35f;
    public const float PLAYER_HEALTH_PERCENT_CRITICAL = 0.3f;
    public const float CHANCE_DANCER_EXTEND_SUMMON = 0.25f;
    public const float CHANCE_NIGHTMARE_EXTRAMOD = 0.33f;
    public const float CHANCE_NIGHTMAREPRINCE_LEGENDARY = 0.025f;
    public const float CHANCE_NIGHTMAREPRINCE_DROP_NKITEM = 0.5f;
    public const float CHANCE_HAZARD_SWEEP = 0.33f;
    public const float CHANCE_CHARM_CHAMPION = 0.33f;
    public const float CHANCE_PROC_SHADOWCAST = 0.5f;
    public const float CHANCE_TOWN_RESTOCKGOODS = 0.5f;
    public const float CHANCE_SIDEAREA_RESTOCKGOODS = 0.35f;
    public const int NOTORIOUS_NEWCHAMP_SPAWN_DELAY = 90;
    public const float NOTORIOUS_NEWCHAMP_SPAWNCHANCE = 0.02f;
    public const float PLAYER_PET_HEAL_PERCENTAGE = 0.5f; // Amount of inherited healing player's Monster Pet receives from healing effects.
    public const float RUNIC_HEAL_PERCENTAGE = 0.3f;
    public const float PANDORA_DROP_ORB_CHANCE = 0.4f;
    public const float CORRALPET_BONUS_RESISTCAP = 0.25f;
    public const float CORRALPET_MAX_RESISTANCES = 0.25f;
    public const float MAX_RESISTANCES = 0.05f;
    public const float TREASURETRACKER_POWERUP_CONVERTCHANCE = 0.33f;

    public const float PANDORA_MONSTER_DEFENSE_UP = 0.01f;
    public const float PANDORA_MONSTER_DEFENSE_CAP = 0.15f;
    public const float PANDORA_MONSTER_DAMAGE_UP = 0.013f;
    public const float PANDORA_BONUS_MONEY = 0.01f;
    public const float PANDORA_BONUS_MAGICCHANCE = 0.0075f;
    public const float PANDORA_BONUS_MAGICCHANCE_CAP = 0.5f;

    public const float CHANCE_WILDCHILD_LEARN_ENEMYSKILL = 0.5f;

#if !UNITY_ANDROID && !UNITY_IPHONE
    public const float MIN_FPS_DURING_LOAD = 0.0133f;
#else
    public const float MIN_FPS_DURING_LOAD = 0.056f;
#endif

    public const int XML_CHUNKS_BEFORE_YIELD = 5;
    public const int FLASK_HEAL_STAMINAENERGY = 1;
    public const int MAX_FLASK_CHARGES = 255;
    public const int FLASK_BUFF_ATTACKDEF = 2;
    public const int FLASK_INSTANT_HEAL = 3;
    public const int FLASK_HEAL_MORE = 4;
    public const int FLASK_BUFF_DODGE = 5;
    public const int FLASK_HASTE = 6;
    public const int MAX_ITEM_MODS = 5;
    public const int MAX_2H_ITEM_MODS = 8;
    public const int MAX_1H_ITEM_MODS = 5;
    public const int MAX_PLAYER_LEVEL_CAP = 15;
    public const int MAX_PLAYER_LEVEL_CAP_EXPANSION = 20;
    public const int MAX_MONSTER_LEVEL_CAP = 20;
    public const int MAX_MONSTER_LEVEL_CAP_EXPANSION = 25;
    public const float SCAVENGER_BONUS_LOOT_CHANCE = 0.032f;
    public const float SCAVENGER_BONUS_MAGIC_CHANCE = 0.02f;
    public const float RUBYMOON_BONUS_MAGIC_CHANCE = 0.035f;
    public const float PANDORA_BASE_LEGENDARY_CHANCE = 0.135f;
    public const float MAX_CHALLENGE_RATING = 1.9f;
    public const float MAX_CHALLENGE_RATING_EXPANSION = 2.2f;
    //public const float PANDORA_LEGENDARY_SCALING = 0.01f;

    public const int DEFAULT_MAX_BANKABLE_ITEMS = 30;

    // We don't have disk space on Switch for lots of slots.
#if UNITY_SWITCH
    public const int kNumSaveSlots = 4;
#else
    public const int kNumSaveSlots = 15;
#endif

    public const int GAME_BUILD_VERSION = 151;

    public const float NOTORIOUS_SPECIAL_ITEM_CHANCE = 0.33f;
}
