using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TDSlider : Slider
{
    public void OnMove()
    {
        // IGNORE KEY INPUT
        Debug.Log("Ignore normal axis input.");
    }
}