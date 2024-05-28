using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Switch_PrettyLoadingArtComponent : MonoBehaviour
{
    [SerializeField]
    private List<Sprite> listPossibleArtBackgrounds;
    
    [SerializeField]
    private Sprite sharaModeLoadingScreen;

    [SerializeField]
    private Sprite lunarNewYearLoadingScreen;    

    [SerializeField]
    private Sprite dragonsLoadingScreen;

    [SerializeField]
    private Image imgBackground;

    [SerializeField]
    private Image blackBackgroundBehindImage;

    [SerializeField]
    private Image imgBWLogo;

    [SerializeField]
    private Image imgColorLogo;

    public TextMeshProUGUI txtHotTip;

    private float fCurrentFillValue;
    private float fFillRatio;
    public void SetFillRatio(float fNewRatio) { fFillRatio = fNewRatio; }
    public void AdjustFillRatio(float fDelta) { fFillRatio += fDelta; }
    public float GetFillRatio() { return fFillRatio; }

    private List<Image> listFadeObjects;

    private bool bActive;
    private int iLastSelectedBackdrop = -1;
    private bool bTextSwapInProgressPleaseDoNotEnterThanksQMZNome;

    //if the bar was just fillalillin', how much of it would fill in one second?
    private float fPercentFillPerSecond = 0.2f;

    public float GetFillValue()
    {
        return fCurrentFillValue;
    }
    void Awake()
    {
        listFadeObjects = new List<Image>();
        listFadeObjects.Add( imgBackground);
        listFadeObjects.Add( imgBWLogo);
        listFadeObjects.Add( imgColorLogo);
        listFadeObjects.Add(blackBackgroundBehindImage);
    }

    private void Start()
    {
        FontManager.LocalizeMe(txtHotTip, TDFonts.WHITE);
    }

    /*
    /// <summary>
    /// Starts a coroutine that moves the bar to a given value.
    /// </summary>
    /// <param name="fDesiredValue">Where you would like the value to end up.</param>
    /// <param name="fSecondsToFillEntireBar">The amount of time it would take to go from 0 to 100% full. NOT the amount of time you would like to take to go from where it currently is to where you want it to be.</param>
    
    public void SetAutoFill(float fDesiredValue, float fSecondsToFillEntireBar )
    {
        //figure out how far we need to travel
        float fTravelTime = Mathf.Abs(fDesiredValue - fFillRatio) * fSecondsToFillEntireBar;

        //go for it
        StartCoroutine(AutoFillCoroutine(fDesiredValue, fTravelTime));
    }

    IEnumerator AutoFillCoroutine(float fEndFill, float fFillTime)
    {
        float fTime = 0f;
        float fStartFill = fFillRatio;
        while (fTime < fFillTime)
        {
            fTime += Time.deltaTime;
            fFillRatio = Mathf.Lerp(fStartFill, fEndFill, fTime / fFillTime);
            yield return null;
        }

    }
    */

    /// <summary>
    /// Activate the loading picture and reset the fill rate.
    /// </summary>
    /// <param name="fOptionalFadeInTime">If non zero, the screen will fade in from black over the amount of time.</param>
    public void TurnOn(float fOptionalFadeInTime = 0f, float fillPerSecondTime = 0.2f)
    {
        if (bActive) return;

        gameObject.SetActive(true);

        fPercentFillPerSecond = fillPerSecondTime;

        TurnOnInternal();

        //ensure we start at 0 progress
        fFillRatio = 0f;
        imgColorLogo.fillAmount = 0f;

        //which pretty?
        int iRando;
        do
        {
            iRando = Random.Range(0, listPossibleArtBackgrounds.Count);
        } while (iRando == iLastSelectedBackdrop && listPossibleArtBackgrounds.Count > 1);

        //so pretty
        imgBackground.sprite = listPossibleArtBackgrounds[iRando];
    
        if (SharaModeStuff.IsSharaModeActive())
        {
            imgBackground.sprite = sharaModeLoadingScreen;
        }

        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.DRAGON_DEFEAT) >= 1 && UnityEngine.Random.Range(0,2) == 0)
        {
            imgBackground.sprite = dragonsLoadingScreen;
        }

        if (GameMasterScript.gmsSingleton.lunarNewYearEnabled)
        {
            imgBackground.sprite = lunarNewYearLoadingScreen;

        }        

        // PC version: This isn't necessary.
        /* var rt = imgBackground.rectTransform;
        Rect r = new Rect(0,0, Screen.width, Screen.height);
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        rt.localScale = Vector3.one; */

        iLastSelectedBackdrop = iRando;

        if (fOptionalFadeInTime > 0f)
        {
            //set all the colors to 0 and fade in.
            foreach (var o in listFadeObjects)
            {
                o.color = Color.black;
                LeanTween.color(o.transform as RectTransform, Color.white, fOptionalFadeInTime);
            }
        }
    }

    void TurnOnInternal()
    {
        blackBackgroundBehindImage.enabled = false;
        imgColorLogo.enabled = true;
        imgBWLogo.enabled = true;
        imgBackground.enabled = true;
        bActive = true;
        GuideMode.OnFullScreenUIOpened(true);
    }


    public void TurnOff(float fOptionalFadeOutTime = 0f, float fButWaitThisLongFirstBecauseWeMust = 0f)
    {
        if (!bActive) return;

        //Prevent the coroutines below from being run twice
        bActive = false;

        //giggty
        GC.Collect();

        //fade text out
        StartCoroutine(TextSwapCoroutine("", fButWaitThisLongFirstBecauseWeMust));

        //if there is a delay, don't turn things off until time is up.
        if (fOptionalFadeOutTime > 0f)
        {
            StartCoroutine(TurnOffCoroutine(fOptionalFadeOutTime, fButWaitThisLongFirstBecauseWeMust));
            return;
        }

        //otherwise
        TurnOffInternal();
    }

    IEnumerator TurnOffCoroutine(float fFadeTime, float fInitialDelay)
    {
        imgColorLogo.fillAmount = 1.0f;
        if (fInitialDelay > 0f)
        {
            yield return new WaitForSeconds(fInitialDelay);
        }

        float fTime = 0;
        while (fTime < fFadeTime)
        {
            fTime += Time.deltaTime;
            imgBackground.color = Color.Lerp(Color.white, Color.clear, fTime / fFadeTime);
            blackBackgroundBehindImage.color = Color.Lerp(Color.white, Color.clear, fTime / fFadeTime);
            imgBWLogo.color = Color.Lerp(Color.white, Color.clear, fTime / fFadeTime);
            imgColorLogo.color = Color.Lerp(Color.white, Color.clear, fTime / fFadeTime);
            yield return null;
        }

        //fade out
        /*
        foreach (var o in listFadeObjects)
        {
            LeanTween.color(o.transform as RectTransform, Color.black, fTime);
        }
        */

        yield return new WaitForSeconds(fTime);
        TurnOffInternal();
        
    }

    public bool IsActive()
    {
        return bActive;
    }

    void TurnOffInternal()
    {
        //UIManagerScript.FadeIn(0.0f); Not necessary.

        UIManagerScript.singletonUIMS.blackFade.SetActive(false);

        blackBackgroundBehindImage.enabled = false;
        imgColorLogo.enabled = false;
        imgBWLogo.enabled = false;
        imgBackground.enabled = false;
        bActive = false;
    }

    /// <summary>
    /// Changes the text string where we display loading tips
    /// </summary>
    /// <param name="strHotTip">The hot tip</param>
    /// <param name="bDoFadeStuff"></param>
    public void SetTextString(string strHotTip, bool bDoFadeStuff = false)
    {
        if (!bDoFadeStuff)
        {
            txtHotTip.text = strHotTip;
            return;
        }

        StartCoroutine(TextSwapCoroutine(strHotTip));
    }

    //hide the current text, show the new text
    IEnumerator TextSwapCoroutine(string strHotTip, float fInitialDelay = 0f)
    {
        yield return new WaitWhile( () => bTextSwapInProgressPleaseDoNotEnterThanksQMZNome);

        bTextSwapInProgressPleaseDoNotEnterThanksQMZNome = true;

        if (fInitialDelay > 0f)
        {
            yield return new WaitForSeconds(fInitialDelay);
        }

        Color startColor = txtHotTip.color;

        //If we are setting to nothing, fade out slower. If we're swapping to a different tip,
        //fade out on the quick.
        float fFadeOutDuration = string.IsNullOrEmpty(strHotTip) ? 0.5f : 0.2f;
        float fTime = fFadeOutDuration;

        while (fTime > 0.0f)
        {
            fTime -= Time.deltaTime;
            txtHotTip.color = Color.Lerp(startColor, Color.clear, 1.0f - fTime / fFadeOutDuration);
            yield return null;
        }

        //fade back up!
        txtHotTip.text = strHotTip;
        float fFadeUpDuration = 0.2f;
        fTime = fFadeUpDuration;

        while (fTime > 0.0f)
        {
            fTime -= Time.deltaTime;
            txtHotTip.color = Color.Lerp(Color.clear, startColor, 1.0f - fTime / fFadeUpDuration);
            yield return null;
        }

        txtHotTip.color = startColor;

        bTextSwapInProgressPleaseDoNotEnterThanksQMZNome = false;
    }

    // Update is called once per frame
    void Update ()
    {
        if (!bActive)
        {
            return;
        }

	    //keep the logo where it needs to be
        if (!CustomAlgorithms.CompareFloats(fCurrentFillValue,fFillRatio))
        {
            //move to the requested fillRatio;
            fCurrentFillValue += fPercentFillPerSecond * Time.deltaTime;
            if (fCurrentFillValue > fFillRatio)
            {
                fCurrentFillValue = fFillRatio;
            }
        }
            

        //this keeps us moving toward the goal smoothly.
        imgColorLogo.fillAmount = fCurrentFillValue;

        //Debug.Log("FILL VALUE: " + fCurrentFillValue);
    }


}
