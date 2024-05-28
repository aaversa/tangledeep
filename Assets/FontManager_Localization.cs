using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public partial class FontManager
{

    public static void LocalizeMe(TMP_Text tmp, TDFonts theFont)
    {
        // For Switch's small screen, make adjustments to JP font size
#if UNITY_SWITCH
        tmp.font = GetFontAsset(theFont);
        if (StringManager.gameLanguage == EGameLanguage.jp_japan)
        {
            if (tmp.fontSize > 48 ||
                tmp.fontSizeMax > 48)
            {
                tmp.fontSizeMax = 48;
            }
        }
#else
        TMP_FontAsset tmpfa = GetFontAsset(theFont);
        if (tmp.font != tmpfa)
        {
            //Debug.Log("Swapped font of " + tmp.gameObject.name + " successfully to " + tmpfa.name);
            tmp.font = tmpfa;
        }

        // Add this to our master cache of all TMPros, storing default font size values BEFORE we do per-language adjustment
        TMProDataPack data = AddTMProToCache(tmp, theFont);

        if (StringManager.gameLanguage == EGameLanguage.jp_japan || StringManager.gameLanguage == EGameLanguage.zh_cn
            || StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            if (CheckForExceptionsToSizeAdjustment(tmp))
            {
                //Debug.Log("Cannot resize.");
                return;
            }

            AdjustFontSize(tmp);
        }

        // If we have something that can run a coroutine, run our Autosizing check for languages that need
        // pixel-perfect fonts. This will ensure that after TMPro's initial update (1 frame) we don't have
        // a non-integer autosized font size.

        if (GameMasterScript.gmsSingleton != null && GameMasterScript.gmsSingleton.gameObject.activeSelf)
        {
            GameMasterScript.gmsSingleton.StartCoroutine(WaitThenAdjustAutoSizing(tmp, data));
        }
        else if (TitleScreenScript.titleScreenSingleton != null && TitleScreenScript.titleScreenSingleton.gameObject.activeSelf)
        {
            TitleScreenScript.titleScreenSingleton.StartCoroutine(WaitThenAdjustAutoSizing(tmp, data));
        }
        else if (UIManagerScript.singletonUIMS != null && UIManagerScript.singletonUIMS.gameObject.activeSelf)
        {
            UIManagerScript.singletonUIMS.StartCoroutine(WaitThenAdjustAutoSizing(tmp, data));
        }

#endif

    }

    // The levelup dialogs' window size must be bigger in Japanese. Too much text otherwise.
    public static void DialogLocalizationTweaks()
    {
        if (StringManager.gameLanguage != EGameLanguage.jp_japan && StringManager.gameLanguage != EGameLanguage.zh_cn) return;

        // Levelup dialogue needs to be bigger!
        List<Conversation> conversations = new List<Conversation>();
        conversations.Add(GameMasterScript.FindConversation("levelupstats"));
        conversations.Add(GameMasterScript.FindConversation("levelupstats_redistribute"));
        foreach (Conversation c in conversations)
        {
            c.windowSize.x = 1400f;
            c.windowSize.y = 700f;
        }
    }

    /// <summary>
    /// Returns the character we use to display a range of values, like ~ or -, depending on language
    /// </summary>
    /// <returns></returns>
    public static string GetValueRangeCharacterByLanguage()
    {
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.de_germany:
                return "-";
            default:
                return "~";
        }
    }
    public static string GetLargeSizeTagForCurrentLanguage()
    {
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                return "<size=42>";
            default:
                return "<size=44>";
        }
    }
}