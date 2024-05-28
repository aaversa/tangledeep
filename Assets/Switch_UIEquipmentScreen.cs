using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rewired;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum EEquipmentUISpecialtySlots
{
    EQ_WEAPONHOTBAR_SLOT1 = 0,
    EQ_WEAPONHOTBAR_SLOT2,
    EQ_WEAPONHOTBAR_SLOT3,
    EQ_WEAPONHOTBAR_SLOT4,
    EQ_OFFHAND_SLOT,
    EQ_ARMOR_SLOT,
    EQ_ACCESSORY_SLOT1,
    EQ_ACCESSORY_SLOT2,
    EQ_EMBLEM_SLOT,
    EQ_COUNT,
}

public enum GearBonusAreaState
{
    CURRENTGEAR,
    GEARBONUSES,
    COUNT
}

//Mainly used to keep track of prefabs and place 
//objects in the correct position
public class Switch_UIEquipmentScreen : ImpactUI_WithItemColumn
{
    [Header("Top row: Categories")]
    public Switch_InvItemButton btnFirstButton_Categories;
    public int PixelsBetweenButtons_Categories;
    public List<GearFilters> list_categoryTypes;
    private List<Switch_InvItemButton> categoryButtons;

    [Header("Right Top Column: Filter")]
    public Switch_InvItemButton btnFirstButton_Filter;
    public int PixelsBetweenButtons_Filter;
    public List<GearFilters> list_filterTypes;
    private List<Switch_InvItemButton> filterButtons;

    [Header("Right Bottom Column: Sort")]
    public Switch_InvItemButton btnFirstButton_Sort;
    public int PixelsBetweenButtons_Sort;
    public List<InventorySortTypes> list_sortTypes;
    private List<Switch_InvItemButton> sortButtons;

    [Header("Weapon Hotbar And Gear Info")]
    public Switch_InvItemButton[] Hotbar_WeaponButtons;
    public GameObject Label_EquippedWeapon;
    public Switch_InvItemButton Info_Offhand;
    public Switch_InvItemButton Info_Armor;
    public Switch_InvItemButton Info_Accessory1;
    public Switch_InvItemButton Info_Accessory2;
    public Switch_InvItemButton Info_Emblem;

    [Header("Gear Bonuses")]
    public Image GearBonusArea;
    [Tooltip("Where we want the cursor to point when the GearBonusArea is the selected object.")]
    public GameObject GearBonusArea_Header;
    public Scrollbar scrollbar_GearBonusArea;
    public TextMeshProUGUI txt_GearBonuses;
    private Coroutine coroutine_GearBonuses;
    public Switch_InvItemButton tab_ToggleToGear;
    public Switch_InvItemButton tab_ToggleToBonuses;
    public GameObject gearBonusHolderObject;
    public GameObject currentGearHolderObject;

    [Header("Searchbar")]
    public TMP_InputField eqSearchbar;

    public static GearBonusAreaState bonusAreaState;

    //private UIManagerScript.UIObject uiObject_GearBonusArea;

    //A master list that covers all of the above
    private List<Switch_InvItemButton> equippedGearButtons;

    public static LTDescr tweenEQItemBounce;
    public static Vector2 vBounceItemOrigin;

    //Used for scrolling up and down the equipment list.
    public int listArrayIndexOffset;

    //Selected category is different from the filters on the right.
    private GearFilters filterSelectedCategory;

    //When dragging an item out of weapon hotbar slots or 
    //accessory slots, we need to auto swap the item we're replacing
    //if it is also in the weapon hotbar or an accessory
    private EEquipmentUISpecialtySlots lastDraggedFromEQSlot;

    //When we are TurningOn() the UI, we don't want to play sounds/effects for items 
    //filling the buttons
    private bool bDuringTurnOnAction;

    public override void Awake()
    {
        base.Awake();
        myTabType = UITabs.EQUIPMENT;

    }

    public override void OnDialogClose()
    {
        //close the submenu if it is still open
        if (CursorOwnershipState == EImpactUICursorOwnershipState.tooltip_has_cursor)
        {
            UIManagerScript.TooltipReleaseCursor();
        }

        //make sure we have control of the cursor again
        if (!UIManagerScript.allUIObjects.Contains(UIManagerScript.uiObjectFocus))
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.GetDefaultUIFocus());
        }

        Update();

        UIManagerScript.UpdateActiveWeaponInfo(); // Our weapon hotbar state may have changed, so let's update it        

    }

    //Used to make an item icon pop
    public static void TooltipItemBounce(float fBounceTime, int iBouncePX, RectTransform rtBounceMe)
    {
        vBounceItemOrigin = rtBounceMe.anchoredPosition;
        Vector2 vChangePos = rtBounceMe.anchoredPosition;
        vChangePos.y -= iBouncePX;
        rtBounceMe.anchoredPosition = vChangePos;

        tweenEQItemBounce = LeanTween.moveY(rtBounceMe, vBounceItemOrigin.y, fBounceTime);
        tweenEQItemBounce.setEaseOutElastic().setOvershoot(iBouncePX);
    }


    public void HideItemSelectInfo()
    {
    }

    //Made to handle pressing ESC or Back, should go back one step instead of
    //just jumping out the airlock
    public override bool TryTurnOff()
    {
        switch (CursorOwnershipState)
        {
            case EImpactUICursorOwnershipState.tooltip_has_cursor:
                UIManagerScript.PlayCursorSound("UITock"); // was Cancel
                UIManagerScript.TooltipReleaseCursor();
                return false;
        }

        return true;
    }

    public void ScrollEQBonusArea()
    {

    }

    protected override void InitializeFilterState()
    {
        lastFilterState = new InventoryUIState(false);
        lastFilterState.filterStates[(int)GearFilters.COMMON] = true;
        lastFilterState.filterStates[(int)GearFilters.MAGICAL] = true;
        lastFilterState.filterStates[(int)GearFilters.LEGENDARY] = true;
        lastFilterState.filterStates[(int)GearFilters.GEARSET] = true;
        lastFilterState.filterStates[(int)GearFilters.FAVORITES] = false;
    }

    public override void TurnOff()
    {
        base.TurnOff();

        if (coroutine_GearBonuses != null)
        {
            StopCoroutine(coroutine_GearBonuses);
            coroutine_GearBonuses = null;
        }

    }

    public override void TurnOn()
    {
        bDuringTurnOnAction = true;

        base.TurnOn();

        bonusAreaState = GearBonusAreaState.CURRENTGEAR;

        //dynamic button content has been created already, cool.
        listArrayIndexOffset = 0;

        //reset the ui objects
        //Add all our buttons and finery to the UI objects
        UIManagerScript.allUIObjects.Clear();
        AddAllUIObjects(UIManagerScript.allUIObjects);


        bool anyButtonToggled = false;
        foreach (Switch_InvItemButton butan in categoryButtons)
        {
            if (butan.IsToggled())
            {
                anyButtonToggled = true;
                break;
            }
        }

        if (!anyButtonToggled) // This is probably only the case when first opening EQ sheet.
        {
            categoryButtons[0].ToggleButton(true);
            filterSelectedCategory = GearFilters.VIEWALL;
        }

        UpdateContent();

        //point at first object
        if (itemColumn.GetNumObjectsInList() == 0)
        {
            // No items, go to ViewAll instead
            UIManagerScript.ChangeUIFocusAndAlignCursor(categoryButtons[0].myUIObject);
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(GetDefaultUiObjectForFocus());
        }

        //UIManagerScript.ChangeUIFocusAndAlignCursor(GetDefaultUiObjectForFocus());

        // AA: This is to make sure the item is *actually* selected with the on-select logic, which was not happening previously
        Switch_InvItemButton switchBtn = UIManagerScript.uiObjectFocus.gameObj.GetComponent<Switch_InvItemButton>();
        if (switchBtn != null)
        {
            switchBtn.Default_OnPointerEnter(null);
        }

        bDuringTurnOnAction = false;

        UpdateGearBonusAreaState();
    }

    /*
    * Add top row of category buttons
    * Add side row of filter buttons
    * Add side row of sort buttons
    * Add left 4 weapon hotboxes
    * Add left column of offhand, armor, acc1, acc2
    * Add main column of item buttons 
    * Add tooltip submenu

    AddUIObjectsFromListOfButtons( JIBBA , UIManagerScript.allUIObjects);
    */
    protected override void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        base.AddAllUIObjects(allObjects);
        AddUIObjectsFromListOfButtons(filterButtons, allObjects);
        AddUIObjectsFromListOfButtons(categoryButtons, allObjects);
        AddUIObjectsFromListOfButtons(sortButtons, allObjects);
        AddUIObjectsFromListOfButtons(equippedGearButtons, allObjects);
        UIManagerScript.allUIObjects.Add(tab_ToggleToBonuses.myUIObject);
        UIManagerScript.allUIObjects.Add(tab_ToggleToGear.myUIObject);
    }

    //Activate tooltip, then compare if necessary
    public override void DisplayItemInfo(ISelectableUIObject itemToDisplay, GameObject refObject, bool mouseHoverSource)
    {
        base.DisplayItemInfo(itemToDisplay, refObject, mouseHoverSource);

        bool checkAlternate = TDInputHandler.IsCompareAlternateButtonHeld();

        //if the item is not a consumable (and it should never be in the Equip screen, but...) find an item to compare it against.
        //todo check the dragging item state to see if we need to compare against that

        Item itemToCompare = itemToDisplay as Item;

        //Debug.Log("Item to display is " + itemToCompare.actorRefName + " alternate is " + checkAlternate + " we must find the alt.");

        Item compareItem = EquipmentBlock.FindEquipmentToCompareAgainst(itemToDisplay as Equipment, checkAlternate);
        if (compareItem != null)
        {
            genericTooltip.SetComparisonItem(compareItem);
        }
    }

    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        //Create category, filter, and main item buttons
        CreateButtons();

        //Scrolling section with information on what bonuses our gear provides
        CreateGearBonusArea();

        ConnectButtons();

        bHasBeenInitialized = true;
        return true;
    }

    void ConnectButtons()
    {
        FontManager.LocalizeMe(txt_GearBonuses, TDFonts.WHITE);        

        //connect main lists
        MakeListOfNeighbors(categoryButtons, Directions.WEST, Directions.EAST);
        MakeListOfNeighbors(filterButtons, Directions.NORTH, Directions.SOUTH);
        MakeListOfNeighbors(sortButtons, Directions.NORTH, Directions.SOUTH);

        categoryButtons[0].GetTMPro().text = StringManager.GetString("item_filters_view_all");
        categoryButtons[1].GetTMPro().text = StringManager.GetString("eq_slot_weapon_plural").ToUpperInvariant();
        categoryButtons[2].GetTMPro().text = StringManager.GetString("eq_slot_offhand_plural").ToUpperInvariant();
        categoryButtons[3].GetTMPro().text = StringManager.GetString("eq_slot_armor_plural").ToUpperInvariant();
        categoryButtons[4].GetTMPro().text = StringManager.GetString("eq_slot_accessory_plural").ToUpperInvariant();

        filterButtons[0].GetTMPro().text = StringManager.GetString("misc_rarity_0").ToUpperInvariant();
        filterButtons[1].GetTMPro().text = StringManager.GetString("misc_rarity_2").ToUpperInvariant();
        filterButtons[2].GetTMPro().text = StringManager.GetString("misc_rarity_4b").ToUpperInvariant();
        filterButtons[3].GetTMPro().text = StringManager.GetString("misc_rarity_5").ToUpperInvariant();
        filterButtons[4].GetTMPro().text = StringManager.GetString("item_filters_favorites").ToUpperInvariant();

        sortButtons[0].GetTMPro().text = StringManager.GetString("item_sort_type_type");
        sortButtons[1].GetTMPro().text = StringManager.GetString("item_sort_type_alpha");
        sortButtons[2].GetTMPro().text = StringManager.GetString("item_sort_type_value");
        sortButtons[3].GetTMPro().text = StringManager.GetString("item_sort_type_rank");
        sortButtons[4].GetTMPro().text = StringManager.GetString("item_sort_type_rarity");

        //Start at 4, since 0-3 are the hotbars
        int iGearAreaStart = 4;


        //Connect item column to top
        itemColumn.neighborTop = categoryButtons[0].myUIObject;

        //Give everything access to main items
        foreach (var btn in categoryButtons)
        {
            btn.myUIObject.neighbors[(int)Directions.SOUTH] = itemColumn.GetTopUIObject();
        }
        foreach (var btn in filterButtons)
        {
            btn.myUIObject.neighbors[(int)Directions.WEST] = itemColumn.GetTopUIObject();

            // East will wrap around and go to your gear list.
            btn.myUIObject.neighbors[(int)Directions.EAST] = equippedGearButtons[iGearAreaStart].myUIObject;
        }
        // The TOP filter button will now go to tabs instead of gear list
        filterButtons[0].myUIObject.neighbors[(int)Directions.WEST] = categoryButtons[categoryButtons.Count-1].myUIObject;
        foreach (var btn in sortButtons)
        {
            btn.myUIObject.neighbors[(int)Directions.WEST] = itemColumn.GetTopUIObject();
        }

        // AA: This block is so that the top item in the gear list, when you press UP on controller/keyboard, will first try
        // scrolling the list up. If it is already at the top, it will move to the neighbor (the 0-index category button)
        itemColumn.GetTopUIObject().neighbors[(int)Directions.NORTH] = categoryButtons[0].myUIObject;
        itemColumn.GetTopUIObject().directionalActions[(int)Directions.NORTH] = itemColumn.GetTopUIObject().TryScrollPool;
        itemColumn.GetTopUIObject().directionalValues[(int)Directions.NORTH] = -1;

        //filter and sort need to bro up
        filterButtons[filterButtons.Count - 1].myUIObject.neighbors[(int)Directions.SOUTH] = sortButtons[0].myUIObject;
        sortButtons[0].myUIObject.neighbors[(int)Directions.NORTH] = filterButtons[filterButtons.Count - 1].myUIObject;

        //category and filter as well
        filterButtons[0].myUIObject.neighbors[(int)Directions.WEST] = categoryButtons[categoryButtons.Count - 1].myUIObject;
        categoryButtons[categoryButtons.Count - 1].myUIObject.neighbors[(int)Directions.EAST] = filterButtons[0].myUIObject;

        //category left needs to go to the hotbar
        //categoryButtons[0].myUIObject.neighbors[(int)Directions.WEST] = Hotbar_WeaponButtons[Hotbar_WeaponButtons.Length - 1].myUIObject;
        // Actually send it to the tab toggle buttons
        categoryButtons[0].myUIObject.neighbors[(int)Directions.WEST] = tab_ToggleToBonuses.myUIObject;

        //Item column can go to gear set
        itemColumn.neighborLeft = Hotbar_WeaponButtons[Hotbar_WeaponButtons.Length - 1].myUIObject;

        // and east goes to our filter buttons
        itemColumn.neighborRight = filterButtons[1].myUIObject;

        //Make the hotbar into bros, and have them approach their neighbors
        for (int t = 0; t < Hotbar_WeaponButtons.Length; t++)
        {
            if (t != 0)
            {
                Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.WEST] =
                    Hotbar_WeaponButtons[t - 1].myUIObject;
            }
            else
            {
                // Leftmost button wraps to filters.
                Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.WEST] =
                    filterButtons[0].myUIObject;
            }

            if (t != Hotbar_WeaponButtons.Length - 1)
            {
                Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.EAST] =
                    Hotbar_WeaponButtons[t + 1].myUIObject;
            }
            else
            {
                Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.EAST] = itemColumn.GetTopUIObject();
            }

            Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.SOUTH] = Info_Offhand.myUIObject;
            Hotbar_WeaponButtons[t].myUIObject.neighbors[(int)Directions.NORTH] = tab_ToggleToGear.myUIObject;

        }

        for (int t = iGearAreaStart; t < equippedGearButtons.Count; t++)
        {
            // Shortcut to get to the filters on right side of the screen - push left on equipped gear list
            equippedGearButtons[t].myUIObject.neighbors[(int)Directions.WEST] =
                filterButtons[0].myUIObject;

            if (t != iGearAreaStart)
            {
                equippedGearButtons[t].myUIObject.neighbors[(int)Directions.NORTH] =
                    equippedGearButtons[t - 1].myUIObject;
            }
            else
            {
                equippedGearButtons[t].myUIObject.neighbors[(int)Directions.NORTH] =
                    Hotbar_WeaponButtons[0].myUIObject;
            }

            if (t != equippedGearButtons.Count - 1)
            {
                equippedGearButtons[t].myUIObject.neighbors[(int)Directions.SOUTH] =
                    equippedGearButtons[t + 1].myUIObject;
            }
            else
            {
                /* equippedGearButtons[t].myUIObject.neighbors[(int)Directions.SOUTH] =
                    uiObject_GearBonusArea; */
            }

            equippedGearButtons[t].myUIObject.neighbors[(int)Directions.EAST] = itemColumn.GetTopUIObject();
        }

        //Gearbonus area connects north to the 2nd accessory
        //uiObject_GearBonusArea.neighbors[(int)Directions.NORTH] = equippedGearButtons[equippedGearButtons.Count - 1].myUIObject;

        //and east to the items
        //uiObject_GearBonusArea.neighbors[(int)Directions.EAST] = itemColumn.GetTopUIObject();

    }

    void CreateGearBonusArea()
    {
        /* uiObject_GearBonusArea = new UIManagerScript.UIObject();
        uiObject_GearBonusArea.gameObj = GearBonusArea.gameObject; */

        //set listeners for new button
        EventTrigger et = GearBonusArea.gameObject.GetComponent<EventTrigger>();

        //Mouseover start
        EventTrigger.Entry newEntry = new EventTrigger.Entry();
        newEntry.eventID = EventTriggerType.PointerEnter;
        newEntry.callback.AddListener(arg0 => FocusOnGearBonusArea());
        et.triggers.Add(newEntry);

        newEntry = new EventTrigger.Entry();
        newEntry.eventID = EventTriggerType.PointerExit;
        newEntry.callback.AddListener(arg0 => EndFocusOnGearBonusArea());
        et.triggers.Add(newEntry);

        List<Switch_InvItemButton> toggleBtns = new List<Switch_InvItemButton>();
        toggleBtns.Add(tab_ToggleToBonuses);
        toggleBtns.Add(tab_ToggleToGear);

        tab_ToggleToBonuses.SetDisplayText(StringManager.GetString("ui_tab_gearbonuses"));
        tab_ToggleToGear.SetDisplayText(StringManager.GetString("ui_tab_currentgear"));

        foreach (Switch_InvItemButton btn in toggleBtns)
        {
            btn.onClickAction = SetGearBonusToggleState;

            //Create a Shadow UIObject
            btn.myUIObject = new UIManagerScript.UIObject();
            btn.myUIObject.gameObj = btn.gameObject;
            btn.myUIObject.onSubmitValue = btn.myID;
            btn.myUIObject.mySubmitFunction = btn.SubmitFunction_OnClickOrPress;

            btn.SetEventListener(EventTriggerType.PointerClick, btn.Default_OnClick);

            //use our navigation code to move about
            for (int i = (int)Directions.NORTH; i <= (int)Directions.WEST; i += 2)
            {
                btn.myUIObject.directionalActions[i] = btn.MoveCursorToNeighbor;
            }
        }

        tab_ToggleToGear.iSubmitValue = new int[] { (int)GearBonusAreaState.CURRENTGEAR };
        tab_ToggleToBonuses.iSubmitValue = new int[] { (int)GearBonusAreaState.GEARBONUSES };

        tab_ToggleToGear.myUIObject.neighbors[(int)Directions.EAST] = tab_ToggleToBonuses.myUIObject;
        tab_ToggleToGear.myUIObject.neighbors[(int)Directions.WEST] = categoryButtons[categoryButtons.Count - 1].myUIObject;

        tab_ToggleToBonuses.myUIObject.neighbors[(int)Directions.EAST] = categoryButtons[0].myUIObject;
        tab_ToggleToBonuses.myUIObject.neighbors[(int)Directions.WEST] = tab_ToggleToGear.myUIObject;

        tab_ToggleToBonuses.myUIObject.neighbors[(int)Directions.SOUTH] = Hotbar_WeaponButtons[0].myUIObject;
        tab_ToggleToGear.myUIObject.neighbors[(int)Directions.SOUTH] = Hotbar_WeaponButtons[0].myUIObject;

        TextMeshProUGUI gearBonusHeaderTM = GearBonusArea_Header.GetComponent<TextMeshProUGUI>();
        FontManager.LocalizeMe(gearBonusHeaderTM, TDFonts.WHITE);
        gearBonusHeaderTM.text = StringManager.GetString("ui_misc_bonusesfromgear");
    }

    public void SetGearBonusToggleState(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        UIManagerScript.PlayCursorSound("UITock");
        if (args.Length > 0)
        {
            int iState = args[0];
            bonusAreaState = (GearBonusAreaState)iState;
        }

        UpdateGearBonusAreaState();
    }

    void UpdateGearBonusAreaState()
    {
        bool currentGearIsActive = bonusAreaState == GearBonusAreaState.CURRENTGEAR;

        currentGearHolderObject.SetActive(currentGearIsActive);
        tab_ToggleToGear.ToggleButton(currentGearIsActive);
        gearBonusHolderObject.SetActive(!currentGearIsActive);
        tab_ToggleToBonuses.ToggleButton(!currentGearIsActive);

        foreach (Switch_InvItemButton btn in equippedGearButtons)
        {
            btn.myUIObject.enabled = currentGearIsActive;
        }
    }

    void FocusOnGearBonusArea()
    {
        //if (UIManagerScript.AllowCursorToFocusOnThis(uiObject_GearBonusArea.gameObj))
        {
            //UIManagerScript.ChangeUIFocusAndAlignCursor(uiObject_GearBonusArea);

            if (coroutine_GearBonuses != null)
            {
                StopCoroutine(coroutine_GearBonuses);
                coroutine_GearBonuses = null;
            }
        }
    }

    //Restart the stock ticker
    void EndFocusOnGearBonusArea()
    {
        if (coroutine_GearBonuses == null)
        {
            coroutine_GearBonuses = StartCoroutine(SlowlyScrollGearBonuses());
        }
    }


    //Builds and positions the navigation buttons based on the filter enums they use    
    void CreateButtons()
    {
        if (bHasBeenInitialized)
        {
            return;
        }

        TextMeshProUGUI equippedWeaponTMPro = Label_EquippedWeapon.GetComponent<TextMeshProUGUI>();

        switch(StringManager.gameLanguage)
        {
            case EGameLanguage.de_germany:
                equippedWeaponTMPro.text = StringManager.GetString("ui_button_equipped");
                break;
            default:
                equippedWeaponTMPro.text = StringManager.GetString("ui_button_equipped").ToUpperInvariant();
                break;
        }
        
        FontManager.LocalizeMe(equippedWeaponTMPro, TDFonts.WHITE);

        // AA - this appears to be shared by the inventory sheet...? :thinking: 
        categoryButtons = CreateListOfButtons(btnFirstButton_Categories, list_categoryTypes, new Vector2(1, 0), PixelsBetweenButtons_Categories, OnSubmit_CategoryButton);

        filterButtons = CreateListOfButtons(btnFirstButton_Filter, list_filterTypes, new Vector2(0, -1), PixelsBetweenButtons_Filter, OnSubmit_FilterButton);
        sortButtons = CreateListOfButtons(btnFirstButton_Sort, list_sortTypes, new Vector2(0, -1), PixelsBetweenButtons_Sort, OnSubmit_SortButton);

        //Combine all the gear buttons into a list
        equippedGearButtons = new List<Switch_InvItemButton>();
        for (int idx = 0; idx < 4; idx++)
        {
            equippedGearButtons.Add(Hotbar_WeaponButtons[idx]);
        }
        equippedGearButtons.Add(Info_Offhand);
        equippedGearButtons.Add(Info_Armor);
        equippedGearButtons.Add(Info_Accessory1);
        equippedGearButtons.Add(Info_Accessory2);
        equippedGearButtons.Add(Info_Emblem);

        equippedGearButtons.RemoveAll(r => r == null);
        //spit and polish them for proper behavior
        int iButtonIdx = 0;
        foreach (Switch_InvItemButton btn in equippedGearButtons)
        {
            btn.myID = iButtonIdx;

            //find the button on this object and tell it things
            //Button unityButton = btn.GetComponent<Button>();

            //Clean off old listeners
            //unityButton.onClick.RemoveAllListeners();
            //unityButton.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);

            btn.SetEventListener(EventTriggerType.BeginDrag, btn.Default_OnDrag);
            btn.SetEventListener(EventTriggerType.Drop, btn.Default_OnDrop);
            btn.SetEventListener(EventTriggerType.PointerEnter, btn.Default_OnPointerEnter);
            btn.SetEventListener(EventTriggerType.PointerExit, btn.Default_OnPointerExit);
            btn.SetEventListener(EventTriggerType.PointerClick, btn.Default_OnClick);

            btn.onPointerEnter = SetTooltipViaEquipmentButtonByID;
            if (Hotbar_WeaponButtons.Contains(btn))
            {
                btn.onClickAction = OpenTooltipSubmenuViaWeaponHotbar;
            }
            else
            {
                btn.onClickAction = OpenTooltipSubmenu;
            }
            btn.onDrag = StartDragItemFromEquipmentPanel;
            btn.onDrop = OnDrop_HandleItemEquip;

            //btn.onClickAction = OnSubmit_EquippedGearButton;
            btn.iSubmitValue = new[] { btn.myID };

            //Create a Shadow UIObject
            btn.myUIObject = new UIManagerScript.UIObject();
            btn.myUIObject.gameObj = btn.gameObject;
            btn.myUIObject.onSubmitValue = btn.myID;
            btn.myUIObject.subObjectImage = btn.gameObject.GetComponentsInChildren<Image>()[1];
            btn.myUIObject.mySubmitFunction = btn.SubmitFunction_OnClickOrPress;
            btn.myUIObject.myOnSelectAction = btn.TryPlaceContentInTooltip;

            //use our navigation code to move about
            for (int i = (int)Directions.NORTH; i <= (int)Directions.WEST; i += 2)
            {
                btn.myUIObject.directionalActions[i] = btn.MoveCursorToNeighbor;
            }


            iButtonIdx++;
        }
    }

    //todo: handle turn ending if equipping out in the wild
    public void OnDrop_HandleItemEquip(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //Debug.Log("Dropped an Itam");
        Equipment eq = UIManagerScript.GetHeldGenericObject() as Equipment;
        if (eq == null)
        {
            EndDragGenericObject();
            UIManagerScript.PlayCursorSound("UITock"); // was cancel
            return;
        }

        Switch_InvItemButton dropButton = equippedGearButtons[iValue[0]];
        bool bTargetIsWeaponHotbar = dropButton.boundGearSlot == EquipmentSlots.WEAPON;
        bool bItemIsWeapon = (eq is Weapon);

        //Debug.Log("Dropped " + eq.actorRefName + " " + eq.actorUniqueID + " " + iValue[0] + " " + dropButton.gameObject.name + " " + dropButton.boundGearSlot);

        //Can the button we're dragging towards even handle an item of our magnitude?
        if (bTargetIsWeaponHotbar != bItemIsWeapon &&
            dropButton.boundGearSlot != EquipmentSlots.OFFHAND)
        {
            EndDragGenericObject();
            UIManagerScript.PlayCursorSound("UITock"); // was cancel
            return;
        }

        //If we're dropping to a hotbar, and from a hotbar, do teh swaps
        if (bTargetIsWeaponHotbar &&
            lastDraggedFromEQSlot >= EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1 &&
            lastDraggedFromEQSlot <= EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4)
        {
            MouseDragSwap(dropButton, lastDraggedFromEQSlot);
        }


        //if we're dropping to an accessory, from the other accessory, do teh swaps
        if (dropButton.boundGearSlot == EquipmentSlots.ACCESSORY && lastDraggedFromEQSlot == EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2 ||
            dropButton.boundGearSlot == EquipmentSlots.ACCESSORY2 && lastDraggedFromEQSlot == EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1)
        {
            MouseDragSwap(dropButton, lastDraggedFromEQSlot);
        }

        //Try to equip the item, and if it does not fit, we must quit. accquit.
        // AA note - this is happening when we drag items on to the hotbar, and switching the active slot.
        // Do not do this. This should just add to hotbar, thats it. No equip.
        /* if (!GameMasterScript.heroPCActor.myEquipment.Equip(eq, SND.PLAY, dropButton.boundGearSlot, true))
        {
            EndDragGenericObject();
            UpdateContent();
            return;
        } */

        if (bTargetIsWeaponHotbar)
        {
            UIManagerScript.hotbarWeapons[dropButton.weaponHotbarIdx] = eq as Weapon;
            UpdateContent();
        }

        bool returnToWorld = false; // #todo

        bool equipSucceeded = true;

        if (bTargetIsWeaponHotbar && dropButton.weaponHotbarIdx == UIManagerScript.GetActiveWeaponSlot())
        {
            equipSucceeded = GameMasterScript.heroPCActor.myEquipment.Equip(eq, SND.PLAY, dropButton.boundGearSlot, true);
        }
        else if (!bTargetIsWeaponHotbar)
        {
            // This should run when you drop an item on a non-hotbar slot.
            equipSucceeded = GameMasterScript.heroPCActor.myEquipment.EquipOnlyIfValid(eq, SND.SILENT, dropButton.boundGearSlot, true);
        }

        EndDragGenericObject();

        //everything is in its place
        if (equipSucceeded)
        {
            GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem");
            if (!UIManagerScript.singletonUIMS.CheckIfPassTurnFromEquipping())
            {
                //FlashInventoryButtonOnSuccessfulEquip(dropButton, eq);
                UpdateContent();
            }
        }
    }

    void MouseDragSwap(Switch_InvItemButton btnFrom, EEquipmentUISpecialtySlots slotTo)
    {
        //Remove the item from the slot
        Item itm = btnFrom.GetContainedData() as Item;
        if (itm == null)
        {
            return;
        }

        HeroPC h = GameMasterScript.heroPCActor;

        //Unequip the item from the character
        h.Unequip(itm);

        //if the from button is a hotbar weapon slot, clear it out.
        if (btnFrom.boundGearSlot == EquipmentSlots.WEAPON)
        {
            UIManagerScript.hotbarWeapons[btnFrom.weaponHotbarIdx] = null;
        }

        //if the slotTo is a hotbar weapon slot, put it there
        if (slotTo <= EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4)
        {
            UIManagerScript.hotbarWeapons[(int)slotTo] = itm as Weapon;
        }
        //otherwise, equip the item
        else
        {
            h.myEquipment.Equip(itm as Equipment, SND.SILENT, (EquipmentSlots)(slotTo - EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4), false);
        }

    }

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

        UIManagerScript.singletonUIMS.ToggleFilterType(iValue);
        UIManagerScript.PlayCursorSound("Tick");

        UpdateContent();
    }

    public void OnSubmit_CategoryButton(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        filterSelectedCategory = (GearFilters)iValue[0];

        foreach (Switch_InvItemButton btn in categoryButtons)
        {
            btn.ToggleButton(btn.iSubmitValue[0] == (int)filterSelectedCategory);
        }

        UIManagerScript.PlayCursorSound("Organize");
        UpdateContent();
    }


    //Looks inside a button for an item or ability to put in a tooltip
    protected void SetTooltipViaEquipmentButtonByID(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //grab the button we passed in
        Switch_InvItemButton btn = equippedGearButtons[args[0]];

        Item itm = btn.GetContainedData() as Item;

        if (itm == null)
        {
            // We need to clear the tooltip here if the slot is empty.
            if (genericTooltip != null)
            {
                genericTooltip.Hide();
            }

            return;
        }
        DisplayItemInfo(itm, null, false);
        FocusAndBounceButton(btn);
    }

    //Start dragging a copy of this object, but don't remove us from the list.
    protected void StartDragItemFromEquipmentPanel(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {        
        //grab the button we passed in
        Switch_InvItemButton btn = equippedGearButtons[args[0]];

        Item itm = btn.GetContainedData() as Item;
        if (itm == null)
        {
            //lol?
            return;
        }

        //todo: worry feverishly about the day when Andrew decides to make unequip a bool because cursed items
        HeroPC hero = GameMasterScript.GetHeroActor();
        bool bCanRemove = true;

        /*
         * bCanRemove = hero.TryUnequip(itm);
         */

        if (!bCanRemove)
        {
            return;
        }

        if (itm.itemType != ItemTypes.WEAPON && GameMasterScript.heroPCActor.WouldChangingEquipmentFailQuest() && GameMasterScript.gmsSingleton.ReadTempGameData("equip_confirm_fail") != 1)
        {
            UIManagerScript.StartConversationByRef("dialog_confirm_changegear_failrumor", DialogType.STANDARD, null);           
            return;
        }           
        GameMasterScript.gmsSingleton.SetTempGameData("equip_confirm_fail", 0);

        //begin the drag
        UIManagerScript.BeginDragGenericObject(btn.GetContainedData(), btn.gameObject);

        //remove the item
        if (btn.boundGearSlot == EquipmentSlots.WEAPON)
        {
            //remove from hotbar
            UIManagerScript.hotbarWeapons[btn.weaponHotbarIdx] = null;
            if (hero.myEquipment.IsEquipped(itm))
            {
                hero.Unequip(EquipmentSlots.WEAPON, false);
            }

            //store the last-dragged-from value
            lastDraggedFromEQSlot = (EEquipmentUISpecialtySlots)btn.weaponHotbarIdx;
        }
        else
        {
            hero.Unequip(btn.boundGearSlot, false);
            //store the last-dragged-from value
            // The specialty enum is weapon hotbars from 0-3
            lastDraggedFromEQSlot = EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4 + (int)btn.boundGearSlot;
        }

        //update our content
        UpdateContent();

    }

    public override void BeginDragGenericObject(ISelectableUIObject obj, GameObject originGO)
    {
        lastDraggedFromEQSlot = EEquipmentUISpecialtySlots.EQ_COUNT;
        base.BeginDragGenericObject(obj, originGO);
    }

    public override void FocusAndBounceButton(Switch_InvItemButton btn)
    {
        //UIManagerScript.uiObjectFocus = btn.myUIObject;
        UIManagerScript.ChangeUIFocus(btn.myUIObject, processEvent: false);

        if (btn.boundGearSlot != EquipmentSlots.WEAPON)
        {
            base.FocusAndBounceButton(btn);
            return;
        }

        //This is one of the weaponhotbar buttons, bounce it vertically
        btn.PrepareForTween(Vector2.zero);
        RectTransform rt = btn.transform.GetChild(0) as RectTransform;
        Vector2 vPos = new Vector2(1, 2); // <- copied from the editor. Bad magic number :(  
        rt.anchoredPosition = new Vector2(vPos.x, vPos.y - 4);
        LeanTween.moveY(rt, vPos.y, 0.3f).setEaseInOutBack().setOvershoot(2);
    }

    public override int GetFilterIndexForViewAll() { return (int)GearFilters.VIEWALL; }
    public override int GetFilterIndexForFavorites() { return (int)GearFilters.FAVORITES; }

    public override void UpdateContent(bool adjustSorting = true)
    {
        //Make all the pretty side buttons on/off correctly
        MaintainRadioButtonState(filterButtons, UIManagerScript.itemFilterTypes);

        //equipped weapons and armor
        foreach (var btn in equippedGearButtons)
        {
            //if these calls return true, the gear inside has changed. If we've swapped out an
            //item, we should display some stuff
            bool bHasSwapped = btn.UpdateContentFromCharacter();
            if (!bDuringTurnOnAction &&
                bHasSwapped &&
                btn.GetContainedData() != null)
            {
                FlashInventoryButtonOnSuccessfulEquip(btn, btn.GetContainedData());
            }
        }

        //Set the label to match the currently equipped hotbar slot.
        int iActiveWeaponSlot = UIManagerScript.GetActiveWeaponSlot();
        if (iActiveWeaponSlot >= 0 && iActiveWeaponSlot < Hotbar_WeaponButtons.Length)
        {
            Label_EquippedWeapon.transform.SetParent(Hotbar_WeaponButtons[UIManagerScript.GetActiveWeaponSlot()].transform);
            Label_EquippedWeapon.transform.localPosition = new Vector3(0, -14f);
        }

        foreach (var btn in Hotbar_WeaponButtons)
        {
            Weapon w = btn.GetContainedData() as Weapon;
            Image background = btn.GetComponent<Image>();

            /*
            if (!bEquipLabelHasBeenSet && w != null && GameMasterScript.heroPCActor.myEquipment.IsEquipped(w))
            {
                Label_EquippedWeapon.transform.SetParent(btn.transform);
                Label_EquippedWeapon.transform.localPosition = Vector3.zero;
                bEquipLabelHasBeenSet = true;
            }
            */

            if (w == null || w.rarity == Rarity.COMMON)
            {
                background.color = Color.clear;
            }
            else
            {
                background.color = Item.GetRarityColor(w);                
            }
        }

        List<ISelectableUIObject> playerItemList = CreateItemListFromInventoryOfThisTypeThatMatchesFilters<Equipment>(GameMasterScript.heroPCActor.myInventory);

        if (genericTooltip != null &&
            !playerItemList.Contains(genericTooltip.GetTargetItem()))
        {
            genericTooltip.ClearAllItems();
        }

        //Now we have a list of all pertinent items!
        itemColumn.PlaceObjectsInList(playerItemList, false);

        //Change the button display names based on quantity or hunger
        itemColumn.AdjustButtonInformationViaAction(Action_UpdateContainedItemName);

        //What all those items do for us
        UpdateGearBonusText();

        RefreshItemInfoForSelectedButton();

        UIManagerScript.UpdateWeaponHotbarGraphics();

        // Ensure our color state is correct when sheet is updated. Previously, this was not happening 
        // when sheet was first opened.
        for (int i = 0; i < sortButtons.Count; i++)
        {
            sortButtons[i].ToggleButton(false);
        }
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            if (filterSelectedCategory == list_categoryTypes[i])
            {
                categoryButtons[i].ToggleButton(true);
            }
            else
            {
                categoryButtons[i].ToggleButton(false);
            }
        }        

        // Forget this stuff below, isn't needed
        /* for (int i = 0; i < filterButtons.Count; i++)
        {
            
            Debug.Log("Checking button index " + i + " corresponding to " + (GearFilters)i + " ___ " + filterButtons[i].myID + " " + filterButtons[i].myUIObject.onSubmitValue + " " + filterButtons[i].myUIObject.onSelectValue);
            bool doToggle = false;
            for (int x = 0; x < (int)GearFilters.COUNT; x++)
            {
                if (lastFilterState.filterStates[x] && i == x)
                {
                    Debug.Log((GearFilters)x + " should be enabled and we are checking button index " + (GearFilters)i + " " + i);
                    doToggle = true;
                    break;
                }
            }
            filterButtons[i].ToggleButton(doToggle);
        } */
    }

    public override void Update()
    {
        base.Update();

        if (!GameMasterScript.actualGameStarted)
        {
            return;
        }

        UIManagerScript.UIObject currentFocusedShadowObject = UIManagerScript.uiObjectFocus;
        CursorBounce cb = UIManagerScript.singletonUIMS.uiDialogMenuCursorBounce;

        // Run special code if searchbar is focused.
        if (PlatformVariables.SHOW_SEARCHBARS && eqSearchbar.isFocused)
        {
            return;
        }

        //look at the gear on the left, and if we are pointing at one of these, adjust the position of the
        //cursor.
        bool bShouldFaceEast = true;
        for (int iBtnIdx = 0; iBtnIdx < equippedGearButtons.Count; iBtnIdx++)
        {
            if (currentFocusedShadowObject == equippedGearButtons[iBtnIdx].myUIObject)
            {
                bShouldFaceEast = false;
                float multiplier = Screen.width / 1920f;

                float fButtonWidth = ((RectTransform)currentFocusedShadowObject.gameObj.transform).sizeDelta.x * multiplier; //because tangledeep
                // <4 is the top row 
                if (iBtnIdx < 4)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(currentFocusedShadowObject, fButtonWidth * 0.5f, 0f);
                    cb.SetFacing(Directions.NORTH);
                }
                else
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(currentFocusedShadowObject, fButtonWidth, 0f);
                    cb.SetFacing(Directions.WEST);
                }
            }
        }

        // Cursor on category buttons should also face west so it doesn't go off the screen.
        // Same with gear bonus buttons.
        if (bShouldFaceEast)
        {
            UIManagerScript.UIObject possibleFocusObject = null;
            if (currentFocusedShadowObject == tab_ToggleToGear.myUIObject || currentFocusedShadowObject == tab_ToggleToBonuses.myUIObject)
            {
                bShouldFaceEast = false;
            }
            else
            {
                for (int iBtnIdx = 0; iBtnIdx < categoryButtons.Count; iBtnIdx++)
                {
                    if (currentFocusedShadowObject == categoryButtons[iBtnIdx].myUIObject)
                    {
                        bShouldFaceEast = false;
                        break;
                    }
                }
            }

            if (!bShouldFaceEast)
            {
                float multiplier = Screen.width / 1920f;
                float fButtonWidth = ((RectTransform)currentFocusedShadowObject.gameObj.transform).sizeDelta.x * multiplier; //because tangledeep
                UIManagerScript.ChangeUIFocusAndAlignCursor(currentFocusedShadowObject, fButtonWidth, 0f);
                cb.SetFacing(Directions.WEST);
            }
        }

        if (bShouldFaceEast)
        {
            cb.SetFacing(Directions.EAST);
        }

    }

    public void RefreshItemInfoForSelectedButton()
    {
        //Update our tooltip if we have a selectedItem and we're not in the submenu
        if (CursorOwnershipState == EImpactUICursorOwnershipState.normal)
        {
            UIManagerScript.UIObject currentFocus = UIManagerScript.uiObjectFocus;

            if (currentFocus != null && currentFocus.gameObj != null)
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
    }


    //Change color of food we can't eat, and add (X) to multi-quantity stacks
    public void Action_UpdateContainedItemName(Switch_InvItemButton btn, int[] iArray, string[] sArray)
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

        //However! We might change the color of the name of the item
        //int the button, or add other information as well.
        string strDisplayName = itemFromInventory.displayName;

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


    //return TRUE if the item is...
    // ...in the current category
    // ...allowed by the filter list on the right
    protected override bool AllowItemInItemListSpecialConditions(Item itm)
    {
        //don't draw items in the item column that are also in the weapon hotbar.
        if (equippedGearButtons.Any(btn => btn.GetContainedData() == itm))
        {
            return false;
        }

        //if the item is being dragalagged around, don't draw it in the list
        if (itm == UIManagerScript.GetHeldGenericObject())
        {
            return false;
        }

        //check category
        switch (filterSelectedCategory)
        {
            case GearFilters.WEAPON:
                if (!(itm is Weapon))
                {
                    return false;
                }
                break;

            case GearFilters.ARMOR:
                if (!(itm is Armor))
                {
                    return false;
                }
                break;
            case GearFilters.ACCESSORY: // Also allow emblems...?
                if (!(itm is Accessory) && !(itm is Emblem))
                {
                    return false;
                }
                break;
            case GearFilters.OFFHAND:
                if (!(itm is Offhand))
                {
                    return false;
                }
                break;
        }

        //if the right hand ViewAll is checked, then accept this item
        if (UIManagerScript.itemFilterTypes[GetFilterIndexForViewAll()])
        {
            return true;
        }

        //otherwise filter based on rarity
        /*
         * 
         * public enum Rarity { COMMON, UNCOMMON, MAGICAL, ANCIENT, ARTIFACT, LEGENDARY, GEARSET, COUNT };
           public enum GearFilters { WEAPON, OFFHAND, ARMOR, ACCESSORY, COMMON, MAGICAL, LEGENDARY, GEARSET, VIEWALL, FAVORITES, COUNT }
         * 
         * GearFilters   |   Rarity
         * COMMON           COMMON 
         * MAGICAL          UNCOMMON, MAGICAL, ANCIENT, ARTIFACT
         * LEGENDARY        LEGENDARY
         * GEARSET          GEARSET
         * 
         */
        int itemRarity = (int)itm.rarity;

        if (UIManagerScript.itemFilterTypes[(int)GearFilters.COMMON] &&
            itemRarity == (int)Rarity.COMMON)
        {
            return true;
        }
        if (UIManagerScript.itemFilterTypes[(int)GearFilters.MAGICAL] &&
            itemRarity >= (int)Rarity.UNCOMMON &&
            itemRarity <= (int)Rarity.ARTIFACT)
        {
            return true;
        }
        if (UIManagerScript.itemFilterTypes[(int)GearFilters.LEGENDARY] &&
            itemRarity == (int)Rarity.LEGENDARY)
        {
            return true;
        }
        if (UIManagerScript.itemFilterTypes[(int)GearFilters.GEARSET] &&
            itemRarity == (int)Rarity.GEARSET)
        {
            return true;
        }
        if (UIManagerScript.itemFilterTypes[(int)GearFilters.FAVORITES])
        {
            if (itm.favorite)
            {
                return true;
            }
        }

        return false;
    }

    public void OpenTooltipSubmenuViaWeaponHotbar(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        OpenTooltipSubmenu(args, inputSource);
        genericTooltip.iWeaponHotbarIdx = args[0];
    }

    //Handles the submenu for equipment needs, which are different from inventory needs!
    public override void OpenTooltipSubmenu(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //Hey wait! 
        if (CheckForFakeyDrop())
        {
            return;
        }

        //If we have nothing selected, we are clicking on an empty item slot
        if (genericTooltip == null)
        {
            return;
        }

        // AA - If the tooltip is INACTIVE it might not be null, but there still shouldn't be any item info displayed
        if (!genericTooltip.CheckIfActive())
        {
            return;
        }

        genericTooltip.ClearSubmenu();
        Item targetItem = genericTooltip.GetTargetItem();

        //If targetItem is null here, then the tooltip has nothing selected and we are 
        //likely clicking on a space with no item.
        if (targetItem == null)
        {
            return;
        }

        int itemID = targetItem.actorUniqueID;

        //based on this item, decide what buttons to show
        bool bItemIsEquipped = GameMasterScript.heroPCActor.myEquipment.IsEquipped(targetItem);

        /*
         *  If the weapon is in a hotbar
         *      * Switch To
         *      * Equip to Offhand
         *      * Unequip
         *      * Drop
         *      * Mark As Favorite
         * 
         *  If otherwise equipped
         *      * Unequip
         *      * Equip to Offhand
         *      * Drop
         *      * Mark As Favorite
         *  
         *  Otherwise
         * 
         *      If accessory
         *          * Accesory Slot 1
         *          * Accesory Slot 2
         *          * Drop
         *          * Mark As Favorite    
         *          
         *      if armor 
         *          * Equip
         *          * Drop
         *          * Mark As Favorite    
         *      
         *      if offhandable
         *          * Equip Offhand
         *          
         *          if offhandable and equipped
         *          * Pair With Weapon
         *          
         *      if weapon
         *          * Set Hotbar 1
         *          * Set Hotbar 2
         *          * Set Hotbar 3
         *          * Set Hotbar 4
         *          * Drop
         *          * Mark As Favorite    
         */

        UIManagerScript.PlayCursorSound("UITick");

        int iIdxInHotbar = UIManagerScript.GetWeaponHotbarIndex(targetItem as Weapon);
        bool bWeaponInHotbar = iIdxInHotbar >= 0;

        //We may check the item-as-weapon in multiple cases.

        Weapon weapon = targetItem as Weapon;
        Equipment eq = targetItem as Equipment;
        bool bCanBeOffhand = eq.IsOffhandable();
        bool bIsFists = GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(targetItem, onlyActualFists: true);

        if (bWeaponInHotbar)
        {
            //add switch to
            genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_switchto"), genericTooltip.SwitchToHotbarWeapon, new int[] { iIdxInHotbar });

            //if not equipped, but in the hotbar, add the option to remove it from the hotbar
            if (!bItemIsEquipped && !bIsFists)
            {
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_unequip"), genericTooltip.RemoveWeaponFromHotbar, new int[] { itemID });
            }

        }
        if (bItemIsEquipped)
        {
            //add unequip
            genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_unequip"), genericTooltip.UnequipItem, new int[] { itemID });

            if (bCanBeOffhand)
            {
                //it IS equipped, but NOT in the hotbar, so it must be in the offhand slot.
                if (!bWeaponInHotbar)
                {
                    //add offhand

                    string buttonlabel = StringManager.GetString("ui_button_pairwith").ToUpperInvariant();

                    if (eq.CheckIfPairedWithSpecificItem(GameMasterScript.heroPCActor.myEquipment.GetWeapon()))
                    {
                        buttonlabel = StringManager.GetString("ui_button_unpairwith").ToUpperInvariant();
                    }

                    genericTooltip.AddDynamicButton(buttonlabel, genericTooltip.PairWithWeapon, new int[] { itemID });
                }
                else if (!GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(eq, onlyActualFists: true)) //otherwise, since it is not in the offhand slot, we can offer to put it there.
                {
                    //add offhand
                    genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipoff"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT });
                }
            }
        }
        else
        {
            //if accessory
            if (targetItem is Accessory)
            {
                // add slot 1 and 2
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipacc1"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1 });
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipacc2"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2 });
            }
            //if armor
            else if (targetItem is Armor)
            {
                // add equip 
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipgear"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ARMOR_SLOT });
            }
            //if emblem
            else if (targetItem is Emblem)
            {
                // add equip 
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipgear"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_EMBLEM_SLOT });
            }
            //if offhandable
            if (bCanBeOffhand)
            {
                //add offhand
                genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_equipoff"), genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT });
            }

            //if weapon
            if (weapon != null)
            {
                // Set Hotbar 1
                // Set Hotbar 2
                // Set Hotbar 3
                // Set Hotbar 4               
                for (int t = 0; t < 4; t++)
                {
                    StringManager.SetTag(0, (t + 1).ToString());
                    string strEquip = StringManager.GetString("ui_button_equipslotnum");
                    //strEquip = strEquip.Replace("^tag1^", (t + 1).ToString());
                    genericTooltip.AddDynamicButton(strEquip, genericTooltip.EquipItem, new int[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1 + t });
                }
            }

        }

        //and almost always
        //
        // add drop
        // add mark as favorite
        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(targetItem, onlyActualFists: true))
        {
            if (UnityEngine.Random.value < 0.1f)
            {
                Switch_InvItemButton dropButton = genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_disarm"), genericTooltip.EatItem, new int[] { itemID });
                dropButton.ToggleButton(true);
            }
        }
        else
        {
            genericTooltip.AddDynamicButton(StringManager.GetString("ui_button_drop"), genericTooltip.DropItem, new int[] { itemID });

            string strFav = StringManager.GetString(targetItem.favorite ? "ui_button_unfavorite" : "ui_button_favorite");
            genericTooltip.AddDynamicButton(strFav, genericTooltip.MarkItemFavorite, new int[] { itemID });

            string strTrash = StringManager.GetString(targetItem.vendorTrash ? "ui_button_nottrash" : "ui_button_trash");
            genericTooltip.AddDynamicButton(strTrash, genericTooltip.MarkItemTrash, new int[] { itemID });
        }


        genericTooltip.ShowSubmenu();
        genericTooltip.SetActionCloseSubmenu(SubmenuClosed);
        CursorOwnershipState = EImpactUICursorOwnershipState.tooltip_has_cursor;

    }

    public override Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj)
    {
        Switch_InvItemButton retButton = itemColumn.GetButtonContaining(obj);
        if (retButton == null)
        {
            foreach (Switch_InvItemButton btn in equippedGearButtons)
            {
                if (btn.GetContainedData() == obj)
                {
                    return btn;
                }
            }
        }
        return retButton;
    }

    //Returns TRUE if the input has been handled and there's no need to process it further
    public override bool HandleInput(Directions dInput)
    {
        //Doing this out of order as we want to grab directional input and 
        //simulate the scroll wheel with certain button presses.
        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;
        ControllerType lastActiveControllerType = ReInput.controllers.GetLastActiveControllerType();

        if (playerInput.GetAxis("Scroll UI Boxes Vertical") != 0f)
        {
            float modifiedAmount = playerInput.GetAxis("Scroll UI Boxes Vertical") * TDInputHandler.CONTROLLER_STICKSCROLL_SPEEDMOD * Time.deltaTime;
            // scroll eq bonus area
            float scrollbarValue = scrollbar_GearBonusArea.value;
            scrollbarValue += modifiedAmount;
            scrollbarValue = Mathf.Clamp(scrollbarValue, 0f, 1f);
            scrollbar_GearBonusArea.value = scrollbarValue;

            if (coroutine_GearBonuses != null) // #todo - is this pricey to check once per frame?
            {
                StopCoroutine(coroutine_GearBonuses);
                coroutine_GearBonuses = null;
            }
        }

        if (playerInput.GetButtonDown("Jump to Searchbar"))
        {
            eqSearchbar.Select();
            eqSearchbar.ActivateInputField();
            return true;
        }

        if (TDInputHandler.IsCompareAlternateButtonDown() || TDInputHandler.IsCompareAlternateButtonUp())
        {
            RefreshItemInfoForSelectedButton();
            return true;
        }

        if (base.HandleInput(dInput))
        {
            return true;
        }




        //Weapon swapping and hotkeys
        //Don't do these with the gamepad, because those keys are bound to swapping the UI tab
        if (lastActiveControllerType != ControllerType.Joystick)
        {
            if (playerInput.GetButtonDown("Cycle Weapons Right"))
            {
                UIManagerScript.CycleWeapons(1);
                UpdateContent();
                return true;
            }
            if (playerInput.GetButtonDown("Cycle Weapons Left"))
            {
                UIManagerScript.CycleWeapons(-1);
                UpdateContent();
                return true;
            }
        }

        Item highlightedItem = genericTooltip != null ? genericTooltip.GetTargetItem() : null;

if (!PlatformVariables.GAMEPAD_ONLY)
{
        for (int t = 0; t < 4; t++)
        {
            if (playerInput.GetButtonDown("Switch to Weapon " + (t + 1)))
            {
                UIManagerScript.SwitchActiveWeaponSlot(t, false);
                UpdateContent();
                return true;
            }

            //Hotbar slot keys 1-4 are checked in the equipscreen to put a weapon in a hotbar slot
            if (playerInput.GetButtonDown("Use Hotbar Slot " + (t + 1)) && highlightedItem is Weapon)
            {
                genericTooltip.EquipItem(new[] { highlightedItem.actorUniqueID, t }, Switch_InvItemButton.ELastInputSource.keyboard_or_gamepad);
                UpdateContent();
                return true;
            }
        }
}




        if (playerInput.GetButtonDown("Unequip Item") && highlightedItem != null)
        {
            genericTooltip.UnequipItem(new[] { highlightedItem.actorUniqueID }, Switch_InvItemButton.ELastInputSource.keyboard_or_gamepad);
            UpdateContent();
            return true;
        }

        if (playerInput.GetButtonDown("Equip Item") && highlightedItem != null)
        {
            genericTooltip.EquipItemAndGuessBestSlot(new[] { highlightedItem.actorUniqueID }, Switch_InvItemButton.ELastInputSource.keyboard_or_gamepad);
            UpdateContent();
            return true;
        }

        return false;
    }

    //fdelta comes in here as a +/- value which we should normalize
    void ScrollGearBonusArea(float fDelta)
    {
        fDelta = 0.2f * (fDelta > 0f ? 1f : -1f);
        scrollbar_GearBonusArea.value = Mathf.Clamp(scrollbar_GearBonusArea.value + fDelta, 0f, 1f);
    }


    void UpdateGearBonusText()
    {
        //scroll around slowly if the gear bonus text is too big
        //and the mouse cursor isn't there
        /* if (uiObject_GearBonusArea != UIManagerScript.uiObjectFocus &&
            coroutine_GearBonuses == null )
        {
            coroutine_GearBonuses = StartCoroutine(SlowlyScrollGearBonuses());
        }
        else if (uiObject_GearBonusArea == UIManagerScript.uiObjectFocus)
        {
            if (coroutine_GearBonuses != null)
            {
                StopCoroutine(coroutine_GearBonuses);
            }
            coroutine_GearBonuses = null;
        } */

        string txt = "";
        EquipmentBlock eb = GameMasterScript.heroPCActor.myEquipment;

        float[] statMods = new float[(int)StatTypes.COUNT];
        for (int i = 0; i < statMods.Length; i++)
        {
            statMods[i] = 0.0f;
        }

        float changeCritChance = 0f;
        float changeCritDamage = 0f;
        float totalDodge = GameMasterScript.heroPCActor.myEquipment.GetDodgeFromArmor();

        Dictionary<string, int> modDescriptions = new Dictionary<string, int>();


        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (eb.equipment[i] != null)
            {
                Equipment eq = eb.equipment[i];

                bool skipWeaponProperties = false;

                if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(eq, onlyActualFists: true)) continue;

                if ((EquipmentSlots)i == EquipmentSlots.OFFHAND)
                {
                    if (eq.itemType == ItemTypes.WEAPON)
                    {
                        Weapon w2 = eq as Weapon;
                        if (w2.weaponType == GameMasterScript.heroPCActor.myEquipment.GetWeaponType())
                        {
                            skipWeaponProperties = true;
                        }
                    }
                }

                if (eq.itemType == ItemTypes.WEAPON && !skipWeaponProperties)
                {
                    Weapon wpn = eq as Weapon;
                    if (!String.IsNullOrEmpty((Weapon.weaponProperties[(int)wpn.weaponType])))
                    {
                        //bonuses.Add(Weapon.weaponProperties[(int)wpn.weaponType]);
                        txt += "* " + UIManagerScript.orangeHexColor + Weapon.weaponProperties[(int)wpn.weaponType] + "</color>\n";
                    }
                }

                if (eq.mods != null)
                {
                    foreach (MagicMod mm in eq.mods)
                    {
                        if (mm.noDescChange) continue;

                        bool anyStatChanges = false;

                        foreach (StatusEffect se in mm.modEffects)
                        {
                            foreach (EffectScript eff in se.listEffectScripts)
                            {
                                if (eff.effectType == EffectType.CHANGESTAT)
                                {
                                    ChangeStatEffect cse = eff as ChangeStatEffect;
                                    statMods[(int)cse.stat] += cse.effectPower; // power could be stored here
                                    statMods[(int)cse.stat] += cse.baseAmount; // or here (probably here)
                                    anyStatChanges = true;
                                }
                                else if (eff.effectType == EffectType.ALTERBATTLEDATA)
                                {
                                    AlterBattleDataEffect abde = eff as AlterBattleDataEffect;
                                    if (abde.changeCritChance != 0f)
                                    {
                                        changeCritChance += abde.changeCritChance;
                                        anyStatChanges = true;
                                    }
                                    if (abde.changeCritDamage != 0f)
                                    {
                                        changeCritDamage += abde.changeCritDamage;
                                        anyStatChanges = true;
                                    }
                                }
                                else if (eff.effectType == EffectType.ATTACKREACTION)
                                {
                                    AttackReactionEffect are = eff as AttackReactionEffect;
                                    if (are.alterAccuracyFlat != 0f)
                                    {
                                        totalDodge += (-1f * are.alterAccuracyFlat);
                                        anyStatChanges = true;
                                    }
                                }
                            }
                        }



                        if (anyStatChanges) continue;

                        if (!String.IsNullOrEmpty(mm.description))
                        {
                            string txtToUse = String.Copy(mm.description);
                            if (eq.itemType == ItemTypes.EMBLEM)
                            {
                                Emblem emb = eq as Emblem;
                                StringManager.SetTag(0, CharacterJobData.GetJobDataByEnum((int)emb.jobForEmblem).DisplayName);
                                txtToUse = CustomAlgorithms.ParseLiveMergeTags(txtToUse);
                            }
                            string toAdd = "* <color=yellow>" + txtToUse + "</color>";

                            if (modDescriptions.ContainsKey(toAdd))
                            {
                                modDescriptions[toAdd]++;
                            }
                            else
                            {
                                modDescriptions.Add(toAdd, 1);
                            }

                        }
                    }
                }
            }
        }

        for (int i = 0; i < statMods.Length; i++)
        {
            if (statMods[i] != 0f)
            {
                txt += "* " + UIManagerScript.greenHexColor + "+" + (int)statMods[i] + " " + StatBlock.statNames[i] + "</color>\n";
            }
        }
        if (changeCritChance != 0f)
        {
            StringManager.SetTag(0, "+" + (int)(changeCritChance * 100f));
            txt += "* " + UIManagerScript.cyanHexColor + StringManager.GetString("ui_crit_chance_percent") + "</color>\n";
        }
        if (changeCritDamage != 0f)
        {
            StringManager.SetTag(0, "+" + (int)(changeCritDamage * 100f));
            txt += "* " + UIManagerScript.cyanHexColor + StringManager.GetString("ui_crit_damage_percent") + "</color>\n";
        }
        if (totalDodge != 0f)
        {
            StringManager.SetTag(0, "+" + (int)totalDodge);
            txt += "* " + UIManagerScript.cyanHexColor + StringManager.GetString("ui_dodge_chance_percent") + "</color>\n";
        }

        foreach (string key in modDescriptions.Keys)
        {
            int amount = modDescriptions[key];
            if (amount > 1)
            {
                txt += key + " (x" + amount + ")\n";
            }
            else
            {
                txt += key + "\n";
            }
        }

        txt_GearBonuses.text = txt;
    }

    IEnumerator SlowlyScrollGearBonuses()
    {
        float fScrollbarUnitsPerSecond = 0.2f;

        while (true)
        {
            yield return new WaitForSeconds(5.0f);

            while (scrollbar_GearBonusArea.value < 1.0f)
            {
                scrollbar_GearBonusArea.value = Mathf.Clamp(scrollbar_GearBonusArea.value + Time.deltaTime * fScrollbarUnitsPerSecond, 0f, 1f);
                yield return null;
            }

            yield return new WaitForSeconds(5.0f);

            while (scrollbar_GearBonusArea.value > 0.0f)
            {
                scrollbar_GearBonusArea.value = Mathf.Clamp(scrollbar_GearBonusArea.value - Time.deltaTime * fScrollbarUnitsPerSecond, 0f, 1f);
                yield return null;
            }
        }
    }



    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
        /*
        ISelectableUIObject obj = itemColumn.GetContainedDataFrom(iButtonIndex);
        */
    }

    public override void StartAssignObjectToHotbarMode(ISelectableUIObject obj)
    {
        /*
        UIManagerScript.ChangeUIFocusAndAlignCursor(listVerticalHotbarButtons[0].myUIObject, listVerticalHotbarButtons[0].CursorXOffset, listVerticalHotbarButtons[0].CursorYOffset);
        CursorBounce cb = UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<CursorBounce>();
        cb.SetFacing(true);
        CursorOwnershipState = EImpactUICursorOwnershipState.vertical_hotbar_has_cursor;
        objSlotInHotbar = obj;
        */
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
        }

        return true;
    }
}

public partial class EQItemButtonScript
{
    public void HoverItemInfo_InventoryScreen_Switch(BaseEventData bte)
    {
        Debug.LogError("Don't call this!");
    }

    public void ClearItemInfo_InventoryScreen_Switch(BaseEventData bte)
    {
        Debug.LogError("Don't call this either!");
    }


    public void HoverItemInfo_Switch(BaseEventData bte)
    {
        //Don't switch item info if the tooltip has our cursor
        if (UIManagerScript.TooltipHasCursor())
        {
            return;
        }
        UIManagerScript.singletonUIMS.HoverItemInfo(myID);
    }

    public void ClearItemInfo_Switch(BaseEventData bte)
    {
        //Don't switch item info if the tooltip has our cursor
        if (UIManagerScript.TooltipHasCursor())
        {
            return;
        }
        UIManagerScript.singletonUIMS.ClearItemInfo(myID);
    }

}