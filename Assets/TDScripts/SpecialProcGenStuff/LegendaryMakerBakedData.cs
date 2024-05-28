using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public partial class LegendaryMaker
{
    static string[] lowercaseArtifactNameElements;

    static List<WeaponTypes> possibleLegWeaponTypes = new List<WeaponTypes>()
    { WeaponTypes.AXE,
        WeaponTypes.SWORD,
        WeaponTypes.SPEAR,
        WeaponTypes.BOW,
        WeaponTypes.BOW, // bows can show up a bit more often
        WeaponTypes.STAFF,
        WeaponTypes.MACE,
        WeaponTypes.DAGGER,
        WeaponTypes.CLAW
    };

    public static Dictionary<ELegendarySpriteTypes, List<int>> legPossibleSpritesByType = new Dictionary<ELegendarySpriteTypes, List<int>>()
    {
        { ELegendarySpriteTypes.SWORD, new List<int>() { 2,3,4,5,6,7,8,9,11,14,16,17,18,19,49,66 } },
        { ELegendarySpriteTypes.MACE, new List<int>() { 10,22,23,24,28,31,33,34,51,59,115,116,117, 130 } },
        { ELegendarySpriteTypes.AXE, new List<int>() { 42,43,44,45,46,47,50,52,67,53,54,55 } },
        { ELegendarySpriteTypes.STAFF, new List<int>() { 27,29,30,32,35,36,37,38,137,138,139, 130 } },
        { ELegendarySpriteTypes.CLAW, new List<int>() { 74,75,76,77,78,79,95,96,111,112,113,114 } },
        { ELegendarySpriteTypes.DAGGER, new List<int>() { 62,63,64,65,68,97,98,99,118,119,133,134,135 } },
        { ELegendarySpriteTypes.BOW, new List<int>() { 104,105,106,108,109,110,125,126, 128 } },
        { ELegendarySpriteTypes.SPEAR, new List<int>() { 82,83,84,85,86,87,88,89,90,91,56,57,58, 127, 131 } },
        { ELegendarySpriteTypes.SHIELD, new List<int>() { 163,164,165,166,167,168,169,170,171 } },
        { ELegendarySpriteTypes.LIGHTARMOR, new List<int>() { 188,192,201,202,203,204,205,206,207,208,211,225 } },
        { ELegendarySpriteTypes.MEDIUMARMOR, new List<int>() { 221,222,223,224,226,227,228 } },
        { ELegendarySpriteTypes.HEAVYARMOR, new List<int>() { 242,243,244,245,246,247,249,250,251,252,253,255 } },
        { ELegendarySpriteTypes.HELMET, new List<int>() { 182,183,184,185,186,187,189,191,210,256,300,357,381,387,392 } },
        { ELegendarySpriteTypes.BOOK, new List<int>() { 190,423,434,436,437 } },
        { ELegendarySpriteTypes.QUIVER, new List<int>() { 305,308,309,310 } },
        { ELegendarySpriteTypes.GLOVE, new List<int>() { 356,369,370,371,372,373,375 } },
        { ELegendarySpriteTypes.RING, new List<int>() { 264,265,266,282,283,285,286,287,288,289, 129 } },
        { ELegendarySpriteTypes.ACCESSORY, new List<int>() { 284,301,302,327,343,391,393,395,396,412,418, 129 } },
        { ELegendarySpriteTypes.NECKLACE, new List<int>() { 321,322,324,325,326,328,329,330,331,332,333,334,335,336 } },
        { ELegendarySpriteTypes.INSTRUMENT, new List<int>() { 399,519,520,521 } },
        { ELegendarySpriteTypes.HANDWRAP, new List<int>() { 142,356,369,370,371,372,373,375,142,143,144,145,146 } },
        { ELegendarySpriteTypes.WHIP, new List<int>() { 147,148,149,150,151,152,153,69,154,155 } }
    };

    public static Dictionary<string, LegModData> specialNonLegMods = new Dictionary<string, LegModData>()
    {
        { "mm_elemental_bolt", new LegModData(new Vector2(1.1f, 1.65f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON, EquipmentSlots.OFFHAND } ) },
        { "mm_elemental_bolt2", new LegModData(new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON, EquipmentSlots.OFFHAND } ) },
        { "mm_skill_portalwarp", new LegModData(new Vector2(1.6f,2.2f)) },
        { "mm_skill_throwstone", new LegModData(new Vector2(1.0f,1.3f)) },
        { "mm_skill_warcry", new LegModData(new Vector2(1.2f,2.2f)) },
        { "mm_skill_intimidate", new LegModData(new Vector2(1.3f,2.2f)) },
        { "mm_skill_soulfire", new LegModData(new Vector2(1.4f,2.2f)) },
        { "mm_skill_nordshout", new LegModData(new Vector2(1.6f,2.2f)) },
        { "mm_skill_shadowmeld", new LegModData(new Vector2(1.3f,2.2f)) },
        { "mm_skill_vortexarmor", new LegModData(new Vector2(1.4f,2.2f)) },
        { "mm_glowing2", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 energy
        { "mm_agility2", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 swift
        { "mm_disc15", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 disc
        { "mm_spirit15", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 disc
        { "mm_strength15", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 strength        
        { "mm_guile15", new LegModData(new Vector2(1.25f,1.5f)) }, // +15 guile
        { "mm_guile25", new LegModData(new Vector2(1.8f,2.2f)) }, // +25 guile
        { "mm_swiftness25", new LegModData(new Vector2(1.8f,2.2f)) }, // +25 swift
        { "mm_strength25", new LegModData(new Vector2(1.6f,2.2f)) }, // +25 strength

        { "mm_might3", new LegModData(new Vector2(1.8f,2.2f)) }, // +25 strength

        { "mm_spirit25", new LegModData(new Vector2(1.6f,2.2f)) }, // +25 spirit

        { "mm_spirit40", new LegModData(new Vector2(1.8f,2.2f)) }, // +40 spirit

        { "mm_disc25", new LegModData(new Vector2(1.8f,2.2f)) }, // +25 disc
        { "mm_goldfind1_silent", new LegModData(new Vector2(1.0f,1.4f)) }, // slight gold find
        { "mm_mechanist", new LegModData(new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY }) }, // spawn robots sometimes
        { "mm_juggernaut", new LegModData(new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND, EquipmentSlots.ACCESSORY }) }, // cap max damage that can be taken
        { "mm_soothingflute", new LegModData(new Vector2(1.0f, 2.2f)) }, // put monsters to sleep at random
        { "mm_candleskull", new LegModData(new Vector2(1.3f, 1.8f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY, EquipmentSlots.OFFHAND }) }, // extra dmg to enemies that are debuffed
        { "mm_giantgauntlet", new LegModData(new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON, EquipmentSlots.ACCESSORY }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // phys skills in melee do more dmg
        { "mm_lowlifeheal", new LegModData(new Vector2(1.5f, 2.2f)) }, // regen health at low life
        { "mm_bountifulpouch", new LegModData(new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY }) }, // reusable consumables
        { "mm_assassingloves", new LegModData(new Vector2(1.2f, 1.7f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND, EquipmentSlots.ACCESSORY }) }, // +15% dmg with sharp weapons
        { "mm_knightgloves", new LegModData(new Vector2(1.2f, 1.7f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND, EquipmentSlots.ACCESSORY }) }, // sword/mace dmg with shield
        { "mm_bonedagger", new LegModData(new Vector2(1.3f, 1.8f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 5 ct per melee attack
        { "mm_20ctattack", new LegModData(new Vector2(1.9f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 20 ct per melee attack
        { "mm_dragonbrave", new LegModData( new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // +% champion/boss fighting        
                
        { "mm_obsidian", new LegModData( new Vector2(1.5f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY }) }, // lower energy, more stamina cost
        { "mm_surestrike", new LegModData( new Vector2(1.0f, 1.35f)) }, // +5% crit
        { "mm_crit8", new LegModData( new Vector2(1.4f, 1.65f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON, EquipmentSlots.OFFHAND }) }, // +8% crit
        { "mm_crit10", new LegModData( new Vector2(1.7f, 1.9f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // +10% crit
        { "mm_crit15", new LegModData( new Vector2(1.95f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // +15% crit
        { "mm_fairychoker", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) }, // 20% reduce cooldowns when struck
        { "mm_manaseeker", new LegModData( new Vector2(1.3f, 1.8f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }) }, // stamina --> energy powerups
        { "mm_dodge1", new LegModData( new Vector2(1.0f, 1.05f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge2", new LegModData( new Vector2(1.1f, 1.15f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge3", new LegModData( new Vector2(1.2f, 1.25f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge4", new LegModData( new Vector2(1.3f, 1.35f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge5", new LegModData( new Vector2(1.4f, 1.45f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge6", new LegModData( new Vector2(1.5f, 1.55f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge7", new LegModData( new Vector2(1.6f, 1.65f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND })  },
        { "mm_dodge8", new LegModData( new Vector2(1.7f, 1.75f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge9", new LegModData( new Vector2(1.8f, 1.85f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_dodge10", new LegModData( new Vector2(1.9f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) },
        { "mm_tier1helmethp", new LegModData( new Vector2(1.0f, 1.05f)) }, // 15 hp
        { "mm_tier2helmethp", new LegModData( new Vector2(1.1f, 1.15f)) }, // 30 hp
        { "mm_tier3helmethp", new LegModData( new Vector2(1.2f, 1.25f)) }, // 50 hp
        { "mm_tier4helmethp", new LegModData( new Vector2(1.3f, 1.45f)) }, // 75 hp
        { "mm_lightreactionheal", new LegModData( new Vector2(1.0f, 1.55f)) }, // 75 hp        
        { "mm_100hp", new LegModData( new Vector2(1.5f, 1.55f)) }, // 100 hp        
        { "mm_150hp", new LegModData( new Vector2(1.6f, 1.7f)) }, // 150 hp
        { "mm_200hp", new LegModData( new Vector2(1.75f, 2.2f)) }, // 200 hp
        { "mm_glowtorch", new LegModData( new Vector2(1.0f, 1.5f)) }, // +10% fire/lightning dmg dealt
        { "mm_firering", new LegModData( new Vector2(1.0f, 1.6f)) }, // +15% fire dmg dealt
        { "mm_icering", new LegModData( new Vector2(1.0f, 1.6f)) }, // +15% water dmg dealt


        { "mm_waterdamage20", new LegModData( new Vector2(1.1f, 1.8f)) }, // +20% water dmg dealt
        { "mm_waterres25" , new LegModData( new Vector2(1.2f, 1.8f)) }, // +25% water res

        { "mm_firewater20res" , new LegModData( new Vector2(1.3f, 1.8f)) }, // +20% fire and water res

        { "mm_shadowring", new LegModData( new Vector2(1.0f, 1.45f)) }, // +15% shadow dmg dealt
        { "mm_shadowdmg25", new LegModData( new Vector2(1.5f, 2.2f)) }, // +25% shadow dmg dealt
        { "mm_poisonring", new LegModData( new Vector2(1.0f, 1.35f)) }, // +15% poison dmg dealt
        { "mm_boostpoison20", new LegModData( new Vector2(1.4f, 1.55f)) }, // +20% poison dmg dealt
        { "mm_wildnatureweapon", new LegModData( new Vector2(1.6f, 2.2f)) }, // +25% poison dmg dealt        
        { "mm_lightningring", new LegModData( new Vector2(1.0f, 1.45f)) }, // +15% lightning dmg dealt
        { "mm_lightningdmg25", new LegModData( new Vector2(1.5f, 2.2f)) }, // +25% lightning dmg dealt
        { "mm_firewaterdmgup", new LegModData( new Vector2(1.6f, 2.2f)) }, // +25% fire + water dmg dealt       

        { "mm_movect1", new LegModData( new Vector2(1.0f, 1.35f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }) }, // 2 ct per step
        { "mm_movect2", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }) }, // 3 ct per step

        { "mm_movect5", new LegModData( new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }) }, // 5 ct per step

        { "mm_resistall10", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND }) }, // resist 10% all

        { "mm_resistmud", new LegModData( new Vector2(1.0f, 1.35f)) }, // resist mud
        { "mm_reactroot", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR }) }, // 33% root enemies when hit
        { "mm_charmenemy5", new LegModData( new Vector2(1.1f, 1.45f)) }, // 5% chance to charm on atk
        { "mm_charmenemy1", new LegModData( new Vector2(1.5f, 2.2f)) }, // 8% chance to charm on atk
        { "mm_samurai", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.WEAPON }) }, // +10% all dmg 
        { "mm_superheavy", new LegModData( new Vector2(1.5f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) }, // 50% immovable
        { "mm_reflective", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR }) }, // 25% chance reflect magic projectiles
        { "mm_parry3", new LegModData( new Vector2(1.0f, 1.35f)) }, // 3% parry
        { "mm_parry5", new LegModData( new Vector2(1.4f, 1.55f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 5% parry        
        { "mm_parry7", new LegModData( new Vector2(1.6f, 1.65f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 7% parry
        { "mm_parry8", new LegModData( new Vector2(1.7f, 1.85f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 8% parry
        { "mm_parry10", new LegModData( new Vector2(1.9f, 2.05f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 10% parry
        { "mm_parry13", new LegModData( new Vector2(2.1f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // 13% parry     
        { "mm_sheriffbelt", new LegModData( new Vector2(1.2f, 1.7f)) }, // +20% fighting vs bandits
        { "mm_jumpboots", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY }) }, // extend movement ability range     
        { "mm_statusblock5", new LegModData( new Vector2(1.0f, 1.35f)) }, // 5% block
        { "mm_block8", new LegModData( new Vector2(1.4f, 1.55f)) }, // 8% block        
        { "mm_block12", new LegModData( new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND }) }, // 12% block                
        { "mm_crit3", new LegModData( new Vector2(1.0f, 1.35f)) }, // 3% crit
        { "mm_crit5", new LegModData( new Vector2(1.4f, 1.85f)) }, // 5% crit
        { "mm_crit7", new LegModData( new Vector2(1.9f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // 7% crit
        { "mm_immune_sealed", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND, EquipmentSlots.ACCESSORY }) }, // immune to being sealed
        { "mm_companions", new LegModData( new Vector2(1.4f, 1.85f)) } // +10% corral pet
    };

    public static Dictionary<string, LegModData> legendaryOnlyMods = new Dictionary<string, LegModData>()
    { 
        // Legendary-only mods
        { "mm_athyes", new LegModData( new Vector2(1.4f, 2.2f)) }, // +33% fire dmg, chance to stun
        { "mm_ultraheavy", new LegModData( new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }) }, // 100% immovable
        { "mm_legshard", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) }, // % chance to negate magic and gain spirit power
        { "mm_legkatana", new LegModData( new Vector2(1.1f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to attack with razor winds
        { "mm_procbolt", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to attack with lightning bolt
        { "mm_procshadow", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to attack with shadow knives
        { "mm_soulsteal", new LegModData( new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to drain health, lose stam/energy on attack
        { "mm_antipode", new LegModData( new Vector2(1.5f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to attack with extra fire OR ice 
        { "mm_gaelmydd", new LegModData( new Vector2(1.4f, 2.2f)) }, // % chance to harvest robots
        { "mm_asceticgrab", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND }, CharacterJobs.BUDOKA) }, // doesnt interfere with unarmed; % chance to grab projectiles
        { "mm_asceticaura", new LegModData( new Vector2(1.4f, 2.2f), null, CharacterJobs.BUDOKA) }, // critical strikes + budoka skills can paralyze
        { "mm_oceangem", new LegModData( new Vector2(1.5f, 2.2f)) }, // big bonuses in water
        { "mm_magicmirrors", new LegModData( new Vector2(1.3f, 2.2f)) }, // never miss, see through illusions
        { "mm_shadowcast", new LegModData( new Vector2(1.5f, 2.2f)) }, // % chance summon shadow traps on spend energy
        { "mm_doublebite", new LegModData( new Vector2(1.4f, 2.2f)) }, // boost shadow or water by 25%, alternating
        { "mm_dragonscale", new LegModData( new Vector2(1.7f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND, EquipmentSlots.ACCESSORY }) }, // -25% dmg from all basic attacks
        { "mm_draik", new LegModData( new Vector2(1.5f, 2.2f)) }, // increased difficulty and rewards
        { "mm_aetherslash", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance to root and shadow bleed
        { "mm_bigstick", new LegModData( new Vector2(1.0f, 1.6f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance for big knockback
        { "mm_starhelm", new LegModData( new Vector2(1.5f, 2.2f)) }, // % chance for random smiting
        { "mm_wildnaturevest", new LegModData( new Vector2(1.0f, 1.6f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) }, // % chance summon thorns when struck        
        { "mm_wildnatureband", new LegModData( new Vector2(1.0f, 1.5f)) }, // % chance resist root
        { "mm_hairband", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY, EquipmentSlots.ARMOR }) }, // converts % spent energy to stamina and vice versa
        { "mm_freezeattack", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance for freeze attack
        { "mm_chillaura", new LegModData( new Vector2(1.2f, 2.2f)) }, // % chance for freezing aura
        { "mm_crystalspearbleed", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // % chance for massive bleed
        { "mm_paralyzereact", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ARMOR, EquipmentSlots.OFFHAND }) }, // % chance to paralyze attackers
        { "mm_findweakness", new LegModData( new Vector2(1.0f, 1.6f)) }, // % chance to find enemy weakness
        { "mm_catears", new LegModData( new Vector2(1.0f, 2.2f)) }, // % chance to confuse/charm
        { "mm_rujasucards", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON, EquipmentSlots.ACCESSORY }, CharacterJobs.GAMBLER) }, // boost some gambler skills
        { "mm_trumpet", new LegModData( new Vector2(1.1f, 2.2f)) }, // % chance to sing song and boost self
        { "mm_fistattackproc", new LegModData( new Vector2(1.0f, 2.2f)) }, // % chance proc psychic fist
        { "mm_findhealth", new LegModData( new Vector2(1.5f, 2.2f)) }, // can find health powerups        
        { "mm_butterfly", new LegModData( new Vector2(1.6f, 2.2f), null, CharacterJobs.SWORDDANCER) }, // empower sword dancer skills, crits bleed
        { "mm_rubymoon", new LegModData( new Vector2(1.5f, 2.2f)) }, // increase magic/rare item chance
        { "mm_summonice", new LegModData( new Vector2(1.5f, 2.2f)) }, // summon random ice shards
        { "mm_hergerobe", new LegModData( new Vector2(1.2f, 2.2f)) }, // boost spirit power at low stamina   
        { "mm_vezakpoison", new LegModData( new Vector2(1.5f, 2.2f)) }, // chance to proc long lasting poison
        { "mm_shootfire", new LegModData( new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // shoot fire on attack
        { "mm_swing_defense", new LegModData( new Vector2(1.4f, 2.2f)) }, // gain temp defense with each attack
        { "mm_immune_poisonbleed", new LegModData( new Vector2(1.4f, 2.2f)) }, // immune to poison and bleed
        { "mm_immune_defenselower", new LegModData( new Vector2(1.3f, 2.2f)) }, // immune to defense lowering effects
        { "mm_summonthorns", new LegModData( new Vector2(1.0f, 1.7f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // chance to summon thorns on attack
        { "mm_plantgrowth", new LegModData( new Vector2(1.0f, 2.2f)) }, // chance to summon vines when moving around
        { "mm_moonbeams", new LegModData( new Vector2(1.3f, 2.2f)) }, // summon random moonbeams that hurt or buff
        { "mm_seraphblock", new LegModData( new Vector2(1.5f, 2.2f)) }, // restore health on block if below X% health
        { "mm_chance_freespell", new LegModData( new Vector2(1.4f, 2.2f)) }, // 15% chance to restore energy 
        { "mm_ignorelowdamage", new LegModData( new Vector2(1.2f, 2.2f)) }, // chance to ignore minor damage
        { "mm_blightpoison", new LegModData( new Vector2(1.8f, 2.2f)) }, // chance to inflict long lasting poison
        { "mm_songblade", new LegModData( new Vector2(1.4f, 1.9f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // chance to sing edge thane song
        { "mm_axestyle", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.NO_AXES) }, // axe effect on swing
        { "mm_phasmaquiver", new LegModData( new Vector2(1.3f, 2.2f)) }, // spend energy to charge energy attack
        { "mm_breathstealer", new LegModData( new Vector2(1.4f, 1.9f)) }, // chance to seal monsters
        { "mm_dismantler", new LegModData( new Vector2(1.4f, 1.9f)) }, // chance to paralyze monsters
        { "mm_ramirelmask", new LegModData( new Vector2(1.6f, 2.2f)) }, // decrease visibility from monsters
        { "mm_starcall", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_RANGED) }, // fire starshards when attacking at range

        { "mm_twoarrows", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_RANGED) }, // always fire two arrows

        { "mm_iceonhit", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // adds water dmg per hit
        { "mm_bigfreezeonhit", new LegModData( new Vector2(1.5f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }) }, // freezes target on hit

        { "mm_bowchange", new LegModData( new Vector2(1.2f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // increase damage of melee when attacking with bow
        { "mm_staffcopy", new LegModData( new Vector2(1.6f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT, EWeaponFilterProperties.ONLY_MELEE) }, // jingu bang shadow copy
        { "mm_summonshade_onkill", new LegModData( new Vector2(1.4f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.WEAPON }, CharacterJobs.COUNT) }, // summon soulshade on kill

        { "mm_goldpower", new LegModData( new Vector2(1.0f, 2.2f)) }, // power when picking up gold piles
        { "mm_droppile_gold", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.ACCESSORY }) }, // chance to drop gold piles

        { "mm_meltblockparry", new LegModData( new Vector2(1.2f, 2.2f)) }, // reduce enemy block + parry chance on hit
        { "mm_confuseblock", new LegModData( new Vector2(1.1f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND }) }, // chance to confuse enemies on block
        { "mm_enhancebleed", new LegModData( new Vector2(1.5f, 2.2f)) }, // increase power of bleeds
        { "mm_powershot", new LegModData( new Vector2(1.6f, 2.2f)) }, // charge up attack while moving -> unleash
        { "mm_paladinboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.PALADIN) },
        { "mm_soulkeeperboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SOULKEEPER) },
        { "mm_hunterboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.HUNTER) },
        { "mm_budokaboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.BUDOKA) },
        { "mm_husynboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.HUSYN) },
        { "mm_floramancerboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.FLORAMANCER) },
        { "mm_spellshaperboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SPELLSHAPER) },
        { "mm_brigandboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.BRIGAND) },
        { "mm_edgethaneboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.EDGETHANE) },
        { "mm_gamblerboost", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.GAMBLER) },
        { "mm_dualwielderemblem_tier0_dualwield", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }, CharacterJobs.DUALWIELDER) },
        { "mm_dualwielderemblem_tier0_biography", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.DUALWIELDER) },
        { "mm_brigandemblem_tier0_bleed", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.BRIGAND) },
        { "mm_brigandemblem_tier0_stealth", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.BRIGAND) },
        { "mm_floramanceremblem_tier0_pethealth", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.FLORAMANCER) },
        { "mm_floramanceremblem_tier0_poisondmg", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.FLORAMANCER) },
        { "mm_sworddanceremblem_tier0_wildhorse", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SWORDDANCER) },
        { "mm_attackstep_special", new LegModData( new Vector2(1.0f, 2.2f), null) },
        { "mm_spellshaperemblem_tier0_elemental", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SPELLSHAPER) },
        { "mm_spellshaperemblem_tier0_aura", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SPELLSHAPER) },
        { "mm_soulkeeperemblem_tier0_shadowdmg", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SOULKEEPER) },
        { "mm_soulkeeperemblem_tier0_pets", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.SOULKEEPER) },
        { "mm_paladinemblem_tier0_divinedmg", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.PALADIN) },
        { "mm_paladinemblem_tier0_block", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.PALADIN) },
        { "mm_wildchildemblem_tier0_technique", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.WILDCHILD) },
        { "mm_wildchildemblem_tier0_claws", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }, CharacterJobs.WILDCHILD) },
        { "mm_edgethaneemblem_tier0_song", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.EDGETHANE) },
        { "mm_edgethaneemblem_tier0_lowhp", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.EDGETHANE) },
        { "mm_gambleremblem_tier0_heal", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.GAMBLER) },
        { "mm_gambleremblem_tier0_items", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.GAMBLER) },
        { "mm_budokaemblem_tier0_spiritfists", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }, CharacterJobs.BUDOKA) },
        { "mm_budokaemblem_tier0_fear", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.BUDOKA) },
        { "mm_husynemblem_tier0_energy", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.HUSYN) },
        { "mm_husynemblem_tier0_runic", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.HUSYN) },
        { "mm_hunteremblem_tier0_arrows", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>() { EquipmentSlots.OFFHAND, EquipmentSlots.ARMOR, EquipmentSlots.ACCESSORY }, CharacterJobs.HUNTER) },
        { "mm_hunteremblem_tier0_shadow", new LegModData( new Vector2(1.0f, 2.2f), null, CharacterJobs.HUNTER) },
        { "mm_passive_herbforaging", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_gloriousbattler", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_immunology", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_hazardsweep", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_fatchance", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_bloodtracking", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_armortraining", new LegModData( new Vector2(1.3f, 2.2f), new List<EquipmentSlots>() {EquipmentSlots.OFFHAND, EquipmentSlots.WEAPON, EquipmentSlots.ACCESSORY }) },
        { "mm_passive_qimastery", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_passive_thornedskin", new LegModData( new Vector2(1.3f, 2.2f)) },
        { "mm_budokavalid", new LegModData( new Vector2(1.0f, 2.2f), new List<EquipmentSlots>(){EquipmentSlots.OFFHAND}) } // JUST does not interfere with unarmed fighting        
    };

    public static Dictionary<WeaponTypes, List<string>> projectilePrefabs = new Dictionary<WeaponTypes, List<string>>()
    {
        { WeaponTypes.BOW, new List<string>() { "LargeArrowEffect","SmallArrowEffect","SmallBoltEffect","LargeBoltEffect" } },
        { WeaponTypes.STAFF, new List<string>() { "BasicEnergyProjectile" } }
    };

    public static Dictionary<EFlavorTextElements, List<string>> flavorElements = new Dictionary<EFlavorTextElements, List<string>>();

    static void GetNameAndFlavorElementsFromData()
    {
        lowercaseArtifactNameElements = new string[(int)EArtifactNameElements.COUNT];
        for (int i = 0; i < (int)EArtifactNameElements.COUNT; i++)
        {
            lowercaseArtifactNameElements[i] = ((EArtifactNameElements)i).ToString().ToLowerInvariant();
        }

        Dictionary<EFlavorTextElements, int> countOfFlavorElementsFromFile = new Dictionary<EFlavorTextElements, int>()
        {
        };


        Dictionary<EArtifactNameElements, int> countOfNameElementsFromFile = new Dictionary<EArtifactNameElements, int>()
        {
            { EArtifactNameElements.HISTORIC,101 },
            { EArtifactNameElements.GENERICOBJECT, 20 },
            { EArtifactNameElements.GENERICWEAPON, 15 },
            { EArtifactNameElements.HAMMER, 9 },
            { EArtifactNameElements.SWORD, 6 },
            { EArtifactNameElements.AXE, 5 },
            { EArtifactNameElements.CLAW, 6 },
            { EArtifactNameElements.DAGGER, 9 },
            { EArtifactNameElements.SPEAR, 7 },
            { EArtifactNameElements.STAFF, 9 },
            { EArtifactNameElements.RANGEDWEAPON, 5 },
            { EArtifactNameElements.MAGIC, 9 },
            { EArtifactNameElements.DEFENSE, 17 },
            { EArtifactNameElements.ACCESSORY, 21 },
            { EArtifactNameElements.MAGICBOOK, 9 },
            { EArtifactNameElements.QUIVER, 6 },
            { EArtifactNameElements.LIGHTARMOR, 7 },
            { EArtifactNameElements.MEDIUMARMOR, 6 },
            { EArtifactNameElements.HEAVYARMOR, 4 },
            { EArtifactNameElements.PERSONALDESCRIPTOR, 17 },
            { EArtifactNameElements.GENERALDESCRIPTOR, 31 },
            { EArtifactNameElements.SWORDPROPER, 11 },
            { EArtifactNameElements.SPEARPROPER, 7 },
            { EArtifactNameElements.DAGGERPROPER, 7 },
            { EArtifactNameElements.AXEPROPER, 7 },
            { EArtifactNameElements.CLAWPROPER, 7 },
            { EArtifactNameElements.HAMMERPROPER, 7 },
            { EArtifactNameElements.RANGEDPROPER, 18 },
            { EArtifactNameElements.STAFFPROPER, 8 },
            { EArtifactNameElements.ARMORPROPER, 14 },
            { EArtifactNameElements.SHIELDPROPER, 13 },
            { EArtifactNameElements.BOOKPROPER, 13 },
            { EArtifactNameElements.ACCESSORYPROPER, 10 },
            { EArtifactNameElements.GLOVE, 5 },
            { EArtifactNameElements.HELMET, 6 },
            { EArtifactNameElements.RING, 5 },
            { EArtifactNameElements.NECKLACE, 6 },
            { EArtifactNameElements.INSTRUMENT, 8 },
            { EArtifactNameElements.HELMETPROPER, 2 },
            { EArtifactNameElements.RINGPROPER, 6 },
        };

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            countOfNameElementsFromFile.Add(EArtifactNameElements.WHIP, 5);
            countOfNameElementsFromFile.Add(EArtifactNameElements.WHIPPROPER, 5);

            possibleLegWeaponTypes.Add(WeaponTypes.WHIP);
            possibleLegWeaponTypes.Add(WeaponTypes.NATURAL);
        }

        // We want to read the contents of, say, exp_leg_spearpropername1 to exp_leg_spearpropername7
        // And add these to a list to use later.

        StringBuilder sb = new StringBuilder();
        
        for (int i = 0; i < (int)EArtifactNameElements.COUNT; i++)
        {
            List<string> nameList = new List<string>();
            int startIndex = 1;

            EArtifactNameElements currentElement = (EArtifactNameElements)i;

            if (!countOfNameElementsFromFile.ContainsKey(currentElement))
            {
                //Debug.Log("We don't have " + currentElement + " in this language: " + StringManager.gameLanguage);
                continue;
            }
            for (int n = startIndex; n <= countOfNameElementsFromFile[currentElement]; n++)
            {
                sb.Length = 0;
                sb.Append("exp_leg_");
                sb.Append(lowercaseArtifactNameElements[i]);
                sb.Append("name");
                sb.Append(n);
                nameList.Add(StringManager.GetString(sb.ToString()));
            }

            nameElements.Add(currentElement, nameList);
        }
    }
}
