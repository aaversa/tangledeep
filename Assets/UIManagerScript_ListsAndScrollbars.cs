using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public partial class UIManagerScript
{
    public static void ScrollPages(bool forward)
    {
        int numButtons = 0;
        int maxItems = 0;
        if (singletonUIMS.dialogValueSliderParent.activeSelf) // move sliders by chunks if we push page up/down
        {
            if (forward)
            {
                MoveCursor(Directions.EAST, 10f);
            }
            else
            {
                MoveCursor(Directions.WEST, 10f);
            }
            return;
        }

        if (!ShopUIScript.CheckShopInterfaceState() 
            && !ItemWorldUIScript.itemWorldInterfaceOpen 
            && !MonsterCorralScript.corralFoodInterfaceOpen)
        {
            return;
        }

        if (ShopUIScript.CheckShopInterfaceState())
        {
            numButtons = ShopUIScript.shopItemButtonList.Length;
            maxItems = ShopUIScript.playerItemList.Count;
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            numButtons = ItemWorldUIScript.itemListButtons.Length;
            maxItems = ItemWorldUIScript.playerItemList.Count;
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            numButtons = MonsterCorralScript.corralFoodButtons.Length;
            maxItems = MonsterCorralScript.playerItemList.Count;
        }

        //Debug.Log("Num buttons: " + numButtons + " Max items: " + maxItems + " List offset " + listArrayIndexOffset);

        if (maxItems <= numButtons) return;

        if (forward)
        {
            listArrayIndexOffset += numButtons;
        }
        else
        {
            listArrayIndexOffset -= numButtons;
        }

        if (listArrayIndexOffset < 0)
        {
            SetListOffset(0);
        }
        if (listArrayIndexOffset >= maxItems - numButtons)
        {
            SetListOffset(maxItems - numButtons);
            //Debug.Log("List offset set to " + (maxItems - numButtons));
        }

        //Debug.Log("List offset is now " + listArrayIndexOffset);

        if (ShopUIScript.CheckShopInterfaceState())
        {
            singletonUIMS.HoverItemInfoConditional(singletonUIMS.GetIndexOfSelectedButton(), true, false);
            ShopUIScript.UpdateShop();
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            ItemWorldUIScript.singleton.ShowItemInfo(singletonUIMS.GetIndexOfSelectedButton());
            UpdateItemWorldList(ItemWorldUIScript.menuState == ItemWorldMenuState.SELECTORB);
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            UpdateMonsterCorralFoodList();
        }
        else if (GetWindowState(UITabs.SKILLS))
        {
            UpdateSkillSheet();
            if (selectedUIObjectGroup == UI_GROUP_ACTIVES) // Actives
            {
                // singletonUIMS.HoverSkillInfo(singletonUIMS.GetIndexOfSelectedButton()); // Was 100+
            }
        }

        if (!forward)
        {
            PlayCursorSound("UITock");
        }
        else
        {
            PlayCursorSound("Tick");
        }
    }

    public static void MouseScroll(int iDelta)
    {
        if ((ShopUIScript.CheckShopInterfaceState()) || (ItemWorldUIScript.itemWorldInterfaceOpen) || (MonsterCorralScript.corralFoodInterfaceOpen))
        {
            if ((uiObjectFocus == ShopUIScript.shopItemSortType) || (uiObjectFocus == ShopUIScript.shopItemSortValue)) return;
            if (UIManagerScript.dialogBoxOpen || GameMasterScript.gmsSingleton.ReadTempGameData("dropitem") >= 0)
            {
                return;
            }
            GlobalTryScrollPool(iDelta);
        }
        else if (jobSheetOpen)
        {
            GlobalTryScrollPool(2 * iDelta);
        }
    }

    // This is done via mouse scrollwheel only.
    public static void GlobalTryScrollPool(int amount)
    {
        int maxUIObjectButtons = 0;
        int maxItemsInList = 0;
        UIObject topOfList = null;

        if (ShopUIScript.CheckShopInterfaceState())
        {
            maxUIObjectButtons = ShopUIScript.shopItemButtonList.Length;
            maxItemsInList = ShopUIScript.playerItemList.Count;
            topOfList = ShopUIScript.shopItemButton1;
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            maxUIObjectButtons = ItemWorldUIScript.itemListButtons.Length;
            maxItemsInList = ItemWorldUIScript.playerItemList.Count;
            topOfList = ItemWorldUIScript.itemListButtons[0];
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            maxUIObjectButtons = MonsterCorralScript.corralFoodButtons.Length;
            maxItemsInList = MonsterCorralScript.playerItemList.Count;
            topOfList = MonsterCorralScript.corralFoodButtons[0];
        }

        else if (jobSheetOpen)
        {
            return;
        }

        if (maxItemsInList <= maxUIObjectButtons) // No need to scroll.
        {
            return;
        }



        PlayCursorSound("Tick");

        if (GetWindowState(UITabs.INVENTORY))
        {
            // Don't offset the list in a weird way.
            if (listArrayIndexOffset + amount >= (maxItemsInList - maxUIObjectButtons + 2))
            {
                return;
            }
        }

        listArrayIndexOffset += amount;

        // Off by one below? Maybe?
        if (listArrayIndexOffset > (maxItemsInList - maxUIObjectButtons))
        {
            SetListOffset(maxItemsInList - maxUIObjectButtons);
        }
        else if (listArrayIndexOffset < 0)
        {
            SetListOffset(0);
        }

        //Debug.Log("After scroll: " + listArrayIndexOffset);

        if (ShopUIScript.CheckShopInterfaceState())
        {
            singletonUIMS.HoverItemInfoConditional(singletonUIMS.GetIndexOfSelectedButton(), true, false); // New code to sync mouse scrolling w/ wheel and displayed item
            // was indexpos - offset
            ShopUIScript.UpdateShop();
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            ItemWorldUIScript.singleton.ShowItemInfo(singletonUIMS.GetIndexOfSelectedButton());
            UpdateItemWorldList(ItemWorldUIScript.menuState == ItemWorldMenuState.SELECTORB);
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            UpdateMonsterCorralFoodList();
        }
        else if (GetWindowState(UITabs.SKILLS))
        {
            UpdateSkillSheet();
            if (selectedUIObjectGroup == UI_GROUP_ACTIVES) // Actives
            {
                // singletonUIMS.HoverSkillInfo(singletonUIMS.GetIndexOfSelectedButton()); // Was 100+
            }

        }
    }

    public static void UpdateMonsterCorralFoodList()
    {
        if (!MonsterCorralScript.corralFoodInterfaceOpen) return;

        bool validTCMSelected = MonsterCorralScript.tcmSelected != null;

        for (int i = 0; i < MonsterCorralScript.corralFoodButtons.Length; i++)
        {
            if (i >= MonsterCorralScript.playerItemList.Count)
            {
                TextMeshProUGUI txt = MonsterCorralScript.corralFoodButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = "";
                MonsterCorralScript.corralFoodButtons[i].gameObj.SetActive(false);
                MonsterCorralScript.corralFoodButtons[i].enabled = false;
            }
            else
            {
                TextMeshProUGUI txt = MonsterCorralScript.corralFoodButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                int offset = i;

                offset += Math.Abs(listArrayIndexOffset);

                if (offset >= MonsterCorralScript.playerItemList.Count)
                {
                    MonsterCorralScript.corralFoodButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    MonsterCorralScript.corralFoodButtons[i].gameObj.SetActive(false);
                    MonsterCorralScript.corralFoodButtons[i].enabled = false;
                    break;
                }

                Item itm = MonsterCorralScript.playerItemList[offset];

                if (itm.actorUniqueID == GameMasterScript.gmsSingleton.ReadTempGameData("feedmonsteritem"))
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("feedmonsterbuttonindex", i);
                }

                txt.text = itm.displayName;

                if (MonsterCorralScript.tcmSelected.knownHateFoods.Contains(MonsterCorralScript.playerItemList[offset].actorRefName))
                {
                    // Mark hated foods red, if we know about them
                    txt.text = UIManagerScript.redHexColor + txt.text + " </color>(" + StringManager.GetString("corral_relationship_negative2") + "!)";
                }
                else if (MonsterCorralScript.tcmSelected.knownLoveFoods.Contains(MonsterCorralScript.playerItemList[offset].actorRefName))
                {
                    // Mark loved foods green!
                    txt.text = UIManagerScript.greenHexColor + txt.text + " </color>(" + StringManager.GetString("corral_relationship_positive3") + "!)";
                }

                if (itm.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable c = itm as Consumable;
                    if (c.Quantity > 1)
                    {
                        txt.text = txt.text + " (" + c.Quantity + ")";
                    }
                }

                MonsterCorralScript.corralFoodButtons[i].gameObj.SetActive(true);
                string invSpriteRef = "";
                if ((itm.spriteRef == null) || (MonsterCorralScript.playerItemList[offset].spriteRef == ""))
                {
                    MonsterCorralScript.corralFoodButtons[i].subObjectImage.sprite = null;
                    MonsterCorralScript.corralFoodButtons[i].subObjectImage.color = transparentColor;
                }
                else
                {
                    invSpriteRef = itm.spriteRef;
                    MonsterCorralScript.corralFoodButtons[i].subObjectImage.sprite = LoadSpriteFromDict(dictItemGraphics, invSpriteRef);
                    MonsterCorralScript.corralFoodButtons[i].subObjectImage.color = Color.white;
                }
                MonsterCorralScript.corralFoodButtons[i].enabled = true;
            }
        }

        UpdateScrollbarPosition();
    }

    public static void UpdateItemWorldList(bool selectOrb)
    {
        if (!ItemWorldUIScript.itemWorldInterfaceOpen) return;

        for (int i = 0; i < ItemWorldUIScript.itemListButtons.Length; i++)
        {
            if (i >= ItemWorldUIScript.playerItemList.Count)
            {
                TextMeshProUGUI txt = ItemWorldUIScript.itemListButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = "";
                ItemWorldUIScript.itemListButtons[i].gameObj.SetActive(false);
                ItemWorldUIScript.itemListButtons[i].enabled = false;
            }
            else
            {
                TextMeshProUGUI txt = ItemWorldUIScript.itemListButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                int offset = i;

                offset += Math.Abs(listArrayIndexOffset);

                if (offset >= ItemWorldUIScript.playerItemList.Count)
                {
                    ItemWorldUIScript.itemListButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    ItemWorldUIScript.itemListButtons[i].gameObj.SetActive(false);
                    ItemWorldUIScript.itemListButtons[i].enabled = false;
                    break;
                }

                bool colorRed = false;

                if (selectOrb)
                {
                    Equipment eq = ItemWorldUIScript.itemSelected as Equipment;

                    Consumable orb = ItemWorldUIScript.playerItemList[offset] as Consumable;
                    if (ItemWorldUIScript.IsItemCompatibleWithOrb(orb) != MagicModCompatibility.POSSIBLE)
                    {
                        // Show incompatibility to player
                        txt.text = UIManagerScript.redHexColor + ItemWorldUIScript.playerItemList[offset].strippedName + "</color>";
                        colorRed = true;
                    }
                    else
                    {
                        txt.text = ItemWorldUIScript.playerItemList[offset].displayName;
                    }
                }
                else
                {
                    txt.text = ItemWorldUIScript.playerItemList[offset].displayName;

                }

                if (ItemWorldUIScript.playerItemList[offset].IsEquipment())
                {
                    if (GameMasterScript.heroPCActor.myEquipment.IsEquipped(ItemWorldUIScript.playerItemList[offset]))
                    {
                        txt.text = "[E] " + ItemWorldUIScript.playerItemList[offset].displayName;
                    }
                }
                else
                {
                    // We must be selecting orb.
                    int qty = ItemWorldUIScript.playerItemList[offset].GetQuantity();
                    if (qty > 1)
                    {
                        if (colorRed)
                        {
                            txt.text = UIManagerScript.redHexColor + ItemWorldUIScript.playerItemList[offset].strippedName + "</color> (" + qty + ")";
                        }
                        else
                        {
                            txt.text = ItemWorldUIScript.playerItemList[offset].displayName + " (" + qty + ")";
                        }

                    }
                }

                txt.text = CustomAlgorithms.CheckForFavoriteOrTrashAndInsertMark(txt.text, ItemWorldUIScript.playerItemList[offset]);

                ItemWorldUIScript.itemListButtons[i].gameObj.SetActive(true);
                string invSpriteRef = "";
                if ((ItemWorldUIScript.playerItemList[offset].spriteRef == null) || (ItemWorldUIScript.playerItemList[offset].spriteRef == ""))
                {
                    ItemWorldUIScript.itemListButtons[i].subObjectImage.sprite = null;
                    ItemWorldUIScript.itemListButtons[i].subObjectImage.color = transparentColor;
                }
                else
                {
                    invSpriteRef = ItemWorldUIScript.playerItemList[offset].spriteRef;
                    ItemWorldUIScript.itemListButtons[i].subObjectImage.sprite = LoadSpriteFromDict(dictItemGraphics, invSpriteRef);
                    ItemWorldUIScript.itemListButtons[i].subObjectImage.color = Color.white;
                }
                ItemWorldUIScript.itemListButtons[i].enabled = true;
            }
        }

        UpdateScrollbarPosition();
    }

    public static void SetListOffset(int amount, Switch_UIEquipmentScreen eqScreen = null)
    {
        listArrayIndexOffset = amount;
        if (eqScreen != null)
        {
            eqScreen.itemColumn.iOffsetFromTopOfList = amount;
        }
    }

    public static void SetJobAbilityListPosition(int pos)
    {
        //listArrayIndexPosition = pos; // Was job specific before?
        ChangeUIFocusAndAlignCursor(jobAbilButtons[pos]); // Will this work?
        UpdateJobSheetCursorPos();
    }

    public void AdjustListOffsetFromScrollbar()
    {
        if (ShopUIScript.CheckShopInterfaceState())
        {
            SetListOffset((int)shopScrollbar.GetComponent<Slider>().value);
            ShopUIScript.UpdateShop();
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            SetListOffset((int)itemWorldScrollbar.GetComponent<Slider>().value);
            UpdateItemWorldList(ItemWorldUIScript.menuState == ItemWorldMenuState.SELECTORB);
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            SetListOffset((int)corralFoodScrollbar.GetComponent<Slider>().value);
            UpdateMonsterCorralFoodList();
        }
        else if (GetWindowState(UITabs.SKILLS))
        {
            if (selectedUIObjectGroup == UI_GROUP_PASSIVES)
            {
                SetListOffset((int)skillSupportScrollbar.GetComponent<Slider>().value);
            }
            else
            {
                SetListOffset((int)skillActiveScrollbar.GetComponent<Slider>().value);
            }

            UpdateSkillSheet();
        }
    }

    public static void UpdateScrollbarPosition()
    {

        if (ShopUIScript.CheckShopInterfaceState())
        {
            if (ShopUIScript.playerItemList.Count < ShopUIScript.shopItemButtonList.Length)
            {
                shopScrollbar.SetActive(false);
                return;
            }
            shopScrollbar.SetActive(true);
            SetMaxScrollbarValue(shopScrollbar, (ShopUIScript.playerItemList.Count - ShopUIScript.shopItemButtonList.Length));
            shopScrollbar.GetComponent<Slider>().value = listArrayIndexOffset;
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            if (ItemWorldUIScript.playerItemList.Count < ItemWorldUIScript.itemListButtons.Length)
            {
                itemWorldScrollbar.SetActive(false);
                return;
            }
            itemWorldScrollbar.SetActive(true);
            SetMaxScrollbarValue(itemWorldScrollbar, (ItemWorldUIScript.playerItemList.Count - ItemWorldUIScript.itemListButtons.Length));
            itemWorldScrollbar.GetComponent<Slider>().value = (listArrayIndexOffset);
        }
        else if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            if (MonsterCorralScript.playerItemList.Count < MonsterCorralScript.corralFoodButtons.Length)
            {
                corralFoodScrollbar.SetActive(false);
                return;
            }
            corralFoodScrollbar.SetActive(true);
            SetMaxScrollbarValue(corralFoodScrollbar, (MonsterCorralScript.playerItemList.Count - MonsterCorralScript.corralFoodButtons.Length));
            corralFoodScrollbar.GetComponent<Slider>().value = (listArrayIndexOffset);
        }
    }

    public static void SetMaxScrollbarValue(GameObject scrollbar, int max)
    {
        scrollbar.GetComponent<Slider>().maxValue = max;
        scrollbar.GetComponent<Slider>().value = 0;
    }

    public static void TryScrollUITextBox(float amount)
    {
        if (GetWindowState(UITabs.RUMORS))
        {
            if (JournalScript.journalState == JournalTabs.COMBATLOG)
            {
                float scrollbarValue = JournalScript.singleton.combatLogScrollbar.value;
                scrollbarValue += amount;
                scrollbarValue = Mathf.Clamp(scrollbarValue, 0f, 1f);
                JournalScript.singleton.combatLogScrollbar.value = scrollbarValue;
            }
        }
    }
}
