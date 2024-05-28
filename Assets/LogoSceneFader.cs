using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogoSceneFader : MonoBehaviour
{
    public Image myImage;

    EFadeStates fadeState = EFadeStates.COUNT;

    float timeAtFade;

    float currentFadeTime;

    public void FadeOut(float time)
    {
        fadeState = EFadeStates.FADING_OUT;
        timeAtFade = Time.time;
        currentFadeTime = time;
    }

    public void FadeIn(float time)
    {
        fadeState = EFadeStates.FADING_IN;
        timeAtFade = Time.time;
        currentFadeTime = time;
    }

    private void Update()
    {
        if (fadeState != EFadeStates.FADING_IN && fadeState != EFadeStates.FADING_OUT)
        {
            return;
        }

        float pComplete = (Time.time - timeAtFade) / currentFadeTime;

        bool done = false;

        if (pComplete >= 1f)
        {
            done = true;
            pComplete = 1f;
        }

        float alphaValue = 0f;

        if (fadeState == EFadeStates.FADING_IN)
        {
            alphaValue = EasingFunction.Linear(0f, 1f, pComplete);
        }
        else
        {
            alphaValue = EasingFunction.Linear(1f, 0f, pComplete);
        }

        myImage.color = new Color(0f, 0f, 0f, alphaValue);

        if (done)
        {
            fadeState = EFadeStates.NOT_FADING;
        }
    }
    
    public void SetToBlack()
    {
        myImage.color = new Color(0f, 0f, 0f, 1f);
    }

}
