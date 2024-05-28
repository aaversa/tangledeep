using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class RemoveStatusEffect : EffectScript
{
    public string statusRef;
    public List<string> removableStatuses;
    public bool removeAllNegative;
    public bool removeAllPositive;
    public bool[] removeFlags;

    public RemoveStatusEffect()
    {
        removableStatuses = new List<string>();
        removeFlags = new bool[(int)StatusFlags.COUNT];
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        RemoveStatusEffect nTemplate = (RemoveStatusEffect)template as RemoveStatusEffect;
        removeAllNegative = nTemplate.removeAllNegative;
        statusRef = nTemplate.statusRef;
        removeAllPositive = nTemplate.removeAllPositive;
        foreach (string txt in nTemplate.removableStatuses)
        {
            removableStatuses.Add(txt);
        }
        for (int i = 0; i < removeFlags.Length; i++)
        {
            removeFlags[i] = nTemplate.removeFlags[i];
        }
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        RemoveStatusEffect eff = compareEff as RemoveStatusEffect;
        if (statusRef != eff.statusRef) return false;
        if (removeAllNegative != eff.removeAllNegative) return false;
        for (int i = 0; i < removeFlags.Length; i++)
        {
            if (removeFlags[i] != eff.removeFlags[i]) return false;
        }
        foreach (string remove in removableStatuses)
        {
            if (!eff.removableStatuses.Contains(remove)) return false;
        }

        return true;
    }
    public override float DoEffect(int indexOfEffect = 0)
    {
        affectedActors.Clear();
        results.Clear();
        List<Actor> actorsToProcess = new List<Actor>();
        Fighter origFighter = (Fighter)originatingActor as Fighter;
        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        bool perTargetAnim = true;

        if (parentAbility != null)
        {
            perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);
        }
        if ((playAnimation) && (!parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM)))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPosition, this, originatingActor.GetObject());
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }                

        List<StatusEffect> seToRemove = new List<StatusEffect>();
        Fighter ft;
        foreach (Actor act in actorsToProcess)
        {
            ft = act as Fighter;
            if ((act.GetActorType() == ActorTypes.MONSTER) || (act.GetActorType() == ActorTypes.HERO))
            {
                Fighter fight = (Fighter)act as Fighter;

                if ((!removeAllNegative) && (!removeAllPositive))
                {
                    foreach (StatusEffect checkEffect in fight.myStats.GetAllStatuses())
                    {
                        if (checkEffect.refName == "status_foodfull") continue;
                        if (checkEffect.noRemovalOrImmunity) continue;
                        if (removableStatuses.Contains(checkEffect.refName))
                        {
                            seToRemove.Add(checkEffect);
                        }
                        for (int i = 0; i < removeFlags.Length; i++)
                        {
                            if ((removeFlags[i]) && (checkEffect.statusFlags[i]))
                            {
                                //Debug.Log("Removing " + checkEffect.refName);
                                seToRemove.Add(checkEffect);
                            }
                        }
                    }
                }
                else
                {
                    foreach (StatusEffect eff in fight.myStats.GetAllStatuses())
                    {
                        if (eff.refName == "status_foodfull") continue;
                        if (removeAllPositive)
                        {
                            if ((eff.isPositive) && (!eff.CheckDurTriggerOn(StatusTrigger.PERMANENT)))
                            {
                                seToRemove.Add(eff);
                            }
                        }
                        else
                        {
                            if ((!eff.isPositive) && (!eff.CheckDurTriggerOn(StatusTrigger.PERMANENT)))
                            {
                                seToRemove.Add(eff);
                            }
                        }


                    }

                }


                if (seToRemove.Count > 0)
                {

                    foreach (StatusEffect se in seToRemove)
                    {
                        fight.myStats.RemoveStatus(se, true);
                        StringManager.SetTag(0, fight.displayName);
                        StringManager.SetTag(1, se.abilityName);
                        if (!removeAllPositive)
                        {
                            GameLogScript.LogWriteStringRef("log_cure_status");
                        }
                        else
                        {
                            GameLogScript.LogWriteStringRef("log_cure_positive_status");
                        }
                        //Debug.Log("Ability " + effectRefName + " " + effectName + " Removed " + se.abilityName + " from " + fight.displayName);
                    }

                    affectedActors.Add(act);
                }
            }

            CombatManagerScript.ProcessGenericEffect(origFighter, ft, this, false, perTargetAnim);

        }
        float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = animLength * affectedActors.Count;
        }
        else
        {
            returnVal = animLength;
        }
        return returnVal;
    }
}
