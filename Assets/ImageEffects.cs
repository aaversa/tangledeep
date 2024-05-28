using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class ImageEffects : MonoBehaviour
{
    // Enabled for one and only one object in the Gameplay scene, "BlackFade"
    // It will report fade status to UIManager to avoid problems with fades being called while other fades are active
    public bool isMainGameUIFade;

    float timeAtFadeStart;
    float timeAtColorChangeStart;
    float colorPercentComplete;
    float fadePercentComplete;
    float timeToFade;
    float timeToColorChange;
    public bool activelyFading;
    bool activelyChangingColor;
    bool blinkingRed;
    bool fadingToRed; // Used for color cycling
    public bool fadingOut;
    public bool fadeInThenOut;
    Color myColor;
    Image myImage;

    public bool fadeInOnEnable;
    public float fadeInOnEnableTime;

    public bool forceStartColor;

    [Header("For Dynamic Canvas")]
    public bool hasSubordinateFade;
    public SubordinateImage subordinateFade; // This will do everything the parent does.    

    void OnEnable()
    {
        StartFadeOnEnable();
    }

    void Awake()
    {
        StartFadeOnEnable();

        if (forceStartColor)
        {
            myColor = Color.white;
        }
    }

    void StartFadeOnEnable()
    {
        if (fadeInOnEnable && !fadingOut)
        {
            myColor = Color.white;
            SetAlpha(0f);
            VerifyColorImageValid();
            myImage.color = myColor;
            FadeOut(fadeInOnEnableTime);
            if (subordinateFade)
            {
                subordinateFade.UpdateFromParent();
            }
        }
    }

    public void SetColorToWhite()
    {
    #if UNITY_EDITOR
        Debug.Log("Set " + gameObject.name + " color to " + myImage.color.a);
    #endif
        myColor = new Color(1f, 1f, 1f, myImage.color.a);
        myImage.color = myColor;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    // Use this for initialization
    void Start()
    {
        if (!fadeInOnEnable)
        {
            activelyFading = false;
        }
        myImage = GetComponent<Image>();
        gameObject.SetActive(true);
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    public void TurnOffAtOnce()
    {
        fadeInThenOut = false;
        activelyFading = false;
        fadingOut = false;
    }

    public void TurnOnAtOnce()
    {        
        UIManagerScript.ToggleBackgroundGradientImage(true);
        UIManagerScript.ToggleBGPlayerBar(false);
        fadeInThenOut = false;
        activelyFading = false;
        fadingOut = false;
        myColor = Color.white;
        myImage.color = myColor;
        //Debug.Log("Turning " + gameObject.name + " on at once. Color is " + myImage.color.a);
    }    

    public void FadeOut(float time)
    {
        #if UNITY_EDITOR
            //Debug.Log("<color=green>" + gameObject.name + " requests fade out over " + time + "</color>");
        #endif
        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
        timeToFade = time;
        timeAtFadeStart = Time.realtimeSinceStartup;
        if (time >= 0.01f)
        {
            activelyFading = true;
            fadingOut = true;
            myColor.a = 0.0f;
            myImage.color = myColor;
            fadeInThenOut = false;
            if (isMainGameUIFade)
            {
                GuideMode.OnFullScreenUIOpened();
                UIManagerScript.SetFadeState(EFadeStates.FADING_OUT);
            }
            if (subordinateFade)
            {
                subordinateFade.gameObject.SetActive(true);
                subordinateFade.UpdateFromParent();
            }
        }
        else
        {
            myImage.color = Color.white;
        }


    }

    public float GetAlpha()
    {
        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
        return myImage.color.a;
    }

    public void SetAlpha(float alph)
    {
    #if UNITY_EDITOR
        //Debug.Log("Set " + gameObject.name + " alpha to " + alph);
    #endif

        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
        myColor.a = alph;
        myImage.color = myColor;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    public void FadeIn(float time)
    {
        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
        timeToFade = time;        
        timeAtFadeStart = Time.realtimeSinceStartup;

#if UNITY_EDITOR        
        //Debug.Log("<color=green>" + gameObject.name + " requests fadein.</color>");
#endif
        
        activelyFading = true;
        fadingOut = false;
        myColor.a = 1.0f;
        if (isMainGameUIFade)
        {
            UIManagerScript.SetFadeState(EFadeStates.FADING_IN);
        }
        if (myImage != null)
        {
            myImage.color = myColor;
        }
        else
        {
            myImage = GetComponent<Image>();
            myImage.color = myColor;
        }
        fadeInThenOut = false;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    void VerifyColorImageValid()
    {
        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
    }

    public void FadeInAndOut(float time)
    {
#if UNITY_EDITOR
    Debug.Log(gameObject.name + " fade in then out");
#endif

        if (myImage == null)
        {
            myImage = GetComponent<Image>();
        }
        timeToFade = time;
        timeAtFadeStart = Time.realtimeSinceStartup;

        activelyFading = true;
        fadingOut = true;
        myColor.a = 0.0f;
        myImage.color = myColor;
        fadeInThenOut = true;
        if (isMainGameUIFade)
        {
            UIManagerScript.SetFadeState(EFadeStates.FADING_OUT);
            GuideMode.OnFullScreenUIOpened();
        }
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    public void ColorToRedThenBackToNormal(float time)
    {
        VerifyColorImageValid();
        if (blinkingRed) return;
        activelyChangingColor = true;
        timeToColorChange = time;
        timeAtColorChangeStart = Time.realtimeSinceStartup;
        myColor = Color.red;
        myImage.color = myColor;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    public void BlinkRed(float cycleTime)
    {
        VerifyColorImageValid();
        blinkingRed = true;
        activelyChangingColor = false;
        timeAtColorChangeStart = Time.realtimeSinceStartup;
        myColor = Color.white;
        myImage.color = myColor;
        timeToColorChange = cycleTime;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }

    public void ResetToNormal()
    {
        VerifyColorImageValid();
        myColor = Color.white;
        myImage.color = myColor;
        blinkingRed = false;
        activelyChangingColor = false;
        activelyFading = false;
        fadingOut = false;
        if (isMainGameUIFade)
        {
            UIManagerScript.SetFadeState(EFadeStates.NOT_FADING);
        }
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();            
        }
    }

    public void ResetToWhite()
    {
        VerifyColorImageValid();
        myColor = Color.white;
        myColor.a = 1.0f;
        myImage.color = myColor;
        blinkingRed = false;
        activelyChangingColor = false;
        activelyFading = false;
        fadingOut = false;
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
            GuideMode.OnFullScreenUIOpened();
        }
    }

    public void ResetToColor(Color newC)
    {
        VerifyColorImageValid();
        myColor = newC;
        myColor.a = 1.0f;
        myImage.color = myColor;
        blinkingRed = false;
        activelyChangingColor = false;
        activelyFading = false;
        fadingOut = false;
        if (isMainGameUIFade) UIManagerScript.SetFadeState(EFadeStates.NOT_FADING);
        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();            
        }
    }

    public bool IsFading()
    {
        return activelyFading;
    }

    // Update is called once per frame
    void Update()
    {

        if (blinkingRed)
        {
            colorPercentComplete = (Time.realtimeSinceStartup - timeAtColorChangeStart) / timeToColorChange;

            if (fadingToRed)
            {
                myColor.g = 1f - colorPercentComplete;
                myColor.b = 1f - colorPercentComplete;
                if (colorPercentComplete >= 1.0f)
                {
                    fadingToRed = false;
                    timeAtColorChangeStart = Time.realtimeSinceStartup;
                }
            }
            else
            {
                myColor.g = colorPercentComplete;
                myColor.b = colorPercentComplete;
                if (colorPercentComplete >= 1.0f)
                {
                    fadingToRed = true;
                    timeAtColorChangeStart = Time.realtimeSinceStartup;
                }
            }

            myImage.color = myColor;
        }

        if (activelyChangingColor)
        {
            colorPercentComplete = (Time.realtimeSinceStartup - timeAtColorChangeStart) / timeToColorChange;
            myColor.g = colorPercentComplete;
            myColor.b = colorPercentComplete;
            if (colorPercentComplete >= 1.0f)
            {
                activelyChangingColor = false;
            }
            myImage.color = myColor;
        }

        if (!activelyFading)
        {
            if (subordinateFade)
            {
                subordinateFade.UpdateFromParent();
            }
            return;
        }

        if (fadeInThenOut)
        {
            fadePercentComplete = (Time.realtimeSinceStartup - timeAtFadeStart) / (timeToFade / 2f);
        }
        else
        {
            fadePercentComplete = (Time.realtimeSinceStartup - timeAtFadeStart) / (timeToFade);
        }

        //Debug.Log(gameObject.name + " " + fadePercentComplete + " " + fadingOut + " " + activelyFading + " FITO: " + fadeInThenOut);

        if (fadingOut)
        {
            if (fadePercentComplete >= 1.0f)
            {
                fadingOut = false;
                timeAtFadeStart = Time.realtimeSinceStartup;
                if (!fadeInThenOut)
                {
                    if (isMainGameUIFade)
                    {
                        UIManagerScript.SetFadeState(EFadeStates.NOT_FADING);
                    }
                    activelyFading = false;
                }
                fadePercentComplete = 1.0f;
            }
            myColor.a = fadePercentComplete;

        }
        else
        {
            if (fadePercentComplete >= 1.0f)
            {
                activelyFading = false;
                if (isMainGameUIFade)
                {
                    UIManagerScript.SetFadeState(EFadeStates.NOT_FADING);
                }
                fadePercentComplete = 1.0f;
            }
            myColor.a = 1f - fadePercentComplete;
            if (!activelyFading)
            {
                gameObject.SetActive(false);
                if (subordinateFade)
                {
                    subordinateFade.gameObject.SetActive(false);
                }
            }

        }
        myImage.color = myColor;

        if (subordinateFade)
        {
            subordinateFade.UpdateFromParent();
        }
    }
}
