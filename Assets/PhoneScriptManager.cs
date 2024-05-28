using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneScriptManager : MonoBehaviour {

    public Canvas phoneCanvas;

#if UNITY_IPHONE || UNITY_ANDROID
    private void Awake()
    {
        
    }
#endif
}
