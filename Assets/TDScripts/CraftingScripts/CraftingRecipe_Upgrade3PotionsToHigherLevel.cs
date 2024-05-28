using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CraftingRecipe_Upgrade3PotionsToHigherLevel : CraftingRecipe {

    string refOfPotion = "";

    static readonly List<string> validItemsForRecipe = new List<string>()
    {
        "potion_healing1",
        "potion_healing2",
        "potion_stamina1",
        "potion_stamina2",
        "potion_energy1",
        "potion_energy2",
        "potion_stealth1",
        "bomb_fire1",
        "bomb_fire2",
        "bomb_lightning1",
        "bomb_lightning2",
        "bomb_ice",
    };

    public override bool IsRecipePossible(List<Item> inputItems)
    {
        dictCachedItemsToUse.Clear();
        int countOfPotions = 0;
        int potionsRemaining = numIngredients;
        refOfPotion = "";

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;

            if (string.IsNullOrEmpty(refOfPotion))
            {
                if (validItemsForRecipe.Contains(itm.actorRefName))
                {
                    refOfPotion = itm.actorRefName; // we'll use this potion for the recipe
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (itm.actorRefName != refOfPotion) continue; // all potions must be the same
            }

            countOfPotions += itm.GetQuantity();

            if (itm.GetQuantity() <= potionsRemaining)
            {
                dictCachedItemsToUse.Add(itm, itm.GetQuantity());
            }
            else
            {
                dictCachedItemsToUse.Add(itm, potionsRemaining); 
            }

            potionsRemaining -= itm.GetQuantity();

            if (countOfPotions >= numIngredients)
            {
                return true;
            }
        }

        return false;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        int potionLevel = 0;
        string potionRef = "";
        if (refOfPotion == "bomb_ice")
        {
            potionRef = "bomb_ice2";
        }
        else
        {
            potionLevel = Int32.Parse(refOfPotion.Substring(refOfPotion.Length - 1, 1));
            potionLevel++;
            potionRef = refOfPotion.Substring(0, refOfPotion.Length - 1);
            potionRef += potionLevel.ToString();
        }                
        
        Consumable potion = LootGeneratorScript.CreateItemFromTemplateRef(potionRef, 1.0f, 0f, false) as Consumable;

        List<Item> returnItems = new List<Item>() { potion };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
