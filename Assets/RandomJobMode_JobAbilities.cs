using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RandomJobMode
{

    public static Dictionary<string, JobAbility> dictSpecialJobAbilities;

    private static List<string> skillsToConvertToActiveJobAbilities = new List<string>()
    {
        "skill_swordmastery1","skill_axemastery1","skill_bowmastery1","skill_spearmastery1","skill_clawmastery1",
        "skill_staffmastery1","skill_daggermastery1","skill_macemastery1",
        "skill_playerfirebreath","skill_playerthunderstorm","skill_playerfroghop","skill_flurryofbites","skill_playervinepull",
        "skill_playerclawrake","skill_playersharkroar","skill_playerwebshot","skill_playermortarfire","skill_playerneutralize",
        "skill_playericetraps","skill_playerrocktoss","skill_acidpoolpotion_rj","skill_summonoil_rj","skill_stealthpotion_rj",
        "skill_pumpkinbread_rj","skill_spicytacos_rj","skill_summonmonster_rj","skill_teleport_rj"
    };

    private static List<string> skillsToConvertToPassiveJobAbilities = new List<string>()
    {
        "skill_swordmastery2","skill_swordmastery3",
        "skill_axemastery2","skill_axemastery3",
        "skill_bowmastery2","skill_bowmastery3",
        "skill_spearmastery2","skill_spearmastery3",
        "skill_clawmastery2","skill_clawmastery3",
        "skill_staffmastery2","skill_staffmastery3",
        "skill_daggermastery2","skill_daggermastery3",
        "skill_macemastery2","skill_macemastery3"
    };

    public static void InitializeSpecialJobAbilities()
    {
        if (dictSpecialJobAbilities != null) return;

        dictSpecialJobAbilities = new Dictionary<string, JobAbility>();

        InitializeLegendaryOnlyPassives();

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            skillsToConvertToActiveJobAbilities.Add("skill_whipmastery1");

            skillsToConvertToPassiveJobAbilities.Add("skill_whipmastery2");
            skillsToConvertToPassiveJobAbilities.Add("skill_whipmastery3");
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            skillsToConvertToActiveJobAbilities.Add("skill_playerglitterskin");
            skillsToConvertToActiveJobAbilities.Add("skill_playerspinblade");
            skillsToConvertToActiveJobAbilities.Add("skill_playerspitice");
        }

        foreach(string abilRef in skillsToConvertToPassiveJobAbilities)
        {
            JobAbility ja = CreateJobAbility(abilRef, RANDOM_JOB_MODE_PASSIVE_JP_COST);        
            allPossiblePassives.Add(ja);
        }

        foreach (string abilRef in skillsToConvertToActiveJobAbilities)
        {
            JobAbility ja = CreateJobAbility(abilRef, RANDOM_JOB_MODE_ACTIVE_JP_COST);

            allPossibleActives.Add(ja);
        }
    }

    static JobAbility CreateJobAbility(string abilRef, int jpCost)
    {
        JobAbility ja = new JobAbility();
        ja.ability = GameMasterScript.masterAbilityList[abilRef];
        ja.abilityRef = abilRef;
        ja.innate = false;
        ja.jobParent = CharacterJobs.BRIGAND;
        ja.jpCost = jpCost;

        dictSpecialJobAbilities.Add(abilRef, ja);
        dictAllJobAbilities.Add(abilRef, ja);

        return ja;
    }



}
