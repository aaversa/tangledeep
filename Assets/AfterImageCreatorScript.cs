using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AfterImageCreatorScript : MonoBehaviour {

    int numberOfImages;
    public float imageFadeTime;
    
    float animationTime;
    float animationDistance;

    bool initialized;
    Vector2 startPoint;
    Vector2 endPoint;
    float[] timeThresholds;

    float timeAtCreationStart;
    int indexOfAfterImage;

    private bool bDestroyOnEnd;

    SpriteRenderer sourceSR;
    MovementTypes moveType;

    public SpriteEffect mySpriteEffect;

	public void Initialize(Vector2 ePoint, float aTime, float aDistance, SpriteRenderer source, bool bDestroyAfterTimeUp = true, float fFadeTime = -1.0f, MovementTypes mType = MovementTypes.LERP, int forceNumImages = 0)
    {
        moveType = mType;
        startPoint = transform.position; // NOT local by design
        if (fFadeTime > 0f)
        {
            imageFadeTime = fFadeTime;
        }
        numberOfImages = (int)(aDistance * 0.5f) + 1;
        if (numberOfImages < 4) numberOfImages = 4;

        if (forceNumImages > 0)
        {
            numberOfImages = forceNumImages;
        }

        endPoint = ePoint;

        animationTime = aTime;
        animationDistance = aDistance;

        timeThresholds = new float[numberOfImages];

        //Debug.Log("Going from " + startPoint + " to " + endPoint);

        for(int i = 0; i < numberOfImages; i++)
        {
            float pComplete = (float)i / numberOfImages;            
            Vector2 lerpPos = Vector2.Lerp(startPoint, endPoint, pComplete);
            if (mType == MovementTypes.SLERP)
            {
                lerpPos = Vector3.Slerp(startPoint, endPoint, pComplete);
            }

                float timeThreshold = aTime * pComplete;
            timeThresholds[i] = timeThreshold;
            //Debug.Log("Time threshold " + i + " is " + timeThreshold);
        }

        initialized = true;
        timeAtCreationStart = Time.time;
        indexOfAfterImage = 0;
        sourceSR = source;

        if (sourceSR == null || !sourceSR.gameObject.activeSelf)
        {
            initialized = false;
            GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnEffectToStack(mySpriteEffect, imageFadeTime * 3f));
        }

        bDestroyOnEnd = bDestroyAfterTimeUp;
    }

    void Update()
    {
        if (!initialized) return;

        if (indexOfAfterImage >= numberOfImages)
        {
            initialized = false;
            if (bDestroyOnEnd)
            {
                // todo: Get rid of this object after X seconds.   
                GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnEffectToStack(mySpriteEffect, imageFadeTime * 3f));
            }
            return;
        }

        float adjustedTime = Time.time - timeAtCreationStart;

        if (adjustedTime >= timeThresholds[indexOfAfterImage])
        {
            if (indexOfAfterImage == 0)
            {
                indexOfAfterImage++;
                return;
            }
            // Create a subobject
            GameObject afterImage = GameMasterScript.TDInstantiate("SingleAfterImagePrefab");

            // Find desired position
            float pComplete = adjustedTime / animationTime;
            //Debug.Log("Percent complete is " + pComplete + " for threshold " + timeThresholds[indexOfAfterImage] + " go from " + startPoint + " to " + endPoint);

            Vector2 correctPosition = Vector2.Lerp(startPoint, endPoint, pComplete);
            if (moveType == MovementTypes.SLERP)
            {
                correctPosition = Vector3.Slerp(startPoint, endPoint, pComplete);
            }
            else if (moveType == MovementTypes.SMOOTH)
            {
                correctPosition = transform.position;
            }

            afterImage.transform.position = correctPosition;
            //Debug.Log("At index " + indexOfAfterImage + " we appear at " + correctPosition);

            afterImage.GetComponent<AfterImageEffectScript>().Initialize(imageFadeTime, sourceSR);
            
            if (moveType == MovementTypes.SMOOTH)
            {
                afterImage.transform.localEulerAngles = transform.parent.localEulerAngles;
            }

            indexOfAfterImage++;
        }


    }

}
