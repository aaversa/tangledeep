/*
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP
/// http://www.exceed7.com/introloop
*/

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System;

public class IntroloopTrack : MonoBehaviour
{

	/* ================================================================================================================
	 Note : UNITY (somewhat hidden) AUDIO RULES (Note to self/ to those who that wanted to edit it)
     (This is from experiments from Unity 3.5 era, I am not sure if now the mechanics changed or not)

	 1. When Stop(),Pause() the PLAY/STOP schedule that met while stopping will be remembered
	 	but won't execute UNTIL you call Play().
	 2. AudioSettings.dspTime is time-critical. A couple lines of code and call dspTime again, it's different!
	 3. According to 1. if you run a new schedule while Stop(), Pause().. the remembered schedule will be.......
	 3.1 If STOPschedule is remembered, your newly scheduled PLAYschedule WILL NOT win.
	 	 After your playSchedule execute the stopSchedule will follows. (stopSchedule always comes last?)
	 4. AudioListerner.pause = true IS THE ONLY WAY to stop AudioSettings.dspTime so no worries when pausing like this :D
	 5. [audioSource].isPlaying will becomes TRUE after called PlayScheduled() (even if the schedule is not met yet!)
	 6. [audioSource].Stop() does not always reset the [audioSource].time to 0, but instead reset to whatever the last
	 	[audioSource].time that you set to. (Which is default to 0!) / .Pause() will freeze .time at that instance.
	 7. According to 5. and 1. , Stop() and Pause() will override .isPlaying to FALSE, regardless of schedule or remembered
	 	schedule.
	 8. Calling PlaySchedule while pausing can start the AudioSource (not calling any Play()) BUT another PlaySchedule
	 	that have been met while pausing will not be overrided by new PlaySchedule's time. You must use SetScheduleStartTime
	 	to reschedule it, alongside with PlaySchedule to start the audio. Using Play() instead of PlaySchedule is untested.
	================================================================================================================ */

	private AudioSource[] twoSources;
	private IntroloopAudio music;

    bool externalFadeActive;

	internal IntroloopAudio Music {
		get {
			return this.music;
		}
	}

    //This is used by IntroloopPlayer to check about unloading.
	private IntroloopAudio musicAboutToPlay;
	internal IntroloopAudio MusicAboutToPlay {
		get {
			return this.musicAboutToPlay;
		}
	}

	private double nextScheduleTime;

    private double playDspTimestamp;
    private double pauseDspTimestamp;

    //When resume, this variable will add up how long we have paused the audio. Used in determining total playtime.
    private double pauseDspTotal;

	private int nextSourceToPlay = 0;
	private double rememberHowFarFromNextScheduleTime;

    //There is one case that isPlaying is not correct, if non-looping music ends, this isPlaying is not updated to "false".
    //In other case, music can only be stoped by user's command so I can set it to false at that moment.
    //This is intentional, since it is not required for anything else and I don't want to waste a Coroutine just to
    //correctly update this.
	private bool isPlaying = false;

    internal bool IsPlaying
    {
        get{
            return isPlaying;
        }
    }

    internal float PlayedTimeSeconds
    {
        get
        {
            double currentDspPlayhead = 0;
            if(!isPlaying && !isPausing)
            {
                return 0;
            }
            else if(isPausing || !isPlaying)
            {
                currentDspPlayhead = pauseDspTimestamp;
            }
            else
            {
                currentDspPlayhead = AudioSettings.dspTime;
            }
            return (float)(currentDspPlayhead - playDspTimestamp - pauseDspTotal);
        }
    }

    public int PlayheadPositionSamples
    {
        get
        {
            if (music == null)
            {
                return 0;
            }
            else
            {
                return twoSources[1 - nextSourceToPlay].timeSamples;
            }
        }
    }

    public float GetVolume()
    {
        //Debug.Log(twoSources[0].isPlaying + " " + twoSources[1].isPlaying);

        for (int i = 0; i < twoSources.Length; i++)
        {
            if (twoSources[i].isPlaying)
            {
                return twoSources[i].volume;
            }
        }

        return 0f;
    }

    public float PlayheadPositionSeconds
    {
        get
        {
            if(music == null)
            {
                return 0;
            }
            float playedTime = PlayedTimeSeconds;
            if(!music.nonLooping && !music.loopWholeAudio) //If contain intro
            {
                //We think carefully..
                if(playedTime < music.IntroLength + music.LoopingLength)
                {
                    return playedTime;
                }
                else
                {
                    return music.IntroLength + ((playedTime - music.IntroLength) % music.LoopingLength);
                }
            }
            else
            {
                //We can modulo with length!
                return playedTime % music.ClipLength;
            }
        }
    }

	private bool isPausing = false;
	private bool isPausingOnIntro = false;
	//private bool isContainIntro = false;
	private double introFinishDspTime = 0;
    private int playingSourceBeforePause;

	private double dspPlusHalfAudio;
	private double source1WillPlay;
	private double source1WillEnd;
	private double source2WillPlay;
	private double source2WillEnd;

	public string[] DebugInformation {
		get {
			return new string[] {
				"Source 1 Will Play :" + source1WillPlay.ToString(".00"),
				"Source 1 Will End :" + source1WillEnd.ToString(".00"),
				"Source 2 Will Play :" + source2WillPlay.ToString(".00"),
				"Source 2 Will End :" + source2WillEnd.ToString(".00"),
				"Source 1 : " + (twoSources[0].isPlaying ? "PLAYING/SCHEDULED" : "STOPPED"),
				"Source 2 : " + (twoSources[1].isPlaying ? "PLAYING/SCHEDULED" : "STOPPED"),
				"Source 1 Time : " + twoSources[0].time.ToString(".00"),
				"Source 2 Time : " + twoSources[1].time.ToString(".00"),
				"Next schedule time : " + nextScheduleTime.ToString(".00"),
				"Dsp plus half audio : " + dspPlusHalfAudio.ToString(".00"),
				"Is pausing : " + isPausing.ToString()
			};
		}
	}

	private float fadeVolume = 0;

	internal float FadeVolume {        
		get {
			return this.fadeVolume;
		}
		set {
			float clampedValue = Mathf.Clamp01(value);
			fadeVolume = clampedValue;
            //Debug.Log(twoSources[0].clip.name + " volume intended to be set to " + fadeVolume);
            if (!externalFadeActive)
            {
                ApplyVolume();
            }
            
		}
	}

    public void SetExternalFadeState(bool state)
    {
        externalFadeActive = state;
    }

	void Awake()
	{
		twoSources = new AudioSource[2];

		Transform gameObTransform = gameObject.transform;

		GameObject sourceObject1 = new GameObject("Source1");
		AudioSource as1 = sourceObject1.AddComponent<AudioSource>();
		as1.bypassEffects = true;
		as1.bypassListenerEffects = true;
		as1.bypassReverbZones = true;
		as1.playOnAwake = false;
		as1.spatialBlend = 0;
		sourceObject1.transform.parent = gameObTransform;

		twoSources[0] = as1;

		GameObject sourceObject2 = new GameObject("Source2");
		AudioSource as2 = sourceObject2.AddComponent<AudioSource>();
		as2.bypassEffects = true;
		as2.bypassListenerEffects = true;
		as2.bypassReverbZones = true;
		as2.playOnAwake = false;
		as2.spatialBlend = 0;
		sourceObject2.transform.parent = gameObTransform;

		twoSources[1] = as2;

	}

    internal void SetMixerGroup(AudioMixerGroup mixerGroup)
    {
        twoSources[0].outputAudioMixerGroup = mixerGroup;
        twoSources[1].outputAudioMixerGroup = mixerGroup;
    }

	internal void Unload()
	{
        if (music == null)
        {
            return;
        }
        music.Unload();
        IntroloopLogger.Log(String.Format("Unloaded \"{0}\" from memory.",music.audioClip.name));
		twoSources[0].clip = null;
		twoSources[1].clip = null;
		music = null;
		musicAboutToPlay = null;
	}

	void Update()
	{
		if(isPlaying)
        {
            if(!music.nonLooping) //In the case of non-looping, no scheduling happen at all.
            {
                dspPlusHalfAudio = AudioSettings.dspTime + (music.LoopingLength/2f);
				if(dspPlusHalfAudio > nextScheduleTime) {
					//Schedule halfway of looping audio.
					ScheduleNextLoop();
				}
            }
		}
	}

	internal void Stop()
	{
		twoSources[0].Stop();
		twoSources[1].Stop();
        pauseDspTimestamp = AudioSettings.dspTime;

		//This is so that the schedule won't cancel the stop by itself
		isPlaying = false;
		isPausing = false;
	}

    internal bool IsPausable()
    {
        if(!isPlaying || (!twoSources[0].isPlaying && !twoSources[1].isPlaying))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

	internal void Pause()
	{
		if(!isPlaying)
        {
			return;
		}

        //Determining which one is playing?
        if(twoSources[0].isPlaying && twoSources[1].isPlaying)
        {
            //hard case, which one is not actually playing but scheduled?
            //(Scheduled audio reports isPlaying as true)

            if(AudioSettings.dspTime < source1WillPlay)
            {
                playingSourceBeforePause = 1;
            }
            else
            {
                playingSourceBeforePause = 0;
            }
        }
        else
        {
            //easy case
            if(twoSources[0].isPlaying)
            {
                playingSourceBeforePause = 0;
            }
            else
            {
                playingSourceBeforePause = 1;
            }
        }
		twoSources[0].Pause();
		twoSources[1].Pause();

		double pausingDspTime = AudioSettings.dspTime;
		rememberHowFarFromNextScheduleTime = nextScheduleTime - pausingDspTime;
        pauseDspTimestamp = pausingDspTime;

		if(!music.nonLooping && !music.loopWholeAudio) //If contain intro
		{
			if(pausingDspTime >= introFinishDspTime)
			{
				isPausingOnIntro = false;
			}
			else
			{
				isPausingOnIntro = true;
			}
		}

		//So the schedule won't cancel the stop by itself
		isPlaying = false;
		isPausing = true;
        IntroloopLogger.Log("\"" + music.audioClip.name + "\" paused.");
	}

	internal bool Resume()
	{
		if(!isPausing) {
			return false;
		}

		int sourceToContinuePlaying = playingSourceBeforePause;

		//Rescheduling!
		double absoluteTimeNow = AudioSettings.dspTime;

        pauseDspTotal += absoluteTimeNow - pauseDspTimestamp;

        float remainingTime;
		if(!music.nonLooping && !music.loopWholeAudio) //If contain intro
        {
            if(isPausingOnIntro)
            {
                remainingTime = music.IntroLength - (twoSources[sourceToContinuePlaying].time/music.Pitch);
            }
            else
            {
                remainingTime = music.IntroLength + music.LoopingLength - (twoSources[sourceToContinuePlaying].time/music.Pitch);
            }
        }
        else
        {
            remainingTime = music.ClipLength - (twoSources[sourceToContinuePlaying].time/music.Pitch);
        }

		//For current track
		SetScheduledEndTime(sourceToContinuePlaying, absoluteTimeNow + remainingTime); //Intro has no tail!

		//Order does not matter but both must exist.
		SetScheduledStartTime(sourceToContinuePlaying, absoluteTimeNow);
		PlayScheduled(sourceToContinuePlaying, absoluteTimeNow);

        if(!music.nonLooping)
        {
            //For next track
            SetScheduledStartTime((sourceToContinuePlaying + 1) % 2, absoluteTimeNow + remainingTime);
            PlayScheduled((sourceToContinuePlaying + 1) % 2, absoluteTimeNow + remainingTime );
        }

		if(isPausingOnIntro)
		{
            //For the case of pausing on intro too long (so long that the previously scheduled intro has finished)
			introFinishDspTime = absoluteTimeNow + remainingTime;
		}

		nextScheduleTime = AudioSettings.dspTime + rememberHowFarFromNextScheduleTime;

		isPlaying = true;
		isPausing = false;
        IntroloopLogger.Log("\"" + music.audioClip.name + "\" resumed.");
		return true;
	}

	internal void Play(IntroloopAudio music,bool isFade, int delayInSamples)
	{
        //Debug.Log("Playing from object " + gameObject.transform.parent.name);
        if (twoSources[0].clip == null) Debug.Log("But my source 0 is null.");
        if (twoSources[1].clip == null) Debug.Log("But my source 1 is null.");
        if (music == null)
        {
            Debug.Log("Asking " + gameObject.name + " " + gameObject.transform.parent.name + " to play null music.");
            return;
        }
        pauseDspTimestamp = 0;
        pauseDspTotal = 0;

		twoSources[0].pitch = music.Pitch;
		twoSources[1].pitch = music.Pitch;

        AudioDataLoadState loadState = music.audioClip.loadState;
        string musicName = music.audioClip.name;
        FadeVolume = isFade ? 0 : 1;
        if(loadState != AudioDataLoadState.Loaded)
        {
            IntroloopLogger.Log("\"" + musicName + "\" not loaded yet. Loading into memory...");
            StartCoroutine(LoadAndPlayRoutine(music, delayInSamples));
        }
        else
        {
            IntroloopLogger.Log("\"" + musicName + "\" already loaded in memory.");
            SetupPlayingSchedule(music, delayInSamples);
        }
	}

    private IEnumerator LoadAndPlayRoutine(IntroloopAudio music, int delayInSamples)
    {
        string musicName = music.audioClip.name;
        float startLoadingTime = Time.time;
        float endLoadingTime;
        music.audioClip.LoadAudioData();
        while(music.audioClip.loadState != AudioDataLoadState.Loaded && music.audioClip.loadState != AudioDataLoadState.Failed)
        {
            yield return null;
        }
        if(music.audioClip.loadState == AudioDataLoadState.Loaded)
        {
            endLoadingTime = Time.time;
            if(music.audioClip.loadInBackground)
            {
                IntroloopLogger.Log(musicName + " loading successful. (Took " + (endLoadingTime - startLoadingTime) + " seconds loading in background.)");
            }
            else
            {
                IntroloopLogger.Log(musicName + " loading successful.");
            }
            SetupPlayingSchedule(music, delayInSamples);
        }
        else
        {
            IntroloopLogger.LogError(musicName + " loading failed!");
        }
    }

	private void SetupPlayingSchedule(IntroloopAudio music, int delayInSamples)
	{
        //Unload();

		IntroloopLogger.Log("Playing \"" + music.audioClip.name + "\"");

		musicAboutToPlay = music;

		musicAboutToPlay = null;
		this.music = music;
        if (!externalFadeActive)
        {
            ApplyVolume();
        }        
		nextSourceToPlay = 0;
		isPausing = false;
        
        twoSources[0].clip = music.audioClip;
        twoSources[1].clip = music.audioClip;

		//Essential to cancel the Pause
		Stop();

		//PlayScheduled does not reset the playhead!
		twoSources[0].time = 0;
		twoSources[1].time = music.LoopBeginning; //Waiting at the intro part so it will go looping..

        double absoluteTimeNow = AudioSettings.dspTime;

        //double calcExtraTime = delayInSamples / 44100f;

        //Debug.Log("Adding " + calcExtraTime + " from " + delayInSamples);
        //absoluteTimeNow += calcExtraTime;

        if(music.nonLooping)
        {
            PlayScheduled(0,absoluteTimeNow);
            IntroloopLogger.Log("Type : Non-looping");
        }
        else if(music.loopWholeAudio)
        {
            PlayScheduled(0,absoluteTimeNow);
            SetScheduledEndTime(0,absoluteTimeNow + music.ClipLength);
            introFinishDspTime = absoluteTimeNow + music.ClipLength;

            PlayScheduled(1,absoluteTimeNow + music.ClipLength);
            nextScheduleTime = absoluteTimeNow + music.ClipLength*2;

            IntroloopLogger.Log("Type : Loop whole audio");
        }
        else {
            PlayScheduled(0, absoluteTimeNow);
            SetScheduledEndTime(0, absoluteTimeNow + music.IntroLength);
            introFinishDspTime = absoluteTimeNow + music.IntroLength;

            //Followed by looping track

            //If has intro but no looping, music will end and won't loop
            //But in this case it goes to looping track
            PlayScheduled(1, absoluteTimeNow + music.IntroLength);
            nextScheduleTime = (absoluteTimeNow + music.IntroLength) + (music.LoopingLength);
            IntroloopLogger.Log("Type : Introloop");
		}
        playDspTimestamp = absoluteTimeNow;
        pauseDspTimestamp = 0;
        pauseDspTotal = 0;
		isPlaying = true;
	}

	private void ScheduleNextLoop()
	{
		//note : (nextSourceToPlay + 1) % 2 is not always the same as "currently playing source" even though we have 2 tracks in total, because this "nextSourceToPlay" updates when next loop is "scheduled".

		SetScheduledEndTime((nextSourceToPlay + 1) % 2, nextScheduleTime);
		PlayScheduled(nextSourceToPlay, nextScheduleTime);
		twoSources[nextSourceToPlay].time = music.LoopBeginning;

        if(music.loopWholeAudio)
        {
            nextScheduleTime = nextScheduleTime + music.ClipLength;
        }
        else
        {
            nextScheduleTime = nextScheduleTime + music.LoopingLength;
        }

		nextSourceToPlay = (nextSourceToPlay + 1) % 2;
		//Debug.Log("IntroloopTrack : Next loop scheduled.");
	}

	internal void ApplyVolume()
	{
        for (int i = 0; i < twoSources.Length; i++) {
			if(music != null) {
                //if (twoSources[i].volume >= 0.99f) continue; // Don't 'blip' the audio if its already at max?
                twoSources[i].volume = FadeVolume * music.Volume; 
                /* if (twoSources[i].clip != null)
                {
                    Debug.Log(twoSources[i].clip.name + " volume set to " + twoSources[i].volume);
                }  */
			}
		}
	}

	private void PlayScheduled(int sourceNumber, double absoluteTime)
	{
        //Debug.Log("Source " +  sourceNumber + " play at " + absoluteTime);
        // #todo - here is where we may need to insert our timeSamples call?
        twoSources[sourceNumber].PlayScheduled(absoluteTime);
		if(sourceNumber == 0) {
			source1WillPlay = absoluteTime;
		}
		else {
			source2WillPlay = absoluteTime;
		}
	}

	private void SetScheduledEndTime(int sourceNumber, double absoluteTime)
	{
		twoSources[sourceNumber].SetScheduledEndTime(absoluteTime);
		if(sourceNumber == 0) {
			source1WillEnd = absoluteTime;
		}
		else {
			source2WillEnd = absoluteTime;
		}
	}

	private void SetScheduledStartTime(int sourceNumber, double absoluteTime)
	{
		twoSources[sourceNumber].SetScheduledStartTime(absoluteTime);
		if(sourceNumber == 0) {
			source1WillPlay = absoluteTime;
		}
		else {
			source2WillPlay = absoluteTime;
		}
	}

}
