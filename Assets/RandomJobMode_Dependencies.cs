using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RandomJobMode {

    static Dictionary<string, string> forcePairedInnates;

    static void CreateForcePairedInnateDictionary()
    {
        if (forcePairedInnates != null) return;

        forcePairedInnates = new Dictionary<string, string>();

        forcePairedInnates["skill_gamblercrit"] = "skill_gamblerstatbonus";
        forcePairedInnates["skill_miraisharabonus1"] = "skill_sorceressstatbonus";
        forcePairedInnates["skill_wildchildbonus1"] = "skill_wildchildstatbonus";
        forcePairedInnates["skill_thanebonus1"] = "skill_edgethanestatbonus";
        forcePairedInnates["skill_dropsoul"] = "skill_soulkeeperstatbonus";
        forcePairedInnates["skill_recharge"] = "skill_husynstatbonus";
        forcePairedInnates["skill_preciseshot"] = "skill_hunterstatbonus";
        forcePairedInnates["skill_paladinblockbuff"] = "skill_paladinstatbonus";
        forcePairedInnates["skill_spellshaperpowerupbuff"] = "skill_spellshaperstatbonus";
        forcePairedInnates["skill_dancermove"] = "skill_sworddancerstatbonus";
        forcePairedInnates["skill_compost"] = "skill_floramancerstatbonus";
        forcePairedInnates["skill_bleedbonus"] = "skill_brigandstatbonus";
        forcePairedInnates["skill_dualwielderbonus1"] = "skill_dualwielderstatbonus";
    }

    static void CreatePairingDictionary()
    {
        if (dictAbilitiesToPairedRequiredAbilities != null) return;

        dictAbilitiesToPairedRequiredAbilities = new Dictionary<string, List<string>>();

        List<string> allEvocations = new List<string>() { "skill_fireevocation", "skill_shadowevocation", "skill_iceevocation", "skill_acidevocation" };
        List<string> allSpellshapes = new List<string>() { "skill_spellshapeline", "skill_spellshaperay", "skill_spellshapesquare", "skill_spellshiftpenetrate", "skill_spellshiftmaterialize", "skill_spellshiftbarrier" };

        foreach (string str in allSpellshapes)
        {
            dictAbilitiesToPairedRequiredAbilities[str] = allEvocations;
        }

        List<string> allVerses = new List<string>() { "skill_versesuppression", "skill_versechallenges", "skill_verseelements" };
        List<string> allSongs = new List<string>() { "skill_songmight", "skill_songendurance", "skill_songspirit" };

        foreach (string str in allVerses)
        {
            dictAbilitiesToPairedRequiredAbilities[str] = allSongs;
        }

        dictAbilitiesToPairedRequiredAbilities["skill_furiouscrescendo"] = allSongs;
        dictAbilitiesToPairedRequiredAbilities["skill_highlandcharge"] = allSongs;
        dictAbilitiesToPairedRequiredAbilities["skill_photosynthesis"] = new List<string>() { "skill_summonplantturret", "skill_bedofthorns", "skill_summonlivingvine" };

        List<string> abilitiesThatSummonVines = new List<string>() { "skill_summonlivingvine", "skill_summonanchorvine" };

        dictAbilitiesToPairedRequiredAbilities["skill_summonanchorvine"] = abilitiesThatSummonVines;
        dictAbilitiesToPairedRequiredAbilities["skill_detonatevines"] = abilitiesThatSummonVines;
        dictAbilitiesToPairedRequiredAbilities["skill_auraofgrowth"] = new List<string>() { "skill_detonatevines", "skill_vineswing" };
        dictAbilitiesToPairedRequiredAbilities["skill_vineswing"] = abilitiesThatSummonVines;

        dictAbilitiesToPairedRequiredAbilities["skill_crystalshift"] = new List<string>() { "skill_runiccrystal" };
        dictAbilitiesToPairedRequiredAbilities["skill_fortify"] = new List<string>() { "skill_runiccrystal" };
        dictAbilitiesToPairedRequiredAbilities["skill_gravitysurge"] = new List<string>() { "skill_runiccrystal" };

        List<string> vitalPoints = new List<string>() { "skill_ppexplode", "skill_ppbleed", "skill_ppextradamage" };
        /* List<string> budokaComboAbilities = new List<string>() { "skill_hundredfists", "skill_powerkick", "skill_qistrike" };

        foreach(string str in vitalPoints)
        {
            dictAbilitiesToPairedRequiredAbilities[str] = budokaComboAbilities;
        } */

        dictAbilitiesToPairedRequiredAbilities["skill_partinggift"] = new List<string>() { "skill_summonshade", "skill_summonelemspirit", "skill_revivemonster" };

        // innate abilities have some restrictions

        // Brigand
        dictAbilitiesToPairedRequiredAbilities["skill_brigandbomber"] = new List<string>() { "skill_cloakanddagger", "skill_shadowstep" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallbrigandskills"] = new List<string>() { "skill_cloakanddagger", "skill_shadowstep", "skill_shrapnelbomb", "skill_fanofknives", "skill_shadowstep" };

        // Floramancer
        dictAbilitiesToPairedRequiredAbilities["skill_compost"] = new List<string>() { "skill_summonlivingvine" };
        dictAbilitiesToPairedRequiredAbilities["skill_floraconda2"] = new List<string>() { "skill_summonlivingvine" };
        dictAbilitiesToPairedRequiredAbilities["skill_floraconda3"] = new List<string>() { "skill_summonlivingvine" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallfloramancerskills"] = new List<string>()
        { "skill_bedofthorns", "skill_detonatevines", "skill_creepingdeath" };

        // Budoka
        dictAbilitiesToPairedRequiredAbilities["skill_lethalfists"] = vitalPoints;

        // Sword Dancer
        dictAbilitiesToPairedRequiredAbilities["skill_dancermove"] = new List<string>() { "skill_flameserpent", "skill_icetortoise" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_status_mmbutterfly"] = new List<string>()
        { "skill_flameserpent", "skill_icetortoise", "skill_thunderinglion", "skill_phoenixwing", "skill_relentlesscurrent" };

        // Paladin
        dictAbilitiesToPairedRequiredAbilities["skill_paladinbonus2"] = new List<string>() { "skill_smiteevil", "skill_divineretribution", "skill_shieldslam" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallpaladinskills"] = new List<string>()
        { "skill_smiteevil", "skill_divineretribution", "skill_blessedhammer" };

        // Spellshaper
        dictAbilitiesToPairedRequiredAbilities["skill_spellshaperbonus2"] = allEvocations;

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallspellshaperskills"] = new List<string>()
        { "skill_fireevocation", "skill_shadowevocation", "skill_iceevocation", "skill_acidevocation" };

        // Edge Thane
        dictAbilitiesToPairedRequiredAbilities["skill_thanebonus2"] = allSongs;
        dictAbilitiesToPairedRequiredAbilities["skill_gloriousbattler"] = allSongs;

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostalledgethaneskills"] = new List<string>()
        { "skill_highlandcharge", "skill_furiouscrescendo" };

        // Hunter

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallhunterskills"] = new List<string>()
        { "skill_hailofarrows", "skill_icemissile", "skill_triplebolt" };

        // Soulkeeper

        List<string> soulUsingAbilities = new List<string>() { "skill_aetherslash", "skill_revivemonster", "skill_summonshade", "skill_echobolts", "skill_summonelemspirit", "skill_balefulechoes" };

        dictAbilitiesToPairedRequiredAbilities["skill_dropsoul"] = soulUsingAbilities;
        dictAbilitiesToPairedRequiredAbilities["skill_dropsoul2"] = soulUsingAbilities;
        dictAbilitiesToPairedRequiredAbilities["skill_spiritmaster"] = soulUsingAbilities;

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallsoulkeeperskills"] = new List<string>()
        { "skill_aetherslash", "skill_echobolts" };

        // Gambler

        dictAbilitiesToPairedRequiredAbilities["skill_gamblerbonus2"] = new List<string>() { "skill_wildcards" };
        dictAbilitiesToPairedRequiredAbilities["skill_gamblerbonus3"] = new List<string>() { "skill_wildcards" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallgamblerskills"] = new List<string>()
        { "skill_wildcards", "skill_rollthebones", "skill_doubledown", "skill_hotstreak", "skill_goldtoss" };

        // HuSyn

        dictAbilitiesToPairedRequiredAbilities["skill_recharge"] = new List<string>() { "skill_runiccrystal" };
        dictAbilitiesToPairedRequiredAbilities["skill_runic2"] = new List<string>() { "skill_runiccrystal" };

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_boostallhusynskills"] = new List<string>()
        { "skill_photoncannon", "skill_crystalshift" };

        // Calligrapher
        List<string> calligrapherScrolls = new List<string>() { "skill_waterscroll", "skill_lightningscroll", "skill_shadowscroll" };
        List<string> calligrapherScrollsPlusInkstorm = new List<string>() { "skill_waterscroll", "skill_lightningscroll", "skill_shadowscroll", "skill_inkstorm" };

        dictAbilitiesToPairedRequiredAbilities["skill_dualwielderbonus2"] = calligrapherScrolls;
        dictAbilitiesToPairedRequiredAbilities["skill_dualwielderbonus3"] = calligrapherScrollsPlusInkstorm;

        dictAbilitiesToPairedRequiredAbilities["rj_skill_se_status_enhancebleed"] = new List<string>() { "skill_cloakanddagger", "skill_shadowstep", "skill_ppbleed" };
    }
}
