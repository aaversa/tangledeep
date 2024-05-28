using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SimpleLightCycle : MonoBehaviour {

    Image myImage;
    public float cycleTime;
    public float transOffset;
    bool fadeOut = true;
    float startCycleTime = 0f;

    // Use this for initialization
    
    void Start () {
        myImage = GetComponent<Image>();
        startCycleTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
		if (myImage != null)
        {
            float pcomplete = (Time.time - startCycleTime) / cycleTime;

            

            if (fadeOut)
            {
                float cSlerp = Mathfx.Sinerp(1f, 0.5f, pcomplete);
                //myImage.color = new Color(1f, 1f, 1f, 1f - (pcomplete / 2f));
                myImage.color = new Color(1f, 1f, 1f, cSlerp + transOffset);
            }
            else
            {
                float cSlerp = Mathfx.Sinerp(0.5f, 1.0f, pcomplete);
                //myImage.color = new Color(1f, 1f, 1f, 0.5f + (pcomplete / 2f));
                myImage.color = new Color(1f, 1f, 1f, cSlerp + transOffset);
            }
            
            if (pcomplete >= 1.0f)
            {
                startCycleTime = Time.time;
                fadeOut = !fadeOut;
            }
        }
	}
}
