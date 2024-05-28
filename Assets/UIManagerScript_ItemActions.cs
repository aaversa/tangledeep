using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerScript
{

    public void TryUseConsumable()
    {
        if (!GetWindowState(UITabs.INVENTORY)) return;
        if (swappingItems) return;

        if (playerItemList == null) return;

        Item item = null;

        if (invSubmenuOpen)
        {
            item = invItemSelected;
        }
        else
        {
            int selectedButtonIndex = GetIndexOfSelectedButton();
            if (selectedButtonIndex < 0) return;
            if (selectedButtonIndex + listArrayIndexOffset >= playerItemList.Count)
            {
                Debug.Log("Error trying to use item, btn index " + GetIndexOfSelectedButton() + " offset " + listArrayIndexOffset + " vs item count " + playerItemList.Count);
                return;
            }

            item = playerItemList[selectedButtonIndex + listArrayIndexOffset]; // was just index pos
        }

        if (item == null)
        {
            Debug.Log("There is no item at index " + GetIndexOfSelectedButton());
            return;
        }

        if (item.itemType == ItemTypes.CONSUMABLE)
        {
            Consumable drink = (Consumable)item as Consumable;
            if (drink.parentForEffectChildren == null && drink.actorRefName != "scroll_jobchange"
                && drink.actorRefName != "item_dungeonmap")
            {
                // Item not consumable - error.
                return;
            }
            CloseInventorySheet();
            //SetWindowState(UITabs.INVENTORY, false);
            CleanupAfterUIClose(UITabs.INVENTORY);
            gms.PlayerUseConsumable(drink);
            return;
        }
    }

    public void TryUseEQItem(int index)
    {
        // TODO - Rewrite this function to be better...
        //Debug.Log("Trying to use an item.");

        Item item = null;

        if (invSubmenuOpen)
        {
            item = invItemSelected;
        }
        else
        {
            if (GetIndexOfSelectedButton() + listArrayIndexOffset >= playerItemList.Count)
            {
                Debug.Log("Error trying to use item, btn index " + GetIndexOfSelectedButton() + " offset " + listArrayIndexOffset + " vs item count " + playerItemList.Count);
                return;
            }

            item = playerItemList[GetIndexOfSelectedButton() + listArrayIndexOffset]; // was just index pos
        }

        // This must be a consumable.

        if (item.itemType == ItemTypes.CONSUMABLE)
        {
            if (!swappingItems) // Hotbar swap?
            {
                selectedItem = null;
                Consumable drink = (Consumable)item as Consumable;
                if (drink.parentForEffectChildren == null && drink.actorRefName != "scroll_jobchange" && drink.actorRefName != "item_dungeonmap")
                {
                    // Item not consumable - error.
                    return;
                }
                CloseInventorySheet();
                gms.PlayerUseConsumable(drink);
                return;
            }
            // Hotbar swap. Don't use the item.
            HideEQBlinkingCursor();
            AddItemToSlot(item as Consumable, hotbarIndexToReplace, true);
            swappingItems = false;
            UpdateInventorySheet();
        }
        else
        {
            HideEQBlinkingCursor();
        }
    }

    public static void DropItemFromSheet(Item thingToDrop)
    {
        if (thingToDrop == null)
        {
            return;
        }

        if (thingToDrop.itemType == ItemTypes.EMBLEM || thingToDrop.ReadActorData("permabound") == 1)
        {
            UIManagerScript.PlayCursorSound("Error");
            // #todo - Maybe write some text here? Cannot drop emblems
            return;
        }

        Vector2 pos = GameMasterScript.heroPCActor.GetPos();

        bool removed = false;
        if (thingToDrop.IsEquipment())
        {
            Equipment eq = thingToDrop as Equipment;
            if (GameMasterScript.heroPCActor.myEquipment.equipment[(int)eq.slot] == eq)
            {
                GameMasterScript.heroPCActor.Unequip(eq.slot, true);
            }

            if (thingToDrop.itemType == ItemTypes.WEAPON)
            {
                for (int i = 0; i < hotbarWeapons.Length; i++)
                {
                    if (hotbarWeapons[i] == thingToDrop)
                    {
                        Weapon wp = (Weapon)thingToDrop as Weapon;
                        RemoveWeaponFromActiveSlot(wp, i);
                        // break; Could be in more than one slot, so don't break it.
                    }
                }
            }
            removed = GameMasterScript.heroPCActor.myInventory.RemoveItem(thingToDrop);
        }
        else
        {
            if (thingToDrop.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable c = thingToDrop as Consumable;
                if (c.Quantity > 1)
                {

                    //singletonUIMS.CloseAllDialogs();
                    //ForceCloseFullScreenUI();
                    GameMasterScript.gmsSingleton.SetTempStringData("adjustquantity", "drop");
                    GameMasterScript.gmsSingleton.SetTempGameData("dropitem", thingToDrop.actorUniqueID);
                    StringManager.SetTag(0, thingToDrop.displayName);
                    StringManager.SetTag(1, StringManager.GetString("misc_drop"));
                    StartConversationByRef("adjust_quantity", DialogType.COUNT, null);
                    GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "notgold");
                    EnableDialogSlider("", 1, c.Quantity, false, 1.0f);
                    return;
                }
                else
                {
                    removed = GameMasterScript.heroPCActor.myInventory.RemoveItem(thingToDrop);
                }
            }
        }


        // Play a drop sound here

        if (removed)
        {
            LootGeneratorScript.DropItemOnGround(thingToDrop, GameMasterScript.heroPCActor.GetPos(), 1);
        }
        else
        {
            Debug.Log("Failed to drop " + thingToDrop.actorRefName);
        }


    }

    public static void TryUnequipSpecificItem(Equipment eq)
    {
        if (eq.itemType == ItemTypes.WEAPON)
        {
            Weapon wp = eq as Weapon;
            RemoveWeaponFromActives(wp);
        }
        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            if (eq == GameMasterScript.heroPCActor.myEquipment.equipment[i])
            {
                GameMasterScript.heroPCActor.Unequip((EquipmentSlots)i, true);
                if ((eq.itemType == ItemTypes.WEAPON) && ((EquipmentSlots)i == EquipmentSlots.WEAPON))
                {
                    for (int x = 0; x < hotbarWeapons.Length; x++)
                    {
                        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[x], onlyActualFists: true))
                        {
                            SwitchActiveWeaponSlot(x, false);
                            break;
                        }
                    }
                    UpdateActiveWeaponInfo();
                }
                UpdateFullScreenUIContent();
                return;
            }
        }

        GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Unequip");

        singletonUIMS.ClearItemInfo(0);
    }

    public void UnequipViaSubmenu()
    {
        TryUnequipSelected();
    }

    public static void TryUnequipSelected()
    {
        selectedItem = null;

        for (int i = 0; i < eqPlayerEquipment.Length; i++)
        {
            if (uiObjectFocus == eqPlayerEquipment[i])
            {
                if ((i >= 0) && (i < 4))
                {
                    selectedItem = hotbarWeapons[i];
                    if (selectedItem == null)
                    {
                        return;
                    }
                    Weapon wp = selectedItem as Weapon;
                    if (!GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(wp, onlyActualFists: true))
                    {
                        RemoveWeaponFromActives(wp);
                        UpdateActiveWeaponInfo();
                    }
                }
                else
                {
                    if (uiObjectFocus.onSubmitValue < 0)
                    {
                        switch (uiObjectFocus.onSubmitValue)
                        {
                            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1:
                                selectedItem = hotbarWeapons[0];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT2:
                                selectedItem = hotbarWeapons[1];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT3:
                                selectedItem = hotbarWeapons[2];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4:
                                selectedItem = hotbarWeapons[3];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_ARMOR_SLOT:
                                selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ARMOR];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT:
                                selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.OFFHAND];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1:
                                selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2:
                                selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2];
                                break;
                            case (int)EEquipmentUISpecialtySlots.EQ_EMBLEM_SLOT:
                                selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.EMBLEM];
                                break;
                        }
                    }
                    else
                    {
                        selectedItem = GameMasterScript.heroPCActor.myEquipment.equipment[uiObjectFocus.onSubmitValue];
                    }


                }
            }
        }
        if (selectedItem != null)
        {
            for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
            {
                if (selectedItem == GameMasterScript.heroPCActor.myEquipment.equipment[i])
                {
                    GameMasterScript.heroPCActor.Unequip((EquipmentSlots)i, true);
                    if ((selectedItem.itemType == ItemTypes.WEAPON) && ((EquipmentSlots)i == EquipmentSlots.WEAPON))
                    {
                        for (int x = 0; x < hotbarWeapons.Length; x++)
                        {
                            if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[x], onlyActualFists: true))
                            {
                                SwitchActiveWeaponSlot(x, false);
                                break;
                            }
                        }
                        UpdateActiveWeaponInfo();
                    }
                    return;
                }
            }
        }

        singletonUIMS.ClearItemInfo(0);
    }

    public void DropInvItem(int index)
    {
        if (draggingItem == null)
        {
            ExitDragMode();
            return;
        }

        if (index == 999)
        {
            if (draggingItemButtonIndex < 0)
            {
                ClearHotbar(draggingItemButtonIndex + MAX_HOTBARS * 8);
            }
            UpdateInventorySheet();
            ExitDragMode();
            return;
        }

        if (index < 0)
        {
            AbilityScript switchedAbil = null;
            // Inventory hotbar
            bool swapped = false;
            int nIndex = index + 16;
            if (draggingItemButtonIndex < 0)
            {
                int dIndex = draggingItemButtonIndex + MAX_HOTBARS * 8;
                bool swappedAbil = false;

                if (hotbarAbilities[nIndex].actionType == HotbarBindableActions.ABILITY)
                {
                    switchedAbil = hotbarAbilities[nIndex].ability;
                    AddAbilityToSlot(switchedAbil, dIndex, false);
                    swappedAbil = true;
                }

                if (hotbarAbilities[dIndex].actionType == HotbarBindableActions.CONSUMABLE)
                {
                    Consumable swap = hotbarAbilities[nIndex].consume;
                    if (swap != null)
                    {
                        AddItemToSlot(swap, dIndex, false);
                        swapped = true;
                    }
                }
                else
                {
                    if (!swappedAbil)
                    {
                        ClearHotbar(dIndex);
                    }


                }
            }
            ClearHotbar(nIndex);
            Consumable consume = draggingItem as Consumable;
            AddItemToSlot(consume, nIndex, !swapped);
            UpdateInventorySheet();
            ExitDragMode();
            return;
        }

        GameMasterScript.heroPCActor.myInventory.RemoveItem(draggingItem);

        if (draggingItem.itemType == ItemTypes.CONSUMABLE)
        {
            RefreshHotbarItems();
        }

        StringManager.SetTag(0, draggingItem.displayName);
        GameLogScript.LogWriteStringRef("log_playerdrop_item");
        // Play a drop sound here

        // Check player offering
        Vector2 pos = GameMasterScript.heroPCActor.GetPos();

        draggingItem.SetSpawnPosXY((int)pos.x, (int)pos.y);
        GameMasterScript.SpawnItemAtPosition(draggingItem, pos);
        draggingItem.collection = null;

        UpdateInventorySheet();
        ExitDragMode();
    }
}
