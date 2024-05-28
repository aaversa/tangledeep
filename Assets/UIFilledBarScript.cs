using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UIFilledBarScript : MonoBehaviour {

    public Image fillbar;
    public Image iconRef;
    public TextMeshProUGUI textLabel;
    public TextMeshProUGUI textValue;

    // #todo - Cool bar animations, glimmers, shines etc

    public void UpdateFillAmount(float percentage)
    {
        percentage = Mathf.Clamp(percentage, 0f, 1f);
        fillbar.fillAmount = percentage;
    }

    public void SetLabel(string text)
    {
        textLabel.text = text;
    }

    public void SetTextValue(string text)
    {
        textValue.text = text;
    }


}
