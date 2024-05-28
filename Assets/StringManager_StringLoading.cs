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
    static bool loadInProgress = false;

    public static IEnumerator LoadAllStrings(EGameLanguage gl = EGameLanguage.COUNT)
    {
        if (initializedCompletely) yield break;
        if (loadInProgress) yield break;
        loadInProgress = true;

        if (gl == EGameLanguage.COUNT)
        {
            gl = TryGetGameLanguageFromPlayerPrefs();
        }

        SetGameLanguage(gl);
        //Read in our localized strings

        dictMiscStrings = new Dictionary<string, string>();
        mergeTags = new string[MAX_MERGE_TAGS];
        TutorialManagerScript.InitializeGameTipList();

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES) 
		{
	        yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath, "localization"));		
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath, "localization_dlc1"));

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
            {
                yield return TDAssetBundleLoader.LoadSpecificAssetBundle(Path.Combine(Application.streamingAssetsPath,
        "localization_dlc2"));
            }

            yield return LoadLocalizationDataFromBundle("localization", gameLanguage);
            yield return LoadLocalizationDataFromBundle("localization_dlc1", gameLanguage);

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
            {
                yield return LoadLocalizationDataFromBundle("localization_dlc2", gameLanguage);
            }

            if (gameLanguage == EGameLanguage.zh_cn || gameLanguage == EGameLanguage.es_spain)
            {
                // Backup loading for fallback
                yield return LoadLocalizationDataFromBundle("localization", EGameLanguage.en_us);
                yield return LoadLocalizationDataFromBundle("localization_dlc1", EGameLanguage.en_us);

                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
                {
                    yield return LoadLocalizationDataFromBundle("localization_dlc2", EGameLanguage.en_us);
                }
            }


            for (int i = 1; i < 17; i++)
            {
                TutorialManagerScript.AddGameTip("game_tips_" + i);
            }

            TutorialManagerScript.AddGameTip("game_tips_2b");
        }
		else 
		{
		    yield return LoadLocalizationDataNoBundles();		
		}       
		
		PostLoadingInitialization();
		
        InitializeCultureDictionaries();

        UpdateCasinoStringsDependingOnLanguageAndPlatform();

        UpdateChineseBindingLocalization();

        initializedCompletely = true;

        loadInProgress = false;
    }

    /// <summary>
    /// For consoles, don't need to run this. For PC, replace "Lounge" with "Casino" because "Casino" is cooler and we
    /// don't care about PC ratings.
    /// </summary>
    static void UpdateCasinoStringsDependingOnLanguageAndPlatform()
    {
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        return;
#endif

#if UNITY_EDITOR
    return;
#endif

        List<string> stringsToEdit = null;

        //Debug.Log(gameLanguage + " is current language.");

        switch (gameLanguage)
        {
            case EGameLanguage.en_us:
                stringsToEdit = new List<string>() { "item_item_casinochip_name", "log_casino_earnchips", "npc_npc_casino1_name",
                "dialog_gamingjelly_placebets_txt" };

                foreach(string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.en_us][str] = dictStringsByLanguage[EGameLanguage.en_us][str].Replace("Lounge", "Casino");
                    //dictStringsByLanguage[EGameLanguage.en_us][str] = content;
                }

                stringsToEdit = new List<string>() { "dialog_casino2_intro_txt", "dialog_casino2_quest_cleared_txt", "dialog_casino2_quest_cleared_2_txt",
                "dialog_casino2_casino_bandits_txt", "dialog_casino2_casino_bandits_help_txt", "dialog_casino2_casino_bandits_help2_txt",
                "dialog_casino2_casino_bandits_help3_txt", "dialog_casino2_intro_questcomplete_txt", "dialog_casino2_helpgames_txt",
                "dialog_casino2_explain_slots_txt", "dialog_casino2_explain_ceelo_txt", "dialog_casino2_explain_blackjack_txt",
                "dialog_casino2_info_rewards_txt", "dialog_casino1_intro_txt", "dialog_casino1_info_rewards_txt",
                "dialog_casino1_placebets_txt", "dialog_casino1_intro_questcomplete_txt"};

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.en_us][str] = dictStringsByLanguage[EGameLanguage.en_us][str].Replace("LOUNGE", "CASINO");
                    //dictStringsByLanguage[EGameLanguage.en_us][str] = content;
                }

                stringsToEdit = new List<string>() { "floor_110_cname", "floor_226_cname", "dialog_casino_flyer_main_txt" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.en_us][str] = dictStringsByLanguage[EGameLanguage.en_us][str].Replace("Lizard Lounge", "Casino");
                    //dictStringsByLanguage[EGameLanguage.en_us][str] = content;
                }
                break;

            case EGameLanguage.jp_japan:

                stringsToEdit = new List<string>() { "dialog_casino_flyer_main_txt"};
                
                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.jp_japan][str] = dictStringsByLanguage[EGameLanguage.jp_japan][str].Replace("リザードラウンジ", "タングルディープカジノ");
                    //dictStringsByLanguage[EGameLanguage.jp_japan][str] = content;
                }

                stringsToEdit = new List<string>() { "floor_110_cname", "floor_226_cname", "dialog_gamingjelly_intro_txt" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.jp_japan][str] = dictStringsByLanguage[EGameLanguage.jp_japan][str].Replace("リザードラウンジ", "カジノ");
                    //dictStringsByLanguage[EGameLanguage.jp_japan][str] = content;
                }

                stringsToEdit = new List<string>() { "item_item_casinochip_name", "log_casino_earnchips", "npc_npc_casino1_name",
                "dialog_gamingjelly_placebets_txt", "dialog_casino2_intro_txt", "dialog_casino2_quest_cleared_txt",
                "dialog_casino2_quest_cleared_2_txt", "dialog_casino2_casino_bandits_txt", "dialog_casino2_casino_bandits_help_txt",
                "dialog_casino2_casino_bandits_help2_txt", "dialog_casino2_casino_bandits_help3_txt", "dialog_casino2_intro_questcomplete_txt",
                "dialog_casino2_helpgames_txt", "dialog_casino2_explain_slots_txt", "dialog_casino2_explain_ceelo_txt", "dialog_casino2_explain_blackjack_txt",
                "dialog_casino2_info_rewards_txt", "dialog_casino1_intro_txt", "dialog_casino1_placebets_txt", "dialog_casino1_intro_questcomplete_txt",
                "dialog_casino1_info_rewards_txt", "dialog_katie_casino_intro_txt"
                };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.jp_japan][str] = dictStringsByLanguage[EGameLanguage.jp_japan][str].Replace("ラウンジ", "カジノ");
                    //dictStringsByLanguage[EGameLanguage.jp_japan][str] = content;
                }

                break;

            case EGameLanguage.de_germany:
                stringsToEdit = new List<string>() { "floor_110_cname", "floor_226_cname", "dialog_casino_flyer_main_txt" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.en_us][str] = dictStringsByLanguage[EGameLanguage.en_us][str].Replace("Glückssalon", "Casino");
                    //dictStringsByLanguage[EGameLanguage.en_us][str] = content;
                }

                stringsToEdit = new List<string>() { "log_casino_earnchips", "npc_npc_casino1_name" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.de_germany][str] = dictStringsByLanguage[EGameLanguage.de_germany][str].Replace("Salon", "Casino");
                    //dictStringsByLanguage[EGameLanguage.de_germany][str] = content;
                }

                stringsToEdit = new List<string>() { "dialog_gamingjelly_placebets_txt", "dialog_casino1_info_rewards_txt",
                    "dialog_casino1_placebets_txt", "dialog_casino2_info_rewards_txt", "item_item_casinochip_name" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.de_germany][str] = dictStringsByLanguage[EGameLanguage.de_germany][str].Replace("Spielmarken", "Casinomarken");
                    //dictStringsByLanguage[EGameLanguage.de_germany][str] = content;
                }


                stringsToEdit = new List<string>() { "dialog_casino1_info_rewards_txt", "dialog_casino2_info_rewards_txt" };

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.de_germany][str] = dictStringsByLanguage[EGameLanguage.de_germany][str].Replace("Spielmarke", "Casinomarke");
                    //dictStringsByLanguage[EGameLanguage.de_germany][str] = content;
                }

                stringsToEdit = new List<string>() { "dialog_casino2_intro_txt", "dialog_casino2_quest_cleared_txt",
                    "dialog_casino2_quest_cleared_2_txt", "dialog_casino2_casino_bandits_txt", "dialog_casino2_casino_bandits_help_txt",
                "dialog_casino2_casino_bandits_help2_txt", "dialog_casino2_casino_bandits_help3_txt", "dialog_casino2_intro_questcomplete_txt",
                "dialog_casino2_helpgames_txt", "dialog_casino2_explain_slots_txt", "dialog_casino2_explain_ceelo_txt",
                "dialog_casino2_explain_blackjack_txt", "dialog_casino2_info_rewards_txt", "dialog_casino1_placebets_txt",
                "dialog_casino1_intro_questcomplete_txt", "dialog_casino1_info_rewards_txt"};

                foreach (string str in stringsToEdit)
                {
                    //string content = GetString(str);
                    dictStringsByLanguage[EGameLanguage.de_germany][str] = dictStringsByLanguage[EGameLanguage.de_germany][str].Replace("SALONHAI", "CASINOHAI");
                    //dictStringsByLanguage[EGameLanguage.de_germany][str] = content;
                }

                break;
        }
    }

    static float timeLastPause = 0f;
    static int waitChecker = 0;

    static IEnumerator LoadLocalizationDataNoBundles()
    {
        //if (Debug.isDebugBuild) Debug.Log("Begin loading localization data no bundles.");
        while (!DLCManager.initialized)
        {
            yield return null;
        }
        List<TextAsset> assetsToLoad = new List<TextAsset>();
        TextAsset[] loadedText = Resources.LoadAll<TextAsset>("Localization");
        /* for (int i = 0; i < loadedText.Length; i++)
        {
            Debug.Log(loadedText[i].name + " index " + i + " for core localization");
        } */

        assetsToLoad.AddRange(loadedText);
       
        TextAsset[] dlc1LoadedText = Resources.LoadAll<TextAsset>("DLCResources/DLC1/Localization");

        /* for (int i = 0; i < dlc1LoadedText.Length; i++)
        {
            Debug.Log(dlc1LoadedText[i].name + " index " + i + " for dlc 1");
        } */

        assetsToLoad.AddRange(dlc1LoadedText);

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            TextAsset[] dlc2LoadedText = Resources.LoadAll<TextAsset>("DLCResources/DLC2/Localization");

            /* for (int i = 0; i < dlc2LoadedText.Length; i++)
            {
                Debug.Log(dlc2LoadedText[i].name + " index " + i + " for dlc 2");
            } */

            assetsToLoad.AddRange(dlc2LoadedText);
        }
        dictStringsByLanguage = new Dictionary<EGameLanguage, Dictionary<string, string>>();
        dictKanjiToKana = new Dictionary<string, string>();

        waitChecker = 0;

        foreach (TextAsset asset in assetsToLoad)
        {
            //TextAsset asset = loadedText[t];

            // Construct our homebrew dictionary that stores conversions of kanji -> kana
            // Based on a dictionary file we pre-baked in the DebugConsole.
            if (asset.name.Contains("TangledeepkanjiToKanaDict") && StringManager.gameLanguage == EGameLanguage.jp_japan)
            {
                string allKana = asset.text;
                string[] lineSeparated = allKana.Split('\n');
                for (int i = 0; i < lineSeparated.Length; i++)
                {
                    string[] divided = lineSeparated[i].Split('|');
                    if (divided.Length < 2) continue;
                    dictKanjiToKana.Add(divided[0], divided[1]);
                }
                continue;
            }

            //default value
            EGameLanguage loadedLanguage = EGameLanguage.en_us;

            //Get the name of the file without the .txt at the end
            string strFileName = asset.name.Split('.')[0];
            try
            {
                loadedLanguage = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), strFileName.ToLower());
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild && !strFileName.Contains("perchance") && !strFileName.Contains("kanji"))
                {
                    Debug.Log("LoadLocalizationData: Couldn't understand what language to use for file '" + strFileName + "'?!? Exception: " + e.Message);
                }
                continue;
            }



            if (gameLanguage == EGameLanguage.en_us && loadedLanguage != EGameLanguage.en_us)
            {
#if !UNITY_EDITOR
                continue;
#endif
            }
            else if (gameLanguage != EGameLanguage.en_us && loadedLanguage != gameLanguage && loadedLanguage != EGameLanguage.en_us)
            {
#if !UNITY_EDITOR
                continue;
#endif
            }

#if UNITY_EDITOR
            /* if (gameLanguage != EGameLanguage.en_us || loadedLanguage != EGameLanguage.en_us)
            {
                Debug.LogError("SKIPPING NON ENGLISH");
                continue;
            } */
#endif

            //Read the strings in pairs, like mighty animals unto an ark.
            //Split by TAB and pull off the carriage returns on the tail end of lines
            string[] strParsed = asset.text.Split(new string[] { "\t", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            //if this list isn't even, it's possible the wheels fell off somewhere
            bool debugBrokenList = false;
            if (strParsed.Length % 2 != 0)
            {
                Debug.Log("LoadLocalizationData: File '" + strFileName + "' has an uneven amount of strings -- " +
                          "some key has no value! That means there's a missing tab or return character somewhere. " +
                          "Length is: " + strParsed.Length);

                debugBrokenList = true;

            }

            Dictionary<string, string> newDictionary = new Dictionary<string, string>();
            bool extendingExistingDictionary = false;
            // Are we adding to an existing dictionary, e.g. loading DLC text for same language?
            if (dictStringsByLanguage.ContainsKey(loadedLanguage))
            {
                extendingExistingDictionary = true;
                newDictionary = dictStringsByLanguage[loadedLanguage];
            }

            string prevKey = "";
            string prevValue = "";


            bool isDebugFile = false;

            waitChecker = 0;

            for (int idxString = 0; idxString < strParsed.Length; idxString += 2)
            {
                if (debugBrokenList &&
                    idxString + 1 == strParsed.Length)
                {
                    Debug.LogError("Loc File " + strFileName + " has a bad entry. The last 50 entries were logged, " +
                                   "check them and see if you're missing a tab somewhere. The most suspect entry is " +
                                   "logged as a Warning so check that first. ");
                    yield break;

                }
                string strKey = strParsed[idxString];
                string strValue = strParsed[idxString + 1];

                //if (Debug.isDebugBuild && strKey.Contains("banditdragon")) isDebugFile = true;

                if (debugBrokenList)
                {
                    //guessing here that the broken line was added recently, 
                    //and is in the last 50 entries somewhere.
                    if (idxString > strParsed.Length - 100)
                    {
                        if (strKey.Contains("_") && strValue.Contains("_"))
                        {
                            Debug.LogWarning("It's probably this line: " + strKey + " , " + strValue);
                        }
                        else
                        {
                            Debug.Log("Key " + strKey + "  Value " + strValue);
                        }

                    }
                }

                //clean out the XML jank
                strValue = strValue.Replace("&#xA;", "\n");

                if (newDictionary.ContainsKey(strKey))
                {
#if UNITY_EDITOR
                    Debug.LogError(loadedLanguage + " " + idxString + " Oh no! " + strKey + " is being added to the dictionary twice! Index " + idxString + " content: " + strValue + " previous key/value: " + prevKey + " " + prevValue);
#else
                    Debug.Log(loadedLanguage + " Oh no! '" + strKey + "' is being added to the dictionary twice! Index " + idxString + " content: " + strValue + " previous key/value: " + prevKey + " " + prevValue);
#endif
                }
                else
                {
                    newDictionary.Add(strKey, strValue);
                    if (isDebugFile) Debug.Log(strKey + " : " + strValue);
                }
                prevKey = strKey;
                prevKey = strValue;

                waitChecker++;
                if (waitChecker >= 10 && Time.time - timeLastPause >= GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    waitChecker = 0;
                    timeLastPause = Time.time;
                }
            }

            //ready for action
            //Unless we're just adding on to an existing dictionary, e.g. loading DLC1/2 text
            if (!extendingExistingDictionary)
            {
                dictStringsByLanguage.Add(loadedLanguage, newDictionary);
            }

        }

        initializedCompletely = true;
        //bAllStringsLoaded = true;
    }

    static List<string> bundlesLoaded;

    public static IEnumerator LoadLocalizationDataFromBundle(string bundleName, EGameLanguage glToLoad)
    {
        if (bundlesLoaded == null) bundlesLoaded = new List<string>();
        if (bundlesLoaded.Contains(bundleName))
        {
            yield break;
        }
        bundlesLoaded.Add(bundleName);

        var bun = TDAssetBundleLoader.GetBundleIfExists(bundleName);
        TextAsset[] loadedText = bun.LoadAllAssets<TextAsset>();

        if (dictStringsByLanguage == null)
        {
            dictStringsByLanguage = new Dictionary<EGameLanguage, Dictionary<string, string>>();
        }
        if (dictKanjiToKana == null)
        {
            dictKanjiToKana = new Dictionary<string, string>();
        }

        for (int t = 0; t < loadedText.Length; t++)
        {
            TextAsset asset = loadedText[t];

            // Construct our homebrew dictionary that stores conversions of kanji -> kana
            // Based on a dictionary file we pre-baked in the DebugConsole.
            if (asset.name.Contains("TangledeepkanjiToKanaDict"))
            {
                if (glToLoad == EGameLanguage.jp_japan)
                {
                    TDAssetBundleLoader.RunThisCoroutineForMeOK(LoadKanaDictionary(asset.text));
                }

                continue;
            }

            //default value
            EGameLanguage loadedLanguage = EGameLanguage.en_us;

            //Get the name of the file without the .txt at the end
            string strFileName = asset.name.Split('.')[0];

            if (strFileName.Contains("perchance")) continue;

            loadedLanguage = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), strFileName.ToLower());

            if (loadedLanguage != glToLoad)
            {
                continue;
            }

            bool languageAlreadyExistsInDict = dictStringsByLanguage.ContainsKey(loadedLanguage);


            //Read the strings in pairs, like mighty animals unto an ark.
            //Split by TAB and pull off the carriage returns on the tail end of lines
            string[] strParsed = asset.text.Split(new string[] { "\t", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            //if this list isn't even, it's possible the wheels fell off somewhere
            if (strParsed.Length % 2 != 0)
            {
                Debug.Log("LoadLocalizationData: File '" + strFileName + "' has an uneven amount of strings -- some key has no value! Length is: " + strParsed.Length);
                continue;
            }

            Dictionary<string, string> newDictionary = new Dictionary<string, string>();

            if (languageAlreadyExistsInDict)
            {
                newDictionary = dictStringsByLanguage[loadedLanguage];
            }

            float fDelayTimer = Time.realtimeSinceStartup;
            int checkForDelay = 20;
            for (int idxString = 0; idxString < strParsed.Length; idxString += 2)
            {
                string strKey = strParsed[idxString];
                string strValue = strParsed[idxString + 1];


                //clean out the XML jank
                strValue = strValue.Replace("&#xA;", "\n");
                //Debug.Log(strKey);

                /* if (newDictionary.ContainsKey(strKey))
                {
                    Debug.LogError("Oh no! '" + strKey + "' is being added to the dictionary twice!"); // Index " + idxString + " content: " + strValue + " previous key/value: " + prevKey + " " + prevValue);
                }
                else */
                {
                    strValue = strValue.Replace("<color=yellow>", "<#fffb00>");
                    strValue = strValue.Replace("<color=red>", "<#f20a0a>");
                    strValue = strValue.Replace("<color=#", "<#");


                    if (newDictionary.ContainsKey(strKey))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Already have key " + strKey + " with current value " + newDictionary[strKey] + " compare to request value of " + strValue);
                        continue;
                    }


                    newDictionary.Add(strKey, strValue);

                    /* if (Debug.isDebugBuild && strKey == "dialog_tutorial_wait_main_txt_universal")
                    {
                        Debug.Log("HEY! Loaded text value is " + strValue);
                    } */
                }

                checkForDelay--;
                if (checkForDelay == 0)
                {
                    if (Time.time - fDelayTimer > GameMasterScript.MIN_FPS_DURING_LOAD)
                    {                        
                        yield return null;
                        fDelayTimer = Time.time;
                    }
                    checkForDelay = 10;
                }
            }

            //ready for action
            if (!languageAlreadyExistsInDict)
            {
                dictStringsByLanguage.Add(loadedLanguage, newDictionary);
            }
        }

        //bAllStringsLoaded = true;


    }	
	
	static void PostLoadingInitialization() 
	{        
        AssignCharacterJobNames();        
        AssignEnumsToDictionaries();	
	}

    public static EGameLanguage TryGetGameLanguageFromPlayerPrefs()
    {
        EGameLanguage lang = EGameLanguage.en_us;
        string checkLang = TDPlayerPrefs.GetString(GlobalProgressKeys.LANGUAGE);
        
        if (!string.IsNullOrEmpty(checkLang))
        {
            try
            {
                lang = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), checkLang);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to parse game language from playerprefs due to " + e);
            }
        }

        return lang;
    }

    static void UpdateChineseBindingLocalization()
    {
        if (StringManager.gameLanguage != EGameLanguage.zh_cn) return;
#if UNITY_SWITCH
        return;
#endif



    }

}