using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OutOfBoundsUICheckScript : MonoBehaviour {

    TextMeshProUGUI[] myTMPro;
    bool initialized = false;
    bool textActive = true;

    const int FRAMES_TO_UPDATE = 4;
    int frameUpdate = 0;

    void Start()
    {
        myTMPro = GetComponentsInChildren<TextMeshProUGUI>();
        initialized = true;
    }

    void SetTextActiveState(bool state)
    {
        if (textActive == state)
        {
            return;
        }

        textActive = state;
        for (int i = 0; i < myTMPro.Length; i++)
        {
            myTMPro[i].gameObject.SetActive(state);
        }
    }

	void Update () {
        if (!initialized) return;
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        // Don't need to recalculate this every frame, it's pretty expensive.
        frameUpdate++;
        if (frameUpdate < FRAMES_TO_UPDATE)
        {
            return;
        }
        frameUpdate = 0;
        Vector3 tPos = gameObject.transform.position;
        if (textActive)
        {
            if (tPos.y > Screen.height + 25f)
            {
                // We're off the screen. Can disable the object entirely?
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }                
                SetTextActiveState(false);
            }
            else if (tPos.y < -50f)
            {
                // Text hasn't reached the bottom of screen yet.
                SetTextActiveState(false);
            }
        }
        else
        {
            if (tPos.y <= Screen.height + 25f && tPos.y >= -50f)
            {
                SetTextActiveState(true);
            }
        }

	}
}
