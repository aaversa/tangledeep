using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Extension of this catch-all button class for inventory specific needs
//on console and mobile
public class Switch_InvItemButton : EQItemButtonScript
{
    public enum ELastInputSource
    {
        keyboard_or_gamepad = 0,
        mouse,
        MAX
    }

    private ELastInputSource lastInputSource;

    [HideInInspector] public UIManagerScript.UIObject myUIObject;

    [Header("Button Backgrounds")]
    public Sprite spriteButtonUnclicked;
    public Color colorFontUnclicked;

    public Sprite spriteButtonClicked;
    public Color colorFontClicked;

    [Header("Allow Button Toggling")]
    [Tooltip("Set this to false for buttons that don't have on/off visual states")]
    public bool ToggleWhenActivated;
    public string strStringCodeLabel;
    public TextMeshProUGUI txtLabel;

    private Image imageBackgroundSprite;

    //keep track of being toggled on or off
    private bool bToggled;

    [HideInInspector]
    public Action<int[], ELastInputSource > onClickAction;
    public Action<int[], ELastInputSource> onPointerEnter;
    public Action<int[], ELastInputSource> onPointerExit;
    public Action<int[], ELastInputSource> onDrag;
    public Action<int[], ELastInputSource> onDrop;
    public Action<int[], ELastInputSource> onCancel;

    [HideInInspector]
    public int[] iSubmitValue;

    private bool bInitialized;

    private ISelectableUIObject containedData;

    public Switch_UIButtonColumn myButtonColumn;

    private Vector2 vStoredAnchorPositionPreTween;

    [Header("Binding to equipped slots")]
    [Tooltip("Which gear slot on the character should I represent? 'Weapon' means 1 of 4 hotbar slots.")]
    public EquipmentSlots boundGearSlot = EquipmentSlots.COUNT;
    [Tooltip("If I am 'Weapon' above, which hot swappo bop bar hotslots bots (0-3) do I represent?")]
    public int weaponHotbarIdx = -1;

    public void Awake()
    {
        Initialize();

        //If our colors are changed before Awake() is called,
        //double check here, because TMP might change them back!        
        if (bToggled)
        {
            ToggleButton(true);
        }
    }

    public void SetContainedData(ISelectableUIObject newData, bool bUpdateImageAndLabel = false)
    {
        containedData = newData;

        if (bUpdateImageAndLabel && myUIObject != null)
        {
            if (myUIObject.subObjectImage != null)
            {
                myUIObject.subObjectImage.enabled = true;
                myUIObject.subObjectImage.color = new Color(1, 1, 1, 1);
                myUIObject.subObjectImage.sprite = newData.GetSpriteForUI();

            }
            if (txtLabel != null)
            {
                txtLabel.text = newData.GetNameForUI();
            }
        }
    }

    public ISelectableUIObject GetContainedData()
    {
        return containedData;
    }

    public bool IsToggled()
    {
        return bToggled;
    }

    public void ClearContainedData(bool bAlsoClearVisualInfo)
    {
        containedData = null;
        if (bAlsoClearVisualInfo)
        {
            if (txtLabel != null)
            {
                txtLabel.text = "";
                //txtLabel.color = Color.white; Let the font manager handle this
            }
            if (myUIObject.subObjectImage != null)
            {
                myUIObject.subObjectImage.sprite = null;
                myUIObject.subObjectImage.color = new Color(0, 0, 0, 0);
            }
        }
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }


    //returns true if the contents were changed
    public bool UpdateContentFromCharacter()
    {
#if UNITY_EDITOR
        //quick check that the button actually is bound to the character
        if (boundGearSlot == EquipmentSlots.COUNT)
        {
            Debug.LogError("Asking a button to draw from character equipment, but it is not bound to the character equipment.");      
        }
#endif
        HeroPC hero = GameMasterScript.heroPCActor;

        Equipment boundGear;
        if (boundGearSlot == EquipmentSlots.WEAPON && weaponHotbarIdx >= 0)
        {
            boundGear = UIManagerScript.hotbarWeapons[weaponHotbarIdx];
            if (boundGear == null)
            {
                UIManagerScript.hotbarWeapons[weaponHotbarIdx] = hero.myEquipment.defaultWeapon;
                boundGear = hero.myEquipment.defaultWeapon;
            }

        }
        else
        {
            boundGear = hero.myEquipment.equipment[(int)boundGearSlot];
        }

        if (boundGear == null)
        {
            ClearContainedData(true);
            if (txtLabel != null)
            {
                txtLabel.text = hero.myEquipment.GetGearNameForSlot(boundGearSlot);
            }
            //Don't return true if we have no data, as we don't want to flash nothing
            return false;
        }

        // Make sure the name of item updates if it was upgraded via dreamcaster, or otherwise received a mod
        if (containedData != boundGear || boundGear.CheckNameDirty())
        {
            boundGear.SetNameDirty(false);
            SetContainedData(boundGear, true);
            return true;
        }

        //nothing changed
        return false;
    }

    void Initialize()
    {
        if (bInitialized)
        {
            return;
        }

        if (txtLabel != null)
        {
            FontManager.LocalizeMe(txtLabel, TDFonts.WHITE); // #todo - Is this the right font?
            // We're going to let LocalizeMe handle everything here rather than mess with materials

            //txtLabel.fontMaterial = new Material(txtLabel.fontMaterial);
        }
        imageBackgroundSprite = GetComponent<Image>();

        dontSetColorOnStart = true;
        bInitialized = true;
    }

    public void MoveCursorToNeighbor(int dir)
    {
        //if our column has rules for us, use those
        if (myButtonColumn != null)
        {
            myButtonColumn.MoveCursorToNeighbor( this, dir);
            return;
        }

        if (myUIObject != null)
        {
            myUIObject.MoveCursorToNeighbor(dir);
        }
    }

    public void TryPlaceContentInTooltip(int iValue)
    {
        UIManagerScript.TryShowObjectInFullscreenUI(GetContainedData());
    }

    //Function is cosmetic and does not change any game state information
    public void ToggleButton(bool bPressed)
    {
        Initialize();

        bToggled = bPressed;

        imageBackgroundSprite.sprite = bToggled ? spriteButtonClicked : spriteButtonUnclicked;

        if (txtLabel != null)
        {
            FontManager.LocalizeMe(txtLabel, bToggled ? TDFonts.WHITE : TDFonts.BLACK);
            // LocalizeMe is a simpler way of switching the button font color as needed, since we have these pre-baked
            // with proper underlay values as needed.

            /* Color fontColor = bToggled ? colorFontClicked : colorFontUnclicked;
            Color shadowColor = bToggled ? colorFontUnclicked : colorFontClicked;

            txtLabel.fontMaterial.SetColor("_FaceColor", fontColor);
            txtLabel.fontMaterial.SetColor("_UnderlayColor", shadowColor);

            Debug.Log(gameObject.name + " state is " + bPressed + " and color changed. " + txtLabel.fontMaterial.GetColor("_FaceColor") + " " + txtLabel.font.ToString()); */
        }

    }

    public void SubmitWasCalled(int idx, Action<int> action)
    {
        
    }


    //This cancels any tween running on this button, 
    //restores our original position, then
    //records our original position and moves us to a new position
    //based on the offset. 
    public void PrepareForTween(Vector2 vInitialOffset)
    {
        RectTransform rt = gameObject.transform as RectTransform;
        if (LeanTween.isTweening(rt))
        {
            LeanTween.cancel(rt);
            rt.anchoredPosition = vStoredAnchorPositionPreTween;
        }

        vStoredAnchorPositionPreTween = rt.anchoredPosition;
        rt.anchoredPosition = vStoredAnchorPositionPreTween + vInitialOffset;
    }


    //Changes the cosmetic values, and then runs game code
    public void SubmitFunction_OnClickOrPress( int iInputType )
    {
        if (ToggleWhenActivated)
        {
            ToggleButton(!bToggled);
        }

        //run Tangledeep code
        if (onClickAction != null)
        {
            // Try playing a tock here for ~ juice ~
            UIManagerScript.PlayCursorSound("UITock");
            onClickAction(iSubmitValue, (ELastInputSource)iInputType);
        }

    }

    public void Default_OnClick(BaseEventData bte)
    {
        PointerEventData ped = bte as PointerEventData;
        if (ped != null && ped.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        //Allow for clicks to happen when we're dragging an object, because that means there is a fakey drop.
        if (UIManagerScript.draggingGenericObject != null || UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            SubmitFunction_OnClickOrPress((int) ELastInputSource.mouse);
        }
    }

    //Drag and drop always use Mouse as a source.
    public void Default_OnDrag(BaseEventData bte)
    {
        if (UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            if (onDrag != null)
            {
                onDrag(iSubmitValue, ELastInputSource.mouse);
            }
        }
    }

    //Drag and drop always use Mouse as a source.
    public void Default_OnDrop(BaseEventData bte)
    {
        if (onDrop != null)
        {
            onDrop(iSubmitValue, ELastInputSource.mouse);
        }
    }

    public void Default_OnPointerEnter(BaseEventData bte)
    {
        if (UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            if (onPointerEnter != null)
            {
                onPointerEnter(iSubmitValue, bte == null ? ELastInputSource.keyboard_or_gamepad : ELastInputSource.mouse);
            }
        }
    }

    public void Default_OnPointerExit(BaseEventData bte)
    {
        if (UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            if (onPointerExit != null)
            {
                onPointerExit(iSubmitValue, ELastInputSource.mouse);
            }
        }
    }

    public void Default_Cancel(BaseEventData bte)
    {
        if (onCancel != null)
        {
            onCancel(iSubmitValue, bte == null ? ELastInputSource.keyboard_or_gamepad : ELastInputSource.mouse);
        }
    }


    //todo change this to a generic drag function, like the one above
    public void DragInvItem_Switch(BaseEventData bte)
    {
        UIManagerScript.singletonUIMS.DragInvItem(myID);
    }

    public void SetEventListener(EventTriggerType eType, UnityAction<BaseEventData> action)
    {
        //set listeners for new button
        EventTrigger et = gameObject.GetComponent<EventTrigger>();

        //Mouseover start
        EventTrigger.Entry newEntry = new EventTrigger.Entry();
        newEntry.eventID = eType;
        newEntry.callback.AddListener(action);
        et.triggers.Add(newEntry);

    }

    public void SetActionOnReceiveEvent(EventTriggerType eventType, Action<int[], ELastInputSource> action)
    {
        switch (eventType)
        {
            case EventTriggerType.BeginDrag:
                onDrag = action;
                break;
            case EventTriggerType.PointerClick:
                onClickAction = action;
                break;
            case EventTriggerType.PointerEnter:
                onPointerEnter = action;
                break;
            case EventTriggerType.PointerExit:
                onPointerExit = action;
                break;
            case EventTriggerType.Drop:
                onDrop = action;
                break;
        }

    }

    public TextMeshProUGUI GetTMPro()
    {
        return txtLabel;
    }

    public void SetDisplayText(string strDisplayName)
    {
        if (txtLabel != null)
        {
            txtLabel.text = strDisplayName;
        }
    }

    public void ClearDisplayText()
    {
        if (txtLabel != null)
        {
            txtLabel.text = "";
        }
    }
}
