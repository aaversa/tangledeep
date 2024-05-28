using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDisableRenderer : MonoBehaviour
{
    
    public SpriteRenderer myRenderer;

    public float disableAfterSeconds;

    float timeAtEnable;

    bool finished;

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        timeAtEnable = Time.time;        
        finished = false;
    }

    void Update() 
    {
        if (finished) return;;
        if (Time.time - timeAtEnable >= disableAfterSeconds) 
        {
            myRenderer.enabled = false;
            finished = true;
        }
    }
    
}
