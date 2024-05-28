using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// This component should only be used in the expansion!
// It is appended to the existing shop UI and lets the player view what items are being added to the crafting box.

public enum ECraftingColumnSelected { PLAYER, BOX, COUNT }

[System.Serializable]
public class CraftingScreen : ImpactUI_WithItemColumn
{
    bool initialized;

    public const int MAX_CRAFTING_ITEMS_IN_RECIPE = 10;

    public static CraftingScreen singleton;
    bool craftingUIState;

    [Header("Box Item Column")]
    public Switch_UIButtonColumn boxItemColumn;

    [Header("Labels")]
    public TextMeshProUGUI uiHeader;

    [Header("Misc Buttons")]
    public Switch_InvItemButton craftItemsButton;

    ECraftingColumnSelected columnSelected;

    public Image resultImage;
    public TextMeshProUGUI resultText;

    static Dictionary<string, int> craftItemQuantities;

    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
        throw new NotImplementedException();
    }

    public override void StartAssignObjectToHotbarMode(ISelectableUIObject content)
    {
        throw new NotImplementedException();
    }

    public override bool TryTurnOff()
    {
        gameObject.SetActive(false);
        UIManagerScript.singletonUIMS.currentFullScreenUI = null;
        UIManagerScript.bFullScreenUIOpen = false;
        UIManagerScript.PlayCursorSound("UITock");

        NPC craftingBox = DLCManager.FindCraftingBoxNPC();
        craftingBox.myAnimatable.SetAnim("TakeDamage");

        UIManagerScript.HideDialogMenuCursor();

        GuideMode.OnFullScreenUIClosed();
            
        return false;
    }

    public override void TurnOn()
    {
        FontManager.LocalizeMe(resultText, TDFonts.WHITE);

        TDSearchbar.ClearSearchTerms();

        gameObject.SetActive(true);
        InitializeDynamicUIComponents();

        UIManagerScript.allUIObjects.Clear();
        AddAllUIObjects(UIManagerScript.allUIObjects);
        UpdateContent();

        UIManagerScript.UIObject firstFocus = GetDefaultUiObjectForFocus();
        if (itemColumn.GetNumObjectsInList() > 0)
        {
            UIManagerScript.SetDefaultUIFocus(itemColumn.GetTopUIObject());
        }
        else
        {
            UIManagerScript.SetDefaultUIFocus(craftItemsButton.myUIObject);
        }

        //focus on the first button 
        UIManagerScript.ChangeUIFocusAndAlignCursor(firstFocus);

        // AA: This is to make sure the item is *actually* selected with the on-select logic, which was not happening previously
        Switch_InvItemButton switchBtn = UIManagerScript.uiObjectFocus.gameObj.GetComponent<Switch_InvItemButton>();
        if (switchBtn != null)
        {
            switchBtn.Default_OnPointerEnter(null);
        }

        //tell the manager we're open for business!
        UIManagerScript.SetWindowState(myTabType, true);

        TDScrollbarManager.SMouseExitedSpecialScrollArea();

        //Make sure our UI decides when we can focus on what
        UIManagerScript.SetFocusFunction(AllowFocus);

        GuideMode.OnFullScreenUIOpened();
    }

    protected override void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        base.AddAllUIObjects(allObjects);
        itemColumn.AddButtonsToUIObjectMasterList();
        boxItemColumn.AddButtonsToUIObjectMasterList();

        UIManagerScript.allUIObjects.Add(craftItemsButton.myUIObject);
    }

    public override void Start()
    {
        myTabType = UITabs.CRAFTING;
        singleton = this;
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            gameObject.SetActive(false);
            return;
        }
        FontManager.LocalizeMe(uiHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(itemColumn.text_TopLabel, TDFonts.WHITE);
        FontManager.LocalizeMe(boxItemColumn.text_TopLabel, TDFonts.WHITE);
        FontManager.LocalizeMe(craftItemsButton.txtLabel, TDFonts.BLACK);

        uiHeader.text = StringManager.GetString("exp_ui_craftheader");
        itemColumn.text_TopLabel.text = StringManager.GetString("exp_ui_playeritemheader");
        boxItemColumn.text_TopLabel.text = StringManager.GetString("exp_ui_boxitemheader");
        craftItemsButton.SetDisplayText(StringManager.GetString("exp_ui_craftitems"));

        craftItemsButton.myUIObject = new UIManagerScript.UIObject();
        craftItemsButton.myUIObject.gameObj = craftItemsButton.gameObject;
        craftItemsButton.myUIObject.mySubmitFunction = craftItemsButton.SubmitFunction_OnClickOrPress;
        craftItemsButton.onClickAction = TryCraftItems;
        craftItemsButton.SetEventListener(EventTriggerType.PointerClick, craftItemsButton.Default_OnClick);

        craftItemQuantities = new Dictionary<string, int>();
    }

    public static void SetCraftingUIState(bool state)
    {
        if (state) GuideMode.OnFullScreenUIOpened();
        else
        {
            if (singleton.craftingUIState) GuideMode.OnFullScreenUIClosed();
        }

        singleton.craftingUIState = state;
        singleton.gameObject.SetActive(state);
    }

    // Update is called once per frame
    public override void Update()
    {
        if (!bHasBeenInitialized)
        {
            return;
        }
    }

    public override bool InitializeDynamicUIComponents()
    {
        if (bHasBeenInitialized)
        {
            return false;
        }

        //The vertical main column that holds all the items and scrolls around
        itemColumn.CreateStartingContent(0);

        /* foreach(Switch_InvItemButton btn in itemColumn.GetAllButtons())
        {
            
        } */

        itemColumn.SetActionForEventOnButtons(EventTriggerType.PointerEnter, SetTooltipViaButtonByIDInPlayerColumn);
        itemColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, TransferItemToBox);        
        itemColumn.onCursorPositionInListUpdated = OnColumnUpdateFocus;

        // And the crafting box column too
        boxItemColumn.CreateStartingContent(0);
        boxItemColumn.SetActionForEventOnButtons(EventTriggerType.PointerEnter, SetTooltipViaButtonByIDInBoxColumn);
        boxItemColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, TransferItemToPlayer);
        boxItemColumn.onCursorPositionInListUpdated = OnColumnUpdateFocus;

        ConnectButtons();

        bHasBeenInitialized = true;

        return true;
    }

    void TransferItemToBox(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //grab the button we passed in
        Switch_InvItemButton btn = itemColumn.GetButtonInList(args[0]);

        // get item contained therein
        Item itemToTransfer = btn.GetContainedData() as Item;

#if UNITY_EDITOR
        Debug.Log("Transferring " + itemToTransfer.actorRefName + " " + itemToTransfer.GetQuantity() + " to box");
#endif

        DLCManager.FindCraftingBoxNPC().myInventory.AddItemRemoveFromPrevCollection(itemToTransfer, true, 1);

        UpdateContent();
    }

    void TransferItemToPlayer(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //grab the button we passed in
        Switch_InvItemButton btn = boxItemColumn.GetButtonInList(args[0]);

        // get item contained therein
        Item itemToTransfer = btn.GetContainedData() as Item;

#if UNITY_EDITOR
        Debug.Log("Transferring " + itemToTransfer.actorRefName + " " + itemToTransfer.GetQuantity() + " to player");
#endif

        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itemToTransfer, true, 1);

        UpdateContent();
    }

    //Take the lists of buttons and connect them to each other. One-time initialization
    void ConnectButtons()
    {
        if (bHasBeenInitialized)
        {
            return;
        }

        itemColumn.neighborRight = craftItemsButton.myUIObject;
        itemColumn.neighborLeft = boxItemColumn.btn_first.myUIObject;

        boxItemColumn.neighborRight = itemColumn.btn_first.myUIObject;
        boxItemColumn.neighborLeft = craftItemsButton.myUIObject;

        craftItemsButton.myUIObject.neighbors[(int)Directions.WEST] = itemColumn.btn_first.myUIObject;
        craftItemsButton.myUIObject.neighbors[(int)Directions.EAST] = boxItemColumn.btn_first.myUIObject;
    }

    public override void UpdateContent(bool adjustSorting = true)
    {
        // itemButtons == our list of buttons on screen to display items
        List<ISelectableUIObject> playerItemList = CreateItemListFromInventoryOfThisTypeThatMatchesFilters<Item>(GameMasterScript.heroPCActor.myInventory);

        NPC craftingBox = DLCManager.FindCraftingBoxNPC();

        // box is an NPC we are talking to
        List<ISelectableUIObject> boxItemList = CreateItemListFromInventoryOfThisTypeThatMatchesFilters<Item>(craftingBox.myInventory, ignoreSearchBar:true);

        //Now we have a list of all pertinent items!
        itemColumn.PlaceObjectsInList(playerItemList, false);
        boxItemColumn.PlaceObjectsInList(boxItemList, false);

        //Change the button display names based on quantity or hunger
        itemColumn.AdjustButtonInformationViaAction(Action_UpdateContainedItemName);
        boxItemColumn.AdjustButtonInformationViaAction(Action_UpdateContainedItemName);

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

    //Add (X) to multi-quantity stacks
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

        //if it is a favorite <3 add a * next to it
        if (itemFromInventory.favorite)
        {
            strDisplayName += UIManagerScript.favoriteStar;
        }
        if (itemFromInventory.vendorTrash)
        {
            strDisplayName += UIManagerScript.vendorTrashMark;
        }

        //If we have more than one, add (#) to the end
        strDisplayName += itemFromInventory.GetQuantityText();

        //hooray
        btn.SetDisplayText(strDisplayName);

    }

    public static void OpenCraftingUI()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return;

        CraftingRecipeManager.Initialize();

        UIManagerScript.bFullScreenUIOpen = true;

        UIManagerScript.PlayCursorSound("OpenDialog");

        UIManagerScript.singletonUIMS.currentFullScreenUI = UIManagerScript.singletonUIMS.dictFullScreenUI[UITabs.CRAFTING];
        UIManagerScript.singletonUIMS.currentFullScreenUI.TurnOn();

        singleton.resultImage.enabled = false;
        singleton.resultText.text = "";
    }

    void SetTooltipViaButtonByIDInPlayerColumn(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        columnSelected = ECraftingColumnSelected.PLAYER;
        SetTooltipViaButtonByID(args, inputSource);
    }

    void SetTooltipViaButtonByIDInBoxColumn(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        columnSelected = ECraftingColumnSelected.BOX;
        SetTooltipViaButtonByID(args, inputSource);
    }

    //Looks inside a button for an item or ability to put in a tooltip
    protected override void SetTooltipViaButtonByID(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        // Which list are we in tho?
        Switch_UIButtonColumn columnToUse = (columnSelected == ECraftingColumnSelected.PLAYER) ? itemColumn : boxItemColumn;

        //grab the button we passed in
        Switch_InvItemButton btn = columnToUse.GetButtonInList(args[0]);
        DisplayItemInfo(btn.GetContainedData() as Item, null, false);
        FocusAndBounceButton(btn);
    }

    void TryCraftItems(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        craftItemQuantities.Clear();

        List<Item> itemsToUse = DLCManager.FindCraftingBoxNPC().myInventory.GetInventory();
        List<Item> itemsNotUsed;
        List<Item> returnItems = CraftingRecipeManager.FindAnyValidRecipeAndCreate(itemsToUse, out itemsNotUsed);

        bool anySuccess = false;

        string txt = "";

        while (returnItems != null)
        {
            anySuccess = true;
            singleton.resultImage.enabled = true;
            // Success!
            Sprite retSprite = null;
            foreach (Item itm in returnItems)
            {
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itm, true);
                retSprite = itm.GetSpriteForUI();
                StringManager.SetTag(0, itm.displayName);
                GameLogScript.LogWriteStringRef("exp_log_item_crafted");
                txt += itm.displayName + itm.GetQuantityText() + "\n";

                int curValue;
                if (!craftItemQuantities.TryGetValue(itm.displayName, out curValue))
                {
                    craftItemQuantities.Add(itm.displayName, itm.GetQuantity());
                }
                else
                {
                    curValue += itm.GetQuantity();
                    craftItemQuantities[itm.displayName] = curValue;
                }
            }

            resultImage.sprite = retSprite;
            resultText.text = txt;

            foreach(Item itm in itemsNotUsed)
            {
                GameMasterScript.heroPCActor.myInventory.AddItem(itm, true);
            }

            returnItems = null;
        }
        
        if (!anySuccess)
        {
            singleton.resultImage.enabled = false;
            resultText.text = "";
            UIManagerScript.PlayCursorSound("Error");
            // Nothing possible!
        }
        else
        {
            UIManagerScript.PlayCursorSound("CookingSuccess");


            GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Frogcrafting_Used();

            DLCManager.FindCraftingBoxNPC().myInventory.ClearInventory();
            UpdateContent();
        }
    }
}

public partial class UIManagerScript
{
    public static CraftingScreen craftingScreen;

}