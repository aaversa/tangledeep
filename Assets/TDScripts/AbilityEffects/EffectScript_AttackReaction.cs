using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class AttackReactionEffect : EffectScript
{
    public string effectEquation;
    public float alterParry; // as % mod to current
    public float alterAccuracy; // as %
    public float alterBlock;
    public float alterBlockFlat;
    public float alterParryFlat; // flat number
    public float alterAccuracyFlat; // flat number
    public float alterDamage; // as flat mod to current
    public float alterDamagePercent; // as % mod to current
    public AttackConditions reactCondition;

    public string script_Special;

    public AttackReactionEffect()
    {
        reactCondition = AttackConditions.ANY;
        alterParry = 1.0f;
        alterAccuracy = 1.0f;
        alterParryFlat = 0.0f;
        alterAccuracyFlat = 0.0f;
        alterBlockFlat = 0.0f;
        alterBlock = 1.0f;
        alterDamage = 0.0f; // Was in the process of changing damage flat to %
        alterDamagePercent = 1.0f;
        tActorType = TargetActorType.SELF;
        script_Special = "";
    }

    public override bool CompareToEffect(EffectScript eff)
    {
        bool checkBase = base.CompareToEffect(eff);
        if (!checkBase) return checkBase;

        AttackReactionEffect compareEff = eff as AttackReactionEffect;
        if (effectEquation != compareEff.effectEquation) return false;
        if (effectPower != compareEff.effectPower) return false;
        if (alterParry != compareEff.alterParry) return false;
        if (alterParryFlat != compareEff.alterParryFlat) return false;
        if (alterDamage != compareEff.alterDamage) return false;
        if (alterDamagePercent != compareEff.alterDamagePercent) return false;
        if (alterBlock != compareEff.alterBlock) return false;
        if (alterBlockFlat != compareEff.alterBlockFlat) return false;
        if (alterAccuracy != compareEff.alterAccuracy) return false;
        if (alterAccuracyFlat != compareEff.alterAccuracyFlat) return false;
        if (reactCondition != compareEff.reactCondition) return false;

        return true;
    }
    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        AttackReactionEffect nTemplate = (AttackReactionEffect)template as AttackReactionEffect;
        effectEquation = nTemplate.effectEquation;
        effectPower = nTemplate.effectPower;
        alterBlock = nTemplate.alterBlock;
        alterBlockFlat = nTemplate.alterBlockFlat;
        alterParry = nTemplate.alterParry;
        alterAccuracy = nTemplate.alterAccuracy;
        tActorType = nTemplate.tActorType;
        alterParryFlat = nTemplate.alterParryFlat;
        alterAccuracyFlat = nTemplate.alterAccuracyFlat;
        alterDamage = nTemplate.alterDamage;
        alterDamagePercent = nTemplate.alterDamagePercent;
        reactCondition = nTemplate.reactCondition;
        script_Special = nTemplate.script_Special;
    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        // Flesh out more conditions here.
        float value = 0.0f;

        Fighter fight = selfActor as Fighter;

        // If we have bad self/originating actor data and the function below can't fix it, don't do anything.
        if (!VerifySelfActorIsFighterAndFix())
        {
            return 0f;
        }

        // Who is the attacker?
        Fighter attacker = CombatManagerScript.bufferedCombatData.attacker;

        bool valid = false;

        // Flesh this out more.
        //Debug.Log("Try run " + effectName + " " + effectRefName + " " + reactCondition);

        if (EvaluateTriggerCondition(new List<Actor>()))
        {
            valid = true;
        }

        if (UnityEngine.Random.Range(0, 1.0f) > procChance || !valid)
        {
            return 0.0f;
        }

        if (!string.IsNullOrEmpty(script_Special))
        {
            EffectResultPayload erp = null;

            Func<SpecialEffect, List<Actor>, EffectResultPayload> dFunc;

            if (SpecialEffectFunctions.dictDelegates.TryGetValue(script_Special, out dFunc))
            {
                erp = dFunc(null, new List<Actor>() { attacker, fight });
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(SpecialEffectFunctions), script_Special);
                object[] paramList = new object[2];
                paramList[0] = null;
                paramList[1] = new List<Actor>() { attacker, fight };
                object returnObj = runscript.Invoke(null, paramList);
                if (returnObj == null)
                {
                    Debug.Log("No object for " + script_Special);
                }

                erp = returnObj as EffectResultPayload;
            }
        }

        // Need a way to display FX.

        CombatManagerScript.bufferedCombatData.blockMod += alterBlock;
        CombatManagerScript.bufferedCombatData.blockModFlat += alterBlockFlat;

        //Debug.Log("Modify: " + alterBlock + " " + alterBlockFlat);

        CombatManagerScript.ModifyBufferedAccuracy(alterAccuracy);
        CombatManagerScript.ModifyBufferedParry(alterParry);
        CombatManagerScript.ModifyBufferedAccuracyFlat(alterAccuracyFlat);
        CombatManagerScript.ModifyBufferedParryFlat(alterParryFlat);
        CombatManagerScript.ModifyBufferedDamageAsPercent(alterDamagePercent);
        CombatManagerScript.ModifyBufferedDamage(alterDamage);
        
        //CombatManagerScript.ModifyBufferedAccuracy(alterAccuracyFlat, true);
        //CombatManagerScript.ModifyBufferedParry(alterParry, true);

        return value;
    }
}