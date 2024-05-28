using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;

public partial class GameMasterScript
{

    public static void AddToDeadQueue(Actor act, bool forceAddAliveActorToDeadQueue = false)
    {
        if (act == null || deadQueue == null)
        {
            return;
        }

        //Debug.Log("Adding " + act.actorRefName + " " + act.GetActorType() + " " + act.actorUniqueID + " to dead queue");

        gmsSingleton.SetTempGameData("deadqueue_process", 1);

        if (!act.isInDeadQueue)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (SharaModeStuff.IsMonsterFinalBoss(mn))
                {
                    return;
                }
                if (mn.myStats.GetCurStat(StatTypes.HEALTH) > 1f && !forceAddAliveActorToDeadQueue)
                {
                
                    //Debug.Log(mn.actorRefName + " isn't dead but is being added to queue...");

                    if (mn.objectSet && mn.myMovable != null)
                    {
                        mn.myMovable.FadeOutThenDie();
                    }
                    return;
                }
                if (mn.destroyed)
                {
                    // We're keeping this monster around for some reason?
                    return;
                }
            }

            /* if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Debug.Log(act.actorUniqueID + " " + act.turnsToDisappear + " is dead");
            } */

            deadQueue.Enqueue(act);
            act.isInDeadQueue = true;
        }
    }

    public void ProcessDeadQueue(Map mapToProcess)
    {
        if (mapToProcess == null)
        {
            Debug.Log("Can't process dead queue with null map.");
            return;
        }
        // Excessive but... Let's check all monsters.
        for (int i = 0; i < mapToProcess.monstersInMap.Count; i++)
        {
            if (!mapToProcess.monstersInMap[i].myStats.IsAlive())
            {
                AddToDeadQueue(mapToProcess.monstersInMap[i]);
                //Debug.Log(mapToProcess.monstersInMap[i].actorRefName + " is dead.");
            }
        }

        int tries = 0;
        while (deadQueue.Count > 0 && tries <= 999)
        {
            tries++;
            Actor act = deadQueue.Dequeue();
            act.isInDeadQueue = false;
            act.MarkAsDestroyed();
            switch (act.GetActorType())
            {
                case ActorTypes.MONSTER:
                    CombatResultsScript.CheckCombatResult(CombatResult.MONSTERDIED, act, mapToProcess);
                    Monster mon = act as Monster;
                    mon.SymmetricalRemoveTargetsAndAllies();
                    break;

                case ActorTypes.HERO:
                    CombatResultsScript.CheckCombatResult(CombatResult.PLAYERDIED, act, mapToProcess);
                    break;

                case ActorTypes.DESTRUCTIBLE:
                    mapToProcess.RemoveActorFromMap(act);
                    GameObject dgo = act.GetObject();
                    
                    //Debug.Log(act.actorRefName + " " + act.actorUniqueID + " is dying. " + act.turnsToDisappear + " " + act.maxTurnsToDisappear + " " + act.actOnlyWithSummoner + " " + turnNumber);
                    
                    if (act.actorRefName == "obj_mudtile")
                    {
                        MapTileData mtd = mapToProcess.GetTile(act.GetPos());
                        mtd.RemoveTag(LocationTags.SUMMONEDMUD);
                    }

                    if (dgo != null && dgo.activeSelf)
                    {
                        act.myMovable.FadeOutThenDie();
                        //dgo.GetComponent<Movable>().FadeOutThenDie();
                    }
                    if (act.summoner != null)
                    {
                        act.summoner.RemoveSummon(act);
                        if (act.GetActorType() == ActorTypes.MONSTER)
                        {
                            StringManager.SetTag(0, act.displayName);
                            GameLogScript.GameLogWrite(StringManager.GetString("log_summon_disappear"), act);
                        }
                    }

                    DTPooling.ReturnToPool(act as Destructible);

                    break;
            }
        }
        if (tries >= 999)
        {
            Debug.Log("Broke dead queue while loop");
        }

        SetTempGameData("deadqueue_process", 0);
    }

    public void DestroyActor(Actor act)
    {
        //Debug.Log("Try destroy " + act.actorRefName);
        if (!act.destroyed)
        {
            act.MarkAsDestroyed(ignoreHealth: true);
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                mn.deathProcessed = true;
                mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
                if (heroPCActor.CheckTarget(act))
                {
                    heroPCActor.RemoveTarget(act);
                }
                if (heroPCActor.CheckAlly(act))
                {
                    heroPCActor.RemoveAlly(act);
                }
                MapMasterScript.activeMap.RemoveActorFromLocation(mn.positionAtStartOfTurn, mn);
                mn.RemoveAllSummonedActorsOnDeath(); // Make sure when summoned monsters disappear, we take THEIR summons with us as needed
                if (mn.actorfaction == Faction.PLAYER && heroPCActor.CanSeeActor(mn))
                {
                    CombatResultsScript.DoMonsterDeathFX(mn);
                }
            }
            MapMasterScript.activeMap.RemoveActorFromLocation(act.previousPosition, act);

            mms.RemoveActorFromMap(act);
            GameObject dgo = act.GetObject();
            if (dgo != null)
            {
                dgo.GetComponent<Movable>().FadeOutThenDie();
            }
            if (act.summoner != null)
            {
                act.summoner.RemoveSummon(act);
                /* if (act.summoner == heroPCActor)
                {
                    UIManagerScript.UpdatePetInfo();
                } */
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    StringManager.SetTag(0, act.displayName);
                    GameLogScript.GameLogWrite(StringManager.GetString("log_summon_disappear"), act);
                }
                UIManagerScript.UpdatePetInfo();
            }

            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                DTPooling.ReturnToPool(act as Destructible);
            }
        }
    }

    public IEnumerator WaitToDestroyActorObject(Actor act, GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        ReturnActorObjectToStack(act, go);
    }


    public void WaitThenDestroyObject(GameObject go, float time)
    {
        StartCoroutine(WaitToDestroyObject(go, time));
    }

    IEnumerator WaitToDestroyObject(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        string maybeRefName = go.name.Replace("(Clone)", String.Empty);
        ReturnToStack(go, maybeRefName);
    }

    /// <summary>
    /// Start a coroutine that delays the game for a period of time and then destroys the actor.
    /// </summary>
    /// <param name="focus"></param>
    /// <param name="delayTime"></param>
    /// <param name="optionalEffectRef"></param>
    public static void WaitThenDestroyActorWithVFX(Actor focus, float delayTime, string optionalEffectRef = null)
    {
        StartWatchedCoroutine(gmsSingleton.WaitToDestroyActorWithVFX(focus, delayTime, optionalEffectRef));
    }

    IEnumerator WaitToDestroyActorWithVFX(Actor focus, float delayTime, string optionalEffectRef = null)
    {
        yield return new WaitForSeconds(delayTime);

        if (!string.IsNullOrEmpty(optionalEffectRef))
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(focus.GetPos(), optionalEffectRef, null, true);
        }

        DestroyActor(focus);
    }

    public static void RemoveActorFromDeadQueue(Actor removeAct)
    {
        List<Actor> prevDead = deadQueue.ToList();

        deadQueue.Clear();

        foreach (Actor act in prevDead)
        {
            if (act != removeAct)
            {
                deadQueue.Enqueue(act);
                act.isInDeadQueue = true;
            }
            else
            {
                act.isInDeadQueue = false;
            }
        }
    }
}