using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TitleScreenScript {

    public static bool cursorIsOnChangePages = false;

    int currentModIndex;
    int CurrentModIndex
    {
        get
        {
            return currentModIndex;
        }
        set
        {
            currentModIndex = value;
        }
    }

    /// <summary>
    /// Controls input during the Select Save Slot Screen.
    /// </summary>
    /// <returns>True if input received</returns>
    private bool HandleSelectSlotInput()
    {
        //Debug.Log("Handle select slot input special case in TitleScreenScript");
        // 312019 - Something with AudioStuff.PlayCue is failing here

        //if we hit cancel, just dip out
        if (player.GetButtonDown("Cancel"))
        {
            ReturnToMenu();
            return true;
        }

        //we may need this
        var uisingleton = UIManagerScript.singletonUIMS;

        //Check the pad for movement
        int iOldIdx = idxActiveSaveSlotInMenu;

        bool dpadPressed = false;

        Directions dInput = GetDirectionalInput(out dpadPressed);
        bool bInputReceived = false;

        int modIndexThisFrame = idxActiveSaveSlotInMenu % 3;
        // 0 = top slot
        // 1 = mid
        // 2 = bottom

        if (cursorIsOnChangePages)
        {
            if (dInput == Directions.SOUTH || dInput == Directions.NORTH)
            {
                return HandleChangePagesInput(dInput);
            }
            return false;
        }

        if (dInput == Directions.SOUTH)
        {
            bInputReceived = true;            
            idxActiveSaveSlotInMenu++;
            CheckForWrappingOfActiveIndex(modIndexThisFrame);
        }
        else if (dInput == Directions.NORTH)
        {
            bInputReceived = true;
            idxActiveSaveSlotInMenu--;
            CheckForWrappingOfActiveIndex(modIndexThisFrame);
        }

        if (bInputReceived)
        {
            //make a sound, one way or the other, and then return.            
            UIManagerScript.PlayCursorSound("Move");

            // If we navigated to the change page button, hop out of here
            if (cursorIsOnChangePages) return true;

            idxActiveSaveSlotInMenu = Mathf.Clamp(idxActiveSaveSlotInMenu, 0, GameMasterScript.kNumSaveSlots - 1);

            UpdateCursorDuringSlotSelection();
            return true;
        }

        //if we pushed butan, then do something with the save slot we are targeting
        if (player.GetButtonDown("Confirm"))
        {
            confirmInputThisFrame = false;
            OnSelectSlotConfirmPressed(-1);
            return confirmInputThisFrame;
        }

        //we didn't detect any input
        return false;
    }

    void CheckForWrappingOfActiveIndex(int prevModIndex)
    {
        //Debug.Log("Checking for wrapping index");
        CurrentModIndex = idxActiveSaveSlotInMenu % 3;

        bool shouldWrap = false;

        if (idxActiveSaveSlotInMenu == -1 || (prevModIndex == 0 && CurrentModIndex == 2))
        {
            idxActiveSaveSlotInMenu++;
            //if (Debug.isDebugBuild) Debug.Log("Pushed up at top");
            shouldWrap = true;
        }
        else if ((prevModIndex == 2 && CurrentModIndex == 0) || idxActiveSaveSlotInMenu >= GameMasterScript.kNumSaveSlots)
        {
            idxActiveSaveSlotInMenu--;
            //if (Debug.isDebugBuild) Debug.Log("Pushed down at bottom");
            shouldWrap = true;
        }
        
        if (shouldWrap)
        {
            currentModIndex = idxActiveSaveSlotInMenu % 3;

            foreach (var response in UIManagerScript.dialogUIObjects)
            {
                if (response.button.dbr == DialogButtonResponse.CONTINUE && response.button.actionRef == "changepages")
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(response);
                    cursorIsOnChangePages = true;
                    return;
                }
            }
        }
    }

    public void OnSelectSlotConfirmPressed(int slot)
    {
        if (slot != -1) idxActiveSaveSlotInMenu = slot;

        confirmInputThisFrame = true;

        //what are we here for?
        GameStartData.saveGameSlot = idxActiveSaveSlotInMenu;

        UIManagerScript uisingleton = UIManagerScript.singletonUIMS;

        if (UIManagerScript.saveDataDisplayComponents == null || GameStartData.saveGameSlot >= UIManagerScript.saveDataDisplayComponents.Length || GameStartData.saveGameSlot < 0
|| UIManagerScript.saveDataDisplayComponents[GameStartData.saveGameSlot] == null)
        {
            //Debug.Log("Something is null in handle select slot input.");
            confirmInputThisFrame = false;
            return; // 5302019 - this really should not happen, but maybe the component/slot was not yet initialized or set here?
        }

        var selectedSDDC = UIManagerScript.saveDataDisplayComponents[GameStartData.saveGameSlot];

        /* if (player.GetButton("UI Page Left") && player.GetButton("UI Page Right") && player.GetButton("Toggle Pet Hud") &&
            selectedSDDC.displayType == SaveDataDisplayBlock.ESaveDataDisplayType.load_game &&
            reasonWeAreInSelectSlotWindow == EWhyAreWeInTheSelectSlotWindow.because_load_game)
        {
            SaveUploadManager.TryUploadSaveData(idxActiveSaveSlotInMenu);
            return false;
        } */

        //if this is the case, load the game if the slot has a game in it, 
        //or start a new game if the slot is empty.
        if (reasonWeAreInSelectSlotWindow == EWhyAreWeInTheSelectSlotWindow.because_load_game ||
            reasonWeAreInSelectSlotWindow == EWhyAreWeInTheSelectSlotWindow.because_new_game)
        {
            //Is this save slot a candidate for NG+? If so, we want to show a dialog while keeping these windows open.
            if (selectedSDDC.saveInfo.bGameClearSave)
            {
                PromptNGPlusDialogSwitch();
                confirmInputThisFrame = true;
                return;
            }

            //Don't update the slot select boxes, because they are gone!
            CreateStage = CreationStages.COUNT;

            //hide these, we're done with them
            foreach (var displaySlot in UIManagerScript.saveDataDisplayComponents)
            {
                displaySlot.gameObject.SetActive(false);
            }

            //if we have selected because_new_game, then we need clear out any save data in this file. In that case,
            //we behave the same as if we had picked ManageData for this slot.
            if (selectedSDDC.displayType == SaveDataDisplayBlock.ESaveDataDisplayType.load_game)
            {
                if (reasonWeAreInSelectSlotWindow == EWhyAreWeInTheSelectSlotWindow.because_new_game)
                {
                    //there's a save file here, what do?
                    //Maybe goodbye to file for now
                    SpawnDialogForDataDeletion();
                    confirmInputThisFrame = true;
                    return;

                }

                //otherwise, load the game as normal.
                UIManagerScript.PlayCursorSound("OPSelect");
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.LOADGAME);
                UIManagerScript.HideDialogMenuCursor();
                uisingleton.StartCoroutine(uisingleton.FadeOutThenLoadGame());
            }
            else
            {
                //start a new one
                UIManagerScript.PlayCursorSound("OPSelect");
                UIManagerScript.SetGlobalResponse(DialogButtonResponse.NEWGAME);
                UIManagerScript.HideDialogMenuCursor();
                
                if (CharCreation.IsSharaCampaignAvailable())
                //if (SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE))
                {
                    uisingleton.StartCharacterCreation_ChooseCampaign();
                }
                else
                {
                    uisingleton.StartCharacterCreation_Mirai();
                }

                confirmInputThisFrame = true;
                return;
            }
        }
        else if (reasonWeAreInSelectSlotWindow == EWhyAreWeInTheSelectSlotWindow.because_delete_game)
        {
            //if this slot has no data to delete, make a noise and return.
            if (selectedSDDC.displayType == SaveDataDisplayBlock.ESaveDataDisplayType.empty_af)
            {
                UIManagerScript.PlayCursorSound("Error");
                confirmInputThisFrame = true;
                return;
            }

            //hide these, we're done with them
            foreach (var displaySlot in UIManagerScript.saveDataDisplayComponents)
            {
                displaySlot.gameObject.SetActive(false);
            }

            //Don't update the slot select boxes, because they are gone!
            CreateStage = CreationStages.COUNT;

            //Maybe goodbye to file for now
            SpawnDialogForDataDeletion();

            confirmInputThisFrame = true;
            return;
        }

        confirmInputThisFrame = false;
        return;
    }

    public void OnHoverPrefabSaveBlockSlot(int slot)
    {
        idxActiveSaveSlotInMenu = slot;
        CurrentModIndex = idxActiveSaveSlotInMenu % 3;
        UpdateCursorDuringSlotSelection();
        cursorIsOnChangePages = false;
    }

    public bool HandleChangePagesInput(Directions navDir)
    {

        if (navDir == Directions.NORTH)
        {            
            OnHoverPrefabSaveBlockSlot(idxActiveSaveSlotInMenu);
        }
        return false;
    }
}
