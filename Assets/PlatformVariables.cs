using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Text;
using System.Globalization;


/// <summary>
/// Toggles behaviors that might vary depending on platform (PC, Switch, etc.) on a case-by-case basis
/// </summary>
public static class PlatformVariables
{
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool ALLOW_PLAYER_MODS = false;
#else
    public static bool ALLOW_PLAYER_MODS = true;
#endif

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool SHOW_TEXT_INPUT_BOXES = false;
#else
    public static bool SHOW_TEXT_INPUT_BOXES = true;
#endif

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR = true;
#else
    public static bool CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR = false;
#endif

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool ALLOW_STEAM_ACHIEVEMENTS = false;
#else
    public static bool ALLOW_STEAM_ACHIEVEMENTS = true;
#endif

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool OPTIMIZE_MONSTER_BEHAVIOR = true;
#else
    public static bool OPTIMIZE_MONSTER_BEHAVIOR = false;
#endif

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
    public static bool ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION = false;
#else
    public static bool ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool OPTIMIZED_GAME_LOG = false;
#else
    public static bool OPTIMIZED_GAME_LOG = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool OPTIMIZE_SPRITE_MATERIALS = false;
#else
    public static bool OPTIMIZE_SPRITE_MATERIALS = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool USE_INTROLOOP = true;
#else
    public static bool USE_INTROLOOP = false;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool ALLOW_WEB_CHALLENGES = true;
#else
    public static bool ALLOW_WEB_CHALLENGES = false;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool VERIFY_FILE_HASH_FOR_CHALLENGES = true;
#else
    public static bool VERIFY_FILE_HASH_FOR_CHALLENGES = false;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool SEND_UNITY_ANALYTICS = true;
#else
    public static bool SEND_UNITY_ANALYTICS = false;
#endif

#if !UNITY_SWITCH //&& !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool LOAD_EVERYTHING_FROM_ASSET_BUNDLES = false;
#else
    public static bool LOAD_EVERYTHING_FROM_ASSET_BUNDLES = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool GAMEPAD_STYLE_OPTIONS_MENU = false;
#else
    public static bool GAMEPAD_STYLE_OPTIONS_MENU = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE //&& !UNITY_ANDROID
    public static bool USE_GAMEPAD_ONLY_HOTBAR_STYLE = false;
#else
    public static bool USE_GAMEPAD_ONLY_HOTBAR_STYLE = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool SHOW_SEARCHBARS = true;
#else
    public static bool SHOW_SEARCHBARS = false;
#endif

#if !UNITY_SWITCH
    public static bool FIXED_FRAMERATE = false;
#else
    public static bool FIXED_FRAMERATE = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool GAMEPAD_ONLY = false;
#else
    public static bool GAMEPAD_ONLY = true;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool LEADERBOARDS_ENABLED = true;
#else
    public static bool LEADERBOARDS_ENABLED = false;
#endif

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public static bool CHANGE_SKILLS_ONLY_IN_SAFE_AREAS = true;
#else
    public static bool CHANGE_SKILLS_ONLY_IN_SAFE_AREAS = false;
#endif

    static void CreatePlatformOptionsFile()
    {

        string path = CustomAlgorithms.GetPersistentDataPath() + "/platformoptions.txt";

        string separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

        string[] textForFile = { "show_text_input_boxes\tfalse",
            "use_abilities_off_hotbar\tfalse",
            "allow_steam_achievements\ttrue",
            "optimized_monster_behavior\tfalse",
            "titlescreen_language_selection\ttrue",
            "optimized_game_log\tfalse",
            "optimized_sprite_materials\tfalse",
            "use_introloop_music\ttrue",
            "enable_leaderboards\ttrue",
            "allow_web_challenges\ttrue",
            "send_unity_analytics\ttrue",
            "load_all_from_asset_bundles\tfalse",
            "gamepad_only\tfalse",
            "gamepad_style_options\tfalse",
            "gamepad_style_hotbar\tfalse",
            "show_searchbars\ttrue",
            "fixed_framerate\tfalse",
            "change_skills_safe_areas_only\ttrue"
        };

        try
        {
            File.WriteAllLines(path, textForFile, Encoding.UTF8);
        }
        catch(Exception e)
        {
            Debug.Log("Could not write platform variables file because: " + e);
        }
        
        
    }

    /// <summary>
    /// Load values from "platformoptions.txt" file for debug builds so we can play around for testing. 
    /// </summary>
    public static void OnInitialLoad()
    {
        // This is not relevant for consoles.

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        return;
#endif

        string path = CustomAlgorithms.GetPersistentDataPath() + "/platformoptions.txt";

        if (!File.Exists(path))
        {
            CreatePlatformOptionsFile();
            //if (Debug.isDebugBuild) Debug.Log("Did not find platform variables file.");
            return;
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Checking platform variables file...");
        }

        string[] allLines = File.ReadAllLines(path);

        for (int i = 0; i < allLines.Length; i++)
        {
            string[] split = allLines[i].Split('\t');
            if (split.Length != 2) continue;

            bool value = false;
            switch(split[0])
            {
                case "show_text_input_boxes":
                    if (bool.TryParse(split[1], out value))
                    {
                        SHOW_TEXT_INPUT_BOXES = value;
                    }
                    break;
                case "use_abilities_off_hotbar":
                    if (bool.TryParse(split[1], out value))
                        CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR = value;
                    break;
                case "allow_player_mods":
                    if (bool.TryParse(split[1], out value))
                        ALLOW_PLAYER_MODS = value;
                    break;
                case "allow_steam_achievements":
                    if (bool.TryParse(split[1], out value))
                        ALLOW_STEAM_ACHIEVEMENTS = value;
                    break;
                case "optimized_monster_behavior":
                    if (bool.TryParse(split[1], out value))
                        OPTIMIZE_MONSTER_BEHAVIOR = value;
                    break;
                case "titlescreen_language_selection":
                    if (bool.TryParse(split[1], out value))
                        ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION = value;
                    break;
                case "optimized_game_log":
                    if (bool.TryParse(split[1], out value))
                        OPTIMIZED_GAME_LOG = value;
                    break;
                case "optimized_sprite_materials":
                    if (bool.TryParse(split[1], out value))
                        OPTIMIZE_SPRITE_MATERIALS = value;
                    break;
                case "use_introloop_music":
                    if (bool.TryParse(split[1], out value))
                        USE_INTROLOOP = value;
                    break;
                case "enable_leaderboards":
                    if (bool.TryParse(split[1], out value))
                        LEADERBOARDS_ENABLED = value;
                    break;
                case "allow_web_challenges":
                    if (bool.TryParse(split[1], out value))
                        ALLOW_WEB_CHALLENGES = value;
                    break;
                case "send_unity_analytics":
                    if (bool.TryParse(split[1], out value))
                        SEND_UNITY_ANALYTICS = value;
                    break;
                case "load_all_from_asset_bundles":
                    if (bool.TryParse(split[1], out value))
                        LOAD_EVERYTHING_FROM_ASSET_BUNDLES = value;
                    break;
                case "gamepad_only":
                    if (bool.TryParse(split[1], out value))
                        GAMEPAD_ONLY = value;
                    break;
                case "gamepad_style_options":
                    if (bool.TryParse(split[1], out value))
                        GAMEPAD_STYLE_OPTIONS_MENU = value;
                    break;
                case "gamepad_style_hotbar":
                    if (bool.TryParse(split[1], out value))
                        USE_GAMEPAD_ONLY_HOTBAR_STYLE = value;
                    break;
                case "show_searchbars":
                    if (bool.TryParse(split[1], out value))
                        SHOW_SEARCHBARS = value;
                    break;
                case "fixed_framerate":
                    if (bool.TryParse(split[1], out value))
                        FIXED_FRAMERATE = value;
                    break;
                case "change_skills_safe_areas_only":
                    if (bool.TryParse(split[1], out value))
                        CHANGE_SKILLS_ONLY_IN_SAFE_AREAS = value;
                    break;
            }

        }
    }

}
