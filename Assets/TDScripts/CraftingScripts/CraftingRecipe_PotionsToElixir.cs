using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe_PotionsToElixir : CraftingRecipe {

    List<string> requiredPotions = new List<string>()
    {
        "potion_healing3",
        "potion_stamina3",
        "potion_energy3"
    };
    
    public override bool IsRecipePossible(List<Item> inputItems)
    {
        Dictionary<string, int> potionsRemaining = new Dictionary<string, int>()
        {
            {  "potion_healing3", 2 },
            {  "potion_stamina3", 2 },
            {  "potion_energy3", 2 }
        };

        Dictionary<string, int> countOfPotions = new Dictionary<string, int>()
        {
            {  "potion_healing3", 0 },
            {  "potion_stamina3", 0 },
            {  "potion_energy3", 0 }
        };


        dictCachedItemsToUse.Clear();        

        foreach (Item itm in inputItems)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;

            if (!requiredPotions.Contains(itm.actorRefName)) continue;

            if (potionsRemaining[itm.actorRefName] == 0) continue;

            countOfPotions[itm.actorRefName] += itm.GetQuantity();

            if (itm.GetQuantity() <= potionsRemaining[itm.actorRefName])
            {
                dictCachedItemsToUse.Add(itm, itm.GetQuantity());
            }
            else
            {
                dictCachedItemsToUse.Add(itm, potionsRemaining[itm.actorRefName]);
            }

            potionsRemaining[itm.actorRefName] -= itm.GetQuantity();
        }


        foreach(string potion in potionsRemaining.Keys)
        {
            if (potionsRemaining[potion] > 0)
            {
                return false;
            }
        }

        return true;
    }

    public override List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        Consumable potion = LootGeneratorScript.CreateItemFromTemplateRef("potion_elixir", 1.0f, 0f, false) as Consumable;

        List<Item> returnItems = new List<Item>() { potion };
        unusedInputItems = RemoveUsedItemsFromList(inputItems);
        return returnItems;
    }
}
