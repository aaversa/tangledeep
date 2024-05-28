using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AuraAnimateScript : MonoBehaviour {

    SpriteRenderer sr;
    public float cycleTime;
    public float minOpacity;
    public float maxOpacity;
    float timeAtCycleStart;
    float valueRange;
    Color myColor;
    bool opacityUp;

    SpriteRenderer parentSR;
    private bool foundParentSR;
    private int attemptsToFindParentSR = 0;
        
	// Use this for initialization
	void Start () {
        sr = GetComponent<SpriteRenderer>();
        timeAtCycleStart = Time.time;
        myColor = sr.color;
        myColor.a = minOpacity;
        opacityUp = true;
        valueRange = maxOpacity - minOpacity;
	}

	private void OnEnable()
	{
		parentSR = null;
		foundParentSR = false;
		attemptsToFindParentSR = 0;
		if (transform.parent != null)
		{
			parentSR = transform.parent.GetComponent<SpriteRenderer>();
			if (parentSR != null)
			{
				foundParentSR = true;
			}
		}
	}

	// Update is called once per frame
	void Update () 
	{
		// We're trying to connect to our parent SR a few times, because maybe there was a delay between initial OnEnable() and connecting this aura to a parent object
		if (!foundParentSR && attemptsToFindParentSR < 3) 
		{
			OnEnable();
			attemptsToFindParentSR++;
		}

		if (foundParentSR)
		{
			// We want the aura to be directly underneath us at all times, offset by -1 so we draw on top.
			sr.sortingOrder = parentSR.sortingOrder - 1;
		}
		
		
        float percentComplete = (Time.time - timeAtCycleStart) / cycleTime;
        bool finished = false;
        if (percentComplete >= 1.0f)
        {
            percentComplete = 1.0f;
            finished = true;
        }

        float opacityValue = Mathfx.Sinerp(0, valueRange, percentComplete);

        if (opacityUp)
        {
            myColor.a = minOpacity + opacityValue;
        }
        else
        {
            myColor.a = maxOpacity - opacityValue;
        }

        sr.color = myColor;

        if (finished)
        {
            timeAtCycleStart = Time.time;
            opacityUp = !opacityUp;
        }

	}
}
