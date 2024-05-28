using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_SWITCH
    using nn.hid;
#endif

public class SwitchInputFieldCommunicator : MonoBehaviour
{
#if UNITY_SWITCH
    private NpadId npadId = NpadId.Invalid;
    private NpadState npadState = new NpadState();
    private nn.swkbd.ShowKeyboardArg showKeyboardArg;
#endif

    private void Start()
    {
#if UNITY_SWITCH
        Npad.Initialize();
        Npad.SetSupportedStyleSet(NpadStyle.Handheld | NpadStyle.JoyDual | NpadStyle.FullKey);
        NpadId[] npadIds = { NpadId.Handheld, NpadId.No1 };
        Npad.SetSupportedIdType(npadIds);

        nn.swkbd.Swkbd.Initialize(ref showKeyboardArg, false, true);
#endif
    }

    public void OnContentsChanged()
    {
        return;
#if !UNITY_SWITCH
        return;
#else
        if (UIManagerScript.textInputFieldIsActivated)
        {
            UIManagerScript.DeactivateTextInputField();
        }
#endif


    }
}
