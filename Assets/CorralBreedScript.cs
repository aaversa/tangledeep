using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CorralBreedScript : MonoBehaviour {

    public static CorralBreedScript singleton;
    public static bool initialized;

    public static bool corralBreedInterfaceOpen;
    static GameObject corralBreedInterface;
    static UIManagerScript.UIObject confirmBreed;
    static UIManagerScript.UIObject exitBreed;

    static UIManagerScript.UIObject breedButton1;
    static UIManagerScript.UIObject breedButton2;
    static UIManagerScript.UIObject breedButton3;
    static UIManagerScript.UIObject breedButton4;
    static UIManagerScript.UIObject breedButton5;
    static UIManagerScript.UIObject breedButton6;
    static UIManagerScript.UIObject breedButton7;
    static UIManagerScript.UIObject breedButton8;
    static UIManagerScript.UIObject breedButton9;
    static UIManagerScript.UIObject breedButton10;
    static UIManagerScript.UIObject breedButton11;
    static UIManagerScript.UIObject breedButton12;

    static TextMeshProUGUI breedHeader;
    static TextMeshProUGUI confirmText;
    static TextMeshProUGUI friendshipText;
    static GameObject friendshipContainer;    

    static UIManagerScript.UIObject[] breedButtonList;

    static MonsterListButton hoverBreedMonsterStats = new MonsterListButton();
    static TextMeshProUGUI hoverMonsterStatsBattleText;

    static List<Monster> monstersSelected = new List<Monster>();

    // Use this for initialization
    void Start () {        
        singleton = this;
	}

    public static void ResetAllVariablesToGameLoad()
    {
        initialized = false;
    }
    public static void Initialize(bool force = false)
    {
        if (initialized && !force) return;

        initialized = true;

        corralBreedInterface = GameObject.Find("MonsterCorralBreedInterface");

        confirmBreed = new UIManagerScript.UIObject();
        confirmBreed.gameObj = GameObject.Find("Corral Breed Confirm");
        confirmBreed.mySubmitFunction = singleton.ConfirmMonstersForBreeding;
        confirmBreed.myOnSelectAction = singleton.HoverToBreedButton;
        confirmBreed.enabled = false;
        confirmBreed.gameObj.SetActive(false);

        FontManager.LocalizeMe(confirmBreed.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);

        exitBreed = new UIManagerScript.UIObject();
        exitBreed.gameObj = GameObject.Find("Corral Breed Exit");
        exitBreed.mySubmitFunction = singleton.ExitBreedingInterface;

        breedButtonList = new UIManagerScript.UIObject[MonsterCorralScript.MAX_MONSTERS_IN_CORRAL];

        breedButton1 = new UIManagerScript.UIObject();
        breedButton1.gameObj = GameObject.Find("Corral Breed Button 1");
        breedButton1.subObjectTMPro = breedButton1.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton1.subObjectImage = GameObject.Find("Corral Breed Button 1 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton1.onSelectValue = 0;
        breedButton1.onSubmitValue = 0;
        breedButton1.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton1.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton2 = new UIManagerScript.UIObject();
        breedButton2.gameObj = GameObject.Find("Corral Breed Button 2");
        breedButton2.subObjectTMPro = breedButton2.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton2.subObjectImage = GameObject.Find("Corral Breed Button 2 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton2.onSelectValue = 1;
        breedButton2.onSubmitValue = 1;
        breedButton2.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton2.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton3 = new UIManagerScript.UIObject();
        breedButton3.gameObj = GameObject.Find("Corral Breed Button 3");
        breedButton3.subObjectTMPro = breedButton3.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton3.subObjectImage = GameObject.Find("Corral Breed Button 3 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton3.onSelectValue = 2;
        breedButton3.onSubmitValue = 2;
        breedButton3.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton3.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton4 = new UIManagerScript.UIObject();
        breedButton4.gameObj = GameObject.Find("Corral Breed Button 4");
        breedButton4.subObjectTMPro = breedButton4.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton4.subObjectImage = GameObject.Find("Corral Breed Button 4 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton4.onSelectValue = 3;
        breedButton4.onSubmitValue = 3;
        breedButton4.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton4.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton5 = new UIManagerScript.UIObject();
        breedButton5.gameObj = GameObject.Find("Corral Breed Button 5");
        breedButton5.subObjectTMPro = breedButton5.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton5.subObjectImage = GameObject.Find("Corral Breed Button 5 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton5.onSelectValue = 4;
        breedButton5.onSubmitValue = 4;
        breedButton5.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton5.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton6 = new UIManagerScript.UIObject();
        breedButton6.gameObj = GameObject.Find("Corral Breed Button 6");
        breedButton6.subObjectTMPro = breedButton6.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton6.subObjectImage = GameObject.Find("Corral Breed Button 6 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton6.onSelectValue = 5;
        breedButton6.onSubmitValue = 5;
        breedButton6.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton6.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton7 = new UIManagerScript.UIObject();
        breedButton7.gameObj = GameObject.Find("Corral Breed Button 7");
        breedButton7.subObjectTMPro = breedButton7.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton7.subObjectImage = GameObject.Find("Corral Breed Button 7 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton7.onSelectValue = 6;
        breedButton7.onSubmitValue = 6;
        breedButton7.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton7.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton8 = new UIManagerScript.UIObject();
        breedButton8.gameObj = GameObject.Find("Corral Breed Button 8");
        breedButton8.subObjectTMPro = breedButton8.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton8.subObjectImage = GameObject.Find("Corral Breed Button 8 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton8.onSelectValue = 7;
        breedButton8.onSubmitValue = 7;
        breedButton8.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton8.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton9 = new UIManagerScript.UIObject();
        breedButton9.gameObj = GameObject.Find("Corral Breed Button 9");
        breedButton9.subObjectTMPro = breedButton9.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton9.subObjectImage = GameObject.Find("Corral Breed Button 9 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton9.onSelectValue = 8;
        breedButton9.onSubmitValue = 8;
        breedButton9.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton9.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton10 = new UIManagerScript.UIObject();
        breedButton10.gameObj = GameObject.Find("Corral Breed Button 10");
        breedButton10.subObjectTMPro = breedButton10.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton10.subObjectImage = GameObject.Find("Corral Breed Button 10 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton10.onSelectValue = 9;
        breedButton10.onSubmitValue = 9;
        breedButton10.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton10.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton11 = new UIManagerScript.UIObject();
        breedButton11.gameObj = GameObject.Find("Corral Breed Button 11");
        breedButton11.subObjectTMPro = breedButton11.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton11.subObjectImage = GameObject.Find("Corral Breed Button 11 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton11.onSelectValue = 10;
        breedButton11.onSubmitValue = 10;
        breedButton11.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton11.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButton12 = new UIManagerScript.UIObject();
        breedButton12.gameObj = GameObject.Find("Corral Breed Button 12");
        breedButton12.subObjectTMPro = breedButton12.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        breedButton12.subObjectImage = GameObject.Find("Corral Breed Button 12 Sprite").GetComponent<Image>(); // the monster sprite
        breedButton12.onSelectValue = 11;
        breedButton12.onSubmitValue = 11;
        breedButton12.myOnSelectAction = singleton.HoverOverMonsterButton;
        breedButton12.mySubmitFunction = singleton.SelectMonsterForBreeding;

        breedButtonList[0] = breedButton1;
        breedButtonList[1] = breedButton2;
        breedButtonList[2] = breedButton3;
        breedButtonList[3] = breedButton4;
        breedButtonList[4] = breedButton5;
        breedButtonList[5] = breedButton6;
        breedButtonList[6] = breedButton7;
        breedButtonList[7] = breedButton8;
        breedButtonList[8] = breedButton9;
        breedButtonList[9] = breedButton10;
        breedButtonList[10] = breedButton11;
        breedButtonList[11] = breedButton12;

        for (int i = 0; i < breedButtonList.Length; i++)
        {
            FontManager.LocalizeMe(breedButtonList[i].subObjectTMPro, TDFonts.WHITE);
        }

        breedButton1.neighbors[(int)Directions.NORTH] = confirmBreed;
        breedButton1.neighbors[(int)Directions.EAST] = breedButton2;
        breedButton1.neighbors[(int)Directions.WEST] = breedButton2;
        breedButton1.neighbors[(int)Directions.SOUTH] = breedButton3;

        breedButton2.neighbors[(int)Directions.NORTH] = confirmBreed;
        breedButton2.neighbors[(int)Directions.EAST] = breedButton1;
        breedButton2.neighbors[(int)Directions.WEST] = breedButton1;
        breedButton2.neighbors[(int)Directions.SOUTH] = breedButton4;

        breedButton3.neighbors[(int)Directions.NORTH] = breedButton1;
        breedButton3.neighbors[(int)Directions.EAST] = breedButton4;
        breedButton3.neighbors[(int)Directions.WEST] = breedButton4;
        breedButton3.neighbors[(int)Directions.SOUTH] = breedButton5;

        breedButton4.neighbors[(int)Directions.NORTH] = breedButton2;
        breedButton4.neighbors[(int)Directions.EAST] = breedButton3;
        breedButton4.neighbors[(int)Directions.WEST] = breedButton3;
        breedButton4.neighbors[(int)Directions.SOUTH] = breedButton6;

        breedButton5.neighbors[(int)Directions.NORTH] = breedButton3;
        breedButton5.neighbors[(int)Directions.EAST] = breedButton6;
        breedButton5.neighbors[(int)Directions.WEST] = breedButton6;
        breedButton5.neighbors[(int)Directions.SOUTH] = breedButton7;

        breedButton6.neighbors[(int)Directions.NORTH] = breedButton4;
        breedButton6.neighbors[(int)Directions.EAST] = breedButton5;
        breedButton6.neighbors[(int)Directions.WEST] = breedButton5;
        breedButton6.neighbors[(int)Directions.SOUTH] = breedButton8;

        breedButton7.neighbors[(int)Directions.NORTH] = breedButton5;
        breedButton7.neighbors[(int)Directions.EAST] = breedButton8;
        breedButton7.neighbors[(int)Directions.WEST] = breedButton8;
        breedButton7.neighbors[(int)Directions.SOUTH] = breedButton9;

        breedButton8.neighbors[(int)Directions.NORTH] = breedButton6;
        breedButton8.neighbors[(int)Directions.EAST] = breedButton7;
        breedButton8.neighbors[(int)Directions.WEST] = breedButton7;
        breedButton8.neighbors[(int)Directions.SOUTH] = breedButton10;

        breedButton9.neighbors[(int)Directions.NORTH] = breedButton7;
        breedButton9.neighbors[(int)Directions.EAST] = breedButton10;
        breedButton9.neighbors[(int)Directions.WEST] = breedButton10;
        breedButton9.neighbors[(int)Directions.SOUTH] = breedButton11;

        breedButton10.neighbors[(int)Directions.NORTH] = breedButton8;
        breedButton10.neighbors[(int)Directions.EAST] = breedButton9;
        breedButton10.neighbors[(int)Directions.WEST] = breedButton9;
        breedButton10.neighbors[(int)Directions.SOUTH] = breedButton12;

        breedButton11.neighbors[(int)Directions.NORTH] = breedButton9;
        breedButton11.neighbors[(int)Directions.EAST] = breedButton12;
        breedButton11.neighbors[(int)Directions.WEST] = breedButton12;
        breedButton11.neighbors[(int)Directions.SOUTH] = confirmBreed;

        breedButton12.neighbors[(int)Directions.NORTH] = breedButton10;
        breedButton12.neighbors[(int)Directions.EAST] = breedButton11;
        breedButton12.neighbors[(int)Directions.WEST] = breedButton11;
        breedButton12.neighbors[(int)Directions.SOUTH] = confirmBreed;

        confirmBreed.neighbors[(int)Directions.SOUTH] = breedButton1;
        confirmBreed.neighbors[(int)Directions.NORTH] = breedButton11;

        confirmText = confirmBreed.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        confirmText.text = StringManager.GetString("ui_button_confirm_breed");

        FontManager.LocalizeMe(confirmText, TDFonts.BLACK);

        breedHeader = GameObject.Find("Corral Breed Header").GetComponent<TextMeshProUGUI>();
        breedHeader.text = StringManager.GetString("ui_breed_header");

        FontManager.LocalizeMe(breedHeader, TDFonts.WHITE);

        hoverBreedMonsterStats = GameObject.Find("HoverBreedMonsterStats").GetComponent<MonsterListButton>();
        hoverMonsterStatsBattleText = GameObject.Find("HoverMonsterStatsBattleStatsText").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(hoverMonsterStatsBattleText, TDFonts.WHITE);

        friendshipText = GameObject.Find("Corral Breed Friendship").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(friendshipText, TDFonts.WHITE);

        friendshipContainer = GameObject.Find("Breeding Friendship Container");
        friendshipContainer.SetActive(false);

        singleton.ExitBreedingInterface(0);

        monstersSelected = new List<Monster>();
    }
	
	public void SelectMonsterForBreeding(int buttonID)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        Monster getMon = MetaProgressScript.localTamedMonstersForThisSlot[buttonID].monsterObject;
        if (getMon == null)
        {
            Debug.Log("Monster in slot " + buttonID + " is null?");
            return;
        }

        if (monstersSelected.Contains(getMon))
        {
            monstersSelected.Remove(getMon);
            confirmBreed.enabled = false;
            confirmBreed.gameObj.SetActive(false);
            breedButtonList[buttonID].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
            friendshipContainer.SetActive(false);
        }
        else
        {
            if (monstersSelected.Count == 2)
            {
                UIManagerScript.PlayCursorSound("Error");
                return;
            }
            monstersSelected.Add(getMon);
            breedButtonList[buttonID].gameObj.GetComponent<Image>().color = Color.white;
            if (monstersSelected.Count == 2)
            {
                confirmBreed.enabled = true; 
                confirmBreed.gameObj.SetActive(true);                
                hoverBreedMonsterStats.gameObject.SetActive(false);
                friendshipContainer.SetActive(true);

                string friendText = "";

                StringManager.SetTag(0, monstersSelected[1].displayName);
                friendText += UIManagerScript.lightPurpleHexColor + monstersSelected[0].displayName + "</color> --> " + StringManager.GetString("ui_corral_feelings") + "</color> " + monstersSelected[0].tamedMonsterStuff.GetRelationshipString(monstersSelected[1].tamedMonsterStuff) + "\n";
                StringManager.SetTag(0, monstersSelected[0].displayName);
                friendText += UIManagerScript.lightPurpleHexColor + monstersSelected[1].displayName + "</color> --> " + StringManager.GetString("ui_corral_feelings") + "</color> " + monstersSelected[1].tamedMonsterStuff.GetRelationshipString(monstersSelected[0].tamedMonsterStuff);

                /* Debug.Log(monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff));
                Debug.Log(monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff));
                Debug.Log(MonsterCorralScript.MIN_BREED_LEVEL); */

                int relationship1Value = monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff);
                int relationship2Value = monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff);

                if ((monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff) < 0) || (monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff) < 0))
                {
                    friendText += "\n" + StringManager.GetString("ui_monsterbreed_1");
                }
                else if (relationship1Value >= MonsterCorralScript.MIN_BREED_LEVEL
                    && relationship2Value >= MonsterCorralScript.MIN_BREED_LEVEL)
                {
                    friendText += "\n" + StringManager.GetString("ui_monsterbreed_3");
                }
                else
                {
                    friendText += "\n" + StringManager.GetString("ui_monsterbreed_2");
                }

                friendshipText.text = friendText;
                UIManagerScript.ChangeUIFocusAndAlignCursor(confirmBreed);
            }            
        }        
    }

    public static void OpenBreedingInterface()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        UIManagerScript.PlayCursorSound("Select");
        monstersSelected.Clear();
        corralBreedInterface.SetActive(true);
        corralBreedInterfaceOpen = true;
        GuideMode.OnFullScreenUIOpened();
        friendshipContainer.SetActive(false);
        hoverBreedMonsterStats.gameObject.SetActive(false);

        for (int i = 0; i < breedButtonList.Length; i++)
        {
            breedButtonList[i].gameObj.SetActive(false);
            breedButtonList[i].enabled = false;
            breedButtonList[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
        }

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        for (int i = 0; i < maxMonsterCount; i++)
        {            
            Monster m = MetaProgressScript.localTamedMonstersForThisSlot[i].monsterObject;
            //Debug.Log("Enabling button " + i + " " + m.displayName + " " + m.tamedMonsterStuff.displayName);
            if (m.myAnimatable == null) continue;
            Sprite spr = m.myAnimatable.GetAnim().mySprites[0].mySprite;
            breedButtonList[i].gameObj.SetActive(true);
            breedButtonList[i].enabled = true;
            breedButtonList[i].subObjectImage.sprite = spr;
            breedButtonList[i].subObjectImage.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(spr.rect.size.x * 2f, spr.rect.size.y * 2f);
            breedButtonList[i].subObjectTMPro.text = m.displayName;
        }

        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(corralBreedInterface.transform);        
        UIManagerScript.ChangeUIFocusAndAlignCursor(breedButtonList[0]);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenAlignCursor(0.025f, breedButtonList[0]));
    }

    public void ExitBreedingInterface(int dummy)
    {
        GuideMode.OnFullScreenUIClosed();
        corralBreedInterface.SetActive(false);
        corralBreedInterfaceOpen = false;
        UIManagerScript.HideDialogMenuCursor();
    }

    public static float GetJPGrowthCost(Monster m1, Monster m2)
    {
        float calcLevel = (m1.myStats.GetLevel() + m2.myStats.GetLevel()) / 2f;

        calcLevel = Mathf.Pow(calcLevel, 1.3f);

        float jpCost = calcLevel * 36f;

        // Modify based on rarity

        Rarity rToUse = Rarity.COMMON;
        if ((int)m1.tamedMonsterStuff.GetMonsterRarity() >= (int)m2.tamedMonsterStuff.GetMonsterRarity())
        {
            rToUse = m1.tamedMonsterStuff.GetMonsterRarity();
        }        
        else
        {
            rToUse = m2.tamedMonsterStuff.GetMonsterRarity();
        }

        switch(rToUse)
        {
            case Rarity.UNCOMMON:
                jpCost *= 1.15f;
                break;
            case Rarity.MAGICAL:
                jpCost *= 1.3f;
                break;
            case Rarity.ANCIENT:
                jpCost *= 1.6f;
                break;
            case Rarity.ARTIFACT:
                jpCost *= 2f;
                break;
        }

        return jpCost;
    }

    public void ConfirmMonstersForBreeding(int dummy)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (monstersSelected.Count < 2)
        {
            return;
        }

        ExitBreedingInterface(0);

        int maxJPGrowthCost = (int)GetJPGrowthCost(monstersSelected[0], monstersSelected[1]);

        GameMasterScript.gmsSingleton.SetTempGameData("breed_m1_id", monstersSelected[0].actorUniqueID);
        GameMasterScript.gmsSingleton.SetTempGameData("breed_m2_id", monstersSelected[1].actorUniqueID);

        StringManager.SetTag(0, monstersSelected[0].displayName);
        StringManager.SetTag(1, monstersSelected[1].displayName);

        int relationshipValue1 = monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff);
        int relationshipValue2 = monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff);

        if (relationshipValue1 >= MonsterCorralScript.MIN_BREED_LEVEL)
        {
            if (relationshipValue2 >= MonsterCorralScript.MIN_BREED_LEVEL)
            {
                UIManagerScript.StartConversationByRef("train_monster_corral", DialogType.STANDARD, null);
                int maxPossibleJPSpend = (int)GetJPGrowthCost(monstersSelected[0], monstersSelected[1]);
                if (maxPossibleJPSpend >= GameMasterScript.heroPCActor.GetCurJP())
                {
                    maxPossibleJPSpend = (int)GameMasterScript.heroPCActor.GetCurJP();
                }
                GameMasterScript.gmsSingleton.SetTempFloatData("mon_breed_maxjp", (float)maxPossibleJPSpend);
                GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "jp");
                UIManagerScript.EnableDialogSlider(StringManager.GetString("ui_quantity_monstertrainjp"), 0, maxPossibleJPSpend, false, 1.0f);
                return;
            }
        }

        BeginMonsterBreedRoutine();
    }

    public static void BeginMonsterBreedRoutine()
    {
        if (monstersSelected[0] == null || monstersSelected[1] == null)
        {
            Debug.Log("Something failed in monster breed.");
            return;
        }

        UIManagerScript.FadeOut(3f);
        MusicManagerScript.RequestPlayNonLoopingMusicFromScratchWithCrossfade("resttheme", true);		
        MusicManagerScript.singleton.WaitThenPlay("grovetheme", 8f);

        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.singletonUIMS.WaitThenFadeIn(3f, 3f);


        StringManager.SetTag(0, monstersSelected[0].displayName);
        StringManager.SetTag(1, monstersSelected[1].displayName);

        CombatManagerScript.GenerateSpecificEffectAnimation(monstersSelected[0].GetPos(), "CharmEffectSystem", null);
        CombatManagerScript.WaitThenGenerateSpecificEffect(monstersSelected[0].GetPos(), "CharmEffectSystem", null, 0.7f);
        CombatManagerScript.GenerateSpecificEffectAnimation(monstersSelected[1].GetPos(), "CharmEffectSystem", null);
        CombatManagerScript.WaitThenGenerateSpecificEffect(monstersSelected[1].GetPos(), "CharmEffectSystem", null, 0.7f);

        Item meal = GameMasterScript.heroPCActor.myInventory.GetItemByID(GameMasterScript.gmsSingleton.ReadTempGameData("romanticmealitem"));

        Consumable c = meal as Consumable;
        c.ChangeQuantity(-1);
        if (c.Quantity == 0)
        {
            GameMasterScript.heroPCActor.myInventory.RemoveItem(meal);
        }

        int relationshipValue1 = monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff);
        int relationshipValue2 = monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff);

        if (relationshipValue1 >= MonsterCorralScript.MIN_BREED_LEVEL)
        {
            if (relationshipValue2 >= MonsterCorralScript.MIN_BREED_LEVEL)
            {
                Monster newlyCreatedMonster = MonsterCorralScript.BreedMonsters(monstersSelected[0].tamedMonsterStuff, monstersSelected[1].tamedMonsterStuff);
                MapMasterScript.activeMap.AddActorToMap(newlyCreatedMonster);
                MetaProgressScript.AddExistingTamedMonsterActorToCorral(newlyCreatedMonster);
                MonsterCorralScript.NameMonsterThenAddToCorralForFirstTime(newlyCreatedMonster, true, 5.7f);
                GameMasterScript.gmsSingleton.statsAndAchievements.IncrementMonstersHatched();
            }
        }

        int mealsCooked = ProgressTracker.CheckProgress(TDProgress.ROMANCE_MEALS_SHARED, ProgressLocations.META);
        if (mealsCooked < 0)
        {
            mealsCooked = 0;
        }
        mealsCooked++;
        ProgressTracker.SetProgress(TDProgress.ROMANCE_MEALS_SHARED, ProgressLocations.META, mealsCooked);

        // Develop the monsters' relationship
        if (monstersSelected[0].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[1].tamedMonsterStuff) < 0)
        {
            monstersSelected[0].tamedMonsterStuff.SetRelationshipAmount(monstersSelected[1].tamedMonsterStuff, 0);
        }
        else
        {
            monstersSelected[0].tamedMonsterStuff.AdjustRelationship(monstersSelected[1].tamedMonsterStuff, UnityEngine.Random.Range(1, 4), false);
        }

        if (monstersSelected[1].tamedMonsterStuff.TryGetRelationshipAmount(monstersSelected[0].tamedMonsterStuff) < 0)
        {
            monstersSelected[1].tamedMonsterStuff.SetRelationshipAmount(monstersSelected[0].tamedMonsterStuff, 0);
        }
        else
        {
            monstersSelected[1].tamedMonsterStuff.AdjustRelationship(monstersSelected[0].tamedMonsterStuff, UnityEngine.Random.Range(1, 4), false);
        }

        StringManager.SetTag(0, monstersSelected[0].displayName);
        StringManager.SetTag(1, monstersSelected[1].displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("log_corral_relationship"), GameMasterScript.heroPCActor);
    }

    public static void CancelPressed()
    {
        if (monstersSelected.Count > 0)
        {
            int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

            for (int i = 0; i < maxMonsterCount; i++)
            {
                foreach(Monster m in monstersSelected)
                {
                    if (MetaProgressScript.localTamedMonstersForThisSlot[i].monsterObject == m)
                    {
                        breedButtonList[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
                    }
                }
                
            }
            friendshipContainer.SetActive(false);
            monstersSelected.Clear();
            confirmBreed.enabled = false;
            confirmBreed.gameObj.SetActive(false);
            UIManagerScript.ChangeUIFocusAndAlignCursor(breedButton1);
        }
        else
        {
            singleton.ExitBreedingInterface(0);
        }
    }

    public void HoverOverMonsterButton(int buttonID)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (buttonID >= breedButtonList.Length || buttonID < 0)
        {
            hoverBreedMonsterStats.gameObject.SetActive(false);
            return;
        }

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        if (buttonID >= maxMonsterCount)
        {
            Debug.Log("WARNING: Hover ID for monster button exceeds max length.");
            return;
        }
        UIManagerScript.ChangeUIFocusAndAlignCursor(breedButtonList[buttonID]);

        TamedCorralMonster m = MetaProgressScript.localTamedMonstersForThisSlot[buttonID];
        hoverBreedMonsterStats.gameObject.SetActive(true);
        hoverBreedMonsterStats.happinessValue.text = m.GetHappinessString();
        hoverBreedMonsterStats.weightValue.text = m.GetWeightString();
        hoverBreedMonsterStats.uniqueValue.text = m.GetRarityString();
        hoverBreedMonsterStats.beautyValue.text = m.GetBeautyString();
        hoverMonsterStatsBattleText.text = m.GetBattlePowerStats();

        /* Vector3 curPos = new Vector3(0, 0, 0);
        Vector3 refButtonPos = breedButtonList[buttonID].gameObj.transform.localPosition;
        if (refButtonPos.x < 0)
        {
            curPos.x = 400f;
        }
        else {
            curPos.x = -400f;   
        }
        curPos.y = refButtonPos.y;
        hoverBreedMonsterStats.gameObject.transform.localPosition = curPos; */
        
    }

    public void HoverToBreedButton(int dummy)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        UIManagerScript.ChangeUIFocusAndAlignCursor(confirmBreed);
    }
}
