using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static void SetCursorVisible(bool shouldBeVisible)
    {
#if UNITY_EDITOR
        Debug.Log("Set cursor visibility to " + shouldBeVisible);
#endif
        if (PlatformVariables.GAMEPAD_ONLY)
        {
                Cursor.visible = false;
        }
        else 
        {
                Cursor.visible = shouldBeVisible;
        }
    }

    public IEnumerator WaitThenCloseControlMapper()
    {
        yield return new WaitForSeconds(0.25f);
#if UNITY_EDITOR
        Debug.Log("Setting cursor visibility to true");
#endif
        cMapper.Close(true);
        Cursor.visible = true;
        masterMouseBlocker.SetActive(true);
        UIManagerScript.dynamicCanvasRaycaster.enabled = true;
    }

    public void CheckAndTryHotbar(HotbarBindable hb)
    {
        switch (hb.actionType)
        {
            case HotbarBindableActions.ABILITY:
                CheckAndTryAbility(hb.ability);
                break;
            case HotbarBindableActions.CONSUMABLE:
                PlayerUseConsumable(hb.consume);
                break;
        }
    }


    public void OpenInputManager(int dummy)
    {
        masterMouseBlocker.SetActive(false);

        cMapper.Open();

    }



    void SetMouseControlState(bool state)
    {
        masterMouseBlocker.SetActive(!state);
        Cursor.visible = state;
        //if (!GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            UIManagerScript.dynamicCanvasRaycaster.enabled = state;
        }        
    }

    public static Vector2 Rotate2DVector(Vector2 rotateMe, float fAngleInDegrees)
    {
        Vector2 retVec = new Vector2();
        float rads = (fAngleInDegrees / 360.0f) * 6.28f;

        retVec.x = rotateMe.x * Mathf.Cos(rads) - rotateMe.y * Mathf.Sin(rads);
        retVec.y = rotateMe.x * Mathf.Sin(rads) + rotateMe.y * Mathf.Cos(rads);
        return retVec;
    }

    /// <summary>
    /// Tells you if the dpad is pressified. Probably works only on console.
    /// </summary>
    /// <returns>True if the dpad is padaladdin'</returns>
    public static bool IsDPadPressed()
    {
        var rw = gmsSingleton.player;

        return rw.GetButton("DPadPressed");
    }
}