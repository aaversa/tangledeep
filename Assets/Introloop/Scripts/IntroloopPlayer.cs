/* 
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP 
/// http://www.exceed7.com/introloop
*/

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class IntroloopPlayer : MonoBehaviour
{
    //private IntroloopTrack[] twoTracks;
    IntroloopTrack myTrack;
    private float towardsVolume;
    private bool willStop;
    private bool willPause;
    private float fadeLength;
    //Will change to 0 on first Play(), 0 is the first track.
    private int currentTrack = 1;
    //This fade is inaudible, it helps removing loud pop/click when you stop song suddenly. (0 fade length)
    private const float popRemovalFadeTime = 0.055f;

    private static IntroloopPlayer instance;
    private static IntroloopPlayer instance2; // new code lol

    AudioSource[] mySources;

    public bool externalFade;

    static IntroloopPlayer[] instances;

    internal IntroloopSettings introloopSettings;

    /// <summary>
    /// Singleton pattern point of access. Get the instance of IntroloopPlayer.
    /// </summary>
    public static IntroloopPlayer Instance {
        get {
            if (instance == null) {
                instance = (IntroloopPlayer)FindObjectOfType(typeof(IntroloopPlayer));
                if (instance == null)
                {
                    //Try loading a template prefab
                    GameObject templatePrefab = Resources.Load<GameObject>(IntroloopSettings.defaultTemplatePath);

                    GameObject introloopPlayer;
                    if (templatePrefab != null)
                    {
                        introloopPlayer = Instantiate(templatePrefab);
                        introloopPlayer.name = IntroloopSettings.defaultGameObjectName;
                    }
                    else
                    {
                        //Create a new one in hierarchy, and this one will persist throughout the game/scene too.
                        introloopPlayer = new GameObject(IntroloopSettings.defaultGameObjectName);
                        introloopPlayer.AddComponent<IntroloopPlayer>(); //This point Awake will be called
                    }

                    instance = introloopPlayer.GetComponent<IntroloopPlayer>();
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Another point of access which allows you to create and control multiple instances of Introloop.
    /// Using ID 0 will translate to the singleton ones.
    /// </summary>
    public static IntroloopPlayer InstanceID(int id)
    {
        if (instances == null)
        {
            instances = new IntroloopPlayer[2];
            instances[0] = null;
            instances[1] = null;
        }

        if (id >= instances.Length) return null;

        if (instances[id] == null)
        {
            //Try loading a template prefab
            GameObject templatePrefab = Resources.Load<GameObject>(IntroloopSettings.defaultTemplatePath);

            GameObject introloopPlayer;
            if (templatePrefab != null)
            {
                introloopPlayer = Instantiate(templatePrefab);
                introloopPlayer.name = IntroloopSettings.defaultGameObjectName + " " + id;
            }
            else
            {
                //Create a new one in hierarchy, and this one will persist throughout the game/scene too.
                introloopPlayer = new GameObject(IntroloopSettings.defaultGameObjectName + " " + id);
                introloopPlayer.AddComponent<IntroloopPlayer>(); //This point Awake will be called
            }

            instance = introloopPlayer.GetComponent<IntroloopPlayer>();
            instances[id] = instance;
        }

        return instances[id];
    }

    public void LoadAudio(AudioClip clip)
    {
        //Debug.Log("Instance " + gameObject.name + " loading " + clip.name);
        for (int i = 0; i < mySources.Length; i++)
        {
            if (mySources[i].clip != null)
            {
                mySources[i].clip.UnloadAudioData();
            }            
            mySources[i].clip = clip;
        }
    }

    public void SetExternalFadeState(bool state, float intendedVolume)
    {
        externalFade = state;
        myTrack.SetExternalFadeState(state);
        towardsVolume = intendedVolume;
        //Debug.Log(gameObject.name + " ext fade state is now " + state);
    }

    public void StopAllTracks()
    {
        for (int i = 0; i < mySources.Length; i++)
        {
            if (mySources[i].clip != null) mySources[i].Stop();
        }
    }
    public AudioClip GetClip() 
    {
        for (int i = 0; i < mySources.Length; i++)
        {
            if (mySources[i].clip != null) return mySources[i].clip;
        }
        return null;
    }

    public int GetTimeSamplesOfCurrent()
    {
        return myTrack.PlayheadPositionSamples;
    }

    public void SetTimeSamples(int amount) // is this bad?!
    {
        for (int i = 0; i < mySources.Length; i++)
        {
            mySources[i].timeSamples = amount;            
            //Debug.Log("Audio source index " + i + " " + gameObject.name + " now has " + mySources[i].timeSamples);
        }
    }

    public bool IsPlaying()
    {
        for (int i = 0; i < mySources.Length; i++)
        {
            if (mySources[i].isPlaying)
            {
                return true;
            }
        }
        return false;
    }
		
	void Awake()
	{	
        //Check for duplicates in the scene
        if (instances == null)
        {
            InstanceID(0);
        }

		/* UnityEngine.Object[] introloopPlayers = FindObjectsOfType(typeof(IntroloopPlayer));
		for(int i = 0; i < introloopPlayers.Length; i++) {

            bool deleteThis = true;
            for (int x = 0; x < instances.Length; x++)
            {
                if (introloopPlayers[i] == instances[x])
                {
                    deleteThis = false;
                }
            }

            //if (introloopPlayers[i] != this) { //Not conform to singleton pattern
            if (deleteThis) { 
				//Self destruct!
				Destroy(gameObject);
			}
		} */

        //DontDestroyOnLoad(gameObject);

        introloopSettings = gameObject.GetComponent<IntroloopSettings>();
        if(introloopSettings == null)
        {
            introloopSettings = gameObject.AddComponent<IntroloopSettings>();
        }
		
		//towardsVolume = new float[2];
		//willStop = new bool[2];
		//willPause = new bool[2];
        //twoTracks = new IntroloopTrack[2];
        myTrack = new IntroloopTrack();
        //fadeLength = new float[2]{introloopSettings.defaultFadeLength,introloopSettings.defaultFadeLength};
        fadeLength = introloopSettings.defaultFadeLength;

        CreateImportantChilds();

        mySources = GetComponentsInChildren<AudioSource>();
	}
	
    public void SetVolumeAllSources(float vol)
    {
        for (int i = 0; i < mySources.Length; i++)
        {            
            mySources[i].volume = vol;            
        }
    }

    public void SetInternalFadeVolume(float vol)
    {
        myTrack.FadeVolume = vol;
        towardsVolume = vol;
    }

	void CreateImportantChilds()
	{
        //These are all the components that make this plugin works. Basically 4 AudioSources with special control script
        //to juggle music file carefully, stop/pause/resume gracefully while retaining the Introloop function.

		Transform musicPlayerTransform = transform;
		GameObject musicTrack1 = new GameObject();
		musicTrack1.AddComponent<IntroloopTrack>();
		musicTrack1.name = "Music Track 1";
		musicTrack1.transform.parent = musicPlayerTransform;

        myTrack = musicTrack1.GetComponent<IntroloopTrack>();
        myTrack.SetMixerGroup(introloopSettings.routeToMixerGroup);
        /* twoTracks[0] = musicTrack1.GetComponent<IntroloopTrack>();
        twoTracks[0].SetMixerGroup(introloopSettings.routeToMixerGroup);
		
		GameObject musicTrack2 = new GameObject();
		musicTrack2.AddComponent<IntroloopTrack>();
		musicTrack2.name = "Music Track 2";
		musicTrack2.transform.parent = musicPlayerTransform;
		twoTracks[1] = musicTrack2.GetComponent<IntroloopTrack>();
        twoTracks[1].SetMixerGroup(introloopSettings.routeToMixerGroup); */
	}

	void Update()
	{
        //if (externalFade) return;
		FadeUpdate();
	}

    private void FadeUpdate()
    {
        //For two main tracks      

        float towardsVolumeBgmVolumeApplied = towardsVolume;
        if (myTrack.FadeVolume != towardsVolumeBgmVolumeApplied)
        { //Fade in/out
            if (fadeLength == 0)
            {                
            myTrack.FadeVolume = towardsVolumeBgmVolumeApplied;
            }
            else
            {
                if (myTrack.FadeVolume > towardsVolumeBgmVolumeApplied)
                {                        
                    myTrack.FadeVolume -= Time.deltaTime / fadeLength;
                    if (myTrack.FadeVolume <= towardsVolumeBgmVolumeApplied)
                    {
                    //Stop the fade
                    myTrack.FadeVolume = towardsVolumeBgmVolumeApplied;
                    }
                }
                else
                {
                    myTrack.FadeVolume += Time.deltaTime / fadeLength;
                    if (myTrack.FadeVolume >= towardsVolumeBgmVolumeApplied)
                    {
                    //Stop the fade
                    myTrack.FadeVolume = towardsVolumeBgmVolumeApplied;
                    }
                }
            }

            if (!externalFade)
            {
                //Stop check
                if (willStop && myTrack.FadeVolume == 0)
                {
                    willStop = false;
                    willPause = false;
                    myTrack.Stop();
                    //UnloadTrack();
                }
                //Pause check
                if (willPause && myTrack.FadeVolume == 0)
                {
                    willStop = false;
                    willPause = false;
                    myTrack.Pause();
                    //don't unload!
                }
            }
        }
        
    }

    /* private void FadeUpdate()
	{
		//For two main tracks
		for(int i=0; i< 2; i++) {
			float towardsVolumeBgmVolumeApplied = towardsVolume[i];
			if(twoTracks[i].FadeVolume != towardsVolumeBgmVolumeApplied) { //Fade in/out
				if(fadeLength[i] == 0) {
					twoTracks[i].FadeVolume = towardsVolumeBgmVolumeApplied;
				} else {
					if(twoTracks[i].FadeVolume > towardsVolumeBgmVolumeApplied) {
						twoTracks[i].FadeVolume -= Time.deltaTime / fadeLength[i];
						if(twoTracks[i].FadeVolume <= towardsVolumeBgmVolumeApplied) {
							//Stop the fade
							twoTracks[i].FadeVolume = towardsVolumeBgmVolumeApplied;
						}
					} else {
						twoTracks[i].FadeVolume += Time.deltaTime / fadeLength[i];
						if(twoTracks[i].FadeVolume >= towardsVolumeBgmVolumeApplied) {
							//Stop the fade
							twoTracks[i].FadeVolume = towardsVolumeBgmVolumeApplied;
						}
					}
				}
				//Stop check
				if(willStop[i] && twoTracks[i].FadeVolume == 0) {
					willStop[i] = false;
					willPause[i] = false;
					twoTracks[i].Stop();
					UnloadTrack(i);
				}
				//Pause check
				if(willPause[i] && twoTracks[i].FadeVolume == 0) {
					willStop[i] = false;
					willPause[i] = false;
					twoTracks[i].Pause();
					//don't unload!
				}
			}
		}
	} */

    private void UnloadTrack(int trackNumber)
	{
        /* 
        //Have to check if other track is using the music or not?
        //If playing the same song again,
        //the loading of the next song might come earlier, then got immediately unloaded by this.

        //Also check for when using different IntroloopAudio with the same source file.
        //In this case .Music will be not equal, but actually the audioClip inside is the same song.

        bool musicEqualCurrent = (myTrack.Music == twoTracks[(trackNumber + 1) % 2].Music);
        bool clipEqualCurrent = (twoTracks[trackNumber].Music != null && twoTracks[(trackNumber + 1) % 2].Music != null) &&
		 (twoTracks[trackNumber].Music.audioClip == twoTracks[(trackNumber + 1) % 2].Music.audioClip);
        bool isSameSongAsCurrent = musicEqualCurrent || clipEqualCurrent;

		bool musicEqualNext = (twoTracks[trackNumber].Music == twoTracks[(trackNumber + 1) % 2].MusicAboutToPlay);
		bool clipEqualNext = (twoTracks[trackNumber].Music != null && twoTracks[(trackNumber + 1) % 2].MusicAboutToPlay != null) &&
		(twoTracks[trackNumber].Music.audioClip == twoTracks[(trackNumber + 1) % 2].MusicAboutToPlay.audioClip);
			
        bool isSameSongAsAboutToPlay = musicEqualNext || clipEqualNext;

        bool usingAndPlaying = twoTracks[(trackNumber + 1) % 2].IsPlaying && isSameSongAsCurrent;

        if (!usingAndPlaying && !isSameSongAsAboutToPlay)
        {
            //If not, it is now safe to unload it
            //Debug.Log("Unload");
            twoTracks[trackNumber].Unload();
        } */
    }
	
	internal void ApplyVolumeSettingToAllTracks()
	{
        myTrack.ApplyVolume();
		//twoTracks[0].ApplyVolume();
		//twoTracks[1].ApplyVolume();
	}

    public float GetCurrentVolume()
    {
        return myTrack.GetVolume();
    }

    /// <summary>
    /// Play the audio using settings specified in IntroloopAudio file's inspector area.
    /// </summary>
    /// <param name="audio"> An IntroloopAudio asset file to play.</param>
	public void Play(IntroloopAudio audio, int delayInSamples)
	{
		PlayFade(audio, 0.05f, delayInSamples);
	}
	
    /// <summary>
    /// Play the audio using settings specified in IntroloopAudio file's inspector area with fade-in 
    /// or cross fade (if other IntroloopAudio is playing now) default length specified in IntroloopSettings component
    /// that is attached to IntroloopPlayer.
    /// </summary>
    /// <param name="audio"> An IntroloopAudio asset file to play.</param>
	public void PlayFade(IntroloopAudio audio, int delayInSamples)
	{
		PlayFade(audio, introloopSettings.defaultFadeLength, delayInSamples);
	}
	
    /// <summary>
    /// Play the audio using settings specified in IntroloopAudio file's inspector area with fade-in 
    /// or cross fade (if other IntroloopAudio is playing now) length specified by argument.
    /// </summary>
    /// <param name="audio"> An IntroloopAudio asset file to play.</param>
    /// <param name="fadeLengthSeconds"> Fade in/Cross fade length to use.</param>
	public void PlayFade(IntroloopAudio audio, float fadeLengthSeconds, int delayInSamples)
	{
		//Auto-crossfade old ones. If no fade length specified, a very very small fade will be used to avoid pops/clicks.
		StopFade(fadeLengthSeconds== 0 ? popRemovalFadeTime : fadeLengthSeconds); 
		
		int next = (currentTrack + 1) % 2;
        //twoTracks[next].Play(audio,fadeLengthSeconds == 0 ? false : true);

        myTrack.Play(audio, fadeLengthSeconds == 0 ? false : true, delayInSamples);

        towardsVolume = 1;
		fadeLength = fadeLengthSeconds;
		
		currentTrack = next;
	}
	
    /// <summary>
    /// Stop the currently playing IntroloopAudio instantly, and unload the audio from memory.
    /// </summary>
	public void Stop()
	{
		StopFade(popRemovalFadeTime);
	}
	
    /// <summary>
    /// Stop the currently playing IntroloopAudio with fade out length specified by
    /// default length specified in IntroloopSettings component. Unload the audio from memory once
    /// the fade out finished.
    /// </summary>
	public void StopFade()
	{
		StopFade(introloopSettings.defaultFadeLength);
	}
	
    /// <summary>
    /// Stop the currently playing IntroloopAudio with fade out length specified by
    /// argument. Unload the audio from memory once the fade out finished.
    /// </summary>
    /// <param name="fadeLengthSeconds">Fade out length to use.</param>
	public void StopFade(float fadeLengthSeconds)
	{
		willStop = true;
		willPause = false;
		fadeLength = fadeLengthSeconds;
		towardsVolume = 0;
	}

    /// <summary>
    /// Stop the currently playing IntroloopAudio instantly without unloading,
    /// you will be able to use Resume() to continue later.
    /// </summary>
	public void Pause()
	{
		PauseFade(popRemovalFadeTime);
	}
	
    /// <summary>
    /// Stop the currently playing IntroloopAudio without unloading,
    /// with fade length specified by default length in IntroloopSettings component.
    /// You will be able to use Resume() to continue later.
    /// </summary>
	public void PauseFade()
	{
		PauseFade(introloopSettings.defaultFadeLength);
	}
	
    /// <summary>
    /// Stop the currently playing IntroloopAudio without unloading,
    /// with fade length specified by the argument.
    /// You will be able to use Resume() to continue later.
    /// </summary>
    /// <param name="fadeLengthSeconds">Fade out length to use.</param>
	public void PauseFade(float fadeLengthSeconds)
	{
        if(myTrack.IsPausable())
        {
            willPause = true;
            willStop = false;
            fadeLength = fadeLengthSeconds;
            towardsVolume = 0;
        }
	}
	
    /// <summary>
    /// Resume playing of previously paused IntroloopAudio instantly.
    /// </summary>
	public void Resume()
	{
		ResumeFade(0);
	}
	
    /// <summary>
    /// Resume playing of previously paused IntroloopAudio with fade in length
    /// specified in IntroloopSettings component.
    /// </summary>
	public void ResumeFade()
	{
		ResumeFade(introloopSettings.defaultFadeLength);
	}
	
    /// <summary>
    /// Resume playing of previously paused IntroloopAudio with fade in length
    /// specified by argument.in IntroloopSettings component.
    /// </summary>
    /// <param name="fadeLengthSeconds">Fade out length to use.</param>
	public void ResumeFade(float fadeLengthSeconds)
	{
		if(myTrack.Resume()) {
			//Resume success
			willStop = false;
			willPause = false;
			towardsVolume = 1;
			fadeLength = fadeLengthSeconds;	
		}
	}
	
    /// <summary>
	// An experimental feature in the case that you really want the audio to start in an instant you call Play.
	// By normally using Play and Stop it loads the audio the moment you called Play. It will introduces a
	// small delay in the case of a large audio file.
	// But by using this before actually calling Play it will be instant. However be aware that RAM is managed less efficiently in the following case.
	// Normally Introloop immediately unloads the previous track to minimize memory, but if you use Preload then 
	// did not call Play with the same IntroloopAudio afterwards, the loaded memory will be unmanaged. 
	// (Just like if you tick "Preload Audio Data" on your clip and have them in a hierarchy somewhere, then did not use it.)
    /// </summary>
	public void Preload(IntroloopAudio audio)
	{
        if (audio == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Tried preloading null audio!");
            return;
        }
		audio.Preload();
	}

    public float GetClipLength()
    {
        if (mySources[0].clip == null) return 0f;
        return mySources[0].clip.length;
    }

    //These 3 functions is for debugging purpose.

    public float GetTime()
    {
        return myTrack.PlayheadPositionSeconds;
    }
	
	public string[] GetDebugInformation1()
	{
		return myTrack.DebugInformation;
	}

	public string[] GetDebugInformation2()
	{
		return myTrack.DebugInformation;
	}
}
