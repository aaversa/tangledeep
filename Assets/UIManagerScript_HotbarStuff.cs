using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerScript
{


    public static void UpdateHotbarOnSkillSheet()
    {
        for (int i = 0; i < skillHotbar.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.NOTHING)
            {
                skillHotbar[i].gameObj.GetComponent<Image>().sprite = transparentSprite;
            }
            else
            {
                if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
                {
                    //skillHotbar[i].gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allItemGraphics, hotbarAbilities[i].consume.spriteRef);
                    skillHotbar[i].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictItemGraphics, hotbarAbilities[i].consume.spriteRef);
                }
                else
                {
                    //skillHotbar[i].gameObj.GetComponent<Image>().sprite = LoadSpriteFromAtlas(allUIGraphics, hotbarAbilities[i].ability.iconSprite);
                    skillHotbar[i].gameObj.GetComponent<Image>().sprite = LoadSpriteFromDict(dictUIGraphics, hotbarAbilities[i].ability.iconSprite);
                }
            }
        }
    }

    public static void UpdateInventoryHotbar()
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            Image impComp = invHotbar[i].gameObj.GetComponent<Image>();
            Sprite nSprite = null;
            if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
            {
                nSprite = LoadSpriteFromDict(dictUIGraphics, hotbarAbilities[i].ability.iconSprite);
                impComp.sprite = nSprite;
                impComp.color = Color.white;
            }
            else if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
            {
                nSprite = LoadSpriteFromDict(dictItemGraphics, hotbarAbilities[i].consume.spriteRef);
                impComp.sprite = nSprite;
                impComp.color = Color.white;
            }
            else
            {
                // Nothing here.                
                impComp.sprite = null;
                impComp.color = transparentColor;
            }
        }
    }

    public static void SetWeaponToHotbar()
    {
        if (!GetWindowState(UITabs.EQUIPMENT))
        {
            return;
        }
        // TODO
    }

    public static void AlignEquippedWeaponTextOnHotbar(GameObject weapon)
    {
        // This is for main hotbar
        eqWeaponHighlight.transform.position = weapon.transform.position;
    }

    public static void HotbarConfirm()
    {
        singletonUIMS.CloseHotbarNavigating();
        uiObjectFocus.mySubmitFunction.Invoke(uiObjectFocus.onSubmitValue);
        singletonUIMS.MouseoverFetchAbilityInfo(-1);
    }

    public void CloseHotbarNavigating()
    {
        singletonUIMS.uiHotbarNavigating = false;
        HideDialogMenuCursor();
        singletonUIMS.DisableCursor();
        singletonUIMS.MouseoverFetchAbilityInfo(-1); // Clears
        HideGenericInfoBar();
        //Put the glowy hotbar to rest for now.
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) hotbarGlowComponent.StopGlowing();
    }

    public void ToggleHotbarNavigating()
    {
        singletonUIMS.CloseExamineMode();
        uiHotbarNavigating = !uiHotbarNavigating;
        if (uiHotbarNavigating)
        {
            CloseAllDialogs();
            uiHotbarNavigating = true;
            ShowDialogMenuCursor();
            EnableCursor();
            allUIObjects.Clear();
            for (int i = 0; i < hudHotbarAbilities.Length; i++)
            {
                allUIObjects.Add(hudHotbarAbilities[i]);
            }

            UIObject targetOfGlowyBoy = hudHotbarSkill1;
            if (indexOfActiveHotbar == 0)
            {
                ChangeUIFocusAndAlignCursor(hudHotbarSkill1);
                singletonUIMS.MouseoverFetchAbilityInfo(hudHotbarSkill1.onSelectValue);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(hudHotbarAbilities[11]);
                targetOfGlowyBoy = hudHotbarAbilities[11];
                singletonUIMS.MouseoverFetchAbilityInfo(hudHotbarAbilities[11].onSelectValue);
            }

            ShowGenericInfoBar(true);
            SetInfoText("<color=yellow>" + StringManager.GetString("ui_select_hotbar_skill") + "</color>");
            forceShowInfoBar = true;
            //Make the hotbar shine
            if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE) hotbarGlowComponent.StartGlowing();

            //send ghost cursor from player to hotbar
            var rt = targetOfGlowyBoy.gameObj.transform as RectTransform;

            float fCoreTime = 0.3f;
            float fTrailStartTime = 0.4f;
            float fTrailScale = 0.06f;

            int iTrailNum = 8;

            //these arrive on time
            SendGhostCursorFromPointToPoint("GhostCursorSparkles", Vector2.zero + UnityEngine.Random.insideUnitCircle * 4.0f, rt, fCoreTime);
            SendGhostCursorFromPointToPoint("GhostCursorSparkles", Vector2.zero + UnityEngine.Random.insideUnitCircle * 4.0f, rt, fCoreTime);
            SendGhostCursorFromPointToPoint("GhostCursorSparkles", Vector2.zero, rt, fCoreTime);

            //these trail
            for (int t = 0; t < iTrailNum; t++)
            {
                SendGhostCursorFromPointToPoint("GhostCursorSparkles", Vector2.zero + UnityEngine.Random.insideUnitCircle * 24.0f,
                    rt, fTrailStartTime + (fTrailScale * t));
            }

            //Flash the first skill in the hotbar (or the empty slot) 
            Image img = rt.GetComponent<Image>();
            Color baseColor = img.color;

            LeanTween.value(rt.gameObject, Color.yellow, baseColor, 0.5f).
                setOnUpdate((Color val) => img.color = val).
                setEase(LeanTweenType.easeInExpo);

            //Make it pulse just a touch.
            rt.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            LeanTween.scale(rt, Vector3.one, 0.5f).setEase(LeanTweenType.easeInBack);
        }
        else
        {
            CloseHotbarNavigating();
            forceShowInfoBar = false;
        }
    }

    public bool CheckHotbarNavigating()
    {
        return uiHotbarNavigating;
    }

    public HotbarBindable GetSelectedHotbarSlot()
    {
        return hotbarAbilities[uiHotbarSelected];
    }

    // Deprecated
    public void NudgeHotbarNavigating(int amount)
    {
        uiHotbarCursor.GetComponent<AudioStuff>().PlayCue("Move");
        uiHotbarSelected += amount;
        int tries = 0;
        if (uiHotbarSelected < 0)
        {
            uiHotbarSelected = hotbarAbilities.Length - 1;
        }
        if (uiHotbarSelected >= hotbarAbilities.Length)
        {
            uiHotbarSelected = 0;
        }
        Vector3 newPos = new Vector3(abilityIcons[uiHotbarSelected].transform.localPosition.x - 55, uiHotbarCursor.transform.localPosition.y, uiHotbarCursor.transform.localPosition.z);
        uiHotbarCursor.transform.localPosition = newPos;
        while ((hotbarAbilities[uiHotbarSelected] == null) && (tries < 8))
        {
            tries++;
            uiHotbarSelected += amount;
            if (uiHotbarSelected < 0)
            {
                uiHotbarSelected = hotbarAbilities.Length - 1;
            }
            if (uiHotbarSelected >= hotbarAbilities.Length)
            {
                uiHotbarSelected = 0;
            }
            newPos = new Vector3(abilityIcons[uiHotbarSelected].transform.localPosition.x - 55, uiHotbarCursor.transform.localPosition.y, uiHotbarCursor.transform.localPosition.z);
            uiHotbarCursor.transform.localPosition = newPos;
        }
        MouseoverFetchAbilityInfo(uiHotbarSelected);
    }

    public void SelectInvHotbarSlotForSwap(int slot)
    {
        UIObject[] invBtn = GetInvItemButtons();

        if (swappingItems)
        {
            HotbarBindableActions orig = hotbarAbilities[slot].actionType;
            Consumable c = hotbarAbilities[slot].consume;
            AbilityScript a = hotbarAbilities[slot].ability;

            hotbarAbilities[slot].actionType = hotbarAbilities[hotbarIndexToReplace].actionType;
            hotbarAbilities[slot].consume = hotbarAbilities[hotbarIndexToReplace].consume;
            hotbarAbilities[slot].ability = hotbarAbilities[hotbarIndexToReplace].ability;

            hotbarAbilities[hotbarIndexToReplace].actionType = orig;
            hotbarAbilities[hotbarIndexToReplace].consume = c;
            hotbarAbilities[hotbarIndexToReplace].ability = a;
            HideEQBlinkingCursor();
            swappingItems = false;
            UpdateInventorySheet();
            return;
        }
        else
        {
            swappingItems = true;
            ShowEQBlinkingCursor();
            AlignCursorPos(eqBlinkingCursor, invHotbar[slot].gameObj, -5f, -4f, false);
            hotbarIndexToReplace = slot;

            if (playerItemList.Count > 0)
            {
                ChangeUIFocusAndAlignCursor(invBtn[0]);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(invHotbar[0]);
            }
        }
    }

    // This is the UI hotbar buttons when clicked
    public void TryUseAbility(int slot)
    {
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;

        if (hotbarAbilities[slot] != null && hotbarAbilities[slot].actionType == HotbarBindableActions.ABILITY)
        {
            MouseoverFetchAbilityInfo(slot);
            //CloseSkillSheet();
            gms.CheckAndTryAbility(hotbarAbilities[slot].ability);
        }
        else if (hotbarAbilities[slot] != null)
        {
            Consumable drink = hotbarAbilities[slot].consume as Consumable;
            gms.PlayerUseConsumable(drink);
            return;
        }
    }
}