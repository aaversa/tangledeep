using UnityEngine;
using System.Collections;
using System;

[System.Serializable]
public class HealthBarScript : MonoBehaviour {

    public Transform childTransform;
    public SpriteRenderer childSR;
    public SpriteRenderer mySR;

    public SpriteRenderer parentSR;

    public bool hasOverlays;
    public bool playerBar;
    public float customAmount;

    int iUpdateFrames; // no need to adjust transparency every single turn for health bars.
    const int UPDATE_FRAME_INTERVAL = 3;

	// Use this for initialization
	void Awake ()
    {
        Initialize();
    }
    void OnEnable()
    {
        Initialize();
    }

    void Initialize()
    {
        if (childTransform == null)
        {
            childTransform = transform.GetChild(0);
            Debug.Log("HealthBar " + gameObject.name + " had no childTransform set on the Switch. It is now: " + childTransform.name);
            childSR = childTransform.gameObject.GetComponent<SpriteRenderer>();
            mySR = GetComponent<SpriteRenderer>();
        }
        hasOverlays = false;
        SetAlpha(1.0f);
        UpdateBar(1.0f);
        iUpdateFrames = 3;
    }

    void Start()
    {

        if (PlatformVariables.OPTIMIZE_SPRITE_MATERIALS)
        {
            if (childSR != null)
            {
                childSR.material = GameMasterScript.spriteMaterialUnlit;
            }

            if (mySR != null)
            {
                mySR.material = GameMasterScript.spriteMaterialUnlit;
            }
        }
    }

    void Update()
    {
        iUpdateFrames--;
        if (iUpdateFrames <= 0)
        {
            iUpdateFrames = UPDATE_FRAME_INTERVAL;
        }
        else
        {
            return;
        }
        if (parentSR == null)
        {
            return;
        }
        if (parentSR.enabled)
        {
            float alpha = parentSR.color.a;
            SetAlpha(alpha);
        }
        else
        {
            SetAlpha(0f);
        }

    }

	public void UpdateBar (float percentage) {        
        try
        {           
            if (!gameObject.activeSelf) return;
            Vector3 localPos = new Vector3(0f, -0.6f, 0f);
            if (hasOverlays)
            {
                localPos.x = 0.23f;
            }
            transform.localPosition = localPos;
            if (percentage <= 0f) percentage = 0f;
            if (percentage > 1.0f)
            {
                percentage = 1.0f;
            }
            /* if (childTransform == null)
            {
                childTransform = transform.GetChild(0);
                childSR = childTransform.gameObject.GetComponent<SpriteRenderer>();
            } */
            Vector3 newPos = Vector3.zero;
            childTransform.localScale = new Vector3(percentage, 1f, 1f);
            float diff = 1f - percentage;
            if (!playerBar)
            {
                newPos.x = -1f * (diff * 0.3f);
            }
            else
            {
                newPos.x = -1f * (diff * customAmount);
            }

            newPos.z = 1f;

            childSR.sortingOrder = 1;
            
            childTransform.localPosition = newPos;
            /* if (mySR.color.a > 0.0f)
            {
                SetAlpha(1.0f);
            } */
            
        }
        catch(Exception e)
        {
            Debug.Log("Health bar error: " + e);
        }

    }

    public void SetAlpha(float alpha)
    {
        if (mySR == null || childSR == null)
        {
            return;
        }
        if (alpha >= 0.6f)
        {
            alpha = 0.6f;
        }
        Color c = new Color(1f, 1f, 1f, alpha);
        mySR.color = c;
        childSR.color = c;
    }

    public void Fadeout(float time)
    {

    }
}
