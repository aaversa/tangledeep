using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModifierData
{
    public GameModifiers mod;
    public string modifierDescription;
    public bool enableAchievements;

    public GameModifierData(GameModifiers whichMod, bool chievos)
    {
        mod = whichMod;
        enableAchievements = chievos;
    }

    public string GetModifierDescription()
    {
        if (string.IsNullOrEmpty(modifierDescription))
        {
            modifierDescription = StringManager.GetString("gamemods_" + mod.ToString().ToLowerInvariant());
        }
        return modifierDescription;
    }
}