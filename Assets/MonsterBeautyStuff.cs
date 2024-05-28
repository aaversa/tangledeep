using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterBeautyStuff 
{
    public const int UGLIEST_MIN_RANGE = 0;
    public const int UGLIEST_MAX_RANGE = 14;
    public const int SEMIUGLY_MIN_RANGE = 15;
    public const int SEMIUGLY_MAX_RANGE = 35;
    public const int AVERAGE_MIN_RANGE = 36;
    public const int AVERAGE_MAX_RANGE = 64;
    public const int SEMIBEAUTY_MIN_RANGE = 65;
    public const int SEMIBEAUTY_MAX_RANGE = 85;
    public const int BEAUTIEST_MIN_RANGE = 86;
    public const int BEAUTIEST_MAX_RANGE = 100;

    public static List<string> beautyStatusEffects = new List<string>()
    {
        "petugly_fear",
        "petugly_infuriate",
        "",
        "petbeauty_blind",
        "petbeauty_charm"
    };

    public static string GetBeautyEffectDescription(string beautyEffect, string monsterName)
    {
        switch (beautyEffect)
        {
            case "petugly_fear":
                StringManager.SetTag(0, "25" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                StringManager.SetTag(1, monsterName);
                return StringManager.GetString("pet_beauty0_effect");
            case "petugly_infuriate":
                StringManager.SetTag(0, "100" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                StringManager.SetTag(1, monsterName);
                return StringManager.GetString("pet_beauty1_effect");
            case "petbeauty_blind":
                StringManager.SetTag(0, "20" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                StringManager.SetTag(1, monsterName);
                return StringManager.GetString("pet_beauty2_effect");
            case "petbeauty_charm":
                StringManager.SetTag(0, "10" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                StringManager.SetTag(1, monsterName);
                return StringManager.GetString("pet_beauty3_effect");
        }

        return "";
    }

    public static void ProcessPetToAddAppropriateBeautyEffects(TamedCorralMonster tcm)
    {
        string effectToUse = GetEffectToUseFromBeauty(tcm);

        // Remove unused effects
        foreach(string effect in beautyStatusEffects)
        {
            if (effect == effectToUse) continue;
            
            tcm.monsterObject.myStats.RemoveAllStatusByRef(effect);
        }

        // No new effect? ok
        if (string.IsNullOrEmpty(effectToUse)) return;

        tcm.monsterObject.myStats.AddStatusByRef(effectToUse, tcm.monsterObject, 99, false);
    }

    public static string GetEffectToUseFromBeauty(TamedCorralMonster tcm)
    {
        if (tcm.beauty <= UGLIEST_MAX_RANGE)
        {
            return beautyStatusEffects[0];
        }
        else if (tcm.beauty >= SEMIUGLY_MIN_RANGE && tcm.beauty <= SEMIUGLY_MAX_RANGE)
        {
            return beautyStatusEffects[1];
        }
        else if (tcm.beauty >= AVERAGE_MIN_RANGE && tcm.beauty <= AVERAGE_MAX_RANGE)
        {
            return beautyStatusEffects[2];
        }
        else if (tcm.beauty >= SEMIBEAUTY_MIN_RANGE && tcm.beauty <= SEMIBEAUTY_MAX_RANGE)
        {
            return beautyStatusEffects[3];
        }
        else if (tcm.beauty >= BEAUTIEST_MIN_RANGE && tcm.beauty <= BEAUTIEST_MAX_RANGE)
        {
            return beautyStatusEffects[4];
        }
        return "";
    }
}
