using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HologramEffect : MonoBehaviour {

    public Material myHologramMat;
    public Animatable myAnim;

    float timeAtLastReset = 0f;
    public float blueCycleTime = 2.0f;
    bool increasing = true;

    public float chanceOfPixelRowShift = 0.2f;
    public float chanceOfWholeImageShift = 0.1f;
    public float chanceOfHoldAnimFrame = 0.1f;
    public float rowShiftAmount = 0.15f;
    public float holdTime = 0.5f;
    public float defaultTime = 0.12f;
    float timeAtHold;
    bool holding = false;

    public int shiftHoldFrames = 3;
    public float lastShiftAmount = 0f;
    public int maxDistortRows = 5;
    int framesHeld = 0;

    public bool initialized;

    string[] yMinNames;
    string[] yMaxNames;
    float[] yMinDistortValues;
    float[] yMaxDistortValues;
    bool distorting;
    int disRows;

    private void Start()
    {
        myHologramMat = GetComponent<SpriteRenderer>().material;        
        timeAtLastReset = Time.realtimeSinceStartup;
        increasing = true;
        myAnim.SetAnim("Idle");
        initialized = true;
        yMinNames = new string[maxDistortRows];
        yMaxNames = new string[maxDistortRows];
        for (int i = 0; i < yMinNames.Length; i++)
        {
            yMinNames[i] = "_ShiftYMins" + i;
            yMaxNames[i] = "_ShiftYMaxs" + i;
        }
        yMinDistortValues = new float[maxDistortRows];
        yMaxDistortValues = new float[maxDistortRows];
    }


    // Update is called once per frame
    void Update ()
    {
        if (!initialized) return;

        float t = Time.realtimeSinceStartup;

        // Randomly hold our animation every now and again.
        if (!holding)
        {
            if (UnityEngine.Random.Range(0, 1f) <= chanceOfHoldAnimFrame)
            {
                holding = true;
                myAnim.Pause();
                timeAtHold = t;
            }
        }
        else
        {
            if (t - timeAtHold >= holdTime)
            {
                holding = false;
                myAnim.Unpause();
            }
        }

        bool distortThisFrame = false;

        float pComplete = (t - timeAtLastReset) / blueCycleTime;
        if (pComplete > 1f)
        {
            pComplete = 1f;
            timeAtLastReset = t;
        }

        if (increasing)
        {
            myHologramMat.SetFloat("_CycleTime", pComplete);
        }
        else
        {
            myHologramMat.SetFloat("_CycleTime", 1f - pComplete);
        }

        if (framesHeld == 0)
        {
            // We can roll again for a new image shift.
            if (UnityEngine.Random.Range(0, 1f) <= chanceOfWholeImageShift)
            {
                framesHeld = shiftHoldFrames;
                lastShiftAmount = UnityEngine.Random.Range(-0.15f, 0.15f);
            }
            distorting = false;
            myHologramMat.SetFloat("_ShiftImage", 0);            
        }
        else
        {
            framesHeld--;
            myHologramMat.SetFloat("_ShiftImage", lastShiftAmount);
            distortThisFrame = true;
        }

        if (!distorting)
        {
            if (UnityEngine.Random.Range(0, 1f) <= chanceOfPixelRowShift || distortThisFrame)
            {
                int rowsToDistort = UnityEngine.Random.Range(1, maxDistortRows + 1);
                for (int i = 0; i < rowsToDistort; i++)
                {
                    float yMin = UnityEngine.Random.Range(0.1f, 0.8f);
                    float yMax = yMin + UnityEngine.Random.Range(0.06f, 0.19f);
                    myHologramMat.SetFloat(yMinNames[i], yMin);
                    myHologramMat.SetFloat(yMaxNames[i], yMax);
                    yMinDistortValues[i] = yMin;
                    yMaxDistortValues[i] = yMax;
                    //Debug.Log("Distort " + yMinNames[i] + " to " + yMaxNames[i] + " values " + yMin + " " + yMax);
                }
                myHologramMat.SetFloat("_ShiftAmount", rowShiftAmount);
                myHologramMat.SetInt("_ShiftNumRows", rowsToDistort);
                distorting = true;
                disRows = rowsToDistort;
            }
            else
            {
                myHologramMat.SetInt("_ShiftNumRows", 0);
            }
        }
        else
        {
            for (int i = 0; i < disRows; i++)
            {
                myHologramMat.SetFloat(yMinNames[i], yMinDistortValues[i]);
                myHologramMat.SetFloat(yMaxNames[i], yMaxDistortValues[i]);
                //Debug.Log("Distort " + yMinNames[i] + " to " + yMaxNames[i] + " values " + yMinDistortValues[i] + " " + yMaxDistortValues[i]);
            }
            myHologramMat.SetFloat("_ShiftAmount", rowShiftAmount);
            myHologramMat.SetInt("_ShiftNumRows", disRows);
        }


        if (pComplete >= 1f)
        {
            increasing = !increasing;
        }

	}
}
