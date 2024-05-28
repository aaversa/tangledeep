using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[System.Serializable]
public class MonsterListButton : MonoBehaviour
{
    int indexOfButtonInList;
    public Image myImage;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI happinessHeader;
    public TextMeshProUGUI happinessValue;
    public TextMeshProUGUI weightHeader;
    public TextMeshProUGUI weightValue;
    public TextMeshProUGUI uniqueHeader;
    public TextMeshProUGUI uniqueValue;
    public TextMeshProUGUI beautyHeader;
    public TextMeshProUGUI beautyValue;

    public TextMeshProUGUI backButtonText;
    public TextMeshProUGUI groomButtonText;
    public TextMeshProUGUI feedButtonText;
    public TextMeshProUGUI infoButtonText;
    public TextMeshProUGUI releaseButtonText;
    public TextMeshProUGUI petButtonText;

    public UIManagerScript.UIObject uiObject;

    public UIManagerScript.UIObject backButton;
    public UIManagerScript.UIObject groomButton;
    public UIManagerScript.UIObject feedButton;
    public UIManagerScript.UIObject infoButton;
    public UIManagerScript.UIObject petButton;
    public UIManagerScript.UIObject releaseButton;

    public Image happyFill;

    void Start()
    {
        happinessHeader.text = StringManager.GetString("ui_corral_header_happiness");
        weightHeader.text = StringManager.GetString("ui_corral_header_weight");
        uniqueHeader.text = StringManager.GetString("ui_corral_header_rarity");
        beautyHeader.text = StringManager.GetString("ui_corral_header_beauty");

        FontManager.LocalizeMe(happinessHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(happinessValue, TDFonts.WHITE);
        FontManager.LocalizeMe(weightHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(weightValue, TDFonts.WHITE);
        FontManager.LocalizeMe(uniqueHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(uniqueValue, TDFonts.WHITE);
        FontManager.LocalizeMe(beautyHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(beautyValue, TDFonts.WHITE);

        if (backButtonText != null) FontManager.LocalizeMe(backButtonText, TDFonts.BLACK);
        if (feedButtonText != null) FontManager.LocalizeMe(feedButtonText, TDFonts.BLACK);
        if (groomButtonText != null) FontManager.LocalizeMe(groomButtonText, TDFonts.BLACK);
        if (infoButtonText != null) FontManager.LocalizeMe(infoButtonText, TDFonts.BLACK);
        if (releaseButtonText != null) FontManager.LocalizeMe(releaseButtonText, TDFonts.BLACK);
        if (petButtonText != null) FontManager.LocalizeMe(petButtonText, TDFonts.BLACK);

        if (descText != null) FontManager.LocalizeMe(descText, TDFonts.WHITE);
    }

    public void SetMonIndex(int id)
    {
        indexOfButtonInList = id;
    }

    public void PopulateButtonContents(Monster petMon)
    {
        if (petMon == null)
        {
            Debug.Log("Can't populate null monster button.");
            return;
        }
        if (petMon.tamedMonsterStuff == null)
        {
            Debug.Log(petMon.actorUniqueID + " " + petMon.actorRefName + " " + petMon.displayName + " has no TCM?!");
            return;
        }

        if (petMon.myAnimatable == null)
        {
            Debug.Log(petMon.actorRefName + " has no animatable?");
            MapMasterScript.singletonMMS.SpawnMonster(petMon);
        }

        if (petMon.myAnimatable.GetAnim() == null)
        {
            petMon.myAnimatable.SetAnim("Idle");
        }

        Sprite monIdleSprite = petMon.myAnimatable.GetAnim().mySprites[0].mySprite;

        myImage.sprite = monIdleSprite;

        // Scale to 2x actual sprite size
        float width = monIdleSprite.bounds.size.x;
        float height = monIdleSprite.bounds.size.y;

        myImage.SetNativeSize();
        myImage.rectTransform.sizeDelta = new Vector3(myImage.rectTransform.sizeDelta.x * 2f, myImage.rectTransform.sizeDelta.y * 2f, 1f);

        happinessValue.text = petMon.tamedMonsterStuff.GetHappinessString();
        weightValue.text = petMon.tamedMonsterStuff.GetWeightString();
        beautyValue.text = petMon.tamedMonsterStuff.GetBeautyString();
        uniqueValue.text = petMon.tamedMonsterStuff.GetRarityString();

        groomButtonText.text = StringManager.GetString("ui_button_groom");
        feedButtonText.text = StringManager.GetString("ui_button_feed");
        if (infoButtonText != null)
        {
            infoButtonText.text = StringManager.GetString("ui_button_info");
        }
        if (backButtonText != null)
        {
            backButtonText.text = StringManager.GetString("ui_button_back");
        }

        // TODO: Conditional for whether player already has this monster as a pet.
        petButtonText.text = StringManager.GetString("ui_button_getmonsterpet");
        releaseButtonText.text = StringManager.GetString("ui_button_release");

        StringManager.SetTag(0, petMon.displayName);
        StringManager.SetTag(1, petMon.myStats.GetLevel().ToString());
        StringManager.SetTag(2, (int)petMon.myStats.GetCurStat(StatTypes.HEALTH) + "/" + (int)petMon.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX));
        StringManager.SetTag(3, Monster.GetFamilyName(petMon.tamedMonsterStuff.family));

        string extraText2 = "";
        // Pets no longer die, they just get upset.
        int daysSinceDied = petMon.ReadActorData("day_at_unisured_death");
        if (daysSinceDied > 0) // died on day 250?
        {
            daysSinceDied = MetaProgressScript.totalDaysPassed - daysSinceDied;
            if (daysSinceDied >= 0 && daysSinceDied < GameMasterScript.MONSTER_PET_ANGRY_THRESHOLD_DAYS)
            {
                int daysLeft = GameMasterScript.MONSTER_PET_ANGRY_THRESHOLD_DAYS - daysSinceDied;
                StringManager.SetTag(4, daysLeft.ToString());
                extraText2 = " (" + StringManager.GetString("ui_corral_monster_angry") + ")";
            }
            else if (daysSinceDied >= 3)
            {
                petMon.RemoveActorData("day_at_unisured_death");
            }
        }

        string headerText = StringManager.GetString("corral_monster_descline1");

        string catchName = petMon.tamedMonsterStuff.catcherName;

        // This shouldn't be null, but it might be from old data.
        if (String.IsNullOrEmpty(catchName))
        {
            catchName = GameMasterScript.heroPCActor.displayName;
        }

        string extraText = petMon.tamedMonsterStuff.CheckTimePassed();

        /* if (!string.IsNullOrEmpty(petMon.tamedMonsterStuff.parent1Name))
        {
            extraText = " " + StringManager.GetString("misc_parents") + petMon.tamedMonsterStuff.parent1Name + "," + petMon.tamedMonsterStuff.parent2Name + " " + extraText;
        } */

        int dispXP = petMon.myStats.GetXP();
        int dispXPToNext = petMon.myStats.GetXPToNextLevel();
        if (dispXP > dispXPToNext)
        {
            dispXP = dispXPToNext;
        }
        headerText += "\n" + StringManager.GetString("corral_mon_subheader") + "<color=yellow>" + catchName + "</color>. " + dispXP + "/" + dispXPToNext + " " + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.XP) + extraText2 + "\n" + extraText;

        descText.text = headerText;

        int foodMeter = petMon.tamedMonsterStuff.foodMeter;
        int maxMeter = petMon.tamedMonsterStuff.CalculateFoodThresholdForPet();

        float fill = (float)foodMeter / (float)maxMeter;

        happyFill.fillAmount = fill;
    }

    public void GroomMonster()
    {
        MonsterCorralScript.singleton.OpenGroomMonsterInterface(indexOfButtonInList);
    }
    public void FeedMonster()
    {
        MonsterCorralScript.singleton.FeedMonster(indexOfButtonInList);
    }

    public void ReleaseMonster()
    {
        MonsterCorralScript.singleton.ReleaseMonster(indexOfButtonInList);
    }

    public void GetMonsterInfo()
    {
        MonsterCorralScript.singleton.OpenMonsterStatsInterface(indexOfButtonInList);
    }

    public void PutOrGetMonsterPet()
    {
        MonsterCorralScript.singleton.PutOrGetMonsterInCorral(indexOfButtonInList);
    }

    public void BackToList()
    {
        MonsterCorralScript.singleton.CloseMonsterStatsInterfaceAndOpenCorralInterface(0);
    }

    public void OnScroll( BaseEventData eventData)
    {
        var ped = (PointerEventData) eventData;

        if (MonsterCorralScript.corralScrollbarVertical != null)
        {
            MonsterCorralScript.corralScrollbarVertical.value += 0.02f * (ped.scrollDelta.y);
        }

    }
}
