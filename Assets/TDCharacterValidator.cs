using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TDCharacterValidator : MonoBehaviour {

    static TMP_InputField myInputField;
    static TMP_FontAsset currentFont;

    void Awake()
    {
        if (myInputField == null)
        {
            myInputField = GetComponent<TMP_InputField>();
        }

        if (myInputField != null)
        {
            myInputField.onValidateInput = ValidateInput;
        }                
    }

    static char ValidateInput(string text, int charIndex, char addedChar)
    {
        if (currentFont == null)
        {
            currentFont = FontManager.GetFontAsset(TDFonts.WHITE);
        }
        return currentFont.HasCharacter(addedChar) ? addedChar : '?';
    }
}
