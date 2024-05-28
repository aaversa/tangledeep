using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_OrbAndJobScrollToSkillOrb : CraftingRecipe {

    public Item jobScroll;
    public Item orbToUse;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        bool hasScroll = false;
        bool hasOrb = false;

        foreach (Item itm in inputItems)
        {
            if (!hasOrb && !itm.IsLucidOrb() && !itm.IsJobSkillOrb() && itm.actorRefName == "orb_itemworld")
            {
                orbToUse = itm;
                hasOrb = true;
            }

            if (itm.actorRefName == "scroll_jobchange")
            {
                jobScroll = itm;
                hasScroll = true;
            }

            if (hasOrb && hasScroll)
            {
                dictCachedItemsToUse.Add(jobScroll, 1);
                dictCachedItemsToUse.Add(orbToUse, 1);
                return true;
            }
        }
        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Item newOrb = ItemWorldUIScript.CreateItemWorldOrb(1.6f, false, true);
        List<Item> returnItems = new List<Item>() { newOrb };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
