using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Switch_UIInventoryScreen : ImpactUI_WithItemColumn
{
    [Header("Top row: Categories")]
    public Switch_InvItemButton btnFirstButton_Categories;

    [Header("Right Top row: Filter")]
    public Switch_InvItemButton btnFirstButton_Filter;
    public int PixelsBetweenButtons_Filter;
    public List<ItemFilters> list_filterTypes;
    private List<Switch_InvItemButton> filterButtons;

    [Header("Right Bottom row: Sort")]
    public Switch_InvItemButton btnFirstButton_Sort;
    public int PixelsBetweenButtons_Sort;
    public List<InventorySortTypes> list_sortTypes;
    private List<Switch_InvItemButton> sortButtons;

    [Header("Tooltip Display")]
    public Image            image_SelectItem_Switch;
    public TextMeshProUGUI  text_SelectItem_Switch;

    [Header("Vertical Hotbar Buttons")]
    public GameObject hotbar_vertical;
    public int NumVerticalHotbarButtons;
    public int PixelsBetweenButtons_Hotbar;
    public Switch_InvItemButton btnSwapButton;
    private List<Switch_InvVerticalHotbarButton> listVerticalHotbarButtons;

    [Header("Searchbar")]
    public TMP_InputField invSearchbar;

    //for interfacing with other game code
    [HideInInspector]
    public UIManagerScript.UIObject[] invItemButtons;

    //Keep track of the last item we used.
    private string strLastSelectedItemRef;

    // When opening food bag, we want to store previous state of the item list, which may not be a simple sort
    // And then we want to be able to return to it afterward
    List<Item> backupInventorySort;
    static bool lastSortWasSpecialSortBag;
    public static bool openingSpecialSortBag;
    public static InventorySortTypes lastSortType;
    public static bool lastSortForward;

    public override void Awake()
    {
        base.Awake();
        myTabType = UITabs.INVENTORY;
    }

    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        CreateButtons();

        ConnectButtons();

        listVerticalHotbarButtons[listVerticalHotbarButtons.Count - 1].myUIObject.neighbors[(int)Directions.SOUTH] = btnSwapButton.myUIObject;
        btnSwapButton.myUIObject.neighbors[(int)Directions.NORTH] = listVerticalHotbarButtons[listVerticalHotbarButtons.Count - 1].myUIObject;

        bHasBeenInitialized = true;
        return true;
    }

 
    //Builds and positions the navigation buttons based on the filter enums they use
    void CreateButtons()
    {
        if (bHasBeenInitialized)
        {
            return;
        }

        filterButtons = CreateListOfButtons(btnFirstButton_Filter, list_filterTypes, new Vector2(0, -1), PixelsBetweenButtons_Filter, OnSubmit_FilterButton);
        sortButtons = CreateListOfButtons(btnFirstButton_Sort, list_sortTypes, new Vector2(0, -1), PixelsBetweenButtons_Sort, OnSubmit_SortButton);

        filterButtons[0].GetTMPro().text = StringManager.GetString("item_filters_view_all");
        filterButtons[1].GetTMPro().text = StringManager.GetString("item_filters_recovery");
        filterButtons[2].GetTMPro().text = StringManager.GetString("item_filters_self_buff");
        filterButtons[3].GetTMPro().text = StringManager.GetString("item_filters_offense");
        filterButtons[4].GetTMPro().text = StringManager.GetString("item_filters_summon");
        filterButtons[5].GetTMPro().text = StringManager.GetString("item_filters_utility");
        filterButtons[6].GetTMPro().text = StringManager.GetString("item_filters_valuables");
        filterButtons[7].GetTMPro().text = StringManager.GetString("item_filters_favorites");

        sortButtons[0].GetTMPro().text = StringManager.GetString("item_sort_type_type");
        sortButtons[1].GetTMPro().text = StringManager.GetString("item_sort_type_alpha");
        sortButtons[2].GetTMPro().text = StringManager.GetString("item_sort_type_value");
        sortButtons[3].GetTMPro().text = StringManager.GetString("item_sort_type_rank");
        sortButtons[4].GetTMPro().text = StringManager.GetString("item_sort_type_rarity");

        //Vertical side bar full of hotbar buttons
        listVerticalHotbarButtons = CreateVerticalHotbar(hotbar_vertical, NumVerticalHotbarButtons, PixelsBetweenButtons_Hotbar, btnSwapButton, SwapHotbarViaGamepad);        
    }

    //Our buttons should go to the submenu when clicked or selected,
    //and drag like normal
    protected override void SetActionsForContainerButton(Switch_InvItemButton btn, int[] iAdditionalParameters)
    {
        base.SetActionsForContainerButton(btn, iAdditionalParameters);
        btn.onClickAction = OpenTooltipSubmenu;
        btn.onPointerEnter = SetTooltipViaButtonByID;
    }


    //Handles the submenu for inventory needs, which are different from equipment needs!
    public override void OpenTooltipSubmenu(int[] args, Switch_InvItemButton.ELastInputSource inputSource )
    {
        //Hey wait! 
        int iButtonIndex = args[0];
        if (CheckForFakeyDrop())
        {
            return;
        }

        if (genericTooltip == null)
        {
            //weird
            return;
        }

        // AA - If the tooltip is INACTIVE it might not be null, but there still shouldn't be any item info displayed
        if (!genericTooltip.CheckIfActive())
        {
            return;
        }

        Item targetItem = genericTooltip.GetTargetItem();
        if (targetItem == null)
        {
            return;
        }

        genericTooltip.ClearSubmenu();
        int itemID = targetItem.actorUniqueID;

        /*
         *  Very simple:
         *      * Use if usable
         *      * Drop
         *      * Mark As Favorite
         *      * If Use, also Hotbar
         * 
         *  BUT
         *  if it is a food
         *      * EAT instead of USE
         *  but if you are too full
         *      * EAT is greyed out
         * 
         */

        bool bUsable = targetItem.CanBeUsed();

        if (targetItem.IsItemFood())
        {
            bool playerIsFull = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");
            Switch_InvItemButton eatButton = genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_eat"), genericTooltip.EatItem, new int[] { itemID });
            if (playerIsFull)
            {
                eatButton.ToggleButton(true);
            }
        }
        else if (bUsable)
        {
            genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_use"), genericTooltip.UseItem, new int[] { itemID });
        }

        genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_drop"), genericTooltip.DropItem, new int[] { itemID });
        string strFav = StringManager.GetString(targetItem.favorite ? "ui_button_unfavorite" : "ui_button_favorite");
        genericTooltip.AddDynamicButton(strFav, genericTooltip.MarkItemFavorite, new int[] { itemID, iButtonIndex });

        string strTrash = StringManager.GetString(targetItem.vendorTrash ? "ui_button_nottrash" : "ui_button_trash");
        genericTooltip.AddDynamicButton(strTrash, genericTooltip.MarkItemTrash, new int[] { itemID, iButtonIndex });

        if (bUsable)
        {
            genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_hotbar"), genericTooltip.EnterItemHotbarMode, new int[] { itemID });
        }

        genericTooltip.ShowSubmenu();
        genericTooltip.SetActionCloseSubmenu(SubmenuClosed);
        CursorOwnershipState = EImpactUICursorOwnershipState.tooltip_has_cursor;
    }

    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
        ISelectableUIObject obj = itemColumn.GetContainedDataFrom(iButtonIndex);
        StartAssignObjectToHotbarMode(obj);
        
    }

    public override void StartAssignObjectToHotbarMode(ISelectableUIObject obj)
    {
        listVerticalHotbarButtons[0].SetFocusOnMeForHotbarSlotting();
        CursorOwnershipState = EImpactUICursorOwnershipState.vertical_hotbar_has_cursor;
        UIManagerScript.SetHeldGenericObject(obj);
    }

    //return TRUE if the item matches any of the active filters
    protected override bool AllowItemInItemListSpecialConditions(Item itm)
    {
        return UIManagerScript.allPossibleInventoryFilters.Any(iFilter => UIManagerScript.itemFilterTypes[(int)iFilter] && itm.CheckTag((int)iFilter));
    }

    //Take the lists of buttons and connect them to each other
    void ConnectButtons()
    {
        if (bHasBeenInitialized)
        {
            return;
        }

        //MakeListOfNeighbors(categoryButtons, Directions.WEST, Directions.EAST);

        

        MakeListOfNeighbors(filterButtons, Directions.NORTH, Directions.SOUTH);
        MakeListOfNeighbors(sortButtons, Directions.NORTH, Directions.SOUTH);

        // In german, reduce character spacing for filter/sort buttons.
        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            foreach (Switch_InvItemButton btn in filterButtons)
            {
                btn.GetTMPro().characterSpacing = 0;
            }
            foreach (Switch_InvItemButton btn in sortButtons)
            {
                btn.GetTMPro().characterSpacing = 0;
            }
        }

        //MakeListOfNeighbors(itemButtons, Directions.NORTH, Directions.SOUTH);

        //Any itemButton EAST needs to go to filters
        itemColumn.neighborRight = filterButtons[0].myUIObject;

        //WEST is the hotbar 
        itemColumn.neighborLeft = listVerticalHotbarButtons[0].myUIObject;

        //any filter WEST needs to go items
        ConnectButtonsInListToOtherButton(filterButtons, Directions.WEST, itemColumn.GetTopUIObject());

        //any sort WEST needs to go items
        ConnectButtonsInListToOtherButton(sortButtons, Directions.WEST, itemColumn.GetTopUIObject());

        //bottom filter must connect to first sort
        filterButtons[filterButtons.Count - 1].myUIObject.neighbors[(int) Directions.SOUTH] = sortButtons[0].myUIObject;
        sortButtons[0].myUIObject.neighbors[(int)Directions.NORTH] = filterButtons[filterButtons.Count - 1].myUIObject;

        foreach (Switch_InvVerticalHotbarButton vbtn in listVerticalHotbarButtons)
        {
            UIManagerScript.UIObject obj = vbtn.myUIObject;
            obj.directionalActions[(int) Directions.EAST] = LeaveVerticalHotbarViaGamepad;
            obj.myOnSelectAction = vbtn.OnSelectAction_FocusOnMe;
        }
    }

    protected override void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        base.AddAllUIObjects(allObjects);
        AddUIObjectsFromListOfButtons(filterButtons, allObjects);
        AddUIObjectsFromListOfButtons(sortButtons, allObjects);
        allObjects.Add(btnSwapButton.myUIObject);
    }

    public override bool AssignObjectToHotbarViaKeypress(ISelectableUIObject obj, int iHotbarIdx)
    {
        Switch_InvVerticalHotbarButton buttonTarget = listVerticalHotbarButtons[iHotbarIdx];
        buttonTarget.SetContentAndAddToHotbar(obj);
        UpdateContent();
        return true;
    }

    public override void UpdateContent(bool adjustSorting = true)
    {
        //Make all the pretty side buttons on/off correctly
        MaintainRadioButtonState(filterButtons, UIManagerScript.itemFilterTypes);

        //Debug.Log(lastSortWasSpecialSortBag + " " + openingSpecialSortBag + " " + UIManagerScript.messageForFullScreenUI);

        if (lastSortWasSpecialSortBag && UIManagerScript.messageForFullScreenUI != UIManagerScript.EMessageForFullScreenUI.inventory_as_snack_bag)
        {
            lastSortWasSpecialSortBag = false;
            if (adjustSorting)
            {
                InventoryScript.SortAnyInventory(GameMasterScript.heroPCActor.myInventory.GetInventory(), (int)lastSortType, lastSortForward);
            }            
        }
        if (openingSpecialSortBag)
        {
            openingSpecialSortBag = false;
            lastSortWasSpecialSortBag = true;
        }

        // itemButtons == our list of buttons on screen to display items
        List<ISelectableUIObject> playerItemList = CreateItemListFromInventoryOfThisTypeThatMatchesFilters<Consumable>(GameMasterScript.heroPCActor.myInventory);

        //Now we have a list of all pertinent items!
        itemColumn.PlaceObjectsInList(playerItemList, false);

        //Change the button display names based on quantity or hunger
        itemColumn.AdjustButtonInformationViaAction(Action_UpdateContainedItemName);
        
        //Update our tooltip if we have a selectedItem and we're not in the submenu
        if (CursorOwnershipState == EImpactUICursorOwnershipState.normal)
        {
            UIManagerScript.UIObject currentFocus = UIManagerScript.uiObjectFocus;

            if (currentFocus != null && currentFocus.gameObj != null )
            {
                Switch_InvItemButton focusButton = currentFocus.gameObj.GetComponent<Switch_InvItemButton>();
                if (focusButton != null)
                {
                    Item focusItem = focusButton.GetContainedData() as Item;
                    if (focusItem != null)
                    {
                        DisplayItemInfo(focusItem, null, false);
                    }
                }
            }
        }

        //Update the contents of our hottest hotbar
        foreach (Switch_InvVerticalHotbarButton vb in listVerticalHotbarButtons)
        {
            vb.UpdateInformation(true);
        }


    }

    //This override keeps track of the last item we looked at so when we re-open the inventory menu we can
    //hop back to it.
    public override void DisplayItemInfo(ISelectableUIObject itemToDisplay, GameObject refObject, bool mouseHoverSource)
    {
        Item itam = itemToDisplay as Item;
        if (itam == null)
        {
            UIGenericItemTooltip tooltip = GetTooltipIfActive() ?? UIManagerScript.ShowGenericItemTooltip();
            AbilityScript abil = itemToDisplay as AbilityScript;
            if (abil != null)
            {
                tooltip.imageItemSprite.sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictUIGraphics, abil.iconSprite);
                tooltip.textItemName.text = abil.abilityName;
                tooltip.textItemInfo.text = abil.GetAbilityInformation();
            }
            return;

        }
        strLastSelectedItemRef = itam.actorRefName;
        base.DisplayItemInfo(itemToDisplay, refObject, mouseHoverSource);
    }

    //Change color of food we can't eat, and add (X) to multi-quantity stacks
    public void Action_UpdateContainedItemName(Switch_InvItemButton btn, int[] iArray, string[] sArray )
    {
        if (btn == null ||
            btn.gameObject.activeSelf == false)
        {
            return;
        }

        Item itemFromInventory = btn.GetContainedData() as Item;
        if (itemFromInventory == null)
        {
            return;
        }

        //Check this bool because it will affect how we display our food
        bool playerIsFull = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");

        //However! We might change the color of the name of the item
        //int the button, or add other information as well.
        string strDisplayName = itemFromInventory.displayName;

        //if it is food, and we are full, make the item red
        if (itemFromInventory.IsItemFood() && playerIsFull)
        {
            strDisplayName = UIManagerScript.redHexColor + strDisplayName + "</color>";
        }

        if (itemFromInventory.newlyPickedUp)
        {
            strDisplayName = "<color=orange>[" + StringManager.GetString("ui_misc_new") + "]</color> " + strDisplayName;            
        }

        strDisplayName = CustomAlgorithms.CheckForFavoriteOrTrashAndInsertMark(strDisplayName, itemFromInventory);

        //If we have more than one, add (#) to the end
        strDisplayName += itemFromInventory.GetQuantityText();

        //hooray
        btn.SetDisplayText(strDisplayName);

    }



    public override bool AllowFocus(GameObject obj)
    {
        if (!base.AllowFocus(obj))
        {
            return false;
        }

        switch (CursorOwnershipState)
        {
            case EImpactUICursorOwnershipState.normal:
                return true;
            case EImpactUICursorOwnershipState.tooltip_has_cursor:
                return UIManagerScript.ObjectInTooltip(obj);
            case EImpactUICursorOwnershipState.vertical_hotbar_has_cursor:
                return obj.GetComponent<Switch_InvVerticalHotbarButton>() != null;
        }

        //?
        return true;
    }

    //Change our view to one of beautiful delicious snacking.
    void SetFilterForSnackStatus()
    {
        InventoryUIState foodBasedFiltering = new InventoryUIState(false);
        foodBasedFiltering.indexOfSelectedItem = 0;
        foodBasedFiltering.listIndexOffset = 0;

        foodBasedFiltering.filterStates[(int)ItemFilters.RECOVERY] = true;
        foodBasedFiltering.filterStates[(int)ItemFilters.FAVORITES] = false;
        foodBasedFiltering.filterStates[(int)ItemFilters.VIEWALL] = false;

        openingSpecialSortBag = true;

        foodBasedFiltering.sortType = InventorySortTypes.RARITY;

        SetFiltersViaStateAndSortInventory(foodBasedFiltering);
    }

    //Change our view to one of hateful foomtossing
    void SetFilterForDamageConsumables()
    {
        InventoryUIState hateBasedFiltering = new InventoryUIState(false);
        hateBasedFiltering.indexOfSelectedItem = 0;
        hateBasedFiltering.listIndexOffset = 0;

        hateBasedFiltering.filterStates[(int)ItemFilters.OFFENSE] = true;
        hateBasedFiltering.filterStates[(int)ItemFilters.FAVORITES] = false;
        hateBasedFiltering.filterStates[(int)ItemFilters.VIEWALL] = false;
        openingSpecialSortBag = true;

        hateBasedFiltering.sortType = InventorySortTypes.RARITY;

        SetFiltersViaStateAndSortInventory(hateBasedFiltering);
    }

    public override void TurnOn()
    {
        base.TurnOn();

        TDSearchbar.ClearSearchTerms();

        UIManagerScript.allUIObjects.Clear();
        AddAllUIObjects(UIManagerScript.allUIObjects);

        //if (Debug.isDebugBuild) Debug.Log("Inventory opening. Message is: " + UIManagerScript.messageForFullScreenUI);

        //are we in snackbag mode?
        switch (UIManagerScript.messageForFullScreenUI)
        {
            case UIManagerScript.EMessageForFullScreenUI.none:
                break;
            case UIManagerScript.EMessageForFullScreenUI.inventory_as_snack_bag:
                //do not store our filter settings when we are turned off
                bDoNotStoreFilterStateOnExit = true;

                //load up special SnackBag filter settings.
                SetFilterForSnackStatus();
                break;
            case UIManagerScript.EMessageForFullScreenUI.inventory_as_attack_bag:
                //do not store our filter settings when we are turned off
                bDoNotStoreFilterStateOnExit = true;

                //foom foom foom
                SetFilterForDamageConsumables();
                break;
            case UIManagerScript.EMessageForFullScreenUI.MAX:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        UIManagerScript.messageForFullScreenUI = UIManagerScript.EMessageForFullScreenUI.none;

        UpdateContent();

        //Set first button as default -- not as clever as setting the first ability as such,
        //but we don't know how many we'll have (maybe 0!) and we don't know what mode of this
        //screen we'll be in

        if (itemColumn.GetNumObjectsInList() > 0)
        {
            UIManagerScript.SetDefaultUIFocus(itemColumn.GetTopUIObject());
        }
        else
        {
            UIManagerScript.SetDefaultUIFocus(filterButtons[0].myUIObject);
        }        

        //However, if we used something last time, and it's on the list, try looking at it.
        UIManagerScript.UIObject firstFocus = GetDefaultUiObjectForFocus();
        if (!string.IsNullOrEmpty(strLastSelectedItemRef))
        {
            var lastBtn = itemColumn.GetButtonContaining(strLastSelectedItemRef);
            if (lastBtn != null && lastBtn.gameObject.activeInHierarchy)
            {
                firstFocus = lastBtn.myUIObject;
                lastBtn.Default_OnPointerEnter(null);
            }
            else
            {
                strLastSelectedItemRef = null;
            }
        }

        //focus on the first button 
        UIManagerScript.ChangeUIFocusAndAlignCursor(firstFocus);

        if (itemColumn.GetNumObjectsInList() == 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(filterButtons[0].myUIObject);
        }

        // AA: This is to make sure the item is *actually* selected with the on-select logic, which was not happening previously
        Switch_InvItemButton switchBtn = UIManagerScript.uiObjectFocus.gameObj.GetComponent<Switch_InvItemButton>();
        if (switchBtn != null)
        {
            switchBtn.Default_OnPointerEnter(null);
        }

        for (int i = 0; i < sortButtons.Count; i++)
        {
            if (sortButtons[i].iSubmitValue[0] == (int)lastSortType)
            {
                sortButtons[i].ToggleButton(true);
            }
            else
            {
                sortButtons[i].ToggleButton(false);
            }
        }
    }

    //Made to handle pressing ESC or Back, should go back one step instead of
    //just jumping out the airlock
    public override bool TryTurnOff()
    {
        switch (CursorOwnershipState)
        {
            case EImpactUICursorOwnershipState.tooltip_has_cursor:
                UIManagerScript.PlayCursorSound("Cancel");
                UIManagerScript.TooltipReleaseCursor();
                return false;
            case EImpactUICursorOwnershipState.vertical_hotbar_has_cursor:
                UIManagerScript.PlayCursorSound("Cancel");
                CursorOwnershipState = EImpactUICursorOwnershipState.normal;

                //jump back to the item list
                UIManagerScript.ChangeUIFocusAndAlignCursor(itemColumn.GetTopUIObject());
                return false;
        }

        return true;
    }
    

    public static void InvCloseSubmenuAndRestoreTooltip()
    {
        
    }

    public void HideItemSelectInfo()
    {
    }

    /*
    public void OnSubmit_CategoryButton(int[] iValue)
    {
        UIManagerScript.singletonUIMS.ToggleFilterType(iValue[0]);

        //change the buttons to be on/off based on which filter is active
        //MaintainRadioButtonState(categoryButtons, UIManagerScript.itemFilterTypes);
    }
    */

    public void OnSubmit_SortButton(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        UIManagerScript.singletonUIMS.SortPlayerInventory(iValue[0]);

        //turn off every one but us
        foreach (var btn in sortButtons)
        {
            btn.ToggleButton(btn.iSubmitValue == iValue);
        }

    }

    public void OnSubmit_FilterButton(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        UIManagerScript.singletonUIMS.ToggleFilterType(iValue, true);
        UIManagerScript.PlayCursorSound("Organize");

        UpdateContent();
    }

    public static object Debug_ChangeActiveVerticalHotbar(string[] s)
    {
        //ChangeActiveVerticalHotbar(listVerticalHotbarButtons);
        return "No longer active :(";
    }

    public override void OnClickSwapHotbars(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        SwapVerticalHotbarList(listVerticalHotbarButtons);
    }


    public override void SwapHotbarViaGamepad()
    {
        SwapVerticalHotbarList(listVerticalHotbarButtons);
    }


    public override bool HandleInput(Directions dInput)
    {
		if (PlatformVariables.SHOW_SEARCHBARS)
        {
            Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;
            if (playerInput.GetButtonDown("Jump to Searchbar"))
            {
                invSearchbar.Select();
                invSearchbar.ActivateInputField();
                return true;
            }
        }

        if (base.HandleInput(dInput))
        {
            return true;
        }

        return false;
    }


}

public partial class UIManagerScript
{
    public static Switch_UIInventoryScreen switch_UIInventoryScreen;

    //To make sure Switch/Console works alongside the PC build
    public static UIManagerScript.UIObject[] GetInvItemButtons()
    {
#if UNITY_SWITCH
        return switch_UIInventoryScreen.invItemButtons;
#elif UNITY_EDITOR
        if (GameMasterScript.pretendSwitchEnabled && switch_UIInventoryScreen != null)
        {
            return switch_UIInventoryScreen.invItemButtons;
        }
#endif
        return invItemButtons;
    }

}
