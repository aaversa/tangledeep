/* 
/// Copyright (c) 2015 Sirawat Pitaksarit, Exceed7 Experiments LP 
/// http://www.exceed7.com/introloop
*/

using UnityEngine;
using System.Collections;

public static class IntroloopLogger {

    public static void Log(string logMessage)
    {
        if(IntroloopPlayer.Instance.introloopSettings.logInformation)
        {
            Debug.Log("[Introloop] " + logMessage);
        }
    }

    public static void LogError(string logMessage)
    {
        if(IntroloopPlayer.Instance.introloopSettings.logInformation)
        {
            Debug.Log("<color=red>[Introloop]</color> " + logMessage);
        }
    }
}
