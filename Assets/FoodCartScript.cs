using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class FoodCartScript : MonoBehaviour {

    public const int FOOD_PER_PACKAGE = 10;

    static List<string> foodDemanded;

    static float valueMultOfDemand = 1.0f;
    static int quantityDemanded;

    public static int goldInvestedInAdvertising;
    public static int dayOfLastGoldInvestment;
    public static int dayOfLastDemandUpdate;
    public static int dayOfLastSaleUpdate;

    const float CHANCE_UPDATE_DEMANDS_PER_DAY = 0.25f;
    const float CHANCE_SELLITEMS_PER_DAY = 1.0f;

    const float BASE_VALUE_MULT = 2.5f;

    static NPC cachedCartNPC;

    static int lastDayChecked;

    static Dictionary<Item, int> itemsToSell;

    public static void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("foodcart");

        writer.WriteElementString("lastdemandupdate", dayOfLastDemandUpdate.ToString());
        writer.WriteElementString("lastsaleupdate", dayOfLastSaleUpdate.ToString());
        writer.WriteElementString("lastdaychecked", lastDayChecked.ToString());
        writer.WriteElementString("lastadv", dayOfLastGoldInvestment.ToString());
        writer.WriteElementString("valuemult", valueMultOfDemand.ToString());
        writer.WriteElementString("qtydemanded", quantityDemanded.ToString());
        writer.WriteElementString("goldinvested", goldInvestedInAdvertising.ToString());
        string foodListBuilder = "";
        for (int i = 0; i < foodDemanded.Count; i++)
        {
            foodListBuilder += foodDemanded[i];
            if (i < foodDemanded.Count-1)
            {
                foodListBuilder += ",";
            }
        }

        if (!string.IsNullOrEmpty(foodListBuilder))
        {
            writer.WriteElementString("demands", foodListBuilder);
        }

        writer.WriteEndElement();
    }

    public static void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name)
            {
                case "lastdemandupdate":
                    dayOfLastDemandUpdate = reader.ReadElementContentAsInt();
                    break;
                case "lastsaleupdate":
                    dayOfLastSaleUpdate = reader.ReadElementContentAsInt();
                    break;
                case "lastdaychecked":
                    lastDayChecked = reader.ReadElementContentAsInt();
                    break;
                case "lastadv":
                    dayOfLastGoldInvestment = reader.ReadElementContentAsInt();
                    break;
                case "valuemult":
                    string unparsed = reader.ReadElementContentAsString();
                    valueMultOfDemand = CustomAlgorithms.TryParseFloat(unparsed);
                    break;
                case "qtydemanded":
                    quantityDemanded = reader.ReadElementContentAsInt();
                    break;
                case "goldinvested":
                    goldInvestedInAdvertising = reader.ReadElementContentAsInt();
                    break;
                case "demands":
                    foodDemanded.Clear();
                    string foods = reader.ReadElementContentAsString();
                    string[] parsedFoods = foods.Split(',');
                    for (int i = 0; i < parsedFoods.Length; i++)
                    {
                        foodDemanded.Add(parsedFoods[i]);
                    }
                    break;                
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();
    }

    public static bool HasDemand(string itemRef)
    {
        return foodDemanded.Contains(itemRef);
    }

    public static void UpdateFoodCartDemands()
    {
        UpdateFoodDemands();

        valueMultOfDemand = UnityEngine.Random.Range(3.0f, 4.4f);

        dayOfLastDemandUpdate = MetaProgressScript.totalDaysPassed;
    }

    public static int UpdateFoodCartSales(out int quantity)
    {
        // Sell items Now Right Now
        dayOfLastSaleUpdate = MetaProgressScript.totalDaysPassed;

        Actor findFoodCart = cachedCartNPC;

        if (cachedCartNPC == null)
        {
            findFoodCart = MapMasterScript.activeMap.FindActor("npc_foodcart");
            cachedCartNPC = findFoodCart as NPC;            
        }

        //Debug.Log("Cart npc is " + findFoodCart.actorUniqueID);

        quantity = 0;
        

        int totalGoldHaul = 0;


        if (findFoodCart != null)
        {
            NPC cart = findFoodCart as NPC;
            int numItems = cart.myInventory.GetActualInventoryCount();

            float percentToSell = UnityEngine.Random.Range(0.45f, 0.75f);
            int numToSell = (int)(percentToSell * (float)numItems);
            if (numToSell < 1)
            {
                numToSell = 1;
            }

            //Debug.Log("There are " + numItems + " in cart's inventory, we want to sell " + numToSell);

            int origNumToSell = numToSell;
            
            itemsToSell.Clear();

            foreach(Item itm in cart.myInventory.GetInventory())
            {
                //Debug.Log("Examine pop? " + itm.actorRefName);
                if (foodDemanded.Contains(itm.actorRefName))
                {
                    int qty = itm.GetQuantity();
                    if (numToSell > qty)
                    {
                        itemsToSell.Add(itm, qty);
                        numToSell -= qty;
                    }
                    else
                    {
                        itemsToSell.Add(itm, numToSell);
                        numToSell = 0;
                    }
                }

                if (numToSell == 0)
                {
                    break;
                }
            }

            // Gone through all possible "popular" items
            if (numToSell > 0)
            {
                cart.myInventory.GetInventory().Shuffle();
                List<Item> theInventory = cart.myInventory.GetInventory();

                for (int i = 0; i < theInventory.Count; i++)
                {
                    Item itm = theInventory[i];
                    //Debug.Log("Examine " + itm.actorRefName);
                    if (itemsToSell.ContainsKey(itm))
                    {
                        continue;
                    }
                    else
                    {
                        //Debug.Log("Sell some " + itm.actorRefName);
                        int qty = itm.GetQuantity();
                        if (numToSell > qty)
                        {
                            //Debug.Log(numToSell + " greater than qty " + qty);
                            itemsToSell.Add(itm, qty);
                            numToSell -= qty;
                        }
                        else
                        {
                            //Debug.Log(numToSell + " less than or equal to " + qty);
                            itemsToSell.Add(itm, numToSell);
                            numToSell = 0;
                        }
                    }
                    if (numToSell == 0) break;
                }
            }



            //int numPopularItemsSold = 0;
            //int numTotalItems = origNumToSell - numToSell;

            int totalSold = 0;
            foreach(Item key in itemsToSell.Keys)
            {
                cart.myInventory.ChangeItemQuantityAndRemoveIfEmpty(key, -1 * itemsToSell[key]);
                totalSold += itemsToSell[key];
                if (foodDemanded.Contains(key.actorRefName))
                {
                    totalGoldHaul += (int)(itemsToSell[key] * key.GetIndividualSalePrice() * valueMultOfDemand);
                }
                else
                {
                    totalGoldHaul += (int)(itemsToSell[key] * key.GetIndividualSalePrice() * BASE_VALUE_MULT);
                }
            }

            quantity = totalSold;
        }
        else
        {
            Debug.Log("Food cart is null?");
        }

        

        return totalGoldHaul;
    }

    public static void CheckForUpdateFoodCart()
    {
        bool doCheck = false;
        if (lastDayChecked != MetaProgressScript.totalDaysPassed)
        {
            lastDayChecked = MetaProgressScript.totalDaysPassed;
            doCheck = true;
        }

        int goldHaul = 0;
        int itemsSold = 0;

        if (doCheck)
        {
            float chanceToUpdate = CHANCE_UPDATE_DEMANDS_PER_DAY * (MetaProgressScript.totalDaysPassed - dayOfLastDemandUpdate);

            if (UnityEngine.Random.Range(0, 1f) <= chanceToUpdate || foodDemanded.Count == 0)
            {
                UpdateFoodCartDemands();
            }

            chanceToUpdate = CHANCE_SELLITEMS_PER_DAY * (MetaProgressScript.totalDaysPassed - dayOfLastSaleUpdate);

            if (UnityEngine.Random.Range(0, 1f) <= chanceToUpdate)
            {
                goldHaul = UpdateFoodCartSales(out itemsSold);
            }
        }

        string masterString = StringManager.GetString("foodcart_heading") + "\n\n";

        string foodListBuilder = "";
        for (int i = 0; i < foodDemanded.Count; i++)
        {
            string dispName = GameMasterScript.masterItemList[foodDemanded[i]].displayName;
            foodListBuilder += dispName;
            if (i < foodDemanded.Count-1)
            {
                foodListBuilder += ", ";
            }
        }

        StringManager.SetTag(1, foodListBuilder);
        masterString += StringManager.GetString("foodcart_demand") + "\n\n";

        int marketPrice = (int)((valueMultOfDemand - 1f) * 100f);

        StringManager.SetTag(1, "+" + marketPrice + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));

        masterString += StringManager.GetString("foodcart_price");

        if (goldHaul > 0)
        {
            StringManager.SetTag(1, goldHaul.ToString());
            StringManager.SetTag(2, itemsSold.ToString());

            //GameMasterScript.heroPCActor.ChangeMoney(goldHaul);
            int numPiles = UnityEngine.Random.Range(3, 6);
            int amountPerPile = (goldHaul / numPiles);
            int difference = goldHaul - (amountPerPile * numPiles);

            MapTileData origPos = MapMasterScript.GetTile(MapMasterScript.activeMap.FindActor("npc_foodcart").GetPos());

            for (int i = 0; i < numPiles; i++)
            {
                MapTileData newTile = MapMasterScript.GetRandomEmptyTile(origPos.pos, 1, true, true);
                int localAmount = amountPerPile;
                if (i == 0) localAmount += difference;
                MapMasterScript.SpawnCoins(origPos, newTile, localAmount);
            }

            masterString += "\n\n" + StringManager.GetString("foodcart_sales");
            UIManagerScript.PlayCursorSound("Buy Item");
        }

        masterString += "\n";

        StringManager.SetTag(0, masterString);


        // ^tag1^ should have the complete string.
    }

    public static void ResetAllVariablesToGameLoad()
    {
        if (foodDemanded == null)
        {
            foodDemanded = new List<string>();
        }
        cachedCartNPC = null;
        foodDemanded.Clear();
        dayOfLastGoldInvestment = 0;
        dayOfLastDemandUpdate = 0;
        dayOfLastSaleUpdate = 0;
        valueMultOfDemand = 1.0f;
        quantityDemanded = 0;
        goldInvestedInAdvertising = 0;
    }

    public static void UpdateFoodDemands()
    {
        // how many demands? 2-3 basic ingredients, 1-2 meals
        foodDemanded.Clear();

        ActorTable possible = LootGeneratorScript.GetLootTable("food");
        int numBasicFoods = UnityEngine.Random.Range(2, 4);
        for (int i = 0; i < numBasicFoods; i++)
        {
            string foodCheck = possible.GetRandomActorRef();
            while (foodDemanded.Contains(foodCheck) || foodCheck == "item_summonfood" || foodCheck == "spice_rosepetals")
            {
                foodCheck = possible.GetRandomActorRef();
            }
            foodDemanded.Add(foodCheck);
        }

        possible = LootGeneratorScript.GetLootTable("food_and_meals");
        int numFoodOrMeals = UnityEngine.Random.Range(1, 3);
        for (int i = 0; i < numFoodOrMeals; i++)
        {
            string foodCheck = possible.GetRandomActorRef();
            while (foodDemanded.Contains(foodCheck) || foodCheck == "item_summonfood" || foodCheck == "spice_rosepetals")
            {
                foodCheck = possible.GetRandomActorRef();
            }
            foodDemanded.Add(foodCheck);
        }

        quantityDemanded = UnityEngine.Random.Range(10, 14);
    }

	// Use this for initialization
	void Start () {
        foodDemanded = new List<string>();
        
         itemsToSell = new Dictionary<Item, int>();
    }
	
}
