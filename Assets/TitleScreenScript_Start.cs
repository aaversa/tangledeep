using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine.UI;
using TMPro;
using System;

#if !UNITY_SWITCH  && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using LapinerTools.Steam.Data;
    using LapinerTools.uMyGUI;
    using Steamworks;
#endif

using System.IO;
using System.Runtime;
using Random = UnityEngine.Random;

public partial class TitleScreenScript
{
    void Awake()
    {
        titleScreenSingleton = this;
        SharedBank.Initialize();

        PrefabSaveDataBlock = prefabSaveDataBlock;

        DataLoadHelper.InitializeResourcePaths(); // MUST be done first before ANYTHING happens.

        if (PlatformVariables.ALLOW_TITLE_SCREEN_LANGUAGE_SELECTION)
        {
            string langFromPrefs = TDPlayerPrefs.GetString(GlobalProgressKeys.LANGUAGE);

            //if (Debug.isDebugBuild) Debug.Log("Reading language from player prefs, and it is " + langFromPrefs);

            if (!string.IsNullOrEmpty(langFromPrefs))
            {
                try
                {
                    GameLanguage = (EGameLanguage)Enum.Parse(typeof(EGameLanguage), langFromPrefs);
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to parse game language from playerprefs due to " + e);
                }
            }
            StringManager.SetGameLanguage(GameLanguage);
        }

        //Shep: Sending in a language now
        StartCoroutine(StringManager.LoadAllStrings(GameLanguage));

        if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            StringManager.LoadTips();
        }

        //disable mouse input on the PS4 (mouse input is coming from touchpad)
#if UNITY_PS4
        ReInput.controllers.Mouse.enabled = false;
#endif

        //we need to load stuff first, so for now disable LogoElemnts, so SimpleTimedMove objects don't start
#if UNITY_PS4 || UNITY_XBOXONE
        LogoElements = GameObject.Find("Logo Elements");
        LogoElements.SetActive(false);
#endif


        if (PlatformVariables.GAMEPAD_ONLY)
        {
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }
        if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            StartCoroutine(FontManager.LoadAllFontsAsync());
        }

        GameStartData.Initialize();
        allowInput = true;

    }

    void Start()
    {
        RandomJobMode.OnReturnToTitleScreen();
        MetaProgressScript.OnReturnToTitleScreen();

        UIManagerScript.HideDialogMenuCursor();

        loadingGame = false;        

        try { PlayerOptions.ReadOptionsFromFile(); }
        catch  //#questionable_try_block
        {
            Debug.Log("Failed to load player options at startup.");
        }

        InitializeMetaProgressAndOtherScripts();

        if (PlatformVariables.FIXED_FRAMERATE)
        {
            Application.targetFrameRate = 60;
        }
        else
        {
            GameMasterScript.UpdateFrameCapFromOptionsValue();
        }

#if UNITY_SWITCH
        //garbage collector please chill
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        sharaModeUnlocked = PlayerPrefs.GetInt("switch_sharacampaignunlocked") == 1;		        
#endif

        InitializeGameStartData();
        InitializeUIManagerAndControlMapperStuff();
        FindMusicManagerAndSelectTitleScreenMusic();
        InitializeWriteBuildText();
        InitializeInputStuff();
#if UNITY_PS4 || UNITY_XBOXONE
        StartCoroutine(WaitNowLoading());
#else
        FindAndConnectBGLayersAndMovers();
        InitializeScrollingBGLogoAndFade();        
        

        SelectAndDisplayDLCImages();

        isFinishing = false;
#endif

    }

    void InitializeMetaProgressAndOtherScripts()
    {
        MetaProgressScript.FlushAllData(GameStartData.saveGameSlot);
        MetaProgressScript.totalCharacters = 0;
        MetaProgressScript.totalDaysPassed = 0;
        MetaProgressScript.playTimeAtGameLoad = 0;
        MetaProgressScript.LoadMetaDictProgress();
        MonsterCorralScript.initialized = false;
        ItemWorldUIScript.initialized = false;
        CorralBreedScript.initialized = false;
		
		if (PlatformVariables.FIXED_FRAMERATE)
		{
	        Application.targetFrameRate = 60;		
		}


    }

    void InitializeGameStartData()
    {
        GameStartData.saveSlotLevels = new int[GameMasterScript.kNumSaveSlots];
        GameStartData.saveSlotNGP = new int[GameMasterScript.kNumSaveSlots];

        if (GameStartData.allFeats == null)
        {
            GameStartData.allFeats = new List<string>();
            GameStartData.allFeats.Add("skill_fivefootstep");
            GameStartData.allFeats.Add("skill_toughness");
            GameStartData.allFeats.Add("skill_fastlearner");
            GameStartData.allFeats.Add("skill_keeneyes");
            GameStartData.allFeats.Add("skill_intimidating");
            GameStartData.allFeats.Add("skill_thirstquencher");
            GameStartData.allFeats.Add("skill_entrepreneur");
            GameStartData.allFeats.Add("skill_foodlover");
            GameStartData.allFeats.Add("skill_scavenger");
            GameStartData.allFeats.Add("skill_rager");
        }
    }

    void InitializeUIManagerAndControlMapperStuff()
    {
        cMapper = GameObject.Find("ControlMapper").GetComponent<ControlMapper>();
        uims = GameObject.Find("UIManager").GetComponent<UIManagerScript>();
        UIManagerScript.FadingToGame = false;
    }

    void InitializeWriteBuildText()
    {
        if (buildText != null)
        {
            Debug.Log("Build: " + buildText.text);
            string finalText = buildText.text;
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                finalText += ", Legend of Shara";
            }
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
            {
                finalText += ", Dawn of Dragons";
            }
            buildText.text = finalText;
        }
    }

    void FindAndConnectBGLayersAndMovers()
    {

        GameObject.Find("Waterfall Anim").GetComponent<Animatable>().SetAnim("Default");
        titleBGLayers = new GameObject[5];
        titleBGLayers[0] = GameObject.Find("Title BG Back");
        titleBGLayers[1] = GameObject.Find("Title BG Mid");
        titleBGLayers[2] = GameObject.Find("Title BG Front");
        titleBGLayers[3] = GameObject.Find("Title BG Frontmost");
        titleBGLayers[4] = GameObject.Find("Title BG Light");		
		
        layerMovers = new SimpleTimedMove[6];
        layerMovers[0] = titleBGLayers[0].GetComponent<SimpleTimedMove>();
        layerMovers[1] = titleBGLayers[1].GetComponent<SimpleTimedMove>();
        layerMovers[2] = titleBGLayers[2].GetComponent<SimpleTimedMove>();
        layerMovers[3] = titleBGLayers[3].GetComponent<SimpleTimedMove>();
        layerMovers[4] = titleBGLayers[4].GetComponent<SimpleTimedMove>();
        layerMovers[5] = sharaPrefab.gameObject.GetComponent<SimpleTimedMove>();
    }

    public static void LocalizeButtonsAndInputFields()
    {
        if (titleScreenSingleton == null)
        {
            return;
        }
        titleScreenSingleton._LocalizeButtonsAndInputFields();
    }

    void _LocalizeButtonsAndInputFields()
    {
        FontManager.LocalizeMe(nameInputText, TDFonts.WHITE);
        FontManager.LocalizeMe(worldSeedText, TDFonts.WHITE);

        FontManager.LocalizeMe(confirmNameButton, TDFonts.BLACK);
        FontManager.LocalizeMe(randomNameButton, TDFonts.BLACK);

        nameInputPlaceholder.text = StringManager.GetString("misc_name_placeholder");
        FontManager.LocalizeMe(nameInputPlaceholder, TDFonts.WHITE);

        worldSeedPlaceholder.text = StringManager.GetString("ui_text_placeholder_inputseed");
        worldSeedPlaceholder.enableAutoSizing = true;
        worldSeedPlaceholder.fontSizeMax = 28f;
        FontManager.LocalizeMe(worldSeedPlaceholder, TDFonts.WHITE);
    }

    void FindMusicManagerAndSelectTitleScreenMusic()
    {
        if (PlatformVariables.USE_INTROLOOP)
        {
            if (mms == null)
            {
                mms = GameObject.Find("AudioManager").GetComponent<MusicManagerScript>();
                mms.MusicManagerStart();
            }

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
                && DLCManager.ShouldShowLegendOfSharaTitleScreen())
            {
                mms.LoadMusicByName_WithIntroloop("shara_titlescreen", true);
            }
            else
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2) && TDPlayerPrefs.GetInt(GlobalProgressKeys.STARTUP_DRAGON_MUSIC) == 1)
                {
                    mms.LoadMusicByName_WithIntroloop("dragontitle", true);
                    TDPlayerPrefs.SetInt(GlobalProgressKeys.STARTUP_DRAGON_MUSIC, 0);
                }
                else
                {
                    mms.LoadMusicByName_WithIntroloop("titlescreen", true);
                    TDPlayerPrefs.SetInt(GlobalProgressKeys.STARTUP_DRAGON_MUSIC, 1);
                }
            }

            mms.Play_WithIntroloop(false, false);
        }
    }

    void InitializeInputStuff()
    {
        framesSinceNeutral = 0;
        timeDirectionPressed = 0f;
        player = ReInput.players.GetPlayer(0);
        inputAxes = new float[2];
        CursorManagerScript.ChangeCursorSprite(CursorSpriteTypes.NORMAL);
    }

    void InitializeScrollingBGLogoAndFade()
    {
        blackFadeImage = GameObject.Find("BlackFade").GetComponent<Image>();

        //if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            logoCG = GameObject.Find("Game Logo").GetComponent<CanvasGroup>();

            scrollingTitleBG = true;
            timeScrollStarted = Time.time;
            if (blackFadeImage == null)
            {
                blackFadeImage = GameObject.Find("BlackFade").GetComponent<Image>();
            }
            blackFading = true;
        }        
    }

    void SelectAndDisplayDLCImages()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
			bool displayConditional = true;
            if (displayConditional)
            {
#if !UNITY_SWITCH
                logoHolder.transform.localPosition = new Vector3(0f, 50f);			
#else
                logoHolder.transform.localPosition = new Vector3(0f, 50f);
#endif
                sharaPrefabImage.color = Color.white;
                sharaPrefab.SetAnimDirectional("Idle", Directions.NORTH, Directions.NORTH, true);
                scrollingLayer03.sprite = sharaBackgroundLayer03Mid;
            }
        }
        else
        {
            sharaPrefabImage.color = UIManagerScript.transparentColor;
            sharaPrefabImage.gameObject.SetActive(false);
        }
    }

    //used on xbox and Ps4
    //we need a delay to load stuff
    private IEnumerator WaitNowLoading()
    {
#if UNITY_PS4 || UNITY_XBOXONE
        yield return new WaitWhile(() => bReadyForMainMenuDialog != true);
        //yield return new WaitForSeconds(xboxWaitTime);
        LogoElements.SetActive(true);
        FindAndConnectBGLayersAndMovers();
        InitializeScrollingBGLogoAndFade();


        SelectAndDisplayDLCImages();

        isFinishing = false;
#else
        return null;
#endif
    }
}