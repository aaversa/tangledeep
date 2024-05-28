using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public partial class GameMasterScript
{ 

    // ====================================================================================
    //
    // Loading bar stuff -- a wrapper for a wrapper around the loading bar.
    //
    // Why? Because the loading routines don't always know if they're doing a full new game,
    // or loading an existing one. That matters, because if we are loading a full new game,
    // the loading bar needs to leave room for another 50% where the dungeon is built.
    // ====================================================================================

    private static bool bLoadingBarInLoadGameOnlyMode;

    public enum ELoadingBarIncrementValues
    {
        small = 0,
        medium,
        lots,
        whoa,
        all_the_way_live,
        tiny,
        MAX,
    }

    static void ResetLoadingBar()
    {
        //decide if we are loading a game or starting a new one
        bLoadingBarInLoadGameOnlyMode = false;
    }

    static void SetLoadingBarToLoadGameOnlyMode() { bLoadingBarInLoadGameOnlyMode = true; }

    public static void IncrementLoadingBar(float fDelta)
    {
        if (SharaModeStuff.IsSharaModeActive()) fDelta *= 0.5f;

        //If only loading game and not doing dungeon gen, move up 2x fast
        if (bLoadingBarInLoadGameOnlyMode)
        {
            fDelta *= 2.0f;
        }
#if UNITY_SWITCH
        else
        {
            // actually
            fDelta *= 5f;
        }
        //then send value in

        fDelta *= 0.2f;
#endif
        UIManagerScript.MoveLoadingBar(fDelta);
    }

    public static void IncrementLoadingBar(ELoadingBarIncrementValues val)
    {
        switch (val)
        {
            case ELoadingBarIncrementValues.tiny:
                IncrementLoadingBar(0.0065f);
                break;
            case ELoadingBarIncrementValues.small:
                IncrementLoadingBar(0.01f);
                break;
            case ELoadingBarIncrementValues.medium:
                IncrementLoadingBar(0.03f);
                break;
            case ELoadingBarIncrementValues.lots:
                IncrementLoadingBar(0.06f);
                break;
            case ELoadingBarIncrementValues.whoa:
                IncrementLoadingBar(0.1f);
                break;
            case ELoadingBarIncrementValues.all_the_way_live:
                IncrementLoadingBar(1.0f); //this will cap it
                break;
        }
    }

    public static IEnumerator MakeBouncyText(TMP_Text txtObject, string strBase, float fBounceHeight, float fBounceSpeed)
    {
        while (txtObject != null)
        {
            txtObject.autoSizeTextContainer = false;
            txtObject.text = SillyTextBounce(strBase, fBounceHeight, fBounceSpeed);
            yield return null;
        }
    }

    static string SillyTextBounce(string strBounceMeh, float fBounceHeight, float fBounceSpeed)
    {
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
                strRet.Append("<voffset=" + fBounceVal.ToString("0.00") + "px>" + strBounceMeh[t] + "</voffset>");
            }
        }
        return strRet.ToString();
    }
}
