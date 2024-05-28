using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Rewired.UI.ControlMapper;
using LapinerTools.uMyGUI;
using System;
using UnityEngine.UI;
using System.IO;
using Random = System.Random;

public partial class TDInputHandler : MonoBehaviour {

    // References to other singletons/managers
    public static ControlMapper cMapper;
    public static GameMasterScript gms;
    public static UIManagerScript uims;
    public static Player player;

    public static bool initialized;

    // Constant values and control settings
    public const float CONTROLLER_STICKSCROLL_SPEEDMOD = 1.5f;
    static int mouseButtonForStraightMove = 1;
    static int mouseButtonForPathfind = 0;

    // Current control states
    static bool mouseMovement;
    static bool ignoreThisMouseAction;
    public static bool directionalInput = false;
    static int framesSinceCleanedMouseWalk;
    public static ControllerType lastActiveControllerType;
    public static int framesSinceNeutral;
    public static float timeSinceLastActionInput;
    public static float turnTimer;
    public static Directions lastInputDirection;
    static float inputBufferCount = 0.0f;
    public static bool bufferingInput = false;
    public static float timeSinceControlMapperWasOpen;
    public static float timeAtLastAbilSelection;
    static bool disableAllInput = false;
    public static int targetClicksRemaining;
    public static int targetClicksMax;
    static float[] inputAxes;

    // Needed for A* when using mouse
    static HashSet<MapTileData> openList;
    static MapTileData[] adjacent;
    static bool[] adjacentValid;
    static List<MapTileData> tilePath;
    static MapTileData startTile;
    static MapTileData evalTile;
    static MapTileData finalTile;
    static MapTileData nextMove = null;
    static MapTileData capturePFTile;
    static bool pathfindingToMousePosition = false;
    static bool movingOnLineToMousePosition = false;
    static GameObject pathfindingHighlight;

    public static float fLastTimeUpdateInputFinished;

    static bool ignoreDirectionalInputUntilNextNetural;

    /// <summary>
    /// When above 0, don't accept mouse clicks
    /// </summary>
    static float mouseInputDelay = 0f;

    /// <summary>
    /// Should be called anytime we open a full screen UI or a dialog for the *first* time. Will halt directional movement until our next neutral.
    /// </summary>
    public static void OnDialogOrFullScreenUIOpened()
    {
        ignoreDirectionalInputUntilNextNetural = true;
        GameMasterScript.gmsSingleton.StartCoroutine(WaitThenEnsureDirectionalInputIsEnabled(1.0f));
    }

    /// <summary>
    /// Safety coroutine to ensure after 'time' seconds that we can move around in menus/UIs again.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    static IEnumerator WaitThenEnsureDirectionalInputIsEnabled(float time)
    {
        yield return new WaitForSeconds(time);
        if (UIManagerScript.AnyInteractableWindowOpen())
        {
            ignoreDirectionalInputUntilNextNetural = false;
        }
    }

    public static void Initialize()
    {
		if (!PlatformVariables.GAMEPAD_ONLY)
		{
		        cMapper = GameMasterScript.gmsSingleton.cMapper;
		}
		


        pathfindingHighlight = Instantiate(GameMasterScript.GetResourceByRef("PlayerMouseTargeting"));
        pathfindingHighlight.SetActive(false);
        gms = GameMasterScript.gmsSingleton;
        uims = UIManagerScript.singletonUIMS;
        player = GameMasterScript.gmsSingleton.player;
        openList = new HashSet<MapTileData>();
        adjacent = new MapTileData[8];
        adjacentValid = new bool[8];
        tilePath = new List<MapTileData>();
        inputAxes = new float[2];

        initialized = true;

#if UNITY_EDITOR
        //Debug.Log("<color=green>INPUT HANDLER INITIALIZED!</color>");
#endif
    }

    public static void DelayMouseInput(float time)
    {
        mouseInputDelay += time;
    }

    public static void UpdateInput()
    {
        if (GameMasterScript.actualGameStarted && GameMasterScript.gmsSingleton.CurrentSaveGameState != SaveGameState.NOT_SAVING)
        {
            return;
        }

        bool debugInput = false;

        if (Debug.isDebugBuild)
        {
            debugInput = player.GetButton("Debug Input");
        }

#if UNITY_ANDROID || UNITY_IPHONE
        TDTouchControls.UpdateTouchControlsForThisFrame();
#endif
        if (!initialized) return;
    
        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            if (cMapper.isOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    cMapper.Close(true);
                }
                timeSinceControlMapperWasOpen = 0f;
                return;
            }
            else
            {
                timeSinceControlMapperWasOpen += Time.deltaTime;
            }
        }
        else
        {
            // If we are in gamepad only mode, and the text input field is active,
            // And we're on Switch
            // Let the Switch widget take over input.
#if UNITY_SWITCH
            if (UIManagerScript.textInputFieldIsActivated)
            {
                if (player.GetButton("Toggle Menu Select"))
                {
                    UIManagerScript.DeactivateTextInputField();
                }
                return;
            }
#endif
        }

#if UNITY_SWITCH
        if (Debug.isDebugBuild)
        {
            if (Switch_DebugMenu.HandleInput())
            {
                return;
            }
        }
#else
        if (DebugConsole.IsOpen) return;
#endif


        if (mouseInputDelay > 0f)
        {
            mouseInputDelay -= Time.deltaTime;
            if (mouseInputDelay < 0f)
            {
                mouseInputDelay = 0f;
            }
        }

        if (PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS && uMyGUI_PopupManager.Instance.IsPopupShown)
        {
            Cursor.visible = true;
            return;
        }

        //Check for Confirm being pressed and if so, avoid some of the 
        //input munching code that was written to slow input repeats.
        bool bConfirmPressedSoDontEatInput = player.GetButtonDown("Confirm");

        // Endgame / credits state?
        if (UIManagerScript.singletonUIMS.creditsRoll.UpdateInput())
        {
            return;
        }



        if (TDTouchControls.UpdateInput())
        {
            return;
        }

        if (!TDTouchControls.GetMouseButton(mouseButtonForPathfind) && pathfindingToMousePosition)
        {
            ClearMousePathfinding();
        }

        if (mouseInputDelay > 0f)
        {
            if (TDTouchControls.GetMouseButton(0) || TDTouchControls.GetMouseButton(1))
            {
                return;
            }            
        }

        if (CheckForConditionsThatHaltInput())
        {
            return;
        }        

        if (GameMasterScript.actualGameStarted && GameMasterScript.gameLoadSequenceCompleted)
        {
            framesSinceCleanedMouseWalk++;
            if (framesSinceCleanedMouseWalk > 30)
            {
                if (!TDTouchControls.GetMouseButton(0) && !TDTouchControls.GetMouseButton(1))
                {
                    ClearMousePathfinding();
                }
                framesSinceCleanedMouseWalk = 0;
            }

#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.LeftBracket) && Input.GetKey(KeyCode.RightBracket) && Input.GetKeyDown(KeyCode.R))
            {
                TDPlayerPrefs.DeleteAll();
                GameLogScript.GameLogWrite("All player preferences reset, please restart the game.", null);
            }
#endif


        }

        // Skip overlay text
        if (!UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.gameLoadSequenceCompleted)
        {
            if (!UIManagerScript.skippingOverlayText)
            {
                if (UIManagerScript.HasOverlayText())
                {
                    if (bConfirmPressedSoDontEatInput || TDTouchControls.GetMouseButtonDown(0))
                    {
                        UIManagerScript.SkipOverlayText();
                        return;
                    }
                }
            }
            if (MinimapUIScript.shouldMiniMapBeOpen && MinimapUIScript.MinimapState == MinimapStates.CLOSED)
            {
                MinimapUIScript.SetMinimapToSpecificState(MinimapUIScript.prevMinimapState);
            }
        }

        if (PlatformVariables.GAMEPAD_ONLY)
        {
            lastActiveControllerType = ControllerType.Joystick;
        }
        else
        {
            lastActiveControllerType = ReInput.controllers.GetLastActiveControllerType();
        }

        UpdateControllerPromptVisibilityBasedOnInputType();

        UIManagerScript.optionsUseStepMoveJoystickStyle.gameObj.GetComponent<Toggle>().isOn = PlayerOptions.joystickControlStyle == JoystickControlStyles.STEP_MOVE;

        if (PlatformVariables.GAMEPAD_ONLY)
        {
            if (PetPartyUIScript.singleton.HasFocus())
            {
                PetPartyUIScript.singleton.HandleInput();
                return;
            }

            Directions dirThisFrame = GetDirectionalInput();

            if (Switch_RadialMenu.HandleInput(dirThisFrame))
            {
                return;
            }
        }

        float localTime = 0.0f;
        if (UIManagerScript.AnyInteractableWindowOpen())
        {
            localTime = GameMasterScript.gmsSingleton.movementInputOptionsTime; // Cursor delay in options / dialog menus
        }
        else
        {
            localTime = GameMasterScript.gmsSingleton.movementInputDelayTime; // Ingame movement
        }

        turnTimer += Time.deltaTime;

        if (uims.CheckTargeting())
        {
            localTime = 0.15f;
        }

        Directions dInput = GetDirectionalInput();
        framesSinceNeutral++;

        if (dInput != Directions.NEUTRAL)
        {
            if (UIManagerScript.dialogBoxOpen && framesSinceNeutral == 1) // New: pushing directions will finish typing text. Why not?
            {
                UIManagerScript.FinishTypewriterTextImmediately();
            }
            //Clear TurnWasStopped if we are using a gamepad and change our inputs or push a button
            if (lastActiveControllerType == ControllerType.Joystick)
            {
                if (dInput != lastInputDirection ||
                    player.GetButtonUp("Confirm"))
                {
                    gms.turnWasStopped = false;
                }
            }

            if (gms.turnWasStopped)
            {
                return;
            }

            // Experimental; don't interrupt the attack animation for any reason.

            if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.heroPCActor.myAnimatable.IsAnimAttackAnimation())
            {

                if (GameMasterScript.heroPCActor.myAnimatable.GetCompletionPercentage() < 0.8f)
                {
                    return;
                }

            }

            directionalInput = true;

            if (framesSinceNeutral > 1)
            {
                // Holding direction
                if (bufferingInput)
                {
                    inputBufferCount += Time.deltaTime;
                    if (inputBufferCount >= localTime)
                    {
                        inputBufferCount = Mathf.Max(localTime * 0.8f, 0.1f);
                    }
                    else if (!bConfirmPressedSoDontEatInput)
                    {
                        // This was killing newgameplus input. Why?
                        return;
                    }
                }
            }
            else
            {
                bufferingInput = true;
                inputBufferCount = 0;
            }
            // End new code
        }
        else
        {
            // Neutral direction
            directionalInput = false;
            bufferingInput = false;
            gms.turnWasStopped = false;
            framesSinceNeutral = 0;
        }

        if (CheckForGameoverInput(dInput, bConfirmPressedSoDontEatInput))
        {
            return;
        }

        bool allowOnlyMovement = false;
        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_FOREST_FLOOR && !UIManagerScript.AnyInteractableWindowOpen())
        {
            // don't allow any other input in shara start sequence.
            allowOnlyMovement = true;
        }

        if (!allowOnlyMovement && Switch_RadialMenu.HandleInput(dInput))
        {
            return;
        }

        if (player.GetButtonDown("Cycle Hotbars") && !UIManagerScript.dialogBoxOpen && !allowOnlyMovement)
        {
            
            UIManagerScript.ToggleSecondaryHotbar();
        }

        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        CheckForInputAffectingDiagonalMovementOverlay(heroPCActor);

        if (HandleAbilityTargetingInput()) return;

        if (HandleExamineModeInput(bConfirmPressedSoDontEatInput)) return;

        //Shep: Moved this above HandleInteractable since HandleInteractable was eating confirm presses and
        //preventing controller based hotbar use
        if (HandleHotbarInput(dInput, bConfirmPressedSoDontEatInput) && !allowOnlyMovement)
        {
            return;
        }

        if (!PlatformVariables.GAMEPAD_ONLY && MinimapUIScript.GetOverlay() && Input.GetKeyDown(KeyCode.Escape))
        {
            MinimapUIScript.singleton.ToggleMiniMapFromButton();
            return;
        }

        if (HandleInteractableWindowInput(dInput, allowOnlyMovement, bConfirmPressedSoDontEatInput))
        {
            return;
        }

        if (player.GetAxis("Scroll UI Boxes Vertical") != 0f)
        {
            // Scroll combat log, probably
            float modified = player.GetAxis("Scroll UI Boxes Vertical") * CONTROLLER_STICKSCROLL_SPEEDMOD * Time.deltaTime;
            GameLogScript.TryScrollLog(modified);
        }

        if (GameMasterScript.playerDied) return;
        if (!GameMasterScript.actualGameStarted) return;
        if (UIManagerScript.dialogBoxOpen) return;

        // This will open or close sheets.

        bool keyDialogOpen = false;

        if (UIManagerScript.dialogBoxOpen 
            && (UIManagerScript.dialogBoxType == DialogType.LEVELUP
            || UIManagerScript.dialogBoxType == DialogType.KEYSTORY))
        {
            keyDialogOpen = true;
        }

        if (CheckForKeyboardUIShortcuts(allowOnlyMovement, keyDialogOpen)) return;

        if (!allowOnlyMovement && player.GetButtonDown("Hide UI") && !UIManagerScript.AnyInteractableWindowOpen())
        {
            UIManagerScript.TogglePlayerHUD();
        }

        if (CheckForToggleMenuSelectInput(allowOnlyMovement)) return;

        if (!allowOnlyMovement && UIManagerScript.GetWindowState(UITabs.OPTIONS) && player.GetButtonDown("Cancel"))
        {
            if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
            if (!UIManagerScript.playerHUDEnabled)
            {
                UIManagerScript.TogglePlayerHUD();
            }
            return;
        }

        if (!allowOnlyMovement && (player.GetButtonDown("Options Menu") || player.GetButtonDown("Toggle Menu Select")))
        {
            if (uims.CheckTargeting() && !UIManagerScript.CheckOptionsMenuState())
            {
                uims.ExitTargeting();
                GameMasterScript.gmsSingleton.SetItemBeingUsed(null);
                return;
            }
            if (UIManagerScript.examineMode)
            {
                UIManagerScript.singletonUIMS.CloseExamineMode();
                return;
            }

            if (!PlatformVariables.GAMEPAD_ONLY)
            {
                UIManagerScript.OpenFullScreenUI(UITabs.OPTIONS);
                MinimapUIScript.StopOverlay();
            }
            return;
        }

        if ((UIManagerScript.AnyInteractableWindowOpen() || UIManagerScript.GetWindowState(UITabs.CHARACTER)) && !uims.CheckHotbarNavigating())
        {
            return;
        }

        if (!allowOnlyMovement && player.GetButtonDown("View Help"))
        {
            if (UIManagerScript.CheckDialogBoxState())
            {
                if (UIManagerScript.dialogBoxType != DialogType.LEVELUP && !UIManagerScript.IsCurrentConversationKeyStory())
                {
                    UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                    return;
                }
            }

            Conversation tut = GameMasterScript.FindConversation("tutorial");
            if (tut == null)
            {
                return;
            }
            UIManagerScript.StartConversation(tut, DialogType.STANDARD, null);
            return;
        }


        if (!allowOnlyMovement && player.GetButtonDown("Examine Mode")) // WAS "Examine"
        {
            uims.ToggleExamine();
            return;
        }
                
        AbilityScript abilityToTry = GameMasterScript.GetAbilityToTry();

        CheckForMinimapInput(allowOnlyMovement);

        if (gms.turnExecuting)
        {
            // Insert buffer commands here.
            return;
        }

        if (GameMasterScript.IsNextTurnPausedByAnimations()) return;

        if (CheckForExamineModeInput()) return;
     
	    if (CheckForTargetingInput(allowOnlyMovement, abilityToTry, bConfirmPressedSoDontEatInput)) return;

        if (CheckForNPCInteractionsOnConfirm(bConfirmPressedSoDontEatInput, heroPCActor, dInput)) return;

        CheckForClearMousePathfindingOnButtonsUp();

        // Attack check here.

        // New 11/18 to prevent millions of movements
        if (Time.time - timeSinceLastActionInput <= gms.playerMoveSpeed - 0.01f) // was + 0.02
        {
            return;
        }

        if (TDTouchControls.GetMouseButtonDown(mouseButtonForStraightMove) || (TDTouchControls.GetMouseButtonDown(mouseButtonForPathfind) && !uims.CheckTargeting()))
        {
            Vector2 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 targetingPosition = new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f));
            float distance = MapMasterScript.GetGridDistance(heroPCActor.GetPos(), targetingPosition);
            if (distance < 1)
            {
                bool pickUp = TileInteractions.TryPickupItemsInHeroTile();
                pathfindingHighlight.SetActive(false);
                pathfindingToMousePosition = false;
                movingOnLineToMousePosition = false;
                bool stairs = TravelManager.TryTravelStairs();

                if (!pickUp && !stairs)
                {
                    if (Time.time - timeSinceLastActionInput >= (gms.playerMoveSpeed * 1.25f))
                    {
                        TurnData td = new TurnData();
                        td.actorThatInitiatedTurn = heroPCActor;
                        td.SetTurnType(TurnTypes.PASS);
                        timeSinceLastActionInput = Time.time;
                        gms.TryNextTurn(td, true);
                    }
                    return;


                }
                turnTimer = 0;
                return;
            }

            //if we are clicking on a targetable, then see if we have a ranged weapon available to fire with
            Actor tAct = MapMasterScript.GetTargetableAtLocation(targetingPosition);
            bool checkLOS = MapMasterScript.CheckTileToTileLOS(heroPCActor.GetPos(), targetingPosition, heroPCActor, MapMasterScript.activeMap);
            bool visible = MapMasterScript.activeMap.InBounds(targetingPosition) && heroPCActor.CheckIfTileIsTrulyVisible(targetingPosition, viewerIsHero: true);
            if (tAct != null &&
                tAct.actorfaction != Faction.PLAYER &&
                (checkLOS || visible) &&
                distance > 1)
            {
                //if we have a ranged weapon that can reach, or can switch to one,
                if (distance <= heroPCActor.GetMaxAttackRange()
                    || (GameMasterScript.SwitchToFirstWeaponOfCondition(w => w.isRanged
                    && heroPCActor.GetMaxAttackRange(w) >= distance)) != null)
                {
                    //pew pew
                    TurnData nTD = new TurnData();
                    nTD.actorThatInitiatedTurn = heroPCActor;
                    nTD.SetTurnType(TurnTypes.ATTACK);
                    if (heroPCActor.myStats.CheckParalyzeChance() == 1.0f)
                    {
                        GameLogScript.DelayedParalyzeMessage(StringManager.GetString("player_paralyzed"), heroPCActor);
                        UIManagerScript.DisplayPlayerError(heroPCActor);
                        return;
                    }
                    nTD.SetSingleTargetActor(MapMasterScript.GetTargetableAtLocation(targetingPosition));
                    nTD.SetSingleTargetPosition(targetingPosition);
                    gms.TryNextTurn(nTD, true);
                    return;
                }

                //otherwise if we clicked on a monster out of melee range without a ranged weapon that can reach, just chill
                else if (TDTouchControls.GetMouseButton(0))
                {
                    return;
                }
                //Debug.Log(checkLOS + " " + visible + " " + targetingPosition + " " + TDTouchControls.GetMouseButton(0));
            }

            bool anyWindowOpen = UIManagerScript.AnyInteractableWindowOpen();

            if (uims.IsMouseInGameWorld() && !anyWindowOpen)
            {
                mouseMovement = true;
                directionalInput = true;
            }
        }

        //Debug.Log(uims.IsMouseOverUI() + " " + mouseMovement + " " + uims.CheckTargeting());

        if (TDTouchControls.GetMouseButtonDown(mouseButtonForPathfind) && !uims.CheckTargeting())
        {
            if (uims.IsMouseInGameWorld() && !UIManagerScript.AnyInteractableWindowOpen())
            {
                mouseMovement = true;
                directionalInput = true;
            }
        }

        if (TDTouchControls.GetMouseButtonUp(mouseButtonForPathfind)) // || (TDTouchControls.GetMouseButtonUp(mouseButtonForStraightMove)))
        {
            ClearMousePathfinding();
        }

        if (HandleMouseInputForPathfinding()) return;

        if (HandleMouseInputPostPathfinding()) return;
        
        if (player.GetButtonDown("Pick Up Item"))
        {
            // Gets an item.
            if (TileInteractions.TryPickupItemsInHeroTile())
            {
                return;
            }            
        }

        CheckForKeyboardSpecificActionShortcuts(allowOnlyMovement);

        if (!uims.CheckTargeting())
        {
            //if (debugInput) Debug.Log("We are not targeting. Allow only movement? " + allowOnlyMovement + " Confirm pressed? " + bConfirmPressedSoDontEatInput);

            if (!allowOnlyMovement)
            {			
                if (IsRingMenuButtonDown())
                {
                    //open the radial menu
                    UIManagerScript.radialMenu.OpenMenu();
                    return;
                }
            }

            if (player.GetButtonDown("Use Stairs") || bConfirmPressedSoDontEatInput)
            {
                //if (Debug.isDebugBuild) Debug.Log("Pressed Use Stairs or confirm, d input is " + dInput);

                if (dInput == Directions.NEUTRAL ||
                    PlayerOptions.joystickControlStyle != JoystickControlStyles.STEP_MOVE)
                {
                    if (TravelManager.TryTravelStairs())
                    {
                        return;
                    }
                }
            }
            // Can take normal actions
            Vector2 attemptMovePosition = heroPCActor.GetPos();
            Vector2 addPos = new Vector2(0, 0);

            //bool keyPressed = false;
            Directions inDir = GetDirectionalInput();

            if (inDir == Directions.WEST)
            {
                addPos.x -= 1;
            }

            if (inDir == Directions.EAST)
            {
                addPos.x += 1;
            }

            if (inDir == Directions.SOUTH)
            {
                addPos.y -= 1;
            }

            if (inDir == Directions.NORTH)
            {
                addPos.y += 1;
            }

            // Special case for diagonals.

            if (inDir == Directions.NORTHEAST)
            {
                addPos.x += 1;
                addPos.y += 1;
            }
            if (inDir == Directions.NORTHWEST)
            {
                addPos.x -= 1;
                addPos.y += 1;
            }
            if (inDir == Directions.SOUTHWEST)
            {
                addPos.x -= 1;
                addPos.y -= 1;
            }
            if (inDir == Directions.SOUTHEAST)
            {
                addPos.x += 1;
                addPos.y -= 1;
            }

            //Here is where we check to see if the joystick was the last controller used.
            // But wait, on PC, we don't want to override confirm step move when mouse is touched.
            if ((lastActiveControllerType == ControllerType.Joystick || PlatformVariables.GAMEPAD_ONLY)
                && PlayerOptions.joystickControlStyle == JoystickControlStyles.STEP_MOVE)
            {
                //if this function is false, we should not move.
                if (!HandleJoystickIndividualStepMovement(ref addPos, bConfirmPressedSoDontEatInput, debugInput))
                {
                    return;
                }

                //otherwise, we should continue using that vector for motion
            }

            addPos.x = Mathf.Clamp(addPos.x, -1, 1);
            addPos.y = Mathf.Clamp(addPos.y, -1, 1);

            attemptMovePosition += addPos;


            if (attemptMovePosition != heroPCActor.GetPos() && attemptMovePosition != Vector2.zero)
            {
                // Moved!
                TurnData td = new TurnData();
                td.actorThatInitiatedTurn = heroPCActor;

                if (GameMasterScript.playerMovingAnimation)
                {
                    return;
                }

                MapTileData checkMTD = MapMasterScript.GetTile(attemptMovePosition);
                if (checkMTD == null)
                {
                    return;
                }
                if (checkMTD.tileType == TileTypes.WALL)
                {
                    // New - don't do anything if trying to move into wall.
                    return;
                }
                td.SetTurnType(TurnTypes.MOVE);
                timeSinceLastActionInput = Time.time;
                td.newPosition = attemptMovePosition;
                gms.TryNextTurn(td, true);
                return;
            }

            if (!allowOnlyMovement && player.GetButtonDown("Fire Ranged Weapon") && !uims.CheckTargeting())
            {
                // Try firing a ranged weapon.
                Weapon weap = heroPCActor.myEquipment.GetWeapon();

                //When we press Fire Ranged Weapon, we look for a ranged weapon and switch to it if we aren't already using one
                //If we don't have one, throw a fit
                if (weap.range <= 1)
                {
                    weap = GameMasterScript.SwitchToFirstWeaponOfCondition(f => f.range > 1) ?? weap;
                }

                if (weap.range > 1)
                {
                    int rangeToUse = heroPCActor.GetMaxAttackRange();
                    heroPCActor.SetActorData("fireranged", 1);
                    MetaProgressScript.SetMetaProgress("rangedtactics", 1);
                    GameMasterScript.rangedWeaponAbilityDummy.range = rangeToUse;
                    GameMasterScript.rangedWeaponAbilityDummy.targetRange = rangeToUse;
                    GameMasterScript.gmsSingleton.SetAbilityToTry(GameMasterScript.rangedWeaponAbilityDummy);
                    abilityToTry = GameMasterScript.rangedWeaponAbilityDummy;
                    uims.EnterTargeting(abilityToTry, Directions.NEUTRAL);
                    targetClicksMax = 1;
                    targetClicksRemaining = 1;
                }
                else
                {
                    UIManagerScript.PlayCursorSound("Error");
                    GameLogScript.LogWriteStringRef("log_error_no_rangedweapon");

                }
            }

            if (!PlatformVariables.GAMEPAD_ONLY && player.GetButtonDown("Wait Turn"))
            {
                if (Time.time - timeSinceLastActionInput >= (gms.playerMoveSpeed * 1.25f))
                {
                    TurnData td = new TurnData();
                    td.actorThatInitiatedTurn = heroPCActor;
                    //Debug.Log(turnNumber + " wait from manual command");
                    td.SetTurnType(TurnTypes.PASS);
                    gms.TryNextTurn(td, true);
                    timeSinceLastActionInput = Time.time;

                    heroPCActor.myMovable.Jab(Directions.NORTH);

                }
                return;
            }

#if !UNITY_IPHONE && !UNITY_ANDROID
            // Player input related to UI.
            if (!allowOnlyMovement)
            {
                if (player.GetButtonDown("Cycle Weapons Right"))
                {
                    UIManagerScript.CycleWeapons(1);
                    return;
                }
                if (player.GetButtonDown("Cycle Weapons Left"))
                {
                    UIManagerScript.CycleWeapons(-1);
                    return;
                }
            }
#endif

            if (!PlatformVariables.GAMEPAD_ONLY && !allowOnlyMovement)
            {
                if (player.GetButtonDown("Switch to Weapon 1"))
                {
                    UIManagerScript.SwitchActiveWeaponSlot(0, false);
                    return;
                }
                if (player.GetButtonDown("Switch to Weapon 2"))
                {
                    UIManagerScript.SwitchActiveWeaponSlot(1, false);
                    return;
                }
                if (player.GetButtonDown("Switch to Weapon 3"))
                {
                    UIManagerScript.SwitchActiveWeaponSlot(2, false);
                    return;
                }
                if (player.GetButtonDown("Switch to Weapon 4"))
                {
                    UIManagerScript.SwitchActiveWeaponSlot(3, false);
                    return;
                }
            }

            if (!PlatformVariables.GAMEPAD_ONLY && !allowOnlyMovement)
            {
                string builder = "Use Hotbar Slot ";

                for (int i = 0; i < UIManagerScript.GetHotbarAbilities().Length; i++)
                {
                    HotbarBindable hb = UIManagerScript.GetHotbarAbilities()[i];
                    if (hb.actionType != HotbarBindableActions.NOTHING)
                    {
                        int disp = i + 1;
                        bool buttonPressed = false;

                        if (UIManagerScript.GetIndexOfActiveHotbar() == 0) // We're using first hotbar
                        {
                            if (i >= 8) continue;
                            if (player.GetButtonDown(builder + disp))
                            {
                                buttonPressed = true;
                            }
                        }

                        if (UIManagerScript.GetIndexOfActiveHotbar() == 1) // We're using first hotbar
                        {
                            if (i < 8) continue;
                            if (player.GetButtonDown(builder + (i - 7)))
                            {
                                buttonPressed = true;
                            }
                        }

                        if (buttonPressed)
                        {

                            switch (hb.actionType)
                            {
                                case HotbarBindableActions.ABILITY:
                                    gms.CheckAndTryAbility(hb.ability);
                                    break;
                                case HotbarBindableActions.CONSUMABLE:
                                    gms.PlayerUseConsumable(hb.consume);
                                    break;
                            }
                            return;
                        }
                    }
                }
            }
        }

        //Nothing happened this time, but we should make sure to record that
        //we called the function and got to the end.
        //Keep track of the realest time.
        fLastTimeUpdateInputFinished = Time.realtimeSinceStartup;
    }
    
    const int DISCRETE_MOVE_FRAME_BUFFER = 3;
    static bool discreteBufferInitialized;
    static int[] discreteXMoves;
    static int[] discreteYMoves;

    static Directions CheckForDiscreteDirectionalInput(bool diagonalMoveOnly)
    {
        if (!discreteBufferInitialized)
        {
            discreteXMoves = new int[DISCRETE_MOVE_FRAME_BUFFER];
            discreteYMoves = new int[DISCRETE_MOVE_FRAME_BUFFER];
            discreteBufferInitialized = true;
        }

        Directions directionCapturedThisFrame = Directions.COUNT;

        if (player.GetButton("Move Up+Left"))
        {
            directionCapturedThisFrame = Directions.NORTHWEST;
        }
        if (player.GetButton("Move Down+Left"))
        {
            directionCapturedThisFrame = Directions.SOUTHWEST;
        }
        if (player.GetButton("Move Up+Right"))
        {
            directionCapturedThisFrame = Directions.NORTHEAST;
        }
        if (player.GetButton("Move Down+Right"))
        {
            directionCapturedThisFrame = Directions.SOUTHEAST;
        }

        if (directionCapturedThisFrame != Directions.COUNT) return directionCapturedThisFrame;
                
        bool moveLeft = player.GetButton("Move Left");
        bool moveRight = player.GetButton("Move Right");
        bool moveUp = player.GetButton("Move Up");
        bool moveDown = player.GetButton("Move Down");

        int xMove = 0;
        int yMove = 0;

        if (moveLeft) xMove = -1;
        else if (moveRight) xMove = 1;
        if (moveUp) yMove = 1;
        else if (moveDown) yMove = -1;

        if (!diagonalMoveOnly)
        {
            // We're holding at least one direction
            if (xMove != 0 || yMove != 0)
            {
#if UNITY_EDITOR
                //Debug.Log("Inputs this frame: " + xMove + "," + yMove);
#endif
                // This only matters in regular gameplay, not menus.
                if (!UIManagerScript.AnyInteractableWindowOpen() && !UIManagerScript.singletonUIMS.CheckTargeting())
                {
                    // But if this does NOT match what we were holding last frame, let's escape out
                    // This way we have a frame buffer to try and capture diagonals (sloppy inputs)
                    if (!CheckMoveAgainstFrameBuffer(xMove, yMove))
                    {
                        UpdateFrameBuffer(xMove, yMove);
#if UNITY_EDITOR
                        //Debug.Log("Not processing movement this frame.");
#endif
                        return Directions.COUNT;
                    }
                }

            }

            if (xMove == 1 && yMove == 0) directionCapturedThisFrame = Directions.EAST;
            else if (xMove == -1 && yMove == 0) directionCapturedThisFrame = Directions.WEST;
            else if (yMove == 1 && xMove == 0) directionCapturedThisFrame = Directions.NORTH;
            else if (yMove == -1 && xMove == 0) directionCapturedThisFrame = Directions.SOUTH;
        }
        if (xMove == 1 && yMove == 1) directionCapturedThisFrame = Directions.NORTHEAST;
        else if (xMove == 1 && yMove == -1) directionCapturedThisFrame = Directions.SOUTHEAST;
        else if (xMove == -1 && yMove == 1) directionCapturedThisFrame = Directions.NORTHWEST;
        else if (xMove == -1 && yMove == -1) directionCapturedThisFrame = Directions.SOUTHWEST;

#if UNITY_EDITOR
        if (directionCapturedThisFrame != Directions.COUNT)
        {
            //Debug.Log("Success with " + xMove + "," + yMove + ": " + directionCapturedThisFrame);
        }        
#endif

        UpdateFrameBuffer(xMove, yMove);

        return directionCapturedThisFrame;
    }

    /// <summary>
    /// Returns FALSE if this move is different from anything in our buffer. TRUE if it's valid.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    static bool CheckMoveAgainstFrameBuffer(int x, int y)
    {
        for (int i = 0; i < DISCRETE_MOVE_FRAME_BUFFER; i++)
        {
#if UNITY_EDITOR
            //Debug.Log("Compare against previous frame " + i + ": " + discreteXMoves[i] + "," + discreteYMoves[i]);
#endif
            if (x != discreteXMoves[i]) return false;
            if (y != discreteYMoves[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Adds move x,y from this frame into the buffer and moves other stuff out.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    static void UpdateFrameBuffer(int x, int y)
    {
        for (int i = DISCRETE_MOVE_FRAME_BUFFER-1; i > 0; i--)
        {
            discreteXMoves[i] = discreteXMoves[i - 1];
            discreteYMoves[i] = discreteYMoves[i - 1];
        }

        // Latest move in 0 index
        discreteXMoves[0] = x;
        discreteYMoves[0] = y;
    }

    static bool CheckForGameoverInput(Directions dInput, bool bConfirmPressedSoDontEatInput)
    {
        if (GameMasterScript.playerDied && UIManagerScript.dialogBoxType == DialogType.GAMEOVER)
        {
            if (dInput != Directions.NEUTRAL)
            {
                if (dInput == Directions.SOUTH)
                {
                    UIManagerScript.MoveCursor(Directions.SOUTH);
                }
                if (dInput == Directions.NORTH)
                {
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
                return true;
            }
            if (bConfirmPressedSoDontEatInput)
            {
                UIManagerScript.DialogCursorConfirm();
            }
            return true;
        }
        return false;
    }

    static bool CheckForConditionsThatHaltInput()
    {
        if (!PlatformVariables.GAMEPAD_ONLY && timeSinceControlMapperWasOpen <= 0.5f) return true;

        if (TDInputHandler.disableAllInput)
        {
            return true;
        }

        if (GameMasterScript.IsNextTurnPausedByAnimations())
        {
            return true; // New conditional 11/1/2017
        }

        if (GameMasterScript.cameraScript.customAnimationPlaying)
        {
            return true;
        }

        if (!gms.tdHasFocus)
        {
            return true;
        }
        else if (ignoreThisMouseAction && (TDTouchControls.GetMouseButton(0) || TDTouchControls.GetMouseButton(1)))
        {
            ignoreThisMouseAction = false;
            return true;
        }

        return false;
    }

    static bool HandleInteractableWindowInput(Directions dInput, bool allowOnlyMovement, bool bConfirmPressedSoDontEatInput)
    {
        // Add code that passes input directly to fullscreen UI if we have one open
        // Check to see if any of the other full screen UIs are being toggled.
        if (UIManagerScript.HandleInput_CurrentFullScreenUI(dInput))
        {
            return true;
        }

#if !UNITY_SWITCH
        //Shep: this is a hack, but the clock's ticking.
        //if the optionsUI is open and the last input was mouse based, 
        //don't draw the cursor.
        //Problems this may cause include stomping bForceHideCursor if it set true elsewhere.
        UIManagerScript.bForceHideCursor = UIManagerScript.GetWindowState(UITabs.OPTIONS) &&
                                           ReInput.controllers.GetLastActiveControllerType() == ControllerType.Mouse;
#endif

        if (UIManagerScript.AnyInteractableWindowOpen())
        {
            if (player.GetAxis("Scroll UI Boxes Vertical") != 0f)
            {
                float modified = player.GetAxis("Scroll UI Boxes Vertical") * CONTROLLER_STICKSCROLL_SPEEDMOD * Time.deltaTime;
                UIManagerScript.TryScrollUITextBox(modified);
                //return true;
            }

            if (ItemWorldUIScript.itemWorldInterfaceOpen)
            {
                if (PlatformVariables.SHOW_SEARCHBARS && player.GetButtonDown("Jump to Searchbar"))
                {
                    UIManagerScript.singletonUIMS.itemWorldSearchbar.Select();
                    UIManagerScript.singletonUIMS.itemWorldSearchbar.ActivateInputField();
                    return true;
                }

                if (IsCompareAlternateButtonDown())
                {
                    ItemWorldUIScript.ToggleAlternateInfo(true);
                }
                else if (IsCompareAlternateButtonUp())
                {
                    ItemWorldUIScript.ToggleAlternateInfo(false);
                }

            }

            if (ShopUIScript.CheckShopInterfaceState())
            {
                if (PlatformVariables.SHOW_SEARCHBARS)
                {
                    if (player.GetButtonDown("Jump to Searchbar"))
                    {
                        UIManagerScript.singletonUIMS.shopSearchbar.Select();
                        UIManagerScript.singletonUIMS.shopSearchbar.ActivateInputField();
                        return true;
                    }
                }
                // This will become deprecated once Shop switches over to the new UI system.
                if (player.GetButton("Diagonal Move Only") && ReInput.controllers.GetLastActiveControllerType() == ControllerType.Joystick)
                {
                    if (dInput == Directions.NORTH)
                    {
                        UIManagerScript.ScrollPages(false);
                        return true;
                    }
                    else if (dInput == Directions.SOUTH)
                    {
                        UIManagerScript.ScrollPages(true);
                        return true;
                    }
                }
                if (IsCompareAlternateButtonDown() || IsCompareAlternateButtonUp())
                {
                    UIManagerScript.singletonUIMS.ShowItemInfoShop(UIManagerScript.singletonUIMS.GetIndexOfSelectedButton(), ShopUIScript.playerItemList);
                }
            }

            if (player.GetButtonDown("List Page Down"))
            {
                UIManagerScript.ScrollPages(true);
                return true;
            }
            else if (player.GetButtonDown("List Page Up"))
            {
                UIManagerScript.ScrollPages(false);
                return true;
            }

            if (!TDScrollbarManager.mouseIsInSpecialScrollArea)
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0) // Forward
                {
                    UIManagerScript.MouseScroll(-1);
                    return true;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0) // Back
                {
                    UIManagerScript.MouseScroll(1);
                    return true;
                }
            }


            if (player.GetButtonDown("UI Page Right"))
            {
                UIManagerScript.CycleUITabs(Directions.EAST);
                return true;
            }
            else if (player.GetButtonDown("UI Page Left"))
            {
                UIManagerScript.CycleUITabs(Directions.WEST);
                return true;
            }

            if (dInput != Directions.NEUTRAL && !ignoreDirectionalInputUntilNextNetural)
            {
                if (dInput == Directions.SOUTH)
                {
                    UIManagerScript.MoveCursor(Directions.SOUTH);
                }
                if (dInput == Directions.NORTH)
                {
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
                return true;
            }
            else if (dInput == Directions.NEUTRAL)
            {
                ignoreDirectionalInputUntilNextNetural = false;
            }
            if (bConfirmPressedSoDontEatInput)
            {
                uims.CursorConfirm();
                return true;
            }
        }

        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            if (UIManagerScript.GetWindowState(UITabs.OPTIONS) && player.GetButtonDown("Options Menu"))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.ForceCloseFullScreenUI();
                return true;
            }
        }

        if (CharCreation.creationActive)
        {
            if (player.GetButtonDown("Cancel"))
            {
                CharCreation.CancelPressed();
                return true;
            }
        }

        if (UIManagerScript.CheckDialogBoxState() && !UIManagerScript.myDialogBoxComponent.IsDelayed())
        {
            if (PlatformVariables.SHOW_TEXT_INPUT_BOXES && UIManagerScript.CheckForNameInputOpen())
            {
                return false;
            }
            if ((player.GetButtonDown("Options Menu") || (player.GetButtonDown("Cancel"))) && GameMasterScript.actualGameStarted && !GameMasterScript.playerDied)
            {
                if (UIManagerScript.dialogBoxType != DialogType.LEVELUP && !UIManagerScript.IsCurrentConversationKeyStory())
                {
                    if (UIManagerScript.currentConversation != null)
                    {
                        bool anyExit = false;
                        foreach (ButtonCombo bc in UIManagerScript.currentTextBranch.responses)
                        {
                            if (bc.actionRef == "exit")
                            {
                                anyExit = true;
                                break;
                            }
                        }

                        if (!anyExit)
                        {
                            //Debug.Log("No exit tho");
                            return true;
                        }
                        UIManagerScript.PlayCursorSound("Cancel");
                        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                        return true;
                    }
                    else
                    {
                        UIManagerScript.PlayCursorSound("Cancel");
                        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                    }
                    return true;
                }
                else if (UIManagerScript.IsCurrentConversationKeyStory()) // Pressing "Cancel" in key story should be the same as "Confirm"
                {
                    uims.CursorConfirm();
                    return true;
                }
            }

            // Separate dialog box handling
            return true;
        }

        if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                ItemWorldUIScript.CancelPressed();
                return true;
            }
        }

        if (MonsterCorralScript.corralFoodInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                MonsterCorralScript.CancelPressed();
                return true;
            }
        }

        if (MonsterCorralScript.corralGroomingInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                MonsterCorralScript.singleton.CloseCorralGroomingInterface(0);
                return true;
            }
        }

        if (MonsterCorralScript.corralInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                MonsterCorralScript.CloseCorralInterface();
                MonsterCorralScript.singleton.CloseCorralGroomingInterface(0);
                return true;
            }
        }

        if (CorralBreedScript.corralBreedInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                CorralBreedScript.CancelPressed();
                return true;
            }
        }

        if (MonsterCorralScript.monsterStatsInterfaceOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                MonsterCorralScript.singleton.CloseMonsterStatsInterface(0);
                return true;
            }
        }

        if (UIManagerScript.GetWindowState(UITabs.COOKING))
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.CloseCookingInterface();
                return true;
            }
            bool allEmpty = true;
            for (int i = 0; i < UIManagerScript.cookingIngredientItems.Length; i++)
            {
                if (UIManagerScript.cookingIngredientItems[i] != null)
                {
                    allEmpty = false;
                    break;
                }
            }
            if (UIManagerScript.cookingSeasoningItem != null)
            {
                allEmpty = false;
            }
            if (player.GetButtonDown("Cancel"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                if (allEmpty)
                {
                    UIManagerScript.CloseCookingInterface();
                    return true;
                }
                else
                {
                    CookingScript.CancelPressed();
                    return true;
                }
            }
        }

        if (UIManagerScript.CheckSkillSheetState())
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("View Skills") || player.GetButtonDown("Cancel")
                || player.GetButtonDown("Toggle Menu Select"))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                return true;
            }
        }

        if (UIManagerScript.casinoGameOpen)
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Cancel") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                UIManagerScript.CloseBlackjackGame();
                UIManagerScript.CloseSlotsGame();
                return true;
            }
        }

        if (player.GetButtonDown("View Character Info") && !allowOnlyMovement)
        {
            if (UIManagerScript.GetWindowState(UITabs.CHARACTER))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
            }
            else
            {
                UIManagerScript.OpenFullScreenUI(UITabs.CHARACTER);
            }
            return true;
        }

        if (player.GetButtonDown("Options Menu") && !UIManagerScript.GetWindowState(UITabs.OPTIONS) && !allowOnlyMovement)
        {
            if (UIManagerScript.GetWindowState(UITabs.SHOP))
            {
                ShopUIScript.CloseShopInterface();
                return true;
            }
            if (UIManagerScript.GetWindowState(UITabs.CHARACTER))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                return true;
            }
            if (UIManagerScript.GetWindowState(UITabs.INVENTORY))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseInventorySheet();
                return true;
            }
            if (UIManagerScript.GetWindowState(UITabs.RUMORS))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                return true;
            }
            if (CharCreation.creationActive)
            {
                CharCreation.CancelPressed();
                return true;
            }

            
            UIManagerScript.OpenFullScreenUI(UITabs.OPTIONS);
            return true;
        }

        if (UIManagerScript.GetWindowState(UITabs.CHARACTER) && (player.GetButtonDown("Cancel") || player.GetButtonDown("Options Menu")))
        {
            UIManagerScript.TryCloseFullScreenUI();

            return true;
        }

        if (player.GetButtonDown("View Rumors") && !allowOnlyMovement)
        {
            if (UIManagerScript.GetWindowState(UITabs.RUMORS))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
            }
            else
            {
                UIManagerScript.OpenFullScreenUI(UITabs.RUMORS);
            }

            return true;
        }

        if (UIManagerScript.GetWindowState(UITabs.RUMORS) && player.GetButtonDown("Cancel"))
        {
            if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
            return true;
        }

        //allow BACK to close the submenu instead of just jumping out the airlock
        if ((UIManagerScript.CheckInventorySheetState()) && ((player.GetButtonDown("Options Menu")) || (player.GetButtonDown("Cancel"))))
        {
            if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseInventorySheet();
            return true;
        }

        if (ShopUIScript.CheckShopInterfaceState() && (player.GetButtonDown("Cancel") || player.GetButtonDown("Options Menu") || 
            player.GetButtonDown("Toggle Menu Select")))
        {
            UIManagerScript.PlayCursorSound("Cancel");
            ShopUIScript.CloseShopInterface();
            return true;
        }

        if (UIManagerScript.GetWindowState(UITabs.COOKING))
        {
            if (player.GetButtonDown("Options Menu") || player.GetButtonDown("Toggle Menu Select"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                UIManagerScript.CloseCookingInterface();
                return true;
            }
        }
        if (UIManagerScript.GetWindowState(UITabs.RUMORS))
        {
            if (player.GetButtonDown("View Rumors") || player.GetButtonDown("Options Menu"))
            {
                if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                return true;
            }
        }

        if (player.GetButtonDown("View Help") && !allowOnlyMovement)
        {
            if (UIManagerScript.CheckDialogBoxState())
            {
                if (UIManagerScript.dialogBoxType != DialogType.LEVELUP && !UIManagerScript.IsCurrentConversationKeyStory())
                {
                    UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                    return true;
                }
            }
        }

        if (UIManagerScript.CheckDialogBoxState() && UIManagerScript.dialogBoxType != DialogType.LEVELUP && player.GetButtonDown("Cancel"))
        {
            if (PlatformVariables.SHOW_TEXT_INPUT_BOXES)
            {
                if (UIManagerScript.CheckForNameInputOpen())
                {
                    return false;
                }
            }

            if (!UIManagerScript.IsCurrentConversationKeyStory())
            {
                UIManagerScript.PlayCursorSound("Cancel");
                UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
                return true;
            }
        }

        return false;
    }

    static bool HandleExamineModeInput(bool bConfirmPressedSoDontEatInput)
    {
        if (UIManagerScript.examineMode)
        {
            if (player.GetButtonDown("Cancel"))
            {
            UIManagerScript.PlayCursorSound("Cancel");
                uims.CloseExamineMode();
                return true;
            }

            // Pet conversation
            if (bConfirmPressedSoDontEatInput)
            {
                Vector2 clickedPosition = uims.GetVirtualCursorPosition();
                MapTileData mtd = MapMasterScript.GetTile(clickedPosition);
                return gms.CheckTileForFriendlyConversation(mtd, rightClick: false);
            }
        }
        
        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            if (player.GetButtonDown("Mark As Hostile"))
            {
                Vector3 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                uims.SetVirtualCursorPosition(new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f)));
                MapTileData mtd = MapMasterScript.GetTile(uims.GetVirtualCursorPosition());
                return gms.CheckTileForFriendlyConversation(mtd, rightClick: true);
            }
        }
        else
        {            
            return HandleExamineModeInput_GamepadStyle();
        }

        return false;
    }
	
	 public static void ClearMousePathfinding()
    {
        if (!initialized) return;
        mouseMovement = false;
        directionalInput = false;
        pathfindingToMousePosition = false;
        movingOnLineToMousePosition = false;
        capturePFTile = null;
        if (pathfindingHighlight.activeSelf)
        {
            pathfindingHighlight.SetActive(false);
        }
    }

    public static bool PhysicalMouseTouched(float amount)
    {
        Vector2 mp = Vector2.zero;
        mp.x = Input.mousePosition.x;
        mp.y = Input.mousePosition.y;
        if (Vector2.Distance(mp, uims.lastPhysicalMousePosition) > amount)
        {
            return true;
        }
        return false;
    }

    public static bool HandleAbilityTargetingInput()
    {
        if (uims.CheckTargeting())
        {
            if (player.GetButtonDown("Cancel"))
            {
                UIManagerScript.PlayCursorSound("Cancel");
                uims.ExitTargeting();

                GameMasterScript.heroPCActor.TrySwitchToPreviousUsedWeapon();

                GameMasterScript.gmsSingleton.SetItemBeingUsed(null);
                return true;
            }
        }

        return false;
    }

    public static bool HandleHotbarInput(Directions dInput, bool bConfirmPressedSoDontEatInput)
    {
        if (UIManagerScript.AnyInteractableWindowOpen() && !uims.CheckHotbarNavigating()) // || uims.currentFullScreenUI != null)
        {
            return false;
        }
        if (player.GetButtonDown("Jump to Hotbar") && MapMasterScript.activeMap.floor != MapMasterScript.SHARA_START_FOREST_FLOOR)
        {
            uims.ToggleHotbarNavigating();
            if (uims.CheckHotbarNavigating())
            {
                UIManagerScript.uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Move");
            }
            else
            {
                UIManagerScript.uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Cancel");
            }
            return true;
        }
        if (uims.CheckHotbarNavigating())
        {

            // This code is for joycon flippy check. not relevant elsewhere
#if UNITY_SWITCH
            if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE && GameMasterScript.gmsSingleton.Input_CheckMotionChangeHotbar())
            {
                UIManagerScript.singletonUIMS.CycleHotbars(1);
                return true;
            }
#endif

            // Move cursor on hotbar.
            if (dInput != Directions.NEUTRAL)
            {
                Directions id = GetDirectionalInput();

                if (id == Directions.WEST)
                {
                    UIManagerScript.MoveCursor(Directions.WEST);
                    //uims.NudgeHotbarNavigating(-1);
                }
                if (id == Directions.EAST)
                {
                    UIManagerScript.MoveCursor(Directions.EAST);
                    //uims.NudgeHotbarNavigating(1);
                }
                if (id == Directions.NORTH)
                {
                    UIManagerScript.MoveCursor(Directions.NORTH);
                    //uims.NudgeHotbarNavigating(-1);
                }
                if (id == Directions.SOUTH)
                {
                    UIManagerScript.MoveCursor(Directions.SOUTH);
                    //uims.NudgeHotbarNavigating(1);
                }
                return true;
            }
            if (player.GetButtonDown("Cancel"))
            {
                uims.ToggleHotbarNavigating();
                UIManagerScript.uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Cancel");
            }
            if (bConfirmPressedSoDontEatInput)
            {
                UIManagerScript.HotbarConfirm();
                uims.CloseHotbarNavigating();
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks joystick input to determine step direction, which also changes our facing.
    /// We can also not move the stick and press Confirm to attack a nearby monster in melee.
    /// </summary>
    /// <param name="vControllerInput">The input from the stick. Modified to face a nearby monster if the input starts as (0,0) 
    /// and we pressed the attack button</param>
    /// <returns>False if we should stop handling input</returns>
    static bool HandleJoystickIndividualStepMovement(ref Vector2 vControllerInput, bool bConfirmPressedSoDontEatInput, bool debugInput)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        //Do not do any of this if we're in examine mode. Sit on the input.
        if (UIManagerScript.singletonUIMS.GetExamineMode())
        {
            return false;
        }

        Rewired.Player inputPlayer = player;
        bool bConfirmPressed = bConfirmPressedSoDontEatInput;

        //if (debugInput) Debug.Log("Checking handle joystick step movement. " + bConfirmPressed + " , vector dir is " + vControllerInput);

        //#shirenattack is always on, so check that first.
        //if we pressed confirm with no motion, we could attack the monster we are facing.
        if (bConfirmPressed && vControllerInput == Vector2.zero)
        {
            //confirm pressed, no direction set, so hide the diagonal thingy
            GameMasterScript.heroPCActor.diagonalOverlay.SetActive(false);

            Directions shirenDir = GetDirectionForAutoSlashAtNearbyEnemy();
            if (shirenDir != Directions.NEUTRAL)
            {
                //face the hero in that direction
                GameMasterScript.heroPCActor.UpdateLastMovedDirection(shirenDir);

                //pretend like we pushed the stick in that direction too.
                vControllerInput = MapMasterScript.xDirections[(int)shirenDir];

                //"move" in that direction.
                return true;
            }

            //we pressed confirm without pressing a direction, stop handling input.
            return false;
        }

        //ignore all of this business unless we're using the new fresh style
        //also ignore if we are using the dpad to get around
        if (PlayerOptions.joystickControlStyle != JoystickControlStyles.STEP_MOVE ||
            IsDPadPressed())
        {
            return true;
        }

        //return true if we want the game to move us. This happens if we have
        //just pressed the confirm button, or if we have been holding it down

        //draw the cursor at our desired movement location
        if (vControllerInput != Vector2.zero)
        {
            heroPCActor.diagonalOverlay.SetActive(true);
            heroPCActor.diagonalOverlay.transform.localPosition = new Vector3(vControllerInput.x, vControllerInput.y, 0);

            //face the direction we've pushed with the joystick

            float angle = CombatManagerScript.GetAngleBetweenPoints(Vector2.zero, vControllerInput);
            Directions directionFromAngle = MapMasterScript.GetDirectionFromAngle(angle);

            heroPCActor.UpdateLastMovedDirection(directionFromAngle);
            heroPCActor.myStats.UpdateStatusDirections();

            //Hide the targeting icon if we are running around
            bool bIsRunning = inputPlayer.GetButtonShortPress("Confirm") || inputPlayer.GetButtonDoublePressDown("Confirm");
            if (bIsRunning)
            {
                heroPCActor.diagonalOverlay.SetActive(false);
                return true;
            }

            //We're not running, and we are pointing at a tile. If there is a something in that tile,
            //we should hoverbar that thing.
            UpdateHoverInfoScriptBasedOnAnalogTargeting(vControllerInput);

            //if we have pushed or held the confirm button, return true so we keep processing the move.
            return bConfirmPressed;
        }
        else
        {
            AnalogStickHighlightingTile = false;
        }

        //Since there is no motion on the joystick, we can hide the icon
        heroPCActor.diagonalOverlay.SetActive(false);

        //we pointed in no direction, and handled no input, so keep processing for other input.
        return true;
    }

    static bool analogStickHighlightState;
    public static bool AnalogStickHighlightingTile
    {
        get
        {
            return analogStickHighlightState;
        }
        set
        {
            analogStickHighlightState = value;
        }
    }

    /// <summary>
    /// Determine what should show in the hoverbar based on the way our stick is pointing.
    /// </summary>
    /// <param name="analogTargetingDirection"></param>
    public static void UpdateHoverInfoScriptBasedOnAnalogTargeting(Vector2 analogTargetingDirection)
    {
        AnalogStickHighlightingTile = true;
        HoverInfoScript.lastCheckedMTDFromAnalogTargeting = MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos() + analogTargetingDirection);
        HoverInfoScript.UpdateHoverTextFromTile(HoverInfoScript.lastCheckedMTDFromAnalogTargeting);
    }

    public static bool IsDPadPressed()
    {
        var rw = player;

        return rw.GetButton("DPadPressed");
    }

    static Directions GetDirectionForAutoSlashAtNearbyEnemy()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        //where were we facing before?
        Directions lastMoved = heroPCActor.lastMovedDirection;
        Directions dirTowardsMonster = Directions.NEUTRAL;
        Directions dirTowardsDestructible = Directions.NEUTRAL;

        Vector2 vHeroPos = heroPCActor.GetPos();

        //if there is a monster in that tile, hit it.
        if (lastMoved < Directions.RANDOMALL)
        {
            MapTileData mtdNeighbor = MapMasterScript.GetTile(vHeroPos + MapMasterScript.xDirections[(int)lastMoved]);
            if (mtdNeighbor != null)
            {
                //If there's a monster in the tile, and he ain't ours, swing.
                var mon = mtdNeighbor.GetMonster() as Monster;
                if (mon != null && mon.bufferedFaction != Faction.PLAYER)
                {
                    dirTowardsMonster = lastMoved;
                }

                //take note if there's a chest or jar here.
                if (mtdNeighbor.HasBreakableCollidable(heroPCActor))
                {
                    dirTowardsDestructible = lastMoved;
                }
            }
        }

        //if we didn't find one, look around us.
        if (dirTowardsMonster == Directions.NEUTRAL)
        {
            var listBaddies = MapMasterScript.GetFactionMonstersAroundTile(vHeroPos, Faction.ENEMY);
            int iCount = listBaddies.Count;
            if (iCount == 0)
            {
                //no monsters near by, we should look for breakables instead
                var adjacentDestructibles = MapMasterScript.GetUsableBreakableCollidablesForHeroAroundTile(vHeroPos);
                dirTowardsDestructible = GetDirectionOfAdjacentActorFromListThatIsClosestToAGivenDirection(lastMoved, vHeroPos, adjacentDestructibles);

            }
            //if there is only one baddie, face that baddie (to bloodshed) and then attack it.
            else if (iCount == 1)
            {
                var baddie = listBaddies[0];
                float angle = CombatManagerScript.GetAngleBetweenPoints(vHeroPos, baddie.GetPos());
                dirTowardsMonster = MapMasterScript.GetDirectionFromAngle(angle);
            }
            //if there's more than one, target the last one we hit
            else if (listBaddies.Contains(heroPCActor.lastActorAttacked))
            {
                float angle = CombatManagerScript.GetAngleBetweenPoints(vHeroPos, heroPCActor.lastActorAttacked.GetPos());
                dirTowardsMonster = MapMasterScript.GetDirectionFromAngle(angle);
            }
            //if there's more than one monster, and none of them are the last one we hit,
            //face the one that's closest to the direction we're facing now.
            else
            {
                dirTowardsMonster = GetDirectionOfAdjacentActorFromListThatIsClosestToAGivenDirection(lastMoved, vHeroPos, listBaddies);
            }

        }

        if (dirTowardsMonster == Directions.NEUTRAL)
        {
            return dirTowardsDestructible;
        }

        return dirTowardsMonster;
    }

    /// <summary>
    /// Takes a list of actors and gives us the direction pointing towards the one that is closest to our current facing direction.
    /// </summary>
    /// <param name="dirOrigin">The direction we are currently facing. Please be one of the eight normal regular directions.</param>
    /// <param name="actors">A list of people to point at.</param>
    /// <returns>The direction closest to ours that points at an actor.</returns>
    static Directions GetDirectionOfAdjacentActorFromListThatIsClosestToAGivenDirection(Directions dirOrigin, Vector2 vOriginPos, List<Actor> actors)
    {
        int iClosest = 999;
        Directions dirResult = Directions.NEUTRAL;
        if (actors == null || actors.Count == 0)
        {
            return dirResult;
        }

        foreach (var adjecentBaddie in actors)
        {
            float angle = CombatManagerScript.GetAngleBetweenPoints(vOriginPos, adjecentBaddie.GetPos());
            Directions checkDir = MapMasterScript.GetDirectionFromAngle(angle);

            int iDelta = Mathf.Abs(checkDir - dirOrigin);

            //if this iDelta > 4, then count the other direction -- 2 on the clock is not 9 away from 11 on the clock, but rather 3.
            //NW is not 6 away from NE, but rather 2.
            //8 is the number of directions on the earth compass, and so our cutoff is half of that.
            if (iDelta > 4)
            {
                iDelta -= 8;
            }

            if (iDelta < iClosest)
            {
                iClosest = iDelta;
                dirResult = checkDir;
            }
        }

        return dirResult;
    }

    public static void IgnoreNextMouseAction()
    {
        ignoreThisMouseAction = true;
    }

    public static void DisableInput()
    {
        disableAllInput = true;
    }

    public static void EnableInput()
    {
        disableAllInput = false;
    }

    public static bool CheckForExamineModeInput()
    {

        // Check targeting stuff
        if (uims.GetExamineMode())
        {
            if (player.GetButtonDown("Cancel"))
            {
                UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("Cancel");
                uims.ToggleExamine();
                return true;
            }

            Directions inDir = GetDirectionalInput();
            if (inDir != Directions.NEUTRAL)
            {
                if (turnTimer < gms.movementInputOptionsTime)
                {
                    return true;
                }
                turnTimer = 0;
            }

            if (inDir == Directions.NORTH)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(0, 1));
            }

            if (inDir == Directions.SOUTH)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(0, -1));
            }
            if (inDir == Directions.WEST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(-1, 0));
            }
            if (inDir == Directions.EAST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(1, 0));
            }
            if (inDir == Directions.NORTHEAST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(1, 1));
            }
            if (inDir == Directions.NORTHWEST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(-1, 1));
            }
            if (inDir == Directions.SOUTHWEST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(-1, -1));
            }
            if (inDir == Directions.SOUTHEAST)
            {
                uims.ChangeVirtualCursorPosition(new Vector2(1, -1));
            }
            return true;
        }

        return false;
    }

    public static bool CheckForTargetingInput(bool allowOnlyMovement, AbilityScript abilityToTry, bool bConfirmPressedSoDontEatInput)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if (uims.CheckTargeting()) // USING AN ABILITY!
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0)
                {
                    uims.TryRotateTargetingShape(true);
                }
                else
                {
                    uims.TryRotateTargetingShape(false);
                }
            }
            else if (player.GetAxisRaw("Rotate Targeting Shape") != 0 && ((player.GetNegativeButtonDown("Rotate Targeting Shape")) || player.GetButtonDown("Rotate Targeting Shape")))
            {
                if (player.GetAxis("Rotate Targeting Shape") < 0)
                {
                    uims.TryRotateTargetingShape(false);
                }
                else
                {
                    uims.TryRotateTargetingShape(true);
                }

                // Rotate line?
            }

            bool canRotate = false;
            if (abilityToTry != null)
            {
                canRotate = !abilityToTry.CheckAbilityTag(AbilityTags.FLOATING);
            }

            if (PhysicalMouseTouched(0.2f) && Cursor.visible)
            {
                //Vector3 centerPoint = new Vector2(Screen.width / 2f,Screen.height / 2f);
                Vector3 centerPoint = Camera.main.WorldToScreenPoint(GameMasterScript.heroPCActor.GetPos()); // ??? Will this work?
                float angle = CombatManagerScript.GetAngleBetweenPoints(centerPoint, Input.mousePosition);
                uims.lastPhysicalMousePosition = Input.mousePosition;
                Vector3 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 roundPos = new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f));
                uims.SetVirtualCursorPosition(roundPos);
                if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                {
                    if (canRotate)
                    {
                        Directions tDir = MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(GameMasterScript.heroPCActor.GetPos(), roundPos));
                        uims.TryRotateTargetingShape(tDir);
                    }
                }
            }

            Directions inDir = GetDirectionalInput();

            // Floating shapes should not rotate when the shape is moved.

            {
                if (inDir == Directions.NORTH)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(0, 1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.NORTH);
                    }

                }

                if (inDir == Directions.SOUTH)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(0, -1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.SOUTH);
                    }
                }
                if (inDir == Directions.WEST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(-1, 0));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.WEST);
                    }
                }
                if (inDir == Directions.EAST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(1, 0));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.EAST);
                    }
                }
                if (inDir == Directions.NORTHEAST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(1, 1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.NORTHEAST);
                    }
                }
                if (inDir == Directions.NORTHWEST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(-1, 1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.NORTHWEST);
                    }
                }
                if (inDir == Directions.SOUTHWEST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(-1, -1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.SOUTHWEST);
                    }
                }
                if (inDir == Directions.SOUTHEAST)
                {
                    uims.ChangeVirtualCursorPosition(new Vector2(1, -1));
                    if (!abilityToTry.CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        if (canRotate) uims.TryRotateTargetingShape(Directions.SOUTHEAST);
                    }
                }
            }

            bool confirmed = false;

            // Did we hit a hotbar binding?

            if (!allowOnlyMovement && !PlatformVariables.GAMEPAD_ONLY)
            {
                string builder = "Use Hotbar Slot ";
                for (int i = 0; i < UIManagerScript.GetHotbarAbilities().Length; i++)
                {
                    HotbarBindable hb = UIManagerScript.GetHotbarAbilities()[i];

                    int disp = i + 1;

                    bool buttonPressed = false;

                    if (UIManagerScript.GetIndexOfActiveHotbar() == 0) // We're using first hotbar
                    {
                        if (i >= 8) continue;
                        if (player.GetButtonDown(builder + disp))
                        {
                            buttonPressed = true;
                        }
                    }

                    if (UIManagerScript.GetIndexOfActiveHotbar() == 1) // We're using first hotbar
                    {
                        if (i < 8) continue;
                        if (player.GetButtonDown(builder + (i - 7)))
                        {
                            buttonPressed = true;
                        }
                    }

                    if (!buttonPressed) continue;

                    if (hb.actionType == HotbarBindableActions.ABILITY && heroPCActor.myAbilities.AreAbilitiesSameParent(hb.ability, abilityToTry))
                    {
                        confirmed = true;
                    }
                }
            }

            if (confirmed || (uims.IsMouseInGameWorld() && TDTouchControls.GetMouseButtonDown(0) || bConfirmPressedSoDontEatInput || player.GetButtonDown("Fire Ranged Weapon")))
            {
                if (Time.time - timeAtLastAbilSelection <= 0.5f) return true;
                // Try to use ability.
                Vector2 clickedPosition = uims.GetVirtualCursorPosition();
                if (abilityToTry.CheckAbilityTag(AbilityTags.TARGETED)) // This is a targeted ability
                {
                    List<Vector2> targetTiles = uims.GetAllValidTargetTiles();

                    if (targetTiles == null || targetTiles.Count == 0)
                    {
                        UIManagerScript.PlayCursorSound("Error");
                        return true;
                    }

                    if (GameMasterScript.GetAbilityToTry().CheckAbilityTag(AbilityTags.UNIQUETARGET))
                    {
                        if (UIManagerScript.IsTileUsedAlready(clickedPosition))
                        {
                            UIManagerScript.PlayCursorSound("Error");
                            return true;
                        }
                    }

                    Actor target = MapMasterScript.GetTargetableAtLocation(clickedPosition);

                    if (GameMasterScript.GetAbilityToTry() == GameMasterScript.rangedWeaponAbilityDummy)
                    {
                        if (target.actorfaction == Faction.PLAYER)
                        {
                            UIManagerScript.PlayCursorSound("Error");
                            return true;
                        }
                    }

                    TargetData ntd = new TargetData();

                    if (GameMasterScript.GetAbilityToTry().CheckAbilityTag(AbilityTags.CURSORTARGET) && target != null)
                    {
                        ntd.targetActors.Add(target); // Better way of addign actors?
                        if (GameMasterScript.GetAbilityToTry() == GameMasterScript.petAttackAbilityDummy)
                        {
                            UIManagerScript.singletonUIMS.ExitTargeting();
                            gms.SetTempGameData("hitfriendly", target.actorUniqueID);
                            DialogEventsScript.ConfirmHitCharmedEnemy("");
                            UIManagerScript.PlayCursorSound("Equip Item");
                            Actor myPet = gms.TryLinkActorFromDict(gms.ReadTempGameData("pet_behavior_convo"));
                            StringManager.SetTag(0, myPet.displayName);
                            StringManager.SetTag(2, target.displayName);
                            StringManager.SetTag(1, StringManager.GetString("misc_pet_attack_log"));
                            GameLogScript.LogWriteStringRef("log_pet_command");
                            return true;
                        }
                    }


                    if (GameMasterScript.GetAbilityToTry().CheckAbilityTag(AbilityTags.CURSORTARGET))
                    {
                        ntd.clickedPosition = clickedPosition;
                    }
                    else
                    {
                        ntd.clickedPosition = heroPCActor.GetPos();
                    }

                    // These values might be different for multi-target abilities that change their abil logic.
                    if (GameMasterScript.GetAbilityToTry() != UIManagerScript.singletonUIMS.abilityInTargeting && UIManagerScript.singletonUIMS.abilityInTargeting != null)
                    {
                        ntd.whichAbility = UIManagerScript.singletonUIMS.abilityInTargeting;
                    }
                    else
                    {
                        ntd.whichAbility = GameMasterScript.GetAbilityToTry();
                    }


                    foreach (Vector2 nt in targetTiles)
                    {
                        if (!ntd.targetTiles.Contains(nt))
                        {
                            ntd.targetTiles.Add(nt);
                            MapTileData chk = MapMasterScript.GetTile(nt);
                            Actor grab = chk.GetTargetable();
                            if (!ntd.targetActors.Contains(grab) && grab != null)
                            {
                                ntd.targetActors.Add(grab); // Will this get items or destructibles? Hopefully not.
                            }

                        }
                    }

                    if (ntd.whichAbility.CheckAbilityTag(AbilityTags.CURSORTARGET) && ntd.targetActors.Count == 0 && (ntd.whichAbility.CheckAbilityTag(AbilityTags.HEROAFFECTED) || ntd.whichAbility.CheckAbilityTag(AbilityTags.MONSTERAFFECTED)))
                    {
                        UIManagerScript.PlayCursorSound("Error");
                        return true;
                    }

                    gms.AddBufferTargetData(ntd, false);

                    targetClicksRemaining--;
                    if (targetClicksRemaining == 0)
                    {
                        // Continue on
                    }
                    else
                    {

                        int whichTarget = targetClicksMax - targetClicksRemaining + 1;
                        StringManager.SetTag(0, abilityToTry.abilityName);
                        string disp = StringManager.GetString("log_prompt_ability_target");
                        GameLogScript.GameLogWrite(disp + " (" + whichTarget + "/" + targetClicksMax + ")", heroPCActor);
                        UIManagerScript.uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Select");
                        // Is there logic for more abilities? If so, load up the new ability here
                        if (abilityToTry.subAbilities != null)
                        {
                            if (abilityToTry.subAbilities.Count > 0)
                            {
                                foreach (AbilityScript aTry in abilityToTry.subAbilities)
                                {
                                    if (whichTarget == aTry.targetChangeCondition)
                                    {
                                        abilityToTry = aTry;
                                        uims.UpdateAbilityToTry(aTry);
                                    }
                                }
                            }
                        }
                        UIManagerScript.UpdateTargetingMeshes();

                        // HARDCODED - AUTO SELECT OPPOSITE SQUARE FOR HOLD THE MOON
                        if (abilityToTry.refName == "skill_holdthemoon")
                        {
                            Vector2 selected = ntd.clickedPosition;
                            Vector2 checkVector = selected - heroPCActor.GetPos();
                            checkVector.x = -1 * checkVector.x;
                            checkVector.y = -1 * checkVector.y;
                            Vector2 newVector = heroPCActor.GetPos() + checkVector;
                            uims.SetVirtualCursorPosition(newVector);
                        }

                        timeAtLastAbilSelection = Time.time;
                        return true;
                    }

                    uims.ExitTargeting();

                    TurnData td = new TurnData();
                    td.actorThatInitiatedTurn = heroPCActor;

                    if (GameMasterScript.rangedWeaponAbilityDummy == abilityToTry)
                    {
                        td.SetTurnType(TurnTypes.ATTACK);
                        if (heroPCActor.myStats.CheckParalyzeChance() == 1.0f)
                        {
                            GameLogScript.DelayedParalyzeMessage(StringManager.GetString("player_paralyzed"), heroPCActor);
                            UIManagerScript.DisplayPlayerError(heroPCActor);
                            return true;
                        }
                        td.SetSingleTargetActor(MapMasterScript.GetTargetableAtLocation(clickedPosition));
                    }
                    else
                    {
                        td.SetTurnType(TurnTypes.ABILITY);
                    }

                    td.tAbilityToTry = abilityToTry;
                    //Debug.Log("TD turntype? " + td.turnType.ToString() + " btd length " + bufferTargetData.Count);

                    if (GameMasterScript.bufferTargetData.Count == 0)
                    {
                        Debug.Log("WARNING: Cannot start a turn with no buffer target data.");
                        return true;
                    }
                    GameMasterScript.processBufferTargetDataIndex = 0;
                    gms.TryNextTurn(td, true);
                    return true;
                }

            }
            if (player.GetButtonDown("Cancel")) //|| (Input.GetButtonDown("Cancel")) || (TDTouchControls.GetMouseButtonDown(1)))
            {
                //Debug.Log("Quitting targeting without taking action");
                UIManagerScript.PlayCursorSound("Cancel");
                uims.ExitTargeting();
                GameMasterScript.gmsSingleton.SetItemBeingUsed(null);
                return true;
            }

            //We didn't do anything this frame,
            //but we did fully check input for our ability.
            fLastTimeUpdateInputFinished = Time.realtimeSinceStartup;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if input is absorbed
    /// </summary>
    /// <returns></returns>
    public static bool TryPathfindToClickedLocation()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        openList.Clear();
        tilePath.Clear();

        startTile = MapMasterScript.GetTile(heroPCActor.GetPos());
        evalTile = startTile;

        if (pathfindingToMousePosition)
        {
            finalTile = capturePFTile;
        }
        else
        {
            pathfindingToMousePosition = true;
            finalTile = capturePFTile;
            pathfindingHighlight.SetActive(true);
        }

        if (capturePFTile != null)
        {
            pathfindingHighlight.transform.position = capturePFTile.pos;
        }
        else
        {
            return true;
        }


        float lowestFscore = 999f;

        MapMasterScript.FlushTilePathfindingData(startTile.pos, finalTile.pos, false);
        openList.Add(startTile);

        int tries = 0;
        while ((openList.Count > 0) && (tries < 5000))
        {
            tries++;
            lowestFscore = 999f;

            foreach (MapTileData tile in openList)
            {
                if (tile.f < lowestFscore)
                {
                    evalTile = tile;
                    lowestFscore = tile.f;
                }
            }

            if (Vector2.Equals(evalTile.pos, finalTile.pos))
            {
                // Found our path!
                finalTile = evalTile;
                break;
            }

            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                Vector2 newPos = evalTile.pos + MapMasterScript.xDirections[i];
                adjacentValid[i] = false;
                if ((newPos.x > 0) && (newPos.y > 0) && (newPos.x < MapMasterScript.activeMap.columns - 1) && (newPos.y < MapMasterScript.activeMap.rows - 1))
                {
                    MapTileData newtile = MapMasterScript.GetTile(newPos);
                    adjacent[i] = newtile;
                    adjacentValid[i] = true;
                }
            }

            openList.Remove(evalTile);
            evalTile.open = false;
            evalTile.closed = true;

            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                if (!adjacentValid[i]) continue;
                MapTileData tile = adjacent[i];
                if (Vector2.Equals(tile.pos, finalTile.pos))
                {
                    tile.parent = evalTile;
                    evalTile.child = tile;
                    tile.f = -1;
                    openList.Add(tile);
                    break;
                }

                if (tile.tileType != TileTypes.GROUND)
                {
                    continue;
                }

                if ((!MapMasterScript.activeMap.dungeonLevelData.revealAll) && (!MapMasterScript.activeMap.exploredTiles[(int)tile.pos.x, (int)tile.pos.y])) // Is this ok?
                {
                    continue;
                }

                if (tile.closed)
                {
                    continue;
                }

                float gScore = evalTile.g + 1;

                // Modify gScore here

                if (tile.CheckTag(LocationTags.WATER))
                {
                    gScore *= 1.2f;
                }

                if (tile.CheckTag(LocationTags.MUD))
                {
                    gScore *= 1.3f;
                }

                if (tile.CheckTag(LocationTags.LAVA) || tile.CheckTag(LocationTags.LASER))
                {
                    gScore *= 2.5f;
                }

                if (tile.CheckTag(LocationTags.ELECTRIC))
                {
                    gScore *= 2.5f;
                }

                bool containsTile = false;

                if (openList.Contains(tile))
                {
                    float localF = gScore + tile.GetHScore(finalTile);
                    if (localF < tile.f)
                    {
                        // better path
                        tile.g = gScore;
                        tile.f = localF;
                        tile.parent = evalTile;
                        evalTile.child = tile;
                    }
                    containsTile = true;
                }

                if (!containsTile)
                {
                    // Not in open list   
                    tile.parent = evalTile;
                    evalTile.child = tile;
                    tile.g = gScore; // # of steps to get to this tile
                                     //tile.h = Vector2.Distance(tile.pos, finalTile.pos) * 5f;
                                     //tile.h = MapMasterScript.GetGridDistance(tile.pos, finalTile.pos); //Modifying this because diagonals are OK.
                    tile.f = tile.g + tile.GetHScore(finalTile);
                    openList.Add(tile);
                }
            }

        }
        if (tries >= 2500)
        {
            Debug.Log("Broke player pathfinding while loop");
            return true;
        }

        if (openList.Count > 0)
        {
            // Found a path
            bool finished = false;
            MapTileData pTile = finalTile.parent;
            tilePath.Clear();
            tilePath.Add(finalTile);
            tilePath.Add(pTile);
            tries = 0;
            while ((!finished) && (tries <= 250))
            {
                if (pTile.parent == null)
                {
                    finished = true;
                    nextMove = pTile;
                }
                else if (Vector2.Equals(pTile.parent.pos, startTile.pos))
                {
                    // Use pTile as the next move.
                    finished = true;
                    nextMove = pTile;
                    //tilePath.Add(startTile);
                }
                //Debug.Log("Ptile is " + pTile.pos + " when traveling " + GetPos() + " " + finalTile.pos);

                pTile = pTile.parent;
                tilePath.Add(pTile);
            }
            if (tries >= 250)
            {
                Debug.Log("Broke player pathfinding while loop 2");
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns TRUE if input is handled
    /// </summary>
    /// <returns></returns>
    public static bool HandleMouseInputPostPathfinding()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if ((TDTouchControls.GetMouseButton(mouseButtonForStraightMove) || TDTouchControls.GetMouseButton(mouseButtonForPathfind)) && !uims.CheckTargeting() && mouseMovement &&
            !PlayerOptions.disableMouseMovement)
        {

            // Simple 8-dir movement.
            if (GameMasterScript.playerMovingAnimation)
            {
                return true;
            }
            if (turnTimer < GameMasterScript.movementMouseDelayTime)
            {
                return true;
            }
            turnTimer = 0;
            Vector2 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 targetingPosition = new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f));

            targetingPosition.x = Mathf.Clamp(targetingPosition.x, 0, MapMasterScript.activeMap.columns);
            targetingPosition.y = Mathf.Clamp(targetingPosition.y, 0, MapMasterScript.activeMap.rows);

            float distance = MapMasterScript.GetGridDistance(heroPCActor.GetPos(), targetingPosition);
            if (distance == 1)
            {
                // One space away, so try moving there?
                if (MapMasterScript.GetTile(targetingPosition).tileType == TileTypes.WALL)
                {
                    return true;
                }
                TurnData td = new TurnData();
                td.actorThatInitiatedTurn = heroPCActor;
                td.SetTurnType(TurnTypes.MOVE);
                timeSinceLastActionInput = Time.time;
                td.newPosition = targetingPosition;
                if (GameMasterScript.playerMovingAnimation)
                {
                    return true;
                }
                pathfindingHighlight.SetActive(false);
                pathfindingToMousePosition = false;
                movingOnLineToMousePosition = false;
                gms.TryNextTurn(td, true);
                return true;
            }
            else
            {
                if (distance > 1)
                {
                    if (capturePFTile == null)
                    {
                        capturePFTile = MapMasterScript.GetTile(targetingPosition);
                        if (capturePFTile.GetTargetable() != null)
                        {
                            // If we're here, it's a monster that is not visible or within range, but we don't want to ever move by
                            // clicking on a tile with a monster on it.
                            return true;
                        }
                    }

                    if (TDTouchControls.GetMouseButton(mouseButtonForStraightMove) && !PlayerOptions.disableMouseMovement)
                    {
                        // Pathfind to mouse point
                        float angle = CombatManagerScript.GetAngleBetweenPoints(heroPCActor.GetPos(), targetingPosition);

                        Directions moveDir = MapMasterScript.GetDirectionFromAngle(angle);
                        Vector2 newPos = heroPCActor.GetPos() + MapMasterScript.xDirections[(int)moveDir];

                        if (MapMasterScript.GetTile(newPos).tileType == TileTypes.WALL)
                        {
                            return true;
                        }

                        TurnData ntd = new TurnData();
                        ntd.actorThatInitiatedTurn = heroPCActor;
                        ntd.SetTurnType(TurnTypes.MOVE);
                        timeSinceLastActionInput = Time.time;
                        ntd.newPosition = newPos;
                        gms.TryNextTurn(ntd, true);
                        return true;

                    }
                }
            }
        }

        return false;
    }

    public static bool HandleMouseInputForPathfinding()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        if (TDTouchControls.GetMouseButton(mouseButtonForPathfind) && !uims.CheckTargeting() && mouseMovement &&
            !PlayerOptions.disableMouseMovement)
        {
            if (GameMasterScript.playerMovingAnimation)
            {
                return true;
            }
            if (turnTimer < GameMasterScript.movementMouseDelayTime)
            {
                return true;
            }
            turnTimer = 0;
            Vector2 basePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 targetingPosition = new Vector2((int)Math.Floor(basePosition.x + 0.5f), (int)Math.Floor(basePosition.y + 0.5f));

            targetingPosition.x = Mathf.Clamp(targetingPosition.x, 0, MapMasterScript.activeMap.columns);
            targetingPosition.y = Mathf.Clamp(targetingPosition.y, 0, MapMasterScript.activeMap.rows);


            if (TDTouchControls.GetMouseButtonDown(mouseButtonForPathfind) || capturePFTile == null)
            {
                // Fresh pathfinding button down, probably
                capturePFTile = MapMasterScript.GetTile(targetingPosition);

                if (capturePFTile == null) return true;
                if (MapMasterScript.GetGridDistance(heroPCActor.GetPos(), capturePFTile.pos) <= 1f)
                {
                    TurnData ntd = new TurnData();
                    ntd.actorThatInitiatedTurn = heroPCActor;
                    ntd.SetTurnType(TurnTypes.MOVE);
                    ntd.newPosition = capturePFTile.pos;
                    timeSinceLastActionInput = Time.time;
                    gms.TryNextTurn(ntd, true);
                    return true;
                }
                else
                {
                    if (capturePFTile.tileType == TileTypes.WALL)
                    {
                        ClearMousePathfinding();
                        return true; // Can't PF to collidable.
                    }
                }
            }

            if (capturePFTile.GetTargetable() != null)
            {
                // If we're here, it's a monster that is not visible or within range, but we don't want to ever move by
                // clicking on a tile with a monster on it.
                return true;
            }


            if (capturePFTile.tileType == TileTypes.WALL)
            {
                ClearMousePathfinding();
                return true;
            }
            //Debug.Log("Moving on line? " + movingOnLineToMousePosition + " PF? " + pathfindingToMousePosition + " " + capturePFTile.pos + " " + capturePFTile.tileType + " " + capturePFTile.IsCollidable(heroPCActor));

            if ((MapMasterScript.GetGridDistance(heroPCActor.GetPos(), capturePFTile.pos) <= 1) && (!uims.CheckTargeting()))
            {
                TileInteractions.TryPickupItemsInHeroTile();
                bool check = TravelManager.TryTravelStairs();
                TurnData ntd = new TurnData();
                ntd.actorThatInitiatedTurn = heroPCActor;
                ntd.SetTurnType(TurnTypes.MOVE);
                timeSinceLastActionInput = Time.time;
                ntd.newPosition = capturePFTile.pos;
                ClearMousePathfinding();
                if (!check)
                {
                    gms.TryNextTurn(ntd, true);
                }
                return true;
            }

            if ((movingOnLineToMousePosition || pathfindingToMousePosition) && capturePFTile.pos == heroPCActor.GetPos())
            {
                ClearMousePathfinding();
                return true;
            }

            bool forceStraightMove = false;

            if (movingOnLineToMousePosition || forceStraightMove)
            {
                // Moving along a line.
                CustomAlgorithms.GetPointsOnLineNoGarbage(heroPCActor.GetPos(), capturePFTile.pos);

                if (CustomAlgorithms.numPointsInLineArray == 0)
                {
                    ClearMousePathfinding();
                    return true;
                }

                int indexOfStartTile = 0;
                if (CustomAlgorithms.pointsOnLine[0] == heroPCActor.GetPos())
                {
                    indexOfStartTile++;
                }
                if (CustomAlgorithms.pointsOnLine[indexOfStartTile] == capturePFTile.pos)
                {
                    ClearMousePathfinding();
                    return true;
                }

                if ((MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[indexOfStartTile]).tileType == TileTypes.WALL) && (!forceStraightMove))
                {
                    ClearMousePathfinding();
                    return true;
                }

                bool straightLineToTarget = true;

                //foreach (Vector2 pt in fPoints)
                for (int i = indexOfStartTile; i < CustomAlgorithms.numPointsInLineArray; i++)
                {
                    Vector2 pt = CustomAlgorithms.pointsOnLine[i];
                    MapTileData mtdCheck = MapMasterScript.GetTile(pt);
                    if (((mtdCheck.IsCollidable(heroPCActor)) || (mtdCheck.IsDangerous(heroPCActor))) && (!forceStraightMove))
                    {
                        straightLineToTarget = false;
                        break;
                    }
                }

                if (straightLineToTarget)
                {
                    TurnData ntd = new TurnData();
                    ntd.actorThatInitiatedTurn = heroPCActor;
                    ntd.SetTurnType(TurnTypes.MOVE);
                    timeSinceLastActionInput = Time.time;
                    ntd.newPosition = CustomAlgorithms.pointsOnLine[indexOfStartTile];
                    gms.TryNextTurn(ntd, true);
                    return true;
                }
                else
                {
                    movingOnLineToMousePosition = false;
                }
            }

            if (forceStraightMove) return true;

            if (!pathfindingToMousePosition && !movingOnLineToMousePosition)
            {
                // Haven't started a move. Is this in direct LOS?

                // Player pathfinding

                if (MapMasterScript.CheckTileToTileLOS(heroPCActor.GetPos(), capturePFTile.pos, heroPCActor, MapMasterScript.activeMap))
                {
                    CustomAlgorithms.GetPointsOnLineNoGarbage(heroPCActor.GetPos(), capturePFTile.pos);

                    if (CustomAlgorithms.numPointsInLineArray == 0)
                    {
                        ClearMousePathfinding();
                        return true;
                    }

                    int indexOfStartTile = 0;
                    if (CustomAlgorithms.pointsOnLine[indexOfStartTile] == heroPCActor.GetPos())
                    {
                        indexOfStartTile++;
                    }
                    if (CustomAlgorithms.pointsOnLine[indexOfStartTile] == capturePFTile.pos)
                    {
                        ClearMousePathfinding();
                        return true;
                    }

                    bool straightLineToTarget = true;

                    //foreach(Vector2 pt in fPoints)
                    for (int i = indexOfStartTile; i < CustomAlgorithms.numPointsInLineArray; i++)
                    {
                        Vector2 pt = CustomAlgorithms.pointsOnLine[i];
                        MapTileData mtdCheck = MapMasterScript.GetTile(pt);
                        if ((mtdCheck.IsCollidable(heroPCActor)) || (mtdCheck.IsDangerous(heroPCActor)))
                        {
                            straightLineToTarget = false;
                            break;
                        }
                    }

                    if (straightLineToTarget)
                    {
                        movingOnLineToMousePosition = true;
                        pathfindingHighlight.SetActive(true);
                        pathfindingHighlight.transform.position = capturePFTile.pos;

                        if (MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[indexOfStartTile]).tileType == TileTypes.WALL)
                        {
                            return true;
                        }

                        TurnData ntd = new TurnData();
                        ntd.actorThatInitiatedTurn = heroPCActor;
                        ntd.SetTurnType(TurnTypes.MOVE);
                        timeSinceLastActionInput = Time.time;
                        ntd.newPosition = CustomAlgorithms.pointsOnLine[indexOfStartTile];
                        gms.TryNextTurn(ntd, true);
                        return true;
                    }

                }
            }

            if (TryPathfindToClickedLocation())
            {
                return true;
            }

            // End pathfind

            // I shouldn't have to do this
            tilePath.Remove(startTile);
            tilePath.Remove(startTile);

            TurnData td = new TurnData();
            td.actorThatInitiatedTurn = heroPCActor;
            td.SetTurnType(TurnTypes.MOVE);
            timeSinceLastActionInput = Time.time;
            if (tilePath.Count >= 1 && tilePath[tilePath.Count - 1] != null)
            {
                td.newPosition = tilePath[tilePath.Count - 1].pos;
            }
            else
            {
                pathfindingToMousePosition = false;
                pathfindingHighlight.SetActive(false);
                return true;
            }

            gms.TryNextTurn(td, true);
            return true;
        }

        return false;
    }

    public static bool IsInputDisabled()
    {
        return disableAllInput;
    }

    public static bool IsCompareAlternateButtonDown()
    {
        bool state = player.GetButtonDown("Compare Alternate");
#if !UNITY_SWITCH
        if (!state)
        {
            state = player.GetButtonDown("Compare Alternate");
        }
#endif
        return state;
    }

    public static bool IsCompareAlternateButtonHeld()
    {
        bool state = player.GetButton("Compare Alternate");
#if !UNITY_SWITCH
        if (!state)
        {
            state = player.GetButton("Compare Alternate");
        }
#endif
        return state;
    }

    public static bool IsCompareAlternateButtonUp()
    {
        bool state = player.GetButtonUp("Compare Alternate");
#if !UNITY_SWITCH
        if (!state)
        {
            state = player.GetButtonUp("Compare Alternate");
        }
#endif
        return state;
    }

    public static bool IsRingMenuButtonDown()
    {
        return player.GetButtonDown("Toggle Ring Menu");
    }

    public static Directions GetDirectionalInput()
    {
        bool usingJoystick = false;
        if (PlatformVariables.GAMEPAD_ONLY)
        {
            usingJoystick = true;
        }
        else
        {
            usingJoystick = ReInput.controllers.GetLastActiveControllerType() == ControllerType.Joystick;
        }

        // This should stay as COUNT until we confirm some kind of directional input
        Directions directionCapturedThisFrame = Directions.COUNT;

        // Check to see if there's dpad or stick movement. 
        float joystickDeadZone = 0.33f;
        if (!PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
        {
            joystickDeadZone = PlayerOptions.buttonDeadZone / 100f;
        }

        //if the dpad is being pressed, we do not want a deadzone, because we want to 
        //act as if the dpad press is digital 0/1, instead of an analog axis like the stick gives us.

        bool diagonalMoveOnly = player.GetButton("Diagonal Move Only");

        if (directionCapturedThisFrame == Directions.COUNT)
        {
            directionCapturedThisFrame = CheckForDiscreteDirectionalInput(diagonalMoveOnly);
        }

        if (usingJoystick && directionCapturedThisFrame == Directions.COUNT)
        {
            Vector2 vMoveAxis = new Vector2(player.GetAxis("Move Horizontal"), player.GetAxis("Move Vertical"));

            // if deadzone, check for the four diagonal move buttons
            // otherwise return neutral
            if (vMoveAxis.magnitude < joystickDeadZone)
            {
                directionCapturedThisFrame = Directions.NEUTRAL;
            }

            if (directionCapturedThisFrame != Directions.NEUTRAL)
            {
#if UNITY_EDITOR
                //Debug.Log("Analog input: " + vMoveAxis + " " + joystickDeadZone + " " + vMoveAxis.magnitude);
#endif

                //this value is clamalamped at 180, so if we know we are pointing left, subtract this value from
                //360 to get a True Angle TM
                float fStickAngle = Vector2.Angle(vMoveAxis, Vector2.up);
                if (vMoveAxis.x < 0)
                {
                    fStickAngle = 360f - fStickAngle;
                }

                //break compass into eight parts, for the eight directions that will go into the Directions Gauntlet.
                float fConeDegrees = 360.0f / 8;

                //quantize our 0-360 value to 45 degree increments, and then 0-8 (360 == 8)
                int iRoundifiedValue = (int)Mathf.Round(fStickAngle / fConeDegrees) % 8;

                //if we're diagonal only and not in a menu, drop any not-diagonal values
                if (diagonalMoveOnly &&
                    !UIManagerScript.AnyInteractableWindowOpen() &&
                    iRoundifiedValue % 2 == 0)
                {
                    directionCapturedThisFrame = Directions.NEUTRAL;
                }
                else
                {
                    directionCapturedThisFrame = Directions.NORTH + (iRoundifiedValue % 8);
                }
            }
        }

        // For example, we tapped dpad left to select something in the ring menu.
        // And this frame - after the ring menu has closed - we are still holding left
        // We should ignore that left input forever, or until we press some other direction
        if (!Switch_RadialMenu.IsActive() && Switch_RadialMenu.directionHeldWhenConfirmWasPressed != Directions.COUNT)
        {
            if (Switch_RadialMenu.directionHeldWhenConfirmWasPressed == directionCapturedThisFrame)
            {
                directionCapturedThisFrame = Directions.NEUTRAL;
            }
            else
            {
                // We pressed a different direction, so we can now turn off this input-canceling nonsense
                Switch_RadialMenu.directionHeldWhenConfirmWasPressed = Directions.COUNT;
            }
        }

        if (directionCapturedThisFrame == Directions.COUNT) directionCapturedThisFrame = Directions.NEUTRAL;



        return directionCapturedThisFrame;
    }

    public static void ResetPathfindingVariablesAndLists()
    {
        if (openList == null)
        {
            openList = new HashSet<MapTileData>();
        }

        openList.Clear();

        if (tilePath == null)
        {
            tilePath = new List<MapTileData>(100);
        }

        tilePath.Clear();
    }

    static void CheckForMinimapInput(bool allowOnlyMovement)
    {
        // See if we can move the minimap via analogs tick.
        if (MinimapUIScript.GetOverlay())
        {
            float xAxis = player.GetAxis("Move Minimap Horizontal");
            float yAxis = player.GetAxis("Move Minimap Vertical");
            if (xAxis != 0 || yAxis != 0)
            {
                MinimapUIScript.MoveMinimap(xAxis, yAxis);
            }
        }

        if (!allowOnlyMovement && player.GetButtonDown("Toggle Small Minimap"))
        {
            gms.ToggleMinimap(MinimapStates.SMALL, alwaysSwitchOnOrOff: true);
        }

        if (!allowOnlyMovement && player.GetButtonDown("Toggle Large Minimap"))
        {
            gms.ToggleMinimap(MinimapStates.LARGE, alwaysSwitchOnOrOff:true);
        }

        //cycle through all three types
        if (!allowOnlyMovement && player.GetButtonDown("Cycle Minimap"))
        {
            MinimapUIScript.SetMinimapToStateBasedOnPlayerOptions();
        }
    }

    static void CheckForClearMousePathfindingOnButtonsUp()
    {
        if (TDTouchControls.GetMouseButtonUp(0))
        {
            ClearMousePathfinding();
        }
        if (TDTouchControls.GetMouseButtonUp(1))
        {
            ClearMousePathfinding();
        }
    }

    static bool CheckForToggleMenuSelectInput(bool allowOnlyMovement)
    {
        // Try to get this in the input handler.
        if (!allowOnlyMovement && player.GetButtonDown("Toggle Menu Select"))
        {

            if (ItemWorldUIScript.itemWorldInterfaceOpen)
            {
                ItemWorldUIScript.singleton.CloseItemWorldInterface(0);
                return true;
            }
            if (ShopUIScript.CheckShopInterfaceState())
            {
                ShopUIScript.CloseShopInterface();
                return true;
            }

            //Debug.LogError("Player pressed toggle menu select");
                if (UIManagerScript.AnyInteractableWindowOpen())
            {
                Debug.LogError("There's a window open");
                if (!UIManagerScript.PreventingOptionMenuToggle()) 
                {                    
                    //Debug.LogError("Try close UI");
                    UIManagerScript.TryCloseFullScreenUI();
                }
                else {
                }
            }
            else
            {
                UIManagerScript.OpenFullScreenUI(UIManagerScript.lastUITabOpened);
            }

            return true;
        }

        return false;
    }

    static bool CheckForKeyboardUIShortcuts(bool allowOnlyMovement, bool keyDialogOpen)
    {
        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            if (!allowOnlyMovement && player.GetButtonDown("View Equipment") && !keyDialogOpen)
            {
                if (UIManagerScript.GetWindowState(UITabs.EQUIPMENT))
                {
                    if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                }
                else
                {
                    UIManagerScript.OpenFullScreenUI(UITabs.EQUIPMENT);
                }
                return true;
            }
            if (!allowOnlyMovement && player.GetButtonDown("View Consumables") && !keyDialogOpen)
            {
                if (UIManagerScript.GetWindowState(UITabs.INVENTORY))
                {
                    if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                }
                else
                {
                    UIManagerScript.OpenFullScreenUI(UITabs.INVENTORY);
                }
                return true;
            }
            if (!allowOnlyMovement && player.GetButtonDown("View Skills") && !keyDialogOpen)
            {
                if (UIManagerScript.GetWindowState(UITabs.SKILLS))
                {
                    if (!UIManagerScript.PreventingOptionMenuToggle()) UIManagerScript.TryCloseFullScreenUI();
                }
                else
                {
                    UIManagerScript.OpenFullScreenUI(UITabs.SKILLS);
                }

                return true;
            }
            if (!allowOnlyMovement && player.GetButtonDown("View Healing Items"))
            {
                uims.CloseHotbarNavigating();
                SnackBagUIScript.singleton.OpenSnackBagUI();
                return true;
            }

            if (!allowOnlyMovement && player.GetButtonDown("Toggle Player Health Bar"))
            {
                gms.TogglePlayerHealthBarFromKB();
                return true;
            }

            if (!allowOnlyMovement && player.GetButtonDown("Toggle Monster Health Bars"))
            {
                gms.ToggleMonsterHealthBarsFromKB();
                return true;
            }

            if (!allowOnlyMovement && player.GetButtonDown("Toggle Pet HUD"))
            {
                gms.TogglePetHUDFromKB();
                return true;
            }
        }

        return false;
    }

    static void CheckForInputAffectingDiagonalMovementOverlay(HeroPC heroPCActor)
    {
        // Only do this if no window is open
        if (player.GetButtonDown("Diagonal Move Only"))
        {
            heroPCActor.diagonalOverlay.SetActive(true);
            heroPCActor.diagonalOverlay.transform.localPosition = Vector3.zero; // New in 2/2/18 to make sure it doesn't get decentered
        }
        if (player.GetButtonUp("Diagonal Move Only"))
        {
            heroPCActor.diagonalOverlay.SetActive(false);
        }

        if (heroPCActor.diagonalOverlay.activeSelf && !player.GetButton("Diagonal Move Only"))
        {
            heroPCActor.diagonalOverlay.SetActive(false);
        }
    }

    static void UpdateControllerPromptVisibilityBasedOnInputType()
    {
        // Check for controller input?
        if (!PlayerOptions.showControllerPrompts && lastActiveControllerType == ControllerType.Joystick)
        {
            PlayerOptions.showControllerPrompts = true;
            UIManagerScript.optionsShowControllerPrompts.gameObj.GetComponent<Toggle>().isOn = true;
        }
        else if (PlayerOptions.showControllerPrompts && lastActiveControllerType == ControllerType.Keyboard)
        {
            PlayerOptions.showControllerPrompts = false;
            UIManagerScript.optionsShowControllerPrompts.gameObj.GetComponent<Toggle>().isOn = false;
        }
    }

    static bool CheckForKeyboardSpecificActionShortcuts(bool allowOnlyMovement)
    {
        if (!PlatformVariables.GAMEPAD_ONLY)
        {
            if (!allowOnlyMovement && !UIManagerScript.AnyInteractableWindowOpen() && !uims.CheckTargeting() && player.GetButtonDown("Use Healing Flask"))
            {
                gms.UseAbilityRegenFlask();
                return true;
            }

            if (!allowOnlyMovement && !UIManagerScript.AnyInteractableWindowOpen() && !uims.CheckTargeting())
            {
                if (player.GetButtonDown("Build Planks"))
                {
                    if (!MapMasterScript.activeMap.IsItemWorld())
                    {
                        GameLogScript.LogWriteStringRef("log_error_dream_planks");
                        return true;
                    }

                    gms.TryUseItemViaShortcut("item_planks");
                    return true;
                }
                else if (player.GetButtonDown("Use Shovel"))
                {
                    if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("item_itemworldwallbreaker") > 0)
                    {
                        gms.TryUseItemViaShortcut("item_itemworldwallbreaker");
                    }
                    else if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("item_wallbreaker") > 0)
                    {
                        gms.TryUseItemViaShortcut("item_wallbreaker");
                    }
                    return true;
                }
                else if (player.GetButtonDown("Use Monster Mallet"))
                {
                    gms.TryUseItemViaShortcut("item_monstermallet");
                    return true;
                }

            }


            if (!allowOnlyMovement && !UIManagerScript.AnyInteractableWindowOpen() && !uims.CheckTargeting() && player.GetButtonDown("Use Town Portal"))
            {
                gms.CheckAndTryAbility(GameMasterScript.escapeTorchAbility);
                return true;
            }
        }

        return false;
    }

    static bool CheckForNPCInteractionsOnConfirm(bool bConfirmPressedSoDontEatInput, HeroPC heroPCActor, Directions dInput)
    {
        // Check for movement and other actions
        if (bConfirmPressedSoDontEatInput && MapMasterScript.GetTile(heroPCActor.GetPos()).GetStairsInTile() == null)
        {
            if (MapMasterScript.activeMap.floor == MapMasterScript.FROG_DRAGON_DUNGEONEND_FLOOR &&
                GameMasterScript.gmsSingleton.ReadTempGameData("frogdying") == 1) return true;

            // See if any nearby NPCs want to chat!
            MapTileData pointingTile = MapMasterScript.GetTile(heroPCActor.GetPos() + MapMasterScript.xDirections[(int)heroPCActor.lastMovedDirection]);
            GameMasterScript.pool_checkMTDs.Clear();
            GameMasterScript.pool_checkMTDs.Add(pointingTile); // Check the tile we're pointing at...
            GameMasterScript.pool_checkMTDs.Add(MapMasterScript.GetTile(heroPCActor.GetPos())); // Also check the tile we are ON

            Directions dirPressedOrHeld = GetDirectionalInput();

            // And the nearby ones, if we're on the bridge.
            if ((heroPCActor.GetPos() == new Vector2(18f,11f) || heroPCActor.GetPos() == new Vector2(20f, 11f)) &&
                dirPressedOrHeld == Directions.NEUTRAL)
            {

                GameMasterScript.pool_checkMTDs.Add(MapMasterScript.GetTile(new Vector2(19f, 10f))); // just add the river directly lol
            }

            foreach (MapTileData mtd in GameMasterScript.pool_checkMTDs)
            {
                NPC check = mtd.GetInteractableNPC();
                if (check != null)
                {
                    Conversation convo = check.GetConversation();
                    if (convo != null)
                    {
                        if (check.noBumpToTalk && dInput != Directions.NEUTRAL && dInput != Directions.NORTH && dInput != Directions.SOUTH)
                        {
                            continue;
                        }
                        UIManagerScript.StartConversation(convo, DialogType.STANDARD, check);
                    }
                    else
                    {
                        if (Debug.isDebugBuild) Debug.Log("No conversation to start, but there should be.");
                    }
                    timeSinceLastActionInput = Time.time;
                    heroPCActor.ChangeCT(-100f);
                    return true;
                }
            }
        }

        return false;
    }
}
