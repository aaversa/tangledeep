using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class DestroyActorEffect : EffectScript
{

    public List<string> destroySpecificActors;

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        DestroyActorEffect temp = template as DestroyActorEffect;
        tActorType = temp.tActorType;
        for (int i = 0; i < temp.destroySpecificActors.Count; i++)
        {
            destroySpecificActors.Add(temp.destroySpecificActors[i]);
        }
    }

    public DestroyActorEffect()
    {
        destroySpecificActors = new List<string>();
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        DestroyActorEffect eff = compareEff as DestroyActorEffect;

        foreach (string aRef in destroySpecificActors)
        {
            if (!eff.destroySpecificActors.Contains(aRef)) return false;
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

        GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }        

        if (destroySpecificActors.Count > 0)
        {
            List<Actor> removeActorRefs = new List<Actor>();
            for (int i = 0; i < actorsToProcess.Count; i++)
            {
                if (!destroySpecificActors.Contains(actorsToProcess[i].actorRefName))
                {
                    removeActorRefs.Add(actorsToProcess[i]);
                }
            }

            foreach (Actor act in removeActorRefs)
            {
                actorsToProcess.Remove(act);
            }
        }


        if (actorsToProcess.Count == 0)
        {
            return 0f;
        }

        float addWaitTime = 0f;

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);
        if ((playAnimation) && (!parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM)))
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
        else if ((playAnimation) && (parentAbility.CheckAbilityTag(AbilityTags.PLAYANIMONEMPTY)))
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
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        foreach (Actor act in actorsToProcess)
        {
            //Debug.Log("Destroyer: " + effectRefName + " " + act.actorRefName + " " + actorsToProcess.Count + " " + tActorType);
            if ((act.GetActorType() == ActorTypes.MONSTER) || (act.GetActorType() == ActorTypes.HERO))
            {
                if (act.actorRefName == "mon_targetdummy") continue;
                Fighter fight = (Fighter)act as Fighter;
                bool isBoss = false;
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.isBoss || mn.isChampion)
                    {
                        isBoss = true;
                    }
                }
                float value = 0;
                if (!isBoss)
                {
                    DestroyTargetMonster(fight, value, silent);
                }
                else
                {
                    fight.TakeDamage(fight.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) * 0.5f, DamageTypes.PHYSICAL);
                    value = fight.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR) * 0.5f;
                    if (!silent)
                    {
                        StringManager.SetTag(0, fight.displayName);
                        StringManager.SetTag(1, ((int)value).ToString());
                        GameLogScript.LogWriteStringRef("log_reaper_damage");
                    }
                }



                CombatManagerScript.ProcessGenericEffect(origFighter, fight, this, false, perTargetAnim);

                fight.whoKilledMe = GameMasterScript.heroPCActor;

                /* if (!fight.myStats.IsAlive())
                {
                    results.Add(CombatResult.MONSTERDIED);
                }
                else {
                    results.Add(CombatResult.NOTHING);
                } */

                results.Add(CombatResult.NOTHING);

                affectedActors.Add(act);
                //results.Add(CombatResult.NOTHING);
            }
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                GameMasterScript.AddToDeadQueue(act);
                affectedActors.Add(act);
                results.Add(CombatResult.NOTHING);
            }
        }

        float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = addWaitTime + (animLength * affectedActors.Count);
        }
        else
        {
            returnVal = animLength + addWaitTime;
        }

        if (playAnimation == false)
        {
            returnVal = 0.0f;
        }
        CombatManagerScript.accumulatedCombatWaitTime += returnVal;
        return returnVal;
    }

    public static void DestroyTargetMonster(Fighter fight, float value, bool silent)
    {
        fight.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
        value = 9999f;
        GameMasterScript.AddToDeadQueue(fight);
        if (!silent)
        {
            StringManager.SetTag(0, fight.displayName);
            GameLogScript.LogWriteStringRef("log_reaper");
        }
    }
}