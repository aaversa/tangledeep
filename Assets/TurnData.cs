using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnData
{
    public Vector2 newPosition;
    TurnTypes turnType;
    public List<Actor> target;
    public List<int> targetIDs;
    public List<Vector2> targetPosition;
    public AbilityScript tAbilityToTry;
    public List<Vector2> targetTiles;
    public List<CombatResult> results;
    public List<Actor> affectedActors;
    public Vector2 centerPosition;
    public bool canHeroSeeThisTurn;
    public bool extraTurn;
    public Item usedItem
    {
        get
        {
            return itemUsedDuringThisTurnByHero;
        }
        set
        {
            itemUsedDuringThisTurnByHero = value;
        }
    }
    Item itemUsedDuringThisTurnByHero;
    public Directions direction;
    public Actor actorThatInitiatedTurn;
    bool playerActedThisTurn;

    public TurnData()
    {
        newPosition = new Vector2(0, 0);
        targetTiles = new List<Vector2>();
        target = new List<Actor>();
        targetPosition = new List<Vector2>();
        affectedActors = new List<Actor>();
        results = new List<CombatResult>();
        targetIDs = new List<int>();
        playerActedThisTurn = false;
    }

    public void SetPlayerActedThisTurn(bool value, int iThreadIndex)
    {
        if (!playerActedThisTurn && value)
        {
            GameMasterScript.heroPCActor.RefreshDamageTakenLastThreeTurns();
        }
        playerActedThisTurn = value;
        GameLogScript.EndTextBufferAndWrite();

    }

    public bool GetPlayerActedThisTurn()
    {
        return playerActedThisTurn;
    }

    public TurnTypes GetTurnType()
    {
        return turnType;
    }

    public void SetTurnType(TurnTypes tt)
    {
        TurnTypes original = turnType;
        turnType = tt;
        if (original == TurnTypes.ABILITY || tt == TurnTypes.ABILITY)
        {
            //Debug.Log("Turn type changed from " + original + " to " + tt + " on turn " + GameMasterScript.turnNumber + " executing? " + GameMasterScript.gmsSingleton.turnExecuting);
        }
        //Debug.Log("Set turntype to " + tt);
    }

    public Actor GetSingleTargetActor()
    {
        if (target.Count > 0)
        {
            return target[0];
        }
        return null;
    }

    public Actor GetTargetIfDestructibleAndOnlyDestructible()
    {
        if (target.Count > 0)
        {
            Actor dt = null;
            foreach (Actor act in target)
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) return null;
                if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) dt = act as Destructible;
            }

            return dt;
        }

        return null;
    }

    public bool IsDestructibleOnlyValidTarget()
    {
        Destructible dt = null;
        if (target.Count > 0)
        {
            foreach(Actor act in target)
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) return false;
                if (act.GetActorType() == ActorTypes.DESTRUCTIBLE) dt = act as Destructible;
            }

            if (dt != null) return true;
        }

        return false;
    }

    public void SetSingleTargetActor(Actor act)
    {
        target.Clear();
        target.Add(act);
    }

    public void SetSingleTargetPosition(Vector2 pos)
    {
        targetPosition.Clear();
        targetPosition.Add(pos);
    }

    public Vector2 GetSingleTargetPosition()
    {
        if (targetPosition.Count > 0)
        {
            return targetPosition[0];
        }
        return new Vector2(-1, -1);
    }
}