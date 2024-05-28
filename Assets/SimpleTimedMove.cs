using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleTimedMove : MonoBehaviour {

    RectTransform rt;
    Transform normalT;
    public float moveTime;
    public float startPos;
    public float endPos;

    public float startXPos;
    public float endXPos;

    public bool onlyMoveWhenInitialized;
    public bool moveX;

    float timeStarted;
    bool moving;
    public CameraScrollTypes scrollType;
    Vector3 pos;

    public bool useRegularTransform;

    bool continuousMovement;
    float xThresholdToRestart;
    float secondsPerUnit;
    float widthOfObject;

    float moveAmountBuffer;

    private float fCheatingAss30FPSLastMarker;
	// Use this for initialization
	void Start () {        
        if (useRegularTransform)
        {
            normalT = GetComponent<Transform>();
        }
        else
        {
            rt = GetComponent<RectTransform>();
        }
        if (!onlyMoveWhenInitialized)
        {
            BeginMovement(startPos, startXPos, endPos, endXPos, moveTime);
        }
	    fCheatingAss30FPSLastMarker = Time.realtimeSinceStartup;
	}

    public void BeginContinuousMovement(float spUnit, float minX, float woo)
    {
        moving = true;
        continuousMovement = true;
        secondsPerUnit = spUnit;
        xThresholdToRestart = minX;
        pos = transform.localPosition;
        widthOfObject = woo;
    }

    public void EndContinuousMovement()
    {
        moving = false;
        continuousMovement = false;
    }

    public void BeginMovement (float startY, float startX, float endY, float endX, float nTime)
    {
        continuousMovement = false;
        moving = true;
        timeStarted = Time.realtimeSinceStartup;
        pos = transform.localPosition;
        startPos = startY;
        startXPos = startX;
        endPos = endY;
        endXPos = endX;
        moveTime = nTime;
    }
	
    public void FinishMovement()
    {
        moving = false;
        pos.y = endPos;
        pos.x = endXPos;        
        if (startXPos == endXPos)
        {
            if (useRegularTransform)
            {
                pos.x = normalT.localPosition.x;
            }
            else
            {
                pos.x = rt.localPosition.x;
            }
            
        }
        if (useRegularTransform)
        {
            normalT.localPosition = pos;
        }
        else
        {
            rt.localPosition = pos;
        }
        
    }

    // Update is called once per frame
    void Update () {
        if (!moving) return;

        if (continuousMovement)
        {
            float xAmount = Time.deltaTime / secondsPerUnit;

            moveAmountBuffer += xAmount;

            // Avoid sub pixel amounts.
            if (Mathf.Abs(moveAmountBuffer) < 0.015625f)
            {
                return;
            }
            else
            {
                if (moveAmountBuffer < 0)
                {
                    moveAmountBuffer = -0.015625f;
                }
                else
                {
                    moveAmountBuffer = 0.015625f;
                }
            }

            pos.x += moveAmountBuffer;
            moveAmountBuffer = 0f;

            if (pos.x <= xThresholdToRestart)
            {
                pos.x = widthOfObject; 
            }
        }
        else
        {
            float fDelta = Time.realtimeSinceStartup - fCheatingAss30FPSLastMarker;
            if (fDelta < 0.032f)
            {
                return;
            }
            fCheatingAss30FPSLastMarker += 0.032f; 
            float percentComplete = (fCheatingAss30FPSLastMarker - timeStarted) / moveTime;

            if (percentComplete >= 1.0f)
            {
                percentComplete = 1.0f;
            }

            float yValue = 0f;
            float xValue = 0f;

            switch (scrollType)
            {
                case CameraScrollTypes.LERP:
                    yValue = Mathf.Lerp(startPos, endPos, percentComplete);
                    xValue = Mathf.Lerp(startXPos, endXPos, percentComplete);
                    break;
                case CameraScrollTypes.SINERP:
                    yValue = Mathfx.Sinerp(startPos, endPos, percentComplete);
                    xValue = Mathfx.Sinerp(startXPos, endXPos, percentComplete);
                    break;
                case CameraScrollTypes.CLERP:
                    yValue = Mathfx.Clerp(startPos, endPos, percentComplete);
                    xValue = Mathfx.Clerp(startXPos, endXPos, percentComplete);
                    break;
                case CameraScrollTypes.SMOOTHDAMP:
                    yValue = Mathfx.SmoothStep(startPos, endPos, percentComplete);
                    xValue = Mathfx.SmoothStep(startXPos, endXPos, percentComplete);
                    break;
                case CameraScrollTypes.HERMITE:
                    yValue = Mathfx.Hermite(startPos, endPos, percentComplete);
                    xValue = Mathfx.Hermite(startXPos, endXPos, percentComplete);
                    break;
                case CameraScrollTypes.BERP:
                    yValue = Mathfx.Berp(startPos, endPos, percentComplete);
                    xValue = Mathfx.Berp(startXPos, endXPos, percentComplete);
                    break;
            }

            if (percentComplete >= 1.0f)
            {
                moving = false;
            }

            pos.y = yValue;
            if (moveX)
            {
                pos.x = xValue;
            }
        }

        if (useRegularTransform)
        {
            normalT.localPosition = pos;
        }
        else
        {
            rt.localPosition = pos;
        }

        if (GameMasterScript.actualGameStarted)
        {
            //Debug.Log("New pos: " + pos + " " + percentComplete);
        }        
    }
}
