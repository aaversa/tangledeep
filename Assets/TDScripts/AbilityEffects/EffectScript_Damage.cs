using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;

public enum DamageEffectFlags { BLEED, COUNT };

// These are used as a replacement for the deprecated EffectEquation and ExpressionParser
public enum EDamageEquationVars { ATK_WEAPON_POWER, ATK_SPIRIT_POWER, ATK_LEVEL, DEF_MAX_HP, DEF_CUR_HP, COMBAT_DAMAGE,
    BASE_DAMAGE, BUFFER_DAMAGE, BLOCK_DMG, RND_MIN, RND_MAX, MAX_STAT, CUR_STAT, CUR_HP, ORIG_CUR_HP, COUNT }

public class DamageEffect : EffectScript
{
    public DamageTypes damType;
    public string effectEquation;
    public float floorValue;
    public float ceilingValue;
    public float lastDamageAmount;
    public float missChance;
    public bool canCrit;
    public bool damageItem;
    public bool canBeParriedOrBlocked;
    public bool noDodgePossible;
    public string script_modifyDamage;
    public AttackConditions runCondition = AttackConditions.ANY;
    //public List<DamageEffectFlags> damFlags;
    public bool[] damFlags;
    public bool inheritWeaponDamageType;
    public bool anyVitalPoint; // This is a local variable only, no need to use it in the template

    public List<Actor> actorsToProcess;

    public DamageEffect()
    {
        floorValue = 0;
        ceilingValue = 9999f;
        //damFlags = new List<DamageEffectFlags>();
        damFlags = new bool[(int)DamageEffectFlags.COUNT];
        damType = DamageTypes.PHYSICAL;
        actorsToProcess = new List<Actor>();
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        DamageEffect nTemplate = template as DamageEffect;
        runCondition = nTemplate.runCondition;
        tActorType = nTemplate.tActorType;
        damType = nTemplate.damType;
        missChance = nTemplate.missChance;
        effectPower = nTemplate.effectPower;
        effectEquation = nTemplate.effectEquation;
        floorValue = nTemplate.floorValue;
        ceilingValue = nTemplate.ceilingValue;
        canCrit = nTemplate.canCrit;
        damageItem = nTemplate.damageItem;
        canBeParriedOrBlocked = nTemplate.canBeParriedOrBlocked;
        script_modifyDamage = nTemplate.script_modifyDamage;
        noDodgePossible = nTemplate.noDodgePossible;
        inheritWeaponDamageType = nTemplate.inheritWeaponDamageType;

        for (int i = 0; i < nTemplate.damFlags.Length; i++)
        {
            damFlags[i] = nTemplate.damFlags[i];
        }
        for (int i = 0; i < (int)EDamageEquationVars.COUNT; i++)
        {
            damageEquationVars[i] = nTemplate.damageEquationVars[i];
        }
        anyDamageEquationVars = nTemplate.anyDamageEquationVars;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        DamageEffect eff = compareEff as DamageEffect;

        if (runCondition != eff.runCondition) return false;
        if (damType != eff.damType) return false;
        if (effectPower != eff.effectPower) return false;
        if (effectEquation != eff.effectEquation) return false;
        if (canCrit != eff.canCrit) return false;
        if (damageItem != eff.damageItem) return false;
        if (canBeParriedOrBlocked != eff.canBeParriedOrBlocked) return false;
        if (script_modifyDamage != eff.script_modifyDamage) return false;

        return true;
    }
    public override float DoEffect(int indexOfEffect = 0)
    {
        affectedActors.Clear();
        results.Clear();
        actorsToProcess.Clear();
        //List<Actor> actorsToProcess = new List<Actor>();
        Fighter origFighter = null;
        if (originatingActor == null || originatingActor.GetActorType() == ActorTypes.DESTRUCTIBLE || originatingActor.GetActorType() == ActorTypes.ITEM)
        {
            // Use the hero? What happens for floor damage tiles?
            originatingActor = GameMasterScript.heroPCActor;
            origFighter = GameMasterScript.heroPCActor;
            if (effectRefName.Contains("floorspikes"))
            {
                origFighter = GameMasterScript.genericMonster;
                originatingActor = GameMasterScript.genericMonster;
            }            
#if UNITY_EDITOR
            //Debug.Log(effectName + " " + effectRefName + " damage effect has no originating fighter.");
#endif
        }
        else
        {
            origFighter = originatingActor as Fighter;
        }
        if (origFighter == null)
        {
            Debug.Log("Fighter STILL null for effect " + effectName + " " + effectRefName + " " + originatingActor.actorUniqueID + " " + originatingActor.actorRefName);
            return 0f;
        }
        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        actorsToProcess.Clear();
        GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }        
        

        bool valid = false;

        // Flesh this out more.

        if (EvaluateTriggerCondition(actorsToProcess))
        {
            valid = true;
        }

        if (effectRefName == "eff_reflectdamagemelee" && valid)
        {
            if (CombatManagerScript.bufferedCombatData == null)
            {
                valid = true;
            }
            else if (MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) > 1)
            {
                valid = false;
            }
        }

        if (UnityEngine.Random.Range(0, 1f) < missChance)
        {
            BattleTextManager.NewText(StringManager.GetString("misc_miss"), originatingActor.GetObject(), Color.white, -0.15f);
            return 0.0f;
        }

        if (!valid)
        {
            return 0.0f;
        }

        float addWaitTime = 0.0f;


        EffectResultPayload erp = CheckForPreProcessFunction(actorsToProcess, addWaitTime);
        actorsToProcess = erp.actorsToProcess;
        addWaitTime = erp.waitTime;


        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);

        bool forceSilent = false;

        if (originatingActor.GetActorType() == ActorTypes.MONSTER && GameMasterScript.gmsSingleton.ReadTempGameData("monincombat") == 0)
        {
            forceSilent = true;
        }

        DamageEffectAnimPackage deap = PreProcessAnimationStuff(perTargetAnim, addWaitTime, forceSilent);
        if (deap.failure)
        {
            return 0f;
        }
        perTargetAnim = deap.perTargetAnim;
        addWaitTime = deap.addWaitTime;

        // Check for reflective...

        CheckForDamageReflection(origFighter, actorsToProcess);

        anyVitalPoint = false;

        bool parentAbilityExists = parentAbility != null;
        bool origIsHero = originatingActor.GetActorType() == ActorTypes.HERO;
        foreach (Actor act in actorsToProcess)
        {
            bool targetIsHero = act.GetActorType() == ActorTypes.HERO;
            if (act.GetActorType() == ActorTypes.MONSTER || targetIsHero)
            {
                Fighter fight = act as Fighter;

                if (!fight.myStats.IsAlive()) continue;

                for (int f = 0; f < (int)ActorFlags.COUNT; f++)
                {
                    if (switchFlags[f])
                    {
                        fight.SetFlag((ActorFlags)f, true);
                    }
                }


                float value = GetBaseDamageValue(origFighter, fight);

                if (damageItem && origIsHero)
                {
                    value *= (1f + (origFighter.myStats.CheckStatusQuantity("status_itemdamageup") * 0.25f));
                }

                if (!string.IsNullOrEmpty(script_modifyDamage))
                {
                    value = CheckForScriptModifier(fight, value, actorsToProcess);
                }

                if (origIsHero)
                {
                    if (parentAbilityExists && parentAbility.jobLearnedFrom == CharacterJobs.SWORDDANCER)
                    {
                        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_mmbutterfly"))
                        {
                            value *= 1.25f;
                        }
                    }
                }

                value = EffectScript.CheckForEffectValueModifier(effectRefName, origFighter, value);

                bool crit = false;

                bool skillBlocked = false;
                if (targetIsHero && originatingActor.GetActorType() == ActorTypes.MONSTER)
                {
                    if (parentAbilityExists)
                    {
                        string learnAbility = parentAbility.teachPlayerAbility;
                        if (!string.IsNullOrEmpty(learnAbility) && fight.myStats.CheckHasStatusName("learnmonsterskills") &&
                            fight.myAbilities.HasAbilityRef(learnAbility))
                        {
                            value *= 0.8f;
                            GameLogScript.LogWriteStringRef("log_mon_knownskill", fight, TextDensity.VERBOSE);
                        }
                    }

                    if (canBeParriedOrBlocked)
                    {
                        CombatDataPack cdp = new CombatDataPack();
                        cdp.attacker = origFighter;
                        cdp.defender = fight;
                        cdp.effect = this;
                        cdp.attackerWeapon = origFighter.myEquipment.GetWeapon();
                        cdp.atkType = AttackType.ABILITY;
                        CombatManagerScript.AddToCombatStack(cdp);
                        cdp.damage = value;
                        skillBlocked = CombatManagerScript.CheckForBlock(origFighter, fight, false, value, false);
                        //float blockAmount = fight.myEquipment.GetBlockDamageReduction();
                        if (skillBlocked)
                        {
                            //value *= blockAmount;                            
                            value = CombatManagerScript.bufferedCombatData.damage;
                            CombatManagerScript.GenerateSpecificEffectAnimation(fight.GetPos(), "BlockEffect", null, true);
                        }
                        if (!skillBlocked && fight.myStats.CheckHasActiveStatusName("autobarrier"))
                        {
                            fight.myStats.RemoveStatusByRef("autobarrier");
                            fight.myStats.AddStatusByRef("autobarrierwaiter", GameMasterScript.heroPCActor, 2, false);
                            float dReduction = fight.myEquipment.GetBlockDamageReduction();
                            if (CustomAlgorithms.CompareFloats(dReduction, 1f))
                            {
                                dReduction = 0.65f;
                            }
                            value *= dReduction;
                            skillBlocked = true;
                            CombatManagerScript.GenerateSpecificEffectAnimation(fight.GetPos(), "AutoBarrierEffect", null, true);
                        }
                        if (!skillBlocked && GameMasterScript.heroPCActor.CanHeroParry(AttackType.ABILITY))
                        {
                            bool checkForParry = CombatManagerScript.CheckForParry(origFighter, fight, false, AttackType.ABILITY);
                            if (checkForParry)
                            {
                                CombatManagerScript.DoParryStuff(origFighter, fight, true, 0, false);
                                if (GameStartData.NewGamePlus >= 2 && MysteryDungeonManager.InOrCreatingMysteryDungeon())
                                {
                                    value *= GameStartData.NGPLUSPLUS_PARRY_DAMAGE_MODIFIER;
                                }
                                else
                                {
                                    value = 0f;
                                }
                                
                            }
                        }
                        CombatManagerScript.RemoveFromCombatStack(cdp);
                    }
                }


                float abilCritChance = 0.05f;

                if (inheritWeaponDamageType)
                {
                    damType = origFighter.myEquipment.GetWeaponElement();
                }

                if (origIsHero)
                {
                    abilCritChance = GameMasterScript.heroPCActor.cachedBattleData.critMeleeChance;
                    if (origFighter.myStats.CheckHasStatusName("status_gamblercrit"))
                    {
                        abilCritChance += 0.1f;
                    }

                    if (fight.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster defMon = fight as Monster;
                        if ((defMon.isChampion || defMon.isBoss) && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_wildchildemblem_tier1_champion"))
                        {
                            abilCritChance += 0.5f;
                        }
                    }

                    if (damType == DamageTypes.PHYSICAL && canCrit && UnityEngine.Random.Range(0, 1f) <= 0.15f && origFighter.myStats.CheckHasStatusName("status_fatemiss"))
                    {
                        StringManager.SetTag(0, fight.displayName);
                        GameLogScript.LogWriteStringRef("log_fate_miss");
                        value = 0f;
                        continue;
                    }

                }

                if (canCrit && UnityEngine.Random.Range(0, 1f) <= abilCritChance) // New crit logic - hardcoded ability crit chance 5%
                {
                    if (fight == GameMasterScript.heroPCActor && fight.myStats.CheckHasStatusName("status_steelresolve"))
                    {
                        // No crit
                    }
                    else
                    {
                        float baseCritMult = 1.33f;
                        if (origFighter.GetActorType() == ActorTypes.HERO)
                        {
                            baseCritMult += (origFighter.cachedBattleData.critMeleeDamageMult / 4f);
                        }

                        value *= baseCritMult;
                        crit = true;
                    }

                }

                value = CheckForMediumArmorDodge(fight, value);

                if (targetIsHero && damFlags[(int)DamageEffectFlags.BLEED])
                {
                    if (fight.myStats.CheckHasStatusName("status_mmclotting"))
                    {
                        value *= 0.66f;
                    }
                }

                if (origIsHero && damFlags[(int)DamageEffectFlags.BLEED])
                {
                    if (origFighter.myStats.CheckHasStatusName("emblem_brigand_tier0_bleedbonus"))
                    {
                        value *= 1.12f;
                    }
                }

                if (parentAbilityExists && parentAbility.CheckAbilityTag(AbilityTags.ONHITPROPERTIES))
                {
                    CombatDataPack cdp = new CombatDataPack();
                    CombatManagerScript.AddToCombatStack(cdp);
                    //Debug.Log("Add new CDP to stack.");
                    CombatManagerScript.bufferedCombatData.damage = value;
                    CombatManagerScript.bufferedCombatData.attacker = origFighter;
                    CombatManagerScript.bufferedCombatData.defender = act as Fighter;
                    CombatManagerScript.bufferedCombatData.attackerWeapon = origFighter.myEquipment.GetWeapon();
                    CombatManagerScript.bufferedCombatData.flavorDamageType = origFighter.myEquipment.GetFlavorDamageType(origFighter.myEquipment.GetWeapon());
                    CombatManagerScript.bufferedCombatData.damageType = origFighter.myEquipment.GetDamageType(origFighter.myEquipment.GetWeapon());
                    CombatManagerScript.bufferedCombatData.attackDirection = CombatManagerScript.GetDirection(origFighter, act);
                    origFighter.myStats.CheckRunAllStatuses(StatusTrigger.ATTACK);
                    origFighter.myStats.CheckConsumeAllStatuses(StatusTrigger.ATTACK);
                    GameMasterScript.heroPCActor.TryRefreshStatuses();
                    value += CombatManagerScript.bufferedCombatData.damageMod;
                    value *= CombatManagerScript.bufferedCombatData.damageModPercent;
                    //Debug.Log("Remove new CDP to stack.");
                    CombatManagerScript.RemoveFromCombatStack(cdp);
                }

                if (value < floorValue)
                {
                    value = floorValue;
                }
                if (value > ceilingValue)
                {
                    value = ceilingValue;
                }

                if (playAnimationInstant)
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(fight.GetPos(), spriteEffectRef, this);
                    results.Add(CombatManagerScript.ProcessDamageEffect(origFighter, fight, value, this, false, false, crit));
                }
                else
                {

                    bool localSilent = false;
                    if (silent)
                    {
                        localSilent = true;
                    }

                    if (parentAbilityExists && parentAbility.CheckAbilityTag(AbilityTags.INSTANT))
                    {
                        CombatResultsScript.CheckCombatResult(CombatManagerScript.ProcessDamageEffect(origFighter, fight, value, this, localSilent, perTargetAnim, crit), fight, MapMasterScript.activeMap);
                    }
                    else
                    {
                        results.Add(CombatManagerScript.ProcessDamageEffect(origFighter, fight, value, this, localSilent, perTargetAnim, crit));
                    }
                }

                if (crit && (origIsHero || targetIsHero))
                {
                    GameMasterScript.cameraScript.AddScreenshake(0.2f);
                }




                        // TODO: Does water freeze?!

                lastDamageAmount = value;
                affectedActors.Add(act);
            }
        }

        if (anyVitalPoint)
        {
            origFighter.ChangeCT(100f);
            BattleTextManager.NewText(StringManager.GetExcitedString("misc_combo"), origFighter.GetObject(), Color.cyan, 0.5f);
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


        if (PlayerOptions.animSpeedScale != 0)
        {
            returnVal *= PlayerOptions.animSpeedScale;
        }


        CombatManagerScript.accumulatedCombatWaitTime += returnVal;
		returnVal = Mathf.Clamp(returnVal,-1f,1.5f);
        return returnVal;
    }

    public void CheckForDamageReflection(Fighter origFighter, List<Actor> actorsToProcess)
    {
        if (origFighter.actorfaction == Faction.ENEMY && isProjectile && actorsToProcess.Contains(GameMasterScript.heroPCActor))
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_mmreflective") && UnityEngine.Random.Range(0, 1f) <= 0.25f)
            {
                actorsToProcess.Add(origFighter);
                actorsToProcess.Remove(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("log_gem_reflect");
            }
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("asceticgrab") && UnityEngine.Random.Range(0, 1f) <= 0.25f)
            {
                actorsToProcess.Remove(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("log_ascetic_destroy");
                CombatManagerScript.GenerateEffectAnimation(origFighter.GetPos(), GameMasterScript.heroPCActor.GetPos(), this, GameMasterScript.heroPCActor.GetObject());
            }
        }
    }

    DamageEffectAnimPackage PreProcessAnimationStuff(bool perTargetAnim, float addWaitTime, bool forceSilent = false)
    {
        DamageEffectAnimPackage deap = new DamageEffectAnimPackage();
        deap.perTargetAnim = perTargetAnim;
        deap.addWaitTime = addWaitTime;

        if (delayBeforeAnimStart == 0)
        {
            if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
            {
                deap.perTargetAnim = false;
                // Just play ONE animation for the entire thing.
                Vector2 usePosition = centerPosition;

                if (usePosition == Vector2.zero)
                {
                    if (!VerifySelfActorIsFighterAndFix())
                    {
                        deap.failure = true;
                        return deap;
                    }
                    usePosition = selfActor.GetPos();
                }

                if (centerSpriteOnOriginatingActor)
                {
                    usePosition = originatingActor.GetPos();
                }

                if (centerSpriteOnMiddlePosition)
                {
                    usePosition = centerPosition;
                    if (positions.Count > 0)
                    {
                        usePosition = positions[positions.Count / 2];
                    }
                }

                CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), usePosition, this, originatingActor.GetObject(), forceSilent);
            }
            else if (playAnimation && parentAbility.CheckAbilityTag(AbilityTags.PLAYANIMONEMPTY))
            {
                foreach (Vector2 v2 in positions)
                {
                    GameObject targObject = originatingActor.GetObject();
                    if (isProjectile)
                    {
                        targObject = null;
                    }
                    CombatManagerScript.EnqueueCombatAnimation(new CombatAnimation(originatingActor.GetPos(), v2, this, null, targObject, forceSilent, false, false)); // Target individual fighters?
                }
                if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
                {
                    deap.addWaitTime = positions.Count * animLength;
                }
                deap.perTargetAnim = false;
            }
            else if (!playAnimation)
            {
                deap.perTargetAnim = false;
            }
        }

        return deap;
    }

    public class DamageEffectAnimPackage
    {
        public bool perTargetAnim;
        public float addWaitTime;
        public bool failure;
    }

    float GetBaseDamageValue(Fighter origFighter, Fighter fight)
    {
        float removedStatusDamage = 0.0f;
        foreach (StatusEffect se in fight.myStats.statusesRemovedSinceLastTurn)
        {
            removedStatusDamage += se.estimateRemainingDamage;
        }
        float spiritMult = 1f + origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT);

        float calcSpiritPower = origFighter.cachedBattleData.spiritPower;
        if (origFighter == GameMasterScript.heroPCActor && origFighter.myStats.CheckHasStatusName("status_kineticmagic") && origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.51f)
        {
            calcSpiritPower *= 1.2f;
        }

        float targetCurHealth = fight.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.CUR);
        float targetMaxHealth = fight.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);

        if (fParser == null)
        {
            fParser = new ExpressionParser();
        }

        float offhandPower = origFighter.cachedBattleData.physicalWeaponOffhandDamage;
        offhandPower *= 0.25f;
        float weaponPower = origFighter.cachedBattleData.physicalWeaponDamage;

        float finalWeaponValue = weaponPower + offhandPower;

        if (origFighter.ReadActorData("cached_ability_weaponpower") > 0)
        {
            finalWeaponValue = origFighter.ReadActorData("cached_ability_weaponpower");
            origFighter.RemoveActorData("cached_ability_weaponpower");
        }

        if (string.IsNullOrEmpty(effectEquation))
        {
            effectEquation = "1";
        }
        string localEquation = String.Copy(effectEquation);

        localEquation = localEquation.Replace("$AttackerWeaponPower", finalWeaponValue.ToString());

        if (CombatManagerScript.bufferedCombatData != null)
        {
            localEquation = localEquation.Replace("$BufferDamage", CombatManagerScript.bufferedCombatData.damage.ToString());
            localEquation = localEquation.Replace("$BlockedDamage", CombatManagerScript.bufferedCombatData.blockedDamage.ToString());
        }

        float origHPMissing2x = origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH); // 0.6f 
        origHPMissing2x = 1f - origHPMissing2x; // This gives us missing %. Now we have 0.4
        origHPMissing2x *= 2f; // Now 0.8
        origHPMissing2x += 1f; // Now 1.8

        float origHPMissing3x = origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH); // 0.6f 
        origHPMissing3x = 1f - origHPMissing3x; // This gives us missing %. Now we have 0.4
        origHPMissing3x *= 3f; // Now 0.8
        origHPMissing3x += 1f; // Now 1.8

        float value = 0f;

        if (!anyDamageEquationVars)
        {
            localEquation = localEquation.Replace("$AtkSpiritPower", calcSpiritPower.ToString());
            localEquation = localEquation.Replace("$RemovedStatusDamage", removedStatusDamage.ToString());
            localEquation = localEquation.Replace("$AtkLevel", origFighter.myStats.GetLevel().ToString());
            localEquation = localEquation.Replace("$UserHPMissing3x", origHPMissing3x.ToString());
            localEquation = localEquation.Replace("$TargetCurHealth", targetCurHealth.ToString());
            localEquation = localEquation.Replace("$TargetMaxHealth", targetMaxHealth.ToString());
            localEquation = localEquation.Replace("$AtkHealthPercent", origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH).ToString());
            localEquation = localEquation.Replace("$NumStatusRemoved", fight.myStats.statusesRemovedSinceLastTurn.Count.ToString());

            if (CombatManagerScript.bufferedCombatData != null)
            {
                localEquation = localEquation.Replace("$CombatDamage", CombatManagerScript.bufferedCombatData.damage.ToString());
            }
            value = (float)fParser.Evaluate(localEquation);
        }
        else
        {
            // Alternative to expression parser that is way, way, way faster
            value += finalWeaponValue * damageEquationVars[(int)EDamageEquationVars.ATK_WEAPON_POWER];
            value += calcSpiritPower * damageEquationVars[(int)EDamageEquationVars.ATK_SPIRIT_POWER];
            value += targetCurHealth * damageEquationVars[(int)EDamageEquationVars.DEF_CUR_HP];
            value += targetMaxHealth * damageEquationVars[(int)EDamageEquationVars.DEF_MAX_HP];
            value += origFighter.myStats.GetCurStat(StatTypes.HEALTH) * damageEquationVars[(int)EDamageEquationVars.CUR_HP];
            value += origFighter.myStats.GetLevel() * damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL];

            //Debug.Log(calcSpiritPower + " " + value + " " + damageEquationVars[(int)EDamageEquationVars.ATK_SPIRIT_POWER] + " " + origFighter.cachedBattleData.spiritPower);

            if (damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
            {
                value += UnityEngine.Random.Range(damageEquationVars[(int)EDamageEquationVars.RND_MIN], damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
            }
            if (CombatManagerScript.bufferedCombatData != null)
            {
                value += CombatManagerScript.bufferedCombatData.damage * damageEquationVars[(int)EDamageEquationVars.BUFFER_DAMAGE];
                value += CombatManagerScript.bufferedCombatData.damage * damageEquationVars[(int)EDamageEquationVars.COMBAT_DAMAGE];
                value += CombatManagerScript.bufferedCombatData.blockedDamage * damageEquationVars[(int)EDamageEquationVars.BLOCK_DMG];
            }

            value += effectPower;
        }

        return value;
    }

    float CheckForMediumArmorDodge(Fighter fight, float value)
    {
        if (fight == GameMasterScript.heroPCActor)
        {
            if (fight.myEquipment.GetArmorType() == ArmorTypes.MEDIUM && !noDodgePossible)
            {
                float dodgeChance = (fight.cachedBattleData.dodgeRangedChance + fight.cachedBattleData.dodgeMeleeChange) / 2f;
                if (dodgeChance <= 0.1f)
                {
                    dodgeChance = 0.1f;
                }
                if (UnityEngine.Random.Range(0, 1f) <= dodgeChance)
                {
                    value = 0f;
                    // Display clear message that we dodged / a miss happened due to our Medium armor.
                    BattleTextManager.NewText(StringManager.GetString("misc_miss"), fight.GetObject(), Color.yellow, 0.5f);
                    GameLogScript.LogWriteStringRef("log_skill_dodge", null, TextDensity.VERBOSE);
                }
            }
        }

        return value;
    }

    float CheckForScriptModifier (Fighter fight, float value, List<Actor> actorsToProcess)
    {
        /* Func<Actor, Fighter, float, int, DamageEffect, float> myFunc;
        if (DamageModifierFunctions.dictDelegates.TryGetValue(script_modifyDamage, out myFunc))
        {
            value = myFunc(originatingActor, fight, value, actorsToProcess.Count, this);
        }
        else */
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(DamageModifierFunctions), script_modifyDamage);
            if (runscript == null)
            {
                Debug.Log("WARNING! Could not find damage function " + script_modifyDamage);
            }
            else
            {
                object[] paramList = new object[5];
                paramList[0] = originatingActor;
                paramList[1] = fight;
                paramList[2] = value;
                paramList[3] = actorsToProcess.Count;
                paramList[4] = this;
                object returnObj = runscript.Invoke(null, paramList);
                if (returnObj == null)
                {
                    Debug.Log("No object for " + script_modifyDamage);
                }
                float.TryParse(returnObj.ToString(), out value);
            }
        }

        return value;
    }
}