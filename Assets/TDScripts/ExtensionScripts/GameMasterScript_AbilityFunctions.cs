using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript
{
    // For UI button.
    public void UseAbilityRegenFlask()
    {
        if (IsGameInCutsceneOrDialog()) return;
        if (uims.CheckTargeting()) return;
        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA) return;
        CheckAndTryAbility(regenFlaskAbility);
    }

    public void UseAbilityEscapeTorch()
    {
        if (IsGameInCutsceneOrDialog()) return;
        CheckAndTryAbility(escapeTorchAbility);
    }


    /// <summary>
    /// Special code to handle special cases for the portal ability. Returns TRUE if portal ability was handled here.
    /// </summary>
    /// <param name="abil"></param>
    bool ProcessEscapeAbility(AbilityScript abil)
    {
        //Track that we have used a portal at least one time.
        MetaProgressScript.SetMetaProgress("portalused", 1);

        //If you are in a job trial, let the player know the consequences of porting out during a trial.
        if (JobTrialScript.IsJobTrialActive() & ReadTempGameData("confirm_jobtrial_useportal") != 1)
        {
            UIManagerScript.StartConversationByRef("confirm_use_portal_jobtrial", DialogType.STANDARD, null);
            return true;
        }

        //is it possible for town portal to be sealed?
        

        // Portal is normally disabled in Mystery Dungeons, so if we are in one and have yet to be
        // victorious, disable the casting.
        if (MapMasterScript.activeMap.IsMysteryDungeonMap() )
        {
            if (!heroPCActor.myMysteryDungeonData.dungeonVictory)
            {
                UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("exp_log_error_noportal_mysterydungeon");
                return true;
            }
            // Otherwise, let the player exit the dungeon via portal.
        }
        
        
        //Disallow portals in campfire levels, as well as boss floors under certain conditions.
        bool cantUsePortal = heroPCActor.dungeonFloor == MapMasterScript.CAMPFIRE_FLOOR;

        if (heroPCActor.dungeonFloor == MapMasterScript.FINAL_BOSS_FLOOR)
        {
            if (heroPCActor.ReadActorData("finalboss1") <= 1)
            {
                cantUsePortal = true;
            }
        }

        if (heroPCActor.dungeonFloor == MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            if (heroPCActor.ReadActorData("finalboss2") <= 1)
            {
                cantUsePortal = true;
            }
        }

        if (cantUsePortal)
        {
            heroPCActor.myStats.RemoveStatusByRef("status_escapedungeon");
            GameLogScript.LogWriteStringRef("log_nocampfireportal");
            return true;
        }

        //If we are not in a town area, maybe we're warping back to a side entrance.
        if (heroPCActor.dungeonFloor != MapMasterScript.TOWN_MAP_FLOOR &&
            heroPCActor.dungeonFloor != MapMasterScript.TOWN2_MAP_FLOOR &&
            heroPCActor.dungeonFloor != MapMasterScript.SHARA_START_CAMPFIRE_FLOOR )
        {
            if (heroPCActor.myStats.CheckHasStatusName("status_escapedungeon") &&
                heroPCActor.ReadActorData("sideareawarp") != 1)
            {
                heroPCActor.myStats.RemoveStatusByRef("status_escapedungeon");
                GameLogScript.LogWriteStringRef("log_cancel_portal");
                return true;
            }

            if (heroPCActor.ReadActorData("sideclear" + heroPCActor.dungeonFloor) == 1 ||
                MapMasterScript.activeMap.dungeonLevelData.safeArea ||
                (MapMasterScript.activeMap.IsClearableSideArea() &&
                 MapMasterScript.activeMap.unfriendlyMonsterCount <= 0) || 
                 MapMasterScript.activeMap.floor == 107) // stupid hardcoded exception for Bottles n' Brews
            {
                if (!MapMasterScript.activeMap.IsItemWorld())
                {
                    Conversation cWarper = FindConversation("sidearea_portal");
                    heroPCActor.SetActorData("sideareawarp", 1);
                    UIManagerScript.StartConversation(cWarper, DialogType.TUTORIAL, null);
                    return true;
                }
            }
        }

        //Shara does something different with her portals.
        if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            GameLogScript.LogWriteStringRef("exp_log_shara_noportal");
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return true;
        }

        //If we're in town, set various flags based on the state of the existing portal
        //and the item world portal.
        if (heroPCActor.GetActorMap().IsTownMap())
        {
            Stairs mainDungeonPortal = null;
            foreach (Actor act in mms.townMap.mapStairs)
            {
                if (act.GetActorType() == ActorTypes.STAIRS)
                {
                    Stairs st = act as Stairs;
                    if (st.isPortal)
                    {
                        mainDungeonPortal = st;
                        break;
                    }
                }
            }

            Stairs itemDreamPortal = null;
            if (MapMasterScript.itemWorldOpen)
            {
                foreach (Actor act in mms.townMap2.mapStairs)
                {
                    if (act.GetActorType() == ActorTypes.STAIRS)
                    {
                        Stairs st = act as Stairs;
                        if (st.isPortal)
                        {
                            itemDreamPortal = st;
                            break;
                        }
                    }
                }
            }

            if (mainDungeonPortal != null)
            {
                heroPCActor.SetActorData("td_portal_open", 1);
            }
            else
            {
                heroPCActor.RemoveActorData("td_portal_open");
            }

            if (itemDreamPortal != null)
            {
                heroPCActor.SetActorData("id_portal_open", 1);
            }
            else
            {
                heroPCActor.RemoveActorData("id_portal_open");
            }

            UIManagerScript.StartConversationByRef("portal_in_town", DialogType.WAYPOINT, null);
            return true;
        }

        return false;
    }

    // This is where we check fundamental requirements to use an ability (right weapon type / location)
    public void CheckAndTryAbility(AbilityScript abil)
    {
        if (playerDied) return;
        if (!actualGameStarted) return;
        if (animationPlaying) return;

        unmodifiedAbility = null;

        if (abil.refName == "skill_escapedungeon")
        {
            if (ProcessEscapeAbility(abil))
            {
                // Ability was handled in above function, return here.
                return;
            }
            // Otherwise, it wasn't handled, continue as normal.
            
        }

        if (JobTrialScript.IsJobTrialActive())
        {
            if (!JobTrialScript.CanPlayerUseAbilityDuringTrial(abil) &&
                abil.refName != "skill_regenflask" && abil.refName != "skill_escapedungeon")
            {
                UIManagerScript.DisplayPlayerError(heroPCActor);
                StringManager.SetTag(0, heroPCActor.myJob.DisplayName);
                GameLogScript.LogWriteStringRef("log_error_jobtrial_ability");
                return;
            }
        }

        if (MapMasterScript.GetItemWorldAura(heroPCActor.GetPos()) == (int) ItemWorldAuras.PLAYERSEALED)
        {
            GameLogScript.LogWriteStringRef("log_error_crystal_seal");
            return;
        }

        if (abil.refName.Contains("crystalshift"))
        {
            if (heroPCActor.GetSummonByRef("mon_runiccrystal") == null)
            {
                StringManager.SetTag(0, abil.abilityName);
                GameLogScript.LogWriteStringRef("log_error_nocrystal");
                return;
            }
        }

        if (abil.reqWeaponType != WeaponTypes.ANY)
        {
            if (abil.reqWeaponType != GameMasterScript.heroPCActor.myEquipment.GetWeaponType())
            {
                // Let's try to switch to that weapon. If we don't have it, then display the error
                //todo Make this an option.
                if (SwitchToFirstWeaponOfCondition(w => w.weaponType == abil.reqWeaponType) == null)
                {
                    StringManager.SetTag(0, Weapon.weaponTypesVerbose[(int) abil.reqWeaponType]);
                    StringManager.SetTag(1, abil.abilityName);
                    GameLogScript.GameLogWrite(StringManager.GetString("log_error_abilityweapontype"),
                        GameMasterScript.heroPCActor);
                    UIManagerScript.DisplayPlayerError(heroPCActor);
                    return;
                }
            }
        }

        if (abil.abilityFlags[(int) AbilityFlags.THANEVERSE])
        {
            if (heroPCActor.myStats.CountStatusesByFlag(StatusFlags.THANESONG) == 0)
            {
                StringManager.SetTag(0, abil.abilityName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_error_thaneverse"),
                    GameMasterScript.heroPCActor);
                UIManagerScript.DisplayPlayerError(heroPCActor);
                return;
            }
        }

        if (heroPCActor.myStats.CheckIfSealed() && itemBeingUsed == null && abil.refName != "skill_regenflask")
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_error_cantuseabilities"), heroPCActor);
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (abil.CheckAbilityTag(AbilityTags.REQUIREMELEE))
        {
            //todo Perhaps make auto-switch to melee/ranged an option
            if (heroPCActor.myEquipment.GetWeapon().isRanged &&
                SwitchToFirstWeaponOfCondition(w => !w.isRanged) == null)
                //if (heroPCActor.myEquipment.IsWeaponRanged(heroPCActor.myEquipment.GetWeapon()))
            {
                StringManager.SetTag(0, abil.abilityName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_error_abilityrequiremelee"),
                    GameMasterScript.heroPCActor);
                UIManagerScript.DisplayPlayerError(heroPCActor);
                return;
            }
        }

        if (abil.CheckAbilityTag(AbilityTags.REQUIRERANGED))
        {
            //todo Perhaps make auto-switch to melee/ranged an option
            if (!heroPCActor.myEquipment.GetWeapon().isRanged &&
                SwitchToFirstWeaponOfCondition(w => w.isRanged) == null)
                //if (heroPCActor.myEquipment.IsWeaponRanged(heroPCActor.myEquipment.GetWeapon()))
            {
                StringManager.SetTag(0, abil.abilityName);
                GameLogScript.GameLogWrite(StringManager.GetString("log_error_abilityrequireranged"),
                    GameMasterScript.heroPCActor);
                UIManagerScript.DisplayPlayerError(heroPCActor);
                return;
            }
        }

        if (abil.refName == "skill_shieldslam" && !heroPCActor.myEquipment.IsOffhandShield())
        {
            StringManager.SetTag(0, abil.abilityName);
            GameLogScript.LogWriteStringRef("log_error_noshield");
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (abil.refName == "skill_wildcards" && heroPCActor.gamblerHand.Count == 0)
        {
            StringManager.SetTag(0, abil.abilityName);
            GameLogScript.LogWriteStringRef("log_error_nocards");
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (abil == rangedWeaponAbilityDummy)
        {
            abil.AddAbilityTag(AbilityTags.MONSTERAFFECTED);
        }
        else if (abil.refName == "skill_regenflask")
        {
            if (heroPCActor.regenFlaskUses == 0)
            {
                GameLogScript.LogWriteStringRef("log_error_flask_empty");
                UIManagerScript.DisplayPlayerError(heroPCActor);
                return;
            }

            if (heroPCActor.myStats.CheckHasStatusName("status_regenflask"))
            {
                GameLogScript.LogWriteStringRef("log_error_flaskinuse");
                UIManagerScript.DisplayPlayerError(heroPCActor);
                return;
            }

            bool flaskWouldFailAQuest = heroPCActor.CheckForQuestsWithRequirement(QuestRequirementTypes.NOFLASK);

            if (flaskWouldFailAQuest && gmsSingleton.ReadTempGameData("flask_confirm_fail") != 1 &&
                !MapMasterScript.activeMap.IsJobTrialFloor() && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
            {
                // prompt - are you sure you want to fail a rumor?
                UIManagerScript.StartConversationByRef("dialog_confirm_failquest_rumor", DialogType.STANDARD, null);
                return;
            }
            else if (flaskWouldFailAQuest)
            {
                gmsSingleton.SetTempGameData("flask_confirm_fail", 0);
            }

            heroPCActor.ChangeRegenFlaskUses(-1);
            statsAndAchievements.IncrementFlaskUses();
            UIManagerScript.UpdateFlaskCharges();
            if (flaskWouldFailAQuest && heroPCActor.myQuests.Count > 0)
            {
                List<QuestScript> qToRemove = new List<QuestScript>();
                foreach (QuestScript qs in heroPCActor.myQuests)
                {
                    if (qs.complete) continue;
                    if (qs.qRequirements.Count > 0)
                    {
                        foreach (QuestRequirement qr in qs.qRequirements)
                        {
                            if (qr.qrType == QuestRequirementTypes.NOFLASK &&
                                !MapMasterScript.activeMap.IsJobTrialFloor())
                            {
                                qToRemove.Add(qs);
                            }
                        }
                    }
                }

                foreach (QuestScript qs in qToRemove)
                {
                    QuestScript.HeroFailedQuest(qs);
                }

            }
        }

        if (uims.CheckTargeting())
        {
            if (abilityToTry.refName == abil.refName)
            {
                uims.ExitTargeting();
                return;
            }
        }

        // uims.ExitTargeting();
        uims.CloseHotbarNavigating();

        //Shep: this will set abilityToTry and modify the F out of it based on costs and what not.
        SetAbilityToTryWithModifiedCostsAndInformation(abil, true, ref realAbilToTry);

        //Shep: Here is where we check against costs to see if the ability can be used

        //Shep: I wish we could do this before the big Modifier function above, but we really can't
        if (abilityToTry.GetCurCooldownTurns() > 0)
        {
            StringManager.SetTag(0, abilityToTry.abilityName);
            StringManager.SetTag(1, abilityToTry.GetCurCooldownTurns().ToString());
            GameLogScript.LogWriteStringRef("log_error_cooldown");
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }


        if (abilityToTry.spiritsRequired > heroPCActor.myStats.CheckStatusQuantity("spiritcollected"))
        {
            StringManager.SetTag(0, abilityToTry.abilityName);
            StringManager.SetTag(1, abilityToTry.spiritsRequired.ToString());
            GameLogScript.LogWriteStringRef("log_error_lowechoes");
            abilityToTry.SetCurCooldownTurns(0);
            if (unmodifiedAbility != null)
            {
                unmodifiedAbility.SetCurCooldownTurns(0);
            }

            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;

        }

        if (abilityToTry.staminaCost > heroPCActor.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.CUR))
        {
            StringManager.SetTag(0, abilityToTry.abilityName);
            StringManager.SetTag(1, abilityToTry.staminaCost.ToString());
            GameLogScript.LogWriteStringRef("log_error_lowstamina");
            abilityToTry.SetCurCooldownTurns(0);
            if (unmodifiedAbility != null)
            {
                unmodifiedAbility.SetCurCooldownTurns(0);
            }

            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (abilityToTry.energyCost > heroPCActor.myStats.GetStat(StatTypes.ENERGY, StatDataTypes.CUR))
        {
            StringManager.SetTag(0, abilityToTry.abilityName);
            StringManager.SetTag(1, abilityToTry.energyCost.ToString());
            GameLogScript.LogWriteStringRef("log_error_lowenergy");
            abilityToTry.SetCurCooldownTurns(0);
            if (unmodifiedAbility != null)
            {
                unmodifiedAbility.SetCurCooldownTurns(0);
            }

            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (abilityToTry.healthCost > heroPCActor.myStats.GetCurStat(StatTypes.HEALTH))
        {
            StringManager.SetTag(0, abilityToTry.abilityName);
            StringManager.SetTag(1, abilityToTry.healthCost.ToString());
            GameLogScript.LogWriteStringRef("log_error_lowhealth");
            abilityToTry.SetCurCooldownTurns(0);
            if (unmodifiedAbility != null)
            {
                unmodifiedAbility.SetCurCooldownTurns(0);
            }

            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        // Try using the ability
        SetItemBeingUsed(null);


        try { TryAbility(abilityToTry); }

        catch (Exception e)
        {
            Debug.Log("Problem with " + abil.refName + " exception: " + e.ToString());
        }

    }

    public void SetItemBeingUsed(Item itm)
    {
        itemBeingUsed = itm;
    }

    public void SetAbilityToTryWithModifiedCostsAndInformation(AbilityScript abil, bool prepareForUseOnTurn,
        ref AbilityScript setAbility)
    {

        //check hero for remapping -- is one ability mapped over another? 
        // DON'T touch cooldowns here.
        setAbility = heroPCActor.cachedBattleData.GetRemappedAbilityIfExists(abil, heroPCActor, false);

        bool brigandBomberAbility = false;
        bool unstableMagic = false;

        int aura = MapMasterScript.GetItemWorldAura(heroPCActor.GetPos());

        if (abil.refName == "skill_unstablemagic")
        {
            unstableMagic = true;
        }

        if (heroPCActor.myStats.CheckHasStatusName("status_brigandbomber"))
        {
            if ( /*setAbility.refName == "skill_cloakanddagger" 
                || setAbility.refName == "skill_cloakanddagger_2"
                ||*/ setAbility.refName == "skill_shadowstep")
            {
                brigandBomberAbility = true;
            }
        }

        //Shep: Changed so that we always apply our cost / power / size / whatever modifiers to our powers
        //if (((setAbility.spellshift) || (setAbility.budokaMod)) || (brigandBomberAbility) || (unstableMagic) || (obsidianBand) || (necroBand) || (modsFromBattleData) || (miscMod))
        //{

        AbilityScript localCopy = new AbilityScript();

        //unmodifiedAbility = setAbility;
        localCopy = setAbility;
        AbilityScript modified = new AbilityScript();
        AbilityScript.CopyFromTemplate(modified, localCopy);
        setAbility = modified;

        if (prepareForUseOnTurn)
        {
            unmodifiedAbility = localCopy;
        }


        //#abilitycost :modify the shape and cost now before applying other modifiers
        //which can change the cost by %
        //these changes are StatusEffect.listEffectScripts and they must match the ability refName or job class to apply
        setAbility.ModifyCostAndShape(heroPCActor);

        /*
        setAbility.curCooldownTurns = abil.curCooldownTurns;
        setAbility.maxCooldownTurns = abil.maxCooldownTurns;
        */

        //Debug.Log("Modified ability. " + modsFromBattleData + " " + setAbility.spellshift + " " + unstableMagic);

        setAbility.energyCost = (int) (setAbility.energyCost * heroPCActor.cachedBattleData.energyCostMod);
        setAbility.staminaCost = (int) (setAbility.staminaCost * heroPCActor.cachedBattleData.staminaCostMod);

        //This mod is 0 by default, but if not it adds a % of our maxhealth to the cost of every power
        setAbility.healthCost += (int) (heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) *
                                        heroPCActor.cachedBattleData.healthCostMod);

        int spiritReq = setAbility.spiritsRequired;

        if (setAbility.refName == "skill_furiouscrescendo")
        {
            int numSongStacks = heroPCActor.myStats.CountStatusesByFlag(StatusFlags.THANESONG);
            setAbility.range += numSongStacks;
        }

        if (setAbility.abilityFlags[(int) AbilityFlags.SOULKEEPER])
        {
            switch (setAbility.refName)
            {
                case "skill_aetherslash":
                case "skill_aetherslash_2":
                    spiritReq = 1;
                    if (spiritReq <= GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("spiritcollected"))
                    {
                        setAbility.range += 3;
                        setAbility.spiritsRequired = 1;
                    }
                    else
                    {
                        spiritReq = 0;
                    }

                    break;
                case "skill_balefulechoes":
                    spiritReq = 1;
                    if (spiritReq <= GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("spiritcollected"))
                    {
                        setAbility.repetitions = 1;
                        setAbility.spiritsRequired = 1;
                    }
                    else
                    {
                        spiritReq = 0;
                    }

                    break;
            }
        }

        if (aura == (int) ItemWorldAuras.RESOURCEMINUS50)
        {
            setAbility.energyCost /= 2;
            setAbility.staminaCost /= 2;
        }

        float baseHealthCost = 0;
        if (setAbility.percentCurHealthCost > 0)
        {
            baseHealthCost = setAbility.percentCurHealthCost * heroPCActor.myStats.GetCurStat(StatTypes.HEALTH);
        }

        if (setAbility.percentMaxHealthCost > 0)
        {
            baseHealthCost += setAbility.percentMaxHealthCost *
                              heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
        }

        //Add the base health cost, the percent of max health requested by the power, and (added above) any % of max health requested by statuses
        //on the hero
        setAbility.healthCost = (int) baseHealthCost + setAbility.healthCost;

        // These should never cost health for any reason, regardless of what effects the player has.
        if (setAbility.refName == "skill_escapedungeon" || setAbility.refName == "skill_regenflask")
        {
            setAbility.healthCost = 0;
        }
        
        //Some modifiers force us to pay an extra percent of cost. Modifiers that say 
        //"costs +X stamina" still apply even if the power only uses Energy, and vice-versa.
        float fForcedStamina = heroPCActor.cachedBattleData.forcedStaminaCosts;
        float fForcedEnergy = heroPCActor.cachedBattleData.forcedEnergyCosts;

        setAbility.staminaCost += (int) ((setAbility.staminaCost > 0)
            ? (fForcedStamina * setAbility.staminaCost)
            : (fForcedStamina * setAbility.energyCost));
        
        setAbility.energyCost += (int) ((setAbility.energyCost > 0)
            ? (fForcedEnergy * setAbility.energyCost)
            : (fForcedEnergy * setAbility.staminaCost));

        /*
        if (obsidianBand)
        {
            //setAbility.energyCost = (int)(setAbility.energyCost * 0.8f);
            if (setAbility.staminaCost == 0)
            {
                setAbility.staminaCost = (int)(setAbility.energyCost * 0.1f);
            }
            else
            {
                setAbility.staminaCost += (int)(setAbility.staminaCost * 0.1f);
            }
        }
        */
        if (setAbility.refName.Contains("smokecloud"))
        {
            if (heroPCActor.myStats.CheckHasStatusName("emblem_brigand_tier1_smokecloud"))
            {
                setAbility.energyCost /= 2;
                setAbility.SetMaxCooldown(8);
            }
        }

        if (setAbility.refName == "skill_bedofthorns")
        {
            if (heroPCActor.myStats.CheckHasStatusName("wildnaturebonus2"))
            {
                setAbility.range = 4;
                setAbility.targetRange = 2;
                setAbility.listEffectScripts[0] = GetEffectByRef("eff_bedofthornssummon2");
            }
        }
        else if (setAbility.refName == "skill_photoncannon")
        {
            if (heroPCActor.myStats.CheckHasStatusName("crystalarmamentbonus2"))
            {
                setAbility.listEffectScripts.Add(GetEffectByRef("eff_clightning"));
            }
        }
        else if (setAbility.refName == "skill_icemissile")
        {
            setAbility.range++;
            if (heroPCActor.myStats.CheckHasStatusName("blizzardgearbonus2"))
            {
                setAbility.boundsShape = TargetShapes.CLAW;
                setAbility.AddAbilityTag(AbilityTags.CENTERED);
                for (int i = 0; i < setAbility.listEffectScripts.Count; i++)
                {
                    setAbility.listEffectScripts[i].tActorType = TargetActorType.ALL;
                }
            }
        }

        if (heroPCActor.myStats.CheckHasStatusName("status_jumpboots"))
        {
            if (setAbility.abilityFlags[(int) AbilityFlags.MOVESELF])
            {
                setAbility.range++;
            }

            if (setAbility.refName == "skill_fistmastery1" || setAbility.refName == "skill_righteouscharge")
            {
                setAbility.targetOffsetY += 1;
            }
        }

        if (setAbility.refName == "skill_highlandcharge")
        {
            int numSongLevel = heroPCActor.myStats.CountStatusesByFlag(StatusFlags.THANESONG);
            if (numSongLevel > 0)
            {
                setAbility.targetOffsetY += numSongLevel;
            }
        }

        if (brigandBomberAbility)
        {
            setAbility.listEffectScripts.Add(brigandBomberTemplate);
        }

        if (unstableMagic)
        {
            EffectScript eff1 =
                spellshaperEvocationEffects[UnityEngine.Random.Range(0, spellshaperEvocationEffects.Count)];
            while (eff1.effectType != EffectType.DAMAGE)
            {
                eff1 = spellshaperEvocationEffects[UnityEngine.Random.Range(0, spellshaperEvocationEffects.Count)];
            }

            setAbility.listEffectScripts.Add(eff1);
            EffectScript eff2 =
                spellshaperEvocationEffects[UnityEngine.Random.Range(0, spellshaperEvocationEffects.Count)];
            while ((eff2.effectType == EffectType.DAMAGE))
            {
                eff2 = spellshaperEvocationEffects[UnityEngine.Random.Range(0, spellshaperEvocationEffects.Count)];
            }

            setAbility.listEffectScripts.Add(eff2);
        }


        if (setAbility.budokaMod)
        {

            if (heroPCActor.myStats.CheckHasStatusName("asceticaura"))
            {
                setAbility.listEffectScripts.Add(vitalPointParalyzeTemplate);
            }

            if (setAbility.refName == "skill_qistrike" && heroPCActor.myStats.CheckHasStatusName("qiwaveset"))
            {
                setAbility.boundsShape = TargetShapes.FLEXCROSS;
                setAbility.range = 3;
                setAbility.AddAbilityTag(AbilityTags.CENTERED);
                setAbility.targetOffsetY = 0;
            }
        }

    }

    private void TryAbility(AbilityScript abilityToTry)
    {
        originatingAbility = abilityToTry;
        if (abilityToTry.CheckAbilityTag(AbilityTags.REQUIRESHIELD))
        {
            if ((heroPCActor.myEquipment.GetOffhand() == null) ||
                (heroPCActor.myEquipment.GetOffhand().itemType != ItemTypes.OFFHAND))
            {
                StringManager.SetTag(0, abilityToTry.abilityName);
                GameLogScript.LogWriteStringRef("log_error_noshield");
                return;
            }
        }

        if (abilityToTry.CheckAbilityTag(AbilityTags.REQUIRERANGED))
        {
            if (!heroPCActor.myEquipment.GetWeapon().isRanged)
            {
                StringManager.SetTag(0, abilityToTry.abilityName);
                GameLogScript.LogWriteStringRef("log_error_noprojectile");
                return;
            }
        }

        if (abilityToTry.CheckAbilityTag(AbilityTags.TARGETED))
        {
            // Write some targeting code here.

            ClearBufferTargetData();
            uims.EnterTargeting(abilityToTry, Directions.NEUTRAL);
            if (abilityToTry.CheckAbilityTag(AbilityTags.MULTITARGET))
            {
                TDInputHandler.targetClicksRemaining = abilityToTry.numMultiTargets;
                TDInputHandler.targetClicksMax = abilityToTry.numMultiTargets;
                StringManager.SetTag(0, abilityToTry.abilityName);
                GameLogScript.GameLogWrite(
                    StringManager.GetString("log_prompt_ability_target") + " (1/" + TDInputHandler.targetClicksMax +
                    ")", heroPCActor);
            }
            else
            {
                TDInputHandler.targetClicksRemaining = 1;
                TDInputHandler.targetClicksMax = 0;
            }
        }
        else
        {
            //Debug.Log(abilityToTry.refName + " NOT a targeted ability, executing it immediately.");
            ClearBufferTargetData();
            TurnData td = new TurnData();
            td.actorThatInitiatedTurn = heroPCActor;
            td.SetTurnType(TurnTypes.ABILITY);
            td.tAbilityToTry = abilityToTry;
            td.SetSingleTargetActor(heroPCActor);
            td.SetSingleTargetPosition(heroPCActor.GetPos());
            TargetData ntd = new TargetData();
            ntd.whichAbility = abilityToTry;
            ntd.targetActors.Add(heroPCActor);
            ntd.targetTiles.Add(heroPCActor.GetPos());
            AddBufferTargetData(ntd, false);
            TryNextTurn(td, true);
        }
    }

    private void UseAbility(AbilityScript ability, Vector3 position, GameObject target)
    {

    }
}
