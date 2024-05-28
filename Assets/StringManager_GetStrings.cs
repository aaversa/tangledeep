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

    public static string GetStringForEnum<T>(T e) where T : IConvertible
    {
        if (dictEnumStrings == null)
        {
            AssignEnumsToDictionaries();
        }

        Type myType = typeof(T);

        if (!myType.IsEnum)
        {
            return "lol";
        }

        if (!dictEnumStrings.ContainsKey(myType))
        {
            if (Debug.isDebugBuild) Debug.Log("dictEnumStrings does not contain " + myType);
            return "????";
        }
        Dictionary<int, string> result = dictEnumStrings[myType];
        var convertedT = (Enum)Enum.ToObject(myType, e);
        return result[(int)(object)convertedT];
    }

    // Convenience method since we do this a lot...
    public static string GetExcitedString(string refName)
    {
        string baseString = GetString(refName);
        if (StringManager.gameLanguage != EGameLanguage.de_germany)
        {
            return baseString.ToUpperInvariant() + "!";
        }
        else
        {
            return baseString;
        }

    }

    public static string GetString(string refName, bool forceParseButtonAssignments = false, bool backupLookup = false)
    {
        EGameLanguage glToUse = StringManager.gameLanguage;
        if (backupLookup == true)
        {
            glToUse = EGameLanguage.en_us;
        }
        bool specialDebug = false;
        if (Debug.isDebugBuild && refName == "dialog_tutorial_wait_main_txt_universal")
        {
            specialDebug = true;
        }
        //get the localized string
        string retString = GetLocalizedString(refName, glToUse);

        if (specialDebug)
        {
            Debug.Log("Return string is now: " + retString);
        }
        //if we have it, cool, use it, if not check miscstrings which will soon be dead.
        if (retString != refName)
        {
            retString = CustomAlgorithms.ParseRichText(retString, false);

            // Not sure why this is not done on Switch.
//#if !UNITY_SWITCH
            if (forceParseButtonAssignments)
            {
                retString = CustomAlgorithms.ParseButtonAssignments(retString);
            }
//#endif
            retString = CustomAlgorithms.ParseLiveMergeTags(retString, true, refName);
            return retString;
        }

        //check misc strings, bad.
        if (dictMiscStrings.ContainsKey(refName))
        {
            string baseString = dictMiscStrings[refName];
            baseString = CustomAlgorithms.ParseLiveMergeTags(baseString, true, refName);
            return baseString;
        }



        if (!backupLookup && StringManager.gameLanguage != EGameLanguage.en_us)
        {
            retString = GetString(refName, false, true);
            return retString;
        }

#if UNITY_EDITOR
        if (!refName.Contains("dmg_words") && !refName.Contains("poker"))
        {

            Debug.Log("Misc string ref " + refName + " not found.");
        }
#endif

        return refName;

    }

    public static string GetLocalizedStringInCurrentLanguage(string strID)
    {
        return GetLocalizedString(strID, StringManager.gameLanguage);
    }

    public static string GetLocalizedStringOrFallbackToEnglish(string strID)
    {
        string text = GetLocalizedString(strID, gameLanguage);

        bool fallback = false;

        if (text == strID)
        {
            text = GetLocalizedString(strID, EGameLanguage.en_us);
            fallback = true;
        }

        //Debug.Log("Fallback? " + fallback + " Check string ID " + strID + ", content is " + text);

        return text;
    }
    public static string GetLocalizedString(string strID, EGameLanguage glToUse)
    {
        string strResult = "";

        if (!dictStringsByLanguage.ContainsKey(glToUse))
        {
            if (Debug.isDebugBuild) Debug.LogError("The gameLanguage " + glToUse + " isn't loaded yet when asking for stringID " + strID);
            return strID;
        }
        //stop game?
        if (!dictStringsByLanguage[glToUse].TryGetValue(strID, out strResult))
        {
            //todo: create some switch we can flip to totally check out all strings for localization status
            return strID;
        }

        if (TDCalendarEvents.IsAprilFoolsDay() && GameStartData.miscGameStartTags.Contains("malemode"))
        {
            strResult = TDCalendarEvents.ProcessAprilFoolsText(strResult);
        }

        return strResult;
    }

    static List<string> GetMatchesFor(string source, string begin, string end)
    {
        Regex regex = new Regex("(" + begin + ".*?" + end + ")");
        MatchCollection mc = regex.Matches(source);
        List<string> retList = new List<string>();

        foreach (Match m in mc)
        {
            //Groups 0 is the complete match,
            //Groups 1 ignores the start/end tags
            retList.Add(m.Groups[1].ToString());
        }

        return retList;
    }

    static string DisenjankenString(string strTarget, bool bStripTags = false)
    {
        //clean out the XML jank
        strTarget = strTarget.Replace("&#xA;", "\\n");
        strTarget = strTarget.Replace("\n", "\\n");

        //remove any < > tags if we are stripping them
        if (bStripTags)
        {
            var matchList = GetMatchesFor(strTarget, "<", ">");
            matchList.AddRange(GetMatchesFor(strTarget, "\\^", "\\^"));
            matchList.AddRange(GetMatchesFor(strTarget, "#", "#"));

            foreach (var s in matchList)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    strTarget = strTarget.Replace(s, "");
                }
            }
        }

        return strTarget;
    }
}
