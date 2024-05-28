using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LanguageSelector : MonoBehaviour {

    bool dropdownState;

    public LanguageSelectorButton[] languageButtons;
    public TextMeshProUGUI resultsText;
    public Image myImage;
    public CanvasGroup myCG;
    public Image followThisImageState;

    public TMP_FontAsset chineseFont;

    // Use this for initialization
    void Start() {

// These objects should only be enabled if we have mouse control, and region is NOT detected at launch

        if (!PlatformVariables.ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION)
        {
            gameObject.SetActive(false);
            return;
        }

        SetDropdownState(false);

        UpdateSprite();
    }

    public void OnClick()
    {
        if (!dropdownState)
        {
            UIManagerScript.PlayCursorSound("UITick");
        }
        else
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
        SetDropdownState(!dropdownState);
    }

    void UpdateSprite()
    {
        for (int i = 0; i < languageButtons.Length; i++)
        {
            if (languageButtons[i].lang == StringManager.gameLanguage)
            {
                if (StringManager.gameLanguage == EGameLanguage.jp_japan)
                {
                    if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 1)
                    {
                        if (!languageButtons[i].isVectorFont) continue;
                    }
                    else
                    {
                        if (languageButtons[i].isVectorFont) continue;
                    }
                }
                /* if (StringManager.gameLanguage == EGameLanguage.zh_cn)
                {
                    if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_CN) == 1)
                    {
                        if (!languageButtons[i].isVectorFont) continue;
                    }
                    else
                    {
                        if (languageButtons[i].isVectorFont) continue;
                    }
                } */
                myImage.sprite = languageButtons[i].GetSprite();
                return;
            }
        }
    }

    public void OnLanguageSelected()
    {
        SetDropdownState(false);
        UIManagerScript.PlayCursorSound("Ultra Learn");
        string txt = "Language changed! Please restart game.";

        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.de_germany: // lol
                try 
                {
                    FontManager.LocalizeMe(resultsText, TDFonts.WHITE);
                }
                catch(Exception e)
                {
                    if (Debug.isDebugBuild) Debug.Log("Couldn't localize text box for jp/germany because: " + e);
                }
                
                txt = StringManager.GetString("misc_switchlanguage_jp_japan");
                break;
            case EGameLanguage.zh_cn: // lol
                FontManager.LocalizeMe(resultsText, TDFonts.WHITE);
                txt = StringManager.GetString("misc_switchlanguage_jp_japan");
                resultsText.font = chineseFont;
                break;
            case EGameLanguage.en_us:
                FontManager.LocalizeMe(resultsText, TDFonts.WHITE);
                break;
        }
        resultsText.text = txt;

        // Don't forget to hide the build text.
        UIManagerScript.singletonUIMS.buildText.GetComponent<TextMeshProUGUI>().color = UIManagerScript.transparentColor;

        UpdateSprite();
    }

    void SetDropdownState(bool state)
    {
        for (int i = 0; i < languageButtons.Length; i++)
        {
            languageButtons[i].gameObject.SetActive(state);
        }
        dropdownState = state;
    }

    // Update is called once per frame
    void Update()
    {
        if (!PlatformVariables.ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION) return;

        myImage.enabled = followThisImageState.enabled;
        myImage.color = new Color(1f, 1f, 1f, followThisImageState.color.a);
    }
}
