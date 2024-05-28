using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SoundCue
{
    public string cueName;
    public AudioClip[] clips;

    [Range(0.0f, 1.0f)]
    public float volume;

    [Range(0.0f, 1.0f)]
    public float randomVolumeVar;

    [Range(-1.0f, 1.0f)]
    public float pitchMod;

    [Range(0.0f, 1.0f)]
    public float randomPitchVar;

    public int voiceLimit;

    public bool voiceVolumeAdjustment;
    public bool loopUntilKilled;
    public MixerGroupNames mixerGroup;

    public SoundCue()
    {

    }
}

[System.Serializable]
public class AudioStuff : MonoBehaviour
{

    public SoundCue[] cues;

    // Use this for initialization
    void Start()
    {

    }

    void OnEnable()
    {
        /* if (!GameMasterScript.actualGameStarted) return;
		if (cues.Length > 0)
        {
            PlayCue("Awake");
        } */
    }

    public void PlayCue(string name)
    {
        bool foundCue = false;
        for (int i = 0; i < cues.Length; i++)
        {
            if (cues[i].cueName == name)
            {
                if (cues[i].clips.Length == 0)
                {
                    //Debug.Log("No cue");
                    return;
                }
                AudioClip clipToPlay = cues[i].clips[Random.Range(0, cues[i].clips.Length)];
                float baseVolume = cues[i].volume;
                baseVolume += UnityEngine.Random.Range(-1f * cues[i].randomVolumeVar, cues[i].randomVolumeVar);
                baseVolume = Mathf.Clamp(baseVolume, 0f, 1f);
                float pitchMod = cues[i].pitchMod;
                pitchMod += UnityEngine.Random.Range(-1f * cues[i].randomPitchVar, cues[i].randomPitchVar);
                pitchMod = Mathf.Clamp(pitchMod, -1f, 1f);

                if (clipToPlay == null)
                {
                    //Debug.Log("WARNING: Clip in index " + i + " is null for cue called " + name + " on obj " + gameObject.name);
                    return;
                }

                MusicManagerScript.singleton.PlaySound(name, clipToPlay, baseVolume, cues[i].mixerGroup, pitchMod, cues[i].voiceVolumeAdjustment, cues[i].loopUntilKilled, cues[i].voiceLimit);
                foundCue = true;
                //Debug.Log("Play cue: " + name + " " + clipToPlay.name + " at volume " + baseVolume + " from " + gameObject.name);
            }
        }
        if (!foundCue)
        {
            //Debug.Log("Could not find cue: " + name + " " + gameObject.name);
        }
    }

    public AudioClip GetClipForCue(string name)
    {
        for (int i = 0; i < cues.Length; i++)
        {
            if (cues[i].cueName == name)
            {
                //string resourceName = "Audio/SFX/" + cues[i].sfxResources[Random.Range(0, cues[i].sfxResources.Length)];
                //return Resources.Load(resourceName) as AudioClip; // Right now this picks purely at random.
                return cues[i].clips[Random.Range(0, cues[i].clips.Length)];
            }
        }
        return null;
    }


}
