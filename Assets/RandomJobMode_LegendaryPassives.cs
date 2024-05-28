using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RandomJobMode {

    public class LegRJInfo
    {
        public int sprite;
    }

    static void InitializeLegendaryOnlyPassives()
    {
        List<string> modContentToIgnore = new List<string>()
        {
            "emblem",
            "passive",
            "gaelmydd", // could be free passive?
            "ascetic",
            "draik",
            "blightpoison",
            "starhelm",
            "wildnatureband", // free?
            "crystalspear",
            "findweakness",
            "rubymoon",
            "summonice", // wonky
            "plantgrowth",
            "moonbeams", // strong
            "budoka",
            "attackstep"

        };

        //questionable
        //moonbeams - long anim, strong
        //legshard - strong
        //magicmirrors - strong
        //dragonscale - strong
        // wildnatureband - weak
        // bigstick - strong

        Dictionary<string, string> abilRefsToSprites = new Dictionary<string, string>()
        {
            {  "mm_athyes","36" },
            { "mm_ultraheavy","55" },
            { "mm_legshard", "44" },
            {"mm_legkatana", "24" },
            {"mm_procbolt", "183" },
            { "mm_procshadow", "78" },
            { "mm_soulsteal", "200" },
            { "mm_antipode", "36" },
            { "mm_oceangem", "223" },
            { "mm_magicmirrors", "99" },
            { "mm_shadowcast", "105" },
            { "mm_doublebite", "119" },
            { "mm_dragonscale", "149" },
            { "mm_aetherslash", "121" },
            { "mm_bigstick", "172" },
            { "mm_wildnaturevest", "3" },
            { "mm_hairband", "243" },
            { "mm_freezeattack", "246" },
            { "mm_chillaura", "37" },
            { "mm_paralyzereact", "215" },
            { "mm_catears", "220"},
            { "mm_trumpet", "148" },
            { "mm_fistattackproc", "70" },
            {  "mm_findhealth", "201"}, // too strong?
            { "mm_butterfly", "22" }, // sword dancer only
            { "mm_hergerobe", "24" },
            { "mm_vezakpoison", "12"},
            { "mm_shootfire", "16" }, // too strong?
            { "mm_swing_defense", "107" },
            { "mm_immune_poisonbleed", "6" },
            { "mm_immune_defenselower", "74" },
            { "mm_summonthorns", "7" },
            { "mm_seraphblock", "59" },
            { "mm_chance_freespell", "48" },
            { "mm_ignorelowdamage", "9" },
            { "mm_songblade", "151" },
            { "mm_phasmaquiver", "63" },
            { "mm_breathstealer", "145" },
            { "mm_dismantler", "188" },
            { "mm_ramirelmask", "26" }, // weak?
            { "mm_starcall", "125" }, // strong?
            { "mm_meltblockparry", "144" },
            { "mm_confuseblock", "157" },
            { "mm_enhancebleed", "32" },
            { "mm_powershot", "61" }, // strong?
            { "mm_paladinboost", "51" },
            { "mm_brigandboost", "13" },
            { "mm_soulkeeperboost", "123" },
            { "mm_hunterboost", "62" },
            { "mm_floramancerboost", "0" },
            { "mm_edgethaneboost", "152" },
            { "mm_gamblerboost", "93" },
            { "mm_spellshaperboost", "42" },
            //{ "mm_sworddancerboost", "20"},
            { "mm_husynboost", "84" }
        };

        string passive = StringManager.GetString("misc_normalpassive");

        foreach (var kvp in LegendaryMaker.legendaryOnlyMods)
        {
            bool skip = false;
            foreach (string str in modContentToIgnore)
            {
                if (kvp.Key.Contains(str))
                {
                    skip = true;
                    break;
                }
            }

            if (!abilRefsToSprites.ContainsKey(kvp.Key)) skip = true;

            if (skip) continue;


            // Step 1 - get the mod
            MagicMod mm = GameMasterScript.masterMagicModList[kvp.Key];

            if (mm.modEffects.Count == 0)
            {
                //Debug.Log(mm.refName + " has no mod effects? ok");
                continue;
            }

            // Step 2 - pull out the status
            StatusEffect se = mm.modEffects[0];

            // step 3 - make a pretend passive ability
            AbilityScript thePassive = new AbilityScript();
            thePassive.refName = "rj_skill_" + se.refName;

            foreach (string tag in se.numberTags)
            {
                thePassive.numberTags.Add(tag);
            }

            //Debug.Log("Process " + mm.refName + " which is " + thePassive.refName);

            thePassive.abilityName = StringManager.GetString(mm.refName + "_passive_name");
            if (string.IsNullOrEmpty(thePassive.abilityName)) thePassive.abilityName = thePassive.refName;
            thePassive.description = passive + " " + mm.GetDescription();
            thePassive.displayInList = true;
            thePassive.passiveAbility = true;
            thePassive.jobLearnedFrom = CharacterJobs.BRIGAND;
            thePassive.usePassiveSlot = true;
            thePassive.iconSprite = "SkillIcons_" + abilRefsToSprites[mm.refName];
            thePassive.shortDescription = thePassive.description;

            GameMasterScript.masterAbilityList.Add(thePassive.refName, thePassive);

            AddStatusEffect newPassiveEffect = new AddStatusEffect();
            newPassiveEffect.effectType = EffectType.ADDSTATUS;
            newPassiveEffect.effectRefName = thePassive.refName + "_psv";
            newPassiveEffect.statusRef = se.refName;
            newPassiveEffect.tActorType = TargetActorType.ORIGINATING;
            newPassiveEffect.parentAbility = thePassive;

            thePassive.AddEffectScript(newPassiveEffect);

            GameMasterScript.masterEffectList.Add(newPassiveEffect.effectRefName, newPassiveEffect);

            // step 4 - create the job ability
            JobAbility ja = CreateJobAbility(thePassive.refName, RANDOM_JOB_MODE_PASSIVE_JP_COST);

            allPossiblePassives.Add(ja);

            //Debug.Log("Created job ability and passive " + thePassive.refName);
        }
    }
}
