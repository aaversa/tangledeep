using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Tangledeep specific info that wraps around a IntroloopAudioObject
public class ScriptableObject_MusicData : ScriptableObject
{
    [SerializeField]
    [Tooltip("The .wav to play.")]
    public AudioClip clip;

    [SerializeField]
    [Tooltip("Friendly name for track reference, ex (dungeontheme1)")]
    public string strReference;

    [SerializeField]
    [Tooltip("File name for actual track, ex (IRL DungeonMusic1 LP)")]
    public string strFileName;

    [SerializeField]
    [Tooltip("Should be added to the DungeonMusic list")]
    public bool isDungeonTrack;

    [SerializeField]
    [Tooltip("And if so, what is the minimum floor for this music to be played on?")]
    public int iMinimumFloor;

    [SerializeField]
    [Tooltip("Should be added to the ItemDream list")]
    public bool isItemDream;

}
