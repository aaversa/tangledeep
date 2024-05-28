using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using TMPro;
using UnityEngine.UI;

public partial class TitleScreenScript
{
    IEnumerator PrepareTitleScreenScrolling()
    {
        TitleScreenState = ETitleScreenStates.loading;

#if UNITY_EDITOR
        //Debug.Log("Prepare title screen scrolling!");
#endif

        //Load fonts here, we already have them because they live in the NowLoading asset bundle
        var listPortraits = new List<Sprite>();
        yield return FontManager.LoadAllFontsAsync();    

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            //we're gonna need the portraits now
            string strBundle = Path.Combine(Application.streamingAssetsPath, "portraits");
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBundle);

            //get them
            AssetBundle portraitBundle = TDAssetBundleLoader.GetBundleIfExists("portraits");
            var request = portraitBundle.LoadAllAssetsAsync<Sprite>();
            yield return new WaitWhile(() => !request.isDone);

            //put them
            foreach (var o in request.allAssets)
            {
                var pSprite = o as Sprite;
                if (pSprite != null && !listPortraits.Contains(pSprite))
                {
                    listPortraits.Add(pSprite);
                }
            }

            //load the assetbundle that has the title screen
            strBundle = Path.Combine(Application.streamingAssetsPath, "titlescreen_assets");
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBundle);

            //titlescreen_assets has portraits as well! 
            portraitBundle = TDAssetBundleLoader.GetBundleIfExists("titlescreen_assets");
            request = portraitBundle.LoadAllAssetsAsync<Sprite>();
            yield return new WaitWhile(() => !request.isDone);

            foreach (var o in request.allAssets)
            {
                //if (Debug.isDebugBuild) Debug.Log("Checking asset " + o.name);
                if (!o.name.ToLowerInvariant().Contains("portrait") && o.name != "BattleBard" &&
                    o.name != "BrokenHolo" ) continue;

                var pSprite = o as Sprite;
                if (pSprite != null && !listPortraits.Contains(pSprite))
                {
                    listPortraits.Add(pSprite);
                    //if (Debug.isDebugBuild) Debug.Log("Loading portrait sprite " + pSprite.name);
                }
            }

            //we have loaded each and every portrait
            UIManagerScript.allPortraits = listPortraits.ToArray();
            UIManagerScript.portraitNames = new string[UIManagerScript.allPortraits.Length];
            for (int i = 0; i < UIManagerScript.allPortraits.Length; i++)
            {
                UIManagerScript.portraitNames[i] = UIManagerScript.allPortraits[i].name;
            }


            strBundle = Path.Combine(Application.streamingAssetsPath, "titlescreen_object");
            yield return TDAssetBundleLoader.LoadSpecificAssetBundle(strBundle);

            //spawn the game objects
            AssetBundle abTitleTimes = TDAssetBundleLoader.GetBundleIfExists("titlescreen_object");
            request = abTitleTimes.LoadAllAssetsAsync<GameObject>();
            yield return new WaitWhile(() => !request.isDone);

            //attach them to us in order, and make assigments where necessary.
            titleBGLayers = new GameObject[5];

            foreach (UnityEngine.Object o in request.allAssets)
            {
                GameObject prefab = o as GameObject;

                //do I have to make a new one to use this as a prefab? 
                if (prefab.name.Contains("prefab_savedatablock"))
                {
                    //Determine which prefab we're gonna use based on the game language.
                    //We have two because there are two different character sets and
                    //the spacing is different as well.
                    if (StringManager.gameLanguage == EGameLanguage.jp_japan ||
                        StringManager.gameLanguage == EGameLanguage.zh_cn)
                    {
                        if (prefab.name.Contains("jpcn"))
                        {
                            prefabSaveDataBlock = prefab;
                        }
                    }
                    else if (!prefab.name.Contains("jpcn"))
                    {
                        prefabSaveDataBlock = prefab;
                    }

                    continue;
                }

                GameObject newGO = Instantiate(prefab, titleScreenScrollAnchor.transform);
            }

#if UNITY_PS4 || UNITY_XBOXONE //wait until bReadyForMainMenuDialog is true, then continue
            LoadingWaiterManager.Display(0.2f);
            yield return new WaitWhile(() => bReadyForMainMenuDialog != true);
#endif

            scrollingTitleBG = true;
            timeScrollStarted = Time.time;
            if (blackFadeImage == null)
            {
                blackFadeImage = GameObject.Find("BlackFade").GetComponent<Image>();
            }
            blackFading = true;

            CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
        }

        TitleScreenState = ETitleScreenStates.ready_to_scroll;

        LoadingWaiterManager.Hide(0.2f);
    }
}