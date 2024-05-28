using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TitleScreenPromo : MonoBehaviour
{
    public TextMeshProUGUI promoTMPro;

    static Dictionary<EGameLanguage, string> eShopURLByLanguage = new Dictionary<EGameLanguage, string>()
    {
        { EGameLanguage.en_us, "https://nintendo.com/games/detail/tangledeep-switch" },
        { EGameLanguage.jp_japan, "https://ec.nintendo.com/JP/ja/titles/70010000003095" },
        { EGameLanguage.zh_cn, "https://ec.nintendo.com/JP/ja/titles/70010000003095" },
        { EGameLanguage.de_germany, "https://www.nintendo.co.uk/Games/Nintendo-Switch-download-software/Tangledeep-1505728.html"},
        { EGameLanguage.es_spain, "https://www.nintendo.co.uk/Games/Nintendo-Switch-download-software/Tangledeep-1505728.html"}
    };

    static string dlcPurchaseURL_steam = "https://store.steampowered.com/app/953080/Tangledeep__Legend_of_Shara/";
    static string dlcPurchaseURL_gog = "https://www.gog.com/game/tangledeep_legend_of_shara";

    static string dlc2PurchaseURL_steam = "https://store.steampowered.com/app/1156710/Tangledeep__Dawn_of_Dragons/";
    static string dlc2PurchaseURL_gog = "https://www.gog.com/game/tangledeep_dawn_of_dragons";

    private void Start()
    {
        if (LogoSceneScript.globalIsSolsticeBuild || LogoSceneScript.globalSolsticeDebug)
        {
            promoTMPro.text = "";
            Image childImage = promoTMPro.gameObject.GetComponentInChildren<Image>();
            childImage.enabled = false;
            enabled = false;
            return;
        }

        StartCoroutine(WaitThenTrySettingText());
    }

    void InitializeMe()
    {
        FontManager.LocalizeMe(promoTMPro, TDFonts.WHITE);
        promoTMPro.color = new Color(1f, 251f / 255f, 16f / 255f, 204f / 255f); // special special color
        promoTMPro.text = StringManager.GetString("switch_promo_text");
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && StringManager.gameLanguage == EGameLanguage.en_us)
        {
            promoTMPro.text = "LEGEND OF SHARA Expansion DLC Now Available!";
        }
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            if (StringManager.gameLanguage == EGameLanguage.es_spain)
            {
                promoTMPro.text = StringManager.GetString("switch_promo_text"); 
            }
            else
            {
                promoTMPro.text = StringManager.GetString("expansion2_promo");
            }
            
        }

    }

    IEnumerator WaitThenTrySettingText()
    {
        yield return null;
        float timeStart = Time.realtimeSinceStartup;
        while (!StringManager.initializedCompletely || !FontManager.initializedCompletely)
        {
            yield return null;
            if (Time.realtimeSinceStartup - timeStart >= (GameMasterScript.MIN_FPS_DURING_LOAD * 60f))
            {
                if (Debug.isDebugBuild) Debug.Log("Promo text is taking too long");
                yield break; // this is taking too long!!
            }
        }
        InitializeMe();
    }

    public void OnClick()
    {
        string urlToOpen = eShopURLByLanguage[StringManager.gameLanguage];
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            bool urlFound = false;
#if UNITY_STANDALONE_LINUX
            urlToOpen = dlcPurchaseURL_steam;
            urlFound = true;
#endif
            if (!urlFound)
            {
#if !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
                if (GogGalaxyManager.IsInitialized())
                {
                    urlToOpen = dlcPurchaseURL_gog;
                }
                else
#endif
                {
                    urlToOpen = dlcPurchaseURL_steam;
                }
            }                       
        }
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            bool urlFound = false;
#if UNITY_STANDALONE_LINUX
            urlToOpen = dlcPurchaseURL_steam;
            urlFound = true;
#endif
            if (!urlFound)
            {
#if !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
                if (GogGalaxyManager.IsInitialized())
                {
                    urlToOpen = dlc2PurchaseURL_gog;
                }
                else
#endif
                {
                    urlToOpen = dlc2PurchaseURL_steam;
                }
            }
        }
        Application.OpenURL(urlToOpen);
    }

}
