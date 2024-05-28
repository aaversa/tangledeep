using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Used for localization-related font cleanup
public class UIJournalSheet : MonoBehaviour {

    [Header("Nav Buttons")]
    public TextMeshProUGUI journalRecipeButton;
    public TextMeshProUGUI journalRumorButton;
    public TextMeshProUGUI journalCombatLogButton;
    public TextMeshProUGUI journalMonsterpediaButton;

    [Header("Recipe Stuff")]
    public GameObject recipeHolder;

    [Header("Quest Stuff")]
    public GameObject questHolder;

    [Header("Combat Log")]
    public TextMeshProUGUI combatLogText;

    [Header("Monsterpedia")]
    public TextMeshProUGUI monsterDescText;

    // Use this for initialization
    void Start () {
        List<TextMeshProUGUI> fontsToUpdate = new List<TextMeshProUGUI>();
        journalRecipeButton.text = StringManager.GetString("ui_btn_recipes");
        journalRumorButton.text = StringManager.GetString("ui_btn_rumors");
        journalCombatLogButton.text = StringManager.GetString("ui_btn_combatlog");
        journalMonsterpediaButton.text = StringManager.GetString("ui_btn_monsterpedia");

        fontsToUpdate.Add(journalRecipeButton);
        fontsToUpdate.Add(journalRumorButton);
        fontsToUpdate.Add(journalCombatLogButton);
        fontsToUpdate.Add(journalMonsterpediaButton);

        foreach(TextMeshProUGUI tm in fontsToUpdate)
        {
            FontManager.LocalizeMe(tm, TDFonts.BLACK);
            if (StringManager.gameLanguage == EGameLanguage.de_germany)
            {
                tm.characterSpacing = 0f;
            }
        }

        TextMeshProUGUI[] recipeStuff = recipeHolder.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < recipeStuff.Length; i++)
        {
            FontManager.LocalizeMe(recipeStuff[i], TDFonts.WHITE);
        }

        TextMeshProUGUI[] questStuff = questHolder.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < questStuff.Length; i++)
        {
            FontManager.LocalizeMe(questStuff[i], TDFonts.WHITE);
        }

        FontManager.LocalizeMe(combatLogText, TDFonts.WHITE);
        FontManager.LocalizeMe(monsterDescText, TDFonts.WHITE);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
