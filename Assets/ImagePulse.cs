using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePulse : MonoBehaviour
{
    public Image myImage;
    public RectTransform rt;

    public float startScale;
    public float finishScale;
    public float startAlpha;
    public float finishAlpha;

    public float cycleTime;

    bool scalingUp;
    float timeAtCycleChange;

    Vector3 currentScale;
    Color currentColor;

    bool waitingToEnable;
    float timeAtEnable;

    const float WAIT_TO_ENABLE_TIME = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        scalingUp = true;
        timeAtCycleChange = Time.time;
        currentScale = new Vector3(startScale, startScale, startScale);
        currentColor = new Color(1f, 1f, 1f, 0f);
        myImage.color = currentColor;
        
    }

    private void OnEnable()
    {
        waitingToEnable = true;
        timeAtEnable = Time.time;
        currentColor = new Color(1f, 1f, 1f, 0f);
        myImage.color = currentColor;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingToEnable)
        {
            if (Time.time - timeAtEnable < WAIT_TO_ENABLE_TIME) return;
        }
        else
        {
            waitingToEnable = false;
        }

        float pComplete = (Time.time - timeAtCycleChange) / cycleTime;

        bool done = false;

        if (pComplete >= 1f)
        {
            pComplete = 1f;
            done = true;
        }

        float targetScaleThisFrame = 0f;
        float targetColorThisFrame = 0f;

        if (scalingUp)
        {
            targetScaleThisFrame = EasingFunction.Linear(startScale, finishScale, pComplete);
            targetColorThisFrame = EasingFunction.Linear(startAlpha, finishAlpha, pComplete);

            //float scale = startScale + addScale;
        }
        else
        {
            targetScaleThisFrame = EasingFunction.EaseInSine(finishScale, startScale, pComplete);
            targetColorThisFrame = EasingFunction.Linear(finishAlpha, startAlpha, pComplete);

        }

        currentScale.x = targetScaleThisFrame;
        currentScale.y = targetScaleThisFrame;
        currentScale.z = targetScaleThisFrame;

        currentColor.a = targetColorThisFrame;

        rt.localScale = currentScale;

        myImage.color = currentColor;

        if (done)
        {
            timeAtCycleChange = Time.time;
            scalingUp = !scalingUp;
        }
    }
}
