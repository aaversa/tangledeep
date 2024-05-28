using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CraftingRecipe_GemLeavesToGems : CraftingRecipe {

    string refOfGemLeaf = "";
    
    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        int countOfGemLeaves = 0;
        int gemLeavesRemaining = numIngredients;
        refOfGemLeaf = "";

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;

            if (string.IsNullOrEmpty(refOfGemLeaf))
            {
                if (itm.actorRefName.Contains("cashcrop"))
                {
                    refOfGemLeaf = itm.actorRefName; // we'll use this gem for the recipe
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (itm.actorRefName != refOfGemLeaf) continue; // all gem must be the same
            }

            countOfGemLeaves += itm.GetQuantity();

            if (itm.GetQuantity() <= gemLeavesRemaining)
            {
                dictCachedItemsToUse.Add(itm, itm.GetQuantity());
            }
            else
            {
                dictCachedItemsToUse.Add(itm, gemLeavesRemaining);
            }

            gemLeavesRemaining -= itm.GetQuantity();

            if (countOfGemLeaves >= numIngredients)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        int gemLeafLevel = Int32.Parse(refOfGemLeaf.Substring(refOfGemLeaf.Length - 1, 1));
        string gemRef = "item_gem" + gemLeafLevel;

        Consumable gem = LootGeneratorScript.CreateItemFromTemplateRef(gemRef, 1.0f, 0f, false) as Consumable;

        List<Item> returnItems = new List<Item>() { gem };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
