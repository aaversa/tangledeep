using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

//Pops up X icons around the player, use the stick to select one
public class Switch_RadialMenu : MonoBehaviour
{
    //how many units away from the player should these icons appear
    public int iButtonPxFromCenter;

    //List of selectable buttons
    //this is TRASH but Unity doesn't want List<List<T>> in the inspector, so who is the trash really?*
    public List<Switch_RadialMenuButton> buttonRing0;
    public List<Switch_RadialMenuButton> buttonRing1;

    private List<List<Switch_RadialMenuButton>> listButtonCollection;
    private int iButtonCollectionIdx;

    private bool bIsActive;
    private static int iTickTockTracker;

    public static bool bLoadedFromBundle;

    //scaling value because resolution changes
    [HideInInspector]
    public static float fScaleValue;

    private static Canvas myCanvas;

    /// <summary>
    /// The direction (e.g. dpad, analog stick) that was held at the frame that the ring menu option was confirmed.
    /// </summary>
    public static Directions directionHeldWhenConfirmWasPressed;

    void Awake()
    {
        listButtonCollection = new List<List<Switch_RadialMenuButton>>();
        InitializeListOfButtons(buttonRing0);
        InitializeListOfButtons(buttonRing1);

        var goCanvas = GameObject.Find("DynamicCanvas");
        myCanvas = goCanvas.GetComponent<Canvas>();
        fScaleValue = myCanvas.scaleFactor;

        iButtonPxFromCenter = (int) (iButtonPxFromCenter * fScaleValue);

    }

    void InitializeListOfButtons(List<Switch_RadialMenuButton> listButtons)
    {
        foreach (var btn in listButtons)
        {
            btn.SetActionOnSelect(typeof(Switch_RadialMenu).GetMethod(btn.strFunctionOnSelect));
            btn.gameObject.SetActive(false);
        }

        listButtonCollection.Add(listButtons);
    }

    void Start ()
    {
        /*
        listButtons = GetComponentsInChildren<Switch_RadialMenuButton>().ToList();
        foreach (var btn in listButtons)
        {
            btn.SetActionOnSelect( typeof(Switch_RadialMenu).GetMethod(btn.strFunctionOnSelect) );
            btn.gameObject.SetActive(false);
        }
        */
    }

    //Pull it out of the asset bundle, make it real, and stick into the uimanager singleton.
    public static void Initialize()
    {
        //don't do this twice
        if (UIManagerScript.radialMenu != null) return;

        UIManagerScript.singletonUIMS.StartCoroutine(InitializeCoroutine());
    }

    static IEnumerator InitializeCoroutine()
    {

        GameObject resource = Resources.Load<GameObject>("Switch_RadialMenu");
        GameObject goMenu = GameObject.Instantiate(resource);
        goMenu.transform.SetParent(myCanvas.gameObject.transform);
        goMenu.transform.SetAsLastSibling();
        UIManagerScript.radialMenu = goMenu.GetComponent<Switch_RadialMenu>();

        /* 
        //pull up the radial menu prefab bundle
        AssetBundle abTitleTimes = null;
        while (abTitleTimes == null)
        {
            abTitleTimes = TDAssetBundleLoader.GetBundleIfExists("radialmenu_object");
            yield return null;
        }

        //get that prefab
        var request = abTitleTimes.LoadAllAssetsAsync<GameObject>();
        yield return new WaitWhile(() => !request.isDone);


        foreach (Object o in request.allAssets)
        {
            if (o.name == "Switch_RadialMenu")
            {
                var goMenu = Instantiate(o) as GameObject;
                goMenu.transform.SetParent(myCanvas.gameObject.transform);
                goMenu.transform.SetAsLastSibling();
                UIManagerScript.radialMenu = goMenu.GetComponent<Switch_RadialMenu>();
            }
        } */

        bLoadedFromBundle = true;

        yield break;
    }

    void Update ()
    {
        //sure
        if (GameMasterScript.heroPC == null) return;

        if (TDInputHandler.IsInputDisabled() && bIsActive)
        {
            CloseMenu();
        }

		FocusOnHeroPosition();

        UpdateAnalogPointing();
    }

    /// <summary>
    /// If the player is using the stick to point, we should make what we're pointing at glow. 
    /// </summary>
    void UpdateAnalogPointing()
    {
        var rewiredPlayer = ReInput.players.GetPlayer(0);
        Vector2 vStickNative = new Vector2(rewiredPlayer.GetAxis("Move Horizontal"), rewiredPlayer.GetAxis("Move Vertical"));

        foreach (var btn in listButtonCollection[iButtonCollectionIdx])
        {
            btn.SetGlowySelectRatio(vStickNative);
        }
    }

    void FocusOnHeroPosition()
    {
        //place ourselves on the player
        Vector3 screenPos = Camera.main.WorldToScreenPoint(GameMasterScript.heroPC.transform.position);

        //cough cough fudge
        //magic numbers, :jim_garbage:
        screenPos.y += 8f;

        transform.position = screenPos;
    }

    public void OpenMenu()
    {
        directionHeldWhenConfirmWasPressed = Directions.COUNT;

        //start from the first ring
        iButtonCollectionIdx = 0;

        FocusOnHeroPosition();

        //move and light up each button
        var listButtons = listButtonCollection[0];
        foreach (var btn in listButtons)
        {
            //wake up
            btn.gameObject.SetActive(true);

            //protection from overlapping coroutines
            if (btn.coroutineRotation != null)
            {
                btn.StopCoroutine(btn.coroutineRotation);
            }

            //spin into position
            btn.StartCoroutine(btn.RotateMeIntoPlace(btn.offsetFromCoreWhenActive, 270f, 0.25f, 0f, iButtonPxFromCenter, false));

            //fade in
            btn.FadeIn(0.2f);
        }

        //now, every other button in other rings needs to be put into position, but not shown
        for (int t = 1; t < listButtonCollection.Count; t++)
        {
            foreach (var btn in listButtons)
            {
                var rt = btn.transform as RectTransform;
                rt.localPosition = btn.offsetFromCoreWhenActive;
            }
        }

        //darkness?
        var fadeObject = UIManagerScript.singletonUIMS.blackFade;
        fadeObject.SetActive(true);
        var fadeImage = fadeObject.GetComponent<Image>();
        LeanTween.color(fadeObject, new Color(0.0f, 0.0f, 0.0f, 0.2f), 0.2f).setOnUpdate(fadeColor => fadeImage.color = fadeColor);

        bIsActive = true;

        UIManagerScript.PlayCursorSound("RadialOpen");
    }

    /// <summary>
    /// Closes the menu with animation. Does not play sound.
    /// </summary>
    /// <param name="fDelayDeactivation">Delay the setting of bIsActive to false, this will hold up game input until accomplished.</param>
    public void CloseMenu(float fDelayDeactivation = 0f)
    {
        if (!bIsActive)
        {
            return;
        }

        //hide each button
        var listButtons = listButtonCollection[iButtonCollectionIdx];
        foreach (var btn in listButtons)
        {
            //protection from overlapping coroutines
            if (btn.coroutineRotation != null)
            {
                btn.StopCoroutine(btn.coroutineRotation);
            }

            //spin into position
            Vector2 vGoal = GameMasterScript.Rotate2DVector(btn.offsetFromCoreWhenActive, 270f);

            btn.coroutineRotation = btn.StartCoroutine(btn.RotateMeIntoPlace(vGoal, -270f, 0.25f, iButtonPxFromCenter, 0f, true));

            //fade out
            btn.FadeOut(0.25f);
        }

        //Don't auto-play the close sound here, as other UI sounds might be playing.
        //instead, do it case-by-case.

        if (fDelayDeactivation > 0f)
        {
            GameMasterScript.StartWatchedCoroutine(WaitThenDeactivate(fDelayDeactivation));
        }
        else
        {
            bIsActive = false;
        }

        //restore light!
        var fadeObject = UIManagerScript.singletonUIMS.blackFade;
        var fadeImage = fadeObject.GetComponent<Image>();
        LeanTween.color(fadeObject, new Color(0.0f, 0.0f, 0.0f, 0.0f), 0.2f).setOnUpdate( fadeColor => fadeImage.color = fadeColor );


    }

    /// <summary>
    /// Make this a WatchedCoroutine to hold up gameplay for a set amount of time. Use this after receiving input and closing the menu.
    /// </summary>
    /// <param name="fTime"></param>
    /// <returns></returns>
    IEnumerator WaitThenDeactivate(float fTime)
    {
        yield return new WaitForSeconds(fTime);
        bIsActive = false;
    }

    public static bool HandleInput(Directions directionHeldDuringThisFrame)
    {
        if (UIManagerScript.radialMenu == null) return false;

        return UIManagerScript.radialMenu.HandleInput_Internal(directionHeldDuringThisFrame);
    }

    public static bool IsActive()
    {
        return bLoadedFromBundle && UIManagerScript.radialMenu.bIsActive;
    }

    /// <summary>
    /// Checks controller input for determining which button to highlight
    /// and also if button is pressed.
    /// </summary>
    /// <returns>True if we started this call active.</returns>
    bool HandleInput_Internal(Directions directionHeldDuringThisFrame)
    {
        //Don't do anything if we are asleep zzz
        if (!bIsActive) return false;

        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;
     
        //close? Force close on game over or is dead
        if (!playerInput.GetButton("Toggle Ring Menu") ||
            GameMasterScript.playerDied)
        {
            CloseMenu();
            UIManagerScript.PlayCursorSound("RadialClose");
            return true;
        }

        //butan?
        //#todo: this requires knowledge of the editor buttons and order they are in. Bad.
        Switch_RadialMenuButton pressed = null;
        var listButtons = listButtonCollection[iButtonCollectionIdx];

        bool confirmedRadialInputWithConfirmOrDpad = false;

        //check for analog selection first
        var stickDirection = new Vector2(playerInput.GetAxis("Move Horizontal"), playerInput.GetAxis("Move Vertical"));
        if (stickDirection.sqrMagnitude > 0.1f)
        {
            //If we pressed confirm, and are pointing in a given direction, use that button.
            if (playerInput.GetButtonDown("Confirm"))
            {
                //Don't select a button if we aren't pressing the stick with some degree of commitment.
                float bestVal = 0.2f;
                foreach (var btn in listButtons)
                {
                    float dotVal = btn.GetDotValueAgainstStickInput(stickDirection);
                    if (dotVal > bestVal)
                    {
                        bestVal = dotVal;
                        pressed = btn;
                    }
                }
                
                //if confirm was pressed but the stick is only sort-of pressed, nothing happens. 
                
            }
        }
        //otherwise, if no stick is being used, rely on radial buttons.
        else
        {
            if (playerInput.GetButtonDown("RadialUp")) pressed = listButtons[0];
            if (playerInput.GetButtonDown("RadialRight")) pressed = listButtons[1];
            if (playerInput.GetButtonDown("RadialDown")) pressed = listButtons[2];
            if (playerInput.GetButtonDown("RadialLeft")) pressed = listButtons[3];
        }

        if( pressed != null )
        {
            pressed.OnSelect();
            confirmedRadialInputWithConfirmOrDpad = true;
            directionHeldWhenConfirmWasPressed = directionHeldDuringThisFrame;
        }

        return true;
    }

    /// <summary>
    /// Changes to the next ring in the list, does pretty movement stuff.
    /// </summary>
    private void SwitchToNextRing_Internal()
    {
        //take the current ring, fade them out
        var listCurrent = listButtonCollection[iButtonCollectionIdx];
        foreach (var btn in listCurrent)
        {
            //protection from overlapping coroutines
            if (btn.coroutineRotation != null)
            {
                btn.StopCoroutine(btn.coroutineRotation);
            }

            Vector2 vGoal = GameMasterScript.Rotate2DVector(btn.offsetFromCoreWhenActive, 45f);
            btn.coroutineRotation = btn.StartCoroutine(btn.RotateMeIntoPlace(vGoal,-45f,0.2f, iButtonPxFromCenter, iButtonPxFromCenter, true));
            btn.FadeOut(0.3f);
        }

        iButtonCollectionIdx++;
        iButtonCollectionIdx %= listButtonCollection.Count;

        //wake up the next ring
        var listNext = listButtonCollection[iButtonCollectionIdx];
        foreach (var btn in listNext)
        {
            btn.gameObject.SetActive(true);

            //protection from overlapping coroutines
            if (btn.coroutineRotation != null)
            {
                btn.StopCoroutine(btn.coroutineRotation);
            }

            btn.coroutineRotation = btn.StartCoroutine(btn.RotateMeIntoPlace(btn.offsetFromCoreWhenActive, -45f, 0.2f, iButtonPxFromCenter, iButtonPxFromCenter, false));
            btn.FadeIn(0.3f);
        }
        UIManagerScript.PlayCursorSound("RadialOpen");
    }


    public static void SelectRegenFlask()
    {
        GameMasterScript.gmsSingleton.UseAbilityRegenFlask();
        UIManagerScript.radialMenu.CloseMenu(0.25f);
    }

    public static void SelectTownPortal()
    {
        GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.escapeTorchAbility);
        UIManagerScript.radialMenu.CloseMenu(0.25f);
    }

    public static void SelectPassTurn()
    {
        GameMasterScript.PassTurnViaPlayerInput();
        UIManagerScript.PlayCursorSound(iTickTockTracker++ % 2 == 0 ? "UITick" : "UITock");
        //don't close or delay input.
    }

    public static void SelectPetMenu()
    {
        //This will open the pet collection AND force a recount
        //of how many pets are in the group (fightersInParty)
        PetPartyUIScript.singleton.ExpandPetPartyUI();

        var numPets = PetPartyUIScript.singleton.fightersInParty.Count;

        //Do nothing if there are no pets.
        if (numPets == 0) return;
        
        //If we have pets but these buttons aren't showing up, that is Also Bad.
        var petList = PetPartyUIScript.singleton.petReadoutButtons;
        if (petList == null || petList.Count == 0)
        {
            return;
        }

        //if there's only one pet, jump right to the dialog.
        if (numPets == 1)
        {
            petList[0].StartConversation();
            UIManagerScript.radialMenu.CloseMenu();
            return;
        }

        //Set the cursor and focus to the pet list.
        PetPartyUIScript.singleton.OnGetFocus();

        //ghost cursor to the pet window?
        GameObject goCursor = UIManagerScript.singletonUIMS.uiDialogMenuCursor;
        if (goCursor != null)
        {
            UIManagerScript.SendGhostCursorFromPointToPoint("prefab_ghostcursor_sparkles", Vector2.zero, goCursor.transform as RectTransform, 0.75f, new Vector2(64.0f, 0f));
        }

        //No sound plays otherwise
        UIManagerScript.PlayCursorSound("Confirm");
        UIManagerScript.radialMenu.CloseMenu();
    }

    public static void SwitchToNextRing()
    {
        UIManagerScript.radialMenu.SwitchToNextRing_Internal();
    }

    public static void OpenSnackBag()
    {
        UIManagerScript.radialMenu.CloseMenu(0.25f);
        GameMasterScript.heroPCActor.myInventory.SortMyInventory(InventorySortTypes.CONSUMABLETYPE, true, false);
        UIManagerScript.OpenSpecialItemBagFullScreenUI( UIManagerScript.EMessageForFullScreenUI.inventory_as_snack_bag);
    }

    //she snak, but she also attac
    public static void OpenAttackBag()
    {
        UIManagerScript.radialMenu.CloseMenu(0.25f);
        GameMasterScript.heroPCActor.myInventory.SortMyInventory(InventorySortTypes.CONSUMABLETYPE, true, false);
        UIManagerScript.OpenSpecialItemBagFullScreenUI(UIManagerScript.EMessageForFullScreenUI.inventory_as_attack_bag);
    }

    public static object Debug_PetMenu(string[] args)
    {
        SelectPetMenu();
        return "Pets!";
    }
}

public partial class UIManagerScript
{
    public static Switch_RadialMenu radialMenu;
}



// * it is me. :jim_garbage: