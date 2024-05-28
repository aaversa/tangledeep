using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GenericButtonForUIObject : MonoBehaviour
{
    public UIManagerScript.UIObject myUIobject;

    public void OnHoverAction()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        UIManagerScript.ChangeUIFocusAndAlignCursor(myUIobject);
    }

    public void OnExitAction()
    {

    }

}
