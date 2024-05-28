using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;

public class AddStatusEffect : EffectScript
{
    public float baseDuration;
    public string statusRef;
    public string script_preRunConditional;
    public string script_extraPerActorFunction;

    public string localRef; // only called during function, don't copy this.
    public float localDuration; // only called during function, don't copy this in core data

    public const float CHANCE_SOULKEEPER_EMBLEM_REFLECTDEBUFF = 0.2f;
    
    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        AddStatusEffect nTemplate = template as AddStatusEffect;
        tActorType = nTemplate.tActorType;
        statusRef = nTemplate.statusRef;
        baseDuration = nTemplate.baseDuration;
        script_preRunConditional = nTemplate.script_preRunConditional;
        script_extraPerActorFunction = nTemplate.script_extraPerActorFunction;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        AddStatusEffect eff = compareEff as AddStatusEffect;

        if (statusRef != eff.statusRef) return false;
        if (baseDuration != eff.baseDuration) return false;

        return true;
    }

    public AddStatusEffect()
    {
        baseDuration = 0;
    }

    public override void ReverseEffect()
    {
        if (!VerifyOriginatingActorIsFighterAndFix())
        {
            originatingActor = GameMasterScript.heroPCActor;
        }

        Fighter origFighter = originatingActor as Fighter;
        StatBlock origStats = origFighter.myStats;
        List<Actor> actorsToProcess = new List<Actor>();

        GetTargetActorsAndUpdateBuildActorsToProcess();
        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }

        StatusEffect seToRemove = null;

        foreach (Actor act in actorsToProcess)
        {
            if ((act.GetActorType() == ActorTypes.MONSTER) || (act.GetActorType() == ActorTypes.HERO))
            {
                Fighter fight = (Fighter)act as Fighter;
                foreach (StatusEffect eff in fight.myStats.GetAllStatuses())
                {
                    if (eff.refName == statusRef)
                    {
                        // remove this status;
                        seToRemove = eff;
                        break;
                    }
                }
                if (seToRemove != null)
                {
                    //Debug.Log("Removing from " + fight.actorRefName + ", status called " + seToRemove.refName + " based on effect " + statusRef);
                    fight.myStats.RemoveStatus(seToRemove, false);
                    if (fight == GameMasterScript.heroPCActor)
                    {
                        GameMasterScript.heroPCActor.TryRefreshStatuses();                        
                    }
                }

            }
        }
    }

    public override float DoEffect(int indexOfEffect = 0)
    {

        affectedActors.Clear();
        results.Clear();
        List<Actor> actorsToProcess = new List<Actor>();

        Fighter origFighter = originatingActor as Fighter;
        if (!VerifyOriginatingActorIsFighterAndFix())
        {
            return 0f;
        }
        origFighter = originatingActor as Fighter; // if originatingActor got updated, origFighter might not have!
        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        //Debug.Log("Running " + effectName + " " + effectRefName);

        if (!string.IsNullOrEmpty(script_preRunConditional))
        {
            Func<AddStatusEffect, bool> preRunDelegate;
            if (AddStatusCustomFunctions.dictPreStatusDelegates.TryGetValue(script_preRunConditional, out preRunDelegate))
            {
                preRunDelegate(this);
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(AddStatusCustomFunctions), script_preRunConditional);
                object[] paramList = new object[1];
                paramList[0] = this;
                object returnObj = runscript.Invoke(null, paramList);
                // #todo - Shouldn't this evaluate boolean true/false?
            }
        }


        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        bool valid = false;

        // Flesh this out more.

        try
        {
            actorsToProcess.Clear();
            GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);            
            foreach(Actor a in buildActorsToProcess)
            {
                actorsToProcess.Add(a);
            }            
        }
        catch(Exception e)
        {
            Debug.LogError("Failed executing effect " + effectRefName + " " + effectName + " due to " + e);
            return 0f;
        }
        

        if (EvaluateTriggerCondition(actorsToProcess))
        {
            valid = true;
        }

        if (!valid)
        {
            return 0.0f;
        }

        bool perTargetAnim = true;

        if (parentAbility != null)
        {
            perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);
        }

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            Vector2 usePosition = centerPosition;

            if (usePosition == Vector2.zero)
            {
                if (VerifySelfActorIsFighterAndFix()) // 412019 - If self actor is null, get outta here
                {
                    return 0f;
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

            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), usePosition, this, originatingActor.GetObject());
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        MethodInfo extraFunction = null;
        Func<AddStatusEffect, Actor, bool> extraDelegate = null;
        bool hasExtraFunction = false;
        bool hasDelegate = false;

        if (!string.IsNullOrEmpty(script_extraPerActorFunction))
        {
            if (AddStatusCustomFunctions.dictPerActorDelegates.TryGetValue(script_extraPerActorFunction, out extraDelegate))
            {
                hasDelegate = true;
            }
            else
            {
                extraFunction = CustomAlgorithms.TryGetMethod(typeof(AddStatusCustomFunctions), script_extraPerActorFunction);
                hasExtraFunction = true;
            }            
        }
        Actor act = null;

        for (int i = 0; i < actorsToProcess.Count; i++)
        {
            localRef = statusRef;
            localDuration = baseDuration;

            bool skipThisActor = false;

            if (hasExtraFunction)
            {
                object[] paramList = new object[2];
                paramList[0] = this;
                paramList[1] = actorsToProcess[i];
                skipThisActor = (bool)(extraFunction.Invoke(null, paramList));                
            }
            else if (hasDelegate)
            {
                skipThisActor = extraDelegate(this, actorsToProcess[i]);
            }

            act = actorsToProcess[i];

            if (skipThisActor) continue;

            if (act.GetActorType() == ActorTypes.MONSTER) // Special case for mud resistance.
            {
                if (effectRefName == "mudtileroot")
                {
                    Monster m = act as Monster;
                    if (m.CheckAttribute(MonsterAttributes.LOVESMUD) > 0 || m.CheckAttribute(MonsterAttributes.FLYING) > 0)
                    {
                        continue;
                    }
                    if (m.isChampion && UnityEngine.Random.Range(0, 1f) <= 0.5f) continue;
                }
            }

            if (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO)
            {
                Fighter fight = act as Fighter;

                if (localRef == "runic_charge")
                {
                    if (!fight.myStats.CheckHasStatusName("runic_crystal2_buff"))
                    {
                        continue;
                    }
                }


                StatusEffect nStatus = new StatusEffect();
                StatusEffect template = GameMasterScript.FindStatusTemplateByName(localRef);
                if (template == null)
                {
                    return 0f;
                }

                // Special case: Soulkeeper's reflect debuff
                if (originatingActor.actorfaction != Faction.PLAYER && fight.GetActorType() == ActorTypes.HERO && !template.isPositive)
                {
                    float roll = UnityEngine.Random.Range(0, 1f);
                    if (roll <= CHANCE_SOULKEEPER_EMBLEM_REFLECTDEBUFF 
                        && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_soulkeeperemblem_tier1_reflect") 
                        && originatingActor.IsFighter())
                    {
                        StringManager.SetTag(0, origFighter.displayName);
                        StringManager.SetTag(1, GameMasterScript.heroPCActor.displayName);
                        GameLogScript.LogWriteStringRef("log_status_reflected");
                        BattleTextManager.NewText(StringManager.GetExcitedString("misc_reflected"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.8f);
                        fight = origFighter; // Change the target
                        act = origFighter;
                    }
                }


                if (fight.CheckForStatusImmunity(template))
                {
                    continue;
                }


                // #todo - Add paralyze tag to skills

                if (act.GetActorType() == ActorTypes.HERO && (localRef == "status_paralyzed" || localRef == "status_bigfreeze"))
                {
                    BattleTextManager.NewText(StringManager.GetString("misc_disarmed"), act.GetObject(), Color.red, 0.0f);
                }

                if (originatingActor.GetActorType() == ActorTypes.HERO && effectRefName == "add_enemy_verse_challenges")
                {
                    originatingActor.SetActorData("verse_challenges_target", fight.actorUniqueID);
                }

                if (originatingActor.GetActorType() == ActorTypes.HERO && effectRefName == "verse_challenges_addbuff")
                {
                    if (CombatManagerScript.bufferedCombatData == null || CombatManagerScript.bufferedCombatData.attacker == null)
                    {
                        return 0f;
                    }
                    if (CombatManagerScript.bufferedCombatData.attacker.actorUniqueID != originatingActor.ReadActorData("verse_challenges_target"))
                    {
                        return 0f;
                    }
                }

                if (fight.myStats.CheckStatusQuantity(localRef) >= template.maxStacks)
                {
                    continue;
                }

                for (int f = 0; f < (int)ActorFlags.COUNT; f++)
                {
                    if (switchFlags[f])
                    {
                        fight.SetFlag((ActorFlags)f, true);
                    }
                }

                if (localRef == "crabbleed") // Hardcoded - identify if a crab is trying to pince
                {
                    fight.AddAnchor(originatingActor);
                    originatingActor.anchor = fight;
                    originatingActor.anchorID = fight.actorUniqueID;
                    originatingActor.anchorRange = 0;
                }

                if (localRef == "status_tracked")
                {
                    fight.SetFlag(ActorFlags.TRACKED, true);
                }

                // TODO - Give monsters specific anti-charm status...
                if (fight.GetActorType() == ActorTypes.MONSTER && localRef == "status_charmed")
                {
                    // Chance to add at all.
                    Monster monny = fight as Monster;
                    if (monny.isChampion || monny.isBoss)
                    {
                        StringManager.SetTag(0, monny.displayName);
                        GameLogScript.LogWriteStringRef("log_resist_charm");
                        continue;
                    }
                    float chanceToAdd = 1.0f;
                    if (monny.challengeValue > 1.4f)
                    {
                        chanceToAdd = 0.8f;
                    }
                    if (monny.challengeValue > 1.6f)
                    {
                        chanceToAdd = 0.6f;
                    }
                    if (UnityEngine.Random.Range(0, 1f) > chanceToAdd)
                    {
                        StringManager.SetTag(0, monny.displayName);
                        GameLogScript.LogWriteStringRef("log_resist_charm");
                        continue;
                    }

                }

                nStatus.CopyStatusFromTemplate(template);
                
                if (origFighter.GetActorType() == ActorTypes.HERO && fight.GetActorType() == ActorTypes.HERO &&
                    nStatus.isPositive && parentAbility != null && destructibleOwnerOfEffect == null)
                {
                    // track where hero statuses are coming from
                    nStatus.addedByAbilityRef = parentAbility.refName;
                }

                ProcessDirectionalSpecialCases(nStatus, act);

                if (nStatus.direction == Directions.MONSTERTARGETDIR)
                {
                    ProcessMonsterDirectionalSpecialCases(nStatus);
                }

                if (fight.actorUniqueID != origFighter.actorUniqueID && fight.actorfaction != origFighter.actorfaction 
                    && origFighter.myStats.CheckHasStatusName("status_immunology"))
                {
                    if (!nStatus.statusFlags[(int)StatusFlags.STUN])
                    {
                        localDuration += 1;
                    }
                }

                if (effectRefName == "regenflaskstatus")
                {
                    if (GameMasterScript.heroPCActor.ReadActorData("flask_apple_infuse") == 1)
                    {
                        localDuration++;
                    }
                }

                if (nStatus.statusFlags[(int)StatusFlags.THANESONG])
                {
                    if (origFighter.myStats.CheckHasStatusName("thanebonus2"))
                    {
                        localDuration += 3;
                    }
                }

                bool isFullStatusForHero = false;

                if (fight.IsHero() && nStatus.refName == "status_foodfull") isFullStatusForHero = true;

                if (GameStartData.CheckGameModifier(GameModifiers.FAST_FULLNESS) && isFullStatusForHero)
                {
                    localDuration /= 2;
                    if (localDuration <= 2) localDuration = 2;
                }

                nStatus.curDuration = localDuration;
                nStatus.maxDuration = localDuration;

                ProcessHardcodedSpecialCases1(localRef, origFighter, nStatus);

                CheckForBleedAndBrigandMods(nStatus, fight, origFighter);

                if (CheckForFullModifiersAndSkipIfAvoidingFull(nStatus, isFullStatusForHero, fight))
                {
                    continue;
                }

                bool statusPreviouslyExisted = fight.myStats.CheckHasStatusName(nStatus.refName);

                fight.myStats.AddStatus(nStatus, originatingActor); // NOT hardcoded

                //if (originatingActor.GetActorType() == ActorTypes.HERO && fight.GetActorType() == ActorTypes.HERO)
                if (fight.GetActorType() == ActorTypes.HERO)
                {
                    nStatus.addedByActorID = originatingActor.actorUniqueID;
                }

                if (parentAbility != null && parentAbility.abilityFlags[(int)AbilityFlags.THANESONG])
                {
                    ProcessThaneSongBonus();
                }

                if (localRef == "runic_charge")
                {
                    fight.EnableWrathBarIfNeeded();
                    fight.wrathBarScript.UpdateWrathCount(fight.myStats.CheckStatusQuantity("runic_charge"));
                }

                // END HARDCODED STUFF

                WriteToLogIfNecessary(nStatus, fight, statusPreviouslyExisted, act);

                CombatManagerScript.ProcessGenericEffect(origFighter, fight, this, false, perTargetAnim);

                DoAnimationsIfNecessary(fight, nStatus);

                DoBleedConditionalCleanup(origFighter, fight, nStatus);

                affectedActors.Add(act);
                if (nStatus.isPositive)
                {
                    TryBattleText(act, true);
                }
                else
                {
                    TryBattleText(act, false);
                }

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nStatus">The newly added status</param>
    /// <param name="fight">The target</param>
    /// <param name="origFighter">The originating fighter</param>
    public void CheckForBleedAndBrigandMods(StatusEffect nStatus, Fighter fight, Fighter origFighter)
    {
        if (nStatus.listEffectScripts.Count > 0)
        {
            if (fight == GameMasterScript.heroPCActor && nStatus.statusFlags[(int)StatusFlags.BLEED])
            {
                if (fight.myStats.CheckHasStatusName("status_mmclotting"))
                {
                    nStatus.curDuration--;
                    nStatus.maxDuration--;
                }
            }
            else if (origFighter == GameMasterScript.heroPCActor && nStatus.statusFlags[(int)StatusFlags.BLEED])
            {
                if (origFighter.myAbilities.HasAbilityRef("skill_brigandbonus2"))
                {
                    nStatus.curDuration += 2;
                    nStatus.maxDuration += 2;
                }
                if (origFighter.myStats.CheckHasStatusName("envenom") && nStatus.refName != "shadowbleed")
                {
                    nStatus.statusFlags[(int)StatusFlags.POISON] = true;
                    nStatus.ConvertDamage(DamageTypes.POISON);
                }
            }
        }
    }

    public void ProcessThaneSongBonus()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("buffer_thanesong_level") > 1)
        {
            for (int x = 1; x < GameMasterScript.heroPCActor.ReadActorData("buffer_thanesong_level"); x++)
            {
                GameMasterScript.heroPCActor.IncreaseSongLevel();
            }

            GameMasterScript.heroPCActor.RemoveActorData("buffer_thanesong_level");
        }
    }

    public void ProcessMonsterDirectionalSpecialCases(StatusEffect nStatus)
    {
        Monster mn = originatingActor as Monster;

        // Hardcoded so that the second shield is opposite the first.
        if (nStatus.refName == "status_turtleshield" && mn.myStats.CheckHasStatusName("status_turtleshield"))
        {
            if (parentAbility.refName == "skill_turtleshield" || parentAbility.refName == "skill_turtle2")
            {
                foreach (StatusEffect se in mn.myStats.GetAllStatuses())
                {
                    if (se.refName == "status_turtleshield")
                    {
                        Directions existing = se.direction;
                        nStatus.direction = MapMasterScript.oppositeDirections[(int)existing];
                        break;
                    }
                }
                bool openDirection = false;
                int tries = 0;
                while (!openDirection)
                {
                    if (tries > 8) break;
                    openDirection = true;
                    tries++;
                    foreach (StatusEffect se in mn.myStats.GetAllStatuses())
                    {
                        if (se.refName == "status_turtleshield")
                        {
                            if (nStatus.direction == se.direction)
                            {
                                int cDir = (int)nStatus.direction;
                                cDir++;
                                if (cDir >= 8) cDir = 0;
                                nStatus.direction = (Directions)cDir;
                                openDirection = false;
                                break;
                            }
                        }
                    }
                }

            }
            else
            {
                float baseAngle = 0.0f;
                foreach (StatusEffect se in mn.myStats.GetAllStatuses())
                {
                    if (se.refName == "status_turtleshield")
                    {
                        baseAngle = MapMasterScript.GetAngleFromDirection(se.direction);
                        break;
                    }
                }
                int number = mn.myStats.CheckStatusQuantity("status_turtleshield");
                float addAngle = number * 90f;
                baseAngle += addAngle;
                if (baseAngle > 180f)
                {
                    baseAngle -= 360f;
                }
                nStatus.direction = MapMasterScript.GetDirectionFromAngle(baseAngle);
            }
        }


        else
        {
            if (mn.myTarget != null)
            {
                nStatus.direction = MapMasterScript.allDirections[(int)CombatManagerScript.GetDirection(originatingActor, mn.myTarget)];
            }
            else
            {
                nStatus.direction = MapMasterScript.cardinalDirections[UnityEngine.Random.Range(0, MapMasterScript.cardinalDirections.Length)];
            }
        }
    }

    public void ProcessDirectionalSpecialCases(StatusEffect nStatus, Actor act)
    {
        // If direction is "RANDOMALL" or "RANDOMCARDINAL" for status, pick a random direction of course.

        if (nStatus.direction == Directions.RANDOMCARDINAL)
        {
            nStatus.direction = MapMasterScript.cardinalDirections[UnityEngine.Random.Range(0, MapMasterScript.cardinalDirections.Length)];
        }
        if (nStatus.direction == Directions.RANDOMALL)
        {
            int randomDir = UnityEngine.Random.Range(0, 8);
            Vector2 check = act.GetPos() + MapMasterScript.xDirections[randomDir];
            MapTileData mtd = MapMasterScript.GetTile(check);
            bool[] tried = new bool[8];
            while (mtd.tileType == TileTypes.WALL)
            {
                randomDir = UnityEngine.Random.Range(0, 8);
                bool allTried = true;
                for (int x = 0; x < tried.Length; x++)
                {
                    if (!tried[x])
                    {
                        allTried = false;
                        break;
                    }
                }
                if (allTried) break;
                check = act.GetPos() + MapMasterScript.xDirections[randomDir];
                mtd = MapMasterScript.GetTile(check);
                tried[randomDir] = true;
            }
            nStatus.direction = MapMasterScript.allDirections[randomDir];
        }
        if (nStatus.direction == Directions.ATTACKERDIR)
        {
            nStatus.direction = MapMasterScript.oppositeDirections[(int)CombatManagerScript.GetDirection(originatingActor, act)];
        }
        else if (nStatus.direction == Directions.COMBATATTACKERDIR)
        {
            if (CombatManagerScript.bufferedCombatData != null)
            {
                nStatus.direction = MapMasterScript.oppositeDirections[(int)CombatManagerScript.bufferedCombatData.attackDirection];
            }

        }
    }

    public void ProcessHardcodedSpecialCases1(string localRef, Fighter origFighter, StatusEffect nStatus)
    {
        if (localRef == "highlandattack" && effectRefName == "addhighlandchargebuff")
        {
            int songLevel = GameMasterScript.heroPCActor.myStats.CountStatusesByFlag(StatusFlags.THANESONG);

            if (songLevel >= 2)
            {
                GameMasterScript.heroPCActor.myStats.AddStatusByRef("highlandattack", GameMasterScript.heroPCActor, 10);
            }
            if (songLevel >= 3)
            {
                GameMasterScript.heroPCActor.myStats.AddStatusByRef("highlandattack", GameMasterScript.heroPCActor, 10);
            }
        }

        //Debug.Log(nStatus.refName + " " + nStatus.direction);

        if (localRef == "status_staticfield")
        {
            Actor crystal = origFighter.GetSummonByRef("mon_runiccrystal");
            if (crystal != null)
            {
                Monster cm = crystal as Monster;
                cm.myStats.AddStatusByRef("status_staticfield", origFighter, (int)nStatus.curDuration);
            }
        }

        if (nStatus.refName == "swordmastery1_parrybuff")
        {
            AttackReactionEffect are = nStatus.listEffectScripts[0] as AttackReactionEffect;
            are.alterParryFlat += (EffectScript.actorsAffectedByPreviousAbility.Count * 0.075f);
            //Debug.Log(are.alterParryFlat + " final parry buff");
        }

        // HARDCODED SPECIAL CASES
        if (effectRefName == "thaumdelayteleportadd")
        {
            nStatus.listEffectScripts[1].positions.Add(positions[0]);
            GameMasterScript.heroPCActor.SetActorData("delayedteleportfloor", GameMasterScript.heroPCActor.dungeonFloor);
            GameMasterScript.heroPCActor.SetActorData("delayedteleportposx", (int)positions[0].x);
            GameMasterScript.heroPCActor.SetActorData("delayedteleportposy", (int)positions[0].y);
        }
    }

    public void WriteToLogIfNecessary(StatusEffect nStatus, Fighter fight, bool statusPreviouslyExisted, Actor act)
    {
        if (GameMasterScript.actualGameStarted && !silent && fight.myStats.IsAlive() && fight.dungeonFloor == MapMasterScript.activeMap.floor)
        {
            if (!nStatus.CheckDurTriggerOn(StatusTrigger.PERMANENT) && !statusPreviouslyExisted) // Test: Does this make sense for all statuses? Permanent = no display?
            {
                GainStatusPackage ldp = GameLogDataPackages.GetGainStatusPackage();
                ldp.gameActor = act;
                ldp.statusRefNames.Add(nStatus.abilityName);
                GameLogScript.CombatEventWrite(ldp);                
            }
        }
    }

    public void DoAnimationsIfNecessary(Fighter fight, StatusEffect nStatus)
    {
        if (fight.GetActorType() == ActorTypes.MONSTER && !nStatus.isPositive)
        {
            if (fight.myAnimatable != null)
            {
                fight.myAnimatable.SetAnimConditional(fight.myAnimatable.defaultTakeDamageAnimationName);
            }
            else
            {
                Debug.Log(fight.actorRefName + " " + fight.actorUniqueID + " " + fight.GetPos() + " no anim???");
            }
        }
        else if (fight.GetActorType() == ActorTypes.HERO && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA && !nStatus.isPositive)
        {
            fight.myAnimatable.SetAnimConditional(fight.myAnimatable.defaultTakeDamageAnimationName);
        }
    }

    public void DoBleedConditionalCleanup(Fighter origFighter, Fighter fight, StatusEffect nStatus)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("last_status_bleed", 0);

        if (origFighter.GetActorType() == ActorTypes.HERO)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("target_of_addstatus", fight.actorUniqueID);
            if (nStatus.statusFlags[(int)StatusFlags.BLEED])
            {
                GameMasterScript.gmsSingleton.SetTempGameData("last_status_bleed", 1);
            }
            origFighter.myStats.CheckRunAndTickAllStatuses(StatusTrigger.PLAYER_CAUSE_STATUS);
        }

    }

    public bool CheckForFullModifiersAndSkipIfAvoidingFull(StatusEffect nStatus, bool isFullStatusForHero, Fighter fight)
    {
        if (isFullStatusForHero)
        {
            int hungryStacks = fight.myStats.CheckStatusQuantity("status_mmhungry");
            nStatus.curDuration -= hungryStacks;
            nStatus.maxDuration -= hungryStacks;

            if (UnityEngine.Random.Range(0, 1f) <= 0.33f && fight.myStats.CheckHasStatusName("status_fatchance"))
            {
                GameLogScript.LogWriteStringRef("log_lucky_eatmore");
                return true;
            }

            if (fight.myStats.CheckHasStatusName("status_gooddigestion"))
            {
                nStatus.curDuration /= 2;
                nStatus.maxDuration /= 2;
            }
        }        

        return false;
    }

}