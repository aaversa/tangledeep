using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BakedStatusEffectDefinitions
{
    // These status definitions are very simple and we don't need to take up XML space with them.
    // This function runs after the main LoadAllStatuses call runs.

    static readonly string[] permaBuffNoRunStatusesDLC1 = new string[]
    {
        "exp_status_improveddominate",
        "exp_status_necessarysacrifice",        
        "dualwielderbonus2",
        "dualwielderbonus3",
        "emblem_dualwielderemblem_tier0_biography",
        "emblem_dualwielderemblem_tier1_glide",
        "relichunter",
        "menagerie",
        "treasuretracker",
        "scholar"
    };

    static readonly string[] permaBuffNoRunStatusMultiDLC = new string[]
    {
        "dualwielderbonus1"
    };

    public static void AddAllBakedStatusDefinitions_DLC1()
    {
        //if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            foreach (string abilRef in permaBuffNoRunStatusesDLC1)
            {
                SetupPermaBuffNoRunStatus(abilRef);
            }
        }
    }

}
