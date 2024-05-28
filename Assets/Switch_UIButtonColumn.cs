using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Switch_UIButtonColumn : MonoBehaviour
{
    enum EDisplayMode
    {
        buttons = 0,
        text_info_only,
    }

    public Switch_InvItemButton         btn_first;
    public int                          NumberOfButtons;
    public int                          PxBetweenButtons;
    private List<Switch_InvItemButton>    listAllButtons;

    public Switch_InvItemButton         btn_BottomLeft;
    public Switch_InvItemButton         btn_BottomRight;

    public TextMeshProUGUI              text_TopLabel;

    private List<ISelectableUIObject>   listHeldObjects;

    //These are the objects we jump to when we have moved past the top or bottom 
    //of our scroll area, but also scrolled all the way to the top or bottom
    public UIManagerScript.UIObject neighborTop;
    public UIManagerScript.UIObject neighborBottom;
    public UIManagerScript.UIObject neighborLeft;
    public UIManagerScript.UIObject neighborRight;

    [Tooltip("If the column needs to display information instead of list buttons, use this.")]
    public TextMeshProUGUI txt_Info;
    private EDisplayMode displayMode;

    public Action<Switch_UIButtonColumn> onCursorPositionInListUpdated;
    private Action<Switch_InvItemButton, int[], string[]> actionAdjustButtonContentOnUpdate;

    //offset from the first item in whatever we're displaying to allow us to scroll up and down through a list
    public int iOffsetFromTopOfList;

    public GameObject sliderObject;
    private Slider mySlider;
    private int iLastGoodScrollbarOffset = -1;

    //When the action is called on every button in the column,
    //there may need to be changes based on the values in other buttons
    //use these to accumulate or set data between button calls
    private int[] intRegisterAllActions;
    private string[] stringRegisterAllActions;

    public void Awake()
    {
        if (sliderObject != null)
        {
            mySlider = sliderObject.GetComponent<Slider>();
            if (mySlider != null)
            {
                mySlider.onValueChanged.AddListener(OnScrollbarValueChange);
            }
        }
    }

    public void Start()
    {
        if (text_TopLabel != null)
        {
            FontManager.LocalizeMe(text_TopLabel, TDFonts.WHITE);
        }
        if (txt_Info != null)
        {
            FontManager.LocalizeMe(txt_Info, TDFonts.WHITE);
        }        
    }

    public bool HandleScrollInput(Rewired.Player playerInput, Directions dInput)
    {
        if (ShouldReceiveScrollWheelInput())
        {
            // Page up/down: quantized movement
            ControllerType lastActiveControllerType = ReInput.controllers.GetLastActiveControllerType();

            if (!PlatformVariables.GAMEPAD_ONLY && lastActiveControllerType == ControllerType.Joystick)
            {
                if (playerInput.GetButton("Diagonal Move Only"))
                {
                    if (dInput == Directions.NORTH)
                    {
                        AdjustOffsetByFullPage(PageScroll.UP);
                        return true;
                    }
                    if (dInput == Directions.SOUTH)
                    {
                        AdjustOffsetByFullPage(PageScroll.DOWN);
                        return true;
                    }

                    return true;
                }
            }
            else 
            {
                if (playerInput.GetButtonDown("List Page Down"))
                {
                    AdjustOffsetByFullPage(PageScroll.DOWN);
                    return true;

                }

                if (playerInput.GetButtonDown("List Page Up"))
                {
                    AdjustOffsetByFullPage(PageScroll.UP);
                    return true;
                }
            }

            float fDelta;

            //float fastScrollAxis = playerInput.GetAxis("Scroll UI Boxes Vertical"); // Right stick on controllers

            if (TDInputHandler.IsCompareAlternateButtonHeld() && (dInput == Directions.NORTH || dInput == Directions.SOUTH))
            {
                fDelta = dInput == Directions.NORTH ? 1.0f : -1.0f;
            }
            /* else if (fastScrollAxis != 0f)
            {
                fDelta = fastScrollAxis > 0f ? 1.0f : -1.0f;
            } */
            else
            {
                fDelta = Input.GetAxis("Mouse ScrollWheel");
            }
            
            if (fDelta != 0f)
            {
                AdjustOffsetViaScrolling(fDelta > 0 ? -2 : 2); // 3 seemed too fast
                return true;
            }
        }
        return false;
    }

    public void SetButtonDisplayMode()
    {
        displayMode = EDisplayMode.buttons;
        UpdateInternalObjectsForModeChange();
    }

    void UpdateInternalObjectsForModeChange()
    {
        if (listAllButtons != null)
        {
            foreach (var btn in listAllButtons)
            {
                btn.gameObject.SetActive( displayMode == EDisplayMode.buttons);
            }
        }

        if (txt_Info != null)
        {
            txt_Info.gameObject.SetActive( displayMode == EDisplayMode.text_info_only);
        }

    }

    public void SetTextInfoDisplayMode(string strText)
    {
        displayMode = EDisplayMode.text_info_only;
        UpdateInternalObjectsForModeChange();
        if (txt_Info != null)
        {
            txt_Info.text = strText;
        }
    }

    public void SetButtonInfo(int iButtonIndex, bool bActive, string strNewLabel = null)
    {
        Switch_InvItemButton btn = iButtonIndex == 0 ? btn_BottomLeft : btn_BottomRight;
        btn.gameObject.SetActive(bActive);

        if (btn.txtLabel != null)
        {
            btn.txtLabel.text = strNewLabel;
        }
    }

    public void SetBottomButtonAction(int iButtonIndex, EventTriggerType eventType, Action<int[], Switch_InvItemButton.ELastInputSource> action)
    {
        Switch_InvItemButton btn = iButtonIndex == 0 ? btn_BottomLeft : btn_BottomRight;
        btn.iSubmitValue = new [] { iButtonIndex };
        btn.SetActionOnReceiveEvent(eventType, action);
    }

    public void OnScrollbarValueChange(float fNewValue)
    {
        //Determine our maximum offset
        int iNumButtons = listAllButtons.Count;
        int iNumThings = listHeldObjects.Count;

        int iMaxOffset = iNumThings - iNumButtons;

        iOffsetFromTopOfList = (int) Math.Max(0, Math.Min(fNewValue, iMaxOffset));

        UpdateButtonsInList();
    }

    public Switch_InvItemButton GetButtonInList(int index)
    {
        if (listAllButtons != null && index < listAllButtons.Count && index >= 0)
        {
            return listAllButtons[index];
        }

        return null;
    }

    public Switch_InvItemButton GetSelectedButtonInList()
    {
        var l = listAllButtons.Where(btn => btn.myUIObject == UIManagerScript.uiObjectFocus).ToList();
        return l.Count > 0 ? l[0] : null;
    }

    //Link up the bottom buttons of two columns
    public void ConnectBottomButtonsToOtherColumn(Switch_UIButtonColumn otherColumn, Directions dirOtherColumnRelativeToMe )
    {
        if (dirOtherColumnRelativeToMe == Directions.EAST)
        {
            btn_BottomRight.myUIObject.neighbors[(int) Directions.EAST] = otherColumn.btn_BottomLeft.myUIObject;
            otherColumn.btn_BottomLeft.myUIObject.neighbors[(int)Directions.WEST] = btn_BottomRight.myUIObject;
        }
        else if (dirOtherColumnRelativeToMe == Directions.WEST)
        {
            btn_BottomLeft.myUIObject.neighbors[(int)Directions.WEST] = otherColumn.btn_BottomRight.myUIObject;
            otherColumn.btn_BottomRight.myUIObject.neighbors[(int)Directions.EAST] = btn_BottomLeft.myUIObject;
        }

        //North and South connections can be here if we need them, but I'm not sure how that would work!
    }

    public void CreateStartingContent( int iColumnID )
    {
        listAllButtons = ImpactUI_Base.CreateListOfButtons(btn_first, NumberOfButtons, Vector2.down, PxBetweenButtons);
        //make them bros
        ImpactUI_Base.MakeListOfNeighbors(listAllButtons, Directions.NORTH, Directions.SOUTH);

        //set event listeners for all buttons to default so that we can insert our tangledangle code to be called
        foreach (Switch_InvItemButton btn in listAllButtons)
        {
            SetAllListenersOnButton(btn);
            btn.myButtonColumn = this;

            btn.iSubmitValue = new [] { btn.myID, iColumnID };

            for (int i = (int) Directions.NORTH; i <= (int) Directions.WEST; i += 2)
            {
                btn.myUIObject.directionalActions[i] = btn.MoveCursorToNeighbor;
            }
        }

        //If our column has bottom buttons, make it so.
        if (btn_BottomLeft != null)
        {
            //Make the bottom buttons into proper buttons, have them connect to each other, and to the
            //list above.
            btn_BottomLeft = InitializeBottomButton(btn_BottomLeft);
            btn_BottomRight = InitializeBottomButton(btn_BottomRight);

            btn_BottomLeft.myUIObject.neighbors[(int) Directions.EAST] = btn_BottomRight.myUIObject;
            btn_BottomLeft.myUIObject.neighbors[(int) Directions.WEST] = neighborLeft;

            btn_BottomRight.myUIObject.neighbors[(int) Directions.WEST] = btn_BottomLeft.myUIObject;
            btn_BottomRight.myUIObject.neighbors[(int) Directions.EAST] = neighborRight;

            neighborBottom = btn_BottomLeft.myUIObject;
        }

    }

    Switch_InvItemButton InitializeBottomButton(Switch_InvItemButton btn)
    {
        btn.myUIObject = new UIManagerScript.UIObject();
        btn.myUIObject.gameObj = btn.gameObject;
        btn.myUIObject.mySubmitFunction = btn.SubmitFunction_OnClickOrPress;

        for (int i = (int)Directions.NORTH; i <= (int)Directions.WEST; i += 2)
        {
            btn.myUIObject.directionalActions[i] = btn.MoveCursorToNeighbor;
        }

        SetAllListenersOnButton(btn);

        //Bottom buttons need to behave better when being bound to buttons aBove
        btn.myUIObject.directionalActions[(int) Directions.NORTH] = ReturnToListFromBottomButton;

        return btn;
    }

    //when we press UP while focused on a bottom button.
    private void ReturnToListFromBottomButton(int obj)
    {
        //look at the list, find the last active button, and switch to that.
        for (int t = listAllButtons.Count-1; t >= 0; t--)
        {
            if (listAllButtons[t].gameObject.activeSelf)
            {
                UIManagerScript.PlayCursorSound("Move");
                UIManagerScript.ChangeUIFocusAndAlignCursor(listAllButtons[t].myUIObject);
                onCursorPositionInListUpdated(this);
                return;
            }
        }

        //Move back up to the top of the page since we can't find a button
        UIManagerScript.UIObject nextFocus = neighborTop ?? UIManagerScript.GetDefaultUIFocus();
        UIManagerScript.ChangeUIFocusAndAlignCursor(nextFocus);
        UIManagerScript.PlayCursorSound("Move");
    }

    void SetAllListenersOnButton(Switch_InvItemButton btn)
    {
        if (btn == null)
        {
            return;
        }

        btn.SetEventListener(EventTriggerType.BeginDrag, btn.Default_OnDrag);
        btn.SetEventListener(EventTriggerType.Drop, btn.Default_OnDrop);
        btn.SetEventListener(EventTriggerType.PointerEnter, btn.Default_OnPointerEnter);
        btn.SetEventListener(EventTriggerType.PointerExit, btn.Default_OnPointerExit);
        btn.SetEventListener(EventTriggerType.PointerClick, btn.Default_OnClick);
    }

    public void ForceBottomButtonToggleState(bool bLeftToggled, bool bRightToggled)
    {
        btn_BottomLeft.ToggleButton(bLeftToggled);
        btn_BottomRight.ToggleButton(bRightToggled);
    }

    public void SetLabel(string s)
    {
        if (text_TopLabel != null)
        {
            text_TopLabel.text = s;
        }
    }

    public int GetNumObjectsInList()
    {
        if (listHeldObjects == null) return 0;
        return listHeldObjects.Count;
    }

    public void PlaceObjectsInList(List<ISelectableUIObject> listSelectables, bool bForceResetOffset = true)
    {
        if (listHeldObjects == null ||
            listHeldObjects.Count != listSelectables.Count)
        {
            bForceResetOffset = true;
        }

        //update what we are holding
        listHeldObjects = listSelectables;

        //reset our offset
        if (bForceResetOffset)
        {
            iOffsetFromTopOfList = 0;
            iLastGoodScrollbarOffset = -1;
        }

        //update the list
        UpdateButtonsAndScrollbar();
    }

    void UpdateButtonsAndScrollbar()
    {
        UpdateButtonsInList();
        UpdateScrollbar();
    }

    void UpdateButtonsInList()
    {
        if (displayMode == EDisplayMode.text_info_only)
        {
            return;
        }

        for (int iBtnIdx = 0; iBtnIdx < listAllButtons.Count; iBtnIdx++)
        {
            Switch_InvItemButton currentButton = listAllButtons[iBtnIdx];
            //place an object from our list of held objects into the button
            //the object number is iBtnIdx + offset,
            //if that number is greater than our list of buttons, disable the button instead
            int iObjectIdx = iBtnIdx + iOffsetFromTopOfList;
            if (iObjectIdx >= listHeldObjects.Count)
            {
                //sleepy button sleeps now
                currentButton.myUIObject.gameObj.SetActive(false);
                currentButton.myUIObject.enabled = false; // AA new 1/15/18, prevents null navigation
            }
            else
            {
                //wake up!
                currentButton.myUIObject.enabled = true;

                currentButton.myUIObject.gameObj.SetActive(true);
                currentButton.iSubmitValue[0] = iBtnIdx;

                var obj = listHeldObjects[iObjectIdx];
                if (currentButton.txtLabel != null)
                {
                    currentButton.txtLabel.text = obj.GetNameForUI();
                }

                currentButton.myUIObject.subObjectImage.sprite = obj.GetSpriteForUI();
                currentButton.SetContainedData(obj);
            }
        }

        RunUpdateActionOnButtons();
    }

    void UpdateScrollbar()
    {
        if (iOffsetFromTopOfList == iLastGoodScrollbarOffset)
        {
            return;
        }

        int iNumButtons = listAllButtons.Count;
        int iNumThings = listHeldObjects.Count;

        int iMaxOffset = iNumThings - iNumButtons;
        mySlider.maxValue = iMaxOffset;

        //Lock this here so we don't call ourselves ontop of ourselves, seven and sevenfold times.
        iLastGoodScrollbarOffset = iOffsetFromTopOfList;
        mySlider.value = iOffsetFromTopOfList;

        iLastGoodScrollbarOffset = iOffsetFromTopOfList;
    }

    //We do this to allow keyboard / controller navigation
    public void AddButtonsToUIObjectMasterList()
    {
        ImpactUI_Base.AddUIObjectsFromListOfButtons(listAllButtons, UIManagerScript.allUIObjects);
        if (btn_BottomLeft != null)
        {
            UIManagerScript.allUIObjects.Add(btn_BottomLeft.myUIObject);
            UIManagerScript.allUIObjects.Add(btn_BottomRight.myUIObject);
        }
    }

    //Do something to all the buttons in the list -- perhaps change the color of the text based on some condition
    public void AdjustButtonInformationViaAction(Action<Switch_InvItemButton, int[], string[]> actionButton)
    {
        actionAdjustButtonContentOnUpdate = actionButton;
        RunUpdateActionOnButtons();
    }

    void RunUpdateActionOnButtons()
    {
        if (listAllButtons == null || actionAdjustButtonContentOnUpdate == null)
        {
            return;
        }

        intRegisterAllActions = new int[5];
        stringRegisterAllActions = new string[5];

        foreach (Switch_InvItemButton btn in listAllButtons)
        {
            actionAdjustButtonContentOnUpdate(btn, intRegisterAllActions, stringRegisterAllActions);
        }
    }

    public void SetActionForEventOnButtons(EventTriggerType eventType, Action<int[], Switch_InvItemButton.ELastInputSource> action )
    {
        foreach (Switch_InvItemButton btn in listAllButtons)
        {
            switch (eventType)
            {
                case EventTriggerType.BeginDrag:
                    btn.onDrag = action;
                    break;
                case EventTriggerType.PointerClick:
                    btn.onClickAction = action;
                    break;
                case EventTriggerType.PointerEnter:
                    btn.onPointerEnter = action;
                    break;
                case EventTriggerType.PointerExit:
                    btn.onPointerExit = action;
                    break;
                case EventTriggerType.Drop:
                    btn.onDrop = action;
                    break;
            }
        }
    }

    #region UI Callbacks

    public void BeginDragGenericObjectFromButton(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        // Don't allow messing w/ hotbar with this game modifier on.
        if (UIManagerScript.CheckSkillSheetState() 
            && !GameModifiersScript.CanUseAbilitiesOutsideOfHotbar()
            && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            if (!MapMasterScript.activeMap.dungeonLevelData.safeArea)
            {
                GameModifiersScript.PlayerTriedToAlterSkills();
                UIManagerScript.PlayCursorSound("Error");
                if (GameModifiersScript.CheckForSwitchAbilitiesTutorialPopup())
                {
                    UIManagerScript.ForceCloseFullScreenUIWithNoFade();
                }
                return;
            }
        }
        var obj = listAllButtons[iValue[0]].GetContainedData();
        UIManagerScript.BeginDragGenericObject(obj, listAllButtons[iValue[0]].gameObject);
    }


    #endregion

    public void AdjustOffsetByFullPage(PageScroll pageDir)
    {
        int iNumButtons = listAllButtons.Count;
        AdjustOffsetViaScrolling((int)pageDir * iNumButtons);
        if (pageDir == PageScroll.DOWN)
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
        else
        {
            UIManagerScript.PlayCursorSound("Tick");
        }
    }

    public void AdjustOffsetViaScrolling( int iDelta )
    {
        int iOldOffset = iOffsetFromTopOfList;
        int iNumButtons = listAllButtons.Count;

        iOffsetFromTopOfList += iDelta;      

        //bound the offset from 0 to (number of things) - (number of buttons)
        int iNumThings = listHeldObjects.Count;
        int iMaxOffset = iNumThings - iNumButtons;

        iOffsetFromTopOfList = Math.Max(0, Math.Min(iOffsetFromTopOfList, iMaxOffset));


        //did we move?
        if (iOldOffset != iOffsetFromTopOfList)
        {
            UIManagerScript.PlayCursorSound("Move");
            onCursorPositionInListUpdated(this);
        }
        else
        {
            //play brmmp noise
            UIManagerScript.PlayCursorSound("Tick");
            //don't move
        }

        UpdateScrollbar();
    }

    //Use the controller/keys to move up and down this menu.
    //When moving left or right, jump over to the uiObject assigned to us
    //hopefully earlier
    public void MoveCursorToNeighbor(Switch_InvItemButton originButton, int dir)
    {
        //if (Debug.isDebugBuild) Debug.Log("Move in column function?");
        if (originButton == null)
        {
            originButton = listAllButtons.Find(p => p.myUIObject == UIManagerScript.uiObjectFocus);
        }

        if (originButton == null)
        {
            return;
        }

        int iOriginIndex = originButton.myID;
        UIManagerScript.UIObject nextObject = originButton.myUIObject.neighbors[dir];

        //Catch L/R inputs to leave the column if necessary
        bool bMotionCanScrollList = true;
        if (dir == (int)Directions.EAST)
        {
            nextObject = neighborRight;
            bMotionCanScrollList = false;
        }
        else if (dir == (int)Directions.WEST)
        {
            nextObject = neighborLeft;
            bMotionCanScrollList = false;
        }

        if (nextObject != null )
        {
            //if the object exists, but is inactive, we are trying to scroll to a button
            //that is turned off, probably because we're a list that's not full yet.
            if (nextObject.gameObj.activeSelf == false || !nextObject.enabled)
            {
                UIManagerScript.UIObject nextFocus = null;
                
                if (dir == (int) Directions.SOUTH)
                {
                    nextFocus = neighborBottom;
                }
                else if (dir == (int) Directions.NORTH)
                {
                    nextFocus = neighborTop;
                }

                if (nextFocus != null)
                {
                    UIManagerScript.PlayCursorSound("Move");
                    UIManagerScript.ChangeUIFocusAndAlignCursor(neighborBottom);
                }
                else
                {
                    UIManagerScript.PlayCursorSound("Tick");
                }

                return;

            }

            // new 1/17 to get the little twean boop when you move from item list to the weapon area
            Switch_InvItemButton switchBtn = nextObject.gameObj.GetComponent<Switch_InvItemButton>();
            Switch_InvVerticalHotbarButton vertiBtn = nextObject.gameObj.GetComponent<Switch_InvVerticalHotbarButton>();
            if (switchBtn != null)
            {
                switchBtn.Default_OnPointerEnter(null);                
            }
            else if (vertiBtn != null)
            {
                vertiBtn.OnPointerEnter(null);
            }            
            

            UIManagerScript.PlayCursorSound("Move");
            UIManagerScript.ChangeUIFocusAndAlignCursor(nextObject);
            if (nextObject.myOnSelectAction != null)
            {
                nextObject.myOnSelectAction.Invoke(nextObject.onSelectValue);
            }

            if (onCursorPositionInListUpdated != null)
            {
                onCursorPositionInListUpdated(this);
            }
        }
        else //nextObject is null
        {
            //don't do all this scroll checking code if our motion wasn't scrolling the list
            if (!bMotionCanScrollList)
            {
                //if (Debug.isDebugBuild) Debug.Log("Can't scroll list.");
                //play brmmp noise
                UIManagerScript.PlayCursorSound("Tick");
                //don't move
                return;
            }

            //perhaps we've moved off the top or bottom of the screen?
            UIManagerScript.UIObject newFocus = UIManagerScript.uiObjectFocus;
            int iOldOffset = iOffsetFromTopOfList;
            if (iOriginIndex == 0 && dir == (int) Directions.NORTH)
            {
                iOffsetFromTopOfList = Math.Max(0, iOffsetFromTopOfList - 1);

                //if we didn't change the index, we must be ready to leave the top
                if (iOldOffset == iOffsetFromTopOfList)
                {
                    newFocus = neighborTop;
                }

                //if (Debug.isDebugBuild) Debug.Log("Changed offset from north input");

            }
            else if (iOriginIndex >= listAllButtons.Count - 1 && dir == (int) Directions.SOUTH)
            {
                //if (Debug.isDebugBuild) Debug.Log("Cur offset is " + iOffsetFromTopOfList);

                //our maximum offset is the number of items total - the number of buttons we can display
                iOffsetFromTopOfList = Math.Min(iOffsetFromTopOfList + 1, listHeldObjects.Count - listAllButtons.Count);

                //if (Debug.isDebugBuild) Debug.Log("Is now: " + iOffsetFromTopOfList);

                //if we didn't change the index, we must be ready to leave the top
                if (iOldOffset == iOffsetFromTopOfList)
                {
                    newFocus = neighborBottom;
                }

                /* if (Debug.isDebugBuild)
                {
                    Debug.Log("Changed offset from south input. Origin i: " + iOriginIndex + " Old offset: " + iOldOffset + " Button count: " + (listAllButtons.Count-1));
                    Debug.Log("Offset from top: " + iOffsetFromTopOfList + " total objects: " + listHeldObjects.Count);
                } */


            }

            if (newFocus != null && listAllButtons.Any( btn => btn.myUIObject == newFocus) )
            {
                UIManagerScript.PlayCursorSound("Move");
                onCursorPositionInListUpdated(this);

                //if (Debug.isDebugBuild) Debug.Log("I guess we moved");
            }
            else
            {
                //if (Debug.isDebugBuild) Debug.Log("Some kind of failure");
                //play brmmp noise
                UIManagerScript.PlayCursorSound("Tick");
                //don't move
                if (newFocus != null)
                {
                    //if (Debug.isDebugBuild) Debug.Log("But new focus is not null.");
                    UIManagerScript.ChangeUIFocusAndAlignCursor(newFocus);
                }
            }
        }


        UpdateScrollbar();

    }

    public UIManagerScript.UIObject GetTopUIObject()
    {
        if (listAllButtons != null && listAllButtons.Count > 0)
        {
            return listAllButtons[0].myUIObject;
        }

        return null;
    }

    public ISelectableUIObject GetContainedDataFrom(int i)
    {
        return listAllButtons[i].GetContainedData();
    }

    public Switch_InvItemButton GetButtonContaining(ISelectableUIObject obj)
    {
        return listAllButtons.Find(p => p.GetContainedData() == obj);
    }

    //Look for an object by refname, we may not have the actual objectref any more but
    //perhaps we're keeping track of what we used last time?
    public Switch_InvItemButton GetButtonContaining(string strObjectRef)
    {
        foreach (var b in listAllButtons)
        {
            Item maybeItam = b.GetContainedData() as Item;
            AbilityScript maybeAbil = b.GetContainedData() as AbilityScript;

            if (maybeItam != null && maybeItam.actorRefName == strObjectRef)
            {
                return b;
            }
            if (maybeAbil != null && maybeAbil.refName == strObjectRef)
            {
                return b;
            }
        }

        return null;
    }


    //Returns true if the current UI focus is in our list of buttons
    public bool HasUIFocus()
    {
        return listAllButtons.Find( p => p.myUIObject == UIManagerScript.uiObjectFocus) != null;
    }

    public bool ShouldReceiveScrollWheelInput()
    {
        GameObject currentPointerObj = EventSystem.current.currentSelectedGameObject;
        return listAllButtons.Find(p => p.gameObject == currentPointerObj) != null;
    }
}
