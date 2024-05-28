using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public partial class GameMasterScript
{
    public static GameObject GetResourceByRef(string resourceRef)
    {
        GameObject objOut = null;
        if (coreResources.TryGetValue(resourceRef, out objOut))
        {
            if (objOut == null)
            {
                if (Debug.isDebugBuild) Debug.LogError("Resource " + resourceRef + " was stored into the coreResources list as null.");
            }
            return objOut;
        }
        else
        {
#if UNITY_EDITOR
            foreach (KeyValuePair<string, GameObject> kvp in coreResources)
            {
                if (kvp.Key.ToLowerInvariant() == resourceRef)
                {
                    return kvp.Value;
                }
            }
#endif
            if (Debug.isDebugBuild) Debug.Log("Couldn't find resource " + resourceRef);
            return null;
        }
    }

    IEnumerator CreateResourcePools()
    {
        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            musicManager.FillStackWithTracksToLoad();
        }

        loadGame_creatingResourcePools = true;

        pooledObjectsWithSpriteEffects = new HashSet<string>();
        pooledObjectsWithAnimatables = new HashSet<string>();
        pooledObjectsWithMovables = new HashSet<string>();
        //for our convenience, here is an object we can parent this new
        //instance to so that the editor window isn't a long streak of poop
        goPooledObjectHolder = new GameObject();
        goPooledObjectHolder.name = "Pooled Objects";
        goPooledObjectHolder.SetActive(false);

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
   {
        float fDelayTime = Time.realtimeSinceStartup;
        foreach (var sArray in resourcesToLoadAfterMainSceneThatWeUsedToPreload)
        {
            TryPreloadResourceInstant(sArray[0], sArray[1]);
            if (Time.realtimeSinceStartup - fDelayTime > MIN_FPS_DURING_LOAD)
            {                
                yield return null;
                fDelayTime = Time.realtimeSinceStartup;
            }
        }
}

        dictObjectPools = new Dictionary<string, Stack<GameObject>>(800);
        GameObject pooled = null;
        float fFrameTimer = Time.realtimeSinceStartup;
        foreach (var kvp in coreResources)
        {
            //if we stored a null value in coreResources, whoops bad.
            if (kvp.Value == null)
            {
                continue;
            }

            string refName = kvp.Key;

            if (!dictObjectPools.ContainsKey(refName))
            {
                // Figure out max stack size to prevent senseless resizing and reallocation
                int maxAmountToPool = 5; // Default amount

                // Now storing key data about certain objects in our master dictionary
                // The exact values depend on how the effects/objects are used ingame
                TDPoolingData tdp;
                if (TDPoolingData.dictPooledObjects.TryGetValue(refName, out tdp))
                {
                    maxAmountToPool = tdp.quantityToPool;
                }
                Stack<GameObject> ns = new Stack<GameObject>(maxAmountToPool);
                dictObjectPools.Add(refName, ns);

                //Debug.Log("Created pool and am instantiating " + refName);

                for (int i = 0; i < maxAmountToPool; i++)
                {
                    pooled = Instantiate(kvp.Value);
                    pooled.SetActive(false);
                    ns.Push(pooled);
                    if (pooled.tag != "do_not_reparent") // Children within prefabs should not be reparented.
                    {
                        pooled.transform.SetParent(goPooledObjectHolder.transform);
                    }
                }

                AddObjectToComponentSubPools(pooled, refName);
            }

            //allow for 30fps, this is load time anyway.
            if (Time.realtimeSinceStartup - fFrameTimer >= GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                yield return null;
                fFrameTimer = Time.realtimeSinceStartup;
                IncrementLoadingBar(ELoadingBarIncrementValues.small);
            }
        }

        pooled = GameObject.Find("BattleTextManager");
        pooled.GetComponent<BattleTextManager>().InitializeBTM();
        resourcePoolsCreated = true;
        loadGame_creatingResourcePools = false;
#if UNITY_EDITOR
        //Debug.Log("<color=green>Created all resource pools!</color>");
#endif
    }


    public static void ReturnToStack(GameObject go, string refName, string[] strOrMaybeOneOfTheseRef = null, bool returnChildren = true, bool debugStackReturn = false)
    {
        if (gmsSingleton == null || gmsSingleton.dictObjectPools == null)
        {
            // abandon_thread.jpg
            Destroy(go);
            return; //in shame
        }

        // Check our collection of stacks to see if we have the one we instantiated from
        Stack<GameObject> getStack;
        gmsSingleton.dictObjectPools.TryGetValue(refName, out getStack);

        // If not, maybe it came from a different stack, because sometimes instantiate uses information we
        // no longer have access to on destroy, so we just guess.
        // We just guess.
        if (getStack == null && strOrMaybeOneOfTheseRef != null)
        {
            foreach (var s in strOrMaybeOneOfTheseRef)
            {
                gmsSingleton.dictObjectPools.TryGetValue(s, out getStack);
                if (getStack != null)
                {
                    break;
                }
            }
        }

        // Concurrent scripts might try to stack the same object twice, we must prevent this.
        if (go.transform.parent == goPooledObjectHolder.transform)
        {
            // But the Contains method could be pricey. Maybe there's a better way
            if (getStack.Contains(go))
            {
                if (debugStackReturn)
                {
                    if (Debug.isDebugBuild) Debug.Log(go.name + " is already stacked, probably?");    
                }

                go.SetActive(false);
                return;
            }
        }


        //Did we find a stack? Then restackalack this object
        if (getStack != null)
        {
            if (go.tag != "do_not_reparent") // Children within prefabs should not be reparented.
            {
                go.transform.SetParent(goPooledObjectHolder.transform);
            }

            getStack.Push(go);

            go.SetActive(false);

            if (debugStackReturn)
            {
                if (Debug.isDebugBuild) Debug.Log(go.name + " is being returned to stack");
            }

            //Debug.Log("Returning " + go.name + " to stack");

            // Experimental: When we are returned to stack, make sure our CHILDREN are returned to their stacks as well.
            if (returnChildren)
            {
                foreach (Transform childTransform in go.transform)
                {
                    if (childTransform == go.transform) continue;
                    if (childTransform.gameObject.tag == "do_not_reparent") continue;
                    bool doReturnChildren = true;
                    if (childTransform.gameObject.name.Contains("OtherSpriteText"))
                    {
                        doReturnChildren = false;
                    }
                    ReturnToStack(childTransform.gameObject, childTransform.gameObject.name.Replace("(Clone)", String.Empty), null, doReturnChildren);
                }
            }
        }
        else //rip performance
        {
            // Oooone more try, maybe we used the wrong name?
            string stripName = go.name.Replace("(Clone)", String.Empty);
            if (refName != stripName)
            {
                ReturnToStack(go, stripName, null, true, debugStackReturn);
                return;
            }
            Destroy(go);
            if (Debug.isDebugBuild) Debug.Log("Destroyed " + go.name + " because we couldn't find a stack.");
        }

    }

    // If "forceNew" is true, don't use pooling.
    public static GameObject TDInstantiate(string refName, bool forceNew = false)
    {
        Stack<GameObject> getStack;

        if (!GameMasterScript.gmsSingleton.resourcePoolsCreated || forceNew)
        {
            //Debug.Log(refName + " has no resource pool.");
            return GameObject.Instantiate(GetResourceByRef(refName));
        }

        if (gmsSingleton.dictObjectPools.TryGetValue(refName, out getStack))
        {
            if (getStack.Count == 0)
            { // Nothing in the effect stack.
#if UNITY_EDITOR
                //Debug.Log("Not enough in the stack when instantiating " + refName);
#endif
                GameObject nObj = GameObject.Instantiate(GetResourceByRef(refName));
                if (nObj == null)
                {
                    //Debug.Log("WARNING: Tried to instantiate " + refName + " but it was NULL.");
                    return null;
                }
                nObj.SetActive(false);
                getStack.Push(nObj);

            }
            // At least 1 in the stack.
            GameObject returnObj = getStack.Pop();
            if (returnObj == null) // Tried retrieving but it was null.
            {
                GameObject nObj = GameObject.Instantiate(GetResourceByRef(refName));
                if (nObj == null)
                {
#if UNITY_EDITOR
                    Debug.Log("WARNING: Tried to instantiate " + refName + " but it was NULL.");
#endif
                    return null;
                }
                nObj.SetActive(true); // this was false before, but shouldn't we ALWAYS return an active object?
                                      //getStack.Push(nObj); Don't push this on the stack, since we're returning it.
                                      // It should never be both in the stack AND ingame!
                return nObj;

            }
            else // Popping something from stack but it wasn't null.
            {
			
if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                SpriteRenderer sr = returnObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.material = spriteMaterialUnlit;
                }
}

                if (pooledObjectsWithSpriteEffects.Contains(refName))
                {
                SpriteEffect se = returnObj.GetComponent<SpriteEffect>();

                if (se != null)
                {
                    se.animInitialized = false;
                    se.parentAbility = null;
                    se.spriteParent = null;
                    }
                }

                if (pooledObjectsWithAnimatables.Contains(refName))
                {
                Animatable anm = returnObj.GetComponent<Animatable>();
                if (anm != null)
                {
                    anm.ResetAnim();
                    }
                }
                if (pooledObjectsWithMovables.Contains(refName))
                {
                Movable mv = returnObj.GetComponent<Movable>();
                if (mv != null)
                {
                    mv.fadingIn = false;
                    mv.fadingOut = false;
                    mv.SetBShouldBeVisible(true);
                }

                }

                    returnObj.SetActive(true);

                if (returnObj.tag != "do_not_reparent") // Children within prefabs should not be reparented.
                {
                    returnObj.transform.SetParent(null);
                }

                return returnObj;
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("Pool not found: " + refName);
#endif
            return GameObject.Instantiate(GetResourceByRef(refName));
        }
    }

    // This is a safer destroy function to deal with the following scenario:
    // Actor A is killed earlier in the turn and its object is returned to stack. Actor B is spawned at the same time.
    // Actor B uses Actor A's newly returned object. But then, for some reason, we have a leftover coroutine double checking that A was destroyed
    // This is normally a good thing, however the coroutine sees that the object is alive and kills it again... 
    // ... but that object belongs to B now! Now B is invisible!
    public static void ReturnActorObjectToStack(Actor act, GameObject go, string refName = "")
    {
        if (go != null)
        {
            // Verify that go's movable and/or animatable component is still act. If not, then it has changed hands
            // and we must exit. If it has no movable/animatable, there is probably nothing to worry about.
            bool verified = true;
            Movable mv = go.GetComponent<Movable>();
            if (mv != null && mv.GetOwner() != act)
            {
                verified = false;
            }
            if (verified)
            {
                Animatable anm = go.GetComponent<Animatable>();
                if (anm != null && anm.GetOwner() != act)
                {
                    verified = false;
                }
            }
            if (verified)
            {
                TryReturnChildrenToStack(go);
                string maybeRefName = go.name.Replace("(Clone)", String.Empty);
                if (refName != "")
                {
                    maybeRefName = refName;
                }
                ReturnToStack(go, maybeRefName);
            }
            else
            {
#if UNITY_EDITOR
                //Debug.Log(go.name + " belongs to someone else now!");
#endif
            }
        }
    }

    public IEnumerator DestroyObject(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        if (go != null)
        {

            TryReturnChildrenToStack(go);
            string maybeRefName = go.name.Replace("(Clone)", String.Empty);
            ReturnToStack(go, maybeRefName);
        }
    }

    public static bool TryReturnChildrenToStack(GameObject go)
    {
        if (go == null || go.transform == null)
        {
            Debug.Log("Object or transform is null.");
            return false;
        }
        SpriteEffect se;
        SpriteEffectSystem ses;
        bool stackable = false;
        foreach (Transform child in go.transform)
        {
            se = child.gameObject.GetComponent<SpriteEffect>();
            if (se != null)
            {
                if (child.tag != "do_not_reparent") // Children within prefabs should not be reparented.
                {
                    child.gameObject.transform.SetParent(null);
                }
                ReturnToStack(child.gameObject, se.refName);
                stackable = true;
            }
            ses = child.gameObject.GetComponent<SpriteEffectSystem>();
            if (ses != null)
            {
                //Debug.Log(go.name + " has an effect system");
                foreach (Transform sesChild in ses.gameObject.transform)
                {
                    ReturnToStack(sesChild.gameObject, sesChild.GetComponent<SpriteEffect>().refName);
                }
                //Destroy(ses.gameObject);
                string maybeRefName = ses.gameObject.name.Replace("(Clone)", String.Empty);
                ReturnToStack(ses.gameObject, maybeRefName);
                stackable = true;
            }
        }

        return stackable;
    }

    // Doesn't require a SpriteEffect, so it's more versatile.
    public IEnumerator WaitThenReturnObjectToStack(GameObject go, string refName, float time)
    {
        yield return new WaitForSeconds(time);
        ReturnToStack(go, refName);
    }

    public IEnumerator WaitThenReturnEffectToStack(SpriteEffect se, float time)
    {
        yield return new WaitForSeconds(time);
        // se can apparently be null, so verify this first.
        if (se != null)
        {
            ReturnToStack(se.gameObject, se.refName);
        }
    }

    static void AddObjectToComponentSubPools(GameObject pooled, string refName)
    {
        if (pooled.GetComponent<Movable>() != null)
        {
            pooledObjectsWithMovables.Add(refName);
        }
        if (pooled.GetComponent<Animatable>() != null)
        {
            pooledObjectsWithAnimatables.Add(refName);
        }
        if (pooled.GetComponent<SpriteEffect>() != null)
        {
            pooledObjectsWithSpriteEffects.Add(refName);
        }
    }
}