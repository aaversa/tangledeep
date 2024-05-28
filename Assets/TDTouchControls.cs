using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TDTouchControls
{
    const float MAGNITUDE_FOR_ZOOM = 3f;
    const float MIN_TIME_BETWEEN_ZOOMS = 0.25f;

    static float magnitudeAtLastZoomGesture = 0f;
    static float timeAtLastZoom;

    /// <summary>
    /// Support up to 3 simultaneous touches per frame, keep this updated *every* frame
    /// </summary>
    static Touch[] touchesThisFrame;
    static int touchCountThisFrame;

    static bool touchInitialized;

    const float LONG_TOUCH_TIME = 0.8f;

    /// <summary>
    /// Runs at the top of the main UpdateInput function, capturing our touch state this frame.
    /// </summary>
    public static void UpdateTouchControlsForThisFrame()
    {        
        if (!touchInitialized)
        {
            touchesThisFrame = new Touch[3]; 
        }

        touchCountThisFrame = Input.touchCount;

        // Keep track of our touches every frame so we can use these anywhere else
        for (int i = 0; i < touchCountThisFrame; i++)
        {
            touchesThisFrame[i] = Input.GetTouch(i);
        }
    }

    /// <summary>
    /// Returns TRUE if we're absorbing + handling input for any touch actions.
    /// </summary>
    /// <returns></returns>
    public static bool UpdateInput()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            return false;
        }
        // Check for pinches
        if (touchCountThisFrame >= 2)
        {
            // Store both touches.

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchesThisFrame[0].position - touchesThisFrame[0].deltaPosition;
            Vector2 touchOnePrevPos = touchesThisFrame[1].position - touchesThisFrame[1].deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchesThisFrame[0].position - touchesThisFrame[1].position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            bool zoomHappened = false;

            // can't zoom super quickly, so just wait
            if (Time.time - timeAtLastZoom < MIN_TIME_BETWEEN_ZOOMS)
            {
                return false;
            }

            if (deltaMagnitudeDiff < -1f * MAGNITUDE_FOR_ZOOM) // fingers moving apart (pinch out)
            {
                PlayerOptions.zoomScale += 1;
                if (PlayerOptions.zoomScale > 8) PlayerOptions.zoomScale = 8;
                zoomHappened = true;
            }
            else if (deltaMagnitudeDiff > MAGNITUDE_FOR_ZOOM) // pinch in
            {
                PlayerOptions.zoomScale -= 1;
                if (PlayerOptions.zoomScale < 1) PlayerOptions.zoomScale = 1;                
                zoomHappened = true;
            }

            if (zoomHappened)
            {
                magnitudeAtLastZoomGesture = deltaMagnitudeDiff;
                timeAtLastZoom = Time.time;
                GameMasterScript.cameraScript.SetFOVInstant();                
            }

            // Disregard anything else if we have 2+ touch inputs, because that probably means we are trying to do a gesture
            // And in that case, we don't want to move the player etc
            return true;

        }

        return false;
    }

    public static bool GetMouseButtonDown(int buttonID)
    {
#if UNITY_ANDROID || UNITY_IPHONE
        return Input.GetMouseButtonDown(buttonID);
#else
        return Input.GetMouseButtonDown(buttonID);
#endif            
    }

    public static bool GetMouseButtonUp(int buttonID)
    {
#if UNITY_ANDROID || UNITY_IPHONE
        return Input.GetMouseButtonUp(buttonID);
#else
        return Input.GetMouseButtonUp(buttonID);
#endif
    }

    public static bool GetMouseButton(int buttonID)
    {
#if UNITY_ANDROID || UNITY_IPHONE
        return Input.GetMouseButton(buttonID);
#else
        return Input.GetMouseButton(buttonID);
#endif
    }

}
