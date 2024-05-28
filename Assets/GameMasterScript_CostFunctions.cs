using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript
{
    public static int GetJobChangeCost()
    {
        int basePrice = heroPCActor.myStats.GetLevel() * 100;
        int finalPrice = basePrice * (heroPCActor.numberOfJobChanges + 1);
        if (finalPrice >= MAX_JOBCHANGE_COST)
        {
            finalPrice = MAX_JOBCHANGE_COST;
        }

        if (GameStartData.CheckGameModifier(GameModifiers.FREE_JOBCHANGE))
        {
            finalPrice = 0;
        }

        return finalPrice;
    }

    public static int GetBlessingCost()
    {
        int basePrice = heroPCActor.myStats.GetLevel() * 50;
        return basePrice;
    }

    public static int GetHealerCostJP()
    {
        int calc = 100 + (heroPCActor.timesHealedThisLevel * 25);
        if (calc > 500) calc = 500;
        return calc;
    }

    public static int GetHealerCost()
    {
        int minCost = 50;
        if (heroPCActor.myStats.GetLevel() >= 15) minCost = 400;
        else if (heroPCActor.myStats.GetLevel() >= 12) minCost = 300;
        else if (heroPCActor.myStats.GetLevel() >= 8) minCost = 200;
        else if (heroPCActor.myStats.GetLevel() >= 5) minCost = 100;

        if (heroPCActor.timesHealedThisLevel == 0)
        {
            return minCost;
        }
        else
        {
            int modLevel = GameMasterScript.heroPCActor.myStats.GetLevel() - 1;
            int calc = minCost + (heroPCActor.timesHealedThisLevel * (50 + modLevel));
            if (calc > 9999)
            {
                calc = 9999;
            }
            return calc;
        }
    }

    public static int GetPowerupLength()
    {
        int baseLen = 12;
        if (heroPCActor.myStats.CheckHasStatusName("dropsoul2"))
        {
            baseLen *= 2;
        }
        return baseLen;
    }

    public static float GetPandoraMonsterDefenseUpValue()
    {
        return PANDORA_MONSTER_DEFENSE_UP;
    }

    public static float GetPandoraMonsterDefenseCapValue()
    {
        return PANDORA_MONSTER_DEFENSE_CAP;
    }

    public static float GetPandoraMonsterDamageUpValue()
    {
        return PANDORA_MONSTER_DAMAGE_UP;
    }

    public static float GetChallengeModToPlayer(float cv)
    {
        // First, get the 'converted' expected player level based on CV.

        int compareLevel = 0;

        foreach (int checkKey in BalanceData.DICT_LEVEL_TO_CV.Keys)
        {
            float checkCV = BalanceData.DICT_LEVEL_TO_CV[checkKey];
            if (CustomAlgorithms.CompareFloats(cv, checkCV))
            {
                compareLevel = checkKey;
                //Debug.Log(compareLevel + " is comparable to " + cv + " " + checkCV);
                break;
            }
            else
            {
                if (checkCV >= cv)
                {
                    compareLevel = checkKey; // well, it's close
                                             //Debug.Log("Well, " + checkCV + " is higher than " + cv + " so use lvl " + compareLevel);
                    break;
                }
            }
        }

        if (cv > 2.15f)
        {
            compareLevel = 20;
        }

        if (compareLevel == 0)
        {
            compareLevel = heroPCActor.myStats.GetLevel();
            //Debug.Log("Warning: No comparison for " + cv);
        }
        else
        {
            compareLevel = heroPCActor.myStats.GetLevel() - compareLevel;
        }

        // Now pull the xp mod value from the monster table I made just for this.
        // X = Current player level, Y = difference.

        // compareLevel is actually the level DIFFERENCE!!!!!

        if (compareLevel < 0) compareLevel = 0;

        try
        {
            float modChallengeValue = BalanceData.playerMonsterRewardTable[heroPCActor.myStats.GetLevel(), compareLevel];
            //Debug.Log("Comparing " + heroPCActor.myStats.GetLevel() + " to " + compareLevel + " yields " + modChallengeValue);
            return modChallengeValue;
        }
        catch (Exception e)
        {
            Debug.Log("Problem comparing herolvl " + heroPCActor.myStats.GetLevel() + " vs " + compareLevel + " search " + cv + ": " + e);
            return 1.0f;
        }

    }
}