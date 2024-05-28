using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rewired;

public enum PageScroll
{
    UP = -1,
    DOWN = 1
}

public enum EImpactUICursorOwnershipState
{
    normal = 0,
    tooltip_has_cursor,
    vertical_hotbar_has_cursor,
    focus_column_has_cursor,
    dragalaggin,
}

public abstract class ImpactUI_Base : MonoBehaviour
{
    [Header("Initialization Options")]
    public bool DisableGameObjectOnStart;
    protected UITabs myTabType;

    [Header("Tooltip Anchors")]
    [Tooltip("The top left position of the tooltip")]
    public GameObject TooltipAnchorObjectTopLeft;
    [Tooltip("The top right position of the tooltip")]
    public GameObject TooltipAnchorObjectTopRight;
    [Tooltip("The top position of the tooltip's button based submenu. The X value is ignored, as the buttons are centered in the tooltip")]
    public GameObject TooltipAnchorObjectSubmenuLeft;


    //Set to true when dynamic elements have been created
    protected bool bHasBeenInitialized;

    //used in children that are swapping hotbars around
    protected float fLastHotbarSwapTime;

    //we should have access to this, it is our friend
    protected UIGenericItemTooltip genericTooltip;

    //Used to determine if some area of the UI has the cursor locked down,
    //and shouldn't give it up if the mouse moves elsewhere.
    protected EImpactUICursorOwnershipState CursorOwnershipState;

    //If our UI has columns, one of them might have cursor. 
    private Switch_UIButtonColumn currentFocusedColumn;

    //What are we driving around with touch/mouse
    private GameObject cursorDragger;

    //Where did the most recent drag originate from
    private GameObject dragOriginGO;

    //What is our MOUSE looking at, during a drag -- even if we don't make this
    //the uiObjectFocus because it's locked otherwise. We might need this for FakeyDrops
    private GameObject dragCurrentTargetGO;

    private bool bInDragObjectMode;

    protected InventoryUIState lastFilterState;

    //We may decide to open a UI in a special filter state, such as the SnackBag. 
    //When we do so, we don't want to override the saved filter state the user had set for
    //their screen prior.
    protected bool bDoNotStoreFilterStateOnExit;


    public virtual void Awake()
    {

    }

    public UIGenericItemTooltip GetTooltipIfActive()
    {
        if (genericTooltip != null && genericTooltip.CheckIfActive())
        {
            return genericTooltip;
        }
        return null;
    }

    public virtual void Start()
    {
        if (DisableGameObjectOnStart)
        {
            gameObject.SetActive(false);
        }   
    }

    protected virtual void InitializeFilterState()
    {
        lastFilterState = new InventoryUIState(false);
        lastFilterState.ClearAllBut(GetFilterIndexForViewAll());
    }

    //When cancel or escape is pressed, we may not want to immediately close the UI.
    //For instance, we could be in a tooltip mode, or other sub-mode, and we simply want
    //to back out. When a close command is sent to us, we should intercept it here
    public abstract bool TryTurnOff();

    public virtual void TurnOn()
    {
        if (lastFilterState == null)
        {
           InitializeFilterState();
        }

        TDSearchbar.ClearSearchTerms();

        gameObject.SetActive(true);
        InitializeDynamicUIComponents();

        //tell the manager we're open for business!
        UIManagerScript.SetWindowState(myTabType, true);

        //this is the top of the screen bar, probably
        UIManagerScript.singletonUIMS.ShowUINavigation();
        UIManagerScript.UpdateUINavHighlights();

        TDScrollbarManager.SMouseExitedSpecialScrollArea();

        //Make sure our UI decides when we can focus on what
        UIManagerScript.SetFocusFunction(AllowFocus);

        if (cursorDragger != null)
        {
            cursorDragger.SetActive(false);
        }

        //It is possible that we don't want to change our sorting from last time. If that's the case,
        //don't set the filters, we'll do it elsewhere.
        if (UIManagerScript.messageForFullScreenUI == UIManagerScript.EMessageForFullScreenUI.none)
        {
            //Update the UIManager's filters based on the last inventory state open
            SetFiltersViaState(lastFilterState);
        }        
    }

    public virtual void TurnOff()
    {
        //save filter state, maybe write into a function later?
        if (bDoNotStoreFilterStateOnExit)
        {
            //don't store this time, but clear the flag so next time we will.
            bDoNotStoreFilterStateOnExit = false;
        }
        //otherwise, store our settings
        else if( lastFilterState != null )
        {
            for (int i = 0; i < UIManagerScript.itemFilterTypes.Length; i++)
            {
                lastFilterState.filterStates[i] = UIManagerScript.itemFilterTypes[i];
            }

            lastFilterState.sortType = UIManagerScript.invSortItemType;
        }

        if (genericTooltip != null)
        {
            genericTooltip.Hide();
        }

        gameObject.SetActive(false);
        UIManagerScript.ClearFocusFunction();
        UIManagerScript.SetWindowState(myTabType, false);
        UIManagerScript.uiObjectFocus = null;

        if (cursorDragger != null)
        {
            cursorDragger.SetActive(false);
        }

        if (UIManagerScript.singletonUIMS.uiDialogMenuCursor != null)
        {
            var cb = UIManagerScript.singletonUIMS.uiDialogMenuCursorBounce;
            cb.SetFacing(Directions.EAST);
        }

        UIManagerScript.ClearAllHeldGenericObjects();

    }

    public virtual bool ToggleActive(bool forceClose = false)
    {
        if (gameObject.activeSelf || forceClose)
        {
            TurnOff();
            return false;
        }

        TurnOn();
        return true;
    }

    public UITabs GetUIType()
    {
        return myTabType;
    }

    protected virtual void SetFiltersViaState(InventoryUIState newState)
    {
        for (int i = 0; i < UIManagerScript.itemFilterTypes.Length; i++)
        {
            UIManagerScript.itemFilterTypes[i] = newState.filterStates[i];
        }
    }

    protected virtual void SetFiltersViaStateAndSortInventory(InventoryUIState newState)
    {
        SetFiltersViaState(newState);

        if (UIManagerScript.messageForFullScreenUI == UIManagerScript.EMessageForFullScreenUI.inventory_as_snack_bag) return;

        //Sort the inventory without making any noise or reversing the sort order
        UIManagerScript.singletonUIMS.SortPlayerInventory((int)newState.sortType, true);
    }

    public virtual void Update()
    {
        if (cursorDragger != null && cursorDragger.activeInHierarchy)
        {
            cursorDragger.transform.position = Input.mousePosition;
        }
    }

    /// <summary>
    /// This refreshes all of the various buttons, lists, objects, etc. Shouldn't be called constantly but rather
    /// in response to a given event
    /// </summary>
    public virtual void UpdateContent(bool adjustSorting = true)
    {
        
    }

    //Called to create the dynamic lists of buttons and other objects
    //returns true if initialization should continue
    public virtual bool InitializeDynamicUIComponents()
    {
        return !bHasBeenInitialized;
    }

    //What should the UICursor focus on if we don't have a set destination?
    public abstract UIManagerScript.UIObject GetDefaultUiObjectForFocus();


    //Activates and correctly positions the generic item tooltip
    //todo: update to use IGenericSelectableUIObject interface
    public virtual void DisplayItemInfo(ISelectableUIObject itemToDisplay, GameObject refObject, bool mouseHoverSource)
    {
        if (itemToDisplay == null)
        {
            return;
        }

        Item checkAsItem = itemToDisplay as Item;
        if (checkAsItem != null)
        {
            genericTooltip = UIManagerScript.ShowGenericItemTooltip(checkAsItem);
        }
        else
        {
            //todo: Should the tooltip in the item screen be able to show job ability information?
            //genericTooltip = UIManagerScript.ShowGenericItemTooltip(itemToDisplay as AbilityScript);
            //Debug.Log("Can't check a non-item info.");
            return;
        }        
        
        genericTooltip.DisplayBorder(false);
        genericTooltip.SetPosition(TooltipAnchorObjectTopLeft.transform as RectTransform, TooltipAnchorObjectTopRight.transform as RectTransform);
        genericTooltip.ClearComparison();
        genericTooltip.transform.SetParent(transform);
        genericTooltip.transform.SetAsLastSibling();
    }


    //Make sure we don't jump focus with the mouse when we are in the middle of using
    //KB or Controller put a power in the hotbar
    public virtual bool AllowFocus(GameObject obj)
    {
        //Don't let anything in a UI under a dialog box steal focus away
        if (UIManagerScript.dialogBoxOpen)
        {
            return false;
        }

        //if we're dragging something around with mouse/touch, don't move the cursor
        if (CursorOwnershipState == EImpactUICursorOwnershipState.dragalaggin)
        {
            //buuuut keep track of this anyway cause why not
            dragCurrentTargetGO = obj;

#if UNITY_EDITOR
            if (dragCurrentTargetGO != null)
            {
                UIManagerScript.uiNavHeaderText.text = dragCurrentTargetGO.name;
            }
#endif

            return false;
        }

        return true;
    }

    //Returns TRUE if the input has been handled and there's no need to process it further
    public virtual bool HandleInput(Directions dInput)
    {
        //todo verify that we don't want to have a button that skips the juice?
        //But, if an anim is playing, just tell everybody we handled the input and it is fine.
        if (GameMasterScript.IsNextTurnPausedByAnimations())
        {
            return true;
        }

        //Don't take any input if a dialog is up
        if (UIManagerScript.dialogBoxOpen)
        {
            return false;
        }

        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;
        bool bTryingToExit = false;
        if (!UIManagerScript.PreventingOptionMenuToggle())
        {
            bTryingToExit = playerInput.GetButtonDown("Options Menu") ||
                            playerInput.GetButtonDown("Cancel") ||
                            playerInput.GetButtonDown("Toggle Menu Select");
        }

        //If we're dragging an object, the bTryingToExit flag will allow us to cancel that drag.
        if (bInDragObjectMode)
        {
            //if we let the mouse button up, it's possible that OnDrop isn't caught by Unity
            //if we're dropping on the same object we started dragging from. Booo!
            bool bMouseUpThisFrame = TDTouchControls.GetMouseButtonUp(0);

            //Seems jank to do a variable assignment here, but we could
            //(and at one time, have actually had) have a situation where
            //we have multiple checks to see if this is handled.
            bool bHandled = bTryingToExit || bMouseUpThisFrame;

            if (bHandled)
            {
                UIManagerScript.singletonUIMS.ExitDragMode();

                //Make sure we are up to date on the listing of dragged items
                UpdateContent();
                return true;
            }

        }

        if (bTryingToExit)
        {
            //This will end up checking in with us to see if we're allowed to close.
            //Our own TryClose functions might instead move us out of a modal state,
            //such as when the gamepad/kb cursor is on the tooltip submenu
            UIManagerScript.TryCloseFullScreenUI();

            //success or failure, we've handled the input
            return true;
        }

        //What if input is trying to open a different UI tab?
        UITabs attemptedTab = UIManagerScript.CheckInputForFullScreenUIToggle();

        //Is someone trying to toggle away? 
        if (attemptedTab != UITabs.NONE )
        {
            //If they are pressing my button, we should turn off.
            if (attemptedTab == myTabType)
            {
                //This skips the backing out and simply closes
                if (!UIManagerScript.PreventingOptionMenuToggle())
                {
                    return UIManagerScript.ForceCloseFullScreenUI();
                }
                else
                {
                    return true; // Make sure to swallow input if we cannot yet close this UI
                }
                
            }
            //Otherwise, a tab was pressed that isn't us. We need to tell the UIManager to close us, and then 
            //open a new one.
            return UIManagerScript.OpenFullScreenUI(attemptedTab);
        }

        UIManagerScript.UIObject currentFocus = UIManagerScript.uiObjectFocus;
        if (currentFocus == null)
        {
            return false;
        }

        //What if someone is trying to move the cursor?
        if( !playerInput.GetButton("Diagonal Move Only"))   //don't move the cursor via controller when controller-shift is held down
        {
            if (dInput <= Directions.NORTHWEST && currentFocus.directionalActions[(int)dInput] != null )
            {
                currentFocus.directionalActions[(int) dInput](currentFocus.directionalValues[(int) dInput]);
                return true;
            }
        }

        //finally, handle push butan
        if (playerInput.GetButtonDown("Confirm"))
        {
            UIManagerScript.singletonUIMS.CursorConfirm();
            return true;
        }

        return false;
    }

    //It's possible that Unity returns OnClick for drag/drop events
    //that have the same origin. We need to #handleit
    public virtual bool CheckForFakeyDrop()
    {
        if (bInDragObjectMode &&
            dragOriginGO == dragCurrentTargetGO)
        {
            var btnItem = dragCurrentTargetGO.GetComponent<Switch_InvItemButton>();
            var btnVert = dragCurrentTargetGO.GetComponent<Switch_InvVerticalHotbarButton>();

            if (btnItem)
            {
                btnItem.Default_OnDrop(null);
            }
            if (btnVert)
            {
                btnVert.OnDrop(null);
            }

            UIManagerScript.singletonUIMS.ExitDragMode();
            return true;
        }

        return false;

    }

    //Returns true if we did something with the input.
    protected virtual bool HandleDirectionalInput(Directions dir)
    {
        UIManagerScript.UIObject currentObject = UIManagerScript.uiObjectFocus;
        UIManagerScript.UIObject nextObject = currentObject.neighbors[(int)dir];

        //if we do have one, and we're allowed to focus on it, do so.
        if (nextObject != null && AllowFocus(nextObject.gameObj))
        {
            UIManagerScript.ChangeUIFocus(nextObject, processEvent:false);
            UpdateContent();
            return true;
        }

        return false;
    }
    
    //Returns TRUE if the offset has changed
    protected bool TryChangeCurrentOffset(int iMoveDelta, int iNumButtons, int iNumItems, ref int iCurrentIndex, ref int iOffset)
    {
        //iCurrentIndex is bound between 0 and #of buttons
        //iOffset is bound between 0 and MaxItems - #of buttons
        int iMaxOffset = iNumItems - iNumButtons;

        iCurrentIndex += iMoveDelta;

        //Moving UP the list
        if (iMoveDelta < 0)
        {
            //we can't be at less than the 0th button
            if (iCurrentIndex < 0)
            {
                iCurrentIndex = 0;
                //If we are moved up past the top button, try scrolling
                if (iOffset > 0)
                {
                    //The offset can't be less than 0
                    iOffset = Math.Max(0, iOffset + iMoveDelta);

                    //We changed the offset
                    return true;
                }

                //at the top, with no offset, means we did not scroll any.
                //The cursor should bounce off the top or leave 
                return false;
            }
        }
        else
        {
            //we can't go off the bottom of the list
            if (iCurrentIndex >= iNumButtons)
            {
                iCurrentIndex = iNumButtons -1;
                //If we can scroll down the list, let's do so.
                if (iOffset < iMaxOffset)
                {
                    //Don't let our offset get any greater than the delta between how many buttons we have
                    //and how many items can go in those buttons
                    iOffset = Math.Min(iMaxOffset, iOffset + iMoveDelta);

                    //We changed the offset
                    return true;
                }

                //we pushed down at the bottom, but couldn't scroll any further
                //The cursor should bounce off or leave
                return false;
            }
        }

        //We did not scroll the pool
        return false;
    }

    //Move over to the right
    public virtual void LeaveVerticalHotbarViaGamepad(int iValue)
    {
        //This is either an abandon action or just moving back.
        //If we're holding an object in the cursor, consider it an abandon
        if (UIManagerScript.GetHeldGenericObject() != null)
        {
            UIManagerScript.ClearAllHeldGenericObjects();
            UIManagerScript.PlayCursorSound("Cancel");
        }
        else
        {
            UIManagerScript.PlayCursorSound("Move");
        }

        CursorBounce cb = UIManagerScript.singletonUIMS.uiDialogMenuCursorBounce;
        cb.SetFacing(Directions.WEST);
        UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.GetDefaultUIFocus());
        CursorOwnershipState = EImpactUICursorOwnershipState.normal;

        if (UIManagerScript.uiObjectFocus != null)
        {
            Switch_InvItemButton switchBtn = UIManagerScript.uiObjectFocus.gameObj.GetComponent<Switch_InvItemButton>();
            Switch_InvVerticalHotbarButton vertiBtn = UIManagerScript.uiObjectFocus.gameObj.GetComponent<Switch_InvVerticalHotbarButton>();
            if (switchBtn != null)
            {
                switchBtn.Default_OnPointerEnter(null);
            }
            else if (vertiBtn != null)
            {
                vertiBtn.OnPointerEnter(null);
            }
        }


        //Replace any objects that were in limbo or being held by the cursor,
        //this restores everything to match the state of the actual game data
        UpdateContent();
    }

    //Only good for children with vertical hotbars <3 
    // AA - We should redo this to return a bool based on whether the UI actually has a hotbar to swap or not
    public virtual void SwapHotbarViaGamepad()
    {

    }

    //Set an entire group of buttons to have a single neighbor in a given direction
    protected void ConnectButtonsInListToOtherButton(IList<Switch_InvItemButton> butanz, Directions dir, UIManagerScript.UIObject target)
    {
        for (int t = 0; t < butanz.Count; t++)
        {
            butanz[t].myUIObject.neighbors[(int)dir] = target;
        }
    }

    //Make sure radio buttons are toggled according to some list of tracked bool flags
    public void MaintainRadioButtonState(IList<Switch_InvItemButton> btnList, bool[] checkArray)
    {
        if (btnList == null)
        {
            return;
        }

        foreach (var btn in btnList)
        {
            btn.ToggleButton(checkArray[btn.iSubmitValue[0]]);
        }
    }

    //This is the internal call because it requires a list, and not every child will have the same list,
    //and some might have multiple lists, who knows?
    protected void SwapVerticalHotbarList(List<Switch_InvVerticalHotbarButton> swapme)
    {
        //prevent crazy mad spamming of the bar swapping
        if (Time.realtimeSinceStartup - fLastHotbarSwapTime < 0.3f)
        {
            return;
        }

        //Swap each button in the bar
        foreach (var btn in swapme)
        {
            btn.SwitchActiveButton();
        }

        //Prevent us from swapping again for a limited time, and then play a sound
        fLastHotbarSwapTime = Time.realtimeSinceStartup;
        UIManagerScript.PlayCursorSound("Tick");
    }

    //Creates a vertical hotbar for this UI. Currently, because this is not static, it won't let us create
    //a vertical hotbar for an object that isn't an ImpactUI_Base. But that feels ok for now.
    public List<Switch_InvVerticalHotbarButton> CreateVerticalHotbar(GameObject objFirstButton, int iNumButtons, int pxBetweenButtons, Switch_InvItemButton swapButton, Action swapFunction)
    {
        List<Switch_InvVerticalHotbarButton> retList = new List<Switch_InvVerticalHotbarButton>();
        RectTransform baseRT = objFirstButton.transform as RectTransform;
        int yOffset = (int)baseRT.sizeDelta.y + pxBetweenButtons;

        for (int t = 0; t < iNumButtons; t++)
        {
            Switch_InvVerticalHotbarButton newButton = null;
            if (t != 0)
            {
                newButton = Instantiate(objFirstButton, objFirstButton.transform.parent).GetComponent<Switch_InvVerticalHotbarButton>();
                newButton.transform.localScale = Vector3.one;
            }
            else
            {
                newButton = objFirstButton.GetComponent<Switch_InvVerticalHotbarButton>();
            }
            newButton.iHotbarButtonIndex = t;
            retList.Add(newButton);

            //position button correctly
            RectTransform newRT = newButton.transform as RectTransform;
            Vector2 vPos = newRT.anchoredPosition;
            vPos.y -= yOffset * t;
            newRT.anchoredPosition = vPos;

            //add a UIObject to the button
            newButton.myUIObject = new UIManagerScript.UIObject();
            newButton.myUIObject.gameObj = newButton.gameObject;

            //set neighbors
            if (t > 0)
            {
                retList[t].myUIObject.neighbors[(int)Directions.NORTH] = retList[t - 1].myUIObject;
                retList[t - 1].myUIObject.neighbors[(int)Directions.SOUTH] = retList[t].myUIObject;
            }

            //assign commands
            newButton.myUIObject.directionalActions[(int)Directions.NORTH] = newButton.MoveCursorToNeighbor;
            newButton.myUIObject.directionalActions[(int)Directions.SOUTH] = newButton.MoveCursorToNeighbor;

            newButton.myUIObject.directionalActions[(int)Directions.EAST] = newButton.SwapVerticalHotbars;
            newButton.myUIObject.directionalActions[(int)Directions.WEST] = newButton.SwapVerticalHotbars;

            //action when button is pushed
            newButton.myUIObject.mySubmitFunction = newButton.OnSubmit;
            newButton.myUIObject.onSubmitValue = t;

            //swapalop action to call when l/r is pressed while selected
            newButton.SetSwapHotbarAction(swapFunction);

            //set listeners for new button
            EventTrigger et = newButton.gameObject.GetComponent<EventTrigger>();

            //Mouseover start
            EventTrigger.Entry hoverStart = new EventTrigger.Entry();
            hoverStart.eventID = EventTriggerType.PointerEnter;
            hoverStart.callback.AddListener(newButton.OnPointerEnter);
            et.triggers.Add(hoverStart);

            EventTrigger.Entry dropalop = new EventTrigger.Entry();
            dropalop.eventID = EventTriggerType.Drop;
            dropalop.callback.AddListener(newButton.OnDrop);
            et.triggers.Add(dropalop);

            EventTrigger.Entry dragalag = new EventTrigger.Entry();
            dragalag.eventID = EventTriggerType.BeginDrag;
            dragalag.callback.AddListener(newButton.OnDrag);
            et.triggers.Add(dragalag);

            EventTrigger.Entry clickalick = new EventTrigger.Entry();
            clickalick.eventID = EventTriggerType.PointerClick;
            clickalick.callback.AddListener(newButton.OnClick);
            et.triggers.Add(clickalick);

        }

        //do something with the swap button
        if (swapButton != null)
        {
            UIManagerScript.UIObject swapObj = new UIManagerScript.UIObject();
            swapObj.gameObj = swapButton.gameObject;
            swapButton.myUIObject = swapObj;
            swapObj.mySubmitFunction = swapButton.SubmitFunction_OnClickOrPress;
            swapButton.onClickAction = OnClickSwapHotbars;
        }

        return retList;
    }

    //Children will decide what this button does when clicked, dragged, or otherwise
    protected virtual void SetActionsForContainerButton(Switch_InvItemButton btn, int[] iAdditionalParameters)
    {
        int iNumSubmitValueParameters = 1;
        bool bHasAdditionalParameters = iAdditionalParameters != null;
        if (bHasAdditionalParameters)
        {
            iNumSubmitValueParameters += iAdditionalParameters.Length;
        }

        btn.iSubmitValue = new int[iNumSubmitValueParameters];
        btn.iSubmitValue[0] = btn.myID;

        if (bHasAdditionalParameters)
        {
            for (int t = 0; t < iAdditionalParameters.Length; t++)
            {
                btn.iSubmitValue[t + 1] = iAdditionalParameters[t];
            }
        }
    }


    //Opens the Tooltip submenu and applies dynamic buttons
    public virtual void OpenTooltipSubmenu(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {

    }

    //Looks inside a button for an item or ability to put in a tooltip
    protected virtual void SetTooltipViaButtonByID(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        
    }

    public virtual void OnSuccessfulSlotObject()
    {
        //play a noise
        UIManagerScript.PlayCursorSound("SlotHotbar");

        //We don't need to lock the cursor to the hotbar anymore if we aren't carrying anything,
        //even though we're pointing at a hotbar button it doesn't mean we're stuck here anymore
        if (CursorOwnershipState == EImpactUICursorOwnershipState.vertical_hotbar_has_cursor)
        {
            CursorOwnershipState = EImpactUICursorOwnershipState.normal;
        }
    }

    public virtual void FocusAndBounceButton(Switch_InvItemButton btn)
    {
        //UIManagerScript.uiObjectFocus = btn.myUIObject;
        UIManagerScript.ChangeUIFocus(btn.myUIObject, processEvent: false);

        //let's bounce while we're here
        btn.PrepareForTween(Vector2.zero);

        //immediately move the button to the left a touch, then run a tween
        //that sends it back to the orignal position
        RectTransform rt = btn.transform as RectTransform;
        Vector2 vPos = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(vPos.x - 4, vPos.y);
        LeanTween.moveX(rt, vPos.x, 0.3f).setEaseInOutBack().setOvershoot(2);
    }

    public virtual void BeginDragGenericObject(ISelectableUIObject obj, GameObject go)
    {
        //Clear this out just in case
        dragOriginGO = null;

        if (obj == null)
        {
            return;
        }

        //But if we have an obj, we're ok
        dragOriginGO = go;

        if (cursorDragger == null)
        {
            cursorDragger = Instantiate(Resources.Load<GameObject>("ShepPrefabs/prefab_genericDragger"), transform);
            cursorDragger.transform.SetAsLastSibling();
        }

        cursorDragger.SetActive(true);
        Image icon = cursorDragger.GetComponent<Image>();
        icon.sprite = obj.GetSpriteForUI();
        bInDragObjectMode = true;

        CursorOwnershipState = EImpactUICursorOwnershipState.dragalaggin;
    }

    public virtual void EndDragGenericObject()
    {
        if (cursorDragger != null)
        {
            cursorDragger.SetActive(false);
        }
        bInDragObjectMode = false;

        if (CursorOwnershipState == EImpactUICursorOwnershipState.dragalaggin)
        {
            CursorOwnershipState = EImpactUICursorOwnershipState.normal;
        }

        //Clear out the markers for FakeyDrops
        dragCurrentTargetGO = null;
        dragOriginGO = null;

        UIManagerScript.ClearAllHeldGenericObjects();
    }

    public virtual int GetFilterIndexForViewAll() { return (int)ItemFilters.VIEWALL;  }
    public virtual int GetFilterIndexForFavorites() { return (int)ItemFilters.FAVORITES; }


    public virtual List<ISelectableUIObject> CreateItemListFromInventoryOfThisTypeThatMatchesFilters<T>(InventoryScript inventoryToUse, bool ignoreSearchBar = false)
    {
        // itemButtons == our list of buttons on screen to display items
        //HeroPC hero = GameMasterScript.heroPCActor;
        List<ISelectableUIObject> retList = new List<ISelectableUIObject>();

        foreach (Item itm in inventoryToUse.GetInventory())
        {
            //Equipment only plz
            if (!(itm is T))
            {
                continue;
            }

            //if we're looking for favorites and not a favorite, get out.
            //this overrides VIEW ALL
            if (UIManagerScript.itemFilterTypes[GetFilterIndexForFavorites()] &&
                !itm.favorite)
            {
                continue;
            }

if (PlatformVariables.SHOW_SEARCHBARS)
{
            if (!ignoreSearchBar && !TDSearchbar.CheckIfItemMatchesTerms(itm)) continue;
}

            //If we are viewing all, add the item and keep going
            if (UIManagerScript.itemFilterTypes[GetFilterIndexForViewAll()])
            {
                retList.Add(itm);
                continue;
            }

            //Check against existing filters, and if we match any of them, add ourselves to the list
            if( AllowItemInItemListSpecialConditions(itm))
            {
                retList.Add(itm);
            }
        }

        return retList;
    }

    //Check against any conditions unique to the UI, such as categories vs filters for the Equipment vs Inventory UI
    protected virtual bool AllowItemInItemListSpecialConditions(Item itm)
    {
        return true;
    }


    //Called by children to populate the list of shadowobjects needed to navigate and click
    protected virtual void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        
    }

    public void OnColumnUpdateFocus(Switch_UIButtonColumn column)
    {
        UpdateContent(adjustSorting: false);

        //grab the button we passed in
        Switch_InvItemButton btn = column.GetSelectedButtonInList();
        if (btn != null)
        {
            DisplayItemInfo(btn.GetContainedData(), null, false);
            FocusAndBounceButton(btn);
        }
        /*
        else
        {
            //only adjust the focus if we're not there already
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.uiObjectFocus);
        }
        */

    }

    //Returns true if something was slotted and the input was useful.
    public virtual bool AssignObjectToHotbarViaKeypress(ISelectableUIObject obj, int iHotbarIdx)
    {
        return false;
    }

    // ====== UI Callbacks =================================================
    #region UI Callbacks

    protected virtual void ClearItemInfo(int obj)
    {
        /*
        ISelectableUIObject content = 
        UIManagerScript.ShowGenericItemTooltip( itam )
        UIManagerScript.singletonUIMS.ClearItemInfo(obj);
        */
    }

    protected virtual void ShowItemInfo(int obj)
    {
        UIManagerScript.singletonUIMS.ShowItemInfo(obj);
    }

    public virtual void OnClickSwapHotbars(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {

    }
    #endregion
    // =====================================================================

    // ====== Statics ======================================================
    #region Statics
    public static IEnumerator FadeTextColor(Color startColor, Color endColor, TextMeshProUGUI txtObject, float fLifeTime)
    {
        float fTime = 0;
        while (fTime < fLifeTime)
        {
            txtObject.color = Color.Lerp(startColor, endColor, fTime / fLifeTime);
            fTime += Time.deltaTime;
            yield return null;
        }
        txtObject.color = endColor;

    }

    //Makes a key/controller navigable chain out of a list of buttons. 
    public static void MakeListOfNeighbors(List<Switch_InvItemButton> listButtons, Directions decrement, Directions increment)
    {
        int decDir = (int)decrement;
        int incDir = (int)increment;

        for (int t = 0; t < listButtons.Count; t++)
        {
            UIManagerScript.UIObject myObject = listButtons[t].myUIObject;

            //if first button, don't connect backwards
            if (t != 0)
            {
                myObject.neighbors[decDir] = listButtons[t - 1].myUIObject;
            }
            //if last button, don't connect forwards
            if (t != listButtons.Count - 1)
            {
                myObject.neighbors[incDir] = listButtons[t + 1].myUIObject;
            }
        }
    }

    //pretty :3 
    public static void AddUIObjectsFromListOfButtons(List<Switch_InvItemButton> fromList, List<UIManagerScript.UIObject> toList)
    {
        //I used resharper to make this
        toList.AddRange(fromList.Select(t1 => t1.myUIObject));
    }

    //Dynamically creates a list of buttons. Sets the position, label, and assigns them a UIObject. Uses the list of enums
    //to count quantity and assign labels. If an onSubmit action is passed in, the Enum value will be supplied to that
    //action when the button is clicked
    public static List<Switch_InvItemButton> CreateListOfButtons<T>(Switch_InvItemButton btnFirstButton, IList<T> list_enums,
        Vector2 vButtonSpawnDirection, int iPxBetweenButtons, Action<int[], Switch_InvItemButton.ELastInputSource> onSubmit) where T : IConvertible
    {
        List<Switch_InvItemButton> retList = new List<Switch_InvItemButton>();
        for (int idx = 0; idx < list_enums.Count; idx++)
        {
            //Debug.Log("Index " + idx);
            //Debug.Log("List enum check " + list_enums[idx]);
            int iSubmitValue = Convert.ToInt32(list_enums[idx]);

            Switch_InvItemButton newButan = CreateButtonInList(btnFirstButton, vButtonSpawnDirection, iPxBetweenButtons, idx, onSubmit, new int[] { iSubmitValue });

            //text on button matters as well
            string strButtonText = StringManager.GetStringForEnum(list_enums[idx]);
            newButan.txtLabel.text = strButtonText;

            retList.Add(newButan);
        }

        return retList;
    }

    //Also dynamically creates a list of buttons. Sets the position and assigns them a UIObject. No labels are given
    //and it is assumed these are empty inventory buttons. No onSubmit functions are set here.
    public static List<Switch_InvItemButton> CreateListOfButtons(Switch_InvItemButton btnFirstButton, int iNumButtons, Vector2 vButtonSpawnDirection, int iPxBetweenButtons)
    {
        List<Switch_InvItemButton> retList = new List<Switch_InvItemButton>();

        for (int idx = 0; idx < iNumButtons; idx++)
        {
            Switch_InvItemButton newButan = CreateButtonInList(btnFirstButton, vButtonSpawnDirection, iPxBetweenButtons, idx, null, null);
            retList.Add(newButan);
        }

        return retList;
    }

    //Create a single button in the list, and assign it an onSubmit function if we pass one in.
    //lol thx resharper for making this out of code in a bigger function prior
    static Switch_InvItemButton CreateButtonInList(Switch_InvItemButton btnFirstButton, Vector2 vButtonSpawnDirection, int iPxBetweenButtons, int idx, 
        Action<int[], Switch_InvItemButton.ELastInputSource> onSubmit, int[] submitValue)
    {
        Switch_InvItemButton newButan;
        if (idx == 0)
        {
            newButan = btnFirstButton;
        }
        //Otherwise make a new one
        else
        {
            newButan = Instantiate(btnFirstButton, btnFirstButton.transform.parent);
        }

        UIManagerScript.UIObject newObject = new UIManagerScript.UIObject();
        newButan.myUIObject = newObject;
        newObject.gameObj = newButan.gameObject;
        newButan.myID = idx;
        newObject.subObjectTMPro = newButan.GetComponentInChildren<TextMeshProUGUI>();

        newObject.mySubmitFunction = newButan.SubmitFunction_OnClickOrPress;

        if (onSubmit != null)
        {
            newButan.SetEventListener(EventTriggerType.PointerClick, newButan.Default_OnClick);
            newButan.onClickAction = onSubmit;
            newButan.iSubmitValue = submitValue;
        }

        //assign the last sprite on the button to the object, that's where the icons go
        Image[] images = newObject.gameObj.GetComponentsInChildren<Image>();
        if (images != null && images.Length > 0)
        {
            newObject.subObjectImage = images[images.Length - 1];
        }

        //position of button is important!
        RectTransform myRectTransform = newButan.transform as RectTransform;
        myRectTransform.localScale = Vector3.one;
        Vector2 vCurrentPos = myRectTransform.localPosition;

        //Determine a size and direction and move once for each step in the loop
        //Note that pixels between buttons must also account for the size of the button
        Vector2 vSpacingVector = vButtonSpawnDirection;
        vSpacingVector.x *= (iPxBetweenButtons + myRectTransform.sizeDelta.x) * idx;
        vSpacingVector.y *= (iPxBetweenButtons + myRectTransform.sizeDelta.y) * idx;

        vCurrentPos += vSpacingVector;
        myRectTransform.localPosition = vCurrentPos;
        return newButan;
    }

    public static void FlashInventoryButtonOnSuccessfulEquip(Switch_InvItemButton dropButton, ISelectableUIObject obj)
    {
        //dropButton.SetContainedData(obj, true);

        // AA: 1/26/18 the text flashing was causing some issues. The zoom pop is good enough, we don't need this
        /* if (dropButton.txtLabel != null)
        {
            dropButton.StartCoroutine(FadeTextColor(Color.yellow, Color.white, dropButton.txtLabel, 0.2f));
        } */

        if (!dropButton.gameObject.activeSelf)
        {
            return;
        }

        //immediately terminate any children that were just made for flashing.
        Image[] kiddieImages = dropButton.gameObject.transform.GetComponentsInChildren<Image>();
        for (int t = 0; t < kiddieImages.Length; t++)
        {
            if (kiddieImages[t].gameObject.tag == "equipflash")
            {
                kiddieImages[t].gameObject.SetActive(false);
                kiddieImages[t].transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        dropButton.StartCoroutine(UIManagerScript.singletonUIMS.Flash_EquipItem(dropButton.gameObject.GetComponentsInChildren<Image>()[1], 1.0f));
    }

    #endregion
    // =====================================================================

    //Should be able to find this object somewhere -- an item in inventory, an ability on the bar, who knows?
    public abstract Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj);

    //Usually called when the Drop Quantity dialog is closed.
    public abstract void OnDialogClose();

    public abstract void StartAssignObjectToHotbarMode(int iButtonIndex);
    public abstract void StartAssignObjectToHotbarMode(ISelectableUIObject content);

    //Whatever this is, we should try to display it in whatever method fits us best.
    //This will be different for inventory, equipment, sklls, etc.
    public abstract void TryShowSelectableObject(ISelectableUIObject obj);
}

//Parent class to Equipment, Inventory, and perhaps any future UI that use a column full of items. 
public abstract class ImpactUI_WithItemColumn : ImpactUI_Base
{
    [Header("Item Column")]
    public Switch_UIButtonColumn itemColumn;
    

    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        //The vertical main column that holds all the items and scrolls around
        itemColumn.CreateStartingContent(0);

        itemColumn.SetActionForEventOnButtons(EventTriggerType.PointerEnter, SetTooltipViaButtonByID);
        itemColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, OpenTooltipSubmenu);
        itemColumn.SetActionForEventOnButtons(EventTriggerType.BeginDrag, StartDragItemFromInventory);

        itemColumn.onCursorPositionInListUpdated = OnColumnUpdateFocus;

        return true;
    }


    //Looks inside a button for an item or ability to put in a tooltip
    protected override void SetTooltipViaButtonByID(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //grab the button we passed in
        Switch_InvItemButton btn = itemColumn.GetButtonInList(args[0]);
        DisplayItemInfo(btn.GetContainedData() as Item, null, false);
        FocusAndBounceButton(btn);
    }

    //Start dragging a copy of this object, but don't remove us from the list.
    protected void StartDragItemFromInventory(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //grab the button we passed in
        Switch_InvItemButton btn = itemColumn.GetButtonInList(args[0]);
        UIManagerScript.BeginDragGenericObject(btn.GetContainedData(), btn.gameObject);
    }

    public override Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj)
    {
        if (itemColumn != null)
        {
            return itemColumn.GetButtonContaining(obj);
        }

        return null;
    }

    protected override void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        base.AddAllUIObjects(allObjects);
        itemColumn.AddButtonsToUIObjectMasterList();
    }


    public override void OnDialogClose()
    {
        //close the submenu if it is still open
        if (UIManagerScript.TooltipHasCursor())
        {
            UIManagerScript.TooltipReleaseCursor();
        }

        //Clear out the old dialog and add our objects
        UIManagerScript.allUIObjects.Clear();
        AddAllUIObjects(UIManagerScript.allUIObjects);

        //make sure we have control of the cursor again
        if (!UIManagerScript.allUIObjects.Contains(UIManagerScript.uiObjectFocus))
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.GetDefaultUIFocus());
        }

        UpdateContent();
    }

    public override UIManagerScript.UIObject GetDefaultUiObjectForFocus()
    {
        return itemColumn != null ? itemColumn.GetTopUIObject() : null;
    }

    //Returns TRUE if the input has been handled and there's no need to process it further
    public override bool HandleInput(Directions dInput)
    {
        //Doing this out of order as we want to grab directional input and 
        //simulate the scroll wheel with certain button presses.

        //First -- check for anims and dialogs.
        if (GameMasterScript.IsNextTurnPausedByAnimations())
        {
            return true;
        }

        //Don't take any input if a dialog is up
        if (UIManagerScript.dialogBoxOpen)
        {
            return false;
        }

        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;

        if (playerInput.GetButtonDown("Cycle Hotbars"))
        {
            SwapHotbarViaGamepad();
            return true;
        }

        //check for mousewheel scrolling or page up/down (quantized)
        if (itemColumn.HandleScrollInput(playerInput, dInput))
        {
            return true;
        }

        Switch_InvItemButton selectedButton = itemColumn.GetSelectedButtonInList();
        if (selectedButton != null)
        {
            Item itam = selectedButton.GetContainedData() as Item;
            if (itam != null)
            {
                if (playerInput.GetButtonDown("Mark Favorite Item"))
                {
                    genericTooltip.MarkItemFavorite(new[] { itam.actorUniqueID }, Switch_InvItemButton.ELastInputSource.keyboard_or_gamepad);
                    TutorialManagerScript.markedItemAsFavoriteOrTrash = true;
                }

                if (!PlatformVariables.GAMEPAD_ONLY && playerInput.GetButtonDown("Mark As Trash"))
                {
                    genericTooltip.MarkItemTrash(new[] { itam.actorUniqueID }, Switch_InvItemButton.ELastInputSource.keyboard_or_gamepad);
                    TutorialManagerScript.markedItemAsFavoriteOrTrash = true;
                }

                if (playerInput.GetButtonDown("Drop Item"))
                {
                    UIManagerScript.DropItemFromSheet(itam);
                    genericTooltip.HideSubmenu();
                    UIManagerScript.UpdateFullScreenUIContent();                    
                    genericTooltip.ClearAllItems();

                    //I'm pointing at something new now
                    UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);

                    // #todo - play SFX here?
                    //UIManagerScript.ForceCloseFullScreenUI();

                    return true;
                }

if (!PlatformVariables.GAMEPAD_ONLY)
{
                for (int t = 0; t < 8; t++)
                {
                    if (playerInput.GetButtonDown("Use Hotbar Slot " + (t + 1)) &&
                        AssignObjectToHotbarViaKeypress(itam, t))
                    {
                        return true;
                    }
                }
}
            }
        }
       

        //otherwise handle input normally.
        if (base.HandleInput(dInput))
        {
            return true;
        }



        return false;
    }

    public void SubmenuClosed()
    {
        if (CursorOwnershipState == EImpactUICursorOwnershipState.tooltip_has_cursor)
        {
            CursorOwnershipState = EImpactUICursorOwnershipState.normal;
        }
    }

    public override void TryShowSelectableObject(ISelectableUIObject obj)
    {
        if (obj is Item)
        {
            DisplayItemInfo((Item) obj, null, false);
        }

        //todo: skills/ability etc
    }

}

public partial class UIManagerScript
{
    private GameObject mainUINavContainer;

    public enum EMessageForFullScreenUI
    {
        none = 0,
        inventory_as_snack_bag,
        inventory_as_attack_bag, //smack_bag, imo
        MAX,
    }

    [Header("New Full Screen UI Interfaces")]
    public List<ImpactUI_Base> FullscreenUIObjectsInScene;

    public ImpactUI_Base currentFullScreenUI;

    //if we would like our fullScreenUI to do something special on activation,
    //we can send it a message. We have to use this delayed/stored info style because
    //the UI is opened via delayed coroutine.
    public static EMessageForFullScreenUI messageForFullScreenUI;

    public Dictionary<UITabs, ImpactUI_Base> dictFullScreenUI;

    //Returns true if input is handled and nothing else needs to happen 
    public static bool HandleInput_CurrentFullScreenUI(Directions dInput)
    {
        return singletonUIMS.currentFullScreenUI != null && singletonUIMS.currentFullScreenUI.HandleInput(dInput);
    }

    //Unity doesn't fire a Drop event if the drag origin and destination are the same.
    //Instead it fires OnClick which is terrible. So if we're dragging and OnClick is fired
    //we need to check ourselves.
    public static bool CheckForFakeyDrop()
    {
        return singletonUIMS.currentFullScreenUI != null && singletonUIMS.currentFullScreenUI.CheckForFakeyDrop();
    }
    
    //Jump directly to the inventory page, open up the food section, and select
    //first food button
    public static bool OpenSnackBagFullScreenUI()
    {
        //Don't open the snackbag if the UI is already active
        if (singletonUIMS.currentFullScreenUI != null)
        {
            return false;
        }

        //Tell the UI when it opens that it needs to get its snackery on
        messageForFullScreenUI = EMessageForFullScreenUI.inventory_as_snack_bag;

        //pop open the UI
        OpenFullScreenUI(UITabs.INVENTORY);

        return true;
    }

    //Jump directly to the inventory page, and use some unique settings
    public static bool OpenSpecialItemBagFullScreenUI(EMessageForFullScreenUI msgBagType)
    {
        //Don't open the snackbag if the UI is already active
        if (singletonUIMS.currentFullScreenUI != null)
        {
            return false;
        }

        //Tell the UI when it opens that it needs to get its snackery on
        messageForFullScreenUI = msgBagType;

        //pop open the UI
        OpenFullScreenUI(UITabs.INVENTORY);

        return true;
    }

    //returns true if this ui has been converted to new method
    public static bool OpenFullScreenUI(UITabs newTab)
    {
        
        
        bool successfullyOpenedUI = false; // do things like SFX, OnOpen ONLY if this is TRUE by the end of the function.


        //if (Debug.isDebugBuild) Debug.Log("Request open full screen UI " + newTab +", message: " + messageForFullScreenUI);

        bool existingUIOpen = false;

        if (MonsterCorralScript.corralInterfaceOpen)
        {
            MonsterCorralScript.CloseCorralInterface();
        }
        if (CorralBreedScript.corralBreedInterfaceOpen)
        {
            CorralBreedScript.singleton.ExitBreedingInterface(0);
        }


        //If we have a new style UI, close it
        if (singletonUIMS.currentFullScreenUI != null)
        {
            existingUIOpen = true;
            singletonUIMS.currentFullScreenUI.TurnOff();
            singletonUIMS.currentFullScreenUI = null;
        }

        if (!existingUIOpen)
        {
            existingUIOpen = bFullScreenUIOpen;
            
            // Prevent possible issues with menu being closed too fast. Start the timer!
            UIManagerScript.StartPreventOptionMenuToggleTimer(0.8f);
            //Debug.Log("<color=green>No existing UI open when attempting to open " + newTab + ", so starting a coroutine to prevent fast close.</color>");
        }

        if (!existingUIOpen)
        {
            TDInputHandler.OnDialogOrFullScreenUIOpened();
            GuideMode.OnFullScreenUIOpened();
        }

        //For now, close all the other dialogs just in case :( :( 
        singletonUIMS.CloseAllDialogs();

        string cueToPlay = "UITock";

        //Debug.Log(uiTabSelected + " " + existingUIOpen + " " + cueToPlay + " " + newTab);

        // Play cue if we're switching pages.
        if (uiTabSelected != newTab && existingUIOpen)
        {
            cueToPlay = "UITock";
        }
        else if (!existingUIOpen)
        {
            cueToPlay = "OpenDialog";
        }

        UIManagerScript.PlayCursorSound(cueToPlay);

        // update key menu shortcuts                
        string[] navShortcuts = new string[6];
        UITopArea uiTop = singletonUIMS.mainUINavContainer.GetComponent<UITopArea>();


        if (!PlatformVariables.GAMEPAD_ONLY && ReInput.controllers.GetLastActiveControllerType() == ControllerType.Keyboard)
        {
            navShortcuts[0] = CustomAlgorithms.GetButtonAssignment("View Character Info");
            navShortcuts[1] = CustomAlgorithms.GetButtonAssignment("View Equipment");
            navShortcuts[2] = CustomAlgorithms.GetButtonAssignment("View Consumables");
            navShortcuts[3] = CustomAlgorithms.GetButtonAssignment("View Skills");
            navShortcuts[4] = CustomAlgorithms.GetButtonAssignment("View Rumors");
            navShortcuts[5] = CustomAlgorithms.GetButtonAssignment("Options Menu");
            for (int i = 0; i < uiTop.shortcutText.Length; i++)
            {
                if (!string.IsNullOrEmpty(navShortcuts[i]) && navShortcuts[i].ToLowerInvariant() != "unassigned" && navShortcuts[i].ToLowerInvariant() != "undefined")
                {
                    uiTop.shortcutText[i].text = navShortcuts[i];
                    FontManager.LocalizeMe(uiTop.shortcutText[i], TDFonts.WHITE);
                }
                else if (navShortcuts[i].ToLowerInvariant() == "unassigned" && navShortcuts[i].ToLowerInvariant() == "undefined")
                {
                    uiTop.shortcutText[i].text = "";
                }
            }
        }
        else

        {
            for (int i = 0; i < navShortcuts.Length; i++)
            {
                navShortcuts[i] = "";
                uiTop.shortcutText[i].text = "";
            }
        }
       
        //this is true regardless of whether the UI is new or old
        uiTabSelected = newTab;
        lastOpenedFullScreenUI = newTab;

        //if the tab we're asking for is old, let the old code handle it
        if (!singletonUIMS.dictFullScreenUI.ContainsKey(newTab))
        {
            //old code 
            switch (newTab)
            {
                //case UITabs.CHARACTER:
                //    ToggleCharacterSheet();
                //    break;

                case UITabs.OPTIONS:
					if (!PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
					{
 		                   ToggleOptionsMenu();
					}
                    break;
                case UITabs.RUMORS:
                    UIManagerScript.FadeGradientOut(0f);
                    ToggleQuestSheet();
                    break;
                default:
                    return false;
            }

            bFullScreenUIOpen = true;
            return true;
        }
        else
        {
            if (newTab == UITabs.OPTIONS && !PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
            {
                singletonUIMS.gradientBGImageForUltraWide.GetComponent<ImageEffects>().TurnOnAtOnce();
                ToggleOptionsMenu();
                bFullScreenUIOpen = true;
                return true;
            }
        }

        //Debug.Log("MEssage state " + messageForFullScreenUI);

        //if no fullscreen UI is open, do flashy flash
        if (bFullScreenUIOpen == false)
        {
            //set this flag to true because we don't have every UI in the new style yet
            bFullScreenUIOpen = true;

            //do flashy
            DoPrettyMenuFadeOutThenInBecauseJuice(newTab, false, false, messageForFullScreenUI);
            return true;
        }

        //Otherwise, we're in a fullscreen UI, we've closed the prior tab, now open this one.
        singletonUIMS.currentFullScreenUI = singletonUIMS.dictFullScreenUI[newTab];
        singletonUIMS.currentFullScreenUI.TurnOn();

        return true;

    }

    //This is NOT called when switching tabs. This is called when
    //we are closing out entirely and going back to the main game.
    public static bool TryCloseFullScreenUI()
    {
        //If we have one up, and it won't let us close, then dont' close
        if (singletonUIMS.currentFullScreenUI != null && !singletonUIMS.currentFullScreenUI.TryTurnOff())
        {
            //if (Debug.isDebugBuild) Debug.Log("Cannot close because, current full screen UI is null? " + (singletonUIMS.currentFullScreenUI));
            return false;
        }

        return CloseFullScreenUI();
    }

    //This call will close the UI no matter what modal state the cursor is in.
    //It won't ask anything at all. This is used primarily when we are in a given
    //UI Screen the button that toggles that screen is pressed again.
    public static bool ForceCloseFullScreenUI()
    {
        return CloseFullScreenUI(forced:true);
    }

    // Special case for things that need to instantly exit (such as tents) w/ no fade
    public static bool ForceCloseFullScreenUIWithNoFade(bool reallyActuallyForce = false)
    {
        //Debug.Log("Force close, no fade.");
        UIManagerScript.FadeGradientIn(0.01f);
        return CloseFullScreenUIWithNoFade(reallyActuallyForce);
    }

    public void OnClick_CloseFullscreenUI()
    {
        PlayCursorSound("Cancel");
        ForceCloseFullScreenUI();
    }

    static bool CloseFullScreenUIWithNoFade(bool reallyActuallyForce = false)
    {
        CloseFullScreen_Internal(true);
        singletonUIMS.ToggleUITab(uiTabSelected, forceClose:reallyActuallyForce);
        return true;
    }

    static void CloseFullScreen_Internal( bool bHideImmediately = false)
    {
        //set this flag to false because we don't have every UI in the new style yet
        bFullScreenUIOpen = false;

        //Hide the bar at the top
        singletonUIMS.HideUINavigation(bHideImmediately);

        //No longer *force* the cursor to be hidden
        bForceHideCursor = false;

        //But hide the cursor regardless. When we load it again later, it won't be hidden!
        HideDialogMenuCursor();

        //No longer track any carried or dragged object since we don't
        //want to drag items from the fullscreen UI into the real world outside
        //of the holodeck.
        ClearAllHeldGenericObjects();

        UpdateActiveWeaponInfo(); // our weapon hotbar may have changed
    }

    //This will close the fullscreen UI without asking first
    static bool CloseFullScreenUI(bool forced = false)
    {
        
        if (UIManagerScript.PreventingOptionMenuToggle())
        {
            return false;
        }

        StartPreventOptionMenuToggleTimer(1.1f); // new from merge

        CloseFullScreen_Internal();

        //Turn things off with the fade in/out action
        DoPrettyMenuFadeOutThenInBecauseJuice(uiTabSelected, forced, closingUI:true);

        singletonUIMS.RefreshAbilityCooldowns();
        GameModifiersScript.CheckForInvalidBuffsAndSummons();
        TutorialManagerScript.OnUIClosed();

        return true;
    }

    //Asks us for which full screen menu is open
    //If we have a new style UI active, return that value
    //otherwise, return the stored value from the old UI
    public static UITabs GetUITabSelected()
    {
        return singletonUIMS.currentFullScreenUI != null ? singletonUIMS.currentFullScreenUI.GetUIType() : uiTabSelected;
    }

    //This plays the black fade on screen and then activates the UITab we've asked for
    /// <summary>
    /// Returns TRUE if we begin the animation, FALSE if we exited out.
    /// </summary>
    /// <param name="tabToOpen"></param>
    /// <param name="forced"></param>
    /// <returns></returns>
    public static bool DoPrettyMenuFadeOutThenInBecauseJuice(UITabs tabToOpen, bool forced = false, bool closingUI = false, EMessageForFullScreenUI fullScreenMessage = EMessageForFullScreenUI.none)
    {
        // If we are FORCING to close, then who cares if we're fading? Just power through

        // If screen is fading for some reason, don't start another fade.
        if (singletonUIMS.blackFade.GetComponent<ImageEffects>().IsFading() && !forced)
        {
            return false;
        }

        float fFadeTiem = singletonUIMS.menuFadeTime;

        if (messageForFullScreenUI == EMessageForFullScreenUI.inventory_as_snack_bag)
        {
            fFadeTiem = 0.05f;
        }

        //make pretty <3
        singletonUIMS.StartCoroutine(FadeOutThenInForUI(fFadeTiem, 0.025f, fFadeTiem, closingUI));

        if (closingUI)
        {
            GuideMode.OnFullScreenUIClosed();
        }

        float fadeTimeToUse = fullScreenMessage == EMessageForFullScreenUI.none ? singletonUIMS.menuFadeTime : 0.1f;

        //make sure our UI comes back up 
        if (tabToOpen != UITabs.NONE)
        {
            singletonUIMS.StartCoroutine(singletonUIMS.WaitThenToggleUITab(fadeTimeToUse, tabToOpen, fullScreenMessage));
        }
        return true;
    }

    //This returns a UITab that isn't .none if the current input is pressing a key that switches UI screens
    public static UITabs CheckInputForFullScreenUIToggle()
    {
        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;

        //if we are in a mode where this doesn't matter, quit early.
        if (dialogBoxOpen &&
            (dialogBoxType == DialogType.LEVELUP || dialogBoxType == DialogType.KEYSTORY))
        {
            return UITabs.NONE;
        }
 
        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            //Check against the toggle buttons
            if (playerInput.GetButtonDown("View Equipment"))
            {
                return UITabs.EQUIPMENT;
            }
            if (playerInput.GetButtonDown("View Consumables"))
            {
                return UITabs.INVENTORY;
            }
            if (playerInput.GetButtonDown("View Skills"))
            {
                return UITabs.SKILLS;
            }
        }

        //none of the above!
        return UITabs.NONE;
    }

    public static void OnSuccessfulSlotObject()
    {
        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.OnSuccessfulSlotObject();
        }
        else
        {
            //play a noise
            PlayCursorSound("SlotHotbar");

            //jump back to a default object
            UIObject localFocus = GetDefaultUIFocus();
            ChangeUIFocusAndAlignCursor(localFocus);
        }

    }

    public static Switch_InvItemButton GetButtonContainingThisObjectFromFullScreenUI(ISelectableUIObject obj)
    {
        if (singletonUIMS.currentFullScreenUI != null)
        {
            return singletonUIMS.currentFullScreenUI.GetButtonContainingThisObject(obj);
        }

        return null;
    }

    /// <summary>
    /// HideSubmenu and UpdateFullScreenUIContent usually need to be called in that order.
    /// If not, then the Update updates information based on the submenu being open. When the menu
    /// then closes, the information is dirty, which ends up leading to weird input bugs down the road.
    /// </summary>
    public static void HideSubmenuAndUpdateFullScreenUIContent()
    {
        if (genericItemTooltip != null)
        {
            genericItemTooltip.HideSubmenu();
        }

        UpdateFullScreenUIContent();
    }
    public static void UpdateFullScreenUIContent()
    {
        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.UpdateContent();
        }
    }

    public static void FullScreenUIStartAssignInventoryItemToHotbarMode(int iItemIndex)
    {
        if (singletonUIMS.currentFullScreenUI != null)
        {
            Item itm = GameMasterScript.heroPCActor.myInventory.GetItemByID(iItemIndex);
            singletonUIMS.currentFullScreenUI.StartAssignObjectToHotbarMode(itm);
        }
    }

    //Whatever it is we're passing in, do our best to display it.
    public static void TryShowObjectInFullscreenUI( ISelectableUIObject obj )
    {
        if (obj == null)
        {
            return;
        }

        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.TryShowSelectableObject(obj);
        }
    }

    /// <summary>
    /// We don't want the player to spam the option button and cause all the coroutines to die from being called crissy chrono crossy willie nillie
    /// </summary>
    private static float fPreventOptionMenuToggleTimer = 0f;
    public static bool WasFullScreenMenuRecentlyOpened
    {
        get
        {
            return fPreventOptionMenuToggleTimer > 0f;
        }
    }


    /// <summary>
    /// Tick down this value that gives us a breather between presses of the Option button.
    /// </summary>
    static void UpdatePreventOptionMenuToggleTimer()
    {
        if (fPreventOptionMenuToggleTimer > 0f)
        {
            fPreventOptionMenuToggleTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// We don't want the player to spam the option button and cause all the coroutines to die from being called crissy chrono crossy willie nillie
    /// </summary>
    /// <returns>TRUE if the timer is > 0f</returns>
    public static bool PreventingOptionMenuToggle()
    {
        return fPreventOptionMenuToggleTimer > 0f ||
               GameMasterScript.IsNextTurnPausedByAnimations() ||
               GameMasterScript.IsGameInCutsceneOrDialog();	
	}
	
    /// <summary>
    /// We want a way to kill this immediately.
    /// </summary>
    public static void UnlockOptionsMenuToggle()
    {
        fPreventOptionMenuToggleTimer = 0f;
    }

    /// <summary>
    /// Create a mandatory pause between presses of the Option menu toggle, to prevent wild coroutine hysteresis
    /// </summary>
    public static void StartPreventOptionMenuToggleTimer(float fDelayTime)
    {
        fPreventOptionMenuToggleTimer = fDelayTime;
    }
}
