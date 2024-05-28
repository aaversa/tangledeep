using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TooltipScript : MonoBehaviour {

    public const float FADE_IN_TIME = 0.18f;
    public const float DELAY_BEFORE_FADE = 0.32f;

    public float overrideFadeTime;
    public float overrideDelayTime;

    float fadeTime;
    float delayTime;

    public float timeAtEnable;
    public bool waitingToFade;

    public TextMeshProUGUI tooltipText;

    CanvasGroupFader cgf;
    
	// Use this for initialization
	void Start () {
        return; // 7/9/21 - Do we care about this at all?

        cgf = GetComponent<CanvasGroupFader>();
        if (tooltipText != null)
        {
            FontManager.LocalizeMe(tooltipText, TDFonts.WHITE);
        }        
        fadeTime = FADE_IN_TIME;
        if (overrideFadeTime != 0)
        {
            fadeTime = overrideFadeTime;
        }
        delayTime = DELAY_BEFORE_FADE;
        if (overrideDelayTime != 0)
        {
            delayTime = overrideDelayTime;
        }
    }

    public void FadeInImmediately()
    {
        if (cgf == null) return;
        waitingToFade = false;
        cgf.SetAlpha(1f);
    }
	
    void OnEnable ()
    {
        return; // 7/9/21 - Do we care about this at all?

        if (cgf == null) return;

#if UNITY_SWITCH
        cgf.SetAlpha(1f);
        return;
#else
        if (GameMasterScript.pretendSwitchEnabled)
        {
            cgf.SetAlpha(1f);
            return;
        }
#endif
        cgf.SetAlpha(0f);
        timeAtEnable = Time.time;
        waitingToFade = true;
    }

    void Update ()
    {
        return; // 7/9/21 - Do we care about this at all?

        if (waitingToFade)
        {
            float pComplete = (Time.time - timeAtEnable) / delayTime;
            if (pComplete >= 1.0f)
            {
                waitingToFade = false;
                cgf.FadeIn(fadeTime);
            }
        }
    }
}
