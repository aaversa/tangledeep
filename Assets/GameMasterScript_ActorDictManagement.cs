using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript
{
    public static bool AddActorToDict(Actor act)
    {

#if UNITY_EDITOR
        bool debug = false;
        if (debug)
        {
            //Debug.Log("Let's try adding " + act.actorUniqueID + " " + act.actorRefName + " " + act.displayName);
            if (dictAllActors.ContainsKey(act.actorUniqueID))
            {
                Debug.Log("Uh oh, " + act.actorUniqueID + " is already in actor dict. " + act.GetActorType() + " " + act.actorRefName + " " + act.dungeonFloor);
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    Debug.Log(mn.PrintCorralDebug());
                }

                Actor existing = dictAllActors[act.actorUniqueID];
                if (existing.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = existing as Monster;
                    Debug.Log("Existing is " + mn.PrintCorralDebug());
                }
                else
                {
                    Debug.Log("Existing is " + existing.actorRefName + " " + existing.displayName + " " + existing.dungeonFloor);
                }

            }
        }
#endif

        try
        {
            if (act.actorUniqueID == 0)
            {
                return false;
            }
            dictAllActors.Add(act.actorUniqueID, act);

#if UNITY_EDITOR
            //if (act.GetActorType() == ActorTypes.MONSTER && act.dungeonFloor == 150) Debug.Log("Adding " + act.actorUniqueID + " " + act.displayName + " " + act.actorRefName + " " + act.dungeonFloor);
#endif

        }
        catch (ArgumentException e)
        {

            /* Debug.Log("Trying to add actorname " + act.displayName + " ref " + act.actorRefName + " ID: " + act.actorUniqueID + " Floor: " + act.dungeonFloor + " but failed due to " + e);
            if (dictAllActors.ContainsKey(act.actorUniqueID))
            {
                Debug.Log("Existing actor is: " + dictAllActors[act.actorUniqueID].actorRefName + " " + dictAllActors[act.actorUniqueID].GetActorType());
            }  
            Debug.Log(e);*/

            return false;
        }
        return true;
    }

    public static void AssignActorID(Actor act, int startIndex = -1)
    {
        bool foundIndex = false;

        // Pass in a specific start index and we will ONLY search starting from that.
        if (startIndex != -1)
        {            
            while (!foundIndex)
            {
                if (dictAllActors.ContainsKey(startIndex))
                {
                    startIndex++;
                }
                else
                {
                    act.actorUniqueID = startIndex;
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = act as Monster;
                        if (mn.tamedMonsterStuff != null) mn.tamedMonsterStuff.monsterID = act.actorUniqueID;
                    }
                    return;
                }
            }
        }

        if (allActorIDs == 0) allActorIDs++;
        while (!foundIndex)
        {
            if (dictAllActors.ContainsKey(allActorIDs))
            {
                allActorIDs++;
            }
            else
            {
                act.actorUniqueID = allActorIDs;

                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.tamedMonsterStuff != null) mn.tamedMonsterStuff.monsterID = act.actorUniqueID;
                }

                //Debug.Log("Assigned " + act.actorRefName + " ID of " + act.actorUniqueID);
                foundIndex = true;
                allActorIDs++;
            }
        }
    }

    public Actor TryLinkActorFromDict(int id)
    {
        Actor findActor;
        if (dictAllActors.TryGetValue(id, out findActor))
        {
            return findActor;
        }
        else
        {
            if (id != 0 && !MetaProgressScript.loadingMetaProgress)
            {
#if UNITY_EDITOR
                Debug.Log("Couldn't link this actor ID: " + id);
#endif
            }
            return null;
        }
    }

    public static bool DoesActorExistByID(int actorID)
    {
        return dictAllActors.ContainsKey(actorID);
    }

    static void ClearActorDict()
    {
        if (dictAllActors == null)
        {
            dictAllActors = new Dictionary<int, Actor>();
        }
        if (allLoadedNPCs == null)
        {
            allLoadedNPCs = new List<NPC>();
        }
        dictAllActors.Clear();
        allLoadedNPCs.Clear();
        //Debug.Log("Cleared actor dict.");
    }
}