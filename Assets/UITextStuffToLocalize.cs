using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Using this to consolidate any HUD elements (buttons etc) from miscellaneous UIs that aren't localized elsewhere
// TMPro objects are set in the editor, and we assign the strings below. Initializes on Gameplay start
// Keeping this out of UIManager because that is a mess already.

[System.Serializable]
public class UITextStuffToLocalize : MonoBehaviour {

    [Header("Shop UI")]
    public TextMeshProUGUI shopSortByType;
    public TextMeshProUGUI shopSortByValue;

    [Header("Equipment UI")]
    public TextMeshProUGUI eqSortItemsHeader;
    public TextMeshProUGUI eqFilterItemsHeader;

    [Header("Inventory UI")]
    public TextMeshProUGUI invSortItemsHeader;
    public TextMeshProUGUI invFilterItemsHeader;

    [Header("Cooking UI")]
    public TextMeshProUGUI cookingIngredientsHeader;
    public TextMeshProUGUI cookingSeasoningHeader;
    public TextMeshProUGUI cookingPlaceIngredientsHeader;
    public TextMeshProUGUI cookingResultsText;
    public TextMeshProUGUI cookButton;
    public TextMeshProUGUI cookRepeatButton;
    public TextMeshProUGUI cookResetButton;
    public TextMeshProUGUI placeSeasoningHeader;

    void Start () {
        List<TextMeshProUGUI> objectFontsToSet = new List<TextMeshProUGUI>()
        {
            eqSortItemsHeader,
            eqFilterItemsHeader,
            invSortItemsHeader,
            invFilterItemsHeader,
            placeSeasoningHeader,
            cookingIngredientsHeader,
            cookingSeasoningHeader,
            cookingPlaceIngredientsHeader,
            cookingResultsText
        };

        cookButton.text = StringManager.GetString("ui_cook_button");
        cookResetButton.text = StringManager.GetString("ui_cookreset_button");
        cookRepeatButton.text = StringManager.GetString("ui_cookrepeat_button");
        cookingIngredientsHeader.text = StringManager.GetString("ui_cooking_ingredients_header");
        cookingSeasoningHeader.text = StringManager.GetString("ui_cooking_seasoning_header");
        cookingPlaceIngredientsHeader.text = StringManager.GetString("ui_cooking_place_header");
        placeSeasoningHeader.text = StringManager.GetString("misc_seasoning");

        FontManager.LocalizeMe(cookResetButton, TDFonts.BLACK);
        FontManager.LocalizeMe(cookRepeatButton, TDFonts.BLACK);
        FontManager.LocalizeMe(cookButton, TDFonts.BLACK);

        shopSortByType.text = StringManager.GetString("ui_button_sortbytype");
        shopSortByValue.text = StringManager.GetString("ui_button_sortbyvalue");
        FontManager.LocalizeMe(shopSortByType, TDFonts.BLACK);
        FontManager.LocalizeMe(shopSortByValue, TDFonts.BLACK);

        eqSortItemsHeader.text = StringManager.GetString("misc_sortitems");
        eqFilterItemsHeader.text = StringManager.GetString("misc_filteritems");
        invSortItemsHeader.text = StringManager.GetString("misc_sortitems");
        invFilterItemsHeader.text = StringManager.GetString("misc_filteritems");

        foreach (TextMeshProUGUI tm in objectFontsToSet)
        {
            FontManager.LocalizeMe(tm, TDFonts.WHITE);
        }


    }

}
