using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasteriesBakedData  {

    public static List<string> allBaseWeaponMasteries = new List<string>()
        {
            "skill_clawmastery1", "skill_swordmastery1", "skill_axemastery1", "skill_macemastery1", "skill_daggermastery1",
            "skill_staffmastery1", "skill_bowmastery1", "skill_spearmastery1", "skill_fistmastery1"
        };

    public static List<string> allArmorMasteries = new List<string>()
        {
            "skill_lightarmormastery1", "skill_mediumarmormastery1", "skill_heavyarmormastery1"
        };

    
    /// <summary>
    /// Game MUST be started with a heroPCActor for this to work!
    /// </summary>
    /// <returns></returns>
    public static string GetUnknownWeaponMastery()
    {
        List<string> availableMasteries = new List<string>();

        foreach (string str in MasteriesBakedData.allBaseWeaponMasteries)
        {
            if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(str))
            {
                availableMasteries.Add(str);
            }
        }

        if (availableMasteries.Count == 0) return "";
        return availableMasteries.GetRandomElement();
    }

    /// <summary>
    /// Game MUST be started with a heroPCActor for this to work!
    /// </summary>
    /// <returns></returns>
    public static string GetUnknownArmorMastery()
    {
        List<string> availableMasteries = new List<string>();

        foreach (string str in MasteriesBakedData.allArmorMasteries)
        {
            if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(str))
            {
                availableMasteries.Add(str);
            }
        }

        if (availableMasteries.Count == 0) return "";
        return availableMasteries.GetRandomElement();
    }
}
