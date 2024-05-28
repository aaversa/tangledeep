using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimpleUICenterScript : MonoBehaviour {

    int frames = 0;
    public bool xCenterOnly;
    RectTransform rt;

	// Use this for initialization
	void Start () {
        rt = gameObject.GetComponent<RectTransform>();
	}
	
	// Update is called once per frame
	void Update () {
        frames++;
        if (frames >= 5)
        {
            float yValue = rt.anchoredPosition.y;
            if (!xCenterOnly)
            {
                yValue = 0f;
            }
            rt.anchoredPosition = new Vector2(0f, yValue);
            frames = 0;
        }
	}
}
