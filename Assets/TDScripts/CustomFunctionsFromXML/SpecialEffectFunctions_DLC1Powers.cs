using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpecialEffectFunctions
{
    static List<AbilityScript> tickPossible;

    public static EffectResultPayload ReduceRandomCooldown(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        if (tickPossible == null) tickPossible = new List<AbilityScript>();
        tickPossible.Clear();
        
        foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.passiveAbility)
            {
                continue;
            }
            if (abil.GetCurCooldownTurns() > 0)
            {
                tickPossible.Add(abil);
            }
        }
        if (tickPossible.Count > 0)
        {
            AbilityScript abil = tickPossible[UnityEngine.Random.Range(0, tickPossible.Count)];
            abil.ChangeCurrentCooldown(-1);
            UIManagerScript.RefreshHotbarSkills();
        }

        return new EffectResultPayload();
    }

    public static EffectResultPayload SpecialEmpowerMonster(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        Monster orig = effect.originatingActor as Monster;
        if (orig == null)
        {
            return erp; // should not happen
        }
        orig.displayName += "+";

        int killsToLevel = orig.ReadActorData("killstolevel");
        if (killsToLevel == -1)
        {
            killsToLevel = 2;
        }
        killsToLevel--;

        if (killsToLevel == 0)
        {
            orig.myStats.AdjustLevel(1);
            killsToLevel = 2;
        }

        orig.SetActorData("killstolevel", killsToLevel);

        
        orig.xpMod += 0.1f;

        GameObject go2 = CombatManagerScript.SpawnChildSprite("LevelUpEffect", orig, Directions.NORTH, false);

        Weapon w = orig.myEquipment.GetWeapon();
        w.power += 1f;
        w.power *= 1.05f;

        return erp;
    }

    public static EffectResultPayload CheckIfTongueAttackerIsStuckToUs(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        bool removeThisStatus = false;        

        Actor tongueAttacker = GameMasterScript.gmsSingleton.TryLinkActorFromDict(effect.selfActor.ReadActorData("tongueattacker"));
        if (tongueAttacker == null || !tongueAttacker.IsFighter())
        {
            removeThisStatus = true;
        }
        else
        {
            Fighter ft = tongueAttacker as Fighter;
            if (!ft.myStats.IsAlive())
            {
                removeThisStatus = true;
            }
        }

        effect.VerifySelfActorIsFighterAndFix();

        if (removeThisStatus)
        {
            Fighter selfFT = effect.selfActor as Fighter;
            selfFT.myStats.ForciblyRemoveStatus("tonguechecker");
            selfFT.myStats.ForciblyRemoveStatus("adhesivetongue_target");
            selfFT.RemoveActorData("tongueattacker");
        }

        return new EffectResultPayload();
    }

    public static EffectResultPayload CheckForDivineStagPowerBuff(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();
        Fighter origFighter = effect.originatingActor as Fighter;

        if (origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f) // Make sure we have the divine aura status.
        {
            if (!origFighter.myStats.CheckHasStatusName("spiritstag_divineaura"))
            {
                origFighter.myStats.AddStatusByRefAndLog("spiritstag_divineaura", origFighter, 6);
            }
        }
        else
        {
            // If we have the status already, then start ticking the duration down.
            if (origFighter.myStats.CheckHasStatusName("spiritstag_divineaura"))
            {
                StatusEffect dAura = origFighter.myStats.GetStatusByRef("spiritstag_divineaura");
                dAura.curDuration--;
                if (dAura.curDuration == 0)
                {
                    origFighter.myStats.RemoveStatus(dAura, true);
                }
            }
        }
        actorsToProcess.Add(origFighter);

        return erp;
    }

    public static EffectResultPayload MonsterStealFoodAndBuffSelf(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter origFighter = effect.originatingActor as Fighter;

        foreach (Actor act in actorsToProcess)
        {
            if (act == effect.originatingActor) continue;

            Fighter ft = act as Fighter;

            Item foodStolen = TryStealFoodFromActor(origFighter, ft, false, false);

            if (foodStolen != null) 
            {
                string chompk = StringManager.GetString("misc_eatingsounds");
                BattleTextManager.NewText(chompk, origFighter.GetObject(), Color.red, 0f);
                BattleTextManager.NewText(chompk, origFighter.GetObject(), Color.red, 0f);   
                // Now also reduce quantity by 1, because we ate the item.
                Consumable c = foodStolen as Consumable;
                origFighter.myInventory.ChangeItemQuantityAndRemoveIfEmpty(c, -1);

                StringManager.SetTag(0, origFighter.displayName);
                StringManager.SetTag(1, ft.displayName);
                StringManager.SetTag(2, c.displayName);
                GameLogScript.LogWriteStringRef("exp_log_monster_atefood", origFighter);

                // And heal self
                float healAmount = origFighter.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.15f;
                int iHealAmt = (int)healAmount;
                origFighter.myStats.ChangeStat(StatTypes.HEALTH, healAmount, StatDataTypes.CUR, true);
                BattleTextManager.NewDamageText(iHealAmt, true, Color.green, origFighter.GetObject(), 0.1f, 1f);
                StringManager.SetTag(0, c.displayName);
                StringManager.SetTag(1, origFighter.displayName);
                StringManager.SetTag(2, iHealAmt.ToString());
                GameLogScript.LogWriteStringRef("log_actorheal_noeffect");
                CombatManagerScript.WaitThenGenerateSpecificEffect(origFighter.GetPos(), "FervirRecovery", effect, 0.3f, true);
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }   

    /// <summary>
    /// Status effect that is added to a Mimic after it sticks its tongue to something.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="actorsToProcess"></param>
    /// <returns></returns>
    public static EffectResultPayload CheckIfTongueIsStillValid(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        actorsToProcess.Clear();
        actorsToProcess.Add(effect.originatingActor);
        erp.actorsToProcess = actorsToProcess;

        Fighter origFT = effect.originatingActor as Fighter;
        Fighter targetFT = null;

        bool hasValidTarget = false;

        // Verify that we have a valid "stuck" target. If not, remove this status ("stickywarp") from ourselves.
        int stuckActorID = effect.originatingActor.ReadActorData("stucktoactor");
        if (stuckActorID >= 0)
        {
            Actor findActor = GameMasterScript.gmsSingleton.TryLinkActorFromDict(stuckActorID);
            if (findActor != null && findActor.IsFighter())
            {
                if (findActor.IsFighter())
                {
                    targetFT = findActor as Fighter;
                    if (targetFT.myStats.IsAlive() && !targetFT.destroyed)
                    {
                        hasValidTarget = true; // Our target must be an ALIVE fighter.
                    }
                }                                
            }
        }

        if (hasValidTarget)
        {
            // Are we rooted or stunned? If so, this breaks the status.
            Monster mn = origFT as Monster;
            if (mn.influenceTurnData.rootedThisTurn || mn.influenceTurnData.stunnedThisTurn || mn.influenceTurnData.movedByExternalActorThisTurn || !mn.myStats.IsAlive() || mn.destroyed || mn.isInDeadQueue)
            {
                //Debug.Log("We were rooted, stunned, or moved. No longer valid.");
                targetFT.myStats.ForciblyRemoveStatus("adhesivetongue_target");
                hasValidTarget = false;
            }
        }

        if (!hasValidTarget)
        {
            // Somehow we don't have a target, or target is dead, or the link was broken. So just remove this status. 
            origFT.myStats.ForciblyRemoveStatus("stickywarp");            
            origFT.anchor = origFT;
            origFT.anchorRange = 99;
            origFT.summoner = null;
            return erp;
        }

        CustomAlgorithms.GetNonCollidableTilesAroundPoint(targetFT.GetPos(), 1, origFT, MapMasterScript.activeMap);
        if (CustomAlgorithms.numNonCollidableTilesInBuffer == 0)
        {
            // break the status!
            targetFT.myStats.ForciblyRemoveStatus("adhesivetongue_target");
            origFT.myStats.ForciblyRemoveStatus("stickywarp");
            targetFT.RemoveAnchor(origFT);
            origFT.anchor = origFT;
            origFT.anchorRange = 99;
            origFT.summoner = null;
            //Debug.Log("No valid tiles, we're done here.");
        }
        /* else
        {
            if (MapMasterScript.GetGridDistance(origFT.GetPos(), targetFT.GetPos()) > 1)
            {
                // Pick a tile to move to - we're "sticking" to target if they're more than a tile away.
                MapTileData moveTarget = CustomAlgorithms.nonCollidableTileBuffer[UnityEngine.Random.Range(0, CustomAlgorithms.numNonCollidableTilesInBuffer)];
                Vector2 oldPos = origFT.GetPos();
                origFT.SetPos(moveTarget.pos);
                origFT.myMovable.AnimateSetPosition(moveTarget.pos, 0.05f, false, 0f, 0f, MovementTypes.LERP);
                Debug.Log("Good to move to target tile: " + moveTarget.pos + " from " + oldPos);

                // Make *absolutely sure* we end up in the intended tile. Ok?
                MapMasterScript.activeMap.GetTile(oldPos).RemoveActor(origFT);
                MapMasterScript.activeMap.AddActorToLocation(moveTarget.pos, origFT);

                Debug.Log("OrigFT now in position: " + origFT.GetPos() + " Does map have target? " + MapMasterScript.activeMap.actorsInMap.Contains(origFT));

                Debug.Log(moveTarget.HasActor(origFT));
            }
        } */

        return erp;
    }

    public static EffectResultPayload SurroundTargetWithIce(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        erp.actorsToProcess = actorsToProcess;
        erp.actorsToProcess.Remove(effect.originatingActor);

        if (erp.actorsToProcess.Count == 0)
        {
            return erp;
        }

        Vector2 pos = erp.actorsToProcess[0].GetPos();

        GameMasterScript.gmsSingleton.StartCoroutine(SurroundTargetWithIceAnimation(effect, effect.originatingActor.GetPos(), pos));

        erp.waitTime = 0.5f;

        return erp;
    }

    static IEnumerator SurroundTargetWithIceAnimation(SpecialEffect effect, Vector2 attackerPos, Vector2 targetCenterPos)
    {
        // Fire up to 8 potions around target square full of ice.
        float tossTime = 0.4f;

        Destructible summonObject = GameMasterScript.masterMapObjectDict["obj_icetrap2"];

        CustomAlgorithms.GetTilesAroundPoint(targetCenterPos, 1, MapMasterScript.activeMap);

        MapTileData[] tilesAroundPoint = new MapTileData[9];
        MapMasterScript.activeMap.GetListOfTilesAroundPoint(targetCenterPos, 1).CopyTo(tilesAroundPoint);

        for (int i = 0; i < tilesAroundPoint.Length; i++)
        {
            MapTileData tile = tilesAroundPoint[i];
            if (tile == null) continue;
            if (tile.tileType != TileTypes.GROUND) continue;
            if (tile.CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER)) continue;
            if (tile.pos == targetCenterPos) continue;
            //Debug.Log(CustomAlgorithms.tileBuffer[i].pos);
            // Fire a potion at this tile then create ice right after.
            GameObject potion = GameMasterScript.TDInstantiate("BluePotionSpinEffect");
            GameObject invisibleTarget = GameMasterScript.TDInstantiate("TransparentStairs");
            invisibleTarget.transform.position = tile.pos;

            // Delete the target later
            GameMasterScript.gmsSingleton.WaitThenDestroyObject(invisibleTarget, tossTime + 0.01f);

            CombatManagerScript.FireProjectile(attackerPos, tile.pos, potion, 0.4f, false, invisibleTarget, MovementTypes.TOSS, effect, 360f, true);
            yield return new WaitForSeconds(0.13f);
            GameMasterScript.SummonDestructible(effect.originatingActor, summonObject, tile.pos, 5, (tossTime - 0.13f));
        }
    }

    public static EffectResultPayload EatAttackerWhole(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        if (MapMasterScript.GetGridDistance(effect.originatingActor.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) > 1)
        {
            // This only triggers in melee.
            return new EffectResultPayload();
        }

        actorsToProcess.Clear();

        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess.Add(CombatManagerScript.bufferedCombatData.attacker);
        actorsToProcess.Add(CombatManagerScript.bufferedCombatData.attacker);
        erp.waitTime = 1f;

        Monster eaten = CombatManagerScript.bufferedCombatData.attacker as Monster;
        eaten.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);

        eaten.myAnimatable.SetAnim("TakeDamage");

        GameMasterScript.tossProjectileDummy.animLength = 0.4f;

        CombatManagerScript.FireProjectile(eaten.GetPos(), effect.originatingActor.GetPos(),
            eaten.GetObject(), 0.5f, false, effect.originatingActor.GetObject(), MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy, 360f, true, true);

        GameMasterScript.tossProjectileDummy.animLength = 0.25f;
        
        string chompk = StringManager.GetString("misc_eatingsounds");

        BattleTextManager.NewText(chompk, effect.originatingActor.GetObject(), Color.red, 0f);
        BattleTextManager.NewText(chompk, effect.originatingActor.GetObject(), Color.red, 0f);

        BattleTextManager.NewText(StringManager.GetString("exp_misc_eatenwhole"), effect.originatingActor.GetObject(), Color.red, 0.25f);

        StringManager.SetTag(0, effect.originatingActor.displayName);
        StringManager.SetTag(1, eaten.displayName);
        GameLogScript.LogWriteStringRef("exp_log_eatenwhole");

        return erp;
    }

    public static EffectResultPayload MeteorConditionalAnimation(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0.1f;
        erp.actorsToProcess.Add(effect.originatingActor);

        Destructible dtSummoner = effect.destructibleOwnerOfEffect as Destructible;
        if (dtSummoner.ReadActorData("meteoranim") != 1)
        {
            erp.waitTime = 0f;
            return erp;
        }

        GameObject meteor = CombatManagerScript.GenerateSpecificEffectAnimation(dtSummoner.GetPos(), "MeteorEffect", null, true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(dtSummoner.GetPos(), "GroundStompEffect2x", null, 0.25f, false, 0f, true);
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnObjectToStack(meteor, "MeteorEffect", 1f));

        effect.affectedActors.Add(effect.originatingActor);

        return erp;
    }

    public static EffectResultPayload ChameleonCopyAbilities(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        Monster actor = effect.originatingActor as Monster;
        int turnsSinceCopiedPowers = GameMasterScript.turnNumber - actor.ReadActorData("copypowers_turn");
        bool combatChange = (actor.myBehaviorState == BehaviorState.FIGHT || actor.myBehaviorState == BehaviorState.RUN || actor.myBehaviorState == BehaviorState.STALKING);

        if (MapMasterScript.activeMap.floor != effect.originatingActor.dungeonFloor)
        {
#if UNITY_EDITOR
            //Debug.Log("Chameleon on floor " + effect.originatingActor.dungeonFloor + " is not on floor of spawning, do nothing.");
#endif
            return new EffectResultPayload();
        }

        bool stillHasDefaultPower = false;
        if (actor.myAbilities.HasAbilityRef("skill_mon_copyabilities"))
        {
            stillHasDefaultPower = true;
        }

        if (stillHasDefaultPower || (combatChange && turnsSinceCopiedPowers >= 8))
        {
            actor.SetActorData("copypowers_turn", GameMasterScript.turnNumber);
            actor.monsterPowers.Clear();
            actor.myAbilities.RemoveAllAbilities();

            if (MapMasterScript.activeMap.dungeonLevelData.spawnTable == null)
            {
                return new EffectResultPayload();
            }

            string randomMonsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();
            while (randomMonsterRef == actor.actorRefName)
            {
                randomMonsterRef = MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef();
            }            

            MonsterTemplateData monTemplate = GameMasterScript.masterMonsterList[randomMonsterRef];
            foreach (MonsterPowerData mpd in monTemplate.monsterPowers)
            {
                AbilityScript template = mpd.abilityRef;
                if (template.passiveAbility) continue;
                MonsterPowerData chameleonMPD = new MonsterPowerData();
                AbilityScript newAbil = new AbilityScript();
                AbilityScript.CopyFromTemplate(newAbil, template);
                actor.myAbilities.AddNewAbility(newAbil, true);
                chameleonMPD.CopyFromTemplate(mpd, newAbil);
                actor.monsterPowers.Add(chameleonMPD);
                actor.OnMonsterPowerAdded(chameleonMPD, newAbil);
            }
            if (actor.myEquipment.GetArmor() != null)
            {
                actor.myEquipment.Unequip(EquipmentSlots.ARMOR, false, SND.SILENT, false);
                actor.myInventory.RemoveInvalidItems();
                // #todo - remove what we unequipped from our inventory
            }
            if (!string.IsNullOrEmpty(monTemplate.armorID))
            {
                Armor newArmor = new Armor();                
                Armor templateArmor = GameMasterScript.masterItemList[monTemplate.armorID] as Armor;
                newArmor.CopyFromItem(templateArmor);
                actor.myEquipment.AddEquipment(EquipmentSlots.ARMOR, newArmor);
            }
#if UNITY_EDITOR
            //Debug.Log("Chameleon copied powers from " + monTemplate.refName);
#endif

        }

        EffectResultPayload erp = new EffectResultPayload();
        return erp;
    }

    public static EffectResultPayload FireRandomPhotonBolts(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        int numBolts = 9;

        int maxBoltsPerTarget = 3;
        int totalBoltsFired = 0;

        float waitTimePerBolt = 0.08f;

        if (effect.parentAbility.refName == "skill_expmon_randomphoton2")
        {
            numBolts = 22;
            waitTimePerBolt = 0.05f;
        }
        else if (effect.parentAbility.refName == "dragonbreak_robot_power")
        {
            numBolts = 20;
        }

        Dictionary<Actor, int> boltsPerActor = new Dictionary<Actor, int>();

        Faction targetFaction = Faction.PLAYER;

        string hitText = StringManager.GetString("misc_hit");
        string effectToUse = "photonstormdamage";
        string impactEffect = "FervirShadowHit";

        if (effect.originatingActor.actorfaction == Faction.PLAYER)
        {
            targetFaction = Faction.ENEMY;
            maxBoltsPerTarget = 5;
            effectToUse = "photonstormdamage_player";
            impactEffect = "StaticShockEffect";
        }

        // Every actor in range gets at least one bolt
        for (int i = 0; i < effect.targetActors.Count; i++)
        {
            if (effect.targetActors[i].actorfaction != targetFaction) continue;
            boltsPerActor.Add(effect.targetActors[i], 1);

            if (i == 0)
            {
                CombatManagerScript.GenerateEffectAnimation(effect.originatingActor.GetPos(), effect.targetActors[i].GetPos(), effect, effect.targetActors[i].GetObject());
                CombatManagerScript.WaitThenGenerateSpecificEffect(effect.targetActors[i].GetPos(), impactEffect, null, 0f);
                //BattleTextManager.NewText(hitText, effect.targetActors[i].GetObject(), Color.red, 0f);
            }
            else
            {
                CombatManagerScript.cmsInstance.StartCoroutine(CombatManagerScript.cmsInstance.WaitThenGenerateEffectAnimation(effect.originatingActor.GetPos(), effect.targetActors[i].GetPos(), effect, effect.targetActors[i].GetObject(), i * waitTimePerBolt));
                CombatManagerScript.WaitThenGenerateSpecificEffect(effect.targetActors[i].GetPos(), impactEffect, null, i * waitTimePerBolt);
                //BattleTextManager.WaitThenNewTextOnObject(i * waitTimePerBolt, hitText, effect.targetActors[i].GetObject(), Color.red, 0f);
            }           

            numBolts--;
            totalBoltsFired++;
            erp.waitTime += waitTimePerBolt;
            erp.actorsToProcess.Add(effect.targetActors[i]);
        }

        // Now how many bolts are left? Fire them at random in the 3x3 square, but don't exceed X bolts per target
        for (int i = 0; i < numBolts; i++)
        {
            Vector2 randomPosition = effect.positions[UnityEngine.Random.Range(0, effect.positions.Count)];            
            MapTileData mtd = MapMasterScript.GetTile(randomPosition);
            foreach(Actor act in mtd.GetAllActors())
            {
                if (act.actorfaction != targetFaction) continue;
                if (act.GetActorType() != ActorTypes.MONSTER && act.GetActorType() != ActorTypes.HERO) continue;
                int existingBolts = 0;
                if (boltsPerActor.TryGetValue(act, out existingBolts))
                {
                    if (existingBolts >= maxBoltsPerTarget)
                    {
                        // Maxed out. Pick another tile for flashiness
                        i--;
                        continue;
                    }
                    boltsPerActor[act]++;
                    //BattleTextManager.WaitThenNewTextOnObject((totalBoltsFired+1) * waitTimePerBolt, hitText, act.GetObject(), Color.red, 0f);
                }
            }

            totalBoltsFired++;
            CombatManagerScript.cmsInstance.StartCoroutine(CombatManagerScript.cmsInstance.WaitThenGenerateEffectAnimation(effect.originatingActor.GetPos(), mtd.pos, effect, null, totalBoltsFired * waitTimePerBolt));
            CombatManagerScript.WaitThenGenerateSpecificEffect(mtd.pos, impactEffect, null, totalBoltsFired * waitTimePerBolt);            
            erp.waitTime += 0.075f;
        }

        DamageEffect usedForCalculation = GameMasterScript.masterEffectList[effectToUse] as DamageEffect;
        usedForCalculation.originatingActor = effect.originatingActor;
        usedForCalculation.targetActors.Clear();
        usedForCalculation.positions = effect.positions;
        usedForCalculation.centerPosition = effect.centerPosition;
        usedForCalculation.parentAbility = effect.parentAbility;
        // Finally, calculate damage.

        foreach(Actor key in boltsPerActor.Keys)
        {
            usedForCalculation.targetActors.Add(key);
            key.SetActorData("hitbybolts", boltsPerActor[key]);
        }

        usedForCalculation.DoEffect();

        erp.waitTime *= 0.6f;

        return erp;
    }

    public static EffectResultPayload FrogSquashJumpToPosition(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        Fighter user = effect.originatingActor as Fighter;

        GameMasterScript.tossProjectileDummy.projectileTossHeight = 3.5f;
        GameMasterScript.tossProjectileDummy.projectileMovementType = MovementTypes.TOSS;

        CombatManagerScript.GenerateSpecificEffectAnimation(user.GetPos(), "DustCloudLanding", effect, true);

        GameMasterScript.cameraScript.AddScreenshake(0.2f);

        Vector2 targetPos = Vector2.zero;

        Actor target = effect.targetActors.GetRandomElement();

        if (target == null)
        {
            target = GameMasterScript.heroPCActor;
        }

        MapTileData landingTile = MapMasterScript.activeMap.GetRandomEmptyTile(target.GetPos(), 1, true, anyNonCollidable: true, preferLOS: true);        

        GameObject afterImageCreator = GameMasterScript.TDInstantiate("AfterImageCreator");
        afterImageCreator.transform.SetParent(user.GetObject().transform);
        afterImageCreator.transform.localPosition = Vector3.zero;
        afterImageCreator.GetComponent<AfterImageCreatorScript>().Initialize(target.GetPos(), 1.5f, Vector2.Distance(user.GetPos(), target.GetPos()), user.mySpriteRenderer,
            true,
            1f,
            MovementTypes.SMOOTH,
            20); // number of images lol

        effect.animLength = 1.5f;

        Vector2 originalPos = user.GetPos();

        CombatManagerScript.FireProjectile(effect.originatingActor.GetPos(), target.GetPos(),
            user.GetObject(),
            1.5f,
            false,
            target.GetObject(),
            MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy,
            360f,
            false,
            true);

        CombatManagerScript.FireProjectile(originalPos, target.GetPos(),
            afterImageCreator,
            1.5f,
            false,
            target.GetObject(),
            MovementTypes.TOSS,
            GameMasterScript.tossProjectileDummy,
            360f,
            false,
            true);

        GameMasterScript.tossProjectileDummy.projectileTossHeight = 1.2f;

        GameMasterScript.gmsSingleton.StartCoroutine(WaitThenMoveActorWithShake(user, originalPos, landingTile.pos, 1.51f));

        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "MudExplosion", effect, 1.5f, false, 0f, true);

        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 1.5f;
        erp.actorsToProcess = new List<Actor>() { effect.originatingActor };

        return erp;
    }

    public static IEnumerator WaitThenMoveActorWithShake(Actor act, Vector2 oldPos, Vector2 newPos, float time)
    {
        if (act.GetActorType() == ActorTypes.HERO)
        {
            GameMasterScript.gmsSingleton.SetTempFloatData("bufferx", newPos.x);
            GameMasterScript.gmsSingleton.SetTempFloatData("buffery", newPos.y);
            TileInteractions.HandleEffectsForHeroMovingIntoTile(MapMasterScript.GetTile(newPos), true);
            TileInteractions.CheckAndRunTileOnMove(MapMasterScript.GetTile(newPos), GameMasterScript.heroPCActor);
            CameraController.UpdateCameraPosition(newPos, true);
        }
        yield return new WaitForSeconds(time);
        MapMasterScript.singletonMMS.MoveAndProcessActor(oldPos, newPos, act);
        act.myMovable.AnimateSetPosition(newPos, 0.2f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.AddScreenshake(0.3f);
    }

    public static EffectResultPayload ImproveCombatInMud(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        ImproveCombatInTerrain(effect, LocationTags.MUD);

        EffectResultPayload erp = new EffectResultPayload();
        return erp;
    }

    public static EffectResultPayload ImproveCombatInWater(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        ImproveCombatInTerrain(effect, LocationTags.WATER);

        EffectResultPayload erp = new EffectResultPayload();
        return erp;
    }

    static void ImproveCombatInTerrain(SpecialEffect effect, LocationTags tagToCheck)
    {
        MapTileData curTile = MapMasterScript.activeMap.GetTile(effect.originatingActor.GetPos());
        bool improved = false;
        if (curTile.CheckTag(tagToCheck))
        {
            if (CombatManagerScript.damagePayload.atk.actorUniqueID == effect.originatingActor.actorUniqueID)
            {
                // IMPROVE damage if we are attacker                
                CombatManagerScript.damagePayload.damage.amount *= 1.25f;
                improved = true;
            }
            if (CombatManagerScript.damagePayload.def.actorUniqueID == effect.originatingActor.actorUniqueID)
            {
                // DECREASE damage if we are attacker
                CombatManagerScript.damagePayload.damage.amount *= 0.66f;
                improved = true;
            }
        }

        // Print message if we're an enemy and haven't seen the message in awhile.
        if (improved && effect.originatingActor.actorfaction == Faction.ENEMY)
        {
            int turnOfLastPowerMsg = effect.originatingActor.ReadActorData("turnterrainpowermsg");
            if (GameMasterScript.turnNumber - turnOfLastPowerMsg > 3)
            {
                StringManager.SetTag(0, effect.originatingActor.displayName);
                switch(tagToCheck)
                {
                    case LocationTags.WATER:
                        StringManager.SetTag(1, StringManager.GetString("misc_dmg_water"));
                        break;
                    case LocationTags.MUD:
                        StringManager.SetTag(1, StringManager.GetString("des_obj_mudtile_dname"));
                        break;
                }
                GameLogScript.LogWriteStringRef("log_mon_terrain_power", effect.originatingActor);
                effect.originatingActor.SetActorData("turnterrainpowermsg", GameMasterScript.turnNumber);
            }
        }
    }

    public static EffectResultPayload ChakraShiftConsumeBuffs(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        List<StatusEffect> effectsToRemove = new List<StatusEffect>();
        int countPriorToRemoval = 0;

        // Look for every positive, temporary status buff and count that up.
        foreach (StatusEffect se in GameMasterScript.heroPCActor.myStats.GetAllStatuses())
        {
            if (!se.isPositive) continue; // Don't remove debuffs
            if (se.toggled) continue; // Can't be any toggled status.
            if (!se.CheckDurTriggerOn(StatusTrigger.TURNEND)) continue; // Must be something temporary that ticks down.
            if (!se.showIcon) continue; // Don't remove hidden buffs.

            effectsToRemove.Add(se);
            countPriorToRemoval += se.quantity;
        }

        foreach (StatusEffect se in effectsToRemove)
        {
            GameMasterScript.heroPCActor.myStats.RemoveStatus(se, true);
        }

        if (countPriorToRemoval > 0)
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("chakrashift", GameMasterScript.heroPCActor, 5);
        }
        for (int i = 1; i < countPriorToRemoval; i++)
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("chakrashift", GameMasterScript.heroPCActor, 5);
        }

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirBuff", null, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirGrandRecovery", null, false, 0f, true);

        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess.Clear();
        erp.actorsToProcess.Add(GameMasterScript.heroPCActor);
        erp.waitTime = 0.33f;
        return erp;
    }

    public static EffectResultPayload DestroyHazardTile(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        // Destroy any non-player, non-dungeon, SUMMONED hazard tiles that aren't already dead.
        if (effect.positions.Count > 0)
        {
            MapTileData tileToCheck = MapMasterScript.activeMap.GetTile(effect.positions[0]);
            foreach (Actor act in tileToCheck.GetAllActors())
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                if (act.turnsToDisappear == 0) continue;
                if (act.isInDeadQueue) continue;
                if (act.actorfaction == Faction.DUNGEON || act.actorfaction == Faction.PLAYER)
                {
                    continue;
                }

                GameMasterScript.AddToDeadQueue(act, true);
            }

            CombatManagerScript.GenerateSpecificEffectAnimation(effect.positions[0], "DustCloudLanding", effect, true);
            CombatManagerScript.WaitThenGenerateSpecificEffect(effect.positions[0], "FervirRecovery", effect, 0.25f, true);
        }


        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0.25f;
        return erp;
    }

    public static EffectResultPayload CombatBiographyPickup(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        if (actorsToProcess.Contains(GameMasterScript.heroPCActor))
        {
            Debug.Log("Picking up biography!");
            Actor book = MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos()).GetActorRef("exp_obj_combatbiography");
            if (book != null)
            {
                if (book.isInDeadQueue)
                {
                    return erp;
                }
                GameMasterScript.AddToDeadQueue(book, true);
            }
            else
            {
                return erp;
            }
            CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirGrandRecovery", effect, true);
            int bookCharges = effect.destructibleOwnerOfEffect.ReadActorData("brushstrokes");
            int currentCharges = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("brushstroke_charge");
            int totalChargesToAdd = bookCharges + currentCharges;
            if (totalChargesToAdd > 6) totalChargesToAdd = 6; // 6 is max stacks of brushstroke
            for (int i = 0; i < totalChargesToAdd; i++)
            {
                GameMasterScript.heroPCActor.myStats.AddStatusByRef("brushstroke_charge", GameMasterScript.heroPCActor, 99);
            }

            StringManager.SetTag(0, totalChargesToAdd.ToString());
            GameLogScript.LogWriteStringRef("exp_log_readcombatbook");

            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_dualwielderemblem_tier0_biography"))
            {
                GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("activedodge33", GameMasterScript.heroPCActor, 10);
            }

            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("calligrapherskill50", GameMasterScript.heroPCActor, 8);

            // And destroy the book too.
        }

        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }


    public static EffectResultPayload TransferDominatedHealth(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;

        Monster mnActor = effect.selfActor as Monster;
        if (mnActor == null || !mnActor.myStats.IsAlive())
        {
            return erp;
        }

        float dmg = mnActor.myStats.GetMaxStat(StatTypes.HEALTH) * 0.02f;

        int abilityLevel = SharaModeStuff.GetDominateLevel();

        bool doEffect = true;

        if (abilityLevel >= 3) 
        {
            if (dmg >= mnActor.myStats.GetCurStat(StatTypes.HEALTH) - 1f)
            {
                return erp;
            } 

            // The damage only occurs every X turns, otherwise the critters die too fast.
            int turnsSinceDominateLoseHealth = mnActor.ReadActorData("turns_since_domlosehealth");
            if (turnsSinceDominateLoseHealth == -1) 
            {
                turnsSinceDominateLoseHealth = 6;
            }
            turnsSinceDominateLoseHealth--;
            if (turnsSinceDominateLoseHealth <= 0) 
            {
                turnsSinceDominateLoseHealth = 6;
            }            
            else 
            {
                doEffect = false;
            }
            mnActor.SetActorData("turns_since_domlosehealth", turnsSinceDominateLoseHealth);
        }

        if (!doEffect) 
        {
            return erp;
        }

        // When ability is maxed out, we stop healing the player per turn, and instead the creature loses health per turn.
        // We heal AFTER the creature dies.

        float healing = dmg * 0.5f;

        if (healing < 1f)
        {
            healing = 1f;
        }

        if (abilityLevel >= 3) 
        {
            mnActor.TakeDamage(dmg, DamageTypes.PHYSICAL);
            LoseHPPackage lhp = GameLogDataPackages.GetLoseHPPackage();
            lhp.abilityUser = GameMasterScript.heroPCActor.displayName;
            lhp.damageAmount = dmg;
            lhp.damageEffectSource = effect.effectName;
            lhp.dType = DamageTypes.PHYSICAL;
            lhp.gameActor = mnActor;
            GameLogScript.CombatEventWrite(lhp, TextDensity.VERBOSE); 
        }

        if (mnActor.GetXPModToPlayer() >= 0.25f && abilityLevel < 3)
        {
            /* ChangeCoreStatPackage ccs = new ChangeCoreStatPackage();
            ccs.abilityUser = mnActor.displayName;
            ccs.effectSource = effect.effectName;
            ccs.gameActor = GameMasterScript.heroPCActor;
            ccs.percentBased = false;
            ccs.statChanges[(int)StatTypes.HEALTH] = healing;
            GameLogScript.CombatEventWrite(ccs, TextDensity.VERBOSE); */
            GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, healing, StatDataTypes.CUR, true);
            GameMasterScript.heroPCActor.AddActorData("dominatehealth", (int)healing);
            
            /* {
                BattleTextManager.NewText(((int)healing).ToString(), GameMasterScript.heroPCActor.GetObject(), Color.green, 1f);
            } */
            
        }

        if (abilityLevel >= 3) 
        {
            BattleTextManager.NewDamageText((int)dmg, false, Color.red, mnActor.GetObject(), 0.25f, 1f);
        }        

        return erp;
    }

    static EffectResultPayload ShieldDestroy(Actor act, List<Actor> actorsToProcess, string prefab)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter ft = act as Fighter;
        ft.myStats.CheckRunAllStatuses(StatusTrigger.ON_SHIELD_SHATTER);

        GameObject go = CombatManagerScript.GenerateSpecificEffectAnimation(act.GetPos(), prefab, null, true);
        SpriteEffect se = go.GetComponent<SpriteEffect>();

        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnEffectToStack(se, 3f));

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload VoidShieldDestroyedEffect(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        // Let's try not doing anything. Fire void bolts while shield is UP instead.
        return ShieldDestroy(effect.originatingActor, actorsToProcess, "VoidShatterParticles");

        // Consider firing bolts at nearby targets

        int numBolts = 3;
        List<MapTileData> nearbyTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(effect.originatingActor.GetPos(), 3);
        List<Monster> possibleTargets = new List<Monster>();
        Fighter ft = effect.originatingActor as Fighter;
        foreach (MapTileData mtd in nearbyTiles)
        {
            if (mtd.tileType == TileTypes.WALL) continue;
            bool visible = ft.visibleTilesArray[(int)mtd.pos.x, (int)mtd.pos.y];
            if (!visible) continue;
            List<Actor> targs = mtd.GetAllTargetable();

            foreach (Actor act in targs)
            {
                if (act.GetActorType() != ActorTypes.MONSTER) continue;
                Monster mn = act as Monster;
                if (mn.actorfaction == Faction.PLAYER) continue;
                if (!mn.myStats.IsAlive()) continue;
                possibleTargets.Add(mn);
            }
        }      

        int targetCount = possibleTargets.Count;

        if (targetCount > 0)
        {
            possibleTargets.Shuffle();
            for (int i = 0; i < 3; i++) 
            {
                if (i >= targetCount-1) break;
                // Do the bolt
                //Monster target = possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];
                Monster target = possibleTargets[i];
                DamageEffect refBolt = GameMasterScript.GetEffectByRef("voidbolt_damage") as DamageEffect;
                DamageEffect vBolt = new DamageEffect();
                vBolt.CopyFromTemplate(refBolt);
                vBolt.originatingActor = effect.originatingActor;
                vBolt.centerPosition = target.GetPos();
                vBolt.parentAbility = GameMasterScript.masterAbilityList["skill_voidbolt"];
                vBolt.targetActors = new List<Actor>() { target };
                vBolt.positions = new List<Vector2>() { target.GetPos() };
                vBolt.DoEffect();
                target.myStats.AddStatusByRefAndLog("exp_status_essencedrain", effect.originatingActor, 5);                
                actorsToProcess.Add(target);
            }
        }

        return ShieldDestroy(effect.originatingActor, actorsToProcess, "VoidShatterParticles");
    }

    public static EffectResultPayload FlowShieldDestroyedEffect(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        return ShieldDestroy(effect.originatingActor, actorsToProcess, "FlowShatterParticles");
    }

    public static EffectResultPayload FlowShieldAbsorbDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float dmg = CombatManagerScript.damagePayload.currentDamageValue;

        Fighter ft = effect.selfActor as Fighter;

        float damageRemainingInShield = ft.ReadActorData("flowshield_dmgleft");

        if (dmg < 0) dmg = 0;

        if (damageRemainingInShield > dmg)
        {
            int iDmg = (int)dmg;

            damageRemainingInShield -= iDmg;
            
            //shield damage is important to display
            BattleTextManager.NewDamageText(iDmg, false, Color.cyan, ft.GetObject(), 0.0f, 1f);

            GameMasterScript.gmsSingleton.SetTempGameData("shieldabsorbedalldamage", 1);
            PrintShieldAbsorption(iDmg, ft);           

            //but don't throw up a 0 for the fully absorbed damage.
            ft.SetActorData("hide_next_zero_battledamage", 1);
            dmg = 0f;
        }
        else
        {
            dmg -= damageRemainingInShield;
            
            //even though the shield is broken, we want to show how much it ate.
            BattleTextManager.NewDamageText((int)dmg, false, Color.cyan, ft.GetObject(), 0.0f, 1f);

            PrintShieldAbsorption((int)damageRemainingInShield, ft);

            damageRemainingInShield = 0f;
        }

        ft.SetActorData("flowshield_dmgleft", (int)damageRemainingInShield);
        ft.SetActorData("shieldinfo_dirty", 1);
        

        if (damageRemainingInShield <= 0f)
        {
            ft.myStats.RemoveStatus(effect.parentAbility as StatusEffect, true);
            StringManager.SetTag(0, ft.displayName);
            GameLogScript.LogWriteStringRef("exp_log_shieldshatter");
        }

        CombatManagerScript.damagePayload.currentDamageValue = dmg;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    static void PrintShieldAbsorption(int iDmg, Fighter shieldActor)
    {
        if (CombatManagerScript.bufferedCombatData != null && CombatManagerScript.bufferedCombatData.attacker != null)
        {
            StringManager.SetTag(0, CombatManagerScript.bufferedCombatData.attacker.displayName);
            StringManager.SetTag(1, iDmg.ToString());
            StringManager.SetTag(2, shieldActor.displayName);
            if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
            {
                StringManager.SetTag(3, CombatManagerScript.damagePayload.effParent.effectName);
                GameLogScript.LogWriteStringRef("log_shield_absorb_damage_effect", shieldActor);
            }
            else
            {
                GameLogScript.LogWriteStringRef("log_shield_absorb_damage_attack", shieldActor);
            }
        }
    }

    public static EffectResultPayload AbsorbPowerupsOnTile(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        MapTileData mtd = MapMasterScript.activeMap.GetTile(effect.originatingActor.GetPos());
        TileInteractions.pool_removeActors.Clear();
        bool anyAbsorbed = false;
        foreach (Actor act in mtd.GetPowerupsInTile())
        {
            TileInteractions.pool_removeActors.Add(act);
            anyAbsorbed = true;
        }
        foreach(Actor act in TileInteractions.pool_removeActors)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(act);
            act.myMovable.FadeOutThenDie();
        }

        if (anyAbsorbed)
        {
            addWaitTime = 0.5f;
            CombatManagerScript.GenerateSpecificEffectAnimation(effect.originatingActor.GetPos(), "FervirRecovery", effect, true);
            Fighter origFT = effect.originatingActor as Fighter;
            float restoreAmount = 0.15f * origFT.myStats.GetMaxStat(StatTypes.HEALTH);
            origFT.myStats.ChangeStat(StatTypes.HEALTH, restoreAmount, StatDataTypes.CUR, true);
            int iRestoreAmount = (int)restoreAmount;
            BattleTextManager.NewText(iRestoreAmount.ToString(), effect.originatingActor.GetObject(), Color.green, 0.2f);
            StringManager.SetTag(0, effect.originatingActor.displayName);
            StringManager.SetTag(1, iRestoreAmount.ToString());
            GameLogScript.LogWriteStringRef("exp_log_powerup_absorb", effect.originatingActor);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload TrueHoverReduceGroundDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;
        if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            DamageEffect de = CombatManagerScript.damagePayload.effParent as DamageEffect;
            if (de != null && de.parentAbility != null && de.parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
            {
                curdamage *= 0.7f;
            }
        }
        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload RandomElementalAbsorb(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectScript customFX = null;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter orig = effect.originatingActor as Fighter;

        DamageTypes rand = (DamageTypes)UnityEngine.Random.Range(0, (int)DamageTypes.COUNT);

        if (orig.lastDamageTypeReceived != DamageTypes.COUNT)
        {
            rand = orig.lastDamageTypeReceived;
        }        

        switch (rand)
        {
            case DamageTypes.PHYSICAL:
                customFX = GameMasterScript.GetEffectByRef("exp_physabsorb");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.FIRE:
                customFX = GameMasterScript.GetEffectByRef("exp_fireabsorb");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.WATER:
                customFX = GameMasterScript.GetEffectByRef("exp_waterabsorb");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.LIGHTNING:
                customFX = GameMasterScript.GetEffectByRef("exp_lightningabsorb");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.POISON:
                customFX = GameMasterScript.GetEffectByRef("exp_poisonabsorb");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.SHADOW:
                customFX = GameMasterScript.GetEffectByRef("exp_shadowabsorb");
                customFX.CopyLiveData(effect);
                break;
        }
        addWaitTime += customFX.DoEffect();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }
}
