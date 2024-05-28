using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_ThreeCommonArmorToRandomArmor : CraftingRecipe {

    List<ArmorTypes> armorTypes;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();
        int countOfArmor = 0;
        highestCVOfInputItems = 1.0f;
        armorTypes = new List<ArmorTypes>();

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.ARMOR) continue;
            Armor arm = itm as Armor;
            if (arm.rarity != Rarity.COMMON) continue;
            countOfArmor++;
            dictCachedItemsToUse.Add(itm, 1);
            itemRefsUsed.Add(itm.actorRefName);
            if (itm.challengeValue > highestCVOfInputItems)
            {
                highestCVOfInputItems = itm.challengeValue;
            }
            if (!armorTypes.Contains(arm.armorType))
            {
                armorTypes.Add(arm.armorType);
            }
            if (countOfArmor == 3)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Armor lookupTemplate = GetPossibleItemRef("bodyarmor", highestCVOfInputItems - 0.2f, highestCVOfInputItems) as Armor;

        Armor armorToMake = new Armor();
        armorToMake.CopyFromItem(lookupTemplate);
        armorToMake.SetUniqueIDAndAddToDict();
        List<Item> returnItems = new List<Item>() { armorToMake };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
