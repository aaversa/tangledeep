using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class EndingCutsceneManager : MonoBehaviour {

    public TextMeshProUGUI[] endingText;
    public float timeToFirstTextFadeIn;
    public float textFadeTime;
    public float waitBetweenTexts;

    float timeAtActionStart;

    bool readyToFadeText;
    bool waitingForNextText;
    int indexOfTextToFade;
    bool prepForFinalTextFade;
    bool finishedText;
    bool finishedAll;

    bool fadingInEntireScene = false;
    public const float TIME_FADE_SCENE = 5.5f;
    public const float COUNTDOWN_TO_CREDITS = 4.1f; // was 4.34
    bool initialized = false;

    public Image blackFadeImage;
    public Image whiteFadeImage;

    public void Initialize()
    {
        for (int i = 0; i < endingText.Length; i++)
        {
            endingText[i].color = UIManagerScript.transparentColor;
            endingText[i].text = StringManager.GetString("ending_cutscene_text" + (i + 1));
            FontManager.LocalizeMe(endingText[i], TDFonts.WHITE);
        }
        timeAtActionStart = Time.time;
        initialized = true;
        fadingInEntireScene = true;
        //Debug.Log("Initialized ending cutscene!");

		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("ending_theme");


        CanvasParallaxScript[] cps = GetComponentsInChildren<CanvasParallaxScript>();
        for (int i = 0; i < cps.Length; i++)
        {
            cps[i].Initialize();
        }

        if (UIManagerScript.GetFadeState() != EFadeStates.FADING_OUT && UIManagerScript.singletonUIMS.blackFade.GetComponent<Image>().color.a != 1f)
        {
            // instant fade out
            UIManagerScript.FadeOut(0.1f);
            Debug.Log("Do instant fade.");
        }
    }

    public void BeginEndSequence(float waitTime)
    {
        GuideMode.OnFullScreenUIClosed();
        if (!SharaModeStuff.IsSharaModeActive())
        {
            StartCoroutine(WaitThenInitialize(waitTime));
        }        
        SharaEndingComponent sharaEndingCutscene = UIManagerScript.endingCanvas.gameObject.GetComponentInChildren<SharaEndingComponent>();
        if (sharaEndingCutscene != null)
        {
            if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) || !SharaModeStuff.IsSharaModeActive())
            {
                sharaEndingCutscene.gameObject.SetActive(false);
            }
            else
            {
                sharaEndingCutscene.gameObject.SetActive(true);
                SharaEndingComponent.DoEnding();
            }
        }
    }

    IEnumerator WaitThenInitialize(float time)
    {
        yield return new WaitForSeconds(time);
        Initialize();
    }

	
	// Update is called once per frame
	void Update () {
        if (!initialized) return;
        if (finishedAll)
        {
            gameObject.SetActive(false);
            return;
        }
        if (finishedText)
        {
            float pComplete = (Time.time - timeAtActionStart) / COUNTDOWN_TO_CREDITS;
            if (pComplete >= 1.0f)
            {
                //Debug.Log("Finished counting down to credits!");
                pComplete = 1.0f;
                finishedAll = true;
                //blackFadeImage.color = Color.black;
                UIManagerScript.singletonUIMS.creditsRoll.gameObject.SetActive(true);
                UIManagerScript.singletonUIMS.creditsRoll.RollCredits();
                GameMasterScript.gmsSingleton.creditsActive = true;
            }
            if (pComplete >= 0.5f)
            {
                float adjusted = (pComplete - 0.5f) * 2f;
                whiteFadeImage.color = new Color(1f, 1f, 1f, adjusted);
            }
            
            return;
        }
        if (fadingInEntireScene)
        {
            float pComplete = (Time.time - timeAtActionStart) / TIME_FADE_SCENE;
            if (pComplete >= 1.0f)
            {
                pComplete = 1.0f;
                fadingInEntireScene = false;
                timeAtActionStart = Time.time;
            }
            blackFadeImage.color = new Color(0f, 0f, 0f, 1f - pComplete);
            return;            
        }
		if (!readyToFadeText) // First WAIT before doing ANY text display at all.
        {
            float pComplete = (Time.time - timeAtActionStart) / timeToFirstTextFadeIn;
            if (pComplete >= 1.0f)
            {
                pComplete = 1.0f;
                readyToFadeText = true;
                timeAtActionStart = Time.time;
            }
        }
        else
        {
            float pComplete = 0f;

            if (prepForFinalTextFade)
            {
                // fade out everything but the last element.
                pComplete = (Time.time - timeAtActionStart) / textFadeTime;
                if (pComplete >= 1.0f)
                {
                    waitingForNextText = false;
                    prepForFinalTextFade = false;
                    timeAtActionStart = Time.time;
                }
                for (int i = 0; i < endingText.Length-1; i++)
                {
                    endingText[i].color = new Color(1f, 1f, 1f, 1f - pComplete);
                }
                return;
            }

            if (waitingForNextText) // Delay between previous action and the next text fade.
            {
                pComplete = (Time.time - timeAtActionStart) / waitBetweenTexts;
                if (pComplete >= 1.0f)
                {
                    pComplete = 1.0f;
                    timeAtActionStart = Time.time;
                    waitingForNextText = false;

                    if (indexOfTextToFade >= endingText.Length-1) // We've waited for the FINAL text element, which will stand alone.
                    {
                        prepForFinalTextFade = true;
                    }
                }
                return;
            }
            // We are fading some text.
            pComplete = (Time.time - timeAtActionStart) / textFadeTime;
            if (pComplete >= 1.0f)
            {
                pComplete = 1.0f;                
                timeAtActionStart = Time.time;
                if (indexOfTextToFade >= endingText.Length)
                {
                    finishedText = true;
                    timeAtActionStart = Time.time;
                    return;
                }
            }

            endingText[indexOfTextToFade].color = new Color(1f, 1f, 1f, pComplete);

            if (pComplete >= 1.0f)
            {
                indexOfTextToFade++;
                waitingForNextText = true;
                
                if (indexOfTextToFade >= endingText.Length)
                {
                    // That was the last text to fade. We're done.
                    finishedText = true;
                    timeAtActionStart = Time.time;
                }
            }
        }
	}
}
