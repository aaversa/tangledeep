using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SpriteEffectMetadata
{
    public string spriteEffect;
    public float randomSpawnXMin;
    public float randomSpawnXMax;
    public float randomSpawnYMin;
    public float randomSpawnYMax;
}

[System.Serializable]
public class SpriteEffectSystem : MonoBehaviour
{
    public SpriteEffectMetadata[] mySpriteEffects;
    public int numEffectTriggers;
    public bool sequential;
    public bool sequentialAllInstances; // All instances of this system share the same index counter if enabled.
    public float animDelay;
    public bool overrideIndividualAudio;
    public float lifetime;

    public string refName;

    private float timeCounter;
    private int triggerCount;
    public bool alwaysFlipAxes;
    public bool randomlyFlipSpriteX;
    public bool randomlyFlipSpriteY;
    private int lastXAxis = -1;
    private int lastYAxis = 1;
    private float totalLifetime;

    public bool loopAtEnd;
    public bool setChildToParent;
    public bool spawnTowardEdges;
    public bool sparkleSystem;

    public float rotationVariance;

    public float enforceRotationOnChildren;

    int nextIndexToUse;

    static Dictionary<string, int> lastIndexUsedForAllSystems;

    void Start()
    {
        // Moved to enable
    }

    void OnEnable ()
    {
        if (!GameMasterScript.actualGameStarted) return;
        triggerCount = 0;
        timeCounter = 100.0f;
        if (overrideIndividualAudio)
        {
            AudioStuff aStuff = GetComponent<AudioStuff>();
            aStuff.PlayCue("Effect Fire");
        }
        totalLifetime = 0.0f;

        if (!lastIndexUsedForAllSystems.ContainsKey(refName))
        {
            lastIndexUsedForAllSystems.Add(refName, 0);
        }

        //calculatedSpriteFrameTime = totalLength / numEffectTriggers;
        //Debug.Log(calculatedSpriteFrameTime);
    }

    public static void Initialize()
    {
        if (lastIndexUsedForAllSystems == null)
        {
            lastIndexUsedForAllSystems = new Dictionary<string, int>();
        }
    }

    void OnDestroy()
    {

    }

    void Update()
    {
        if (GameMasterScript.applicationQuittingOrChangingScenes)
        {
            return;
        }

        totalLifetime += Time.deltaTime;
        if (totalLifetime >= lifetime)
        {
            if (!loopAtEnd)
            {
                GameMasterScript.ReturnToStack(gameObject, refName);
                return;
            }
            else
            {
                timeCounter = 100f;
                totalLifetime = 0f;
                triggerCount = 0;
            }
        }
        timeCounter += Time.deltaTime;
        if (timeCounter >= animDelay && triggerCount < numEffectTriggers)
        {
            timeCounter = 0.0f;

            // Pick an effect to spawn.

            SpriteEffectMetadata semd = null;
            GameObject effect = null;

            if (!sparkleSystem)
            {
                int indexForThisFrame = Random.Range(0, mySpriteEffects.Length);

                if (sequential)
                {
                    if (sequentialAllInstances)
                    {
                        nextIndexToUse = lastIndexUsedForAllSystems[refName];
                    }
                    semd = mySpriteEffects[nextIndexToUse];
                    indexForThisFrame = nextIndexToUse;
                    nextIndexToUse++;
                    if (nextIndexToUse >= mySpriteEffects.Length)
                    {
                        nextIndexToUse = 0;
                    }
                }
                else
                {                    
                    semd = mySpriteEffects[indexForThisFrame];
                }

                lastIndexUsedForAllSystems[refName] = nextIndexToUse;
                
                effect = GameMasterScript.TDInstantiate(semd.spriteEffect);

                if (enforceRotationOnChildren != 0f)
                {
                    float localRot = enforceRotationOnChildren;
                        localRot += UnityEngine.Random.Range(-16f, 16f);
                    effect.transform.Rotate(new Vector3(0, 0, localRot), Space.Self);
                    effect.GetComponent<SpriteEffect>().baseRotation = localRot;
                }
            }
            else {
				effect = GameMasterScript.TDInstantiate(mySpriteEffects[Random.Range(0,mySpriteEffects.Length)].spriteEffect);
                if (!overrideIndividualAudio)
                {
                    effect.GetComponent<AudioStuff>().PlayCue("Awake");
                }
                effect.transform.position = gameObject.transform.position;              
            }

            if (setChildToParent)
            {
                effect.transform.SetParent(gameObject.transform);
                effect.transform.position = Vector3.zero;
            }
            if (randomlyFlipSpriteX && Random.Range(0,2) == 0)
            {
                effect.GetComponent<SpriteRenderer>().flipX = true;
            }
            if (randomlyFlipSpriteY && Random.Range(0, 2) == 0)
            {
                effect.GetComponent<SpriteRenderer>().flipY = true;
            }
            AudioStuff adio = effect.GetComponent<AudioStuff>();
            if (overrideIndividualAudio)
            {
                if (adio != null)
                {
                    adio.enabled = false;
                }                
            }
            else
            {
                adio.PlayCue("Awake");
            }
            Vector3 position = transform.position;
            float randX = Random.Range(semd.randomSpawnXMin, semd.randomSpawnXMax);
            float randY = Random.Range(semd.randomSpawnYMin, semd.randomSpawnYMax);
            if (lastXAxis < 0 && randX < 0 && alwaysFlipAxes)
            {
                randX *= -1;
                lastXAxis *= -1;
            }
            else if (lastXAxis > 0 && randX > 0 && alwaysFlipAxes)
            {
                randX *= -1;
                lastXAxis *= -1;
            }
            if (lastYAxis < 0 && randY < 0 && alwaysFlipAxes)
            {
                randY *= -1;
                lastYAxis *= -1;
            }
            else if (lastYAxis > 0 && randY > 0 && alwaysFlipAxes)
            {
                randY *= -1;
                lastYAxis *= -1;
            }
            position.x = position.x + randX;
            position.y = position.y + randY;
            effect.transform.position = position;
            triggerCount++;
            if (triggerCount >= numEffectTriggers)
            {
                // Do nothing.
            }
        }
    }
}
