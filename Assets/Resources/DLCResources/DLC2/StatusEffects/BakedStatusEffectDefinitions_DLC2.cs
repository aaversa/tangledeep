using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BakedStatusEffectDefinitions
{
    static readonly string[] permaBuffNoRunStatusesDLC2 = new string[]
    {
        "whipmastery3",
        "xp2_echoingattack",
        "xp2_absorbingshield",
        //"xp2_zookeeper",
        "xp2_ricochet",
        "xp2_legends",
        "xp2_grandmaster",
        "xp2_dragons",
        "xp2_hydrating",
        "xp2_battlemage"
    };

    static readonly HashSet<string> dlc2statusesThatShouldStack = new HashSet<string>
    {
        "xp2_dragons",
        "xp2_hydrating"
    };

    public static void AddAllBakedStatusDefinitions_DLC2()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            foreach (string abilRef in permaBuffNoRunStatusesDLC2)
            {
                StatusEffect se = SetupPermaBuffNoRunStatus(abilRef);
                if (dlc2statusesThatShouldStack.Contains(abilRef))
                {
                    se.stackMultipleEffects = true;
                    se.stackMultipleDurations = false;
                }
            }
        }
    }
}
