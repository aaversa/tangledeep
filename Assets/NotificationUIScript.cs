using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

enum NotificationFadeSteps { FILL, TEXTFADE, HOLD, FADEOUT, COUNT }

public class NotificationUIScript : MonoBehaviour {

    public CanvasGroup cgFillElement;
    public Image fillImage;
    public CanvasGroup cgTextElement;
    public TextMeshProUGUI textLabel;

    public float bgFillTime;
    public float textFadeInTime;
    public float holdTime;
    public float fadeOutTime;

    static float timeAtActionStart;
        
    static NotificationUIScript singleton;

    static NotificationFadeSteps currentStep;

	// Use this for initialization
	void Start () {
        singleton = this;

        FontManager.LocalizeMe(textLabel, TDFonts.WHITE);
        Clear();

    }

    private void Update()
    {
        float pComplete = 1f;
        float timeToUse = 0f;

        switch(currentStep)
        {
            case NotificationFadeSteps.COUNT:
                return;
            case NotificationFadeSteps.FILL:
                timeToUse = bgFillTime;
                break;
            case NotificationFadeSteps.TEXTFADE:
                timeToUse = textFadeInTime;
                break;
            case NotificationFadeSteps.HOLD:
                timeToUse = holdTime;
                break;
            case NotificationFadeSteps.FADEOUT:
                timeToUse = fadeOutTime;
                break;
        }

        pComplete = (Time.time - timeAtActionStart) / timeToUse;

        if (pComplete >= 1f)
        {
            pComplete = 1f;
        }

        switch(currentStep)
        {
            case NotificationFadeSteps.FILL:
                fillImage.fillAmount = pComplete;
                break;
            case NotificationFadeSteps.TEXTFADE:
                cgTextElement.alpha = pComplete;
                break;
            case NotificationFadeSteps.HOLD:
                break;
            case NotificationFadeSteps.FADEOUT:
                cgTextElement.alpha = 1f - pComplete;
                cgFillElement.alpha = 1f - pComplete;
                break;
        }

        if (pComplete >= 1f)
        {
            int iStep = (int)currentStep;
            iStep++;
            currentStep = (NotificationFadeSteps)iStep;
            timeAtActionStart = Time.time;
        }
    }

    public static void Clear()
    {
        singleton.cgFillElement.alpha = 0f;
        singleton.cgTextElement.alpha = 0f;
    }

    public static void WriteGenericText(string theText)
    {
        singleton.textLabel.text = theText;
        timeAtActionStart = Time.time;
        currentStep = NotificationFadeSteps.FILL;
        singleton.cgFillElement.alpha = 1f;
        singleton.fillImage.fillAmount = 0f;
        singleton.cgTextElement.alpha = 0f;
    }

    public static void NewDay(int day)
    {
        if (MapMasterScript.activeMap == null)
        {
            return;
        }
        if (!MapMasterScript.activeMap.IsBossFloor()) UIManagerScript.PlayCursorSound("TimePassing"); 
        StringManager.SetTag(0, day.ToString());
        WriteGenericText(StringManager.GetString("misc_daymarker"));
    }

}
