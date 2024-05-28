using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LanguageSelectorButton : MonoBehaviour {

    public EGameLanguage lang;
    public LanguageSelector parent;
    public Image myImage;
    public bool isVectorFont;

    public Sprite GetSprite()
    {
        return myImage.sprite;
    }

    public void OnClick()
    {
        TDPlayerPrefs.SetString(GlobalProgressKeys.LANGUAGE, lang.ToString());
        StringManager.SetGameLanguage(lang);
        parent.OnLanguageSelected();
        PlayerOptions.WriteOptionsToFile();
        TDPlayerPrefs.Save();
    }

    public void OnClick_JP(int value)
    {
        TDPlayerPrefs.SetInt(GlobalProgressKeys.VECTOR_JP, value);
        OnClick();
    }

    public void OnClick_CN(int value)
    {
        TDPlayerPrefs.SetInt(GlobalProgressKeys.VECTOR_CN, 1);
        OnClick();
    }
}
