using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class GenericTriggerConditionalFunction
{
    public static Dictionary<string, Func<EffectScript, TriggerConditionStates>> dictDelegates;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        dictDelegates = new Dictionary<string, Func<EffectScript, TriggerConditionStates>>();


        initialized = true;
    }

    public static void CacheScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        //Debug.Log("Caching " + scriptName);

        MethodInfo myMethod = typeof(GenericTriggerConditionalFunction).GetMethod(scriptName, new Type[] { typeof(EffectScript) });

        Func<EffectScript, TriggerConditionStates> converted =
            (Func<EffectScript, TriggerConditionStates>)Delegate.CreateDelegate(typeof(Func<EffectScript, TriggerConditionStates>), myMethod);

        dictDelegates.Add(scriptName, converted);
    }

    public static TriggerConditionStates AbilityCostsEnergyOrStamina(EffectScript effect)
    {
        if (GameMasterScript.heroPCActor.ReadActorData("lastenergyspent") > 0 || GameMasterScript.heroPCActor.ReadActorData("laststaminaspent") > 0)
        {
            return TriggerConditionStates.PASSTHROUGH;
        }

        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates LastKillWasAbility(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;
        if (ft.ReadActorDataString("lastkill") != "abil")
        {
            return TriggerConditionStates.INVALID;
        }

        return TriggerConditionStates.PASSTHROUGH;
    }

    public static TriggerConditionStates OnlyPhysicalSkills(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        string abilRef = GameMasterScript.gmsSingleton.ReadTempStringData("last_abilityref_used");

        AbilityScript check;

        if (!GameMasterScript.masterAbilityList.TryGetValue(abilRef, out check))
        {
            return TriggerConditionStates.INVALID;
        }

        foreach(EffectScript eff in check.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                DamageEffect de = eff as DamageEffect;
                if (de.damType == DamageTypes.PHYSICAL)
                {
                    return TriggerConditionStates.VALID;
                }
            }
        }

        return TriggerConditionStates.INVALID;

    }

    public static TriggerConditionStates HasMinimum3Energy(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        if (ft.myStats.GetCurStat(StatTypes.ENERGY) < 3f)
        {
            return TriggerConditionStates.INVALID;
        }

        return TriggerConditionStates.VALID;
    }
    
    public static TriggerConditionStates AllowOnlyIfUsingItem(EffectScript effect)
    {
        if (GameMasterScript.gmsSingleton.ReadTempGameData("useditem") == 1)
        {
            return TriggerConditionStates.VALID;
        }

        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates MModVibratingTriggerCondition(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        int stacks = ft.ReadActorData("vibrating_stacks");
        if (stacks < 0)
        {
            stacks = 0;
        }

        bool valid = false;

        stacks++;

        if (stacks >= 3)
        {
            valid = true;
            stacks = 0;
        }

        ft.SetActorData("vibrating_stacks", stacks);

        if (valid)
        {
            return TriggerConditionStates.VALID;
        }
        else
        {
            return TriggerConditionStates.INVALID;
        }

    }

    public static TriggerConditionStates MModIcicleTriggerCondition(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        int stacks = ft.ReadActorData("icicle_stacks");
        if (stacks < 0)
        {
            stacks = 0;
        }

        bool valid = false;

        stacks++;

        if (stacks >= 4)
        {
            valid = true;
            stacks = 0;
        }

        ft.SetActorData("icicle_stacks", stacks);

        if (valid)
        {
            return TriggerConditionStates.VALID;
        }
        else
        {
            return TriggerConditionStates.INVALID;
        }
        
    }

    public static TriggerConditionStates CheckForAndRemoveMashStacks(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        int stacks = ft.myStats.CheckStatusQuantity("status_mashcharge");

        if (stacks < 3) return TriggerConditionStates.INVALID;

        ft.myStats.RemoveAllStatusByRef("status_mashcharge");

        BattleTextManager.NewText(StringManager.GetString("misc_mashed"), ft.GetObject(), Color.yellow, 0.1f, 2f);

        return TriggerConditionStates.VALID;
    }    

    public static TriggerConditionStates CheckForLastAttackWeaponType(EffectScript effect)
    {
        if (GameMasterScript.heroPCActor.lastWeaponTypeUsed == WeaponTypes.BOW)
        {
            return TriggerConditionStates.VALID;
        }

        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates PreviousAbilityWasPhysicalDamage(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        AbilityUsageInstance aui = CombatManagerScript.GetLastUsedAbility();

        bool anyElementalDamage = false;
        bool anyPhysicalDamage = false;

        foreach (EffectScript eff in aui.abilityRef.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                DamageEffect de = eff as DamageEffect;
                if (de.damType != DamageTypes.PHYSICAL && !de.inheritWeaponDamageType)
                {
                    anyElementalDamage = true;
                    break;
                }
                else if (de.damType == DamageTypes.PHYSICAL)
                {
                    anyPhysicalDamage = true;
                }
            }
        }

        if (anyElementalDamage || !anyPhysicalDamage)
        {
            return TriggerConditionStates.INVALID;
        }

        return TriggerConditionStates.VALID;
    }

    public static TriggerConditionStates PreviousAbilityWasElementalDamage(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;

        AbilityUsageInstance aui = CombatManagerScript.GetLastUsedAbility();

        bool anyElementalDamage = false;

        foreach(EffectScript eff in aui.abilityRef.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                DamageEffect de = eff as DamageEffect;
                if (de.damType != DamageTypes.PHYSICAL && !de.inheritWeaponDamageType)
                {
                    anyElementalDamage = true;
                    break;
                }
            }
        }

        if (anyElementalDamage)
        {
            return TriggerConditionStates.VALID;
        }

        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates TriggerOnlyIfUndamagedForABit(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;
        if (ft.turnsSinceLastDamaged > 3 && ft.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) < 0.95f)
        {
            return TriggerConditionStates.VALID;
        }
        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates OriginatingIsUnfriendly(EffectScript effect)
    {
        if (effect.originatingActor != null && effect.originatingActor.actorfaction != Faction.PLAYER)
        {
            return TriggerConditionStates.VALID;
        }

        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates PhoenixWingCheck(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;
        if (ft.myStats.CheckHasActiveStatusName("spiritwalk") || ft.myStats.CheckHasActiveStatusName("invisible"))
        {
            return TriggerConditionStates.INVALID;
        }

        if (effect.originatingActor.ReadActorData("phoenixwingturn") == GameMasterScript.turnNumber)
        {
            // Only one phoenix wing proc allowed per turn.
            return TriggerConditionStates.INVALID;
        }

        effect.originatingActor.SetActorData("phoenixwingturn", GameMasterScript.turnNumber);

        return TriggerConditionStates.PASSTHROUGH;
    }

    public static TriggerConditionStates InkCollectorCondition(EffectScript effect)
    {
        if (CombatManagerScript.bufferedCombatData == null)
        {
            return TriggerConditionStates.INVALID;
        }
        Monster m = CombatManagerScript.bufferedCombatData.attacker as Monster;
        if (m != null)
        {
            if (m.GetXPModToPlayer() <= 0.03f) return TriggerConditionStates.INVALID;
        }

        m = CombatManagerScript.bufferedCombatData.defender as Monster;
        if (m != null)
        {
            if (m.GetXPModToPlayer() <= 0.03f) return TriggerConditionStates.INVALID;
        }

        if (CombatManagerScript.bufferedCombatData.ability != null && 
            CombatManagerScript.bufferedCombatData.ability.refName == "skill_inkstorm")
        {
            return TriggerConditionStates.INVALID;
        }

        return TriggerConditionStates.PASSTHROUGH;
    }

    public static TriggerConditionStates CheckForGoldblock(EffectScript effect)
    {
        if (GameMasterScript.heroPCActor.GetMoney() % 2 == 0)
        {
            //if (Debug.isDebugBuild) Debug.Log("Working, valid!");
            return TriggerConditionStates.VALID;
        }
        //if (Debug.isDebugBuild) Debug.Log("Not valid!");
        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates CheckTargetInRange(EffectScript effect)
    {
        if (effect.GetTargetActorsAndUpdateBuildActorsToProcess().Count == 0)
        {
            return TriggerConditionStates.INVALID;
        }
        int abilRange = effect.parentAbility.range;
        Actor user = effect.originatingActor;
        Actor target = effect.GetTargetActorsAndUpdateBuildActorsToProcess()[0];
        if (MapMasterScript.GetGridDistance(user.GetPos(), target.GetPos()) <= abilRange)
        {
            return TriggerConditionStates.VALID;
        }
        return TriggerConditionStates.INVALID;
    }


    public static TriggerConditionStates OnlyAffectBanditAttacker(EffectScript effect)
    {
        if (CombatManagerScript.bufferedCombatData == null)
        {
            return TriggerConditionStates.INVALID;
        }
        Monster m = CombatManagerScript.bufferedCombatData.attacker as Monster;
        if (m == null)
        {
            return TriggerConditionStates.INVALID;
        }
        if (m.isChampion || m.isBoss)
        {
            return TriggerConditionStates.INVALID;
        }
        if (m.monFamily != "bandits")
        {
            return TriggerConditionStates.INVALID;
        }

        return TriggerConditionStates.PASSTHROUGH;
    }

    public static TriggerConditionStates IsThaneSongAtMaxLevel(EffectScript effect)
    {
        int songLevel = GameMasterScript.heroPCActor.GetThaneSongLevel();
        if (songLevel == 3)
        {
            // We're going to 
            return TriggerConditionStates.VALID;
        }
        return TriggerConditionStates.INVALID;
    }

    

    public static TriggerConditionStates IsVineBurstAvailableToPlayer(EffectScript effect)
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_floramancer_tier2_vineburst"))
        {
            return TriggerConditionStates.VALID;
        }
        return TriggerConditionStates.INVALID;
    }
    
    public static TriggerConditionStates TriggerOnlyAfterStep(EffectScript effect)
    {
        Fighter ft = effect.originatingActor as Fighter;
        if (ft == null)
        {
            return TriggerConditionStates.INVALID;
        }
        Vector2 cPos = ft.GetPos();
        if (ft.movedLastTurn || cPos != ft.previousPosition)
        {
            return TriggerConditionStates.VALID;
        }
        else
        {
            return TriggerConditionStates.INVALID;
        }
    }
    
    public static TriggerConditionStates OnlyTriggerFromBudokaTech(EffectScript effect)
    {
        string abilRef = GameMasterScript.gmsSingleton.ReadTempStringData("last_abilityref_used");
        AbilityScript playerAbil = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(abilRef);
        if (playerAbil == null) return TriggerConditionStates.INVALID; // must be an item
        if (playerAbil.jobLearnedFrom == CharacterJobs.BUDOKA)
        {
            foreach(EffectScript eff in playerAbil.listEffectScripts)
            {
                //if (eff.effectType == EffectType.DAMAGE || eff.effectType == EffectType.MOVEACTOR)
                {
                    return TriggerConditionStates.VALID;
                }
            }            
        }
        
        {
            return TriggerConditionStates.INVALID;
        }
    }

    public static TriggerConditionStates PlayerUsedWildHorse(EffectScript effect)
    {
        if (GameMasterScript.gmsSingleton.ReadTempStringData("last_abilityref_used").Contains("wildhorse"))
        {
            return TriggerConditionStates.VALID;
        }
        else
        {
            return TriggerConditionStates.INVALID;
        }
    }

    public static TriggerConditionStates TargetOnlyNonHostileMonster(EffectScript effect)
    {
        Monster mn = CombatManagerScript.bufferedCombatData.defender as Monster;
        if (mn != null)
        {
            if (mn.myBehaviorState == BehaviorState.NEUTRAL || mn.myBehaviorState == BehaviorState.RUN)
            {
                return TriggerConditionStates.VALID;
            }
            else
            {
                return TriggerConditionStates.INVALID;
            }
        }
        return TriggerConditionStates.VALID;
    }

    public static TriggerConditionStates CheckIfStarcallerValid(EffectScript effect)
    {
        CombatManagerScript.ProcessDamagePayload pdp = CombatManagerScript.damagePayload;

        /* if (MapMasterScript.GetGridDistance(CombatManagerScript.damagePayload.atk.GetPos() ,CombatManagerScript.damagePayload.def.GetPos()) == 1)
        {
            return TriggerConditionStates.INVALID;
        } */

        if (CombatManagerScript.bufferedCombatData != null)
        {
            if (CombatManagerScript.bufferedCombatData.atkType == AttackType.ATTACK) return TriggerConditionStates.PASSTHROUGH;

            if (CombatManagerScript.bufferedCombatData.atkType != AttackType.ATTACK && CombatManagerScript.bufferedCombatData.ability == null)
            {
                return TriggerConditionStates.INVALID;
            }            
            Debug.Log(CombatManagerScript.bufferedCombatData.ability.refName);
            if (CombatManagerScript.bufferedCombatData.ability.refName == "starcaller")
            {
                return TriggerConditionStates.INVALID;
            }
        }
        else
        {
            Debug.Log("Null data.");
            return TriggerConditionStates.INVALID;
        }

        /* if (pdp.effParent != null)
        {
            Debug.Log("Eff parent is " + pdp.effParent.effectRefName);
            if (pdp.effParent.parentAbility != null)
            {
                // is this a status effect?
                Debug.Log(pdp.effParent.parentAbility.abilityName);
            }
        } */

        return TriggerConditionStates.PASSTHROUGH;
    }

    public static TriggerConditionStates CheckForLowLifeHeal(EffectScript effect)
    {
        if (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f)
        {
            return TriggerConditionStates.VALID;
        }
        return TriggerConditionStates.INVALID;
    }

    public static TriggerConditionStates CheckForGoldArmor(EffectScript effect)
    {
        if (UnityEngine.Random.Range(0,1f) > 0.2f)
        {
            return TriggerConditionStates.INVALID;
        }
        int cost = GameMasterScript.heroPCActor.myStats.GetLevel() * 50;
        if (GameMasterScript.heroPCActor.GetMoney() < cost)
        {
            return TriggerConditionStates.INVALID;
        }
        GameMasterScript.heroPCActor.ChangeMoney(cost * -1);
        BattleTextManager.NewText("-" + cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD), GameMasterScript.heroPCActor.GetObject(), Color.yellow, 0.3f, 1f, BounceTypes.STANDARD, true);
        return TriggerConditionStates.VALID;
    }
}

public class GenericPreEffectFunctions {

    /// <summary>
    /// Execute 3 attacks split at random between actorsToProcess. The same actor can be hit multiple times.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="actorsToProcess"></param>
    /// <returns></returns>
    public static EffectResultPayload PickUpToThreeTargets(EffectScript effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (actorsToProcess.Count == 0)
        {
            return erp;
        }

        List<Actor> newActorsToProcess = new List<Actor>();
        for (int i = 0; i < 3; i++)
        {
            newActorsToProcess.Add(actorsToProcess.GetRandomElement());
        }

        erp.actorsToProcess = newActorsToProcess;

        return erp;
    }    

    public static EffectResultPayload TargetSummonsOnly(EffectScript effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        List<Actor> newActorsToProcess = new List<Actor>();
        foreach(Actor act in actorsToProcess)
        {
            if (act.summoner != null && act.GetActorType() == ActorTypes.MONSTER && act != GameMasterScript.heroPCActor.GetMonsterPet())
            {
                if (act.turnsToDisappear > 0 && act.maxTurnsToDisappear > 0)
                {
                    newActorsToProcess.Add(act);
                }                
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = newActorsToProcess;
        return erp;
    }

    public static EffectResultPayload ReactiveRegenCheckDamageReceived(EffectScript effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.bufferedCombatData == null)
        {
            return erp;
        }

        float playerMaxHP = GameMasterScript.heroPCActor.myStats.GetMaxStat(StatTypes.HEALTH);
        float dmg = CombatManagerScript.bufferedCombatData.lastDamageAmountReceived;

        if (dmg / playerMaxHP >= 0.1f)
        {
            // good
        }
        else
        {
            actorsToProcess.Clear();
        }

        erp.waitTime = 0;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload MoonbeamConvertToBuff(EffectScript effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        List<Actor> toRemove = new List<Actor>();
        foreach(Actor act in actorsToProcess)
        {
            if (!act.IsFighter()) continue;
            if (act.actorfaction == Faction.PLAYER)
            {                
                Fighter ft = act as Fighter;
                if (!ft.myStats.IsAlive()) continue;
                toRemove.Add(act);

                ft.myStats.AddStatusByRefAndLog("status_storyattackup", GameMasterScript.heroPCActor, 6);
                CombatManagerScript.GenerateSpecificEffectAnimation(ft.GetPos(), "FervirBuff", effect, true);
            }
        }
        foreach(Actor act in toRemove)
        {
            actorsToProcess.Remove(act);
        }

        erp.actorsToProcess = actorsToProcess;
        return erp;
    }
}
