using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_ThreeCommonWeaponsToRandomWeapon : CraftingRecipe {

    List<WeaponTypes> wTypes;

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        itemRefsUsed.Clear();     
        int countOfWeapons = 0;
        highestCVOfInputItems = 1.0f;
        wTypes = new List<WeaponTypes>();

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.WEAPON) continue;
            Weapon w = itm as Weapon;
            if (w.rarity != Rarity.COMMON) continue;
            countOfWeapons++;
            dictCachedItemsToUse.Add(itm, 1);
            itemRefsUsed.Add(itm.actorRefName);
            if (itm.challengeValue > highestCVOfInputItems)
            {
                highestCVOfInputItems = itm.challengeValue;
            }
            if (!wTypes.Contains(w.weaponType))
            {
                wTypes.Add(w.weaponType);
            }
            if (countOfWeapons == 3)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Weapon lookupTemplate = GetPossibleItemRef("weapons", highestCVOfInputItems - 0.2f, highestCVOfInputItems) as Weapon;
        while (wTypes.Contains(lookupTemplate.weaponType))
        {
            lookupTemplate = GetPossibleItemRef("weapons", highestCVOfInputItems - 0.2f, highestCVOfInputItems) as Weapon;
        }
        
        Weapon weapToMake = new Weapon();
        weapToMake.CopyFromItem(lookupTemplate);
        weapToMake.SetUniqueIDAndAddToDict();
        List<Item> returnItems = new List<Item>() { weapToMake };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
