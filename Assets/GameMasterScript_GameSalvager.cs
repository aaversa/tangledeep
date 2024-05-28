using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.IO;

public partial class GameMasterScript : MonoBehaviour
{
    /// <summary>
    /// Runs if our previous turn was interrupted due to some kind of unknown exception. Attempts to reset the turn state to ensure game is still playable.
    /// </summary>
    void ResetTurnDataDueToError()
    {
        // Clear variables that could cause the game to never take turns again.
        animationPlaying = false;
        turnWasStopped = false;
        turnExecuting = false;

        // Figure out if a specific actor was the cause of our ills.
        int idOfLastMonsterThatAttemptedToAct = ReadTempGameData("latest_monsterturn_beforeaction");
        int idOfLastMonsterThatSuccessfullyActed = ReadTempGameData("latest_monsterturn_afteraction");
        int idOfLastDestructibleThatAttemptedToAct = ReadTempGameData("latest_destructibleturn_beforeaction");
        int idOfLastDestructibleThatSuccessfullyActed = ReadTempGameData("latest_destructibleturn_afteraction");

        // These should be identical. If they are not, then this monster needs to be cleaned.
        if (idOfLastMonsterThatAttemptedToAct != idOfLastMonsterThatSuccessfullyActed)
        {
            Monster badMon = TryLinkActorFromDict(idOfLastMonsterThatAttemptedToAct) as Monster;
            if (badMon != null)
            {
                badMon.VerifyStatusesAndClearAllTemporaryData();
                if (badMon.GetObject() != null)
                {
                    BattleTextManager.NewText("?!", badMon.GetObject(), Color.yellow, 0.4f); // just a little nod to use devs in case something goes wrong "?!"
                }
                
            }
        }

        // Did a temporary destructible fail? Well get rid of it.
        if (idOfLastDestructibleThatAttemptedToAct != idOfLastDestructibleThatSuccessfullyActed)
        {
            Destructible badObject = TryLinkActorFromDict(idOfLastDestructibleThatAttemptedToAct) as Destructible;
            if (badObject != null)
            {
                if (badObject.summoner != null)
                {
                    badObject.summoner.RemoveSummon(badObject);
                }
                badObject.destroyed = true;
                badObject.isDestroyed = true;
                badObject.myMovable.FadeOutThenDie();
                MapMasterScript.activeMap.RemoveActorFromMap(badObject);
            }
        }

        // Now try running our TurnEndCleanup() to close out the turn that didn't finish. This will ensure our turn-end statuses (among other things) still trigger.
        // It will also process the dead queue, clear effects, and a number of other things we really want to do each turn.

        TurnData fakeyData = new TurnData();
        fakeyData.SetTurnType(TurnTypes.PASS);
        try { TurnEndCleanup(fakeyData); }
        catch (Exception)
        {
            // Oh boy. If the turn end cleanup failed, then there could be a problem on the current map, with the player statuses, etc. 
            // We are now in emergency alart mode because something is causing the game to consistently fail.

            // First, sanity check all hero statuses to remove null.
            heroPCActor.myStats.RemoveNullStatuses();

            // Then make sure we have a self and originating actor for everything.
            heroPCActor.myStats.VerifySelfAndOriginatingActorForAllStatuses();

            // Now remove every temporary hero status. Maybe a monster gave us a bad one.
            heroPCActor.myStats.RemoveAllTemporaryEffects();

            // Get rid of all our combat targets.
            heroPCActor.ClearCombatTargets();

            // Get rid of all our summons.
            heroPCActor.RemoveAllSummonedActorsOnDeath();

            // Heal us to full because we're nice.
            heroPCActor.myStats.HealToFull();
            UIManagerScript.RefreshPlayerStats();

            // Hope for the best.
            
        }
    }

    /// <summary>
    /// Something happened during the load process, what do we do? If we haven't tried a fix, we're BUSTED and should
    /// attempt to fix it. If this happened DURING the fix, 
    /// </summary>
    void SetLoadStatusDueToErrorInGameLoad()
    {
        if (gameLoadingState == GameLoadingState.ATTEMPTING_REBUILD_FIX)
        {
            gameLoadingState = GameLoadingState.HOPELESS;
        }
        else
        {
            gameLoadingState = GameLoadingState.BUSTED;
        }
    }
}

public partial class Monster : Fighter
{
    /// <summary>
    /// This makes sure all our statuses are valid, then clears any temporary effects, pathfinding data, target data, etc.
    /// </summary>
    public void VerifyStatusesAndClearAllTemporaryData()
    {
        myStats.RemoveNullStatuses();
        myStats.VerifySelfAndOriginatingActorForAllStatuses();
        myStats.RemoveAllTemporaryEffects();
        myBehaviorState = BehaviorState.NEUTRAL;
        myTarget = null;
        myTargetTile = Vector2.zero;
        UpdateMyAnchor();
        myMovable.RemoveParticleSystem("charging_skill");
        storeTurnData = null;
        ClearCombatAllies();
        ClearCombatTargets();
        tilePath = new List<MapTileData>();
        TryRemoveTargetingLine();
    }
}
