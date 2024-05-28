using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGroupFader : MonoBehaviour {

    CanvasGroup myCG;
    bool fading;
    bool fadingOut; // if false, then fadingIn
    float timeFadeStart;
    float totalFadeTime;

	// Use this for initialization
	void Awake () {
        if (myCG == null)
        {
            myCG = GetComponent<CanvasGroup>();
        }        
        if (myCG == null)
        {
            Debug.Log(gameObject.name + " couldn't find own CanvasGroup?");
        }
	}
	
    public void FadeIn(float time)
    {
        myCG.alpha = 0f;
        totalFadeTime = time;
        timeFadeStart = Time.time;
        fading = true;
        fadingOut = false;
    }

    public void SetAlpha(float value)
    {
        myCG.alpha = value;
        fading = false;
        fadingOut = false;
    }

    public void FadeOut(float time)
    {
        myCG.alpha = 1.0f;
        totalFadeTime = time;
        timeFadeStart = Time.time;
        fading = true;
        fadingOut = true;
    }

    // Update is called once per frame
    void Update () {
        if (!fading) return;

        float percentComplete = (Time.time - timeFadeStart) / totalFadeTime;
        if (percentComplete > 1.0f)
        {
            percentComplete = 1.0f;
        }

        if (fadingOut)
        {
            myCG.alpha = 1.0f - percentComplete;
        }
        else
        {
            myCG.alpha = percentComplete;
        }

        if (percentComplete == 1.0f)
        {
            fading = false;
        }
	}
}
