using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerializableTemplates {

    public static void ConvertAllAbilitiesToTemplates()
    {

    }
}



public class AbilityTemplate
{
    public string refName;
    public string abilityName;
    public int maxCooldownTurns;
    public string combatLogText;
    public string chargeText;
    public string extraDescription;
    public bool combatOnly;
    public int exclusionGroup;
    public int repetitions;
    public int range;
    public int spiritsRequired;        
    public string iconSprite;
    public string sfxOverride;
    public List<string> requireTargetRef;
    public TargetShapes targetShape;
    public TargetShapes boundsShape;
    public int targetRange;
    public float randomChance;
    public int numMultiTargets; 
    public int targetChangeCondition;
    public int staminaCost;
    public int energyCost;
    public int healthCost;
    public float percentCurHealthCost;
    public float percentMaxHealthCost;
    public int chargeTime;
    public string description;
    public string shortDescription;
    public AbilityTarget targetForMonster;
    public bool passiveAbility;
    public bool displayInList;
    public int passTurns;
    public int chargeTurns;
    public bool toggled;
    public bool passiveEquipped;
    public bool usePassiveSlot;
    public bool spellshift;
    public bool budokaMod;
    public bool[] abilityFlags;
    public CharacterJobs jobLearnedFrom;
    public int targetOffsetX;
    public int targetOffsetY;
    public KeyCode binding;
    public Directions direction;
    public Directions lineDir;
    public LandingTileTypes landingTile;
    public WeaponTypes reqWeaponType;
    public static string[] AbilityTargetNames;
    public string script_AttackBlock;

    public List<string> listEffectScripts; // EffectTemplate
    public List<string> subAbilities; // AbilityScript
    public int[] conditionsIndex;
    public EffectConditionalEnums[] conditionalEnums;
    //public List<EffectConditional> conditions; // EffectConditional
}

public class EffectTemplate
{
    public EffectType effectType;
    public string effectName;
    public string effectRefName;
    public TargetActorType tActorType;
    public string spriteEffectRef;
    public float animLength;
    public bool playAnimation;
    public bool rotateAnimToTarget;
    public float procChance;
    public bool silent;
    public bool centerSpriteOnOriginatingActor;
    public bool centerSpriteOnMiddlePosition;
    public bool noClearPositionsOnRun;
    public float attackerBelowHealth;
    public float attackerAboveHealth;
    public float defenderBelowHealth;
    public float defenderAboveHealth;
    public float origBelowHealth;
    public float origAboveHealth;
    public string reqTargetCondition;
    public Faction reqActorFaction;
    public int triggerPerTurns; 
    public AttackConditions triggerCondition;
    public bool[] switchFlags;
    public int extraTempData;
    public int randTargetRange;
    public int processBufferIndex;
    public string battleText;
    public bool[] effectTags;
}

public class AttackReactionEffectTemplate : EffectTemplate
{
    public string effectEquation;
    public float effectPower;
    public float alterParry; // as % mod to current
    public float alterAccuracy; // as %
    public float alterBlock;
    public float alterBlockFlat;
    public float alterParryFlat; // flat number
    public float alterAccuracyFlat; // flat number
    public float alterDamage; // as flat mod to current
    public float alterDamagePercent; // as % mod to current
    public AttackConditions reactCondition;
}

public class InfluenceTurnEffectTemplate : EffectTemplate
{
    public float confuseChance;
    public float sleepChance;
    public float paralyzeChance;
    public float silenceChance;
    public float stunChance;
    public float rootChance;
    public float charmChance;
    public float fearChance;
}

public class EmpowerAttackEffectTemplate : EffectTemplate
{
    public List<AttackConditions> theConditions = new List<AttackConditions>();
    public string effectEquation;
    public float effectPower;
    public float baseDamage = 0.0f;
    public float maxExtraDamageAsPercent = 1.0f; // Adds at most an additional (up to) 100% of original damage
    public bool silentEmpower;
    public float extraChanceToCrit = 1.0f;
    public float extraChanceToCritFlat = 0.0f;
}

public class MoveActorEffectTemplate : EffectTemplate
{
    public bool pullActor; // This is used by monsters.
    public int distance; // used for push effects
    public bool spin;
    public bool moveThroughObstacles;
    public float arcMult;
    public Directions forceDirection;
    public bool moveToLandingTile;
    public bool swapPlaces;
    public int randomRange;
}

public class RemoveStatusEffectTemplate : EffectTemplate
{
    public string statusRef;
    public List<string> removableStatuses;
    public bool removeAllNegative;
    public bool[] removeFlags;
}

public class AbilityModifierEffectTemplate : EffectTemplate
{
    public int changeStaminaCost;
    public int changeEnergyCost;

    //ghooooosts
    public int changeEchoCost;

    //maybe one day powers can cost health or change CT 
    //when paid for. We'll see.
    public int changeHealthCost;
    public int changeCTCost;

    //if not GENERIC or COUNT, will apply to every ability that uses this job.
    public CharacterJobs jobGroupToModify;

    //if the job is GENERIC, look here for a specific list of refNames to apply the effect to
    public List<string> abilityRefsToModify;

    //Any additional text we want to add or remove from the ability description?
    public string strTextToAddToDescription;
    public string strTextToRemoveFromDescription;

    //replace one ability with another
    //such as replacing cloak and dagger with Cloaks and Daggerinos
    public string strRemapAbilitiesToThisRef;
}

public class SpellshaperEffectTemplate : AbilityModifierEffectTemplate
{
    public ESpellShape spellShape;
    public string strAdditionalAudio;
    public int changeRange;
}

public class AlterBattleDataEffectTemplate : EffectTemplate
{
    public int changeDurability;
    public float changePercentAllDamage;
    public float changePercentAllMitigation;
    public float changeFireResist;
    public float changeWaterResist;
    public float changePhysicalResist;
    public float changeShadowResist;
    public float changePoisonResist;
    public float changeLightningResist;
    public float changeFireDamage;
    public float changeWaterDamage;
    public float changePhysicalDamage;
    public float changeShadowDamage;
    public float changePoisonDamage;
    public float changeLightningDamage;
    public float changeSpiritPower;
    public float changeSpiritPowerMult;
    public float changeCritDamage;
    public float changeEnergyCosts;
    public float changeStaminaCosts;
    public float changeHealthCosts;

    //These numbers force additional costs even if the base power doesn't cost that resource.
    //0.1 forcedStaminaCosts makes all powers cost 10% more stamina, and powers that do not normally
    //cost stamina cost +stam == (energyCost * 0.1)
    public float forcedStaminaCosts;
    public float forcedEnergyCosts;

    public string monFamilyName;
    public float familyDamage;
    public float familyDefense;
    public float changeCritChance;
}

public class AddStatusEffectTemplate : EffectTemplate
{
    public float baseDuration;
    public string statusRef;
}

public class DamageEffectTemplate : EffectTemplate
{
    public DamageTypes damType;
    public float effectPower;
    public string effectEquation;
    public float floorValue;
    public float ceilingValue;
    public float lastDamageAmount;
    public bool canCrit;
    public bool damageItem;
    public bool canBeParriedOrBlocked;
    public AttackConditions runCondition;
}

public class SummonActorEffectTemplate : EffectTemplate
{
    public ActorTypes summonActorType;
    public string summonActorRef;
    public int summonDuration;
    public bool summonOnCollidable;
    public TargetActorType anchorType;
    public int anchorRange;
    public bool scaleWithLevel;
    public bool summonActorPerTile;
    public bool uniqueSummon;
    public bool summonOnSummoner;
    public bool actOnlyWithSummoner;
    public bool dieWithSummoner;
    public bool hideCharmVisual;
    public bool summonNoStacking;
    public int maxRandomSummonRange;
    public int numRandomSummons;
}

public class DestroyTileEffectTemplate : EffectTemplate
{

}

public class SpecialEffectTemplate : EffectTemplate
{

}
public class DestroyActorEffectTemplate : EffectTemplate
{
    public List<string> destroySpecificActors;
}

public class ChangeStatEffectTemplate : EffectTemplate
{
    public StatTypes stat;
    public StatDataTypes statData;
    public float effectPower;
    public string effectEquation;
    public float floorValue;
    public float ceilingValue;
    public bool reverseOnEnd;
    public bool changeSubtypes;
  
}