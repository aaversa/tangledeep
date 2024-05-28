using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_LucidOrbAndGemToRandomLucid : CraftingRecipe {

    public Item gemToUse;
    public Item orbToUse;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        bool hasOrb = false;
        bool hasGem = false;

        foreach (Item itm in inputItems)
        {
            if (itm.IsLucidOrb())
            {
                if (hasOrb)
                {
                    if (itm.challengeValue > orbToUse.challengeValue)
                    {
                        orbToUse = itm;
                    }
                }
                else
                {
                    orbToUse = itm;
                    hasOrb = true;
                }
            }
            
            if (itm.CheckTag((int)ItemFilters.GEM))
            {
                if (hasGem)
                {
                    if (itm.challengeValue > gemToUse.challengeValue)
                    {
                        gemToUse = itm;
                    }
                }
                else
                {
                    gemToUse = itm;
                    hasGem = true;
                }
            }

            if (hasOrb && hasGem)
            {
                dictCachedItemsToUse.Add(orbToUse, 1);
                dictCachedItemsToUse.Add(gemToUse, 1);
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        float cv = gemToUse.challengeValue;
        Item newOrb = ItemWorldUIScript.CreateItemWorldOrb(cv, true, false);
        List<Item> returnItems = new List<Item>() { newOrb };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
