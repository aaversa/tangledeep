using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;

public enum GlobalProgressKeys { 
    BEAT_FIRST_BOSS, 
    SHARA_STORY_CLEARED, 
    LAST_PLAYED_CAMPAIGN,
    VECTOR_JP,
    VECTOR_CN,
    ABIL_USED_EVER,
    ABIL_LEARNED_EVER,
    LANGUAGE,
    SORCERESS_UNLOCKED,
    SHARA_CAMPAIGN_UNLOCKED,
    SHARA_WHO_UNLOCKED_ME,
    LAST_SEPARATOR,
    DRAGON_DEFEAT,
    KEYBOARD_MAP_SELECTED,
    LAST_TIP,
    STARTUP_DRAGON_MUSIC,
    CONVERTED_PLAYER_PREFS_EVER,
    CUSTOM_SEASON,
    COUNT }

/// <summary>
/// Wrapper for PlayerPrefs that will save a global progress file instead of strictly relying on Unity's PlayerPrefs.
/// </summary>
public class TDPlayerPrefs  
{    

    enum PrefsLoadType { LOAD_INTS, LOAD_ENUM_TO_STRINGS, LOAD_STRINGS, COUNT };

    public static Dictionary<GlobalProgressKeys, int> dictEnumToIntData;
    public static Dictionary<GlobalProgressKeys, string> dictEnumToStringData;
    
    /// <summary>
    /// Used to handle controller mappings.
    /// </summary>
    public static Dictionary<string, string> dictStringToStringData;

    const string DATA_FILE_NAME = "/globalprefsdata.txt";

    const string HEADER_INT_DICT = "dictint";
    const string HEADER_ENUM_TO_STRING_DICT = "dictenumtostring";
    const string HEADER_STRING_TO_STRING_DICT = "dictstringtostring";
    const string DEFAULT_STRING_VALUE = "NULL_NOTHING";
    const char SEPARATOR = '&';

    static Dictionary<GlobalProgressKeys, string> globalProgressKeysToStrings;

    static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        globalProgressKeysToStrings = new Dictionary<GlobalProgressKeys, string>()
        {
            { GlobalProgressKeys.BEAT_FIRST_BOSS, "beatfirstboss" },
            { GlobalProgressKeys.SHARA_STORY_CLEARED, "sharastorycleared" },
            { GlobalProgressKeys.LAST_PLAYED_CAMPAIGN, "lastplayedcampaign" },
            { GlobalProgressKeys.VECTOR_JP, "vectorjp" },
            { GlobalProgressKeys.VECTOR_CN, "vectorcn" },
            { GlobalProgressKeys.ABIL_USED_EVER, "AbilUsedEver" },
            { GlobalProgressKeys.ABIL_LEARNED_EVER, "AbilLearnedEver" },
            { GlobalProgressKeys.LANGUAGE, "lang" },
            { GlobalProgressKeys.SORCERESS_UNLOCKED, "sorceress_unlocked" },
            { GlobalProgressKeys.SHARA_CAMPAIGN_UNLOCKED, "sharacampaignunlocked" },
            { GlobalProgressKeys.SHARA_WHO_UNLOCKED_ME, "sharacampaignherowhounlockedme" },
            { GlobalProgressKeys.LAST_SEPARATOR, "last_separator" },
            { GlobalProgressKeys.DRAGON_DEFEAT, "dragondefeat" },
            { GlobalProgressKeys.KEYBOARD_MAP_SELECTED, "KeyboardMapSelected" },
            { GlobalProgressKeys.LAST_TIP, "lasttip" },
            { GlobalProgressKeys.STARTUP_DRAGON_MUSIC, "startup_dragon_music" },
            { GlobalProgressKeys.CUSTOM_SEASON, "custom_season" }
        };

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        return;
#endif
        dictEnumToIntData = new Dictionary<GlobalProgressKeys, int>();
        dictEnumToStringData = new Dictionary<GlobalProgressKeys, string>();
        dictStringToStringData = new Dictionary<string, string>();


    }

    public static void SetLocalInt(string keyName, int keyValue)
    {
#if UNITY_XBOXONE
        XboxSaveManager.instance.SetInt(keyName, keyValue);
#else
        PlayerPrefs.SetInt(keyName, keyValue);
#endif
    }

    public static void SetInt(GlobalProgressKeys keyName, int keyValue)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        PlayerPrefs.SetInt(globalProgressKeysToStrings[keyName], keyValue);
        return;
#elif UNITY_XBOXONE
        XboxSaveManager.instance.SetInt(globalProgressKeysToStrings[keyName], keyValue);
        return;
#endif
        dictEnumToIntData[keyName] = keyValue;
    }

    public static void SetString(GlobalProgressKeys keyName, string keyValue)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        PlayerPrefs.SetString(globalProgressKeysToStrings[keyName], keyValue);
        return;
#elif UNITY_XBOXONE
        XboxSaveManager.instance.SetString(globalProgressKeysToStrings[keyName], keyValue);
        return;
#endif
        dictEnumToStringData[keyName] = keyValue;
    }

    /// <summary>
    /// Used by Rewired where we don't know the enum key name, e.g. this is not for key data in TD code.
    /// </summary>
    /// <param name="keyName"></param>
    /// <param name="keyValue"></param>
    public static void SetString(string keyName, string keyValue)
    {
#if UNITY_SWITCH || UNITY_PS4 || UNITY_ANDROID
        PlayerPrefs.SetString(keyName, keyValue);
        return;
#elif UNITY_XBOXONE
        //XboxSaveManager.instance.SetString(keyName, keyValue);
        PlayerPrefs.SetString(keyName, keyValue);
        return;
#endif
        dictStringToStringData[keyName] = keyValue;
    }

    public static string GetString(string keyName)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        return PlayerPrefs.GetString(keyName);
#elif UNITY_XBOXONE
        //return XboxSaveManager.instance.GetString(keyName);
        return PlayerPrefs.GetString(keyName);
#endif
        string returnValue = string.Empty;
        if (!dictStringToStringData.TryGetValue(keyName, out returnValue))
        {
            return string.Empty;
        }
        return returnValue;
    }

    public static string GetString(string keyName, string defaultValue)
    {
#if UNITY_SWITCH || UNITY_PS4 || UNITY_ANDROID
        if (!PlayerPrefs.HasKey(keyName)) return defaultValue;
        return PlayerPrefs.GetString(keyName);
#elif UNITY_XBOXONE
        //if (!XboxSaveManager.instance.HasKey(keyName)) return defaultValue;
        //return XboxSaveManager.instance.GetString(keyName);

        if (!PlayerPrefs.HasKey(keyName)) return defaultValue;
        return PlayerPrefs.GetString(keyName);
#endif
        string returnValue = defaultValue;
        if (!dictStringToStringData.TryGetValue(keyName, out returnValue))
        {
            return defaultValue;
        }
        return returnValue;
    }

    public static bool HasKey(string keyName)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        return PlayerPrefs.HasKey(keyName);
#elif UNITY_XBOXONE
        return XboxSaveManager.instance.HasKey(keyName);
#endif
        return dictStringToStringData.ContainsKey(keyName);
    }

    public static int GetInt(string keyName)
    {
#if UNITY_XBOXONE
        return XboxSaveManager.instance.GetInt(keyName);
#endif
        return PlayerPrefs.GetInt(keyName);
    }

    public static int GetInt(GlobalProgressKeys keyName)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        return PlayerPrefs.GetInt(globalProgressKeysToStrings[keyName]);
#elif UNITY_XBOXONE
        return XboxSaveManager.instance.GetInt(globalProgressKeysToStrings[keyName]);
#endif
        int returnValue = 0;
        dictEnumToIntData.TryGetValue(keyName, out returnValue);
        return returnValue;
    }

    public static string GetString(GlobalProgressKeys keyName)
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        return PlayerPrefs.GetString(globalProgressKeysToStrings[keyName]);
#elif UNITY_XBOXONE
        return XboxSaveManager.instance.GetString(globalProgressKeysToStrings[keyName]);
#endif
        string returnValue = "";
        dictEnumToStringData.TryGetValue(keyName, out returnValue);
        return returnValue;
    }

    /// <summary>
    /// Called by rewired, but do we really need to do anything here?
    /// </summary>
    public static void SaveTemp()
    {
        Save();
    }

    public static void Save()
    {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_ANDROID
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Saved playerprefs.");
        PlayerPrefs.Save();
        return;
#elif UNITY_XBOXONE
        XboxSaveManager.instance.Save();
        PlayerPrefs.Save();
        return;
#endif
        CreateFileOnDiskIfNecessary();

        StringBuilder sb = new StringBuilder();
        
        if (dictEnumToIntData.Keys.Count > 0)
        {
            sb.Append(HEADER_INT_DICT);
            sb.Append("\n");
            foreach (var kvp in dictEnumToIntData)
            {
                sb.Append(kvp.Key);
                sb.Append(SEPARATOR);
                sb.Append(kvp.Value);
                sb.Append("\n");
            }
        }

        if (dictEnumToStringData.Keys.Count > 0)
        {
            sb.Append(HEADER_ENUM_TO_STRING_DICT);
            sb.Append("\n");
            foreach (var kvp in dictEnumToStringData)
            {
                sb.Append(kvp.Key);
                sb.Append(SEPARATOR);
                sb.Append(kvp.Value);
                sb.Append("\n");
            }
        }

        if (dictStringToStringData.Keys.Count > 0)
        {
            sb.Append(HEADER_STRING_TO_STRING_DICT);
            sb.Append("\n");
            foreach (var kvp in dictStringToStringData)
            {
                sb.Append(kvp.Key);
                sb.Append(SEPARATOR);
                sb.Append(kvp.Value);
                sb.Append("\n");
            }
        }

        string path = CustomAlgorithms.GetPersistentDataPath() + DATA_FILE_NAME;

        try
        {
            File.WriteAllText(path, sb.ToString());
        }
        catch(Exception e)
        {
            GameLogScript.LogWriteStringRef("<color=red>An error occurred while trying to save certain global data. Please check that no application is interfering with or locking Tangledeep save files.");
        }        

    }

    static void PutPlayerPrefsIntoDictionaries()
    {
        if (LogoSceneScript.globalIsSolsticeBuild || LogoSceneScript.globalSolsticeDebug) return;

        if (GetInt(GlobalProgressKeys.CONVERTED_PLAYER_PREFS_EVER) == 1) return;

        SetInt(GlobalProgressKeys.CONVERTED_PLAYER_PREFS_EVER, 1);

#if UNITY_EDITOR
        Debug.Log("<color=green>Did ONE TIME conversion of player prefs. Don't do it again.</color>");
#endif

        foreach (var kvp in globalProgressKeysToStrings)
        {
            string strValue = globalProgressKeysToStrings[kvp.Key]; // this gives us something like "beatfirstboss"

            int checkInt = PlayerPrefs.GetInt(strValue); // if this doesn't exist, we just get 0, which is fine

            string checkString = PlayerPrefs.GetString(strValue, DEFAULT_STRING_VALUE); // if we dont' have this, we get "NULL_NOTHING" back

            if (checkString != DEFAULT_STRING_VALUE)
            {
                dictEnumToStringData.Add(kvp.Key, checkString);
                if (Debug.isDebugBuild) Debug.Log("Added string value for " + kvp.Key + " from existing registry: " + checkString);
            }
            else
            {
                dictEnumToIntData.Add(kvp.Key, checkInt);
                if (Debug.isDebugBuild) Debug.Log("Added int value for " + kvp.Key + " from existing registry: " + checkInt);
            }

        }
    }

    static void CreateFileOnDiskIfNecessary()
    {
        string path = CustomAlgorithms.GetPersistentDataPath() + DATA_FILE_NAME;
        if (!File.Exists(path))
        {
            if (Debug.isDebugBuild) Debug.Log("Global data does not exist. Check our PlayerPrefs registry...?");
            Stream saveStream = File.Create(path);            
            saveStream.Close();
        }
    }

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        return;
#endif
        string path = CustomAlgorithms.GetPersistentDataPath() + DATA_FILE_NAME;
        if (!File.Exists(path))
        {
            if (Debug.isDebugBuild) Debug.Log("Global data does not exist. Check our PlayerPrefs registry...?");
            PutPlayerPrefsIntoDictionaries();
        }
        else
        {
            string[] allTextInFile = File.ReadAllLines(path);

            PrefsLoadType currentLoadType = PrefsLoadType.LOAD_INTS;

            for (int i = 0; i < allTextInFile.Length; i++)
            {
                currentLoadType = TryReadLineInFile(allTextInFile[i], currentLoadType);
            }
        }
    }

    static PrefsLoadType TryReadLineInFile(string lineText, PrefsLoadType loadTypeBeforeThisLine)
    {
        PrefsLoadType loadTypeAfterThisLine = loadTypeBeforeThisLine;
        if (lineText == HEADER_INT_DICT)
        {
            loadTypeAfterThisLine = PrefsLoadType.LOAD_INTS;
        }
        else if (lineText == HEADER_ENUM_TO_STRING_DICT)
        {
            loadTypeAfterThisLine = PrefsLoadType.LOAD_ENUM_TO_STRINGS;
        }
        else if (lineText == HEADER_STRING_TO_STRING_DICT)
        {
            loadTypeAfterThisLine = PrefsLoadType.LOAD_STRINGS;
        }
        else
        {
            string[] contents = lineText.Split(SEPARATOR);
            if (contents.Length == 1)
            {
                return loadTypeAfterThisLine;
            }
            string key = contents[0];

            if (loadTypeAfterThisLine == PrefsLoadType.LOAD_STRINGS)
            {
                TryLoadStringData(contents);
            }
            else
            {
                GlobalProgressKeys eKey = GlobalProgressKeys.COUNT;

                try
                {
                    eKey = (GlobalProgressKeys)Enum.Parse(typeof(GlobalProgressKeys), key);
                }
                catch(Exception e)
                {
                    if (Debug.isDebugBuild) Debug.Log("Failed to parse global progress key " + key);
                    return loadTypeAfterThisLine;
                }
                
                string value = contents[1];
                if (loadTypeAfterThisLine == PrefsLoadType.LOAD_INTS)
                {
                    int iValue = 0;
                    int.TryParse(value, out iValue);
                    dictEnumToIntData[eKey] = iValue;
#if UNITY_EDITOR
                    //Debug.Log("Loaded int global data " + eKey + " " + iValue);
#endif
                }
                else
                {
                    dictEnumToStringData[eKey] = value;
#if UNITY_EDITOR
                    //Debug.Log("Loaded string global data " + eKey + " " + value);
#endif
                }
            }
        }

        return loadTypeAfterThisLine;
    }

    /// <summary>
    /// Pass in a length=2 array with two strings, key and value.
    /// </summary>
    /// <param name="lineSeparatedArray"></param>
    static void TryLoadStringData(string[] lineSeparatedArray)
    {
        string key = lineSeparatedArray[0];
        string value = lineSeparatedArray[1];
#if UNITY_EDITOR
        //Debug.Log("Loaded string to string KVP: " + key + "," + value);
#endif
        dictStringToStringData[key] = value;
    }

    public static void DeleteAll()
    {
        dictEnumToIntData.Clear();
        dictEnumToStringData.Clear();
        dictStringToStringData.Clear();
    }

}
