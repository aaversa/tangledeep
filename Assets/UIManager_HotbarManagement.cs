using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerScript : MonoBehaviour {

    public static void RemoveDuplicatesFromHotbar(Consumable consume)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
            {
                if (hotbarAbilities[i].consume == consume)
                {
                    //Debug.Log("Clearing slot " + i);
                    ClearHotbar(i);
                }
            }
        }
        UpdateHotbarBindings();
    }

    public static void RemoveDuplicatesFromHotbar(AbilityScript abil)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
            {
                if (hotbarAbilities[i].ability == abil)
                {
                    ClearHotbar(i);
                }
            }
        }
        UpdateHotbarBindings();
    }

    public static bool AddContentToSlot(ISelectableUIObject newObject, int slot)
    {
        Consumable i = newObject as Consumable;
        AbilityScript a = newObject as AbilityScript;

        if (i != null)
        {
            AddItemToSlot(i, slot, false);
            return true;
        }

        if (a != null)
        {
            AddAbilityToSlot(a, slot, true);
            return true;
        }

        return false;
    }

    public static void AddItemToSlot(Consumable consume, int slot, bool removeDupes)
    {
        if (removeDupes)
        {
            RemoveDuplicatesFromHotbar(consume);
        }
        hotbarAbilities[slot].Clear();
        Sprite nSprite = LoadSpriteFromDict(dictItemGraphics, consume.spriteRef);
        Image impComp = abilityIcons[slot].GetComponent<Image>();

        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE || HotbarHelper.IsHotbarSlotInView(slot))
        {
            impComp.color = Color.white;
        }
        else
        {
            
        }
        impComp.sprite = nSprite;
        hotbarAbilities[slot].actionType = HotbarBindableActions.CONSUMABLE;
        hotbarAbilities[slot].consume = consume;
        UpdateHotbarBindings();
    }

    public static int AddAbilityToOpenSlot(AbilityScript abil)
    {
        if (IsAbilityInHotbar(abil)) return -1;
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.NOTHING)
            {
                int slot = i;
                AddAbilityToSlot(abil, slot, true);
                /* Sprite nSprite = LoadSpriteFromAtlas(allUIGraphics, abil.iconSprite);
                Image impComp = abilityIcons[slot].GetComponent<Image>();
                impComp.sprite = nSprite;
                hotbarAbilities[slot].Clear();
                hotbarAbilities[slot].actionType = HotbarBindableActions.ABILITY;
                hotbarAbilities[slot].ability = abil;
                UpdateHotbarBindings(); */
                return slot;

            }
        }
        return -1;
    }

    public static void AddAbilityToSlot(AbilityScript abil, int slot, bool removeDupes)
    {
        if (removeDupes)
        {
            RemoveDuplicatesFromHotbar(abil);
        }
        hotbarAbilities[slot].Clear();

        Sprite nSprite = LoadSpriteFromDict(dictUIGraphics, abil.iconSprite);
        Image impComp = abilityIcons[slot].GetComponent<Image>();
        impComp.sprite = nSprite;

        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE || HotbarHelper.IsHotbarSlotInView(slot))
        {
	        impComp.color = Color.white;
        }
        else
        {
            
        }
        hotbarAbilities[slot].actionType = HotbarBindableActions.ABILITY;
        hotbarAbilities[slot].ability = abil;
        UpdateHotbarBindings();
    }

    public static KeyCode NumberToKeycode(int number)
    {
        switch (number)
        {
            case 0:
                return KeyCode.Alpha1;
            case 1:
                return KeyCode.Alpha2;
            case 2:
                return KeyCode.Alpha3;
            case 3:
                return KeyCode.Alpha4;
            case 4:
                return KeyCode.Alpha5;
            case 5:
                return KeyCode.Alpha6;
            case 6:
                return KeyCode.Alpha7;
            case 7:
                return KeyCode.Alpha8;
        }
        return KeyCode.Alpha0;
    }

    private static void UpdateHotbarBindings()
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType != HotbarBindableActions.NOTHING)
            {
                hotbarAbilities[i].SetBinding(NumberToKeycode(i));
            }
        }
    }
    #region Great Ideas 2016
    /*
    public static void TryRepairWeapon()
    {
        return;
        // Deprecated - no Durability anymore
        if (selectedItem != null)
        {
            if (selectedItem.itemType == ItemTypes.WEAPON)
            {
                Weapon wp = (Weapon)selectedItem as Weapon;
                Weapon wp2 = null;
                foreach(Item itm in playerItemList)
                {
                    if ((itm.actorRefName == wp.actorRefName) && (itm.rarity == Rarity.COMMON) && (GameMasterScript.heroPCActor.myEquipment.GetWeapon() != itm) && (selectedItem != itm)) 
                    {
                        wp2 = (Weapon)itm as Weapon;
                        float durRestore = wp2.curDurability * 0.5f;
                        wp.ChangeDurability((int)durRestore);
                        UpdateActiveWeaponInfo();
                        break;                        
                    }
                }
                if (wp2 != null)
                {
                    GameMasterScript.heroPCActor.myInventory.RemoveItem(wp2);
                    UpdateEquipmentSheet();
                    for (int i = 0; i < hotbarWeapons.Length; i++)
                    {
                        if (hotbarWeapons[i] == wp2)
                        {
                            RemoveWeaponFromActiveSlot(wp2, i);
                            return;
                        }
                    }
                }
            }
        }
    }
    */
    #endregion
    public void AddWeaponToSlotViaMouse(int slot)
    {
        TryAddWeaponToActiveSlot(slot);
    }

    public static void TryAddWeaponToActiveSlot(int whichSlot)
    {
        if (selectedItem != null)
        {
            if (selectedItem.itemType == ItemTypes.WEAPON)
            {
                bool wasInHotbar = false;
                if (IsWeaponInHotbar(selectedItem as Weapon))
                {
                    wasInHotbar = true;
                }

                bool changeWeapon = false;
                if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() == hotbarWeapons[whichSlot])
                {
                    // New hotbar weapon replaces currently equipped weapon, so switch them.
                    changeWeapon = true;
                }
                Weapon weap = (Weapon)selectedItem as Weapon;
                AddWeaponToActiveSlot(weap, whichSlot);
                if (changeWeapon)
                {
                    GameMasterScript.heroPCActor.myEquipment.Equip(weap, SND.PLAY, 0, false);
                }
                UpdateActiveWeaponInfo();

                singletonUIMS.StartCoroutine(singletonUIMS.Flash_EquipItem(eqWeapons[whichSlot].gameObj.GetComponentsInChildren<Image>()[1], 1.0f));

                //singletonUIMS.CheckIfPassTurnFromEquipping();
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem"); // This is new code when called from submenu 

                if (!wasInHotbar)
                {
                    singletonUIMS.CheckIfPassTurnFromEquipping();
                }
            }

        }

    }

    public static bool AddWeaponToOpenSlot(Weapon weap)
    {
        if (weap == null)
        {
            Debug.Log("Can't add null weapon to slot.");
            return false;
        }
        if (GameMasterScript.heroPCActor.myEquipment == null)
        {
            Debug.Log("Hero has no equipment block?");
            return false;
        }
        if (GameMasterScript.heroPCActor.myEquipment.defaultWeapon == null)
        {
            Debug.Log("Hero's default weapon is null?");
            return false;
        }
        if (hotbarWeapons == null)
        {
            Debug.Log("Hotbar weapons is null?");
            return false;
        }
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if ((GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[i], onlyActualFists: true)) || (hotbarWeapons[i] == null))
            {
                hotbarWeapons[i] = weap;
                UpdateWeaponHotbarGraphics();

                singletonUIMS.StartCoroutine(singletonUIMS.Flash_EquipItem(eqWeaponSprites[i], 1.25f, 1.5f));

                //singletonUIMS.StartCoroutine(singletonUIMS.Flash_EquipItem(eqWeaponSprites[i].transform.parent.GetComponent<Image>(), 1.25f, 1.5f));

                //singletonUIMS.CheckIfPassTurnFromEquipping();
                return true;
            }
        }
        return false; // No open slots
    }

    public static void AddWeaponToActiveSlot(Weapon weap, int slot)
    {
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (hotbarWeapons[i] == weap)
            {
                ResetSlotToNoWeapon(i);
            }
        }
        hotbarWeapons[slot] = weap;
        UpdateWeaponHotbarGraphics();

        singletonUIMS.StartCoroutine(singletonUIMS.Flash_EquipItem(weaponItemIcons[slot], 1.0f));

        if (slot == activeWeaponSlot)
        {
            GameMasterScript.heroPCActor.myEquipment.Equip(weap as Weapon, SND.PLAY, 0, true);
        }
    }

    private static void ResetSlotToNoWeapon(int slot)
    {
        hotbarWeapons[slot] = null;
        UpdateWeaponHotbarGraphics();
    }

    public static void RemoveWeaponFromActives(Weapon weap, bool updateHotbarImmediately = true)
    {
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (weap == hotbarWeapons[i])
            {
                RemoveWeaponFromActiveSlot(weap, i, updateHotbarImmediately);
                return;
            }
        }
    }

    public static void RemoveWeaponFromActiveSlot(Weapon weap, int slot, bool updateHotbarImmediately = true)
    {
        ResetSlotToNoWeapon(slot);
        hotbarWeapons[slot] = null;
        UpdateWeaponHotbarGraphics();

        if (!updateHotbarImmediately) return;

        // Go to the next available slot
        if (slot == activeWeaponSlot)
        {
            int i = slot++;
            if (i > 3)
            {
                i = 0;
            }
            activeWeaponSlot = i;
            SwitchActiveWeaponSlot(i, false);
            UpdateActiveWeaponInfo();
        }
    }

    public static void UpdateActiveWeaponInfo()
    {
        if (!GameMasterScript.actualGameStarted)
        {
            return;
        }

        // :(
        if (singletonUIMS.currentFullScreenUI != null)
        {
            return;
        }

        if (activeWeaponSlot < 0 || activeWeaponSlot > hotbarWeapons.Length)
        {
            Debug.Log("Requesting update weapon info on slot " + activeWeaponSlot + "?");
            activeWeaponSlot = 0;
            return;
        }

        // Verify the active weapon is actually equipped
        Weapon w = hotbarWeapons[activeWeaponSlot];

        if (w != null)
        {
            if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() != w)
            {
                GameMasterScript.heroPCActor.myEquipment.Equip(w, SND.SILENT, 0, true); // this was play, but we probably don't need it to
            }
        }
        else
        {
            if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() != null)
            {
                GameMasterScript.heroPCActor.myEquipment.Unequip(EquipmentSlots.WEAPON, true, SND.PLAY, true);
            }
        }

        return;
    }

    public void SwitchActiveWeaponSlotLocal(int slot)
    {
        if (swappingItems)
        {
            return;
        }
        SwitchActiveWeaponSlot(slot, false, activeWeaponSlot);
    }

    IEnumerator WaitThenSetRectTransformPosition(GameObject go, Vector2 v2, float time)
    {
        yield return new WaitForSeconds(time);

        go.GetComponent<RectTransform>().localPosition = v2; // was v2
        //Debug.Log(go.GetComponent<RectTransform>().localPosition + " " + go.GetComponent<RectTransform>().rect.position);
    }

    public static void UpdateWeaponHotbarGraphics()
    {
        for (int i = 0; i < 4; i++)
        {
            Image impComp = weaponItemIcons[i];
            bool noSprite = false;
            if (hotbarWeapons[i] == null)
            {
                //this is trash
                hotbarWeapons[i] = GameMasterScript.heroPCActor.myEquipment.defaultWeapon;
            }

            // Update holder color based on rarity
            Image holderImageComponent = weaponHolders[i].GetComponent<Image>();
            holderImageComponent.color = Item.GetRarityColor(hotbarWeapons[i]);

            if (i == activeWeaponSlot)
            {
                AlignEquippedWeaponTextOnHotbar(weaponHolders[i]);
                if (!noSprite)
                {
                    //Sprite weaponSpr = LoadSpriteFromDict(dictItemGraphics, hotbarWeapons[i].spriteRef);
                    Sprite weaponSpr = hotbarWeapons[i].GetSpriteForUI();
                    weaponItemIcons[i].sprite = weaponSpr;
                }
            }
            else
            {

            }
            if (!noSprite)
            {
                //Sprite nSprite = LoadSpriteFromDict(dictItemGraphics, hotbarWeapons[i].spriteRef);
                Sprite nSprite = hotbarWeapons[i].GetSpriteForUI();
                impComp.sprite = nSprite;
                impComp.color = Color.white;
            }
            else
            {
                impComp.sprite = null;
                impComp.color = transparentColor;
            }

        }
    }

    public static void SwitchActiveWeaponSlot(int newSlot, bool silent, bool playSFX = true)
    {
        SwitchActiveWeaponSlot(newSlot, silent, activeWeaponSlot, playSFX);
    }

    // If SILENT is FALSE, then equip the weapon also
    public static void SwitchActiveWeaponSlot(int slot, bool silent, int origSlot, bool playSFX = true)
    {
        //if (Debug.isDebugBuild) Debug.Log("Switch to " + slot + " from " + origSlot);
        Weapon wInOrigSlot = hotbarWeapons[origSlot];
        Weapon wInNewSlot = hotbarWeapons[slot];

        if (wInOrigSlot != null && wInOrigSlot.weaponType == WeaponTypes.CLAW && wInNewSlot != null && 
            wInNewSlot.weaponType != WeaponTypes.CLAW)
        {
            if (dialogBoxOpen && currentConversation.refName == "clawfrenzy_cancel")
            {
                // Don't double start the conversation
                return;
            }

            // Prompt before getting rid of Claw Mastery 1.
            if (GameMasterScript.heroPCActor.myStats.CheckHasActiveStatusName("status_clawfrenzy") &&
                GameMasterScript.gmsSingleton.ReadTempGameData("clawfrenzy_cancel") != 1)
            {
                singletonUIMS.StartCoroutine(singletonUIMS.WaitThenExitTargeting(0.01f)); // make sure we get rid of targeting mode we may have entered by auto-switching
                StartConversationByRef("clawfrenzy_cancel", DialogType.STANDARD, null);
                GameMasterScript.gmsSingleton.SetTempGameData("weaponswitch_slot", slot);
                GameMasterScript.gmsSingleton.SetTempGameData("weaponswitch_prevslot", origSlot);
                int iSilent = silent ? 1 : 0;
                GameMasterScript.gmsSingleton.SetTempGameData("weaponswitch_silent", iSilent);
                int iPlaySFX = playSFX ? 1 : 0;
                GameMasterScript.gmsSingleton.SetTempGameData("weaponswitch_playsfx", iPlaySFX);
                return;
            }
        }

        if (origSlot < 0 || origSlot >= hotbarWeapons.Length)
        {
            //Debug.Log("WARNING: Request orig slot " + origSlot + " vs slot of " + slot + "?");
            return;
        }
        if (GameMasterScript.heroPCActor == null) return;
        if (!silent && GameMasterScript.actualGameStarted && playSFX)
        {
            PlayCursorSound("UITick");
        }

        bool wasFists = false;
        if (hotbarWeapons[origSlot] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[origSlot], onlyActualFists: true))
        {
            wasFists = true;
            //if (Debug.isDebugBuild) Debug.Log("Was fists");
        }

        activeWeaponSlot = slot;
        UpdateWeaponHotbarGraphics();


        if (silent)
        {
            return;
        }
        if (slot < 4)
        {
            Weapon weap = hotbarWeapons[slot];
            if (weap == null)
            {
                GameMasterScript.heroPCActor.Unequip(EquipmentSlots.WEAPON, false);
                //if (Debug.isDebugBuild) Debug.Log("Unequipping");
            }
            else
            {
                Weapon prevWeap = GameMasterScript.heroPCActor.myEquipment.GetWeapon();

                if (hotbarWeapons[slot] == null)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Try to equip default weapon");
                    GameMasterScript.heroPCActor.TryEquip(GameMasterScript.heroPCActor.myEquipment.defaultWeapon, SND.SILENT, false);
                }
                else
                {
                    GameMasterScript.heroPCActor.TryEquip(hotbarWeapons[slot], SND.SILENT, false);
                }
            }
        }
        UpdateActiveWeaponInfo();

        bool newWeapIsFists = false;

        if (hotbarWeapons[activeWeaponSlot] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[activeWeaponSlot], onlyActualFists: true))
        {
            newWeapIsFists = true;
        }

        if (!wasFists && newWeapIsFists)
        {
            GameLogScript.LogWriteStringRef("log_switch_weapon_fists", GameMasterScript.heroPCActor, TextDensity.VERBOSE);
        }
        else if (!newWeapIsFists)
        {
            if (hotbarWeapons[activeWeaponSlot].rarity == Rarity.COMMON)
            {
                StringManager.SetTag(6, silverHexColor + hotbarWeapons[activeWeaponSlot].displayName + "</color>");
            }
            else
            {
                StringManager.SetTag(6, hotbarWeapons[activeWeaponSlot].displayName);
            }
            GameLogScript.LogWriteStringRef("log_switch_weapon", GameMasterScript.heroPCActor, TextDensity.VERBOSE);
        }

        if (newWeapIsFists)
        {
            //if (Debug.isDebugBuild) Debug.Log("New weapon is fists");
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_unarmedfighting1") || GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_unarmedfighting2"))
            {
                Offhand oh = GameMasterScript.heroPCActor.myEquipment.GetOffhand() as Offhand;
                if (oh != null && GameMasterScript.heroPCActor.myEquipment.defaultWeapon.GetPairedItem() != oh)
                {
                    GameMasterScript.heroPCActor.myEquipment.Unequip(EquipmentSlots.OFFHAND, false, SND.SILENT, false);
                }
            }
        }

        if (UIManagerScript.singletonUIMS.GetCurrentFullScreenUI() == null && hotbarWeapons[activeWeaponSlot] != null)
        {
            TDVisualEffects.PopupSprite(hotbarWeapons[activeWeaponSlot].spriteRef, GameMasterScript.heroPCActor.GetObject().transform, false, hotbarWeapons[activeWeaponSlot].GetSpriteForUI());
        }

        if (!GameMasterScript.heroPCActor.myEquipment.IsCurrentWeaponRanged())
        {
            GameMasterScript.heroPCActor.lastUsedMeleeWeapon = GameMasterScript.heroPCActor.myEquipment.GetWeapon();
        }
    }

    // This is run when the player picks up a new weapon.
    public static void UpdateActiveGear()
    {
        // This is basically an auto-equip.
        InventoryScript myInventory = GameMasterScript.heroPCActor.myInventory;
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (hotbarWeapons[i] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[i], onlyActualFists: true))
            {
                //Debug.Log("Slot " + i + " is empty.");
                foreach (Item item in myInventory.GetInventory())
                {
                    if (item.itemType == ItemTypes.WEAPON)
                    {
                        Weapon wp = (Weapon)item as Weapon;
                        //Debug.Log(wp.displayName + " is a weapon");
                        if (!IsWeaponInHotbar(wp))
                        {
                            hotbarWeapons[i] = wp;
                            AddWeaponToActiveSlot(wp, i);

                            if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(GameMasterScript.heroPCActor.myEquipment.GetWeapon(), onlyActualFists: true))
                            {
                                SwitchActiveWeaponSlot(i, false);
                            }

                            break;
                        }
                    }
                }
            }
            if (hotbarWeapons[i] != null && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(hotbarWeapons[i], onlyActualFists: true) && hotbarWeapons[i].curDurability == 0 && hotbarWeapons[i].maxDurability > 0)
            {
                //Debug.Log("Removing " + hotbarWeapons[i].displayName + " from actives");
                RemoveWeaponFromActives(hotbarWeapons[i]);
            }
        }
    }

    public static int GetActiveWeaponSlot()
    {
        return activeWeaponSlot;
    }

    public static HotbarBindable[] GetHotbarAbilities()
    {
        return hotbarAbilities;
    }

    public static bool IsWeaponInHotbar(Weapon wp)
    {
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (wp == hotbarWeapons[i])
            {
                return true;
            }

            //BAD: checking null then a hardcoded value, ugh
            if (hotbarWeapons[i] == null &&
                wp.actorRefName == "weapon_fists")
            {
                return true;
            }
        }
        return false;
    }

    //returns the index of the weapon's position in the hotbar
    public static int GetWeaponHotbarIndex(Weapon wp)
    {
        if (wp == null)
        {
            return -1;
        }
        bool bWeaponIsFists = wp.actorRefName == "weapon_fists";
        for (int i = 0; i < hotbarWeapons.Length; i++)
        {
            if (wp == hotbarWeapons[i])
            {
                return i;
            }

            if (hotbarWeapons[i] == null && bWeaponIsFists)
            {
                return i;
            }
        }
        return -1;
    }

    public static bool IsAbilityInHotbar(AbilityScript abil)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
            {
                if (hotbarAbilities[i].ability.refName == abil.refName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static int GetIndexOfActiveHotbar()
    {
        return indexOfActiveHotbar;
    }

    public static void ToggleSecondaryHotbar()
    {
        indexOfActiveHotbar++;
        if (indexOfActiveHotbar >= MAX_HOTBARS)
        {
            indexOfActiveHotbar = 0;
        }

        UpdateHotbarToggleState(1);
    }

    /// <summary>
    /// Function runs when player scrolls between hotbars on main HUD - NOT skill sheet.
    /// </summary>
    /// <param name="direction">Value of 1 means we pushed down, -1 means we pushed up</param>
    public void CycleHotbars(int direction)
    {
        // Do not allow cycling if player is on the FIRST hotbar, and has nothing on the second hotbar.
        // If we're on second hotbar already, always allow cycling.
        if (indexOfActiveHotbar == 0 && !HotbarHelper.AnyHotbarBindablesOnSecondHotbar())
        {
            return;
        }
        indexOfActiveHotbar += direction;
        if (indexOfActiveHotbar < 0)
        {
            indexOfActiveHotbar = MAX_HOTBARS - 1;
        }
        if (indexOfActiveHotbar >= MAX_HOTBARS)
        {
            indexOfActiveHotbar = 0;
        }

        UpdateHotbarToggleState(direction);
    }



    public static void RefreshHotbarSkills()
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
            {
                if (!GameMasterScript.heroPCActor.myAbilities.HasAbility(hotbarAbilities[i].ability))
                {
                    ClearHotbar(i);
                }
            }

        }
    }

    public static void RefreshHotbarItems()
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
            {
                if (hotbarAbilities[i].consume.Quantity == 0 || hotbarAbilities[i].consume.collection != GameMasterScript.heroPCActor.myInventory || !GameMasterScript.heroPCActor.myInventory.HasItem(hotbarAbilities[i].consume))
                {
                    ClearHotbar(i);
                }
            }

        }
    }

    public static void ClearEntireHotbar()
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            hotbarAbilities[i].Clear();
            hotbarAbilities[i].SetBinding(KeyCode.None);
            Image impComp = abilityIcons[i].GetComponent<Image>();
            impComp.sprite = null;
            impComp.color = transparentColor;
            singletonUIMS.RefreshAbilityCooldowns();
        }
    }

    public static void ClearHotbar(int slot)
    {
        hotbarAbilities[slot].Clear();
        hotbarAbilities[slot].SetBinding(KeyCode.None);
        Image impComp = abilityIcons[slot].GetComponent<Image>();
        impComp.sprite = null;
        impComp.color = transparentColor;
        singletonUIMS.RefreshAbilityCooldowns();
    }

    public void RefreshAbilityCooldowns(bool alterCooldownsIfNeeded = false)
    {
        // Refresh ability cooldowns

        GameObject go;
        Image img;
        HeroPC hero = GameMasterScript.GetHeroActor();

        // Food should show a "cooldown" based on your Full turns
        float fullMaxTurns = 0;
        float fullCurTurns = 0;
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            StatusEffect full = GameMasterScript.heroPCActor.myStats.GetStatusByRef("status_foodfull");
            if (full != null)
            {
                fullMaxTurns = full.maxDuration;
                fullCurTurns = full.curDuration;
            }
        }

        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            go = abilityIcons[i].transform.GetChild(0).gameObject;
            img = go.GetComponent<Image>();

            if (hotbarAbilities[i].actionType != HotbarBindableActions.NOTHING 
                && (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE || HotbarHelper.IsHotbarSlotInView(i)))
            {
                if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
                {
                    AbilityScript abil = hotbarAbilities[i].ability;

                    if (hero != null)
                    {
                        abil = hero.cachedBattleData.GetRemappedAbilityIfExists(abil, hero, alterCooldownsIfNeeded);
                        if (abil == null) // This shouldn't happen but if you have a remapper without the new upgraded ability, it can
                        {
                            abil = hotbarAbilities[i].ability;
                        }
                    }

                    if (abil.GetCurCooldownTurns() > 0)
                    {
                        img.color = new Color(img.color.r, img.color.g, img.color.b, 0.78f);
                        img.fillAmount = (float)abil.GetCurCooldownTurns() / (float)abil.maxCooldownTurns;
                    }
                    else if (abil.GetCurCooldownTurns() == 0)
                    {
                        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                    }
                    if (abil.toggled)
                    {
                        img.color = new Color(1.0f, 1.0f, 0f, 0.78f);
                        img.fillAmount = 1.0f;

                    }
                }
                else
                {
                    // Items should show cooldown IF they are food AND you are full.
                    Consumable c = hotbarAbilities[i].consume;
                    float alphaValue = 0f;

                    if (c.isFood && fullMaxTurns > 0f)
                    {
                        alphaValue = 0.78f;
                        img.fillAmount = fullCurTurns / fullMaxTurns;
                    }

                    img.color = new Color(img.color.r, img.color.g, img.color.b, alphaValue);
                }
            }
            else
            {
                
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
            }
        }
    }

    public static void TryRemoveAbilityFromHotbar(AbilityScript abil)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.ABILITY)
            {
                if (hotbarAbilities[i].ability == abil)
                {
                    ClearHotbar(i);
                }
            }
        }
        UpdateHotbarBindings();
    }

    public static void RemoveItemFromHotbar(Item itm)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            if (hotbarAbilities[i].actionType == HotbarBindableActions.CONSUMABLE)
            {
                if (hotbarAbilities[i].consume == itm)
                {
                    ClearHotbar(i);
                }
            }
        }
        UpdateHotbarBindings();
    }

    public static void RemoveObjectFromHotbar(ISelectableUIObject obj)
    {
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            HotbarBindable hb = hotbarAbilities[i];
            if (hb.ability == obj ||
                hb.consume == obj)
            {
                ClearHotbar(i);
            }
        }
        UpdateHotbarBindings();
    }

    public void SelectHotbarSlotForSkill(int index)
    {
        // This selects a hotbar slot on the Skills screen so we can swap a new skill into it.
        if (playerActiveAbilities.Count == 0) return;

        if (swappingHotbarAction)
        {
            // INDEX is the slot we are swapping INTO.
            // HotbarIndexToReplace is what we're swapping FROM.
            HotbarBindableActions orig = hotbarAbilities[index].actionType;
            Consumable c = hotbarAbilities[index].consume;
            AbilityScript a = hotbarAbilities[index].ability;

            hotbarAbilities[index].actionType = hotbarAbilities[hotbarIndexToReplace].actionType;
            hotbarAbilities[index].consume = hotbarAbilities[hotbarIndexToReplace].consume;
            hotbarAbilities[index].ability = hotbarAbilities[hotbarIndexToReplace].ability;

            hotbarAbilities[hotbarIndexToReplace].actionType = orig;
            hotbarAbilities[hotbarIndexToReplace].consume = c;
            hotbarAbilities[hotbarIndexToReplace].ability = a;

            UpdateSpriteForHotbar(index);
            UpdateSpriteForHotbar(hotbarIndexToReplace);

            RefreshAbilityCooldowns();

            HideEQBlinkingCursor();
            swappingHotbarAction = false;
            UpdateSkillSheet();
            return;
        }

        swappingSkill = false;
        hotbarIndexToReplace = index;
        ShowEQBlinkingCursor();
        AlignCursorPos(eqBlinkingCursor, skillHotbar[index].gameObj, -5f, -4f, false);
        //ChangeUIFocusAndAlignCursor(skillActiveAbilities[0]);
        SwitchSelectedUIObjectGroup(UI_GROUP_ACTIVES);
        swappingHotbarAction = true;
    }

    void UpdateSpriteForHotbar(int index)
    {
        if (index >= 0)
        {
            Image impComp = abilityIcons[index].GetComponent<Image>();
            Sprite nSprite = null;
            HotbarBindable hb = hotbarAbilities[index];

            if (hb.actionType == HotbarBindableActions.CONSUMABLE && hb.consume != null)
            {
                nSprite = LoadSpriteFromDict(dictUIGraphics, hb.consume.spriteRef);
            }
            else if (hb.actionType == HotbarBindableActions.ABILITY && hb.ability != null)
            {
                nSprite = LoadSpriteFromDict(dictUIGraphics, hb.ability.iconSprite);
            }

            impComp.sprite = nSprite;

            if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE || HotbarHelper.IsHotbarSlotInView(index))
            {
                impComp.color = Color.white;
   	        }
 	   }
    }
    public static void UpdateHotbarToggleState(int direction)
    {
		if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
		{
			UpdateJuicedHotbarCycleAnimation(direction);
			return;
		}

        //Debug.Log("Updating to " + indexOfActiveHotbar);

        switch (indexOfActiveHotbar)
        {
            case 0:
                for (int i = 3; i < 11; i++)
                {
                    hudHotbarAbilities[i].enabled = true;
                    hudHotbarAbilities[i].gameObj.SetActive(true);
                }
                for (int i = 11; i < 19; i++)
                {
                    hudHotbarAbilities[i].enabled = false;
                    hudHotbarAbilities[i].gameObj.SetActive(false);
                }
                for (int i = 3; i < hudHotbarAbilities.Length; i++)
                {
                    if (uiObjectFocus == hudHotbarAbilities[i] && i >= 11)
                    {
                        ChangeUIFocusAndAlignCursor(hudHotbarAbilities[i - 8]);
                        break;
                    }
                }

                if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
                {
                    hudHotbarAbilities[0].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarSkill8;
                }

                break;
            case 1:
                for (int i = 3; i < 11; i++)
                {
                    hudHotbarAbilities[i].enabled = false;
                    hudHotbarAbilities[i].gameObj.SetActive(false);
                }
                for (int i = 11; i < 19; i++)
                {
                    hudHotbarAbilities[i].enabled = true;
                    hudHotbarAbilities[i].gameObj.SetActive(true);
                }
                for (int i = 3; i < hudHotbarAbilities.Length; i++)
                {
                    if (uiObjectFocus == hudHotbarAbilities[i] && i >= 3 && i < 11)
                    {
                        ChangeUIFocusAndAlignCursor(hudHotbarAbilities[i + 8]);
                        break;
                    }
                }

if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
{
                hudHotbarAbilities[0].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarSkill16;
}
                break;
        }

        // Refresh tooltip on hotbar cycle
        for (int i = 0; i < hudHotbarAbilities.Length; i++)
        {
            if (uiObjectFocus == hudHotbarAbilities[i])
            {
                singletonUIMS.MouseoverFetchAbilityInfo(i - 3);
                return;
            }
        }

        singletonUIMS.MouseoverFetchAbilityInfo(-1); // clear
    }
	
    /// <summary>
    /// Kicks off the juicy hotbar cycle animation and updates shadow objects, cursor position etc via coroutine
    /// </summary>
    /// <param name="direction">-1 for up navigation, 1 for down navigation</param>	
	public static void UpdateJuicedHotbarCycleAnimation(int direction)
	{
	        GameMasterScript.StartWatchedCoroutine(HotbarHelper.AnimateHotbarSwitch(direction));
	}
}
