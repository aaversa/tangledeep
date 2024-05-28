using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParallaxHandler : MonoBehaviour {

    public float startXPosition;
    public float startYPosition;
    public float parallaxMult;
    public float playerMinX;
    public float playerMaxX;
    public float playerMinY;
    public float playerMaxY;
    public bool moveHorizontal;
    public bool moveVertical;
    	
	// Update is called once per frame
	void Update ()
	{
        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.BACKTOTITLE || !GameMasterScript.gameLoadSequenceCompleted || GameMasterScript.applicationQuittingOrChangingScenes)
        {
            return;
        }

        Vector2 pos = new Vector2();
	    Vector2 vFocusPosition = GameMasterScript.cameraScript.transform.position;

        //If the camera is in bounds, adjust accordingly
	    if (vFocusPosition.x >= playerMinX && vFocusPosition.x <= playerMaxX)
        {
            pos.x = startXPosition - (vFocusPosition.x * parallaxMult);
        }
        else if (vFocusPosition.x < playerMinX)
        {
            pos.x = startXPosition - (playerMinX * parallaxMult);
        }
        else
        {
            pos.x = startXPosition - (playerMaxX * parallaxMult);
        }

        //Same with y values
        if (vFocusPosition.y >= playerMinY && vFocusPosition.y <= playerMaxY)
        {
            pos.y = startYPosition + (vFocusPosition.y * parallaxMult);
        }
        else if (vFocusPosition.y < playerMinY)
        {
            pos.y = startYPosition + (playerMinY * parallaxMult);
        }
        else
        {
            pos.y = startYPosition + (playerMaxY * parallaxMult);
        }

        if (!moveVertical)
        {
            pos.y = startYPosition;
        }
        if (!moveHorizontal)
        {
            pos.x = startXPosition;
        }

        transform.position = pos;
	}
}
