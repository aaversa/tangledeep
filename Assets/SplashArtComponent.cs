using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SplashArtComponent : MonoBehaviour {

    public Image myImage;
    public Sprite[] possibleSplashArts;
    public Image fillBar;
    public Image logoBGForFill;

    public float fillPercent;

    public CanvasGroup parentCG;

    bool fadingOut;
    float fadeOutTime;
    float timeAtFadeBegin;

    int framesToTurnOff = 3;

    public bool splashArtFadedOut;

	// Use this for initialization
	void Start () {
        myImage.sprite = possibleSplashArts[UnityEngine.Random.Range(0, possibleSplashArts.Length)];
	}		

    public void FadeOut(float time)
    {
        fadeOutTime = time;
        fadingOut = true;
        timeAtFadeBegin = Time.time;
    }

    void Update()
    {
        if (!fadingOut) return;
        float pComplete = (Time.time - timeAtFadeBegin) / fadeOutTime;
        parentCG.alpha = 1f - pComplete;
        if (pComplete >= 1.0f)
        {
            framesToTurnOff--;
            if (framesToTurnOff <= 0)
            {
                fadingOut = false;
                splashArtFadedOut = true;
            }
        }
    }

    public void SetLoadingBar(float percent)
    {
        fillPercent = percent;
        logoBGForFill.fillAmount = fillPercent;
    }

    public void MoveLoadingBar(float percent)
    {
        fillPercent += percent;
        if (fillPercent >= 1.0f)
        {
            fillPercent = 1.0f;
        }
        logoBGForFill.fillAmount = fillPercent;
    }
}
