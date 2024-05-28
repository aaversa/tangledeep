using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using System.Security.Policy;
#endif

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Switch_UIOptions_OptionObject : MonoBehaviour
{
    public enum EOptionType
    {
        on_off = 0,
        value_list,
        percent_range,
        only_a_label,
        command_button,
        max
    };

    private EOptionType myOptionType;
    
    [Space(12)]
    [Header("Option Information")]

    [SerializeField]    private TextMeshProUGUI txtOptionName;
    [SerializeField]    private TextMeshProUGUI txtOptionValue;
    public Image imageLeftArrow;
    public Image imageRightArrow;

    /// <summary>
    /// This function takes an updated option value, changes the gameplay setting, 
    /// and if necessary returns an updated string ID for the help text at the bottom.
    /// </summary>
    private Func<int,object> funcExecuteOnChange;

    private List<string> listValues;

    //for bools, this is just 0 or 1
    //for value lists, it's the index in listValues
    //for percent range, it is a 0 to 100 value.
    private int iValueIdx;

    //what text goes at the bottom of the screen while this option is selected?
    private string strInfoLabelText;

    private Switch_UIOptions myOwningOptionsMenu;

    //Usually an id that indicates a position in a list of option objects
    private int iIndexInParent;
    public int IndexInParent
    {
        get { return iIndexInParent; }
        set { iIndexInParent = value; }
    }

    //True when the option is active on screen, or fading in to be such.
    private bool bActivelyDrawing = true;

    //Will play a tick/tock when changed, if this is false it will not do so, and hopefully
    //we are doing that for a reason, such as when we change the SFX or Footstep values and play other noises.
    private bool bPlayDefaultSoundOnChange;

    public bool IsLabel() {  return myOptionType == EOptionType.only_a_label; }
    public bool IsCommand() { return myOptionType == EOptionType.command_button; }

    public void Start()
    {
        FontManager.LocalizeMe(txtOptionName, TDFonts.WHITE);

        //Headers don't have this member
        if (txtOptionValue != null)
        {
            FontManager.LocalizeMe(txtOptionValue, TDFonts.WHITE);
        }
    }

    /// <summary>
    /// Sets up this object to just be a label that indicates a new option section, 
    /// such as "Graphics" or "Gameplay"
    /// </summary>
    /// <param name="stringID_LabelText"></param>
    public void InitializeAsLabelOnly(string stringID_LabelText)
    {
        myOptionType = EOptionType.only_a_label;
        txtOptionName.text = StringManager.GetLocalizedStringInCurrentLanguage(stringID_LabelText);
    }

    /// <summary>
    /// Sets up this object to be a command that executes when Confirm is pressed.
    /// Currently only used for Save and return to Title Screen
    /// </summary>
    /// <param name="stringID_LabelText"></param>
    /// <param name="stringID_HelpText"></param>
    /// <param name="callOnExecute"></param>
    /// <param name="funcGetTextForOptionColumn">This is used if we want to display information in the right column, such as World Seed info.</param>
    public void InitializeAsCommandObject(string stringID_LabelText, string stringID_HelpText, Func<int, object> callOnExecute, Func<string> funcGetTextForOptionColumn = null)
    {
        myOptionType = EOptionType.command_button;
        txtOptionName.text = StringManager.GetLocalizedStringInCurrentLanguage(stringID_LabelText);
        if (funcGetTextForOptionColumn != null)
        {
            txtOptionValue.text = funcGetTextForOptionColumn();
        }
        else //hide the default "on" here
        {
            txtOptionValue.text = "";
        }

        strInfoLabelText = StringManager.GetLocalizedStringInCurrentLanguage(stringID_HelpText);
        funcExecuteOnChange = callOnExecute;
    }

    /// <summary>
    /// Begins setting up this option information. A style call must still be invoked
    /// to set up further information.
    /// </summary>
    /// <param name="stringID_Name"></param>
    /// <param name="stringID_HelpText"></param>
    /// <param name="callOnUpdate"></param>
    public void InitializeAsOptionInformation(string stringID_Name, string stringID_HelpText, Func<int, object> callOnUpdate, Switch_UIOptions boss, bool playDefaultSound = true)
    {
        myOwningOptionsMenu = boss;
        txtOptionName.text = StringManager.GetLocalizedStringInCurrentLanguage(stringID_Name);
        strInfoLabelText = StringManager.GetLocalizedStringInCurrentLanguage(stringID_HelpText);

        //on PS4/XBOX in options show coresponding button
#if UNITY_PS4 || UNITY_XBOXONE
        strInfoLabelText = ShowIconForCorespondingButton(stringID_HelpText, strInfoLabelText);
#endif

        funcExecuteOnChange = callOnUpdate;
        bPlayDefaultSoundOnChange = playDefaultSound;
    }

    /// <summary>
    /// Makes this option a boolean on/off tracker
    /// </summary>
    /// <param name="bStartingValue"></param>
    public void SetStyleBoolean(bool bStartingValue)
    {
        myOptionType = EOptionType.on_off;
        iValueIdx = bStartingValue ? 1 : 0;
        UpdateOptionLabelToRepresentValue();
    }

    /// <summary>
    /// Makes this option a percent range from 0 to 100
    /// </summary>
    /// <param name="iPercent">0 to 100 value to start at</param>
    public void SetStylePercentRange(int iPercent)
    {
        myOptionType = EOptionType.percent_range;
        iValueIdx = Mathf.Clamp(iPercent, 0, 100);
        UpdateOptionLabelToRepresentValue();
    }

    /// <summary>
    /// Makes this option a list of values.
    /// </summary>
    /// <param name="iStartingIdx">Index of the default value</param>
    /// <param name="strValues">List of string IDs that represent the various values </param>
    public void SetStyleValueList(int iStartingIdx, params string[] stringIDs)
    {
        myOptionType = EOptionType.value_list;
        listValues = stringIDs.ToList();
        iValueIdx = iStartingIdx;
        UpdateOptionLabelToRepresentValue();
    }

    /// <summary>
    /// Make an image pulse in size and flash yellow. Typically used on our change arrows.
    /// </summary>
    /// <param name="arrow"></param>
    void PulseArrow(Image arrow)
    {
        //flash and bounce a bit
        Vector3 vNewScale = new Vector3(1.2f, 1.2f, 1.2f);
        arrow.rectTransform.localScale = vNewScale;
        LeanTween.scale(arrow.rectTransform, Vector3.one, 0.15f).setEaseInOutBack();

        arrow.color = Color.yellow;
        LeanTween.color(arrow.rectTransform, Color.white, 0.15f).setEaseInOutBack();
    }

    /// <summary>
    /// Called when a +/- button is changed via any input
    /// </summary>
    /// <param name="delta"></param>
    public void OnClickChangeValue(int delta)
    {
        //Don't bother if we are a command
        if (IsCommand())
        {
            return;
        }

        //adjust the value
        ChangeValue(delta);

        //pulse the arrow
        PulseArrow(delta < 0 ? imageLeftArrow : imageRightArrow);

        //don't change which value is selected in the main scene,
        //because that might scroll the window, which we do not want.
        if (bPlayDefaultSoundOnChange)
        {
            UIManagerScript.PlayCursorSound( delta > 0 ? "UITick" : "UITock");
        }
    }

    /// <summary>
    /// Called this command object is selected and Confirm is pressed.
    /// </summary>
    public void OnConfirm()
    {
        //Play the select noise
        UIManagerScript.PlayCursorSound("Select");

        //run the script
        funcExecuteOnChange(0);
    }


    /// <summary>
    /// Changes the value by some delta. Does not simply set a new value,
    /// Use SetValue for that. Changes in response to player input should use
    /// the OnClick call above.
    /// </summary>
    /// <param name="delta"></param>
    void ChangeValue(int delta)
    {
        //adjust our value by delta amount based on myOptionType
        switch (myOptionType)
        {
            //simple toggles ignore the delta, they toggle whether you press left or right
            case EOptionType.on_off:
                iValueIdx = (iValueIdx + 1) % 2;
                break;
            case EOptionType.value_list:
                iValueIdx += delta;
                if (iValueIdx < 0)
                {
                    iValueIdx += listValues.Count;
                }
                iValueIdx %= listValues.Count;
                break;
            case EOptionType.percent_range:
                iValueIdx = Mathf.Clamp(iValueIdx + 10 * delta, 0, 100);
                break;
        }

        //make sure we look the part
        UpdateOptionLabelToRepresentValue();

        if (funcExecuteOnChange != null)
        {
            //Call our function and pass the new value in
            string strChange = funcExecuteOnChange(iValueIdx) as string;

            //That value change may have changed our help text. Possibly.
            //This is NOT the call that changes the option text, that happened some lines above.
            if (!string.IsNullOrEmpty(strChange))
            {
                strInfoLabelText = StringManager.GetLocalizedStringInCurrentLanguage(strChange);

                //on PS4/XBOX in options show coresponding button
#if UNITY_PS4 || UNITY_XBOXONE
                strInfoLabelText = ShowIconForCorespondingButton(strChange, strInfoLabelText);
#endif

            }
        }
    }


    /// <summary>
    /// Make sure the text information represents the true value of the index.
    /// If you're calling this every Update() something is going wrong.
    /// </summary>
    public void UpdateOptionLabelToRepresentValue()
    {
        //adjust our value by delta amount based on myOptionType
        switch (myOptionType)
        {
            case EOptionType.on_off:
                txtOptionValue.text = StringManager.GetLocalizedStringInCurrentLanguage(iValueIdx == 1 ? "option_str_on" : "option_str_off");
                break;
            case EOptionType.value_list:
                txtOptionValue.text = StringManager.GetLocalizedStringInCurrentLanguage(listValues[iValueIdx]);
                break;
            case EOptionType.percent_range:
                txtOptionValue.text = iValueIdx.ToString();
                break;
        }
    }

    /// <summary>
    /// Returns the text that lives in the bottom of the options menu
    /// </summary>
    /// <returns></returns>
    public string GetLocalizedHelpText()
    {
        return strInfoLabelText;
    }

    /// <summary>
    /// Places the UIManager dialog cursor in the correct location
    /// </summary>
    /// <returns></returns>
    public void AttachCursorToMe()
    {
        UIManagerScript.AlignCursorPos(txtOptionName.gameObject, -5.0f, 0, false);
    }

    /// <summary>
    /// Cause the text to fade out. If called while fading out, nothing happens.
    /// </summary>
    /// <param name="fTime"></param>
    public void FadeOut(float fTime)
    {
        if (!bActivelyDrawing)
        {
            InstantlyMakeColorsTransparent();
            return;
        }
        bActivelyDrawing = false;
        fadeIndex++;
        int indexToUse = fadeIndex;
        StartCoroutine(FadeColors(Color.white, Color.clear, fTime, indexToUse));
    }

    void InstantlyMakeColorsTransparent()
    {
        txtOptionName.color = Color.clear;

        //behavior for the arrows and values changes based on the status of the button
        switch (myOptionType)
        {
            case EOptionType.only_a_label:
                //nothing fades here
                break;
            case EOptionType.command_button:
                //fade in the option value, which may have data, but not the arrows
                txtOptionValue.color = Color.clear;
                break;
            default:
                //everything else fades in all three.
                txtOptionValue.color = Color.clear;
                imageLeftArrow.color = Color.clear;
                imageRightArrow.color = Color.clear;
                break;
        }
    }

    /// <summary>
    /// Cause the text to fade in. If called while fading in, nothing happens.
    /// </summary>
    /// <param name="fTime"></param>
    public void FadeIn(float fTime)
    {
        if (bActivelyDrawing)
        {
            return;
        }
        bActivelyDrawing = true;
        fadeIndex++;
        int indexToUse = fadeIndex;
        StartCoroutine(FadeColors(Color.clear, Color.white, fTime, indexToUse));
    }

    int fadeIndex = 0;

    /// <summary>
    /// Smoothly transition from color start to color end over fFadeTime seconds.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="fFadeTime"></param>
    /// <returns></returns>
    IEnumerator FadeColors(Color start, Color end, float fFadeTime, int reqFadeIndex)
    {
        float fTime = 0f;
        while (fTime < fFadeTime)
        {
            if (fadeIndex != reqFadeIndex)
            {
                yield break;
            }
            fTime += Time.deltaTime;
            Color drawColor = Color.Lerp(start, end, fTime / fFadeTime);

            txtOptionName.color = drawColor;

            //behavior for the arrows and values changes based on the status of the button
            switch (myOptionType)
            {
                case EOptionType.only_a_label:
                    //nothing fades here
                    break;
                case EOptionType.command_button:
                    //fade in the option value, which may have data, but not the arrows
                    txtOptionValue.color = drawColor;
                    break;
                default:
                    //everything else fades in all three.
                    txtOptionValue.color = drawColor;
                    imageLeftArrow.color = drawColor;
                    imageRightArrow.color = drawColor;
                    break;
            }

            //chill out for one frame
            yield return null;
        }
    }

    //used on PS4/XBOX ONE
    /// <summary>
    /// Show icon for coresponding button in help text,
    /// Used on PS4/XBOX ONE
    /// </summary>
    /// <param name="stringID">String ID</param>
    /// <param name="labelText">String, in which we want to show coresponding icon</param>
    private string ShowIconForCorespondingButton(string stringID, string labelText)
    {
        //for "Minimap Type" in help text instead of "(-)" show coresponding button
        if (stringID == "switch_ui_options_info_minimaptype" && labelText.Contains("(-)"))
            return labelText.Replace("(-)", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.TOGGLE_MINIMAP));
        //for "Confirm analog Movement", when it's on, in help text instead of "A" show coresponding button
        if (stringID == "switch_ui_options_info_analog_step_move" && labelText.Contains("A"))
            return labelText.Replace("A", BakedInputBindingDisplay.GetControlBinding(TDControlBindings.CONFIRM));

        //return the same text without change
        return labelText;
    }

}
