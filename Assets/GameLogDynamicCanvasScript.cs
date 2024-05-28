using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogDynamicCanvasScript : MonoBehaviour {

    public bool activeState = true;
    public CanvasGroup myCG;

	// Update is called once per frame
	void Update () {        
		if (activeState && UIManagerScript.AnyInteractableWindowOpenExceptDialog())
        {
            //Debug.Log("<color=red>TURNED OFF</color>");
            activeState = false;
            myCG.alpha = 0f;
            myCG.blocksRaycasts = false;
        }
        if (!activeState && !UIManagerScript.AnyInteractableWindowOpenExceptDialog())
        {
            //Debug.Log("<color=green>TURNED ON</color>");
            activeState = true;
            myCG.alpha = 1f;
            myCG.blocksRaycasts = true;
        }
	}

}
