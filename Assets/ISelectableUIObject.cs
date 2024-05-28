using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectableUIObject
{
    Sprite GetSpriteForUI();
    string GetNameForUI();
    string GetInformationForTooltip();
}
