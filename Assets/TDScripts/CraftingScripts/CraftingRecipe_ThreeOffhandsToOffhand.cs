using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_ThreeOffhandsToOffhand : CraftingRecipe {

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();
        int countOfOffhands = 0;
        highestCVOfInputItems = 1.0f;

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.OFFHAND) continue;
            Offhand oh = itm as Offhand;
            if (oh.rarity != Rarity.COMMON) continue;
            countOfOffhands++;
            dictCachedItemsToUse.Add(itm, 1);
            itemRefsUsed.Add(itm.actorRefName);
            if (itm.challengeValue > highestCVOfInputItems)
            {
                highestCVOfInputItems = itm.challengeValue;
            }
            if (countOfOffhands == numIngredients)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Offhand lookupTemplate = GetPossibleItemRef("offhands", highestCVOfInputItems - 0.2f, highestCVOfInputItems) as Offhand;

        Offhand offhandToMake = new Offhand();
        offhandToMake.CopyFromItem(lookupTemplate);
        offhandToMake.SetUniqueIDAndAddToDict();
        List<Item> returnItems = new List<Item>() { offhandToMake };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
