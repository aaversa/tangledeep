using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_ThreeScrollsAndGemToScroll : CraftingRecipe {

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();

        int countOfScrolls = 0;
        int scrollsRemaining = 3;
        Item gemUsed = null;
        
        foreach (Item itm in inputItems)
        {
            if (itm.CheckTag(ItemFilters.GEM))
            {
                if (gemUsed != null)
                {
                    if (itm.challengeValue > highestCVOfInputItems)
                    {
                        gemUsed = itm;
                        highestCVOfInputItems = itm.challengeValue;
                    }
                }
                else
                {
                    gemUsed = itm;
                }
            }

            if (itm.actorRefName.Contains("scroll_"))
            {
                countOfScrolls += itm.GetQuantity();

                if (itm.GetQuantity() <= scrollsRemaining)
                {
                    dictCachedItemsToUse.Add(itm, itm.GetQuantity());
                }
                else
                {
                    dictCachedItemsToUse.Add(itm, scrollsRemaining); 
                }

                scrollsRemaining -= itm.GetQuantity();
            }

            if (countOfScrolls == 3 && gemUsed != null)
            {
                dictCachedItemsToUse.Add(gemUsed, 1);
                return true;
            }

        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Consumable scrollTemplate = GetPossibleItemRef("scrolls", highestCVOfInputItems - 0.3f, highestCVOfInputItems) as Consumable;
        Consumable scrollToMake = new Consumable();
        scrollToMake.CopyFromItem(scrollTemplate);
        scrollToMake.SetUniqueIDAndAddToDict();

        List<Item> returnItems = new List<Item>() { scrollToMake };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
