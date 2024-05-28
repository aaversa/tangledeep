using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OptionLabelElemental : MonoBehaviour {

    public TextMeshProUGUI label_damage;
    public TextMeshProUGUI label_resist;

	// Use this for initialization
	void Start () {
        label_damage.text = StringManager.GetString("misc_generic_damage");
        label_resist.text = StringManager.GetString("misc_generic_defense");
        FontManager.LocalizeMe(label_damage, TDFonts.WHITE);
        FontManager.LocalizeMe(label_resist, TDFonts.WHITE);
    }
	
}
