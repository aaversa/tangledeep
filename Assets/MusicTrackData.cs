using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTrackData
{
    public AudioClip clip;
    public string trackFileName;
    public string refName;
    public int loopPoint;
    public bool looping;
    public int index;
    public string bundleName;

    //data to help select tracks when exploring areas that don't have
    //predefined tracks
    public bool isDungeonTrack;
    public bool isItemDreamTrack;
    public int iMinimumFloor;

    private static int iNextIndex = 0;
    public MusicTrackData(string filename, string reference, int loop, string bun = "audio")
    {
        trackFileName = filename;
        refName = reference;
        loopPoint = loop;
        looping = true;
        bundleName = bun;
    }

    public MusicTrackData(ScriptableObject_MusicData scmd)
    {
        clip = scmd.clip;
        trackFileName = scmd.strFileName;
        refName = scmd.strReference;
        isDungeonTrack = scmd.isDungeonTrack;
        isItemDreamTrack = scmd.isItemDream;
        iMinimumFloor = scmd.iMinimumFloor;

        bundleName = "music_" + refName.ToLower();
    }
}