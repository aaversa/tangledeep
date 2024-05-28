using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
    using Galaxy.Api;
#endif
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
using Steamworks;
#endif
#if UNITY_PS4
using UnityEngine.PS4;
#endif
#if UNITY_XBOXONE
using Marketplace;
#endif

// This class verifies the state of purchased and installed DLC
// Some DLC-related code may be baked into the game, so we will need a way to bypass it as needed
// Verifying DLC status must be independent of platform (Steam, GOG, etc.)

public enum EDLCPackages { EXPANSION1, EXPANSION2, COUNT }
public enum DLCStatus { NOTCHECKED, INSTALLED, NOTINSTALLED, COUNT }
public enum StoryCampaigns { UNKNOWN, SHARA, MIRAI, COUNT }

// AA sez: I'm going to try to be consistent with the use of static vs. "internal only" methods

[System.Serializable]
public class DLCManager : MonoBehaviour {

    [Header("Debug")]
    public bool considerDLC1Installed;
    public bool considerDLC2Installed;
   
    static DLCManager singleton;
    List<EDLCPackages> dlcInstalled;

    static Dictionary<EDLCPackages, string> prefixesPerDLC;

    // PC only.
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    private static Dictionary<EDLCPackages, AppId_t> steamDLCIds;
#endif

    private static Dictionary<EDLCPackages, ulong> galaxyDLCIds;
        
    public static bool initialized;

    static Dictionary<EDLCPackages, DLCStatus> dlcPackagesInstalled;

    static bool allDLCsOwnedForSomeReason;
	
    static int cachedDLCContentCount;
    static bool checkedDLC;	
	
	
    public static void MarkAllDLCsOwnedForSession()
    {
        allDLCsOwnedForSomeReason = true;
    }

	void Awake () {
        if (initialized || (singleton != null && singleton != this))
        {
			Destroy(gameObject);
            return;
        }

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
		        considerDLC1Installed = true;
#endif

        dlcPackagesInstalled = new Dictionary<EDLCPackages, DLCStatus>();
        for (int i = 0; i < (int)EDLCPackages.COUNT; i++)
        {
            dlcPackagesInstalled.Add((EDLCPackages)i, DLCStatus.NOTCHECKED);
        }

        singleton = this;
        dlcInstalled = new List<EDLCPackages>();
        initialized = true;
        DontDestroyOnLoad(this);

        prefixesPerDLC = new Dictionary<EDLCPackages, string>();
        prefixesPerDLC[EDLCPackages.EXPANSION1] = "exp_";
        prefixesPerDLC[EDLCPackages.EXPANSION2] = "exp2_";
	    	    
	    galaxyDLCIds = new Dictionary<EDLCPackages, ulong>();

        //SteamIDs and Galaxy ProductIDs go here
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        steamDLCIds = new Dictionary<EDLCPackages, AppId_t>();
        steamDLCIds[EDLCPackages.EXPANSION1] = new AppId_t(953080);
	    steamDLCIds[EDLCPackages.EXPANSION2] = new AppId_t(1156710);
#endif

        galaxyDLCIds[EDLCPackages.EXPANSION1] = 1971722134;
	    galaxyDLCIds[EDLCPackages.EXPANSION2] = 0;
	}

    // If we're not initialized, this is always FALSE.
    public static bool CheckDLCInstalled(EDLCPackages dlc)
    {
        bool value= singleton._CheckDLCInstalled(dlc);
        //Debug.Log("Is " + dlc + " installed? " + value);
        return value;
    }

    // Internal methods

    bool _CheckDLCInstalled(EDLCPackages dlc)
    {

        if (!initialized) return false;

        if (allDLCsOwnedForSomeReason) return true;

        // Switch just owns Legend of Shara by default, always
#if UNITY_SWITCH
        if (dlc == EDLCPackages.EXPANSION1) return true;

        if (dlc == EDLCPackages.EXPANSION2 && Debug.isDebugBuild && considerDLC2Installed) return true;

        int count = GetCachedDLCContentCount();

        if (count > 0) dlcPackagesInstalled[dlc] = DLCStatus.INSTALLED;
        else dlcPackagesInstalled[dlc] = DLCStatus.NOTINSTALLED;

        return count > 0;
#endif

#if UNITY_EDITOR
        if (dlc == EDLCPackages.EXPANSION1 && considerDLC1Installed)
        {
            return true;
        }
        else if (dlc == EDLCPackages.EXPANSION1)
        {
            return false;
        }
        if (dlc == EDLCPackages.EXPANSION2 && considerDLC2Installed)
        {
            return true;
        }
        else if (dlc == EDLCPackages.EXPANSION2)
        {
            return false;
        }
#endif

        if (dlcPackagesInstalled[dlc] != DLCStatus.NOTCHECKED)
        {
            return dlcPackagesInstalled[dlc] == DLCStatus.INSTALLED;
        }

        var newState = DLCStatus.NOTINSTALLED;
        try
        {
            //Steam version
#if !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE

            if (SteamManager.Initialized)
            {
                // In case of fail, uncomment these and check to make sure your appid is not 480!
                //var owner = SteamApps.GetAppOwner();
                //var appid = SteamUtils.GetAppID();
                Debug.Log("Check SteamApps install state for " + dlc + ": " + SteamApps.BIsDlcInstalled(steamDLCIds[dlc]));
                
                if (SteamApps.BIsDlcInstalled(steamDLCIds[dlc]))
                {
                    newState = DLCStatus.INSTALLED;
                }
            }
            else
#endif
            {

                Debug.Log("Steam Manager is NOT initialized for DLC check of " + dlc);

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                // Manually check file status.
                List<string> pathsToCheck = new List<string>();

                if (dlc == EDLCPackages.EXPANSION1)
                {
                    pathsToCheck.Add(Application.dataPath + "/los/loschk");
                    pathsToCheck.Add(Application.dataPath + "/dlc/loschk");
                }
                else if (dlc == EDLCPackages.EXPANSION2)
                {
                    pathsToCheck.Add(Application.dataPath + "/dod/dodchk");
                    pathsToCheck.Add(Application.dataPath + "/dlc/dodchk");
                }

                foreach (string path in pathsToCheck)
                {
                    //Debug.Log(path);
                    if (File.Exists(path))
                    {
                        Debug.Log("Manually found dlc at path " + path);
                        newState = DLCStatus.INSTALLED;
                    }
                }        
#endif

                if (newState != DLCStatus.INSTALLED)
                {
#if !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IPHONE && !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE
                    Debug.Log("Test.");
                    if (GogGalaxyManager.IsInitialized())
                    {
                        if (GalaxyInstance.Apps().IsDlcInstalled(galaxyDLCIds[dlc]))
                        {
                            newState = DLCStatus.INSTALLED;
                        }
                    }
#endif
                }
            }
#if UNITY_PS4
            //DLC1 is always installed
            if (dlc == EDLCPackages.EXPANSION1)
            {
                if (considerDLC1Installed)
                {
                    newState = DLCStatus.INSTALLED;
                }               
            }
            //for PS4 we test if we have DLC2 installed
            if (dlc == EDLCPackages.EXPANSION2)
            {
                PS4DRM.DrmContentFinder finder = new PS4DRM.DrmContentFinder();               
                finder.serviceLabel = 0;
                bool found = false;
                if (PS4DRM.ContentFinderOpen(ref finder))
                {
                    found = true;
                    string entitlementLabel = finder.entitlementLabel;                    
                    if (PS4DRM.ContentOpen(entitlementLabel, finder.serviceLabel) == true)
                    {
                        if (entitlementLabel == "DAWNOFDRAGONS000")
                        {
                            newState = DLCStatus.INSTALLED;
                        }
                        else
                        {
                            Debug.LogError("found DLC which isn't Dawn of Dragons, we found:" + entitlementLabel);
                        }
                        

                        PS4DRM.ContentClose(entitlementLabel);
                    }
                    else
                    {
                        OnScreenLog.Add("Can't open entitlement");
                    }
                    PS4DRM.ContentFinderClose(ref finder);
                }
                if (!found)
                {
                    OnScreenLog.Add("No content found");
                }
            }
#endif

#if UNITY_XBOXONE
            DLCManagerXBOXONE.Create();
            DownloadableContentPackageList m_DLC = DLCManagerXBOXONE.FindPackages(InstalledPackagesFilter.AllRelatedPackages);
            //DLC1 is always installed
            if (dlc == EDLCPackages.EXPANSION1)
            {
                if (considerDLC1Installed)
                {
                    newState = DLCStatus.INSTALLED;
                }
            }
            //for XBOXONE we test if we have DLC2 installed
            if (dlc == EDLCPackages.EXPANSION2)
            {
                if (m_DLC.Length > 0)
                {
                    for (int i = 0; i < m_DLC.Length; ++i)
                    {
                        bool mounted = m_DLC[i].IsMounted;
                        bool wasMounted = mounted;

                        bool isDlc = true;
                        string displayName = m_DLC[i].DisplayName;
                        if (displayName == null || displayName == "")
                        {
                            isDlc = false;
                            displayName = "NOT DLC";
                            //Debug.LogError("we found: " + displayName);
                            continue;
                        }

                        if (!mounted)
                        {
                            m_DLC[i].Mount();
                            Debug.LogError(string.Format("> [MOUNTING] [{1}] DLC {0} \n", m_DLC[i].DisplayName, m_DLC[i].IsMounted ? "Ok" : "Failed"));

                            newState = DLCStatus.INSTALLED;
                        }
                        //else
                        //{
                            //m_DLC[i].UnMount();
                            //Debug.LogError(string.Format("> [UNMOUNT]  [{1}] DLC {0}\n", m_DLC[i].DisplayName, m_DLC[i].IsMounted ? "Failed" : "Ok"));
                        //}
                        mounted = m_DLC[i].IsMounted;
                    }
                }
            }

#endif

#if UNITY_ANDROID
            //DLC1 is always installed
            if (dlc == EDLCPackages.EXPANSION1)
            {
                if (considerDLC1Installed)
                {
                    newState = DLCStatus.INSTALLED;
                }
            }
            //DLC2

#endif

        }
        catch ( Exception e)
        {
#if UNITY_EDITOR
                Debug.LogError("Looking for DLC when this happened: " + e);
#endif
            newState = DLCStatus.NOTCHECKED;
        }

        dlcPackagesInstalled[dlc] = newState;

        Debug.Log("State is " + newState + " so it is installed.");

        return dlcPackagesInstalled[dlc] == DLCStatus.INSTALLED;
    }

    public static bool IsFileValidToLoad(string fileName, EDLCPackages allowEvenIfWeDontOwnThis = EDLCPackages.COUNT)
    {
        return singleton._IsFileValidToLoad(fileName, allowEvenIfWeDontOwnThis);
    }

    bool _IsFileValidToLoad(string fileName, EDLCPackages allowEvenIfWeDontOwnThis = EDLCPackages.COUNT)
    {
        if (!initialized) return true;

        //Debug.Log(CheckDLCInstalled(EDLCPackages.EXPANSION1) + " " + allowEvenIfWeDontOwnThis);

        if (!CheckDLCInstalled(EDLCPackages.EXPANSION1) && allowEvenIfWeDontOwnThis == EDLCPackages.EXPANSION1)
        {
            return true;
        }

        foreach (var kvp in prefixesPerDLC)
        {
            if (fileName.StartsWith(kvp.Value) &&
                !_CheckDLCInstalled(kvp.Key))
            {
#if UNITY_EDITOR
                //Debug.LogError("Cannot load " + fileName + " because " + kvp.Value + " " + kvp.Key);
#endif
                return false;
            }                
        }

        // "multidlc" files contain things used across multiple dlc packages, so
        if (fileName.StartsWith("multidlc") && 
            !_CheckDLCInstalled(EDLCPackages.EXPANSION1) &&
            !_CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
#if UNITY_EDITOR
            //Debug.LogError("Can't load multidlc: " + fileName);
#endif
            return false;
        }

        //Debug.Log("CAN load " + fileName);
        return true;
    }

    // Do DLC specific stuff here
    public static void OnMapChangeOrLoad()
    {
        if (CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
            {
                GameStartData.gameInSharaMode = true;
                GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = true;
            }
            else
            {
                GameStartData.gameInSharaMode = false;
                GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;
            }
            if (GameStartData.gameInSharaMode)
            {
                SharaModeStuff.RefreshSharaAbilityNamesAndDescriptions();
                SharaModeStuff.EnsureConnectionFromCedarToBoss1();
                SharaModeStuff.EnsureConnectionsInPreBoss1Area();
            }

            bool anyNewMapsCreated = false;

            if (MapMasterScript.theDungeon.FindFloor(MapMasterScript.RIVERSTONE_WATERWAY_START) == null || GameMasterScript.gmsSingleton.ReadTempGameData("waterway_load_failure") == 1)
            {
                GameMasterScript.gmsSingleton.SetTempGameData("waterway_load_failure", 0);
                CreateAndConnectDLCMaps(MapMasterScript.RIVERSTONE_WATERWAY_START, MapMasterScript.RIVERSTONE_WATERWAY_END, true, connectToStartFloor: MapMasterScript.TOWN_MAP_FLOOR, connectToEndFloor: MapMasterScript.PREBOSS1_MAP_FLOOR);
                anyNewMapsCreated = true;
            }

            if (!SharaModeStuff.IsSharaModeActive())
            {
                anyNewMapsCreated = VerifyDLCMapStateIsCorrect(MapMasterScript.REALM_OF_GODS_START, MapMasterScript.REALM_OF_GODS_END, MapMasterScript.FINAL_BOSS_FLOOR2);

                if (MapMasterScript.activeMap.floor >= MapMasterScript.REALM_OF_GODS_START && MapMasterScript.activeMap.floor <= MapMasterScript.REALM_OF_GODS_END)
                {
                    FixBadObjectPlacementInRealmOfGods(MapMasterScript.activeMap.floor);
                }

                CreateRealmOfGodsStairsInFinalBossRoomIfAllowed();
            }                        

            if (anyNewMapsCreated)
            {
                MakeDLCStairConnectionsIfNeeded();
            }

            if (MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_FOREST_FLOOR)
            {
                GameEventsAndTriggers.StartSharaIntroScene();
            }
            else if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN_MAP_FLOOR)
            {
                // Add the Wanderer if he isn't there already, as long as Dreamcaster is unlocked
                if (ShouldWandererBeUnlocked() && MapMasterScript.activeMap.FindActor("npc_fisherman") == null)
                {
                    NPC fisherman = NPC.CreateNPC("npc_fisherman");
                    MapMasterScript.activeMap.PlaceActor(fisherman, MapMasterScript.activeMap.GetTile(17, 10));
                    MapMasterScript.singletonMMS.SpawnNPC(fisherman);
                }

                CheckForWaterwayUnlockAndJumpPoint();
            }            
            else if (MapMasterScript.activeMap.floor == 24) // Forest of Dreams
            {
                GameEventsAndTriggers.StartSharaIntroScene();
            }
            else if (MapMasterScript.activeMap.effectiveFloor == 4 && SharaModeStuff.IsSharaModeActive() && MapMasterScript.activeMap.IsMainPath()) // Cedar Caverns 5F pre-boss stuff.
            {
                if (GameMasterScript.heroPCActor.ReadActorData("exp_friendlydirtbeak1_talk") != 1)
                {
                    GameEventsAndTriggers.SpawnNPCDirtbeakNearShara();
                }
            }
            else if (MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_CAMPFIRE_FLOOR)
            {
                GameEventsAndTriggers.CheckForSharaGameStart();
            }
        }

        if (CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            if (!SharaModeStuff.IsSharaModeActive())
            {
                VerifyAllDragonDungeons();
            }            

            if (MapMasterScript.activeMap.IsDragonDungeonMap())
            {
                MapMasterScript.activeMap.CheckForBadLevelScaling();
            }

            if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                // If player has met conditions for the Frog King (crafting box), then make sure he exists on the map.

                if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) >= 1 && MapMasterScript.activeMap.FindActor("npc_babyfrogking") == null)
                {
                    Vector2 pos = new Vector2(15f, 12f);
                    NPC dummy = NPC.CreateNPC("npc_babyfrogking");
                    MapTileData spawnTile = MapMasterScript.GetTile(pos);
                    dummy.SetCurPos(pos);
                    dummy.SetSpawnPos(pos);
                    MapMasterScript.activeMap.PlaceActor(dummy, spawnTile);
                    MapMasterScript.singletonMMS.SpawnNPC(dummy);
                }

            }
        }
    }

    public static void MakeDLCStairConnectionsIfNeeded()
    {
        if (CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            if (!SharaModeStuff.IsSharaModeActive())
            {
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.FROG_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.ROBOT_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.NUM_BEASTDUNGEON_MAPS, EDLCPackages.EXPANSION2);
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);
                DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);
            }
        }

        if (!CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return;
        }

        ConnectRiverstoneWaterwayStairs();

        if (!SharaModeStuff.IsSharaModeActive())
        {
            CreateAndConnectDLCMaps(MapMasterScript.REALM_OF_GODS_START, MapMasterScript.REALM_OF_GODS_END, false, MapMasterScript.FINAL_BOSS_FLOOR2);
        }        
    }
    
    static void ConnectRiverstoneWaterwayStairs()
    {
        // Find our Riverstone Passage start map and remove links to tutorial stairs, replacing with stairs to Riverstone.
        Map altPathZone1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.RIVERSTONE_WATERWAY_START);
        foreach (Stairs st in altPathZone1.mapStairs)
        {
            if (st.NewLocation == null)
            {
                Debug.Log("Stairs on floor " + st.dungeonFloor + " have no location? PTF? " + st.pointsToFloor);
                continue;
            }
            //Debug.Log("check " + st.actorUniqueID + " " + st.GetPos() + " " + st.stairsUp + " " + st.pointsToFloor);
            if (st.NewLocation.floor == MapMasterScript.TUTORIAL_FLOOR)
            {
                st.NewLocation = MapMasterScript.singletonMMS.townMap;
                st.newLocationID = st.NewLocation.mapAreaID;
                st.pointsToFloor = MapMasterScript.TOWN_MAP_FLOOR;
                //also just don't use these stairs
                st.DisableActor();
                break;
            }
        }

        // Now in the Bandit Hideout Passage, check for stairs to Alt Path Zone 5 (floor 354) and if there aren't any...
        // ... make some!

        // If we're not qualified, hide the spawned stairs
        bool enableStairs = ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1;

        if (SharaModeStuff.IsSharaModeActive())
        {
            enableStairs = false;
        }

        Map hideout = MapMasterScript.theDungeon.FindFloor(204);
        Map altPath5 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.RIVERSTONE_WATERWAY_END);
        Stairs pathToDLCZone = null;
        Stairs pathToDungeon = null;
        foreach (Stairs st in hideout.mapStairs)
        {
            if (st.NewLocation == null)
            {
                st.NewLocation = MapMasterScript.theDungeon.FindFloor(st.pointsToFloor);
                continue;
            }
            if (st.NewLocation.floor == MapMasterScript.RIVERSTONE_WATERWAY_END)
            {
                pathToDLCZone = st;
                if (!enableStairs)
                {
                    pathToDLCZone.DisableActor();
                }
            }
            else if (st.NewLocation.floor == 4)
            {
                pathToDungeon = st;
            }
        }
        if (pathToDLCZone == null && !SharaModeStuff.IsSharaModeActive())
        {
            // Gotta make the stairs then.
            MapTileData nearbyTile = hideout.GetRandomEmptyTile(pathToDungeon.GetPos(), 3, false, false, true, false, true);
            while (nearbyTile.GetStairsInTile() != null)
            {
                nearbyTile = hideout.GetRandomEmptyTile(pathToDungeon.GetPos(), 2, false, false, true);
            }
            Stairs st = new Stairs();
            st.prefab = pathToDungeon.prefab;
            st.stairsUp = true;
            st.NewLocation = altPath5;
            st.pointsToFloor = MapMasterScript.RIVERSTONE_WATERWAY_END;
            st.newLocationID = altPath5.mapAreaID;
            pathToDLCZone = st;
            hideout.PlaceActor(pathToDLCZone, nearbyTile);
            if (!enableStairs)
            {
                st.DisableActor();
            }
            if (MapMasterScript.activeMap == hideout)
            {
                MapMasterScript.singletonMMS.SpawnStairs(st);
            }
        }

        // But wait! Does Riverstone Waterway 5F have stairs to the boss? It should, but maybe it doesn't.
        Stairs toBoss = null;
        foreach (Stairs st in altPath5.mapStairs)
        {
            if (st.NewLocation == null)
            {
                continue;
            }
            if (st.NewLocation.floor == 204)
            {
                toBoss = st;
                break;
            }
        }

        // No stairs to hideout (204), so we gotta make em.
        if (toBoss == null)
        {
            MapTileData forStairs = altPath5.GetRandomEmptyTileForMapGen();
            Stairs pathToBossZone = new Stairs();
            pathToBossZone.prefab = "StoneStairsUp";
            pathToBossZone.stairsUp = false;
            pathToBossZone.NewLocation = hideout;
            pathToBossZone.pointsToFloor = 204;
            pathToBossZone.newLocationID = hideout.mapAreaID;
            toBoss = pathToBossZone;
            altPath5.PlaceActor(pathToBossZone, forStairs);
            if (MapMasterScript.activeMap == altPath5)
            {
                MapMasterScript.singletonMMS.SpawnStairs(pathToBossZone);
            }
        }
    }

    public static void OnSwitchToGameplaySceneFromMainMenu()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;

    }

    public static NPC FindCraftingBoxNPC()
    {
        // For now, just use the banker
        NPC banker = MapMasterScript.theDungeon.FindFloor(MapMasterScript.TOWN2_MAP_FLOOR).FindActor("npc_babyfrogking") as NPC;

        return banker;
    }

    public static bool ShouldShowLegendOfSharaTitleScreen()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        int lastPlayed = TDPlayerPrefs.GetInt(GlobalProgressKeys.LAST_PLAYED_CAMPAIGN);
        if (lastPlayed < 0) lastPlayed = 0;
        StoryCampaigns sc = (StoryCampaigns)lastPlayed;
        if (sc == StoryCampaigns.SHARA) return true;

        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return true;

#if !UNITY_SWITCH
        return false;
#else
        // On Switch, which always has Shara installed, we don't want to show it immediately.

        return TDPlayerPrefs.GetInt("show_shara_loading_screen_nexttime") == 1;
#endif
    }

    public static StoryCampaigns GetLastPlayedCampaign()
    {
        int lastPlayed = TDPlayerPrefs.GetInt(GlobalProgressKeys.LAST_PLAYED_CAMPAIGN);
        if (lastPlayed < 0) lastPlayed = 0;
        StoryCampaigns sc = (StoryCampaigns)lastPlayed;
        return sc;
    }

    public static void SetLastPlayedCampaign(StoryCampaigns campaign)
    {
        int iCampaign = (int)campaign;
        TDPlayerPrefs.SetInt(GlobalProgressKeys.LAST_PLAYED_CAMPAIGN, iCampaign);
    }

    /// <summary>
    /// From XML string data, parses a list of all expansions installed as a list of TDExpansion
    /// </summary>
    /// <param name="expansionList"></param>
    /// <param name="data"></param>
    public static void ParseSavedPlayerExpansionsIntoList(List<EDLCPackages> expansionList, string data)
    {
        if (!initialized) return;
        if (expansionList == null)
        {
            expansionList = new List<EDLCPackages>();
        }
        else
        {
            expansionList.Clear();
        }
        string unparsedExpansions = data;
        unparsedExpansions = unparsedExpansions.Replace("||", "|");
        string[] parsedExpansions = unparsedExpansions.Split('|');
        for (int i = 0; i < parsedExpansions.Length; i++)
        {
            EDLCPackages installedExp = (EDLCPackages)Enum.Parse(typeof(EDLCPackages), parsedExpansions[i].ToUpperInvariant()); 
            expansionList.Add(installedExp);
        }
    }

    public static string GetExpansionsStringForSerialization()
    {
        if (!initialized) return "";
        string expansionStringBuilder = "";
        bool anyExpansionsRead = false;
        for (int i = 0; i < (int)EDLCPackages.COUNT; i++)
        {
            if (!CheckDLCInstalled((EDLCPackages)i)) continue;
            if (anyExpansionsRead)
            {
                expansionStringBuilder += "||";
            }
            expansionStringBuilder += ((EDLCPackages)i);
            anyExpansionsRead = true;
        }
        return expansionStringBuilder;
    }

    public static string GetNameOfDLCPackage(EDLCPackages package)
    {
        return StringManager.GetString("dlcname_" + package.ToString().ToLowerInvariant());
    }

    public static string GetDLCNotFound(List<EDLCPackages> compareDLCList)
    {
        if (!initialized) return "";
        string missingDLC = "";
        bool anyMismatch;
        bool first = true;
        foreach (EDLCPackages dlc in compareDLCList)
        {
            if (!CheckDLCInstalled(dlc))
            {
                anyMismatch = true; // UH OH! Even one mismatch could be very bad.
                if (!first)
                {
                    missingDLC += ", ";
                }
                missingDLC += GetNameOfDLCPackage(dlc);
                first = false;
            }
        }

        return missingDLC;
    }

    /// <summary>
    /// Updates a player's save to include new areas and makes stair connections. 
    /// </summary>
    /// <param name="startFloor"></param>
    /// <param name="endFloor"></param>
    /// <param name="connectToStartFloor"></param>
    /// <param name="connectToEndFloor"></param>
    public static void CreateAndConnectDLCMaps(int startFloor, int endFloor, bool createMaps, int connectToStartFloor = -1, int connectToEndFloor = -1)
    {
        for (int i = startFloor; i <= endFloor; i++)
        {
            Map createdMap = null;
            if (createMaps)
            {
                // Get rid of this map if it already exists
                Map existingMap = MapMasterScript.theDungeon.FindFloor(i);
                if (existingMap != null)
                {
                    MapMasterScript.theDungeon.RemoveMapByFloor(i);                    
                    MapMasterScript.dictAllMaps.Remove(existingMap.mapAreaID);
                    MapMasterScript.OnMapRemoved(existingMap);
                }               

                DungeonLevel dl = GameMasterScript.masterDungeonLevelList[i];
                createdMap = MapMasterScript.CreateMap(dl.floor);
                //createdMap = MapMasterScript.dictAllMaps[i];
            }
            else
            {
                createdMap = MapMasterScript.theDungeon.FindFloor(i);
            }
            
            bool hasStairsUp = false;
            bool hasStairsDown = false;
            foreach(Stairs st in createdMap.mapStairs)
            {
                if (st.stairsUp)
                {
                    hasStairsUp = true;
                    if (i == startFloor && connectToStartFloor >= 0)
                    {
                        st.NewLocation = MapMasterScript.theDungeon.FindFloor(connectToStartFloor);
                    }
                }
                if (!st.stairsUp)
                {
                    hasStairsDown = true;
                    if (i == endFloor && connectToEndFloor >= 0)
                    {
                        st.NewLocation = MapMasterScript.theDungeon.FindFloor(connectToEndFloor);
                    }
                }
                    
            }
            if (i != endFloor && !hasStairsDown && 
                !(i >= MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR && i <= MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR) && 
                !(i >= MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR && i <= MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR))
            {
                Stairs st = createdMap.SpawnStairs(false); // goes to next level
#if UNITY_EDITOR
                //Debug.Log("Created stairs at " + st.GetPos() + " pointing to " + st.pointsToFloor + " up? " + st.stairsUp + " in map " + createdMap.floor);
#endif
            }
            if (i != startFloor && !hasStairsUp)
            {
                Stairs st = createdMap.SpawnStairs(true); // goes to previous level
#if UNITY_EDITOR
                //Debug.Log("Created stairs at " + st.GetPos() + " pointing to " + st.pointsToFloor + " up? " + st.stairsUp + " in map " + createdMap.floor);
#endif
            }
        }

        // Make sure our connections are good.

        DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(startFloor, endFloor - startFloor + 1, EDLCPackages.EXPANSION1);        
    }

    public static void CreateRealmOfGodsStairsInFinalBossRoomIfAllowed()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;
        if (ProgressTracker.CheckProgress(TDProgress.REALMGODS_UNLOCKED, ProgressLocations.META) != 1) return;
        if (ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.HERO) < 2) return;

        Map finalBossArea = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);

        Map realmGods1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.REALM_OF_GODS_START);

        Vector2 spawnStairsPosition = Vector2.zero;
        foreach (Stairs st in finalBossArea.mapStairs)
        {
            // make sure we're using the right stairs as reference
            if (st.prefab == "FutureStairsUp" && st.actorEnabled && (st.pointsToFloor == 999 || st.ReadActorData("finalstairs") == 1))
            {
                spawnStairsPosition = st.GetPos();
            }

            if (st.pointsToFloor == MapMasterScript.REALM_OF_GODS_START || st.NewLocation == realmGods1)
            {
                // already have em
                return;
            }
        }

        if (spawnStairsPosition == Vector2.zero)
        {
            int xPos = UnityEngine.Random.Range(10, finalBossArea.columns - 10);
            int yPos = UnityEngine.Random.Range(10, finalBossArea.rows - 10);
            spawnStairsPosition = new Vector2(xPos, yPos);
        }
        else
        {
            int xPos = (int)spawnStairsPosition.x + UnityEngine.Random.Range(-3, 4);
            int yPos = (int)spawnStairsPosition.y + UnityEngine.Random.Range(-6, -2);
            spawnStairsPosition = new Vector2(xPos, yPos);
        }

        Stairs stairsToRealm = new Stairs();
        stairsToRealm.stairsUp = false;
        stairsToRealm.isPortal = true;
        stairsToRealm.NewLocation = realmGods1;
        stairsToRealm.newLocationID = stairsToRealm.NewLocation.mapAreaID;
        stairsToRealm.pointsToFloor = MapMasterScript.REALM_OF_GODS_START;
        stairsToRealm.autoMove = true;
        stairsToRealm.prefab = "AltPortal";

        finalBossArea.PlaceActor(stairsToRealm, finalBossArea.GetTile(spawnStairsPosition));

        foreach(Stairs st in realmGods1.mapStairs)
        {
            if (st.stairsUp)
            {
                st.NewLocation = finalBossArea;
                st.newLocationID = finalBossArea.mapAreaID;
                st.pointsToFloor = MapMasterScript.FINAL_BOSS_FLOOR2;
            }
        }
    }

    static void CleanMapRangeStairsConnections(int startFloor, int endFloor, int initialConnectionFloor)
    {
        bool hasInitialConnection = false;

        // Scan through DLC map range, make sure none of them have spawned a staircase in a bad location.
        for (int i = startFloor; i <= endFloor; i++)
        {
            Map workMap = MapMasterScript.theDungeon.FindFloor(i);
            if (workMap == null)
            {
                Debug.Log("Why is DLC map " + i + " null in cleaning function?");
                continue;
            }

            foreach (Stairs st in workMap.mapStairs)
            {
                if (st.NewLocation == null) continue; // #todo - why would this happen

                if (i == startFloor && st.NewLocation.floor == initialConnectionFloor)
                {
                    hasInitialConnection = true;
                }

                if (st.NewLocation.floor < startFloor && st.NewLocation.floor != initialConnectionFloor || 
                    (st.NewLocation.floor == initialConnectionFloor && i != startFloor))
                {
                    st.NewLocation.RemoveStairsPointingToFloor(i);

                    if (st.stairsUp && workMap.floor == MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR) continue; // ignore this floor, since it has a weird connection state

                    if (Debug.isDebugBuild && workMap.floor != 375)
                    {
                        //Debug.Log("Uh oh, DLC area in map " + workMap.floor + " area starting at " + startFloor + " staircase direction " + st.stairsUp + " on floor " + i + " is pointing to " + st.NewLocation.floor + " " + st.NewLocation.GetName());
                    }
                    if (st.stairsUp)
                    {
                        if (i == MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR && st.pointsToFloor != MapMasterScript.BOSS1_MAP_FLOOR)
                        {
                            st.SetDestination(MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS1_MAP_FLOOR));
                            //if (Debug.isDebugBuild) Debug.Log("Rerouted to floor " + (i - 1));
                        }
                        else if (i == MapMasterScript.REALM_OF_GODS_START)
                        {
                            st.SetDestination(MapMasterScript.theDungeon.FindFloor(initialConnectionFloor));
                            //if (Debug.isDebugBuild) Debug.Log("Rerouted to final boss floor2.");
                        }
                        else
                        {
                            st.SetDestination(MapMasterScript.theDungeon.FindFloor(i - 1));
                            //if (Debug.isDebugBuild) Debug.Log("Rerouted to floor " + (i-1));
                        }
                    }
                    else
                    {
                        if (i < MapMasterScript.REALM_OF_GODS_END)
                        {
                            if (i == startFloor)
                            {
                                st.SetDestination(MapMasterScript.theDungeon.FindFloor(initialConnectionFloor));
                            }
                            else
                            {
                                st.SetDestination(MapMasterScript.theDungeon.FindFloor(i + 1));
                            }

                            /* if (Debug.isDebugBuild)
                            {
                                if (st.NewLocation != null) Debug.Log("Rerouted to " + st.NewLocation.GetName());
                            } */
                        }
                        else
                        {
                            Debug.Log("Don't know how to reroute.");
                            st.DisableActor();
                        }
                    }
                    // And remove this stairs from the connecting map                    
                }
                else
                {
                    //Debug.Log("Realm of gods stairs up? " + st.stairsUp + " on floor " + i + " points to " + st.newLocation.floor);
                }
            }
        }

        if (!hasInitialConnection)
        {
            Map startMap = MapMasterScript.theDungeon.FindFloor(startFloor);

            MapTileData forceTile = null;

            if (GameMasterScript.heroPCActor.dungeonFloor == startMap.floor && startMap.FindActorByID(GameMasterScript.heroPCActor.actorUniqueID) != null)
            {
                forceTile = startMap.GetTile(GameMasterScript.heroPCActor.GetPos());
            }

            Stairs st = startMap.SpawnStairs(true, initialConnectionFloor, forceTile);
            if (st != null)
            {
                if (MapMasterScript.activeMap.floor == startFloor) MapMasterScript.singletonMMS.SpawnStairs(st);
            }
            
        }
    }

    /// <summary>
    /// Creates, cleans, or repairs DLC area of given floors (such as realm of gods start/finish). Returns TRUE if the map state was changed.
    /// </summary>
    /// <returns></returns>
    static bool VerifyDLCMapStateIsCorrect(int mapFloorStart, int mapFloorEnd, int initialConnectionFloor)
    {
        bool anyNewMapsCreated = false;
        if (MapMasterScript.theDungeon.FindFloor(mapFloorStart) == null)
        {
            CreateAndConnectDLCMaps(mapFloorStart, mapFloorEnd, true);
            anyNewMapsCreated = true;
        }
        else
        {
            bool anyNullMaps = false;
            // Make sure each map exists!
            for (int i = mapFloorStart; i <= mapFloorEnd; i++)
            {
                Map checkForMap = MapMasterScript.theDungeon.FindFloor(i);
                if (checkForMap == null)
                {
                    anyNullMaps = true;
                    //if (Debug.isDebugBuild) Debug.Log("Uh oh, DLC floor " + i + " does not exist.");
                }
            }

            if (anyNullMaps)
            {
                for (int i = mapFloorStart; i <= mapFloorEnd; i++)
                {
                    // Clear out existing maps from the master dict.
                    Map m = MapMasterScript.theDungeon.FindFloor(i);
                    if (m != null)
                    {
                        MapMasterScript.dictAllMaps.Remove(m.mapAreaID);
                        MapMasterScript.theDungeon.RemoveMapByFloor(m.floor);
                        MapMasterScript.OnMapRemoved(m);
                    }                    
                }

                // Then try fixing it.
                CreateAndConnectDLCMaps(mapFloorStart, mapFloorEnd, true);
                anyNewMapsCreated = true;
            }

            CleanMapRangeStairsConnections(mapFloorStart, mapFloorEnd, initialConnectionFloor);
        }

        return anyNewMapsCreated;
    }

    /// <summary>
    /// Checks if any DLC maps had an alarmingly high number of actor add failures, usually indicating a bad map gen. Flags those maps to be remade as needed.
    /// </summary>
    /// <param name="failureArray"></param>
    public static void CheckForMapLoadFailures(int[] failureArray)
    {
        for (int i = MapMasterScript.RIVERSTONE_WATERWAY_START; i <= MapMasterScript.RIVERSTONE_WATERWAY_END; i++)
        {
            if (failureArray[i] >= 15)
            {
                Debug.Log("Map " + i + " had too many failures. we should remake it. " + failureArray[i]);
                GameMasterScript.gmsSingleton.SetTempGameData("waterway_load_failure", 1);
                break;
            }
        }
    }

    static bool ShouldWaterwayBeUnlocked()
    {
        Map town = MapMasterScript.singletonMMS.townMap;
        bool waterwayExists = town.FindActor("npc_jumpintoriver") as NPC != null;

        if (waterwayExists || SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.RIVERSTONE_WATERWAY))
        {
            return true;
        }
        else
        {            

        if ((ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1 && GameMasterScript.heroPCActor.lowestFloorExplored <= 1) 
            || (TDPlayerPrefs.GetInt(GlobalProgressKeys.BEAT_FIRST_BOSS) == 1 && PlayerOptions.globalUnlocks))
            {
                SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.RIVERSTONE_WATERWAY);
                return true;
            }            

            return false;
        }

        //Debug.Log(ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) + " AND " + GameMasterScript.heroPCActor.lowestFloorExplored);

        if ((ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1 && GameMasterScript.heroPCActor.lowestFloorExplored <= 1) 
            || (TDPlayerPrefs.GetInt(GlobalProgressKeys.BEAT_FIRST_BOSS) == 1 && PlayerOptions.globalUnlocks)
            || waterwayExists)
        {
            return true;
        }

        return false;
    }

    static bool ShouldWandererBeUnlocked()
    {
        if (RandomJobMode.IsCurrentGameInRandomJobMode()) return false;

        if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1 ||
            TDPlayerPrefs.GetInt(GlobalProgressKeys.BEAT_FIRST_BOSS) == 1 && PlayerOptions.globalUnlocks)
        {
            return true;
        }
        return false;
    }

    static void VerifyAllDragonDungeons()
    {        

        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) >= 1)
        {
            Map grotto = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JELLY_GROTTO);
            if (grotto.mapIsHidden)
            {
                grotto.SetMapVisibility(true);
            }

            Map beastlake = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BEASTLAKE_SIDEAREA);
            if (beastlake.mapIsHidden)
            {
                beastlake.SetMapVisibility(true);
            }
        }

        bool anyNewMapsCreated = false;

        /* if (MapMasterScript.theDungeon.FindFloor(MapMasterScript.RIVERSTONE_WATERWAY_START) == null || GameMasterScript.gmsSingleton.ReadTempGameData("waterway_load_failure") == 1)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("waterway_load_failure", 0);
            CreateAndConnectDLCMaps(MapMasterScript.RIVERSTONE_WATERWAY_START, MapMasterScript.RIVERSTONE_WATERWAY_END, true, connectToStartFloor: MapMasterScript.TOWN_MAP_FLOOR, connectToEndFloor: MapMasterScript.PREBOSS1_MAP_FLOOR);
            anyNewMapsCreated = true;
        } */

        anyNewMapsCreated = VerifyDLCMapStateIsCorrect(MapMasterScript.FROG_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.FROG_DRAGON_DUNGEONEND_FLOOR, MapMasterScript.MAP_FROG_BOG);

        if (VerifyDLCMapStateIsCorrect(MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR, MapMasterScript.BEASTLAKE_SIDEAREA))
        {
            anyNewMapsCreated = true;
        }
        if (VerifyDLCMapStateIsCorrect(MapMasterScript.BANDIT_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.BANDIT_DRAGON_DUNGEONEND_FLOOR, MapMasterScript.BOSS1_MAP_FLOOR))
        {
            anyNewMapsCreated = true;
        }
        if (VerifyDLCMapStateIsCorrect(MapMasterScript.ROBOT_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.ROBOT_DRAGON_DUNGEONEND_FLOOR, MapMasterScript.GUARDIAN_RUINS_ENTRY_FLOOR))
        {
            anyNewMapsCreated = true;
        }
        if (VerifyDLCMapStateIsCorrect(MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR, MapMasterScript.JELLY_GROTTO))
        {
            anyNewMapsCreated = true;
        }
        if (VerifyDLCMapStateIsCorrect(MapMasterScript.SPIRIT_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.SPIRIT_DRAGON_DUNGEONEND_FLOOR, 0))
        {
            anyNewMapsCreated = true;
        }

        //CreateRealmOfGodsStairsInFinalBossRoomIfAllowed();

        if (anyNewMapsCreated)
        {
            MakeDLCStairConnectionsIfNeeded();
        }

        MetaProgressScript.CheckForFirstTimeDLC2Cleanup();
    }

    /// <summary>
    /// Make sure no objects are overlapping with 'blocker' tiles
    /// </summary>
    /// <param name="floor"></param>
    static void FixBadObjectPlacementInRealmOfGods(int floor)
    {
        Map theMap = MapMasterScript.theDungeon.FindFloor(floor);
        int count = theMap.actorsInMap.Count;
        for (int i = 0; i < count; i++)
        {
            Actor act = theMap.actorsInMap[i];
            
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = act as Destructible;
                if (dt.mapObjType == SpecialMapObject.BLOCKER) continue;
            }

            MapTileData mtd = theMap.GetTile(act.GetPos());
            if (mtd.specialMapObjectsInTile[(int)SpecialMapObject.BLOCKER])
            {
                // if this actor is in the same tile as a Blocker, move the actor.
                MapTileData nextEmpty = theMap.GetRandomEmptyTile(mtd.pos, 1, true, true, true, false, true);
                theMap.RemoveActorFromLocation(mtd.pos, act);
                theMap.AddActorToLocation(nextEmpty.pos, act);
                act.SetPos(nextEmpty.pos);
                act.myMovable.AnimateSetPosition(nextEmpty.pos, 0.01f, false, 0f, 0f, MovementTypes.LERP);
                //Debug.Log("Moved " + act.actorRefName + " from " + mtd.pos + " to " + nextEmpty.pos);
            }            
        }
    }

    public static void UpdateItemDefinitionsAfterLoad()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            Weapon vampireKiller = GameMasterScript.masterItemList["weapon_leg_whip"] as Weapon;
            vampireKiller.weaponType = WeaponTypes.WHIP;
        }
    }

    /// <summary>
    /// Returns true if DLC2 is installed and we beat the first 5 dragons.
    /// </summary>
    /// <returns></returns>
    public static bool DragonDefeatedCalloutPossible()
    {
        if (!CheckDLCInstalled(EDLCPackages.EXPANSION2)) return false;

        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) >= 1 && 
            ProgressTracker.CheckProgress(TDProgress.DRAGON_BANDIT, ProgressLocations.HERO) >= 1 && 
            ProgressTracker.CheckProgress(TDProgress.DRAGON_BEAST, ProgressLocations.HERO) >= 1 && 
            ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO) >= 1 &&
            ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) >= 4)
        {
            if (ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.HERO) < 2 && ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.META) < 2 
                && GameStartData.NewGamePlus == 0)
            {
                // ok, we haven't cleared final boss on this file ever
                return false;
            }
            if (ProgressTracker.CheckProgress(TDProgress.DRAGON_ROBOT_KICKOFF, ProgressLocations.HERO) < 1)
            {
                return true;
            }
        }

        return false;
    }

    static void CheckForWaterwayUnlockAndJumpPoint()
    {
        // Create a path to the alternate area by jumping into the river. Put some sparkles there that allow
        // the player to jump in! But only do it if they've had at least one other character
        // But only do this for new characters, otherwise it would be confusing to see path to low-level area appearing.
        if (ShouldWaterwayBeUnlocked())
        {
            //if (Debug.isDebugBuild) Debug.Log("Map is " + MapMasterScript.activeMap.floor);

            NPC riverSparkles = MapMasterScript.activeMap.FindActor("npc_jumpintoriver") as NPC;
            Vector2 pos = new Vector2(19f, 11f);
            MapTileData gTile = MapMasterScript.GetTile(pos);

            if (riverSparkles == null)
            {
                //if (Debug.isDebugBuild) Debug.Log("Sparkles DO NOT exist.");
                NPC sparkles = NPC.CreateNPC("npc_jumpintoriver");
                sparkles.SetCurPos(pos);
                sparkles.SetSpawnPos(pos);
                MapMasterScript.activeMap.PlaceActor(sparkles, gTile);
                MapMasterScript.singletonMMS.SpawnNPC(sparkles);
            }
            else
            {
                //if (Debug.isDebugBuild) Debug.Log("Sparkles DO exist.");
                MapMasterScript.activeMap.RemoveActorFromLocation(riverSparkles.GetPos(), riverSparkles);
                riverSparkles.SetCurPos(pos);
                riverSparkles.SetSpawnPos(pos);
                MapMasterScript.activeMap.AddActorToLocation(pos, riverSparkles);
                riverSparkles.myMovable.AnimateSetPosition(pos, 0.0001f, false, 0f, 0f, MovementTypes.LERP);
            }

            NPC dummyTarget = MapMasterScript.activeMap.FindActor("npc_dummyriver") as NPC;
            pos = new Vector2(19f, 10f - 3f);
            gTile = MapMasterScript.GetTile(pos);

            if (dummyTarget == null)
            {
                //if (Debug.isDebugBuild) Debug.Log("Dummy target DOES NOT exist.");
                NPC dummy = NPC.CreateNPC("npc_dummyriver");
                dummy.SetCurPos(pos);
                dummy.SetSpawnPos(pos);
                MapMasterScript.activeMap.PlaceActor(dummy, gTile);
                MapMasterScript.singletonMMS.SpawnNPC(dummy);                
            }
            else
            {
                //if (Debug.isDebugBuild) Debug.Log("Dummy target DOES exist.");
                MapMasterScript.activeMap.RemoveActorFromLocation(dummyTarget.GetPos(), dummyTarget);
                dummyTarget.SetCurPos(pos);
                dummyTarget.SetSpawnPos(pos);
                MapMasterScript.activeMap.AddActorToLocation(pos, dummyTarget);
                dummyTarget.myMovable.AnimateSetPosition(pos, 0.0001f, false, 0f, 0f, MovementTypes.LERP);
            }

            Map preDirtbeak = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PREBOSS1_MAP_FLOOR);
            foreach (Stairs st in preDirtbeak.mapStairs)
            {
                st.EnableActor();
            }
        }
    }
    public static int GetCachedDLCContentCount()
    {
        //  This is how Switch checks for DLC. It is specific to Nintendo SDK.
#if UNITY_SWITCH
        if (!checkedDLC)
        {
            cachedDLCContentCount = nn.aoc.Aoc.CountAddOnContent();
            Debug.Log("We have aoc content count: " + cachedDLCContentCount);
            checkedDLC = true;
        }
        return cachedDLCContentCount;
#endif
        return 1;
    }

    public static void XboxOneUnmountDLC()
    {
#if UNITY_XBOXONE
        DownloadableContentPackageList m_DLC = DLCManagerXBOXONE.FindPackages(InstalledPackagesFilter.AllRelatedPackages);
        if (m_DLC.Length > 0)
        {
            for (int i = 0; i < m_DLC.Length; ++i)
            {
                bool mounted = m_DLC[i].IsMounted;
                bool wasMounted = mounted;

                bool isDlc = true;
                string displayName = m_DLC[i].DisplayName;
                if (displayName == null || displayName == "")
                {
                    isDlc = false;
                    displayName = "NOT DLC";
                    //Debug.LogError("we found: " + displayName);
                    continue;
                }

                if (mounted)
                {
                    m_DLC[i].UnMount();
                    Debug.LogError(string.Format("> [UNMOUNT]  [{1}] DLC {0}\n", m_DLC[i].DisplayName, m_DLC[i].IsMounted ? "Failed" : "Ok"));
                }
                mounted = m_DLC[i].IsMounted;
            }
        }
#endif
    }
}
