using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class HUDPetReadout : MonoBehaviour {

    public Monster attachedMonster;
    public Image monsterSprite;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI turnsText;
    public Image healthBar;

    public void StartConversation()
    {
        if (attachedMonster == null) return;
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;
        PetPartyUIScript.StartPetBehaviorConversationFromRef(attachedMonster);
    }

    void Start()
    {
        FontManager.LocalizeMe(nameText, TDFonts.WHITE);
        FontManager.LocalizeMe(healthText, TDFonts.WHITE);
        FontManager.LocalizeMe(turnsText, TDFonts.WHITE);
    }
	
}
