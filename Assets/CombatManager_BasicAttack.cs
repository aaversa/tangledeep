using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CombatManagerScript
{
    
    public static CombatResultPayload Attack(Fighter atk, Fighter def)
    {
        bool attackerIsHero = atk.GetActorType() == ActorTypes.HERO;

        accumulatedCombatWaitTime = 0.0f;
        //bufferedCombatData.counterAttack = false; // What was this for?

        GameObject attacker = atk.GetObject();
        GameObject defender = def.GetObject();
        StatBlock attackerStats = atk.myStats;
        StatBlock defenderStats = def.myStats;
        EquipmentBlock atkEquip = atk.myEquipment;
        EquipmentBlock defEquip = def.myEquipment;

        if (attackerIsHero || atk.actorRefName == "mon_nightmareking")
        {

            int hpToSet = (int)(def.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) * 100f);
            GameMasterScript.gmsSingleton.SetTempGameData(def.actorUniqueID.ToString() + "hp_preatk", hpToSet);
            atk.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(GetAngleBetweenPoints(atk.GetPos(), def.GetPos())));
            atk.myAnimatable.SetAnimDirectional("Attack", atk.lastMovedDirection, atk.lastCardinalDirection);
            atk.myStats.UpdateStatusDirections();
            
            //Shara weapon swing if the weapon is not ranged and not default
            if (attackerIsHero && (atk.myJob.jobEnum == CharacterJobs.SHARA || atk.myJob.jobEnum == CharacterJobs.MIRAISHARA))
            {
                var attackWeapon = atk.myEquipment.GetWeapon();
                if (attackWeapon != null &&
                    !atk.myEquipment.IsDefaultWeapon(attackWeapon) &&
                    !atk.myEquipment.IsWeaponRanged(attackWeapon))
                {
                    var weaponTK = atk.GetObject().GetComponentInChildren<SharaWeaponTKEffect>();
                    if (weaponTK != null)
                    {
                        weaponTK.Activate(atk.lastMovedDirection);
                    }
                }
            }
        }
        else
        {
            atk.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(GetAngleBetweenPoints(atk.GetPos(), def.GetPos())));
            atk.myStats.UpdateStatusDirections();
        }

        atk.ResetTurnsSinceLastCombatAction();
        def.ResetTurnsSinceLastCombatAction();

        if (atk.lastActorAttacked != def)
        {
            atk.consecutiveAttacksOnLastActor = 0;
        }
        else
        {
            atk.consecutiveAttacksOnLastActor++;
        }
        atk.lastActorAttacked = def;
        
        // Calculate number of attacks.
        int numAttacks = 1;

        bool validOffhandAttack = false;

        Weapon offhandWeapon = atk.myEquipment.GetOffhandWeapon();
        Weapon mainhandWeapon = atk.myEquipment.GetWeapon();
        Equipment offhandItem = atk.myEquipment.GetOffhandItem();
               
        bool offhandWeaponIsNull = offhandWeapon == null;
        bool usingFistsOnly = attackerIsHero && (mainhandWeapon == null 
            || atk.myEquipment.IsDefaultWeapon(mainhandWeapon) || mainhandWeapon.ReadActorData("monkweapon") == 1);

        if ((!offhandWeaponIsNull && offhandWeapon.ReadActorData("monkweapon") != 1) || // We have an offhand weapon, or
            ((usingFistsOnly && atk.myStats.CheckHasStatusName("status_unarmedfighting1") && // We are fighting with fists AND
            (offhandWeaponIsNull || offhandItem.actorRefName == "offhand_leg_ascetic_wrap" 
            || offhandItem.HasModByRef("mm_budokavalid") || offhandItem.HasModByRef("mm_asceticgrab") ||
            offhandWeapon.ReadActorData("monkweapon") == 1)) // We have nothing, or Ascetic wrap, in offhand
            ))
        {
            validOffhandAttack = true;
        }

        if (validOffhandAttack && (MapMasterScript.GetGridDistance(atk.GetPos(), def.GetPos()) <= 1f))
        {
            if (offhandWeapon != atk.myEquipment.GetWeapon())
            {
                numAttacks++;
            }
        }

        if (staticBonusCreated == false)
        {
            staticBonusAttackPackage = new BonusAttackPackage();
            staticBonusCreated = true;
        }
        else
        {
            staticBonusAttackPackage.Clear();
        }

        CombatResult totalResult = CombatResult.NOTHING;
        CombatResultPayload crp = new CombatResultPayload();
        bool offhand = false;

        float addToWaitTime = 0.0f;

        bool extraAttackFromSongOfMight = false;
        bool extraAttackFromEchoing = false;

        if (attackerIsHero)
        {
            if (UnityEngine.Random.Range(0, 1f) <= PROC_SONG_MIGHT_LEVEL3)
            {
                if (atk.myStats.CheckHasStatusName("song_might_3") || atk.myStats.CheckHasStatusName("song_might_3_songblade"))
                {
                    extraAttackFromSongOfMight = true;
                    numAttacks++;
                    GameLogScript.LogWriteStringRef("log_songmight_extra_attack");
                }
            }
            if (UnityEngine.Random.Range(0,1f) <= CHANCE_ECHOING_EXTRA_ATTACK && atk.myStats.CheckHasActiveStatusName("xp2_echoingattack"))
            {
                extraAttackFromEchoing = true;
                BattleTextManager.NewText(StringManager.GetString("mm_xp2_echoing_name"), atk.GetObject(), Color.green, 0.1f);
            }
        }

        GameMasterScript.gmsSingleton.SetTempGameData("enemiesattacked", 1);

        for (int i = 0; i < numAttacks; i++)
        {
            // Song of might, no offhand: 2 attacks, when i == 1, use mainhand
            // Song of might, WITH offhand: 2 attacks, when i == 1, use offhand, but when i == 2, use mainhand

            if (extraAttackFromEchoing)
            {
                extraAttackFromEchoing = false;
                i--;
            }

            if (extraAttackFromSongOfMight)
            {
                if (numAttacks == 2) // Not dual wielding
                {
                    if (i == 1)
                    {
                        offhand = false;
                    }
                }
                else if (numAttacks == 3) // Dual wielding
                {
                    if (i == 1)
                    {
                        offhand = true;
                    }
                    else if (i == 2)
                    {
                        offhand = false;
                    }
                }
            }
            else
            {
                if (i == 1)
                {
                    offhand = true;
                }
            }

            CombatResultPayload thisAttackResult = ExecuteAttack(atk, def, offhand, i, false);           
            // Legendary harp code
            if (attackerIsHero)
            {
                bool havocHarp = atk.myEquipment.GetWeaponMod("mm_havocharp");
                bool twoArrows = atk.myEquipment.GetWeaponMod("mm_twoarrows");

                if (havocHarp || twoArrows)
                {
                    List<Actor> nearby = MapMasterScript.GetAllTargetableInTiles(MapMasterScript.activeMap.GetListOfTilesAroundPoint(def.GetPos(), 2));
                    nearby.Remove(atk);
                    nearby.Remove(def);

                    float bonusAttackChance = havocHarp ? CHANCE_LEGHARP_EXTRA_ATTACK : BASE_CHANCE_TWOARROWS_ATTACK;

                    List<Actor> remove = new List<Actor>();
                    foreach (Actor act in nearby)
                    {
                        if (act.actorfaction == Faction.PLAYER)
                        {
                            remove.Add(act);
                        }
                    }
                    foreach (Actor act in remove)
                    {
                        nearby.Remove(act);
                    }

                    nearby.Shuffle();
                    if (UnityEngine.Random.Range(0, 1f) <= bonusAttackChance && nearby.Count > 0)
                    {
                        Monster mn = nearby[0] as Monster;
                        ExecuteAttackWrapperWithAnimationAndDeadQueue(atk, mn, false, 0, false);
                        nearby.Remove(mn);

                        float thirdAttackChance = havocHarp ? CHANCE_LEGHARP_EXTRA_ATTACK : BASE_CHANCE_THREEARROWS_ATTACK;

                        if (nearby.Count > 0 && UnityEngine.Random.Range(0, 1f) <= thirdAttackChance)
                        {
                            mn = nearby[0] as Monster;
                            ExecuteAttackWrapperWithAnimationAndDeadQueue(atk, mn, false, 0, false);
                            nearby.Remove(mn);
                        }

                        addToWaitTime += staticBonusAttackPackage.GetWaitTimeThenSetToZero();
                    }
                }
            }
            // End legendary harp code

            // Axes hit everything nearby. Even offhand?

            WeaponTypes wType = mainhandWeapon.weaponType;

            if (offhand)
            {
                wType = atk.myEquipment.GetOffhandWeaponType();
            }

            bool anyMonstersDiedToAxe = false;            

            if (wType == WeaponTypes.SPEAR)
            {
                SpearMastery2AttackLogic(atk, def);
                addToWaitTime += staticBonusAttackPackage.GetWaitTimeThenSetToZero();
            }

            if (wType == WeaponTypes.AXE || wType == WeaponTypes.WHIP || (atk.GetActorType() == ActorTypes.HERO && atk.myStats.CheckHasStatusName("status_axestyle"))) // No longer hero only
            {
                AxeOrWhipAttackLogic(atk, def, wType);
                if (!anyMonstersDiedToAxe)
                {
                    anyMonstersDiedToAxe = staticBonusAttackPackage.anyMonstersDiedToAxe;
                }
                addToWaitTime += staticBonusAttackPackage.GetWaitTimeThenSetToZero();
            }

            if (anyMonstersDiedToAxe && atk.GetActorType() == ActorTypes.HERO && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("axemastery3"))
            {
                AxeMastery3Logic(atk, def);
                addToWaitTime += staticBonusAttackPackage.GetWaitTimeThenSetToZero();
            }

            if (def.myStats.IsAlive() && def.objectSet && def.myMovable != null)
            {
                defender.GetComponent<Movable>().Jitter(0.1f);
            }

            bufferedCombatData.numAttacks = i + 1;
            if (thisAttackResult.result == CombatResult.MONSTERDIED || thisAttackResult.result == CombatResult.PLAYERDIED)
            {
                totalResult = thisAttackResult.result;
                break;
            }
        }

        if (atk.myStats.IsAlive() && atk.myMovable != null)
        {
            float atkAngle = GetAngleBetweenPoints(atk.GetPos(), def.GetPos());
            atk.myMovable.Jab(MapMasterScript.GetDirectionFromAngle(atkAngle));
        }

        //bufferedCombatData.ResetData();
        crp.result = totalResult;
        crp.waitTime = bufferedCombatData.addToWaitTime;
        accumulatedCombatWaitTime += crp.waitTime;

        if (attackerIsHero)
        {
            GameMasterScript.heroPCActor.lastMainhandWeaponIDAttackedWith = atkEquip.GetWeapon().actorUniqueID;
            if (!def.myStats.IsAlive())
            {
                if (atk.myEquipment.GetWeaponType() == WeaponTypes.NATURAL)
                {
                    int parsedHP = GameMasterScript.gmsSingleton.ReadTempGameData(def.actorUniqueID + "hp_preatk");
                    if (parsedHP >= 99)
                    {
                        // ONE PUNCH
                        GameMasterScript.gmsSingleton.statsAndAchievements.MonsterPunchedOut();
                    }
                }
                CheckGaelmyddAxeProc(def);
            }
        }

        if (atk.IsHero()) 
        {
            if (mainhandWeapon != null) GameMasterScript.heroPCActor.lastWeaponTypeUsed = mainhandWeapon.weaponType;
            else GameMasterScript.heroPCActor.lastWeaponTypeUsed = WeaponTypes.NATURAL;
        }

        return crp;
    }

}
