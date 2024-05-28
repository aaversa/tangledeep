using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class Ingredient
{
    public List<string> componentRef;
    public int numRequired;

    public Ingredient()
    {
        componentRef = new List<string>();
    }
}

public class Recipe
{
    public string refName;
    public string displayName;
    public string description;
    public string ingredientsDescription;
    public List<Ingredient> ingredients;
    public string itemCreated;

    public Recipe()
    {
        ingredients = new List<Ingredient>();
    }

    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name.ToLowerInvariant())
            {
                case "displayname":
                    displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "itemref":
                    itemCreated = reader.ReadElementContentAsString();
                    break;
                case "refname":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "description":
                    description = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "ingredientsdescription":
                    ingredientsDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "ingredient":
                    Ingredient ing = new Ingredient();
                    ingredients.Add(ing);
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "componentref":
                                ing.componentRef.Add(reader.ReadElementContentAsString());
                                //Debug.Log("Recipe " + rp.refName + " add ingredient component " + ing.componentRef);
                                break;
                            case "numrequired":
                                ing.numRequired = reader.ReadElementContentAsInt();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();        

        return true;
    }
}

public class CookingScript {

    // Define recipes here

    public static List<Recipe> masterRecipeList;
    public static bool initialized;

    public const int NUM_ALL_SEASONINGS = 6;

    public static void CancelPressed()
    {
        int lastIndexUsed = -1;
        for (int i = 0; i < UIManagerScript.cookingIngredientItems.Length; i++)
        {
            if (UIManagerScript.cookingIngredientItems[i] != null)
            {
                lastIndexUsed = i;
            }
        }

        if (UIManagerScript.cookingSeasoningItem != null)
        {
            UIManagerScript.cookingSeasoningItem = null;
        }
        else
        {
            if (lastIndexUsed != -1)
            {
                UIManagerScript.cookingIngredientItems[lastIndexUsed] = null;
            }
        }

        UIManagerScript.singletonUIMS.UpdatePanGraphics();
        UIManagerScript.UpdateCookingPlayerLists();
    }

    public static void Initialize()
    {
        if (!initialized)
        {
            masterRecipeList = new List<Recipe>();
            initialized = true;
        }        
    }

    public static Recipe FindRecipe(string refName)
    {
        foreach (Recipe r in masterRecipeList)
        {
            if (r.refName == refName)
            {
                return r;
            }
        }

        Debug.Log("Warning: Recipe " + refName + " does not exist.");
        return null;
    }

    public static Item EvaluateRecipes(List<Item> ingredients)
    {
        Dictionary<Item, int> dummy;
        Recipe backupRecipe = null;
        Recipe bestRecipe = null;
        foreach(Recipe recipe in masterRecipeList)
        {
            int totalIngredientsInRecipe = 0;
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                totalIngredientsInRecipe += recipe.ingredients[i].numRequired;
            }
            if (CanMakeRecipe(recipe,ingredients, false, out dummy))
            {
                if (ingredients.Count > totalIngredientsInRecipe)
                {
                    backupRecipe = recipe;
                    continue;
                }
                bestRecipe = recipe;
                break;
            }
        }
        if (backupRecipe != null && bestRecipe == null)
        {
            bestRecipe = backupRecipe;
        }
        if (bestRecipe != null)
        {
            MetaProgressScript.LearnRecipe(bestRecipe.refName);
            Item template = Item.GetItemTemplateFromRef(bestRecipe.itemCreated);
            Item created = new Consumable();
            created.CopyFromItem(template);
            created.SetUniqueIDAndAddToDict();
            return created;
        }

        return null;
    }


    public static Item MakeRecipeIfPossible(Recipe recipeToCreate)
    {
        Dictionary<Item, int> usedItems;
        InventoryScript playerInventory = GameMasterScript.heroPCActor.myInventory;
        if (CanMakeRecipeFromPlayerInventory(recipeToCreate, out usedItems))
        {
            // Remove the items from the player inventory, then create the new item.
            foreach(Item iKey in usedItems.Keys)
            {
                playerInventory.ChangeItemQuantityByRef(iKey.actorRefName, -1 * usedItems[iKey]);
                //Debug.Log("Removed " + usedItems[iKey] + " of " + iKey.actorRefName);
            }
            Item newlyCreatedRecipe = LootGeneratorScript.CreateItemFromTemplateRef(recipeToCreate.itemCreated, 1f, 0f, false);
            return newlyCreatedRecipe;
        }
        return null;
    }

    // Checks if a recipe can be made using ANY items in player's inventory, not just the ones selected by player.
    // Used by selecting a recipe from list
    public static bool CanMakeRecipeFromPlayerInventory(Recipe recipeToUse, out Dictionary<Item, int> itemsUsed)
    {
        itemsUsed = new Dictionary<Item, int>();

        bool debug = false;

        // To make a recipe, ALL Ingredients in the recipe must be used. An "Ingredient" is basically a requirement.        
        foreach (Ingredient i in recipeToUse.ingredients)
        {
            int numberOfThisComponentNeeded = i.numRequired;            

            // A COMPONENT is any item that can be used for this ingredient. Some recipes allow subtitutions.
            // The numRequired is ANY combination of components. For example: Requires 2 Cheese, Fish, or Meat
            // Means we could use 1 Cheese+1 Fish, or 2 Fish, or 2 Meat, etc.
            foreach(string checkComponent in i.componentRef)
            {
                if (debug) Debug.Log("Checking for component " + checkComponent + " of recipe " + recipeToUse.refName);

                Item itemBeingUsed = GameMasterScript.heroPCActor.myInventory.GetItemByRef(checkComponent);

                if (itemBeingUsed == null)
                {
                    if (debug) Debug.Log("Do not have that component.");
                    continue;
                }

                int playerQuantity = itemBeingUsed.GetQuantity();

                if (debug) Debug.Log("Have " + playerQuantity + " of that item.");

                if (itemsUsed.ContainsKey(itemBeingUsed))
                {
                    // This was used for another Ingredient. Therefore, we reduce the 'effective' quantity so we don't
                    // use the same item twice.
                    playerQuantity -= itemsUsed[itemBeingUsed];
                    if (debug) Debug.Log("Now we have " + playerQuantity + " after checking what was used already.");
                }

                if (playerQuantity == 0) continue;

                // Drill down and use as many of itemBeingUsed as needed for this Ingredient, but not exceeding what player has.

                int originalPlayerQuantity = playerQuantity;

                for (int x = 0; x < originalPlayerQuantity; x++)
                {
                    numberOfThisComponentNeeded--;
                    playerQuantity--;

                    // Track the item usage here. We must remember this when evaluating other Ingredients, so we 
                    // don't use the same player item twice for two different Ingredients.                    

                    CustomAlgorithms.AddIntToDictionary(itemsUsed, itemBeingUsed, 1);

                    if (debug) Debug.Log("Added 1 to dict, value is now " + itemsUsed[itemBeingUsed] + ". P quantity is " + playerQuantity + " num left is " + numberOfThisComponentNeeded);

                    //itemsUsed.Add(itemBeingUsed, 1);

                    if (playerQuantity == 0) break;
                    if (numberOfThisComponentNeeded == 0) break;
                }

                if (debug) Debug.Log("Remaining for component: " + numberOfThisComponentNeeded);

                if (numberOfThisComponentNeeded == 0) break;
            }
            if (numberOfThisComponentNeeded > 0)
            {
                if (debug) Debug.Log("Still need " + numberOfThisComponentNeeded);
                return false; // Don't have enough to make this recipe.
            }
        }

        return true;
    }

    public static bool CanMakeRecipe(Recipe recipeToUse, List<Item> playerProvidedIngredients, bool itemsMayBeStacked, out Dictionary<Item, int> itemsUsed)
    {
        itemsUsed = new Dictionary<Item, int>();       

        bool debug = false;

        if (debug) Debug.Log("Evaluate recipe " + recipeToUse.refName + " Num ingredients: " + recipeToUse.ingredients.Count);

        foreach (Ingredient i in recipeToUse.ingredients) // Ingredient: Double fruit.
        {
            if (debug)
            {
                Debug.Log("Check recipe required ingredient: " + i.componentRef[0]);
                Debug.Log("Current list: ");
                foreach (Item itam in itemsUsed.Keys)
                {
                    Debug.Log(itam.actorRefName + " is being used, qty " + itemsUsed[itam]);
                }
            }

            bool hasIngredient = false;
            int numComponents = 0;
            foreach (Item itm in playerProvidedIngredients)
            {
                if (debug) Debug.Log("Looking at possible component " + itm.actorRefName);
                Consumable c = itm as Consumable;
                int eQuantity = 1;

                if (itemsUsed.ContainsKey(itm))
                {
                    if (debug) Debug.Log("Already using one of these.");
                    eQuantity -= itemsUsed[itm];
                    if (eQuantity == 0) continue;
                }

                if (i.componentRef.Contains(itm.actorRefName)) // First Pass: Apple. Second pass: Banana.
                {
                    if (i.numRequired > 1) // We do need 2.
                    {
                        int countOfIngredientsInList = 0;
                        foreach(Item count in playerProvidedIngredients)
                        {
                            if (count.actorRefName == itm.actorRefName) // Only passed in one apple. Only passed in one banana.
                            {
                                countOfIngredientsInList++;
                            }
                        }
                        if (debug) Debug.Log("We do have " + itm.actorRefName + " " + countOfIngredientsInList);
                        numComponents += countOfIngredientsInList; // Count should be 2.

                        if (itemsUsed.ContainsKey(itm))
                        {
                            itemsUsed[itm]++;
                        }
                        else
                        {
                            itemsUsed.Add(itm, numComponents);
                        }
                        
                        if (numComponents >= i.numRequired) break;
                    }
                    else
                    {
                        if (debug) Debug.Log("We have " + itm.actorRefName);
                        numComponents++;

                        if (itemsUsed.ContainsKey(itm))
                        {
                            itemsUsed[itm]++;
                        }
                        else
                        {
                            itemsUsed.Add(itm, numComponents);
                        }

                        break;
                    }

                }
            }
            if (numComponents >= i.numRequired)
            {
                if (debug) Debug.Log("We have " + numComponents + " vs " + i.numRequired + " of " + i.componentRef[0]);
                hasIngredient = true;
            }
            if (!hasIngredient)
            {
                if (debug) Debug.Log("Cannot make this recipe: " + recipeToUse.refName);
                return false;
            }
        }
        if (debug) Debug.Log("We have everything!");
        return true;
    }

    public static Dictionary<Item, int> CheckRecipe(string refName, List<Item> ingredients)
    {
        //List<Item> playerInv = GameMasterScript.heroPCActor.myInventory.GetInventory();

        List<Item> playerInv = ingredients;

        Recipe recipeToUse = null;

        recipeToUse = FindRecipe(refName);

        if (recipeToUse == null) return null;

        //List<Item> itemsUsed = new List<Item>();
        Dictionary<Item, int> itemsUsed = new Dictionary<Item, int>();

        foreach(Ingredient i in recipeToUse.ingredients)
        {
            bool hasIngredient = false;
            int numComponents = 0;            
            foreach(Item itm in playerInv)
            {
                if (itemsUsed.ContainsKey(itm))
                {
                    continue;
                }

                if (i.componentRef.Contains(itm.actorRefName))
                {
                    int quantityTouse = 1;
                    if (itm.itemType == ItemTypes.CONSUMABLE)
                    {
                        Consumable c = itm as Consumable;                        
                        // Quantity we have: 2 legs of turkey
                        // Require: 4 legs
                        if (i.numRequired > c.Quantity)
                        {
                            quantityTouse = c.Quantity;
                            numComponents += quantityTouse;
                        }
                        else
                        {
                            quantityTouse = i.numRequired;
                            numComponents += i.numRequired;
                        }
                    }
                    else
                    {
                        numComponents++;
                    }
                    itemsUsed.Add(itm, quantityTouse);
                }
                if (numComponents >= i.numRequired)
                {
                    hasIngredient = true;
                    break;
                }
            }
            if (!hasIngredient)
            {
                return null;
            }
        }
        return itemsUsed;
    }

}
