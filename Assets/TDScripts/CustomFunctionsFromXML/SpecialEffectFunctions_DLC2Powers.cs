using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SpecialEffectFunctions
{
    static List<string> poolStrings;
    static List<AbilityScript> poolAbilities;
    static List<zirconAnim.AnimationFrameData> afdList;
    static List<Actor> possibleActors;

    public static EffectResultPayload PullTyrantAllies(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        bool anyMoved = false;

        foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorfaction != Faction.ENEMY) continue;
            if (m.isInDeadQueue) continue;

            int dist = MapMasterScript.GetGridDistance(m.GetPos(), effect.originatingActor.GetPos());
            if (dist < 2)
            {
                continue;
            }

            CustomAlgorithms.GetPointsOnLineNoGarbage(m.GetPos(), effect.originatingActor.GetPos());

            Vector2 pos = Vector2.zero;
            bool foundPos = false;

            Debug.Log(dist + " " + CustomAlgorithms.numPointsInLineArray);

            for (int i = 1; i < CustomAlgorithms.numPointsInLineArray; i++)
            {
                MapTileData mtd = MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]);
                if (mtd.IsCollidable(m)) continue;
                pos = mtd.pos;
                foundPos = true;
                if (i >= 3) break;
            }

            if (foundPos)
            {
                MapMasterScript.activeMap.MoveActor(m.GetPos(), pos, m);
                m.myMovable.AnimateSetPosition(pos, 0.12f, false, 0f, 0f, MovementTypes.LERP);                
                erp.waitTime += 0.08f;
                anyMoved = true;
            }
        }

        if (anyMoved)
        {
            BattleTextManager.NewText(StringManager.GetString("help_bt"), effect.originatingActor.GetObject(), Color.red, 0.5f);
        }

        return erp;
    }

    public static EffectResultPayload StripRandomBuffFromDefender(SpecialEffect effect, List<Actor> actorsToProcess)
    {        
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.bufferedCombatData == null)
        {
            return erp;
        }

        if (CombatManagerScript.bufferedCombatData.defender.GetActorType() != ActorTypes.MONSTER)
        {
            return erp;
        }

        poolAbilities.Clear();
        foreach(StatusEffect checkSE in CombatManagerScript.bufferedCombatData.defender.myStats.GetAllStatuses())
        {
            if (!checkSE.isPositive) continue;
            if (checkSE.noRemovalOrImmunity) continue;
            if (checkSE.durStatusTriggers[(int)StatusTrigger.PERMANENT]) continue;
            poolAbilities.Add(checkSE);
        }

        if (poolAbilities.Count == 0)
        {
            return erp;
        }

        string toRemove = poolAbilities.GetRandomElement().refName;

        StatusEffect se = GameMasterScript.masterStatusList[toRemove];

        CombatManagerScript.bufferedCombatData.defender.myStats.RemoveAllStatusByRef(toRemove);
        CombatManagerScript.GenerateSpecificEffectAnimation(CombatManagerScript.bufferedCombatData.defender.GetPos(), "FervirDebuff", effect, true);

        if (!string.IsNullOrEmpty(se.abilityName))
        {
            StringManager.SetTag(0, CombatManagerScript.bufferedCombatData.defender.displayName);
            StringManager.SetTag(1, se.abilityName);
            GameLogScript.LogWriteStringRef("log_buffstrip", GameMasterScript.heroPCActor);
        }

        BattleTextManager.NewText(StringManager.GetString("battletext_nullify"), CombatManagerScript.bufferedCombatData.defender.GetObject(), Color.green, 0.33f);

        erp.waitTime = 0.3f;

        return erp;
    }

    public static EffectResultPayload ReduceRandomCooldownByOne(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        string abilRef = GameMasterScript.gmsSingleton.ReadTempStringData("last_abilityref_used");

        poolAbilities.Clear();

        foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.GetCurCooldownTurns() > 0 && abil.refName != abilRef)
            {
                poolAbilities.Add(abil);
            }
        }

        if (poolAbilities.Count > 0)
        {
            AbilityScript rand = poolAbilities.GetRandomElement();
            rand.ChangeCurrentCooldown(-1);
        }

        return erp;
    }

    public static EffectResultPayload DieOnlyInBlessedPool(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload == null)
        {
            return erp;
        }

        MapTileData mtd = MapMasterScript.GetTile(effect.originatingActor.GetPos());

        bool fatal = false;

        Fighter ft = effect.originatingActor as Fighter;

        if (CombatManagerScript.damagePayload.currentDamageValue >= ft.myStats.GetCurStat(StatTypes.HEALTH))
        {
            fatal = true;
        }

        if (fatal && !mtd.CheckForSpecialMapObjectType(SpecialMapObject.BLESSEDPOOL))
        {
            CombatManagerScript.damagePayload.currentDamageValue = 0f;
            GameMasterScript.gmsSingleton.SetTempFloatData("dmg", 0);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload LoneWolfEnhanceDmg(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload == null)
        {
            return erp;
        }

        if (GameMasterScript.heroPCActor.ReadActorData("any_summoned_creatures") != 1)
        {
            if (CombatManagerScript.damagePayload.atk.GetActorType() == ActorTypes.HERO)
            {
                // boost hero damage, or
                CombatManagerScript.damagePayload.currentDamageValue *= (1f + CombatManagerScript.LONE_WOLF_DMG_MULT);

            }
            else
            {
                // must be taking damage
                CombatManagerScript.damagePayload.currentDamageValue *= (1f + CombatManagerScript.LONE_WOLF_DEF_MULT);
            }

            
        }        

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload ZookeeperReduceDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload == null)
        {
            return erp;
        }

        int numAliveSummonedMonsters = GameMasterScript.heroPCActor.ReadActorData("any_summoned_creatures");
        if (numAliveSummonedMonsters < 0) numAliveSummonedMonsters = 0;
        
            /* int numAliveSummonedMonsters = 0;
        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            if (act.isInDeadQueue) continue;
            Monster m = act as Monster;
            if (!m.myStats.IsAlive()) continue;
            if (m.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID()) continue;
            numAliveSummonedMonsters++;
        } */

        float dmgMultiplier = 1f - (numAliveSummonedMonsters * 0.01f);

        CombatManagerScript.damagePayload.currentDamageValue *= dmgMultiplier;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload EnhanceItemBasedGroundDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;
        if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            DamageEffect de = CombatManagerScript.damagePayload.effParent as DamageEffect;
            if (de != null && de.damageItem)
            {
                if (de.parentAbility != null && de.parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
                {
                    curdamage *= 1.5f;
                }
            }
        }
        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload TrackPrismDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Monster mn = effect.originatingActor as Monster;

        if (!mn.storingTurnData) return erp;

        if (mn.storeTurnData == null) return erp;

        if (mn.storeTurnData.GetTurnType() != TurnTypes.ABILITY) return erp;

        if (mn.storeTurnData.tAbilityToTry.refName != "skill_prismblast") return erp;

        if (CombatManagerScript.damagePayload == null) return erp;

        int damageIndex = (int)CombatManagerScript.damagePayload.damage.damType;

        if (damageIndex > 0)
        {
            string checkStr = DamageModifierFunctions.prismStrings[damageIndex];
            int curValue = mn.ReadActorData(checkStr);
            curValue += (int)CombatManagerScript.damagePayload.currentDamageValue;
            mn.SetActorData(checkStr, curValue);
        }               

        return erp;
    }

    public static EffectResultPayload GhostAuraReduceDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload == null || effect.originatingActor == null)
        {
            return erp;
        }

        MapTileData myTile = MapMasterScript.GetTile(CombatManagerScript.damagePayload.atk.GetPos());

        if (!myTile.HasActorByRef("obj_ghostaura_cloud"))
        {
            return erp;
        }

        StringManager.SetTag(0, effect.originatingActor.displayName);

        if (effect.originatingActor.actorUniqueID != CombatManagerScript.damagePayload.def.actorUniqueID)
        {
            // we must hit the thing that summoned the ghost aura! otherwise, reduce the damage.
            CombatManagerScript.damagePayload.currentDamageValue *= 0.1f;
            if (CombatManagerScript.damagePayload.atk.GetActorType() == ActorTypes.HERO) GameLogScript.LogWriteStringRef("exp_log_ghostaura_wrongtarget");
        }
        /* else
        {
            CombatManagerScript.damagePayload.currentDamageValue *= 0.9f;
            if (CombatManagerScript.damagePayload.atk.GetActorType() == ActorTypes.HERO) GameLogScript.LogWriteStringRef("exp_log_ghostaura_righttarget");
        } */
                        

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload TryCharmCombatCreature(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        Fighter targ = null;

        if (CombatManagerScript.bufferedCombatData.attacker.actorUniqueID == effect.originatingActor.actorUniqueID)
        {
            targ = CombatManagerScript.bufferedCombatData.defender;
        }
        else
        {
            targ = CombatManagerScript.bufferedCombatData.attacker;
        }

        if (targ.GetActorType() != ActorTypes.MONSTER)
        {
            return erp;
        }

        Monster mon = targ as Monster;

        // We can only charm TEMPORARY pets.
        if (mon.actorfaction == Faction.ENEMY || mon.maxTurnsToDisappear == 0 || mon.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            return erp;
        }

        if (mon.actorRefName == "mon_xp_defensecrystal")
        {
            return erp;
        }

        float CHARM_CHANCE = 0.33f;

        if (UnityEngine.Random.Range(0,1f) > CHARM_CHANCE)
        {
            return erp;
        }

        mon.ChangeMyFaction(Faction.ENEMY);
        mon.bufferedFaction = Faction.ENEMY;
        GameMasterScript.heroPCActor.RemoveSummon(mon);
        GameMasterScript.heroPCActor.RemoveAnchor(mon);
        mon.dieWithSummoner = false;
        mon.actOnlyWithSummoner = false;
        mon.summoner = null;

        Fighter origFT = effect.originatingActor as Fighter;
        //origFT.AddSummon(mon);

        StringManager.SetTag(0, origFT.displayName);
        StringManager.SetTag(1, mon.displayName);
        GameLogScript.LogWriteStringRef("misc_log_psychic_charmed");
        BattleTextManager.NewText(StringManager.GetExcitedString("status_status_charmed_name"), mon.GetObject(), Color.red, 1.0f);

        CombatManagerScript.GenerateSpecificEffectAnimation(mon.GetPos(), "MindControl", effect, true);

        erp.waitTime = 0.15f;

        return erp;
    }

    public static EffectResultPayload CheckForPlaneShift(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        int turnsLeft = effect.originatingActor.ReadActorData("planeshift");
        if (turnsLeft == -1)
        {
            turnsLeft = 1;
        }
        turnsLeft--;

        bool triggerEffect = false;

        if (turnsLeft <= 0)
        {
            turnsLeft = 6;
            triggerEffect = true;
        }

        effect.originatingActor.SetActorData("planeshift", turnsLeft);

        bool newFormIsPhysical = false;

        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess.Add(effect.originatingActor);

        if (triggerEffect)
        {
            Fighter ft = effect.originatingActor as Fighter;
            bool hasPhysical = ft.myStats.CheckHasStatusName("spiritdragon_physical");
            bool hasSpirit = ft.myStats.CheckHasStatusName("spiritdragon_spirit");

            string newDataKey = "physicalform";
            string oldDataKey = "spiritform";

            newFormIsPhysical = true;
            
            if (hasPhysical)
            {
                // remove physical, add spirit
                ft.myStats.RemoveStatusByRef("spiritdragon_physical");
                ft.myStats.RemoveStatusByRef("elementaldebuffattack");
                ft.myStats.AddStatusByRefAndLog("spiritdragon_spirit", ft, 99);

                newDataKey = "spiritform";
                oldDataKey = "physicalform";

                newFormIsPhysical = false;
            }
            else
            {
                // if we have spirit but not physical, or neither, then we just switch to physical
                ft.myStats.RemoveStatusByRef("spiritdragon_spirit");
                ft.myStats.AddStatusByRef("elementaldebuffattack", ft, 99, false);
                ft.myStats.AddStatusByRefAndLog("spiritdragon_physical", ft, 99);
            }

            // swap our statuses, of course

            ft.SetActorData(newDataKey, 1);
            ft.SetActorData(oldDataKey, 0);

            CombatManagerScript.GenerateSpecificEffectAnimation(ft.GetPos(), "FervirBuff", effect, true);
            erp.waitTime = 0.15f;

            SpiritDragonStuff.ChangeDragonAnimationByForm(ft, newFormIsPhysical);
        }

        return erp;
    }
    
}