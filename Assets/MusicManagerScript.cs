using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using AssetBundles;

#if UNITY_SWITCH
	using nn.fs;
#endif

#if UNITY_EDITOR
	using UnityEditor;
#endif

public enum MixerGroupNames { PLAYERSFX, MONSTERSFX, OBJECTSSFX, COMBATSFX, UISFX, FOOTSTEPSSFX }



[System.Serializable]
public partial class MusicManagerScript : MonoBehaviour
{
    public int GLOBAL_SFX_VOICE_LIMIT;

    public float minThresholdDoNotPlay; // good value: 0.08

	// Switch does not use the below
    public IntroloopAudio townThemeILA;
    public IntroloopAudio titleThemeILA;
    public IntroloopAudio dungeonTheme1ILA;
    public IntroloopAudio dungeonTheme2ILA;
    public IntroloopAudio dungeonTheme3ILA;
    public IntroloopAudio dungeonTheme4ILA;
    public IntroloopAudio dungeonTheme5ILA;
    public IntroloopAudio dungeonTheme7ILA;
    public IntroloopAudio bossTheme1ILA;
    public IntroloopAudio easydungeonILA;
    public IntroloopAudio gameoverILA;
    public IntroloopAudio grovethemeILA;
    public IntroloopAudio funmerchantILA;
    public IntroloopAudio bossvictoryILA;
    public IntroloopAudio postbossILA;
    public IntroloopAudio passagewayILA;
    public IntroloopAudio casinoILA;
    public IntroloopAudio villainousILA;
    public IntroloopAudio bosstheme2ILA;
    public IntroloopAudio desertthemeILA;
    public IntroloopAudio endingthemeILA;
    public IntroloopAudio finalboss2ILA;
    public IntroloopAudio sharatheme1ILA;
    public IntroloopAudio restthemeILA;
    public IntroloopAudio sharaSeriousILA;
    public IntroloopAudio sadnessILA;
    public IntroloopAudio dimRiffBossILA;
    public IntroloopAudio dimRiftLevelILA;
    public IntroloopAudio jobTrialILA;
    public IntroloopAudio charCreationILA;
    public IntroloopAudio finalLevelsILA;
    public IntroloopAudio groveReduxILA;
    public IntroloopAudio sharaTitleThemeILA;
    public IntroloopAudio sharaCampaignBoss1ILA;
    public IntroloopAudio wandererThemeILA;
    public IntroloopAudio hardMysteryDungeonILA;
    public IntroloopAudio waterwayILA;
    public IntroloopAudio realmGodsILA;
    public IntroloopAudio mediumMysteryDungeonILA;

    public IntroloopAudio dragonDreadILA;
    public IntroloopAudio dragonBossILA;
    public IntroloopAudio dragonTitleILA;

    public IntroloopAudio lunarTownILA;

    static Dictionary<string, IntroloopAudio> dictIntroLoopAssets;

    public static MusicManagerScript singleton;

    private const float kVolumeMuteThreshold = -30.0f;
    private const float kSilence = -100.0f;

    private AudioSource[] musicChannels;
    public MusicTrackData[] musicTracksLoaded;
    private AudioSource[] sfxChannels;
    public AudioMixer mixer;
    int activeChannel;
    public float crossfadeTime;
    public const float TIME_TO_START_XFADETRACK_2 = 0.4f;
    public float fadeTime;
    float startFadeTime;

    bool isCrossfading;

    bool IsCrossfading
    {
        get
        {
            return isCrossfading;
        }
        set
        {
            //Debug.Log("Setting xfade value to " + value);
            isCrossfading = value;
        }
    }

    bool track1FullyFadedOut = false;
    float startXfadeTrack2Time;
    float timeAtTrack2FadeStart;
    bool track2FadingIn = false;
    bool track2FullyFadedIn = false;

    float preCrossfadeTrack1Volume;
    float preCrossfadeTrack2Volume;
    bool isFading;
    int[] playbackPosition;
    public AudioMixerGroup[] mixerGroups;

    private static Dictionary<string, MusicTrackData> allMusicTracks;
    public static MusicTrackData[] dungeonMusicTracks;
    private Dictionary<string, int> allMusicTrackPositions;
    public static Dictionary<string, int> allMusicTrackLengths;
    public MusicTrackData currentTrack;

    private int nextAvailableSFXChannel;
    private List<int> sfxChannelOrder;

    public const int NUM_SOUND_CHANNELS = 24;
    public const int NUM_MUSIC_TRACKS = 42;
    public const int NUM_DUNGEON_TRACKS = 6;
    private const int NUM_MUSIC_CHANNELS = 2;	

    private float bufferMusicVolume = 1.0f;

    static Dictionary<string, AudioSource> loopingSFXTracks;

    public static bool appHasFocus;

    private bool bLoadingNotComplete;
    private bool bWaitingToPlayTrack_NotLoadedYet;
    private MusicTrackData currentRequestedLoadTrack;
    private Coroutine currentLoadingCoroutine;
    private Coroutine currentPlayingCoroutine;
    private Coroutine playWhenLoadedCoroutine;

    public Dictionary<string, int> sfxPlaying;
    public Dictionary<string, float> timeLastPlayed;
    public HashSet<AudioDataPackage> sfxHashSet;

    private Stack<string> stackTracksToLoad;
    public static bool bPauseLoadAllMusicCoroutine;
    [Header ("Cutscene SFX")]
    public AudioStuff cutsceneSFX;

    static HashSet<string> musicTrackRefsEverPlayed;

    int globalMostRecentTrackNumberSelected;

    const int SHARA_THEME_INDEX = 23;

    List<MusicTrackData> localPossibleDungeonTracks;

    public class AudioDataPackage
    {
        public float length;
        public float timeStarted;
        public string cueName;
        public string clipName;
        public AudioSource sourcePlayer;

        public void KillImmediately()
        {
            length = 0.0001f;
            timeStarted = 0f;
            sourcePlayer.Stop();
            sourcePlayer.volume = 0.1f;
        }

        // Returns true if complete
        public bool UpdateAndCheckRemove()
        {
            float pComplete = (Time.time - timeStarted) / length;
            if (pComplete >= 1.0f)
            {
                MusicManagerScript.CueFinished(clipName);
                return true;
            }
            return false;
        }

    }

    // Use this for initialization
    private void Start()
    {
        allMusicTrackLengths = new Dictionary<string, int>();
        musicTrackRefsEverPlayed = new HashSet<string>();
    }
    public void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        if (sfxHashSet == null)
        {
            sfxHashSet = new HashSet<AudioDataPackage>();
        }
        
        if (stackTracksToLoad == null)
        {
            stackTracksToLoad = new Stack<string>();
        }
        if (loopingSFXTracks == null)
        {
            loopingSFXTracks = new Dictionary<string, AudioSource>();
        }
        
        timeLastPlayed = new Dictionary<string, float>();
        appHasFocus = true;

        if (dictIntroLoopAssets == null)
        {
            dictIntroLoopAssets = new Dictionary<string, IntroloopAudio>();
        }        

        IntroloopPlayer.InstanceID(0);
        IntroloopPlayer.InstanceID(1);

    }

    public static bool PlayCutsceneSound(string strCue)
    {
        if (!singleton.cutsceneSFX.cues.Any(c => c.cueName == strCue))
        {
            return false;
        }

        singleton.cutsceneSFX.PlayCue(strCue);
        return true;
    }

    public static void CueFinished(string clipName)
    {
        singleton.sfxPlaying[clipName]--;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Switch cannot be minimized really
		#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
            appHasFocus = hasFocus;
            if (PlayerOptions.audioOffWhenMinimized)
            {
                if (!hasFocus)
                {
                    //Debug.Log("App focus: " + hasFocus + " setting volume to -80f");
                    mixer.GetFloat("MusicVolume", out bufferMusicVolume);
                    mixer.SetFloat("MusicVolume", kSilence);
                }
                else
                {
                    //Debug.Log("App focus: " + hasFocus + " setting volume to " + bufferMusicVolume);
                    //mixer.SetFloat("MusicVolume", bufferMusicVolume);
                    SetMusicVolume(bufferMusicVolume);
                }
            }
		#endif


    }

    public void LoadTDMusicDataObject(ScriptableObject_MusicData scmd)
    {
        //if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Loading up the ScriptableObject");
        if (allMusicTracks.ContainsKey(scmd.strReference))
        {
            //if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Wait it already exists? Lol?");
            return;
        }

        MusicTrackData track = new MusicTrackData(scmd);

        allMusicTracks.Add(track.refName, track);

#if UNITY_EDITOR
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Creating TD Music Data Object " + scmd.strReference + " track name " + track.refName);
#endif
    }

    public void MusicManagerStart()
    {
        //if (Debug.isDebugBuild) Debug.Log("Started the music manager.");

        if (dungeonMusicTracks == null)
        {
            dungeonMusicTracks = new MusicTrackData[NUM_DUNGEON_TRACKS]; // Number of DUNGEON tracks
        }
        if (allMusicTracks == null)
        {
            allMusicTracks = new Dictionary<string, MusicTrackData>();
            if (PlatformVariables.USE_INTROLOOP)
            {
                AddAllTracksToDictionaries();
            }
        }

        if (allMusicTrackPositions == null)
        {
            allMusicTrackPositions = new Dictionary<string, int>();
            foreach (var kvp in allMusicTracks)
            {
                if (allMusicTrackPositions.ContainsKey(kvp.Key))
                {
#if UNITY_EDITOR
                    Debug.LogError("We already have " + kvp.Key + " track in the track positions dict, skipping.");
#endif
                    continue;
                }
                allMusicTrackPositions.Add(kvp.Key, 0);
            }
        }        

        string musicBundleName = PlatformVariables.USE_INTROLOOP ? "audio" : "music";

        sfxPlaying = new Dictionary<string, int>();

        musicChannels = new AudioSource[NUM_MUSIC_CHANNELS];
        musicTracksLoaded = new MusicTrackData[NUM_MUSIC_CHANNELS];
        for (int i = 0; i < musicTracksLoaded.Length; i++)
        {
            musicTracksLoaded[i] = new MusicTrackData("", "", 0, musicBundleName);
        }
        sfxChannels = new AudioSource[NUM_SOUND_CHANNELS]; // NUMBER OF SOUND CHANNELS
        mixerGroups = new AudioMixerGroup[6];
        mixerGroups[(int)MixerGroupNames.COMBATSFX] = GameObject.Find("MixerGroupCombatSFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        mixerGroups[(int)MixerGroupNames.MONSTERSFX] = GameObject.Find("MixerGroupMonsterSFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        mixerGroups[(int)MixerGroupNames.OBJECTSSFX] = GameObject.Find("MixerGroupObjectsSFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        mixerGroups[(int)MixerGroupNames.PLAYERSFX] = GameObject.Find("MixerGroupPlayerSFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        mixerGroups[(int)MixerGroupNames.UISFX] = GameObject.Find("MixerGroupUISFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        mixerGroups[(int)MixerGroupNames.FOOTSTEPSSFX] = GameObject.Find("MixerGroupFootstepSFX").GetComponent<AudioSource>().outputAudioMixerGroup;
        GameObject go = GameObject.Find("MusicTrack1");
        musicChannels[0] = go.GetComponent<AudioSource>();
        go = GameObject.Find("MusicTrack2");
        if (musicChannels.Length > 1)
        {
            musicChannels[1] = go.GetComponent<AudioSource>();
        }

        for (int i = 0; i < NUM_SOUND_CHANNELS; i++)
        {
            var aTrack = GameObject.Find("AudioTrack" + (i + 1));
            sfxChannels[i] = aTrack.GetComponent<AudioSource>();
        }

        //Create and define sources for playing music. Usually 2, because crossfade, but who knows?
        for (int t = 0; t < NUM_MUSIC_CHANNELS; t++)
        {
            GameObject newSpeaker = new GameObject("music_" + (t + 1));
            newSpeaker.transform.SetParent(transform);
            musicChannels[t] = newSpeaker.AddComponent<AudioSource>();
            musicChannels[t].outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
        }


        //Debug.Log(audioTracks[0].name + " " + audioTracks[1].name);

        playbackPosition = new int[2];
        nextAvailableSFXChannel = 0;
        sfxChannelOrder = new List<int>();

        SetMusicVolume(PlayerOptions.musicVolume);
        mixer.SetFloat("SFXVolume", PlayerOptions.SFXVolume);
        mixer.SetFloat("FootstepsVolume", PlayerOptions.footstepsVolume);

        StartCoroutine(LoadAllGameMusic_Coroutine());
    }

    IEnumerator CWaitThenPlay(string cue, float time)
    {
        yield return new WaitForSeconds(time);

        //Debug.Log("Finished waiting " + time + " to load & play music " + cue);
        if (PlatformVariables.USE_INTROLOOP)
        {
            LoadMusicByName_WithIntroloop(cue, true, true);
        }
        else
        {
            LoadMusicByName_NoIntroloop(cue, true, true);
        }
        
    }

    public void WaitThenPlay(string cue, float time, bool stompOtherTrack = false)
    {
        StartCoroutine(CWaitThenPlay(cue, time));
    }

    public void PlaySound(string cueName, AudioClip clip, float volume, MixerGroupNames mixerGroup, float pitchMod, bool voiceVolumeAdjustment, bool loopUntilKilled, int voiceLimit)
    {
        if (voiceLimit == 0)
        {
            voiceLimit = GLOBAL_SFX_VOICE_LIMIT;
        }

        // NOTE: cueName can be something like "awake", vs. a specific SFX. Clip.name gives us the true SFX being played.

        int numPlaying = 0;

        if (sfxPlaying.TryGetValue(clip.name, out numPlaying))
        {
            // Don't play new sound cue if previous one was triggered recently.
            if (Time.time - timeLastPlayed[clip.name] <= minThresholdDoNotPlay)
            {
                //Debug.Log(cueName + " last played at " + timeLastPlayed[clip.name] + " vs current " + Time.time + " min thresh: " + minThresholdDoNotPlay);
                return;
            }

            if (numPlaying >= GLOBAL_SFX_VOICE_LIMIT || numPlaying >= voiceLimit)
            {

                foreach (AudioDataPackage checkADP in sfxHashSet)
                {
                    if (checkADP.clipName == clip.name)
                    {
                        //Debug.Log("Hit voice limit " + numPlaying + " so killing an adp.");
                        checkADP.KillImmediately();
                        break;
                    }
                }
            }
        }

        sfxChannels[nextAvailableSFXChannel].clip = clip;



        sfxChannels[nextAvailableSFXChannel].outputAudioMixerGroup = mixerGroups[(int)mixerGroup];


        if (sfxPlaying.ContainsKey(clip.name))
        {
            sfxPlaying[clip.name]++;
            timeLastPlayed[clip.name] = Time.time;
        }
        else
        {
            sfxPlaying.Add(clip.name, 1);
            timeLastPlayed.Add(clip.name, Time.time);
        }



        AudioDataPackage adp = new AudioDataPackage();
        adp.length = clip.length;
        adp.cueName = cueName;
        adp.clipName = clip.name;
        adp.timeStarted = Time.time;
        adp.sourcePlayer = sfxChannels[nextAvailableSFXChannel];

        sfxHashSet.Add(adp);

        if (volume != 0f)
        {
            sfxChannels[nextAvailableSFXChannel].volume = volume;
        }
        else
        {
            sfxChannels[nextAvailableSFXChannel].volume = 1f;
        }
        sfxChannels[nextAvailableSFXChannel].pitch = 1f + pitchMod;

        int numCopies = sfxPlaying[clip.name];
        if (voiceVolumeAdjustment && numCopies > 1)
        {
            float volMod = 1 - (0.1f * numCopies); // HARDCODED: Modify based on max number of sounds.
            if (volMod <= 0.25f) volMod = 0.25f;
            //Debug.Log("Modify " + clip.name + " vol to " + volMod);
            foreach (AudioDataPackage existingADP in sfxHashSet)
            {
                if (existingADP.clipName == clip.name)
                {
                    existingADP.sourcePlayer.volume *= volMod;
                }
            }
        }

        if (loopingSFXTracks.ContainsKey(clip.name))
        {
            loopingSFXTracks[clip.name].Stop();
        }

        sfxChannels[nextAvailableSFXChannel].loop = loopUntilKilled;

        if (loopUntilKilled)
        {
            if (loopingSFXTracks.ContainsKey(clip.name))
            {
                loopingSFXTracks[clip.name] = sfxChannels[nextAvailableSFXChannel];
            }
            else
            {
                loopingSFXTracks.Add(clip.name, sfxChannels[nextAvailableSFXChannel]);
            }
        }

        sfxChannels[nextAvailableSFXChannel].Play();
        //Debug.Log("Playing on track " + nextAvailableAudioTrack);

        // Find next open track.
        sfxChannelOrder.Add(nextAvailableSFXChannel);

        nextAvailableSFXChannel = -1;

        for (int i = 0; i < sfxChannels.Length; i++)
        {
            if (!sfxChannels[i].isPlaying)
            {
                nextAvailableSFXChannel = i;
                sfxChannelOrder.Remove(i);
            }
        }

        if (nextAvailableSFXChannel == -1)
        {
            // Full on voices.
            if (sfxChannelOrder.Count > 0)
            {
                sfxChannels[sfxChannelOrder[0]].Stop();
                nextAvailableSFXChannel = sfxChannelOrder[0];
                sfxChannelOrder.Remove(sfxChannelOrder[0]);

            }
            else
            {
                Debug.Log("Audio queue error, everything is playing but list has desynced.");
            }
        }
    }

    public static void StopCue(string refName)
    {
        if (loopingSFXTracks.ContainsKey(refName))
        {
            loopingSFXTracks[refName].Stop();
            loopingSFXTracks.Remove(refName);
        }
    }

    // Update is called once per frame
    void Update()
    {
		TryCleanupSFXHashSet();

        if (PlatformVariables.USE_INTROLOOP) UpdateCrossfade_WithIntroLoop();
    }

    public void LoadMusic_WithIntroloop(MusicTrackData music, bool looping, bool playImmediately = false)
    {
#if UNITY_ANDROID
        return;
#endif
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Request to load music refname " + music.refName + " / filename " + music.trackFileName);

        //If we are in the middle of trying to load a different track, 
        //cancel that load so we can play this new track.
        if (currentRequestedLoadTrack != music &&
            bLoadingNotComplete &&
            currentLoadingCoroutine != null)
        {
            bLoadingNotComplete = false;
            StopCoroutine(currentLoadingCoroutine);

            if (currentPlayingCoroutine != null)
            {
                StopCoroutine(currentPlayingCoroutine);
            }
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Requested play of " + music.trackFileName + " while " + currentRequestedLoadTrack.trackFileName + " was still trying to load.");
        }

        currentRequestedLoadTrack = music;
        currentLoadingCoroutine = StartCoroutine(LoadClipAsync_WithIntroloop(music, looping, playImmediately));
    }

    //Begin loading a music clip, it may not happen right away.
    IEnumerator LoadClipAsync_WithIntroloop(MusicTrackData music, bool looping, bool playImmediately = false)
    {
        //if (Debug.isDebugBuild) Debug.Log("Async Music Load Request: " + music.trackFileName);
        bLoadingNotComplete = true;

        while (GameMasterScript.gmsSingleton == null)
        {
            yield return null;
        }

        if (music == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Dont... dont request null music");
            yield break;
        }

        while (GameMasterScript.loadedAssetBundles == null 
            || !GameMasterScript.loadedAssetBundles.ContainsKey(music.bundleName))
        {
            yield return null;
        }

        AssetBundle musicBun = GameMasterScript.loadedAssetBundles[music.bundleName];
        AssetBundleRequest rr = null;

        if (musicBun != null)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Search in assetbundle '" + musicBun.name + "' for filename " + music.trackFileName);
            rr = musicBun.LoadAssetAsync(music.trackFileName + ".ogg");
            yield return rr;
        }
        else
        {
            if (Debug.isDebugBuild) Debug.LogError("Asset Bundle '" + music.bundleName + "' appears to be null, can't load track '" + music.trackFileName + "' ");
            bLoadingNotComplete = false;
            yield break;
        }

        if (rr == null)
        {
            if (Debug.isDebugBuild) Debug.Log("<color=red>Tried to load music clip async, filenme " + music.trackFileName + " ref " + music.refName + " but we got null instead.</color>");
            yield break;
        }
        else
        {
            if (rr.asset == null)
            {
                if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("rr ASSET is null! Wat");
            }
            else if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("<color=green>Found asset " + rr.asset.name + "</color>");
        }

        /* if (rr.asset == null)  
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Asset bundle request null.");
        } */

        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Should be loaded? " + " " + music.refName + " " + rr.asset.name);

        ActuallyLoadClip_WithIntroloop(music, rr.asset as AudioClip, looping, playImmediately);
        bLoadingNotComplete = false;
    }

    public void SwitchTracks()
    {
        activeChannel = 1 - activeChannel;
    }

    //Plays the track that we have already loaded.
    public void Play_NoIntroloop(bool playFromLastPosition, bool crossfade, bool bOkToPlaySameTrack = false)
    {
        currentPlayingCoroutine = StartCoroutine(Play_Async_NoIntroloop(playFromLastPosition, crossfade, bOkToPlaySameTrack));
    }

    public void Play_WithIntroloop(bool playFromLastPosition, bool crossfade, bool okToPlaySameTrack = false)
    {

        if (!okToPlaySameTrack &&
            (musicChannels[0].isPlaying || musicChannels[1].isPlaying))
        {
            if (musicTracksLoaded[0].refName == musicTracksLoaded[1].refName)
            {
                if (Debug.isDebugBuild) Debug.Log("Don't play identical tracks please!");
                return;
            }
        }

        currentPlayingCoroutine = StartCoroutine(Play_Async_WithIntroloop(playFromLastPosition, crossfade, okToPlaySameTrack));
    }

    // These "specific track" routines ignore all crossfades and forcibly play a track.
    // Use very carefully as this eliminates transitions. However, it may be necessary in some situations.
    public void PlaySpecificTrack_WithIntroloop(int trackIndex, bool playFromLastPosition, bool crossfade)
    {
        currentPlayingCoroutine = StartCoroutine(Play_SpecificTrack_Async_WithIntroloop(trackIndex, playFromLastPosition, crossfade));
    }

    IEnumerator Play_SpecificTrack_Async_WithIntroloop(int trackIndex, bool playFromLastPosition, bool crossfade)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Play from track " + trackIndex + " " + playFromLastPosition + " " + crossfade);

        while (bLoadingNotComplete)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("loading not complete......");
            yield return null;
        }

        activeChannel = trackIndex;

        //musicTracks[1 - activeTrack].Stop();
        ActuallyStopClip_WithIntroloop(musicChannels[1 - activeChannel], 1 - activeChannel);

        //musicTracks[activeTrack].Play();
        ActuallyPlayAudioInClip_WithIntroloop(musicChannels[activeChannel], activeChannel);

        //musicTracks[activeTrack].volume = 1f;
        ActuallySetClipVolume(musicChannels[activeChannel], activeChannel, 1f);
    }    

    public string GetCurrentTrackName()
    {
        if (musicTracksLoaded == null) return "";
        if (activeChannel < 0 ||
            activeChannel >= musicTracksLoaded.Length)
        {
            return "";
        }

        return musicTracksLoaded[activeChannel] == null ? "" : musicTracksLoaded[activeChannel].refName;
    }



    public static void WriteMusicDataToSave(XmlWriter writer)
    {
        // Serialize the current music track so we don't always have to evaluate it.

        string mapMusic = "";

        foreach (Map m in MapMasterScript.dictAllMaps.Values)
        {
            if (!string.IsNullOrEmpty(m.musicCurrentlyPlaying))
            {
                mapMusic += m.mapAreaID + "," + m.musicCurrentlyPlaying + "|";
            }
        }

        if (mapMusic == "")
        {
            mapMusic = "none";
        }

        writer.WriteElementString("mmusic", mapMusic);
    }

    public static void ReadMusicDataFromSave(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        string content = reader.ReadElementContentAsString();
        if (content == "none")
        {
            return;
        }
        string[] parsedMapDataPairs = content.Split('|');
        for (int i = 0; i < parsedMapDataPairs.Length; i++)
        {
            string[] dataPair = parsedMapDataPairs[i].Split(',');
            if (dataPair.Length < 2) continue;
            int mapID;
            if (Int32.TryParse(dataPair[0], out mapID))
            {
                string songRef = dataPair[1];
                Map mapToAssignMusic;
                if (MapMasterScript.dictAllMaps.TryGetValue(mapID, out mapToAssignMusic))
                {
                    mapToAssignMusic.musicCurrentlyPlaying = songRef;
                }
            }

        }
    }

    public void ActuallyPlayAudioInClip_WithIntroloop(AudioSource aSource, int index, bool playFromLastPosition = false)
    {
        //Debug.Log("Prepare to play on track " + index);

        string cueName = "";

        if (aSource.clip == null)
        {
            cueName = musicTracksLoaded[index].trackFileName;
            //Debug.Log("No clip, so use " + cueName + " instead.");
        }
        else
        {
            cueName = aSource.clip.name;
            //Debug.Log("Or use " + cueName);
        }

        IntroloopAudio ila;

        if (dictIntroLoopAssets.TryGetValue(cueName, out ila))
        {
            if (ila == null)
            {
                //Debug.Log("Wait! We can't play null " + cueName);
            }

            IntroloopPlayer.InstanceID(index).SetVolumeAllSources(0f); // new to avoid pops

            if (playFromLastPosition)
            {
                int lastPosition = allMusicTrackPositions[currentTrack.refName];
                //SetTimeSamples(index, allMusicTrackPositions[currentTrack.index]);
                //Debug.Log("Playing from " + allMusicTrackPositions[currentTrack.index] + " on " + IntroloopPlayer.InstanceID(index).gameObject.name);

                //Debug.Log("Play from last position. Playing from " + allMusicTrackPositions[currentTrack.index] + " on " + IntroloopPlayer.InstanceID(index).gameObject.name + " " + IntroloopPlayer.InstanceID(index).GetCurrentVolume());
                IntroloopPlayer.InstanceID(index).Play(ila, lastPosition);
            }
            else
            {
                //Debug.Log("No play from last position. Playing from " + allMusicTrackPositions[currentTrack.index] + " on " + IntroloopPlayer.InstanceID(index).gameObject.name + " " + IntroloopPlayer.InstanceID(index).GetCurrentVolume());
                IntroloopPlayer.InstanceID(index).Play(ila, 0);
            }

        }
        else
        {
            //Debug.Log("We will play " + cueName + " on old system (BAD)");
            //Debug.Log(aSource.clip.name);
            aSource.Play();
        }
    }

    public void ActuallyStopClip_WithIntroloop(AudioSource aSource, int index)
    {
        string cueName = "";
        if (aSource.clip == null)
        {
            cueName = musicTracksLoaded[index].trackFileName;
            //Debug.Log("No clip, so use " + cueName + " instead.");
        }
        else
        {
            cueName = aSource.clip.name;
        }
        IntroloopAudio ila;

        if (dictIntroLoopAssets.TryGetValue(cueName, out ila))
        {
            IntroloopPlayer.InstanceID(index).StopFade();
        }
        else
        {
            aSource.Stop();
        }
    }

    public AudioClip GetMusicClip_FromIntroloop(int index)
    {
        return IntroloopPlayer.InstanceID(index).GetClip();
    }

    public float GetClipLength_FromIntroloop(int track)
    {
        return IntroloopPlayer.InstanceID(track).GetClipLength();
    }

    public int GetTimeSamples(int track)
    {
        return IntroloopPlayer.InstanceID(track).GetTimeSamplesOfCurrent();
    }

    public float GetMusicVolume(int index)
    {
        return IntroloopPlayer.InstanceID(index).GetCurrentVolume();
    }

    public void SetTimeSamples(int track, int amount)
    {
        IntroloopPlayer.InstanceID(track).SetTimeSamples(amount);
    }

    public static void RequestPlayNonLoopingMusicImmediatelyFromScratchWithCrossfade(string theme, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, false, false, true, okToPlaySameTrack, true);
    }

    public static void RequestPlayNonLoopingMusicFromScratchWithCrossfade(string theme, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, false, false, false, okToPlaySameTrack, true);
    }

    public static void RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade(string theme, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, true, false, true, okToPlaySameTrack, true);
    }

    public static void RequestPlayLoopingMusicImmediatelyFromScratchNoCrossfade(string theme, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, true, false, true, okToPlaySameTrack, false);
    }

    public static void RequestPlayLoopingMusicFromScratchWithCrossfade(string theme, bool playImmediately = false, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, true, false, playImmediately, okToPlaySameTrack, true);
    }

    public static void RequestPlayMusicFromScratchWithCrossfade(string theme, bool looping, bool playImmediately = false, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, looping, false, playImmediately, okToPlaySameTrack, true);
    }

    public static void RequestPlayMusicWithCrossfade(string theme, bool looping, bool playFromLastPosition = false, bool playImmediately = false, bool okToPlaySameTrack = false)
    {
        RequestPlayMusic(theme, looping, playFromLastPosition, playImmediately, okToPlaySameTrack, true);
    }

    static bool musicTrackRefsHashsetCreated;

    public static void RequestPlayMusic(string theme, bool looping, bool playFromLastPosition = false, bool playImmediately = false, bool okToPlaySameTrack = false, bool crossfade = true)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Request play music " + theme + ", with crossfade? " + crossfade + ", Play immediately? " + playImmediately);

        if (!musicTrackRefsHashsetCreated) 
        {
            musicTrackRefsEverPlayed = new HashSet<string>();
            musicTrackRefsHashsetCreated = true;
        }

        if (!musicTrackRefsEverPlayed.Contains(theme))
        {
            musicTrackRefsEverPlayed.Add(theme);
            playFromLastPosition = false;
        }

        if (PlatformVariables.USE_INTROLOOP)
        {
            singleton.LoadMusicByName_WithIntroloop(theme, looping, playImmediately);
            singleton.Play_WithIntroloop(playFromLastPosition, crossfade, okToPlaySameTrack);
        }
        else
        {
            LoadAndPlayTrack_NoIntroLoop(theme, looping, playFromLastPosition, playImmediately);
        }
    }

    void AddAllTracksToDictionaries()
    {
        //Debug.Log("Adding all tracks to dictionaries.");

        MusicTrackData track = null;

        track = new MusicTrackData("IRL DungeonMusic1 LP", "dungeontheme1", 208901);
        track.index = 0;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[0] = track;

        dictIntroLoopAssets.Add("IRL DungeonMusic1 LP", dungeonTheme1ILA);

        track = new MusicTrackData("IRL DungeonMusic3 LP", "dungeontheme3", 631865);
        track.index = 1;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[1] = track;

        dictIntroLoopAssets.Add("IRL DungeonMusic3 LP", dungeonTheme3ILA);

        track = new MusicTrackData("IRL DungeonMusic2 LP", "dungeontheme2", 631865);
        track.index = 2;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[2] = track;

        dictIntroLoopAssets.Add("IRL DungeonMusic2 LP", dungeonTheme2ILA);

        track = new MusicTrackData("IRL TownMusic1 LP", "towntheme1", 631865);
        track.index = 3;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("IRL TownMusic1 LP", townThemeILA);

        track = new MusicTrackData("TD BossTheme1 LP", "bosstheme1", 631865);
        track.index = 4;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD BossTheme1 LP", bossTheme1ILA);

        track = new MusicTrackData("TD EasyDungeon LP", "easydungeon", 631865);
        track.index = 5;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD EasyDungeon LP", easydungeonILA);

        track = new MusicTrackData("IRL DungeonTheme4 LP", "dungeontheme4", 631865);
        track.index = 6;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[3] = track;

        dictIntroLoopAssets.Add("IRL DungeonTheme4 LP", dungeonTheme4ILA);

        track = new MusicTrackData("TD TitleScreen LP", "titlescreen", 631865, "title");
        track.index = 7;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD TitleScreen LP", titleThemeILA);

        track = new MusicTrackData("TD Gameover LP", "gameover", 631865);
        track.index = 8;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Gameover LP", gameoverILA);

        track = new MusicTrackData("TD GroveTheme LP", "grovetheme", 631865);
        track.index = 9;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD GroveTheme LP", grovethemeILA);

        track = new MusicTrackData("TD FunMerchant LP", "funmerchant", 631865);
        track.index = 10;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD FunMerchant LP", funmerchantILA);

        track = new MusicTrackData("TD DungeonTheme5 LP", "dungeontheme5", 631865);
        track.index = 11;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[4] = track;

        dictIntroLoopAssets.Add("TD DungeonTheme5 LP", dungeonTheme5ILA);

        track = new MusicTrackData("TD RestMusic NL", "resttheme", 631865);
        track.index = 12;
        track.looping = false;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD RestMusic NL", restthemeILA);

        track = new MusicTrackData("TD BossVictory NL", "BossVictory", 631865);
        track.index = 13;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD BossVictory NL", bossvictoryILA);

        track = new MusicTrackData("TD Mysterious LP", "postboss", 0);
        track.index = 14;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Mysterious LP", postbossILA);

        track = new MusicTrackData("TD Passageway LP", "passageway", 0);
        track.index = 15;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Passageway LP", passagewayILA);

        track = new MusicTrackData("TD Casino LP", "casino", 0);
        track.index = 16;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Casino LP", casinoILA);

        track = new MusicTrackData("TD DungeonTheme7 LP", "dungeontheme7", 0);
        track.index = 17;
        allMusicTracks.Add(track.refName, track);
        dungeonMusicTracks[5] = track;

        dictIntroLoopAssets.Add("TD DungeonTheme7 LP", dungeonTheme7ILA);

        track = new MusicTrackData("TD Villainous LP", "villainous_intro", 0);
        track.index = 18;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Villainous LP", villainousILA);

        track = new MusicTrackData("TD_Boss2_LP", "bosstheme2", 631865);
        track.index = 19;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_Boss2_LP", bosstheme2ILA);

        track = new MusicTrackData("TD Desert LP", "deserttheme", 631865);
        track.index = 20;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Desert LP", desertthemeILA);

        track = new MusicTrackData("TD_Ending_NL", "ending_theme", 631865);
        track.index = 21;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_Ending_NL", endingthemeILA);

        track = new MusicTrackData("TD_FinalBoss_LP", "finalboss_phase2", 631865);
        track.index = 22;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_FinalBoss_LP", finalboss2ILA);

        track = new MusicTrackData("TD_SharaTheme_LP", "sharatheme1", 631865);
        track.index = SHARA_THEME_INDEX;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_SharaTheme_LP", sharatheme1ILA);

        track = new MusicTrackData("TD_SharaSerious_LP", "sharaserious", 631865);
        track.index = 24;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_SharaSerious_LP", sharaSeriousILA);

        track = new MusicTrackData("TD_Sadness_LP", "sadness", 631865);
        track.index = 25;
        allMusicTracks.Add(track.refName, track);

        if (sadnessILA == null) Debug.Log("Why is sadness null");

        dictIntroLoopAssets.Add("TD_Sadness_LP", sadnessILA);

        track = new MusicTrackData("TD_DimRiffBoss_LP", "dimriffboss", 631865);
        track.index = 26;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_DimRiffBoss_LP", dimRiffBossILA);

        track = new MusicTrackData("TD_DimRift_LP", "dimriftlevel", 631865);
        track.index = 27;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_DimRift_LP", dimRiftLevelILA);

        track = new MusicTrackData("TD_JobTrial_LP", "jobtrial", 631865);
        track.index = 28;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_JobTrial_LP", jobTrialILA);

        track = new MusicTrackData("TD Training Theme", "charcreation", 631865);
        track.index = 29;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD Training Theme", charCreationILA);

        track = new MusicTrackData("TD_FinalLevels_LP", "finallevels", 631865);
        track.index = 30;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_FinalLevels_LP", finalLevelsILA);

        track = new MusicTrackData("TD_GroveThemeRedux_LP", "grovethemeredux", 631865);
        track.index = 31;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_GroveThemeRedux_LP", groveReduxILA);

        track = new MusicTrackData("TD_TitleThemeShara_LP", "shara_titlescreen", 631865, "title");
        track.index = 32;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_TitleThemeShara_LP", sharaTitleThemeILA);

        track = new MusicTrackData("TD_SharaHeroBoss_LP", "sharamode_boss1", 631865);
        track.index = 33;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_SharaHeroBoss_LP", sharaCampaignBoss1ILA);

        track = new MusicTrackData("TD_WandererTheme_LP", "wanderer", 631865);
        track.index = 34;
        allMusicTracks.Add(track.refName, track);

        dictIntroLoopAssets.Add("TD_WandererTheme_LP", wandererThemeILA);

        track = new MusicTrackData("TD_MysteryDungeonHard_LP", "hardmysterydungeon", 631865);
        track.index = 35;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_MysteryDungeonHard_LP", hardMysteryDungeonILA);

        track = new MusicTrackData("TD_Waterway_LP", "waterway", 631865);
        track.index = 36;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_Waterway_LP", waterwayILA);

        track = new MusicTrackData("TD_RealmOfGods_LP", "realmgods", 631865);
        track.index = 37;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_RealmOfGods_LP", realmGodsILA);

        track = new MusicTrackData("TD_MysteryDungeonMid_LP", "mediummysterydungeon", 631865);
        track.index = 38;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_MysteryDungeonMid_LP", mediumMysteryDungeonILA);

        track = new MusicTrackData("TD_DragonDread_LP", "dragondread", 631865);
        track.index = 39;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_DragonDread_LP", dragonDreadILA);

        track = new MusicTrackData("DragonBoss_LP", "dragonboss", 631865);
        track.index = 40;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("DragonBoss_LP", dragonBossILA);

        track = new MusicTrackData("TD_DragonTitle_LP", "dragontitle", 631865, "title");
        track.index = 41;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("TD_DragonTitle_LP", dragonTitleILA);

        track = new MusicTrackData("Riverstone_LNY", "lunartown", 631865);
        track.index = 42;
        allMusicTracks.Add(track.refName, track);
        dictIntroLoopAssets.Add("Riverstone_LNY", lunarTownILA);        

        if (lunarTownILA == null) Debug.Log("Why is lunar town null");
    }


    public void PushSpecificMusicTrackOnStack(string trackName)
    {
        if (stackTracksToLoad == null)
        {
            stackTracksToLoad = new Stack<string>();
        }
        //if (Debug.isDebugBuild) Debug.Log("PushSpecificMusic: " + trackName + " is pushed on to the stack.");

        stackTracksToLoad.Push(trackName);
    }

    public void FillStackWithTracksToLoad()
    {
        

        if (stackTracksToLoad == null)
        {
            stackTracksToLoad = new Stack<string>();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Already filled stack with tracks.");
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("Filling stack with tracks.");

        if (SharaModeStuff.IsSharaModeActive())
        {
            PushSpecificMusicTrackOnStack("sadness");
        }

        PushSpecificMusicTrackOnStack("dungeontheme3");
        PushSpecificMusicTrackOnStack("dungeontheme1");
        PushSpecificMusicTrackOnStack("easydungeon");

        //grab every name in the list

#if UNITY_EDITOR
        if (PlatformVariables.USE_INTROLOOP)
        {
            var names = AssetDatabase.GetAllAssetBundleNames();
            foreach (string strName in names)
            {
                if (!strName.StartsWith("music_"))
                {
                    continue;
                }

                string strTrackName = strName.Replace("music_", "");
                //Debug.Log("Pushing " + strTrackName + " on to loading stack.");
                stackTracksToLoad.Push(strTrackName);
            }
        }
        else
        {

        }
#endif

#if UNITY_SWITCH
        //Look for the bundles
        string strBuildMeAPath = Path.Combine(Application.streamingAssetsPath, "music");

        //if (Debug.isDebugBuild) Debug.Log("Opening music directory");

        //Open the directory and grab the handle.
        nn.fs.DirectoryHandle dirHandle = new DirectoryHandle();
        nn.fs.Directory.Open(ref dirHandle, strBuildMeAPath, OpenDirectoryMode.File);

        //if (Debug.isDebugBuild) Debug.Log("Counting music entries");

        //how many files are in there?
        long lNumEntries = 0;
        nn.fs.Directory.GetEntryCount(ref lNumEntries, dirHandle);
        DirectoryEntry[] entries = new DirectoryEntry[lNumEntries];

        //if (Debug.isDebugBuild) Debug.Log("Reading them");

        //look at each of them.
        long outValue = 0;
        nn.fs.Directory.Read(ref outValue, entries, dirHandle, lNumEntries);

        foreach (var e in entries)
        {
            string strName = e.name;
            if( strName.Contains(".manifest") || 
                !strName.StartsWith("music_"))
            {
                continue;
            }

            string strTrackName = strName.Replace("music_", "");
            stackTracksToLoad.Push(strTrackName);
        }

        //if (Debug.isDebugBuild) Debug.Log("Closing directory");

        nn.fs.Directory.Close(dirHandle);
#endif

        //now, to make sure we load title first, place that on the tippy top
        stackTracksToLoad.Push("trainingtheme");
        stackTracksToLoad.Push("titlescreen");
        stackTracksToLoad.Push("towntheme1");
    }

    /// <summary>
    /// Checks to see if a given track is loaded and playing.
    /// </summary>
    /// <param name="strTrackName"></param>
    /// <returns></returns>
    public static bool IsTrackPlaying(string strTrackName)
    {
        foreach (var m in singleton.musicTracksLoaded)
        {
            if (m.refName == strTrackName)
            {
                return true;
            }
        }

        return false;
    }

    public void SetAllVolumeToZero()
    {
		if (PlatformVariables.USE_INTROLOOP) 
		{
	        IntroloopPlayer.InstanceID(0).SetVolumeAllSources(0);
	        IntroloopPlayer.InstanceID(1).SetVolumeAllSources(0);

	        IntroloopPlayer.InstanceID(0).SetInternalFadeVolume(0);
	        IntroloopPlayer.InstanceID(1).SetInternalFadeVolume(0);
		}
		else 
		{
	        foreach (AudioSource a in musicChannels)
	        {
	            a.volume = 0;
	        }		
		}

    }

    public static void StopAllMusic()
    {
        singleton.StopAllMusic_Internal();
    }

    private void StopAllMusic_Internal()
    {
        //if (Debug.isDebugBuild) Debug.Log("music: All music stopped.");

        for (int t = 0; t < musicTracksLoaded.Length; t++)
        {
            var mtd = musicTracksLoaded[t];
            var a = musicChannels[t];

            //record the stopping times of the tracks.
            allMusicTrackPositions[mtd.refName] = a.timeSamples;

            //then stopalop them. If the track is picked up later, it should
            //carry on where we left off.
            a.Stop();
        }

        //if we are waiting to play a song because of loading issues, cancel that too
        if (playWhenLoadedCoroutine != null)
        {
            StopCoroutine(playWhenLoadedCoroutine);
        }

        IsCrossfading = false;
    }

    private void TryCleanupSFXHashSet()
    {
        //only look for removal when there is something to remove
        if (sfxHashSet.Count == 0) return;

        //All this hoop jumping is to avoid calling RemoveWhere every frame
        //because it makes all of 40 bytes of garbage :D but we don't to do that
        //while idling.
        bool bShouldRemove = false;
        foreach (var a in sfxHashSet)
        {
            if (a.UpdateAndCheckRemove())
            {
                bShouldRemove = true;
                break;
            }
        }

        if (bShouldRemove)
        {
            sfxHashSet.RemoveWhere(a => a.UpdateAndCheckRemove());
        }
    }

    /// <summary>
    /// Called when we know we want to play a track, but it hasn't been loaded yet.
    /// </summary>
    /// <param name="nameOfTrack"></param>
    /// <param name="looping"></param>
    /// <param name="playFromLastPosition"></param>
    /// <param name="playImmediately"></param>
    /// <param name="bOKToPlaySameTrack"></param>
    /// <returns></returns>
    public IEnumerator PlayAsSoonAsLoaded_NoIntroloop(string nameOfTrack, bool looping, bool playFromLastPosition, bool playImmediately = false, bool bOKToPlaySameTrack = false)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Gonna play " + nameOfTrack + " as soon as loaded");

        MusicTrackData mtd;
        allMusicTracks.TryGetValue(nameOfTrack, out mtd);

        string lowerVar = nameOfTrack.ToLowerInvariant();

        // Wait patiently
        while (mtd == null)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("But " + nameOfTrack + " is not loaded. Try " + lowerVar);
            allMusicTracks.TryGetValue(lowerVar, out mtd);
            yield return null;
        }

        //we have the MTD, which contains the clip, so let's play
        bWaitingToPlayTrack_NotLoadedYet = false;

        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("Ok, so we can play now?");

        //it would be best if playImmediately was false, because that allows for crossfade.
        ActuallyLoadClip_NoIntroloop(mtd, looping, playImmediately, playFromLastPosition);

        //if playImmediately was called, we've already played the music with no crossfade.
        if (!playImmediately)
        {
            Play_NoIntroloop(playFromLastPosition, true, bOKToPlaySameTrack);
        }

        //Clear this out because we're done.
        playWhenLoadedCoroutine = null;
    }

    /// <summary>
    /// Load a track and play it. If the track is already loaded, the play happens right away.
    /// If the track is not loaded, a coroutine fires that will play the track as soon as
    /// it is in memory.
    /// </summary>
    /// <param name="nameOfTrack"></param>
    /// <param name="looping">I don't know what this does, I thought the tracks kept that data internally</param>
    /// <param name="playImmediately">Play without a crossfade</param>
    public static void LoadAndPlayTrack_NoIntroLoop(string nameOfTrack, bool looping, bool playFromLastPosition, bool playImmediately = false, bool bOKToPlaySameTrack = false)
    {
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("NO INTRO LOOP Request play " + nameOfTrack + " looping? " + looping + " lastpos? " + playFromLastPosition + " immediate? " + playImmediately + " same? " + bOKToPlaySameTrack);

        MusicTrackData mtd;
        allMusicTracks.TryGetValue(nameOfTrack, out mtd);

        // mtd might be null because we haven't yet loaded the assetbundle that has the information that
        // we use to create the MusicTrackData. If so, use the code path that creates a delay
        // until the load is complete.
        if (mtd == null)
        {
            if (Debug.isDebugBuild && LogoSceneScript.debugMusic) Debug.Log("MTD is null, so let's try to load it.");

            //load it up
            singleton.LoadMusicByName_NoIntroloop(nameOfTrack, looping);

            //if we're waiting to play something else, kill that
            if (singleton.playWhenLoadedCoroutine != null)
            {
                singleton.StopCoroutine(singleton.playWhenLoadedCoroutine);
            }

            //then play it when we're loaded
            singleton.playWhenLoadedCoroutine = singleton.StartCoroutine(singleton.PlayAsSoonAsLoaded_NoIntroloop(nameOfTrack, looping, playFromLastPosition,
                playImmediately, bOKToPlaySameTrack));

            return;
        }

         
        if (Debug.isDebugBuild && LogoSceneScript.debugMusic)
        {
            Debug.Log("music: mtd is not null for track '" + nameOfTrack + "' so let's play it! looping==" + looping +
                  " playFromLastPosition==" + playFromLastPosition +
                  " playImmediately==" + playImmediately); 
        } 

        //we have the MTD, which contains the clip, so let's play
        singleton.bWaitingToPlayTrack_NotLoadedYet = false;

        //it would be best if playImmediately was false, because that allows for crossfade.
        singleton.ActuallyLoadClip_NoIntroloop(mtd, looping, playImmediately, playFromLastPosition);

        //if playImmediately was called, we've already played the music with no crossfade.
        if (!playImmediately)
        {
            singleton.Play_NoIntroloop(playFromLastPosition, true, bOKToPlaySameTrack);
        }
    }

    public void LoadMusicByName_WithIntroloop(string nameOfTrack, bool looping, bool playImmediately = false)
    {
        MusicTrackData mtd;
        if (allMusicTracks.TryGetValue(nameOfTrack, out mtd))
        {
            LoadMusic_WithIntroloop(mtd, mtd.looping, playImmediately); // Ignore bool looping?
            return;
        }

        Debug.LogError("Track not found: " + nameOfTrack);
    }

    public string LoadDungeonMusicAtRandom(Map mapForMusic, bool itemDream)
    {
        if (mapForMusic.floor == 0 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            // Special music just for Shara in cedar 0
            MusicTrackData sharaMusic = allMusicTracks["sharatheme1"];
            LoadMusic_WithIntroloop(sharaMusic, true);
            return sharaMusic.refName;
        }

        List<MusicTrackData> possibleTracks = new List<MusicTrackData>();

        if (mapForMusic.IsMainPath())
        {
            possibleTracks.Add(dungeonMusicTracks[0]);
            possibleTracks.Add(dungeonMusicTracks[1]);
            possibleTracks.Add(dungeonMusicTracks[4]);

            if (mapForMusic.effectiveFloor >= 5)
            {
                possibleTracks.Add(dungeonMusicTracks[2]);
                possibleTracks.Add(dungeonMusicTracks[3]);
            }
            if (mapForMusic.effectiveFloor >= 11)
            {
                possibleTracks.Add(dungeonMusicTracks[5]);
            }

        }
        else
        {
            for (int i = 0; i < dungeonMusicTracks.Length; i++)
            {
                possibleTracks.Add(dungeonMusicTracks[i]);
            }
        }

        if (itemDream)
        {
            possibleTracks.Add(allMusicTracks["passageway"]); // passageway
            possibleTracks.Add(allMusicTracks["deserttheme"]); // desert
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                possibleTracks.Add(allMusicTracks["waterway"]);
            }
        }

        MusicTrackData mtd = possibleTracks[UnityEngine.Random.Range(0, possibleTracks.Count)];
        if (currentTrack != null)
        {
            int tries = 0;
            while ((mtd == currentTrack) && (tries < 50))
            {
                mtd = possibleTracks[UnityEngine.Random.Range(0, possibleTracks.Count)];
                tries++;
            }
            if (tries >= 50)
            {
                Debug.Log("Broke LoadMusicAtRandom WHILE loop");
            }
        }
        //Debug.Log("Picked track " + mtd.refName);
        LoadMusic_WithIntroloop(mtd, true);
        return mtd.refName;
    }

    public string SelectDungeonMusicAtRandom(Map mapForMusic, bool itemDream)
    {
        if (localPossibleDungeonTracks == null) localPossibleDungeonTracks = new List<MusicTrackData>();
        localPossibleDungeonTracks.Clear();

        //For main path music, only load dungeon tracks that are eligible based on 
        //our depth into the dungeon
        if (mapForMusic.IsMainPath())
        {
            int iEffectiveFloor = mapForMusic.effectiveFloor;
            if (iEffectiveFloor < 0)
            {                
                iEffectiveFloor = 0;
            }

            localPossibleDungeonTracks.AddRange(allMusicTracks.Where(
                kvp => kvp.Value.isDungeonTrack && 
                kvp.Value.iMinimumFloor <= iEffectiveFloor).Select(
                    keyValuePair => keyValuePair.Value));

        }
        //not main path, just load all dungeonable tracks
        else
        {
            localPossibleDungeonTracks.AddRange(allMusicTracks.Where(
                kvp => kvp.Value.isDungeonTrack).Select(
                    keyValuePair => keyValuePair.Value));

        }

        if (itemDream)
        {
            localPossibleDungeonTracks.AddRange(allMusicTracks.Where(
                kvp => kvp.Value.isItemDreamTrack).Select(
                keyValuePair => keyValuePair.Value));
        }

        if (localPossibleDungeonTracks.Count == 0)
        {
            //LoadMusicByName("dungeontheme1", true, false);
            return "dungeontheme1";
        }


        MusicTrackData mtd = localPossibleDungeonTracks[UnityEngine.Random.Range(0, localPossibleDungeonTracks.Count)];
        if (currentTrack != null)
        {
            int tries = 0;
            while (mtd == currentTrack && tries < 50)
            {
                mtd = localPossibleDungeonTracks[UnityEngine.Random.Range(0, localPossibleDungeonTracks.Count)];
                tries++;
            }
            if (tries >= 50)
            {
                //Debug.Log("Broke LoadMusicAtRandom WHILE loop");
            }
        }

        return mtd.refName;
    }

    public void LoadMusicByName_NoIntroloop(string nameOfTrack, bool looping, bool nocrossfade = false)
    {
        //if (Debug.isDebugBuild) Debug.Log("Request load music by name, without introloop: " + nameOfTrack);

        MusicTrackData mtd;
        allMusicTracks.TryGetValue(nameOfTrack, out mtd);

        // mtd might be null because we haven't yet loaded the assetbundle that has the information that
        // we use to create the MusicTrackData
        if (mtd == null)
        {
            //push it to the top of the stack of tracks to load, we need it quickly
            PushSpecificMusicTrackOnStack(nameOfTrack);

            //and don't let us play yet
            bWaitingToPlayTrack_NotLoadedYet = true;

            //bounce out, but don't fret, we will play as soon as we're loaded
            StartCoroutine(WaitUntilTrackLoadedThenTryAgain_NoIntroloop(nameOfTrack, looping, nocrossfade));
            return;
        }

        //we have the MTD, which contains the clip, so let's play
        ActuallyLoadClip_NoIntroloop(mtd, looping, nocrossfade);
    }

    void ActuallyLoadClip_NoIntroloop(MusicTrackData mtdData, bool looping, bool playImmediately = false, bool playFromLastPosition = true)
    {
#if UNITY_EDITOR
        if (LogoSceneScript.debugMusic) Debug.Log("Actually load a clip without introloop. " + mtdData.refName + ", last position? " + playFromLastPosition + " Play immediately? " + playImmediately);
#endif

        PrintMusicDebugOutput(mtdData);

        if (mtdData.clip == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Clip was null for music refname " + mtdData.refName + " / filename: " + mtdData.trackFileName);
#endif
            return;
        }

        //if we are trying to play the music we're already playing, don't.
        if (musicTracksLoaded[activeChannel] == mtdData &&
            musicChannels[activeChannel].isPlaying)
        {
            if (Debug.isDebugBuild) Debug.Log("We're already playing this track.");
            return;
        }

        bool bAlreadyLoaded = false;
        for (int t = 0; t < NUM_MUSIC_CHANNELS; t++)
        {
            if (musicTracksLoaded[t] == mtdData)
            {
                bAlreadyLoaded = true;
                //it's loaded, and currently playing, that's no good. If we play it again
                //the music will skip
                if (activeChannel == t && musicChannels[t].isPlaying)
                {
                    if (Debug.isDebugBuild) Debug.Log("It's already loaded and playing...");
                    return;
                }
                else
                {
                    //otherwise, make sure this is the active channel
                    activeChannel = t;
                }
            }
        }
        //play this in the first open channel we have
        // if 0 is free, use that.
        // if 1 is free, use THAT
        // otherwise, use the inactive channel and do some fadery
        if (!bAlreadyLoaded)
        {
            if (musicChannels[0].clip == null)
            {
                activeChannel = 0;
            }
            else if (musicChannels[1].clip == null)
            {
                activeChannel = 1;
            }
            else
            {
                activeChannel = 1 - activeChannel;
            }
        }

        //Keep track of the data we're now going to use
        currentTrack = mtdData;

        //if this clip is one we played before, pick up where we left off
        int iPreviousSamples;
        allMusicTrackPositions.TryGetValue(mtdData.refName, out iPreviousSamples);

        int sampleLength = 0;

        if (!allMusicTrackLengths.TryGetValue(mtdData.refName, out sampleLength))
        {
            sampleLength = mtdData.clip.samples;
        }

        if (playFromLastPosition)
        {
            if (sampleLength <= iPreviousSamples)
            {
                musicChannels[activeChannel].timeSamples = 0;
            }
            else
            {
                try
                {
                    musicChannels[activeChannel].timeSamples = iPreviousSamples;
                }
                catch (Exception e)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Invalid time samples for track " + mtdData.refName + " " + iPreviousSamples + " vs " + musicChannels[activeChannel].timeSamples);
                    musicChannels[activeChannel].timeSamples = 0;
                }

            }
        }

        //here it is!
        musicChannels[activeChannel].clip = mtdData.clip;

        //if (Debug.isDebugBuild) Debug.Log("Clip " + mtdData.refName + " should be loaded. Is clip null? " + (mtdData.clip == null));

        //make sure we keep track of the data for the song
        musicTracksLoaded[activeChannel] = mtdData;

        //Set looping here, even though it might already exist in the .wav file itself?
        musicChannels[activeChannel].loop = looping;

        //Force it to play if we must.
        if (playImmediately)
        {
            ForcePlayChannel_NoIntroloop(activeChannel);
        }
    }

    void PrintMusicDebugOutput(MusicTrackData mtdData)
    {
        return;
#if !UNITY_EDITOR
        return;
#endif
        string s1 = musicTracksLoaded[0] != null ? musicTracksLoaded[0].refName : "*null*";
        string s2 = musicTracksLoaded[1] != null ? musicTracksLoaded[1].refName : "*null*";
        
        Debug.Log("music: ActuallyLoadClip for clip " + mtdData.refName + ", tracks currently loaded are " +
                  "0: " + s1 + ", 1:" + s2);

     
    }

    void UpdateCrossfade_WithIntroLoop()
    {
        if (IsCrossfading)
        {
            if (track2FullyFadedIn && track1FullyFadedOut)
            {
                IsCrossfading = false;
                //StartCoroutine(WaitThenChangeExternalFadeState(1f, 0, false));
                //StartCoroutine(WaitThenChangeExternalFadeState(1f, 1, false));
                IntroloopPlayer.InstanceID(1 - activeChannel).SetExternalFadeState(false, 0f);
                IntroloopPlayer.InstanceID(activeChannel).SetExternalFadeState(false, 1f);
                return;
            }

            if (!track1FullyFadedOut) // Fade out previous track.
            {
                float timeSinceStarted = Time.time - startFadeTime;
                float percentComplete = timeSinceStarted / crossfadeTime;
                if (percentComplete >= 1.0f)
                {
                    percentComplete = 1.0f;
                    track1FullyFadedOut = true;
                    //int lastPlaybackIndex = musicTracksLoaded[1 - activeChannel].index;

                    allMusicTrackPositions[musicTracksLoaded[1 - activeChannel].refName] = GetTimeSamples(1 - activeChannel);
                    //Debug.Log("Stored playback position " + lastPlaybackIndex + " to " + allMusicTrackPositions[lastPlaybackIndex] + " from track index " + (1-activeTrack));
                }
                // Instead of using 1, we should track the previous volume the track was at... maybe it was muted!

                //musicTracks[1 - activeTrack].volume = preCrossfadeTrack2Volume - percentComplete;
                ActuallySetClipVolume(musicChannels[1 - activeChannel], 1 - activeChannel, preCrossfadeTrack2Volume - percentComplete);
            }

            if (!track2FadingIn && !track2FullyFadedIn) // Wait a bit before fading IN track 2
            {
                float timeSinceXfadeStart = Time.time - startFadeTime;
                float percentToTrack2Start = timeSinceXfadeStart / TIME_TO_START_XFADETRACK_2;
                if (percentToTrack2Start >= 1.0f)
                {
                    percentToTrack2Start = 1.0f;
                    // Now begin
                    track2FadingIn = true;
                    timeAtTrack2FadeStart = Time.time;
                }
            }
            else
            {
                float timeSinceTrack2FadeinStart = Time.time - timeAtTrack2FadeStart;
                float percentComplete = timeSinceTrack2FadeinStart / (crossfadeTime * 0.4f);

                if (percentComplete >= 1.0f)
                {
                    percentComplete = 1.0f;
                    track2FullyFadedIn = true;
                    track2FadingIn = false;
                }

                // This conditional was messing things up, I don't remember why I needed it.
                //if (percentComplete >= preCrossfadeTrack1Volume)
                {
                    ActuallySetClipVolume(musicChannels[activeChannel], activeChannel, percentComplete);
                }
            }
        }
        else if (isFading)
        {
            float timeSinceStarted = Time.time - startFadeTime;
            float percentComplete = timeSinceStarted / fadeTime;
            if (percentComplete >= 1.0f)
            {
                isFading = false;
                percentComplete = 1.0f;
            }
            //musicTracks[activeTrack].volume = 1f - percentComplete;

            ActuallySetClipVolume(musicChannels[activeChannel], activeChannel, 1f - percentComplete);

            ActuallySetClipVolume(musicChannels[1 - activeChannel], 1 - activeChannel, 0f); // new to prevent blips when not crossfading
        }
    }
}
