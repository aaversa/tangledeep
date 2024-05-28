using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class SpecialEffect : EffectScript
{
    public string script_special;    

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        SpecialEffect temp = template as SpecialEffect;
        tActorType = temp.tActorType;
        script_special = temp.script_special;
    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        affectedActors.Clear();
        results.Clear();
        List<Actor> actorsToProcess = new List<Actor>();

        if (!VerifyOriginatingActorIsFighterAndFix())
        {
            return 0f;
        }

        Fighter origFighter = originatingActor as Fighter;

        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        bool valid = false;
        if (EvaluateTriggerCondition(actorsToProcess))
        {
            valid = true;
        }

        if (!valid) return 0.0f;

        try { 
            GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect); 
            foreach(Actor act in buildActorsToProcess)
            {
                actorsToProcess.Add(act);
            }                    
        }

        catch(Exception e)
        {
            Debug.Log("Error trying to get target actors for " + effectRefName + ": " + e);
            actorsToProcess.Clear();
        }

        float addWaitTime = 0.0f;
        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);
        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            Vector2 usePosition = centerPosition;

            if (usePosition == Vector2.zero)
            {
                usePosition = selfActor.GetPos();
            }

            if (centerSpriteOnOriginatingActor)
            {
                usePosition = originatingActor.GetPos();
            }

            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), usePosition, this, originatingActor.GetObject());
        }
        else if (playAnimation && parentAbility.CheckAbilityTag(AbilityTags.PLAYANIMONEMPTY))
        {
            foreach (Vector2 v2 in positions)
            {
                CombatManagerScript.EnqueueCombatAnimation(new CombatAnimation(originatingActor.GetPos(), v2, this, null, originatingActor.GetObject(), false, false, false)); // Target individual fighters?
            }
            if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
            {
                addWaitTime = positions.Count * animLength;
            }
            perTargetAnim = false;
        }
        else if (playAnimation && parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            foreach(Actor act in actorsToProcess)
            {
                CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), act.GetPos(), this, act.GetObject());
                addWaitTime += animLength;
            }
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        if (!actorsToProcess.Contains(originatingActor))
        {
            actorsToProcess.Add(originatingActor);
        }

        //Debug.Log(effectName + " " + effectRefName + " " + actorsToProcess.Count);

        if (!string.IsNullOrEmpty(script_special))
        {
            EffectResultPayload erp = null;

            Func<SpecialEffect, List<Actor>, EffectResultPayload> dFunc;

            if (SpecialEffectFunctions.dictDelegates.TryGetValue(script_special, out dFunc))
            {
                erp = dFunc(this, actorsToProcess);
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(SpecialEffectFunctions), script_special);
                object[] paramList = new object[2];
                paramList[0] = this;
                paramList[1] = actorsToProcess;
                object returnObj = runscript.Invoke(null, paramList);
                if (returnObj == null)
                {
                    Debug.Log("No object for " + script_special);
                }

                erp = returnObj as EffectResultPayload;
            }


            addWaitTime += erp.waitTime;
            actorsToProcess = erp.actorsToProcess;
            if (addWaitTime > 0f)
            {
                playAnimation = true;
            }
        }

        /* float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = addWaitTime + (animLength * affectedActors.Count);
        }
        else
        {
            returnVal = animLength + addWaitTime;
        } */

        float returnVal = addWaitTime;

        if (playAnimation == false)
        {
            returnVal = 0.0f;
        }


        if (PlayerOptions.animSpeedScale != 0f)
        {
            returnVal *= PlayerOptions.animSpeedScale;
        }


        CombatManagerScript.accumulatedCombatWaitTime += returnVal;
        return returnVal;
    }


}