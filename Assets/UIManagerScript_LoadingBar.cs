using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManagerScript
{
    public static void ToggleLoadingBar()
    {
        loadingGroup.GetComponent<CanvasGroup>().alpha = Mathf.Abs(1.0f - loadingGroup.GetComponent<CanvasGroup>().alpha);
        if (loadingGroup.GetComponent<CanvasGroup>().alpha == 0f)
        {
            loadingGroup.SetActive(false);
        }
        else
        {
            loadingGroup.SetActive(true);
        }
    }

    public static void DisableLoadingBar()
    {
        //loadingGroup.SetActive(false);
    }

    public static void WriteLoadingText(string text)
    {
        prettyLoadingArtComponent.SetTextString(text, true);
        //loadingBarText.text = text;
    }

    public static void FillLoadingBar(float percent)
    {
        prettyLoadingArtComponent.SetFillRatio(percent);
        //loadingBar.fillAmount = percent;
    }

    public static void MoveLoadingBar(float percent)
    {
        prettyLoadingArtComponent.AdjustFillRatio(percent);
    }

    public static float GetLoadingBarFillValue()
    {
        return prettyLoadingArtComponent.GetFillRatio();
    }
}