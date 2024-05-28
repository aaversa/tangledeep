using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MusicManagerScript
{
    // These "specific track" routines ignore all crossfades and forcibly play a track.
    // Use very carefully as this eliminates transitions. However, it may be necessary in some situations.
    private void ForcePlayChannel_NoIntroloop(int iChannelIndex)
    {
        currentPlayingCoroutine = StartCoroutine(ForcePlaySpecificChannel_Async(iChannelIndex));
    }

    IEnumerator ForcePlaySpecificChannel_Async(int iChannelIndex)
    {
        while (bLoadingNotComplete)
        {
            yield return null;
        }

        //stop all the tracks
        StopAllMusic();

        //this is the new channel now.
        activeChannel = iChannelIndex;

        //play the audio -- it is already set to loop or not loop
        ActuallyPlayAudioInClip_NoIntroloop(musicChannels[activeChannel], activeChannel);

        //crank the volume
        musicChannels[activeChannel].volume = 1f;
    }


    //Allows us to wait for a track to be loaded if it isn't already.
    //Then plays music on activeChannel
    IEnumerator Play_Async_NoIntroloop(bool playFromLastPosition, bool crossfade, bool okToPlaySameTrack)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Request play async...");

        //if we are waiting for a track to load, AND that's the track we're supposed to be playing,
        //chill out for a while.
        while (bWaitingToPlayTrack_NotLoadedYet)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Track is not loaded yet.");
            yield return null;
        }

        //Don't just play if we're already playing.
        if (musicChannels[activeChannel].isPlaying)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("But channel " + activeChannel + " is already playing");
            yield break;
        }


        //check to see if we're playing the same track on both channels
        //assumption: we have at least 2 channels
        if (!okToPlaySameTrack &&
            (musicChannels[0].isPlaying || musicChannels[1].isPlaying))
        {
            if (musicTracksLoaded[0].refName == musicTracksLoaded[1].refName)
            {
                if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Don't play identical tracks please!");
                yield break;
            }
        }

#if UNITY_EDITOR
        if (LogoSceneScript.debugMusic) Debug.Log("music: Play_Async_NoIntroloop fired, crossfade==" + crossfade + ", active channel is " + activeChannel + ". Num channels is: " + musicChannels.Length);
#endif

        //if there's nothing actually playing in the other channel, don't crossfade.
        if (musicChannels[(activeChannel + 1) % 2].volume == 0)
        {
            crossfade = false;
        }

        if (musicChannels[activeChannel].timeSamples >= musicChannels[activeChannel].clip.samples)
        {
            musicChannels[activeChannel].timeSamples = 0;
        }

        if (!playFromLastPosition)
        {
            musicChannels[activeChannel].timeSamples = 0;
        }

        //we have set the activeChannel and current track, so just play
        /* if (musicChannels[activeChannel].clip == null)
        {
            Debug.Log("No clip in active channel?");
        }
        else
        {
            Debug.Log("Playing " + musicChannels[activeChannel].clip.name);
        } */
        musicChannels[activeChannel].Play();

        //cross fade if we need to
        if (crossfade)
        {
            //set the other channel to 0, and our channel to 1 over the course of crossfadeTime
            //the last number is a % where we wait to fade the new track in until the old track is % faded out.
            //which means this coroutine actually runs for crossfadeTime + crossfadeTime * fadePercent seconds.
            StartCoroutine(ICrossfadeMusic_NoIntroloop(1 - activeChannel, activeChannel, 0f, 1f, crossfadeTime, 0.5f));
        }
        else
        {
            musicChannels[activeChannel].volume = 1f;
        }
    }

    //Allows us to wait for a track to be loaded if it isn't already.
    IEnumerator Play_Async_WithIntroloop(bool playFromLastPosition, bool crossfade, bool okToPlaySameTrack = false)
    {
        while (bLoadingNotComplete)
        {
            yield return null;
        }

        //Debug.Log("Finished loading music in Play_Async");

        // Debug stuff.
        if (musicTracksLoaded == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Track loaded array is null?");
            musicTracksLoaded = new MusicTrackData[2];
        }
        if (IntroloopPlayer.InstanceID(activeChannel) == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Why is instance " + activeChannel + " ILP null?");
            yield break;
        }
        if (musicTracksLoaded[activeChannel] == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Music track loaded for index " + activeChannel + " is null?");
            yield break;
        }
        // End debug.

        if ((IntroloopPlayer.InstanceID(0).IsPlaying() || IntroloopPlayer.InstanceID(1).IsPlaying()) 
            && !okToPlaySameTrack)
        {
            if (musicTracksLoaded[0].refName == musicTracksLoaded[1].refName)
            {
                //Debug.Log("Don't play identical tracks please!");
                yield break;
            }
        }

        //Debug.Log("Request play async " + IntroloopPlayer.InstanceID(activeTrack).IsPlaying() + " " + IntroloopPlayer.InstanceID(1 - activeTrack).IsPlaying());

        // The first conditional was ! before.
        if (!IntroloopPlayer.InstanceID(activeChannel).IsPlaying() && musicTracksLoaded[activeChannel].refName != "resttheme" &&
            musicTracksLoaded[activeChannel].refName != "BossVictory")
        {
            currentTrack = musicTracksLoaded[activeChannel];

            ActuallyPlayAudioInClip_WithIntroloop(musicChannels[activeChannel], activeChannel);

            if (!musicTracksLoaded[activeChannel].looping)
            {
                musicChannels[activeChannel].loop = false;
            }
            else
            {
                musicChannels[activeChannel].loop = true;
            }
            //Debug.Log("Playing on track " + activeTrack);
        }
        else
        {
            int otherTrack = 1 - activeChannel;

            currentTrack = musicTracksLoaded[otherTrack];

            ActuallyPlayAudioInClip_WithIntroloop(musicChannels[otherTrack], otherTrack, playFromLastPosition);

            if (!musicTracksLoaded[otherTrack].looping)
            {
                musicChannels[otherTrack].loop = false;
            }
            else
            {
                musicChannels[otherTrack].loop = true;
            }
            if (crossfade)
            {
                Crossfade_WithIntroloop(activeChannel, otherTrack, crossfadeTime);
            }
            else
            {
                ActuallySetClipVolume(musicChannels[otherTrack], otherTrack, 1f);
            }
        }
    }

    private void ActuallyPlayAudioInClip_NoIntroloop(AudioSource aSource, int index, bool playFromLastPosition = false)
    {
        if (!playFromLastPosition)
        {
            aSource.time = 0;
            aSource.timeSamples = 0;
        }
        aSource.Play();
    }

    void ActuallyLoadClip_WithIntroloop(MusicTrackData music, AudioClip clip, bool looping, bool playImmediately = false)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Actually load a clip WITH introloop. " + music.refName);

        /* if (currentTrack != null) // debug only
        {
            Debug.Log("CurrentTrack " + currentTrack.refName);
        } */

        currentTrack = music;
        // Get the clip.

        if (clip == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Clip was null for music refname " + music.refName + " / filename: " + music.trackFileName);
            return;
        }

        //Debug.Log("Preparing to load " + music.refName + " to " + clip.name);

        int trackNumberSelected = -1;

        if (GetMusicClip_FromIntroloop(0) == null)
        {
            if (clip == null) if (Debug.isDebugBuild) Debug.Log("Clip is null?");
            if (allMusicTrackPositions == null) if (Debug.isDebugBuild) Debug.Log("AMT positions is null?");

            if (clip.samples < allMusicTrackPositions[music.refName])
            {
                playbackPosition[0] = 0;
            }
            else
            {
                playbackPosition[0] = allMusicTrackPositions[music.refName];
            }

            //if (Debug.isDebugBuild) Debug.Log("Loaded to track 0, load ILA " + clip.name);

            string trackName = clip.name;
            IntroloopAudio ila = null;
            if (!dictIntroLoopAssets.TryGetValue(trackName, out ila))
            {
                Debug.Log(trackName + " doesn't exist, so searching " + music.refName);
                trackName = music.refName;
            }

            //Debug.Log(trackName + " try loading. null? " + (ila == null));

            IntroloopPlayer.InstanceID(0).LoadAudio(clip);
            IntroloopPlayer.InstanceID(0).Preload(dictIntroLoopAssets[trackName]);

            //musicTracks[0].clip = clip;
            //musicTracks[0].loop = looping;

            //Debug.Log("Loaded on track 0");
            musicTracksLoaded[0] = music;
            trackNumberSelected = 0;
        }
        //else if (musicTracks[1].clip == null)
        else if (GetMusicClip_FromIntroloop(1) == null)
        {
            //Debug.Log("Loaded to track 1");
            //musicTracks[1].clip = clip;

            string trackName = clip.name;
            IntroloopAudio ila;
            if (!dictIntroLoopAssets.TryGetValue(trackName, out ila))
            {
                trackName = music.refName;
            }

            IntroloopPlayer.InstanceID(1).LoadAudio(clip);
            IntroloopPlayer.InstanceID(1).Preload(dictIntroLoopAssets[trackName]);

            musicChannels[1].loop = looping;
            if (clip.samples < allMusicTrackPositions[music.refName])
            {
                playbackPosition[1] = 0;
            }
            else
            {
                playbackPosition[1] = allMusicTrackPositions[music.refName];
            }
            musicTracksLoaded[1] = music;
            trackNumberSelected = 1;
        }

        // ONLY if we are playing immediately do we alter the activeTrack status.
        // Otherwise, we let the Play() function do it as needed.
        if (playImmediately && trackNumberSelected != -1)
        {
            activeChannel = trackNumberSelected;
        }
        else if (playImmediately && trackNumberSelected == -1)
        {
            activeChannel = 1 - activeChannel;
            trackNumberSelected = activeChannel;
        }
        else
        {
            trackNumberSelected = 1 - activeChannel;
        }

        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Track to play on is: " + trackNumberSelected + " " + playImmediately);
        globalMostRecentTrackNumberSelected = trackNumberSelected;

        //musicTracks[trackNumberSelected].clip = clip; // was 1- activetrack but now we're setting it above

        IntroloopPlayer.InstanceID(trackNumberSelected).LoadAudio(clip);
        IntroloopPlayer.InstanceID(trackNumberSelected).Preload(dictIntroLoopAssets[clip.name]);

        musicChannels[trackNumberSelected].loop = looping;
        playbackPosition[trackNumberSelected] = allMusicTrackPositions[music.refName];
        musicTracksLoaded[trackNumberSelected] = music;
        if (playImmediately)
        {
            PlaySpecificTrack_WithIntroloop(trackNumberSelected, false, false);
        }
    }
}