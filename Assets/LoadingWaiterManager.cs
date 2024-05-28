using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LoadingWaiterManager : MonoBehaviour
{
    public TextMeshProUGUI  bouncyString;
    public float            fTextBounceHeight = 11.0f;
    public float minAlpha = 0.4f;
    public float cycleTime = 0.75f;
    public Animatable       animatedHero;
    //Struggle: We can't load ALL the string data during the logo screen,
    //but we do need to localize this one phrase here. 
    //Eventually we might bake data into the build that we can check directly, a single file perhaps
    //but for now we'll do this.
    public string strEnglishNowLoading;
    public string strJapaneseNowLoading;

    public CanvasGroup myCG;

    private List<GameObject> prefabsToLoadFromForAnims;
    private bool bActive = false;
    private Coroutine coroutineTextBounce;

    public static LoadingWaiterManager _instance;
    private static Dictionary<CharacterJobs, string> dictEnumToJobPrefab = new Dictionary<CharacterJobs, string>
    {
        {
            CharacterJobs.BRIGAND, "Brigand_LoadingScreen"
        },
        {
            CharacterJobs.FLORAMANCER, "Floramancer_LoadingScreen"
        },
        {
            CharacterJobs.SWORDDANCER, "SwordDancer_LoadingScreen"
        },
        {
            CharacterJobs.SPELLSHAPER, "Spellshaper_LoadingScreen"
        },
        {
            CharacterJobs.PALADIN, "Paladin_LoadingScreen"
        },
        {
            CharacterJobs.BUDOKA, "MartialArtist_LoadingScreen"
        },
        {
            CharacterJobs.HUNTER, "Hunter_LoadingScreen"
        },
        {
            CharacterJobs.GAMBLER, "Gambler_LoadingScreen"
        },
        {
            CharacterJobs.HUSYN, "Husyn_LoadingScreen"
        },
        {
            CharacterJobs.SOULKEEPER, "Soulkeeper_LoadingScreen"
        },
        {
            CharacterJobs.EDGETHANE, "Edge Thane_LoadingScreen"
        },
        {
            CharacterJobs.WILDCHILD, "Wildling_LoadingScreen"
        },
        {
            CharacterJobs.DUALWIELDER, "Calligrapher_LoadingScreen"
        },
        {
            CharacterJobs.BERSERKER, "lol wut"
        },
        {
            CharacterJobs.SHARA, "DLC only DRM for you"
        },
        {
            CharacterJobs.MIRAISHARA, "MiraiSharaGreen_LoadingScreen"
        },
    };

    static bool cycleFading;
    static bool cycleFadingDown;
    static float timeAtLastFadeChange;

    public static void Display(float fFadeInTime = 0f)
    {
        //if (Debug.isDebugBuild) Debug.Log("Display now loading text");
        if (_instance == null) return;

        if (_instance.bActive)
        {
            return;
        }
        
        _instance.gameObject.SetActive(true);
        _instance.TurnOn();
        _instance.StartCoroutine(ShowThenFadeIn(fFadeInTime));

        if (GameMasterScript.gmsSingleton != null && !GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            // Don't show hero sprite
        }
        else
        {
            _instance.animatedHero.SetAnimDirectional("Walk", Directions.EAST, Directions.EAST, true);
        }

        



        _instance.bActive = true;
    }

    public static void Hide(float fFadeOutTime = 0f)
    {
        //if (Debug.isDebugBuild) Debug.Log("Hiding Now Loading text");
        if (_instance == null) return;

        if (!_instance.gameObject.activeSelf) return;

        if (fFadeOutTime > 0f)
        {

            _instance.gameObject.SetActive(true);
            _instance.StartCoroutine(FadeThenHide(fFadeOutTime));

            return;
        }

        _instance.TurnOff();
    }

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }        
        _instance = this;
        _instance.enabled = true;
        _instance.bouncyString.color = Color.clear;
        _instance.bouncyString.text = strEnglishNowLoading;

    }

    void TurnOn()
    {
        if (bActive) return;

        //if (Debug.isDebugBuild) Debug.Log("Turning on lower left Now Loading text");

        myCG.alpha = 1f;
        cycleFading = true;
        timeAtLastFadeChange = Time.time;
        cycleFadingDown = true;

        bool showHeroSprite = true;

#if UNITY_SWITCH
        // Switch loads kind of slow & choppy so we can just disable the hero running anim.
        if (animatedHero != null) showHeroSprite = false;
#endif

        if (GameMasterScript.gmsSingleton != null && !GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            showHeroSprite = false;
        }

        if (!showHeroSprite) animatedHero.gameObject.SetActive(false);

            //Check for the localized string here.
            //coroutineTextBounce = StartCoroutine(LoopBouncyText(bouncyString, strEnglishNowLoading, 11.0f, 4.0f));

            /* if (animatedHero != null)
            {
                animatedHero.gameObject.SetActive(false);
            } */


            AttachToCanvas();
    }

    void TurnOff()
    {
        gameObject.SetActive(false);

        if (!bActive)
        {
            return;
        }

        if (coroutineTextBounce != null)
        {
            StopCoroutine(coroutineTextBounce);
        }

        bActive = false;

    }

    //instantly turn on with no animation - used on PS4
    public void TurnOnNoAnimation()
    {
        myCG.alpha = 1f;
        _instance.bouncyString.color = Color.white;
        _instance.animatedHero.GetComponent<Image>().enabled = false;
        gameObject.SetActive(true);
    }

    public static void SetPrefabsListForRunningHeroines(List<GameObject> l)
    {
        _instance.prefabsToLoadFromForAnims = new List<GameObject>();
        foreach (GameObject go in l)
        {
            _instance.prefabsToLoadFromForAnims.Add(go);
        }
    }

    public static GameObject GetPrefabForJob(CharacterJobs jobEnum)
    {
        if (_instance.prefabsToLoadFromForAnims == null)
        {
            //Debug.Log("Don't have prefabs to load for anims.");
            return null;
        }

        var sname = dictEnumToJobPrefab[jobEnum];
        foreach (var go in _instance.prefabsToLoadFromForAnims)
        {
            if (go.name == sname)
            {
                return go;
            }
        }

        Debug.LogError("mb2_gameover.wav you passed an enum and there was no job prefab. Somehow.");
        throw new Exception("mb2_gameover.wav you passed an enum and there was no job prefab. Somehow.");
    }

    static IEnumerator ShowThenFadeIn(float fFadeTime)
    {
        //Don't allow ourselves overlapping coroutines
        if (_instance.bActive)
        {
            yield break;
        }

        //_instance.TurnOn();
        _instance.bouncyString.color = Color.clear;
        _instance.animatedHero.GetComponent<Image>().enabled = false;

        yield return null;

#if UNITY_SWITCH
#else
        _instance.animatedHero.GetComponent<Image>().enabled = true;
#endif        

        float fTime = 0f;
        while (fTime < fFadeTime)
        {
            float fDelta = fTime / fFadeTime;
            _instance.bouncyString.color = Color.Lerp(Color.clear, Color.white, fDelta);
            _instance.animatedHero.opacityMod = fDelta;
            fTime += Time.deltaTime;
            yield return null;
        }

        _instance.bouncyString.color = Color.white;
        _instance.animatedHero.opacityMod = 1.0f;

        cycleFading = true;
    }

    private void Update()
    {
        if (!bActive) return;

#if UNITY_SWITCH
        myCG.alpha = 1f;
        return;
#endif

        float pComplete = (Time.time - timeAtLastFadeChange) / cycleTime;

        if (pComplete >= 1f)
        {
            pComplete = 1f;
        }

        float cgValue = 0f;


        if (cycleFadingDown)
        {
            cgValue = EasingFunction.EaseInOutSine(1f, minAlpha, pComplete);
        }
        else
        {
            cgValue = EasingFunction.EaseInOutSine(minAlpha, 1f, pComplete);
        }

        myCG.alpha = cgValue;

        if (pComplete >= 1f)
        {
            cycleFadingDown = !cycleFadingDown;
            timeAtLastFadeChange = Time.time;
        }
    }

    static IEnumerator FadeThenHide(float fFadeTime)
    {
        //Debug.Log("Prepare to fade then hide loading manager");
        float fTime = 0f;
        while (fTime < fFadeTime)
        {
            float fDelta = fTime / fFadeTime;
            _instance.bouncyString.color = Color.Lerp(Color.white, Color.clear, fDelta);
            _instance.animatedHero.opacityMod = 1.0f - fDelta;
            fTime += Time.deltaTime;
            yield return null;
        }

        _instance.TurnOff();
    }

    static IEnumerator LoopBouncyText(TMP_Text txtObject, string strBase, float fBounceHeight, float fBounceSpeed)
    {
        while (txtObject != null && _instance.bActive)
        {
            txtObject.autoSizeTextContainer = false;
            txtObject.text = SillyTextBounce(strBase, fBounceHeight, fBounceSpeed);
            yield return null;
        }
    }

    //:(
    public static string SillyTextBounce(string strBounceMeh, float fBounceHeight, float fBounceSpeed)
    {
        return strBounceMeh;
        /*
        StringBuilder strRet = new StringBuilder();
        for (int t = 0; t < strBounceMeh.Length; t++)
        {
            float fBounceVal = fBounceHeight + Mathf.Sin((Time.time + 0.3f * t) * fBounceSpeed) * fBounceHeight;

            //don't append 0 pixel changes or <voffset> won't parse.
            if (Mathf.Abs(fBounceVal) < 1.01f)
            {
                strRet.Append(strBounceMeh[t]);
            }
            else
            {
                strRet.Append("<voffset=");
                strRet.Append(fBounceVal.ToString("0.00"));
                strRet.Append("px>");
                strRet.Append(strBounceMeh[t]);
                strRet.Append("</voffset>");
            }
        }
        return strRet.ToString();
        */
    }

    public static void OnSceneEnd()
    {

    }

    public static void OnSceneBegin()
    {
        AttachToCanvas();
    }

    static void AttachToCanvas()
    {
        return;
        GameObject go = GameObject.Find("LoadingCanvas");

        _instance.transform.SetParent(go.transform);

        var myRT = _instance.transform as RectTransform;
        myRT.anchoredPosition = new Vector2(-192,128);

        DontDestroyOnLoad(go);

    }
}
