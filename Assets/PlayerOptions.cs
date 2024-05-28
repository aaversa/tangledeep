using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Text;
using System.Globalization;
//using UnityEditor;

public enum OptionsSlider { RESOLUTION, ZOOMSCALE, MUSICVOLUME, SFXVOLUME, FOOTSTEPSVOLUME, CURSORREPEATDEALY, BUTTONDEADZONE, FRAMECAP, TEXTSPEED, BATTLETEXTSPEED, BATTLETEXTSCALE, COUNT }
public enum TextSpeed { SLOW, MEDIUM, FAST, VERYFAST, INSTANT }
public enum MiniMapStyles { CYCLE, SMALL, LARGE, OVERLAY, COUNT }

public class PlayerOptions
{

    public static int musicVolume;
    public static int SFXVolume;
    public static int footstepsVolume;

    public static int hundredBasedMusicVolume;
    public static int hundredBasedSFXVolume;
    public static int hundredBasedFootstepsVolume;

    public static int zoomScale;
    public static int cursorRepeatDelay;
    public static int framecap;
    public static int buttonDeadZone;
    public static bool lockCamera;
    public static bool smoothCamera;
    public static bool scanlines;
    public static bool tutorialTips;
    public static bool battleJPXPGain;
    public static bool autoPickupItems;
    public static bool pickupDisplay;
    public static bool extraTurnPopup;
    public static bool gridOverlay;
    public static bool audioOffWhenMinimized;
    public static int resolutionX;
    public static int resolutionY;
    public static bool fullscreen;
    public static KeyboardControlMaps keyboardMap;
    public static KeyboardControlMaps defaultKeyboardMap;
    public static bool playerHealthBar;
    public static bool monsterHealthBars;
    public static bool showControllerPrompts;
    public static bool autoEquipWeapons;
    public static bool autoPlanksInItemWorld;
    public static bool autoEquipBestOffhand;
    public static bool smallLogText;
    public static bool showUIPulses;
    public static bool showRumorOverlay;
    public static bool autoEatFood;
    public static int textSpeed;
    public static int battleTextSpeed;
    public static bool screenFlashes;
    public static int battleTextScale;

    public static bool autoAbandonTrivialRumors;

    public static bool disableMouseOnKeyJoystick;

    public static bool disableMouseMovement;

    public static bool draggedMinimap;
    public static MinimapStates mapState;
    public static float miniMapPositionX;
    public static float miniMapPositionY;
    public static bool useVectorJPFont;

    public static JoystickControlStyles joystickControlStyle;

    public static int mapStyle; // 312019: Allows player to select what minimap button does. Fix to a specific style or cycle
    public static int mapOpacity; // 312019: scalable map opacity

    public static bool verboseCombatLog;

    public static float fixedMinimapSize;
    public static float minimapOpacity;
    public static bool speedrunMode;
    public static bool globalUnlocks = false;
    public static float animSpeedScale;
    public static float turnSpeedScale;

    public static List<string> playerModsEnabled;

    public static float[] miscSettingsModBalance;

    public static void ReadOptionsFromFile()
    {
#if UNITY_SWITCH
        string path = "preferences.xml";
        string strLoadedData = "";
        var sdh = Switch_SaveDataHandler.GetInstance();
        if( !sdh.LoadSwitchDataFile(ref strLoadedData, path))
        { 
#elif UNITY_PS4
        string path = "preferences.xml";
        string strLoadedData = "";
        byte[] byteLoadedData = null;
        LoadMiscSettings();        
        if (!PS4SaveManager.instance.ExistsFile(PS4SaveManager.ROOT_DIR, path))
        {
#elif UNITY_XBOXONE
        string path = "preferences.xml";
        string strLoadedData = "";
        LoadMiscSettings();
        if (!XboxSaveManager.instance.HasKey(path))
        {
#else    

        LoadMiscSettings();
        string path = CustomAlgorithms.GetPersistentDataPath() + "/preferences.xml";

        if (!File.Exists(path))
        {
#endif
            // Initial options
            playerModsEnabled = new List<string>();
            verboseCombatLog = false;
            zoomScale = 2;
            lockCamera = true;
            smoothCamera = false;
            scanlines = true;
            tutorialTips = true;
            battleJPXPGain = false;
            musicVolume = -8;
            SFXVolume = -5;
            buttonDeadZone = 33;
            footstepsVolume = -20;
#if UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
            //on PS4/XBOXONE I changed and rounded the value of sounds
            musicVolume = -6;
            SFXVolume = -0;
            footstepsVolume = -12;
#endif
            autoPickupItems = true;
            disableMouseMovement = false;
            pickupDisplay = false;
            extraTurnPopup = false;
            gridOverlay = false;
            framecap = 1;
            cursorRepeatDelay = 300;
            autoPlanksInItemWorld = true;
            autoEquipBestOffhand = true;
            autoAbandonTrivialRumors = true;
#if !UNITY_SWITCH
            smallLogText = false;
#else
            smallLogText = StringManager.gameLanguage == EGameLanguage.jp_japan || StringManager.gameLanguage == EGameLanguage.zh_cn;
#endif
            joystickControlStyle = JoystickControlStyles.STEP_MOVE;

#if UNITY_PS4
            mapStyle = (int)MiniMapStyles.LARGE;
#else
            mapStyle = (int)MiniMapStyles.CYCLE;
#endif
            textSpeed = (int)TextSpeed.MEDIUM;
            battleTextSpeed = (int)TextSpeed.FAST;
            battleTextScale = 100;
            autoEquipWeapons = true;
            showUIPulses = true;
            showRumorOverlay = true;
            autoEatFood = true;
            keyboardMap = KeyboardControlMaps.COUNT;
            screenFlashes = true;
            draggedMinimap = false;
            mapState = MinimapStates.CLOSED;
            fixedMinimapSize = 0f;
            minimapOpacity = 0.82f;

            if (Debug.isDebugBuild) Debug.Log("Resolution at launch is " + Screen.currentResolution.ToString() + " But height is " + Screen.height);

            // BUG: Screen is getting big and huge regardless of player settings in initial dialog.
            // FIX: Do not run code that automatically makes screen big and huge regardless of player settings in initial dialog.

            fullscreen = Screen.fullScreen;
            Screen.SetResolution(Screen.width, Screen.height, fullscreen);

            resolutionX = Screen.width;
            resolutionY = Screen.height;

            playerHealthBar = true;
            monsterHealthBars = true;

            if (PlatformVariables.GAMEPAD_ONLY)
            {
                showControllerPrompts = false;
            }
            else
            {
                showControllerPrompts = TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick ? true : false;
                disableMouseOnKeyJoystick = true;
                joystickControlStyle = JoystickControlStyles.STEP_MOVE;
            }

            audioOffWhenMinimized = true;

            WriteOptionsToFile();

            SetHundredBasedVolumeValuesFromBaseValues();

            return;
        }

        //Before we read in player prefs, recognize that we've
        //moved the resolution data out of there and rely on Unity's
        //tracking of said values.

        fullscreen = Screen.fullScreen;
        resolutionX = Screen.width;
        resolutionY = Screen.height;

        //now read in everything else <3 

        XmlReaderSettings metaSettings = new XmlReaderSettings();
        metaSettings.IgnoreComments = true;
        metaSettings.IgnoreProcessingInstructions = true;
        metaSettings.IgnoreWhitespace = true;
#if UNITY_SWITCH
        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_PS4
        PS4SaveManager.instance.ReadData(PS4SaveManager.ROOT_DIR, path, out byteLoadedData);
        //convert byte to string
        strLoadedData = System.Text.Encoding.UTF8.GetString(byteLoadedData);

        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#elif UNITY_XBOXONE
        strLoadedData = XboxSaveManager.instance.GetString(path);

        XmlReader metaReader = XmlReader.Create(new StringReader(strLoadedData), metaSettings);
#else
        FileStream metaStream = new FileStream(path, FileMode.Open);
        XmlReader metaReader = XmlReader.Create(metaStream, metaSettings);
#endif

        metaReader.Read();

        bool readBattleTextSpeed = false;

        while (metaReader.NodeType != XmlNodeType.EndElement)
        {
            //Debug.Log("<color=green>Options read: " + metaReader.Name + "</color>");
            switch (metaReader.Name)
            {
                case "playermodsenabled":
                    if (playerModsEnabled == null) playerModsEnabled = new List<string>();
                    playerModsEnabled.Clear();

                    string unparsedMods = metaReader.ReadElementContentAsString();
                    string[] parsedMods = unparsedMods.Split('|');

                    for (int i = 0; i < parsedMods.Length; i++)
                    {
                        playerModsEnabled.Add(parsedMods[i]);
                    }

                    PlayerModManager.SyncModEnableStateFromOptions();

                    break;
                case "musicvolume":
                    musicVolume = metaReader.ReadElementContentAsInt();
                    break;
                case "sfxvolume":
                    SFXVolume = metaReader.ReadElementContentAsInt();
                    break;
                case "footstepsvolume":
                    footstepsVolume = metaReader.ReadElementContentAsInt();
                    //not sure why it is set to -1 but it made that when you change valume to 100, save and load, it would be 97, so for PS4/XBOX i changed it
#if UNITY_PS4 || UNITY_XBOXONE
                    if (footstepsVolume >= 0)
                    {
                        footstepsVolume = 0;
                    }
#else
                    if (footstepsVolume >= -1)
                    {
                        footstepsVolume = -1;
                    }
#endif
                    break;
                case "zoomscale":
                    zoomScale = metaReader.ReadElementContentAsInt();
                    break;
                case "cursorrepeatdelay":
                    cursorRepeatDelay = metaReader.ReadElementContentAsInt();
                    break;
                case "framecap":
                    framecap = metaReader.ReadElementContentAsInt();
                    break;
                case "autoabandonrumors":
                    autoAbandonTrivialRumors = metaReader.ReadElementContentAsBoolean();
                    break;
                case "buttondeadzone":
                    buttonDeadZone = metaReader.ReadElementContentAsInt();
                    break;
                case "disablemousemovement":
                    disableMouseMovement = metaReader.ReadElementContentAsBoolean();
                    break;
                case "lockcamera":
                    lockCamera = metaReader.ReadElementContentAsBoolean();
                    break;
                case "vectorjp":
                    useVectorJPFont = metaReader.ReadElementContentAsBoolean();
                    break;
                case "screenflashes":
                    screenFlashes = metaReader.ReadElementContentAsBoolean();
                    break;
                case "smoothcamera":
                    smoothCamera = metaReader.ReadElementContentAsBoolean();
                    smoothCamera = true;
                    break;
                case "scanlines":
                    scanlines = metaReader.ReadElementContentAsBoolean();
                    break;
                case "uipulses":
                    showUIPulses = metaReader.ReadElementContentAsBoolean();
                    break;
                case "rumoroverlay":
                    showRumorOverlay = metaReader.ReadElementContentAsBoolean();
                    break;
                case "autoeat":
                    autoEatFood = metaReader.ReadElementContentAsBoolean();
                    break;
                case "tutorialtips":
                    tutorialTips = metaReader.ReadElementContentAsBoolean();
                    break;
                case "battlejpxpgain":
                    battleJPXPGain = metaReader.ReadElementContentAsBoolean();
                    break;
                case "autopickupitems":
                    autoPickupItems = metaReader.ReadElementContentAsBoolean();
                    autoPickupItems = true;
                    break;
                case "pickupdisplay":
                    pickupDisplay = metaReader.ReadElementContentAsBoolean();
                    break;
                case "smalllogtext":
                    smallLogText = metaReader.ReadElementContentAsBoolean();
                    break;
                case "extraturnpopup":
                    extraTurnPopup = metaReader.ReadElementContentAsBoolean();
                    break;
                // These values are set by the launcher, if we read them in from PlayerPrefs
                // we will cancel the values the player actually prefs.
                /*
                case "fullscreen":
                    fullscreen = metaReader.ReadElementContentAsBoolean();
                    break;
                case "resolutionx":
                    resolutionX = metaReader.ReadElementContentAsInt();
                    break;
                case "resolutiony":
                    resolutionY = metaReader.ReadElementContentAsInt();
                    break;
                */
                case "gridoverlay":
                    gridOverlay = metaReader.ReadElementContentAsBoolean();
                    break;
                case "playerhealthbar":
                    playerHealthBar = metaReader.ReadElementContentAsBoolean();
                    break;
                case "monsterhealthbars":
                    monsterHealthBars = metaReader.ReadElementContentAsBoolean();
                    break;
                case "defaultkeyboardcontrol":
                    defaultKeyboardMap = (KeyboardControlMaps)Enum.Parse(typeof(KeyboardControlMaps), metaReader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "textdensity": // deprecated
                    //textDensity = (TextDensity)Enum.Parse(typeof(TextDensity), metaReader.ReadElementContentAsString().ToUpperInvariant());
                    metaReader.Read();
                    break;
                case "logverbosity":
                    verboseCombatLog = metaReader.ReadElementContentAsBoolean();
                    break;
                case "showcontrollerprompts":
                    showControllerPrompts = metaReader.ReadElementContentAsBoolean();
                    break;
                case "audiooffwhenminimized":
                    audioOffWhenMinimized = metaReader.ReadElementContentAsBoolean();
                    break;
                case "autoplanksinitemworld":
                    autoPlanksInItemWorld = metaReader.ReadElementContentAsBoolean();
                    break;
                case "autoequipbestoffhand":
                    autoEquipBestOffhand = metaReader.ReadElementContentAsBoolean();
                    break;
                case "autoequipweapons":
                    autoEquipWeapons = metaReader.ReadElementContentAsBoolean();
                    break;
                case "textspeed":
                    textSpeed = metaReader.ReadElementContentAsInt();
                    break;
                case "battletextscale":
                case "battletextsscale":
                    battleTextScale = metaReader.ReadElementContentAsInt();
                    break;
                case "battletextspeed":
                    battleTextSpeed = metaReader.ReadElementContentAsInt();
                    readBattleTextSpeed = true;
                    break;
                case "mapstate":
                    mapState = (MinimapStates)Enum.Parse(typeof(MinimapStates), metaReader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "minimappositionx":
                    miniMapPositionX = (float)(metaReader.ReadElementContentAsInt());
                    break;
                case "minimappositiony":
                    miniMapPositionY = (float)(metaReader.ReadElementContentAsInt());
                    break;
                case "draggedminimap":
                    draggedMinimap = metaReader.ReadElementContentAsBoolean();
                    break;
                case "disablemouse":
                    disableMouseOnKeyJoystick = metaReader.ReadElementContentAsBoolean();
                    break;
                case "controllerstyle":
                    joystickControlStyle = (JoystickControlStyles)Enum.Parse(typeof(JoystickControlStyles), metaReader.ReadElementContentAsString());
                    break;
                /* case "logdatadisplaytypes":
                    string unparsed = metaReader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        Boolean.TryParse(parsed[i], out logDataDisplayTypes[i]);
                    }
                    break; */

                case "minimapstyle":
                    mapStyle = metaReader.ReadElementContentAsInt();
                    break;
                case "mapopacity":
                    mapOpacity = metaReader.ReadElementContentAsInt();
                    break;
                default:
                    metaReader.Read();
                    break;
            }
        }
        metaReader.ReadEndElement();
        metaReader.Close();
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
        metaStream.Close();
#endif
        if (!readBattleTextSpeed)
        {
            battleTextSpeed = (int)TextSpeed.FAST;
        }
        if (battleTextScale < 50)
        {
            battleTextScale = 100;
        }

        SetHundredBasedVolumeValuesFromBaseValues();
    }

    public static void WriteOptionsToFile()
    {
#if UNITY_SWITCH
        System.Text.StringBuilder sbData = new System.Text.StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding(true);
        MemoryStream ms = new MemoryStream();
        XmlWriter metaWriter = XmlWriter.Create(ms, xmlSettings);
#elif UNITY_PS4
        StringBuilder sbData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding(true);
        MemoryStream ms = new MemoryStream();
        XmlWriter metaWriter = XmlWriter.Create(ms, xmlSettings);
#elif UNITY_XBOXONE
        StringBuilder sbData = new StringBuilder();
        XmlWriterSettings xmlSettings = new XmlWriterSettings();
        xmlSettings.Encoding = new UTF8Encoding(true);
        MemoryStream ms = new MemoryStream();
        XmlWriter metaWriter = XmlWriter.Create(ms, xmlSettings);
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/preferences.xml";
        //Debug.Log("Saving preferences to " + path);
        XmlWriter metaWriter = XmlWriter.Create(path);
#endif
        metaWriter.WriteStartDocument();
        metaWriter.WriteStartElement("DOCUMENT");

        if (playerModsEnabled == null)
        {
            playerModsEnabled = new List<string>();
        }
        else
        {
            playerModsEnabled.Clear();
        }

        foreach (ModDataPack mdp in PlayerModManager.GetAllLoadedPlayerMods())
        {
            if (mdp.enabled)
            {
                playerModsEnabled.Add(mdp.modName);
            }
        }

        if (playerModsEnabled.Count > 0)
        {
            string modBuilder = "";
            for (int i = 0; i < playerModsEnabled.Count; i++)
            {
                if (i > 0)
                {
                    modBuilder += "|";
                }
                modBuilder += playerModsEnabled[i];
            }
            metaWriter.WriteElementString("playermodsenabled", modBuilder);
        }

        metaWriter.WriteElementString("musicvolume", musicVolume.ToString());
        metaWriter.WriteElementString("sfxvolume", SFXVolume.ToString());
        metaWriter.WriteElementString("footstepsvolume", footstepsVolume.ToString());
        metaWriter.WriteElementString("zoomscale", zoomScale.ToString());
        metaWriter.WriteElementString("cursorrepeatdelay", cursorRepeatDelay.ToString());
        metaWriter.WriteElementString("buttondeadzone", buttonDeadZone.ToString());
        metaWriter.WriteElementString("framecap", framecap.ToString());
        metaWriter.WriteElementString("lockcamera", lockCamera.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("disablemousemovement", disableMouseMovement.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("smoothcamera", smoothCamera.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("scanlines", scanlines.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("uipulses", showUIPulses.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("rumoroverlay", showRumorOverlay.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autoeat", autoEatFood.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("tutorialtips", tutorialTips.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("battlejpxpgain", battleJPXPGain.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autopickupitems", autoPickupItems.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("pickupdisplay", pickupDisplay.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("extraturnpopup", extraTurnPopup.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("gridoverlay", gridOverlay.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("playerhealthbar", playerHealthBar.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("monsterhealthbars", monsterHealthBars.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autoabandonrumors", autoAbandonTrivialRumors.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("logverbosity", verboseCombatLog.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("showcontrollerprompts", showControllerPrompts.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("smalllogtext", smallLogText.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("audiooffwhenminimized", audioOffWhenMinimized.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autoplanksinitemworld", autoPlanksInItemWorld.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autoequipbestoffhand", autoEquipBestOffhand.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("autoequipweapons", autoEquipWeapons.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("textspeed", textSpeed.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("battletextscale", battleTextScale.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("battletextspeed", battleTextSpeed.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("screenflashes", screenFlashes.ToString().ToLowerInvariant());

        metaWriter.WriteElementString("minimappositionx", ((int)miniMapPositionX).ToString().ToLowerInvariant());
        metaWriter.WriteElementString("minimappositiony", ((int)miniMapPositionY).ToString().ToLowerInvariant());
        metaWriter.WriteElementString("draggedminimap", draggedMinimap.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("disablemouse", disableMouseOnKeyJoystick.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("mapstate", mapState.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("controllerstyle", joystickControlStyle.ToString());
        metaWriter.WriteElementString("minimapstyle", mapStyle.ToString());
        metaWriter.WriteElementString("resolutionx", resolutionX.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("resolutiony", resolutionY.ToString().ToLowerInvariant());
        metaWriter.WriteElementString("fullscreen", fullscreen.ToString().ToLowerInvariant());

        metaWriter.WriteElementString("vectorjp", useVectorJPFont.ToString().ToLowerInvariant());

        if (defaultKeyboardMap == KeyboardControlMaps.NOTSET)
        {
            defaultKeyboardMap = KeyboardControlMaps.DEFAULT;
        }
        metaWriter.WriteElementString("defaultkeyboardcontrol", defaultKeyboardMap.ToString());

        /* string dataString = "";
        for (int i = 0; i < logDataDisplayTypes.Length; i++)
        {
            dataString += logDataDisplayTypes[i].ToString();
            if (i < logDataDisplayTypes.Length-1)
            {
                dataString += "|";
            }
        } */

        //metaWriter.WriteElementString("textdensity", textDensity.ToString().ToLowerInvariant());

        metaWriter.WriteEndElement();
        metaWriter.WriteEndDocument();
        metaWriter.Close();
        //Debug.Log("Wrote options to file.");

#if UNITY_SWITCH
        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        var sdh = Switch_SaveDataHandler.GetInstance();
        sdh.SaveSwitchFile(sr.ReadToEnd(), "preferences.xml");
#endif

#if UNITY_PS4
        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        string myString = sr.ReadToEnd();
        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(myString);        
        PS4SaveManager.instance.SaveData(PS4SaveManager.ROOT_DIR, "preferences.xml", myByte);
#endif

#if UNITY_XBOXONE
        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        string myString = sr.ReadToEnd();
        XboxSaveManager.instance.SetString("preferences.xml", myString);
        XboxSaveManager.instance.Save();

#endif

    }

    static void CreateMiscSettingsFile()
    {
        string miscSettingsPath = CustomAlgorithms.GetPersistentDataPath() + "/miscsettings.txt";

        string separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

        string[] textForFile = { "fixedmapsize\t0", "mapopacity\t0" + separator + "82", "speedrunmode\tfalse", "animspeedscale\t1", "turnspeedscale\t1", "globalunlocks\tfalse",
            "monster_spawn_rate\t1" + separator + "0", "xp_gain\t1" + separator + "0", "jp_gain\t1" + separator + "0", "gold_gain\t1" + separator +"0", "loot_rate\t1" + separator + "0",
            "magic_item_chance\t1" + separator + "0", "hero_dmg\t1" + separator + "0", "pet_dmg\t1" + separator + "0",
            "enemy_dmg\t1" + separator + "0", "pet_xp\t1" + separator + "0", "powerup_rate\t1" + separator + "0", "powerup_healing\t1" + separator + "0",
            "orb_drop_rate\t1" + separator + "0", "monster_density\t1" + separator + "0" };

        File.WriteAllLines(miscSettingsPath, textForFile, Encoding.UTF8);
    }

    static void LoadMiscSettings()
    {
        miscSettingsModBalance = new float[(int)BalanceAdjustments.COUNT];
        for (int i = 0; i < miscSettingsModBalance.Length; i++)
        {
            miscSettingsModBalance[i] = 1f;
        }

        animSpeedScale = 1f;
        minimapOpacity = 0.82f;
        fixedMinimapSize = 0f;

        string miscSettingsPath = CustomAlgorithms.GetPersistentDataPath() + "/miscsettings.txt";


        string lastUsedSeparator = TDPlayerPrefs.GetString(GlobalProgressKeys.LAST_SEPARATOR);

        if (string.IsNullOrEmpty(lastUsedSeparator))
        {
            string separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            TDPlayerPrefs.SetString(GlobalProgressKeys.LAST_SEPARATOR, separator);
        }

        //CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator

        if (string.IsNullOrEmpty(lastUsedSeparator) || !File.Exists(miscSettingsPath))
        {
            try
            {
                CreateMiscSettingsFile();
            }
            catch (Exception e)
            {
                Debug.Log("Warning! Couldn't create misc settings file due to " + e);
            }
            return;
        }
        string[] textLines = File.ReadAllLines(miscSettingsPath);

        for (int i = 0; i < textLines.Length; i++)
        {
            string[] split = textLines[i].Split('\t');
            if (split.Length < 2)
            {
                // try splitting by space...?
                split = textLines[i].Split(' ');
                if (split.Length < 2)
                {
                    return;
                }
            }

            //split[1] = split[1].Replace(" ", String.Empty);

            switch (split[0])
            {
                case "mapsize":
                case "fixedminimapsize":
                case "fixedmapsize":
                    fixedMinimapSize = CustomAlgorithms.TryParseFloat(split[1]);
                    if (fixedMinimapSize < 0 || fixedMinimapSize > Screen.width)
                    {
                        fixedMinimapSize = 0;
                    }
                    break;
                case "minimapopacity":
                case "mapopacity":
                    minimapOpacity = CustomAlgorithms.TryParseFloat(split[1]);
                    minimapOpacity = Mathf.Clamp(minimapOpacity, 0.05f, 1f);
                    break;
                case "animspeedscale":
                    animSpeedScale = CustomAlgorithms.TryParseFloat(split[1]);
                    animSpeedScale = Mathf.Clamp(minimapOpacity, 0.05f, 1f);
                    break;
                case "turnspeedscale":
                    turnSpeedScale = CustomAlgorithms.TryParseFloat(split[1]);
                    turnSpeedScale = Mathf.Clamp(minimapOpacity, 0.05f, 1f);
                    break;
                case "speedrunmode":
                    Boolean.TryParse(split[1], out speedrunMode);
                    break;
                case "globalunlocks":
                    Boolean.TryParse(split[1], out globalUnlocks);
                    break;
                case "monsterspawnrate":
                case "monster_spawn_rate":
                    miscSettingsModBalance[(int)BalanceAdjustments.MONSTER_SPAWN_RATE] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "xpgain":
                case "xp_gain":
                    miscSettingsModBalance[(int)BalanceAdjustments.XP_GAIN] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "jpgain":
                case "jp_gain":
                    miscSettingsModBalance[(int)BalanceAdjustments.JP_GAIN] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "gold_gain":
                case "goldgain":
                    miscSettingsModBalance[(int)BalanceAdjustments.GOLD_GAIN] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "lootrate":
                case "loot_rate":
                    miscSettingsModBalance[(int)BalanceAdjustments.LOOT_RATE] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "magicitemchance":
                case "magic_item_chance":
                    miscSettingsModBalance[(int)BalanceAdjustments.MAGIC_ITEM_CHANCE] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "herodmg":
                case "hero_dmg":
                    miscSettingsModBalance[(int)BalanceAdjustments.HERO_DMG] = CustomAlgorithms.TryParseFloat(split[1]);

                    break;
                case "petdmg":
                case "pet_dmg":
                    miscSettingsModBalance[(int)BalanceAdjustments.PET_DMG] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "enemydmg":
                case "enemy_dmg":
                    miscSettingsModBalance[(int)BalanceAdjustments.ENEMY_DMG] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "petxp":
                case "pet_xp":
                    miscSettingsModBalance[(int)BalanceAdjustments.PET_XP] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "poweruprate":
                case "powerup_rate":
                    miscSettingsModBalance[(int)BalanceAdjustments.POWERUP_RATE] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "poweruphealing":
                case "powerup_healing":
                    miscSettingsModBalance[(int)BalanceAdjustments.POWERUP_HEALING] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "orbdroprate":
                case "orb_drop_rate":
                    miscSettingsModBalance[(int)BalanceAdjustments.ORB_DROP_RATE] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;
                case "monsterdensity":
                case "monster_density":
                    miscSettingsModBalance[(int)BalanceAdjustments.MONSTER_DENSITY] = CustomAlgorithms.TryParseFloat(split[1]);
                    break;

            }
        }
    }

    public static void UpdateMinimapStyle()
    {
        // 312019
        // Do we need to do anything here? Probably not?
    }

    public static void SetHundredBasedVolumeValuesFromBaseValues()
    {
        hundredBasedMusicVolume = ConvertDBFloatTo0to100Int(musicVolume);
        hundredBasedSFXVolume = ConvertDBFloatTo0to100Int(SFXVolume);
        hundredBasedFootstepsVolume = ConvertDBFloatTo0to100Int(footstepsVolume);
    }

    static int ConvertDBFloatTo0to100Int(float fValue)
    {
#if UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        //there was an issue where when I set sound to 20, then saved, after loading it it would be 19, so I had to change it
        return (int)(Mathf.Lerp(0, 100, Mathf.Round((1 + fValue / 30) * 100) / 100));
#else
        return (int)(Mathf.Lerp(0, 100, 1 + fValue / 30));
#endif
    }

    public static int SSFXVolume
    {
        set
        {
            SFXVolume = value;
            hundredBasedSFXVolume = ConvertDBFloatTo0to100Int(value);
        }
    }

    public static int MusicVolume
    {
        set
        {
            musicVolume = value;
            hundredBasedMusicVolume = ConvertDBFloatTo0to100Int(value);
        }
    }

    public static int FootstepsVolume
    {
        set
        {
            footstepsVolume = value;
            hundredBasedFootstepsVolume = ConvertDBFloatTo0to100Int(value);
            //not sure why it is set to -1 but it made that when you change valume to 100, save and load, it would be 97, so for PS4/XBOX i changed it
#if UNITY_PS4 || UNITY_XBOXONE
            if (footstepsVolume >= 0) footstepsVolume = 0;
#else
            if (footstepsVolume >= -1) footstepsVolume = -1;
#endif
        }
    }

    public static void SetCurrentMiniMapPosition(float x, float y)
    {
        miniMapPositionX = x;
        miniMapPositionY = y;
    }

    public static void OffsetCurrentMiniMapPosition(float x, float y)
    {
        miniMapPositionX += x;
        miniMapPositionY += y;
    }
}

