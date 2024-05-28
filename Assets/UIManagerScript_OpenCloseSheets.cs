using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public partial class UIManagerScript
{

    public ImpactUI_Base GetCurrentFullScreenUI()
    {
        return currentFullScreenUI;
    }

    public static void UpdateSkillSheet()
    {
    }

    // Job sheet will be obsolete.
    public static void UpdateJobSheet()
    {
        HeroPC hero = GameMasterScript.heroPCActor;
        CharacterJobData cjd = hero.myJob;
        jobName.text = cjd.jobName;
        jobLore.text = cjd.jobDescription + "\n\n<#fffb00>Mastery: " + (int)hero.jobJPspent[(int)hero.myJob.jobEnum] + "/" + GameMasterScript.maxJPAllJobs[(int)hero.myJob.jobEnum] + "jp</color>";
        jobSkillHeader.text = "Learn Skills (JP: " + (int)hero.jobJP[(int)hero.myJob.jobEnum] + ")";

        // Construct ability list from class sheet.
        playerJobAbilities.Clear();
        foreach (JobAbility abil in cjd.JobAbilities)
        {
            //Debug.Log("Checking " + abil.ability.abilityName + " " + abil.ability.refName);
            bool masterable = false;
            if (abil.masterCost > 0)
            {
                masterable = true;
            }

            if ((((!hero.myAbilities.HasMasteredAbility(abil.ability)) && (masterable)) || (!hero.myAbilities.HasAbility(abil.ability))) && (!abil.innate))
            {
                playerJobAbilities.Add(abil);
            }
        }

        float jp = GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum];

        for (int i = 0; i < jobAbilButtons.Length; i++)
        {
            if (i >= playerJobAbilities.Count)
            {
                TextMeshProUGUI txt = jobAbilButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = "";
                jobAbilButtons[i].gameObj.SetActive(false);
                jobAbilButtons[i].enabled = false;
            }
            else
            {
                TextMeshProUGUI txt = jobAbilButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();

                int offset = i;

                offset += Math.Abs(listArrayIndexOffset);

                if (offset >= playerJobAbilities.Count)
                {
                    jobAbilButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    jobAbilButtons[i].gameObj.SetActive(false);
                    jobAbilButtons[i].enabled = false;
                    break;
                }

                Sprite nSprite = LoadSpriteFromDict(dictUIGraphics, playerJobAbilities[offset].ability.iconSprite);
                Image impComp = jobAbilButtons[i].gameObj.GetComponentInChildren<Image>();
                impComp.sprite = nSprite;

                if (jp < GameMasterScript.heroPCActor.GetCostForAbilityBecauseWeDoStuffIfWeArentInOurStartingJob(playerJobAbilities[offset]) || jp < playerJobAbilities[offset].masterCost)
                {
                    txt.text = "<color=red>" + playerJobAbilities[offset].ability.abilityName + "</color>";
                }
                else
                {
                    txt.text = playerJobAbilities[offset].ability.abilityName;
                }

                jobAbilButtons[i].gameObj.SetActive(true);
                jobAbilButtons[i].enabled = true;
            }
        }

        string innateText = "<color=#40b843>Basic Traits</color>\n" + cjd.BonusDescription1;
        if (hero.jobJPspent[(int)hero.myJob.jobEnum] < 1000)
        {
            innateText += "\n\n<#fffb00>Advanced Traits (Spend 1000+ JP)</color>\n" + cjd.BonusDescription2;
        }
        else
        {
            innateText += "\n\n<color=#40b843>Advanced Traits</color>\n" + cjd.BonusDescription2;
        }
        if (playerJobAbilities.Count > 0)
        {
            innateText += "\n\n<#fffb00>MASTER Traits (Learn all abilities)</color>\n" + cjd.BonusDescription3;
        }
        else
        {
            innateText += "\n\n<color=#40b843>MASTER Traits</color>\n" + cjd.BonusDescription3;
        }
        jobInnate.text = innateText;



        if ((uiObjectFocus != null) && (playerJobAbilities.Count > 0))
        {
            int index = singletonUIMS.GetIndexOfSelectedButton();
            if (index == -1) return;
            selectedAbility = playerJobAbilities[index + listArrayIndexOffset];
        }

        if (selectedAbility != null)
        {
            jobAbilInfo.text = selectedAbility.GetAbilityInformation();
        }

    }

    public static void UpdateInventorySheet()
    {
        UIObject[] invBtn = GetInvItemButtons();

        if (!GetWindowState(UITabs.INVENTORY))
        {
            return;
        }

        UpdateInventoryHotbar();

        HeroPC hero = GameMasterScript.heroPCActor;

        invPlayerMoney.text = StringManager.GetString("ui_money") + ":<#fffb00> " + GameMasterScript.heroPCActor.GetMoney() + "</color>g";

        playerItemList.Clear();
        foreach (Item itm in hero.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.CONSUMABLE)
            {
                continue;
            }

            if (itemFilterTypes[(int)ItemFilters.FAVORITES])
            {
                if (!itm.favorite)
                {
                    continue;
                }
            }

            bool matchAnyFilters = false;

            if (!itemFilterTypes[(int)ItemFilters.VIEWALL])
            {
                foreach (ItemFilters iFilter in allPossibleInventoryFilters)
                {
                    if (!itemFilterTypes[(int)iFilter]) continue;
                    if (itm.CheckTag((int)iFilter))
                    {
                        matchAnyFilters = true;
                        break;
                    }
                }
            }
            else
            {
                matchAnyFilters = true;
                // Show all types with matchAnyFilters
            }



            if (!matchAnyFilters)
            {
                continue;
            }


            playerItemList.Add(itm);
        }

        bool playerIsFull = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");

        for (int i = 0; i < invBtn.Length; i++)
        {
            if (i >= playerItemList.Count)
            {
                TextMeshProUGUI txt = invBtn[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = "";
                invBtn[i].gameObj.SetActive(false);
                invBtn[i].enabled = false;
            }
            else
            {
                TextMeshProUGUI txt = invBtn[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                int offset = i;

                offset += Math.Abs(listArrayIndexOffset);

                if (offset >= playerItemList.Count)
                {
                    invBtn[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    invBtn[i].gameObj.SetActive(false);
                    invBtn[i].enabled = false;
                    break;
                }

                Consumable cx = (Consumable)playerItemList[offset] as Consumable;

                if ((cx.isFood) && (playerIsFull))
                {
                    txt.text = redHexColor + playerItemList[offset].displayName + "</color>";
                }
                else
                {
                    txt.text = playerItemList[offset].displayName;
                }

                txt.text = CustomAlgorithms.CheckForFavoriteOrTrashAndInsertMark(txt.text, playerItemList[offset]);

                if (cx.Quantity > 1)
                {
                    txt.text += " (" + cx.Quantity + ")";
                }
                invBtn[i].gameObj.SetActive(true);
                string invSpriteRef = "";
                if ((playerItemList[offset].spriteRef == null) || (playerItemList[offset].spriteRef == ""))
                {
                    //invSpriteRef = "assorteditems_140"; // TODO: Better placeholders.
                    invBtn[i].subObjectImage.sprite = null;
                    invBtn[i].subObjectImage.color = transparentColor;
                }
                else
                {
                    invSpriteRef = playerItemList[offset].spriteRef;
                    invBtn[i].subObjectImage.sprite = LoadSpriteFromDict(dictItemGraphics, invSpriteRef);
                    invBtn[i].subObjectImage.color = Color.white;
                }
                invBtn[i].enabled = true;
            }
        }

        //UpdateSelectedItem();

        string info = "";

        if (selectedItem != null)
        {
            info = selectedItem.GetItemInformationNoName(true);
            invItemInfoName.text = selectedItem.displayName;
            //Debug.Log("Update inv text to " + info);
            invItemInfoText.text = info;
            invItemInfoImage.color = Color.white;
            invItemInfoImage.sprite = LoadSpriteFromDict(dictItemGraphics, selectedItem.spriteRef);
        }
        else
        {
            singletonUIMS.ClearItemInfo(0);
        }

        int cursorIndex = singletonUIMS.GetIndexOfSelectedButton();
        if (cursorIndex >= playerItemList.Count)
        {
            if (playerItemList.Count == 0)
            {
                //ChangeUIFocusAndAlignCursor(invItemSortAZ);
                ChangeUIFocusAndAlignCursor(invHotbar1);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(invBtn[playerItemList.Count - 1]);
            }
        }

        if (uiObjectFocus == invHotbar1)
        {
            singletonUIMS.MouseoverFetchAbilityInfo(0);
        }

        invItemListDummyButton.enabled = false;

        UpdateScrollbarPosition();

    }

    public static void UpdateQuestSheet()
    {
        JournalScript.OpenJournal();
        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.ShowDialogMenuCursor();
        //JournalScript.UpdateQuests();
    }

    public static void UpdatePlayerCharacterSheet()
    {
        HeroPC hero = GameMasterScript.heroPCActor;

        string coreStats = (int)hero.myStats.GetCurStat(StatTypes.HEALTH) + " / " + (int)hero.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) + "\n";
        coreStats += (int)hero.myStats.GetCurStat(StatTypes.STAMINA) + " / " + (int)hero.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX) + "\n";
        coreStats += (int)hero.myStats.GetCurStat(StatTypes.ENERGY) + " / " + (int)hero.myStats.GetStat(StatTypes.ENERGY, StatDataTypes.MAX);

        csResources.text = coreStats;

        // Make it so that the stats show in green or red if they are affected / non-max?

        csStrength.text = ((int)hero.myStats.GetCurStat(StatTypes.STRENGTH)).ToString();
        csSwiftness.text = ((int)hero.myStats.GetCurStat(StatTypes.SWIFTNESS)).ToString();
        csGuile.text = ((int)hero.myStats.GetCurStat(StatTypes.GUILE)).ToString();
        csDiscipline.text = ((int)hero.myStats.GetCurStat(StatTypes.DISCIPLINE)).ToString();
        csSpirit.text = ((int)hero.myStats.GetCurStat(StatTypes.SPIRIT)).ToString();

        double avg = 0f;

        if (hero.myEquipment.IsWeaponRanged(hero.myEquipment.GetWeapon()))
        {
            avg = hero.cachedBattleData.critRangedChance;
        }
        else
        {
            avg = hero.cachedBattleData.critMeleeChance;
        }

        //double avg = (hero.cachedBattleData.critMeleeChance + hero.cachedBattleData.critRangedChance) / 2f;
        float critDisp = (float)Math.Round(avg, 2) * 100f;



        if (critDisp < 0)
        {
            critDisp = 0;
        }

        /* if (hero.myStats.CheckHasActiveStatusName("status_heavyguard"))
        {
            StatusEffect se = hero.myStats.GetStatusByRef("status_heavyguard");
            if (hero.myStats.GetCurStat(StatTypes.STAMINA) >= se.staminaReq)
            {
                critDisp /= 2f;
            }
        } */

        if (critDisp > CombatManagerScript.CRIT_CHANCE_MAX * 100f)
        {
            critDisp = CombatManagerScript.CRIT_CHANCE_MAX * 100f;
        }

        csCritChance.text = ((int)critDisp).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        avg = (hero.cachedBattleData.blockMeleeChance + hero.cachedBattleData.blockRangedChance) / 2f;
        float blockDisp = (float)Math.Round(avg, 2) * 100f;

        foreach (StatusEffect se in hero.myStats.GetAllStatuses())
        {
            if ((se.listEffectScripts == null) || (se.listEffectScripts.Count == 0)) continue;

            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.EMPOWERATTACK)
                {
                    EmpowerAttackEffect eae = eff as EmpowerAttackEffect;

                    if (eae.parentAbility.reqWeaponType != WeaponTypes.ANY && eae.parentAbility.reqWeaponType != GameMasterScript.heroPCActor.myEquipment.GetWeaponType())
                    {
                        continue;
                    }
                    if (eae.extraChanceToCritFlat != 0f) // Add extra logic for conditionals here.
                    {
                        critDisp += (eae.extraChanceToCritFlat * 100f);
                    }
                    if (eae.extraChanceToCrit != 1f)
                    {
                        critDisp *= eae.extraChanceToCrit;
                    }
                }
                else if (eff.effectType == EffectType.ATTACKREACTION)
                {
                    AttackReactionEffect area = eff as AttackReactionEffect;
                    if (area.alterBlockFlat != 0f) // Add extra logic for conditionals here.
                    {
                        blockDisp += (area.alterBlockFlat * 100f);
                    }
                    if (area.alterBlock != 1f)
                    {
                        blockDisp *= area.alterBlock;
                    }
                }
            }
        }

        if ((hero.myStats.CheckHasActiveStatusName("status_heavyguard")) && (GameMasterScript.heroPCActor.myEquipment.GetOffhandBlock() > 0f))
        {
            StatusEffect se = hero.myStats.GetStatusByRef("status_heavyguard");
            if (hero.myStats.GetCurStat(StatTypes.STAMINA) >= se.staminaReq)
            {
                //blockDisp *= 1.25f;
                blockDisp += 20f;
                blockDisp = (int)blockDisp;
            }
        }

        csBlockChance.text = ((int)blockDisp).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);


        float dodgeDisp = (int)Math.Abs((hero.CalculateDodge()));

        csDodgeChance.text = ((int)dodgeDisp).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        float spiritPowerDisplay = hero.cachedBattleData.spiritPower;

        // Hardcoded status
        if ((hero.myStats.CheckHasStatusName("status_kineticmagic")) && (hero.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.51f))
        {
            spiritPowerDisplay *= 1.25f;
        }

        csSpiritPower.text = ((int)spiritPowerDisplay).ToString();
        csCritDamage.text = ((int)((hero.cachedBattleData.critMeleeDamageMult - 1f) * 100f)).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        csWeaponPower.text = ((int)hero.cachedBattleData.physicalWeaponDamage).ToString();
        csCritDamage.text = ((int)((hero.cachedBattleData.critMeleeDamageMult - 1f) * 100f)).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        int dispCTGain = (int)hero.cachedBattleData.chargeGain - 100;

        csChargeTime.text = dispCTGain.ToString();

        float calcPowerupChance = (GameMasterScript.gmsSingleton.globalPowerupDropChance * 100f) + hero.myStats.GetCurStat(StatTypes.GUILE) / 4f;
        float extraChance = (hero.myStats.CheckStatusQuantity("status_mmscavenging") * 400f); // Scavenging improves drop chance by 4%        

        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("sthergebonus1"))
        {
            extraChance += 20f;
        }

        float percent = 0.0f;
        float offset = 0.0f;
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            percent = hero.cachedBattleData.resistances[i].multiplier; // 0.1 = 90% resistance, 1.1 = -10%
            percent = (1 - percent) * 100f;
            percent = (int)percent;
            offset = (int)hero.cachedBattleData.resistances[i].flatOffset;
            string nString = percent + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

            if (offset != 0)
            {
                nString += " (" + offset + ")";
            }

            float damagePercent = hero.cachedBattleData.damageExternalMods[i];
            damagePercent += (hero.cachedBattleData.temporaryDamageMods[i] - 1f);
            damagePercent -= 1f;
            damagePercent *= 100f;

            damagePercent = (float)Math.Round(damagePercent, 1);

            string damageString = (int)damagePercent + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

            if (damagePercent >= 1f)
            {
                damageString = cyanHexColor + "+" + damageString + "</color>";
            }
            if (damagePercent < -1f)
            {
                damageString = redHexColor + damageString + "</color>";
            }


            switch (i)
            {
                case (int)DamageTypes.PHYSICAL:
                    csPhysicalResist.text = nString;
                    csPhysicalDamage.text = damageString;
                    break;
                case (int)DamageTypes.FIRE:
                    csFireResist.text = nString;
                    csFireDamage.text = damageString;
                    break;
                case (int)DamageTypes.WATER:
                    csWaterResist.text = nString;
                    csWaterDamage.text = damageString;
                    break;
                case (int)DamageTypes.LIGHTNING:
                    csLightningResist.text = nString;
                    csLightningDamage.text = damageString;
                    break;
                case (int)DamageTypes.SHADOW:
                    csShadowResist.text = nString;
                    csShadowDamage.text = damageString;
                    break;
                case (int)DamageTypes.POISON:
                    csPoisonResist.text = nString;
                    csPoisonDamage.text = damageString;
                    break;
            }
        }
        csParryChance.text = ((int)hero.CalculateAverageParry()).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        float damageMod = hero.allDamageMultiplier;
        float defenseMod = hero.allMitigationAddPercent;

        damageMod -= 1f;
        damageMod *= 100f;
        defenseMod -= 1f;

        defenseMod *= -1f; // Since a higher number means we take MORE damage

        defenseMod *= 100f;

        damageMod = (float)Math.Round(damageMod, 2);
        defenseMod = (float)Math.Round(defenseMod, 2);

        //Debug.Log(damageMod + " " + defenseMod);

        if (damageMod <= -1f)
        {
            csDamageMod.text = redHexColor + damageMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else if (damageMod >= 1f)
        {
            csDamageMod.text = greenHexColor + "+" + damageMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else
        {
            csDamageMod.text = "0" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }

        if (defenseMod <= -1f)
        {
            csDefenseMod.text = redHexColor + defenseMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else if (defenseMod >= 1f)
        {
            csDefenseMod.text = greenHexColor + "+" + defenseMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else
        {
            csDefenseMod.text = "0" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }


        int dispChance = (int)(calcPowerupChance + extraChance);

        csPowerupDrop.text = ((int)dispChance).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        csName.text = GameMasterScript.heroPCActor.displayName;
        csLevel.text = GameMasterScript.characterJobNames[(int)hero.myJob.jobEnum];

        string progression = ""; // "<color=yellow>Progress</color>\n\n" + hero.displayName + "\n";        

        progression = GameMasterScript.heroPCActor.myStats.GetLevel() + "\n";
        progression += hero.myStats.GetXP() + "\n";

        int calcXPDisplay = hero.myStats.GetXPToNextLevel();

        progression += calcXPDisplay;

        csExperience.text = progression;

        progression = "";


        progression += StringManager.GetString("misc_days_passed") + ": " + MetaProgressScript.totalDaysPassed + "\n";
        int disp = hero.lowestFloorExplored + 1;

        progression = disp + "\n";
        progression += (MetaProgressScript.lowestFloorReached + 1) + "\n";
        progression += MetaProgressScript.totalDaysPassed;
        csExplore.text = progression;

        progression = "";
        progression = hero.monstersKilled + "\n";
        progression += hero.championsKilled + "\n";
        progression += hero.stepsTaken;
        csMonster.text = progression;

        progression = "";

        CharacterJobs highest = CharacterJobs.COUNT;
        int highestCount = 0;
        for (int i = 0; i < (int)CharacterJobs.COUNT; i++)
        {
            if (MetaProgressScript.jobsStarted[i] > highestCount)
            {
                highestCount = MetaProgressScript.jobsStarted[i];
                highest = (CharacterJobs)i;
            }
        }

        progression = highest + "\n";
        progression += MetaProgressScript.totalCharacters + "\n";
        progression += MetaProgressScript.GetDisplayPlayTime(false, GameMasterScript.heroPCActor.GetPlayTime());

        csFavorites.text = progression;

        string positiveStatuses = "";
        string negativeStatuses = "";
        string statusString;
        int countStatuses = 0;

        Dictionary<string, int> dictPositives = new Dictionary<string, int>();
        Dictionary<string, int> dictNegatives = new Dictionary<string, int>();

        foreach (StatusEffect se in GameMasterScript.heroPCActor.myStats.GetAllStatuses())
        {
            if (!se.showIcon) continue;
            if (se.passiveAbility) continue;
            countStatuses++;
            if (se.isPositive)
            {
                string durString = "";

                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                {
                    durString = " (" + se.curDuration + "t)";
                }

                string addString = greenHexColor + se.abilityName + ": " + se.GetLiveDescription() + durString + "</color>";
                if (dictPositives.ContainsKey(addString))
                {
                    dictPositives[addString]++;
                }
                else
                {
                    dictPositives.Add(addString, 1);
                }

            }
            else
            {
                string durString = "";

                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                {
                    durString = " (" + se.curDuration + "t)";
                }

                string addString = redHexColor + se.abilityName + ": " + se.GetLiveDescription() + durString + "</color>";

                if (dictNegatives.ContainsKey(addString))
                {
                    dictNegatives[addString]++;
                }
                else
                {
                    dictNegatives.Add(addString, 1);
                }
            }
        }

        if (countStatuses > 0)
        {
            statusString = StringManager.GetString("ui_current_statuses") + "\n\n"; // was adding size before, but don't do that

            foreach (string key in dictPositives.Keys)
            {
                if (dictPositives[key] == 1)
                {
                    positiveStatuses += key + "\n";
                }
                else
                {
                    positiveStatuses += key + " (" + dictPositives[key] + "x)\n";
                }
            }
            foreach (string key in dictNegatives.Keys)
            {
                if (dictNegatives[key] == 1)
                {
                    negativeStatuses += key + "\n";
                }
                else
                {
                    negativeStatuses += key + " (" + dictNegatives[key] + "x)\n";
                }
            }

            if (positiveStatuses != "")
            {
                statusString += positiveStatuses;
                if (negativeStatuses != "")
                {
                    statusString += "\n" + negativeStatuses;
                }
            }
            else
            {
                statusString += negativeStatuses;
            }
            statusString += "</size>";
        }
        else
        {
            statusString = "";
        }

        csStatusEffects.text = statusString;

        string featsText = "";

        if (GameMasterScript.heroPCActor.heroFeats.Count > 0)
        {
            featsText = "<#fffb00>" + StringManager.GetString("misc_feats_plural") + ":</color>";
        }

        bool hasKeenEyes = false;
        foreach (string feat in GameMasterScript.heroPCActor.heroFeats)
        {
            if (feat == "skill_keeneyes") hasKeenEyes = true;
            CreationFeat cf = CreationFeat.FindFeatBySkillRef(feat);
            if (cf != null)
            {
                featsText += "\n\n" + greenHexColor + cf.featName + "</color>: " + cf.description;
            }
        }

        if ((!hasKeenEyes) && (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("mmdungeondigest")))
        {
            AbilityScript theFeat = AbilityScript.GetAbilityByName("skill_keeneyes");
            if (theFeat != null)
            {
                featsText += "\n\n" + greenHexColor + theFeat.abilityName + "</color>: " + theFeat.description;
            }
        }

        csFeatsText.text = featsText;

        string moneyText = "<#fffb00>" + GameMasterScript.heroPCActor.GetMoney() + "</color>g";

        string moneyLabel = StringManager.GetString("ui_money");

        if (GameMasterScript.heroPCActor.numPandoraBoxesOpened > 0)
        {
            moneyText += "\n" + GameMasterScript.heroPCActor.numPandoraBoxesOpened;
            moneyLabel += "\n" + StringManager.GetString("ui_pandora_opened");
        }

        csMoney.text = moneyText;
        csMoneyLabel.text = moneyLabel;
    }

    public static void ToggleCharacterSheet()
    {

        if (!GetWindowState(UITabs.CHARACTER))
        {
            // Open it
            SetWindowState(UITabs.CHARACTER, true);
            TDScrollbarManager.SMouseExitedSpecialScrollArea();
            //OpenFullScreenUI(UITabs.CHARACTER);
            singletonUIMS.CloseAllDialogsExcept(UITabs.CHARACTER);
            SetWindowState(UITabs.CHARACTER, true);
            singletonUIMS.ShowUINavigation();
            UpdateUINavHighlights();
            characterSheet.SetActive(GetWindowState(UITabs.CHARACTER));
            charSheetStatImage.SetActive(false);
            UpdatePlayerCharacterSheet();

            allUIObjects.Clear();
            for (int i = 0; i < csObjectButtons.Length; i++)
            {
                allUIObjects.Add(csObjectButtons[i]);
            }

            ChangeUIFocusAndAlignCursor(csStrengthUIObject);
            ShowDialogMenuCursor();
            singletonUIMS.EnableCursor();
        }
        else
        {
            //characterSheet.SetActive(GetWindowState(UITabs.CHARACTER));
            // Close it
            singletonUIMS.DisableCursor();
            HideDialogMenuCursor();
            //singletonUIMS.HideUINavigation();
            CleanupAfterUIClose(UITabs.CHARACTER);
        }
    }

    public static void ToggleQuestSheet(bool forceClose = false)
    {
        if (!GetWindowState(UITabs.RUMORS) && !forceClose)
        {
            SetWindowState(UITabs.RUMORS, true);
            singletonUIMS.CloseAllDialogsExcept(UITabs.RUMORS);
            questSheet.SetActive(true);
            singletonUIMS.ShowUINavigation();
            UpdateUINavHighlights();
            UpdateQuestSheet();
            TDInputHandler.OnDialogOrFullScreenUIOpened();
            GuideMode.OnFullScreenUIOpened();
        }
        else
        {
            // Close it
            CleanupAfterUIClose(UITabs.RUMORS);
        }
    }

    public void CloseAllDialogsExcept(UITabs tab)
    {
        TDScrollbarManager.SMouseExitedSpecialScrollArea();
        ExitTargeting();
        if (GameMasterScript.playerDied) return;

        //Equipment, Inventory, Skills all use the new UI system

        if (tab != UITabs.CHARACTER)
        {
            CloseCharacterSheet();
        }
        if (tab != UITabs.SHOP)
        {
            ShopUIScript.CloseShopInterface();
        }
        if (tab != UITabs.RUMORS)
        {
            CloseQuestSheet();
        }
        if (tab != UITabs.OPTIONS)
        {
            CloseOptionsMenu();
        }
        if (tab != UITabs.COOKING)
        {
            CloseCookingInterface();
        }
        CloseDialogBox();
        CloseExamineMode();
        CloseSlotsGame();
        //HideUINavigation();
        ItemWorldUIScript.singleton.CloseItemWorldInterface(0);
        CharCreation.singleton.EndCharCreation();
    }

    public void CloseAllDialogs()
    {
        CloseAllDialogsExcept(UITabs.NONE);
    }

    // This simply disables the UI game object itself.
    static void CloseUIByTab(UITabs tab)
    {
        if (tab == UITabs.NONE)
        {
            // Closing something like shop interface, which isn't a main menu object.
            return;
        }

        //if tabs is the NEW ui stuff, it won't be in here
        if (menuScreenObjects[(int)tab] != null)
        {
            SetWindowState(tab, false);
            menuScreenObjects[(int)tab].SetActive(false);
        }
    }

    IEnumerator WaitThenCloseTab(float time, UITabs tab)
    {
        yield return new WaitForSeconds(time);
        CloseUIByTab(tab);
    }

    // All calls that CLOSE main menu tabs/windows should now funnel through this function
    // This is so we can do a nice fade-out effect regardless of what window we closed
    // However, fades should not be done if the game is loading    
    // Checking for anim playing also ensures we don't end up with multiple fades happening at once
    public static void CleanupAfterUIClose(UITabs tab)
    {
        if (GameMasterScript.gameLoadSequenceCompleted)
        {
            foreach (HotbarBindable hb in hotbarAbilities)
            {
                if (hb.actionType == HotbarBindableActions.CONSUMABLE && hb.consume != null)
                {
                    hb.MarkDirty();
                }
            }
        }

        //Debug.Log("Game requests to close " + tab + " and current selected tab is " + uiTabSelected);
        CloseUIByTab(tab);
        UIManagerScript.ClearDefaultUIFocus();

        /*
        //If the game is loaded, we're not playing an animation, and we're closing the current active tab, 
        if ( GameMasterScript.gameLoadSequenceCompleted && 
             !GameMasterScript.gmsSingleton.animationPlaying && 
             uiTabSelected == tab &&
             GetWindowState(tab)) //<-- don't need this
        {
            SetWindowState(tab, false);
        }
        else
        {
            CloseUIByTab(tab);
        }
        */

        CheckConversationQueue();
        singletonUIMS.CloseHotbarNavigating();
    }

    public void ToggleCharacterSheetFromButton()    //#question: keep this function?
    {
        if (GameMasterScript.playerDied) return;
        ExitTargeting();
        MinimapUIScript.StopOverlay();
        ToggleCharacterSheet();
    }

    public void ToggleOptionsSheetFromButton()  //#question: keep this function?
    {
        if (GameMasterScript.playerDied) return;
        ExitTargeting();
        MinimapUIScript.StopOverlay();
        ToggleOptionsMenu();
    }

    public static void CloseCharacterSheet()
    {
        singletonUIMS.CloseExamineMode();
        //GetWindowState(UITabs.CHARACTER) = false;
        //characterSheet.SetActive(false);
        singletonUIMS.DisableCursor();
        //HideUINavigation();
        CleanupAfterUIClose(UITabs.CHARACTER);
    }

    public void CloseQuestSheetFromButton()
    {
        singletonUIMS.CloseExamineMode();
        CloseFullScreenUI();

    }

    public void CloseQuestSheetFromButtonDummy(int dummy)
    {
        singletonUIMS.CloseExamineMode();
        CloseFullScreenUI();
    }

    public void CloseOptionsSheetFromButton()
    {
        singletonUIMS.CloseExamineMode();
        CloseFullScreenUI();
    }

    public static void CloseQuestSheet()
    {
        singletonUIMS.CloseExamineMode();
        //GetWindowState(UITabs.RUMORS) = false;
        //questSheet.SetActive(false);
        singletonUIMS.DisableCursor();
        //singletonUIMS.HideUINavigation();
        CleanupAfterUIClose(UITabs.RUMORS);
    }

    public static void CloseJobSheet()
    {
        singletonUIMS.CloseExamineMode();
        jobSheetOpen = false;
        jobSheet.SetActive(false);
        singletonUIMS.DisableCursor();
        //singletonUIMS.HideUINavigation();
        CleanupAfterUIClose(UITabs.SKILLS);
    }

    public static void CloseEquipmentSheet()
    {
    }

    public static void TryCloseInventorySheet()
    {
        UIManagerScript.TryCloseFullScreenUI();
    }

    public static void TryCloseSkillSheet()
    {
        UIManagerScript.TryCloseFullScreenUI();
    }

    public static void CloseInventorySheet()
    {
        if (GetWindowState(UITabs.INVENTORY))
        {
            SaveInventoryUIState();
        }

        singletonUIMS.ExitDragMode();
        singletonUIMS.CloseExamineMode();
        HideEQBlinkingCursor();
        //GetWindowState(UITabs.INVENTORY) = false;
        //inventorySheet.SetActive(false);
        singletonUIMS.DisableCursor();
        //invDragger.SetActive(false);
        invSubmenuOpen = false;
        HideInvTooltipContainer();
        //singletonUIMS.HideUINavigation();
        CleanupAfterUIClose(UITabs.INVENTORY);
    }

    public static bool CheckEquipmentSheetState()
    {
        return GetWindowState(UITabs.EQUIPMENT);
    }

    public static bool CheckSkillSheetState()
    {
        return GetWindowState(UITabs.SKILLS);
    }

    public static bool CheckInventorySheetState()
    {
        return GetWindowState(UITabs.INVENTORY);
    }

    public static bool CheckJobSheetState()
    {
        return jobSheetOpen;
    }

    public static bool AnyInteractableWindowOpenExceptDialog()
    {
        if (GameMasterScript.applicationQuittingOrChangingScenes == true) return false;
        if (GetWindowState(UITabs.EQUIPMENT) || jobSheetOpen || GetWindowState(UITabs.INVENTORY)
            || GetWindowState(UITabs.SKILLS) || GetWindowState(UITabs.OPTIONS) ||
            ShopUIScript.CheckShopInterfaceState() || casinoGameOpen ||
            GetWindowState(UITabs.RUMORS) || GetWindowState(UITabs.COOKING) ||
            GetWindowState(UITabs.CHARACTER))
        {
            return true;
        }

        if (GameStartData.CurrentLoadState == LoadStates.BACK_TO_TITLE || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY
            || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUS || GameStartData.CurrentLoadState == LoadStates.PLAYER_VICTORY_NGPLUSPLUS)
        {
            return false;
        }

        if (ItemWorldUIScript.itemWorldInterfaceOpen) return true;
        if (MonsterCorralScript.corralInterfaceOpen) return true;
        if (CorralBreedScript.corralBreedInterfaceOpen) return true;
        if (MonsterCorralScript.monsterStatsInterfaceOpen) return true;
        if (MonsterCorralScript.corralFoodInterfaceOpen) return true;
        if (CharCreation.creationActive) return true;
        if (MonsterCorralScript.corralGroomingInterfaceOpen) return true;
        if (!GameMasterScript.actualGameStarted)
        {
            if (TitleScreenScript.CreateStage == CreationStages.NAMEINPUT) return true;
        }

        if (singletonUIMS != null)
        {
            if (singletonUIMS.currentFullScreenUI != null) return true;
            if (singletonUIMS.CheckHotbarNavigating()) return true;
        }

        return false;
    }

    public static bool AnyInteractableWindowOpen()
    {
        if (AnyInteractableWindowOpenExceptDialog()) return true;
        if (dialogBoxOpen) return true;
        return false;
    }

    public static bool CheckDialogBoxState()
    {
        return dialogBoxOpen;
    }

    public static void CloseOptionsMenu()
    {
        //GetWindowState(UITabs.OPTIONS) = false;
        //uiOptionsMenu.SetActive(false);
        singletonUIMS.DisableCursor();
        //singletonUIMS.HideUINavigation();
        CleanupAfterUIClose(UITabs.OPTIONS);
    }

    public static void ToggleOptionsMenu(bool forceClose = false)
    {

        //if (Debug.isDebugBuild) Debug.Log("Options menu turning on.");

        if (!PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
        {
            worldSeedText.text = StringManager.GetString("ui_text_worldseed") + " " + GameMasterScript.gmsSingleton.gameRandomSeed;
        }

        UpdateResolutionText();
        if (highlightingOptionsObject)
        {
            highlightingOptionsObject = false;
            highlightedOptionsObject.GetComponent<Image>().color = transparentColor;
            UpdateOptionsSlidersDirectionalActions();
        }

        movingSliderViaKeyboard = false;

        //TogglePlayerHUD();
        singletonUIMS.CloseExamineMode();

        if (GetWindowState(UITabs.OPTIONS) || forceClose)
        {
            //uiOptionsMenu.SetActive(false);
            //singletonUIMS.HideUINavigation();
            CleanupAfterUIClose(UITabs.OPTIONS);
        }
        else
        {
            SetWindowState(UITabs.OPTIONS, true);
            singletonUIMS.CloseAllDialogsExcept(UITabs.OPTIONS);
            TDScrollbarManager.SMouseExitedSpecialScrollArea();            
            allUIObjects.Clear();

            for (int i = 0; i < uiOptionsObjects.Count; i++)
            {
                allUIObjects.Add(uiOptionsObjects[i]);
            }

            if (!LogoSceneScript.AllowFullScreenSelection())
            {
                optionsFullscreen.gameObj.SetActive(false);                
            }
            if (!LogoSceneScript.AllowResolutionSelection())
            {
                optionsResolution.gameObj.SetActive(false);
                optionsResolutionContainer.gameObject.SetActive(false);
            }          
            if (LogoSceneScript.globalIsSolsticeBuild || LogoSceneScript.globalSolsticeDebug)
            {
                optionsFramecap.gameObj.SetActive(false);
                optionsFrameCapContainer.gameObject.SetActive(false);
            }

            SetListOffset(0);
            ChangeUIFocus(uiOptionsObjects[0]);
            AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
            uiOptionsMenu.SetActive(true);

            singletonUIMS.ShowUINavigation();
            UpdateUINavHighlights();

            ShowDialogMenuCursor();
            singletonUIMS.uiDialogMenuCursor.transform.SetParent(uiOptionsMenu.transform);
            ReadOptionsMenuVariables();
            UpdateFrameCapText();
            UpdateTextSpeedText();
            UpdateBattleTextScaleText();
            UpdateCursorRepeatDelayText();
            UpdateButtonDeadZoneText();
            if (dialogBoxOpen)
            {
                ToggleDialogBox(DialogType.EXIT, false, false);
            }

            if (LogoSceneScript.globalIsSolsticeBuild)
            {
                ChangeUIFocusAndAlignCursor(optionsMusicVolume);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(optionsResolution);
            }

            
        }
    }

    public static bool CheckOptionsMenuState()
    {
        return GetWindowState(UITabs.OPTIONS);
    }
}
