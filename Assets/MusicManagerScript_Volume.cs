using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MusicManagerScript
{
    bool lastFadeSetVolumeToZero = false;

    public void SetMusicVolumeToMatchOptionsSlider()
    {
        float fNewVolume = UIManagerScript.GetMusicVolume();

        if (fNewVolume == 1f)
        {
            fNewVolume = PlayerOptions.musicVolume;
        }

        SetMusicVolume(fNewVolume);

    }

    public object SetMusicVolumeFromHundredBasedPlayerOptions()
    {
#if UNITY_PS4 || UNITY_XBOXONE
        //set musicVolume in options in order to save our change of volume
        PlayerOptions.musicVolume = (int)Convert0to100IntToDBFloat(PlayerOptions.hundredBasedMusicVolume);
#endif
        SetMusicVolume(Convert0to100IntToDBFloat(PlayerOptions.hundredBasedMusicVolume));
        return null;
    }

    public object SetMusicVolumeFromPlayerOptions()
    {
        SetMusicVolume(PlayerOptions.musicVolume);
        return null;
    }

    public object SetMusicVolume(float fNewVolume)
    {
        if (fNewVolume <= kVolumeMuteThreshold)
        {
            fNewVolume = -100;
        }
        if (!appHasFocus && PlayerOptions.audioOffWhenMinimized)
        {
            // Nothing
            //Debug.Log("App does not have focus while setting volume to " + fNewVolume + " so we will buffer that value and set it when the app gains focus.");
        }
        else
        {
            mixer.SetFloat("MusicVolume", fNewVolume);
        }

        bufferMusicVolume = fNewVolume;

        return null;
    }

    public object SetSFXVolumeFromHundredBasedPlayerOptions(bool bPlayTestSound = false)
    {
#if UNITY_PS4 || UNITY_XBOXONE
        //set SFXVolume in options in order to save our change of volume
        PlayerOptions.SFXVolume = (int)Convert0to100IntToDBFloat(PlayerOptions.hundredBasedSFXVolume);
#endif

        return SetSFXVolume(Convert0to100IntToDBFloat(PlayerOptions.hundredBasedSFXVolume), bPlayTestSound);
    }

    public object SetSFXVolume(float volume, bool bPlayTestSound)
    {
        mixer.SetFloat("SFXVolume", volume);

        if (bPlayTestSound)
        {
            string s = "";
            switch (UnityEngine.Random.Range(0, 8))
            {
                case 0:
                    s = "Ice Shatter";
                    break;
                case 1:
                    s = "Heavy Learn";
                    break;
                case 2:
                    s = "Buy Item";
                    break;
                case 3:
                    s = "Equip Item";
                    break;
                case 4:
                    s = "Whirlwind";
                    break;
                case 5:
                    s = "CookingSuccess";
                    break;
                case 6:
                    s = "Pickup";
                    break;
                case 7:
                    s = "Roll Dice";
                    break;
            }
            UIManagerScript.PlayCursorSound(s);
        }

        return null;
    }

    public object SetSFXVolumeFromPlayerOptions(bool bPlayTestSound = false)
    {
        float integerAmt = UIManagerScript.GetSFXVolume();
        if (integerAmt == 1f)
        {
            //integerAmt = PlayerPrefs.GetInt("SFXVolume");
            integerAmt = PlayerOptions.SFXVolume;
        }

        integerAmt = (1 * integerAmt);
        if (integerAmt == -30)
        {
            integerAmt = -100;
        }

        return SetSFXVolume(integerAmt, bPlayTestSound);
    }

    public object SetFootstepsVolumeFromHundredBasedPlayerOptions(bool bPlayTestSound = false)
    {
#if UNITY_PS4 || UNITY_XBOXONE
        //set footstepsVolume in options in order to save our change of volume
        PlayerOptions.footstepsVolume = (int)Convert0to100IntToDBFloat(PlayerOptions.hundredBasedFootstepsVolume);
#endif

        return SetFootstepsVolume(Convert0to100IntToDBFloat(PlayerOptions.hundredBasedFootstepsVolume), bPlayTestSound);
    }

    public object SetFootstepsVolume(float volume, bool bPlayTestSound = false)
    {
        if (volume >= 0f) volume = 0f;
        mixer.SetFloat("FootstepsVolume", volume);

        if (bPlayTestSound)
        {
            GameMasterScript.heroPCActor.myMovable.GetComponent<AudioStuff>().PlayCue("Footstep");
        }

        return null;
    }

    public object SetFootstepsVolumeFromPlayerOptions(bool bPlayTestSound = false)
    {
        float integerAmt = UIManagerScript.GetFootstepsVolume();

        if (integerAmt == 1f)
        {
            integerAmt = PlayerOptions.footstepsVolume;
        }
        //float 
        integerAmt = (1 * integerAmt);
        if (integerAmt == -30)
        {
            integerAmt = -100;
        }

        return SetFootstepsVolume(integerAmt, bPlayTestSound);
    }

    IEnumerator WaitThenChangeExternalFadeState(float time, int index, bool state)
    {
        yield return new WaitForSeconds(time);
        if (IsCrossfading) yield break;
        Debug.Log("Setting fade state of " + index + " to " + state);
        IntroloopPlayer.InstanceID(index).SetExternalFadeState(state, 1f);
    }

    public void FadeoutThenSetAllToZero(float time)
    {
        if (!PlatformVariables.USE_INTROLOOP)
        {
            StartCoroutine(ICrossfadeMusic_NoIntroloop(0, 1, 0f, 0f, time));
            StartCoroutine(WaitThenSetAllVolumeToZeroAndStopTracks(time));
            return;
        }

        //if (Debug.isDebugBuild) Debug.Log("Fade out all then set to zero over " + time);

        // Wait let's verify that the right track is set to active
        float activeTrackVol = IntroloopPlayer.InstanceID(activeChannel).GetCurrentVolume();
        if (activeTrackVol < 0.1f)
        {
            activeChannel = 1 - activeChannel;
        }

        startFadeTime = Time.time;
        isFading = true;
        IsCrossfading = false;
        track1FullyFadedOut = false;
        track2FadingIn = false;
        track2FullyFadedIn = false;
        fadeTime = time;
        StartCoroutine(WaitThenSetAllVolumeToZeroAndStopTracks(time + 0.01f));
    }

    IEnumerator WaitThenSetAllVolumeToZeroAndStopTracks(float time)
    {
        yield return new WaitForSeconds(time);
        SetAllVolumeToZero();

        if (PlatformVariables.USE_INTROLOOP)
        {
            IntroloopPlayer.InstanceID(0).StopAllTracks();
            IntroloopPlayer.InstanceID(1).StopAllTracks();
        }
        else
        {
            StopAllMusic();
        }
    }

    public void Fadeout(float time)
    {
        if (!PlatformVariables.USE_INTROLOOP)
        {
            //fade both channels to zero
            StartCoroutine(ICrossfadeMusic_NoIntroloop(0, 1, 0f, 0f, time));
            StartCoroutine(WaitThenSetAllVolumeToZeroAndStopTracks(time));
            return;
        }

        startFadeTime = Time.time;
        isFading = true;
        IsCrossfading = false;
        track1FullyFadedOut = false;
        track2FadingIn = false;
        track2FullyFadedIn = false;
        fadeTime = time;
    }

    private void Crossfade_WithIntroloop(int track1, int track2, float time)
    {
        startFadeTime = Time.time;
        IsCrossfading = true;
        track1FullyFadedOut = false;
        track2FadingIn = false;
        track2FullyFadedIn = false;
        activeChannel = track2;
        preCrossfadeTrack1Volume = GetMusicVolume(activeChannel);
        preCrossfadeTrack2Volume = GetMusicVolume(1 - activeChannel);

        IntroloopPlayer.InstanceID(activeChannel).SetExternalFadeState(true, 1f);
        IntroloopPlayer.InstanceID(1 - activeChannel).SetExternalFadeState(true, 0f);

        ActuallySetClipVolume(musicChannels[activeChannel], track2, 0f); // new 1/28 to avoid blipz

        //Debug.Log(musicTracksLoaded[track1].refName + " " + musicTracksLoaded[track2].refName + " for tracks1, 2");
    }

    private IEnumerator ICrossfadeMusic_NoIntroloop(int idxOld, int idxNew, float fOldDestVolume, float fNewDestVolume, float fFadeTime, float fDelayPercentBeforeFadingInNewTrack = 0f)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Request crossfade " + fDelayPercentBeforeFadingInNewTrack);

        //if a fade is happening, just wait        
        if (IsCrossfading)
        {
            yield return new WaitWhile(() => IsCrossfading);
        }

        IsCrossfading = true;

        float fTime = 0f;
        float fOldVolumeStart = musicChannels[idxOld].volume;
        float fNewVolumeStart = musicChannels[idxNew].volume;

        float fDelayNewTrackTimer = fFadeTime * fDelayPercentBeforeFadingInNewTrack;
        
        if (Debug.isDebugBuild)
        {
                if (LogoSceneScript.debugMusic)
                {
                    Debug.Log("music: Crossfading from " + musicTracksLoaded[idxOld].refName + " to " +
                            musicTracksLoaded[idxNew].refName + " in " + fFadeTime + "s after delay of " + fDelayNewTrackTimer +
                            "s.");
                }
        }
                                  
        float fTimeAtStart = Time.realtimeSinceStartup;
        int iSteps = 0;

        //Debug.Log(fTime + " " + fFadeTime + " " + fDelayNewTrackTimer + " " + fDelayPercentBeforeFadingInNewTrack);

        //added this isCrossfading check because it may be turned off elsewhere while this
        //coroutine is running.

        while (fTime < fFadeTime + fDelayNewTrackTimer && IsCrossfading)
        {
            musicChannels[idxOld].volume = Mathf.Lerp(fOldVolumeStart, fOldDestVolume, fTime / fFadeTime);

            //new one might fade in late
            if (fTime > fDelayNewTrackTimer)
            {
                musicChannels[idxNew].volume = Mathf.Lerp(fNewVolumeStart, fNewDestVolume, fTime - fDelayNewTrackTimer / fFadeTime);
            }
            fTime += Time.deltaTime;
            iSteps++;
            yield return null;
        }

        //at the end, force values to their destinations
        musicChannels[idxOld].volume = fOldDestVolume;
        musicChannels[idxNew].volume = fNewDestVolume;

        if (LogoSceneScript.debugMusic && Debug.isDebugBuild) 
        {
            Debug.Log("music: Crossfade took " + (Time.realtimeSinceStartup - fTimeAtStart) + "s over " + iSteps + " steps. ");
        }
        

        //make sure we stop at the end of the crossfade.
        if (fOldDestVolume == 0f)
        {
            //record the stopping times of the tracks.
            allMusicTrackPositions[musicTracksLoaded[idxOld].refName] = musicChannels[idxOld].timeSamples;

            //and hush
            musicChannels[idxOld].Stop();
            //Debug.Log("Stopped " + idxOld);
            //if (musicChannels[idxOld].clip != null) Debug.Log(musicChannels[idxOld].clip.name + " was stopped");
        }

        IsCrossfading = false;

        if (fNewDestVolume == 0)
        {
            lastFadeSetVolumeToZero = true;
        }
    }

    public void ActuallySetClipVolume(AudioSource aSource, int index, float volume)
    {
        string cueName = "";
        if (aSource.clip == null)
        {
            cueName = musicTracksLoaded[index].trackFileName;
        }
        else
        {
            cueName = aSource.clip.name;
        }
        //string cueName = aSource.clip.name;
        IntroloopAudio ila;

        if (dictIntroLoopAssets.TryGetValue(cueName, out ila))
        {
            //Debug.Log("<color=yellow>Set volume track " + index + " to " + volume + " " + musicTracksLoaded[index].refName + "</color>");
            IntroloopPlayer.InstanceID(index).SetVolumeAllSources(volume);
        }
        else
        {
            aSource.Stop();
        }
    }

    /// <summary>
    /// Takes a 0 to 100 option range and gives us a -30db to 0db value.
    /// </summary>
    /// <param name="iValue">A number between 0 and 100</param>
    /// <returns>A more different number between -30 and 0</returns>
    static float Convert0to100IntToDBFloat(int iValue)
    {
        return -30f + iValue / 10 * 3f;
    }
}
