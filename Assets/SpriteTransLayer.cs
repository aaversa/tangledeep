using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SpriteTransLayer : MonoBehaviour {

    SpriteRenderer sr;
    Color transColor;
    SpriteRenderer parentSR;
    Movable myMovable;
    public bool initialized;
    public float transMult;

    const int UPDATE_PER_FRAMES = 2;
    int framesToUpdate = 0;
	// Use this for initialization
	void Start ()
    {
        Initialize();
	}

    void OnEnable ()
    {
        Initialize();
    }

    void OnDisable ()
    {
        initialized = false;
    }
	
    void Initialize()
    {
        if (gameObject.transform.parent == null)
        {
            //Debug.Log("Cannot initialize translayer, no parent.");
            return;
        }
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = 1400;
        transColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        parentSR = GetComponentsInParent<SpriteRenderer>()[1];
        Movable[] parentMov = GetComponentsInParent<Movable>();
        if (parentMov.Length > 0)
        {
            myMovable = GetComponentsInParent<Movable>()[0];
        }        
        initialized = true;
        transform.localPosition = Vector3.zero;
        sr.enabled = false;
    }

    // Update is called once per frame
    void Update ()
    {
        if (!initialized)
        {
            Initialize();            
        }
        if (framesToUpdate > 0)
        {
            framesToUpdate--;
            return;
        }
        else
        {
            framesToUpdate = UPDATE_PER_FRAMES;
        }
        if (!parentSR.enabled)
        {
            if (sr.enabled) sr.enabled = false;
            return;
        }
        else
        {
            if (myMovable.overlappingTransparentObject)
            {
                if (!sr.enabled) sr.enabled = true;
            }
            else 
            {
                if (sr.enabled) sr.enabled = false;
                return;
            }
            
        }
        sr.sprite = parentSR.sprite;
        transColor = parentSR.color;
        transColor.a = parentSR.color.a * transMult;
        sr.color = transColor;
        sr.flipX = parentSR.flipX;
        sr.flipY = parentSR.flipY;
	}
}
