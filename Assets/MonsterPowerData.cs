using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MonsterPowerData
{
    public AbilityScript abilityRef;
    public float healthThreshold; // from 0 to 1f
    public BehaviorState useState;
    public int minRange;
    public int maxRange;
    public float chanceToUse; // from 0 to 1f
    public bool useWithNoTarget;
    public bool ignoreCosts;

    public bool alwaysUseIfInRange; // If TRUE, we'll always use this ability if we CAN use it and are within range - even if it's not the most optimal range.

    public bool enforceRangesForHeroTargeting; // If TRUE, we MUST be within min/max range of the Hero, or a hero pet
                                               // Even if we haven't targeted the hero, this should still be checked

    Dictionary<int, FailedTargetingData> targetData;

    /// <summary>
    /// This key must exist in our ActorDict
    /// </summary>
    public string reqActorData;

    /// <summary>
    /// We must have the reqActorData key at THIS VALUE or HIGHER
    /// </summary>
    public int reqActorDataValue;

    public MonsterPowerData()
    {
        healthThreshold = 1.0f;
        useState = BehaviorState.ANY; // this WAS fight, before...
        chanceToUse = 1.0f;
        useWithNoTarget = false;
        minRange = 0;
        maxRange = 99;
        ignoreCosts = false;
        targetData = new Dictionary<int, FailedTargetingData>();
    }

    public void CopyFromTemplate(MonsterPowerData mpd, AbilityScript abil)
    {
        abilityRef = abil;
        healthThreshold = mpd.healthThreshold;
        useState = mpd.useState;
        minRange = mpd.minRange;
        maxRange = mpd.maxRange;
        chanceToUse = mpd.chanceToUse;
        useWithNoTarget = mpd.useWithNoTarget;
        ignoreCosts = mpd.ignoreCosts;
        enforceRangesForHeroTargeting = mpd.enforceRangesForHeroTargeting;
        alwaysUseIfInRange = mpd.alwaysUseIfInRange;
        reqActorData = mpd.reqActorData;
        reqActorDataValue = mpd.reqActorDataValue;
    }

    public bool CheckIfValid(Vector2 origPos, Actor target)
    {
        FailedTargetingData ptd;
        if (targetData.TryGetValue(target.actorUniqueID, out ptd))
        {
            if (origPos == ptd.originPosition && target.GetPos() == ptd.targetPosition)
            {
                // We already tried using this ability from our position to target position
                // And it failed, so we can't use it again here.
                return false;
            }

            return true; // Positions changed so we can go ahead and try again.
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// This MPD *does* work from originPos to target's pos!
    /// </summary>
    /// <param name="originPos"></param>
    /// <param name="target"></param>
    public void MarkAsSucceeded(Actor target)
    {
        targetData.Remove(target.actorUniqueID);
    }

    /// <summary>
    /// Flags this MPD as not working from originPos --> target's pos, so we won't try it again until something changes.
    /// </summary>
    /// <param name="originPos"></param>
    /// <param name="target"></param>
    public void MarkAsFailed(Vector2 originPos, Actor target)
    {
        if (target == null)
        {
            return;
        }
        FailedTargetingData ptd;
        if (targetData.TryGetValue(target.actorUniqueID, out ptd))
        {
            ptd.targetPosition = target.GetPos();
            ptd.originPosition = originPos;
        }
        else
        {
            FailedTargetingData ftd = new FailedTargetingData();
            ftd.originPosition = originPos;
            ftd.targetID = target.actorUniqueID;
            ftd.targetPosition = target.GetPos();
            targetData.Add(target.actorUniqueID, ftd);
        }
    }

    class FailedTargetingData
    {
        public Vector2 originPosition;
        public int targetID;
        public Vector2 targetPosition;
    }
}
