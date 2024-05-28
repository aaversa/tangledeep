using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public partial class UIManagerScript
{

    protected int GetFilterIndexForViewAll()
    {
        return currentFullScreenUI != null ? currentFullScreenUI.GetFilterIndexForViewAll() : (int)ItemFilters.VIEWALL;
    }

    protected int GetFilterIndexForFavorites()
    {
        return currentFullScreenUI != null ? currentFullScreenUI.GetFilterIndexForFavorites() : (int)ItemFilters.FAVORITES;
    }

    public void ToggleFilterType(int[] iType, bool bRadioStyle = false)
    {
        ToggleFilterType(iType[0], bRadioStyle);
    }

    public void ToggleFilterType(int idxFilter, bool bRadioStyle = false)
    {
        if (bRadioStyle && idxFilter != GetFilterIndexForFavorites())
        {
            ToggleFilterTypeRadioStyle(idxFilter);
            return;
        }

        bool bToggleToThis = !itemFilterTypes[idxFilter];

        //don't toggle ViewAll off, or everything would vanish
        if (idxFilter == GetFilterIndexForViewAll())
        {
            bToggleToThis = true;
        }

        //but otherwise, if we're not clicking ViewAll or Favorites, turn OFF ViewAll
        else if (idxFilter != GetFilterIndexForFavorites())
        {
            itemFilterTypes[GetFilterIndexForViewAll()] = false;
        }

        //toggle the filter on/off
        itemFilterTypes[idxFilter] = bToggleToThis;

        //if ViewAll is now true, then the other filters (besides Favorites) should be off.
        if (itemFilterTypes[GetFilterIndexForViewAll()])
        {
            for (int i = 0; i < itemFilterTypes.Length; i++)
            {
                if (i == GetFilterIndexForFavorites() ||
                    i == GetFilterIndexForViewAll())
                {
                    continue;
                }
                itemFilterTypes[i] = false;
            }
        }
    }

    //Turns a filter ON, and if it was not on, turns off all the other filters, except for Favorites.
    public void ToggleFilterTypeRadioStyle(int idxFilter)
    {
        for (int i = 0; i < itemFilterTypes.Length; i++)
        {
            if (i == GetFilterIndexForFavorites())
            {
                continue;
            }

            itemFilterTypes[i] = (i == idxFilter);
        }
    }

    void ClearItemTypeFilters()
    {
        //Debug.Log("Clearing all filters.");
        int maxExclusiveCategories = 0;
        if (GetWindowState(UITabs.EQUIPMENT))
        {
            maxExclusiveCategories = 4;
            itemFilterTypes[(int)GearFilters.VIEWALL] = false;
        }
        else if (GetWindowState(UITabs.INVENTORY))
        {
            maxExclusiveCategories = 5;
            itemFilterTypes[(int)ItemFilters.VIEWALL] = false;
        }

        for (int i = 0; i < maxExclusiveCategories; i++) // was < itemtypefilters..
        {
            itemFilterTypes[i] = false;
        }
    }

    void UpdateItemTypeFilters(bool refreshSheet)
    {
        //#TODO: Catch and Replace for Switch Inventory screen

        if (GetWindowState(UITabs.EQUIPMENT))
        {
            if (itemFilterTypes[(int)GearFilters.VIEWALL])
            {
                eqFilterViewAll.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_17");
            }
            else
            {
                eqFilterViewAll.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_8");
            }

            if (itemFilterTypes[(int)GearFilters.WEAPON])
            {
                eqFilterWeapons.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_9");
            }
            else
            {
                eqFilterWeapons.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_0");
            }

            if (itemFilterTypes[(int)GearFilters.OFFHAND])
            {
                eqFilterOffhand.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_10");
            }
            else
            {
                eqFilterOffhand.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_1");
            }
            if (itemFilterTypes[(int)GearFilters.ARMOR])
            {
                eqFilterArmor.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_12");
            }
            else
            {
                eqFilterArmor.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_3");
            }
            if (itemFilterTypes[(int)GearFilters.ACCESSORY])
            {
                eqFilterAccessory.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_13");
            }
            else
            {
                eqFilterAccessory.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_4");
            }

            if (itemFilterTypes[(int)GearFilters.MAGICAL])
            {
                // Selected
                eqFilterMagical.gameObj.GetComponent<Image>().sprite = pressedGreyButton250x50;
                FontManager.LocalizeMe(eqFilterMagicalText, TDFonts.WHITE_NO_OUTLINE);
                //eqFilterMagical.gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(eqFilterButtons, "buttons-spritesheet_11");
            }
            else
            {
                // Not selected
                eqFilterMagical.gameObj.GetComponent<Image>().sprite = greyButton250x50;
                FontManager.LocalizeMe(eqFilterMagicalText, TDFonts.BLACK);
            }

            if (itemFilterTypes[(int)GearFilters.COMMON])
            {
                eqFilterCommon.gameObj.GetComponent<Image>().sprite = pressedGreyButton250x50;
                FontManager.LocalizeMe(eqFilterCommonText, TDFonts.WHITE_NO_OUTLINE);
            }
            else
            {
                eqFilterCommon.gameObj.GetComponent<Image>().sprite = greyButton250x50;
                FontManager.LocalizeMe(eqFilterCommonText, TDFonts.BLACK);
            }

            if (itemFilterTypes[(int)GearFilters.LEGENDARY])
            {
                eqFilterLegendary.gameObj.GetComponent<Image>().sprite = pressedGreyButton250x50;
                FontManager.LocalizeMe(eqFilterLegendaryText, TDFonts.WHITE_NO_OUTLINE);
            }
            else
            {
                eqFilterLegendary.gameObj.GetComponent<Image>().sprite = greyButton250x50;
                FontManager.LocalizeMe(eqFilterLegendaryText, TDFonts.BLACK);
            }
            if (itemFilterTypes[(int)GearFilters.GEARSET])
            {
                eqFilterGearSet.gameObj.GetComponent<Image>().sprite = pressedGreyButton250x50;
                FontManager.LocalizeMe(eqFilterGearSetText, TDFonts.WHITE_NO_OUTLINE);
            }
            else
            {
                eqFilterGearSet.gameObj.GetComponent<Image>().sprite = greyButton250x50;
                FontManager.LocalizeMe(eqFilterGearSetText, TDFonts.BLACK);
            }

        }
        else if (GetWindowState(UITabs.INVENTORY))
        {
            for (int i = 0; i < invFilterObjects.Count; i++)
            {
                if (itemFilterTypes[invFilterObjects[i].onSubmitValue])
                {
                    invFilterObjects[i].gameObj.GetComponent<Image>().sprite = pressedGreyButton250x50;
                    FontManager.LocalizeMe(invFilterObjects[i].gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE_NO_OUTLINE);
                }
                else
                {
                    invFilterObjects[i].gameObj.GetComponent<Image>().sprite = greyButton250x50;
                    FontManager.LocalizeMe(invFilterObjects[i].gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
                }
            }
        }
    }

    public void SortPlayerAbilities(int sortType)
    {
        List<AbilityScript> skills = GameMasterScript.heroPCActor.myAbilities.GetAbilityList();

        if (skills.Count < 2) return;

        List<AbilityScript> skillsToSort = new List<AbilityScript>();

        switch ((AbilitySortTypes)sortType)
        {
            case AbilitySortTypes.ALPHA:
            case AbilitySortTypes.JOB:
                foreach (AbilityScript abil in skills)
                {
                    if (!abil.passiveAbility)
                    {
                        skillsToSort.Add(abil);
                    }
                }
                break;
            case AbilitySortTypes.PASSIVEALPHA:
            case AbilitySortTypes.PASSIVEEQUIPPED:
                foreach (AbilityScript abil in skills)
                {
                    if (abil.passiveAbility)
                    {
                        skillsToSort.Add(abil);
                    }
                }
                break;
        }

        if (skillsToSort.Count <= 2)
        {
            return;
        }

        foreach (AbilityScript abil in skillsToSort)
        {
            skills.Remove(abil);
        }

        GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Organize");
        switch ((AbilitySortTypes)sortType)
        {
            case AbilitySortTypes.ALPHA:
            case AbilitySortTypes.PASSIVEALPHA:
                for (int i = 1; i < skillsToSort.Count; i++)
                {
                    AbilityScript x = skillsToSort[i];
                    int j = i - 1;
                    while ((j >= 0) && (String.Compare(skillsToSort[j].abilityName, x.abilityName) > 0))
                    {
                        skillsToSort[j + 1] = skillsToSort[j];
                        j -= 1;
                    }
                    skillsToSort[j + 1] = x;
                }
                break;
            case AbilitySortTypes.JOB:
                for (int i = 1; i < skillsToSort.Count; i++)
                {
                    AbilityScript x = skillsToSort[i];
                    int j = i - 1;
                    while ((j >= 0) && ((int)skillsToSort[j].jobLearnedFrom > (int)x.jobLearnedFrom))
                    {
                        skillsToSort[j + 1] = skillsToSort[j];
                        j -= 1;
                    }
                    skillsToSort[j + 1] = x;
                }
                break;
            case AbilitySortTypes.PASSIVEEQUIPPED:
                for (int i = 1; i < skillsToSort.Count; i++)
                {
                    AbilityScript x = skillsToSort[i];

                    int j = i - 1;
                    while ((j >= 0) && (skillsToSort[j].GetEquipSortValue()) < x.GetEquipSortValue())
                    {
                        skillsToSort[j + 1] = skillsToSort[j];
                        j -= 1;
                    }
                    skillsToSort[j + 1] = x;
                }
                break;
        }

        foreach (AbilityScript abil in skillsToSort)
        {
            skills.Add(abil);
        }
        UpdateSkillSheet();
    }

    public void SortPlayerInventory_UICallback(int sortType)
    {
        if (UIManagerScript.dialogBoxOpen) return; // Adjust quantity dialog is probably open.
        SortPlayerInventory(sortType);
    }

    public void SortPlayerInventory(int sortType, bool bSortSilentWithoutFlipping = false)
    {
        //When we first open a menu, we may want to sort based on a new filterstate -- like the snackbag.
        //If so, we don't want to play a sound or reverse the order every time we open.
        if (!bSortSilentWithoutFlipping)
        {
            GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Organize");

            if ((int)invSortItemType == sortType)
            {
                // Reverse direction
                sortForward[(int)invSortItemType] = !sortForward[(int)invSortItemType];
            }
            else
            {
                sortForward[(int)invSortItemType] = true;
            }
        }
        else
        {
            sortForward[(int)invSortItemType] = true;
        }

        invSortItemType = (InventorySortTypes)sortType;
        if (!Switch_UIInventoryScreen.openingSpecialSortBag)
        {
            Switch_UIInventoryScreen.lastSortType = invSortItemType;
            Switch_UIInventoryScreen.lastSortForward = sortForward[(int)invSortItemType];
        }

        List<Item> invToSort = GameMasterScript.heroPCActor.myInventory.GetInventory();

        if (ShopUIScript.CheckShopInterfaceState())
        {
            if (ShopUIScript.shopState == ShopState.SELL)
            {
                invToSort = GameMasterScript.heroPCActor.myInventory.GetInventory();
            }
            else
            {
                invToSort = currentConversation.whichNPC.myInventory.GetInventory();
            }
            // Always alpha sort shops first.
        }

        //Debug.Log("Sorting the inventory");
        try { InventoryScript.SortAnyInventory(invToSort, sortType, sortForward[(int)invSortItemType]); }
        catch (Exception e)
        {
            Debug.Log("SORT ERROR: " + e);
        }

        if (currentFullScreenUI != null)
        {
            currentFullScreenUI.UpdateContent();
        }

        if (ShopUIScript.CheckShopInterfaceState()) ShopUIScript.UpdateShop();
    }

    // This is called when using keyboard to hover over an inventory slot.
    public void FilterItemsByType(int type)
    {
        if (swappingItems) return;
        if (eqSubmenuOpen) return;
        HideEQTooltipContainer();

        SetListOffset(0);

        invFilterItemType = (ItemTypes)type;
        if (type == (int)ItemTypes.WEAPON)
        {
            eqSlotSelected = EquipmentSlots.WEAPON;
        }

        ClearItemTypeFilters();

        ShowItemInfo(type);

        switch (type)
        {
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1:
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT2:
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT3:
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4:
                ShowItemInfo(type);
                itemFilterTypes[(int)ItemTypes.WEAPON] = true;
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_ARMOR_SLOT:
                itemFilterTypes[(int)ItemTypes.ARMOR] = true;
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT:
                itemFilterTypes[(int)ItemTypes.WEAPON] = true;
                itemFilterTypes[(int)ItemTypes.OFFHAND] = true;
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1:
            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2:
                itemFilterTypes[(int)ItemTypes.ACCESSORY] = true;
                break;
        }

        UpdateItemTypeFilters(true);

    }
}
