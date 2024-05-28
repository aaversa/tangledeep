/* 
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP 
/// http://www.exceed7.com/introloop
*/

using UnityEngine;
using System.Collections;
using System;

public class IntroloopAudio : ScriptableObject
{
	
	[SerializeField, Range(0,1)]
	private float volume;
	[SerializeField, Range(0.1f,3)]
	private float pitch = 1;

    [SerializeField]
    public AudioClip audioClip;
    [SerializeField,PositiveFloat]
    internal float introBoundary;
    [SerializeField,PositiveFloat]
    internal float loopingBoundary;
    [SerializeField]
    internal bool nonLooping;
    [SerializeField]
    internal bool loopWholeAudio;


	public float Volume {
		get {
			return this.volume;
		}
		set {
			this.volume = value;
		}
	}

	internal float Pitch {
		get {
			return this.pitch;
		}
	}
	
    internal float IntroLength
    {
        get{
            return introBoundary/pitch;
        }
    }

    internal float LoopingLength
    {
        get{
            return (loopingBoundary - introBoundary)/pitch;
        }
    }

    public float ClipLength
    {
        get{
            return audioClip.length/pitch;
        }
    }

    //This is for timing the seam between intro and looping section instead of IntroLength
    //It intentionally does not get divided by pitch. Unity's audio position is not affected by pitch.
    internal float LoopBeginning
    {
        get{
            return introBoundary;
        }
    }

	internal void Preload()
	{
        audioClip.LoadAudioData();
	}
	
	internal void Unload()
	{
        audioClip.UnloadAudioData();
	}
}

