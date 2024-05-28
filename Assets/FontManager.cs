using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public enum TDFonts { WHITE, WHITE_NO_OUTLINE, BLACK, COUNT }

public partial class FontManager : MonoBehaviour
{

    public class TMProDataPack
    {
        public float fontSize;
        public float fontSizeMax;
        public bool autoSizingEnabled;
        public float overrideTargetSize;
        //public bool isButton250Width; // For the many UI buttons that are Generic Button 250
        public TDFonts fontUsed;
    }

    static Dictionary<EGameLanguage, Dictionary<TDFonts, TMP_FontAsset>> dictFonts;
    static Dictionary<EGameLanguage, Dictionary<TDFonts, TMP_FontAsset>> dict720pFonts;
    static Dictionary<TMP_Text, TMProDataPack> cachedLocalizedTMPros;

    static float lastKnownScreenResolution;

    public static bool initializedCompletely;

    public static TMP_FontAsset GetFontAsset(TDFonts theFont)
    {
        if (!dictFonts[StringManager.gameLanguage].ContainsKey(theFont))
        {
            Debug.Log("WARNING: Could not find " + theFont.ToString() + " in font resource dict for language " + StringManager.gameLanguage);
            return null;
        }

        var returnvar = dictFonts[StringManager.gameLanguage][theFont];

        //if (returnvar == null) Debug.Log("Font is null?");
        //else if (returnvar.material == null) Debug.Log("Material is null?");

        return returnvar;
    }
   
    // Store data about this TextMeshProUGUI's default settings, since we might be changing it
    // And we need to know the original values in case we need to revert later
	// Relevant to PC only?
    public static TMProDataPack AddTMProToCache(TMP_Text tmpFa, TDFonts tdFont)
    {
        TMProDataPack data;
        if (!cachedLocalizedTMPros.TryGetValue(tmpFa, out data))
        {
            data = new TMProDataPack();
            data.autoSizingEnabled = tmpFa.enableAutoSizing;
            data.fontSize = tmpFa.fontSize;
            data.fontSizeMax = tmpFa.fontSizeMax;
            data.fontUsed = tdFont;

            cachedLocalizedTMPros.Add(tmpFa, data);

            data.overrideTargetSize = GetOverrideTargetFontSize(tmpFa);

            return data;
        }
        else
        {
            data.fontUsed = tdFont;
            return data;
        }        
    }

	// Relevant to PC only?
	public static void OnResolutionChanged()
    {
        //Debug.Log("Resolution changed from last known resolution of " + lastKnownScreenResolution + " to new res of " + Screen.height);

        // This only applies if we are JP and we changed resolutions to something different than last known.
        if (StringManager.gameLanguage != EGameLanguage.jp_japan)
        {
            lastKnownScreenResolution = Screen.height;
            return;
        }
        if (CustomAlgorithms.CompareFloats(lastKnownScreenResolution, Screen.height))
        {
            lastKnownScreenResolution = Screen.height;
            return;
        }

        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 1) return;

        // Iterate through all cached TMPro objects and adjust font / font asset to account for 
        // pixel perfect sizing.

        foreach (TMP_Text tmPro in cachedLocalizedTMPros.Keys)
        {
            // Retrieve our data pack for this particular TMPro object to check what TDFont was used            
            TMProDataPack data = cachedLocalizedTMPros[tmPro];

            // Then we can send it through the LocalizeMe chain which will:
            // 1. Select the right font asset (720p / 1080p)
            // 2. Adjust font size for Pixel Perfectness
            LocalizeMe(tmPro, data.fontUsed);
        }

        lastKnownScreenResolution = Screen.height;
    }
}


