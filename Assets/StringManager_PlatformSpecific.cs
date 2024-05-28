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
    /// <summary>
    /// Convert the string passed from NN SDK into our enum.
    /// </summary>
    /// <param name="strCode">A string with many trailing \0 because C++</param>
    /// <returns></returns>
    public static EGameLanguage GetLanguageFromNNLanguageCode(string strCode)
    {
        //pulled directly from C++ land, so there's 0s chillin at the end.
        strCode = strCode.TrimEnd('\0');



        if (strCode.Contains("es-"))
        {
            return EGameLanguage.es_spain;
        }
        if (strCode.Contains("jp-"))
        {
            return EGameLanguage.jp_japan;
        }
        if (strCode.Contains("zh-"))
        {
            return EGameLanguage.zh_cn;
        }
        if (strCode.Contains("de-"))
        {
            return EGameLanguage.de_germany;
        }

        switch (strCode.ToLowerInvariant())
        {
            case "ja":
                //Debug.Log("Lang: '" + strCode + "' == 'ja'");
                return EGameLanguage.jp_japan;
            case "en-us":
                //Debug.Log("Lang: '" + strCode + "' == 'en-US'");
                return EGameLanguage.en_us;
            case "de":
                return EGameLanguage.de_germany;
            case "es":
            case "es-ar":
            case "es-bo":
            case "es-cl":
            case "es-co":
            case "es-cr":
            case "es-do":
            case "es-ec":
            case "es-sv":
            case "es-gt":
            case "es-hn":
            case "es-mx":
            case "es-ni":
            case "es-pa":
            case "es-py":
            case "es-pe":
            case "es-pr":
            case "es-uy":
            case "es-ve":
                return EGameLanguage.es_spain;
            case "zh-Hans":
            case "zh-Hant":
                return EGameLanguage.zh_cn;
            default:
                Debug.LogWarning("Lang: '" + strCode + "' == none of the above, so default.");
                return EGameLanguage.en_us;
        }
    }

    public static EGameLanguage GetLanguageForPS4(string strCode)
    {
        switch (strCode)
        {
            case "ENGLISH_US":
            case "ENGLISH_GB":
                return EGameLanguage.en_us;
            case "JAPANESE":
                return EGameLanguage.jp_japan;
            case "GERMAN":
                return EGameLanguage.de_germany;
            case "CHINESE_S":
            case "CHINESE_T":
                return EGameLanguage.zh_cn;
            case "SPANISH":
            case "SPANISH_LA":
                return EGameLanguage.es_spain;
            default:
                Debug.LogWarning("Lang: '" + strCode + "' == none of the above, so default.");
                return EGameLanguage.en_us;
        }
    }

    public static EGameLanguage GetLanguageForXBOXONE(string strCode)
    {
        switch (strCode)
        {
            case "en-US":
            case "en-CA":
            case "en-AU":
            case "en-GB":
            case "en-NZ":
            case "en-IE":
            case "en-CZ":
            case "en-GR":
            case "en-HK":
            case "en-HU":
            case "en-IN":
            case "en-IL":
            case "en-SA":
            case "en-SG":
            case "en-SK":
            case "en-ZA":
            case "en-AE":
                return EGameLanguage.en_us;
            case "ja-JP":
                return EGameLanguage.jp_japan;
            case "de-DE":
            case "de-AT":
            case "de-CH":
                return EGameLanguage.de_germany;
            case "zh_CN":
            case "zh_HK":
            case "zh_SG":
            case "zh_TW":
                return EGameLanguage.zh_cn;
            case "es-ES":
            case "es-MX":
            case "es-CL":
            case "es-CO":
                return EGameLanguage.es_spain;
            default:
                Debug.LogWarning("Lang: '" + strCode + "' == none of the above, so default.");
                return EGameLanguage.en_us;
        }
    }
    public static EGameLanguage GetLanguageForAndroid(string strCode)
    {
        switch (strCode)
        {
            case "English":
                return EGameLanguage.en_us;
            case "Japanese":
                return EGameLanguage.jp_japan;
            case "German":
                return EGameLanguage.de_germany;
            case "ChineseSimplified":
            case "ChineseTraditional":
                return EGameLanguage.zh_cn;
            case "Spanish":
                return EGameLanguage.es_spain;
            default:
                Debug.LogWarning("Lang: '" + strCode + "' == none of the above, so default.");
                return EGameLanguage.en_us;
        }
    }
}