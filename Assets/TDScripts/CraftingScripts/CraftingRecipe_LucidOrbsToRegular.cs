using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Takes ANY three Lucid Orbs and spits out a regular Orb of Reverie.

public class CraftingRecipe_LucidOrbsToRegular : CraftingRecipe
{
    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();

        int countOfLucidOrbs = 0;
        int orbsRemainining = numIngredients;

        // Let's say we input:
        // Orb A qty = 2
        // Orb B qty = 2
        // Orb C qty = 1

        foreach(Item itm in inputItems)
        {
            if (!itm.IsLucidOrb()) continue;
            countOfLucidOrbs += itm.GetQuantity();

            if (itm.GetQuantity() <= orbsRemainining)
            {
                dictCachedItemsToUse.Add(itm, itm.GetQuantity());
            }
            else
            {
                dictCachedItemsToUse.Add(itm, orbsRemainining); // Don't ever use more than 3 orbs!
            }

            orbsRemainining -= itm.GetQuantity();

            if (countOfLucidOrbs >= numIngredients)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Item newOrb = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 1.0f, 0f, false);
        List<Item> returnItems = new List<Item>() { newOrb };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
