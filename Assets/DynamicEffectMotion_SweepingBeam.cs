using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

//Attatch one of these to an effect prefab in order to give it some
//scripted motion and changed behavior in the world. 
public class DynamicEffectMotion_SweepingBeam : MonoBehaviour
{
    [Tooltip("How far out from the center are we allowed to sweep?")]
    public float MaxRadiusFromCenterOfTile = 0.5f;

    [Tooltip("How many seconds should this effect run for?")]
    public float Lifetime;

    public bool DestroyOnEnd;

    [Header("Animations")]
    public Animatable myAnimatable;
    public string StartAnim;
    public string LoopAnim;
    public string EndAnim;

    private bool bActive;
    private bool bRunOnNextUpdate;

    // Use this for initialization
    void Start ()
	{
        bRunOnNextUpdate = true;
	}

    void OnEnable()
    {
        bRunOnNextUpdate = true;
    }

    void Update()
    {
        if (GameMasterScript.actualGameStarted && !bActive && bRunOnNextUpdate)
        {
            bActive = true;
            bRunOnNextUpdate = false;
            GameMasterScript.StartWatchedCoroutine(Motion_RandomSweepInSingleTile());
        }
    }

    IEnumerator Motion_RandomSweepInSingleTile()
    {
        float fTime = 0f;
        Vector2 vTileCenter = transform.position;

        //play optional start anim
        if (myAnimatable != null && !string.IsNullOrEmpty(StartAnim))
        {
            myAnimatable.SetAnim(StartAnim);
            myAnimatable.OverrideCompletionBehavior("Stop");

            while (!myAnimatable.AnimComplete())
            {
                yield return null;
            }
        }


        //loop main anim
        if (myAnimatable != null && !string.IsNullOrEmpty(LoopAnim))
        {
            myAnimatable.SetAnim(LoopAnim);
            myAnimatable.OverrideCompletionBehavior("Loop");
        }

        //for as long as we are alive
        while (fTime < Lifetime)
        {
            //pick a destination point
            Vector2 vDestinationPoint =
                vTileCenter + (Random.insideUnitCircle.normalized * Random.value * MaxRadiusFromCenterOfTile);

            Vector2 vStartPoint = transform.position;

            //move to it
            float fMovementMaxTime = Random.Range(0.3f, 0.7f);
            float fMovementCurrentTime = 0f;

            //inner loop, keep track of both the full time and our own movetime.
            while (fMovementCurrentTime < fMovementMaxTime &&
                   fTime < Lifetime)
            {
                Vector2 vPosVector2 = Vector2.Lerp( vStartPoint, vDestinationPoint, fMovementCurrentTime / fMovementMaxTime);
                transform.position = new Vector3(vPosVector2.x, vPosVector2.y, transform.position.z);

                fTime += Time.deltaTime;
                fMovementCurrentTime += Time.deltaTime;

                yield return null;
            }

        }

        //optionally, end the loop and close the anim out if we have an end anim
        if (myAnimatable != null)
        {
            //if we aren't done because we're looping
            if (!myAnimatable.AnimComplete())
            {
                //stop at the next end point
                myAnimatable.OverrideCompletionBehavior("Stop");

                //wait for it.
                while (!myAnimatable.AnimComplete())
                {
                    yield return null;
                }
            }

            //play an end anim if we have one
            if (!string.IsNullOrEmpty(EndAnim))
            {
                myAnimatable.SetAnim(EndAnim);
                myAnimatable.OverrideCompletionBehavior("Stop");

                while (!myAnimatable.AnimComplete())
                {
                    yield return null;
                }
            }
        }

        //Heads up, this means we must have a SpriteEffect on our object <3    
        if (DestroyOnEnd)
        {
            GetComponent<SpriteEffect>().CleanUpAndReturnToStack();
        }

        bActive = false;
    }
}
