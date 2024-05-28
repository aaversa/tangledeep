using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Globalization;

[SerializeField]
public class LocalizeLaunchScreen : MonoBehaviour
{
    public TMP_FontAsset chineseFontWhite;
    public TMP_FontAsset chineseFontBlack;

    public TextMeshProUGUI header;
    public TextMeshProUGUI displayNote;
    public TextMeshProUGUI resolution;
    public TextMeshProUGUI display;
    public TextMeshProUGUI playButton;

    public GameObject newResolutionDialog;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    public NewResolutionDialog.Scripts.Controller.GraphicSettingsHandler handler;
#endif

    private void Awake()
    {
        TDPlayerPrefs.Initialize();
        TDPlayerPrefs.Load();

        string langFromPrefs = TDPlayerPrefs.GetString(GlobalProgressKeys.LANGUAGE);

        EGameLanguage GameLanguage = EGameLanguage.en_us;

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
        else
        {
            try
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                if (ci.Name == "zh-CN" || ci.Name == "zh-TW" || ci.Name == "zh-HK")
                {
                    GameLanguage = EGameLanguage.zh_cn;
                    TDPlayerPrefs.SetString(GlobalProgressKeys.LANGUAGE, "zh_cn");
                }
            }
            catch(Exception e)
            {
                Debug.Log("Couldn't get culture due to: " + e);
            }
        }

        isChinese = GameLanguage == EGameLanguage.zh_cn;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        handler.isChinese = isChinese;
#endif

    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();

        newResolutionDialog.SetActive(true);
    }

    bool initialized;



    public bool isChinese;

    void Initialize()
    {
        if (isChinese)
        {
            header.font = chineseFontWhite;
            displayNote.font = chineseFontWhite;
            playButton.font = chineseFontWhite;

            header.text = "显示设置";
            displayNote.text = "下次启动显示本界面。该设置可在游戏进行时修改。";
            playButton.text = "开始游戏";

            displayNote.alignment = TextAlignmentOptions.MidlineLeft;

            playButton.fontSize = 22;
            Vector3 curPos = playButton.rectTransform.anchoredPosition;
            curPos.y = 2f;
            playButton.rectTransform.anchoredPosition = curPos;
        }
    }
}
