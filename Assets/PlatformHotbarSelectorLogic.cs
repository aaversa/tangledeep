using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformHotbarSelectorLogic : MonoBehaviour {

    public GameObject gamepadVersion;

    public GameObject kbMouseVersion;

    private void Awake()
    {
        if (PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
        {
            gamepadVersion.SetActive(true);
        }
        else
        {
            kbMouseVersion.SetActive(true);
        }
    }
}
