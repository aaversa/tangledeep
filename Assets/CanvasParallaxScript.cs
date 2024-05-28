using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CanvasParallaxScript : MonoBehaviour {

    public Vector3 startPosition;
    public float animationTime;

    float timeAtParallaxStart;
    bool animComplete;
    bool initialized;
    Vector3 finishPosition;

    void Start()
    {
        // Handle this on enable
    }

    void OnEnable()
    {
    }

    public void Initialize()
    {
        finishPosition = transform.localPosition;
        transform.localPosition = startPosition;
        timeAtParallaxStart = Time.time;        
        initialized = true;
        animComplete = false;
    }

    void Update()
    {
        if (!initialized) return;
        if (!animComplete)
        {
            float percentComplete = (Time.time - timeAtParallaxStart) / animationTime;
            if (percentComplete > 1.0f)
            {
                percentComplete = 1.0f;
                animComplete = true;
            }
            float xLerp = Mathfx.Sinerp(startPosition.x, finishPosition.x, percentComplete);
            float yLerp = Mathfx.Sinerp(startPosition.y, finishPosition.y, percentComplete);
            //Vector3 newPos = Vector3.Lerp(startPosition, finishPosition, percentComplete);
            Vector3 newPos = new Vector3(xLerp, yLerp, 0f);
            transform.localPosition = newPos;
        }
    }

}
