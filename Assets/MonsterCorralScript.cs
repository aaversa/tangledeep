using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using TMPro;
using UnityEngine.UI;

public enum GroomingTypes { BRUSH_AND_TRIM, BATHE_AND_STYLE, MUD_BATH, HUMILIATING_OUTFIT, COUNT }

public class MonsterCorralScript : MonoBehaviour {

    public static TamedCorralMonster tcmSelected;

    public static GameObject corralGroomingInterface;
    public static bool corralGroomingInterfaceOpen;
    public static TextMeshProUGUI corralGroomingHeaderText;
    public static UIManagerScript.UIObject corralGroomingOption1;
    public static UIManagerScript.UIObject corralGroomingOption2;
    public static UIManagerScript.UIObject corralGroomingExit;

    public static GameObject corralInterface;

    public static bool corralInterfaceOpen;

    public static UIManagerScript.UIObject[] monsterListUIObjects;
    public static MonsterListButton[] monsterListButtons;

    public static Scrollbar corralScrollbarVertical;

    public static MonsterCorralScript singleton;

    public static UIManagerScript.UIObject corralExit;

    public static GameObject corralFoodInterface;
    public static bool corralFoodInterfaceOpen;
    public static TextMeshProUGUI corralFoodHeader;
    public static Scrollbar corralFoodScrollbar;

    public static UIManagerScript.UIObject corralFoodExit;
    public static UIManagerScript.UIObject corralMonsterStatsExit;
    public static UIManagerScript.UIObject corralFoodConfirm;
    public static UIManagerScript.UIObject corralFoodBackToList;

    public static UIManagerScript.UIObject[] corralFoodButtons;

    static Item selectedItem;

    public static List<Item> playerItemList;

    const int numCorralFoodButtons = 10;
    public const int MAX_MONSTERS_IN_CORRAL = 12;
    public const int MONSTERCHOW_MAX_THRESHOLD = 3;
    const int MAX_GROOM_TIMES = 10;

    public static bool initialized;

    public static int indexOfTopVisibleMonsterButton;

    static bool forceUpdateScrollbarPositionOnNextKeyPress;

    static GameObject monsterStatsInterface;
    static MonsterListButton monsterStatsMLBScript;
    static TextMeshProUGUI monsterStatsBattleText;
    static TextMeshProUGUI monsterStatsRelationshipText;
    public static bool monsterStatsInterfaceOpen;

    static List<MonsterPowerData> allPossiblePowers;
    static List<MonsterPowerData> listOfPowers;
    static List<string> possiblePowersAsStrings;

    public readonly int[] THRESHOLD_MONSTER_PET_HAPPINESS_BY_MONLEVEL = new int[]{
        3, // Mon level 1
        3,
        3,
        3, // Level 4
        3,
        3,
        4, // Level 7
        4,
        5,
        6, // Level 10
        6,
        6,
        7, // Level 13
        7,
        7,
        8,
        8 };
    public const int MIN_BREED_LEVEL = 7;
    public const float DAILY_HEAL_PERCENT = 0.25f;
    public const float CHANCE_HAPPY_RLS_MONSTER_SAVE = 0.5f;
    public const float CHANCE_RELEASED_MONSTER_WRITELETTER = 0.1f;

    void Start()
    {
        singleton = this;
        allPossiblePowers = new List<MonsterPowerData>();
        listOfPowers = new List<MonsterPowerData>();
        possiblePowersAsStrings = new List<string>();
    }

    public static void ResetAllVariablesToGameLoad()
    {
        initialized = false;
    }

    public static void Initialize(bool force = false)
    {
        if ((initialized) && (!force)) return;

        CorralBreedScript.Initialize(force);

        initialized = true;

        corralInterface = GameObject.Find("MonsterCorralInterface");

        monsterListButtons = new MonsterListButton[MAX_MONSTERS_IN_CORRAL];
        monsterListUIObjects = new UIManagerScript.UIObject[MAX_MONSTERS_IN_CORRAL];

        GameObject corralLayout = GameObject.Find("CorralVLayout");
        corralLayout.transform.localPosition = new Vector3(0f, 0f, 1f);

        if (corralScrollbarVertical == null)
        {
            GameObject scrollParent = GameObject.Find("CorralScrollView");
            corralScrollbarVertical = scrollParent.GetComponent<ScrollRect>().verticalScrollbar;
        }
        
        for (int i = 0; i < monsterListUIObjects.Length; i++)
        {
            monsterListUIObjects[i] = new UIManagerScript.UIObject();
            monsterListUIObjects[i].gameObj = GameObject.Instantiate(GameMasterScript.GetResourceByRef("MonsterCorralMonster"));
            monsterListUIObjects[i].gameObj.transform.localPosition = Vector3.zero;
            monsterListUIObjects[i].gameObj.transform.SetParent(corralLayout.transform);
            monsterListUIObjects[i].gameObj.transform.localScale = new Vector3(1f, 1f, 1f);
            monsterListUIObjects[i].gameObj.transform.localPosition = Vector3.zero;

            monsterListButtons[i] = monsterListUIObjects[i].gameObj.GetComponent<MonsterListButton>();
            monsterListButtons[i].uiObject = monsterListUIObjects[i];
            monsterListButtons[i].SetMonIndex(i);

            monsterListButtons[i].happinessHeader.text = StringManager.GetString("ui_corral_header_happiness");
            monsterListButtons[i].weightHeader.text = StringManager.GetString("ui_corral_header_weight");
            monsterListButtons[i].uniqueHeader.text = StringManager.GetString("ui_corral_header_rarity");
            monsterListButtons[i].beautyHeader.text = StringManager.GetString("ui_corral_header_beauty");
            monsterListButtons[i].groomButtonText.text = StringManager.GetString("ui_button_groom");
            monsterListButtons[i].feedButtonText.text = StringManager.GetString("ui_button_feed");
            monsterListButtons[i].infoButtonText.text = StringManager.GetString("ui_button_info");
            monsterListButtons[i].releaseButtonText.text = StringManager.GetString("ui_button_release");            

            monsterListButtons[i].groomButton = new UIManagerScript.UIObject();
            monsterListButtons[i].groomButton.gameObj = monsterListButtons[i].groomButtonText.gameObject.transform.parent.gameObject;
            monsterListButtons[i].groomButton.mySubmitFunction = singleton.OpenGroomMonsterInterface;
            monsterListButtons[i].groomButton.onSubmitValue = i;
            monsterListButtons[i].groomButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterListButtons[i].groomButton;

            monsterListButtons[i].feedButton = new UIManagerScript.UIObject();
            monsterListButtons[i].feedButton.gameObj = monsterListButtons[i].feedButtonText.gameObject.transform.parent.gameObject;
            monsterListButtons[i].feedButton.mySubmitFunction = singleton.FeedMonster;
            monsterListButtons[i].feedButton.onSubmitValue = i;
            monsterListButtons[i].feedButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterListButtons[i].feedButton;

            monsterListButtons[i].infoButton = new UIManagerScript.UIObject();
            monsterListButtons[i].infoButton.gameObj = monsterListButtons[i].infoButtonText.gameObject.transform.parent.gameObject;
            monsterListButtons[i].infoButton.mySubmitFunction = singleton.OpenMonsterStatsInterface;
            monsterListButtons[i].infoButton.onSubmitValue = i;
            monsterListButtons[i].infoButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterListButtons[i].infoButton;

            monsterListButtons[i].petButton = new UIManagerScript.UIObject();
            monsterListButtons[i].petButton.gameObj = monsterListButtons[i].petButtonText.gameObject.transform.parent.gameObject;
            monsterListButtons[i].petButton.mySubmitFunction = singleton.PutOrGetMonsterInCorral;
            monsterListButtons[i].petButton.onSubmitValue = i;
            monsterListButtons[i].petButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterListButtons[i].petButton;

            monsterListButtons[i].releaseButton = new UIManagerScript.UIObject();
            monsterListButtons[i].releaseButton.gameObj = monsterListButtons[i].releaseButtonText.gameObject.transform.parent.gameObject;
            monsterListButtons[i].releaseButton.mySubmitFunction = singleton.ReleaseMonster;
            monsterListButtons[i].releaseButton.onSubmitValue = i;
            monsterListButtons[i].releaseButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterListButtons[i].releaseButton;
        }

        // Now set neighbors

        for (int i = 0; i < monsterListUIObjects.Length; i++)
        {
            monsterListButtons[i].groomButton.neighbors[(int)Directions.WEST] = monsterListButtons[i].releaseButton;
            monsterListButtons[i].groomButton.neighbors[(int)Directions.EAST] = monsterListButtons[i].feedButton;

            monsterListButtons[i].feedButton.neighbors[(int)Directions.WEST] = monsterListButtons[i].groomButton;
            monsterListButtons[i].feedButton.neighbors[(int)Directions.EAST] = monsterListButtons[i].infoButton;

            monsterListButtons[i].infoButton.neighbors[(int)Directions.WEST] = monsterListButtons[i].feedButton;
            monsterListButtons[i].infoButton.neighbors[(int)Directions.EAST] = monsterListButtons[i].petButton;

            monsterListButtons[i].petButton.neighbors[(int)Directions.WEST] = monsterListButtons[i].infoButton;
            monsterListButtons[i].petButton.neighbors[(int)Directions.EAST] = monsterListButtons[i].releaseButton;

            monsterListButtons[i].releaseButton.neighbors[(int)Directions.WEST] = monsterListButtons[i].petButton;
            monsterListButtons[i].releaseButton.neighbors[(int)Directions.EAST] = monsterListButtons[i].groomButton;

            if (i == 0)
            {
                monsterListButtons[i].groomButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i].groomButton;
                monsterListButtons[i].feedButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i].feedButton;
                monsterListButtons[i].infoButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i].infoButton;
                monsterListButtons[i].petButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i].petButton;
                monsterListButtons[i].releaseButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i].releaseButton;
            }
            else
            {
                monsterListButtons[i].groomButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i - 1].groomButton;
                monsterListButtons[i].feedButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i - 1].feedButton;
                monsterListButtons[i].infoButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i - 1].infoButton;
                monsterListButtons[i].petButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i - 1].petButton;
                monsterListButtons[i].releaseButton.neighbors[(int)Directions.NORTH] = monsterListButtons[i - 1].releaseButton;
            }

            if (i == monsterListUIObjects.Length-1)
            {
                monsterListButtons[i].groomButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i].groomButton;
                monsterListButtons[i].feedButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i].feedButton;
                monsterListButtons[i].infoButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i].infoButton;
                monsterListButtons[i].petButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i].petButton;
                monsterListButtons[i].releaseButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i].releaseButton;
            }
            else
            {
                monsterListButtons[i].groomButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i+1].groomButton;
                monsterListButtons[i].feedButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i+1].feedButton;
                monsterListButtons[i].infoButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i + 1].infoButton;
                monsterListButtons[i].petButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i + 1].petButton;
                monsterListButtons[i].releaseButton.neighbors[(int)Directions.SOUTH] = monsterListButtons[i+1].releaseButton;
            }
        }

        corralExit = new UIManagerScript.UIObject();
        corralExit.gameObj = GameObject.Find("CorralExit");
        corralExit.mySubmitFunction = singleton.ExitCorral;

        CloseCorralInterface();

        corralFoodInterface = GameObject.Find("MonsterCorralFoodInterface");
        corralFoodHeader = GameObject.Find("Corral Food Header").GetComponent<TextMeshProUGUI>();
        corralFoodHeader.text = StringManager.GetString("ui_corral_feedmonster_header");

        corralFoodExit = new UIManagerScript.UIObject();
        corralFoodExit.gameObj = GameObject.Find("Corral Food Exit");
        corralFoodExit.mySubmitFunction = singleton.CloseCorralFoodInterface;

        corralFoodConfirm = new UIManagerScript.UIObject();
        corralFoodConfirm.gameObj = GameObject.Find("Corral Food Confirm");
        corralFoodConfirm.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("ui_corral_feedmonster_confirmbutton");
        corralFoodConfirm.mySubmitFunction = singleton.ConfirmFoodForMonster;

        corralFoodBackToList = new UIManagerScript.UIObject();
        corralFoodBackToList.gameObj = GameObject.Find("Corral Food Back");
        corralFoodBackToList.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("ui_button_back");
        corralFoodBackToList.mySubmitFunction = singleton.BackToMonsterList;

        FontManager.LocalizeMe(corralFoodHeader, TDFonts.WHITE);

        FontManager.LocalizeMe(corralFoodConfirm.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(corralFoodBackToList.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);

        corralFoodConfirm.neighbors[(int)Directions.EAST] = corralFoodBackToList;
        corralFoodConfirm.neighbors[(int)Directions.WEST] = corralFoodBackToList;

        corralFoodBackToList.neighbors[(int)Directions.EAST] = corralFoodConfirm;
        corralFoodBackToList.neighbors[(int)Directions.WEST] = corralFoodConfirm;

        UIManagerScript.corralFoodScrollbar = GameObject.Find("CorralFoodScrollbar");

        corralFoodButtons = new UIManagerScript.UIObject[numCorralFoodButtons];

        for (int i = 0; i < numCorralFoodButtons; i++)
        {
            corralFoodButtons[i] = new UIManagerScript.UIObject();
            string search = "Corral Food Button " + (i + 1);
            corralFoodButtons[i].gameObj = GameObject.Find(search);
            corralFoodButtons[i].subObjectImage = GameObject.Find(search + " Sprite").GetComponent<Image>();
            corralFoodButtons[i].mySubmitFunction = singleton.SelectFood;
            corralFoodButtons[i].onSubmitValue = i;
            FontManager.LocalizeMe(corralFoodButtons[i].gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        }

        corralFoodButtons[0].neighbors[(int)Directions.NORTH] = corralFoodButtons[9];
        corralFoodButtons[0].directionalActions[(int)Directions.NORTH] = corralFoodButtons[0].GoToBottomOfCorralFoodList;

        corralFoodButtons[0].neighbors[(int)Directions.SOUTH] = corralFoodButtons[1];

        corralFoodButtons[1].neighbors[(int)Directions.NORTH] = corralFoodButtons[0];
        corralFoodButtons[1].neighbors[(int)Directions.SOUTH] = corralFoodButtons[2];

        corralFoodButtons[2].neighbors[(int)Directions.NORTH] = corralFoodButtons[1];
        corralFoodButtons[2].neighbors[(int)Directions.SOUTH] = corralFoodButtons[3];

        corralFoodButtons[3].neighbors[(int)Directions.NORTH] = corralFoodButtons[2];
        corralFoodButtons[3].neighbors[(int)Directions.SOUTH] = corralFoodButtons[4];

        corralFoodButtons[4].neighbors[(int)Directions.NORTH] = corralFoodButtons[3];
        corralFoodButtons[4].neighbors[(int)Directions.SOUTH] = corralFoodButtons[5];

        corralFoodButtons[5].neighbors[(int)Directions.NORTH] = corralFoodButtons[4];
        corralFoodButtons[5].neighbors[(int)Directions.SOUTH] = corralFoodButtons[6];

        corralFoodButtons[6].neighbors[(int)Directions.NORTH] = corralFoodButtons[5];
        corralFoodButtons[6].neighbors[(int)Directions.SOUTH] = corralFoodButtons[7];

        corralFoodButtons[7].neighbors[(int)Directions.NORTH] = corralFoodButtons[6];
        corralFoodButtons[7].neighbors[(int)Directions.SOUTH] = corralFoodButtons[8];

        corralFoodButtons[8].neighbors[(int)Directions.NORTH] = corralFoodButtons[7];
        corralFoodButtons[8].neighbors[(int)Directions.SOUTH] = corralFoodButtons[9];

        corralFoodButtons[9].neighbors[(int)Directions.NORTH] = corralFoodButtons[8];
        corralFoodButtons[9].neighbors[(int)Directions.SOUTH] = corralFoodButtons[0];

        corralFoodButtons[9].directionalActions[(int)Directions.SOUTH] = corralFoodButtons[9].TryScrollPool;
        corralFoodButtons[9].directionalValues[(int)Directions.SOUTH] = 1;

        corralFoodButtons[0].directionalActions[(int)Directions.NORTH] = corralFoodButtons[0].TryScrollPool;
        corralFoodButtons[0].directionalValues[(int)Directions.NORTH] = -1;

        playerItemList = new List<Item>();

        corralFoodScrollbar = GameObject.Find("CorralFoodScrollbar").GetComponent<Scrollbar>();

        corralGroomingInterface = GameObject.Find("MonsterCorralGrooming");
        corralGroomingHeaderText = GameObject.Find("Corral Groom Header").GetComponent<TextMeshProUGUI>();

        corralGroomingOption1 = new UIManagerScript.UIObject();
        corralGroomingOption1.gameObj = GameObject.Find("Corral Groom 1");
        corralGroomingOption1.mySubmitFunction = singleton.GroomMonster;
        corralGroomingOption1.onSubmitValue = (int)GroomingTypes.BRUSH_AND_TRIM;        

        corralGroomingOption2 = new UIManagerScript.UIObject();
        corralGroomingOption2.gameObj = GameObject.Find("Corral Groom 2");
        corralGroomingOption2.mySubmitFunction = singleton.GroomMonster;
        corralGroomingOption2.onSubmitValue = (int)GroomingTypes.BATHE_AND_STYLE;

        corralGroomingExit = new UIManagerScript.UIObject();
        corralGroomingExit.gameObj = GameObject.Find("Corral Groom Exit");
        corralGroomingExit.mySubmitFunction = singleton.CloseCorralGroomingInterface;
        corralGroomingExit.onSubmitValue = 0;
        corralGroomingExit.gameObj.GetComponentInChildren<TextMeshProUGUI>().SetText(StringManager.GetString("misc_button_exit_normalcase").ToUpperInvariant());

        FontManager.LocalizeMe(corralGroomingHeaderText, TDFonts.WHITE);
        FontManager.LocalizeMe(corralGroomingOption1.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(corralGroomingOption2.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(corralGroomingExit.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);

        corralGroomingOption1.neighbors[(int)Directions.NORTH] = corralGroomingExit;
        corralGroomingOption1.neighbors[(int)Directions.SOUTH] = corralGroomingOption2;

        corralGroomingOption2.neighbors[(int)Directions.NORTH] = corralGroomingOption1;
        corralGroomingOption2.neighbors[(int)Directions.SOUTH] = corralGroomingExit;

        corralGroomingExit.neighbors[(int)Directions.NORTH] = corralGroomingOption2;
        corralGroomingExit.neighbors[(int)Directions.SOUTH] = corralGroomingOption1;

        singleton.CloseCorralGroomingInterface(0);

        singleton.CloseCorralFoodInterface(0);

        monsterStatsInterface = GameObject.Find("CorralMonsterStatsView");
        monsterStatsMLBScript = monsterStatsInterface.GetComponent<MonsterListButton>();

        monsterStatsMLBScript.groomButton = new UIManagerScript.UIObject();
        monsterStatsMLBScript.groomButton.gameObj = monsterStatsMLBScript.groomButtonText.gameObject.transform.parent.gameObject;
        monsterStatsMLBScript.groomButton.mySubmitFunction = singleton.OpenGroomMonsterInterface;
        monsterStatsMLBScript.groomButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterStatsMLBScript.groomButton;

        monsterStatsMLBScript.feedButton = new UIManagerScript.UIObject();
        monsterStatsMLBScript.feedButton.gameObj = monsterStatsMLBScript.feedButtonText.gameObject.transform.parent.gameObject;
        monsterStatsMLBScript.feedButton.mySubmitFunction = singleton.FeedMonster;
        monsterStatsMLBScript.feedButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterStatsMLBScript.feedButton;

        monsterStatsMLBScript.backButton = new UIManagerScript.UIObject();
        monsterStatsMLBScript.backButton.gameObj = monsterStatsMLBScript.backButtonText.gameObject.transform.parent.gameObject;
        monsterStatsMLBScript.backButton.mySubmitFunction = singleton.CloseMonsterStatsInterfaceAndOpenCorralInterface;
        monsterStatsMLBScript.backButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterStatsMLBScript.backButton;

        monsterStatsMLBScript.petButton = new UIManagerScript.UIObject();
        monsterStatsMLBScript.petButton.gameObj = monsterStatsMLBScript.petButtonText.gameObject.transform.parent.gameObject;
        monsterStatsMLBScript.petButton.mySubmitFunction = singleton.PutOrGetMonsterInCorral;
        monsterStatsMLBScript.petButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterStatsMLBScript.petButton;

        monsterStatsMLBScript.releaseButton = new UIManagerScript.UIObject();
        monsterStatsMLBScript.releaseButton.gameObj = monsterStatsMLBScript.releaseButtonText.gameObject.transform.parent.gameObject;
        monsterStatsMLBScript.releaseButton.mySubmitFunction = singleton.ReleaseMonster;
        monsterStatsMLBScript.releaseButton.gameObj.GetComponent<GenericButtonForUIObject>().myUIobject = monsterStatsMLBScript.releaseButton;

        monsterStatsMLBScript.backButton.neighbors[(int)Directions.WEST] = monsterStatsMLBScript.releaseButton;
        monsterStatsMLBScript.backButton.neighbors[(int)Directions.EAST] = monsterStatsMLBScript.groomButton;

        monsterStatsMLBScript.groomButton.neighbors[(int)Directions.WEST] = monsterStatsMLBScript.backButton;
        monsterStatsMLBScript.groomButton.neighbors[(int)Directions.EAST] = monsterStatsMLBScript.feedButton;

        monsterStatsMLBScript.feedButton.neighbors[(int)Directions.WEST] = monsterStatsMLBScript.groomButton;
        monsterStatsMLBScript.feedButton.neighbors[(int)Directions.EAST] = monsterStatsMLBScript.petButton;

        monsterStatsMLBScript.petButton.neighbors[(int)Directions.WEST] = monsterStatsMLBScript.feedButton;
        monsterStatsMLBScript.petButton.neighbors[(int)Directions.EAST] = monsterStatsMLBScript.releaseButton;

        monsterStatsMLBScript.releaseButton.neighbors[(int)Directions.WEST] = monsterStatsMLBScript.petButton;
        monsterStatsMLBScript.releaseButton.neighbors[(int)Directions.EAST] = monsterStatsMLBScript.backButton;

        corralMonsterStatsExit = new UIManagerScript.UIObject();
        corralMonsterStatsExit.gameObj = GameObject.Find("CorralMonsterStatsExit");
        corralMonsterStatsExit.mySubmitFunction = singleton.CloseMonsterStatsInterface;

        monsterStatsBattleText = GameObject.Find("MonsterStatsBattleStatsText").GetComponent<TextMeshProUGUI>();
        monsterStatsRelationshipText = GameObject.Find("MonsterStatsRelationshipText").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(monsterStatsBattleText, TDFonts.WHITE);
        FontManager.LocalizeMe(monsterStatsRelationshipText, TDFonts.WHITE);

        singleton.CloseMonsterStatsInterface(0);

        corralFoodButtons[0].directionalActions[(int)Directions.NORTH] = corralFoodButtons[0].GoToBottomOfCorralFoodList;

    }
        
    public static int GetPetInsuranceCost()
    {
        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
        if (pet == null) return 0;

        int cost = GameMasterScript.heroPCActor.myStats.GetLevel() * 25;
        return cost;
    }

    public void Update()
    {
        if (corralInterfaceOpen)
        {
            UIManagerScript.singletonUIMS.RefocusCursor();
        }
    }

    // Returns JP cost required to teach the given pet the given ability
    // Used by the new monster trainer NPC
    public static int CalculateSkillJPCost(Monster pet, AbilityScript template)
    {
        int level = pet.myStats.GetLevel();
        int cost = 100 + (level * 100); // base cost
        float multiplier = template.power;
        cost = (int)(cost * multiplier);
        //Debug.Log("Cost for " + pet.displayName + " to learn " + template.abilityName + " is " + cost);
        return cost;
    }

    public static int CalculateForgetSkillJPCost(Monster pet, AbilityScript template)
    {
        int level = pet.myStats.GetLevel();
        int cost = 100 + (level * 50);
        return cost;
    }

    // Run this if the player died, but pet did not die.
    // return pet to corral, returnpet, rescue pet
    public static void ReturnPlayerPetToCorralAfterDeath()
    {
        int petID = GameMasterScript.heroPCActor.GetMonsterPetID();

        if (Debug.isDebugBuild) Debug.Log(Time.time + " Corral pet ID " + petID + " is being returned to the corral, probably after death.");

        if (petID >= 0)
        {
            Actor act = GameMasterScript.gmsSingleton.TryLinkActorFromDict(petID);
            // Don't put pet back in corral twice.
            if (act != null)// && act.GetActorMap() != MapMasterScript.singletonMMS.townMap2)
            {
                Monster mon = act as Monster;                
                GameMasterScript.heroPCActor.RemoveSummon(mon);

                if (Debug.isDebugBuild) Debug.Log("Destroyed pet actor is " + mon.actorRefName + " " + mon.tamedMonsterStuff.sharedBankID + " " + mon.actorUniqueID + " " + mon.displayName);

                mon.SetActorData("death_processed", 1);

                mon.isInCorral = true;

                if (!MetaProgressScript.localTamedMonstersForThisSlot.Contains(mon.tamedMonsterStuff))
                {
                    if (Debug.isDebugBuild) Debug.Log("The defeated pet " + mon.PrintCorralDebug() + " is not in master corral list, adding it.");
                    MetaProgressScript.AddPetToLocalSlotCorralList(mon.tamedMonsterStuff);
                    //MetaProgressScript.localTamedMonstersForThisSlot.Add(mon.tamedMonsterStuff);
                }

                if (act.GetActorMap().floor == MapMasterScript.TOWN2_MAP_FLOOR)
                {

                }
                else
                {
                    mon.GetActorMap().RemoveActorFromMap(mon);
                    mon.SetActorMap(MapMasterScript.singletonMMS.townMap2);
                    mon.dungeonFloor = MapMasterScript.TOWN2_MAP_FLOOR;                    
                    MapMasterScript.singletonMMS.townMap2.AddActorToMap(mon);
                    if (mon.objectSet && mon.GetObject().activeSelf)
                    {
                        GameMasterScript.ReturnActorObjectToStack(mon, mon.GetObject());
                    }
                }
                

                mon.GetActorMap().RemoveActorFromLocation(mon.positionAtStartOfTurn, mon);

                mon.myStats.HealToFull();

                GameMasterScript.RemoveActorFromDeadQueue(mon);
            }
            else if (act == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Pet could not be found?! ID: " + GameMasterScript.heroPCActor.GetMonsterPetID());
            }            
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Wait, I can't find a player pet.");
        }
        

        GameMasterScript.heroPCActor.ResetPetData();
    }

    public void CloseMonsterStatsInterfaceAndOpenCorralInterface(int dummy)
    {
        CloseMonsterStatsInterface(0);
        OpenCorralInterface();
    }

    public void PutOrGetMonsterInCorral(int monIndex)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (GameMasterScript.heroPCActor.HasMonsterPet())
        {
            // error
            GameLogScript.LogWriteStringRef("log_error_alreadyhavepet");
            UIManagerScript.PlayCursorSound("Error");
            return;
        }
       
        if (monIndex >= MetaProgressScript.localTamedMonstersForThisSlot.Count)
        {
            Debug.Log("Index of put/get exceeded max monsters?");
            return;
        }

        Monster monPetToTake = MetaProgressScript.localTamedMonstersForThisSlot[monIndex].monsterObject;

        bool monsterPossibleAsPet = MetaProgressScript.localTamedMonstersForThisSlot[monIndex].CanMonsterBePet();
        bool angryAtPlayer = MetaProgressScript.localTamedMonstersForThisSlot[monIndex].IsAngryAtPlayer();

        if (!monsterPossibleAsPet || angryAtPlayer)
        {
            CloseCorralInterface();
            CloseMonsterStatsInterface(0);
            StringManager.SetTag(0, monPetToTake.displayName);

            if (!monsterPossibleAsPet)
            {
                int checkHappinessNumber = monPetToTake.tamedMonsterStuff.CalculateHappinessThresholdForPet();
                string happinessString = monPetToTake.tamedMonsterStuff.GetHappinessString_Internal(checkHappinessNumber);
                StringManager.SetTag(1, happinessString);
                UIManagerScript.StartConversationByRef("corral_unhappy_pet", DialogType.STANDARD, null);
            }
            else if (angryAtPlayer)
            {
                UIManagerScript.StartConversationByRef("corral_angry_pet", DialogType.STANDARD, null);
            }

            return;
        }

        MetaProgressScript.localTamedMonstersForThisSlot.Remove(monPetToTake.tamedMonsterStuff);

        if (Debug.isDebugBuild) Debug.Log("Removed from shared corral: " + monPetToTake.PrintCorralDebug());

        MapTileData newSpaceForMon = MapMasterScript.GetTile(new Vector2(11f, 3f));
        monPetToTake.isInCorral = false;

        if (!MapMasterScript.activeMap.IsActorObjectInMap(monPetToTake))
        {
            MapMasterScript.activeMap.AddActorToMap(monPetToTake);
        }
        
        MapMasterScript.singletonMMS.MoveAndProcessActor(monPetToTake.GetPos(), newSpaceForMon.pos, monPetToTake);
        monPetToTake.myMovable.SetPosition(newSpaceForMon.pos);
        monPetToTake.anchor = GameMasterScript.heroPCActor;
        monPetToTake.anchorID = GameMasterScript.heroPCActor.actorUniqueID;
        monPetToTake.anchorRange = 3;
        monPetToTake.destroyed = false;
        monPetToTake.deathProcessed = false;
        monPetToTake.surpressTraits = false;
        monPetToTake.SetState(BehaviorState.NEUTRAL);
        monPetToTake.myTarget = null;

        GameMasterScript.RemoveActorFromDeadQueue(monPetToTake);

        monPetToTake.myStats.SetStat(StatTypes.CHARGETIME, 99f, StatDataTypes.ALL, true);

        GameMasterScript.heroPCActor.AddSummon(monPetToTake);

        if (Debug.isDebugBuild) Debug.Log("We're taking pet: " + monPetToTake.actorUniqueID + ", shared corral id is " + monPetToTake.tamedMonsterStuff.sharedBankID);

        GameMasterScript.heroPCActor.SetMonsterPetID(monPetToTake.actorUniqueID);
        GameMasterScript.heroPCActor.SetMonsterPetSharedID(monPetToTake.tamedMonsterStuff.sharedBankID);
        if (monPetToTake.actorRefName == "mon_swamptoad")
        {
            GameMasterScript.heroPCActor.SetActorData("pet_bog_frog", 1);
        }

        monPetToTake.ApplyHeroPetAttributes();

        StringManager.SetTag(0, monPetToTake.displayName);
        GameLogScript.LogWriteStringRef("ui_corral_buddyup");

        UIManagerScript.PlayCursorSound("CookingSuccess");

        UIManagerScript.UpdatePetInfo();
        CloseCorralInterface();
        CloseMonsterStatsInterface(0);
    }

    public void CloseCorralGroomingInterface(int dummy)
    {
        //if (!GameMasterScript.gameLoadSequenceCompleted) return;

        if (corralGroomingInterface == null) return;
        if (corralGroomingInterfaceOpen)
        {
            UIManagerScript.PlayCursorSound("UITock");
        }

        corralGroomingInterface.SetActive(false);
        corralGroomingInterfaceOpen = false;
        UIManagerScript.HideDialogMenuCursor();
    }

    public void GroomMonster(int groomOptionIndex)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        int cost = tcmSelected.GetGroomingCost((GroomingTypes)groomOptionIndex);

        if (cost > GameMasterScript.heroPCActor.GetMoney())
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        if (tcmSelected.timesGroomed >= MAX_GROOM_TIMES)
        {
            UIManagerScript.PlayCursorSound("Error");
            StringManager.SetTag(0, tcmSelected.monsterObject.displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("error_corral_maxgroom"), GameMasterScript.heroPCActor);
            return;
        }

        GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);

        CloseCorralGroomingInterface(0);
        tcmSelected.DoMonsterGrooming((GroomingTypes)groomOptionIndex);
    }

    public static void AdjustScrollbarAfterCursorScroll(Directions moveDirection)
    {
        if (!corralInterfaceOpen) return;
        // Assume three monsters are displayed on screen at once.

        int buttonIndex = UIManagerScript.uiObjectFocus.onSubmitValue;

        if (MetaProgressScript.localTamedMonstersForThisSlot.Count <= 3) return;

        int maxItems = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //int maxItems = MetaProgressScript.localTamedMonstersForThisSlot.Count;
        if (maxItems > MAX_MONSTERS_IN_CORRAL) maxItems = MAX_MONSTERS_IN_CORRAL;

        float valueDeltaPerMonster = 1f / (maxItems - 3);

        int numSteps = maxItems - 3 + 1;
        int numExtraValues = maxItems - 3;
        float newScrollbarValue = corralScrollbarVertical.value;

        if (forceUpdateScrollbarPositionOnNextKeyPress)
        {
            newScrollbarValue = 1f - (indexOfTopVisibleMonsterButton * valueDeltaPerMonster);
            forceUpdateScrollbarPositionOnNextKeyPress = false;
        }

        if (moveDirection == Directions.NORTH)
        {
            if (buttonIndex == maxItems-1)
            {
                newScrollbarValue = 0f;
                indexOfTopVisibleMonsterButton = numSteps - 1;
            }
            else
            {
                if (buttonIndex < indexOfTopVisibleMonsterButton)
                {
                    indexOfTopVisibleMonsterButton--;
                    newScrollbarValue = 1f - (indexOfTopVisibleMonsterButton * valueDeltaPerMonster);
                }                
            }
        }
        else
        {
            if (buttonIndex == 0)
            {
                // Wrapped to top
                indexOfTopVisibleMonsterButton = 0;
                newScrollbarValue = 1f;
            }
            else
            {
                if (buttonIndex > indexOfTopVisibleMonsterButton + 2)
                {
                    indexOfTopVisibleMonsterButton++;
                    newScrollbarValue = 1f - (indexOfTopVisibleMonsterButton * valueDeltaPerMonster);
                }
                
            }
        }

        //Debug.Log("New scroll value: " + newScrollbarValue + " Our index: " + buttonIndex + " num steps: " + numSteps + " max items: " + maxItems + " top button? " + indexOfTopVisibleMonsterButton + " value delta " + valueDeltaPerMonster);

        corralScrollbarVertical.value = newScrollbarValue;        
    }

    public void HoverGroom(int index)
    {
        if (!GameMasterScript.actualGameStarted) return;
        if (index == -1)
        {
            return;
        }

        switch(index)
        {
            case 0:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralGroomingExit);
                break;
            case 1:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralGroomingOption1);
                break;
            case 2:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralGroomingOption2);
                break;
        }
    }

    public void HoverFood(int index)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        if (!corralFoodInterfaceOpen)
        {
            Debug.Log("Beep1");
            return;
        }

        if (index == -1)
        {
            return;
        }

        if (selectedItem != null)
        {
            return;
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodButtons[index]);
        }
    }

    public void SelectFood(int index)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (selectedItem != null) return;
        int indexOfSelectedItem = index + UIManagerScript.listArrayIndexOffset;        
        if (indexOfSelectedItem >= playerItemList.Count)
        {
            Debug.Log("Index " + index + " is out of monster corral food range.");
            return;
        }

        selectedItem = playerItemList[indexOfSelectedItem];
        corralFoodButtons[index].gameObj.GetComponent<Image>().color = Color.white;
        UIManagerScript.UpdateMonsterCorralFoodList();
        UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodConfirm);
    }

    public static void CancelPressed()
    {
        if (corralFoodInterfaceOpen)
        {
            if (selectedItem != null)
            {
                ClearItemHighlights();
                selectedItem = null;
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodButtons[0]);
                UIManagerScript.UpdateMonsterCorralFoodList();
            }
            else
            {
                singleton.CloseCorralFoodInterface(0);
            }
        }

    }

    static void ClearItemHighlights()
    {
        for (int i = 0; i < corralFoodButtons.Length; i++)
        {
            corralFoodButtons[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
        }
    }

    public void BackToMonsterList(int dummy)
    {
        CloseCorralFoodInterface(0);
        OpenCorralInterface();
    }

    public void ConfirmFoodForMonster(int dummy)
    {
        if (selectedItem == null)
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }
        CloseCorralFoodInterface(0);
        tcmSelected.FeedMonster(selectedItem);

        StartCoroutine(WaitThenReopenFoodInterface(tcmSelected, 1.0f));
    }

    IEnumerator WaitThenReopenFoodInterface(TamedCorralMonster tcm, float time)
    {
        GameMasterScript.SetAnimationPlaying(true);
        yield return new WaitForSeconds(time);
        GameMasterScript.SetAnimationPlaying(false);
        OpenCorralFoodInterface(tcm);
    }

    public void ExitCorral(int dummy)
    {
        CloseCorralInterface();
    }

    public void GetMonsterInfo(int monIndex)
    {
        CloseCorralInterface();
        //Debug.Log("Showing monster stats for " + monIndex);
        OpenMonsterStatsInterface(monIndex);
    }

    public void FeedMonster(int monIndex)
    {
        CloseCorralInterface();
        if (monIndex >= MetaProgressScript.localTamedMonstersForThisSlot.Count)
        {
            Debug.Log(monIndex + " exceeds max tamed monsters for feeding.");
            return;
        }
        TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[monIndex];        
        OpenCorralFoodInterface(tcm);
    }

    public void ReleaseMonster(int monIndex)
    {
        CloseCorralInterface();
        CloseMonsterStatsInterface(0);

        if (monIndex >= MetaProgressScript.localTamedMonstersForThisSlot.Count)
        {
            Debug.Log("Cannot release index " + monIndex + ", exceeds max monsters.");
            return;
        }

        StringManager.ClearTags();
        StringManager.SetTag(0, MetaProgressScript.localTamedMonstersForThisSlot[monIndex].monsterObject.displayName);

        string confirmation = StringManager.GetString("confirm_corral_releasemonster");
        UIManagerScript.lastDialogDBRSelected = DialogButtonResponse.RELEASEMONSTER;
        UIManagerScript.lastDialogActionRefSelected = "r" + monIndex;
        UIManagerScript.ToggleConfirmationDialog(confirmation, false, null);

    }


    public void OpenGroomMonsterInterface(int monIndex)
    {
        //GameMasterScript.gmsSingleton.SetTempGameData("groommonster", monIndex);
        CloseCorralInterface();
        singleton.CloseMonsterStatsInterface(0);

        UIManagerScript.PlayCursorSound("Select");

        tcmSelected = MetaProgressScript.localTamedMonstersForThisSlot[monIndex];

        StringManager.SetTag(0, tcmSelected.monsterObject.displayName);
        StringManager.SetTag(1, GameMasterScript.heroPCActor.GetMoney() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD));

        int cost = tcmSelected.GetGroomingCost(GroomingTypes.BRUSH_AND_TRIM);
        //GameMasterScript.gmsSingleton.SetTempStringData("cost1", cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD));

        StringManager.SetTag(2, cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD));

        /* cost = tcmSelected.GetGroomingCost(GroomingTypes.BATHE_AND_STYLE);
        GameMasterScript.gmsSingleton.SetTempStringData("cost2", cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD));        

        cost = tcmSelected.GetGroomingCost(GroomingTypes.MUD_BATH);
        GameMasterScript.gmsSingleton.SetTempStringData("cost3", cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD));

        cost = tcmSelected.GetGroomingCost(GroomingTypes.HUMILIATING_OUTFIT);
        GameMasterScript.gmsSingleton.SetTempStringData("cost4", cost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD)); */

        StringManager.SetTag(6, (MAX_GROOM_TIMES - tcmSelected.timesGroomed).ToString());
        StringManager.SetTag(7, MAX_GROOM_TIMES.ToString());

        UIManagerScript.StartConversationByRef("petgrooming", DialogType.STANDARD, null);

        /* 

        corralGroomingInterface.SetActive(true);
        corralGroomingInterfaceOpen = true;

        corralGroomingHeaderText.text = StringManager.GetString("ui_corral_groommonster_header") +" <color=yellow>" + GameMasterScript.heroPCActor.GetMoney() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>";

        string option1text = StringManager.GetString("ui_corral_groommonster_service1");
        option1text += " (" + tcmSelected.GetGroomingCost(GroomingTypes.BRUSH_AND_TRIM) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";        

        string option2text = StringManager.GetString("ui_corral_groommonster_service2");
        option2text += " (" + tcmSelected.GetGroomingCost(GroomingTypes.BATHE_AND_STYLE) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + ")";

        TextMeshProUGUI groom1TMP = corralGroomingOption1.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI groom2TMP = corralGroomingOption2.gameObj.GetComponentInChildren<TextMeshProUGUI>();

        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            groom1TMP.characterSpacing = 0;
            groom2TMP.characterSpacing = 0;
            corralGroomingOption1.gameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 50f);
            corralGroomingOption2.gameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 50f);
        }

        groom1TMP.text = option1text;
        groom2TMP.text = option2text;

        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(corralGroomingInterface.transform);
        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.ChangeUIFocusAndAlignCursor(corralGroomingOption1); */
    }

    public void HoverCursor(int button)
    {
        if (!GameMasterScript.actualGameStarted) return;
         switch (button)
        {
            case 0:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralExit);
                break;
            case 1:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralMonsterStatsExit);
                break;
            case 2:
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodExit);
                break;
        } 
    }

    public static void CloseCorralInterface()
    {
        corralInterface.SetActive(false);
        corralInterfaceOpen = false;
        GuideMode.OnFullScreenUIClosed();
        UIManagerScript.HideDialogMenuCursor();
        UIManagerScript.singletonUIMS.DisableCursor();
    }

    public void CloseCorralFoodInterface(int dummy)
    {
        corralFoodInterface.SetActive(false);
        corralFoodInterfaceOpen = false;
        UIManagerScript.HideDialogMenuCursor();
        UIManagerScript.singletonUIMS.DisableCursor();
    }

    public static void OpenCorralFoodInterface(TamedCorralMonster whichTCM)
    {
        UIManagerScript.PlayCursorSound("Select");
        singleton.CloseMonsterStatsInterface(0);
        selectedItem = null; // Last food item fed to monster.
        tcmSelected = whichTCM;
        ClearItemHighlights();
        corralFoodInterface.SetActive(true);
        corralFoodInterfaceOpen = true;
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(corralFoodInterface.transform);
        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.ShowDialogMenuCursor();

        StringManager.ClearTags();
        StringManager.SetTag(0, tcmSelected.monsterObject.displayName);

        corralFoodHeader.text = StringManager.GetString("ui_corral_feedmonster_header");

        playerItemList.Clear();

        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable con = itm as Consumable;
                if (con.isFood)
                {
                    playerItemList.Add(con);
                }
            }
        }

        UIManagerScript.UpdateMonsterCorralFoodList();

        UIManagerScript.UpdateScrollbarPosition();

        if (playerItemList.Count == 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodExit);
        }
        else
        {
            // Try to put the cursor on the last item we fed to the monster, too.
            int lastFedItemID = GameMasterScript.gmsSingleton.ReadTempGameData("feedmonsteritem");
            if (lastFedItemID >= 0)
            {
                // The UpdateMonsterCorralFoodList function generates the index of the actual button we need to align to
                int buttonIndex = GameMasterScript.gmsSingleton.ReadTempGameData("feedmonsterbuttonindex");
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodButtons[buttonIndex]);
            }
            else
            {
                UIManagerScript.ChangeUIFocusAndAlignCursor(corralFoodButtons[0]);
            }            
        }
    }

    public void MouseTouchedScrollbar()
    {
        // TODO: Calculate this based on rects / bounds, don't just guess at it
        indexOfTopVisibleMonsterButton = 0;
        forceUpdateScrollbarPositionOnNextKeyPress = true;
    }

    public static void OpenCorralInterface()
    {
        UIManagerScript.CloseDialogBox();

        // Update the image.
        if (MetaProgressScript.localTamedMonstersForThisSlot.Count == 0)
        {
            Debug.Log("Cannot open corral interface with no tamed monsters.");
            return;
        }

        UIManagerScript.PlayCursorSound("Select");

        corralInterface.SetActive(true);
        corralInterfaceOpen = true;

        GuideMode.OnFullScreenUIOpened();

        indexOfTopVisibleMonsterButton = 0;        

        bool anyEggs = false;

        for (int i = 0; i < MAX_MONSTERS_IN_CORRAL; i++)
        {
            monsterListButtons[i].gameObject.SetActive(false);
            monsterListButtons[i].releaseButton.enabled = false;
            monsterListButtons[i].groomButton.enabled = false;
            monsterListButtons[i].infoButton.enabled = false;
            monsterListButtons[i].petButton.enabled = false;
            monsterListButtons[i].feedButton.enabled = false;
        }

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[i];

            //if (Debug.isDebugBuild) Debug.Log("Iterating over " + i + " with max of " + maxMonsterCount + ", " + tcm.monsterObject.PrintCorralDebug());

            if (tcm.monsterObject == null)
            {
                Debug.Log("Error: " + tcm.monsterID + " null object.");
                GuideMode.OnFullScreenUIClosed();
                corralInterface.SetActive(false);
                corralInterfaceOpen = false;
            }            

            monsterListButtons[i].gameObject.SetActive(true);
            monsterListButtons[i].releaseButton.enabled = true;
            monsterListButtons[i].infoButton.enabled = true;
            monsterListButtons[i].petButton.enabled = true;
            monsterListButtons[i].groomButton.enabled = true;
            monsterListButtons[i].feedButton.enabled = true;

            Monster monsterInMap = tcm.monsterObject;

            monsterListButtons[i].SetMonIndex(i);
            monsterListButtons[i].PopulateButtonContents(monsterInMap);
        }

        corralScrollbarVertical.value = 1;

        if (anyEggs)
        {
            UIManagerScript.PlayCursorSound("ShamanHeal");
        }

        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(corralInterface.transform);
        UIManagerScript.ChangeUIFocusAndAlignCursor(monsterListButtons[0].feedButton);
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.ChangeUIFocusAndAlignCursor(monsterListButtons[0].feedButton);
        GameObject.Find("CorralVLayout").GetComponent<VerticalLayoutGroup>().CalculateLayoutInputVertical();
        GameObject.Find("CorralScrollView").GetComponent<ScrollRect>().Rebuild(CanvasUpdate.LatePreRender);        
        corralScrollbarVertical.value = 0.95f;
        corralScrollbarVertical.value = 1f;
        corralScrollbarVertical.Rebuild(CanvasUpdate.PostLayout);
        singleton.StartCoroutine(singleton.WaitThenMaxScrollbar(0.05f));
    }

    public IEnumerator WaitThenMaxScrollbar(float time)
    {
        yield return new WaitForSeconds(time); // trying to get the corral ui to always scroll to top
        // layout groups are messing it up
        corralScrollbarVertical.value = 1f;
    }

    public static Monster BreedMonsters(TamedCorralMonster monster1, TamedCorralMonster monster2)
    {
        Monster newlyBredMonster = MonsterManagerScript.CreateMonster("mon_customplayerpet", false, false, false, 0f, true);

        newlyBredMonster.bufferedFaction = Faction.PLAYER;
        newlyBredMonster.actorfaction = Faction.PLAYER;

        MonsterTemplateData monster1MTD = MonsterManagerScript.GetTemplateByRef(monster1.refName);
        MonsterTemplateData monster2MTD = MonsterManagerScript.GetTemplateByRef(monster2.refName);

        string monFamily = "";

        // Appearance
        if (UnityEngine.Random.Range(0,2) == 0)
        {
            newlyBredMonster.prefab = monster1.monsterObject.prefab;
            monFamily = monster1.family;
            newlyBredMonster.monFamily = monster1.family;
        }
        else
        {
            newlyBredMonster.prefab = monster2.monsterObject.prefab;
            monFamily = monster2.family;
            newlyBredMonster.monFamily = monster2.family;
        }

        // Challenge value
        float newCV = (monster1.monsterObject.challengeValue + monster2.monsterObject.challengeValue) / 2f;
        newlyBredMonster.challengeValue = newCV;

        int level = (monster1.monsterObject.myStats.GetLevel() + monster2.monsterObject.myStats.GetLevel()) / 2;
        
        if (level > 15)
        {
            level = 15;
        }

        newlyBredMonster.myStats.SetLevel(level);

        // Weapons and Armor
        string weaponRef = "";
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            weaponRef = monster1MTD.weaponID;            
        }
        else
        {
            weaponRef = monster2MTD.weaponID;
        }

        Weapon newWeapon = LootGeneratorScript.CreateItemFromTemplateRef(weaponRef, newlyBredMonster.challengeValue, 0f, false) as Weapon;
        newlyBredMonster.myInventory.AddItemRemoveFromPrevCollection(newWeapon, false);
        newlyBredMonster.myEquipment.Equip(newWeapon, SND.SILENT, 0, false, false);

        Weapon parent1Weapon = monster1.monsterObject.myEquipment.GetWeapon();
        Weapon parent2Weapon = monster2.monsterObject.myEquipment.GetWeapon();

        monster1.monsterObject.SaveWeaponSwingDataToDict();
        monster2.monsterObject.SaveWeaponSwingDataToDict();

        newWeapon.power = (parent1Weapon.power + parent2Weapon.power) / 2f;

        float maxJPSpend = GameMasterScript.gmsSingleton.ReadTempFloatData("mon_breed_maxjp");
        float jpSpent = GameMasterScript.gmsSingleton.ReadTempFloatData("mon_breed_jpspent");

        GameMasterScript.heroPCActor.AddJP(-1 * jpSpent);

        float percentGrowthByJP = 0;

        if (jpSpent > 0 && maxJPSpend > 0)
        {
            percentGrowthByJP = 1f + ((jpSpent / maxJPSpend) * .08f);
        }
        
        //Debug.Log("Max jp: " + maxJPSpend + " Jp spent: " + jpSpent + " P Growth JP: " + percentGrowthByJP);

        if (percentGrowthByJP >= 1.08f)
        {
            percentGrowthByJP = 1.08f;
        }


        string armorRef = "";
        if (UnityEngine.Random.Range(0, 2) == 0 && !String.IsNullOrEmpty(monster1MTD.armorID))
        {
            armorRef = monster1MTD.armorID;
        }
        else if (!String.IsNullOrEmpty(monster2MTD.armorID))
        {
            armorRef = monster2MTD.armorID;
        }

        if (armorRef != "")
        {
            Armor newArmor = LootGeneratorScript.CreateItemFromTemplateRef(armorRef, newlyBredMonster.challengeValue, 0f, false) as Armor;
            newlyBredMonster.myInventory.AddItemRemoveFromPrevCollection(newArmor, false);
            newlyBredMonster.myEquipment.Equip(newArmor, SND.SILENT, 0, false, false);
        }

        newlyBredMonster.myInventory.ClearInventory();

        // All monster abilities: active and passive
        listOfPowers.Clear();
        allPossiblePowers.Clear();
        possiblePowersAsStrings.Clear();

        int parent1PossiblePowers = 0;
        int parent2PossiblePowers = 0;

        // This was reading from templates at first.
        foreach(MonsterPowerData mpd in monster1.monsterObject.monsterPowers)
        {
            bool skip = false;
            if (mpd.abilityRef.CheckAbilityTag(AbilityTags.TEACHABLE_MONSTERTECH) || mpd.abilityRef.CheckAbilityTag(AbilityTags.CANNOT_INHERIT))
            {
                continue;
            }
            foreach(EffectScript eff in mpd.abilityRef.listEffectScripts)
            {
                if (eff.effectType == EffectType.DESTROYACTOR)
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;
            if (possiblePowersAsStrings.Contains(mpd.abilityRef.refName)) continue;
            parent1PossiblePowers++;
            allPossiblePowers.Add(mpd);
            possiblePowersAsStrings.Add(mpd.abilityRef.refName);
        }
        foreach (MonsterPowerData mpd in monster2.monsterObject.monsterPowers)
        {
            bool skip = false;
            if (mpd.abilityRef.CheckAbilityTag(AbilityTags.TEACHABLE_MONSTERTECH) || mpd.abilityRef.CheckAbilityTag(AbilityTags.CANNOT_INHERIT))
            {
                continue;
            }
            foreach (EffectScript eff in mpd.abilityRef.listEffectScripts)
            {
                if (eff.effectType == EffectType.DESTROYACTOR)
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;
            if (possiblePowersAsStrings.Contains(mpd.abilityRef.refName)) continue;
            parent2PossiblePowers++;
            allPossiblePowers.Add(mpd);
            possiblePowersAsStrings.Add(mpd.abilityRef.refName);
        }

        int maxPossiblePowers = allPossiblePowers.Count;

        int minimumPowers = 0;
        if (parent1PossiblePowers < parent2PossiblePowers) minimumPowers = parent1PossiblePowers;
        else minimumPowers = parent2PossiblePowers;

        if (minimumPowers < 1) minimumPowers = 1;

        if (maxPossiblePowers > 0)
        {
            int numPowers = UnityEngine.Random.Range(minimumPowers, maxPossiblePowers + 1);
            allPossiblePowers.Shuffle();

            for (int i = 0; i < numPowers; i++)
            {
                if (allPossiblePowers.Count > 0)
                {
                    MonsterPowerData selected = allPossiblePowers[0];
                    bool hasAbility = false;
                    foreach (MonsterPowerData existingMPD in listOfPowers)
                    {
                        if (existingMPD.abilityRef.refName == selected.abilityRef.refName)
                        {
                            hasAbility = true;
                            break;
                        }
                    }
                    if (hasAbility)
                    {
                        continue;
                    }
                    listOfPowers.Add(selected);
                    allPossiblePowers.Remove(selected);
                }
            }

            foreach (MonsterPowerData mpd in listOfPowers)
            {
                AbilityScript newAbil = new AbilityScript();
                AbilityScript template = mpd.abilityRef;
                AbilityScript.CopyFromTemplate(newAbil, template);
                newlyBredMonster.myAbilities.AddNewAbility(newAbil, true);
                MonsterPowerData newMPD = new MonsterPowerData();
                newMPD.CopyFromTemplate(mpd, newAbil);
                newlyBredMonster.monsterPowers.Add(newMPD);

                newlyBredMonster.OnMonsterPowerAdded(newMPD, newAbil);
            }
        }

        List<string> loveFoods = new List<string>();
        List<string> hateFoods = new List<string>();

        if (loveFoods.Count > 0 && hateFoods.Count > 0)
        {
            int numLoves = (monster1.loveFoods.Count + monster2.loveFoods.Count) / 2;
            int numHates = (monster1.hateFoods.Count + monster2.hateFoods.Count) / 2;

            for (int i = 0; i < numLoves; i++)
            {
                string tryLoveFood = "";
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    tryLoveFood = monster1.loveFoods[UnityEngine.Random.Range(0, monster1.loveFoods.Count)];
                }
                else
                {
                    tryLoveFood = monster2.loveFoods[UnityEngine.Random.Range(0, monster2.loveFoods.Count)];
                }
                if (loveFoods.Contains(tryLoveFood))
                {
                    continue;
                }
                loveFoods.Add(tryLoveFood);
            }

            for (int i = 0; i < numHates; i++)
            {
                string tryHateFood = "";
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    tryHateFood = monster1.hateFoods[UnityEngine.Random.Range(0, monster1.hateFoods.Count)];
                }
                else
                {
                    tryHateFood = monster2.hateFoods[UnityEngine.Random.Range(0, monster2.hateFoods.Count)];
                }
                if ((loveFoods.Contains(tryHateFood)) || (hateFoods.Contains(tryHateFood)))
                {
                    continue;
                }
                hateFoods.Add(tryHateFood);
            }
        }

        // Stats
        //float health = (monster1.monsterObject.myStats.GetStat(StatTypes.HEALTH,StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX)) / 2f;

        // update parent base health to make sure it's accurate
        monster1.baseMonsterHealth = monster1.monsterObject.myStats.GetMaxStat(StatTypes.HEALTH);
        monster2.baseMonsterHealth = monster2.monsterObject.myStats.GetMaxStat(StatTypes.HEALTH);

        float health = (monster1.baseMonsterHealth + monster2.baseMonsterHealth) / 2f;

        float strength = (monster1.monsterObject.myStats.GetStat(StatTypes.STRENGTH, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.STRENGTH, StatDataTypes.MAX)) / 2f;
        float spirit = (monster1.monsterObject.myStats.GetStat(StatTypes.SPIRIT, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.SPIRIT, StatDataTypes.MAX)) / 2f;
        float discipline = (monster1.monsterObject.myStats.GetStat(StatTypes.DISCIPLINE, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.DISCIPLINE, StatDataTypes.MAX)) / 2f;
        float swiftness = (monster1.monsterObject.myStats.GetStat(StatTypes.SWIFTNESS, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.SWIFTNESS, StatDataTypes.MAX)) / 2f;
        float guile = (monster1.monsterObject.myStats.GetStat(StatTypes.GUILE, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.GUILE, StatDataTypes.MAX)) / 2f;
        float accuracy = (monster1.monsterObject.myStats.GetStat(StatTypes.ACCURACY, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.ACCURACY, StatDataTypes.MAX)) / 2f;
        float chargeTime = (monster1.monsterObject.myStats.GetStat(StatTypes.CHARGETIME, StatDataTypes.MAX) + monster2.monsterObject.myStats.GetStat(StatTypes.CHARGETIME, StatDataTypes.MAX)) / 2f;

        newlyBredMonster.myStats.SetStat(StatTypes.HEALTH, health, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.STRENGTH, strength, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.SWIFTNESS, swiftness, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.SPIRIT, spirit, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.DISCIPLINE, discipline, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.GUILE, guile, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.ACCURACY, accuracy, StatDataTypes.ALL, false);
        newlyBredMonster.myStats.SetStat(StatTypes.CHARGETIME, chargeTime, StatDataTypes.ALL, false);

        // Scale with player level? Necessary?

        float curAccuracy = newlyBredMonster.myStats.GetStat(StatTypes.ACCURACY, StatDataTypes.MAX);
        float curChargeTime = newlyBredMonster.myStats.GetStat(StatTypes.CHARGETIME, StatDataTypes.MAX);

        float baseWeaponGrowth = 1.035f;
        float baseCoreStatsGrowth = 0.05f;
        float baseHealthGrowth = 0.035f;


        // Rare parents = stronger monster
        Rarity parentRarity = Rarity.COMMON;
        if ((int)monster1.GetMonsterRarity() > (int)monster2.GetMonsterRarity())
        {
            parentRarity = monster1.GetMonsterRarity();
        }
        else
        {
            parentRarity = monster2.GetMonsterRarity();
        }
        
        switch (parentRarity)
        {
            case Rarity.UNCOMMON:
                baseWeaponGrowth = 1.05f;
                baseCoreStatsGrowth = 0.06f;
                baseHealthGrowth = 0.05f;
                curAccuracy += 2f;
                curChargeTime += 2f;
                break;
            case Rarity.MAGICAL:
                baseWeaponGrowth = 1.06f;
                baseCoreStatsGrowth = 0.08f;
                baseHealthGrowth = 0.07f;
                curAccuracy += 4f;
                curChargeTime += 4f;
                break;
            case Rarity.ANCIENT:
                baseWeaponGrowth = 1.07f;
                baseCoreStatsGrowth = 0.1f;
                baseHealthGrowth = 0.1f;

                curAccuracy += 6f;
                curChargeTime += 6f;
                break;
            case Rarity.ARTIFACT:
                baseWeaponGrowth = 1.085f;
                baseCoreStatsGrowth = 0.12f;
                baseHealthGrowth = 0.14f;

                curAccuracy += 8f;
                curChargeTime += 8f;
                break;
        }
        

        if (percentGrowthByJP > 1f)
        {
            baseWeaponGrowth *= percentGrowthByJP;
            baseCoreStatsGrowth *= percentGrowthByJP;
            baseHealthGrowth *= percentGrowthByJP;
        }
        else
        {
            baseWeaponGrowth = 1f;
            baseCoreStatsGrowth = 0f;
            baseHealthGrowth = 0f;
        }


        newWeapon.power *= baseWeaponGrowth;
        if (newWeapon.power >= Weapon.MAX_WEAPON_POWER)
        {
            newWeapon.power = Weapon.MAX_WEAPON_POWER;
        }

        InheritWeaponSwingFXFromParents(newlyBredMonster, newWeapon, monster1.monsterObject, monster2.monsterObject);

        newlyBredMonster.SaveWeaponSwingDataToDict();

        if (baseCoreStatsGrowth > 0f)
        {
            newlyBredMonster.myStats.BoostCoreStatsByPercent(baseCoreStatsGrowth);
        }
        if (baseHealthGrowth > 0f)
        {
            //newlyBredMonster.myStats.BoostStatByPercent(StatTypes.HEALTH, baseHealthGrowth);
            float origHealth = health;
            health += (health * baseHealthGrowth);
            //Debug.Log("Base health is " + health + " with growth of " + baseHealthGrowth + " for result of " + health);
        }


        
        if (curChargeTime >= 99f)
        {
            curChargeTime = 99f;
        }

        

        // Now the tamed monster stats.

        TamedCorralMonster tamedStats = new TamedCorralMonster();
        newlyBredMonster.tamedMonsterStuff = tamedStats;
        
        tamedStats.monsterObject = newlyBredMonster;
        tamedStats.monsterID = newlyBredMonster.actorUniqueID;
        tamedStats.beauty = (monster1.beauty + monster2.beauty) / 2;
        tamedStats.weight = (monster1.weight + monster2.weight) / 2;

        if (monster1.unique > monster2.unique)
        {
            tamedStats.unique = monster1.unique;
        }
        else
        {
            tamedStats.unique = monster2.unique;
        }

        tamedStats.family = monFamily;
        tamedStats.StartRelationship(monster1, true);
        tamedStats.StartRelationship(monster2, true);
        tamedStats.AdjustRelationship(monster1, 5, true);
        tamedStats.AdjustRelationship(monster2, 5, true);
        tamedStats.refName = newlyBredMonster.actorRefName;        
        tamedStats.happiness = ((monster1.happiness + monster2.happiness) / 2) + 1;
        
        if (tamedStats.happiness >= TamedCorralMonster.MAX_HAPPINESS)
        {
            tamedStats.happiness = TamedCorralMonster.MAX_HAPPINESS;
        }

        tamedStats.parent1Name = monster1.monsterObject.displayName;
        tamedStats.parent2Name = monster2.monsterObject.displayName;

        tamedStats.inheritedMaxWeight = (monster1MTD.weight + monster2MTD.weight) * 2;
        tamedStats.loveFoods = loveFoods;
        tamedStats.hateFoods = hateFoods;

        float maxHealth = BalanceData.MAX_CORRAL_PET_HEALTH;
        if (GameStartData.NewGamePlus == 1) maxHealth = BalanceData.MAX_CORRAL_PET_HEALTH_NG1;
        if (GameStartData.NewGamePlus >= 2) maxHealth = BalanceData.MAX_CORRAL_PET_HEALTH_NG2;

        if (health > maxHealth)
        {
            health = maxHealth;
            newlyBredMonster.myStats.SetStat(StatTypes.HEALTH, health, StatDataTypes.ALL, true);
        }

        tamedStats.baseMonsterHealth = health;

        tamedStats.sharedBankID = SharedCorral.GetUniqueSharedPetID();

        newlyBredMonster.myStats.ValidateStat(StatTypes.HEALTH);
        newlyBredMonster.myStats.SetStat(StatTypes.ACCURACY, curAccuracy, StatDataTypes.ALL, true);
        newlyBredMonster.myStats.SetStat(StatTypes.CHARGETIME, curChargeTime, StatDataTypes.ALL, true);
        newlyBredMonster.SetBattleDataDirty();

        Debug.Log("New monster has been created! Actor ID " + newlyBredMonster.actorUniqueID + " and ref " + newlyBredMonster.actorRefName + " " + tamedStats.sharedBankID);


        return newlyBredMonster;        
    }

    static void InheritWeaponSwingFXFromParents(Monster newMonster, Weapon newWeapon, Monster parent1, Monster parent2)
    {
        List<string> stringsToSearch = new List<string>() { "weaponimpactfx", "weaponswingfx" };

        for (int i = 0; i < stringsToSearch.Count; i++)
        {
            string searchRef = stringsToSearch[i];
            if (i == 1 && newWeapon.range == 1) // melee weapons don't need swing FX.
            {
                return;
            }
            string p1fx = parent1.ReadActorDataString(searchRef);
            string p2fx = parent2.ReadActorDataString(searchRef);

            if (string.IsNullOrEmpty(p1fx) && string.IsNullOrEmpty(p2fx))
            {
                return;
            }
            // At least one FX to inherit exists.

            string fxToUse = "";

            if (string.IsNullOrEmpty(p1fx)) // Parent1 has nothing? Use parent 2
            {
                fxToUse = p2fx;
            }
            else if (string.IsNullOrEmpty(p1fx)) // Parent 1 has something, p2 doesn't? Use p1
            {
                fxToUse = p1fx;
            }
            else //  Both parents have something, so we'll pick at random
            {
                fxToUse = UnityEngine.Random.Range(0, 2) == 0 ? p1fx : p2fx;
            }

            if (i == 0)
            {
                // impact fx
                newWeapon.impactEffect = fxToUse;
            }
            else
            {
                // swing fx
                newWeapon.swingEffect = fxToUse;
            }
        }
    }

    public void OpenMonsterStatsInterface(int monIndex)
    {
        UIManagerScript.PlayCursorSound("Select");
        CloseCorralInterface();
        monsterStatsInterface.SetActive(true);
        monsterStatsInterfaceOpen = true;

        monsterStatsMLBScript.SetMonIndex(monIndex);
        monsterStatsMLBScript.groomButton.onSubmitValue = monIndex;
        monsterStatsMLBScript.releaseButton.onSubmitValue = monIndex;
        monsterStatsMLBScript.feedButton.onSubmitValue = monIndex;
        monsterStatsMLBScript.petButton.onSubmitValue = monIndex;

        if (monIndex >= MetaProgressScript.localTamedMonstersForThisSlot.Count)
        {
            Debug.Log("Cannot check stats for monster exceeding number of mons in corral");
            CloseMonsterStatsInterface(0);
            return;
        }

        Monster monToView = MetaProgressScript.localTamedMonstersForThisSlot[monIndex].monsterObject;

        monsterStatsMLBScript.PopulateButtonContents(monToView);

        // Battle and relationships


        monsterStatsBattleText.text = monToView.tamedMonsterStuff.GetBattlePowerStats();

        string relationsText = "";
        bool first = true;

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[i];
            if (tcm == monToView.tamedMonsterStuff) continue;
            StringManager.SetTag(0, tcm.monsterObject.displayName);
            if (first)
            {
                first = false;                
            }
            else
            {
                relationsText += "\n";
            }
            string extraEditorStuff = "";
#if UNITY_EDITOR
            extraEditorStuff = " " + monToView.tamedMonsterStuff.TryGetRelationshipAmount(tcm);
#endif
            relationsText += StringManager.GetString("ui_corral_feelings") + "</color> " + monToView.tamedMonsterStuff.GetRelationshipString(tcm) + extraEditorStuff;
        }

        monsterStatsRelationshipText.text = relationsText;

        GuideMode.OnFullScreenUIOpened();

        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(monsterStatsInterface.transform);
        UIManagerScript.ChangeUIFocusAndAlignCursor(monsterStatsMLBScript.feedButton);
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.ChangeUIFocusAndAlignCursor(monsterStatsMLBScript.feedButton);
    }

    public void CloseMonsterStatsInterface(int dummy)
    {
        monsterStatsInterface.SetActive(false);
        monsterStatsInterfaceOpen = false;
    }

    public static void CloseAllInterfaces()
    {
        corralInterface.SetActive(false);
        corralFoodInterface.SetActive(false);
        monsterStatsInterface.SetActive(false);
        monsterStatsInterfaceOpen = false;
        corralInterfaceOpen = false;
        corralFoodInterfaceOpen = false;
        GuideMode.OnFullScreenUIClosed();
    }

    public static void NameMonsterThenAddToCorralForFirstTime(Monster addedMon, bool newlyBredMonster, float waitTime = 0f)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("monsterbeingnamedforcorral", addedMon.actorUniqueID);

        addedMon.recentlyNamedMonster = true;

        if (newlyBredMonster)
        {            
            if (waitTime > 0f)
            {
                GameMasterScript.SetAnimationPlaying(true);
                Conversation c = GameMasterScript.FindConversation("corral_namenewmonster");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(c, DialogType.KEYSTORY, null, waitTime));
            }
            else
            {
                UIManagerScript.StartConversationByRef("corral_namenewmonster", DialogType.KEYSTORY, null);
            }            
        }
        else
        {
            UIManagerScript.StartConversationByRef("corral_namemonster", DialogType.KEYSTORY, null);
            addedMon.myStats.ForciblyRemoveStatus("enemy_quest_target");
        }                
    }


}


public class ReleasedMonster
{
    public string displayName;
    public string teachAbilityRef;
    public string firstOwner;
    public int dayReleased;
    public MonsterPowerData mpd;

    public ReleasedMonster()
    {
        displayName = "";
        teachAbilityRef = "";
        firstOwner = "";
        dayReleased = 0;
    }

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("rlsmon");
        writer.WriteElementString("name", displayName);
        writer.WriteElementString("teachabil", teachAbilityRef);
        writer.WriteElementString("firstowner", firstOwner);
        writer.WriteElementString("dayrls", dayReleased.ToString());

        writer.WriteElementString("minrange", mpd.minRange.ToString());
        writer.WriteElementString("maxrange", mpd.maxRange.ToString());
        writer.WriteElementString("usestate", ((int)mpd.useState).ToString());
        writer.WriteElementString("usechance", ((int)(mpd.chanceToUse*100f)).ToString());
        writer.WriteElementString("usethresh", ((int)(mpd.healthThreshold * 100f)).ToString());
        if (mpd.useWithNoTarget)
        {
            writer.WriteElementString("notarget", "1");
        }
        else
        {
            writer.WriteElementString("notarget", "0");
        }

        writer.WriteEndElement();
    }

    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        mpd = new MonsterPowerData();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                continue;
            }
            switch (reader.Name)
            {
                case "name":
                    displayName = reader.ReadElementContentAsString();
                    break;
                case "teachabil":
                    teachAbilityRef = reader.ReadElementContentAsString();
                    break;
                case "firstowner":
                    firstOwner = reader.ReadElementContentAsString();
                    break;
                case "dayrls":
                    dayReleased = reader.ReadElementContentAsInt();
                    break;
                case "minrange":
                    mpd.minRange = reader.ReadElementContentAsInt();
                    break;
                case "maxrange":
                    mpd.maxRange = reader.ReadElementContentAsInt();
                    break;
                case "usestate":
                    mpd.useState = (BehaviorState)reader.ReadElementContentAsInt();
                    break;
                case "usechance":
                    mpd.chanceToUse = (reader.ReadElementContentAsInt() / 100f);
                    break;
                case "usethresh":
                    mpd.healthThreshold = (reader.ReadElementContentAsInt() / 100f);
                    break;
                case "notarget":
                    mpd.useWithNoTarget = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }


}
