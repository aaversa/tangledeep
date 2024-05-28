using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InheritSpriteOrderScript : MonoBehaviour {

    SpriteRenderer mySR;
    SpriteRenderer parentSR;
    bool initialized;

	// Use this for initialization
	void Awake () {
        mySR = GetComponent<SpriteRenderer>();
        parentSR = transform.parent.GetComponent<SpriteRenderer>();
        initialized = true;
	}
	
	// Update is called once per frame
	void Update () {
        if (!initialized) return;

        mySR.sortingOrder = parentSR.sortingOrder;	
	}
}
