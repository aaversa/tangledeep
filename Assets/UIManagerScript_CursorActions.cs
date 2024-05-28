using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public partial class UIManagerScript
{
    public class CursorUpdateData
    {
        public GameObject obj;
        public float xOffset;
        public float yOffset;
        public GameObject cursor;
        public bool waiting;
        public int framesWaited = 0;
    }

    public void ChangeCursorOpacity(float fNewValue)
    {
        if (fCursorOpacity != fNewValue)
        {
            fCursorOpacity = fNewValue;
            uiDialogMenuCursorImage.color = new Color(uiDialogMenuCursorImage.color.r, uiDialogMenuCursorImage.color.g, uiDialogMenuCursorImage.color.b, fCursorOpacity); // Color.white;
            Animatable anim = uiDialogMenuCursorImage.GetComponent<Animatable>();
            anim.opacityMod = fCursorOpacity;
        }

    }

    public void EnableCursor(bool bForceDisplay = false)
    {
        bool bShouldDisplay = true;
        if (uiObjectFocus != null &&
            uiObjectFocus.bHideCursorWhileFocused)
        {
            bShouldDisplay = false;
        }

        if (uiDialogMenuCursor != null &&
            (bForceDisplay || AnyInteractableWindowOpen()))
        {
            if (!uiDialogMenuCursor.activeInHierarchy)
            {
                fCursorOpacity = 1.0f;
                uiDialogMenuCursor.SetActive(true);
            }
            uiDialogMenuCursorImage.enabled = bShouldDisplay;
        }
        else
        {
            //Debug.Log("Cursor null, or no window open. " + (uiDialogMenuCursor==null) + " " + AnyInteractableWindowOpen() + " " + dialogBoxOpen);
        }

    }

    public void DisableCursor()
    {
        if (uiDialogMenuCursor != null)
        {
            if (CheckHotbarNavigating())
            {
                return;
            }
            uiDialogMenuCursor.SetActive(false);
        }
        else
        {
            uiDialogMenuCursor = GameObject.Find("Dialog Cursor");
            uiDialogMenuCursorImage = uiDialogMenuCursor.GetComponent<Image>();
            uiDialogMenuCursorAudioComponent = uiDialogMenuCursor.GetComponent<AudioStuff>();
            uiDialogMenuCursorBounce = uiDialogMenuCursor.GetComponent<CursorBounce>();
        }
    }

    //For UI objects, only GetWorldCorners provides accurate measurements.
    public static bool ChangeCursorOpacityIfInBounds(Vector3[] vCorners, float fDesiredOpacity)
    {
        GameObject cursor = UIManagerScript.singletonUIMS.uiDialogMenuCursor;
        if (vCorners == null || cursor == null)
        {
            return false;
        }

        //The returned array of 4 vertices is clockwise. It starts bottom left 
        Rect rectFromCorners = new Rect(vCorners[0].x, vCorners[0].y, vCorners[2].x - vCorners[0].x, vCorners[1].y - vCorners[0].y);

        //Cursor rect
        Vector3[] vCursorCorners = new Vector3[4];
        RectTransform cursorTransform = cursor.transform as RectTransform;
        cursorTransform.GetWorldCorners(vCursorCorners);
        Rect rectFromCursor = new Rect(vCursorCorners[0].x, vCursorCorners[0].y, vCursorCorners[2].x - vCursorCorners[0].x, vCursorCorners[1].y - vCursorCorners[0].y);

        if (rectFromCorners.Overlaps(rectFromCursor))
        {
            UIManagerScript.singletonUIMS.ChangeCursorOpacity(fDesiredOpacity);
            return true;
        }

        return false;
    }

    public static void ChangeUIFocusAndAlignCursor(UIObject obj, float xOffset = -5f, float yOffset = -4f)
    {
        ChangeUIFocus(obj);
        if (obj == null || obj.gameObj == null) return; // ????
        AlignCursorPos(obj.gameObj, xOffset, yOffset, false);
    }

    public static void ChangeUIFocus(UIObject obj, bool processEvent = true)
    {
        uiObjectFocus = obj;

        if (processEvent)
        {
            if (uiObjectFocus != null)
            {
                TitleScreenScript.OnChangedUIFocus(obj);
                EventSystem.current.SetSelectedGameObject(uiObjectFocus.gameObj);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    public void MoveCursorToUIObject(UIObject obj)
    {
        if (obj == null || !obj.enabled || !TryLoadDialogCursor())
        {
            return;
        }

        if (uiObjectFocus.myOnExitAction != null)
        {
            uiObjectFocus.myOnExitAction.Invoke(uiObjectFocus.onExitValue);
        }

        ChangeUIFocus(obj);
        SwitchSelectedUIObjectGroup(obj.uiGroup);
        PlayCursorSound("Move");
        //UpdateListIndex(Directions.NEUTRAL); // Should this always be neutral?
        if (uiObjectFocus.myOnSelectAction != null)
        {
            uiObjectFocus.myOnSelectAction.Invoke(uiObjectFocus.onSelectValue);
        }
        AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
    }

    public static void MoveCursor(Directions dir, float amountMultiplierForSliders = 1f)
    {
        //Debug.Log("Moving cursor " + dir);
        if (uiObjectFocus == null)
        {
            //Debug.Log("WARNING: Trying to move cursor but this is a null UI Object!");
            if (UIManagerScript.dialogBoxOpen)
            {
                try { ChangeUIFocusAndAlignCursor(dialogUIObjects[0]); }
                catch (Exception e)
                {
                    Debug.Log("Failed to move cursor in weird dialog situation");
                }
            }
            return;
        }

        // Hack for sliders.
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            if ((dir == Directions.WEST || dir == Directions.EAST) && ItemWorldUIScript.itemWorldInterfaceOpen)
            {
                float cVal = ItemWorldUIScript.itemGoldSlider.value;
                if (dir == Directions.WEST)
                {
                    cVal -= 0.025f;
                }
                else
                {
                    cVal += 0.025f;
                }
                cVal *= amountMultiplierForSliders;
                if (cVal < 0) cVal = 0;
                if (cVal > 1f) cVal = 1f;
                ItemWorldUIScript.itemGoldSlider.value = cVal;
                singletonUIMS.AdjustItemWorldGoldTribute();
                return;
            }
            if ((dir == Directions.WEST || dir == Directions.EAST) && singletonUIMS.dialogValueSliderParent.activeSelf)
            {
                float maxRange = singletonUIMS.dialogValueSlider.maxValue - singletonUIMS.dialogValueSlider.minValue;
                float tickAmount = maxRange * 0.01f;
                tickAmount *= amountMultiplierForSliders;
                float curValue = singletonUIMS.dialogValueSlider.value;

                if (singletonUIMS.dialogValueSlider.wholeNumbers)
                {
                    if (tickAmount < 1f)
                    {
                        tickAmount = 1f;
                    }
                }

                if (dir == Directions.WEST)
                {
                    curValue -= tickAmount;
                }
                else
                {
                    curValue += tickAmount;
                }
                if (curValue == singletonUIMS.dialogValueSlider.value)
                {
                    // Nothing - we are at min or max range.
                }
                else
                {
                    singletonUIMS.dialogValueSlider.value = curValue;
                    PlayCursorSound("Tick");
                }
                return;
            }

        }
        // End slider hack
        //Debug.Log("Moving cursor " + dir + " " + uiObjectFocus.gameObj.name + " " + uiObjectFocus.directionalActions[(int)dir].ToString() + " " + uiObjectFocus.directionalValues[(int)dir].ToString());
        UIObject prevFocus = uiObjectFocus;

        try { uiObjectFocus.directionalActions[(int)dir].Invoke(uiObjectFocus.directionalValues[(int)dir]); }
        catch (Exception e)
        {
            //Debug.Log("Error invoking directional action");
            Debug.Log(e);
            //Debug.Log("EQ " + GetWindowState(UITabs.EQUIPMENT) + " Inv " + GetWindowState(UITabs.INVENTORY) + " Char " + GetWindowState(UITabs.CHARACTER) + " Rumor " + GetWindowState(UITabs.RUMORS) + " Dialog " + dialogBoxOpen + " Shop " + ShopUIScript.CheckShopInterfaceState() + " Skill " + GetWindowState(UITabs.SKILLS) + " Job " + jobSheetOpen);
            if (uiObjectFocus == null)
            {
                //Debug.Log("Null focus.");
            }
            else
            {
                if (prevFocus != null)
                {
                    //Debug.Log("Current focus: " + uiObjectFocus.gameObj.name + " prev focus: " + prevFocus.gameObj.name);
                }
                else
                {
                    //Debug.Log("Current focus: " + uiObjectFocus.gameObj.name + " prev focus: NULL");
                }

            }
        }
    }

    private static void ShowEQBlinkingCursor()
    {
        swappingItems = true;
        eqBlinkingCursor.GetComponent<Image>().enabled = true;
        eqBlinkingCursor.SetActive(true);
    }

    public static void HideEQBlinkingCursor()
    {
        swappingItems = false;
        eqBlinkingCursor.GetComponent<Image>().enabled = false;
        eqBlinkingCursor.SetActive(false);

    }

    //Assumes use of the dialog cursor, which may not exist yet!
	// Came from switch Version
    public static void AlignCursorPos(GameObject alignmentTarget, float xOffset, float yOffset, bool waitToUpdate)
    {
        if (!TryLoadDialogCursor()) return;
        AlignCursorPos(singletonUIMS.uiDialogMenuCursor, alignmentTarget, xOffset, yOffset, waitToUpdate);
    }

    public static void AlignCursorPos(GameObject cursor, GameObject obj, float xOffset, float yOffset, bool waitToUpdate)
    {
        if (obj == null || cursor == null)
        {
            return;
        }

        //Debug.Log("Updating cursor position to " + obj.name);

        if (waitToUpdate)
        {
            //cursor.SetActive(false);
            cursor.GetComponent<Image>().color = UIManagerScript.transparentColor;
            waitToUpdateCursorData.cursor = cursor;
            waitToUpdateCursorData.waiting = true;
            waitToUpdateCursorData.obj = obj;
            waitToUpdateCursorData.xOffset = xOffset;
            waitToUpdateCursorData.yOffset = yOffset;
            waitToUpdateCursorData.framesWaited = 0;
            return;
        }

        singletonUIMS.EnableCursor();

        // EXPERIMENTAL
        if (dialogBoxOpen)
        {
            singletonUIMS.SetCursorAsChildOfDialogBox(cursor, obj);
            return;
        }

        // END EXPERIMENTAL

        singletonUIMS.EnableCursor();
        RectTransform p = obj.GetComponent<RectTransform>();
        Vector3 localPos = new Vector3(0, 0, 0);
        localPos = (-1 * p.right) * (p.rect.width / 2f);
        Vector3 global = p.TransformPoint(localPos);
        global.x += xOffset;
        global.y = obj.transform.position.y + yOffset;
        CursorBounce cb = cursor.GetComponent<CursorBounce>();
        cb.ResetBounce(global, obj);

    }

    static void UpdateCursorPosFromData()
    {
        if (waitToUpdateCursorData == null)
        {
            return;
        }
        else if ((waitToUpdateCursorData.obj == null) || (!waitToUpdateCursorData.obj.activeSelf))
        {
            return;
        }
        //singletonUIMS.SetCursorAsChildOfDialogBox(waitToUpdateCursorData.cursor, waitToUpdateCursorData.obj);

        singletonUIMS.EnableCursor();
        RectTransform p = waitToUpdateCursorData.obj.GetComponent<RectTransform>();
        Vector3 localPos = new Vector3(0, 0, 0);
        localPos = (-1 * p.right) * (p.rect.width / 2f);
        Vector3 global = p.TransformPoint(localPos);
        global.x += waitToUpdateCursorData.xOffset;
        global.y = waitToUpdateCursorData.obj.transform.position.y + waitToUpdateCursorData.yOffset;
        CursorBounce cb = waitToUpdateCursorData.cursor.GetComponent<CursorBounce>();
        cb.ResetBounce(global, waitToUpdateCursorData.obj);
        waitToUpdateCursorData.waiting = false;
        if (AnyInteractableWindowOpen())
        {
            waitToUpdateCursorData.cursor.SetActive(true);
        }
    }

    public static void UpdateDialogCursorPos()
    {
        /* if (listArrayIndexPosition < 0)
        {
            return;
        } */
        if (singletonUIMS.GetIndexOfSelectedButton() == -1)
        {
            return;
        }
        if (dialogObjects.Count == 0)
        {
            return;
        }

        AlignCursorPos(dialogObjects[singletonUIMS.GetIndexOfSelectedButton()], -5f, -4f, false); // was just index.
    }

    public static void UpdateJobSheetCursorPos()
    {
        if (!jobSheetOpen) return;
        if (uiObjectFocus == null) return;
        AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);

        UpdateJobSheet();

    }

    public static void UpdateSkillSheetCursorPos()
    {
        if (!GetWindowState(UITabs.SKILLS)) return;
        if (uiObjectFocus == null) return;
        AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);

        UpdateSkillSheet();

    }

    public static void UpdatePhysicalMousePosition()
    {
        singletonUIMS.lastPhysicalMousePosition = Input.mousePosition;
    }

    public static void FocusCursorViaMouse(GameObject obj)
    {
        for (int i = 0; i < allUIObjects.Count; i++)
        {
            if (obj == allUIObjects[i].gameObj)
            {
                if (!allUIObjects[i].enabled)
                {
                    return;
                }
                ChangeUIFocus(allUIObjects[i]);
                break;
            }
        }
        AlignCursorPos(obj, -5f, -4f, false);
    }

    public void SetCursorAsChildOfDialogBox(GameObject cursor, GameObject dialogBoxObject)
    {
        // Wow really bad special case but I don't know how else to do it!
        // Redirect cursor in a 3-column setup to point at the image and not the text
        if (dialogBoxObject.name.Contains("ThreeColumn"))
        {
            dialogBoxObject = dialogBoxObject.GetComponent<DialogButtonScript>().iconSprite.gameObject;
        }

        //Calls to SetParent need to pass in false as a second parameter BECAUSE UNITY, but mainly because
        //SetParent causes the scale to go off the scalerails without that param.
        cursor.transform.SetParent(dialogBoxObject.transform, false);

        float width = dialogBoxObject.GetComponent<RectTransform>().rect.width;
        float offset = width / 2f;
        Vector3 pos = Vector3.zero;
        pos.x -= (offset + 5f);

        //If we have a UIObject and it says be on the right side, then be on the right side.
        UIObject selectedShadowObject = allUIObjects.FirstOrDefault(o => o.gameObj == dialogBoxObject);
        if (selectedShadowObject != null &&
            selectedShadowObject.bForceCursorToRightSideAndFaceLeft)
        {
            pos.x += width;
            cursor.GetComponent<CursorBounce>().SetFacing(Directions.WEST);
        }
        else
        {
            cursor.GetComponent<CursorBounce>().SetFacing(Directions.EAST);
        }


        cursor.transform.localPosition = pos;
    }





    public static void HideDialogMenuCursor()
    {
        if (singletonUIMS == null) return;

        if (singletonUIMS.CheckHotbarNavigating())
        {
            return;
        }
        if (singletonUIMS != null && singletonUIMS.uiDialogMenuCursor != null)
        {
            singletonUIMS.uiDialogMenuCursorImage.enabled = false;
        }
        singletonUIMS.DisableCursor();

        //clear out anything we may have been holding in the cursor when we 
    }

    public void CursorConfirm()
    {
        if (typingText)
        {
            FinishTypewriterTextImmediately();
            return;
        }
        if (singletonUIMS.dialogInteractableDelayed)
        {
            return;
        }

        if (TitleScreenScript.CreateStage == CreationStages.NAMEINPUT && (uiObjectFocus == null || uiObjectFocus.gameObj == null))
        {
            if (CharCreation.nameInputTextBox != null)
            {
                CharCreation.nameInputTextBox.DeactivateInputField();
            }
            requireDoubleConfirm = false;
            ignoreNextButtonConfirm = false;
            ChangeUIFocusAndAlignCursor(CharCreation.titleScreenConfirmButton);

            //if (Debug.isDebugBuild) Debug.Log("Problem " + (uiObjectFocus == null) + " " + (uiObjectFocus.gameObj == null));

            return;
        }

        if (TitleScreenScript.CreateStage == CreationStages.JOBSELECT)
        {
            if (uiObjectFocus.mySubmitFunction != null)
            {
                uiObjectFocus.mySubmitFunction.Invoke(uiObjectFocus.onSubmitValue);
            }

            return;
        }

        if (uiObjectFocus == null || uiObjectFocus.gameObj == null)
        {
            //if (Debug.isDebugBuild) Debug.Log("No object focus. " + TitleScreenScript.CreateStage); 
            bool doReturn = true;
            if (dialogBoxOpen && UIManagerScript.currentConversation != null && !IsCurrentConversationKeyStory())
            {
                if (dialogUIObjects.Count == 1) // "More" or "Close"
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
                    doReturn = false;
                    //if (Debug.isDebugBuild) Debug.Log("Dialog objects 1");
                }
            }
            if (doReturn) return;
        }
        else
        {
            //Debug.Log("Confirming cursor press. " + uiObjectFocus.gameObj.name);
        }

        if (ignoreNextButtonConfirm)
        {
            ignoreNextButtonConfirm = false;
            //if (Debug.isDebugBuild) Debug.Log("Ignore next confirm"); 
            return;
        }

        if (MonsterCorralScript.corralInterfaceOpen || CharCreation.creationActive || CorralBreedScript.corralBreedInterfaceOpen
            || MonsterCorralScript.monsterStatsInterfaceOpen || MonsterCorralScript.corralGroomingInterfaceOpen ||
            MonsterCorralScript.corralFoodInterfaceOpen || ItemWorldUIScript.itemWorldInterfaceOpen || dialogBoxOpen ||
            GetWindowState(UITabs.EQUIPMENT) || GetWindowState(UITabs.INVENTORY) || jobSheetOpen || GetWindowState(UITabs.SKILLS)
            || GetWindowState(UITabs.OPTIONS) || ShopUIScript.CheckShopInterfaceState() || casinoGameOpen || GetWindowState(UITabs.COOKING)
            || nameInputOpen || GetWindowState(UITabs.RUMORS) || GetWindowState(UITabs.CRAFTING) || GetWindowState(UITabs.CHARACTER))
        {

            if (uiObjectFocus.mySubmitFunction == null)
            {
                //if (Debug.isDebugBuild) Debug.Log("No submit function for " + uiObjectFocus.gameObj.name);
                return;
            }
            else
            {
                try
                {
                    uiObjectFocus.mySubmitFunction.Invoke(uiObjectFocus.onSubmitValue);
                }
                catch (Exception e)
                {
                    Debug.Log("Error invoking submit function of " + uiObjectFocus.gameObj.name + " " + uiObjectFocus.onSubmitValue);
                    Debug.Log(e);
                }
                return;
            }
            //DialogCursorConfirm();
        }
    }

    public void MouseEnterUI()
    {
        isMouseOverUI = true;
        //Debug.Log("Mouse is over ui");
    }

    public void MouseEnterGameWorld()
    {
        isMouseOverGameWorld = true;
    }
    public void MouseExitGameWorld()
    {
        isMouseOverGameWorld = false;
    }

    public void MouseExitUI()
    {
        isMouseOverUI = false;
        //Debug.Log("Mouse is not over ui");
    }

    // Right now, both alphas are the same.


    public bool IsMouseInGameWorld()
    {
        if (isMouseOverGameWorld) return true;
        if (MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_FOREST_FLOOR) return true;
        return false;
    }

    public bool IsMouseOverUI()
    {
        if (!isMouseOverGameWorld || isMouseOverUI)
        {
            return true;
        }

        return false;
    }

    public void SetVirtualCursorPosition(Vector2 pos)
    {
        lastCursorPosition = virtualCursorPosition;
        SetVirtualCursorPosition_Internal(pos);
        UpdateVirtualCursorPosition();
    }

    void SetVirtualCursorPosition_Internal(Vector2 pos)
    {
        virtualCursorPosition = pos;
    }

    public void ChangeVirtualCursorPosition(Vector2 pos)
    {
        lastCursorPosition = virtualCursorPosition;
        virtualCursorPosition += pos;
        UpdateVirtualCursorPosition();
    }

    public Vector2 GetVirtualCursorPosition()
    {
        return virtualCursorPosition;
    }




    public static void PlayCursorSound(string cue)
    {
        if (PlayerOptions.audioOffWhenMinimized && !Application.isFocused)
        {
            return;
        }
        singletonUIMS.uiDialogMenuCursorAudioComponent.PlayCue(cue);
    }

    public void RefocusCursor()
    {
        if (uiObjectFocus == null) return; // why would it be null...?
        AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
    }

    public void FocusCursorOnThisObject(int value)
    {
        switch (value)
        {
            case 0:
                ChangeUIFocusAndAlignCursor(blackjackPlayGame);
                break;
            case 1:
                ChangeUIFocusAndAlignCursor(blackjackHit);
                break;
            case 2:
                ChangeUIFocusAndAlignCursor(blackjackStay);
                break;
            case 3:
                ChangeUIFocusAndAlignCursor(blackjackExit);
                break;
            case 4:
                ChangeUIFocusAndAlignCursor(slotsPlayGame);
                break;
            case 5:
                ChangeUIFocusAndAlignCursor(slotsExit);
                break;
        }
    }

    public void CursorToConfirmButton()
    {
        if (TitleScreenScript.titleScreenSingleton.player.GetButton("Cancel"))
        {
            return;
        }
        EnableCursor();
        ShowDialogMenuCursor();
        uiDialogMenuCursor.transform.SetParent(CharCreation.nameInputParentCanvasGroup.transform);
        ChangeUIFocusAndAlignCursor(CharCreation.titleScreenConfirmButton);
        ignoreNextButtonConfirm = true;
        titleScreenNameInputDone = true;
    }

    public IEnumerator WaitThenAlignCursor(float time, UIObject obj)
    {
        yield return new WaitForSeconds(time);
        EnableCursor();
        ChangeUIFocusAndAlignCursor(obj);
    }

    /// <summary>
    /// To be used ONLY in dialogs.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator WaitThenAlignCursorToSelectedObject(float time)
    {
        yield return new WaitForSeconds(time);

        EnableCursor();
        if (uiObjectFocus != null)
        {
            ChangeUIFocusAndAlignCursor(uiObjectFocus);
        }
        else
        {
            ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);
        }
    }
	
    private bool UpdateVirtualCursorPosition()
    {
        if (examineMode)
        {
            lastCursorPosition = virtualCursorPosition;
            UpdateExamineModeTiles();
            return true;
        }
        if (groundTargetingMesh == null) return true;
        TargetingMeshScript gtms = groundTargetingMesh.GetComponent<TargetingMeshScript>();

        var positionWasValid = true;
        if ((!gtms.goodTiles.Contains(virtualCursorPosition)) && (!gtms.badTiles.Contains(virtualCursorPosition)) && (!gtms.usedTiles.Contains(virtualCursorPosition)))
        {
            SetVirtualCursorPosition_Internal(lastCursorPosition);
            positionWasValid = false;
        }

        if (cursorTargetingMesh != null)
        {
            lastCursorPosition = virtualCursorPosition;
            UpdateCursorTargetingTiles();
        }
        return positionWasValid;
    }	
    /// <summary>
    /// Attempts to change a cursor position to a valid location. Use this when moving along a diagonal, and send in
    /// the naked X and Y values as alternates.
    /// </summary>
    /// <param name="tryThis"></param>
    /// <param name="thenThis"></param>
    /// <param name="finallyThis"></param>
    public void ChangeVirtualCursorPositionWithFallbacks(Vector2 tryThis, Vector2 thenThis, Vector2 finallyThis)
    {
        lastCursorPosition = virtualCursorPosition;

        virtualCursorPosition += tryThis;
        if (UpdateVirtualCursorPosition()) return;

        //not that
        virtualCursorPosition = lastCursorPosition + thenThis;
        if (UpdateVirtualCursorPosition()) return;

        //last chance
        virtualCursorPosition = lastCursorPosition + finallyThis;
        UpdateVirtualCursorPosition();

    }
    public static void ShowDialogMenuCursor(bool bForceGameObjectActive = false)
    {
        if (TryLoadDialogCursor())
        {
            singletonUIMS.uiDialogMenuCursorImage.enabled = true;
            singletonUIMS.EnableCursor();

            if (bForceGameObjectActive)
            {
                singletonUIMS.uiDialogMenuCursor.SetActive(true);
            }
        }
    }
	
    IEnumerator WaitThenAlignCursorPos(GameObject target, float xOffset, float yOffset)
    {
        if (!TryLoadDialogCursor())
        {
            yield break;
        }
        if (target == null || !target.activeSelf) yield break;

        uiDialogMenuCursorImage.color = transparentColor;
        yield return null;
        EnableCursor();
        AlignCursorPos(uiDialogMenuCursor, target, xOffset, yOffset, false);


    }	
}
