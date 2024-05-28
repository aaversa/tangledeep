using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.Serialization;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Globalization;
using Debug = UnityEngine.Debug;

public partial class StringManager
{
    public static CultureInfo GetCurrentCulture()
    {
        return dictCultures[gameLanguage];

        switch (gameLanguage)
        {
            case EGameLanguage.en_us:
            default:
                return new CultureInfo("en-US");
            case EGameLanguage.zh_cn:
                return new CultureInfo("zh-Hans");
            case EGameLanguage.jp_japan:
                return new CultureInfo(JAPANESE_ALTERNATE_CULTURE); // JP alternate sort.
        }
    }

    // In Japanese (maybe other languages?) we can exclude certain characters/words when
    // breaking out chunks of text. English equivalent would be "the" or "and"
    public static List<string> GetListOfCulturalDividers()
    {
        switch (gameLanguage)
        {
            case EGameLanguage.jp_japan:
                return new List<string>()
                {
                    "な",
                    "の"
                    //"に" questionable?? related to location only
                };
            default:
                return new List<string>();
        }
    }

    // This checks our master Tangledeep-specific kanji to kana dictionary to see if the search string
    // has a kana equivalent. It will check not just that exact string, but shortened versions of it
    // as needed. This is necessary for phonetic A-Z sort.
    public static string GetKanjiToKanaValue(string kanji)
    {
        bool firstSearch = true;
        string searchKanji = kanji;
        // Reduce the string until it's just one character, we might find a match!
        while (searchKanji.Length > 1 || firstSearch)
        {
            string result = GetKanjiToKanaValue_NonRecursive(searchKanji);
            if (result != "")
            {
                return result;
            }
            searchKanji = searchKanji.Substring(0, searchKanji.Length - 1);
            firstSearch = false;
        }
        // By this point, we found nothing at all.
        return "";
    }

    // Just a simple dictionary lookup
    static string GetKanjiToKanaValue_NonRecursive(string kanji)
    {
        string returnKana;
        if (dictKanjiToKana.TryGetValue(kanji, out returnKana))
        {
            return returnKana;
        }
        return "";
    }

    // This function will handle conversions of the stripped display name for sorting purposes
    // Currently used in japanese to convert kanji (not sortable) to kana (sortable)
    public static string BuildCultureSensitiveName(string baseName)
    {
        string cultureSensitiveName = "";
        switch (gameLanguage)
        {
            case EGameLanguage.jp_japan:
                // Need to store a special version of the item name with no kanji for easier sorts!                
                char[] cStr = baseName.ToCharArray();

                List<string> dividers = StringManager.GetListOfCulturalDividers();

                // search through the original string (strippedName) in chunks
                // i.e. 魔物トンカチ
                string searchString = "";
                for (int i = 0; i < cStr.Length; i++)
                {
                    bool endOfString = i == cStr.Length - 1;
                    string charToString = cStr[i].ToString();
                    if ((dividers.Contains(charToString) || endOfString) && searchString != "")
                    {
                        // Is this chunk of text in our master Kanji -> Kana dict?
                        string kanjiToKana = StringManager.GetKanjiToKanaValue(searchString);
                        if (kanjiToKana != "")
                        {
                            // Then replace the chunk in the name.
                            cultureSensitiveName += kanjiToKana;
                        }
                        else
                        {
                            // Not found, so just use the original text.
                            cultureSensitiveName += searchString;
                        }

                        // Either way, reset our search string for the next chunk
                        searchString = "";
                    }
                    else
                    {
                        // Keep building our search string, one character at a time
                        // Until we hit a divider.
                        searchString += charToString;
                    }
                }
                break;
            case EGameLanguage.en_us:
            default:
                cultureSensitiveName = baseName;
                break;
        }

        return cultureSensitiveName;
    }

    // Fairly self explanatory :D Japanese doesn't use spaces, and we want to keep that in mind for some strings!
    public static bool DoesCurrentLanguageUseSpaces()
    {
        switch (gameLanguage)
        {
            case EGameLanguage.en_us:
            default:
                return true;
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                return false;
        }
    }

    public static string RemoveGermanArticles(string startString)
    {
        if (StringManager.gameLanguage != EGameLanguage.de_germany) return startString;
        string copyString = startString;
        copyString = copyString.Replace(" das ", string.Empty);
        copyString = copyString.Replace(" die ", string.Empty);
        copyString = copyString.Replace(" der ", string.Empty);
        return copyString;
    }

    public static string GetLocalizedSymbol(AbbreviatedSymbols symbol)
    {
        string sRef = "";
        switch (symbol)
        {
            case AbbreviatedSymbols.GOLD:
                sRef = GetString("misc_moneysymbol");
                break;
            case AbbreviatedSymbols.XP:
                sRef = GetString("experience_abbreviation");
                break;
            case AbbreviatedSymbols.JP:
                sRef = GetString("jobpoints_abbreviation");
                break;
            case AbbreviatedSymbols.PERCENT:
                sRef = "%";
                break;
        }

        if (gameLanguage != EGameLanguage.de_germany && gameLanguage != EGameLanguage.es_spain)
        {
            return sRef;
        }
        else
        {
            return " " + sRef; // German always has space beforehand
        }
    }

    public static int GetLineSpacingForTMPSpriteBasedOnLanguage()
    {
        switch (gameLanguage)
        {
            case EGameLanguage.jp_japan:
                return 32;

            case EGameLanguage.en_us:
            case EGameLanguage.de_germany:
            case EGameLanguage.zh_cn:
            default:
                return 20;
        }
    }

    static IEnumerator LoadKanaDictionary(string allKana)
    {
        string[] lineSeparated = allKana.Split('\n');
        yield return null;
        float fTime = Time.realtimeSinceStartup;
        for (int i = 0; i < lineSeparated.Length; i++)
        {
            string[] divided = lineSeparated[i].Split('|');
            if (divided.Length < 2) continue;
            dictKanjiToKana.Add(divided[0], divided[1]);
            if (Time.realtimeSinceStartup - fTime > GameMasterScript.MIN_FPS_DURING_LOAD)
            {
                fTime = Time.realtimeSinceStartup;
                yield return null;
            }
        }
    }
}
