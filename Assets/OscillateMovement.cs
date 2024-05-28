using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OscillateMovement : MonoBehaviour
{

    public float loopTime = 0.5f;
    public float maxDistance = 0.2f;

    Vector2 startPosition;

    bool initialized = false;

    bool animatingUp = false;
    float timeAtStateChange = 0;

    // Start is called before the first frame update
    void Awake()
    {
        Initialize();        
    }

    void OnEnable()
    {
        Initialize();
    }

    void Initialize()
    {
        if (!initialized) 
        {
            startPosition = transform.localPosition;
            initialized = true;
        }

        StartAnimation();        
    }

    void StartAnimation()
    {
        animatingUp = false;
        timeAtStateChange = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.time;

        float pComplete = (time - timeAtStateChange) / loopTime;

        bool complete = false;

        if (pComplete >= 1f)
        {
            pComplete = 1f;
            complete = true;
        }

        Vector2 pos = transform.localPosition;
        float y = startPosition.y;

        if (animatingUp)
        {
            y = EasingFunction.EaseOutQuad(startPosition.y, startPosition.y + maxDistance, pComplete);
        }
        else
        {
            y = EasingFunction.EaseOutQuad(startPosition.y + maxDistance, startPosition.y, pComplete);            
        }

        pos.y = y;

        transform.localPosition = pos;

        if (complete)
        {
            animatingUp = !animatingUp;
            timeAtStateChange = time;
        }
    }
}
