using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImageEffectScript : MonoBehaviour {


    float timeAtCreation;
    float animTime;
    bool initialized;
    bool finished;
    SpriteRenderer sr;

	// Use this for initialization
	public void Initialize(float aTime, SpriteRenderer sourceSR)
    {
        animTime = aTime;
        timeAtCreation = Time.time;
        initialized = true;
        finished = false;
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }
        sr.sprite = sourceSR.sprite;
        sr.flipX = sourceSR.flipX;
        sr.flipY = sourceSR.flipY;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }
	
	// Update is called once per frame
	void Update () {

        if (!initialized) return;

        float pComplete = (Time.time - timeAtCreation) / animTime;
        if (pComplete > 1.0f)
        {
            pComplete = 1.0f;
            finished = true;
        }

        float srValue = 1f - pComplete - 0.15f;
        if (srValue < 0f) srValue = 0f;

        sr.color = new Color(1f, 1f, 1f, srValue);


        if (finished)
        {
            GameMasterScript.ReturnToStack(gameObject, "SingleAfterImagePrefab");
        }
	}
}
