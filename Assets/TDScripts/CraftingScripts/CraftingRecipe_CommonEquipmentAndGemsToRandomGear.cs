using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class CraftingRecipe_CommonEquipmentAndGemsToRandomGear : CraftingRecipe {

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();

        int countOfGems = 0;
        int gemsRemaining = 3;
        string gemRef = "";
        Item equipmentToUse = null;

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
                    gemsRemaining -= itm.GetQuantity();
                }
            }

            if (itm.rarity == Rarity.COMMON && itm.IsEquipment())
            {
                if (equipmentToUse == null)
                {
                    equipmentToUse = itm;
                    highestCVOfInputItems = equipmentToUse.challengeValue;
                }
                else
                {
                    if (highestCVOfInputItems < equipmentToUse.challengeValue)
                    {
                        equipmentToUse = itm;
                        highestCVOfInputItems = equipmentToUse.challengeValue;
                    }
                }
            }
        }

        if (equipmentToUse != null && countOfGems == 3)
        {
            dictCachedItemsToUse.Add(equipmentToUse, 1);
            itemRefsUsed.Add(equipmentToUse.actorRefName);
            return true;
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        // We can make any piece of equipment OR POSSIBLY legendary!

        string tableToUse = "equipment";

        if (UnityEngine.Random.Range(0,1f) <= 0.05f)
        {
            tableToUse = "legendary";
        }
        float maxCVOfGearPossible = GetAverageCVFromAllItemsUsed();

        Item lookupTemplate = GetPossibleItemRef(tableToUse, highestCVOfInputItems - 0.2f, highestCVOfInputItems);

        var generatedItem = Activator.CreateInstance(lookupTemplate.GetType()) as Item;
        generatedItem.CopyFromItem(lookupTemplate);
        generatedItem.SetUniqueIDAndAddToDict();

        List<Item> returnItems = new List<Item>() { generatedItem };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;

    }
}
