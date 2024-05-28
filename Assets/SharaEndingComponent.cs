using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SharaEndingCutsceneStep { FADEIN, HOLD, FADEOUT, COUNT }

[System.Serializable]
public class SharaEndingComponent : MonoBehaviour {

    public Image[] endingImages;
    public float fadeTimeBetweenImages = 0.5f;
    public float holdTimeForImages = 2f;

    public TextMeshProUGUI[] endingText;

    public float timeAtActionStart;

    int indexOfCurrentImage;
    public SharaEndingCutsceneStep currentEndingStep = SharaEndingCutsceneStep.COUNT;

    static SharaEndingComponent singleton;

	// Use this for initialization
	void Start() {

        if (singleton != null && singleton != this) return;

        // Bring all images to 0f, fade them one at a time.
		for (int i = 0; i < endingImages.Length; i++)
        {
            endingImages[i].color = new Color(1f, 1f, 1f, 0f);
        }

        singleton = this;
   }
	
    IEnumerator WaitToDisableSelf(float time)
    {
        yield return new WaitForSeconds(time);        
        gameObject.SetActive(false);
    }

	// Update is called once per frame
	void Update ()
    {
        if (currentEndingStep == SharaEndingCutsceneStep.COUNT) return;

        switch(currentEndingStep)
        {
            case SharaEndingCutsceneStep.FADEIN:
                float percentComplete = (Time.time - timeAtActionStart) / fadeTimeBetweenImages;
                endingImages[indexOfCurrentImage].color = new Color(1f, 1f, 1f, percentComplete);
                endingText[indexOfCurrentImage].color = new Color(1f, 1f, 1f, percentComplete);
                if (percentComplete >= 1f)
                {
                    currentEndingStep = SharaEndingCutsceneStep.HOLD;
                    percentComplete = 1f;
                    timeAtActionStart = Time.time;
                }
                break;
            case SharaEndingCutsceneStep.FADEOUT:
                float localTime = fadeTimeBetweenImages;
                if (indexOfCurrentImage == endingImages.Length -1)
                {
                    localTime *= 1.9f;
                }
                percentComplete = (Time.time - timeAtActionStart) / localTime;
                endingImages[indexOfCurrentImage].color = new Color(1f, 1f, 1f, 1f - percentComplete);
                endingText[indexOfCurrentImage].color = new Color(1f, 1f, 1f, 1f - percentComplete);
                if (percentComplete >= 1f)
                {
                    currentEndingStep = SharaEndingCutsceneStep.FADEIN;
                    percentComplete = 1f;
                    indexOfCurrentImage++;
                    timeAtActionStart = Time.time;
                    if (indexOfCurrentImage >= endingImages.Length)
                    {
                        currentEndingStep = SharaEndingCutsceneStep.COUNT;
                        StartCoroutine(WaitToDisableSelf(1.5f));                        
                        UIManagerScript.singletonUIMS.creditsRoll.gameObject.SetActive(true);
                        UIManagerScript.singletonUIMS.creditsRoll.RollCredits();
                        GameMasterScript.gmsSingleton.creditsActive = true;
                        UIManagerScript.singletonUIMS.endingCutscene.gameObject.SetActive(false);
                    }
                }
                break;
            case SharaEndingCutsceneStep.HOLD:
                percentComplete = (Time.time - timeAtActionStart) / holdTimeForImages;
                if (percentComplete >= 1f)
                {
                    currentEndingStep = SharaEndingCutsceneStep.FADEOUT;
                    percentComplete = 1f;
                    timeAtActionStart = Time.time;
                }
                break;
        }
	}

    public static void DoEnding()
    {        
        GameMasterScript.gmsSingleton.StartCoroutine(singleton._DoEnding());
    }

    IEnumerator _DoEnding()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
		
		MusicManagerScript.RequestPlayMusic("ending_theme", false, false);

        UIManagerScript.FadeOut(2f);
        yield return new WaitForSeconds(2.05f);
        UIManagerScript.endingCanvas.gameObject.SetActive(true);
        timeAtActionStart = Time.time;
        currentEndingStep = SharaEndingCutsceneStep.FADEIN;
        indexOfCurrentImage = 0;
        for (int i = 0; i < endingText.Length; i++)
        {
            endingText[i].text = StringManager.GetString("exp_sharaending_text" + (i+1));
            endingText[i].color = UIManagerScript.transparentColor;
            FontManager.LocalizeMe(endingText[i], TDFonts.WHITE);
        }
        
    }
}
