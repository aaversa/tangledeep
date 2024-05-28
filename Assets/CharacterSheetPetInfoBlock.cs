using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSheetPetInfoBlock : MonoBehaviour
{

    public List<GameObject> OrderedListOfNavigableObjects;

    [Header("Pet Picture")]
    public Image imagePetPicture;
    public Animatable animPet;

    [Header("Info fields")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtSpecies;
    public TextMeshProUGUI txtHealth;
    public TextMeshProUGUI txtWeaponPower;
    public TextMeshProUGUI txtAbilitiesBox;
    public TextMeshProUGUI txtBonusesFromHeroBox;
    public TextMeshProUGUI txtInsured;
    public TextMeshProUGUI txtLabelSkills;
    public TextMeshProUGUI txtLabelBonusesFromHero;

    [HideInInspector]
    public List<UIManagerScript.UIObject> petInfoPoints;

    private Monster myActivePet;

    // Use this for initialization
    void Start ()
    {
        FontManager.LocalizeMe(txtName, TDFonts.WHITE);
        FontManager.LocalizeMe(txtSpecies, TDFonts.WHITE);
        FontManager.LocalizeMe(txtHealth, TDFonts.WHITE);
        FontManager.LocalizeMe(txtWeaponPower, TDFonts.WHITE);
        FontManager.LocalizeMe(txtAbilitiesBox, TDFonts.WHITE);
        FontManager.LocalizeMe(txtBonusesFromHeroBox, TDFonts.WHITE);
        FontManager.LocalizeMe(txtInsured, TDFonts.WHITE);

        txtLabelSkills.text = StringManager.GetString("dialog_menuselect_intro_btn_3");
        txtLabelBonusesFromHero.text = StringManager.GetString("ui_misc_bonusesfromhero");
    }

    public void InitializeDynamicUIComponents()
    {
        petInfoPoints = new List<UIManagerScript.UIObject>();
        foreach (GameObject go in OrderedListOfNavigableObjects)
        {
            UIManagerScript.UIObject newShadowObject = new UIManagerScript.UIObject();
            newShadowObject.gameObj = go;

            //mouse over?
            EventTrigger et = go.GetComponent<EventTrigger>();
            if (et != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((eventData) => { newShadowObject.FocusOnMe(); });
                et.triggers.Add(entry);
            }

            petInfoPoints.Add(newShadowObject);
        }
    }

	// Update is called once per frame
	void Update ()
    {
        if (myActivePet != null)
        {
            imagePetPicture.sprite = myActivePet.mySpriteRenderer.sprite;
        }
    }

    public void UpdateContent()
    {
        myActivePet = GameMasterScript.heroPCActor.GetMonsterPet();
        if (myActivePet == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        imagePetPicture.sprite = myActivePet.mySpriteRenderer.sprite;

        float width = imagePetPicture.sprite.rect.width;
        float height = imagePetPicture.sprite.rect.height;

        imagePetPicture.GetComponent<RectTransform>().sizeDelta = new Vector2(width * 3f, height * 3f);

        animPet.animPlaying = myActivePet.myAnimatable.animPlaying;
        animPet.validAnimationSet = true;
        
        txtName.text = myActivePet.displayName;
        txtSpecies.text = GameMasterScript.masterMonsterList[myActivePet.actorRefName].monsterName;

        // Health
        txtHealth.text = StringManager.GetString("stat_health") + ": " + (int)myActivePet.myStats.GetCurStat(StatTypes.HEALTH) + " / " + (int)myActivePet.myStats.GetMaxStat(StatTypes.HEALTH);

        // Abilities
        // If there are too many, let's use commas instead.

        bool useCommas = myActivePet.myAbilities.GetAbilityList().Count > 4;

        string abilBuilder = "";
        for (int i = 0; i < myActivePet.myAbilities.GetAbilityList().Count; i++)
        {
            AbilityScript abil = myActivePet.myAbilities.GetAbilityList()[i];
            if (abil.passiveAbility) continue;
            abilBuilder += abil.abilityName;
            if (abil.GetCurCooldownTurns() > 0)
            {
                abilBuilder += " (" + StringManager.GetString("misc_cooldown_abbreviation") + ": " + abil.GetCurCooldownTurns() + ")";
            }
            if (i < myActivePet.myAbilities.GetAbilityList().Count - 1)
            {
                if (!useCommas)
                {
                    abilBuilder += "\n";
                }
                else
                {
                    abilBuilder += ", ";
                }
                
            }
        }

        txtAbilitiesBox.text = abilBuilder;

        // Weapon power
        float rawPower = myActivePet.myEquipment.GetWeaponPower(myActivePet.myEquipment.GetWeapon());
        int displayPower = (int)(rawPower * 10f);
        txtWeaponPower.text = StringManager.GetString("ui_equipment_weaponpower") + ": " + displayPower.ToString();

        // Insurance
        txtInsured.text = "";
        if (GameMasterScript.heroPCActor.ReadActorData("petinsurance") == 1)
        {
            txtInsured.text = StringManager.GetString("ui_insured");
        }

        // hero bonuses

        string bonusBuilder = "";

        int combatBonus = (int)(GameMasterScript.heroPCActor.advStats[(int)AdventureStats.CORRALPETBONUS] * 100f);

        bonusBuilder = StringManager.GetString("ui_pet_combat_bonus") + ": +" + combatBonus + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "\n";

        float pDodge = GameMasterScript.heroPCActor.CalculateDodge();
        int dodgeBonus = (int)(pDodge / -2f);
        bonusBuilder += StringManager.GetString("stat_dodgechance") + ": " + dodgeBonus + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "\n";

        float averageResistance = GameMasterScript.heroPCActor.CalculateAverageResistance();
        int resDisplay = (int)((1f - averageResistance) * 100f);

        bonusBuilder += StringManager.GetString("ui_pet_resist_bonus") + ": " + resDisplay + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        txtBonusesFromHeroBox.text = bonusBuilder;
            
    }
}
