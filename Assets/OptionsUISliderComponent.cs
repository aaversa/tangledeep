using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUISliderComponent : MonoBehaviour
{
    public Slider mySlider;

	// Use this for initialization
	void Start ()
	{
	    mySlider.enabled = false;
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void OnPointerEnter()
    {
        mySlider.enabled = true;

        //disable the focus if we're not it. This fixes the weirdness
        //players might see if they are bouncing from keyboard control
        //to mouse control. It will disable any other sliders
        //that have a highlight over them.
        if (UIManagerScript.highlightedOptionsObject != gameObject)
        {
            UIManagerScript.singletonUIMS.DeselectOptionsSlider(0);
        }
    }

    public void OnPointerExit()
    {
        mySlider.enabled = false;
    }


}
