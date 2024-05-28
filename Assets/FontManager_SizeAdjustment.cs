using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public partial class FontManager
{
    public static void AdjustFontSize(TMP_Text tmp, float targetSize = 0f, float absoluteMaxSize = 128f)
    {
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.zh_cn:
                tmp.characterSpacing = 0;

                break;
            case EGameLanguage.jp_japan:
                // Get rid of any extra character spacing. We don't need it in Japanese.
                tmp.characterSpacing = 0;
	
                
                if (PlatformVariables.ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION)
                {
                    if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 1)
                    {
                        if (targetSize > 48f)
                        {
                            if (tmp.enableAutoSizing)
                            {
                                tmp.fontSizeMax = 48f;
                            }
                            else
                            {
                                tmp.fontSize = 48f;
                            }
                        }
                        return;
                    }

                    // This contains our default settings for this TMPro object.
                    TMProDataPack data = cachedLocalizedTMPros[tmp];

                    // Pull our original size from stored data. The current size might have been modified already
                    float baseSize = data.fontSize;
                    float baseSizeMax = data.fontSizeMax;
                    bool handle720p = false;

                    if (CustomAlgorithms.CompareFloats(720f, Screen.height))
                    {
                        handle720p = true;
                    }

                    // Some text objects might have special size exceptions, check that here.
                    if (data.overrideTargetSize != 0f && (baseSize > data.overrideTargetSize || baseSizeMax > data.overrideTargetSize))
                    {
                        baseSize = data.overrideTargetSize;
                        baseSizeMax = data.overrideTargetSize;
                    }

                    if (!tmp.enableAutoSizing)
                    {
                        if (targetSize == 0f)
                        {
                            targetSize = baseSize;
                        }
                    }
                    else
                    {
                        if (targetSize == 0f)
                        {
                            targetSize = baseSizeMax;
                        }
                    }

                    // 1. Figure out the closest integer-scaled font size given the current resolution
                    // 2. Don't exceed absoluteMaxSize
                    // 3. Swap fonts as necessary
                    targetSize = GetPixelPerfectFontSize(tmp, targetSize, handle720p, data.fontUsed, absoluteMaxSize);

                    if (!tmp.enableAutoSizing)
                    {
                        tmp.fontSize = targetSize;
                    }
                    else
                    {
                        tmp.fontSizeMax = targetSize;
                        if (tmp.fontSizeMin >= tmp.fontSizeMax)
                        {
                            tmp.fontSizeMin = tmp.fontSizeMax - 6f;
                        }
                    }
                }
                break;
            case EGameLanguage.COUNT:
                // All fonts must be scaled to 12 / 24 / 36 pt
                float requiredSizeMult = 12f; // required by our font
                requiredSizeMult *= UIManagerScript.GetCanvasScale(); // scaler fucks with UI

                // For example, if user is playing at 720p, the canvas scale is approx 0.66 of normal
                // Meaning true 12 pt would be 12 * 0.66 = 7.92 or so.

                if (tmp.enableAutoSizing)
                {
                    if (targetSize == 0f)
                    {
                        targetSize = tmp.fontSizeMax;
                    }
                    // Take our current max font size, divide by our target, re-multiply to get close to target.
                    int multiplier = (int)(targetSize / requiredSizeMult);
                    tmp.fontSizeMax = requiredSizeMult * multiplier;
                }
                else
                {
                    if (targetSize == 0f)
                    {
                        targetSize = tmp.fontSize;
                    }
                    int multiplier = (int)(targetSize / requiredSizeMult);
                    tmp.fontSize = requiredSizeMult * multiplier;
                }

                break;
            default:
                if (tmp.enableAutoSizing)
                {
                    if (targetSize == 0f)
                    {
                        targetSize = tmp.fontSizeMax;
                    }
                    tmp.fontSizeMax = targetSize;
                }
                else
                {
                    if (targetSize == 0f)
                    {
                        targetSize = tmp.fontSize;
                    }
                    tmp.fontSize = targetSize;
                }
                break;
        }
    }

	// Relevant for PC only?
    public static float GetPixelPerfectFontSize(TMP_Text tmp, float currentSize, bool handle720p, TDFonts fontToUse, float absoluteMax)
    {
        if (handle720p)
        {
            if (currentSize < 18f || absoluteMax < 18f)
            {
                tmp.font = dict720pFonts[EGameLanguage.jp_japan][fontToUse];
                return 15f; // 1x scale of 10pt
            }
            else if ((currentSize >= 18f && currentSize < 24f) || absoluteMax < 30f)
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 18f; // 1x scale of 12pt
            }
            else if ((currentSize >= 24f && currentSize < 36f) || absoluteMax < 36f)
            {
                tmp.font = dict720pFonts[EGameLanguage.jp_japan][fontToUse];
                return 30f; // 2x scale of 10pt
            }
            else if ((currentSize >= 36f && currentSize <= 40f) || absoluteMax < 45f)
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 36f; // 2x scale of 12pt
            }
            else if ((currentSize > 40f && currentSize < 54f) || absoluteMax < 54f)
            {
                tmp.font = dict720pFonts[EGameLanguage.jp_japan][fontToUse];
                return 45f; // 3x scale of 10pt
            }
            else
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 54f; // 3x scale of 12pt
            }
        }
        else
        {
            if (currentSize <= 15f || absoluteMax < 20f)
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 12f;
            }
            if ((currentSize > 15f && currentSize < 24f) || absoluteMax < 24f)
            {
                tmp.font = dict720pFonts[EGameLanguage.jp_japan][fontToUse];
                return 20f;
            }
            else if ((currentSize >= 24f && currentSize < 30f) || absoluteMax < 30f)
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 24f;
            }
            else if ((currentSize >= 30f && currentSize <= 38f) || absoluteMax < 36f)
            {
                tmp.font = dict720pFonts[EGameLanguage.jp_japan][fontToUse];
                return 30f;
            }
            else if ((currentSize > 38f && currentSize <= 46f) || absoluteMax < 48f)
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 36f;
            }
            else
            {
                tmp.font = dictFonts[EGameLanguage.jp_japan][fontToUse];
                return 48f;
            }
        }
    }

    // Put all of our special cases and exceptions here
    // For example, dialog buttons and elements can be forced to a certain size, or text within Generic Button 250
    // This way we don't have to edit the scene and affect other languages.
    public static float GetOverrideTargetFontSize(TMP_Text tmpFa)
    {
        if (!(StringManager.gameLanguage == EGameLanguage.jp_japan && TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 0))
        {
            return 0f;
        }

        // Check if this is a Generic Button of width 250. We want a universal size for these.
        Transform tParent = tmpFa.gameObject.transform.parent;
        RectTransform rt = null;
        if (tParent != null)
        {
            Image tParentImage = tParent.gameObject.GetComponent<Image>();
            if (tParentImage != null && tParentImage.sprite != null)
            {
                if (tParentImage.sprite.name.Contains("Button 250"))
                {
                    rt = tParent.gameObject.GetComponent<RectTransform>();
                    if (rt.sizeDelta.x == 250f)
                    {
                        return 36f; // Because It Looks Good
                    }
                }
            }

            // Not a grey button, but a dialogue option or something in the dialog box? We can scale it down.
            if (tParent.name.Contains("ButtonThreeColumn") ||
                tParent.name.Contains("DialogBox") ||
                (tParent.name.Contains("DialogButton") && !tParent.name.Contains("Big")))
            {
                return 30f;
            }
        }

        // Also make sure we're not too big given our width, this looks bad with JP characters.
        rt = tmpFa.GetComponent<RectTransform>();
        if (rt.sizeDelta.x >= 350f && rt.sizeDelta.x <= 475f)
        {
            return 36f;
        }

        return 0f;
    }

    public static IEnumerator WaitThenAdjustAutoSizing(TMP_Text tmp, TMProDataPack data)
    {
        yield return null;
        AdjustAutoSizingIfNecessary(tmp, data);
    }

    // For JP, auto sizing just wrecks havoc on everything. If we are auto sizing, set the font size up
    // So that we don't have to! Maybe this is better :thinking:
    public static bool AdjustAutoSizingIfNecessary(TMP_Text tmp, TMProDataPack data)
    {
        // Not needed for non-JP
        if (StringManager.gameLanguage != EGameLanguage.jp_japan) return true;
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.VECTOR_JP) == 1) return true;
        if (tmp.enableAutoSizing && tmp.fontSize < tmp.fontSizeMax)
        {
            //Debug.Log("Obj " + tmp.gameObject.name + " auto sizing target " + tmp.fontSizeMax + " has actual size " + tmp.fontSize + " for text: " + tmp.text);
            AdjustFontSize(tmp, tmp.fontSize, tmp.fontSize);
            return true;
        }
        return false; // no adjustment needed!
    }

    public static bool CheckForExceptionsToSizeAdjustment(TMP_Text tmp)
    {
        if (tmp == null)
        {
            return false;
        }

        // If we don't want ANY kind of pixel-perfect adjustment to occur, put logic here
        // Return TRUE if this object is an exception and it should not be touched in any way.
        // This should ideally never be done for pixel-perfect fonts like JP PixelMPlus but 
        // Sometimes we need to make a sacrifice for readability

        if (tmp.CompareTag("no_txt_resize"))
        {
            return true;
        }

        return false;
    }
}