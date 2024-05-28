using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum GameDataPaths {
    JOBS, 
    ABILITIES, // Includes both player and monster abilities
    STATUSEFFECTS,  // Includes ground (tile based) effects as well as normal statuses
    ITEMS, 
    MAGICMODS,
    MONSTERS, // Includes champion and family data
    NPCS, // Includes shop data
    MAPGENERATION, // Definition for dungeon floors
    LOOTTABLES,
    SPAWNTABLES,
    DIALOGS, // All dialogs including hint popups
    DUNGEONROOMS, // Room templates and special area templates
    MAPOBJECTS, // Destructibles, summonable objects, floor triggers
    SHOPS,
    COUNT
}

public class DataLoadHelper
{
    // Resource loading paths. These are all subfolders of Resources.
    static string[] resourcePaths;

    static string dlcPath;
    static bool initialized;

    public static void InitializeResourcePaths()
    {
        if (initialized) return;

        // Should this be different per platform?
        resourcePaths = new string[(int)GameDataPaths.COUNT];
        resourcePaths[(int)GameDataPaths.JOBS] = "Jobs/XML";
        resourcePaths[(int)GameDataPaths.ABILITIES] = "Abilities/XML";
        resourcePaths[(int)GameDataPaths.STATUSEFFECTS] = "StatusEffects/XML";
        resourcePaths[(int)GameDataPaths.MAPOBJECTS] = "MapObjects/XML";
        resourcePaths[(int)GameDataPaths.DUNGEONROOMS] = "DungeonRooms/XML";
        resourcePaths[(int)GameDataPaths.MAPGENERATION] = "DungeonGenerator/XML";
        resourcePaths[(int)GameDataPaths.DIALOGS] = "Dialogs/XML";
        resourcePaths[(int)GameDataPaths.NPCS] = "NPCs/XML";
        resourcePaths[(int)GameDataPaths.SHOPS] = "NPCs/Shops";
        resourcePaths[(int)GameDataPaths.ITEMS] = "Items/XML";
        resourcePaths[(int)GameDataPaths.SPAWNTABLES] = "SpawnTables/XML";
        resourcePaths[(int)GameDataPaths.LOOTTABLES] = "LootTables/XML";
        resourcePaths[(int)GameDataPaths.MAGICMODS] = "MagicMods/XML";

        dlcPath = "DLCResources";

        initialized = true;
    }

    // Returns the TEXT contained in each file for ez. parsing
    public static List<string> GetAllFilesToLoad(GameDataPaths dataType, string onlyIncludeFilesContainingString = "")
    {
        List<string> pathsToLoad = new List<string>();

        // Always load our core game data from appropriate directories in EDITOR mode
        // In release mode, we have to use the Resources.LoadAll method below.
#if UNITY_EDITOR
        pathsToLoad.Add(Application.dataPath + "/Resources/" + resourcePaths[(int)dataType]);
#endif
        
        // Don't even bother to look at expansion files if we're not allowed to use them

        
#if UNITY_EDITOR
            pathsToLoad.Add(Application.dataPath + "/Resources/" + dlcPath + "/DLC1/" + resourcePaths[(int)dataType]);
#endif
         
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
#if UNITY_EDITOR
            pathsToLoad.Add(Application.dataPath + "/Resources/" + dlcPath + "/DLC2/" + resourcePaths[(int)dataType]);
#endif
        }

        List<string> filesToLoad = new List<string>();

        // In Release mode we can't load our CORE game resources from a directory, since they're packed in a single file.
        // But we can pull a list of textassets and then grab the text from within. The sub-paths ("Jobs/XML") are the same.
#if !UNITY_EDITOR
        var textAssets = Resources.LoadAll(resourcePaths[(int)dataType], typeof(TextAsset));
#endif  

        bool allowCertainFileTypes = dataType == GameDataPaths.ABILITIES || dataType == GameDataPaths.MONSTERS
            || dataType == GameDataPaths.STATUSEFFECTS || dataType == GameDataPaths.MAGICMODS || dataType == GameDataPaths.JOBS 
            || dataType == GameDataPaths.ITEMS;

        EDLCPackages packageToAllow = EDLCPackages.COUNT;

        if (allowCertainFileTypes)
        {
            packageToAllow = EDLCPackages.EXPANSION1;
            //Debug.Log("Allow " + dataType + " files as if we owned Expansion 1. " + packageToAllow);
        }

        foreach (string directory in pathsToLoad)
        {            
            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFiles(directory);
                for (int i = 0; i < files.Length; i++)
                {
                    if (!files[i].Contains(".xml") || files[i].Contains(".meta")) continue;
                    string fileName = Path.GetFileName(files[i]);

                    //Debug.Log("Checking " + fileName + ", what is package? " + packageToAllow + " " + dataType);

                    if (!DLCManager.IsFileValidToLoad(fileName, packageToAllow))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(onlyIncludeFilesContainingString) && !fileName.Contains(onlyIncludeFilesContainingString))
                    {
                        if (Debug.isDebugBuild) Debug.Log("Skipping " + fileName + " because it doesn't include " + onlyIncludeFilesContainingString);
                    }
                    string fileText = File.ReadAllText(files[i]);
                    filesToLoad.Add(fileText);                    
                }
            }
        }

        // Now we iterate through the loaded text assets for our CORE resources that were packed in a single file.
        // ORDER MATTERS. We need the CORE game files loaded first!
#if !UNITY_EDITOR
        foreach (var t in textAssets)
        {
            TextAsset asset = t as TextAsset;
            if (!DLCManager.IsFileValidToLoad(asset.name, packageToAllow))
            {
                //if (Debug.isDebugBuild) Debug.Log("Not valid to load " + asset.name + " " + dataType + " " + packageToAllow);
                continue;
            }
            if (!string.IsNullOrEmpty(onlyIncludeFilesContainingString) && !asset.name.Contains(onlyIncludeFilesContainingString))
            {
                //Debug.Log("Skipping " + asset.name + " because it doesn't include " + onlyIncludeFilesContainingString);
            }
            filesToLoad.Add(asset.text);  
        }     


        {
            var dlcTextAssets = Resources.LoadAll(dlcPath + "/DLC1/" + resourcePaths[(int)dataType], typeof(TextAsset));    
            // I know I'm repeating code here but I'm not sure how to concatenate dlcTextAssets and textAssets
            foreach(var t in dlcTextAssets) 
            {
                TextAsset asset = t as TextAsset;
                if (!DLCManager.IsFileValidToLoad(asset.name, packageToAllow))
                {
                    //if (Debug.isDebugBuild) Debug.Log("Not valid to load " + asset.name + " " + dataType + " " + packageToAllow);
                    continue;
                }
                if (!string.IsNullOrEmpty(onlyIncludeFilesContainingString) && !asset.name.Contains(onlyIncludeFilesContainingString))
                {
                    //Debug.Log("Skipping " + asset.name + " because it doesn't include " + onlyIncludeFilesContainingString);
                }
                filesToLoad.Add(asset.text);
            }
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) 
        {
            var dlcTextAssets = Resources.LoadAll(dlcPath + "/DLC2/" + resourcePaths[(int)dataType], typeof(TextAsset));    
            // I know I'm repeating code here but I'm still not sure how to concatenate dlcTextAssets and textAssets
            foreach(var t in dlcTextAssets) 
            {
                TextAsset asset = t as TextAsset;
                if (!DLCManager.IsFileValidToLoad(asset.name))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(onlyIncludeFilesContainingString) && !asset.name.Contains(onlyIncludeFilesContainingString))
                {
                    Debug.Log("Skipping " + asset.name + " because it doesn't include " + onlyIncludeFilesContainingString);
                }
                filesToLoad.Add(asset.text);
            }
        }
  
#endif
        return filesToLoad;
    }

    // #todo - figure out how to write below
    /* List<string> ParseAssetsForFilesToLoad(List<TextAsset> assets)
    {
        var dlcTextAssets = Resources.LoadAll(dlcPath + "/DLC1/" + resourcePaths[(int)dataType], typeof(TextAsset));
    } */
}
