using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackModifierFunctions {

    public static float SplitElementalDamageForBudoka(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        Fighter origFighter = originatingActor as Fighter;
        if (origFighter == null)
        {
            return 0;
        }
        float splitValue = value * 0.2f;
        float finalValue = 0;
        if (target == null)
        {
            return 0;
        }
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            if (i == (int)DamageTypes.PHYSICAL) continue;
            float baseDamage = splitValue * origFighter.cachedBattleData.damageExternalMods[i];
            baseDamage *= target.cachedBattleData.resistances[i].multiplier;
            baseDamage += target.cachedBattleData.resistances[i].flatOffset;
            if (target.cachedBattleData.resistances[i].absorb) baseDamage = 0;
            finalValue += baseDamage;
        }
        return finalValue;
    }

    public static float ReduceDamage75(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        value = CombatManagerScript.bufferedCombatData.damage * -0.75f;

        return value;
    }

    public static float ReduceDamage50(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        value = CombatManagerScript.bufferedCombatData.damage * -0.5f;

        return value;
    }

    public static float CollectiveStrengthBoost(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        float mult = 0.08f * SharaModeStuff.GetNumberOfDominatedCreatures();
        if (mult > 0.4f)
        {
            mult = 0.4f;
        }
        value = CombatManagerScript.bufferedCombatData.damage * mult;

        return value;
    }

    public static float ReduceCrushEffect(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        if (target.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = target as Monster;
            if (mn.isBoss)
            {
                value *= 0.5f;
            }
            if (mn.isChampion)
            {
                value *= 0.5f;
            }
        }

        return value;
    }

    public static float DaggerMastery3Crit(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        Fighter ft = parent.selfActor as Fighter;
        
        int numAttacksOnLast = ft.consecutiveAttacksOnLastActor;
        if (numAttacksOnLast > 5) numAttacksOnLast = 5;

        if (parent == null)
        {
            // Relink this to the correct status.
            foreach(StatusEffect se in ft.myStats.GetAllStatuses())
            {
                if (se.refName == "daggermastery3")
                {
                    parent = se.listEffectScripts[0] as EmpowerAttackEffect;
                    break;
                }
            }
            if (parent == null)
            {
                Debug.Log("WARNING: Dagger mastery 3 effect has no parent! Player does not have the correct status! Should never happen!");
                return 0f;
            }
        }

        parent.extraChanceToCritFlat = numAttacksOnLast * 0.06f;

        return value;
    }

    public static float ScaleDamageFromCT(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        float currentCT = GameMasterScript.heroPCActor.actionTimer - 100f;
        if (currentCT <= 0f)
        {
            return value;
        }
        //Debug.Log("Current CT: " + currentCT);
        currentCT *= 0.01f;
        currentCT *= 0.25f;
        //Debug.Log("Multiplier is: " + currentCT);
        if (currentCT > 0.25f)
        {
            currentCT = 0.25f;
        }
        value += value * currentCT;
        return value;
    }

    public static float ResetCT(Actor originatingActor, Fighter target, float value, EmpowerAttackEffect parent)
    {
        Fighter attacker = originatingActor as Fighter;
        if (attacker == null)
        {
            attacker = parent.selfActor as Fighter;
            if (attacker == null)
            {
                return value;
            }
        }
        attacker.actionTimer = 100f;
        UIManagerScript.RefreshPlayerCT(false);
        return value;
    }

}
