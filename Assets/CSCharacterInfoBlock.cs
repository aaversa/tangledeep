using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DynamicHeroStatUIElement : MonoBehaviour
{
    protected HeroPC myHero;
    protected bool bHeroHasBeenSet;

    protected bool bIsActive;
    protected float fLastTurnOnTime;

    protected virtual void Start()
    {
    }

    public virtual void AssignHero(HeroPC setHero)
    {
        myHero = setHero;
        bHeroHasBeenSet = true;
    }

    protected virtual void UpdateInfo()
    {

    }

    public virtual void TurnOn()
    {
        gameObject.SetActive(true);
        bIsActive = true;
        fLastTurnOnTime = Time.realtimeSinceStartup;
    }
    public virtual void TurnOff()
    {


    }
    
}

public class CSCharacterInfoBlock : DynamicHeroStatUIElement
{
    public TextMeshProUGUI  textName;
    public TextMeshProUGUI  textClass;
    public TextMeshProUGUI  textXP;
    public TextMeshProUGUI  textLevel;
    public TextMeshProUGUI  textJP;
    public TextMeshProUGUI  textGP;
    public Image            imagePortrait;
    public Image            jpIcon;
    public Image            xpIcon;
    public Image            levelIcon;
    public Image            goldIcon;

    //Cached information about hero progress
    private CharacterJobs   lastAssignedJob = CharacterJobs.COUNT;
    private int             iCachedHeroLevel = 0;    
    private string          strCachedJobName = "Poet";
    private string          strCachedHeroName = "";
    private int             iCachedXP;
    private int             iCachedJP;
    private int             iCachedGP;

    //Make the JP label pretty *-*
    private Coroutine       glowingJPLabelCoroutine;

    [Header("Tweakable Sliding Object Values")]
    public float BaseDelayBeforeSlideStart = 0.05f; // was 0.2
    public float SlideDuration = 0.15f; // was 0.4
    public float DelayBetweenImageAndOtherElements = 0.15f; // was 0.5
    public float PerElementExtraDelay = 0.03f;
    public Vector2 SlideStartAsOffsetFromEndPosition = new Vector2(-64f, 0f);

    protected Dictionary<RectTransform, Vector2> dictRecordedBasePositions;
    protected List<Coroutine> runningSlideCoroutines;

    // Use this for initialization
    protected override void Start ()
    {
        base.Start();

        dictRecordedBasePositions = new Dictionary<RectTransform, Vector2>();

        StoreOrigin(textName.rectTransform);
        StoreOrigin(textClass.rectTransform);
        StoreOrigin(textLevel.rectTransform);
        StoreOrigin(textXP.rectTransform);
        StoreOrigin(textJP.rectTransform);
        StoreOrigin(textGP.rectTransform);
        StoreOrigin(imagePortrait.rectTransform);

        StoreOrigin(xpIcon.rectTransform);
        StoreOrigin(jpIcon.rectTransform);
        StoreOrigin(levelIcon.rectTransform);
        StoreOrigin(goldIcon.rectTransform);

        runningSlideCoroutines = new List<Coroutine>();

        FontManager.LocalizeMe(textName, TDFonts.WHITE);
        FontManager.LocalizeMe(textClass, TDFonts.WHITE);

    }

    protected void StoreOrigin(RectTransform rt)
    {
        if (dictRecordedBasePositions == null)
        {
            dictRecordedBasePositions = new Dictionary<RectTransform, Vector2>();
        }
        dictRecordedBasePositions.Add(rt, rt.anchoredPosition);
    }

    /// <summary>
    /// We need to move the JP/GP icons up to where the XP icons were when Shara is our mode. 
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="newYPos"></param>
    protected void AdjustElementForSharaMode(RectTransform rt, float newYPos)
    {
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newYPos);        
        StoreOrigin(rt);
    }

    public override void AssignHero(HeroPC setHero)
    {
        base.AssignHero(setHero);
        iCachedHeroLevel = -1;
        iCachedJP = -1;
        iCachedGP = -1;
        iCachedXP = -1;
        strCachedHeroName = "";
        strCachedJobName = "";

        //change Shara to not display XP, and move JP/Gold up
        if (setHero.myJob.jobEnum == CharacterJobs.SHARA)
        {
            var xpTextLoc_y = textXP.rectTransform.anchoredPosition.y;
            var xpIconLoc_y = xpIcon.rectTransform.anchoredPosition.y;

            AdjustElementForSharaMode(textGP.rectTransform, xpTextLoc_y);
            AdjustElementForSharaMode(textJP.rectTransform, xpTextLoc_y);
            AdjustElementForSharaMode(jpIcon.rectTransform, xpIconLoc_y);
            AdjustElementForSharaMode(goldIcon.rectTransform, xpIconLoc_y);
        }
    }

    protected override void UpdateInfo()
    {
        //if we have no hero, well that's too bad
        if (!bHeroHasBeenSet)
        {
            return;
        }

        bool bClassAndLevelUpdate = false;
        bool bXPUpdate = false;
        bool bJPUpdate = false;
        bool bGPUpdate = false;
        
        //Check on the number stats we have
        if (iCachedHeroLevel != myHero.myStats.GetLevel())
        {

            bClassAndLevelUpdate = true;
            bXPUpdate = true;

            iCachedHeroLevel = myHero.myStats.GetLevel();
        }
        
        //update the portrait if our job has changed or name has changed
        if (lastAssignedJob != myHero.myJob.jobEnum ||
            strCachedHeroName != myHero.displayName)
        {
            lastAssignedJob = myHero.myJob.jobEnum;
            imagePortrait.sprite = myHero.GetPortrait();
            strCachedJobName = myHero.myJob.DisplayName + " " + UIManagerScript.cyanHexColor + iCachedHeroLevel + "</color>";
            strCachedHeroName = myHero.displayName;

            bClassAndLevelUpdate = true;
        }



        if (iCachedJP != myHero.GetCurJP())
        {
            iCachedJP = (int)myHero.GetCurJP();
            bJPUpdate = true;
        }

        if (iCachedGP != myHero.GetMoney())
        {
            iCachedGP = myHero.GetMoney();
            bGPUpdate = true;
        }

        if (iCachedXP != myHero.myStats.GetXP())
        {
            bXPUpdate = true;
            iCachedXP = myHero.myStats.GetXP();
        }

        //Update the gameobjects if necessary
        if (bClassAndLevelUpdate)
        {
            textName.text = strCachedHeroName;
            strCachedJobName = myHero.myJob.DisplayName + " " + UIManagerScript.cyanHexColor + iCachedHeroLevel + "</color>";
            textClass.text = strCachedJobName;
            imagePortrait.sprite = myHero.GetPortrait();
        }

        if (bXPUpdate)
        {
            textXP.text = iCachedXP + " / " + myHero.myStats.GetXPToNextLevel();
        }

        if (bJPUpdate)
        {
            //StringManager.SetTag(0, iCachedJP.ToString());
            //textJP.text = StringManager.GetString("ui_dreamcaster_player_jp");
            textJP.text = iCachedJP.ToString();

            //Don't start glowing if we're mid slide
            bool bShouldGlow = myHero.HasEnoughJPForSkillAndCanPurchase();

            if (bShouldGlow && glowingJPLabelCoroutine == null )
            {
                glowingJPLabelCoroutine = StartCoroutine(CycleUIObjectColor(textJP, new Color(212/255f,175/255f,55/255f,1f), Color.yellow, 1.0f));
            }
            else if (!bShouldGlow && glowingJPLabelCoroutine != null )
            {
                StopCoroutine(glowingJPLabelCoroutine);
                glowingJPLabelCoroutine = null;
                textJP.color = Color.white;
            }
        }

        if (bGPUpdate)
        {
            //StringManager.SetTag(0, iCachedGP.ToString());
            //textGP.text = StringManager.GetString("ui_dreamcaster_player_gold");
            textGP.text = iCachedGP.ToString(); // + StringManager.GetString("misc_moneysymbol");
        }
    }

    IEnumerator CycleUIObjectColor( Graphic uiObject, Color startColor, Color endColor, float fBounceSeconds = 2.0f )
    {
        //wait until we're done sliding in
        while (Time.realtimeSinceStartup - fLastTurnOnTime < BaseDelayBeforeSlideStart + SlideDuration + 0.5f)
        {
            uiObject.color = Color.clear;
            yield return null;
        }

        //fBounceSeconds is a full bounce from start to end back to start
        float fTime = fBounceSeconds / 2.0f;
        while (true)
        {
            uiObject.color = Color.Lerp( startColor, endColor, Mathf.PingPong(Time.realtimeSinceStartup, fTime) / fTime);
            yield return null;
        }
    }

    public override void TurnOn()
    {
        if (bIsActive && gameObject.activeSelf)
        {
            //Debug.Log("<color=red>NOT turning on cs block because it's already active.</color>");
            return;
        }

        //Debug.Log("<color=green>TURNING ON CS Char Info Block</color>");

        base.TurnOn();

        if (myHero.myJob.jobEnum == CharacterJobs.SHARA)
        {
            textXP.enabled = false;
            xpIcon.enabled = false;
        }
        

        //Make sure we don't glow early
        textJP.color = Color.clear;

        StopCoroutine("SlideObjectOutAfterDelay");

//        Debug.Log("Request CS char info block");

        float fElementDelay = BaseDelayBeforeSlideStart + DelayBetweenImageAndOtherElements;

        StartCoroutine(SlideObjectInAfterDelay(BaseDelayBeforeSlideStart, SlideDuration, imagePortrait.rectTransform, (c) => imagePortrait.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay, SlideDuration, textName.rectTransform, (c) => textName.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 2, SlideDuration, textClass.rectTransform, (c) => textClass.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 3, SlideDuration, textLevel.rectTransform, (c) => textLevel.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 3, SlideDuration, levelIcon.rectTransform, (c) => levelIcon.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 3.5f, SlideDuration, textXP.rectTransform, (c) => textXP.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 3.5f, SlideDuration, xpIcon.rectTransform, (c) => xpIcon.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 4, SlideDuration, textJP.rectTransform, (c) => textJP.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 4, SlideDuration, jpIcon.rectTransform, (c) => jpIcon.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 5, SlideDuration, textGP.rectTransform, (c) => textGP.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectInAfterDelay(fElementDelay + PerElementExtraDelay * 5, SlideDuration, goldIcon.rectTransform, (c) => goldIcon.color = c, Color.clear, Color.white, SlideStartAsOffsetFromEndPosition));
    }

    public override void TurnOff()
    {
        if (!bIsActive)
        {
            //Debug.Log("<color=red>NOT turning off CS block, because it's inactive.</color>");
            return;
        }        

        // #todo bIsActive is not always accurate? 12/25/2017 
        // Coroutines below are not running (but throwing errors) once in awhile, because game object is inactive

        if (!gameObject.activeSelf) return;
            //Debug.Log("<color=red>NOT turning off CS block, because it's inactive self.</color>");

        //Debug.Log("<color=green>TURNING OFF CS Char Info Block</color>");

        if (glowingJPLabelCoroutine != null)
        {
            StopCoroutine(glowingJPLabelCoroutine);
            glowingJPLabelCoroutine = null;
        }
        StopCoroutine("SlideObjectInAfterDelay");

        //when sliding *out*, do not use a base delay
        //float fElementDelay = BaseDelayBeforeSlideStart + DelayBetweenImageAndOtherElements;
        float fElementDelay = 0f + 0f;

        StartCoroutine(SlideObjectOutAfterDelay(0f, SlideDuration, imagePortrait.rectTransform, (c) => imagePortrait.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition ));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay, SlideDuration, textName.rectTransform, (c) => textName.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition ));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 2, SlideDuration, textClass.rectTransform, (c) => textClass.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition ));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 3, SlideDuration, textLevel.rectTransform, (c) => textLevel.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition ));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 3, SlideDuration, levelIcon.rectTransform, (c) => levelIcon.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 3.5f, SlideDuration, textXP.rectTransform, (c) => textXP.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 3.5f, SlideDuration, xpIcon.rectTransform, (c) => xpIcon.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 4, SlideDuration, textJP.rectTransform, (c) => textJP.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 4, SlideDuration, jpIcon.rectTransform, (c) => jpIcon.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 5, SlideDuration, textGP.rectTransform, (c) => textGP.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition ));
        StartCoroutine(SlideObjectOutAfterDelay(fElementDelay + PerElementExtraDelay * 5, SlideDuration, goldIcon.rectTransform, (c) => goldIcon.color = c, Color.white, Color.clear, SlideStartAsOffsetFromEndPosition));

        //turn off the gameobject after the sliding is done.
        StartCoroutine(TurnOffAfter(fElementDelay + PerElementExtraDelay * 6, gameObject, 0));

        bIsActive = false;
    }

    IEnumerator TurnOffAfter(float fSeconds, GameObject go, int safetyCheck)
    {
        yield return new WaitForSeconds(fSeconds);
        if (!UIManagerScript.singletonUIMS.AnyMenuUIOpen())
        {
            go.SetActive(false);
            //Debug.Log("<color=green>TURNING OFF CS Char Info Block after delay.</color>");
        }        
        else
        {
            // But maybe there is some weird stuff happening and we're spamming the button.
            // Even though we should probably stay ON, let's just check a couple more times.
            safetyCheck++;
            if (safetyCheck < 4)
            {
                StartCoroutine(TurnOffAfter(0.1f, go, safetyCheck));
            }            
        }
    }

    public IEnumerator SlideObjectOutAfterDelay(float fDelay, float fSlideDuration, RectTransform rt,
        Action<Color> actionColorSet, Color startColor, Color endColor, Vector2 vGoalAsOffsetFromStart)
    {
        //hang on to start position
        Vector2 vStartPosition = dictRecordedBasePositions[rt]; //rt.anchoredPosition;
        Vector2 vEndPos = vStartPosition + vGoalAsOffsetFromStart;
        //Chill
        yield return new WaitForSeconds(fDelay);

        //Tween it over
        LeanTween.moveLocal(rt.gameObject, vEndPos, fSlideDuration).setEaseOutQuint();
        
        //fade it out at the same time.
        float fTime = 0f;
        while (fTime < fSlideDuration)
        {
            Color fadeColor = Color.Lerp(startColor, endColor, fTime / fSlideDuration);
            actionColorSet(fadeColor);
            fTime += Time.deltaTime;
            yield return null;
        }

        actionColorSet(endColor);

        //and then return it to the start position to be re-slid in later
        rt.anchoredPosition = vStartPosition;

    }

    public IEnumerator SlideObjectInAfterDelay(float fDelay, float fSlideDuration, RectTransform rt, Action<Color> actionColorSet, Color startColor, Color endColor, Vector2 vStartAsOffsetFromEndPos)
    {
        //Make the object clear right away
        actionColorSet(startColor);

        //Chill
        yield return new WaitForSeconds(fDelay);

        //Move the object to the start position
        Vector2 vEndPos = dictRecordedBasePositions[rt]; // rt.anchoredPosition;
        Vector2 vStartPos = vEndPos + vStartAsOffsetFromEndPos;
        rt.anchoredPosition = vStartPos;

        //Tween it over
        LeanTween.moveLocal(rt.gameObject, vEndPos, fSlideDuration).setEaseOutQuint();

        //fade it in at the same time.
        float fTime = 0f;
        while (fTime < fSlideDuration)
        {
            Color fadeColor = Color.Lerp(startColor, endColor, fTime / fSlideDuration);
            actionColorSet(fadeColor);
            fTime += Time.deltaTime;
            yield return null;
        }

        actionColorSet(endColor);
    }

    public IEnumerator FlashAndPulsePortrait(float fWaitTime, Color flashColor)
    {
        yield return new WaitForSeconds(fWaitTime);

        UIManagerScript.PlayCursorSound("OPSelect"); // was Skill Learnt

        Image flashImage = Instantiate(imagePortrait.gameObject, imagePortrait.gameObject.transform.parent).GetComponent<Image>();
        flashImage.color = flashColor;
        LeanTween.color(flashImage.rectTransform, Color.clear, 0.2f);

        flashImage.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
        LeanTween.scale(flashImage.rectTransform, new Vector3(1f, 1f, 1f), 0.2f).setEaseInOutBounce();

        flashImage.transform.SetAsFirstSibling();

        Destroy(flashImage.gameObject, 0.3f);
    }

    void Update()
    {
        if (GameMasterScript.heroPC == null)
        {
            return;
        }
        UpdateInfo();

        //Fun thing here to keep track of the EventSystem in the fullscreen UI
        //textName.text = EventSystem.current.currentSelectedGameObject.name;
    }

}

public partial class UIManagerScript
{
    public CSCharacterInfoBlock newCSBlock;
    private CSStatBars csStatBars;
    public RectTransform GetCSBlockImageTransform()
    {
        return newCSBlock.imagePortrait.transform as RectTransform;
    }

    public static void FlashCSPortrait(float fWaitTime, Color color)
    {
        singletonUIMS.newCSBlock.StartCoroutine(singletonUIMS.newCSBlock.FlashAndPulsePortrait(fWaitTime, color));
    }

    public static object Debug_TestCSBlock(params string[] args)
    {

        return "Done!";
    }

    public static void ShowUICharacterStatBlock()
    {
        if (singletonUIMS.newCSBlock == null)
        {
            singletonUIMS.newCSBlock = Instantiate(Resources.Load<GameObject>("Art/UI/prefab_characterinfoblock")).GetComponent<CSCharacterInfoBlock>();
            singletonUIMS.newCSBlock.gameObject.transform.SetParent(GameObject.Find("CSCBlockHolder").transform);
            singletonUIMS.newCSBlock.AssignHero(GameMasterScript.heroPCActor);

            RectTransform boxRect = singletonUIMS.newCSBlock.transform as RectTransform;
            boxRect.anchoredPosition = new Vector2(0, 0);
            boxRect.localScale = new Vector3(1, 1, 1);
        }

        singletonUIMS.newCSBlock.TurnOn();

        // Note, if we're showing this then we are also showing the Stat Bars.

        singletonUIMS.csStatBars.AssignHero(GameMasterScript.heroPCActor);

        singletonUIMS.csStatBars.TurnOn();
    }

    

    public static void HideUICharacterStatBlock( bool bHideImmediately = false)
    {
        if (singletonUIMS.newCSBlock != null)
        {
            singletonUIMS.newCSBlock.TurnOff();
        }

        // as above
        if (singletonUIMS.csStatBars != null)
        {
            singletonUIMS.csStatBars.TurnOff();
        }

        if (bHideImmediately)
        {
            singletonUIMS.newCSBlock.gameObject.SetActive(false);
            singletonUIMS.csStatBars.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Returns TRUE if skills, eq, inv, journal, settings, or char pages are open
    /// </summary>
    /// <returns></returns>
    public bool AnyMenuUIOpen()
    {
        if (GetCurrentFullScreenUI() == null && !GetWindowState(UITabs.OPTIONS) && !GetWindowState(UITabs.RUMORS))
        {
            return false;
        }
        return true;
    }

}