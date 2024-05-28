using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using UnityEngine.EventSystems;

public partial class TitleScreenScript
{

    int checkFrames = 0;

    // Update is called once per frame
    void Update()
    {
        if (TitleScreenState == ETitleScreenStates.unloaded)
        {
            StartCoroutine(PrepareTitleScreenScrolling());
            return;
        }
        if (TitleScreenState == ETitleScreenStates.loading)
        {
            return;
        }
        if (framesBeforeClearDebug > 0)
        {
            framesBeforeClearDebug--;
        }

        if (framesBeforeClearDebug == 0 && !LogoSceneScript.globalIsSolsticeBuild)
        {
            Debug.ClearDeveloperConsole();
            Debug.developerConsoleVisible = false;
        }

        if (UIManagerScript.FadingToGame) return;

        if (blackFading)
        {
            float percentComplete = (Time.time - timeScrollStarted) / blackFadeInTime;
            if (percentComplete >= 1.0f)
            {
                blackFadeImage.color = new Color(0f, 0f, 0f, 0f);
                blackFading = false;
            }
            else
            {
                blackFadeImage.color = new Color(0f, 0f, 0f, 1f - percentComplete);
            }
        }

        framesSinceNeutral++;

        /* checkFrames++;
        if (checkFrames == 60 && CharCreation.singleton != null && CharCreation.singleton.NameInputTextBox != null)
        {
            Debug.Log("Name input text box active? " + CharCreation.singleton.NameInputTextBox.IsActive() + " Focused? " + CharCreation.singleton.NameInputTextBox.isFocused + " Interactable? " + CharCreation.singleton.NameInputTextBox.interactable + " Is input field eventsystem target? " + (EventSystem.current.currentSelectedGameObject == CharCreation.singleton.NameInputTextBox));
            checkFrames = 0;
        } */

        if (!allowInput || CharCreation.NameEntryScreenState == ENameEntryScreenState.game_loading_stop_updating)
        {
            //Debug.Log("Name entry state is wrong.");
            return;
        }

        if (modManager.AnyModUIOpen())
        {
            Cursor.visible = true;
            return;
        }

        bool dpadPressedThisFrame = false;
        if (GetDirectionalInput(out dpadPressedThisFrame) != Directions.NEUTRAL)
        {

            // Some kind of direction is down.
            if (framesSinceNeutral > 1)
            {
                // We've been holding a direction down.
                if (bufferingInput)
                {
                    inputBufferCount += Time.deltaTime;
                    if (inputBufferCount >= movementInputOptionsTime)
                    {
                        inputBufferCount = 0;

                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                // First time direction pressed, so just execute it.
                timeDirectionPressed = Time.fixedTime;
                bufferingInput = true;
                inputBufferCount = 0;
            }
        }
        else
        {
            // No direction was pressed
            bufferingInput = false;
            framesSinceNeutral = 0;
        }

        bool confirmPressed = player.GetButtonDown("Confirm");
        
        if (scrollingTitleBG)
        {
		if (!PlatformVariables.GAMEPAD_ONLY)
			{
	            TDInputHandler.lastActiveControllerType = ReInput.controllers.GetLastActiveControllerType();		
			}			
        }
        if (scrollingTitleBG && bReadyForMainMenuDialog && (confirmPressed || TDTouchControls.GetMouseButtonDown(0)))
        {
            for (int i = 0; i < layerMovers.Length; i++)
            {
                layerMovers[i].FinishMovement();
            }
            // Uncomment this to allow finishing of the scroll.
            FinishTitleScroll();
            return;
        }
        else if (scrollingTitleBG)
        {
            float timeThisFrame = Time.time;

            float percentComplete = (timeThisFrame - timeScrollStarted) / bgScrollTime;

            if ((timeThisFrame - timeScrollStarted) >= logoStartFadeInTime && !logoFading)
            {
                logoFading = true;
                timeLogoStarted = timeThisFrame;
            }

            float pTextComplete = 0f;

            switch (creditsFadeTextState)
            {
                case TextFadeStates.WAIT_TO_FADE_IN:
                    if (timeTextEvent == 0f)
                    {
                        timeTextEvent = timeThisFrame;
                    }
                    pTextComplete = (timeThisFrame - timeTextEvent) / textWaitToFadeTime;
                    if (pTextComplete > 1f) pTextComplete = 1f;
                    if (pTextComplete >= 1.0f)
                    {
                        creditsFadeTextState = TextFadeStates.FADING_IN;
                        timeTextEvent = timeThisFrame;
                    }
                    break;
                case TextFadeStates.FADING_IN:
                    pTextComplete = (timeThisFrame - timeTextEvent) / textFadeTime;
                    if (pTextComplete > 1f) pTextComplete = 1f;
                    creditsText.color = new Color(1f, 1f, 1f, pTextComplete);
                    if (pTextComplete >= 1.0f)
                    {
                        creditsFadeTextState = TextFadeStates.HOLDING;
                        timeTextEvent = timeThisFrame;
                    }
                    break;
                case TextFadeStates.HOLDING:
                    pTextComplete = (timeThisFrame - timeTextEvent) / textHoldTime;
                    if (pTextComplete > 1f) pTextComplete = 1f;
                    if (pTextComplete >= 1.0f)
                    {
                        creditsFadeTextState = TextFadeStates.FADING_OUT;
                        timeTextEvent = timeThisFrame;
                    }
                    break;
                case TextFadeStates.FADING_OUT:
                    pTextComplete = (timeThisFrame - timeTextEvent) / textFadeOutTime;
                    if (pTextComplete > 1f) pTextComplete = 1f;
                    creditsText.color = new Color(1f, 1f, 1f, 1f - pTextComplete);
                    if (pTextComplete >= 1.0f)
                    {
                        creditsFadeTextState = TextFadeStates.COMPLETE;
                    }
                    break;
            }


            if (logoFading)
            {
                float pc = (timeThisFrame - timeLogoStarted) / (bgScrollTime - logoStartFadeInTime);
                logoCG.alpha = pc;
                bool canDoShara = true;
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
                {
                    if (DLCManager.GetLastPlayedCampaign() != StoryCampaigns.SHARA)
                    {
                        canDoShara = false;
                        dragonsColor.a = pc;
                        dragonsImage.color = dragonsColor;
                    }
                }
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
                    && DLCManager.ShouldShowLegendOfSharaTitleScreen())
                {
                    sharaColor.a = pc;
                    legendOfSharaImage.color = sharaColor;
                }
            }

            if (percentComplete >= 1.0f)
            {
                FinishTitleScroll();
            }

            return;
        }

        //if (confirmPressed && Debug.isDebugBuild) Debug.Log("Confirm pressed in title input. Stage? " + CreateStage);

        // Handle Input for NAMEINPUT is different and special.
        if (CreateStage == CreationStages.NAMEINPUT)
        {
            bool dPadPressed = false;
            if (HandleSelectNameAndConfirmInput(GetDirectionalInput(out dPadPressed)))
            {
                CharCreation.singleton.UpdateCursorForNameSelect();
                return;
            }
        }
        // Does this need to be outside the code below?
        if (confirmPressed && CreateStage == CreationStages.NAMEINPUT)
        {
            if (!submitConfirm && UIManagerScript.RequireDoubleConfirm())
            {
                submitConfirm = true;
                return;
            }
            uims.CursorConfirm();
            submitConfirm = false;
            return;
        }


        if ((UIManagerScript.AnyInteractableWindowOpen() || CharCreation.creationActive) && 
            (CreateStage != CreationStages.SELECTSLOT || (CreateStage == CreationStages.SELECTSLOT && cursorIsOnChangePages)))
        {
            Directions dInput = GetDirectionalInput(out dpadPressedThisFrame);
            if (dInput != Directions.NEUTRAL)
            {
                bool checkForReturnToSelectSlotObjects = false;
                // UIManagerScript.uiObjectFocus.button.dialogEventScriptValue
                if (UIManagerScript.uiObjectFocus != null && UIManagerScript.uiObjectFocus.button != null)
                {
                    if (UIManagerScript.uiObjectFocus.button.actionRef == "changepages")
                    {
                        checkForReturnToSelectSlotObjects = true;
                    }
                }                

                if (UIManagerScript.nameInputOpen)
                {
                    if (CharCreation.nameInputTextBox.isFocused && !dpadPressedThisFrame) return;
                }
                if (dInput == Directions.SOUTH || dInput == Directions.SOUTHEAST || dInput == Directions.SOUTHWEST)
                {
                    if (checkForReturnToSelectSlotObjects && 
                        (UIManagerScript.dialogUIObjects.Count == 1 || UIManagerScript.uiObjectFocus.button.dialogEventScriptValue == "next"))
                    {
                        TitleScreenScript.titleScreenSingleton.NavigateFromChangePagesToTop();
                        return;
                    }
                    UIManagerScript.MoveCursor(Directions.SOUTH);
                }
                if (dInput == Directions.NORTH || dInput == Directions.NORTHEAST || dInput == Directions.NORTHWEST)
                {
                    if (checkForReturnToSelectSlotObjects &&
                        (UIManagerScript.dialogUIObjects.Count == 1 || UIManagerScript.uiObjectFocus.button.dialogEventScriptValue == "previous"))
                    {
                        TitleScreenScript.titleScreenSingleton.NavigateFromChangePagesToBottom();
                        return;
                    }
                    UIManagerScript.MoveCursor(Directions.NORTH);
                }
                if (dInput == Directions.EAST)
                {
                    UIManagerScript.MoveCursor(Directions.EAST);
                }
                if (dInput == Directions.WEST)
                {
                    UIManagerScript.MoveCursor(Directions.WEST);
                }
                return;
            }

            if (confirmPressed)
            {
                if (!submitConfirm && UIManagerScript.RequireDoubleConfirm())
                {
                    submitConfirm = true;
                    return;
                }
                uims.CursorConfirm();
                submitConfirm = false;
                return;
            }
        }
        // cancel character create
        // back character create
        //Load/Save screen update and input, after the scrolling title code above
        if (CreateStage == CreationStages.SELECTSLOT)
        {
            //bounce if we've made our selection
            bool bShouldBounce = HandleSelectSlotInput();

            //position the selecto-hand appropriately
            //UpdateCursorDuringSlotSelection();

            if (bShouldBounce) return;

        }
        if (player.GetButtonDown("Cancel"))
        {
            // Return to previous page?
            if (CreateStage == CreationStages.SELECTSLOT)
            {
                ReturnToMenu();
                return;
            }
            else if (CreateStage == CreationStages.NAMEINPUT) // Cancel on name input
            {
#if UNITY_SWITCH
                // If the input field is activated, let the Switch widget handle everything
                if (UIManagerScript.charCreationInputFieldActivated)
                {
                    return;
                }
#endif
                if (UIManagerScript.titleScreenNameInputDone)
                {
                    UIManagerScript.ResetNameInputCursor();
                }
                else
                {
                    // Previously, this was the first thing after save slot. Now, it is AFTER job selection.
                    UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
                    UIManagerScript.CloseDialogBox();

                    if (RandomJobMode.preparingEntryForRandomJobMode)
                    {
                        uims.StartCharacterCreation_DifficultyModeSelect();
                        CreateStage = CreationStages.SELECTMODE;
                    }
                    else
                    {
                        CharCreation.singleton.StartCharacterCreation_FeatSelect();
                        CreateStage = CreationStages.PERKSELECT;
                    }
                }
            }
            else if (CreateStage == CreationStages.SELECTMODE) // Cancel on select mode
            {
                //if we have the expansion, go back to campaign select instead.
                if (CharCreation.IsSharaCampaignAvailable())
                {
                    uims.StartCharacterCreation_ChooseCampaign();
                    return;
                }

                //Otherwise, back to the main menu
                ReturnToMenu();
            }
            else if (CreateStage == CreationStages.CAMPAIGNSELECT)
            {
                //close the campaign select window and return to the title screen
                uims.CloseCampaignSelectWithoutChoosing();
                ReturnToMenu();

            }
            else if (CreateStage == CreationStages.JOBSELECT)
            {
                if (CharCreation.jobSelected)
                {
                    CharCreation.DeselectJob();
                    return;
                }
                else
                {
                    CharCreation.singleton.EndCharCreation();
                    UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
                    UIManagerScript.CloseDialogBox();
                    uims.StartCharacterCreation_DifficultyModeSelect();
                    CreateStage = CreationStages.SELECTMODE;
                    return;
                }

            }
            else if (CreateStage == CreationStages.PERKSELECT)
            {
                //if we have previously picked the Shara campaign, instead of going back to job select,
                //go back to mode select.
                if (SharaModeStuff.IsSharaModeActive())
                {
                    //copy pasta from above, but I'm not sure what all these calls do :( 
                    CharCreation.singleton.EndCharCreation();
                    UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
                    UIManagerScript.CloseDialogBox();
                    uims.StartCharacterCreation_DifficultyModeSelect();
                    CreateStage = CreationStages.SELECTMODE;
                    return;
                }

                UIManagerScript.myDialogBoxComponent.GetDialogText().GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
                UIManagerScript.CloseDialogBox();

                CreateStage = CreationStages.JOBSELECT;
                CharCreation.singleton.BeginCharCreation_JobSelection();

                return;
            }
            else if (CreateStage == CreationStages.COMMUNITY)
            {
                Vector2 size;
                Vector2 pos;
                pos = SetMainMenuDialogBoxSizeAndPosition(out size);
                TitleScreenScript.CreateMainMenuDialog(size, pos);
                CreateStage = CreationStages.TITLESCREEN;
            }
        }
    }
}
