using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TDScrollbarManager : MonoBehaviour {

    // Used for sub-windows within UI. If the mouse is in a special area, don't use normal TryScrollPool (etc) scrollbar calls.
    // In other words, this indicates if mouse is in a ScrollRect - override my custom functions
    public static bool mouseIsInSpecialScrollArea;

    public void MouseEnteredSpecialScrollArea()
    {
        mouseIsInSpecialScrollArea = true;
    }

    public void MouseExitedSpecialScrollArea()
    {
        mouseIsInSpecialScrollArea = false;
    }

    public static void SMouseEnteredSpecialScrollArea()
    {
        mouseIsInSpecialScrollArea = true;
    }

    public static void SMouseExitedSpecialScrollArea()
    {
        mouseIsInSpecialScrollArea = false;
    }

}
