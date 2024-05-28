using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class DamageModifierFunctions
{

    //public static Dictionary<string, Func<Actor, Fighter, float, int, DamageEffect, float>> dictDelegates;

    public static readonly string[] prismStrings = new string[]
    {
        "prismdmg0",
        "prismdmg1",
        "prismdmg2",
        "prismdmg3",
        "prismdmg4",
        "prismdmg5"
    };

    static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

//        dictDelegates = new Dictionary<string, Func<Actor, Fighter, float, int, DamageEffect, float>>();


        initialized = true;
    }

    public static void CacheScript(string scriptName)
    {
        /*if (!initialized) Initialize();

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo myMethod = typeof(DamageModifierFunctions).GetMethod(scriptName, new Type[] { typeof(Actor), typeof(Fighter), typeof(float), typeof(int), typeof(DamageEffect) });

        Func<Actor, Fighter, float, int, DamageEffect, float> converted = 
            (Func<Actor, Fighter, float, int, DamageEffect, float>)Delegate.CreateDelegate(typeof(Func<Actor, Fighter, float, int, DamageEffect, float>), myMethod);

        dictDelegates.Add(scriptName, converted);*/
    }

    public static void ResetPrismVariablesForActor(string[] args)
    {
        int actorID = Int32.Parse(args[0]);

        Actor act = GameMasterScript.gmsSingleton.TryLinkActorFromDict(actorID);
        if (act == null) return;
        if (act.GetActorType() != ActorTypes.MONSTER) return;

        Monster mn = act as Monster;
        if (mn.isInDeadQueue || !mn.myStats.IsAlive()) return;

        for (int i = 0; i < prismStrings.Length; i++)
        {
            mn.SetActorData(prismStrings[i], 0);
        }
    }

    public static float ArcaneArrow(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int roll = UnityEngine.Random.Range(0, 5);
        switch(roll)
        {
            case 0:
                parent.damType = DamageTypes.FIRE;
                parent.spriteEffectRef = "FireBall";
                break;
            case 1:
                parent.damType = DamageTypes.LIGHTNING;
                parent.spriteEffectRef = "LightningBolt";
                break;
            case 2:
                parent.damType = DamageTypes.SHADOW;
                parent.spriteEffectRef = "ShadowBolt";
                break;
            case 3:
                parent.damType = DamageTypes.POISON;
                parent.spriteEffectRef = "PoisonBolt";
                break;
            case 4:
                parent.damType = DamageTypes.WATER;
                parent.spriteEffectRef = "WaterProjectile2";
                break;
        }

        return value;
    }

    public static float PlayMeteorAnimation(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        GameObject meteor = CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "MeteorEffect", null, true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(GameMasterScript.heroPCActor.GetPos(), "GroundStompEffect2x", null, 0.4f, false, 0f, true);
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnObjectToStack(meteor, "MeteorEffect", 1f));
        parent.extraWaitTime = 0.75f;
        return 0f;
    }

    public static float ModifyDamagePrismBlast(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        value = 0;

        Fighter owner = originatingActor as Fighter;

        int[] dmgArray = new int[(int)DamageTypes.COUNT];
        float highestValue = 0f;
        DamageTypes highestElement = DamageTypes.COUNT;

        for (int i = 1; i < dmgArray.Length; i++) // start at 1 to skip Physical
        {
            dmgArray[i] = owner.ReadActorData(prismStrings[i]);
            if (dmgArray[i] > 0)
            {
                value += (float)dmgArray[i];
                if (dmgArray[i] > highestValue)
                {
                    highestElement = (DamageTypes)i;
                    highestValue = dmgArray[i];
                }
            }            
        }

        int lastTimeUsedPrism = owner.ReadActorData("prismexplode_turn");
        if (lastTimeUsedPrism != GameMasterScript.turnNumber)
        {
            GameMasterScript.AddEndOfTurnFunction(DamageModifierFunctions.ResetPrismVariablesForActor, new string[] { owner.actorUniqueID.ToString() });
            owner.SetActorData("prismexplode_turn", GameMasterScript.turnNumber);
        }

        if (highestElement != DamageTypes.COUNT)
        {
            parent.damType = highestElement;
        }

        parent.spriteEffectRef = GetProjectileByDamageType(highestElement);

        value *= 2f; // twice as strong as the element it absorbed

        return value;
    }

    public static float EnhanceWildChildDamage(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter owner = originatingActor as Fighter;
        if (owner.myStats.CheckHasActiveStatusName("wildchild_skilldmg_up"))
        {
            value *= 1.3f;
            owner.myStats.RemoveStatusByRef("wildchild_skilldmg_up");
        }

        return value;
    }

    public static float SetDamageTypeToLastUsedSkill(Actor originatingActor, Fighter target, float value, int numActorsToProcess,
        DamageEffect parent)
    {
        AbilityUsageInstance aui = CombatManagerScript.GetLastUsedAbility();

        DamageTypes dtToUse = DamageTypes.PHYSICAL;

        foreach(EffectScript eff in aui.abilityRef.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                DamageEffect de = eff as DamageEffect;
                dtToUse = de.damType;
            }
        }

        string projectileAnim = "";

        projectileAnim = GetProjectileByDamageType(dtToUse);

        parent.spriteEffectRef = projectileAnim;
        parent.damType = dtToUse;

        return value;
    }

    static string GetProjectileByDamageType(DamageTypes dtToUse)
    {
        switch (dtToUse)
        {
            case DamageTypes.FIRE:
                return "FireBall";
            case DamageTypes.POISON:
                return "PoisonBolt";
            case DamageTypes.LIGHTNING:
                return "LightningBolt";
            case DamageTypes.WATER:
                return "WaterProjectile2";
            case DamageTypes.SHADOW:
                return "ShadowBoltFast";
        }

        return "BasicEnergyProjectile";
    }

    public static float SpitIceDamageAndVFX(Actor originatingActor, Fighter target, float value, int numActorsToProcess,
        DamageEffect parent)
    {
        // Increase damage by 25% for each tile over 1, so 2 tiles = +25% dmg, 3 tiles = +50%, 4 tiles = +75%
        int distance = MapMasterScript.GetGridDistance(originatingActor.GetPos(), target.GetPos());
        distance -= 1;

        value += (value *= (distance * 0.25f));

        value = EnhanceWildChildDamage(originatingActor, target, value, numActorsToProcess, parent);

        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "IceGrowAttack2x", parent, 0.5f, true);

        return value;
    }

    public static float InkstormModifyDamage(Actor originatingActor, Fighter target, float value, int numActorsToProcess,
        DamageEffect parent)
    {
        int inkstormValue = -1;
        if (target.effectsInflictedOnTurn.TryGetValue("inkstorm", out inkstormValue))
        {
            if (inkstormValue == 0)
            {
                value *= 0.235f;
            }
        }
        else
        {
            target.AddEffectInflicted("inkstorm", 0);
        }        

        return value;
    }

    public static float AddScreenShake(Actor originatingActor, Fighter target, float value, int numActorsToProcess,
        DamageEffect parent)
    {
        GameMasterScript.cameraScript.AddScreenshake(0.33f);
        return value;
    }
    
    public static float EatTargetWeapon(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (target.GetActorType() != ActorTypes.HERO)
        {
            return value;
        }

        // our pet should not eat our weapon
        if (originatingActor.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            return value;
        }

        Weapon w = GameMasterScript.heroPCActor.myEquipment.GetWeapon();

        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(w))
        {
            return value;
        }
        
        // Let's eat that weapon! 

        Sprite weaponSprite = w.GetSpriteForUI();
        TDAnimationScripts.TossItemSprite(weaponSprite, GameMasterScript.heroPCActor, originatingActor, 0.55f);

        string chompk = StringManager.GetString("misc_eatingsounds");

        BattleTextManager.NewText(chompk, originatingActor.GetObject(), Color.red, 0f);
        BattleTextManager.NewText(chompk, originatingActor.GetObject(), Color.red, 0f);

        GameMasterScript.heroPCActor.myEquipment.UnequipByReference(w);
        Fighter oActor = originatingActor as Fighter;
        oActor.myInventory.AddItemRemoveFromPrevCollection(w, false);
        UIManagerScript.RemoveWeaponFromActives(w);
        UIManagerScript.UpdateActiveWeaponInfo();
        StringManager.SetTag(0, originatingActor.displayName);
        StringManager.SetTag(1, w.displayName);
        GameLogScript.LogWriteStringRef("exp_log_eatweapon");
        w.SetActorData("alwaysdrop", 1);

        MetaProgressScript.EnsureRelicRefIsNeverRemoved(w.actorRefName);

        return value;
    }

    public static float DragonLimitDamageEnhance(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter ft = originatingActor as Fighter;
        value += (value * 0.1f * ft.myStats.CheckStatusQuantity("xp2_dragons"));

        return value;
    }

    public static float ScaleDamageToNumberOfPhotonBolts(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        CustomAlgorithms.sBuilder.Length = 0;
        int numTimesHitByBolt = target.ReadActorData("hitbybolts");
        if (numTimesHitByBolt > 1)
        {
            value += (value * 0.5f * (numTimesHitByBolt-1));
        }

        if (originatingActor.GetActorType() == ActorTypes.HERO)
        {
            Fighter ft = originatingActor as Fighter;

            value += (value * 0.1f * ft.myStats.CheckStatusQuantity("xp2_dragons"));
        }

        target.SetActorData("hitbybolts", 0);

        CustomAlgorithms.sBuilder.Append(numTimesHitByBolt);
        CustomAlgorithms.sBuilder.Append(" ");
        CustomAlgorithms.sBuilder.Append(StringManager.GetString("misc_hit"));
        BattleTextManager.NewText(CustomAlgorithms.sBuilder.ToString(), target.GetObject(), Color.red, 0.7f);

        return value;
    }

    public static float DamageBasedOnThrownEnemy(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        value = target.myStats.GetCurStat(StatTypes.HEALTH) * 0.15f;

        if (EffectScript.actorsAffectedByPreviousAbility.Count > 0)
        {            
            Fighter thrownMon = EffectScript.actorsAffectedByPreviousAbility[0] as Fighter;
            if (target != thrownMon)
            {
                value = thrownMon.myStats.GetCurStat(StatTypes.HEALTH) * 0.15f;
            }
        }

        return value;
    }

    public static float DoNotExceedTargetHealth(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        // 105 >= 106-1?
        if (value > target.myStats.GetCurStat(StatTypes.HEALTH) - 1f)
        {
            value = target.myStats.GetCurStat(StatTypes.HEALTH) - 1f;
        }

        /* if (CombatManagerScript.bufferedCombatData == null)
        {
            Debug.Log("No buffered combat data?");
            return value;
        }
        else
        {
            CombatManagerScript.bufferedCombatData.damageCapForPayload = target.myStats.GetCurStat(StatTypes.HEALTH) - 1f;
        } */

        GameMasterScript.gmsSingleton.SetTempFloatData("dmgcap", value);
                
        return value;
    }

    public static float TryEnhanceCalligrapherScroll(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter owner = originatingActor as Fighter;
        if (owner.myStats.CheckHasActiveStatusName("dualwielderbonus3"))
        {
            int lastAmountOfBrushstrokesUsed = originatingActor.ReadActorData("last_brushstrokesused");
            value += (value * 0.15f * lastAmountOfBrushstrokesUsed);
        }        

        return value;
    }

    public static float EatThunderingLionCharge(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter origFighter = originatingActor as Fighter;
        if (origFighter != null && parent != null && parent.parentAbility != null)
        {
            origFighter.myStats.MarkStatusForRemoval("status_tlioncharge");
        }
        return value;
    }

    public static float ScorpionStingOnTarget(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "FervirPierceEffect", parent, true);

        target.myStats.AddStatusByRefAndLog("mildvenom", originatingActor, 5);

        return value;
    }

    public static float Suplex(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter atk = originatingActor as Fighter;

        GameMasterScript.SetAnimationPlaying(true);       

        GameEventsAndTriggers.DoStartSuplex(originatingActor, target, 0.65f);

        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenStopAnimation(1.5f));

        return value;
    }

    public static float PlayGreenSmokeEffect(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "GreenSmokePoof", parent, true, 0.15f);
        return value;  
    }

    public static float PlayLeafPoofEffect(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "LeafPoof", parent, true, 0.15f);
        return value;
    }

    public static float PlayIceBreakEffect(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "IceGrowAttack2x", parent, 0.15f, true, 0.15f);
        return value;
    }

    public static float GamblerTossCard(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (GameMasterScript.heroPCActor.gamblerHand.Count == 0)
        {
            return value;
        }
        PlayingCard pc = GameMasterScript.heroPCActor.gamblerHand[UnityEngine.Random.Range(0, GameMasterScript.heroPCActor.gamblerHand.Count)];

        GameMasterScript.heroPCActor.gamblerHand.Remove(pc);
        PlayingCard.ReturnCard(pc);

        int faceValue = (int)pc.face + 1;

        float valMod = ((faceValue * 2) / 100f) + 1f;

        value *= valMod;

        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, false);

        return value;
    }

    public static float ShockAddParalysis(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        StatusEffect se = target.myStats.AddStatusByRef("status_paralyzed", originatingActor, 2);

        if (se != null)
        {
            StringManager.SetTag(0, target.displayName);
            StringManager.SetTag(1, se.abilityName);
            GameLogScript.LogWriteStringRef("log_gainstatus_single_withtag");
        }

        return value;
    }
    public static float ValkyrieDamage(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (MapMasterScript.GetGridDistance(originatingActor.GetPos(),target.GetPos()) > 1)
        {
            value *= 1.2f;
        }

        return value;
    }

    public static float MoreDamageIfBleeding(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (target.myStats.CountStatusesByFlag(StatusFlags.BLEED) > 0)
        {
            value *= 1.3f;
        }

        return value;
    }
    
    public static float ReduceEssenceStormAmmo(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter ft = originatingActor as Fighter;
        int curAmmo = ft.ReadActorData("essencestorm_ammo");
        curAmmo--;
        ft.SetActorData("essencestorm_ammo", curAmmo);
        if (curAmmo <= 0)
        {
            ft.myStats.RemoveAllStatusByRef("exp_status_essencestorm");
        }
        return value;
    }

    public static float IncreaseEvocationDamageFromEmblem(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter ft = originatingActor as Fighter;
        if (ft.myStats.CheckHasStatusName("emblem_spellshaperemblem_tier1_evocation"))
        {
            value *= 1.12f;
        }

        return value;
    }

    public static float GodspeedDaggerStrike(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter atk = originatingActor as Fighter;
        if (atk.lastActorAttacked == target)
        {
            atk.consecutiveAttacksOnLastActor++;
        }
        else
        {
            atk.lastActorAttacked = target;
            atk.consecutiveAttacksOnLastActor = 1;
        }

        value = CombatManagerScript.BoostDamageWithDagger(atk, value);

        return value;
    }
    public static float LavaBurns(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (target.actorfaction == Faction.ENEMY)
        {
            value *= 0.4f;
            if (target.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = target as Monster;
                if ((mn.isBoss) || (mn.isChampion) || (mn.isItemBoss))
                {
                    value *= 0.5f;
                }
            }
        }
        return value;
    }

    public static float IncreaseHeavySmashDamage(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emeraldsetbonus2")) 
        {
            value *= 3f;
        }

        return value;
    }

    public static float BlackHoleExtraEffect(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (target.GetPos() == parent.destructibleOwnerOfEffect.GetPos())
        {
            // Root if we're on same tile and it's been 2 turns or less since previous root
            int turnsSinceRoot = 999;
            bool hasEffect = target.effectsInflictedOnTurn.TryGetValue("blackhole", out turnsSinceRoot);
            if (turnsSinceRoot >= 2 || !hasEffect)
            {
                target.myStats.AddStatusByRefAndLog("status_rooted", originatingActor, 1);
                if (hasEffect)
                {
                    target.SetEffectInflicted("blackhole", 0);
                }
                else
                {
                    target.AddEffectInflicted("blackhole", 0);
                }
            }
            CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "FervirDebuff", null, true);
        }
        else if (MapMasterScript.GetGridDistance(target.GetPos(), parent.destructibleOwnerOfEffect.GetPos()) == 1)
        {
            if (!target.CheckIfActorCanBeMoved(true))
            {
                return value;
            }
            
            // If we're one away, pull toward the black hole center
            Vector2 movePoint = parent.destructibleOwnerOfEffect.GetPos();

            // Make sure the middle tile has no other collidable actors.            
            MapTileData checkMTD = MapMasterScript.GetTile(parent.destructibleOwnerOfEffect.GetPos());
            if (checkMTD.IsCollidableEvenWithBreakable(target))
            {
                return value;
            }

            MapMasterScript.singletonMMS.MoveAndProcessActor(target.GetPos(), movePoint, target);
            target.myMovable.AnimateSetPosition(movePoint, 0.1f, false, 0f, 0f, MovementTypes.LERP);
            CombatManagerScript.SpawnChildSprite("AggroEffect", target, Directions.NORTHWEST, false);
            if (target.GetActorType() != ActorTypes.HERO || GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
            {
                target.myAnimatable.SetAnim(target.myAnimatable.defaultTakeDamageAnimationName);
            }

            BattleTextManager.NewText(StringManager.GetString("misc_pulled"), target.GetObject(), Color.yellow, 1.2f);

            //only move the camera if the hero is the move target
            if (target.GetActorType() == ActorTypes.HERO)
            {
                CameraController.UpdateCameraPosition(movePoint, true);
            }
        }

        return value;
    }

    public static float TornadoPushToSummoner(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        bool bTargetIsHero = target == GameMasterScript.heroPCActor;

        if (bTargetIsHero)
        {
            if (!target.CheckIfActorCanBeMoved(true))
            {
                return 0f;
            }
        }

        Vector2 summonerPos = originatingActor.GetPos();

        int distance = MapMasterScript.GetGridDistance(target.GetPos(), summonerPos);

        Vector2 movePoint = Vector2.zero;

        if (distance > 1)
        {
            CustomAlgorithms.GetPointsOnLineNoGarbage(target.GetPos(), summonerPos);
            int maxMoveDistance = 2;
            for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
            {
                if (maxMoveDistance == 0) break;
                if (CustomAlgorithms.pointsOnLine[i] == target.GetPos()) continue;
                MapTileData mtd = MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]);
                if (mtd.IsCollidable(originatingActor)) continue;

                if (!MapMasterScript.CheckTileToTileLOS(target.GetPos(), mtd.pos, target, MapMasterScript.activeMap))
                {
                    break;
                }

                movePoint = CustomAlgorithms.pointsOnLine[i];
                maxMoveDistance--;
            }
            if (movePoint == Vector2.zero) return 0f;
            if (movePoint == target.GetPos()) return 0f;
            
            // Make sure we're not pushing or pulling through walls.            

            MapMasterScript.singletonMMS.MoveAndProcessActor(target.GetPos(), movePoint, target);
            target.myMovable.AnimateSetPosition(movePoint, 0.1f, false, 0f, 0f, MovementTypes.LERP);
            CombatManagerScript.SpawnChildSprite("AggroEffect", target, Directions.NORTHWEST, false);
            if (!bTargetIsHero || GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
            {
                target.myAnimatable.SetAnim(target.myAnimatable.defaultTakeDamageAnimationName);
            }

            BattleTextManager.NewText(StringManager.GetString("misc_pulled"), target.GetObject(), Color.yellow, 1.2f);
            //target.UpdateSpriteOrder(movePoint);

            //only move the camera if the hero is the move target
            if (bTargetIsHero)
            {
                CameraController.UpdateCameraPosition(movePoint, true);
            }
        }
        else
        {
            value = 0f;
        }

        return value;
    }

    public static float RandomElementalDamage(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int randomElement = UnityEngine.Random.Range(0, (int)DamageTypes.COUNT);

        parent.damType = (DamageTypes)randomElement;

        return value;
    }

    public static float FuriousCrescendo(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (originatingActor.GetActorType() == ActorTypes.HERO)
        {
            int songLevel = GameMasterScript.gmsSingleton.ReadTempGameData("buffer_thanesong_level");
            float bonus = (songLevel * 0.35f) * value;
            value += bonus;
        }

        return value;
    }

    public static float AetherBarrageScaling(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int numSoulkeeperSummons = 0;
        if (GameMasterScript.heroPCActor.summonedActors != null)
        {
            foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
            {
                if (act.GetActorType() != ActorTypes.MONSTER) continue;

                Monster mn = act as Monster;
                if (mn.CheckAttribute(MonsterAttributes.SOULKEEPER_SUMMON) == 100)
                {
                    numSoulkeeperSummons++;
                }
            }
        }

        value += (value * 0.2f * numSoulkeeperSummons);

        return value;
    }

    public static float GilToss(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int money = GameMasterScript.heroPCActor.GetMoney();
        if (money < value)
        {
            value = money;
        }
        GameMasterScript.heroPCActor.ChangeMoney(-1 * (int)value);
        StringManager.SetTag(0, ((int)(value)).ToString());
        GameLogScript.LogWriteStringRef("log_throw_gold");
        value /= numActorsToProcess;

        return value;
    }

    public static float StaffMastery1(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter origFighter = originatingActor as Fighter;
        float energyMultiplier = 1f + ((1f - origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.ENERGY)) / 2f);
        value *= energyMultiplier;
        return value;
    }

    public static float GamblerDarts(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (GameMasterScript.heroPCActor.myEquipment.HasEquipmentByRef("weapon_leg_playingcards"))
        {
            value *= 1.35f;
        }
        return value;        
    }

    public static float GamblerDartsAlternate(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {


        if (GameMasterScript.heroPCActor.myEquipment.HasEquipmentByRef("weapon_leg_playingcards"))
        {
            value *= 1.35f;
        }

        int diceRoll = parent.extraTempData;

        value += (value * diceRoll * 0.125f);

        return value;
    }

    public static float GamblerHighCard(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        float addValue = parent.extraTempData * .1f;
        value *= (1 + addValue);
        if (GameMasterScript.heroPCActor.myEquipment.HasEquipmentByRef("weapon_leg_playingcards"))
        {
            value *= 1.35f;
        }
        return value;
    }

    public static float HighlandChargeAlt(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int songCount = GameMasterScript.heroPCActor.myStats.CountStatusesByFlag(StatusFlags.THANESONG);

        value += value * songCount * 0.3f;

        return value;
    }

    public static float SelectRandomElementAndAnimation(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int randomNumber = UnityEngine.Random.Range((int)DamageTypes.FIRE, (int)DamageTypes.COUNT);
        DamageTypes element = (DamageTypes)randomNumber;

        switch(element)
        {
            case DamageTypes.FIRE:
                parent.spriteEffectRef = "FireBurst";
                break;
            case DamageTypes.LIGHTNING:
                parent.spriteEffectRef = "LightningStrikeEffect";
                break;
            case DamageTypes.WATER:
                parent.spriteEffectRef = "WaterProjectile";
                break;
            case DamageTypes.POISON:
                parent.spriteEffectRef = "AcidSplash";
                break;
            case DamageTypes.SHADOW:
                parent.spriteEffectRef = "ShadowPierceAttack";
                break;
        }

        parent.damType = element;

        return value;
    }

    public static float CloakAndDagger(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (MapMasterScript.activeMap.GetTile(target.GetPos()).GetActorRef("obj_smokecloud") != null)
        {
            value *= 1.2f;
        }
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_brigandbomber") && 
            parent.effectRefName != "shadowbleeddmg")
        {
            GameMasterScript.brigandBomberTemplate.originatingActor = GameMasterScript.heroPCActor;
            GameMasterScript.brigandBomberTemplate.targetActors.Clear();
            GameMasterScript.brigandBomberTemplate.targetActors.Add(target);
            GameMasterScript.brigandBomberTemplate.positions.Clear();
            GameMasterScript.brigandBomberTemplate.positions.Add(GameMasterScript.heroPCActor.GetPos());
            GameMasterScript.brigandBomberTemplate.centerPosition = target.GetPos();
            GameMasterScript.brigandBomberTemplate.DoEffect();
        }
        return value;
    }

    static void CheckForPaladinEmblemBlockAdder()
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_paladinemblem_tier0_block"))
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("status_blockup25", GameMasterScript.heroPCActor, 99);
        }
    }

    public static float BlessedHammer(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        if (parent.destructibleOwnerOfEffect.ReadActorData("empowerhammer") == 1)
        {
            value *= 1.25f;
        }

        CheckForPaladinEmblemBlockAdder();

        return value;
    }

    public static float PaladinEmpower(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter origFighter = originatingActor as Fighter;
        if (origFighter.myStats.CheckHasStatusName("status_empowerpaladin"))
        {
            value *= 1.25f;
            origFighter.myStats.RemoveStatusByRef("status_empowerpaladin");
        }
        CheckForPaladinEmblemBlockAdder();

        return value;
    }

    public static float SmiteEvil2Empower(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        Fighter origFighter = originatingActor as Fighter;
        if (origFighter.myStats.CheckHasStatusName("status_empowerpaladin"))
        {
            value *= 1.25f;
            origFighter.myStats.RemoveStatusByRef("status_empowerpaladin");
        }

        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.refName.Contains("blessedhammer"))
            {
                abil.ChangeCurrentCooldown(-1);
            }
        }
        CheckForPaladinEmblemBlockAdder();

        return value;
    }

    public static float StrikeVitalPoint(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        // Budoka vital point triggers
        parent.anyVitalPoint = EffectScript.EvaluateVitalPointCombo(parent.parentAbility, target, originatingActor as Fighter);        

        return value;
    }

    public static float CallStarshards(Actor originatingActor, Fighter target, float value, int numActorsToProcess, DamageEffect parent)
    {
        int numShards = UnityEngine.Random.Range(2, 5);
        value *= numShards;

        for (int i = 0; i < numShards; i++)
        {
            GameObject shardProjectile = GameMasterScript.TDInstantiate("StarshardEffect");

            if (i == 0)
            {
                CombatManagerScript.FireProjectile(GameMasterScript.heroPCActor.GetPos(), target.GetPos(), shardProjectile, 0.3f, false, target.GetObject(), MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f);
            }
            else
            {
                CombatManagerScript.WaitThenFireProjectile((i * 0.1f), GameMasterScript.heroPCActor.GetPos(), target.GetPos(), shardProjectile, 0.3f, false, target.GetObject(), MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f);
            }

            CombatManagerScript.TryPlayEffectSFX(shardProjectile, target.GetPos(), parent, true);
        }
        
        return value;
    }

}
