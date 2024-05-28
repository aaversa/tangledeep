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

//Shep 9 Aug 2017: Groundwork for multi language translation
public enum EGameLanguage
{
    en_us = 0,
    de_germany,
    jp_japan,
    es_spain,
    zh_cn, // Simplified chinese
    COUNT
}

public enum AbbreviatedSymbols
{
    GOLD,
    JP,
    XP,
    PERCENT,
    COUNT
}

public partial class StringManager : MonoBehaviour
{
    public static Dictionary<EGameLanguage, CultureInfo> dictCultures;

    public static Dictionary<string, string> englishActionsToChinese;

    private class HardCodeToTagConversionHelper
    {
        public class ConversionInfo
        {
            public string strFieldName;
            public string strUnlocalizedText;
            public string strLocalizedTag;
        }

        public string prefix;

        public List<ConversionInfo> listConversions;        

        public HardCodeToTagConversionHelper()
        {
            listConversions = new List<ConversionInfo>();
        }


        public void AddConversionIfNeeded(string strFieldName, string strUnlocalizedText, string strTag, bool bStripTags = false)
        {
            //reject empty entries
            if (string.IsNullOrEmpty(strUnlocalizedText))
            {
                return;
            }

            //reject entries with _ because they've already been localized
            if (strUnlocalizedText.Contains(prefix + "_"))
            {
                return;
            }

            //clean out the XML jank
            strUnlocalizedText = DisenjankenString(strUnlocalizedText, bStripTags);

            /* if (strUnlocalizedText.Contains("^")) {
                Debug.Log(strUnlocalizedText);
            } */
            

            var cInfo = new ConversionInfo();

            cInfo.strFieldName = strFieldName;
            cInfo.strUnlocalizedText = strUnlocalizedText.Replace("&", ";&amp");

            //If we've already stored this text, grab the tag
            if (dictUniqueTextToTags.ContainsKey(cInfo.strUnlocalizedText))
            {
                cInfo.strLocalizedTag = dictUniqueTextToTags[cInfo.strUnlocalizedText];
            }
            //otherwise, remember it for later.
            else
            {
                cInfo.strLocalizedTag = prefix + "_" + strTag;
                dictUniqueTextToTags[cInfo.strUnlocalizedText] = cInfo.strLocalizedTag;
            }


            listConversions.Add(cInfo);
        }
    }


    public static EGameLanguage gameLanguage;
	static Dictionary<string, string> dictMiscStrings;
    public static string[] mergeTags;

    const int MAX_MERGE_TAGS = 10;

    public static Dictionary<EGameLanguage, Dictionary<string,string>> dictStringsByLanguage;

    private static Dictionary<Type, Dictionary<int, string>> dictEnumStrings;

    // Dictionary that is necessary to break down complex Kanji into sortable Kana
    public static Dictionary<string, string> dictKanjiToKana;

    const string JAPANESE_DEFAULT_CULTURE = "ja-JP";
    const int JAPANESE_ALTERNATE_CULTURE = 0x00010411;

    public static bool initializedCompletely;

    //a dictionary of unlocalized text to string tags
    //
    // "Help me!", msg_help
    //
    //We do this during conversion only to make sure we aren't localizing the same
    //string more than one time.
    private static Dictionary<string, string> dictUniqueTextToTags;

    static bool initializedCultureDictionaries;

	// Use this for initialization
    void Awake()
    {
        InitializeCultureDictionaries();       
    }

    public static void InitializeCultureDictionaries()
    {
        if (initializedCultureDictionaries) return;
        /* dictEnumStrings = new Dictionary<Type, Dictionary<int, string>>();
        AssignEnumsToDictionaries(); */
        dictCultures = new Dictionary<EGameLanguage, CultureInfo>();
        dictCultures.Add(EGameLanguage.en_us, new CultureInfo("en-US"));
        dictCultures.Add(EGameLanguage.de_germany, new CultureInfo("en-US"));
        dictCultures.Add(EGameLanguage.es_spain, new CultureInfo("en-US"));
        dictCultures.Add(EGameLanguage.zh_cn, new CultureInfo("zh-Hans"));

        try
        {
            dictCultures.Add(EGameLanguage.jp_japan, new CultureInfo(JAPANESE_ALTERNATE_CULTURE));
        }
        catch (Exception e)
        {
            //Debug.Log("Failed to create JP alt culture: " + e);
            dictCultures.Add(EGameLanguage.jp_japan, new CultureInfo("ja-JP"));
        }
        initializedCultureDictionaries = true;
    }
    
    void Start ()
    {
        englishActionsToChinese = new Dictionary<string, string>();        
    }

    public static void SetGameLanguage(EGameLanguage gl)
    {
        gameLanguage = gl;
        //if (Debug.isDebugBuild) Debug.Log("Game language set to " + gl);
    }

#if UNITY_SWITCH
    public static List<string> GetAllGameTips()
    {
        var retList = new List<string>();
        foreach (var kvp in dictStringsByLanguage[gameLanguage])
        {
            if (kvp.Key.ToLowerInvariant().Contains("game_tips_"))
            {
                retList.Add(kvp.Value);
            }
        }

        return retList;
    }
#endif

    public static string GetPortalBindingString()
    {
        string portal = UIManagerScript.cyanHexColor + GetString("misc_magicportal") + "</color>";

        string binding = "";

        StringManager.SetTag(0, BakedInputBindingDisplay.GetControlBinding(TDControlBindings.TOGGLE_RING_MENU));

        if (PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
        {            
            binding = " " + GetString("switch_portal_binding", true) + " ";
        }
        else
        {
            binding = UIManagerScript.greenHexColor + " (" + CustomAlgorithms.GetButtonAssignment("Use Town Portal") + ")</color>";
        }                       

        return portal + binding;        
    }

    static bool tipsLoadedEver = false;
    public static void LoadTips()
    {
        if (tipsLoadedEver) return;
        GameMasterScript titleScreenGMS = GameObject.Find("TitleScreenGMS").GetComponent<GameMasterScript>();

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;

        for (int i = 0; i < titleScreenGMS.stringXML.Length; i++)
        {
            if (titleScreenGMS.stringXML[i] == null) continue;
            using (XmlReader reader = XmlReader.Create(new StringReader(titleScreenGMS.stringXML[i].text), settings))
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    switch (reader.Name.ToLowerInvariant())
                    {
                        case "string":
                            reader.ReadStartElement();
                            string content = "";
                            string refName = "";
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                switch (reader.Name.ToLowerInvariant())
                                {
                                    case "refname":
                                        refName = reader.ReadElementContentAsString();
                                        break;
                                    case "gametip":
                                        reader.ReadElementContentAsString();
                                        TutorialManagerScript.AddGameTip(refName);
                                        break;
                                    case "content":
                                        content = reader.ReadElementContentAsString();
                                        content = CustomAlgorithms.ParseRichText(content, false);
                                        break;
                                    default:
                                        reader.Read();
                                        break;
                                }
                            }
                            reader.ReadEndElement();
                            if (String.IsNullOrEmpty(refName) || String.IsNullOrEmpty(content))
                            {
                                Debug.Log("Ref: " + refName + " name or content is null or empty");
                            }
                            else
                            {
                                try { dictMiscStrings.Add(refName, content); }
                                catch (Exception e)
                                {
                                    Debug.Log("WARNING: Error adding to master string dict " + e);
                                    Debug.Log(refName + " " + content);
                                }
                            }

                            break;
                        case "document":
                            reader.ReadStartElement();
                            break;
                        default:
                            reader.Read();
                            break;
                    }
                }
                reader.ReadEndElement(); // End doc
            }
        }

        tipsLoadedEver = true;
    }

    


}
