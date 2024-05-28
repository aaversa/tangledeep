using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CraftingRecipe_TwoRelicsToRelic : CraftingRecipe
{

    float totalCVOfRelics = 0f;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();
        highestCVOfInputItems = 1.0f;
        totalCVOfRelics = 0f;

        foreach (Item itm in inputItems)
        {
            if (!itm.customItemFromGenerator) continue;

            dictCachedItemsToUse.Add(itm, 1);
            highestCVOfInputItems = itm.challengeValue;
            totalCVOfRelics += itm.challengeValue;

            if (dictCachedItemsToUse.Values.Count == 2) return true;
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        float targetCV = (totalCVOfRelics / 2f);
        
        int targetRank = BalanceData.ConvertChallengeValueToRank(targetCV);

        if (UnityEngine.Random.Range(0, 2) == 0 && targetRank < 10)
        {
            targetCV += 0.05f;
        }


        Item copiedRelic = null;

        try
        {
            Item legRelic = LegendaryMaker.CreateNewLegendaryItem(UnityEngine.Random.Range(targetCV, targetCV));
            copiedRelic = LootGeneratorScript.CreateItemFromTemplateRef(legRelic.actorRefName, targetCV, 0f, false, true);
        }
        catch(Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("Failed making random relic: " + e);
            copiedRelic = LootGeneratorScript.GenerateLoot(2f, 5f);
        }        

        List<Item> returnItems = new List<Item>() { copiedRelic };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
