using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public partial class FontManager
{

    // Update is called once per frame
    public static IEnumerator LoadAllFontsAsync()
    {

        if (dictFonts != null) yield break;

        cachedLocalizedTMPros = new Dictionary<TMP_Text, TMProDataPack>();

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES) // why was this disabled... help
        {
            //grab the asset bundle with fonts in it, which should aready be loaded
            var bundleFont = TDAssetBundleLoader.GetBundleIfExists("nowloading_assets");

            var abr = bundleFont.LoadAllAssetsAsync<TMP_FontAsset>();
            yield return new WaitWhile(() => !abr.isDone);

            //Place the loaded fonts into the dictionary.
            dictFonts = new Dictionary<EGameLanguage, Dictionary<TDFonts, TMP_FontAsset>>();

            for (EGameLanguage lang = 0; lang < EGameLanguage.COUNT; lang++)
            {
                dictFonts[lang] = new Dictionary<TDFonts, TMP_FontAsset>();
            }

            foreach (var obj in abr.allAssets)
            {
                var font = obj as TMP_FontAsset;


                switch (font.name.ToLowerInvariant())
                {
                    case "chrono trigger ui":
                        dictFonts[EGameLanguage.en_us].Add(TDFonts.BLACK, font);
                        break;
                    case "chrono trigger sdf":
                        dictFonts[EGameLanguage.en_us].Add(TDFonts.WHITE, font);
                        break;
                    case "ct white":
                        dictFonts[EGameLanguage.en_us].Add(TDFonts.WHITE_NO_OUTLINE, font);
                        break;
                    case "de kubasta black":
                        dictFonts[EGameLanguage.de_germany].Add(TDFonts.BLACK, font);
                        dictFonts[EGameLanguage.es_spain].Add(TDFonts.BLACK, font);
                        break;
                    case "de kubasta white":
                        dictFonts[EGameLanguage.de_germany].Add(TDFonts.WHITE, font);
                        dictFonts[EGameLanguage.de_germany].Add(TDFonts.WHITE_NO_OUTLINE, font);
                        dictFonts[EGameLanguage.es_spain].Add(TDFonts.WHITE, font);
                        dictFonts[EGameLanguage.es_spain].Add(TDFonts.WHITE_NO_OUTLINE, font);
                        break;
                    case "jp smartfont white":
                        dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE, font);
                        dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE_NO_OUTLINE, font);
                        break;
                    case "jp smartfont black":
                        dictFonts[EGameLanguage.jp_japan].Add(TDFonts.BLACK, font);
                        break;

                    case "china vector white":
                        dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE, font);
                        dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE_NO_OUTLINE, font);
                        break;

                    case "china vector black":

                        dictFonts[EGameLanguage.zh_cn].Add(TDFonts.BLACK, font);
                        break;

                    default:
                        //if (Debug.isDebugBuild) Debug.Log("Tryna load font " + font.name + " and I don't know wtf to do with it.");
                        break;
                }
            }
        }
        else 
        {
        
            dictFonts = new Dictionary<EGameLanguage, Dictionary<TDFonts, TMP_FontAsset>>();
            dict720pFonts = new Dictionary<EGameLanguage, Dictionary<TDFonts, TMP_FontAsset>>();

            // English
            dictFonts.Add(EGameLanguage.en_us, new Dictionary<TDFonts, TMP_FontAsset>());

            TMP_FontAsset fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/Chrono Trigger SDF");
            dictFonts[EGameLanguage.en_us].Add(TDFonts.WHITE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/CT White");
            dictFonts[EGameLanguage.en_us].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/Chrono Trigger UI");
            dictFonts[EGameLanguage.en_us].Add(TDFonts.BLACK, fontToLoad);

            // Japanese
            dictFonts.Add(EGameLanguage.jp_japan, new Dictionary<TDFonts, TMP_FontAsset>());

            // Uncomment for vector-based

            if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 1)
            {
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/JP Smartfont White");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE, fontToLoad);
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/JP Smartfont White");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/JP Smartfont Black");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.BLACK, fontToLoad);
            }
            else
            {
                // Uncomment for pixel style
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 12 White");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE, fontToLoad);

                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 12 White");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 12 Black");
                dictFonts[EGameLanguage.jp_japan].Add(TDFonts.BLACK, fontToLoad);

                // Special 720p font handling
                dict720pFonts.Add(EGameLanguage.jp_japan, new Dictionary<TDFonts, TMP_FontAsset>());
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 10 White");

                dict720pFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE, fontToLoad);
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 10 Black");

                dict720pFonts[EGameLanguage.jp_japan].Add(TDFonts.BLACK, fontToLoad);
                fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/PixelMPlus 10 White");
                dict720pFonts[EGameLanguage.jp_japan].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);
            }

            // Spanish
            dictFonts.Add(EGameLanguage.es_spain, new Dictionary<TDFonts, TMP_FontAsset>());
            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/DE Kubasta White");
            dictFonts[EGameLanguage.es_spain].Add(TDFonts.WHITE, fontToLoad);
            dictFonts[EGameLanguage.es_spain].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/DE Kubasta Black");
            dictFonts[EGameLanguage.es_spain].Add(TDFonts.BLACK, fontToLoad);

            // German
            dictFonts.Add(EGameLanguage.de_germany, new Dictionary<TDFonts, TMP_FontAsset>());

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/DE Kubasta White");
            dictFonts[EGameLanguage.de_germany].Add(TDFonts.WHITE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/DE Kubasta White");
            dictFonts[EGameLanguage.de_germany].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/DE Kubasta Black");
            dictFonts[EGameLanguage.de_germany].Add(TDFonts.BLACK, fontToLoad);

            // Chinese - Can we just use JP character sets? Uhh maybe?
            dictFonts.Add(EGameLanguage.zh_cn, new Dictionary<TDFonts, TMP_FontAsset>());

        /* if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_CN) == 0) // pixel
        {
            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Zpix White");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Zpix White");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Zpix Black");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.BLACK, fontToLoad);
        }
        else */ // Smooooth
        {
            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Vector White");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Vector White");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.WHITE_NO_OUTLINE, fontToLoad);

            fontToLoad = Resources.Load<TMP_FontAsset>("Fonts/China Vector Black");
            dictFonts[EGameLanguage.zh_cn].Add(TDFonts.BLACK, fontToLoad);
        }
    }

        lastKnownScreenResolution = Screen.height;

        initializedCompletely = true;

        TitleScreenScript.LocalizeButtonsAndInputFields();

        GameObject txt = GameObject.Find("NewLoadingText");
        if (txt != null)
        {
            TextMeshProUGUI newLoadingText = txt.GetComponent<TextMeshProUGUI>();
            if (newLoadingText != null)
            {
                FontManager.LocalizeMe(newLoadingText, TDFonts.WHITE);
                newLoadingText.text = StringManager.GetString("ui_loading");
            }
        }

        if (GameMasterScript.gmsSingleton != null && GameMasterScript.gmsSingleton.newLoadingText != null)
        {
            FontManager.LocalizeMe(GameMasterScript.gmsSingleton.newLoadingText, TDFonts.WHITE);
        }
    }
}