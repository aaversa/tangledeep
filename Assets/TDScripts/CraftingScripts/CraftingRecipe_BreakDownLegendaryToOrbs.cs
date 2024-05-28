using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_BreakDownLegendaryToOrbs : CraftingRecipe {

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();

        int countOfGems = 0;
        int gemsRemaining = 3;
        string gemRef = "";
        Item legendaryToUse = null;

        foreach (Item itm in inputItems)
        {
            if (itm.CheckTag((int)ItemFilters.GEM))
            {
                if (gemsRemaining > 0)
                {
                    if (gemRef != "" && itm.actorRefName != gemRef) // must be same gem used.
                    {
                        continue;
                    }
                    countOfGems += itm.GetQuantity();
                    if (itm.GetQuantity() <= gemsRemaining)
                    {
                        dictCachedItemsToUse.Add(itm, itm.GetQuantity());
                    }
                    else
                    {
                        dictCachedItemsToUse.Add(itm, gemsRemaining);
                    }
                    gemsRemaining -= countOfGems;
                }
            }  
            
            if (itm.rarity == Rarity.LEGENDARY || itm.rarity == Rarity.GEARSET)
            {
                if (legendaryToUse == null)
                {
                    legendaryToUse = itm;
                    highestCVOfInputItems = legendaryToUse.challengeValue;
                }
                else
                {
                    if (highestCVOfInputItems < legendaryToUse.challengeValue)
                    {
                        legendaryToUse = itm;
                        highestCVOfInputItems = legendaryToUse.challengeValue;
                    }
                }
            }
        }

        if (legendaryToUse != null)
        {
            dictCachedItemsToUse.Add(legendaryToUse, 1);
            return true;
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        List<Item> returnItems = new List<Item>();

        for (int i = 0; i < 5; i++)
        {
            Item newOrb = ItemWorldUIScript.CreateItemWorldOrb(highestCVOfInputItems, false, false);
            returnItems.Add(newOrb);
        }
                
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
