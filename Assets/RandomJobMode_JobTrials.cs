using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RandomJobMode
{
    public static Dictionary<string, List<string>> jobEmblemAbilityRequirements = new Dictionary<string, List<string>>()
    {
        {"brigandemblem_tier1_smokecloud", new List<string>() {"skill_smokecloud"} },
        {"floramanceremblem_tier0_pethealth", new List<string>() {"skill_summonlivingvine", "skill_summonelemspirit", "skill_summonshade", "skill_summonplantturret"} },
        {"mm_floramanceremblem_tier1_resummon", new List<string>() {"skill_summonlivingvine", "skill_summonelemspirit", "skill_summonshade", "skill_summonplantturret"} },
        {"mm_floramanceremblem_tier1_thornbuff", new List<string>() {"skill_thornedskin"} },
        {"mm_floramanceremblem_tier2_pethealing", new List<string>() {"skill_summonlivingvine", "skill_summonelemspirit", "skill_summonshade", "skill_summonplantturret"} },
        {"mm_floramanceremblem_tier2_vineburst", new List<string>() {"skill_auraofgrowth", "skill_summonanchorvine"} },
        {"mm_sworddanceremblem_tier0_wildhorse", new List<string>() {"skill_wildhorse"} },
        {"mm_sworddanceremblem_tier2_icefreeze", new List<string>() {"skill_relentlesscurrent", "skill_icetortoise"} },
        {"mm_spellshaperemblem_tier0_aura", new List<string>() { "skill_spellshiftbarrier"} },
        {"mm_spellshaperemblem_tier1_evocation", new List<string>() { "skill_iceevocation", "skill_fireevocation", "skill_shadowevocation", "skill_acidevocation" } },
        {"mm_spellshaperemblem_tier2_tpburst", new List<string>() { "skill_delayedteleport" } },
        {"mm_soulkeeperemblem_tier0_pets", new List<string>() {"skill_summonlivingvine", "skill_summonelemspirit", "skill_summonshade", "skill_summonplantturret"} },
        {"mm_soulkeeperemblem_tier1_summonlength", new List<string>() {"skill_summonlivingvine", "skill_summonelemspirit", "skill_summonshade", "skill_summonplantturret"} },
        {"mm_soulkeeperemblem_tier2_echopull", new List<string>() {"skill_aetherslash", "skill_summonelemspirit", "skill_summonshade", "skill_revivemonster", "skill_echobolts", "skill_balefulechoes" } },
        {"mm_paladinemblem_tier0_divinedmg", new List<string>() { "skill_smiteevil", "skill_divineretribution", "skill_blessedhammer" } },
        {"mm_paladinemblem_tier0_block", new List<string>() { "skill_smiteevil", "skill_divineretribution", "skill_blessedhammer" } },
        {"mm_paladinemblem_tier1_wrathelemdmg", new List<string>() { "skill_smiteevil" } },
        {"mm_paladinemblem_tier1_wrathelemdef", new List<string>() { "skill_smiteevil" } },
        {"mm_paladinemblem_tier2_critwrath", new List<string>() { "skill_smiteevil" } },
        {"mm_wildchildemblem_tier2_monpowers", new List<string>() { "exclude"} },
        {"mm_wildchildemblem_tier2_straddle", new List<string>() { "skill_straddlemonster"} },
        {"mm_edgethaneemblem_tier0_song", new List<string>() { "skill_songmight", "skill_songendurance", "skill_songspirit" } },
        {"mm_edgethaneemblem_tier1_song", new List<string>() { "skill_songmight", "skill_songendurance", "skill_songspirit" } },
        {"mm_edgethaneemblem_tier2_song", new List<string>() { "skill_songmight", "skill_songendurance", "skill_songspirit" } },
        {"mm_gambleremblem_tier2_cards", new List<string>() { "skill_wildcards"} },
        {"mm_gambleremblem_tier2_luck", new List<string>() { "skill_wildcards"} },
        {"mm_budokaemblem_tier2_qi", new List<string>() {"exclude"} },
        {"mm_husynemblem_tier0_runic", new List<string>() {"skill_runiccrystal"} },
        {"mm_husynemblem_tier1_runic", new List<string>() {"skill_runiccrystal"} },
        {"mm_husynemblem_tier2_energy", new List<string>() {"skill_runiccrystal"} },
        {"mm_husynemblem_tier2_runic", new List<string>() {"skill_runiccrystal"} },
        {"mm_hunteremblem_tier0_shadow", new List<string>() {"skill_grapplinghook"} },
        {"mm_hunteremblem_tier1_wolf", new List<string>() {"skill_hunterwolf"} },
        {"mm_hunteremblem_tier2_tech", new List<string>() {"skill_hailofarrows", "skill_triplebolt"} },
        {"mm_hunteremblem_tier2_stalk", new List<string>() { "skill_bloodtracking" } },
        { "mm_dualwielderemblem_tier0_biography", new List<string>() {"exclude"} },
        { "mm_dualwielderemblem_tier1_glide", new List<string>() {"skill_glide"} },
        { "mm_dualwielderemblem_tier2_brush", new List<string>() {"skill_inkstorm", "skill_forbiddance"} },

    };

   
}
