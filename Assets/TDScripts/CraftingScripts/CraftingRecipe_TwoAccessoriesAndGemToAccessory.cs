using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_TwoAccessoriesAndGemToAccessory : CraftingRecipe {

    Item gemToUse;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();
        int countOfAccessories = 0;
        highestCVOfInputItems = 1.0f;

        foreach (Item itm in inputItems)
        {
            if (itm.CheckTag((int)ItemFilters.GEM))
            {
                if (gemToUse == null)
                {
                    gemToUse = itm;
                }
                else
                {
                    if (gemToUse.challengeValue < itm.challengeValue)
                    {
                        gemToUse = itm;
                    }
                }
            }

            else if (itm.itemType == ItemTypes.ACCESSORY)
            {
                Accessory acc = itm as Accessory;
                if (acc.rarity != Rarity.COMMON) continue;
                countOfAccessories++;
                dictCachedItemsToUse.Add(itm, 1);
                itemRefsUsed.Add(itm.actorRefName);
                if (itm.challengeValue > highestCVOfInputItems)
                {
                    highestCVOfInputItems = itm.challengeValue;
                }
            }

            if (countOfAccessories == 2 && gemToUse != null)
            {
                dictCachedItemsToUse.Add(gemToUse, 1);
                return true;
            }

        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        highestCVOfInputItems = (highestCVOfInputItems + gemToUse.challengeValue) / 2f;
        Accessory lookupTemplate = GetPossibleItemRef("accessories", highestCVOfInputItems - 0.2f, highestCVOfInputItems) as Accessory;

        Accessory accToMake = new Accessory();
        accToMake.CopyFromItem(lookupTemplate);
        accToMake.SetUniqueIDAndAddToDict();
        List<Item> returnItems = new List<Item>() { accToMake };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
