using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsAndFramerateManager
{
    static int bufferedVSyncCount = 0;
    static int bufferedFrameRate = Application.targetFrameRate;

    const int FIXED_MOBILE_FRAMERATE = 60;

    public static void OnStartLoad()
    {
if (PlatformVariables.FIXED_FRAMERATE) return;

        bufferedFrameRate = Application.targetFrameRate;
        SetApplicationFPS(-1);
        bufferedVSyncCount = QualitySettings.vSyncCount;

        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            QualitySettings.vSyncCount = 0;
        }        
    }

    public static void OnEndLoad()
    {
	
if (PlatformVariables.FIXED_FRAMERATE) return;

	
#if UNITY_ANDROID || UNITY_IPHONE
        SetApplicationFPS(FIXED_MOBILE_FRAMERATE);
#else
        SetApplicationFPS(bufferedFrameRate);
#endif

        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            QualitySettings.vSyncCount = bufferedVSyncCount;
        }        
    }

    public static void SetApplicationFPS(int value)
    {
        if (PlatformVariables.FIXED_FRAMERATE)
        {
            Application.targetFrameRate = 60;
            return;
        }
        Application.targetFrameRate = value;

        //if (Debug.isDebugBuild) Debug.Log("SET APPLICATION FPS TO: " + value);

    }

}
