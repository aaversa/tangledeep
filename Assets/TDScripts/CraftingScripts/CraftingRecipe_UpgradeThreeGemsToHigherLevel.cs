using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CraftingRecipe_UpgradeThreeGemsToHigherLevel : CraftingRecipe {

    string refOfGem = "";

    List<string> validItemsForRecipe = new List<string>();

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        if (validItemsForRecipe.Count == 0)
        {
            for (int i = 0; i < 9; i++)
            {
                validItemsForRecipe.Add("item_gem" + (i + 1));
            }
        }


        dictCachedItemsToUse.Clear();
        int countOfGems = 0;
        int gemsRemaining = numIngredients;
        refOfGem = "";

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;

            if (itm.actorRefName == "item_gem9") continue;

            if (string.IsNullOrEmpty(refOfGem))
            {
                if (validItemsForRecipe.Contains(itm.actorRefName))
                {
                    refOfGem = itm.actorRefName; // we'll use this gem for the recipe
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (itm.actorRefName != refOfGem) continue; // all gems must be the same
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

            gemsRemaining -= itm.GetQuantity();

            if (countOfGems >= numIngredients)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        int gemLevel = Int32.Parse(refOfGem.Substring(refOfGem.Length - 1, 1));
        gemLevel++;
        string gemRef = refOfGem.Substring(0, refOfGem.Length - 1);
        gemRef += gemLevel.ToString();

        Consumable gem = LootGeneratorScript.CreateItemFromTemplateRef(gemRef, 1.0f, 0f, false) as Consumable;

        List<Item> returnItems = new List<Item>() { gem };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
