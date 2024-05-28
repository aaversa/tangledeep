using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public partial class MusicManagerScript
{

    //look through the loaded asset bundles, 
    private IEnumerator LoadMusicTrackFromAssetBundle(string strNameOfTrack, bool bPlayImmediately, bool bRecurseBackToLoadMusicByName = true)
    {

        if (LogoSceneScript.debugMusic) Debug.Log("Request load music track: " + strNameOfTrack);


        //if we are already loading music, wait for that to complete
        if (bLoadingNotComplete)
        {
            yield return new WaitWhile(() => bLoadingNotComplete);
        }

        //flag up, we can't play any music until this is done.
        bLoadingNotComplete = true;

        //which asset bundle has this song?
        string strBundleName = "music_" + strNameOfTrack.ToLowerInvariant();

        //have we loaded it?
        ScriptableObject_MusicData smd = null;

        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Try loading from bundle: " + strBundleName + " track name: " + strNameOfTrack);

        var loadedClip = TDAssetBundleLoader.GetCachedAudioClip(strBundleName, ref smd);
        if (loadedClip == null)
        {
            AssetBundle abWithMyStuff = TDAssetBundleLoader.GetBundleIfExists(strBundleName);
            if (abWithMyStuff == null)
            {
                //if (Debug.isDebugBuild) Debug.Log("Searching for " + strBundleName + ": Bundle wasn't loaded yet, so let's try to load it.");

                //if not, load it
                //the asset bundle lives inside a folder called "music"
                string strBuildMeAPath = Path.Combine(Application.streamingAssetsPath, "music");
                strBuildMeAPath = Path.Combine(strBuildMeAPath, strBundleName);

                //if (Debug.isDebugBuild) Debug.Log("Path we will use is: " + strBuildMeAPath);

                yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBuildMeAPath);

                //if this doesn't work, maybe we're quitting in the early game during a muisc load?
                abWithMyStuff = TDAssetBundleLoader.GetBundleIfExists(strBundleName);

                if (abWithMyStuff == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Tried to load " + strNameOfTrack + " but failed -- was the load interrupted? Will try again.");
                    PushSpecificMusicTrackOnStack(strNameOfTrack);
                    yield break;
                }
            }

            //load the ScriptableObject that contains a reference to the .wav and the data.
            string assetName = null;
            //AssetBundleRequest request = null;

            if (LogoSceneScript.debugMusic) Debug.Log("Prepare to load the asset from " + abWithMyStuff.name);

            //grab the .wav file
            assetName = abWithMyStuff.GetAllAssetNames().Where(n => n.Contains(".wav")).ToList()[0];

            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Loading " + assetName);

            AssetBundleRequest request = abWithMyStuff.LoadAssetAsync(assetName);
            yield return new WaitWhile(() => !request.isDone);

            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Request done! Got " + assetName);

            loadedClip = request.asset as AudioClip;

#if UNITY_EDITOR
    var files = abWithMyStuff.GetAllAssetNames().ToList();
    foreach(var file in files)
    {
        Debug.Log("Found file in bundle: " + file);
    }
#endif

            //this grabs the first thing with .asset at the end, which is our scriptable object.            
            assetName = abWithMyStuff.GetAllAssetNames().Where(n => n.Contains(".asset")).ToList()[0];
            request = abWithMyStuff.LoadAssetAsync(assetName);
            yield return new WaitWhile(() => !request.isDone);

            //if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("All done with that");

            //assign the loaded clip to the music data
            smd = request.asset as ScriptableObject_MusicData;

            //store this brilliance for later
            TDAssetBundleLoader.CacheAudioClip(strBundleName, loadedClip, smd);
        }

        smd.clip = loadedClip;

        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Load the TD Music object. " + strNameOfTrack + ", recurse? " + bRecurseBackToLoadMusicByName);

        //Create the tracking data object and put it in the dictionary
        LoadTDMusicDataObject(smd);

        //Loading is done! Now the play_async coroutine can continue
        bLoadingNotComplete = false;

        //nothing but net.
        if (bRecurseBackToLoadMusicByName)
        {
            LoadMusicByName_WithIntroloop(strNameOfTrack, true, bPlayImmediately);
        }
    }

    //This will spin for all time, ready to load music when we need it.
    IEnumerator LoadAllGameMusic_Coroutine()
    {
        //A reliable engine we can throw music at in order to load it on up.
        if (Debug.isDebugBuild) Debug.Log("LoadAllGameMusic_Coroutine has begun, we will load everything we are asked to load!");
        while (true)
        {
            if (stackTracksToLoad == null ||
                stackTracksToLoad.Count == 0 ||
                allMusicTracks == null)
            {
                yield return null;
                continue;
            }

            //allow this to be held up if needed to load other assets.
            while (bPauseLoadAllMusicCoroutine)
            {
                yield return null;
            }

            string strTrack = stackTracksToLoad.Pop();


            //If we've already loaded this, cool, keep going
            if (allMusicTracks.ContainsKey(strTrack))
            {
                //Debug.Log("LoadAllGameMusic_Coroutine: Already loaded " + strTrack);
                continue;
            }
            //Debug.Log("LoadAllGameMusic_Coroutine: attempting to load " + strTrack );

            //load it from the bundle, but don't play it! 
            //UIManagerScript.Debug_AddSwitchDebugText("Loading track " + strTrack + " at " + Time.realtimeSinceStartup);
            yield return LoadMusicTrackFromAssetBundle(strTrack, false, false);
        }

    }

    IEnumerator WaitUntilTrackLoadedThenTryAgain_NoIntroloop(string nameOfTrack, bool looping, bool bPlayImmediately)
    {
        MusicTrackData mtd;
        allMusicTracks.TryGetValue(nameOfTrack, out mtd);
        //if (Debug.isDebugBuild) Debug.Log("Waiting until " + nameOfTrack + " is loaded, then I will play it!");

        //wait patiently until it is loaded
        while (mtd == null)
        {
            yield return null;
            allMusicTracks.TryGetValue(nameOfTrack, out mtd);
        }

        //it is in memory now, so load it into a channel and go go go 
        bWaitingToPlayTrack_NotLoadedYet = false;

        //Debug.Log("Oh boy! " + nameOfTrack + " is loaded");

        LoadMusicByName_NoIntroloop(nameOfTrack, looping, bPlayImmediately);
    }
}
