using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManagerScript
{
    public void TitleScreenStart(bool firstStart)
    {
        FadingToGame = false;
        if (firstStart && characterCreationBG == null)
        {
            characterCreationBG = GameObject.Find("BG Card");
            if (charCreationManager == null)
            {
                charCreationManager = GameObject.Find("CharacterSelect").GetComponent<CharCreation>();
            }			
            if (characterCreationBG != null)
            {
                characterCreationBG.GetComponent<CanvasGroup>().alpha = 1.0f;
            }
            charCreationManager = GameObject.Find("CharacterSelect").GetComponent<CharCreation>();


            nameInput = GameObject.Find("NameInput");
            CharCreation.singleton.worldSeedInput = GameObject.Find("SeedInput").GetComponent<TMP_InputField>();
            buildText = GameObject.Find("BuildText");
            buildText.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.8f);

            switchPromoTextCG = GameObject.Find("SwitchPromoText").GetComponent<CanvasGroup>();
            switchPromoTextCG.alpha = 1f;

            CharCreation.nameInputParentCanvasGroup.alpha = 1.0f;

            if (allUIGraphics == null)
            {
                allUIGraphics = Resources.LoadAll<Sprite>("Spritesheets/SkillIcons");
                dictUIGraphics = new Dictionary<string, Sprite>();
                for (int i = 0; i < allUIGraphics.Length; i++)
                {
                    dictUIGraphics.Add(allUIGraphics[i].name, allUIGraphics[i]);
                }
            }
            if (quickslotNumbers == null)
            {
                quickslotNumbers = Resources.LoadAll<Sprite>("Art/UI/PlayerHud/QuickslotNumbers");
            }



        }

        TitleScreenScript.CreateStage = CreationStages.TITLESCREEN;
        allUIObjects = new List<UIObject>();
        dialogObjects = new List<GameObject>();
        dialogUIObjects = new List<UIObject>();

        characterCreationBG.SetActive(false);
        buildText.SetActive(true);
        CharCreation.nameInputParentCanvasGroup.gameObject.SetActive(false);
		
		// Promo text is probably only relevant on PC, stuff like
		// "Dawn of Dragons now available"
		bool showPromotionalTextAtBottomOfScreen = true;

#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        showPromotionalTextAtBottomOfScreen = false;
#endif

		switchPromoTextCG.gameObject.SetActive(showPromotionalTextAtBottomOfScreen);

        myDialogBoxComponent.PrepareForTitleScreen();

    }
}