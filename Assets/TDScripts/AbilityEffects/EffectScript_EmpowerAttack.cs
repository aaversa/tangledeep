using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class EmpowerAttackEffect : EffectScript
{
    public List<AttackConditions> theConditions = new List<AttackConditions>();
    public string effectEquation;
    public float baseDamage = 0.0f;
    public float maxExtraDamageAsPercent = 1.0f; // Adds at most an additional (up to) 100% of original damage
    public bool silentEmpower;
    public float extraChanceToCrit = 1.0f;
    public float extraChanceToCritFlat = 0.0f;
    public string script_attackModifier;

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        EmpowerAttackEffect nTemplate = (EmpowerAttackEffect)template as EmpowerAttackEffect;
        theConditions = nTemplate.theConditions;
        effectEquation = nTemplate.effectEquation;
        effectPower = nTemplate.effectPower;
        silentEmpower = nTemplate.silentEmpower;
        extraChanceToCrit = nTemplate.extraChanceToCrit;
        extraChanceToCritFlat = nTemplate.extraChanceToCritFlat;
        script_attackModifier = nTemplate.script_attackModifier;
        maxExtraDamageAsPercent = 400f;
        for (int i = 0; i < (int)EDamageEquationVars.COUNT; i++)
        {
            damageEquationVars[i] = nTemplate.damageEquationVars[i];
        }
        anyDamageEquationVars = nTemplate.anyDamageEquationVars;
    }

    public EmpowerAttackEffect()
    {
        effectPower = 0;
        effectEquation = "";
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        EmpowerAttackEffect eff = compareEff as EmpowerAttackEffect;
        if (effectEquation != eff.effectEquation) return false;
        if (effectPower != eff.effectPower) return false;
        if (baseDamage != eff.baseDamage) return false;
        if (maxExtraDamageAsPercent != eff.maxExtraDamageAsPercent) return false;
        if (silentEmpower != eff.silentEmpower) return false;
        if (extraChanceToCrit != eff.extraChanceToCrit) return false;
        if (extraChanceToCritFlat != eff.extraChanceToCritFlat) return false;
        foreach (AttackConditions ac in theConditions)
        {
            if (!eff.theConditions.Contains(ac)) return false;
        }
        return true;

    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        // Flesh out more conditions here.
        float value = 0.0f;

        Fighter fight = selfActor as Fighter;
        bool valid = false;

        // If we have bad self/originating actor data and the function below can't fix it, don't do anything.
        if (!VerifySelfActorIsFighterAndFix())
        {
            return 0f;
        }
        VerifyOriginatingActorIsFighterAndFix();

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        if (effectRefName == "potpie")
        {
            maxExtraDamageAsPercent = 5f;
        }
        if (effectRefName == "swordmastery3_riposte")
        {
            if (!CombatManagerScript.bufferedCombatData.counterAttack)
            {
                return 0.0f;
            }
            Fighter ft = originatingActor as Fighter;
            if (ft.myEquipment.GetWeaponType() != WeaponTypes.SWORD)
            {
                return 0.0f;
            }
        }

        List<Actor> toProcess = new List<Actor>() { fight };
        valid = EvaluateTriggerCondition(toProcess);

        /* foreach (AttackConditions ac in theConditions)
        {
            switch (ac)
            {
                case AttackConditions.TARGET_ISOLATED:
                    List<Actor> nearbyMonsters = MapMasterScript.GetMonstersAroundTile(CombatManagerScript.bufferedCombatData.defender.GetPos());
                    if (nearbyMonsters.Count == 0)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.DEFENDER_SAMEANGLE:
                    // Attacker was attacked FROM the North. Now it's receiving an attack going South. So it's the same angle.
                    if (CombatManagerScript.bufferedCombatData.attackDirection == CombatManagerScript.bufferedCombatData.defender.lastDirectionAttackedFrom)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.DEFENDER_DIFFERENTANGLE:
                    if (CombatManagerScript.bufferedCombatData.attackDirection != CombatManagerScript.bufferedCombatData.defender.lastDirectionAttackedFrom)
                    {
                        valid = true;
                    }
                    break;

                case AttackConditions.DEFENDER_REVERSEANGLE:
                    if (CombatManagerScript.bufferedCombatData.attackDirection == MapMasterScript.oppositeDirections[(int)CombatManagerScript.bufferedCombatData.defender.lastDirectionAttackedFrom])
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.PARENTABILITYANGLE:
                    if (CombatManagerScript.bufferedCombatData.attackDirection == parentAbility.direction)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.OPPOSITE_PARENTABILITYANGLE:
                    if (CombatManagerScript.bufferedCombatData.attackDirection == MapMasterScript.oppositeDirections[(int)parentAbility.direction])
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.SAMEACTOR:
                    if (fight.lastActorAttacked == CombatManagerScript.bufferedCombatData.defender)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.TARGET_STUNNED:
                    //if ((CombatManagerScript.bufferedCombatData.defender != null) && ((CombatManagerScript.bufferedCombatData.defender.myStats.CheckHasStatusName("status_basicstun")) || (CombatManagerScript.bufferedCombatData.defender.myStats.CheckHasStatusName("status_tempstun"))))
                    if ((CombatManagerScript.bufferedCombatData.defender != null) 
                        && (CombatManagerScript.bufferedCombatData.defender.turnsSinceLastStun <= 2))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.ANY:
                    valid = true;
                    break;
                case AttackConditions.WEAPON_BLADES:
                    if (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.DAGGER
                        || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SWORD
                        || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SPEAR
                        || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.CLAW
                        || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.AXE
                        )
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.WEAPON_PIERCE:
                    if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.DAGGER) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SPEAR))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.WEAPON_SLASH:
                    if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.AXE) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SWORD) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.CLAW))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.WEAPON_BLUNT:
                    if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.NATURAL) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.MACE))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.DEFENDER_NOT_TARGETING:
                    if (!CombatManagerScript.bufferedCombatData.defender.CheckTarget(originatingActor))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.DEFENDER_BELOWHEALTH:
                    if (CombatManagerScript.bufferedCombatData.defender.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= defenderBelowHealth)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.DEFENDER_ABOVEHEALTH:
                    if (CombatManagerScript.bufferedCombatData.defender.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > defenderAboveHealth)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.ATTACKER_BELOWHEALTH:
                    if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= attackerBelowHealth)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.ATTACKER_ABOVEHEALTH:
                    if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > attackerAboveHealth)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.MELEE:
                    if (MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) <= 1)
                    {
                        valid = true;
                    }
                    break;

                case AttackConditions.ATTACKER_MELEEWEAPON:
                    if (!CombatManagerScript.bufferedCombatData.attacker.myEquipment.IsCurrentWeaponRanged())
                    {
                        valid = true;
                    }
                    break;

                case AttackConditions.ATTACKER_RANGEDWEAPON:
                    if (MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) > 1)
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.MAXRANGE:
                    int distance = MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos());
                    if (distance >= (CombatManagerScript.bufferedCombatData.attacker.GetMaxAttackRange() * 0.9f))
                    {
                        valid = true;
                    }
                    break;
                case AttackConditions.ATTACKER_CHAMPION:
                    if (CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mon = CombatManagerScript.bufferedCombatData.attacker as Monster;
                        if (mon.isChampion)
                        {
                            valid = true;
                        }
                    }
                    break;
                case AttackConditions.DEFENDER_CHAMPION:
                    if (CombatManagerScript.bufferedCombatData.defender.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mon = CombatManagerScript.bufferedCombatData.defender as Monster;
                        if (mon.isChampion)
                        {
                            valid = true;
                        }
                    }
                    break;
            }
            if (!valid)
            {
                break;
            }
        } */

        if (!valid)
        {
            return 0.0f;
        }


        baseDamage = CombatManagerScript.bufferedCombatData.damage;

        //Debug.Log("Run effect " + effectName + " " + effectRefName + " on base damage " + baseDamage);

        int numAttacks = fight.consecutiveAttacksOnLastActor;
        float wPower = fight.cachedBattleData.physicalWeaponDamage + (fight.cachedBattleData.physicalWeaponOffhandDamage * .75f);
        float targetCur = CombatManagerScript.bufferedCombatData.defender.myStats.GetCurStat(StatTypes.HEALTH);

        if (anyDamageEquationVars)
        {
            value = effectPower;
            value += wPower * damageEquationVars[(int)EDamageEquationVars.ATK_WEAPON_POWER];
            value += baseDamage * damageEquationVars[(int)EDamageEquationVars.BASE_DAMAGE];
            value += fight.cachedBattleData.spiritPower * damageEquationVars[(int)EDamageEquationVars.ATK_SPIRIT_POWER];
            value += targetCur * damageEquationVars[(int)EDamageEquationVars.DEF_CUR_HP];
            value += fight.myStats.GetCurStat(StatTypes.HEALTH) * damageEquationVars[(int)EDamageEquationVars.CUR_HP];
            value += CombatManagerScript.bufferedCombatData.defender.myStats.GetMaxStat(StatTypes.HEALTH) * damageEquationVars[(int)EDamageEquationVars.DEF_MAX_HP];
            value += fight.myStats.GetLevel() * damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL];

            if (damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
            {
                value += UnityEngine.Random.Range(damageEquationVars[(int)EDamageEquationVars.RND_MIN], damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(effectEquation))
            {
                //ExpressionParser fParser = new ExpressionParser();        
                fParser.AddConst("$AttackerWeaponPower", () => wPower);
                fParser.AddConst("$AtkSpiritPower", () => fight.cachedBattleData.spiritPower);
                fParser.AddConst("$EffectPower", () => effectPower);
                fParser.AddConst("$BaseDamage", () => baseDamage);
                fParser.AddConst("$TargetCurHealth", () => targetCur);
                fParser.AddConst("$NumAttacks", () => numAttacks);

                value = (float)fParser.Evaluate(effectEquation);

                //Debug.Log("Value prior to script is " + value);
            }
        }




        if (!string.IsNullOrEmpty(script_attackModifier))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(AttackModifierFunctions), script_attackModifier);
            object[] paramList = new object[4];
            paramList[0] = originatingActor;
            paramList[1] = fight;
            paramList[2] = value;
            paramList[3] = this;
            object returnObj = runscript.Invoke(null, paramList);
            if (returnObj == null)
            {
                Debug.Log("No object for " + script_attackModifier);
            }
            float.TryParse(returnObj.ToString(), out value);
        }

        if (effectRefName == "mmwild")
        {
            value = UnityEngine.Random.Range(baseDamage * -0.35f, baseDamage * 0.35f);
        }
        else if (effectRefName == "empoweraxemastery2")
        {
            int count = MapMasterScript.GetFactionMonstersAroundTile(originatingActor.GetPos(), Faction.ENEMY).Count;
            //Debug.Log(count);
            value = (0.05f * count);
        }



        //Debug.Log(effectName + " Empowered extra damage: " + value + " " + maxExtraDamageAsPercent + " " + (baseDamage * maxExtraDamageAsPercent) + " " + baseDamage);

        if (value > (baseDamage * maxExtraDamageAsPercent))
        {
            value = (baseDamage * maxExtraDamageAsPercent);
        }

        //Debug.Log("Value added to attack is " + value);

        if (silentEmpower)
        {
            CombatManagerScript.bufferedCombatData.silent = true;
        }

        CombatManagerScript.ModifyBufferedDamage(value);

        if (extraChanceToCrit != 1.0f)
        {
            CombatManagerScript.ModifyChanceToCrit(extraChanceToCrit);
        }
        if (extraChanceToCritFlat != 0)
        {
            CombatManagerScript.ModifyChanceToCritFlat(extraChanceToCritFlat);
        }


        if (PlayerOptions.animSpeedScale != 0)
        {
            value *= PlayerOptions.animSpeedScale;
        }


        return value;
    }
}