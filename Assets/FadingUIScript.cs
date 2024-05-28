using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using TMPro;

[System.Serializable]
public class FadingUIScript : MonoBehaviour {

    public CanvasGroup myCG;

    public TextMeshProUGUI myText;

    // Keeping track of ALL queued fade objects
    static List<FadingUIScript> currentlyFadingObjects = new List<FadingUIScript>();
    static List<FadingUIScript> queuedFadingObjects = new List<FadingUIScript>();

    float startAlpha = 1.0f;

    public float fadeTime = 1.6f;
    float startY;
    float endY;
    float timeSinceStarted;
    float startTime;
    float percentComplete;
    bool started = false;
    bool waiting = false; // If TRUE, this instance is waiting to start its animation. Will sit at invisible.
    static float timeAtLastStarted = 0.0f;
    static float activeCompletionPercent; // Completion % of the active on-screen item pickup box.
    Vector2 position = Vector2.zero;
    float yPositionOffset = 90f; // Try different values here
    float startPositionOffset = 60f;

    void Awake()
    {
        FontManager.LocalizeMe(myText, TDFonts.WHITE);
    }

    void SetPositionAndDestination(Vector2 startingPosition)
    {
        // Starting position setup
        startingPosition.y += startPositionOffset;
        position = startingPosition;
        transform.position = position;
        startY = startingPosition.y; // Lerp from this, to        
        endY = startingPosition.y + yPositionOffset; // The Final Destination
    }

    public void StartBox(Vector2 pos, Sprite spr, string text)
    {
        // Set up init values.
        myCG.alpha = 0.75f;
        startAlpha = myCG.alpha;
        transform.localScale = new Vector3(1f, 1f, 1f);
        SetPositionAndDestination(pos);
        started = true;

        // Make sure any and all child sprites have the desired sprite (usually an item graphic)
        Image[] arr = GetComponentsInChildren<Image>();
        for (int x = 0; x < arr.Length; x++)
        {
            if (arr[x].gameObject != gameObject)
            {
                arr[x].sprite = spr;
            }
        }

        myText.text = text;

        // If there are already fadeObjects like me queued, don't start yet, just wait around.
        if (currentlyFadingObjects.Count > 0)
        {
            started = false;
            waiting = true;
            myCG.alpha = 0.0f;
            queuedFadingObjects.Add(this);
        }
        else
        {
            // Nothing in queue? Start immediately.
            currentlyFadingObjects.Add(this);
            startTime = Time.time;
            waiting = false;
            timeAtLastStarted = Time.time;
        }
    }

    // Update is called once per frame
    void Update () {

        float adjustedFadeTime = fadeTime;
        int objCount = queuedFadingObjects.Count;

        // If we have a lot of objects to fade, start scaling down the fade time.
        // For each object over X, decrease the time
        if (objCount > 3)
        {
            adjustedFadeTime *= 1 - (0.06f * (objCount - 3));
            adjustedFadeTime = Mathf.Clamp(adjustedFadeTime, fadeTime * 0.35f, fadeTime);
        }

        //Debug.Log("Adjusted fade time is " + adjustedFadeTime + " " + objCount + " max fade: " + fadeTime + " " + currentlyFadingObjects.Count);

        if (waiting) // Another fading UI box is in progress
        {
            if (((Time.time - timeAtLastStarted) >= adjustedFadeTime*0.65f) || objCount <= 0)
            {
                // We are cleared to start! Now It's My Turn!                
                Vector3 heroPos = Camera.main.WorldToScreenPoint(GameMasterScript.heroPCActor.GetPos());
                SetPositionAndDestination(heroPos);
                started = true;
                waiting = false;
                myCG.alpha = startAlpha;
                startTime = Time.time;
                timeAtLastStarted = Time.time;
                currentlyFadingObjects.Add(this);
                queuedFadingObjects.Remove(this);
                return;
            }

            // Not cleared to start. Stay OFF and wait.
            myCG.alpha = 0f;
            return;
        }

        if (!started) return;

        // Nudge ourselves up and fade out
        timeSinceStarted = Time.time - startTime;
        percentComplete = timeSinceStarted / adjustedFadeTime;
        activeCompletionPercent = percentComplete;
        myCG.alpha = Mathf.Lerp(startAlpha, 0f, percentComplete);
        position.y = Mathf.Lerp(startY, endY, percentComplete);
        transform.position = position;

        if (timeSinceStarted >= adjustedFadeTime)
        {
            // We are finished with our animation. Remove self from the list of all fading boxes.
            currentlyFadingObjects.Remove(this);
            GameMasterScript.ReturnToStack(gameObject, gameObject.name.Replace("(Clone)", String.Empty));
        }
	}
}
