using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_ThreeSeedsToRandomSeed : CraftingRecipe {

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();

        int countOfSeeds = 0;
        int seedsRemaining = 3;

        foreach (Item itm in inputItems)
        {

            if (itm.actorRefName.Contains("seeds_tree"))
            {
                countOfSeeds += itm.GetQuantity();

                if (itm.GetQuantity() <= seedsRemaining)
                {
                    dictCachedItemsToUse.Add(itm, itm.GetQuantity());
                }
                else
                {
                    dictCachedItemsToUse.Add(itm, seedsRemaining);
                }

                seedsRemaining -= itm.GetQuantity();
            }

            if (seedsRemaining == 0 || countOfSeeds >= 3)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Consumable seedTemplate = GetPossibleItemRef("seeds", highestCVOfInputItems - 0.3f, highestCVOfInputItems) as Consumable;
        Item seed = LootGeneratorScript.CreateItemFromTemplateRef(seedTemplate.actorRefName, highestCVOfInputItems, 0f, true);
        LootGeneratorScript.MakeSeedsMagicalIfPossible(seed);
        List<Item> returnItems = new List<Item>() { seed };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
