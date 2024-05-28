using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public partial class UIManagerScript
{
    public class UIObject
    {
        public GameObject gameObj;
        public Image subObjectImage;
        public TextMeshProUGUI subObjectTMPro;
        public UIObject[] neighbors;
        public ButtonCombo button; // Used only for dialog stuff.
        public Action<int> mySubmitFunction;
        public Action<string, GameObject> SoundFunction;
        public Func<int> myInfoFunction;
        public Action<int> myOnSelectAction;
        public Action<int> myOnExitAction;
        public int onExitValue;
        public int onSelectValue;
        public int onSubmitValue;
        public int uiGroup;
        public bool enabled;

        //If this is true, the hand cursor will not display here even when focused.
        public bool bHideCursorWhileFocused;

        //Default behavior is for the cursor to appear on the left side, and point right.
        public bool bForceCursorToRightSideAndFaceLeft;

        public Action<int>[] directionalActions;
        public int[] directionalValues;

        public UIObject()
        {
            enabled = true;
            neighbors = new UIObject[8];
            directionalActions = new Action<int>[8];
            directionalValues = new int[8];
            directionalActions[(int)Directions.NORTH] = MoveCursorToNeighbor;
            directionalActions[(int)Directions.EAST] = MoveCursorToNeighbor;
            directionalActions[(int)Directions.SOUTH] = MoveCursorToNeighbor;
            directionalActions[(int)Directions.WEST] = MoveCursorToNeighbor;
            directionalValues[(int)Directions.NORTH] = (int)Directions.NORTH;
            directionalValues[(int)Directions.EAST] = (int)Directions.EAST;
            directionalValues[(int)Directions.WEST] = (int)Directions.WEST;
            directionalValues[(int)Directions.SOUTH] = (int)Directions.SOUTH;

        }

        public virtual void Update()
        {

        }
        public void FocusOnMe()
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(this);
        }

        public void ClearHighlight(int dummy)
        {
            gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
        }

        public void GoToBottomOfCorralFoodList(int amount)
        {
            if (listArrayIndexOffset != 0)
            {
                TryScrollPool(-1);
                return;
            }

            SetListOffset(MonsterCorralScript.playerItemList.Count - MonsterCorralScript.corralFoodButtons.Length); // Loop to the bottom of the list
            if (listArrayIndexOffset < 0)
            {
                SetListOffset(0);
            }
            UpdateMonsterCorralFoodList();
            if (listArrayIndexOffset == 0)
            {
                ChangeUIFocusAndAlignCursor(MonsterCorralScript.corralFoodButtons[MonsterCorralScript.playerItemList.Count - 1]);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(MonsterCorralScript.corralFoodButtons[MonsterCorralScript.corralFoodButtons.Length - 1]);
            }
            //uiObjectFocus.myOnSelectAction.Invoke(uiObjectFocus.onSelectValue);
        }

        public void TryScrollPool(int amount)
        {
            //if (Debug.isDebugBuild) Debug.Log("Trying to scroll pool.");

            int maxUIObjectButtons = 0;
            int maxItemsInList = 0;

            if (ShopUIScript.CheckShopInterfaceState())
            {
                maxUIObjectButtons = ShopUIScript.shopItemButtonList.Length;
                maxItemsInList = ShopUIScript.playerItemList.Count;
            }
            else if (ItemWorldUIScript.itemWorldInterfaceOpen)
            {
                maxUIObjectButtons = ItemWorldUIScript.itemListButtons.Length;
                maxItemsInList = ItemWorldUIScript.playerItemList.Count;
            }
            else if (MonsterCorralScript.corralFoodInterfaceOpen)
            {
                maxUIObjectButtons = MonsterCorralScript.corralFoodButtons.Length;
                maxItemsInList = MonsterCorralScript.playerItemList.Count;
            }
            else if (GetWindowState(UITabs.SKILLS))
            {
                switch (selectedUIObjectGroup)
                {
                    case UI_GROUP_JOBSKILLS: // Reserve passives
                        //Debug.Log("Don't scroll this?!");
                        return;
                }
            }
            else if (jobSheetOpen)
            {
                return;
            }

            PlayCursorSound("Move"); // was tick.

            //Debug.Log("Pool scrolling, offset is now " + listArrayIndexOffset + " " + singletonUIMS.GetIndexOfSelectedButton() + " " + maxUIObjectButtons);

            int btnIndex = singletonUIMS.GetIndexOfSelectedButton();

            int indexToCheck = listArrayIndexOffset;

            Switch_UIEquipmentScreen eqCurrent = singletonUIMS.GetCurrentFullScreenUI() as Switch_UIEquipmentScreen;
            if (eqCurrent != null)
            {
                indexToCheck = eqCurrent.itemColumn.iOffsetFromTopOfList;
            }

            if (GetWindowState(UITabs.INVENTORY)) // Three columns
            {
                //
                //
                //   l   o   l
                //
                //
            }
            else // ***** NOT INVENTORY SHEET **********
            {
                if ((btnIndex == 0) || (btnIndex == 1)) // At most 2 columns...
                {
                    if (amount < 0) // Move NORTH from the first button
                    {
                        if (indexToCheck == 0) // There is no offset.
                        {
                            if (!ShopUIScript.CheckShopInterfaceState()) // There is something above the top item in the shop, don't loop back to bottom.
                            {
                                if (maxItemsInList > maxUIObjectButtons)
                                {
                                    SetListOffset(maxItemsInList - maxUIObjectButtons); // Loop to the bottom of the list
                                }
                            }

                            MoveCursorToNeighbor((int)Directions.NORTH);

                            if ((!ShopUIScript.CheckShopInterfaceState()) && (!ItemWorldUIScript.itemWorldInterfaceOpen))
                            {
                                SetListOffset(0);
                            }
                        }
                        else
                        {
                            // Move the offset up by one.
                            SetListOffset(indexToCheck + amount, eqCurrent);
                            if (eqCurrent != null)
                            {
                                eqCurrent.UpdateContent();
                            }
                            if (listArrayIndexOffset < 0)
                            {
                                SetListOffset(0);
                            }
                        }
                    }
                    else
                    {
                        // Move SOUTH
                        MoveCursorToNeighbor((int)Directions.SOUTH);
                    }
                }
                else if ((btnIndex == maxUIObjectButtons - 1) || (btnIndex == maxUIObjectButtons - 2))
                {
                    // Bottom button
                    if (amount < 0) // Move NORTH from the bottom button
                    {
                        // Just move the position up.
                        MoveCursorToNeighbor((int)Directions.NORTH);
                    }
                    else // Move SOUTH from the bottom button
                    {
                        // Try scrolling the pool if possible
                        if (maxUIObjectButtons >= maxItemsInList) // No offset needed.
                        {
                            //Debug.Log("No offset needed");
                            MoveCursorToNeighbor((int)Directions.SOUTH);
                        }
                        else
                        {
                            if (listArrayIndexOffset == maxItemsInList - maxUIObjectButtons)
                            {
                                //Debug.Log("Loopback");
                                int oldIndex = listArrayIndexOffset;
                                if (!GetWindowState(UITabs.SKILLS))
                                {
                                    SetListOffset(0);
                                }
                                MoveCursorToNeighbor((int)Directions.SOUTH);
                            }
                            else
                            {
                                // Just scroll pool.

                                listArrayIndexOffset += amount;
                                if (listArrayIndexOffset < 0)
                                {
                                    SetListOffset(0);
                                }
                                if (listArrayIndexOffset + maxUIObjectButtons >= maxItemsInList)
                                {
                                    SetListOffset(maxItemsInList - maxUIObjectButtons); // New code - don't go beyond bounds... ie down in left column goes to bottom right?
                                }
                            }

                        }
                    }
                }
            }

            if (ShopUIScript.CheckShopInterfaceState())
            {
                ShopUIScript.UpdateShop();
                int index = singletonUIMS.GetIndexOfSelectedButton();
                singletonUIMS.ShowItemInfo(index); // New code to try to make keyboard display items deep in the shop.                                

                // was indxpos - offset
            }
            else if (ItemWorldUIScript.itemWorldInterfaceOpen)
            {
                UpdateItemWorldList(ItemWorldUIScript.menuState == ItemWorldMenuState.SELECTORB);
                int index = singletonUIMS.GetIndexOfSelectedButton();
                ItemWorldUIScript.singleton.ShowItemInfo(index); // New code to try to make keyboard display items deep in the shop.
            }
            else if (MonsterCorralScript.corralFoodInterfaceOpen)
            {
                UpdateMonsterCorralFoodList();
            }

            //Debug.Log(listArrayIndexOffset + " " + playerSupportAbilities.Count + " " + skillPassiveAbilities.Length);

        }

        public void ChangeSliderValue(int amount)
        {
            if (uiObjectFocus == null) return;
            if (!movingSliderViaKeyboard) return; // will this break mouse movement?

            PlayCursorSound("Tick");
            Slider sli = uiObjectFocus.gameObj.GetComponent<Slider>();
            if (sli == null) return;
            float value = sli.value;
            value += amount;
            if (value > sli.maxValue)
            {
                value = sli.maxValue;
            }
            if (value < sli.minValue)
            {
                value = sli.minValue;
            }
            sli.value = value;

            if (uiObjectFocus == optionsFramecap)
            {
                PlayerOptions.framecap = (int)sli.value;
                GameMasterScript.UpdateFrameCapFromOptionsValue();
                UpdateFrameCapText();
            }
            else if (uiObjectFocus == optionsCursorRepeatDelay)
            {
                PlayerOptions.cursorRepeatDelay = (int)sli.value;
                GameMasterScript.UpdateCursorRepeatDelayFromOptionsValue();
                UpdateCursorRepeatDelayText();
            }
            else if (uiObjectFocus == optionsButtonDeadZone)
            {
                PlayerOptions.buttonDeadZone = (int)sli.value;
                GameMasterScript.UpdateButtonDeadZoneFromOptionsValues();
                UpdateButtonDeadZoneText();
            }
            else if (uiObjectFocus == optionsResolution)
            {
                Resolution selection = singletonUIMS.availableDisplayResolutions[(int)optionsResolution.gameObj.GetComponent<Slider>().value];
                //Debug.Log("SET player resolution via options slider: " + selection.width + "," + selection.height);
                PlayerOptions.resolutionX = selection.width;
                PlayerOptions.resolutionY = selection.height;
                UpdateResolutionText();
            }
            else if (uiObjectFocus == optionsTextSpeed)
            {
                PlayerOptions.textSpeed = (int)sli.value;
                singletonUIMS.UpdateTextSpeed();
            }
            else if (uiObjectFocus == optionsBattleTextSpeed)
            {
                PlayerOptions.battleTextSpeed = (int)sli.value;
                singletonUIMS.UpdateTextSpeed();
            }
            else if (uiObjectFocus == optionsBattleTextScale)
            {
                PlayerOptions.battleTextScale = (int)sli.value;
                singletonUIMS.UpdateBattleTextScale();
            }
        }

        //All purpose function to play a click and return the truth when an option
        //checkbox is pressed.
        bool DoOptionToggleAndGetState(GameObject myObject)
        {
            //disable the focus if we're not it. This fixes the weirdness
            //players might see if they are bouncing from keyboard control
            //to mouse control. It will disable any other objects
            //that have a highlight over them.
            if (highlightedOptionsObject != myObject)
            {
                singletonUIMS.DeselectOptionsSlider(0);
            }

            //play the cleek
            PlayCursorSound("Tick");

            //find the thing and return y/n
            Toggle tgl = gameObj.GetComponent<Toggle>();
            return tgl.isOn;
        }

        public void ToggleFullscreen(int amount)
        {
            PlayerOptions.fullscreen = DoOptionToggleAndGetState(gameObj);
            singletonUIMS.ToggleFullscreen();
        }

        public void ToggleScanlines(int amount)
        {
            PlayerOptions.scanlines = DoOptionToggleAndGetState(gameObj);
            mainCamera.GetComponent<CameraController>().UpdateScanlinesFromOptionsValue();
        }

        public void ToggleControllerPrompts(int amount)
        {
            if (!GameMasterScript.gameLoadSequenceCompleted) return;
            PlayerOptions.showControllerPrompts = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleAudioOffWhenMinimized(int amount)
        {
            if (!GameMasterScript.gameLoadSequenceCompleted) return;
            PlayerOptions.audioOffWhenMinimized = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleGridOverlay(int amount)
        {
            PlayerOptions.gridOverlay = DoOptionToggleAndGetState(gameObj);
            UpdateGridOverlayFromOptionsValue();
        }

        public void ToggleTutorialPopups(int amount)
        {
            PlayerOptions.tutorialTips = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleShowJPXPGain(int amount)
        {
            PlayerOptions.battleJPXPGain = DoOptionToggleAndGetState(gameObj);
        }

        public void TogglePlayerHealthBar(int amount)
        {
            if (!GameMasterScript.gameLoadSequenceCompleted) return;
            PlayerOptions.playerHealthBar = DoOptionToggleAndGetState(gameObj);
            GameMasterScript.gmsSingleton.TogglePlayerHealthBar();
        }

        public void ToggleMonsterHealthBars(int amount)
        {
            PlayerOptions.monsterHealthBars = DoOptionToggleAndGetState(gameObj);
            GameMasterScript.gmsSingleton.ToggleMonsterHealthBars();
        }

        public void ToggleScreenFlashes(int amount)
        {
            PlayerOptions.screenFlashes = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleSmallCombatLogText(int amount)
        {
            PlayerOptions.smallLogText = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleUIPulses(int amount)
        {
            PlayerOptions.showUIPulses = DoOptionToggleAndGetState(gameObj);
            GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
        }

        public void ToggleShowRumorOverlay(int amount)
        {
            PlayerOptions.showRumorOverlay = DoOptionToggleAndGetState(gameObj);
            RumorTextOverlay.OnRumorOverlayToggleChanged();
        }

        public void ToggleAutoEatFood(int amount)
        {
            PlayerOptions.autoEatFood = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleDisableMouseMovement(int amount)
        {
            PlayerOptions.disableMouseMovement = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleAutoAbandonRumors(int amount)
        {
            PlayerOptions.autoAbandonTrivialRumors = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleAutoPlanksInItemWorld(int amount)
        {
            PlayerOptions.autoPlanksInItemWorld = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleAutoEquipBestOffhand(int amount)
        {
            PlayerOptions.autoEquipBestOffhand = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleAutoEquipWeapons(int amount)
        {
            PlayerOptions.autoEquipWeapons = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleGameLogVerbosity(int amount)
        {
            PlayerOptions.verboseCombatLog = DoOptionToggleAndGetState(gameObj);
        }

        public void TogglePickupDisplay(int amount)
        {
            PlayerOptions.pickupDisplay = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleExtraTurnPopup(int amount)
        {
            PlayerOptions.extraTurnPopup = DoOptionToggleAndGetState(gameObj);
        }

        public void ToggleJoystickStyle(int amount)
        {
            OptionsUIScript.singleton.ToggleJoystickStyle();
        }

        public void ToggleDisableMouse(int amount)
        {
            PlayerOptions.disableMouseOnKeyJoystick = DoOptionToggleAndGetState(gameObj);
        }

        public void SaveAndQuit(int x)
        {
            GameMasterScript.gmsSingleton.SaveAndQuit();
        }

        public void SaveAndBackToTitle(int x)
        {
            GameMasterScript.gmsSingleton.SaveAndQuitToTitle();
        }

        public void CloseShopFromButton(int x)
        {
            if (UIManagerScript.dialogBoxOpen)
            {
                return;
            }
            PlayCursorSound("Cancel");
            ShopUIScript.CloseShopInterface();
        }

        public void CloseJobFromButton(int x)
        {
            PlayCursorSound("Cancel");
            //CloseJobSheet();
        }

        public void CloseSkillFromButton(int x)
        {
            PlayCursorSound("Cancel");
            TryCloseSkillSheet();
        }

        public void MoveCursorToNeighbor(int dir)
        {
            //if (Debug.isDebugBuild) Debug.Log("Move cursor to neighbor " + dir);
            if (dir < 0 || dir > 7)
            {
                return;
            }
            if (uiObjectFocus == null)
            {
                //Debug.Log("Null focus from " + uiObjectFocus.gameObj.name + " " + (Directions)dir);
                return;
            }
            //Debug.Log("Valid focus");
            if (neighbors[dir] == null)
            {
                //Debug.Log("No neighbor " + (Directions)dir + " of " + gameObj.name);
                return;
            }

            //Debug.Log("Move from " + uiObjectFocus.gameObj.name + " " + (Directions)dir + " to " + neighbors[dir].gameObj.name + " active? " + neighbors[dir].enabled);

            // Moving to one of the fancy new buttons from an Old and Busted ui element
            // Let's call the button's method instead of this one.

            if (neighbors[dir].enabled)
            {
                Switch_InvItemButton switchBtn = neighbors[dir].gameObj.GetComponent<Switch_InvItemButton>();
                Switch_InvVerticalHotbarButton vertiBtn = neighbors[dir].gameObj.GetComponent<Switch_InvVerticalHotbarButton>();
                if (switchBtn != null)
                {
                    switchBtn.Default_OnPointerEnter(null);
                }
                else if (vertiBtn != null)
                {
                    //cool thanks                
                    PlayCursorSound("Move");
                    vertiBtn.OnPointerEnter(null);
                    return;
                }
            }

            UIObject switchToObj = neighbors[dir];
            //if (Debug.isDebugBuild) Debug.Log("Move to neighbor from " + uiObjectFocus.gameObj.name + " to " + switchToObj.gameObj.name);

            Directions dirSwitchedFrom = MapMasterScript.oppositeDirections[(int)dir];

            if (neighbors[dir].enabled == false)
            {
                //if (Debug.isDebugBuild) Debug.Log("Object " + (Directions)dir + " neighbor " + neighbors[dir].gameObj.name + " is not enabled");

                bool foundObj = false;
                UIObject navigate = neighbors[dir];
                int tries = 0;
                while ((!foundObj) && (tries < 30))
                {
                    navigate = navigate.neighbors[dir];
                    //Debug.Log("Check " + navigate.gameObj.name);
                    if ((navigate == null) || (navigate == switchToObj))
                    {
                        //if (Debug.isDebugBuild) Debug.Log("Navigate is null, or equal to switchobj.");
                        break;
                    }
                    if (navigate.enabled)
                    {
                        foundObj = true;
                        break;
                    }
                    else
                    {
                        //if (Debug.isDebugBuild) Debug.Log(navigate.gameObj.name + " isn't enabled");
                    }
                    tries++;
                }

                if (!foundObj)
                {
                    // Experimental. If we're moving EAST or WEST, try moving NORTH?
                    if (dir == (int)Directions.WEST || dir == (int)Directions.EAST)
                    {
                        //Debug.Log("Were west or east... what's next? " + neighbors[dir].gameObj.name + " " + neighbors[dir].neighbors[(int)Directions.NORTH].gameObj.name);
                        navigate = neighbors[dir].neighbors[(int)Directions.NORTH];
                        if (navigate != null && navigate.enabled)
                        {
                            foundObj = true;
                        }
                    }
                }

                if (tries >= 30)
                {


                    //Debug.Log("Infinite while loop during cursor neighbor find");
                    if (!foundObj)
                    {
                        return;
                    }

                }
                if (!foundObj)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Didn't find obj");
                    return;
                }
                switchToObj = navigate;
                //if (Debug.isDebugBuild) Debug.Log("Switching to " + navigate.gameObj.name);
            }

            if (uiObjectFocus.myOnExitAction != null)
            {
                //if (Debug.isDebugBuild) Debug.Log("Invoking exit");
                uiObjectFocus.myOnExitAction.Invoke(uiObjectFocus.onExitValue);
            }

            ChangeUIFocus(switchToObj);
            SwitchSelectedUIObjectGroup(switchToObj.uiGroup);

            PlayCursorSound("Move");

            if (uiObjectFocus.myOnSelectAction != null)
            {
                //Debug.Log("Invoking select action of " + uiObjectFocus.gameObj.name);
                uiObjectFocus.myOnSelectAction.Invoke(uiObjectFocus.onSelectValue);
            }

            if (MonsterCorralScript.corralInterfaceOpen)
            {
                MonsterCorralScript.AdjustScrollbarAfterCursorScroll((Directions)dir);
            }

            //move cursor to right side if asked 
            if (switchToObj.bForceCursorToRightSideAndFaceLeft)
            {
                float fWidthOfObject = uiObjectFocus.gameObj.GetComponent<RectTransform>().sizeDelta.x;
                AlignCursorPos(uiObjectFocus.gameObj, fWidthOfObject, -4f, false);
            }
            else
            {
                AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
            }
        }

        public void ModifySlider(int amount)
        {
            Slider sli = gameObj.GetComponentInChildren<Slider>();
            if (sli == null)
            {
                return;
            }

            amount = (int)Mathf.Clamp(amount, sli.minValue, sli.maxValue);
            sli.value = amount;
        }
    }
}