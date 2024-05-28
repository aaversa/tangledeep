using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System;

public partial class UIManagerScript
{

    public static void HideDialogBoxImage()
    {
        if (dialogBoxImage != null)
        {
            dialogBoxImageLayoutParent.SetActive(false);
            //dialogBoxImage.gameObject.SetActive(false);
        }
    }

    public static void ShowDialogBoxImage(Sprite spr, float scaleMult, bool bForceToNativeSize = false)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            return;
        }

        // Scale mult other than 1f screws up dialog box, #todo fix this
        // Until then
        scaleMult = 1f;

        dialogBoxImageLayoutParent.SetActive(true);
        dialogBoxImageLayoutParent.GetComponent<RectTransform>().localScale = Vector3.one;
        dialogBoxImage.sprite = spr;
        RectTransform rt = dialogBoxImage.GetComponent<RectTransform>();
        rt.sizeDelta.Set(scaleMult, scaleMult);
        rt.localScale = new Vector3(scaleMult, scaleMult, scaleMult);
        RectTransform dbilpRT = dialogBoxImageLayoutParent.GetComponent<RectTransform>();
#if UNITY_XBOXONE
        if (spr.name == "tutorial_movement_main_XB1")
        {
            rt.transform.localPosition = new Vector2(0f, 15f); // position of image within parent
            rt.sizeDelta = new Vector2(662f, 382f);
            dbilpRT.sizeDelta = new Vector2(100f, 400f);
        }
#elif UNITY_PS4
        if (spr.name == "tutorial_movement_main_PS4")
        {
            rt.transform.localPosition = new Vector2(0f, 15f); // position of image within parent
            rt.sizeDelta = new Vector2(662f, 382f);
            dbilpRT.sizeDelta = new Vector2(100f, 400f);
        }
#elif UNITY_ANDROID
        if (spr.name == "tutorial_movement_main_ANDROID")
        {
            rt.transform.localPosition = new Vector2(0f, 15f); // position of image within parent
            rt.sizeDelta = new Vector2(662f, 382f);
            dbilpRT.sizeDelta = new Vector2(100f, 400f);
        }
#else
        if (spr.name == "tutorial_movement_main")
        {
	        rt.transform.localPosition = new Vector2(0f, 15f); // position of image within parent
            rt.sizeDelta = new Vector2(662f, 382f);
            dbilpRT.sizeDelta = new Vector2(100f, 400f);
        }
#endif
        else
        {
            //rt.anchoredPosition = new Vector2(50f, -40f); // position of image within parent
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(96f, 96f);
            dbilpRT.sizeDelta = new Vector2(100f, 100f);
        }

    }

    public static void HideTextInputField()
    {
        if (genericTextInput == null)
        {
            return;
        }
        genericTextInput.SetActive(false);
        if (genericTextInputField == null)
        {
            return;
        }
        textInputFieldIsActivated = false;

        //if (Debug.isDebugBuild) Debug.Log("HIDING text input field in regular dialog box, contents are: " + genericTextInputField.text + ", any convo? " + (currentConversation==null));

#if !UNITY_SWITCH
        genericTextInputField.DeactivateInputField();
#endif
    }

    public static void DeactivateTextInputField()
    {
        //if (Debug.isDebugBuild) Debug.Log("Deactivated text input field");
        textInputFieldIsActivated = false;
#if !UNITY_SWITCH
        genericTextInputField.DeactivateInputField();
#endif
    }

    public static bool textInputFieldIsActivated = false;
    public static void ShowTextInputField(string placeholderText)
    {
        //if (Debug.isDebugBuild) Debug.Log("Activating text input field in regular dialog box, convo is " + currentConversation.refName + ", placeholder is " + placeholderText);

        genericTextInput.SetActive(true);
        genericTextInput.transform.localScale = Vector3.one; // make sure this didn't get squashed by the dialog box somehow

        genericTextInputPlaceholder.text = placeholderText;
        genericTextInputField.MoveToEndOfLine(false, false);
        genericTextInputField.OnSelect(null);
        genericTextInputField.ActivateInputField();
        textInputFieldIsActivated = true;
    }

    public static void EnableDialogSlider(string txt, int minValue, int maxValue, bool floatVal, float fSetVal = -1.0f)
    {
        singletonUIMS.dialogValueSliderParent.SetActive(true);

        if (floatVal)
        {
            singletonUIMS.dialogValueSlider.wholeNumbers = false;
        }
        else
        {
            singletonUIMS.dialogValueSlider.wholeNumbers = true;
        }
        singletonUIMS.dialogValueSlider.minValue = minValue;
        singletonUIMS.dialogValueSlider.maxValue = maxValue;

        if (fSetVal > 0f)
        {
            singletonUIMS.dialogValueSlider.value = fSetVal;
        }

        singletonUIMS.dialogValueSliderParent.transform.localScale = Vector3.one; // make sure this didn't get squashed by the dialog box somehow

        singletonUIMS.dialogValueSliderText.text = txt;
        defaultSliderText = txt;
        singletonUIMS.UpdateDialogSliderText();
    }

    public static int GetSliderValueInt()
    {
        return (int)singletonUIMS.dialogValueSlider.value;
    }

    public static void DisableDialogSlider()
    {
        if (singletonUIMS != null && singletonUIMS.dialogValueSliderParent != null)
        {
            singletonUIMS.dialogValueSliderParent.SetActive(false);
        }
    }

    public void UpdateDialogSliderText()
    {
        string extraChar = "";
        if (GameMasterScript.gmsSingleton.ReadTempStringData("dialogslider") == "gold")
        {
            extraChar = StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD);
        }
        else if (GameMasterScript.gmsSingleton.ReadTempStringData("dialogslider") == "jp")
        {
            extraChar = StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP);
        }
        singletonUIMS.dialogValueSliderText.text = defaultSliderText + "\n<color=yellow>" + singletonUIMS.dialogValueSlider.value + extraChar + "</color>";
    }

    public static void ToggleConfirmationDialog(string nConfirmText, bool newConvo, string convoName)
    {
        ClearAllDialogOptions();
        TextBranch confirmTB = new TextBranch();
        string confirmText = String.Copy(nConfirmText);
        confirmText = CustomAlgorithms.ParseButtonAssignments(confirmText);
        confirmTB.text = confirmText;
        ButtonCombo YES = new ButtonCombo();
        YES.buttonText = StringManager.GetString("dialog_confirm_hit_friendly_intro_btn_1");
        YES.dbr = DialogButtonResponse.YES;
        YES.actionRef = selectedButton.actionRef;
        confirmTB.responses.Add(YES);
        ButtonCombo NO = new ButtonCombo();
        NO.buttonText = StringManager.GetString("dialog_healer_town_intro_adv_needhelp2_btn_0");
        NO.dbr = DialogButtonResponse.NO;
        NO.actionRef = selectedButton.actionRef;
        confirmTB.responses.Add(NO);
        Vector2 size = new Vector2(800f, 10f);
        ToggleDialogBox(DialogType.CONFIRM, true, true, size, Vector2.zero);

        if (newConvo)
        {
            Conversation c = new Conversation();
            c.allBranches.Add(confirmTB);
            c.refName = convoName;
            currentConversation = c;
        }

        SwitchConversationBranch(confirmTB);
        UpdateDialogBox();
    }

    private static void EnableDialogOption(GameObject go, string nText, DialogButtonScript dbs, ButtonCombo bc, bool forceOverwriteButtonText = false)
    {
        //Debug.Log("Enabling dialog option with text " + nText);

        go.GetComponent<Button>().interactable = true;
        go.GetComponent<LayoutElement>().minHeight = 40;

        string text = String.Copy(nText);
        text = CustomAlgorithms.ParseButtonAssignments(text);
        text = CustomAlgorithms.ParseLiveMergeTags(text);

        if (bc != null)
        {
            bc.OnEnabledInDialogBox(forceOverwriteButtonText, text, dbs, go);
        }
        else
        {
            dbs.bodyText.text = text;
            if (dbs.headerText != null)
            {
                dbs.headerText.gameObject.SetActive(false);
            }
        }



    }

    public static void CreateBigDialogOption(string name, DialogButtonResponse dbr)
    {
        //GameObject go = (GameObject)Instantiate(Resources.Load("Art/ui/BigDialogButton"));
        GameObject go = GameMasterScript.TDInstantiate("BigDialogButton");
        go.transform.SetParent(myDialogBoxComponent.gameObject.transform);
        go.transform.localScale = new Vector3(1f, 1f, 1f);
        DialogButtonScript dbs = go.GetComponent<DialogButtonScript>();
        dbs.myID = dialogObjects.Count; // If there are 0 objects, ID is 0, etc...
        dbs.myResponse = dbr;

        /*
#if UNITY_SWITCH
        var txtButton = go.GetComponentInChildren<TextMeshProUGUI>();
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
                txtButton.fontSize = 36;
                break;
            default:
                txtButton.fontSize = 60;
                break;
        }
#endif */

        dialogObjects.Add(go);
        EnableDialogOption(go, name, dbs, null);
        UpdateDialogCursorPos();
    }

    static void CreateDialogOptionInternal(string name, DialogButtonResponse dbr, int optionalValue = -999, Sprite optionalSprite = null)
    {
        GameObject go = GameMasterScript.TDInstantiate("DialogButtonWithImage");
        go.transform.SetParent(myDialogBoxComponent.gameObject.transform);
        go.transform.localScale = new Vector3(1f, 1f, 1f);

        DialogButtonScript dbs = go.GetComponent<DialogButtonScript>();
        dbs.myID = dialogObjects.Count; // If there are 0 objects, ID is 0, etc...
        dbs.myResponse = dbr;
        if (optionalValue != -999)
        {
            dbs.myResponse = (DialogButtonResponse)optionalValue;
        }
        if (optionalSprite != null)
        {
            dbs.iconSprite.sprite = optionalSprite;
        }
        else
        {
            dbs.iconSprite.gameObject.SetActive(false);
        }
        dialogObjects.Add(go);
        EnableDialogOption(go, name, dbs, null);
        UpdateDialogCursorPos();
        //Debug.Log("Created dialog option " + name + " " + dbr);
    }

    public static void CreateDialogOption(string name, DialogButtonResponse dbr, int optionalValue = -999, Sprite optionalSprite = null)
    {
        CreateDialogOptionInternal(name, dbr, optionalValue, optionalSprite);
    }

    public static void CreateDialogOptionByInt(string name, int number)
    {
        CreateDialogOptionInternal(name, DialogButtonResponse.CONTINUE, number);
    }

    public static void SetDialogPos(float posX, float posY)
    {
        //GameObject dBox = GameObject.Find("DialogBox");
        if (GameMasterScript.actualGameStarted)
        {
            posY += 90f;
        }
        Vector3 pos = new Vector3(posX, posY, myDialogBoxComponent.transform.position.z);
        myDialogBoxComponent.transform.localPosition = pos;
    }

    public static void SetDialogSize(float width, float height)
    {
        if (!myDialogBoxComponent.gameObject.activeSelf)
        {
            return;
        }
        //GameObject dBox = GameObject.Find("DialogBoxText");

        if (height == 0)
        {
            height = 200f;
        }

        myDialogBoxComponent.SetLayoutElementMinDimensions(width, height);
    }

    public static void OverrideDialogWidth(float width, float height = 0f)
    {
        //if (Debug.isDebugBuild) Debug.Log("Overriding dialog width to be " + width + " height " + height);
        myDialogBoxComponent.SetHorizontalFitType(ContentSizeFitter.FitMode.Unconstrained);
        myDialogBoxComponent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        myDialogBoxComponent.GetTextGameObject().GetComponent<RectTransform>().sizeDelta = new Vector2(width - 80f, height);
    }

    public static void ResetDialogBoxComponents()
    {
        myDialogBoxComponent.SetHorizontalFitType(ContentSizeFitter.FitMode.PreferredSize);
    }

    public static void CloseDialogBox(bool forceWaitToFadeOptions = false, bool forceCloseEvenIfPlayerIsDead = false)
    {
        //Debug.Log("Close dbox.");
        if (GameMasterScript.playerDied && !forceCloseEvenIfPlayerIsDead) return;

        // new location for dbox components thing.
        ResetDialogBoxComponents(); // New 12/22, probably not necessary all the time?

        RectTransform rt = myDialogBoxComponent.GetComponent<RectTransform>();
        Vector2 rtSizeDelta = rt.sizeDelta;
        myDialogBoxComponent.MyCSF.enabled = false;
        myDialogBoxComponent.MyLayoutGroup.enabled = false;
        rt.sizeDelta = rtSizeDelta;

        // All this dialog box resizing is bad, CPU heavy, messy, etc but it's really baked into the system
        // And the problem is that when you close a dialog box, for various reasons, the text ends up outside the box briefly
        // Therefore I propose making the text opacity = 0 at the time of closing the box so you don't see that.
        myDialogBoxComponent.GetDialogText().color = transparentColor;

        ClearAllDialogOptions(forceWaitToFadeOptions, true);
        dialogBoxOpen = false;
        if (singletonUIMS.dialogValueSliderParent != null)
        {
            singletonUIMS.dialogValueSliderParent.SetActive(false);
        }

        singletonUIMS.DisableCursor();

            //Debug.Log("Closing dialog box from " + currentConversation.refName);

        // reset dbox components WAS here...
        CheckConversationQueue();

        //Do end of conversation things if necessary
        myDialogBoxComponent.OnEndConversation();

        //return to normal conversation box
        SetDialogBoxScriptType(EDialogBoxScriptTypes.@default);

        singletonUIMS.StartCoroutine(singletonUIMS.WaitThenTryStoredOverlayTextData(0.5f));

        if (singletonUIMS.currentFullScreenUI != null)
        {
            singletonUIMS.currentFullScreenUI.OnDialogClose();
        }

    }

    public IEnumerator WaitThenAllowDialogConfirmation(float time)
    {
        dialogInteractableDelayed = true;
        yield return new WaitForSeconds(time);
        dialogInteractableDelayed = false;
    }

    private static int GetMaxDialogOptions()
    {
        return dialogObjects.Count;
    }

    IEnumerator WaitToClearDialogOptions(float time)
    {
        waitingToClearDialogOptions = true;
        yield return new WaitForSeconds(time);
        if (waitingToClearDialogOptions)
        {
            ClearAllDialogOptions();
        }
    }

    public int MoveTypewriterText(string fullText, TextMeshProUGUI uiElement, int previousIndex, int textID)
    {
        if (!typingText) return 0;
        if (textID != idOfText) return 0;

        if (typingText && finishTypewriterTextImmediately)
        {
            uiElement.maxVisibleCharacters = 9999;
            finishTypewriterTextImmediately = false;
            typingText = false;
            //Debug.Log("Text finished typing.");
            return 99999;
        }

        // try this cool new alternate method
        int nextIndex = previousIndex + 1;
        uiElement.maxVisibleCharacters = nextIndex;

        return nextIndex;
    }

    IEnumerator NEW_BeginTypewriterRoutine(TextMeshProUGUI uiElement, int textID)
    {
        float typingDelay = 1f / 60f;

        while (myDialogBoxComponent.IsDelayed())
        {
            yield return null;
        }

        int charsToDisplayThisFrame = 1;

        int curCharsToDisplay = uiElement.textInfo.pageInfo[0].firstCharacterIndex;

        // This might be e.g. 600 characters
        // But what if we are split into pages? The pages are created by TMP automatically. We'll have to check this.
        int totalVisibleCharacters = uiElement.textInfo.characterCount;

        Debug.Log("Typewriter coroutine begins. " + curCharsToDisplay + " " + totalVisibleCharacters + " " + uiElement.text);

        typingText = true;

        while (curCharsToDisplay <= totalVisibleCharacters)
        {
            int lastCharacterIndex = uiElement.textInfo.pageInfo[0].lastCharacterIndex;

            if (idOfText != textID)
            {
#if UNITY_EDITOR
                Debug.Log("Moved to next page, stop this routine!");
#endif
                yield break;
            }
            if (finishTypewriterTextImmediately)
            {

                typingText = false;
                finishTypewriterTextImmediately = false;
                uiElement.maxVisibleCharacters = lastCharacterIndex + 1; // was totalVisibleIndex
                                                                         // last page reached.
#if UNITY_EDITOR
                Debug.Log("Request immediate stop");
#endif
                yield break;
            }

            curCharsToDisplay += charsToDisplayThisFrame;
            uiElement.maxVisibleCharacters = curCharsToDisplay;

            // Let's say this current page shows up to character 250
            // We can safely display UP TO 250
            if (curCharsToDisplay <= lastCharacterIndex)
            {
                // do nothing?
            }
            else
            {
                typingText = false;

                // last page reached
#if UNITY_EDITOR
                Debug.Log("Reached last page...?");
#endif
                yield break;
            }

            yield return new WaitForSeconds(typingDelay);
        }

        // show dialog responses if there are any, and we're on the last page
        // last page reached

        typingText = false;
    }

    public IEnumerator TypeTextRoutine(string fullText, TextMeshProUGUI uiElement, int index, int textID, int actualTextLength)
    {
        //if the Dialog isn't showing up, don't type
        while (myDialogBoxComponent.IsDelayed())
        {
            yield return null;
        }

        yield return new WaitForSeconds(typewriterTextSpeed);

        if (actualTextLength == 0) // must not have been checked yet
        {
            actualTextLength = fullText.Length;
        }

#if UNITY_EDITOR
        //Debug.Log(uiElement.text);
        //Debug.Log("Typing text, which is: " + fullText + " Index: " + index + " Text length: " + actualTextLength);
#endif

        if (typingText && textID == idOfText)
        {
            int nIndex = 0;

            nIndex = MoveTypewriterText(fullText, uiElement, index, textID);

            //Debug.Log("Index is now " + nIndex);
            if (nIndex < actualTextLength)
            {
                for (int i = 1; i < charactersPerTypewriterTick; i++)
                {
                    nIndex = MoveTypewriterText(fullText, uiElement, nIndex, textID);
                    if (nIndex >= actualTextLength)
                    {
                        //if (Debug.isDebugBuild) Debug.Log("Uh oh, with " + charactersPerTypewriterTick + " now " + nIndex + " exceeds length of " + actualTextLength);
                        break;
                    }
                }

            }

            if (nIndex < actualTextLength)
            {
                StartCoroutine(TypeTextRoutine(fullText, uiElement, nIndex, textID, actualTextLength));
            }
            else
            {
                finishTypewriterTextImmediately = false;
                typingText = false;
#if UNITY_EDITOR
                //Debug.Log("Text finished typing. " + fullText.Length + " " + actualTextLength);
#endif
                //MusicManagerScript.StopCue("Typing");
            }
        }
    }

    public string BeginTypewriterText(string nFullText, TextMeshProUGUI uiElement)
    {
        myDialogBoxComponent.gameObject.GetComponent<ContentSizeFitter>().enabled = true;
        uiElement.maxVisibleCharacters = 9999;

        if (string.IsNullOrEmpty(nFullText))
        {
            return "";
        }

        if (typewriterSB == null)
        {
            typewriterSB = new StringBuilder();
            typewriterSBopen = new StringBuilder();
            typewriterSBclose = new StringBuilder();
        }

        bool forceTypewriter = false;

        if (currentConversation != null && currentConversation.forceTypewriter)
        {
            forceTypewriter = true;
        }

        string fullText = String.Copy(nFullText);
        fullText = CustomAlgorithms.ParseButtonAssignments(fullText);
        fullText += "\n"; // Extra line breaks before buttons, for aesthetics.

        //Debug.Log(fullText);

        if (currentConversation != null && currentConversation.hasLiveMergeTags)
        {
            fullText = CustomAlgorithms.ParseLiveMergeTags(fullText);
        }

        if (((currentConversation == null || currentConversation.whichNPC == null) && !forceTypewriter) || PlayerOptions.textSpeed == (int)TextSpeed.INSTANT)
        {
            //Debug.Log("Null convo, or null NPC... so just display normally.");           
            uiElement.text = fullText;
            return fullText;
        }

        closeTags = "";
        hiddenStartSizeTag = "";
        currentDialogTextArray = fullText.ToCharArray();
        typingText = true;
        finishTypewriterTextImmediately = false;
        idOfText += 1;

        uiElement.text = fullText;
        
        uiElement.maxVisibleCharacters = 1;

        //LayoutRebuilder.ForceRebuildLayoutImmediate(myDialogBoxComponent.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();

        myDialogBoxComponent.MyLayoutGroup.SetLayoutVertical();

        StartCoroutine(TypeTextRoutine(fullText, uiElement, 0, idOfText, 0));
        //StartCoroutine(BeginTypewriterRoutine(uiElement, idOfText));
        return fullText;
    }
}
