using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_LucidShardsToLucidOrb : CraftingRecipe {

    float maxCV = 1.0f;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();

        int countOfShards = 0;
        int shardsRemaining = 6;

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (itm.actorRefName != "item_lucidorb_shard") continue;
            countOfShards += itm.GetQuantity();

            if (itm.challengeValue > maxCV)
            {
                maxCV = itm.challengeValue;
            }

            if (itm.GetQuantity() <= shardsRemaining)
            {
                dictCachedItemsToUse.Add(itm, itm.GetQuantity());
            }
            else
            {
                dictCachedItemsToUse.Add(itm, shardsRemaining); // Don't ever use more than 6 shards!
            }

            shardsRemaining -= itm.GetQuantity();

            if (countOfShards >= 6)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Item newOrb = ItemWorldUIScript.CreateItemWorldOrb(maxCV + 0.1f, true, false);
        List<Item> returnItems = new List<Item>() { newOrb };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
