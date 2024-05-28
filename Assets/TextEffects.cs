using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class TextEffects : MonoBehaviour {

    public float timeToFadeIn;
    float timeAtSpawn;
    bool fadingIn;
    bool started = false;
    CanvasRenderer myCanvasRenderer;
    float percentComplete;

    float timeFlashStart;
    public bool textFlash;
    public float flashCycleTime;
    bool flashDirection; // true is fading IN

    TextMeshProUGUI myText;

	// Use this for initialization
	void Start () {
        myCanvasRenderer = GetComponent<CanvasRenderer>();
	    myText = GetComponent<TextMeshProUGUI>();
        myCanvasRenderer.SetAlpha(0.0f);
        timeAtSpawn = Time.fixedTime;
        started = true;
        fadingIn = true;
        if (textFlash)
        {
            flashDirection = false;
            timeFlashStart = Time.fixedTime;
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (started && textFlash && !fadingIn)
        {
            percentComplete = (Time.fixedTime - timeFlashStart) / flashCycleTime;
            if (flashDirection)
            {
                myCanvasRenderer.SetAlpha(percentComplete);
            }
            else
            {
                myCanvasRenderer.SetAlpha(1f - percentComplete);
            }
            
            if (percentComplete >= 1.0f)
            {
                percentComplete = 0f;
                flashDirection = !flashDirection;
                timeFlashStart = Time.fixedTime;
            }

        }

        if (started && fadingIn)
        {
            percentComplete = (Time.fixedTime - timeAtSpawn) / timeToFadeIn;
            myCanvasRenderer.SetAlpha(percentComplete);
            if (percentComplete >= 1.0f)
            {
                fadingIn = false;
            }

            if (myText != null)
            {
                CanvasRenderer[] renderersOnMe = myText.GetComponentsInChildren<CanvasRenderer>();
                foreach (var cr in renderersOnMe)
                {
                    cr.SetAlpha(percentComplete);
                }
            }
        }
    }
}
