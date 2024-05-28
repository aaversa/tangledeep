using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public partial class AddStatusCustomFunctions
{
    public static Dictionary<string, Func<AddStatusEffect, Actor, bool>> dictPerActorDelegates;
    public static Dictionary<string, Func<AddStatusEffect, bool>> dictPreStatusDelegates;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        dictPerActorDelegates = new Dictionary<string, Func<AddStatusEffect, Actor, bool>>();
        dictPreStatusDelegates = new Dictionary<string, Func<AddStatusEffect, bool>>();

        initialized = true;
    }

    public static void CachePerActorScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictPerActorDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo myMethod = typeof(AddStatusCustomFunctions).GetMethod(scriptName, new Type[] { typeof(AddStatusEffect), typeof(Actor) });

        Func<AddStatusEffect, Actor, bool> converted = (Func<AddStatusEffect, Actor, bool>)Delegate.CreateDelegate(typeof(Func<AddStatusEffect, Actor, bool>), myMethod);

        dictPerActorDelegates.Add(scriptName, converted);
    }

    public static void CachePreStatusScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictPreStatusDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo myMethod = typeof(AddStatusCustomFunctions).GetMethod(scriptName, new Type[] { typeof(AddStatusEffect) });

        Func<AddStatusEffect, bool> converted = (Func<AddStatusEffect, bool>)Delegate.CreateDelegate(typeof(Func<AddStatusEffect, bool>), myMethod);

        dictPreStatusDelegates.Add(scriptName, converted);
    }

    // Return TRUE if skipping actor, FALSE if continuing as normal

    /// <summary>
    /// Executes a movement ability to pull the target close to us
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool PullTargetToAttacker(AddStatusEffect effect, Actor target)
    {
        int distance = MapMasterScript.GetGridDistance(target.GetPos(), effect.originatingActor.GetPos());
        if (distance == 1) return false;

        // We must get the target least somewhat closer for this to work.

        MapTileData nearbyTile = MapMasterScript.GetRandomEmptyTile(effect.originatingActor.GetPos(), 1, true, true, true, true, false);
        int distToOtherTile = MapMasterScript.GetGridDistance(nearbyTile.pos, effect.originatingActor.GetPos());
        if (distToOtherTile >= distance) return false;

        MapMasterScript.activeMap.MoveActor(effect.originatingActor.GetPos(), nearbyTile.pos, target);
        target.myMovable.AnimateSetPosition(nearbyTile.pos, 0.1f, false, 360f, 0f, MovementTypes.LERP);
        BattleTextManager.NewText(StringManager.GetString("misc_pulled"), target.GetObject(), Color.green, 0.2f);

        return false;
    }

    /// <summary>
    ///  Used by Wraiths (etc) when Haunting their target. Target cannot damage anything except the wraith.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool AssignHauntFlagToTarget(AddStatusEffect effect, Actor target)
    {
        effect.originatingActor.SetActorData("haunttarget", target.actorUniqueID);
        return false;
    }

    /// <summary>
    /// Used by Mimics when they strike a target in melee range and stick their tongue to them. Effects described below.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool StickTongueToTarget(AddStatusEffect effect, Actor target)
    {
        Fighter origFT = effect.originatingActor as Fighter;
        
        // First, verify that target is not our faction. If our attack was reflected etc, we don't want to stick to ourselves.
        if (origFT.actorfaction == target.actorfaction)
        {
            return true; // Skip.
        }

        Fighter targetFT = target as Fighter;

        // Are we already stuck to the target?
        if (targetFT.myStats.CheckHasActiveStatusName("adhesivetongue_target"))
        {
            return true; // Skip
        }

        // We can only be stuck to one target at once, so if we're stuck to an existing fighter, remove Stuck status from them.
        int existingStuckActorID = origFT.ReadActorData("stucktoactor");
        if (existingStuckActorID >= 0)
        {
            Actor findActor = GameMasterScript.gmsSingleton.TryLinkActorFromDict(existingStuckActorID);
            if (findActor != null && findActor.IsFighter())
            {
                Fighter previousTarget = findActor as Fighter;
                previousTarget.myStats.ForciblyRemoveStatus("adhesivetongue_target");
            }
        }

        // Stick ourselves to this new target.
        origFT.SetActorData("stucktoactor", target.actorUniqueID);

        // This will silently check if we are still stuck to the target each turn
        targetFT.myStats.AddStatusByRef("tonguechecker", origFT, 99);
        targetFT.SetActorData("tongueattacker", origFT.actorUniqueID);

        // If we don't already have our hidden "stickywarp" status, add it. This status is what moves us when the target moves.
        origFT.myStats.AddStatusByRef("stickywarp", origFT, 99);

        LatchOnToTarget(effect, target);

        return false;
    }

    public static bool AffectOnlyMonsters(AddStatusEffect effect, Actor target)
    {
        if (target.GetActorType() != ActorTypes.MONSTER)
        {
            return true; // Skip!
        }

        return false;
    }

    public static bool UseRandomElementalDebuff(AddStatusEffect effect, Actor target)
    {
        List<string> possibleStatusRefs = new List<string>()
        {
            "lightningdebuff30",
            "shadowdebuff30",
            "waterdebuff30"
        };

        effect.localRef = possibleStatusRefs[UnityEngine.Random.Range(0, possibleStatusRefs.Count)];

        return false;
    }

    public static bool AddResistBonusFromEmblem(AddStatusEffect effect, Actor target)
    {
        Fighter ft = target as Fighter;

        if (!ft.myStats.CheckHasStatusName("emblem_spellshaperemblem_tier0_aura"))
        {
            return false;
        }

        string resistRef = "";
        switch (effect.statusRef)
        {
            case "firebarrier":
                resistRef = "resistfire12";
                break;
            case "shadowbarrier":
                resistRef = "resistshadow12";
                break;
            case "icebarrier":
                resistRef = "resistwater12";
                break;
            case "acidbarrier":
            default:
                resistRef = "resistpoison12";
                break;
        }

        ft.myStats.AddStatusByRefAndLog(resistRef, ft, 6);

        return false;
    }

    public static bool CheckDisarmingTargetHostile(AddStatusEffect effect, Actor target)
    {
        if (CombatManagerScript.bufferedCombatData == null) return false;

        if (!effect.targetActors.Contains(CombatManagerScript.bufferedCombatData.defender))
        {
            effect.targetActors.Add(CombatManagerScript.bufferedCombatData.defender);
        }
        foreach (Actor act in effect.targetActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster mon = act as Monster;
            float virtualProcChance = 0.2f;
            if (mon.myBehaviorState != BehaviorState.FIGHT || mon.GetTargetAggro(GameMasterScript.heroPCActor) <= 0)
            {
                virtualProcChance = 1.0f;
            }

            if (UnityEngine.Random.Range(0, 1f) > virtualProcChance)
            {
                effect.skipTargetActors.Add(act);
            }
        }

        return false;
    }

    public static bool RandomFearEffect(AddStatusEffect effect, Actor target)
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            effect.localRef = "status_shaken";
        }
        else
        {
            effect.localRef = "status_fear50";
        }
        return false;
    }

    public static bool CheckBreathstealerTargetHostile(AddStatusEffect effect, Actor target)
    {
        if (CombatManagerScript.bufferedCombatData == null) return false;

        if (!effect.targetActors.Contains(CombatManagerScript.bufferedCombatData.defender))
        {
            effect.targetActors.Add(CombatManagerScript.bufferedCombatData.defender);
        }
        foreach (Actor act in effect.targetActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster mon = target as Monster;
            float virtualProcChance = 0.25f;
            if (mon.myBehaviorState != BehaviorState.FIGHT || mon.GetTargetAggro(GameMasterScript.heroPCActor) <= 0)
            {
                virtualProcChance = 1.0f;
            }

            if (UnityEngine.Random.Range(0, 1f) > virtualProcChance)
            {
                effect.skipTargetActors.Add(act);
            }
        }

        return false;
    }

    public static bool BufferTargetMonsterID(AddStatusEffect effect, Actor target)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("last_monster_effecthit", target.actorUniqueID);

        Fighter ft = target as Fighter;
        ft.myAnimatable.SetAnimConditional(ft.myAnimatable.defaultTakeDamageAnimationName);

        return false;
    }


    public static bool CheckSneakAttackToRestorePreviousDuration(AddStatusEffect effect, Actor target)
    {
        if (effect.localRef == "sneakattack" && effect.effectRefName == "addsneakattackpassive")
        {
            int prevWaiterDur = target.ReadActorData("statusdur_sneakattackwaiter");
            if (prevWaiterDur >= 1)
            {
                effect.localRef = "sneakattackwaiter";
                effect.localDuration = prevWaiterDur;
            }
        }

        return false;
    }

    public static bool ExtendWildChildDuration(AddStatusEffect effect, Actor target)
    {
        if (target.GetActorType() == ActorTypes.HERO)
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("wildchildbonus3"))
            {
                effect.localDuration += (int)(effect.localDuration * 1.33f);
            }
        }

        return false;
    }

    public static bool EagleEye(AddStatusEffect effect, Actor target)
    {
        for (int p = 0; p < effect.positions.Count; p++)
        {
            GameMasterScript.heroPCActor.tempRevealTiles.Add(effect.positions[p]);
        }

        return false;
    }

    public static bool SuppressionDebuff(AddStatusEffect effect, Actor target)
    {
        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                effect.localRef = "status_rooted";
                break;
            case 1:
                effect.localRef = "status_paralyzed";
                break;
            case 2:
                effect.localRef = "status_sealed";
                break;
        }

        return false;
    }

    public static bool RandomGamblerDebuff(AddStatusEffect effect, Actor target)
    {
        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                effect.localRef = "status_fear50";
                break;
            case 1:
                effect.localRef = "status_asleep";
                break;
            case 2:
                effect.localRef = "status_confused50";
                break;
        }

        return false;
    }


    public static bool PlaySmokeSpewEffect(AddStatusEffect effect, Actor target)
    {
        CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "SmokeSpewEffect", effect, true);

        return false;
    }

    public static bool LatchOnToTarget(AddStatusEffect effect, Actor target)
    {
        Fighter ft = target as Fighter;
        ft.AddAnchor(effect.originatingActor);
        effect.originatingActor.anchor = target;
        effect.originatingActor.anchorID = target.actorUniqueID;
        effect.originatingActor.anchorRange = 0;
        //Debug.Log(effect.originatingActor.actorRefName + " should now be attached to " + target.actorRefName + " at a range of " + effect.originatingActor.anchorRange);

        StringManager.SetTag(0, effect.originatingActor.displayName);
        StringManager.SetTag(1, target.displayName);
        GameLogScript.LogWriteStringRef("log_stucktotarget");

        return false;
    }

    public static bool ChemicalCocktail(AddStatusEffect effect, Actor target)
    {
        Fighter fight = target as Fighter;
        bool validStatus = false;
        int attempts = 0;
        while (!validStatus)
        {
            attempts++;
            switch (UnityEngine.Random.Range(0, 6))
            {
                case 0:
                    effect.localRef = "status_armorbreakweaker";
                    break;
                case 1:
                    effect.localRef = "atkdownns";
                    break;
                case 2:
                    effect.localRef = "status_rooted";
                    effect.localDuration = 3f;
                    break;
                case 3:
                    effect.localRef = "status_paralyzed";
                    effect.localDuration = 3f;
                    break;
                case 4:
                    effect.localRef = "status_charmed";
                    break;
                case 5:
                    effect.localRef = "status_sealed";
                    break;
            }
            if (!fight.myStats.CheckHasStatusName("localRef"))
            {
                validStatus = true;
            }
            if (attempts > 5)
            {
                break;
            }
        }

        return false;
    }

    public static bool TryAddHunterEmblemBonus(AddStatusEffect effect, Actor target)
    {
        Fighter origFight = effect.originatingActor as Fighter;
        if (origFight != null && origFight.myStats.CheckHasStatusName("emblem_hunteremblem_tier2_stalk"))
        {
            Fighter tFight = target as Fighter;
            tFight.myStats.AddStatusByRef("hunteremblem_tracking_dmgbonus", origFight, (int)effect.localDuration);
        }
        return false;
    }

    public static bool DelayedPlayBuffAndRoar(AddStatusEffect effect, Actor target)
    {
        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "FervirBuffSilent", effect, 0.25f, false, 0f, true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "SoundEmanation", effect, 0.25f, true);

        return false;
    }

    public static bool DelayedPlayHealAndRoar(AddStatusEffect effect, Actor target)
    {
        CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "FervirRecovery", effect, 0.25f, false, 0f, true);
        //CombatManagerScript.WaitThenGenerateSpecificEffect(target.GetPos(), "SoundEmanation", effect, 0.25f, true);

        return false;
    }

    public static bool BalefulEchoes(AddStatusEffect effect, Actor target)
    {
        Fighter fight = target as Fighter;
        bool validStatus = false;
        int attempts = 0;
        while (!validStatus)
        {
            attempts++;
            switch (UnityEngine.Random.Range(0, 6))
            {
                case 0:
                    effect.localRef = "status_armorbreakweaker";
                    break;
                case 1:
                    effect.localRef = "atkdownns";
                    break;
                case 2:
                    effect.localRef = "status_rooted";
                    effect.localDuration = 3f;
                    break;
                case 3:
                    effect.localRef = "status_paralyzed";
                    effect.localDuration = 3f;
                    break;
                case 4:
                    if (effect.statusRef == "randomdebuff2") continue;
                    effect.localRef = "status_charmed";
                    break;
                case 5:
                    effect.localRef = "status_sealed";
                    break;
            }
            if (!fight.myStats.CheckHasStatusName("localRef"))
            {
                validStatus = true;
            }
            if (attempts > 5)
            {
                break;
            }
        }

        return false;
    }
}
