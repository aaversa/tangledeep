using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_SWITCH
using nn.util;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxScript : MonoBehaviour
{
    bool scale1FLastFrame = false;
    //#dialogrefactor: Bold Ass Mission #1
    //move all the objects in UIManager to this class, and have them accessible via accessors.
    //That way the outside code doesn't need to know the difference between a normal dialog and 
    //one with faces

    //Serialize Field is being added to objects I want to adjust in the editor, but not in the codebase
    //via direct public access

    [SerializeField]
    protected GameObject dialogBox;
    [SerializeField]
    protected GameObject dialogBoxText;
    [SerializeField]
    protected GameObject dialogBoxNextPageIcon;
    [SerializeField]
    protected GameObject dialogBoxCloseIcon;
    [SerializeField]
    protected TextMeshProUGUI txtDialogBoxMessage;

    [SerializeField]
    protected GameObject dialogValueSliderParent;
    [SerializeField]
    protected TextMeshProUGUI dialogValueSliderText;
    [SerializeField]
    protected string defaultSliderText;
    [SerializeField]
    protected Slider dialogValueSlider;

    [SerializeField]
    protected GameObject genericTextInput;
    [SerializeField]
    protected Image dialogBoxImage;
    [SerializeField]
    protected TMP_InputField genericTextInputField;
    [SerializeField]
    protected TextMeshProUGUI genericTextInputPlaceholder;
    [SerializeField]
    protected TextMeshProUGUI genericTextInputText;

    public virtual GameObject GetTextGameObject()  { return dialogBoxText; }
    public virtual TextMeshProUGUI GetDialogText() { return txtDialogBoxMessage; }

    private GameObject goAdvanceIconToDisplay;

#if UNITY_ANDROID || UNITY_IPHONE
    public const float TARGET_DIALOGBOX_SCALE = 1.25f;
#else
    public const float TARGET_DIALOGBOX_SCALE = 1f;
#endif

    /// <summary>
    /// Multiplier for button response text
    /// </summary>
#if UNITY_ANDROID || UNITY_IPHONE
    public const float DIALOGBOX_RESPONSE_SCALE = 1.25f;
#else
    public const float DIALOGBOX_RESPONSE_SCALE = 1f;
#endif

    /// <summary>
    /// Multiplier for MORE/CLOSE (book) icons
    /// </summary>
#if UNITY_ANDROID || UNITY_IPHONE
    public const float DIALOGBOX_ICON_SCALE = 1.25f;
#else
    public const float DIALOGBOX_ICON_SCALE = 2f;
#endif

    //Perhaps we are displaying something in front of the dialog box?
    private HashSet<GameObject> goChildrenToDisplay;
    public float fSwitchDialogScaleMultiplier = 1.0f;


    ContentSizeFitter myCSF;
    public ContentSizeFitter MyCSF
    {
        get
        {
            if (myCSF == null)
            {
                myCSF = GetComponent<ContentSizeFitter>();
            }
            return myCSF;
        }
    }

    VerticalLayoutGroup myVLG;
    public VerticalLayoutGroup MyLayoutGroup
    {
        get
        {
            if (myVLG == null)
            {
                myVLG = GetComponent<VerticalLayoutGroup>();
            }
            return myVLG;
        }
    }	
    /// <summary>
    /// Allows us to keep track of child objects on us, so we can clean 'em up later.
    /// </summary>
    /// <param name="newGO"></param>
    public void AddChildGameObject(GameObject newGO)
    {
        if (goChildrenToDisplay == null)
        {
            goChildrenToDisplay = new HashSet<GameObject>();
        }

        goChildrenToDisplay.Add(newGO);
    }

    /// <summary>
    /// Returns all child objects that we are tracking to the pool.
    /// </summary>
    public void ClearChildGameObjects()
    {
        if (goChildrenToDisplay == null) return;
        foreach (var o in goChildrenToDisplay)
        {
            GameMasterScript.ReturnToStack(o, o.name.Replace("(Clone)", string.Empty));
        }
        goChildrenToDisplay.Clear();
    }

    /// <summary>
    /// Returns the More or Next icon if either are active.
    /// </summary>
    /// <returns>A GameObject with the More or Next icon inside.</returns>
    public GameObject GetAdvanceIcon()
    {
        return goAdvanceIconToDisplay;
    }

    public virtual void DisableCloseNextIcons()
    {
        dialogBoxNextPageIcon.SetActive(false);
        dialogBoxCloseIcon.SetActive(false);
        goAdvanceIconToDisplay = null;
    }

    protected virtual void OnIconEnabled()
    {
        goAdvanceIconToDisplay.GetComponent<Animatable>().SetAnim("Default");
    }

    public void EnableNextIcon()
    {
        dialogBoxNextPageIcon.SetActive(true);
        dialogBoxCloseIcon.SetActive(false);

        RectTransform rt = dialogBoxNextPageIcon.GetComponent<RectTransform>();
        Vector3 newRTAnchoredPos = new Vector3(rt.anchoredPosition.x, 64f, 0f);
        StartCoroutine(WaitThenAdjustRectTransformAnchoredPos(rt, newRTAnchoredPos, 0.05f));

        goAdvanceIconToDisplay = dialogBoxNextPageIcon;
        OnIconEnabled();
    }

    public void EnableCloseIcon()
    {
        dialogBoxNextPageIcon.SetActive(false);
        dialogBoxCloseIcon.SetActive(true);

        RectTransform rt = dialogBoxCloseIcon.GetComponent<RectTransform>();
        Vector3 newRTAnchoredPos = new Vector3(rt.anchoredPosition.x, 64f, 0f);
        StartCoroutine(WaitThenAdjustRectTransformAnchoredPos(rt, newRTAnchoredPos, 0.05f));

        goAdvanceIconToDisplay = dialogBoxCloseIcon;
        OnIconEnabled();
    }

    // AA made a change to dialog boxes to ensure their Y-scale starts at 0f and increases (as intended), but
    // This made the Next/Close symbol positioning screwy. Setting it in the functions above did not work. 
    // However, waiting a very brief time and *then* setting it works just fine?
    IEnumerator WaitThenAdjustRectTransformAnchoredPos(RectTransform rt, Vector3 newPos, float time)
    {
        yield return new WaitForSeconds(time);
        rt.anchoredPosition = newPos;
    }

    //called by UIManager on Awake()
    public virtual void Init()
    {
        dialogBoxNextPageIcon.SetActive(false);
        dialogBoxCloseIcon.SetActive(false);
        if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            SetFonts();
        }
    }
    public void SetFonts()
    {
        FontManager.LocalizeMe(txtDialogBoxMessage,TDFonts.WHITE);
        txtDialogBoxMessage.UpdateFontAsset();        
        if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            FontManager.LocalizeMe(dialogValueSliderText, TDFonts.WHITE);

            FontManager.LocalizeMe(genericTextInputPlaceholder, TDFonts.WHITE);

            FontManager.LocalizeMe(genericTextInputText, TDFonts.WHITE);

            if (dialogValueSlider != null)
            {
                switch (StringManager.gameLanguage)
                {
                    case EGameLanguage.jp_japan:
                    case EGameLanguage.zh_cn:
                        // Dialog slider needs to be massaged for japanese
                        VerticalLayoutGroup vlg = dialogValueSliderParent.GetComponent<VerticalLayoutGroup>();
                        if (vlg != null)
                        {
                            vlg.padding.top = 60;
                            vlg.spacing = 60f;
                        }
                        RectTransform rt = dialogValueSliderParent.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(900f, 200f);
                        break;
                }
            }
        }


        transform.localScale = new Vector3(TARGET_DIALOGBOX_SCALE, 0f, TARGET_DIALOGBOX_SCALE); // to avoid pop-in on first enable
    }

    public void PrepareForTitleScreen()
    {
        SetFonts();
        UIManagerScript.ClearAllDialogOptions();
        UIManagerScript.CloseDialogBox();
        ActivateDialogBoxAndEnableFitters();

        Color dColor = new Color(0f, 0.4039f, 0.6705f, 0.8215f);
        var bg = dialogBox.GetComponent<Image>();
        bg.color = dColor;
        bg.enabled = true;

        txtDialogBoxMessage.enabled = false;
    }

    public void PrepareSelectSlot(bool bTurnBlueBGOn = true)
    {
        //turn off lots of things, and then turn some on
        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);

        //turn on the bold blue background behind the slot 
        if (bTurnBlueBGOn)
        {
            UIManagerScript.ActivateBGForCharacterCreation();
        }

        //we should already be enabled but whatevs
        //this is called in ToggleDialogBox, but I'm keeping the order from
        //UIManager for fear of destruction
        ActivateDialogBoxAndEnableFitters();

        //pretty up the background
        var bgImg = GetComponent<Image>();
        Color existing = bgImg.color;
        existing.a = 1.0f;
        bgImg.color = existing;

        float dialogBoxTextSize = 38f;
		
#if UNITY_SWITCH
        if (StringManager.gameLanguage == EGameLanguage.jp_japan && TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 0)
        {
            dialogBoxTextSize = 36f; // could be 30f... dunno...
        }
#endif

        FontManager.AdjustFontSize(txtDialogBoxMessage, dialogBoxTextSize);        
        txtDialogBoxMessage.enabled = true;
        dialogBoxText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Top;
        Vector2 size = new Vector2(1000f, 120f);

        //turn off lots of things, and then turn some on
        UIManagerScript.ToggleDialogBox(DialogType.SELECTSLOT, true, true, size, Vector2.zero);
    }

    public void PrepareForStartCharacterCreation()
    {
        //Toggle the box in exit mode
        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);

        //this was called above but it was from the original code so I will leave it.
        ActivateDialogBoxAndEnableFitters();

        //darken the background
        var img = GetComponent<Image>();
        Color existing = img.color;
        existing.a = 1.0f;
        img.color = existing;

        //size everything properly
        FontManager.AdjustFontSize(txtDialogBoxMessage, 38);        
        txtDialogBoxMessage.enabled = true;

        //flip some switches around 
        Vector2 size = new Vector2(1000f, 120f);
        UIManagerScript.ToggleDialogBox(DialogType.GAMEINTRO, true, true, size, Vector2.zero);
    }

    public void OverrideConversationPos(Vector2 vOverridePos)
    {
        transform.localPosition = vOverridePos;
        // If we're doing this, we might be overlapping the BG Image Bar. Therefore, make sure the color for our 9slice is a bit darker.
        SetImageColor(new Color(1f, 1f, 1f, (220f / 255f)));
    }

    public void SetImageColor(Color c)
    {
        GetComponent<Image>().color = c; // This is our 9slice
    }

    public void ResetToDefaultPosition()
    {
        transform.localPosition = new Vector2(0f, 90f);
    }

    public virtual void OverrideConversationSize( Vector2 vOverrideSize )
    {
        MyCSF.enabled = false;
        //GetComponent<ContentSizeFitter>().enabled = false;
        GetComponent<RectTransform>().sizeDelta = vOverrideSize;
        dialogBoxText.GetComponent<RectTransform>().sizeDelta = new Vector2(vOverrideSize.x - 35f, vOverrideSize.y - 35f);
        MyCSF.enabled = true; // once we've manually resized the box, the CSF can adjust borders
    }

    public virtual void SetHorizontalFitType(ContentSizeFitter.FitMode fitModeSquad)
    {
        MyCSF.enabled = true;
        dialogBoxText.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 300f);
        MyCSF.horizontalFit = fitModeSquad;
    }


    public virtual void ActivateDialogBoxAndEnableFitters()
    {
        MyLayoutGroup.enabled = true;
        dialogBoxText.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 300f);
        MyCSF.enabled = true;
        gameObject.SetActive(true);
    }

    public virtual string GetButtonRef()
    {
        return "DialogButtonWithImage";
    }

    //Cleans up the bottom of the dialog box to adjust for spacing needed by
    //multiple answers
    //public virtual void AdjustSettingsForResponsesAfterDialogBoxStart(ref float fAdjustedCursorOffset)
    public virtual void AdjustSettingsForResponsesAfterDialogBoxStart(bool mainMenu, ref float fAdjustedCursorOffset)
    {
        MyLayoutGroup.padding.bottom = 30;
        fAdjustedCursorOffset = -5f;

        if (UIManagerScript.CurrentConversationIsSelectSlot())
        {
            MyLayoutGroup.spacing = 15f;
            MyLayoutGroup.padding.left = 25;
            MyLayoutGroup.padding.right = 25;
            dialogBoxText.GetComponent<LayoutElement>().minHeight = 60f;
        }
        else // This is for virtually all dialogs, basically anything but title screen select slot.
        {
            if (GameMasterScript.gmsSingleton.titleScreenGMS)
            {
                MyLayoutGroup.spacing = 0f;
                MyLayoutGroup.padding.left = 100;
                MyLayoutGroup.padding.right = 100;
                MyLayoutGroup.padding.top = 40;

                float minHeight = 120f;

                if (mainMenu)
                {
                    minHeight = 0f;
                }
                else if (TitleScreenScript.CreateStage == CreationStages.SELECTMODE || TitleScreenScript.CreateStage == CreationStages.PERKSELECT)
                {
                    minHeight = 30f;
                }

                dialogBoxText.GetComponent<LayoutElement>().minHeight = mainMenu ? 0f : minHeight;
            }
            else
            {
                MyLayoutGroup.spacing = 0f;
                MyLayoutGroup.padding.left = 50;
                MyLayoutGroup.padding.right = 50;
                MyLayoutGroup.padding.top = 40;
                dialogBoxText.GetComponent<LayoutElement>().minHeight = mainMenu ? 0f : 120f;
            }

        }
    }

    public virtual void SetLayoutElementMinDimensions(float width, float height)
    {
        var le = txtDialogBoxMessage.gameObject.GetComponent<LayoutElement>();
        le.minWidth = width;
        le.minHeight = height;
    }

    // ===========================================================================
    // Older code that worked with the one generic dialog box object. Could still work now.
    // probably


    CanvasGroup cg;
    bool fadingIn;
    bool fadingOut;
    float timeFadeStarted;
    float fadeTime;
    bool linkedCG;

    //Music playing when the dialog started
    public string strMusicWhenConvoStarted;

    //If this is true, go back to playing what we were playing before when this conversation ends.
    private bool bRestoreMusicOnConvoEnd;

    public string strCutsceneScriptOnConvoEnd;

    public static bool bWaitingToClearOptions;

    //if this value is > 0f, the dialog box will not draw or accept input.
    private float fDelaySeconds;
    public bool IsDelayed() { return fDelaySeconds > 0f; }

    private Dictionary<GameObject, bool> preDelayState;

#if UNITY_EDITOR
    bool yScaleWas0LastFrame = false;
#endif

    // Use this for initialization
    void Awake () {
        cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            linkedCG = true;
        }
        fadingIn = false;
        fadingOut = false;
	}
	
    public void StopFadeImmediatelyIfFadingOut()
    {
        if (!fadingOut) return;        
        fadingOut = false;
        fadingIn = false;
        if (!linkedCG)
        {
            cg = GetComponent<CanvasGroup>();
            linkedCG = true;
        }
        //Debug.Log("Stoping fade immediately, " + cg.alpha);
        cg.alpha = 1f;
        gameObject.SetActive(true);
    }

	// Update is called once per frame
	void Update ()
    {
        //tick down our delay if we have one.
        if (fDelaySeconds > 0f)
        {
            fDelaySeconds -= Time.deltaTime;
            if (fDelaySeconds <= 0f)
            {
                UnHideDialogBox();
            }
        }


		if (fadingIn)
        {
            if (!linkedCG) return;
            float percentComplete = (Time.time - timeFadeStarted) / fadeTime;
            cg.alpha = percentComplete;
            if (percentComplete >= 1.0f)
            {
                fadingIn = false;
            }
        }
        else if (fadingOut)
        {
            if (!linkedCG)
            {
                gameObject.SetActive(false);
                return;
            }
            float percentComplete = (Time.time - timeFadeStarted) / fadeTime;
            cg.alpha = 1f - percentComplete;
            if (percentComplete >= 1.0f)
            {
                fadingOut = false;
                gameObject.SetActive(false);
                transform.localScale = new Vector3(TARGET_DIALOGBOX_SCALE, 0f, TARGET_DIALOGBOX_SCALE); // to avoid pop-in next time we start the box
            }
        }
    }

    public bool FadedOutCompletely()
    {
        if (!linkedCG) return true;
        if (cg.alpha == 0) return true;
        return false;
    }

    public void FadeIn(float time)
    {
        if (!linkedCG)
        {
            return;
        }
        //Debug.Log("Fade in over " + time);
        fadingOut = false;
        timeFadeStarted = Time.time;
        fadingIn = true;
        fadeTime = time;
        cg.alpha = 0f;
        transform.localScale = new Vector3(1f, 0f, 1f);
    }

    public void FadeOut(float fTime = UIManagerScript.DIALOG_FADEOUT_TIME)
    {
        if (!linkedCG)
        {
            return;
        }
        //Debug.Log("Fade out over " + fTime);
        cg.alpha = 1.0f;
        fadingIn = false;
        timeFadeStarted = Time.time;
        fadingOut = true;
        fadeTime = fTime;
    }


    /*
     *  playmusic|[trackname]|(optional "restoreonend")
     *  
     *  fadeout|(optional time)
     *  
     *  stop
     *  
     *  playcue|[cuename]|(optional "stopmusic")        <--  for OA requested record scratch + music stop
     *  
     *  restoremusic <-- immediately fades in the music that was playing at the dialog start. Use to recover from a record scratch :)
     * 
     */

    public void HandleAudioCommands(string strCommandString)
    {
        string[] splitCommands = strCommandString.Split('|');

        switch (splitCommands[0])
        {
            case "playmusic":
            {
                string strTrack = splitCommands[1];
                if (splitCommands.Length > 2 && splitCommands[2] == "restoreonend")
                {
                    bRestoreMusicOnConvoEnd = true;
                }
				MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade(strTrack);

            }
                break;

            case "fadeout":
            {
                float fTime = 1.0f;
                if (splitCommands.Length > 1)
                {
                    float.TryParse(splitCommands[1], out fTime);
                }

                GameMasterScript.musicManager.Fadeout(fTime);
            }
                break;

            case "stop":
            {
                    //there is no Stop routine :( 
                GameMasterScript.musicManager.FadeoutThenSetAllToZero(0.01f);
            }
                break;
            case "restoremusic":
            {
				MusicManagerScript.RequestPlayMusic(strMusicWhenConvoStarted, true, true, true, true, true);

            }
                break;

            case "playcue":
            {
                string strCue = splitCommands[1];

                if (!MusicManagerScript.PlayCutsceneSound(strCue))
                {
                    //bad bad bad bad bad bad bad bad bad bad bad bad bad bad bad
                    //:bad:
                    UIManagerScript.PlaySound(strCue);
                }

                if (splitCommands.Length > 2 && splitCommands[2] == "stopmusic")
                {
                    GameMasterScript.musicManager.Fadeout(0.01f);
                }
            }
                break;
        }
    }

    public void OnEndConversation()
    {
        //Play the original music if we are asked to do so.
        if (bRestoreMusicOnConvoEnd && (!string.IsNullOrEmpty(strMusicWhenConvoStarted)))
        {
				MusicManagerScript.RequestPlayMusic(strMusicWhenConvoStarted, true, true, true, true, true);

        }

        
        //perhaps run a script
        if (!string.IsNullOrEmpty(strCutsceneScriptOnConvoEnd))
        {
            MethodInfo cutsceneMethod = CustomAlgorithms.TryGetMethod(typeof(Cutscenes), strCutsceneScriptOnConvoEnd);
            if (cutsceneMethod == null)
            {
                if (Debug.isDebugBuild) Debug.LogError("Very Bad Error: trying to call cutscene method '" + strCutsceneScriptOnConvoEnd + "' but it does not exist." );
            }
            else
            {                
                cutsceneMethod.Invoke(null,null);
            }
        }

        //reset for next time
        bRestoreMusicOnConvoEnd = false;
        strCutsceneScriptOnConvoEnd = null;

    }

    // When this coroutine is called, we don't necessarily know what DialogBox object is being used
    // Thus we check UIManagerScript.myDialogBoxComponent in case it went from regular -> faces, or faces -> regular
    public static IEnumerator PlayOpenDialogSoundIfNotDelayed()
    {
        yield return null; // Wait a frame while we figure out if the current text branch has a delay.
        if (!UIManagerScript.myDialogBoxComponent.IsDelayed())
        {
            UIManagerScript.PlayCursorSound("OpenDialog");
        }
        else
        {
            Cutscenes.WaitThenPlayCursorSound("OpenDialog", UIManagerScript.myDialogBoxComponent.fDelaySeconds);
        }
    }

    public void SetDelay(float fSeconds)
    {
        if (fSeconds <= 0f)
        {
            return;
        }

        fDelaySeconds = fSeconds;

        HideButDoNotDisable();
    }

    void HideButDoNotDisable()
    {
        //Freeze the current dialog state
        preDelayState = new Dictionary<GameObject, bool>();
        Transform[] allThemTs = gameObject.GetComponentsInChildren<Transform>();

        //Store the state of each gameobject.
        foreach (var t in allThemTs)
        {
            if (t.gameObject == gameObject)
            {
                continue;
            }

            preDelayState.Add(t.gameObject, t.gameObject.activeInHierarchy);
        }

        //now turn them all off
        foreach (var t in allThemTs)
        {
            if (t.gameObject == gameObject)
            {
                continue;
            }

            t.gameObject.SetActive(false);
        }

        //hide our own image, but don't disable us
        Image borderBox = GetComponent<Image>();
        borderBox.enabled = false;
    }

    void UnHideDialogBox()
    {
        // Also make sure input is enabled again.
        GameMasterScript.SetAnimationPlaying(false);

        if( preDelayState != null )
        {
            foreach (var kvp in preDelayState)
            {
                if (kvp.Key != null)
                {   
                    kvp.Key.SetActive(kvp.Value);
                }
            }

            //clear this so we don't hold on to a un-wanted state from before
            preDelayState = null;
        }

        //restore our grand border
        Image borderBox = GetComponent<Image>();
        borderBox.enabled = true;

    }

    public void AddTheseObjectsToDelay(List<UIManagerScript.UIObject> theseObjects)
    {
        foreach (var s in theseObjects)
        {
            if (preDelayState.ContainsKey(s.gameObj)) continue;

            preDelayState.Add(s.gameObj, true);
            s.gameObj.SetActive(false);
        }
    }

    public void ClearDelayState()
    {
        preDelayState = null;
    }

    public void CalculateLayoutInputs()
    {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        MyLayoutGroup.CalculateLayoutInputVertical();
        MyLayoutGroup.CalculateLayoutInputHorizontal();
#endif
    }

    IEnumerator _WaitThenMakeTextWhite(float time)
    {
        yield return new WaitForSeconds(time);
        MyLayoutGroup.CalculateLayoutInputVertical();        
        GetDialogText().color = Color.white;
        // Try to get this box sizing to stop being so bad by moving the cursor...?
        try
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.dialogUIObjects[0]);
        }
        catch(Exception e)
        {

        }
        
    }

    public void WaitThenMakeTextWhite(float time)
    {
        StartCoroutine(_WaitThenMakeTextWhite(time));
    }

    // Some dialogs in some areas of the game may need culture-specific adjustments to size/position
    // Otherwise, character size, sentence structure (etc) can cause visual issues
    public virtual void DoSizeAdjustmentsByCulture()
    {
        // The feats dialog needs custom sizing in Japanese
        if (UIManagerScript.currentTextBranch.branchRefName == "player_select_feats")
        {
            switch(StringManager.gameLanguage)
            {
                case EGameLanguage.jp_japan:
                case EGameLanguage.zh_cn:
                    foreach(UIManagerScript.UIObject button in UIManagerScript.dialogUIObjects)
                    {
                        // For each feat option, shrink the header and expand the body
                        if (button.button.threeColumnStyle)
                        {
                            DialogButtonScript dbs = button.gameObj.GetComponent<DialogButtonScript>();

                            RectTransform rt = dbs.headerText.GetComponent<RectTransform>();
                            rt.sizeDelta = new Vector2(160f, rt.sizeDelta.y);

                            rt = dbs.bodyText.GetComponent<RectTransform>();

                            float size = 860f;
                            // previously, 1024f was used for UNITY_SWITCH

                            rt.sizeDelta = new Vector2(size, rt.sizeDelta.y);
                        }
                    }
                    break;
            }
        }
    }

}


//Dialog specific UIManagerScript functions / fields can live here.
public partial class UIManagerScript
{
    public enum EDialogBoxScriptTypes
    {
        @default = 0,
        conversation_with_faces,
    }


    private static EDialogBoxScriptTypes currentDialogBoxScriptType;
    private static DialogBoxScript  _myDBS;
    public static ButtonCombo nextPageResponseButton;
    public static ButtonCombo previousPageResponseButton;

    //We should not need to reference this directly to get components
    //and change values. The DialogBoxScript will have all the accessors we need.

    [SerializeField]
    private GameObject goDialogBoxDefault;

    [SerializeField]
    public GameObject goDialogBoxFaces;

    // These are ALL dialog options that are valid in the current branch. We have the right flags, required items, etc.
    public static List<ButtonCombo> validResponsesInCurrentTextBranch;

    // If we have more than the MAX displayable responses, we might start at a non-zero index.
    public static int startIndexOfCurrentResponseList; 
    
    const int MAX_DISPLAY_RESPONSES_PER_BRANCH = 11; // Max for Jorito's dialog.
	public static GameObject GetDialogBoxObject() { return singletonUIMS.goDialogBoxDefault; }

    static void InitializeDialogBoxComponent()
    {
        _myDBS = singletonUIMS.goDialogBoxDefault.GetComponent<DialogBoxScript>();
        _myDBS.Init();
    }

    public static DialogBoxScript myDialogBoxComponent
    {
        get { return _myDBS ?? (_myDBS = singletonUIMS.goDialogBoxDefault.GetComponent<DialogBoxScript>()); }
    }

    public static DialogBoxScript SetDialogBoxScriptType(EDialogBoxScriptTypes newType)
    {
        currentDialogBoxScriptType = newType;
        DialogBoxScript switchScript;
        switch (newType)
        {
            case EDialogBoxScriptTypes.@default:
                switchScript = singletonUIMS.goDialogBoxDefault.GetComponent<DialogBoxScript>();
                break;
            case EDialogBoxScriptTypes.conversation_with_faces:
                switchScript = singletonUIMS.goDialogBoxFaces.GetComponent<DialogBoxScript>();
                break;
            default:
                throw new ArgumentOutOfRangeException("newType", newType, null);
        }

        //if we just changed objects, turn off the old one.
        if (switchScript != _myDBS)
        {
            _myDBS.gameObject.SetActive(false);
            _myDBS = switchScript;
        }

        return myDialogBoxComponent;
    }

    public static void ActivateBGForCharacterCreation()
    {
        singletonUIMS.characterCreationBG.SetActive(true);
        singletonUIMS.buildText.SetActive(false);

        // Don't need promo text on Switch.
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        singletonUIMS.switchPromoTextCG.gameObject.SetActive(false);
#endif
    }

    public static object Debug_SetScriptOnNextDialogEnd(params string[] args)
    {
        if (args.Length < 2)
        {
            return "Requires a parameter, the script you want to run at the end of the next dialog.";
        }

        myDialogBoxComponent.strCutsceneScriptOnConvoEnd = args[1];
        return "Will run '" + myDialogBoxComponent.strCutsceneScriptOnConvoEnd + "' at the end of the next dialog.";
    }

    public static void Dialog_SetScriptOnConvoEnd(string strScriptToRun)
    {
        myDialogBoxComponent.strCutsceneScriptOnConvoEnd = strScriptToRun;
    }

    //This will cause the dialog box to not display or take input for this many seconds
    public static void Dialog_DelayDialog(float fSeconds)
    {
        myDialogBoxComponent.SetDelay(fSeconds);
    }
    
    // Instantiates all physical, usable buttons in current text branch
    public static void CreateResponseButtonsForCurrentBranch()
    {
        // Store all responses we can ACTUALLY use in the list below. We meet the right flags/requirements for these
        validResponsesInCurrentTextBranch.Clear();

        //Here's where we evaluate each button for display
        foreach (ButtonCombo bc in currentTextBranch.responses)
        {
            //Don't show responses where we don't meet the data requirements
            if (bc.actionRef == "backtotown" && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
            {
                continue;
            }

            if (bc.reqFlags.Count > 0)
            {
                bool anyFlagsFailed = false;
                foreach (DialogButtonResponseFlag dbFlag in bc.reqFlags)
                {                    
                    int heroFlagValue = dbFlag.isMetaDataFlag ? MetaProgressScript.ReadMetaProgress(dbFlag.flagName) : GameMasterScript.heroPCActor.ReadActorData(dbFlag.flagName);
                    //Debug.Log(dbFlag.flagName + " " + dbFlag.flagMinValue + " " + dbFlag.flagMaxValue + " " + dbFlag.isMetaDataFlag + " ____ " + heroFlagValue);
                    //Debug.Log("Meta: " + MetaProgressScript.ReadMetaProgress(dbFlag.flagName) + " Regular: " + GameMasterScript.heroPCActor.ReadActorData(dbFlag.flagName));
                    if (heroFlagValue < dbFlag.flagMinValue || heroFlagValue > dbFlag.flagMaxValue)
                    {
                        anyFlagsFailed = true;
                        break;
                    }
                }
                if (anyFlagsFailed)
                {
                    continue;
                }
            }

            //Don't show responses that require items if we don't have them.
            if (bc.reqItems.Keys.Count > 0)
            {
                bool anyFlagsFailed = false;
                foreach (string iKey in bc.reqItems.Keys)
                {
                    int quantityReq = bc.reqItems[iKey];
                    anyFlagsFailed = false;
                    if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity(iKey) < quantityReq)
                    {
                        anyFlagsFailed = true;
                        //Debug.Log("Player does not have enough " + iKey);
                        break;
                    }
                }
                if (anyFlagsFailed)
                {
                    continue;
                }
            }

            // At this point, we can definitely use this response.
            validResponsesInCurrentTextBranch.Add(bc); 
        }

        // Now that we have a list, do the instantiation.
        InstantiateButtonsForCurrentDialog();
    }

    // Physically instantiate buttons based on our existing list of valid responses (ButtonCombos), instantiate dialog
    static void InstantiateButtonsForCurrentDialog()
    {        
        List<ButtonCombo> buttonsToInstantiate = null;
        bool addNextPageButton = false;
        bool addPreviousPageButton = false;

        int maxResponsesForThisDialog = GetMaxDisplayResponsesForDialog();

        bool saveLoadOrManageDataDialog = false;

        if (UIManagerScript.CurrentConversationIsSelectSlot())       
        {
            saveLoadOrManageDataDialog = true;
        }            

        // We might have more responses than can be physically displayed on screen
        // Don't bother with this in char creation, this is just gameplay stuff.
        // But DO bother with it for load/new game which is limited to 5 responses
        if (validResponsesInCurrentTextBranch.Count > maxResponsesForThisDialog && (GameMasterScript.gameLoadSequenceCompleted || saveLoadOrManageDataDialog)) 
        {
            // In this case, display up to our MAX constant, not going past the end of the List of course.
            // We will also need to add "Next Page" and "Previous Page" buttons so you can navigate all responses
            buttonsToInstantiate = new List<ButtonCombo>();
            int maxIndex = startIndexOfCurrentResponseList + maxResponsesForThisDialog;

            bool atFinalPage = maxIndex >= validResponsesInCurrentTextBranch.Count;

            if (startIndexOfCurrentResponseList > 0)
            {
                addPreviousPageButton = true;
            }

            if (atFinalPage)
            {
                maxIndex = validResponsesInCurrentTextBranch.Count;
            }
            else
            {
                addNextPageButton = true;
            }
            for (int i = startIndexOfCurrentResponseList; i < maxIndex; i++)
            {
                // startIndexOfCurrentResponseList is basically an offset, so we begin instantiating from there instead.
                buttonsToInstantiate.Add(validResponsesInCurrentTextBranch[i]);
            }

            if (addPreviousPageButton)
            {
                buttonsToInstantiate.Add(previousPageResponseButton);
            }
            if (addNextPageButton)
            {
                buttonsToInstantiate.Add(nextPageResponseButton);
            }

        }
        else
        {
            buttonsToInstantiate = validResponsesInCurrentTextBranch;
        }

        foreach(ButtonCombo bc in buttonsToInstantiate)
        {
            if (!bc.visible) continue;

            GameObject instantiatedButton = null;

            string modText = "";

            if (dialogBoxType == DialogType.LEVELUP && currentConversation.refName == "levelupstats")
            {
                bc.threeColumnStyle = true;
                int curStat = 0;
                switch (bc.actionRef)
                {
                    case "STRENGTH":
                        curStat = (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.STRENGTH);
                        break;
                    case "DISCIPLINE":
                        curStat = (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.DISCIPLINE);
                        break;
                    case "SWIFTNESS":
                        curStat = (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SWIFTNESS);
                        break;
                    case "SPIRIT":
                        curStat = (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SPIRIT);
                        break;
                    case "GUILE":
                        curStat = (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.GUILE);
                        break;
                }

                bc.headerText = "<color=yellow>" + StringManager.GetString("stat_" + bc.actionRef.ToLowerInvariant()) + "</color>";

                bc.headerText += " (" + curStat + ")";

                if (curStat != 0)
                {
                    //modText = bc.buttonText + " " + UIManagerScript.cyanHexColor + StringManager.GetString("misc_current") + ":</color> <color=yellow>" + curStat + "</color>\n\n";
                }
            }

            string physicalButtonObjectRef = myDialogBoxComponent.GetButtonRef();

            if (bc.threeColumnStyle)
            {
                physicalButtonObjectRef = "DialogButtonThreeColumn";
            }

            bool foundUnusedButton = false;
            // We don't need to instantiate the button if the dialog box already has what we're looking for.
            foreach (Transform child in myDialogBoxComponent.gameObject.transform)
            {
                if (!child.gameObject.activeSelf && child.name.Contains(physicalButtonObjectRef))
                {
                    instantiatedButton = child.gameObject;
                    instantiatedButton.SetActive(true);
                    // For the feat selection screen, we have to move things to the bottom of the hierarchy in order of 
                    // creation. I don't think this will be necessary for other dialogs... at least I haven't noticed
                    // any evidence this would be a problem elsewhere.
                    if (dialogBoxType == DialogType.CCFEATSELECT || currentConversation.refName == "dialog_spellshape_toggle")
                    {
                        instantiatedButton.transform.SetAsLastSibling();
                    }
                    foundUnusedButton = true;
                    break;
                }
            }

            if (!foundUnusedButton)
            {
                instantiatedButton = GameMasterScript.TDInstantiate(physicalButtonObjectRef);
            }
            
            DialogButtonScript btnScript = instantiatedButton.GetComponent<DialogButtonScript>();
            btnScript.InitializeButton();
            if (!btnScript.parented)
            {
                instantiatedButton.transform.SetParent(myDialogBoxComponent.gameObject.transform);
                instantiatedButton.transform.localScale = new Vector3(DialogBoxScript.DIALOGBOX_RESPONSE_SCALE, DialogBoxScript.DIALOGBOX_RESPONSE_SCALE, DialogBoxScript.DIALOGBOX_RESPONSE_SCALE);
                btnScript.parented = true;
            }

            btnScript.UpdateLayoutElement(saveLoadOrManageDataDialog);
            
            Vector2 bodyTextSize = new Vector2(800f, 54f); // default
            float containerSizeX = 950f; // default
            int leftPaddingHLG = 0;
            RectTransform rt = btnScript.bodyText.GetComponent<RectTransform>();
            btnScript.myLayoutGroup.padding.top = 0;
            btnScript.myLayoutGroup.padding.bottom = 0;

            // Bad special case to center stuff in the levelup box :/
            if (bc.threeColumnStyle && dialogBoxType == DialogType.LEVELUP)
            {
                leftPaddingHLG = -75;
                if (StringManager.gameLanguage == EGameLanguage.jp_japan || StringManager.gameLanguage == EGameLanguage.zh_cn)
                {
                    bodyTextSize = new Vector2(950f, 54f);
                    containerSizeX = 1100f;
                }
            }
            else if (bc.threeColumnStyle)
            {
                leftPaddingHLG = -50;
                // Spellshape toggle window needs more room too.
                if (StringManager.gameLanguage == EGameLanguage.jp_japan || StringManager.gameLanguage == EGameLanguage.zh_cn) 
                {
                    // Reset non-level up 3column style
                }
                if (currentConversation.refName == "dialog_spellshape_toggle")
                {
                    bodyTextSize = new Vector2(1000f, 54f); // Body text
                    containerSizeX = 1200f;
                }

                if (bc.extraVerticalPadding > 0f)
                {
                    btnScript.myLayoutGroup.padding.top = bc.extraVerticalPadding / 2;
                    btnScript.myLayoutGroup.padding.bottom = bc.extraVerticalPadding / 2;
                }
            }

            if (bc.threeColumnStyle)
            {
                rt.sizeDelta = bodyTextSize;
                rt = btnScript.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(containerSizeX, rt.sizeDelta.y); // entire button container
                instantiatedButton.GetComponent<HorizontalLayoutGroup>().padding.left = leftPaddingHLG;
            }

            if (!string.IsNullOrEmpty(bc.spriteRef))
            {
                btnScript.iconSprite.sprite = LoadSpriteFromDict(dictUIGraphics, bc.spriteRef);
            }
            else
            {
                btnScript.iconSprite.gameObject.SetActive(false);
            }

            btnScript.myID = dialogObjects.Count + startIndexOfCurrentResponseList; // Offset must now be taken into account if we have multi pages.
            btnScript.myResponse = bc.dbr;
            dialogObjects.Add(instantiatedButton);

            string txtToDo = bc.buttonText;

            if (modText != "")
            {
                txtToDo = modText;
            }

            bool buttonTextChanged = true;

            if (bc.actionRef == "newquest")
            {
                txtToDo = StringManager.GetString("misc_quest_heard");
            }
            else if (bc.actionRef == "buypetinsurance")
            {
                txtToDo = StringManager.GetString("dialog_buypetinsurance") + " (<color=yellow>" + MonsterCorralScript.GetPetInsuranceCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>)";
            }
            else if (bc.actionRef == "healme_gold")
            {
                txtToDo = StringManager.GetString("dialog_healmeplease") + " (" + GameMasterScript.GetHealerCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else if (bc.actionRef == "healme_jp")
            {
                txtToDo = StringManager.GetString("dialog_healmeplease") + " (" + GameMasterScript.GetHealerCostJP() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP) + ")";
            }
            else if (bc.actionRef == "changejobs")
            {
                txtToDo = StringManager.GetString("dialog_changejob") + " (" + GameMasterScript.GetJobChangeCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else if (bc.actionRef == "blessxp")
            {
                txtToDo = StringManager.GetString("dialog_blessingofwisdom") + " (" + GameMasterScript.GetBlessingCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else if (bc.actionRef == "blessjp")
            {
                txtToDo = StringManager.GetString("dialog_blessingofmastery") + " (" + GameMasterScript.GetBlessingCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else if (bc.actionRef == "blessattack")
            {
                txtToDo = StringManager.GetString("dialog_blessingofpower") + " (" + GameMasterScript.GetBlessingCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else if (bc.actionRef == "blessdefense")
            {
                txtToDo = StringManager.GetString("dialog_blessingofprotection") + " (" + GameMasterScript.GetBlessingCost() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";
            }
            else
            {
                buttonTextChanged = false;
            }

            if (bc.buttonText.Contains("^"))
            {
                buttonTextChanged = true;
            }

            if (saveLoadOrManageDataDialog && bc.actionRef != "changepages")
            {
                //Debug.Log("Deactivating button " + instantiatedButton.gameObject.name);
                EnableDialogOption(instantiatedButton, txtToDo, btnScript, bc, buttonTextChanged);
                instantiatedButton.gameObject.SetActive(false);
                continue;
            }

            EnableDialogOption(instantiatedButton, txtToDo, btnScript, bc, buttonTextChanged);

            UIObject dialogUIThing = new UIObject();
            dialogUIThing.gameObj = instantiatedButton;
            dialogUIThing.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
            dialogUIThing.onSubmitValue = (int)bc.dbr;
            dialogUIThing.enabled = true;
            dialogUIThing.button = bc;
            dialogUIObjects.Add(dialogUIThing);
        }
    }

    // Moving to NEXT or PREVIOUS page of dialog responses if we have too many responses to display at once
    public static void ChangeDialogResponsePages(bool nextPage)
    {        
        // Adjust our offset
        if (nextPage)
        {
            startIndexOfCurrentResponseList += GetMaxDisplayResponsesForDialog();
        }
        else
        {
            startIndexOfCurrentResponseList -= GetMaxDisplayResponsesForDialog();
        }

        // Clamp it
        if (startIndexOfCurrentResponseList < 0)
        {
            startIndexOfCurrentResponseList = 0;
        }
        if (startIndexOfCurrentResponseList >= validResponsesInCurrentTextBranch.Count)
        {
            startIndexOfCurrentResponseList = validResponsesInCurrentTextBranch.Count - 1;
        }

        // Disable then clear all existing responses.
        foreach(UIObject uiOBJ in dialogUIObjects)
        {
            uiOBJ.enabled = false;
            uiOBJ.gameObj.SetActive(false);            
        }

        if (currentConversation != null && (currentConversation.refName == "loadgame" || currentConversation.refName == "newgame" || currentConversation.refName == "managedata"))
        {
            for (int i = 0; i < UIManagerScript.saveDataDisplayComponents.Length; i++)
            {
                UIManagerScript.saveDataDisplayComponents[i].gameObject.SetActive(false);
            }
        }

        dialogObjects.Clear();
        dialogUIObjects.Clear();
        allUIObjects.Clear();

        // And now instantiate our new list of buttons.
        InstantiateButtonsForCurrentDialog();

        // Why do we need 3 different lists for this. I'm sorry. It's old cruft.
        for (int i = 0; i < dialogUIObjects.Count; i++)
        {
            allUIObjects.Add(dialogUIObjects[i]);
        }

        // Put our cursor at the top of the response list.
        ChangeUIFocusAndAlignCursor(dialogUIObjects[0]);

        // Reconnect neighbors to ensure we're not looking at neighbors that aren't displayed.
        for (int i = 0; i < dialogUIObjects.Count; i++)
        {
            if (i == 0) // Top of list wraps to bottom
            {
                dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[dialogUIObjects.Count - 1];
            }
            else
            {
                dialogUIObjects[i].neighbors[(int)Directions.NORTH] = dialogUIObjects[i-1];
            }

            if (i == dialogUIObjects.Count-1) // Bottom of list wraps to top
            {
                dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[0];
            }
            else
            {
                dialogUIObjects[i].neighbors[(int)Directions.SOUTH] = dialogUIObjects[i + 1];
            }
        }

        dialogBoxOpen = true; // Make sure this didn't get changed for any reason.

        if (TitleScreenScript.CreateStage == CreationStages.SELECTSLOT && GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            TitleScreenScript.titleScreenSingleton.OnSelectSlotPagesChanged();
        }        
    }

    public static bool IsConversationInvalid(Conversation c)
    {
        int NUM_INFUSE_UPGRADES = 3;
        for (int i = 1; i <= NUM_INFUSE_UPGRADES; i++)
        {
            int heroVal = GameMasterScript.heroPCActor.ReadActorData("infuse" + i);
            if (c.refName == "flask" + i + "_upgrade" && heroVal != 99)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckForNameInputOpen()
    {
        return genericTextInputField.IsActive();
    }

    static int GetMaxDisplayResponsesForDialog()
    {
        int numResponses = MAX_DISPLAY_RESPONSES_PER_BRANCH;

        if (StringManager.gameLanguage == EGameLanguage.es_spain)
        {
            numResponses -= 3;
        }

        if (currentConversation != null)
        {
            switch(currentConversation.refName)
            {
                case "loadgame":
                case "newgame":
                case "managedata":
                    numResponses = 3;
                    /* if (StringManager.gameLanguage != EGameLanguage.en_us && StringManager.gameLanguage != EGameLanguage.de_germany)
                    {
                        numResponses = 3;
                    }
                    else
                    {
                        numResponses = 4;
                    } */
                    
                    break;
            }
        }

        return numResponses;
    }
}


