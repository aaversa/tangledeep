using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Linq;
using System.Globalization;

public class InventoryScript {

    List<Item> listInventory;

    Actor owner;
    public Actor Owner
    {
        get
        {
            return owner;
        }
        set
        {
            if (value != null && value.GetActorType() == ActorTypes.HERO) ownerIsHero = true;
            owner = value;
        }
    }

    static List<Item> pool_itemList = new List<Item>();

    bool ownerIsHero = false;

    public bool consumableItemListDirty = true;

    public void ReadFromSave(XmlReader reader, bool addItemsToMasterDict = true, bool validateItemsForInventory = true) {
		while (reader.NodeType != XmlNodeType.EndElement) {
			string strValue = reader.Name.ToLowerInvariant();

			switch(strValue) {
				case "item":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }

					Item create = new Item();
                    //Debug.Log("Read item for " + owner.actorRefName + " " + owner.actorUniqueID);
					create = create.ReadFromSave(reader, addItemsToMasterDict);
                    //Debug.Log("Read item " + create.actorRefName + " " + create.actorUniqueID + " for " + owner.actorRefName + " " + owner.actorUniqueID);
                    if (create == null)
                    {
                        continue;
                    }

                    if (GameMasterScript.gmsSingleton.TryLinkActorFromDict(create.actorUniqueID) != null)
                    {
                        /* if (validateItemsForInventory && owner.actorUniqueID == 13847)
                        {
                            Debug.Log(create.actorRefName + " " + create.actorUniqueID + " already exists in dict.");
                        } */

                        if (GetItemByID(create.actorUniqueID) != null)
                        {
                            //Debug.Log("Item " + create.actorUniqueID + " " + create.actorRefName + " already exists in " + owner.actorRefName + " on fl " + owner.dungeonFloor);
                        }
                        else
                        {
                            if (validateItemsForInventory && !create.ValidForInventory(Owner))
                            {
                                // Skip.
                            }
                            else
                            {
                                _AddToInventory(create);
                                create.collection = this;
                                if (create.legendary && validateItemsForInventory)
                                {
                                    if (Owner.GetActorType() == ActorTypes.HERO || Owner.actorRefName == "npc_banker")
                                    {
                                        create.SetActorData("pwn", 1);
                                    }
                                }                                
                            }
                        }
                    }
                    else
                    {
                        /* if (validateItemsForInventory && owner.actorUniqueID == 13847)
                        {
                            Debug.Log(create.actorRefName + " " + create.actorUniqueID + " is not in the dict, but also we're not adding items to dict because why?");
                        } */

                        // Still add to dict, right?
                        if (!addItemsToMasterDict)
                        {
                            if (validateItemsForInventory && create.ValidForInventory(Owner))
                            {
                                _AddToInventory(create);
                                create.collection = this;
                            }
                        }
                    }
                    break;
				default:
					reader.Read();
					break;
			}
		}
		reader.ReadEndElement();
        //Debug.Log("Done reading inventory. Reader is at " + reader.Name + " " + reader.NodeType.ToString());
    }

    public void WriteToSave(XmlWriter writer) {

        //writer.WriteStartElement("Inventory");
        writer.WriteStartElement("inv");

        foreach (Item itm in listInventory)
        {
            itm.collection = this; // New code 9/27 to ensure that if an item is in a collection, we mark it as such.
            itm.WriteToSave(writer);
    	}

    	writer.WriteEndElement();
    }

    public InventoryScript()
    {
        listInventory = new List<Item>();
    }

    public int GetSize()
    {
        return listInventory.Count;
    }

    public List<Item> GetInventory()
    {
        return listInventory;
    }

    public bool HasItem(Item itemToCheck)
    {
        return listInventory.Contains(itemToCheck);
    }

    public void SetInventoryList(List<Item> newList)
    {
        if (Debug.isDebugBuild) Debug.Log("Setting inventory list for " + owner.actorRefName + " to new list with count " + newList.Count);
        listInventory = newList;
        //if (Debug.isDebugBuild) Debug.Log("That list inv now has " + listInventory.Count);
    }

    /* public void DropItem(Item itam)
    {
        if (itam.GetQuantity() > 1)
        {
            UIManagerScript.StartConversationByRef("adjust_quantity", DialogType.KEYSTORY, null);
        }
        else
        {
            RemoveItem(itam);
            LootGeneratorScript.DropItemOnGround(itam, GameMasterScript.heroPCActor.GetPos(), 1);
        }
    } */

    public bool HasItemByRef(string refToCheck)
    {
        foreach (Item itm in listInventory)
        {
            if (itm.actorRefName == refToCheck) return true;
        }
        return false;
    }

    // Returns TRUE if a unique item was added to the collection
    // Returns FALSE if for any reason that did not occur, i.e. invalid item or we just increased quantity of existing item
    public bool AddItem(Item itemToAdd, bool stackItems)
    {
        if (itemToAdd == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Tried to add null item.");
#endif
            return false;
        }
        bool ownerValid = Owner != null;
        if (listInventory.Contains(itemToAdd))
        {
            if (ownerValid)
            {
#if UNITY_EDITOR
                Debug.Log("WARNING: Cannot add " + itemToAdd.actorRefName + " " + itemToAdd.actorUniqueID + " to " + Owner.actorRefName + " inventory twice.");
#endif
            }
            return false;
        }
        
        if (ownerValid && Owner.actorRefName == "npc_rubymoon_merchant_desert")
        {
            if (itemToAdd.IsEquipment() && itemToAdd.rarity == Rarity.COMMON)
            {
                Equipment eq = itemToAdd as Equipment;
                EquipmentBlock.MakeMagical(eq, eq.challengeValue, false);
            }
        }
        if (ownerValid && Owner.GetActorType() == ActorTypes.HERO)
        {
            if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsMysteryDungeonMap() && !MysteryDungeonManager.exitingMysteryDungeon)
            {
                if (itemToAdd.ReadActorData("md_entry_gear") != 1)
                {
                    itemToAdd.SetActorData("mdgear", 1);
                }
            }
            if (GameMasterScript.gameLoadSequenceCompleted && (SharaModeStuff.IsSharaModeActive() || RandomJobMode.IsCurrentGameInRandomJobMode()) 
                && itemToAdd.itemType == ItemTypes.CONSUMABLE && itemToAdd.actorRefName == "scroll_jobchange")
            {
                StringManager.SetTag(0, itemToAdd.displayName);
                GameLogScript.LogWriteStringRef("log_summon_disappear");
#if UNITY_EDITOR
                //Debug.Log("Item go poof");
#endif
                return false;
            }
        }

        if (ownerValid && Owner.IsFighter())
        {
            if (Owner.GetActorType() == ActorTypes.HERO)
            {
                bool canAddItem = GameModifiersScript.CheckForValidItemPickup(itemToAdd);
                if (!canAddItem)
                {
                    MapTileData randomTile = MapMasterScript.GetRandomEmptyTile(Owner.GetPos(), 1, true, true);
                    if (Debug.isDebugBuild) Debug.Log("Can't add item so adding to random tile.");
                    return false;
                }
            }

                Fighter ft = Owner as Fighter;
            for (int i = 0; i < ft.myEquipment.equipment.Length; i++)
            {
                if (itemToAdd == ft.myEquipment.equipment[i])
                {
                    if (Debug.isDebugBuild) Debug.Log("WARNING: Cannot add " + itemToAdd.actorRefName + " " + itemToAdd.actorUniqueID + " to " + Owner.actorRefName + " as owner has equipped it already.");
                    return false;
                }
            }
            if (Owner.GetActorType() == ActorTypes.HERO && itemToAdd.legendary)
            {
                LootGeneratorScript.RemoveLegendaryFromEverything(itemToAdd);
                itemToAdd.SetActorData("pwn", 1);
            }
        }

        // Is this an item we found in a dream?
        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsItemWorld() && itemToAdd.ReadActorData("fromdreammob") == 1)
        {
            itemToAdd.RemoveActorData("fromdreammob");
            GameMasterScript.heroPCActor.AddActorData("dream_numitems", 1);
        }

        bool combinedIntoExistingItem = false;

        Item existingItemCombinedInto = null;

        if (itemToAdd.itemType == ItemTypes.CONSUMABLE && stackItems)
        {
     
            Consumable adding = itemToAdd as Consumable;
            int addQuantity = adding.Quantity;
            foreach(Item compare in listInventory)
            {
                if (compare.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable con = compare as Consumable;
                    if (itemToAdd.actorRefName == compare.actorRefName)
                    {
                        if (!adding.IsConsumableStackable(con)) continue;
                        
                        if (con.seasoningAttached == adding.seasoningAttached)
                        {                            
                            //Debug.Log(con.quantity + " current quantity of " + con.actorRefName + " in " + owner.actorRefName + " inventory. We are picking up item " + itemToAdd.actorRefName + " of quantity " + addQuantity);
                            con.ChangeQuantity(addQuantity);
                            combinedIntoExistingItem = true;
                            existingItemCombinedInto = con;
                            break;
                        }
                    }
                }
            }
        }
        itemToAdd.collection = this;

        if (!combinedIntoExistingItem)
        {
            itemToAdd.newlyPickedUp = true;
        }

        //Debug.Log(itemToAdd.actorRefName + " " + itemToAdd.GetQuantity() + " " + combinedIntoExistingItem);

        bool okToAddToInventory = true;

        if (itemToAdd.actorRefName == "item_lucidorb_shard" && 
            Owner is HeroPC)
        {
            int existingQuantity = 0;
            if (combinedIntoExistingItem)
            {
                existingQuantity = existingItemCombinedInto.GetQuantity();
            }
            else
            {
                existingQuantity = itemToAdd.GetQuantity();
                existingItemCombinedInto = itemToAdd; // We took a stack of 3 shards from the bank.
            }

            //What is this for? It is never used.
            string newlyAddedRef = itemToAdd.GetOrbMagicModRef();

            //Maybe this value will change for more complex orbs? 
            const int iShardsRequiredForOrb = 3;

            //for every three shards we have, create one orb. It is possible that we will have > 3 in our
            //inventory at the same time. 
            while (existingQuantity >= iShardsRequiredForOrb)
            {
                //create an orb, congratulations!
                Item newLucidOrb = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 0f, 0f, false, false);

                newLucidOrb.SetOrbMagicModRef(existingItemCombinedInto.GetOrbMagicModRef());
                //newLucidOrb.SetActorDataString("orbmagicmodref", existingItemCombinedInto.ReadActorDataString("orbmagicmodref"));
                newLucidOrb.RebuildDisplayName();
                StringManager.SetTag(0, newLucidOrb.displayName);
                GameLogScript.LogWriteStringRef("log_fuse_lucidorbshards");
                AddItem(newLucidOrb, true);

                //now reduce the quantity of the existing item by iShardsRequiredForOrb.
                existingItemCombinedInto.ChangeQuantity(-iShardsRequiredForOrb);                
                existingQuantity = existingItemCombinedInto.GetQuantity();
                combinedIntoExistingItem = true;
            }

            //If we've drained the quantity of the item, remove it from our bags.
            if (existingQuantity == 0)
            {
                RemoveItem(existingItemCombinedInto);
                okToAddToInventory = false;
            }
        }

        if (okToAddToInventory)
        {
            // This checks for stuff like Legendary duplicates.
            okToAddToInventory = itemToAdd.ValidForInventory(Owner);
            if (!okToAddToInventory)
            {
                // At this point, if the item is actually INVALID it means we should pretend this pickup never happened
                // i.e. It's a bugged item (CV > 500f) or a duplicate legendary
                // So just exit.
                //if (Debug.isDebugBuild) Debug.Log("Bugged item? Why won't it add.");

#if UNITY_EDITOR
                //Debug.Log("Invalid for owner's inventory");
#endif
                return false;
            }
        }

        if (combinedIntoExistingItem || !okToAddToInventory)
        {
            // DO NOT actually add to this inventory collection.
        }
        else
        {
            // Safe to add to our inventory.            
            if (Owner.GetActorType() == ActorTypes.HERO)
            {
                listInventory.Insert(0, itemToAdd);
                //if (Debug.isDebugBuild) Debug.Log("Insert to inv list.");
            }
            else
            {
                listInventory.Add(itemToAdd);
            }            
        }

        // Run a custom script if we have one. Note that at this point we have either:
        // 1. Already added the Item to our inventory List<> collection, OR
        // 2. NOT added to our collection, because we increased the Quantity of an existing item

        if (!String.IsNullOrEmpty(itemToAdd.scriptOnAddToInventory))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(ItemPickupScript), itemToAdd.scriptOnAddToInventory);
            #if UNITY_EDITOR
            
            object[] paramList = new object[3];
            paramList[0] = this; // this Inventory
            paramList[1] = itemToAdd; // the Item that was added
            paramList[2] = combinedIntoExistingItem; // TRUE if we added this item to a stack
            object wasUniqueItemAddedToInventory = runscript.Invoke(null, paramList);
            return Convert.ToBoolean(wasUniqueItemAddedToInventory);
            
            #else
            try
            {
                object[] paramList = new object[3];
                paramList[0] = this; // this Inventory
                paramList[1] = itemToAdd; // the Item that was added
                paramList[2] = combinedIntoExistingItem; // TRUE if we added this item to a stack
                object wasUniqueItemAddedToInventory = runscript.Invoke(null, paramList);
                return Convert.ToBoolean(wasUniqueItemAddedToInventory);

            }
            catch (Exception e)
            {
                Debug.Log("Error with " + itemToAdd.scriptOnAddToInventory + ": " + e);
            }
            #endif
        }

        if (itemToAdd.itemType == ItemTypes.CONSUMABLE) consumableItemListDirty = true;

        return true;    
    }

    public void ChangeItemQuantityByRef(string refName, int amount)
    {
        Item itemRef = null;
        foreach(Item itm in listInventory)
        {
            if (refName == itm.actorRefName)
            {
                itemRef = itm;
                break;
            }
        }
        if (itemRef == null)
        {
            if (Owner != null) Debug.Log(Owner.actorRefName + " does not have item " + refName);
            return;
        }
        ChangeItemQuantityAndRemoveIfEmpty(itemRef, amount);
    }

    public void ChangeItemQuantityAndRemoveIfEmpty(Item itm, int amount)
    {
        if (itm.itemType != ItemTypes.CONSUMABLE)
        {
            RemoveItem(itm);
            return;
        }
        Consumable c = itm as Consumable;
        bool anyRemaining = c.ChangeQuantity(amount);
        if (!anyRemaining)
        {
            RemoveItem(itm);
        }
        consumableItemListDirty = true;
    }

    public bool RemoveItem(Item itemToRemove, bool equippingItem = false)
    {
        // Check if item is equipped for some reason. If so, unequip it.
        if (itemToRemove.IsEquipment())
        {
            Equipment eq = itemToRemove as Equipment;
            if (Owner.GetActorType() == ActorTypes.MONSTER || Owner.GetActorType() == ActorTypes.HERO)
            {
                Fighter ft = Owner as Fighter;
                ft.myEquipment.UnequipByReference(eq);
                /* if (owner.GetActorType() == ActorTypes.HERO && eq.itemType == ItemTypes.WEAPON)
                {
                    Weapon w = eq as Weapon;
                    UIManagerScript.RemoveWeaponFromActives(w);
                } */
            }

            if (!equippingItem)
            {
                eq.RemoveAllPairedItem();
            }
        }

        if (listInventory.Contains(itemToRemove))
        {
            _RemoveFromInventory(itemToRemove);
            return true;
        }
        else
        {
            return false;
        }
    }

    public Item GetMostRareItemByRef(string iRef)
    {
        Item best = null;
        Rarity bestRarity = Rarity.COMMON;            
        foreach(Item checkItem in listInventory)
        {
            if (checkItem.actorRefName == iRef)
            {
                if ((int)checkItem.rarity >= (int)bestRarity)
                {
                    best = checkItem;
                    bestRarity = checkItem.rarity;
                }
            }
        }

        if (best == null)
        {
            //Debug.Log("Couldn't find " + iRef);
        }
        return best;
    }

    public Item GetItemByID(int id)
    {        
        foreach(Item itm in listInventory)
        {
            if (itm.actorUniqueID == id)
            {
                return itm;
            }
        }
        //Debug.Log(owner.actorUniqueID + " " + owner.actorRefName + " couldn't find item ID " + id);
        return null;
    }

    public bool RemoveItemByRef(string refName)
    {
        Item remover = null;
        foreach(Item itm in listInventory)
        {
            if (itm.actorRefName == refName)
            {
                remover = itm;
                break;
            }
        }
        if (remover != null)
        {
            _RemoveFromInventory(remover);
            return true;
        }

        return false; // an item was not removed
    }

    public bool RemoveItemOrDecrementQuantityByRef(string refName)
    {
        Item remover = null;
        foreach(Item itm in listInventory)
        {
            if (itm.actorRefName == refName)
            {
                remover = itm;
            }
        }
        if (remover != null)
        {
            if (remover.GetQuantity() == 1)
            {
                _RemoveFromInventory(remover);
            }
            else
            {
                Consumable c = remover as Consumable;
                c.ChangeQuantity(-1);
                consumableItemListDirty = true;
            }
        }

        return false; // an item was not removed
    }

    public bool GetCurry(out Item curry)
    {
        curry = null;
        string stew = "item_food_tangledeepstew";
        foreach (Item itm in listInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            Consumable c = itm as Consumable;
            if (!c.CheckTag((int)ItemFilters.RECOVERY)) continue;
            if (itm.actorRefName != stew) continue;
            if (c.seasoning) continue;
            curry = itm;
            return true;
        }

        return false;
    }

    public Item GetItemByRef(string refName)
    {
        foreach(Item itm in listInventory)
        {
            if (itm.actorRefName == refName)
            {
                return itm;
            }
        }
        return null;
    }

    public bool CanItemFitInInventory(Item itm)
    {
        if (itm.itemType != ItemTypes.CONSUMABLE) return true;
        //if (!GameStartData.CheckGameModifier(GameModifiers.ITEM_LIMIT_9STACKS)) return true;
        Consumable c = itm as Consumable;
        if (c.CheckTag((int)ItemFilters.VALUABLES))
        {
            return true;
        }

        int qty = itm.GetQuantity();
        int curQty = GetItemQuantity(itm.actorRefName);

        if (qty + curQty > 9)
        {
            return false;
        }

        return true;
    }

    public bool AddItemRemoveFromPrevCollection(Item itemToAdd, bool stackItems, int maxQuantity = 9999, bool isBanker = false)
    {
        if (itemToAdd == null)
        {
            if (Owner != null) Debug.Log("Trying to add a null item to collection of " + Owner.actorRefName);
            return false;
        }

        Item itemToTransfer = itemToAdd;

        if (itemToTransfer.GetQuantity() > maxQuantity)
        {
            // Let's say we can only transfer 1, but we are being asked to transfer 5.
            // We must create a new stack.
            // Consumables are the only things with quantity, so we know we are creating one of those.
            itemToTransfer = new Consumable();
            Consumable newConsumable = itemToTransfer as Consumable;
            itemToTransfer.CopyFromItem(itemToAdd);
            itemToTransfer.SetUniqueIDAndAddToDict();
            newConsumable.Quantity = maxQuantity;
            itemToAdd.ChangeQuantity(-1 * maxQuantity);
#if UNITY_EDITOR
            //Debug.Log("Reduced " + itemToAdd.displayName + " " + itemToAdd.actorUniqueID + " quantity by " + maxQuantity + ", creating a new stack with quantity " + newConsumable.Quantity + ". Original item now has " + itemToTransfer.GetQuantity() + ", returning NEW item " + newConsumable.actorUniqueID);
#endif
        }

        InventoryScript collection = itemToAdd.collection; // Existing collection

        bool deferRemovingFromPlayerInventory = false;

        if (collection != null && collection != this)
        {
            if (collection.Owner == GameMasterScript.heroPCActor)
            {
                deferRemovingFromPlayerInventory = true;
            }
            else
            {
                collection.RemoveItem(itemToTransfer);
            }
            
        }

        // Only modify favorite status when selling to shop
        if (Owner.GetActorType() == ActorTypes.NPC)
        {
            itemToTransfer.favorite = false;
            itemToAdd.vendorTrash = false;
        }

        if (!AddItem(itemToTransfer, stackItems))
        {
#if UNITY_EDITOR
            //Debug.Log("Adding item failed!");
            return false;
#endif
        }
        else
        {
            // Only remove an item from the player's inventory if we definitely added it to the target inventory, otherwise it could be gone 4ever
            if (deferRemovingFromPlayerInventory)
            {
                collection.RemoveItem(itemToTransfer);
            }            
        }

        return true;
    }

    public bool HasAnyFood()
    {
        foreach(Item itm in listInventory)
        {
            if (itm.IsItemFood()) return true;
        }
        return false;
    }

    public bool HasAnyNonSpicedNonInstantRestorativeFood(out Item restorativeToUse)
    {
        restorativeToUse = null;
        foreach (Item itm in listInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            Consumable c = itm as Consumable;
            if (c.IsValidForAutoEat(StatTypes.HEALTH) == AutoEatState.HEAL_HP)
            {
                restorativeToUse = c;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Retrieves quantity of this actorRef, accounting for lucid/skill orbs
    /// </summary>
    /// <param name="specificItem"></param>
    /// <returns></returns>
    public int GetItemQuantity(Item specificItem)
    {
        //string lucidOrbRef = specificItem.ReadActorDataString("orbmagicmodref");
        string lucidOrbRef = specificItem.GetOrbMagicModRef();
        bool isLucidOrSkillorb = !string.IsNullOrEmpty(lucidOrbRef);

        int count = 0;
        foreach (Item itm in listInventory)
        {
            if (itm.actorRefName == specificItem.actorRefName)
            {
                if (itm.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable c = itm as Consumable;

                    if (isLucidOrSkillorb)
                    {
                        if (itm.GetOrbMagicModRef() != lucidOrbRef)
                        {
                            continue;
                        }
                    }

                    count += c.Quantity;
                }
                else
                {
                    count++;
                }
            }
        }

        return count;
    }

    public int GetItemQuantity(string name)
    {        
        int count = 0;
        foreach(Item itm in listInventory)
        {
            if (itm.actorRefName == name)
            {
                if (itm.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable c = itm as Consumable;
                    count += c.Quantity;
                }
                else
                {
                    count++;
                }
            }
        }

        return count;
    }
    
    // BE CAREFUL with this!
    public void SortMyInventory(InventorySortTypes sortType, bool forward, bool doFavoriteSort = true)
    {
        //Debug.Log(owner.actorUniqueID + " sorting my inventory.");
        try {
            listInventory = SortAnyInventory(listInventory, (int)sortType, forward, doFavoriteSort);
        }
        catch(Exception e)
        {
            Debug.Log("Sort own inventory error: " + e);
        }        
    }

    public static List<Item> SortAnyInventory(List<Item> inventory, int sortType, bool forward, bool doFavoriteSort = true)
    {
        //Debug.Log("Request sort any inventory by " + (InventorySortTypes)sortType + " " + forward + ", fave sort? " + doFavoriteSort);
        if (inventory.Count < 2) return inventory;

        switch ((InventorySortTypes)sortType)
        {
            case InventorySortTypes.ALPHA:

                // We must be specific about exactly how the sort executes, since some cultures
                // have multiple sort types etc. This should be the best all-around set of parameters.
                CultureInfo culture = StringManager.GetCurrentCulture();
                StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase;

                /* if (forward)
                {
                    inventory.Sort((a, b) => string.Compare(a.GetSortableName(), b.GetSortableName(), stringComparison) + b.favorite.CompareTo(a.favorite));
                }
                else
                {
                    inventory.Sort((a, b) => string.Compare(b.GetSortableName(), a.GetSortableName(), stringComparison) + b.favorite.CompareTo(a.favorite));
                } */

                inventory.Sort((a, b) =>
                b.favorite.CompareTo(a.favorite) * 4 +
                string.Compare(a.GetSortableName(), b.GetSortableName(), stringComparison) * (forward ? 2 : 2));

                break;
            case InventorySortTypes.RARITY:
                inventory.Sort((a,b) =>
                    b.favorite.CompareTo(a.favorite) * 4 +
                    a.rarity.CompareTo(b.rarity) * (forward ? 2 : -2) +
                    a.GetSortableName().CompareTo(b.GetSortableName())
                );
                break;
            case InventorySortTypes.RANK:
                // This WAS GetBaseItemRank()
                inventory.Sort((a, b) =>
                b.favorite.CompareTo(a.favorite) * 4 +
                a.GetConvertedItemRankFloat().CompareTo(b.GetConvertedItemRankFloat()) * (forward ? 2 : -2) + 
                a.GetSortableName().CompareTo(b.GetSortableName()));
                break;
            case InventorySortTypes.ITEMTYPE:
                inventory.Sort((a, b) =>
                b.favorite.CompareTo(a.favorite) * 4 +
                a.GenerateSubtypeAsInt().CompareTo(b.GenerateSubtypeAsInt()) * (forward ? 2 : -2) + 
                a.GetSortableName().CompareTo(b.GetSortableName()));
                break;
            case InventorySortTypes.VALUE:
                inventory.Sort((a,b) =>
                b.favorite.CompareTo(a.favorite) * 4 +
                a.GetIndividualShopPrice().CompareTo(b.GetIndividualShopPrice()) * (forward ? 2 : -2) +
                a.GetSortableName().CompareTo(b.GetSortableName()));
                break;
            case InventorySortTypes.CONSUMABLETYPE:
                inventory.Sort((a,b) =>
                b.favorite.CompareTo(a.favorite) * 4 +
                GetConsumableTypeSortValue(a).CompareTo(GetConsumableTypeSortValue(b)) * (forward ? 2 : -2) +
                a.GetSortableName().CompareTo(b.GetSortableName()));
                break;
        }

        //if (doFavoriteSort) inventory.Sort((a, b) => b.favorite.CompareTo(a.favorite));

        return inventory;
    }
    
    //Sub-optimal, but we don't want to change the order of the 
    //ItemFilters enum just to satisfy this particular need
    public static int GetConsumableTypeSortValue(Item itm)
    {
        if (itm.IsCurative(StatTypes.HEALTH))
        {
            return 0;
        }

        if (itm.CheckTag(ItemFilters.SUPPORT))
            return 1;
        if (itm.CheckTag(ItemFilters.MEAL))
            return 2;
        if (itm.CheckTag(ItemFilters.OFFENSE))
            return 3;
        if (itm.CheckTag(ItemFilters.INGREDIENT))
            return 4;
        if (itm.CheckTag(ItemFilters.VALUABLES))
            return 5;

        return 999;
    }

    public static List<Item> SortAnyInventory_onb(List<Item> inventory, int sortType, bool forward)
    {        
        if (inventory.Count < 2) return inventory;

        if (forward)
        {
            switch ((InventorySortTypes)sortType)
            {
                case InventorySortTypes.ALPHA:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (String.Compare(inventory[j].GetSortableName(), x.GetSortableName()) > 0))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.RARITY:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && ((int)inventory[j].rarity < (int)x.rarity))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.RANK:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (inventory[j].GetBaseItemRank() < x.GetBaseItemRank()))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.ITEMTYPE:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && ((int)inventory[j].itemType < (int)x.itemType))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.VALUE:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (inventory[j].GetIndividualShopPrice() < x.GetIndividualShopPrice()))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.CONSUMABLETYPE:

                    break;
            }
        }
        else
        {
            switch ((InventorySortTypes)sortType)
            {
                case InventorySortTypes.ALPHA:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (String.Compare(inventory[j].GetSortableName(), x.GetSortableName()) < 0))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.RARITY:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && ((int)inventory[j].rarity > (int)x.rarity))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.RANK:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (inventory[j].GetBaseItemRank() > x.GetBaseItemRank()))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.ITEMTYPE:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && ((int)inventory[j].itemType > (int)x.itemType))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
                case InventorySortTypes.VALUE:
                    for (int i = 1; i < inventory.Count; i++)
                    {
                        Item x = inventory[i];
                        int j = i - 1;
                        while ((j >= 0) && (inventory[j].GetIndividualSalePrice() > x.GetIndividualSalePrice()))
                        {
                            inventory[j + 1] = inventory[j];
                            j -= 1;
                        }
                        inventory[j + 1] = x;
                    }
                    break;
            }
        }

        return inventory;
    }

    public int GetActualInventoryCount()
    {
        int runningQuantity = 0;
        foreach(Item itm in listInventory)
        {
            runningQuantity += itm.GetQuantity();
        }
        return runningQuantity;
    }

    public void RemoveAllDreamItems()
    {
        List<Item> toRemove = new List<Item>();
        foreach(Item itm in listInventory)
        {
            if (itm.dreamItem)
            {
                toRemove.Add(itm);
            }
        }
        foreach(Item itm in toRemove)
        {
            RemoveItem(itm);
        }
        UIManagerScript.RefreshHotbarItems();
    }
    public Item GetItemAndSplitIfNeeded(Item itm, int quantity)
    {
        if (itm.itemType != ItemTypes.CONSUMABLE)
        {
            RemoveItem(itm);
            return itm;
        }
        if (itm.GetQuantity() == 1)
        {
            RemoveItem(itm);
            return itm;
        }

        // Must be consumable.
        Consumable originalItemBeingReduced = itm as Consumable;

        int initialQuantity = originalItemBeingReduced.Quantity;

        if (Debug.isDebugBuild) Debug.Log("Maybe split " + originalItemBeingReduced.actorUniqueID + " which has q " + originalItemBeingReduced.GetQuantity());
        originalItemBeingReduced.ChangeQuantity(-1 * quantity);
        if (Debug.isDebugBuild) Debug.Log("It now has " + originalItemBeingReduced.GetQuantity());

        if (originalItemBeingReduced.Quantity <= 0)
        {
            RemoveItem(originalItemBeingReduced);
            originalItemBeingReduced.Quantity = quantity;
            if (Debug.isDebugBuild) Debug.Log(originalItemBeingReduced.displayName + " has no quantity left, so we are returning it with max quantity of " + quantity);
            return itm;
        }
        else
        {
            Consumable copyStack = new Consumable();            
            copyStack.CopyFromItem(originalItemBeingReduced);
            copyStack.SetUniqueIDAndAddToDict();
            copyStack.Quantity = quantity;
            if (Debug.isDebugBuild) Debug.Log("Reduced " + originalItemBeingReduced.displayName + " " + originalItemBeingReduced.actorUniqueID + " quantity by " + quantity + ", creating a new stack with quantity " + copyStack.Quantity + ". Original item now has " + originalItemBeingReduced.Quantity + ", returning NEW item " + copyStack.actorUniqueID);
            return copyStack;
        }
    }

    public List<Item> GetAllCookingIngredients()
    {
        return listInventory.FindAll(a => (a.IsCookingIngredient())).ToList();
    }

    void _AddToInventory(Item toAdd)
    {
        listInventory.Add(toAdd);
        if (toAdd.itemType == ItemTypes.CONSUMABLE && ownerIsHero)
        {            
            consumableItemListDirty = true;
            GuideMode.CalculateThenCheckFoodPulse();
        }
    }

    void _RemoveFromInventory(Item toRemove)
    {        
        listInventory.Remove(toRemove);
        if (toRemove.itemType == ItemTypes.CONSUMABLE)
        {
            consumableItemListDirty = true;
            GuideMode.CalculateThenCheckFoodPulse();
        }
    }

    public void ClearInventory()
    {
        listInventory.Clear();
        consumableItemListDirty = true;
    }

    public void RemoveInvalidItems()
    {
        List<Item> toRemove = new List<Item>();
        foreach(Item itm in listInventory)
        {
            if (Owner.GetActorType() == ActorTypes.HERO)
            {
                if (!itm.ValidForPlayer())
                {
                    toRemove.Add(itm);
                }
            }
            else if (Owner.GetActorType() == ActorTypes.MONSTER)
            {
                if (itm.challengeValue >= 500f)
                {
                    toRemove.Add(itm);
                }
            }

        }
        foreach(Item itm in toRemove)
        {
            _RemoveFromInventory(itm);
        }        
    }

    // Gets orbs, food, etc with no special seasoning or other traits
    public Item GetIdeallyUnmodifiedFoodbyRef(string refName)
    {
        Item retItem = null;
        foreach(Item itm in listInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            Consumable c = itm as Consumable;
            if (c.actorRefName != refName) continue;
            if (c.isFood && string.IsNullOrEmpty(c.seasoningAttached)) // Unmodified ingredient or meal
            {
                retItem = c;
                break;
            }
            else if (!c.isFood && c.actorRefName == "orb_itemworld" && string.IsNullOrEmpty(c.GetOrbMagicModRef()))
            {
                // Unmodified, plain ol' orb
                retItem = c;
                break;
            }
            retItem = c;
            
        }
        return retItem;
    }

    public Item GetRandomFoodItem()
    {
        pool_itemList.Clear();
        foreach(Item itm in listInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (itm.IsItemFood())
            {
                pool_itemList.Add(itm);
            }
        }

        return pool_itemList.GetRandomElement();
    }

    public bool CanStackItem(Item i)
    {
        if (i.itemType != ItemTypes.CONSUMABLE) return false;

        Consumable c = i as Consumable;

        foreach(Item itm in listInventory)
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (itm.actorRefName != i.actorRefName) continue;
            Consumable c2 = itm as Consumable;
            return c.IsConsumableStackable(c2);
        }

        return false;
    }

    public void RemoveNullItems() 
    {
        //if (Debug.isDebugBuild) Debug.Log("Inventory count " + listInventory.Count);
        listInventory.RemoveAll(a => a == null);
        //if (Debug.isDebugBuild) Debug.Log("New count, nulls removed, is " + listInventory.Count);
    }
}
/*
public class ItemAlphaComparer : IComparer<Item>
{
    public int Compare(Item a, Item b)
    {
        CultureInfo culture = StringManager.GetCurrentCulture();
        StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase;

        return string.Compare(a.GetSortableName(), b.GetSortableName(), stringComparison));
    }
}
*/
