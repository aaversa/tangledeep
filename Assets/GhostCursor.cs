using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Moves an image (usually a cursor) from one location to another on the canvas
public class GhostCursor : MonoBehaviour
{
    public float RotationRatePerSecondInDegrees;

    private Image   myImage;

    //will fade from 1.0 to 0.0 alpha at this last percent of the travel time.
    private float fFadeAtEndPercent;

    [HideInInspector]
    public string strPoolName;

    void Awake()
    {
        myImage = GetComponent<Image>();
    }

    void Update()
    {
        //if we spinnin', then spin
        if (RotationRatePerSecondInDegrees != 0f)
        {
            RectTransform rt = transform as RectTransform;
            Vector3 rot = rt.localEulerAngles;
            rot.z += RotationRatePerSecondInDegrees * Time.deltaTime;
            rt.rotation = Quaternion.Euler(rot);
        }
    }

    public void StartMove(Vector2 start, Vector2 end, float fTime, LeanTweenType easing, float fadePercent = 0f, bool bPauseGameDuringTween = false, Action funcOnEnd = null)
    {
        myImage.enabled = true;

        //assume the position
        RectTransform rt = transform as RectTransform;
        rt.anchoredPosition = start;

        //start the move
        var tween = LeanTween.move(rt, end, fTime).setEase(easing);

        if (funcOnEnd != null)
        {
            tween.setOnComplete(funcOnEnd);
        }

        //track this for our coroutine
        fFadeAtEndPercent = fadePercent;

        //clean up when done, and hold up the game if requested
        if (bPauseGameDuringTween)
        {
            GameMasterScript.StartWatchedCoroutine(HideAfterMove(fTime));
        }
        else
        {
            StartCoroutine(HideAfterMove(fTime));
        }
    }

    IEnumerator HideAfterMove(float fTime)
    {
        //wait before fading -- this waits 100% if fade == 0;
        float fFadeDuration = fTime * fFadeAtEndPercent;
        float fWaitTime = fTime - fFadeDuration;

        yield return new WaitForSeconds(fWaitTime);

        //now wait for fFadeDuration and fade out as we do.
        float fImageStartAlpha = myImage.color.a;

        float fFadeTick = 0f;
        Color c = myImage.color;
        while (fFadeTick < fFadeDuration)
        {
            c.a = Mathf.Lerp(fImageStartAlpha, 0f, fFadeTick / fFadeDuration);
            myImage.color = c;
            fFadeTick += Time.deltaTime;
            yield return null;
        }

        //all done, disable the image, BUT restore the original transparency
        myImage.enabled = false;
        myImage.color = new Color(c.r, c.g, c.b, fImageStartAlpha);

        GameMasterScript.ReturnToStack(gameObject, strPoolName);
    }
}

public partial class UIManagerScript
{
    public static GhostCursor GetGhostCursor(string strPrefab)
    {
        //we don't have one yet, oh noes!
        GameObject go = GameMasterScript.TDInstantiate(strPrefab); // (); // Instantiate(singletonUIMS.prefab_GhostCursor, GameObject.Find("Canvas").transform);
        go.GetComponent<Image>().rectTransform.SetParent(GameObject.Find("Canvas").transform);
        go.transform.SetAsLastSibling();
        var ghoooost = go.GetComponent<GhostCursor>();
        ghoooost.strPoolName = strPrefab;
        return ghoooost;
    }


    /// <summary>
    /// This will send a GhostCursor from some point on screen to the position of a given RectTransform, +offset.
    /// Remember that 0,0 is the dead center of the screen, and Y- is DOWN towards the bottom.
    /// </summary>
    /// <param name="vStart">The canvas position to start at. See note in summary!</param>
    /// <param name="rt">Goal RectTransform</param>
    /// <param name="fTime">The length of time for the movement to take</param>
    public static void SendGhostCursorFromPointToPoint(string strCursorPrefab, Vector2 vStart, RectTransform rt, float fTime)
    {
        SendGhostCursorFromPointToPoint(strCursorPrefab, vStart, rt, fTime, Vector2.zero);
    }

    /// <summary>
    /// This will send a GhostCursor from some point on screen to the position of a given RectTransform, +offset.
    /// Remember that 0,0 is the dead center of the screen, and Y- is DOWN towards the bottom.
    /// </summary>
    /// <param name="vStart">The canvas position to start at. See note in summary!</param>
    /// <param name="rt">Goal RectTransform</param>
    /// <param name="fTime">The length of time for the movement to take</param>
    /// <param name="vOffsetFromGoal">Offset in pixels from rt's center, this will adjust the final goal position.</param>
    public static void SendGhostCursorFromPointToPoint(string strCursorPrefab, Vector2 vStart, RectTransform rt, float fTime, Vector2 vOffsetFromGoal)
    {
        //#todo: Uh yeah so the cursor needs to face left when moving left.

        Vector2 vScaledSize = Vector2.Scale(rt.sizeDelta, rt.lossyScale);
        var rectPosition = new Rect(rt.position.x, Screen.height - rt.position.y, vScaledSize.x, vScaledSize.y);
        rectPosition.x -= rt.pivot.x * vScaledSize.x;
        rectPosition.y -= (1.0f - rt.pivot.y) * vScaledSize.y;

        //the rectPostion is based on 0,0 at top left, with screen-down being +y
        //our canvas's center is screen.center with screen-down being -y
        rectPosition.x -= Screen.width / 2;
        rectPosition.y -= Screen.height / 2;
        rectPosition.y *= -1f;

        rectPosition.x *= (1 / rt.lossyScale.x);
        rectPosition.y *= (1 / rt.lossyScale.y);
        rectPosition.width *= (1 / rt.lossyScale.x);
        rectPosition.height *= (1 / rt.lossyScale.y);

        Vector2 vGoal = new Vector2(rectPosition.position.x - rectPosition.width / 2, rectPosition.position.y - rectPosition.height / 2);
        vGoal += vOffsetFromGoal;
        GetGhostCursor(strCursorPrefab).StartMove(vStart, vGoal, fTime, LeanTweenType.easeOutExpo, 0.1f);
    }
}
