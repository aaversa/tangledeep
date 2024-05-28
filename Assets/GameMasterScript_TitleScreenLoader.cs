using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    IEnumerator DoTitleScreenLoader()
    {
        simpleBool = new bool[2];
        simpleBool[0] = false;
        simpleBool[1] = true;
        randomSign = new int[2];
        randomSign[0] = 1;
        randomSign[1] = -1;

        /* if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            PlayerOptions.ReadOptionsFromFile();
        } */

        if (!PlatformVariables.USE_INTROLOOP)
        {
            //These must be loaded because the music lives in them
            if (musicManager == null)
            {
                musicManager = GameObject.Find("AudioManager").GetComponent<MusicManagerScript>();
                musicManager.MusicManagerStart();
            }

            musicManager.PushSpecificMusicTrackOnStack("trainingtheme");
#if !UNITY_PS4 && !UNITY_XBOXONE //on PS4/XBOXONE we want to load everithing before playing music, this part is moved to the end of this IEnumerator
            if (DLCManager.ShouldShowLegendOfSharaTitleScreen())
            {
                MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("shara_titlescreen");
            }
            else
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2) && UnityEngine.Random.Range(0, 2) == 0)
                {
                    MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dragontitle");
                }
                else
                {
                    MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("titlescreen");
                }
            }
#endif
        }

        float beforeLoadBundles = Time.realtimeSinceStartup;


        //if (Debug.isDebugBuild) Debug.Log("Title screen prepare to load asset bundles");


        yield return LoadAllAssetBundles();
        //if (Debug.isDebugBuild) Debug.Log("Done! Time to load asset bundles was: " + (Time.realtimeSinceStartup - beforeLoadBundles));

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            TryPreloadResourceInstant("DialogButton", "Art/ui/DialogButton");
            TryPreloadResourceInstant("DialogButtonThreeColumn", "Art/ui/DialogButtonThreeColumn");
            TryPreloadResourceInstant("DialogButtonWithImage", "Art/ui/DialogButtonWithImage");
            TryPreloadResourceInstant("DialogButtonWithImageRightJustified", "Art/ui/DialogButtonWithImageRightJustified");
            TryPreloadResourceInstant("BigDialogButton", "Art/ui/BigDialogButton");
        }
        else
        {
            TryPreloadResourceNoBundles("DialogButton", "Art/ui/DialogButton");
            TryPreloadResourceNoBundles("DialogButtonThreeColumn", "Art/ui/DialogButtonThreeColumn");
            TryPreloadResourceNoBundles("DialogButtonWithImage", "Art/ui/DialogButtonWithImage");
            TryPreloadResourceNoBundles("DialogButtonWithImageRightJustified", "Art/ui/DialogButtonWithImageRightJustified");
            TryPreloadResourceNoBundles("BigDialogButton", "Art/ui/BigDialogButton");
            GetStringsFromAbilityXML();
        }


        yield return LoadAllAbilities();

        while (!allAbilitiesLoaded)
        {
            yield return null;
        }


#if UNITY_EDITOR
        //Debug.Log("All abilities loaded at title.");
#endif

        if (masterJobList == null)
        {
            yield return LoadAllJobs();
        }
        allJobsLoaded = true;

        float fParseTimer = Time.realtimeSinceStartup;

        foreach (AbilityScript abil in GameMasterScript.masterAbilityList.Values)
        {
            abil.ParseNumberTags();
#if UNITY_SWITCH
            if (Time.realtimeSinceStartup - fParseTimer > 0.013f)
            {
                fParseTimer = Time.realtimeSinceStartup;
                yield return null;
            }
#endif
        }

        foreach (CharacterJobData cjd in GameMasterScript.masterJobList)
        {
            cjd.ParseNumberTags();
#if UNITY_SWITCH
            if (Time.realtimeSinceStartup - fParseTimer > 0.013f)
            {
                fParseTimer = Time.realtimeSinceStartup;
                yield return null;
            }
#endif
        }

        maxJPAllJobs = new int[(int)CharacterJobs.COUNT - 2];

        for (int x = 0; x < (int)CharacterJobs.COUNT - 2; x++)
        {
            CharacterJobData cjd = CharacterJobData.GetJobData(((CharacterJobs)x).ToString());
            if (cjd == null) continue;
            int jobJPMax = 0;
            for (int i = 0; i < cjd.JobAbilities.Count; i++)
            {
                if (cjd.JobAbilities[i].jpCost > 0)
                {
                    jobJPMax += cjd.JobAbilities[i].jpCost;
                }
            }
            maxJPAllJobs[x] = jobJPMax;
        }

        CharCreation.NameEntryScreenState = ENameEntryScreenState.max;

        TitleScreenScript.bReadyForMainMenuDialog = true;

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            TitleScreenScript.bReadyForMainMenuDialog = true;
            //StartCoroutine(TDAssetBundleLoader.LoadSpecificAssetBundle(Application.streamingAssetsPath + "/audio"));
            //StartCoroutine(TDAssetBundleLoader.LoadSpecificAssetBundle(Application.streamingAssetsPath + "/spriteeffects"));
        }

#if UNITY_PS4 || UNITY_XBOXONE //wait until everything is loaded, when bReadyForMainMenuDialog is true, play music
        if (!PlatformVariables.USE_INTROLOOP)
        {
            if (DLCManager.GetLastPlayedCampaign() == StoryCampaigns.SHARA)
            {
                MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("shara_titlescreen");
            }
            else
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2) && UnityEngine.Random.Range(0, 2) == 0)
                {
                    MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dragontitle");
                }
                else
                {
                    MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("titlescreen");
                }
            }
        }
#endif

    }

}