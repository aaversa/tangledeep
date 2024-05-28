/* 
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP 
/// http://www.exceed7.com/introloop
*/

using UnityEngine;
using UnityEngine.Audio;

public class IntroloopSettings : MonoBehaviour{

    ///<summary>
    ///This is the path of IntroloopPlayer template relative to Resources folder.
    ///</summary>
    public static readonly string defaultTemplatePath = "Introloop/IntroloopPlayer";

    ///<summary>
    ///The first call to IntroloopPlayer.Instance will spawn a GameObject of
    ///this name in your scene.
    ///</summary>
    public static readonly string defaultGameObjectName = "IntroloopPlayer";

    /* Planned feature 
  
    ///<summary>
    ///   The ID of this IntroloopPlayer instance. Using the singleton accessor IntroloopPlayer.Instance will
    ///   result in getting the player of ID 0.
    ///
    ///   Get the player of another ID with IntroloopPlayer.InstanceID(id)
    ///
    ///   In template prefab path, the prefab with matching instanceID will get copied to the one in scene.
    ///   Use this fact to assign different AudioMixerGroup to each instance so you can get 
    ///   the high level control of each one separately!
    ///</summary>
    [ImportantInt]
    public int instanceID;

    */

    [Space(8)]
    [Header("Settings")]

    ///<summary>
    ///Drag your AudioMixerGroup to this in IntroloopPlayer template.
    ///</summary>
    public AudioMixerGroup routeToMixerGroup;

    ///<summary>
    /// Method with "Fade" and without fade time parameter will use this length.
    ///</summary>
    [PositiveFloat("Sec.")]
    public float defaultFadeLength;

    ///<summary>
    /// Check this in your IntroloopPlayer template to log various debug data.
    ///</summary>
    public bool logInformation;
}
