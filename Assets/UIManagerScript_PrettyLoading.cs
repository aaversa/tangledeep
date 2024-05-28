using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManagerScript
{
    static void DoLoadPrettyLoadingScreen()
    {
        //:(
        if (GameMasterScript.gmsSingleton == null ||
            GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            return;
        }

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            var AB = TDAssetBundleLoader.GetBundleIfExists("nowloading_object");
            var go = AB.LoadAsset<GameObject>("prefab_loadinglogo");
            if (UIManagerScript.prettyLoadingArtComponent == null)
            {
                UIManagerScript.prettyLoadingArtComponent = Instantiate(go).GetComponent<Switch_PrettyLoadingArtComponent>();
            }            
        }

        TurnOnPrettyLoading();
    }

    public static void TurnOnPrettyLoading(float fFadeTime = 0f, float fillPerSecondTime = 0.2f)
    {
        prettyLoadingArtComponent.TurnOn(fFadeTime, fillPerSecondTime);
    }

    public static void TurnOffPrettyLoading(float fFadeTime = 0f, float fDelayBeforeFade = 0f)
    {
        prettyLoadingArtComponent.TurnOff(fFadeTime, fDelayBeforeFade);
    }
}