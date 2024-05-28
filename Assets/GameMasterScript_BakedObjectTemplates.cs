using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    public void CreateBakedGameDataObjectTemplates()
    {
        genericMonster = new Monster();

        spellshiftMaterializeTemplate = new SummonActorEffect();
        spellshiftMaterializeTemplate.effectType = EffectType.SUMMONACTOR;
        spellshiftMaterializeTemplate.summonActorType = ActorTypes.DESTRUCTIBLE;
        spellshiftMaterializeTemplate.summonDuration = 9;
        spellshiftMaterializeTemplate.summonActorPerTile = true;
        spellshiftMaterializeTemplate.anchorType = TargetActorType.SELF;
        spellshiftMaterializeTemplate.anchorRange = 3;

        vitalPointPainTemplate = new AddStatusEffect();
        vitalPointPainTemplate.effectType = EffectType.ADDSTATUS;
        vitalPointPainTemplate.statusRef = "status_painenhanced";
        vitalPointPainTemplate.baseDuration = 7;
        vitalPointPainTemplate.tActorType = TargetActorType.ALL;

        vitalPointBleedTemplate = new AddStatusEffect();
        vitalPointBleedTemplate.effectType = EffectType.ADDSTATUS;
        vitalPointBleedTemplate.statusRef = "ppbleed";
        vitalPointBleedTemplate.baseDuration = 5;
        vitalPointBleedTemplate.tActorType = TargetActorType.ALL;

        vitalPointExplodeTemplate = new AddStatusEffect();
        vitalPointExplodeTemplate.effectType = EffectType.ADDSTATUS;
        vitalPointExplodeTemplate.statusRef = "status_ppexplode";
        vitalPointExplodeTemplate.baseDuration = 10;
        vitalPointExplodeTemplate.tActorType = TargetActorType.ALL;

        vitalPointParalyzeTemplate = new AddStatusEffect();
        vitalPointParalyzeTemplate.effectType = EffectType.ADDSTATUS;
        vitalPointParalyzeTemplate.statusRef = "status_paralyzed";
        vitalPointParalyzeTemplate.baseDuration = 3;
        vitalPointParalyzeTemplate.tActorType = TargetActorType.ALL;

        brigandBomberTemplate = new SummonActorEffect();
        brigandBomberTemplate.effectRefName = "brigandbombersummoner";
        brigandBomberTemplate.effectType = EffectType.SUMMONACTOR;
        brigandBomberTemplate.effectName = StringManager.GetString("skill_bomber");
        brigandBomberTemplate.summonActorRef = "obj_weakershrapnelbomb";
        brigandBomberTemplate.summonActorType = ActorTypes.DESTRUCTIBLE;
        brigandBomberTemplate.summonDuration = 3;
        brigandBomberTemplate.summonOnSummoner = true;
        brigandBomberTemplate.summonOnCollidable = true;
        brigandBomberTemplate.anchorType = TargetActorType.SELF;

        // Create a dummy effect used for tossing items, gold, powerups, etc
        // Height and length can be changed locally
        tossProjectileDummy = new EffectScript();
        tossProjectileDummy.projectileTossHeight = 1.2f;
        tossProjectileDummy.projectileMovementType = MovementTypes.TOSS;
        tossProjectileDummy.animLength = 0.25f;

        // Create a dummy ability that is used for ordering your pet to hit stuff.
        petAttackAbilityDummy = new AbilityScript();
        petAttackAbilityDummy.range = 7;
        petAttackAbilityDummy.abilityName = StringManager.GetString("misc_pet_attack");
        petAttackAbilityDummy.shortDescription = StringManager.GetString("misc_pet_attack_desc");
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.INSTANT);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.TARGETED);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.CURSORTARGET);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.TVISIBLEONLY);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.LINEOFSIGHTREQ);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.MONSTERAFFECTED);
        petAttackAbilityDummy.AddAbilityTag(AbilityTags.CENTERED);
        petAttackAbilityDummy.boundsShape = TargetShapes.RECT;
        petAttackAbilityDummy.targetShape = TargetShapes.POINT;
        petAttackAbilityDummy.targetRange = 0;
        petAttackAbilityDummy.targetForMonster = AbilityTarget.ENEMY;

        // Create a dummy ability that is used for all ranged weapons.
        rangedWeaponAbilityDummy = new AbilityScript();
        rangedWeaponAbilityDummy.range = 0;
        rangedWeaponAbilityDummy.abilityName = StringManager.GetString("misc_ranged_attack");
        rangedWeaponAbilityDummy.shortDescription = StringManager.GetString("misc_ranged_attack_desc");
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.INSTANT);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.TARGETED);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.CURSORTARGET);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.TVISIBLEONLY);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.LINEOFSIGHTREQ);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.MONSTERAFFECTED);
        rangedWeaponAbilityDummy.AddAbilityTag(AbilityTags.CENTERED);
        rangedWeaponAbilityDummy.boundsShape = TargetShapes.RECT;
        rangedWeaponAbilityDummy.targetShape = TargetShapes.POINT;
        rangedWeaponAbilityDummy.targetRange = 0;
        rangedWeaponAbilityDummy.targetForMonster = AbilityTarget.ENEMY;

        theDungeonActor = new Fighter();
        theDungeonActor.actorfaction = Faction.DUNGEON;
        theDungeonActor.displayName = StringManager.GetString("misc_dungeon_name");
        theDungeonActor.myStats = new StatBlock();
        theDungeonActor.myEquipment = new EquipmentBlock();
        theDungeonActor.CreateNewInventory();
        theDungeonActor.myAbilities = new AbilityComponent();
        theDungeonActor.allDamageMultiplier = 1.0f;
        theDungeonActor.actorUniqueID = -100;
        theDungeonActor.actorRefName = "ref_dungeondummy";
        AddActorToDict(theDungeonActor);
        //Debug.Log("Created the dungeon actor");

        monsterJob = new CharacterJobData();
        monsterJob.jobEnum = CharacterJobs.MONSTER;
        monsterJob.jobName = "Monster";
    }

}