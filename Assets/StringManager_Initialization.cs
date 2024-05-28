using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public partial class StringManager
{

    public static void AssignWeaponPropertyStrings()
    {
        if (Weapon.weaponProperties == null)
        {
            Weapon.weaponProperties = new string[(int)WeaponTypes.COUNT];
            Weapon.weaponProperties[(int)WeaponTypes.SPEAR] = GetString("info_weaponproperties_spear");
            Weapon.weaponProperties[(int)WeaponTypes.SLING] = "";
            Weapon.weaponProperties[(int)WeaponTypes.BOW] = GetString("info_weaponproperties_bow");
            Weapon.weaponProperties[(int)WeaponTypes.STAFF] = GetString("info_weaponproperties_staff");
            Weapon.weaponProperties[(int)WeaponTypes.SWORD] = GetString("info_weaponproperties_sword");
            Weapon.weaponProperties[(int)WeaponTypes.CLAW] = GetString("info_weaponproperties_claw");
            Weapon.weaponProperties[(int)WeaponTypes.AXE] = GetString("info_weaponproperties_axe");
            Weapon.weaponProperties[(int)WeaponTypes.DAGGER] = GetString("info_weaponproperties_dagger");
            Weapon.weaponProperties[(int)WeaponTypes.MACE] = GetString("info_weaponproperties_mace");
            Weapon.weaponProperties[(int)WeaponTypes.WHIP] = GetString("info_weaponproperties_whip");
        }
    }

    static void AssignEnumsToDictionaries(bool bForceRefresh = false)
    {
        if (dictEnumStrings != null && !bForceRefresh)
        {
            return;
        }

        bool autoConvertToUpper = StringManager.gameLanguage != EGameLanguage.de_germany;

        dictEnumStrings = new Dictionary<Type, Dictionary<int, string>>();

        // == ItemFilters ==========================================================
        Dictionary<int, string> d = new Dictionary<int, string>();
        dictEnumStrings.Add(typeof(ItemFilters), d);

        d.Add((int)ItemFilters.DEALDAMAGE, GetString("item_filters_deal_damage"));
        d.Add((int)ItemFilters.VALUABLES, GetString("item_filters_valuables"));
        d.Add((int)ItemFilters.HEALENERGY, GetString("item_filters_heal_energy"));
        d.Add((int)ItemFilters.HEALHP, GetString("item_filters_heal_hp"));
        d.Add((int)ItemFilters.HEALSTAMINA, GetString("item_filters_heal_stamina"));
        d.Add((int)ItemFilters.INGREDIENT, GetString("item_filters_ingredient"));
        d.Add((int)ItemFilters.MEAL, GetString("item_filters_meals"));
        d.Add((int)ItemFilters.OFFENSE, GetString("item_filters_offense"));
        d.Add((int)ItemFilters.SELFBUFF, GetString("item_filters_self_buff"));
        d.Add((int)ItemFilters.SUMMON, GetString("item_filters_summon"));
        d.Add((int)ItemFilters.SUPPORT, GetString("item_filters_support"));
        d.Add((int)ItemFilters.UTILITY, GetString("item_filters_utility"));
        d.Add((int)ItemFilters.RECOVERY, GetString("item_filters_recovery"));
        d.Add((int)ItemFilters.FAVORITES, GetString("item_filters_favorites"));
        d.Add((int)ItemFilters.VIEWALL, GetString("item_filters_view_all"));

        List<int> keys = d.Keys.ToList();
        if (autoConvertToUpper)
        {
            foreach (int key in keys)
            {
                d[key] = d[key].ToUpperInvariant();
            }
        }

        // == InventorySortTypes ==========================================================
        d = new Dictionary<int, string>();
        dictEnumStrings.Add(typeof(InventorySortTypes), d);

        //eventually use the #str_ for localization
        d.Add((int)InventorySortTypes.ALPHA, GetString("item_sort_type_alpha")); // "A -Z");
        d.Add((int)InventorySortTypes.ITEMTYPE, GetString("item_sort_type_type")); // "TYPE");
        d.Add((int)InventorySortTypes.RANK, GetString("item_sort_type_rank")); // "RANK");
        d.Add((int)InventorySortTypes.RARITY, GetString("item_sort_type_rarity")); // "RARITY");//worst pony
        d.Add((int)InventorySortTypes.VALUE, GetString("item_sort_type_value")); // "VALUE");
        d.Add((int)InventorySortTypes.CONSUMABLETYPE, GetString("item_sort_type_type")); // "TYPE");  //Also TYPE on the button, players won't know <3 

        // == SkillSheet Modes ==========================================================
        d = new Dictionary<int, string>();
        dictEnumStrings.Add(typeof(ESkillSheetMode), d);

        d.Add((int)ESkillSheetMode.assign_abilities, GetString("ui_skillsheet_setabilities"));
        d.Add((int)ESkillSheetMode.purchase_abilities, GetString("ui_skillsheet_learnabilities"));
        d.Add((int)ESkillSheetMode.wild_child_abilities, GetString("ui_skillsheet_monsterabilities"));

        if (autoConvertToUpper)
        {
            keys = d.Keys.ToList();
            foreach (int key in keys)
            {
                d[key] = d[key].ToUpperInvariant();
            }
        }

        // == Equipment Categories ==========================================================
        d = new Dictionary<int, string>();
        dictEnumStrings.Add(typeof(GearFilters), d);

        d.Add((int)GearFilters.VIEWALL, GetString("ui_misc_viewall"));
        d.Add((int)GearFilters.ACCESSORY, GetString("eq_slot_accessory_plural"));
        d.Add((int)GearFilters.ARMOR, GetString("eq_slot_armor_plural"));
        d.Add((int)GearFilters.COMMON, GetString("misc_rarity_0"));
        d.Add((int)GearFilters.GEARSET, GetString("misc_rarity_5"));
        d.Add((int)GearFilters.LEGENDARY, GetString("misc_rarity_4b"));
        d.Add((int)GearFilters.MAGICAL, GetString("misc_rarity_2"));
        d.Add((int)GearFilters.OFFHAND, GetString("eq_slot_offhand_plural"));
        d.Add((int)GearFilters.WEAPON, GetString("eq_slot_weapon_plural"));
        d.Add((int)GearFilters.FAVORITES, GetString("item_filters_favorites"));

        if (autoConvertToUpper)
        {
            keys = d.Keys.ToList();
            foreach (int key in keys)
            {
                d[key] = d[key].ToUpperInvariant();
            }
        }

        // == Character Sheet Info ==========================================================
        d = new Dictionary<int, string>();
        dictEnumStrings.Add(typeof(Switch_UICharacterSheet.ECharacterSheetValueType), d);

        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.discipline, GetString("stat_discipline"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.energy, GetString("stat_energy"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.guile, GetString("stat_guile"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.health, GetString("stat_health"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.spirit, GetString("stat_spirit"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.stamina, GetString("stat_stamina"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.stremf, GetString("stat_strength"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.swiftness, GetString("stat_swiftness"));

        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.all_damage_mod, GetString("stat_alldamage"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.block_chance, GetString("stat_blockchance"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.all_defense_mod, GetString("stat_alldefense"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.charge_time, GetString("misc_ct_gain"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.crit_chance, GetString("stat_critchance"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.crit_damage, GetString("misc_crit_damage"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.dodge_chance, GetString("stat_dodgechance"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.parry_chance, GetString("stat_parrychance"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.powerup_drop, GetString("stat_powerupdrop"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.spirit_power, GetString("stat_spiritpower"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.weapon_power, GetString("ui_equipment_weaponpower"));

        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_physical, GetString("misc_dmg_physical"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_fire, GetString("misc_dmg_fire"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_poison, GetString("misc_dmg_poison"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_water, GetString("misc_dmg_water"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_lightning, GetString("misc_dmg_lightning"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.element_shadow, GetString("misc_dmg_shadow"));

        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.highest_floor, GetString("saveslot_highestfloor"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.highest_floor_ever, GetString("highest_floor_ever"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.days_passed, GetString("misc_days_passed"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.monsters_killed, GetString("monsters_defeated"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.champs_killed, GetString("champions_defeated"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.steps_taken, GetString("steps_taken"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.favorite_job, GetString("favorite_job"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.total_characters, GetString("total_characters"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.playtime, GetString("playtime"));
        d.Add((int)Switch_UICharacterSheet.ECharacterSheetValueType.pandora_boxes, GetString("ui_pandora_opened"));
    }

    static void AssignCharacterJobNames()
    {
        GameMasterScript.characterJobNames = new string[(int)CharacterJobs.COUNT - 2];
        GameMasterScript.characterJobNames[(int)CharacterJobs.BRIGAND] = StringManager.GetString("job_brigand");
        GameMasterScript.characterJobNames[(int)CharacterJobs.FLORAMANCER] = StringManager.GetString("job_floramancer");
        GameMasterScript.characterJobNames[(int)CharacterJobs.SWORDDANCER] = StringManager.GetString("job_sworddancer");
        GameMasterScript.characterJobNames[(int)CharacterJobs.SPELLSHAPER] = StringManager.GetString("job_spellshaper");
        GameMasterScript.characterJobNames[(int)CharacterJobs.PALADIN] = StringManager.GetString("job_paladin");
        GameMasterScript.characterJobNames[(int)CharacterJobs.BUDOKA] = StringManager.GetString("job_budoka");
        GameMasterScript.characterJobNames[(int)CharacterJobs.HUNTER] = StringManager.GetString("job_hunter");
        GameMasterScript.characterJobNames[(int)CharacterJobs.GAMBLER] = StringManager.GetString("job_gambler");
        GameMasterScript.characterJobNames[(int)CharacterJobs.HUSYN] = StringManager.GetString("job_husyn");
        GameMasterScript.characterJobNames[(int)CharacterJobs.SOULKEEPER] = StringManager.GetString("job_soulkeeper");
        GameMasterScript.characterJobNames[(int)CharacterJobs.EDGETHANE] = StringManager.GetString("job_edgethane");
        GameMasterScript.characterJobNames[(int)CharacterJobs.WILDCHILD] = StringManager.GetString("job_wildchild");
        GameMasterScript.characterJobNames[(int)CharacterJobs.SHARA] = StringManager.GetString("exp_jobname_sorceress");
        GameMasterScript.characterJobNames[(int)CharacterJobs.DUALWIELDER] = StringManager.GetString("exp_job_dualwielder");
        GameMasterScript.characterJobNames[(int)CharacterJobs.MIRAISHARA] = StringManager.GetString("exp_jobname_sorceress");
    }
}
