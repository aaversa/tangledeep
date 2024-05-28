using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BakedStatusEffectDefinitions
{
    // This function runs after the main LoadAllStatuses call runs.

    static readonly string[] permaBuffNoRunStatuses = new string[]
    {
        "status_mmsharktooth",
        "status_mmlucky",
        "status_plantsynergypassive",
        "monmod_steeltoe",
        "status_fastlearner",
        "status_toughness",
        "status_keeneyes",
        "notorious",
        "luciddreamer",
        "status_intimidating",
        "status_explorer",
        "status_thirstquencher",
        "status_entrepreneur",
        "status_rager",
        "status_foodlover",
        "status_scavenger",
        "status_paladinblockbuff",
        "status_spellshapemaster",
        "status_bloodspecialist",
        "status_qimasterypassive",
        "status_effortlessparry",
        "status_floraconda2",
        "status_floraconda3",
        "status_brigandbomber",
        "status_lethalfists",
        "status_preciseshot",
        "status_deadlyfocus",
        "status_arrowcatch",
        "status_unarmedfighting2",
        "status_shieldmastery",
        "status_dragontailpassive",
        "status_alwaysriposte",
        "status_thefourthsword",
        "status_manaseeker",
        "status_detectweakness",
        "status_compost",
        "status_spellshaperpowerup",
        "status_mmzen",
        "status_mmpenetrating",
        "status_mmgluttony",
        "status_mmgluttony2",
        "sthergebonus1",
        "status_mmclotting",
        "status_armortraining",
        "status_divineprotection",
        "status_bloodlust",
        "status_gamblercrit",
        "gamblercards2",
        "gamblercards3",
        "status_cheatdeath",
        "status_fatchance",
        "husynrecharge",
        "dropsoul",
        "dropsoul2",
        "husynrunicbuff",
        "hazardsweep",
        "harvestrobot",
        "asceticgrab",
        "qiwaveset",
        "status_dancermove",
        "magicmirrors",
        "shadowcast",
        "draik",
        "wildnaturebonus1",
        "wildnaturebonus2",
        "mmhairband",
        "status_mmvengeance",
        "status_soothingaura",
        "status_bountiful",
        "status_jumpboots",
        "blizzardgearbonus2",
        "monmod_avenger",
        "crystalarmamentbonus2",
        "fistmastery2",
        "status_collect_wildcards",
        "spiritcollector",
        "eyeshield",
        "findhealth",
        "spiritmaster",
        "status_prismatic",
        "thanebonus2",
        "glorious_battler_passive",
        "twohand_specialist",
        "status_extrajpxp",
        "status_jp10perm",
        "blackfurbonus2",
        "emeraldsetbonus1",
        "emeraldsetbonus2",
        "wildchildbonus2",
        "wildchildbonus3",
        "learnmonsterskills",
        "status_panthoxskin",
        "status_herbforaging",
        "status_dragonbrave",
        "status_fatemiss",
        "ramirelstealth",
        "heavyarmormastery1",
        "lightarmormastery1",
        "mediumarmormastery1",
        "status_jptogold",
        "status_taunting",
        "status_fasthealing",
        "emblem_brigand_tier1_smokecloud",
        "emblem_floramancer_tier0_pethealth",
        "emblem_floramancer_tier1_resummon",
        "emblem_floramancer_tier2_pethealing",
        "emblem_floramancer_tier2_vineburst",
        "emblem_sworddanceremblem_tier2_parry",
        "emblem_sworddanceremblem_tier2_icefreeze",
        "emblem_sworddanceremblem_tier2_dragontail",
        "emblem_spellshaperemblem_tier0_aura",
        "emblem_spellshaperemblem_tier1_evocation",
        "emblem_spellshaperemblem_tier2_tpburst",
        "emblem_soulkeeperemblem_tier0_pets",
        "emblem_soulkeeperemblem_tier1_reflect",
        "emblem_soulkeeperemblem_tier1_summonlength",
        "emblem_paladinemblem_tier0_block",
        "emblem_paladinemblem_tier1_wrathelemdmg",
        "emblem_paladinemblem_tier1_wrathelemdef",
        "emblem_paladinemblem_tier2_maxblock",
        "emblem_wildchildemblem_tier0_technique",
        "emblem_wildchildemblem_tier1_champion",
        "emblem_wildchildemblem_tier2_straddle",
        "emblem_gambleremblem_tier2_luck",
        "emblem_budokaemblem_tier0_spiritfists",
        "emblem_budokaemblem_tier2_hamedo",
        "emblem_husynemblem_tier0_runic",
        "emblem_husynemblem_tier1_runic",
        "emblem_husynemblem_tier2_runic",
        "emblem_hunteremblem_tier0_shadow",
        "emblem_hunteremblem_tier1_wolf",
        "emblem_hunteremblem_tier1_shadow",
        "emblem_hunteremblem_tier2_stalk",
        "emblem_brigand_tier0_bleedbonus",
        "mmchef",
        "frogsuit",
        "dummystatus",
        "status_pointblankshot",
        "miraisharabonus1",
        "status_axestyle",
        "twoarrows",
        "goldpickup_power"
    };

    public static void AddAllBakedStatusDefinitions()
    {
        AddAllBakedStatusDefinitions_DLC1();
        AddAllBakedStatusDefinitions_DLC2();

        //if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) || DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            foreach (string abilRef in permaBuffNoRunStatusMultiDLC)
            {
                SetupPermaBuffNoRunStatus(abilRef);
            }
        }

        foreach (string abilRef in permaBuffNoRunStatuses)
        {
            SetupPermaBuffNoRunStatus(abilRef);
        }

        SetupSimpleParryStatuses();
        SetupSimpleDodgeStatuses();
    }

    static StatusEffect SetupPermaBuffNoRunStatus(string abilRef)
    {
        StatusEffect se = new StatusEffect();
        se.refName = abilRef;
        se.runStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
        se.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
        se.isPositive = true;
        se.noRemovalOrImmunity = true;
        se.stackMultipleDurations = true;

        if (abilRef == "status_armortraining")
        {
            se.stackMultipleEffects = true;
        }

        if (GameMasterScript.masterStatusList.ContainsKey(se.refName))
        {
            Debug.Log("WARNING: Master status dict already has " + se.refName);
        }
        else
        {
            GameMasterScript.masterStatusList.Add(se.refName, se);
        }


        if (abilRef == "magicmirrors")
        {
            se.stackMultipleEffects = true;
            se.stackMultipleDurations = false;
        }
        return se;
    }

    static void SetupSimpleDodgeStatuses()
    {
        // status_mmdodge1
        // runs on attacked, dur permanent
        // positive, stack multiple

        for (int i = 1; i <= 50; i++)
        {
            StatusEffect dodge = new StatusEffect();
            dodge.isPositive = true;
            dodge.stackMultipleEffects = true;
            dodge.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
            dodge.runStatusTriggers[(int)StatusTrigger.ATTACKED] = true;
            dodge.refName = "status_mmdodge" + i;
            dodge.showIcon = false;

            float dodgeAmount = i * 0.01f;

            AttackReactionEffect arDodgeEffect = new AttackReactionEffect();
            arDodgeEffect.effectType = EffectType.ATTACKREACTION;
            arDodgeEffect.effectRefName = "dodge" + i;
            arDodgeEffect.effectEquation = dodgeAmount.ToString().Replace(',', '.');
            arDodgeEffect.silent = true;
            arDodgeEffect.triggerCondition = AttackConditions.ANY;
            arDodgeEffect.reactCondition = AttackConditions.ANY;
            arDodgeEffect.alterAccuracyFlat = -1 * i;

            dodge.listEffectScripts.Add(arDodgeEffect);

            if (!GameMasterScript.masterEffectList.ContainsKey(arDodgeEffect.effectRefName))
            {
                GameMasterScript.masterEffectList.Add(arDodgeEffect.effectRefName, arDodgeEffect);
            }

            if (GameMasterScript.masterStatusList.ContainsKey(dodge.refName))
            {
                if (Debug.isDebugBuild) Debug.Log("WARNING: Master status dict already has " + dodge.refName);
            }
            else
            {
                GameMasterScript.masterStatusList.Add(dodge.refName, dodge);
            }
        }
    }

    static void SetupSimpleParryStatuses()
    {
        // Add status effects in the format:
        // refname: status_mmparry8
        // no icon, no display name
        // permanent buffs run when attacked
        // positive, stack multiple
        // Sole effect is:
        /* < Type > ATTACKREACTION </ Type >
        < ERef > parry8 </ ERef >
        < Eq > 0.08 </ Eq >
        < AlterParryFlat > 0.08 </ AlterParryFlat >
        < TCon > ANY </ TCon >
        < Silent /> */

        for (int i = 1; i <= 30; i++)
        {
            StatusEffect parry = new StatusEffect();
            parry.isPositive = true;
            parry.stackMultipleEffects = true;
            parry.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
            parry.runStatusTriggers[(int)StatusTrigger.ATTACKED] = true;
            parry.refName = "status_mmparry" + i;
            parry.showIcon = false;

            float parryAmount = i * 0.01f;

            AttackReactionEffect arParryEffect = new AttackReactionEffect();
            arParryEffect.effectType = EffectType.ATTACKREACTION;
            arParryEffect.effectRefName = "parry" + i;
            arParryEffect.effectEquation = parryAmount.ToString().Replace(',','.');
            arParryEffect.silent = true;
            arParryEffect.triggerCondition = AttackConditions.ANY;
            arParryEffect.alterParryFlat = parryAmount;

            parry.listEffectScripts.Add(arParryEffect);

            if (!GameMasterScript.masterEffectList.ContainsKey(arParryEffect.effectRefName))
            {
                GameMasterScript.masterEffectList.Add(arParryEffect.effectRefName, arParryEffect);
            }

            if (GameMasterScript.masterStatusList.ContainsKey(parry.refName))
            {
                Debug.Log("WARNING: Master status dict already has " + parry.refName);
            }
            else
            {
                GameMasterScript.masterStatusList.Add(parry.refName, parry);
            }
        }
    }
}
