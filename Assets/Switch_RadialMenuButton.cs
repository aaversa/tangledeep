using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class Switch_RadialMenuButton : MonoBehaviour
{
    //the shiny border around the icon
    [SerializeField]
    private Image imageBorder;

    //the icon itself
    [SerializeField]
    private Image imageObject;
    
    //glowing selection 
    [SerializeField]
    private Image imageGlowyBorder;

    public Vector2 offsetFromCoreWhenActive;
    public string strFunctionOnSelect;

    private ISelectableUIObject myObject;

    private MethodInfo func_onSelect;

    public Coroutine coroutineRotation;

    public ISelectableUIObject GetObject() { return myObject; }


    public void Start()
    {
        //Just border, since image is child of border
        AdjustRTForScale(imageBorder.transform as RectTransform);
    }

    void AdjustRTForScale(RectTransform rt)
    {
        rt.localScale = new Vector3( Switch_RadialMenu.fScaleValue, Switch_RadialMenu.fScaleValue, Switch_RadialMenu.fScaleValue );
    }


    public void SetObject(ISelectableUIObject newObject)
    {
        myObject = newObject;
        if (myObject == null)
        {
            imageObject.enabled = false;
        }
        else
        {
            imageObject.enabled = true;
            imageObject.sprite = myObject.GetSpriteForUI();
        }
    }

    public void SetActionOnSelect(MethodInfo act)
    {
        func_onSelect = act;
    }

    public void OnSelect()
    {
        //play a sound?

        //do a thing?
        if (func_onSelect != null)
        {
            func_onSelect.Invoke(null,null);
        }
    }

    public void FadeIn( float fTime )
    {
        imageBorder.color = Color.clear;
        imageObject.color = Color.clear;
        LeanTween.color(transform as RectTransform, Color.white, fTime);
    }

    public void FadeOut(float fTime)
    {
        LeanTween.color(transform as RectTransform, Color.clear, fTime);
    }

    /// <summary>
    /// Use stick input to determine how glowy our border should be.
    /// </summary>
    /// <param name="controllerDirection"></param>
    public void SetGlowySelectRatio(Vector2 controllerDirection)
    {
        float dotVal = Vector2.Dot(offsetFromCoreWhenActive, controllerDirection);
        dotVal = dotVal * dotVal * dotVal;
        
        var c = imageGlowyBorder.color;
        c.a = dotVal;
        imageGlowyBorder.color = c;
    }

    /// <summary>
    /// Gets the dot product of a vector against our position in the ring.
    /// </summary>
    /// <param name="controllerDirection">Ideally, the direction the stick is pointing</param>
    /// <returns></returns>
    public float GetDotValueAgainstStickInput(Vector2 controllerDirection)
    {
        return Vector2.Dot(offsetFromCoreWhenActive, controllerDirection);
    }
    
    public IEnumerator RotateMeIntoPlace(Vector2 vGoalTranslationNormalized, float fDegreesOffsetAtStart, float fAnimTime,
        float fStartLength, float fEndLength, bool bDisableOnEnd )
    {
        float fTime = 0f;
        RectTransform rt = transform as RectTransform;
        
        while (fTime < fAnimTime)
        {
            //tick up, don't exceed 100%
            fTime += Time.deltaTime;
            fTime = Math.Min(fTime, fAnimTime);

            float fRatio = fTime / fAnimTime;

            //get a rotated vector based on our eventual goal
            float fDegrees = Mathf.Lerp(fDegreesOffsetAtStart, 0f, fRatio);
            Vector2 vAdjustedVector = GameMasterScript.Rotate2DVector(vGoalTranslationNormalized, fDegrees);

            //adjust the length
            vAdjustedVector *= Mathf.Lerp(fStartLength, fEndLength, fRatio);

            //here we go
            rt.localPosition = vAdjustedVector;
            
            //glowy border off during this
            imageGlowyBorder.color = Color.clear;

            yield return null;
        }
        
        imageGlowyBorder.color = new Color(1,1,1,0);

        if (bDisableOnEnd)
        {
            gameObject.SetActive(false);
        }
    }
}
