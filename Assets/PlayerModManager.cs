using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using LapinerTools;
using LapinerTools.Steam;
using LapinerTools.uMyGUI;
using System.Xml;

public enum PlayerModfileTypes { STATUSEFFECTS, MAGICMODS, MONSTERS, LOOTTABLES, ITEMS, SPAWNTABLES, SHOPS,
    SPRITE, JOBPORTRAIT, SPRITESHEET, SPRITESHEET_META, DUNGEONROOMS, DUNGEONLEVELS, MAPOBJECTS, GAMEBALANCE, COUNT }

public enum SpriteReplaceTypes { NPC, MONSTER, DESTRUCTIBLE, BATTLEFX, COUNT }

public enum BalanceAdjustments { MONSTER_SPAWN_RATE, XP_GAIN, JP_GAIN, GOLD_GAIN, LOOT_RATE, MAGIC_ITEM_CHANCE, HERO_DMG, PET_DMG, ENEMY_DMG, PET_XP,
    POWERUP_RATE, POWERUP_HEALING, ORB_DROP_RATE, MONSTER_DENSITY, COUNT }

public class ModDataPack
{
    public string modName;
    public bool enabled;
    public Dictionary<PlayerModfileTypes, List<string>> dictPlayerModfiles;
    public Dictionary<string, Sprite> dictSprites;
    public Dictionary<string, PlayerMods_Spritesheet> dictSpriteSheets;
    public Sprite logoSprite;
    public string logoSpritePath;
    public string modDescription;
    public string modBaseDataPath;
    public float[] modBalance;
    public bool sharaOnly;
    public bool miraiOnly;

    public const string DEFAULT_MOD_DESC = "No mod description. Mod might be missing a description.txt file.";

    public ModDataPack()
    {
        modName = "";
        enabled = false; 

        dictPlayerModfiles = new Dictionary<PlayerModfileTypes, List<string>>();
        dictSprites = new Dictionary<string, Sprite>();
        dictSpriteSheets = new Dictionary<string, PlayerMods_Spritesheet>();
        logoSprite = null;
        logoSpritePath = "";
        modBaseDataPath = "";
        modDescription = DEFAULT_MOD_DESC;

        modBalance = new float[(int)BalanceAdjustments.COUNT];
        for (int i = 0; i < modBalance.Length; i++)
        {
            modBalance[i] = 1f; // default value, 1x multiplier for everything
        }
    }
}

public class PlayerModManager : MonoBehaviour {

    static bool initialized;
    static HashSet<string> spriteReferences;

    public const string PLAYER_MOD_HELP_URL = "http://impactgameworks.com/tangledeep-usercontent.html";

    //public static Dictionary<PlayerModfileTypes, List<string>> dictPlayerModfiles; This is now done on a per-mod basis.
    //public static Dictionary<string, Sprite> dictPlayerSprites;
    static List<ModDataPack> loadedPlayerMods;

    public static PlayerModManager singleton;

    public PlayerModManagerUI modBrowser;

    public static string modUploadDataPath;
    public static string modDownloadDataPath;

    [Header("Title Screen Corner")]
    public CanvasGroup modAreaCG;
    public GameObject modWorkshopButton;
    public GameObject modInstallerButton;
    public GameObject modDownloaderButton;
    public GameObject modUploaderButton;

    GameObject[] subWorkshopButtons;
    bool workshopButtonsExpanded = false;

    int attemptsToSyncModStates = 0;

    public static List<string> mostRecentModsAddedToList; // May be used by XML loaders to track where XML came from

    static float[] modBalanceAdjustments;

    public static void OnEnterGameplayScene()
    {
        for (int i = 0; i < modBalanceAdjustments.Length; i++)
        {
            modBalanceAdjustments[i] = 1f;
        }
    }

	// Use this for initialization
	void Awake () {
        Initialize();

        if (singleton != null && singleton != this)
        {
            return;
        }        
        singleton = this;
        modAreaCG.alpha = 0f;
        modBalanceAdjustments = new float[(int)BalanceAdjustments.COUNT];
        for (int i = 0; i < modBalanceAdjustments.Length; i++)
        {
            modBalanceAdjustments[i] = 1f;
        }
	}

    void Start()
    {
        subWorkshopButtons = new GameObject[3];
        subWorkshopButtons[0] = modInstallerButton;
        subWorkshopButtons[1] = modDownloaderButton;
        subWorkshopButtons[2] = modUploaderButton;
    }

    public static float GetBalanceAdjustment(BalanceAdjustments ba)
    {
        return modBalanceAdjustments[(int)ba];
    }

    /// <summary>
    /// Set by every Mod we load. Note that each mod has a MULTIPLIER for balance values. Not added/subtracted! 
    /// </summary>
    /// <param name="ba"></param>
    /// <param name="multiplier"></param>
    public static void SetBalanceAdjustment(BalanceAdjustments ba, float multiplier)
    {
        modBalanceAdjustments[(int)ba] *= multiplier;
    }

    public static List<ModDataPack> GetAllLoadedPlayerMods()
    {
        if (!initialized)
        {
            return new List<ModDataPack>();
        }
        return loadedPlayerMods;
    }

    public static void AddModFilesToList(List<string> existingList, PlayerModfileTypes modType)
    {
        if (!initialized) return;
        mostRecentModsAddedToList.Clear();
        foreach (ModDataPack mdp in loadedPlayerMods)
        {
            //Debug.Log("<color=green>" + mdp.modName + "</color> " + mdp.enabled + " " + mdp.dictPlayerModfiles.ContainsKey(modType) + " " + modType);
            if (!mdp.enabled) continue;
            if (mdp.dictPlayerModfiles.ContainsKey(modType))
            {
                foreach (string str in mdp.dictPlayerModfiles[modType])
                {
#if !UNITY_EDITOR
                    if (Debug.isDebugBuild) Debug.Log("Loading player mod file of type " + modType + ": " + str);
#endif

                    try
                    {                        
                        existingList.Add(System.IO.File.ReadAllText(str));
                        if (!mostRecentModsAddedToList.Contains(mdp.modName))
                        {
                            mostRecentModsAddedToList.Add(mdp.modName);
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Failed to add mod file of type " + modType + ": " + str);
                    }
                }
            }
        }
    }

    public static void Initialize()
    {
        if (initialized) return;

        spriteReferences = new HashSet<string>();
        mostRecentModsAddedToList = new List<string>();

        loadedPlayerMods = new List<ModDataPack>();
        //dictPlayerSprites = new Dictionary<string, Sprite>();

        modDownloadDataPath = Application.dataPath; // Load all mods from STEAM folder

        modDownloadDataPath = modDownloadDataPath.Replace("common/Tangledeep/Tangledeep_Data","");

        char finalCharacter = modDownloadDataPath[modDownloadDataPath.Length - 1];

        modDownloadDataPath += "/workshop/content/" + GameMasterScript.STEAM_APP_ID + "/";

#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        modUploadDataPath = CustomAlgorithms.GetPersistentDataPath();
#endif

        bool doDebug = false;

        if (Debug.isDebugBuild && doDebug) Debug.Log("Mod DL path is " + modDownloadDataPath + ", upload is " + modUploadDataPath);

        List<string> directoriesToCheck = new List<string>() { modUploadDataPath, modDownloadDataPath };

        foreach(string dir in directoriesToCheck)
        {
#if UNITY_SWITCH  || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
            continue;
#endif
            if (!Directory.Exists(dir))
            {
                //if (Debug.isDebugBuild) Debug.Log("Couldn't find " + dir);
                continue;
            }

            string[] directories = Directory.GetDirectories(dir);

            List<string> allFiles = new List<string>();

            if (Debug.isDebugBuild && doDebug) Debug.Log("Try load mod files from directory " + dir + " and compare to upload path " + modUploadDataPath);

            string[] splitFileName = null;

            for (int x = 0; x < directories.Length; x++)
            {
                // Find all files in this directory, which could be a player mod folder.
                string[] files = Directory.GetFiles(directories[x]);

                string folderName = directories[x].Replace(dir, "");
                folderName = folderName.Replace("\\", "").Replace(@"\", "");
                folderName = folderName.Replace("//", "").Replace(@"/", "");

                // First, find the master definitions file and the logo sprite.
                string fileMasterDef = "";

                ModDataPack mdp = new ModDataPack();

                string workshopItemInfoPath = "";

                List<string> spritesheetsToProcessInDirectory = new List<string>();

                for (int i = 0; i < files.Length; i++)
                {
                    string localFileName = GetLocalFilename(files[i], directories[x]);
                    splitFileName = localFileName.Split('\\');
                    if (splitFileName.Length > 0)
                    {
                        localFileName = splitFileName[splitFileName.Length - 1];
                    }


                    if (Debug.isDebugBuild && doDebug) Debug.Log("Iterating over " + files[i] + " with filename " + localFileName + " from dir " + folderName);

                    if (localFileName.ToLowerInvariant() == "playerfiledefs.txt")
                    {
                        fileMasterDef = localFileName;

                        if (Debug.isDebugBuild && doDebug) Debug.Log("Found file master def.");
                    }

                    else if (localFileName.ToLowerInvariant() == "description.txt")
                    {
                        mdp.modDescription = File.ReadAllText(files[i]);
                    }
                    else if (localFileName.ToLowerInvariant() == "workshopiteminfo.xml")
                    {
                        workshopItemInfoPath = files[i];
                    }
                }

                if (fileMasterDef == "")
                {
                    if (Debug.isDebugBuild && doDebug) Debug.Log("No file master def for " + directories[x]);
                    continue;
                }

                if (Debug.isDebugBuild && doDebug) Debug.Log("Loading mod from directory " + directories[x]);

                mdp.modBaseDataPath = dir;

                bool loadSuccess = false;

                // Now create a dictionary. The player (modder) must specify file type for every file in the directory.
                // Some extensions like .meta are ignored/unused.
                string fileDefContent = "";
                try
                {
                    fileDefContent = System.IO.File.ReadAllText(directories[x] + "/" + fileMasterDef);
                    loadSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to read text from manifest " + directories[x] + "/" + fileMasterDef);
                    loadSuccess = false;
                }
                if (!loadSuccess)
                {
                    Debug.Log("Couldn't read from directory.");
                    continue; // Skip this mod directory.
                }

                // If the player is uploading a mod, their folder name is the mod name.

                if (dir == modUploadDataPath)
                {
                    mdp.modName = directories[x].Replace(dir, "");
                    mdp.modName = mdp.modName.Replace("\\", "").Replace(@"\", "");
                    mdp.modName = mdp.modName.Replace("//", "").Replace(@"/", "");

                    if (Debug.isDebugBuild && doDebug) Debug.Log("MOD NAME IS: " + mdp.modName);
                }
                else
                {
                    // Otherwise, it's in the Steam workshop subfolder and has a jank numerical filename.
                    // Let's pull up WorkshopItemInfo and get the mod name from that.
                    if (workshopItemInfoPath == "")
                    {
                        Debug.Log("Workshop item info path was empty for mod in " + dir + ", skipping it.");
                        continue;
                    }
                    else
                    {
                        mdp.modName = GetModNameFromWorkshopFile(workshopItemInfoPath);
                        if (mdp.modName == "")
                        {
                            Debug.Log("No mod name for " + workshopItemInfoPath);
                            continue;
                        }
                    }
                }

                // Link up sprite

                for (int i = 0; i < files.Length; i++)
                {
                    string localFileName = GetLocalFilename(files[i], directories[x]);

                    splitFileName = localFileName.Split('\\');
                    if (splitFileName.Length > 0)
                    {
                        localFileName = splitFileName[splitFileName.Length - 1];
                    }

                    if (localFileName.ToLowerInvariant() == (mdp.modName + ".png").ToLowerInvariant())
                    {
                        mdp.logoSpritePath = files[i];
                        mdp.logoSprite = ImportSprite(files[i]);
#if UNITY_EDITOR
                        //Debug.Log("Linked sprite.");
#endif
                    }
                }


                //Debug.Log("Mod name is now " + mdp.modName);

                if (GetPlayerModFromRef(mdp.modName, printError:false) != null)
                {
                    Debug.Log(mdp.modName + " already exists in mod list, skipping.");
                    continue;
                }

                Dictionary<string, PlayerModfileTypes> dictPlayerModFilesToTypes = new Dictionary<string, PlayerModfileTypes>();

                bool skipThisFolder = false;

                string[] strParsed = fileDefContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                for (int i = 0; i < strParsed.Length; i++)
                {
                    string tabReplaced = Regex.Replace(strParsed[i], @"\t+", "\t");
                    tabReplaced = tabReplaced.Replace("\t", "|");
                    if (string.IsNullOrEmpty(tabReplaced)) continue;
                    string[] split = tabReplaced.Split('|');
                    if (split.Length != 2)
                    {
                        Debug.Log("Failure to parse " + strParsed[i] + ", skipping. " + tabReplaced);
                        continue;
                    }
                    split[0] = split[0].ToLowerInvariant();
                    if (!dictPlayerModFilesToTypes.ContainsKey(split[0]))
                    {
                        PlayerModfileTypes pmfType = PlayerModfileTypes.COUNT;
                        try { pmfType = (PlayerModfileTypes)Enum.Parse(typeof(PlayerModfileTypes), split[1].ToUpperInvariant()); }
                        catch (Exception e)
                        {
                            Debug.Log("Failed to parse " + split[1] + " to player mod file type, skipping.");
                            continue;
                        }
                        dictPlayerModFilesToTypes.Add(split[0], pmfType);
                        if (Debug.isDebugBuild && doDebug) Debug.Log("File " + split[0] + " marked as type " + pmfType);
                    }
                    else
                    {
                        Debug.Log("Key " + split[0] + " was already in dict...? Skipping.");
                        continue;
                    }
                }

                if (skipThisFolder)
                {
                    Debug.Log("Skipping entire directory " + directories[x] + " due to parsing error.");
                    continue;
                }


                for (int i = 0; i < files.Length; i++)
                {
                    string strippedFileName = GetLocalFilename(files[i], directories[x]).ToLowerInvariant();
                    splitFileName = strippedFileName.Split('\\');
                    if (splitFileName.Length > 0)
                    {
                        strippedFileName = splitFileName[splitFileName.Length - 1];
                    }

                    /* 
                                        string strippedFileName = files[i].Replace(directories[x], "").ToLowerInvariant();
                                        strippedFileName = strippedFileName.Replace("\\", "");
#if UNITY_STANDALONE_LINUX
                                        string[] unparsed = files[i].Split('/');
                                        strippedFileName = unparsed[unparsed.Length - 1];
#endif */

                    if (files[i].Contains(".meta")) continue;
                    if (strippedFileName == "workshopiteminfo.xml") continue;
                    if (strippedFileName == "description.txt") continue;
                    if (strippedFileName == mdp.modName.ToLowerInvariant() + ".png") continue;
                    if (strippedFileName == fileMasterDef.ToLowerInvariant()) continue;
                    if (dictPlayerModFilesToTypes.ContainsKey(strippedFileName))
                    {
                        if (Debug.isDebugBuild && doDebug)Debug.Log("Parse " + files[i] + " as type " + dictPlayerModFilesToTypes[strippedFileName]);
                    }
                    else
                    {
#if !UNITY_EDITOR
                        if (Debug.isDebugBuild) Debug.Log("No definition for " + files[i] + ", NOT processing modfile.");
#endif
                        continue;
                    }

                    PlayerModfileTypes fileType = dictPlayerModFilesToTypes[strippedFileName];

                    if (!mdp.dictPlayerModfiles.ContainsKey(dictPlayerModFilesToTypes[strippedFileName]))
                    {
                        mdp.dictPlayerModfiles.Add(fileType, new List<string>());
                    }

                    if (fileType == PlayerModfileTypes.SPRITESHEET)
                    {
                        spritesheetsToProcessInDirectory.Add(files[i]);
                    }

                    if (fileType == PlayerModfileTypes.GAMEBALANCE)
                    {
                        if (!TryParseGameBalanceFile(files[i], mdp))
                        {
                            continue;
                        }
                    }

                    if (fileType == PlayerModfileTypes.SPRITESHEET_META)
                    {
                        if (!TryParseSpritesheetMeta(files[i], mdp))
                        {
                            continue;
                        }
                    }

                    if (fileType == PlayerModfileTypes.SPRITE || fileType == PlayerModfileTypes.JOBPORTRAIT)
                    {
                        if (!TryParseSpriteOrPortrait(files[i], mdp, strippedFileName))
                        {
                            continue;
                        }
                    }

                    mdp.dictPlayerModfiles[fileType].Add(files[i]);
                    if (Debug.isDebugBuild && doDebug) Debug.Log("Loaded mod file " + files[i] + " for mod " + mdp.modName + " from folder " + directories[x]);
                }

                // Match up spritesheet files with spritesheet meta data, which hopefully exists.
                foreach(string sheetPath in spritesheetsToProcessInDirectory)
                {
                    string strippedFileName = GetLocalFilename(sheetPath, directories[x]).ToLowerInvariant();
                    bool foundSheet = false;
                    foreach (PlayerMods_Spritesheet pmSheet in mdp.dictSpriteSheets.Values)
                    {
                        if (strippedFileName.Contains(pmSheet.spritesheetName.ToLowerInvariant()))
                        {
                            foundSheet = true;
                            bool success = true;
                            try { pmSheet.ChopUpSpritesheetFromPath(sheetPath); }
                            catch (Exception e)
                            {
                                if (Debug.isDebugBuild) Debug.Log("Failed chopping up spritesheet " + strippedFileName + " due to " + e);
                                success = false;
                            }

                            if (success)
                            {
                                try { pmSheet.ConnectChoppedSpritesToDict(); }
                                catch(Exception e)
                                {
                                    if (Debug.isDebugBuild) Debug.Log("Failed to connect chopped sprites to dict in " + strippedFileName + " due to " + e);
                                    success = false;
                                }
                                if (success)
                                {
                                    if (!string.IsNullOrEmpty(pmSheet.replaceRef))
                                    {
                                        spriteReferences.Add(pmSheet.replaceRef); // Keep track of ALL sprite references across all mods
                                    }                                    
                                    // Saves time when checking if a replacement is needed later                                    
                                }
                                /* if (success)
                                {
                                    Debug.Log("Chopped up spritesheet " + strippedFileName);
                                } */
                                
                            }
                        }
                    }
                    if (!foundSheet)
                    {
                        if (Debug.isDebugBuild) Debug.Log("Could not find metadata for " + sheetPath);
                    }
                }

                loadedPlayerMods.Add(mdp);
            }
        }


        initialized = true;
    }

    public static void AdjustGameBalanceFromModFiles()
    {
        if (!PlatformVariables.ALLOW_PLAYER_MODS) return;
        foreach(ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            for (int i = 0; i < (int)BalanceAdjustments.COUNT; i++)
            {
                SetBalanceAdjustment((BalanceAdjustments)i, mdp.modBalance[i]);
                //Debug.LogError("Loaded mod " + mdp.modName + " has adjustment for " + (BalanceAdjustments)i + " of " + mdp.modBalance[i]);
            }
        }
        // also from misc settings!
        for (int i = 0; i < (int)BalanceAdjustments.COUNT; i++)
        {
            SetBalanceAdjustment((BalanceAdjustments)i, PlayerOptions.miscSettingsModBalance[i]);
        }
    }

    public static void PrintAnyModLogData()
    {
        for (int i = 0; i < modBalanceAdjustments.Length; i++)
        {
            if (modBalanceAdjustments[i] != 1.0f)
            {
                string refName = "modbal_" + ((BalanceAdjustments)i).ToString().ToLowerInvariant();
                string colorTag = UIManagerScript.greenHexColor;
                if (modBalanceAdjustments[i] < 1f)
                {
                    colorTag = UIManagerScript.redHexColor;
                }
                string printString = UIManagerScript.cyanHexColor + StringManager.GetString(refName) + "</color>: " + colorTag + (modBalanceAdjustments[i] * 100f) + "%</color>";
                GameLogScript.GameLogWrite(printString, null);
            }
        }
    }

    public void OpenApplicationDataPath()
    {
        Application.OpenURL(modUploadDataPath);
    }

    public void OpenModDownloadDataPath()
    {
        if (Directory.Exists(modDownloadDataPath))
        {
            Application.OpenURL(modDownloadDataPath);
        }        
    }

    public void OpenModInstructionsURL()
    {
        Application.OpenURL(PLAYER_MOD_HELP_URL);
    }

    static Sprite ImportSprite(string path)
    {
        Sprite spr = null;

        byte[] getImageBytes = System.IO.File.ReadAllBytes(path);
        Texture2D basicTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        basicTexture.filterMode = FilterMode.Point;
        basicTexture.LoadImage(getImageBytes);
        spr = Sprite.Create(basicTexture, new Rect(0f, 0f, basicTexture.width, basicTexture.height), new Vector2(0.5f, 0.5f), 32f);
        
        return spr;
    }

    public static Sprite TryGetPortraitSpriteFromMods(CharacterJobs job)
    {
        if (!initialized) return null;

        Sprite retSprite = null;
        foreach (ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            List<string> files;
            if (mdp.dictPlayerModfiles.TryGetValue(PlayerModfileTypes.JOBPORTRAIT, out files))
            {
                string jobAsLower = job.ToString().ToLowerInvariant();
                foreach(string portraitFName in files)
                {
                    string converted = portraitFName.ToLowerInvariant();
                    converted = converted.Replace(" ", "");
                    if (converted.Contains(jobAsLower))
                    {
                        string stripped = portraitFName.Replace(mdp.modBaseDataPath, "");
                        stripped = stripped.Replace("\\", "");
                        stripped = stripped.Replace("//", "");                        
                        stripped = stripped.Replace(mdp.modName, "");
                        stripped = stripped.ToLowerInvariant();
                        stripped = stripped.Replace(".png", "");
                        if (mdp.dictSprites.TryGetValue(stripped, out retSprite))
                        {
                            return retSprite;
                        }
                    }
                }
            }
        }
        return null;
    }

    public static Sprite TryGetSpriteFromMods(string spriteRef)
    {
        if (!initialized) return null;
        Sprite retSprite = null;
        foreach(ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            if (mdp.dictSprites.TryGetValue(spriteRef, out retSprite))
            {
                return retSprite;
            }
        }
        return null;
    }

    public static string GetPlayerModStringForSerialization()
    {
        if (!initialized) return "";
        string playerModBuilder = "";
        bool anyPlayerModsRead = false;
        foreach (ModDataPack mdp in PlayerModManager.loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            if (anyPlayerModsRead)
            {
                playerModBuilder += "||";
            }
            playerModBuilder += mdp.modName;
            anyPlayerModsRead = true;
        }
        return playerModBuilder;
    }

    public static void ParseSavedPlayerModsIntoList(List<string> stringList, string unparsedModData)
    {
        if (!initialized) return;
        if (stringList == null)
        {
            stringList = new List<string>();
        }
        string unparsedMods = unparsedModData;
        unparsedMods = unparsedMods.Replace("||", "|");
        string[] parsedMods = unparsedMods.Split('|');
        for (int i = 0; i < parsedMods.Length; i++)
        {
            stringList.Add(parsedMods[i]);
        }
    }

    public static string GetModsNotFound(List<string> compareModList)
    {
        if (!initialized) return "";
        string missingMods = "";
        bool anyMismatch;
        bool first = true;
        foreach (string prevMod in compareModList)
        {
            if (!IsModLoadedAndActive(prevMod))
            {
                anyMismatch = true; // UH OH! Even one mismatch could be very bad.
                if (!first)
                {
                    missingMods += ", ";
                }
                missingMods += prevMod;
                first = false;
            }
        }

        return missingMods;
    }

    public static void CheckForModMismatchOnLoadAndWarnPlayer()
    {
        if (!initialized) return;
        if (GameMasterScript.heroPCActor.playerModsSavedLast.Count == 0 && MetaProgressScript.playerModsSavedLastInMeta.Count == 0)
        {
            // Didn't save any mods. We're fine.
            return;
        }

        // Combine mods saved last in player and meta file, then check to see if each mod is CURRENTLY loaded & enabled.
        List<string> previouslyLoadedModList = new List<string>();
        foreach(string prevMod in GameMasterScript.heroPCActor.playerModsSavedLast)
        {
            previouslyLoadedModList.Add(prevMod);
        }
        foreach (string prevMod in MetaProgressScript.playerModsSavedLastInMeta)
        {
            previouslyLoadedModList.Add(prevMod);
        }
        previouslyLoadedModList = previouslyLoadedModList.Distinct().ToList();

        string missingMods = GetModsNotFound(previouslyLoadedModList);

        if (missingMods == "")
        {
            return;
        }

        // Warn the player.
        StringManager.SetTag(0, missingMods);
        string modMissing = StringManager.GetString("ui_misc_modmissing_warning");
        StringManager.SetTag(1, modMissing);
        UIManagerScript.StartConversationByRef("dialog_playermods_disabled_warning", DialogType.KEYSTORY, null);
    }

    static bool IsModLoadedAndActive(string modRefName)
    {
        if (!initialized) return false;
        foreach (ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            if (mdp.modName == modRefName)
            {
                return true;
            }
        }

        return false;
    }

    public static ModDataPack GetPlayerModFromRef(string refName, bool printError = true)
    {
        if (!initialized) return null;
        foreach (ModDataPack mdp in loadedPlayerMods)
        {
            if (mdp.modName.ToLowerInvariant() == refName.ToLowerInvariant())
            {
                return mdp;
            }
        }
        if (printError) Debug.Log("MOD NOT FOUND: " + refName);
        return null;
    }

    public void EnableModBrowser()
    {
        SyncModEnableStateFromOptions();
        modBrowser.gameObject.SetActive(true);
    }

    public void DisableModBrowser()
    {
        modBrowser.gameObject.SetActive(false);
        PlayerOptions.WriteOptionsToFile(); // Mod state may have changed.
    }

    IEnumerator WaitThenTrySyncModStates()
    {
        yield return new WaitForSeconds(0.1f);
        SyncModEnableStateFromOptions();
    }

    public static void SyncModEnableStateFromOptions()
    {
        if (!initialized || PlayerOptions.playerModsEnabled == null)
        {
            if (singleton.attemptsToSyncModStates > 50)
            {
                return;
            }
            singleton.attemptsToSyncModStates++;
            singleton.StartCoroutine(singleton.WaitThenTrySyncModStates());
            return;
        }

        List<string> modsToRemoveFromOptions = new List<string>();
        foreach(string enableMod in PlayerOptions.playerModsEnabled)
        {
            ModDataPack mod = GetPlayerModFromRef(enableMod);
            if (mod == null)
            {
                modsToRemoveFromOptions.Add(enableMod);
                continue;
            }
            mod.enabled = true;
        }
        foreach(string modRemove in modsToRemoveFromOptions)
        {
            PlayerOptions.playerModsEnabled.Remove(modRemove);
        }
    }

    public void TryEnableWorkshopArea()
    {
        // Mod features are PC only for now
#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            modAreaCG.gameObject.SetActive(true);
            modAreaCG.alpha = 1f;
            modWorkshopButton.SetActive(true);
            workshopButtonsExpanded = false;
            for (int i = 0; i < subWorkshopButtons.Length; i++)
            {
                subWorkshopButtons[i].SetActive(false);
            }
        }
#endif
    }

    public void ToggleModBrowser()
    {
        if (uMyGUI_PopupManager.Instance.IsPopupShown)
        {
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_browse");
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_upload");
        }
        modBrowser.gameObject.SetActive(!modBrowser.gameObject.activeSelf);
        if (modBrowser.gameObject.activeSelf)
        {
            UIManagerScript.PlayCursorSound("Select");
        }
        else
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
    }

    public void ToggleSteamWorkshopBrowser()
    {
        if (!SteamManager.Initialized) return;
        modBrowser.gameObject.SetActive(false);
        if (uMyGUI_PopupManager.Instance.IsPopupShown)
        {
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_browse");
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_upload");
            UIManagerScript.PlayCursorSound("UITock");
        }
        else
        {
            uMyGUI_PopupManager.Instance.ShowPopup("steam_ugc_browse");
            UIManagerScript.PlayCursorSound("Select");
        }
    }

    public void ToggleSteamWorkshopUploader()
    {
        if (!SteamManager.Initialized) return;
        modBrowser.gameObject.SetActive(false);
        if (uMyGUI_PopupManager.Instance.IsPopupShown)
        {
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_upload");
            uMyGUI_PopupManager.Instance.HidePopup("steam_ugc_browse");
            UIManagerScript.PlayCursorSound("UITock");
        }
        else
        {
            uMyGUI_PopupManager.Instance.ShowPopup("steam_ugc_upload");
            UIManagerScript.PlayCursorSound("Select");
        }
    }

    public bool AnyModUIOpen()
    {
        if (!PlatformVariables.ALLOW_PLAYER_MODS) return false;
        if (modBrowser.gameObject.activeSelf) return true;
        if (uMyGUI_PopupManager.Instance.IsPopupShown) return true;
        return false;
    }

    public void DisableWorkshopArea()
    {
        modAreaCG.gameObject.SetActive(false);
    }

    public void CollapseOrExpandWorkshopButtons()
    {
        if (!initialized) return;
        workshopButtonsExpanded = !workshopButtonsExpanded;
        modInstallerButton.SetActive(workshopButtonsExpanded);
        if (SteamManager.Initialized)
        {
            modDownloaderButton.SetActive(workshopButtonsExpanded);
            modUploaderButton.SetActive(workshopButtonsExpanded);
        }

        if (workshopButtonsExpanded)
        {
            UIManagerScript.PlayCursorSound("UITick");
        }
        else
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
    }

    public static string GetModNameFromWorkshopFile(string xmlPath)
    {        
        string textFromFile = File.ReadAllText(xmlPath);
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        if (string.IsNullOrEmpty(textFromFile))
        {
            Debug.Log("File at " + xmlPath + " not found or empty.");
            return ""; 
        }
        XmlReader reader = XmlReader.Create(new StringReader(textFromFile), settings);
        using (reader)
        {
            bool finished = false;
            int readAttempts = 0;
            while (!finished)
            {
                readAttempts++;
                if (readAttempts > 1500) // Something definitely went wrong.
                {
                    Debug.Log("Failure to parse file at " + xmlPath + " due to infinite read loop.");
                    return "";
                }
                switch (reader.Name.ToLowerInvariant())
                {
                    case "name":
                        return reader.ReadElementContentAsString();
                    default:
                        reader.Read();
                        break;
                }
                finished = reader.NodeType == XmlNodeType.EndElement && reader.Name.ToLowerInvariant() == "document";
            }
        }

        Debug.Log("Couldn't find mod name from workshop file at " + xmlPath);
        return "";
    }

    static bool ReplaceAnimationSpritesInObject(GameObject go, ModDataPack mdp, PlayerMods_Spritesheet playerSheet)
    {
        if (!initialized) return false;
        // Do we have all the necessary animation frames?
        // Well, let's try it.
        Animatable anim = go.GetComponent<Animatable>();
        bool hasAllFrames = true;

        foreach (zirconAnim za in anim.myAnimations)
        {
            if (!playerSheet.HasAnimData(za.animName))
            {
                hasAllFrames = false;
                if (!playerSheet.isPartialSheet)
                {
                    Debug.Log("WARNING: " + mdp.modName + " " + playerSheet.fileName + " is missing frame " + za.animName);
                    break;
                }
                    
            }
        }
        if (!hasAllFrames && !playerSheet.isPartialSheet)
        {
            return false;
        }

        List<float> existingFrameTimes = new List<float>();

        // OK, now to replace the sprites
        foreach (zirconAnim za in anim.myAnimations)
        {            
            TDPlayerAnimData replacementAnimData = playerSheet.GetAnimData(za.animName);
            if (replacementAnimData == null) continue;
            zirconAnim origFrame = za;

            existingFrameTimes.Clear();
            foreach (zirconAnim.AnimationFrameData afd in origFrame.mySprites)
            {
                existingFrameTimes.Add(afd.spriteTime);
            }

            //Debug.Log("CLEARING ANIMATION " + origFrame.animName);
            za.mySprites.Clear();
            int replaceIndex = 0;
            foreach (TDPlayerAnimData.FrameData newFrameData in replacementAnimData.frames)
            {                
                zirconAnim.AnimationFrameData newZAFD = new zirconAnim.AnimationFrameData();
                //Debug.Log("Replacing  " + origFrame.animName + " index " + replaceIndex + " with " + newFrameData.spriteIndexInSheet + " out of total anim frames: " + replacementAnimData.frames.Count);
                newZAFD.mySprite = newFrameData.spr;
                newZAFD.opacity = 1f;
                newZAFD.scale = 1f;

                if (newFrameData.frameTime > 0)
                {
                    newZAFD.spriteTime = newFrameData.frameTime;
                }
                else
                {
                    if (replaceIndex < existingFrameTimes.Count)
                    {
                        newZAFD.spriteTime = existingFrameTimes[replaceIndex];
                    }
                    else
                    {
                        newZAFD.spriteTime = 0.1f;
                    }                    
                }
                
                za.mySprites.Add(newZAFD);
                replaceIndex++;
            }
        }
        return true;
    }

    public static void TryReplaceMonsterOrObjectOrNPCSprites(string mRef, GameObject go, SpriteReplaceTypes aType)
    {
        if (!initialized) return;
        if (!spriteReferences.Contains(mRef))
        {
            return;
        }
        if (string.IsNullOrEmpty(mRef)) return;
        if (go == null) return;        
        foreach (ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            foreach (PlayerMods_Spritesheet playerSheet in mdp.dictSpriteSheets.Values)
            {
                if (playerSheet.replaceRef == mRef)
                {
                    ReplaceAnimationSpritesInObject(go, mdp, playerSheet);
                    if (!playerSheet.isPartialSheet)
                    {
                        return;
                    }                    
                }
                /* if (playerSheet.replaceRef == mRef && aType == SpriteReplaceTypes.NPC)
                {
                    ReplaceAnimationSpritesInObject(go, mdp, playerSheet);
                    if (!playerSheet.isPartialSheet)
                    {
                        return;
                    }                        
                }
                if (playerSheet.replaceRef == mRef && aType == SpriteReplaceTypes.DESTRUCTIBLE)
                {
                    ReplaceAnimationSpritesInObject(go, mdp, playerSheet);
                    if (!playerSheet.isPartialSheet)
                    {
                        return;
                    }
                }
                if (playerSheet.replaceRef == mRef && aType == SpriteReplaceTypes.BATTLEFX)
                {
                    ReplaceAnimationSpritesInObject(go, mdp, playerSheet);
                    if (!playerSheet.isPartialSheet)
                    {
                        return;
                    }
                } */
            }
        }
    }

    public static void TryReplaceJobSprites(CharacterJobs cj, GameObject go)
    {
        if (!initialized) return;
        if (go == null) return;
        foreach(ModDataPack mdp in loadedPlayerMods)
        {
            if (!mdp.enabled) continue;
            foreach(PlayerMods_Spritesheet playerSheet in mdp.dictSpriteSheets.Values)
            {
                if (playerSheet.replaceJob == cj)
                {
                    ReplaceAnimationSpritesInObject(go, mdp, playerSheet);
                    if (!playerSheet.isPartialSheet)
                    {
                        return;
                    }                    
                }
            }
        }
    }

    public static ulong TryGetPublishedFileId(string textFromFile)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        if (string.IsNullOrEmpty(textFromFile))
        {
            return 0;
        }

        XmlReader reader = XmlReader.Create(new StringReader(textFromFile), settings);

        reader.ReadStartElement();
        int attempts = 0;
        while (true)
        {
            switch(reader.Name.ToLowerInvariant())
            {
                case "publishedfileid":
                    return (ulong)reader.ReadElementContentAsLong();
            }
            if (reader.Name.ToLowerInvariant() == "document" && reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }
            attempts++;
            if (attempts > 800)
            {
                Debug.Log(reader.Name + " " + reader.NodeType + " position during XML mod upload, breaking.");
                return 0;
            }
        }

        return 0;
    }

    static bool TryParseSpritesheetMeta(string fileToParse, ModDataPack mdp)
    {
        PlayerMods_Spritesheet pmSheet = new PlayerMods_Spritesheet();
        bool success = true;
        try { success = pmSheet.ReadFromXml(fileToParse); }
        catch (Exception e)
        {
            Debug.LogError("Failed to read spritesheet " + fileToParse + ", skipping due to exception " + e);
            return false;
        }
        if (!success)
        {
            Debug.LogError("Failed to read spritesheet " + fileToParse + ", skipping.");
            return false;
        }
        if (mdp.dictSpriteSheets.ContainsKey(pmSheet.refName))
        {
            Debug.Log(mdp.modName + " already has a sheet called " + pmSheet.refName + ", not adding.");
            return false;
        }
        mdp.dictSpriteSheets.Add(pmSheet.refName, pmSheet);
        return true;
    }

    static bool TryParseSpriteOrPortrait(string fileToParse, ModDataPack mdp, string strippedFileName)
    {
        Sprite spr = null;
        try { spr = ImportSprite(fileToParse); }
        catch (Exception e)
        {
            Debug.Log("Failed to load resource of intended sprite file: " + fileToParse);
        }
        if (spr == null)
        {
            return false;
        }

        string actualSpriteRef = strippedFileName.Replace(".png", "");

        if (mdp.dictSprites.ContainsKey(actualSpriteRef))
        {
            Debug.Log("Sprite dict already contains a file named " + actualSpriteRef);
            return false;
        }
        //Debug.Log("Adding " + actualSpriteRef + " to player sprite dict.");
        mdp.dictSprites.Add(actualSpriteRef, spr);
        return true;
    }

    static bool TryParseGameBalanceFile(string fileToParse, ModDataPack mdp)
    {
        //Debug.LogError("Attemping to parse game balance file: " + fileToParse + " part of " + mdp.modName);

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        string fText = File.ReadAllText(fileToParse);

        if (string.IsNullOrEmpty(fText))
        {
            Debug.Log("No text in " + fText);
            return false;
        }

        XmlReader reader = XmlReader.Create(new StringReader(fText), settings);

        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name.ToLowerInvariant())
            {
                case "sharaonly":
                case "shara_only":
                    mdp.sharaOnly = true;
                    break;
                case "miraionly":
                case "mirai_only":
                    mdp.miraiOnly = true;
                    break;
                case "monsterdensity":
                    mdp.modBalance[(int)BalanceAdjustments.MONSTER_DENSITY] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "herodmg":
                    mdp.modBalance[(int)BalanceAdjustments.HERO_DMG] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "enemydmg":
                    mdp.modBalance[(int)BalanceAdjustments.ENEMY_DMG] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "petdmg":
                    mdp.modBalance[(int)BalanceAdjustments.PET_DMG] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "xpgain":
                    mdp.modBalance[(int)BalanceAdjustments.XP_GAIN] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "jpgain":
                    mdp.modBalance[(int)BalanceAdjustments.JP_GAIN] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "goldgain":
                    mdp.modBalance[(int)BalanceAdjustments.GOLD_GAIN] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "lootrate":
                    mdp.modBalance[(int)BalanceAdjustments.LOOT_RATE] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "magicitemchance":
                    mdp.modBalance[(int)BalanceAdjustments.MAGIC_ITEM_CHANCE] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "monsterspawnrate":
                case "spawnrate":
                    mdp.modBalance[(int)BalanceAdjustments.MONSTER_SPAWN_RATE] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "petxp":
                    mdp.modBalance[(int)BalanceAdjustments.PET_XP] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "poweruphealing":
                    mdp.modBalance[(int)BalanceAdjustments.POWERUP_HEALING] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "powerupdrop":
                case "powerupdroprate":
                    mdp.modBalance[(int)BalanceAdjustments.POWERUP_RATE] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "orbdroprate":
                    mdp.modBalance[(int)BalanceAdjustments.ORB_DROP_RATE] = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        return true;
    }

    public static void MyRandomFireScript(Fighter target)
    {   
        // set a base damage value
        float damage = UnityEngine.Random.Range(1, 1000f);
        damage *= (CombatManagerScript.bufferedCombatData.attacker.myStats.GetCurStat(StatTypes.STAMINA) / 100f);

        // are there any weird objects in the tile?
        MapTileData defTile = MapMasterScript.activeMap.GetTile(target.GetPos());
        foreach(Actor act in defTile.GetAllActors())
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            Destructible dt = act as Destructible;
            if (dt.runEffectOnLastTurn || dt.destroyOnStep)
            {
                damage *= 5f;
            }
        }

        target.TakeDamage(damage, DamageTypes.FIRE);
    }

    static string GetLocalFilename(string fullPath, string directoryPath)
    {
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        string[] unparsed = fullPath.Split('/');
        return unparsed[unparsed.Length - 1];
#else
        string localFileName = fullPath.Replace(directoryPath, "");
        localFileName = localFileName.Replace("\\", "");
        return localFileName;
#endif

    }
}
