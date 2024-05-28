using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Switch_InvVerticalHotbarButton : MonoBehaviour
{
    public Image[]          images;
    public TextMeshProUGUI  textName;
    public Sprite           defaultSprite;
    private Vector3         vTextLocalAnchor;

    [HideInInspector]
    public UIManagerScript.UIObject myUIObject;

    public int iHotbarButtonIndex;

    private int iActiveImageIdx = 0;
    
    [HideInInspector]
    public int ActiveImageIndex
    {
        get { return iActiveImageIdx; }
    }

    private Vector2 vActiveOffset;
    private Vector2 vInactiveOffset;

    //todo: replace these in short order (friday? sunday?)
    private Item[]          content_Item = new Item[2];
    private AbilityScript[] content_AbilityScript = new AbilityScript[2];
    
    private ISelectableUIObject[] content_GenericObject = new ISelectableUIObject[2];

    public float CursorXOffset = 48f;
    public float CursorYOffset = -18f;

    private bool bIsUIFocus;

    private Image imagePotentialObject;
    private Action actionSwapHotbars;

    // Use this for initialization
    void Start ()
    {
        int inactive = (iActiveImageIdx + 1) % 2;
	    vInactiveOffset = images[inactive].rectTransform.anchoredPosition;
	    vActiveOffset = images[iActiveImageIdx].rectTransform.anchoredPosition;

	    vTextLocalAnchor = textName.transform.localPosition;

        FontManager.LocalizeMe(textName, TDFonts.WHITE);

    }

    public ISelectableUIObject GetMyContent()
    {
        return content_GenericObject[iActiveImageIdx];        
        if (content_GenericObject[iActiveImageIdx] == null)
        {
            if (content_GenericObject[1 - iActiveImageIdx] == null)
            {
                return null;
            }
            else
            {
                return content_GenericObject[1 - iActiveImageIdx];
            }
        }
        return content_GenericObject[iActiveImageIdx];
    }

    public void SetSwapHotbarAction(Action swapalop)
    {
        actionSwapHotbars = swapalop;
    }

    public void UpdateInformation(bool bFirstUpdate = false)
    {
        for (int t = 0; t < 2; t++)
        {
            //On first update, set the colors directly
            //and that no sprite is drawn if the hotbar slot is empty
            if (bFirstUpdate)
            {
                images[t].color = images[t].color = t == iActiveImageIdx ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.25f);
                Image spriteChild = images[t].gameObject.transform.GetChild(0).GetComponent<Image>();
                spriteChild.color = images[t].color;

            }

            HotbarBindable hb = UIManagerScript.hotbarAbilities[iHotbarButtonIndex + (t * 8)];

            if (hb.ability != null)
            {
                SetContent(hb.ability, t);
            }
            else if (hb.consume != null)
            {
                SetContent(hb.consume, t);
            }
            else
            {
                ClearContent(t);
            }

        }

    }

    // Update is called once per frame
    void Update ()
    {
        if (!GameMasterScript.actualGameStarted)
        {
            return;
        }

        bool bSelected = myUIObject == UIManagerScript.uiObjectFocus;

        //if we've changed all of a sudden, do the changey things
        if (bSelected != bIsUIFocus)
        {
            UpdateDrawInfoForFocusChange(bSelected);
        }

        //if the object in the actual hotbar does not match our current object, do something
        HotbarBindable hb = UIManagerScript.hotbarAbilities[iHotbarButtonIndex + (iActiveImageIdx * 8)];

        if (hb.consume != content_GenericObject[iActiveImageIdx] &&
            hb.ability != content_GenericObject[iActiveImageIdx] )
        {
            //reset our content!
            UpdateInformation();
        }

    }

    void UpdateDrawInfoForFocusChange(bool bSelected)
    {
        bIsUIFocus = bSelected;

        //move the text box
        LeanTween.cancel(textName.rectTransform);
        Vector3 vOffset = textName.rectTransform.localPosition;
        vOffset.x = vTextLocalAnchor.x + (bIsUIFocus ? 48f : 0f);
        LeanTween.moveLocal(textName.gameObject, vOffset, 0.2f).setEaseOutCubic();

        //draw / don't draw the extra sprite renderer
        if (imagePotentialObject == null)
        {
            GameObject go = new GameObject();
            go.transform.SetParent(transform);
            imagePotentialObject = go.AddComponent<Image>();

            //move it off to the right a bit?
            go.transform.localPosition = new Vector3(72, 8, 0);
            imagePotentialObject.rectTransform.sizeDelta = new Vector2(48, 48);
        }

        imagePotentialObject.enabled = false;

        if (bIsUIFocus)
        {
            UpdateImageForCarriedObject();
        }
    }

    void UpdateImageForCarriedObject()
    {
        ISelectableUIObject data = UIManagerScript.GetHeldGenericObject();
        if (data == null)
        {
            return;
        }
        if (imagePotentialObject == null)
        {
            return;
        }

        imagePotentialObject.sprite = data.GetSpriteForUI();
        imagePotentialObject.enabled = true;
    }

    public void SetContent(Item setItem, int idx)
    {
        content_Item[idx] = setItem;
        content_AbilityScript[idx] = null;
        content_GenericObject[idx] = setItem;

        if (idx == ActiveImageIndex)
        {
            textName.text = content_Item[idx].displayName;
        }

        Image spriteChild = images[idx].gameObject.transform.GetChild(0).GetComponent<Image>();
        //spriteChild.sprite = UIManagerScript.LoadSpriteFromAtlas(UIManagerScript.allItemGraphics, setItem.spriteRef);
        spriteChild.sprite = setItem.GetSpriteForUI();

    }

    public void SetContent(AbilityScript abil, int idx)
    {
        content_AbilityScript[idx] = abil;
        content_Item[idx] = null;
        content_GenericObject[idx] = abil;

        if (idx == ActiveImageIndex)
        {
            textName.text = content_AbilityScript[idx].abilityName;
        }

        Image spriteChild = images[idx].gameObject.transform.GetChild(0).GetComponent<Image>();
        spriteChild.sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictUIGraphics, abil.iconSprite);

    }

    public void SetContent(ISelectableUIObject obj, int idx)
    {
        content_GenericObject[idx] = obj;
        if (idx == ActiveImageIndex)
        {
            textName.text = obj.GetNameForUI();
        }

        Image spriteChild = images[idx].gameObject.transform.GetChild(0).GetComponent<Image>();
        spriteChild.sprite = obj.GetSpriteForUI();

    }

    public void ClearContent(int idx)
    {
        content_AbilityScript[idx] = null;
        content_Item[idx] = null;
        content_GenericObject[idx] = null;

        if (idx == ActiveImageIndex)
        {
            textName.text = "";
        }
        Image spriteChild = images[idx].gameObject.transform.GetChild(0).GetComponent<Image>();
        spriteChild.sprite = defaultSprite;
    }

    public void SwitchActiveButton()
    {
        iActiveImageIdx = (iActiveImageIdx + 1) % images.Length;

        //fancy swoops
        for (int t = 0; t < images.Length; t++)
        {
            bool bIsActive = t == iActiveImageIdx;
            RectTransform rt = images[t].rectTransform;
            Vector2 vDest = bIsActive ? vActiveOffset : vInactiveOffset;
            LeanTween.moveX(rt, vDest.x, 0.5f).setEaseOutExpo();
            LeanTween.moveY(rt, vDest.y, 0.5f).setEaseInOutBack().setOvershoot(0.2f);
            LeanTween.color(rt, bIsActive ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.4f), 0.5f);

            if (!bIsActive)
            {
                rt.SetAsFirstSibling();
            }
        }

        UpdateInformation();
    }

    //find the next vertical hotbar button, and do nothing else
    //simply point at it
    public void MoveCursorToNeighbor(int dir)
    {
        UIManagerScript.UIObject nextObject = myUIObject.neighbors[dir];

        if (nextObject != null )
        {
            Switch_InvVerticalHotbarButton nextButton = nextObject.gameObj.GetComponent<Switch_InvVerticalHotbarButton>();            
            //set the cursor off to the right
            if (nextButton != null)
            {
                nextButton.SetFocusOnMeForHotbarSlotting();
                UIManagerScript.PlayCursorSound("Move");
            }
            else
            {
                if (UIManagerScript.AllowCursorToFocusOnThis(nextObject.gameObj))
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(nextObject);
                }
            }
        }
    }

    //L/R on the dpad when highlit
    public void SwapVerticalHotbars(int dir)
    {
        if (actionSwapHotbars != null)
        {
            actionSwapHotbars();
        }
    }

    public void OnPointerEnter(BaseEventData bte)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject)) // Switch_UIInventoryScreen.CursorOwnershipState != EImpactUICursorOwnershipState.vertical_hotbar_has_cursor)
        {
            return;
        }

        SetFocusOnMeForHotbarSlotting();
    }

    public void SetFocusOnMeForHotbarSlotting()
    {
        ImpactUI_Base currentUIScreen = UIManagerScript.singletonUIMS.GetCurrentFullScreenUI();
        if (currentUIScreen != null)
        {
            ISelectableUIObject myContent = GetMyContent();
            if (myContent != null)
            {
                currentUIScreen.DisplayItemInfo(myContent, gameObject, false);
            }
            else
            {
                // Ideally we should clear the tooltip
            }
        }
        //focus on meeee
        UIManagerScript.TooltipReleaseCursor();
        UIManagerScript.ChangeUIFocusAndAlignCursor(myUIObject, CursorXOffset, CursorYOffset);
        CursorBounce cb = UIManagerScript.singletonUIMS.uiDialogMenuCursorBounce;
        cb.SetFacing(Directions.WEST);
    }

    public void OnSelectAction_FocusOnMe(int i)
    {
        SetFocusOnMeForHotbarSlotting();
    }

    public void OnClick(BaseEventData bte)
    {
        //Wait! This might be a fakeydrop
        if ( UIManagerScript.CheckForFakeyDrop())
        {
            return;
        }

        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        OnSubmit(iHotbarButtonIndex);
    }

    public void OnDrop(BaseEventData bte)
    {
        SetContentAndAddToHotbar(UIManagerScript.GetHeldGenericObject());
        UIManagerScript.singletonUIMS.ExitDragMode();
    }

    public void OnSubmit(int iValue)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject)) // Switch_UIInventoryScreen.CursorOwnershipState != EImpactUICursorOwnershipState.vertical_hotbar_has_cursor)
        {
            return;
        }

        //if the button is pressed and there is nothing being moved around, then pick this up
        if (UIManagerScript.GetHeldGenericObject() == null )
        {
            //set the data
            UIManagerScript.SetHeldGenericObject(content_GenericObject[iActiveImageIdx]);

            //empty ourselves
            ClearContent(iActiveImageIdx);

            //begin focusing on us
            SetFocusOnMeForHotbarSlotting();

            //Draw the object we're carrying now
            UpdateImageForCarriedObject();

            return;
        }

        //otherwise, whatever is held by the UI is going to be place here, annnnd
        //if we have any conent, the UI should pick it up
        ISelectableUIObject possibleSwapolio = content_GenericObject[iActiveImageIdx];

        //Put the stuff held by the UI into us, and...
        SetContentAndAddToHotbar(UIManagerScript.GetHeldGenericObject());

        if (possibleSwapolio != null)
        {
            //stay in gamepad update mode
            UIManagerScript.SetHeldGenericObject(possibleSwapolio);
            SetFocusOnMeForHotbarSlotting();
            UpdateImageForCarriedObject();
        }
    }

    public void OnDrag(BaseEventData bte)
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

        //place the ISelectableUIObject inside us into a containedData value in the UIManager
        UIManagerScript.BeginDragGenericObject(content_GenericObject[iActiveImageIdx], gameObject);

        //remove from hotbar
        UIManagerScript.ClearHotbar(iHotbarButtonIndex + (iActiveImageIdx * 8));

        //clear out our sprite and name
        ClearContent(iActiveImageIdx);

    }

    public void SetContentAndAddToHotbar(ISelectableUIObject newContent)
    {
        Item assignItem = newContent as Item;
        AbilityScript assignAbilityScript = newContent as AbilityScript;
        JobAbility assignJobAbility = newContent as JobAbility;
        
        //Do what we must do
        if (assignItem != null)
        {
            //only accept consumables that can be used
            if (!assignItem.CanBeUsed())
            {
                UIManagerScript.PlayCursorSound("Error");
                UIManagerScript.singletonUIMS.ExitDragMode();
                return;
            }

            //otherwise, hooray
            SetItemAndAddToHotbar(assignItem);
            UIManagerScript.singletonUIMS.ExitDragMode();
        }
        else if (assignAbilityScript != null)
        {
            SetAbilityAndAddToHotbar(assignAbilityScript);
            UIManagerScript.singletonUIMS.ExitDragMode();
        }
        else if (assignJobAbility != null)
        {
            if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(assignJobAbility.abilityRef))
            {
                newContent = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(assignJobAbility.abilityRef);
                SetAbilityAndAddToHotbar((AbilityScript)newContent);
            }
        }

        if (UIManagerScript.AddContentToSlot(newContent, (iActiveImageIdx * 8) + iHotbarButtonIndex))
        {
            OnSuccessfulSlotObject();
        }
        else
        {
            UIManagerScript.PlayCursorSound("Error");
        }

        //remove the object from the UIManager, success or fail
        UIManagerScript.ClearAllHeldGenericObjects();
    }
    
    private void SetItemAndAddToHotbar(Item setItem)
    {
        UIManagerScript.AddItemToSlot(setItem as Consumable, (iActiveImageIdx * 8) + iHotbarButtonIndex, false);
        OnSuccessfulSlotObject();
    }

    private void SetAbilityAndAddToHotbar(AbilityScript ab)
    {
        UIManagerScript.AddAbilityToSlot(ab, (iActiveImageIdx * 8) + iHotbarButtonIndex, true);

        //dupes have been removed!
        OnSuccessfulSlotObject();

    }

    private void OnSuccessfulSlotObject()
    {
        UpdateInformation();

        //return to normal inventory state
        UIManagerScript.OnSuccessfulSlotObject();

        //we are no longer selected, return to our position and also flash a bit
        UpdateDrawInfoForFocusChange(false);
        StartCoroutine(ImpactUI_Base.FadeTextColor(Color.yellow, Color.white, textName, 0.2f));

        //pulse border box
        GameObject childOfImage = images[iActiveImageIdx].transform.GetChild(1).gameObject;
        Image borderImage = childOfImage.GetComponent<Image>();
        borderImage.color = Color.yellow;
        LeanTween.color(borderImage.rectTransform, Color.white, 0.2f);

        borderImage.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
        LeanTween.scale(borderImage.rectTransform, new Vector3(1f, 1f, 1f), 0.2f).setEaseInOutBounce();

    }


}
