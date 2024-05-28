using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class AlterBattleDataEffect : EffectScript
{
    public int changeDurability;
    public float changePercentAllDamage;
    public float changePercentAllMitigation;
    public float accumulatedPercentAllDamage;
    public float accumulatedPercentAllMitigation;
    public float changeFireResist;
    public float changeWaterResist;
    public float changePhysicalResist;
    public float changeShadowResist;
    public float changePoisonResist;
    public float changeLightningResist;
    public float changeFireDamage;
    public float changeWaterDamage;
    public float changePhysicalDamage;
    public float changeShadowDamage;
    public float changePoisonDamage;
    public float changeLightningDamage;
    public float changeSpiritPower;
    public float changeSpiritPowerMult;
    public float changeCritDamage;
    public float changeEnergyCosts;
    public float changeStaminaCosts;
    public float changeHealthCosts;

    public float pierceFire;
    public float piercePoison;
    public float pierceWater;
    public float pierceShadow;
    public float pierceLightning;
    public float piercePhysical;

    public float alterStealthDuringCache; // New kind of alter thing that doesn't do anything when the effects run; it is merely cached
    public float alterHealingDuringCache;

    //These numbers force additional costs even if the base power doesn't cost that resource.
    //0.1 forcedStaminaCosts makes all powers cost 10% more stamina, and powers that do not normally
    //cost stamina cost +stam == (energyCost * 0.1)
    public float forcedStaminaCosts;
    public float forcedEnergyCosts;
    
    public string monFamilyName;
    public float familyDamage;
    public float familyDefense;
    public float changeCritChance;

    public Dictionary<string, float> alterEffectValues;

    public override void ResetAccumulatedAmounts()
    {
        accumulatedPercentAllDamage = 0f;
        accumulatedPercentAllMitigation = 0f;
    }

    public AlterBattleDataEffect()
    {
        alterEffectValues = new Dictionary<string, float>();
        alterStealthDuringCache = 1f;
        alterHealingDuringCache = 1f;

        piercePoison = 1f;
        pierceLightning = 1f;
        piercePhysical = 1f;
        pierceWater = 1f;
        pierceFire = 1f;
        pierceShadow = 1f;

        tActorType = TargetActorType.SELF;
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        AlterBattleDataEffect nTemplate = (AlterBattleDataEffect)template as AlterBattleDataEffect;
        changeDurability = nTemplate.changeDurability;
        changePercentAllDamage = nTemplate.changePercentAllDamage;
        changePercentAllMitigation = nTemplate.changePercentAllMitigation;
        changeFireResist = nTemplate.changeFireResist;
        changeWaterResist = nTemplate.changeWaterResist;
        changePhysicalResist = nTemplate.changePhysicalResist;
        changeShadowResist = nTemplate.changeShadowResist;
        changePoisonResist = nTemplate.changePoisonResist;
        changeLightningResist = nTemplate.changeLightningResist;
        changeFireDamage = nTemplate.changeFireDamage;
        changeWaterDamage = nTemplate.changeWaterDamage;
        changePhysicalDamage = nTemplate.changePhysicalDamage;
        changeShadowDamage = nTemplate.changeShadowDamage;
        changePoisonDamage = nTemplate.changePoisonDamage;
        changeLightningDamage = nTemplate.changeLightningDamage;
        changeSpiritPower = nTemplate.changeSpiritPower;
        changeSpiritPowerMult = nTemplate.changeSpiritPowerMult;
        changeCritDamage = nTemplate.changeCritDamage;
        changeEnergyCosts = nTemplate.changeEnergyCosts;
        changeStaminaCosts = nTemplate.changeStaminaCosts;
        changeHealthCosts = nTemplate.changeHealthCosts;

        forcedStaminaCosts = nTemplate.forcedStaminaCosts;
        forcedEnergyCosts = nTemplate.forcedEnergyCosts;

        monFamilyName = nTemplate.monFamilyName;
        familyDamage = nTemplate.familyDamage;
        familyDefense = nTemplate.familyDefense;
        changeCritChance = nTemplate.changeCritChance;

        alterStealthDuringCache = nTemplate.alterStealthDuringCache;
        alterHealingDuringCache = nTemplate.alterHealingDuringCache;

        foreach (string key in nTemplate.alterEffectValues.Keys)
        {
            alterEffectValues.Add(key, nTemplate.alterEffectValues[key]);
        }

        pierceFire = nTemplate.pierceFire;
        pierceLightning = nTemplate.pierceLightning;
        piercePhysical = nTemplate.piercePhysical;
        pierceWater = nTemplate.pierceWater;
        pierceShadow = nTemplate.pierceShadow;
        piercePoison = nTemplate.piercePoison;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        AlterBattleDataEffect eff = compareEff as AlterBattleDataEffect;

        if (changePercentAllDamage != eff.changePercentAllDamage) return false;
        if (changePercentAllMitigation != eff.changePercentAllMitigation) return false;
        if (changeSpiritPower != eff.changeSpiritPower) return false;
        if (changeSpiritPowerMult != eff.changeSpiritPowerMult) return false;
        if (changeFireDamage != eff.changeFireDamage) return false;
        if (changeFireResist != eff.changeFireResist) return false;
        if (changeWaterDamage != eff.changeWaterDamage) return false;
        if (changeWaterResist != eff.changeWaterResist) return false;
        if (changeShadowDamage != eff.changeShadowDamage) return false;
        if (changeShadowResist != eff.changeShadowResist) return false;
        if (changePoisonDamage != eff.changePoisonDamage) return false;
        if (changePoisonResist != eff.changePoisonResist) return false;
        if (changePhysicalDamage != eff.changePhysicalDamage) return false;
        if (changePhysicalResist != eff.changePhysicalResist) return false;
        if (changeLightningDamage != eff.changeLightningDamage) return false;
        if (changeLightningResist != eff.changeLightningResist) return false;
        if (changeCritChance != eff.changeCritChance) return false;
        if (changeCritDamage != eff.changeCritDamage) return false;
        if (changeEnergyCosts != eff.changeEnergyCosts) return false;
        if (changeStaminaCosts != eff.changeStaminaCosts) return false;
        if (changeHealthCosts != eff.changeHealthCosts) return false;
        if (forcedEnergyCosts != eff.forcedEnergyCosts) return false;
        if (forcedStaminaCosts != eff.forcedStaminaCosts) return false;
        if (monFamilyName != eff.monFamilyName) return false;
        if (familyDamage != eff.familyDamage) return false;
        if (familyDefense != eff.familyDefense) return false;

        return true;
    }

    public override float DoEffect(int indexOfEffect = 0)
    {

        Fighter targetFighter = originatingActor as Fighter;

        if (!VerifyOriginatingActorIsFighterAndFix())
        {
            return 0f;
        }
        if (tActorType == TargetActorType.SELF)
        {
            targetFighter = selfActor as Fighter;
            if (!VerifySelfActorIsFighterAndFix())
            {
                return 0f;
            }
        }

        StatBlock targetStats = targetFighter.myStats;
        EquipmentBlock targetEquipment = targetFighter.myEquipment;

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPosition, this, targetFighter.GetObject());
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        //Debug.Log("For " + effectName + " processing " + originatingActor.displayName);

        targetFighter.allDamageMultiplier += changePercentAllDamage;
        //Debug.Log(targetFighter.actorRefName + " percent all mitigation changing by way of " + effectName + " " + effectRefName + " amount " + changePercentAllDamage);
        targetFighter.allMitigationAddPercent += changePercentAllMitigation;
        accumulatedPercentAllDamage += changePercentAllDamage;
        accumulatedPercentAllMitigation += changePercentAllMitigation;

        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.PHYSICAL] += changePhysicalResist;

        //Debug.Log(effectRefName + " " + effectName + " " + originatingActor.actorRefName + " Changing " + targetFighter.actorRefName + " phys mod " + changePhysicalResist + " is now " + targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.PHYSICAL]);


        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.FIRE] += changeFireResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.LIGHTNING] += changeLightningResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.WATER] += changeWaterResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.SHADOW] += changeShadowResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.POISON] += changePoisonResist;

        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.PHYSICAL] += changePhysicalDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.FIRE] += changeFireDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.LIGHTNING] += changeLightningDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.WATER] += changeWaterDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.SHADOW] += changeShadowDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.POISON] += changePoisonDamage;

        targetFighter.cachedBattleData.critDamageMod += changeCritDamage;
        targetFighter.cachedBattleData.critChanceMod += changeCritChance;
        targetFighter.cachedBattleData.spiritPowerMod += changeSpiritPower;
        targetFighter.cachedBattleData.spiritPowerModMult += changeSpiritPowerMult;

        targetFighter.cachedBattleData.energyCostMod += changeEnergyCosts;
        targetFighter.cachedBattleData.staminaCostMod += changeStaminaCosts;
        targetFighter.cachedBattleData.healthCostMod += changeHealthCosts;

        targetFighter.cachedBattleData.forcedStaminaCosts += forcedStaminaCosts;
        targetFighter.cachedBattleData.forcedEnergyCosts += forcedEnergyCosts;

        foreach(string key in alterEffectValues.Keys)
        {
            if (targetFighter.cachedBattleData.effectValueModifiers.ContainsKey(key))
            {
                targetFighter.cachedBattleData.effectValueModifiers[key] += alterEffectValues[key];
            }
            else
            {
                targetFighter.cachedBattleData.effectValueModifiers.Add(key, alterEffectValues[key]);
            }
        }

        if (monFamilyName != null && monFamilyName != "")
        {
            //Debug.Log(targetFighter.actorUniqueID + " gains " + familyDamage + " " + familyDefense + " against " + monFamilyName + " " + effectRefName);
            targetFighter.cachedBattleData.ChangeFamilyBonus(monFamilyName, familyDamage, familyDefense);
        }

        targetFighter.SetBattleDataDirty();
        //Debug.Log(targetFighter.displayName + " shadow " + changeShadowResist + " " + targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.SHADOW] + " " + targetFighter.cachedBattleData.resistances[(int)DamageTypes.SHADOW].multiplier);

        CombatManagerScript.ProcessGenericEffect(targetFighter, targetFighter, this, false, perTargetAnim);

        float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = animLength * 1;
        }
        else
        {
            returnVal = animLength;
        }

        if (playAnimation == false)
        {
            returnVal = 0.0f;
        }

        return returnVal;
    }

    public override void ReverseEffect()
    {
        if (!VerifyOriginatingActorIsFighterAndFix()) return;

        Fighter targetFighter = originatingActor as Fighter;

        if (tActorType == TargetActorType.SELF)
        {
            targetFighter = selfActor as Fighter;
        }
        if (selfActor == null)
        {
            if (originatingActor != null)
            {
                Debug.Log("No self actor for " + effectName + " " + effectRefName + " but orig. fighter is " + originatingActor.actorRefName);
                targetFighter = originatingActor as Fighter;
                selfActor = originatingActor;
            }
            else if (originatingActorUniqueID == 1)
            {
                Debug.Log("No self/orig actor for EName " + effectName + " EffName " + effectRefName + ", setting to hero.");
                originatingActor = GameMasterScript.heroPCActor;
                selfActor = GameMasterScript.heroPCActor;
                targetFighter = selfActor as Fighter;
            }
            else
            {
                Debug.Log("No self/orig actor for EName " + effectName + " EffName " + effectRefName + ", not hero: " + originatingActorUniqueID + " returning.");
                return;
            }

        }



        targetFighter.allDamageMultiplier -= accumulatedPercentAllDamage;
        targetFighter.allMitigationAddPercent -= accumulatedPercentAllMitigation;
        //Debug.Log(targetFighter.actorRefName + " percent all mitigation reversing by way of " + effectName + " " + effectRefName + " amount " + accumulatedPercentAllMitigation);


        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.PHYSICAL] -= changePhysicalResist;

        //Debug.Log(effectRefName + " " + effectName + " " + originatingActor.actorRefName + " Reversing " + targetFighter.actorRefName + " phys mod " + changePhysicalResist + " is now " + targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.PHYSICAL]);

        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.FIRE] -= changeFireResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.LIGHTNING] -= changeLightningResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.WATER] -= changeWaterResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.SHADOW] -= changeShadowResist;
        targetFighter.cachedBattleData.resistanceExternalMods[(int)DamageTypes.POISON] -= changePoisonResist;

        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.PHYSICAL] -= changePhysicalDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.FIRE] -= changeFireDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.LIGHTNING] -= changeLightningDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.WATER] -= changeWaterDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.SHADOW] -= changeShadowDamage;
        targetFighter.cachedBattleData.damageExternalMods[(int)DamageTypes.POISON] -= changePoisonDamage;

        targetFighter.cachedBattleData.critDamageMod -= changeCritDamage;
        targetFighter.cachedBattleData.critChanceMod -= changeCritChance;
        targetFighter.cachedBattleData.spiritPowerMod -= changeSpiritPower;
        targetFighter.cachedBattleData.spiritPowerModMult -= changeSpiritPowerMult;

        targetFighter.cachedBattleData.energyCostMod -= changeEnergyCosts;
        targetFighter.cachedBattleData.staminaCostMod -= changeStaminaCosts;
        targetFighter.cachedBattleData.healthCostMod -= changeHealthCosts;

        targetFighter.cachedBattleData.forcedStaminaCosts -= forcedStaminaCosts;
        targetFighter.cachedBattleData.forcedEnergyCosts -= forcedEnergyCosts;


        if ((monFamilyName != null) && (monFamilyName != ""))
        {
            targetFighter.cachedBattleData.ChangeFamilyBonus(monFamilyName, -1f * familyDamage, -1f * familyDefense);
        }

        foreach (string key in alterEffectValues.Keys)
        {
            if (targetFighter.cachedBattleData.effectValueModifiers.ContainsKey(key))
            {
                targetFighter.cachedBattleData.effectValueModifiers[key] -= alterEffectValues[key];
                if (CustomAlgorithms.CompareFloats(targetFighter.cachedBattleData.effectValueModifiers[key], 0f))
                {
                    targetFighter.cachedBattleData.effectValueModifiers.Remove(key);
                }
            }
        }

        targetFighter.SetBattleDataDirty();
        accumulatedPercentAllDamage = 0.0f;
        accumulatedPercentAllMitigation = 0.0f;
    }
}