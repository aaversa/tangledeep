using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class UIGenericItemTooltip : MonoBehaviour
{
    [Header("Base Tooltip Info")]
    public TextMeshProUGUI textItemInfo;
    public TextMeshProUGUI textItemName;
    public TextMeshProUGUI textComparisonArea;
    public Image           imageItemSprite;

    [Header("Border Info")]
    public Sprite          spriteDefaultBorder;
    private bool           bIsDisplayingBorder;


    [Header("Dynamic Submenu Buttons")]
    public GameObject prefab_submenuButton;
    public VerticalLayoutGroup layoutGroup;
    public int PixelsBetweenSubmenuButtons;

    private bool bSubmenuActivated;
    private bool bActive;
    private Item targetItem;
    private Item compareItem;

    private List<Switch_InvItemButton> listDynamicButtons;

    //funsies section
    [HideInInspector]
    public LTDescr tweenEQItemBounce;
    [HideInInspector]
    public Vector2 vBounceItemOrigin;

    private Transform tformOldCursorParent;
    private UIManagerScript.UIObject uiObjectOldCursorObject;

    private Action onCloseSubmenuAction;


    //If the tooltip is showing a weapon from the hotbar, keep track of that index
    [HideInInspector]
    public int iWeaponHotbarIdx;

    // Use this for initialization
    void Awake()
    {
        prefab_submenuButton.SetActive(false);
    }

    void Start ()
    {
        FontManager.LocalizeMe(textItemInfo, TDFonts.WHITE);
        FontManager.LocalizeMe(textItemName, TDFonts.WHITE);
        FontManager.LocalizeMe(textComparisonArea, TDFonts.WHITE);
    }
	
	// Update is called once per frame
	void Update ()
    {
        //if the submenu is being shown
        //but the cursor has moved away from us for whatever reason
        //we should hide the submenu
        if (bSubmenuActivated)
        {
            /*
            UIManagerScript.UIObject currentObject = UIManagerScript.uiObjectFocus;
            if (listDynamicButtons.FindAll(ldb => ldb.myUIObject == currentObject).Count == 0)
            {
                //huh!
                HideSubmenu();
            }
            */
        }
    }

    public bool CheckIfActive()
    {
        return bActive;
    }

    public void Show()
    {
        if (bActive)
        {
            return;
        }

        bActive = true;
        HideSubmenu();
        layoutGroup.gameObject.SetActive(true);

    }

    public void Hide()
    {
        if (!bActive)
        {
            return;
        }

        bActive = false;
        HideSubmenu();
        layoutGroup.gameObject.SetActive(false);
    }

    //Steals the cursor from wherever it lived, and attaches it to us
    void TakeCursor(UIManagerScript.UIObject newFocusObject)
    {
        GameObject cursor = UIManagerScript.singletonUIMS.uiDialogMenuCursor;
        if (cursor == null)
        {
            //uh
            return; //?
        }

        //if we already control it, don't change the old parent
        if (tformOldCursorParent != layoutGroup.transform)
        {
            tformOldCursorParent = cursor.transform.parent;
            uiObjectOldCursorObject = UIManagerScript.uiObjectFocus;
        }

        //now it belongs to us
        cursor.transform.SetParent(layoutGroup.transform.parent);
        UIManagerScript.ChangeUIFocusAndAlignCursor(newFocusObject);
    }

    public bool HasTakenCursor()
    {
        GameObject cursor = UIManagerScript.singletonUIMS.uiDialogMenuCursor;
        return cursor != null && cursor.transform.parent == layoutGroup.transform.parent;
    }

    public void ReleaseCursor()
    {
        GameObject cursor = UIManagerScript.singletonUIMS.uiDialogMenuCursor;

        //we should only release the cursor if we actually have it. Perhaps it got moved over to a modal dialog?
        if (!HasTakenCursor())
        {
            return;
        }

        if (tformOldCursorParent != null)
        {
            cursor.transform.SetParent(tformOldCursorParent);
            tformOldCursorParent = null;
        }
        if (uiObjectOldCursorObject != null)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(uiObjectOldCursorObject);
            uiObjectOldCursorObject = null;
        }

        Image border = layoutGroup.GetComponent<Image>();
        border.enabled = bIsDisplayingBorder;
        border.sprite = spriteDefaultBorder;

    }

    public void ShowSubmenu()
    {
        bSubmenuActivated = true;

        //hide text info
        textItemInfo.gameObject.SetActive(false);
        textComparisonArea.gameObject.SetActive(false);

        //display buttons
        if (listDynamicButtons != null)
        {
            foreach (Switch_InvItemButton btn in listDynamicButtons)
            {
                btn.gameObject.SetActive(true);
                FontManager.LocalizeMe(btn.txtLabel, TDFonts.BLACK);
            }

            TakeCursor(listDynamicButtons[0].myUIObject);
        }


    }

    public void HideSubmenu()
    {
        bSubmenuActivated = false;

        ClearSubmenu();
        ReleaseCursor();

        //show text info
        textItemInfo.gameObject.SetActive(true);
        textComparisonArea.gameObject.SetActive(true);

        iWeaponHotbarIdx = -1;

        if (onCloseSubmenuAction != null)
        {
            onCloseSubmenuAction();
        }
        onCloseSubmenuAction = null;

    }

    //remove all settings for dynamic buttons
    public void ClearSubmenu()
    {
        //hide buttons
        if (listDynamicButtons != null)
        {
            foreach (Switch_InvItemButton btn in listDynamicButtons)
            {
                btn.gameObject.SetActive(false);
                Destroy(btn.gameObject);
            }

            listDynamicButtons.Clear();
        }
        else
        {
            listDynamicButtons = new List<Switch_InvItemButton>();
        }
    }

    public void ClearAllItems()
    {
        targetItem = null;
        compareItem = null;

        //clean up sprites
        imageItemSprite.enabled = false;
        imageItemSprite.sprite = null;

        //clean up text
        textItemName.text = "";
        textItemInfo.text = "";
        textComparisonArea.text = "";

    }

    public void SetItem(Item newItem, bool bBounceItem = true)
    {
        // 1/17 moved this outside because why wouldn't we always do this
        //imageItemSprite.sprite = UIManagerScript.LoadSpriteFromAtlas(UIManagerScript.allItemGraphics, newItem.spriteRef);
        imageItemSprite.sprite = newItem.GetSpriteForUI();

        if (newItem != targetItem)
        {
            targetItem = newItem;
            //assign sprite
            imageItemSprite.enabled = true;
            
            //yay fun
            if (bBounceItem)
            {
                ItemBounce(0.75f, 4);
            }
        }

        //assign text even if the item hasn't changed,
        //as the item info may have changed
        textItemName.text = newItem.displayName;
        textItemInfo.text = newItem.GetItemInformationNoName(true);
        UpdateComparisonText();

    }

    public void ClearComparison()
    {
        compareItem = null;
        textComparisonArea.text = "";
    }

    public void SetComparisonItem(Item compareToMe)
    {
        compareItem = compareToMe;
        UpdateComparisonText();
    }

    public void SetPosition(Vector2 vPos)
    {
        RectTransform rtParent = layoutGroup.transform.parent as RectTransform;
        rtParent.anchoredPosition = vPos;
    }

    public void SetPosition(RectTransform rtTopLeft, RectTransform rtTopRight)
    {
        float fDelta = rtTopRight.anchoredPosition.x - rtTopLeft.anchoredPosition.x;
        SetWidth(fDelta);

        SetPosition(rtTopLeft.anchoredPosition);
    }



    public void SetWidth(float px)
    {
        //set width of parent and self
        RectTransform layoutRT = layoutGroup.transform as RectTransform;
        
        Vector2 vSize = layoutRT.sizeDelta;
        vSize.x = px;
        layoutRT.sizeDelta = vSize;

        RectTransform parentRT = layoutRT.transform.parent as RectTransform;
        parentRT.sizeDelta = vSize;

        //also resize the Name box, since it doesn't
        //auto-size with the rest of the tooltip.
        //Allow a buffer on each side of the box
        textItemName.rectTransform.sizeDelta = new Vector2( px - 8, textItemName.rectTransform.sizeDelta.y);
    }

    void UpdateComparisonText(Item newCompare = null)
    {
        if (newCompare != null)
        {
            compareItem = newCompare;
        }

        if (compareItem == null ||
            targetItem == null ||
            targetItem == compareItem)
        {
            textComparisonArea.text = "";
            return;
        }

        //Note: The slot parameter is not actually used
        string compareString = EquipmentBlock.CompareItems(compareItem as Equipment, targetItem as Equipment, EquipmentSlots.ANY);

        if (compareString != "" && compareString != "\n")
        {
            textComparisonArea.text = "\n" + UIManagerScript.cyanHexColor + StringManager.GetString("ui_replacing") + "</color> " + compareItem.displayName + ":\n" + compareString;
        }
    }

    public Switch_InvItemButton AddDynamicButton(string strText, Action<int[], Switch_InvItemButton.ELastInputSource> actionOnSelect, int[] valueOnSelect)
    {
        //Create a new object and position it at the start
        GameObject go = Instantiate(prefab_submenuButton, layoutGroup.transform);

        RectTransform rt = go.transform as RectTransform;

        Switch_InvItemButton newButton = go.GetComponent<Switch_InvItemButton>();
        if (actionOnSelect != null)
        {
            newButton.onClickAction = actionOnSelect;
            newButton.iSubmitValue = valueOnSelect;
        }

        //move it down the list based on how many we already have
        int yOffset = (int)rt.sizeDelta.y + (PixelsBetweenSubmenuButtons * listDynamicButtons.Count);

        Vector2 vPos = rt.localPosition;
        vPos.y += yOffset;
        rt.localPosition = vPos;

        //set the text on the button
        if (newButton.txtLabel != null)
        {
            newButton.txtLabel.text = strText;
            FontManager.LocalizeMe(newButton.txtLabel, TDFonts.BLACK); // Always black, the buttons are never toggled.
            newButton.dontSetColorOnStart = true; // overrides the button's default start/awake color behavior
        }

        //create a UI Object for this button
        UIManagerScript.UIObject newUIObject = new UIManagerScript.UIObject();
        newButton.myUIObject = newUIObject;
        newUIObject.gameObj = newButton.gameObject;
        newUIObject.mySubmitFunction = newButton.SubmitFunction_OnClickOrPress;


        //assign north neighbor
        //and assign self to north neighbor as south neighbor
        if (listDynamicButtons.Count > 0)
        {
            Switch_InvItemButton northNeighborButton = listDynamicButtons[listDynamicButtons.Count - 1];
            UIManagerScript.UIObject northNeighborUIObject = northNeighborButton.myUIObject;

            newUIObject.neighbors[(int) Directions.NORTH] = northNeighborUIObject;
            northNeighborUIObject.neighbors[(int) Directions.SOUTH] = newUIObject;
        }

        //put ourselves on the list
        listDynamicButtons.Add(newButton);

        //make sure we respond to action
        newButton.SetEventListener(EventTriggerType.PointerClick, newButton.Default_OnClick);

        if (!bSubmenuActivated)
        {
            newButton.gameObject.SetActive(false);
        }

        return newButton;
    }

    //Used to make an item icon pop
    public void ItemBounce(float fBounceTime, int iBouncePX)
    {
        RectTransform rt = imageItemSprite.rectTransform;

        //reset the item if bouncing currently 
        if (vBounceItemOrigin != Vector2.zero)
        {
            LeanTween.cancel(rt);
            rt.anchoredPosition = vBounceItemOrigin;
        }

        //Store the data and start bouncing
        vBounceItemOrigin = rt.anchoredPosition;
        Vector2 vChangePos = rt.anchoredPosition;
        vChangePos.y -= iBouncePX;
        rt.anchoredPosition = vChangePos;

        tweenEQItemBounce = LeanTween.moveY(rt, vBounceItemOrigin.y, fBounceTime);
        tweenEQItemBounce.setEaseOutElastic().setOvershoot(iBouncePX);
    }

    public void DisplayBorder(bool bShouldDisplay)
    {
        bIsDisplayingBorder = bShouldDisplay;
        layoutGroup.GetComponent<Image>().enabled = bShouldDisplay;
    }

    public Item GetTargetItem()
    {
        return targetItem;
    }

    public void SetActionCloseSubmenu(Action a)
    {
        onCloseSubmenuAction = a;
    }

    #region UI Callbacks

    public void RemoveWeaponFromHotbar(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];
        Item removeMe = GameMasterScript.heroPCActor.GetItemByID(itemID);

        //Don't allow fists to be removed from bar
        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(removeMe, onlyActualFists: true))
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            if (UIManagerScript.hotbarWeapons[i] == removeMe)
            {
                UIManagerScript.hotbarWeapons[i] = null;
            }
        }
        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);

    }

    public void SwitchToHotbarWeapon(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        UIManagerScript.SwitchActiveWeaponSlot(iWeaponHotbarIdx, false); // before, this was just SETTING the weapon slot        
        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);

    }

    //The player selected an item and pushed an equip hotkey, so we have to make some educated guesses
    public void EquipItemAndGuessBestSlot(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (iWeaponHotbarIdx >= 0)
        {
            UIManagerScript.SwitchActiveWeaponSlot(iWeaponHotbarIdx, true); // before, this was just SETTING the weapon slot
        }

        int itemID = args[0];
        Item equipme = GameMasterScript.heroPCActor.GetItemByID(itemID);
        EquipmentBlock heroStuff = GameMasterScript.heroPCActor.myEquipment;
        bool bShiftIsDown = TDInputHandler.IsCompareAlternateButtonHeld();

        if (equipme is Armor)
        {
            EquipItem( new [] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ARMOR_SLOT }, inputSource);
            return;
        }
        if (equipme is Emblem)
        {
            EquipItem(new[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_EMBLEM_SLOT }, inputSource);
            return;
        }
        if (equipme is Accessory)
        {
            //If we aren't pressing shift, and our first slot is empty or the second slot is not, equip in the first slot
            if (!bShiftIsDown &&
                (heroStuff.GetEquipmentInSlot(EquipmentSlots.ACCESSORY) == null ||
                heroStuff.GetEquipmentInSlot(EquipmentSlots.ACCESSORY2) != null))
            {
                EquipItem(new[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1 }, inputSource);
            }
            //Otherwise, equip in the second slot
            else
            {
                EquipItem(new[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2 }, inputSource);
            }
            return;
        }
        if (equipme is Offhand)
        {
            EquipItem(new[] { itemID, (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT }, inputSource);
            return;
        }
        if (equipme is Weapon)
        {
            //find the first empty weapon slot and put it there,
            //and if there isn't one just put it in the first slot
            int iBestIndex = 0;
            for (int t = 0; t < 4; t++)
            {
                Weapon checkWeapon = UIManagerScript.hotbarWeapons[t];
                if( UIManagerScript.hotbarWeapons[t] == null ||
                    heroStuff.IsDefaultWeapon(checkWeapon, onlyActualFists: true))
                {
                    iBestIndex = t;
                    break;
                }
            }

            EquipItem(new [] { itemID, iBestIndex}, inputSource );
        }
    }

    public void PairWithWeapon(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];
        Equipment pairMe = GameMasterScript.heroPCActor.GetItemByID(itemID) as Equipment;

        //if this is not a weapon, we can't do this.
        if (pairMe == null || !pairMe.IsOffhandable())
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        //what weapon is currently equipped?
        Weapon currentEquip = GameMasterScript.heroPCActor.myEquipment.GetWeapon();

        //todo: perhaps prevent this button from being clicked at all if this is the case.
        if (currentEquip == null || currentEquip == pairMe)
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        // Wait what if this is already paired?

        Weapon playerEquippedWeapon = GameMasterScript.heroPCActor.myEquipment.GetWeapon();

        bool unpairItem = false;
        foreach (EQPair eqp in pairMe.pairedItems)
        {
            if (eqp.eq == playerEquippedWeapon)
            {
                Debug.Log("This item is already paired with this weapon, so unpair it.");                
                unpairItem = true;
                break;
            }
        }

        StringManager.SetTag(0, pairMe.displayName);
        StringManager.SetTag(1, currentEquip.displayName);

        if (unpairItem) // Already paired? Well, unpair 'em both from each other.
        {
            playerEquippedWeapon.RemovePairedItemByRef(pairMe);
            pairMe.RemovePairedItemByRef(playerEquippedWeapon);
            UIManagerScript.PlayCursorSound("Cancel");
            GameLogScript.GameLogWrite(StringManager.GetString("log_unpairoffhand"), GameMasterScript.heroPCActor);
        }
        else
        {
            //Bind them toegther in a vow of violence
            pairMe.PairWithItem(currentEquip, false, true);
            UIManagerScript.PlayCursorSound("Equip Item");

            //And seal this truth in the combat log
            GameLogScript.GameLogWrite(StringManager.GetString("log_pairoffhand"), GameMasterScript.heroPCActor);
        }

        //Flash items? Do a thing?
        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
        ClearAllItems();

        //I'm pointing at something new now
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);
    }

    public void EquipItem(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];
        int slotIDX = args[1];

        Item equipme = GameMasterScript.heroPCActor.GetItemByID(itemID);
        if (equipme == null || UIManagerScript.uiObjectFocus == null) // 4112019 - should never happen, and yet.
        {
            return;
        }

        bool weaponHotbarInteraction = false;

        if (slotIDX >= (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1 && slotIDX <= (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4)
        {
            UIManagerScript.AddWeaponToActiveSlot(equipme as Weapon, slotIDX - (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1);
                        
            UIManagerScript.SwitchActiveWeaponSlot(slotIDX, true);
            weaponHotbarInteraction = true;
        }

        if (!weaponHotbarInteraction && equipme.itemType != ItemTypes.WEAPON && GameMasterScript.heroPCActor.WouldChangingEquipmentFailQuest() 
        && GameMasterScript.gmsSingleton.ReadTempGameData("equip_confirm_fail") != 1)
        {            
            UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
            UIManagerScript.ForceCloseFullScreenUIWithNoFade(true);
            UIManagerScript.StartConversationByRef("dialog_confirm_changegear_failrumor", DialogType.STANDARD, null);           
            return;
        }           
        GameMasterScript.gmsSingleton.SetTempGameData("equip_confirm_fail", 0);        

        //This function takes 1 as a subslot if you are equipping... 
        //  Accessory 2
        //  Offhand
        //
        int iSlot = 0;
        if (slotIDX == (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2 ||
            slotIDX == (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT)
        {
            iSlot = 1;
        }

        //Debug.Log("Player request equip: " + equipme.actorRefName + " " + equipme.actorUniqueID + " in subslot " + iSlot);

        // This new function below will verify that we are not illegally equipping an offhand with a 2h mainhand equipped

        bool successEquip = GameMasterScript.heroPCActor.myEquipment.EquipOnlyIfValid(equipme as Equipment, SND.PLAY, iSlot, true);


        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();

        //I'm pointing at something new now
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);

        UIGenericItemTooltip currentTooltip = UIManagerScript.singletonUIMS.GetCurrentFullScreenUI().GetTooltipIfActive();
        //currentTooltip.SetItem() 

        if (!weaponHotbarInteraction && successEquip)
        {
            UIManagerScript.singletonUIMS.CheckIfPassTurnFromEquipping();
        }
    }

    public void DropItem(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];

        Item dropMe = GameMasterScript.heroPCActor.GetItemByID(itemID);

        if (dropMe == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(dropMe, onlyActualFists: true))
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        UIManagerScript.DropItemFromSheet(dropMe);        
        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
        //ClearAllItems();

        //I'm pointing at something new now
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);
        
    }

    public void UnequipItem(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];

        Equipment unequipMe = GameMasterScript.heroPCActor.GetItemByID(itemID) as Equipment;

        if (unequipMe == null) return;

        GameMasterScript.heroPCActor.Unequip(unequipMe, true);


        if (unequipMe.itemType == ItemTypes.WEAPON)
        {
            Weapon unequipWeap = unequipMe as Weapon;
            if (UIManagerScript.IsWeaponInHotbar(unequipWeap))
            {
                UIManagerScript.RemoveWeaponFromActives(unequipWeap);
            }
        }

        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
        ClearAllItems();

        //I'm pointing at something new now
        UIManagerScript.singletonUIMS.HoverItemInfo(UIManagerScript.uiObjectFocus.onSubmitValue);
    }


    public void MarkItemFavorite(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];

        Item bestItem20XX = GameMasterScript.heroPCActor.GetItemByID(itemID);
        bestItem20XX.favorite = !bestItem20XX.favorite;

        Switch_InvItemButton btn = UIManagerScript.GetButtonContainingThisObjectFromFullScreenUI(bestItem20XX);

        if (btn == null || btn.myUIObject == null)
        {
            return;
        }

        TutorialManagerScript.markedItemAsFavoriteOrTrash = true;

        UIManagerScript.UIObject objWithItem = btn.myUIObject;
        if (bestItem20XX.favorite)
        {
            bestItem20XX.vendorTrash = false;            
            UIManagerScript.PlayCursorSound("GetSparkle");
            if (objWithItem != null)
            {
                StartCoroutine(LerpTextColor(objWithItem.subObjectTMPro, Color.yellow, Color.white, 0.2f));
            }
        }
        else
        {
            UIManagerScript.PlayCursorSound("UITock");
            if (objWithItem != null)
            {
                StartCoroutine(LerpTextColor(objWithItem.subObjectTMPro, new Color(75f/255f, 0, 130f / 255f), Color.white, 0.4f));
            }
        }

        UIManagerScript.HideSubmenuAndUpdateFullScreenUIContent();
    }

    public void MarkItemTrash(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];

        Item itemToMark = GameMasterScript.heroPCActor.GetItemByID(itemID);
        itemToMark.vendorTrash = !itemToMark.vendorTrash;

        Switch_InvItemButton btn = UIManagerScript.GetButtonContainingThisObjectFromFullScreenUI(itemToMark);

        TutorialManagerScript.markedItemAsFavoriteOrTrash = true;

        if (btn == null || btn.myUIObject == null)
        {
            return;
        }        

        UIManagerScript.UIObject objWithItem = btn.myUIObject;
        if (itemToMark.vendorTrash)
        {
            itemToMark.favorite = false;
            UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("UITick");
            if (objWithItem != null)
            {
                StartCoroutine(LerpTextColor(objWithItem.subObjectTMPro, Color.yellow, Color.white, 0.2f));
            }
        }
        else
        {
            UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("UITock");
            if (objWithItem != null)
            {
                StartCoroutine(LerpTextColor(objWithItem.subObjectTMPro, new Color(75f / 255f, 0, 130f / 255f), Color.white, 0.4f));
            }
        }

        HideSubmenu();
        UIManagerScript.UpdateFullScreenUIContent();
    }

    public void EnterItemHotbarMode(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        //release cursor
        HideSubmenu();

        //move cursor to hotbar and put item in cursor
        UIManagerScript.FullScreenUIStartAssignInventoryItemToHotbarMode(args[0]);
    }

    public void UseItem(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        int itemID = args[0];
        Consumable c = GameMasterScript.heroPCActor.GetItemByID(itemID) as Consumable;
        if (c == null)
        {
            //some how we are UseItem on a not consumable

            //which may totally happen some day 
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        if (!PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
        {
            UIManagerScript.UnlockOptionsMenuToggle();
        }

        //close the inventory
        if (c.actorRefName != "item_tent")
        {
            UIManagerScript.ForceCloseFullScreenUI();
        }
        else
        {
            UIManagerScript.ForceCloseFullScreenUIWithNoFade();
        }
        
        GameMasterScript.gmsSingleton.PlayerUseConsumable(c);
    }

    public void EatItem(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        bool playerIsFull = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");
        if (playerIsFull)
        {
            //do not do a thing
            UIManagerScript.PlayCursorSound("Error");
            //don't hide the submenu or actually use the item either
            return;
        }

        //otherwise
        UseItem(args, inputSource);
    }

    public static IEnumerator LerpTextColor(TextMeshProUGUI txt, Color startColor, Color endColor, float fLifeTime)
    {
        if (txt == null)
        {
            yield break;
        }

        float fTime = 0;
        while (fTime < fLifeTime)
        {
            txt.color = Color.Lerp(startColor, endColor, fTime / fLifeTime);
            fTime += Time.deltaTime;
            yield return null;
        }
        txt.color = endColor;

    }
    #endregion
}

public partial class UIManagerScript
{
    static UIGenericItemTooltip genericItemTooltip;
    private static Func<GameObject, bool> funcShouldAllowCursorToFocusOnThis;

    //Allows us to have control over what buttons can be selected and when.
    //This allows us to prevent the mouse from selecting buttons all willy-nilly when the Submenu is open
    //rather than having to constantly check against a list of bools
    public static void SetFocusFunction(Func<GameObject, bool> newFunc)
    {
        funcShouldAllowCursorToFocusOnThis = newFunc;
    }

    public static void ClearFocusFunction()
    {
        funcShouldAllowCursorToFocusOnThis = null;
    }

    public static UIGenericItemTooltip ShowGenericItemTooltip(Item itam, Vector2 vCanvasPosition = default(Vector2))
    {
        InitGenericItemTooltip();

        //set item
        genericItemTooltip.SetItem(itam);

        //set position
        if (vCanvasPosition != Vector2.zero)
        {
            genericItemTooltip.SetPosition(vCanvasPosition);
        }

        //show        
        genericItemTooltip.Show();

        return genericItemTooltip;

    }

    public static void SetGenericTooltipPosition(RectTransform rtTopLeft, RectTransform rtTopRight)
    {
        genericItemTooltip.SetPosition(rtTopLeft, rtTopRight);
    }

    static void InitGenericItemTooltip()
    {
        //create the tooltip if it don't exist
        if (genericItemTooltip == null)
        {
            genericItemTooltip = Instantiate(Resources.Load<GameObject>("ShepPrefabs/prefab_genericItemTooltip"), playerHUD.transform).GetComponent<UIGenericItemTooltip>();
            genericItemTooltip.transform.SetAsLastSibling();
        }
    }

    public static UIGenericItemTooltip ShowGenericItemTooltip()
    {
        InitGenericItemTooltip();
        return genericItemTooltip;
    }

    public static void HideGenericItemTooltip()
    {
        genericItemTooltip.Hide();
    }

    //Shows different equip buttons based on item type
    public static void ShowGenericItemTooltipSubmenuForEquippable(int iButtonIndex)
    {

    }



    public static bool AllowCursorToFocusOnThis(GameObject obj)
    {
        //check any special functions we might have set by whatever UI is active
        if (funcShouldAllowCursorToFocusOnThis != null)
        {
            return funcShouldAllowCursorToFocusOnThis(obj);
        }

        return true;
    }

    public static bool TooltipHasCursor()
    {
        return genericItemTooltip != null && genericItemTooltip.HasTakenCursor();
    }

    public static void TooltipReleaseCursor()
    {
        if (genericItemTooltip != null)
        {
            genericItemTooltip.HideSubmenu();
        }
    }

    public static bool ObjectInTooltip(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            if (t == genericItemTooltip.transform)
            {
                return true;
            }
            t = t.parent;
        }

        return false;
    }

    public static object Debug_ShowGenericTooltip(string[] args)
    {
        genericItemTooltip.Show();
        return "done";
    }

    public static object Debug_HideGenericTooltip(string[] args)
    {
        genericItemTooltip.Hide();
        return "done";
    }

    public static object Debug_SetRandomItemGenericTooltip(string[] args)
    {
        //spawn a random item, leak into memory somewhere
        /*
        List<Item> allItems = GameMasterScript.masterItemList.Values.ToList();
        Item template = allItems[UnityEngine.Random.Range(0, allItems.Count)];
        Item newItem = Activator.CreateInstance(template.GetType()) as Item;

        newItem.CopyFromItem(template);
        newItem.SetUniqueIDAndAddToDict();

        GameMasterScript.heroPCActor.myInventory.AddItem(newItem, true);

        //test against the equipscreen anchor
        Switch_UIEquipmentScreen eqscreen = UIManagerScript.singletonUIMS.switch_UIEquipmentScreen;

        UIManagerScript.ShowGenericItemTooltip(newItem);
        UIManagerScript.genericItemTooltip.DisplayBorder(false);
        UIManagerScript.genericItemTooltip.SetPosition(eqscreen.TooltipAnchorObjectTopLeft.transform as RectTransform, eqscreen.TooltipAnchorObjectTopRight.transform as RectTransform);
        */

        return "OUT OF ORDER LOL COME AGAIN";
    }

    public static object Debug_GenericTooltipShowSubmenu(string[] args)
    {
        ShowGenericItemTooltipSubmenuForEquippable(0);
        return "woop";
    }

    public static object Debug_GenericTooltipHideSubmenu(string[] args)
    {
        genericItemTooltip.HideSubmenu();
        return "woop";
    }

    public static object Debug_GenericTooltipSetWidth(string[] args)
    {
        int px = Int32.Parse(args[1]);
        genericItemTooltip.SetWidth(px);

        return "new width == " + px;
    }




}
