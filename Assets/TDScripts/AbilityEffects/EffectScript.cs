using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Linq;

public enum EffectType { BLANK, CHANGESTAT, MOVEACTOR, ADDSTATUS, DAMAGE, EMPOWERATTACK, ATTACKREACTION, INFLUENCETURN, REMOVESTATUS, ALTERBATTLEDATA, SUMMONACTOR, DESTROYACTOR, DESTROYTILE, SPECIAL, ABILITYCOSTMODIFIER, SPELLSHAPE, IMMUNESTATUS, COUNT }
public enum TargetActorType { ORIGINATING, SELF, SINGLE, ALL, ATTACKER, ADJACENT, DEFENDER, LOCAL, CRYSTAL, RANDOMNEARBY,
    MULTIPLERANDOMNEARBY, LASTTARGET, RANDOMFROMALL, ADJACENT_NOT_CENTER, ALL_EXCLUDE_HERO, LAST_ACTOR_BUFFER, ADJACENT_ORIGINATING,
    LOCAL_HEROONLY, HERO_ANYWHERE, ALL_EXCLUDE_HERO_ANDSELF, CORRALPET, DEFENDER_AND_ADJACENT, ADJACENT_NOT_CENTERTARGET, COUNT
}
public enum AttackConditions { ANY, DEFENDER_SAMEANGLE, DEFENDER_REVERSEANGLE, SAMEACTOR, PARENTABILITYANGLE, OPPOSITE_PARENTABILITYANGLE, WEAPON_BLADES, WEAPON_PIERCE, WEAPON_SLASH, WEAPON_BLUNT,
    DEFENDER_DIFFERENTANGLE, DEFENDER_NOT_TARGETING, MELEE, NOTMELEE, NOT_OPPOSITE_PARENTABILITYANGLE, DEFENDER_BELOWHEALTH, DEFENDER_ABOVEHEALTH, ATTACKER_BELOWHEALTH, ATTACKER_ABOVEHEALTH,
    ATTACKER_CHAMPION, DEFENDER_CHAMPION, ATTACKER_RANGEDWEAPON, ATTACKER_MELEEWEAPON, ORIG_ABOVEHEALTH, ORIG_BELOWHEALTH, PLAYERONLY, TARGET_STUNNED, TARGET_ISOLATED, PREVIOUS, MAXRANGE,
    TARGET_NOCOMBAT, REQ_ORIG_STATUS, REQ_ORIG_NOSTATUS, CARD_IN_HAND, MONSTER_NOT_WORTHLESS, ATTACKER_DUALWIELD, ADJACENTONLY, ATTACKER_ISMONSTER, ATTACKER_ISMONSTER_NOTPET, TARGET_ISMONSTER, TARGET_WITHIN_TWOTILES,
    ATTACKER_ISHERO, NOT_DAMAGED_TURNS, COUNT
}

public enum ItemWorldAuras { MELEEDAMAGEPLUS50, RANGEDDAMAGEPLUS50, ELEMENTALDAMAGEPLUS50, MONSTERREGEN5, NOCRITICAL, EXPLODEONDEATH, DOUBLECRITICAL, BONUSXP, BONUSJP, BONUSGOLD,
    DOUBLEHEALING, RESOURCEMINUS50, TOUGHMONSTER, MONSTER_CLEARSTATUS, PLAYERSEALED, BLESSEDPOOL, COUNT }

public enum TriggerConditionStates { VALID, INVALID, PASSTHROUGH, COUNT }

public enum EffectTags { EGGHATCH, FOODHEAL, COUNT }

public class EffectScript {

    public EffectType effectType;
    public string effectName;
    public string effectRefName;
    public Actor selfActor;
    public Actor originatingActor;
    public int originatingActorUniqueID;
    public TargetActorType tActorType;
    public Vector2 centerPosition;
    public List<Vector2> positions;
    public List<Actor> targetActors;
    public List<Actor> skipTargetActors;
    public List<CombatResult> results;
    public List<Actor> affectedActors;
    public string spriteEffectRef;
    public float animLength = 0.0f;
    public float delayBeforeAnimStart = 0.0f;
    public AbilityScript parentAbility;
    public bool playAnimationInstant;
    public bool playAnimation;
    public bool rotateAnimToTarget;
    public float procChance;
    public bool silent;
    public bool isProjectile; // affects game mechanics AND animation
    public bool centerSpriteOnOriginatingActor;
    public bool centerSpriteOnMiddlePosition;

    public bool doNotAlterPreviousAffectedActorList;
    public bool noClearPositionsOnRun;
    public float attackerBelowHealth;
    public float attackerAboveHealth;
    public float defenderBelowHealth;
    public float defenderAboveHealth;
    public float origBelowHealth;
    public float origAboveHealth;
    public string reqTargetCondition;
    public string script_processActorsPreEffect;
    public string script_triggerCondition;
    public float chanceToHitSpecificTarget;
    public Faction reqActorFaction;
    public int triggerPerTurns; // Triggers every X turns
    public List<Actor> buildActorsToProcess;// = new List<Actor>(); // New for pooling. Will this work?
    public List<Actor> localTarg;// = new List<Actor>(); // New for pooling. Will this work?
    public List<Actor> removeActors;// = new List<Actor>(); // New for pooling. Will this work?
    public List<MapTileData> adjacentTiles;// new List<MapTileData>();
    public AttackConditions triggerCondition;
    public bool[] switchFlags;
    public int extraTempData; // No need to serialize
    public float extraWaitTime; // no need to serialize
    public int randTargetRange;
    public int processBufferIndex;
    public int adjacentRange;
    public string battleText;    
    public Actor destructibleOwnerOfEffect;

    public string requiredStatusForOrigFighter;
    public int requiredStatusStacks;

    public MovementTypes projectileMovementType;
    public float projectileTossHeight;

    public static string[] itemWorldAuraDescriptions;
    public static Color[] itemWorldAuraColors;
    public static List<Actor> actorsAffectedByAbility;
    public static List<Actor> actorsAffectedByPreviousAbility;
    public static List<MapTileData> nearbyTiles;
    public static List<Actor> pool_actorList;
    public static ExpressionParser fParser;    
    public bool[] effectTags;

    public const float LETHAL_FISTS_PROC_CHANCE = 0.3f;
    public static bool staticInitialized;

    public static Dictionary<string, float> dictEffectData;

    public float[] damageEquationVars;
    public bool anyDamageEquationVars;
    public float effectPower;

    public bool enforceTriggerPerTurns;

    public int minimumTurnsSinceLastDamaged;

    public static void ResetAllVariablesToGameLoad()
    {
        actorsAffectedByAbility = new List<Actor>();
        actorsAffectedByPreviousAbility = new List<Actor>();
        nearbyTiles = new List<MapTileData>();
        pool_actorList = new List<Actor>();
        dictEffectData = new Dictionary<string, float>();
    }

    public static void Initialize()
    {
        actorsAffectedByAbility = new List<Actor>();
        actorsAffectedByPreviousAbility = new List<Actor>();
        nearbyTiles = new List<MapTileData>();
        pool_actorList = new List<Actor>();
        fParser = new ExpressionParser();
        staticInitialized = true;
    }

    public float GetEffectData(string sRef)
    {
        if (dictEffectData == null)
        {
            dictEffectData = new Dictionary<string, float>();
            return 0;
        }
        float value;
        if (dictEffectData.TryGetValue(sRef, out value))
        {
            return value;
        }
        return 0;
    }

    public void AddEffectData(string sRef, float value)
    {
        if (dictEffectData == null)
        {
            dictEffectData = new Dictionary<string, float>();
            return;
        }
        if (dictEffectData.ContainsKey(sRef))
        {
            dictEffectData[sRef] = value;
        }
        else
        {
            dictEffectData.Add(sRef, value);
        }
    }

    public void RemoveEffectData(string sRef)
    {
        if (dictEffectData == null)
        {
            dictEffectData = new Dictionary<string, float>();
            return;
        }
        dictEffectData.Remove(sRef);
    }

    public static float CheckForEffectValueModifier(string effectRefName, Fighter target, float value)
    {
        float valueModifier;
        if (target.cachedBattleData.effectValueModifiers.TryGetValue(effectRefName, out valueModifier))
        {
            if (CustomAlgorithms.CompareFloats(valueModifier, 0f))
            {
                target.cachedBattleData.effectValueModifiers.Remove(effectRefName);
            }
            else
            {
                value *= valueModifier;
            }
        }
        return value;
    }

    public virtual void ResetAccumulatedAmounts()
    {

    }

    public bool EvaluateTriggerCondition(List<Actor> actorsToProcess)
    {
        Fighter fight = null;
        if (originatingActor == null)
        {
            // Then it's probably self actor.
            originatingActor = selfActor;
            if (selfActor == null)
            {
                // Uhhh just don't trigger it then.
                //Debug.Log(effectName + " " + effectRefName + " " + effectType + " had no originating nor self actor");
                return false;
            }
        }
        if (originatingActor.IsFighter())
        {
            fight = originatingActor as Fighter;
        }
        else
        {
            return true;
        }

        bool bufferTurnCounter = false;
        bool removeIfFalse = false;
        int preTurnCounter = 0;

        if (triggerPerTurns > 0 && (effectType == EffectType.SUMMONACTOR || (effectType == EffectType.ADDSTATUS && enforceTriggerPerTurns))) // why was AddStatus here. it shouldnt be
        {
            int turnCheck = 0;
            fight.effectsInflictedOnTurn.TryGetValue(effectRefName, out turnCheck);
            if (fight.effectsInflictedOnTurn.ContainsKey(effectRefName))
            {
                if (turnCheck < triggerPerTurns)
                {
                    return false;
                }
                else
                {
                    //Debug.Log("We are good!");
                    bufferTurnCounter = true;
                    preTurnCounter = turnCheck;
                    fight.SetEffectInflicted(effectRefName, 0);
                }
            }
            else
            {
                removeIfFalse = true;
                bufferTurnCounter = true;
                fight.AddEffectInflicted(effectRefName, 0);
            }            
        }

        //Debug.Log(triggerPerTurns + " " + effectRefName + " " + bufferTurnCounter + " " + preTurnCounter);

        if (!string.IsNullOrEmpty(script_triggerCondition))
        {
            Func<EffectScript, TriggerConditionStates> myFunc;

            TriggerConditionStates triggerValid = TriggerConditionStates.PASSTHROUGH;

            if (GenericTriggerConditionalFunction.dictDelegates.TryGetValue(script_triggerCondition, out myFunc))
            {
                triggerValid = myFunc(this);
            }
            else
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(GenericTriggerConditionalFunction), script_triggerCondition);
                object[] paramList = new object[1];
                paramList[0] = this;
                if (runscript == null)
                {
                    Debug.Log("Uh oh, " + script_triggerCondition + " is null for " + effectRefName + " " + effectName);
                }
                object returnObj = runscript.Invoke(null, paramList);
                if (returnObj == null)
                {
                    Debug.Log("No object for " + script_triggerCondition);
                }
                else
                {
                    triggerValid = (TriggerConditionStates)returnObj;
                }
            }

            switch (triggerValid)
            {
                case TriggerConditionStates.VALID:                    
                    return true;
                case TriggerConditionStates.INVALID:
                    if (bufferTurnCounter)
                    {
                        if (removeIfFalse)
                        {
                            fight.effectsInflictedOnTurn.Remove(effectRefName);
                            fight.effectsInflictedStringKeys.Remove(effectRefName);
                        }
                        else
                        {
                            fight.SetEffectInflicted(effectRefName, preTurnCounter);
                        }                        
                    }
                    return false;
            }

        }

        bool combatDataAvailable = true;
        if (CombatManagerScript.bufferedCombatData == null)
        {
            combatDataAvailable = false;
            //return true; // Should this always be the case?
        }

        AttackConditions checkTriggerCondition = triggerCondition;
        if (effectType == EffectType.ATTACKREACTION && checkTriggerCondition == AttackConditions.ANY)
        {
            AttackReactionEffect are = this as AttackReactionEffect;
            if (are.reactCondition != AttackConditions.ANY) // This effect still uses the deprecated "ReactCondition" field
            {
                checkTriggerCondition = are.reactCondition;
            }
        }

        switch (checkTriggerCondition)
        {
            case AttackConditions.ANY:                
                return true;
            case AttackConditions.TARGET_WITHIN_TWOTILES:
                foreach (Actor act in actorsToProcess)
                {
                    if (MapMasterScript.GetGridDistance(act.GetPos(), originatingActor.GetPos()) > 2)
                    {
                        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                        return false;
                    }
                }
                return true;
            case AttackConditions.ADJACENTONLY:
                foreach(Actor act in actorsToProcess)
                {
                    if (MapMasterScript.GetGridDistance(act.GetPos(), originatingActor.GetPos()) > 1)
                    {
                        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                        return false;
                    }
                }
                return true;
            case AttackConditions.MONSTER_NOT_WORTHLESS:
                if (!combatDataAvailable)
                {
                    return true;
                }
                if (CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster m = CombatManagerScript.bufferedCombatData.attacker as Monster;
                    if (m.GetXPModToPlayer() <= 0.05f)
                    {
                        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                        return false;
                    }
                }
                if (CombatManagerScript.bufferedCombatData.defender.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster m = CombatManagerScript.bufferedCombatData.defender as Monster;
                    if (m.GetXPModToPlayer() <= 0.05f)
                    {
                        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                        return false;
                    }
                }
                return true;
            case AttackConditions.TARGET_ISOLATED:
                List<Actor> nearbyMonsters = MapMasterScript.GetMonstersAroundTile(CombatManagerScript.bufferedCombatData.defender.GetPos());
                if (nearbyMonsters.Count == 0)
                {
                    return true;
                }
                break;
            case AttackConditions.SAMEACTOR:
                if (fight.lastActorAttacked == CombatManagerScript.bufferedCombatData.defender)
                {
                    return true;
                }
                break;
            case AttackConditions.ATTACKER_ISMONSTER:
                if (CombatManagerScript.bufferedCombatData.attacker != null && CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.MONSTER && 
                    CombatManagerScript.bufferedCombatData.attacker.actorUniqueID != CombatManagerScript.bufferedCombatData.defender.actorUniqueID)
                {
                    return true;
                }
                break;

            case AttackConditions.ATTACKER_ISMONSTER_NOTPET:
                if (CombatManagerScript.bufferedCombatData.attacker != null && CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.MONSTER && 
                    CombatManagerScript.bufferedCombatData.attacker.actorUniqueID != CombatManagerScript.bufferedCombatData.defender.actorUniqueID)
                {
                    Monster mn = CombatManagerScript.bufferedCombatData.attacker as Monster;
                    if (mn.tamedMonsterStuff != null) return false;
                    return true;
                }
                break;                
            case AttackConditions.ATTACKER_ISHERO:
                if (CombatManagerScript.bufferedCombatData.attacker != null && CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.HERO)
                {
                    return true;
                }
                break;
            case AttackConditions.TARGET_STUNNED:
                if ((CombatManagerScript.bufferedCombatData.defender != null)
                    && (CombatManagerScript.bufferedCombatData.defender.turnsSinceLastStun <= 2))
                {
                    return true;
                }
                break;
            case AttackConditions.WEAPON_BLADES:
                if (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.DAGGER
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SWORD
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SPEAR
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.CLAW
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.AXE
                    )
                {
                    return true;
                }
                break;
            case AttackConditions.WEAPON_PIERCE:
                if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.DAGGER) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SPEAR))
                {
                    return true;
                }
                break;
            case AttackConditions.WEAPON_SLASH:
                if (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.AXE
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.SWORD
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.CLAW
                    || CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.WHIP)
                {
                    return true;
                }
                break;
            case AttackConditions.WEAPON_BLUNT:
                if ((CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.NATURAL) || (CombatManagerScript.bufferedCombatData.attackerWeapon.weaponType == WeaponTypes.MACE))
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_NOT_TARGETING:
                if (!CombatManagerScript.bufferedCombatData.defender.CheckTarget(originatingActor))
                {
                    return true;
                }
                break;

            case AttackConditions.MAXRANGE:
                int distance = MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos());
                if (distance >= (CombatManagerScript.bufferedCombatData.attacker.GetMaxAttackRange() * 0.9f))
                {
                    return true;
                } 
                break;
            case AttackConditions.PARENTABILITYANGLE:
                if (!combatDataAvailable) return false;
                if ((int)parentAbility.direction >= MapMasterScript.oppositeDirections.Length) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection == parentAbility.direction)
                {
                    return true;
                }
                break;
            case AttackConditions.OPPOSITE_PARENTABILITYANGLE:
                if (!combatDataAvailable) return false;
                if ((int)parentAbility.direction >= MapMasterScript.oppositeDirections.Length) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection == MapMasterScript.oppositeDirections[(int)parentAbility.direction])
                {
                    return true;
                }
                break;
            case AttackConditions.NOT_OPPOSITE_PARENTABILITYANGLE:
                if (!combatDataAvailable) return false;
                if ((int)parentAbility.direction >= MapMasterScript.oppositeDirections.Length) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection != MapMasterScript.oppositeDirections[(int)parentAbility.direction])
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_DIFFERENTANGLE:
                if (!combatDataAvailable) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection != fight.lastDirectionAttackedFrom)
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_SAMEANGLE:
                if (!combatDataAvailable) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection == fight.lastDirectionAttackedFrom)
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_REVERSEANGLE:
                if (!combatDataAvailable) return false;
                if (CombatManagerScript.bufferedCombatData.attackDirection == MapMasterScript.oppositeDirections[(int)fight.lastDirectionAttackedFrom])
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_BELOWHEALTH:
                if (!combatDataAvailable) return false;
                if (CombatManagerScript.bufferedCombatData.defender.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= defenderBelowHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.DEFENDER_ABOVEHEALTH:
                if (!combatDataAvailable) return false;
                if (CombatManagerScript.bufferedCombatData.defender.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > defenderAboveHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.ATTACKER_BELOWHEALTH:
                if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= attackerBelowHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.ATTACKER_ABOVEHEALTH:
                if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) > attackerAboveHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.ORIG_BELOWHEALTH:
                if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= origBelowHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.ORIG_ABOVEHEALTH:
                if (fight.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) >= origAboveHealth)
                {
                    return true;
                }
                break;
            case AttackConditions.MELEE:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) <= 1)
                {
                    return true;
                }
                break;
            case AttackConditions.ATTACKER_DUALWIELD:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.attacker != null)
                {
                    if (CombatManagerScript.bufferedCombatData.attacker.myEquipment.IsDualWielding())
                    {
                        return true;
                    }
                }
                break;
            case AttackConditions.ATTACKER_RANGEDWEAPON:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.attacker != null)
                {
                    if (CombatManagerScript.bufferedCombatData.attacker.myEquipment.IsWeaponRanged(CombatManagerScript.bufferedCombatData.attacker.myEquipment.GetWeapon()))
                    {
                        return true;
                    }
                }
                break;
            case AttackConditions.ATTACKER_MELEEWEAPON:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.attacker != null)
                {
                    if (!CombatManagerScript.bufferedCombatData.attacker.myEquipment.IsWeaponRanged(CombatManagerScript.bufferedCombatData.attackerWeapon))
                    {
                        return true;
                    }
                }
                break;
            case AttackConditions.REQ_ORIG_STATUS:
                //Debug.Log(requiredStatusForOrigFighter + " " + requiredStatusStacks);
                if (fight.myStats.CheckStatusQuantity(requiredStatusForOrigFighter) >= requiredStatusStacks)
                {
                    return true;
                }
                break;
            case AttackConditions.NOT_DAMAGED_TURNS:
                //Debug.Log(GameMasterScript.turnNumber + " " + fight.lastTurnDamaged + " " + minimumTurnsSinceLastDamaged);
                if (GameMasterScript.turnNumber - fight.lastTurnDamaged >= minimumTurnsSinceLastDamaged)
                {
                    if (fight.lastTurnDamaged > 0 && GameMasterScript.turnNumber % 10 == 0 
                        && PlayerOptions.tutorialTips && effectRefName == "petheal_overtime" && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_pethealing"))
                    {
                        Conversation healTut = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_pethealing");
                        UIManagerScript.StartConversation(healTut, DialogType.TUTORIAL, null);
                    }
                    return true;
                }
                break;
            case AttackConditions.REQ_ORIG_NOSTATUS:
                if (fight.myStats.CheckStatusQuantity(requiredStatusForOrigFighter) == 0)
                {
                    return true;
                }
                break;
            case AttackConditions.CARD_IN_HAND:
                if (GameMasterScript.heroPCActor.gamblerHand.Count > 0)
                {
                    return true;
                }
                break;
            case AttackConditions.NOTMELEE:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }

                if (MapMasterScript.GetGridDistance(CombatManagerScript.bufferedCombatData.defender.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos()) > 1)
                {
                    return true;
                }
                break;
            case AttackConditions.ATTACKER_CHAMPION:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.attacker.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mon = CombatManagerScript.bufferedCombatData.attacker as Monster;
                    if (mon.isChampion)
                    {
                        return true;
                    }
                }
                break;

            case AttackConditions.TARGET_NOCOMBAT:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.defender.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mon = CombatManagerScript.bufferedCombatData.attacker as Monster;
                    // Monster is not in combat.
                    if (mon.myBehaviorState != BehaviorState.FIGHT || !mon.CheckTarget(GameMasterScript.heroPCActor))
                    {
                        return true;
                    }
                }
                else
                {
                    if (CombatManagerScript.bufferedCombatData.defender.CheckTarget(CombatManagerScript.bufferedCombatData.attacker))
                    {
                        return true;
                    }
                }
                break;

            case AttackConditions.DEFENDER_CHAMPION:
                if (!combatDataAvailable)
                {
                    if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
                    return false;
                }
                if (CombatManagerScript.bufferedCombatData.defender.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mon = CombatManagerScript.bufferedCombatData.defender as Monster;
                    if (mon.isChampion)
                    {
                        return true;
                    }
                }
                break;
            case AttackConditions.PLAYERONLY:
                if (actorsToProcess.Count > 0)
                {
                    for (int i = 0; i < actorsToProcess.Count; i++)
                    {
                        if (actorsToProcess[i] == GameMasterScript.heroPCActor)
                        {
                            return true;
                        }
                    }
                }
                break;

        }
        
        if (bufferTurnCounter) fight.ResetEffectLogic(effectRefName, preTurnCounter, removeIfFalse);
        return false;
    }






    public EffectScript()
    {
        if (!staticInitialized)
        {
            Initialize();

        }
        adjacentRange = 1;
        effectTags = new bool[(int)EffectTags.COUNT];
        effectRefName = "";
        effectName = "";
        requiredStatusForOrigFighter = "";
        playAnimationInstant = false;
        playAnimation = false;
        targetActors = new List<Actor>();
        skipTargetActors = new List<Actor>();
        affectedActors = new List<Actor>();
        results = new List<CombatResult>();
        positions = new List<Vector2>();
        effectType = EffectType.BLANK;
        chanceToHitSpecificTarget = 1.0f;
        procChance = 1.0f;
        buildActorsToProcess = new List<Actor>(); // New for pooling. Will this work?
        localTarg = new List<Actor>(); // New for pooling. Will this work?
        removeActors = new List<Actor>(); // New for pooling. Will this work?
        adjacentTiles = new List<MapTileData>();
        reqActorFaction = Faction.ANY;
        requiredStatusStacks = 1;
        triggerCondition = AttackConditions.ANY;
        switchFlags = new bool[(int)ActorFlags.COUNT];
        processBufferIndex = 0;
        damageEquationVars = new float[(int)EDamageEquationVars.COUNT];
        if (itemWorldAuraDescriptions == null)
        {
            itemWorldAuraDescriptions = new string[(int)ItemWorldAuras.COUNT];
            itemWorldAuraDescriptions[(int)ItemWorldAuras.DOUBLECRITICAL] = StringManager.GetString("itemdream_aura_doublecritical");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.NOCRITICAL] = StringManager.GetString("itemdream_aura_nocritical");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.MELEEDAMAGEPLUS50] = StringManager.GetString("itemdream_aura_meleeplus50");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.RANGEDDAMAGEPLUS50] = StringManager.GetString("itemdream_aura_rangedplus50");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.ELEMENTALDAMAGEPLUS50] = StringManager.GetString("itemdream_aura_elementalplus50");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.MONSTERREGEN5] = StringManager.GetString("itemdream_aura_monsterregen5");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.PLAYERSEALED] = StringManager.GetString("itemdream_aura_playersealed");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.MONSTER_CLEARSTATUS] = StringManager.GetString("itemdream_aura_monsterclearstatus");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.TOUGHMONSTER] = StringManager.GetString("itemdream_aura_toughmonsters");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.EXPLODEONDEATH] = StringManager.GetString("itemdream_aura_explodeondeath");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.DOUBLEHEALING] = StringManager.GetString("itemdream_aura_doublehealing");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.RESOURCEMINUS50] = StringManager.GetString("itemdream_aura_resourceminus50");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.BONUSGOLD] = StringManager.GetString("itemdream_aura_bonusgold");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.BONUSXP] = StringManager.GetString("itemdream_aura_bonusxp");
            itemWorldAuraDescriptions[(int)ItemWorldAuras.BONUSJP] = StringManager.GetString("itemdream_aura_bonusjp");

            itemWorldAuraColors = new Color[(int)ItemWorldAuras.COUNT];
            itemWorldAuraColors[(int)ItemWorldAuras.DOUBLECRITICAL] = new Color(1f, 0.92f, 0.016f); // Yellow - affects both player/mon
            itemWorldAuraColors[(int)ItemWorldAuras.NOCRITICAL] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.MELEEDAMAGEPLUS50] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.RANGEDDAMAGEPLUS50] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.ELEMENTALDAMAGEPLUS50] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.MONSTERREGEN5] = new Color(1f, 0.05f, 0.05f); // Red - bad for player
            itemWorldAuraColors[(int)ItemWorldAuras.PLAYERSEALED] = new Color(1f, 0.05f, 0.05f); // Red - bad for player
            itemWorldAuraColors[(int)ItemWorldAuras.MONSTER_CLEARSTATUS] = new Color(1f, 0.05f, 0.05f); // Red - bad for player
            itemWorldAuraColors[(int)ItemWorldAuras.TOUGHMONSTER] = new Color(1f, 0.05f, 0.05f); // Red - bad for player
            itemWorldAuraColors[(int)ItemWorldAuras.EXPLODEONDEATH] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.DOUBLEHEALING] = new Color(1f, 0.92f, 0.016f);
            itemWorldAuraColors[(int)ItemWorldAuras.RESOURCEMINUS50] = new Color(0f, 1f, 0f); // Good for player!
            itemWorldAuraColors[(int)ItemWorldAuras.BONUSGOLD] = new Color(0f, 1f, 0f);
            itemWorldAuraColors[(int)ItemWorldAuras.BONUSXP] = new Color(0f, 1f, 0f);
            itemWorldAuraColors[(int)ItemWorldAuras.BONUSJP] = new Color(0f, 1f, 0f);
            itemWorldAuraColors[(int)ItemWorldAuras.BLESSEDPOOL] = new Color(0.7f, 1f, 1f); // Good for player!
        }
    }

    // This is for OUR purposes, to see if two effects are functionally identical. Allows us to clear out unused/redundant XML data
    public virtual bool CompareToEffect(EffectScript eff)
    {
        if (effectType != eff.effectType) return false;
        if (tActorType != eff.tActorType) return false;
        if (playAnimation != eff.playAnimation) return false;
        if (playAnimation)
        {
            if (spriteEffectRef != eff.spriteEffectRef) return false;
            if (animLength != eff.animLength) return false;
        }
        if (rotateAnimToTarget != eff.rotateAnimToTarget) return false;
        if (procChance != eff.procChance) return false;
        if (silent != eff.silent) return false;
        if (centerSpriteOnMiddlePosition != eff.centerSpriteOnMiddlePosition) return false;
        if (centerSpriteOnOriginatingActor != eff.centerSpriteOnOriginatingActor) return false;
        if (noClearPositionsOnRun != eff.noClearPositionsOnRun) return false;
        if (attackerBelowHealth != eff.attackerBelowHealth) return false;
        if (attackerAboveHealth != eff.attackerAboveHealth) return false;
        if (defenderAboveHealth != eff.defenderAboveHealth) return false;
        if (defenderBelowHealth != eff.defenderBelowHealth) return false;
        if (origBelowHealth != eff.origBelowHealth) return false;
        if (origAboveHealth != eff.origAboveHealth) return false;
        if (reqTargetCondition != eff.reqTargetCondition) return false;
        if (reqActorFaction != eff.reqActorFaction) return false;
        if (triggerCondition != eff.triggerCondition) return false;
        if (triggerPerTurns != eff.triggerPerTurns) return false;
        if (extraTempData != eff.extraTempData) return false;
        if (chanceToHitSpecificTarget != eff.chanceToHitSpecificTarget) return false;
        for (int i = 0; i < switchFlags.Length; i++)
        {
            if (switchFlags[i] != eff.switchFlags[i]) return false;
        }
        if (randTargetRange != eff.randTargetRange) return false;
        if (battleText != eff.battleText) return false;
        for (int i = 0; i < effectTags.Length; i++)
        {
            if (effectTags[i] != eff.effectTags[i]) return false;
        }
        if (processBufferIndex != eff.processBufferIndex) return false;

        if ((triggerPerTurns > 0) || (eff.triggerPerTurns > 0)) return false;

        return true;
    }

    //returns false if the node was not parsed
    public virtual bool ReadNextNodeFromXML(XmlReader r)
    {
        Debug.LogError("BAD " + r.NodeType + " " + r.Name);
        return false;
    }

    public void TryBattleText(Actor act, bool positive)
    {
        Color c;
        if (positive)
        {
            c = Color.green;
        }
        else
        {
            c = Color.red;
        }
        if (!String.IsNullOrEmpty(battleText))
        {
            BattleTextManager.NewText(battleText, act.GetObject(), c, 0f);
        }
    }

    public EffectResultPayload CheckForPreProcessFunction(List<Actor> actorsToProcess, float addWaitTime)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess = actorsToProcess;
        if (!string.IsNullOrEmpty(script_processActorsPreEffect))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(GenericPreEffectFunctions), script_processActorsPreEffect);
            object[] paramList = new object[2];
            paramList[0] = this;
            paramList[1] = actorsToProcess;
            object returnObj = runscript.Invoke(null, paramList);
            if (returnObj == null)
            {
                Debug.Log("No object for " + script_processActorsPreEffect);
            }

            erp = returnObj as EffectResultPayload;

            addWaitTime += erp.waitTime;
            actorsToProcess = erp.actorsToProcess;
        }
        return erp;
    }

    public void CopyLiveData(EffectScript eff)
    {
        originatingActor = eff.originatingActor;
        targetActors = eff.targetActors;
        selfActor = eff.selfActor;
        centerPosition = eff.centerPosition;
        positions = eff.positions;
        parentAbility = eff.parentAbility;
        procChance = eff.procChance;
        chanceToHitSpecificTarget = eff.chanceToHitSpecificTarget;
    }

    public static bool EvaluateVitalPointCombo(AbilityScript parent, Fighter fight, Fighter origFighter)
    {
        // Worsens vital point and can combo
        bool anyVitalPoint = false;
        if (fight.myStats.CheckHasStatusName("ppbleed"))
        {
            StatusEffect curBleed = fight.myStats.GetStatusByRef("ppbleed");
            curBleed.curDuration = curBleed.maxDuration;
            EffectScript spreader = GameMasterScript.GetEffectByRef("spreadppbleed");
            spreader.parentAbility = parent;
            spreader.originatingActor = origFighter;
            spreader.selfActor = fight;
            spreader.positions.Add(fight.GetPos());
            spreader.centerPosition = origFighter.GetPos();
            //addWaitTime += spreader.DoEffect();
            spreader.DoEffect();
            anyVitalPoint = true;
        }
        if (fight.myStats.CheckHasStatusName("status_painenhanced"))
        {
            fight.myStats.AddStatusByRef("status_confused50", origFighter, 4);
            anyVitalPoint = true;
        }
        if (fight.myStats.CheckHasStatusName("status_ppexplode"))
        {
            fight.myStats.AddStatusByRef("status_rooted", origFighter, 6);
            anyVitalPoint = true;
        }
        return anyVitalPoint;
    }

    public bool IsFactionValid(Actor target)
    {        
        if (reqActorFaction == Faction.NONE || reqActorFaction == Faction.ANY)
        {
            return true;
        }

        if (reqActorFaction == Faction.MYFACTION)
        {
            if (originatingActor.actorfaction == target.actorfaction || GameStartData.CheckGameModifier(GameModifiers.FRIENDLY_FIRE))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        if (reqActorFaction == Faction.NOTMYFACTION)
        {
            if (originatingActor.actorfaction != target.actorfaction || (GameStartData.CheckGameModifier(GameModifiers.FRIENDLY_FIRE) && target != originatingActor))
            {
                return true;
            }
            if (originatingActor.actorfaction == Faction.PLAYER && originatingActor.IsHero() && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_confused50"))
            {                
                return true;
            }
            else
            {
                return false;
            }
        }

        if (reqActorFaction == Faction.HERO_ONLY)
        {
            if (target.GetActorType() != ActorTypes.HERO)
            {
                return false;
            }
            return true;
        }

        if (reqActorFaction == Faction.NOTMYFACTION_NOHERO)
        {
            if (originatingActor.actorfaction != target.actorfaction || (GameStartData.CheckGameModifier(GameModifiers.FRIENDLY_FIRE) && target != originatingActor))
            {
                if (target.GetActorType() == ActorTypes.HERO) return false;
                return true;
            }
            if (originatingActor.actorfaction == Faction.PLAYER && originatingActor.IsHero() && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_confused50"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        if (reqActorFaction == Faction.PLAYER)
        {
            if (target.actorfaction == Faction.PLAYER)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        if (reqActorFaction == Faction.ENEMY)
        {
            if (target.actorfaction == Faction.DUNGEON)
            {
                if (target.actorfaction != originatingActor.actorfaction)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (target.actorfaction == Faction.ENEMY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        if (reqActorFaction == Faction.DUNGEON)
        {
            if (target.actorfaction == Faction.DUNGEON)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public List<Actor> GetTargetActorsAndUpdateBuildActorsToProcess(int indexOfEffect = 0)
    {
        buildActorsToProcess.Clear();
        Actor act;

        bool hasOriginatingActor = originatingActor != null;
        if (originatingActorUniqueID == 1 && !hasOriginatingActor)
        {
            originatingActor = GameMasterScript.heroPCActor;
        }

        if (selfActor == null)
        {
            selfActor = originatingActor;
            if (!hasOriginatingActor) // self AND originating are null? welp....
            {
                actorsAffectedByAbility.Clear();
                Debug.Log("welp");
                return buildActorsToProcess;
            }
        }

        //Debug.Log("Clearing actors to process. Let's see. " + effectRefName + " " + tActorType + " prev count: " + actorsAffectedByPreviousAbility.Count + " affected " + actorsAffectedByAbility.Count);

        // This may break....... everything.
        if (!doNotAlterPreviousAffectedActorList)
        {
            actorsAffectedByPreviousAbility.Clear();
        }
        
        foreach (Actor prevAct in actorsAffectedByAbility) 
        {
            if (actorsAffectedByAbility.Count > 30) break; // stupid hack to prevent infinite loops
            //Debug.Log("Affected most recently: " + prevAct.actorRefName);
            actorsAffectedByPreviousAbility.Add(prevAct);
        }

        //Debug.LogError(effectRefName + " " + tActorType + " " + parentAbility.myID);        

        switch (tActorType)
        {
            case TargetActorType.LASTTARGET:
                actorsAffectedByPreviousAbility.Remove(originatingActor);
                if (actorsAffectedByPreviousAbility.Count > 0)
                {
                    buildActorsToProcess.Add(actorsAffectedByPreviousAbility[UnityEngine.Random.Range(0, actorsAffectedByPreviousAbility.Count)]);
                }
                else
                {
                    if (actorsAffectedByAbility.Count > 0)
                    {
                        List<Actor> possibles = new List<Actor>();
                        foreach(Actor checkAct in actorsAffectedByAbility)
                        {
                            if (checkAct.actorUniqueID == originatingActor.actorUniqueID) continue;
                            possibles.Add(checkAct);
                        }
                        if (possibles.Count > 0)
                        {
                            buildActorsToProcess.Add(possibles[UnityEngine.Random.Range(0, possibles.Count)]);
                        }                        
                    }
                }
                break;
            case TargetActorType.MULTIPLERANDOMNEARBY:
            case TargetActorType.RANDOMNEARBY:
                pool_actorList.Clear();
                CustomAlgorithms.GetTilesAroundPoint(originatingActor.GetPos(), randTargetRange, MapMasterScript.activeMap);
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND)
                    {
                        foreach(Actor actToAdd in CustomAlgorithms.tileBuffer[i].GetAllActors())
                        {
                            if (UnityEngine.Random.Range(0, 1f) > chanceToHitSpecificTarget) continue;
                            if (!IsFactionValid(actToAdd)) continue;
                            if (actToAdd.actorfaction == Faction.DUNGEON) continue; // Random targeting shouldn't hit neutrals, right?
                            if (actToAdd.GetActorType() == ActorTypes.DESTRUCTIBLE) continue;
                            if (!actToAdd.IsFighter()) continue;
                            Fighter ft = actToAdd as Fighter;
                            if (ft.myStats.CheckHasStatusName("spiritwalk")) continue;
                            if (MapMasterScript.CheckTileToTileLOS(originatingActor.GetPos(), CustomAlgorithms.tileBuffer[i].pos,originatingActor, MapMasterScript.activeMap))
                            {
                                pool_actorList.Add(actToAdd);
                            }                                
                        }
                    }
                }
                if (pool_actorList.Count > 0)
                {
                    if (tActorType == TargetActorType.MULTIPLERANDOMNEARBY)
                    {
                        for (int i = 0; i < pool_actorList.Count; i++)
                        {
                            buildActorsToProcess.Add(pool_actorList[i]);
                        }
                    }
                    else
                    {
                        Actor targetActorToUse = pool_actorList[UnityEngine.Random.Range(0, pool_actorList.Count)];
                        buildActorsToProcess.Add(targetActorToUse);
                    }
                }
                break;
            case TargetActorType.SELF:
                buildActorsToProcess.Add(selfActor);
                break;
            case TargetActorType.CRYSTAL:
                Fighter origFighter = originatingActor as Fighter;
                Actor crystal = origFighter.GetSummonByRef("mon_runiccrystal");
                if (crystal != null)
                {
                    buildActorsToProcess.Add(crystal);
                }
                break;
            case TargetActorType.ORIGINATING:
                buildActorsToProcess.Add(originatingActor);
                break;
            case TargetActorType.ATTACKER:
                if (CombatManagerScript.bufferedCombatData != null)
                {
                    buildActorsToProcess.Add(CombatManagerScript.bufferedCombatData.attacker);
                }                
                break;
            case TargetActorType.DEFENDER:
                if (CombatManagerScript.bufferedCombatData != null)
                {
                    buildActorsToProcess.Add(CombatManagerScript.bufferedCombatData.defender);
                }                
                break;
            case TargetActorType.SINGLE:
                if (targetActors.Count > 0)
                {
                    for (int i = 0; i < targetActors.Count; i++)
                    {
                        if (targetActors[i] == null)
                        {
                            //Debug.Log("Actor index " + i + " in single target ability is null...?");
                            continue;
                        }
                        if (targetActors[i].GetActorType() == ActorTypes.DESTRUCTIBLE && effectRefName != "destroypowerup" && effectRefName != "eff_destroyvine")
                        {
                            continue;
                        }
                        if (IsFactionValid(targetActors[i]))
                        {
                            buildActorsToProcess.Add(targetActors[i]);
                            break;
                        }
                    }                    
                }
                break;
            case TargetActorType.ALL:
            case TargetActorType.RANDOMFROMALL:
            case TargetActorType.ALL_EXCLUDE_HERO:
            case TargetActorType.ALL_EXCLUDE_HERO_ANDSELF:
                //for (Actor act in targetActors)
                for (int i = 0; i < targetActors.Count; i++)
                {
                    act = targetActors[i];
                    if (act.GetActorType() == ActorTypes.HERO && (tActorType == TargetActorType.ALL_EXCLUDE_HERO || tActorType == TargetActorType.ALL_EXCLUDE_HERO_ANDSELF))
                    {
                        continue;
                    }
                    if (act.actorUniqueID == originatingActor.actorUniqueID && tActorType == TargetActorType.ALL_EXCLUDE_HERO_ANDSELF)
                    {
                        continue;
                    }
                    buildActorsToProcess.Add(act);

                    /* if ((parentAbility.targetForMonster == AbilityTarget.ALLY) || (parentAbility.targetForMonster == AbilityTarget.SELF))
                    {
                        if (act.actorfaction == originatingActor.actorfaction)
                        {
                            buildActorsToProcess.Add(act);
                        }
                    }
                    else
                    {
                        if (act.actorfaction != originatingActor.actorfaction)
                        {
                            buildActorsToProcess.Add(act);
                        }
                    } */
                }

                if (buildActorsToProcess.Count > 0 && tActorType == TargetActorType.RANDOMFROMALL)
                {
                    Actor singleTarget = buildActorsToProcess[UnityEngine.Random.Range(0, buildActorsToProcess.Count)];
                    buildActorsToProcess.Clear();
                    buildActorsToProcess.Add(singleTarget);
                }
                

                break;
            case TargetActorType.CORRALPET:
                Monster mn = GameMasterScript.heroPCActor.GetMonsterPet();
                if (mn != null && mn.myStats.IsAlive())
                {
                    buildActorsToProcess.Add(mn);
                }
                break;
            case TargetActorType.HERO_ANYWHERE:
                buildActorsToProcess.Add(GameMasterScript.heroPCActor);
                break;
            case TargetActorType.LOCAL:
            case TargetActorType.LOCAL_HEROONLY:
                localTarg = MapMasterScript.GetTile(centerPosition).GetAllTargetable();
                //foreach (Actor act in localTarg)
                for (int i = 0; i < localTarg.Count; i++)
                {
                    act = localTarg[i];
                    if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) continue;
                    if (tActorType == TargetActorType.LOCAL_HEROONLY && act.GetActorType() != ActorTypes.HERO) continue;

                    bool crystalGotPowerup = false;

                        if (!buildActorsToProcess.Contains(act))
                    {

                        if (act.GetActorType() == ActorTypes.MONSTER)
                        {
                            mn = act as Monster;
                            // KOed monsters should be immune to lava
                            if ((mn.CheckAttribute(MonsterAttributes.LOVESLAVA) > 0 || mn.surpressTraits) && effectRefName == "eff_lavaburning")
                            {
                                continue;
                            }
                            if ((mn.CheckAttribute(MonsterAttributes.LOVESELEC) > 0 || mn.surpressTraits) && effectRefName == "eff_elec")
                            {
                                continue;
                            }
                        }

                        if (IsFactionValid(act))
                        {
                            buildActorsToProcess.Add(act);
                        }

                    }
                }
                break;
            case TargetActorType.LAST_ACTOR_BUFFER:
                int getActID = GameMasterScript.gmsSingleton.ReadTempGameData("last_monster_effecthit");
                Actor getAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(getActID);
                if ((getAct != null) && (getAct.IsFighter()))
                {
                    buildActorsToProcess.Add(getAct);
                }
                else
                {
                    Debug.Log("WARNING: No good buffered actor...");
                }
                break;
            case TargetActorType.ADJACENT:
            case TargetActorType.ADJACENT_NOT_CENTER:
            case TargetActorType.ADJACENT_NOT_CENTERTARGET:
            case TargetActorType.ADJACENT_ORIGINATING:
            case TargetActorType.DEFENDER_AND_ADJACENT:
                MapTileData mtd;
                if (positions.Count == 0)
                {
                    positions.Add(selfActor.GetPos());
                }

                if (tActorType == TargetActorType.ADJACENT_ORIGINATING)
                {
                    positions.Clear();
                    positions.Add(originatingActor.GetPos());
                }
                if (tActorType == TargetActorType.DEFENDER_AND_ADJACENT)
                {
                    positions.Clear();
                    if (CombatManagerScript.bufferedCombatData != null)
                    {
                        positions.Add(CombatManagerScript.bufferedCombatData.defender.GetPos());
                    }
                    else
                    {
                        positions.Add(originatingActor.GetPos());
                    }
                }

                foreach (Vector2 tile in positions)
                {
                    CustomAlgorithms.GetTilesAroundPoint(tile, adjacentRange, MapMasterScript.activeMap);
                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        mtd = CustomAlgorithms.tileBuffer[i];

                        if (tActorType == TargetActorType.ADJACENT_NOT_CENTERTARGET && mtd.pos == tile) continue;
                        if (mtd.pos == originatingActor.GetPos() && tActorType == TargetActorType.ADJACENT_NOT_CENTER) continue;

                        localTarg = mtd.GetAllTargetable();
                        for (int x = 0; x < localTarg.Count; x++)
                        {
                            act = localTarg[x];
                            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) continue;
                            // Adjacent effects only affected enemies before??? Use req actor faction, dumbass
                            if (!buildActorsToProcess.Contains(act))
                            {
                                buildActorsToProcess.Add(act);
                            }
                        }
                    }
                }
                break;
        }

        removeActors.Clear();

        foreach(Actor remAct in skipTargetActors)
        {
            removeActors.Add(remAct);
        }

        if (parentAbility != null && parentAbility.combatOnly)
        {
            int count = buildActorsToProcess.Count;
            for (int i = 0; i < count; i++)
            //foreach (Actor act in actorsToProcess)
            {
                act = buildActorsToProcess[i];
                Fighter ft = act as Fighter;
                if (ft.GetNumCombatTargets() == 0)
                {
                    removeActors.Add(act);
                    continue;
                }
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mon = act as Monster;
                    if (mon.myBehaviorState != BehaviorState.FIGHT)
                    {
                        removeActors.Add(act);
                    }
                }
            }
        }

        if (reqTargetCondition != null && reqTargetCondition != "")
        {
            int count = buildActorsToProcess.Count;
            for (int i = 0; i < count; i++)
            {
                Fighter ft = buildActorsToProcess[i] as Fighter;
                if (!ft.myStats.CheckHasStatusName(reqTargetCondition))
                {
                    removeActors.Add(buildActorsToProcess[i]);
                }
            }
        }


        if (reqActorFaction != Faction.ANY && reqActorFaction != Faction.NONE)
        {
            int count = buildActorsToProcess.Count;
            for (int i = 0; i < count; i++)
            {
                if (!IsFactionValid(buildActorsToProcess[i]))
                {
                    removeActors.Add(buildActorsToProcess[i]);
                    continue;
                }
            }
        }       

        if (triggerPerTurns > 0 && effectType != EffectType.ADDSTATUS && effectType != EffectType.SUMMONACTOR)
        {
            int count = buildActorsToProcess.Count;
            for (int i = 0; i < count; i++)
            {
                act = buildActorsToProcess[i];
                if (act.GetActorType() == ActorTypes.MONSTER || act.GetActorType() == ActorTypes.HERO)
                {
                    Fighter ft = act as Fighter;
                    if (string.IsNullOrEmpty(effectRefName))
                    {
                        Debug.Log("Warning! " + effectName + " has no ref name.");
                    }
                    if (ft.effectsInflictedOnTurn.ContainsKey(effectRefName))
                    {
                        if (ft.effectsInflictedOnTurn[effectRefName] < triggerPerTurns)
                        {
                            removeActors.Add(act);
                        }
                        else
                        {
                            ft.SetEffectInflicted(effectRefName, 0);
                        }
                    }
                    else
                    {
                        ft.AddEffectInflicted(effectRefName, 0);
                    }
                }
            }
        }

        bool groundBasedEffect = false;
        if (parentAbility != null && parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
        {
            groundBasedEffect = true;
        }

        int baCount = buildActorsToProcess.Count;
        for (int i = 0; i < baCount; i++)
        {
            if (buildActorsToProcess[i] == null)
            {
                continue;
            }

            if (parentAbility != null)
            {
                if (buildActorsToProcess[i].CheckIgnoreAbility(parentAbility))
                {
                    removeActors.Add(buildActorsToProcess[i]);
                }

                if (groundBasedEffect)
                {
                    if (buildActorsToProcess[i].GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = buildActorsToProcess[i] as Monster;
                        if (mn.myStats.CheckHasStatusName("monmod_steeltoe"))
                        {
                            removeActors.Add(buildActorsToProcess[i]);
                        }
                    }
                    else if (buildActorsToProcess[i].GetActorType() == ActorTypes.HERO)
                    {
                        if ((UnityEngine.Random.Range(0,2) == 0) && 
                            (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("song_endurance_3") || GameMasterScript.heroPCActor.myStats.CheckHasStatusName("song_endurance_3_songblade")))
                        {
                            removeActors.Add(buildActorsToProcess[i]);
                        }
                    }
                }

            }

            if (isProjectile && indexOfEffect == 0)
            {
                if (UnityEngine.Random.Range(0, 1f) <= CombatManagerScript.CHANCE_WATER_PROJECTILE_DODGE && MapMasterScript.CheckIfSubmerged(buildActorsToProcess[i]) &&
                    effectRefName != "throwstone")
                {
                    StringManager.SetTag(0, buildActorsToProcess[i].displayName);
                    StringManager.SetTag(1, originatingActor.displayName);                    
                    GameLogScript.GameLogWrite(StringManager.GetString("log_dodge_water"), originatingActor);
                    buildActorsToProcess[i].ignoreEffectsOfAbility = CombatManagerScript.GetLastUsedAbility();
                    removeActors.Add(buildActorsToProcess[i]);
                }
            }

            if ((buildActorsToProcess[i].GetActorType() == ActorTypes.DESTRUCTIBLE) && (effectRefName != "destroypowerup") && (effectRefName != "eff_destroyvine"))
            {
                removeActors.Add(buildActorsToProcess[i]); // Destructibles cannot be targeted with abilities?
            }
            else
            {
                if (buildActorsToProcess[i].GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = buildActorsToProcess[i] as Monster;
                    if (!mn.myStats.IsAlive())
                    {
                        removeActors.Add(mn);
                    }
                }
            }
        }

        for (int i = 0; i < removeActors.Count; i++)
        //foreach (Actor act in removeActors)
        {
            act = removeActors[i];
            buildActorsToProcess.Remove(act);
        }

        actorsAffectedByAbility.Clear();
        for (int i = 0; i < buildActorsToProcess.Count; i++)
        {
            if (actorsAffectedByAbility.Count > 45) break; // Don't let the list get too big.
            actorsAffectedByAbility.Add(buildActorsToProcess[i]);
            //Debug.Log("NOW affected by " + effectRefName + ": " + buildActorsToProcess[i].actorRefName);
        }

        if (GameMasterScript.gameLoadSequenceCompleted && doNotAlterPreviousAffectedActorList)
        {
            if (tActorType == TargetActorType.SELF || tActorType == TargetActorType.ORIGINATING)
            {
                foreach(Actor a in actorsAffectedByPreviousAbility)
                {
                    if (a.dungeonFloor == MapMasterScript.activeMap.floor) 
                    {
                        if (actorsAffectedByAbility.Count > 45) break; // Don't let the list get too big.
                        actorsAffectedByAbility.Add(a);                
                        //Debug.Log("Copy from " + a.actorRefName);
                    }
                }                                     
            }    
        }

   
   
        return buildActorsToProcess;
    }

    public virtual float DoEffect(int indexOfEffect = 0)
    {
        Debug.Log("Virtual - effect template not set properly for " + effectName);
        return 0.0f;
    }

    public virtual void ReverseEffect()
    {
        
    }


    public virtual void CopyFromTemplate(EffectScript template)
    {
        for (int i = 0; i < effectTags.Length; i++)
        {
            effectTags[i] = template.effectTags[i];
        }
        minimumTurnsSinceLastDamaged = template.minimumTurnsSinceLastDamaged;
        enforceTriggerPerTurns = template.enforceTriggerPerTurns;
        isProjectile = template.isProjectile;
        adjacentRange = template.adjacentRange;
        playAnimationInstant = template.playAnimationInstant;
        chanceToHitSpecificTarget = template.chanceToHitSpecificTarget;
        triggerCondition = template.triggerCondition;        
        noClearPositionsOnRun = template.noClearPositionsOnRun;
        centerSpriteOnOriginatingActor = template.centerSpriteOnOriginatingActor;
        centerSpriteOnMiddlePosition = template.centerSpriteOnMiddlePosition;
        reqTargetCondition = template.reqTargetCondition;
        reqActorFaction = template.reqActorFaction;
        silent = template.silent;
        effectRefName = template.effectRefName;
        triggerPerTurns = template.triggerPerTurns;
        tActorType = template.tActorType;
        requiredStatusForOrigFighter = template.requiredStatusForOrigFighter;
        effectType = template.effectType;
        effectName = template.effectName;
        selfActor = template.selfActor;
        centerPosition = template.centerPosition;
        originatingActor = template.originatingActor;
        spriteEffectRef = template.spriteEffectRef;
        animLength = template.animLength;
        delayBeforeAnimStart = template.delayBeforeAnimStart;
        playAnimation = template.playAnimation;
        procChance = template.procChance;
        //Debug.Log(effectRefName + " " + effectName + " proc chance " + procChance);
        rotateAnimToTarget = template.rotateAnimToTarget;
        attackerAboveHealth = template.attackerAboveHealth;
        attackerBelowHealth = template.attackerBelowHealth;
        script_processActorsPreEffect = template.script_processActorsPreEffect;
        defenderAboveHealth = template.defenderAboveHealth;
        defenderBelowHealth = template.defenderBelowHealth;
        origAboveHealth = template.origAboveHealth;
        origBelowHealth = template.origBelowHealth;
        randTargetRange = template.randTargetRange;
        processBufferIndex = template.processBufferIndex;
        battleText = template.battleText;
        script_triggerCondition = template.script_triggerCondition;
        requiredStatusStacks = template.requiredStatusStacks;
        anyDamageEquationVars = template.anyDamageEquationVars;

        doNotAlterPreviousAffectedActorList = template.doNotAlterPreviousAffectedActorList;

        for (int i = 0; i < (int)EDamageEquationVars.COUNT; i++)
        {
            damageEquationVars[i] = template.damageEquationVars[i];
        }
        //Debug.Log(effectRefName + " has PBI of " + processBufferIndex);
    }

    public void WriteToSave(XmlWriter writer, int effectIndex, int actorOwnerID)
    {
        // Does anything ACTUALLY need to be written here?
        bool writeAnyData = false;

        if (originatingActor != null)
        {
            if (originatingActor.actorUniqueID != actorOwnerID)
            {
                writeAnyData = true;
            }
        }

        if (effectIndex > 0)
        {
            writeAnyData = true;
        }
        if (effectType == EffectType.CHANGESTAT)
        {
            ChangeStatEffect cse = this as ChangeStatEffect;
            if (cse.accumulatedAmount > 0)
            {
                writeAnyData = true;
            }
        }
        if (effectType == EffectType.ALTERBATTLEDATA)
        {
            AlterBattleDataEffect abde = this as AlterBattleDataEffect;
            if (abde.accumulatedPercentAllDamage != 0 || abde.accumulatedPercentAllMitigation != 0)
            {
                writeAnyData = true;
            }
        }

        if (!writeAnyData)
        {
            return;
        }


        writer.WriteStartElement("eff"); // was "abileffect"

        int i = effectIndex;

        if (i > 0)
        {
            writer.WriteElementString("index", i.ToString());
        }

        if (originatingActor != null)
        {
            writer.WriteElementString("oaid", originatingActor.actorUniqueID.ToString());
        }
        else
        {
            writer.WriteElementString("oaid", 0.ToString());
        }

        if (effectType == EffectType.CHANGESTAT)
        {
            ChangeStatEffect cse = this as ChangeStatEffect;
            if (cse.accumulatedAmount > 0)
            {
                writer.WriteElementString("accumulated", cse.accumulatedAmount.ToString());
            }
        }
        if (effectType == EffectType.ALTERBATTLEDATA)
        {
            AlterBattleDataEffect abde = this as AlterBattleDataEffect;
            if (abde.changeDurability != 0)
            {
                writer.WriteElementString("changedurability", abde.changeDurability.ToString());
            }
            if (abde.accumulatedPercentAllDamage != 0)
            {
                writer.WriteElementString("accumulateddamage", abde.accumulatedPercentAllDamage.ToString());
            }
            if (abde.accumulatedPercentAllMitigation != 0)
            {
                writer.WriteElementString("accumulatedmitigation", abde.accumulatedPercentAllMitigation.ToString());
            }
        }

        // SpellShapeEffect and AbilityModifierEffect would save here, except currently they're only data and no mutable values.
        // unless you abuse public and change numbers. Bad.

        writer.WriteEndElement();
    }

    public void TryReadDamageEquationVarFromXml(XmlReader reader, EDamageEquationVars dVar)
    {
        string unparsed = reader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');
        damageEquationVars[(int)dVar] = CustomAlgorithms.TryParseFloat(parsed[0]);
        if (parsed.Length == 2)
        {
            effectPower = CustomAlgorithms.TryParseFloat(parsed[1]);
        }
        anyDamageEquationVars = true;
    }

    // If we have a null originatingActor, something may have been disconnected
    // We MAY still have a "selfActor" which is probably the intended originatingActor
    // If we have neither an OA nor a selfActor, we cannot verify&fix
    public bool VerifyOriginatingActorIsFighterAndFix()
    {        
        if (originatingActor == null || !originatingActor.IsFighter())
        {
            if (selfActor != null && selfActor.IsFighter())
            {
                originatingActor = selfActor;
                return true;
            }
            else return false;
        }
        return true;
    }

    // Similar to above but with SelfActor instead of OriginatingActor.
    public bool VerifySelfActorIsFighterAndFix()
    {
        if (selfActor == null || !selfActor.IsFighter())
        {
            if (originatingActor != null && originatingActor.IsFighter())
            {
                selfActor = originatingActor;
                return true;
            }
            return false;
        }
        return true;
    }
}


public class EffectResultPayload
{
    public float waitTime;
    public List<Actor> actorsToProcess;
    public float value;
    public bool cancel;

    static Stack<EffectResultPayload> erpPool;

    public static void Initialize()
    {
        erpPool = new Stack<EffectResultPayload>(50);

        for (int i = 0; i < 50; i++)
        {
            erpPool.Push(new EffectResultPayload());
        }
    }

    public static EffectResultPayload GetERP()
    {
        if (erpPool.Count == 0)
        {
            erpPool.Push(new EffectResultPayload());
        }

        EffectResultPayload erp = erpPool.Pop();
        erp.waitTime = 0f;
        erp.value = 0f;
        erp.cancel = false;
        erp.actorsToProcess.Clear();

        return erp;
    }

    public static void PoolERP(EffectResultPayload erp)
    {
        erpPool.Push(erp);
    }

    public EffectResultPayload()
    {
        actorsToProcess = new List<Actor>();
        cancel = false;
        value = 0.0f;
    }

    
}



