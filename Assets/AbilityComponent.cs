using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;

public class AbilityComponent
{

    public List<AbilityScript> abilities = new List<AbilityScript>();
    // dictionary is for fast lookups, that's all
    public Dictionary<string, AbilityScript> dictAbilities = new Dictionary<string, AbilityScript>();
    public List<AbilityScript> masteredAbilities = new List<AbilityScript>();
    public List<string> abilitiesThatReserveEnergy = new List<string>();
    public List<string> abilitiesThatReserveStamina = new List<string>();

    public Fighter owner;
    public AbilityScript lastUsedAbility; // not necessary to serialize for now
    bool isDirty;

    bool isDragonSoulEquipped;
    bool dragonSoulDirty = true;

    public void ResetCooldownForAbilityByRef(string abilRef) 
    {
        if (!HasAbilityRef(abilRef)) return;

        AbilityScript baseAbil = GetAbilityByRef(abilRef);

        baseAbil.ResetCooldown();
    }

    public void TryCleanAbilitiesThatReserveEnergy()
    {
        bool anyRemoved = false;
        for (int i = 0; i < abilitiesThatReserveEnergy.Count; i++)
        {
            AbilityScript fetchAbil = GetAbilityByRef(abilitiesThatReserveEnergy[i]);            
            if (!fetchAbil.toggled)
            {
                //Debug.Log("We are removing " + fetchAbil.refName + " " + fetchAbil.myID);
                abilitiesThatReserveEnergy.Remove(abilitiesThatReserveEnergy[i]);                
                i--;
                anyRemoved = true;
            }
        }
        if (anyRemoved)
        {
            owner.SetBattleDataDirty();
        }
    }

    public void TryCleanAbilitiesThatReserveStamina()
    {
        bool anyRemoved = false;
        for (int i = 0; i < abilitiesThatReserveStamina.Count; i++)
        {
            AbilityScript fetchAbil = GetAbilityByRef(abilitiesThatReserveStamina[i]);
            if (!fetchAbil.toggled)
            {
                //Debug.Log("We are removing " + fetchAbil.refName + " " + fetchAbil.myID);
                abilitiesThatReserveStamina.Remove(abilitiesThatReserveStamina[i]);
                i--;
                anyRemoved = true;
            }
        }
        if (anyRemoved)
        {
            owner.SetBattleDataDirty();
        }
    }

    public void ActivateEnergyReservingAbility(AbilityScript abil, bool state)
    {
        if (state)
        {
            if (!abilitiesThatReserveEnergy.Contains(abil.refName))
            {
                //Debug.Log("We are activating " + abil.refName + " " + abil.myID);
                abilitiesThatReserveEnergy.Add(abil.refName);
                abil.Toggle(true); // anything that reserves ability must be counted as toggled
            }
            else
            {
                //Debug.Log(abil.refName + " was already in our list?");
            }
        }
        else
        {            
            AbilityScript trueAbilityRef = owner.cachedBattleData.GetOriginalVersionOfRemappedAbility(abil.refName, owner);
            //Debug.Log("We are DEACTIVATING " + trueAbilityRef.refName + " " + trueAbilityRef.myID);
            abilitiesThatReserveEnergy.Remove(abil.refName);
            trueAbilityRef.Toggle(false);
        }
    }

    public void ActivateStaminaReservingAbility(AbilityScript abil, bool state)
    {
        if (state)
        {
            if (!abilitiesThatReserveStamina.Contains(abil.refName))
            {
                abilitiesThatReserveStamina.Add(abil.refName);
                abil.Toggle(true); // anything that reserves ability must be counted as toggled
            }
            else
            {
                //Debug.Log(abil.refName + " was already in our list?");
            }
        }
        else
        {
            AbilityScript trueAbilityRef = owner.cachedBattleData.GetOriginalVersionOfRemappedAbility(abil.refName, owner);
            //Debug.Log("We are DEACTIVATING " + trueAbilityRef.refName + " " + trueAbilityRef.myID);
            abilitiesThatReserveStamina.Remove(abil.refName);
            trueAbilityRef.Toggle(false);
        }
    }

    public AbilityComponent()
    {
        isDirty = true;
    }

    public int CountAbilities()
    {
        return abilities.Count;
    }

    public void SetDirty(bool state)
    {
        isDirty = state;
    }

    public bool CheckIfDirty()
    {
        return isDirty;
    }

    public void RemoveAllAbilities()
    {
        abilities.Clear();
        dictAbilities.Clear();
        masteredAbilities.Clear();
    }

    public void WriteToSave(XmlWriter writer, bool mysteryDungeonBlock = false)
    {
        foreach (AbilityScript abil in abilities)
        {
            if (mysteryDungeonBlock)
            {
                abil.WriteToSave(writer, GameMasterScript.heroPCActor.actorUniqueID);
            }
            else
            {
                abil.WriteToSave(writer, owner.actorUniqueID);
            }
            
        }
    }

    public int GetPassiveSlotsUsed()
    {
        int count = 0;
        foreach (AbilityScript abil in abilities)
        {
            if (abil.passiveAbility && abil.UsePassiveSlot && abil.passiveEquipped && !abil.CheckAbilityTag(AbilityTags.DRAGONSOUL))
            {
                count++;
            }
        }
        return count;
    }

    public AbilityScript GetAbilityByRef(string abilRef)
    {
        AbilityScript foundAbility;
        if (dictAbilities.TryGetValue(abilRef, out foundAbility))
        {
            return foundAbility;
        }

        return null;
    }

    public void TickAllCooldowns()
    {
        foreach (AbilityScript abil in abilities)
        {
            if (abil.GetCurCooldownTurns() > 0)
            {
                if (!abil.CheckCooldownConditions(owner))
                {
                    continue;
                }
                //Debug.Log(abil.refName + " had cooldown " + abil.GetCurCooldownTurns());
                abil.ChangeCurrentCooldown(-1);
                //Debug.Log("Now has " + abil.GetCurCooldownTurns());
            }
            /* else if (abil.toggled && abil.refName.Contains("livingvine"))
            {
                // This ability can get into a weird state where its cooldown is 0 but it's display at max CD, toggled
                abil.Toggle(false);
            } */
        }
    }

    public void ClearAllCooldowns()
    {
        foreach (AbilityScript abil in abilities)
        {
            //Debug.Log(owner.actorRefName + " " + owner.actorUniqueID + " tick " + abil.refName + " is " + abil.curCooldownTurns + " " + abil.maxCooldownTurns + " " + abil.uniqueID);
            if (abil.GetCurCooldownTurns() > 0)
            {
                abil.ChangeCurrentCooldown(-1 * abil.maxCooldownTurns);
            }
        }
    }

    public bool HasAbility(AbilityScript abil)
    {
        return dictAbilities.ContainsKey(abil.refName);
    }

    public bool HasAbilityRef(string abilRef)
    {
        return dictAbilities.ContainsKey(abilRef);

    }

    public bool HasMasteredAbilityByRef(string abilRef)
    {
        foreach (AbilityScript ability in masteredAbilities)
        {
            if (abilRef == ability.refName)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasMasteredAbility(AbilityScript abil)
    {
        foreach (AbilityScript ability in masteredAbilities)
        {
            if (abil.refName == ability.refName)
            {
                return true;
            }
        }
        return false;
    }

    public List<AbilityScript> GetAbilityList()
    {
        return abilities;
    }

    public void UnequipPassiveAbility(AbilityScript abil)
    {
        if (abil.passiveEquipped)
        {
            if (abil.passiveAbility && abil.active)
            {
                foreach (EffectScript eff in abil.listEffectScripts)
                {
                    //Debug.Log("Reversing " + eff.effectName + " " + eff.effectType + " " + eff.spriteEffectRef + " " + abil.abilityName);
                    eff.originatingActor = owner;
                    eff.ReverseEffect();
                }
                // Reverse effect.
                abil.active = false;
                abil.passiveEquipped = false;

            }
            if (abil.refName == "skill_sneakattack")
            {
                StatusEffect se = null;
                foreach (StatusEffect RSE in owner.myStats.GetAllStatuses())
                {
                    if (RSE.refName == "sneakattackwaiter")
                    {
                        //owner.SetActorData("sneakattack_turns", RSE.GetCurCooldownTurns());
                        se = RSE;
                        break;
                    }
                }
                if (se != null)
                {
                    owner.myStats.GetAllStatuses().Remove(se);
                }
            }
            else if (abil.refName == "skill_autobarrier")
            {
                StatusEffect se = null;
                foreach (StatusEffect RSE in owner.myStats.GetAllStatuses())
                {
                    if (RSE.refName == "autobarrierwaiter")
                    {
                        se = RSE;
                        break;
                    }
                }
                if (se != null)
                {
                    owner.myStats.GetAllStatuses().Remove(se);
                }
            }
            else if (abil.refName == "skill_vanishingdodge")
            {
                owner.myStats.RemoveStatusByRef("vanishing_lowhealthdodge");
            }
        }
        if (owner != null)
        {
            owner.SetBattleDataDirty();
            if (owner.GetActorType() == ActorTypes.HERO)
            {
                GameMasterScript.heroPCActor.EnableWrathBarIfNeeded();
            }            
        }

        dragonSoulDirty = true;
    }

    public void EquipPassiveAbility(AbilityScript abil, bool bRecalculateBattleData = true)
    {
        
        if (HasAbility(abil) && !abil.passiveEquipped && abil.passiveAbility)
        {
            //if (owner.GetActorType() == ActorTypes.HERO) Debug.Log("Equip " + abil.refName);
            if (abil.CheckAbilityTag(AbilityTags.SPELLSHAPE))
            {
                // Spellshapes / spellshifts are activated in a special way.
                return;
            }

            if (owner != null && bRecalculateBattleData)
            {
                owner.SetBattleDataDirty();
            }

            abil.passiveEquipped = true;
            abil.active = true;
            
            foreach (EffectScript eff in abil.listEffectScripts)
            {
                //if (owner.GetActorType() == ActorTypes.HERO) Debug.Log("Execute " + eff.effectRefName);
                eff.originatingActor = owner;
                eff.originatingActor.actorUniqueID = owner.actorUniqueID;
                eff.selfActor = owner;
                eff.parentAbility = abil;
                eff.DoEffect();
            }
            
        }
        else
        {
            Debug.Log("Error trying to equip+activate " + abil.refName + " " + owner.actorRefName + " " + abil.passiveEquipped + " " + abil.passiveAbility);
        }

        dragonSoulDirty = true;
        
        if (owner != null && owner.GetActorType() == ActorTypes.HERO)
        {
            GameMasterScript.heroPCActor.EnableWrathBarIfNeeded();
        }
    }

    public void AddNewAbility(AbilityScript abilityToAdd, bool equipAbilIfPassive, bool learnFromJob = true, bool verboseMessage = false)
    {
        if (!string.IsNullOrEmpty(abilityToAdd.script_onLearn))
        {
            // First argument is always the 'command'
            // Subsequent arguments might vary
            string[] parsed = abilityToAdd.script_onLearn.Split(',');

            int numArgs = parsed.Length - 1;
            string[] args = new string[numArgs];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = parsed[i + 1];
            }

            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(AbilitySpecialFunctions), parsed[0]);
            object[] paramList = new object[3];
            paramList[0] = owner;
            paramList[1] = abilityToAdd;
            paramList[2] = args;
            runscript.Invoke(null, paramList);
        }

        if (!HasAbility(abilityToAdd))
        {
            //if (owner.GetActorType() == ActorTypes.HERO) Debug.Log("Let's learn " + abilityToAdd.refName + " " + equipAbilIfPassive + " " + learnFromJob);
            if (abilityToAdd.CheckAbilityTag(AbilityTags.DRAGONSOUL))
            {
                abilities.Insert(0, abilityToAdd);
            }
            else
            {
                abilities.Add(abilityToAdd);
            }
            
            if (!dictAbilities.ContainsKey(abilityToAdd.refName))
            {
                dictAbilities.Add(abilityToAdd.refName, abilityToAdd);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log(owner.actorRefName + " already knows " + abilityToAdd.refName);
            }
            SetDirty(true);
            if (owner != null && owner.myJob != null)
            {
                if (learnFromJob)
                {
                    abilityToAdd.jobLearnedFrom = owner.myJob.jobEnum;
                }
                else
                {
                    abilityToAdd.jobLearnedFrom = CharacterJobs.GENERIC;
                }

            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log(abilityToAdd.abilityName + " no owner or job / job enum");
            }

            bool playerEquippedPassivesFull = false;
            bool isHero = false;
            if (owner == GameMasterScript.heroPCActor)
            {
                isHero = true;
                int equippedPassiveCount = 0;
                foreach (AbilityScript abil in abilities)
                {
                    //Debug.Log(abil.abilityName + " " + "Active: " + abil.active + " P.Equipped: " + abil.passiveEquipped + " Use P Slot: " + abil.usePassiveSlot);
                    if (abil.passiveAbility && abil.active && abil.passiveEquipped && abil.UsePassiveSlot && !abil.CheckAbilityTag(AbilityTags.DRAGONSOUL)) // If it doesn't display in list, it's an innate ability or trait that shouldn't be counted?
                    {
                        //Debug.Log(abil.abilityName + " is equipped, so...");
                        equippedPassiveCount++;
                    }
                }
                if (equippedPassiveCount >= GameMasterScript.gmsSingleton.maxEquippedPassives)
                {
                    playerEquippedPassivesFull = true;
                }
            }

            // Passive code goes elsewhere.


            //if (isHero) Debug.Log(abilityToAdd.abilityName + " " + owner.displayName + " " + equipAbilIfPassive + " " + abilityToAdd.passiveAbility + " " + playerEquippedPassivesFull);

            if (abilityToAdd.passiveAbility)
            {
                if ((equipAbilIfPassive || !playerEquippedPassivesFull || !abilityToAdd.UsePassiveSlot) && !abilityToAdd.CheckAbilityTag(AbilityTags.DRAGONSOUL))
                {
                    EquipPassiveAbility(abilityToAdd, false);
                }

            }
            else if (!abilityToAdd.passiveAbility || equipAbilIfPassive || !playerEquippedPassivesFull)
            {
                abilityToAdd.active = true;
            }
            if (owner != null) owner.SetBattleDataDirty();
            if (owner == GameMasterScript.heroPCActor && abilityToAdd.displayInList && GameMasterScript.actualGameStarted)
            {
                StringManager.SetTag(0, abilityToAdd.abilityName);
                if (verboseMessage)
                {
                    GameLogScript.LogWriteStringRef("ui_log_learnweaponmastery2", null, TextDensity.VERBOSE);
                }
                else
                {
                    GameLogScript.LogWriteStringRef("ui_log_learnweaponmastery2");
                }
                
                if (owner == GameMasterScript.heroPCActor)
                {
                    if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SOULKEEPER 
                        && !GameMasterScript.heroPCActor.myAbilities.HasAbilityRef("skill_spiritcollector"))
                    {
                        AbilityScript collector = GameMasterScript.masterAbilityList["skill_spiritcollector"];
                        GameMasterScript.heroPCActor.myAbilities.AddNewAbility(collector, true, true);
                    }
                    if (abilityToAdd.CheckAbilityTag(AbilityTags.SPELLSHAPE) && !GameMasterScript.heroPCActor.myAbilities.HasAbilityRef("skill_managespellshapes"))
                    {
                        AbilityScript manageSpellshapes = GameMasterScript.masterAbilityList["skill_managespellshapes"];
                        GameMasterScript.heroPCActor.myAbilities.AddNewAbility(manageSpellshapes, true, true);
                        UIManagerScript.AddAbilityToOpenSlot(GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_managespellshapes"));
                    }
                }
            }
        }
        else
        {
#if UNITY_EDITOR
            //Debug.Log("We already know " + abilityToAdd.refName);
#endif
        }

    }

    public void MasterAbility(AbilityScript abilityToAdd)
    {
        if (!HasMasteredAbility(abilityToAdd))
        {
            masteredAbilities.Add(abilityToAdd);
        }
        AddNewAbility(abilityToAdd, false);
    }

    public AbilityScript RemoveAbility(AbilityScript abil, bool bCheckKeepFromAlternateSources = true)
    {
        return RemoveAbility(abil.refName, bCheckKeepFromAlternateSources);
    }

    public AbilityScript RemoveAbility(string refName, bool bCheckKeepFromAlternateSources = true)
    {
        //Maybe we're trying to remove the ability because it's granted to us by an item,
        //but we're ALSO wearing another item that grants the ability. Pandemonium!
        if (bCheckKeepFromAlternateSources)
        {
            HeroPC hero = GameMasterScript.heroPCActor;
            foreach (Equipment eq in hero.myEquipment.equipment)
            {
                if (eq == null || eq.addAbilities == null)
                {
                    continue;
                }

                foreach (AbilityScript abilOnItem in eq.addAbilities)
                {
                    if (abilOnItem.refName == refName)
                    {
                        //oops, we should not get rid of this ability just yet.
                        return null;
                    }
                }

                foreach (MagicMod mm in eq.mods)
                {
                    foreach (AbilityScript abilOnItem in mm.addAbilities)
                    {
                        if (abilOnItem.refName == refName)
                        {
                            //oops, we should not get rid of this ability just yet.
                            return null;
                        }
                    }
                }
            }
        }

        //Debug.Log("Removing " + refName);
        List<AbilityScript> abilToRemove = new List<AbilityScript>();
        foreach (AbilityScript learnt in abilities)
        {
            if (refName == learnt.refName)
            {
                if (learnt.passiveAbility && learnt.active)
                {
                    foreach (EffectScript eff in learnt.listEffectScripts)
                    {
                        //Debug.Log("Reversing " + eff.effectName + " " + eff.effectType + " " + eff.spriteEffectRef + " " + learnt.abilityName);
                        eff.originatingActor = owner;
                        eff.ReverseEffect();
                    }
                    // Reverse effect.
                }
                abilToRemove.Add(learnt);
            }
        }


        foreach (AbilityScript learnt in abilToRemove)
        {
            abilities.Remove(learnt);
            dictAbilities.Remove(learnt.refName);
            SetDirty(true);
            if (owner != null)
            {
                if (owner.GetActorType() == ActorTypes.HERO)
                {
            UIManagerScript.TryRemoveAbilityFromHotbar(learnt);
                }
                owner.SetBattleDataDirty();
            }
            return learnt;
        }

        return null;
    }

    public void VerifyEquippedPassivesAreActivated()
    {
        if (owner == null) return;
        foreach (AbilityScript abil in abilities)
        {
            if (abil.passiveAbility && abil.passiveEquipped && abil.listEffectScripts.Count >= 1)
            {
                foreach (EffectScript eff in abil.listEffectScripts)
                {
                    if (eff.effectType == EffectType.ADDSTATUS)
                    {
                        AddStatusEffect ase = eff as AddStatusEffect;
                        if (!owner.myStats.CheckHasStatusName(ase.statusRef))
                        {
                            owner.myStats.AddStatusByRef(ase.statusRef, owner, 99);
                        }
                    }
                }

            }
        }
    }

    // Checks if a normal ability is the same as a modded one
    public bool AreAbilitiesSameParent(AbilityScript abil1, AbilityScript abil2)
    {
        if (abil1.refName == abil2.refName)
        {
            return true;
        }
        if (abil1 == abil2)
        {
            return true;
        }

        if (string.IsNullOrEmpty(abil1.refName) || string.IsNullOrEmpty(abil2.refName))
        {
            return false;
        }

        if (abil1.refName.Contains(abil2.refName) || abil2.refName.Contains(abil1.refName))
        {
            return true;
        }

        // Above is a kinda hacky way of detecting modified skills... we should do it better #todo

        return false;
    }

    // Thundering Lion, Feral Fighting, etc... turn 'em all off, let 'em burn
    public void ToggleOffAllAbilities()
    {
        foreach (AbilityScript abil in abilities)
        {
            abil.Toggle(false);
            abil.SetCurCooldownTurns(0);

            // Also, remove any statuses associated with the un-toggled ability
            foreach (EffectScript eff in abil.listEffectScripts)
            {
                if (eff.effectType != EffectType.ADDSTATUS) continue;
                AddStatusEffect ase = eff as AddStatusEffect;
                if (!string.IsNullOrEmpty(ase.statusRef))
                {
                    owner.myStats.RemoveAllStatusByRef(ase.statusRef);
                }
            }
        }
    }

    public bool IsDragonSoulEquipped()
    {
        if (!dragonSoulDirty)
        {
            return isDragonSoulEquipped;
        }

        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            isDragonSoulEquipped = false;
            dragonSoulDirty = false;
            return false;
        }
        foreach(AbilityScript abil in abilities)
        {
            if (!abil.UsePassiveSlot) continue;
            if (!abil.CheckAbilityTag(AbilityTags.DRAGONSOUL)) continue;
            if (abil.passiveEquipped)
            {
                isDragonSoulEquipped = true;
                dragonSoulDirty = false;
                return true;
            }
        }

        isDragonSoulEquipped = false;
        dragonSoulDirty = false;
        return false;
    }

    public AbilityScript GetFirstEquippedPassiveAbilityOfTag(AbilityTags tag)
    {
        foreach (AbilityScript abil in abilities)
        {
            if (!abil.passiveAbility) continue;
            if (!abil.passiveEquipped) continue;
            if (abil.CheckAbilityTag(tag))
            {
                return abil;
            }
        }

        return null;
    }
}
