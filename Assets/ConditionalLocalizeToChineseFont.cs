using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConditionalLocalizeToChineseFont : MonoBehaviour
{

    bool checkedForChina;

    private void OnEnable()
    {
        TryLocalize();
    }

    private void Start()
    {
        TryLocalize();
    }

    void TryLocalize()
    {
        if (checkedForChina) return;

        if (StringManager.gameLanguage != EGameLanguage.zh_cn)
        {
            return;
        }

        TextMeshProUGUI localTMPro = GetComponent<TextMeshProUGUI>();

        if (localTMPro == null)
        {
            return;
        }

        FontManager.LocalizeMe(localTMPro, TDFonts.WHITE);

        float sizeMax = localTMPro.fontSizeMax;
        sizeMax += 2f;
        localTMPro.fontSizeMax = sizeMax;

        float size = localTMPro.fontSize;
        size += 2f;
        localTMPro.fontSize = size;

        localTMPro.fontStyle = FontStyles.Normal;

        //checkedForChina = true;
    }
}
