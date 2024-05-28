using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DialogBoxScript_Faces : DialogBoxScript
{
    public static bool dialog_ShouldDisplayFaces;

    public Image dialog_SpeakerImage;
    public Image dialog_AnswererImage;
    public Animatable dialog_SpeakerAnimatable;

    //If the More/Next icon is displayed, hide the answerer face
    protected override void OnIconEnabled()
    {
        base.OnIconEnabled();
        dialog_AnswererImage.enabled = false;
    }

    public override void DisableCloseNextIcons()
    {
        base.DisableCloseNextIcons();

        //because we are a face conversation window, let's make sure
        //our face is always on when More/Next isn't displayed
        dialog_AnswererImage.enabled = true;
    }


    //Show or hide the answerer face, usually toggled if we 
    //are displaying the (more) or (exit) icons.
    public void EnableAnswerer(bool bEnabled)
    {
        dialog_AnswererImage.enabled = bEnabled;
    }

    void Start()
    {
        DoOneTimeSizeAdjustmentByCulture();

        FontManager.LocalizeMe(txtDialogBoxMessage, TDFonts.WHITE);
        if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            if (dialogValueSliderText != null)
            {
                FontManager.LocalizeMe(dialogValueSliderText, TDFonts.WHITE);
            }

            if (genericTextInputPlaceholder != null)
            {
                FontManager.LocalizeMe(genericTextInputPlaceholder, TDFonts.WHITE);
            }

            if (genericTextInput != null)
            {
                FontManager.LocalizeMe(genericTextInputText, TDFonts.WHITE);
            }
        }
        transform.localScale = new Vector3(1f, 0f, 1f); // to remove pop-in when first enabled
    }

    //Set a given sprite to the top left face
    public void SetFaces(string strSpeakerSpriteRef, float[] optionalAnimTiming)
    {
        Sprite playerJobSprite = GameMasterScript.heroPCActor.GetPortrait();

        //If we passed in no name, just keep the face as it was last time.
        if (dialog_SpeakerImage != null && !string.IsNullOrEmpty(strSpeakerSpriteRef))
        {
            dialog_SpeakerImage.enabled = true;
            if (strSpeakerSpriteRef == "player")
            {
                dialog_SpeakerImage.sprite = playerJobSprite;
                dialog_SpeakerImage.rectTransform.localRotation = Quaternion.Euler(0, 180, 0);
                dialog_SpeakerAnimatable.StopAnimation();
            }
            else if (dialog_SpeakerImage.sprite == null ||
                     !(dialog_SpeakerImage.sprite.name.Contains(strSpeakerSpriteRef)))
            {

                Sprite[] portraitSprites = UIManagerScript.GetPortraitForDialog(strSpeakerSpriteRef);
                if (portraitSprites.Length == 0)
                {
                    Debug.LogError("Uh oh! Couldn't find " + strSpeakerSpriteRef);
                }
                else
                {
                    /* if (optionalAnimTiming != null)
                    {
                        Debug.Log("Array length is " + portraitSprites.Length + ", anim timing is " + optionalAnimTiming.Length);
                    }   */                  
                }
                dialog_SpeakerImage.sprite = portraitSprites[0];
                dialog_SpeakerImage.rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                if (optionalAnimTiming != null && optionalAnimTiming.Length > 0)
                {
                    dialog_SpeakerAnimatable.PlayDynamicAnimFromSprite(portraitSprites, optionalAnimTiming, true);  
                }
                else
                {
                    dialog_SpeakerAnimatable.PlayDynamicAnimFromSprite(portraitSprites, 0.1f, true);
                }

                Vector3 anchoredPos = dialog_SpeakerImage.rectTransform.anchoredPosition;
                anchoredPos.y = -110f; // fixed position that looks good, don't change this
                dialog_SpeakerImage.rectTransform.anchoredPosition = anchoredPos;
            }
        }

        if (dialog_AnswererImage != null)
        {
            dialog_AnswererImage.enabled = true;
            dialog_AnswererImage.sprite = playerJobSprite;
        }
    }

    
    public override void OverrideConversationSize(Vector2 vOverrideSize)
    {
        //never do this Well, Actually
        // For the JP version we do need to do this sometimes :|
        MyCSF.enabled = true; // once we've manually resized the box, the CSF can adjust borders
    }

    public override void SetLayoutElementMinDimensions(float width, float height)
    {
        //or this
    }

    public override void SetHorizontalFitType(ContentSizeFitter.FitMode fitModeSquad)
    {
        //never change the horizontal fit for conversation boxes.
        MyCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    public override string GetButtonRef()
    {
        return "DialogButtonWithImageRightJustified";
    }

    //The set up here is more complex with hands in many directions and extra spacing at the bottom
    public override void AdjustSettingsForResponsesAfterDialogBoxStart(bool mainMenu, ref float fAdjustedXCursorOffset)
    {
        var dialogUIObjects = UIManagerScript.dialogUIObjects;
        //add padding to the bottom
        MyLayoutGroup.padding.bottom = Math.Max(30, 150 - dialogUIObjects.Count * 30);

        //make sure the cursor appears on the right of the responses
        for (int t = 0; t < dialogUIObjects.Count; t++)
        {
            dialogUIObjects[t].bForceCursorToRightSideAndFaceLeft = true;
        }

        //adjust the offset as well
        fAdjustedXCursorOffset = dialogUIObjects[0].gameObj.GetComponent<RectTransform>().sizeDelta.x;
    }

    // This only runs on Start() and is specific to the Faces style dialogue
    void DoOneTimeSizeAdjustmentByCulture()
    {
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:

                // This just expands the box.
                RectTransform rt = gameObject.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(1200f, rt.sizeDelta.y);

                // The speaker's text has to be widened also.
                rt = dialogBoxText.gameObject.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(750f, rt.sizeDelta.y);
                break;
        }
    }

    // The "faces" style dialogue may need to be adjusted for some languages, like Japanese,
    // where the characters take up lotsa space. We'll make things more roomy.
    public override void DoSizeAdjustmentsByCulture()
    {
        foreach(UIManagerScript.UIObject button in UIManagerScript.dialogUIObjects)
        {
            switch(StringManager.gameLanguage)
            {
                case EGameLanguage.jp_japan:
                case EGameLanguage.zh_cn:
                    RectTransform rt = button.gameObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(400f, rt.sizeDelta.y);
                    // The body text probably also needs to be size 400f.
                    RectTransform textRT = button.gameObj.GetComponent<DialogButtonScript>().bodyText.GetComponent<RectTransform>();
                    textRT.sizeDelta = new Vector2(400f, rt.sizeDelta.y);
                    LayoutElement layoutElem = button.gameObj.GetComponent<LayoutElement>();
                    layoutElem.preferredWidth = 740f;
                    layoutElem.preferredHeight = 60f;
                    break;
            }
        }
    }
}
