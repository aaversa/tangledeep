using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Text;

public enum CraftingRecipeCategory { ORBS_OF_REVERIE, EQUIPMENT, CONSUMABLES, MISC, COUNT }

public class CraftingRecipe
{
    public string refName;
    public string displayName;
    public string displayIngredients;
    public int numIngredients;
    public Dictionary<Item, int> dictCachedItemsToUse;
    public List<string> itemRefsUsed;
    public float highestCVOfInputItems;
    public CraftingRecipeCategory myCategory;

    public void Initialize(string rName, string dIngredients, string dName, int ingr, CraftingRecipeCategory cat)
    {
        refName = rName;
        displayIngredients = dIngredients;
        displayName = dName;
        numIngredients = ingr;
        dictCachedItemsToUse = new Dictionary<Item, int>();
        itemRefsUsed = new List<string>();
        highestCVOfInputItems = 1.0f;
        myCategory = cat;
    }

    public virtual bool IsRecipePossible(List<Item> inputItems)
    {
        return false;
    }

    public virtual List<Item> MakeRecipe(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        unusedInputItems = null;
        return null;
    }

    // Returns *unused* items, which will be put back in player inventory.
    public virtual List<Item> RemoveUsedItemsFromList(List<Item> inputItems)
    {
        foreach (Item itm in dictCachedItemsToUse.Keys)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE)
            {
                inputItems.Remove(itm);
            }
            else
            {
                if (!itm.ChangeQuantity(-1 * dictCachedItemsToUse[itm]))
                {
                    inputItems.Remove(itm);
                }
            }
        }

        return inputItems;
    }

    public int GetCountOfItemRef(List<Item> inputItems, string refName)
    {
        int count = 0;
        foreach(Item itm in inputItems)
        {
            if (itm.actorRefName == refName)
            {
                count++;
            }
        }
        return count;
    }

    public virtual Item GetPossibleItemRef(string tableToUse, float minCV, float maxCV)
    {
        ActorTable itemTable = LootGeneratorScript.GetLootTable(tableToUse);
        string templateRef = itemTable.GetRandomActorRef();
        Item lookupTemplate = GameMasterScript.masterItemList[templateRef];
        while (lookupTemplate.challengeValue < minCV || lookupTemplate.challengeValue > maxCV || itemRefsUsed.Contains(templateRef) ||
            !lookupTemplate.ValidForPlayer())
        {
            templateRef = itemTable.GetRandomActorRef();
            lookupTemplate = GameMasterScript.masterItemList[templateRef];
        }

        return lookupTemplate;
    }

    public float GetAverageCVFromAllItemsUsed()
    {
        float totalCV = 0f;
        foreach(Item itm in dictCachedItemsToUse.Keys)
        {
            totalCV += itm.challengeValue;
        }
        return (totalCV / dictCachedItemsToUse.Keys.Count);
    }
}

public class CraftingRecipeManager
{
    public static List<CraftingRecipe> allRecipes;
    static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;

        allRecipes = new List<CraftingRecipe>();

        DefineRecipe(typeof(CraftingRecipe_LucidOrbsToRegular), 
            refName: "threelucidorbs_to_oneregular", 
            displayIngredients: StringManager.GetString("exp_crafting_threelucidorbs_to_oneregular"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.ORBS_OF_REVERIE);

        DefineRecipe(typeof(CraftingRecipe_LucidOrbAndGemToRandomLucid),
            refName: "lucidorbandgem_to_randomlucidorb",
            displayIngredients: StringManager.GetString("exp_crafting_lucidorbandgem_to_randomlucidorb"),
            numIngredients: 2,
            cat: CraftingRecipeCategory.ORBS_OF_REVERIE);

        DefineRecipe(typeof(CraftingRecipe_OrbAndJobScrollToSkillOrb),
            refName: "orbandjobscroll_to_skillorb",
            displayIngredients: StringManager.GetString("exp_crafting_orbandjobscroll_to_skillorb"),
            numIngredients: 2,
            cat: CraftingRecipeCategory.ORBS_OF_REVERIE);

        DefineRecipe(typeof(CraftingRecipe_LucidShardsToLucidOrb),
            refName: "sixlucidshards_to_randomlucidorb",
            displayIngredients: StringManager.GetString("exp_crafting_sixlucidshards_to_randomlucidorb"),
            numIngredients: 6,
            cat: CraftingRecipeCategory.ORBS_OF_REVERIE);

        DefineRecipe(typeof(CraftingRecipe_ThreeCommonWeaponsToRandomWeapon),
            refName: "threecommonweapons_to_randomweapon",
            displayIngredients: StringManager.GetString("exp_crafting_threecommonweapons_to_randomweapon"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_ThreeCommonArmorToRandomArmor),
            refName: "threecommonarmor_to_randomarmor",
            displayIngredients: StringManager.GetString("exp_crafting_threecommonarmor_to_randomarmor"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_ThreeOffhandsToOffhand),
            refName: "threecommonoffhand_to_randomoffhand",
            displayIngredients: StringManager.GetString("exp_crafting_threecommonoffhand_to_randomoffhand"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_TwoAccessoriesAndGemToAccessory),
            refName: "twoaccessoriesandgem_to_randomaccessory",
            displayIngredients: StringManager.GetString("exp_crafting_twoaccessoriesandgem_to_randomaccessory"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_BreakDownLegendaryToOrbs),
            refName: "legendaryandthreegems_to_orbs",
            displayIngredients: StringManager.GetString("exp_crafting_legendaryandthreegems_to_orbs"),
            numIngredients: 4,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_CommonEquipmentAndGemsToRandomGear),
            refName: "commongearandthreegems_to_randomgear",
            displayIngredients: StringManager.GetString("exp_crafting_commongearandthreegems_to_randomgear"),
            numIngredients: 4,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_TwoRelicsToRelic),
            refName: "tworelics_to_randomrelic",
            displayIngredients: StringManager.GetString("exp_crafting_tworelics_to_randomrelic"),
            numIngredients: 2,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_TwoRelicsAndItemToSpecificRelicType),
            refName: "tworelicsanditem_to_specificrelic",
            displayIngredients: StringManager.GetString("exp_crafting_tworelicsanditem_to_specificrelic"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.EQUIPMENT);

        DefineRecipe(typeof(CraftingRecipe_Upgrade3PotionsToHigherLevel),
            refName: "threepotions_upgrade",
            displayIngredients: StringManager.GetString("exp_crafting_threepotions_upgrade"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.CONSUMABLES);

        DefineRecipe(typeof(CraftingRecipe_PotionsToElixir),
            refName: "potions_to_elixir",
            displayIngredients: StringManager.GetString("exp_crafting_potions_to_elixir"),
            numIngredients: 6,
            cat: CraftingRecipeCategory.CONSUMABLES);

        DefineRecipe(typeof(CraftingRecipe_ThreeScrollsAndGemToScroll),
            refName: "threescrollsandgem_to_randomscroll",
            displayIngredients: StringManager.GetString("exp_crafting_threescrollsandgem_to_randomscroll"),
            numIngredients: 4,
            cat: CraftingRecipeCategory.CONSUMABLES);

        DefineRecipe(typeof(CraftingRecipe_UpgradeThreeGemsToHigherLevel),
            refName: "threegems_upgrade",
            displayIngredients: StringManager.GetString("exp_crafting_threegems_upgrade"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.MISC);

        DefineRecipe(typeof(CraftingRecipe_GemLeavesToGems),
            refName: "gemleaves_to_gems",
            displayIngredients: StringManager.GetString("exp_crafting_gemleaves_to_gems"),
            numIngredients: 2,
            cat: CraftingRecipeCategory.MISC);

        DefineRecipe(typeof(CraftingRecipe_ThreeSeedsToRandomSeed),
            refName: "threeseeds_to_randomseed",
            displayIngredients: StringManager.GetString("exp_crafting_threeseeds_to_randomseed"),
            numIngredients: 3,
            cat: CraftingRecipeCategory.MISC);


    }

    public static List<CraftingRecipe> GetCraftingRecipesByCategory(CraftingRecipeCategory cat)
    {
        if (!initialized)
        {
            Initialize();
        }
        List<CraftingRecipe> recipes = new List<CraftingRecipe>();
        foreach (CraftingRecipe cr in allRecipes)
        {
            if (cr.myCategory != cat) continue;
            recipes.Add(cr);
            
        }
        return recipes;
    }

    public static void DefineRecipe(Type recipeClass, string refName, string displayIngredients, int numIngredients, CraftingRecipeCategory cat, string displayName = "")
    {
        var recipe = Activator.CreateInstance(recipeClass);
        CraftingRecipe cr = recipe as CraftingRecipe;
        cr.Initialize(refName, displayIngredients, displayName, numIngredients, cat);
        allRecipes.Add(recipe as CraftingRecipe);
    }
    
    public static List<Item> FindAnyValidRecipeAndCreate(List<Item> inputItems, out List<Item> unusedInputItems)
    {
        CraftingRecipe backupRecipe = null;
        CraftingRecipe bestRecipe = null;
        List<Item> returnItems = new List<Item>();

        int numInputItems = 0;
        foreach(Item itm in inputItems)
        {
            numInputItems += itm.GetQuantity();
        }

        foreach (CraftingRecipe recipe in allRecipes)
        {
            int totalIngredientsInRecipe = recipe.numIngredients;
            if (recipe.IsRecipePossible(inputItems))
            {
                // Ideally, we want to use the recipe that uses *exactly* as many items as we put in.
                // But a backup will work too.
                if (numInputItems >= totalIngredientsInRecipe) 
                {
                    backupRecipe = recipe;
                    //Debug.Log("Backup recipe: " + backupRecipe.refName);
                }

                if (numInputItems == totalIngredientsInRecipe)
                {
                    bestRecipe = recipe;
                    //Debug.Log("Best recipe: " + bestRecipe.refName);
                    break;
                }
            }
        }
        if (backupRecipe != null && bestRecipe == null)
        {
            bestRecipe = backupRecipe;
        }
        if (bestRecipe != null)
        {
            returnItems = bestRecipe.MakeRecipe(inputItems, out unusedInputItems);
            return returnItems;
        }

        unusedInputItems = inputItems;
        return null;
    }
}