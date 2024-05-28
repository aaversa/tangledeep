using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public partial class UIManagerScript
{

    public void HoverStatInfo(int stat)
    {
        if (stat != -1)
        {
            charSheetStatImage.SetActive(true);
        }

        string builder = "";
        float amount = 0;
        float baseAmount = 0;

        StatTypes checkStat = StatTypes.HEALTH;

        switch (stat)
        {
            case -1:
                charSheetStatImage.SetActive(false);
                break;
            case 0: // Strength
                checkStat = StatTypes.STRENGTH;
                builder = "<size=42>" + StringManager.GetString("stat_strength").ToUpperInvariant() + " </size>\n\n";
                builder += StringManager.GetString("stat_strength_desc") + "\n\n";

                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.STRENGTH);
                amount = (float)Math.Round(baseAmount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_base_melee_power") + "\n";

                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_dagger_damage") + "\n";

                amount = baseAmount;
                amount *= StatBlock.STRENGTH_PERCENT_PHYSICALRESIST_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_physical_defense");
                currentCharsheetTooltipObject = csStrength.gameObject;
                ChangeUIFocusAndAlignCursor(csStrengthUIObject);
                break;
            case 1: // Swiftness
                checkStat = StatTypes.SWIFTNESS;
                builder = "<size=42>" + StringManager.GetString("stat_swiftness").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("stat_swiftness_desc") + "\n\n";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SWIFTNESS);
                amount = (float)Math.Round(baseAmount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_base_ranged_power") + "\n";
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_crit_damage") + "\n";
                amount = baseAmount / 10f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + " " + StringManager.GetString("misc_ct_gain") + "\n";
                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                currentCharsheetTooltipObject = csSwiftness.gameObject;
                ChangeUIFocusAndAlignCursor(csSwiftnessUIObject);
                break;
            case 2: // Spirit
                checkStat = StatTypes.SPIRIT;
                builder = "<size=42>" + StringManager.GetString("stat_spirit").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("stat_spirit_desc") + "\n\n";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SPIRIT);
                amount = baseAmount * StatBlock.SPIRIT_PERCENT_SPIRITPOWER_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_spiritpower") + "\n";
                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_staff_damage") + "\n";
                amount = baseAmount;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_powerup_healing") + "\n";
                currentCharsheetTooltipObject = csSpirit.gameObject;
                ChangeUIFocusAndAlignCursor(csSpiritUIObject);
                break;
            case 3: // Discipline
                checkStat = StatTypes.DISCIPLINE;
                builder = "<size=42>" + StringManager.GetString("stat_discipline").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("stat_discipline_desc") + "\n\n";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.DISCIPLINE);
                amount = baseAmount * StatBlock.DISCIPLINE_PERCENT_SPIRITPOWER_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_spiritpower") + "\n";
                amount = baseAmount * 0.33f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_elemental_resist") + "\n";
                amount = baseAmount * StatBlock.DISCIPLINE_PERCENT_ELEMRESIST_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_buff_duration") + "\n";
                amount = baseAmount / 2f;
                builder += "+" + (int)amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_staff_damage") + "\n";
                amount = (baseAmount * 0.5f);
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_pet_health") + "\n";

                /* amount = baseAmount * 0.04f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + "% Defeat Dodge\n";
                amount = baseAmount * 0.05f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + "% Defeat Parry"; */

                currentCharsheetTooltipObject = csDiscipline.gameObject;
                ChangeUIFocusAndAlignCursor(csDisciplineUIObject);
                break;
            case 4: // Guile
                checkStat = StatTypes.GUILE;
                builder = "<size=42>" + StringManager.GetString("stat_guile").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("stat_discipline_guile") + "\n\n";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.GUILE);
                amount = baseAmount * StatBlock.GUILE_PERCENT_CRITCHANCE_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_critchance") + "\n";

                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_dagger_damage") + "\n";

                amount = baseAmount * StatBlock.GUILE_PERCENT_PARRY_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_parrychance") + "\n";
                amount = baseAmount / 4f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_powerupdrop");
                currentCharsheetTooltipObject = csGuile.gameObject;
                ChangeUIFocusAndAlignCursor(csGuileUIObject);
                break;
            case 5: // Weapon Power
                builder = "<size=42>" + StringManager.GetString("ui_equipment_weaponpower").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_battle_power");
                currentCharsheetTooltipObject = csWeaponPower.gameObject;
                ChangeUIFocusAndAlignCursor(csWeaponPowerUIObject);
                break;
            case 6: // Spirit Power
                builder = "<size=42>" + StringManager.GetString("stat_spiritpower").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_spirit_power");
                currentCharsheetTooltipObject = csSpiritPower.gameObject;
                ChangeUIFocusAndAlignCursor(csSpiritPowerUIObject);
                break;
            case 7: // Crit Chance
                builder = "<size=42>" + StringManager.GetString("stat_critchance").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_critical_chance");
                currentCharsheetTooltipObject = csCritChance.gameObject;
                ChangeUIFocusAndAlignCursor(csCritChanceUIObject);
                break;
            case 8: // Charge Time
                builder = "<size=42>" + StringManager.GetString("stat_chargetime").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_chargetime");
                currentCharsheetTooltipObject = csChargeTime.gameObject;
                ChangeUIFocusAndAlignCursor(csChargeTimeUIObject);
                break;
            case 9: // Powerup drop
                builder = "<size=42>" + StringManager.GetString("stat_powerupdrop").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_powerup_drop");
                currentCharsheetTooltipObject = csPowerupDrop.gameObject;
                ChangeUIFocusAndAlignCursor(csPowerupDropUIObject);
                break;
            case 10: // all damage
                builder = "<size=42>" + StringManager.GetString("stat_alldamage").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_boost_alldamage");
                currentCharsheetTooltipObject = csDamageMod.gameObject;
                ChangeUIFocusAndAlignCursor(csDamageModUIObject);
                break;
            case 11: // all defense
                builder = "<size=42>" + StringManager.GetString("stat_alldefense").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_boost_alldefense");
                currentCharsheetTooltipObject = csDefenseMod.gameObject;
                ChangeUIFocusAndAlignCursor(csDefenseModUIObject);
                break;
            case 12: // parry
                builder = "<size=42>" + StringManager.GetString("stat_parrychance").ToUpperInvariant() + "</size>\n\n";
                builder += StringManager.GetString("desc_parry");
                currentCharsheetTooltipObject = csParryChance.gameObject;
                ChangeUIFocusAndAlignCursor(csParryChanceUIObject);
                break;
        }


        // Core stat? Show base vs. modified
        if ((stat >= 0) && (stat <= 4))
        {
            int baseValue = (int)GameMasterScript.heroPCActor.myStats.GetStat(checkStat, StatDataTypes.TRUEMAX);
            builder += "\n" + StringManager.GetString("ui_stats_base") + ": " + baseValue;
            int modvalue = (int)GameMasterScript.heroPCActor.myStats.GetStat(checkStat, StatDataTypes.CUR);
            if (modvalue >= baseValue)
            {
                builder += "\n" + greenHexColor + StringManager.GetString("ui_stats_modified") + ": " + modvalue;
            }
            else
            {
                builder += "\n" + redHexColor + StringManager.GetString("ui_stats_modified") + ": " + modvalue;
            }
        }

        charSheetStatInfo.text = builder;


        return;
    }

    // Called by mouse hover
    public void HoverItemInfo(int itemSlot)
    {
        if (TooltipHasCursor())
        {
            return;
        }

        if (((GetWindowState(UITabs.EQUIPMENT)) && (eqSubmenuOpen)) && (!GetWindowState(UITabs.INVENTORY)))
        {
            return;
        }


        if (UIManagerScript.dialogBoxOpen || GameMasterScript.gmsSingleton.ReadTempGameData("dropitem") >= 0)
        {
            // Maybe the adjust quantity dialog is open.
            return;
        }

        UpdatePhysicalMousePosition();
        switch (itemSlot)
        {
            case -4: // offhand
                ChangeUIFocusAndAlignCursor(eqOffhand);
                break;
            case -3: // armor
                ChangeUIFocusAndAlignCursor(eqArmor);
                break;
            case -2: // acc1
                ChangeUIFocusAndAlignCursor(eqAccessory1);
                break;
            case -1: // acc2
                ChangeUIFocusAndAlignCursor(eqAccessory2);
                break;
        }
        HoverItemInfoConditional(itemSlot, false, true); // False or true? I dunno
    }

    // called internally, not by UI
    public void HoverItemInfoConditional(int itemSlot, bool readCurrentFocus, bool selectByMouse)
    {
        if (UIManagerScript.dialogBoxOpen) return;
        List<Item> listToUse = playerItemList;
        if (ShopUIScript.CheckShopInterfaceState())
        {
            ShowItemInfoShop(itemSlot, ShopUIScript.playerItemList);
            return;
        }

        if (!GetWindowState(UITabs.EQUIPMENT) && !GetWindowState(UITabs.INVENTORY) && !ShopUIScript.CheckShopInterfaceState())
        {
            return;
        }

        if ((eqSubmenuOpen && GetWindowState(UITabs.EQUIPMENT)) || (GetWindowState(UITabs.INVENTORY) && invSubmenuOpen))
        {
            return;
        }

        if (itemSlot >= listToUse.Count && !readCurrentFocus)
        {
            //Debug.Log("Slot " + itemSlot + " out of range " + listToUse.Count + " " + readCurrentFocus);
            return;
        }

        if (selectByMouse)
        {
            invFilterItemType = ItemTypes.ANY; // New code
        }

        if (playerItemList.Count == 0)
        {
            ClearItemInfo(0);
        }

        if (readCurrentFocus)
        {
            itemSlot = GetIndexOfSelectedButton();
        }

        Item localItem = null;
        GameObject localFocus = null;

        if (localFocus != null)
        {
            mouseHoverItem = localItem;
            DisplayItemInfo(localItem, localFocus, true);
        }

        bool highlightedWeaponSlot = false;

        switch (itemSlot)
        {
            /* case (int)EEquipmentUISpecialtySlots.EQ_EMBLEM_SLOT:
                localItem = GameMasterScript.heroPCActor.myEquipment.GetEmblem();
                localFocus = eqEmblem.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.EMBLEM;
                }
                //UpdateEquipmentSheet();
                break; */
            case (int)EEquipmentUISpecialtySlots.EQ_ARMOR_SLOT:
                localItem = GameMasterScript.heroPCActor.myEquipment.GetArmor();
                localFocus = eqArmor.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.ARMOR;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_OFFHAND_SLOT:
                localItem = GameMasterScript.heroPCActor.myEquipment.GetOffhand();
                localFocus = eqOffhand.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.OFFHAND;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT1:
                localItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY];
                localFocus = eqAccessory1.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.ACCESSORY;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_ACCESSORY_SLOT2:
                localItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2];
                localFocus = eqAccessory2.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.ACCESSORY;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT1:
                highlightedWeaponSlot = true;
                localItem = hotbarWeapons[0];
                localFocus = eqWeapon1.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.WEAPON;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT2:
                highlightedWeaponSlot = true;
                localItem = hotbarWeapons[1];
                localFocus = eqWeapon2.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.WEAPON;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT3:
                highlightedWeaponSlot = true;
                localItem = hotbarWeapons[2];
                localFocus = eqWeapon3.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.WEAPON;
                }
                //UpdateEquipmentSheet();
                break;
            case (int)EEquipmentUISpecialtySlots.EQ_WEAPONHOTBAR_SLOT4:
                highlightedWeaponSlot = true;
                localItem = hotbarWeapons[3];
                localFocus = eqWeapon4.gameObj;
                if ((!swappingItems) && (!selectByMouse))
                {
                    invFilterItemType = ItemTypes.WEAPON;
                }
                //UpdateEquipmentSheet();
                break;
            default:
                if (localItem != null) break;
                int itemIndex = itemSlot + Math.Abs(listArrayIndexOffset);

                if (itemIndex >= listToUse.Count)
                {
                    //Debug.Log("Out of range. " + itemSlot + " " + listArrayIndexOffset + " " + listToUse.Count + " " + readCurrentFocus);
                    return;
                }

                localItem = listToUse[itemIndex];
                if (ShopUIScript.CheckShopInterfaceState())
                {
                    localFocus = ShopUIScript.shopItemButtonList[itemSlot].gameObj;
                    ChangeUIFocus(ShopUIScript.shopItemButtonList[itemSlot]);
                }
                break;
        }

        if (localItem == null)
        {
            if (!highlightedWeaponSlot)
            {
                return;
            }
            else
            {
                localItem = GameMasterScript.heroPCActor.myEquipment.defaultWeapon;
            }

        }
        if (localFocus != null)
        {
            mouseHoverItem = localItem;
            DisplayItemInfo(localItem, localFocus, true);
        }
    }

    public void MouseoverFetchWeaponInfo(int slot)
    {
        if (slot == -1)
        {
            uiPlayerSkillInfo.text = "";
            uiPlayerSkillInfoContainer.gameObject.SetActive(false);
            TryEnableGamblerHand();
            return;
        }
        else
        {
            if (hotbarWeapons[slot] != null)
            {
                uiPlayerSkillInfoContainer.gameObject.SetActive(true);
                uiPlayerSkillInfo.text = "<size=42>" + hotbarWeapons[slot].displayName + "</size>\n\n" + hotbarWeapons[slot].GetItemInformationNoName(false);
            }
            DisableGamblerHand();
        }
    }

    public void MouseoverFetchAbilityInfo(int slot)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        GameObject go = null;
        TextMeshProUGUI textToUse = null;
        go = uiPlayerSkillInfoContainer.gameObject;
        textToUse = uiPlayerSkillInfo;

        if (slot == -1)
        {
            textToUse.text = "";
            go.SetActive(false);
            TryEnableGamblerHand();
            return;
        }
        if (slot < 100)
        {
            if (hotbarAbilities[slot] != null)
            {
                go.SetActive(true);
                textToUse.text = hotbarAbilities[slot].GetHotbarActionInfo();
                if (textToUse.text == "")
                {
                    go.SetActive(false);
                }
            }
        }
        else
        {
            // 100 = Flask, 101 = Portal
            uiPlayerSkillInfoContainer.gameObject.SetActive(true);
            switch (slot)
            {
                case 100:

                    // FLASK HEAL CALCULATION
                    float restoreAmount = (18f + (0.023f * GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX)));
                    if (GameMasterScript.heroPCActor.ReadActorData("infuse2") == GameMasterScript.FLASK_HEAL_MORE)
                    {
                        restoreAmount += (GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.012f);
                    }
                    float baseDuration = 5f;
                    baseDuration *= (1f + (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * 0.33f));
                    int dur = (int)baseDuration;
                    if (GameMasterScript.heroPCActor.ReadActorData("flask_apple_infuse") == 1)
                    {
                        dur += 1;
                    }
                    restoreAmount *= dur;
                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_thirstquencher"))
                    {
                        restoreAmount *= 1.11f;
                    }

                    string getFlaskHotkey = CustomAlgorithms.GetButtonAssignment("Use Healing Flask");
                    StringManager.SetTag(0, getFlaskHotkey);
                    StringManager.SetTag(1, ((int)restoreAmount).ToString());
                    StringManager.SetTag(2, dur.ToString());

                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_healingflask") + " </color>\n" + GetHotkeyCheckForSkillHoverInfo() + "\n\n" + StringManager.GetString("description_healingflask");
                    break;
                case 101:
                    string getPortalHotkey = CustomAlgorithms.GetButtonAssignment("Use Town Portal");
                    StringManager.SetTag(0, getPortalHotkey);
                    StringManager.SetTag(1, "12"); // number of turns

                    if (SharaModeStuff.IsSharaModeActive() && StringManager.gameLanguage == EGameLanguage.en_us)
                    {
                        
                        uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_magicportal") + " </color>\n" + GetHotkeyCheckForSkillHoverInfo() + "\n\n" + StringManager.GetString("description_escapeportal_shara");
                    }
                    else
                    {
                        uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_magicportal") + " </color>\n" + GetHotkeyCheckForSkillHoverInfo() + "\n\n" + StringManager.GetString("description_escapeportal");
                    }
                    break;
                case 102:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_chargetime") + " </color>\n\n" + StringManager.GetString("desc_chargetime");
                    break;
                case 103:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_experience") + " </color>\n\n" + StringManager.GetString("description_experience");
                    break;
                case 104:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_health") + " </color>\n\n" + StringManager.GetString("desc_health");
                    break;
                case 105:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_stamina") + " </color>\n\n" + StringManager.GetString("desc_stamina");
                    break;
                case 106:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_energy") + " </color>\n\n" + StringManager.GetString("desc_energy");
                    break;
                case 107:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_warptostairs") + " </color>\n\n" + StringManager.GetString("description_warptoentrance");
                    break;
                case 108:
                    string getSnackBagHotkey = CustomAlgorithms.GetButtonAssignment("View Healing Items");
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_snackbag") + "</color>\n" + StringManager.GetString("misc_hotkey") + ": " + getSnackBagHotkey + "\n\n" + StringManager.GetString("desc_snackbag");
                    break;
                case 109:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("stat_health") + " </color>\n\n" + StringManager.GetString("desc_health");
                    break;
                case 110:
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_minimap") + " </color>\n\n" + StringManager.GetString("description_map");
                    break;
                case 111:
                    int roundedLimit = GameMasterScript.heroPCActor.GetVisualLimitBreakAmount();
                    StringManager.SetTag(0, roundedLimit + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                    uiPlayerSkillInfo.text = "<color=yellow>" + StringManager.GetString("misc_xp2_limitbreak") + " </color>\n\n" + StringManager.GetString("description_xp2_limitbreak");
                    break;
            }
        }
        DisableGamblerHand();

    }

    public void MouseoverFetchBuffInfo(int slot)
    {
        if (slot == -1)
        {
            uiPlayerSkillInfoContainer.gameObject.SetActive(false);
            return;
        }
        if (playerBuffs[slot] != null)
        {
            string newText = "";
            if ((playerBuffs[slot].toggled) || (playerBuffs[slot].CheckDurTriggerOn(StatusTrigger.PERMANENT)))
            {
                newText = "<color=yellow>" + playerBuffs[slot].abilityName + "</color>\n"; // (Toggled)\n";
            }
            else
            {
                newText = "<color=yellow>" + playerBuffs[slot].abilityName + "</color> (" + playerBuffs[slot].curDuration + " " + StringManager.GetString("misc_turns") + ")\n";
            }

            newText += playerBuffs[slot].GetLiveDescription();

            uiPlayerSkillInfoContainer.gameObject.SetActive(true);
            uiPlayerSkillInfo.text = newText;
            //Debug.Log("Set text to " + newText);
        }
    }
    public void MouseoverFetchDebuffInfo(int slot)
    {
        if (slot == -1)
        {
            uiPlayerSkillInfoContainer.gameObject.SetActive(false);
            return;
        }
        if (playerDebuffs[slot] != null)
        {
            string newText = "<color=red>" + playerDebuffs[slot].abilityName + "</color> (" + playerDebuffs[slot].curDuration + " turns)\n";
            newText += playerDebuffs[slot].GetLiveDescription();
            uiPlayerSkillInfoContainer.gameObject.SetActive(true);
            uiPlayerSkillInfo.text = newText;
        }
    }

    public void HoverEquipmentTab(int tab)
    {
        if (TooltipHasCursor())
        {
            return;
        }

        if (eqSubmenuOpen) return;
        //HideEQTooltipContainer();
        switch (tab)
        {
            case -1:
                return;
            case (int)GearFilters.VIEWALL:
                ChangeUIFocusAndAlignCursor(eqFilterViewAll);
                break;
            case (int)GearFilters.WEAPON:
                ChangeUIFocusAndAlignCursor(eqFilterWeapons);
                break;
            case (int)GearFilters.OFFHAND:
                ChangeUIFocusAndAlignCursor(eqFilterOffhand);
                break;
            case (int)GearFilters.ARMOR:
                ChangeUIFocusAndAlignCursor(eqFilterArmor);
                break;
            case (int)GearFilters.ACCESSORY:
                ChangeUIFocusAndAlignCursor(eqFilterAccessory);
                break;
            case (int)GearFilters.COMMON:
                ChangeUIFocusAndAlignCursor(eqFilterCommon);
                break;
            case (int)GearFilters.MAGICAL:
                ChangeUIFocusAndAlignCursor(eqFilterMagical);
                break;
            case (int)GearFilters.LEGENDARY:
                ChangeUIFocusAndAlignCursor(eqFilterLegendary);
                break;
            case (int)GearFilters.GEARSET:
                ChangeUIFocusAndAlignCursor(eqFilterGearSet);
                break;
        }
        HideEQTooltipContainer();
    }

    public void HoverInventoryTab(int tab)
    {
        if (invSubmenuOpen) return;
        switch (tab)
        {
            case -1:
                return;
            case (int)ItemFilters.VIEWALL:
                ChangeUIFocusAndAlignCursor(invFilterViewAll);
                break;
            case (int)ItemFilters.INGREDIENT:
                //ChangeUIFocusAndAlignCursor(invTabIngredients);
                break;
            case (int)ItemFilters.MEAL:
                //ChangeUIFocusAndAlignCursor(invTabMeals);
                break;
            case (int)ItemFilters.SUPPORT:
                //ChangeUIFocusAndAlignCursor(invTabSupport);
                break;
            case (int)ItemFilters.OFFENSE:
                ChangeUIFocusAndAlignCursor(invFilterOffense); // was tab
                break;
            case (int)ItemFilters.VALUABLES:
                ChangeUIFocusAndAlignCursor(invFilterValuables); // was tab
                break;
            case (int)ItemFilters.HEALHP:
                //ChangeUIFocusAndAlignCursor(invFilterHealHP);
                break;
            case (int)ItemFilters.HEALSTAMINA:
                //ChangeUIFocusAndAlignCursor(invFilterHealStamina);
                break;
            case (int)ItemFilters.HEALENERGY:
                //ChangeUIFocusAndAlignCursor(invFilterHealEnergy);
                break;
            case (int)ItemFilters.DEALDAMAGE:
                //ChangeUIFocusAndAlignCursor(invFilterDealDamage);
                break;
            case (int)ItemFilters.SELFBUFF:
                ChangeUIFocusAndAlignCursor(invFilterSelfBuff);
                break;
            case (int)ItemFilters.SUMMON:
                ChangeUIFocusAndAlignCursor(invFilterSummon);
                break;
            case (int)ItemFilters.RECOVERY:
                ChangeUIFocusAndAlignCursor(invFilterRecovery);
                break;
            case (int)ItemFilters.UTILITY:
                ChangeUIFocusAndAlignCursor(invFilterUtility);
                break;
            case (int)ItemFilters.FAVORITES:
                ChangeUIFocusAndAlignCursor(invFilterFavorites);
                break;
        }
    }

    // This both SETS the selecteditem, and displays info about it
    public void ShowItemInfo(int id)
    {
        ShowItemInfoInternal(id, playerItemList);
    }

    public void DisplayAbilInfo(AbilityScript abilToDisplay)
    {
        if (abilToDisplay == null)
        {
            Debug.Log("Can't display info for null ability.");
            return;
        }
        HeroPC hero = GameMasterScript.heroPCActor;
        if (hero != null)
        {
            abilToDisplay = hero.cachedBattleData.GetRemappedAbilityIfExists(abilToDisplay, hero, false);
        }
        string text = "<#fffb00>" + abilToDisplay.abilityName + "</color>\n" + abilToDisplay.GetAbilityInformation();
    }
    public void DisplayItemInfo(Item itemToDisplay, GameObject refObject, bool mouseHoverSource)
    {
        if (dialogBoxOpen) return; // Adjust quantity might be up on the screen.

        //todo Ask the new currentUIFullScreen to DisplayItemInfo first
        if (currentFullScreenUI != null)
        {
            currentFullScreenUI.DisplayItemInfo(itemToDisplay, refObject, mouseHoverSource);
            return;
        }

        //Debug.Log("Displaying item info for " + itemToDisplay.actorRefName + " " + uiObjectFocus.gameObj.name);
        Item compareItem = null;
        if (isDraggingItem)
        {
            // Compare item: Check hover slot?
            if (GameMasterScript.heroPCActor.myEquipment.IsEquipped(itemToDisplay))
            {
                compareItem = itemToDisplay;
                //Debug.Log("Compare item is " + compareItem.actorRefName + " which is equipped");
            }
            itemToDisplay = draggingItem;
        }
        if (itemToDisplay == null)
        {
            //Debug.Log("Can't display info for null item.");
            return;
        }
        Vector3 pos = refObject.transform.localPosition;
        if (pos.x < 0)
        {
            pos.x += refObject.GetComponent<RectTransform>().rect.width;
        }
        else
        {
            pos.x -= refObject.GetComponent<RectTransform>().rect.width;
        }

        pos.y -= refObject.GetComponent<RectTransform>().rect.height * 2f;
        pos.y -= 30f;

        if (ShopUIScript.CheckShopInterfaceState())
        {
            // TODO: Add item comparisons here too, for stats etc.
            string baseText = itemToDisplay.GetItemInformationNoName(true);

            if (currentConversation.whichNPC.actorRefName == "npc_banker")
            {
                if (ShopUIScript.shopState == ShopState.BUY) // Withdrawal
                {
                    baseText += "\n\n<#fffb00>" + StringManager.GetString("misc_withdraw") + ": " + StringManager.GetString("misc_free_exciting") + " </color>";
                }
                else // Depositing
                {
                    baseText += "\n\n<#fffb00>" + StringManager.GetString("misc_deposit_cost") + ": " + (int)itemToDisplay.GetBankPrice() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>";
                }
            }
            else
            {
                if (ShopUIScript.shopState == ShopState.BUY)
                {
                    if (currentConversation.whichNPC.actorRefName == "npc_foodcart")
                    {
                        baseText += "\n\n<#fffb00>" + StringManager.GetString("misc_withdraw") + ": " + StringManager.GetString("misc_free_exciting") + " </color>";
                    }
                    else
                    {
                        // As of 10/28, this is for individual items only.                    
                        baseText += "\n\n<#fffb00>" + StringManager.GetString("ui_misc_cost") + " " + itemToDisplay.GetIndividualShopPriceWithUnit() + "</color>";
                    }
                }
                else
                {
                    //reading GetShop().GetShop() took two hours off my life.
                    float val = UIManagerScript.currentConversation.whichNPC.GetShop().GetShop().saleMult;
                    int stackSalePrice = itemToDisplay.GetSalePrice(val);
                    int individualSalePrice = itemToDisplay.GetIndividualSalePrice(val);
                    baseText += "\n\n";
                    StringManager.SetTag(0, individualSalePrice.ToString());
                    StringManager.SetTag(1, stackSalePrice.ToString());

                    if (currentConversation.whichNPC.actorRefName != "npc_foodcart")
                    {
                        if (itemToDisplay.GetQuantity() > 1)
                        {
                            baseText += StringManager.GetString("misc_shop_stack");
                        }
                        else
                        {
                            baseText += StringManager.GetString("misc_shop_single");
                        }
                    }
                }
            }

            ShopUIScript.shopItemInfoName.text = itemToDisplay.displayName;
            ShopUIScript.shopItemInfoText.text = baseText;
            ShopUIScript.shopItemInfoImage.color = Color.white;
            ShopUIScript.shopItemInfoImage.sprite = itemToDisplay.GetSpriteForUI();
            //ShopUIScript.shopItemInfoImage.sprite = LoadSpriteFromAtlas(allItemGraphics, itemToDisplay.spriteRef);

            if (itemToDisplay.itemType != ItemTypes.CONSUMABLE)
            {
                string bText = "";
                Equipment eq = itemToDisplay as Equipment;
                compareItem = null;
                switch (eq.slot)
                {

                    case EquipmentSlots.WEAPON:
                        Weapon cur = GameMasterScript.heroPCActor.myEquipment.GetWeapon();
                        if (cur != null)
                        {
                            bText = cur.GetItemInformationNoName(true);
                            compareItem = cur;
                        }
                        break;
                    case EquipmentSlots.ARMOR:
                        Armor arm = GameMasterScript.heroPCActor.myEquipment.GetArmor();
                        if (arm != null)
                        {
                            bText = arm.GetItemInformationNoName(true);
                        }
                        compareItem = arm;
                        break;
                    case EquipmentSlots.OFFHAND:
                        Item itm = GameMasterScript.heroPCActor.myEquipment.GetOffhand();
                        if (itm != null)
                        {
                            bText = itm.GetItemInformationNoName(true);
                        }
                        compareItem = itm;
                        break;
                    case EquipmentSlots.ACCESSORY:Equipment eq2 = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY];
                        if (TDInputHandler.IsCompareAlternateButtonDown())
                        {
                            eq2 = GameMasterScript.heroPCActor.myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2];
                        }

                        if (eq2 != null)
                        {
                            bText = eq2.GetItemInformationNoName(true);
                        }
                        compareItem = eq2;
                        break;
                }
                if (bText != "")
                {
                    ShopUIScript.shopItemComparisonHeader.GetComponent<TextMeshProUGUI>().text = StringManager.GetString("ui_current_gear");
                    ShopUIScript.shopCompItemInfoName.text = compareItem.displayName;
                    ShopUIScript.shopCompItemInfoText.text = bText;
                    ShopUIScript.shopCompItemInfoImage.color = Color.white;
                    ShopUIScript.shopCompItemInfoImage.sprite = LoadSpriteFromDict(dictItemGraphics, compareItem.spriteRef);
                }
            }
            else if (ShopUIScript.shopState == ShopState.BUY)
            {
                // Compare to how many of these consumables we already have.
                int countOfItem = GameMasterScript.heroPCActor.myInventory.GetItemQuantity(itemToDisplay);
                StringManager.SetTag(0, itemToDisplay.displayName);
                ShopUIScript.shopComparisonAreaText.text = cyanHexColor + StringManager.GetString("misc_already_owned") + "</color> " + countOfItem;
            }
            // End shop interface case.
        }

        if (itemToDisplay.itemType != ItemTypes.CONSUMABLE)
        {
            Equipment eq = null;
            EquipmentSlots slot = EquipmentSlots.ANY;

            Equipment curItem = null;

            if (swappingItems)
            {
                curItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)slot];
                eq = itemToDisplay as Equipment; // This is the new item
                slot = eq.slot;
            }
            else if (isDraggingItem)
            {
                // For DRAGGING items. 
                curItem = compareItem as Equipment;
                eq = draggingItem as Equipment;
                slot = GameMasterScript.heroPCActor.myEquipment.GetSlotOfEquippedItem(curItem);
                slotToSwap = slot;
            }
            else
            {
                if (GetWindowState(UITabs.EQUIPMENT))
                {
                    eq = itemToDisplay as Equipment; // This is the new item
                    slot = eq.slot;
                    slotToSwap = slot; // ???
                    curItem = GameMasterScript.heroPCActor.myEquipment.equipment[(int)slot];

                }
            }

            string compareString = "";

            if (ShopUIScript.CheckShopInterfaceState())
            {
                if (compareItem != null && itemToDisplay != null)
                {
                    compareString = EquipmentBlock.CompareItems(compareItem as Equipment, itemToDisplay as Equipment, slot);

                    if (StringManager.gameLanguage == EGameLanguage.de_germany)
                    {
                        compareString = compareString.Replace('.', ',');
                    }

                    if (compareString != "" && compareString != "\n")
                    {
                        ShopUIScript.shopComparisonAreaText.text = cyanHexColor + StringManager.GetString("misc_newitem_changes") + ":</color>\n" + compareString;
                    }
                }

            }
        }

        //Debug.Log("Done displaying " + uiObjectFocus.gameObj.name);
    }

    private string GetHotkeyCheckForSkillHoverInfo()
    {
        bool showKeyboardHotkeys = true;

        if (PlatformVariables.GAMEPAD_ONLY)
        {
            showKeyboardHotkeys = false;
        }

        string checkString = "";

        if (showKeyboardHotkeys)
        {
            checkString += StringManager.GetString("misc_hotkeycheck");
        }
        else
        {
            checkString += "\n";
        }

        return checkString;
    }

}
