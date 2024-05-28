using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class ChangeStatEffect : EffectScript
{
    public StatTypes stat;
    public StatDataTypes statData;
    public string effectEquation;
    public float floorValue;
    public float ceilingValue;
    public float accumulatedAmount;
    public bool reverseOnEnd;
    public bool changeSubtypes;
    public string script_effectModifier;

    // New way of bypassing EffectEquation for faster processing.
    public float baseAmount;
    public bool modBySpirit;
    public bool modByDiscipline;

    public ChangeStatEffect()
    {
        floorValue = -9999f;
        ceilingValue = 9999f;
        effectEquation = "0";
        statData = StatDataTypes.CUR;
        reverseOnEnd = false;
        modBySpirit = false;
        baseAmount = 0f;
    }

    public override void ResetAccumulatedAmounts()
    {
        accumulatedAmount = 0;
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        ChangeStatEffect nTemplate = (ChangeStatEffect)template as ChangeStatEffect;
        tActorType = nTemplate.tActorType;

        // Here is our case for RANDOM stats.
        if (nTemplate.stat == StatTypes.RANDOM_CORE || nTemplate.stat == StatTypes.RANDOM_NONRESOURCE || nTemplate.stat == StatTypes.RANDOM_RESOURCE)
        {
            switch (nTemplate.stat)
            {
                case StatTypes.RANDOM_CORE:
                    stat = StatBlock.coreStats[UnityEngine.Random.Range(0, StatBlock.coreStats.Length)];
                    break;
                case StatTypes.RANDOM_NONRESOURCE:
                    stat = StatBlock.nonResourceStats[UnityEngine.Random.Range(0, StatBlock.nonResourceStats.Length)];
                    break;
                case StatTypes.RANDOM_RESOURCE:
                    stat = StatBlock.onlyResourceStats[UnityEngine.Random.Range(0, StatBlock.onlyResourceStats.Length)];
                    break;
            }
        }
        else
        {
            stat = nTemplate.stat;
        }
        statData = nTemplate.statData;
        effectPower = nTemplate.effectPower;
        effectEquation = nTemplate.effectEquation;
        floorValue = nTemplate.floorValue;
        ceilingValue = nTemplate.ceilingValue;
        reverseOnEnd = nTemplate.reverseOnEnd;
        changeSubtypes = nTemplate.changeSubtypes;
        script_effectModifier = nTemplate.script_effectModifier;
        baseAmount = nTemplate.baseAmount;
        modBySpirit = nTemplate.modBySpirit;
        modByDiscipline = nTemplate.modByDiscipline;
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

        ChangeStatEffect eff = compareEff as ChangeStatEffect;

        if (stat != eff.stat) return false;
        if (statData != eff.statData) return false;
        if (effectPower != eff.effectPower) return false;
        if (effectEquation != eff.effectEquation) return false;
        if (reverseOnEnd != eff.reverseOnEnd) return false;
        if (changeSubtypes != eff.changeSubtypes) return false;

        return true;
    }

    public override void ReverseEffect()
    {
        affectedActors.Clear();
        results.Clear();
        List<Actor> actorsToProcess = new List<Actor>();

        GetTargetActorsAndUpdateBuildActorsToProcess();        
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }        

        foreach (Actor act in actorsToProcess)
        {

            Fighter fight = act as Fighter;
            if (fight != null)
            {
                switch (statData)
                {
                    case StatDataTypes.ALL:
                        if (changeSubtypes)
                        {
                            fight.myStats.ChangeStatAndSubtypes(stat, -1 * accumulatedAmount, StatDataTypes.ALL);
                        }
                        else
                        {
                            fight.myStats.ChangeStat(stat, -1 * accumulatedAmount, StatDataTypes.ALL, true);
                        }
                        break;
                    case StatDataTypes.TRUEMAX:
                        if (changeSubtypes)
                        {
                            fight.myStats.ChangeStatAndSubtypes(stat, -1 * accumulatedAmount, StatDataTypes.TRUEMAX);
                        }
                        else
                        {
                            fight.myStats.ChangeStat(stat, -1 * accumulatedAmount, StatDataTypes.TRUEMAX, true);
                        }

                        break;
                    case StatDataTypes.CUR:
                    case StatDataTypes.MAX:

                        // If max HP is boosted, don't kill player when it wears off?
                        if (fight.myStats.GetCurStat(stat) < accumulatedAmount)
                        {
                            fight.myStats.ChangeStat(stat, accumulatedAmount + 1f, StatDataTypes.CUR, false);
                        }

                        if (changeSubtypes)
                        {
                            fight.myStats.ChangeStatAndSubtypes(stat, -1 * accumulatedAmount, statData);
                        }
                        else
                        {
                            fight.myStats.ChangeStat(stat, -1 * accumulatedAmount, statData, true);
                        }

                        if (effectRefName == "blooddebtsbuff") // Hardcoded
                        {
                            fight.myStats.ChangeStat(StatTypes.HEALTH, -0.1f * accumulatedAmount, StatDataTypes.CUR, true);
                            GameMasterScript.heroPCActor.CheckForLimitBreakOnDamageTaken(accumulatedAmount);
                        }

                        break;

                }
                //Debug.Log("Set " + (-1 * accumulatedAmount) + " of " + stat.ToString() + " to " + fight.displayName);
                affectedActors.Add(act);
            }
        }
        accumulatedAmount = 0;
        //Debug.Log("Reversed " + effectRefName + " " + effectName + " and reset accumulated to 0");
    }

    private List<Fighter> petsToConsiderHealing;

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

        actorsToProcess.Clear();
        GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);        
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }        

        bool valid = false;

        EffectResultPayload erp = CheckForPreProcessFunction(actorsToProcess, 0);
        actorsToProcess = erp.actorsToProcess;
        extraWaitTime = erp.waitTime;

        if (EvaluateTriggerCondition(actorsToProcess))
        {
            valid = true;
        }

        if (!valid) return 0.0f;

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPosition, this, originatingActor.GetObject());
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        MethodInfo runScript = null;

        if (!string.IsNullOrEmpty(script_effectModifier))
        {
            runScript = CustomAlgorithms.TryGetMethod(typeof(ChangeStatModifierFunctions), script_effectModifier);
        }


        foreach (Actor act in actorsToProcess)
        {
            if (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO)
            {
                TextDensity logMessageDensityState = TextDensity.NORMAL;
                if (act.GetActorType() != ActorTypes.HERO && stat != StatTypes.HEALTH)
                {
                    logMessageDensityState = TextDensity.VERBOSE;
                }

                float calcSpiritPower = origFighter.cachedBattleData.spiritPower;
                if (origFighter == GameMasterScript.heroPCActor && origFighter.myStats.CheckHasStatusName("status_kineticmagic") && origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.51f)
                {
                    calcSpiritPower *= 1.2f;
                }

                Fighter fight = act as Fighter;

                bool doCalculations = true;

                if (stat == StatTypes.ALLRESOURCE)
                {
                    if (effectRefName == "gamblerheal")
                    {
                        // Just heal everything.
                        fight.myStats.HealToFull();
                        doCalculations = false;

                        Monster corralPet = GameMasterScript.heroPCActor.GetMonsterPet();
                        if (corralPet != null && !corralPet.isInDeadQueue)
                        {
                            float pcToUse = corralPet.GetHealPercentageAsPlayerPet();
                            corralPet.myStats.ChangeStat(StatTypes.HEALTH, corralPet.myStats.GetMaxStat(StatTypes.HEALTH) * pcToUse, StatDataTypes.CUR, true);
                        }
                    }
                }

                float value = 0f;

                bool evaluateExpression = true;

                bool isPowerup = effectRefName.Contains("powerup");

                if (doCalculations)
                {
                    float maxStat = fight.myStats.GetStat(stat, StatDataTypes.MAX);

                    if (anyDamageEquationVars)
                    {
                        // Skip expression parsing, this is much faster.
                        evaluateExpression = false;
                        value = baseAmount;
                        value += damageEquationVars[(int)EDamageEquationVars.MAX_STAT] * maxStat;
                        value += damageEquationVars[(int)EDamageEquationVars.CUR_STAT] * fight.myStats.GetCurStat(stat);
                        value += damageEquationVars[(int)EDamageEquationVars.DEF_MAX_HP] * fight.myStats.GetMaxStat(stat);
                        value += damageEquationVars[(int)EDamageEquationVars.DEF_CUR_HP] * fight.myStats.GetCurStat(stat);
                        value += damageEquationVars[(int)EDamageEquationVars.CUR_HP] * fight.myStats.GetCurStat(StatTypes.HEALTH);
                        value += damageEquationVars[(int)EDamageEquationVars.ORIG_CUR_HP] * origFighter.myStats.GetCurStat(StatTypes.HEALTH);
                        value += damageEquationVars[(int)EDamageEquationVars.ATK_SPIRIT_POWER] * fight.cachedBattleData.spiritPower;
                        value += effectPower;
                        value += damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL] * fight.myStats.GetLevel();
                        if (damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
                        {
                            value += UnityEngine.Random.Range(damageEquationVars[(int)EDamageEquationVars.RND_MIN], damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
                        }

                        if (modBySpirit)
                        {
                            value += (value * fight.myStats.GetCurStatAsPercent(StatTypes.SPIRIT));
                            if (isPowerup)
                            {
                                value *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.POWERUP_HEALING);
                                if (GameStartData.NewGamePlus >= 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
                                {
                                    value *= GameStartData.NGPLUSPLUS_POWERUPHEAL_MODIFIER;
                                }
                                TutorialManagerScript.powerUpFoundOnTurn = GameMasterScript.turnNumber;
                            }
                                
                        }
                        if (modByDiscipline)
                        {
                            value += (value * fight.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE));
                        }

                        if (fight.GetActorType() == ActorTypes.MONSTER && GameStartData.NewGamePlus >= 1 && fight.actorfaction == Faction.ENEMY && stat == StatTypes.HEALTH)
                        {
                            value *= (1f - (0.16f * (float)GameStartData.NewGamePlus));
                        }
                    }
                }
                
                if (doCalculations)
                {
                    if (evaluateExpression)
                    {
                        float maxStat = fight.myStats.GetStat(stat, StatDataTypes.MAX);

                        string localEquation = string.Copy(effectEquation);
                        localEquation = localEquation.Replace("$AttackerWeaponPower", origFighter.cachedBattleData.physicalWeaponDamage + (origFighter.cachedBattleData.physicalWeaponOffhandDamage * .75f).ToString());
                        //localEquation = localEquation.Replace("$EffectPower", effectPower.ToString());
                        localEquation = localEquation.Replace("$AtkSpiritPower", calcSpiritPower.ToString());
                        localEquation = localEquation.Replace("$TargetLevel", fight.myStats.GetLevel().ToString());
                        localEquation = localEquation.Replace("$MaxStat", maxStat.ToString());
                        localEquation = localEquation.Replace("$StrengthMod", (1.0f + fight.myStats.GetCurStatAsPercent(StatTypes.STRENGTH)).ToString());
                        localEquation = localEquation.Replace("$SpiritMod", (1.0f + fight.myStats.GetCurStatAsPercent(StatTypes.SPIRIT)).ToString());
                        localEquation = localEquation.Replace("$DisciplineMod", (1.0f + fight.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE)).ToString());
                        localEquation = localEquation.Replace("$TargetMaxHealth", fight.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX).ToString());

                        value = (float)fParser.Evaluate(localEquation);
                    }

                    if (originatingActor.GetActorType() == ActorTypes.HERO && parentAbility.abilityFlags[(int)AbilityFlags.POTION] && GameMasterScript.heroPCActor.ReadActorData("schematist_infuse") == 1)
                    {
                        value *= 1.25f;
                    }

                    // This checks the TARGET of the effect... as well as the user of the effect
                    value = EffectScript.CheckForEffectValueModifier(effectRefName, fight, value);
                    value = EffectScript.CheckForEffectValueModifier(effectRefName, origFighter, value);

                    if (value < floorValue)
                    {
                        value = floorValue;
                    }
                    if (value > ceilingValue)
                    {
                        value = ceilingValue;
                    }

                    //Debug.Log(stat + " changed by " + value);


                    if (runScript != null)
                    {
                        object[] paramList = new object[3];
                        paramList[0] = this;
                        paramList[1] = act as Fighter;
                        paramList[2] = value;
                        object returnObj = runScript.Invoke(null, paramList);
                        if (returnObj == null)
                        {
                            Debug.Log("No object for " + script_processActorsPreEffect);
                        }
                        erp = returnObj as EffectResultPayload;
                        value = erp.value;
                        if (erp.cancel)
                        {
                            continue;
                        }
                    }

                    if (effectRefName == "stampowerupheal" && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_spellshaperpowerup"))
                    {
                        float energyHeal = value * 0.15f;
                        fight.myStats.ChangeStatAndSubtypes(StatTypes.ENERGY, energyHeal, StatDataTypes.CUR);

                        ChangeCoreStatPackage ldp = GameLogDataPackages.GetChangeCoreStatPackage();
                        ldp.gameActor = fight;
                        ldp.abilityUser = originatingActor.displayName;

                        ldp.effectSource = effectName;
                        if (string.IsNullOrEmpty(effectName) && parentAbility != null)
                        {
                            ldp.effectSource = parentAbility.abilityName;
                        }

                        ldp.statChanges[(int)StatTypes.ENERGY] = energyHeal;
                        GameLogScript.CombatEventWrite(ldp, logMessageDensityState);
                    }

                    // #todo - MAKE THIS LANGUAGE INDEPENDENT
                    if (isPowerup && !effectRefName.Contains("herb"))
                    {
                        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("spiritcollector"))
                        {
                            GameMasterScript.heroPCActor.myStats.AddStatusByRef("spiritcollected", GameMasterScript.heroPCActor, 1);
                        }

                        int giltouchedQty = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("giltouched");
                        float chance = giltouchedQty * 0.15f;
                        float roll = UnityEngine.Random.Range(0, 1f);
                        if (roll < chance)
                        {
                            int goldFound = UnityEngine.Random.Range(4, 12) * GameMasterScript.heroPCActor.myStats.GetLevel();
                            goldFound = GameMasterScript.heroPCActor.ChangeMoney(goldFound);
                            BattleTextManager.NewText(goldFound + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD), GameMasterScript.heroPCActor.GetObject(), Color.yellow, 0.1f);
                            StringManager.SetTag(0, goldFound.ToString());
                            GameLogScript.LogWriteStringRef("log_findmoney");
                        }
                    }

                    if (stat == StatTypes.HEALTH)
                    {                       
                        if (statData == StatDataTypes.CUR)
                        {
                            if (CombatManagerScript.bufferedCombatData == null)
                            {
                                CombatManagerScript.bufferedCombatData = new CombatDataPack();
                            }
                            CombatManagerScript.bufferedCombatData.healValue = value;
                            fight.myStats.CheckRunAllStatuses(StatusTrigger.ONHEAL);
                            value = CombatManagerScript.bufferedCombatData.healValue;
                            value *= fight.cachedBattleData.healModifierValue;
                        }                        
                    }                    

                    if (fight.GetActorType() == ActorTypes.HERO)
                    {
                        if (effectTags[(int)EffectTags.FOODHEAL])
                        {
                            if (fight.myStats.CheckHasStatusName("status_foodlover"))
                            {
                                value *= 1.18f;
                            }
                            if (fight.myStats.CheckHasStatusName("status_mmgluttony"))
                            {
                                value *= 1.15f;
                            }
                            if (fight.myStats.CheckHasStatusName("status_mmgluttony2"))
                            {
                                value *= 1.25f;
                            }
                        }
                        else if (effectRefName == "flaskheal")
                        {
                            if (fight.myStats.CheckHasStatusName("status_thirstquencher"))
                            {
                                value *= 1.11f;
                            }
                            if (fight.ReadActorData("infuse2") == GameMasterScript.FLASK_HEAL_MORE)
                            {
                                value += GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.012f;
                            }
                            value += (GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("xp2_hydrating") * value * 0.15f);
                        }
                    }


                    if (GameMasterScript.gameLoadSequenceCompleted)
                    {
                        if (MapMasterScript.activeMap.IsItemWorld() && (stat == StatTypes.HEALTH || stat == StatTypes.ENERGY || stat == StatTypes.STAMINA))
                        {
                            int aura = MapMasterScript.GetItemWorldAura(fight.GetPos());
                            if (aura == (int)ItemWorldAuras.DOUBLEHEALING)
                            {
                                value *= 2f;
                            }
                        }
                    }
                }

                // END SPECIAL CASES

                if (GameMasterScript.actualGameStarted)
                {
                    CombatManagerScript.ProcessGenericEffect(origFighter, fight, this, false, perTargetAnim);
                }

                accumulatedAmount += value;

                if (doCalculations)
                {
                    if (stat != StatTypes.CHARGETIME)
                    {
                        if (changeSubtypes)
                        {
                            if (!reverseOnEnd)
                            {
                                fight.myStats.ChangeStatAndSubtypes(stat, value, statData);
                            }
                            else
                            {
                                if (statData == StatDataTypes.MAX)
                                {
                                    fight.myStats.ChangeStat(stat, value, StatDataTypes.CUR, false);
                                    fight.myStats.ChangeStat(stat, value, StatDataTypes.MAX, false);
                                }
                                else if (statData == StatDataTypes.CUR)
                                {
                                    fight.myStats.ChangeStat(stat, value, StatDataTypes.CUR, false);
                                }
                            }


                        }
                        else
                        {
                            if (!reverseOnEnd)
                            {
                                fight.myStats.ChangeStat(stat, value, statData, true);
                            }
                            else
                            {
                                fight.myStats.ChangeStat(stat, value, statData, false);
                            }

                        }

                        //Debug.Log("Changing stat " + stat + " by " + value + " " + statData);

                        string addPercent = "";
                        if (stat == StatTypes.ACCURACY)
                        {
                            addPercent = StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
                        }

                        if (value > 0)
                        {
                            if (GameMasterScript.actualGameStarted && !silent && !(stat == StatTypes.HEALTH && effectRefName == "monsterspirit"))
                            {
                                ChangeCoreStatPackage ldp = GameLogDataPackages.GetChangeCoreStatPackage();
                                ldp.abilityUser = originatingActor.displayName;
                                ldp.effectSource = effectName;
                                if (string.IsNullOrEmpty(effectName) && parentAbility != null)
                                {
                                    ldp.effectSource = parentAbility.abilityName;
                                }
                                ldp.gameActor = fight;
                                ldp.statChanges[(int)stat] = value;
                                if (!string.IsNullOrEmpty(addPercent))
                                {
                                    ldp.percentBased = true;
                                }
                                GameLogScript.CombatEventWrite(ldp, logMessageDensityState);

                                if (effectRefName == "stampowerupheal" || effectRefName == "herb_stampowerupheal")
                                {
                                    string heal = (int)value + " " + StringManager.GetString("stat_stamina") + "!";
                                    BattleTextManager.NewText(heal, act.GetObject(), Color.green, 0.0f, 1f);
                                    TutorialManagerScript.powerUpFoundOnTurn = GameMasterScript.turnNumber;
                                }
                                else if (effectRefName == "energypowerupheal" || effectRefName == "herb_energypowerupheal")
                                {
                                    string heal = (int)value + " " + StringManager.GetString("stat_energy") + "!";
                                    BattleTextManager.NewText(heal, act.GetObject(), Color.cyan, 0.0f, 1f);
                                    TutorialManagerScript.powerUpFoundOnTurn = GameMasterScript.turnNumber;
                                }

                                TryBattleText(act, true);
                            }

                            // For restoring HEALTH, make an exception and create battle text.
                            if (stat == StatTypes.HEALTH)
                            {
                                if (!silent)
                                {
                                    BattleTextManager.NewDamageText((int)value, true, Color.green, fight.GetObject(), 0.5f, 1.25f);
                                }

                                // Player's monster pet inherits some % of healing.
                                if (act == GameMasterScript.heroPCActor && statData == StatDataTypes.CUR)
                                {
                                    HeroPC hpc = act as HeroPC;
                                    Actor findPet = null;
                                    if (petsToConsiderHealing == null) petsToConsiderHealing = new List<Fighter>();
                                    petsToConsiderHealing.Clear();                                    
                                    if (hpc.GetMonsterPetID() > 0)
                                    {
                                        findPet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(hpc.GetMonsterPetID());
                                        if (findPet != null)
                                        {
                                            petsToConsiderHealing.Add(findPet as Fighter);
                                        }
                                    }

                                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_floramancer_tier2_pethealing"))
                                    {
                                        foreach(Actor summonedActor in GameMasterScript.heroPCActor.summonedActors)
                                        {
                                            if (summonedActor.GetActorType() == ActorTypes.MONSTER)
                                            {
                                                Monster mn = summonedActor as Monster;
                                                if (findPet != mn)
                                                {
                                                    petsToConsiderHealing.Add(mn);
                                                }
                                            }
                                        }
                                    }
                                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_husynemblem_tier1_runic"))
                                    {
                                        Actor runic = GameMasterScript.heroPCActor.GetSummonByRef("mon_runiccrystal");
                                        if (runic != null)
                                        {
                                            petsToConsiderHealing.Add(runic as Fighter);
                                        }
                                    }
                                    
                                    foreach(Fighter healFighter in petsToConsiderHealing)
                                    {
                                        Monster healMon = healFighter as Monster;
                                        float percentageToUse = healMon.GetHealPercentageAsPlayerPet();
                                        float adjValue = value * percentageToUse;
                                        healFighter.myStats.ChangeStat(StatTypes.HEALTH, value * GameMasterScript.PLAYER_PET_HEAL_PERCENTAGE, StatDataTypes.CUR, true);

                                        ChangeCoreStatPackage ldp = GameLogDataPackages.GetChangeCoreStatPackage();
                                        ldp.abilityUser = originatingActor.displayName;
                                        ldp.effectSource = effectName;
                                        if (string.IsNullOrEmpty(effectName) && parentAbility != null)
                                        {
                                            ldp.effectSource = parentAbility.abilityName;
                                        }
                                        ldp.gameActor = healFighter;
                                        ldp.statChanges[(int)stat] = adjValue;
                                        if (!string.IsNullOrEmpty(addPercent))
                                        {
                                            ldp.percentBased = true;
                                        }
                                        GameLogScript.CombatEventWrite(ldp);

                                        UIManagerScript.UpdatePetInfo();
                                        BattleTextManager.NewDamageText((int)adjValue, true, Color.green, healFighter.GetObject(), 0.0f, 1f);
                                    }

                                }
                            }
                        }
                        else
                        {
                            if (GameMasterScript.actualGameStarted && !silent)
                            {
                                TryBattleText(act, false);

                                ChangeCoreStatPackage ldp = GameLogDataPackages.GetChangeCoreStatPackage();
                                ldp.abilityUser = originatingActor.displayName;
                                ldp.effectSource = effectName;
                                if (string.IsNullOrEmpty(effectName) && parentAbility != null)
                                {
                                    ldp.effectSource = parentAbility.abilityName;
                                }
                                ldp.gameActor = act;
                                ldp.statChanges[(int)stat] = value;
                                if (!string.IsNullOrEmpty(addPercent))
                                {
                                    ldp.percentBased = true;
                                }
                                GameLogScript.CombatEventWrite(ldp, logMessageDensityState);
                            }

                            // For damaging HEALTH, make an exception and create battle text.
                            if (stat == StatTypes.HEALTH)
                            {
                                //BattleTextManager.NewText(((int)value).ToString(), act.GetObject(),  BattleTextManager.genericDamageColor, 0.0f);
                                BattleTextManager.NewDamageText((int)value, false, Color.white, act.GetObject(), 0.0f, 1f);
                            }
                        }
                    }
                    else
                    {
                        // *** HARDCODE special case for current CHARGETIME
                        fight.ChangeCT(value);
                        if (value < 0)
                        {
                            if (fight.turnsSinceLastSlow >= 5 || fight.lastSlowedEffect != this)
                            {
                                StringManager.SetTag(0, act.displayName);
                                GameLogScript.GameLogWrite(StringManager.GetString("log_target_slowed"), act, TextDensity.VERBOSE);
                                fight.lastSlowedEffect = this;
                                TryBattleText(fight, false);
                            }                            
                        }
                        if (fight == GameMasterScript.heroPCActor)
                        {
                            UIManagerScript.RefreshPlayerCT(false);
                        }
                    }
                }
                else
                {
                    StringManager.SetTag(0, act.displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("log_healtofull"), act);
                    BattleTextManager.NewText(StringManager.GetString("misc_fullheal"), act.GetObject(), Color.green, 1.5f);
                }

                affectedActors.Add(act);
            }
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

        if (playAnimation == false)
        {
            returnVal = 0.0f;
        }


        if (PlayerOptions.animSpeedScale != 0)
        {
            returnVal *= PlayerOptions.animSpeedScale;
        }


        return returnVal;
    }
}